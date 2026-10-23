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

        const string W = "wizard", C = "cleric", B = "bard";

        static SpellsDb()
        {
            // ---------------- Cantrips
            Add(new SpellDef { Id = "fire_bolt", Name = "Fire Bolt", Level = 0, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d10"), DamageType = DamageType.Fire,
                Description = "Hurl a mote of fire. Ranged spell attack, 1d10 fire." }, W);
            Add(new SpellDef { Id = "ray_of_frost", Name = "Ray of Frost", Level = 0, School = "Evocation", RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Cold, EffectId = "ray_of_frost",
                Description = "A frigid beam. Ranged spell attack, 1d8 cold, and the target's Speed drops by 10 ft." }, W);
            Add(new SpellDef { Id = "shocking_grasp", Name = "Shocking Grasp", Level = 0, School = "Evocation", RangeFt = 5, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Lightning,
                Description = "Lightning springs from your hand. Melee spell attack, 1d8 lightning; the target can't take Opportunity Attacks." }, W);
            Add(new SpellDef { Id = "sacred_flame", Name = "Sacred Flame", Level = 0, School = "Evocation", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.DEX, Dice = DiceExpr.Parse("1d8"), DamageType = DamageType.Radiant,
                Description = "Flame-like radiance descends. DEX save or 1d8 radiant. Ignores cover." }, C);
            Add(new SpellDef { Id = "vicious_mockery", Name = "Vicious Mockery", Level = 0, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Dice = DiceExpr.Parse("1d6"), DamageType = DamageType.Psychic, EffectId = "vicious_mockery",
                Description = "Unleash a string of magical insults. WIS save or 1d6 psychic and Disadvantage on its next attack." }, B);
            Add(new SpellDef { Id = "guidance", Name = "Guidance", Level = 0, School = "Divination", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "guidance",
                Description = "Touch an ally: their next ability check gets +1d4." }, C, B);
            Add(new SpellDef { Id = "light", Name = "Light", Level = 0, School = "Evocation", RangeFt = 5, Kind = SpellKind.Utility, EffectId = "light",
                Description = "An object sheds bright light in a 20-ft radius for 1 hour. Essential in dark dungeons." }, W, C, B);
            Add(new SpellDef { Id = "spare_the_dying", Name = "Spare the Dying", Level = 0, School = "Necromancy", RangeFt = 15, Kind = SpellKind.Utility, EffectId = "stabilize",
                Description = "Stabilize a creature at 0 HP." }, C);
            Add(new SpellDef { Id = "thaumaturgy", Name = "Thaumaturgy", Level = 0, School = "Transmutation", RangeFt = 30, Kind = SpellKind.Utility,
                Description = "Minor wonders: booming voice, flickering flames, tremors. Grants Advantage on an Intimidation check in conversation." }, C);
            Add(new SpellDef { Id = "minor_illusion", Name = "Minor Illusion", Level = 0, School = "Illusion", RangeFt = 30, Kind = SpellKind.Utility,
                Description = "Create a sound or an image. Useful for distractions (Advantage on a Deception or Stealth check)." }, W, B);
            Add(new SpellDef { Id = "prestidigitation", Name = "Prestidigitation", Level = 0, School = "Transmutation", RangeFt = 10, Kind = SpellKind.Utility,
                Description = "Harmless magical tricks. Clean, chill, flavor, light candles. Very good at impressing children and drunk dwarves." }, W, B);

            // ---------------- Level 1
            Add(new SpellDef { Id = "magic_missile", Name = "Magic Missile", Level = 1, School = "Evocation", RangeFt = 120, Kind = SpellKind.AutoHit, Dice = DiceExpr.Parse("1d4+1"), Projectiles = 3, DamageType = DamageType.Force,
                Description = "Three glowing darts that always hit, each 1d4+1 force. One more dart per slot level above 1." }, W);
            Add(new SpellDef { Id = "burning_hands", Name = "Burning Hands", Level = 1, School = "Evocation", RangeFt = 0, Shape = TargetShape.Cone, AreaFt = 15, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, HalfOnSave = true, Dice = DiceExpr.Parse("3d6"), UpcastDice = 1, DamageType = DamageType.Fire,
                Description = "A 15-ft cone of flame. DEX save, 3d6 fire (half on a success)." }, W);
            Add(new SpellDef { Id = "thunderwave", Name = "Thunderwave", Level = 1, School = "Evocation", RangeFt = 0, Shape = TargetShape.Cube, AreaFt = 15, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("2d8"), UpcastDice = 1, DamageType = DamageType.Thunder, EffectId = "push10",
                Description = "A wave of thunder. CON save, 2d8 thunder and pushed 10 ft (half and no push on a success)." }, W, B);
            Add(new SpellDef { Id = "shield", Name = "Shield", Level = 1, School = "Abjuration", CastTime = CastTime.Reaction, RangeFt = 0, Kind = SpellKind.Buff, EffectId = "shield",
                Description = "Reaction when hit: +5 AC until the start of your next turn, including against the triggering attack." }, W);
            Add(new SpellDef { Id = "mage_armor", Name = "Mage Armor", Level = 1, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Buff, EffectId = "mage_armor",
                Description = "Your base AC becomes 13 + DEX for 8 hours (if not wearing armor)." }, W);
            Add(new SpellDef { Id = "sleep", Name = "Sleep", Level = 1, School = "Enchantment", RangeFt = 60, Shape = TargetShape.Sphere, AreaFt = 5, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "laughter",
                Description = "Creatures in a 5-ft sphere make a WIS save or become Incapacitated (then fall asleep). Concentration." }, W, B);
            Add(new SpellDef { Id = "charm_person", Name = "Charm Person", Level = 1, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.WIS, EffectId = "charmed",
                Description = "WIS save (with Advantage if you're fighting it) or the humanoid is Charmed for 1 hour. In conversation: Advantage on CHA checks with them." }, W, B);
            Add(new SpellDef { Id = "detect_magic", Name = "Detect Magic", Level = 1, School = "Divination", RangeFt = 0, Kind = SpellKind.Utility, Ritual = true, Concentration = true,
                Description = "Sense magic within 30 ft. Ritual." }, W, C, B);
            Add(new SpellDef { Id = "cure_wounds", Name = "Cure Wounds", Level = 1, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d8"), AddSpellMod = true, UpcastDice = 2,
                Description = "Touch: regain 2d8 + your spellcasting modifier HP." }, C, B);
            Add(new SpellDef { Id = "healing_word", Name = "Healing Word", Level = 1, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d4"), AddSpellMod = true, UpcastDice = 2,
                Description = "Bonus Action, 60 ft: regain 2d4 + your spellcasting modifier HP. The classic 'get back up!' spell." }, C, B);
            Add(new SpellDef { Id = "bless", Name = "Bless", Level = 1, School = "Enchantment", RangeFt = 30, MaxTargets = 3, Kind = SpellKind.Buff, Concentration = true, EffectId = "bless",
                Description = "Up to 3 allies add +1d4 to attack rolls and saving throws. Concentration, 1 minute." }, C);
            Add(new SpellDef { Id = "guiding_bolt", Name = "Guiding Bolt", Level = 1, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("4d6"), UpcastDice = 1, DamageType = DamageType.Radiant, EffectId = "guiding_bolt",
                Description = "Ranged spell attack, 4d6 radiant; the next attack against the target has Advantage." }, C);
            Add(new SpellDef { Id = "inflict_wounds", Name = "Inflict Wounds", Level = 1, School = "Necromancy", RangeFt = 5, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("2d10"), UpcastDice = 1, DamageType = DamageType.Necrotic,
                Description = "Touch: CON save, 2d10 necrotic (half on a success)." }, C);
            Add(new SpellDef { Id = "shield_of_faith", Name = "Shield of Faith", Level = 1, School = "Abjuration", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Buff, Concentration = true, EffectId = "shield_of_faith",
                Description = "Bonus Action: an ally gains +2 AC. Concentration, 10 minutes." }, C);
            Add(new SpellDef { Id = "command", Name = "Command", Level = 1, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, EffectId = "prone",
                Description = "One-word command. WIS save or obey. In combat: 'Grovel!' knocks the target Prone." }, C, B);
            Add(new SpellDef { Id = "dissonant_whispers", Name = "Dissonant Whispers", Level = 1, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, HalfOnSave = true, Dice = DiceExpr.Parse("3d6"), UpcastDice = 1, DamageType = DamageType.Psychic, EffectId = "frightened",
                Description = "A discordant melody only the target hears. WIS save, 3d6 psychic (half on a success); on a failure it flees in terror." }, B);
            Add(new SpellDef { Id = "faerie_fire", Name = "Faerie Fire", Level = 1, School = "Evocation", RangeFt = 60, Shape = TargetShape.Cube, AreaFt = 20, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.DEX, Concentration = true, EffectId = "faerie_fire",
                Description = "Creatures in a 20-ft cube make a DEX save or are outlined in light: attacks against them have Advantage." }, B);
            Add(new SpellDef { Id = "heroism", Name = "Heroism", Level = 1, School = "Enchantment", RangeFt = 5, Kind = SpellKind.Buff, Concentration = true, EffectId = "heroism",
                Description = "An ally is immune to Frightened and gains temporary HP equal to your spellcasting modifier each turn." }, B);
            Add(new SpellDef { Id = "hideous_laughter", Name = "Hideous Laughter", Level = 1, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "laughter",
                Description = "WIS save or the target collapses laughing (Prone, Incapacitated). Repeats the save at the end of each turn." }, W, B);
            Add(new SpellDef { Id = "comprehend_languages", Name = "Comprehend Languages", Level = 1, School = "Divination", RangeFt = 0, Kind = SpellKind.Utility, Ritual = true,
                Description = "Understand any spoken or written language for 1 hour. (Kobold diplomacy, anyone?)" }, W, B);
            Add(new SpellDef { Id = "disguise_self", Name = "Disguise Self", Level = 1, School = "Illusion", RangeFt = 0, Kind = SpellKind.Utility,
                Description = "Change your appearance for 1 hour. Advantage on Deception checks to pass as someone else." }, W, B);

            // ---------------- Level 2
            Add(new SpellDef { Id = "scorching_ray", Name = "Scorching Ray", Level = 2, School = "Evocation", RangeFt = 120, Kind = SpellKind.Attack, Projectiles = 3, Dice = DiceExpr.Parse("2d6"), DamageType = DamageType.Fire,
                Description = "Three rays of fire. Each is a ranged spell attack for 2d6 fire." }, W);
            Add(new SpellDef { Id = "misty_step", Name = "Misty Step", Level = 2, School = "Conjuration", CastTime = CastTime.BonusAction, RangeFt = 30, Kind = SpellKind.Utility, EffectId = "teleport30",
                Description = "Bonus Action: teleport up to 30 ft to a space you can see." }, W);
            Add(new SpellDef { Id = "hold_person", Name = "Hold Person", Level = 2, School = "Enchantment", RangeFt = 60, Kind = SpellKind.Save, SaveAbility = Ability.WIS, Concentration = true, EffectId = "hold_person",
                Description = "A humanoid makes a WIS save or is Paralyzed. It repeats the save at the end of each of its turns." }, W, C, B);
            Add(new SpellDef { Id = "shatter", Name = "Shatter", Level = 2, School = "Evocation", RangeFt = 60, Shape = TargetShape.Sphere, AreaFt = 10, MaxTargets = 99, Kind = SpellKind.Save, SaveAbility = Ability.CON, HalfOnSave = true, Dice = DiceExpr.Parse("3d8"), UpcastDice = 1, DamageType = DamageType.Thunder,
                Description = "A painfully loud ringing in a 10-ft radius. CON save, 3d8 thunder (half on a success)." }, W, B);
            Add(new SpellDef { Id = "spiritual_weapon", Name = "Spiritual Weapon", Level = 2, School = "Evocation", CastTime = CastTime.BonusAction, RangeFt = 60, Kind = SpellKind.Attack, Dice = DiceExpr.Parse("1d8"), AddSpellMod = true, DamageType = DamageType.Force, Concentration = true, EffectId = "spiritual_weapon",
                Description = "Bonus Action: a floating spectral weapon makes a melee spell attack for 1d8 + modifier force. Concentration." }, C);
            Add(new SpellDef { Id = "aid", Name = "Aid", Level = 2, School = "Abjuration", RangeFt = 30, MaxTargets = 3, Kind = SpellKind.Buff, EffectId = "aid",
                Description = "Up to 3 allies gain +5 max HP and current HP for 8 hours." }, C, B);
            Add(new SpellDef { Id = "prayer_of_healing", Name = "Prayer of Healing", Level = 2, School = "Abjuration", CastTime = CastTime.TenMinutes, RangeFt = 30, MaxTargets = 5, Kind = SpellKind.Heal, Dice = DiceExpr.Parse("2d8"), AddSpellMod = true,
                Description = "Out of combat: up to 5 allies regain 2d8 + modifier HP and get the benefit of a Short Rest." }, C);
            Add(new SpellDef { Id = "lesser_restoration", Name = "Lesser Restoration", Level = 2, School = "Abjuration", RangeFt = 5, Kind = SpellKind.Utility, EffectId = "cleanse",
                Description = "End one condition: Blinded, Deafened, Paralyzed or Poisoned." }, C, B);
            Add(new SpellDef { Id = "suggestion", Name = "Suggestion", Level = 2, School = "Enchantment", RangeFt = 30, Kind = SpellKind.Utility, Concentration = true,
                Description = "Suggest a reasonable course of action. In conversation: a guaranteed success on one reasonable request (WIS save negates)." }, W, B);
            Add(new SpellDef { Id = "invisibility", Name = "Invisibility", Level = 2, School = "Illusion", RangeFt = 5, Kind = SpellKind.Utility, Concentration = true,
                Description = "A creature becomes Invisible until it attacks or casts a spell." }, W, B);

            // Not castable by the playtest classes; used by the scroll.
            Add(new SpellDef { Id = "revivify", Name = "Revivify", Level = 3, School = "Necromancy", RangeFt = 5, Kind = SpellKind.Utility,
                Description = "Return a creature that died within the last minute to life with 1 HP." }, C);
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
