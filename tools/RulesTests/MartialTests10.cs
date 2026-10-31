using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void MartialTests10()
        {
            Dice.Rng = new SystemRandom();

            // ---------------- Fighter
            var f9 = AtLevelTen("fighter", 9);
            Eq(f9.ResourceMax("indomitable"), 1, "fighter 9: one Indomitable");
            Eq(AtLevelTen("fighter", 8).ResourceMax("indomitable"), 0, "fighter 8: none yet");
            // IndomitableRerollsWithLevelBonus
            Dice.Rng = new ScriptedRandom(2, 15);
            var failed = Checks.RollSave(f9, Ability.WIS, AdvantageState.Normal);
            var again = Checks.TryIndomitable(f9, Ability.WIS, 20, failed, null);
            Check(again != failed && again.Natural == 15 && again.Terms.Any(t => t.Label == "indomitable" && t.Value == 9), "Indomitable rerolls with +fighter level");
            Eq(f9.ResourceLeft("indomitable"), 0, "and is spent");
            Check(Checks.TryIndomitable(f9, Ability.WIS, 20, failed, null) == failed, "no second reroll");
            Dice.Rng = new SystemRandom();
            var notFighter = AtLevelTen("rogue", 9);
            var r0 = Checks.RollSave(notFighter, Ability.WIS, AdvantageState.Normal);
            Check(Checks.TryIndomitable(notFighter, Ability.WIS, 30, r0, null) == r0, "only fighters are Indomitable");
            Check(f9.TacticalMasteries().SequenceEqual(new[] { WeaponMastery.Push, WeaponMastery.Sap, WeaponMastery.Slow }), "Tactical Master: Push, Sap or Slow");
            Eq(AtLevelTen("fighter", 8).TacticalMasteries().Count, 0, "not before 9");
            // HeroicWarriorRefreshesInspiration
            var f10 = AtLevelTen("fighter", 10);
            f10.HeroicInspiration = false;
            f10.StartOfTurnInCombat(null);
            Check(f10.HeroicInspiration, "Heroic Warrior: inspiration at the start of your turn");

            // ---------------- Barbarian
            var b5 = AtLevelTen("barbarian", 5);
            b5.Armor = null;
            Eq(b5.Speed, 40, "Fast Movement: +10 ft");
            var b6 = AtLevelTen("barbarian", 6);
            b6.StartRage(null);
            b6.AddEffect(EffectLibrary.Create("frightened", null, 15));
            Check(!b6.Has(Condition.Frightened), "Mindless Rage: can't be frightened while raging");
            Check(AtLevelTen("barbarian", 7).InitiativeAdvantage, "Feral Instinct: Advantage on Initiative");
            // BrutalStrikeTradesAdvantageForDamage
            var b9 = AtLevelTen("barbarian", 9);
            Eq(b9.RageDamage, 3, "rage damage +3 at 9");
            b9.StartRage(null);
            b9.AddEffect(EffectLibrary.Create("reckless", b9));
            var ctx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true };
            var foe = Dummy(ac: 1);
            var atk = b9.GetAttacks()[0];
            var res = new AttackRollResult();
            Check(AttackResolver.ComputeAdvantage(b9, foe, atk, ctx, res) == AdvantageState.Advantage, "reckless gives advantage");
            var brutal = EffectLibrary.Create("brutal_ready", b9); brutal.Power = 1;
            b9.AddEffect(brutal);
            res = new AttackRollResult();
            Check(AttackResolver.ComputeAdvantage(b9, foe, atk, ctx, res) == AdvantageState.Normal, "Brutal Strike gives up that advantage");
            AttackRollResult hit = null; DamageRoll dmg = null;
            for (int i = 0; i < 100 && hit == null; i++)
            {
                if (!b9.HasEffect("brutal_ready")) { var again2 = EffectLibrary.Create("brutal_ready", b9); again2.Power = 1; b9.AddEffect(again2); }
                var turn = new TurnState(); turn.Reset(b9);
                var rr = AttackResolver.RollAttack(b9, foe, atk, ctx, turn);
                Check(!b9.HasEffect("brutal_ready"), "Brutal Strike is spent on the attack it was declared for");
                if (rr.Hit && !rr.Critical) { hit = rr; dmg = AttackResolver.RollDamage(rr); }
            }
            Check(dmg != null && dmg.Terms.Any(t => t.Label == "brutal strike") && hit.BrutalStrike == 1, "...for +1d10 and a Forceful Blow");

            // ---------------- Rogue
            // SneakAttackScalesTo5d6
            Eq(AttackResolver.SneakAttackDiceFor(AtLevelTen("rogue", 5)), 3, "rogue 5: 3d6");
            Eq(AttackResolver.SneakAttackDiceFor(AtLevelTen("rogue", 7)), 4, "rogue 7: 4d6");
            Eq(AttackResolver.SneakAttackDiceFor(AtLevelTen("rogue", 9)), 5, "rogue 9: 5d6");
            // EvasionTakesNoneOnSaveHalfOnFail
            var r7 = AtLevelTen("rogue", 7);
            Eq(Checks.AreaDamage(r7, Ability.DEX, 20, true, true), 0, "Evasion: nothing on a success");
            Eq(Checks.AreaDamage(r7, Ability.DEX, 20, false, true), 10, "Evasion: half on a failure");
            Eq(Checks.AreaDamage(r7, Ability.CON, 20, false, true), 20, "Evasion is only for DEX saves");
            Eq(Checks.AreaDamage(Make("fighter"), Ability.DEX, 20, true, true), 10, "no Evasion: half on a success");
            // UncannyDodgeHalvesOnce
            var r5 = AtLevelTen("rogue", 5);
            var rt = new TurnState(); rt.Reset(r5);
            Eq(AttackResolver.UncannyDodge(r5, 15, rt), 7, "Uncanny Dodge halves the hit");
            Eq(AttackResolver.UncannyDodge(r5, 15, rt), 15, "it's a reaction: once per round");
            // ReliableTalentFloorsAtTen
            Dice.Rng = new ScriptedRandom(2);
            var prof = r7.SkillProficiencies.First();
            var chk = Checks.RollSkill(r7, prof, AdvantageState.Normal);
            Eq(chk.Natural, 10, "Reliable Talent: a 2 counts as 10 on a proficient skill");
            Dice.Rng = new ScriptedRandom(2);
            var notProf = SkillUtil.All.First(s => !r7.SkillProficiencies.Contains(s));
            Eq(Checks.RollSkill(r7, notProf, AdvantageState.Normal).Natural, 2, "not on skills you aren't proficient in");
            Dice.Rng = new SystemRandom();
            // Cunning Strike: spend a Sneak Attack die for a trip
            var rogue = AtLevelTen("rogue", 5);
            var cs = EffectLibrary.Create("cunning_ready", rogue); cs.Power = 1; // 1 = trip
            rogue.AddEffect(cs);
            var victim = Dummy(ac: 1);
            var allyCtx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true, AllyAdjacentToTarget = true };
            var rturn = new TurnState(); rturn.Reset(rogue);
            var (rh, rd) = HitWith(rogue, victim, allyCtx, rturn);
            Check(rh.CunningStrike == 1, "Cunning Strike: the trip rides on the sneak attack");
            int weaponDice = rogue.GetAttacks()[0].Damage.Count * (rh.Critical ? 2 : 1);
            Eq(rd.Dice.Count - weaponDice, 2 * (rh.Critical ? 2 : 1), "...costing one of the three sneak attack dice");
        }
    }
}
