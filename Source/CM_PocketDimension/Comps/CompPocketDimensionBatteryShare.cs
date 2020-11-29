using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public class CompPocketDimensionBatteryShare : ThingComp
    {
        private CompPocketDimensionBatteryShare linkedBatteryShare = null;
        private CompPowerBattery thisBattery = null;

        // Only modify these two variables inside GetEnergyStored, since it is called by our linked counterpart
        private float duplicateEnergy = 0.0f;
        private float oldDuplicateEnergy = 0.0f;

        private float storedEnergyMax = 100.0f;

        private bool reloadedEnergyCalculated = false;

        public CompProperties_PocketDimensionBatteryShare Props => (CompProperties_PocketDimensionBatteryShare)props;

        public float StoredEnergyMax => storedEnergyMax;

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (thisBattery == null)
                thisBattery = this.parent.GetComp<CompPowerBattery>();
            if (thisBattery != null)
                storedEnergyMax = Mathf.Max(storedEnergyMax, thisBattery.StoredEnergy);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (thisBattery == null)
                thisBattery = this.parent.GetComp<CompPowerBattery>();
            if (thisBattery != null)
                storedEnergyMax = Mathf.Max(storedEnergyMax, thisBattery.StoredEnergy);
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

        public override void CompTickLong()
        {
            base.CompTickLong();

            this.CompTickWhatever();
        }

        private void CompTickWhatever()
        {
            if (linkedBatteryShare == null)
            {
                Building_PocketDimensionEntranceBase parentEntrance = this.parent as Building_PocketDimensionEntranceBase;

                if (parentEntrance != null)
                {
                    Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(parentEntrance);

                    if (otherSide != null)
                        linkedBatteryShare = otherSide.GetComp<CompPocketDimensionBatteryShare>();
                }
            }

            if (thisBattery != null && linkedBatteryShare != null && linkedBatteryShare.parent.Map != null)
            {
                float newEnergy = linkedBatteryShare.GetEnergyStored(thisBattery.StoredEnergy);
                float maxEnergy = linkedBatteryShare.GetEnergyMax() + Props.storedEnergyMax;
                float energyPercent = 1.0f;

                if (newEnergy > maxEnergy)
                    newEnergy = maxEnergy;

                storedEnergyMax = maxEnergy;

                // The props is a single instance shared across all instances of the comp. Just need to make sure it can always hold enough.
                if (thisBattery.Props.storedEnergyMax < maxEnergy)
                    thisBattery.Props.storedEnergyMax = maxEnergy;

                if (newEnergy != thisBattery.Props.storedEnergyMax && thisBattery.Props.storedEnergyMax > 0.0f)
                    energyPercent = newEnergy / thisBattery.Props.storedEnergyMax;

                thisBattery.SetStoredEnergyPct(energyPercent);

                Logger.MessageFormat(this, "Updated battery, energy: {0} ({2}), max: {1} ({3}), {4} - {5}", newEnergy, maxEnergy, thisBattery.StoredEnergy, thisBattery.Props.storedEnergyMax, thisBattery.parent.Label, linkedBatteryShare.parent.Label);
            }
        }

        public float GetEnergyStored(float currentEnergyOtherSide)
        {
            if (thisBattery.PowerNet == null)
            {
                //Logger.MessageFormat(this, "No PowerNet found.");
                reloadedEnergyCalculated = true;

                oldDuplicateEnergy = duplicateEnergy = 1.0f;
                return 0.0f;
            }

            List<CompPowerBattery> networkBatteries = this.AvailableBatteries();

            oldDuplicateEnergy = duplicateEnergy;
            duplicateEnergy = currentEnergyOtherSide;
            float duplicateEnergyChange = duplicateEnergy - oldDuplicateEnergy;

            float networkEnergy = networkBatteries.Select(x => x.StoredEnergy).DefaultIfEmpty(0.0f).Sum();
            float networkMaxEnergy = networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();

            //Logger.MessageFormat(this, "{0}, Old duplicate energy: {1}, new duplicate energy: {2}, Duplicate energy change: {3}, network: {4}/{5}", this.parent.GetType().ToString(), oldDuplicateEnergy, duplicateEnergy, duplicateEnergyChange, networkEnergy, networkMaxEnergy);

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
                else if (oldDuplicateEnergy > 0.0f)
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

            // Add in any extra energy stored in the sharer
            if (currentEnergyOtherSide > networkMaxEnergy)
                networkEnergy += (currentEnergyOtherSide - networkMaxEnergy);

            duplicateEnergy = networkEnergy;
            return networkEnergy;
        }

        public float GetEnergyMax()
        {
            List<CompPowerBattery> networkBatteries = this.AvailableBatteries();
            return networkBatteries.Select(x => x.Props.storedEnergyMax).DefaultIfEmpty(0.0f).Sum();
        }

        private List<CompPowerBattery> AvailableBatteries()
        {
            //List<CompPowerBattery> batteries = new List<CompPowerBattery>();
            //foreach (CompPowerBattery battery in thisBattery.PowerNet.batteryComps)
            //{
            //    if (battery != thisBattery && battery.parent.GetComp<CompPocketDimensionBatteryShare>() == null)
            //        batteries.Add(battery);
            //}
            //return batteries;

            // This method would allow chains of pocket dimensions to all share one battery pool.
            // Unfortunately it could result in infinite power in a case where someone teleports (using farskip or another mod) a box entrance/exit so that it makes a circular loop
            //return thisBattery.PowerNet.batteryComps.Where(x => x != thisBattery).ToList();

            // Teleport safe version, get batteries that are not linked to outside sources
            return thisBattery.PowerNet.batteryComps.Where(x => x != thisBattery && x.parent.GetComp<CompPocketDimensionBatteryShare>() == null).ToList();
        }
    }
}
