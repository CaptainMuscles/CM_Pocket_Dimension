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
    [StaticConstructorOnStartup]
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

        private static Graphic glowGraphic = GraphicDatabase.Get(typeof(Graphic_Multi), "Things/Building/PocketDimensionBox/Dim_Glow", ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);

        private static float baseRedness = 0.5f;
        private static float rednessPerDegree = 0.02f;

        private static float energyCapacityColorScale = 0.1f;

        protected bool ventOpen = true;

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

        public void SetVentOpen(bool open)
        {
            ventOpen = open;

            CompHasButton compHasButton = this.GetComp<CompHasButton>();

            if (compHasButton != null)
                compHasButton.SetActiveState(ventOpen);
        }

        public override void Draw()
        {
            base.Draw();

            float redness = 0.0f;
            float relativeCapacity = 0.0f;
            float energyPercent = 0.0f;

            Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(this);

            if (otherSide != null)
            {
                float myTemp = this.PositionHeld.GetTemperature(this.MapHeld);
                float otherTemp = otherSide.PositionHeld.GetTemperature(otherSide.MapHeld);

                redness = baseRedness;
                redness += (otherTemp - myTemp) * rednessPerDegree;
                redness = Mathf.Clamp(redness, 0.0f, 1.0f);


                CompPocketDimensionBatteryShare myBatteryShare = this.GetComp<CompPocketDimensionBatteryShare>();
                CompPocketDimensionBatteryShare otherBatteryShare = otherSide.GetComp<CompPocketDimensionBatteryShare>();

                if (myBatteryShare != null && otherBatteryShare != null)
                {
                    float otherEnergyCapacity = myBatteryShare.StoredEnergyMax;
                    float myEnergyCapacity = otherBatteryShare.StoredEnergyMax;
                    if (myEnergyCapacity == 0.0f)
                    {
                        relativeCapacity = 0.0f;
                    }
                    else
                    {
                        relativeCapacity = (((otherEnergyCapacity - myEnergyCapacity) / Mathf.Min(myEnergyCapacity, otherEnergyCapacity)) * energyCapacityColorScale) + 0.5f;
                        relativeCapacity = Mathf.Clamp(relativeCapacity, 0.0f, 1.0f);

                        energyPercent = myBatteryShare.EnergyPercent;
                    }
                }
            }

            Color glowColor = new Color(redness, energyPercent, relativeCapacity);
            glowGraphic.GetColoredVersion(ShaderDatabase.Cutout, glowColor, glowColor).Draw(new Vector3(this.DrawPos.x, this.DrawPos.y + 1f, this.DrawPos.z), Rot4.North, this);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref this.GetLost, "getLost", false);
            Scribe_Values.Look<string>(ref this.uniqueName, "uniqueName", null);
            Scribe_Values.Look<string>(ref this.dimensionSeed, "dimensionSeed", string.Empty);
            Scribe_Values.Look<bool>(ref this.ventOpen, "ventOpen", true);
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

            if (this.Spawned && this.MapCreated)
            {
                if (ventOpen)
                    inspectString += "CM_PocketDimension_VentOpen".Translate();
                else
                    inspectString += "CM_PocketDimension_VentClosed".Translate();

                if (compBatteryShare != null && compPowerBattery != null)
                {
                    inspectString += "\n" + "PowerBatteryStored".Translate() + ": " + compPowerBattery.StoredEnergy.ToString("F0") + " / " + compBatteryShare.StoredEnergyMax.ToString("F0") + " Wd";

                    if (compPowerBattery.StoredEnergy > 0.0f)
                    {
                        inspectString += "\n" + "SelfDischarging".Translate() + ": " + 5f.ToString("F0") + " W";
                    }

                    if (Prefs.DevMode)
                    {
                        inspectString += "\n" + compBatteryShare.GetDebugString();
                    }
                }
            }

            // Deliberately ignoring comp and quest related output from parent classes
            return inspectString;// + "\n" + base.GetInspectString();
        }

        protected override void ReceiveCompSignal(string signal)
        {
            Building_PocketDimensionEntranceBase otherSide = PocketDimensionUtility.GetOtherSide(this);

            if (signal == "CM_PocketDimension_ButtonPressed_On")
                ventOpen = true;
            else if(signal == "CM_PocketDimension_ButtonPressed_Off")
                ventOpen = false;

            if (otherSide != null)
                otherSide.SetVentOpen(ventOpen);
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
                {
                    GenSpawn.Spawn(first, otherSide.InteractionCell, otherSide.Map, WipeMode.Vanish);
                }
                else if (transporter.LoadingInProgressOrReadyToLaunch && !transporter.AnyInGroupHasAnythingLeftToLoad)
                {
                    transporter.CancelLoad();
                }
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
