using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using System.Text;

namespace CM_PocketDimension
{
    public class Building_PocketDimensionBox : Building_PocketDimensionEntranceBase
    {
        // For backwards compatibility from when CompRefuelable was being used
        private float fuel = 0.0f;

        private float temperatureEqualizeRate = 4.0f; // Put this in an xml configurable property? Number 14.0f pulled from Building_Vent hardcoding
        private int temperatureEqualizeInterval = 250; // Rare tick

        private bool ventOpen = true;
        private int mapSize = 0;
        private int desiredMapSize = 1;

        private int minMapSize = 1;
        private int maxMapSize = 30;

        public int MapSize => mapSize;
        private int MapDiameter => (mapSize * 6) + 1;
        private int DesiredMapDiameter => (desiredMapSize * 6) + 1;

        CompPocketDimensionCreator compCreator = null;
        private CompTransporter compTransporter = null;

        private int desiredComponentCount = 1;
        private float desiredEnergyAmount = 32000000f;

        public bool doingRecursiveThing = false;

        public int ComponentsNeeded => (compCreator != null) ? desiredComponentCount - compCreator.SupplyCount : 0;
        public bool NeedsComponents => ComponentsNeeded > 0;

        public void AddComponents(List<Thing> componentsList)
        {
            if (compCreator == null)
                return;

            while (ComponentsNeeded > 0 && componentsList.Count > 0)
            {
                Thing component = componentsList.Pop();
                int componentsGiven = Mathf.Min(ComponentsNeeded, component.stackCount);
                if (compCreator.AddComponents(componentsGiven))
                    component.SplitOff(componentsGiven).Destroy();
                else
                    break;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.ventOpen, "ventOpen", true);
            Scribe_Values.Look<int>(ref this.mapSize, "mapSize", 0);
            Scribe_Values.Look<int>(ref this.desiredMapSize, "desiredMapSize", 1);
            Scribe_Values.Look<int>(ref this.desiredComponentCount, "desiredComponentCount", 1);
            Scribe_Values.Look<float>(ref this.desiredEnergyAmount, "desiredEnergyAmount", 1);
            Scribe_Values.Look<float>(ref this.fuel, "fuel", 0.0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!string.IsNullOrEmpty(dimensionSeed))
                    PocketDimensionUtility.Boxes[this.dimensionSeed] = this;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Logger.MessageFormat(this, "Spawning");

            compCreator = this.GetComp<CompPocketDimensionCreator>();
            compTransporter = this.GetComp<CompTransporter>();

            if (mapSize == 0)
                mapSize = 1;

            if (fuel >= 1.0f)
            {
                if (compCreator != null)
                {
                    compCreator.AddComponents((int)Mathf.Round(fuel));
                    fuel = 0.0f;

                    if (compCreator.SupplyCount > desiredComponentCount)
                    {
                        int amountToRefund = compCreator.SupplyCount - desiredComponentCount;
                        if (compCreator.ConsumeComponents(amountToRefund))
                        {
                            ThingDef thingToRefundDef = compCreator.Props.componentDef;

                            RefundComponents(thingToRefundDef, amountToRefund);
                        }
                    }
                }
                else
                {
                    ThingDef thingToRefundDef = ThingDefOf.ComponentSpacer;

                    int amountToRefund = (int)Mathf.Round(fuel);
                    fuel = 0.0f;

                    RefundComponents(thingToRefundDef, amountToRefund);
                }
            }

            // Reconfigure runtime-set comp property values
            SetDesiredMapSize(desiredMapSize);

            if (MapCreated)
            {
                MapParent_PocketDimension dimensionMapParent = PocketDimensionUtility.GetMapParent(this.dimensionSeed);

                // Looks like we just got installed somewhere. Make sure map tile is the same as our current tile
                if (this.Map != null && dimensionMapParent != null)
                {
                    dimensionMapParent.Tile = this.Map.Parent.Tile;
                }
            }
            else
            {
                if (compCreator != null && compCreator.Props.preMadeMapSize > 0)
                {
                    SetDesiredMapSize(compCreator.Props.preMadeMapSize);
                    mapSize = desiredMapSize;
                    CreateMap(this.MapDiameter);
                }
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);

            MapParent_PocketDimension dimensionMapParent = null;

            if (!string.IsNullOrEmpty(dimensionSeed) && PocketDimensionUtility.MapParents.TryGetValue(dimensionSeed, out dimensionMapParent))
            {
                dimensionMapParent.Abandon(GetLost);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                // Don't show load button if map not yet created
                if (!MapCreated && c is Command_LoadToTransporter)
                    continue;
                // Don't show toggle auto refuel button if map is created and we are not resizing the dimension
                if (MapCreated && desiredMapSize <= mapSize && c is Command_Toggle && (c as Command_Toggle).defaultLabel == "CommandToggleAllowAutoRefuel".Translate())
                    continue;
                yield return c;
            }

            if (compCreator != null && !MapCreated)
            {
                // Decrease pocket dimension size
                yield return new Command_Action
                {
                    action = delegate
                    {
                        SetDesiredMapSize(desiredMapSize - 1);
                    },
                    defaultLabel = "CM_DecreasePocketDimensionSizeLabel".Translate(),
                    defaultDesc = "CM_DecreasePocketDimensionSizeDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Minus"),
                    disabled = (desiredMapSize <= minMapSize),
                };

                // Increase pocket dimension size
                yield return new Command_Action
                {
                    action = delegate
                    {
                        SetDesiredMapSize(desiredMapSize + 1);
                    },
                    defaultLabel = "CM_IncreasePocketDimensionSizeLabel".Translate(),
                    defaultDesc = "CM_IncreasePocketDimensionSizeDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Plus"),
                    disabled = (desiredMapSize >= maxMapSize),
                };

                // Create the pocket dimension
                yield return new Command_Action
                {
                    action = delegate
                    {
                        InitializePocketDimension();
                    },
                    defaultLabel = "CM_CreatePocketDimension".Translate(),
                    defaultDesc = "CM_CreatePocketDimension".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Mote/ShotHit_Spark"),
                    disabled = (compCreator == null || 
                                compCreator.SupplyCount < desiredComponentCount || 
                                (compPowerBattery == null && this.desiredEnergyAmount >= 1.0f) || 
                                (compPowerBattery != null && (compPowerBattery.PowerNet == null || compPowerBattery.PowerNet.CurrentStoredEnergy() < (this.desiredEnergyAmount - 1.0f)))),
                };

                if (Prefs.DevMode)
                {
                    // Create the pocket dimension
                    yield return new Command_Action
                    {
                        action = delegate
                        {
                            InitializePocketDimension(true);
                        },
                        defaultLabel = "DEBUG: Initialize",
                    };
                }
            }
        }

        private void SetDesiredMapSize(int newMapSize)
        {
            newMapSize = Math.Min(newMapSize, maxMapSize);
            newMapSize = Math.Max(minMapSize, newMapSize);

            desiredMapSize = newMapSize;

            if (compCreator == null)
                return;

            if (!MapCreated)
            {
                desiredComponentCount = (desiredMapSize * desiredMapSize) * compCreator.Props.componentMultiplier;
                desiredEnergyAmount = desiredMapSize * compCreator.Props.powerMultiplier;
            }
            else
            {
                desiredComponentCount = ((desiredMapSize * desiredMapSize) - (mapSize * mapSize)) * compCreator.Props.componentMultiplier;
                desiredEnergyAmount = (desiredMapSize - mapSize) * compCreator.Props.powerMultiplier;
            }

            if (compCreator.SupplyCount > desiredComponentCount && this.SpawnedOrAnyParentSpawned)
            {
                int amountToRefund = compCreator.SupplyCount - desiredComponentCount;
                if (compCreator.ConsumeComponents(amountToRefund))
                {
                    ThingDef thingToRefundDef = compCreator.Props.componentDef;

                    RefundComponents(thingToRefundDef, amountToRefund);
                }
            }
        }

        private void RefundComponents(ThingDef thingToRefundDef, int amountToRefund)
        {
            if (thingToRefundDef != null)
            {
                Logger.MessageFormat(this, "Refunding {0} {1}(s)", amountToRefund, thingToRefundDef.defName);

                Thing refundedFuel = ThingMaker.MakeThing(thingToRefundDef);
                refundedFuel.stackCount = amountToRefund;
                GenPlace.TryPlaceThing(refundedFuel, this.PositionHeld, this.MapHeld, ThingPlaceMode.Direct);
            }
        }

        private void InitializePocketDimension(bool devCheat = false)
        {
            if (MapCreated)
                return;

            if (!devCheat)
            {
                if (compCreator == null || (compPowerBattery != null && (compPowerBattery.PowerNet == null || compPowerBattery.PowerNet.CurrentStoredEnergy() < (this.desiredEnergyAmount - 1.0f))))
                    return;

                // Consume advanced components or whatever
                if (!compCreator.ConsumeComponents(desiredComponentCount))
                    return;

                // Consume battery power
                if (compPowerBattery != null)
                {
                    float percentLost = (this.desiredEnergyAmount - 1.0f) / compPowerBattery.PowerNet.CurrentStoredEnergy();
                    float percentRemaining = 1.0f - percentLost;
                    foreach (CompPowerBattery battery in compPowerBattery.PowerNet.batteryComps)
                        battery.SetStoredEnergyPct(percentRemaining * battery.StoredEnergyPct);
                }
            }

            desiredComponentCount = 0;
            desiredEnergyAmount = 0.0f;

            mapSize = desiredMapSize;
            CreateMap(this.MapDiameter);
        }

        private void CreateMap(int mapWidth)
        {
            if (string.IsNullOrEmpty(dimensionSeed) && compCreator != null)
            {
                GeneratePocketMap(mapWidth);
            }
        }

        //Generates a map with a defined seed
        private void GeneratePocketMap(int mapWidth)
        {
            IntVec3 size = new IntVec3(mapWidth, 1, mapWidth);
            this.dimensionSeed = Find.TickManager.TicksAbs.ToString();

            // The new map must be connected to a parent on the world map
            var mapParent = (MapParent_PocketDimension)WorldObjectMaker.MakeWorldObject(PocketDimensionDefOf.CM_WorldObject_PocketDimension);
            mapParent.Tile = this.Map.Tile;
            mapParent.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(mapParent);

            // Generate the map and set the maps entrance to this box so the map knows what stuff it is made of
            string cachedSeedString = Find.World.info.seedString;
            Find.World.info.seedString = this.dimensionSeed;
            PocketDimensionUtility.Boxes[this.dimensionSeed] = this;
            mapParent.dimensionSeed = this.dimensionSeed;
            Map generatedMap = MapGenerator.GenerateMap(size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
            Find.World.info.seedString = cachedSeedString;

            // Permanent darkness - seems moot since adding roof and walls...
            GameCondition_NoSunlight gameCondition_NoSunlight = (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(PocketDimensionDefOf.CM_PocketDimensionCondition, -1);
            gameCondition_NoSunlight.Permanent = true;
            generatedMap.gameConditionManager.RegisterCondition(gameCondition_NoSunlight);

            // Now make an exit in the map
            ThingDef thingToMake = compCreator.Props.exitDef;
            List<Thing> thingList = generatedMap.Center.GetThingList(generatedMap).Where(x => x.def == thingToMake).ToList();
            if (thingList.Count() == 0)
            {
                var newExit = ThingMaker.MakeThing(thingToMake, this.Stuff);
                newExit.SetFaction(this.Faction);
                GenPlace.TryPlaceThing(newExit, generatedMap.Center, generatedMap, ThingPlaceMode.Direct);
                thingList = generatedMap.Center.GetThingList(generatedMap).Where(x => x.def == thingToMake).ToList();
            }

            Logger.MessageFormat(this, this.dimensionSeed);

            Building_PocketDimensionExit exit = thingList.First() as Building_PocketDimensionExit;
            exit.dimensionSeed = this.dimensionSeed;
            if (!string.IsNullOrEmpty(this.uniqueName))
                exit.uniqueName = "CM_PocketDimension_ExitName".Translate(this.uniqueName);
            else
                exit.uniqueName = "CM_PocketDimension_ExitName".Translate(this.LabelCap);

            PocketDimensionUtility.MapParents[this.dimensionSeed] = mapParent;

            PocketDimensionUtility.Exits[this.dimensionSeed] = exit;

            Messages.Message("CM_PocketDimensionCreated".Translate(), new TargetInfo(this), MessageTypeDefOf.PositiveEvent);

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_PsycastPsychicEffect");
            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail("Psycast_Skip_Exit");

            if (moteDef != null)
                MoteMaker.MakeAttachedOverlay(this, moteDef, Vector3.zero, mapSize);

            if (soundDef != null)
                soundDef.PlayOneShot(new TargetInfo(this.Position, this.Map));
        }

        public override void Tick()
        {
            base.Tick();

            if (this.GetLost)
                return;

            if (MapCreated)
            {
                if (ventOpen && this.IsHashIntervalTick(temperatureEqualizeInterval))
                {
                    EqualizeTemperatures();
                }
            }
        }

        private void EqualizeTemperatures()
        {
            Building_PocketDimensionExit exit = PocketDimensionUtility.GetExit(this.dimensionSeed);
            if (exit == null)
                return;

            RoomGroup thisRoomGroup = this.GetRoomGroup();
            RoomGroup otherRoomGroup = exit.GetRoomGroup();

            if (thisRoomGroup == null || otherRoomGroup == null || thisRoomGroup == otherRoomGroup)
                return;

            float totalTemperature = thisRoomGroup.Temperature + otherRoomGroup.Temperature;
            float averageTemperature = totalTemperature / 2.0f;

            float thisRoomChangeAmount = this.GetTemperatureChangeAmount(thisRoomGroup, averageTemperature);
            float otherRoomChangeAmount = this.GetTemperatureChangeAmount(otherRoomGroup, averageTemperature);
            float temperatureChangeAmount = Mathf.Min(thisRoomChangeAmount, otherRoomChangeAmount);

            EqualizeTemperatureForRoom(thisRoomGroup, averageTemperature, temperatureChangeAmount);
            EqualizeTemperatureForRoom(otherRoomGroup, averageTemperature, temperatureChangeAmount);
        }

        private float GetTemperatureChangeAmount(RoomGroup room, float averageTemperature)
        {
            if (!room.UsesOutdoorTemperature)
            {
                float roomSize = (float)room.CellCount;
                float temperature = room.Temperature;
                float temperatureDifferenceTimesRate = (averageTemperature - temperature) * temperatureEqualizeRate;
                float temperatureChange = temperatureDifferenceTimesRate / roomSize;
                float resultingTemperature = temperature + temperatureChange;
                if (temperatureDifferenceTimesRate > 0f && resultingTemperature > averageTemperature)
                {
                    resultingTemperature = averageTemperature;
                }
                else if (temperatureDifferenceTimesRate < 0f && resultingTemperature < averageTemperature)
                {
                    resultingTemperature = averageTemperature;
                }
                return Mathf.Abs((resultingTemperature - temperature) * roomSize / temperatureDifferenceTimesRate);
            }

            return 0.0f;
        }

        private void EqualizeTemperatureForRoom(RoomGroup room, float averageTemperature, float temperatureChangeAmount)
        {
            if (!room.UsesOutdoorTemperature)
            {
                float roomSize = (float)room.CellCount;
                float temperature = room.Temperature;
                float temperatureChange = (averageTemperature - temperature) * temperatureEqualizeRate * temperatureChangeAmount / roomSize;

                if (float.IsNaN(temperatureChange))
                {
                    //Logger.ErrorFormat(this, "averageTemperature: {0}, temperature: {1}, temperatureEqualizeRate: {2}, temperatureChangeAmount: {3}, roomSize: {4}", averageTemperature, temperature, temperatureEqualizeRate, temperatureChangeAmount, roomSize);
                }
                else
                {
                    room.Temperature += temperatureChange;
                }
            }
        }

        public override string GetInspectString()
        {
            string inspectString = "";
            int mapSizeToDisplay = mapSize;
            if (compCreator != null)
            {
                if (compCreator.Props.preMadeMapSize > 0)
                    mapSizeToDisplay = compCreator.Props.preMadeMapSize;
                else if (desiredMapSize > mapSize || !MapCreated)
                    mapSizeToDisplay = desiredMapSize;
            }

            if (!this.Spawned)
            {
                inspectString = base.GetInspectString();

                if (mapSizeToDisplay > 0)
                    inspectString = AddInspectStringLine(inspectString, "CM_PocketDimension_MapSize".Translate(mapSizeToDisplay));

                return inspectString;
            }

            if (mapSizeToDisplay > 0)
                inspectString = AddInspectStringLine(inspectString, "CM_PocketDimension_MapSize".Translate(mapSizeToDisplay));

            if (!MapCreated)
            {
                if (desiredEnergyAmount >= 1.0f)
                    inspectString = AddInspectStringLine(inspectString, (((string)("CM_BatteryPowerNeeded".Translate() + ": " + (desiredEnergyAmount).ToString("#####0") + " Wd"))));
            }
            else
            {
                inspectString = AddInspectStringLine(inspectString, base.GetInspectString());
            }

            if (compCreator != null && (desiredMapSize > mapSize || !MapCreated))
            {
                inspectString = AddInspectStringLine(inspectString, compCreator.Props.ComponentLabel + ": " + compCreator.SupplyCount.ToString() + " / " + desiredComponentCount.ToString());
            }

            return inspectString;
        }

        private string AddInspectStringLine(string inspectString, string line)
        {
            if (string.IsNullOrEmpty(inspectString))
                inspectString = line;
            else if (!string.IsNullOrEmpty(line))
                inspectString = inspectString + "\n" + line;

            return inspectString;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            DrawBoxAround(DesiredMapDiameter);

            if (MapCreated && desiredMapSize != mapSize)
                DrawBoxAround(MapDiameter, SimpleColor.Yellow);
        }

        private void DrawBoxAround(int diameter, SimpleColor color = SimpleColor.White)
        {
            float y = AltitudeLayer.MetaOverlays.AltitudeFor();

            int diameterMinusOne = diameter - 1;
            int radius = diameterMinusOne / 2;
            int radiusPlusOne = radius + 1;

            Vector3 vector = new Vector3((float)(Position.x - radius), y, (float)(Position.z - radius));
            Vector3 vector2 = new Vector3((float)(Position.x + radiusPlusOne), y, (float)(Position.z - radius));
            Vector3 vector3 = new Vector3((float)(Position.x + radiusPlusOne), y, (float)(Position.z + radiusPlusOne));
            Vector3 vector4 = new Vector3((float)(Position.x - radius), y, (float)(Position.z + radiusPlusOne));
            GenDraw.DrawLineBetween(vector, vector2, color);
            GenDraw.DrawLineBetween(vector2, vector3, color);
            GenDraw.DrawLineBetween(vector3, vector4, color);
            GenDraw.DrawLineBetween(vector4, vector, color);
        }

        public float CalculateAdditionalMarketValue(float baseValue)
        {
            if (this.doingRecursiveThing)
            {
                Logger.MessageFormat(this, "Counting box wealth again recursively. Skipping...");
                return 0.0f;
            }

            float componentValue = CalculateUsedComponentValue();
            float contentValue = 0.0f;

            MapParent_PocketDimension innerMapParent = PocketDimensionUtility.GetMapParent(this.dimensionSeed);
            if (innerMapParent != null && innerMapParent.Map != null)
            {
                this.doingRecursiveThing = true;
                try
                {
                    contentValue = innerMapParent.Map.wealthWatcher.WealthTotal;
                }
                finally
                {
                    this.doingRecursiveThing = false;
                }
            }

            Logger.MessageFormat(this, "Adding value to box: {0}, {1} + {2} + {3} = {4}", this.Label, baseValue, contentValue, componentValue, (baseValue + contentValue + componentValue));

            return contentValue + componentValue;
        }

        private float CalculateUsedComponentValue()
        {
            float result = 0.0f;

            if (MapCreated)
            {
                int componentsUsed = 0;

                ThingDef fuelItemDef = null;

                compCreator = this.GetComp<CompPocketDimensionCreator>();
                if (compCreator != null)
                {
                    if (compCreator.Props.preMadeMapSize > 0)
                        componentsUsed = -(compCreator.Props.preMadeMapSize * compCreator.Props.preMadeMapSize);

                    fuelItemDef = compCreator.Props.componentDef;
                }
                    

                if (fuelItemDef != null)
                {
                    componentsUsed += (this.MapSize * this.MapSize) * compCreator.Props.componentMultiplier;

                    result = fuelItemDef.BaseMarketValue * componentsUsed;
                }
            }

            return result;
        }
    }
}
