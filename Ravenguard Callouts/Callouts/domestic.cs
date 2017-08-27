///<summary> RavenCallouts.Domestic
///Domestic dispute called in by neighbors claiming between one and three shots fired after hearing neighbors arguing. 
///Using Random number Generation to determine which situation to run, some situations may have mutliple outcomes as well.
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
/// <summary>V 0.1.3
/// Rewrote entire plugin to be more efficient when running as well as added new situations and outcomes.
/// 
/// </summary>
#endregion

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Domestic Disturbance", CalloutProbability.Low)]
    public class domestic : Callout
    {
        #region Declaration
        public CalloutState calloutState;
        private Ped suspect;
        private Ped victim;
        Vector3 location;
        Blip suspectBlip;
        private bool pursuitCreated = false;
        private LHandle pursuit;
        private int speechCheck { get; set; }
        Vehicle suspectVehicle;
        private bool Debug = false;
        private int r;
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
            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(450f));
            suspectVehicle = new Vehicle(3286105550, location);

            ShowCalloutAreaBlipBeforeAccepting(location, 150f);
            AddMinimumDistanceCheck(100f, location);



            suspect = suspectVehicle.CreateRandomDriver();
            suspect.Tasks.LeaveVehicle(flags: LeaveVehicleFlags.LeaveDoorOpen);


            suspect.Tasks.Clear();
            victim = new Ped(suspect.GetOffsetPosition(new Vector3(0, 2.8f, 0)));

            if (!suspect.Exists()) return false;
            if (!victim.Exists()) return false;

            suspect.BlockPermanentEvents = true;
            victim.BlockPermanentEvents = true;

            suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 120, true);

            CalloutMessage = "Domestic Disturbance";
            CalloutPosition = location;

            Functions.PlayScannerAudioUsingPosition("CITIZENS_REPORT_01 CRIME_SHOTS_FIRED_01 IN POSITION UNITS_RESPOND_CODE_03_01", location);

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
            suspectBlip = suspect.AttachBlip();
            suspectBlip.Color = System.Drawing.Color.Red;
            suspectBlip.EnableRoute(System.Drawing.Color.Red);

            suspect.Tasks.AimWeaponAt(victim, -1);
            victim.Tasks.PutHandsUp(-1, suspect);

            Game.DisplaySubtitle("Dispatch: RP reports two shots fired just before calling, saw two people arguing outside just before shots fired, not further details at this time. Advise caution on scene", 10000);
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

            if (calloutState == CalloutState.Enroute && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 15)
            { calloutState = CalloutState.OnScene; StartDomestic(); suspectBlip.DisableRoute(); Game.LogTrivial("Raven arrived"); }

            if (calloutState == CalloutState.End && !Functions.IsPursuitStillRunning(pursuit))
            { this.End(); }
        }

        public override void End()
        {
            calloutState = CalloutState.End;
            if (suspect.Exists()) suspect.Dismiss();
            if (victim.Exists()) victim.Dismiss();
            if (suspectBlip.Exists()) suspectBlip.Delete();
            Game.LogTrivialDebug("Raven.Clean");
            base.End();
        }

        public void StartDomestic()
        {
            GameFiber.StartNew(delegate
            {
                speechCheck = 1;
                this.pursuit = Functions.CreatePursuit();

                // Find new Random Number Gen, better results are needed.                       
                if (Debug == true)
                {
                     r = 2;                                //Debug only
                }
                else
                
                     r = new Random().Next(4, 100);       //Release  
                


                calloutState = CalloutState.DecisionMade;


                #region Suspect Fight
                if (r <= 49 && r >= 11 || r == 1)
                {
                    Game.LogTrivialDebug("Raven1");
                    suspect.Tasks.FightAgainst(Game.LocalPlayer.Character, -1);
                    victim.Tasks.Flee(suspect, 100000, 5000);

                    GameFiber.Wait(4000);

                    suspect.Tasks.Clear();
                    suspect.Tasks.Flee(Game.LocalPlayer.Character, 1000, -1);
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, suspect);
                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                    pursuitCreated = true;
                    Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                    if (suspect.IsDead)
                    { calloutState = CalloutState.End; this.End(); }
                    if (suspect.IsCuffed)
                    { calloutState = CalloutState.End; this.End(); }
                }
                #endregion

                #region Suspect Kill Victim
                if (r >= 50 && r >= 11 && r > 3 || r == 2)
                {
                    Game.LogTrivialDebug("Raven2");
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, suspect);
                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                    pursuitCreated = true;

                    Game.DisplayNotification("Officer: Dispatch shots fired 10-99 emergency!");
                    Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                    if (speechCheck == 1)
                    {
                        Game.DisplaySubtitle("Suspect: I'm gonna fucking kill you!");
                        suspect.Tasks.FightAgainst(victim, 5000);
                        victim.Tasks.Flee(suspect, 10000, -1);
                        speechCheck = 2;
                    }
                    

                    GameFiber.Wait(6000);

                    suspect.Tasks.Clear();
                    suspect.Tasks.Flee(Game.LocalPlayer.Character, 1000, -1);

                    if (suspect.IsDead)
                    { calloutState = CalloutState.End; this.End(); }
                    if (suspect.IsCuffed)
                    { calloutState = CalloutState.End; this.End(); }

                }
                #endregion

                #region Suspect Surrender
                if (r <= 10 && r > 3 || r == 3)
                {
                    Game.LogTrivialDebug("Raven3");
                    suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    Game.DisplaySubtitle("Suspect: I didn't do anything, I give up.");

                    //Add more investigative things to this callout

                    //foreach (var item in speechSuspect)           needs testing to see if works with foreach loops
                    //{
                        while (true && speechCheck == 1)
                        {
                            Game.DisplayHelp("Press Y to talk.");
                            GameFiber.Yield();
                            if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <=5f)
                            {
                                break;
                            }
                        }

                        Game.DisplaySubtitle("Suspect: She had a knife and was threating to kill me, I had no choice", 5000);
                        Game.DisplaySubtitle("Suspect: I ran from the house to my car and she followed me,", 5000);
                        speechCheck = 2;

                        while (true && speechCheck == 2)
                        {
                            Game.DisplayHelp("Press Y to talk.");
                            GameFiber.Yield();
                            if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                            { break; }
                        }

                        Game.DisplaySubtitle("Suspect: I managed to get my pistol out of the glove box.", 5000);
                        Game.DisplaySubtitle("Suspect: Thats when I saw her charging me from the porch with the knife still in her hand", 5000);
                        speechCheck = 3;
                        while (true && speechCheck == 3)
                        {
                            Game.DisplayHelp("Press Y to talk.");
                            GameFiber.Yield();
                            if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                            { break; }
                        }

                        Game.DisplaySubtitle("Suspect: I had no choice, I needed to defend myself so I fired one shot, not sure if I hit her or not though.", 5000);
                        Game.DisplaySubtitle("Speak with the Victim");
                        speechCheck = 4;

                        while (true && speechCheck == 4)
                        {
                            Game.DisplayHelp("Press Y to talk.");
                            GameFiber.Yield();
                            if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                            { break; }
                        }
                        ///<summary>
                        ///Create more random outcomes for this here at least three.
                        /// </summary>
                        Game.DisplaySubtitle("Victim: I'm going to cut that cheaters heart out!");
                        victim.Inventory.GiveNewWeapon("WEAPON_KNIFE", 1, true);
                        victim.Tasks.FightAgainst(suspect, 10000);

                        GameFiber.Wait(10000);

                        victim.Tasks.Flee(Game.LocalPlayer.Character, 50000, -1);
                    //}

                    if (suspect.IsDead)
                    { calloutState = CalloutState.End; this.End(); }
                    if (suspect.IsCuffed)
                    { calloutState = CalloutState.End; this.End(); }
                }
                #endregion
            }
            );
        }       
    }

}


