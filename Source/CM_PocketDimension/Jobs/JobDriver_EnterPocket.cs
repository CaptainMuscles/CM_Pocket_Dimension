using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class JobDriver_EnterPocket : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil enterPocket = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(enterPocket, TargetIndex.A, false, -0.5f);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(enterPocket, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(enterPocket, TargetIndex.A, PathEndMode.InteractionCell);
            yield return enterPocket;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Pawn pawn = GetActor();
                    if (TargetA.Thing is Building_PocketDimensionEntranceBase pocketDimensionEntrance)
                    {
                        Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(pocketDimensionEntrance);

                        if (otherSide != null && otherSide.Map != null)
                        {
                            IntVec3 position = otherSide.Position;
                            Map map = otherSide.Map;

                            // If otherSide is uninstalled... 
                            if (otherSide.Map == null && otherSide.holdingOwner != null && otherSide.holdingOwner.Owner != null && (otherSide.holdingOwner.Owner as MinifiedThing) != null)
                            {
                                MinifiedThing miniThing = (otherSide.holdingOwner.Owner as MinifiedThing);
                                position = miniThing.Position;
                                map = miniThing.Map;
                            }

                            if (position != null && map != null)
                            {
                                pawn.ClearAllReservations();
                                pawn.DeSpawn(DestroyMode.Vanish);
                                GenSpawn.Spawn(pawn, position, map, WipeMode.Vanish);
                            }
                        }
                    }
                }
            };
        }
    }
}
