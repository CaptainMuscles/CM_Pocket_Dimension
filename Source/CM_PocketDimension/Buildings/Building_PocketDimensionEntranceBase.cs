using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace CM_PocketDimension
{
    public class Building_PocketDimensionEntranceBase : Building
    {
        public string uniqueName;

        public string dimensionSeed = null;

        private List<IntVec3> cachedAdjCellsCardinal;

        private IntVec3 oldPosition = new IntVec3(0, 0, 0);
        private Map oldMap = null;

        public override string LabelNoCount => uniqueName ?? base.LabelNoCount;
        public override string LabelCap => uniqueName ?? base.LabelCap;

        public bool BeingDestroyed = false;

        public bool GetLost = false;

        protected CompPowerBattery compPowerBattery = null;
        protected CompPocketDimensionBatteryShare compBatteryShare = null;

        public static readonly float WattsToWattDaysPerTick = 1.66666669E-05f;

        public bool MapCreated => !string.IsNullOrEmpty(dimensionSeed);

        public List<IntVec3> AdjCellsCardinalInBounds
        {
            get
            {
                if (this.Map != null && (cachedAdjCellsCardinal == null || oldPosition != this.Position || oldMap != this.Map))
                {
                    oldPosition = this.Position;
                    oldMap = this.Map;
                    cachedAdjCellsCardinal = (from c in GenAdj.CellsAdjacentCardinal(this)
                                              where c.InBounds(base.Map)
                                              select c).ToList();
                }
                return cachedAdjCellsCardinal;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref this.GetLost, "getLost", false);
            Scribe_Values.Look<string>(ref this.uniqueName, "uniqueName", null);
            Scribe_Values.Look<string>(ref this.dimensionSeed, "dimensionSeed", string.Empty);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            compPowerBattery = this.GetComp<CompPowerBattery>();
            compBatteryShare = this.GetComp<CompPocketDimensionBatteryShare>();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);

            BeingDestroyed = true;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            // Rename
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true),
                action = () => Find.WindowStack.Add(new Dialog_RenamePocketDimensionEntranceBase(this)),
                defaultLabel = "CM_RenamePocketDimensionLabel".Translate(),
                defaultDesc = "CM_RenamePocketDimensionDescription".Translate()
            };

            Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(this);

            if (otherSide != null && !otherSide.BeingDestroyed)
            { 
                // Select other side
                yield return new Command_Action
                {
                    action = delegate
                    {
                        if (otherSide != null)
                        {
                            LookTargets otherSideTarget = new LookTargets(otherSide.SpawnedParentOrMe);
                            CameraJumper.TrySelect(otherSideTarget.TryGetPrimaryTarget());
                        }
                    },
                    defaultLabel = this is Building_PocketDimensionBox ? "CM_ViewExitLabel".Translate() : "CM_ViewEntranceLabel".Translate(),
                    defaultDesc = this is Building_PocketDimensionBox ? "CM_ViewExitDescription".Translate() : "CM_ViewEntranceDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest"),
                };
            }

            if (Prefs.DevMode && MapCreated)
            {
                // Fix walls not being owned by player
                yield return new Command_Action
                {
                    action = () => PocketDimensionUtility.ClaimWalls(PocketDimensionUtility.GetMapParent(this.dimensionSeed).Map, Faction.OfPlayer),
                    defaultLabel = "DEBUG: Claim walls",
                };
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            bool isExit = ((this as Building_PocketDimensionBox) == null);
            Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(this);
            bool otherSideAvailable = (otherSide != null && otherSide.Map != null);

            string goThroughMenuItemText = "";

            if (isExit)
                goThroughMenuItemText = "CM_ExitPocketDimension".Translate(this.Label);
            else
                goThroughMenuItemText = "CM_EnterPocketDimension".Translate(this.Label);

            

            foreach (var opt in base.GetFloatMenuOptions(selPawn))
            {
                if (opt.Label != goThroughMenuItemText)
                {
                    yield return opt;
                }
            }

            if (otherSideAvailable)
            {
                Action goThroughAction = delegate
                {
                    Job goThroughJob = JobMaker.MakeJob(PocketDimensionDefOf.CM_EnterPocket, this);
                    selPawn.jobs.TryTakeOrderedJob(goThroughJob);
                };

                FloatMenuOption goThroughOption = new FloatMenuOption(goThroughMenuItemText, goThroughAction, MenuOptionPriority.Default, null, this);
                yield return goThroughOption;
            }
        }

        public override void Tick()
        {
            base.Tick();

            Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(this);

            if (otherSide != null && otherSide.Map != null && this.Map != null)
            {
                TransferHopperItems(otherSide);
                TransferTransporterItems(otherSide);
            }

            if (GetLost)
                this.Destroy(DestroyMode.Vanish);
        }

        public override string GetInspectString()
        {
            string inspectString = "";

            if (this.Spawned && this.MapCreated && compBatteryShare != null && compPowerBattery != null)
            {
                inspectString += "PowerBatteryStored".Translate() + ": " + compPowerBattery.StoredEnergy.ToString("F0") + " / " + compBatteryShare.StoredEnergyMax.ToString("F0") + " Wd";

                if (compPowerBattery.StoredEnergy > 0.0f)
                {
                    inspectString += "\n" + "SelfDischarging".Translate() + ": " + 5f.ToString("F0") + " W";
                }

                if (Prefs.DevMode)
                {
                    inspectString += "\n" + compBatteryShare.GetDebugString();
                }
            }

            // Deliberately ignoring comp and quest related output from parent classes
            return inspectString;// + "\n" + base.GetInspectString();
        }

        public bool ExistsInWorld()
        {
            if (this.MapHeld != null)
            {
                return true;
            }

            string parentage = this.Label;

            IThingHolder holder = this.ParentHolder;
            while (holder != null)
            {
                parentage += " - " + holder.GetType().ToString();

                // First check that there is a world object holding this
                WorldObject worldObject = holder as WorldObject;
                if (worldObject != null && worldObject.Tile >= 0)
                {
                    //Logger.MessageFormat(this, parentage);
                    return true;
                }

                // Could also be in a drop pod
                ActiveDropPodInfo activeDropPodInfo = holder as ActiveDropPodInfo;
                if (activeDropPodInfo != null)
                {
                    //Logger.MessageFormat(this, parentage);
                    return true;
                }

                holder = holder.ParentHolder;
            }

            //Logger.MessageFormat(this, parentage);

            return false;
        }

        private void TransferTransporterItems(Building_PocketDimensionEntranceBase otherSide)
        {
            CompTransporter transporter = this.GetComp<CompTransporter>();

            if (transporter != null)
            {
                Thing first = transporter.innerContainer.FirstOrFallback();

                if (first != null)
                    GenSpawn.Spawn(first, otherSide.InteractionCell, otherSide.Map, WipeMode.Vanish);

                if (transporter.leftToLoad != null && transporter.innerContainer != null && transporter.leftToLoad.Count == 0 && transporter.innerContainer.Count == 0)
                    transporter.CancelLoad();
            }
        }

        private void TransferHopperItems(Building_PocketDimensionEntranceBase otherSide)
        {
            for (int i = 0; i < AdjCellsCardinalInBounds.Count; i++)
            {
                Building_Storage hopper = null;
                List<Thing> thingList = AdjCellsCardinalInBounds[i].GetThingList(base.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    if ((thing.def == ThingDefOf.Hopper || thing.def == PocketDimensionDefOf.CM_PocketDimensionHopper) && thing as MinifiedThing == null)
                    {
                        hopper = thing as Building_Storage;
                        SlotGroup slotGroup = hopper.GetSlotGroup();
                        if (slotGroup != null && slotGroup.HeldThings != null)
                        {
                            foreach (Thing thing2 in slotGroup.HeldThings)
                            {
                                if (hopper.Accepts(thing2))
                                {
                                    thing2.DeSpawn(DestroyMode.Vanish);
                                    GenSpawn.Spawn(thing2, otherSide.InteractionCell, otherSide.Map, WipeMode.Vanish);
                                }
                            }
                        }
                        break;
                    }
                }

            }
        }
    }
}
