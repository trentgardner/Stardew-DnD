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
            // ============================================================ Task 2: the ending and narrator moments

        static IEnumerator WaitFor(System.Func<bool> cond, int frames = 300) { for (int i = 0; i < frames && !cond(); i++) yield return null; }

        [UnityTest]
        public IEnumerator ActEndCardShowsOncePerPlaythrough()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(10), "fighter", "human");
            var p2 = g.DebugStart(Still(11), "cleric", "elf");
            yield return null;
            g.World.EggHatched = true;
            g.Quests.Set("fallen_star", FallenStar.Hatched, p1);
            p1.Hud.CloseAll(); p2.Hud.CloseAll();
            g.CompleteFallenStar(p1);
            yield return WaitFor(() => p1.Hud.Top is ActEndScreen && p2.Hud.Top is ActEndScreen);
            Assert.IsTrue(p1.Hud.Top is ActEndScreen, "the act ends with a card");
            Assert.IsTrue(p2.Hud.Top is ActEndScreen, "both players see it");
            StringAssert.Contains("End of Act I", ((ActEndScreen)p1.Hud.Top).Text);
            p1.Hud.CloseAll(); p2.Hud.CloseAll();
            yield return Frames(5);
            Assert.IsFalse(p1.Hud.Top is ActEndScreen, "once is enough");

            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(10);
            foreach (var p in g.Players) Assert.IsFalse(p.Hud.Top is ActEndScreen, "not again after a reload");
        }

        [UnityTest]
        public IEnumerator FirstNightfallIsNarratedOnce()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(12), "fighter", "human");
            yield return null;
            g.Clock.Minutes = 19 * 60 + 58;
            g.Clock.Advance(3);
            yield return null;
            Assert.AreEqual(1, p.Hud.LogLines.Count(l => l.Contains("lanterns")), "the first evening gets a line from the narrator");
            g.Clock.Advance(24 * 60);
            yield return null;
            Assert.AreEqual(1, p.Hud.LogLines.Count(l => l.Contains("lanterns")), "only the first");
        }

        [UnityTest]
        public IEnumerator FirstLevelUpIsNarrated()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(13), "fighter", "human");
            yield return null;
            g.GiveXP(p, 300, "test");
            Assert.IsTrue(p.Hud.LogLines.Any(l => l.Contains("clicks into place")), "the narrator points at the level-up");
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
    }
}
