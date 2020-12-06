using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CM_PocketDimension
{
    public class Building_PocketDimensionBox : Building_PocketDimensionEntranceBase
    {
        private float temperatureEqualizeRate = 4.0f; // Put this in an xml configurable property? Number 14.0f pulled from Building_Vent hardcoding
        private int temperatureEqualizeInterval = 250; // Rare tick

        private bool ventOpen = true;
        private int mapSize = 1;
        private int desiredMapSize = 1;

        private int minMapSize = 1;
        private int maxMapSize = 30;

        private int MapDiameter => (mapSize * 6) + 1;
        private int DesiredMapDiameter => (desiredMapSize * 6) + 1;

        //private float wattsPerFullVanillaBattery = 32000000f;
        private float wattDaysPerFullVanillaBattery = 600f;

        private CompRefuelable compRefuelable = null;
        //private CompFlickable compFlickable = null;
        //private CompPowerShare compPowerShare = null;
        private CompTransporter compTransporter = null;

        private float desiredComponentCount = 1.0f;
        private float desiredEnergyAmount = 32000000f;

        public bool countingWealth = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.ventOpen, "ventOpen", true);
            Scribe_Values.Look<int>(ref this.mapSize, "mapSize", 1);
            Scribe_Values.Look<int>(ref this.desiredMapSize, "mapSize", 1);

            if (!string.IsNullOrEmpty(dimensionSeed))
                PocketDimensionUtility.Boxes[this.dimensionSeed] = this;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Logger.MessageFormat(this, "Spawning");

            compRefuelable = this.GetComp<CompRefuelable>();
            //compFlickable = this.GetComp<CompFlickable>();
            //compPowerShare = this.GetComp<CompPowerShare>();
            compTransporter = this.GetComp<CompTransporter>();

            if (!respawningAfterLoad)
            {
                compRefuelable.Refuel(1.0f);
                //compFlickable.SwitchIsOn = false;
                // wantSwitchOn defaults to true and can only be changed by flicking *grumble grumble*
                //var prop = compFlickable.GetType().GetField("wantSwitchOn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                //prop.SetValue(compFlickable, false);
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
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);

            MapParent_PocketDimension dimensionMapParent = null;

            if (PocketDimensionUtility.MapParents.TryGetValue(dimensionSeed, out dimensionMapParent))
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

            if (!MapCreated)
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
                    disabled = (compRefuelable.Fuel < compRefuelable.Props.fuelCapacity /*|| !compFlickable.SwitchIsOn*/ || compPowerBattery.PowerNet == null || compPowerBattery.PowerNet.CurrentStoredEnergy() < (this.desiredEnergyAmount - 1.0f)),
                };
            }
        }

        private void SetDesiredMapSize(int newMapSize)
        {
            newMapSize = Math.Min(newMapSize, maxMapSize);
            newMapSize = Math.Max(minMapSize, newMapSize);

            desiredMapSize = newMapSize;

            if (!MapCreated)
            {
                desiredComponentCount = desiredMapSize * desiredMapSize;
                desiredEnergyAmount = desiredMapSize * wattDaysPerFullVanillaBattery;
            }
            else
            {
                desiredComponentCount = (desiredMapSize * desiredMapSize) - (mapSize * mapSize);
                desiredEnergyAmount = (desiredMapSize - mapSize) * wattDaysPerFullVanillaBattery;
            }

            compRefuelable.Props.fuelCapacity = desiredComponentCount;

            if (compRefuelable.Fuel > compRefuelable.Props.fuelCapacity && this.Spawned)
            {
                float amountToRefund = compRefuelable.Fuel - compRefuelable.Props.fuelCapacity;
                compRefuelable.ConsumeFuel(amountToRefund);

                ThingDef thingToRefundDef = compRefuelable.Props.fuelFilter.AnyAllowedDef;

                if (thingToRefundDef != null)
                {
                    Thing refundedFuel = ThingMaker.MakeThing(thingToRefundDef);
                    refundedFuel.stackCount = (int)Mathf.Round(amountToRefund);
                    GenPlace.TryPlaceThing(refundedFuel, this.PositionHeld, this.MapHeld, ThingPlaceMode.Direct);
                }
            }

        }

        private void InitializePocketDimension()
        {
            if (compPowerBattery.PowerNet == null || compPowerBattery.PowerNet.CurrentStoredEnergy() < (this.desiredEnergyAmount - 1.0f))
                return;

            // Consume advanced components
            compRefuelable.ConsumeFuel(compRefuelable.Props.fuelCapacity);
            compRefuelable.Props.fuelCapacity = 0.0f;

            // Consume battery power
            float percentLost = (this.desiredEnergyAmount - 1.0f) / compPowerBattery.PowerNet.CurrentStoredEnergy();
            float percentRemaining = 1.0f - percentLost;
            foreach (CompPowerBattery battery in compPowerBattery.PowerNet.batteryComps)
                battery.SetStoredEnergyPct(percentRemaining * battery.StoredEnergyPct);

            mapSize = desiredMapSize;
            CreateMap(this.MapDiameter);
        }

        private void CreateMap(int mapSize)
        {
            if (string.IsNullOrEmpty(dimensionSeed))
            {
                GeneratePocketMap(mapSize);
            }
        }

        //Generates a map with a defined seed
        private void GeneratePocketMap(int mapSize)
        {
            CompPocketDimensionContainer dimensionProperties = this.GetComp<CompPocketDimensionContainer>();

            IntVec3 size = new IntVec3(mapSize, 1, mapSize);
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
            var thingToMake = PocketDimensionDefOf.CM_PocketDimensionExit;
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

            PocketDimensionUtility.MapParents[this.dimensionSeed] = mapParent;
            
            PocketDimensionUtility.Exits[this.dimensionSeed] = exit;

            Messages.Message("CM_PocketDimensionCreated".Translate(), new TargetInfo(this), MessageTypeDefOf.PositiveEvent);
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

            return 1.0f;
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
                    Logger.ErrorFormat(this, "averageTemperature: {0}, temperature: {1}, temperatureEqualizeRate: {2}, temperatureChangeAmount: {3}, roomSize: {4}", averageTemperature, temperature, temperatureEqualizeRate, temperatureChangeAmount, roomSize);
                }
                else
                {
                    room.Temperature += temperatureChange;
                }
            }
        }

        public override string GetInspectString()
        {
            if (!this.Spawned)
                return base.GetInspectString();

            string inspectString = "";

            if (!MapCreated)
            {
                inspectString = (((string)("CM_BatteryPowerNeeded".Translate() + ": " + (desiredEnergyAmount).ToString("#####0") + " Wd")));
            }
            else
            {
                inspectString = base.GetInspectString();
            }

            if (desiredMapSize > mapSize || !MapCreated)
            {
                inspectString += "\n" + compRefuelable.Props.FuelLabel + ": " + compRefuelable.Fuel.ToStringDecimalIfSmall() + " / " + compRefuelable.Props.fuelCapacity.ToStringDecimalIfSmall();
            }

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
    }
}
