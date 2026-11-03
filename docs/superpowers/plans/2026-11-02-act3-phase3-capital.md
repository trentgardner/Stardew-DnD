# Act III Phase 3: The Capital Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Act III opens in the party's first visit to the capital, **Aurelhold**. It is a grand, warm city inside the ring of the Wyrmback, bright while the rest of the realm went grey. It is full of Crown pageantry, and small things are off if you look. The party gets there by the King's carriage and has an audience with the King. They explore the upper city, the Roots below it, and four interiors, take on side quests, and follow four clues down to a sealed foundation gate. The gate opens in Phase 4.

**Architecture:** Same data-driven pattern as the Academy (Act II Phase 3):
- Zones: `ZoneDef` + `Maps` arrays in a new `WorldDb.Capital.cs`.
- Dialogue: a new `DialogueDb.Capital.cs`.
- Quests in `QuestDb`, lore in `LoreDb`.

Small engine additions:
- Two new map chars (the victory statue, the ever-lamp).
- `ZoneDef.DragonStaysText`.
- A dialogue-driven carriage warp that moves every ready player.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite takes ~25 min, so run it in the background). Art goes through `Assets/Editor/ArtMap.txt` and `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-11-02-act3-design.md (§1 "The capital", geography, the four forces, Buoy, the anti-goals). Roadmap item 3 is in docs/superpowers/plans/2026-11-02-act3-phase1-levels.md.

## Global Constraints

- **THE SECRET (spec anti-goal):** nothing before the door may hint that she is a child. The capital may grow suspicion about the Crown; that is allowed and wanted. `LoreTests.NoActTwoTextHintsAtTheSecret` must stay green and must scan the new files. No new file is named `*.AfterTheDoor.cs` in this phase. The banned word starts include "son" and "small", so avoid "song" and "smaller" in any line that names the Queen.
- **Unassuming (user):** the clues read as a city with odd details, never as a mystery box shouting "something is down there". Each clue is one quiet line, not a cutscene.
- The capital is **warm and bright while the realm is grey** (spec). `BlightWeight = 0` everywhere in Aurelhold. The ever-lamps burn day and night.
- The Crown is grey: soldiers, clerks and pageant folk sincerely believe the legend. Only the throne knows, and in Phase 3 the King gives nothing away beyond an Insight tell.
- Tone: bright fantasy, PG-13, Mercer + Mulligan. Player tastes: varied, explorable, hand-shaped places with landmarks, never grid-like or empty.
- Dice are always visible. Peaceful options pay the same XP as fighting.
- Data-driven content. Every new art id gets an ArtMap line.
- Every window closes with Esc/B and has a ✕. Tests run muted.
- Co-op: either player can do anything, world flags are shared, and the carriage takes every ready player.
- **Buoy stays pettable in Thistledown in Act III** (spec goal 10; he comes to the capital in Phase 7's ending).
- Working names are approved in bulk; list them in LORE_PITCH.md.
- Branch `act3/phase3-capital`. Merge to main and push after the full suite passes.

## Names (working)

- **Aurelhold**, the capital, in the high valley inside the Wyrmback ring.
  - **The upper city:** the **Gate of Wings** (the south gate, where the carriage stops), the **Great Square** with the **Victory of the Crown** statue and the **Ever-Lamps**, the **Sunstair** up to the palace, the **Grand Market**, the **Hall of Records**, the **Lantern Cathedral** and its crypt stair, and **the Gilded Wyvern** inn.
  - **The Roots:** the lower city at the mountain's roots, with workers' terraces, **the Steamhouse** (hot baths fed from below), the **Foundation Works** and its sealed **Undergate**.
- **People:**
  - **King Aldric IX**.
  - **Royal Courier Bertram Quell** (the carriage).
  - **Wenna Brightwick**, a lamplighter with nothing to light.
  - **Mags Tolley**, whose husband Bran works the deep shift.
  - **Foreman Crale** of the Foundation Works.
  - **Pageant-Master Fennimore Glass**.
  - **Archivist-Royal Delphine Oare**.
  - The four leaders, in the capital for the pageant: Marsh, Ashby, Sorrel and Calloway.
- **The Thousandth Pageant:** the city's festival for a thousand years since the Queen's fall, in a month.

## Review Focus

1. **Every way into Act III starts "The Warm City" exactly once:**
   - BeginActThree.
   - ApplyActThreeStart.
   - Loading a Phase 2 Act III save that lacks the quest.
   - It never restarts once advanced, after a reload too.
   Test: `WarmCityStartsWithActThreeEveryWay` (Task 1).
2. **The carriage in co-op:** P2 standing elsewhere, or in a dialogue or menu, still arrives with P1. Companions and the dragon come too. Riding back lands everyone in Thistledown square. Nobody is left in a zone alone. Test: `CarriageTakesEveryoneBothWays` (Task 2).
3. **The dragon and DragonStays interiors in the city:** walk into the throne room and the archive, load a save made inside, or have P2 in the square. The dragon waits outside with the zone's own line, rejoins on exit, and is never stuck. Test: `DragonWaitsOutsideThePalaceAndRejoins` (Task 4).
4. **The clue count:** any 3 of the 4 clues move "The Warm City" to stage 4. The fourth still records its lore. A clue found again (Daily, after load) never double-pays XP or re-advances. Test: `AnyThreeCluesPointDown` (Task 5).
5. **The secret guard covers the capital:** both new files are scanned, and no capital line breaks the guard. Test: `CapitalFilesAreScannedBySecretGuard` (Task 1) plus the existing guard.

---

### Task 1: Engine bits and the main quest "The Warm City"

**Files:**
- `World/Zone.cs` (map chars `7`, `8`)
- `World/Maps.cs` (legend)
- `World/WorldDb.cs` (`ZoneDef.DragonStaysText`)
- `Core/GameRoot.cs` (`Warp` uses it)
- `Quests/QuestDb.cs` (`WarmCity`)
- `Core/GameRoot.ActThree.cs` (start the quest)
- `Core/SaveSystem.cs` or the load hook (start it on an Act III load that lacks it)
- `Assets/Editor/ArtMap.txt`
- Tests: `Assets/Tests/PlayMode/CapitalTests.cs` (new; SetUp/TearDown as ActThreeIntoTests) and `LoreTests.cs`

**Interfaces (produces):**
```csharp
public static class WarmCity { public const string Id = "warm_city"; public const int Summoned = 1, Arrived = 2, Audience = 3, Below = 4; }
public class ZoneDef { public string DragonStaysText; } // null = the old "won't come down the stairs" line; "{name}" is replaced
```
- Map chars:

| Char | Becomes | Solid? | Art id |
|---|---|---|---|
| `7` | the Victory of the Crown statue | solid | `obj_victory_statue` |
| `8` | an ever-lamp | solid | `obj_ever_lamp`, always lit (LightSource on day and night, warm amber, not the violet MagicLit glow) |

  Hand-draw both as 16×16 `pattern` lines in ArtMap under "Act III Phase 3: the capital":
  - The statue: a knight on a plinth with a sword raised over a crowned shape in carved flames. Stone grey and gold.
  - The lamp: a black-iron post with an amber flame.
- Quest `warm_city`, "The Warm City", Act 3:
  - Stage texts:
    1. "A royal courier is waiting in Thistledown square with the King's own carriage."
    2. "Aurelhold, the capital, inside the ring of the Wyrmback. Everywhere else went grey. Here, every lamp is lit. The King expects you at the palace."
    3. "The King was gracious and asked your dragon to lead the Thousandth Pageant. The city is beautiful. Some of it doesn't add up."
    4. "Lamps that burn on nothing. Records with holes in them. A crypt nobody admits to using. A shift that never comes up. Everything in Aurelhold points down, to the Undergate in the Roots."
  - StageXp: {2: 2000, 3: 3000, 4: 6000}.
  - Trigger: `ZoneEntered "capital"` 1 → 2.
  - Trigger: `AllFlags(3, 4, clue_lamps, clue_records, clue_crypt, clue_shift)` with "at least 3".
- `BeginActThree` and `ApplyActThreeStart` set stage 1. Loading a save with `World.Act >= 3` and the stage at 0 sets stage 1 (no XP, no toast).

- [ ] **Step 1: Write failing tests** in CapitalTests.cs:
  - `WarmCityStartsWithActThreeEveryWay`:
    - BeginActThree after WardenDown gives stage 1.
    - ApplyActThreeStart gives stage 1.
    - An Act III save with the quest reset to 0 by hand loads at stage 1.
    - Set stage 3, save, load: still 3.
  - `CapitalCharsBuild`: a test ZoneDef row `"78."` builds. `7` and `8` aren't walkable. `8` has a LightSource that is on at noon.
  - `DragonStaysTextIsUsed`: a test zone with `DragonStays = true, DragonStaysText = "{name} waits."`. Warping there narrates "<name> waits.".

  Add to LoreTests:
  - `CapitalFilesAreScannedBySecretGuard`: `GuardScans("WorldDb.Capital.cs")` and `GuardScans("DialogueDb.Capital.cs")` are true.
- [ ] **Step 2:** `.superpowers/unity-test.sh CapitalTests` → FAIL (compile).
- [ ] **Step 3:** Implement. Run `.superpowers/import-art.sh`.
- [ ] **Step 4:** `CapitalTests` + `ActThreeIntoTests` + `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act III engine: the victory statue and ever-lamps, the dragon's own waiting line, and 'The Warm City' begins"`.

### Task 2: The King's carriage and Aurelhold's upper city

**Files:**
- `Maps.cs` (`Capital`, about 64×48)
- `WorldDb.Capital.cs` (new: `BuildCapital()`, called from the WorldDb static ctor after `BuildEmberhorn()`; NPCs in `BuildCapitalNpcs()`)
- `DialogueDb.Capital.cs` (new, wired like `DialogueDb.Academy.cs`)
- `LoreDb.cs`, `DragonDb.cs`, ArtMap (NPC looks, `music_capital` = an unused pack track, e.g. "32 - Manor.ogg")
- Tests: CapitalTests

**Interfaces (produces):**
- Zone `capital` ("Aurelhold", `Act = 3`, `BlightWeight = 0`, `Music = "music_capital"`), DefaultSpawn at the Gate of Wings.
- NPC `quell` (the courier):
  - In Thistledown square while `act3` is set (`RequiresFlag = WorldDb.ActThreeFlag`), all day.
  - In the capital at the Gate of Wings, all day.
- `GameRoot.RideCarriage(PlayerSlot by, string zone, Vector2Int pos)`: warps every ready player to `pos`, then `pos + (1,0)` and so on. It closes their screens first, and companions and the dragon follow. It narrates the ride once to each player.
- Dialogue `quell`:
  - In town: "To Aurelhold" (ride) and "Not yet".
  - In the capital: "Back to Thistledown" and "Stay".
  - First ride narration: "Two days up the King's Road, through passes white with early snow. Then the carriage tops a ridge and the whole party goes quiet. A city fills the valley inside the mountains' ring, and every window in it is lit."
- Lore ids: `victory_statue`, `ever_lamps`, `thousandth_pageant` (a pageant poster).
- Doors (warps added by later tasks with their interiors):
  - the Sunstair top door → `throne_room`;
  - the Hall of Records → `royal_archive`;
  - the Gilded Wyvern → `capital_inn`.
- The east stairs → `roots` (Task 3).

**Layout** (hand-shaped; landmarks you can name from across the map):
- **South:** the Gate of Wings, a great arch in the city wall `W`, with Crown guards (flavour NPCs). The carriage stop is just inside.
- **Centre:** the **Great Square**, cobbles `o` with the **Victory of the Crown** statue `7` at its heart and eight ever-lamps `8` around it. There are fountains `O`, benches `N` and banners.
- **West:** the **Grand Market**, rows of stalls `m`. The **Lantern Cathedral** is a big gold roof `G`, with a walled yard and a crypt stair `V` (Task 5's clue spot).
- **North:** the **Sunstair**, wide stairs up to the palace front (a long red roof `R`), with the throne-room door.
- **East:** the **Hall of Records** (blue roof `U`), the Gilded Wyvern (red roof), and the stair down to the Roots at the east wall.
- Gardens with trees `t`, flowers `*` and planters `p`. Ever-lamps line every main street. No dead space: every block holds a landmark, people or a secret.

**Content:**
- **EnterNarration (first entry):** "Aurelhold. Snow on every peak of the ring, and not a flake in the streets. The air is warm. The lamps are lit at noon. Somewhere a band is rehearsing, badly, and three hundred people have stopped to stare at your dragon."
- **Dragon's first-entry bark:** "{name} has to duck under the Gate of Wings. A baker drops an entire tray of buns. {name}, after some thought, helps." (No children in pre-door capital lines that sit near the legend: keep the guard's words out entirely.)
- **`victory_statue`** (lore spot at the statue): the legend as the Crown tells it, proudly.

  "A THOUSAND YEARS. Here the first King of Aurelhold struck down the Ashen Queen and bound her fire, so that the realm might never burn again. *Long may the Crown keep the flame.*"

  Add a narration line: the crowned figure in the carved flames has been worn smooth by a thousand years of hands. Nobody rubs this one's nose.
- **`ever_lamps`:** a Secret at one lamp, Arcana 16.

  "No oil. No wick. The flame sits on bare iron and burns on nothing at all, and the post is warm, like a hearthstone. The warmth isn't coming from the flame. It's coming up from the ground."

  Sets `clue_lamps` and lore `ever_lamps`. Failure: "Pretty. Warm. You can't see what feeds it. Try again tomorrow."
- **`thousandth_pageant`** (poster sign): "THE THOUSANDTH PAGEANT! One month hence. The Fall of the Queen, with fireworks. Performers wanted. DRAGONS ESPECIALLY WANTED (enquire F. Glass, the Square)."
- **NPCs with schedules:**
  - `wenna` (Wenna Brightwick, lamplighter, the square by day). Her lines: "I'm the Lamplighter of the Great Square. Thirty years. I've never lit a single one. They're always lit. I polish them, mostly." She points at the lamps as a curiosity, gently.
  - Two guards at the gate.
  - Townsfolk: a baker, a band, children by the fountain. They use existing looks; each has a one-line bark through a small `capital_folk` dialogue keyed by id.
- **Dragon opinions:**

| Opinion | Who |
|---|---|
| Fond | Wenna, Quell |
| Wary | the gate guards |

- [ ] **Step 1: Write failing tests:**
  - `CarriageTakesEveryoneBothWays`:
    - Co-op: P1 in town at Quell; P2 in the forest with a menu open; the dragon present.
    - `RideCarriage(p1, "capital", gate)` puts both players, their companions and the dragon in `capital`, with no screens open.
    - Riding back puts everyone in `town`.
    - `warm_city` is at stage 2.
  - `QuellOnlyInActThree`: in Act 2, Quell's schedule has no town entry that applies. In Act 3 he is at the square.
  - `CapitalIsReachable`: from the gate spawn, every NPC day spot, lore spot, secret, door and the Roots stair can be reached.
  - `CapitalIsWarmWhileTheRealmIsGrey`: at blight stage 2, `Blight.OverlayAlpha(capital) == 0` and `> 0` in town. The capital has at least 12 ever-lamps, all lit at 12:00.
  - `BuoyStaysInThistledownInActThree`: in Act 3, `TownDog.HomeFor(d)` is a Thistledown-area zone for every weekday, and Buoy can be petted in town.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `LoreTests` + `WorldDataIsConsistent`. Take a screenshot of the square by day and by night and review it: warm, busy, landmarks.
- [ ] **Step 5:** Commit `"Act III: the King's carriage and Aurelhold's upper city (the Great Square, the ever-lamps, the Victory statue)"`.

### Task 3: The Roots, and "The Deep Shift"

**Files:**
- `Maps.cs` (`Roots`, about 56×40; outdoors, but the mountain roof makes it dusky: `Dusk = true`)
- `Maps.cs` (`Steamhouse` interior, about 18×12)
- `WorldDb.Capital.cs`, `DialogueDb.Capital.cs`, `QuestDb.cs`, `LoreDb.cs`
- Tests: CapitalTests

**Interfaces (produces):**
- Zone `roots` ("The Roots of Aurelhold", Act 3, `BlightWeight = 0`). The east stairs connect it both ways with `capital`.
- Zone `steamhouse`.
- NPCs `mags`, `crale`.
- Quest `deep_shift` ("The Deep Shift", Act 3, giver `mags`). Stages:
  1. "Mags Tolley's husband Bran works the deep shift at the Foundation Works. His letters come up every week. He never does."
  2. "Foreman Crale won't talk about the deep shift. The works ledger might."
  3. "The deep shift goes down through the Undergate and stays down, on the Crown's orders and the Crown's pay. Bran is alive, somewhere below."

  Reaching stage 3 sets `clue_shift`. Stage 3 is not the last stage. Phase 4 adds stage 4, "Bran comes home", so the journal shows it as ongoing. StageXp: {3: 4000}.
- Group `roots_patrol` (Talk, TalkDC 16, ScareDC 18, label "the Crown patrol"): 2 `crown_knight` + 1 `crown_battlemage`, there 22:00–05:00 only. "Curfew in the Roots, by order." Vouch: `banner_crown` stands them down.

  This needs a new night-window field on the group, `EnemyGroupDef.NightOnly`, mirroring SecretDef. Add it in this task with its own test.
- Group `crale_men` (Brawl, TalkDC 15): 3 `brawler`. They're there if you push Crale too hard (stage 2, Intimidation failure).

**Layout:**
- Narrow, winding alleys of terraced workers' houses cut into the rock `X`. Washing lines `L` between them.
- The **Steamhouse** with a door.
- A market of second-hand stalls.
- A shrine to the Dawnmother with candles `i`.
- The **Foundation Works**: a fenced yard with crates `x`, a crane of fences and a huge iron door in the cliff face. That is the **Undergate**, a Sign for now: "A gate of black iron twice the height of a house, set straight into the mountain. It's barred from this side and sealed with the King's own seal, in wax the size of a dinner plate. The iron is warm."
- Steam vents `e` (glowing crystals, reused as warm vents).
- The stairs up to the capital.

**Content:**
- **EnterNarration:** "The Roots. The city's underside, where the mountain leans over the rooftops and the light comes from lamps and steam. Warm here too. Warmer, if anything."
- **Mags** (by the Steamhouse): starts `deep_shift`.
- **Crale** (the Works yard, 6–18):
  - Insight 14 shows that he's scared, not cruel.
  - Persuasion 16 gets "Crown orders, sealed. Deep shift goes down and stays down. Paid triple. I don't ask."
  - Intimidation 15:
    - success gives the same line;
    - failure brings `crale_men`.
  - Either success → stage 3.
  - **Sneak route:** a SecretDef `works_ledger` (Sleight of Hand 16, QuestStage 2, AdvanceTo 3). "Forty names in the deep-shift column, going back years. Not one of them in the 'came up' column." Lore `works_ledger`.
- **Steamhouse:** a Secret `hot_spring`, Medicine 13, Daily. It heals to full and grants HeroicInspiration. "The water comes up from below, near boiling, and smells of nothing. Not sulphur. Not stone. Just heat."
- Wenna's sister, a Roots lamplighter, has a one-liner: "Down here we use oil. Up there they don't. Nobody minds, so long as it's warm."

- [ ] **Step 1: Write failing tests:**
  - `DeepShiftEveryWay`:
    - Talk Crale (Persuasion success hook) → stage 3 and `clue_shift`.
    - Alternatively the ledger secret → stage 3.
    - Intimidation failure spawns `crale_men` as a Brawl.
    - The journal shows the quest as not complete at stage 3.
  - `RootsPatrolOnlyAtNight`: at 23:00 `roots_patrol` is in stage; at 12:00 it isn't. With `banner_crown`, the patrol stands down with full XP.
  - `RootsIsReachable`: every spot can be reached from the stair spawn.
  - `UndergateIsSealedForNow`: the Undergate tile has a sign and no warp.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS + `LoreTests` + `WorldDataIsConsistent`; screenshot of the Roots.
- [ ] **Step 5:** Commit `"Act III: the Roots of Aurelhold, the Steamhouse, and 'The Deep Shift'"`.

### Task 4: The palace and the King, the Hall of Records, the Gilded Wyvern

**Files:**
- `Maps.cs`:
  - `ThroneRoom`, about 30×22: a long hall of pillars `P`, red rugs `_` and a dais. Ever-lamps line the walls.
  - `RoyalArchive`, about 26×18: shelves `z` and desks `-`.
  - `CapitalInn`, about 20×14.
- `WorldDb.Capital.cs`, `DialogueDb.Capital.cs`, `LoreDb.cs`
- Tests: CapitalTests

**Interfaces (produces):**
- Zones:
  - `throne_room` (DragonStays, `DragonStaysText = "{name} tries the palace doors, which are tall, but not that tall. It settles on the Sunstair to wait, and the guards pretend not to be nervous."`).
  - `royal_archive` (DragonStays, text: "{name} takes one look at all that paper and decides to wait outside. Wise.").
  - `capital_inn` (SafeForLongRest, with the inn's rest offered by the keeper).
- NPCs:
  - `king_aldric`: the throne room, 9–17. Away otherwise ("The King is not receiving.").
  - `oare`: the archive, 8–18.
  - `inn_keeper` of the Gilded Wyvern.
- Dialogue `king_aldric`, the audience. It sets `warm_city` 2 → 3 on its first full conversation.
  - **Opening:** "So you are the heroes of the Emberhorn. And that, outside, I'm told, is the dragon. Welcome to Aurelhold. You've done the realm a service it can't repay, so let me at least try."
  - **The ask:** lead the Thousandth Pageant with your dragon, "a dragon who serves the Crown, a thousand years after the Fall. The people will never forget it." The reply options are Yes / Maybe / "Why now?".
  - **Insight 16 (the tell):** "He means every warm word. But each time you say 'Emberhorn', his thumb turns the plain iron ring on his finger, round and round, and he's glad when the subject changes."

    This is suspicion of the Crown only: no child, no door. Record lore `the_kings_ring`.
  - **History 15:** "Every King of Aurelhold has worn a plain iron ring under the gold one. Nobody knows why. It's tradition."
  - **Side reactions** (one line each):
    - `banner_crown`: "Lady-Commander Marsh speaks well of you. That is rare."
    - `banner_academy`: "Corvane's friends. He tells me you ask difficult questions."
    - `banner_fae`: "You've been to the Courts. Mind the bargains."
    - `banner_hunters`: "The Wyrmwatch. You'll forgive my guards for counting your arrows."
  - **The close:** "Rest. See my city. It's the only warm place left in the realm, and I'd like you to see why that matters."
- **`royal_archive`:** a Secret `royal_annals` (Investigation 15) sets `clue_records` and lore `royal_annals`:

  "The Royal Annals, nine hundred bound volumes. Year 1 of the Crown is magnificent. So is Year 3. Year 2 isn't there. Someone has cut the pages out with a razor, cleanly, a long time ago. The same year the Academy's Annals were 'corrected'."

  Oare: brisk, proud, honest. "Water damage." (Insight 13: she believes it.)
- **The Gilded Wyvern:** a warm, rowdy, gold-and-red taproom. A rest costs 1 gp per person. Story barks.

- [ ] **Step 1: Write failing tests:**
  - `AudienceWithTheKing`:
    - Talking to the King at 10:00 moves `warm_city` 2 → 3.
    - The Insight option records `the_kings_ring` on success.
    - At 20:00 the King isn't in the throne room.
  - `DragonWaitsOutsideThePalaceAndRejoins`:
    - Warping into `throne_room` narrates its DragonStaysText, and the dragon stays in `capital`.
    - Leaving, the dragon rejoins.
    - A save made inside the throne room loads with the dragon outside, never inside.
  - `RecordsClue`: the annals secret sets `clue_records` and its lore.
  - `InnRestWorks`: resting at the Gilded Wyvern long-rests every player and costs the stated price.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS + `LoreTests`; screenshot of the throne room.
- [ ] **Step 5:** Commit `"Act III: an audience with King Aldric, the Hall of Records, the Gilded Wyvern"`.

### Task 5: The crypt clue, the four leaders, the Pageant and the market

**Files:** `WorldDb.Capital.cs`, `DialogueDb.Capital.cs`, `QuestDb.cs`, `ItemsDb.cs`, `ShopDb` (or wherever shops live), `LoreDb.cs`, `DragonDb.cs`, tests.

**Interfaces (produces):**
- **The crypt clue:** a Secret `sealed_crypt` at the cathedral's crypt stair, Perception 14. Sets `clue_crypt` and lore `sealed_crypt`.

  "A royal crypt, sealed with iron and the King's seal. The dust on the steps is a year thick, except for a single clean path down the middle, worn smooth, as if someone walks it often. The verger swears no one has opened it in a century."
- **The four leaders** (NPCs with a few nodes each; Phase 5 uses them at the questioning):
  - `marsh_capital`: Marsh, at the palace gate. She is now the King's household guard, proud and tired. Crown side: warm; otherwise civil.
  - `ashby_capital`: Ashby, in the archive, "consulting the Royal Annals". Academy side: an ally. He saw the cut pages too, and it bothers him.
  - `sorrel_capital`: Sorrel, in the market's flower stalls. With `fae_clause` set, she reminds you of the favour, lightly.
  - `calloway_capital`: Calloway, in the Gilded Wyvern. Hunters side: counts the city's guards. With `wyrmwatch_belled`, the dragon's bell comes up.

  They use their existing Looks. Each has one line that grows suspicion of the Crown or of each other, without the secret.
- **Quest `dragon_parade`** ("A Dragon for the Parade", Act 3, giver `glass`). Fennimore Glass, the Pageant-Master, needs your dragon for the rehearsal. Stages:
  1. "Fennimore Glass wants {dragon} at the rehearsal in the Great Square at noon."
  2. "The rehearsal: hit your marks."

     Three dialogue beats, each a check:
     - Performance 14, to lead the march;
     - Animal Handling 14, to keep the dragon from eating the paper Queen;
     - Acrobatics or Persuasion 13, to save the falling scenery.

     Any two successes pass. A failure is funny, not fatal: it can be retried tomorrow.
  3. "The rehearsal was a triumph (mostly). Glass is beside himself."

  Reward: 3000 XP + `pageant_sash` (a cosmetic-plus item: +1 Charisma checks, `Performance` advantage). The paper Queen is a huge, puppet-flamed figure: the legend's image only.
- **The Grand Market shop** (`ShopId = "capital_market"`): the best gear in the game so far.
  - Existing +1/+2 items: the ItemsDb items with the highest tier.
  - Greater healing potions.
  - A new `potion_superior_healing` (8d4+8, 450 gp).
  - Where ItemsDb has no +2 item for a weapon kind, don't add one: this phase adds one potion, not a new tier.

- [ ] **Step 1: Write failing tests:**
  - `AnyThreeCluesPointDown`:
    - Clues 1 and 2 leave the quest at 3.
    - The third moves it to 4 and pays 6000 once.
    - The fourth still records its lore, without re-paying.
    - Save and load keeps 4.
  - `LeadersAreInTheCapitalForEachSide`: for each of the four sides (via ApplyActThreeStart), all four leaders are in their capital spots by day and their dialogues open without error.
  - `DragonParadePassesOnTwoOfThree`: two successes reach stage 3 and give the sash; two failures leave it retryable the next day.
  - `MarketSellsSuperiorHealing`: the shop lists it, and drinking one heals 8d4+8 through the dice tray.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS + `LoreTests` + `WorldDataIsConsistent`.
- [ ] **Step 5:** Commit `"Act III: the sealed crypt, the four leaders in the capital, the Pageant rehearsal, the Grand Market"`.

### Task 6: Acceptance, screenshots, docs, build

- [ ] **Play-mode `CapitalGoldenPath`:** Start at Act III (academy side), then:
  - ride the carriage (stage 2);
  - an audience with the King (stage 3);
  - the lamp clue, the records clue and the deep shift via test hooks (stage 4);
  - the rehearsal;
  - ride home and back;
  - save and load: everything holds;
  - Buoy pettable in town.
- [ ] **Screenshots** to `%TEMP%/embers_screenshots/act3_*.png`:
  - the Gate of Wings;
  - the Great Square by day and by night (warm lamps);
  - the Roots;
  - the Undergate;
  - the throne room;
  - the archive.

  Review them for "grand, warm, lived-in, with something off" and fix anything grid-like or empty.
- [ ] **Docs:**
  - PLAYTEST.md: "New: Aurelhold", with a checklist.
  - DESIGN.md: the carriage, the clue quorum, DragonStaysText, the night groups, the XP table.
  - LORE_PITCH.md: the new names (approved in bulk).
- [ ] **Release:**
  - The rules and play-mode suites are green.
  - Rebuild the exe and zip.
  - End-of-branch self-review, then the fix pass.
  - Merge `act3/phase3-capital` into main and push.
