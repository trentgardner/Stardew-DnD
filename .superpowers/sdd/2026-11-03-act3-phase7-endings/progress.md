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
