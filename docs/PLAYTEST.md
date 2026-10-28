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
| Back / close any window | Esc (or the window's ✕, or its own key again) | B |
| Inventory | I or Tab | Y |
| Character sheet (level up here!) | C | View |
| Spellbook | K | RT |
| Journal (quests, friendships) | J | LT |
| Pause / save / settings | Esc | Menu |
| Use hotbar item | 1-0 | LB/RB select, X use |
| Sneak (then walk up to an unaware enemy and interact to **ambush**) | Ctrl | L3 |
| Inventory: options for an item (use, equip, **give a potion to an ally**, split...) | right-click | X |
| Inventory: move items | drag, or click to pick up / click to drop | A |
| **Combat**: move/attack | click a tile / click an enemy | stick moves cursor, A |
| **Combat**: choose action | 1-9 or click the bar at the bottom (Spells / Actions open a small list; right-click closes it) | LB / RB |
| **Combat**: end turn | Space | X |
| **Combat**: a companion's turn | you get their action bar; **Auto** lets them decide | same |
| **Combat**: give a potion to an adjacent ally / retreat | Items ▴ / Actions ▴ | LB / RB |

**Co-op:** your fiance presses **START** on a second controller at any time. The screen splits and she makes a
character. You can go anywhere independently; if one of you starts a fight, the other can walk over and join it.

## New since playtest 2
- **Menus close properly**: Esc / B, the window's own key, or the ✕ in its corner. (Esc used to close a menu *and* open the pause menu.)
- **You don't have to fight**: when enemies spot you, the DM asks what you do: talk them down, calm animals, scare them off,
  slip away, or fight. Or sneak up and **ambush** them. Mid-fight you can **Retreat**.
- **Command your companions**: on their turn you get their action bar (or press Auto). Talk to them to let them fight on their
  own, give them a role (front line / hang back), or tell them to wait.
- **Take point**: tell a companion "Take point" and they walk ahead of you. Enemies spot them first, and when a fight starts
  they step in between you and the enemy. Front-line fighters and clerics do that step automatically.
- Companions keep their own spots in a loose formation now instead of stacking on one tile.
- **Companion HP** shows under yours. **Give potions to allies**: in combat (Items ▴, adjacent) or from the inventory (right-click a potion).
- **XP fixed**: companions were taking a share of combat XP (that's why you never hit level 2). Now they don't, and talking or
  scaring a group off gives exactly the same XP *and loot* as killing it. XP to next level shows under your HP.
- **Inventory**: right-click any item for a menu of what you can do with it; drag-and-drop; double-click to use.
- **New area: Miller's Ford** (east of town, past the notice-board sign): a broken bridge, a crew of "Ford Wardens" running a toll,
  a mill, boars, three secrets, and a new quest with five different ways to solve it.

## What's in this build
- Character creation: 4 classes (Fighter, Wizard, Cleric, Bard), 5 species, 4 backgrounds,
  **rolled stats (4d6 drop lowest, 5 free rerolls)**, assign them yourself, skills, spells. Or Quick Start.
- **Thistledown**: a river town with a bridge, a town square (dragon statue, well, market stalls), Tobin's farm,
  a pond and an orchard. 6 NPCs with daily schedules (Brannoc's shop is open 8am-6pm, closed Hearthday),
  4 interiors, a day/night clock (30-min days by default).
- **Whisperwood** (west road out of town): groves, a stream, old shrine ruins, a fairy ring, a goblin camp and
  a wolf den. **Hidden spots** everywhere: walk up and roll Perception / Investigation / Performance to find them.
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
- [ ] Explore Whisperwood. Can you find all 3 of its secrets (and 2 in town)? Dance in the fairy ring.
- [ ] Fight the first kobolds. Does the click-to-roll damage feel good, or slow?
- [ ] When something spots you, try NOT fighting: talk, scare, or slip away. Then try sneaking up for an ambush.
- [ ] Recruit someone at the tavern. Command them in a fight; then talk to them and set them to Auto. Which do you prefer?
- [ ] Hand a companion a potion (right-click it in your inventory). Retreat from a fight that's going badly.
- [ ] Head east to **Miller's Ford** and deal with the Ford Wardens however you like. Then tell Old Hobb.
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

## Co-op checklist (couch, two controllers)

Tick these off on the laptop and TV. If something feels wrong, note what you were doing.

1. **Join:** start a game, then press **Start** on the second controller. Player 2 makes a character and appears next to Player 1. Player 1's pause menu should NOT pop open.
2. **Split up:** walk into different places (one in the forest, one in town). Each half of the screen follows its own player.
3. **Fight while the other explores:** one of you starts a fight while the other shops. The shopper can walk over and join the fight.
4. **Trade:** stand next to each other and open the inventory (Y / I). Send a potion across, and give gold (X / G).
5. **Unplug a controller** mid-game, then plug it back in. You should see a "Controller disconnected" box, and then you get control back. Try unplugging during your turn in a fight: after about 10 seconds the game skips that turn instead of freezing.
6. **Leave and come back:** Player 2 opens the pause menu, picks **Leave the game**, then presses Start again later. Same character, same items.
7. **Sleep together:** both of you go to bed (home or the tavern). The night only passes once you're both asleep.
8. **Save, quit, load:** save, quit to the title screen, load the save, and Player 2 presses Start to rejoin.
9. **One of you falls:** if only one of you goes down in a fight, they wake at the temple and no day passes for the other.

## What feedback helps most
- Anything that **broke** (a screenshot of the error, or "I did X, then Y happened").
- Anything that felt **slow or fiddly**, especially combat pacing and the dice rolls.
- What made you **laugh** or **care**. That tells me what to do more of.
- Numbers that felt off: prices, damage, how fast the day goes, XP.

## Known placeholders and limits (expected!)
- Art and sound are generated placeholders. See `ART_IMPORT_GUIDE.md`.
- Lore names are pending your approval (`LORE_PITCH.md`).
- Level-up choices (Bard Expertise, etc.) are picked automatically for now.
- The road past Miller's Ford is blocked by a fallen tree (Port Varrel comes later).
- Level cap is 3 for this playtest.
