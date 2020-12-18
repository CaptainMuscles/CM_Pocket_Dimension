using System.Collections.Generic;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    static class PocketDimensionUtility
    {
        public static Dictionary<string, MapParent_PocketDimension> MapParents = new Dictionary<string, MapParent_PocketDimension>();
        public static Dictionary<string, Building_PocketDimensionBox> Boxes = new Dictionary<string, Building_PocketDimensionBox>();
        public static Dictionary<string, Building_PocketDimensionExit> Exits = new Dictionary<string, Building_PocketDimensionExit>();

        public static MapParent_PocketDimension GetMapParent(string dimensionSeed)
        {
            MapParent_PocketDimension mapParent = null;
            if (dimensionSeed != null)
                MapParents.TryGetValue(dimensionSeed, out mapParent);
            return mapParent;
        }

        public static Building_PocketDimensionBox GetBox(string dimensionSeed)
        {
            Building_PocketDimensionBox box = null;
            if (dimensionSeed != null)
                Boxes.TryGetValue(dimensionSeed, out box);
            return box;
        }

        public static Building_PocketDimensionExit GetExit(string dimensionSeed)
        {
            Building_PocketDimensionExit exit = null;
            if (dimensionSeed != null)
                Exits.TryGetValue(dimensionSeed, out exit);
            return exit;
        }

        public static Building_PocketDimensionEntranceBase GetOtherSide(Building_PocketDimensionEntranceBase thisSide)
        {
            Building_PocketDimensionBox box = GetBox(thisSide.dimensionSeed);
            Building_PocketDimensionExit exit = GetExit(thisSide.dimensionSeed);

            if (box != null && box != thisSide)
                return box;
            else if (exit != null && exit != thisSide)
                return exit;

            return null;
        }

        public static List<Thing> GetWalls(string dimensionSeed)
        {
            MapParent_PocketDimension mapParent = GetMapParent(dimensionSeed);

            if (mapParent != null && mapParent.Map != null)
            {
                List<Thing> wallList = mapParent.Map.listerThings.ThingsOfDef(PocketDimensionDefOf.CM_PocketDimensionWall);

                if (wallList.Count == 0)
                    return null;

                return wallList;
            }

            return null;
        }

        public static void OnCriticalMapObjectDestroyed(Thing thingInMap, bool getLost = false)
        {
            Thing thingHolder = thingInMap.SpawnedParentOrMe;
            Map map = thingInMap.Map;
            if (map == null)
                map = thingHolder.Map;

            if (map != null)
            {
                MapParent_PocketDimension parent = map.Parent as MapParent_PocketDimension;

                if (parent != null)
                {
                    parent.Abandon(getLost);
                }
            }
        }

        public static void ClaimWalls(Map map, Faction faction)
        {
            List<Thing> wallList = map.listerThings.ThingsOfDef(PocketDimensionDefOf.CM_PocketDimensionWall);
            foreach (Thing wall in wallList)
            {
                Building wallBuilding = wall as Building;
                wallBuilding.SetFaction(faction);
            }
        }

        public static Map GetHighestContainingMap(Map map)
        {
            Map result = map;

            if (map != null)
            {
                MapParent_PocketDimension mapParent = map.info.parent as MapParent_PocketDimension;
                if (mapParent != null)
                {
                    Building_PocketDimensionBox box = PocketDimensionUtility.GetBox(mapParent.dimensionSeed);
                    if (box == null)
                    {
                        Logger.ErrorFormat(map, "Looking for a map containing a box that does not exist!");
                    }
                    else if (box.doingRecursiveThing)
                    {
                        Logger.WarningFormat(map, "Tried to find a containing map for a pocket dimension nested in a loop!");
                    }
                    else if (box.SpawnedOrAnyParentSpawned)
                    {
                        box.doingRecursiveThing = true;
                        try
                        {
                            result = GetHighestContainingMap(box.MapHeld);
                        }
                        finally
                        {
                            box.doingRecursiveThing = false;
                        }
                    }
                    else
                    {
                        //Logger.WarningFormat(map, "Could not find map containing pocket dimension box. Is it in a caravan?");
                    }
                }
                else
                {
                    //Logger.MessageFormat(map, "Not a pocket dimension.");
                }
            }

            return result;
        }
    }
}
