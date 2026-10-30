using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Full casters trade at-will damage for spells, healing and support (SRD): they get a looser floor.</summary>
        static readonly string[] FullCasters = { "bard", "cleric", "druid", "sorcerer", "wizard" };
        static readonly string[] AllClasses = { "barbarian", "bard", "cleric", "druid", "fighter", "monk", "paladin", "ranger", "rogue", "sorcerer", "warlock", "wizard" };

        /// <summary>Expected damage of one attack against an AC: hit chance × average, plus the extra dice on a 5% crit.</summary>
        static double ExpectedDamage(AttackOption a, int ac, int bonusDiceAvgOnHit = 0)
        {
            double hit = Math.Clamp((21 - (ac - a.ToHit)) / 20.0, 0.05, 0.95);
            double avg = a.Damage.Average + a.DamageMod + bonusDiceAvgOnHit;
            double critExtra = (a.Damage.Average - a.Damage.Bonus) + bonusDiceAvgOnHit;
            return hit * avg + 0.05 * critExtra;
        }

        /// <summary>A character's best single-target damage per round from its weapons and attack cantrips (Sneak Attack for rogues).</summary>
        public static double DamagePerRound(PlayerCharacter pc, int ac)
        {
            var options = new List<AttackOption>(pc.GetAttacks());
            foreach (var id in pc.Cantrips)
            {
                var s = SpellsDb.Get(id);
                if (s != null && s.Kind == SpellKind.Attack) options.Add(SpellResolver.AsAttack(pc, s));
            }
            int sneakAvg = (int)Math.Round(AttackResolver.SneakAttackDiceFor(pc) * 3.5);
            double best = options.Max(a => ExpectedDamage(a, ac, sneakAvg));
            // Saving-throw cantrips (Sacred Flame...): average damage × the chance a typical foe (+1 save) fails.
            foreach (var id in pc.Cantrips)
            {
                var s = SpellsDb.Get(id);
                if (s == null || s.Kind != SpellKind.Save || s.Dice.IsNone) continue;
                double fail = Math.Clamp((pc.SpellSaveDC - 2) / 20.0, 0.05, 0.95);
                best = Math.Max(best, fail * s.Dice.Average);
            }
            // Monks: Martial Arts gives a bonus-action unarmed strike every turn.
            if (pc.HasFeature("Martial Arts")) best += ExpectedDamage(pc.UnarmedStrike(), ac);
            return best;
        }

        static PlayerCharacter AtLevel(string cls, int level)
        {
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices(cls, "human", "Bal " + cls));
            while (pc.Level < level)
            {
                pc.XP = PlayerCharacter.XpThresholds[pc.Level + 1];
                pc.LevelUp(pc.AutoPlan(), new List<string>());
            }
            return pc;
        }

        /// <summary>
        /// No class is left behind: at every level 1–4, every martial (and the warlock) deals at least half the strongest
        /// at-will damage per round vs AC 13, and full casters at least a quarter (their spells carry the rest).
        /// </summary>
        static void BalanceTests()
        {
            for (int level = 1; level <= 4; level++)
            {
                var dpr = AllClasses.ToDictionary(c => c, c => DamagePerRound(AtLevel(c, level), 13));
                var best = dpr.OrderByDescending(kv => kv.Value).First();
                Console.WriteLine($"L{level} at-will DPR vs AC 13: " + string.Join(", ", dpr.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value:0.0}")));
                foreach (var kv in dpr)
                {
                    double floor = FullCasters.Contains(kv.Key) ? 0.25 : 0.5;
                    Check(kv.Value >= floor * best.Value, $"balance L{level}: {kv.Key} {kv.Value:0.0} is under {floor:0.##} of {best.Key} {best.Value:0.0}");
                }
            }

            // Krag (the warrens' war-boss) is beatable by a level-3 pair: the median pair drops him in 6 rounds or fewer.
            var krag = MonstersDb.Get("kobold_warboss");
            var pairs = new List<double>();
            for (int i = 0; i < AllClasses.Length; i++)
                for (int j = i + 1; j < AllClasses.Length; j++)
                    pairs.Add(krag.HitDice.Average / (DamagePerRound(AtLevel(AllClasses[i], 3), krag.AC) + DamagePerRound(AtLevel(AllClasses[j], 3), krag.AC)));
            pairs.Sort();
            double median = pairs[pairs.Count / 2], worstPair = pairs.Last();
            Console.WriteLine($"Krag at level 3: median pair {median:0.0} rounds, slowest pair {worstPair:0.0} rounds");
            Check(median <= 6, $"Krag: a typical level-3 pair needs {median:0.0} rounds");
            Check(worstPair <= 9, $"Krag: the slowest level-3 pair needs {worstPair:0.0} rounds");
        }
    }
}
