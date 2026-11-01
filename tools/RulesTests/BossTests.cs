using System.Collections.Generic;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static MonsterDef TestBoss() => new MonsterDef
        {
            Id = "test_boss", Name = "Test Boss", AC = 18, HitDice = DiceExpr.Parse("10d10+50"), CR = 10, XP = 5900,
            Attacks = { new MonsterAttackDef { Name = "Rend", ToHit = 10, Damage = DiceExpr.Parse("2d6+6"), DamageType = DamageType.Slashing, ReachFt = 10 } },
            Abilities = { new MonsterAbilityDef { Name = "Breath", Telegraph = true, RechargeMin = 5, Dice = DiceExpr.Parse("10d6"), DamageType = DamageType.Fire, SaveDC = 17, AreaFt = 30 } },
            LegendaryPerRound = 2,
            LegendaryActions = { new LegendaryActionDef { Name = "Tail Sweep", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Rend", RangeFt = 15 } },
            PhaseTwoAt = 0.5, PhaseTwoText = "Phase two!", PhaseTwoRechargeMin = 4, LairEruptions = 3,
        };

        /// <summary>Act II Phase 6: the boss engine (legendary actions, the breath it draws, phase two).</summary>
        static void BossTests()
        {
            var m = new Monster(TestBoss(), rollHp: false);
            Eq(m.LegendaryLeft, 2, "boss: starts with its legendary actions");
            Check(m.SpendLegendary(1), "boss: spend one");
            Check(m.SpendLegendary(1), "boss: spend another");
            Check(!m.SpendLegendary(1), "boss: none left this round");
            Eq(m.LegendaryLeft, 0, "boss: legendary budget empty");
            m.ResetLegendary();
            Eq(m.LegendaryLeft, 2, "boss: its own turn resets them");
            Check(!new Monster(MonstersDb.Get("ogre"), false).SpendLegendary(1), "boss: an ordinary monster has none");

            Check(m.CheckPhaseTwo() == null, "phase two: not at full HP");
            m.HP = (int)(m.MaxHP * 0.49);
            Eq(m.CheckPhaseTwo(), "Phase two!", "phase two: at under half HP, the text, once");
            Check(m.PhaseTwo, "phase two: flag set");
            Check(m.CheckPhaseTwo() == null, "phase two: only once");
            Eq(m.RechargeMinFor(m.Def.Abilities[0]), 4, "phase two: the breath recharges faster");
            Check(new Dragonling(5).Immunities.Contains(DamageType.Fire), "the party's red dragon is immune to fire");
            Eq(new Monster(TestBoss(), false).RechargeMinFor(TestBoss().Abilities[0]), 5, "phase one: the normal recharge");
        }
    }
}
