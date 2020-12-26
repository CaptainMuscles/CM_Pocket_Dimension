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
    public class WorkGiver_PressButton : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Designation> desList = pawn.Map.designationManager.allDesignations;
            for (int i = 0; i < desList.Count; i++)
            {
                if (desList[i].def == PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton)
                {
                    yield return desList[i].target.Thing;
                }
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.Map.designationManager.DesignationOn(t, PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton) == null)
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(PocketDimensionDefOf.CM_PocketDimension_Job_PressButton, t);
        }
    }
}
