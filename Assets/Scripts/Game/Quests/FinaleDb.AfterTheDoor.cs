using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 6: the finales. EXEMPT from the secret guard: nothing here is reachable before WorldDb.MetHer and the
    /// choice at the turn (Turn.Chosen). Flag names and ids.
    /// </summary>
    public static class Finale
    {
        /// <summary>Every finale fight's group id starts with this (allies only join these).</summary>
        public const string Prefix = "finale_";
        public static bool IsFinaleGroup(string groupId) => groupId != null && groupId.StartsWith(Prefix);
    }

    /// <summary>The finales' tables, built from the ending and Act II state (TurnDb's reactions).</summary>
    public static class FinaleDb
    {
        static readonly Dictionary<string, (string monsterId, string name)> AllyOf = new Dictionary<string, (string, string)>
        {
            ["crown"] = ("crown_knight", "Crown Knight"),
            ["academy"] = ("academy_warden", "Ashby's Ward-Mage"),
            ["fae"] = ("fae_knight", "Knight of the Courts"),
            ["hunters"] = ("wyrmwatch_hunter", "Wyrmwatch Hunter"),
        };

        public const int MaxAllies = 3;

        /// <summary>One ally per force that stands With you (Turn.Forces order), plus Marsh's Household Guard if she defects. At most 3.</summary>
        public static List<(string monsterId, string name)> Allies(string ending, WorldState w)
        {
            var list = new List<(string, string)>();
            if (ending == null || w == null) return list;
            if (TurnDb.MarshDefects(ending, w)) list.Add(("crown_knight", "Household Guard"));
            foreach (var f in Turn.Forces)
                if (TurnDb.Reaction(f, ending, w) == Stance.With) list.Add(AllyOf[f]);
            return list.Take(MaxAllies).ToList();
        }
    }
}
