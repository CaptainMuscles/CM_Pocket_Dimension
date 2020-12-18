using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_PocketDimension
{
    public class Debug_SummonTradeShip
    {
        [DebugAction(category: "Pocket Dimensions", name: "Summon Trade Ship", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SummonTradeShip()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }
            if (DefDatabase<TraderKindDef>.AllDefs.Where((TraderKindDef x) => CanSpawn(map, x)).TryRandomElementByWeight((TraderKindDef traderDef) => traderDef.CalculatedCommonality, out var result))
            {
                TradeShip tradeShip = new TradeShip(result, GetFaction(result));
                Messages.Message(new Message("Debug: Trade ship arrived: " + tradeShip.def.LabelCap, MessageTypeDefOf.PositiveEvent));
                //SendStandardLetter(tradeShip.def.LabelCap, "TraderArrival".Translate(tradeShip.name, tradeShip.def.label, (tradeShip.Faction == null) ? "TraderArrivalNoFaction".Translate() : "TraderArrivalFromFaction".Translate(tradeShip.Faction.Named("FACTION"))), LetterDefOf.PositiveEvent, parms, LookTargets.Invalid);
                map.passingShipManager.AddShip(tradeShip);
                tradeShip.GenerateThings();
            }
        }

        private static Faction GetFaction(TraderKindDef trader)
        {
            if (trader.faction == null)
            {
                return null;
            }
            if (!Find.FactionManager.AllFactions.Where((Faction f) => f.def == trader.faction).TryRandomElement(out var result))
            {
                return null;
            }
            return result;
        }

        private static bool CanSpawn(Map map, TraderKindDef trader)
        {
            if (!trader.orbital)
            {
                return false;
            }
            if (trader.faction == null)
            {
                return true;
            }
            Faction faction = GetFaction(trader);
            if (faction == null)
            {
                return false;
            }
            foreach (Pawn freeColonist in map.mapPawns.FreeColonists)
            {
                if (freeColonist.CanTradeWith(faction, trader))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
