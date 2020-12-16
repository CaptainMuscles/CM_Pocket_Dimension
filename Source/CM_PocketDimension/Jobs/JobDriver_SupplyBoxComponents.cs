using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class JobDriver_SupplyBoxComponents : JobDriver
    {
        protected Building_PocketDimensionBox Box => job.GetTarget(TargetIndex.A).Thing as Building_PocketDimensionBox;

        protected CompSuppliable SuppliableComp => Box.TryGetComp<CompSuppliable>();

        protected Thing Components => job.GetTarget(TargetIndex.B).Thing;

        private const int SupplyingDuration = 240;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(Box, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Components, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            AddEndCondition(() => Box.NeedsComponents ? JobCondition.Ongoing : JobCondition.Succeeded);

            yield return Toils_General.DoAtomic(delegate
            {
                job.count = Box.ComponentsNeeded;
            });
            Toil reserveComponents = Toils_Reserve.Reserve(TargetIndex.B);
            yield return reserveComponents;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveComponents, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(SupplyingDuration).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);

            Toil finalizeSupplying = new Toil();
            finalizeSupplying.initAction = delegate
            {
                Job curJob = finalizeSupplying.actor.CurJob;

                if (finalizeSupplying.actor.CurJob.placedThings.NullOrEmpty())
                {
                    Box.AddComponents(new List<Thing> { Components });
                }
                else
                {
                    List<Thing> placedComponents = finalizeSupplying.actor.CurJob.placedThings.Select((ThingCountClass p) => p.thing).ToList();
                    Box.AddComponents(placedComponents);
                }
            };
            finalizeSupplying.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalizeSupplying;
        }
    }
}
