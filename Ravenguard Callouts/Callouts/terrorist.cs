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

namespace RavenCallouts.Callouts
{
    [CalloutInfo("Terrorist Activity", CalloutProbability.VeryLow)]
   public class terrorist : Callout
    {
        #region Declaration
        public CalloutState calloutState;
        private Ped suspect;
        private Ped suspect1;
        private Ped suspect2;
        private Ped suspect3;
        private Ped suspect4;
        private Ped suspect5;
        Vector3 location;
        Blip suspectBlip;
        Blip suspectBlip1;
        Blip suspectBlip2;
        Blip suspectBlip3;
        Blip suspectBlip4;
        Blip suspectBlip5;
        private bool pursuitcreated = false;
        private LHandle pursuit;
        private int speechCheck { get; set; }
        private bool Debug = true;
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

    }
}
