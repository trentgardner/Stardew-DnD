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
    }
}
