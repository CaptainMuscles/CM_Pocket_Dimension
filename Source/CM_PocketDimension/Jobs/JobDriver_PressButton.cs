using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class JobDriver_PressButton : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => (base.Map.designationManager.DesignationOn(base.TargetThingA, PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton) == null) ? true : false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(15).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            Toil finalize = new Toil();
            finalize.initAction = delegate
            {
                Pawn actor = finalize.actor;
                ThingWithComps thingWithComps = (ThingWithComps)actor.CurJob.targetA.Thing;
                for (int i = 0; i < thingWithComps.AllComps.Count; i++)
                {
                    CompHasButton compHasButton = thingWithComps.AllComps[i] as CompHasButton;
                    if (compHasButton != null && compHasButton.WantsPress)
                    {
                        compHasButton.DoPress();
                    }
                }
                actor.records.Increment(PocketDimensionDefOf.CM_PocketDimension_Record_ButtonsPressed);
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finalize;
        }
    }
}
