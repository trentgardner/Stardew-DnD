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
            S("rogue", 9, "Supreme Sneak", "You have Advantage on Stealth checks if you move no more than half your Speed on your turn.");
        }
    }
}
