# Act II Phase 6: The Volcano Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Act II's finale. The party goes through the Emberhorn's gate, crosses the burning tunnels, and faces **the Warden of the Emberhorn** in a real D&D boss fight:
- legendary actions, a telegraphed breath, a second phase and lair eruptions;
- an "illusion of peace" talk that pays something small and never avoids the fight;
- a lore-gated Insight check that reveals the Warden's weak spot;
- the half solution: the mountain calms, the blight eases, and the source is still out there.

**Architecture:** Three engine additions in the rules layer (engine-free, covered by RulesTests) and the combat layer:
1. **Legendary actions** (`MonsterDef.LegendaryActions`, `LegendaryActionDef`), used by the AI right after another creature's turn.
2. **A telegraphed breath** (`MonsterAbilityDef.Telegraph`): the monster spends its turn drawing breath, and the breath goes off at the start of its next turn. With `MonsterDef.WeakSpotFlag` set in the world, a hit during the draw chokes the breath.
3. **A second phase with lair eruptions** (`MonsterDef.PhaseTwoAt`, `PhaseTwoText`, `LairEruptions`).

Content follows the established style in `WorldDb.Volcano.cs` and `DialogueDb.Volcano.cs`.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh`; the full suite is ~25 min, so run it in the background).

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md ("The volcano finale", "Lore payoff", "illusion of peace", "Winning calms the volcano", "A seed for Act III", "moments where the party's dragon matters"; the anti-goals: no peaceful skip, no cheat-feeling talk, no hint the Queen is a child). Roadmap item 6.

## Global Constraints

- **No peaceful resolution** (the user's deliberate exception). The talk can't end the fight, and every talk path ends in combat. **Rolling well must pay something** (spec): every successful check in the talk grants one listed boon.
- **The weak-spot Insight check exists only if `World.LoreFound.Contains("emberhorn_warden")`** (the Restricted Section bestiary). Without the lore, the option isn't shown at all.
- **THE SECRET:** the Warden reads as a monster in its own right, burned out by the Queen's fire, and never hints at her age. Its last words reference "the door" (the shared dream) and nothing else. `LoreTests` stays green.
- Dice are always visible: every boss attack, save and eruption goes through the ticker/tray like existing monster rolls.
- Balance: the Warden is beatable by a typical level-9 party of four (two players + two companions; the dragon helps but isn't counted) in 5–9 rounds of at-will damage. It must still hit hard enough to feel like a boss. Guarded by a RulesTests sim.
- The party's dragon matters but doesn't carry: red dragons are immune to fire, so its wings shelter allies from the breath (Advantage on the save for allies within 5 ft of it).
- Co-op first-class. Either player can trigger the talk, and both players fight.
- Branch `act2/phase6-volcano`. Merge and push after the full suite passes.

## Names (working)

- **The Emberhorn:** the volcano. Inside:
  - **the Throat**, the burning tunnels
  - **the Ring Chamber**, with the Act III seed
  - **the Heart**, the caldera arena
- **The Warden of the Emberhorn:** an ancient red dragon, one of the Great Ones, burned grey by the fire it guards.

## The Warden (stat block, SRD Young Red Dragon base, tuned)

| Stat | Value |
|---|---|
| Size and sprite | Huge in the fiction. Drawn at `Scale = 2.2` (sprite only); occupies one tile (**[Claude's call]** see the Task 1 ruling). |
| AC / HP | AC 18. HP 220 (21d12+84 avg 220 → use `DiceExpr.Parse("20d12+90")`) |
| Speed | 40, fly 80 (it doesn't fly in the arena) |
| Scores | STR 23 DEX 10 CON 21 INT 14 WIS 11 CHA 19 |
| CR / XP | 10 / 5900 |
| Immunities | fire |
| Multiattack | Rend, Rend, Rend. Rend: +10, 2d6+6 slashing, reach 10 ft. |
| Grey Breath | 30 ft cone, 10d6 fire, DEX DC 17, Recharge 5–6, `Telegraph = true` |
| Legendary actions | 2 per round: **Tail Sweep** (cost 1: +10 vs one creature within 15 ft, 2d8+6 bludgeoning); **Searing Glare** (cost 1: one creature within 60 ft, DEX DC 15, 3d6 fire) |
| Phase two at 50% HP | `PhaseTwoText`: "The grey fire takes it back completely. The Warden's eyes go dull as ash, and the mountain answers: the floor splits and the lava rises." From then on: `LairEruptions = 3` per round, and the breath recharges on 4–6. |
| Weak spot | `WeakSpotFlag = "warden_weakness"`: a hit while it draws breath chokes it. The breath is cancelled, it takes 4d10 extra force damage, and it loses its next legendary action. |

## Review Focus

- **The fight ends while a breath is drawn, or the Warden is downed by a legendary-action trigger:** no breath goes off from a dead Warden, and no legendary action is taken after it's down. The combat ends cleanly (victory → the half solution). Test: `NoBreathOrLegendaryFromTheFallen` (Task 1).
- **Party wipe at the Warden:** the existing wipe rule applies (wake at the temple). The Warden is back at full HP, the talk can be tried again (boons re-earned, not stacked), and the weak-spot flag persists (it's knowledge). Test: `WipeResetsTheWarden` (Task 4).
- **Save/load inside the Heart before the fight, and after victory:** before → the Warden is there and the talk is available. After → no Warden, the blight eased exactly once, and Answers done. Test: `VictorySurvivesSaveLoad` (Task 4).
- **Co-op: P2 elsewhere when P1 starts the talk:** P2 joins the fight late (the existing JoinLate path) and isn't stuck in a dialogue. Test: `CoopPartnerJoinsTheWardenFight` (Task 4).
- **Lair eruptions near the edges or on lava:** an eruption never targets a non-walkable tile, never spawns a fire zone outside the map, and the dragon (fire-immune) takes no damage from it. Test: `EruptionsOnlyOnFloor` (Task 2).

---

### Task 1: Legendary actions, the telegraphed breath, the weak spot, and big sprites

**Files:**
- `Assets/Scripts/Rules/Monster.cs`: `LegendaryActionDef`; `MonsterDef.LegendaryActions`, `LegendaryPerRound`, `WeakSpotFlag`, `Scale`; `MonsterAbilityDef.Telegraph`; on `Monster`: `LegendaryLeft`, `ResetLegendary()`, `Charging`
- `Game/Combat/CombatInstance.cs` (Run loop hooks)
- `Game/Combat/CombatTurns.cs` (AI: legendary use, telegraph, choke)
- `Game/World/Actors.cs` (EnemyAgent scale)
- Tests: `tools/RulesTests/BossTests.cs` (new, registered in Program), `Assets/Tests/PlayMode/VolcanoTests.cs` (new)

**Interfaces (produces):**
```csharp
public enum LegendaryKind { Attack, SaveBurst }
public class LegendaryActionDef { public string Name; public int Cost = 1; public LegendaryKind Kind; public string Attack; /* Attack: an Attacks name */ public int RangeFt = 5; public DiceExpr Dice; public DamageType DamageType; public Ability SaveAbility; public int SaveDC; }
public class MonsterDef { public List<LegendaryActionDef> LegendaryActions; public int LegendaryPerRound; public string WeakSpotFlag; public float Scale = 1f; public double PhaseTwoAt; public string PhaseTwoText; public int LairEruptions; public int PhaseTwoRechargeMin; }
public class MonsterAbilityDef { public bool Telegraph; }
public class Monster { public int LegendaryLeft; public void ResetLegendary(); public bool SpendLegendary(int cost); public MonsterAbilityDef Charging; public bool PhaseTwo; }
```
- **Run loop:**
  - At the start of a legendary monster's own turn: `ResetLegendary()`. If `Charging != null`, the breath goes off first, then the turn continues without its action.
  - After every *other* creature's turn ends (before `Enc.Advance`): each active legendary monster may spend one action via `CombatInstance.LegendaryOpportunity(Unit boss)`. AI choice: Tail Sweep if a foe is within reach, else Searing Glare on the lowest-HP foe in range.
- **Telegraph:**
  - When `TryMonsterAbility` picks a `Telegraph` ability, it sets `Charging = ab`, spends the action, logs "{name} rears back, its throat glowing. (It will breathe at the start of its next turn: spread out!)", and shows a red tint on the boss sprite.
- **Weak spot:** in `AttackAction`/`AiAttack` damage resolution, if the target is a Monster with `Charging != null`, the attack hit, and `World.Flag(WeakSpotFlag)`:
  - the breath is cancelled (`Charging = null`; the ability stays spent);
  - +4d10 force damage, shown;
  - `LegendaryLeft = 0`;
  - log "{name} chokes on its own fire!".
- **Dragon's wings:** in the breath's save loop, a target within 5 ft of an active fire-immune ally Dragonling gets Advantage (ticker: "sheltered by {dragon}'s wings").
- **Sprite scale:** `EnemyAgent` applies `Def.Scale` to its sprite transform. The footprint stays one tile. **[Claude's call]** True multi-tile footprints would touch every range and occupancy check in a 2000-line combat file; a stationary-feeling, huge-drawn boss gets the look without that risk.

- [ ] **Step 1: Write failing tests:**
  - RulesTests `BossTests`:
    - `LegendaryBudgetResetsAndSpends`: 2 per round; spending 1 twice leaves 0; a third returns false; Reset → 2.
    - `PhaseTwoFlips`: a monster with `PhaseTwoAt = 0.5`. Damage to 49% sets `PhaseTwo` once and returns the text once.
  - Play-mode `VolcanoTests` (a test arena: a test group with a test monster that has legendary actions and a telegraph breath, in the Lantern Road's open field):
    - `LegendaryActionAfterAHerosTurn`: after the hero's turn ends, the log contains the legendary action's name, and `LegendaryLeft` dropped by its cost.
    - `TelegraphThenBreathe`: the boss's first turn with the breath charged sets `Charging` and logs "rears back". Its next turn starts with the breath (the log contains "save DC").
    - `WeakSpotChokes`: with the flag set and `Charging` set, a hit cancels the breath, logs "chokes", and deals the 4d10 (the damage log line is present). Without the flag: no choke.
    - `DragonWingsShelter`: an ally adjacent to the party dragon gets "sheltered" in the breath's save line.
    - `NoBreathOrLegendaryFromTheFallen`: zero the boss's HP mid-charge → the combat ends, and no "save DC" or legendary line follows.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** RulesTests + `VolcanoTests` + `DragonTests` + `SmokeTests.FightInTheMineWithEachClass` → PASS.
- [ ] **Step 5:** Commit `"Engine: legendary actions, telegraphed breath and the weak spot, phase two, big sprites"`.

### Task 2: Phase two and lair eruptions

**Files:** `CombatInstance.cs` / `CombatTurns.cs` (round-start hook), `Rules/Monster.cs` (PhaseTwo), `CombatZones.cs` (a fire patch that isn't a spell: `SpellZone` with a synthetic `SpellDef` "Eruption", damage 2d10 fire, ending at round end), tests in VolcanoTests + BossTests.

- When the boss's HP first drops to `PhaseTwoAt × MaxHP` or below:
  - narrate `PhaseTwoText` to every human;
  - set `PhaseTwo`;
  - breath `RechargeMin` becomes `PhaseTwoRechargeMin`.
- **Lair eruptions** at the start of each round while any monster in the fight has `PhaseTwo && LairEruptions > 0`:
  - pick N walkable, in-bounds tiles near heroes;
  - each creature on one makes a DEX DC 15 save: 2d10 fire, half on success, visible on the ticker;
  - a one-round burning patch stays on the tile (the existing zone drawing).
  - Fire-immune creatures take no damage (the existing immunity path).

- [ ] **Step 1: Write failing tests:**
  - `PhaseTwoAtHalf` (play-mode): damage the test boss to 50% → narration once, `PhaseTwo` set, `RechargeMin` lowered.
  - `EruptionsOnlyOnFloor`: 50 seeded rounds of eruption picks in the test arena → every tile is walkable and in bounds. The party dragon standing on one takes 0.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + Task 1 tests.
- [ ] **Step 5:** Commit `"Engine: boss phase two and lair eruptions"`.

### Task 3: The Emberhorn: the Throat, the Ring Chamber, the Heart, and the Warden's stat block

**Files:** `Maps.cs` (`EmberhornThroat` ~40×24 Dark; `EmberhornHeart` ~30×22, lava-lit), `World/WorldDb.Volcano.cs` (new), `Rules/Content/MonstersDb.cs` (`warden`), `LoreDb.cs`, `Zone.cs` (lava char `9`: solid, glowing orange, an animated tint, a `LightSource`), ArtMap, tests.

**Interfaces (produces):** zones `emberhorn_throat`, `emberhorn_heart`; monster `warden`; group `warden` (Style Talk, `ParleyDialogue = "warden_parley"`, Unique; peaceful resolution is impossible because the parley always ends in combat); lore `iron_ring` (the Act III seed); flag `emberhorn_gate` (replaces the gate warp's MinAct 3: Answers 8's `StageFlags[8] = "emberhorn_gate"`).

- **The gate warp** on the road: `RequiresFlag = "emberhorn_gate"` (Answers 8), no MinAct. It leads to the Throat.
- **The Throat** (Dark):
  - lava channels `9`, narrow stone bridges, and heat shimmer (the existing `WardShimmer` on lava edges);
  - two fights: `throat_salamanders` (2 `salamander`, Talk/beasts style: TalkDC 16) and `throat_elemental` (1 `fire_elemental`, Beasts, TalkDC 17, `TalkSkill = Arcana`);
  - one secret: an Oathbound's last cache, Perception 15 → 2 `potion_superior_healing` if that item exists, else 3 greater healing.
- **The Ring Chamber** (a side room off the Throat): lore spot `iron_ring`.
  - "Set into the rock, an iron ring as wide as a cartwheel, old beyond reckoning. The chain that hung from it is long gone, melted away link by link. Someone, once, wanted something to stay here."
  - Neutral: no culprit, no age.
- **The Heart:**
  - a round caldera with lava `9` around the edge and in two pools, broken pillars `P` as cover, and an obsidian floor;
  - the Warden coiled at the centre (`EnemySpawn("warden", 15, 8)`);
  - the ally from the Last Camp stands at the entrance, if your side's banner is set. Talking to them gives the **ally's boon**, once:

| Ally | Boon |
|---|---|
| Holloway | each player +15 temp HP ("I'll hold the tunnel.") |
| Perrin | Ashby's ward: each player resists fire for this fight (`Creature.Resistances.Add(Fire)` until the fight ends; flag `ward_active`) |
| Thimble | each player gains Heroic Inspiration |
| Odo | the Warden starts the fight marked: the first attack against it has Advantage (flag `warden_marked`) |

- [ ] **Step 1: Write failing tests:**
  - `GateOpensAtAnswersEight`: the gate warp is closed at Answers 7 and open at 8. It leads to the Throat, and the Throat links to the Heart.
  - `EmberhornIsReachable`: every secret, lore spot, group and warp in both zones is reachable.
  - `WardenStatBlock`: `MonstersDb.Get("warden")`: AC 18, Rend ×3, a Telegraph breath, 2 legendary actions, PhaseTwoAt 0.5, `WeakSpotFlag "warden_weakness"`, fire immunity.
  - `AllyBoonOncePerSide`: for each side, the ally at the Heart gives their boon once.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. Lava art: a tinted water tile (`tint tile_lava tile_water 20 1.4 1.3`) or a hand-painted pattern. Warden sprite: `still mon_warden_src "Actor/Monster/Dragon/SpriteSheet.png"` tinted grey-red (different from the Ashmaw's tint). Music: `music_heart` = "24 - Final Area.ogg"; `music_throat` = "40 - Crypt.ogg".
- [ ] **Step 4:** PASS + `WorldDataIsConsistent` + `LoreTests`.
- [ ] **Step 5:** Commit `"Act II: the Emberhorn (the Throat, the Ring Chamber, the Heart) and the Warden"`.

### Task 4: The illusion of peace, the weak spot, the fight, and the half solution

**Files:** `Dialogue/DialogueDb.Volcano.cs` (`warden_parley`), `Core/GameRoot.cs` (victory hook for the `warden` group), `QuestDb.cs` (Answers 9), `Blight.cs` (use `Ease`), `LoreDb.cs`, tests in VolcanoTests.

**The talk** (opens when the Warden notices you; real dialogue, real dice):
- It speaks: vast, slow, half-burned-out. "Little flames. Go home. The door is shut. I am the lock." It recognises the hatchling and hesitates.
- Options (each available once; each success sets its boon flag; every branch eventually reaches "The fire takes it back"):

| Option | Check | On success |
|---|---|---|
| Persuade it to rest | Persuasion DC 18 | `warden_hesitated`: the party gets a surprise round (enemies surprised) |
| Show it the hatchling | Animal Handling DC 15, dragon present | `warden_spares_dragon`: the Warden never targets the party dragon in phase one |
| (Insight) Watch its throat as it speaks | DC 14 | Shown only if `emberhorn_warden` lore is found. Recalls the bestiary: sets `warden_weakness` ("when it draws breath, strike the glowing throat") |
| Fight | none | straight to combat |

- **Failure** on a roll gives nothing, and the other options stay available. After any two checks, or choosing Fight, the next node is the turn: "For a moment it seems to work. Then the grey fire rises in its eyes, and the Warden of the Emberhorn remembers what it is for." → `StartGroupCombat("warden", p, enemiesSurprised: warden_hesitated)`.
- **Victory** (the group resolved by defeat): Answers 9; `Blight.Ease()` once (flag `warden_fallen`); last-words narration; lore `warden_last_words`.
  - Last words: "It lays its great head down on the stone. 'Hold... the door,' it breathes, and the light goes out of it, and the mountain goes quiet for the first time in a year."
  - Answers stage 9: "The Warden of the Emberhorn is at rest, and the mountain sleeps. The ash will thin. But something beneath the Wyrmback is still waking." StageXp[9] = 3000.
  - The End of Act II card is Phase 7.
- **Wipe:** the existing party-wipe flow. The Warden's group isn't marked defeated, so it's back at full HP. Boon flags `warden_hesitated` and `warden_spares_dragon` are cleared so the talk can earn them again. `warden_weakness` stays.

- [ ] **Step 1: Write failing tests:**
  - `TalkAlwaysEndsInAFight`: every option path in `warden_parley` reaches a node whose OnEnter starts combat. No path pacifies the group.
  - `InsightOnlyWithTheBestiary`: the Insight option is hidden without the lore and shown with it. Success sets `warden_weakness`.
  - `RollingWellPays`: Persuasion success → the combat starts with enemies surprised. Animal Handling success → `warden_spares_dragon`.
  - `VictoryEasesTheBlightOnce`: win the fight (the WinFight pattern) → Answers 9, the blight is eased (stage 3 → 2), lore recorded. Save/load → still 2, no second ease.
  - `WipeResetsTheWarden` and `CoopPartnerJoinsTheWardenFight` (Review Focus).
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `LoreTests` + `BlightStoryTests`.
- [ ] **Step 5:** Commit `"Act II: the Warden of the Emberhorn (the illusion of peace, the weak spot, the half solution)"`.

### Task 5: Balance, acceptance, docs, release

- [ ] **RulesTests `WardenIsABossNotAWall`:**
  - Sum the at-will DPR of every 4-class level-9 party drawn from (fighter, rogue, cleric, wizard, ranger, paladin, bard, druid) vs AC 18.
  - Median rounds to drop 220 HP: between 4 and 9. Slowest: ≤ 12.
  - The Warden's expected damage per round (Rend ×3 at +10 vs AC 17, plus 1 legendary Tail Sweep) is ≥ 25.
- [ ] **Play-mode `VolcanoGoldenPath`:**
  - Start at Act II → commit → banner → Answers 8 (via hooks) → the gate → the Throat (pacify both groups) → the ring lore → the Heart.
  - The ally boon; the talk with the bestiary lore found (Insight success → weakness).
  - The fight via WinFight → Answers 9, blight eased. Save/load holds.
- [ ] **Screenshots:** the Throat, the Ring Chamber, the Heart with the Warden (before), the Warden mid-breath telegraph (red glow), phase two eruptions.
- [ ] **Docs:**
  - PLAYTEST.md: "New: the Emberhorn (Act II finale)", with a checklist.
  - DESIGN.md: legendary actions, telegraph, phases, lair, the Warden table, boons.
  - LORE_PITCH.md: names, the Warden's last words, the iron ring.
- [ ] **Release:** RulesTests + the full suite green; build; self-review; fix pass; merge; push.
