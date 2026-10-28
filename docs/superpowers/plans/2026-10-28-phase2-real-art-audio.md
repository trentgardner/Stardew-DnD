# Phase 2: Real Art and Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace every placeholder sprite and synthesized tone the player normally sees and hears with the Ninja Adventure pack (CC0), plus hand-authored gaps, without changing the ArtRegistry id contract.

**Architecture:** An editor importer reads a text mapping (`Assets/Editor/ArtMap.txt`: id → sheet, tile rect, pivot) and writes one PNG per id into `Assets/Resources/Art/` with point-filter/PPU16 import settings, so `ArtRegistry.Get(id)` picks real art up with no runtime change. A new engine-free `TileStyler` picks edge, corner and strip variants (`tile_path_ne`, `tile_roof_l_top`...) from a cell's same-type neighbors; `Zone.BuildCell` asks for the variant and ArtRegistry falls back to the base id when a variant has no art. Audio follows the same pattern: `Assets/Resources/Audio/{id}.ogg` if present, otherwise the current synth.

**Tech Stack:** Unity 6000.5.9f1, C#, Tilemap, Unity Test Framework (play mode), Ninja Adventure pack at `ArtSource/NinjaAdventure/Ninja Adventure - Asset Pack/` (gitignored source; the sliced PNGs/OGGs are committed).

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md (Goals 2, the Art pipeline, Anti-goals: "no Kenney or placeholder sprites left visible in normal play")

## Global Constraints

- Art stays swappable: code only ever asks ArtRegistry for ids; nothing references pack paths at runtime.
- 16 px tiles, `ArtRegistry.PixelsPerUnit = 16`, point filtering, no compression on sliced sprites.
- Tests run muted. Any logged error fails a play-mode test.
- Pack license CC0 (verified in LICENSE.txt). Add a credit line to NOTICE.md anyway ("appreciated").
- The dragon must read as *red/ember* (the hatching text says "furious red dragon"): recolor the pack's green Monster/Dragon.
- Don't run scripts from inside `ArtSource/` (untrusted download); tools take its paths as arguments.

## Review Focus

- A map character with no neighbor-specific art (e.g. a lone 1-tile path) must still draw something sensible: variant → base id → placeholder, never a missing (pink/blank) tile.
- Big props (2×2 trees, 4-wide houses) overlapping actors: y-sorting must keep the player in front of a tree trunk when standing south of it and behind it when north.
- Split-screen at 2 players: the real tiles must stay pixel-crisp (no bleeding seams between tiles at non-integer zoom). Pad the sliced sprites or use extrude if seams appear.
- Indoor maps and the dark mine: the darkness overlay and torch light still read correctly on the new, more colorful tiles.
- Old saves keep working: no save field changes in this phase.

---

### Task 1: ArtRegistry real-art bookkeeping + no-placeholder audit test

**Files:**
- Modify: `Assets/Scripts/Game/Art/ArtRegistry.cs`
- Create: `Assets/Tests/PlayMode/ArtTests.cs`

**Interfaces:**
- Produces: `public static bool ArtRegistry.IsReal(string id)` (a Resources asset exists for the id), `public static string ArtRegistry.Resolve(params string[] ids)` (the first id with real art, else the last id), `public static readonly HashSet<string> ArtRegistry.Requested` (every id asked for since `ClearCache`).
- Produces: test helper `ArtTests.PlaceholdersVisibleIn(zoneId)` → list of requested ids without real art after building that zone and spawning its actors.

- [ ] **Step 1: Failing test `EveryZoneDrawsOnlyRealArt`**: for every zone in `WorldDb.Zones`, warp a player there, wait 30 frames, and collect `Requested` ids where `!IsReal(id)` excluding `tile_none`, `tile_grid`, `tile_highlight`, `tile_cursor`, `ui_white`. Assert the list is empty, printing the ids. (Expected RED for the whole phase; it is the phase's acceptance test and goes GREEN in Task 7.)
- [ ] **Step 2: Failing test `ResolvePrefersRealArtVariants`**: `Resolve("tile_does_not_exist_ne", "tile_grass")` returns `"tile_grass"` once a `tile_grass` PNG exists (use a test PNG committed at `Assets/Resources/Art/test_real.png`: `Resolve("nope", "test_real") == "test_real"`, `IsReal("test_real")`, `!IsReal("nope")`).
- [ ] **Step 3: Implement** IsReal (cached `Resources.Load` check), Resolve, Requested.
- [ ] **Step 4: Run.** Expected: `ResolvePrefersRealArtVariants` passes; `EveryZoneDrawsOnlyRealArt` fails listing the placeholder ids (record the count in the ledger).
- [ ] **Step 5: Commit** `"Art: ArtRegistry knows which ids have real art; placeholder audit test"`.

### Task 2: Art importer + mapping file

**Files:**
- Create: `Assets/Editor/ArtImporter.cs`, `Assets/Editor/ArtMap.txt`
- Test: `Assets/Tests/PlayMode/ArtTests.cs`

**Interfaces:**
- Produces: menu `Embers/Import Art` and batch entry `ArtImporter.ImportAll()` (`-executeMethod ArtImporter.ImportAll`). It reads the pack root from `ArtSource/NinjaAdventure/Ninja Adventure - Asset Pack`.
- ArtMap.txt line formats (`#` comments; whitespace separated; paths relative to the pack root; tile units are 16 px unless `@px` is appended):
  - `tile <id> <sheet> <col> <row> [w h] [pivot=center|bottom]`: one sprite from a tile rect.
  - `actor <idPrefix> <sheet>`: a Character/Monster 4-column sheet (columns: down, up, left, right; rows: walk frames 0–3). Writes `{idPrefix}_down_{0..3}`, `{idPrefix}_up_{0..3}`, `{idPrefix}_side_{0..3}` (side = right column), pivot bottom.
  - `anim <idPrefix> <sheet> <frames>`: a horizontal strip → `{idPrefix}_{0..n-1}`.
  - `tint <id> <fromId> <hueShiftDegrees>`: a recolored copy of an already-written id (used for the red dragon).
  - `audio <id> <file>`: copies an .ogg/.wav to `Assets/Resources/Audio/{id}.{ext}`.
- Every written texture: `TextureImporter` Sprite (Single), PPU 16, FilterMode.Point, Compression None, no mipmaps, alphaIsTransparency, the pivot as specified.

- [ ] **Step 1: Failing test `ImportedSpritesArePixelPerfect`**: for ids `tile_grass`, `hero_fighter_down_0` (after Task 2's first map lines) assert `ArtRegistry.Get(id).texture.filterMode == Point`, `pixelsPerUnit == 16`, and `rect.width == 16`.
- [ ] **Step 2: Implement** the importer and the first ArtMap lines (grass, path, Knight as the fighter).
- [ ] **Step 3: Run the importer** in batch mode; confirm the PNGs and .meta files exist; run the test → PASS.
- [ ] **Step 4: Commit** `"Art: ArtMap-driven importer slices Ninja Adventure into ArtRegistry ids"`.

### Task 3: TileStyler: neighbor-aware tiles

**Files:**
- Create: `Assets/Scripts/Game/World/TileStyler.cs` (no UnityEngine types in its API)
- Modify: `Assets/Scripts/Game/World/Zone.cs` (`BuildCell` asks TileStyler for the variant id, then `ArtRegistry.Resolve(variant, baseId)`)
- Test: `Assets/Tests/PlayMode/ArtTests.cs`

**Interfaces:**
- Produces: `enum TileShape { Single, Blob, Rect }`; `static string TileStyler.Variant(string baseId, TileShape shape, Func<int,int,bool> same, int x, int y)`. `same(dx,dy)` says whether the neighbor at that offset is the same terrain (dy = +1 is SOUTH, row-major like Maps).
  - **Blob** (paths, sand, water, shallow pond, cave floor edges): suffixes `_c` (all four sides same), `_n`, `_s`, `_e`, `_w` (that side differs), `_ne`, `_nw`, `_se`, `_sw` (outer corners), `_ine`, `_inw`, `_ise`, `_isw` (all four sides same but that diagonal differs), `_v_n`, `_v_m`, `_v_s` (one-wide vertical), `_h_w`, `_h_m`, `_h_e` (one-wide horizontal), `_dot` (isolated).
  - **Rect** (roofs, house walls, counters, cliffs): `_{l|m|r}_{top|mid|bot}` from whether the left/right/up/down neighbors are the same; a 1-high rect uses `_bot`.
- Map chars → shapes: `,` path Blob; `;` sand Blob; `~` water Blob; `X` cliff Rect; `R` `U` `G` roofs Rect; `W` walls Rect; `C` counter Rect; `f` fence Rect (h/v runs); everything else Single.

- [ ] **Step 1: Failing tests** (pure logic, no art needed): `BlobPicksOuterCornerAndInnerCorner` (a 3×3 path square: the NE cell → `_ne`, the center → `_c`; a path L-shape: the elbow's inner side → `_ine` etc.), `BlobHandlesOneWideRuns` (a vertical 1-wide path: top `_v_n`, middle `_v_m`, bottom `_v_s`; isolated → `_dot`), `RectPicksRoofPieces` (a 5×3 roof: (0,0) `_l_top`, (2,1) `_m_mid`, (4,2) `_r_bot`).
- [ ] **Step 2: Run → FAIL** (TileStyler missing).
- [ ] **Step 3: Implement** TileStyler and use it in Zone.BuildCell (also for the ground layer under props, so a tree on a path still gets the path edge).
- [ ] **Step 4: Run** the new tests plus the full suite → PASS (placeholders still render via fallback; WorldDataIsConsistent etc. unchanged).
- [ ] **Step 5: Commit** `"World: neighbor-aware tile variants (blob and rect autotiling) with fallback"`.

### Task 4: Terrain and buildings mapped

**Files:**
- Modify: `Assets/Editor/ArtMap.txt`; generated `Assets/Resources/Art/tile_*.png`
- Modify (only if a map char needs a new look): `Assets/Scripts/Game/World/Zone.cs` FloorFor/BuildCell

Map from the pack (the builder chooses the exact cells by viewing the sheets with a zoomed grid preview):
- Grass/forest grass/flowers/crops/mushrooms/reeds: TilesetFloor (green blocks rows 11–12), TilesetNature small plants, TilesetField.
- Path (dirt on grass, Blob): TilesetFloor rows 7–10 (3×3 at cols 0–2, strips at col 3 and row 10, inner corners at cols 4–8).
- Sand (Blob): TilesetFloor rows 0–3 (orange). Cobbles: TilesetFloorB. Water (Blob): TilesetWater green-bank block rows 6–10. Bridge: TilesetWater rows 12–16.
- Roofs R/U/G (Rect): TilesetHouse rows 0–1 (orange, beige, red). Walls/windows/doors (Rect + Single): TilesetHouse row 2 and row 3 pieces.
- Cliff X (Rect): TilesetRelief (green for outdoors, brown for the mine). Cave floor and walls: TilesetDungeon / TilesetFloor brown block.
- Interiors: wood floor (TilesetWater planks rows 12–15 or TilesetFloor), rug, counter, bed (tileset_bed), table/crate/barrel/chest/altar/sign/lantern/well/stall/statue (TilesetElement, TilesetHouse, TilesetNature).
- Trees and bushes: TilesetNature 2×2 trees with pivot=bottom, drawn as props over the solid cell.

- [ ] **Step 1: Write the ArtMap lines** for every `tile_*`/`obj_*` id the zones request (the Task 1 audit prints the list).
- [ ] **Step 2: Import; run `ScreenshotTests`; view every zone screenshot** in `%TEMP%/embers_screenshots`. Fix seams, wrong pieces, or ugly joins (adjust the map lines, or add a Rect/Blob case) until each zone looks hand-made.
- [ ] **Step 3: Run the full suite → PASS** (the audit still lists actor/item ids).
- [ ] **Step 4: Commit** `"Art: real terrain, buildings and props from Ninja Adventure"`.

### Task 5: Characters, monsters, NPCs, the dragon

**Files:**
- Modify: `Assets/Editor/ArtMap.txt`; `Assets/Scripts/Game/World/Actors.cs` (walk cycles use 4 frames when `{look}_{dir}_3` has real art, else the current 2)
- Test: `Assets/Tests/PlayMode/ArtTests.cs`

**Interfaces:**
- Consumes: the `actor` lines from Task 2; `ArtRegistry.IsReal`.

Casting **[Claude's call; easy to swap in ArtMap]**: fighter Knight; wizard SorcererOrange; cleric Monk; bard Noble or Villager; other species/class looks for recruits from the remaining characters; Brannoc Master; Elowen OldWoman; Pip Boy or Inspector; Rhoswen KnightGold; Hobb OldMan; Wren Hunter; villagers Villager1–6/Woman; bandits NinjaDark/Masked; kobolds Monster Lizard/Reptile (Snik = a distinct recolor); goblins CamouflageGreen; rats Mouse; wolves Animal Dog recolored grey (no wolf in the pack); boar Animal WildBoar; dragonling Monster Dragon tinted red; the egg and the egg glow hand-authored 16×16.

- [ ] **Step 1: Failing test `WalkCycleUsesFourFramesWhenAvailable`**: spawn a fighter, hold move for 60 frames, and record the sprite names seen; assert `hero_fighter_side_2` or `_down_2` appears (only possible with the 4-frame path).
- [ ] **Step 2: Implement** 4-frame cycling; write the actor ArtMap lines plus the tint line; import.
- [ ] **Step 3: Run the tests; review the screenshots** (town NPCs, a mine fight, the hatched dragon).
- [ ] **Step 4: Commit** `"Art: real characters, monsters and a red baby dragon; 4-frame walk cycles"`.

### Task 6: Items, UI icons, FX

**Files:** `Assets/Editor/ArtMap.txt` (item_* from Items/*, ui_die stays Claude-drawn per the dice-color pillar, ui_panel/ui_slot from Ui/Theme if they 9-slice cleanly. Otherwise keep the current panels.)

- [ ] **Step 1: Map every `item_*` id in ItemsDb** (weapons → Items/Weapons, potions → Items/Potion, scrolls, food → rations, treasure → gems/coins, egg shard hand-authored).
- [ ] **Step 2: Add a test `EveryItemHasRealArt`** (iterate ItemsDb icons: `IsReal`) → run → PASS after the import.
- [ ] **Step 3: Commit** `"Art: real item icons"`.

### Task 7: Music and sound

**Files:**
- Modify: `Assets/Scripts/Game/Core/Sfx.cs` (play `Resources/Audio/sfx_{name}` when present, else synth); Create: `Assets/Scripts/Game/Core/Music.cs` (one looping AudioSource; `Music.Play(string trackId, float fade = 1f)`; zone → track table in ZoneDef `Music` field; combat → `music_fight`; resumes the zone track after)
- Modify: `Assets/Scripts/Game/World/WorldDb.cs` (Music per zone), `GameRoot` (on warp, combat start/end, title)
- Modify: the Settings page: `Music volume` row (0–100% in 20% steps, like Sound volume)
- Test: `Assets/Tests/PlayMode/AudioTests.cs`

Track picks **[Claude's call]**: title "38 - Intro", town "36 - Village" (day) / "27 - Chill" (night), forest "11 - Clearing", ford "23 - Road", mine "2 - The Cave", temple "12 - Temple", tavern "20 - Good Time", home "5 - Peaceful", fights "17 - Fight", hatching jingle from Audio/Jingles.

- [ ] **Step 1: Failing tests** `ZoneChangeSwitchesMusic` (warp to forest → `Music.Current == "music_forest"`), `CombatPlaysFightMusicThenReturns`, `MutedTestsStayMuted` (`AudioListener.volume == 0` while music plays).
- [ ] **Step 2: Implement; import the audio lines; run → PASS.**
- [ ] **Step 3: Commit** `"Audio: real music per zone and combat, real sound effects"`.

### Task 8: Acceptance, credits, build

- [ ] **Step 1: Run `EveryZoneDrawsOnlyRealArt` → PASS**, plus the full PlayMode suite and RulesTests.
- [ ] **Step 2: Review every screenshot** (solo and split-screen); fix anything that looks broken.
- [ ] **Step 3: NOTICE.md credit**: "Art and audio: Ninja Adventure by Pixel-boy and AAA (CC0), https://pixel-boy.itch.io/ninja-adventure-asset-pack". Update docs/ART_IMPORT_GUIDE.md for the ArtMap workflow.
- [ ] **Step 4: Rebuild the exe and zip; commit** `"Art: Ninja Adventure pass complete; credits and import guide"`.
