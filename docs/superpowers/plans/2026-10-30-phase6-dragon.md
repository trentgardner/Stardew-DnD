# Phase 6: The Baby Dragon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The hatchling becomes a real party member: a hatching scene worth the climax, a typed name, pet interactions with a light bond, Sniff and brazier-lighting in the world, and a BG3 "Us"-level combatant that either player can command and that retreats scared instead of dying.

**Architecture:** Rules get an engine-free `Dragonling : Monster` (stats scale with party level, Ember Breath with a recharge roll), tested in tools/RulesTests. The game keeps one persistent `Dragonling` creature on `GameRoot.Dragon`, with its persistent state (HP, scared, bond, sniff, lit braziers) in `WorldState` and the save. The existing `CompanionAgent` with `IsDragon` stays the world body; its "leader" becomes whichever ready player is nearest. Combat adds the dragon as a unit like a companion, and `PlayerTurn` works on any `Creature` so a dragon turn uses a short action list of its own.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8, `dotnet run`), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`, full suite ~12 min, background, muted), art via `Assets/Editor/ArtMap.txt` + `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md ("The baby dragon", "Art pipeline", build order item 6).

## Global Constraints

- Anti-goal: an overpowered dragon. It must be "Us-level (BG3): useful, never fight-carrying."
- Stats (spec): AC 13, about 10 HP that scales with party level, bite 1d4+2, **Ember Breath** (5-ft cone, 1d6 fire, DEX save, recharge 5–6). At 0 HP it "retreats, scared" until the next short rest; it never dies.
- Owned by the **whole party**: it follows the nearest player in co-op; either player can interact with it or command it.
- Sniff: highlights SecretInteractables within range, **once per short rest**. It can light torches and braziers with a tiny flame.
- Bond rises with interaction; light, **no fail state**.
- Data hooks only for growth: `DragonStage { Hatchling, Wyrmling, Young, Adult }` and `CanFly`; only Hatchling is implemented.
- Dice always visible: the breath's save and the dragon's bite go through the same dice tray and log as other attacks; checks helped by Sniff show "Advantage (Sniff)".
- Every window closes with Esc/B and has a ✕. Tests run muted. Art stays swappable through ArtRegistry ids.
- Old saves: a save with `EggHatched = true` and no dragon fields loads with a full-HP, unscared dragon named from `DragonName`, bond 0.
- No D&D, BG3 or Critical Role names in anything player-facing.

## Review Focus

- **Co-op leader hand-off:** P1 walks to another zone while P2 stays; the dragon follows whoever is nearer (same zone first) and never teleports back and forth every frame. Test: `DragonFollowsNearestPlayerWithoutFlicker` (Task 1).
- **Dragon down mid-fight:** at 0 HP it leaves the initiative order, isn't a "player down" for the wipe rule, and the fight still ends in victory when the players win. Test: `ScaredDragonLeavesFightAndPartyCanStillWin` (Task 2).
- **Two fights at once:** the dragon joins only one combat (the first one near it); a second fight elsewhere doesn't steal it. Test: `DragonJoinsOnlyOneFight` (Task 2).
- **Sniff spam:** Sniff can't be re-used by the other player before a short rest, and a long rest also resets it. Test: `SniffOncePerShortRestShared` (Task 4).
- **Old saves:** an `EggHatched` save without dragon fields gets a working dragon. Test: `OldHatchedSaveGetsAWorkingDragon` (Task 1).

---

### Task 1: The Dragonling creature and party ownership

**Files:**
- Create: `Assets/Scripts/Rules/Dragonling.cs`; `tools/RulesTests/DragonTests.cs`
- Modify: `Assets/Scripts/Rules/Content/MonstersDb.cs` (def `dragonling`), `Assets/Scripts/Game/Core/WorldState.cs`, `SaveSystem.cs`, `GameRoot.cs` (`SpawnDragon`, rest hooks, `Dragon` property), `Assets/Scripts/Game/World/Actors.cs` (CompanionAgent leader hand-off), `UI/Screens.cs` (short rest hook), `tools/RulesTests/Program.cs` (register)
- Test: `tools/RulesTests/DragonTests.cs`, `Assets/Tests/PlayMode/DragonTests.cs` (new)

**Interfaces:**
- Produces (rules): `public enum DragonStage { Hatchling, Wyrmling, Young, Adult }`; `public class Dragonling : Monster` with `Dragonling(int partyLevel)`, `void SetPartyLevel(int lvl)` (MaxHP = 5 + 5·lvl, HP keeps its damage taken), `DragonStage Stage`, `bool CanFly => Stage >= DragonStage.Young` (false now), `bool BreathReady`, `bool RollRecharge(System.Func<int> d6)` (sets BreathReady on 5–6, returns it), `int BreathDC => 8 + ProficiencyBonus + Mod(CON)` (= 11), `DiceExpr BreathDamage => 1d6`, `int BreathConeTiles => 1`. Team = Players.
- MonsterDef `dragonling`: AC 13, Scores {10, 14, 13, 6, 12, 12}, Speed 30 (fly hook unused), Darkvision 60, Bite +4 1d4+2 piercing, Traits {"Ember Breath (Recharge 5–6)"}, SpriteId `mon_dragonling`.
- Produces (game): `WorldState`: `int DragonHP = -1` (-1 = full), `bool DragonScared`, `int DragonBond`, `bool DragonSniffUsed`, `HashSet<string> LitBraziers`; matching save fields. `GameRoot.Dragon` (the `Dragonling`, null before hatching), `GameRoot.DragonAgent` (the `CompanionAgent`), `GameRoot.PartyLevel` (max level of ready players), `GameRoot.OnPartyRest(bool longRest)` (dragon to full HP, `DragonScared = false`, `DragonSniffUsed = false`, breath ready).

- [ ] **Step 1: RulesTests** `DragonlingStatsScaleWithPartyLevel` (L1: AC 13, MaxHP 10, bite +4 1d4+2; L4: MaxHP 25), `RechargeOnlyOnFiveOrSix` (d6 = 4 → not ready; 5 → ready), `DragonlingCanNotFlyAsHatchling`, `DamageTakenSurvivesLevelChange` (take 3 at L1, SetPartyLevel(2) → HP 12/15).
- [ ] **Step 2:** `cd tools/RulesTests && dotnet run` → those fail (no type).
- [ ] **Step 3:** Implement `Dragonling` and the def. Register the tests in Program.cs.
- [ ] **Step 4:** `dotnet run` → all pass (526 + new).
- [ ] **Step 5: Play-mode tests** in `DragonTests.cs`: `HatchedDragonIsAPartyCreature` (HatchEgg + name → `g.Dragon != null`, `Team.Players`, `DragonAgent.Actor.Creature == g.Dragon`); `DragonFollowsNearestPlayerWithoutFlicker` (two players, P2 warps to another zone, P1 stays near the dragon: across 60 frames the leader is P1 and the agent's zone never changes; move P1 away to another zone, P2 is now nearer-in-zone: leader switches once); `RestRestoresTheDragon` (DragonHP 2, scared, sniff used → `OnPartyRest(false)` → full, unscared, sniff ready); `OldHatchedSaveGetsAWorkingDragon` (SaveData with EggHatched and no dragon fields → load → `g.Dragon.HP == g.Dragon.MaxHP`, bond 0).
- [ ] **Step 6:** Implement. The leader hand-off rule: same zone beats other zones, then distance; switch only when the new candidate is at least 3 tiles nearer than the current leader (hysteresis). Short rest (Screens.cs `ShortRest`) and `DoLongRest` call `OnPartyRest`. `DragonHP` is written back on save and on combat end.
- [ ] **Step 7:** `.superpowers/unity-test.sh DragonTests` → pass; `SmokeTests.HatchTheEggPeacefully` still passes.
- [ ] **Step 8:** Commit: `Dragon: Dragonling creature, party ownership, persistent state`.

### Task 2: The dragon in combat

**Files:**
- Modify: `Assets/Scripts/Game/Core/GameRoot.cs` (`StartGroupCombat` adds the dragon), `Assets/Scripts/Game/Combat/CombatInstance.cs` (`Unit.Ctrl` for the dragon, `JoinLate`, `Finish` writes HP back, scared retreat), `CombatTurns.cs` (`PlayerTurn` on `Creature`, `BuildDragonActions`, `EmberBreath`, AI breath), `UI/PlayerHud.cs` (party frame shows the dragon with "(scared)" when retreated)
- Test: `Assets/Tests/PlayMode/DragonTests.cs`

**Interfaces:**
- Consumes: Task 1 `GameRoot.Dragon`, `DragonAgent`, `Dragonling.BreathReady/RollRecharge/BreathDC/BreathDamage/BreathConeTiles`, `WorldState.DragonScared`.
- Produces: `CombatInstance.DragonUnit` (or null); `IEnumerator EmberBreath(Unit me, Vector2Int tile)`; dragon actions named exactly `Move`, `Attack (Bite)`, `Ember Breath`, `Dodge`, `Auto`, `End Turn` (Ember Breath disabled with Why "Recharging (5–6 on a d6 each turn)." when not ready).

- [ ] **Step 1: Tests**: `DragonJoinsFightNearAPlayer` (dragon within 12 tiles of a joining player and not scared → a unit with `Comp.IsDragon`); `ScaredDragonSitsOut` (DragonScared → not added); `EitherPlayerCommandsTheDragon` (co-op, only P2 in the fight → `DragonUnit.Ctrl == P2`); `EmberBreathRollsASaveAndRecharges` (seeded dice: breath hits an adjacent wolf, a DEX save is logged, BreathReady false; at the dragon's next turn start a d6 is rolled and logged as "Recharge"); `ScaredDragonLeavesFightAndPartyCanStillWin` (dragon at 0 HP → log "retreats, scared", `DragonScared`, not in `Enc.Order`, the fight continues; killing the enemies ends in victory, the dragon's actor reappears at the leader's side after combat); `DragonJoinsOnlyOneFight`.
- [ ] **Step 2:** Run → fail.
- [ ] **Step 3:** Implement. `Unit.Ctrl` for the dragon: its `Comp.Manual` leader if that player is in this fight, else the first `Humans` player. `PlayerTurn` takes `Creature` (`pc` becomes `u.C`; the "Move" attack hint uses `u.C.GetAttacks()[0]`). `BuildActions` returns `BuildDragonActions(u)` when `u.C is Dragonling`. Recharge rolls at the dragon's turn start (`Enc.StartTurn` log line), not when the turn is skipped. AI: breathe if ready and ≥1 foe in the cone from an adjacent tile, otherwise bite like a melee monster. At 0 HP: `HP = 0`, mark scared, remove from order, hide the actor until combat ends; no death saves, no "ally down" options target it. XP: the dragon is not a player and earns none (it already doesn't count toward party size).
- [ ] **Step 4:** `.superpowers/unity-test.sh DragonTests` → pass; also `CoopTests` and `ClassPlayTests` (combat regression).
- [ ] **Step 5:** Commit: `Dragon: fights alongside, Ember Breath, retreats scared`.

### Task 3: Pet interactions, emotes and bond

**Files:**
- Create: `Assets/Scripts/Game/World/DragonPet.cs` (menu actions, fetch, nap, emotes)
- Modify: `Assets/Scripts/Game/World/Actors.cs` (`CompanionAgent.Interact` for the dragon opens the menu), `Assets/Editor/ArtMap.txt` (emotes from `Ui/Emote`: `fx_emote_heart`, `fx_emote_spark`, `fx_emote_sleep`, `fx_emote_happy`; a `obj_stick` prop), `Assets/Scripts/Rules/Content/ItemsDb.cs` (`dragon_treat`, "Smoked pepper jerky", 5 cp, Pip and Brannoc sell it), `UI/Screens.cs` (sheet line: name, HP, bond hearts), `GameRoot.cs` (bond on victory)
- Test: `Assets/Tests/PlayMode/DragonTests.cs`

**Interfaces:**
- Consumes: Task 1 `WorldState.DragonBond`, `GameRoot.DragonAgent`.
- Produces: `DragonPet.Emote(string kind)` (`heart|spark|sleep|happy`, a sprite 0.6 tiles above for 1.5 s); `DragonPet.AddBond(PlayerSlot p, int amount, string reason)` (clamped 0–100, a toast at each new tier); `int DragonPet.BondTier => DragonBond / 20` (0–5, shown as ♥/♡ on the sheet).
- Menu (`ChoiceScreen` titled with the dragon's name): **Pet** (+2 bond, once per in-game hour per player, random line from ≥6, heart emote), **Play fetch** (throws a stick 3–5 tiles ahead on a free tile; the dragon runs, picks it up, returns; +3, once per hour), **Give a treat** (needs `dragon_treat`; consumes one; +5; spark emote and a happy line), **Sniff around** (Task 4), **Light that** (Task 4, only listed near an unlit brazier), **In a fight: let it act on its own / I'll give orders** (toggles `Comp.Manual`), **Never mind**. Fights won with the dragon present: +1 bond.
- Nap: when no player has moved for 20 s of real time and the dragon is within 2 tiles of its leader, it plays the sleep emote every 3 s and its sprite stops animating; any player movement wakes it. During a long rest a narration line says it naps curled on someone's pack.

- [ ] **Step 1: Tests**: `PettingRaisesBondOncePerHour` (pet twice → +2 only; advance the clock 60 min → +2 more); `TreatNeedsAndConsumesATreat` (no treat → option disabled with a reason; one treat → consumed, +5); `FetchBringsTheStickBack` (after the fetch coroutine the dragon is back within 1.5 tiles of the leader and no stick object remains); `BondTierShowsOnTheSheet` (bond 45 → sheet text contains "♥♥♡♡♡").
- [ ] **Step 2:** Run → fail.
- [ ] **Step 3:** Implement; add the ArtMap lines and run `.superpowers/import-art.sh` (no new missing-art lines).
- [ ] **Step 4:** Run the DragonTests and `ArtTests` → pass.
- [ ] **Step 5:** Commit: `Dragon: pet, fetch, treats, naps, bond`.

### Task 4: Sniff and lighting braziers

**Files:**
- Modify: `Assets/Scripts/Game/World/DragonPet.cs` (Sniff, Light), `World/Interactables.cs` (`SecretInteractable`: sniffed sparkle + Advantage), `World/Zone.cs` (map char `j` = unlit brazier: solid, `obj_brazier_unlit`, becomes lit with a LightSource radius 4 when its key is in `LitBraziers`), `World/Maps.cs` (place 2–3 `j` in the mine, warrens and the watchtower's dark corners), `Assets/Editor/ArtMap.txt` (`obj_brazier_unlit`), `Core/PlayerInput.cs` (none expected)
- Test: `Assets/Tests/PlayMode/DragonTests.cs`

**Interfaces:**
- Consumes: Task 1 `DragonSniffUsed`, `LitBraziers`; Task 3 menu and `Emote`.
- Produces: `DragonPet.Sniff(PlayerSlot p)`: if `DragonSniffUsed` → "Already sniffed out; it needs a rest." Otherwise sets it, then every unfound `SecretInteractable` within 8 tiles in the zone (present ones only) gets `Sniffed = true`: a pulsing sparkle until found or zone exit, and its next check rolls with Advantage shown as "Advantage (Sniff)". If none are in range: "{name} sniffs everywhere, sneezes, and finds nothing here." Brazier key: `"{zoneId}:{x},{y}"`. `DragonPet.Light(PlayerSlot p, Vector2Int brazier)`: spark emote, flame puff, adds the key, the brazier lights up immediately and stays lit after reloads. The menu also offers **Light my torch** when the player has a `torch` and none is burning (calls `Avatar.LightTorch()`, so the torch item is still consumed as usual).

- [ ] **Step 1: Tests**: `SniffMarksNearbySecretsAndGivesAdvantage` (Whisperwood, stand within 8 tiles of a known secret → after Sniff it is `Sniffed`; a forced roll uses Advantage); `SniffOncePerShortRestShared` (P1 sniffs, P2 can't; `OnPartyRest(false)` → P2 can); `LitBrazierStaysLitAfterReload` (light one in the mine, save, load → LightSource on, sprite lit); `UnlitBraziersOnlyWhereDark` (every `j` is in a Dark zone).
- [ ] **Step 2:** Run → fail.
- [ ] **Step 3:** Implement, import art.
- [ ] **Step 4:** Run the DragonTests, `PlacesTests` and `SmokeTests.WorldDataIsConsistent` → pass.
- [ ] **Step 5:** Commit: `Dragon: Sniff and lighting braziers`.

### Task 5: The hatching scene, naming and reactions

**Files:**
- Create: `Assets/Scripts/Game/World/HatchScene.cs` (coroutine on GameRoot), `Assets/Scripts/Game/UI/NameEntryScreen.cs` (typed name, default offered)
- Modify: `GameRoot.HatchEgg`, `World/Interactables.cs` (`EggInteractable`), `Assets/Editor/ArtMap.txt` (`obj_egg_crack1`, `obj_egg_crack2` hand-painted from the `egg` pattern; `fx_hatch_flash` from `FX/Particle/Spark.png`), `Assets/Resources/Audio/music_hatch.ogg` (copy of the pack's "19 - Ascension.ogg"), `Core/Music.cs` (one-shot sting), `Dialogue/DialogueDb*.cs` (NPC comments), narrator lines
- Test: `Assets/Tests/PlayMode/DragonTests.cs`

**Interfaces:**
- Consumes: Tasks 1 and 3 (`SpawnDragon`, `Emote`).
- Produces: `IEnumerator HatchScene.Play(GameRoot g, PlayerSlot p, Vector3 eggPos)` (~8 s, skippable with Confirm/Esc after 1 s). The sequence: every player in the zone's input is held; the music ducks to the hatch sting; the egg wobbles three times with a glow pulse and camera shake (0.1 tiles); `crack1` → `crack2` → white flash → the shell (`item_egg_shard`) with the dragon on it; the dragon hops, sneezes sparks (spark emote), then heart emote toward the hatching player. Then the existing narration message, then `NameEntryScreen` ("Name your dragon", default `Cinder`; 1–16 letters, space, ' and -; a "Suggest another" button cycles the existing five names; the controller path uses the suggestion button + Confirm). XP and quest stage happen at the same point as now. In co-op the other player sees the scene if in the zone, and a toast "{name} hatched!" if not.
- Reactions: Captain Vale's turn-in line, Pip, Brannoc, Elowen, Perrin and Snik each get one dragon-aware greeting line when `EggHatched` (dialogue condition already used for flags); two narrator lines on entering Thistledown for the first time with the dragon.

- [ ] **Step 1: Tests**: `HatchSceneEndsInNamingAndSpawnsTheDragon` (run `HatchEgg`, skip the scene, type "Ziggy" + Enter in `NameEntryScreen` → `DragonName == "Ziggy"`, `g.Dragon != null`, stage Hatched, 100 XP each); `NameEntryRejectsEmptyAndKeepsDefault` (Enter on an empty field → "Cinder"); `HatchSceneCanBeSkipped` (Confirm after 1 s → naming within 2 frames); update `SmokeTests.HatchTheEggPeacefully` if its flow changes.
- [ ] **Step 2:** Run → fail.
- [ ] **Step 3:** Implement; import art; add the ScreenshotTests shot `20_hatching` (mid-scene) and `21_dragon_pet` (menu with emote).
- [ ] **Step 4:** Run the DragonTests, `SmokeTests`, `ScreenshotTests` → pass; look at the PNGs in `%TEMP%/embers_screenshots`.
- [ ] **Step 5:** Commit: `Dragon: hatching scene, typed name, NPC reactions`.

### Task 6: Acceptance

**Files:**
- Modify: `docs/PLAYTEST.md` (a dragon section: petting, fetch, treats, Sniff, braziers, combat orders), `docs/DESIGN.md` (dragon stats), `Assets/Tests/PlayMode/SmokeTests.cs` fuzz (a hatched-dragon fuzz run: 2 min of random input with the dragon present and a fight), `.superpowers/sdd/2026-10-30-phase6-dragon/progress.md`

- [ ] **Step 1:** Add `FuzzWithTheDragon` (DebugStart, hatch, random input incl. fights for 120 s → no exceptions logged).
- [ ] **Step 2:** Full play-mode suite in the background and `dotnet run` in tools/RulesTests → all green.
- [ ] **Step 3:** Review the screenshots; fix anything ugly.
- [ ] **Step 4:** Self-review the branch against the spec's dragon section; write rulings in the progress ledger.
- [ ] **Step 5:** Rebuild the exe and zip (`-executeMethod BuildTools.BuildWindows`). Commit docs. Merge to main (user's standing OK: "merge to main as soon as it's over"); do not push.
