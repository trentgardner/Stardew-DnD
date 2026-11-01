using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 3: the Lantern Road and the Starfall Academy.</summary>
    public class AcademyTests
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
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static int Count(PlayerSlot p, string text) => p.Hud.LogLines.Count(l => l.Contains(text));

        // ============================================================ Task 1: engine bits

        [UnityTest]
        public IEnumerator RoadIsClosedInActOne()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            var ford = WorldDb.Zones["ford"];
            var at = new Vector2Int(43, 14);
            var w = new WarpDef(at.x, at.y, "town", 25, 19) { MinAct = 2, ClosedText = "test-road-closed" };
            ford.Warps.Add(w);
            try
            {
                g.Warp(p, "ford", at + Vector2Int.left, false);
                yield return Frames(2);
                p.Avatar.PlaceAt(p.Zone, at);
                yield return Frames(5);
                Assert.AreEqual("ford", p.Zone.Def.Id, "a closed road doesn't move you");
                Assert.AreEqual(1, Count(p, "test-road-closed"), "it says so once per step");
                g.World.Act = 2;
                p.Avatar.PlaceAt(p.Zone, at + Vector2Int.left);
                yield return Frames(2);
                p.Avatar.PlaceAt(p.Zone, at);
                yield return Frames(3);
                Assert.AreEqual("town", p.Zone.Def.Id, "open in Act II");
            }
            finally { ford.Warps.Remove(w); }
        }

        [UnityTest]
        public IEnumerator FlagGatedWarps()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            var forbid = new WarpDef(0, 0, "town", 1, 1) { ForbidFlag = "f_test" };
            var require = new WarpDef(0, 0, "town", 1, 1) { RequiresFlag = "f_test" };
            Assert.IsTrue(forbid.Open(g.World));
            Assert.IsFalse(require.Open(g.World));
            g.World.SetFlag("f_test");
            Assert.IsFalse(forbid.Open(g.World));
            Assert.IsTrue(require.Open(g.World));
        }

        [UnityTest]
        public IEnumerator AcademyCharsBuild()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            var warnings = new List<string>();
            void Log(string msg, string st, LogType t) { if (t == LogType.Error || t == LogType.Exception) warnings.Add(msg); }
            Application.logMessageReceived += Log;
            var def = new ZoneDef
            {
                Id = "test_academy_chars", Name = "Test", DefaultSpawn = new Vector2Int(1, 1),
                Map = new[] { "XXXXXXXXXXXX", "Xz#+^&$!-<.X", "XXXXXXXXXXXX" },
            };
            Zone z;
            try { z = Zone.Build(def, 91, new GameObject("test_root").transform); }
            finally { Application.logMessageReceived -= Log; }
            Assert.IsEmpty(warnings, string.Join("\n", warnings));
            foreach (char c in "z#+^&$!-") Assert.IsFalse(z.IsWalkable(new Vector2Int(def.Map[1].IndexOf(c), 1)), $"'{c}' blocks");
            foreach (char c in "<.") Assert.IsTrue(z.IsWalkable(new Vector2Int(def.Map[1].IndexOf(c), 1)), $"'{c}' is walkable");
            Assert.GreaterOrEqual(z.GetComponentsInChildren<Floater>(true).Length, 2, "floating stones and the starwell's light bob");
            Assert.GreaterOrEqual(z.GetComponentsInChildren<WardShimmer>(true).Length, 2, "wards and runes shimmer");
            Object.Destroy(z.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator HourWindowSecret()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            var s = new SecretDef { Id = "test_hour", FromHour = 0, ToHour = 1 };
            g.Clock.Minutes = 30;
            Assert.IsTrue(SecretInteractable.PresentNow(s));
            g.Clock.Minutes = 2 * 60;
            Assert.IsFalse(SecretInteractable.PresentNow(s));
            var late = new SecretDef { Id = "test_late", FromHour = 22, ToHour = 2 };
            g.Clock.Minutes = 23 * 60;
            Assert.IsTrue(SecretInteractable.PresentNow(late), "windows wrap past midnight");
            g.Clock.Minutes = 12 * 60;
            Assert.IsFalse(SecretInteractable.PresentNow(late));
        }

        // ============================================================ helpers

        static void TakeWarp(GameRoot g, PlayerSlot p, string zone, int x, int y)
        {
            var w = WorldDb.Zones[zone].Warps.Find(v => v.At == new Vector2Int(x, y));
            Assert.IsNotNull(w, $"{zone} has a warp at ({x},{y})");
            Assert.IsTrue(w.Open(g.World), $"the warp at {zone} ({x},{y}) is open");
            g.Warp(p, w.ToZone, w.ToPos, false);
        }

        static bool CanReachBeside(Zone z, Vector2Int from, Vector2Int m)
        {
            if (z.IsWalkable(m) && (m == from || z.FindPath(from, m, null, 40000).Count > 0)) return true;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var t = m + d;
                if (z.IsWalkable(t) && (t == from || z.FindPath(from, t, null, 40000).Count > 0)) return true;
            }
            return false;
        }

        /// <summary>Every secret, lore spot, chest, warp and daytime NPC spot in the zone can be reached from `from`.</summary>
        static void AssertAllReachable(GameRoot g, Zone z, Vector2Int from)
        {
            foreach (var kv in z.Def.Secrets) Assert.IsTrue(CanReachBeside(z, from, kv.Key), $"{z.Def.Id}: secret {kv.Value.Id} at {kv.Key}");
            foreach (var l in z.Def.Lore) Assert.IsTrue(CanReachBeside(z, from, l.Pos), $"{z.Def.Id}: lore {l.Id} at {l.Pos}");
            foreach (var c in z.Def.Chests) Assert.IsTrue(CanReachBeside(z, from, c.Pos), $"{z.Def.Id}: chest {c.Id} at {c.Pos}");
            foreach (var w in z.Def.Warps) Assert.IsTrue(w.At == from || z.FindPath(from, w.At, null, 40000).Count > 0, $"{z.Def.Id}: warp to {w.ToZone} at {w.At}");
            foreach (var npc in WorldDb.Npcs.Values)
                foreach (var hour in new[] { 10, 15 })
                {
                    var e = WorldDb.Current(npc, hour, 0, g.World);
                    if (e != null && e.Zone == z.Def.Id) Assert.IsTrue(CanReachBeside(z, from, e.Pos), $"{z.Def.Id}: {npc.Id} at {hour}:00 ({e.Pos})");
                }
        }

        // ============================================================ Task 2: the Lantern Road and Answers

        [UnityTest]
        public IEnumerator LanternRoadLinksFordAndAcademy()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(10), "ranger", "elf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            g.Warp(p, "ford", new Vector2Int(43, 14), false);
            yield return Frames(2);
            Assert.IsTrue(p.Zone.FindPath(new Vector2Int(43, 14), new Vector2Int(45, 14), null, 20000).Count > 0, "the ford's east edge is open");
            TakeWarp(g, p, "ford", 45, 14);
            yield return Frames(2);
            Assert.AreEqual("lantern_road", p.Zone.Def.Id);
            AssertAllReachable(g, p.Zone, p.MapPos);
            Assert.IsTrue(p.Zone.FindPath(p.MapPos, new Vector2Int(52, 0), null, 40000).Count > 0, "the road climbs to the Academy gate");
            TakeWarp(g, p, "lantern_road", 0, 14);
            yield return Frames(2);
            Assert.AreEqual("ford", p.Zone.Def.Id);
        }

        [UnityTest]
        public IEnumerator AnswersStartsWithActTwoEveryWay()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(11), "fighter", "human");
            yield return Frames(2);
            Assert.AreEqual(0, g.Quests.Stage(Answers.Id), "not in Act I");
            g.BeginActTwo(p);
            Assert.AreEqual(Answers.Road, g.Quests.Stage(Answers.Id), "Begin Act II starts it");
            g.QuitToTitle();
            yield return Frames(2);
            var q = g.DebugStart(Still(12), "wizard", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(q);
            Assert.AreEqual(Answers.Road, g.Quests.Stage(Answers.Id), "Start at Act II starts it");
            // An Act II save from before the quest existed picks it up on load.
            q.Hud.CloseAll();
            g.Quests.Stages.Remove(Answers.Id);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(Answers.Road, g.Quests.Stage(Answers.Id), "loading an Act II save starts it");
            // Never restarts once done, and never re-pays.
            g.Quests.Set(Answers.Id, Answers.Heard, g.Players[0]);
            int xp = g.Players[0].Character.XP;
            path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(Answers.Heard, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(xp, g.Players[0].Character.XP, "no re-pay on load");
        }

        // ============================================================ Task 3: the Academy grounds

        static PlayerSlot ActTwoAt(GameRoot g, PlayerSlot p, string zone, Vector2Int pos)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            g.Warp(p, zone, pos, false);
            return p;
        }

        [UnityTest]
        public IEnumerator AcademyGroundsAreReachable()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(20), "rogue", "elf");
            yield return Frames(2);
            ActTwoAt(g, p, "lantern_road", new Vector2Int(52, 2));
            yield return Frames(2);
            TakeWarp(g, p, "lantern_road", 52, 0);
            yield return Frames(2);
            Assert.AreEqual("academy", p.Zone.Def.Id);
            Assert.AreEqual(Answers.Academy, g.Quests.Stage(Answers.Id), "reaching the Academy moves Answers on");
            AssertAllReachable(g, p.Zone, p.MapPos);
            foreach (var door in new[] { new Vector2Int(12, 11), new Vector2Int(44, 13), new Vector2Int(48, 25), new Vector2Int(8, 26) })
                Assert.IsTrue(p.Zone.FindPath(p.MapPos, door + Vector2Int.up, null, 40000).Count > 0 || p.Zone.FindPath(p.MapPos, door, null, 40000).Count > 0, $"door at {door}");
        }

        [UnityTest]
        public IEnumerator ThirteenthBellOnlyAtMidnight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(21), "bard", "human");
            yield return Frames(2);
            ActTwoAt(g, p, "academy", new Vector2Int(34, 12));
            yield return Frames(2);
            var bell = Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == "thirteenth_bell");
            g.Clock.Minutes = 23 * 60;
            Assert.IsFalse(bell.Available(p), "not before midnight");
            g.Clock.Minutes = 30;
            Assert.IsTrue(bell.Available(p), "between midnight and one");
            bell.Resolve(p, Rules.CheckOutcome.Success);
            Assert.IsTrue(g.World.LoreFound.Contains("thirteenth_bell"));
        }

        [UnityTest]
        public IEnumerator AcademyHoldsTheBlightBack()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(22), "fighter", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            Blight.SetStage(2);
            Assert.Less(Blight.OverlayAlpha(g.Zones["academy"]), Blight.OverlayAlpha(g.Zones["town"]), "the wards hold the ash back");
        }

        [UnityTest]
        public IEnumerator AcademyHasVisibleMagic()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(23), "wizard", "elf");
            yield return Frames(2);
            var z = g.Zones["academy"];
            Assert.GreaterOrEqual(z.GetComponentsInChildren<Floater>(true).Length, 6, "floating stones");
            Assert.GreaterOrEqual(z.GetComponentsInChildren<WardShimmer>(true).Length, 8, "wards");
            Assert.AreEqual(1, z.GetComponentsInChildren<FrameFlip>(true).Count(f => f.Id == "obj_orrery"), "the Grand Orrery");
        }

        // ============================================================ Task 4: the library, the Restricted Section, the Sealing Hall

        /// <summary>Stands the player on a tile the way walking there would, so warps fire.</summary>
        static IEnumerator StepOn(PlayerSlot p, Vector2Int from, Vector2Int to)
        {
            p.Avatar.PlaceAt(p.Zone, from);
            yield return Frames(2);
            p.Avatar.PlaceAt(p.Zone, to);
            yield return Frames(3);
        }

        static SecretInteractable SpotNamed(string id) => Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == id);

        [UnityTest]
        public IEnumerator StacksLoopUntilSeenThrough()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(30), "wizard", "elf");
            yield return Frames(2);
            ActTwoAt(g, p, "library", new Vector2Int(26, 2));
            yield return Frames(2);
            yield return StepOn(p, new Vector2Int(28, 2), new Vector2Int(29, 2));
            Assert.AreEqual("library", p.Zone.Def.Id);
            Assert.AreEqual(new Vector2Int(2, 2), p.MapPos, "the end of the corridor is its beginning");
            SpotNamed("stacks_illusion").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.IsTrue(g.World.Flag(WorldDb.StacksUnlooped));
            yield return StepOn(p, new Vector2Int(28, 2), new Vector2Int(29, 2));
            Assert.AreEqual(new Vector2Int(29, 2), p.MapPos, "seen through: the corridor ends where it should");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsTrue(g.World.Flag(WorldDb.StacksUnlooped), "saved");
        }

        [UnityTest]
        public IEnumerator RestrictedSectionOpensEveryWay()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(31), "rogue", "elf");
            yield return Frames(2);
            ActTwoAt(g, p, "library", new Vector2Int(31, 2));
            g.World.SetFlag(WorldDb.StacksUnlooped);
            yield return Frames(2);
            yield return StepOn(p, new Vector2Int(32, 2), new Vector2Int(33, 2));
            Assert.AreEqual("library", p.Zone.Def.Id, "warded shut without a way in");
            Assert.AreEqual(1, Count(p, "Runes crawl across the door"));
            foreach (var way in new[] { "pass", "unweave_ward", "pick_ward_lock" })
            {
                g.World.Flags.Remove(WorldDb.RestrictedPass);
                g.World.Flags.Remove("secret:unweave_ward"); g.World.Flags.Remove("secret:pick_ward_lock");
                if (way == "pass") g.World.SetFlag(WorldDb.RestrictedPass);
                else SpotNamed(way).Resolve(p, Rules.CheckOutcome.Success);
                p.Hud.CloseAll();
                if (p.Zone.Def.Id != "library") g.Warp(p, "library", new Vector2Int(31, 2), false);
                yield return Frames(2);
                yield return StepOn(p, new Vector2Int(32, 2), new Vector2Int(33, 2));
                Assert.AreEqual("restricted", p.Zone.Def.Id, $"the door opens by way of: {way}");
                g.Warp(p, "library", new Vector2Int(31, 2), false);
                yield return Frames(2);
            }
            // The Archivist's pass, shown: the guardians stand down (full XP), the existing vouch path.
            var group = WorldDb.Zones["restricted"].EnemyGroups.Single(e => e.Id == "restricted_guardians");
            Assert.AreEqual(WorldDb.ThornePassShown, group.VouchFlag);
        }

        [UnityTest]
        public IEnumerator DragonWaitsOutsideTheSealingHallAndRejoins()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(32), "fighter", "human");
            yield return Frames(2);
            ActTwoAt(g, p, "restricted", new Vector2Int(20, 15));
            yield return Frames(15);
            var dragon = g.DragonAgent;
            Assert.IsNotNull(dragon);
            Assert.AreEqual("restricted", dragon.Actor.Zone.Def.Id, "the dragon came this far");
            g.Warp(p, "sealing_hall", new Vector2Int(12, 19), false);
            yield return Frames(15);
            Assert.AreEqual("restricted", dragon.Actor.Zone.Def.Id, "it won't go down");
            Assert.GreaterOrEqual(Count(p, "won't come down the stairs"), 1, "and it says so");
            g.Warp(p, "restricted", new Vector2Int(21, 14), false);
            yield return Frames(15);
            Assert.AreEqual("restricted", dragon.Actor.Zone.Def.Id);
            Assert.Less(Vector2.Distance(dragon.Actor.MapPos, p.MapPos), 6f, "it rejoins you");
            // Saved in the hall: it waits upstairs, and finds you again once you come up.
            g.Warp(p, "sealing_hall", new Vector2Int(12, 19), false);
            yield return Frames(5);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(15);
            Assert.AreEqual("sealing_hall", g.Players[0].Zone.Def.Id);
            Assert.AreEqual("restricted", g.DragonAgent.Actor.Zone.Def.Id, "loaded in the hall: the dragon is waiting upstairs");
            g.Warp(g.Players[0], "library", new Vector2Int(17, 22), false);
            yield return Frames(15);
            Assert.AreEqual("library", g.DragonAgent.Actor.Zone.Def.Id, "and catches up once you leave by any route");
        }

        [UnityTest]
        public IEnumerator WardenLoreIsInTheRestrictedSection()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(33), "cleric", "dwarf");
            yield return Frames(2);
            Assert.IsTrue(WorldDb.Zones["restricted"].Lore.Exists(l => l.Id == "emberhorn_warden"));
            Assert.AreEqual(2, LoreDb.Get("emberhorn_warden").MinAct, "Act II only");
        }

        // ============================================================ Task 5: the Orrery Tower and Ashby

        [UnityTest]
        public IEnumerator HearingAshbyOutAdvancesAnswers()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(40), "paladin", "human");
            yield return Frames(2);
            ActTwoAt(g, p, "academy", new Vector2Int(44, 15));
            yield return Frames(2);
            Assert.AreEqual(Answers.Academy, g.Quests.Stage(Answers.Id));
            TakeWarp(g, p, "academy", 44, 13);
            yield return Frames(2);
            Assert.AreEqual("ashby_tower", p.Zone.Def.Id);
            Assert.AreEqual(Answers.Tower, g.Quests.Stage(Answers.Id), "his door is open");
            int xp = p.Character.XP;
            var d = DialogueDb.Get("ashby");
            var c = new DCtx { P = p };
            Assert.AreEqual("intro", d.Start(c));
            var agree = d.Nodes["plan"].Options.Single(o => o.Text == "You could be right.");
            agree.Do(c);
            Assert.IsTrue(g.World.Flag("ashby_heard"));
            Assert.AreEqual(Answers.Heard, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(10, Factions.Standing("academy"));
            Assert.AreEqual(xp + 600, p.Character.XP, "the milestone pays once");
            Assert.AreEqual("again", d.Start(c), "afterwards he greets you as someone he's spoken to");
        }

        [UnityTest]
        public IEnumerator ConfrontingAboutTheHallNeedsToHaveSeenIt()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(41), "bard", "half_elf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            var d = DialogueDb.Get("ashby");
            var c = new DCtx { P = p };
            var confront = d.Nodes["plan"].Options.Single(o => o.Check == Rules.Skill.Persuasion);
            Assert.IsFalse(confront.Show(c), "not offered before you've seen the hall");
            g.World.SetFlag("seen:sealing_hall");
            Assert.IsTrue(confront.Show(c));
        }

        // ============================================================ Task 6: the professors' quests

        [UnityTest]
        public IEnumerator OverdueBooksOpenTheRestrictedSection()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(50), "rogue", "gnome");
            yield return Frames(2);
            ActTwoAt(g, p, "academy", new Vector2Int(27, 30));
            yield return Frames(2);
            var thorne = DialogueDb.Get("thorne");
            var c = new DCtx { P = p };
            thorne.Nodes["earn"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("overdue"));
            SpotNamed("geese_nest").Resolve(p, Rules.CheckOutcome.Success);
            SpotNamed("maze_book").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(1, g.Quests.Stage("overdue"), "two of three");
            var pell = DialogueDb.Get("pell");
            pell.Nodes["gives"].OnEnter(c);
            pell.Nodes["gives"].OnEnter(c); // talking to him twice doesn't make a fourth book
            Assert.AreEqual(3, p.Character.CountOf("overdue_book"));
            Assert.AreEqual(2, g.Quests.Stage("overdue"), "all three: back to Thorne");
            int xp = p.Character.XP;
            Assert.AreEqual("returned", thorne.Start(c));
            thorne.Nodes["returned"].OnEnter(c);
            Assert.AreEqual(0, p.Character.CountOf("overdue_book"), "the books go home");
            Assert.IsTrue(g.Quests.Done("overdue"));
            foreach (var f in new[] { WorldDb.RestrictedPass, WorldDb.ThornePassShown, WorldDb.StacksUnlooped }) Assert.IsTrue(g.World.Flag(f), f);
            Assert.AreEqual(xp + 600, p.Character.XP, "paid once");
        }

        [UnityTest]
        public IEnumerator GlasshouseQuestRunsPeacefully()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(51), "druid", "elf");
            yield return Frames(2);
            ActTwoAt(g, p, "glasshouse", new Vector2Int(9, 10));
            yield return Frames(2);
            var group = WorldDb.Zones["glasshouse"].EnemyGroups.Single(e => e.Id == "glasshouse_blights");
            Assert.AreEqual(Rules.Skill.Nature, group.TalkSkill, "calmed with Nature, not Animal Handling");
            var c = new DCtx { P = p };
            DialogueDb.Get("oakes").Nodes["ask"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("glasshouse"));
            int xp = p.Character.XP;
            g.PacifyGroup("glasshouse_blights", p);
            Assert.AreEqual(2, g.Quests.Stage("glasshouse"), "the vines are still");
            Assert.AreEqual(xp + 400, p.Character.XP, "calming them pays the group's full XP, as fighting would");
            var stone = SpotNamed("cracked_heatstone");
            Assert.IsTrue(stone.Available(p), "now find the root");
            stone.Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(3, g.Quests.Stage("glasshouse"));
            Assert.IsTrue(g.World.LoreFound.Contains("grey_fire"));
            Assert.IsFalse(SpotNamed("cracked_heatstone_arcana").Available(p), "the other route closes once one works");
            xp = p.Character.XP;
            var oakes = DialogueDb.Get("oakes");
            Assert.AreEqual("done", oakes.Start(c));
            oakes.Nodes["done"].OnEnter(c);
            Assert.IsTrue(g.Quests.Done("glasshouse"));
            Assert.AreEqual(xp + 700, p.Character.XP);
            Assert.AreEqual(2, p.Character.CountOf("ember_lily"));
        }

        [UnityTest]
        public IEnumerator MeasureOfAWyrmEveryChoiceCompletes()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(52), "ranger", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            var d = DialogueDb.Get("varrow");
            var c = new DCtx { P = p };
            var node = d.Nodes["measure"];
            void Reset() { g.Quests.Stages["measure_wyrm"] = 1; g.World.DragonBond = 50; g.World.Standing["academy"] = 0; }

            Reset();
            node.Options.Single(o => o.Text == "Go ahead.").Do(c);
            Assert.AreEqual(2, g.Quests.Stage("measure_wyrm"));
            Assert.AreEqual(47, g.World.DragonBond); Assert.AreEqual(5, Factions.Standing("academy"));

            Reset();
            node.Options.Single(o => o.Text.StartsWith("Not tonight")).Do(c);
            Assert.AreEqual(2, g.Quests.Stage("measure_wyrm"));
            Assert.AreEqual(50, g.World.DragonBond); Assert.AreEqual(0, Factions.Standing("academy"));

            var decide = node.Options.Single(o => o.Check == Rules.Skill.AnimalHandling);
            Reset();
            decide.OnSuccess(c);
            Assert.AreEqual(2, g.Quests.Stage("measure_wyrm"));
            Assert.AreEqual(52, g.World.DragonBond); Assert.AreEqual(5, Factions.Standing("academy"));
            Reset();
            decide.OnFailure(c);
            Assert.AreEqual(2, g.Quests.Stage("measure_wyrm"));
            Assert.AreEqual(49, g.World.DragonBond); Assert.AreEqual(0, Factions.Standing("academy"));
        }

        // ============================================================ Task 7: acceptance

        [UnityTest]
        public IEnumerator AcademyGoldenPath()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(60), "fighter", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Assert.AreEqual(Answers.Road, g.Quests.Stage(Answers.Id));
            // The Ford's east road, the Lantern Road, the Academy gate.
            g.Warp(p, "ford", new Vector2Int(44, 14), false);
            yield return Frames(2);
            TakeWarp(g, p, "ford", 45, 14);
            yield return Frames(2);
            Assert.AreEqual("lantern_road", p.Zone.Def.Id);
            Assert.IsTrue(p.Zone.FindPath(p.MapPos, new Vector2Int(52, 0), null, 40000).Count > 0, "the road can be walked end to end");
            TakeWarp(g, p, "lantern_road", 52, 0);
            yield return Frames(2);
            Assert.AreEqual(Answers.Academy, g.Quests.Stage(Answers.Id));
            // The library: see through the Stacks, get the pass the honest way.
            TakeWarp(g, p, "academy", 12, 11);
            yield return Frames(2);
            Assert.AreEqual("library", p.Zone.Def.Id);
            SpotNamed("stacks_illusion").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            g.World.SetFlag(WorldDb.RestrictedPass); g.World.SetFlag(WorldDb.ThornePassShown);
            TakeWarp(g, p, "library", 33, 2);
            yield return Frames(2);
            Assert.AreEqual("restricted", p.Zone.Def.Id);
            Object.FindObjectsByType<LoreSpot>(FindObjectsSortMode.None).Single(s => s.Def.Id == "emberhorn_warden").Interact(p);
            p.Hud.CloseAll();
            Assert.IsTrue(g.World.LoreFound.Contains("emberhorn_warden"), "the weakness lore, for Phase 6");
            // The Sealing Hall: the dragon stops at the stairs.
            yield return Frames(10);
            TakeWarp(g, p, "restricted", 21, 15);
            yield return Frames(10);
            Assert.AreEqual("sealing_hall", p.Zone.Def.Id);
            Assert.AreEqual("restricted", g.DragonAgent.Actor.Zone.Def.Id);
            // Up the tower; hear Ashby out (and confront him about the hall).
            g.Warp(p, "academy", new Vector2Int(44, 15), false);
            yield return Frames(5);
            TakeWarp(g, p, "academy", 44, 13);
            yield return Frames(2);
            Assert.AreEqual(Answers.Tower, g.Quests.Stage(Answers.Id));
            var d = DialogueDb.Get("ashby");
            var c = new DCtx { P = p };
            var confront = d.Nodes["plan"].Options.Single(o => o.Check == Rules.Skill.Persuasion);
            Assert.IsTrue(confront.Show(c), "having seen the hall, you can ask him about it");
            confront.OnSuccess(c);
            d.Nodes["plan"].Options.Single(o => o.Text == "You could be right.").Do(c);
            Assert.AreEqual(Answers.Heard, g.Quests.Stage(Answers.Id));
            // Save and load: everything holds.
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(5);
            Assert.AreEqual(Answers.Heard, g.Quests.Stage(Answers.Id));
            foreach (var f in new[] { WorldDb.StacksUnlooped, WorldDb.RestrictedPass, "ashby_heard", "ashby_hall_confronted", "seen:sealing_hall" }) Assert.IsTrue(g.World.Flag(f), f);
            Assert.IsTrue(g.World.LoreFound.Contains("emberhorn_warden"));
            Assert.AreEqual(15, Factions.Standing("academy"));
        }

        [UnityTest]
        public IEnumerator CaptureAcademy()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(61), "wizard", "elf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            g.Clock.Minutes = 11 * 60;
            IEnumerator At(string zone, int x, int y, string shot, int hour = 11)
            {
                g.Clock.Minutes = hour * 60;
                g.Warp(p, zone, new Vector2Int(x, y), false);
                yield return Frames(40);
                p.Hud.CloseAll();
                p.Hud.HideNarration();
                yield return Frames(5);
                Shot(shot);
            }
            yield return At("lantern_road", 23, 13, "act2_lantern_road");
            yield return At("academy", 27, 25, "act2_academy_day");
            yield return At("academy", 27, 25, "act2_academy_night", 22);
            yield return At("library", 14, 6, "act2_library");
            yield return At("library", 12, 2, "act2_endless_stacks");
            yield return At("sealing_hall", 12, 15, "act2_sealing_hall");
            yield return At("ashby_tower", 8, 10, "act2_ashby_study");
        }

        static void Shot(string name)
        {
            string dir = Path.Combine(Path.GetTempPath(), "embers_screenshots");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24);
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var cam in cams) { if (!cam.isActiveAndEnabled) continue; var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt);
        }
    }
}
