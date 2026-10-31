using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules.Content;

namespace DnD.Rules
{
    public enum ChoiceKind { Expertise, BonusSkill, Cantrip, Spell, Feat, FightingStyle, HunterPrey, LandType, Invocation, Metamagic, HunterDefense, DraconicElement, BonusSpell }

    /// <summary>One decision the next level asks for, e.g. "pick 2 spells from these".</summary>
    public class LevelUpChoice
    {
        public ChoiceKind Kind;
        public int Count = 1;
        public string Title;
        /// <summary>Option ids: skill enum names, spell ids, or feat codes ("asi:STR:CON", "grappler:DEX").</summary>
        public List<string> Options = new List<string>();
    }

    /// <summary>The player's answers for one level-up, in the same order as PendingChoices().</summary>
    public class LevelUpPlan
    {
        /// <summary>The rolled hit die, or null to take the average (the player chooses, Critical Role style).</summary>
        public int? HpRoll;
        public List<List<string>> Picks = new List<List<string>>();
    }

    public partial class PlayerCharacter
    {
        static readonly Skill[] ScholarSkills = { Skill.Arcana, Skill.History, Skill.Investigation, Skill.Medicine, Skill.Nature, Skill.Religion };

        /// <summary>The highest spell level the class can cast at a class level (SRD 5.2 tables).</summary>
        public int MaxSpellLevelAt(int level)
        {
            switch (Class.Caster)
            {
                case CasterType.Full: return Math.Min(9, (level + 1) / 2);
                case CasterType.Half: return Math.Min(5, (level + 3) / 4);
                case CasterType.Pact:
                    for (int l = 5; l >= 1; l--) if (SpellSlots.For(CasterType.Pact, level, l) > 0) return l;
                    return 1;
                default: return 0;
            }
        }

        /// <summary>Ability Score Improvement levels: 4, 8, 12, 16, 19 for everyone; the Fighter adds 6 and 14, the Rogue 10.</summary>
        public bool AsiLevel(int level)
        {
            if (level == 4 || level == 8 || level == 12 || level == 16 || level == 19) return true;
            if (Class.Id == "fighter" && (level == 6 || level == 14)) return true;
            if (Class.Id == "rogue" && level == 10) return true;
            return false;
        }

        static readonly string[] MagicalSecretsLists = { "bard", "cleric", "druid", "wizard" };
        static readonly string[] FighterStyles = { "archery", "defense", "dueling", "great_weapon" };

        static readonly Dictionary<string, string[]> FightingStyles = new Dictionary<string, string[]>
        {
            { "paladin", new[] { "defense", "dueling", "great_weapon" } },
            { "ranger", new[] { "archery", "defense", "dueling" } },
        };

        /// <summary>The choices reaching the next level asks for (empty if there's nothing to decide besides HP).</summary>
        public List<LevelUpChoice> PendingChoices()
        {
            int next = Level + 1;
            var list = new List<LevelUpChoice>();

            // Skills
            if ((Class.Id == "bard" && (next == 2 || next == 9)) || (Class.Id == "rogue" && next == 6) || (Class.Id == "ranger" && next == 9))
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Expertise, Count = 2, Title = "Expertise: double your proficiency in two skills",
                    Options = SkillProficiencies.Where(s => !Expertise.Contains(s)).Select(s => s.ToString()).OrderBy(s => s).ToList() });
            if (Class.Id == "wizard" && next == 2)
            {
                var opts = ScholarSkills.Where(s => SkillProficiencies.Contains(s)).ToList();
                if (opts.Count == 0) opts = ScholarSkills.ToList();
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Expertise, Count = 1, Title = "Scholar: Expertise in one field of study", Options = opts.Select(s => s.ToString()).ToList() });
            }
            if (Class.Id == "bard" && next == 3)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.BonusSkill, Count = 3, Title = "College of Lore: three new skill proficiencies",
                    Options = SkillUtil.All.Where(s => !SkillProficiencies.Contains(s)).Select(s => s.ToString()).ToList() });

            if (Class.Id == "ranger" && next == 2)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Expertise, Count = 1, Title = "Deft Explorer: Expertise in one of your skills",
                    Options = SkillProficiencies.Where(s => !Expertise.Contains(s)).Select(s => s.ToString()).OrderBy(s => s).ToList() });
            if (Class.Id == "ranger" && next == 3)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.HunterPrey, Count = 1, Title = "Hunter's Prey", Options = new List<string> { "colossus_slayer", "horde_breaker" } });
            if (Class.Id == "bard" && next == 6 && Class.Subclass?.Id == "lore")
                list.Add(new LevelUpChoice { Kind = ChoiceKind.BonusSpell, Count = 2, Title = "Magical Discoveries: two spells from the Cleric, Druid or Wizard lists (always prepared)",
                    Options = new[] { "cleric", "druid", "wizard" }.SelectMany(c => SpellsDb.ForClass(c, MaxSpellLevelAt(next))).Where(s => !KnowsSpell(s.Id) && !s.Classes.Contains("bard")).Select(s => s.Id).Distinct().ToList() });
            if (Class.Id == "ranger" && next == 7)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.HunterDefense, Count = 1, Title = "Defensive Tactics", Options = new List<string> { "escape_the_horde", "multiattack_defense" } });
            if (Class.Id == "druid" && next == 3)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.LandType, Count = 1, Title = "Circle of the Land: which land calls to you?", Options = CircleSpells.Keys.ToList() });
            int newInvocations = InvocationsKnownAt(next) - Invocations.Count;
            if (newInvocations > 0)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Invocation, Count = newInvocations, Title = $"Eldritch Invocations: learn {newInvocations}",
                    Options = InvocationOptions.Where(o => !Invocations.Contains(o)).ToList() });
            if (Class.Id == "sorcerer" && (next == 2 || next == 10))
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Metamagic, Count = 2, Title = "Metamagic: two ways to twist your spells", Options = MetamagicOptions.Where(o => !Metamagic.Contains(o)).ToList() });
            if (Class.Id == "fighter" && next == 7 && Class.Subclass?.Id == "champion")
                list.Add(new LevelUpChoice { Kind = ChoiceKind.FightingStyle, Count = 1, Title = "Additional Fighting Style", Options = FighterStyles.Where(o => o != FightingStyle).ToList() });
            if (Class.Id == "barbarian" && next == 3)
                list.Add(new LevelUpChoice { Kind = ChoiceKind.BonusSkill, Count = 1, Title = "Primal Knowledge: one more Barbarian skill",
                    Options = Class.SkillOptions.Where(s => !SkillProficiencies.Contains(s)).Select(s => s.ToString()).ToList() });

            if (next == 2 && FightingStyles.TryGetValue(Class.Id, out var styles))
                list.Add(new LevelUpChoice { Kind = ChoiceKind.FightingStyle, Count = 1, Title = "Fighting Style", Options = styles.ToList() });

            // Spells
            if (Class.Caster != CasterType.None)
            {
                int newCantrips = Class.CantripsKnown[next] - Class.CantripsKnown[Level];
                if (newCantrips > 0)
                    list.Add(new LevelUpChoice { Kind = ChoiceKind.Cantrip, Count = newCantrips, Title = $"Learn {newCantrips} new cantrip{(newCantrips > 1 ? "s" : "")}",
                        Options = SpellsDb.ForClass(Class.Id, 0).Where(s => !Cantrips.Contains(s.Id)).Select(s => s.Id).ToList() });
                int maxLevel = MaxSpellLevelAt(next);
                bool wizard = Class.Id == "wizard";
                int newSpells = wizard ? 2 : Class.PreparedSpells[next] - Class.PreparedSpells[Level];
                if (newSpells > 0)
                {
                    var known = wizard ? (ICollection<string>)Spellbook : PreparedSpells;
                    bool secrets = Class.Id == "bard" && next >= 10;
                    var pool = secrets ? MagicalSecretsLists.SelectMany(c => SpellsDb.ForClass(c, maxLevel)).Distinct() : SpellsDb.ForClass(Class.Id, maxLevel);
                    list.Add(new LevelUpChoice { Kind = ChoiceKind.Spell, Count = newSpells,
                        Title = wizard ? "Copy 2 new spells into your spellbook" : secrets ? $"Magical Secrets: prepare {newSpells} more from the Bard, Cleric, Druid or Wizard lists" : $"Prepare {newSpells} more spell{(newSpells > 1 ? "s" : "")}",
                        Options = pool.Where(s => s.Level > 0 && !known.Contains(s.Id)).OrderByDescending(s => s.Level).Select(s => s.Id).ToList() });
                }
                if (wizard && next == 3 && Class.Subclass?.Id == "evoker")
                    list.Add(new LevelUpChoice { Kind = ChoiceKind.Spell, Count = 2, Title = "Evocation Savant: two free Evocation spells",
                        Options = SpellsDb.ForClass("wizard", 2).Where(s => s.Level > 0 && s.School == "Evocation" && !Spellbook.Contains(s.Id)).Select(s => s.Id).ToList() });
            }

            // Ability Score Improvement or a feat (SRD 5.2 general feats) at 4.
            if (AsiLevel(next))
            {
                var opts = new List<string>();
                var abilities = (Ability[])Enum.GetValues(typeof(Ability));
                for (int i = 0; i < 6; i++)
                    for (int j = i; j < 6; j++)
                    {
                        var a = abilities[i]; var b = abilities[j];
                        if (a == b ? Score(a) + 2 <= 20 : Score(a) + 1 <= 20 && Score(b) + 1 <= 20) opts.Add($"asi:{a}:{b}");
                    }
                if (Score(Ability.STR) >= 13 || Score(Ability.DEX) >= 13)
                    foreach (var a in new[] { Ability.STR, Ability.DEX }) if (Score(a) < 20) opts.Add($"grappler:{a}");
                list.Add(new LevelUpChoice { Kind = ChoiceKind.Feat, Count = 1, Title = "Ability Score Improvement or a feat", Options = opts });
            }
            return list;
        }

        /// <summary>Human-readable label for an option id (skill, spell or feat code).</summary>
        public static string DescribeOption(string id)
        {
            if (id.StartsWith("asi:"))
            {
                var p = id.Split(':');
                return p[1] == p[2] ? $"+2 {p[1]}" : $"+1 {p[1]}, +1 {p[2]}";
            }
            if (id.StartsWith("grappler:")) return $"Grappler feat (+1 {id.Split(':')[1]})";
            switch (id)
            {
                case "defense": return "Defense (+1 AC in armor)";
                case "dueling": return "Dueling (+2 damage, one-handed melee)";
                case "great_weapon": return "Great Weapon Fighting (reroll 1s and 2s)";
                case "archery": return "Archery (+2 to hit with ranged weapons)";
                case "escape_the_horde": return "Escape the Horde (Opportunity Attacks against you have Disadvantage)";
                case "multiattack_defense": return "Multiattack Defense (after a creature hits you, its other attacks that turn have Disadvantage)";
                case "colossus_slayer": return "Colossus Slayer (+1d8 once per turn vs a wounded foe)";
                case "horde_breaker": return "Horde Breaker (an extra attack vs a second adjacent foe)";
                case "arid": return "Arid land (Burning Hands, Fire Bolt)";
                case "agonizing_blast": return "Agonizing Blast (+CHA to Eldritch Blast damage)";
                case "armor_of_shadows": return "Armor of Shadows (Mage Armor at will: AC 13 + DEX)";
                case "repelling_blast": return "Repelling Blast (Eldritch Blast pushes 10 ft)";
                case "pact_of_the_blade": return "Pact of the Blade (melee weapons use CHA; proficient with all)";
                case "eldritch_mind": return "Eldritch Mind (Advantage to keep Concentration)";
                case "ember_sight": return "Ember Sight (see 120 ft in darkness)";
                case "quickened": return "Quickened Spell (2 pts: cast an action spell as a Bonus Action)";
                case "empowered": return "Empowered Spell (1 pt: reroll low damage dice)";
                case "careful": return "Careful Spell (1 pt: allies auto-succeed your area saves)";
                case "distant": return "Distant Spell (1 pt: double the range)";
                case "extended": return "Extended Spell (1 pt: double the duration)";
                case "twinned": return "Twinned Spell (spell level in pts: a second target)";
                case "polar": return "Polar land (Fog Cloud, Hold Person, Ray of Frost)";
                case "temperate": return "Temperate land (Misty Step, Shocking Grasp, Sleep)";
            }
            if (Enum.TryParse<Skill>(id, out var sk)) return SkillUtil.Name(sk);
            return SpellsDb.Get(id)?.Name ?? id;
        }

        /// <summary>Levels up with the player's choices.</summary>
        public int LevelUp(LevelUpPlan plan, List<string> log)
        {
            var choices = PendingChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                var picks = i < plan.Picks.Count ? plan.Picks[i] : new List<string>();
                Apply(choices[i], picks.Where(choices[i].Options.Contains).Distinct().Take(choices[i].Count).ToList(), log);
            }
            return GainLevel(plan.HpRoll, log);
        }

        void Apply(LevelUpChoice c, List<string> picks, List<string> log)
        {
            foreach (var id in picks)
            {
                switch (c.Kind)
                {
                    case ChoiceKind.Expertise:
                        var sk = SkillUtil.Parse(id);
                        SkillProficiencies.Add(sk); Expertise.Add(sk);
                        log?.Add($"Expertise: {SkillUtil.Name(sk)}");
                        break;
                    case ChoiceKind.BonusSkill:
                        SkillProficiencies.Add(SkillUtil.Parse(id));
                        log?.Add($"New proficiency: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.Cantrip:
                        if (!Cantrips.Contains(id)) Cantrips.Add(id);
                        log?.Add($"New cantrip: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.Spell:
                        if (Class.Id == "wizard" && !Spellbook.Contains(id)) Spellbook.Add(id);
                        if (!PreparedSpells.Contains(id) && PreparedCount < Class.PreparedSpells[Math.Min(Level + 1, 20)]) PreparedSpells.Add(id);
                        log?.Add(Class.Id == "wizard" ? $"Copied into your spellbook: {DescribeOption(id)}" : $"New spell prepared: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.Invocation:
                        if (!Invocations.Contains(id)) Invocations.Add(id);
                        log?.Add($"Invocation: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.Metamagic:
                        if (!Metamagic.Contains(id)) Metamagic.Add(id);
                        log?.Add($"Metamagic: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.BonusSpell:
                        if (!BonusAlwaysPrepared.Contains(id)) BonusAlwaysPrepared.Add(id);
                        log?.Add($"Magical Discovery: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.HunterDefense:
                        HunterDefense = id;
                        log?.Add($"Defensive Tactics: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.HunterPrey:
                        HunterPrey = id;
                        log?.Add($"Hunter's Prey: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.LandType:
                        LandType = id;
                        foreach (var sp in CircleSpells[id]) if (!BonusAlwaysPrepared.Contains(sp)) BonusAlwaysPrepared.Add(sp);
                        log?.Add($"Circle of the Land: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.FightingStyle:
                        if (FightingStyle == null) FightingStyle = id; else FightingStyle2 = id;
                        log?.Add($"Fighting Style: {DescribeOption(id)}");
                        break;
                    case ChoiceKind.Feat:
                        var p = id.Split(':');
                        if (p[0] == "asi")
                        {
                            Raise((Ability)Enum.Parse(typeof(Ability), p[1]));
                            Raise((Ability)Enum.Parse(typeof(Ability), p[2]));
                        }
                        else
                        {
                            Feats.Add(p[0]);
                            Raise((Ability)Enum.Parse(typeof(Ability), p[1]));
                        }
                        log?.Add($"{DescribeOption(id)}");
                        break;
                }
            }
        }

        void Raise(Ability a) { if (Score(a) < 20) AsiBonus[(int)a]++; }

        /// <summary>Prepared spells that count against the limit (subclass always-prepared spells don't).</summary>
        int PreparedCount
        {
            get
            {
                var always = Class.Subclass != null ? Class.Subclass.AlwaysPreparedAt(Level) : new List<string>();
                return PreparedSpells.Count(s => !always.Contains(s) && !Class.AlwaysPrepared.ContainsKey(s));
            }
        }

        /// <summary>Sensible picks for companions (and anyone who doesn't want to choose): best skills, highest-level new spells, +2 to the main ability.</summary>
        public LevelUpPlan AutoPlan()
        {
            var plan = new LevelUpPlan();
            foreach (var c in PendingChoices())
            {
                IEnumerable<string> picks = c.Options;
                if (c.Kind == ChoiceKind.Expertise) picks = c.Options.OrderByDescending(o => SkillBonus(SkillUtil.Parse(o)));
                if (c.Kind == ChoiceKind.Feat)
                {
                    var main = Class.PrimaryAbility;
                    string best = c.Options.Contains($"asi:{main}:{main}") ? $"asi:{main}:{main}" : c.Options.FirstOrDefault(o => o.StartsWith("asi:") && o.Contains(main.ToString())) ?? c.Options.FirstOrDefault();
                    picks = best != null ? new[] { best } : new string[0];
                }
                plan.Picks.Add(picks.Take(c.Count).ToList());
            }
            return plan;
        }
    }
}
