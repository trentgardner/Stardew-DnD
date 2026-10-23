using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    public enum Team { Players, Enemies, Neutral }

    /// <summary>One way a creature can attack (a weapon, a natural attack, or an attack spell).</summary>
    public class AttackOption
    {
        public string Name;
        public int ToHit;
        public DiceExpr Damage;
        /// <summary>Flat modifier added to damage (ability mod, Dueling...).</summary>
        public int DamageMod;
        public DamageType DamageType;
        public int ReachFt = 5;
        public int RangeNormal, RangeLong;
        public bool Ranged;
        public WeaponMastery Mastery;
        public WeaponDef Weapon;
        public int CritOn = 20;
        /// <summary>Extra damage of a different type (magic weapons).</summary>
        public DiceExpr ExtraDamage;
        public DamageType ExtraDamageType;
        /// <summary>Great Weapon Fighting: damage dice rolling 1 or 2 count as 3.</summary>
        public int MinDieValue;
        public Ability Ability;
        public SpellDef Spell;
        public bool IsSpell => Spell != null;

        public int MaxRangeFt => Ranged ? RangeLong : ReachFt;

        public string Summary()
        {
            string dmg = Damage.ToString();
            if (DamageMod != 0) dmg += AbilityUtil.Fmt(DamageMod);
            return $"{Name}: {AbilityUtil.Fmt(ToHit)} to hit, {dmg} {DamageType.ToString().ToLower()}";
        }
    }

    public abstract class Creature
    {
        static int _nextId = 1;
        public readonly int Uid = _nextId++;

        public string Name;
        public Team Team;
        public int[] Scores = { 10, 10, 10, 10, 10, 10 };
        public int MaxHP;
        public int HP;
        public int TempHP;
        public int Exhaustion;
        public readonly List<ActiveEffect> Effects = new List<ActiveEffect>();
        public readonly HashSet<DamageType> Resistances = new HashSet<DamageType>();
        public readonly HashSet<DamageType> Immunities = new HashSet<DamageType>();
        public readonly HashSet<DamageType> Vulnerabilities = new HashSet<DamageType>();

        /// <summary>Art key for the game layer.</summary>
        public string SpriteId;

        public abstract int ProficiencyBonus { get; }
        protected abstract int BaseArmorClass { get; }
        protected virtual int BaseSpeed => 30;
        public virtual int Darkvision => 0;
        public virtual bool IsPlayerCharacter => false;

        public abstract List<AttackOption> GetAttacks();

        public int Score(Ability a) => Math.Min(30, Scores[(int)a] + ScoreBonus(a));
        protected virtual int ScoreBonus(Ability a) => 0;
        public int Mod(Ability a) => AbilityUtil.Modifier(Score(a));

        public int EffectiveMaxHP
        {
            get
            {
                int m = MaxHP;
                foreach (var e in Effects) m += e.TempMaxHp;
                return m;
            }
        }

        public int ArmorClass
        {
            get
            {
                int ac = BaseArmorClass;
                foreach (var e in Effects) ac += e.AcBonus;
                return ac;
            }
        }

        public int Speed
        {
            get
            {
                if (Has(Condition.Grappled) || Has(Condition.Restrained) || IsIncapacitated) return 0;
                int s = BaseSpeed - 5 * Exhaustion;
                foreach (var e in Effects) s -= e.SpeedPenalty;
                return Math.Max(0, s);
            }
        }

        public bool Has(Condition c)
        {
            foreach (var e in Effects) if (e.Condition == c) return true;
            if (c == Condition.Unconscious && HP <= 0 && !IsDead) return true;
            return false;
        }

        public bool HasEffect(string id)
        {
            foreach (var e in Effects) if (e.Id == id) return true;
            return false;
        }

        public void RemoveEffect(string id) => Effects.RemoveAll(e => e.Id == id);

        public void AddEffect(ActiveEffect e)
        {
            // Effects with the same id from the same source don't stack; refresh instead.
            Effects.RemoveAll(x => x.Id == e.Id && x.Source == e.Source);
            Effects.Add(e);
        }

        public bool IsIncapacitated =>
            Has(Condition.Incapacitated) || Has(Condition.Paralyzed) || Has(Condition.Stunned) ||
            Has(Condition.Unconscious) || Has(Condition.Petrified);

        public bool IsDown => HP <= 0;
        public virtual bool IsDead { get; protected set; }

        /// <summary>Penalty applied to every d20 Test (2024 Exhaustion: -2 per level).</summary>
        public int D20Penalty => -2 * Exhaustion;

        public virtual int SaveBonus(Ability a) => Mod(a);
        public virtual int SkillBonus(Skill s) => Mod(SkillUtil.AbilityOf(s));
        public virtual int InitiativeBonus => Mod(Ability.DEX);
        public virtual bool InitiativeAdvantage => false;
        public virtual bool AdvantageOnSave(Ability a, string vs) => false;

        public int PassivePerception => 10 + SkillBonus(Skill.Perception);

        /// <summary>Applies damage after resistances and temp HP. Returns the HP actually lost.</summary>
        public virtual int TakeDamage(int amount, DamageType type, bool critical, List<string> log)
        {
            if (IsDead || amount <= 0) return 0;
            if (Immunities.Contains(type)) { log?.Add($"{Name} is immune to {type.ToString().ToLower()}."); return 0; }
            if (Resistances.Contains(type)) { amount /= 2; log?.Add($"{Name} resists ({type.ToString().ToLower()}), halved to {amount}."); }
            if (Vulnerabilities.Contains(type)) { amount *= 2; log?.Add($"{Name} is vulnerable! Doubled to {amount}."); }

            if (TempHP > 0)
            {
                int absorbed = Math.Min(TempHP, amount);
                TempHP -= absorbed;
                amount -= absorbed;
            }
            int before = HP;
            HP -= amount;
            OnDamaged(before, amount, critical, log);
            return amount;
        }

        protected virtual void OnDamaged(int hpBefore, int amount, bool critical, List<string> log)
        {
            if (HP <= 0)
            {
                HP = 0;
                IsDead = true;
                log?.Add($"{Name} falls!");
            }
        }

        public virtual int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            int before = HP;
            HP = Math.Min(EffectiveMaxHP, HP + amount);
            return HP - before;
        }

        public void BreakConcentration(List<string> log)
        {
            // Remove every concentration effect this creature is maintaining, on anyone.
            // The game layer calls this with all creatures via ConcentrationRegistry.
            ConcentrationBroken?.Invoke(this);
            log?.Add($"{Name} loses concentration.");
        }

        /// <summary>Raised when this creature's concentration ends; the encounter removes linked effects.</summary>
        public static event Action<Creature> ConcentrationBroken;

        /// <summary>The spell this creature is concentrating on (null if none).</summary>
        public string ConcentratingOn;
    }
}
