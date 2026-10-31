using System.Collections;
using System.IO;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 1: levels 5-10 in the running game.</summary>
    public class ActTwoLevelTests
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

        [UnityTest]
        public IEnumerator SecondFightingStyleSurvivesASaveAndTheCapResetsOnLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human", level: 7);
            yield return null;
            var pc = p.Character;
            Assert.AreEqual(7, pc.Level, "debug start can preview Act II levels");
            Assert.IsNotNull(pc.FightingStyle2, "champion 7 picked an additional fighting style");
            Assert.AreNotEqual(pc.FightingStyle, pc.FightingStyle2);
            string style2 = pc.FightingStyle2;
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return null;
            Assert.AreEqual(style2, g.Players[0].Character.FightingStyle2, "the second style is saved");
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap, "loading (an Act I save) puts the cap back to 4");
        }
    }
}
