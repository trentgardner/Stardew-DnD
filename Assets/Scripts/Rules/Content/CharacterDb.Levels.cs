namespace DnD.Rules.Content
{
    /// <summary>Class and subclass features for levels 5-10 (Act II), SRD 5.2. Names are what the code checks with HasFeature.</summary>
    public static partial class CharacterDb
    {
        static void F(string cls, int level, string name, string text) => Classes[cls].Features.Add(new FeatureDef(level, name, text));
        static void S(string cls, int level, string name, string text) => Classes[cls].Subclass.Features.Add(new FeatureDef(level, name, text));

        static void Asi(string cls, params int[] levels)
        {
            foreach (var l in levels) F(cls, l, "Ability Score Improvement", "+2 to one ability or +1 to two (max 20), or a feat.");
        }

        static void AddLevelFiveToTen()
        {
            // ---------------- Fighter (Champion)
            F("fighter", 5, "Extra Attack", "Attack twice whenever you take the Attack action.");
            F("fighter", 5, "Tactical Shift", "When you use Second Wind, move up to half your Speed without provoking Opportunity Attacks.");
            Asi("fighter", 6, 8);
            S("fighter", 7, "Additional Fighting Style", "Learn a second Fighting Style.");
            F("fighter", 9, "Indomitable", "Reroll a failed saving throw with a bonus equal to your Fighter level. Once per Long Rest.");
            F("fighter", 9, "Tactical Master", "When you attack with a weapon, you can swap its mastery for Push, Sap or Slow for that attack.");
            S("fighter", 10, "Heroic Warrior", "In combat, you gain Heroic Inspiration at the start of your turn if you don't have it.");

            // ---------------- Barbarian (Berserker)
            F("barbarian", 5, "Extra Attack", "Attack twice whenever you take the Attack action.");
            F("barbarian", 5, "Fast Movement", "+10 ft Speed while you aren't wearing Heavy armor.");
            S("barbarian", 6, "Mindless Rage", "While raging you can't be Charmed or Frightened, and entering a rage ends those conditions.");
            F("barbarian", 7, "Feral Instinct", "Advantage on Initiative rolls.");
            F("barbarian", 7, "Instinctive Pounce", "When you enter a rage, move up to half your Speed.");
            Asi("barbarian", 8);
            F("barbarian", 9, "Brutal Strike", "When you attack recklessly, give up the Advantage on one Strength attack: if it hits, +1d10 damage and a Forceful Blow (push 15 ft) or Hamstring Blow (−15 ft Speed). Rage damage becomes +3.");
            S("barbarian", 10, "Retaliation", "When a creature within 5 ft damages you, use your Reaction to make a melee attack against it.");

            // ---------------- Rogue (Thief)
            F("rogue", 5, "Cunning Strike", "Give up one Sneak Attack die for a rider: Trip (DEX save or Prone), Poison (CON save or Poisoned) or Withdraw (move half your Speed without provoking).");
            F("rogue", 5, "Uncanny Dodge", "When an attacker you can see hits you, use your Reaction to halve the damage.");
            F("rogue", 6, "Expertise", "Two more skills get Expertise.");
            F("rogue", 7, "Evasion", "DEX-save area effects: no damage on a success, half on a failure.");
            F("rogue", 7, "Reliable Talent", "On a skill you're proficient in, a d20 roll of 9 or lower counts as 10.");
            Asi("rogue", 8, 10);
            // ---------------- Monk (Open Hand)
            F("monk", 5, "Extra Attack", "Attack twice whenever you take the Attack action.");
            F("monk", 5, "Stunning Strike", "Once per turn when you hit with a monk weapon or unarmed strike, spend 1 Focus: CON save or Stunned until your next turn (on a success: half Speed, and the next attack against it has Advantage). Martial Arts die becomes d8.");
            F("monk", 6, "Empowered Strikes", "Your unarmed strikes can deal force damage.");
            S("monk", 6, "Wholeness of Body", "Bonus Action: heal a Martial Arts die + WIS. WIS-modifier uses per Long Rest.");
            F("monk", 7, "Evasion", "DEX-save area effects: no damage on a success, half on a failure.");
            Asi("monk", 8);
            F("monk", 9, "Acrobatic Movement", "You run along walls and across water during your move.");
            F("monk", 10, "Heightened Focus", "Flurry of Blows makes three strikes.");
            F("monk", 10, "Self-Restoration", "At the end of each of your turns, end one Charmed, Frightened or Poisoned condition on yourself.");

            // ---------------- Paladin (Devotion)
            F("paladin", 5, "Extra Attack", "Attack twice whenever you take the Attack action.");
            F("paladin", 5, "Faithful Steed", "Find Steed is always prepared (a loyal mount answers your call outside combat).");
            F("paladin", 6, "Aura of Protection", "You and allies within 10 ft add your CHA modifier (min +1) to saving throws while you're conscious.");
            S("paladin", 7, "Aura of Devotion", "You and allies within 10 ft can't be Charmed while you're conscious.");
            Asi("paladin", 8);
            F("paladin", 9, "Abjure Foes", "Channel Divinity (action): up to your CHA modifier creatures within 60 ft make a WIS save or are Frightened for a minute (it ends if they take damage).");
            F("paladin", 10, "Aura of Courage", "You and allies within 10 ft can't be Frightened while you're conscious.");
            Classes["paladin"].Subclass.LaterSpells[5] = new System.Collections.Generic.List<string> { "aid", "lesser_restoration" };
            Classes["paladin"].Subclass.LaterSpells[9] = new System.Collections.Generic.List<string> { "beacon_of_hope", "revivify" };

            // ---------------- Ranger (Hunter)
            F("ranger", 5, "Extra Attack", "Attack twice whenever you take the Attack action.");
            F("ranger", 6, "Roving", "+10 ft Speed while you aren't wearing Heavy armor.");
            S("ranger", 7, "Defensive Tactics", "Escape the Horde (Opportunity Attacks against you have Disadvantage) or Multiattack Defense (after a creature hits you, its other attacks that turn have Disadvantage).");
            Asi("ranger", 8);
            F("ranger", 9, "Expertise", "Two more skills get Expertise.");
            F("ranger", 10, "Tireless", "Action: gain 1d8 + WIS temporary HP (WIS-modifier uses per Long Rest). A Short Rest removes a level of Exhaustion.");

            // ---------------- Cleric (Life)
            F("cleric", 5, "Sear Undead", "Your Turn Undead also deals radiant damage (there are no undead in Sundermere... that you know of).");
            S("cleric", 6, "Blessed Healer", "When you heal another creature with a spell, you regain 2 + the spell's level in HP.");
            F("cleric", 7, "Blessed Strikes", "Potent Spellcasting: add your WIS modifier to the damage of your cleric cantrips.");
            Asi("cleric", 8);
            F("cleric", 10, "Divine Intervention", "Once per Long Rest, cast any cleric spell of level 5 or lower without a spell slot.");
            Classes["cleric"].Subclass.LaterSpells[5] = new System.Collections.Generic.List<string> { "mass_healing_word", "revivify" };
            Classes["cleric"].Subclass.LaterSpells[7] = new System.Collections.Generic.List<string> { "death_ward", "guardian_of_faith" };
            Classes["cleric"].Subclass.LaterSpells[9] = new System.Collections.Generic.List<string> { "greater_restoration", "mass_cure_wounds" };

            // ---------------- Druid (Land)
            F("druid", 5, "Wild Resurgence", "Out of Wild Shape uses? Spend a spell slot to regain one.");
            S("druid", 6, "Natural Recovery", "Once per Long Rest, a Short Rest recovers spell slots totalling up to half your druid level.");
            F("druid", 7, "Elemental Fury", "Potent Spellcasting: add your WIS modifier to the damage of your druid cantrips.");
            Asi("druid", 8);
            F("druid", 8, "Wild Shape Improvement", "Beast forms up to CR 1, including ones that fly (Dire Wolf, Giant Owl).");
            S("druid", 10, "Nature's Ward", "You can't be Poisoned, and you resist your land's damage type (Arid fire, Polar cold, Temperate lightning).");

            // ---------------- Bard (Lore)
            F("bard", 5, "Font of Inspiration", "Bardic Inspiration comes back on a Short Rest, and you can spend a spell slot to regain a use. Your Bardic die becomes a d8.");
            S("bard", 6, "Magical Discoveries", "Learn two spells from the Cleric, Druid or Wizard lists; they're always prepared.");
            F("bard", 7, "Countercharm", "Allies within 30 ft have Advantage on saves against being Charmed or Frightened.");
            Asi("bard", 8);
            F("bard", 9, "Expertise", "Two more skills get Expertise.");
            F("bard", 10, "Magical Secrets", "Your new prepared spells can come from the Bard, Cleric, Druid or Wizard lists. Your Bardic die becomes a d10.");

            // ---------------- Sorcerer (Draconic)
            F("sorcerer", 5, "Sorcerous Restoration", "Once per Long Rest, a Short Rest gives back sorcery points equal to half your level.");
            S("sorcerer", 6, "Elemental Affinity", "Pick a draconic element: you resist it, and add your CHA modifier to the damage of your spells of that type.");
            F("sorcerer", 7, "Sorcery Incarnate", "Out of Innate Sorcery uses? Spend 2 sorcery points to activate it.");
            Asi("sorcerer", 8);
            F("sorcerer", 10, "Metamagic", "Learn two more Metamagic options.");

            // ---------------- Warlock (Ember Patron)
            F("warlock", 5, "More Invocations", "Your invocations grow (5 at level 5, 6 at 7, 7 at 9). Thirsting Blade becomes available.");
            S("warlock", 6, "Ember's Own Luck", "Add 1d10 to a failed save when it could make the difference. CHA-modifier uses per Long Rest.");
            Asi("warlock", 8);
            F("warlock", 9, "Contact Patron", "Once per Long Rest, ask your patron a question (they answer in riddles and warmth).");
            S("warlock", 10, "Ember Resilience", "Your patron's fire runs in you: resistance to fire damage.");

            // ---------------- Wizard (Evoker)
            F("wizard", 5, "Memorize Spell", "On a Short Rest, swap one prepared spell for another in your spellbook.");
            S("wizard", 6, "Sculpt Spells", "Up to 1 + the spell's level of your allies in your Evocation spells' areas are untouched.");
            Asi("wizard", 8);
            S("wizard", 10, "Empowered Evocation", "Add your INT modifier to one damage roll of your wizard Evocation spells.");

            S("rogue", 9, "Supreme Sneak", "You have Advantage on Stealth checks if you move no more than half your Speed on your turn.");
        }
            static void AddLevelElevenToThirteen()
        {
            // ---------------- Fighter (Champion)
            F("fighter", 11, "Two Extra Attacks", "Attack three times whenever you take the Attack action.");
            Asi("fighter", 12);
            F("fighter", 13, "Studied Attacks", "When you miss with an attack roll, your next attack roll against that creature before the end of your next turn has Advantage. Indomitable: two uses.");

            // ---------------- Barbarian (Berserker)
            F("barbarian", 11, "Relentless Rage", "While raging, dropping to 0 HP instead calls for a DC 10 CON save: on a success your HP becomes twice your barbarian level. Each use adds 5 to the DC until you finish a rest.");
            Asi("barbarian", 12);
            F("barbarian", 13, "Improved Brutal Strike", "Two new Brutal Strike riders: Staggering Blow (Disadvantage on its next save, no Opportunity Attacks until your next turn) and Sundering Blow (the next attack against it by someone else gets +5).");

            // ---------------- Rogue (Thief)
            F("rogue", 11, "Improved Cunning Strike", "Use up to two Cunning Strike effects on one Sneak Attack, paying each one's die. Sneak Attack becomes 6d6.");
            Asi("rogue", 12);
            S("rogue", 13, "Use Magic Device", "You can use any spell scroll, and a charged magic item has a 1-in-6 chance not to spend its charge. Sneak Attack becomes 7d6.");

            // ---------------- Monk (Open Hand)
            S("monk", 11, "Fleet Step", "When you take a Bonus Action other than Step of the Wind, you can also use Step of the Wind right after it. Martial Arts die becomes d10.");
            Asi("monk", 12);
            F("monk", 13, "Deflect Energy", "Deflect Attacks now works against attacks of any damage type.");

            // ---------------- Paladin (Devotion)
            F("paladin", 11, "Radiant Strikes", "Your melee weapon and unarmed hits deal an extra 1d8 radiant damage.");
            Asi("paladin", 12);
            Classes["paladin"].Subclass.LaterSpells[13] = new System.Collections.Generic.List<string> { "freedom_of_movement", "guardian_of_faith" };

            // ---------------- Ranger (Hunter)
            S("ranger", 11, "Superior Hunter's Prey", "Once per turn, when your Hunter's Mark damages a creature, a second creature within 30 ft of it takes that extra damage too.");
            Asi("ranger", 12);
            F("ranger", 13, "Relentless Hunter", "Taking damage can't break your concentration on Hunter's Mark.");

            // ---------------- Casters at 11 and 13 (SRD 5.2 gives them slots, not features)
            foreach (var (cls, title) in new[] { ("cleric", "Sixth-Level Miracles"), ("druid", "Deep Magic"), ("bard", "Sixth-Level Songs"), ("sorcerer", "Sixth-Level Sorcery"), ("wizard", "Sixth-Level Spells") })
                F(cls, 11, title, "You can now cast 6th-level spells; 7th-level at 13.");
            Asi("cleric", 12); Asi("druid", 12); Asi("bard", 12); Asi("sorcerer", 12); Asi("wizard", 12); Asi("warlock", 12);
            F("warlock", 11, "Mystic Arcanum (6th)", "Your patron grants one 6th-level spell you can cast once per Long Rest without a slot. A third Pact slot.");
            F("warlock", 13, "Mystic Arcanum (7th)", "...and one 7th-level spell, also once per Long Rest.");
        }
    }
}
