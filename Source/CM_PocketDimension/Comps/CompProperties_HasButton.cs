using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class CompProperties_HasButton : CompProperties
    {
        [NoTranslate]
        public string commandTexture = "UI/Commands/DesirePower";

        [NoTranslate]
        public string commandLabelKey = "CommandDesignateTogglePowerLabel";

        [NoTranslate]
        public string commandDescKey = "CommandDesignateTogglePowerDesc";

        public string extraPressSignal = "";

        public CompProperties_HasButton()
        {
            compClass = typeof(CompHasButton);
        }
    }
}
