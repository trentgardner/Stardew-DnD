using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void WarlockTests()
        {
            Dice.Rng = new SystemRandom();
            var w = Make("warlock");
            Eq(w.Class.Caster, CasterType.Pact, "warlock uses pact magic");
            Eq(w.SlotsMax(1), 1, "warlock 1: one pact slot (level 1)");
            Eq(AtLevel("warlock", 2).SlotsMax(1), 2, "warlock 2: two pact slots");
            var w3 = AtLevel("warlock", 3);
            Eq(w3.SlotsMax(1), 0, "warlock 3: pact slots are level 2 now");
            Eq(w3.SlotsMax(2), 2, "warlock 3: two level-2 pact slots");
            w3.SlotsUsed[2] = 2;
            w3.ShortRest(null);
            Eq(w3.SlotsLeft(2), 2, "pact slots come back on a short rest");
            var wiz = Make("wizard");
            wiz.SlotsUsed[1] = 2;
            wiz.ShortRest(null);
            Eq(wiz.SlotsLeft(1), 0, "a wizard's slots do NOT come back on a short rest");

            Check(w.KnowsSpell("eldritch_blast"), "warlock knows eldritch blast");
            Eq(w.Invocations.Count, 1, "one invocation at level 1");
            var w2 = Make("warlock");
            w2.XP = 999999;
            var inv = w2.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Invocation);
            Check(inv != null && inv.Count == 2, "warlock 2 learns two more invocations");

            w.Invocations.Clear(); w.Invocations.Add("agonizing_blast");
            var blast = SpellResolver.AsAttack(w, SpellsDb.Get("eldritch_blast"));
            Eq(blast.DamageMod, w.Mod(Ability.CHA), "agonizing blast adds CHA to eldritch blast");
            w.Invocations.Clear(); w.Invocations.Add("armor_of_shadows"); w.Armor = null; // only works unarmored
            Eq(w.ArmorClass, 13 + w.Mod(Ability.DEX), "armor of shadows: mage armor at will");

            // Hex: +1d6 necrotic on any attack hit against the hexed creature.
            var target = Dummy();
            target.AddEffect(EffectLibrary.Create("hex", w));
            var turn = new TurnState();
            AttackRollResult res = null; DamageRoll dmg = null;
            for (int i = 0; i < 300 && dmg == null; i++)
            {
                var r = AttackResolver.RollAttack(w, target, SpellResolver.AsAttack(w, SpellsDb.Get("eldritch_blast")), new AttackContext { DistanceFt = 30 }, turn);
                if (r.Hit && !r.Critical) { res = r; dmg = AttackResolver.RollDamage(r); }
            }
            Check(dmg != null && dmg.Extra != null && dmg.Extra.Dice.Count == 1, "hex adds 1d6 necrotic to a spell attack");

            var fiend = AtLevel("warlock", 3);
            Check(fiend.KnowsSpell("scorching_ray") && fiend.KnowsSpell("burning_hands"), "patron spells always prepared at 3");
            fiend.TempHP = 0;
            fiend.OnReducedHostileToZero(null);
            Eq(fiend.TempHP, System.Math.Max(1, fiend.Mod(Ability.CHA) + fiend.Level), "ember's blessing: temp HP = CHA + level");
        }

        static void SorcererTests()
        {
            Dice.Rng = new SystemRandom();
            var s = Make("sorcerer");
            Eq(s.Class.Caster, CasterType.Full, "sorcerer is a full caster");
            Eq(s.ResourceMax("innate_sorcery"), 2, "innate sorcery twice per long rest");
            Eq(s.ResourceMax("sorcery_points"), 0, "no sorcery points at 1");
            var s2 = AtLevel("sorcerer", 2);
            Eq(s2.ResourceMax("sorcery_points"), 2, "sorcery points = level at 2");
            Eq(s2.Metamagic.Count, 2, "two metamagic options at 2");

            Check(!s2.SlotToPoints(1), "no converting a slot when your points are already full");
            s2.SpendResource("sorcery_points", 2);
            Check(s2.SlotToPoints(1), "slot → 1 point");
            Eq(s2.ResourceLeft("sorcery_points"), 1, "one point gained");
            Check(!s2.PointsToSlot(1), "a level-1 slot costs 2 points");
            s2.ResourcesUsed.Remove("sorcery_points");
            int before = s2.SlotsLeft(1);
            Check(s2.PointsToSlot(1), "points → level-1 slot");
            Eq(s2.SlotsLeft(1), before + 1, "a slot comes back");
            Eq(s2.ResourceLeft("sorcery_points"), 0, "it cost 2 points");

            s.AddEffect(EffectLibrary.Create("innate_sorcery", s));
            Eq(s.SpellSaveDC, 8 + s.ProficiencyBonus + s.Mod(Ability.CHA) + 1, "innate sorcery: +1 spell save DC");

            var d3 = AtLevel("sorcerer", 3);
            var plain = Make("sorcerer");
            Eq(d3.ArmorClass, 10 + d3.Mod(Ability.DEX) + d3.Mod(Ability.CHA), "draconic resilience: AC 10 + DEX + CHA");
            Check(d3.KnowsSpell("chromatic_orb") && d3.KnowsSpell("dragons_breath"), "draconic spells always prepared");
            int perLevel = d3.AverageHitDie + d3.Mod(Ability.CON) + (d3.Species?.BonusHpPerLevel ?? 0);
            Eq(d3.MaxHP, plain.MaxHP + 2 * perLevel + 3, "draconic resilience: +1 HP per sorcerer level (3 at level 3)");
        }
    }
}
