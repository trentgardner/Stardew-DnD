# How to Put Real Art in the Game

The game looks up every image **by filename**. If a file exists, it's used; if not, the game draws its own
placeholder. So you can replace art **one sprite at a time**, in any order, without touching code.

## The 3-step version

1. Get a PNG that follows `docs/ART_BRIEF.md` (right size, transparent background).
2. Name it **exactly** as listed in the brief, e.g. `hero_fighter_human_down_0.png` or `tile_grass.png`.
3. Drop it into this folder (create it the first time):
   ```
   C:\Users\trent\Projects\ai_2D_DnD\Assets\Resources\Art\
   ```
   Then open or refocus the Unity editor (it imports automatically) and press Play. Done.

You **don't** need to change any import settings: the project automatically makes everything in that folder
crisp pixel art (16 pixels per unit, no blur, no compression, and feet-at-the-bottom pivots for characters).

## Checking your work
- Press **Play** in Unity: the new sprite appears wherever that id is used.
- Wrong size? Characters may be 16×16, 16×24 or 16×32. The game positions them by their feet, so all work.
- Blurry? The file isn't in `Assets/Resources/Art/`, or its name has a typo (names are case-sensitive: use lowercase).
- Magenta/black checkerboard? That's the "missing" placeholder: the name doesn't match any known sprite.

## Animation frames
Walking uses `_0` and `_1` frames, and characters use `down`, `up` and `side` (facing right; the game flips it for left):
```
hero_bard_elf_down_0.png   hero_bard_elf_down_1.png
hero_bard_elf_up_0.png     hero_bard_elf_up_1.png
hero_bard_elf_side_0.png   hero_bard_elf_side_1.png
```
If you only have one frame, save it as both `_0` and `_1`.

## Sprite sheets (packs like Kenney)
Packs usually come as one big sheet or a folder of numbered tiles (`tile_0042.png`).
The easy path: **tell Claude where the pack is** and Claude will map the right tiles to the right names
(it can look at images directly). Doing it yourself:
1. Find the tile you want in the pack.
2. Copy it into `Assets/Resources/Art/` and rename it to the id from the brief.

## Sounds work the same way
Drop `.wav` or `.ogg` files into `Assets/Resources/Audio/` with these names to replace the placeholder beeps:
`dice_shake, dice_roll, dice_land, hit, miss, crit, heal, spell, door, coin, ui, success, fail, combat_start`.

## A nicer font (optional)
Put a pixel font file at `Assets/Resources/Fonts/GameFont.ttf` and every menu will use it.

## License check before adding any art
- ✅ **CC0** (Kenney, many itch.io packs): use freely, even commercially.
- ✅ **CC-BY**: fine, but add the credit to `NOTICE.md`.
- ⚠️ **CC-BY-SA / GPL**: avoid. "Share-alike" can force you to open-source your game.
- ❌ **Non-commercial / personal use only**: avoid if you might ever sell the game.
- AI-generated art: check the tool's terms (most paid tiers grant commercial rights). Also note that in the US,
  purely AI-generated images may not be copyrightable on their own, so your human edits and choices matter.
