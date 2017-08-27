using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using ComputerPlus;
using function;

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Supicous Vehicle", CalloutProbability.VeryLow)]
    public class DeadBody : Callout
    {
        #region Declatration
        public CalloutState calloutState;
        private Ped Suspect;
        private Ped Victim;
        private Ped Victim2;
        Vehicle SuspectVehicle;
        Vector3 location;
        Blip SuspectBlip;
        private LHandle Pursuit;
        private bool PursuitCreated = false;
        public int speechCheck { get; set; }
        private int r;
        private bool Debug = true;
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
            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(500f));
            SuspectVehicle = new Vehicle(3286105550, location); //Spawn a Obey Tailgaiter as the suspect Vehicle
            SuspectVehicle.IsPersistent = true;

            ShowCalloutAreaBlipBeforeAccepting(location, 100f);
            AddMinimumDistanceCheck(20f, location);


            Suspect = SuspectVehicle.CreateRandomDriver();
            Suspect.IsPersistent = true;
            Suspect.BlockPermanentEvents = true;

            Victim = new Ped(location);
            Victim.IsPersistent = true;
            Victim.BlockPermanentEvents = true;

            Victim2 = new Ped(location);
            Victim2.IsPersistent = true;
            Victim2.BlockPermanentEvents = true;

            Victim.WarpIntoVehicle(SuspectVehicle, 0); //Warp victim into car
            Victim2.WarpIntoVehicle(SuspectVehicle, 1);

            Victim.Health = 0; //Kill Victims
            Victim2.Health = 0;

            CalloutMessage = "Suspicous Vehicle";
            CalloutPosition = location;

            Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 CRIME_10_99_DAVID_01 IN_01 POSITION UNITS_RESPOND_CODE_03_01", location);

            Game.LogTrivialDebug("Raven.Displayed");

            if (Debug == true)
            {
                Game.DisplayNotification("DEBUG MODE ENABLED!");
            }

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            calloutState = CalloutState.Enroute;
            SuspectBlip = Suspect.AttachBlip();
            SuspectBlip.Color = System.Drawing.Color.Red;
            SuspectBlip.EnableRoute(System.Drawing.Color.Red);

            Suspect.Tasks.CruiseWithVehicle(SuspectVehicle, 75f, VehicleDrivingFlags.Emergency);

            // Dialouge needs to be rewritten. Mention something about a person slumped over in the back or passenger seat and that the driver was driving erratic or something, as it is, it doesnt make much sense.
            Game.DisplaySubtitle("Dispatch: Caller reports seeing two slumped passengers and the driver driving erraticly, no futher details at the moment. Be Advise use caution when aproching vehicle");
            Game.LogTrivialDebug("Raven.Attached");
            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            End();
        }

        public override void Process()
        {
            base.Process();

            if (calloutState == CalloutState.End)
            {
                End();
            }

            if (calloutState == CalloutState.Enroute && Game.LocalPlayer.Character.Position.DistanceTo(SuspectVehicle) <= 10)
            { calloutState = CalloutState.OnScene; startBody(); SuspectBlip.DisableRoute(); Game.LogTrivial("Raven arrived"); }

            if (calloutState == CalloutState.End && !Functions.IsPursuitStillRunning(Pursuit))
            { End(); }
        }

        public override void End()
        {
            calloutState = CalloutState.End;
            if (Suspect.Exists()) Suspect.Dismiss();
            if (Victim.Exists()) Victim.Dismiss();
            if (Victim2.Exists()) Victim2.Dismiss();
            if (SuspectVehicle.Exists()) SuspectVehicle.Dismiss();
            if (SuspectBlip.Exists()) SuspectBlip.Delete();
            Game.LogTrivialDebug("Raven.Clean");
            base.End();
        }

        public void startBody()
        {
            ///<summary>
            ///Add flee options and than add a iscuffed check, than create dialouge for suspect after suspect is cuffed.
            /// </summary>
            /// 
            GameFiber.StartNew(delegate
            {
                speechCheck = 1;
                this.Pursuit = Functions.CreatePursuit();

                // Find new Random Number Gen, better results are needed.                       
                if (Debug == true)
                {
                    r = 2;                                //Debug only
                }
                else

                    r = new Random().Next(4, 100);       //Release  



                calloutState = CalloutState.DecisionMade;


                #region Suspect Flee (On Foot)
                if (r <= 49 && r >= 11 || r == 1)
                {
                    Game.LogTrivialDebug("Raven1");
                    Suspect.Tasks.LeaveVehicle(flags: LeaveVehicleFlags.LeaveDoorOpen);
                    Suspect.Tasks.Clear();

                    GameFiber.Wait(1000);

                    if (!Suspect.IsInAnyVehicle(false))
                    {

                        Suspect.Tasks.Flee(Game.LocalPlayer.Character, 10000, -1);
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        PursuitCreated = true;
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);
                    }

                    if (Suspect.IsDead)
                    { calloutState = CalloutState.End; End(); }
                    if (Suspect.IsCuffed)
                    { calloutState = CalloutState.End; End(); }
                }
                #endregion

                #region Suspect Fight
                if (r >= 50 && r >= 11 && r > 3 || r == 2)
                {
                    Game.LogTrivialDebug("Raven2");

                    Suspect.Tasks.LeaveVehicle(flags: LeaveVehicleFlags.LeaveDoorOpen);

                    GameFiber.Wait(500);

                    Suspect.Tasks.Clear();

                    GameFiber.Wait(500);

                    if (!Suspect.IsInAnyVehicle(false))
                    {
                        Suspect.Tasks.FightAgainst(Game.LocalPlayer.Character, -1);

                        GameFiber.Wait(3000);

                        // Created a pursuit so backup works properly. Looks good and should run flawlessly here.
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        PursuitCreated = true;
                        /*
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                        */
                    }

                    if (Suspect.IsDead)
                    { calloutState = CalloutState.End; End(); }
                    if (Suspect.IsCuffed)
                    { calloutState = CalloutState.End; End(); }
                }
                #endregion

                #region Suspect Flee (In Vehicle)
                if (r <= 10 && r > 3 || r == 3)
                {
                    Game.LogTrivialDebug("Raven3");
                    SuspectBlip = Suspect.AttachBlip();
                    
                    Pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(Pursuit, Suspect);
                    Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                    PursuitCreated = true;

                    if (Suspect.IsDead)
                    { calloutState = CalloutState.End; End(); }
                    if (Suspect.IsCuffed)
                    { calloutState = CalloutState.End; End(); }
                }
                #endregion
            });
        }
    }
}
