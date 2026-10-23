using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    [Flags]
    public enum WeaponProperty
    {
        None = 0, Ammunition = 1, Finesse = 2, Heavy = 4, Light = 8, Loading = 16, Reach = 32,
        Thrown = 64, TwoHanded = 128, Versatile = 256
    }

    /// <summary>2024 Weapon Mastery properties.</summary>
    public enum WeaponMastery { None, Cleave, Graze, Nick, Push, Sap, Slow, Topple, Vex }

    public enum WeaponCategory { Simple, Martial }

    public enum ItemKind { Weapon, Armor, Shield, Consumable, Gear, Treasure, Wondrous }

    public class ItemDef
    {
        public string Id;
        public string Name;
        public ItemKind Kind;
        /// <summary>Price in copper pieces (1 gp = 100 cp).</summary>
        public int PriceCp;
        public string Description = "";
        /// <summary>Art lookup key (see ArtRegistry in the game layer).</summary>
        public string Icon;
        /// <summary>Weight in pounds (SRD values). Coins are not items and weigh nothing.</summary>
        public float Weight;
        /// <summary>How many fit in one inventory slot. 0 = use the default for the item kind.</summary>
        public int StackSize;

        public int MaxStack
        {
            get
            {
                if (StackSize > 0) return StackSize;
                switch (Kind)
                {
                    case ItemKind.Consumable: return 20;
                    case ItemKind.Treasure: return 99;
                    case ItemKind.Gear: return 20;
                    default: return 1; // weapons, armor, shields, wondrous items
                }
            }
        }

        public string PriceText => Currency.Format(PriceCp);
    }

    public class WeaponDef : ItemDef
    {
        public WeaponCategory Category;
        public DiceExpr Damage;
        public DiceExpr VersatileDamage;
        public DamageType DamageType;
        public WeaponProperty Properties;
        public WeaponMastery Mastery;
        /// <summary>Normal / long range in feet for ranged or thrown weapons. 0 = melee only.</summary>
        public int RangeNormal, RangeLong;

        /// <summary>+1/+2/+3 to attack and damage rolls (magic weapons).</summary>
        public int MagicBonus;
        /// <summary>Extra damage dice on a hit (e.g. 1d4 fire), doubled on a crit.</summary>
        public DiceExpr ExtraDamage;
        public DamageType ExtraDamageType;
        public string Rarity;

        public bool Has(WeaponProperty p) => (Properties & p) != 0;
        public WeaponDef MemberwiseCloneWeapon() => (WeaponDef)MemberwiseClone();
        public bool IsRanged => Has(WeaponProperty.Ammunition);
        public int ReachFt => Has(WeaponProperty.Reach) ? 10 : 5;

        public string PropertyText()
        {
            var list = new List<string>();
            foreach (WeaponProperty p in Enum.GetValues(typeof(WeaponProperty)))
                if (p != WeaponProperty.None && Has(p)) list.Add(p == WeaponProperty.TwoHanded ? "Two-Handed" : p.ToString());
            return string.Join(", ", list);
        }
    }

    public enum ArmorCategory { Light, Medium, Heavy }

    public class ArmorDef : ItemDef
    {
        public ArmorCategory Category;
        public int BaseAC;
        /// <summary>Max DEX bonus; -1 = unlimited.</summary>
        public int MaxDex = -1;
        public int StrengthRequirement;
        public bool StealthDisadvantage;
    }

    public enum WornSlot { Cloak, Ring, Amulet, Trinket, Focus }

    /// <summary>Wondrous items worn or carried for a passive benefit.</summary>
    public class WondrousDef : ItemDef
    {
        public WornSlot Slot;
        public string Rarity = "Uncommon";
        public bool RequiresAttunement;
        public int AcBonus;
        public int SaveBonus;
        public int SpellAttackBonus;
        public bool DeathSaveAdvantage;
        /// <summary>Once per long rest, reroll a d20 Test you just rolled (offered by the dice UI).</summary>
        public bool RerollPerLongRest;
        public string Lore;
    }

    public enum ConsumableEffect { Heal, Revive, Light, Ration, SpellScroll }

    public class ConsumableDef : ItemDef
    {
        public ConsumableEffect Effect;
        public DiceExpr Amount;
        public string SpellId;
    }

    public static class Currency
    {
        public static string Format(int cp)
        {
            int gp = cp / 100, sp = (cp % 100) / 10, c = cp % 10;
            var parts = new List<string>();
            if (gp > 0) parts.Add(gp + " gp");
            if (sp > 0) parts.Add(sp + " sp");
            if (c > 0 || parts.Count == 0) parts.Add(c + " cp");
            return string.Join(" ", parts);
        }
    }

    public static class MasteryText
    {
        public static string Describe(WeaponMastery m)
        {
            switch (m)
            {
                case WeaponMastery.Cleave: return "Cleave: on a hit, make one more attack against a creature within 5 ft of the first (no ability modifier to damage). Once per turn.";
                case WeaponMastery.Graze: return "Graze: on a miss, deal damage equal to your ability modifier.";
                case WeaponMastery.Nick: return "Nick: the extra Light-weapon attack is part of the Attack action instead of a Bonus Action.";
                case WeaponMastery.Push: return "Push: on a hit, push the target up to 10 ft straight away from you.";
                case WeaponMastery.Sap: return "Sap: on a hit, the target has Disadvantage on its next attack roll before your next turn.";
                case WeaponMastery.Slow: return "Slow: on a hit, reduce the target's Speed by 10 ft until the start of your next turn.";
                case WeaponMastery.Topple: return "Topple: on a hit, the target makes a CON save or falls Prone.";
                case WeaponMastery.Vex: return "Vex: on a hit, you have Advantage on your next attack roll against that creature before the end of your next turn.";
                default: return "";
            }
        }
    }
}
