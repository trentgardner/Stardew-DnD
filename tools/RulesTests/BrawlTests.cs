using System.Collections.Generic;
using DnD.Rules;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>2024 knockout: a creature dropped to 0 in a nonlethal fight is unconscious and stable. No death saves, no massive-damage death.</summary>
        static void KnockoutTests()
        {
            var pc = Make("fighter");
            pc.Nonlethal = true;
            pc.TakeDamage(pc.HP + pc.MaxHP * 3, DamageType.Bludgeoning, false, new List<string>());
            Check(!pc.IsDead, "knockout: massive damage doesn't kill in a nonlethal fight");
            Check(pc.IsDown && pc.Stable, "knockout: unconscious and stable");
            pc.TakeDamage(5, DamageType.Bludgeoning, true, new List<string>());
            Eq(pc.DeathFailures, 0, "knockout: hits while down don't count as death save failures");
            Check(!pc.IsDead, "knockout: still alive");

            var real = Make("fighter");
            real.TakeDamage(real.HP + real.MaxHP, DamageType.Bludgeoning, false, new List<string>());
            Check(real.IsDead, "a normal fight is unchanged: massive damage still kills");
        }
    }
}
