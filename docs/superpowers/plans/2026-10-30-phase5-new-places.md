# Phase 5: New Places Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The Fallen Star becomes a real journey (Starfall Crater → the shiny thief → the kobold tracks → the multi-level Kobold Warrens → the egg). The Old Watchtower is a small Zelda-style puzzle ruin, and the Harvest Festival happens on a calendar day. This covers spec quests 1, 13, 14 and 17.

**Architecture:** New zones are plain `ZoneDef`s plus `Maps` arrays (crater, watchtower, warrens, egg_shrine), and the existing `mine` becomes level 1 of the Warrens. There are three small engine additions: (1) a puzzle layer (`PuzzleDef` pieces: plates, levers, push blocks, mirrors, a light beam, gates), with pure logic in `PuzzleLogic` so it can be tested; (2) `SecretDef` grows `Dialogue` (success opens a conversation) and `PassageTo` (success moves you through a crack: the sneak routes); (3) `EnemyGroupDef.VouchFlag` (a group stands down on sight if a friend vouched for you). The festival is a `Festival` static (which days, which hours) that drives a world lock `"festival"`. Schedules, props, music and four `FestivalGame` interactables key off that lock.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite takes ~12 min, run it in the background, muted), art via `Assets/Editor/ArtMap.txt` + `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md ("Content: Act I", places + quests 1/13/14/17; build order item 5).

## Global Constraints

- Rules stay 2024 PHB / SRD 5.2. Every check, attack and save (puzzle hints, festival games, stakeout, passages) goes through the visible dice tray (`p.Hud.Dice.RollD20`). DCs stay hidden.
- Peaceful resolution pays the same XP and loot as fighting. Quest XP goes to every ready player once (`QuestReward`, `Paid`).
- Tone: Mercer's lived-in world plus Mulligan's heart and humor. Light, PG. Kobolds are sincere and funny, never cannon fodder.
- New names go in docs/LORE_PITCH.md under "Pending approval: Act I side content": Perrin Quillby (Academy scholar), Elder Ssark, Tik (kobold trader), Krag (kobold war-boss), the First Ideal wording.
- No D&D/Stardew/Critical Role names in anything player-facing.
- Content stays data-driven (WorldDb, DialogueDb, QuestDb, MonstersDb, ItemsDb). Art stays swappable through ArtRegistry ids; every new id gets an ArtMap line (pack art) or a hand-drawn PlaceholderArt sprite listed in the art test.
- Every window closes with Esc/B and has a ✕. Tests run muted. Zelda flavor applies to exploration only; never change D&D mechanics for it.
- Phase 6 owns the dragon (hatching cutscene, pet, sniff, combat). Here `HatchEgg` keeps working as it does now, only from the new egg location.
- Work on branch `act1/phase5-places` off main. Don't push to GitHub.

## Review Focus

- **Old saves mid-main-quest:** a v1 save with `fallen_star = 1..4` loads into the new stage numbers (1→3, 2→4, 3→5, 4→6) and never re-pays. A save standing on a mine tile that is now a wall spawns at the zone's DefaultSpawn. Tests: `OldSaveFallenStarMigrates`, `SavedPositionOnAWallFallsBackToSpawn` (Task 1).
- **Skipping ahead:** a player who never visits the crater or catches Snik and walks straight down the mine still reaches the egg, and the journal shows the right step. Test: `MainQuestCanSkipTheCraterAndSnik` (Task 3).
- **Puzzle soft-locks:** a push block shoved into a corner, or a player leaving mid-puzzle: leaving and re-entering the zone resets the unlatched pieces, latched gates stay open forever, and a block never moves onto a creature, warp, door or gate. Tests: `PuzzleResetsOnReentryButLatchedGatesStayOpen`, `BlockOnlyMovesOntoFreeFloor` (Task 5).
- **Plates held by companions:** a companion ordered to wait on a plate holds it, and a downed or retreated creature doesn't. Test: `CompanionWaitingOnAPlateHoldsIt` (Task 5).
- **Missing the festival:** if you sleep through day 10, the festival returns on day 17 (and every 7 days) until the quest is done. Each game is once per player per festival day, and a festival can't start mid-combat in town. Test: `FestivalComesBackIfYouMissIt` (Task 7).

---

### Task 1: The Fallen Star's new stages, save migration, map chars

**Files:** `Assets/Scripts/Game/Quests/QuestDb.cs`, `Core/SaveSystem.cs`, `Core/GameRoot.cs` (HatchEgg, CompleteFallenStar, load spawn), `Dialogue/DialogueDb.cs` (Rhoswen), `World/Zone.cs`, `World/Maps.cs` (legend), tests `Assets/Tests/PlayMode/PlacesTests.cs` (new), `SmokeTests.cs` (HatchTheEggPeacefully stage numbers)

**Interfaces (produces):**
```csharp
public static class FallenStar { public const int Crater = 1, Trail = 2, Warrens = 3, Egg = 4, Hatched = 5, Done = 6; }
public class SaveData { public int Version = 2; ... }        // v1 → v2 remaps fallen_star (1→3, 2→4, 3→5, 4→6)
static void SaveSystem.Migrate(SaveData d);                    // called right after FromJson, before applying
```
- New fallen_star `StageText` (7 entries). 1: "Captain Vale asked you to find where the star fell, in the hills past Whisperwood's north trail." 2: "The crater is empty. Something carried the star away on small clawed feet, and things keep going missing in town. Follow the tracks, or catch the thief in the act after dark." 3: "The trail leads into the old copper mine and down, to the kobolds' warrens." 4–6 keep the old texts for stages 2–4. Every call site uses the `FallenStar` constants (GameRoot `HatchEgg` → `Hatched`, `CompleteFallenStar` → `Done`, Rhoswen's `s < 4` / `s >= 4` → `Hatched`/`Done`).
- Triggers: `SecretFound crater_hollow` Crater→Trail; `SecretFound crater_tracks` Trail→Warrens; `ZoneEntered warrens` from Crater and from Trail → Warrens; `GroupResolved mine_egg` from 0, Crater, Trail and Warrens → Egg.
- Rhoswen's intro and accept lines now point to the hills past Whisperwood's north trail, not the mine.
- Load: if the saved tile isn't walkable in the current map, the player spawns at `DefaultSpawn`.
- New map chars (legend in Maps.cs, handled in `Zone.BuildCell`, and the solid ones added to `LineOfSight`): `d` scorched earth (ground), `e` glowing crystal cluster (solid, glow), `H` kobold hut (solid prop), `i` brazier (solid, glow), `V` stairs down (ground, `tile_stairs_down`), `Q` pressure plate, `I` lever, `J` push block, `Z` puzzle gate, `/` mirror, `@` light source, `%` light crystal (the last seven are built by Task 5's puzzle layer; until then they draw as plain floor). `FloorFor`: any `Def.Dark` zone uses `tile_cave_floor` (not just `mine`).

- [ ] **Step 1: Write failing tests** in PlacesTests.cs (same SetUp/TearDown as QuestTests):
  - `OldSaveFallenStarMigrates`: a SaveData with `Version = 1` and fallen_star = 2 → after `Migrate`, stage 4 and Version 2; = 4 (done, paid) → 6, and `Set("fallen_star", 6)` pays nothing.
  - `SavedPositionOnAWallFallsBackToSpawn`: save in `mine` at a tile that is `X` in the new map → after load, the player is in `mine` at `DefaultSpawn`.
  - `NewMapCharsBuild`: a zone using every new char builds without the "no SecretDef" warning or exceptions; `e`, `H`, `i`, `Z`, `J` are blocked; `d`, `V`, `Q` are walkable.
- [ ] **Step 2:** `.superpowers/unity-test.sh PlacesTests` → FAIL.
- [ ] **Step 3:** Implement as above. Update `SmokeTests.HatchTheEggPeacefully` to expect `FallenStar.Egg` / `FallenStar.Hatched` (the zone change comes in Task 3).
- [ ] **Step 4:** `.superpowers/unity-test.sh PlacesTests` and `QuestTests` and `SmokeTests` → PASS.
- [ ] **Step 5:** Commit `"Main quest: Fallen Star journey stages, save v2 migration, new map chars"`.

### Task 2: Starfall Crater

**Files:** `World/Maps.cs` (`Crater`, Forest rows 0–1 opened at cols 30–31), `World/WorldDb.Places.cs` (new; `static partial class WorldDb` with `BuildCrater()`, and WorldDb made `partial`), `Rules/Content/MonstersDb.cs`, `Rules/Content/ItemsDb.cs`, `Dialogue/DialogueDb.Places.cs` (new), `Assets/Editor/ArtMap.txt`, tests in PlacesTests.cs

**Interfaces (produces):** zone `crater` ("Starfall Crater", ~44×30, outdoors, `Music = "music_crater"` → "22 - Dream.ogg"); warps Forest (30,0),(31,0) ↔ the crater's south edge; NPC `perrin`; monster `giant_fire_beetle`; items `star_shard`, `star_iron`.
- Layout (hand-shaped, not symmetrical): a scorched bowl of `d` with a cracked hollow at the center, crystals `e` scattered unevenly on the rim and in the bowl, a lone burnt tree, a gawkers' spot with a tent and campfire (Perrin), a cliffside overlook in one corner, a rocky goat path, and kobold tracks leaving north-east toward the mountains (and so the mine).
- Secrets: `crater_hollow` (quest spot, stage Crater, Investigation DC 12, "Study the empty hollow": a shattered "star" casing, warm, scaled; it wasn't a star at all). `crater_tracks` (quest spot, stage Trail, Survival DC 13, "Follow the little clawed tracks": they lead to the old copper mine). `crystal_shard` (Arcana 13 → `star_shard` ×1). `star_iron` (Athletics 14 → `star_iron` ×1). `crater_overlook` (Perception 12, `HeroicInspiration`, Daily: the whole valley at once).
- `giant_fire_beetle`: SRD Giant Fire Beetle (AC 13, HP 1d6+1, CR 0, XP 10, Bite +1, 1d6−1 fire; trait "Illumination", text only).
- Gawkers besides Perrin: add afternoon schedule entries (14:00–17:00) so Tobin turns up on Leafday and Maddie plus Wick on Coinday, each with one crater-only line. Group `crater_beetles` (3 beetles, Beasts, TalkDC 11, ScareDC 10), drawn to the crystals.
- `perrin`, Perrin Quillby, "Starfall Academy scholar", at the crater 8:00–19:00, at the Drake 20:00 (tavern, a table), away otherwise. He's nervous, thrilled and wrong about everything. He hints at the hollow (stage Crater) and at "small footprints, possibly a very short bear" (stage Trail), and foreshadows that the Academy will want whatever fell. Insight 12 shows he's been sent to report back.
- `star_shard`: trinket, 10 gp, glows faintly. `star_iron`: trinket, 25 gp ("Garrick would love this").

- [ ] **Step 1: Write failing tests:** `CraterIsReachableFromWhisperwood` (warp forest (30,0) → in crater; a path from the crater spawn to every secret tile and Perrin's spot; warp back). `CraterHollowAndTracksAdvanceTheStar` (Start fallen_star; resolve `crater_hollow` success → Trail; `crater_tracks` → Warrens; neither spot is present at another stage).
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. Add ArtMap lines for the beetle, crystals, tent and scorched tiles (pick pack frames; hand-draw anything the pack lacks in PlaceholderArt). Run `.superpowers/import-art.sh`.
- [ ] **Step 4:** `PlacesTests` + `SmokeTests.WorldDataIsConsistent` → PASS.
- [ ] **Step 5:** Commit `"Places: Starfall Crater, Perrin, fire beetles, crater secrets"`.

### Task 3: The Kobold Warrens (mine level 1, village level 2, egg shrine level 3)

**Files:** `World/Maps.cs` (`Mine` egg chamber, new `Warrens`, `EggShrine`), `World/WorldDb.cs` (mine), `WorldDb.Places.cs` (`BuildWarrens()`), `WorldDb.cs` (`SecretDef.PassageTo`, `EnemyGroupDef.VouchFlag/VouchText`), `World/Interactables.cs`, `Core/GameRoot.cs` (`OnEnemyNoticed`), `UI/Screens.cs` (shop `kobold`), `MonstersDb.cs`, `DialogueDb.Places.cs`, tests

**Interfaces (produces):**
```csharp
// SecretDef
public Vector2Int? PassageTo;   // success moves the player (and their companions) to this tile in the same zone; once found, no roll again
// EnemyGroupDef
public string VouchFlag;        // if World.Flag(VouchFlag) when they spot you: they stand down (PacifyGroup, full XP), VouchText is narrated
public string VouchText;
```
- `mine`: the egg chamber (top center room) keeps its shape but loses the `g`; `V` at (19,2) warps to `warrens`. `mine.EggPos` is removed. Every other mine group and chest stays where it is.
- `warrens` ("The Kobold Warrens", Dark, `music_warrens` → "26 - Lost Village.ogg", ~44×32): `E` up to the mine; the **gate** (group `warrens_gate`: 2 kobolds + 1 slinger, Talk 13/Scare 13, `VouchFlag = "snik_friend"`); a **crack** beside the gate (`warrens_crack`, Acrobatics DC 12, `PassageTo` inside the village: the sneak route); the **village cavern** (huts `H`, braziers `i`, a mushroom farm `k`, an underground stream with a plank bridge); **Elder Ssark** (NPC `ssark`, by the biggest hut); **Tik's Shiny Stall** (NPC `tik`, `ShopId = "kobold"`: buys trinkets at full price, sells torches, rope, a healing potion and a "kobold good-luck pebble" trinket); **Krag's warband** blocking the way down (group `warrens_warband`: `kobold_warboss` + 2 kobolds, Talk 14/Scare 15, `VouchFlag = "elder_blessing"`); a **crawlway** around the warband (`warrens_crawl`, Athletics DC 13, `PassageTo` beside the `V`); a chest of kobold "treasures" (`warrens_hoard`: 1000 cp, 2 copper trinkets, 1 healing potion). `V` → `egg_shrine`.
- `egg_shrine` ("Shrine of the Great Egg", Dark, `music_shrine` → "3 - Revelation.ogg", ~26×18): `E` up; a hall of offerings (the kettle-and-spoon gag as signs/props); altar `A`; the egg `g` (`EggPos`); group `mine_egg` moved here unchanged (Snik + 2 kobolds, parley "snik"); secret `offering_pile` (Investigation 11: copper + "four very content chickens in a pen, wearing tiny crowns").
- `kobold_warboss` (Krag): AC 14, 5d6+5 HP (≈22), CR 1/2, XP 100, spear +4 1d6+2 piercing, trait "Pack Tactics" (text only, as the kobold has). Kobold-sized.
- `OnEnemyNoticed`: before the parley/encounter checks, if `g.VouchFlag != null && World.Flag(g.VouchFlag)` → narrate `VouchText`, `PacifyGroup(g.Id, p)`, return.
- Elder Ssark's dialogue: if `go_between` ≥ 2 (Snik vouched), a warm talk gives `elder_blessing`. Otherwise you can still earn it: Persuasion DC 15, Insight DC 12 (she's scared of the "tall ones' Academy") which unlocks a Persuasion at advantage, or giving her a shiny (any `copper_trinket` or `star_shard`, removed from the bag). She is harder (DC +2) if `warrens_gate` was defeated rather than pacified. You can always sneak (the crawl) or fight Krag instead.

- [ ] **Step 1: Write failing tests:**
  - `WarrensLevelsConnect`: mine `V` → warrens, warrens `V` → egg_shrine, both `E`s lead back; a path from each arrival tile to the next stairs exists (ignoring groups).
  - `MainQuestCanSkipTheCraterAndSnik`: Start fallen_star (Crater); warp to the warrens → stage Warrens; `PacifyGroup("mine_egg")` → Egg; `HatchEgg` → Hatched.
  - `CrackPassageMovesYouOnce`: resolve `warrens_crack` success → player on `PassageTo`; interact again from the gate side → moved with no dice roll.
  - `VouchedGroupStandsDown`: set `snik_friend`; `OnEnemyNoticed` for a `warrens_gate` member → group pacified, its full XP paid, no dialogue pushed.
  - `ElderBlessingByShiny`: give a `copper_trinket` through her dialogue → `elder_blessing` set, trinket gone.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. Move `SmokeTests.HatchTheEggPeacefully` to warp into `egg_shrine`. Add ArtMap lines (stairs, hut, brazier, Ssark/Tik/Krag looks: recolored kobold sprites are fine).
- [ ] **Step 4:** `PlacesTests`, `SmokeTests`, `QuestTests` → PASS.
- [ ] **Step 5:** Commit `"Places: Kobold Warrens village and egg shrine, passages, vouching"`.

### Task 4: Snik the go-between (quest 17) and the night stakeout

**Files:** `WorldDb.cs` (`SecretDef.Dialogue`), `Interactables.cs`, `WorldDb.cs` town secrets, `QuestDb.cs`, `DialogueDb.Places.cs` (`snik_caught`, Ssark), `DialogueDb.cs` (Snik's `start` selector), tests

**Interfaces (produces):** `SecretDef.Dialogue` (string: on success, push `new DialogueScreen(Dialogue, ...)` instead of the result message). Quest `go_between`, "Snik Speaks for You", Giver null; stages: "", "Snik agreed to speak for you in the kobold warrens below the old mine. Meet him at the village gate.", "Snik talked the gate guards round. Now he needs Elder Ssark's blessing.", "Complete. The Elder blessed you; the way to the Great Egg is open." Triggers: `GroupResolved warrens_gate` 1→2; `FlagSet elder_blessing` 1→3 and 2→3. Reward 150 XP, Snik's good-luck pebble to the finisher.
- Town quest spot `snik_stakeout` at the market stalls (a walkable tile next to an `m`): `QuestId fallen_star`, `QuestStage = Trail`, `NightOnly`, Perception DC 13, "Keep watch on the market stalls". Success → dialogue `snik_caught`. Failure: "Something small clatters away over the rooftops. Try again tomorrow night", and the crater tracks still work, so it never soft-locks.
- `snik_caught`: Snik, caught in the act holding Pip's best ladle, explains (badly) that the Great Egg needs shinies to keep warm. Peaceful (Persuasion 12, Insight 11 → advantage, Animal Handling 14 as a joke option, or simply "Put the ladle back and talk") → `snik_friend` flag, `go_between` starts, fallen_star → Warrens, and the ladle goes back to Pip (flag `ladle_returned`). Intimidation 12 → he flees, drops the ladle, fallen_star → Warrens, no go-between. "Grab him" (Athletics 13): success = the peaceful branch with him sulking; failure = he flees.
- Snik's egg parley: the `start` selector returns a new `friend` node when `go_between` is done. Snik is overjoyed, introduces you to the egg, and `PacifyGroup("mine_egg")`. All the old nodes stay for everyone else.

- [ ] **Step 1: Write failing tests:**
  - `StakeoutOnlyAtNightOnTheTrail`: the spot is absent by day and at other stages, and present at 22:00 with fallen_star = Trail.
  - `BefriendingSnikOpensThePeacefulRoad`: resolve the stakeout and pick the peaceful option (drive `DialogueScreen` as the Ford tests do) → `snik_friend`, go_between 1, fallen_star Warrens. Gate noticed → pacified → go_between 2. Ssark talk → 3 and XP paid once. Snik parley → `friend` → `mine_egg` pacified.
  - `ScaringSnikStillAdvances`: the Intimidation branch → fallen_star Warrens, go_between 0.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `PlacesTests` + `QuestTests.EveryQuestIsWellFormed` + `EveryQuestCanStart` → PASS.
- [ ] **Step 5:** Commit `"Quests: Snik the go-between, night stakeout, Elder Ssark"`.

### Task 5: Puzzle layer

**Files:** `World/Puzzle.cs` (new: defs + `PuzzleLogic` + `PuzzleRoom` MonoBehaviour + piece interactables), `WorldDb.cs` (`ZoneDef.Puzzle`), `Zone.cs` (build pieces), `Core/SaveSystem.cs` (latched gates are world flags, so nothing new), `ArtMap.txt`, tests `Assets/Tests/PlayMode/PuzzleTests.cs`

**Interfaces (produces):**
```csharp
public enum PieceKind { Plate, Lever, Block, Mirror, Gate, Light, Crystal }
public class PuzzlePiece { public string Id; public PieceKind Kind; public Vector2Int Pos;
  public string[] OpensWhen;   // Gate: all must be "on"; "!id" = must be off
  public bool Latches;         // Gate: once open, stays open (flag "puzzle:{zone}:{id}")
  public int Dir;              // Light: 0=N 1=E 2=S 3=W.  Mirror: 0 = '/', 1 = '\'
  public string Hint; public Rules.Skill HintSkill; public int HintDC; }  // optional, on Levers/Mirrors: a dice-tray check reveals Hint
public class PuzzleDef { public List<PuzzlePiece> Pieces = new(); }
public static class PuzzleLogic {
  // Beam from start heading dir; mirrors reflect ('/' N↔E, S↔W; '\' N↔W, S↔E); stops at a solid tile or after 64 steps.
  public static List<Vector2Int> TraceBeam(Vector2Int start, int dir, Func<Vector2Int,bool> solid, IReadOnlyDictionary<Vector2Int,int> mirrors);
  public static bool GateOpen(PuzzlePiece gate, Func<string,bool> isOn);
}
public class PuzzleRoom : MonoBehaviour { public bool IsOn(string id); public void Refresh(); public void ResetUnlatched(); }
```
- Plates are on while a player, companion, enemy or Block stands on them. They're checked each Refresh, which runs on any piece change and every 0.2 s while a player is in the zone. A Lever toggles on Interact (a clunk sound). A Block moves one tile away from the pusher on Interact (no roll; a blocked push says "It won't budge that way") only onto walkable floor with no creature, warp, door, gate or other block. A Mirror toggles `/`↔`\` on Interact. A Crystal is on while the beam's last tile is next to it or on it. A Gate is solid while closed (blocks movement and sight) and opens with the door sound.
- The beam is drawn as a line of `fx_beam` sprites along `TraceBeam`'s tiles, updated on Refresh.
- `ResetUnlatched` runs when the last player leaves the zone: blocks, levers and mirrors go back to their map state, and latched gates stay open.

- [ ] **Step 1: Write failing tests** (PuzzleTests):
  - `BeamReflectsOffMirrors` (pure logic): a 5×5 grid, light at (0,2) heading E, `/` at (2,2) → the beam turns N and its tiles end at (2,0); flip to `\` → it turns S.
  - `GateNeedsAllConditions` (pure): `OpensWhen = {"a","!b"}` → open only when a is on and b is off.
  - `BlockOnlyMovesOntoFreeFloor`: in a test zone, pushing toward a wall, a companion or a gate does nothing; pushing toward open floor moves it one tile, and onto a plate turns the plate on.
  - `CompanionWaitingOnAPlateHoldsIt`: a companion on a plate with its wait order → the plate is on; the companion is downed → off.
  - `PuzzleResetsOnReentryButLatchedGatesStayOpen`: open a latched gate, move a block, leave and re-enter → the block is back and the gate is still open (the flag is saved through a save/load round trip).
- [ ] **Step 2:** `.superpowers/unity-test.sh PuzzleTests` → FAIL.
- [ ] **Step 3:** Implement. Test zones may be registered by the test (`WorldDb.Zones["test_puzzle"] = ...`) and removed in TearDown. Add ArtMap/PlaceholderArt for `obj_plate`, `obj_plate_down`, `obj_lever`, `obj_lever_on`, `obj_push_block`, `obj_mirror_a`, `obj_mirror_b`, `obj_light_window`, `obj_light_crystal`, `obj_light_crystal_lit`, `obj_gate`, `fx_beam`.
- [ ] **Step 4:** `PuzzleTests` → PASS.
- [ ] **Step 5:** Commit `"World: puzzle layer (plates, levers, blocks, mirrors, light beam, gates)"`.

### Task 6: The Old Watchtower (quest 13)

**Files:** `Maps.cs` (`Watchtower`, Ford row 0 opened at cols 36–37), `WorldDb.Places.cs` (`BuildWatchtower()`), `QuestDb.cs`, `DialogueDb.cs` (Rhoswen jobs), `DialogueDb.Places.cs` (`watch_armor`), `MonstersDb.cs`, `ItemsDb.cs`, tests in PuzzleTests.cs

**Interfaces (produces):** zone `watchtower` ("The Old Watchtower", outdoors ruin, not Dark, `music_watchtower` → "30 - Ruins.ogg", ~32×26), warps Ford (36,0),(37,0) ↔ its south edge. Quest `watchtower`, "The Watchtower Inscription", Giver `rhoswen`: "", "Captain Vale asked you to scout the Old Watchtower on the ridge north of Miller's Ford.", "You read the inscription at the top of the tower. Tell Captain Vale what it says.", "Complete. Captain Vale has copied the words into her notebook. Twice." Triggers: `SecretFound tower_inscription` 0→2 and 1→2. Reward 200 XP, 2000 cp, Rhoswen +50. Rhoswen offers it in her `jobs` node once fallen_star ≥ Trail, and her stage-2 line completes it (she goes quiet at the words).
- Layout: an overgrown courtyard with a broken wall and a lone sentry statue, then three rooms climbing the tower, each a puzzle (all gates `Latches`):
  1. **The three fires** (levers): 3 levers; the gate opens on `{"lever_1","!lever_2","lever_3"}`. A worn plaque `Hint` (History DC 12): "The watch lit the first fire and the last, and let the middle sleep." Trial and error also works.
  2. **The heavy floor** (plates): 2 plates on opposite sides; the gate opens on both. One push block in the room, so solo players use the block, and in co-op the second player or a waiting companion can stand on the other plate.
  3. **The sunlit stair** (light): a `Light` from a broken window heading S, 2 mirrors and a `Crystal` beside the stair gate; the gate opens on the crystal. Mirror `Hint` (Investigation DC 13) "Scratches on the floor show where the light once fell."
  - **The lantern room** (top): the inscription `tower_inscription` (a non-quest-gated secret, History DC 10 so everyone can read it. Success text = the First Ideal: "*Life before death. The watch keeps the light so others may sleep.*" Lore: an Oathbound order kept this tower); a chest `tower_chest` with `watchwarden_cloak` and 2 potions of healing; and the **last watchman** (group `tower_armor`: 1 `animated_armor`, `ParleyDialogue = "watch_armor"`, Unique, Style None).
- `watch_armor`: the armor creaks to life and demands the watchword. Options: recite the First Ideal (History DC 10, or no roll if `secret:tower_inscription` is set: "you read it on the wall"), Religion 13 ("you serve the light too"), Persuasion 15, or fight. Peaceful → `PacifyGroup` (full XP) and the armor kneels and goes still.
- `animated_armor`: SRD Animated Armor, AC 18, 6d8+6 HP, CR 1, XP 200, one Slam +4 1d6+2 bludgeoning per turn (no multiattack in the engine: note it in Description like the owlbear), immune to poison and psychic (only if the engine supports immunities; otherwise as text).
- `watchwarden_cloak`: Cloak of Protection, `Wondrous(... WornSlot.Cloak, 500, true, ..., ac: 1, save: 1)`, lore "Worn by the tower's last watch. Still smells, very faintly, of lamp oil."

- [ ] **Step 1: Write failing tests:** `WatchtowerSolvesInOrder`: warp to the courtyard, then for each room set the solution state (levers via `Interact`, a block pushed onto plate A plus the player on plate B, mirrors flipped to the solution) → its gate opens; a path to the next room exists only after it does. `ArmorStandsDownToTheWatchword`: with `secret:tower_inscription` set, the watchword option ends peacefully, the XP equals `animated_armor` XP, and the watchtower quest goes to 2. `WatchtowerReachableFromTheFord`.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement; ArtMap lines (the armor: a pack knight/statue sprite or hand-drawn; ruin tiles).
- [ ] **Step 4:** `PuzzleTests`, `PlacesTests`, `QuestTests.EveryQuestIsWellFormed`, `SmokeTests.WorldDataIsConsistent` → PASS.
- [ ] **Step 5:** Commit `"Places: the Old Watchtower puzzle ruin, the last watchman, quest 13"`.

### Task 7: The Harvest Festival (quest 14)

**Files:** `World/Festival.cs` (new: `Festival` static + `FestivalGame` interactable), `WorldDb.cs` (town festival props/spots, festival schedule entries for every town NPC), `WorldDb.Current` (festival rule), `Core/GameRoot.cs` (hour tick → lock, quest start), `Core/WorldState.cs` + `SaveSystem.cs` (`FestivalDoneDay`), `Core/Music.cs` (town plays `music_festival` → "31 - Sunny.ogg" while it runs), `QuestDb.cs` (+ `QuestTrigger.FlagsAtLeast`), `DialogueDb.Places.cs` (`pie_judging`, Pip's festival lines), `ItemsDb.cs`, `Zone.cs` (festival props toggle), tests `Assets/Tests/PlayMode/FestivalTests.cs`

**Interfaces (produces):**
```csharp
public static class Festival {
  public const int FirstDay = 10, Repeat = 7, OpenHour = 9, CloseHour = 22, AnnounceDay = 6;
  public static bool IsFestivalDay(int day, int doneDay);   // day == 10, or day > 10 && (day-10) % 7 == 0 && (doneDay == 0 || day == doneDay)
  public static bool IsOn(GameClock c, int doneDay);         // festival day and OpenHour <= hour < CloseHour
}
public enum FestivalKind { AppleBob, Archery, PieJudging, BardStage }
public class FestivalGame : Interactable { public FestivalKind Kind; }   // only Available while the "festival" lock runs; once per player per day (lock "fest:{kind}:{p.Index}")
public static QuestTrigger QuestTrigger.FlagsAtLeast(int from, int to, int n, params string[] flags);
```
- GameRoot, on every hour change: if `Festival.IsOn` and no town combat is running → `World.Lock("festival", minutes until CloseHour)` and narrate once ("Bunting, fiddles and the smell of pie: the Harvest Festival has begun in the square!"). From `AnnounceDay` on, start quest `harvest_festival` if it's at 0 (toast + a notice-board line).
- `WorldDb.Current`: an entry with `RequiresLock == "festival"` wins over every other entry while the lock runs. Every town NPC gets one at a spot around the square (Pip at the cider stall, Brannoc judging, Hesper and Dunstan at rival pie tables, Maddie at the apple barrel, Rhoswen at the archery butts, Lark and Tamsin dancing).
- Festival props (bunting, pie tables, an apple barrel, archery butts, a small stage) are listed in `ZoneDef.FestivalProps` (`List<(Vector2Int, string)>`). The zone shows/hides them as the lock starts/ends; they sit on walkable square tiles and never block the bridge or doors.
- Games (each one is dice-tray rolls; `XP` comes from the quest, not per game):
  - **Apple bobbing:** three Constitution checks vs DC 10, 12, 14; 2+ apples = win.
  - **Archery:** three attack rolls with a borrowed shortbow (DEX mod + proficiency if the character is proficient with simple weapons) vs AC 12, 14, 16; a natural 20 = bullseye (narrated); 2+ hits = win.
  - **Pie judging:** dialogue `pie_judging`. Insight DC 13 notices Dunstan's pie uses Hesper's honey. Persuasion DC 12 declares a diplomatic tie. Either success or failure ends the contest with a funny line (and a callback if `bad_blood` is done).
  - **Bard stage:** one Performance check; tips: 20+ = 10 gp, 15+ = 5 gp, 10+ = 2 gp, else 5 sp and polite applause.
  - Win → `festival_ribbon` (trinket, 1 sp: "a blue ribbon. It says WINNER in Maddie's handwriting"). Each game sets `fest_bob` / `fest_archery` / `fest_pie` / `fest_stage` on first play, win or lose.
- Quest `harvest_festival`, "The Harvest Festival", Giver `pip`: "", "Pip says the Harvest Festival is on Day 10 in the square, from nine bells. Try the games!", "Complete. You ate too much pie. Everyone did." Trigger `FlagsAtLeast(1, 2, 3, fest_bob, fest_archery, fest_pie, fest_stage)`. Reward 150 XP, 1000 cp, Pip +50. On completion `FestivalDoneDay = Clock.Day`.

- [ ] **Step 1: Write failing tests** (FestivalTests):
  - `FestivalDaysAndHours` (pure): days 10/17/24 are festival days, 11 and 16 aren't; with doneDay 10, day 17 isn't; `IsOn` is false at 8:00 and at 22:00, true at 9:00 on day 10.
  - `FestivalComesBackIfYouMissIt`: Clock day 9 → `SleepUntil` past day 10 (never awake during it) → on day 17 at 9:00 the lock runs and the games are Available.
  - `FestivalFillsTheSquare`: on festival hours, `WorldDb.Current(pip, 14, weekday, world)` is the festival entry; the props are active; the games are present; the town music id is `music_festival`.
  - `GamesOncePerPlayerAndQuestCompletesAtThree`: play three games (stub the dice with fixed results as the brawl test does) → the quest completes, XP is paid once; a fourth game still plays; replaying a game the same day says "come back next festival".
  - `ArcheryUsesAttackRolls`: the bonus shown in the dice-tray subtitle equals DEX mod + proficiency for a fighter and DEX mod only for a wizard who lacks simple-weapon proficiency (if wizards have it in this engine, assert that instead: read it from `PlayerCharacter`).
- [ ] **Step 2:** `.superpowers/unity-test.sh FestivalTests` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `FestivalTests`, `QuestTests` (schedules: `MarketDayFillsTheSquare`, `EveryNpcHasAWeeklyVariation`) → PASS.
- [ ] **Step 5:** Commit `"Festival: Harvest Festival day, square dressing, four mini-games, quest 14"`.

### Task 8: Phase 5 acceptance

**Files:** `Assets/Tests/PlayMode/ArtTests.cs` (`Phase5ArtIsReal`), `QuestTests.cs` (walkthrough fuzz), `ScreenshotTests.cs`, docs/PLAYTEST.md, docs/DESIGN.md, docs/LORE_PITCH.md, build

- [ ] **Step 1:** Add `Phase5ArtIsReal` (every new sprite id from Tasks 2–7: real pack art or listed as `CodeDrawn` hand art). Extend `QuestWalkthroughFuzz` with `("crater", spawn)`, `("warrens", gate)`, `("egg_shrine", hall)`, `("watchtower", courtyard)` and `("town", square)` on festival day 10 at 14:00. Add screenshot shots: the crater, the kobold village, the light puzzle mid-solve and the festival square.
- [ ] **Step 2:** Run the full play-mode suite in the background (`.superpowers/unity-test.sh`) plus `dotnet test tools/RulesTests` → all green. Fix whatever fails.
- [ ] **Step 3:** Review the screenshots in `%TEMP%/embers_screenshots`: no placeholder art, nothing covering the HUD, the beam is visible, the square isn't blocked by props.
- [ ] **Step 4:** Docs: PLAYTEST.md gets a Phase 5 checklist (crater → stakeout → warrens all three ways → shrine; watchtower solo and co-op; festival on day 10, with "sleep to day 10" tips). DESIGN.md: puzzle layer, passages, vouching, festival. LORE_PITCH.md: pending names + the First Ideal wording.
- [ ] **Step 5:** Self-review the branch diff against this plan and the spec (or the reviewer the user picked). Fix anything found and rerun the affected tests.
- [ ] **Step 6:** Rebuild the exe + zip (`Embers > Build Windows` / `-executeMethod BuildTools.BuildWindows`). Commit `"Phase 5 acceptance: art, fuzz, screenshots, docs"`. Ask the user before merging to main.
