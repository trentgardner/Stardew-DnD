# Phase 1: Co-op Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make two-player split-screen co-op solid enough to play through Act I: joining, leaving, controller loss, splitting up, and the shared rest, save and death flows.

**Architecture:** Real virtual gamepads (Unity Input System `InputSystem.AddDevice<Gamepad>()` plus queued state events) drive play-mode tests end to end through `GameRoot.TryDropIn` and `HybridInput`. The fixes live in `GameRoot` (drop-out, rebind), `PlayerInput.cs` (start-press ownership), `PlayerSlot` (disconnected state), `PlayerHud` (a disconnected banner) and `PauseScreen` (a leave option).

**Tech Stack:** Unity 6000.5.9f1, C#, Input System 1.19.0, Unity Test Framework 1.4.5 (play mode).

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md (Build order, phase 1)

## Global Constraints

- Tests run MUTED (`AudioListener.volume = 0`, `Sfx.Volume = 0`). This is already in `SmokeTests.SetUp`; any new fixture must do the same.
- Any error or exception logged during a play-mode test fails it. Don't suppress logs.
- Player 1 uses `HybridInput` (KB/M plus any unclaimed pad, hot-swapped). A controller pressing Start becomes Player 2, and P1 releases that pad.
- Text entry stays keyboard-only.
- Every window closes with Esc/B and has a ✕ button (existing GameScreen behavior; reuse it).
- Run Unity: `"C:/Program Files/Unity/Hub/Editor/6000.5.9f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <scratch>/results.xml` (the editor must be closed). Rules tests: `dotnet run --project tools/RulesTests`.

## Review Focus

- A solo P1 on KB/M who picks up a second controller and presses Start: it should join as P2, and P1's pause menu must NOT open on the same press.
- A controller unplugged mid-dialogue or mid-combat: that player's HUD shows a banner, the game doesn't crash, and plugging any pad in and pressing Start restores control to that same player.
- P2 leaving while P1 is in a menu: P1's camera goes back to full screen, P1's open screen keeps working, and P2's character returns on the next Start press.
- Both players asleep requirement: if P2 has left, P1's long rest must not wait forever for a player who's gone.
- Save while P2 is disconnected or has left: P2's character must still be in the save (a pending player is saved too).

---

### Task 1: Virtual pad test helper + Start ownership on join

**Files:**
- Create: `Assets/Tests/PlayMode/CoopTests.cs`
- Modify: `Assets/Scripts/Game/Core/PlayerInput.cs` (HybridInput: Start on a pad counts as Pause only if that pad was P1's `ActivePad` before this frame)
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (`TryDropIn`: a Start press on a pad that isn't P1's pre-frame `ActivePad` joins)

**Interfaces:**
- Produces: `class VirtualPad : IDisposable` in CoopTests.cs: `VirtualPad()` adds a `Gamepad`, `IEnumerator Tap(GamepadButton b)` (press for one frame, release the next), `void Hold(Vector2 leftStick)`, `void Unplug()` (`InputSystem.RemoveDevice`), `void Replug()` (`InputSystem.AddDevice(pad)`), `Gamepad Pad`. `Dispose()` removes the device.
- Produces: `CoopTests` fixture with the same SetUp/TearDown as `SmokeTests` (temp saves, muted), plus a disposal of all VirtualPads.
- Produces: `HybridInput.PrevActivePad` (the pad that was active at the start of the current frame).

- [ ] **Step 1: Write failing test `PadStartJoinsAsPlayerTwoWithoutPausingPlayerOne`**: start solo with `new HybridInput()` (`g.DebugStart`), create pad B, `Tap(Start)`, wait 2 frames. Assert `g.Players.Count == 2`, `Players[1].Input is GamepadInput gi && gi.Pad == B.Pad`, `!(Players[0].Hud.Top is PauseScreen)`. Then `B.Hold(right)` for 30 frames and assert P1's `Avatar.MapPos` is unchanged (P1 released the pad).
- [ ] **Step 2: Write failing test `PlayerOnesOwnPadStartStillPauses`**: solo, pad A, `Hold(left stick)` for 5 frames (pad A becomes ActivePad), release, `Tap(Start)`. Assert `Players.Count == 1` and `Players[0].Hud.Top is PauseScreen`.
- [ ] **Step 3: Run both and confirm they fail** (expected: P1 PauseScreen opens on B's Start in test 1).
- [ ] **Step 4: Implement** the pre-frame ActivePad rule in HybridInput (record `PrevActivePad` at the top of `Refresh()` before scanning; Pause from pads counts only for `pad == PrevActivePad`) and use it in `TryDropIn`.
- [ ] **Step 5: Run the whole PlayMode suite and confirm it passes** (all existing tests plus the 2 new ones).
- [ ] **Step 6: Commit** `"Co-op: a controller's Start joins as P2 without pausing P1; virtual pad test helper"`.

### Task 2: Drop out and rejoin

**Files:**
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (new `DropOut`; `_pendingPlayers` saved; long rest ignores departed players)
- Modify: `Assets/Scripts/Game/UI/Screens.cs` (PauseScreen main page: `"Leave the game (your character waits for you)"` for `Hud.Slot.Index > 0`, disabled while `Hud.Slot.InCombat`)
- Modify: `Assets/Scripts/Game/Core/SaveSystem.cs` (Save writes pending players after the active ones)
- Test: `Assets/Tests/PlayMode/CoopTests.cs`

**Interfaces:**
- Consumes: `VirtualPad` (Task 1).
- Produces: `public void GameRoot.DropOut(PlayerSlot p)`: snapshot `SaveSystem.ToData(p.Character)` with zone and position, add to `_pendingPlayers`, dismiss that player's companions to the tavern (`DismissCompanion`), destroy the HUD, camera and avatar, remove the slot, `LayoutCameras()`, and narrate `"{name} heads off for now. (Press Start to rejoin.)"` on the remaining players.
- Produces: `public IReadOnlyList<PcData> GameRoot.PendingPlayers`.

- [ ] **Step 1: Write failing test `DropOutFreesTheScreenAndRejoinRestoresTheSameCharacter`**: P1 hybrid plus P2 joined via pad B (using Task 1's flow, then `DebugStart`-equivalent character: call `g.DebugStart(new GamepadInput(B.Pad), "cleric", "elf")`). Give P2 item `"potion_healing"` and 500 cp. Call `g.DropOut(p2)`. Assert `Players.Count == 1`, `Players[0].Cam.rect.width == 1`, `PendingPlayers.Count == 1`. `B.Tap(Start)`; wait 2 frames. Assert P2's `Character.Name` matches, the bag contains `potion_healing`, Copper ≥ 500, and `Cam.rect.width == 0.5`.
- [ ] **Step 2: Write failing test `SavingWhileP2IsAwayKeepsTheirCharacter`**: drop out P2, `SaveSystem.Save(false)`, `g.LoadGame(path)`. Assert `PendingPlayers.Count == 1` with P2's name.
- [ ] **Step 3: Write failing test `LongRestDoesNotWaitForADepartedPlayer`**: drop out P2, `g.BeginLongRest(p1)`, run 600 frames at timeScale 4. Assert the clock day advanced and no `ChoiceScreen` "Waiting for" is open.
- [ ] **Step 4: Run and confirm they fail** (expected: `DropOut` doesn't exist → compile error).
- [ ] **Step 5: Implement** `DropOut`, `PendingPlayers`, the pause menu entry, saving pending players, and the long-rest filter (already ready players only).
- [ ] **Step 6: Run the PlayMode suite and confirm it passes.**
- [ ] **Step 7: Commit** `"Co-op: drop out from the pause menu and rejoin with Start; saves keep departed players"`.

### Task 3: Controller disconnect and rebind

**Files:**
- Modify: `Assets/Scripts/Game/Core/PlayerSlot.cs` (`public bool Disconnected => Input != null && !Input.IsConnected;` and `IsBusy` includes `Disconnected`)
- Modify: `Assets/Scripts/Game/Core/PlayerInput.cs` (`GamepadInput.Pad` becomes settable through `Rebind(Gamepad pad)`)
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (`TryDropIn`: when any slot is Disconnected, a Start press on an unowned pad rebinds that slot instead of adding a player)
- Modify: `Assets/Scripts/Game/UI/PlayerHud.cs` (a centered banner while Disconnected: `"Controller disconnected. Plug it back in, or press Start on any controller."`)
- Test: `Assets/Tests/PlayMode/CoopTests.cs`

**Interfaces:**
- Consumes: `VirtualPad.Unplug/Replug` (Task 1).
- Produces: `PlayerSlot.Disconnected`, `GamepadInput.Rebind(Gamepad)`.

- [ ] **Step 1: Write failing test `UnpluggedPadFreezesOnlyThatPlayerAndAnotherPadTakesOver`**: P1 plus P2 on pad B. `B.Unplug()`; wait 2 frames. Assert `p2.Disconnected`, `p2.IsBusy`, P2 HUD banner active, and the clock still advances (P1 not busy). New pad C, `C.Tap(Start)`. Assert `Players.Count == 2`, `!p2.Disconnected`, and `((GamepadInput)p2.Input).Pad == C.Pad`.
- [ ] **Step 2: Write failing test `ReplugSamePadResumes`**: unplug B, replug B, wait 2 frames. Assert `!p2.Disconnected` (Input System re-adds the same device instance; `Pad.added` true again).
- [ ] **Step 3: Write failing test `UnplugMidCombatDoesNotBreakTheFight`**: P1 and P2 in the forest, start a wolf combat (`StartGroupCombat` with Whisperwood's wolf group id from `WorldDb`), unplug B on P2's turn, wait 300 frames with P1 fuzz input. Assert no errors are logged (fixture default), and when it's P2's turn while disconnected the combat auto-ends P2's turn after 10 seconds unscaled (`"Player 2's controller is disconnected; skipping their turn."` narrated).
- [ ] **Step 4: Run and confirm they fail.**
- [ ] **Step 5: Implement** Disconnected, the banner, the rebind-on-Start, and the combat turn skip (in `CombatTurns`, where a human unit waits for input).
- [ ] **Step 6: Run the PlayMode suite and confirm it passes.**
- [ ] **Step 7: Commit** `"Co-op: controller disconnect banner, rebind with any Start, combat skips a disconnected player's turn"`.

### Task 4: Splitting up and shared flows

**Files:**
- Test: `Assets/Tests/PlayMode/CoopTests.cs`
- Modify: whichever runtime file a failing test points at (expected: none or small fixes in `GameRoot`, `CombatInstance`, `InventoryScreen`)

**Interfaces:**
- Consumes: Tasks 1–3 helpers. Uses the existing `g.Warp`, `g.StartGroupCombat`, `g.BeginLongRest`, `g.TempleRevive`, `InventoryScreen` trade (`_partner` when standing within range with nothing else open), and `PlayerSlot.Combat`.

- [ ] **Step 1: Test `PlayersInDifferentZonesEachSeeTheirOwnWorld`**: P1 in town, P2 warped to `"forest"`. For 300 frames assert each camera's position lies within its own `Zone.WorldBounds`, and that P2 moving with `Hold` changes only P2's MapPos.
- [ ] **Step 2: Test `OneFightsWhileTheOtherExploresThenWalksIn`**: P1 starts a forest wolf combat, P2 in town. Assert the clock still ticks (P2 not busy). Warp P2 to within 5 tiles of the fight; wait 2 frames; assert `p2.InCombat && p2.Combat == p1.Combat`.
- [ ] **Step 3: Test `BothRestTogether`**: both ready in `"home"`. P1 `BeginLongRest`: assert a "Waiting for" screen for P1 and the day not yet advanced. P2 `BeginLongRest`: run frames until the day advances (max 900). Assert both are at full HP and the spell slots restored.
- [ ] **Step 4: Test `PartyWipeWakesBothAtTheTemple`**: set both characters `HP = 0` and dead via a lost combat (`OnCombatFinished(c, false)`), wait. Assert both avatars are in zone `"temple"` and copper is reduced by 10%.
- [ ] **Step 5: Test `TradeMovesItemsAndGoldBetweenPartners`**: both in town within 1 tile; open P1's `InventoryScreen`. Assert the partner panel is present; move one `potion_healing` with the trade API used by quick-transfer; use Give to send gold. Assert counts on both bags.
- [ ] **Step 6: Run; fix whatever fails at the root cause (superpowers:systematic-debugging); rerun until the full suite passes.**
- [ ] **Step 7: Commit** `"Co-op: tests for splitting up, joining fights, resting, wiping and trading (plus fixes)"`.

### Task 5: Two-player fuzz, checklist, build

**Files:**
- Test: `Assets/Tests/PlayMode/CoopTests.cs`
- Modify: `docs/PLAYTEST.md` (a new "Co-op checklist" section)

- [ ] **Step 1: Test `TwoPlayersFuzzEveryZone`**: two FuzzInputs (seeds 31, 32); for every zone id in `WorldDb.Zones`, warp both (P2 one tile away) and run 600 frames at timeScale 4. Assert `g.InGame` at the end and no errors.
- [ ] **Step 2: Run the whole PlayMode suite plus the rules tests; both pass.**
- [ ] **Step 3: Add the PLAYTEST.md co-op checklist** (manual steps for the couch): join with Start on the 2nd controller; walk apart into different areas; one fights while the other shops, then walk in and join; trade a potion; unplug a controller and replug it; leave from the pause menu and rejoin; sleep together; save, quit, load, and rejoin.
- [ ] **Step 4: Rebuild** `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.BuildWindows`; confirm `Builds/EmbersOfSundermere/Embers of Sundermere.exe` and the zip timestamps updated.
- [ ] **Step 5: Commit** `"Co-op: two-player fuzz across every zone; co-op playtest checklist"`.
