using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public class CompPowerShare : CompPowerPlant
    {
        public float PowerTransferAmount { get; set; }

        private CompPowerBattery thisBattery;
        private ThingWithComps otherThing;
        private CompPowerShare otherSharer;

        private float totalEnergy = 0.0f;
        private float oldTotalEnergy = 0.0f;
        private float myEnergy = 0.0f;
        private float oldMyEnergy = 0.0f;
        private bool reloadedEnergyCalculated = false;

        protected override float DesiredPowerOutput
        {
            get
            {
                return this.PowerTransferAmount;
            }
        }

        public void SetOtherThing(ThingWithComps other)
        {
            otherThing = other;
            otherSharer = null;

            if (otherThing != null)
            {
                otherSharer = otherThing.GetComp<CompPowerShare>();

                if (otherSharer == null)
                    otherThing = null;
                else
                    Logger.MessageFormat(this, "Setting otherSharer.");
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            thisBattery = this.parent.GetComp<CompPowerBattery>();
        }

        public override void CompTick()
        {
            base.CompTick();

            this.CompTickWhatever();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            this.CompTickWhatever();
        }

        private void CompTickWhatever()
        {
            if (thisBattery == null)
                Logger.MessageFormat(this, "No battery found.");

            if (otherSharer == null)
            {
                Building_PocketDimensionEntranceBase parentEntrance = this.parent as Building_PocketDimensionEntranceBase;

                if (parentEntrance != null)
                {
                    Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(parentEntrance);

                    if (otherSide != null)
                        SetOtherThing(otherSide);
                }
            }

            if (thisBattery != null && otherSharer != null && otherSharer.parent.Map != null)
            {
                thisBattery.Props.storedEnergyMax = otherSharer.GetEnergyStored(thisBattery.StoredEnergy);
                thisBattery.SetStoredEnergyPct(1.0f);
            }
            else if (thisBattery != null)
            {
                thisBattery.Props.storedEnergyMax = 0.0f;
                thisBattery.SetStoredEnergyPct(1.0f);
            }

            if (otherSharer != null && otherSharer.parent.Map != null)
            {
                this.PowerTransferAmount = otherSharer.GetExcessEnergy();
            }
            else
            {
                this.PowerTransferAmount = 0.0f;
            }


            //this.UpdateDesiredPowerOutput();
            // Using refuelable to add components breaks the normal UpdatedDesiredPowerOutput logic, reproduced and circumvented here
            if ((breakdownableComp != null && breakdownableComp.BrokenDown) || /*(refuelableComp != null && !refuelableComp.HasFuel) ||*/ (flickableComp != null && !flickableComp.SwitchIsOn) || !base.PowerOn)
            {
                base.PowerOutput = 0.0f;
            }
            else
            {
                base.PowerOutput = DesiredPowerOutput;
            }
        }

        public float GetEnergyStored(float currentEnergyOtherSide)
        {
            if (this.PowerNet == null)
            {
                //Logger.MessageFormat(this, "No PowerNet found.");
                reloadedEnergyCalculated = true;

                oldTotalEnergy = oldMyEnergy = myEnergy = totalEnergy = 0.0f;
                return totalEnergy;
            }

            oldTotalEnergy = totalEnergy;
            oldMyEnergy = myEnergy;

            totalEnergy = this.PowerNet.batteryComps.Where(x => x != thisBattery).Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();
            myEnergy = currentEnergyOtherSide;

            float totalOldEnergy = (oldTotalEnergy + oldMyEnergy);
            float totalCurrentEnergy = (totalEnergy + myEnergy);

            if (totalEnergy > 0.0f && reloadedEnergyCalculated)
            {
                // Double energy lost to counteract energy duplication
                float energyLost = (totalOldEnergy - totalCurrentEnergy) * 2.0f;
                if (energyLost > totalEnergy)
                {
                    //Logger.MessageFormat(this, "Removing all energy from batteries");
                    foreach (CompPowerBattery battery in this.PowerNet.batteryComps)
                    {
                        if (battery != thisBattery)
                            battery.SetStoredEnergyPct(0.0f);
                    }

                    totalEnergy = 0.0f;
                }
                else if (energyLost > 0.0f && totalEnergy > 0.0f)
                {
                    float percentLost = energyLost / totalOldEnergy;
                    //Logger.MessageFormat(this, "Removing {0} ({1}%) energy from batteries", energyLost, percentLost);
                    float percentRemaining = 1.0f - percentLost;
                    foreach (CompPowerBattery battery in this.PowerNet.batteryComps)
                    {
                        if (battery != thisBattery)
                            battery.SetStoredEnergyPct(percentRemaining * battery.StoredEnergyPct);
                    }

                    totalEnergy = this.PowerNet.batteryComps.Where(x => x != thisBattery).Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();
                }
                else
                {
                    //Logger.MessageFormat(this, "Batteries gained {0} energy", -energyLost);
                }
            }

            reloadedEnergyCalculated = true;

            myEnergy = totalEnergy;
            return totalEnergy;
        }

        public float GetExcessEnergy()
        {
            
            if (this.PowerNet == null)
            {
                //Logger.MessageFormat(this, "No PowerNet found.");
                return 0.0f;
            }

            foreach(CompPowerBattery battery in this.PowerNet.batteryComps)
            {
                //Logger.MessageFormat(this, "Battery: {0}, Energy%: {1}, Energy: {2} Max: {3}", battery.parent.GetType().ToString(), battery.StoredEnergyPct, battery.StoredEnergy, battery.Props.storedEnergyMax);
                if (battery != thisBattery && Math.Abs(battery.Props.storedEnergyMax - battery.StoredEnergy) > 0.01f)
                    return 0.0f;
            }

            return this.PowerNet.powerComps.Where(x => x != this && x.PowerOn).Select(x => x.PowerOutput).DefaultIfEmpty(0.0f).Sum();
        }
    }
}
