using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void NatureCasterTests10()
        {
            Dice.Rng = new SystemRandom();

            // ---------------- Cleric (Life)
            var c6 = AtLevelTen("cleric", 6);
            Eq(SpellResolver.BlessedHealerAmount(c6, SpellsDb.Get("cure_wounds"), 3), 5, "Blessed Healer: 2 + slot level back to the cleric");
            Eq(SpellResolver.BlessedHealerAmount(AtLevelTen("cleric", 5), SpellsDb.Get("cure_wounds"), 3), 0, "not before 6");
            Check(c6.Class.Subclass.AlwaysPreparedAt(5).Contains("mass_healing_word") && c6.PreparedSpells.Contains("revivify"), "Life domain spells at 5");
            Check(AtLevelTen("cleric", 9).PreparedSpells.Contains("mass_cure_wounds"), "Life domain spells at 9");
            // PotentSpellcastingAddsWisToCantrips
            var c7 = AtLevelTen("cleric", 7);
            var flame = SpellResolver.RollEffect(c7, SpellsDb.Get("sacred_flame"), 0, false);
            Check(flame.Terms.Any(t => t.Label == "potent spellcasting" && t.Value == c7.Mod(Ability.WIS)), "Blessed Strikes (Potent Spellcasting): +WIS to cantrip damage");
            // DivineInterventionOncePerLongRest
            var c10 = AtLevelTen("cleric", 10);
            Eq(c10.ResourceMax("divine_intervention"), 1, "Divine Intervention: once");
            Check(c10.SpendResource("divine_intervention") && !c10.SpendResource("divine_intervention"), "...per long rest");
            c10.ShortRest(null);
            Eq(c10.ResourceLeft("divine_intervention"), 0, "a short rest doesn't bring it back");
            c10.LongRest(true, null);
            Eq(c10.ResourceLeft("divine_intervention"), 1, "a long rest does");

            // ---------------- Druid (Land)
            var d7 = AtLevelTen("druid", 7);
            var cantrip = d7.Cantrips.Select(SpellsDb.Get).First(s => s.Kind == SpellKind.Attack || s.Kind == SpellKind.Save);
            var potent = SpellResolver.RollEffect(d7, cantrip, 0, false);
            Check(potent.Terms.Any(t => t.Label == "potent spellcasting"), "Elemental Fury (Potent Spellcasting) for druids at 7");
            // WildShapeCrOneAndFlyAtEight
            var d7forms = AtLevelTen("druid", 7).WildShapeForms();
            var d8forms = AtLevelTen("druid", 8).WildShapeForms();
            Check(!d7forms.Contains("giant_owl") && d8forms.Contains("giant_owl") && d8forms.Contains("dire_wolf"), "druid 8: CR 1 and flying forms");
            Check(MonstersDb.Get("giant_owl").FlySpeed > 0, "the owl flies");
            var d8 = AtLevelTen("druid", 8);
            Check(d8.StartWildShape("giant_owl", null) && d8.Flying, "a druid shaped as an owl is flying");
            // NaturalRecoveryCapsAtHalfLevel
            var d6 = AtLevelTen("druid", 6);
            d6.SlotsUsed[1] = 4; d6.SlotsUsed[2] = 3; d6.SlotsUsed[3] = 3;
            d6.ShortRest(null);
            int regained = (4 - d6.SlotsUsed[1]) * 1 + (3 - d6.SlotsUsed[2]) * 2 + (3 - d6.SlotsUsed[3]) * 3;
            Eq(regained, 3, "Natural Recovery: slot levels up to half the druid level (3)");
            d6.ShortRest(null);
            Eq((4 - d6.SlotsUsed[1]) * 1 + (3 - d6.SlotsUsed[2]) * 2 + (3 - d6.SlotsUsed[3]) * 3, 3, "once per long rest");
            // Wild Resurgence: a slot for a Wild Shape use
            var d5 = AtLevelTen("druid", 5);
            d5.ResourcesUsed["wild_shape"] = d5.ResourceMax("wild_shape");
            Check(d5.WildResurgence(null) && d5.ResourceLeft("wild_shape") == 1, "Wild Resurgence: spend a slot to get a Wild Shape back");
            // Nature's Ward
            var d10 = AtLevelTen("druid", 10);
            d10.AddEffect(new ActiveEffect { Id = "poisoned", Name = "Poisoned", Condition = Condition.Poisoned, RoundsLeft = 10 });
            Check(!d10.Has(Condition.Poisoned), "Nature's Ward: can't be Poisoned");
            Check(d10.Class.Subclass.Features.Any(f => f.Level == 10), "Land 10 feature exists");
            Check(d10.BonusAlwaysPrepared.Count > AtLevelTen("druid", 3).BonusAlwaysPrepared.Count, "circle spells grow at 5, 7, 9");

            // ---------------- Bard (Lore)
            var b5 = AtLevelTen("bard", 5);
            Eq(b5.BardicDie, 8, "bard 5: d8");
            b5.ResourcesUsed["bardic_inspiration"] = b5.ResourceMax("bardic_inspiration");
            b5.ShortRest(null);
            Eq(b5.ResourceLeft("bardic_inspiration"), b5.ResourceMax("bardic_inspiration"), "Font of Inspiration: back on a short rest");
            b5.ResourcesUsed["bardic_inspiration"] = b5.ResourceMax("bardic_inspiration");
            Check(b5.FontOfInspiration(null) && b5.ResourceLeft("bardic_inspiration") == 1, "...or spend a spell slot for one");
            Eq(AtLevelTen("bard", 4).ResourceLeft("bardic_inspiration") >= 0, true, "sanity");
            PlayerCharacter.LevelCap = 10;
            var b5b = AtLevel("bard", 5);
            var md = b5b.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.BonusSpell);
            Check(md != null && md.Count == 2 && md.Options.Any(id => !SpellsDb.Get(id).Classes.Contains("bard")), "Lore 6: Magical Discoveries from other lists");
            var b6 = AtLevel("bard", 6);
            Check(b6.BonusAlwaysPrepared.Count >= 2, "the discoveries are always prepared");
            PlayerCharacter.LevelCap = 4;
            // Countercharm: allies near a bard 7 save against charm and fear with Advantage
            var b7 = AtLevelTen("bard", 7);
            var friend = Make("fighter");
            var fake = new FakeAuras(); fake.Feet[b7] = 0; fake.Feet[friend] = 25;
            Auras.Source = fake;
            try { Check(Auras.Countercharm(friend), "Countercharm: an ally within 30 ft"); fake.Feet[friend] = 40; Check(!Auras.Countercharm(friend), "not at 40 ft"); }
            finally { Auras.Source = null; }
        }
    }
}
