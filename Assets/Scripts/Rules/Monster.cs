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

    /// <summary>A save-based area ability with a recharge (a hound's fire breath, an ankheg's acid spray, later the dragons' breath).</summary>
    public class MonsterAbilityDef
    {
        public string Name;
        public TargetShape Shape = TargetShape.Cone;
        public int AreaFt = 15;
        public DiceExpr Dice;
        public DamageType DamageType;
        public Ability SaveAbility = Ability.DEX;
        public int SaveDC = 12;
        /// <summary>Recharges on a d6 roll of at least this at the start of the monster's turn (5 = "Recharge 5-6").</summary>
        public int RechargeMin = 5;
        /// <summary>A boss's breath: it spends a turn drawing breath (visible to everyone), and lets go at the start of its next turn.</summary>
        public bool Telegraph;
    }

    public enum LegendaryKind { Attack, SaveBurst }

    /// <summary>A legendary action (SRD): taken right after another creature's turn, from a budget that refills on its own turn.</summary>
    public class LegendaryActionDef
    {
        public string Name;
        public int Cost = 1;
        public LegendaryKind Kind;
        /// <summary>Attack: the name of one of the monster's Attacks.</summary>
        public string Attack;
        public int RangeFt = 5;
        /// <summary>SaveBurst: one creature in range makes a save against this damage (half on a success).</summary>
        public DiceExpr Dice;
        public DamageType DamageType;
        public Ability SaveAbility = Ability.DEX;
        public int SaveDC = 15;
    }

    public class MonsterDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int AC;
        public DiceExpr HitDice;
        public int Speed = 30;
        /// <summary>Flying speed (0 = can't fly). Flyers cross water and chasms on the grid.</summary>
        public int FlySpeed;
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
        public List<DamageType> Immunities = new List<DamageType>();
        public List<DamageType> Vulnerabilities = new List<DamageType>();
        /// <summary>Attack names used by one Attack action (Multiattack), e.g. Bite, Claw, Claw. Empty = its first attack, once.</summary>
        public List<string> Multiattack = new List<string>();
        public List<MonsterAbilityDef> Abilities = new List<MonsterAbilityDef>();
        /// <summary>Legendary actions and how many it gets per round (0 = none).</summary>
        public List<LegendaryActionDef> LegendaryActions = new List<LegendaryActionDef>();
        public int LegendaryPerRound;
        /// <summary>A world flag that, once set, lets a hit while it draws breath choke it (the party knows the weak spot).</summary>
        public string WeakSpotFlag;
        /// <summary>How big the sprite is drawn (the footprint stays one tile).</summary>
        public float Scale = 1f;
        /// <summary>Phase two: at or under this fraction of max HP (0 = no phase two). Then breath recharges on PhaseTwoRechargeMin and the lair erupts.</summary>
        public double PhaseTwoAt;
        public string PhaseTwoText;
        public int PhaseTwoRechargeMin;
        /// <summary>Phase two: tiles of the floor that erupt at the start of each round.</summary>
        public int LairEruptions;
        /// <summary>HP regained at the start of each of its turns (Troll), unless it took one of RegenBlockedBy since its last turn.</summary>
        public int Regeneration;
        public List<DamageType> RegenBlockedBy = new List<DamageType>();
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
            foreach (var r in def.Immunities) Immunities.Add(r);
            foreach (var r in def.Vulnerabilities) Vulnerabilities.Add(r);
            foreach (var a in def.Abilities) _charged.Add(a);
            LegendaryLeft = def.LegendaryPerRound;
        }

        readonly HashSet<MonsterAbilityDef> _charged = new HashSet<MonsterAbilityDef>();

        /// <summary>Legendary actions left this round.</summary>
        public int LegendaryLeft;
        public void ResetLegendary() => LegendaryLeft = Def.LegendaryPerRound;
        public bool SpendLegendary(int cost)
        {
            if (cost <= 0 || LegendaryLeft < cost) return false;
            LegendaryLeft -= cost;
            return true;
        }

        /// <summary>The breath it is drawing right now (a Telegraph ability), or null.</summary>
        public MonsterAbilityDef Charging;
        public bool PhaseTwo { get; private set; }

        /// <summary>Returns PhaseTwoText the first time its HP is at or under PhaseTwoAt of max (and switches to phase two); else null.</summary>
        public string CheckPhaseTwo()
        {
            if (PhaseTwo || Def.PhaseTwoAt <= 0 || HP > Def.PhaseTwoAt * MaxHP) return null;
            PhaseTwo = true;
            return Def.PhaseTwoText ?? $"{Name} grows desperate.";
        }

        public int RechargeMinFor(MonsterAbilityDef a) => PhaseTwo && Def.PhaseTwoRechargeMin > 0 ? Def.PhaseTwoRechargeMin : a.RechargeMin;
        bool _regenBlocked;

        public bool IsCharged(MonsterAbilityDef a) => _charged.Contains(a);
        public void Spend(MonsterAbilityDef a) => _charged.Remove(a);

        /// <summary>Start of its turn: a d6 for each spent ability; it comes back on RechargeMin or higher.</summary>
        public void RollRecharges(List<string> log)
        {
            foreach (var a in Def.Abilities)
            {
                if (_charged.Contains(a)) continue;
                int d = Dice.Roll(6);
                if (d >= RechargeMinFor(a)) { _charged.Add(a); log?.Add($"{Name}'s {a.Name} recharges! (d6: {d})"); }
            }
        }

        /// <summary>Start of its turn: Regeneration. Returns HP regained.</summary>
        public int StartTurnRegen(List<string> log)
        {
            bool blocked = _regenBlocked;
            _regenBlocked = false;
            if (Def.Regeneration <= 0 || IsDead || IsDown || blocked) return 0;
            int h = Heal(Def.Regeneration);
            if (h > 0) log?.Add($"{Name} regenerates {h} HP.");
            return h;
        }

        public override int TakeDamage(int amount, DamageType type, bool critical, List<string> log)
        {
            int lost = base.TakeDamage(amount, type, critical, log);
            if (Def.RegenBlockedBy.Contains(type) && amount > 0 && !Immunities.Contains(type)) _regenBlocked = true;
            return lost;
        }

        /// <summary>The attacks one Attack action makes (Multiattack), in order.</summary>
        public virtual List<AttackOption> AttackRoutine()
        {
            var all = GetAttacks();
            var list = new List<AttackOption>();
            foreach (var name in Def.Multiattack)
            {
                var a = all.Find(x => x.Name == name);
                if (a != null) list.Add(a);
            }
            if (list.Count == 0 && all.Count > 0) list.Add(all.Find(x => !x.Ranged) ?? all[0]);
            return list;
        }

        /// <summary>Whether a Multiattack goes on: a monster that fell mid-routine stops.</summary>
        public static bool KeepsAttacking(Creature c) => !c.IsDead && !c.IsDown && !c.IsIncapacitated;

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
