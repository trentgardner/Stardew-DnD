# Inventory Upgrades: Design

Date: 2026-10-30. Status: the user approved the feature list and handed the details over ("I'm cool with whatever you think is smoothest. BG3 does it really well").
Calls marked **[Claude's call]** were made without explicit user input.

## Intent

The user played keyboard and mouse only and found the inventory unintuitive: "hot swapping or seeing what I have selected was non-existent", and
"having to manually drag everything sucked". Stacks and splits were fine. The goal is a BG3-smooth inventory: you always know what's selected,
common actions take one click or one key, and nothing needs dragging unless you want to drag.

## Goals

1. **Selection you can see.** The selected hotbar slot is unmistakable: a bright raised frame, and the item's name shown above the hotbar (fading after 2 s).
   The same treatment on the hovered or cursor cell in the inventory, with the info panel always describing it.
2. **Hotbar controls (KB/M parity with the controller).** Mouse wheel or **Q / R** move the selection; **X** uses the selected item; **1–0, -, =** still
   use a slot instantly (unchanged). **Left-click a hotbar slot uses it, right-click opens its options** (BG3). A small key hint sits under the hotbar.
3. **Weapon hot-swap.** A second weapon set (BG3-style melee/ranged): equipment gains an **Off-set weapon** slot. **Z** (KB) / **hold X** (pad; a tap of X still uses the selected hotbar item) swaps
   the two sets [Claude's call: F is already Interact and every pad button is taken]. Outside combat: free. In combat: free once per turn (it's your free object interaction), shown as "Swap Weapons" on the action bar.
4. **Compare before you equip.** Hovering or selecting gear shows its stats against what's equipped in that slot, with green/red differences:
   weapons: average damage and to-hit; armor/shields: AC; other gear: its effect line. "(equipped)" on the item you're wearing.
5. **No forced dragging.** Every move has a one-click/one-key form:
   - double-click / Y: use or equip (exists);
   - **Shift+click / R3+A: send to the other panel** (exists); with no other panel open it **moves the item to the hotbar** (or back to the backpack if it's on the hotbar);
   - right-click options gain **Add to hotbar**, **Send to <party member>** (each companion and a nearby co-op partner), **Mark as junk / Unmark**.
6. **Party bags side by side.** The second panel gets **tabs for each party member** (your companions always; your co-op partner when within 3 tiles), so you can
   see their bags and drag or Shift+click items across. A chest, loot or home storage still takes the panel when one is open (party tabs come after it).
7. **Sort and filter.** Tabs above the backpack: **All · Gear · Use · Treasure · Quest**. A filter dims non-matching items rather than moving them
   **[Claude's call: dimming keeps positions stable, so muscle memory and the hotbar are never disturbed]**. **Sort** (T / View) orders backpack slots
   (never the hotbar row) by kind, then name.
8. **Junk.** Mark items as junk (a small coin-bag badge). Treasure items you've never marked start unmarked **[Claude's call: nothing is junk by default]**.
   Shops show **"Sell all junk (N items, X gp)"** at the top of the sell list. Junk marks are saved per item id per player.
9. **Controller.** **Hold A** on an item opens a radial wheel (Use/Equip, Hotbar, Send to..., Junk, Split, Drop) navigated with the stick and released to pick;
   a tap of A keeps today's pick-up/place. LB/RB still switch panels/tabs.

## Non-goals

- Weight/encumbrance changes, new items, crafting, shops redesign beyond "sell all junk".
- A separate hotbar storage (the hotbar stays the backpack's top row).

## Anti-goals

- Anything that moves items without the player asking (no auto-sorting the hotbar, no auto-equipping).
- A second way of doing something that behaves differently from the first (drag, Shift+click and the options menu must agree).
- Breaking saves: old saves load with no off-set weapon and no junk marks.

## Constraints

- Every window closes with Esc/B and a ✕; tests run muted; dice-always-visible is unaffected.
- Co-op: two players can have inventories open at once; moving items into a partner's bag must respect their space (existing QuickTransfer rules).
- 2024 rules: swapping a weapon in combat uses the free object interaction (once per turn).
- Existing tests for inventory behaviour (drag, split, right-click options, give potion, trade) must keep passing.

## Settled while reviewing the design [Claude's calls]

- **The shield belongs to the main set.** Swapping to the off-set stows the shield (no shield AC) and swapping back re-straps it, so a sword-and-board
  fighter can swap to a longbow (BG3 behaviour). Equipping a two-handed weapon *into the main set* still requires taking the shield off, as today.
- **Hotbar clicks never move your character.** A click that lands on the HUD hotbar is consumed by it (the same pointer-over-UI rule the combat bar uses).
- **Filter groups:** Gear = weapons, armour, shields, wondrous items and gear; Use = consumables (potions, scrolls, food, torches, treats); Treasure =
  treasure; Quest = quest items. All = everything.
- **Junk storage:** a per-character list of item ids (saved with the character), so co-op partners keep separate junk marks.
- **Off-set weapon storage:** `PlayerCharacter.OffSetWeapon` (saved; null on old saves), plus `ShieldStowed`.

## Left to the builder

Exact layout and colours (match UIKit), the radial wheel's look, which keys show in footer hints, test names.
