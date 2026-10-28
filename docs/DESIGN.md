# Design Decisions (living document)

Agreed between the designer (Trent) and Claude. Change a decision here first, then in the game.

## Pillars
1. **It's a D&D table.** Current rules (2024 / SRD 5.2), not BG3 rules. Dice are never hidden: you roll them.
2. **Stardew heart.** Top-down pixel town, NPCs with schedules, friendships, a home, days that pass.
3. **Your story.** A main track (the dragons waking) with full player autonomy over how it ends.
4. **Couch co-op first.** Split screen for two (up to four), keyboard/mouse and controllers mixed freely.

## Dice and rules
- Skill checks in dialogue and the world: the player presses to roll a visible d20. DCs are hidden; the margin is revealed after.
- Combat: the attack roll shows on the HUD automatically; on a hit **the player clicks to roll damage**.
- Weapons deal damage **dice** (no flat damage) and carry 2024 **weapon masteries** (Sap, Vex, Topple, Graze...).
- Nat 20 / nat 1 on ability checks: Critical Role house rule (best outcome / mishap). Impossible tasks aren't offered as rolls.
- Crits double the damage dice. HP on level-up: player chooses roll or average (Critical Role style).
- Retrying: physical tasks can be retried; a failed social approach locks for 24 hours (2024 Influence rule).
- Heroic Inspiration (Humans each long rest; reroll). Fennick's Lucky Copper: 1 reroll per long rest.
- No flanking. A grid appears only during combat.

## Characters
- Playtest: Fighter (Champion), Wizard (Evoker), Cleric (Life), Bard (Lore), levels 1-3.
- Species: Human, Elf, Half-Elf (custom 2024-style), Dwarf, Gnome. Backgrounds: Acolyte, Criminal, Sage, Soldier.
- Stats: 4d6 drop lowest ×6, 5 free full rerolls, player assigns. Background gives +2/+1 or +1/+1/+1.

## Combat
- Hybrid: real-time exploration → enemies notice you → **the DM asks what you do** → turn-based on a 5-ft grid if it comes to blows.
- **Being spotted never forces a fight** (playtest 2 feedback). An encounter choice opens: Persuasion / Deception (people),
  Animal Handling (beasts), Intimidation (scare them off), Stealth vs their best passive Perception (slip away), or fight.
  Failed social approaches lock for 24 h (2024 Influence rule) and start the fight. Story groups (Snik, Wren) use their own conversation.
- **Sneak** (Ctrl / L3): enemies roll your Stealth against their passive Perception. Get close unseen and press Interact to
  **ambush**: the enemies are surprised (Disadvantage on initiative, 2024 rules).
- **Retreat** (combat action): Athletics or Acrobatics vs DC 10 + enemies standing (max 15). Success ends the fight with no
  winner; the group loses track of you for an hour and keeps its wounds. Downed allies are carried out (stable).
- **Companions are yours to command** (BG3 style): on a companion's turn you get its action bar. "Auto" lets it decide for one
  turn; talking to it switches it to full AI, sets its role (front line / hang back / its call) or tells it to wait somewhere.
  AI front-liners go for the enemy nearest the most hurt ally; others keep to range.
- **Formation**: companions follow in a loose formation (spread sideways, staggered), never on one tile. A companion told to
  **take point** walks ahead; enemies can spot them (unless you're sneaking), which starts the encounter with them in front.
  When a fight starts, front-liners and point-takers step between their leader and the nearest enemy.
- **Potions on allies**: Give Potion to Ally (Bonus Action, within 5 ft, 2024 rules; works on downed allies). Out of combat,
  right-click a potion → "Give to ...", or talk to the companion.
- Co-op partner keeps exploring in real time and can walk into the fight to join (rolls initiative on arrival).
- Monsters don't finish off downed players (lighter tone). Death saves are real.

## Death and difficulty
| | Story | Normal | Hard | Hardcore |
|---|---|---|---|---|
| Ember of Return | 25 gp | 50 gp | 150 gp, use within 10 rounds / 10 min | 300 gp, same limit |
| Revive side effect | — | — | +1 Exhaustion | +1 Exhaustion |
| Party wipe | wake at temple | wake at temple (−10% gold, +1 day) | same | **save deleted** |
| Extra | long rest revives the fallen | — | temple revive costs 100 gp | single save, only when sleeping |

## Time and survival (Critical Role style)
- 24-hour day = **30 real minutes** by default (20/30/45/60 in settings). The clock stops during combat, dialogue, dice and menus.
- Weekdays matter (shop closed on Hearthday). Seasons and weather come later.
- Food is automatic in town; rations matter when camping in dungeons. Skipping sleep → CON saves vs Exhaustion.
- Light matters: torches burn for 1 hour; darkvision lets you see dimly; enemies without darkvision only see your light.
- Encumbrance: 2024 rules (STR × 15 lb); over capacity = speed 5 ft. **Coins weigh nothing.**
- Short rest anywhere safe; long rest in a bed (home free, tavern 5 sp, temple pews) or risky camping (watch roll).

## Progression
- Official 2024 XP curve (300 / 900 / 2,700...).
- XP from: defeating monsters (SRD values, split between **players only**), resolving fights peacefully (same XP),
  quests, first-time skill-check successes (DC × 2, ×3 on a nat 20), discovering places (15).
- **Every playstyle pays the same** (playtest 2): talking a group down, calming it or scaring it off gives exactly the XP
  *and* the loot roll of defeating it (befriended groups share it, fleeing ones drop it). Encounter checks don't add the
  skill-check bonus on top, so the totals match. Sneaking past gives nothing yet: the group is still there.
- Companions never take a share. They earn whatever their leader earns and level up on their own.
  (Before this fix, a companion halved your combat XP, which is why the first playtester never reached level 2.)
- Ordinary monster groups respawn after 3 in-game days, whether they were beaten, befriended or scared off; story encounters don't.

## Inventory
- 36-slot Stardew-style bag (top row = hotbar), per-item stack limits, split/merge/swap, quick-transfer,
  sort, trading items and gold with a nearby co-op partner, home storage chest.
- Mouse: drag-and-drop or click to move; **right-click opens an options menu** listing everything you can do with the item
  (use, equip, give a potion to an ally, move to hotbar/backpack, send, split); double-click uses/equips. Controller: X opens it.
- Every window closes with Esc / B, its own key again, or the ✕ in its corner. A key that closes a window can't also open
  another one on the same frame (playtest 2: Esc closed the inventory and opened the pause menu).
- The HUD shows XP toward the next level, a "Level up!" reminder, and your companions' / partner's HP under your own.
- Magic items are lore-tied and modest (uncommon at most early); max 3 attuned items.

## World
- Hand-shaped, varied maps (not a grid): Zelda / Skyrim / BG3-style places worth poking around in.
  Each area should mix landmarks, a fight or two, and secrets.
- **Secrets** (`ZoneDef.Secrets`): walk up and roll a skill (Perception, Investigation, Performance...). Success
  rewards once (or once a day); failure can be retried the next day.
- **Miller's Ford** (east road): a flood-broken bridge, the "Ford Wardens" charging a toll at the ford upstream (talk them into
  honest work, scare them off, pay, sneak, or fight), Old Hobb's idle mill, a boar meadow, three secrets, quest "The Broken Bridge".
- Maps live in `Maps.cs` as text. Town, Whisperwood and Miller's Ford were painted by a small generator script, then pasted in.
  The play-mode test `WorldDataIsConsistent` checks warps, spawns, NPC spots and secrets after any edit.

## Story
- Setting pitch in `LORE_PITCH.md`: **not approved yet**; names in the build are placeholders.
- Tone: Matthew Mercer worldbuilding + Brennan Lee Mulligan heart and humor. Bright high fantasy, dragons,
  PG-13 romance (Stardew level), no gothic or undead overlords.

## Saves
- Multiple playthroughs. Each: one autosave (zone changes, sleeping) + unlimited manual saves.
- Saves live in OneDrive when available (shared between the desktop and the laptop).

## Tech
- Unity 6.5, C#. The rules engine (`Assets/Scripts/Rules`) has no Unity dependency and is unit-tested (`tools/RulesTests`).
- All art and sound are looked up by name with generated fallbacks (`ART_BRIEF.md`, `ART_IMPORT_GUIDE.md`).
- Proprietary license; SRD 5.2 used under CC-BY-4.0 (`NOTICE.md`).
