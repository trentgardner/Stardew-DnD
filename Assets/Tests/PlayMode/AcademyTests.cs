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
    }
}
