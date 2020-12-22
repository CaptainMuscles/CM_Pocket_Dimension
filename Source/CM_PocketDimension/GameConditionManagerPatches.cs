using System.Collections.Generic;

using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public static class GameConditionManagerPatches
    {
        [HarmonyPatch(typeof(GameConditionManager))]
        [HarmonyPatch("ElectricityDisabled", MethodType.Getter)]
        public static class PocketDimensionBiomeGetter
        {
            [HarmonyPostfix]
            public static void Postfix(GameConditionManager __instance, ref bool __result)
            {
                if (__result && __instance.ownerMap != null)
                {
                    MapParent_PocketDimension mapParent = __instance.ownerMap.info.parent as MapParent_PocketDimension;
                    if (mapParent != null)
                    {
                        Map containingMap = PocketDimensionUtility.GetHighestContainingMap(__instance.ownerMap);
                        if (containingMap != __instance.ownerMap)
                        {
                            __result = containingMap.gameConditionManager.ElectricityDisabled;
                        }
                    }
                }
            }
        }
    }
}
