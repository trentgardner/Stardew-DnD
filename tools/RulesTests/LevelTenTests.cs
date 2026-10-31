using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {

        /// <summary>Levels to `level` under the Act II cap; restores the Act I cap afterwards.</summary>
        static PlayerCharacter AtLevelTen(string cls, int level = 10)
        {
            int old = PlayerCharacter.LevelCap;
            PlayerCharacter.LevelCap = PlayerCharacter.ActTwoLevelCap;
            try { return AtLevel(cls, level); }
            finally { PlayerCharacter.LevelCap = old; }
        }

        static void LevelTenTests()
        {
            // LevelCapIsFourUntilRaised
            Eq(PlayerCharacter.LevelCap, 4, "the cap is 4 by default (Act I)");
            var f = Make("fighter");
            f.XP = 64000;
            while (f.CanLevelUp) f.LevelUp(f.AutoPlan(), null);
            Eq(f.Level, 4, "Act I: stops at 4 with 64000 XP");
            PlayerCharacter.LevelCap = PlayerCharacter.ActTwoLevelCap;
            try
            {
                // CapRiseLevelsOneAtATime
                Check(f.CanLevelUp, "raising the cap lets a capped character level again");
                f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 5, "one level per LevelUp");
                while (f.CanLevelUp) f.LevelUp(f.AutoPlan(), null);
                Eq(f.Level, 10, "Act II: up to 10");
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }

            // AsiAtFourEightAndClassExtras
            int[] AsiLevels(string cls)
            {
                var pc = Make(cls);
                return Enumerable.Range(2, 9).Where(l => pc.AsiLevel(l)).ToArray();
            }
            Eq(string.Join(",", AsiLevels("fighter")), "4,6,8", "fighter ASIs at 4, 6, 8");
            Eq(string.Join(",", AsiLevels("rogue")), "4,8,10", "rogue ASIs at 4, 8, 10");
            Eq(string.Join(",", AsiLevels("wizard")), "4,8", "wizard ASIs at 4, 8");
            var r9 = AtLevelTen("rogue", 9);
            PlayerCharacter.LevelCap = 10;
            Check(r9.PendingChoices().Any(c => c.Kind == ChoiceKind.Feat), "rogue 9→10 offers a feat");
            PlayerCharacter.LevelCap = 4;

            // ExtraAttackAtFiveForMartials
            foreach (var cls in new[] { "fighter", "barbarian", "monk", "paladin", "ranger" })
            {
                Eq(AtLevelTen(cls, 4).AttacksPerAction, 1, cls + " 4: one attack");
                Eq(AtLevelTen(cls, 5).AttacksPerAction, 2, cls + " 5: Extra Attack");
            }
            foreach (var cls in new[] { "rogue", "wizard", "cleric" })
                Eq(AtLevelTen(cls, 10).AttacksPerAction, 1, cls + " never gets Extra Attack");

            // MaxSpellLevelFollowsTables
            var wiz = Make("wizard");
            Eq(wiz.MaxSpellLevelAt(5), 3, "wizard 5: 3rd-level spells");
            Eq(wiz.MaxSpellLevelAt(7), 4, "wizard 7: 4th-level spells");
            Eq(wiz.MaxSpellLevelAt(9), 5, "wizard 9: 5th-level spells");
            var pal = Make("paladin");
            Eq(pal.MaxSpellLevelAt(5), 2, "paladin 5: 2nd-level spells");
            Eq(pal.MaxSpellLevelAt(9), 3, "paladin 9: 3rd-level spells");
            var wl = Make("warlock");
            Eq(wl.MaxSpellLevelAt(5), 3, "warlock 5: 3rd-level pact slots");
            Eq(wl.MaxSpellLevelAt(7), 4, "warlock 7: 4th-level pact slots");
            Eq(wl.MaxSpellLevelAt(9), 5, "warlock 9: 5th-level pact slots");
            Eq(AtLevelTen("wizard", 9).SlotsMax(5), 1, "wizard 9 has a 5th-level slot");

            // SpellTablesToTen (SRD 5.2 class tables)
            Eq(CharacterDb.Classes["wizard"].PreparedSpells[10], 15, "wizard prepares 15 at 10");
            Eq(CharacterDb.Classes["wizard"].CantripsKnown[10], 5, "wizard knows 5 cantrips at 10");
            Eq(CharacterDb.Classes["sorcerer"].CantripsKnown[10], 6, "sorcerer knows 6 cantrips at 10");
            Eq(CharacterDb.Classes["warlock"].PreparedSpells[10], 10, "warlock knows 10 spells at 10");
            Eq(CharacterDb.Classes["bard"].PreparedSpells[5], 9, "bard prepares 9 at 5");
            Eq(Make("warlock").InvocationsKnownAt(7), 6, "warlock 7: 6 invocations");
            Eq(Make("warlock").InvocationsKnownAt(9), 7, "warlock 9: 7 invocations");

            // Expertise / secrets / extra choices in 5-10
            PlayerCharacter.LevelCap = 10;
            bool Asks(string cls, int from, ChoiceKind k) => AtLevel(cls, from).PendingChoices().Any(c => c.Kind == k);
            Check(Asks("rogue", 5, ChoiceKind.Expertise), "rogue 6: Expertise");
            Check(Asks("bard", 8, ChoiceKind.Expertise), "bard 9: Expertise");
            Check(Asks("ranger", 8, ChoiceKind.Expertise), "ranger 9: Expertise");
            Check(Asks("sorcerer", 9, ChoiceKind.Metamagic), "sorcerer 10: two more Metamagic");
            Check(Asks("fighter", 6, ChoiceKind.FightingStyle), "champion 7: Additional Fighting Style");
            var bard9 = AtLevel("bard", 9);
            var secrets = bard9.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Spell);
            Check(secrets != null && secrets.Options.Any(id => !SpellsDb.Get(id).Classes.Contains("bard")), "bard 10: Magical Secrets opens other class lists");
            var f6 = AtLevel("fighter", 6);
            var style = f6.PendingChoices().First(c => c.Kind == ChoiceKind.FightingStyle);
            Check(!style.Options.Contains(f6.FightingStyle), "the second fighting style is a different one");
            PlayerCharacter.LevelCap = 4;

            // AutoPlanAnswersEveryChoiceToTen
            PlayerCharacter.LevelCap = 10;
            foreach (var cls in AllClasses)
            {
                var pc = Make(cls);
                pc.XP = 999999;
                bool ok = true;
                try
                {
                    while (pc.Level < 10)
                    {
                        var choices = pc.PendingChoices();
                        var plan = pc.AutoPlan();
                        for (int i = 0; i < choices.Count; i++)
                            if (i >= plan.Picks.Count || plan.Picks[i].Count != System.Math.Min(choices[i].Count, choices[i].Options.Count)) ok = false;
                        pc.LevelUp(plan, null);
                    }
                }
                catch (System.Exception e) { ok = false; System.Console.WriteLine(cls + ": " + e.Message); }
                Check(ok && pc.Level == 10, cls + " auto-levels 1→10 answering every choice");
            }
            PlayerCharacter.LevelCap = 4;
        }
    }
}
