using System.Collections;
using System.Linq;
using System.IO;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Phase 7: Act I as a whole (XP pacing, the ending, the golden path).</summary>
    public class ActOneTests
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

        // ============================================================ Task 1: XP pacing

        const int Level3 = 900, Level4 = 2700;
        /// <summary>Doing literally everything before the hatching: side content can also wait until after it, so allow 2x level 4.</summary>
        const int MaxAll = 2 * Level4;

        [Test]
        public void ActOneXpBandHoldsForSolo()
        {
            int main = ActOneXp.Available(false, 1), all = ActOneXp.Available(true, 1);
            Debug.Log($"Act I XP before the hatching, solo: main path {main}, everything {all}");
            Assert.GreaterOrEqual(main, Level3, "the main story alone gets you to level 3 by the egg");
            Assert.GreaterOrEqual(all, Level4, "doing everything gets you to level 4 by the hatching");
            Assert.LessOrEqual(all, MaxAll, "not so much that a completionist outlevels the act");
        }

        [Test]
        public void ActOneXpBandHoldsForTwoPlayers()
        {
            int main = ActOneXp.Available(false, 2), all = ActOneXp.Available(true, 2);
            Debug.Log($"Act I XP before the hatching, per player in co-op: main path {main}, everything {all}");
            Assert.GreaterOrEqual(main, Level3);
            Assert.GreaterOrEqual(all, Level4);
            Assert.LessOrEqual(all, MaxAll);
        }

        [UnityTest]
        public IEnumerator MainQuestMilestonesPayOnce()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(2), "fighter", "human");
            var p2 = g.DebugStart(Still(3), "cleric", "elf");
            yield return null;
            g.Quests.Set("fallen_star", FallenStar.Crater, p1);
            int a = p1.Character.XP, b = p2.Character.XP;
            g.Quests.Set("fallen_star", FallenStar.Egg, p1); // straight to the egg: every milestone on the way pays
            int milestones = QuestDb.Get("fallen_star").StageXp.Values.Sum();
            Assert.AreEqual(300, milestones, "trail, warrens and egg: 100 each");
            Assert.AreEqual(a + milestones, p1.Character.XP);
            Assert.AreEqual(b + milestones, p2.Character.XP, "each player gets the milestone, like quest rewards");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return null;
            var q1 = g.Players[0];
            int after = q1.Character.XP;
            g.Quests.Set("fallen_star", FallenStar.Egg, q1);
            g.Quests.Set("fallen_star", FallenStar.Warrens, q1);
            Assert.AreEqual(after, q1.Character.XP, "never paid twice, even after a reload");
        }

        [UnityTest]
        public IEnumerator NoLevelFiveInActOne()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return null;
            g.GiveXP(p, 9000, "test");
            while (p.Character.CanLevelUp) p.Character.LevelUp(p.Character.AutoPlan(), new System.Collections.Generic.List<string>());
            Assert.AreEqual(4, p.Character.Level, "Act I caps at level 4");
            Assert.IsFalse(p.Character.CanLevelUp);
            Assert.GreaterOrEqual(p.Character.XP, 9000, "the XP is kept for later");
        }
    }
}
