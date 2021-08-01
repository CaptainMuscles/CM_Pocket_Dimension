using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public static class DefPatches
    {
        [HarmonyPatch(typeof(DefGenerator))]
        [HarmonyPatch("GenerateImpliedDefs_PreResolve", MethodType.Normal)]
        public static class PocketDimensionGenerateTerrainDefs
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                List<ThingDef> woodyStuff = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsStuff && def.stuffProps.categories.Contains(StuffCategoryDefOf.Woody)).ToList();
                List<ThingDef> stonyStuff = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsStuff && def.stuffProps.categories.Contains(StuffCategoryDefOf.Stony)).ToList();
                List<ThingDef> metallicStuff = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsStuff && def.stuffProps.categories.Contains(StuffCategoryDefOf.Metallic)).ToList();

                CopyAndStuffTerrainDef(PocketDimensionDefOf.CM_PocketDimensionFloorWood, woodyStuff);
                CopyAndStuffTerrainDef(PocketDimensionDefOf.CM_PocketDimensionFloorStone, stonyStuff);
                CopyAndStuffTerrainDef(PocketDimensionDefOf.CM_PocketDimensionFloorMetal, metallicStuff);
            }

            private static void CopyAndStuffTerrainDef(TerrainDef terrainToCopy, List<ThingDef> stuffList)
            {
                foreach (ThingDef stuff in stuffList)
                {
                    TerrainDef terrain = CopyAndStuffTerrainDef(terrainToCopy, stuff);
                    DefGenerator.AddImpliedDef(terrain);
                }
            }

            private static TerrainDef CopyAndStuffTerrainDef(TerrainDef terrainToCopy, ThingDef stuffThingDef)
            {
                TerrainDef terrain = new TerrainDef();

                terrain.modContentPack = PocketDimensionMod.Instance.Content;

                // Copy properties, everything we can think of, so that the xml can still be the main source for properties
                terrain.affordances = terrainToCopy.affordances.NullOrEmpty()
                                          ? new List<TerrainAffordanceDef>()
                                          : new List<TerrainAffordanceDef>(terrainToCopy.affordances);
                terrain.avoidWander = terrainToCopy.avoidWander;
                terrain.altitudeLayer = terrainToCopy.altitudeLayer;
                terrain.artisticSkillPrerequisite = terrainToCopy.artisticSkillPrerequisite;
                terrain.blocksAltitudes = terrainToCopy.blocksAltitudes.NullOrEmpty()
                                                    ? new List<AltitudeLayer>()
                                                    : new List<AltitudeLayer>(terrainToCopy.blocksAltitudes);
                terrain.blueprintDef = terrainToCopy.blueprintDef;
                terrain.buildingPrerequisites = terrainToCopy.buildingPrerequisites.NullOrEmpty()
                                                    ? new List<ThingDef>()
                                                    : new List<ThingDef>(terrainToCopy.buildingPrerequisites);
                terrain.burnedDef = terrainToCopy.burnedDef;
                terrain.bridge = terrainToCopy.bridge;
                terrain.canGenerateDefaultDesignator = terrainToCopy.canGenerateDefaultDesignator;
                terrain.changeable = terrainToCopy.changeable;
                terrain.clearBuildingArea = terrainToCopy.clearBuildingArea;
                //terrain.color = terrainToCopy.color;
                terrain.colorPerStuff = terrainToCopy.colorPerStuff.NullOrEmpty()
                                                    ? new List<ColorForStuff>()
                                                    : new List<ColorForStuff>(terrainToCopy.colorPerStuff);
                terrain.constructEffect = terrainToCopy.constructEffect;
                terrain.constructionSkillPrerequisite = terrainToCopy.constructionSkillPrerequisite;
                //terrain.costList = terrainToCopy.costList.NullOrEmpty()
                //                                    ? new List<ThingDefCountRange>()
                //                                    : new List<ThingDefCountRange>(terrainToCopy.costList);
                terrain.costStuffCount = terrainToCopy.costStuffCount;
                terrain.defaultPlacingRot = terrainToCopy.defaultPlacingRot;
                //terrain.designationCategory = DefDatabase<DesignationCategoryDef>.GetNamed("Floors");
                //terrain.designationHotKey = terrainToCopy.designationHotKey;
                terrain.destroyBuildingsOnDestroyed = terrainToCopy.destroyBuildingsOnDestroyed;
                terrain.destroyEffect = terrainToCopy.destroyEffect;
                terrain.destroyEffectWater = terrainToCopy.destroyEffectWater;
                terrain.destroyOnBombDamageThreshold = terrainToCopy.destroyOnBombDamageThreshold;
                //terrain.designatorDropdown = terrainToCopy.designatorDropdown;
                terrain.dominantStyleCategory = terrainToCopy.dominantStyleCategory;
                terrain.driesTo = terrainToCopy.driesTo;
                
                terrain.edgeType = terrainToCopy.edgeType;
                terrain.extinguishesFire = terrainToCopy.extinguishesFire;
                terrain.extraDeteriorationFactor = terrainToCopy.extraDeteriorationFactor;
                terrain.extraDraftedPerceivedPathCost = terrainToCopy.extraDraftedPerceivedPathCost;
                terrain.extraNonDraftedPerceivedPathCost = terrainToCopy.extraNonDraftedPerceivedPathCost;
                terrain.fertility = terrainToCopy.fertility;
                terrain.filthAcceptanceMask = terrainToCopy.filthAcceptanceMask;
                terrain.frameDef = terrainToCopy.frameDef;
                terrain.generatedFilth = terrainToCopy.generatedFilth;
                terrain.graphic = terrainToCopy.graphic;
                terrain.installBlueprintDef = terrainToCopy.installBlueprintDef;
                terrain.isAltar = terrainToCopy.isAltar;
                terrain.holdSnow = terrainToCopy.holdSnow;
                terrain.layerable = terrainToCopy.layerable;
                terrain.maxTechLevelToBuild = terrainToCopy.maxTechLevelToBuild;
                terrain.minTechLevelToBuild = terrainToCopy.minTechLevelToBuild;
                //terrain.menuHidden = terrainToCopy.menuHidden;
                terrain.passability = terrainToCopy.passability;
                terrain.pathCost = terrainToCopy.pathCost;
                terrain.pathCostIgnoreRepeat = terrainToCopy.pathCostIgnoreRepeat;
                terrain.placeWorkers = terrainToCopy.placeWorkers.NullOrEmpty()
                                           ? new List<Type>()
                                           : new List<Type>(terrainToCopy.placeWorkers);
                terrain.placingDraggableDimensions = terrainToCopy.placingDraggableDimensions;
                terrain.renderPrecedence = terrainToCopy.renderPrecedence;
                //terrain.repairEffect = terrainToCopy.repairEffect;
                terrain.researchPrerequisites = terrainToCopy.researchPrerequisites.NullOrEmpty()
                                                    ? new List<ResearchProjectDef>()
                                                    : new List<ResearchProjectDef>(terrainToCopy.researchPrerequisites);
                terrain.resourcesFractionWhenDeconstructed = terrainToCopy.resourcesFractionWhenDeconstructed;
                terrain.scatterType = terrainToCopy.scatterType;
                terrain.smoothedTerrain = terrainToCopy.smoothedTerrain;
                terrain.specialDisplayRadius = terrainToCopy.specialDisplayRadius;
                terrain.statBases = terrainToCopy.statBases.NullOrEmpty() ? new List<StatModifier>() : new List<StatModifier>(terrainToCopy.statBases);
                terrain.stuffCategories = terrainToCopy.stuffCategories.NullOrEmpty()
                                                    ? new List<StuffCategoryDef>()
                                                    : new List<StuffCategoryDef>(terrainToCopy.stuffCategories);
                //terrain.stuffUiIcons = terrainToCopy.stuffUiIcons.NullOrEmpty()
                //                                    ? new Dictionary<StuffAppearanceDef, Texture2D>()
                //                                    : new Dictionary<StuffAppearanceDef, Texture2D>(terrainToCopy.stuffUiIcons);
                terrain.tags = terrainToCopy.tags.NullOrEmpty() ? new List<string>() : new List<string>(terrainToCopy.tags);
                terrain.takeFootprints = terrainToCopy.takeFootprints;
                terrain.takeSplashes = terrainToCopy.takeSplashes;
                terrain.terrainAffordanceNeeded = terrainToCopy.terrainAffordanceNeeded;
                terrain.texturePath = terrainToCopy.texturePath;
                terrain.tools = terrainToCopy.tools.NullOrEmpty() ? new List<Tool>() : new List<Tool>(terrainToCopy.tools);
                terrain.traversedThought = terrainToCopy.traversedThought;
                // uiIcon properties needed?
                terrain.useStuffTerrainAffordance = terrainToCopy.useStuffTerrainAffordance;
                terrain.waterDepthMaterial = terrainToCopy.waterDepthMaterial;
                terrain.waterDepthShader = terrainToCopy.waterDepthShader;
                terrain.waterDepthShaderParameters = terrainToCopy.waterDepthShaderParameters.NullOrEmpty()
                    ? new List<ShaderParameter>()
                    : new List<ShaderParameter>(terrainToCopy.waterDepthShaderParameters);

                // apply stuff elements
                StuffProperties stuff = stuffThingDef.stuffProps;
                terrain.color = stuff.color;
                terrain.constructEffect = stuff.constructEffect;
                terrain.costList = new List<ThingDefCountClass>();
                terrain.description = terrainToCopy.description;
                terrain.defName = terrainToCopy.defName + "_" + stuffThingDef.defName;
                terrain.label = "ThingMadeOfStuffLabel".Translate(stuffThingDef.LabelAsStuff, terrainToCopy.label);
                terrain.repairEffect = stuff.constructEffect;
                

                //Log.Message(String.Format("Created {0} from {1}", terrain.defName, stuffThingDef.defName));

                return terrain;
            }
        }
    }
}
