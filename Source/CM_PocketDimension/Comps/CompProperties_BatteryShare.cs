using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class CompProperties_BatteryShare : CompProperties_Battery
    {
        public CompProperties_BatteryShare()
        {
            compClass = typeof(CompPowerBatteryShare);
        }
    }
}
