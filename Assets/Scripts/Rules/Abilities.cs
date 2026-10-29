using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    public enum Ability { STR, DEX, CON, INT, WIS, CHA }

    public enum Skill
    {
        Acrobatics, AnimalHandling, Arcana, Athletics, Deception, History, Insight, Intimidation,
        Investigation, Medicine, Nature, Perception, Performance, Persuasion, Religion, SleightOfHand,
        Stealth, Survival
    }

    public enum DamageType
    {
        Slashing, Piercing, Bludgeoning, Fire, Cold, Lightning, Thunder, Acid, Poison,
        Radiant, Necrotic, Force, Psychic
    }

    public static class AbilityUtil
    {
        public static readonly Ability[] All = { Ability.STR, Ability.DEX, Ability.CON, Ability.INT, Ability.WIS, Ability.CHA };

        public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

        public static string Fmt(int mod) => mod >= 0 ? "+" + mod : mod.ToString();

        public static string FullName(Ability a)
        {
            switch (a)
            {
                case Ability.STR: return "Strength";
                case Ability.DEX: return "Dexterity";
                case Ability.CON: return "Constitution";
                case Ability.INT: return "Intelligence";
                case Ability.WIS: return "Wisdom";
                default: return "Charisma";
            }
        }
    }

    public static class SkillUtil
    {
        public static readonly Skill[] All = (Skill[])Enum.GetValues(typeof(Skill));

        static readonly Dictionary<Skill, Ability> _ability = new Dictionary<Skill, Ability>
        {
            { Skill.Acrobatics, Ability.DEX }, { Skill.AnimalHandling, Ability.WIS }, { Skill.Arcana, Ability.INT },
            { Skill.Athletics, Ability.STR }, { Skill.Deception, Ability.CHA }, { Skill.History, Ability.INT },
            { Skill.Insight, Ability.WIS }, { Skill.Intimidation, Ability.CHA }, { Skill.Investigation, Ability.INT },
            { Skill.Medicine, Ability.WIS }, { Skill.Nature, Ability.INT }, { Skill.Perception, Ability.WIS },
            { Skill.Performance, Ability.CHA }, { Skill.Persuasion, Ability.CHA }, { Skill.Religion, Ability.INT },
            { Skill.SleightOfHand, Ability.DEX }, { Skill.Stealth, Ability.DEX }, { Skill.Survival, Ability.WIS },
        };

        public static Ability AbilityOf(Skill s) => _ability[s];
        public static Skill Parse(string s) => (Skill)Enum.Parse(typeof(Skill), s);

        public static string Name(Skill s)
        {
            switch (s)
            {
                case Skill.AnimalHandling: return "Animal Handling";
                case Skill.SleightOfHand: return "Sleight of Hand";
                default: return s.ToString();
            }
        }
    }

    /// <summary>2024 conditions plus a few engine-level states.</summary>
    public enum Condition
    {
        Blinded, Charmed, Deafened, Frightened, Grappled, Incapacitated, Invisible, Paralyzed,
        Petrified, Poisoned, Prone, Restrained, Stunned, Unconscious
    }
}
