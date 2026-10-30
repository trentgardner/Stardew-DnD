using System.Linq;
using DnD.Rules.Content;

namespace DnD.Game
{
    /// <summary>
    /// The XP one player can earn in Act I before the egg hatches, counted from the data (quests, places, secrets, fights).
    /// Used to keep the pacing honest: level 4 should land around the hatching (spec Goal 7).
    /// Peaceful endings pay the same as fights, so each group is counted once whichever way it's settled.
    /// </summary>
    public static class ActOneXp
    {
        /// <summary>The places the main story walks you through.</summary>
        static readonly string[] MainZones = { "town", "crater", "mine", "warrens", "egg_shrine" };
        public const int Hatching = 100, PerPlace = 15;
        /// <summary>The main story's quests: the Fallen Star and Snik's go-between thread.</summary>
        static readonly string[] MainQuests = { "fallen_star", "go_between" };
        static int HatchedStage(QuestDef q) => q.Id == "fallen_star" ? FallenStar.Hatched : int.MaxValue;

        public static int Available(bool includeSide, int players)
        {
            int xp = Hatching;
            foreach (var z in WorldDb.Zones.Values)
            {
                bool main = MainZones.Contains(z.Id);
                if (!includeSide && !main) continue;
                xp += PerPlace;
                foreach (var grp in z.EnemyGroups)
                {
                    if (grp.Brawl) continue; // the bar brawl pays through its quest
                    int groupXp = grp.Members.Sum(m => MonstersDb.Get(m.MonsterId)?.XP ?? 0);
                    xp += GameRoot.EncounterShare(groupXp, players);
                }
                if (includeSide)
                    foreach (var s in z.Secrets.Values)
                        if (!s.IsQuestSpot) xp += s.DC * 2;
            }
            foreach (var q in QuestDb.All.Values)
            {
                bool mainQuest = MainQuests.Contains(q.Id);
                if (!includeSide && !mainQuest) continue;
                foreach (var kv in q.StageXp) if (kv.Key <= HatchedStage(q)) xp += kv.Value; // milestones before the hatching
                if (q.Id != "fallen_star" && q.Reward != null) xp += q.Reward.Xp; // the Fallen Star's own reward comes after
            }
            return xp;
        }
    }
}
