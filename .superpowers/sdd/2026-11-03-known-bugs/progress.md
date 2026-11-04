# Known-bug sweep (2026-11-03), branch fix/known-bugs

Sources: every .superpowers/sdd/*/progress.md deferred list, docs/superpowers/plans/2026-10-31-act2-phase1-rulings.md, memory.
Tests: tools/RulesTests/KnownBugTests.cs (rules) and Assets/Tests/PlayMode/KnownBugTests.cs (play mode), RED seen before fixes (17/18 red).

Fixed (test):
1. An incapacitated boss loses the breath it was drawing (IncapacitatedBossLosesItsBreath).
2. A late joiner to the Warden fight gets Ashby's ward (LateJoinerGetsTheWard; GameRoot.ApplyWardenWard).
3. Retreating wastes a drawn breath (RetreatLetsTheBreathGo).
5. Savage Attacker rerolls only the weapon dice (AttackResolver.SavageReroll; rules test): Prey/Sneak/Kindle no longer spent twice.
6. Action Surge between swings keeps the leftover attacks (TurnState.StartAttackAction; rules test). Label shows "(n left)" past the max.
7. A brawl keeps Extra Attack's second punch (BrawlKeepsTheSecondPunch).
8. Upcasting: Scorching Ray +1 ray/level, Spiritual Weapon +1d8/level, Aid +5 HP/level (rules tests).
9. Wall of Fire previews as the wall (WallOfFirePreviewsAsAWall; CombatAction.WallTiles).
10. The Quickened copy keeps the line/wall shape (QuickenedLightningBoltIsStillALine).
11. The sheet counts every ASI, the level-4 one included (SheetCountsEveryAbilityScoreImprovement).
12. A closed window leaves the screen at once, even under the dice tray (AClosedWindowDisappearsAtOnce) — also fixes the stale tracker/banners behind beats.
13. The narrator box waits while a window is open (NarratorStaysOutOfTheWayOfWindows).
14. The dialogue hearts row stops short of the close button (HeartsLeaveRoomForTheCloseButton).
15. The searchable ever-lamp glows (TheSearchableEverLampGlows); NEW: five capital 'O' tiles with no SecretDef were invisible walls in the Great Square and market — now ever-lamps (EverySecretTileHasItsSecret, all zones).
16. Night-only groups come and go at dusk/dawn where players are (NightPatrolArrivesAtDusk).
17. Crale with no crew left: no "LADS!", no phantom fight (CraleHasNoLadsLeft).
18. The first street's retry spot is out of the fight's sight: (44,15) (FirstStreetRetryIsOutOfSight).
19. A player who joins while the ending confirm (or rider prompt) is open is asked too (JoinerSeesTheEndingConfirm).
21. RulesTests runner: every group runs through Run(), which restores LevelCap and dice and reports a throw as a failure.
22. NEW: Spiritual Weapon's effect went on the target (and only on a hit), so the caster never got the Bonus Action strike; now the caster's, with its slot (SpiritualWeaponStrikesAgain — written after the fix; ruling).

Rulings:
- 4 (weak spot on spell attacks): Ruling: already fixed (spell attack hits go through ApplyHitResults → TryChoke since the Act II review); the headless test deadlocks on the dice queue (initiative tray first), so it was dropped — cost if wrong: a spell hit wouldn't choke.
- 20 (flaky AcademyGoldenPath): Ruling: watch only — green in both full runs since (488, 507) — cost if wrong: a flaky red in a future suite.
- WallOfFire preview test asserts 1..12 tiles (the forest map clips the wall), not exactly 12.
- The narrator box now hides while any window is open (its line is still in the log).
- Full suite after the batch: 520/524. The 4 puzzle failures were caused by fix 19: GameRoot._endingOffered survived QuitToTitle, so after JoinerSeesTheEndingConfirm every later DebugStart pushed the ending confirm (IsBusy → puzzles ignored). A real bug too (quit during the confirm → it pops up in the next game). Fixed: ResetWorld → ResetTurnOffer(); the joiner hook also needs Turn.Questioning.
- Title opening (TitleBackdrop + TitleScreen): Thistledown built on its own behind the title, golden-hour wash, fireflies, the camera drifting over the square, Brannoc/Elowen/Pip/Rhoswen/Buoy wandering; fade from black → title rises (1.6 s) → menu (3.8 s); any press skips. Canvas drawn by the title camera. Stale "playtest build"/"still being built" text replaced. Tests: TitleTests (3). Fix found: UnityEngine.Input isn't available (Input System project) → Keyboard.current.anyKey.
- HUD spacing (user, 2026-11-03): PlayerHud.Layout() fits the HUD to its width (640 full; 240-320 split). Bottom bands: hotbar · item label · prompt (56) · narrator (78). Clock narrows (≥96) then the status card; toasts in the column beside it, under the clock and the round tracker, hidden in narrow fights (lines in the log); hotbar slots shrink to fit; the key hint only when there's room; log shows only the lines that fit; narrow status card drops the class and shrinks/truncates; the combat ticker is placed by TickerSlot() and hides while the dice tray is open. Tests: HudLayoutTests (solo/split × exploring/fight): no overlaps, nothing off-screen, no text bigger than its box. Screenshots reviewed.
- Audio: one permanent AudioListener on GameRoot (the title camera's is gone): the log had 7340 "2 audio listeners" warnings, and a game could end up with none (no sound) depending on start-up order. 0 warnings after.
- Automated playtest, reading pass (TextFitTests): every node of every dialogue (453; companion/recruit need live context) solo + split, and 118 story beats on both screens. First run: 60 solo + 1190 split + 14 beat problems. Fixes: dialogue box fits the screen width and sizes itself to speaker + narration + line + options (MenuList.WrapRows: option rows wrap and grow); MessageScreen grows to its text (measured again after layout); window titles shrink onto one line; a line of headroom for pixel rounding. Now 0 / 0 / 0.
- Final: full suite 533/533, RulesTests 1436/1436, Build OK.
