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
- All 12 SRD classes, levels 1-4 for Act I, one subclass each at level 3: Fighter (Champion), Wizard (Evoker),
  Cleric (Life), Bard (Lore), Rogue (Thief), Barbarian (Berserker), Monk (Open Hand), Paladin (Devotion),
  Ranger (Hunter), Druid (Circle of the Land), Sorcerer (Draconic), Warlock (Ember Patron, our own patron).
- Level-up is the player's choice: roll HP or take the average, then every choice the level brings (skills, expertise,
  spells, fighting style, invocations, metamagic...), a summary, and confirm. Level 4: ASI (+2 / +1+1) or a feat.
  Companions level up automatically.
- Level-1 class choices in creation: Fighting Style (fighter), Divine Order (cleric), Primal Order (druid: Magician =
  extra cantrip, Warden = medium armor + martial weapons), the first Eldritch Invocation (warlock).
- Druid Wild Shape (level 2): a beast form keeps your own HP, gains temp HP = druid level, uses the beast's AC, speed,
  STR/DEX/CON and attacks, and blocks spellcasting. CR 1/4 forms at 2 (wolf, cat, frog), CR 1/2 at 4 (black bear).
  Dropping to 0 HP ends the form.
- Tavern recruits can be any class. Companion AI uses class bonus actions (Second Wind, Lay on Hands, Rage,
  Hunter's Mark, Hex) before moving and attacking.
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

## Puzzles, passages, vouching and the festival (Phase 5)
- **Puzzle layer** (`World/Puzzle.cs`, `ZoneDef.Puzzle`): pieces on map tiles. Plates (held by any player, companion,
  enemy or block standing on them; downed creatures don't count), levers, push blocks (one tile away from the pusher, only onto
  free floor: never onto a creature, warp, door, gate or another block), mirrors (`/` and `\`), a light source and crystals
  (lit when the beam's last tile is on or next to them), and gates that open when every `OpensWhen` condition holds (`!id` = off).
  Pure logic lives in `PuzzleLogic` (`TraceBeam`, `GateOpen`) so it's unit-tested. Latched gates are world flags
  (`puzzle:{zone}:{id}`, saved); everything else resets when the last player leaves the zone, so a stuck block never soft-locks.
  A piece with a `Hint` offers a dice-tray check first (once found, the hint is everyone's; a failure locks it until midnight).
- **Passages** (`SecretDef.PassageTo`): a successful check moves you and your companions through (a crack, a crawlway);
  once found, it's a free walk-through. `SecretDef.Dialogue` opens a conversation on success (catching Snik in the act).
- **Vouching** (`EnemyGroupDef.VouchFlag/VouchText`): if a friend spoke for you, the group stands down on sight
  (full peaceful XP, same as talking them down).
- **Harvest Festival** (`World/Festival.cs`): `Festival.IsOn` (day 10, then every 7 days until the quest is done, 9:00 to 22:00)
  drives a world lock `"festival"` set on the hour tick (never while a fight is on in town). While it runs, an NPC's
  `RequiresLock = "festival"` schedule entry beats everything, festival dressing appears, the town plays `music_festival`,
  and four `FestivalGame`s are open (once per player per festival day). Each game is real dice: Constitution checks,
  shortbow attack rolls (DEX + proficiency), Insight/Persuasion in the pie contest, a Performance check for tips.
  `QuestTrigger.FlagsAtLeast` finishes the festival quest after any three games.
- **Main quest stages** use `FallenStar` constants (Crater, Trail, Warrens, Egg, Hatched, Done). Save version 2 renumbered
  them; `SaveSystem.Migrate` converts version 1 saves on load.

## Quests (Phase 4)
- Quests are **data** in `QuestDb` (`Assets/Scripts/Game/Quests/`): stages with journal text, **triggers** that move them
  (zone entered, group resolved, item gained, flag set, secret found; `AllFlags` for "collect all three") and a **reward**
  paid exactly once on the last stage (`QuestLog.Paid` is saved; old saves count finished quests as paid).
- Talk-driven stages are set from dialogue (`Quests.Set`). Stage numbers are part of saves: never renumber an existing quest.
- **Quest spots** are `SecretDef`s with `QuestId`/`QuestStage`: they appear only at that stage, can sit on any walkable tile,
  can be night-only, give no "secret" XP (the quest pays), and can advance the quest, set a flag or settle an enemy group.
- **Quest-gated groups** (`EnemyGroupDef.QuestId/MinStage/MaxStage`) appear and leave with the story.
- **Quest items** (`ItemDef.QuestItem`) can't be sold, never get lost on a full bag, and count from a partner's bag or home storage.
- NPC **schedules** can depend on the story: `RequiresFlag`, `ForbidFlag`, `RequiresLock` (e.g. two nights in the cell).
  At the same hour: story-conditional beats weekday beats everyday.
- Timed events (`GameRoot.StoryTick`, every hour and on waking) always resolve: missing a deadline never soft-locks a quest.
- **Brawls** (`EnemyGroupDef.Brawl`): fists and improvised weapons only, no magic or items, 2024 knockout rule
  (`PlayerCharacter.Nonlethal`: 0 HP = unconscious and stable), companions sit out, and losing is not a party wipe.

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

## The baby dragon (Phase 6)

- Rules: `Dragonling : Monster` (Rules/Dragonling.cs). AC 13, HP 5 + 5 × party level (10 at L1, 25 at L4), Bite +4 1d4+2, resists fire, darkvision 60.
  Ember Breath: the engine's 5-ft cone (up to 3 squares facing the target), 1d6 fire, DEX save DC 8 + PB + CON (= 11) for half, recharge 5–6 rolled at the start of its turn.
  Growth hooks only: `DragonStage { Hatchling, Wyrmling, Young, Adult }`, `CanFly` (Young+).
- It never dies: at 0 HP it "retreats, scared" (WorldState.DragonScared): it leaves the grid, sits out fights, and comes back at full HP on any short or long rest (GameRoot.OnPartyRest).
  It doesn't count as "players still up", so it can't keep a lost fight going alone. It joins at most one fight, if it's within 12 tiles of a player in it.
- Owned by the party: follows the nearest ready player (same zone first, 3-tile hysteresis); in a fight, its leader gives orders, or any player in that fight if the leader isn't.
- Pet layer (World/DragonPet.cs): menu, pet/fetch (+2/+3 bond once per in-game hour per player), treats (+5), fights won (+1), bond 0–100 shown as 0–5 hearts. Sniff: once per rest, secrets within 8 tiles sparkle and get Advantage. Cold braziers (map 'y') light for good (WorldState.LitBraziers).
- Saved: name, damage taken (old saves load at full HP), scared, bond, sniff used, lit braziers.

## Buoy (dedication)

Buoy is Thistledown's neighbourhood dog: a black-and-white Australian Shepherd, a tribute to a real dog. He is NOT a companion and never fights.
He wanders his patch by day and naps on the porch at night. His weekly routine: South Lane most days, outside the Sleeping Drake on Hearthday, and Miller's Ford on Leafday.
Walking up to him gives Pet / Look at his collar ("BUOY" on a brass tag). The first pet each day grants Heroic Inspiration.
**He must stay in the game for its whole length.** When Acts II and III add a new home base, point `TownDog.HomeFor` at it (code: Assets/Scripts/Game/World/TownDog.cs; art: npc_buoy_0/1 in ArtMap.txt).
