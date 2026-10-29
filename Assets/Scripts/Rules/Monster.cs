using System.Collections.Generic;

namespace DnD.Rules
{
    public class MonsterAttackDef
    {
        public string Name;
        public int ToHit;
        public DiceExpr Damage;
        public DamageType DamageType;
        public int ReachFt = 5;
        public int RangeNormal, RangeLong;
        public bool Ranged;
        /// <summary>Optional rider effect id on hit (e.g. "prone" for a wolf bite).</summary>
        public string OnHitEffect;
        public int OnHitSaveDC;
        public Ability OnHitSave;
    }

    public enum MonsterBehavior { Melee, Ranged, Skirmisher, Coward }

    public class MonsterDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int AC;
        public DiceExpr HitDice;
        public int Speed = 30;
        public int[] Scores = { 10, 10, 10, 10, 10, 10 };
        public int ProficiencyBonus = 2;
        public int Darkvision;
        public double CR;
        public int XP;
        public List<MonsterAttackDef> Attacks = new List<MonsterAttackDef>();
        public List<string> Traits = new List<string>();
        public bool PackTactics;
        public bool SunlightSensitivity;
        /// <summary>Can Disengage or Hide as a Bonus Action (Goblin Nimble Escape).</summary>
        public bool NimbleEscape;
        public MonsterBehavior Behavior = MonsterBehavior.Melee;
        public List<Skill> ProficientSkills = new List<Skill>();
        public List<Ability> ProficientSaves = new List<Ability>();
        public List<DamageType> Resistances = new List<DamageType>();
        public string SpriteId;
        /// <summary>Loot table entries: item id and chance (0-1).</summary>
        public List<KeyValuePair<string, float>> Loot = new List<KeyValuePair<string, float>>();
        public int CopperMin, CopperMax;
    }

    public class Monster : Creature
    {
        public readonly MonsterDef Def;
        public int Speed30 => Def.Speed;

        public Monster(MonsterDef def, bool rollHp = true)
        {
            Def = def;
            Name = def.Name;
            Team = Team.Enemies;
            Scores = (int[])def.Scores.Clone();
            MaxHP = rollHp ? System.Math.Max(1, Dice.RollDamage(def.HitDice).Total) : (int)def.HitDice.Average;
            HP = MaxHP;
            SpriteId = def.SpriteId;
            foreach (var r in def.Resistances) Resistances.Add(r);
        }

        public override int ProficiencyBonus => Def.ProficiencyBonus;
        protected override int BaseArmorClass => Def.AC;
        protected override int BaseSpeed => Def.Speed;
        public override int Darkvision => Def.Darkvision;

        public override int SkillBonus(Skill s)
        {
            int b = Mod(SkillUtil.AbilityOf(s));
            if (Def.ProficientSkills.Contains(s)) b += ProficiencyBonus;
            return b;
        }

        public override int SaveBonus(Ability a)
        {
            int b = Mod(a);
            if (Def.ProficientSaves.Contains(a)) b += ProficiencyBonus;
            return b;
        }

        public override List<AttackOption> GetAttacks() => AttacksOf(Def);

        /// <summary>A stat block's attacks as attack options (also used by a druid in Wild Shape).</summary>
        public static List<AttackOption> AttacksOf(MonsterDef def)
        {
            var list = new List<AttackOption>();
            foreach (var a in def.Attacks)
            {
                list.Add(new AttackOption
                {
                    Name = a.Name,
                    ToHit = a.ToHit,
                    Damage = a.Damage,
                    DamageMod = 0, // monster damage expressions already include the modifier
                    DamageType = a.DamageType,
                    ReachFt = a.ReachFt,
                    RangeNormal = a.RangeNormal,
                    RangeLong = a.RangeLong,
                    Ranged = a.Ranged,
                });
            }
            return list;
        }

        public MonsterAttackDef DefFor(AttackOption opt)
        {
            foreach (var a in Def.Attacks) if (a.Name == opt.Name) return a;
            return null;
        }
    }
}
