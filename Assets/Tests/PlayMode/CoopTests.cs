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
