using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class CompProperties_PocketDimensionBatteryShare : CompProperties
    {
        public float storedEnergyMax = 100.0f;

        public CompProperties_PocketDimensionBatteryShare()
        {
            compClass = typeof(CompPocketDimensionBatteryShare);
        }
    }
}
