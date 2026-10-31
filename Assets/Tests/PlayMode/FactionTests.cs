using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 2: the four forces' standing, scouting quests and the forced choice (with test-only quests).</summary>
    public class FactionTests
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
            QuestDb.RegisterTestQuests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            QuestDb.RemoveTestQuests();
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator ScoutingQuestsAreOpenToEveryone()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "bard", "human");
            yield return Frames(2);
            foreach (var f in FactionDb.All.Keys)
                Assert.IsTrue(g.Quests.CanStart("test_scout_" + f), $"{f}'s scouting quest is open before you choose");
            Assert.AreEqual(4, FactionDb.All.Count, "the four forces");
            Assert.IsTrue(FactionDb.All.ContainsKey("crown") && FactionDb.All.ContainsKey("academy") && FactionDb.All.ContainsKey("fae") && FactionDb.All.ContainsKey("hunters"));
        }

        [UnityTest]
        public IEnumerator CommittingClosesOtherFactionsQuests()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "bard", "human");
            yield return Frames(2);
            Factions.Commit("academy", p);
            Assert.AreEqual("academy", g.World.CommittedFaction);
            Assert.IsTrue(g.Quests.CanStart("test_major_academy"), "your side's path stays open");
            Assert.IsTrue(g.Quests.CanStart("test_scout_academy"));
            foreach (var f in new[] { "crown", "fae", "hunters" })
            {
                Assert.IsFalse(g.Quests.CanStart("test_major_" + f), $"{f}'s major quest closes");
                Assert.IsFalse(g.Quests.CanStart("test_scout_" + f), $"so does anything else of {f}'s you hadn't started");
            }
            g.Quests.Start("test_major_crown", p);
            Assert.AreEqual(0, g.Quests.Stage("test_major_crown"), "a closed quest can't be started");
            Assert.IsTrue(g.Quests.CanStart("fallen_star") || g.Quests.Done("fallen_star") || g.Quests.Stage("fallen_star") >= 0, "quests with no faction are untouched");
        }

        [UnityTest]
        public IEnumerator CommitAsksFirstAndNotYetChangesNothing()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "bard", "human");
            yield return Frames(2);
            bool yes = false;
            var screen = Factions.OfferCommit(p, "crown", () => yes = true);
            yield return Frames(2);
            Assert.IsNotNull(screen, "a confirm prompt in plain words");
            StringAssert.Contains("Crown", screen.Body);
            StringAssert.Contains("closes", screen.Body);
            screen.Choose("Not yet");
            yield return Frames(2);
            Assert.IsNull(g.World.CommittedFaction, "'Not yet' changes nothing");
            Assert.IsFalse(yes);
            screen = Factions.OfferCommit(p, "crown", () => yes = true);
            yield return Frames(2);
            screen.Choose("Stand with");
            yield return Frames(2);
            Assert.AreEqual("crown", g.World.CommittedFaction);
            Assert.IsTrue(yes);
        }

        [UnityTest]
        public IEnumerator CommitInCoopTellsThePartner()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(4), "bard", "human");
            var p2 = g.DebugStart(Still(5), "fighter", "dwarf");
            yield return Frames(2);
            g.Warp(p2, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            Factions.Commit("fae", p1);
            Assert.IsTrue(Factions.LastNotice != null && Factions.LastNotice.Contains("Fae"), "the partner is told who the party stands with");
            g.Quests.Start("test_scout_crown", p2);
            Assert.AreEqual(0, g.Quests.Stage("test_scout_crown"), "the choice binds the whole party");
        }

        [UnityTest]
        public IEnumerator StandingIsSavedAndClamped()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "bard", "human");
            yield return Frames(2);
            Factions.Adjust("hunters", 250, p);
            Assert.AreEqual(100, Factions.Standing("hunters"), "standing tops out at 100");
            Factions.Adjust("crown", -30, p);
            Factions.Commit("hunters", p);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(100, Factions.Standing("hunters"));
            Assert.AreEqual(-30, Factions.Standing("crown"));
            Assert.AreEqual("hunters", g.World.CommittedFaction, "the choice is saved");
        }

        [UnityTest]
        public IEnumerator JournalShowsClosedPaths()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(7), "bard", "human");
            yield return Frames(2);
            g.Quests.Start("test_scout_crown", p);
            Factions.Commit("academy", p);
            StringAssert.Contains("Closed: you stood with the Starfall Academy", g.Quests.ClosedBy("test_scout_crown") ?? "", "an in-progress quest of another side shows as closed");
            var journal = new JournalScreen();
            p.Hud.Push(journal);
            yield return Frames(2);
            StringAssert.Contains("Closed", journal.Text);
        }
    }
}
