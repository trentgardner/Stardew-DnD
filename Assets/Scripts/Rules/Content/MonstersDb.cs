using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>Stat blocks for the playtest (SRD 5.2 creatures plus a custom kobold leader).</summary>
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
                Loot = { new KeyValuePair<string, float>("gemstone", 1f), new KeyValuePair<string, float>("emberwrought_blade", 1f) },
                CopperMin = 200, CopperMax = 600,
            };
        }

        public static MonsterDef Get(string id) => All.TryGetValue(id, out var m) ? m : null;
    }
}
