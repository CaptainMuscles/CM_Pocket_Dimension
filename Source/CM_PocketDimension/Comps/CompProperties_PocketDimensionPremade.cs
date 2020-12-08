using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    class CompProperties_PocketDimensionPremade : CompProperties
    {
        public int mapSize = 1;

        public CompProperties_PocketDimensionPremade()
        {
            compClass = typeof(CompPocketDimensionPremade);
        }
    }
}
