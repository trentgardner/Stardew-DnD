using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void RangerTests()
        {
            Dice.Rng = new SystemRandom();
            var r = Make("ranger");
            Eq(r.Class.Caster, CasterType.Half, "ranger is a half caster");
            Check(r.KnowsSpell("hunters_mark"), "favored enemy: hunter's mark always prepared");
            Eq(r.ResourceMax("favored_enemy"), 2, "two free hunter's mark casts per long rest");

            // Hunter's Mark: +1d6 on weapon hits against the marked target only.
            var marked = Dummy(); var other = Dummy();
            marked.AddEffect(EffectLibrary.Create("hunters_mark", r));
            var turn = new TurnState();
            var onMarked = HitWith(r, marked, new AttackContext(), turn);
            int baseDice = r.GetAttacks()[0].Damage.Count;
            Check(onMarked.dmg.Extra != null && onMarked.dmg.Extra.Dice.Count == 1 && onMarked.dmg.Extra.Dice[0].Sides == 6, "hunter's mark adds 1d6 force");
            var onOther = HitWith(r, other, new AttackContext(), new TurnState());
            Check(onOther.dmg.Extra == null, "no extra damage on unmarked targets");

            // Level 2: Deft Explorer expertise + fighting style choices.
            var r2 = Make("ranger");
            r2.XP = 999999;
            var kinds = r2.PendingChoices().Select(c => c.Kind).ToList();
            Check(kinds.Contains(ChoiceKind.Expertise) && kinds.Contains(ChoiceKind.FightingStyle), "ranger 2: expertise and fighting style");

            // Level 3: Hunter's Prey choice; Colossus Slayer once per turn vs a wounded target.
            var r3 = AtLevel("ranger", 3);
            Check(r3.HunterPrey == "colossus_slayer" || r3.HunterPrey == "horde_breaker", "hunter's prey chosen");
            r3.HunterPrey = "colossus_slayer";
            var wounded = Dummy(); wounded.HP = wounded.MaxHP - 1;
            var t3 = new TurnState();
            var cs = HitWith(r3, wounded, new AttackContext(), t3);
            Eq(cs.dmg.Dice.Count, r3.GetAttacks()[0].Damage.Count + 1, "colossus slayer adds 1d8 to a wounded target");
            var cs2 = HitWith(r3, wounded, new AttackContext(), t3);
            Eq(cs2.dmg.Dice.Count, r3.GetAttacks()[0].Damage.Count, "colossus slayer once per turn");
        }

        static void DruidTests()
        {
            Dice.Rng = new SystemRandom();
            var d = Make("druid");
            Eq(d.Class.Caster, CasterType.Full, "druid is a full caster");
            Check(SpellsDb.ForClass("druid", 1).Any(s => s.Id == "entangle") && SpellsDb.ForClass("druid", 0).Any(s => s.Id == "produce_flame"), "druid spell list");
            Eq(d.ResourceMax("wild_shape"), 0, "no wild shape at 1");

            var wc = CharacterFactory.DefaultChoices("druid", "human", "W"); wc.PrimalOrder = "warden";
            var warden = CharacterFactory.Create(wc);
            Eq(warden.Armor?.Id, "hide", "warden starts in hide armor");
            Check(warden.CanWearArmor(ItemsDb.Get<ArmorDef>("scale_mail")), "warden can wear medium armor");
            Check(!d.CanWearArmor(ItemsDb.Get<ArmorDef>("scale_mail")), "magician can't wear medium armor");

            var d2 = AtLevel("druid", 2);
            Eq(d2.ResourceMax("wild_shape"), 2, "two wild shape uses at 2");
            var forms = d2.WildShapeForms();
            Check(forms.Contains("wolf") && forms.Contains("cat") && !forms.Contains("black_bear"), "level 2 forms: CR 1/4 or lower");
            Check(AtLevel("druid", 4).WildShapeForms().Contains("black_bear"), "level 4 adds CR 1/2 forms");

            int hpBefore = d2.HP, intBefore = d2.Score(Ability.INT);
            Check(d2.StartWildShape("wolf", null), "can wild shape into a wolf");
            Eq(d2.TempHP, 2, "temp HP = druid level");
            Eq(d2.ArmorClass, 13, "wolf AC");
            Eq(d2.Speed, 40, "wolf speed");
            Eq(d2.Score(Ability.STR), 12, "wolf STR");
            Eq(d2.Score(Ability.INT), intBefore, "keeps your own INT");
            Eq(d2.GetAttacks()[0].Name, "Bite", "attacks as the beast");
            Check(!d2.CanCastSpells, "no spellcasting while shaped");
            Eq(d2.HP, hpBefore, "keeps your own HP");

            d2.TakeDamage(1000, DamageType.Piercing, false, null);
            Check(d2.WildShape == null, "dropping to 0 HP ends wild shape");
            Check(d2.CanCastSpells, "spellcasting back after reverting");

            var d3 = AtLevel("druid", 2);
            d3.StartWildShape("cat", null);
            d3.EndWildShape(null);
            Eq(d3.ResourceLeft("wild_shape"), 1, "one use left");
            d3.StartWildShape("cat", null);
            d3.EndWildShape(null);
            Check(!d3.StartWildShape("cat", null), "out of uses");
            d3.ShortRest(null);
            Eq(d3.ResourceLeft("wild_shape"), 1, "short rest regains one use");

            var d4 = Make("druid");
            d4.XP = 999999;
            d4.LevelUp(d4.AutoPlan(), null);
            var land = d4.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.LandType);
            Check(land != null && land.Options.Contains("temperate"), "circle of the land: choose a land at 3");
            var plan = new LevelUpPlan();
            foreach (var c in d4.PendingChoices()) plan.Picks.Add(c.Kind == ChoiceKind.LandType ? new List<string> { "temperate" } : c.Options.Take(c.Count).ToList());
            d4.LevelUp(plan, null);
            Check(d4.KnowsSpell("misty_step") && d4.KnowsSpell("sleep") && d4.KnowsSpell("shocking_grasp"), "temperate circle spells always prepared");
        }
    }
}
