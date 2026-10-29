# Phase 3: All 12 Classes and Real Level-Up Choices Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every SRD 5.2 class is playable from level 1 to level 4 with its SRD subclass at 3, and leveling up is a real set of player choices instead of auto-picks.

**Architecture:** Rules stay in the engine-free `DnD.Rules` assembly (ClassDef/FeatureDef/SubclassDef data in CharacterDb, spells in SpellsDb, resources tracked by `PlayerCharacter.ResourceMax/ResourceLeft`) and are tested by `tools/RulesTests`. Combat exposes class features as `CombatAction`s in `CombatTurns.BuildActions`, following the existing Second Wind / Bardic Inspiration pattern. A new `LevelUpScreen` (GameScreen) walks the player through each choice, replacing `CharacterSheetScreen.AutoChoices` for humans (companions keep auto-picks).

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8 console tests), Unity play-mode tests.

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md (Goals 3 and 4); Wild Shape visuals agreed in chat 2026-10-28 (see memory: sprite swaps to the pack animal with a smoke/leaf poof both ways, leaf marker, green temp-HP bar).

## Global Constraints

- Rules are 2024 PHB / SRD 5.2 as written. Dice are always visible: every new attack, save, heal and damage roll goes through the dice tray like the existing ones.
- Level cap rises from 3 to 4 (`PlayerCharacter.PlaytestLevelCap = 4`). XP thresholds unchanged.
- Subclasses (SRD): Barbarian Berserker, Druid Circle of the Land, Monk Warrior of the Open Hand, Paladin Oath of Devotion, Ranger Hunter, Rogue Thief, Sorcerer Draconic Sorcery, Warlock Fiend Patron (reflavored as an "Ember Patron" in names/flavor text only; mechanics unchanged).
- Tone: no dark/gothic flavor text (the Fiend becomes an ember-bound patron; Inflict Wounds etc. keep SRD mechanics).
- Character creation lists all 12 classes; each class's starting gear follows the SRD class table (pick option A).
- Every window closes with Esc/B and has a ✕ (existing GameScreen behavior).
- Old saves still load (new resources default to full; new fields optional in PcData).

## Review Focus

- Leveling while in combat or mid-dialogue: the level-up screen must wait until the player is free (it's offered from the character sheet, never auto-pushed over combat).
- Wild Shape at 0 HP: the beast form ends, leftover damage carries to the druid (2024 rule: you revert with your own HP; temp HP absorb first), and the sprite returns.
- Warlock Pact Magic slots refresh on a short rest; other casters' slots must not.
- Multiple uses per rest (Rage 2→3 at L3, Wild Shape 2, Channel Divinity, Focus Points = level, Sorcery Points = level, Lay on Hands pool 5×level) restore correctly on short vs long rests.
- Companions of new classes: AutoChoices and the companion combat AI must not crash on new actions (fuzz the mine fight with every class).

---

### Task 1: Level 4, ASI/feat and the level-up choice model (rules)

**Files:** `Assets/Scripts/Rules/PlayerCharacter.cs`, `Assets/Scripts/Rules/Definitions.cs`, `Assets/Scripts/Rules/Content/CharacterDb.cs` (FeatsDb section), `tools/RulesTests/Program.cs`

**Interfaces (produces):**
- `PlayerCharacter.PlaytestLevelCap = 4`.
- `class LevelUpPlan { int? HpRoll; List<string> NewSpells; List<string> NewCantrips; Skill[] Expertise; string FeatId; Ability[] AsiPicks; string Choice /* e.g. fighting style, land type, metamagic */ ; List<string> Options; }`
- `List<LevelUpChoice> PlayerCharacter.PendingChoices()` → what the next level asks for (kinds: `Hp`, `Cantrip(n)`, `Spell(n)`, `Expertise(n)`, `Feat`, `FightingStyle`, `Metamagic(n)`, `Invocation(n)`, `LandType`, `HunterPrey`, `WeaponMastery(n)`).
- `PlayerCharacter.LevelUp(LevelUpPlan plan, List<string> log)` (the old `LevelUp(int?, log)` stays as a wrapper that auto-picks, used by companions).
- Feats at 4 (SRD 5.2 general feats): `asi` (+2 one ability or +1/+1, max 20), `grappler`.

- [ ] Rules tests first: `LevelFourOffersAsiOrFeat`, `AsiCapsAtTwenty`, `CasterLevelUpAsksForNewSpells` (wizard 1→2 asks for 2 spellbook spells; cleric asks for none, it prepares), `LevelUpPlanIsAppliedExactly`. Run → FAIL.
- [ ] Implement; run `dotnet run --project tools/RulesTests` → all pass.
- [ ] Commit `"Rules: level 4, ASI/feat, explicit level-up choices"`.

### Task 2: Level-up screen

**Files:** Create `Assets/Scripts/Game/UI/LevelUpScreen.cs`; Modify `Screens.cs` (CharacterSheetScreen opens LevelUpScreen; AutoChoices only for companions), `GameRoot.AutoLevel` (companions only)

- Pages in order, one choice per page, Back/Next, Esc closes (progress is discarded until confirmed): HP (Roll the die in the dice tray / Take average N), each PendingChoice as a picker list with the rules text on the right, a summary page, Confirm.
- [ ] Play-mode test `LevelUpScreenAppliesThePlayersPicks` (drive the screen with ScriptInput: pick "Take average", pick the 2nd listed spell, confirm → assert level, HP gain, spell present) and `CompanionsStillLevelAutomatically`. Run → FAIL; implement; PASS.
- [ ] Commit `"UI: level-up screen with real choices"`.

### Task 3: Rogue and Barbarian (martial classes, no spells)

- Rogue: Expertise (2 at L1), Sneak Attack (1d6, 2d6 at L3; once per turn, finesse/ranged with advantage or an ally within 5 ft of the target), Weapon Mastery 2, Cunning Action (bonus Dash/Disengage/Hide) at 2, Steady Aim at 3, Thief: Fast Hands (bonus action: Use an Object / Sleight of Hand), Second-Story Work (climb speed; jump +DEX ft); Thieves' Cant flavor only.
- Barbarian: Rage (bonus action; uses 2, 3 at L3; +2 damage on STR attacks, resistance to B/P/S, advantage on STR checks/saves, no spells; 10 rounds), Unarmored Defense (10+DEX+CON), Weapon Mastery 2, Danger Sense and Reckless Attack at 2, Primal Knowledge at 3, Berserker: Frenzy (while raging and Reckless, first hit each turn +Xd6 where X = rage damage bonus), L4 ASI.
- [ ] Rules tests: `SneakAttackNeedsAdvantageOrAnAlly`, `SneakAttackOncePerTurn`, `RageAddsDamageAndResistance`, `RageUsesRestoreOnRests`, `UnarmoredDefenseUsesCon`, `RecklessGivesAdvantageBothWays`. Play-mode: `FightInTheMineWithEachClass` gains rogue + barbarian.
- [ ] Implement (CharacterDb, Combat.cs damage hooks, CombatTurns actions: Rage, Reckless toggle, Cunning Action group). Run all → PASS. Commit.

### Task 4: Monk and Paladin

- Monk: Martial Arts (d6; DEX for unarmed/monk weapons; bonus unarmed strike), Unarmored Defense (10+DEX+WIS), L2 Monk's Focus (Focus Points = level, SR refresh: Flurry of Blows, Patient Defense, Step of the Wind), Unarmored Movement +10 ft, Uncanny Metabolism; L3 Deflect Attacks (reaction, reduce damage 1d10+DEX+level), Open Hand Technique (Flurry hits: Addle/Push 15 ft/Topple); L4 ASI, Slow Fall.
- Paladin: Lay on Hands (pool 5×level, bonus action), Spellcasting (CHA, half caster), Weapon Mastery 2; L2 Fighting Style, Paladin's Smite (Divine Smite always prepared + 1 free cast/LR); L3 Channel Divinity (Divine Sense), Devotion: Sacred Weapon, Oath spells (Protection from Evil and Good, Shield of Faith); L4 ASI.
- Spells to add: `divine_smite`, `protection_from_evil_and_good`, `compelled_duel`, `searing_smite`, `wrathful_smite` (renamed flavor ok) (paladin L1 list subset).
- [ ] Rules tests: `MartialArtsUsesDexAndD6`, `FocusPointsRestoreOnShortRest`, `DeflectReducesDamage`, `LayOnHandsPoolIsFiveTimesLevel`, `DivineSmiteAddsRadiantDice`. Play-mode mine fight gains monk + paladin. Implement; PASS; commit.

### Task 5: Ranger and Druid (with Wild Shape)

- Ranger: Spellcasting (WIS, half), Favored Enemy (Hunter's Mark always prepared, 2 free casts/LR), Weapon Mastery 2; L2 Deft Explorer (1 Expertise), Fighting Style; L3 Hunter: Hunter's Lore, Hunter's Prey (Colossus Slayer or Horde Breaker); L4 ASI. Spells: `hunters_mark`, `ensnaring_strike`, `fog_cloud`, `goodberry`, `longstrider`, `speak_with_animals`; L2 `pass_without_trace`, `spike_growth`, `barkskin`.
- Druid: Spellcasting (WIS, full), Druidic, Primal Order (Magician: extra cantrip + WIS to Arcana/Nature; Warden: medium armor + martial weapons); L2 Wild Shape + Wild Companion; L3 Circle of the Land (Land type choice → circle spells; Land's Aid), L4 ASI + Wild Shape improvement. Spells: `druidcraft`, `produce_flame`, `shillelagh`, `thorn_whip`, `entangle`, `thunderwave`(exists), `cure_wounds`/`healing_word`/`faerie_fire` (exist, add class), L2 `moonbeam`, `flaming_sphere`.
- **Wild Shape (2024 RAW):** bonus action; 2 uses (one back on SR, all on LR); lasts level/2 hours; temp HP = druid level; beast AC/speed/STR/DEX/CON/attacks; keep INT/WIS/CHA, skills, HP; no spellcasting (concentration continues); ends at 0 HP or as a bonus action. Known forms: L2 Wolf, Boar, Cat, Frog (CR ≤ 1/4, no fly); L4 adds Black Bear (CR 1/2). Beast stat blocks added to MonstersDb (`cat`, `frog`, `black_bear`; wolf/boar exist).
- **Visuals:** the avatar's `Look` swaps to the beast's sprite id; a smoke/leaf poof (pack FX `Smoke` + leaves) plays both ways; a leaf marker shows by the HUD name; the HP bar shows temp HP in green. Exploration perks: Cat/Rat/Frog can pass "crawlspace" tiles (new map char `%`), Frog swims across 1-wide water, Wolf reveals tracks (advantage on Survival).
- [ ] Rules tests: `WildShapeGrantsTempHpAndBeastStats`, `WildShapeEndsAtZeroAndCarriesNoExtraDamageRAW`, `WildShapeUsesRestoreCorrectly`, `NoSpellcastingWhileShaped`, `HuntersMarkAddsD6ToMarkedTarget`, `ColossusSlayerOncePerTurn`. Play-mode: `DruidWildShapesAndChangesLook` (sprite id becomes the beast's real art), mine fight gains ranger + druid. Implement; PASS; commit.

### Task 6: Sorcerer and Warlock

- Sorcerer: Spellcasting (CHA, full), Innate Sorcery (2/LR; +1 spell DC and advantage on spell attacks for 1 min); L2 Font of Magic (sorcery points = level; slot↔points), Metamagic ×2 (options: Careful, Distant, Empowered, Extended, Quickened, Twinned); L3 Draconic Sorcery (Draconic Resilience: +1 HP/level, unarmored AC 10+DEX+CHA; draconic spells: `alter_self`, `chromatic_orb`, `command`(exists), `dragons_breath`); L4 ASI.
- Warlock: Pact Magic (CHA; slots refresh on SR: 1×L1 at 1, 2×L1 at 2, 2×L2 at 3–4), Eldritch Invocations (1 at L1, 3 at L2: Agonizing Blast, Armor of Shadows, Devil's Sight → "Ember Sight", Pact of the Blade/Tome/Chain(Chain = familiar), Repelling Blast); L2 Magical Cunning; L3 Fiend → "Ember Patron": Dark One's Blessing → "Ember's Blessing" (temp HP = CHA + level when you drop a hostile to 0), patron spells (`burning_hands`, `command`, `scorching_ray`, `suggestion`, all exist); L4 ASI.
- Spells to add: `eldritch_blast`, `hex`, `armor_of_agathys`, `hellish_rebuke` (flavor: "ember rebuke"), `chromatic_orb`, `witch_bolt`, `alter_self`, `dragons_breath`, `mirror_image`, `darkness`→skip (Devil's Sight dependency is flavored, keep simple).
- [ ] Rules tests: `PactSlotsRefreshOnShortRest`, `SorceryPointsConvertBothWays`, `QuickenedMakesASpellABonusAction`, `AgonizingBlastAddsCha`, `DraconicResilienceAc`. Play-mode mine fight gains sorcerer + warlock. Implement; PASS; commit.

### Task 7: Creation, companions, art hookup, acceptance

- CharacterCreationScreen lists all 12 classes with their one-line Description; recruits can be any class (CharacterFactory.Recruit uses all classes).
- `EveryClassAndSpeciesSpawns` covers 12×5. `FightInTheMineWithEachClass` covers 12. A new `EveryClassLevelsToFourThroughTheScreen` drives the LevelUpScreen to level 4 for each class.
- [ ] Run RulesTests + the full PlayMode suite; rebuild exe/zip; update docs/DESIGN.md (classes, level cap 4, Wild Shape) and docs/PLAYTEST.md (what's new); commit.
