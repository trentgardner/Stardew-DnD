using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Phase 5's new places: the Fallen Star journey, the crater, the warrens and the shrine.</summary>
    public class PlacesTests
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

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Saves, then rewrites the file as an old version-1 save with fallen_star at `oldStage`.</summary>
        static string OldSave(GameRoot g, int oldStage, bool paid)
        {
            string path = SaveSystem.Save(false);
            var d = SaveSystem.Read(path);
            d.Version = 1;
            d.Quests.RemoveAll(kv => kv.K == "fallen_star");
            d.Quests.Add(new KV("fallen_star", oldStage));
            d.QuestsPaid.Remove("fallen_star");
            if (paid) d.QuestsPaid.Add("fallen_star");
            File.WriteAllText(path, JsonUtility.ToJson(d, true));
            return path;
        }

        // ============================================================ Task 1: stages, migration, map chars

        [UnityTest]
        public IEnumerator OldSaveFallenStarMigrates()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);

            var d = new SaveData { Version = 1 };
            d.Quests.Add(new KV("fallen_star", 2));
            SaveSystem.Migrate(d);
            Assert.AreEqual(2, d.Version);
            Assert.AreEqual(FallenStar.Egg, d.Quests.Find(kv => kv.K == "fallen_star").V, "old stage 2 (found the egg) is the new Egg stage");
            SaveSystem.Migrate(d);
            Assert.AreEqual(FallenStar.Egg, d.Quests.Find(kv => kv.K == "fallen_star").V, "migrating twice changes nothing");

            string path = OldSave(g, 4, true);
            g.LoadGame(path);
            yield return Frames(2);
            p = g.Players[0];
            Assert.AreEqual(FallenStar.Done, g.Quests.Stage("fallen_star"), "old stage 4 (done) is the new Done stage");
            int xp = p.Character.XP, cp = p.Character.Copper;
            g.Quests.Set("fallen_star", FallenStar.Done, p);
            Assert.AreEqual(xp, p.Character.XP, "a finished main quest never pays twice");
            Assert.AreEqual(cp, p.Character.Copper);
        }

        [UnityTest]
        public IEnumerator SavedPositionOnAWallFallsBackToSpawn()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            g.Warp(p, "mine", WorldDb.Zones["mine"].DefaultSpawn, false);
            yield return Frames(2);
            string path = SaveSystem.Save(false);
            var d = SaveSystem.Read(path);
            var mine = WorldDb.Zones["mine"];
            Assert.AreEqual('X', mine.Map[0][0]);
            d.Players[0].X = 0; d.Players[0].Y = 0;
            File.WriteAllText(path, JsonUtility.ToJson(d, true));
            g.LoadGame(path);
            yield return Frames(2);
            p = g.Players[0];
            Assert.AreEqual("mine", p.Zone.Def.Id);
            Assert.AreEqual(mine.DefaultSpawn, p.MapPos);
        }

        [UnityTest]
        public IEnumerator NewMapCharsBuild()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            var warnings = new List<string>();
            void Log(string msg, string st, LogType t) { if (t != LogType.Log) warnings.Add(msg); }
            Application.logMessageReceived += Log;
            var def = new ZoneDef
            {
                Id = "test_chars", Name = "Test", Dark = true, DefaultSpawn = new Vector2Int(1, 1),
                Map = new[] {
                    "XXXXXXXXXXXXXXXX",
                    "XdeHiVQIJZ/@%..X",
                    "XXXXXXXXXXXXXXXX" },
            };
            Zone z;
            try { z = Zone.Build(def, 90, new GameObject("test_root").transform); }
            finally { Application.logMessageReceived -= Log; }
            Assert.IsEmpty(warnings, string.Join("\n", warnings));
            foreach (char c in "eHiZJ") Assert.IsFalse(z.IsWalkable(new Vector2Int(def.Map[1].IndexOf(c), 1)), $"'{c}' blocks");
            foreach (char c in "dVQ") Assert.IsTrue(z.IsWalkable(new Vector2Int(def.Map[1].IndexOf(c), 1)), $"'{c}' is walkable");
            Assert.IsFalse(z.LineOfSight(new Vector2Int(1, 1), new Vector2Int(4, 1)), "crystal clusters and huts block sight");
            Object.Destroy(z.transform.parent.gameObject);
        }
        // ============================================================ Task 2: Starfall Crater

        /// <summary>Steps onto a warp tile's destination the way walking onto it would.</summary>
        static void TakeWarp(GameRoot g, PlayerSlot p, string zone, int x, int y)
        {
            var w = WorldDb.Zones[zone].Warps.Find(v => v.At == new Vector2Int(x, y));
            Assert.IsNotNull(w, $"{zone} has a warp at ({x},{y})");
            g.Warp(p, w.ToZone, w.ToPos, false);
        }

        /// <summary>A walkable tile next to `m` that can be reached from `from` (secrets and NPC props are solid).</summary>
        static bool CanReachBeside(Zone z, Vector2Int from, Vector2Int m)
        {
            if (z.IsWalkable(m) && (m == from || z.FindPath(from, m).Count > 0)) return true;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var t = m + d;
                if (z.IsWalkable(t) && (t == from || z.FindPath(from, t, null, 20000).Count > 0)) return true;
            }
            return false;
        }

        static SecretInteractable Spot(string id) => Object.FindObjectsByType<SecretInteractable>().Single(s => s.Def.Id == id);

        [UnityTest]
        public IEnumerator CraterIsReachableFromWhisperwood()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "ranger", "elf");
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(30, 3), false);
            Assert.IsTrue(g.Zones["forest"].FindPath(new Vector2Int(30, 3), new Vector2Int(30, 1), null, 20000).Count > 0, "the north trail is open");
            TakeWarp(g, p, "forest", 30, 0);
            yield return Frames(2);
            Assert.AreEqual("crater", p.Zone.Def.Id);
            var z = p.Zone;
            var start = p.MapPos;
            foreach (var kv in z.Def.Secrets) Assert.IsTrue(CanReachBeside(z, start, kv.Key), $"secret {kv.Value.Id} at {kv.Key}");
            var perrin = WorldDb.Current(WorldDb.Npcs["perrin"], 12, 0, g.World);
            Assert.AreEqual("crater", perrin.Zone);
            Assert.IsTrue(CanReachBeside(z, start, perrin.Pos), "Perrin's camp");
            var back = z.Def.Warps.Find(w => w.ToZone == "forest");
            Assert.IsTrue(z.FindPath(start, back.At, null, 20000).Count > 0 || start == back.At);
            TakeWarp(g, p, "crater", back.At.x, back.At.y);
            yield return Frames(2);
            Assert.AreEqual("forest", p.Zone.Def.Id);
        }

        [UnityTest]
        public IEnumerator CraterHollowAndTracksAdvanceTheStar()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "ranger", "elf");
            yield return Frames(2);
            g.Warp(p, "crater", WorldDb.Zones["crater"].DefaultSpawn, false);
            yield return Frames(2);
            var hollow = Spot("crater_hollow"); var tracks = Spot("crater_tracks");
            Assert.IsFalse(hollow.Available(p), "no quest, no hollow to study");
            g.Quests.Start("fallen_star", p);
            Assert.IsTrue(hollow.Available(p)); Assert.IsFalse(tracks.Available(p));
            hollow.Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(FallenStar.Trail, g.Quests.Stage("fallen_star"));
            Assert.IsFalse(hollow.Available(p)); Assert.IsTrue(tracks.Available(p));
            tracks.Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(FallenStar.Warrens, g.Quests.Stage("fallen_star"));
            Assert.IsFalse(tracks.Available(p));
        }
    }
}
