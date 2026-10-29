using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static PlayerCharacter Make(string cls, string species = "human")
            => CharacterFactory.Create(CharacterFactory.DefaultChoices(cls, species, "Test " + cls));

        /// <summary>Levels up taking the first options of every choice (a stand-in for a player clicking down a list).</summary>
        static void LevelWithFirstPicks(PlayerCharacter pc)
        {
            var plan = new LevelUpPlan { HpRoll = null };
            foreach (var c in pc.PendingChoices()) plan.Picks.Add(c.Options.Take(c.Count).ToList());
            pc.LevelUp(plan, null);
        }

        static void LevelUpTests()
        {
            Eq(PlayerCharacter.PlaytestLevelCap, 4, "level cap is 4");

            // Level 4 offers an Ability Score Improvement or a feat.
            var f = Make("fighter");
            f.XP = 999999;
            while (f.Level < 3) LevelWithFirstPicks(f);
            var at4 = f.PendingChoices();
            var feat = at4.FirstOrDefault(c => c.Kind == ChoiceKind.Feat);
            Check(feat != null, "level 4 offers a feat choice");
            Check(feat != null && feat.Options.Contains("asi:STR:STR") && feat.Options.Contains("asi:STR:CON") && feat.Options.Any(o => o.StartsWith("grappler:")),
                "feat options include +2, +1/+1 and Grappler");

            // ASI is applied exactly and capped at 20.
            int conBefore = f.Score(Ability.CON);
            var plan = new LevelUpPlan { HpRoll = 7 };
            foreach (var c in at4) plan.Picks.Add(c.Kind == ChoiceKind.Feat ? new List<string> { "asi:CON:CON" } : c.Options.Take(c.Count).ToList());
            int hpBefore = f.MaxHP;
            f.LevelUp(plan, null);
            Eq(f.Level, 4, "reached level 4");
            Eq(f.Score(Ability.CON), System.Math.Min(20, conBefore + 2), "ASI +2 CON applied");
            Eq(f.MaxHP - hpBefore, System.Math.Max(1, 7 + f.Mod(Ability.CON)) + (f.Species?.BonusHpPerLevel ?? 0), "rolled HP used (with the new CON)");
            Check(!f.CanLevelUp, "no level 5 yet");

            var capped = Make("fighter");
            capped.BaseScores[(int)Ability.STR] = 19; capped.Scores[(int)Ability.STR] = 19;
            capped.BackgroundBonus[(int)Ability.STR] = 0;
            capped.XP = 999999;
            while (capped.Level < 3) LevelWithFirstPicks(capped);
            var opts = capped.PendingChoices().First(c => c.Kind == ChoiceKind.Feat).Options;
            Check(!opts.Contains("asi:STR:STR"), "+2 STR not offered when it would pass 20");
            Check(opts.Contains("asi:STR:DEX"), "+1 STR (to 20) still offered");

            // Casters choose their new spells.
            var w = Make("wizard");
            w.XP = 999999;
            var wc = w.PendingChoices();
            var spells = wc.FirstOrDefault(c => c.Kind == ChoiceKind.Spell);
            Check(spells != null && spells.Count == 2, "wizard 1→2 adds two spells to the spellbook");
            Check(spells != null && spells.Options.All(id => SpellsDb.Get(id).Level == 1 && !w.Spellbook.Contains(id)), "only new level-1 wizard spells are offered");
            var wantSpells = spells.Options.Skip(1).Take(2).ToList();
            var wplan = new LevelUpPlan();
            foreach (var c in wc) wplan.Picks.Add(c.Kind == ChoiceKind.Spell ? wantSpells : c.Options.Take(c.Count).ToList());
            w.LevelUp(wplan, null);
            Check(wantSpells.All(w.Spellbook.Contains), "picked spells went into the spellbook");

            // Bard expertise at 2 is a choice among proficient skills.
            var b = Make("bard");
            b.XP = 999999;
            var exp = b.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Expertise);
            Check(exp != null && exp.Count == 2, "bard 2 asks for two expertise skills");
            Check(exp != null && exp.Options.All(s => b.SkillProficiencies.Contains(SkillUtil.Parse(s))), "expertise only in proficient skills");

            // Full casters learn a cantrip at 4.
            var c4 = Make("cleric");
            c4.XP = 999999;
            while (c4.Level < 3) LevelWithFirstPicks(c4);
            Check(c4.PendingChoices().Any(c => c.Kind == ChoiceKind.Cantrip && c.Count == 1), "cleric gets a cantrip at 4");

            // The old auto-pick path still works (companions) and reaches 4.
            var comp = Make("bard");
            comp.XP = 999999;
            while (comp.CanLevelUp) comp.LevelUp(comp.AutoPlan(), null);
            Eq(comp.Level, 4, "auto-plan levels a companion to 4");
            Check(comp.Expertise.Count == 2, "auto-plan picks bard expertise");
        }
    }
}
