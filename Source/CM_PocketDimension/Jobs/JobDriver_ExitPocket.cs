using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class JobDriver_ExitPocket : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil exitPocket = Toils_General.Wait(60, 0);
            ToilEffects.WithProgressBarToilDelay(exitPocket, TargetIndex.A, false, -0.5f);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(exitPocket, TargetIndex.A);
            ToilFailConditions.FailOnCannotTouch<Toil>(exitPocket, TargetIndex.A, PathEndMode.InteractionCell);
            yield return exitPocket;

            yield return new Toil
            {
                initAction = delegate ()
                {
                    Pawn pawn = GetActor();
                    if (TargetA.Thing is Building_PocketDimensionExit pocketDimensionExit)
                    {
                        //PocketDimensionBox box = pocketDimensionExit.GetBox();

                        //if (box != null)
                        //{
                        //    pawn.DeSpawn(DestroyMode.Vanish);
                        //    GenSpawn.Spawn(pawn, box.Position, box.Map, WipeMode.Vanish);
                        //}
                    }
                }
            };
        }
    }
}
