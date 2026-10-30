# Inventory Upgrades Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A BG3-smooth inventory: visible hotbar selection with KB/M parity, weapon-set hot-swap, compare-before-equip, one-click moves (no forced dragging), party bags side by side, filter tabs, junk + "sell all junk", and a controller hold-A wheel.

**Architecture:** Rules gain the data (off-set weapon + stowed shield, junk marks, hotbar moves) with RulesTests; the game layer gains a `HotbarView` upgrade in PlayerHud, an `ItemCompare` text helper, `InventoryScreen` tabs/filters/options, a `RadialMenu` screen, and a "Sell all junk" row in ShopScreen. Tests drive real input through a scriptable `TestInput` (new, in the test assembly) and the existing `VirtualPad`.

**Tech Stack:** C# Unity 6000.5.9f1, tools/RulesTests (`dotnet run`), `.superpowers/unity-test.sh [filter]`.

**Spec:** docs/superpowers/specs/2026-10-30-inventory-upgrades-design.md

## Global Constraints

- Nothing moves without the player asking (no auto-sorting the hotbar row, no auto-equip). Sort never touches slots 0–11.
- Drag, Shift+click and the options menu must agree on what a move does.
- Old saves load: no off-set weapon, shield not stowed, no junk marks.
- Swapping weapons in combat = the free object interaction, once per turn (2024 rules); outside combat it's free.
- Every window closes with Esc/B and a ✕; existing inventory tests keep passing; tests run muted.

## Review Focus

- **Two-handed off-set with a shield:** swapping to a two-handed off-set weapon stows the shield (AC drops by 2) and swapping back restores it; equipping a two-handed weapon into the main set still refuses with the shield on. Test: `SwapToBowStowsTheShield` (Task 2).
- **Full bags:** "Send to" a companion whose bag is full moves what fits and says so; nothing is lost. Test: `SendToFullBagKeepsTheRest` (Task 4).
- **Hotbar click vs world click:** clicking a hotbar slot never also walks the character there. Test: `HotbarClickDoesNotMoveYou` (Task 1).
- **Junk that's equipped or a quest item:** quest items can't be marked junk; "sell all junk" never sells equipped gear. Test: `SellAllJunkSkipsQuestAndEquipped` (Task 7).
- **Co-op inventories at once:** both players open inventories and send items to each other in the same second; counts add up. Test: `BothPlayersTradeAtOnce` (Task 5).

---

### Task 1: Hotbar you can see, KB/M parity

**Files:** Modify `Assets/Scripts/Game/UI/PlayerHud.cs` (hotbar build/refresh, `HotbarSelectedName`, key hint, click handling), `Assets/Scripts/Game/World/Actors.cs` (PlayerAvatar hotbar input: Q/R + X for KB/M too), `Assets/Scripts/Game/Core/PlayerInput.cs` (nothing new: reuse `NextTab`/`PrevTab`/`AltAction`); Create `Assets/Tests/PlayMode/TestInput.cs` (scriptable `PlayerInput`: settable one-frame presses, mouse position, UsesMouse), `Assets/Tests/PlayMode/InventoryUpgradeTests.cs`.
**Interfaces:** Produces `PlayerHud.HotbarIndex` (exists), `string PlayerHud.HotbarLabel` (the name shown above the bar, "" when faded), `int PlayerHud.HotbarSlotAt(Vector2 screen)` (-1 if none), `bool PlayerHud.PointerOverHotbar(PlayerInput)`.
- [ ] Tests: `QAndRMoveTheSelectionAndXUsesIt` (TestInput with UsesMouse: R → index 1 and HotbarLabel = that item's name; X with a potion selected and HP < max → potion used); `HotbarClickUsesTheSlot` (left-click on slot 0's screen rect with a torch → torch lit); `HotbarRightClickOpensOptions`; `HotbarClickDoesNotMoveYou` (avatar position unchanged after the click); `SelectedSlotHasARaisedFrame` (selected cell scale > 1 and outline enabled).
- [ ] Run → fail; implement (selected cell: gold outline + 1.15× scale; label fades after 2 s; hint "Q/R select · X use · 1–0 quick-use" under the bar for KB/M, "LB/RB · X" for pad); run → pass; commit `Inventory: visible hotbar selection and KB/M hotbar controls`.

### Task 2: Weapon sets and hot-swap

**Files:** Modify `Assets/Scripts/Rules/PlayerCharacter.cs` (`WeaponDef OffSetWeapon`, `bool ShieldStowed`, `bool SwapWeaponSets(out string message)`, AC ignores a stowed shield, `EquipFromBag` gains target `"offset"`), `Assets/Scripts/Rules/Combat.cs` (`TurnState.ObjectInteractionUsed`), `SaveSystem.cs` (PcData `OffSet`, `ShieldStowed`), `PlayerInput.cs` (`abstract bool SwapWeapons`: KB `Z`; pad: hold X ≥ 0.4 s, tap X stays AltAction), `Actors.cs` (world swap), `CombatTurns.cs` (action "Swap Weapons", Free, once per turn), `InventoryScreen.cs` (second weapon cell "Off-set"), `tools/RulesTests/InventoryUpgradeTests.cs`, FuzzInput/TestInput overrides.
**Interfaces:** `PlayerCharacter.SwapWeaponSets(out string msg) : bool`; `PlayerCharacter.OffSetWeapon`; `PlayerCharacter.ShieldStowed`.
- [ ] RulesTests: `SwapExchangesMainAndOffSet`, `SwapToBowStowsTheShield` (fighter, longsword + shield, longbow off-set: AC −2 after swap, back to normal after the second swap), `SwapWithEmptyOffSetGoesUnarmed`, `TwoHandedIntoMainStillNeedsShieldOff`.
- [ ] Play tests: `SwapIsFreeOncePerTurnInCombat` (action enabled, then disabled with Why "You've already swapped this turn."), `WeaponSetsSurviveSaveAndLoad`, `ZSwapsOutsideCombat`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: weapon sets and hot-swap`.

### Task 3: Compare before you equip

**Files:** Create `Assets/Scripts/Game/UI/ItemCompare.cs`; modify `InventoryScreen.cs` (info panel), `ShopScreen` (buy list info).
**Interfaces:** `static string ItemCompare.Describe(PlayerCharacter pc, ItemDef item)`: rich text. Weapons: "1d8+3 slashing · +5 to hit (avg 7.5)" then "vs Longsword: <color=green>+2.0 avg</color>" (or red, or "(equipped)"); armour/shield: "AC 16 (+2)"; others: their description line.
- [ ] Tests (play mode, no UI needed): `CompareShowsDamageDifference` (fighter with longsword, greatsword → contains "+" and green), `CompareShowsArmourDifference` (leather vs chain mail → "AC"), `CompareMarksEquipped`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: compare before you equip`.

### Task 4: One-click moves

**Files:** Modify `Assets/Scripts/Rules/Inventory.cs` (`int ToHotbar(int slot)` → first free hotbar index or -1; `int ToBackpack(int slot)`), `InventoryScreen.cs` (Shift+click with no other panel toggles hotbar/backpack; options: Add to hotbar / Move to backpack, Send to <each companion and nearby partner>, Mark as junk (Task 7 wires the flag)).
- [ ] RulesTests: `ToHotbarUsesFirstFreeSlot`, `ToHotbarWhenFullReturnsMinusOne`, `ToBackpackLeavesHotbar`.
- [ ] Play tests: `ShiftClickMovesToHotbarWithNoOtherPanel`, `SendToCompanionFromOptions`, `SendToFullBagKeepsTheRest`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: one-click moves (hotbar, send to party)`.

### Task 5: Party bags side by side

**Files:** Modify `InventoryScreen.cs`: the second panel's tabs = [open container if any] + each companion + nearby co-op partner; tab strip with names; LB/RB or clicking a tab switches; drag/Shift+click across works as with containers.
**Interfaces:** `IReadOnlyList<string> InventoryScreen.Tabs`, `void InventoryScreen.SelectTab(int i)`, `Inventory InventoryScreen.OtherInv`.
- [ ] Tests: `CompanionBagShowsAsATab` (hire a companion, open inventory → Tabs contains their name; SelectTab → OtherInv is their bag), `ChestComesFirstThenParty`, `BothPlayersTradeAtOnce`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: party bags side by side`.

### Task 6: Filter tabs

**Files:** Modify `InventoryScreen.cs` (tabs All · Gear · Use · Treasure · Quest above the backpack; non-matching cells dim to 35 % alpha; T/View sort unchanged).
**Interfaces:** `enum ItemFilter { All, Gear, Use, Treasure, Quest }`, `static bool ItemFilters.Matches(ItemDef d, ItemFilter f)`, `InventoryScreen.Filter`.
- [ ] Tests: `FilterGearDimsPotions` (cell alpha check), `FiltersCoverEveryItem` (every ItemsDb item matches exactly one non-All filter), `SortNeverTouchesTheHotbar`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: filter tabs`.

### Task 7: Junk

**Files:** Modify `PlayerCharacter.cs` (`HashSet<string> Junk`), `SaveSystem.cs` (PcData `Junk`), `InventoryScreen.cs` (options toggle, badge `ui_junk` hand-drawn in ArtMap), `Screens.cs` ShopScreen (top row "Sell all junk (N items, X)" when selling and N > 0).
**Interfaces:** `PlayerCharacter.MarkJunk(string itemId, bool on) : bool` (false for quest items), `ShopDb.SellAllJunk(PlayerSlot p, string shop) : (int items, int copper)`.
- [ ] Tests: `MarkJunkAndSellAll` (two junk stacks sold, copper up by their sale price), `SellAllJunkSkipsQuestAndEquipped`, `JunkMarksSurviveSaveAndLoad`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: junk marks and sell all junk`.

### Task 8: Controller item wheel

**Files:** Create `Assets/Scripts/Game/UI/RadialMenu.cs` (a GameScreen: 6 wedges around the item, stick picks, release A or press A confirms, B cancels); modify `InventoryScreen.cs` (hold A ≥ 0.35 s on an item opens it; tap A unchanged); `CoopTests.VirtualPad` gains `Press(b)` / `Release(b)`.
**Interfaces:** `RadialMenu(List<(string label, Action act)> options)`, `RadialMenu.Highlighted`.
- [ ] Tests: `HoldAOpensTheWheel`, `StickPicksAndReleaseConfirms` (stick right → "Add to hotbar" highlighted; release → item on the hotbar), `TapAStillPicksUp`.
- [ ] Run → fail; implement; run → pass; commit `Inventory: controller item wheel`.

### Task 9: Acceptance

- [ ] PLAYTEST.md: "New: inventory" section + updated controls table (Z swap, Q/R/X hotbar, hold-X swap on pad, hold-A wheel). Screenshots: hotbar label, compare info, party tab, wheel.
- [ ] Full suite + RulesTests green; self-review; rebuild exe + zip; merge to main; push (user OK for pushes after testing).
