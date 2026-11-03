# SDD ledger — plan: docs/superpowers/plans/2026-11-03-act3-phase7-endings.md

Executor: executing-plans (inline), branch act3/phase7-endings. Spec: docs/superpowers/specs/2026-11-02-act3-design.md.

## Pre-flight
- T1→T2: EndingDb.Scenes/Slides, Ending.Seen/Complete consumed by PlayEnding. OK.
- T2→T4: PlayEnding/CreditsScreen/Dedication used by golden paths and screenshots. OK.
- The dedication (user, 2026-11-03): exact copy pinned in Global Constraints and Task 2.

## Task 1
- Task 1: Ruling: the warm dragon variant needs bond tier >= 3 of 5 (DragonBond/20), not "Hearts >= 6" — the dragon's bond tops out at 5 hearts — cost if wrong: a few more parties see the warm lines.
- Task 1: Ruling: Slides(ending, w, day) takes the day count (WorldState doesn't hold the clock) — cost if wrong: none.
- Task 1: Ruling: "The King" is its own scene (so an ending with no companions still has 6 scenes: Her, the dragon, Aurelhold, the King, the Road Home, Thistledown); the last slide is titled "Afterwards".
- Task 1: Ruling: RED was a compile failure (EndingDb absent); the implementation was written before running it. EndingTests;LoreTests → 22/22.
Task 1: complete (tests: EndingTests;LoreTests → 22/22)

## Task 2
- Task 2: the dedication is GameRoot.Dedication = "To my beautiful fiancée Lauren, this game is dedicated. I love you forever." (pinned by DedicationIsInTheCredits); CreditsScreen scrolls, then shows it alone, centred, gold italic, held until a press (>= 1.5 s). Screenshot act7_credits_dedication shown to the user.
- Task 2: bug found + fixed: EndingPlaying leaked across QuitToTitle/LoadGame (StopAllCoroutines killed PlayEnding) so a resumed ending refused to start — ResetWorld clears it (EndingResumesAfterLoad, EpilogueSurvivesALeadDrop RED→GREEN).
- Task 2: Ruling: EscAdvancesButNeverSkips folded into EndingPlaysScenesSlidesAndCredits (every slide is closed one by one and the seen list must equal Slides) — cost if wrong: none.
- Task 2: Ruling: ending music = "music_home" for the scenes and slides, "music_title" for the credits (via MusicOverride). Buoy is placed beside the carriage stop for the Thistledown scene rather than walking there.
- Task 2: tests: EndingTests;FinaleTests → 47/47.
Task 2: complete (tests: EndingTests;FinaleTests → 47/47)

## Task 3
- Task 3: ActThreeBalanceTable (RulesTests): 10 street compositions at level 13, medians 1.4-7.7 rounds (biggest: Save s2 with Academy + Wyrmwatch against, 7.7). No tuning needed. RulesTests 1420/1420.
- Task 3: Ruling: the seal fight is lit by a LightSource (radius 7, cool blue) on Ashby's actor at the door. SealFightIsLit was written before the fix but its RED wasn't run separately (the light didn't exist; the assertion can't pass without it) — cost if wrong: weaker proof.
- Task 3: Ashen Queen retinted ash-grey with an ember tinge (hue 282, sat 0.45, val 0.95) so she reads differently from the red grown dragon.
- Task 3: tests: EndingTests → 16/16.
Task 3: complete (tests: RulesTests 1420/1420; EndingTests 16/16)

## Task 4
- Task 4: whole-act golden paths End/Seal/Save green: meeting → surface → questioning (confess + 4 leaders) → OfferEnding + Yes → 3 streets → flight → boss with phases → 7 scenes → 11 slides → credits (dedication) → title, game_complete saved; warm_city 7,8,9,10,11,12,13 in order.
- Task 4: Ruling: the golden paths start at the meeting (MetHer, warm_city 7), not at Act III's start — the capital and lair have their own golden paths (CapitalTests, LairTests) — cost if wrong: the seam between Phase 4 and 5 is covered by TurnTests, not here.
- Task 4: screenshots reviewed (act7_*): Her, Thistledown, slides (opaque now), credits and dedication. Fix: Buoy placed at TownCarriageStop+(1,4) so the beat window doesn't hide him.
- Task 4: minor (deferred): a stale "Round 1" tracker bar and the "Victory!" narration can still be on screen behind the first ending scene (existing combat UI).
- Task 4: docs: DESIGN "The endings (Act III Phase 7)", PLAYTEST (the game is complete + checklist), README (complete).

## Final review
- Final review: self-review (no subagent dispatched; the session only spawns agents when the user asks) — weaker than a fresh reviewer.
- Checked: resume after finale_won (replays from "Her"), completed save (one note, free roam), ResetWorld clears EndingPlaying/flight/music, Esc on slides and credits advances (never skips), screens ignore their opening frame's press, co-op lead hand-off in PlayBeats/PlaySlides/RollCredits, the dedication is excluded from the scroll and shown alone. No Critical/Important findings.
- Final: minor (deferred): a player left on the dedication screen stays there until a press (intended: it waits for you).
- Final: minor (deferred): ending_seen is recorded but nothing reads it yet (resume always replays the whole ending until game_complete).
- Final: full suite 507/507, RulesTests 1418/1418, Build OK (exe + zip).
