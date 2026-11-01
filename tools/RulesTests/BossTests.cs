using System.Linq;
using System;
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

            WardenIsABossNotAWall();
            ActTwoBalanceTable();
        }

        /// <summary>
        /// Act II's set-piece fights at the level a party should be when it meets them: the median 4-class party needs more
        /// than a round and no more than 8 rounds of at-will damage (talking them down pays the same, but fighting must be fair).
        /// </summary>
        static void ActTwoBalanceTable()
        {
            var fights = new (string name, int level, (string id, int n)[] members)[]
            {
                ("Lantern Road boars", 5, new[] { ("ember_eyed_boar", 3) }),
                ("Restricted Section guardians", 6, new[] { ("animated_armor", 2), ("rug_of_smothering", 1) }),
                ("Cragfoot wolves", 6, new[] { ("ember_eyed_wolf", 3) }),
                ("The beacon's Ashsworn", 7, new[] { ("ashsworn_zealot", 4), ("ashsworn_keeper", 1) }),
                ("The circle's bears", 7, new[] { ("cinder_bear", 3) }),
                ("The Frost guardians", 7, new[] { ("thorn_guardian", 2) }),
                ("The Ashmaw", 7, new[] { ("ashmaw", 1) }),
                ("The Grey Vigil", 8, new[] { ("ashsworn_zealot", 3), ("ashsworn_keeper", 1) }),
                ("Throat salamanders", 9, new[] { ("salamander", 2) }),
                ("Throat elemental", 9, new[] { ("fire_elemental", 1) }),
            };
            string[] pool = { "fighter", "rogue", "cleric", "wizard", "ranger", "paladin", "bard", "druid" };
            PlayerCharacter.LevelCap = PlayerCharacter.ActTwoLevelCap;
            foreach (var f in fights)
            {
                double hp = 0, acSum = 0; int count = 0;
                foreach (var (id, n) in f.members) { var m = MonstersDb.Get(id); hp += m.HitDice.Average * n; acSum += m.AC * n; count += n; }
                int ac = (int)Math.Round(acSum / count);
                var dpr = pool.ToDictionary(c => c, c => DamagePerRound(AtLevel(c, f.level), ac));
                var rounds = new List<double>();
                for (int a = 0; a < pool.Length; a++) for (int b = a + 1; b < pool.Length; b++) for (int c = b + 1; c < pool.Length; c++) for (int d = c + 1; d < pool.Length; d++)
                    rounds.Add(hp / (dpr[pool[a]] + dpr[pool[b]] + dpr[pool[c]] + dpr[pool[d]]));
                rounds.Sort();
                double median = rounds[rounds.Count / 2];
                Console.WriteLine($"  {f.name,-30} L{f.level}: {hp,5:0} HP, AC {ac}: median {median:0.0} rounds");
                Check(median >= 1.0 && median <= 8.0, $"Act II balance: {f.name} takes {median:0.0} rounds at level {f.level}");
            }
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        /// <summary>
        /// The Warden must be beatable by a typical level-9 party of four (two players, two companions; the dragon not counted) in
        /// 4-9 rounds of at-will damage, and hit hard enough to feel like a boss.
        /// </summary>
        static void WardenIsABossNotAWall()
        {
            var w = MonstersDb.Get("warden");
            string[] pool = { "fighter", "rogue", "cleric", "wizard", "ranger", "paladin", "bard", "druid" };
            PlayerCharacter.LevelCap = PlayerCharacter.ActTwoLevelCap;
            var dpr = new System.Collections.Generic.Dictionary<string, double>();
            foreach (var c in pool) dpr[c] = DamagePerRound(AtLevel(c, 9), w.AC);
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
            var rounds = new List<double>();
            for (int a = 0; a < pool.Length; a++)
                for (int b = a + 1; b < pool.Length; b++)
                    for (int c = b + 1; c < pool.Length; c++)
                        for (int d = c + 1; d < pool.Length; d++)
                            rounds.Add(w.HitDice.Average / (dpr[pool[a]] + dpr[pool[b]] + dpr[pool[c]] + dpr[pool[d]]));
            rounds.Sort();
            double median = rounds[rounds.Count / 2], slowest = rounds[rounds.Count - 1];
            Console.WriteLine($"The Warden ({w.HitDice.Average:0} HP, AC {w.AC}) vs level-9 parties of four: median {median:0.0} rounds, slowest {slowest:0.0}");
            Check(median >= 4 && median <= 9, $"Warden: a typical party needs {median:0.0} rounds (want 4-9)");
            Check(slowest <= 12, $"Warden: the slowest party needs {slowest:0.0} rounds (want <= 12)");
            // Its own damage: three Rends and one Tail Sweep a round against AC 17, including crits.
            double Expected(int toHit, double avg, int ac) { double p = Math.Min(0.95, Math.Max(0.05, (21 - (ac - toHit)) / 20.0)); return p * avg + 0.05 * (avg - 6) ; }
            double rend = Expected(10, 13, 17), tail = Expected(10, 15, 17);
            double warden = 3 * rend + tail;
            Console.WriteLine($"The Warden's damage per round vs AC 17: {warden:0.0}");
            Check(warden >= 25, $"Warden: {warden:0.0} damage a round is too gentle for a boss");
        }
    }
}
