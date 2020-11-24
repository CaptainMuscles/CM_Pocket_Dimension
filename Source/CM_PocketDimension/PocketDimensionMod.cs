using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class PocketDimensionMod : Mod
    {
        private static PocketDimensionMod _instance;
        public static PocketDimensionMod Instance => _instance;

        public PocketDimensionMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("CM_PocketDimension");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
