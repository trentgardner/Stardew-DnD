using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void ArcaneTests10()
        {
            Dice.Rng = new SystemRandom();

            // ---------------- Sorcerer (Draconic)
            // SorcerousRestorationHalfLevelOncePerLongRest
            var s5 = AtLevelTen("sorcerer", 5);
            s5.ResourcesUsed["sorcery_points"] = 5;
            s5.ShortRest(null);
            Eq(s5.ResourceLeft("sorcery_points"), 2, "Sorcerous Restoration: half your level (2) back on a short rest");
            s5.ShortRest(null);
            Eq(s5.ResourceLeft("sorcery_points"), 2, "once per long rest");
            PlayerCharacter.LevelCap = 10;
            var s5b = AtLevel("sorcerer", 5);
            var el = s5b.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.DraconicElement);
            Check(el != null && el.Options.Contains("fire") && el.Options.Contains("cold"), "Draconic 6: Elemental Affinity choice");
            PlayerCharacter.LevelCap = 4;
            // ElementalAffinityAddsChaToMatchingDamage
            var s6 = AtLevelTen("sorcerer", 6);
            s6.DraconicElement = "fire";
            var fire = SpellResolver.RollEffect(s6, SpellsDb.Get("fireball"), 3, false);
            Check(fire.Terms.Any(t => t.Label == "elemental affinity" && t.Value == s6.Mod(Ability.CHA)), "Elemental Affinity: +CHA to fire spell damage");
            var cold = SpellResolver.RollEffect(s6, SpellsDb.Get("ray_of_frost"), 0, false);
            Check(!cold.Terms.Any(t => t.Label == "elemental affinity"), "not to other types");
            int hp = s6.HP; s6.TakeDamage(10, DamageType.Fire, false, null);
            Eq(hp - s6.HP, 5, "and resistance to fire");
            var s7 = AtLevelTen("sorcerer", 7);
            s7.ResourcesUsed["innate_sorcery"] = 2;
            int sp = s7.ResourceLeft("sorcery_points");
            Check(s7.SorceryIncarnate(null) && s7.HasEffect("innate_sorcery") && s7.ResourceLeft("sorcery_points") == sp - 2, "Sorcery Incarnate: Innate Sorcery for 2 points when out of uses");

            // ---------------- Warlock (Ember Patron)
            PlayerCharacter.LevelCap = 10;
            var w4 = AtLevel("warlock", 4);
            w4.Invocations.Clear(); w4.Invocations.Add("pact_of_the_blade");
            var inv = w4.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Invocation);
            Check(inv != null && inv.Options.Contains("thirsting_blade"), "warlock 5 with Pact of the Blade can take Thirsting Blade");
            var w4b = AtLevel("warlock", 4);
            w4b.Invocations.Clear(); w4b.Invocations.Add("agonizing_blast");
            Check(!w4b.PendingChoices().First(c => c.Kind == ChoiceKind.Invocation).Options.Contains("thirsting_blade"), "...but not without it");
            PlayerCharacter.LevelCap = 4;
            // ThirstingBladeGivesTwoAttacks
            var w5 = AtLevelTen("warlock", 5);
            w5.Invocations.Clear(); w5.Invocations.Add("pact_of_the_blade"); w5.Invocations.Add("thirsting_blade");
            w5.MainHand = ItemsDb.Get<WeaponDef>("longsword");
            Eq(w5.AttacksPerAction, 2, "Thirsting Blade: two attacks with your pact weapon");
            w5.MainHand = ItemsDb.Get<WeaponDef>("light_crossbow") ?? ItemsDb.Get<WeaponDef>("shortbow");
            Eq(w5.AttacksPerAction, 1, "not with a bow");
            // EmbersOwnLuckAddsD10
            var w6 = AtLevelTen("warlock", 6);
            Check(w6.ResourceMax("embers_luck") >= 1, "Ember's Own Luck uses");
            Dice.Rng = new ScriptedRandom(5, 10);
            var save = Checks.RollSave(w6, Ability.WIS, AdvantageState.Normal);
            int dc = save.Total + 3;
            var rescued = Checks.Rescue(w6, Ability.WIS, dc, save, null);
            Check(Checks.ResolveSave(rescued, dc) && rescued.BonusDice.Any(d => d.Sides == 10), "Ember's Own Luck: +1d10 turns a near miss into a save");
            Dice.Rng = new SystemRandom();
            var w10 = AtLevelTen("warlock", 10);
            hp = w10.HP; w10.TakeDamage(10, DamageType.Fire, false, null);
            Eq(hp - w10.HP, 5, "Ember Resilience: resistance to fire");

            // ---------------- Wizard (Evoker)
            // SculptSpellsSparesAllies
            var wz6 = AtLevelTen("wizard", 6);
            var friends = new List<Creature> { Make("fighter"), Make("cleric"), Dummy(), Make("rogue"), Make("bard"), Make("monk") };
            var spared = SpellResolver.Sculpted(wz6, SpellsDb.Get("fireball"), friends);
            Eq(spared.Count, 4, "Sculpt Spells: 1 + spell level (3) allies spared");
            Check(spared.All(c => c.Team == wz6.Team), "never the enemy");
            Eq(SpellResolver.Sculpted(wz6, SpellsDb.Get("hypnotic_pattern"), friends).Count, 0, "only Evocation spells");
            Eq(SpellResolver.Sculpted(AtLevelTen("wizard", 5), SpellsDb.Get("fireball"), friends).Count, 0, "not before 6");
            // EmpoweredEvocationAddsIntOnce
            var wz10 = AtLevelTen("wizard", 10);
            var ee = SpellResolver.RollEffect(wz10, SpellsDb.Get("fireball"), 3, false);
            Eq(ee.Terms.Count(t => t.Label == "Empowered Evocation"), 1, "Empowered Evocation: +INT once");
            Check(!SpellResolver.RollEffect(AtLevelTen("wizard", 9), SpellsDb.Get("fireball"), 3, false).Terms.Any(t => t.Label == "Empowered Evocation"), "not before 10");
        }
    }
}
