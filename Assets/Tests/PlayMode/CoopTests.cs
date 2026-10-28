using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>A real (virtual) gamepad registered with the Input System, so tests go through the same code as a plugged-in controller.</summary>
    public class VirtualPad : IDisposable
    {
        public readonly Gamepad Pad;
        GamepadState _state;

        public VirtualPad() { Pad = InputSystem.AddDevice<Gamepad>(); }

        void Send() { InputSystem.QueueStateEvent(Pad, _state); }

        /// <summary>Presses a button for one frame, then releases it.</summary>
        public IEnumerator Tap(GamepadButton b)
        {
            _state = _state.WithButton(b, true); Send();
            yield return null;
            _state = _state.WithButton(b, false); Send();
            yield return null;
        }

        public void Hold(Vector2 leftStick) { _state.leftStick = leftStick; Send(); }

        public void Unplug() { if (Pad.added) InputSystem.RemoveDevice(Pad); }
        public void Replug() { if (!Pad.added) InputSystem.AddDevice(Pad); }

        public void Dispose() { if (Pad.added) InputSystem.RemoveDevice(Pad); }
    }

    public class CoopTests
    {
        string _tempSaves;
        readonly List<VirtualPad> _pads = new List<VirtualPad>();

        VirtualPad NewPad() { var p = new VirtualPad(); _pads.Add(p); return p; }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f; // tests run silently
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            foreach (var p in _pads) p.Dispose();
            _pads.Clear();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator PadStartJoinsAsPlayerTwoWithoutPausingPlayerOne()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new HybridInput(), "fighter", "human");
            yield return Frames(2);
            var b = NewPad();
            yield return b.Tap(GamepadButton.Start);
            yield return Frames(2);

            Assert.AreEqual(2, g.Players.Count, "the controller's Start should add Player 2");
            Assert.IsTrue(g.Players[1].Input is GamepadInput gi && gi.Pad == b.Pad);
            Assert.IsFalse(p1.Hud.Top is PauseScreen, "P1's pause menu must not open on P2's join press");

            var before = p1.Avatar.MapPos;
            b.Hold(Vector2.right);
            yield return Frames(30);
            b.Hold(Vector2.zero);
            Assert.AreEqual(before, p1.Avatar.MapPos, "P1 must release the pad P2 joined with");
        }

        [UnityTest]
        public IEnumerator PlayerOnesOwnPadStartStillPauses()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new HybridInput(), "fighter", "human");
            yield return Frames(2);
            var a = NewPad();
            a.Hold(Vector2.left);
            yield return Frames(5);
            a.Hold(Vector2.zero);
            yield return Frames(2);
            yield return a.Tap(GamepadButton.Start);
            yield return Frames(1);

            Assert.AreEqual(1, g.Players.Count, "P1's own controller must not spawn a second player");
            Assert.IsTrue(p1.Hud.Top is PauseScreen, "P1's own Start opens the pause menu");
        }

        // ------------------------------------------------------------------ drop out / rejoin

        /// <summary>P1 on hybrid input plus P2 on a virtual pad, both standing in town.</summary>
        IEnumerator TwoPlayers(Action<PlayerSlot, PlayerSlot, VirtualPad> done)
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new HybridInput(), "fighter", "human");
            var b = NewPad();
            var p2 = g.DebugStart(new GamepadInput(b.Pad), "cleric", "elf");
            yield return Frames(2);
            done(p1, p2, b);
        }

        [UnityTest]
        public IEnumerator DropOutFreesTheScreenAndRejoinRestoresTheSameCharacter()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            string name = p2.Character.Name;
            g.GiveItem(p2, "potion_healing");
            int potions = p2.Character.Bag.Count("potion_healing");
            g.GiveCopper(p2, 500);
            int copper = p2.Character.Copper;

            g.DropOut(p2);
            yield return Frames(2);
            Assert.AreEqual(1, g.Players.Count);
            Assert.AreEqual(1f, p1.Cam.rect.width, 0.01f, "P1 gets the whole screen back");
            Assert.AreEqual(1, g.PendingPlayers.Count);

            yield return b.Tap(GamepadButton.Start);
            yield return Frames(2);
            Assert.AreEqual(2, g.Players.Count, "Start brings the departed player back");
            var back = g.Players[1];
            Assert.AreEqual(name, back.Character.Name);
            Assert.AreEqual(potions, back.Character.Bag.Count("potion_healing"));
            Assert.AreEqual(copper, back.Character.Copper);
            Assert.AreEqual(0.5f, back.Cam.rect.width, 0.01f);
            Assert.AreEqual(0, g.PendingPlayers.Count);
        }

        [UnityTest]
        public IEnumerator SavingWhileP2IsAwayKeepsTheirCharacter()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            string name = p2.Character.Name;
            g.DropOut(p2);
            yield return Frames(2);
            string path = SaveSystem.Save(false);
            Assert.IsNotNull(path);
            g.LoadGame(path);
            yield return Frames(2);
            var saved = SaveSystem.Read(path);
            Assert.AreEqual(2, saved.Players.Count, "save players: " + string.Join(",", saved.Players.ConvertAll(x => x.Name)));
            Assert.AreEqual(1, g.PendingPlayers.Count, "the departed player is in the save");
            Assert.AreEqual(name, g.PendingPlayers[0].Name);
        }

        [UnityTest]
        public IEnumerator LongRestDoesNotWaitForADepartedPlayer()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            g.Warp(p1, "home", WorldDb.Zones["home"].DefaultSpawn, false);
            g.DropOut(p2);
            yield return Frames(2);
            int day = g.Clock.Day;
            g.BeginLongRest(p1);
            yield return Frames(10);
            Assert.Greater(g.Clock.Day, day, "P1 sleeps through the night alone");
        }

        // ------------------------------------------------------------------ controller loss

        [UnityTest]
        public IEnumerator UnpluggedPadFreezesOnlyThatPlayerAndAnotherPadTakesOver()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            b.Unplug();
            yield return Frames(2);
            Assert.IsTrue(p2.Disconnected);
            Assert.IsTrue(p2.IsBusy, "a player without a controller stands still");
            Assert.IsTrue(p2.Hud.DisconnectBannerVisible);
            Assert.IsFalse(p1.IsBusy);
            float minutes = g.Clock.Minutes;
            yield return Frames(60);
            Assert.AreNotEqual(minutes, g.Clock.Minutes, "the clock keeps running for P1");

            var c = NewPad();
            yield return c.Tap(GamepadButton.Start);
            yield return Frames(2);
            Assert.AreEqual(2, g.Players.Count, "Start on another pad takes over P2 instead of adding a player");
            Assert.IsFalse(p2.Disconnected);
            Assert.AreEqual(c.Pad, ((GamepadInput)p2.Input).Pad);
            Assert.IsFalse(p2.Hud.DisconnectBannerVisible);
        }

        [UnityTest]
        public IEnumerator ReplugSamePadResumes()
        {
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            b.Unplug();
            yield return Frames(2);
            Assert.IsTrue(p2.Disconnected);
            b.Replug();
            yield return Frames(2);
            Assert.IsFalse(p2.Disconnected);
            Assert.IsFalse(p2.Hud.DisconnectBannerVisible);
        }

        [UnityTest]
        public IEnumerator UnplugMidCombatDoesNotStallTheFight()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(4) { ConfirmChance = 0.35f, EndTurnChance = 0.01f, MenuChance = 0, CancelChance = 0.01f }, "fighter", "human");
            var b = NewPad();
            var p2 = g.DebugStart(new GamepadInput(b.Pad), "cleric", "elf");
            g.Warp(p1, "forest", new Vector2Int(34, 8), false);
            g.Warp(p2, "forest", new Vector2Int(33, 9), false);
            yield return Frames(2);
            float grace = PlayerSlot.DisconnectGraceSeconds;
            PlayerSlot.DisconnectGraceSeconds = 0.5f;
            try
            {
                g.StartGroupCombat("forest_wolves", p1);
                Assert.IsTrue(p2.InCombat);
                b.Unplug();
                Time.timeScale = 6;
                float start = Time.realtimeSinceStartup;
                while (g.AnyInCombat && Time.realtimeSinceStartup - start < 120f) yield return null;
                Time.timeScale = 1;
                Assert.IsFalse(g.AnyInCombat, "the fight goes on without P2's controller and ends");
            }
            finally { PlayerSlot.DisconnectGraceSeconds = grace; }
        }

        // ------------------------------------------------------------------ splitting up and shared flows

        [UnityTest]
        public IEnumerator PlayersInDifferentZonesEachSeeTheirOwnWorld()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            g.Warp(p2, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            var p1Start = p1.Avatar.MapPos;
            var p2Start = p2.Avatar.MapPos;
            b.Hold(Vector2.down);
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                Assert.IsTrue(p1.Zone.WorldBounds.Contains(new Vector3(p1.Cam.transform.position.x, p1.Cam.transform.position.y, 0)), "P1's camera stays in the cottage");
                Assert.IsTrue(p2.Zone.WorldBounds.Contains(new Vector3(p2.Cam.transform.position.x, p2.Cam.transform.position.y, 0)), "P2's camera stays in the forest");
            }
            b.Hold(Vector2.zero);
            Assert.AreEqual("home", p1.Zone.Def.Id);
            Assert.AreEqual("forest", p2.Zone.Def.Id);
            Assert.AreEqual(p1Start, p1.Avatar.MapPos, "P2's stick doesn't move P1");
            Assert.AreNotEqual(p2Start, p2.Avatar.MapPos, "P2 walks");
        }

        [UnityTest]
        public IEnumerator OneFightsWhileTheOtherExploresThenWalksIn()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            var b = NewPad();
            var p2 = g.DebugStart(new GamepadInput(b.Pad), "cleric", "elf");
            g.Warp(p1, "forest", new Vector2Int(34, 8), false);
            yield return Frames(2);
            g.StartGroupCombat("forest_wolves", p1);
            Assert.IsTrue(p1.InCombat);
            Assert.IsFalse(p2.InCombat, "P2 is in town and keeps exploring");
            float minutes = g.Clock.Minutes;
            yield return Frames(60);
            Assert.AreNotEqual(minutes, g.Clock.Minutes, "the clock runs while P2 explores");

            g.Warp(p2, "forest", new Vector2Int(33, 9), false);
            yield return Frames(2);
            Assert.IsTrue(p2.InCombat, "walking up to the fight joins it");
            Assert.AreSame(p1.Combat, p2.Combat);
        }

        [UnityTest]
        public IEnumerator BothRestTogether()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            var home = WorldDb.Zones["home"].DefaultSpawn;
            g.Warp(p1, "home", home, false);
            g.Warp(p2, "home", home, false);
            p1.Character.HP = 1; p2.Character.HP = 1;
            p2.Character.SlotsUsed[1] = 1;
            int day = g.Clock.Day;
            g.BeginLongRest(p1);
            yield return Frames(5);
            Assert.AreEqual(day, g.Clock.Day, "P1 waits in bed for P2");
            g.BeginLongRest(p2);
            yield return Frames(5);
            Assert.Greater(g.Clock.Day, day);
            Assert.AreEqual(p1.Character.EffectiveMaxHP, p1.Character.HP);
            Assert.AreEqual(p2.Character.EffectiveMaxHP, p2.Character.HP);
            Assert.AreEqual(0, p2.Character.SlotsUsed[1], "spell slots come back");
        }

        [UnityTest]
        public IEnumerator PartyWipeWakesBothAtTheTemple()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            var b = NewPad();
            var p2 = g.DebugStart(new GamepadInput(b.Pad), "cleric", "elf");
            g.Warp(p1, "forest", new Vector2Int(34, 8), false);
            g.Warp(p2, "forest", new Vector2Int(33, 9), false);
            g.GiveCopper(p1, 1000); g.GiveCopper(p2, 1000);
            int c1 = p1.Character.Copper, c2 = p2.Character.Copper;
            yield return Frames(2);
            g.StartGroupCombat("forest_wolves", p1);
            var fight = p1.Combat;
            fight.Finished = true;
            g.OnCombatFinished(fight, false);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual("temple", p1.Zone.Def.Id);
            Assert.AreEqual("temple", p2.Zone.Def.Id);
            Assert.AreEqual(c1 - c1 / 10, p1.Character.Copper, "10% temple donation");
            Assert.AreEqual(c2 - c2 / 10, p2.Character.Copper);
        }

        [UnityTest]
        public IEnumerator HalfThePartyFallingDoesNotSkipTheDayForTheOther()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            var b = NewPad();
            var p2 = g.DebugStart(new GamepadInput(b.Pad), "cleric", "elf");
            g.Warp(p1, "forest", new Vector2Int(34, 8), false);
            yield return Frames(2);
            g.StartGroupCombat("forest_wolves", p1);
            Assert.IsFalse(p2.InCombat);
            int day = g.Clock.Day; float minutes = g.Clock.Minutes;
            var fight = p1.Combat;
            fight.Finished = true;
            g.OnCombatFinished(fight, false);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual("temple", p1.Zone.Def.Id, "the fallen player is carried to the temple");
            Assert.AreEqual("home", p2.Zone.Def.Id, "the partner who wasn't there stays put");
            Assert.AreEqual(day, g.Clock.Day, "it's not a party wipe, so no day passes for the partner");
            Assert.Less(Mathf.Abs(g.Clock.Minutes - minutes), 60f);
        }

        [UnityTest]
        public IEnumerator TradeMovesItemsAndGoldBetweenPartners()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            g.Warp(p2, p1.Zone.Def.Id, p1.MapPos, false);
            g.GiveItem(p1, "potion_healing");
            g.GiveCopper(p1, 500);
            yield return Frames(2);
            var inv = new InventoryScreen(null, null);
            p1.Hud.Push(inv);
            yield return Frames(2);
            Assert.AreSame(p2, inv.Partner, "standing together opens the trade panel");

            int potions2 = p2.Character.Bag.Count("potion_healing");
            int slot = System.Array.FindIndex(p1.Character.Bag.Slots, st => st != null && st.ItemId == "potion_healing");
            int stack = p1.Character.Bag.Slots[slot].Count;
            inv.SendToPartner(slot);
            Assert.AreEqual(potions2 + stack, p2.Character.Bag.Count("potion_healing"), "the whole stack moves");
            int copper2 = p2.Character.Copper;
            inv.GiveGold(100);
            Assert.AreEqual(copper2 + 100, p2.Character.Copper);
        }

        [UnityTest]
        public IEnumerator TwoPlayersFuzzEveryZone()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(31), "bard", "half_elf");
            var p2 = g.DebugStart(new FuzzInput(32), "wizard", "gnome");
            Time.timeScale = 4;
            foreach (var z in WorldDb.Zones.Values)
            {
                g.Warp(p1, z.Id, z.DefaultSpawn, false);
                g.Warp(p2, z.Id, z.DefaultSpawn + Vector2Int.right, false);
                yield return Frames(600);
                Assert.IsTrue(g.InGame, $"still in the game after fuzzing {z.Id}");
            }
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator PartnerLeavingWhileYouWaitInBedEndsTheNight()
        {
            var g = GameRoot.I;
            PlayerSlot p1 = null, p2 = null; VirtualPad b = null;
            yield return TwoPlayers((x, y, z) => { p1 = x; p2 = y; b = z; });
            g.Warp(p1, "home", WorldDb.Zones["home"].DefaultSpawn, false);
            int day = g.Clock.Day;
            g.BeginLongRest(p1);
            yield return Frames(2);
            Assert.AreEqual(day, g.Clock.Day, "still waiting for P2");
            g.DropOut(p2);
            yield return Frames(10);
            Assert.Greater(g.Clock.Day, day, "nobody is left awake, so the night passes");
            Assert.IsFalse(p1.Hud.Top is ChoiceScreen, "the 'waiting for...' window is gone");
        }
    }
}
