# Act II Phase 3: The Road and the Academy Grounds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Act II gets its big new place. The Lantern Road runs from Miller's Ford to the Starfall Academy, a walled campus that feels **powerful and mysterious** (user, 2026-10-31: "like theres something at play and that there is powerful magic there"). Its grounds, library, restricted section, hidden Sealing Hall and Ashby's tower are explorable and full of secrets. High Magister Ashby can be heard out, three professors have side quests, and the main quest "Answers" starts with Act II and leads here.

**Architecture:** Same data-driven style as Act I's Phase 5. New zones are `ZoneDef` + `Maps` arrays, built in a new `WorldDb.Academy.cs`. Dialogue lives in `DialogueDb.Academy.cs`. Quests go in `QuestDb`, lore in `LoreDb`, monsters in `MonstersDb`. Small engine additions:
1. Gated warps (`WarpDef.MinAct`, `RequiresFlag`, `ForbidFlag`, `ClosedText`).
2. New map chars for the magic set dressing, with two tiny components: `Floater` (bob + glow) and `WardShimmer` (alpha pulse).
3. `ZoneDef.DragonStays` (the dragon refuses to follow into the Sealing Hall).
4. `SecretDef.Hours` (a secret present only in an hour window: the thirteenth bell).

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; full suite ~12 min, run it in the background, muted). Art via `Assets/Editor/ArtMap.txt` (pack tiles, `tint`, or hand-drawn 16×16 `pattern`/`paint`) + `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md (Goals 1–4, the secret, the four forces). Roadmap item 3 in docs/superpowers/plans/2026-10-31-act2-phase1-levels.md.

## Global Constraints

- **THE SECRET:** nothing may hint that the Ashen Queen is a child, or that the Crown is the culprit. `LoreTests.NoActTwoTextHintsAtTheSecret` must stay green. Every new line naming the Queen/Ashen avoids the banned word starts, including "son" (so no "song") and "small".
- **Act I stays light** (user): the Lantern Road is closed in Act I, so nothing here is reachable before Act II.
- **The Academy feels powerful and mysterious** (user). Every Academy zone gets at least one visible-magic set piece and one mystery.
- Every force is right about something. Ashby is brilliant, beloved, scared and half right: never a cartoon villain. Crown soldiers are ordinary, decent and grey.
- Tone: bright fantasy, PG-13, Mercer + Mulligan. Unsettling at most, never horror.
- Dice are always visible (`p.Hud.Dice.RollD20` / dialogue `.Roll`). Peaceful options pay the same XP and loot as fighting.
- Data-driven content. Art stays swappable through ArtRegistry ids. Every new id gets an ArtMap line or a `pattern`/`paint`.
- Every window closes with Esc/B and has a ✕. Tests run muted. Co-op: either player can do anything, and world flags are shared.
- Buoy stays in Thistledown. Names are working names, approved in bulk by the user ("im good with all names"); list them in LORE_PITCH.md.
- Branch `act2/phase3-academy`. Merge to main and push after the full suite passes.

## Names (working)

- **The Lantern Road:** Miller's Ford's east edge to the Academy gate. It has a waystation shrine (the Halfway Lamp) and a Crown checkpoint.
- **The Starfall Academy:** a walled campus. Key places:
  - **the Grand Orrery** (courtyard)
  - **the Starwell** (a well of light where the Academy says a star once fell)
  - **the Glasshouse**
  - **the Observatory**
  - **the Thirteen Bells** (bell tower)
- **The Library of Quiet Stars**, with the **Endless Stacks** (a looping illusion) and the **Restricted Section**. Below it: **the Sealing Hall**.
- **The Orrery Tower:** Ashby's tower. It is taller inside than out.
- **People:**
  - **High Magister Corvane Ashby**
  - **Archivist Mirelle Thorne** (the library)
  - **Professor Bramwell Oakes** (botany, the Glasshouse)
  - **Magister Isolde Varrow** (astronomy, the Observatory)
  - **Sergeant Dace Holloway** (the Crown garrison at the gate)
  - students **Pell** and **Juniper**
  - **Perrin** (returns as a familiar face and guide)
- **The volcano:** **the Emberhorn**. Its dragon is the **Warden of the Emberhorn**; the boss itself waits for Phase 6. Here we only place the lore book on its weakness.

## Review Focus

- **Act I players at the east edge of the Ford:** stepping on the closed road narrates ClosedText and doesn't move the player, companions or dragon. Nothing pops twice while standing on it. Test: `RoadIsClosedInActOne` (Task 1).
- **The dragon left upstairs at the Sealing Hall:** leave by any route, load a save made inside the hall, or have P2 elsewhere, and the dragon rejoins the nearest player in a zone it will enter. It never gets stuck in the hall or teleports into it. Test: `DragonWaitsOutsideTheSealingHallAndRejoins` (Task 4).
- **The Endless Stacks after seeing through it:** once `stacks_unlooped` is set, the loop warp never fires again (for either player, after load too). Before that, the loop always returns you to the corridor start, never into a wall. Test: `StacksLoopUntilSeenThrough` (Task 4).
- **Restricted Section access by every route:** the Archivist's pass, picking the ward (Arcana), or the lock (Sleight of Hand). The guardians stand down for the pass and fight otherwise; either way pays full XP once. Test: `RestrictedSectionOpensEveryWay` (Task 4).
- **Old Act II saves (Phase 2) and Start at Act II:** "Answers" starts on BeginActTwo, ApplyActTwoStart, and on loading an Act II save that lacks it. It never restarts once done. Test: `AnswersStartsWithActTwoEveryWay` (Task 2).

---

### Task 1: Engine bits: gated warps, magic set-dressing chars, DragonStays, hour-window secrets

**Files:**
- `Assets/Scripts/Game/World/WorldDb.cs` (WarpDef, ZoneDef, SecretDef)
- `World/Actors.cs` (player warp step; companion follow; the dragon's MaxHP check uses `Dragonling.MaxHPFor(level, Stage)`)
- `Core/GameRoot.cs` (`Warp` skips the dragon into DragonStays zones)
- `World/Zone.cs` (BuildCell chars; secrets' hour window)
- `World/Interactables.cs` (SecretInteractable presence)
- `World/Floater.cs` (new: `Floater`, `WardShimmer`)
- `World/Maps.cs` (legend)
- `Assets/Editor/ArtMap.txt` (hand-drawn props)
- Tests: `Assets/Tests/PlayMode/AcademyTests.cs` (new; SetUp/TearDown as LoreTests)

**Interfaces (produces):**
```csharp
public class WarpDef { public int MinAct = 1; public string RequiresFlag, ForbidFlag, ClosedText; public bool Open(WorldState w); }
public class ZoneDef { public bool DragonStays; }               // the dragon won't follow into this zone
public class SecretDef { public int FromHour = -1, ToHour = -1; } // present only from FromHour to ToHour (wraps midnight), like NightOnly
```
- Player warp step: a closed warp (`!Open`) narrates `ClosedText` once per step-on (reuse `_lastWarp`) and doesn't move anyone. An open warp works as now.
- `GameRoot.Warp`: if the target zone has `DragonStays`, the dragon isn't placed. `CompanionAgent.Update`: if `IsDragon` and the leader's zone has `DragonStays`, it stays put (`Moving = false`) instead of `PlaceAt`. It shows one bark per entry via `DragonPet` ("{name} won't come down the stairs. Every scale is standing on end.").
- New map chars:

| Char | Becomes | Solid? | Art id / extra |
|---|---|---|---|
| `z` | bookshelf | solid | `obj_bookshelf` |
| `#` | hedge | solid | `obj_hedge` (tinted bush) |
| `+` | ward pillar | solid, glows | `obj_ward` + `WardShimmer` |
| `^` | floating stone | solid, glows | `obj_floating_stone` + `Floater` (bobs ±0.08 world units, 2.5 s period, random phase from position) |
| `&` | the Grand Orrery | solid, glows | `obj_orrery` (frames `_0` / `_1` alternate every 0.8 s) |
| `$` | the Starwell | solid, glows | `obj_starwell` + `Floater` on its light |
| `!` | telescope | solid | `obj_telescope` |
| `-` | desk | solid | `obj_desk` |
| `<` | rune floor | walkable | `tile_rune`, faint glow + `WardShimmer` |

  All glowing props are LightSource-lit at night.

- [ ] **Step 1: Write failing tests** in AcademyTests.cs:
  - `RoadIsClosedInActOne`: Act 1, add a test `WarpDef { MinAct = 2, ClosedText = "closed" }` on a Ford tile. Placing the player on it and ticking 5 frames leaves them in the Ford, and the narration log contains "closed" exactly once. Set `World.Act = 2`, step off and on: now in the target zone.
  - `FlagGatedWarps`: a warp with `ForbidFlag = "f"` works until `World.SetFlag("f")`, then narrates its ClosedText. `RequiresFlag` works the other way.
  - `AcademyCharsBuild`: a test ZoneDef with one row `"z#+^&$!-<."` builds without exceptions. `z # + ^ & $ ! -` are not walkable; `<` and `.` are. `^` has a `Floater`; `+` has a `WardShimmer`.
  - `HourWindowSecret`: a SecretDef with `FromHour = 0, ToHour = 1`. At 00:30 `Available` is true; at 02:00 false.
- [ ] **Step 2:** `.superpowers/unity-test.sh AcademyTests` → FAIL (compile).
- [ ] **Step 3:** Implement as above. Hand-draw the 16×16 patterns (bookshelf, ward, floating stone, orrery ×2, starwell, telescope, desk, rune tile) in ArtMap under "Act II Phase 3: the Academy". Palette: deep blues, violet, star-gold `ffd54a`, pale cyan glow. Run `.superpowers/import-art.sh`.
- [ ] **Step 4:** `AcademyTests` + `PlacesTests` + `DragonTests` → PASS.
- [ ] **Step 5:** Commit `"Act II engine: gated warps, academy set-dressing chars, the dragon's no-go zones, hour-window secrets"`.

### Task 2: The Lantern Road, and the main quest "Answers" begins

**Files:**
- `World/Maps.cs`: `LanternRoad`, about 60×30. Ford rows 14–15 open at cols 44–45.
- `World/WorldDb.Academy.cs` (new): `BuildLanternRoad()`, called from the WorldDb static ctor.
- `Quests/QuestDb.cs`; `Core/GameRoot.ActTwo.cs`; `Rules/Content/MonstersDb.cs`
- `Dialogue/DialogueDb.Academy.cs` (new, wired like `PlacesDialogue`); `DialogueDb.cs` (Rhoswen's Act II line)
- Tests: AcademyTests

**Interfaces (produces):** zone `lantern_road` ("The Lantern Road", outdoors, `Music = "music_road"`); the Ford ↔ road warps (MinAct 2); quest `answers`; NPC `holloway`; monster `ember_eyed_boar`.

**Ford:** warps (45,14)/(45,15) → the road's west edge. `ClosedText` for Act I: "The east road is washed out past the ford: a river of mud and a hand-painted sign, ROAD CLOSED TILL AUTUMN. (Act II)". Also update the Ford's east sign text.

**Road layout** (hand-shaped):
- A winding road with lantern posts `l` every few tiles, all cold by day.
- Grey fields of ash `d`.
- A waystation shrine (**the Halfway Lamp**): an altar `A` and an always-lit brazier `i`.
- A ruined farmstead.
- A Crown checkpoint: tents `u`, a barrier and Sergeant Holloway's post.
- A stream with a footbridge.
- The east end climbs to the Academy's outer gate, warping to `academy` (MinAct 2).

**Encounters:**

| Group | Members | Where | Peaceful option |
|---|---|---|---|
| `road_hounds` | 2 `cinder_hound` | in the ash fields | Beasts, TalkDC 15 |
| `road_boars` | 3 `ember_eyed_boar` | the farmstead | Beasts, TalkDC 14 |
| `road_ankheg` | 1 `ankheg` | near the stream | Beasts, TalkDC 16, ScareDC 13 |

`ember_eyed_boar` = SRD Giant Boar (AC 12, HP 42, Tusk +5 2d6+3, Charge text) with an ember-eyed look (tint of `mon_boar`).

**Secrets:**

| Id | Check | Reward / effect |
|---|---|---|
| `halfway_lamp` | Religion 12, Daily | HeroicInspiration. "You add a twig to the flame. It burns a little brighter for you." |
| `farm_cellar` | Investigation 14 | 2 `potion_greater_healing` + 30 gp |
| `ash_circle` | Nature 13 | Lore `ash_fields`, minAct 2: "The ash falls in rings, as if something far away breathed out and the wind carried it in circles." |
| `checkpoint_ledger` | Sleight of Hand 15 | Lore `checkpoint_orders`: plain orders to "count every traveller with an animal companion"; neutral, no culprit |

**Holloway** (the Crown checkpoint) is tired, decent and homesick: "We're here to keep the road safe. Mostly from the road." Insight 13: he doesn't know why they count dragons, and doesn't like it. His Crown faction quests come in Phase 4.

**Quest `answers`** ("Answers", giver `rhoswen`), stages:

| Stage | Journal text | Reached by |
|---|---|---|
| 1 | "Hearths are going cold down the valley. Captain Vale says if anyone has answers, it's the Starfall Academy, up the Lantern Road past Miller's Ford." | Act II starts |
| 2 | "You've reached the Starfall Academy. Find High Magister Ashby. They say he lives at the top of the Orrery Tower." | `ZoneEntered academy`, 1→2 |
| 3 | "Ashby's door is open. Go up and hear him out." | `ZoneEntered ashby_tower`, 2→3 |
| 4 | "Ashby has given you a great deal to think about. (The road ahead opens in a later update.)" | `FlagSet ashby_heard`, 3→4 |

- StageXp: {2: 400, 4: 600}.
- **Starting it:** `BeginActTwo` and `ApplyActTwoStart` call `Quests.Start("answers", p)`. The load path (after `ApplyActCap`) starts it if `World.Act >= 2` and its stage is 0.
- Rhoswen gets an Act II line pointing up the road (show only when `World.Act >= 2`).

- [ ] **Step 1: Write failing tests:**
  - `LanternRoadLinksFordAndAcademy`: Act 2. Take the warp at Ford (45,14) → `lantern_road`. From the spawn, `CanReachBeside` every secret, Holloway's post and the academy warp tile. Warp back to the Ford.
  - `AnswersStartsWithActTwoEveryWay`:
    - `BeginActTwo` → stage 1.
    - A fresh `ApplyActTwoStart` → stage 1.
    - A save with `answers` stripped and Act 2 → after load, stage 1.
    - A save with stage 4 → after load, still 4, with no re-pay.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. Pick pack art for the boar tint and road props; set `audio music_road` to a pack track that isn't used yet.
- [ ] **Step 4:** `AcademyTests` + `ActTwoTransitionTests` + `SmokeTests.WorldDataIsConsistent` → PASS.
- [ ] **Step 5:** Commit `"Act II: the Lantern Road, the Crown checkpoint, and the main quest Answers"`.

### Task 3: The Academy grounds: powerful, mysterious, alive

**Files:** `Maps.cs` (`Academy`, about 56×44), `WorldDb.Academy.cs` (`BuildAcademy()`, NPCs and schedules), `DialogueDb.Academy.cs`, `LoreDb.cs`, `DragonDb.cs` (Base opinions for the new NPCs), tests.

**Interfaces (produces):**
- zone `academy` ("The Starfall Academy", `BlightWeight = 0.15`: the wards hold the ash back)
- warps: the road gate; doors to `library`, `ashby_tower`, `glasshouse` (interior, about 20×14) and `observatory` (interior, about 16×14)
- NPCs `perrin` (Act II schedule here), `pell`, `juniper`, `oakes`, `varrow`
- lore ids `great_ones_statue`, `primer_ashen_age`, `thirteenth_bell`, `starwell`

**Layout** (hand-shaped; landmarks you can name from across the map):
- An outer wall ringed with ward pillars `+`, and a gatehouse with Holloway's two Crown soldiers (flavor NPCs, same Look).
- A great courtyard with **the Grand Orrery** `&` at its heart. Floating stones `^` drift around it in a broken ring.
- **The Starwell** `$` in a sunken garden.
- **The Thirteen Bells** tower (roof block + door; no interior; a bell sign).
- A hedge garden `#` with a small maze, the Glasshouse (gold roof), the Observatory dome (blue roof, telescope `!` on the terrace) and the Library (biggest building).
- **The Orrery Tower**: a tall narrow roof block in the north-east.
- Lantern-lined cobble paths, a refectory lawn with benches and students, and a pond with geese.

**Visible magic (the user's ask):**
- On first entry, narrate the place in full (EnterNarration): "Stone floats here. Lanterns hang on nothing. Somewhere a bell is ringing, and you can't find which one."
- Floating stones and lanterns bob; wards shimmer; the orrery turns.
- At night, the orrery and Starwell glow.
- The dragon's first-entry bark: "{name} sneezes a puff of sparks at the walls. The wards hum back, and it doesn't like that one bit."

**Mysteries:**

| Id | When / check | Result |
|---|---|---|
| `thirteenth_bell` | Secret at the bell tower, `FromHour 0, ToHour 1`, Perception 14 | When the hour starts, every player in the zone gets the narration "The bells ring midnight. Twelve... and then, a breath late, a thirteenth." Lore `thirteenth_bell`: the Academy rings one bell for every Great One asleep in the Wyrmback, "and the thirteenth, the old porters say, so that whatever lies beneath them knows it is still being watched." |
| `starwell` | Secret, Arcana 15, Daily | HeroicInspiration, plus lore `starwell` on first success: the Academy's founding myth, a star fell here and the founders built around it. Neutral. |
| `great_ones_statue` | Lore spot (sign-like) at the courtyard statue | The legend as the Academy tells it, proudly. |
| `primer_ashen_age` | Lore spot on a refectory bench | A student's textbook, *A Primer of the Ashen Age*. The tyrant legend in schoolbook voice; Juniper has doodled in the margins. |

**People:**
- **Perrin:** Act II schedule on the grounds (courtyard 8–18, refectory 18–21). New nodes when `World.Act >= 2`: delighted to see you, nervous about Ashby ("he's wonderful, he's terrifying, he's both, mostly before breakfast"). He points you to the tower.
- **Pell** (a second-year who sneaks out at night): has an Overdue book (Task 6).
- **Juniper** (cheerful, obsessed with the dragon): asks to pet it. Dragon opinion Fond.
- **Oakes, Varrow:** see Task 6.
- Dragon opinions:

| Opinion | Who |
|---|---|
| Trust | Juniper |
| Fond | Oakes |
| Wary | Varrow, Holloway |
| Distrust | Ashby (Phase 2 set none for him; add it) |

- [ ] **Step 1: Write failing tests:**
  - `AcademyGroundsAreReachable`: from the road gate spawn, reach every secret, lore spot, NPC daytime spot and every door warp.
  - `ThirteenthBellOnlyAtMidnight`: at 23:00 the bell secret is unavailable; at 00:30 it is available, and success records lore `thirteenth_bell`.
  - `AcademyHoldsTheBlightBack`: at stage 2, `Blight.OverlayAlpha(academy) < OverlayAlpha(town)`.
  - `AcademyHasVisibleMagic`: the built academy zone has at least 6 `Floater` and 8 `WardShimmer` components and one orrery.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `LoreTests` (secret guard) + `WorldDataIsConsistent`.
- [ ] **Step 5:** Commit `"Act II: the Starfall Academy grounds (orrery, starwell, thirteen bells, wards)"`.

### Task 4: The Library of Quiet Stars, the Restricted Section and the Sealing Hall

**Files:** `Maps.cs` (`Library` about 34×24, `Restricted` about 24×18 Dark, `SealingHall` about 26×22 Dark), `WorldDb.Academy.cs`, `DialogueDb.Academy.cs` (Thorne), `MonstersDb.cs`, `LoreDb.cs`, tests.

**Interfaces (produces):** zones `library`, `restricted`, `sealing_hall` (`DragonStays = true`); NPC `thorne`; monster `rug_of_smothering`; flags `stacks_unlooped`, `restricted_pass`; lore `emberhorn_warden` (**Phase 6 reads `World.LoreFound.Contains("emberhorn_warden")`**), `sealing_work`, `altered_annals`.

**The library:** tall stacks `z`, reading tables `a`, desks `-`, floating lanterns `^`, a rug-lined reading room and Thorne's desk `C`.

**The Endless Stacks:** a long east-west corridor of shelves.
- At its east end a warp (`ForbidFlag stacks_unlooped`) sends you back to its west end, with ClosedText unused and a narration via the warp's own line: "The shelves go on. And on. That reading lamp looks familiar."
- A secret halfway along, `stacks_illusion` (Arcana 15 **or** Investigation 15; two SecretDefs on adjacent tiles), sets `stacks_unlooped`: "You stop looking at the shelves and look at the gaps between them. The corridor folds up like a paper fan, and the door was three steps away all along."
- Past it, the warded door to the Restricted Section: a warp with `RequiresFlag restricted_pass` and ClosedText "Runes crawl across the door like ink in water. It does not open for you."

**Ways into the Restricted Section:**
- Thorne's pass (Task 6 reward).
- Secret `unweave_ward` (Arcana 16, PassageTo inside the door; on success also sets `restricted_pass`).
- Secret `pick_ward_lock` (Sleight of Hand 16, same effect).

**The Restricted Section** (Dark): chained shelves, a reading lectern and a stair down `V` to the Sealing Hall.
- Group `restricted_guardians`: 2 `animated_armor` + 1 `rug_of_smothering`. Style None, `VouchFlag thorne_pass_shown`, VouchText: "The armors turn their helms to the Archivist's seal on your pass, and settle back into their alcoves."
  - Thorne's pass sets `thorne_pass_shown` when given. Picking the ward does not, so you fight them or sneak (existing sneak/ambush).
  - `rug_of_smothering`: SRD (AC 12, HP 33, speed 10, Smother +5 2d6+3 bludgeoning; the grapple is text only). It's a hand-drawn rug sprite.
- Lore spots:
  - `emberhorn_warden` (category Records): *"The Warden of the Emberhorn", from a bestiary chained to its shelf.* An ancient dragon sleeps in the volcano's heart. Its fire has gone wrong: it burns grey at the edges. Wardens of old noted that when it draws breath, the plates of its throat part and glow, and a blow there as it breathes makes it choke on its own fire.
  - `altered_annals` (Records): an Academy annal where one year's entries are written over in a different ink, the original scraped away. Neutral: no culprit, no hint.

**The Sealing Hall** (Dark, `DragonStays`):
- A vast chamber, with a dragon-sized rune circle `<` (radius 7) around an empty dais, ward pillars `+` at twelve points, and floating stones `^` held in place mid-air.
- Scaffolding, Academy workbenches and a chalkboard.
- On first entry, narrate it: "Someone has been building something down here, for a long time, with great care. The circle is big enough to hold a dragon. Not a small one."
- Lore spot `sealing_work`: Ashby's assistants' notes. "Circle nine of twelve. The binding held a fire-drake of the third size for an hour. It must hold forever." It's grey, and it's Ashby's plan made visible.
- No combat: the dread is the point.

**Thorne** (the Archivist): precise, dry and fiercely fond of her books. She gives quest `overdue` (Task 6). Insight 14: she knows about the hall and hates it.

- [ ] **Step 1: Write failing tests:**
  - `StacksLoopUntilSeenThrough`:
    - Walking onto the loop warp returns you to the corridor's west tile.
    - Resolving `stacks_illusion` successfully sets the flag.
    - Afterwards the warp tile doesn't move you.
    - Save, load: still unlooped.
  - `RestrictedSectionOpensEveryWay`:
    - Without the flag, the door warp narrates and doesn't move you.
    - Each of: `World.SetFlag("restricted_pass")`; `unweave_ward` success; `pick_ward_lock` success → you can enter.
    - With `thorne_pass_shown`, `restricted_guardians` stands down with full XP (the existing VouchFlag path).
  - `DragonWaitsOutsideTheSealingHallAndRejoins`:
    - Act 2 with the dragon. Warp P1 into `sealing_hall` → the dragon is still in `restricted`, and the bark was shown.
    - Warp P1 back to `restricted` → the dragon follows again (same zone within 10 frames).
    - Save in the hall and load → the dragon spawns beside P1 only once P1 leaves.
  - `WardenLoreIsInTheRestrictedSection`: a LoreSpot with id `emberhorn_warden` exists in `restricted`, and `LoreDb.Get("emberhorn_warden").MinAct == 2`.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + LoreTests + DragonTests.
- [ ] **Step 5:** Commit `"Act II: the Library of Quiet Stars, the Endless Stacks, the Restricted Section and the Sealing Hall"`.

### Task 5: The Orrery Tower and High Magister Ashby

**Files:** `Maps.cs` (`AshbyTower`, about 18×16: a round study with a great window, star charts and an orrery model `&`), `WorldDb.Academy.cs`, `DialogueDb.Academy.cs` (`ashby`), tests.

**Interfaces (produces):** zone `ashby_tower`; NPC `ashby` (always in the tower: 8–22 at the desk, otherwise at the window, "watching the mountains"); flag `ashby_heard`; standing changes via `Factions.Adjust("academy", …)`.

- **Entering:** "The stairs climb, and climb. You count three hundred and twelve. From outside, the tower has four windows." (one-time narration).
- **The conversation** (the spec's Goal 4: sit down and hear him out; his case is reasonable):
  - He offers tea that pours itself.
  - He tells the legend as he sincerely believes it.
  - He explains the dragons are waking, so the Great Ones' hold is loosening.
  - His plan: understand the waking dragons, then seal them, gently and forever, before the Queen wakes with them. "I don't hate dragons. I've loved them my whole life. That is exactly why I know what one can do."
  - He asks to study your dragon: "Not now. Not without your leave. But think on it."
- **Options:**

| Option | Check | Result |
|---|---|---|
| (Insight) | DC 14 | Success: "He hasn't slept properly in a long time, and he's frightened. Not of you." No standing change. |
| (Arcana) | DC 15 | Success: you recognise the sealing theory; he lights up and treats you as a peer. Academy +5. |
| "You could be right." | none | Academy +10 |
| "You're talking about a cage." | none | Academy −5; he answers gently and doesn't argue back. |
| "Keep away from our dragon." | none | Academy −5; dragon bond +2 for the speaker. |
| (Persuasion) "Show us the Sealing Hall, then." | DC 16 | Only if `seen:sealing_hall`. Success: he's honest about it, and grateful someone asked to his face. Academy +5. Failure: he goes quiet. Either way, flag `ashby_hall_confronted`. |

  Every ending path sets `ashby_heard`.
- **The dragon:** distrusts Ashby (Task 3), so the existing trust system gives Advantage on Insight with him when it's within 4 tiles. Its emote shows.
- **Never:** any hint about the Queen's nature, or a Crown culprit. Ashby says "the Queen" and "the tyrant" only.

- [ ] **Step 1: Write failing tests:**
  - `HearingAshbyOutAdvancesAnswers`: Act 2, `answers` at 2. Warp to `ashby_tower` → stage 3. Drive the dialogue (DialogueScreen `Goto`/choose like DragonTrustTests) down "You could be right." → `ashby_heard`, stage 4, academy standing +10, StageXp paid once.
  - `ConfrontingAboutTheHallNeedsToHaveSeenIt`: that option isn't shown without `seen:sealing_hall`, and is shown with it.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + LoreTests (the guard scans Ashby's lines too).
- [ ] **Step 5:** Commit `"Act II: the Orrery Tower and High Magister Ashby"`.

### Task 6: The professors' quests

**Files:** `WorldDb.Academy.cs` (glasshouse, observatory, book spots), `DialogueDb.Academy.cs`, `QuestDb.cs`, `ItemsDb.cs`, `MonstersDb.cs`, tests.

**Interfaces (produces):** quests `overdue`, `glasshouse`, `measure_wyrm`; items `overdue_book` (quest item, stack 3), `ember_lily` (consumable: resistance to fire for 1 hour, via the existing timed-effect path if there is one; otherwise a Potion of Resistance (fire) equivalent), `star_chart` (trinket, 50 gp); monster `vine_blight` (SRD: AC 12, HP 19, Constrict +4 2d6+2).

**`overdue`** ("Overdue", Thorne):
- The task: three overdue books on the grounds.
  1. In the geese's nest by the pond. Secret, Animal Handling 13 or Athletics 13 (two spots).
  2. Pell has one. Dialogue: Persuasion 13, or Intimidation 12, or Deception 14, or trade a `star_shard` / any 10 gp trinket. Pell gives it, and confesses he sneaks out to watch the thirteenth bell.
  3. At the hedge maze's centre. A plain pickup spot: the maze is the challenge.
- Each book gives `overdue_book`. At 3 → stage 2 "Return them to Thorne".
- Reward from Thorne: `restricted_pass` + `thorne_pass_shown`, 600 XP, 40 gp, and Thorne friendship.

**`glasshouse`** ("The Wilting Glasshouse", Oakes):
- Oakes's ember-lilies are greying.
- In the Glasshouse, group `glasshouse_blights`: 4 `vine_blight`. Style Beasts, TalkDC 14, "calm the vines" with Nature as the skill: data `TalkSkill = Nature`. If `EnemyGroupDef` lacks a skill override, add `public Skill? TalkSkill;` used by the encounter choice.
- After they're resolved, secret `cracked_heatstone` (Nature 13 or Arcana 13): the glasshouse's heat-stone came from the Emberhorn's slopes and its fire has turned grey. Lore `grey_fire`, minAct 2.
- Reward: 700 XP, 2 `ember_lily`, Oakes friendship. Fighting and calming both pay.

**`measure_wyrm`** ("The Measure of a Wyrm", Varrow):
- Varrow wants to measure your dragon under the stars at the Observatory, after 20:00.
- Options at the meeting:
  - Let her. Bond −3, academy +5.
  - Refuse politely. No change; the quest still completes.
  - "Let {dragon} decide": Animal Handling 13. Success: it allows one wing-measure, bond +2, academy +5. Failure: it sulks, bond −1, no academy change.
- (Insight 15) at any time: she writes her notes twice, and the second copy is sealed with plain unmarked wax and sent with the evening post. This is a hook for Phase 4: neutral, no faction named.
- Every resolution completes the quest. Reward: 500 XP and `star_chart`.

- [ ] **Step 1: Write failing tests:**
  - `OverdueBooksOpenTheRestrictedSection`: collect 3 books (give items / resolve spots) → stage 2. Talk to Thorne → done: `restricted_pass` and `thorne_pass_shown` set, XP paid once.
  - `GlasshouseCalmPaysLikeFighting`: resolve the group peacefully in one run and by `ResolveGroup(victory)` in another. Same XP, and the heat-stone secret then appears.
  - `MeasureOfAWyrmEveryChoiceCompletes`: each of the three choices completes the quest. Bond and standing deltas are as listed.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + QuestTests + LoreTests.
- [ ] **Step 5:** Commit `"Act II: professors' quests (Overdue, The Wilting Glasshouse, The Measure of a Wyrm)"`.

### Task 7: Acceptance, screenshots, docs, build

- [ ] **Play-mode `AcademyGoldenPath`:** Start at Act II, then:
  - take the Ford east road and cross the Lantern Road;
  - enter the Academy (Answers → 2);
  - see through the Stacks;
  - get the pass from Thorne via the test hooks;
  - read the warden lore;
  - the dragon stops at the Sealing Hall;
  - hear Ashby out (Answers → 4);
  - save, load: everything holds.
- [ ] **Screenshots** to `%TEMP%/embers_screenshots/act2_*.png`. Review them for "powerful and mysterious":
  - the Lantern Road;
  - the Academy courtyard by day, and at night with the orrery glowing;
  - the Library Stacks;
  - the Sealing Hall;
  - Ashby's study.
- [ ] **Docs:**
  - PLAYTEST.md: "New: the Lantern Road and the Starfall Academy", with a checklist.
  - DESIGN.md: the gated warps, DragonStays, hour-window secrets, the Academy set pieces, the quests and the XP table.
  - LORE_PITCH.md: the new names (approved in bulk).
- [ ] **Release:** the full rules suite and the play-mode suite green; rebuild the exe and zip; end-of-branch review; fix pass; merge `act2/phase3-academy` into main; push.
