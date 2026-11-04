using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Known-bug fixes (2026-11-03): Savage Attacker's reroll, upcasting Scorching Ray / Spiritual Weapon / Aid.</summary>
        static void KnownBugTests()
        {
            Dice.Rng = new SystemRandom();
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                // SavageRerollOnlyRerollsWeaponDice: it must not re-run the once-per-turn riders (Hunter's Prey, Sneak Attack...).
                var rg = AtLevelThirteen("ranger", 11);
                var marked = Dummy(ac: 1);
                marked.AddEffect(EffectLibrary.Create("hunters_mark", rg));
                var turn = new TurnState(); turn.Reset(rg);
                var ctx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true };
                var (res, dmg) = HitWith(rg, marked, ctx, turn);
                Check(res != null && res.HuntersMarkSpill > 0 && turn.SuperiorPreyUsed, "setup: the mark spilled once");
                int spill = res.HuntersMarkSpill, extra = dmg.Extra?.Total ?? 0, terms = dmg.Terms.Count;
                Dice.Rng = new ScriptedRandom(Enumerable.Repeat(12, 40).ToArray()); // the reroll rolls high
                bool better = AttackResolver.SavageReroll(dmg, res);
                Dice.Rng = new SystemRandom();
                Check(better, "a high reroll is kept");
                Eq(res.HuntersMarkSpill, spill, "the spill is untouched");
                Check(turn.SuperiorPreyUsed, "and still counted as used once");
                Eq(dmg.Extra?.Total ?? 0, extra, "the mark's own dice aren't rerolled");
                Eq(dmg.Terms.Count, terms, "no riders added twice");
                // A low reroll is ignored.
                var (res2, dmg2) = HitWith(rg, Dummy(ac: 1), ctx, new TurnState());
                int before = dmg2.Total;
                Dice.Rng = new ScriptedRandom(Enumerable.Repeat(1, 40).ToArray());
                AttackResolver.SavageReroll(dmg2, res2);
                Dice.Rng = new SystemRandom();
                Check(dmg2.Total >= before, "a lower reroll never lowers the damage");

                // UpcastScorchingRaySpiritualWeaponAid
                var wiz = AtLevelThirteen("wizard", 9);
                var ray = SpellsDb.Get("scorching_ray");
                Eq(SpellResolver.Projectiles(wiz, ray, 2), 3, "Scorching Ray: 3 rays at 2nd level");
                Eq(SpellResolver.Projectiles(wiz, ray, 4), 5, "...and one more per slot level above 2nd");
                Eq(SpellResolver.Projectiles(wiz, SpellsDb.Get("eldritch_blast"), 0), SpellResolver.Projectiles(wiz, SpellsDb.Get("eldritch_blast")), "Eldritch Blast unchanged");
                var cleric = AtLevelThirteen("cleric", 9);
                Eq(SpellResolver.EffectDice(cleric, SpellsDb.Get("spiritual_weapon"), 2).Count, 1, "Spiritual Weapon: 1d8 at 2nd");
                Eq(SpellResolver.EffectDice(cleric, SpellsDb.Get("spiritual_weapon"), 4).Count, 3, "...+1d8 per slot level above 2nd");
                Eq(SpellResolver.AidBonus(2), 5, "Aid: +5 HP at 2nd level");
                Eq(SpellResolver.AidBonus(4), 15, "...+5 more per slot level above 2nd");

                // ActionSurgeKeepsLeftoverAttacks: a new Attack action adds its attacks to what's left of the last one.
                var ts = new TurnState();
                ts.StartAttackAction(3);
                Eq(ts.AttacksLeftInAction, 2, "attack 1 of 3: two left");
                Check(ts.ActionUsed, "the action is spent");
                ts.ActionUsed = false; // Action Surge
                ts.StartAttackAction(3);
                Eq(ts.AttacksLeftInAction, 4, "after Action Surge, the two left over are still there, plus two more");
                var spell = new TurnState();
                spell.StartAttackAction(1);
                Eq(spell.AttacksLeftInAction, 0, "a one-attack action leaves none");
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; Dice.Rng = new SystemRandom(); }
        }
    }
}
