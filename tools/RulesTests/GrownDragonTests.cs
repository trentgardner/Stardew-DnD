using System;
using System.Linq;
using DnD.Rules;

namespace RulesTests
{
    public static partial class Program
    {
        static void GrownDragonTests()
        {
            Dice.Rng = new SystemRandom();

            // GrownDragonNumbers
            var g = new Dragonling(12) { Stage = DragonStage.Young };
            Check(g.IsGrown, "Young is grown");
            Eq(g.ArmorClass, 17, "grown dragon AC 17");
            Eq(g.MaxHP, 116, "20 + 8 per party level (12 → 116)");
            Eq(g.AttackRoutine().Count, 2, "bite and claw");
            Check(g.AttackRoutine().All(a => a.ToHit == 8), "+PB+4 to hit (PB 4 at 12 → +8)");
            Eq(g.GetAttacks()[0].Damage.ToString(), "1d10+4", "bite 1d10+4");
            Eq(g.BreathConeTiles, 6, "a 30-ft cone");
            Check(g.CanFly, "big enough to fly (the finale only)");

            // TeenUnchanged
            var t = new Dragonling(10) { Stage = DragonStage.Wyrmling };
            Check(!t.IsGrown, "a teenager isn't grown");
            Eq(t.ArmorClass, 15, "teen AC 15");
            Eq(t.BreathConeTiles, 3, "teen cone 15 ft");

            // GrownDragonStaysBelowTheParty (the user: "I have a dragon on my side", never carrying a fight)
            string[] martials = { "fighter", "barbarian", "paladin", "ranger", "rogue", "monk" };
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                for (int level = 11; level <= 13; level++)
                {
                    double party = martials.Average(c => DamagePerRound(AtLevel(c, level), 17));
                    double dragon = Dragonling.ExpectedDamagePerRound(new Dragonling(level) { Stage = DragonStage.Young }, 17);
                    double ratio = dragon / party;
                    Console.WriteLine($"L{level} grown dragon {dragon:0.0} vs martial average {party:0.0} = {ratio:P0}");
                    Check(ratio >= 0.5 && ratio <= 0.75, $"L{level}: the grown dragon deals {ratio:P0} of a martial's damage (want 50-75%)");
                }
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }
        }
    }
}
