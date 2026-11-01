using System.Collections.Generic;
using System.Linq;
using DnD.Rules.Content;

namespace DnD.Game
{
    /// <summary>
    /// The XP one player can earn in Act II, counted from the data (quests, places, secrets, fights, lore). Keeps the pacing
    /// honest: level 9 on the main path by the Emberhorn's gate, level 10 when the Warden falls (spec: "XP pacing to land level
    /// 10 around the finale"). Peaceful endings pay the same as fights, so each group counts once. You can only take one side's
    /// major quest, so only the richest one counts.
    /// </summary>
    public static class ActTwoXp
    {
        public const int PerPlace = 15;
        /// <summary>The places the main story walks you through.</summary>
        static readonly string[] MainZones = { "lantern_road", "academy", "ashby_tower", "emberhorn_road", "emberhorn_throat", "emberhorn_heart", "cragfoot" };
        /// <summary>The groups on the main route (besides the chosen major's own).</summary>
        static readonly string[] MainRouteZones = { "lantern_road", "emberhorn_road", "emberhorn_throat" };
        /// <summary>Each force's major quest and the groups it sends you through.</summary>
        static readonly Dictionary<string, string[]> MajorGroups = new Dictionary<string, string[]>
        {
            ["crowns_banner"] = new[] { "beacon_ashsworn" },
            ["ninth_circle"] = new[] { "circle_bear" },
            ["green_bargain"] = new[] { "frost_thorns" },
            ["long_hunt"] = new[] { "ashmaw_den" },
        };

        static int GroupXp(EnemyGroupDef g, int players) => GameRoot.EncounterShare(g.Members.Sum(m => MonstersDb.Get(m.MonsterId)?.XP ?? 0), players);
        static int QuestXp(QuestDef q, bool includeWarden) =>
            q.StageXp.Where(kv => includeWarden || q.Id != Answers.Id || kv.Key < Answers.Rest).Sum(kv => kv.Value) + (q.Reward?.Xp ?? 0);

        public static int Available(bool includeSide, int players, bool includeWarden)
        {
            var zones = WorldDb.Zones.Values.Where(z => z.Act >= 2).ToList();
            var groups = zones.SelectMany(z => z.EnemyGroups).ToDictionary(g => g.Id);
            int xp = 0;

            // Places.
            xp += zones.Count(z => includeSide || MainZones.Contains(z.Id)) * PerPlace;

            // Fights: the main route always; side places only for completionists; the Warden only after the gate.
            var majorGroupIds = MajorGroups.Values.SelectMany(x => x).ToList();
            foreach (var z in zones)
                foreach (var g in z.EnemyGroups)
                {
                    if (g.Id == GameRoot.WardenGroup) { if (includeWarden) xp += GroupXp(g, players); continue; }
                    if (majorGroupIds.Contains(g.Id)) continue; // counted with its major below
                    if (includeSide || MainRouteZones.Contains(z.Id)) xp += GroupXp(g, players);
                }
            // The blight's beasts in Act I places come in Act II too.
            if (includeSide)
                foreach (var g in WorldDb.Zones.Values.Where(z => z.Act < 2).SelectMany(z => z.EnemyGroups).Where(g => g.MinBlight > 0)) xp += GroupXp(g, players);

            // Quests: Answers; one side's scouting (main) or every side's (side); one major (the richest, with its fights).
            var act2 = QuestDb.All.Values.Where(q => q.Act >= 2 && !q.Id.StartsWith("test_")).ToList();
            xp += act2.Where(q => q.Id == Answers.Id).Sum(q => QuestXp(q, includeWarden));
            var scouting = act2.Where(q => q.Faction != null && !q.Major).GroupBy(q => q.Faction).ToDictionary(g => g.Key, g => g.Sum(q => QuestXp(q, true)));
            xp += includeSide ? scouting.Values.Sum() : scouting.Values.DefaultIfEmpty(0).Max();
            xp += act2.Where(q => q.Major).Select(q => QuestXp(q, true) + MajorGroups[q.Id].Where(groups.ContainsKey).Sum(id => GroupXp(groups[id], players))).DefaultIfEmpty(0).Max();
            if (includeSide)
            {
                xp += act2.Where(q => q.Faction == null && q.Id != Answers.Id).Sum(q => QuestXp(q, true)); // professors, Ash on the Doors
                foreach (var z in zones) foreach (var s in z.Secrets.Values) if (!s.IsQuestSpot) xp += s.DC * 2;
                xp += LoreDb.All.Values.Count(l => l.MinAct >= 2) * LoreDb.FindXp;
            }
            return xp;
        }
    }
}
