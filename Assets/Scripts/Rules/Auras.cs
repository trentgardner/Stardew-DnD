using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    /// <summary>Answers "who is within N feet of this creature?" (the combat layer knows positions; the rules don't).</summary>
    public interface IAuraSource
    {
        /// <summary>Creatures within `feet` of c, including c itself.</summary>
        IEnumerable<Creature> Within(Creature c, int feet);
    }

    /// <summary>Paladin auras (Protection 6, Devotion 7, Courage 10).</summary>
    public static class Auras
    {
        /// <summary>Set by the game while a fight is running; null means "nobody else is near" (only your own aura counts).</summary>
        public static IAuraSource Source;

        static IEnumerable<Creature> Near(Creature c, int feet) => Source != null ? Source.Within(c, feet) : new[] { c };

        static bool Projects(Creature p, Creature c, string feature) =>
            p is PlayerCharacter pc && pc.Team == c.Team && pc.HasFeature(feature) && !pc.IsDown && !pc.IsIncapacitated;

        /// <summary>Aura of Protection: the best CHA modifier (min +1) among conscious paladins within 10 ft.</summary>
        public static int ProtectionBonus(Creature c)
        {
            int best = 0;
            foreach (var p in Near(c, 10))
                if (Projects(p, c, "Aura of Protection")) best = Math.Max(best, Math.Max(1, p.Mod(Ability.CHA)));
            return best;
        }

        /// <summary>Bard 7: Countercharm: an ally within 30 ft of a conscious bard has Advantage on saves against being Charmed or Frightened.</summary>
        public static bool Countercharm(Creature c)
        {
            foreach (var p in Near(c, 30)) if (Projects(p, c, "Countercharm")) return true;
            return false;
        }

        /// <summary>Aura of Devotion (no Charmed) and Aura of Courage (no Frightened).</summary>
        public static bool Immune(Creature c, Condition cond)
        {
            string feature = cond == Condition.Charmed ? "Aura of Devotion" : cond == Condition.Frightened ? "Aura of Courage" : null;
            if (feature == null) return false;
            foreach (var p in Near(c, 10)) if (Projects(p, c, feature)) return true;
            return false;
        }
    }
}
