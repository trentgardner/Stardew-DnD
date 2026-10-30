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
        public DragonStage Stage = DragonStage.Hatchling;
        public bool CanFly => Stage >= DragonStage.Young;
        public bool BreathReady = true;
        public int BreathDC => 8 + ProficiencyBonus + Mod(Ability.CON);
        public DiceExpr BreathDamage => new DiceExpr(1, 6);
        public int BreathConeTiles => 1;

        public Dragonling(int partyLevel) : base(MonstersDb.Get("dragonling"), false)
        {
            Team = Team.Players;
            SetPartyLevel(partyLevel);
            HP = MaxHP;
        }

        public static int MaxHPFor(int partyLevel) => 5 + 5 * Math.Max(1, partyLevel);

        /// <summary>Grows with the party. Damage already taken stays taken.</summary>
        public void SetPartyLevel(int partyLevel)
        {
            int damage = MaxHP - HP;
            MaxHP = MaxHPFor(partyLevel);
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
