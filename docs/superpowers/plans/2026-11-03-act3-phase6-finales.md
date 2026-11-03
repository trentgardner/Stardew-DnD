# Act III Phase 6: The Finales Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After the break, the party fights through Aurelhold in three street stages, against whoever stands against the ending they chose and beside whoever stands with it. Then the dragon comes down for one rider ("Who rides {dragon}?"), flies a cinematic route over the city, and lands the party into one of three multi-phase boss fights: the Ashen Queen (end her), the sealing defence against King Aldric in the Iron Regalia (seal her), or the Crowned Fire (save her). Every phase change plays its story beats however fast the party deals damage. A win ends on a "To Be Continued" card, which Phase 7 replaces with the endings.

**Architecture:** The rules engine gains phased bosses: `MonsterDef.Phases`, plus an HP floor so no hit can skip a phase. The game side adds `FinaleDb`, a static table built from the ending and Act II state. It holds the street fights' members, the allies, the flight route and the boss staging. A `GameRoot.Finale.AfterTheDoor.cs` partial runs the state machine: the stages, the rider prompt, the flight, the boss, retries and resuming after a load. Allies are AI monsters on `Team.Players` that join finale fights only. All new story text lives in `*.AfterTheDoor.cs` files, which the secret guard exempts.

**Tech Stack:** Unity 6000.5 / C#; the engine-free rules in `Assets/Scripts/Rules` are tested by `tools/RulesTests` (dotnet); play-mode tests run through `.superpowers/unity-test.sh`.

**Spec:** `docs/superpowers/specs/2026-11-02-act3-design.md` (sections "Back to the capital", "The three endings", "The four forces", "The dragon, grown", "The finale must not feel rushed"). The Phase 5 plan `docs/superpowers/plans/2026-11-02-act3-phase5-turn.md` defines everything this plan consumes (`Turn`, `TurnDb`, `Stance`, `PlayBeats`, `ChooseEnding`, `TheBreak`).

**Branch:** `act3/phase6-finales`. **Ledger:** `.superpowers/sdd/2026-11-03-act3-phase6-finales/progress.md` (force-add it; `.superpowers` is gitignored).

## Global Constraints

- Rules follow SRD 5.2 in spirit. Dice are always visible: every attack, save, check and damage roll in the flight and the fights goes through the dice tray.
- Every window closes with Esc/B and has a ✕. Closing the rider prompt never loses the flight: "Ride {dragon}" on the dragon reopens it.
- Co-op is first-class. Every beat goes to every ready player through `PlayBeats`. A player who joins mid-finale is warped to the party.
- Tone: bright fantasy, PG-13. No gore. **She is never the villain.** Every Ashen Queen beat makes clear the terror is the fire, not the girl. The game never says which ending was right.
- Secret guard: every new file with story text is named `*.AfterTheDoor.cs`, and each new file name is added to the list LoreTests checks (`LoreTests.cs:~189`). Nothing in the finale is reachable before `WorldDb.MetHer` + `Turn.Chosen`.
- Old saves load: new state is world flags only (no new save fields). Keep `MonsterDef.PhaseTwoAt` working unchanged for the Warden.
- Art goes through `Assets/Editor/ArtMap.txt` (Ninja Adventure, tinted) plus `.superpowers/import-art.sh`. Import fails silently on a compile error, so compile first. New ids: `mon_ashen_queen` (tint of `Actor/Boss/DragonGreen`, ember red), `mon_king_regalia` (tint of `Actor/Boss/GiantRedSamurai`, iron grey and gold), `mon_crowned_fire` (tint of `Actor/Boss/GiantFlam`, gold-white), `mon_wyrmwatch_hunter`, `mon_fae_knight`, `mon_academy_warden` (tints of existing human/fae sheets, the implementer's pick).
- Working names are approved in bulk; list the new ones in `docs/LORE_PITCH.md`.
- `World.DragonName` is used only when `World.EggHatched`; otherwise "the dragon".

## Review Focus

1. **One enormous hit at a phase threshold** (a crit Smite, a 7th-level spell, a dragon breath, a flight effect that pre-damages the boss) must still stop at the threshold and play the beats. A boss can't drop to 0 before its last phase. → Task 1 `NoHitSkipsAPhase`, Task 2 `HugeHitStillPlaysEveryBeat`.
2. **Saving and loading at every finale point** (just after the break, between street stages, at the rider prompt, after the flight before the boss falls) resumes in place. It never replays the flight and never skips a stage. Loading during the break's beats replays them: this fixes the Phase 5 deferred minor. → Task 4 `FinaleResumesAfterLoadAtEveryStage`, Task 5 `LoadAtRiderPromptReoffersIt`.
3. **A party wipe in a finale fight** carries the fallen back to that stage's retry spot in Aurelhold. Never the Thistledown temple, no lost day, no fee. Hardcore still ends the story. → Task 4 `FinaleWipeRetriesInTheCapital`.
4. **Co-op churn at the rider prompt and during the flight:** P2 drops while they are the candidate, or joins mid-flight. The prompt re-resolves to someone present, and the joiner is warped to the landing. → Task 5 `RiderPromptSurvivesADrop`.
5. **Allies:** party area spells hit allies as they would companions. Allies never give XP, loot or bond, never show on character sheets, and are removed when the fight ends (win or wipe). An ally at 0 HP "falls back", which is never a party wipe. → Task 3 `AlliesFightForYouAndLeave`.

---

### Task 1: Phased bosses and the finale's stat blocks (rules engine)

**Files:**
- Modify: `Assets/Scripts/Rules/Monster.cs` (add `BossPhaseDef`, `MonsterDef.Phases`, `Monster.PhaseIndex`, `Monster.PendingPhase`, the floor in `Monster.TakeDamage`, `RechargeMinFor`)
- Modify: `Assets/Scripts/Rules/Content/MonstersDb.cs` (make `MonstersDb` `partial`; call `BuildFinale()` at the end of the static ctor)
- Create: `Assets/Scripts/Rules/Content/MonstersDb.Finale.AfterTheDoor.cs`
- Test: `tools/RulesTests/FinaleBossTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public class BossPhaseDef { public double At; public string[] Beats; public int RechargeMin; public int LairEruptions; public int LegendaryPerRound; /* 0 = unchanged */ }
  // MonsterDef
  public List<BossPhaseDef> Phases = new List<BossPhaseDef>();   // ordered, At descending (e.g. 0.66 then 0.33)
  // Monster
  public int PhaseIndex;               // phases entered so far (0 = none)
  public BossPhaseDef PendingPhase;    // entered, beats not yet played; the game clears it via TakePendingPhase()
  public BossPhaseDef TakePendingPhase();
  public BossPhaseDef CurrentPhase => PhaseIndex == 0 ? null : Def.Phases[PhaseIndex - 1];
  ```
  `Monster.TakeDamage`: when `Def.Phases.Count > PhaseIndex`, the next threshold is `ceil(Phases[PhaseIndex].At * MaxHP)`. Damage that would take HP to or below it sets HP to exactly the threshold, increments `PhaseIndex`, and sets `PendingPhase`. Excess damage is lost. At most one phase per hit. The return value is the HP actually lost. After the last phase, normal damage applies. `RechargeMinFor` and the lair-eruption count read `CurrentPhase` when it is set, else the PhaseTwo fields as today. `LegendaryPerRound` read sites use `CurrentPhase?.LegendaryPerRound > 0 ? that : Def.LegendaryPerRound` (add `public int LegendaryBudget` on Monster and use it in `ResetLegendary`).
- Monster ids (all `Phases` beats are written in Task 6; here use one-line placeholder beats so the tests run):
  - `ashen_queen` "The Ashen Queen": AC 19, HP `19d12+133`, Speed 40, FlySpeed 80, Scores 27/10/25/16/13/23, PB 6, CR 17, Immune fire, Multiattack Rend×3 (Rend +14, `1d10+8` slashing, reach 10; the SRD fire rider folds into a separate `Ember Rend` attack, `2d4+8` fire, used once in the multiattack), Fire Breath (60 ft cone, DEX DC 21, `17d6` fire, recharge 5), Legendary 3/round: Tail (attack, `2d8+8`, reach 15, cost 1), Wing Buffet (SaveBurst, 15 ft, DEX DC 22, `2d6+8` bludgeoning, cost 2). Scale 2.2. Phases: At 0.66 (RechargeMin 4, LairEruptions 2), At 0.33 (RechargeMin 4, LairEruptions 4, LegendaryPerRound 4).
  - `king_regalia` "King Aldric, in the Iron Regalia": AC 20, HP `18d10+90`, Speed 30, Scores 20/12/20/14/14/20, PB 5, CR 14, Resist fire, MagicResistance, ParryBonus 3, Multiattack Sceptre×2 (Sceptre +11, `2d8+6` bludgeoning), ability "Chains of the Ring" (SaveBurst, 20 ft around him, STR DC 18, `4d8` force, recharge 5, EffectId `restrained`), Legendary 2/round: Sceptre (attack, cost 1). Scale 1.6. Phases: At 0.5 (RechargeMin 4).
  - `crowned_fire` "The Crowned Fire": AC 18, HP `22d12+110`, Speed 30, FlySpeed 60, Scores 24/14/22/14/16/24, PB 6, CR 17, Immune fire, Multiattack Flame Sceptre×2 (+13, `2d10+7` fire, reach 10), Pyre of Lamps (60 ft line, DEX DC 20, `16d6` fire, recharge 5), Legendary 3/round: Flame Sceptre (attack, cost 1), Searing Crown (SaveBurst, 10 ft, CON DC 20, `3d10` fire, cost 2). Scale 2.0. Phases: At 0.66 (LairEruptions 3), At 0.33 (RechargeMin 4, LairEruptions 4).
  - Force units (Team set at spawn; the same defs serve as foes and allies): `wyrmwatch_hunter` "Wyrmwatch Hunter" (AC 15, `9d8+18`, Multiattack Heavy Crossbow×2 `+7 1d10+4` piercing 100/400, Ranged behaviour, CR 4); `fae_knight` "Knight of the Courts" (AC 17, `10d8+30`, Multiattack Thorn Blade×2 `+8 1d8+5` slashing, Resist cold, CR 5); `academy_warden` "Academy Ward-Mage" (AC 15, `12d8+12`, ranged Arcane Bolt `+8 2d10` force 120 ft, ability Shatter-like SaveBurst 10 ft CON DC 16 `3d8` thunder recharge 5, CR 5).
  - Each `Description` ends with its SRD basis in brackets, like the existing ones.

- [ ] **Step 1: Write the failing tests** in `tools/RulesTests/FinaleBossTests.cs`:
  - `NoHitSkipsAPhase`: `var m = new Monster(MonstersDb.Get("ashen_queen"), rollHp:false); int t1 = (int)Math.Ceiling(0.66 * m.MaxHP); m.TakeDamage(9999, DamageType.Slashing, false, new List<string>()); Assert m.HP == t1, m.PhaseIndex == 1, m.PendingPhase == m.Def.Phases[0], !m.IsDown;` a second 9999 → HP == ceil(0.33·Max), PhaseIndex 2; a third → IsDown.
  - `TakePendingPhaseClearsIt`: after the first hit, `TakePendingPhase()` returns phase 0, and a second call returns null.
  - `WardenStillUsesPhaseTwo`: `warden` has no `Phases`; 9999 damage → IsDown (unchanged behaviour).
  - `PhaseChangesRechargeAndLegendaries`: after entering phase 2 of `ashen_queen`, `RechargeMinFor(breath) == 4` and `LegendaryBudget == 4`.
  - `FinaleBossesLastLongEnough`: for each of `ashen_queen`, `crowned_fire`, `king_regalia`: `party = BalanceTests.DamagePerRound(fighter13, AC) + BalanceTests.DamagePerRound(paladin13, AC) + Dragonling.ExpectedDamagePerRound(new Dragonling(13){Stage=DragonStage.Young}, AC)`. Then `rounds = AverageHp / party`. Assert `6 <= rounds <= 14` (the spec's "not two or three rounds").
  - `ForceUnitsAreRegistered`: all six new ids resolve through `MonstersDb.Get`, each has `SpriteId` set and `XP > 0`.
- [ ] **Step 2:** `cd tools/RulesTests && dotnet test --filter FinaleBossTests`. Expected: FAIL (compile: `BossPhaseDef` is not defined).
- [ ] **Step 3:** Implement the Monster changes and the defs per the Interfaces block. Tune HP within the dice expressions above only if `FinaleBossesLastLongEnough` fails; ledger any change.
- [ ] **Step 4:** `dotnet test` (the whole RulesTests project). Expected: all pass (1360 + 6).
- [ ] **Step 5:** Commit `Act III Phase 6: phased bosses and the finale's stat blocks`.

---

### Task 2: Phase beats in combat

**Files:**
- Modify: `Assets/Scripts/Game/Combat/CombatTurns.cs` (`OnDamaged`, `AiTurn`, `LegendaryOpportunities`, lair eruptions)
- Modify: `Assets/Scripts/Game/Combat/CombatInstance.cs` (`public bool BeatsPlaying`; the round loop waits on it)
- Test: `Assets/Tests/PlayMode/FinaleTests.cs` (new; shared helpers for Tasks 2–7 live at its top)

**Interfaces:**
- Consumes: Task 1's `PendingPhase`, `TakePendingPhase()`, `BossPhaseDef.Beats`; `GameRoot.PlayBeats(PlayerSlot by, string[] beats, Func<int, IEnumerator> before = null)`.
- Produces: `CombatInstance.BeatsPlaying` (true while a phase's beats are on screen). `public static event Action<Monster, BossPhaseDef> CombatTurns.PhaseEntered` (Task 6 subscribes, for the seal's chain pull).

`OnDamaged`: when `boss.TakePendingPhase()` returns a phase, set `BeatsPlaying = true` and start `G.PlayBeats(firstHuman, phase.Beats)` at once (the modal screens block human input). When it finishes, set `BeatsPlaying = false`, pop "PHASE n" over the boss, raise `PhaseEntered`. `AiTurn`, `LegendaryOpportunities`, lair eruptions and the round loop each start with `while (BeatsPlaying) yield return null;`. The existing PhaseTwo narration path stays as it is.

- [ ] **Step 1: Write the failing tests:**
  - `HugeHitStillPlaysEveryBeat`: DebugStart a level-13 fighter, start a fight with one `ashen_queen` (a test-only group in the `capital` zone, via the existing test helper pattern in `LairTests`). Deal 9999 through the fight's damage path (`DebugDamage(unit, 9999)`; add that tests-only helper next to `DebugAiTurn` if no equivalent exists). Assert `BeatsPlaying`, the top screen is a `MessageScreen` whose text == `Phases[0].Beats[0]`, and the boss is not down. Close the beats → `!BeatsPlaying`. Repeat for phase 2, then a third hit → victory.
  - `AiWaitsForBeats`: while `BeatsPlaying`, run `DebugAiTurn(boss)` for 30 frames. The party's HP is unchanged until the beats close.
- [ ] **Step 2:** `bash .superpowers/unity-test.sh DnD.Tests.FinaleTests`. Expected: FAIL.
- [ ] **Step 3:** Implement per the Interfaces block.
- [ ] **Step 4:** Run `DnD.Tests.FinaleTests` and `DnD.Tests.VolcanoTests` (the Warden is unchanged). Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 6: phase changes stop the fight for their beats`.

---

### Task 3: Allies in finale fights

**Files:**
- Create: `Assets/Scripts/Game/Quests/FinaleDb.AfterTheDoor.cs` (the `Finale` constants and `FinaleDb`; this task adds `Allies`)
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (`StartGroupCombat`: after `AddDragonTo`, call `AddFinaleAllies(c)`; `OnCombatFinished`: `RemoveFinaleAllies(c)` before any other handling)
- Create: `Assets/Scripts/Game/Core/GameRoot.Finale.AfterTheDoor.cs` (`AddFinaleAllies`, `RemoveFinaleAllies`)
- Modify: `Assets/Scripts/Game/Combat/CombatInstance.cs` (`Unit.Ally` bool; XP, loot and the wipe check skip allies)
- Test: `Assets/Tests/PlayMode/FinaleTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public static class Finale {
      public const string Prefix = "finale_";                         // every finale group id starts with it
      public static bool IsFinaleGroup(string groupId) => groupId != null && groupId.StartsWith(Prefix);
  }
  public static class FinaleDb {
      /// One ally per force that stands With (Turn.Forces order), plus "crown_knight" named "Household Guard" when TurnDb.MarshDefects. At most 3.
      public static List<(string monsterId, string name)> Allies(string ending, WorldState w);
  }
  ```
  Force → ally: crown → (`crown_knight`, "Crown Knight"); academy → (`academy_warden`, "Ashby's Ward-Mage"); fae → (`fae_knight`, "Knight of the Courts"); hunters → (`wyrmwatch_hunter`, "Wyrmwatch Hunter").
- `AddFinaleAllies(CombatInstance c)`: only when `Finale.IsFinaleGroup(c.GroupId)`. For each ally, spawn a plain `Actor` (sprite from the def) on the nearest free walkable tile within 3 tiles of the first human, with `Monster.Team = Team.Players`, and `c.AddUnit(m, actor)` with `Ally = true`. Allies are AI-driven: `Ctrl` is null and `Hostile` is team-based already.
- An ally at 0 HP: narrate "{name} falls back, hurt." and remove its actor (never counted as a party member down).

- [ ] **Step 1: Write the failing tests:**
  - `AlliesTableFollowsReactions`: build a `WorldState` with `ending_end` and nothing else: `Allies(End)` contains `crown_knight` and `wyrmwatch_hunter` (both With in `TurnDb.Base`). With `ending_save` + `banner_fae`: contains `fae_knight`, and no `crown_knight` unless `banner_crown` (then also "Household Guard"). Never more than 3.
  - `AlliesFightForYouAndLeave`: start a test finale group (id `finale_test`, two `crown_knight` foes) with `ending_end` set. Assert 2 ally units (`Ally == true`, `C.Team == Team.Players`). `DebugAiTurn(ally)` damages a foe and no PC. Win → no ally actors remain in the scene, party XP == the foes' XP split as usual, and no ally is in `CompanionsOf(p)`.
  - `AllyDownIsNotAWipe`: set every ally's HP to 0 via the damage path. The fight continues and no `PartyWipe` starts.
- [ ] **Step 2:** Run `DnD.Tests.FinaleTests`. Expected: FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run `DnD.Tests.FinaleTests` and `DnD.Tests.ActThreeFightTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 6: the forces that stand with you fight beside you`.

---

### Task 4: The fight through the streets (the state machine, stages, retry, resume)

**Files:**
- Modify: `Assets/Scripts/Game/Quests/QuestDb.cs` (WarmCity: replace `Finale = 10` with `Streets = 10, Flight = 11, Boss = 12, Won = 13`; StageText 10–13; StageXp `[Streets] = 3000, [Flight] = 3000, [Won] = 12000`)
- Modify: `Assets/Scripts/Game/Quests/FinaleDb.AfterTheDoor.cs` (street members, spots, beats)
- Create: `Assets/Scripts/Game/World/WorldDb.Finale.AfterTheDoor.cs` (the three street groups in `capital`, called from `WorldDb` build like `BuildTheTurn`)
- Modify: `Assets/Scripts/Game/Core/GameRoot.TheTurn.AfterTheDoor.cs` (`TheBreak`: replace the To Be Continued push with `BeginFinale(by, ending)`)
- Modify: `Assets/Scripts/Game/Core/GameRoot.Finale.AfterTheDoor.cs` (`PrepareFinale`, `BeginFinale`, `CheckTheFinale`, stage advance, the End path's dragon)
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (`OnGroupResolved` → `OnFinaleGroupResolved(groupId)`; `PartyWipe` finale branch; load calls `CheckTheFinale()` next to `CheckTheTurn()`; `AddDragonTo` skips the dragon while `Finale.DragonWaits`)
- Modify: `Assets/Tests/PlayMode/TurnTests.cs` (the break no longer ends on a To Be Continued card: update `TurnTests.cs:531` and `:662`, which expect it, to expect the first street intro instead)
- Test: `Assets/Tests/PlayMode/FinaleTests.cs`

**Interfaces:**
- Produces (in `Finale` / `FinaleDb`):
  ```csharp
  public const int Stages = 3;
  public static string StreetGroup(int n) => "finale_street_" + n;          // n = 1..3
  public static string StreetOpen(int n) => "finale_street_open_" + n;      // the group's RequiresFlag
  public static string StreetDone(int n) => "finale_street_done_" + n;      // the group's ResolvedFlag
  public const string Started = "finale_started", DragonWaits = "finale_dragon_waits";
  public static int CurrentStreet(WorldState w);   // 1..3 = the first open, not-done stage; 0 = none open; 4 = all done
  public static List<EnemySpawn> StreetMembers(int n, string ending, WorldState w);
  public static Vector2Int StreetSpot(int n);      // where the group stands
  public static Vector2Int RetrySpot(int n);       // where a wiped party wakes (a few tiles back along the route)
  public static string[] StreetIntro(int n, string ending, WorldState w);   // 1–2 beats before each stage's fight
  ```
- Stage spots in `capital`: 1 = the Great Square below the Sunstair (around (31,19)); 2 = the Grand Market (around (44,26)); 3 = the head of the Roots stair (around (55,30)). The implementer takes the nearest walkable tiles. The tests assert walkability and a path from `WorldDb.SunstairSpot`.
- `StreetMembers` composition (at most 6, fixed order, spread on walkable tiles around `StreetSpot`):
  - End core: s1 `cinder_hound`×2; s2 `fire_elemental` + `cinder_hound`×2; s3 `fire_elemental`×2 + `salamander`. (The lamps went out; the fire coming up through the Roots.)
  - Seal/Save core (the guards who stayed): s1 `crown_knight`×2; s2 `crown_knight`×2 + `crown_battlemage`; s3 `crown_champion` + `crown_knight`×2. When `TurnDb.MarshDefects`, drop one `crown_knight` from each stage.
  - Plus, for each force other than crown whose `TurnDb.Reaction` is Against: academy → s1 `academy_warden`, s2 `stone_guardian`; fae → s1 `fae_knight`, s3 `thorn_guardian`; hunters → s2 `wyrmwatch_hunter`×2.
- Groups: `EncounterStyle.None`, `Unique`, `SightTiles = 8`, `RequiresFlag = StreetOpen(n)`, `ResolvedFlag = StreetDone(n)`, Label "the fighting in the {square|market|Roots}". `PrepareFinale(ending)` clears and refills each group's `Members` from `StreetMembers`. Run it before any capital spawn: in `ChooseEnding`, and on load before `BuildWorld` spawns.
- `BeginFinale(by, ending)`: set `Started`; for End also set `DragonWaits`, the dragon `StaysPut` at the Roots stair head, and narrate that it won't come. Then `Quests.Set(WarmCity.Id, Streets)`, set `StreetOpen(1)`, play `StreetIntro(1)`.
- `OnFinaleGroupResolved(StreetGroup(n))`: n < 3 → set `StreetOpen(n+1)` and play `StreetIntro(n+1)`. n == 3 → `Quests.Set(Flight)` and start Task 5's `BeginFlight(by, ending)` (Task 4 leaves a stub that narrates "The dragon comes." so its tests run).
- `CheckTheFinale()` (on load and on zone entry): if `Turn.Chosen` and not `Started`, replay `TheBreak` (this fixes Phase 5's deferred minor), then begin. If `Started`, re-run `PrepareFinale`; the open stage's group respawns through the normal `SpawnGroupIfNeeded`.
- `PartyWipe` when `Turn.Chosen && !w.Flag(Finale.Won)` and not Hardcore-everyone: no `SleepUntil`, no fee. Revive at half HP as today, warp to `RetrySpot(CurrentStreet)` (or the boss landing spot from Task 6 once `Flown`), and narrate "You come to behind a barricade. The fighting hasn't stopped."
- StageText 10: "Aurelhold is fighting itself. Get through the city: the Great Square, the Grand Market, the head of the Roots stair." 11: "The way is open, and the sky is the only road left." 12: "The last fight for Aurelhold." 13: "It's over. (What comes after is in the next update.)"

- [ ] **Step 1: Write the failing tests:**
  - `StreetMembersFollowTheTable`: End with no hooks → s1 is exactly `cinder_hound`×2 and has no human foes. Seal with `banner_crown` → s1 has one `crown_knight` (Marsh defected), plus `fae_knight` only if the fae are Against. Every stage ≤ 6.
  - `StreetSpotsAreReachable`: for n in 1..3, `StreetSpot(n)` and `RetrySpot(n)` are walkable in `capital` and reachable from `SunstairSpot`.
  - `StreetsRunInOrder`: a Phase-5 golden setup (reuse `TurnTests`' helper that reaches the choice), choose Seal. Assert `warm_city` == Streets, only `StreetGroup(1)` is spawned, no To Be Continued screen. Resolve groups 1 then 2 (through `OnCombatFinished(victory)` on real fights, using the existing test kill helper). Each one opens the next and plays its intro. After 3: `warm_city` == Flight.
  - `EndPathDragonStaysOut`: choose End. The dragon is not a unit in the s1 fight and stands at the Roots stair head.
  - `FinaleWipeRetriesInTheCapital`: wipe in s2 → the player is in `capital` at `RetrySpot(2)`, `Clock.Day` unchanged, copper unchanged, and the s2 group is back at full strength.
  - `FinaleResumesAfterLoadAtEveryStage`: save → load at (a) right after `ChooseEnding`, before the beats finish: the break's beats replay, then s1 opens; (b) after s1 done: s2 is open and s1 is not respawned; (c) after s3 done: `warm_city` == Flight.
- [ ] **Step 2:** Run `DnD.Tests.FinaleTests`. Expected: FAIL.
- [ ] **Step 3:** Implement per the Interfaces block. Write the `StreetIntro` beats: 2 per stage per ending, warm and wry, PG-13, with no gore. On End, the beats show the fire, never the girl, as the enemy.
- [ ] **Step 4:** Run `DnD.Tests.FinaleTests` and `DnD.Tests.TurnTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 6: the fight through the streets of Aurelhold`.

---

### Task 5: "Who rides {dragon}?" and the flight

**Files:**
- Modify: `Assets/Scripts/Game/Quests/FinaleDb.AfterTheDoor.cs` (`FlightRoute`, `FlightPass`, `FlightEffect`)
- Create: `Assets/Scripts/Game/Core/GameRoot.Flight.AfterTheDoor.cs` (`BeginFlight`, the rider prompt, `Fly`)
- Modify: `Assets/Scripts/Game/World/DragonPet.cs` (a "Ride {dragon}" interaction while `Finale.RiderPending`; `FlyTo(Vector2 world, float tilesPerSec)` moves the dragon actor ignoring walkability and draws a shadow)
- Modify: `Assets/Editor/ArtMap.txt` (no new sprite for the rider: the rider's avatar is drawn on the dragon's back, offset and sorted above it)
- Test: `Assets/Tests/PlayMode/FinaleTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  // Finale
  public const string RiderPending = "finale_rider_pending", Flown = "finale_flown";
  public static string RiderFlag(int playerIndex) => "finale_rider_" + playerIndex;
  public enum FlightEffect { BossWounded, NoLegendaryFirstRound, AddsScattered, PartyInspired }
  public class FlightPass { public string Text; public List<(string label, Rules.Skill? skill, Rules.Ability ability, int dc, FlightEffect onSuccess, string success, string failure)> Options; }
  public class FlightRoute { public List<Vector2Int> Waypoints; public FlightPass[] Passes; /* exactly 2 */ public string LandingZone; public Vector2Int LandingSpot; public string[] Takeoff, Landing; }
  public static FlightRoute Route(string ending);
  // GameRoot
  public void BeginFlight(PlayerSlot by, string ending);
  public void OfferRider();                                  // opens the prompt for every ready player (or skips it, solo)
  public void ConfirmRider(PlayerSlot rider);                // nothing starts until this
  public IEnumerator Fly(PlayerSlot rider, string ending);   // the sequence; ends by starting Task 6's boss fight
  public static List<FlightEffect> FlightResults;            // Task 6 reads and clears these when the boss fight starts
  ```
- The routes (waypoints in `capital`; the implementer picks tiles along the described line; landing per Task 6):
  - End: from the Roots stair head, up over the Grand Market to the Lantern Cathedral spire (9,10), where the Ashen Queen has coiled; the dive drives her down into the Great Square. LandingZone `capital`, at the statue.
  - Seal: from the Roots stair head out over the Wyrmback's great vent ("the Chimney", where the warm air comes up) and straight down it. LandingZone `the_deep`, before the grey door. Ashby rides behind the rider (narrated).
  - Save: up over the Great Square to the palace roof, where the Crowned Fire stands; it drops to the Sunstair to meet you. LandingZone `capital`, at the Sunstair.
- Two passes per route, each with two options. The rider rolls a visible check against the DC (or the dragon makes its breath save DC against the target, shown in the tray). Success adds the option's effect. Failure costs the rider `3d6` fire (never below 1 HP) and a near-miss line. Values: DCs 15 and 17. Example (End, pass 1): "Breathe on her as she climbs" (DEX save from her, DC = the dragon's breath DC → `BossWounded`: she starts the fight 10% below full, still above the first threshold) / "Hold tight and go under her wing" (Animal Handling DC 15 → `NoLegendaryFirstRound`). Each route uses each of the four effects at least once across its four options.
- Rider prompt: solo → no prompt. Co-op → every ready player sees ChoiceScreen "Who rides {dragon}?" with one option per ready player (by character name). A pick makes that player the candidate. The candidate sees "Take the reins" / "Let {other} ride" (one press hands it over: the other becomes the candidate). The others see "{candidate} is climbing up..." (closable). Esc or ✕ on any screen closes it and leaves `RiderPending` set; "Ride {dragon}" on the dragon reopens it. A candidate who drops → the prompt reopens for whoever is left. Confirm → `RiderFlag(index)`, clear `RiderPending`, `Fly`.
- `Fly`: `PlayBeats(Takeoff)`. Hide the rider's avatar and draw it on the dragon. Every ready player's camera follows the dragon. Fly waypoint to waypoint (~6 tiles/s), stopping for each pass (the rider picks; the others see the pass text and the roll). Then `PlayBeats(Landing)`. Warp every ready player and their companions next to `LandingSpot`, set `Flown`, `Quests.Set(Boss)`, and call Task 6's `StartFinaleBoss(ending)`. On End, clear `DragonWaits` before takeoff with the beat: "It isn't her any more. {name} knows it before you do."

- [ ] **Step 1: Write the failing tests:**
  - `SoloRidesWithoutAPrompt`: solo, after s3 → no ChoiceScreen titled "Who rides"; after the takeoff beats, `RiderFlag(0)` is set.
  - `CoopRiderPromptHandsOverInOnePress`: co-op, P1 picks P1. P1's screen offers "Let {P2} ride"; pressing it makes P2 the candidate with "Take the reins". Nothing has flown (`!Flown`, dragon at its spot) until P2 confirms. Then `RiderFlag(1)`.
  - `RiderPromptSurvivesADrop`: P2 is the candidate and drops out → P1 sees the prompt again.
  - `EscOnThePromptNeverLosesTheFlight`: Esc → `RiderPending` is still set; interact with the dragon → the prompt reopens.
  - `LoadAtRiderPromptReoffersIt`: save/load with `RiderPending` → the prompt (or the solo takeoff) comes back; `!Flown`.
  - `FlightRollsAreVisibleAndCount`: force each pass's roll (the existing dice test hook) to succeed → `FlightResults` holds both effects. Force fail → the rider has ≥ 1 HP and lost HP, and `FlightResults` is empty.
  - `FlightLandsEveryone`: co-op, after `Fly` → both players and the dragon are in `Route(e).LandingZone` within 3 tiles of `LandingSpot`; `Flown`; `warm_city` == Boss. Run for all three endings.
- [ ] **Step 2:** Run `DnD.Tests.FinaleTests`. Expected: FAIL.
- [ ] **Step 3:** Implement. Write the Takeoff/Landing beats (2 each per ending) and the pass texts, so the flight feels big: the city burning or dark below, the ring of the Wyrmback, the cold air, the dragon's joy.
- [ ] **Step 4:** Run `DnD.Tests.FinaleTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 6: Who rides the dragon? The flight over Aurelhold`.

---

### Task 6: The three boss fights

**Files:**
- Modify: `Assets/Scripts/Rules/Content/MonstersDb.Finale.AfterTheDoor.cs` (the real `Phases[].Beats`)
- Modify: `Assets/Scripts/Game/World/WorldDb.Finale.AfterTheDoor.cs` (boss groups: `finale_boss_end` in `capital`, `finale_boss_seal` in `the_deep`, `finale_boss_save` in `capital`; each `EncounterStyle.None`, `Unique`, `RequiresFlag = Finale.Flown` plus the ending flag)
- Modify: `Assets/Scripts/Game/Core/GameRoot.Finale.AfterTheDoor.cs` (`StartFinaleBoss`, flight effects, the seal's progress, the win)
- Modify: `Assets/Editor/ArtMap.txt` (the six ids from Global Constraints; run the import)
- Test: `Assets/Tests/PlayMode/FinaleTests.cs`

**Interfaces:**
- Consumes: `FlightResults`, `Route(e).LandingSpot`, `CombatTurns.PhaseEntered`, `FinaleDb.Allies`.
- Produces:
  ```csharp
  public static string BossGroup(string ending) => "finale_boss_" + ending;
  public const string Won = "finale_won";
  public const int SealRounds = 7;
  public void StartFinaleBoss(string ending);              // spawns (if needed) and starts the boss fight at once
  public int SealProgress { get; }                         // seal path: 0..SealRounds, for the HUD line and tests
  public static string[] Victory(string ending, WorldState w);   // 2 beats after the win
  ```
- Groups: End = `ashen_queen` alone. Seal = `king_regalia` + `crown_knight`×2 (the Ring-knights). Save = `crowned_fire` alone. Each spawns at the landing spot ±4 tiles.
- Flight effects at the start of the boss fight: `BossWounded` → the boss loses 10% of max HP, clamped above its first threshold. `NoLegendaryFirstRound` → `LegendaryLeft = 0` for round 1. `AddsScattered` → remove one non-boss member (seal) or one lair eruption from phase 1 (others). `PartyInspired` → every PC gets Heroic Inspiration.
- **Seal:** Ashby is an NPC actor at the grey door, not a combat unit. The HUD shows "The seal: n/7". +1 at the end of every round. On the King's `PhaseEntered` (At 0.5): his beat ends with "He hauls on the chains, and the circle flickers" and −1 progress (once). At 7: the King's beats from the seal win play, and the fight ends as a victory (the remaining foes yield). Beating the King first also wins; the seal then completes in the victory beats.
- The win (any ending): set `Won`, `Quests.Set(Won)`, `PlayBeats(Victory(ending))`, then every ready player gets MessageScreen "To Be Continued": "The fight for Aurelhold is won. What comes after is in the next update. Everything you chose is saved." (Phase 7 replaces it; ledger it.)
- Beats (implementer writes them; 2–3 per phase, warm and wry, PG-13):
  - The Ashen Queen: 0.66, the fire roars through the city's empty lamps and calls itself by the legend's name; 0.33, for one breath, inside the fire, a small voice asks where everyone went, and the fire answers for it. Victory: the fire goes out, and it is very quiet. (Phase 7 writes what is left.)
  - The King, in the Iron Regalia: 0.5, the confession turned into rage: "Every lamp in this city is hers. Do you know what it costs to keep a kingdom warm?"
  - The Crowned Fire: 0.66, Aldric's voice is still in it, giving orders to no one; 0.33, the fire stops obeying him. Victory: the fire leaves him and goes home into the twelve Great Ones; the mountains glow, all the way round the ring.
- A wipe at the boss → retry at `Route(e).LandingSpot` (Task 4's wipe branch). The boss group resets, the phases replay their beats, and `SealProgress` resets.

- [ ] **Step 1: Write the failing tests:**
  - `EachBossStartsWhereItLands`: for each ending, after `Fly` the boss fight is running in `Route(e).LandingZone` with the right members and any allies.
  - `FlightEffectsApply`: with `FlightResults = {BossWounded, NoLegendaryFirstRound}` → the boss starts at 90% HP and `LegendaryLeft == 0` in round 1.
  - `SealHoldsForSevenRounds`: seal fight, the party passes 7 rounds (the existing end-turn test helper; the party can't be downed: set PC HP high). `SealProgress` reaches 7, the fight ends in victory, and `Won` is set. With the King's phase entered at round 2, it takes one more round.
  - `BeatingTheKingAlsoSeals`: kill the King (two phase stops) before round 7 → victory.
  - `TheWinIsSavedAndAnnounced`: after any victory → `warm_city` == Won, the To Be Continued screen is up, and `Quests.Active(WarmCity.Id)` is false (13 is Last).
  - `BossWipeRetriesAtTheLanding`: wipe → players at `LandingSpot`, `Flown` still set, the flight not replayed, and the boss back at full HP.
- [ ] **Step 2:** Run `DnD.Tests.FinaleTests`. Expected: FAIL.
- [ ] **Step 3:** Implement, write the beats, and add the art lines. Run `.superpowers/import-art.sh`. `ArtTests` (the existing test that every `SpriteId` resolves) must pass.
- [ ] **Step 4:** Run `DnD.Tests.FinaleTests`, `DnD.Tests.LoreTests` and `DnD.Tests.ArtTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 6: the Ashen Queen, the sealing defence, and the Crowned Fire`.

---

### Task 7: Golden paths, screenshots, docs, verify, merge

**Files:**
- Test: `Assets/Tests/PlayMode/FinaleTests.cs` (golden paths); `Assets/Tests/PlayMode/ScreenshotTests.cs` (finale shots)
- Modify: `docs/DESIGN.md` ("The finales (Act III Phase 6)"), `docs/LORE_PITCH.md` (new names), `docs/PLAYTEST.md` (how to reach and play each finale; the rider prompt), the ledger

- [ ] **Step 1: Golden path tests** `FinaleGoldenPathEnd`, `FinaleGoldenPathSeal`, `FinaleGoldenPathSave` (solo), and `FinaleGoldenPathCoopSave` (co-op, P2 rides). Each one goes from the choice through s1–s3, the rider prompt, both passes, the landing, every phase beat, the win, and the To Be Continued card. Each asserts every beat text in order (collected from the MessageScreens) and that no window was left open between stages.
- [ ] **Step 2: Screenshots** (`ScreenshotTests`, PNGs to `%TEMP%/embers_screenshots`): each street stage at fight start (End and Seal), the rider prompt in co-op, mid-flight over the city, each boss at fight start and at a phase beat, the seal HUD line. Review every PNG; fix overlaps, off-map spawns, a boss sprite covering the party, unreadable text. Ledger each fix.
- [ ] **Step 3:** Docs. DESIGN gets: the flags, `FinaleDb` tables (street members, allies, routes, effects), the phase engine and its HP floor, the seal's rules, the wipe and resume rules, and the To Be Continued placeholder for Phase 7. LORE_PITCH gets: the Iron Regalia, the Crowned Fire, the Chimney, Knight of the Courts, Wyrmwatch Hunter, Academy Ward-Mage. PLAYTEST gets: the finale steps per ending.
- [ ] **Step 4:** Full verification: `dotnet test tools/RulesTests` (all pass) and `bash .superpowers/unity-test.sh` (the full suite; all pass; rerun any order-flaky test alone and ledger it). Build the exe + zip (`Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.BuildWindows`); expect "Build OK".
- [ ] **Step 5:** Final branch review (one reviewer over the whole diff), fix findings, rerun the affected tests, commit. Merge `act3/phase6-finales` into main with `--no-ff` and push. Update the project-status memory (NEXT: Phase 7, the endings).
