# Act II Phase 1: Levels 5–10 for All 12 Classes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every class is playable from level 5 to level 10 under SRD 5.2 (2024), with real level-up choices, spells up to 5th level, Extra Attack, and monsters with Multiattack that can test them. Act I still caps at level 4.

**Architecture:** This follows Phase 3 exactly. Rules data lives in the engine-free `DnD.Rules` assembly:
- `CharacterDb`: features by level
- `SpellsDb`: spells
- `EffectLibrary` (Effects.cs): spell effects
- `PlayerCharacter.ResourceMax`: per-rest resources
- `PendingChoices`: level-up choices

Rules are tested by `tools/RulesTests`. Combat exposes features as `CombatAction`s in `CombatTurns.BuildActions` and resolves them through the dice tray. The level cap becomes an act setting: 4 in Act I, 10 once Act II starts. Phase 2 flips it at the act transition; tests and the debug start flip it now.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8 console, `dotnet run --project tools/RulesTests`), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite takes about 15 min, so run it in the background).

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md (Goal 10, Constraints, scope order: "the full SRD spell lists, replaced by a curated list per class" is the second cut).

---

## Act II roadmap (each phase gets its own plan when it starts)

1. **Levels 5–10** (this plan).
2. **Into Act II.** Covers the act transition (a season time skip; the cap goes to 10), "Start at Act II" (level-5 pregen party plus default flags), the teenage dragon (new stage, stats, sprite, the trust/distrust personality system), the faction-standing and commitment framework, the blight-staging framework, and the lore codex. Includes a save migration.
3. **The road and the Academy grounds.** New zones, Ashby, professors and their quests, lore hunting (books, murals; the legend is everywhere), restricted sections, secrets.
4. **The four forces.** 1–2 scouting quests each, then a major quest for each force that locks out the others; their locations (Crown outpost, Fae-touched glade, hunters' lodge).
5. **The main quest and the blight.** Visible blight stages in Thistledown and the known zones, the Second Ideal, and the road to the volcano. Each faction's flavour is applied.
6. **The volcano.** The zone, the boss (legendary actions, phases, lair/terrain, multi-tile), the illusion-of-peace talk with the lore-gated Insight weakness, the half-solution ending, and the Act III seed.
7. **The Act II pass.** XP pacing to level 10 at the finale, the End of Act II card, a golden-path test, a balance pass, a secret-leak text scan, the build and docs.

---

## Global Constraints

- Rules are 2024 PHB / SRD 5.2 as written. Dice are always visible: every new attack, save, heal and damage roll goes through the dice tray like the existing ones.
- **Level cap by act:** `PlayerCharacter.LevelCap` (static, default 4). Act I = 4, and `ActOneTests.NoLevelFiveInActOne` must still pass unchanged. Act II = 10. XP thresholds are unchanged (`XpThresholds` already runs to level 10).
- Subclasses stay the SRD ones: Champion, Berserker, Lore, Life, Land, Open Hand, Devotion, Hunter, Thief, Draconic, Fiend (shown as the "Ember Patron"), Evoker.
- **Curated spells, not the full SRD lists** (spec scope order). Spells the engine can't express in a 2D grid fight are left out on purpose and listed in the docs: no summons, no Polymorph, no Counterspell or Dispel Magic (monsters don't cast spells yet).
- Tone: no gothic or necromantic flavour. Necromancy-school spells that do get in keep SRD mechanics with neutral names.
- Old saves still load: every new field is optional in `PcData`, and new resources default to full.
- Every window closes with Esc/B and has a ✕. A level-up is offered from the character sheet and never pushed over combat.
- No Act II story content in this phase (no Ashen Queen text anywhere).

## Review Focus

- **Act I players can't pass level 4.** XP keeps accruing past 4 and is kept. When the cap rises, leveling happens one level at a time through the screen (4→5→6…), never skipping choices. Test: `CapRiseLevelsOneAtATime` (Task 1).
- **Extra Attack plus bonus attacks and movement.** A second attack after moving or killing the first target must let you pick a new target, and must not be lost when you open a menu. Test: `ExtraAttackSurvivesRetargetAfterKill` (Task 4, play-mode).
- **Upcasting into 4th/5th-level slots.** Every damage or heal spell scales correctly, and spells with no upcast text don't scale. Test: `UpcastScalesOnlyWhenDefined` (Task 2).
- **Monster Multiattack against reactions.** Deflect Attacks, Uncanny Dodge and Shield are once per round, not once per attack, and a creature that dies mid-Multiattack stops attacking. Test: `MultiattackStopsWhenAttackerDiesAndReactionsAreOncePerRound` (Task 3).
- **Companions at levels 5–10.** AutoPlan must answer every new choice kind, and the companion AI must not crash on new actions. Covered by `FightAtLevelTenWithEachClass` (Task 9) and `AutoPlanAnswersEveryChoiceToTen` (Task 1).

---

### Task 1: Level cap by act, ASIs at 6/8/10, Extra Attack, and spell level by caster

**Files:** `Assets/Scripts/Rules/PlayerCharacter.cs`, `Assets/Scripts/Rules/LevelUp.cs`, `Assets/Scripts/Game/Core/GameRoot.cs` (DebugStart sets the cap for tests), `tools/RulesTests/LevelUpTests.cs`

**Interfaces (produces):**
- `public static int PlayerCharacter.LevelCap = 4; public const int ActOneLevelCap = 4, ActTwoLevelCap = 10;` replaces `PlaytestLevelCap`. Update every use. `CanLevelUp` uses `LevelCap`.
- `int PlayerCharacter.MaxSpellLevelAt(int level)`, now public and following the SRD tables for every caster type:
  - Full casters: `(level+1)/2`, capped at 9.
  - Half casters: 1 at levels 1–4, 2 at 5–8, 3 at 9–12.
  - Pact: the existing `SpellSlots` pact level.
- `int PlayerCharacter.AttacksPerAction`: 2 at level 5+ for fighter, barbarian, monk, paladin and ranger; fighter 3 at 11 (not reachable yet). Warlocks with the `thirsting_blade` invocation get 2 with a pact weapon (Task 7).
- `bool PlayerCharacter.AsiLevel(int level)`: true at 4, 8 and 12+ for everyone, plus 6 for the fighter and 10 for the rogue. `PendingChoices` offers the Feat choice at every `AsiLevel` (it was `next == 4`).
- `PendingChoices` covers every SRD choice in 5–10, using the existing ChoiceKinds:
  - Bard Expertise ×2 at 9 and Magical Secrets at 10. Magical Secrets is a `Spell` choice of 2 from any class list up to the max level.
  - Rogue Expertise ×2 at 6.
  - Ranger Expertise ×2 at 9.
  - Warlock invocations at 5, 7 and 9 through `InvocationsKnownAt`. SRD counts: 1/3/3/3/5/5/6/6/7/7.
  - Sorcerer Metamagic +2 at 10.
  - Champion Additional Fighting Style at 7.
  - Wizard: 2 spellbook spells every level, as before.
- `LevelUpPlan AutoPlan()` must answer every choice kind above.

- [ ] Write the rules tests first:
  - `LevelCapIsFourUntilRaised`: XP = 64000 → level 4, `CanLevelUp` false. Then `LevelCap = 10` → levels to 10.
  - `CapRiseLevelsOneAtATime`: after the raise, `CanLevelUp` is true and each `LevelUp` adds exactly one level.
  - `AsiAtFourEightAndClassExtras`: fighter asked at 4, 6 and 8; rogue at 4, 8 and 10; wizard at 4 and 8 only.
  - `ExtraAttackAtFiveForMartials`: the five classes get 2 at 5; rogue, wizard and cleric stay at 1.
  - `MaxSpellLevelFollowsTables`: wizard 5→3, 7→4, 9→5; paladin 5→2, 9→3; warlock 5→3, 7→4, 9→5.
  - `AutoPlanAnswersEveryChoiceToTen`: all 12 classes, 1→10 with AutoPlan, no exception, and every `PendingChoices` pick count matches.
  - Reset `LevelCap = 4` after each test that raises it.
- [ ] Run `dotnet run --project tools/RulesTests`. Expected: the new tests FAIL.
- [ ] Implement. Run again. Expected: all pass (the 619 existing tests plus the new ones).
- [ ] Commit `"Rules: level cap by act (4/10), ASIs at 4/6/8/10, Extra Attack, spell levels to 5"`.

### Task 2: Curated 3rd–5th-level spells and the engine support they need

**Files:** `Assets/Scripts/Rules/Content/SpellsDb.cs`, `Assets/Scripts/Rules/Effects.cs`, `Assets/Scripts/Rules/Definitions.cs` (only if a new `SpellKind` or field is needed), `Assets/Scripts/Game/Combat/CombatTurns.cs` (zones and auras), `tools/RulesTests/SpellTests5.cs` (new)

**Spell list.** Use SRD 5.2 values; the class letters follow the SpellsDb constants:
- **Level 3:**
  - `fireball` (W,S; 20-ft sphere, 8d6 fire, DEX half, +1d6/slot)
  - `lightning_bolt` (W,S; 100×5 line, 8d6 lightning, DEX half, +1d6)
  - `spirit_guardians` (C; 15-ft aura around you, 3d8 radiant, WIS half, enemies' Speed halved, concentration, +1d8)
  - `mass_healing_word` (C,B; bonus action, up to 6 creatures, 2d4+mod, +1d4)
  - `haste` (W,S; +2 AC, doubled Speed, advantage on DEX saves, one extra Attack/Dash/Disengage/Hide/Use action; when it ends, lethargy: no action next turn)
  - `slow` (W,S; up to 6 targets in a 40-ft cube, WIS save, −2 AC/DEX saves, half Speed, no reactions, repeat save each turn)
  - `hypnotic_pattern` (W,S,B,K; 30-ft cube, WIS save or Charmed + Incapacitated, ends on damage)
  - `call_lightning` (D; 3d10 lightning, 5-ft radius, DEX half, re-cast as a bonus action each turn, concentration)
  - `fly` (W,S,K; flying 60 ft: ignores difficult terrain and ground hazards)
  - `blinding_smite` (P; +3d8 radiant on hit, Blinded with a CON repeat save)
  - `lightning_arrow` (R; replaces the attack, 4d8 lightning on hit or half on miss, plus 2d8 to creatures within 10 ft)
  - `beacon_of_hope` (C; up to 6 allies, advantage on WIS and death saves, healing maximized)
- **Level 4:**
  - `greater_invisibility` (W,S,B; Invisible while attacking, concentration)
  - `ice_storm` (W,S,D; 20-ft cylinder, 2d10 bludgeoning + 4d6 cold, DEX half, difficult terrain until your next turn)
  - `wall_of_fire` (W,S,D; a line of 6 tiles, 5d8 fire, DEX half, burns anyone who ends a turn in it, concentration)
  - `banishment` (W,S,C,K,P; CHA save, removed from the fight while concentrating; a non-native creature that stays banished for the full duration doesn't come back)
  - `death_ward` (C,P; the first drop to 0 HP becomes 1 HP, 8 hours)
  - `staggering_smite` (P; +4d6 psychic, WIS save or no reactions and disadvantage on attacks until the end of its next turn)
  - `dimension_door` (W,S,B,K; teleport yourself plus one adjacent ally up to 500 ft, meaning any visible tile)
  - `guardian_of_faith` (C; a 10-ft-radius tile guardian; first enemy entry each turn takes 20 radiant, DEX half, 60 damage total)
- **Level 5:**
  - `cone_of_cold` (W,S; 60-ft cone, 8d8 cold, CON half, +1d8)
  - `mass_cure_wounds` (C,B,D; up to 6 in a 30-ft sphere, 5d8+mod, +1d8)
  - `hold_monster` (W,S,B,K; like `hold_person`, any creature)
  - `flame_strike` (C; 10-ft cylinder, 5d6 fire + 5d6 radiant, DEX half, +1d6)
  - `greater_restoration` (C,B,D,P,R; removes Charmed, Petrified, a curse, an ability reduction, an HP-max reduction or 1 Exhaustion)
  - `banishing_smite` (P; +5d10 force; banished if it's at 50 HP or fewer)
  - `swift_quiver` (R; bonus action: two attacks with a ranged weapon each turn, concentration)
  - `synaptic_static` (W,B,S,K; 20-ft sphere, 8d6 psychic, INT half, d6 penalty on attacks and checks on a fail)
  - `raise_dead` (C,B,P; out of combat, revives a fallen companion with −4 penalties that shrink with each long rest; shown as a toast)
- Left out: `hunger_of_hadar` (tone). `revivify` already exists.
- Also give the existing L1–2 spells their SRD class lists, if any are missing for the expanded classes.

**Engine support:**
- `TargetShape.Cylinder`, treated as Sphere on the grid.
- Persistent zones: `wall_of_fire`, `guardian_of_faith` and `spirit_guardians` as an aura. Use a `CombatZone { Tiles, OwnerUnit, OnEnterOrEndTurn, Concentration }` list in `CombatInstance`, drawn as a tinted tile overlay.
- Re-castable concentration action (`call_lightning`).
- The `haste` lethargy rule.

**Steps:**
- [ ] Rules tests:
  - `FireballHitsEveryoneInRadiusHalfOnSave` (8d6, a successful DEX save halves the damage)
  - `UpcastScalesOnlyWhenDefined` (`fireball` in a 5th-level slot rolls 10d6; `haste` in a 5th-level slot is unchanged)
  - `SpiritGuardiansHurtsOnEnterAndHalvesSpeed`
  - `DeathWardStopsTheFirstDrop`
  - `HasteEndingCausesLethargy`
  - `BanishedCreatureLeavesAndReturns`
  - `SmitesAddDiceOnlyOnHit`
  - `SpellListsAreCurated`: every class lists at least 2 spells at each level it can cast in 1–10. Warlock: pact level 3 is offered at 5.
- [ ] Play-mode test `WallOfFireBurnsAtTurnEnd`: drive a combat; a wolf ending its turn in the wall takes damage, and the zone overlay is visible.
- [ ] Run → FAIL. Implement. Run rules and the filtered play-mode tests → PASS.
- [ ] Commit `"Spells: curated 3rd-5th level spells, persistent zones and auras"`.

### Task 3: Monster Multiattack and Act II test monsters

**Files:** `Assets/Scripts/Rules/Monster.cs`, `Assets/Scripts/Rules/Content/MonstersDb.cs`, `Assets/Scripts/Game/Combat/CombatTurns.cs` (monster turn), `tools/RulesTests/MonsterTests.cs` (new)

**Interfaces (produces):**
- `MonsterDef.Multiattack: List<string>`, the attack names used per Attack action (for example `{ "Beak", "Claws" }`). Empty means one attack, as today.
- `MonsterDef.Immunities: List<DamageType>`.
- **New SRD monsters**, using art from the pack where it fits and ArtRegistry ids otherwise:
  - `ogre` (CR 2)
  - `cinder_hound` (Hell Hound stats, CR 3, fire breath recharge 5–6, a neutral name)
  - `troll` (CR 5, regeneration 10 unless it took fire or acid damage)
  - `fire_elemental` (CR 5)
  - `salamander` (CR 5)
  - `ankheg` (CR 2)
- The owlbear gains its SRD Multiattack (Beak + Claws).
- Breath and recharge abilities are a `MonsterAbilityDef { Name, Shape, AreaFt, Dice, DamageType, SaveAbility, SaveDC, RechargeMin }` list. The Phase 2 dragon and the Phase 6 boss reuse it.

**Steps:**
- [ ] Rules tests:
  - `OwlbearMakesTwoAttacks`
  - `TrollRegeneratesUnlessBurned`
  - `RechargeAbilityRollsAtTurnStart` (5–6 recharges)
  - `MultiattackStopsWhenAttackerDiesAndReactionsAreOncePerRound`: a monk's Deflect Attacks triggers on the first hit only, and a monster at 0 HP doesn't finish its Multiattack.
- [ ] Implement, and update the AI so a monster uses its recharge ability when 2+ targets are in its area.
- [ ] Run rules tests → PASS. Run the mine/owlbear play-mode tests → PASS (update the owlbear balance expectations if they move; record that in the commit).
- [ ] Commit `"Monsters: Multiattack, recharge abilities, immunities, six CR 2-5 monsters"`.

### Task 4: Fighter, Barbarian, Rogue 5–10

**Features (SRD 5.2):**
- **Fighter:**
  - 5: Extra Attack, Tactical Shift (Second Wind also gives half Speed of movement with no opportunity attacks)
  - 6: ASI
  - 7: Champion Additional Fighting Style
  - 9: Indomitable (reroll a failed save with +level, 1 use per long rest), Tactical Master (swap a mastery to Push/Sap/Slow per attack)
  - 10: Champion Heroic Warrior (Heroic Inspiration at the start of a turn if you have none, in combat)
- **Barbarian:**
  - 5: Extra Attack, Fast Movement (+10 ft without heavy armor)
  - 6: Mindless Rage (immune to Charmed and Frightened while raging)
  - 7: Feral Instinct (advantage on Initiative), Instinctive Pounce (half-Speed move when entering a rage)
  - 8: ASI
  - 9: Brutal Strike (give up Reckless advantage: +1d10 and Forceful Blow or Hamstring Blow), rage damage +3
  - 10: Berserker Retaliation (reaction: attack a creature within 5 ft that damaged you)
- **Rogue:**
  - 5: Sneak Attack 3d6, Cunning Strike (spend a Sneak Attack die: Poison/Trip/Withdraw), Uncanny Dodge (reaction: halve one attack's damage)
  - 6: Expertise
  - 7: Sneak Attack 4d6, Evasion (DEX-save area effects: no damage on a success, half on a failure), Reliable Talent (a d20 under 10 counts as 10 on proficient checks)
  - 8: ASI
  - 9: Sneak Attack 5d6, Thief Supreme Sneak (Cunning Strike Stealth Attack: stay hidden)
  - 10: ASI

**Steps:**
- [ ] Rules tests:
  - `IndomitableRerollsWithLevelBonus`
  - `BrutalStrikeTradesAdvantageForDamage`
  - `SneakAttackScalesTo5d6`
  - `EvasionTakesNoneOnSaveHalfOnFail` (shared helper, reused by the Monk in Task 5)
  - `UncannyDodgeHalvesOnce`
  - `ReliableTalentFloorsAtTen`
  - `HeroicWarriorRefreshesInspiration`
- [ ] Play-mode `ExtraAttackSurvivesRetargetAfterKill`: a level-5 fighter kills wolf A with attack 1, then attack 2 can target wolf B, and the HUD shows "Attack 2/2".
- [ ] Implement:
  - CharacterDb features
  - `ResourceMax`: `indomitable` (1 at 9, 2 at 13)
  - Combat hooks: `CombatTurns` tracks `AttacksLeft` per Attack action, the Brutal Strike toggle, the Cunning Strike picker in the damage step, reactions
- [ ] Run → PASS. Commit `"Classes: Fighter, Barbarian, Rogue to level 10"`.

### Task 5: Monk, Paladin, Ranger 5–10

**Features:**
- **Monk:**
  - Martial Arts d8 at 5
  - 5: Extra Attack, Stunning Strike (1 focus point, once per turn: CON save or Stunned until your next turn; on a success, half Speed and the next attack against it has advantage)
  - 6: Empowered Strikes (force damage), Open Hand Wholeness of Body (bonus action: heal Martial Arts die + WIS, WIS-mod uses per long rest), Unarmored Movement +15
  - 7: Evasion
  - 8: ASI
  - 9: Acrobatic Movement (flavor; ignores difficult terrain on your turn)
  - 10: Heightened Focus (Flurry 3 strikes; Patient Defense and Step of the Wind gain temp HP and an ally boost), Self-Restoration (end Charmed/Frightened/Poisoned at end of turn), Unarmored Movement +20
- **Paladin:**
  - 5: Extra Attack, Faithful Steed (Find Steed is always prepared; shown as flavor only: no mount system)
  - 6: Aura of Protection (allies within 10 ft add CHA mod, minimum +1, to saves)
  - 7: Aura of Devotion (immune to Charmed in the aura)
  - 8: ASI
  - 9: Abjure Foes (Channel Divinity: WIS save or Frightened for up to CHA mod creatures within 60 ft)
  - 10: Aura of Courage (immune to Frightened in the aura)
  - Devotion oath spells: 5 (Aura of Vitality → leave out; Daylight → leave out; give `beacon_of_hope` and `mass_healing_word` instead), 9 (`death_ward` and `guardian_of_faith`)
  - **[Claude's call]** Substitutions within the curated list are documented in the docs.
- **Ranger:**
  - 5: Extra Attack
  - 6: Roving (+10 ft Speed)
  - 7: Hunter Defensive Tactics (choose Escape the Horde: opportunity attacks against you have disadvantage; or Multiattack Defense: after a creature hits you, its further attacks that turn have disadvantage). New ChoiceKind `HunterDefense`.
  - 8: ASI
  - 9: Expertise ×2
  - 10: Tireless (action: temp HP 1d8+WIS, WIS-mod uses per long rest; a short rest removes 1 Exhaustion)
  - Favored Enemy uses come from the existing table.

**Steps:**
- [ ] Rules tests:
  - `StunningStrikeOncePerTurnAndCostsFocus`
  - `AuraOfProtectionAddsChaToNearbyAllySaves` (10 ft yes, 15 ft no; the paladin at 0 HP gives no aura)
  - `AuraOfCourageBlocksFrightened`
  - `AbjureFoesFrightensUpToChaTargets`
  - `MultiattackDefenseGivesDisadvantageAfterFirstHit`
  - `TirelessGivesTempHp`
- [ ] Implement: auras are checked in `Creature.SaveBonus` and condition application through a combat-provided `IAuraSource` query, so the rules engine stays engine-free.
- [ ] Run → PASS. Commit `"Classes: Monk, Paladin, Ranger to level 10"`.

### Task 6: Cleric, Druid, Bard 5–10

**Features:**
- **Cleric:**
  - 5: Sear Undead (flavour: "Searing Light", radiant damage on Turn Undead → shown only if undead exist; none do, so it's text only)
  - 6: Life Blessed Healer (heal yourself 2 + slot level when you heal others), channel divinity 3
  - 7: Blessed Strikes (Potent Spellcasting: + WIS to cantrip damage; **[Claude's call]** not Divine Strike)
  - 8: ASI
  - 10: Divine Intervention (once per long rest, cast any cleric spell of level 5 or lower without a slot)
  - Life domain spells: 5 (`mass_healing_word`, `revivify`), 7 (`death_ward`, `guardian_of_faith`), 9 (`greater_restoration`, `mass_cure_wounds`)
- **Druid:**
  - 5: Wild Resurgence (spend a slot for a Wild Shape use, or a use for a level-1 slot once per long rest)
  - 6: Land's Natural Recovery (recover slots up to half your level, rounded up, on a short rest, once per long rest)
  - 7: Elemental Fury (Potent Spellcasting: + WIS to cantrips)
  - 8: ASI; Wild Shape CR 1 and fly forms: add beast stat blocks `dire_wolf` (CR 1) and `giant_owl` (CR 1/4, fly 60), with pack art where it exists
  - 10: Nature's Ward (immune to Poisoned; resistance from your land type)
  - Circle spells for each land type at 5, 7 and 9 from the curated list (e.g. Arid: `fireball`, `wall_of_fire`, `cone_of_cold`; Polar: `slow`, `ice_storm`, `cone_of_cold`; Temperate: `lightning_bolt`, `greater_invisibility`, `hold_monster`; Tropical: `hypnotic_pattern`, `ice_storm`, `synaptic_static`). These are curated stand-ins where the SRD spell isn't in our list; record the mapping in the docs.
- **Bard:**
  - 5: Bardic Inspiration d8, Font of Inspiration (short rest refresh; spend a slot to regain a use)
  - 6: Lore Magical Discoveries (2 spells from any class list)
  - 7: Countercharm (reaction: an ally within 30 ft rerolls a save against Charmed/Frightened with advantage)
  - 8: ASI
  - 9: Expertise ×2
  - 10: Bardic die d10, Magical Secrets

**Steps:**
- [ ] Rules tests:
  - `BardicDieGrowsAndFontRefreshesOnShortRest`
  - `MagicalSecretsOffersOtherClassLists`
  - `BlessedHealerHealsTheCleric`
  - `DivineInterventionOncePerLongRest`
  - `WildShapeCrOneAndFlyAtEight`
  - `NaturalRecoveryCapsAtHalfLevel`
  - `PotentSpellcastingAddsWisToCantrips`
- [ ] Play-mode `DruidShapesIntoAFlyingFormAtEight`: the sprite id is the owl's, and it can cross a water tile.
- [ ] Implement. Run → PASS. Commit `"Classes: Cleric, Druid, Bard to level 10"`.

### Task 7: Sorcerer, Warlock, Wizard 5–10

**Features:**
- **Sorcerer:**
  - 5: Sorcerous Restoration (regain half your level in sorcery points on a short rest, once per long rest)
  - 6: Draconic Elemental Affinity (choose a damage type; add CHA to that spell damage and gain resistance to it). New ChoiceKind `DraconicElement` (fire/cold/lightning/acid/poison).
  - 7: Sorcery Incarnate (Innate Sorcery for 2 sorcery points when you have no uses; up to 2 Metamagic per spell while it's active)
  - 8: ASI
  - 10: Metamagic +2
- **Warlock:**
  - Invocations at 5, 7 and 9. Add SRD options:
    - `thirsting_blade` (Extra Attack with the pact weapon; requires `pact_of_the_blade`, which is added to the options if it isn't there)
    - `eldritch_smite`
    - `repelling_blast` (added if it isn't already an option)
    - `devils_sight` (renamed "Ember Sight")
    - `one_with_shadows` (`gift_of_the_depths` left out)
  - 6: Fiend Dark One's Own Luck (renamed "Ember's Own Luck": add 1d10 to a check or save, CHA-mod uses per long rest)
  - 9: Contact Patron (flavor)
  - 10: Fiendish Resilience ("Ember Resilience": choose a resistance after each rest)
- **Wizard:**
  - 5: Memorize Spell (swap a prepared spell on a short rest)
  - 6: Evoker Sculpt Spells (choose 1 + spell level allies to auto-succeed and take no damage from your Evocation spells)
  - 8: ASI
  - 10: Evoker Empowered Evocation (add INT to one damage roll of a wizard Evocation spell)

**Steps:**
- [ ] Rules tests:
  - `SorcerousRestorationHalfLevelOncePerLongRest`
  - `ElementalAffinityAddsChaToMatchingDamage`
  - `ThirstingBladeGivesTwoAttacks`
  - `EmbersOwnLuckAddsD10`
  - `SculptSpellsSparesAllies` (Fireball with 2 allies inside and a sculpt count of 4: the allies take 0)
  - `EmpoweredEvocationAddsIntOnce`
- [ ] Play-mode `FireballSparesSculptedAllies`.
- [ ] Implement. Run → PASS. Commit `"Classes: Sorcerer, Warlock, Wizard to level 10"`.

### Task 8: Level-up screen, character sheet and companions for levels 5–10

**Files:** `Assets/Scripts/Game/UI/LevelUpScreen.cs`, `Screens.cs` (CharacterSheetScreen), `Assets/Scripts/Game/Combat/CombatTurns.cs` (companion AI), `Assets/Scripts/Game/Core/GameRoot.cs` (AutoLevel, DebugStart with a level argument)

- The level-up screen handles the new ChoiceKinds (`HunterDefense`, `DraconicElement`) and Magical Secrets. Long option lists (Magical Secrets: around 40 spells) scroll and are grouped by spell level, with the rules text on the right.
- The character sheet shows features up to 10 and spell slots up to level 5 without overflowing at 1280×720 or in split screen.
- **Companion AI:**
  - casts area spells only when 2+ enemies and 0 allies are in the area (or the allies are sculpted)
  - uses Haste, Spirit Guardians and the smites
  - uses Extra Attack fully
  - uses `mass_healing_word` when 2+ allies are under half HP
- `GameRoot.DebugStart(...)` gets an optional `level` that raises `LevelCap` to 10 and levels with AutoPlan, for tests.

**Steps:**
- [ ] Play-mode tests:
  - `LevelUpScreenOffersMagicalSecretsAndScrolls`
  - `CharacterSheetFitsAtLevelTenInSplitScreen` (a screenshot to `%TEMP%/embers_screenshots/act2_sheet_l10.png`, plus the panel rect inside the viewport)
  - `CompanionsLevelToTenAutomatically`
- [ ] Implement. Run → PASS. Review the screenshot visually. Commit `"UI: level-up and character sheet for levels 5-10; companion AI for new spells"`.

### Task 9: Acceptance: fights at levels 5, 7 and 10, balance, docs, build

- [ ] Play-mode test `FightAtLevelTenWithEachClass` (and the parameterised L5 and L7 cases):
  - each class, with one AI companion of a different class, fights a group of the new monsters scaled to a medium encounter at that level (L5: 2 ogres; L7: troll + cinder hound; L10: 2 trolls + fire elemental)
  - auto-play with the companion AI driving the player too, 3 seeds each
  - no exceptions, the fight ends, the log has no "unknown action"
  - Run the full suite in the background.
- [ ] Extend `BalanceTests` to levels 5–10: no martial (or the warlock) deals less than half the strongest martial's expected damage per round against AC 15. Print the table into `docs/DESIGN.md` (balance section).
- [ ] Docs:
  - `docs/PLAYTEST.md` gets a "Levels 5–10 (Act II preview)" section, covering how to try them through the debug start, if exposed
  - `docs/DESIGN.md` lists the curated spells and the substitutions/omissions
- [ ] Full rules suite plus the full play-mode suite → all green. Rebuild the exe and zip (`-executeMethod BuildTools.BuildWindows`).
- [ ] Self-review the branch diff against this plan. Merge `act2/phase1-levels` into main, then push (the user pre-approved pushing after the full suite passes).
- [ ] Commit `"Act II Phase 1: levels 5-10 acceptance, balance table, docs"`, then merge.
