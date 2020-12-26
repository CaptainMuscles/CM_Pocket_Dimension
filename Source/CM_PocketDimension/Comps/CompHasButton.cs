using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CM_PocketDimension
{
    [StaticConstructorOnStartup]
    public class CompHasButton : ThingComp
    {
        public CompProperties_HasButton Props => (CompProperties_HasButton)props;

        private bool isActive = true;
        private bool wantBeOn = true;

        public bool Active => isActive;
        public bool WantsPress => (wantBeOn != isActive);

        private Texture2D cachedCommandTex;
        
        private Texture2D CommandTex
        {
            get
            {
                if (cachedCommandTex == null)
                {
                    cachedCommandTex = ContentFinder<Texture2D>.Get(Props.commandTexture);
                }
                return cachedCommandTex;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<bool>(ref this.isActive, "isActive", true);
            Scribe_Values.Look<bool>(ref this.wantBeOn, "wantBeOn", true);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (WantsPress)
                DesignateForFlicking();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (parent.Faction == Faction.OfPlayer)
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                //command_Toggle.hotKey = KeyBindingDefOf.Command_TogglePower;
                command_Toggle.icon = CommandTex;
                command_Toggle.defaultLabel = Props.commandLabelKey.Translate();
                command_Toggle.defaultDesc = Props.commandDescKey.Translate();
                command_Toggle.isActive = () => wantBeOn;
                command_Toggle.toggleAction = delegate
                {
                    wantBeOn = !wantBeOn;
                    UpdateDesignation();
                };
                yield return command_Toggle;
            }
        }

        private void UpdateDesignation()
        {
            if (WantsPress)
                DesignateForFlicking();
            else
                UndesignateForFlicking();
        }

        private void DesignateForFlicking()
        {
            if (parent.Map?.designationManager.DesignationOn(parent, PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton) == null)
                parent.Map.designationManager.AddDesignation(new Designation(parent, PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton));
        }

        private void UndesignateForFlicking()
        {
            parent.MapHeld.designationManager.DesignationOn(parent, PocketDimensionDefOf.CM_PocketDimension_Designation_PressButton)?.Delete();
        }

        public void SetActiveState(bool active)
        {
            isActive = active;
            wantBeOn = active;
            UpdateDesignation();
        }

        public void DoPress()
        {
            bool turnedOff = isActive;

            isActive = !isActive;
            wantBeOn = isActive;

            UpdateDesignation();

            SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            if (turnedOff)
                parent.BroadcastCompSignal("CM_PocketDimension_ButtonPressed_Off");
            else
                parent.BroadcastCompSignal("CM_PocketDimension_ButtonPressed_On");

            if (!string.IsNullOrEmpty(Props.extraPressSignal))
                parent.BroadcastCompSignal(Props.extraPressSignal);
        }
    }
}
