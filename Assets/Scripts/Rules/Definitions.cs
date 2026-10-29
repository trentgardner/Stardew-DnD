using System.Collections.Generic;

namespace DnD.Rules
{
    public enum CastTime { Action, BonusAction, Reaction, Minute, TenMinutes }

    /// <summary>How a spell resolves mechanically. The engine handles each kind generically.</summary>
    public enum SpellKind
    {
        /// <summary>Spell attack roll vs AC, then damage.</summary>
        Attack,
        /// <summary>Targets make a saving throw; damage (half on success if HalfOnSave).</summary>
        Save,
        /// <summary>Hits automatically (Magic Missile).</summary>
        AutoHit,
        /// <summary>Restores HP.</summary>
        Heal,
        /// <summary>Applies a beneficial effect to allies.</summary>
        Buff,
        /// <summary>Out-of-combat / dialogue / exploration utility.</summary>
        Utility
    }

    public enum TargetShape { Single, Self, Sphere, Cone, Cube, Line }

    public class SpellDef
    {
        public string Id;
        public string Name;
        public int Level;              // 0 = cantrip
        public string School;
        public CastTime CastTime = CastTime.Action;
        public int RangeFt;            // 0 = self, 5 = touch
        public SpellKind Kind;
        public TargetShape Shape = TargetShape.Single;
        public int AreaFt;             // radius / cone length / cube side
        public int MaxTargets = 1;
        public DiceExpr Dice;          // damage or healing
        public bool AddSpellMod;       // healing spells add the spellcasting modifier
        public DamageType DamageType;
        public Ability SaveAbility;
        public bool HalfOnSave;
        public bool Concentration;
        public int DurationRounds;     // for buffs/debuffs in combat
        /// <summary>Number of separate attacks or darts (Scorching Ray = 3, Magic Missile = 3).</summary>
        public int Projectiles = 1;
        /// <summary>Extra dice per slot level above the spell's level.</summary>
        public int UpcastDice;
        /// <summary>Effect key applied on hit / failed save / to buff targets (see EffectLibrary).</summary>
        public string EffectId;
        public bool Ritual;
        public string Description;
        public List<string> Classes = new List<string>();

        public bool IsCantrip => Level == 0;
    }

    public class SpeciesDef
    {
        public string Id;
        public string Name;
        public int Speed = 30;
        public int Darkvision;
        public string Description;
        public List<string> Traits = new List<string>();
        /// <summary>Skills the species grants directly (Elf: Perception via Keen Senses).</summary>
        public List<Skill> GrantedSkills = new List<Skill>();
        /// <summary>Number of additional skills of the player's choice (Human: Skillful = 1, Half-Elf: 2).</summary>
        public int BonusSkillChoices;
        public bool AdvantageVsCharmed;       // Fey Ancestry
        public bool AdvantageMentalSaves;     // Gnomish Cunning (INT/WIS/CHA saves)
        public bool PoisonResilience;         // Dwarven Resilience
        public int BonusHpPerLevel;           // Dwarven Toughness
        public bool HeroicInspirationOnRest;  // Human: Resourceful
    }

    public class BackgroundDef
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>The three abilities the background can raise (+2/+1 or +1/+1/+1).</summary>
        public Ability[] AbilityOptions;
        public Skill[] Skills;
        public string OriginFeat;
        public string OriginFeatText;
        public int StartingGoldCp;
    }

    public class SubclassDef
    {
        public string Id;
        public string Name;
        public string Description;
        public List<FeatureDef> Features = new List<FeatureDef>();
        public List<string> AlwaysPreparedSpells = new List<string>();
    }

    public class FeatureDef
    {
        public int Level;
        public string Name;
        public string Text;
        public FeatureDef(int level, string name, string text) { Level = level; Name = name; Text = text; }
    }

    public enum CasterType { None, Full, Half }

    public class ClassDef
    {
        public string Id;
        public string Name;
        public int HitDie;
        public Ability PrimaryAbility;
        public Ability[] SavingThrows;
        public Skill[] SkillOptions;   // empty = any skill (Bard)
        public int SkillChoices;
        public bool LightArmor, MediumArmor, HeavyArmor, Shields;
        public bool MartialWeapons;
        /// <summary>Rogue: martial weapons that have the Finesse or Light property.</summary>
        public bool MartialFinesseOrLight;
        /// <summary>Class spells that are always prepared from a level on (Paladin's Smite: divine_smite at 2). Don't count against the limit.</summary>
        public Dictionary<string, int> AlwaysPrepared = new Dictionary<string, int>();
        public CasterType Caster;
        public Ability SpellAbility;
        public int[] CantripsKnown = new int[21];   // by level
        public int[] PreparedSpells = new int[21];  // by level
        public int WeaponMasteries;
        public string Description;
        public List<FeatureDef> Features = new List<FeatureDef>();
        public SubclassDef Subclass;   // playtest: one subclass per class (SRD 5.2)
        public List<string> StartingItems = new List<string>();
        public string StartingWeapon;
        public string StartingArmor;
        public bool StartingShield;
    }

    public static class SpellSlots
    {
        // Full caster slot table (levels 1-20, slot levels 1-9).
        static readonly int[,] Full =
        {
            {0,0,0,0,0,0,0,0,0},
            {2,0,0,0,0,0,0,0,0},{3,0,0,0,0,0,0,0,0},{4,2,0,0,0,0,0,0,0},{4,3,0,0,0,0,0,0,0},
            {4,3,2,0,0,0,0,0,0},{4,3,3,0,0,0,0,0,0},{4,3,3,1,0,0,0,0,0},{4,3,3,2,0,0,0,0,0},
            {4,3,3,3,1,0,0,0,0},{4,3,3,3,2,0,0,0,0},{4,3,3,3,2,1,0,0,0},{4,3,3,3,2,1,0,0,0},
            {4,3,3,3,2,1,1,0,0},{4,3,3,3,2,1,1,0,0},{4,3,3,3,2,1,1,1,0},{4,3,3,3,2,1,1,1,0},
            {4,3,3,3,2,1,1,1,1},{4,3,3,3,3,1,1,1,1},{4,3,3,3,3,2,1,1,1},{4,3,3,3,3,2,2,1,1},
        };

        // Half casters (Paladin, Ranger; 2024 rules: spellcasting from level 1), levels 1-20, slot levels 1-5.
        static readonly int[,] Half =
        {
            {0,0,0,0,0},
            {2,0,0,0,0},{2,0,0,0,0},{3,0,0,0,0},{3,0,0,0,0},{4,2,0,0,0},{4,2,0,0,0},{4,3,0,0,0},{4,3,0,0,0},
            {4,3,2,0,0},{4,3,2,0,0},{4,3,3,0,0},{4,3,3,0,0},{4,3,3,1,0},{4,3,3,1,0},{4,3,3,2,0},{4,3,3,2,0},
            {4,3,3,3,1},{4,3,3,3,1},{4,3,3,3,2},{4,3,3,3,2},
        };

        public static int For(CasterType type, int classLevel, int slotLevel)
        {
            if (type == CasterType.None || slotLevel < 1 || slotLevel > 9) return 0;
            if (type == CasterType.Half) return slotLevel > 5 ? 0 : Half[classLevel, slotLevel - 1];
            return Full[classLevel, slotLevel - 1];
        }
    }
}
