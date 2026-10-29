using System.Collections;
using System.IO;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    public class AudioTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f; // tests run silently
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

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator ZoneChangeSwitchesMusic()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            yield return Frames(2);
            Assert.AreEqual(WorldDb.Zones["home"].MusicId, Music.Current, "home has its own tune");
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(2);
            Assert.AreEqual("music_forest", Music.Current);
            Assert.IsTrue(Music.HasRealClip("music_forest"), "the forest track ships in Resources/Audio");
        }

        [UnityTest]
        public IEnumerator CombatPlaysFightMusicThenReturns()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(2);
            g.StartGroupCombat("forest_wolves", p);
            yield return Frames(2);
            Assert.AreEqual("music_fight", Music.Current);
            var fight = p.Combat;
            fight.Finished = true;
            g.OnCombatFinished(fight, true);
            yield return Frames(2);
            Assert.AreEqual("music_forest", Music.Current, "back to the zone's tune after the fight");
        }

        [UnityTest]
        public IEnumerator MutedTestsStayMuted()
        {
            var g = GameRoot.I;
            g.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            yield return Frames(5);
            Assert.AreEqual(0f, AudioListener.volume, "music never unmutes a test run");
        }
    }
}
