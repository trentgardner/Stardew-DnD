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
- **Level cap by act** (Act II Phase 1): Act I stops at level 4 (XP keeps piling up); Act II raises the cap to 10 and you level one step at a time
  through the level-up screen. ASIs at 4/8 (fighter also 6, rogue also 10); Extra Attack at 5 for barbarian, fighter, monk, paladin and ranger
  (and a warlock with Thirsting Blade). All 12 classes have their SRD 5.2 features to 10; the ledger in the Act II plan lists every simplification.

## Levels 5-10: curated spells (Act II Phase 1)
Not every SRD spell fits a 2D grid fight, so each class gets a hand-picked list (at least two per spell level):
- **3rd**: Fireball, Lightning Bolt (a 100-ft line), Spirit Guardians (an aura that follows the cleric), Mass Healing Word, Beacon of Hope, Haste
  (lethargy when it ends), Slow, Hypnotic Pattern, Call Lightning (call again each turn, no slot), Sleet Storm, Fly, Blinding Smite,
  Lightning Arrow, Conjure Barrage, Revivify.
- **4th**: Greater Invisibility, Ice Storm, Wall of Fire (a burning line that stays), Banishment, Death Ward, Staggering Smite, Dimension Door,
  Guardian of Faith (a fixed guardian with 60 damage to give).
- **5th**: Cone of Cold, Mass Cure Wounds, Hold Monster, Flame Strike (fire + radiant), Greater Restoration, Banishing Smite, Swift Quiver,
  Synaptic Static.
- **Left out on purpose**: summons (Conjure Animals, Find Steed in combat), Polymorph, Counterspell and Dispel Magic (monsters don't cast yet),
  Raise Dead (the temple and Embers of Return cover it), Hunger of Hadar (tone).
- **Stand-ins**: Devotion paladins get Aid + Lesser Restoration at 5 and Beacon of Hope + Revivify at 9; Circle of the Land at 5/7/9 gets Arid
  Fireball / Wall of Fire / Cone of Cold, Polar Sleet Storm / Ice Storm / Cone of Cold, Temperate Lightning Bolt / Greater Invisibility / Hold Monster.
- Lingering spells (Spirit Guardians, Wall of Fire, Guardian of Faith) show as tinted tiles and hit once per turn when a creature enters or
  ends its turn there; they vanish when concentration ends.

## Balance: at-will damage per round, levels 5-10 (vs AC 15)
From `tools/RulesTests` (best weapon or cantrip, Extra Attack, Sneak Attack, a monk's bonus strike; no spell slots, no rage or smites):
| Level | Top | Martials | Warlock | Full casters |
|---|---|---|---|---|
| 5 | barbarian 14.3 | monk 14.2, ranger 13.2, rogue 12.1, fighter/paladin 11.5 | 10.8 | wizard 7.7, sorcerer 7.2, druid 6.3, cleric 5.9, bard 4.2 |
| 7 | rogue 14.9 | barbarian 14.3, monk 14.2, fighter 13.8, ranger 13.2, paladin 11.5 | 10.8 | druid 8.9, wizard 7.7, sorcerer 7.2, cleric 5.9, bard 4.2 |
| 10 | rogue 21.0 | monk 18.5, barbarian 17.9, ranger 16.6, fighter/paladin 14.7 | 13.9 | druid 11.0, wizard 8.8, sorcerer 8.3, cleric 6.8, bard 4.9 |
Every martial (and the warlock) stays above half the leader; full casters above a fifth (their 3rd-5th level spells carry the rest).
The Lore bard's Vicious Mockery is RAW the weakest damage cantrip. Fixed while building this: cantrips now scale at 5 in the numbers, and
Eldritch Blast fires two beams at 5 (2024) instead of one bigger one.

## Levels 11-13: curated spells and rulings (Act III Phase 1)
- **4th (gaps for rangers and Devotion paladins)**: Freedom of Movement, Stoneskin.
- **6th**: Chain Lightning (one bolt + 3 leaps, only onto foes), Disintegrate (nothing on a save), Sunbeam (a 60-ft line, call it again each turn),
  Heal (70 HP, cures Blinded/Deafened/Poisoned), Blade Barrier (a lingering wall), Heroes' Feast (out of combat), Irresistible Dance,
  **Withering Circle** (Circle of Death, renamed for tone), Eyebite (the Panicked option only: Frightened).
- **7th**: Fire Storm (every foe within 20 ft of a point, up to ten; allies untouched, standing in for "choose the cubes around your friends"),
  Delayed Blast Fireball (12d6 at once: no delay), **Final Word** (Finger of Death, renamed), Forcecage (a 2x2 cage, one CHA save to slip out,
  legendary creatures can't be caged), Regenerate (4d8+15, then 1 HP each turn), Resurrection (out of combat: every fallen friend, full HP).
- **No gold cost** for Heroes' Feast or Resurrection (materials abstracted).
- **Mystic Arcanum** is its own once-per-Long-Rest use, cast at the spell's level; never a Pact slot, never refreshed by a Short Rest.
- **Left out on purpose**: summons, Teleport, Plane Shift, Etherealness, Reverse Gravity, Simulacrum, Contingency, Mass Suggestion, Prismatic Spray.
- **Use Magic Device** (thief 13) is text only: anyone can already read the game's scrolls, and there are no charged items yet.
- **Crown monsters** (SRD stat blocks, Crown names): Crown Knight (Knight, CR 3, Parry +2), Crown Champion (Gladiator, CR 5, Parry +3),
  Crown Battlemage (Mage without Counterspell, CR 6: Arcane Burst ×3, Fireball recharge 5-6 at range), Stone Guardian (Stone Golem, CR 10:
  Magic Resistance, Slow), Iron Sentinel (Animated Armor, CR 1).

## Balance: at-will damage per round, levels 11-13 (vs AC 17)
| Level | Top | Martials | Warlock | Full casters |
|---|---|---|---|---|
| 11 | rogue 20.4 | fighter 19.2, monk 17.9, barbarian 15.6, ranger 14.7, paladin 12.8 | 17.9 | druid 12.7, wizard 11.6, sorcerer 10.7, cleric 10.1, bard 7.4 |
| 12 | monk 21.3 | rogue 20.4, fighter 19.2, barbarian 15.6, ranger 14.7, paladin 12.8 | 21.3 | druid 12.7, sorcerer/wizard 11.6, cleric 10.1, bard 7.9 |
| 13 | rogue 24.1 | monk 22.9, fighter 20.6, barbarian 16.8, ranger 15.7, paladin 13.8 | 22.9 | druid 13.6, sorcerer/wizard 12.4, cleric 10.8, bard 8.4 |
The paladin's number leaves out Radiant Strikes (+1d8 on every melee hit) and smites; every martial and the warlock stay above half the leader.

## The grown dragon (Act III Phase 2)
- Stage **Young** (`Dragonling.IsGrown`): AC 17, HP 20 + 8 per party level, Bite 1d10+4 and Claw 1d6+4 at PB + 4, Ember Breath a 30-ft cone of
  ceil(level/2)d6 fire (recharge 5-6). Kindle, Shield Wing, pet/fetch/treats/trust are unchanged. `CanFly` is true, but flight is one finale moment.
- **Balance rule (user: "2-3d10 where a fighter rolls 4d10")**: expected at-will damage vs AC 17 is 50-75% of the average martial's.
  Measured: L11 10.7 vs 16.8 (64%), L12 10.7 vs 17.3 (61%), L13 11.5 vs 19.0 (61%). RulesTests `GrownDragonTests` enforces it.
- **"Read again"** (for Act III's reveal): `LoreDef.RevealText`, shown under an entry with "★ Read again" once `met_her` is set. Text that may only
  exist after the meeting lives in `*.AfterTheDoor.cs` files, which the secret guard skips and which must gate on `MetHer`.

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

## Into Act II (Act II Phase 2)

- **Acts**: `WorldState.Act` (saved). Act II sets `PlayerCharacter.LevelCap` to 10 (also on load; an Act I game started afterwards is back to 4), and companions with banked XP catch up.
  `GameRoot.BeginActTwo` = a season passes (+28 days), the dragon grows up, the Act I end card counts as seen. The title screen can start a new game straight in Act II at level 5 (the dragon is named there).
- **The teenage dragon** (`DragonStage.Wyrmling`): HP 10 + 6 × party level, PB grows with the party, bite 1d6+2 / claw 1d4+2 at +PB+2 (both swings on one Attack).
  Tuned to about 45–50% of a fighter's damage (a test keeps it between 30% and 60%): useful, never fight-carrying.
- **Dragon trust** (World/DragonDb.cs): an opinion per NPC (Distrust / Wary / Neutral / Fond / Trust) that the story can shift. Starting opinions: trusts Elowen, Lark, Maddie, Ssark; wary of Dunstan, Wick, Mister Thimble; distrusts Perrin.
  Shown as emotes, barks and a journal line. Within 4 tiles in a conversation: trust → Advantage on Persuasion/Deception/Performance, distrust → Advantage on Insight (the roll subtitle says why).
- **The four forces** (Quests/FactionDb.cs): the Crown, the Starfall Academy, the Fae Courts, the Wyrmwatch (working name). Every force is right about something.
  Standing per force (the journal shows word moods once you have any); 1–2 scouting quests each; then a plain-words prompt offers to commit. Committing closes every other force's quests, in-progress ones included (shown greyed: "Closed: you stood with …").
  The prompt goes to whoever triggers it; a co-op partner gets a narration line.
- **Blight** (World/Blight.cs): stages 0–3, set by the story and eased one step by the volcano. Per-place weight: wild 0.6, Thistledown 0.5, the warrens 0.3 (the egg-song holds it back), interiors 0.2.
  An ash haze over the whole zone (alpha 0.25 × stage × weight), drifting ash from stage 2, lights at half radius from stage 1 (cold hearths and forges), and NPC lines change.
- **Lore** (Quests/LoreDb.cs, World/WorldDb.Lore.cs): each entry has a MinAct (default 2). **Act I stays light** (user, 2026-10-31): no legend, no Academy texts, no dragon-weakness lore before Act II;
  only two ominous chest notes (A Miner's Scrawl in the mine's east chest, A Watchman's Lamp Log in the watchtower chest). Legend spots are hidden until their act.
  Readable spots add journal entries (Lore page, by category: The Legend, Records, The Pact, Places), 25 XP each, once for the party.
  The legend is told the way the world tells it. **The secret** (who the Queen really is, and who is behind the legend) is guarded by a test that scans the code and lore text for give-away words.

## The road and the Academy (Act II Phase 3)

- **Gated warps** (`WarpDef`): `MinAct` (the Lantern Road is closed in Act I: Act I stays light), `RequiresFlag` / `ForbidFlag` (the warded door; the looping Endless Stacks), `ClosedText`, `Narration`.
- **`ZoneDef.DragonStays`**: the dragon refuses to follow into the Sealing Hall. It waits upstairs, rejoins you by any route out, and a save made inside the hall loads with it waiting outside.
- **`ZoneDef.MagicLit`**: the Academy's nights are a soft violet, not dark (the wards). **`BlightWeight` 0.15**: the wards hold the ash back.
- **`SecretDef.FromHour/ToHour`** (the thirteenth bell, 00:00–01:00) and **`SecretDef.Lore`** (a secret that records lore). **`EnemyGroupDef.TalkSkill`**: beasts calmed by another skill (Nature for the blighted vines).
- **Map chars** (Academy set dressing):

  | Char | What it is |
  |---|---|
  | `z` | bookshelf |
  | `#` | hedge |
  | `+` | ward pillar (shimmers) |
  | `^` | floating stone (bobs) |
  | `&` | the Grand Orrery (2.6× outdoors, 3×3 footprint) |
  | `$` | the Starwell (can host a secret) |
  | `!` | telescope |
  | `-` | desk |
  | `<` | rune floor (walkable, shimmers) |

  Arcane props get a soft halo (`ArcaneHalo`) that brightens at night.
- **Zones:**

  | Zone | Notes |
  |---|---|
  | `lantern_road` | |
  | `academy` | |
  | `glasshouse` | |
  | `observatory` | |
  | `library` | Endless Stacks loop (29,2) → (2,2) until `stacks_unlooped`; warded door (33,2) needs `restricted_pass` |
  | `restricted` | dark; guardians vouch on `thorne_pass_shown` |
  | `sealing_hall` | dark, DragonStays |
  | `ashby_tower` | |

- **Main quest "Answers"** (`Answers` constants), with StageXp 400 at the Academy and 600 once Ashby is heard:

  | Stage | Name |
  |---|---|
  | 1 | Road |
  | 2 | Academy |
  | 3 | Tower |
  | 4 | Heard |

  It starts on Begin Act II, on Start at Act II, or on loading an Act II save without it (`ApplyActCap`, which also sets the `act2` world flag for schedules).
- **Ashby:** every ending choice sets `ashby_heard`. The academy standing changes are:

  | Choice or check | Academy standing |
  |---|---|
  | "You could be right." | +10 |
  | "You're talking about a cage." | −5 |
  | "Keep away from our dragon." | −5 (and dragon bond +2) |
  | Arcana DC 15 success | +5 |
  | Persuasion DC 16 about the hall (only after seeing it) | +5 |
- **Side quests:**

  | Quest | XP | Other rewards |
  |---|---|---|
  | `overdue` | 600 | 40 gp; the pass sets `restricted_pass`, `thorne_pass_shown` and `stacks_unlooped` |
  | `glasshouse` | 700 | 2 ember-lily petals (heal 3d4+3) |
  | `measure_wyrm` | 500 | star chart; dragon bond −3 / +2 / −1 by choice |

- **Phase 6 hook:** `World.LoreFound.Contains("emberhorn_warden")` (the Restricted Section's bestiary) unlocks the volcano dragon's weakness check.

## The four forces (Act II Phase 4)

- **Hearing them out:** `Factions.Hear(f)` sets `heard:<f>` the first time each leader is properly met (Marsh, Ashby when you've heard him out, Sorrel, Calloway). The journal shows "Heard out: n/4" and each heard force's belief.
- **The choice:** `Factions.MajorOpen(f)` = uncommitted, `HeardAll()`, and one of f's non-major quests done. Each leader's "We'll stand with…" option calls `Factions.StartMajor(by, questId)`, which shows the `OfferCommit` prompt; yes → `Commit` (once only: a second yes from another prompt is refused with a toast) → the `committed` flag → the major starts. `QuestLog.CanStart` closes every other force's quest, in progress or not.
- **"Answers"** stage 4 now reads "hear out the four forces"; stage 5 (`Answers.Chosen`, 1000 XP) fires on the `committed` flag.
- **Engine bits:**
  - `QuestEvent.BrazierLit` + `QuestTrigger.LitAtLeast(zone, from, to, n)` (Cold Lanterns).
  - `UnlitBrazier.MarkLit` (a lit torch lights it too); map char `)` = a cold road lantern post.
  - `SecretDef.SucceedsIfLit` (the Ember throne) and `EnemyGroupDef.ResolvedFlag` (the Frost guardians win the Frost throne).
  - A quest spot whose `SetsFlag` is already set disappears (alternate routes close once one works).
  - `ZoneDef.Dusk` (the glade's fixed gold-and-violet tint, `PlayerHud.DuskTint`).
  - Props on outdoor flagstones (`P a F K y )`) get paving under them.
- **Quests and XP** (each player):

  | Quest | Force | Kind | XP | Standing |
  |---|---|---|---|---|
  | Counted | Crown | scouting | 1200 | +10 |
  | Two Copies | Crown | scouting | 1000 | ±5/10 (your call) |
  | Under the Crown's Banner | Crown | major | 3000 | +30 |
  | Cold Lanterns | Academy | scouting | 1000 | +10 |
  | The Ninth Circle | Academy | major | 3000 | +30 |
  | A Quarrel of Courts | Fae | scouting | 1100 | +5 / +15 |
  | The Green Bargain | Fae | major | 3000 | +30 (−5 if you strike the clause) |
  | Ember Eyes | Wyrmwatch | scouting | 1200 | +10 |
  | The Long Hunt | Wyrmwatch | major | 3000 | +30, egg and bell ±5–10 |
  | Answers stage 5 (choosing) | | | 1000 | |

  Plus group XP (every fight can be talked down for the same XP): wagon hounds 2100, the beacon 2500, the cinder bear 200, the thorn guardians 900, the Cragfoot wolves 600, the Ashmaw 2300.
- **New monsters:** `ashsworn_zealot` (Cult Fanatic, no spells), `ashsworn_keeper` (Veteran), `cinder_bear` (Brown Bear), `thorn_guardian` (Awakened Tree, trimmed), `ember_eyed_wolf` (Dire Wolf), `ashmaw` (Wyvern: the stinger is one 4d6+4 poison roll, since the engine has no damage-plus-save rider).
- **Zones:** `redoubt`, `glade` (via the forest fairy ring, `ring_open`), `cragfoot`, `ashmaw_lair` (Dark). All `Act = 2` behind `MinAct = 2` warps.

## The blight and the road (Act II Phase 5)

- **Blight follows the story** (`Blight.StoryStage`, synced on every quest change, on load and at the start of Act II):

  | Stage | When |
  |---|---|
  | 0 | Act I |
  | 1 | Act II begins |
  | 2 | a side is chosen (Answers ≥ 5) |
  | 3 | on the Emberhorn Road (Answers ≥ 7) |

  `BlightEased` (the volcano, Phase 6) keeps it one lower for good. A hand-set stage is overridden by the story.
- **`EnemyGroupDef.MinBlight`**: `blight_wolves_forest` (2), `blight_boars_ford` (2), `blight_hounds_crater` (3). All are peaceful-capable wild groups that respawn.
- **`QuestDef.StageFlags`**: reaching a stage sets a flag. Answers 6 sets `emberhorn_open`.
- **`SecretDef.RequiresFlag`**: a secret that's only there once you know something (the Second Ideal's Religion route needs `secret:tower_inscription`).
- **Worried townsfolk:** `DialogueDb.Blight.cs` swaps each regular's usual opening line at stages 1 and 2. Five can tell the shared dream (lore at three tellers). Buoy leans at stage 2. The teenage dragon barks in places with an overlay ≥ 0.15, once a day per place.
- **Ash on the Doors** (900 XP): three marks → a night stakeout → Teodor, with four endings (redeemed ×2, jailed, fled). His schedule follows his fate.
- **Answers 6–8:**

  | Stage | Reached by | XP |
  |---|---|---|
  | 6 | any `banner_<force>` | 500 |
  | 7 | entering `emberhorn_road` | |
  | 8 | the gate spot | 1500 |

  Phase 4 saves with a finished major catch up on load.
- **Allies at the Last Camp** (one gift for the party, flag `ally_gift`):

  | Side | Ally | Gift |
  |---|---|---|
  | Crown | Holloway | 3 Greater Healing |
  | Academy | Perrin | Ember of Return + 2 Greater Healing |
  | Fae | Thimble | 3 Bloom Draughts |
  | Wyrmwatch | Odo | 2 Greater Healing + the road's secrets sparkle |

- **The Second Ideal** (`oath_second`): every long rest grants each player Heroic Inspiration.

## The volcano (Act II Phase 6)

- **Boss engine** (rules layer, RulesTests `BossTests`):
  - `MonsterDef.LegendaryActions` / `LegendaryPerRound`: taken right after another creature's turn via `CombatInstance.LegendaryOpportunities`, one per opportunity, refilled on its own turn. Kinds: an Attack (one of its attacks) or a SaveBurst on one creature.
  - `MonsterAbilityDef.Telegraph`: it spends a turn drawing breath (red tint, a warning to everyone). It breathes at the start of its next turn (`ReleaseBreath`), and that is its whole turn.
  - `MonsterDef.WeakSpotFlag`: with that world flag set, a hit while it draws breath chokes it (`TryChoke`). The breath is lost, it takes 4d10 force, and it has no legendary actions left that round.
  - `PhaseTwoAt` / `PhaseTwoText` / `PhaseTwoRechargeMin` / `LairEruptions`: at half HP it narrates, recharges faster, and the floor erupts at the start of each round (`PickEruptionTiles`/`Erupt`: DEX DC 15, 2d10 fire, walkable tiles near heroes).
  - `Scale`: the sprite is drawn bigger; the footprint stays one tile.
  - Dragon's wings: allies within 5 ft of the fire-immune party dragon get Advantage on fire-breath saves. **The party dragon is now immune to fire** (SRD red dragon).
- **The Warden** (`warden`): SRD Young Red Dragon base.

  | Stat | Value |
  |---|---|
  | AC / HP | 18 / ~220 |
  | Rend | ×3, +10, 2d6+6, reach 10 |
  | Tail | +10, 2d8+6, reach 15 |
  | Grey Breath | 30 ft cone, 10d6 fire, DC 17, telegraphed |
  | Legendary actions | 2 (Tail Sweep, Searing Glare 3d6 DC 15) |
  | Phase two | at 50%: Recharge 4–6, 3 eruptions/round |

  Balance check: a level-9 party of four needs a median of 5.4 rounds of at-will damage (slowest 8.0). It deals about 39/round before its breath.
- **The talk** (`warden_parley`): each approach once (Persuasion 18 / Animal Handling 15 with the dragon / Insight 14 only with the `emberhorn_warden` lore). After two, or "draw your weapon", it's the fight.
  - Boons: `warden_hesitated` (enemies surprised), `warden_spares_dragon` (phase one only), `warden_weakness` (kept after a wipe).
  - A wipe resets the boons and the tries.
- **Ally boons** at the Heart (`ally_boon`, once): Holloway +15 temp HP, Perrin fire resistance for the fight (removed when it ends), Thimble Heroic Inspiration, Odo a "Marked" effect (Advantage on the first attack).
- **Victory** (`WardenFalls`): `warden_fallen`, `Blight.Ease()` once, Answers 9 (`Answers.Rest`, 3000 XP), lore `warden_last_words`.
- **Zones:**

  | Zone | Notes |
  |---|---|
  | `emberhorn_throat` | Dark |
  | `emberhorn_heart` | Dark, lava-lit, braziers |

  Lava is map char `9`.

## The Act II pass (Act II Phase 7)

- **XP audit** (`ActTwoXp`, held by `ActTwoPassTests`). Per player, on top of the 6500 you start Act II with:

  | | Solo | Co-op |
  |---|---|---|
  | Main path to the gate | 57655 | 48980 |
  | + the Warden | 69555 | 57930 |
  | Everything to the gate | 69681 | 58631 |

  Targets: level 9 (48000) by the gate on the main path; level 10 (64000) when the Warden falls; completionists ≤ 64000 × 1.3. Co-op is the binding case, since fights split XP.
  The single tuning knob is "Answers" StageXp:

  | Stage | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
  |---|---|---|---|---|---|---|---|---|
  | XP | 3000 | 2000 | 6000 | 8000 | 6000 | 4000 | 6000 | 6000 |

- **The End of Act II card:** `ActEndScreen(p, 2)`, once per playthrough (`act2_end_seen`), 3 s after the Warden falls, on every player's screen. It also shows on loading an older save past the Warden. It has a line for the side you stood with.
- **The secret guard** (`LoreTests`): it scans the player-facing strings of every line about the Queen, the Ashsworn, the Warden, the waking or the door (Snik the Egg-Warden excepted). `SecretGuardCatchesALeak` proves the matcher works: before Phase 7 it held a literal backspace and never matched anything.
- **Balance table** (RulesTests `ActTwoBalanceTable`): median rounds for a 4-class party at the expected level.

  | Fight | Level | Rounds |
  |---|---|---|
  | road boars | 5 | 3.0 |
  | Restricted guardians | 6 | 2.8 |
  | Cragfoot wolves | 6 | 2.8 |
  | beacon Ashsworn | 7 | 4.5 |
  | the circle's bears (now three) | 7 | 2.0 |
  | Frost guardians | 7 | 2.0 |
  | Ashmaw | 7 | 2.6 |
  | Grey Vigil | 8 | 3.4 |
  | salamanders | 9 | 3.7 |
  | elemental | 9 | 1.9 |
  | Warden | 9 | 5.4 |

## Buoy (dedication)

Buoy is Thistledown's neighbourhood dog: a black-and-white Australian Shepherd, a tribute to a real dog. He is NOT a companion and never fights.
He wanders his patch by day and naps on the porch at night. His weekly routine: South Lane most days, outside the Sleeping Drake on Hearthday, and Miller's Ford on Leafday.
Walking up to him gives Pet / Look at his collar ("BUOY" on a brass tag). The first pet each day grants Heroic Inspiration.
**He must stay in the game for its whole length.** When Acts II and III add a new home base, point `TownDog.HomeFor` at it (code: Assets/Scripts/Game/World/TownDog.cs; art: npc_buoy_0/1 in ArtMap.txt).
