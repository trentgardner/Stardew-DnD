using System.Collections.Generic;

namespace DnD.Rules
{
    public enum CheckOutcome { CriticalFailure, Failure, Success, CriticalSuccess }

    /// <summary>
    /// Ability checks and saving throws.
    /// House rule (Critical Role style, agreed with the designer): on ABILITY CHECKS a natural 20 always
    /// succeeds with the best possible outcome and a natural 1 always fails with a mishap.
    /// Impossible tasks are never offered as a roll. Saving throws follow RAW (no auto success/fail).
    /// </summary>
    public static class Checks
    {
        public static List<RollTerm> SkillTerms(Creature c, Skill s)
        {
            var terms = new List<RollTerm>();
            Ability a = SkillUtil.AbilityOf(s);
            terms.Add(new RollTerm(a.ToString(), c.Mod(a)));
            int prof = c.SkillBonus(s) - c.Mod(a);
            if (prof != 0)
            {
                string label = "proficiency";
                if (c is PlayerCharacter pc)
                {
                    if (pc.Expertise.Contains(s)) label = "expertise";
                    else if (!pc.IsProficient(s)) label = "jack of all trades";
                }
                terms.Add(new RollTerm(label, prof));
            }
            if (c.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", c.D20Penalty));
            return terms;
        }

        /// <summary>Raging creatures have Advantage on Strength checks.</summary>
        static AdvantageState CheckAdvantage(Creature c, Ability a, AdvantageState adv)
            => a == Ability.STR && c.HasEffect("rage") ? Dice.Combine(true, adv == AdvantageState.Disadvantage) : adv;

        public static D20Result RollSkill(Creature c, Skill s, AdvantageState adv)
        {
            adv = CheckAdvantage(c, SkillUtil.AbilityOf(s), adv);
            var r = Dice.RollD20(adv, SkillTerms(c, s));
            // Rogue 7: Reliable Talent: a d20 of 9 or lower counts as 10 on a skill you're proficient in.
            if (c is PlayerCharacter pc && pc.HasFeature("Reliable Talent") && pc.IsProficient(s))
                for (int i = 0; i < r.Dice.Count; i++)
                    if (!r.Dice[i].Discarded && r.Dice[i].Value < 10) { r.Dice[i] = new DieRoll(20, 10); break; }
            ApplyEffectDice(c, r, checkRoll: true);
            return r;
        }

        /// <summary>Fighter 9: Indomitable: reroll a failed save with a bonus equal to your fighter level. Returns the save to use.</summary>
        public static D20Result TryIndomitable(Creature c, Ability a, int dc, D20Result save, List<string> log, string vs = null)
        {
            if (!(c is PlayerCharacter pc) || ResolveSave(save, dc) || pc.ResourceLeft("indomitable") <= 0 || pc.IsIncapacitated) return save;
            pc.SpendResource("indomitable");
            var again = RollSave(c, a, AdvantageState.Normal, vs);
            again.Terms.Add(new RollTerm("indomitable", pc.Level));
            log?.Add($"{pc.Name} is Indomitable! Rerolls: {again.Total} vs DC {dc}.");
            return again;
        }

        /// <summary>Last-ditch help for a failed save: Indomitable (fighter 9), then Ember's Own Luck (+1d10, warlock 6) if it could still turn it.</summary>
        public static D20Result Rescue(Creature c, Ability a, int dc, D20Result save, List<string> log, string vs = null)
        {
            save = TryIndomitable(c, a, dc, save, log, vs);
            if (ResolveSave(save, dc) || !(c is PlayerCharacter pc) || dc - save.Total > 10 || pc.ResourceLeft("embers_luck") <= 0) return save;
            pc.SpendResource("embers_luck");
            int v = Dice.AddBonusDie(save, 10);
            log?.Add($"Ember's Own Luck: {pc.Name} adds {v} → {save.Total} vs DC {dc}.");
            return save;
        }

        /// <summary>Damage from an area effect after the save (Rogue/Monk 7: Evasion on DEX saves).</summary>
        public static int AreaDamage(Creature c, Ability a, int full, bool saved, bool halfOnSave)
        {
            bool evasion = a == Ability.DEX && c is PlayerCharacter pc && pc.HasFeature("Evasion") && !pc.IsIncapacitated;
            if (evasion) return saved && halfOnSave ? 0 : full / 2;
            return saved ? (halfOnSave ? full / 2 : 0) : full;
        }

        public static D20Result RollAbilityCheck(Creature c, Ability a, AdvantageState adv)
        {
            adv = CheckAdvantage(c, a, adv);
            var terms = new List<RollTerm> { new RollTerm(a.ToString(), c.Mod(a)) };
            if (c is PlayerCharacter pc && pc.HasFeature("Jack of All Trades")) terms.Add(new RollTerm("jack of all trades", pc.ProficiencyBonus / 2));
            if (c.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", c.D20Penalty));
            var r = Dice.RollD20(adv, terms);
            ApplyEffectDice(c, r, checkRoll: true);
            return r;
        }

        public static D20Result RollSave(Creature c, Ability a, AdvantageState adv, string vs = null)
        {
            if (c.AdvantageOnSave(a, vs) || ((vs == "charmed" || vs == "frightened") && Auras.Countercharm(c))) adv = Dice.Combine(true, adv == AdvantageState.Disadvantage);
            // Barbarian 13: Staggering Blow: Disadvantage on the next save.
            var stagger = c.Effects.Find(e => e.Id == "staggering_blow");
            if (stagger != null) { c.Effects.Remove(stagger); adv = Dice.Combine(adv == AdvantageState.Advantage, true); }
            var terms = new List<RollTerm> { new RollTerm(a + " save", c.SaveBonus(a)) };
            if (c.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", c.D20Penalty));
            int aura = Auras.ProtectionBonus(c);
            if (aura > 0) terms.Add(new RollTerm("aura of protection", aura));
            var r = Dice.RollD20(adv, terms);
            foreach (var e in c.Effects) if (e.SaveDie > 0) Dice.AddBonusDie(r, e.SaveDie);
            return r;
        }

        static void ApplyEffectDice(Creature c, D20Result r, bool checkRoll)
        {
            if (!checkRoll) return;
            for (int i = c.Effects.Count - 1; i >= 0; i--)
            {
                var e = c.Effects[i];
                if (e.CheckDie > 0)
                {
                    Dice.AddBonusDie(r, e.CheckDie);
                    if (e.Id == "guidance") c.Effects.RemoveAt(i); // one use
                }
            }
        }

        /// <summary>Resolves an ability check against a (hidden) DC using the house rules.</summary>
        public static CheckOutcome ResolveCheck(D20Result r, int dc)
        {
            if (r.IsNat20) return CheckOutcome.CriticalSuccess;
            if (r.IsNat1) return CheckOutcome.CriticalFailure;
            return r.Total >= dc ? CheckOutcome.Success : CheckOutcome.Failure;
        }

        public static bool ResolveSave(D20Result r, int dc) => r.Total >= dc;

        public static bool IsSuccess(CheckOutcome o) => o == CheckOutcome.Success || o == CheckOutcome.CriticalSuccess;

        /// <summary>How the margin is described to the player once the hidden DC is revealed.</summary>
        public static string MarginText(D20Result r, int dc)
        {
            int m = r.Total - dc;
            if (r.IsNat20) return "NATURAL 20!";
            if (r.IsNat1) return "NATURAL 1...";
            if (m >= 10) return $"Crushed it (DC {dc})";
            if (m >= 0) return m == 0 ? $"Just barely! (DC {dc})" : $"Success by {m} (DC {dc})";
            if (m >= -2) return $"So close... (DC {dc})";
            return $"Failed by {-m} (DC {dc})";
        }

        // Typical DCs from the 2024 rules.
        public const int VeryEasy = 5, Easy = 10, Medium = 15, Hard = 20, VeryHard = 25, NearlyImpossible = 30;
    }
}
