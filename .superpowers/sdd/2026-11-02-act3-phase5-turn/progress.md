# Act III Phase 5: the turn: progress ledger

Executed natively (inline) on branch act3/phase5-turn. Tasks 1-4 landed in 8d66da2; Task 5 (golden paths, screenshots, docs) in 3e6ba0f.

## Rulings / deviations from the plan
- The plan named a `confess` node; the confession is split into `confess` + `confess2` so it fits the dialogue box (checked in the screenshots).
- Speaker spots were moved onto the Sunstair paving after the screenshot review: king (31,11), marsh (29,11), ashby (28,13), sorrel (34,12),
  calloway (35,14). The dragon stands at (29,15) with `StaysPut` (until the party leaves the zone), because it was covering the player.
- The crown betrayal line is spoken by the King ("You stood with us... Guards."), not a prefix on the generic line.
- `ChoiceScreen.OnCancel` is new (Esc/✕ without picking). It is used by the ending confirm.
- Added test `SoloChoosesWithOneYesAndEscCancels` (not in the plan).
- TODO next session: warm_city needs a placeholder stage 10 so the break isn't announced as "Quest complete" (Last = StageText.Length - 1). Phase 6 replaces its text.

## Deferred minors
- Loading a save made during the break's beats doesn't replay them or the To Be Continued card (the choice itself is safe). Phase 6 replaces the card anyway.
- If P2 drops in while the ending confirm is open, it waits for a Yes from someone with no prompt. Talking to any speaker re-offers the choice, so nobody is stuck.
- The dialogue box's empty hearts row (♡♡♡) overlaps the ✕ for the King, as it does for every NPC. Existing UI, not Phase 5.
- `lantern_eyes` (an Act II hook) isn't used in the reaction table. Phase 7's epilogue can use it.
- Tests were written right after the code, not before it (they all ran and pass).

## Final verification (2026-11-03)
- Added warm_city stage 10 (Finale placeholder) so the break reads "Quest updated"; TurnTests asserts the quest is still active after the break.
- Full suite: 453/455. Fixed EveryNpcHasAWeeklyVariation (the five *_q speakers get a Hearthday entry at the same spot).
- AcademyGoldenPath failed once in the full run (library→restricted warp left the player in the library) but passes in isolation
  (Academy+Quest+Turn rerun 77/77). Phase 5 code doesn't run there (CheckTheTurn needs MetHer). Logged as a flaky test to watch.
