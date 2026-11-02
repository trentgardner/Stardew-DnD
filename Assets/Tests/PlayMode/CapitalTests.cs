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

        // ============================================================ Task 3: the Roots and the Deep Shift

        static DCtx Ctx(PlayerSlot p) => new DCtx { P = p };
        static DOption Opt(string dialogue, string node, Skill skill) => DialogueDb.Get(dialogue).Nodes[node].Options.First(o => o.Check == skill);

        [UnityTest]
        public IEnumerator DeepShiftEveryWay()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 20);
            yield return Frames(2);
            var q = QuestDb.Get(DeepShift.Id);

            // Crale talked round.
            g.Quests.Start(DeepShift.Id, p);
            DialogueDb.Get("crale").Nodes["ask"].OnEnter(Ctx(p));
            Assert.AreEqual(DeepShift.Crale, g.Quests.Stage(DeepShift.Id), "asking Crale moves it on");
            Opt("crale", "ask", Skill.Persuasion).OnSuccess(Ctx(p));
            Assert.AreEqual(DeepShift.Below, g.Quests.Stage(DeepShift.Id));
            Assert.IsTrue(g.World.Flag("clue_shift"), "the deep shift is a clue");
            Assert.Less(g.Quests.Stage(DeepShift.Id), q.Last, "and the quest goes on (Bran is still below)");
            g.QuitToTitle();
            yield return Frames(2);

            // The ledger, the quiet way.
            p = ActThree(g, 21, "rogue");
            yield return Frames(2);
            g.Quests.Set(DeepShift.Id, DeepShift.Crale, p);
            g.Warp(p, "roots", new Vector2Int(47, 18), false);
            yield return Frames(3);
            var ledger = Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(x => x.Def.Id == "works_ledger");
            ledger.Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(DeepShift.Below, g.Quests.Stage(DeepShift.Id), "the ledger tells it too");
            Assert.IsTrue(g.World.LoreFound.Contains("works_ledger"));
            Assert.IsTrue(g.World.Flag("clue_shift"));
            g.QuitToTitle();
            yield return Frames(2);

            // Leaning on him and failing: his men, fists only.
            p = ActThree(g, 22, "barbarian");
            yield return Frames(2);
            g.Quests.Set(DeepShift.Id, DeepShift.Crale, p);
            g.Warp(p, "roots", new Vector2Int(44, 20), false);
            yield return Frames(3);
            Opt("crale", "ask", Skill.Intimidation).OnFailure(Ctx(p));
            g.SpawnAndFight("crale_men", p);
            yield return Frames(3);
            var fight = g.Combats.SingleOrDefault(c => c.GroupId == "crale_men");
            Assert.IsNotNull(fight, "Crale's men come");
            Assert.IsTrue(fight.Brawl, "fists only");
        }

        [UnityTest]
        public IEnumerator RootsPatrolOnlyAtNight()
        {
            var g = GameRoot.I;
            ActThree(g, 23);
            yield return Frames(2);
            var patrol = WorldDb.Zones["roots"].EnemyGroups.Single(x => x.Id == "roots_patrol");
            g.Clock.Minutes = 23 * 60;
            Assert.IsTrue(patrol.InStage(g.Quests), "curfew at night");
            g.Clock.Minutes = 12 * 60;
            Assert.IsFalse(patrol.InStage(g.Quests), "nobody about at noon");
            Assert.AreEqual("banner_crown", patrol.VouchFlag, "the Crown's friends are waved past");
            var men = WorldDb.Zones["roots"].EnemyGroups.Single(x => x.Id == "crale_men");
            Assert.IsFalse(men.InStage(g.Quests), "Crale's men only come when called");
        }

        [UnityTest]
        public IEnumerator RootsIsReachable()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 24, "rogue");
            yield return Frames(2);
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(2);
            var stair = WorldDb.Zones["capital"].Warps.Find(w => w.At == new Vector2Int(59, 30));
            Assert.IsNotNull(stair, "the stair down from the capital");
            g.Warp(p, stair.ToZone, stair.ToPos, false);
            yield return Frames(2);
            Assert.AreEqual("roots", p.Zone.Def.Id);
            AcademyTests.AssertAllReachable(g, p.Zone, p.MapPos);
            Assert.IsTrue(AcademyTests.CanReachBeside(p.Zone, p.MapPos, new Vector2Int(51, 23)), "the Undergate's notice");
            var up = WorldDb.Zones["roots"].Warps.Find(w => w.ToZone == "capital");
            Assert.IsNotNull(up, "and back up");
            g.Warp(p, "steamhouse", WorldDb.Zones["steamhouse"].DefaultSpawn, false);
            yield return Frames(2);
            AcademyTests.AssertAllReachable(g, p.Zone, p.MapPos);
        }

        [UnityTest]
        public IEnumerator UndergateIsSealedForNow()
        {
            var g = GameRoot.I;
            ActThree(g, 25);
            yield return Frames(2);
            var roots = WorldDb.Zones["roots"];
            Assert.IsFalse(roots.Warps.Any(w => w.At == new Vector2Int(52, 20) || w.At == new Vector2Int(52, 21)), "no way through yet");
            StringAssert.Contains("King's own seal", roots.Signs[new Vector2Int(51, 23)]);
            Assert.AreEqual('D', roots.Map[20][52]);
        }

        // ============================================================ Task 4: the palace, the records, the Wyvern

        [UnityTest]
        public IEnumerator AudienceWithTheKing()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 30, "bard");
            yield return Frames(2);
            g.RideCarriage(p, "capital", WorldDb.CapitalCarriageStop);
            yield return Frames(3);
            Assert.AreEqual(WarmCity.Arrived, g.Quests.Stage(WarmCity.Id));
            var king = WorldDb.Npcs["king_aldric"];
            Assert.AreEqual("throne_room", WorldDb.Current(king, 10, 0, g.World).Zone, "he receives by day");
            Assert.IsNull(WorldDb.Current(king, 20, 0, g.World).Zone, "and not at night");
            var d = DialogueDb.Get("king_aldric");
            Assert.AreEqual("audience", d.Start(Ctx(p)));
            Opt("king_aldric", "audience", Skill.Insight).OnSuccess(Ctx(p));
            Assert.IsTrue(g.World.LoreFound.Contains("the_kings_ring"), "the tell goes in the journal");
            d.Nodes["close"].OnEnter(Ctx(p));
            Assert.AreEqual(WarmCity.Audience, g.Quests.Stage(WarmCity.Id), "the audience moves The Warm City on");
            Assert.AreEqual("again", d.Start(Ctx(p)), "and he doesn't hold it twice");
        }

        [UnityTest]
        public IEnumerator DragonWaitsOutsideThePalaceAndRejoins()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 31);
            yield return Frames(2);
            g.Warp(p, "capital", new Vector2Int(31, 11), false);
            yield return Frames(3);
            Assert.AreEqual("capital", g.DragonAgent.Actor.Zone.Def.Id);
            var door = WorldDb.Zones["capital"].Warps.Find(w => w.At == new Vector2Int(31, 9));
            g.Warp(p, door.ToZone, door.ToPos, false);
            yield return Frames(3);
            Assert.AreEqual("throne_room", p.Zone.Def.Id);
            Assert.AreEqual("capital", g.DragonAgent.Actor.Zone.Def.Id, "the dragon waits on the Sunstair");
            Assert.IsTrue(p.Hud.LogLines.Any(l => l.Contains("tall, but not that tall")), "with the palace's own line");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(4);
            p = g.Players[0];
            Assert.AreEqual("throne_room", p.Zone.Def.Id);
            Assert.AreNotEqual("throne_room", g.DragonAgent.Actor.Zone.Def.Id, "a save made inside never puts the dragon in the palace");
            var back = WorldDb.Zones["throne_room"].Warps.Find(w => w.ToZone == "capital");
            g.Warp(p, back.ToZone, back.ToPos, false);
            for (int i = 0; i < 120 && g.DragonAgent.Actor.Zone != p.Zone; i++) yield return null;
            Assert.AreEqual("capital", g.DragonAgent.Actor.Zone.Def.Id, "and rejoins outside");
            g.Warp(p, "royal_archive", WorldDb.Zones["royal_archive"].DefaultSpawn, false);
            yield return Frames(2);
            Assert.AreEqual("capital", g.DragonAgent.Actor.Zone.Def.Id, "it won't go into the archive either");
        }

        [UnityTest]
        public IEnumerator RecordsClueAndInteriorsReachable()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 32, "rogue");
            yield return Frames(2);
            foreach (var zone in new[] { "throne_room", "royal_archive", "capital_inn" })
            {
                g.Warp(p, zone, WorldDb.Zones[zone].DefaultSpawn, false);
                yield return Frames(2);
                AcademyTests.AssertAllReachable(g, p.Zone, p.MapPos);
                Assert.IsTrue(WorldDb.Zones[zone].Warps.Any(w => w.ToZone == "capital"), zone + " leads back out");
            }
            g.Warp(p, "royal_archive", WorldDb.Zones["royal_archive"].DefaultSpawn, false);
            yield return Frames(2);
            var annals = Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(x => x.Def.Id == "royal_annals");
            annals.Resolve(p, CheckOutcome.Success);
            Assert.IsTrue(g.World.Flag("clue_records"));
            Assert.IsTrue(g.World.LoreFound.Contains("royal_annals"));
        }

        [UnityTest]
        public IEnumerator InnRestWorks()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 33);
            yield return Frames(2);
            g.Warp(p, "capital_inn", WorldDb.Zones["capital_inn"].DefaultSpawn, false);
            yield return Frames(2);
            int cp = p.Character.Copper;
            p.Character.HP = 1;
            g.RentRoom(p);
            for (int i = 0; i < 300 && p.Character.HP < p.Character.EffectiveMaxHP; i++) yield return null;
            Assert.AreEqual(cp - 50, p.Character.Copper, "5 sp");
            Assert.AreEqual(p.Character.EffectiveMaxHP, p.Character.HP, "a long rest at the Gilded Wyvern");
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
            yield return At("roots", 26, 15, "act3_roots", 15);
            yield return At("roots", 47, 21, "act3_undergate", 15);
            yield return At("steamhouse", 8, 9, "act3_steamhouse", 15);
            yield return At("throne_room", 14, 9, "act3_throne_room");
            yield return At("royal_archive", 12, 14, "act3_archive");
            yield return At("capital_inn", 9, 10, "act3_gilded_wyvern", 20);
        }
    }
}
