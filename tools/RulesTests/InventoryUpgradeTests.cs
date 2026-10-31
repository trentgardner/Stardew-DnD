using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Weapon sets: a main set (with the shield) and an off-set, swapped with one key.</summary>
        static void WeaponSetTests()
        {
            var pc = Make("fighter");
            pc.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            pc.Shield = true;
            pc.OffSetWeapon = (WeaponDef)ItemsDb.Get("longbow");
            int ac = pc.ArmorClass;

            Check(pc.SwapWeaponSets(out _), "swap: works");
            Eq(pc.MainHand?.Id, "longbow", "swap: the bow is in hand");
            Eq(pc.OffSetWeapon?.Id, "longsword", "swap: the sword is the off-set");
            Check(!pc.Shield && pc.ShieldStowed, "swap: the shield is stowed with the main set");
            Eq(pc.ArmorClass, ac - 2, "swap: no shield AC while it's stowed");

            Check(pc.SwapWeaponSets(out _), "swap back: works");
            Eq(pc.MainHand?.Id, "longsword", "swap back: sword in hand");
            Check(pc.Shield && !pc.ShieldStowed, "swap back: shield re-strapped");
            Eq(pc.ArmorClass, ac, "swap back: AC restored");

            var bare = Make("fighter");
            bare.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            bare.OffSetWeapon = null;
            bare.Shield = false;
            Check(bare.SwapWeaponSets(out _), "swap with an empty off-set: fine");
            Check(bare.MainHand == null, "swap with an empty off-set: unarmed");
            Eq(bare.OffSetWeapon?.Id, "longsword", "swap with an empty off-set: the sword waits in the off-set");

            // Equipping into the off-set from the bag; a two-handed weapon into the main set still needs the shield off.
            var f = Make("fighter");
            f.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            f.Shield = true;
            for (int i = 0; i < f.Bag.Size; i++) f.Bag.Slots[i] = null;
            f.Bag.Slots[0] = new ItemStack("greatsword", 1);
            Check(f.EquipFromBag(0, out _, offSet: true), "equip into the off-set");
            Eq(f.OffSetWeapon?.Id, "greatsword", "the greatsword waits in the off-set");
            Check(f.Shield, "equipping the off-set leaves the shield alone");
            Check(f.SwapWeaponSets(out _) && f.MainHand.Id == "greatsword" && !f.Shield, "swap to the two-hander stows the shield");
        }
    }
}

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>One-click moves between the hotbar (slots 0–11) and the backpack.</summary>
        static void HotbarMoveTests()
        {
            var inv = new Inventory();
            inv.Slots[0] = new ItemStack("torch", 1);
            inv.Slots[20] = new ItemStack("rations", 2);
            Eq(inv.ToHotbar(20), 1, "to hotbar: the first free hotbar slot");
            Check(inv.Slots[1]?.ItemId == "rations" && inv.Slots[20] == null, "to hotbar: moved, not copied");
            Eq(inv.ToBackpack(1), Inventory.HotbarSize, "to backpack: the first free backpack slot");
            Check(inv.Slots[Inventory.HotbarSize]?.ItemId == "rations", "to backpack: there it is");
            for (int i = 0; i < Inventory.HotbarSize; i++) inv.Slots[i] = new ItemStack("torch", 1);
            Eq(inv.ToHotbar(Inventory.HotbarSize), -1, "to hotbar when it's full: nothing moves");
            Check(inv.Slots[Inventory.HotbarSize]?.ItemId == "rations", "to hotbar when it's full: still in the backpack");
        }
    }
}
