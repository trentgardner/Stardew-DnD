# Art Brief: *Embers of Sundermere*

> Hand this whole file to an AI art tool (PixelLab, Retro Diffusion, etc.) or to a pixel artist.
> It describes the style, the exact canvas sizes, and **every sprite the game needs, by filename**.
> Files named exactly like this drop straight into the game with no code changes (see `ART_IMPORT_GUIDE.md`).

---

## 1. Style

- **Genre feel:** cozy farm-sim town + D&D dungeon crawler. Think *Stardew Valley* charm meets high fantasy
  (*Harry Potter* wonder, *A Court of Thorns and Roses* fae elegance, *Stormlight Archive* storms, and **dragons**).
- **Tone:** bright, warm, hopeful. No grimdark, gore or undead horror. Villains are more mischievous than scary.
- **View:** top-down **3/4 perspective** (like Stardew): you see the top and front of things.
- **Pixel art rules:**
  - Hard pixels: no anti-aliasing, no blur, no gradients except hand-dithered.
  - Outline color: dark plum `#2b1d24` (never pure black). Selective outlining is welcome.
  - Light comes from the **top-left**.
  - Palette: warm and soft-saturated. Greens `#3f8030 #4f9a3a #5a9e3c #6cb348`, earth `#8a5a32 #a0693a #c9a46a`,
    stone `#8a857e #9c958c`, water `#3f8fd2 #8cc8f0`, accents gold `#e8c87a`, red `#b0453a`, blue `#4a6fa5`.
    Aim for at most ~48 colors overall.
- **Readability first:** characters must be recognizable at 1x on a TV from a couch (split-screen halves!).

## 2. Sizes (important)

| Kind | Canvas | Notes |
|---|---|---|
| Ground/wall tiles (`tile_*`) | **16×16** | Must tile seamlessly with themselves. |
| Characters (`hero_*`, `ally_*`, `npc_*`) | **16×24** (16×32 also works) | Feet at the bottom center, ~2px from the bottom edge. |
| Monsters (`mon_*`) | 16×16 small, 32×32 large | Feet at the bottom center. |
| Objects (`obj_*`) | 16×16 (trees may be 32×32 or 32×48) | Bottom center sits on the tile. |
| Item icons (`item_*`) | **16×16** | Centered, readable silhouette, transparent background. |
| UI (`ui_*`) | see list | Panels are 16×16 with a 4px border (they get stretched with 9-slice). |

PNG with a transparent background. One image per file (no sprite sheets needed; sheets can be sliced later).

## 3. Animation frames and directions

Characters face 3 ways, with 2 frames each (idle/step, step):
`{name}_down_0`, `{name}_down_1`, `{name}_up_0`, `{name}_up_1`, `{name}_side_0`, `{name}_side_1`

- `side` faces **right**. The game mirrors it for left.
- `_0` = standing / left foot forward, `_1` = right foot forward (a 2-frame walk).
- Monsters: `mon_{name}_0`, `mon_{name}_1` (facing right; mirrored automatically).

---

## 4. Sprite list

### 4.1 Player heroes: `hero_{class}_{species}_{dir}_{frame}`
Classes × species = 20 looks × 6 frames = **120 sprites**.

| Class | Look |
|---|---|
| `fighter` | Steel armor (chain mail), red tabard, sturdy. Shield on arm optional. |
| `wizard` | Blue robe with gold trim, pointy blue hat, staff. |
| `cleric` | White and gold robes, sunburst holy symbol (the Dawnmother), mace. |
| `bard` | Green tunic, purple cape or sash, a lute slung on the back. |

| Species | Look |
|---|---|
| `human` | Average height, varied hair. |
| `elf` | Tall and slender, pointed ears, often pale gold hair. |
| `half_elf` | Like human with subtly pointed ears, dark hair. |
| `dwarf` | Short and broad, big braided beard (auburn), sturdy boots. |
| `gnome` | Very short, big head and eyes, wild white or pastel hair. |

Example filenames: `hero_wizard_gnome_down_0.png`, `hero_bard_half_elf_side_1.png`.

### 4.2 Recruitable companions: `ally_{class}_{species}_{dir}_{frame}`
Same 120 combinations; slightly different color schemes from the heroes so you can tell them apart
(e.g. fighter in blue tabard, wizard in purple). *Can reuse hero art to start.*

### 4.3 Townsfolk and tavern recruits: `npc_{name}_{dir}_{frame}`
| File prefix | Who |
|---|---|
| `npc_brannoc` | **Brannoc Ironkettle**, dwarf shopkeeper. Grey braided beard in 3 plaits, leather apron, pencil behind the ear. |
| `npc_elowen` | **Mother Elowen**, older human priestess. White hood and robes, gold sunburst, warm smile, flour on sleeves. |
| `npc_pip` | **Pip Thistlewhit**, gnome tavern keeper. Red hair, rolled sleeves, apron, holding a tankard. |
| `npc_rhoswen` | **Captain Rhoswen Vale**, half-elf captain of the watch. Dented breastplate, blue cloak, sword, tired eyes. |
| `npc_villager_tobin` | **Old Tobin**, elderly farmer with a hoe and straw hat. |
| `npc_villager_maddie` | **Maddie**, a kid with a stick "sword", gap-toothed grin. |
| `npc_fighter`, `npc_wizard`, `npc_cleric`, `npc_bard` | Generic tavern adventurers for hire (hooded, travel-worn versions of each class). |

### 4.4 Monsters: `mon_{name}_{frame}`
| File | Description |
|---|---|
| `mon_kobold` | Small rust-red lizard-folk, dragon-worshippers, adorable but feisty, dagger. |
| `mon_kobold_slinger` | Kobold with a sling and a pouch of rocks, browner scales. |
| `mon_snik` | **Snik the Egg-Warden**, kobold leader wearing a **saucepan as a helmet**, holding a spear with great dignity. |
| `mon_rat` | Giant rat (dog-sized), grey-brown, red eyes. |
| `mon_wolf` | Grey mountain wolf. |
| `mon_goblin` | Green goblin with a scimitar. |
| `mon_dragonling` | **The baby dragon companion.** Red-orange scales, big eyes, stubby wings, a tiny smoke puff. The emotional heart of the game: make it lovable! |

### 4.5 Ground and building tiles: `tile_{name}` (16×16, seamless)
`tile_grass`, `tile_grass_v1` (variant), `tile_flowers` (grass with wildflowers), `tile_path` (dirt road),
`tile_water` (pond, animated later), `tile_bridge`, `tile_sand`,
`tile_wood_floor`, `tile_stone_floor`, `tile_rug` (red with gold trim),
`tile_cave_floor`, `tile_cave_wall`, `tile_mine_entrance` (timber-framed dark opening), `tile_exit_up` (light coming down a ramp),
`tile_house_wall` (cream plaster with timber), `tile_window`, `tile_door`,
`tile_roof` (red shingles), `tile_roof_blue` (shop), `tile_roof_gold` (temple),
`tile_stone_wall` (temple interior), `tile_counter` (wooden shop or bar counter), `tile_fence`.

### 4.6 Objects: `obj_{name}`
`obj_tree` (round leafy tree, may be taller than 16px), `obj_bush`, `obj_rock`, `obj_lantern` (lamp post),
`obj_crate`, `obj_barrel`, `obj_table` (tavern table), `obj_altar` (golden sunburst altar),
`obj_sign` (wooden sign), `obj_bed`, `obj_chest`, `obj_chest_open`,
`obj_egg_glow` (**the dragon egg**: red scales, glowing gold from within, about pumpkin-sized).

### 4.7 Item icons: `item_{id}` (16×16)
Weapons: `item_club item_dagger item_greatclub item_handaxe item_javelin item_light_hammer item_mace item_quarterstaff item_sickle item_spear item_light_crossbow item_shortbow item_sling item_battleaxe item_flail item_glaive item_greataxe item_greatsword item_halberd item_longsword item_maul item_morningstar item_rapier item_scimitar item_shortsword item_trident item_warhammer item_war_pick item_whip item_hand_crossbow item_heavy_crossbow item_longbow`

Armor: `item_padded item_leather item_studded_leather item_hide item_chain_shirt item_scale_mail item_breastplate item_half_plate item_ring_mail item_chain_mail item_splint item_plate item_shield`

Consumables and treasure: `item_potion_healing` (red flask) `item_potion_greater_healing` (darker red) `item_ember_of_return` (glowing dragonfire coal) `item_scroll_revivify` `item_torch` `item_rations` `item_gemstone` `item_copper_trinket` `item_egg_shard` `item_copper_kettle` `item_lucky_spoon`

Magic items (make these feel special, with a subtle glow):
- `item_emberwrought_blade`: longsword with an ember-orange glowing edge.
- `item_starfall_staff`: student's staff tipped with a pale-blue star fragment.
- `item_veilthread_cloak`: fae cloak shimmering violet and silver.
- `item_oathbound_signet`: gold signet ring with a tiny storm-blue gem.
- `item_dawnmothers_charm`: gold sunburst pendant on a chain.
- `item_starfall_wand`: slim white wand with a star-blue tip.
- `item_fennicks_lucky_copper`: copper coin with a laughing face.

### 4.8 UI: `ui_{name}`
- `ui_panel`: parchment panel (16×16, 4px border, brown frame, cream inside).
- `ui_panel_dark`: dark plum translucent panel with a thin gold frame.
- `ui_slot`: inventory slot (16×16).
- `ui_die`: a **white d20 silhouette** (32×32). It's tinted in-game: gold for d20s, red for damage, green for healing.
- `ui_white`: 4×4 plain white (used for bars).

---

## 5. Priority order (if doing it in batches)
1. `hero_*` for one class + `mon_dragonling` + `obj_egg_glow`: instant vibe check.
2. Town tiles (`tile_grass`, `tile_path`, `tile_house_wall`, roofs, `tile_door`, `obj_tree`).
3. The six named NPCs.
4. Mine tiles + kobolds + Snik.
5. Item icons and UI.
6. Remaining hero/ally combinations.

## 6. Prompt template (for AI generators)
> "16x24 pixel art sprite, top-down 3/4 view like Stardew Valley, cozy high fantasy, {DESCRIPTION},
> facing {down/up/right}, {standing / mid-step}, dark plum outline #2b1d24, soft warm palette,
> light from top-left, transparent background, no anti-aliasing, crisp pixels, single character centered with feet at the bottom"

Keep the same seed/style reference across a batch so the characters match.
