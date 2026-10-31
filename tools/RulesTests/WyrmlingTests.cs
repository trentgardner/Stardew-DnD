using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Act II: the teenage dragon (Wyrmling stage). Useful, never fight-carrying.</summary>
        static void WyrmlingTests()
        {
            // WyrmlingStatsScaleWithParty
            var w = new Dragonling(5) { Stage = DragonStage.Wyrmling };
            w.SetPartyLevel(5);
            Eq(w.ArmorClass, 15, "wyrmling: AC 15");
            Eq(w.MaxHP, 40, "wyrmling: 10 + 6 per party level (40 at 5)");
            Eq(w.BreathDamage.ToString(), "3d6", "wyrmling breath at 5: 3d6");
            Eq(w.BreathConeTiles, 3, "wyrmling breath: a 15-ft cone");
            w.SetPartyLevel(10);
            Eq(w.MaxHP, 70, "wyrmling: 70 HP at 10");
            Eq(w.BreathDamage.ToString(), "5d6", "wyrmling breath at 10: 5d6");
            Check(!w.CanFly, "a teenager still can't fly (Act III)");
            // WyrmlingMultiattackBiteAndClaw
            var routine = w.AttackRoutine();
            Eq(string.Join(",", routine.Select(a => a.Name)), "Bite,Claw", "wyrmling: bite and claw");
            // HatchlingUnchanged
            var h = new Dragonling(5);
            Eq(h.ArmorClass, 13, "the hatchling keeps AC 13");
            Eq(h.AttackRoutine().Count, 1, "and one bite");
            Eq(h.BreathDamage.ToString(), "1d6", "and its little puff");
            // WyrmlingStaysUnderMartialDamage
            PlayerCharacter.LevelCap = 10;
            try
            {
                foreach (int lv in new[] { 5, 7, 10 })
                {
                    var d = new Dragonling(lv) { Stage = DragonStage.Wyrmling };
                    d.SetPartyLevel(lv);
                    double dpr = d.AttackRoutine().Sum(a => ExpectedDamage(a, 15));
                    double fighter = DamagePerRound(AtLevel("fighter", lv), 15);
                    Check(dpr < 0.6 * fighter, $"wyrmling L{lv}: {dpr:0.0} DPR stays under 60% of a fighter's {fighter:0.0}");
                    Check(dpr > 0.3 * fighter, $"wyrmling L{lv}: {dpr:0.0} DPR is still useful (over 30% of {fighter:0.0})");
                }
            }
            finally { PlayerCharacter.LevelCap = 4; }
        }
    }
}
