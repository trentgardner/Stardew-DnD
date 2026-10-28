# Act I Milestone ("Home"): Design

Date: 2026-10-28. Status: approved direction from the user ("go with whatever you think is best", "do it all without me").
Calls marked **[Claude's call]** were made by Claude without explicit user input and can be revisited.

## Intent

Turn the alpha into a complete, real-art **Act I** of *Embers of Sundermere*: a cozy, D&D-campaign-style opening
that the user and their fiancée can play together in local co-op from character creation to the egg hatching.
The hatching of the baby dragon is the act's climax and a "treat" for the fiancée, so it has to look and feel good.

## Whole-game frame (for context; only Act I is built in this milestone)

- Target length: about 30 hours of main story plus side content. Act I is about 8 hours, Act II about 12–15 and Act III about 8.
- Act I "Home" (levels 1–4): town life, small personal quests like a regular D&D campaign. The egg thread runs
  quietly underneath and ends in the kobold mine and the hatching.
- Act II (levels 5–10): the big bads arrive (Crown, Starfall Academy, Fae Courts, dragon hunters, Ashby).
- Act III (levels 11+): overcoming the big bad, power struggles, the endings.
- Lore: docs/LORE_PITCH.md is accepted "for the time being". The names can still change.

## Goals

1. **Co-op verified**: two players with split screen, controller and controller or KB/M and controller, through the whole of Act I.
2. **Real art**: the Ninja Adventure pack (Pixel-boy, CC0, 16x16) as the base, with its music and SFX.
   Gaps (the baby dragon, class looks, unique props) are hand-authored by Claude in code or PNGs that match the pack's palette and scale.
3. **All 12 SRD classes** playable to level 4, each with its SRD 5.2 subclass at level 3:
   Barbarian (Berserker), Bard (Lore), Cleric (Life), Druid (Land), Fighter (Champion), Monk (Open Hand),
   Paladin (Devotion), Ranger (Hunter), Rogue (Thief), Sorcerer (Draconic), Warlock (Fiend → reflavored as a
   "Patron" **[Claude's call: Fiend is the only SRD warlock subclass. Flavor it as a fae or dragon patron to fit the lighter tone]**), Wizard (Evoker).
4. **Real level-up choices**: the player picks HP (roll or average, already done), spells known or prepared, the subclass
   feature choices that exist, and an ASI or feat at level 4 (SRD feats: a small curated set).
   No more AutoChoices for players. The level cap rises from 3 to 4.
5. **Act I content**: around 15–20 small quests, an expanded town, and new places (see Content).
6. **Baby dragon companion** (see the Dragon section).
7. **XP stays as it is now** (combat, peaceful resolution, quests, first-time checks, discovery; respawns).
   The XP pacing is retuned so level 4 lands around the hatching.

## Non-goals

- Acts II and III, and the dragon growing up or flying (design hooks only).
- Online multiplayer.
- Species beyond the current five **[Claude's call: keep species as they are. Add Halfling, Dragonborn, Orc, Tiefling and Goliath later]**.
- Seasons, full weather, crafting, romance content.

## Anti-goals (a failure even if it "works")

- An overpowered dragon. It must be Us-level (BG3): useful, never fight-carrying.
- Big stakes in Act I. No dragon hunters, Ashby or courts beyond foreshadowing.
- Grid-like, symmetrical or empty maps (see player-tastes). Every place needs landmarks, a fight or two and secrets.
- Mixed or ugly art: no Kenney or placeholder sprites left visible in normal play.
- Breaking existing saves silently. Old saves may be declared incompatible, but the game must say so cleanly.

## The baby dragon (whole party's)

- Hatches at the Act I climax in the kobold mine. The party names it (text entry; a default is offered).
- Owned by the **whole party**: it follows the group (the nearest player in co-op), and either player can interact with it or command it.
- **Pet interactions** (like BG3's Scratch): pet, play fetch or chase, feed treats, react emotes (hearts, sparks), and it naps at rest spots.
  A bond value rises with interaction (light; no fail state).
- **World help**: "Sniff" reveals nearby hidden secrets and items (it highlights SecretInteractables within range, once per short rest),
  and it can light torches and braziers with a tiny flame.
- **Combat** (like BG3's Us): its own initiative and its own turn, commanded like the companions. Stats based on a
  tiny dragon (pseudodragon-ish): AC 13, about 10 HP that scales with party level, bite 1d4+2, and **Ember Breath**
  (5-ft cone, 1d6 fire, DEX save, recharge 5–6). If it drops to 0 HP it isn't killed: it "retreats, scared" until the next short rest.
- Dialogue lines and the narrator react to it. NPCs comment on it.
- Data hooks for growth stages (hatchling / wyrmling / young / adult) and a `CanFly` flag. Only the hatchling is implemented.

## Content: Act I "Home"

Tone: Mercer's lived-in world plus Mulligan's heart and humor. Small and personal.

**Places** (existing: Thistledown, Whisperwood, Miller's Ford, the Old Copper Mine, and the interiors):
- **Thistledown expanded**: more houses and interiors (blacksmith, Rhoswen's watch house, a couple of homes), and NPC schedules over the week.
- **Starfall Crater** (new): where the "star" landed. Scorched meadow, glowing crystals, gawkers, kobold tracks. Its own secrets.
- **The Old Watchtower** (new): a small Zelda-style puzzle ruin (switches, pressure plates, a light puzzle).
  It foreshadows the Oathbound with an inscription of the First Ideal; a minor magic item reward.
- **Kobold Warrens** (expanded mine): multi-level, with a kobold village, a shrine to the egg, and talk, sneak or fight routes everywhere.
- **Harvest Festival** in Thistledown: a timed event (a specific day of the calendar). Mini-games: an apple-bobbing CON check, an
  archery contest (attack rolls), a pie judging (Persuasion/Insight), and a bard stage.

**Quest list (about 18, mostly small)**. Each has a peaceful option where it makes sense; XP is the same either way.
1. The Fallen Star (main): the crater → the missing shiny things → the kobold tracks → the mine → the hatching.
2. Goats Gone Astray (the farm): find the goats in Whisperwood; Animal Handling.
3. The Copper Kettle (existing).
4. The Lucky Spoon (existing).
5. The East Road / Ford Wardens (existing).
6. Bad Blood at the Bridge: two families feuding over a fence; Insight/Persuasion mediation or picking a side.
7. The Haunted Mill: "ghost" noises at Hobb's mill turn out to be a lost owlbear cub → relocate it or fight its mother.
8. A Ring in the Pond: someone's engagement ring; a diving or fishing mini-check; a sweet payoff.
9. Brannoc's Secret Poems: deliver the dwarf's anonymous love poem; a choice to reveal him or keep the secret.
10. Pip's Missing Barrel: a smuggled barrel → a petty thief → mercy or justice.
11. Wolves on the Road (existing wolf den, made into a bounty).
12. Lost Child in the Woods: a timed search in Whisperwood; Survival or Perception; the fae-ring encounter (harmless).
13. The Watchtower Inscription: Rhoswen asks you to scout the ruin.
14. Harvest Festival contests (one quest with several mini-activities).
15. Mother Elowen's Herbs: gather from three zones; Medicine/Nature checks.
16. Bar Brawl at the Drake: an unarmed-strike "fun fight" (nonlethal, no deaths).
17. Snik the kobold (existing peaceful thread) becomes the kobold go-between for the main quest.
18. The Fae Trickster: a riddle game at the Veil-touched pond in Whisperwood; harmless; a trinket reward; foreshadows Act II.

**Quest system**: quests move out of hard-coded GameRoot `if` statements into data (`QuestDef` with stages, objectives,
triggers and rewards) **[Claude's call: needed to scale to about 18 quests safely]**.

## Art pipeline

- Claude downloads Ninja Adventure. If itch.io blocks the scripted download, the user downloads it once into `ArtSource/`
  (gitignored; CC0, so committing is legally fine, but the repo stays lean).
- The current 16 px pixels-per-unit is kept. A mapping table (pack sheet + frame → ArtRegistry id) is generated by an editor tool that
  slices the sheets into `Assets/Resources/Art/{id}.png`. **[Claude's call: slice into individual PNGs so the existing ArtRegistry contract is unchanged]**
- Characters: the pack's characters are mapped to class looks plus NPCs; any class without a good match gets a recolor or an edited sprite.
- Hand-authored by Claude: the baby dragon (idle, walk, sleep, happy, breath; 4 directions where needed), the dragon egg, the crater crystals, and the watchtower puzzle props.
- Music and SFX: the pack's tracks are mapped to zones and moments (town, forest, combat, mine, festival, hatching). The Sfx system plays the real clips.

## Constraints

- Unity, C#; the rules engine stays SRD 5.2 (2024) and engine-free (tools/RulesTests).
- The dice-always-visible pillar applies to every new mechanic (festival games, dragon breath saves, puzzle checks).
- Art remains swappable through ArtRegistry ids.
- All content stays data-driven (WorldDb, DialogueDb, QuestDb, MonstersDb).
- Every phase ends with the rules tests plus the play-mode tests green, screenshots reviewed, and the exe and zip rebuilt.

## Build order (each phase gets its own plan)

1. **Co-op hardening**: automated two-player tests (join, split up across zones, one in combat while the other explores,
   drop-in/out, item transfer, death/spirit, a long rest with both), and fixes. A co-op checklist is added to PLAYTEST.md for the user.
2. **Real art and audio**: import Ninja Adventure, the mapping tool, the replacements, music and SFX, and the hand-authored gaps.
3. **Classes and level-up**: the 8 new classes, cap 4, a level-up choice UI, the feats/ASI, and the new spells needed through level 2 slots.
4. **Quest system and town**: data-driven quests, the expanded Thistledown, NPC schedules, the small town quests.
5. **New places**: Starfall Crater, the Old Watchtower, the expanded Kobold Warrens, the festival.
6. **The dragon**: hatching cutscene, pet interactions, sniffing, combat, bond.
7. **Act I pass**: XP pacing, narrator lines, a full-run fuzz test, a balance pass, a playtest build, and the docs.

## Left to the builder

The exact map layouts, NPC dialogue text, item stats within SRD bounds, puzzle designs, music-to-zone mapping,
UI layout of the level-up screen (it must follow the existing UIKit look and close cleanly with Esc or B and a ✕ button).

## Needs the user (end of milestone only)

- A co-op playtest with real controllers (checklist in PLAYTEST.md).
- A "fun" verdict and feedback.
- Name approvals (anything in LORE_PITCH plus the new NPC names) remain soft and can change.
