using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules.Content;
using DnD.Rules;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Act III Phase 6: phased bosses (no hit skips a phase) and the finale's stat blocks.</summary>
        static void FinaleBossTests()
        {
            Dice.Rng = new SystemRandom();

            // NoHitSkipsAPhase
            var q = new Monster(MonstersDb.Get("ashen_queen"), false);
            int t1 = (int)Math.Ceiling(0.66 * q.MaxHP), t2 = (int)Math.Ceiling(0.33 * q.MaxHP);
            int lost = q.TakeDamage(9999, DamageType.Slashing, false, new List<string>());
            Eq(q.HP, t1, "a huge hit stops at the first threshold");
            Eq(lost, q.MaxHP - t1, "the HP actually lost is up to the threshold");
            Eq(q.PhaseIndex, 1, "phase 1 entered");
            Check(q.PendingPhase == q.Def.Phases[0], "phase 1 is pending");
            Check(!q.IsDown && !q.IsDead, "not down at a threshold");
            // TakePendingPhaseClearsIt
            Check(q.TakePendingPhase() == q.Def.Phases[0], "TakePendingPhase returns the phase");
            Check(q.TakePendingPhase() == null, "and clears it");
            q.TakeDamage(9999, DamageType.Slashing, false, null);
            Eq(q.HP, t2, "the second huge hit stops at the second threshold");
            Eq(q.PhaseIndex, 2, "phase 2 entered");
            // PhaseChangesRechargeAndLegendaries
            var breath = q.Def.Abilities[0];
            Eq(q.RechargeMinFor(breath), 4, "phase 2 breath recharges on 4-6");
            Eq(q.LegendaryBudget, 4, "phase 2: four legendary actions");
            q.ResetLegendary();
            Eq(q.LegendaryLeft, 4, "reset uses the phase's budget");
            q.TakeDamage(9999, DamageType.Slashing, false, null);
            Check(q.IsDown, "after the last phase, damage kills");

            // Small hits that cross a threshold also stop there (no skipping by chip damage either).
            var c = new Monster(MonstersDb.Get("crowned_fire"), false);
            int ct1 = (int)Math.Ceiling(0.66 * c.MaxHP);
            c.TakeDamage(c.MaxHP - ct1 - 1, DamageType.Cold, false, null);
            Eq(c.PhaseIndex, 0, "just above the threshold: no phase yet");
            c.TakeDamage(5, DamageType.Cold, false, null);
            Eq(c.HP, ct1, "a small hit across the line stops on it");
            Eq(c.PhaseIndex, 1, "and enters the phase");
            Eq(c.TakeDamage(0, DamageType.Cold, false, null), 0, "zero damage does nothing");

            // WardenStillUsesPhaseTwo
            var w = new Monster(MonstersDb.Get("warden"), false);
            Eq(w.Def.Phases.Count, 0, "the Warden has no Phases");
            w.TakeDamage(9999, DamageType.Slashing, false, null);
            Check(w.IsDown, "the Warden can still be dropped in one hit (unchanged)");
            Eq(w.LegendaryBudget, 2, "the Warden's legendary budget is unchanged");

            // ForceUnitsAreRegistered
            foreach (var id in new[] { "ashen_queen", "king_regalia", "crowned_fire", "wyrmwatch_hunter", "fae_knight", "academy_warden" })
            {
                var d = MonstersDb.Get(id);
                Check(d != null, id + " is registered");
                if (d == null) continue;
                Check(!string.IsNullOrEmpty(d.SpriteId), id + " has a sprite");
                Check(d.XP > 0, id + " gives XP");
            }
            foreach (var id in new[] { "ashen_queen", "king_regalia", "crowned_fire" })
            {
                var d = MonstersDb.Get(id);
                Check(d.Phases.Count > 0 && d.Phases.All(p => p.Beats != null && p.Beats.Length > 0), id + ": every phase has beats");
                for (int i = 1; i < d.Phases.Count; i++) Check(d.Phases[i].At < d.Phases[i - 1].At, id + ": phases in descending order");
            }

            FinaleBossesLastLongEnough();
        }

        /// <summary>
        /// The spec's anti-goal: "two or three rounds and the game is over". Each finale boss must take a typical level-13 party of
        /// four (two players, two companions; the dragon not counted, like the Warden and Gaoler tests) 6-14 rounds of at-will damage.
        /// </summary>
        static void FinaleBossesLastLongEnough()
        {
            string[] pool = { "fighter", "rogue", "cleric", "wizard", "ranger", "paladin", "bard", "druid" };
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                foreach (var id in new[] { "ashen_queen", "king_regalia", "crowned_fire" })
                {
                    var b = MonstersDb.Get(id);
                    var dpr = pool.ToDictionary(c => c, c => DamagePerRound(AtLevel(c, 13), b.AC));
                    var rounds = new List<double>();
                    for (int a = 0; a < pool.Length; a++) for (int x = a + 1; x < pool.Length; x++) for (int y = x + 1; y < pool.Length; y++) for (int z = y + 1; z < pool.Length; z++)
                        rounds.Add(b.HitDice.Average / (dpr[pool[a]] + dpr[pool[x]] + dpr[pool[y]] + dpr[pool[z]]));
                    rounds.Sort();
                    double median = rounds[rounds.Count / 2];
                    Console.WriteLine($"  {b.Name,-34} {b.HitDice.Average,4:0} HP, AC {b.AC}: median {median:0.0} rounds at level 13");
                    Check(median >= 6 && median <= 14, $"{b.Name}: a typical level-13 party needs {median:0.0} rounds (want 6-14)");
                }
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }
        }
    }
}
