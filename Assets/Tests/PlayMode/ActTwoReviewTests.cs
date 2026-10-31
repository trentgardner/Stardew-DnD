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
    /// <summary>Act II Phase 1: fixes from the final whole-branch review.</summary>
    public class ActTwoReviewTests
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

        /// <summary>Runs a coroutine to the end right now, including the coroutines it yields (waits are skipped).</summary>
        static void RunNow(IEnumerator e)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(e);
            int guard = 0;
            while (stack.Count > 0 && guard++ < 100000)
            {
                var top = stack.Peek();
                if (!top.MoveNext()) { stack.Pop(); continue; }
                if (top.Current is IEnumerator nested) stack.Push(nested);
            }
        }

        /// <summary>A companion of the given class and level, played by the AI.</summary>
        static (CompanionAgent comp, PlayerCharacter pc) Buddy(GameRoot g, PlayerSlot p, string cls, int level)
        {
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices(cls, "dwarf", "Buddy"));
            pc.XP = 999999;
            while (pc.Level < level) pc.LevelUp(pc.AutoPlan(), null);
            var comp = g.SpawnCompanion(p, pc, "hums");
            comp.Manual = false;
            return (comp, pc);
        }

        static IEnumerator ToTheWolves(GameRoot g, PlayerSlot p)
        {
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator PrepareScreenOffersThirdLevelSpellsAtFive()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "cleric", "human", level: 5);
            yield return Frames(2);
            var book = new SpellbookScreen();
            p.Hud.Push(book);
            yield return Frames(2);
            book.SetPrepareMode(true);
            Assert.IsTrue(book.RowTexts.Any(t => t.Contains("(L3)")), "a level 5 cleric can prepare 3rd-level spells");
        }

        [UnityTest]
        public IEnumerator DivineInterventionKeepsConcentration()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "human", level: 5);
            var (_, cleric) = Buddy(g, p, "cleric", 10);
            yield return Frames(2);
            yield return ToTheWolves(g, p);
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.C == cleric);
            if (me.Cb == null) me.Cb = new Combatant(me.C);
            cleric.AddEffect(new ActiveEffect { Id = "divine_ready", Name = "Divine Intervention", Source = cleric, RoundsLeft = 1 });
            var run = c.DebugCast(me, "spirit_guardians", me.Pos);
            while (run.MoveNext()) { }
            Assert.AreEqual("spirit_guardians", cleric.ConcentratingOn, "a free spell still needs (and holds) concentration");
            Assert.AreEqual(1, c.Zones.Count, "and the guardians stay");
        }

        [UnityTest]
        public IEnumerator BanishingTheLastEnemyDoesNotEndTheFight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(3) { ConfirmChance = 0.4f, EndTurnChance = 0.05f, MenuChance = 0, CancelChance = 0 }, "paladin", "human", level: 9);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat(g.DebugSpawnGroup(p, "troll"), p);
            var c = g.Combats.Single();
            var troll = c.Units.First(u => u.C.Team == Team.Enemies);
            var pc = (PlayerCharacter)p.Character;
            pc.ConcentratingOn = "banishment";
            troll.C.AddEffect(EffectLibrary.Create("banished", pc, 15));
            Time.timeScale = 4;
            float t0 = Time.realtimeSinceStartup;
            int round0 = c.Enc.Round;
            while (Time.realtimeSinceStartup - t0 < 20f && !c.Finished && c.Enc.Round < round0 + 2) yield return null;
            Time.timeScale = 1;
            Assert.GreaterOrEqual(c.Enc.Round, round0 + 1, "turns went by (the fight really ran)");
            Assert.IsFalse(c.Finished, "a banished foe is only away: the fight goes on until it returns or is gone for good");
        }

        [UnityTest]
        public IEnumerator CompanionWarlockFiresTwoBeamsAtFive()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human", level: 5);
            var (_, warlock) = Buddy(g, p, "warlock", 5);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat(g.DebugSpawnGroup(p, "troll"), p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.C == warlock);
            var troll = c.Units.First(u => u.C.Team == Team.Enemies);
            troll.C.MaxHP = troll.C.HP = 999;
            if (me.Cb == null) me.Cb = new Combatant(me.C);
            me.Cb.Turn.Reset(me.C);
            int before = c.SpellAttackRolls;
            RunNow(c.DebugAiTurn(me));
            Assert.AreEqual(2, c.SpellAttackRolls - before, "Eldritch Blast: two beams at level 5, for companions too. Log: " + string.Join(" | ", c.LogLines.Skip(System.Math.Max(0, c.LogLines.Count - 8)))
                + $" | dist {Zone.DistanceFt(me.Pos, troll.Pos)} cantrips {string.Join(",", warlock.Cantrips)} inv {string.Join(",", warlock.Invocations)}");
        }

        [UnityTest]
        public IEnumerator ConcentrationEndsWithTheFight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "cleric", "human", level: 5);
            yield return Frames(2);
            yield return ToTheWolves(g, p);
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            p.Character.ConcentratingOn = "spirit_guardians";
            c.Retreat();
            yield return Frames(2);
            Assert.IsNull(p.Character.ConcentratingOn, "nobody is still concentrating on a spell from a finished fight");
        }
    }
}
