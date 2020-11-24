using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    static class Order_CarryToPocketDimension
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        public static class Order_CarryToPocketDimension_FloatMenuOption
        {
            public static TargetingParameters TargetParametersPrisoner
            {
                get
                {
                    return new TargetingParameters()
                    {
                        canTargetPawns = true,
                        canTargetItems = false,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        validator = ((TargetInfo target) =>
                        {
                            if (!target.HasThing)
                                return false;

                            if (target.Thing is Pawn pawn)
                            {
                                return !pawn.InAggroMentalState && (pawn.IsPrisonerOfColony || (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.RaceProps.Animal));
                            }
                            else
                            {
                                return false;
                            }
                        })
                    };
                }
            }

            [HarmonyPostfix]
            public static void Order_CarryToPocketDimension_FloatMenuOptionPostfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                // If the pawn in question cannot take jobs, don't bother.
                if (pawn.jobs == null)
                    return;

                bool pocketDimensionEntrancesChecked = false;
                List<Building> pocketDimensionEntrances = new List<Building>();

                // Find valid prisoners.
                foreach (LocalTargetInfo targetPrisoner in GenUI.TargetsAt_NewTemp(clickPos, TargetParametersPrisoner))//GenUI.TargetsAt(clickPos, TargetParametersPrisoner))
                {
                    // Less unnecessary processing to get list of entrances after we've ensured valid target(s)
                    if (!pocketDimensionEntrancesChecked)
                    {
                        pocketDimensionEntrancesChecked = true;
                        pocketDimensionEntrances = pawn.Map.listerBuildings.allBuildingsColonist.Where(x => x is Building_PocketDimensionEntranceBase).ToList();
                    }

                    if (pocketDimensionEntrances.Count < 1)
                        return;

                    // Ensure target is reachable
                    if (!pawn.CanReach(targetPrisoner, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }

                    foreach (Building_PocketDimensionEntranceBase entrance in pocketDimensionEntrances)
                    {

                        // Add menu option to carry prisoner to pocket dimension entrance/exit
                        FloatMenuOption option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CM_CarryToPocketDimension".Translate(targetPrisoner.Thing.LabelCap, entrance), delegate ()
                        {
                            Job job = JobMaker.MakeJob(PocketDimensionDefOf.CM_CarryToPocket, targetPrisoner, entrance);
                            pawn.jobs.TryTakeOrderedJob(job);
                            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        }, MenuOptionPriority.High), pawn, targetPrisoner);
                        opts.Add(option);
                    }
                }
            }
        }
    }
}
