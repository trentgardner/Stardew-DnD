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

        // ============================================================ Task 2: the carriage and the upper city

        [UnityTest]
        public IEnumerator CarriageTakesEveryoneBothWays()
        {
            var g = GameRoot.I;
            var p1 = ActThree(g, 10);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(11), "rogue", "elf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            g.Warp(p1, "town", WorldDb.TownCarriageStop, false);
            g.Warp(p2, "forest", new Vector2Int(40, 18), false);
            yield return Frames(2);
            p2.Hud.Push(new JournalScreen());
            yield return Frames(1);
            g.RideCarriage(p1, "capital", WorldDb.CapitalCarriageStop);
            yield return Frames(3);
            foreach (var p in new[] { p1, p2 })
            {
                Assert.AreEqual("capital", p.Zone.Def.Id, $"P{p.Index + 1} rode to the capital");
                Assert.IsNull(p.Hud.Top as JournalScreen, $"P{p.Index + 1}'s screens closed for the ride");
            }
            Assert.AreNotEqual(p1.MapPos, p2.MapPos, "set down side by side, not on top of each other");
            Assert.AreEqual("capital", g.DragonAgent.Actor.Zone.Def.Id, "the dragon walks alongside");
            foreach (var c in g.Companions.Where(c => c != null && !c.Waiting)) Assert.AreEqual("capital", c.Actor.Zone.Def.Id, "companions come too");
            Assert.AreEqual(WarmCity.Arrived, g.Quests.Stage(WarmCity.Id), "arriving moves The Warm City on");
            Assert.AreEqual("capital", WorldDb.Current(WorldDb.Npcs["quell"], 12, 0, g.World).Zone, "Quell came with the carriage");

            g.RideCarriage(p2, "town", WorldDb.TownCarriageStop);
            yield return Frames(3);
            foreach (var p in new[] { p1, p2 }) Assert.AreEqual("town", p.Zone.Def.Id, $"P{p.Index + 1} rode home");
            Assert.AreEqual("town", g.DragonAgent.Actor.Zone.Def.Id);
            Assert.AreEqual("town", WorldDb.Current(WorldDb.Npcs["quell"], 12, 0, g.World).Zone, "and Quell with it");
        }

        [UnityTest]
        public IEnumerator QuellOnlyInActThree()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(12), "fighter", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Assert.IsNull(WorldDb.Current(WorldDb.Npcs["quell"], 12, 0, g.World)?.Zone, "no carriage in Act II");
            g.QuitToTitle();
            yield return Frames(2);
            ActThree(g, 13);
            yield return Frames(2);
            Assert.AreEqual("town", WorldDb.Current(WorldDb.Npcs["quell"], 12, 0, g.World).Zone, "the courier waits in Thistledown");
        }

        [UnityTest]
        public IEnumerator CapitalIsReachable()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 14, "rogue");
            yield return Frames(2);
            g.Warp(p, "capital", WorldDb.CapitalCarriageStop, false);
            yield return Frames(2);
            AcademyTests.AssertAllReachable(g, p.Zone, p.MapPos);
            foreach (var door in new[] { new Vector2Int(31, 9), new Vector2Int(54, 10), new Vector2Int(53, 22), new Vector2Int(59, 30), new Vector2Int(29, 45) })
                Assert.IsTrue(AcademyTests.CanReachBeside(p.Zone, p.MapPos, door), $"door/stair/sign at {door}");
        }

        [UnityTest]
        public IEnumerator CapitalIsWarmWhileTheRealmIsGrey()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 15);
            yield return Frames(2);
            g.Warp(p, "capital", WorldDb.CapitalCarriageStop, false);
            yield return Frames(2);
            Blight.SetStage(2);
            Assert.AreEqual(0f, Blight.OverlayAlpha(g.Zones["capital"]), 0.0001f, "no ash in Aurelhold");
            Assert.Greater(Blight.OverlayAlpha(g.Zones["town"]), 0f, "while Thistledown is grey");
            g.Clock.Minutes = 12 * 60;
            var lamps = g.Zones["capital"].GetComponentsInChildren<LightSource>(true).Where(l => l.name == "obj_ever_lamp").ToList();
            Assert.GreaterOrEqual(lamps.Count, 12, "ever-lamps line the streets");
            Assert.IsTrue(lamps.All(l => l.On && l.GetComponentInChildren<ArcaneHalo>() != null), "and every one is burning at noon");
        }

        [UnityTest]
        public IEnumerator BuoyStaysInThistledownInActThree()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 16);
            yield return Frames(2);
            for (int d = 0; d < 7; d++) CollectionAssert.Contains(new[] { "town", "ford" }, TownDog.HomeFor(d).zone, $"weekday {d}");
            var buoy = Object.FindObjectsByType<TownDog>(FindObjectsSortMode.None).Single();
            CollectionAssert.Contains(new[] { "town", "ford" }, buoy.Actor.Zone.Def.Id, "Buoy is home in his neighbourhood");
            p.Character.HeroicInspiration = false;
            buoy.PetHim(p);
            Assert.IsTrue(p.Character.HeroicInspiration, "and still the best boy in Act III");
        }

        // ============================================================ screenshots (review by eye: grand, warm, lived-in, something off)

        [UnityTest]
        public IEnumerator CaptureCapital()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 90, "wizard");
            yield return Frames(2);
            IEnumerator At(string zone, int x, int y, string shot, int hour = 11)
            {
                g.Clock.Minutes = hour * 60;
                g.Warp(p, zone, new Vector2Int(x, y), false);
                yield return Frames(40);
                p.Hud.CloseAll();
                p.Hud.HideNarration();
                yield return Frames(5);
                AcademyTests.Shot(shot);
            }
            yield return At("capital", 31, 40, "act3_gate_of_wings");
            yield return At("capital", 31, 25, "act3_square_day");
            yield return At("capital", 31, 25, "act3_square_night", 22);
            yield return At("capital", 31, 13, "act3_sunstair");
            yield return At("capital", 10, 22, "act3_market");
            yield return At("capital", 52, 25, "act3_east_quarter");
        }
    }
}
