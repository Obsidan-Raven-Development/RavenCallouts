//Version 0.1.3
//Files are available only as a reference to new coders
//Any Unauthorized reproduction of the code held within the files of the project is punishable to the fullest extent allowable by law.
//Do not upload to any site you are unauthorized to. If you are a developer you are authorized only to upload to the team repository and no where else. 
//
//RavenEcho Gaming Studio, Obsidian Raven Development 2017

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System.Reflection;
using System.Windows.Forms;

[assembly: Rage.Attributes.Plugin("RavenCallouts", Description = "A collection of callouts that are meant to be things that an officer sees on his daily routine.", Author = "LordRaven")]

namespace RavenCallouts
{
    /// <summary>
    /// Changelog:
    #region V 0.1.3
    /// - Rewrote all callouts to be more efficient and have cleaner code.
    /// - Work on the Prowler callout to expand it has begun. Early tests are in progress.
    /// - Completed rewrite of Domestic Callout and fixed dialouge for suspect and victim. More to come soon after ALPHA release
    /// - Changed Callout name to RavenCallouts
    #endregion

    #region  V 0.1.2
    /// - Removed prowler callout from plugin due to issues, will be added in when more testing can occur.
    /// - Added the Parking Violation callout, someone left their car in the middle of the road, check out the car and get traffic moving again,
    ///     and find the owner and give them a ticket
    /// - Beginning work on Computer+ integration for next release. All current callouts will be added. Need to update the Callout template to reflect
    ///     the new changes that will come with each callout. Does not work for me, I have been unable to load any callouts with Computer+, will
    ///     remain in the code to see if it works for anyone else.
    /// - Changed the Domestic Callout to be low chance instead of medium. All callouts will now be either Low or Very Low for public release.
    /// - Changed Dead body in vehicle to Suspicious Vehicle.
    /// - Changed name to Raven Callouts, Development studio is now RavenEcho Gaming Studios, and Obsidian Raven Development.
    /// - Most Callouts have been rewritten. This should prevent errors and help with editing them later on. 
    #endregion
    /// </summary>
    public class Main : Plugin
    {
        public override void Initialize()
        {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
            Game.FrameRender += Process;
        }

        public override void Finally()
        {
            Game.LogTrivial("RavenCallouts has been cleaned up.");
        }

        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }

        private static void OnOnDutyStateChangedHandler(bool OnDuty)
        {
            if (OnDuty)
            {
                Game.DisplayNotification("~r~Raven Callouts~w~ ~y~V 0.1.4ALPHA~w~ by                    ~p~LordRaven~w~                              has been ~r~loaded~w~.");

                RegisterCallouts();
            }
        }

        private static void RegisterCallouts()
        {
            Functions.RegisterCallout(typeof(Callouts.domestic));
            Functions.RegisterCallout(typeof(Callouts.Accident));
            //Functions.RegisterCallout(typeof(Callouts.Prowler));
            //Functions.RegisterCallout(typeof(Callouts.terrorist));
            Functions.RegisterCallout(typeof(Callouts.DeadBody));
            //Functions.RegisterCallout(typeof(Callouts.parkingViolation));

        }

        private static void Process(object sender, GraphicsEventArgs e)
        {
            if (Game.IsKeyDown(Keys.G))
            {
                Functions.StartCallout("RavenCallouts");
            }

        }
    }

}

