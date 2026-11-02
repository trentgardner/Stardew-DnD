using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act III Phase 3: the capital, Aurelhold.</summary>
    public class CapitalTests
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
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>A fresh Act III game (the academy side), screens closed.</summary>
        public static PlayerSlot ActThree(GameRoot g, int seed, string cls = "fighter", string side = "academy")
        {
            var p = g.DebugStart(Still(seed), cls, "human");
            g.ApplyActThreeStart(p, side);
            p.Hud.CloseAll();
            return p;
        }

        [UnityTest]
        public IEnumerator WarmCityStartsWithActThreeEveryWay()
        {
            var g = GameRoot.I;
            // 1. Begun after the Warden.
            var p = g.DebugStart(Still(1), "fighter", "human", level: 10);
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Factions.Commit("crown", p);
            g.World.SetFlag(GameRoot.WardenFallen);
            g.Quests.Set(Answers.Id, Answers.Rest, p);
            Assert.AreEqual(0, g.Quests.Stage(WarmCity.Id), "not before Act III");
            g.BeginActThree(p);
            Assert.AreEqual(WarmCity.Summoned, g.Quests.Stage(WarmCity.Id), "BeginActThree");
            g.QuitToTitle();
            yield return Frames(2);

            // 2. Start at Act III.
            p = g.DebugStart(Still(2), "wizard", "elf");
            yield return Frames(2);
            g.ApplyActThreeStart(p, "fae");
            p.Hud.CloseAll();
            Assert.AreEqual(WarmCity.Summoned, g.Quests.Stage(WarmCity.Id), "Start at Act III");

            // 3. A Phase 2 Act III save that lacks the quest.
            g.Quests.Stages.Remove(WarmCity.Id);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(WarmCity.Summoned, g.Quests.Stage(WarmCity.Id), "loading an Act III save without it");

            // 4. Never restarts.
            p = g.Players[0];
            g.Quests.Set(WarmCity.Id, WarmCity.Audience, p);
            path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(WarmCity.Audience, g.Quests.Stage(WarmCity.Id), "it holds its stage across a reload");
        }

        [UnityTest]
        public IEnumerator CapitalCharsBuild()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            var errors = new List<string>();
            void Log(string msg, string st, LogType t) { if (t == LogType.Error || t == LogType.Exception) errors.Add(msg); }
            Application.logMessageReceived += Log;
            var def = new ZoneDef { Id = "test_capital_chars", Name = "Test", DefaultSpawn = new Vector2Int(3, 1), Map = new[] { "XXXXXX", "X78..X", "XXXXXX" } };
            Zone z;
            try { z = Zone.Build(def, 92, new GameObject("test_root").transform); }
            finally { Application.logMessageReceived -= Log; }
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.IsFalse(z.IsWalkable(new Vector2Int(1, 1)), "the statue blocks");
            Assert.IsFalse(z.IsWalkable(new Vector2Int(2, 1)), "the lamp blocks");
            Assert.IsTrue(z.IsWalkable(new Vector2Int(3, 1)));
            Assert.IsTrue(z.GetComponentsInChildren<SignInteractable>(true).Any(s => s.Title == "The Victory of the Crown"), "the statue can be read");
            var lamp = z.GetComponentsInChildren<LightSource>(true).Single();
            Assert.IsTrue(lamp.On, "the ever-lamp is lit");
            Assert.Greater(lamp.Tint.r, lamp.Tint.b, "warm amber, not violet");
            Assert.IsNotNull(lamp.GetComponentInChildren<ArcaneHalo>(), "and its flame shows by day");
            Object.Destroy(z.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator DragonStaysTextIsUsed()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 4);
            yield return Frames(2);
            var hall = WorldDb.Zones["sealing_hall"];
            Assert.IsTrue(hall.DragonStays);
            hall.DragonStaysText = "{name} waits by the test door.";
            try
            {
                g.Warp(p, "sealing_hall", hall.DefaultSpawn, false);
                yield return Frames(2);
                Assert.IsTrue(p.Hud.LogLines.Any(l => l.Contains(g.World.DragonName + " waits by the test door.")), "the zone's own line");
                Assert.AreNotEqual("sealing_hall", g.DragonAgent.Actor.Zone.Def.Id, "the dragon stayed out");
            }
            finally { hall.DragonStaysText = null; }
        }
    }
}
