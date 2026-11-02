# Act III Phase 2: Into Act III Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A finished Act II can move into Act III (cap 13, two weeks later, a grown dragon), a new game can start at Act III with a chosen Act II side, and the lore journal can "re-read" old entries once the reveal happens.

**Architecture:** This mirrors Act II Phase 2:
- `GameRoot.ActTwo.cs` holds `BeginActTwo`, `ApplyActTwoStart`, `GiveActTwoKit` and `DragonGrew`. A new `GameRoot.ActThree.cs` holds the Act III equivalents.
- The dragon's numbers live in the engine-free `Dragonling`. A new stage check `IsGrown` (Stage ≥ Young) sits beside `IsTeen`.
- `LoreDef` gains reveal fields. The journal (`JournalScreen.LorePage`) shows them after a flag.
- The secret guard (`LoreTests.NoActTwoTextHintsAtTheSecret`) learns one exemption convention, so Phase 4's reveal text can exist without tripping it.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests, Unity play-mode tests (`.superpowers/unity-test.sh [filter]`).

**Spec:** docs/superpowers/specs/2026-11-02-act3-design.md (The dragon, grown; Goal 12 carry-over and "Start at Act III"; the reveal's "Read again" call; anti-goal: nothing before the door hints she is a child).

## Global Constraints

- **THE SECRET** still holds for everything a player can read before the meeting: no text in this phase hints that the Queen is a child, or names the Crown as the culprit. The Act III summons may make the Crown look grand, not guilty.
- **Dragon power (user):** "just slightly below the party's power level", for example 2–3d10 where a fighter rolls 4d10. Rule: the grown dragon's expected at-will damage per round vs AC 17 is between 50% and 75% of the average martial's, at levels 11, 12 and 13.
- Flying stays a single finale moment (Phase 6). `CanFly` may be true, but no overworld flight.
- Old saves load: `World.DragonStage` is already an int, so `Young = 2` needs no new field.
- Dice always visible; every window closes with Esc/B and has a ✕.
- Buoy stays in Thistledown.

## Review Focus

- **An Act II save that never saw the End of Act II card.** "Begin Act III" is only offered from the card, after the Warden. Loading an Act III save sets cap 13. Test: `ActThreeSaveLoadsWithCapThirteenAndGrownDragon` (Task 2).
- **A partner joining an Act III game.** They get the level-11 kit, not the level-5 one. Test: `PartnerJoiningActThreeGetsTheActThreeKit` (Task 3).
- **The grown dragon in a fight under manual command.** The actions list must not throw, and the breath cone grows to 30 ft. Test: `GrownDragonFightsAndBreathesThirtyFeet` (Task 2).
- **The "Read again" text before the reveal.** It must never show (not even the marker) until `met_her` is set. Test: `RevealTextHiddenUntilTheDoor` (Task 4).
- **The guard's exemption must be narrow.** Only `*.AfterTheDoor.cs` files and `LoreDef.RevealText` are exempt; a leak anywhere else still fails. Test: `SecretGuardExemptionIsNarrow` (Task 4).

---

### Task 1: The grown dragon (rules)

**Files:** `Assets/Scripts/Rules/Dragonling.cs`, `tools/RulesTests/GrownDragonTests.cs` (new), `tools/RulesTests/Program.cs`

**Interfaces (produces):**
- `public bool Dragonling.IsGrown => Stage >= DragonStage.Young;`
- **Grown numbers** (the teen numbers are unchanged):
  - AC 17
  - `MaxHPFor(level, Young) = 20 + 8 × level`
  - to hit = PB + 4
  - Bite 1d10+4 piercing and Claw 1d6+4 slashing (`AttackRoutine` = both)
  - `BreathConeTiles = 6` (30 ft)
  - breath damage stays ceil(level/2)d6
  - `KindleDice` unchanged
- `public static double Dragonling.ExpectedDamagePerRound(Dragonling d, int ac)`: sum over `AttackRoutine` of hit chance × average damage, plus 5% crits. Same formula as `BalanceTests.ExpectedDamage`, so the test can compare like with like.

- [ ] **Step 1: Write the failing rules tests** in `GrownDragonTests.cs`:
  - `GrownDragonNumbers`: at party level 12, AC 17, MaxHP 116, two attacks at +8, cone 6.
  - `GrownDragonStaysBelowTheParty`: for L 11–13, ratio = dragon DPR / mean(`DamagePerRound` of fighter, barbarian, paladin, ranger, rogue, monk) vs AC 17 is in [0.5, 0.75].
  - `TeenUnchanged`: a Wyrmling at 10 still has AC 15 and cone 3.
- [ ] **Step 2: Run** `dotnet run --project tools/RulesTests`. Expected: FAIL (`IsGrown` is undefined).
- [ ] **Step 3: Implement** the stage branches in `Dragonling`.
- [ ] **Step 4: Run.** Expected: PASS. Print the ratio per level to the console.
- [ ] **Step 5: Commit** `"Rules: the grown dragon (a notch below the party)"`.

### Task 2: Begin Act III from the End of Act II card

**Files:** `Assets/Scripts/Game/Core/GameRoot.ActThree.cs` (new), `Assets/Scripts/Game/Core/GameRoot.ActTwo.cs` (`DragonGrew` handles Young), `Assets/Scripts/Game/UI/ActEndScreen.cs`, `Assets/Editor/ArtMap.txt` (`mon_dragon_grown` from `Actor/Monster/Dragon`, tinted red like the dragonling), `Assets/Tests/PlayMode/ActThreeIntoTests.cs` (new)

**Interfaces (produces):**
- `public void GameRoot.BeginActThree(PlayerSlot by)`:
  - Only if `World.Act == 2 && World.Flag(WardenFallen)`.
  - Sets Act = 3, `Clock.Day += 14`, 8 am, `World.ActThreeDay` (new int, saved), `DragonStage = Young`, the dragon at full HP.
  - Long rests everyone, then calls `ApplyActCap`, `DragonGrew` and `SaveSystem.Save(true)`.
  - Shows a `MessageScreen` "Two Weeks Later" with summons text, then narrates "Act III begins. (You can now reach level 13.)".
  - Summons text **[Claude's call]**: winter comes early; {dragon} is grown, big enough to carry a rider though nobody has dared; a letter under a royal seal invites the heroes of the Emberhorn to the capital. Nothing about the Queen.
- The End of Act II card gets a second option, "Begin Act III (preview: the capital is still being built)", shown when `World.Act == 2`.
- `public const float GrownDragonScale = 1.8f;` `DragonGrew` sets the scale and the look `mon_dragon_grown` when `IsGrown` (fallback: the teen look, scaled).
- `ApplyActCap`: when `World.Act >= 3`, also set the flag `act3` (`WorldDb.ActThreeFlag`).

- [ ] **Step 1: Write the failing play-mode tests:**
  - `BeginActThreeFromTheCard`: finish Act II (the `WardenDown` pattern from `ActTwoPassTests`). The card offers "Begin Act III". Choosing it gives Act 3, cap 13, a grown dragon and day +14.
  - `ActThreeSaveLoadsWithCapThirteenAndGrownDragon`: save, `LevelCap = 4`, load. Cap is 13 and `Dragon.IsGrown`.
  - `GrownDragonFightsAndBreathesThirtyFeet`: `DragonActions` lists "Attack (Bite + Claw)", and the breath's `RangeFt` is 30.
  - `NoBeginActThreeBeforeTheWarden`: an Act II game where the Warden stands, so `BeginActThree` does nothing.
- [ ] **Step 2: Run** `.superpowers/unity-test.sh ActThreeIntoTests`. Expected: FAIL.
- [ ] **Step 3: Implement.** Add the ArtMap lines, then run `.superpowers/import-art.sh`.
- [ ] **Step 4: Run.** Expected: PASS.
- [ ] **Step 5: Commit** `"Act III: begin it after the Warden (two weeks later, a grown dragon, a royal summons)"`.

### Task 3: Start at Act III

**Files:** `Assets/Scripts/Game/UI/TitleScreen.cs`, `Assets/Scripts/Game/Core/GameRoot.cs` (`StartNewGame` gains `actThree` and `side`; the joiner kit), `Assets/Scripts/Game/Core/GameRoot.ActThree.cs`, `Assets/Tests/PlayMode/ActThreeIntoTests.cs`

**Interfaces (produces):**
- Title: "Act III (level 11, preview: after the Warden)" → a side picker: "the Crown", "the Starfall Academy", "the Fae Courts", "the Wyrmwatch" (`FactionDb.NameOf`), plus Back. It calls `StartNewGame(device, diff, actThree: true, side: id)`.
- `public void GameRoot.ApplyActThreeStart(PlayerSlot p, string side)`:
  - Everything `ApplyActTwoStart` sets.
  - Plus: every force heard, `Factions.Commit(side)`, flag `banner_<side>`, Answers at `Rest`, `WardenFallen`, `ActTwoEndSeen`, `OathSecond`, `EmberhornGate`.
  - Act = 3, `DragonStage = Young`, the cap, the kit, then the dragon's NameEntry.
- `void GiveActThreeKit(PlayerSlot p)`:
  - XP to level 10 is applied with `AutoPlan` (**[Claude's call]**: ten level-up screens in a row is a chore), then XP set to the 11 threshold, so the player picks level 11 themselves on the level-up screen.
  - +600 gp, 3 `potion_greater_healing`.
  - Joiners in an Act III game get this kit (the Act II branch becomes `World.Act >= 3 ? GiveActThreeKit : GiveActTwoKit`).

- [ ] **Step 1: Write the failing play-mode tests:**
  - `StartAtActThreeForEachSide`: for each of the 4 sides, Act 3, committed side, banner flag, `WardenFallen`, level 10 with level 11 pending, and a grown dragon.
  - `PartnerJoiningActThreeGetsTheActThreeKit`: a second `DebugStart` in an Act III game is level 10 with XP ≥ 85000.
- [ ] **Step 2: Run.** Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: PASS.
- [ ] **Step 5: Commit** `"Act III: start at Act III with a chosen side"`.

### Task 4: "Read again" lore and the guard's narrow exemption

**Files:** `Assets/Scripts/Game/Quests/LoreDb.cs`, `Assets/Scripts/Game/UI/Screens.cs` (`JournalScreen.LorePage`), `Assets/Scripts/Game/World/WorldDb.cs` (const `MetHer = "met_her"`), `Assets/Tests/PlayMode/LoreTests.cs`

**Interfaces (produces):**
- `LoreDef.RevealText` (string, null by default), used by Phase 4.
- `LoreDb.SetReveal(string id, string text)` is how Phase 4's `*.AfterTheDoor.cs` file fills them in.
- **Journal:**
  - When `World.Flag(WorldDb.MetHer)` is set and an entry has `RevealText`, the entry shows "★ Read again" after its title, and its text is followed by a new line with the RevealText in italics.
  - Entries with RevealText sort first in their group.
  - Before the flag, nothing about them differs.
- **Guard:**
  - Files whose name ends in `.AfterTheDoor.cs` are skipped by `NoActTwoTextHintsAtTheSecret`, as is `LoreDef.RevealText`.
  - A new test `NoAfterTheDoorTextLeaksOutside` asserts every `*.AfterTheDoor.cs` file's strings are only reachable after `met_her`. This is checked by convention: such files may only contain a static `Register()` that `LoreDb`/`DialogueDb` call when the flag is set.
  - **[Claude's call]**: the guard enforces the convention structurally. Each `*.AfterTheDoor.cs` must contain `MetHer`.

- [ ] **Step 1: Write the failing tests:**
  - `RevealTextHiddenUntilTheDoor`: plant a RevealText on a found entry. The journal text doesn't contain it or "Read again" before the flag, and does after.
  - `SecretGuardExemptionIsNarrow`: a banned line in a normal file is still caught by the matcher. Done as a pure function test: extract the file-filter into `static bool GuardScans(string fileName)`, and assert `GuardScans("Foo.cs")` and `!GuardScans("Capital.AfterTheDoor.cs")`.
- [ ] **Step 2: Run** `.superpowers/unity-test.sh LoreTests`. Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run.** Expected: PASS.
- [ ] **Step 5: Commit** `"Lore: 'Read again' after the door; the secret guard's narrow exemption"`.

### Task 5: Acceptance

- [ ] Docs:
  - PLAYTEST.md: "Into Act III (preview)" with a checklist
  - DESIGN.md: the grown dragon's numbers and the DPR ratio table
  - LORE_PITCH.md: the summons
- [ ] Run the full rules suite and the full play-mode suite in the background. Expected: all green.
- [ ] Rebuild the exe and zip.
- [ ] Self-review against this plan and the spec.
- [ ] Merge `act3/phase2-into-act3` into main and push.
