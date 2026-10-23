using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules.Content;

namespace DnD.Rules
{
    /// <summary>Choices made during character creation.</summary>
    public class CharacterChoices
    {
        public string Name = "Adventurer";
        public string SpeciesId = "human";
        public string ClassId = "fighter";
        public string BackgroundId = "soldier";
        /// <summary>Final rolled scores assigned to STR..CHA.</summary>
        public int[] Scores = { 15, 14, 13, 12, 10, 8 };
        /// <summary>Background increase: ability getting +2 and ability getting +1 (if PlusTwo == PlusOne, use +1/+1/+1).</summary>
        public Ability PlusTwo = Ability.STR, PlusOne = Ability.CON;
        public bool SpreadEvenly;
        public List<Skill> ClassSkills = new List<Skill>();
        public List<Skill> BonusSkills = new List<Skill>();
        public string FightingStyle = "defense";
        public string DivineOrder = "protector";
        public List<string> Cantrips = new List<string>();
        public List<string> Spells = new List<string>();
        public string SpriteId = "pc_default";
    }

    public static class CharacterFactory
    {
        /// <summary>Rolls six ability scores with 4d6-drop-lowest. Each entry holds the four dice.</summary>
        public static List<List<DieRoll>> RollAbilityScores()
        {
            var sets = new List<List<DieRoll>>();
            for (int i = 0; i < 6; i++) sets.Add(Dice.Roll4d6DropLowest());
            return sets;
        }

        public const int FreeRerolls = 5;

        public static PlayerCharacter Create(CharacterChoices ch)
        {
            var pc = new PlayerCharacter
            {
                Name = ch.Name,
                Species = CharacterDb.Species[ch.SpeciesId],
                Class = CharacterDb.Classes[ch.ClassId],
                Background = CharacterDb.Backgrounds[ch.BackgroundId],
                SpriteId = ch.SpriteId,
            };
            Array.Copy(ch.Scores, pc.BaseScores, 6);

            var opts = pc.Background.AbilityOptions;
            if (ch.SpreadEvenly) foreach (var a in opts) pc.BackgroundBonus[(int)a] = 1;
            else
            {
                var two = opts.Contains(ch.PlusTwo) ? ch.PlusTwo : opts[0];
                var one = opts.Contains(ch.PlusOne) && ch.PlusOne != two ? ch.PlusOne : opts.First(a => a != two);
                pc.BackgroundBonus[(int)two] = 2;
                pc.BackgroundBonus[(int)one] = 1;
            }
            // Background increases can't raise a score above 20.
            for (int i = 0; i < 6; i++) pc.BackgroundBonus[i] = Math.Min(pc.BackgroundBonus[i], Math.Max(0, 20 - pc.BaseScores[i]));

            foreach (var s in pc.Background.Skills) pc.SkillProficiencies.Add(s);
            foreach (var s in pc.Species.GrantedSkills) pc.SkillProficiencies.Add(s);
            foreach (var s in ch.ClassSkills) pc.SkillProficiencies.Add(s);
            foreach (var s in ch.BonusSkills) pc.SkillProficiencies.Add(s);

            if (pc.Class.Id == "fighter")
            {
                pc.FightingStyle = ch.FightingStyle;
                pc.MasteredWeapons.AddRange(CharacterDb.DefaultFighterMasteries);
            }
            if (pc.Class.Id == "cleric") pc.DivineOrder = ch.DivineOrder;

            // Spells
            pc.Cantrips.AddRange(ch.Cantrips);
            pc.PreparedSpells.AddRange(ch.Spells);
            if (pc.Class.Id == "wizard") pc.Spellbook.AddRange(ch.Spells);
            if (pc.Background.Id == "acolyte") AddInitiateSpells(pc, "cleric", "guidance", "spare_the_dying", "bless");
            if (pc.Background.Id == "sage") AddInitiateSpells(pc, "wizard", "prestidigitation", "minor_illusion", "mage_armor");

            // Gear
            if (pc.Class.StartingWeapon != null) pc.MainHand = ItemsDb.Get<WeaponDef>(pc.Class.StartingWeapon);
            if (pc.Class.StartingArmor != null)
            {
                var armor = ItemsDb.Get<ArmorDef>(pc.Class.StartingArmor);
                // Thaumaturge clerics can't use the chain shirt? (they can: medium armor). Protector upgrades to chain mail.
                if (pc.Class.Id == "cleric" && pc.DivineOrder == "protector") armor = ItemsDb.Get<ArmorDef>("chain_mail");
                pc.Armor = armor;
            }
            pc.Shield = pc.Class.StartingShield;
            foreach (var it in pc.Class.StartingItems)
            {
                var def = ItemsDb.Get(it);
                if (def is WondrousDef wd) pc.Wear(wd);
                else pc.AddItem(it);
            }
            pc.Copper = pc.Background.StartingGoldCp;

            pc.FinalizeCreation();
            return pc;
        }

        static void AddInitiateSpells(PlayerCharacter pc, string list, string cantripA, string cantripB, string spell)
        {
            if (!pc.Cantrips.Contains(cantripA)) pc.Cantrips.Add(cantripA);
            if (!pc.Cantrips.Contains(cantripB)) pc.Cantrips.Add(cantripB);
            if (!pc.PreparedSpells.Contains(spell)) pc.PreparedSpells.Add(spell);
        }

        /// <summary>Sensible defaults for quick-start characters and generated NPC adventurers.</summary>
        public static CharacterChoices DefaultChoices(string classId, string speciesId, string name)
        {
            var c = new CharacterChoices { Name = name, ClassId = classId, SpeciesId = speciesId, SpriteId = "pc_" + classId };
            switch (classId)
            {
                case "fighter":
                    c.BackgroundId = "soldier"; c.Scores = new[] { 15, 13, 14, 8, 12, 10 }; c.PlusTwo = Ability.STR; c.PlusOne = Ability.CON;
                    c.ClassSkills.AddRange(new[] { Skill.Perception, Skill.Persuasion });
                    break;
                case "wizard":
                    c.BackgroundId = "sage"; c.Scores = new[] { 8, 14, 13, 15, 12, 10 }; c.PlusTwo = Ability.INT; c.PlusOne = Ability.CON;
                    c.ClassSkills.AddRange(new[] { Skill.Investigation, Skill.Insight });
                    c.Cantrips.AddRange(new[] { "fire_bolt", "ray_of_frost", "light" });
                    c.Spells.AddRange(new[] { "magic_missile", "burning_hands", "shield", "sleep" });
                    break;
                case "cleric":
                    c.BackgroundId = "acolyte"; c.Scores = new[] { 13, 10, 14, 8, 15, 12 }; c.PlusTwo = Ability.WIS; c.PlusOne = Ability.CHA;
                    c.ClassSkills.AddRange(new[] { Skill.Medicine, Skill.Persuasion });
                    c.Cantrips.AddRange(new[] { "sacred_flame", "light", "thaumaturgy" });
                    c.Spells.AddRange(new[] { "cure_wounds", "healing_word", "guiding_bolt", "shield_of_faith" });
                    break;
                case "bard":
                    c.BackgroundId = "criminal"; c.Scores = new[] { 8, 14, 13, 12, 10, 15 }; c.PlusTwo = Ability.DEX; c.PlusOne = Ability.CON;
                    c.SpreadEvenly = false;
                    c.ClassSkills.AddRange(new[] { Skill.Persuasion, Skill.Deception, Skill.Performance });
                    c.Cantrips.AddRange(new[] { "vicious_mockery", "minor_illusion" });
                    c.Spells.AddRange(new[] { "healing_word", "dissonant_whispers", "charm_person", "faerie_fire" });
                    break;
            }
            if (CharacterDb.Species[speciesId].BonusSkillChoices > 0)
            {
                foreach (var s in new[] { Skill.Insight, Skill.Athletics, Skill.Stealth, Skill.History })
                {
                    if (c.BonusSkills.Count >= CharacterDb.Species[speciesId].BonusSkillChoices) break;
                    if (!c.ClassSkills.Contains(s)) c.BonusSkills.Add(s);
                }
            }
            return c;
        }

        // ------------------------------------------------------------ tabletop-style recruitable NPCs

        static readonly string[] FirstNames =
        {
            "Wren", "Tamsin", "Corwin", "Isolde", "Bram", "Mirela", "Odo", "Sabine", "Fenn", "Liora",
            "Garrick", "Nim", "Rosalind", "Thorne", "Elsbeth", "Quill", "Hollis", "Maren", "Dax", "Petra",
        };

        static readonly string[] Quirks =
        {
            "hums constantly and badly", "keeps a pet beetle named Sir Clicks", "has very strong opinions about soup",
            "is convinced they're cursed (they are not)", "collects teeth (only shed ones, they insist)",
            "speaks in a dramatic narrator voice when nervous", "owes money to at least three people in this room",
            "is secretly terrified of chickens", "writes everything down in a tiny journal", "laughs at their own jokes before the punchline",
        };

        static readonly string[] Hooks =
        {
            "nursing a drink in the corner, watching the door", "arm-wrestling a farmhand and losing on purpose",
            "reading a battered book by candlelight", "tuning a lute that is absolutely not in tune", "loudly telling a story nobody asked for",
            "sharpening a blade with suspicious care", "asleep face-down on the table", "arguing with Pip about the price of stew",
        };

        public class Recruit
        {
            public PlayerCharacter Character;
            public string Quirk;
            public string Hook;
            public int PersuasionDC;
            public int HireCostCp;
            public string Pitch;
        }

        /// <summary>Generates a tavern adventurer the way a DM would improvise one.</summary>
        public static Recruit GenerateRecruit(Random rng)
        {
            string[] classes = { "fighter", "wizard", "cleric", "bard" };
            string[] species = { "human", "elf", "half_elf", "dwarf", "gnome" };
            string cls = classes[rng.Next(classes.Length)];
            string sp = species[rng.Next(species.Length)];
            string name = FirstNames[rng.Next(FirstNames.Length)];
            var ch = DefaultChoices(cls, sp, name);
            ch.SpriteId = "npc_" + cls;
            var pc = Create(ch);
            pc.Team = Team.Players;
            var r = new Recruit
            {
                Character = pc,
                Quirk = Quirks[rng.Next(Quirks.Length)],
                Hook = Hooks[rng.Next(Hooks.Length)],
                PersuasionDC = 10 + rng.Next(0, 6),
                HireCostCp = (5 + rng.Next(0, 11)) * 100,
            };
            string spName = pc.Species.Name.ToLower();
            r.Pitch = $"A {spName} {pc.Class.Name.ToLower()} named {name}, {r.Hook}. Word is they {r.Quirk}.";
            return r;
        }
    }
}
