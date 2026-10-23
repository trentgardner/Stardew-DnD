using System;
using System.Collections.Generic;
using System.Text;

namespace DnD.Rules
{
    /// <summary>Random source abstraction so tests can force specific rolls.</summary>
    public interface IRandom
    {
        /// <summary>Returns an int in [1, sides].</summary>
        int RollDie(int sides);
    }

    public sealed class SystemRandom : IRandom
    {
        readonly Random _rng;
        public SystemRandom(int? seed = null) { _rng = seed.HasValue ? new Random(seed.Value) : new Random(); }
        public int RollDie(int sides) => _rng.Next(1, sides + 1);
    }

    /// <summary>Plays back a fixed list of results (used by tests).</summary>
    public sealed class ScriptedRandom : IRandom
    {
        readonly Queue<int> _values;
        public ScriptedRandom(params int[] values) { _values = new Queue<int>(values); }
        public int RollDie(int sides) => _values.Count > 0 ? Math.Min(sides, _values.Dequeue()) : 1;
    }

    public enum AdvantageState { Normal, Advantage, Disadvantage }

    public enum DieColor { D20, Damage, Healing, Utility }

    /// <summary>A single physical die that was rolled. The UI animates these.</summary>
    public struct DieRoll
    {
        public int Sides;
        public int Value;
        /// <summary>True when this die was rolled but not used (the lower of an advantage pair, etc.).</summary>
        public bool Discarded;
        public DieRoll(int sides, int value, bool discarded = false) { Sides = sides; Value = value; Discarded = discarded; }
        public override string ToString() => Discarded ? $"({Value})" : Value.ToString();
    }

    /// <summary>"2d6+3"-style dice expression.</summary>
    [Serializable]
    public struct DiceExpr
    {
        public int Count;
        public int Sides;
        public int Bonus;

        public DiceExpr(int count, int sides, int bonus = 0) { Count = count; Sides = sides; Bonus = bonus; }

        public static DiceExpr None => new DiceExpr(0, 0, 0);
        public bool IsNone => Count == 0 && Bonus == 0;

        public DiceExpr WithBonus(int b) => new DiceExpr(Count, Sides, Bonus + b);
        public DiceExpr WithExtraDice(int n) => new DiceExpr(Count + n, Sides, Bonus);

        public double Average => Count * (Sides + 1) / 2.0 + Bonus;

        public static DiceExpr Parse(string s)
        {
            s = s.Replace(" ", "").ToLowerInvariant();
            int bonus = 0;
            int signIdx = s.IndexOfAny(new[] { '+', '-' }, 1);
            if (signIdx > 0)
            {
                bonus = int.Parse(s.Substring(signIdx));
                s = s.Substring(0, signIdx);
            }
            int d = s.IndexOf('d');
            if (d < 0) return new DiceExpr(0, 0, int.Parse(s) + bonus);
            int count = d == 0 ? 1 : int.Parse(s.Substring(0, d));
            int sides = int.Parse(s.Substring(d + 1));
            return new DiceExpr(count, sides, bonus);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Count > 0) sb.Append(Count).Append('d').Append(Sides);
            if (Bonus > 0) sb.Append(Count > 0 ? "+" : "").Append(Bonus);
            else if (Bonus < 0) sb.Append(Bonus);
            if (sb.Length == 0) sb.Append('0');
            return sb.ToString();
        }
    }

    /// <summary>A labeled modifier in a roll breakdown, e.g. ("STR", +3).</summary>
    public struct RollTerm
    {
        public string Label;
        public int Value;
        public RollTerm(string label, int value) { Label = label; Value = value; }
        public override string ToString() => (Value >= 0 ? "+" : "") + Value + " " + Label;
    }

    /// <summary>Result of a d20 Test (ability check, saving throw or attack roll).</summary>
    public sealed class D20Result
    {
        public AdvantageState Advantage;
        public List<DieRoll> Dice = new List<DieRoll>();
        /// <summary>Extra dice added on top (Bless d4, Bardic Inspiration, Guidance...).</summary>
        public List<DieRoll> BonusDice = new List<DieRoll>();
        public List<RollTerm> Terms = new List<RollTerm>();

        public int Natural
        {
            get
            {
                foreach (var d in Dice) if (!d.Discarded) return d.Value;
                return 1;
            }
        }
        public int Modifier
        {
            get
            {
                int m = 0;
                foreach (var t in Terms) m += t.Value;
                foreach (var b in BonusDice) m += b.Sides < 0 ? -b.Value : b.Value;
                return m;
            }
        }
        public int Total => Natural + Modifier;
        public bool IsNat20 => Natural == 20;
        public bool IsNat1 => Natural == 1;

        public string Breakdown()
        {
            var sb = new StringBuilder();
            sb.Append("d20");
            if (Advantage != AdvantageState.Normal)
            {
                sb.Append(Advantage == AdvantageState.Advantage ? " (adv: " : " (dis: ");
                sb.Append(string.Join(", ", Dice));
                sb.Append(')');
            }
            sb.Append(" = ").Append(Natural);
            foreach (var t in Terms) sb.Append(' ').Append(t.Value >= 0 ? "+" : "").Append(t.Value).Append(' ').Append(t.Label);
            foreach (var b in BonusDice) sb.Append(" +").Append(b.Value).Append(" (d").Append(Math.Abs(b.Sides)).Append(')');
            sb.Append(" = ").Append(Total);
            return sb.ToString();
        }
    }

    /// <summary>Result of rolling a damage or healing expression.</summary>
    public sealed class DamageRoll
    {
        public DiceExpr Expr;
        public bool Critical;
        public List<DieRoll> Dice = new List<DieRoll>();
        public List<RollTerm> Terms = new List<RollTerm>();
        /// <summary>Additional damage of another type rolled alongside (e.g. +1d4 fire from a magic weapon).</summary>
        public DamageRoll Extra;
        public DamageType ExtraType;

        public int Total
        {
            get
            {
                int t = 0;
                foreach (var d in Dice) if (!d.Discarded) t += d.Value;
                foreach (var x in Terms) t += x.Value;
                return Math.Max(0, t);
            }
        }
        public string Breakdown()
        {
            var sb = new StringBuilder();
            sb.Append(Expr.Count * (Critical ? 2 : 1)).Append('d').Append(Expr.Sides);
            if (Critical) sb.Append(" (CRIT)");
            sb.Append(": [").Append(string.Join(", ", Dice)).Append(']');
            foreach (var t in Terms) sb.Append(' ').Append(t.Value >= 0 ? "+" : "").Append(t.Value).Append(' ').Append(t.Label);
            sb.Append(" = ").Append(Total);
            if (Extra != null) sb.Append("  +  ").Append(Extra.Breakdown()).Append(' ').Append(ExtraType.ToString().ToLower());
            return sb.ToString();
        }
    }

    public static class Dice
    {
        public static IRandom Rng = new SystemRandom();

        public static int Roll(int sides) => Rng.RollDie(sides);

        public static D20Result RollD20(AdvantageState adv, IEnumerable<RollTerm> terms = null)
        {
            var r = new D20Result { Advantage = adv };
            int a = Rng.RollDie(20);
            if (adv == AdvantageState.Normal)
            {
                r.Dice.Add(new DieRoll(20, a));
            }
            else
            {
                int b = Rng.RollDie(20);
                bool keepA = adv == AdvantageState.Advantage ? a >= b : a <= b;
                r.Dice.Add(new DieRoll(20, a, !keepA));
                r.Dice.Add(new DieRoll(20, b, keepA));
            }
            if (terms != null) r.Terms.AddRange(terms);
            return r;
        }

        /// <summary>Adds a bonus die to a d20 test (Bless, Guidance, Bardic Inspiration). Negative sides = subtract.</summary>
        public static int AddBonusDie(D20Result r, int sides)
        {
            int v = Rng.RollDie(Math.Abs(sides));
            r.BonusDice.Add(new DieRoll(sides, v));
            return v;
        }

        /// <summary>
        /// Rolls damage. A critical doubles the number of dice (2024 rules / Critical Role).
        /// minDieValue implements Great Weapon Fighting (treat 1s and 2s as 3s).
        /// </summary>
        public static DamageRoll RollDamage(DiceExpr expr, bool critical = false, IEnumerable<RollTerm> terms = null, int minDieValue = 0)
        {
            var r = new DamageRoll { Expr = expr, Critical = critical };
            int n = expr.Count * (critical ? 2 : 1);
            for (int i = 0; i < n; i++)
            {
                int v = Rng.RollDie(expr.Sides);
                if (v < minDieValue) v = minDieValue;
                r.Dice.Add(new DieRoll(expr.Sides, v));
            }
            if (expr.Bonus != 0) r.Terms.Add(new RollTerm("bonus", expr.Bonus));
            if (terms != null) r.Terms.AddRange(terms);
            return r;
        }

        /// <summary>Rolls 4d6 and drops the lowest. Returns the four dice (lowest marked discarded).</summary>
        public static List<DieRoll> Roll4d6DropLowest()
        {
            var dice = new List<DieRoll>();
            int lowestIdx = 0;
            for (int i = 0; i < 4; i++)
            {
                dice.Add(new DieRoll(6, Rng.RollDie(6)));
                if (dice[i].Value < dice[lowestIdx].Value) lowestIdx = i;
            }
            var low = dice[lowestIdx];
            low.Discarded = true;
            dice[lowestIdx] = low;
            return dice;
        }

        public static int SumKept(List<DieRoll> dice)
        {
            int s = 0;
            foreach (var d in dice) if (!d.Discarded) s += d.Value;
            return s;
        }

        public static AdvantageState Combine(bool advantage, bool disadvantage)
        {
            if (advantage && !disadvantage) return AdvantageState.Advantage;
            if (disadvantage && !advantage) return AdvantageState.Disadvantage;
            return AdvantageState.Normal;
        }
    }
}
