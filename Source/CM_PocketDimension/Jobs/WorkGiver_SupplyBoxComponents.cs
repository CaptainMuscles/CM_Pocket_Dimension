using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using System;

namespace CM_PocketDimension
{
    public class WorkGiver_SupplyBoxComponents : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(PocketDimensionDefOf.CM_PocketDimensionBox);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Thing> list = pawn.Map.listerThings.ThingsOfDef(PocketDimensionDefOf.CM_PocketDimensionBox);
            for (int i = 0; i < list.Count; i++)
            {
                if (((Building_PocketDimensionBox)list[i]).NeedsComponents)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_PocketDimensionBox pocketDimensionBox = t as Building_PocketDimensionBox;
            if (pocketDimensionBox == null || !pocketDimensionBox.NeedsComponents)
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (FindBestComponents(pawn, pocketDimensionBox) == null)
            {
                JobFailReason.Is("CM_PocketDimension_NoComponentsToSupply".Translate(pocketDimensionBox.TryGetComp<CompSuppliable>().Props.componentLabel));
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_PocketDimensionBox pocketDimensionBox = t as Building_PocketDimensionBox;
            Thing components = FindBestComponents(pawn, pocketDimensionBox);

            return JobMaker.MakeJob(PocketDimensionDefOf.CM_PocketDimension_Job_SupplyBoxComponents, t, components);
        }

        private Thing FindBestComponents(Pawn pawn, Building_PocketDimensionBox pocketDimensionBox)
        {
            CompSuppliable compSuppliable = pocketDimensionBox.GetComp<CompSuppliable>();

            if (compSuppliable != null)
            {
                ThingRequest componentRequest = ThingRequest.ForDef(compSuppliable.Props.componentDef);

                Predicate<Thing> validator = delegate (Thing x) { return (!x.IsForbidden(pawn) && pawn.CanReserve(x)); };
                return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, componentRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
            }

            return null;
        }
    }
}
