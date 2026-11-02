# Act III Phase 1: Levels 11–13 for All 12 Classes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every class is playable from level 11 to level 13 under SRD 5.2 (2024), with 6th- and 7th-level spells, the warlock's Mystic Arcanum and CR 3–10 Crown-flavoured test monsters. Acts I and II keep their caps (4 and 10).

**Architecture:** This follows Act II Phase 1 exactly (docs/superpowers/plans/2026-10-31-act2-phase1-levels.md). Rules data lives in the engine-free `DnD.Rules` assembly:
- `CharacterDb.Levels.cs`: features by level (add `AddLevelElevenToThirteen()` beside `AddLevelFiveToTen()`)
- `SpellsDb`: spells; `Effects.cs`: effects
- `PlayerCharacter.ResourceMax`: per-rest resources
- `PendingChoices`: level-up choices

`CharacterDb.Table(...)` repeats its last value up to level 20, so class tables don't crash past 10. They do need correct values at 11–13. Rules are tested by `tools/RulesTests`. Combat exposes features through `CombatTurns.BuildActions`. The level cap becomes 13 in Act III. Phase 2 flips it at the act transition; tests and `DebugStart(level:)` flip it now.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (`dotnet run --project tools/RulesTests`), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite takes ~20 min, so run it in the background).

**Spec:** docs/superpowers/specs/2026-11-02-act3-design.md (Goal 8: levels 11–13 for all 12 classes following SRD 5.2, 6th–7th level spells, cap 10 → 13, XP rules unchanged; the dragon-power rule).

---

## Act III roadmap (each phase gets its own plan when it starts)

1. **Levels 11–13** (this plan).
2. **Into Act III.** The act transition after the Warden (the cap goes to 13), "Start at Act III" (level-11 pregen party plus default flags from a chosen Act II side), the grown dragon (new stage, sprite, stats at about 50–75% of the party's martial damage per round, enforced by a rules test), Act III save migration, and a codex "Read again" framework (entries with a `RevealText` shown only after a flag).
3. **The capital.** Hand-shaped districts inside the Wyrmback ring: warm lamps while the realm went grey, propaganda (statues, rhymes, a parade), side quests, shops, secrets, Buoy's Act III spot. The clues downward. Nothing before the door hints she is a child (the secret guard is extended).
4. **The lair under the capital.** Foundations → vaults → royal crypts → the old prison (wards, chains, the Iron Ring's iron, guards who don't know what they guard), the Third Ideal, and the door. The meeting: quiet, no boss music, the girl, the dragon goes to her first. The "Read again" reframes switch on.
5. **The turn.** Back up into the capital. The questioning scene with every force present (real dialogue and dice), the force-reaction table from Act II state (`banner_<force>`, `fae_clause`, etc.), the three-way choice, the save-path gate that says what's missing.
6. **The finales.** The fight through the capital streets (several stages), the cinematic flight with the "Who rides {dragon}?" prompt, three boss fights (the terror form, the sealing defence, the Crown taking the fire) with story beats at phase changes that always play.
7. **The endings.** Ending scenes per path (her, the dragon, companions, faction leaders, Thistledown, Buoy), epilogue slides from choices that end on a "did we do right?" line, credits, XP pacing to 13 near the turn, the whole-act golden path for all three endings, the balance table, the build and docs.

---

## Global Constraints

- Rules are 2024 PHB / SRD 5.2 as written. Dice are always visible: every new attack, save, heal and damage roll goes through the dice tray.
- **Level cap by act:** `PlayerCharacter.ActThreeLevelCap = 13`. Act I = 4 and Act II = 10 stay unchanged. `GameRoot.ActTwo.cs`'s cap line becomes Act-aware (`World.Act >= 3 ? 13 : World.Act >= 2 ? 10 : 4`).
- **XP thresholds (SRD):** level 11 = 85000, 12 = 100000, 13 = 120000. Append them to `PlayerCharacter.XpThresholds`; the HUD and sheet already read its length.
- **Subclasses stay the SRD ones** (see the Act II Phase 1 plan). Renamed flavour stays: the Fiend is the "Ember Patron".
- **Curated spells, not the full SRD lists.** Leave out spells the engine can't express in a 2D grid fight: summons, Teleport, Plane Shift, Etherealness, Reverse Gravity, Wish-likes, Simulacrum, Contingency. List the omissions in DESIGN.md.
- Tone: necromancy-school spells that get in keep SRD mechanics with neutral names (Circle of Death → "Withering Circle", Finger of Death → "Final Word").
- Old saves still load: every new field is optional in `PcData`, and new resources default to full.
- Every window closes with Esc/B and has a ✕. A level-up is offered from the character sheet and never pushed over combat.
- **No Act III story text in this phase.** Nothing anywhere mentions the Queen's nature. Monster names are generic ("Crown Knight" is fine: the Crown already exists in Act II).

## Review Focus

- **An Act II save at level 10 with banked XP.** When the cap rises to 13, leveling goes one level at a time (10→11→12→13), and no choices are skipped. Test: `CapRiseToThirteenLevelsOneAtATime` (Task 1).
- **The fighter's third attack.** Three attacks per Attack action must survive retargeting after kills, Action Surge (six attacks) and opening a menu between attacks. Test: play-mode `ThreeAttacksSurviveRetargetAndActionSurge` (Task 2).
- **Mystic Arcanum is not a slot.** It must never be upcast, refunded by Magical Cunning or spent through Pact slots, and it refreshes only on a long rest. Test: `MysticArcanumIsOncePerLongRestAndNotASlot` (Task 4).
- **Sixth- and seventh-level slots for full casters.** Upcasting 5th-level spells into 6th and 7th slots scales only where the spell defines it. Divine Intervention still caps at spell level 5. Test: `UpcastIntoSeventhScalesOnlyWhenDefined` (Task 3).
- **Companion AI at 11–13.** It must answer every new choice and must not cast Disintegrate on an ally, Fire Storm onto allies, or Heal on a full-HP target. Covered by `AutoPlanAnswersEveryChoiceToThirteen` (Task 1) and `CompanionAiNeverTargetsAlliesWithNewAreas` (Task 5).

---

### Task 1: Cap 13, XP to 13, class tables at 11–13, and the 11–13 features of the martial classes

**Files:** `Assets/Scripts/Rules/PlayerCharacter.cs`, `Assets/Scripts/Rules/LevelUp.cs`, `Assets/Scripts/Rules/Content/CharacterDb.cs` (tables), `Assets/Scripts/Rules/Content/CharacterDb.Levels.cs` (new `AddLevelElevenToThirteen()`, called after `AddLevelFiveToTen()`), `Assets/Scripts/Rules/Combat.cs`, `Assets/Scripts/Game/Core/GameRoot.ActTwo.cs`, `Assets/Scripts/Game/Core/GameRoot.cs` (DebugStart: `level > 10` raises the cap to 13), `tools/RulesTests/LevelThirteenTests.cs` (new)

**Interfaces (produces):**
- `public const int ActThreeLevelCap = 13;` and `XpThresholds` runs to index 13 (`85000, 100000, 120000`).
- **Prepared spells (2024 tables), values for 11, 12, 13:**
  - Bard, cleric, druid, wizard: 16, 16, 17
  - Sorcerer: 16, 16, 17
  - Warlock: 11, 11, 12
  - Paladin, ranger: 10, 10, 11
  - Cantrips are unchanged from level 10.
- **Features (SRD 5.2, HasFeature names in quotes):**
  - **Fighter:**
    - 11: "Two Extra Attacks" (`AttacksPerAction` is already 3)
    - 12: ASI
    - 13: "Studied Attacks": when you miss with an attack roll, your next attack roll against that creature before the end of your next turn has Advantage. Track it as the effect `studied:<targetId>`. Indomitable becomes 2 uses (already in `ResourceMax`).
  - **Barbarian:**
    - 11: "Relentless Rage": while raging, dropping to 0 HP instead triggers a DC 10 CON save. On a success, HP becomes 2 × barbarian level. The DC rises by 5 per use and resets to 10 on a short or long rest. Store the DC in a `ResourcesUsed["relentless_dc_steps"]` counter, cleared on rests.
    - 12: ASI (rage uses become 5, already in `ResourceMax`)
    - 13: "Improved Brutal Strike": two more Brutal Strike riders, Staggering Blow (target has Disadvantage on its next save and can't make Opportunity Attacks until your next turn starts) and Sundering Blow (the next attack by another creature against the target gets +5 to hit).
  - **Rogue:**
    - 11: "Improved Cunning Strike": up to two Cunning Strike effects per Sneak Attack, each paying its die cost. Sneak Attack becomes 6d6 (formula already right).
    - 12: ASI
    - 13: Thief "Use Magic Device": you can use any spell scroll, and a charged item's charge is not spent on a d6 roll of 6.
  - **Monk:**
    - 11: Open Hand "Fleet Step": when you take a Bonus Action other than Step of the Wind, you can also use Step of the Wind. Martial Arts die is d10 (already right).
    - 12: ASI
    - 13: "Deflect Energy": Deflect Attacks works against any damage type, not only B/P/S.
  - **Paladin:**
    - 11: "Radiant Strikes": melee weapon and unarmed hits deal +1d8 radiant. Channel Divinity becomes 3 (already right).
    - 12: ASI
    - 13: Devotion oath spells `freedom_of_movement` and `guardian_of_faith`, via `Subclass.LaterSpells[13]`.
  - **Ranger:**
    - 11: Hunter "Superior Hunter's Prey": once per turn, when your Hunter's Mark damage hits a creature, deal that bonus damage to a second creature within 30 ft of it.
    - 12: ASI
    - 13: "Relentless Hunter": damage can't break your concentration on Hunter's Mark. Favored Enemy uses become 5 (already right).
- `AsiLevel` already returns true at 12. Confirm it with a test.
- `PendingChoices` adds no new choice kinds for martials. `AutoPlan()` must answer everything to 13.

- [ ] **Step 1: Write the failing rules tests** in `LevelThirteenTests.cs`. Each test that raises `LevelCap` resets it in `finally`.
  - `LevelCapThirteenAndXpTable`: `XpThresholds[11..13] == 85000, 100000, 120000`; at cap 10 with XP 120000 → `CanLevelUp` false; at cap 13 → levels to 13.
  - `CapRiseToThirteenLevelsOneAtATime`: from 10 with XP 120000, each `LevelUp` adds exactly one level and offers that level's choices.
  - `AutoPlanAnswersEveryChoiceToThirteen`: all 12 classes, 1→13 with AutoPlan, no exception, every `PendingChoices` pick count is met.
  - `AsiAtTwelveForEveryClass`: every class is asked at 12.
  - `PreparedCountsAtThirteen`: cleric 17, wizard 17, warlock 12, paladin 11.
  - `StudiedAttacksGivesAdvantageAfterAMiss`: force a miss, then the next attack roll against the same target has Advantage; against another target it doesn't.
  - `RelentlessRageDcRisesAndResetsOnRest`: first drop DC 10, second DC 15, after a short rest DC 10; a success sets HP to 2 × level.
  - `ImprovedCunningStrikeAllowsTwoEffects`: two riders spend two dice (6d6 → 4d6).
  - `RadiantStrikesAddsD8OnMeleeHitOnly`: a melee hit adds a 1d8 radiant term; a ranged hit doesn't.
  - `SuperiorHuntersPreySpillsOncePerTurn`
  - `DeflectEnergyWorksOnFire`: a level-13 monk can deflect fire damage; a level-12 monk can't.
- [ ] **Step 2: Run** `dotnet run --project tools/RulesTests`. Expected: the new tests FAIL; the 1034 existing tests pass.
- [ ] **Step 3: Implement** the cap, the XP table, the table values, the features and their combat hooks. Combat hooks go in `Combat.cs` where Brutal Strike, Cunning Strike, Deflect Attacks, Hunter's Mark and the rage drop-to-0 are resolved now. Make the GameRoot cap line Act-aware.
- [ ] **Step 4: Run** the rules tests. Expected: all pass.
- [ ] **Step 5: Commit** `"Rules: level cap 13 (Act III), XP to 13, martial classes to 13"`.

### Task 2: Three attacks in play and the martials' new combat options

**Files:** `Assets/Scripts/Game/Combat/CombatTurns.cs`, `Assets/Tests/PlayMode/ActThreeLevelTests.cs` (new)

- Brutal Strike's rider picker lists Staggering Blow and Sundering Blow at 13.
- The Cunning Strike picker allows a second effect at 11 (two toggles, with the die cost shown).
- Fleet Step: after another bonus action, a "Step of the Wind (Fleet Step)" button appears.
- Use Magic Device: scrolls of any class are usable (the item's Use action checks it).
- The HUD shows "Attack n/3".

- [ ] **Step 1: Write the play-mode tests:**
  - `ThreeAttacksSurviveRetargetAndActionSurge`: a level-11 fighter vs 4 wolves. Kill A with attack 1, then attacks 2–3 can target B; the HUD reads "Attack 2/3", then "3/3". Action Surge restores 3 more. Opening and closing the Items pop-up between attacks keeps the count.
  - `ImprovedCunningStrikeOffersTwoToggles`: a level-11 rogue's damage step shows two effect slots.
- [ ] **Step 2: Run** `.superpowers/unity-test.sh ActThreeLevelTests`. Expected: FAIL.
- [ ] **Step 3: Implement** the UI hooks above in `CombatTurns.BuildActions` and the damage-step picker.
- [ ] **Step 4: Run** the filtered tests. Expected: PASS.
- [ ] **Step 5: Commit** `"Combat: three attacks, Improved Cunning/Brutal Strike, Fleet Step in play"`.

### Task 3: Curated 6th- and 7th-level spells (plus the 4th-level gaps)

**Files:** `Assets/Scripts/Rules/Content/SpellsDb.cs`, `Assets/Scripts/Rules/Effects.cs`, `Assets/Scripts/Rules/Zones.cs` (if a zone kind is needed), `Assets/Scripts/Game/Combat/CombatTurns.cs` (targeting for the new shapes), `tools/RulesTests/SpellTests7.cs` (new)

**Spell list.** Use SRD 5.2 values; class letters are the SpellsDb constants.
- **Level 4 gaps** (the ranger and the Devotion paladin need them at 13):
  - `freedom_of_movement` (B,C,D,R: 1 hour; ignores difficult terrain; Speed can't be reduced; can't be Paralyzed or Restrained)
  - `stoneskin` (D,R,S,W: concentration; resistance to bludgeoning, piercing and slashing)
- **Level 6:**
  - `chain_lightning` (S,W: one target, DEX save, 10d8 lightning, half on a success; then up to 3 more within 30 ft of it, same save; +1 extra target per slot level above 6)
  - `disintegrate` (S,W: ranged spell, DEX save; on a fail 10d6+40 force, +3d6 per slot; a creature dropped to 0 becomes dust (flavour line only); no damage on a success)
  - `sunbeam` (D,S,W: 60-ft line, CON save, 6d8 radiant and Blinded until your next turn, half on a success; concentration; repeat the line as a Magic action each turn; re-castable like `call_lightning`)
  - `heal` (C,D: 70 HP, +10 per slot; ends Blinded, Deafened and Poisoned)
  - `blade_barrier` (C: wall of 20 tiles along a line or ring (use the line zone), DEX save, 6d10 force, half on a success; enter or end a turn = save; concentration)
  - `heroes_feast` (B,C,D: out of combat, takes the long rest: the whole party gains +2d10 HP max and HP, immunity to Poisoned and Frightened until the next long rest; costs 1000 gp → **[Claude's call]** 100 gp, shown on cast)
  - `irresistible_dance` (B,W: WIS save or Charmed and dancing, concentration: it uses all its movement dancing, has Disadvantage on DEX saves and attack rolls, attacks against it have Advantage; it repeats the save at the end of each of its turns)
  - `circle_of_death`, shown as **"Withering Circle"** (K,S,W: 60-ft sphere, CON save, 8d8 necrotic, half on a success, +2d8 per slot)
  - `eyebite` (K,S,W: concentration; each of your turns, as a Magic action, one creature within 60 ft makes a WIS save or is Frightened of you (it Dashes away on its turn); **[Claude's call]** the Panicked option only, documented)
- **Level 7:**
  - `fire_storm` (C,D,S: up to 10 tiles you choose in a 4-tile-radius area, DEX save, 7d10 fire, half on a success)
  - `delayed_blast_fireball` (S,W: 20-ft sphere, DEX save, 12d6 fire, half on a success; **[Claude's call]** no delay mechanic: it detonates at once with the base 12d6, documented)
  - `finger_of_death`, shown as **"Final Word"** (K,S,W: CON save, 7d8+30 necrotic, half on a success)
  - `forcecage` (B,K,W: a 2×2-tile cage; creatures inside can't leave and nothing passes its bars; a trapped creature can try a CHA save once to slip out by magic; concentration-free, 1 hour, i.e. the whole fight; Legendary creatures can't be caged: **[Claude's call]**, so bosses can't be locked out of their own fight)
  - `regenerate` (B,C,D: 4d8+15 HP now, then 1 HP at the start of each of its turns for the rest of the fight)
  - `resurrection` (B,C: out of combat; revives a fallen companion with no penalty; costs 1000 gp → **[Claude's call]** 250 gp)
- **Warlock Mystic Arcanum:** 6th at 11 (from `circle_of_death`, `eyebite`), 7th at 13 (from `finger_of_death`, `forcecage`). See Task 4.

**Engine support:**
- **Multi-target chain:** `SpellDef.ChainTargets` (int) and `ChainRangeFt`.
- **Chosen-tiles area:** `TargetShape.Tiles` with `MaxTiles` and `AreaFt` (radius of the allowed area), for `fire_storm`. The cursor toggles tiles; Confirm casts.
- Slot levels 6–7 exist in `SpellSlots.Full` already. The spellbook UI and character sheet show slots up to 7 (Task 5 checks the fit).

- [ ] **Step 1: Write the failing rules tests** in `SpellTests7.cs`:
  - `ChainLightningHitsUpToFourAndScales` (one primary + 3; an 8th-level slot would give 5, so test the formula `ChainTargetsAt(slot)`)
  - `DisintegrateDealsNothingOnSave`
  - `HealRestoresSeventyAndCleansesBlindPoisoned`
  - `UpcastIntoSeventhScalesOnlyWhenDefined` (`cone_of_cold` in a 7th slot rolls 10d8; `hold_monster` in a 7th is unchanged; Divine Intervention offers no spell above 5)
  - `CircleOfDeathIsShownAsWitheringCircle` and `FingerOfDeathIsShownAsFinalWord` (`Name` checks; the ids stay SRD)
  - `ForcecageRefusesLegendaryCreatures`
  - `RegenerateHealsEachTurnStart`
  - `SpellListsCuratedToSeven`: every caster class lists at least 2 spells at each level it can cast at 1–13. Ranger and paladin get at least 2 at level 4. Warlock Arcanum pools have 2 each.
- [ ] **Step 2: Run** the rules tests. Expected: FAIL.
- [ ] **Step 3: Implement** the spells, effects, `ChainTargets` and `TargetShape.Tiles`.
- [ ] **Step 4: Write the play-mode test** `FireStormTogglesTilesAndSparesUnchosen` in `ActThreeLevelTests.cs`: a level-13 druid picks 3 tiles; a wolf on a picked tile takes damage and an ally on an unpicked adjacent tile doesn't.
- [ ] **Step 5: Run** the rules tests and `.superpowers/unity-test.sh ActThreeLevelTests`. Expected: PASS.
- [ ] **Step 6: Commit** `"Spells: curated 6th-7th level spells, chain targets, chosen-tile areas"`.

### Task 4: Casters 11–13 (cleric, druid, bard, sorcerer, warlock, wizard) and Mystic Arcanum

**Files:** `Assets/Scripts/Rules/ArcaneClasses.cs`, `Assets/Scripts/Rules/NatureClasses.cs`, `Assets/Scripts/Rules/LevelUp.cs`, `Assets/Scripts/Rules/PlayerCharacter.cs` (+ `PcData` field), `Assets/Scripts/Rules/Content/CharacterDb.Levels.cs`, `Assets/Scripts/Game/Combat/CombatTurns.cs` (Arcanum cast buttons), `tools/RulesTests/CasterTests13.cs` (new)

**Interfaces (produces):**
- **SRD 5.2 casters get no class features at 11 and 13.** Their 11–13 is ASI at 12 plus 6th/7th-level slots (full casters) and new prepared spells. Add a visible feature line at 11 so the level-up screen shows something:
  - cleric "Sixth-Level Miracles"
  - druid "Deep Magic"
  - bard "Sixth-Level Songs"
  - sorcerer "Sixth-Level Sorcery"
  - wizard "Sixth-Level Spells"
  Each line's text is "You can now cast 6th-level spells; 7th-level at 13." Flavour only; no HasFeature check depends on it.
- **Warlock:** "Mystic Arcanum" at 11 (6th) and 13 (7th). Invocations become 8 at 12 (already in `InvocationsKnownAt`). Pact slots become 3 at 11 (already in `SpellSlots`).
  - `public List<string> PlayerCharacter.MysticArcanum` (saved in `PcData.MysticArcanum`, optional).
  - New `ChoiceKind.Arcanum`; `PendingChoices` offers 1 pick from the curated pool at 11 (spell level 6) and at 13 (spell level 7).
  - Resources `"arcanum_6"` and `"arcanum_7"`: 1 each, long rest only.
  - `public bool PlayerCharacter.CastArcanum(string spellId)` spends the matching resource, casts at exactly the spell's level, and never touches Pact slots. `MaxSpellLevelAt` stays 5 for the pact.
- `AutoPlan()` answers `Arcanum` with the first option.

- [ ] **Step 1: Write the failing rules tests** in `CasterTests13.cs`:
  - `FullCastersGetSixthAtElevenSeventhAtThirteen`: `SlotsMax(6)` is 1 at 11 for the wizard, `SlotsMax(7)` is 1 at 13, and `MaxSpellLevelAt(13) == 7`. The paladin's `MaxSpellLevelAt(13) == 4`.
  - `MysticArcanumIsOncePerLongRestAndNotASlot`: cast `circle_of_death` via Arcanum. Pact slots are unchanged. A second cast fails. A short rest and Magical Cunning don't restore it; a long rest does. It can't be cast through `CastSpell` with a pact slot.
  - `ArcanumChoiceOfferedAtElevenAndThirteenOnly`
  - `OldSaveWithoutArcanumLoads`: a `PcData` JSON without the field loads with an empty list.
- [ ] **Step 2: Run** the rules tests. Expected: FAIL.
- [ ] **Step 3: Implement** the features, the Arcanum choice, the resources and the save field. In combat, Arcanum spells appear in the Spells ▴ pop-up with an "Arcanum (1/long rest)" tag.
- [ ] **Step 4: Run** the rules tests. Expected: PASS.
- [ ] **Step 5: Commit** `"Classes: casters to 13, warlock Mystic Arcanum"`.

### Task 5: Level-up screen, sheet and companion AI for levels 11–13

**Files:** `Assets/Scripts/Game/UI/LevelUpScreen.cs`, `Assets/Scripts/Game/UI/Screens.cs` (character sheet, spellbook), `Assets/Scripts/Game/Combat/CombatTurns.cs` (companion AI), `Assets/Tests/PlayMode/ActThreeLevelTests.cs`

- The level-up screen handles `ChoiceKind.Arcanum` (titled "Mystic Arcanum: one Nth-level spell, once per Long Rest").
- The character sheet and spellbook show slots up to 7 and features up to 13 without overflowing at 1280×720 or in split screen.
- **Companion AI:**
  - `chain_lightning` when 2+ enemies are in its chain range
  - `disintegrate` and `finger_of_death` on the enemy with the most current HP, never an ally
  - `fire_storm` picks only tiles with enemies and no allies
  - `heal` and `regenerate` only on allies under 50% HP
  - `forcecage` never on a group containing an ally, never on legendary creatures
  - the Arcanum spells are used when the fight has a Unique or Legendary enemy, or 3+ enemies
  - Relentless Rage and Radiant Strikes are passive
- `DebugStart(level: 11..13)` raises the cap to 13 and levels with AutoPlan.

- [ ] **Step 1: Write the play-mode tests:**
  - `LevelUpScreenOffersMysticArcanum`
  - `CharacterSheetFitsAtLevelThirteenInSplitScreen` (screenshot `%TEMP%/embers_screenshots/act3_sheet_l13.png`; the panel rect is inside the viewport)
  - `CompanionAiNeverTargetsAlliesWithNewAreas`: a level-13 sorcerer companion with `fire_storm` and `chain_lightning` fights 3 wolves next to the player, over 5 seeds. The player never takes spell damage from the companion.
- [ ] **Step 2: Run** `.superpowers/unity-test.sh ActThreeLevelTests`. Expected: FAIL.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run** the filtered tests. Expected: PASS. Look at the screenshot.
- [ ] **Step 5: Commit** `"UI: level-up and sheet to 13; companion AI for 6th-7th level spells"`.

### Task 6: Crown-flavoured test monsters (CR 3–10)

**Files:** `Assets/Scripts/Rules/Content/MonstersDb.cs`, `Assets/Scripts/Game/Art/ArtMap*` or the existing ArtRegistry mapping (pack sprites where they fit), `tools/RulesTests/MonsterTests13.cs` (new)

SRD 5.2 stat blocks with generic names; Act III phases reuse them:
- `crown_knight` (Knight, CR 3: Multiattack Longsword ×2, Parry reaction +2 AC vs one melee hit)
- `crown_champion` (Gladiator, CR 5: Multiattack Spear ×3, Parry)
- `crown_battlemage` (Mage, CR 6: Arcane Burst ×3 ranged; abilities: Fireball as a recharge-5 `MonsterAbilityDef` 20-ft sphere 8d6 DEX DC 14; no Counterspell)
- `stone_guardian` (Stone Golem, CR 10: Multiattack Slam ×2; Slow ability recharge 5, as a 10-ft-radius WIS-save effect applying `slow`; immune to poison and psychic; Magic Resistance as Advantage on saves against spells)
- `iron_sentinel` (Animated Armor, CR 1: fodder for the vaults, Multiattack Slam ×2, immune to poison and psychic)

**Steps:**
- [ ] **Step 1: Write the failing rules tests:**
  - `ParryRaisesAcOncePerRound`
  - `BattlemageUsesFireballWhenTwoTargetsInArea` (the existing recharge AI)
  - `StoneGuardianAdvantageOnSpellSaves`
  - `NewMonstersHaveArtIds` (every new monster's `SpriteId` resolves in the art map)
- [ ] **Step 2: Run** the rules tests. Expected: FAIL.
- [ ] **Step 3: Implement.** Parry is a once-per-round reaction like the monk's Deflect: add `MonsterDef.ParryBonus`.
- [ ] **Step 4: Run.** Expected: PASS.
- [ ] **Step 5: Commit** `"Monsters: Crown knights, champions, battlemages, stone guardians, iron sentinels"`.

### Task 7: Acceptance: fights at 11 and 13, balance, docs, build

- [ ] **Step 1: Play-mode test** `FightAtLevelThirteenWithEachClass` (plus a parameterised L11 case) in `ActThreeLevelTests.cs`:
  - Each class, with one AI companion of a different class, fights a medium encounter at that level (L11: 4 crown knights + 1 battlemage; L13: 1 stone guardian + 2 crown champions).
  - The AI drives the player too, over 3 seeds each.
  - No exceptions, the fight ends, and the log has no "unknown action".
- [ ] **Step 2: Extend `BalanceTests`** to levels 11–13: no martial (or the warlock) deals less than half the strongest martial's expected damage per round against AC 17. Print the table into the balance section of `docs/DESIGN.md`.
- [ ] **Step 3: Docs:**
  - `docs/PLAYTEST.md`: a "Levels 11–13 (Act III preview)" section
  - `docs/DESIGN.md`: the curated 6th–7th-level spells, renames, simplifications and omissions
  - `docs/LORE_PITCH.md`: the new monster names
- [ ] **Step 4: Run** the full rules suite and the full play-mode suite in the background. Expected: all green.
- [ ] **Step 5: Rebuild** the exe and zip (`-executeMethod BuildTools.BuildWindows`).
- [ ] **Step 6: Self-review** the branch diff against this plan and the spec, and fix what it finds (with tests).
- [ ] **Step 7: Commit** `"Act III Phase 1: levels 11-13 acceptance, balance table, docs"`. Merge `act3/phase1-levels` into main and push (the user pre-approved pushing after the full suite passes).
