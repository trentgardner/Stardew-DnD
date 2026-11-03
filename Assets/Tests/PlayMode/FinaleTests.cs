using System.Collections;
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
    /// <summary>Act III Phase 6: the finales (phase beats, allies, the streets, the flight, the bosses).</summary>
    public class FinaleTests
    {
        string _tempSaves;
        const string Arena = "lantern_road";
        static readonly Vector2Int BossAt = new Vector2Int(10, 17), HeroAt = new Vector2Int(10, 18);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            MonstersDb.All["test_phased"] = new MonsterDef
            {
                Id = "test_phased", Name = "Test Phased", SpriteId = "mon_ogre", AC = 18, HitDice = DiceExpr.Parse("10d10+100"), CR = 10, XP = 5900,
                Attacks = { new MonsterAttackDef { Name = "Rend", ToHit = 10, Damage = DiceExpr.Parse("2d6+6"), DamageType = DamageType.Slashing, ReachFt = 10 } },
                LegendaryPerRound = 1,
                LegendaryActions = { new LegendaryActionDef { Name = "Tail", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Rend", RangeFt = 15 } },
                Phases =
                {
                    new BossPhaseDef { At = 0.66, Beats = new[] { "beat A1", "beat A2" } },
                    new BossPhaseDef { At = 0.33, Beats = new[] { "beat B" } },
                },
            };
            var grp = new EnemyGroupDef { Id = "test_phased_grp", Style = EncounterStyle.None, Unique = true };
            grp.Members.Add(new EnemySpawn("test_phased", BossAt.x, BossAt.y));
            WorldDb.Zones[Arena].EnemyGroups.Add(grp);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == "test_phased_grp");
            MonstersDb.All.Remove("test_phased");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        /// <summary>The beat on top of the stack (closed screens linger while the initiative dice is open, so skip them).</summary>
        static MessageScreen Beat(PlayerSlot p) => p.Hud.Top is MessageScreen m && !m.Closed ? m : null;

        /// <summary>A fight with the phased test boss, paused at initiative (Still input never rolls it): the test drives it.</summary>
        static CombatInstance PhasedFight(GameRoot g, PlayerSlot p, out Unit boss, out Unit hero)
        {
            g.Warp(p, Arena, HeroAt, false);
            g.StartGroupCombat("test_phased_grp", p);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            boss = c.Units.Single(u => u.C is Monster m && m.Def.Id == "test_phased");
            hero = c.Units.Single(u => u.Owner == p);
            hero.A.PlaceAt(c.Zone, HeroAt);
            boss.A.PlaceAt(c.Zone, BossAt);
            return c;
        }

        // ================================================================== Task 2: phase beats

        [UnityTest]
        public IEnumerator HugeHitStillPlaysEveryBeat()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(3);
            var c = PhasedFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;

            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.IsTrue(c.BeatsPlaying, "the fight stops for the phase");
            Assert.AreEqual("beat A1", Beat(p)?.Body);
            Assert.IsFalse(m.IsDown, "not down at a threshold");
            Beat(p).Close();
            yield return Frames(2);
            Assert.AreEqual("beat A2", Beat(p)?.Body, "every beat, in order");
            Beat(p).Close();
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying, "the fight goes on");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("phase 2")), "logged (phase 1 is the opening)");

            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.AreEqual("beat B", Beat(p)?.Body);
            Beat(p).Close();
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying);

            c.DebugDamage(boss, 9999);
            Assert.IsTrue(m.IsDown, "the last phase can be finished");
        }

        [UnityTest]
        public IEnumerator AiWaitsForBeats()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "dwarf");
            yield return Frames(3);
            var c = PhasedFight(g, p, out var boss, out var hero);
            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.IsTrue(c.BeatsPlaying);
            int hp = hero.C.HP;
            var ai = g.StartCoroutine(c.DebugAiTurn(boss));
            yield return Frames(30);
            Assert.AreEqual(hp, hero.C.HP, "nothing hits you while the story is on screen");
            Assert.IsFalse(c.LogLines.Any(l => l.Contains("Rend")), "the boss hasn't swung");
            while (Beat(p) != null) { Beat(p).Close(); yield return Frames(2); }
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying);
        }
    }
}
