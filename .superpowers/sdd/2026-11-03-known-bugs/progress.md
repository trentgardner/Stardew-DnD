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
