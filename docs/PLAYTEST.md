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

## New in Phase 7 (Act I, start to finish)
- **The end of Act I**: after Captain Vale's thanks you get an "End of Act I: Home" card: your days in Thistledown, quests done, your dragon's bond, a glimpse of Act II. Then you keep playing in Thistledown.
- **Main-story milestones pay XP** (following the trail, finding the warrens, reaching the egg: 100 each), so the story alone gets you to level 3 by the egg, and doing a fair bit of side content gets you to level 4 around the hatching.
- The narrator now marks your first nightfall and your first level-up.
- Behind the scenes: a test plays the whole main story (with a save and reload in the middle), an XP audit keeps the pacing honest, and a balance check confirms no class falls far behind in damage.

### Phase 7 checklist
- [ ] Play the main story start to finish. Were you level 3 at the egg? Level 4 around the hatching if you did side quests?
- [ ] After turning in to Captain Vale, does the End of Act I card feel like an ending (and a "to be continued")?
- [ ] Anything in Act I that felt too long, too short or confusing?

## New in Phase 6 (the baby dragon, and Buoy)
- **The hatching** is a little scene now: the egg glows and wobbles, cracks, flashes, and the hatchling tumbles out (any button skips it after a second). Then you **type its name** (or press "Suggest another"; empty = Cinder).
- **The dragon belongs to the whole party.** It follows whichever player is nearest, and either player can walk up to it ("Play with ...") for its menu:
  - **Pet** it (heart bubble), **play fetch** (it chases a stick and brings it back), **give a treat** (Smoked Pepper Jerky, 5 cp from Pip or the general store).
  - **Sniff around**: hidden spots within about 8 steps start to sparkle, and their next check has **Advantage**. Once per rest, shared by the party.
  - **Light that brazier**: cold braziers in the mine and the warrens light up for good. It can also **light your torch**.
  - **Wait here / Come along**, and **in fights: your orders or its own**.
- **Bond** (♥ hearts in the journal and its menu) grows with petting (once an hour), fetch (once an hour), treats and fights won together. It never goes down.
- **In fights** it takes its own turn: Move, Bite (+4, 1d4+2), **Ember Breath** (a puff of fire right in front of it: 1d6, DEX save for half; comes back on a 5–6 at the start of its turn), Dodge, Auto, End Turn. If either player is in the fight, they can give it orders.
- **It never dies.** At 0 HP it squeals and bolts for cover, then pops back out after the fight, still shaky. It sits out fights until you rest (short or long), which also fixes its HP and its nose.
- Townsfolk notice it (Pip, Brannoc, Elowen, Perrin, Garrick each have a line), and Thistledown reacts the first time you bring it home.
- **Buoy**, a black-and-white Australian Shepherd, is the neighbourhood dog. Find him on South Lane most days, by the Sleeping Drake's door on Hearthday, and splashing at Miller's Ford on Leafday. Pet him (the first pet each day gives Heroic Inspiration) and read his collar.

### Phase 6 checklist
- [ ] Hatch the egg: does the scene feel special? Is the name screen easy (keyboard and controller)?
- [ ] Pet, fetch, treat. Do the hearts go up? Wait 20 seconds without moving: does it nap (Zz)?
- [ ] Sniff in Whisperwood near a hidden spot, then search it: the dice tray says "Advantage (Sniff)".
- [ ] Light a cold brazier in the mine, save, reload: still lit?
- [ ] Fight with the dragon under your orders and on Auto. Breathe on two wolves. Let it get knocked out: it should flee, not die.
- [ ] Co-op: walk apart. Does the dragon pick the nearer player without jittering between you?
- [ ] Find Buoy. Pet him, read his collar.

## New in Phase 5 (new places)
- **The Fallen Star is a real journey now.** Captain Vale sends you to **Starfall Crater** (Whisperwood's north trail,
  top edge of the forest). Study the empty hollow, then either follow the little clawed tracks or stake out the market
  stalls after dark to catch the thief in the act. It leads down the old mine into the **Kobold Warrens**: a whole
  village with a gate, a stream, Tik's Shiny Stall (he pays full price for trinkets) and Elder Ssark. The egg now waits
  one level deeper, in the **Shrine of the Great Egg**.
- **Three ways through the warrens**: make friends (Snik vouches for you if you caught him kindly; the Elder blesses you
  for a shiny, good words or Snik's say-so), sneak (a crack beside the gate, a crawlway around Krag's warband), or fight.
- **The Old Watchtower** (north edge of Miller's Ford, east of the bridge): a Zelda-style puzzle ruin. Levers, a pressure-plate
  room (push the block, or have a friend or a waiting companion stand on the second plate) and a light-and-mirrors stair.
  Gates stay open once solved. The last watchman at the top asks for the watchword.
- **The Harvest Festival** on **Day 10** in the square, 9 am to 10 pm: apple bobbing, archery, the great pie contest and the
  bard stage. Missed it? It comes back every 7 days until you've been. Tip: sleep at home to skip ahead to Day 10.

### Phase 5 checklist
| Try this | Where | What should happen |
|---|---|---|
| The crater | Whisperwood, walk off the top edge on the north trail | Perrin the scholar, fire beetles, the hollow (Investigation), the tracks (Survival), 3 secrets |
| The stakeout | Market stalls on the square, after 8 pm, once you've studied the hollow | Catch Snik with Pip's ladle: talk, joke, grab, or scare him |
| Warrens, friendly | Down the mine stairs (where the egg used to be) | With Snik befriended the gate waves you through; Elder Ssark blesses you; Krag steps aside |
| Warrens, sneaky | Same | Acrobatics through the crack beside the gate; Athletics through the crawlway around Krag |
| Warrens, by force | Same | Gate guards and Krag's warband are normal fights (Krag is tough at level 1-2) |
| Skip everything | Walk straight down the mine | The quest jumps to the right step; the egg still works |
| Watchtower solo | Ford, north edge east of the bridge | Levers (read the plaque with History), push the block onto a plate and stand on the other, flip both mirrors |
| Watchtower co-op | Same | One player (or a companion told to wait) holds the second plate |
| The watchman | Top of the tower | Read the inscription first, then recite it: no fight needed |
| The festival | Square, Day 10 from 9 am | Play three games for the quest; each game once per player per festival |

## New in Phase 4 (quests and town)
- **Thistledown grew south**: the **South Lane** has Garrick's forge (weapons and armor), Captain Vale's watch house,
  Lark's cottage, a little green, and two beekeepers glaring at each other across a footbridge.
- **Every building has a sign** by its door: walk up to see its name, press Interact to read it.
- **A weekly rhythm**: Coinday is market day (stalls on the square), Hearthday morning everyone goes to the temple service,
  Emberday evening is story night at the Drake, and Captain Vale drills the watch on Stormday mornings.
- **12 new quests** (below). Every one can be solved without a fight, and peaceful endings pay the same XP.
- Repeated messages now stack ("Already at full health ×3") instead of filling the screen.
- The journal lists active quests (with who gave them) first, then a one-line list of completed ones.

### Phase 4 quest checklist
| Quest | Where it starts | Ways to finish |
|---|---|---|
| Bad Blood at the Bridge | Captain Vale: "Anything need doing?" | Hear Hesper and Dunstan out, then mediate (Persuasion; Insight on both gives Advantage) or pick a side |
| Wolves on the Road | Captain Vale (same chat) | Deal with Whisperwood's wolves any way (talk, scare, fight), then collect the bounty |
| A Ring in the Pond | Tamsin (pond, footbridge): "You look worried" | Dive (Athletics) or fish (Survival) at the pond's edge, give the ring back, watch the proposal |
| Brannoc's Secret Poems | Brannoc, once you're friends or have heard about his poetry | Deliver the poem to Mother Elowen; reveal the poet or keep the secret |
| Pip's Missing Barrel | Pip: "You look upset" | Search behind the Drake (Investigation), find Wick in the orchard; mercy (job), justice (cell), or pay |
| Bar Brawl at the Drake | Pip, Coinday after 6 pm | A fistfight: no magic, no weapons, nobody dies. Win or lose, you wake up at 1 HP |
| Goats Gone Astray | Old Tobin: "Need a hand?" | Coax three goats home in Whisperwood (Animal Handling) |
| The Haunted Mill | Old Hobb, after the Ford quest | Listen at night by the mill, calm the owlbear cub, take it home to the den by the lake (or face its mother) |
| Lost Child in the Woods | Starts on its own: a Tideday afternoon after day 3 | Track Maddie in Whisperwood (Survival, Perception) before 8 pm, or the watch finds her |
| Mother Elowen's Herbs | Elowen: "Can I help?" | Moonpetal (Whisperwood), riverleaf (the ford), hearthroot (South Lane green), then bring them back |
| The Fae Trickster | Mister Thimble at the forest lake, evenings (not Hearthday) | Answer three riddles (Arcana gives hints); 2+ right wins a trinket, XP either way |

## New since playtest 3
- **Real art and music** (Ninja Adventure pack, CC0): characters, monsters, tiles, items, and music for every zone plus fight music.
- **All 12 classes**: Rogue, Barbarian, Monk, Paladin, Ranger, Druid, Sorcerer and Warlock join Fighter, Wizard, Cleric
  and Bard. Each gets its subclass at level 3. Druids can **Wild Shape** into a wolf, cat or frog (bear at level 4).
- **Level 4** is the new cap, with an Ability Score Improvement or a feat.
- **You choose your level-ups**: open the character sheet (C) when it says LEVEL UP. Roll for HP or take the average,
  then pick your new spells, skills and features.
- Tavern recruits can be any class, and companions now use their class tricks on their own (rage, healing hands, marks).
- Co-op hardening: a second player can drop out and rejoin, and an unplugged controller is handled instead of breaking the game.

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
- Character creation: all 12 classes, 5 species, 4 backgrounds,
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
- A few sprites are still Claude-made fill-ins where the art pack had nothing (see `ART_IMPORT_GUIDE.md`).
- Lore names are pending your approval (`LORE_PITCH.md`).
- The road past Miller's Ford is blocked by a fallen tree (Port Varrel comes later).
- Level cap is 4 for this playtest (the end of Act I).
