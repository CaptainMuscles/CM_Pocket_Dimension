using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    class CompPocketDimensionPremade : ThingComp
    {
        public CompProperties_PocketDimensionPremade Props => (CompProperties_PocketDimensionPremade)props;
    }
}
