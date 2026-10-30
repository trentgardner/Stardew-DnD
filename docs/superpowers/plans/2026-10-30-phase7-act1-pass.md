# Phase 7: The Act I Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Act I plays start to finish as one piece: the XP lands level 4 around the hatching, the act has a proper ending card, key moments have narrator lines, a golden-path test plays the main story from character creation to Captain Vale's thanks, every class is in a sane damage band, and a playtest build ships.

**Architecture:** Mostly tests that measure what's already there (an XP audit over WorldDb/QuestDb/MonstersDb, a golden-path play test, a rules-side balance table) plus small content: an `ActEndScreen` after the Fallen Star turn-in and a handful of narrator lines on existing events. Retuning happens only where a measurement fails.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (`dotnet run`), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`), build via `-executeMethod BuildTools.BuildWindows`.

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md (Goals 7, "Build order" item 7, "Needs the user").

## Global Constraints

- XP stays as it is (combat, peaceful resolution, quests, first-time checks, discovery); "the XP pacing is retuned so level 4 lands around the hatching". Level cap 4.
- Peaceful choices pay the same XP as fighting. Quest XP is a lump per player.
- No big stakes in Act I: the ending card foreshadows Act II (the capital, the Academy, the Court) without starting it.
- Dice always visible; every window closes with Esc/B and a ✕; tests run muted.
- Old saves load (a save past `fallen_star` Done must not show the ending card again).
- Buoy stays in the game (see docs/DESIGN.md "Buoy").

## Review Focus

- **Ending card replay:** loading a save made after the turn-in never shows the card again; co-op shows it to both players once. Test: `ActEndCardShowsOncePerPlaythrough` (Task 2).
- **XP double-dip through co-op:** quest lumps go to each player and combat XP is split, so a two-player run must still sit inside the band. Test: `ActOneXpBandHoldsForTwoPlayers` (Task 1).
- **Level cap overflow:** XP past level 4 is kept but never offers a level 5. Test: `NoLevelFiveInActOne` (Task 1).
- **Golden path off a reload:** a save made at the warrens stage, reloaded, finishes the act. Covered inside `GoldenPathActOne` (Task 3).
- **A class left behind:** the weakest class at each level deals at least half the strongest's expected damage per round. Test: `ClassDamageBand` (Task 4).

---

### Task 1: XP audit and retune

**Files:** Create `Assets/Tests/PlayMode/ActOneTests.cs`; modify (only if a test fails) `Assets/Scripts/Game/Quests/QuestDb.cs` rewards.
**Interfaces:** Produces `static int ActOneXp.Available(bool includeSide, int players)` in `Assets/Scripts/Game/Quests/ActOneXp.cs`: the XP one player can earn before the hatching, summing quest rewards (all of `QuestDb.All` except `fallen_star`, which pays after), +100 for the hatching, 15 per zone discovered (`WorldDb.Zones`, excluding interiors), secrets (2 × DC each, `ZoneDef.Secrets` that aren't quest spots), and enemy-group XP (`MonstersDb` XP of each `EnemyGroupDef`'s members, split by `GameRoot.EncounterShare(xp, players)`). Main path only (`includeSide = false`): fallen_star's groups/zones (crater, warrens, egg shrine, mine) plus the hatching.

- [ ] **Step 1: Tests**: `ActOneXpBandHoldsForSolo` (main path ≥ 900, i.e. level 3 at the egg; all content ≥ 2700 and ≤ 4500); `ActOneXpBandHoldsForTwoPlayers` (same bands with players = 2); `NoLevelFiveInActOne` (give 9000 XP → level 4, `CanLevelUp` false).
- [ ] **Step 2:** Run → fail (no `ActOneXp`).
- [ ] **Step 3:** Implement `ActOneXp`; if a band fails, retune quest rewards (never combat) and ledger the change.
- [ ] **Step 4:** Run → pass. Commit: `Act I: XP audit (level 4 around the hatching)`.

### Task 2: The Act I ending and narrator lines

**Files:** Create `Assets/Scripts/Game/UI/ActEndScreen.cs`; modify `GameRoot.CompleteFallenStar` (push the card to every ready player once: flag `act1_ended`), `GameRoot.GiveXP` (first "level up available" narration), `GameClock.HourChanged` handler (first nightfall line at 20:00, once), `docs/PLAYTEST.md`.
**Interfaces:** `ActEndScreen(PlayerSlot p)`: title "End of Act I: Home", a recap (days taken, quests done of total, dragon name + bond hearts, a line if Buoy was petted), an Act II teaser (the capital has heard; an Academy carriage on the east road; "the Court is waking"), and "Keep exploring Thistledown" to close (free play continues).

- [ ] **Step 1: Tests**: `ActEndCardShowsOncePerPlaythrough` (CompleteFallenStar → each player's top screen is ActEndScreen; close; save/load; Vale again → no card); `FirstNightfallIsNarratedOnce`.
- [ ] **Step 2:** Run → fail. **Step 3:** Implement. **Step 4:** Run → pass. Commit: `Act I: ending card and narrator moments`.

### Task 3: Golden path

**Files:** `Assets/Tests/PlayMode/ActOneTests.cs`.
- [ ] **Step 1:** `GoldenPathActOne`: fresh bard; take the main quest from Vale (dialogue API), visit the crater (stage Trail), the warrens (Warrens), pacify the gate and the egg guards the peaceful way (`PacifyGroup`), save + reload at the warrens stage, hatch (skip scene, name "Goldie"), return to Vale → stage Done, ActEndScreen shown, level ≥ 3, no logged errors.
- [ ] **Step 2:** Run → fix anything it finds (each fix TDD'd and ledgered). Commit: `Act I: golden-path test`.

### Task 4: Balance table

**Files:** Create `tools/RulesTests/BalanceTests.cs`.
- [ ] **Step 1:** `ClassDamageBand`: for each of the 12 classes at levels 1–4 (`CharacterFactory.DefaultChoices` + `AutoPlan` level-ups), expected damage per round vs AC 13 from the best of its attacks or attack cantrips (hit chance × average damage, crit included); assert min ≥ 0.5 × max at every level. `KragIsBeatableAtLevel3`: two level-3 characters' expected damage kills Krag in ≤ 6 rounds.
- [ ] **Step 2:** Run; tune only if a band fails (ledgered). Commit: `Balance: class damage band and boss check`.

### Task 5: Acceptance and ship

- [ ] Full play-mode suite + RulesTests green; screenshots of the ending card; docs (PLAYTEST "New in Phase 7", the co-op checklist kept); self-review; rebuild exe + zip; merge to main; push to GitHub (user OK 2026-10-30).
