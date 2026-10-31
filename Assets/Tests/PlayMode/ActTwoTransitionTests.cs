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
    /// <summary>Act II Phase 2: moving into Act II (a season passes) and starting there.</summary>
    public class ActTwoTransitionTests
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
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator BeginActTwoSkipsASeasonAndRaisesTheCap()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            int day = g.Clock.Day;
            g.BeginActTwo(p);
            yield return Frames(2);
            Assert.AreEqual(2, g.World.Act);
            Assert.AreEqual(day + 28, g.Clock.Day, "a season passes");
            Assert.AreEqual(day + 28, g.World.ActTwoDay);
            Assert.AreEqual(PlayerCharacter.ActTwoLevelCap, PlayerCharacter.LevelCap);
            Assert.IsTrue(p.Hud.HasScreen, "the 'A Season Passes' card is shown");
        }

        [UnityTest]
        public IEnumerator CompanionsCatchUpWhenTheCapRises()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "human");
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "dwarf", "Bram"));
            g.SpawnCompanion(p, pc, "hums");
            yield return Frames(2);
            g.GiveXP(p, 14000, "test");
            Assert.AreEqual(4, pc.Level, "Act I: the companion stops at 4 too");
            g.BeginActTwo(p);
            Assert.AreEqual(6, pc.Level, "Act II: banked XP takes them to 6");
        }

        [UnityTest]
        public IEnumerator ActTwoSurvivesSaveAndLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            g.BeginActTwo(p);
            p.Hud.CloseAll();
            yield return Frames(2);
            string path = SaveSystem.Save(false);
            g.QuitToTitle();
            yield return Frames(2);
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(2, g.World.Act);
            Assert.AreEqual(PlayerCharacter.ActTwoLevelCap, PlayerCharacter.LevelCap, "an Act II save loads with the Act II cap");
        }

        [UnityTest]
        public IEnumerator ActOneAfterActTwoIsCappedAtFour()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            g.BeginActTwo(p);
            g.QuitToTitle();
            yield return Frames(2);
            var q = g.DebugStart(Still(5), "rogue", "human");
            yield return Frames(2);
            Assert.AreEqual(1, g.World.Act);
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap);
        }

        [UnityTest]
        public IEnumerator OldSaveLoadsAsActOne()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human");
            yield return Frames(2);
            string path = SaveSystem.Save(false);
            var lines = File.ReadAllLines(path).Where(l => !l.Contains("\"Act\"") && !l.Contains("\"ActTwoDay\"")).Select(l => l.Replace("\"Version\": 3", "\"Version\": 2"));
            File.WriteAllLines(path, lines);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(1, g.World.Act, "a save from before Act II is Act I");
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap);
        }

        [UnityTest]
        public IEnumerator StartAtActTwoHasAWorkingWorld()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(7), "ranger", "elf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            yield return Frames(3);
            Assert.AreEqual(2, g.World.Act);
            Assert.IsTrue(g.Quests.Done("fallen_star"), "the Act I story counts as told");
            Assert.IsTrue(g.World.EggHatched);
            Assert.IsNotNull(g.Dragon, "there's a dragon");
            Assert.AreEqual(DragonStage.Wyrmling, g.Dragon.Stage, "a teenage one");
            Assert.IsTrue(p.Character.CanLevelUp, "level up to 5 with real choices");
            Assert.GreaterOrEqual(p.Character.CountOf("potion_healing"), 2, "a starter kit");
            g.Warp(p, "temple", new Vector2Int(5, 5), false);
            yield return Frames(3);
            Assert.AreEqual("temple", g.DragonAgent.Actor.Zone.Def.Id, "waking at the temple, the dragon is there too");
        }

        [UnityTest]
        public IEnumerator SecondPlayerJoinsAtLevelFive()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(8), "fighter", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            var p2 = g.DebugStart(Still(9), "wizard", "gnome");
            yield return Frames(2);
            Assert.GreaterOrEqual(p2.Character.XP, PlayerCharacter.XpThresholds[5], "a partner joining Act II starts with level-5 XP");
            Assert.GreaterOrEqual(p2.Character.CountOf("potion_healing"), 2, "and the same kit");
        }
    }
}
