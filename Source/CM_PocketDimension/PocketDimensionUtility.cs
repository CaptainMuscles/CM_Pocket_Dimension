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
    }
}
