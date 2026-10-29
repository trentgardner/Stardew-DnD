using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void MonkTests()
        {
            Dice.Rng = new SystemRandom();
            var m = Make("monk");
            Eq(m.Class.HitDie, 8, "monk d8");
            Eq(m.ArmorClass, 10 + m.Mod(Ability.DEX) + m.Mod(Ability.WIS), "monk unarmored defense = 10 + DEX + WIS");
            var unarmed = m.UnarmedStrike();
            Eq(unarmed.Damage.ToString(), "1d6", "martial arts: unarmed strikes use a d6");
            Eq(unarmed.DamageMod, m.Mod(Ability.DEX), "martial arts: DEX for unarmed strikes");
            Eq(m.ResourceMax("focus"), 0, "no focus points at level 1");

            var m2 = AtLevel("monk", 2);
            Eq(m2.ResourceMax("focus"), 2, "focus points = monk level");
            Eq(m2.Speed, 40, "unarmored movement +10 ft at level 2");
            m2.SpendResource("focus"); m2.SpendResource("focus");
            m2.ShortRest(null);
            Eq(m2.ResourceLeft("focus"), 2, "focus points come back on a short rest");

            var m3 = AtLevel("monk", 3);
            Dice.Rng = new ScriptedRandom(6);
            Eq(m3.DeflectReduction(), 6 + m3.Mod(Ability.DEX) + 3, "deflect attacks reduces 1d10 + DEX + monk level");
            Dice.Rng = new SystemRandom();
        }

        static void PaladinTests()
        {
            Dice.Rng = new SystemRandom();
            var p = Make("paladin");
            Eq(p.Class.Caster, CasterType.Half, "paladin is a half caster");
            Eq(p.SlotsMax(1), 2, "paladin 1: two 1st-level slots");
            Eq(AtLevel("paladin", 3).SlotsMax(1), 3, "paladin 3: three 1st-level slots");
            Eq(AtLevel("paladin", 4).SlotsMax(2), 0, "no 2nd-level slots before 5");
            Eq(p.ResourceMax("lay_on_hands"), 5, "lay on hands pool = 5 × level");
            Check(p.SpendResource("lay_on_hands", 3), "spend 3 points from the pool");
            Eq(p.ResourceLeft("lay_on_hands"), 2, "2 points left");
            Check(!p.SpendResource("lay_on_hands", 3), "can't overspend the pool");
            Check(SpellsDb.ForClass("paladin", 1).Any(s => s.Id == "bless"), "bless is on the paladin list");

            var p2 = Make("paladin");
            p2.XP = 999999;
            var fs = p2.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.FightingStyle);
            Check(fs != null && fs.Options.Contains("defense") && fs.Options.Contains("dueling"), "paladin 2 picks a fighting style");
            var plan = new LevelUpPlan();
            foreach (var c in p2.PendingChoices()) plan.Picks.Add(c.Kind == ChoiceKind.FightingStyle ? new System.Collections.Generic.List<string> { "defense" } : c.Options.Take(c.Count).ToList());
            int ac = p2.ArmorClass;
            p2.LevelUp(plan, null);
            Eq(p2.FightingStyle, "defense", "fighting style recorded");
            Eq(p2.ArmorClass, ac + 1, "defense: +1 AC in armor");
            Check(p2.KnowsSpell("divine_smite"), "paladin's smite: Divine Smite always prepared at 2");
            Eq(p2.ResourceMax("free_smite"), 1, "one free Divine Smite per long rest");

            Dice.Rng = new ScriptedRandom(8, 8, 8);
            var smite = SpellResolver.DivineSmiteDamage(1, false);
            Eq(smite.Dice.Count, 2, "divine smite: 2d8 at 1st level");
            Eq(SpellResolver.DivineSmiteDamage(2, false).Dice.Count, 3, "divine smite: +1d8 per slot level");
            Dice.Rng = new SystemRandom();

            var p3 = AtLevel("paladin", 3);
            Check(p3.KnowsSpell("protection_from_evil_and_good") && p3.KnowsSpell("shield_of_faith"), "oath of devotion spells always prepared");
            Eq(p3.ResourceMax("channel_divinity"), 2, "paladin channel divinity at 3");
        }
    }
}
