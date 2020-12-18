using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public abstract class HitPointEqualizee : IExposable
    {
        public int currentHitPoints = 1;
        public int lastHitPoints = 1;
        public int maxHitPoints = 1;

        public int hitPointsChange = 0;

        public float currentHitPointsPercent = 1.0f;
        public float hitPointsChangePercent = 0.0f;

        public float spareChange = 0.0f;

        public virtual void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.currentHitPoints, "currentHitPoints", 1);
            Scribe_Values.Look<int>(ref this.lastHitPoints, "lastHitPoints", 1);
            Scribe_Values.Look<int>(ref this.maxHitPoints, "maxHitPoints", 1);
            Scribe_Values.Look<float>(ref this.spareChange, "spareChange", 0.0f);
        }

        public abstract void UpdateValues();
        public abstract void Equalize(float totalHitPointChangePercent);

        public void PostUpdateValues()
        {
            lastHitPoints = currentHitPoints;
        }
    }

    public class HitPointEqualizeeThing : HitPointEqualizee
    {
        private Thing thing = null;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref thing, "thing");
        }

        public void Initialize(Thing thingToTrack)
        {
            thing = thingToTrack;
            lastHitPoints = thing.HitPoints;

            UpdateValues();
        }

        public override void UpdateValues()
        {
            currentHitPoints = thing.HitPoints;
            maxHitPoints = thing.MaxHitPoints;

            hitPointsChange = currentHitPoints - lastHitPoints;

            currentHitPointsPercent = currentHitPoints / (float)maxHitPoints;
            hitPointsChangePercent = hitPointsChange / (float)maxHitPoints;
        }

        public override void Equalize(float totalHitPointChangePercent)
        {
            int newCurrentHitPoints = currentHitPoints;

            float otherHitPointsChangePercent = (totalHitPointChangePercent - hitPointsChangePercent);

            float newHitPointsChange = (otherHitPointsChangePercent * maxHitPoints) + spareChange;
            newCurrentHitPoints += (int)newHitPointsChange;
            spareChange = newHitPointsChange - (int)newHitPointsChange;

            thing.HitPoints = Math.Min(newCurrentHitPoints, thing.MaxHitPoints);

            if (thing.HitPoints != currentHitPoints)
            {
                Building thingBuilding = thing as Building;
                if (thingBuilding != null && thing.Map != null)
                {
                    if (newHitPointsChange > 0.0f)
                        thing.Map.listerBuildingsRepairable.Notify_BuildingRepaired(thingBuilding);
                    else if (newHitPointsChange < 0.0f)
                        thing.Map.listerBuildingsRepairable.Notify_BuildingTookDamage(thingBuilding);
                }
                currentHitPoints = thing.HitPoints;
            }

            if (thing.HitPoints < 0)
                thing.Kill();
        }
    }

    public class HitPointEqualizeeThingList : HitPointEqualizee
    {
        private List<Thing> thingList = null;
        private bool uniformMaxHitPoints = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref thingList, "thingList", LookMode.Reference);
            Scribe_Values.Look<bool>(ref uniformMaxHitPoints, "uniformMaxHitPoints");
        }

        public void Initialize(List<Thing> thingListToTrack, bool uniformMaxHP = true)
        {
            thingList = thingListToTrack;
            uniformMaxHitPoints = uniformMaxHP;
            lastHitPoints = thingList.Sum(x => x.HitPoints);

            UpdateValues();
        }

        public override void UpdateValues()
        {
            currentHitPoints = thingList.Sum(x => x.HitPoints);
            if (uniformMaxHitPoints && thingList.Count > 0)
                maxHitPoints = thingList.First().MaxHitPoints * thingList.Count;
            else
                maxHitPoints = thingList.Sum(x => x.MaxHitPoints);

            hitPointsChange = currentHitPoints - lastHitPoints;

            currentHitPointsPercent = currentHitPoints / (float)maxHitPoints;
            hitPointsChangePercent = hitPointsChange / (float)maxHitPoints;
        }

        public override void Equalize(float totalHitPointChangePercent)
        {
            int newCurrentHitPoints = currentHitPoints;

            float otherHitPointsChangePercent = (totalHitPointChangePercent - hitPointsChangePercent);

            float newHitPointsChange = (otherHitPointsChangePercent * maxHitPoints) + spareChange;
            newCurrentHitPoints += (int)newHitPointsChange;
            spareChange = newHitPointsChange - (int)newHitPointsChange;

            float hitPointCorrectionPercent = newCurrentHitPoints / (float)currentHitPoints;

            foreach (Thing thing in thingList)
            {
                thing.HitPoints = Math.Min((int)(thing.HitPoints * hitPointCorrectionPercent), thing.MaxHitPoints);

                Building thingBuilding = thing as Building;
                if (thingBuilding != null && thing.Map != null)
                {
                    if (newHitPointsChange > 0.0f)
                        thing.Map.listerBuildingsRepairable.Notify_BuildingRepaired(thingBuilding);
                    else if (newHitPointsChange < 0.0f)
                        thing.Map.listerBuildingsRepairable.Notify_BuildingTookDamage(thingBuilding);
                }

                if (thing.HitPoints < 0)
                    thing.Kill();
            }

            currentHitPoints = Math.Min(newCurrentHitPoints, maxHitPoints);
        }
    }

    public class HitPointEqualizer : IExposable
    {
        private List<HitPointEqualizee> Equalizees = new List<HitPointEqualizee>();

        public virtual void ExposeData()
        {
            Scribe_Collections.Look(ref Equalizees, "Equalizees", LookMode.Deep);
        }

        public void AddEqualizee(Thing thingToEqualize)
        {
            HitPointEqualizeeThing equalizee = new HitPointEqualizeeThing();
            equalizee.Initialize(thingToEqualize);

            Equalizees.Add(equalizee);
        }

        public void AddEqualizee(List<Thing> thingListToEqualize)
        {
            HitPointEqualizeeThingList equalizee = new HitPointEqualizeeThingList();
            equalizee.Initialize(thingListToEqualize);

            Equalizees.Add(equalizee);
        }

        public void Update()
        {
            int totalHitPointChange = 0;
            float totalHitPointChangePercent = 0.0f;

            foreach (HitPointEqualizee equalizee in Equalizees)
            {
                equalizee.UpdateValues();
                totalHitPointChange += equalizee.hitPointsChange;
                totalHitPointChangePercent += equalizee.hitPointsChangePercent;
            }

            Logger.MessageFormat(this, "totalHitPointChangePercent: {0}", totalHitPointChangePercent);

            if (totalHitPointChange != 0)
            {
                foreach (HitPointEqualizee equalizee in Equalizees)
                {
                    equalizee.Equalize(totalHitPointChangePercent);
                }
            }

            foreach (HitPointEqualizee equalizee in Equalizees)
                equalizee.PostUpdateValues();
        }
    }
}
