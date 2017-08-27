using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Parking Violation", CalloutProbability.VeryLow)]
    public class parkingViolation : Callout
    {
        private Ped suspect;
        private Vehicle suspectVehicle;
        private Blip vehicleBlip;
        private Vector3 location;
        private Blip suspectBlip;
        private bool pursuitCreated = false;
        private LHandle pursuit;
        private CalloutState calloutState;
        private int speechCheck;

        enum CalloutState
        {
            End = 0,
            Fight = 1,
            Flee = 2,
            Talk = 3
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(750f));

            ShowCalloutAreaBlipBeforeAccepting(location, 200f);
            AddMinimumDistanceCheck(150f, location);

            CalloutMessage = "Illegally Parked Vehicle";
            CalloutPosition = location;

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            ///Create Suspect Vehicle
            suspectVehicle = new Vehicle(location); //Find a vehicle to use instead of random cause of helicopters and blimps spawning.
            suspectVehicle.IsPersistent = true;
            vehicleBlip = suspectVehicle.AttachBlip();
            vehicleBlip.IsFriendly = false;
            vehicleBlip.EnableRoute(System.Drawing.Color.Black);
            vehicleBlip.Color = System.Drawing.Color.Black;

            ///Create Suspect Ped
            suspect = new Ped(location.Around(10f));
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.Wander();

            //Set Callout using Random numbers.
            calloutState = (CalloutState)new Random().Next(1,3);
            Game.DisplayNotification("Citizens report vehicle blocking traffic lanes, get to the vehicle and get it towed, than find the owner and cite them. (Don't forget to search the car)");

            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            speechCheck = 1;
            while (Game.IsKeyDownRightNow(System.Windows.Forms.Keys.End))
            { End(); }

            { Game.DisplayHelp("Press ~r~END~w~ to end the callout"); }

            #region Fight
            ///Check Callout state and set the distance to 5 meters
            if (calloutState == CalloutState.Fight && !pursuitCreated && Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 5f)
            {
                Game.LogTrivialDebug("RG:Suspect Fight");
                Game.DisplaySubtitle("Suspect: What you want you fucking pig! I'm gonna fuck you up bitch!", 5000);
                suspectBlip = suspect.AttachBlip();
                suspect.Tasks.FightAgainst(Game.LocalPlayer.Character);
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, suspect);
                Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                if (suspect.IsDead)
                { this.End(); }
                if (suspect.IsCuffed)
                { this.End(); }
            }
            #endregion
            else
            #region Flee
                if (calloutState == CalloutState.Flee && Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 10f)
            {
                Game.LogTrivialDebug("RG:Suspect Flee");
                suspectBlip = suspect.AttachBlip();

                suspect.Tasks.Flee(Game.LocalPlayer.Character, 10000, 10000);
                pursuit = Functions.CreatePursuit();
                Functions.AddPedToPursuit(pursuit, suspect);
                Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                pursuitCreated = true;

                if (suspect.IsDead)
                { this.End(); }

                if (suspect.IsCuffed)
                    { this.End(); }
                if (pursuitCreated && !Functions.IsPursuitStillRunning(pursuit))
                { this.End(); }
            }
            #endregion
            else
            #region Talk
            if (calloutState == CalloutState.Talk && Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 10f)
            {
                suspectBlip = suspect.AttachBlip();
                suspectBlip.IsFriendly = false;
                Game.DisplayNotification("Officer: Dispatch, I have the vehicle owner, show me 10-6");

                while (Game.LocalPlayer.Character.DistanceTo(suspect.Position) < 5f )
                {
                    Game.DisplayHelp("Press ~y~Y~w~ to talk.");
                    if (speechCheck == 1  && Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                    {
                        Game.DisplaySubtitle("Suspect: *Slurred Speech* What seems to be the officer problem?");
                        speechCheck++;
                        Game.DisplayHelp("Use ~b~LSPDFR~w~ to stop the suspect.");
                    }
                }
                if(suspect.IsStopped)
                { this.End(); }
            }
            #endregion


            if (pursuitCreated && !Functions.IsPursuitStillRunning(pursuit))
            { this.End(); }
            if(suspect.IsDead)
            { this.End(); }
            if (suspect.IsCuffed)
            { this.End(); }
        }

        public override void End()
        {
            base.End();
            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (vehicleBlip.Exists()) { vehicleBlip.Delete(); }
        }
    }
}
