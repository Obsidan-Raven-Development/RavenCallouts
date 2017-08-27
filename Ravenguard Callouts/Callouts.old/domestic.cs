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
using CPWC;
using RavenLibrary;

namespace RavenguardCallouts.Callouts
{
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Low)]
    public class domestic : Callout
    {
        #region Declaration
        private Ped Suspect;
        private Ped Victim;
        //private Ped Witness;
        Guid callID;
        Vector3 location;
        Blip SuspectBlip;
        private bool PursuitCreated = false;
        private LHandle Pursuit;
        public Common.CalloutState 
        private int SpeechCheck { get; set; }
        Vehicle SuspectVehicle;
        bool hasArrived;
        bool computerPlusRunning;
        #endregion


        public override bool OnBeforeCalloutDisplayed()
        {
            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(450f));

            ShowCalloutAreaBlipBeforeAccepting(location, 150f);
            AddMinimumDistanceCheck(60f, location);

            CalloutMessage = "Domestic Disturbance";
            CalloutPosition = location;
            hasArrived = false;

           Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT_01 CRIME_SHOTS_FIRED_01 IN POSITION UNITS_RESPOND_CODE_03_01", location);

            #region Computer+ Integration
            // Check if Computer+ 1.3 or higher is running
            // ComputerPlus+ 1.2.2 and below does not have an API, so the version check is necessary
            computerPlusRunning = Function.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));

            // Creates the callout within the computer
            if (computerPlusRunning)
                callID = ComputerPlusFuncs.CreateCallout("Domestic Dispute", "DOMESTIC", location,
                    (int)EResponseType.Code_3, "Domestic Dispute in street.");
            #endregion

            return base.OnBeforeCalloutDisplayed();
        }


        public override void OnCalloutDisplayed()
        {
            // Updates the callout's status to "Dispatched" when the player sees the callout on screen
            if (computerPlusRunning)
                ComputerPlusFuncs.UpdateCalloutStatus(callID, (int)ECallStatus.Dispatched);
            base.OnCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            #region Computer+ Integration
            // Updates the callout's status to "Unit Responding" when the player accepts
            if (computerPlusRunning)
                ComputerPlusFuncs.SetCalloutStatusToUnitResponding(callID);
            #endregion

            Suspect = new Ped(location);
            Suspect.IsPersistent = true;
            

            Victim = new Ped(location);
            Victim.IsPersistent = true;

            Suspect.BlockPermanentEvents = true;
            Victim.BlockPermanentEvents = true;

            SuspectBlip = Suspect.AttachBlip();
            Suspect.Inventory.GiveNewWeapon(1593441988, 120, true);
            SuspectBlip.IsFriendly = false;

            Suspect.Tasks.AimWeaponAt(Victim, -1);
            Victim.Tasks.PutHandsUp(100000, Suspect);

            SuspectVehicle = new Vehicle(3286105550, location.Around(30f));
            SuspectVehicle.IsPersistent = true;


            //calloutState = (CalloutState)new Random(5171154).Next(1, 5);
            calloutState = (Common.CalloutState)new Random().Next(1, 5);
            Game.DisplayNotification("Citizens report possible shots fired after hearing some arguing, respond ~r~Code 3~w~ to the area and investigate.");


            return base.OnCalloutAccepted();
        }

        public override void Process()
        {

            base.Process();
            SpeechCheck = 1;
            #region Fight
            if (calloutState == CalloutState.Fight && Game.LocalPlayer.Character.DistanceTo(Suspect.Position) < 10f && !hasArrived)
                    {
                        Game.LogTrivialDebug("RG:Suspect Fight 1");
               
                #region Computer+ Integration
                                
                /* Once the player is on scene (approx. 20 metres from the call's location),
                     the callout's status will change to "At Scene". It will also add an update
                     saying "This is an update." and will include both the vehicle and the driver
                     on the computer's call details screen for easy searching */
                if (computerPlusRunning)
                        {
                            ComputerPlusFuncs.SetCalloutStatusToAtScene(callID);
                            ComputerPlusFuncs.AddUpdateToCallout(callID, "Officer arrived on Scene");
                            ComputerPlusFuncs.AddPedToCallout(callID, Suspect);
                            ComputerPlusFuncs.AddVehicleToCallout(callID, SuspectVehicle);
                        }
                #endregion

                Suspect.Tasks.FightAgainst(Victim, -1);
                        Victim.Tasks.Flee(Suspect, 1000000, -1);
                        if (Suspect.IsDead)
                        { End(); }
                        else if (Suspect.IsCuffed)
                        { End(); }
            }
            #endregion
            else
            #region Flee
                    if (calloutState == CalloutState.Flee && !PursuitCreated && Game.LocalPlayer.Character.DistanceTo(Suspect.Position) < 10f 
                                                                                                                              && !hasArrived)
                    {

                        Game.LogTrivialDebug("RG:Suspect Flee1");

                #region Computer+ Integration

                /* Once the player is on scene (approx. 20 metres from the call's location),
                     the callout's status will change to "At Scene". It will also add an update
                     saying "This is an update." and will include both the vehicle and the driver
                     on the computer's call details screen for easy searching */
                if (computerPlusRunning)
                {
                    ComputerPlusFuncs.SetCalloutStatusToAtScene(callID);
                    ComputerPlusFuncs.AddUpdateToCallout(callID, "Officer arrived on Scene");
                    ComputerPlusFuncs.AddPedToCallout(callID, Suspect);
                    ComputerPlusFuncs.AddVehicleToCallout(callID, SuspectVehicle);
                }
                #endregion

                Suspect.Tasks.Flee(Game.LocalPlayer.Character, 100000, -1);
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, Suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        PursuitCreated = true;

                        if (Suspect.IsDead)
                        { End(); }

                        if (Suspect.IsCuffed)
                        { End(); }
                    }
            #endregion
            else
            #region Talk
                    if (calloutState == CalloutState.Talk || calloutState == CalloutState.Talk2 && !hasArrived)
                    {
                        #region Computer+ Integration

                        /* Once the player is on scene (approx. 20 metres from the call's location),
                             the callout's status will change to "At Scene". It will also add an update
                             saying "This is an update." and will include both the vehicle and the driver
                             on the computer's call details screen for easy searching */
                        if (computerPlusRunning)
                        {
                            ComputerPlusFuncs.SetCalloutStatusToAtScene(callID);
                            ComputerPlusFuncs.AddUpdateToCallout(callID, "Officer arrived on Scene");
                            ComputerPlusFuncs.AddPedToCallout(callID, Suspect);
                            ComputerPlusFuncs.AddVehicleToCallout(callID, SuspectVehicle);
                        }
                        #endregion

                while (Game.LocalPlayer.Character.DistanceTo(Suspect.Position) < 10f)
                                {
                            Game.DisplayHelp("Press Y to talk.");                    
                                if (SpeechCheck == 1 && Game.IsKeyDown(System.Windows.Forms.Keys.Y))
                                {
                                Game.DisplaySubtitle("Me:Put the gun down!", 3000);
                                SpeechCheck++;
                                }

                            if (SpeechCheck == 2)
                            {
                                Game.DisplaySubtitle("I surrender don't Kill me!", 3000);
                                SpeechCheck++;
                            }
                            if (SpeechCheck == 3)
                            {
                                Suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                                Victim.Tasks.Flee(Suspect, 1000000, -1);
                                SpeechCheck++;
                            }

                                if (Suspect.IsDead)
                                { End(); }

                                if (Suspect.IsCuffed)
                                { End(); }
                            }                
                    }
            #endregion
            else
            #region Vehicle Pursuit
            if (calloutState == CalloutState.Pursuit && !PursuitCreated && Game.LocalPlayer.Character.DistanceTo(Suspect.Position) < 10f
                                                                                                                         && !hasArrived)
            {
                #region Computer+ Integration

                /* Once the player is on scene (approx. 20 metres from the call's location),
                     the callout's status will change to "At Scene". It will also add an update
                     saying "This is an update." and will include both the vehicle and the driver
                     on the computer's call details screen for easy searching */
                if (computerPlusRunning)
                {
                    ComputerPlusFuncs.SetCalloutStatusToAtScene(callID);
                    ComputerPlusFuncs.AddUpdateToCallout(callID, "Officer arrived on Scene, Suspect fleeing in vehicle attach available units.");
                    ComputerPlusFuncs.AddPedToCallout(callID, Suspect);
                    ComputerPlusFuncs.AddVehicleToCallout(callID, SuspectVehicle);
                }
                #endregion

                Pursuit = Functions.CreatePursuit();
                Suspect.Tasks.FireWeaponAt(Victim, 10000, FiringPattern.FullAutomatic);
                Suspect.Accuracy = 100;
                Suspect.Tasks.EnterVehicle(SuspectVehicle, 0);
                Suspect.Tasks.CruiseWithVehicle(SuspectVehicle, 75, VehicleDrivingFlags.IgnorePathFinding);
                Functions.AddPedToPursuit(Pursuit, Suspect);
                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                PursuitCreated = true;
            }
            #endregion

            #region If End Statements
            if (PursuitCreated && !Functions.IsPursuitStillRunning(Pursuit))
                {
                    End();
                }

            if (Suspect.IsDead)
                {
                    End();
                }
               
            if (Suspect.IsCuffed)
                {
                    End();
                }

            if (this.calloutState == CalloutState.End)
            {
                this.End();
            }
        }
        #endregion

        public override void End()
        {
            if (computerPlusRunning)
            {
                // Changes the call's status to "Concluded" when the callout ends
                ComputerPlusFuncs.ConcludeCallout(callID);
            }
            base.End();
            if (Victim.Exists()) { Victim.Tasks.Wander(); }
            if (SuspectBlip.Exists()) { SuspectBlip.Delete(); }
            if (Suspect.Exists()) { Suspect.Dismiss(); }
            if (Victim.Exists()) { Suspect.Dismiss(); }            
        }
    }
}

