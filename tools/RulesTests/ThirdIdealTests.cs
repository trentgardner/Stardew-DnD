using System;
using System.Collections.Generic;
using DnD.Rules;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Act III Phase 4: the Third Ideal holds you up at 1 HP, once per long rest.</summary>
        static void ThirdIdealTests()
        {
            Dice.Rng = new SystemRandom();
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                var f = AtLevel("fighter", 12);
                f.ThirdIdeal = true;
                var log = new List<string>();
                f.TakeDamage(f.HP, DamageType.Slashing, false, log);
                Eq(f.HP, 1, "Third Ideal: dropped to 0, held at 1 HP");
                Check(log.Exists(l => l.Contains("Third Ideal")), "Third Ideal: the log says so");
                f.TakeDamage(f.HP, DamageType.Slashing, false, log);
                Eq(f.HP, 0, "Third Ideal: only once per long rest");
                f.LongRest(true, log);
                f.TakeDamage(f.HP, DamageType.Slashing, false, log);
                Eq(f.HP, 1, "Third Ideal: back after a long rest");

                var g = AtLevel("fighter", 12);
                g.ThirdIdeal = true;
                g.TakeDamage(g.HP + g.MaxHP, DamageType.Slashing, false, log);
                Check(g.IsDead, "Third Ideal: massive damage still kills outright");

                var h = AtLevel("fighter", 12);
                h.TakeDamage(h.HP, DamageType.Slashing, false, log);
                Eq(h.HP, 0, "Third Ideal: nothing without it");
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }
        }
    }
}
