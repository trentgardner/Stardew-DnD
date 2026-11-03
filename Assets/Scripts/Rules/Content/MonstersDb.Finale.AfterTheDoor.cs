using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>
    /// Act III Phase 6: the finales' bosses and the forces' soldiers. EXEMPT from the secret guard (LoreTests.GuardScans):
    /// none of these appear before WorldDb.MetHer and the choice at the turn. Phase beats always play (BossPhaseDef: no hit
    /// skips a threshold). The terror is the fire, never the girl.
    /// </summary>
    public static partial class MonstersDb
    {
        static void BuildFinale()
        {
            // End her: the fire comes out of her as the legend's terror. SRD Adult Red Dragon at heart, tuned as the game's biggest fight.
            All["ashen_queen"] = new MonsterDef
            {
                Id = "ashen_queen", Name = "The Ashen Queen", SpriteId = "mon_ashen_queen", AC = 19, HitDice = DiceExpr.Parse("28d12+196"),
                Scores = new[] { 27, 10, 25, 16, 13, 23 }, Darkvision = 120, CR = 17, XP = 18000, Speed = 40, FlySpeed = 80, ProficiencyBonus = 6, Scale = 2.2f,
                Description = "A thousand years of the world's fire, poured into one shape: the one the stories gave it. Wings like a burning sky, a crown of horns, eyes like the inside of a forge. It is not her. It was only ever kept in her. (SRD Adult Red Dragon, tuned as a boss.)",
                Attacks = { Melee("Rend", 14, "1d10+8", DamageType.Slashing, 10), Melee("Ember Rend", 14, "2d4+8", DamageType.Fire, 10) },
                Multiattack = { "Rend", "Rend", "Ember Rend" },
                Immunities = { DamageType.Fire },
                ProficientSaves = { Ability.DEX, Ability.CON, Ability.WIS, Ability.CHA },
                ProficientSkills = { Skill.Perception },
                Abilities = { new MonsterAbilityDef { Name = "Fire Breath", Shape = TargetShape.Cone, AreaFt = 60, Dice = DiceExpr.Parse("17d6"), DamageType = DamageType.Fire, SaveAbility = Ability.DEX, SaveDC = 21, RechargeMin = 5, Telegraph = true } },
                LegendaryPerRound = 3,
                LegendaryActions =
                {
                    new LegendaryActionDef { Name = "Tail", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Tail", RangeFt = 15 },
                    new LegendaryActionDef { Name = "Wing Buffet", Cost = 2, Kind = LegendaryKind.SaveBurst, RangeFt = 15, Dice = DiceExpr.Parse("2d6+8"), DamageType = DamageType.Bludgeoning, SaveAbility = Ability.DEX, SaveDC = 22 },
                },
                Phases =
                {
                    new BossPhaseDef { At = 0.66, RechargeMin = 4, LairEruptions = 2, Beats = new[] { "The fire roars." } },
                    new BossPhaseDef { At = 0.33, RechargeMin = 4, LairEruptions = 4, LegendaryPerRound = 4, Beats = new[] { "The fire screams." } },
                },
            };
            All["ashen_queen"].Attacks.Add(Melee("Tail", 14, "2d8+8", DamageType.Bludgeoning, 15));

            // Seal her: the King comes down to stop the seal, in the regalia the throne has fed on her fire for a thousand years.
            All["king_regalia"] = new MonsterDef
            {
                Id = "king_regalia", Name = "King Aldric, in the Iron Regalia", SpriteId = "mon_king_regalia", AC = 20, HitDice = DiceExpr.Parse("30d10+150"),
                Scores = new[] { 20, 12, 20, 14, 14, 20 }, Darkvision = 60, CR = 14, XP = 11500, Speed = 30, ProficiencyBonus = 5, Scale = 1.6f,
                Description = "Black iron plate from the same forge as the Ring, gold at every seam, and every seam warm. The throne's oldest heirloom, and the reason the throne's lamps never went out. (SRD Knight's spirit, built as a boss.)",
                Attacks = { Melee("Sceptre", 11, "2d8+6", DamageType.Bludgeoning) },
                Multiattack = { "Sceptre", "Sceptre" },
                Resistances = { DamageType.Fire },
                MagicResistance = true, ParryBonus = 3,
                ProficientSaves = { Ability.STR, Ability.CON, Ability.WIS, Ability.CHA },
                Abilities = { new MonsterAbilityDef { Name = "Chains of the Ring", Shape = TargetShape.Sphere, AreaFt = 20, Dice = DiceExpr.Parse("4d8"), DamageType = DamageType.Force, SaveAbility = Ability.STR, SaveDC = 18, RechargeMin = 5 } },
                LegendaryPerRound = 2,
                LegendaryActions = { new LegendaryActionDef { Name = "Sceptre", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Sceptre", RangeFt = 5 } },
                Phases = { new BossPhaseDef { At = 0.5, RechargeMin = 4, Beats = new[] { "The King hauls on the chains." } } },
            };

            // Save her: the King takes the fire drawn out of her, and the fire takes the King.
            All["crowned_fire"] = new MonsterDef
            {
                Id = "crowned_fire", Name = "The Crowned Fire", SpriteId = "mon_crowned_fire", AC = 18, HitDice = DiceExpr.Parse("32d12+160"),
                Scores = new[] { 24, 14, 22, 14, 16, 24 }, Darkvision = 120, CR = 17, XP = 18000, Speed = 30, FlySpeed = 60, ProficiencyBonus = 6, Scale = 2.0f,
                Description = "A man-shaped blaze in a crown, taller than the palace doors. Somewhere in the middle of it, a king is still giving orders. (Built as a boss in the spirit of the SRD's fire elementals and dragons.)",
                Attacks = { Melee("Flame Sceptre", 13, "2d10+7", DamageType.Fire, 10) },
                Multiattack = { "Flame Sceptre", "Flame Sceptre" },
                Immunities = { DamageType.Fire },
                ProficientSaves = { Ability.DEX, Ability.CON, Ability.WIS, Ability.CHA },
                Abilities = { new MonsterAbilityDef { Name = "Pyre of Lamps", Shape = TargetShape.Line, AreaFt = 60, Dice = DiceExpr.Parse("16d6"), DamageType = DamageType.Fire, SaveAbility = Ability.DEX, SaveDC = 20, RechargeMin = 5, Telegraph = true } },
                LegendaryPerRound = 3,
                LegendaryActions =
                {
                    new LegendaryActionDef { Name = "Flame Sceptre", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Flame Sceptre", RangeFt = 10 },
                    new LegendaryActionDef { Name = "Searing Crown", Cost = 2, Kind = LegendaryKind.SaveBurst, RangeFt = 10, Dice = DiceExpr.Parse("3d10"), DamageType = DamageType.Fire, SaveAbility = Ability.CON, SaveDC = 20 },
                },
                Phases =
                {
                    new BossPhaseDef { At = 0.66, LairEruptions = 3, Beats = new[] { "The lamps flare." } },
                    new BossPhaseDef { At = 0.33, RechargeMin = 4, LairEruptions = 4, Beats = new[] { "The fire stops obeying." } },
                },
            };

            // The forces' soldiers: foes or allies, depending on where their force stands at the turn.
            All["wyrmwatch_hunter"] = new MonsterDef
            {
                Id = "wyrmwatch_hunter", Name = "Wyrmwatch Hunter", SpriteId = "mon_wyrmwatch_hunter", AC = 15, HitDice = DiceExpr.Parse("9d8+18"),
                Scores = new[] { 12, 18, 14, 11, 14, 10 }, CR = 4, XP = 1100, Speed = 30, ProficiencyBonus = 2, Behavior = MonsterBehavior.Ranged,
                Description = "Leathers, a bell on the belt and a crossbow that could stop a cart. They've been waiting their whole lives for a dragon to shoot. (SRD Scout, hardened.)",
                Attacks = { Ranged("Heavy Crossbow", 7, "1d10+4", DamageType.Piercing, 100, 400), Melee("Hunting Knife", 7, "1d6+4", DamageType.Piercing) },
                Multiattack = { "Heavy Crossbow", "Heavy Crossbow" },
                ProficientSkills = { Skill.Perception, Skill.Survival },
                CopperMin = 300, CopperMax = 1500,
            };
            All["fae_knight"] = new MonsterDef
            {
                Id = "fae_knight", Name = "Knight of the Courts", SpriteId = "mon_fae_knight", AC = 17, HitDice = DiceExpr.Parse("10d8+30"),
                Scores = new[] { 18, 14, 16, 12, 13, 16 }, CR = 5, XP = 1800, Speed = 30, ProficiencyBonus = 3,
                Description = "Armour grown, not made: bark and frost and petals. Very polite. Very sharp. (SRD Knight, with a Fae twist.)",
                Attacks = { Melee("Thorn Blade", 8, "1d8+5", DamageType.Slashing) },
                Multiattack = { "Thorn Blade", "Thorn Blade" },
                Resistances = { DamageType.Cold },
                ProficientSaves = { Ability.CON, Ability.CHA },
            };
            All["academy_warden"] = new MonsterDef
            {
                Id = "academy_warden", Name = "Academy Ward-Mage", SpriteId = "mon_academy_warden", AC = 15, HitDice = DiceExpr.Parse("12d8+12"),
                Scores = new[] { 9, 14, 12, 18, 13, 11 }, CR = 5, XP = 1800, Speed = 30, ProficiencyBonus = 3, Behavior = MonsterBehavior.Ranged,
                Description = "Starfall blue, ink on the cuffs, a ward already half-drawn in the air. (SRD Mage, trimmed.)",
                Attacks = { Ranged("Arcane Bolt", 8, "2d10", DamageType.Force, 120, 120) },
                Multiattack = { "Arcane Bolt", "Arcane Bolt" },
                Abilities = { new MonsterAbilityDef { Name = "Shatter", Shape = TargetShape.Sphere, AreaFt = 10, RangeFt = 60, Dice = DiceExpr.Parse("3d8"), DamageType = DamageType.Thunder, SaveAbility = Ability.CON, SaveDC = 16, RechargeMin = 5 } },
                ProficientSaves = { Ability.INT, Ability.WIS },
            };
        }
    }
}
