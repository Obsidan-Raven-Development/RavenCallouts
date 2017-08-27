using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;

#region Change Log
/// <summary>
/// V 0.1.3 Callout created
/// </summary>
#endregion

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Accident, Vehicle Vs Pedestrian", CalloutProbability.Low)]
    public class Accident : Callout
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
        private int r;
        private bool Debug = true;
        private int flee;
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
            speechCheck = 0;
            location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(450f));

            ShowCalloutAreaBlipBeforeAccepting(location, 150f);
            AddMinimumDistanceCheck(100f, location);

            suspectVehicle = new Vehicle(location);

            suspect = new Ped(suspectVehicle.GetOffsetPosition(new Vector3(0, 5.8f, 0)));

            victim = new Ped(suspect.GetOffsetPosition(new Vector3(0, 2.8f, 0)));
            victim.Health = 0;

            if (!suspect.Exists()) return false;
            if (!victim.Exists()) return false;

            suspect.BlockPermanentEvents = true;
            victim.BlockPermanentEvents = true;            

            CalloutMessage = "Pedestrian Vs. Automobile";
            CalloutPosition = location;

            //Functions.PlayScannerAudioUsingPosition(" ", location);

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

            Game.DisplaySubtitle("Dispatch:", 10000);
            Game.LogTrivialDebug("Raven.Attached");
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();

            if (calloutState == CalloutState.End)
            {
                End();
            }

            if (calloutState == CalloutState.Enroute && Game.LocalPlayer.Character.Position.DistanceTo(location) <= 15)
            { calloutState = CalloutState.OnScene; StartTraffic(); suspectBlip.DisableRoute(); Game.LogTrivialDebug("Raven arrived"); }
            else
            if (calloutState == CalloutState.End && !Functions.IsPursuitStillRunning(pursuit))
            { End(); }
        }

        public override void End()
        {
            if (calloutState == CalloutState.End)
            {
                Game.LogTrivialDebug("CalloutState = CalloutState.End Proceeding");
                if (suspect.Exists()) suspect.Dismiss();
                if (victim.Exists()) victim.Dismiss();
                if (suspectBlip.Exists()) suspectBlip.Delete();
                Game.LogTrivialDebug("Raven.Clean");
                base.End();
            }
            else
                if (calloutState != CalloutState.End)
            {
                Game.LogTrivialDebug("CalloutState does not = CalloutState.End, setting now");
                calloutState = CalloutState.End;
            }

        }

        public void StartTraffic()
        {
            GameFiber.StartNew(delegate                          //Start new gameFiber
            {
                #region Callout Settings
                ///<summary>
                ///Sets the internal callout ints for use for random generation, speech and more.
                /// </summary>
                if (speechCheck == 0)
                { speechCheck = 1; }                                //Set speech check to one
                else End();

                this.pursuit = Functions.CreatePursuit();

                if (Debug == true)
                {
                    r = 3;                                      // Debug settings
                }
                else
                    if (Debug == false)
                { r = new Random().Next(4, 100); }              // Release Settings

                calloutState = CalloutState.DecisionMade;
                #endregion

                #region Suspect Flees
                if (r >= 50 && r >= 11 && r > 3 || r == 1)
                {
                    ///<summary>
                    ///Have suspect get back into car and flee starting a pursuit, maybe find way to damage vehicle. add variable to determine if suspect flees on foot or in the vehicle
                    /// </summary>

                    if (Debug == true)
                    {
                        flee = 1;
                    }

                    else
                        if (Debug == false)
                    { flee = new Random().Next(1, 2); }

                    if (flee == 2)
                    {
                        Game.LogTrivialDebug("Raven.accident1.flee2.start");
                        Game.DisplaySubtitle("Suspect: Oh shit the pigs are here!", 5000);
                        suspect.Tasks.Flee(Game.LocalPlayer.Character, 10000, -1);
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        pursuitCreated = true;
                        Game.DisplayNotification("Officer: Suspect is fleeing, in pursuit!");
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                        GameFiber.Wait(6000);

                        Game.LogTrivialDebug("Raven.accident1.complete");
                        if (suspect.IsDead)
                        { calloutState = CalloutState.End; End(); }
                        if (suspect.IsCuffed)
                        { calloutState = CalloutState.End; End(); }
                    }
                    else if (flee == 1)
                    {
                        Game.LogTrivialDebug("Raven.accident1.flee1.start");
                        Game.DisplaySubtitle("Suspect: I'm not going back to jail!", 5000);
                        suspect.Tasks.EnterVehicle(suspectVehicle, 0);
                        suspect.Tasks.Flee(location, 10000, -1);
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        pursuitCreated = true;
                        Game.DisplayNotification("Officer: Suspect is fleeing, in pursuit!");
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                        GameFiber.Wait(6000);

                        Game.LogTrivialDebug("Raven.accident1.complete");
                        if (suspect.IsDead)
                        { calloutState = CalloutState.End; End(); }
                        if (suspect.IsCuffed)
                        { calloutState = CalloutState.End; End(); }
                    }

                }
                #endregion

                #region suspect fight  
                if (r <= 49 && r >= 11 || r == 2) //need testing to ensure properly working
                {
                    ///<summary>
                    ///Gives suspect a weapon and suspect attacks officer, after ten seconds the suspect flees on foot
                    /// </summary>
                    Game.LogTrivialDebug("Raven.accident2.fight.start");
                    //give suspect a knife(preffered) or gun(Temp)
                    suspect.Inventory.GiveNewWeapon("WEAPON_PISTOL", 120, true);
                    suspect.Tasks.FireWeaponAt(Game.LocalPlayer.Character, -1, FiringPattern.BurstFirePistol);

                    GameFiber.Wait(4000);

                    suspect.Tasks.Clear();
                    suspect.Tasks.Flee(Game.LocalPlayer.Character, 1000, -1);
                    pursuit = Functions.CreatePursuit();
                    Functions.AddPedToPursuit(pursuit, suspect);
                    Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                    pursuitCreated = true;
                    Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                    if (suspect.IsDead)
                    { calloutState = CalloutState.End; End(); }
                    if (suspect.IsCuffed)
                    { calloutState = CalloutState.End; End(); }
                    Game.LogTrivialDebug("Raven.accident2.complete");
                }
                #endregion

                #region Suspect Surrender
                if (r <= 10 && r > 3 || r == 3)
                {
                    Game.LogTrivialDebug("Raven.accident1.surrender1.start");
                    suspect.Tasks.PutHandsUp(-1, Game.LocalPlayer.Character);
                    Game.DisplaySubtitle("Suspect: I didn't do anything, I tried to stop but couldn't. I'm so sorry officer.");

                    speechCheck = 0;

                    //Add more investigative things to this callout

                    //foreach (var item in speechSuspect)           needs testing to see if works without the foreach loops,
                    //{

                    // Currently disabled until completed.

                    /*  remove once complete
                    while (true && speechCheck == 1)
                    {
                        Game.DisplayHelp("Press Y to talk.");
                        GameFiber.Yield();
                        if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                        {
                            break;
                        }
                    }
                    //Add dialouge
                    Game.DisplaySubtitle("", 5000);
                    Game.DisplaySubtitle(",", 5000);
                    speechCheck = 2;

                    while (true && speechCheck == 2)
                    {
                        Game.DisplayHelp("Press Y to talk.");
                        GameFiber.Yield();
                        if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                        { break; }
                    }

                    Game.DisplaySubtitle(".", 5000);
                    Game.DisplaySubtitle("", 5000);
                    speechCheck = 3;
                    while (true && speechCheck == 3)
                    {
                        Game.DisplayHelp("Press Y to talk.");
                        GameFiber.Yield();
                        if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                        { break; }
                    }

                    Game.DisplaySubtitle("", 5000);
                    Game.DisplaySubtitle("");
                    speechCheck = 4;

                    while (true && speechCheck == 4)
                    {
                        Game.DisplayHelp("Press Y to talk.");
                        GameFiber.Yield();
                        if (Game.IsKeyDown(System.Windows.Forms.Keys.Y) && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <= 5f)
                        { break; }
                    }

                    //}

                    if (suspect.IsDead)
                    { calloutState = CalloutState.End; End(); }
                    if (suspect.IsCuffed)
                    { calloutState = CalloutState.End; End(); }
                    Game.LogTrivialDebug("Raven.accident3.complete");

                    */ //remove once complete            
                }
                #endregion
            });
        }
    }
}
