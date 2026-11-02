using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>SRD 5.2 spells for the playtest classes, levels 0-2.</summary>
    public static class SpellsDb
    {
        public static readonly Dictionary<string, SpellDef> All = new Dictionary<string, SpellDef>();

        static SpellDef Add(SpellDef s, params string[] classes)
        {
            s.Classes.AddRange(classes);
            All[s.Id] = s;
            return s;
        }

        const string W = "wizard", C = "cleric", B = "bard", P = "paladin", R = "ranger", D = "druid", S = "sorcerer", K = "warlock";

        static SpellsDb()
        {
            // ---------------- Cantrips
            Add(new SpellDef { Id = "fire_bolt", Name = "Fire Bolt", Level = 0, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d10"), DamageType = DamageType.Fire,
                Description = "Hurl a mote of fire. Ranged spell attack, 1d10 fire." }, W, S);
            Add(new SpellDef { Id = "ray_of_frost", Name = "Ray of Frost", Level = 0, School = "Evocation", RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Cold, EffectId = "ray_of_frost",
                Description = "A frigid beam. Ranged spell attack, 1d8 cold, and the target's Speed drops by 10 ft." }, W, S);
            Add(new SpellDef { Id = "shocking_grasp", Name = "Shocking Grasp", Level = 0, School = "Evocation", RangeFt = 5, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Lightning,
                Description = "Lightning springs from your hand. Melee spell attack, 1d8 lightning; the target can't take Opportunity Attacks." }, W, S);
            Add(new SpellDef { Id = "sacred_flame", Name = "Sacred Flame", Level = 0, School = "Evocation", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.DEX, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Radiant,
                Description = "Flame-like radiance descends. DEX save or 1d8 radiant. Ignores cover." }, C);
            Add(new SpellDef { Id = "vicious_mockery", Name = "Vicious Mockery", Level = 0, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Dice = DiceExpr.Parse("1d6"), DamageType = DamageType.Psychic, EffectId = "vicious_mockery",
                Description = "Unleash a string of magical insults. WIS save or 1d6 psychic and Disadvantage on its next attack." }, B);
            Add(new SpellDef { Id = "guidance", Name = "Guidance", Level = 0, School = "Divination", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "guidance",
                Description = "Touch an ally: their next ability check gets +1d4." }, C, B, D);
            Add(new SpellDef { Id = "light", Name = "Light", Level = 0, School = "Evocation", RangeFt = 5, Kind = SpellKind.Utility, EffectId = "light",
                Description = "An object sheds bright light in a 20-ft radius for 1 hour. Essential in dark dungeons." }, W, C, B, S);
            Add(new SpellDef { Id = "spare_the_dying", Name = "Spare the Dying", Level = 0, School = "Necromancy", RangeFt = 15, Kind = SpellKind.Utility, EffectId = "stabilize",
                Description = "Stabilize a creature at 0 HP." }, C);
            Add(new SpellDef { Id = "thaumaturgy", Name = "Thaumaturgy", Level = 0, School = "Transmutation", RangeFt = 30, Kind = SpellKind.Utility,
                Description = "Minor wonders: booming voice, flickering flames, tremors. Grants Advantage on an Intimidation check in conversation." }, C);
            Add(new SpellDef { Id = "minor_illusion", Name = "Minor Illusion", Level = 0, School = "Illusion", RangeFt = 30, Kind = SpellKind.Utility,
                Description = "Create a sound or an image. Useful for distractions (Advantage on a Deception or Stealth check)." }, W, B, S, K);
            Add(new SpellDef { Id = "prestidigitation", Name = "Prestidigitation", Level = 0, School = "Transmutation", RangeFt = 10, Kind = SpellKind.Utility,
                Description = "Harmless magical tricks. Clean, chill, flavor, light candles. Very good at impressing children and drunk dwarves." }, W, B, S, K);

            // ---------------- Level 1
            Add(new SpellDef { Id = "magic_missile", Name = "Magic Missile", Level = 1, School = "Evocation", RangeFt = 120, Kind = SpellKind.AutoHit, Dice = DiceExpr.Parse("1d4+1"), Projectiles = 3, DamageType = DamageType.Force,
                Description = "Three glowing darts that always hit, each 1d4+1 force. One more dart per slot level above 1." }, W, S);
            Add(new SpellDef { Id = "burning_hands", Name = "Burning Hands", Level = 1, School = "Evocation", RangeFt = 0, Shape = TargetShape.Cone, AreaFt = 15, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("3d6"), UpcastDice = 1, DamageType = DamageType.Fire,
                Description = "A 15-ft cone of flame. DEX save, 3d6 fire (half on a success)." }, W, S);
            Add(new SpellDef { Id = "thunderwave", Name = "Thunderwave", Level = 1, School = "Evocation", RangeFt = 0, Shape = TargetShape.Cube, AreaFt = 15, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("2d8"), UpcastDice = 1, DamageType = DamageType.Thunder, EffectId = "push10",
                Description = "A wave of thunder. CON save, 2d8 thunder and pushed 10 ft (half and no push on a success)." }, W, B, D, S);
            Add(new SpellDef { Id = "shield", Name = "Shield", Level = 1, School = "Abjuration", CastTime = CastTime.Reaction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "shield",
                Description = "Reaction when hit: +5 AC until the start of your next turn, including against the triggering attack." }, W, S);
            Add(new SpellDef { Id = "mage_armor", Name = "Mage Armor", Level = 1, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "mage_armor",
                Description = "Your base AC becomes 13 + DEX for 8 hours (if not wearing armor)." }, W);
            Add(new SpellDef { Id = "sleep", Name = "Sleep", Level = 1, School = "Enchantment", RangeFt = 60, Shape = TargetShape.Sphere, AreaFt = 5, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "laughter",
                Description = "Creatures in a 5-ft sphere make a WIS save or become Incapacitated (then fall asleep). Concentration." }, W, B, S);
            Add(new SpellDef { Id = "charm_person", Name = "Charm Person", Level = 1, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.WIS, EffectId = "charmed",
                Description = "WIS save (with Advantage if you're fighting it) or the humanoid is Charmed for 1 hour. In conversation: Advantage on CHA checks with them." }, W, B, D, S, K);
            // ---- druid & ranger
            Add(new SpellDef { Id = "druidcraft", Name = "Druidcraft", Level = 0, School = "Transmutation", RangeFt = 30, Kind = SpellKind.Utility,
                Description = "Tiny nature magic: predict the weather, make a flower bloom, light or snuff a small flame." }, D);
            Add(new SpellDef { Id = "produce_flame", Name = "Produce Flame", Level = 0, School = "Conjuration", RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Fire,
                Description = "A flame flickers in your palm (it lights the way), and you can hurl it: ranged spell attack, 1d8 fire." }, D);
            Add(new SpellDef { Id = "shillelagh", Name = "Shillelagh", Level = 0, School = "Transmutation", CastTime = CastTime.BonusAction, RangeFt = 0, Shape = TargetShape.Self, Kind = SpellKind.Buff, EffectId = "shillelagh",
                Description = "Bonus Action: your club or quarterstaff uses WIS for attacks and damage and deals a d8." }, D);
            Add(new SpellDef { Id = "thorn_whip", Name = "Thorn Whip", Level = 0, School = "Transmutation", RangeFt = 30, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d6"), DamageType = DamageType.Piercing,
                Description = "A vine whip lashes out 30 ft: melee spell attack, 1d6 piercing." }, D);
            Add(new SpellDef { Id = "entangle", Name = "Entangle", Level = 1, School = "Conjuration", RangeFt = 90, Shape = TargetShape.Cube, AreaFt = 20, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.STR, Concentration = true, EffectId = "entangle",
                Description = "Grasping vines fill a 20-ft square: STR save or Restrained (they can try again each turn). Concentration." }, D);
            Add(new SpellDef { Id = "fog_cloud", Name = "Fog Cloud", Level = 1, School = "Conjuration", RangeFt = 120, Kind = SpellKind.Utility, Concentration = true,
                Description = "A 20-ft sphere of thick fog. Heavily obscured. Concentration, 1 hour." }, D, R, W);
            Add(new SpellDef { Id = "goodberry", Name = "Goodberry", Level = 1, School = "Conjuration", RangeFt = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("1d1"),
                Description = "Ten magic berries; each restores 1 HP and is a full day's food." }, D, R);
            Add(new SpellDef { Id = "longstrider", Name = "Longstrider", Level = 1, School = "Transmutation", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "longstrider",
                Description = "Touch: +10 ft Speed for an hour." }, D, R, B, W);
            Add(new SpellDef { Id = "speak_with_animals", Name = "Speak with Animals", Level = 1, School = "Divination", RangeFt = 0, Kind = SpellKind.Utility, Ritual = true,
                Description = "Talk with beasts for 10 minutes. Ritual." }, D, R, B);
            Add(new SpellDef { Id = "hunters_mark", Name = "Hunter's Mark", Level = 1, School = "Divination", CastTime = CastTime.BonusAction, RangeFt = 90, Kind = SpellKind.Mark, Concentration = true, EffectId = "hunters_mark",
                Description = "Bonus Action: mark a creature. Your weapon hits deal +1d6 force to it. Concentration, 1 hour." }, R);
            Add(new SpellDef { Id = "barkskin", Name = "Barkskin", Level = 2, School = "Transmutation", CastTime = CastTime.BonusAction, RangeFt = 5, Kind = SpellKind.Buff, EffectId = "barkskin",
                Description = "Bonus Action, touch: the target's skin turns bark-hard. AC can't be lower than 17 for an hour." }, D, R);
            Add(new SpellDef { Id = "moonbeam", Name = "Moonbeam", Level = 2, School = "Evocation", RangeFt = 120, Shape = TargetShape.Sphere, AreaFt = 5, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("2d10"), UpcastDice = 1, DamageType = DamageType.Radiant, Concentration = true,
                Description = "A silver beam of moonlight: CON save, 2d10 radiant (half on a success). Concentration." }, D);
            Add(new SpellDef { Id = "flaming_sphere", Name = "Flaming Sphere", Level = 2, School = "Conjuration", RangeFt = 60, Shape = TargetShape.Sphere, AreaFt = 5, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("2d6"), UpcastDice = 1, DamageType = DamageType.Fire, Concentration = true,
                Description = "A ball of fire rolls where you point: DEX save, 2d6 fire (half on a success). Concentration." }, D, W);
            // ---- sorcerer & warlock
            Add(new SpellDef { Id = "eldritch_blast", Name = "Eldritch Blast", Level = 0, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d10"), DamageType = DamageType.Force,
                Description = "A crackling beam of ember-light: ranged spell attack, 1d10 force." }, K);
            Add(new SpellDef { Id = "hex", Name = "Hex", Level = 1, School = "Enchantment", CastTime = CastTime.BonusAction, RangeFt = 90, Kind = SpellKind.Mark, Concentration = true, EffectId = "hex",
                Description = "Bonus Action: curse a creature. Your attack hits deal +1d6 necrotic to it. Concentration, 1 hour." }, K);
            Add(new SpellDef { Id = "armor_of_agathys", Name = "Armor of Agathys", Level = 1, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 0, Shape = TargetShape.Self, Kind = SpellKind.Buff, EffectId = "armor_of_agathys",
                Description = "Bonus Action: frost armor. 5 temporary HP; while it lasts, anything that hits you in melee takes 5 cold damage." }, K);
            Add(new SpellDef { Id = "chromatic_orb", Name = "Chromatic Orb", Level = 1, School = "Evocation", RangeFt = 90, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("3d8"), UpcastDice = 1, DamageType = DamageType.Fire,
                Description = "Hurl an orb of elemental energy (fire, here): ranged spell attack, 3d8." }, S, W);
            Add(new SpellDef { Id = "witch_bolt", Name = "Witch Bolt", Level = 1, School = "Evocation", RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("2d12"), UpcastDice = 1, DamageType = DamageType.Lightning, Concentration = true,
                Description = "A beam of crackling energy: ranged spell attack, 2d12 lightning." }, S, K, W);
            Add(new SpellDef { Id = "alter_self", Name = "Alter Self", Level = 2, School = "Transmutation", RangeFt = 0, Kind = SpellKind.Utility, Concentration = true,
                Description = "Change your appearance, grow gills, or sprout natural weapons. Concentration, 1 hour." }, S, W);
            Add(new SpellDef { Id = "dragons_breath", Name = "Dragon's Breath", Level = 2, School = "Transmutation", RangeFt = 0, Shape = TargetShape.Cone, AreaFt = 15, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("3d6"), UpcastDice = 1, DamageType = DamageType.Fire,
                Description = "Breathe fire like your ancestor: 15-ft cone, DEX save, 3d6 fire (half on a success)." }, S, W);
            Add(new SpellDef { Id = "mirror_image", Name = "Mirror Image", Level = 2, School = "Illusion", RangeFt = 0, Shape = TargetShape.Self, Kind = SpellKind.Buff, EffectId = "mirror_image",
                Description = "Three illusory duplicates: attackers often hit an image instead (+3 AC while they last, in this game)." }, S, K, W, B);
            Add(new SpellDef { Id = "divine_smite", Name = "Divine Smite", Level = 1, School = "Evocation", CastTime = CastTime.BonusAction, RangeFt = 5, Kind = SpellKind.Utility,
                Dice = DiceExpr.Parse("2d8"), UpcastDice = 1, DamageType = DamageType.Radiant,
                Description = "Bonus Action, right after you hit with a melee weapon: +2d8 radiant (+1d8 per slot level above 1st)." }, P);
            Add(new SpellDef { Id = "protection_from_evil_and_good", Name = "Protection from Evil and Good", Level = 1, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "protection_evil_good",
                Description = "Touch: aberrations, celestials, elementals, fey, fiends and undead have Disadvantage attacking the target. Concentration, 10 minutes." }, C, P, W, K);
            Add(new SpellDef { Id = "detect_magic", Name = "Detect Magic", Level = 1, School = "Divination", RangeFt = 0, Kind = SpellKind.Utility, Ritual = true, Concentration = true,
                Description = "Sense magic within 30 ft. Ritual." }, W, C, B, P, D, R, S, K);
            Add(new SpellDef { Id = "cure_wounds", Name = "Cure Wounds", Level = 1, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d8"), AddSpellMod = true, UpcastDice = 2,
                Description = "Touch: regain 2d8 + your spellcasting modifier HP." }, C, B, P, D, R);
            Add(new SpellDef { Id = "healing_word", Name = "Healing Word", Level = 1, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d4"), AddSpellMod = true, UpcastDice = 2,
                Description = "Bonus Action, 60 ft: regain 2d4 + your spellcasting modifier HP. The classic 'get back up!' spell." }, C, B, D);
            Add(new SpellDef { Id = "bless", Name = "Bless", Level = 1, School = "Enchantment", RangeFt = 30, MaxTargets = 3, Kind = SpellKind.Buff, Concentration = true, EffectId = "bless",
                Description = "Up to 3 allies add +1d4 to attack rolls and saving throws. Concentration, 1 minute." }, C, P);
            Add(new SpellDef { Id = "guiding_bolt", Name = "Guiding Bolt", Level = 1, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("4d6"), UpcastDice = 1, DamageType = DamageType.Radiant, EffectId = "guiding_bolt",
                Description = "Ranged spell attack, 4d6 radiant; the next attack against the target has Advantage." }, C);
            Add(new SpellDef { Id = "inflict_wounds", Name = "Inflict Wounds", Level = 1, School = "Necromancy", RangeFt = 5, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("2d10"), UpcastDice = 1, DamageType = DamageType.Necrotic,
                Description = "Touch: CON save, 2d10 necrotic (half on a success)." }, C);
            Add(new SpellDef { Id = "shield_of_faith", Name = "Shield of Faith", Level = 1, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Buff, Concentration = true, EffectId = "shield_of_faith",
                Description = "Bonus Action: an ally gains +2 AC. Concentration, 10 minutes." }, C, P);
            Add(new SpellDef { Id = "command", Name = "Command", Level = 1, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, EffectId = "prone",
                Description = "One-word command. WIS save or obey. In combat: 'Grovel!' knocks the target Prone." }, C, B, P);
            Add(new SpellDef { Id = "dissonant_whispers", Name = "Dissonant Whispers", Level = 1, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, HalfOnSave = true, Dice = DiceExpr.Parse("3d6"), UpcastDice = 1, DamageType = DamageType.Psychic, EffectId = "frightened",
                Description = "A discordant melody only the target hears. WIS save, 3d6 psychic (half on a success); on a failure it flees in terror." }, B);
            Add(new SpellDef { Id = "faerie_fire", Name = "Faerie Fire", Level = 1, School = "Evocation", RangeFt = 60, Shape = TargetShape.Cube, AreaFt = 20, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, Concentration = true, EffectId = "faerie_fire",
                Description = "Creatures in a 20-ft cube make a DEX save or are outlined in light: attacks against them have Advantage." }, B, D);
            Add(new SpellDef { Id = "heroism", Name = "Heroism", Level = 1, School = "Enchantment", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "heroism",
                Description = "An ally is immune to Frightened and gains temporary HP equal to your spellcasting modifier each turn." }, B, P);
            Add(new SpellDef { Id = "hideous_laughter", Name = "Hideous Laughter", Level = 1, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "laughter",
                Description = "WIS save or the target collapses laughing (Prone, Incapacitated). Repeats the save at the end of each turn." }, W, B);
            Add(new SpellDef { Id = "comprehend_languages", Name = "Comprehend Languages", Level = 1, School = "Divination", RangeFt = 0, Kind = SpellKind.Utility, Ritual = true,
                Description = "Understand any spoken or written language for 1 hour. (Kobold diplomacy, anyone?)" }, W, B);
            Add(new SpellDef { Id = "disguise_self", Name = "Disguise Self", Level = 1, School = "Illusion", RangeFt = 0, Kind = SpellKind.Utility,
                Description = "Change your appearance for 1 hour. Advantage on Deception checks to pass as someone else." }, W, B);

            // ---------------- Level 2
            Add(new SpellDef { Id = "scorching_ray", Name = "Scorching Ray", Level = 2, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Projectiles = 3, Dice = DiceExpr.Parse("2d6"), DamageType = DamageType.Fire,
                Description = "Three rays of fire. Each is a ranged spell attack for 2d6 fire." }, W, S);
            Add(new SpellDef { Id = "misty_step", Name = "Misty Step", Level = 2, School = "Conjuration", CastTime = CastTime.BonusAction, RangeFt = 30, Kind = SpellKind.Utility, EffectId = "teleport30",
                Description = "Bonus Action: teleport up to 30 ft to a space you can see." }, W, S, K);
            Add(new SpellDef { Id = "hold_person", Name = "Hold Person", Level = 2, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "hold_person",
                Description = "A humanoid makes a WIS save or is Paralyzed. It repeats the save at the end of each of its turns." }, W, C, B, D, S, K);
            Add(new SpellDef { Id = "shatter", Name = "Shatter", Level = 2, School = "Evocation", RangeFt = 60, Shape = TargetShape.Sphere, AreaFt = 10, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("3d8"), UpcastDice = 1, DamageType = DamageType.Thunder,
                Description = "A painfully loud ringing in a 10-ft radius. CON save, 3d8 thunder (half on a success)." }, W, B, S);
            Add(new SpellDef { Id = "spiritual_weapon", Name = "Spiritual Weapon", Level = 2, School = "Evocation", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), AddSpellMod = true, DamageType = DamageType.Force, Concentration = true, EffectId = "spiritual_weapon",
                Description = "Bonus Action: a floating spectral weapon makes a melee spell attack for 1d8 + modifier force. Concentration." }, C);
            Add(new SpellDef { Id = "aid", Name = "Aid", Level = 2, School = "Abjuration", RangeFt = 30, MaxTargets = 3, Kind = SpellKind.Buff, EffectId = "aid",
                Description = "Up to 3 allies gain +5 max HP and current HP for 8 hours." }, C, B, P, R);
            Add(new SpellDef { Id = "prayer_of_healing", Name = "Prayer of Healing", Level = 2, School = "Abjuration", CastTime = CastTime.TenMinutes, RangeFt = 30, MaxTargets = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d8"), AddSpellMod = true,
                Description = "Out of combat: up to 5 allies regain 2d8 + modifier HP and get the benefit of a Short Rest." }, C);
            Add(new SpellDef { Id = "lesser_restoration", Name = "Lesser Restoration", Level = 2, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Utility, EffectId = "cleanse",
                Description = "End one condition: Blinded, Deafened, Paralyzed or Poisoned." }, C, B, D, P, R);
            Add(new SpellDef { Id = "suggestion", Name = "Suggestion", Level = 2, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Utility, Concentration = true,
                Description = "Suggest a reasonable course of action. In conversation: a guaranteed success on one reasonable request (WIS save negates)." }, W, B, S, K);
            Add(new SpellDef { Id = "invisibility", Name = "Invisibility", Level = 2, School = "Illusion", RangeFt = 5, Kind = SpellKind.Utility, Concentration = true,
                Description = "A creature becomes Invisible until it attacks or casts a spell." }, W, B, S, K);

            // ---------------- Level 3 (Act II; a curated list: see docs/DESIGN.md)
            Add(new SpellDef { Id = "revivify", Name = "Revivify", Level = 3, School = "Necromancy", RangeFt = 5, Kind = SpellKind.Utility,
                Description = "Return a creature that died within the last minute to life with 1 HP." }, C, P);
            Add(new SpellDef { Id = "fireball", Name = "Fireball", Level = 3, School = "Evocation", RangeFt = 150, Kind = SpellKind.Save, Shape = TargetShape.Sphere, AreaFt = 20,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("8d6"), DamageType = DamageType.Fire, UpcastDice = 1,
                Description = "A bead of fire blossoms into a 20-ft-radius explosion. DEX save: 8d6 fire, half on a success. Careful: it doesn't care whose side you're on." }, W, S);
            Add(new SpellDef { Id = "lightning_bolt", Name = "Lightning Bolt", Level = 3, School = "Evocation", RangeFt = 0, Kind = SpellKind.Save, Shape = TargetShape.Line, AreaFt = 100,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("8d6"), DamageType = DamageType.Lightning, UpcastDice = 1,
                Description = "A 100-ft line of lightning from your hand. DEX save: 8d6 lightning, half on a success." }, W, S);
            Add(new SpellDef { Id = "spirit_guardians", Name = "Spirit Guardians", Level = 3, School = "Conjuration", RangeFt = 0, Kind = SpellKind.Buff, Concentration = true,
                SaveAbility = Ability.WIS, HalfOnSave = true, Dice = DiceExpr.Parse("3d8"), DamageType = DamageType.Radiant, UpcastDice = 1, ZoneRadiusTiles = 3,
                Description = "Protective spirits circle you in a 15-ft aura. Enemies that enter it or end their turn there: WIS save, 3d8 radiant (half on a success), and their Speed is halved inside." }, C);
            Add(new SpellDef { Id = "mass_healing_word", Name = "Mass Healing Word", Level = 3, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 60, MaxTargets = 6, AreaFt = 60,
                Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d4"), AddSpellMod = true, UpcastDice = 1,
                Description = "Bonus Action: up to 6 allies you can see within 60 ft regain 2d4 + your modifier HP." }, C, B);
            Add(new SpellDef { Id = "beacon_of_hope", Name = "Beacon of Hope", Level = 3, School = "Abjuration", RangeFt = 30, MaxTargets = 6, Kind = SpellKind.Buff, Concentration = true, EffectId = "beacon_of_hope",
                Description = "Up to 6 allies within 30 ft: Advantage on WIS saves and death saves, and any healing they receive is maximized." }, C);
            Add(new SpellDef { Id = "haste", Name = "Haste", Level = 3, School = "Transmutation", RangeFt = 30, Kind = SpellKind.Buff, Concentration = true, EffectId = "haste",
                Description = "One ally: +2 AC, double Speed, Advantage on DEX saves, and an extra attack each turn. When it ends they're lethargic for a turn." }, W, S);
            Add(new SpellDef { Id = "slow", Name = "Slow", Level = 3, School = "Transmutation", RangeFt = 120, Kind = SpellKind.Save, Shape = TargetShape.Cube, AreaFt = 20, MaxTargets = 6, OnlyEnemies = true,
                SaveAbility = Ability.WIS, Concentration = true, EffectId = "slowed_spell",
                Description = "Up to 6 enemies in a 40-ft cube: WIS save or −2 AC, half Speed and no reactions. They repeat the save each turn." }, W, S);
            Add(new SpellDef { Id = "hypnotic_pattern", Name = "Hypnotic Pattern", Level = 3, School = "Illusion", RangeFt = 120, Kind = SpellKind.Save, Shape = TargetShape.Cube, AreaFt = 15,
                SaveAbility = Ability.WIS, Concentration = true, EffectId = "hypnotized",
                Description = "A twisting pattern of colours in a 30-ft cube. WIS save or entranced (Incapacitated) until it takes damage. Friends aren't immune." }, W, S, B, K);
            Add(new SpellDef { Id = "call_lightning", Name = "Call Lightning", Level = 3, School = "Conjuration", RangeFt = 120, Kind = SpellKind.Save, Shape = TargetShape.Cylinder, AreaFt = 5,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("3d10"), DamageType = DamageType.Lightning, UpcastDice = 1, Concentration = true,
                Description = "A storm cloud gathers. Call a bolt on a 5-ft radius: DEX save, 3d10 lightning (half on a success). Each later turn you can call another with your action, no slot needed." }, D);
            Add(new SpellDef { Id = "sleet_storm", Name = "Sleet Storm", Level = 3, School = "Conjuration", RangeFt = 150, Kind = SpellKind.Save, Shape = TargetShape.Cylinder, AreaFt = 20,
                SaveAbility = Ability.DEX, EffectId = "prone",
                Description = "Freezing rain and sleet in a 20-ft radius. DEX save or fall Prone." }, D, W, S);
            Add(new SpellDef { Id = "fly", Name = "Fly", Level = 3, School = "Transmutation", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "flying",
                Description = "Touch an ally: they fly (Speed +30 ft) for 10 minutes." }, W, S, K);
            Add(new SpellDef { Id = "blinding_smite", Name = "Blinding Smite", Level = 3, School = "Evocation", CastTime = CastTime.BonusAction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "blinding_smite_ready",
                Description = "Your next melee weapon hit deals +3d8 radiant and Blinds the target (CON save at the end of each of its turns)." }, P);
            Add(new SpellDef { Id = "lightning_arrow", Name = "Lightning Arrow", Level = 3, School = "Transmutation", CastTime = CastTime.BonusAction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "lightning_arrow_ready",
                Description = "Your next ranged weapon attack becomes lightning: 4d8 on a hit (half on a miss), and 2d8 to everyone within 10 ft of the target (DEX save for half)." }, R);
            Add(new SpellDef { Id = "conjure_barrage", Name = "Conjure Barrage", Level = 3, School = "Conjuration", RangeFt = 0, Kind = SpellKind.Save, Shape = TargetShape.Cone, AreaFt = 60,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("5d8"), DamageType = DamageType.Force, UpcastDice = 1,
                Description = "Throw a weapon and it becomes a hail of copies: a 60-ft cone, DEX save, 5d8 force (half on a success)." }, R);

            // ---------------- Level 4
            Add(new SpellDef { Id = "greater_invisibility", Name = "Greater Invisibility", Level = 4, School = "Illusion", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "greater_invisibility",
                Description = "Touch an ally: Invisible for a minute, even while attacking or casting." }, W, S, B);
            Add(new SpellDef { Id = "ice_storm", Name = "Ice Storm", Level = 4, School = "Evocation", RangeFt = 300, Kind = SpellKind.Save, Shape = TargetShape.Cylinder, AreaFt = 20,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("2d10"), DamageType = DamageType.Bludgeoning, UpcastDice = 1,
                SecondDice = DiceExpr.Parse("4d6"), SecondDamageType = DamageType.Cold,
                Description = "Hailstones pound a 20-ft radius. DEX save: 2d10 bludgeoning + 4d6 cold, half on a success." }, W, S, D);
            Add(new SpellDef { Id = "wall_of_fire", Name = "Wall of Fire", Level = 4, School = "Evocation", RangeFt = 120, Kind = SpellKind.Save, Shape = TargetShape.Line, AreaFt = 60,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("5d8"), DamageType = DamageType.Fire, UpcastDice = 1, Concentration = true, ZoneLineTiles = 12,
                Description = "A 60-ft wall of flame across the battlefield. Anyone caught in it (or who ends a turn in it, or walks through it) makes a DEX save: 5d8 fire, half on a success." }, W, S, D);
            Add(new SpellDef { Id = "banishment", Name = "Banishment", Level = 4, School = "Abjuration", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.CHA, Concentration = true, EffectId = "banished",
                Description = "CHA save or the target vanishes to a harmless elsewhere while you concentrate. Hold it for the full minute and it doesn't come back." }, W, S, C, K, P);
            Add(new SpellDef { Id = "death_ward", Name = "Death Ward", Level = 4, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "death_ward",
                Description = "Touch an ally: the first time they would drop to 0 HP in the next 8 hours, they drop to 1 instead." }, C, P);
            Add(new SpellDef { Id = "staggering_smite", Name = "Staggering Smite", Level = 4, School = "Enchantment", CastTime = CastTime.BonusAction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "staggering_smite_ready",
                Description = "Your next melee weapon hit deals +4d6 psychic; on a failed WIS save the target can't take reactions and has Disadvantage on its next attack." }, P);
            Add(new SpellDef { Id = "dimension_door", Name = "Dimension Door", Level = 4, School = "Conjuration", RangeFt = 500, Kind = SpellKind.Utility,
                Description = "Step through space to any spot you can see, taking one adjacent ally with you." }, W, S, B, K);
            Add(new SpellDef { Id = "guardian_of_faith", Name = "Guardian of Faith", Level = 4, School = "Conjuration", RangeFt = 30, Kind = SpellKind.Utility,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = new DiceExpr(0, 0, 20), DamageType = DamageType.Radiant, ZoneRadiusTiles = 2,
                Description = "A spectral guardian holds a spot for 8 hours. Enemies that enter within 10 ft of it or end their turn there: DEX save, 20 radiant (half on a success). It fades after dealing 60." }, C);

            Add(new SpellDef { Id = "freedom_of_movement", Name = "Freedom of Movement", Level = 4, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "freedom_of_movement",
                Description = "Touch an ally: for an hour they can't be Paralyzed or Restrained, and nothing can reduce their Speed." }, B, C, D, R);
            Add(new SpellDef { Id = "stoneskin", Name = "Stoneskin", Level = 4, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "stoneskin",
                Description = "Touch an ally: their skin turns hard as stone. Resistance to bludgeoning, piercing and slashing damage while you concentrate." }, D, R, S, W);

            // ---------------- Level 5
            Add(new SpellDef { Id = "cone_of_cold", Name = "Cone of Cold", Level = 5, School = "Evocation", RangeFt = 0, Kind = SpellKind.Save, Shape = TargetShape.Cone, AreaFt = 60,
                SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("8d8"), DamageType = DamageType.Cold, UpcastDice = 1,
                Description = "A blast of cold air in a 60-ft cone. CON save: 8d8 cold, half on a success." }, W, S);
            Add(new SpellDef { Id = "mass_cure_wounds", Name = "Mass Cure Wounds", Level = 5, School = "Abjuration", RangeFt = 60, MaxTargets = 6, AreaFt = 30,
                Kind = SpellKind.Heal, Dice = DiceExpr.Parse("5d8"), AddSpellMod = true, UpcastDice = 1,
                Description = "A wave of healing: up to 6 allies within 30 ft of a point regain 5d8 + your modifier HP." }, C, B, D);
            Add(new SpellDef { Id = "hold_monster", Name = "Hold Monster", Level = 5, School = "Enchantment", RangeFt = 90, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "hold_person",
                Description = "WIS save or any creature is Paralyzed. It repeats the save at the end of each of its turns." }, W, S, B, K);
            Add(new SpellDef { Id = "flame_strike", Name = "Flame Strike", Level = 5, School = "Evocation", RangeFt = 60, Kind = SpellKind.Save, Shape = TargetShape.Cylinder, AreaFt = 10,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("5d6"), DamageType = DamageType.Fire, UpcastDice = 1,
                SecondDice = DiceExpr.Parse("5d6"), SecondDamageType = DamageType.Radiant,
                Description = "A column of divine fire, 10-ft radius. DEX save: 5d6 fire + 5d6 radiant, half on a success." }, C);
            Add(new SpellDef { Id = "greater_restoration", Name = "Greater Restoration", Level = 5, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "cleanse_greater",
                Description = "Touch an ally: end Charmed, Frightened, Paralyzed or Petrified on them, and remove a level of Exhaustion." }, C, B, D, P, R);
            Add(new SpellDef { Id = "banishing_smite", Name = "Banishing Smite", Level = 5, School = "Conjuration", CastTime = CastTime.BonusAction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "banishing_smite_ready",
                Description = "Your next melee weapon hit deals +5d10 force; if that leaves the target at 50 HP or fewer, it's banished while you concentrate." }, P);
            Add(new SpellDef { Id = "swift_quiver", Name = "Swift Quiver", Level = 5, School = "Transmutation", CastTime = CastTime.BonusAction, RangeFt = 0, Kind = SpellKind.Buff, Concentration = true, EffectId = "swift_quiver",
                Description = "Your quiver never runs dry: each turn, a Bonus Action for two attacks with your ranged weapon." }, R);
            Add(new SpellDef { Id = "synaptic_static", Name = "Synaptic Static", Level = 5, School = "Enchantment", RangeFt = 120, Kind = SpellKind.Save, Shape = TargetShape.Sphere, AreaFt = 20,
                SaveAbility = Ability.INT, HalfOnSave = true, Dice = DiceExpr.Parse("8d6"), DamageType = DamageType.Psychic, EffectId = "synaptic",
                Description = "A burst of psychic noise, 20-ft radius. INT save: 8d6 psychic (half on a success); those who fail are muddled (−1d6 to attacks, INT save each turn to clear)." }, W, B, S, K);

            // ---------------- Level 6
            Add(new SpellDef { Id = "chain_lightning", Name = "Chain Lightning", Level = 6, School = "Evocation", RangeFt = 150, Kind = SpellKind.Save, SaveAbility = Ability.DEX, HalfOnSave = true,
                Dice = DiceExpr.Parse("10d8"), DamageType = DamageType.Lightning, ChainTargets = 3, ChainRangeFt = 30,
                Description = "A bolt strikes one creature, then leaps to up to three others within 30 ft of it. Each makes a DEX save: 10d8 lightning, half on a success. One more leap per slot level above 6th." }, S, W);
            Add(new SpellDef { Id = "disintegrate", Name = "Disintegrate", Level = 6, School = "Transmutation", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.DEX,
                Dice = DiceExpr.Parse("10d6+40"), DamageType = DamageType.Force, UpcastDice = 3,
                Description = "A thin green ray. DEX save or take 10d6 + 40 force; nothing on a success. Whatever it drops is reduced to fine grey dust." }, S, W);
            Add(new SpellDef { Id = "sunbeam", Name = "Sunbeam", Level = 6, School = "Evocation", RangeFt = 0, Kind = SpellKind.Save, Shape = TargetShape.Line, AreaFt = 60, SaveAbility = Ability.CON, HalfOnSave = true,
                Dice = DiceExpr.Parse("6d8"), DamageType = DamageType.Radiant, EffectId = "sunbeam_blind", Concentration = true, Recastable = true,
                Description = "A beam of sunlight, 60 ft long. CON save: 6d8 radiant and Blinded until your next turn, half and no blindness on a success. While you concentrate, call the beam again as an action." }, D, S, W);
            Add(new SpellDef { Id = "heal", Name = "Heal", Level = 6, School = "Abjuration", RangeFt = 60, Kind = SpellKind.Heal, Dice = new DiceExpr(0, 0, 70), UpcastFlat = 10,
                Description = "A surge of positive energy: one creature regains 70 HP and is no longer Blinded, Deafened or Poisoned. +10 HP per slot level above 6th." }, C, D);
            Add(new SpellDef { Id = "blade_barrier", Name = "Blade Barrier", Level = 6, School = "Evocation", RangeFt = 90, Kind = SpellKind.Save, Shape = TargetShape.Line, AreaFt = 100,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("6d10"), DamageType = DamageType.Force, Concentration = true, ZoneLineTiles = 20,
                Description = "A wall of whirling blades. Anyone caught in it, entering it or ending a turn in it makes a DEX save: 6d10 force, half on a success." }, C);
            Add(new SpellDef { Id = "heroes_feast", Name = "Heroes' Feast", Level = 6, School = "Conjuration", CastTime = CastTime.TenMinutes, RangeFt = 30, Kind = SpellKind.Utility,
                Description = "A magnificent feast for the whole party. Until your next Long Rest: +2d10 maximum HP (and HP), and immunity to being Poisoned or Frightened. Cast outside combat." }, B, C, D);
            Add(new SpellDef { Id = "irresistible_dance", Name = "Irresistible Dance", Level = 6, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "dancing",
                Description = "WIS save or the target dances a comic jig: it can't move, its attacks and DEX saves have Disadvantage, and attacks against it have Advantage. It repeats the save at the end of each of its turns." }, B, W);
            Add(new SpellDef { Id = "circle_of_death", Name = "Withering Circle", Level = 6, School = "Necromancy", RangeFt = 150, Kind = SpellKind.Save, Shape = TargetShape.Sphere, AreaFt = 60,
                SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("8d8"), DamageType = DamageType.Necrotic, UpcastDice = 2,
                Description = "A ring of withering grey light spreads 60 ft from a point. CON save: 8d8 necrotic, half on a success. +2d8 per slot level above 6th." }, K, S, W);
            Add(new SpellDef { Id = "eyebite", Name = "Eyebite", Level = 6, School = "Necromancy", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "frightened", Recastable = true,
                Description = "Your eyes become pits of shadow. One creature: WIS save or it panics, Frightened of you. While you concentrate, turn your gaze on another as an action." }, K, S, W);

            // ---------------- Level 7
            Add(new SpellDef { Id = "fire_storm", Name = "Fire Storm", Level = 7, School = "Evocation", RangeFt = 150, Kind = SpellKind.Save, Shape = TargetShape.Sphere, AreaFt = 20, MaxTargets = 10, OnlyEnemies = true,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("7d10"), DamageType = DamageType.Fire,
                Description = "Sheets of roaring flame shaped around your friends: every foe within 20 ft of a point (up to ten) makes a DEX save, 7d10 fire, half on a success. Allies in the area are untouched." }, C, D, S);
            Add(new SpellDef { Id = "delayed_blast_fireball", Name = "Delayed Blast Fireball", Level = 7, School = "Evocation", RangeFt = 150, Kind = SpellKind.Save, Shape = TargetShape.Sphere, AreaFt = 20,
                SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("12d6"), DamageType = DamageType.Fire, UpcastDice = 1,
                Description = "A bead of yellow light that bursts in a 20-ft radius. DEX save: 12d6 fire, half on a success." }, S, W);
            Add(new SpellDef { Id = "finger_of_death", Name = "Final Word", Level = 7, School = "Necromancy", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true,
                Dice = DiceExpr.Parse("7d8+30"), DamageType = DamageType.Necrotic,
                Description = "You speak a word that unmakes. One creature: CON save, 7d8 + 30 necrotic, half on a success." }, K, S, W);
            Add(new SpellDef { Id = "forcecage", Name = "Forcecage", Level = 7, School = "Evocation", RangeFt = 100, Kind = SpellKind.Utility,
                Description = "A cage of shimmering bars, 10 ft on a side, drops around a spot. Creatures inside can't leave (a CHA save lets one slip out by magic), though attacks pass through the bars. It lasts the whole fight. Legendary creatures can't be caged." }, B, K, W);
            Add(new SpellDef { Id = "regenerate", Name = "Regenerate", Level = 7, School = "Transmutation", RangeFt = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("4d8+15"),
                Description = "Touch an ally: they regain 4d8 + 15 HP, then 1 HP at the start of each of their turns for the rest of the fight." }, B, C, D);
            Add(new SpellDef { Id = "resurrection", Name = "Resurrection", Level = 7, School = "Necromancy", CastTime = CastTime.TenMinutes, RangeFt = 5, Kind = SpellKind.Utility,
                Description = "Bring every fallen friend in your party back to life at full health. Cast outside combat." }, B, C);
        }

        public static SpellDef Get(string id) => All.TryGetValue(id, out var s) ? s : null;

        public static List<SpellDef> ForClass(string classId, int maxLevel)
        {
            var list = new List<SpellDef>();
            foreach (var s in All.Values)
                if (s.Classes.Contains(classId) && s.Level <= maxLevel) list.Add(s);
            list.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Name, b.Name));
            return list;
        }
    }
}
