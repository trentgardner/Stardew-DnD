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

            var rogue = new ClassDef
            {
                Id = "rogue", Name = "Rogue", HitDie = 8, PrimaryAbility = Ability.DEX,
                SavingThrows = new[] { Ability.DEX, Ability.INT }, SkillChoices = 4,
                SkillOptions = new[] { Skill.Acrobatics, Skill.Athletics, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Investigation, Skill.Perception, Skill.Persuasion, Skill.SleightOfHand, Skill.Stealth },
                LightArmor = true, MartialFinesseOrLight = true, WeaponMasteries = 2,
                Description = "A sneak, a scout, a picker of locks and pockets. One precise strike does what others need three swings for.",
                StartingWeapon = "shortsword", StartingArmor = "leather",
                StartingItems = { "dagger", "dagger", "shortbow", "potion_healing", "rations", "torch" },
            };
            rogue.Features.Add(new FeatureDef(1, "Expertise", "Double your Proficiency Bonus for two skills you're proficient in."));
            rogue.Features.Add(new FeatureDef(1, "Sneak Attack", "Once per turn, deal extra damage (1d6, 2d6 at level 3) with a Finesse or Ranged weapon when you have Advantage, or when an ally is next to your target."));
            rogue.Features.Add(new FeatureDef(1, "Weapon Mastery", "Use the mastery properties of 2 kinds of weapons."));
            rogue.Features.Add(new FeatureDef(2, "Cunning Action", "Bonus Action: Dash, Disengage or Hide."));
            rogue.Features.Add(new FeatureDef(3, "Steady Aim", "Bonus Action (if you haven't moved): Advantage on your next attack this turn; your Speed becomes 0."));
            rogue.Features.Add(new FeatureDef(3, "Rogue Subclass", "You become a Thief."));
            rogue.Subclass = new SubclassDef
            {
                Id = "thief", Name = "Thief",
                Description = "Burglar, treasure hunter, and the reason the shop has a bell on the door.",
                Features =
                {
                    new FeatureDef(3, "Fast Hands", "Cunning Action can also Use an Object (drink a potion, pull a lever) or make a Sleight of Hand check."),
                    new FeatureDef(3, "Second-Story Work", "You climb as fast as you walk, and jump farther (add your DEX modifier in feet)."),
                },
            };
            Classes["rogue"] = rogue;

            var barbarian = new ClassDef
            {
                Id = "barbarian", Name = "Barbarian", HitDie = 12, PrimaryAbility = Ability.STR,
                SavingThrows = new[] { Ability.STR, Ability.CON }, SkillChoices = 2,
                SkillOptions = new[] { Skill.AnimalHandling, Skill.Athletics, Skill.Intimidation, Skill.Nature, Skill.Perception, Skill.Survival },
                LightArmor = true, MediumArmor = true, Shields = true, MartialWeapons = true, WeaponMasteries = 2,
                Description = "A storm of a person. Rage through the pain, shrug off blows, and hit like a falling tree.",
                StartingWeapon = "greataxe",
                StartingItems = { "handaxe", "handaxe", "handaxe", "handaxe", "potion_healing", "rations", "torch" },
            };
            barbarian.Features.Add(new FeatureDef(1, "Rage", "Bonus Action: +2 damage with Strength attacks, resistance to bludgeoning, piercing and slashing, Advantage on Strength checks and saves. 2 uses (3 at level 3); regain one on a Short Rest, all on a Long Rest."));
            barbarian.Features.Add(new FeatureDef(1, "Unarmored Defense", "Without armor, your AC is 10 + DEX + CON (a shield still helps)."));
            barbarian.Features.Add(new FeatureDef(1, "Weapon Mastery", "Use the mastery properties of 2 kinds of weapons."));
            barbarian.Features.Add(new FeatureDef(2, "Danger Sense", "Advantage on Dexterity saving throws."));
            barbarian.Features.Add(new FeatureDef(2, "Reckless Attack", "Attack with Advantage on Strength attacks this turn, but attacks against you have Advantage until your next turn."));
            barbarian.Features.Add(new FeatureDef(3, "Primal Knowledge", "Gain proficiency in another Barbarian skill."));
            barbarian.Features.Add(new FeatureDef(3, "Barbarian Subclass", "You walk the Path of the Berserker."));
            barbarian.Subclass = new SubclassDef
            {
                Id = "berserker", Name = "Path of the Berserker",
                Description = "Rage as a weapon. Wild, joyful, unstoppable.",
                Features =
                {
                    new FeatureDef(3, "Frenzy", "While raging and attacking recklessly, the first hit each turn deals extra damage: a d6 for each point of your Rage damage."),
                },
            };
            Classes["barbarian"] = barbarian;

            var monk = new ClassDef
            {
                Id = "monk", Name = "Monk", HitDie = 8, PrimaryAbility = Ability.DEX,
                SavingThrows = new[] { Ability.STR, Ability.DEX }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Acrobatics, Skill.Athletics, Skill.History, Skill.Insight, Skill.Religion, Skill.Stealth },
                Description = "A martial artist who channels inner focus into lightning-fast strikes. No armor needed, no weapon required.",
                StartingWeapon = "spear",
                StartingItems = { "dagger", "dagger", "potion_healing", "rations", "torch" },
            };
            monk.Features.Add(new FeatureDef(1, "Martial Arts", "Unarmed strikes and monk weapons use DEX and a d6 (or the weapon's die if bigger). Bonus Action: an unarmed strike."));
            monk.Features.Add(new FeatureDef(1, "Unarmored Defense", "Without armor or a shield, your AC is 10 + DEX + WIS."));
            monk.Features.Add(new FeatureDef(2, "Monk's Focus", "Focus Points equal to your level (back on a Short Rest): Flurry of Blows, Patient Defense, Step of the Wind."));
            monk.Features.Add(new FeatureDef(2, "Unarmored Movement", "+10 ft Speed while not wearing armor or a shield."));
            monk.Features.Add(new FeatureDef(2, "Uncanny Metabolism", "Once per Long Rest, when you roll Initiative: regain all Focus Points and heal a Martial Arts die + your level."));
            monk.Features.Add(new FeatureDef(3, "Deflect Attacks", "Reaction when hit by a bludgeoning, piercing or slashing attack: reduce the damage by 1d10 + DEX + your monk level."));
            monk.Features.Add(new FeatureDef(3, "Monk Subclass", "You become a Warrior of the Open Hand."));
            monk.Features.Add(new FeatureDef(4, "Slow Fall", "Reaction: reduce falling damage by 5 × your monk level."));
            monk.Subclass = new SubclassDef
            {
                Id = "open_hand", Name = "Warrior of the Open Hand",
                Description = "Masters of unarmed combat: push, trip and stun with bare hands.",
                Features = { new FeatureDef(3, "Open Hand Technique", "When Flurry of Blows hits, the target makes a DEX save or is knocked Prone.") },
            };
            Classes["monk"] = monk;

            var paladin = new ClassDef
            {
                Id = "paladin", Name = "Paladin", HitDie = 10, PrimaryAbility = Ability.STR,
                SavingThrows = new[] { Ability.WIS, Ability.CHA }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Athletics, Skill.Insight, Skill.Intimidation, Skill.Medicine, Skill.Persuasion, Skill.Religion },
                LightArmor = true, MediumArmor = true, HeavyArmor = true, Shields = true, MartialWeapons = true, WeaponMasteries = 2,
                Caster = CasterType.Half, SpellAbility = Ability.CHA, PreparedSpells = Table(2, 3, 4, 5, 6, 6, 7, 7, 9, 9),
                Description = "A holy knight bound by an oath. Heavy armor, healing hands, and smites that light up the room.",
                StartingWeapon = "longsword", StartingArmor = "chain_mail", StartingShield = true,
                StartingItems = { "javelin", "javelin", "potion_healing", "rations", "torch" },
            };
            paladin.Features.Add(new FeatureDef(1, "Lay on Hands", "Bonus Action: heal from a pool of 5 × your level HP (refills on a Long Rest)."));
            paladin.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Paladin spells using Charisma."));
            paladin.Features.Add(new FeatureDef(1, "Weapon Mastery", "Use the mastery properties of 2 kinds of weapons."));
            paladin.Features.Add(new FeatureDef(2, "Fighting Style", "Choose a combat specialty (Defense, Dueling, Great Weapon Fighting)."));
            paladin.Features.Add(new FeatureDef(2, "Paladin's Smite", "Divine Smite is always prepared, and once per Long Rest you can cast it without a slot."));
            paladin.Features.Add(new FeatureDef(3, "Channel Divinity", "2 uses (one back on a Short Rest). Divine Sense: sense celestials, fiends and undead nearby."));
            paladin.Features.Add(new FeatureDef(3, "Paladin Subclass", "You swear the Oath of Devotion."));
            paladin.Subclass = new SubclassDef
            {
                Id = "devotion", Name = "Oath of Devotion",
                Description = "Honesty, courage, compassion. The shining knight of the stories.",
                Features =
                {
                    new FeatureDef(3, "Sacred Weapon", "Channel Divinity: for 10 minutes add your CHA modifier to attack rolls with your weapon, and it sheds light."),
                    new FeatureDef(3, "Oath of Devotion Spells", "Protection from Evil and Good and Shield of Faith are always prepared."),
                },
                AlwaysPreparedSpells = { "protection_from_evil_and_good", "shield_of_faith" },
            };
            paladin.AlwaysPrepared["divine_smite"] = 2;
            Classes["paladin"] = paladin;

            var ranger = new ClassDef
            {
                Id = "ranger", Name = "Ranger", HitDie = 10, PrimaryAbility = Ability.DEX,
                SavingThrows = new[] { Ability.STR, Ability.DEX }, SkillChoices = 3,
                SkillOptions = new[] { Skill.AnimalHandling, Skill.Athletics, Skill.Insight, Skill.Investigation, Skill.Nature, Skill.Perception, Skill.Stealth, Skill.Survival },
                LightArmor = true, MediumArmor = true, Shields = true, MartialWeapons = true, WeaponMasteries = 2,
                Caster = CasterType.Half, SpellAbility = Ability.WIS, PreparedSpells = Table(2, 3, 4, 5, 6, 6, 7, 7, 9, 9),
                Description = "A wilderness hunter and tracker with a bow, a blade and a little nature magic. Marks a target and never lets it go.",
                StartingWeapon = "longbow", StartingArmor = "leather",
                StartingItems = { "shortsword", "shortsword", "potion_healing", "rations", "torch" },
            };
            ranger.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Ranger spells using Wisdom."));
            ranger.Features.Add(new FeatureDef(1, "Favored Enemy", "Hunter's Mark is always prepared, and you can cast it twice per Long Rest without a slot."));
            ranger.Features.Add(new FeatureDef(1, "Weapon Mastery", "Use the mastery properties of 2 kinds of weapons."));
            ranger.Features.Add(new FeatureDef(2, "Deft Explorer", "Expertise in one of your skills, and two extra languages."));
            ranger.Features.Add(new FeatureDef(2, "Fighting Style", "Choose a combat specialty (Archery, Defense, Dueling)."));
            ranger.Features.Add(new FeatureDef(3, "Ranger Subclass", "You become a Hunter."));
            ranger.AlwaysPrepared["hunters_mark"] = 1;
            ranger.Subclass = new SubclassDef
            {
                Id = "hunter", Name = "Hunter",
                Description = "The shield between civilization and the things in the dark woods.",
                Features =
                {
                    new FeatureDef(3, "Hunter's Lore", "You learn the immunities, resistances and vulnerabilities of creatures marked by your Hunter's Mark."),
                    new FeatureDef(3, "Hunter's Prey", "Colossus Slayer (+1d8 once per turn against a wounded creature) or Horde Breaker (an extra attack against a second creature next to your target)."),
                },
            };
            Classes["ranger"] = ranger;

            var druid = new ClassDef
            {
                Id = "druid", Name = "Druid", HitDie = 8, PrimaryAbility = Ability.WIS,
                SavingThrows = new[] { Ability.INT, Ability.WIS }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Arcana, Skill.AnimalHandling, Skill.Insight, Skill.Medicine, Skill.Nature, Skill.Perception, Skill.Religion, Skill.Survival },
                LightArmor = true, Shields = true,
                Caster = CasterType.Full, SpellAbility = Ability.WIS,
                CantripsKnown = Table(2, 2, 2, 3), PreparedSpells = Table(4, 5, 6, 7),
                Description = "A keeper of the wild with the old magic of root and storm. Heals, tangles foes in vines, and turns into a wolf when it matters.",
                StartingWeapon = "quarterstaff", StartingArmor = "leather", StartingShield = true,
                StartingItems = { "sickle", "potion_healing", "rations", "torch" },
            };
            druid.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Druid spells using Wisdom."));
            druid.Features.Add(new FeatureDef(1, "Druidic", "You know the secret language of druids."));
            druid.Features.Add(new FeatureDef(1, "Primal Order", "Magician (an extra cantrip, WIS to Arcana and Nature) or Warden (medium armor and martial weapons)."));
            druid.Features.Add(new FeatureDef(2, "Wild Shape", "Bonus Action: turn into a beast you know (wolf, boar, cat, frog; bear at 4). 2 uses; one back on a Short Rest. Temporary HP = your level; no spellcasting while shaped."));
            druid.Features.Add(new FeatureDef(2, "Wild Companion", "Spend a Wild Shape use to summon a spirit familiar."));
            druid.Features.Add(new FeatureDef(3, "Druid Subclass", "You join the Circle of the Land."));
            druid.Subclass = new SubclassDef
            {
                Id = "land", Name = "Circle of the Land",
                Description = "Mystics bound to a land: its spells are always on your tongue.",
                Features =
                {
                    new FeatureDef(3, "Circle Spells", "Choose a land (arid, polar or temperate): its spells are always prepared."),
                    new FeatureDef(3, "Land's Aid", "Magic action, spend a Wild Shape use: a burst of thorns (CON save, 2d6 necrotic) and one creature in it heals 2d6."),
                },
            };
            Classes["druid"] = druid;

            var sorcerer = new ClassDef
            {
                Id = "sorcerer", Name = "Sorcerer", HitDie = 6, PrimaryAbility = Ability.CHA,
                SavingThrows = new[] { Ability.CON, Ability.CHA }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Arcana, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Persuasion, Skill.Religion },
                Caster = CasterType.Full, SpellAbility = Ability.CHA,
                CantripsKnown = Table(4, 4, 4, 5), PreparedSpells = Table(2, 4, 6, 7),
                Description = "Magic is in your blood (a dragon's, in your case). Fewer spells than a wizard, but you can bend and twist every one of them.",
                StartingWeapon = "spear", StartingItems = { "dagger", "dagger", "potion_healing", "rations", "torch" },
            };
            sorcerer.Features.Add(new FeatureDef(1, "Spellcasting", "Cast Sorcerer spells using Charisma."));
            sorcerer.Features.Add(new FeatureDef(1, "Innate Sorcery", "Bonus Action, twice per Long Rest: for 1 minute your spell save DC is +1 and your spell attacks have Advantage."));
            sorcerer.Features.Add(new FeatureDef(2, "Font of Magic", "Sorcery Points equal to your level: turn spell slots into points, or points into slots."));
            sorcerer.Features.Add(new FeatureDef(2, "Metamagic", "Two ways to twist your spells (Quickened, Empowered, Careful, Distant, Extended, Twinned)."));
            sorcerer.Features.Add(new FeatureDef(3, "Sorcerer Subclass", "Your magic is Draconic."));
            sorcerer.Subclass = new SubclassDef
            {
                Id = "draconic", Name = "Draconic Sorcery",
                Description = "A dragon ancestor's fire still burns in you. (In Sundermere, that matters.)",
                Features =
                {
                    new FeatureDef(3, "Draconic Resilience", "+1 HP per sorcerer level, and without armor your AC is 10 + DEX + CHA."),
                    new FeatureDef(3, "Draconic Spells", "Alter Self, Chromatic Orb, Command and Dragon's Breath are always prepared."),
                },
                AlwaysPreparedSpells = { "alter_self", "chromatic_orb", "command", "dragons_breath" },
            };
            Classes["sorcerer"] = sorcerer;

            var warlock = new ClassDef
            {
                Id = "warlock", Name = "Warlock", HitDie = 8, PrimaryAbility = Ability.CHA,
                SavingThrows = new[] { Ability.WIS, Ability.CHA }, SkillChoices = 2,
                SkillOptions = new[] { Skill.Arcana, Skill.Deception, Skill.History, Skill.Intimidation, Skill.Investigation, Skill.Nature, Skill.Religion },
                LightArmor = true,
                Caster = CasterType.Pact, SpellAbility = Ability.CHA,
                CantripsKnown = Table(2, 2, 2, 3), PreparedSpells = Table(2, 3, 4, 5),
                Description = "You made a deal with an ancient ember-spirit for power. Few spell slots, but they come back on every short rest, and Eldritch Blast never gets old.",
                StartingWeapon = "sickle", StartingArmor = "leather",
                StartingItems = { "dagger", "dagger", "potion_healing", "rations", "torch" },
            };
            warlock.Features.Add(new FeatureDef(1, "Eldritch Invocations", "Secret gifts from your patron (one at level 1, three at level 2)."));
            warlock.Features.Add(new FeatureDef(1, "Pact Magic", "Cast Warlock spells using Charisma. Your few slots are all the same level and come back on a Short Rest."));
            warlock.Features.Add(new FeatureDef(2, "Magical Cunning", "Once per Long Rest, spend 1 minute to regain half your pact slots."));
            warlock.Features.Add(new FeatureDef(3, "Warlock Subclass", "Your patron is an Ember Spirit."));
            warlock.Subclass = new SubclassDef
            {
                Id = "ember", Name = "Ember Patron",
                Description = "An ancient fire-spirit from the dragons' age, hungry to be remembered. (SRD: the Fiend Patron.)",
                Features =
                {
                    new FeatureDef(3, "Ember's Blessing", "When you drop a hostile creature to 0 HP, gain temporary HP equal to your CHA modifier + warlock level."),
                    new FeatureDef(3, "Ember Patron Spells", "Burning Hands, Command, Scorching Ray and Suggestion are always prepared."),
                },
                AlwaysPreparedSpells = { "burning_hands", "command", "scorching_ray", "suggestion" },
            };
            Classes["warlock"] = warlock;
        }

        /// <summary>Standard weapon mastery picks for a new Fighter (player can change them).</summary>
        public static readonly string[] DefaultFighterMasteries = { "longsword", "greatsword", "javelin" };
        public static readonly Dictionary<string, string[]> DefaultMasteries = new Dictionary<string, string[]>
        {
            { "fighter", DefaultFighterMasteries }, { "rogue", new[] { "shortsword", "dagger" } }, { "barbarian", new[] { "greataxe", "handaxe" } },
            { "paladin", new[] { "longsword", "javelin" } },
            { "ranger", new[] { "longbow", "shortsword" } },
        };
    }
}
