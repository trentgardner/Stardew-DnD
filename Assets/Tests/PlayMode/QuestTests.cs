using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>The data-driven quest system (Phase 4): definitions, triggers, once-only rewards, and the quests themselves.</summary>
    public class QuestTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };

        // ============================================================ Task 1: the quest system

        [UnityTest]
        public IEnumerator EveryQuestIsWellFormed()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(1), "fighter", "human");
            yield return null;
            var groups = WorldDb.Zones.Values.SelectMany(z => z.EnemyGroups).Select(x => x.Id).ToHashSet();
            Assert.GreaterOrEqual(QuestDb.All.Count, 4);
            foreach (var q in QuestDb.All.Values)
            {
                Assert.GreaterOrEqual(q.StageText.Length, 2, q.Id);
                Assert.AreEqual("", q.StageText[0], $"{q.Id}: stage 0 means 'not started' and has no text");
                Assert.IsTrue(q.Giver == null || WorldDb.Npcs.ContainsKey(q.Giver), $"{q.Id}: unknown giver {q.Giver}");
                foreach (var t in q.Triggers)
                {
                    Assert.Less(t.FromStage, t.ToStage, $"{q.Id} trigger {t.On} {t.Id}");
                    Assert.LessOrEqual(t.ToStage, q.Last, $"{q.Id} trigger {t.On} {t.Id}");
                    if (t.On == QuestEvent.GroupResolved) Assert.IsTrue(groups.Contains(t.Id), $"{q.Id}: unknown group {t.Id}");
                    if (t.On == QuestEvent.ItemGained) Assert.IsNotNull(ItemsDb.Get(t.Id), $"{q.Id}: unknown item {t.Id}");
                    if (t.On == QuestEvent.ZoneEntered) Assert.IsTrue(WorldDb.Zones.ContainsKey(t.Id), $"{q.Id}: unknown zone {t.Id}");
                }
                if (q.Reward?.Item != null) Assert.IsNotNull(ItemsDb.Get(q.Reward.Item), $"{q.Id}: unknown reward item");
                if (q.Reward?.Npc != null) Assert.IsTrue(WorldDb.Npcs.ContainsKey(q.Reward.Npc), $"{q.Id}: unknown reward npc");
            }
        }

        [UnityTest]
        public IEnumerator TriggersAdvanceQuests()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(2), "fighter", "human");
            yield return null;
            g.Quests.Notify(QuestEvent.GroupResolved, "ford_toll");
            Assert.AreEqual(0, g.Quests.Stage("east_road"), "a trigger whose FromStage doesn't match does nothing");
            g.Quests.Notify(QuestEvent.ZoneEntered, "ford");
            Assert.AreEqual(1, g.Quests.Stage("east_road"));
            g.Quests.Notify(QuestEvent.GroupResolved, "ford_toll");
            Assert.AreEqual(2, g.Quests.Stage("east_road"));
            g.Quests.Notify(QuestEvent.ItemGained, "lucky_spoon");
            Assert.AreEqual(1, g.Quests.Stage("spoon"));
        }

        [UnityTest]
        public IEnumerator QuestRewardPaysEachPlayerOnce()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(3), "fighter", "human");
            var p2 = g.DebugStart(Still(4), "cleric", "elf");
            yield return null;
            int x1 = p1.Character.XP, x2 = p2.Character.XP;
            g.Quests.Start("spoon");
            g.Quests.Set("spoon", 2, p1);
            g.Quests.Set("spoon", 2, p2);
            g.Quests.Notify(QuestEvent.ItemGained, "lucky_spoon");
            Assert.AreEqual(75, p1.Character.XP - x1, "P1 gets the quest XP exactly once");
            Assert.AreEqual(75, p2.Character.XP - x2, "P2 gets the quest XP exactly once");
            Assert.IsTrue(g.Quests.Done("spoon"));
        }

        [UnityTest]
        public IEnumerator OldSaveQuestsKeepStagesAndDontRepay()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return null;
            g.Quests.Stages["kettle"] = 2; // what an old save holds: a finished quest, no record of payment
            string path = SaveSystem.Save(false);
            g.QuitToTitle();
            yield return null;
            g.LoadGame(path);
            yield return null;
            p = g.Players[0];
            Assert.AreEqual(2, g.Quests.Stage("kettle"));
            Assert.IsTrue(g.Quests.Done("kettle"));
            Assert.IsTrue(g.Quests.Paid.Contains("kettle"), "finished quests from old saves count as paid");
            int xp = p.Character.XP;
            g.Quests.Set("kettle", 2, p);
            Assert.AreEqual(xp, p.Character.XP, "no second payout");
            StringAssert.StartsWith("Complete", QuestDb.Get("kettle").StageText[g.Quests.Stage("kettle")]);
        }
    }
}
