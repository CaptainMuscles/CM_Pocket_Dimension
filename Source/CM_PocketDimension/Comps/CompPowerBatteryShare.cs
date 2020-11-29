using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public class CompPowerBatteryShare : CompPowerBattery
    {
        private CompPowerBatteryShare linkedBattery;

        // Only modify these two variables inside GetEnergyStored, since it is called by our linked counterpart
        private float duplicateEnergy = 0.0f;
        private float oldDuplicateEnergy = 0.0f;

        private bool reloadedEnergyCalculated = false;

        // We can't just override "Props" here because CompPowerBattery will attempt an invalid cast in PostExposeData :P
        public CompProperties_BatteryShare ShareProps => (CompProperties_BatteryShare)props;

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

        public override void CompTickLong()
        {
            base.CompTickLong();

            this.CompTickWhatever();
        }

        private void CompTickWhatever()
        {
            if (linkedBattery == null)
            {
                Building_PocketDimensionEntranceBase parentEntrance = this.parent as Building_PocketDimensionEntranceBase;

                if (parentEntrance != null)
                {
                    Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(parentEntrance);

                    if (otherSide != null)
                        linkedBattery = otherSide.GetComp<CompPowerBatteryShare>();
                }
            }

            if (linkedBattery != null && linkedBattery.parent.Map != null)
            {
                float newEnergy = linkedBattery.GetEnergyStored(this.StoredEnergy);
                float maxEnergy = linkedBattery.GetEnergyMax();

                float energyPercent = 1.0f;

                if (newEnergy != maxEnergy && maxEnergy > 0.0f)
                    energyPercent = newEnergy / maxEnergy;

                this.Props.storedEnergyMax = Mathf.Max(1.0f, maxEnergy); // Can't let the max energy hit 0.0 or we'll get NaN values for StoredEnergyPct which could screw us or some other mod up
                this.SetStoredEnergyPct(energyPercent);
            }
            //else
            //{
            //    this.Props.storedEnergyMax = 0.0f;
            //    this.SetStoredEnergyPct(1.0f);
            //}
        }

        public float GetEnergyStored(float currentEnergyOtherSide)
        {
            if (this.PowerNet == null)
            {
                //Logger.MessageFormat(this, "No PowerNet found.");
                reloadedEnergyCalculated = true;

                oldDuplicateEnergy = duplicateEnergy = 1.0f;
                return 0.0f;
            }

            List<CompPowerBattery> networkBatteries = this.PowerNet.batteryComps.Where(x => x != this).ToList();

            oldDuplicateEnergy = duplicateEnergy;
            duplicateEnergy = currentEnergyOtherSide;
            float duplicateEnergyChange = duplicateEnergy - oldDuplicateEnergy;

            float networkEnergy = networkBatteries.Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();
            float networkMaxEnergy = networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();

            Logger.MessageFormat(this, "{0}, Old duplicate energy: {1}, new duplicate energy: {2}, Duplicate energy change: {3}, network: {4}/{5}", this.parent.GetType().ToString(), oldDuplicateEnergy, duplicateEnergy, duplicateEnergyChange, networkEnergy, networkMaxEnergy);

            if (reloadedEnergyCalculated && duplicateEnergyChange != 0.0f)
            {
                float networkEnergyPlusDuplicate = networkEnergy + duplicateEnergyChange;

                // If more energy lost in duplicate battery than available in duplicated network
                if (networkEnergyPlusDuplicate < 0.0f)
                {
                    if (networkEnergy > 0.0f)
                    {
                        // Drain the whole thing
                        foreach (CompPowerBattery battery in networkBatteries)
                            battery.SetStoredEnergyPct(0.0f);

                        networkEnergy = 0.0f;

                        Logger.MessageFormat(this, "Network drained");
                    }
                }
                // If more energy gained in duplicate battery than the network can hold
                else if (networkEnergyPlusDuplicate > networkMaxEnergy)
                {
                    if (networkEnergy < networkMaxEnergy)
                    {
                        // Max the whole thing
                        foreach (CompPowerBattery battery in networkBatteries)
                            battery.SetStoredEnergyPct(1.0f);

                        networkEnergy = networkMaxEnergy;

                        Logger.MessageFormat(this, "Network filled");
                    }
                }
                else
                {
                    float percentChange = duplicateEnergyChange / oldDuplicateEnergy;
                    float percentOfExisting = 1.0f + percentChange;
                    float newNetworkEnergy = 0.0f;

                    foreach (CompPowerBattery battery in networkBatteries)
                    {
                        float storedEnergyPercent = battery.StoredEnergyPct;
                        if (!float.IsNaN(storedEnergyPercent))
                        {
                            battery.SetStoredEnergyPct(storedEnergyPercent * percentOfExisting);
                            newNetworkEnergy += battery.StoredEnergy;
                        }
                    }

                    Logger.MessageFormat(this, "Duplicate energy change: {0}, energy returned to network: {1}", duplicateEnergyChange, (newNetworkEnergy - networkEnergy));

                    networkEnergy = newNetworkEnergy;
                }
            }


            reloadedEnergyCalculated = true;

            duplicateEnergy = networkEnergy;
            return networkEnergy;
        }

        public float GetEnergyMax()
        {
            List<CompPowerBattery> networkBatteries = this.PowerNet.batteryComps.Where(x => x != this).ToList();
            return networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();
        }
    }
}
