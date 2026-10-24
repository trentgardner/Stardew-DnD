# Playtest Guide: Alpha 0.1

## How to launch

**Option A: the built game (easiest, works on the laptop too)**
1. Open `Builds\EmbersOfSundermere\` and double-click **`Embers of Sundermere.exe`**.
2. For the laptop: copy `Builds\EmbersOfSundermere.zip` over (USB stick or OneDrive), unzip anywhere, run the exe.
   Nothing to install. Alt+Enter toggles fullscreen.

**Option B: inside Unity (for poking around)**
1. Open **Unity Hub** → **Add** → **Add project from disk** → pick `C:\Users\trent\Projects\ai_2D_DnD`.
2. Open it (Unity 6.5). Open `Assets/Scenes/Main.unity` (or any empty scene: the game boots itself).
3. Press **▶ Play**. Errors, if any, show in the **Console** tab: screenshot them for Claude.

## Controls (BG3-style)

| | Keyboard & mouse | Controller |
|---|---|---|
| Move | WASD | Left stick |
| Talk / interact / confirm / roll dice | **E** or click | **A** |
| Back / cancel | Esc | B |
| Inventory | I or Tab | Y |
| Character sheet (level up here!) | C | View |
| Spellbook | K | RT |
| Journal (quests, friendships) | J | LT |
| Pause / save / settings | Esc | Menu |
| Use hotbar item | 1-0 | LB/RB select, X use |
| Sneak | Ctrl | L3 |
| **Combat**: move/attack | click a tile / click an enemy | stick moves cursor, A |
| **Combat**: choose action | 1-9 or click the action list | LB / RB |
| **Combat**: end turn | Space | X |

**Co-op:** your fiance presses **START** on a second controller at any time. The screen splits and she makes a
character. You can go anywhere independently; if one of you starts a fight, the other can walk over and join it.

## What's in this build
- Character creation: 4 classes (Fighter, Wizard, Cleric, Bard), 5 species, 4 backgrounds,
  **rolled stats (4d6 drop lowest, 5 free rerolls)**, assign them yourself, skills, spells. Or Quick Start.
- **Thistledown**: 6 NPCs with daily schedules (Brannoc's shop is open 8am-6pm, closed Hearthday),
  4 interiors, a day/night clock (30-min days by default).
- **Dialogue with real dice**: Persuasion for discounts, Insight to read people, role-only options
  (Bard performances, Faithful prayers...). Hidden DCs, nat 20/nat 1 house rules, 24-hour lockout on failed approaches.
- **Recruitable adventurers** at the Sleeping Drake tavern (randomly generated each week).
- **The Old Copper Mine**: dark (bring torches or darkvision!), 4 encounters, 2 chests, and Snik the Egg-Warden,
  whom you can **fight or talk down**.
- **Turn-based combat**: initiative, a grid, action/bonus action/reaction, attack rolls on the HUD,
  **you click to roll damage**, weapon masteries, spells, Shield and Cutting Words reactions, death saves.
- The egg, a **dragon companion** you name, and 3 quests.
- Inventory with stacking, splitting, trading with your partner, equipment, weight; shops and haggling.
- Rests (short with hit dice, long in beds), exhaustion, temple revival, Ember of Return.
- Multiple playthroughs, each with autosave + manual saves (saves sync through OneDrive if you have it).

## Playtest checklist: things to try
**Solo, ~30-45 minutes**
- [ ] Do a **full** character creation. Did rolling stats feel good? Was anything confusing?
- [ ] Talk to everyone. Try a Persuasion check on Brannoc. Fail one and try again (you should be locked out).
- [ ] Buy torches. Check the inventory: drag, split (right-click / X), sort (T).
- [ ] Get the quest from Captain Vale (by the notice board in the morning).
- [ ] Go into the mine at night vs. day. How does the darkness feel?
- [ ] Fight the first kobolds. Does the click-to-roll damage feel good, or slow?
- [ ] Reach Snik: try talking your way through. Then reload and try fighting.
- [ ] Hatch the egg and name the dragon. Bring it to Captain Vale.
- [ ] Level up from the character sheet (roll your hit die!).
- [ ] Save, quit, start a *second* playthrough, then load the first one again.

**Co-op with your fiance**
- [ ] Join with a second controller mid-game.
- [ ] Split up: one of you shops while the other heads to the mine.
- [ ] Trade an item and some gold (open inventory while standing next to each other).
- [ ] Fight together; let one of you go down and get picked back up (Healing Word, potion, or Medicine check).
- [ ] Both go to bed to end the day.

## What feedback helps most
- Anything that **broke** (a screenshot of the error, or "I did X, then Y happened").
- Anything that felt **slow or fiddly**, especially combat pacing and the dice rolls.
- What made you **laugh** or **care**. That tells me what to do more of.
- Numbers that felt off: prices, damage, how fast the day goes, XP.

## Known placeholders and limits (expected!)
- Art and sound are generated placeholders. See `ART_IMPORT_GUIDE.md`.
- Lore names are pending your approval (`LORE_PITCH.md`).
- Level-up choices (Bard Expertise, etc.) are picked automatically for now.
- Level cap is 3 for this playtest.
