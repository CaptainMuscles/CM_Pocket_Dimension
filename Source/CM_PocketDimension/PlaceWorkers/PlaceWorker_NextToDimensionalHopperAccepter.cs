using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class PlaceWorker_NextToDimensionalHopperAccepter : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVec3 c = loc + GenAdj.CardinalDirections[i];
                if (!c.InBounds(map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(map);

                foreach (Thing thingNearby in thingList)
                {
                    // Check for an already-built pocket dimension building
                    if (thingNearby as Building_PocketDimensionEntranceBase != null)
                        return true;

                    // Might be a blueprint, this won't work with the premade boxes (unless we name them all here... and I'm not doing that because I'll never remember if I add more so :P)
                    ThingDef thingDef = GenConstruct.BuiltDefOf(thingNearby.def) as ThingDef;
                    if (thingDef != null && thingDef == PocketDimensionDefOf.CM_PocketDimensionBox || thingDef == PocketDimensionDefOf.CM_PocketDimensionExit)
                    {
                        return true;
                    }
                }
            }
            return "CM_MustPlaceNextToDimensionalHopperAccepter".Translate();
        }
    }
}
