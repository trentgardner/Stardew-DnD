using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 5: the road to the Emberhorn, your side's ally, and the Second Ideal.</summary>
    public class EmberhornTests
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
        static SecretInteractable SpotNamed(string id) => Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == id);

        static PlayerSlot ActTwo(GameRoot g, PlayerSlot p)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            return p;
        }

        /// <summary>Act II, chosen a side and finished its major quest (via the banner flag).</summary>
        static void SideDone(GameRoot g, PlayerSlot p, string faction)
        {
            Factions.Commit(faction, p);
            g.World.SetFlag("banner_" + faction);
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

        [UnityTest]
        public IEnumerator HighRoadOpensWithYourBanner()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(1), "fighter", "human"));
            yield return Frames(2);
            Factions.Commit("fae", p);
            Assert.AreEqual(Answers.Chosen, g.Quests.Stage(Answers.Id));
            var warp = WorldDb.Zones["cragfoot"].Warps.Find(w => w.ToZone == "emberhorn_road");
            Assert.IsFalse(warp.Open(g.World), "the rockslide blocks it");
            StringAssert.Contains("rockslide", warp.ClosedText);
            int xp = p.Character.XP;
            g.World.SetFlag("banner_fae");
            Assert.AreEqual(Answers.Allies, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(xp + 500, p.Character.XP);
            Assert.IsTrue(g.World.Flag(WorldDb.EmberhornOpen));
            Assert.IsTrue(warp.Open(g.World));
            g.Warp(p, warp.ToZone, warp.ToPos, false);
            yield return Frames(3);
            Assert.AreEqual(Answers.OnTheRoad, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(3, g.World.BlightStage, "the heaviest ash on the mountain road");
            var z = p.Zone;
            foreach (var kv in z.Def.Secrets) Assert.IsTrue(CanReachBeside(z, p.Avatar.MapPos, kv.Key), $"secret {kv.Value.Id}");
            foreach (var grp in z.Def.EnemyGroups) foreach (var m in grp.Members) Assert.IsTrue(CanReachBeside(z, p.Avatar.MapPos, m.Pos), $"{grp.Id} at {m.Pos}");
            foreach (var w in z.Def.Warps) Assert.IsTrue(CanReachBeside(z, p.Avatar.MapPos, w.At), $"warp at {w.At}");
            Assert.IsTrue(CanReachBeside(z, p.Avatar.MapPos, new Vector2Int(10, 17)), "the Last Camp");
        }

        [UnityTest]
        public IEnumerator TheAllyMatchesYourSide([Values("crown", "academy", "fae", "hunters")] string side)
        {
            var g = GameRoot.I;
            var p1 = ActTwo(g, g.DebugStart(Still(2), "fighter", "human"));
            var p2 = g.DebugStart(Still(3), "wizard", "elf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            SideDone(g, p1, side);
            string ally = new System.Collections.Generic.Dictionary<string, string> { ["crown"] = "ally_holloway", ["academy"] = "ally_perrin", ["fae"] = "ally_thimble", ["hunters"] = "ally_odo" }[side];
            foreach (var id in new[] { "ally_holloway", "ally_perrin", "ally_thimble", "ally_odo" })
            {
                var e = WorldDb.Current(WorldDb.Npcs[id], 12, 1, g.World);
                if (id == ally) Assert.AreEqual("emberhorn_road", e?.Zone, $"{id} waits at the Last Camp");
                else Assert.IsNull(e, $"{id} isn't there for {side}");
            }
            g.Warp(p1, "emberhorn_road", new Vector2Int(9, 17), false);
            yield return Frames(3);
            var d = DialogueDb.Get(ally);
            var c1 = new DCtx { P = p1 };
            int Gifts() => g.Players.Where(x => x.Character != null).Sum(x =>
                x.Character.CountOf("potion_greater_healing") + x.Character.CountOf("bloom_draught") + x.Character.CountOf("ember_of_return"));
            int before = Gifts();
            Assert.AreEqual("greet", d.Start(c1));
            d.Nodes["greet"].OnEnter(c1);
            int after = Gifts();
            Assert.Greater(after, before, "a gift");
            d.Nodes["greet"].OnEnter(new DCtx { P = p2 });
            Assert.AreEqual("again", d.Start(new DCtx { P = p2 }), "once for the whole party");
            Assert.AreEqual(after, Gifts());
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual("again", DialogueDb.Get(ally).Start(new DCtx { P = g.Players[0] }), "not again after a reload");
            if (side == "fae")
            {
                g.World.SetFlag("fae_clause");
                StringAssert.Contains("owes us one", DialogueDb.Get(ally).Nodes["greet"].Text(new DCtx { P = g.Players[0] }));
            }
            if (side == "hunters") Assert.IsTrue(g.World.Flag(WorldDb.AllyGift));
        }

        [UnityTest]
        public IEnumerator GateEndsTheRoadForNow()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(4), "fighter", "human"));
            yield return Frames(2);
            SideDone(g, p, "crown");
            g.Warp(p, "emberhorn_road", new Vector2Int(55, 14), false);
            yield return Frames(3);
            Assert.AreEqual(Answers.OnTheRoad, g.Quests.Stage(Answers.Id));
            int xp = p.Character.XP;
            SpotNamed("emberhorn_gate").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(Answers.Gate, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(xp + 1500, p.Character.XP);
            var gate = WorldDb.Zones["emberhorn_road"].Warps.Find(w => w.At == new Vector2Int(59, 14));
            Assert.IsFalse(gate.Open(g.World), "the mountain waits for Act II's finale");
            StringAssert.Contains("later update", gate.ClosedText);
        }

        [UnityTest]
        public IEnumerator PhaseFourSaveContinues()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(5), "fighter", "human"));
            yield return Frames(2);
            Factions.Commit("hunters", p);
            g.World.Flags.Add("banner_hunters"); // set the old way: no FlagSet event, like a Phase 4 save
            Assert.AreEqual(Answers.Chosen, g.Quests.Stage(Answers.Id));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(Answers.Allies, g.Quests.Stage(Answers.Id), "a finished major moves Answers on after loading");
            Assert.IsTrue(g.World.Flag(WorldDb.EmberhornOpen));
        }

        // ============================================================ Task 6: acceptance

        [UnityTest, Timeout(600000)]
        public IEnumerator BlightAndRoadGoldenPath()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(20), "ranger", "elf"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            Assert.AreEqual(1, g.World.BlightStage, "Act II: the ash has begun");
            string calm = DialogueDb.Get("elowen").Nodes["start"].Text(c);
            Factions.Commit("hunters", p);
            Assert.AreEqual(2, g.World.BlightStage, "a side chosen: the ash thickens");
            Assert.AreNotEqual(calm, DialogueDb.Get("elowen").Nodes["start"].Text(c), "Elowen is frightened now");
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(3);
            Assert.IsTrue(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(e => e.Group != null && e.Group.Id == "blight_wolves_forest"), "ember-eyed wolves in the Whisperwood");
            g.Quests.Start("long_hunt", p);
            g.Quests.Set("long_hunt", QuestDb.Get("long_hunt").Last, p);
            Assert.AreEqual(Answers.Allies, g.Quests.Stage(Answers.Id));
            var warp = WorldDb.Zones["cragfoot"].Warps.Find(w => w.ToZone == "emberhorn_road");
            g.Warp(p, warp.ToZone, warp.ToPos, false);
            yield return Frames(3);
            Assert.AreEqual(Answers.OnTheRoad, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(3, g.World.BlightStage);
            DialogueDb.Get("ally_odo").Nodes["greet"].OnEnter(c);
            Assert.IsTrue(g.World.Flag(WorldDb.AllyGift));
            SpotNamed("second_ideal_history").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            SpotNamed("emberhorn_gate").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(Answers.Gate, g.Quests.Stage(Answers.Id));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(Answers.Gate, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(3, g.World.BlightStage);
            Assert.IsTrue(g.World.Flag(WorldDb.OathSecond) && g.World.Flag(WorldDb.AllyGift) && g.World.Flag(WorldDb.EmberhornOpen));
        }

        [UnityTest]
        public IEnumerator CaptureBlightAndRoad()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(21), "paladin", "human"));
            yield return Frames(2);
            Factions.Commit("academy", p);
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
            yield return At("town", 24, 17, "act2_blight_town");
            yield return At("forest", 38, 24, "act2_blight_forest");
            g.World.SetFlag("banner_academy");
            yield return At("emberhorn_road", 9, 15, "act2_blight_last_camp");
            yield return At("emberhorn_road", 25, 9, "act2_blight_waystation");
            yield return At("emberhorn_road", 44, 14, "act2_blight_grey_vigil");
            yield return At("emberhorn_road", 54, 14, "act2_blight_gate");
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

        // ============================================================ Task 5: the Second Ideal

        [UnityTest]
        public IEnumerator SecondIdealByEitherRoute()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(6), "cleric", "dwarf"));
            yield return Frames(2);
            g.Warp(p, "emberhorn_road", new Vector2Int(25, 7), false);
            yield return Frames(3);
            Assert.IsFalse(SpotNamed("second_ideal_religion").Available(p), "you need the First Ideal's words");
            SpotNamed("second_ideal_history").Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.IsTrue(g.World.Flag(WorldDb.OathSecond));
            Assert.IsTrue(g.World.LoreFound.Contains("second_ideal"));
            StringAssert.Contains(WorldDb.SecondIdeal, LoreDb.Get("second_ideal").Text);

            g.QuitToTitle();
            yield return Frames(2);
            p = ActTwo(g, g.DebugStart(Still(7), "cleric", "dwarf"));
            yield return Frames(2);
            g.World.SetFlag("secret:tower_inscription");
            g.Warp(p, "emberhorn_road", new Vector2Int(25, 7), false);
            yield return Frames(3);
            var rel = SpotNamed("second_ideal_religion");
            Assert.IsTrue(rel.Available(p));
            Assert.AreEqual(10, rel.Def.DC);
            rel.Resolve(p, Rules.CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.IsTrue(g.World.Flag(WorldDb.OathSecond));
            Assert.IsFalse(SpotNamed("second_ideal_history").Available(p) && !g.World.Flag(WorldDb.OathSecond));
        }

        [UnityTest]
        public IEnumerator SecondIdealInspiresOnLongRest()
        {
            var g = GameRoot.I;
            var p1 = ActTwo(g, g.DebugStart(Still(8), "fighter", "dwarf")); // not human: humans already get Inspiration on a long rest (Resourceful)
            var p2 = g.DebugStart(Still(9), "wizard", "elf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            foreach (var p in new[] { p1, p2 }) p.Character.HeroicInspiration = false;
            g.BeginLongRest(p1); g.BeginLongRest(p2);
            yield return Frames(3);
            Assert.IsFalse(p1.Character.HeroicInspiration || p2.Character.HeroicInspiration, "nothing without the Ideal");
            g.World.SetFlag(WorldDb.OathSecond);
            g.BeginLongRest(p1); g.BeginLongRest(p2);
            yield return Frames(3);
            Assert.IsTrue(p1.Character.HeroicInspiration && p2.Character.HeroicInspiration, "the Second Ideal steadies everyone");
        }
    }
}
