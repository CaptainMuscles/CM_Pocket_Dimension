using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class CompProperties_PocketDimensionCreator : CompProperties
    {
        public int preMadeMapSize = 0;
        public ThingDef exitDef = null;
        public ThingDef componentDef = null;
        public int componentMultiplier = 1;
        public float powerMultiplier = 600.0f;
        public string componentLabel = "";

        public CompProperties_PocketDimensionCreator()
        {
            compClass = typeof(CompPocketDimensionCreator);
        }

        public string ComponentLabel
        {
            get
            {
                if (componentLabel.NullOrEmpty())
                {
                    return "Fuel".TranslateSimple();
                }
                return componentLabel;
            }
        }
    }
}
