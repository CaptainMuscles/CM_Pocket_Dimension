using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public static class StatPatches
    {
        [HarmonyPatch(typeof(StatExtension))]
        [HarmonyPatch("GetStatValue", MethodType.Normal)]
        public static class PocketDimensionBoxContentMarketValue
        {
            [HarmonyPostfix]
            public static void addContentValue(Thing thing, StatDef stat, ref float __result)
            {
                // We'll just get an error if not in the play state
                if (Current.ProgramState != ProgramState.Playing)
                    return;

                if (stat.defName != "MarketValue" && stat.defName != "MarketValueIgnoreHp")
                    return;

                // Don't check for a minified thing! Minified objects will end up calling this function again
                Building_PocketDimensionBox innerBox = thing as Building_PocketDimensionBox;

                if (innerBox != null)
                {
                    __result += innerBox.CalculateAdditionalMarketValue(__result);
                }
            }
        }
    }
}
