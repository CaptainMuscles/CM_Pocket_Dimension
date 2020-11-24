using System.Collections.Generic;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    class Building_PocketDimensionWall : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            PocketDimensionUtility.OnCriticalMapObjectDestroyed(this);

            base.Destroy(mode);
        }
    }
}
