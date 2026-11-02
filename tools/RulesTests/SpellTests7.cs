using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void SpellTests7()
        {
            Dice.Rng = new SystemRandom();
            var wiz = AtLevelThirteen("wizard", 13);

            // ChainLightningHitsUpToFourAndScales
            var chain = SpellsDb.Get("chain_lightning");
            Eq(SpellResolver.ChainTargetsAt(chain, 6), 3, "Chain Lightning: one bolt + three leaps");
            Eq(SpellResolver.ChainTargetsAt(chain, 8), 5, "...one more leap per slot level above 6th");
            Eq(chain.ChainRangeFt, 30, "leaps within 30 ft");
            Eq(SpellResolver.ChainTargetsAt(SpellsDb.Get("fireball"), 7), 0, "other spells don't chain");

            // DisintegrateDealsNothingOnSave
            var dis = SpellsDb.Get("disintegrate");
            var dummy = Dummy(hp: 200);
            var dmg = SpellResolver.RollEffect(wiz, dis, 6, false);
            Check(dmg.Total >= 50, "Disintegrate: 10d6 + 40");
            Eq(SpellResolver.ApplySaveDamage(wiz, dummy, dis, dmg, true, new List<string>()), 0, "nothing on a success");
            Check(SpellResolver.ApplySaveDamage(wiz, dummy, dis, dmg, false, new List<string>()) == dmg.Total, "all of it on a failure");
            Eq(SpellResolver.EffectDice(wiz, dis, 7).Count, 13, "+3d6 per slot level");

            // HealRestoresSeventyAndCleansesBlindPoisoned
            var heal = SpellsDb.Get("heal");
            var cleric = AtLevelThirteen("cleric", 13);
            var h6 = SpellResolver.EffectDice(cleric, heal, 6);
            Eq(h6.Count, 0, "Heal rolls no dice");
            Eq(h6.Bonus, 70, "Heal: 70 HP");
            Eq(SpellResolver.EffectDice(cleric, heal, 7).Bonus, 80, "+10 per slot level above 6th");
            var sick = Dummy();
            sick.AddEffect(new ActiveEffect { Id = "b", Name = "Blind", Condition = Condition.Blinded, RoundsLeft = 10 });
            sick.AddEffect(new ActiveEffect { Id = "p", Name = "Poison", Condition = Condition.Poisoned, RoundsLeft = 10 });
            sick.AddEffect(new ActiveEffect { Id = "f", Name = "Fear", Condition = Condition.Frightened, RoundsLeft = 10 });
            Eq(SpellResolver.HealCleanse(sick), 2, "Heal ends Blinded and Poisoned");
            Check(sick.Has(Condition.Frightened), "...not Frightened");

            // UpcastIntoSeventhScalesOnlyWhenDefined
            Eq(SpellResolver.EffectDice(wiz, SpellsDb.Get("cone_of_cold"), 7).Count, 10, "Cone of Cold in a 7th-level slot: 10d8");
            Check(SpellResolver.EffectDice(wiz, SpellsDb.Get("hold_monster"), 7).IsNone, "Hold Monster has no dice to scale");
            Eq(SpellResolver.EffectDice(wiz, SpellsDb.Get("finger_of_death"), 7).Count, 7, "Final Word doesn't upcast past its own level");
            Eq(wiz.SlotsMax(6), 1, "wizard 13: one 6th-level slot");
            Eq(wiz.SlotsMax(7), 1, "wizard 13: one 7th-level slot");
            Eq(wiz.MaxSpellLevelAt(13), 7, "wizard 13 casts 7th-level spells");
            Eq(AtLevelThirteen("wizard", 11).MaxSpellLevelAt(11), 6, "wizard 11 casts 6th-level spells");
            Eq(AtLevelThirteen("wizard", 11).SlotsMax(7), 0, "no 7th before 13");
            Eq(AtLevelThirteen("paladin", 13).MaxSpellLevelAt(13), 4, "paladin 13: 4th-level spells");

            // Names: SRD ids, neutral names
            Eq(SpellsDb.Get("circle_of_death").Name, "Withering Circle", "Circle of Death is shown as Withering Circle");
            Eq(SpellsDb.Get("finger_of_death").Name, "Final Word", "Finger of Death is shown as Final Word");

            // ForcecageRefusesLegendaryCreatures
            Check(!SpellResolver.CanCage(new Monster(MonstersDb.Get("warden"))), "the Warden can't be caged");
            Check(SpellResolver.CanCage(Dummy()), "a wolf can");
            Check(SpellResolver.CanCage(AtLevelThirteen("fighter", 11)), "so can a person");

            // RegenerateHealsEachTurnStart
            var hurt = Dummy(hp: 50); hurt.HP = 10;
            hurt.AddEffect(EffectLibrary.Create("regenerating", cleric));
            Eq(hurt.StartOfTurnRegen(null), 1, "Regenerate: 1 HP at the start of each turn");
            Eq(hurt.HP, 11, "...and it sticks");
            hurt.HP = 0;
            Eq(hurt.StartOfTurnRegen(null), 0, "not while down");

            // Irresistible Dance: Disadvantage on attacks and DEX saves, Advantage for attackers
            var dancer = Dummy();
            dancer.AddEffect(EffectLibrary.Create("dancing", wiz, 17));
            var ctx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true };
            var wolfBite = dancer.GetAttacks()[0];
            Check(AttackResolver.ComputeAdvantage(dancer, AtLevelThirteen("fighter", 11), wolfBite, ctx, new AttackRollResult()) == AdvantageState.Disadvantage, "a dancer attacks at Disadvantage");
            var f11 = AtLevelThirteen("fighter", 11);
            Check(AttackResolver.ComputeAdvantage(f11, dancer, f11.GetAttacks()[0], ctx, new AttackRollResult()) == AdvantageState.Advantage, "attacks against a dancer have Advantage");
            Dice.Rng = new ScriptedRandom(18, 3);
            Eq(Checks.RollSave(dancer, Ability.DEX, AdvantageState.Normal).Natural, 3, "DEX saves at Disadvantage");
            Dice.Rng = new SystemRandom();
            Check(dancer.Speed == 0, "it dances in place");

            // Stoneskin and Freedom of Movement (the 4th-level gaps)
            var rock = Dummy(hp: 100);
            rock.AddEffect(EffectLibrary.Create("stoneskin", cleric));
            Eq(rock.TakeDamage(20, DamageType.Slashing, false, null), 10, "Stoneskin halves slashing");
            Eq(rock.TakeDamage(20, DamageType.Fire, false, null), 20, "...not fire");
            var free = Dummy();
            free.AddEffect(EffectLibrary.Create("freedom_of_movement", cleric));
            free.AddEffect(EffectLibrary.Create("hold_person", wiz, 15));
            Check(!free.Has(Condition.Paralyzed), "Freedom of Movement: can't be Paralyzed");
            int sp = free.Speed;
            free.AddEffect(EffectLibrary.Create("slow", wiz));
            Eq(free.Speed, sp, "...and Speed can't be reduced");

            // Heroes' Feast
            var guest = AtLevelThirteen("fighter", 11);
            int max = guest.EffectiveMaxHP;
            int bonus = SpellResolver.HeroesFeast(guest, cleric);
            Check(bonus >= 2 && bonus <= 20, "Heroes' Feast: +2d10");
            Eq(guest.EffectiveMaxHP, max + bonus, "...to maximum HP");
            guest.AddEffect(EffectLibrary.Create("frightened", wiz, 30));
            Check(!guest.Has(Condition.Frightened), "...and no fear");

            // SpellListsCuratedToSeven
            foreach (var cls in new[] { "bard", "cleric", "druid", "sorcerer", "warlock", "wizard", "paladin", "ranger" })
            {
                var pc = AtLevelThirteen(cls, 13);
                int top = pc.MaxSpellLevelAt(13);
                for (int l = 1; l <= top; l++)
                {
                    int n = SpellsDb.ForClass(cls, l).Count(s => s.Level == l);
                    Check(n >= 2, $"{cls} has at least 2 level-{l} spells (has {n})");
                }
            }
            Check(SpellsDb.ForClass("warlock", 6).Count(s => s.Level == 6) >= 2, "warlock Mystic Arcanum: at least 2 sixth-level options");
            Check(SpellsDb.ForClass("warlock", 7).Count(s => s.Level == 7) >= 2, "warlock Mystic Arcanum: at least 2 seventh-level options");
        }
    }
}
