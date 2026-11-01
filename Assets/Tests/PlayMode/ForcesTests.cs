using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 4: the four forces' homes, scouting quests and major quests.</summary>
    public class ForcesTests
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

        // ============================================================ helpers

        static PlayerSlot ActTwo(GameRoot g, PlayerSlot p)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            return p;
        }

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

        /// <summary>Every secret, lore spot, chest, warp, enemy and daytime NPC spot in the zone can be reached from `from`.</summary>
        static void AssertAllReachable(GameRoot g, Zone z, Vector2Int from)
        {
            foreach (var kv in z.Def.Secrets) Assert.IsTrue(CanReachBeside(z, from, kv.Key), $"{z.Def.Id}: secret {kv.Value.Id} at {kv.Key}");
            foreach (var l in z.Def.Lore) Assert.IsTrue(CanReachBeside(z, from, l.Pos), $"{z.Def.Id}: lore {l.Id} at {l.Pos}");
            foreach (var w in z.Def.Warps) Assert.IsTrue(w.At == from || z.FindPath(from, w.At, null, 40000).Count > 0, $"{z.Def.Id}: warp to {w.ToZone} at {w.At}");
            foreach (var grp in z.Def.EnemyGroups) foreach (var m in grp.Members) Assert.IsTrue(CanReachBeside(z, from, m.Pos), $"{z.Def.Id}: {grp.Id} at {m.Pos}");
            foreach (var npc in WorldDb.Npcs.Values)
                foreach (var hour in new[] { 10, 15, 21 })
                {
                    var e = WorldDb.Current(npc, hour, 0, g.World);
                    if (e != null && e.Zone == z.Def.Id) Assert.IsTrue(CanReachBeside(z, from, e.Pos), $"{z.Def.Id}: {npc.Id} at {hour}:00 ({e.Pos})");
                }
        }

        static SecretInteractable SpotNamed(string id) => Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == id);
        static DOption Opt(DNode n, string startsWith) => n.Options.Single(o => o.Text.StartsWith(startsWith));

        /// <summary>Fights the group and wins (every enemy drops to 0 HP on the first turn).</summary>
        static IEnumerator WinFight(GameRoot g, PlayerSlot p, string groupId)
        {
            var input = p.Input;
            p.Input = new FuzzInput(7) { CancelChance = 0, MenuChance = 0 }; // clicks through the dice trays
            g.StartGroupCombat(groupId, p);
            yield return Frames(2);
            Assert.IsTrue(g.AnyInCombat, $"a fight with {groupId}");
            foreach (var c in g.Combats) foreach (var u in c.Units) if (u.C.Team == DnD.Rules.Team.Enemies) u.C.HP = 0;
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 60f) yield return null;
            Time.timeScale = 1;
            p.Input = input;
            Assert.IsFalse(g.AnyInCombat, "the fight ends");
            Assert.IsTrue(g.World.DefeatedGroups.Contains(groupId), "a victory");
            p.Hud.CloseAll();
        }

        static void HearAll() { foreach (var f in FactionDb.All.Keys) Factions.Hear(f); }

        /// <summary>Chooses "Stand with …" on whatever commit prompt is on top.</summary>
        static IEnumerator ConfirmCommit(PlayerSlot p)
        {
            yield return Frames(2);
            var screen = p.Hud.Top as ChoiceScreen;
            Assert.IsNotNull(screen, "the commit prompt");
            screen.Choose("Stand with");
            yield return Frames(2);
        }

        // ============================================================ Task 2: the Crown

        [UnityTest]
        public IEnumerator RedoubtIsReachable()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            var track = WorldDb.Zones["lantern_road"].Warps.Find(w => w.ToZone == "redoubt");
            Assert.IsNotNull(track);
            Assert.IsFalse(track.Open(g.World), "closed in Act I");
            ActTwo(g, p);
            TakeWarp(g, p, "lantern_road", 45, 0);
            yield return Frames(3);
            Assert.AreEqual("redoubt", p.Zone.Def.Id);
            AssertAllReachable(g, p.Zone, p.Avatar.MapPos);
            var road = g.Zones["lantern_road"];
            AssertAllReachable(g, road, new Vector2Int(1, 14));
        }

        [UnityTest]
        public IEnumerator CountedFullRun()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(2), "fighter", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var hol = DialogueDb.Get("holloway");
            g.World.SetFlag("met_holloway");
            Assert.AreEqual("start", hol.Start(c));
            hol.Nodes["counted"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("counted"));
            var hounds = WorldDb.Zones["lantern_road"].EnemyGroups.Single(e => e.Id == "wagon_hounds");
            Assert.IsTrue(hounds.InStage(g.Quests), "the hounds are at the wagon");
            int xp = p.Character.XP;
            g.PacifyGroup("wagon_hounds", p);
            Assert.AreEqual(2, g.Quests.Stage("counted"));
            Assert.AreEqual(xp + 3 * 700, p.Character.XP, "calming them pays like fighting them");
            g.Warp(p, "lantern_road", new Vector2Int(33, 23), false);
            yield return Frames(3);
            var drivers = SpotNamed("counted_drivers");
            Assert.IsTrue(drivers.Available(p));
            drivers.Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(3, g.Quests.Stage("counted"));
            Assert.AreEqual("counted_done", hol.Start(c));
            xp = p.Character.XP;
            hol.Nodes["counted_done"].OnEnter(c);
            hol.Nodes["counted_done"].OnEnter(c);
            Assert.IsTrue(g.Quests.Done("counted"));
            Assert.AreEqual(xp + 1200, p.Character.XP, "paid once");
            Assert.AreEqual(10, Factions.Standing("crown"));
        }

        [UnityTest]
        public IEnumerator TwoCopiesEveryOptionCompletes()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(3), "bard", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var letters = DialogueDb.Get("marsh").Nodes["letters"];
            void Reset() { g.Quests.Stages["two_copies"] = 1; g.World.Standing["crown"] = 0; g.World.Standing["academy"] = 0; }

            Reset();
            Opt(letters, "Then Varrow").Do(c);
            Assert.IsTrue(g.Quests.Done("two_copies"));
            Assert.AreEqual(-5, Factions.Standing("crown")); Assert.AreEqual(10, Factions.Standing("academy"));
            Reset();
            Opt(letters, "Your secret").Do(c);
            Assert.IsTrue(g.Quests.Done("two_copies"));
            Assert.AreEqual(10, Factions.Standing("crown")); Assert.AreEqual(-5, Factions.Standing("academy"));
            var ask = letters.Options.Single(o => o.Check == Rules.Skill.Persuasion);
            Reset();
            ask.OnSuccess(c);
            Assert.IsTrue(g.Quests.Done("two_copies"));
            Assert.AreEqual(5, Factions.Standing("crown")); Assert.AreEqual(5, Factions.Standing("academy"));
            Assert.IsTrue(g.World.Flag("marsh_asks_ashby"));
            Reset();
            ask.OnFailure(c);
            Assert.IsTrue(g.Quests.Done("two_copies"));
            Assert.AreEqual(0, Factions.Standing("crown"));

            // It starts from Varrow once you've noticed her second copies, or from Marsh.
            g.Quests.Stages.Remove("two_copies");
            g.World.SetFlag("varrow_post");
            var start = DialogueDb.Get("marsh").Nodes["start"];
            Assert.IsTrue(Opt(start, "Who writes to you").Show(c));
            Assert.IsTrue(DialogueDb.Get("varrow").Nodes["post"].Options.Any(o => o.Text.StartsWith("(Follow the evening post") && o.Show(c)));
        }

        [UnityTest]
        public IEnumerator CrownsBannerCommitsAndPays()
        {
            foreach (bool talk in new[] { true, false })
            {
                var g = GameRoot.I;
                var p = ActTwo(g, g.DebugStart(Still(talk ? 4 : 5), "paladin", "human"));
                yield return Frames(2);
                var c = new DCtx { P = p };
                var marsh = DialogueDb.Get("marsh");
                g.World.SetFlag("met_marsh");
                var commit = Opt(marsh.Nodes["start"], "We'll stand with the Crown");
                Assert.IsFalse(commit.Show(c), "not before hearing everyone out");
                HearAll();
                Assert.IsFalse(commit.Show(c), "not before one of the Crown's own jobs");
                g.Quests.Start("two_copies", p);
                g.Quests.Set("counted", 4, p);
                Assert.IsTrue(commit.Show(c));
                commit.Do(c);
                yield return ConfirmCommit(p);
                Assert.AreEqual("crown", g.World.CommittedFaction);
                Assert.AreEqual(1, g.Quests.Stage("crowns_banner"));
                Assert.AreEqual(Answers.Chosen, g.Quests.Stage(Answers.Id), "Answers moves on when you choose");
                g.Quests.Set("two_copies", 2, p);
                Assert.IsTrue(g.Quests.Done("two_copies"), "the Crown's own scouting stays open");

                g.Warp(p, "redoubt", new Vector2Int(33, 13), false);
                yield return Frames(3);
                int xp = p.Character.XP;
                if (talk) g.PacifyGroup("beacon_ashsworn", p);
                else yield return WinFight(g, p, "beacon_ashsworn");
                p.Hud.CloseAll();
                Assert.AreEqual(2, g.Quests.Stage("crowns_banner"));
                Assert.AreEqual(xp + 4 * 450 + 700, p.Character.XP, "talking them down pays like the fight");
                var notes = SpotNamed("beacon_notes");
                Assert.IsTrue(notes.Available(p));
                notes.Resolve(p, Rules.CheckOutcome.Success);
                p.Hud.CloseAll();
                Assert.AreEqual(3, g.Quests.Stage("crowns_banner"));
                Assert.IsTrue(g.World.LoreFound.Contains("beacon_orders"));
                Assert.AreEqual("banner_report", marsh.Start(c));
                int bond = g.World.DragonBond;
                xp = p.Character.XP;
                if (talk) Opt(marsh.Nodes["banner_report"], "Do it").Do(c);
                else marsh.Nodes["banner_report"].Options.Single(o => o.Check == Rules.Skill.AnimalHandling).OnSuccess(c);
                Assert.IsTrue(g.Quests.Done("crowns_banner"));
                Assert.AreEqual(xp + 3000, p.Character.XP);
                Assert.AreEqual(talk ? bond - 1 : bond, g.World.DragonBond);
                Assert.IsTrue(g.World.Flag("banner_crown"));
                Assert.AreEqual(1, p.Character.CountOf("crown_writ"));
                g.QuitToTitle();
                yield return Frames(2);
            }
        }

        // ============================================================ Task 3: the Academy

        [UnityTest]
        public IEnumerator ColdLanternsLitByDragonOrTorch()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(20), "wizard", "elf"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var perrin = DialogueDb.Get("perrin");
            perrin.Nodes["lamps"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("cold_lanterns"));
            g.Warp(p, "lantern_road", new Vector2Int(12, 14), false);
            yield return Frames(4);
            var lamps = UnlitBrazier.All.Where(b => b.ZoneId == "lantern_road").ToList();
            Assert.AreEqual(6, lamps.Count, "six cold lanterns on the road");
            var pet = g.DragonAgent.GetComponent<DragonPet>();
            foreach (var b in lamps.Take(4))
            {
                g.DragonAgent.Actor.PlaceAt(p.Zone, p.Avatar.MapPos);
                Assert.IsTrue(pet.Light(p, b), "the dragon lights it");
            }
            Assert.AreEqual(1, g.Quests.Stage("cold_lanterns"), "four isn't six");
            g.DragonAgent.gameObject.SetActive(false); // the dragon wanders off: a torch does the rest
            g.GiveItem(p, "torch");
            p.Avatar.LightTorch();
            foreach (var b in lamps.Skip(4)) b.Interact(p);
            g.DragonAgent.gameObject.SetActive(true);
            Assert.AreEqual(2, g.Quests.Stage("cold_lanterns"), "all six lit");
            Assert.IsTrue(lamps.All(b => b.Lit));
            var eyes = SpotNamed("lantern_eyes");
            Assert.IsTrue(eyes.Available(p));
            eyes.Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.IsTrue(g.World.Flag("lantern_eyes"));
            Assert.IsTrue(g.World.LoreFound.Contains("lantern_eyes"));
            Assert.AreEqual("lamps_done", perrin.Start(c));
            int xp = p.Character.XP;
            perrin.Nodes["lamps_done"].OnEnter(c);
            perrin.Nodes["lamps_done"].OnEnter(c);
            Assert.IsTrue(g.Quests.Done("cold_lanterns"));
            Assert.AreEqual(xp + 1000, p.Character.XP, "paid once");
            Assert.AreEqual(10, Factions.Standing("academy"));
        }

        [UnityTest]
        public IEnumerator LanternsLitBeforeTheQuestStillCount()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(21), "wizard", "elf"));
            yield return Frames(2);
            foreach (var (x, y) in new[] { (12, 13), (18, 13), (30, 13), (36, 13), (42, 13), (51, 11) })
                g.World.LitBraziers.Add(UnlitBrazier.KeyFor("lantern_road", new Vector2Int(x, y)));
            DialogueDb.Get("perrin").Nodes["lamps"].OnEnter(new DCtx { P = p });
            Assert.AreEqual(2, g.Quests.Stage("cold_lanterns"), "already lit: straight to the turn-in");
        }

        [UnityTest]
        public IEnumerator NinthCircleFullRun()
        {
            foreach (bool calm in new[] { true, false })
            {
                var g = GameRoot.I;
                var p = ActTwo(g, g.DebugStart(Still(calm ? 22 : 23), "wizard", "elf"));
                yield return Frames(2);
                var c = new DCtx { P = p };
                var ashby = DialogueDb.Get("ashby");
                g.World.SetFlag("met_ashby"); g.World.SetFlag("ashby_heard");
                var commit = Opt(ashby.Nodes["again"], "We'll help you finish");
                Assert.IsFalse(commit.Show(c));
                HearAll();
                g.Quests.Set("cold_lanterns", 3, p);
                Assert.IsTrue(commit.Show(c));
                commit.Do(c);
                yield return ConfirmCommit(p);
                Assert.AreEqual("academy", g.World.CommittedFaction);
                Assert.AreEqual(1, g.Quests.Stage("ninth_circle"));
                Assert.AreEqual("circle_task", ashby.Start(c));

                var varrow = DialogueDb.Get("varrow");
                Assert.IsTrue(Opt(varrow.Nodes["start"], "Ashby needs your focus stone").Show(c));
                varrow.Nodes["stone"].OnEnter(c);
                var oakes = DialogueDb.Get("oakes");
                var ask = oakes.Nodes["start"].Options.Single(o => o.Check == Rules.Skill.Persuasion);
                Assert.IsTrue(ask.Show(c), "Oakes needs persuading unless you saved his lilies");
                oakes.Nodes["stone_yes"].OnEnter(c);
                Assert.AreEqual(1, g.Quests.Stage("ninth_circle"));
                g.Warp(p, "academy", new Vector2Int(13, 32), false);
                yield return Frames(3);
                SpotNamed("starwell_stone").Resolve(p, Rules.CheckOutcome.Success);
                p.Hud.CloseAll();
                Assert.AreEqual(3, p.Character.CountOf("focus_stone"));
                Assert.AreEqual(2, g.Quests.Stage("ninth_circle"));

                g.Warp(p, "sealing_hall", new Vector2Int(12, 19), false);
                yield return Frames(3);
                SpotNamed("set_the_stones").Resolve(p, Rules.CheckOutcome.Success);
                p.Hud.CloseAll();
                Assert.AreEqual(3, g.Quests.Stage("ninth_circle"));
                yield return Frames(3);
                Assert.IsTrue(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(e => e.Group != null && e.Group.Id == "circle_bear"), "something is in the circle");
                int xp = p.Character.XP;
                if (calm) g.PacifyGroup("circle_bear", p);
                else yield return WinFight(g, p, "circle_bear");
                Assert.AreEqual(4, g.Quests.Stage("ninth_circle"));
                Assert.AreEqual(xp + 200, p.Character.XP, "calm or fight, the same XP");

                Assert.AreEqual("circle_report", ashby.Start(c));
                xp = p.Character.XP;
                Opt(ashby.Nodes["circle_report"], "It'll hold").Do(c);
                Assert.IsTrue(g.Quests.Done("ninth_circle"));
                Assert.AreEqual(xp + 3000, p.Character.XP);
                Assert.AreEqual(0, p.Character.CountOf("focus_stone"), "the stones go back to Ashby");
                Assert.AreEqual(1, p.Character.CountOf("ward_charm"));
                Assert.IsTrue(g.World.Flag("banner_academy"));
                Assert.AreEqual(30, Factions.Standing("academy"), "the major pays +30 (the lanterns were set directly here)");
                g.QuitToTitle();
                yield return Frames(2);
            }
        }

        [UnityTest]
        public IEnumerator NinthCircleSurvivesSaveLoad()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(24), "fighter", "human"));
            yield return Frames(2);
            Factions.Commit("academy", p);
            g.Quests.Start("ninth_circle", p);
            g.Quests.Set("ninth_circle", 3, p);
            g.Warp(p, "sealing_hall", new Vector2Int(12, 19), false);
            yield return Frames(3);
            g.PacifyGroup("circle_bear", p);
            Assert.AreEqual(4, g.Quests.Stage("ninth_circle"));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            p = g.Players[0];
            g.Warp(p, "sealing_hall", new Vector2Int(12, 19), false);
            yield return Frames(3);
            Assert.IsFalse(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(e => e.Group != null && e.Group.Id == "circle_bear" && e.Monster.Team == DnD.Rules.Team.Enemies), "no bear on load");
            Assert.AreEqual(4, g.Quests.Stage("ninth_circle"));
        }

        // ============================================================ Task 4: the Fae Courts

        [UnityTest]
        public IEnumerator FairyRingStaysAnActOneSecret()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(30), "bard", "human");
            yield return Frames(2);
            g.World.SetFlag(WorldDb.RingOpen);
            g.Warp(p, "forest", new Vector2Int(11, 19), false);
            yield return Frames(3);
            int lines = p.Hud.LogLines.Count;
            p.Avatar.PlaceAt(p.Zone, new Vector2Int(10, 19));
            yield return Frames(5);
            Assert.AreEqual("forest", p.Zone.Def.Id, "the glade stays shut in Act I");
            Assert.AreEqual(lines, p.Hud.LogLines.Count, "and nothing is narrated on the ring tile");
            var ring = SpotNamed("fairy_ring");
            Assert.IsTrue(ring.Available(p), "the Act I fairy-ring secret still works");
        }

        [UnityTest]
        public IEnumerator ThimbleOpensTheRingInActTwo()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(31), "bard", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var thimble = DialogueDb.Get("thimble");
            Assert.AreEqual("a2_ring", thimble.Start(c));
            Assert.IsNotNull(WorldDb.Current(WorldDb.Npcs["thimble"], 19, 0, g.World), "he waits at the ring in the evenings");
            Assert.AreEqual("forest", WorldDb.Current(WorldDb.Npcs["thimble"], 19, 1, g.World).Zone);
            var warp = WorldDb.Zones["forest"].Warps.Find(w => w.ToZone == "glade");
            Assert.IsFalse(warp.Open(g.World), "closed until he opens it");
            Opt(thimble.Nodes["a2_ring"], "Open it").Do(c);
            Assert.IsTrue(g.World.Flag(WorldDb.RingOpen));
            Assert.AreEqual("a2_again", thimble.Start(c));
            TakeWarp(g, p, "forest", 10, 19);
            yield return Frames(3);
            Assert.AreEqual("glade", p.Zone.Def.Id);
            AssertAllReachable(g, p.Zone, p.Avatar.MapPos);
            foreach (var hour in new[] { 12, 0 })
            {
                g.Clock.Minutes = hour * 60;
                yield return Frames(2);
                Assert.AreEqual(PlayerHud.DuskTint, p.Hud.AmbientTint, $"always dusk in the glade ({hour}:00)");
            }
        }

        [UnityTest]
        public IEnumerator CourtQuarrelEveryJudgementCompletes()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(32), "bard", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var sorrel = DialogueDb.Get("sorrel");
            sorrel.Nodes["intro"].OnEnter(c);
            Assert.IsTrue(Factions.Heard("fae"));
            sorrel.Nodes["quarrel"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("court_quarrel"));
            DialogueDb.Get("blossom_speaker").Nodes["start"].OnEnter(c);
            Assert.AreEqual(1, g.Quests.Stage("court_quarrel"), "one side isn't a hearing");
            DialogueDb.Get("frost_speaker").Nodes["start"].OnEnter(c);
            Assert.AreEqual(2, g.Quests.Stage("court_quarrel"));
            Assert.AreEqual("judge", sorrel.Start(c));
            var judge = sorrel.Nodes["judge"];
            void Reset() { g.Quests.Stages["court_quarrel"] = 2; g.Quests.Paid.Remove("court_quarrel"); g.World.Standing["fae"] = 0; }
            Reset(); Opt(judge, "The Blossoms").Do(c);
            Assert.IsTrue(g.Quests.Done("court_quarrel")); Assert.AreEqual(5, Factions.Standing("fae")); Assert.IsTrue(g.World.Flag("fae_judged_blossom"));
            Reset(); Opt(judge, "The Frost").Do(c);
            Assert.IsTrue(g.Quests.Done("court_quarrel")); Assert.AreEqual(5, Factions.Standing("fae"));
            var share = judge.Options.Single(o => o.Check == Rules.Skill.Persuasion);
            Reset(); share.OnSuccess(c);
            Assert.IsTrue(g.Quests.Done("court_quarrel")); Assert.AreEqual(15, Factions.Standing("fae"));
            Reset(); share.OnFailure(c);
            Assert.IsTrue(g.Quests.Done("court_quarrel")); Assert.AreEqual(0, Factions.Standing("fae"));
        }

        [UnityTest]
        public IEnumerator GreenBargainEveryRoute()
        {
            foreach (bool calm in new[] { true, false })
            {
                var g = GameRoot.I;
                var p = ActTwo(g, g.DebugStart(Still(calm ? 33 : 34), "druid", "elf"));
                yield return Frames(2);
                var c = new DCtx { P = p };
                var sorrel = DialogueDb.Get("sorrel");
                g.World.SetFlag("met_sorrel");
                HearAll();
                g.Quests.Set("court_quarrel", 3, p);
                var commit = Opt(sorrel.Nodes["start"], "We'll stand with the Courts");
                Assert.IsTrue(commit.Show(c));
                commit.Do(c);
                yield return ConfirmCommit(p);
                Assert.AreEqual("fae", g.World.CommittedFaction);
                Assert.AreEqual(1, g.Quests.Stage("green_bargain"));

                g.World.SetFlag(WorldDb.RingOpen);
                g.Warp(p, "glade", new Vector2Int(20, 27), false);
                yield return Frames(3);
                SpotNamed("throne_blossom_nature").Resolve(p, Rules.CheckOutcome.Success);
                p.Hud.CloseAll();
                Assert.IsFalse(SpotNamed("throne_blossom_song").Available(p), "one route is enough");
                // The Ember throne: the dragon lights the brazier, and the throne takes the token without a roll.
                g.Warp(p, "glade", new Vector2Int(31, 6), false);
                yield return Frames(3);
                var brazier = UnlitBrazier.All.Single(b => b.ZoneId == "glade");
                g.DragonAgent.Actor.PlaceAt(p.Zone, p.Avatar.MapPos);
                Assert.IsTrue(g.DragonAgent.GetComponent<DragonPet>().Light(p, brazier));
                SpotNamed("throne_ember").Interact(p);
                yield return Frames(2);
                p.Hud.CloseAll();
                Assert.IsTrue(g.World.Flag("throne_ember"), "the fire did the work");
                int xp = p.Character.XP;
                g.Warp(p, "glade", new Vector2Int(30, 23), false);
                yield return Frames(3);
                if (calm) g.PacifyGroup("frost_thorns", p);
                else yield return WinFight(g, p, "frost_thorns");
                p.Hud.CloseAll();
                Assert.AreEqual(xp + 2 * 450, p.Character.XP, "calm or fight, the same XP");
                Assert.IsTrue(g.World.Flag("throne_frost"), "dealing with the guardians wins the Frost throne");
                Assert.AreEqual(2, g.Quests.Stage("green_bargain"));

                Assert.AreEqual("bargain", sorrel.Start(c));
                var bargain = sorrel.Nodes["bargain"];
                bargain.Options.First(o => o.Check == Rules.Skill.Investigation).OnSuccess?.Invoke(c);
                sorrel.Nodes["clause"].OnEnter(c);
                Assert.IsTrue(g.World.Flag("fae_clause_found"));
                xp = p.Character.XP;
                Opt(bargain, "We'll sign it as written").Do(c);
                Assert.IsTrue(g.Quests.Done("green_bargain"));
                Assert.AreEqual(xp + 3000, p.Character.XP);
                Assert.IsTrue(g.World.Flag("fae_clause"), "signed as written: the dragon owes the Courts a favour");
                Assert.IsTrue(g.World.Flag("banner_fae"));
                Assert.AreEqual(3, p.Character.CountOf("bloom_draught"));
                g.QuitToTitle();
                yield return Frames(2);
            }
        }

        [UnityTest]
        public IEnumerator GreenBargainSurvivesSaveLoad()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(35), "druid", "elf"));
            yield return Frames(2);
            Factions.Commit("fae", p);
            g.Quests.Start("green_bargain", p);
            g.Warp(p, "glade", new Vector2Int(30, 23), false);
            yield return Frames(3);
            g.PacifyGroup("frost_thorns", p);
            g.World.SetFlag("throne_blossom");
            Assert.AreEqual(1, g.Quests.Stage("green_bargain"));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(1, g.Quests.Stage("green_bargain"));
            Assert.IsTrue(g.World.Flag("throne_blossom") && g.World.Flag("throne_frost"));
            Assert.IsTrue(g.World.PacifiedGroups.Contains("frost_thorns"), "the guardians stay calmed");
            g.World.SetFlag("throne_ember");
            Assert.AreEqual(2, g.Quests.Stage("green_bargain"), "the last token finishes it after a reload");
        }

        [UnityTest]
        public IEnumerator CrownsBannerSurvivesSaveLoad()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(6), "fighter", "human"));
            yield return Frames(2);
            Factions.Commit("crown", p);
            g.Quests.Start("crowns_banner", p);
            g.Warp(p, "redoubt", new Vector2Int(33, 13), false);
            yield return Frames(3);
            yield return WinFight(g, p, "beacon_ashsworn");
            p.Hud.CloseAll();
            Assert.AreEqual(2, g.Quests.Stage("crowns_banner"));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            p = g.Players[0];
            g.Warp(p, "redoubt", new Vector2Int(33, 13), false);
            yield return Frames(3);
            Assert.IsTrue(g.World.DefeatedGroups.Contains("beacon_ashsworn"));
            Assert.IsFalse(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(e => e.Group != null && e.Group.Id == "beacon_ashsworn" && !e.Monster.IsDown && e.Monster.Team == DnD.Rules.Team.Enemies), "the Ashsworn don't come back: " + string.Join(";", Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Where(e => e.Group != null && e.Group.Id == "beacon_ashsworn").Select(e => $"{e.Monster.Name} hp{e.Monster.HP} {e.Zone?.Def.Id} {e.enabled}")));
            Assert.IsTrue(SpotNamed("beacon_notes").Available(p));
            Assert.AreEqual("crown", g.World.CommittedFaction);
        }
    }
}
