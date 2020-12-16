using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class CompProperties_Suppliable : CompProperties
    {
        public ThingDef componentDef = null;
        public int componentMultiplier = 1;
        public float powerMultiplier = 600.0f;
        public string componentLabel = "";

        public CompProperties_Suppliable()
        {
            compClass = typeof(CompSuppliable);
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
