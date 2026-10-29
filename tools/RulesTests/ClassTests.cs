using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static PlayerCharacter AtLevel(string cls, int level, string species = "human")
        {
            var pc = Make(cls, species);
            pc.XP = 999999;
            while (pc.Level < level) pc.LevelUp(pc.AutoPlan(), null);
            return pc;
        }

        static Monster Dummy(int ac = 10, int hp = 200)
        {
            var m = new Monster(MonstersDb.Get("wolf"));
            m.MaxHP = m.HP = hp;
            m.Team = Team.Enemies;
            return m;
        }

        /// <summary>Rolls attacks until one hits, then rolls its damage.</summary>
        static (AttackRollResult res, DamageRoll dmg) HitWith(PlayerCharacter pc, Creature target, AttackContext ctx, TurnState turn)
        {
            for (int i = 0; i < 200; i++)
            {
                var res = AttackResolver.RollAttack(pc, target, pc.GetAttacks()[0], ctx, turn);
                if (res.Hit && !res.Critical) return (res, AttackResolver.RollDamage(res));
            }
            return (null, null);
        }

        static void RogueTests()
        {
            Dice.Rng = new SystemRandom(); // earlier tests leave scripted dice behind
            var r = Make("rogue");
            Eq(r.Class.HitDie, 8, "rogue d8");
            Check(r.MainHand != null && r.MainHand.Has(WeaponProperty.Finesse), "rogue starts with a finesse weapon");
            Eq(r.Expertise.Count, 2, "rogue starts with 2 expertise");
            Check(r.IsProficientWith(ItemsDb.Get<WeaponDef>("rapier")), "rogue proficient with rapier (martial finesse)");
            Check(!r.IsProficientWith(ItemsDb.Get<WeaponDef>("greataxe")), "rogue not proficient with greataxe");
            Eq(AttackResolver.SneakAttackDiceFor(r), 1, "sneak attack 1d6 at level 1");
            Eq(AttackResolver.SneakAttackDiceFor(AtLevel("rogue", 3)), 2, "sneak attack 2d6 at level 3");

            // Needs advantage, or an ally next to the target.
            var target = Dummy();
            var turn = new TurnState();
            var plain = HitWith(r, target, new AttackContext(), turn);
            Eq(plain.dmg.Dice.Count, 1, "no sneak attack without advantage or an ally");
            turn = new TurnState();
            var withAlly = HitWith(r, target, new AttackContext { AllyAdjacentToTarget = true }, turn);
            Eq(withAlly.dmg.Dice.Count, 2, "sneak attack with an ally adjacent adds a d6");
            Check(turn.SneakAttackUsed, "sneak attack marks the turn");
            var again = HitWith(r, target, new AttackContext { AllyAdjacentToTarget = true }, turn);
            Eq(again.dmg.Dice.Count, 1, "sneak attack only once per turn");
            turn = new TurnState();
            var unseen = HitWith(r, target, new AttackContext { TargetCanSeeAttacker = false }, turn);
            Eq(unseen.dmg.Dice.Count, 2, "sneak attack with advantage (unseen)");

            // Steady Aim (L3): advantage on the next attack.
            var r3 = AtLevel("rogue", 3);
            r3.Effects.Add(EffectLibrary.Create("steady_aim", r3));
            var probe = new AttackRollResult { Attacker = r3, Target = target, Attack = r3.GetAttacks()[0] };
            Eq(AttackResolver.ComputeAdvantage(r3, target, probe.Attack, new AttackContext(), probe), AdvantageState.Advantage, "steady aim grants advantage");
        }

        static void BarbarianTests()
        {
            Dice.Rng = new SystemRandom();
            var b = Make("barbarian");
            Eq(b.Class.HitDie, 12, "barbarian d12");
            Eq(b.ArmorClass, 10 + b.Mod(Ability.DEX) + b.Mod(Ability.CON), "unarmored defense = 10 + DEX + CON");
            Eq(b.ResourceMax("rage"), 2, "2 rages at level 1");
            Eq(AtLevel("barbarian", 3).ResourceMax("rage"), 3, "3 rages at level 3");

            int dmgBefore = b.GetAttacks()[0].DamageMod;
            Check(b.StartRage(null), "can rage");
            Check(b.HasEffect("rage"), "raging");
            Eq(b.GetAttacks()[0].DamageMod, dmgBefore + 2, "rage adds +2 to STR melee damage");
            b.MaxHP = b.HP = 100; // plenty of HP so damage isn't capped by dropping to 0
            int hp = b.HP;
            b.TakeDamage(10, DamageType.Slashing, false, null);
            Eq(hp - b.HP, 5, "rage halves slashing damage");
            hp = b.HP;
            b.TakeDamage(10, DamageType.Fire, false, null);
            Eq(hp - b.HP, 10, "rage doesn't resist fire");
            Check(b.AdvantageOnSave(Ability.STR, null), "rage: advantage on STR saves");
            Check(!b.Resistances.Contains(DamageType.Slashing), "rage resistance isn't left on permanently");

            Eq(b.ResourceLeft("rage"), 1, "one rage left");
            b.ShortRest(null);
            Eq(b.ResourceLeft("rage"), 2, "short rest regains a rage");
            b.StartRage(null); b.StartRage(null);
            b.LongRest(true, null);
            Eq(b.ResourceLeft("rage"), 2, "long rest regains all rages");

            // Reckless Attack (L2): advantage on your STR melee attacks, and attackers get it against you.
            var b2 = AtLevel("barbarian", 2);
            b2.Effects.Add(EffectLibrary.Create("reckless", b2));
            var wolf = Dummy();
            var probe = new AttackRollResult { Attacker = b2, Target = wolf, Attack = b2.GetAttacks()[0] };
            Eq(AttackResolver.ComputeAdvantage(b2, wolf, probe.Attack, new AttackContext(), probe), AdvantageState.Advantage, "reckless: advantage attacking");
            var back = new AttackRollResult { Attacker = wolf, Target = b2, Attack = wolf.GetAttacks()[0] };
            Eq(AttackResolver.ComputeAdvantage(wolf, b2, back.Attack, new AttackContext(), back), AdvantageState.Advantage, "reckless: enemies get advantage on you");
            Check(b2.AdvantageOnSave(Ability.DEX, null), "danger sense: advantage on DEX saves at 2");

            // Berserker Frenzy (L3): raging + reckless, the first hit each turn adds (rage damage)d6.
            var b3 = AtLevel("barbarian", 3);
            b3.StartRage(null);
            b3.Effects.Add(EffectLibrary.Create("reckless", b3));
            var turn = new TurnState();
            var first = HitWith(b3, Dummy(), new AttackContext(), turn);
            int weaponDice = b3.GetAttacks()[0].Damage.Count;
            Eq(first.dmg.Dice.Count, weaponDice + 2, "frenzy adds 2d6 to the first hit");
            var second = HitWith(b3, Dummy(), new AttackContext(), turn);
            Eq(second.dmg.Dice.Count, weaponDice, "frenzy only once per turn");
        }
    }
}
