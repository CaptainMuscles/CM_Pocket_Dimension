using System.Collections.Generic;

using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public static class MapPatches
    {
        [HarmonyPatch(typeof(Map))]
        [HarmonyPatch("Biome", MethodType.Getter)]
        public static class PocketDimensionBiomeGetter
        {
            static bool testResult;

            [HarmonyPrefix]
            public static bool interceptBiome(Map __instance)
            {
                testResult = (__instance.info?.parent != null && __instance.info.parent is MapParent_PocketDimension);
                return !testResult;
            }

            [HarmonyPostfix]
            public static void getPocketDimensionBiome(Map __instance, ref BiomeDef __result)
            {
                if (testResult)
                    __result = PocketDimensionDefOf.CM_PocketDimensionBiome;
            }
        }

        [HarmonyPatch(typeof(ExitMapGrid))]
        [HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
        public static class PocketDimensionExitCells_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(ExitMapGrid __instance, Map ___map, ref bool __result)
            {
                if (___map != null && ___map.info.parent is MapParent_PocketDimension)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MapPawns))]
        [HarmonyPatch("AnyPawnBlockingMapRemoval", MethodType.Getter)]
        public static class PocketDimensionBlockingMapRemoval
        {
            [HarmonyPrefix]
            public static bool Prefix(MapPawns __instance, Map ___map, ref bool __result)
            {
                // Check all pocket dimensions accessible to this map and allow their contents to block this maps removal
                List<Thing> pocketDimensionBoxes = ___map.listerThings.ThingsOfDef(PocketDimensionDefOf.CM_PocketDimensionBox);

                foreach (Thing thing in pocketDimensionBoxes)
                {
                    Building_PocketDimensionBox box = thing as Building_PocketDimensionBox;
                    if (box != null)
                    {
                        Building_PocketDimensionEntranceBase boxExit = PocketDimensionUtility.GetOtherSide(box);
                        if (boxExit != null && boxExit.MapHeld != null && boxExit.MapHeld.mapPawns.AnyPawnBlockingMapRemoval)
                        {
                            __result = true;
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(MapTemperature))]
        [HarmonyPatch("OutdoorTemp", MethodType.Getter)]
        public static class FixOutdoorTemp
        {
            [HarmonyPrefix]
            public static bool GetOutdoorTemp(ref float __result, Map ___map)
            {
                MapParent_PocketDimension mapParent = ___map.info.parent as MapParent_PocketDimension;
                if (mapParent != null)
                {
                    Building_PocketDimensionEntranceBase box = PocketDimensionUtility.GetBox(mapParent.dimensionSeed);
                    if (box != null)
                    {
                        __result = 21.0f;

                        if (box.Spawned)
                        {
                            __result =  GenTemperature.GetTemperatureForCell(box.Position, box.Map);
                        }
                        if (box.ParentHolder != null)
                        {
                            for (IThingHolder parentHolder = box.ParentHolder; parentHolder != null; parentHolder = parentHolder.ParentHolder)
                            {
                                
                                if (ThingOwnerUtility.TryGetFixedTemperature(parentHolder, box, out __result))
                                {
                                    return false;
                                }
                            }
                        }
                        if (box.SpawnedOrAnyParentSpawned)
                        {
                            __result = GenTemperature.GetTemperatureForCell(box.PositionHeld, box.MapHeld);
                        }
                        if (box.Tile >= 0)
                        {
                            __result = GenTemperature.GetTemperatureFromSeasonAtTile(GenTicks.TicksAbs, box.Tile);
                        }

                        // Above logic derived from the following function call. Can't call it here due to an edge case which results in infinite loop
                        //__result = box.AmbientTemperature;
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(RoofCollapseCellsFinder))]
        [HarmonyPatch("Notify_RoofHolderDespawned", MethodType.Normal)]
        public static class PocketDimensionNoRoofCollapse
        {
            [HarmonyPrefix]
            public static bool Prefix(Map map)
            {
                if (map.info.parent is MapParent_PocketDimension)
                    return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(GenDraw))]
        [HarmonyPatch("DrawNoBuildEdgeLines", MethodType.Normal)]
        public static class PockeDimensionDrawBuildEdgeLinesNot
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (Find.CurrentMap.info.parent is MapParent_PocketDimension)
                    return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(GenDraw))]
        [HarmonyPatch("DrawNoZoneEdgeLines", MethodType.Normal)]
        public static class PockeDimensionDrawZoneEdgeLinesNot
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (Find.CurrentMap.info.parent is MapParent_PocketDimension)
                    return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(GenGrid))]
        [HarmonyPatch("InNoBuildEdgeArea", MethodType.Normal)]
        public static class PockeDimensionAllowBuildNearEdge
        {
            [HarmonyPrefix]
            public static bool Prefix(Map map, ref bool __result)
            {
                if (map.info.parent is MapParent_PocketDimension)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(GenGrid))]
        [HarmonyPatch("InNoZoneEdgeArea", MethodType.Normal)]
        public static class PockeDimensionAllowZoneNearEdge
        {
            [HarmonyPrefix]
            public static bool Prefix(Map map, ref bool __result)
            {
                if (map.info.parent is MapParent_PocketDimension)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }
    }
}
