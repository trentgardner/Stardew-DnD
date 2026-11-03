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
