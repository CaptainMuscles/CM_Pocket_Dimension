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

        private float networkEnergy = 0.0f;
        private float oldNetworkEnergy = 0.0f;
        private float duplicateEnergy = 0.0f;
        private float oldDuplicateEnergy = 0.0f;
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
                float newEnergy = otherSharer.GetEnergyStored(thisBattery.StoredEnergy);
                float maxEnergy = otherSharer.GetEnergyMax();

                float energyPercent = 1.0f;

                if (newEnergy != maxEnergy && maxEnergy > 0.0f)
                    energyPercent = newEnergy / maxEnergy;

                thisBattery.Props.storedEnergyMax = maxEnergy;
                thisBattery.SetStoredEnergyPct(energyPercent);
            }
            else if (thisBattery != null)
            {
                thisBattery.Props.storedEnergyMax = 0.0f;
                thisBattery.SetStoredEnergyPct(1.0f);
            }

            //if (otherSharer != null && otherSharer.parent.Map != null)
            //{
            //    this.PowerTransferAmount = otherSharer.GetExcessEnergy();
            //}
            //else
            //{
            //    this.PowerTransferAmount = 0.0f;
            //}


            ////this.UpdateDesiredPowerOutput();
            //// Using refuelable to add components breaks the normal UpdatedDesiredPowerOutput logic, reproduced and circumvented here
            //if ((breakdownableComp != null && breakdownableComp.BrokenDown) || /*(refuelableComp != null && !refuelableComp.HasFuel) ||*/ (flickableComp != null && !flickableComp.SwitchIsOn) || !base.PowerOn)
            //{
            //    base.PowerOutput = 0.0f;
            //}
            //else
            //{
            //    base.PowerOutput = DesiredPowerOutput;
            //}

            base.PowerOutput = 0.0f;
        }

        public float GetEnergyStored(float currentEnergyOtherSide)
        {
            if (this.PowerNet == null)
            {
                //Logger.MessageFormat(this, "No PowerNet found.");
                reloadedEnergyCalculated = true;

                oldNetworkEnergy = oldDuplicateEnergy = duplicateEnergy = networkEnergy = 0.0f;
                return networkEnergy;
            }

            List<CompPowerBattery> networkBatteries = this.PowerNet.batteryComps.Where(x => x != thisBattery).ToList();

            oldNetworkEnergy = networkEnergy;
            oldDuplicateEnergy = duplicateEnergy;

            networkEnergy = networkBatteries.Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();
            duplicateEnergy = currentEnergyOtherSide;

            float totalOldEnergy = (oldNetworkEnergy + oldDuplicateEnergy);
            float totalCurrentEnergy = (networkEnergy + duplicateEnergy);

            float duplicateEnergyChange = duplicateEnergy - oldDuplicateEnergy;
            float networkMaxEnergy = networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();

            if (reloadedEnergyCalculated && duplicateEnergyChange != 0.0f)
            {
                // If more energy lost in duplicate battery than available in duplicated network
                if (duplicateEnergyChange < 0.0f && networkEnergy > 0.0f && networkEnergy + duplicateEnergyChange < 0.0f)
                {
                    // Drain the whole thing
                    foreach (CompPowerBattery battery in networkBatteries)
                        battery.SetStoredEnergyPct(0.0f);

                    networkEnergy = 0.0f;

                    Logger.MessageFormat(this, "Network drained");
                }
                // If more energy gained in duplicate battery than the network can hold
                else if (duplicateEnergyChange > 0.0f && networkEnergy + duplicateEnergyChange > networkMaxEnergy)
                {
                    // Max the whole thing
                    foreach (CompPowerBattery battery in networkBatteries)
                        battery.SetStoredEnergyPct(1.0f);

                    networkEnergy = networkMaxEnergy;

                    Logger.MessageFormat(this, "Network filled");
                }
                else
                {
                    float percentChange = duplicateEnergyChange / oldDuplicateEnergy;
                    float percentOfExisting = 1.0f + percentChange;
                    foreach (CompPowerBattery battery in networkBatteries)
                        battery.SetStoredEnergyPct(percentOfExisting * battery.StoredEnergyPct);

                    float previousNetworkEnergy = networkEnergy;
                    networkEnergy = networkBatteries.Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();

                    Logger.MessageFormat(this, "Duplicate energy change: {0}, energy returned to network: {1}", duplicateEnergyChange, (networkEnergy - previousNetworkEnergy));
                }
            }


            reloadedEnergyCalculated = true;

            duplicateEnergy = networkEnergy;
            return networkEnergy;
        }

        public float GetEnergyMax()
        {
            List<CompPowerBattery> networkBatteries = this.PowerNet.batteryComps.Where(x => x != thisBattery).ToList();
            return networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();
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
