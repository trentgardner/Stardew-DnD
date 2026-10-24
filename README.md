# Embers of Sundermere *(working title)*

A cozy, pixel-art, couch co-op dungeon crawler built on the current tabletop RPG rules (SRD 5.2),
where **you roll the dice**. Stardew Valley's heart, with BG3's tactics.

> Proprietary. All rights reserved. See `LICENSE` and `NOTICE.md`.

## Quick links
- **Play it:** `docs/PLAYTEST.md`
- **Design decisions:** `docs/DESIGN.md`
- **Story pitch (awaiting approval):** `docs/LORE_PITCH.md`
- **Art:** `docs/ART_BRIEF.md` (give to an artist or AI) · `docs/ART_IMPORT_GUIDE.md` (how to add art)

## Project layout
```
Assets/Scripts/Rules/      Pure C# rules engine: dice, checks, combat math, characters, content (no Unity code)
Assets/Scripts/Game/       Unity layer: world, NPCs, dialogue, combat, UI, saves, input, art lookup
Assets/Editor/             Build menu (Embers ▸ Build Windows Playtest) and the art auto-importer
Assets/Tests/PlayMode/     Automated play tests, including random-input "fuzz" tests
tools/RulesTests/          Fast rules tests:  dotnet run --project tools/RulesTests
docs/                      Design, lore, art and playtest docs
```

## Building
- In Unity: menu **Embers ▸ Build Windows Playtest** → `Builds/EmbersOfSundermere/` + a zip for the laptop.
- Command line: `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.BuildWindows`

## Testing
- Rules: `dotnet run --project tools/RulesTests`
- Game: Unity **Window ▸ General ▸ Test Runner ▸ PlayMode ▸ Run All**, or
  `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml`
