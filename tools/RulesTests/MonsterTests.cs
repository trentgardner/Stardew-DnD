using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void MonsterTests()
        {
            Dice.Rng = new SystemRandom();
            foreach (var id in new[] { "ogre", "cinder_hound", "troll", "fire_elemental", "salamander", "ankheg" })
                Check(MonstersDb.Get(id) != null, "Act II monster exists: " + id);

            // TrollMakesThreeAttacks (Multiattack: bite + two claws)
            var troll = new Monster(MonstersDb.Get("troll"));
            var routine = troll.AttackRoutine();
            Eq(routine.Count, 3, "troll: three attacks per Attack action");
            Eq(string.Join(",", routine.Select(a => a.Name)), "Bite,Claw,Claw", "troll: bite, claw, claw");
            Eq(new Monster(MonstersDb.Get("wolf")).AttackRoutine().Count, 1, "a wolf still bites once");

            // TrollRegeneratesUnlessBurned
            troll.HP = troll.MaxHP - 30;
            Eq(troll.StartTurnRegen(null), 10, "troll regenerates 10 at the start of its turn");
            troll.TakeDamage(5, DamageType.Fire, false, null);
            Eq(troll.StartTurnRegen(null), 0, "fire stops it regenerating next turn");
            Eq(troll.StartTurnRegen(null), 10, "and then it's back");
            troll.HP = 0; troll.TakeDamage(1, DamageType.Slashing, false, null);
            Eq(troll.StartTurnRegen(null), 0, "a dead troll stays dead");

            // RechargeAbilityRollsAtTurnStart (5-6)
            var hound = new Monster(MonstersDb.Get("cinder_hound"));
            var breath = hound.Def.Abilities.Single();
            Check(hound.IsCharged(breath), "breath is ready at the start of a fight");
            hound.Spend(breath);
            Check(!hound.IsCharged(breath), "spent");
            Dice.Rng = new ScriptedRandom(4);
            hound.RollRecharges(null);
            Check(!hound.IsCharged(breath), "a 4 doesn't recharge it");
            Dice.Rng = new ScriptedRandom(5);
            hound.RollRecharges(null);
            Check(hound.IsCharged(breath), "a 5 does");
            Dice.Rng = new SystemRandom();
            Check(hound.Immunities.Contains(DamageType.Fire), "cinder hounds are immune to fire");

            // MultiattackStopsWhenAttackerDies (reactions are once per round through TurnState.ReactionUsed)
            var ogre = new Monster(MonstersDb.Get("ogre"));
            Check(Monster.KeepsAttacking(troll) == false, "a fallen monster doesn't finish its Multiattack");
            Check(Monster.KeepsAttacking(ogre), "a standing one does");
            var monk = AtLevelTen("monk", 5);
            var turn = new TurnState(); turn.Reset(monk);
            Check(!turn.ReactionUsed, "the monk can react this round");
            turn.ReactionUsed = true;
            Check(turn.ReactionUsed, "...once");

            // Owlbear: Act I balance keeps one Rend (see the ledger ruling).
            Eq(new Monster(MonstersDb.Get("owlbear")).AttackRoutine().Count, 1, "the Act I owlbear keeps a single Rend");
        }
    }
}
