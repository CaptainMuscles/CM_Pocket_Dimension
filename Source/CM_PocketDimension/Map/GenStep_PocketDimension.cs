using System.Collections.Generic;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    class GenStep_PocketDimension : GenStep
    {
        public override void Generate(Map map, GenStepParams parms)
        {
            MapParent_PocketDimension mapParent = map.Parent as MapParent_PocketDimension;
            List<IntVec3> list = new List<IntVec3>();
            TerrainGrid terrainGrid = map.terrainGrid;
            RoofGrid roofGrid = map.roofGrid;

            string terrainDefName = "CM_PocketDimensionFloor";
            ThingDef boxStuffDef = null;

            // Build terrain defname
            Building_PocketDimensionBox box = PocketDimensionUtility.GetBox(mapParent.dimensionSeed);
            if (box != null && box.Stuff != null)
            {
                boxStuffDef = box.Stuff;
                if (boxStuffDef.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic))
                    terrainDefName += "Metal";
                else if (boxStuffDef.stuffProps.categories.Contains(StuffCategoryDefOf.Stony))
                    terrainDefName += "Stone";
                else if (boxStuffDef.stuffProps.categories.Contains(StuffCategoryDefOf.Woody))
                    terrainDefName += "Wood";

                terrainDefName += "_" + boxStuffDef.defName;
            }

            // If that terrain was not found, use default metal terrain
            TerrainDef terrainDef = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainDefName);
            if (terrainDef == null)
                terrainDef = PocketDimensionDefOf.CM_PocketDimensionFloorMetal;

            foreach (IntVec3 current in map.AllCells)
            {
                terrainGrid.SetTerrain(current, terrainDef);

                if (current.OnEdge(map))
                {
                    Thing wall = ThingMaker.MakeThing(PocketDimensionDefOf.CM_PocketDimensionWall, boxStuffDef);
                    wall.SetFaction(Faction.OfPlayer);
                    GenSpawn.Spawn(wall, current, map);
                }

                roofGrid.SetRoof(current, PocketDimensionDefOf.CM_PocketDimensionRoof);
            }
            MapGenFloatGrid elevation = MapGenerator.Elevation;
            foreach (IntVec3 allCell in map.AllCells)
            {
                elevation[allCell] = 0.0f;
            }
            MapGenFloatGrid fertility = MapGenerator.Fertility;
            foreach (IntVec3 allCell2 in map.AllCells)
            {
                fertility[allCell2] = 0.0f;
            }
        }

        public override int SeedPart
        {
            get
            {
                return 8675309;
            }
        }
    }
}
