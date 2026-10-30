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
            Assert.AreEqual(before, p.Avatar.transform.position, "and nothing else happens in the world");
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
            Assert.AreEqual(before, p.Avatar.transform.position);
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
    }
}
