using System.Collections.Generic;

using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public static class TradePatches
    {
        [HarmonyPatch(typeof(TradeShip))]
        [HarmonyPatch("GiveSoldThingToPlayer", MethodType.Normal)]
        public static class PocketDimension_TradeShip_GiveSoldThingToPlayer
        {
            [HarmonyPrefix]
            private static bool Prefix(TradeShip __instance, Thing toGive, int countToGive, Pawn playerNegotiator, List<Pawn> ___soldPrisoners)
            {
                if (__instance.Map != null)
                {
                    MapParent_PocketDimension mapParent = __instance.Map.info.parent as MapParent_PocketDimension;
                    if (mapParent != null)
                    {
                        Map containingMap = PocketDimensionUtility.GetHighestContainingMap(__instance.Map);

                        // If there was no containing map found (should mean box is in a caravan; could mean the box was nested inside itself, it which case, oh well, give them their goods anyway :P)
                        if (containingMap == __instance.Map)
                        {
                            Building_PocketDimensionExit exit = PocketDimensionUtility.GetExit(mapParent.dimensionSeed);
                            if (exit != null && exit.SpawnedOrAnyParentSpawned)
                            {
                                Thing thing = toGive.SplitOff(countToGive);
                                thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, __instance);
                                Pawn pawn = thing as Pawn;
                                if (pawn != null)
                                {
                                    ___soldPrisoners.Remove(pawn);
                                }

                                IntVec3 positionHeld = exit.PositionHeld;
                                Map mapHeld = exit.MapHeld;
                                GenPlace.TryPlaceThing(thing, positionHeld, mapHeld, ThingPlaceMode.Near);
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(DropCellFinder))]
        [HarmonyPatch("TradeDropSpot", MethodType.Normal)]
        public static class PocketDimensionTradeDropSpot
        {
            [HarmonyPrefix]
            private static void Prefix(ref Map map)
            {
                if (map != null)
                {
                    Logger.MessageFormat(map, "Checking for alternate map target.");
                    Map containingMap = PocketDimensionUtility.GetHighestContainingMap(map);
                    if (containingMap != null)
                    {
                        map = containingMap;
                        Logger.MessageFormat(map, "Redirecting trade drop spot check to: {0}", containingMap.GetUniqueLoadID());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DropPodUtility))]
        [HarmonyPatch("MakeDropPodAt", MethodType.Normal)]
        public static class PocketDimensionMakeDropPodAt
        {
            [HarmonyPrefix]
            private static void Prefix(ref Map map)
            {
                if (map != null)
                {
                    //Logger.MessageFormat(map, "Checking for alternate map target.");
                    Map containingMap = PocketDimensionUtility.GetHighestContainingMap(map);
                    if (containingMap != null)
                    {
                        map = containingMap;
                        Logger.MessageFormat(map, "Redirecting drop pod to: {0}", containingMap.GetUniqueLoadID());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DropPodUtility))]
        [HarmonyPatch("DropThingsNear", MethodType.Normal)]
        public static class PocketDimensionDropThingsNear
        {
            [HarmonyPrefix]
            private static void Prefix(ref Map map)
            {
                if (map != null)
                {
                    //Logger.MessageFormat(map, "Checking for alternate map target.");
                    Map containingMap = PocketDimensionUtility.GetHighestContainingMap(map);
                    if (containingMap != null)
                    {
                        map = containingMap;
                        Logger.MessageFormat(map, "Redirecting drop pod to: {0}", containingMap.GetUniqueLoadID());
                    }
                }
            }
        }
    }
}
