using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    class CompProperties_PocketDimensionContainer : CompProperties
    {
        public IntVec3 dimensionSize = new IntVec3(13, 1, 13);

        public CompProperties_PocketDimensionContainer()
        {
            compClass = typeof(CompPocketDimensionContainer);
        }
    }
}
