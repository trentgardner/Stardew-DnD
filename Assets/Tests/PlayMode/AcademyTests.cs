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
    }
}
