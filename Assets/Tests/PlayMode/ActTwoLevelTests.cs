using System.Collections;
using System.Linq;
using System.IO;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 1: levels 5-10 in the running game.</summary>
    public class ActTwoLevelTests
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

        [UnityTest]
        public IEnumerator SecondFightingStyleSurvivesASaveAndTheCapResetsOnLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human", level: 7);
            yield return null;
            var pc = p.Character;
            Assert.AreEqual(7, pc.Level, "debug start can preview Act II levels");
            Assert.IsNotNull(pc.FightingStyle2, "champion 7 picked an additional fighting style");
            Assert.AreNotEqual(pc.FightingStyle, pc.FightingStyle2);
            string style2 = pc.FightingStyle2;
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return null;
            Assert.AreEqual(style2, g.Players[0].Character.FightingStyle2, "the second style is saved");
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap, "loading (an Act I save) puts the cap back to 4");
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator WallOfFireBurnsAtTurnEnd()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "wizard", "human", level: 9);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies && u.Active);
            var pc = (PlayerCharacter)p.Character;
            pc.ConcentratingOn = "wall_of_fire";
            var wall = ZoneRules.Create(pc, SpellsDb.Get("wall_of_fire"), 4, wolf.Pos.x, wolf.Pos.y, new System.Collections.Generic.List<(int x, int y)> { (wolf.Pos.x, wolf.Pos.y) });
            c.AddZone(wall);
            Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(r => r.gameObject.name == "zone_wall_of_fire"), "the wall is drawn on the map");
            wolf.C.MaxHP = wolf.C.HP = 500;
            c.TriggerZones(wolf);
            Assert.Less(wolf.C.HP, 500, "a wolf standing in the wall burns");
            int after = wolf.C.HP;
            c.TriggerZones(wolf);
            Assert.AreEqual(after, wolf.C.HP, "only once per turn");
            pc.ConcentratingOn = null;
            c.DrawZones();
            Assert.AreEqual(0, c.Zones.Count, "losing concentration puts the wall out");
        }
    }
}
