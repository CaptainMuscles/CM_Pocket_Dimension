using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class GenStep_PocketDimensionStartSpot : GenStep
    {
        public override int SeedPart => 86753091;

        public override void Generate(Map map, GenStepParams parms)
        {
            DeepProfiler.Start("RebuildAllRegions");
            map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
            DeepProfiler.End();
            MapGenerator.PlayerStartSpot = map.Center + new IntVec3(0, 0, 1);
        }
    }
}
