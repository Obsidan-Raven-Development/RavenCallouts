///<summary> RavenCallouts.Medical
///
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.Callouts;

#region Version change log 
/// <summary>V 0.1.4
/// - Added Callout
/// 
/// </summary>
#endregion

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Low)]
    public class medical : Callout
    {
        #region Declaration
        public CalloutState calloutState;
        private Ped victim;
        private Ped shooter;
        Vector3 location;
        private bool pursuitCreated = false;
        private LHandle pursuit;
        private Blip victimBlip;
        private int speechCheck { get; set; }
        private bool Debug = true;
        private int r;
        private int r1;
        #endregion

        #region Enums
        public enum CalloutState
        {
            Enroute,
            OnScene,
            DecisionMade,
            End
        }
        #endregion

        public override bool OnBeforeCalloutDisplayed()
        {
            // Find new Random Number Gen, better results are needed.                       
            if (Debug == true)
            {
                r = 1;                                //Debug only
                r1 = 1;
            }
            else

             r = new Random().Next(4, 100);           //Release
            r1 = new Random().Next(1, 2);

            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(550f));
            victim = new Ped(location);

            ShowCalloutAreaBlipBeforeAccepting(location, 150f);
            AddMinimumDistanceCheck(200f, location);


            if (!victim.Exists()) return false;

            victim.BlockPermanentEvents = true;

            victim.Tasks.Wander();

            CalloutMessage = "Medcial Emergency";
            CalloutPosition = location;

            Functions.PlayScannerAudioUsingPosition("", location);

            Game.LogTrivialDebug("Raven.Displayed");

            if (Debug == true)
            {
                Game.DisplayNotification("DEBUG MODE ENABLED!");
            }

            return base.OnBeforeCalloutDisplayed();

        }

        public override bool OnCalloutAccepted()
        {            
            GameFiber.Wait(10000);

            calloutState = CalloutState.Enroute;
            victim.Tasks.Clear();
            if (r1 == 1)
            { victim.Health = 0; Game.LogTrivialDebug("Victim Killed"); }

            else if (r1 == 2)
            {
                shooter = new Ped(victim.FrontPosition);
                shooter.Inventory.GiveNewWeapon("WEAPON_COMBAT_KNIFE", 0, true);
                shooter.Tasks.FightAgainst(victim, 5000);
                victim.Health = 0;
                Game.LogTrivialDebug("Shooter spawned");
            }

            if (victim.IsDead)
            {
                shooter.Tasks.Flee(victim, 100, 10000);
                GameFiber.Wait(10000);
                shooter.Tasks.Wander();
            }

            victimBlip = victim.AttachBlip();

            Game.DisplaySubtitle("Dispatch: ", 10000);
            Game.LogTrivialDebug("Raven.Attached");
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            this.End();
        }

        public override void Process()
        {
            base.Process();

            if (calloutState == CalloutState.End)
            {
                End();
            }

            if (calloutState == CalloutState.Enroute && Game.LocalPlayer.Character.Position.DistanceTo(victim) <= 5)
            { calloutState = CalloutState.OnScene; startMedical(); victimBlip.DisableRoute(); Game.LogTrivial("Raven arrived"); }

            if (calloutState == CalloutState.End && !Functions.IsPursuitStillRunning(pursuit))
            { End(); }
        }

        public override void End()
        {
            calloutState = CalloutState.End;
            if (victim.Exists()) victim.Dismiss();
            if (victimBlip.Exists()) victimBlip.Delete();
            Game.LogTrivialDebug("Raven.Clean");
            base.End();
        }

        public void startMedical()
        {

        }
    }
}