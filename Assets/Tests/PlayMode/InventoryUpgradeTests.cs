using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>The inventory upgrades (docs/superpowers/specs/2026-10-30-inventory-upgrades-design.md).</summary>
    public class InventoryUpgradeTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>A fighter in town with nothing on screen and the given items on hotbar slots 0, 1, 2...</summary>
        static IEnumerator Ready(GameRoot g, ScriptInput input, System.Action<PlayerSlot> got, params string[] hotbar)
        {
            var p = g.DebugStart(input, "fighter", "human");
            g.Warp(p, "town", WorldDb.Zones["town"].DefaultSpawn, false);
            yield return Frames(3);
            p.Hud.CloseAll();
            for (int i = 0; i < Inventory.HotbarSize; i++) p.Character.Bag.Slots[i] = null;
            for (int i = 0; i < hotbar.Length; i++) p.Character.Bag.Slots[i] = new ItemStack(hotbar[i], 2);
            p.Hud.HotbarIndex = 0;
            yield return Frames(2);
            got(p);
        }

        // ============================================================ Task 1: hotbar

        [UnityTest]
        public IEnumerator QAndRMoveTheSelectionAndXUsesIt()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "torch", "potion_healing");
            p.Character.HP = 1;
            input.Next = true; // R
            yield return Frames(2);
            Assert.AreEqual(1, p.Hud.HotbarIndex, "R moves the selection right");
            StringAssert.Contains("Potion of Healing", p.Hud.HotbarLabel, "the selected item's name shows above the bar");
            input.Prev = true; // Q
            yield return Frames(2);
            Assert.AreEqual(0, p.Hud.HotbarIndex, "Q moves it back");
            input.Next = true;
            yield return Frames(2);
            input.Alt = true; // X
            yield return Frames(2);
            Assert.AreEqual(1, p.Character.CountOf("potion_healing"), "X used the selected potion");
        }

        [UnityTest]
        public IEnumerator HotbarClickUsesTheSlot()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "torch");
            p.Avatar.TorchMinutes = 0;
            input.MousePos = p.Hud.HotbarSlotScreenPos(0);
            Assert.AreEqual(0, p.Hud.HotbarSlotAt(input.MousePos));
            Vector3 before = p.Avatar.transform.position;
            input.Left = true;
            yield return Frames(2);
            Assert.Greater(p.Avatar.TorchMinutes, 0f, "clicking the torch on the hotbar lights it");
            Assert.Less(Vector3.Distance(before, p.Avatar.transform.position), 0.01f, "and nothing else happens in the world");
        }

        [UnityTest]
        public IEnumerator HotbarRightClickOpensOptions()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "torch", "potion_healing");
            input.MousePos = p.Hud.HotbarSlotScreenPos(1);
            input.Right = true;
            yield return Frames(2);
            Assert.IsTrue(p.Hud.Top is ChoiceScreen, "right-click: the item's options");
            Assert.AreEqual(1, p.Hud.HotbarIndex, "and it's selected");
        }

        [UnityTest]
        public IEnumerator HotbarClickDoesNotMoveYou()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            input.MousePos = p.Hud.HotbarSlotScreenPos(5); // an empty slot
            Vector3 before = p.Avatar.transform.position;
            input.Left = true;
            yield return Frames(3);
            Assert.Less(Vector3.Distance(before, p.Avatar.transform.position), 0.01f, "you stay where you are");
            Assert.IsFalse(p.Hud.HasScreen, "an empty hotbar slot does nothing");
        }

        [UnityTest]
        public IEnumerator SelectedSlotHasARaisedFrame()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "torch", "potion_healing");
            p.Hud.HotbarIndex = 1;
            yield return Frames(2);
            Assert.Greater(p.Hud.HotbarSlotScale(1), 1.05f, "the selected slot stands out");
            Assert.AreEqual(1f, p.Hud.HotbarSlotScale(0), 0.001f);
        }
            // ============================================================ Task 2: weapon sets

        [UnityTest]
        public IEnumerator ZSwapsOutsideCombat()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            p.Character.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            p.Character.OffSetWeapon = (WeaponDef)ItemsDb.Get("longbow");
            input.Swap = true;
            yield return Frames(2);
            Assert.AreEqual("longbow", p.Character.MainHand.Id, "Z swaps to the bow");
            Assert.IsTrue(p.Hud.ToastTexts.Any(t => t.Contains("Longbow")), "and says so");
        }

        [UnityTest]
        public IEnumerator WeaponSetsSurviveSaveAndLoad()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            p.Character.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            p.Character.Shield = true;
            p.Character.OffSetWeapon = (WeaponDef)ItemsDb.Get("longbow");
            p.Character.SwapWeaponSets(out _);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            var pc = g.Players[0].Character;
            Assert.AreEqual("longbow", pc.MainHand.Id);
            Assert.AreEqual("longsword", pc.OffSetWeapon.Id);
            Assert.IsTrue(pc.ShieldStowed && !pc.Shield, "the shield is still on your back");
        }

        [UnityTest]
        public IEnumerator SwapIsFreeOncePerTurnInCombat()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(4) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Character.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            p.Character.OffSetWeapon = (WeaponDef)ItemsDb.Get("longbow");
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var swap = c.ActionsFor(p).FirstOrDefault(a => a.Name == "Swap Weapons");
            Assert.IsNotNull(swap, "Swap Weapons is on the action bar");
            Assert.IsTrue(swap.Enabled);
            Assert.AreEqual(ActionCost.Free, swap.Cost, "it costs no action");
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            var run = swap.Run(u, u.Pos);
            while (run.MoveNext()) { }
            Assert.AreEqual("longbow", p.Character.MainHand.Id);
            var again = c.ActionsFor(p).First(a => a.Name == "Swap Weapons");
            Assert.IsFalse(again.Enabled, "only once per turn");
            StringAssert.Contains("already swapped", again.Why);
        }
            // ============================================================ Task 3: compare before you equip

        [UnityTest]
        public IEnumerator CompareShowsDamageDifference()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new ScriptInput(), "fighter", "human");
            yield return null;
            var pc = p.Character;
            pc.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            pc.Shield = false;
            string text = ItemCompare.Describe(pc, ItemsDb.Get("greatsword"));
            StringAssert.Contains("to hit", text);
            StringAssert.Contains("vs Longsword", text, "compared with what you're holding");
            StringAssert.Contains("<color=#5fbf5f>+", text, "a bigger average shows in green");
            StringAssert.Contains("<color=#d9534f>-", ItemCompare.Describe(pc, ItemsDb.Get("dagger")), "a smaller one in red");
        }

        [UnityTest]
        public IEnumerator CompareShowsArmourDifference()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new ScriptInput(), "fighter", "human");
            yield return null;
            var pc = p.Character;
            pc.Armor = (ArmorDef)ItemsDb.Get("leather");
            int ac = pc.ArmorClass;
            string text = ItemCompare.Describe(pc, ItemsDb.Get("chain_mail"));
            StringAssert.Contains("AC ", text);
            StringAssert.Contains("<color=#5fbf5f>+", text, "heavier armour: more AC, in green");
            Assert.AreEqual(ac, pc.ArmorClass, "comparing doesn't change what you're wearing");
            Assert.AreEqual("leather", pc.Armor.Id);
        }

        [UnityTest]
        public IEnumerator CompareMarksEquipped()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new ScriptInput(), "fighter", "human");
            yield return null;
            p.Character.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            StringAssert.Contains("(equipped)", ItemCompare.Describe(p.Character, ItemsDb.Get("longsword")));
        }
            // ============================================================ Task 4: one-click moves

        [UnityTest]
        public IEnumerator SendToCompanionFromOptions()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "rations");
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("fighter", "dwarf", "Tova"));
            g.SpawnCompanion(p, pc, "hums");
            int theirs = pc.CountOf("rations");
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            inv.OpenOptionsFor(0);
            var menu = (ChoiceScreen)p.Hud.Top;
            Assert.IsTrue(menu.Choose("Send to Tova"), "each companion is a one-click destination");
            yield return Frames(2);
            Assert.AreEqual(theirs + 2, pc.CountOf("rations"));
            Assert.AreEqual(0, p.Character.CountOf("rations"));
        }

        [UnityTest]
        public IEnumerator SendToFullBagKeepsTheRest()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "rations");
            p.Character.Bag.Slots[0] = new ItemStack("rations", 5);
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("fighter", "dwarf", "Tova"));
            for (int i = 0; i < pc.Bag.Size; i++) pc.Bag.Slots[i] = new ItemStack("dagger", 1);
            g.SpawnCompanion(p, pc, "hums");
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            inv.OpenOptionsFor(0);
            ((ChoiceScreen)p.Hud.Top).Choose("Send to Tova");
            yield return Frames(2);
            Assert.AreEqual(5, p.Character.CountOf("rations"), "nothing is lost when their bag is full");
            Assert.IsTrue(p.Hud.ToastTexts.Any(t => t.Contains("full")), "and you're told why");
        }
            // ============================================================ Task 5: party bags side by side

        [UnityTest]
        public IEnumerator CompanionBagShowsAsATab()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "rations");
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("fighter", "dwarf", "Tova"));
            g.SpawnCompanion(p, pc, "hums");
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            yield return null;
            Assert.IsTrue(inv.Tabs.Contains("Tova"), "a companion's bag sits beside yours");
            inv.SelectTab(inv.Tabs.ToList().IndexOf("Tova"));
            yield return null;
            Assert.AreSame(pc.Bag, inv.OtherInv);
            inv.SendTo(0, inv.OtherInv, "Tova");
            Assert.AreEqual(0, p.Character.CountOf("rations"));
        }

        [UnityTest]
        public IEnumerator ChestComesFirstThenParty()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("fighter", "dwarf", "Tova"));
            g.SpawnCompanion(p, pc, "hums");
            var inv = new InventoryScreen(g.World.Storage, "Home storage");
            p.Hud.Push(inv);
            yield return null;
            CollectionAssert.AreEqual(new[] { "Home storage", "Tova" }, inv.Tabs.ToArray());
            Assert.AreSame(g.World.Storage, inv.OtherInv, "the chest you opened is shown first");
        }

        [UnityTest]
        public IEnumerator BothPlayersTradeAtOnce()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new ScriptInput(), "fighter", "human");
            var p2 = g.DebugStart(new ScriptInput(), "cleric", "elf");
            g.Warp(p1, "town", WorldDb.Zones["town"].DefaultSpawn, false);
            g.Warp(p2, "town", WorldDb.Zones["town"].DefaultSpawn, false);
            yield return Frames(3);
            p1.Hud.CloseAll(); p2.Hud.CloseAll();
            p1.Character.Bag.Slots[0] = new ItemStack("rations", 3);
            p2.Character.Bag.Slots[0] = new ItemStack("torch", 4);
            int r2 = p2.Character.CountOf("rations"), t1 = p1.Character.CountOf("torch");
            var a = new InventoryScreen(null, null); p1.Hud.Push(a);
            var b = new InventoryScreen(null, null); p2.Hud.Push(b);
            yield return null;
            Assert.AreSame(p2.Character.Bag, a.OtherInv, "your partner standing by you shows up to trade");
            a.SendTo(0, a.OtherInv, p2.Character.Name);
            b.SendTo(0, b.OtherInv, p1.Character.Name);
            yield return null;
            Assert.AreEqual(r2 + 3, p2.Character.CountOf("rations"));
            Assert.AreEqual(t1 + 4, p1.Character.CountOf("torch"));
        }
            // ============================================================ Task 6: filter tabs

        [UnityTest]
        public IEnumerator FilterGearDimsPotions()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "potion_healing", "longsword");
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            yield return null;
            inv.Filter = ItemFilter.Gear;
            yield return null;
            Assert.Less(inv.BagCellAlpha(0), 0.5f, "a potion isn't gear: dimmed");
            Assert.AreEqual(1f, inv.BagCellAlpha(1), 0.01f, "the sword stays bright");
            Assert.AreEqual("potion_healing", p.Character.Bag.Slots[0].ItemId, "filtering never moves anything");
            inv.Filter = ItemFilter.All;
            yield return null;
            Assert.AreEqual(1f, inv.BagCellAlpha(0), 0.01f);
        }

        [Test]
        public void FiltersCoverEveryItem()
        {
            var filters = new[] { ItemFilter.Gear, ItemFilter.Use, ItemFilter.Treasure, ItemFilter.Quest };
            foreach (var d in ItemsDb.All.Values)
                Assert.AreEqual(1, filters.Count(f => ItemFilters.Matches(d, f)), $"{d.Id} belongs to exactly one tab");
            Assert.IsTrue(ItemFilters.Matches(ItemsDb.Get("dragon_treat"), ItemFilter.Use), "treats are things you use");
        }

        [Test]
        public void SortNeverTouchesTheHotbar()
        {
            var inv = new Inventory();
            inv.Slots[0] = new ItemStack("torch", 1);
            inv.Slots[3] = new ItemStack("potion_healing", 1);
            inv.Slots[30] = new ItemStack("rations", 1);
            inv.Slots[14] = new ItemStack("dagger", 1);
            inv.Sort();
            Assert.AreEqual("torch", inv.Slots[0].ItemId);
            Assert.AreEqual("potion_healing", inv.Slots[3].ItemId);
            Assert.IsNull(inv.Slots[1]);
        }
            // ============================================================ Task 7: junk

        [UnityTest]
        public IEnumerator MarkJunkAndSellAll()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            var pc = p.Character;
            pc.Bag.Slots[20] = new ItemStack("copper_trinket", 3);
            pc.Bag.Slots[21] = new ItemStack("gemstone", 1);
            pc.Bag.Slots[22] = new ItemStack("rations", 2);
            Assert.IsTrue(pc.MarkJunk("copper_trinket", true));
            Assert.IsTrue(pc.MarkJunk("gemstone", true));
            int copper = pc.Copper;
            int expected = 3 * ShopDb.SellPrice("general", ItemsDb.Get("copper_trinket")) + ShopDb.SellPrice("general", ItemsDb.Get("gemstone"));
            var (items, paid) = ShopDb.SellAllJunk(p, "general");
            Assert.AreEqual(4, items);
            Assert.AreEqual(expected, paid);
            Assert.AreEqual(copper + expected, pc.Copper);
            Assert.AreEqual(0, pc.CountOf("copper_trinket"));
            Assert.AreEqual(2, pc.CountOf("rations"), "only junk is sold");
        }

        [UnityTest]
        public IEnumerator SellAllJunkSkipsQuestAndEquipped()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            var pc = p.Character;
            pc.Bag.Slots[20] = new ItemStack("pond_ring", 1);
            Assert.IsFalse(pc.MarkJunk("pond_ring", true), "quest items can't be junk");
            pc.MainHand = (WeaponDef)ItemsDb.Get("longsword");
            pc.MarkJunk("longsword", true);
            var (items, _) = ShopDb.SellAllJunk(p, "general");
            Assert.AreEqual(0, items);
            Assert.AreEqual("longsword", pc.MainHand.Id, "the sword in your hand stays there");
            Assert.AreEqual(1, pc.CountOf("pond_ring"));
        }

        [UnityTest]
        public IEnumerator JunkMarksSurviveSaveAndLoad()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x);
            p.Character.MarkJunk("copper_trinket", true);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.IsTrue(g.Players[0].Character.Junk.Contains("copper_trinket"));
        }

        [UnityTest]
        public IEnumerator JunkOptionInTheMenu()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            PlayerSlot p = null;
            yield return Ready(g, input, x => p = x, "copper_trinket");
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            inv.OpenOptionsFor(0);
            Assert.IsTrue(((ChoiceScreen)p.Hud.Top).Choose("Mark as junk"));
            Assert.IsTrue(p.Character.Junk.Contains("copper_trinket"));
            yield return null;
            inv.OpenOptionsFor(0);
            Assert.IsTrue(((ChoiceScreen)p.Hud.Top).Labels.Any(l => l.StartsWith("Unmark junk")));
        }
    }
}
