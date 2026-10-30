using System.Collections.Generic;
using DnD.Rules;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>The party's hatchling: a tiny dragon that scales with the party, breathes on a 5–6 recharge and never dies.</summary>
        static void DragonTests()
        {
            var d = new Dragonling(1);
            Eq(d.ArmorClass, 13, "dragonling: AC 13");
            Eq(d.MaxHP, 10, "dragonling: 10 HP at party level 1");
            Eq(d.HP, 10, "dragonling: starts at full HP");
            Eq(d.Team, Team.Players, "dragonling: on the players' side");
            var bite = d.GetAttacks()[0];
            Eq(bite.ToHit, 4, "dragonling: bite +4");
            Eq(bite.Damage.ToString(), "1d4+2", "dragonling: bite 1d4+2");
            Eq(d.BreathDC, 11, "dragonling: Ember Breath DC 11");
            Eq(d.BreathDamage.ToString(), "1d6", "dragonling: Ember Breath 1d6");
            Eq(d.BreathConeTiles, 1, "dragonling: 5-ft cone");
            Check(d.BreathReady, "dragonling: breath starts ready");
            Check(!d.CanFly && d.Stage == DragonStage.Hatchling, "dragonling: a hatchling can't fly");

            d.SetPartyLevel(4);
            Eq(d.MaxHP, 25, "dragonling: 25 HP at party level 4");

            // Recharge: only a 5 or a 6.
            d.BreathReady = false;
            Check(!d.RollRecharge(() => 4), "recharge: 4 fails");
            Check(!d.BreathReady, "recharge: still not ready after a 4");
            Check(d.RollRecharge(() => 5), "recharge: 5 succeeds");
            Check(d.BreathReady, "recharge: ready after a 5");

            // Damage taken survives a level change.
            var e = new Dragonling(1);
            e.TakeDamage(3, DamageType.Slashing, false, new List<string>());
            e.SetPartyLevel(2);
            Eq(e.MaxHP, 15, "dragonling: 15 HP at level 2");
            Eq(e.HP, 12, "dragonling: keeps the 3 damage it took");

            // It never dies: at 0 it's down (and retreats, in the game layer).
            e.TakeDamage(500, DamageType.Fire, true, new List<string>());
            Check(e.IsDown && !e.IsDead, "dragonling: dropped to 0 it is down, not dead");
            Eq(e.Heal(5), 5, "dragonling: can be healed back up");
        }
    }
}
