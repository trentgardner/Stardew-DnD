using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void SpellTests5()
        {
            Dice.Rng = new SystemRandom();
            var wiz = AtLevelTen("wizard", 9);
            var cleric = AtLevelTen("cleric", 9);

            // FireballHitsEveryoneInRadiusHalfOnSave (radius is the game's job; the data and the halving are ours)
            var fb = SpellsDb.Get("fireball");
            Check(fb != null && fb.Level == 3 && fb.Shape == TargetShape.Sphere && fb.AreaFt == 20 && fb.SaveAbility == Ability.DEX && fb.HalfOnSave, "fireball: L3, 20-ft sphere, DEX half");
            Eq(fb?.Dice.ToString(), "8d6", "fireball: 8d6");
            var wolf = Dummy(hp: 500);
            var fixedRoll = new DamageRoll { Expr = DiceExpr.Parse("8d6") };
            fixedRoll.Terms.Add(new RollTerm("test", 40));
            int saved = SpellResolver.ApplySaveDamage(wiz, wolf, fb, fixedRoll, true, new List<string>());
            Eq(saved, 20, "fireball: a successful save halves it");

            // UpcastScalesOnlyWhenDefined
            Eq(SpellResolver.EffectDice(wiz, fb, 5).ToString(), "10d6", "fireball in a 5th-level slot: 10d6");
            Eq(SpellResolver.EffectDice(wiz, SpellsDb.Get("cone_of_cold"), 5).ToString(), "8d8", "cone of cold at its own level: 8d8");
            Eq(SpellResolver.EffectDice(wiz, SpellsDb.Get("ice_storm"), 5).ToString(), "3d10", "ice storm upcast once: 3d10");
            Check(SpellsDb.Get("haste").Dice.IsNone && SpellsDb.Get("haste").UpcastDice == 0, "haste doesn't scale");

            // SecondDamageTypeAppliesSeparately: flame strike vs a fire-immune creature only lands the radiant half
            var fs = SpellsDb.Get("flame_strike");
            Check(fs != null && fs.DamageType == DamageType.Fire && fs.SecondDamageType == DamageType.Radiant && fs.SecondDice.ToString() == "5d6", "flame strike: 5d6 fire + 5d6 radiant");
            var immune = Dummy(hp: 500); immune.Immunities.Add(DamageType.Fire);
            var fire = new DamageRoll { Expr = DiceExpr.Parse("5d6") }; fire.Terms.Add(new RollTerm("t", 20));
            var rad = new DamageRoll { Expr = DiceExpr.Parse("5d6") }; rad.Terms.Add(new RollTerm("t", 15));
            int dealt = SpellResolver.ApplySaveDamage(cleric, immune, fs, fire, false, new List<string>(), rad);
            Eq(dealt, 15, "fire immunity: only the radiant 15 lands");

            // SpiritGuardiansHurtsOnEnterAndHalvesSpeed
            var sg = SpellsDb.Get("spirit_guardians");
            Check(sg != null && sg.Concentration && sg.ZoneRadiusTiles == 3, "spirit guardians: 15-ft concentration aura");
            var zone = ZoneRules.Create(cleric, sg, 3, 0, 0);
            Check(zone.FollowsOwner && ZoneRules.Contains(zone, 3, 0, 0, 0) && !ZoneRules.Contains(zone, 4, 0, 0, 0), "the aura reaches 3 tiles");
            var victim = Dummy(hp: 500);
            int speedBefore = victim.Speed;
            int hurt = ZoneRules.Trigger(zone, victim, new List<string>());
            Check(hurt > 0, "an enemy entering the aura is hurt");
            Eq(victim.Speed, speedBefore / 2, "and its Speed is halved");
            Eq(ZoneRules.Trigger(zone, victim, new List<string>()), 0, "only once per turn");
            ZoneRules.NewTurn(zone);
            Check(ZoneRules.Trigger(zone, victim, new List<string>()) > 0, "again on a later turn");
            var friend = Make("fighter");
            Eq(ZoneRules.Trigger(zone, friend, new List<string>()), 0, "allies are never hurt by the guardians");

            // GuardianOfFaithStopsAfterSixty
            var gof = ZoneRules.Create(cleric, SpellsDb.Get("guardian_of_faith"), 4, 0, 0);
            int total = 0;
            for (int i = 0; i < 10; i++) { var t = Dummy(hp: 500); total += ZoneRules.Trigger(gof, t, new List<string>()); ZoneRules.NewTurn(gof); }
            Check(total <= 60 && gof.Spent, "the guardian vanishes after dealing 60 damage");

            // DeathWardStopsTheFirstDrop
            var f = Make("fighter");
            f.AddEffect(EffectLibrary.Create("death_ward", cleric));
            f.TakeDamage(f.HP + 5, DamageType.Slashing, false, new List<string>());
            Eq(f.HP, 1, "death ward: drop to 1 HP instead");
            Check(!f.HasEffect("death_ward"), "death ward is used up");
            f.TakeDamage(5, DamageType.Slashing, false, new List<string>());
            Check(f.IsDown, "the second drop is real");

            // HasteEndingCausesLethargy
            var enc = new Encounter();
            var hasted = Make("fighter");
            enc.Add(wiz); enc.Add(hasted);
            int ac = hasted.ArmorClass, spd = hasted.Speed;
            hasted.AddEffect(EffectLibrary.Create("haste", wiz));
            wiz.ConcentratingOn = "haste";
            Eq(hasted.ArmorClass, ac + 2, "haste: +2 AC");
            Eq(hasted.Speed, spd * 2, "haste: Speed doubled");
            enc.EndConcentration(wiz);
            Check(!hasted.HasEffect("haste") && hasted.HasEffect("lethargic") && hasted.IsIncapacitated, "haste ends → lethargic (can't act)");

            // BanishedCreatureLeavesAndReturns
            var enc2 = new Encounter();
            var foe = Dummy(hp: 100);
            enc2.Add(wiz); enc2.Add(foe);
            foe.AddEffect(EffectLibrary.Create("banished", wiz, wiz.SpellSaveDC));
            Check(foe.IsBanished && foe.IsIncapacitated, "banished: gone from the fight");
            enc2.EndConcentration(wiz);
            Check(!foe.IsBanished && !foe.IsDead, "concentration breaks → it returns");
            var enc3 = new Encounter();
            var foe2 = Dummy(hp: 100);
            enc3.Add(wiz); enc3.Add(foe2);
            enc3.CurrentIndex = enc3.Order.FindIndex(c => c.Creature == wiz);
            foe2.AddEffect(EffectLibrary.Create("banished", wiz, wiz.SpellSaveDC));
            for (int i = 0; i < 10; i++) enc3.StartTurn(new List<string>());
            Check(foe2.IsDead, "banished for the full minute: it doesn't come back");

            // SmitesAddDiceOnlyOnHit (dice and riders; 'on hit' is the combat layer's)
            Eq(SpellResolver.SmiteDice("blinding_smite", 3).ToString(), "3d8", "blinding smite: 3d8");
            Eq(SpellResolver.SmiteDice("staggering_smite", 4).ToString(), "4d6", "staggering smite: 4d6");
            Eq(SpellResolver.SmiteDice("banishing_smite", 5).ToString(), "5d10", "banishing smite: 5d10");
            Eq(SpellResolver.SmiteDice("blinding_smite", 4).ToString(), "4d8", "blinding smite upcast: +1d8");

            // HypnoticEndsOnDamage
            var dazed = Dummy(hp: 100);
            dazed.AddEffect(EffectLibrary.Create("hypnotized", wiz, 15));
            Check(dazed.IsIncapacitated, "hypnotic pattern incapacitates");
            dazed.TakeDamage(3, DamageType.Slashing, false, null);
            Check(!dazed.HasEffect("hypnotized"), "taking damage snaps it out");

            // BeaconMaximizesHealing
            var ally = Make("fighter");
            ally.AddEffect(EffectLibrary.Create("beacon_of_hope", cleric));
            var heal = new DamageRoll { Expr = DiceExpr.Parse("2d4") };
            heal.Terms.Add(new RollTerm("WIS", 4));
            Eq(SpellResolver.HealTotal(heal, ally), 12, "beacon of hope: healing dice count as max (8 + 4)");
            Check(ally.AdvantageOnSave(Ability.WIS, null), "beacon of hope: advantage on WIS saves");

            // SpellListsAreCurated
            PlayerCharacter.LevelCap = 10;
            foreach (var cls in AllClasses)
            {
                var pc = Make(cls);
                if (!pc.IsCaster) continue;
                for (int lv = 1; lv <= pc.MaxSpellLevelAt(10); lv++)
                {
                    int n = SpellsDb.ForClass(cls, lv).Count(s => s.Level == lv);
                    Check(n >= 2, $"{cls} has at least 2 level-{lv} spells (has {n})");
                }
            }
            var w4 = AtLevel("warlock", 4);
            var wspells = w4.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Spell);
            Check(wspells != null && wspells.Options.Any(id => SpellsDb.Get(id).Level == 3), "warlock 5 is offered 3rd-level spells");
            PlayerCharacter.LevelCap = 4;
        }
    }
}
