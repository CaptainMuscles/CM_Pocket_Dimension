using RimWorld;
using System.Collections.Generic;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public class CompSuppliable : ThingComp
    {
        private int supplyCount = 0;

        public int SupplyCount => supplyCount;

        public CompProperties_Suppliable Props => (CompProperties_Suppliable)props;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<int>(ref this.supplyCount, "supplyCount", 0);
        }

        public bool AddComponents(int count)
        {
            supplyCount += count;
            return true;
        }

        public bool ConsumeComponents(int count)
        {
            if (count >= 0 && supplyCount >= count)
            {
                supplyCount -= count;
                return true;
            }

            return false;
        }
    }
}
