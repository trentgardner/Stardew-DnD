using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>Stat blocks for the playtest (SRD 5.2 creatures plus custom leaders: Snik, Wren).</summary>
    public static class MonstersDb
    {
        public static readonly Dictionary<string, MonsterDef> All = new Dictionary<string, MonsterDef>();

        static MonsterAttackDef Melee(string name, int toHit, string dmg, DamageType t, int reach = 5) =>
            new MonsterAttackDef { Name = name, ToHit = toHit, Damage = DiceExpr.Parse(dmg), DamageType = t, ReachFt = reach };

        static MonsterAttackDef Ranged(string name, int toHit, string dmg, DamageType t, int range, int longRange) =>
            new MonsterAttackDef { Name = name, ToHit = toHit, Damage = DiceExpr.Parse(dmg), DamageType = t, RangeNormal = range, RangeLong = longRange, Ranged = true };

        static MonstersDb()
        {
            All["kobold"] = new MonsterDef
            {
                Id = "kobold", Name = "Kobold", SpriteId = "mon_kobold", AC = 12, HitDice = DiceExpr.Parse("2d6-2"),
                Scores = new[] { 7, 15, 9, 8, 7, 8 }, Darkvision = 60, CR = 0.125, XP = 25,
                Description = "A small, scaly dragon-worshipper. These ones are VERY excited about something.",
                Attacks = { Melee("Dagger", 4, "1d4+2", DamageType.Piercing), Ranged("Sling", 4, "1d4+2", DamageType.Bludgeoning, 30, 120) },
                PackTactics = true, SunlightSensitivity = true, Behavior = MonsterBehavior.Skirmisher,
                Traits = { "Pack Tactics", "Sunlight Sensitivity" },
                Loot = { new KeyValuePair<string, float>("copper_trinket", 0.5f) }, CopperMin = 0, CopperMax = 30,
            };
            All["kobold_slinger"] = new MonsterDef
            {
                Id = "kobold_slinger", Name = "Kobold Slinger", SpriteId = "mon_kobold_slinger", AC = 12, HitDice = DiceExpr.Parse("2d6-2"),
                Scores = new[] { 7, 15, 9, 8, 7, 8 }, Darkvision = 60, CR = 0.125, XP = 25,
                Description = "Stays at the back and throws rocks with great enthusiasm and moderate accuracy.",
                Attacks = { Ranged("Sling", 4, "1d4+2", DamageType.Bludgeoning, 30, 120), Melee("Dagger", 4, "1d4+2", DamageType.Piercing) },
                PackTactics = true, SunlightSensitivity = true, Behavior = MonsterBehavior.Ranged,
                Traits = { "Pack Tactics", "Sunlight Sensitivity" },
                CopperMin = 0, CopperMax = 20,
            };
            All["giant_rat"] = new MonsterDef
            {
                Id = "giant_rat", Name = "Giant Rat", SpriteId = "mon_rat", AC = 12, HitDice = DiceExpr.Parse("2d6"),
                Scores = new[] { 7, 15, 11, 2, 10, 4 }, Darkvision = 60, CR = 0.125, XP = 25, Speed = 30,
                Description = "Mine-dwelling rodent the size of a dog. Mostly teeth.",
                Attacks = { Melee("Bite", 4, "1d4+2", DamageType.Piercing) },
                PackTactics = true, Traits = { "Pack Tactics" },
            };
            All["wolf"] = new MonsterDef
            {
                Id = "wolf", Name = "Wolf", SpriteId = "mon_wolf", AC = 13, HitDice = DiceExpr.Parse("2d8+2"),
                Scores = new[] { 12, 15, 12, 3, 12, 6 }, CR = 0.25, XP = 50, Speed = 40,
                Description = "A lean grey wolf from the mountain forests.",
                Attacks = { new MonsterAttackDef { Name = "Bite", ToHit = 4, Damage = DiceExpr.Parse("1d6+2"), DamageType = DamageType.Piercing, OnHitEffect = "prone", OnHitSave = Ability.STR, OnHitSaveDC = 11 } },
                PackTactics = true, ProficientSkills = { Skill.Perception, Skill.Stealth }, Traits = { "Pack Tactics", "Keen Hearing and Smell" },
            };
            All["goblin"] = new MonsterDef
            {
                Id = "goblin", Name = "Goblin", SpriteId = "mon_goblin", AC = 15, HitDice = DiceExpr.Parse("2d6"),
                Scores = new[] { 8, 15, 10, 10, 8, 8 }, Darkvision = 60, CR = 0.25, XP = 50,
                Description = "Small, nasty and opportunistic. Will run if the fight turns.",
                Attacks = { Melee("Scimitar", 4, "1d6+2", DamageType.Slashing), Ranged("Shortbow", 4, "1d6+2", DamageType.Piercing, 80, 320) },
                NimbleEscape = true, ProficientSkills = { Skill.Stealth }, Behavior = MonsterBehavior.Skirmisher,
                Traits = { "Nimble Escape" }, CopperMin = 5, CopperMax = 60,
            };
            All["snik"] = new MonsterDef
            {
                Id = "snik", Name = "Snik, Egg-Warden", SpriteId = "mon_snik", AC = 14, HitDice = DiceExpr.Parse("6d6+6"),
                Scores = new[] { 10, 15, 12, 10, 10, 14 }, Darkvision = 60, CR = 0.5, XP = 100,
                Description = "Self-appointed guardian of the Great Egg. Wears a saucepan as a helmet with tremendous dignity.",
                Attacks = { Melee("Spear", 4, "1d6+2", DamageType.Piercing), Ranged("Sling", 4, "1d4+2", DamageType.Bludgeoning, 30, 120) },
                PackTactics = true, SunlightSensitivity = true, ProficientSkills = { Skill.Intimidation },
                Traits = { "Pack Tactics", "Sunlight Sensitivity", "Devoted: can be reasoned with if you show respect to the Egg." },
                Loot = { new KeyValuePair<string, float>("gemstone", 1f), new KeyValuePair<string, float>("emberwrought_blade", 1f), new KeyValuePair<string, float>("copper_kettle", 1f) },
                CopperMin = 200, CopperMax = 600,
            };
            All["bandit"] = new MonsterDef
            {
                Id = "bandit", Name = "Ford Warden", SpriteId = "mon_bandit", AC = 12, HitDice = DiceExpr.Parse("2d8+2"),
                Scores = new[] { 11, 12, 12, 10, 10, 10 }, CR = 0.125, XP = 25,
                Description = "An out-of-work bargeman with a crossbow and a 'toll' business. (SRD Bandit.)",
                Attacks = { Melee("Scimitar", 3, "1d6+1", DamageType.Slashing), Ranged("Light Crossbow", 3, "1d8+1", DamageType.Piercing, 80, 320) },
                Behavior = MonsterBehavior.Ranged, CopperMin = 10, CopperMax = 80,
            };
            All["wren"] = new MonsterDef
            {
                Id = "wren", Name = "Wren, Toll-Captain", SpriteId = "mon_wren", AC = 14, HitDice = DiceExpr.Parse("5d8+5"),
                Scores = new[] { 12, 15, 12, 11, 13, 14 }, CR = 0.5, XP = 100,
                Description = "Captain of a barge that hasn't sailed since the bridge fell. Quick with a boathook, quicker with her mouth.",
                Attacks = { Melee("Boathook", 4, "1d8+2", DamageType.Piercing, 10), Ranged("Hand Crossbow", 4, "1d6+2", DamageType.Piercing, 30, 120) },
                ProficientSkills = { Skill.Perception, Skill.Persuasion }, Behavior = MonsterBehavior.Skirmisher,
                Traits = { "Reach (boathook)", "Desperate, not cruel: can be reasoned with." },
                Loot = { new KeyValuePair<string, float>("potion_healing", 1f), new KeyValuePair<string, float>("gemstone", 0.5f) },
                CopperMin = 300, CopperMax = 700,
            };
            // Beasts a druid can Wild Shape into (SRD 5.2 stat blocks, simplified to what combat uses).
            All["cat"] = new MonsterDef
            {
                Id = "cat", Name = "Cat", SpriteId = "mon_cat", AC = 12, HitDice = DiceExpr.Parse("1d4"),
                Scores = new[] { 3, 15, 10, 3, 12, 7 }, Darkvision = 60, CR = 0, XP = 10, Speed = 40,
                Description = "Quiet, quick, and fits through gaps no person could.",
                Attacks = { Melee("Scratch", 4, "1d1", DamageType.Slashing) }, ProficientSkills = { Skill.Perception, Skill.Stealth },
            };
            All["kobold_warboss"] = new MonsterDef
            {
                Id = "kobold_warboss", Name = "Krag, Kobold War-Boss", SpriteId = "mon_kobold_warboss", AC = 14, HitDice = DiceExpr.Parse("5d6+5"),
                Scores = new[] { 12, 15, 12, 8, 9, 12 }, Darkvision = 60, CR = 0.5, XP = 100, Speed = 30,
                Description = "The biggest kobold in the warrens, which makes him about as tall as your belt. He has a spear, a shield made from a pot lid, and a great deal to prove.",
                Attacks = { Melee("Spear", 4, "1d6+2", DamageType.Piercing) },
                PackTactics = true, SunlightSensitivity = true, Traits = { "Pack Tactics", "Sunlight Sensitivity" },
                Loot = { new KeyValuePair<string, float>("copper_trinket", 1f) }, CopperMin = 50, CopperMax = 200,
            };
            All["animated_armor"] = new MonsterDef
            {
                Id = "animated_armor", Name = "The Last Watchman", SpriteId = "mon_animated_armor", AC = 18, HitDice = DiceExpr.Parse("6d8+6"),
                Scores = new[] { 14, 11, 13, 1, 3, 1 }, Darkvision = 60, CR = 1, XP = 200, Speed = 25,
                Description = "An empty suit of watch-armor that never stopped standing guard. (SRD Animated Armor. This engine gives it one Slam per turn instead of two; it is immune to poison and psychic damage in the books.)",
                Attacks = { Melee("Slam", 4, "1d6+2", DamageType.Bludgeoning) },
                Traits = { "Antimagic Susceptibility", "False Appearance", "Immune: poison, psychic" },
            };
            All["giant_fire_beetle"] = new MonsterDef
            {
                Id = "giant_fire_beetle", Name = "Giant Fire Beetle", SpriteId = "mon_fire_beetle", AC = 13, HitDice = DiceExpr.Parse("1d6+1"),
                Scores = new[] { 8, 10, 12, 1, 7, 3 }, Darkvision = 30, CR = 0, XP = 10, Speed = 30,
                Description = "A beetle the size of a dog, with two glands on its back that glow like coals. Drawn to anything that shines.",
                Attacks = { Melee("Bite", 1, "1d6-1", DamageType.Fire) }, Traits = { "Illumination" },
            };
            All["frog"] = new MonsterDef
            {
                Id = "frog", Name = "Frog", SpriteId = "mon_frog", AC = 11, HitDice = DiceExpr.Parse("1d4-1"),
                Scores = new[] { 1, 13, 8, 1, 8, 3 }, Darkvision = 30, CR = 0, XP = 10, Speed = 20,
                Description = "Swims, hops, and is beneath anyone's notice.",
                Attacks = { Melee("Bite", 3, "1d1", DamageType.Piercing) }, ProficientSkills = { Skill.Perception, Skill.Stealth },
            };
            All["black_bear"] = new MonsterDef
            {
                Id = "black_bear", Name = "Black Bear", SpriteId = "mon_bear", AC = 11, HitDice = DiceExpr.Parse("3d8+6"),
                Scores = new[] { 15, 12, 14, 2, 12, 7 }, Darkvision = 60, CR = 0.5, XP = 100, Speed = 30,
                Description = "Big, strong, and very interested in your lunch.",
                Attacks = { Melee("Rend", 4, "1d6+2", DamageType.Slashing) }, ProficientSkills = { Skill.Perception },
            };
            All["brawler"] = new MonsterDef
            {
                Id = "brawler", Name = "Rowdy Farmhand", SpriteId = "npc_brawler", AC = 10, HitDice = DiceExpr.Parse("2d8+2"),
                Scores = new[] { 13, 10, 12, 10, 10, 10 }, CR = 0.125, XP = 25,
                Description = "Payday, cider, and something to prove. (SRD Commoner, a little sturdier; unarmed.)",
                Attacks = { Melee("Fists and furniture", 3, "1d4+1", DamageType.Bludgeoning) },
                Traits = { "Just here for a good time" },
            };
            All["owlbear"] = new MonsterDef
            {
                Id = "owlbear", Name = "Owlbear", SpriteId = "mon_owlbear", AC = 13, HitDice = DiceExpr.Parse("7d10+21"),
                Scores = new[] { 20, 12, 17, 3, 12, 7 }, Darkvision = 60, CR = 3, XP = 700, Speed = 40,
                Description = "Half owl, half bear, all mother. (SRD Owlbear, one Rend per turn: the Act I mother is tuned for a level 3 party.)",
                Attacks = { Melee("Rend", 7, "2d8+5", DamageType.Slashing) },
                ProficientSkills = { Skill.Perception }, Traits = { "Keen sight and smell", "Protective of her cub" },
            };
            All["boar"] = new MonsterDef
            {
                Id = "boar", Name = "Boar", SpriteId = "mon_boar", AC = 11, HitDice = DiceExpr.Parse("2d8+4"),
                Scores = new[] { 13, 11, 14, 2, 9, 5 }, CR = 0.25, XP = 50, Speed = 40,
                Description = "Bristly, bad-tempered and very protective of its meadow. (SRD Boar.)",
                Attacks = { Melee("Gore", 3, "1d6+1", DamageType.Piercing) },
                Traits = { "Bad temper" },
            };
            // ---------------- Act II Phase 3: the blight's beasts, and the Academy's guardians
            All["ember_eyed_boar"] = new MonsterDef
            {
                Id = "ember_eyed_boar", Name = "Ember-Eyed Boar", SpriteId = "mon_ember_boar", AC = 12, HitDice = DiceExpr.Parse("5d10+15"),
                Scores = new[] { 17, 10, 16, 2, 7, 5 }, CR = 2, XP = 450, Speed = 40,
                Description = "A giant boar gone wrong: ash in its bristles, a coal-glow where its eyes should be. It doesn't seem to feel pain. (SRD Giant Boar.)",
                Attacks = { Melee("Tusk", 5, "2d6+3", DamageType.Slashing) },
                Traits = { "Charge", "Relentless" },
            };
            All["rug_of_smothering"] = new MonsterDef
            {
                Id = "rug_of_smothering", Name = "Rug of Smothering", SpriteId = "mon_rug", AC = 12, HitDice = DiceExpr.Parse("6d10"),
                Scores = new[] { 17, 14, 10, 1, 3, 1 }, CR = 2, XP = 450, Speed = 10,
                Description = "A long red rug with a gold border that is, unmistakably, a guard. It lies very flat until it doesn't. (SRD Rug of Smothering; the grapple is left out.)",
                Attacks = { Melee("Smother", 5, "2d6+3", DamageType.Bludgeoning) },
                Traits = { "False Appearance", "Damage Transfer" },
            };
            // ---------------- Wild Shape forms at druid 8 (CR 1, flying)
            All["dire_wolf"] = new MonsterDef
            {
                Id = "dire_wolf", Name = "Dire Wolf", SpriteId = "mon_dire_wolf", AC = 14, HitDice = DiceExpr.Parse("5d10+10"),
                Scores = new[] { 17, 15, 15, 3, 12, 7 }, CR = 1, XP = 200, Speed = 50, PackTactics = true,
                Description = "A wolf the size of a pony. (SRD Dire Wolf.)",
                Attacks = { new MonsterAttackDef { Name = "Bite", ToHit = 5, Damage = DiceExpr.Parse("2d6+3"), DamageType = DamageType.Piercing, OnHitEffect = "prone", OnHitSave = Ability.STR, OnHitSaveDC = 13 } },
                ProficientSkills = { Skill.Perception, Skill.Stealth },
            };
            All["giant_owl"] = new MonsterDef
            {
                Id = "giant_owl", Name = "Giant Owl", SpriteId = "mon_owlbear_cub", AC = 12, HitDice = DiceExpr.Parse("3d10+3"),
                Scores = new[] { 13, 15, 12, 8, 13, 10 }, Darkvision = 120, CR = 0.25, XP = 50, Speed = 5, FlySpeed = 60,
                Description = "A great silent owl. (SRD Giant Owl.)",
                Attacks = { Melee("Talons", 3, "2d6+1", DamageType.Slashing) },
                ProficientSkills = { Skill.Perception, Skill.Stealth },
            };

            // ---------------- Act II test monsters (SRD; stat lines from the SRD stat blocks, names neutral)
            All["ogre"] = new MonsterDef
            {
                Id = "ogre", Name = "Ogre", SpriteId = "mon_ogre", AC = 11, HitDice = DiceExpr.Parse("7d10+21"),
                Scores = new[] { 19, 8, 16, 5, 7, 7 }, Darkvision = 60, CR = 2, XP = 450, Speed = 40,
                Description = "Huge, hungry and mostly club. (SRD Ogre.)",
                Attacks = { Melee("Greatclub", 6, "2d8+4", DamageType.Bludgeoning) },
                CopperMin = 200, CopperMax = 1500,
            };
            All["cinder_hound"] = new MonsterDef
            {
                Id = "cinder_hound", Name = "Cinder Hound", SpriteId = "mon_cinder_hound", AC = 15, HitDice = DiceExpr.Parse("7d8+14"),
                Scores = new[] { 17, 12, 14, 6, 13, 6 }, Darkvision = 60, CR = 3, XP = 700, Speed = 50, PackTactics = true,
                Description = "A hound with coals for eyes and smoke for breath. (SRD Hell Hound stats.)",
                Attacks = { Melee("Bite", 5, "1d8+3", DamageType.Piercing) },
                Immunities = { DamageType.Fire },
                Abilities = { new MonsterAbilityDef { Name = "Fire Breath", Shape = TargetShape.Cone, AreaFt = 15, Dice = DiceExpr.Parse("6d6"), DamageType = DamageType.Fire, SaveAbility = Ability.DEX, SaveDC = 12, RechargeMin = 5 } },
                ProficientSkills = { Skill.Perception },
            };
            All["troll"] = new MonsterDef
            {
                Id = "troll", Name = "Troll", SpriteId = "mon_troll", AC = 15, HitDice = DiceExpr.Parse("8d10+40"),
                Scores = new[] { 18, 13, 20, 7, 9, 7 }, Darkvision = 60, CR = 5, XP = 1800, Speed = 30, ProficiencyBonus = 3,
                Description = "Rubbery, ravenous, and it grows back. Bring fire. (SRD Troll.)",
                Attacks = { Melee("Bite", 7, "1d6+4", DamageType.Piercing), Melee("Claw", 7, "2d6+4", DamageType.Slashing) },
                Multiattack = { "Bite", "Claw", "Claw" },
                Regeneration = 10, RegenBlockedBy = { DamageType.Fire, DamageType.Acid },
                ProficientSkills = { Skill.Perception }, CopperMin = 500, CopperMax = 3000,
            };
            All["fire_elemental"] = new MonsterDef
            {
                Id = "fire_elemental", Name = "Fire Elemental", SpriteId = "mon_fire_elemental", AC = 13, HitDice = DiceExpr.Parse("12d10+36"),
                Scores = new[] { 10, 17, 16, 6, 10, 7 }, Darkvision = 60, CR = 5, XP = 1800, Speed = 50, ProficiencyBonus = 3,
                Description = "A living bonfire in a vaguely person-shaped mood. (SRD Fire Elemental.)",
                Attacks = { Melee("Touch", 6, "2d6+3", DamageType.Fire) },
                Multiattack = { "Touch", "Touch" },
                Immunities = { DamageType.Fire, DamageType.Poison },
                Resistances = { DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing },
            };
            All["salamander"] = new MonsterDef
            {
                Id = "salamander", Name = "Salamander", SpriteId = "mon_salamander", AC = 15, HitDice = DiceExpr.Parse("12d10+24"),
                Scores = new[] { 18, 14, 15, 11, 10, 12 }, Darkvision = 60, CR = 5, XP = 1800, Speed = 30, ProficiencyBonus = 3,
                Description = "A serpent of the deep fires, spear in hand. (SRD Salamander.)",
                Attacks = { Melee("Fire Spear", 7, "2d6+4", DamageType.Fire), Melee("Constrict", 7, "2d6+4", DamageType.Bludgeoning) },
                Multiattack = { "Fire Spear", "Constrict" },
                Immunities = { DamageType.Fire }, Vulnerabilities = { DamageType.Cold },
                CopperMin = 300, CopperMax = 2000,
            };
            All["ankheg"] = new MonsterDef
            {
                Id = "ankheg", Name = "Ankheg", SpriteId = "mon_ankheg", AC = 14, HitDice = DiceExpr.Parse("6d10+6"),
                Scores = new[] { 17, 11, 13, 1, 13, 6 }, Darkvision = 60, CR = 2, XP = 450, Speed = 30,
                Description = "A burrowing insect the size of a cart, mandibles first. (SRD Ankheg.)",
                Attacks = { Melee("Bite", 5, "2d6+3", DamageType.Slashing) },
                Abilities = { new MonsterAbilityDef { Name = "Acid Spray", Shape = TargetShape.Line, AreaFt = 30, Dice = DiceExpr.Parse("3d6"), DamageType = DamageType.Acid, SaveAbility = Ability.DEX, SaveDC = 13, RechargeMin = 6 } },
            };

            // The party's hatchling (see Dragonling): HP comes from the party's level, not from hit dice.
            All["dragonling"] = new MonsterDef
            {
                Id = "dragonling", Name = "Dragonling", SpriteId = "mon_dragonling", AC = 13, HitDice = DiceExpr.Parse("3d4"),
                Scores = new[] { 10, 14, 13, 6, 12, 12 }, Darkvision = 60, Speed = 30,
                Description = "A newly hatched red dragon the size of a cat. Fiercely loyal, easily distracted by shiny things.",
                Attacks = { Melee("Bite", 4, "1d4+2", DamageType.Piercing) },
                ProficientSkills = { Skill.Perception }, Resistances = { DamageType.Fire },
                Traits = { "Ember Breath (Recharge 5–6)", "Scared, not slain: at 0 HP it flees until the next rest" },
            };
        }

        public static MonsterDef Get(string id) => All.TryGetValue(id, out var m) ? m : null;
    }
}
