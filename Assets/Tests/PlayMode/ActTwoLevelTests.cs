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

        [UnityTest]
        public IEnumerator TrollMultiattackAndHoundBreathFightToTheEnd()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(7) { ConfirmChance = 0.35f, EndTurnChance = 0.01f }, "fighter", "human", level: 7);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            var id = g.DebugSpawnGroup(p, "troll", "cinder_hound");
            g.StartGroupCombat(id, p);
            Assert.AreEqual(1, g.Combats.Count);
            var c = g.Combats.Single();
            Assert.IsTrue(c.Units.Any(u => u.C is Monster m && m.Def.Id == "troll"), "the troll joins the fight");
            Time.timeScale = 6;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 150f) yield return null;
            Time.timeScale = 1;
            Assert.IsFalse(g.AnyInCombat, "the fight finishes (won or lost)");
        }

        [UnityTest]
        public IEnumerator ExtraAttackSurvivesRetargetAfterKill()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human", level: 5);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            var first = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            Assert.AreEqual(ActionCost.Action, first.Cost);
            StringAssert.Contains("2 attacks", first.Tooltip, "the button says Extra Attack gives two");
            // The state right after the first swing of the Attack action killed wolf A:
            var wolves = c.Units.Where(x => x.C.Team == Team.Enemies).ToList();
            wolves[0].C.HP = 0;
            u.Cb.Turn.ActionUsed = true;
            u.Cb.Turn.AttacksLeftInAction = ((PlayerCharacter)u.C).AttacksPerAction - 1;
            var second = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("2/2"));
            Assert.IsNotNull(second, "the second attack is still on the bar after the action is spent");
            Assert.IsTrue(second.Enabled, "and usable");
            Assert.AreEqual(ActionCost.Free, second.Cost, "it doesn't need another action");
            Assert.AreEqual(Targeting.Enemy, second.Target, "it picks its own target (a different wolf)");
            Assert.IsTrue(c.ActionsFor(p).First(a => a.Name.StartsWith("Attack (")).Name.EndsWith("2/2"), "the plain Attack button becomes 'Attack 2/2' (clicking an enemy uses it)");
        }

        [UnityTest]
        public IEnumerator PaladinAuraUsesPositionsInAFight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "paladin", "human", level: 6);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var pal = (PlayerCharacter)p.Character;
            int bonus = System.Math.Max(1, pal.Mod(Ability.CHA));
            Assert.AreEqual(bonus, Auras.ProtectionBonus(pal), "the paladin's own saves get the aura");
            var wolf = g.Combats.Single().Units.First(u => u.C.Team == Team.Enemies);
            Assert.AreEqual(0, Auras.ProtectionBonus(wolf.C), "enemies never do, however close");
        }
    }
}
