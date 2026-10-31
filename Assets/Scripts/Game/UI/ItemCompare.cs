using System;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// "Compare before you equip": a short rich-text line for an item against what you're using now.
    /// Weapons: to hit, damage and average per swing vs AC 13; armour and shields: the AC you'd have. Better = green, worse = red.
    /// </summary>
    public static class ItemCompare
    {
        const string Green = "#5fbf5f", Red = "#d9534f";
        const int TypicalAC = 13;

        static string Delta(double d, string unit)
        {
            if (Math.Abs(d) < 0.05) return "same " + unit;
            return $"<color={(d > 0 ? Green : Red)}>{(d > 0 ? "+" : "-")}{Math.Abs(d):0.#} {unit}</color>";
        }

        static double PerSwing(AttackOption a)
        {
            double hit = Math.Clamp((21 - (TypicalAC - a.ToHit)) / 20.0, 0.05, 0.95);
            return hit * (a.Damage.Average + a.DamageMod);
        }

        public static string Describe(PlayerCharacter pc, ItemDef item)
        {
            switch (item)
            {
                case WeaponDef w:
                {
                    var atk = pc.WeaponAttack(w);
                    string line = $"{AbilityUtil.Fmt(atk.ToHit)} to hit · {atk.Damage}{(atk.DamageMod != 0 ? AbilityUtil.Fmt(atk.DamageMod) : "")} {atk.DamageType.ToString().ToLower()}";
                    if (pc.MainHand != null && pc.MainHand.Id == w.Id) return line + " (equipped)";
                    if (pc.OffSetWeapon != null && pc.OffSetWeapon.Id == w.Id) return line + " (off-set)";
                    var current = pc.MainHand != null ? pc.WeaponAttack(pc.MainHand) : pc.UnarmedStrike();
                    string vs = pc.MainHand != null ? pc.MainHand.Name : "your fists";
                    line += $"\nvs {vs}: {Delta(PerSwing(atk) - PerSwing(current), "avg per swing")}";
                    if (w.Has(WeaponProperty.TwoHanded) && pc.Shield) line += "\n<i>Two-handed: your shield comes off.</i>";
                    return line;
                }
                case ArmorDef a:
                {
                    if (pc.Armor != null && pc.Armor.Id == a.Id) return $"AC {pc.ArmorClass} (equipped)";
                    if (!pc.CanWearArmor(a)) return $"<color={Red}>You aren't trained in {a.Category.ToString().ToLower()} armour.</color>";
                    int now = pc.ArmorClass;
                    var keep = pc.Armor;
                    pc.Armor = a;
                    int with = pc.ArmorClass;
                    pc.Armor = keep;
                    return $"AC {with} ({Delta(with - now, "AC")})" + (a.StrengthRequirement > pc.Scores[(int)Ability.STR] ? $"\n<i>Needs STR {a.StrengthRequirement}: slower in it.</i>" : "");
                }
                default:
                    if (item != null && item.Kind == ItemKind.Shield)
                    {
                        if (pc.Shield || pc.ShieldStowed) return "+2 AC (equipped)";
                        if (!pc.Class.Shields) return $"<color={Red}>You aren't trained with shields.</color>";
                        return $"<color={Green}>+2 AC</color>" + (pc.MainHand != null && pc.MainHand.Has(WeaponProperty.TwoHanded) ? "\n<i>Needs a free hand: not with a two-handed weapon.</i>" : "");
                    }
                    return "";
            }
        }
    }
}
