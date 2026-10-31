using System;
using System.Collections.Generic;
using DnD.Rules.Content;

namespace DnD.Rules
{
    /// <summary>Growth hooks for later acts. Only the hatchling exists in Act I.</summary>
    public enum DragonStage { Hatchling, Wyrmling, Young, Adult }

    /// <summary>
    /// The whole party's baby dragon. Useful, never fight-carrying: AC 13, 5 + 5 HP per party level,
    /// a 1d4+2 bite and Ember Breath (5-ft cone, 1d6 fire, DEX save, recharge 5–6).
    /// It never dies: at 0 HP it is down, and the game sends it off scared until the next rest.
    /// </summary>
    public class Dragonling : Monster
    {
        DragonStage _stage = DragonStage.Hatchling;
        public DragonStage Stage { get => _stage; set { _stage = value; SetPartyLevel(_partyLevel); } }
        public bool IsTeen => Stage >= DragonStage.Wyrmling;
        public bool CanFly => Stage >= DragonStage.Young;
        public bool BreathReady = true;
        int _partyLevel = 1;
        /// <summary>The hatchling keeps its stat-block numbers; the teenager grows with the party's proficiency.</summary>
        public override int ProficiencyBonus => IsTeen ? 2 + (_partyLevel - 1) / 4 : base.ProficiencyBonus;
        public int BreathDC => 8 + ProficiencyBonus + Mod(Ability.CON);
        /// <summary>Hatchling: a 1d6 puff. Teenager: ceil(party level / 2) d6 (3d6 at 5, 5d6 at 10).</summary>
        public DiceExpr BreathDamage => IsTeen ? new DiceExpr((_partyLevel + 1) / 2, 6) : new DiceExpr(1, 6);
        public int BreathConeTiles => IsTeen ? 3 : 1;
        protected override int BaseArmorClass => IsTeen ? 15 : base.BaseArmorClass;

        /// <summary>
        /// Teenager: bite and claw, +PB+2 to hit, 1d6+2 and 1d4+2. Tuned to roughly half a fighter's damage per round:
        /// useful, never fight-carrying (BG3's Us).
        /// </summary>
        public override List<AttackOption> GetAttacks()
        {
            if (!IsTeen) return base.GetAttacks();
            int hit = ProficiencyBonus + 2;
            return new List<AttackOption>
            {
                new AttackOption { Name = "Bite", ToHit = hit, Damage = new DiceExpr(1, 6, 2), DamageType = DamageType.Piercing, ReachFt = 5 },
                new AttackOption { Name = "Claw", ToHit = hit, Damage = new DiceExpr(1, 4, 2), DamageType = DamageType.Slashing, ReachFt = 5 },
            };
        }

        public override List<AttackOption> AttackRoutine() => IsTeen ? GetAttacks() : base.AttackRoutine();

        /// <summary>Kindle (teenager): an ally's next weapon hit deals +1d6 fire per 4 party levels (at least 1d6).</summary>
        public int KindleDice => Math.Max(1, _partyLevel / 4);

        public Dragonling(int partyLevel) : base(MonstersDb.Get("dragonling"), false)
        {
            Team = Team.Players;
            SetPartyLevel(partyLevel);
            HP = MaxHP;
        }

        public static int MaxHPFor(int partyLevel) => 5 + 5 * Math.Max(1, partyLevel);
        public static int MaxHPFor(int partyLevel, DragonStage stage) => stage >= DragonStage.Wyrmling ? 10 + 6 * Math.Max(1, partyLevel) : MaxHPFor(partyLevel);

        /// <summary>Grows with the party. Damage already taken stays taken.</summary>
        public void SetPartyLevel(int partyLevel)
        {
            _partyLevel = Math.Max(1, partyLevel);
            int damage = MaxHP - HP;
            MaxHP = MaxHPFor(partyLevel, _stage);
            HP = Math.Max(0, MaxHP - Math.Max(0, damage));
        }

        /// <summary>Start-of-turn recharge roll: the breath comes back on a 5 or 6.</summary>
        public bool RollRecharge(Func<int> d6)
        {
            if (!BreathReady && d6() >= 5) BreathReady = true;
            return BreathReady;
        }

        protected override void OnDamaged(int hpBefore, int amount, bool critical, List<string> log)
        {
            if (HP > 0) return;
            HP = 0;
            log?.Add($"{Name} yelps and bolts for safety!");
        }
    }
}
