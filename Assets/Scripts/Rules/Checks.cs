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
            ApplyEffectDice(c, r, checkRoll: true);
            return r;
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
            if (c.AdvantageOnSave(a, vs)) adv = Dice.Combine(true, adv == AdvantageState.Disadvantage);
            var terms = new List<RollTerm> { new RollTerm(a + " save", c.SaveBonus(a)) };
            if (c.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", c.D20Penalty));
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
