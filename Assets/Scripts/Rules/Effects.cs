using System.Collections.Generic;

namespace DnD.Rules
{
    /// <summary>
    /// A temporary effect on a creature (spell buff, condition, weapon mastery rider...).
    /// Durations count down at the start of the *source's* turn, matching
    /// "until the start of your next turn" wording in the 2024 rules.
    /// </summary>
    public class ActiveEffect
    {
        public string Id;
        public string Name;
        public Creature Source;
        /// <summary>Remaining rounds; -1 = until removed (concentration, long-lasting buffs).</summary>
        public int RoundsLeft = -1;
        public bool Concentration;

        public int AcBonus;
        public int SpeedPenalty;
        public int AttackDie;   // Bless: +d4 to attack rolls
        public int SaveDie;     // Bless: +d4 to saving throws
        public int CheckDie;    // Guidance: +d4 to ability checks
        public int TempMaxHp;   // Aid
        public Condition? Condition;

        /// <summary>Anyone attacking this creature has Advantage (Faerie Fire, Prone in melee is handled separately).</summary>
        public bool GrantsAdvantageToAttackers;
        /// <summary>Removed after the next attack roll against this creature (Guiding Bolt).</summary>
        public bool ConsumedWhenAttacked;
        /// <summary>This creature's next attack roll has Disadvantage (Sap, Vicious Mockery).</summary>
        public bool DisadvantageOnNextAttack;
        /// <summary>Only this attacker gets Advantage on the next attack against the bearer (Vex).</summary>
        public Creature AdvantageForAttacker;

        /// <summary>Bearer repeats this save at the end of each of its turns to end the effect (Hold Person).</summary>
        public Ability? RepeatSave;
        public int RepeatSaveDC;

        public ActiveEffect Clone()
        {
            return (ActiveEffect)MemberwiseClone();
        }
    }

    /// <summary>Templates for effects referenced by spells and features.</summary>
    public static class EffectLibrary
    {
        public static ActiveEffect Create(string id, Creature source, int saveDC = 0)
        {
            switch (id)
            {
                case "bless": return new ActiveEffect { Id = id, Name = "Blessed", Source = source, RoundsLeft = 10, Concentration = true, AttackDie = 4, SaveDie = 4 };
                case "shield_of_faith": return new ActiveEffect { Id = id, Name = "Shield of Faith", Source = source, RoundsLeft = 100, Concentration = true, AcBonus = 2 };
                case "shield": return new ActiveEffect { Id = id, Name = "Shield", Source = source, RoundsLeft = 1, AcBonus = 5 };
                case "mage_armor": return new ActiveEffect { Id = id, Name = "Mage Armor", Source = source, RoundsLeft = -1 };
                case "guidance": return new ActiveEffect { Id = id, Name = "Guidance", Source = source, RoundsLeft = 10, Concentration = true, CheckDie = 4 };
                case "heroism": return new ActiveEffect { Id = id, Name = "Heroism", Source = source, RoundsLeft = 10, Concentration = true, Condition = null };
                case "aid": return new ActiveEffect { Id = id, Name = "Aid", Source = source, RoundsLeft = -1, TempMaxHp = 5 };
                case "guiding_bolt": return new ActiveEffect { Id = id, Name = "Glimmering", Source = source, RoundsLeft = 1, GrantsAdvantageToAttackers = true, ConsumedWhenAttacked = true };
                case "faerie_fire": return new ActiveEffect { Id = id, Name = "Outlined in Light", Source = source, RoundsLeft = 10, Concentration = true, GrantsAdvantageToAttackers = true };
                case "vicious_mockery": return new ActiveEffect { Id = id, Name = "Mocked", Source = source, RoundsLeft = 1, DisadvantageOnNextAttack = true };
                case "sap": return new ActiveEffect { Id = id, Name = "Sapped", Source = source, RoundsLeft = 1, DisadvantageOnNextAttack = true };
                case "slow": return new ActiveEffect { Id = id, Name = "Slowed", Source = source, RoundsLeft = 1, SpeedPenalty = 10 };
                case "ray_of_frost": return new ActiveEffect { Id = id, Name = "Chilled", Source = source, RoundsLeft = 1, SpeedPenalty = 10 };
                case "vex": return new ActiveEffect { Id = id, Name = "Vexed", Source = source, RoundsLeft = 2, AdvantageForAttacker = source, ConsumedWhenAttacked = true };
                case "prone": return new ActiveEffect { Id = id, Name = "Prone", Source = source, RoundsLeft = -1, Condition = Rules.Condition.Prone };
                case "hold_person": return new ActiveEffect { Id = id, Name = "Paralyzed", Source = source, RoundsLeft = 10, Concentration = true, Condition = Rules.Condition.Paralyzed, RepeatSave = Ability.WIS, RepeatSaveDC = saveDC };
                case "laughter": return new ActiveEffect { Id = id, Name = "Hideous Laughter", Source = source, RoundsLeft = 10, Concentration = true, Condition = Rules.Condition.Incapacitated, RepeatSave = Ability.WIS, RepeatSaveDC = saveDC };
                case "frightened": return new ActiveEffect { Id = id, Name = "Frightened", Source = source, RoundsLeft = 10, Condition = Rules.Condition.Frightened, RepeatSave = Ability.WIS, RepeatSaveDC = saveDC };
                case "charmed": return new ActiveEffect { Id = id, Name = "Charmed", Source = source, RoundsLeft = -1, Condition = Rules.Condition.Charmed };
                case "dodge": return new ActiveEffect { Id = id, Name = "Dodging", Source = source, RoundsLeft = 1 };
                case "spiritual_weapon": return new ActiveEffect { Id = id, Name = "Spiritual Weapon", Source = source, RoundsLeft = 10, Concentration = true };
                default: return new ActiveEffect { Id = id, Name = id, Source = source, RoundsLeft = 1 };
            }
        }
    }
}
