# Phase 4: Quest System and Town Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Quests are data (not GameRoot `if`s), Thistledown is bigger and lives on a weekly rhythm, and the small Act I town and wilds quests (spec quests 2–12, 15, 16, 18) are playable, each with a peaceful path worth the same XP.

**Architecture:** A `QuestDb` in the game layer holds `QuestDef`s (stages with journal text, triggers that advance them, a reward paid once on completion). `QuestLog` listens to game events (`QuestEvent`: zone entered, group resolved, item gained, flag set, secret found) instead of GameRoot calling `Quests.Set` from scattered places; dialogue still calls `Quests.Set` directly for talk-driven stages. World "quest spots" reuse `SecretDef` with a quest gate, and enemy groups can be gated by quest stage. Thistledown grows SOUTH (rows appended to `Maps.Town`, so every existing coordinate stays valid).

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`, full suite ~12 min, run in background, muted).

**Spec:** docs/superpowers/specs/2026-10-28-act1-milestone-design.md ("Content: Act I", quest list, "Quest system"; build order item 4).

## Global Constraints

- Rules stay 2024 PHB / SRD 5.2. Every check, attack and save in a quest goes through the visible dice tray (`p.Hud.Dice.RollD20`) like the existing ones. Skill DCs stay hidden.
- Peaceful resolution pays the same XP (and loot) as fighting. Quest XP is a lump per player (every ready player), companions mirror their leader (`GiveXP` already does this).
- Tone: Mercer's lived-in world plus Mulligan's heart and humor; lighter, no dark/gothic themes; romance PG-13 at most.
- New names (NPCs, families, the fae trickster) are side content: list them in docs/LORE_PITCH.md under "Pending approval: Act I side content", like Wren/Hobb.
- No D&D/Stardew/Critical Role names in anything player-facing.
- Content stays data-driven: QuestDb, WorldDb, DialogueDb, MonstersDb, ItemsDb. Art stays swappable through ArtRegistry ids (ArtMap.txt lines + `.superpowers/import-art.sh`).
- Every window closes with Esc/B and has a ✕. Tests run muted.
- Old saves still load: existing quest ids (`fallen_star`, `kettle`, `spoon`, `east_road`) and their stage numbers keep their meaning.
- Phase 5 owns: The Fallen Star rework (crater), the Watchtower (13), the Festival (14), Snik as go-between (17). Don't build those here.

## Review Focus

- **Old saves:** a save with `kettle = 2` (done) loads, shows ✓ in the journal, and never pays the kettle reward again. Test: `OldSaveQuestsKeepStagesAndDontRepay` (Task 1).
- **Co-op double-pay:** both players get completion XP exactly once, even if P2 talks to the giver again or both trigger the final event in the same frame. Test: `QuestRewardPaysEachPlayerOnce` (Task 1).
- **Quest items leaving the bag:** the pond ring, herbs, poem and goats' bell can't be sold or dropped (`ItemDef.QuestItem`), and if they end up in home storage the turn-in still accepts them from storage. Test: `QuestItemsCantBeSoldOrDropped` (Task 2).
- **Timed quest across sleep:** sleeping or passing out during "Lost Child" never soft-locks it: past the deadline the watch finds Maddie, the quest completes with the smaller reward. Test: `LostChildResolvesEvenIfYouSleep` (Task 6).
- **Brawl lethality:** no one can die or roll death saves in the bar brawl, spells are blocked, and everyone wakes at 1 HP afterwards; a real fight elsewhere is unaffected. Test: `BarBrawlIsNonlethal` (Task 7).

---

### Task 1: Data-driven quests

**Files:**
- Create: `Assets/Scripts/Game/Quests/QuestDb.cs` (defs), `Assets/Scripts/Game/Quests/QuestLog.cs` (move `QuestLog` out of WorldState.cs)
- Modify: `Assets/Scripts/Game/Core/WorldState.cs` (remove old QuestLog), `GameRoot.cs` (Warp, OnGroupResolved, SplitLoot, GiveItem, ReturnKettle/ReturnSpoon/CompleteFallenStar/CompleteEastRoad), `Interactables.cs` (chest open, secret found → events), `Screens.cs` JournalScreen, `SaveSystem.cs` (unchanged format; verify), DialogueDb callers
- Test: `Assets/Tests/PlayMode/QuestTests.cs` (new)

**Interfaces (produces):**
```csharp
public enum QuestEvent { ZoneEntered, GroupResolved, ItemGained, FlagSet, SecretFound }
public class QuestTrigger { public QuestEvent On; public string Id; public int FromStage; public int ToStage; }
public class QuestReward { public int Xp, Copper; public string Item; public string Npc; public int Friendship; public string Flag; }
public class QuestDef { public string Id, Title, Giver /* npc id or null */; public string[] StageText; public List<QuestTrigger> Triggers; public QuestReward Reward; public int Last => StageText.Length - 1; }
public static class QuestDb { public static readonly Dictionary<string, QuestDef> All; public static QuestDef Get(string id); }
public class QuestLog {
  Dictionary<string,int> Stages; HashSet<string> Paid;          // Paid is saved
  int Stage(string id); bool Active(string id) /* 0 < stage < Last */; bool Done(string id);
  void Start(string id); void Set(string id, int stage, PlayerSlot by = null); // pays Reward once on reaching Last
  void Notify(QuestEvent e, string id);                          // applies matching triggers (Stage == FromStage → ToStage)
}
```
- Reward payout: every ready player gets `Xp` (via `GiveXP`, reason = quest title) and `Copper`; `Item` goes to `by` (or P1); friendship and flag once. `Paid` makes it idempotent; on load, any quest already at Last is added to `Paid` (old saves).
- Migrate the four existing quests into QuestDb with the same stage numbers and text. Their rewards move into `QuestReward` (kettle 100 XP + Brannoc +100; spoon 75 XP + healing potion + Tobin +100; fallen_star 300 XP + 50 gp + Rhoswen +100; east_road 150 XP + 25 gp + greater healing + Hobb +100). Triggers: `ZoneEntered ford` 0→1 east_road; `GroupResolved mine_egg` 1→2; `GroupResolved ford_toll` 1→2; `ItemGained copper_kettle` 0→1; `ItemGained lucky_spoon` 0→1. Remove the matching hard-coded lines in GameRoot; keep the non-quest side effects (`KettleReturned`, removing the item) in the Return* helpers.
- `QuestLog.Set` toasts: "New quest" / "Quest updated" / "Quest complete" (as now).
- Journal: active quests first (with giver name), then a collapsed "Completed" list.

- [ ] **Step 1: Write failing tests** in QuestTests.cs:
  - `EveryQuestIsWellFormed`: each def has ≥2 stages, `StageText[0] == ""`, every trigger's ToStage ≤ Last and FromStage < ToStage, `Giver` is null or in `WorldDb.Npcs`, every `GroupResolved` id exists in some zone, every `ItemGained`/reward item exists in ItemsDb.
  - `TriggersAdvanceQuests`: `Notify(ZoneEntered,"ford")` → east_road 1; `Notify(GroupResolved,"ford_toll")` → 2; a trigger whose FromStage doesn't match does nothing.
  - `QuestRewardPaysEachPlayerOnce`: two players; `Set("spoon",2)` twice and `Notify` again → each player's XP rose by exactly 75.
  - `OldSaveQuestsKeepStagesAndDontRepay`: save data with `kettle=2`, load, `Set("kettle",2,p)` → no XP change; journal text for kettle is the completed line.
- [ ] **Step 2:** `.superpowers/unity-test.sh QuestTests` → FAIL (types missing).
- [ ] **Step 3:** Implement as above; add `Paid` to save data (`List<string> QuestsPaid`, optional field).
- [ ] **Step 4:** `.superpowers/unity-test.sh QuestTests` → PASS; then `SmokeTests.TheFordQuestCanBeSolvedByTalking` and `SmokeTests.SaveAndLoadRoundTrip` → PASS.
- [ ] **Step 5:** Commit `"Quests: data-driven QuestDb with triggers and once-only rewards"`.

### Task 2: Quest spots, quest-gated groups, quest items, merged toasts

**Files:** `WorldDb.cs` (SecretDef, EnemyGroupDef), `Zone.cs` (prop spawning), `Interactables.cs` (SecretInteractable), `GameRoot.cs` (SpawnGroupIfNeeded), `Rules/Items.cs` + `Rules/Content/ItemsDb.cs`, `Screens.cs` (ShopScreen sell, inventory drop), `PlayerHud.cs` (Toast), tests in QuestTests.cs

**Interfaces (produces):**
- `SecretDef` gains: `string QuestId; int QuestStage = -1;` (spot is only present/interactable while `Quests.Stage(QuestId) == QuestStage`), `string SetsFlag` (set on success, which fires `QuestEvent.FlagSet`), `int AdvanceTo = -1` (on success `Quests.Set(QuestId, AdvanceTo, p)`). Quest spots may sit on any walkable tile (not just `q`); `WorldDataIsConsistent` checks only non-quest secrets for `qOY`.
- Zones re-check spot visibility on `QuestLog.Changed` (new `event Action QuestLog.Changed`).
- `EnemyGroupDef` gains `string QuestId; int MinStage, MaxStage` (spawns only while the stage is in range; 0/0 = always). Gated groups are `Unique`.
- `ItemDef.QuestItem` (bool): price 0, shop refuses to buy ("That's not mine to buy."), inventory "Drop" hidden, stack 1. Existing `copper_kettle`, `lucky_spoon` become quest items.
- `bool GameRoot.TakeQuestItem(PlayerSlot p, string id)`: removes from that player's bag, else another player's, else home storage; false if none.
- Test seam for rolls (no dice-forcing hook exists today): `public void SecretInteractable.Resolve(PlayerSlot p, CheckOutcome o)` holds everything after the roll; `Interact` calls it from the dice callback. Dialogue tests invoke `DOption.OnSuccess/OnFailure(ctx)` and jump to the option's `Success`/`Failure` node, as `TheFordQuestCanBeSolvedByTalking` already does with flags and `PacifyGroup`.
- `PlayerHud.Toast`: an identical text already on screen merges into it ("… ×3") and refreshes its timer instead of stacking (open follow-up from session 5).

- [ ] **Step 1: Failing tests:** `QuestSpotOnlyAppearsAtItsStage`, `QuestGatedGroupSpawnsOnlyInStageRange`, `QuestItemsCantBeSoldOrDropped` (ShopDb sell price 0 and refusal; TakeQuestItem finds the item in home storage), `DuplicateToastsMerge` (3× same text → 1 toast whose label ends in "×3").
- [ ] **Step 2:** Run `QuestTests` → FAIL. **Step 3:** Implement. **Step 4:** Run `QuestTests` and `SmokeTests.WorldDataIsConsistent` → PASS.
- [ ] **Step 5:** Commit `"Quests: quest spots, quest-gated groups, quest items; merged toasts"`.

### Task 3: Art for Phase 4

**Files:** `Assets/Editor/ArtMap.txt`, `Assets/Resources/Art/*` (via `.superpowers/import-art.sh`), hand-authored PNGs in `ArtSource/Authored/` wired through ArtMap, `Assets/Tests/PlayMode/ArtTests.cs`

**Ids (produces)**, pack sheet → id:
- NPCs (`actor` lines): `npc_garrick` (Character/Master), `npc_hesper` Hesper Meadowsweet (Character/OldWoman), `npc_dunstan` Dunstan Burrow (Character/Villager4), `npc_lark` Lark (Character/Woman), `npc_tamsin` Tamsin (Character/Villager5), `npc_wick` Wick the thief (Character/Boy), `npc_thimble` Mister Thimble the fae trickster (Character/MaskFrog), `npc_brawler` (Character/Caveman2), `npc_watchman` (Character/Knight).
- Creatures: `mon_owlbear` (Monster/Bear, tinted brown-cream by the slicer's existing tint option or hand-edited), `mon_owlbear_cub` (Monster/Owl2), `mon_goat` (hand-authored 16×16 from Animal/Donkey: shorter, horns, beard; 4-frame walk).
- Props/items: `obj_forge` (anvil+fire), `obj_barrel_tapped`, `obj_herb` (glowing plant), `obj_bench`, `obj_planter`, `obj_washing_line`; items `item_ring`, `item_poem`, `item_herb`, `item_goat_bell` (Items/ sheets or hand-authored).
- [ ] **Step 1:** Add test `Phase4ArtIsReal` to ArtTests.cs: for each id above, `Assert.IsTrue(ArtRegistry.IsReal(id), id)` → run `ArtTests` → FAIL. (`EveryZoneDrawsOnlyRealArt` also covers the new zones once Task 4 lands.)
- [ ] **Step 2:** Add ArtMap lines, author the gaps, run `.superpowers/import-art.sh`. **Step 3:** `ArtTests` → PASS; eyeball `%TEMP%/embers_screenshots` contact sheet.
- [ ] **Step 4:** Commit `"Art: Phase 4 NPCs, goat, owlbear, quest props and items"`.

### Task 4: Thistledown grows south, and a weekly rhythm

**Files:** `Maps.cs` (Town rows appended; new interiors `Forge`, `WatchHouse`, `Cottage`), `WorldDb.cs` (zones, warps, signs, secrets, NPCs, schedules), `Screens.cs` (ShopDb "forge" stock), `DialogueDb.cs` (Garrick, Hesper, Dunstan, Lark, Tamsin, Wick, watchman stubs to be extended in Tasks 5–6), `Tileset/TileStyler` only if a new char is needed, tests in SmokeTests/QuestTests

**Layout decisions:**
- Replace the bottom tree wall (row 35) with a gap at the path column and append ~16 rows: the **South Lane**. Landmarks: Garrick's forge (open-air anvil yard + interior), the Watch House (Rhoswen's office, holding cell with Wick later), the Meadowsweet and Burrow cottages facing each other across a fence and a little footbridge over the pond outflow (quest 6's "bridge"), Lark's cottage with a washing line, a bench-lined green. Irregular, not grid-like (memory: player tastes).
- Town square props (open follow-up): benches, planters and a second stall row so the square isn't bare.
- New interiors: `forge` (shop counter, Garrick), `watchhouse` (desk, cell), `cottage_lark` (small home). Warps both ways; `WorldDataIsConsistent` covers them.
- Forge shop stock (SRD weapons/armor already in ItemsDb): all simple and martial melee weapons in ItemsDb, shield, chain shirt, scale mail, chain mail; general store keeps its current stock.
- **Weekly rhythm** (`WorldDb.Weekdays`: 0 Dawnday … 5 Coinday, 6 Hearthday): Coinday is market day (Brannoc and Garrick at square stalls 10–14, Lark selling flowers); Hearthday morning service (Elowen + most villagers in the temple 9–11, shops closed); Emberday evening is "story night" at the Drake (Tobin, Brannoc, Hesper, Dunstan in the tavern); Rhoswen drills the watch on Stormday mornings at the Watch House yard. Every NPC (old and new) has at least one weekday-specific entry.
- [ ] **Step 1: Failing tests:** `EveryNpcHasAWeeklyVariation` (each NpcDef has an entry with `OnlyWeekday >= 0`), `MarketDayFillsTheSquare` (Coinday 11:00 → ≥3 NPCs in town within the square rect (18..32, 13..23)), `HearthdayServiceAtTheTemple` (Hearthday 10:00 → Elowen + ≥3 others in `temple`), `ForgeSellsWeapons` (ShopDb.Stock("forge") contains `longsword` and `chain_mail`). Extend `FuzzEveryConversation` id list with the new NPC dialogues.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run `QuestTests`, `SmokeTests.WorldDataIsConsistent`, `SmokeTests.FuzzTheTown`, `FuzzEveryConversation` → PASS. Screenshot test gets shots `20a_south_lane`, `20b_square_market`, `20c_forge`; review them.
- [ ] **Step 5:** Commit `"World: Thistledown South Lane, forge, watch house, weekly NPC rhythm"`.

### Task 5: Town quests (6, 8, 9, 10)

**Files:** `QuestDb.cs`, `DialogueDb.cs`, `WorldDb.cs` (spots), `ItemsDb.cs` (`pond_ring`, `brannoc_poem`, `pips_barrel`), `QuestTests.cs`

Each quest: giver, stages, peaceful and non-peaceful routes, reward. XP values follow existing scale (75–150).
- **6 `bad_blood` "Bad Blood at the Bridge"** (giver Rhoswen, Dawnday–Stormday at the notice board): Hesper Meadowsweet and Dunstan Burrow feud over a fence that moved a foot. Stage 1 talk to both; Insight DC 12 on each reveals the real grievance (Hesper's late husband built the fence; Dunstan's goat died to her dog years ago). Stage 2 → mediate (Persuasion DC 14, advantage if both insights succeeded) **or** side with one (the other's friendship −50). Any ending → stage 3 done. Reward 100 XP, 10 gp; mediation also gives both +50 friendship and a pie (`rations` ×2).
- **8 `pond_ring` "A Ring in the Pond"** (giver Tamsin): dropped the ring meant for Lark off the footbridge. Quest spot on the pond: Athletics DC 13 (dive) or Survival DC 12 (fish with a hook), daily retry → `pond_ring` (QuestItem). Choices on return: give it back to Tamsin (stage 3: Tamsin proposes on the green at 18:00 next day, the party is invited; both get +100 friendship) **or** Persuasion DC 12 to encourage Tamsin to propose right now in front of you. Reward 100 XP, Lark gives a flower crown trinket (`copper_trinket`).
- **9 `secret_poems` "Brannoc's Secret Poems"** (needs Brannoc ≥2 hearts or the `poetry` node seen): deliver an anonymous poem (`brannoc_poem`) to Mother Elowen. She reads it; Insight DC 13 lets you see she's delighted. Choice: reveal Brannoc (both +100 friendship; they're seen at the Drake together on Emberdays after — a schedule entry gated by flag `poems_revealed`) or keep the secret (Brannoc +150, poem stays anonymous, Elowen pins it in the temple). Reward 75 XP.
  - Requires `ScheduleEntry` gating: add `string RequiresFlag` to ScheduleEntry; `WorldDb.Current` skips entries whose flag isn't set.
- **10 `pips_barrel` "Pip's Missing Barrel"** (giver Pip): a barrel of Hearthfire Stout vanished from the cellar. Investigation spot at the tavern back door (DC 12) finds small boot prints and spilled stout toward the orchard → Wick (a teenager, hiding in the orchard 10–16 while the quest is active) has it, selling cups to farmhands to pay for his sick mum's medicine. Choices: mercy (return the barrel together, Persuasion DC 13 to get Pip to give Wick a job; Wick then works at the Drake on schedule), justice (march him to Rhoswen; Wick in the watch-house cell for 2 days then back, sulking), or pay for the barrel yourself (10 gp). All endings → done. Reward 100 XP; mercy+job also gives Pip +100 and a free room once (flag `pip_free_room`, used by `RentRoom`).
- [ ] **Step 1: Failing tests** (drive dialogue options by id with the existing test helpers, `ScriptInput` or direct `DOption.Do` calls as in `TheFordQuestCanBeSolvedByTalking`): `BadBloodMediationAndSidingBothComplete`, `PondRingCanBeFoundAndReturned` (drive the spot with `SecretInteractable.Resolve(p, CheckOutcome.Success)`), `SecretPoemsRevealAddsTavernSchedule`, `PipsBarrelMercyGivesWickAJob`.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run `QuestTests` + `FuzzEveryConversation` → PASS.
- [ ] **Step 5:** Commit `"Quests: Bad Blood, Ring in the Pond, Secret Poems, Pip's Barrel"`.

### Task 6: Wilds quests (2, 7, 11, 12, 15, 18)

**Files:** `QuestDb.cs`, `DialogueDb.cs`, `WorldDb.cs`, `MonstersDb.cs` (`goat`, `owlbear`, `owlbear_cub`), `ItemsDb.cs` (`goat_bell`, `herb_moonpetal`, `herb_riverleaf`, `herb_hearthroot`, `fae_trinket`), `GameRoot.cs` (goats following, deadline check on hour tick), `QuestTests.cs`

- **2 `goats` "Goats Gone Astray"** (giver Tobin): 3 goats in Whisperwood (quest spots with sprite `mon_goat`, stage 1 only). Animal Handling DC 11 each → goat trots home (flag `goat_1..3`; goat actor appears in Tobin's pen). Failing a check: the goat butts you (1 bludgeoning, shown) and you retry. All three flags → stage 2 (return to Tobin) → done. Reward 75 XP + `goat_bell` (trinket).
- **7 `haunted_mill` "The Haunted Mill"** (giver Hobb, after east_road is done): "ghost" noises at night. Investigation spot inside the mill yard 20:00–05:00 → the owlbear cub (quest spot, `mon_owlbear_cub`). Peaceful: Animal Handling DC 13 to calm it, then carry it to the den in Whisperwood (a quest spot at the wolf-den clearing) → done. Fight path: the mother owlbear (`owlbear`, SRD CR 3, quest-gated group in Whisperwood, stage 2 only) — the DM warns her CR before you engage. Both endings 150 XP (same XP rule: the peaceful path pays the owlbear's SRD XP too, 700 split, via `EncounterShare`).
- **11 `wolf_bounty` "Wolves on the Road"** (Rhoswen's notice board): trigger `GroupResolved forest_wolves` 1→2, return to Rhoswen → done. Reward 50 XP + 15 gp on top of the group's own XP.
- **12 `lost_child` "Lost Child in the Woods"**: starts automatically the first Tideday at 15:00 after day 3 (Maddie's parents at the square, quest spot by the forest gate). Maddie is in Whisperwood near the fairy ring. Survival or Perception DC 13 at two track spots leads to her; she's with tiny lights (the fae ring, harmless). Deadline 20:00 the same day: found in time → 150 XP, Maddie +200 friendship; past the deadline (any hour tick, sleep included) → the watch finds her, quest done with 50 XP and a narrator line. Deadline is checked in `GameRoot` on each hour tick and on wake.
- **15 `elowens_herbs` "Mother Elowen's Herbs"** (giver Elowen): three herb spots (Whisperwood: moonpetal, Nature DC 12; Miller's Ford: riverleaf, Nature DC 11; Thistledown south green: hearthroot, Medicine DC 12), daily retry. `ItemGained` triggers count via flags; bring all three → done. Reward 100 XP + 2 healing potions.
- **18 `fae_trickster` "The Fae Trickster"**: Mister Thimble at the Veil-touched pond in Whisperwood (18:00–23:00). Three riddles as dialogue options (one right answer each) plus an Arcana or Investigation DC 13 roll per riddle for a hint that removes one wrong option. 2+ right → `fae_trinket` (a pocket mirror that never fogs; flavor trinket) + 100 XP; fewer → he laughs, gives the XP anyway ("you made me laugh, that's payment"), trinket only on a retry the next night. Foreshadows Act II with one line about "the Court waking".
- [ ] **Step 1: Failing tests:** `GoatsComeHomeAndTobinPays`, `HauntedMillPeacefulAndFightPaySameXp`, `WolfBountyCompletesFromAnyResolution` (talk the wolves down), `LostChildResolvesEvenIfYouSleep`, `HerbsFromThreeZonesComplete`, `FaeTricksterPaysXpEitherWay`, plus `EveryQuestCanStart` (each QuestDef reaches stage 1 via its giver dialogue or trigger: fuzz-free, deterministic).
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run `QuestTests`, `FuzzTheForest`, `FuzzTheFord`, `FuzzEveryConversation` → PASS.
- [ ] **Step 5:** Commit `"Quests: goats, haunted mill, wolf bounty, lost child, herbs, fae trickster"`.

### Task 7: Bar Brawl at the Drake (16)

**Files:** `QuestDb.cs`, `DialogueDb.cs` (Pip offers it on Coindays after 19:00), `WorldDb.cs` (tavern group `drake_brawlers`, quest-gated, 3× `brawler`), `MonstersDb.cs` (`brawler`: SRD Commoner stats, unarmed 1d4 "fists and furniture"), `CombatInstance.cs` + `CombatTurns.cs` (brawl mode), `QuestTests.cs`

**Interfaces:** `CombatInstance.Brawl` (bool, set from `EnemyGroupDef.Brawl`). In brawl mode:
- Actions offered: Move, Unarmed Strike (2024: 1 + STR bludgeoning; the monk's Martial Arts die applies), Improvised Weapon "Tankard" (1d4 bludgeoning, proficiency not added unless Tavern Brawler feat), Shove, Dodge, Dash, End Turn. Spells, class damage features (Rage still allowed: it's not magic), items and Give Potion are hidden. Narrator: "Pip: 'NO MAGIC IN MY BAR!'"
- Anyone reaching 0 HP is knocked out (2024 knockout rule): unconscious, no death saves, can't be finished. When the fight ends, all knocked-out creatures wake at 1 HP; brawlers become neutral and cheer.
- Win → quest done: 100 XP, Pip's free round (friendship +50 with Pip and Tobin). Lose (whole party knocked out) → no wipe/temple: you wake on the tavern floor, quest still done with 50 XP ("you were magnificent").
- [ ] **Step 1: Failing test** `BarBrawlIsNonlethal`: start the brawl with a cleric; assert no spell actions offered; drop the player to 0 → `IsDead == false`, no death-save state; end combat → HP 1; then `FightInTheMineWithEachClass` still passes (normal combat unaffected).
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** Run `QuestTests` + `SmokeTests.FightInTheMineWithEachClass` → PASS.
- [ ] **Step 5:** Commit `"Quests: nonlethal bar brawl at the Sleeping Drake"`.

### Task 8: Phase 4 acceptance

**Files:** `ScreenshotTests.cs`, `docs/PLAYTEST.md` (quest checklist per quest: where it starts, both routes), `docs/DESIGN.md` (quest system section), `docs/LORE_PITCH.md` (pending side-content names), memory/progress ledger

- [ ] **Step 1:** Add `QuestWalkthroughFuzz`: a fuzz run in town, forest and ford with every quest started (deterministic seed), 2000 frames each, no exceptions.
- [ ] **Step 2:** Full suite in background: `.superpowers/unity-test.sh` → all pass; `dotnet run --project tools/RulesTests` → all pass. Record counts.
- [ ] **Step 3:** Review screenshots (South Lane, market day, forge, brawl, journal with many quests).
- [ ] **Step 4:** Docs; rebuild exe + zip (`Embers > Build Windows` / `BuildTools.BuildWindows`).
- [ ] **Step 5:** Final whole-branch review against this plan and the spec; fix findings; commit `"Phase 4 acceptance: quests, South Lane, docs, build"`.
