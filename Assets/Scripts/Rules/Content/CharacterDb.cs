using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>Species, backgrounds and classes (SRD 5.2, plus a custom Half-Elf in 2024 style).</summary>
    public static class CharacterDb
    {
        public static readonly Dictionary<string, SpeciesDef> Species = new Dictionary<string, SpeciesDef>();
        public static readonly Dictionary<string, BackgroundDef> Backgrounds = new Dictionary<string, BackgroundDef>();
        public static readonly Dictionary<string, ClassDef> Classes = new Dictionary<string, ClassDef>();

        static CharacterDb()
        {
            BuildSpecies();
            BuildBackgrounds();
            BuildClasses();
        }

        // ================================================================ species

        static void BuildSpecies()
        {
            Species["human"] = new SpeciesDef
            {
                Id = "human", Name = "Human", Speed = 30,
                Description = "Ambitious, adaptable and everywhere. Humans built most of Sundermere's towns, and argue about all of them.",
                Traits = { "Resourceful: you gain Heroic Inspiration whenever you finish a Long Rest.", "Skillful: proficiency in one extra skill of your choice." },
                BonusSkillChoices = 1, HeroicInspirationOnRest = true,
            };
            Species["elf"] = new SpeciesDef
            {
                Id = "elf", Name = "Elf", Speed = 30, Darkvision = 60,
                Description = "Long-lived and graceful, elves remember the days before the Sundering, or claim their grandparents did.",
                Traits = { "Darkvision 60 ft.", "Fey Ancestry: Advantage on saves to avoid or end the Charmed condition.", "Keen Senses: proficiency in Perception.", "Trance: you don't need sleep; a Long Rest takes 4 hours of meditation." },
                GrantedSkills = { Skill.Perception }, AdvantageVsCharmed = true,
            };
            Species["half_elf"] = new SpeciesDef
            {
                Id = "half_elf", Name = "Half-Elf",
                Speed = 30, Darkvision = 60,
                Description = "Belonging to two worlds and fully claimed by neither, half-elves tend to be excellent at reading a room.",
                Traits = { "Darkvision 60 ft.", "Fey Ancestry: Advantage on saves to avoid or end the Charmed condition.", "Skill Versatility: proficiency in two skills of your choice." },
                BonusSkillChoices = 2, AdvantageVsCharmed = true,
            };
            Species["dwarf"] = new SpeciesDef
            {
                Id = "dwarf", Name = "Dwarf", Speed = 30, Darkvision = 120,
                Description = "Stout, stubborn and deeply loyal. Dwarves of the Wyrmback Mountains say they can hear the dragons snoring.",
                Traits = { "Darkvision 120 ft.", "Dwarven Resilience: resistance to poison damage; Advantage on saves against being Poisoned.", "Dwarven Toughness: +1 HP per level.", "Stonecunning: tremorsense through stone (flavor)." },
                PoisonResilience = true, BonusHpPerLevel = 1,
            };
            Species["gnome"] = new SpeciesDef
            {
                Id = "gnome", Name = "Gnome", Speed = 30, Darkvision = 60,
                Description = "Small, curious and relentlessly inventive. A gnome has never met a mechanism they didn't want to open.",
                Traits = { "Darkvision 60 ft.", "Gnomish Cunning: Advantage on Intelligence, Wisdom and Charisma saving throws." },
                AdvantageMentalSaves = true,
            };
        }

        // ================================================================ backgrounds

        static void BuildBackgrounds()
        {
            Backgrounds["acolyte"] = new BackgroundDef
            {
                Id = "acolyte", Name = "Acolyte", AbilityOptions = new[] { Ability.INT, Ability.WIS, Ability.CHA },
                Skills = new[] { Skill.Insight, Skill.Religion }, OriginFeat = "Magic Initiate (Cleric)",
                OriginFeatText = "You learn two Cleric cantrips and one level 1 Cleric spell.", StartingGoldCp = 800,
                Description = "You served in a temple, perhaps Aurelith's, under Mother Elowen's patient eye.",
            };
            Backgrounds["criminal"] = new BackgroundDef
            {
                Id = "criminal", Name = "Criminal", AbilityOptions = new[] { Ability.DEX, Ability.CON, Ability.INT },
                Skills = new[] { Skill.SleightOfHand, Skill.Stealth }, OriginFeat = "Alert",
                OriginFeatText = "Add your Proficiency Bonus to Initiative.", StartingGoldCp = 1600,
                Description = "You made a living in the shadows of the capital. Thistledown is supposed to be a fresh start.",
            };
            Backgrounds["sage"] = new BackgroundDef
            {
                Id = "sage", Name = "Sage", AbilityOptions = new[] { Ability.CON, Ability.INT, Ability.WIS },
                Skills = new[] { Skill.Arcana, Skill.History }, OriginFeat = "Magic Initiate (Wizard)",
                OriginFeatText = "You learn two Wizard cantrips and one level 1 Wizard spell.", StartingGoldCp = 800,
                Description = "You studied at, or at least near, the Starfall Academy. The library fines are still outstanding.",
            };
            Backgrounds["soldier"] = new BackgroundDef
            {
                Id = "soldier", Name = "Soldier", AbilityOptions = new[] { Ability.STR, Ability.DEX, Ability.CON },
                Skills = new[] { Skill.Athletics, Skill.Intimidation }, OriginFeat = "Savage Attacker",
                OriginFeatText = "Once per turn, roll a weapon's damage dice twice and use either total.", StartingGoldCp = 1400,
                Description = "You marched under the Crown's banner. You've seen enough war to appreciate a quiet village.",
            };
        }

        // ================================================================ classes

        static int[] Table(params int[] byLevel)
        {
            var t = new int[21];
            for (int i = 1; i <= 20; i++) t[i] = byLevel[System.Math.Min(i - 1, byLevel.Length - 1)];
            return t;
        }

        static void BuildClasses()
        {
            var fighter = new ClassDef
            {
                Id = "fighter", Name = "Fighter", HitDie = 10, PrimaryAbility = Ability.STR,
                SavingThrows = new[] { Ability.STR, Ability.CON }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Acrobatics, Skill.AnimalHandling, Skill.Athletics, Skill.History, Skill.Insight, Skill.Intimidation, Skill.Persuasion, Skill.Perception, Skill.Survival },
                LightArmor = true, MediumArmor = true, HeavyArmor = true, Shields = true, MartialWeapons = true,
                WeaponMasteries = 3,
                Description = "A master of weapons and armor. Simple to play, hard to kill, and the best at rolling lots of damage dice.",
                StartingWeapon = "longsword", StartingArmor = "chain_mail", StartingShield = true,
                StartingItems = { "javelin", "javelin", "potion_healing", "rations", "torch" },
            };
            fighter.Features.Add(new FeatureDef(1, "Fighting Style", "Choose a combat specialty (Defense, Dueling, Great Weapon Fighting, Archery)."));
            fighter.Features.Add(new FeatureDef(1, "Second Wind", "Bonus Action: regain 1d10 + Fighter level HP. 2 uses; regain one on a Short Rest, all on a Long Rest."));
            fighter.Features.Add(new FeatureDef(1, "Weapon Mastery", "Use the mastery properties of 3 kinds of weapons."));
            fighter.Features.Add(new FeatureDef(2, "Action Surge", "Once per Short Rest, take one additional action on your turn."));
            fighter.Features.Add(new FeatureDef(2, "Tactical Mind", "When you fail an ability check, spend a Second Wind use to add 1d10 to it (not spent if you still fail)."));
            fighter.Features.Add(new FeatureDef(3, "Fighter Subclass", "You become a Champion."));
            fighter.Subclass = new SubclassDef
            {
                Id = "champion", Name = "Champion",
                Description = "Raw physical excellence. Crit more, hit harder.",
                Features =
                {
                    new FeatureDef(3, "Improved Critical", "Your attacks score a Critical Hit on a 19 or 20."),
                    new FeatureDef(3, "Remarkable Athlete", "Advantage on Initiative and on Strength (Athletics) checks."),
                },
            };
            Classes["fighter"] = fighter;

            var wizard = new ClassDef
            {
                Id = "wizard", Name = "Wizard", HitDie = 6, PrimaryAbility = Ability.INT,
                SavingThrows = new[] { Ability.INT, Ability.WIS }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Arcana, Skill.History, Skill.Insight, Skill.Investigation, Skill.Medicine, Skill.Nature, Skill.Religion },
                Caster = CasterType.Full, SpellAbility = Ability.INT,
                CantripsKnown = Table(3, 3, 3, 4), PreparedSpells = Table(4, 5, 6, 7),
                Description = "A scholar of arcane magic with a spellbook full of answers. Fragile, but devastating.",
                StartingWeapon = "quarterstaff", StartingItems = { "dagger", "potion_healing", "rations", "torch" },
            };
            wizard.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Wizard spells using Intelligence. Prepare spells from your spellbook."));
            wizard.Features.Add(new FeatureDef(1, "Ritual Adept", "Cast any Ritual spell in your spellbook without preparing it."));
            wizard.Features.Add(new FeatureDef(1, "Arcane Recovery", "Once per day on a Short Rest, recover spell slots totaling up to half your Wizard level (rounded up)."));
            wizard.Features.Add(new FeatureDef(2, "Scholar", "Gain Expertise in one of Arcana, History, Investigation, Medicine, Nature or Religion."));
            wizard.Features.Add(new FeatureDef(3, "Wizard Subclass", "You become an Evoker."));
            wizard.Subclass = new SubclassDef
            {
                Id = "evoker", Name = "Evoker",
                Description = "Master of destructive elemental magic.",
                Features =
                {
                    new FeatureDef(3, "Evocation Savant", "Add two Evocation spells to your spellbook for free."),
                    new FeatureDef(3, "Potent Cantrip", "When a creature succeeds on a save against your damaging cantrip (or you miss), it still takes half damage."),
                },
            };
            Classes["wizard"] = wizard;

            var cleric = new ClassDef
            {
                Id = "cleric", Name = "Cleric", HitDie = 8, PrimaryAbility = Ability.WIS,
                SavingThrows = new[] { Ability.WIS, Ability.CHA }, SkillChoices = 2,
                SkillOptions = new[] { Skill.History, Skill.Insight, Skill.Medicine, Skill.Persuasion, Skill.Religion },
                LightArmor = true, MediumArmor = true, Shields = true,
                Caster = CasterType.Full, SpellAbility = Ability.WIS,
                CantripsKnown = Table(3, 3, 3, 4), PreparedSpells = Table(4, 5, 6, 7),
                Description = "A champion of the gods. Heals, protects and smites. Every party wants one.",
                StartingWeapon = "mace", StartingArmor = "chain_shirt", StartingShield = true,
                StartingItems = { "potion_healing", "rations", "torch" },
            };
            cleric.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Cleric spells using Wisdom. Change your prepared spells after a Long Rest."));
            cleric.Features.Add(new FeatureDef(1, "Divine Order", "Protector (heavy armor + martial weapons) or Thaumaturge (extra cantrip, bonus to Arcana/Religion)."));
            cleric.Features.Add(new FeatureDef(2, "Channel Divinity", "2 uses per Short Rest. Divine Spark: heal or deal 1d8 + WIS radiant/necrotic. Turn Undead."));
            cleric.Features.Add(new FeatureDef(3, "Cleric Subclass", "You follow the Life Domain."));
            cleric.Subclass = new SubclassDef
            {
                Id = "life", Name = "Life Domain",
                Description = "Aurelith the Dawnmother's chosen healers.",
                Features =
                {
                    new FeatureDef(3, "Disciple of Life", "Your healing spells restore extra HP equal to 2 + the spell's level."),
                    new FeatureDef(3, "Preserve Life", "Channel Divinity: restore 5 × Cleric level HP, split among allies within 30 ft (up to half their max HP)."),
                    new FeatureDef(3, "Life Domain Spells", "Aid, Bless, Cure Wounds and Lesser Restoration are always prepared."),
                },
                AlwaysPreparedSpells = { "aid", "bless", "cure_wounds", "lesser_restoration" },
            };
            Classes["cleric"] = cleric;

            var bard = new ClassDef
            {
                Id = "bard", Name = "Bard", HitDie = 8, PrimaryAbility = Ability.CHA,
                SavingThrows = new[] { Ability.DEX, Ability.CHA }, SkillChoices = 3,
                SkillOptions = new Skill[0],
                LightArmor = true,
                Caster = CasterType.Full, SpellAbility = Ability.CHA,
                CantripsKnown = Table(2, 2, 2, 3), PreparedSpells = Table(4, 5, 6, 7),
                Description = "A performer whose music is literally magic. Talks the party out of trouble, and inspires them through it.",
                StartingWeapon = "dagger", StartingArmor = "leather",
                StartingItems = { "dagger", "potion_healing", "rations", "torch", "fennicks_lucky_copper" },
            };
            bard.Features.Add(new FeatureDef(1, "Bardic Inspiration", "Bonus Action: give an ally a d6. When they fail a d20 Test, they can add it. Uses = CHA modifier per Long Rest."));
            bard.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Bard spells using Charisma."));
            bard.Features.Add(new FeatureDef(2, "Expertise", "Double your Proficiency Bonus for two skills."));
            bard.Features.Add(new FeatureDef(2, "Jack of All Trades", "Add half your Proficiency Bonus to ability checks you aren't proficient in."));
            bard.Features.Add(new FeatureDef(3, "Bard Subclass", "You join the College of Lore."));
            bard.Subclass = new SubclassDef
            {
                Id = "lore", Name = "College of Lore",
                Description = "Collectors of secrets, stories and devastating comebacks.",
                Features =
                {
                    new FeatureDef(3, "Bonus Proficiencies", "Gain proficiency in three skills of your choice."),
                    new FeatureDef(3, "Cutting Words", "Reaction: spend Bardic Inspiration to subtract the die from an enemy's attack roll, damage roll or ability check."),
                },
            };
            Classes["bard"] = bard;
        }

        /// <summary>Standard weapon mastery picks for a new Fighter (player can change them).</summary>
        public static readonly string[] DefaultFighterMasteries = { "longsword", "greatsword", "javelin" };
    }
}
