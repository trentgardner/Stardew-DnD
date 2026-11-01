# Act II Phase 7: The Act II Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Act II is finished and honest:
- XP pacing lands level 9 on the main path and level 10 for completionists around the Warden;
- the Warden's fall ends the act with an End of Act II card;
- the secret-leak guard covers every Act II voice;
- every Act II fight is checked against the level the party should be.

**Architecture:** Mirrors Act I's Phase 7.
- `ActTwoXp` counts the XP available from data (quests, places, secrets, groups, lore), and play-mode tests hold the band.
- Tuning happens through "Answers" milestone XP only (one knob, like Act I's Fallen Star milestones).
- `ActEndScreen` gets an Act II variant.
- RulesTests gets a per-group balance table.

**Tech Stack:** as before.

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md (Goals 10–11, "XP pacing to land level 10 around the finale", THE SECRET, the half solution). Roadmap item 7.

## Global Constraints

- XP rules unchanged (not milestone leveling). Tuning only moves the "Answers" StageXp values (the act's spine).
- Act II starts at level 5 (6500 XP). **Main path** = Answers + the chosen side's scouting and major quest + the groups on the main route (the Lantern Road, the Throat). It should reach **level 9 (48000) by the gate** and **level 10 (64000) once the Warden falls**. **Everything** (all side content, one side's major) should reach level 10 by the gate, but not exceed 64000 × 1.3 by then. Solo and two-player both hold (encounter XP splits).
- THE SECRET guard must scan every line naming the Queen, the Ashsworn, the Warden, the waking or the door.
- The End of Act II card says what changed (the mountain sleeps, the ash thinned, who you stood with) and promises Act III. It never mentions the Queen's nature.
- Branch `act2/phase7-act2-pass`. Merge and push after the full suite passes.

## Review Focus

- **Seeing the card twice:** the End of Act II card shows once per playthrough (flag `act2_end_seen`), on any player's screen, in co-op on both. Closing it never reopens it after a load. Test: `EndOfActTwoCardOnce`.
- **Old saves past the Warden:** a save with `warden_fallen` but no `act2_end_seen` shows the card once on load. Test: `EndCardForOldSaves`.
- **The level cap at the finale:** a player at 64000+ XP can take level 10. Nothing tries level 11. Test: `LevelTenIsTheCap` (cap stays 10).
- **XP band in co-op:** the per-player numbers hold for two players. Test: `ActTwoXpBandHoldsForTwoPlayers`.
- **The guard's new reach:** the extended guard still passes, and a planted bad line in a test string fails it. Test: `SecretGuardCatchesALeak`.

---

### Task 1: The Act II XP audit and tuning

**Files:** `Assets/Scripts/Game/Quests/ActTwoXp.cs` (new), `Quests/QuestDb.cs` (Answers StageXp only), tests in `Assets/Tests/PlayMode/ActTwoPassTests.cs` (new).

**Interfaces (produces):** `ActTwoXp.Available(bool includeSide, int players, bool includeWarden) -> int`, counting:
- every `Act >= 2` zone: `PerPlace`;
- groups: each once, `EncounterShare`; for the main path, only the Lantern Road, the Throat and the chosen major's own groups; the Warden only when `includeWarden`;
- non-quest secrets: DC × 2 (side only);
- lore: `LoreDb.FindXp` per Act II entry (side only);
- `Act >= 2` quests: StageXp + reward. Majors: count only the largest one (you get one). Faction scouting quests: main = the scouting quests of one force (take the max force); side = all of them.

- [ ] **Step 1: Write failing tests:**
  - `ActTwoXpBandHoldsForSolo` / `ActTwoXpBandHoldsForTwoPlayers`:
    - the main path to the gate ≥ 48000 − 6500;
    - main + the Warden ≥ 64000 − 6500;
    - everything to the gate ≥ 64000 − 6500 and ≤ 64000 × 1.3 − 6500;
    - log the numbers.
  - `LevelTenIsTheCap`: in Act II, XP 70000 → level 10 is offered, never 11.
- [ ] **Step 2:** Run → FAIL (the band, at today's numbers).
- [ ] **Step 3:** Implement `ActTwoXp`. Then tune only the Answers StageXp (stages 2, 4, 5, 6, 8, 9) until the band holds solo and co-op. Ledger the before and after numbers.
- [ ] **Step 4:** PASS + `ActOneTests` (unchanged).
- [ ] **Step 5:** Commit `"Act II: XP audit and pacing (level 9 by the gate, 10 at the Warden)"`.

### Task 2: The End of Act II card

**Files:** `UI/ActEndScreen.cs` (an Act II variant), `Core/GameRoot.cs` (show after `WardenFalls`, and on load if fallen but unseen), tests.

- The card, in the existing ActEndScreen's style:
  - Title: "End of Act II: Answers".
  - Body: the mountain sleeps; the ash thins (Thistledown's hearths catch again); a line for the side you stood with ("The Crown's banner flies over Thistledown's bridge." / "The Academy lights the Lantern Road again." / "The Courts' spring runs warm, for now." / "The Wyrmwatch keeps its watch."); `{dragon}` grew braver; "But something beneath the Wyrmback is still waking. Act III is coming."
  - A single button, "Continue your journey": you keep playing in Act II's world.
- Flag `act2_end_seen`. Both players see it in co-op.

- [ ] **Step 1: Write failing tests:** `EndOfActTwoCardOnce` (it shows after the victory hook; the flag is set; it never shows again), `EndCardForOldSaves`, and `EndCardNamesYourSide` (each side's line appears for its commit).
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS + `VolcanoTests`. **Step 5:** Commit `"Act II: the End of Act II card"`.

### Task 3: The secret guard's reach, and the Act II balance table

**Files:** `Assets/Tests/PlayMode/LoreTests.cs` (the guard regex and a planted-leak self-test), `tools/RulesTests/BalanceTests.cs` (the Act II group table).

- **The guard:** lines matching `queen|ashen|ashsworn|warden|waking|the door` are scanned for the banned words. Add `SecretGuardCatchesALeak`: run the same `Leaks` logic over a planted line ("the warden was only a child") → caught. Then fix any real hits (reword, as with the Phase 6 'Kin' line).
- **The balance table** (RulesTests): for each Act II group, the expected party level by place:

  | Level | Places |
  |---|---|
  | 5 | road |
  | 6 | academy interiors |
  | 7 | the forces' places |
  | 8 | the Emberhorn Road |
  | 9 | the Throat |

  Compute rounds for a typical 2-player + 2-companion party (the median 4-class set from the Warden check). Assert no group takes over 8 rounds and none is a pushover under 1 round, unless marked trivial. Print the table.

- [ ] Steps: tests → FAIL where needed → fix → PASS → commit `"Act II: the secret guard covers every Act II voice; the Act II balance table"`.

### Task 4: Release

- [ ] Play-mode `ActTwoGoldenPath`: Start at Act II → the road → the Academy → Ashby → hear all four → one scouting quest → commit → the major → the blight rises → the Emberhorn Road → the gate → the Warden (WinFight) → the End card → save/load. Assert the level ≥ 9 at the gate when the XP from the audited main path is applied (via hooks, the XP comes from the real rewards).
- [ ] Screenshots: the End of Act II card.
- [ ] Docs:
  - PLAYTEST.md: "Act II is complete", with a full-act checklist.
  - DESIGN.md: the XP table and the balance table.
  - README: the current state.
- [ ] RulesTests + the full suite green; build; self-review; merge; push.
