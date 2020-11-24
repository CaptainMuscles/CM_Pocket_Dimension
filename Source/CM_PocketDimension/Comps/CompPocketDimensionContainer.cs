using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    class CompPocketDimensionContainer : ThingComp
    {
        public CompProperties_PocketDimensionContainer Props => (CompProperties_PocketDimensionContainer)props;
    }
}
