using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        /// <summary>The fight through the streets: three stages, in order.</summary>
        public const int Stages = 3;
        public static string StreetGroup(int n) => Prefix + "street_" + n;
        public static string StreetOpen(int n) => Prefix + "street_open_" + n;
        public static string StreetDone(int n) => Prefix + "street_done_" + n;
        /// <summary>The break's beats have played and the fight for the city has begun.</summary>
        public const string Started = "finale_started";
        /// <summary>End her: the dragon won't fight for it, and waits at the top of the stair down until the flight.</summary>
        public const string DragonWaits = "finale_dragon_waits";

        /// <summary>1..3 = the first stage not yet won (open or about to be); 4 = all won; 0 = the fight hasn't begun.</summary>
        public static int CurrentStreet(WorldState w)
        {
            if (w == null || !w.Flag(Started)) return 0;
            for (int n = 1; n <= Stages; n++) if (!w.Flag(StreetDone(n))) return n;
            return Stages + 1;
        }
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

        // ------------------------------------------------------------------ the streets

        static readonly Vector2Int[] Spots = { new Vector2Int(31, 19), new Vector2Int(52, 25), new Vector2Int(55, 30) };
        static readonly Vector2Int[] Retries = { new Vector2Int(31, 15), new Vector2Int(44, 24), new Vector2Int(49, 28) };
        static readonly Vector2Int[] Spread = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1) };
        static readonly string[] Titles = { "The Great Square", "The Wyvern Yard", "The Head of the Roots Stair" };

        /// <summary>The top of the stair down to the Roots, where the dragon lies on the End path.</summary>
        public static readonly Vector2Int DragonWaitSpot = new Vector2Int(58, 29);

        public static Vector2Int StreetSpot(int n) => Spots[n - 1];
        public static Vector2Int RetrySpot(int n) => Retries[Mathf.Clamp(n, 1, Finale.Stages) - 1];
        public static string StreetTitle(int n) => Titles[n - 1];

        /// <summary>Who stands in the way at stage n (at most 6, one per tile around StreetSpot).</summary>
        public static List<EnemySpawn> StreetMembers(int n, string ending, WorldState w)
        {
            var ids = new List<string>();
            if (ending == Turn.End)
                ids.AddRange(n == 1 ? new[] { "cinder_hound", "cinder_hound" } : n == 2 ? new[] { "fire_elemental", "cinder_hound", "cinder_hound" } : new[] { "fire_elemental", "fire_elemental", "salamander" });
            else
            {
                // The guards who stayed with the King.
                var core = new List<string>(n == 1 ? new[] { "crown_knight", "crown_knight" } : n == 2 ? new[] { "crown_knight", "crown_knight", "crown_battlemage" } : new[] { "crown_champion", "crown_knight", "crown_knight" });
                if (TurnDb.MarshDefects(ending, w)) core.Remove("crown_knight");
                ids.AddRange(core);
            }
            bool Against(string f) => TurnDb.Reaction(f, ending, w) == Stance.Against;
            if (Against("academy")) ids.Add(n == 1 ? "academy_warden" : n == 2 ? "stone_guardian" : null);
            if (Against("fae")) ids.Add(n == 1 ? "fae_knight" : n == 3 ? "thorn_guardian" : null);
            if (Against("hunters") && n == 2) { ids.Add("wyrmwatch_hunter"); ids.Add("wyrmwatch_hunter"); }
            var at = StreetSpot(n);
            return ids.Where(i => i != null).Take(Spread.Length).Select((id, i) => new EnemySpawn(id, at.x + Spread[i].x, at.y + Spread[i].y)).ToList();
        }

        /// <summary>The beats before each stage's fight: the city, then who's waiting.</summary>
        public static string[] StreetIntro(int n, string ending, WorldState w)
        {
            string name = w != null && w.EggHatched ? w.DragonName : "the dragon";
            if (ending == Turn.End)
                return n == 1 ? new[]
                {
                    "Every lamp in Aurelhold is out. For the first time in a thousand years the city is dark at noon, and very, very cold.",
                    "Then the paving of the Great Square cracks, and things made of fire come up through it like water through a broken dam. They aren't her. They're what was poured into her, looking for a way out.",
                }
                : n == 2 ? new[]
                {
                    "The Wyvern Yard is full of smoke. Marigold Penn is throwing buckets of ale at a burning hound and swearing like a sailor.",
                    "More fire is coming up from the Roots. Whatever is under the city, it is getting bigger, and it is getting closer to the surface.",
                }
                : new[]
                {
                    $"At the head of the Roots stair the heat is like a forge door. {Cap(name)} is still lying across the top step, and won't look at you.",
                    "The stair is pouring fire. Hold it here, and whatever is coming will have to come up somewhere else.",
                };
            if (n == 1) return new[]
            {
                "Steel comes out all over the Great Square. The Household Guard closes ranks on the Sunstair, and the King walks back into his palace without looking round.",
                "\"Hold them here,\" a captain shouts. \"By order of the King. Nobody leaves the square.\"",
            };
            if (n == 2) return new[]
            {
                "The Wyvern Yard. Somebody has dragged the Pageant's scenery across the lane as a barricade: a paper Queen, wings and all, lying on her side.",
                "Behind it, more of the King's soldiers, and they've had time to get ready.",
            };
            return new[]
            {
                "The head of the Roots stair. The way down is the only way that matters now, and the King knows it.",
                "His champion is standing on the top step, with the last of the guard behind her.",
            };
        }

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
