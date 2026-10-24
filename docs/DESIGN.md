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
- Hybrid: real-time exploration → enemies notice you → roll initiative → turn-based on a 5-ft grid.
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
- XP from: defeating monsters (SRD values, split), resolving fights peacefully (same XP), quests,
  first-time skill-check successes (DC × 2, ×3 on a nat 20), discovering places (15).
- Ordinary monster groups respawn after 3 in-game days; story encounters don't.

## Inventory
- 36-slot Stardew-style bag (top row = hotbar), per-item stack limits, split/merge/swap, quick-transfer,
  sort, trading items and gold with a nearby co-op partner, home storage chest.
- Magic items are lore-tied and modest (uncommon at most early); max 3 attuned items.

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
