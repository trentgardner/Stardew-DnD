using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Levels to `level` under the Act III cap; restores the previous cap afterwards.</summary>
        static PlayerCharacter AtLevelThirteen(string cls, int level = 13)
        {
            int old = PlayerCharacter.LevelCap;
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try { return AtLevel(cls, level); }
            finally { PlayerCharacter.LevelCap = old; }
        }


        static void LevelThirteenTests()
        {
            Dice.Rng = new SystemRandom();

            // LevelCapThirteenAndXpTable
            Eq(PlayerCharacter.XpThresholds.Length, 14, "XP table runs to 13");
            Eq(PlayerCharacter.XpThresholds[11], 85000, "level 11 at 85000 XP");
            Eq(PlayerCharacter.XpThresholds[12], 100000, "level 12 at 100000 XP");
            Eq(PlayerCharacter.XpThresholds[13], 120000, "level 13 at 120000 XP");
            Eq(PlayerCharacter.ActThreeLevelCap, 13, "Act III caps at 13");
            var f = Make("fighter");
            f.XP = 120000;
            PlayerCharacter.LevelCap = PlayerCharacter.ActTwoLevelCap;
            try
            {
                while (f.CanLevelUp) f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 10, "Act II: stops at 10 with 120000 XP");
                Check(!f.CanLevelUp, "no level 11 in Act II");
                // CapRiseToThirteenLevelsOneAtATime
                PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
                Check(f.CanLevelUp, "raising the cap to 13 lets a capped character level again");
                f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 11, "one level per LevelUp (10 → 11)");
                Check(f.PendingChoices().Any(c => c.Kind == ChoiceKind.Feat), "11 → 12 still asks for its ASI");
                f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 12, "12");
                f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 13, "13");
                Check(!f.CanLevelUp, "13 is the cap");
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }

            // AutoPlanAnswersEveryChoiceToThirteen
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                foreach (var cls in AllClasses)
                {
                    var pc = Make(cls);
                    pc.XP = 120000;
                    bool ok = true;
                    try
                    {
                        while (pc.CanLevelUp)
                        {
                            var choices = pc.PendingChoices();
                            var plan = pc.AutoPlan();
                            for (int i = 0; i < choices.Count; i++)
                                if (i >= plan.Picks.Count || plan.Picks[i].Count != System.Math.Min(choices[i].Count, choices[i].Options.Count)) ok = false;
                            pc.LevelUp(plan, null);
                        }
                    }
                    catch (System.Exception e) { ok = false; System.Console.WriteLine($"{cls}: {e}"); }
                    Check(ok && pc.Level == 13, cls + ": AutoPlan answers every choice to 13");
                }
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }

            // AsiAtTwelveForEveryClass
            foreach (var cls in AllClasses) Check(Make(cls).AsiLevel(12), cls + " gets an ASI at 12");

            // PreparedCountsAtThirteen
            Eq(CharacterDb.Classes["cleric"].PreparedSpells[13], 17, "cleric prepares 17 at 13");
            Eq(CharacterDb.Classes["wizard"].PreparedSpells[13], 17, "wizard prepares 17 at 13");
            Eq(CharacterDb.Classes["wizard"].PreparedSpells[11], 16, "wizard prepares 16 at 11");
            Eq(CharacterDb.Classes["sorcerer"].PreparedSpells[12], 16, "sorcerer prepares 16 at 12");
            Eq(CharacterDb.Classes["warlock"].PreparedSpells[13], 12, "warlock prepares 12 at 13");
            Eq(CharacterDb.Classes["warlock"].PreparedSpells[11], 11, "warlock prepares 11 at 11");
            Eq(CharacterDb.Classes["paladin"].PreparedSpells[13], 11, "paladin prepares 11 at 13");
            Eq(CharacterDb.Classes["ranger"].PreparedSpells[11], 10, "ranger prepares 10 at 11");

            var ctx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true };

            // StudiedAttacksGivesAdvantageAfterAMiss
            var f13 = AtLevelThirteen("fighter", 13);
            Check(f13.HasFeature("Studied Attacks"), "fighter 13: Studied Attacks");
            Eq(f13.ResourceMax("indomitable"), 2, "fighter 13: two Indomitable");
            var tough = Dummy(ac: 40);
            var other = Dummy(ac: 40);
            var turn = new TurnState(); turn.Reset(f13);
            Dice.Rng = new ScriptedRandom(2);
            var miss = AttackResolver.RollAttack(f13, tough, f13.GetAttacks()[0], ctx, turn);
            Check(!miss.Hit, "a 2 misses");
            var r = new AttackRollResult();
            Check(AttackResolver.ComputeAdvantage(f13, tough, f13.GetAttacks()[0], ctx, r) == AdvantageState.Advantage, "Studied Attacks: Advantage on the next attack against that creature");
            r = new AttackRollResult();
            Check(AttackResolver.ComputeAdvantage(f13, other, f13.GetAttacks()[0], ctx, r) == AdvantageState.Normal, "...but not against another creature");
            var f12 = AtLevelThirteen("fighter", 12);
            var t12 = Dummy(ac: 40);
            AttackResolver.RollAttack(f12, t12, f12.GetAttacks()[0], ctx, turn);
            r = new AttackRollResult();
            Check(AttackResolver.ComputeAdvantage(f12, t12, f12.GetAttacks()[0], ctx, r) == AdvantageState.Normal, "not before 13");
            Dice.Rng = new SystemRandom();

            // RelentlessRageDcRisesAndResetsOnRest
            var b11 = AtLevelThirteen("barbarian", 11);
            Check(b11.HasFeature("Relentless Rage"), "barbarian 11: Relentless Rage");
            b11.StartRage(null);
            Eq(b11.RelentlessRageDC, 10, "first DC 10");
            Dice.Rng = new ScriptedRandom(20);
            b11.TakeDamage(b11.HP + 5, DamageType.Fire, false, null);
            Eq(b11.HP, 22, "a success: HP becomes twice the barbarian level");
            Eq(b11.RelentlessRageDC, 15, "the DC rises by 5");
            Dice.Rng = new ScriptedRandom(1);
            b11.TakeDamage(b11.HP + 5, DamageType.Fire, false, null);
            Eq(b11.HP, 0, "a failure: down you go");
            Eq(b11.RelentlessRageDC, 20, "the DC rose again");
            b11.ShortRest(null);
            Eq(b11.RelentlessRageDC, 10, "a Short Rest resets the DC to 10");
            var b11b = AtLevelThirteen("barbarian", 11);
            Dice.Rng = new ScriptedRandom(20);
            b11b.TakeDamage(b11b.HP + 5, DamageType.Fire, false, null);
            Eq(b11b.HP, 0, "only while raging");
            Dice.Rng = new SystemRandom();

            // Improved Brutal Strike
            var b13 = AtLevelThirteen("barbarian", 13);
            Check(b13.BrutalStrikeOptions().SequenceEqual(new[] { 1, 2, 3, 4 }), "barbarian 13: Staggering and Sundering Blows join Forceful and Hamstring");
            Check(AtLevelThirteen("barbarian", 12).BrutalStrikeOptions().SequenceEqual(new[] { 1, 2 }), "barbarian 12: Forceful and Hamstring only");
            var staggered = Dummy();
            staggered.AddEffect(EffectLibrary.Create("staggering_blow", b13));
            Dice.Rng = new ScriptedRandom(18, 3);
            var sv = Checks.RollSave(staggered, Ability.CON, AdvantageState.Normal);
            Eq(sv.Natural, 3, "Staggering Blow: Disadvantage on its next save");
            Check(!staggered.HasEffect("staggering_blow"), "...and only the next one");
            var sundered = Dummy(ac: 10);
            sundered.AddEffect(EffectLibrary.Create("sundered", b13));
            Dice.Rng = new ScriptedRandom(10);
            var ally = AtLevelThirteen("fighter", 11);
            var sres = AttackResolver.RollAttack(ally, sundered, ally.GetAttacks()[0], ctx, turn);
            Check(sres.Roll.Terms.Any(t => t.Label == "sundering blow" && t.Value == 5), "Sundering Blow: +5 to the next attack by someone else");
            Check(!sundered.HasEffect("sundered"), "...used up");
            Dice.Rng = new SystemRandom();

            // ImprovedCunningStrikeAllowsTwoEffects
            var r11 = AtLevelThirteen("rogue", 11);
            Eq(AttackResolver.SneakAttackDiceFor(r11), 6, "rogue 11: 6d6");
            Eq(AttackResolver.SneakAttackDiceFor(AtLevelThirteen("rogue", 13)), 7, "rogue 13: 7d6");
            Eq(r11.CunningStrikesPerSneak, 2, "Improved Cunning Strike: two effects");
            Eq(AtLevelThirteen("rogue", 10).CunningStrikesPerSneak, 1, "one before 11");
            var c1 = EffectLibrary.Create("cunning_ready", r11); c1.Power = 1;
            var c2 = EffectLibrary.Create("cunning_ready2", r11); c2.Power = 2;
            r11.AddEffect(c1); r11.AddEffect(c2);
            var allyCtx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true, AllyAdjacentToTarget = true };
            var rt = new TurnState(); rt.Reset(r11);
            var (rh, rd) = HitWith(r11, Dummy(ac: 1), allyCtx, rt);
            Check(rh.CunningStrike == 1 && rh.CunningStrike2 == 2, "both riders ride the sneak attack");
            int weaponDice = r11.GetAttacks()[0].Damage.Count;
            Eq(rd.Dice.Count - weaponDice, 4, "...costing two of the six dice (6d6 → 4d6)");

            // Use Magic Device (flavour: there are no charged items yet)
            Check(AtLevelThirteen("rogue", 13).HasFeature("Use Magic Device"), "thief 13: Use Magic Device");

            // Monk 11 and 13
            Check(AtLevelThirteen("monk", 11).HasFeature("Fleet Step"), "monk 11: Fleet Step");
            Eq(AtLevelThirteen("monk", 11).MartialArtsDie, 10, "monk 11: d10");
            // DeflectEnergyWorksOnFire
            Check(AtLevelThirteen("monk", 13).CanDeflect(DamageType.Fire), "monk 13: Deflect Energy turns aside fire");
            Check(!AtLevelThirteen("monk", 12).CanDeflect(DamageType.Fire), "monk 12: not fire");
            Check(AtLevelThirteen("monk", 12).CanDeflect(DamageType.Slashing), "monk 12: slashing yes");
            Check(!AtLevelThirteen("fighter", 13).CanDeflect(DamageType.Slashing), "fighters don't deflect");

            // RadiantStrikesAddsD8OnMeleeHitOnly
            var p11 = AtLevelThirteen("paladin", 11);
            Check(p11.HasFeature("Radiant Strikes"), "paladin 11: Radiant Strikes");
            var pt = new TurnState(); pt.Reset(p11);
            var (ph, pd) = HitWith(p11, Dummy(ac: 1), ctx, pt);
            Check(pd.Terms.Any(t => t.Label == "radiant strikes") && pd.Extra != null, "a melee hit adds 1d8 radiant");
            var bow = ItemsDb.Get<WeaponDef>("longbow");
            var ranged = p11.WeaponAttack(bow);
            AttackRollResult rr2 = null;
            for (int i = 0; i < 200 && rr2 == null; i++) { var a = AttackResolver.RollAttack(p11, Dummy(ac: 1), ranged, new AttackContext { DistanceFt = 30, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true }, pt); if (a.Hit) rr2 = a; }
            Check(rr2 != null && !AttackResolver.RollDamage(rr2).Terms.Any(t => t.Label == "radiant strikes"), "...a ranged hit doesn't");
            Check(AtLevelThirteen("paladin", 13).KnowsSpell("freedom_of_movement") && AtLevelThirteen("paladin", 13).KnowsSpell("guardian_of_faith"), "Devotion 13: Freedom of Movement and Guardian of Faith");

            // SuperiorHuntersPreySpillsOncePerTurn
            var rg = AtLevelThirteen("ranger", 11);
            Check(rg.HasFeature("Superior Hunter's Prey"), "hunter 11: Superior Hunter's Prey");
            var marked = Dummy(ac: 1);
            marked.AddEffect(EffectLibrary.Create("hunters_mark", rg));
            var gt = new TurnState(); gt.Reset(rg);
            var (gh1, _) = HitWith(rg, marked, ctx, gt);
            Check(gh1.HuntersMarkSpill > 0, "the mark's damage spills to a second creature");
            var (gh2, _) = HitWith(rg, marked, ctx, gt);
            Eq(gh2.HuntersMarkSpill, 0, "once per turn");
            // Relentless Hunter
            var rg13 = AtLevelThirteen("ranger", 13);
            rg13.ConcentratingOn = "hunters_mark";
            Check(rg13.ConcentrationImmuneToDamage, "ranger 13: damage can't break Hunter's Mark");
            rg13.ConcentratingOn = "fog_cloud";
            Check(!rg13.ConcentrationImmuneToDamage, "...only Hunter's Mark");
            Dice.Rng = new SystemRandom();
        }
    }
}
