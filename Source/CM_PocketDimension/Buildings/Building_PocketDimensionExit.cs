using System.Collections.Generic;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class Building_PocketDimensionExit : Building_PocketDimensionEntranceBase
    {
        public override void ExposeData()
        {
            base.ExposeData();

            if (!string.IsNullOrEmpty(dimensionSeed))
                PocketDimensionUtility.Exits[this.dimensionSeed] = this;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Logger.MessageFormat(this, "Spawning");
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            PocketDimensionUtility.OnCriticalMapObjectDestroyed(this, GetLost);

            base.Destroy(mode);
        }

        public override void Tick()
        {
            base.Tick();

            if (this.GetLost)
                return;

            if (this.IsHashIntervalTick(250))
            {
                Building_PocketDimensionBox box = PocketDimensionUtility.GetBox(this.dimensionSeed);
                if (box == null || !box.ExistsInWorld())
                {
                    this.GetLost = true;
                }
            }

            //Building_PocketDimensionEntranceBase box = this.otherSide;

            //if (box != null && this.IsHashIntervalTick(500))
            //{
            //    string thingString = box.GetType().ToString();
            //    if (box.ParentHolder != null)
            //    {
            //        for (IThingHolder parentHolder = box.ParentHolder; parentHolder != null; parentHolder = parentHolder.ParentHolder)
            //        {
            //            thingString = thingString + " - " + parentHolder.GetType().ToString();
            //        }
            //    }

            //    Logger.MessageFormat(this, "Box: {0}", thingString);
            //}
        }
    }
}
