# Act II Phase 2: Into Act II Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The game can move from Act I into Act II, or start there. A season passes, the cap rises to 10, the dragon becomes a teenager who trusts some people and not others, and the systems Act II's content will plug into exist and are tested: the four forces' standing and forced choice, blight stages, and a lore journal.

**Architecture:**
- **Game state** (`DnD.Game`): act, standing, blight and lore live in `WorldState` and are saved in `SaveData`, which moves to version 3 with a migration.
- **Data:** the rules for factions, blight and lore are small data-driven classes (`FactionDb`, `BlightDb`, `LoreDb`), in the style of QuestDb and WorldDb.
- **Dragon:** the teenage dragon is a second `DragonStage` on the existing `Dragonling`, with stats by stage. Its trust lives in `DragonDb` (opinions per NPC) and is surfaced through `DragonPet` emotes and dialogue advantage.
- **Content:** no Act II places or quests yet. Phases 3–6 add them on top of these hooks.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`).

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md: the four forces, the teenage dragon, blight Goal 7, lore Goal 2, the transition decisions (builder check section). Roadmap: docs/superpowers/plans/2026-10-31-act2-phase1-levels.md.

## Global Constraints

- **THE SECRET:** nothing written in this phase may hint that the Ashen Queen is a child, or that the Crown is the culprit. Lore text tells only the legend: an ancient tyrant. Records are "missing or altered" with no culprit implied.
- Every force is right about something. No faction text paints one side as good or evil.
- Tone: bright fantasy, PG-13. The blight is unsettling, not gory.
- Dice are always visible. Data-driven content. Art stays swappable through ArtRegistry ids.
- Old saves load: v2 → v3 migration (Act 1, no standing, blight 0). An Act I save that finished the hatching can continue into Act II.
- Buoy stays in Thistledown, pettable, in every act.
- **The dragon:** useful, never fight-carrying (BG3's Us level). Equal control for both players. No flying until Act III.
- **Carried from the Phase 1 review:** loading an Act II save sets `PlayerCharacter.LevelCap` to 10, and companions with banked XP catch up when the cap rises.

## Review Focus

- **Loading an Act II save, then quitting to the title and starting an Act I game:** the cap must be 4 again. Test: `ActOneAfterActTwoIsCappedAtFour` (Task 1).
- **Committing to a faction in co-op while the partner is in another zone:** the partner is told, and their journal shows the closed quests. Test: `CommitInCoopTellsThePartner` (Task 5).
- **Starting at Act II, then dying and waking at the temple:** the dragon exists and follows, and Fallen Star shows as complete. Test: `StartAtActTwoHasAWorkingWorld` (Task 2).
- **A dragon opinion of an NPC who isn't in the zone, or an unknown NPC id:** no crash; it counts as neutral. Test: `UnknownNpcIsNeutral` (Task 4).
- **Blight stage set while the player is inside the zone:** the tint updates without having to leave and come back. Test: `BlightTintUpdatesInPlace` (Task 6).

---

### Task 1: Act state, the season time skip, and Act II on load

**Files:** `Assets/Scripts/Game/Core/WorldState.cs`, `Assets/Scripts/Game/Core/SaveSystem.cs`, `Assets/Scripts/Game/Core/GameRoot.cs` (new `ActTwo.cs` partial if GameRoot grows), `Assets/Scripts/Game/UI/ActEndScreen.cs`, `Assets/Tests/PlayMode/ActTwoTransitionTests.cs` (new)

**Interfaces (produces):**
- `WorldState.Act` (int, 1 or 2) and `WorldState.ActTwoDay` (int, the day Act II began; 0 if not yet).
- `SaveData.Version = 3`, plus `SaveData.Act` and `SaveData.ActTwoDay`. `Migrate`: anything under v3 gets Act 1.
- `GameRoot.BeginActTwo(PlayerSlot by)`:
  - `World.Act = 2`, `PlayerCharacter.LevelCap = ActTwoLevelCap`.
  - Clock +28 days (one season), at 8:00.
  - Party and companions healed (a long rest without the hunger check).
  - Companions `AutoLevel` (they catch up).
  - The dragon grows (`Dragonling.Stage = Wyrmling`, Task 3).
  - A narrator card (`MessageScreen` "A Season Passes"), then an autosave.
  - Unfinished Act I quests stay as they are.
- On load (`LoadGame`): `PlayerCharacter.LevelCap = World.Act >= 2 ? 10 : 4`, and companions auto-level.
- The act-end screen gets a second option, "Begin Act II (preview: a season passes)", which calls BeginActTwo.
  - **[Claude's call]** It's labelled a preview until Phase 3 builds the road to the Academy.

**Steps:**
- [ ] Play-mode tests:
  - `BeginActTwoSkipsASeasonAndRaisesTheCap`: day +28, `LevelCap == 10`, `World.Act == 2`.
  - `CompanionsCatchUpWhenTheCapRises`: a companion holding 14000 XP at level 4 becomes level 6.
  - `ActTwoSurvivesSaveAndLoad`: after loading, Act 2 and cap 10.
  - `ActOneAfterActTwoIsCappedAtFour`.
  - `OldSaveLoadsAsActOne`: strip the Act fields from the JSON → Act 1.
- [ ] Run → FAIL. Implement. Run → PASS. Commit `"Act II: act state, a season passes, cap 10 on load"`.

### Task 2: Start at Act II

**Files:** `Assets/Scripts/Game/UI/TitleScreen.cs`, `Assets/Scripts/Game/Core/GameRoot.cs`, `Assets/Scripts/Game/UI/CharacterCreationScreen.cs` (only for the start flag), tests in `ActTwoTransitionTests.cs`

**Interfaces (produces):**
- `GameRoot.StartNewGame(PlayerInput device, Difficulty diff, bool actTwo = false)`. The title screen asks "Start at: Act I / Act II (level 5, preview)" after the difficulty.
- `GameRoot.ApplyActTwoStart(PlayerSlot p)` runs after creation. It sets:
  - `Quests.Set("fallen_star", FallenStar.Done)` without paying rewards (use `rewardScale: 0`)
  - flags `snik_friend` and `elder_blessing`
  - `World.EggHatched = true`, then the dragon is spawned (it asks for a name with NameEntryScreen, default "Cinder")
  - `World.Act = 2` and `LevelCap = 10`
  - day 29
  - `XP = XpThresholds[5]`; the player levels 1→5 through the level-up screen (real choices). The HUD already announces "Level up available".
  - Starter kit: 150 gp, 2 potions of healing, 1 greater healing.
- A second player joining an Act II game also starts at XP for level 5 and gets the same kit.

**Steps:**
- [ ] Play-mode tests:
  - `StartAtActTwoHasAWorkingWorld`: the dragon exists, Fallen Star is done, `CanLevelUp`, a forced party wipe → wakes at the temple with the dragon nearby.
  - `SecondPlayerJoinsAtLevelFive`.
- [ ] Run → FAIL. Implement. Run → PASS. Commit `"Act II: start a new game at Act II (level 5 preview)"`.

### Task 3: The teenage dragon (Wyrmling stage)

**Files:** `Assets/Scripts/Rules/Dragonling.cs`, `Assets/Scripts/Rules/Content/MonstersDb.cs`, `Assets/Scripts/Game/Combat/CombatDragon.cs`, `Assets/Scripts/Game/World/DragonPet.cs` (scale), `Assets/Scripts/Game/Core/SaveSystem.cs` (`DragonStage` int), `tools/RulesTests/DragonTests.cs`

**Stats** (the Hatchling is unchanged). Wyrmling, for party level L:
- AC 15, HP 10 + 6×L.
- **Multiattack:** Bite (+PB+3, 1d10+3 piercing) and Claw (+PB+3, 1d6+3 slashing).
- **Ember Breath:** a 15-ft cone, ⌈L/2⌉d6 fire (3d6 at 5, 5d6 at 10), DEX save DC 8+PB+CON mod, recharge 5–6.
- **Shield Wing** (new, bonus action, once per round): an ally within 5 ft gets +2 AC until the dragon's next turn.
- **Team-up "Kindle"** (new, the dragon's action, either player can order it): one adjacent ally's weapon blazes; their next weapon hit this turn or next deals +1d6 fire per 4 party levels (min 1d6). Each player can order it for themself, which keeps equal control.
- **[Claude's call]** Balance: about 40–50% of a martial's at-will damage at the same level. A rules test checks it stays under 60%.
- **Look:** the same sprite at 1.4× scale, plus a slightly deeper red tint (`tint mon_dragonling_teen_* from mon_dragonling_* 350 1.1 0.9`).
  - **[Claude's call]** No new hand-drawn sheet yet: the spec's "new sprite" is met with a bigger, redder dragon.

**Steps:**
- [ ] Rules tests:
  - `WyrmlingStatsScaleWithParty` (L5: AC 15, HP 40, breath 3d6; L10: HP 70, 5d6)
  - `WyrmlingMultiattackBiteAndClaw`
  - `WyrmlingStaysUnderMartialDamage` (DPR < 0.6 × fighter at L5, 7 and 10)
  - `HatchlingUnchanged`
- [ ] Play-mode tests:
  - `ShieldWingRaisesAnAllysAc`
  - `KindleAddsFireToTheNextHit`
  - `TeenDragonIsBigger` (actor scale 1.4)
- [ ] Implement. Run → PASS. Commit `"Dragon: the teenage Wyrmling stage (multiattack, bigger breath, Shield Wing, Kindle)"`.

### Task 4: The dragon trusts some people and not others

**Files:** `Assets/Scripts/Game/World/DragonDb.cs` (new), `Assets/Scripts/Game/World/DragonPet.cs`, `Assets/Scripts/Game/Dialogue/Dialogue.cs` (advantage hook), `Assets/Scripts/Game/Core/WorldState.cs` + `SaveSystem.cs`, tests in `Assets/Tests/PlayMode/DragonTrustTests.cs` (new)

**Interfaces (produces):**
- `public enum DragonFeeling { Distrust = -2, Wary = -1, Neutral = 0, Fond = 1, Trust = 2 }`.
- `DragonDb.Opinion(string npcId, WorldState w) -> DragonFeeling`. Base opinions come from `DragonDb.Base` (data), shifted by `w.DragonOpinionShift[npcId]` (saved, ±).
  - Act I townsfolk: Elowen, Pip, Brannoc, Garrick, Lark and Buoy are Fond or Trust; Wren is Wary (tolls!); Snik and Ssark are Trust (kobold kin).
  - Act II NPCs get their entries in their own phases.
- `DragonDb.Shift(string npcId, int delta)`: events move opinions (Phase 4 faction quests use it).
- **Visible:**
  - When the dragon comes within 3 tiles of an NPC with a non-neutral opinion, it shows an emote once per NPC per day: heart (Trust), small heart (Fond), "?" (Wary), hiss/smoke puff (Distrust). It also gets a bark line in the HUD ticker, e.g. "Cinder hides behind you and hisses at Wren."
  - The journal's dragon section lists who it trusts and distrusts: "Cinder trusts: Elowen, Snik… · Cinder is wary of: Wren".
- **Meaningful:** in dialogue, when the dragon is in the zone and near, social checks (Persuasion, Insight, Deception) with an NPC it Trusts get Advantage ("Cinder nuzzles them; they soften"). With an NPC it Distrusts, Insight gets Advantage ("Cinder's hackles rise: something's off"). Implemented as one hook in `DialogueScreen` roll setup: `DragonDb.CheckAdvantage(npcId, skill)`.
  - It never blocks a quest or a fight (spec builder-check decision).
- **Personality barks:**
  - teenage moods every few in-game hours (sulks if not petted that day, shows off after a won fight)
  - `DragonDb.BlightBark()`, which Phase 5 calls in blighted zones: the dragon whimpers and presses close
  - Barks never mention the Queen's age or nature.

**Steps:**
- [ ] Tests:
  - `TrustedNpcGivesAdvantageOnPersuasion`
  - `DistrustedNpcGivesAdvantageOnInsightOnly`
  - `DragonEmotesNearWrenOncePerDay`
  - `OpinionShiftIsSaved`
  - `UnknownNpcIsNeutral`
  - `JournalListsTrust`
- [ ] Implement. Run → PASS. Commit `"Dragon: trust and distrust (emotes, journal, social-check advantage, barks)"`.

### Task 5: The four forces: standing, scouting and the forced choice

**Files:** `Assets/Scripts/Game/Quests/FactionDb.cs` (new), `Assets/Scripts/Game/Quests/QuestDb.cs` (`QuestDef.Faction`, `QuestDef.Major`), `Assets/Scripts/Game/Quests/QuestLog.cs`, `Assets/Scripts/Game/Core/WorldState.cs` + `SaveSystem.cs`, `Assets/Scripts/Game/UI/Screens.cs` (journal), tests in `Assets/Tests/PlayMode/FactionTests.cs` (new)

**Interfaces (produces):**
- `FactionDb.All`: `crown` ("The Crown"), `academy` ("The Starfall Academy"), `fae` ("The Fae Courts"), `hunters` ("The Wyrmwatch", **[Claude's call]** a working name for the dragon-hunting order). Each has a one-line belief, written grey. Examples:
  - crown: "A dragon belongs to the realm, and the realm needs protecting."
  - academy: "Understand it first; fear can wait for facts."
  - fae: "The old Pact was a promise. Promises can be renewed, on the right terms."
  - hunters: "Every village we've lost to an ember-eyed beast had someone who said 'it's only small'."
- `WorldState.Standing` (`Dictionary<string,int>`, −100..100) and `WorldState.CommittedFaction` (string or null). Both saved.
- `Factions.Adjust(string faction, int delta, PlayerSlot by)`: clamps and toasts "The Crown thinks better of you."
- `QuestDef.Faction` (string) and `QuestDef.Major` (bool).
  - `QuestLog.CanStart(string questId)`: false if the quest is Major for a faction other than the committed one, or is any quest of a faction locked out by the commitment.
  - `QuestLog.ClosedBy(questId)` returns the committed faction's name for the journal: "Closed: you stood with the Academy."
- `Factions.OfferCommit(PlayerSlot by, string faction, Action onYes)` shows a `ChoiceScreen`: "This is the moment you choose a side. Standing with the Crown closes the Academy's, the Fae Courts' and the Wyrmwatch's paths for good. Are you sure?" Yes / Not yet.
  - On yes: `CommittedFaction = faction`, then every other player gets a narration ("Ash has chosen to stand with the Crown."), then onYes.
- Test-only quests `test_faction_scout` and `test_faction_major` are registered only under a `QuestDb.RegisterTestQuests()` call from tests. **[Claude's call]** No placeholder quests ship.

**Steps:**
- [ ] Tests:
  - `ScoutingQuestsAreOpenToEveryone`
  - `CommittingClosesOtherFactionsQuests`
  - `CommitAsksFirstAndNotYetChangesNothing`
  - `CommitInCoopTellsThePartner`
  - `StandingIsSavedAndClamped`
  - `JournalShowsClosedPaths`
- [ ] Implement. Run → PASS. Commit `"Act II: the four forces (standing, scouting, the forced choice)"`.

### Task 6: Blight stages

**Files:** `Assets/Scripts/Game/World/Blight.cs` (new), `Assets/Scripts/Game/World/Zone.cs` (tint hook), `Assets/Scripts/Game/World/WorldDb.cs` (`ZoneDef.BlightWeight` 0–1), `Assets/Scripts/Game/Core/WorldState.cs` + `SaveSystem.cs`, `Assets/Scripts/Game/Dialogue/Dialogue.cs` (a `Blight(n)` condition helper), tests in `Assets/Tests/PlayMode/BlightTests.cs` (new)

**Interfaces (produces):**
- `WorldState.BlightStage` (0–3) and `WorldState.BlightEased` (bool). Both saved.
- `Blight.SetStage(int stage)`: clamps, saves, and re-applies to every loaded zone at once.
- `Blight.Ease()`: one stage down, sets BlightEased (the volcano's half solution, Phase 6).
- **Look**, per zone, scaled by `stage × BlightWeight`:
  - desaturation toward ash grey (a full-zone overlay sprite with alpha 0.08 × stage × weight, colour #9a958f)
  - drifting ash particles (stage ≥ 2)
  - hearth/forge light sources dimmed by 50% at stage ≥ 1 ("cold forges")
- BlightWeight defaults:
  - outdoor Act I zones 0.6
  - Thistledown 0.5
  - interiors 0.2
  - the Kobold Warrens 0.3 (the kobolds' egg-song keeps it back, **[Claude's call]**)
- `DCtx.Blight >= n` for dialogue variants. Phase 5 writes the frightened-townsfolk lines. This phase adds two sample variants so the hook is tested: Garrick's forge ("won't stay lit") and Pip's gossip.

**Steps:**
- [ ] Tests:
  - `BlightTintScalesWithStageAndWeight`
  - `BlightTintUpdatesInPlace`
  - `EaseLowersTheStageAndIsSaved`
  - `DialogueVariantShowsAtStageOne`
  - `StageZeroLooksLikeActOne` (no overlay)
- [ ] Implement. Run → PASS. Commit `"Act II: blight stages (ash tint, cold forges, dialogue hook)"`.

### Task 7: The lore journal and the legend

**Files:** `Assets/Scripts/Game/Quests/LoreDb.cs` (new), `Assets/Scripts/Game/World/LoreSpot.cs` (new, an interactable like SecretInteractable), `Assets/Scripts/Game/World/WorldDb.cs` (`ZoneDef.Lore` list of `LoreSpotDef { Id, Pos }`), `Assets/Scripts/Game/UI/Screens.cs` (journal gets a Lore page: Quests | Lore toggle with Q/R or LB/RB), `Assets/Scripts/Game/Core/WorldState.cs` + `SaveSystem.cs`, tests in `Assets/Tests/PlayMode/LoreTests.cs` (new)

**Interfaces (produces):**
- `LoreDef { Id, Title, Category ("The Legend" | "Records" | "The Pact" | "Places"), Text }` and `WorldState.LoreFound` (HashSet, saved).
- `LoreDb.Find(string id, PlayerSlot by)` records it, toasts "New lore: …", and gives 25 XP once (discovery XP, same as places).
- **Act II's legend content, starting set:**
  - "The Ashen Queen" (the temple mural, Thistledown): the tyrant legend as the Dawnmother's priests tell it.
  - "The Sleeping Mountains" (the watchtower's back wall): the dragons lay down to hold her.
  - "A Children's Rhyme" (Lark's cottage): a skipping rhyme about the Queen's fire.
    - The rhyme is about *running from* her: "Ash on the hearth, ash on the stair, the Queen of Cinders is everywhere". It must read as a tyrant legend.
  - "Missing Pages" (Old Hobb's mill ledger shelf): a parish record with the pages for that century cut out, no culprit named.
- All text is reviewed against the SECRET constraint.
- **Secret-leak guard (test):** `NoActTwoTextHintsAtTheSecret` scans every LoreDb, DialogueDb, QuestDb, ItemsDb and FactionDb string. Any string mentioning "Queen" or "Ashen" must not contain any of:
  - child, young, little, small, baby, hatchling, daughter, son, girl, crying, cries, afraid, frightened, scared, innocent, mother, orphan, kid
  - crown did, the king did, royal order, by order of the crown
  - The check is case-insensitive. Phase 7 keeps it.

**Steps:**
- [ ] Tests:
  - `FindingLoreRecordsItOnceWithXp`
  - `JournalLorePageListsFoundEntries`
  - `LoreSpotIsInteractableAndSaved`
  - `NoActTwoTextHintsAtTheSecret`
- [ ] Implement (the four spots placed in existing maps on walkable tiles next to the named landmark). Run → PASS. Commit `"Act II: the lore journal and the legend (with a secret-leak guard)"`.

### Task 8: Acceptance, docs, build

- [ ] Play-mode `IntoActTwoGoldenPath`:
  - finish Act I (debug-set fallen_star Done)
  - act-end screen → Begin Act II
  - check: teen dragon, cap 10, day +28
  - level a fighter to 5 through the screen
  - find a lore spot
  - blight stage 1 (tint visible)
  - adjust Crown standing
  - save → load → everything above holds.
- [ ] Screenshots to `%TEMP%/embers_screenshots/act2_*.png`: the teen dragon next to the player, the journal Lore page, Thistledown at blight 2. Review them visually.
- [ ] Docs:
  - PLAYTEST.md: "New: Into Act II (preview)", covering how to get there and what to look for.
  - DESIGN.md: the four forces, blight, lore, the dragon's trust.
  - LORE_PITCH.md: the Wyrmwatch name, the legend texts, pending lore approval.
- [ ] Full rules suite and play-mode suite green; rebuild the exe and zip; end-of-branch review (fresh reviewer); fix pass; merge `act2/phase2-into-act2` into main; push.
