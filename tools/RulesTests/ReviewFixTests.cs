using System.Collections.Generic;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Act II Phase 1 final-review findings.</summary>
        static void ReviewFixTests()
        {
            // OwlFormFliesAtItsFlyingSpeed
            var d8 = AtLevelTen("druid", 8);
            d8.StartWildShape("giant_owl", null);
            Eq(d8.Speed, 60, "a druid shaped as a giant owl moves at its 60 ft flying speed, not 5 ft");
            // LongRestEndsConcentration
            var c = AtLevelTen("cleric", 5);
            c.ConcentratingOn = "spirit_guardians";
            c.LongRest(true, null);
            Check(c.ConcentratingOn == null, "a long rest ends concentration");
        }
    }
}
