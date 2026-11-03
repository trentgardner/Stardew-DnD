# SDD ledger — plan: docs/superpowers/plans/2026-11-03-act3-phase6-finales.md

Executor: executing-plans (inline), branch act3/phase6-finales. Spec: docs/superpowers/specs/2026-11-02-act3-design.md.

## Pre-flight
- T1→T2: T1 produces PendingPhase/TakePendingPhase/Beats; T2 consumes the same names. OK.
- T1→T3/T4: force-unit ids (wyrmwatch_hunter, fae_knight, academy_warden) used by Allies and StreetMembers. OK.
- T3→T4: Finale.Prefix "finale_"; street groups "finale_street_n" match IsFinaleGroup. OK.
- T4→T5: OnFinaleGroupResolved(s3) → BeginFlight stub replaced by T5. OK.
- T5→T6: FlightResults, Route(e).LandingSpot, StartFinaleBoss(ending). OK.
- T4/T6: Finale.Won (flag) vs WarmCity.Won (stage int): different classes, both named Won. OK.
- T2→T6: CombatTurns.PhaseEntered event used by the seal. OK.

## Task 1
- Task 1: Ruling: FinaleBossesLastLongEnough uses the median level-13 party of four (8-class pool, dragon not counted), not fighter+paladin+dragon — matches the Warden/Gaoler balance tests already in BossTests — cost if wrong: bosses a little shorter for strong pairs.
- Task 1: Ruling: boss HP raised past the plan's dice (Queen 28d12+196 = 378, King 30d10+150 = 315, Crowned Fire 32d12+160 = 368) — the plan's values gave 3.8-4.8 rounds, under the spec's floor — cost if wrong: long fights; Phase 7's balance table can trim.
- Task 1: Ruling: Chains of the Ring deals force damage only (no restrained) — the only restrain effect ("entangle") needs concentration on the caster — cost if wrong: the King's area attack is plainer.
- Task 1: Ruling: lair eruptions now read Monster.LairEruptionsNow (CombatTurns), so phases drive them; the Warden keeps PhaseTwo behaviour. Ally/foe XP values chosen from SRD CR.
Task 1: complete (commits 1fa4251..9b2d189, tests: bash -c 'cd tools/RulesTests && dotnet run 2>&1 | tail -1 | grep ' 0 failed'' → 1406 passed, 0 failed)

## Task 2
- Task 2: Ruling: the tests read the top open MessageScreen (Hud.Top, skipping closed ones) instead of Hud.Find — closed screens aren't removed while the initiative dice is open (PlayerHud.Update) — cost if wrong: none (test helper only).
- Task 2: note: in real play a phase beat pushed while the dice tray is open waits for the tray to close before it can be dismissed (existing Hud rule). Acceptable.
- Task 2: green: FinaleTests+VolcanoTests 24/24.
Task 2: complete (commits 9b2d189..74d547d, tests: unity-test.sh FinaleTests;VolcanoTests → 24/24 pass)

## Task 3
- Task 3: Ruling: PlayersUp excludes allies (Unit.Ally) — otherwise a wiped party with a standing ally would never lose — cost if wrong: none.
- Task 3: Ruling: Marsh's Household Guard is listed first (before the With forces) so it survives the 3-ally cap — she is the spec's named defector — cost if wrong: one fewer force soldier in a 4-way-With save run.
- Task 3: Ruling: RED for the Task 3 tests was a compile failure (FinaleDb/FinaleAlly/Unit.Ally absent) — not run separately; the first real run failed on a test-baseline bug (zone-discovery XP), fixed in the test.
Task 3: complete (commits 74d547d..HEAD, tests: unity-test.sh FinaleTests;ActThreeFightTests → 7/7 pass)

## Task 4
- Task 4: Ruling: stage 2 is "the Wyvern Yard" at (52,25) (outside the Gilded Wyvern), not the Grand Market — the market stalls are on the west side, but the route runs east from the square to the Roots stair (59-60,30) — cost if wrong: a name.
- Task 4: Ruling: StreetMembersFollowTheTable uses End + fae_clause for "only the fire" — the base table puts the Fae Against on End, so End with no hooks also has a fae_knight (plan's test text was wrong) — cost if wrong: none.
- Task 4: Ruling: the dragon's End-path refusal uses CompanionAgent.Waiting (already excluded from fights and warps) plus StaysPut at FinaleDb.DragonWaitSpot (58,29); AddDragonTo unchanged — cost if wrong: none.
- Task 4: Ruling: PlayBeats gains an optional title; stage intros are titled screens, which is how TurnTests' PlayBreak now knows the break is over. TurnGoldenPath line 738 now expects Streets after the break.
- Task 4: Ruling: TheBreak made internal so CheckTheFinale can replay it after a load (fixes Phase 5's deferred "load during the break's beats").
- Task 4: tests: FinaleTests+TurnTests 38/39 then TurnTests fix → Turn+Lore+Quest 70/70 (FinaleTests all green in the first run; RED was compile).
Task 4: complete (commits 8fe4e79..HEAD, tests: FinaleTests;TurnTests;LoreTests;QuestTests → green)

## Task 5
- Task 5: Ruling: TDD order broken: GameRoot.Flight.AfterTheDoor.cs was drafted before its tests; tests were then written against the spec/plan behaviours and the first run caught two real bugs (below) — cost if wrong: weaker proof than test-first for paths the tests don't hit.
- Task 5: Ruling: FinaleDb.Route takes (ending, WorldState) — the beats use the dragon's name — cost if wrong: none.
- Task 5: Ruling: every pass option is a rider skill check (d20 + skill vs DC 15/17, in the dice tray); a failure's 3d6 fire is narrated with its breakdown rather than rolled in the tray — cost if wrong: the scorch is less "visible" than the check.
- Task 5: Ruling: Esc on a pass picks its first option (the flight can't stall); Esc on the rider prompt leaves RiderPending, and "Ride {dragon}" on the dragon reopens it.
- Task 5: Ruling: the dragon flies by moving its actor's transform (Waiting + StaysPut hold it), not a DragonPet.FlyTo; the rider's controller is disabled and their avatar rides on the dragon (YSort offset +40); every camera follows the dragon via _flightCam.
- Task 5: bug found + fixed: _flying leaked across QuitToTitle/LoadGame (StopAllCoroutines killed Fly mid-air) so no later flight could start — ResetWorld now calls ResetFlight().
- Task 5: bug found + fixed: CombatInstance's initiative loop rolled for a player whose HUD was destroyed (dropped out) or kept waiting after the fight ended — it now stops once Finished and auto-rolls for a player who left.
- Task 5: tests: FinaleTests;CoopTests;VolcanoTests → 65/65.
Task 5: complete (commits 9077da3..HEAD, tests: FinaleTests;CoopTests;VolcanoTests → 65/65)
