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
    /// <summary>Act III Phase 1: levels 11-13 in the running game.</summary>
    public class ActThreeLevelTests
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
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>A debug start at `level`, warped into the forest wolves' fight.</summary>
        static IEnumerator WolfFight(string cls, int level, int seed, System.Action<PlayerSlot, CombatInstance, Unit> got)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(seed), cls, "human", level: level);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            got(p, c, u);
        }

        static void Run(CombatAction a, Unit u, Vector2Int tile)
        {
            var run = a.Run(u, tile);
            while (run != null && run.MoveNext()) { }
        }

        [UnityTest]
        public IEnumerator ThreeAttacksSurviveRetargetAndActionSurge()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("fighter", 11, 1, (a, b, d) => { p = a; c = b; u = d; });
            var pc = (PlayerCharacter)u.C;
            Assert.AreEqual(11, pc.Level, "debug start previews Act III levels");
            Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap);
            var first = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            StringAssert.Contains("3 attacks", first.Tooltip, "Two Extra Attacks: three per Attack action");
            // Right after attack 1 of the action killed wolf A:
            var wolves = c.Units.Where(x => x.C.Team == Team.Enemies).ToList();
            wolves[0].C.HP = 0;
            u.Cb.Turn.ActionUsed = true;
            u.Cb.Turn.AttacksLeftInAction = pc.AttacksPerAction - 1;
            var second = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("2/3"));
            Assert.IsNotNull(second, "attack 2/3 is on the bar after the kill");
            Assert.AreEqual(Targeting.Enemy, second.Target, "it picks a new target");
            u.Cb.Turn.AttacksLeftInAction--; // swing 2 spent
            Assert.IsNotNull(c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("3/3")), "then 3/3 (the bar is rebuilt each time a menu opens, and the count holds)");
            u.Cb.Turn.AttacksLeftInAction = 0;
            var surge = c.ActionsFor(p).First(a => a.Name == "Action Surge");
            Assert.IsTrue(surge.Enabled, "Action Surge once the action is spent");
            Run(surge, u, u.Pos);
            var again = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            Assert.AreEqual(ActionCost.Action, again.Cost, "Action Surge gives a whole new Attack action...");
            StringAssert.Contains("3 attacks", again.Tooltip, "...with three more attacks");
        }

        [UnityTest]
        public IEnumerator ImprovedCunningStrikeOffersTwoToggles()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("rogue", 11, 2, (a, b, d) => { p = a; c = b; u = d; });
            var trip = c.ActionsFor(p).First(a => a.Name == "Cunning Strike: Trip");
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.StartsWith("Second Cunning Strike")), "the second slot opens after the first pick");
            Run(trip, u, u.Pos);
            var seconds = c.ActionsFor(p).Where(a => a.Name.StartsWith("Second Cunning Strike")).Select(a => a.Name).ToList();
            CollectionAssert.AreEquivalent(new[] { "Second Cunning Strike: Poison", "Second Cunning Strike: Withdraw" }, seconds, "two effects, not the same one twice");
            StringAssert.Contains("one more Sneak Attack die", c.ActionsFor(p).First(a => a.Name == "Second Cunning Strike: Poison").Tooltip, "the die cost is shown");
        }

        [UnityTest]
        public IEnumerator FleetStepFollowsAnotherBonusAction()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("monk", 11, 3, (a, b, d) => { p = a; c = b; u = d; });
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.Contains("Fleet Step")), "no Fleet Step before a Bonus Action");
            Run(c.ActionsFor(p).First(a => a.Name.StartsWith("Patient Defense")), u, u.Pos);
            var fleet = c.ActionsFor(p).FirstOrDefault(a => a.Name.Contains("Fleet Step"));
            Assert.IsNotNull(fleet, "Patient Defense, then Fleet Step");
            int move = u.Cb.Turn.MovementLeftFt;
            Run(fleet, u, u.Pos);
            Assert.Greater(u.Cb.Turn.MovementLeftFt, move, "it Dashes");
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.Contains("Fleet Step")), "once per turn");
        }

        /// <summary>A level-13 companion of `cls` with `spell` prepared, beside a level-11 fighter, in the forest wolves' fight.</summary>
        static IEnumerator CasterFight(string cls, string spell, int seed, System.Action<CombatInstance, Unit, Unit, Unit> got)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(seed), "fighter", "human", level: 11);
            var caster = CharacterFactory.Create(CharacterFactory.DefaultChoices(cls, "elf", "Sable"));
            caster.XP = 999999;
            while (caster.Level < 13) caster.LevelUp(caster.AutoPlan(), null);
            if (!caster.PreparedSpells.Contains(spell)) caster.PreparedSpells.Add(spell);
            var comp = g.SpawnCompanion(p, caster, "hums");
            comp.Manual = false;
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.C == caster);
            var hero = c.Units.First(u => u.Owner == p);
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies && u.Active);
            if (me.Cb == null) me.Cb = new Combatant(me.C);
            hero.C.MaxHP = hero.C.HP = 300; wolf.C.MaxHP = wolf.C.HP = 300;
            var near = wolf.Pos + Vector2Int.left;
            if (!hero.A.Zone.IsWalkable(near) || c.Occupied(near)) near = wolf.Pos + Vector2Int.right;
            hero.A.PlaceAt(hero.A.Zone, near);
            got(c, me, hero, wolf);
        }

        [UnityTest]
        public IEnumerator FireStormSparesAlliesInTheArea()
        {
            CombatInstance c = null; Unit me = null, hero = null, wolf = null;
            yield return CasterFight("druid", "fire_storm", 4, (a, b, d, e) => { c = a; me = b; hero = d; wolf = e; });
            var run = c.DebugCast(me, "fire_storm", wolf.Pos);
            while (run.MoveNext()) { }
            Assert.Less(wolf.C.HP, 300, "the wolf burns");
            Assert.AreEqual(300, hero.C.HP, "the fighter beside it is untouched");
        }

        [UnityTest]
        public IEnumerator ChainLightningLeapsToOtherWolves()
        {
            CombatInstance c = null; Unit me = null, hero = null, wolf = null;
            yield return CasterFight("sorcerer", "chain_lightning", 5, (a, b, d, e) => { c = a; me = b; hero = d; wolf = e; });
            var others = c.Units.Where(u => u.C.Team == Team.Enemies && u.Active && u != wolf).ToList();
            foreach (var o in others) o.C.MaxHP = o.C.HP = 300;
            var run = c.DebugCast(me, "chain_lightning", wolf.Pos);
            while (run.MoveNext()) { }
            Assert.Less(wolf.C.HP, 300, "the first wolf is struck");
            int near = others.Count(o => Vector2Int.Distance(o.Pos, wolf.Pos) <= 6);
            if (near > 0) Assert.IsTrue(others.Any(o => o.C.HP < 300), "the bolt leaps to another wolf");
            Assert.AreEqual(300, hero.C.HP, "it never leaps to an ally");
        }
    }
}
