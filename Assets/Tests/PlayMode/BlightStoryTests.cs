using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 5: the blight follows the story, known places worsen, and one neighbour serves the Queen.</summary>
    public class BlightStoryTests
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
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            QuestDb.All.Remove("test_stageflags");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static int Count(PlayerSlot p, string text) => p.Hud.LogLines.Count(l => l.Contains(text));

        static PlayerSlot ActTwo(GameRoot g, PlayerSlot p)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            return p;
        }

        // ============================================================ Task 1: the engine

        [UnityTest]
        public IEnumerator BlightFollowsTheStoryOnLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            Blight.Sync();
            Assert.AreEqual(0, g.World.BlightStage, "no blight in Act I");
            ActTwo(g, p);
            Assert.AreEqual(1, g.World.BlightStage, "Act II begins: stage 1");
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.AreEqual(2, g.World.BlightStage, "a side chosen: stage 2");
            g.Quests.Set(Answers.Id, 7, p);
            Assert.AreEqual(3, g.World.BlightStage, "on the Emberhorn Road: stage 3");

            g.Quests.Stages[Answers.Id] = Answers.Chosen;
            g.World.BlightStage = 0; // a save from before the blight was staged
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(2, g.World.BlightStage, "loading an older save catches the blight up");
        }

        [UnityTest]
        public IEnumerator EasedBlightStaysEased()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(2), "fighter", "human"));
            yield return Frames(2);
            g.Quests.Set(Answers.Id, 7, p);
            Assert.AreEqual(3, g.World.BlightStage);
            Blight.Ease();
            Assert.AreEqual(2, g.World.BlightStage);
            g.Quests.Start("glasshouse", p); // any quest change re-syncs
            Assert.AreEqual(2, g.World.BlightStage, "eased stays eased");
        }

        [UnityTest]
        public IEnumerator MinBlightGatesAGroup()
        {
            var g = GameRoot.I;
            ActTwo(g, g.DebugStart(Still(3), "fighter", "human"));
            yield return Frames(2);
            var grp = new EnemyGroupDef { Id = "test_blight_grp", MinBlight = 2 };
            Assert.AreEqual(1, g.World.BlightStage);
            Assert.IsFalse(grp.InStage(g.Quests));
            g.World.BlightStage = 2;
            Assert.IsTrue(grp.InStage(g.Quests));
        }

        [UnityTest]
        public IEnumerator StageFlagsAreSet()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            QuestDb.All["test_stageflags"] = new QuestDef { Id = "test_stageflags", Title = "T", StageText = new[] { "", "a", "b", "c" }, StageFlags = { [2] = "test_stage_flag" } };
            g.Quests.Start("test_stageflags", p);
            Assert.IsFalse(g.World.Flag("test_stage_flag"));
            g.Quests.Set("test_stageflags", 3, p);
            Assert.IsTrue(g.World.Flag("test_stage_flag"), "jumping past a stage still sets its flag");
        }

        [UnityTest]
        public IEnumerator DragonFeelsTheBlight()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(5), "fighter", "human"));
            yield return Frames(2);
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.AreEqual(2, g.World.BlightStage);
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(3);
            Assert.AreEqual(1, Count(p, "goes still, nose up"), "the dragon feels the ash");
            g.Warp(p, "tavern", WorldDb.Zones["tavern"].DefaultSpawn, false); // indoors: too little ash to bark at
            yield return Frames(2);
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(3);
            Assert.AreEqual(1, Count(p, "goes still, nose up"), "once a day per place");
        }
    }
}
