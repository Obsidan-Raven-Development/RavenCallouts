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
    [CalloutInfo("Reported Prowler", CalloutProbability.Low)]
    public class Prowler : Callout 
    {
        /// <summary>
        /// Declarations are here. Check these for ones we no longer use after code rework.
        /// </summary>
        #region Declaration
        public CalloutState calloutState;
        private Ped suspect;
        private Ped caller;
        private Vector3 location;
        private Blip suspectBlip;
        private Blip callerBlip;
        private Blip SearchBlip;
        private bool PursuitCreated = false;
        private LHandle Pursuit;
        private Caller callerOption;
        private int RavenRandom;
        private int Callerkill;
        private bool callerKill = false;
        #endregion

        /// <summary>
        /// Placed all my Enums here for better storage in the code.
        /// </summary>
        #region Enums  
        public enum CalloutState
        {
            Enroute,
            WithCaller,
            Searching,
            End
        }
        public enum Caller
        {
            Home,
            Walking,
            Friend,
            Husband
        }
        private enum callerKillSpeech
        {
            inside,
            outside,
            assault,
            drunk
        }
        #endregion

        public override bool OnBeforeCalloutDisplayed()
        {
            ///<summary
            /// Creating the spawn point and spawing in the peds, if this fails than the plugin wont work. Also generates the Blip for the radar.
            ///</Summary>
                        
                location = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(650f));
                AddMinimumDistanceCheck(60f, location);

                caller = new Ped(location);
                if (caller.IsMale)
                { caller.Delete(); return OnBeforeCalloutDisplayed(); }

            GameFiber.StartNew(delegate
            {
                if (caller.IsFemale)
                {
                    caller.IsPersistent = true;
                    caller.Tasks.Wander();
                    GameFiber.Sleep(3000);
                }
            });
            
            caller.Tasks.Clear();
            suspect = new Ped(caller.GetOffsetPosition(new Vector3(0, 1.5f, 0)));
            suspect.IsPersistent = true;

            if (!caller.Exists()) return false;
            if (!suspect.Exists()) return false;

            CalloutMessage = "Reported Prowler";
            CalloutPosition = location;

            suspect.BlockPermanentEvents = true;
            caller.BlockPermanentEvents = true;

            ShowCalloutAreaBlipBeforeAccepting(location, 150f);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            /// <summary
            /// Once accepted creates the blips, the routes, and gives the ped a weapon. 
            /// </summary

            caller.Tasks.StandStill(-1);

            callerBlip = caller.AttachBlip();
            callerBlip.EnableRoute(System.Drawing.Color.MintCream);
            callerBlip.Color = System.Drawing.Color.MintCream;


            calloutState = CalloutState.Enroute;
            suspect.Tasks.Wander();
            suspect.Inventory.GiveNewWeapon(1593441988, 120, false);
            Game.DisplayNotification("Attend to the caller to find out information."); //change to dispatch 
            Game.LogTrivial("RG:Callout created");
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            /// <summary
            /// This is for the main callout scripts that are in the StartProwler section, also tells the script when to execute the End command.
            /// 
            /// </summary>
            base.Process();
            Game.LogTrivial("Raven.Process");
            int Callerkill = new Random().Next(1, 100);

            if (Callerkill >= 65)
            {
                caller.Inventory.GiveNewWeapon("PUMP_SHOTGUN", 20, true);
                caller.Tasks.FightAgainst(suspect);
                if (suspect.IsDead)
                {
                    caller.Tasks.Clear();
                    caller.Tasks.StandStill(-1);
                }
                callerKill = true;
            }

            if (calloutState == CalloutState.Enroute && Game.LocalPlayer.Character.Position.DistanceTo(caller) <= 15f)

            { calloutState = CalloutState.WithCaller; StartCaller(); callerBlip.Delete(); callerBlip.DisableRoute(); }

            if (calloutState == CalloutState.End && !Functions.IsPursuitStillRunning(Pursuit))
            { this.End(); }
            
            
        }

        

        public override void End()
        {
            base.End();
            if (caller.Exists()) { caller.Dismiss(); }
            if (callerBlip.Exists()) { callerBlip.Delete(); }
            if (SearchBlip.Exists()) { SearchBlip.Delete(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            if (suspect.Exists()) { suspect.Dismiss(); }
            Game.LogTrivial("RG:Cleaned up Spawned Entities");
        }

        public void StartCaller()
        {           
            GameFiber.StartNew(delegate
            {
                if (calloutState == CalloutState.WithCaller && callerKill == false && Game.LocalPlayer.Character.Position.DistanceTo(caller) <= 5f)
                {
                    #region Caller Dialouge
                    Game.DisplaySubtitle("Caller:Officer! Officer! Over here!", 5000);

                    GameFiber.Sleep(5000);

                    Game.DisplayNotification("Officer: Dispatch, show me 10-75 with the RP");

                    Game.DisplaySubtitle("Caller:Officer, I saw someone creeping around in my yard peeking in my windows, I came out to investigate when they took off.", 10000);
                    GameFiber.Sleep(10000);

                    Game.DisplaySubtitle("You:Do you know what they look like?", 5000);
                    GameFiber.Sleep(10000);

                    Game.DisplaySubtitle("Caller:I Have him on CCTV, the stupid fucker.", 5000);
                    GameFiber.Sleep(10000);

                    Game.DisplaySubtitle("You:Did you see what direction they went?", 5000);
                    GameFiber.Sleep(5000);

                    Game.DisplaySubtitle("Caller:They went off in that direction.", 5000);
                    GameFiber.Sleep(5000);

                    Game.DisplayNotification("Officer: Dispatch, show me as looking for that 10-14");
                    GameFiber.Sleep(3000);

                    Game.DisplayNotification("Dispatch: Copy, showing you on that 10-14");
                    calloutState = CalloutState.Searching;
                    #endregion
                    Game.LogTrivial("Raven.Caller1");
                }

                if (calloutState == CalloutState.WithCaller && callerKill == true)
                {
                    ///<summary>
                    ///Caller shoots suspect, investigate and arrest the caller if needed. 
                    ///</summary>


                    if (Game.LocalPlayer.Character.Position.DistanceTo(caller.Position) <= 5f)
                    {
                        ///<summary>
                        ///Caller explains situation and what happened. up to offier to determine if the shooting is justified or not,
                        ///multiple checks will be needed here, this will get quite complex.
                        ///</summary>

                        int t = new Random().Next(1, 5);
                        #region Caller speech

                        #endregion
                    }

                    if (caller.IsCuffed)
                    {
                        caller.Inventory.Weapons.Remove("PUMP_SHOTGUN");
                    }

                }

                if (calloutState == CalloutState.Searching && Game.LocalPlayer.Character.Position.DistanceTo(suspect) >= 10f)
                {
                    ///<summary>
                    ///Create Search area and attach it to suspect.
                    ///</summary>                   
                    #region Search area
                    SearchBlip = suspect.AttachBlip();
                    SearchBlip.Alpha = 100;
                    SearchBlip.Color = System.Drawing.Color.Yellow;
                    SearchBlip.Scale = 30;
                    SearchBlip.EnableRoute(System.Drawing.Color.Yellow);
                    Game.DisplayNotification("Search the area for the suspect and investigate.");
                    if (callerBlip.Exists()) { callerBlip.Delete(); }
                    if (caller.Exists()) { caller.Dismiss(); }
                    Game.LogTrivial("Raven.Caller.Complete");
                    StartProwler();
                    #endregion
                    Game.LogTrivial("Raven.Caller2");
                }
            });
        }

        public void StartProwler()
        {
            GameFiber.StartNew(delegate
            {
                if (calloutState == CalloutState.Searching && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <=10f)
                {
                    ///<summary>
                    ///Delete the search blip and attach the new blip to the to the suspect, assign color, and 
                    ///begin script for suspect interaction.
                    ///</summary>
                    if (SearchBlip.Exists()) { SearchBlip.Delete(); SearchBlip.DisableRoute();  }
                    Game.LogTrivial("Raven.Clean.Caller");
                    suspectBlip = suspect.AttachBlip();                    
                    RavenRandom = new Random().Next(1, 75); //Release Option
                    //RavenRandom = 1; //Debug option
                    int Callerkill = new Random().Next(1, 2);     
                    
                   

                    if (RavenRandom <= 25 && Callerkill == 1 && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <=5f && !PursuitCreated)
                    {
                        #region Raven 1
                        ///<summary>
                        ///Suspect is walking, when the player arrives on scene they run, simple script to call a pursuit and add the player and suspect.
                        ///</summary>
                        Game.DisplaySubtitle("Suspect: Fuck you pig!");
                        Pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(Pursuit, suspect);
                        Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                        Game.DisplayNotification("Officer: Dispatch im in pursuit of that 10-14, send additional units.");
                        Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Pursuit, LSPD_First_Response.EBackupUnitType.LocalUnit);

                        if (suspect.IsDead)
                        { this.End(); }
                        else if (suspect.IsCuffed)
                        { this.End(); }
#endregion
                    }

                    if (RavenRandom <= 50 && RavenRandom > 25 && Callerkill == 1 && Game.LocalPlayer.Character.Position.DistanceTo(suspect) <=5f)
                    {
                        ///<summary>
                        ///create speech check for Suspect, suspect will claim innocence on this version
                        ///</summary>
                    #region Suspect innocent
                        int Speech = 1;
                        Game.LogTrivial("Raven.Innocent");


                        if (Speech == 1)
                        {
                            Game.DisplaySubtitle("Suspect: Oh, of course that bitch called the cops, can I explain officer?", 3000);
                            GameFiber.Sleep(3000);

                            Game.DisplaySubtitle("Officer: Hold it right there sir, we have reports of someone peeking in windows that matches your description.", 5000);
                            GameFiber.Sleep(6000);

                            Game.DisplaySubtitle("Suspect: My ex invited me over with the possibility of working things out,", 5000);
                            GameFiber.Sleep(5000);

                            Game.DisplaySubtitle("Suspect: When I got there she started yelling that I needed to get away, so I left and than you showed up", 5000);
                            GameFiber.Sleep(5000);

                            Game.DisplaySubtitle("Officer: Alright, let's get this sorted out, you got any ID on you?", 3000);
                            GameFiber.Sleep(2000);
                            Game.DisplayNotification("Get the Suspects ID and run it through the computer or Dispatch");
                            Speech++;
                        }
                        if (suspect.IsDead)
                        { this.End(); }
                        if (suspect.IsCuffed)
                        { this.End(); }
                    #endregion
                    }

                    if (RavenRandom <=75 && RavenRandom > 50 && Callerkill == 1)
                    {
                        ///<summary>
                        ///Suspect attempts to persuade officer he is innocent, may be drunk
                        ///</summary>                       
                    }
                }
            }

            );
        }
    }
}
