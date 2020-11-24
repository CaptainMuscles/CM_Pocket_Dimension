using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class JobDriver_CarryToPocketDimension : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Try to reserve prisoner.
            if (!pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed))
                return false;

            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();

            job.count = 1;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);

            Toil approachPrisoner = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return approachPrisoner;

            Toil collectPrisoner = Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return collectPrisoner;

            Toil escortPrisoner = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            //Toil escortPrisoner = Toils_Haul.CarryHauledThingToContainer(.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            yield return escortPrisoner;

            Toil enterPocket = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(enterPocket, TargetIndex.B, false, -0.5f);
            ToilFailConditions.FailOnCannotTouch<Toil>(enterPocket, TargetIndex.B, PathEndMode.InteractionCell);
            yield return enterPocket;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Thing thing;
                    pawn.carryTracker.TryDropCarriedThing(TargetB.Thing.Position, ThingPlaceMode.Direct, out thing);

                    if (TargetA.Thing is Pawn prisoner && TargetB.Thing is Building_PocketDimensionEntranceBase pocketDimensionEntrance)
                    {
                        Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(pocketDimensionEntrance);

                        if (otherSide != null && otherSide.Map != null)
                        {
                            IntVec3 position = otherSide.Position;
                            Map map = otherSide.Map;

                            if (position != null && map != null)
                            {
                                prisoner.DeSpawn(DestroyMode.Vanish);
                                GenSpawn.Spawn(prisoner, position, map, WipeMode.Vanish);
                            }
                        }
                    }
                }
            };
        }
    }
}
