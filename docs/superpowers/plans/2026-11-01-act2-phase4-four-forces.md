# Act II Phase 4: The Four Forces Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Each of the four forces (the Crown, the Starfall Academy, the Fae Courts, the Wyrmwatch) gets a home, a leader you can hear out, one or two scouting quests that show what it believes, and a major quest. Starting a major quest is the forced choice: it commits the party to that force and closes the other three. The main quest "Answers" carries the party from Ashby to that choice.

**Architecture:** The same data-driven style as Phase 3. New zones are `ZoneDef` + `Maps` arrays in a new `WorldDb.Forces.cs`; dialogue goes in a new `DialogueDb.Forces.cs` (wired like `AcademyDialogue`); quests go in `QuestDb` with `Faction`/`Major`/`Act = 2`; monsters in `MonstersDb`; lore in `LoreDb`. The framework from Phase 2 (`Factions`, `QuestLog.CanStart`, `OfferCommit`) is reused. Engine additions are small:
1. `Factions.Heard(f)` / `HeardAll()` / `MajorOpen(f)` / `StartMajor(by, questId)`.
2. A lantern/brazier quest trigger (`QuestEvent.BrazierLit`, `QuestTrigger.LitAtLeast`).
3. "Answers" gains stage 5 (the choice).

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite takes ~15 min, so run it in the background, muted). Art via `Assets/Editor/ArtMap.txt` + `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md ("The four forces", "How they work across the acts", Goal 5, the secret). Roadmap item 4 in docs/superpowers/plans/2026-10-31-act2-phase1-levels.md.

## Global Constraints

- **THE SECRET:** nothing may hint that the Ashen Queen is a child, or that the Crown of old is the culprit. `LoreTests.NoActTwoTextHintsAtTheSecret` must stay green and must scan the new dialogue file. The Ashsworn (her secret servants) speak only of "the Queen", "her fire", "the waking". They never mention a voice, crying, fear or size.
- **Every force is grey.** Each is right about something and each asks a real price. No force's quests make it the good one. The modern Crown is ordinary and decent (it spies on the Academy, and it protects villages).
- **The forced choice** (spec): scouting is open to everyone. Starting any major quest goes through `Factions.OfferCommit` (a plain-words confirm prompt). On yes, the other three forces' quests close for good. In co-op either player can commit; the partner gets the narration.
- **[Claude's call] Hear everyone out first:** a force's major quest is offered only once the party has *heard out all four forces* (met each leader) *and* finished at least one of that force's scouting quests. You don't have to finish every scouting quest.
- Peaceful options pay the same XP and loot as fighting (the volcano is the only exception, and that's Phase 6). Dice are always visible.
- The dragon's trust never blocks the players: it shows as emotes, barks and Advantage only.
- Tone: bright fantasy, PG-13. Unsettling at most.
- Every new NPC gets a weekly habit and every outdoor door a sign (existing content-rule tests). Every window closes with Esc/B and has a ✕.
- Data-driven content. Art goes through ArtRegistry ids, and every new id gets an ArtMap line or a `pattern`/`paint`.
- New zones are `Act = 2`, every new quest is `Act = 2`, and every warp into them is `MinAct = 2` (Act I stays light).
- Branch `act2/phase4-four-forces`. Merge to main and push after the full suite passes. Names are working names (bulk-approved); list them in LORE_PITCH.md.

## Names (working)

- **The Crown:** **Greywater Redoubt**, a Crown outpost north of the Lantern Road checkpoint. **Lady-Commander Isaine Marsh** commands it. Sergeant Holloway (Phase 3) is hers.
- **The Academy:** High Magister Ashby (Phase 3) and **Perrin**, who has been made Keeper of the Road Lamps.
- **The Fae Courts:** **the Gloaming Glade**, always at dusk, reached through the Whisperwood fairy ring in Act II. **Mister Thimble** (Act I) opens the way. **Herald Sorrel of the Three Courts** speaks for the Courts of Blossoms, Embers and Frost.
- **The Wyrmwatch:** **Cragfoot**, a mountain hamlet with the order's timber **Wyrmwatch Lodge**, east along the Lantern Road. **Huntmaster Bryn Calloway** leads it, with hunter **Odo Fenn**.
- **The Ashsworn:** a few people who secretly serve the Queen. They're held up in the Redoubt's ruined beacon. **The Ashmaw:** an ember-eyed wyvern, hunted in its lair above Cragfoot.

## The quests

| Id | Title | Force | Kind | Giver | XP | Standing |
|---|---|---|---|---|---|---|
| `counted` | Counted | crown | scouting | holloway | 1200 | crown +10 |
| `two_copies` | Two Copies | crown | scouting | varrow → marsh | 1000 | see Task 2 |
| `crowns_banner` | Under the Crown's Banner | crown | major | marsh | 3000 | crown +30 |
| `cold_lanterns` | Cold Lanterns | academy | scouting | perrin | 1000 | academy +10 |
| `ninth_circle` | The Ninth Circle | academy | major | ashby | 3000 | academy +30 |
| `court_quarrel` | A Quarrel of Courts | fae | scouting | sorrel | 1100 | see Task 4 |
| `green_bargain` | The Green Bargain | fae | major | sorrel | 3000 | fae +30 |
| `ember_eyes` | Ember Eyes | hunters | scouting | calloway | 1200 | hunters +10 |
| `long_hunt` | The Long Hunt | hunters | major | calloway | 3000 | hunters +30 |

Every major's reward sets a flag that Phase 5 reads for the faction flavour: `banner_crown`, `banner_academy`, `banner_fae`, `banner_hunters`.

## Review Focus

- **Committing while another force's scouting quest is half done:** the closed quest's spots, gated groups and dialogue options vanish. The journal says "Closed: you stood with …". No trigger can advance or pay it later, after a reload too. Test: `CommitClosesHalfDoneQuestsEverywhere` (Task 1).
- **Co-op, two prompts at once:** P1 and P2 both open a major giver's commit prompt (or two different givers). Only the first "yes" commits and starts a major. The second "yes" does nothing harmful: no second major starts, and it gets a toast saying "You already stand with …". Test: `TwoPromptsOnlyOneCommit` (Task 1).
- **A Phase 3 save at Answers stage 4** (which was the last stage then, so it counted as complete and paid): after load, Answers is active at stage 4 and moves to stage 5 on commit, paying stage 5's XP exactly once. Test: `PhaseThreeSaveContinuesAnswers` (Task 1).
- **Act I at the fairy ring:** the Glade warp never opens in Act I. The Act I fairy-ring secret and the `lost_child`/`fae_trickster` quests still work, and nothing narrates on the ring tile. Test: `FairyRingStaysAnActOneSecret` (Task 4).
- **A major quest mid-way across save/load:** its Unique gated groups stay resolved and don't respawn. Quest spots reappear at the right stage, and the commitment holds. Test: each force task's `<Major>SurvivesSaveLoad`.

---

### Task 1: The choice framework: hearing them out, starting a major, Answers stage 5

**Files:**
- `Assets/Scripts/Game/Quests/FactionDb.cs` (Factions additions)
- `Quests/QuestDb.cs` (Answers stages; `QuestEvent.BrazierLit`; `QuestTrigger.LitAtLeast`)
- `Quests/QuestLog.cs` (Notify matching for LitAtLeast)
- `World/DragonPet.cs` (`Light` notifies `BrazierLit`)
- `UI/Screens.cs` (journal: "Heard out: n/4")
- Tests: `Assets/Tests/PlayMode/FactionTests.cs` (extend)

**Interfaces (produces):**
```csharp
public static class Factions {
    public static string HeardFlag(string f) => "heard:" + f;      // world flag, set by each leader's first real talk
    public static void Hear(string f);                              // sets the flag; toast "You've heard out the Crown. (2/4)"
    public static bool HeardAll();                                  // all four flags set
    public static bool MajorOpen(string faction);                   // no commitment yet, HeardAll(), and ≥1 Done scouting quest (Faction == f && !Major)
    public static bool StartMajor(PlayerSlot by, string questId);   // committed to q.Faction → Quests.Start; uncommitted → OfferCommit(by, q.Faction, () => Quests.Start(questId, by)); committed elsewhere → toast, false
}
public enum QuestEvent { ..., BrazierLit }                          // Id = the zone id of the lit brazier
public class QuestTrigger { public static QuestTrigger LitAtLeast(string zone, int from, int to, int n); } // fires when ≥ n LitBraziers keys start with zone + ":"
```
- `Commit` also sets the world flag `committed`. A second `Commit` call (another prompt's yes) toasts "You already stand with {name}." to `by` and returns. `OfferCommit`'s yes runs `onYes` only if this call committed (`CommittedFaction == faction` after it, and it was null before).
- Ashby's existing "every ending sets `ashby_heard`" also calls `Factions.Hear("academy")`, and loading a save with `ashby_heard` sets the academy heard flag too (migration-free: `Hear` is idempotent, called in `ApplyActCap`).
- **Answers stages** (the numbers keep their meaning):
  - Stage 4's text becomes: "Ashby wants your dragon studied and the dragons sealed. He isn't the only one with plans for it. Hear out the Crown, the Fae Courts and the Wyrmwatch, then choose who you stand with."
  - New stage 5: "You've chosen a side. (The road to the Emberhorn opens in a later update.)" Trigger `FlagSet committed` 4→5. StageXp[5] = 1000.
  - Add `Answers.Chosen = 5`.
- Journal faction section: a "Heard out: n/4" line, plus each force's belief once it's heard.

- [ ] **Step 1: Write failing tests** in FactionTests (uses `RegisterTestQuests`):
  - `MajorNeedsEveryoneHeardAndOneScout`: `MajorOpen("crown")` is false until all four `Hear` calls are made and `test_scout_crown` is done.
  - `StartMajorAsksThenCommits`: `StartMajor(p1, "test_major_crown")` pushes a ChoiceScreen. "Not yet" changes nothing. Yes → committed, the `committed` flag is set and the quest is at stage 1.
  - `CommitClosesHalfDoneQuestsEverywhere`: `test_scout_fae` at stage 1, commit crown. `Set("test_scout_fae", 2)` does nothing. `Notify` does nothing. After save/load it's still closed, and `ClosedBy` is non-null.
  - `TwoPromptsOnlyOneCommit`: two players, two prompts (crown and fae). Yes on both → committed crown; `test_major_fae` stage 0; `test_major_crown` stage 1.
  - `PhaseThreeSaveContinuesAnswers`: Act 2, Answers at 4, `Paid` contains "answers" (old save). Load → Active. Commit → stage 5, and XP rises by exactly 1000 per player. Commit again → no more.
  - `LitAtLeastAdvancesAQuest`: a test quest with `LitAtLeast("lantern_road", 1, 2, 2)`. Adding two keys and notifying advances it; one key doesn't.
- [ ] **Step 2:** `.superpowers/unity-test.sh FactionTests` → FAIL (compile).
- [ ] **Step 3:** Implement as above.
- [ ] **Step 4:** `FactionTests` + `AcademyTests` + `QuestTests` → PASS.
- [ ] **Step 5:** Commit `"Act II: hearing out the four forces, starting a major quest, Answers stage 5"`.

### Task 2: The Crown: Greywater Redoubt, Marsh, Counted, Two Copies, Under the Crown's Banner

**Files:** `World/Maps.cs` (`Redoubt`, about 44×32; open the Lantern Road's north edge with a track at cols 46–47, rows 0–3), `World/WorldDb.Forces.cs` (new: `BuildForces()` called after `BuildAcademy`; `BuildRedoubt()`), `Dialogue/DialogueDb.Forces.cs` (new; `ForcesDialogue(id)` chained after `AcademyDialogue`), `DialogueDb.Academy.cs` (Holloway, Varrow), `QuestDb.cs`, `Rules/Content/MonstersDb.cs`, `ItemsDb.cs`, `LoreDb.cs`, `DragonDb.cs`, tests in `Assets/Tests/PlayMode/ForcesTests.cs` (new; SetUp/TearDown as AcademyTests).

**Interfaces (produces):** zone `redoubt` ("Greywater Redoubt", outdoors); NPC `marsh`; quests `counted`, `two_copies`, `crowns_banner`; monsters `ashsworn_zealot`, `ashsworn_keeper`; item `crown_writ`; flag `banner_crown`; lore `beacon_orders`.

**The Redoubt** (hand-shaped):
- A timber palisade with a gate (south, to the road) and a drill yard where soldiers drill.
- A command tent (Marsh), barracks tents, stables with a farrier and a mess fire.
- A watch platform with a view sign: "From up here you can see the whole valley. Thistledown is a smudge of smoke. The Wyrmback stands over everything, very still."
- The ruined **old beacon tower** in the north corner, fenced off with a rope and a sign "BY ORDER: NO ENTRY". It's the Ashsworn hideout (gated group).
- Two flavor soldiers reuse `crown_guard`.

**Marsh** is crisp, fair and tired of being the villain in other people's stories. "The Academy studies. The Fae bargain. The hunters hunt. Somebody has to keep forty villages fed and safe while they do it. That somebody is the realm." Her first talk calls `Factions.Hear("crown")`. Insight 15: she means it, and she's been told to bring your dragon in by any means, and is choosing the polite ones. Dragon opinion: Wary.

**`counted`** (Holloway): the weekly supply wagon from the Redoubt is two days late.
- Stage 1 → find the wagon. It's a quest spot on the Lantern Road's ash fields. A new gated group `wagon_hounds` (3 `cinder_hound`, Beasts, TalkDC 15) circles it.
- Stage 2 → the two drivers are hiding in the ruined farmhouse. A quest secret at its door (Persuasion 12: "It's safe, come out.") sets stage 3.
- Stage 3 → tell Holloway. Done: crown +10, 1200 XP, 2 `potion_greater_healing`.
- What it shows: the Crown is the one keeping this road fed.

**`two_copies`** (starts from Varrow once `varrow_post` is set, via a new "(Follow the evening post)" option; or from Marsh's "Who writes to you from the Academy?" if `varrow_post` is set):
- Stage 1 → the post goes to the Redoubt. Talking to Marsh with stage 1 opens the reveal: the Crown has read the Academy's dragon notes for a year. "If the Academy is building a cage under the realm's feet, the realm deserves to know what's in it."
- Every option completes the quest (stage 2, 1000 XP):

| Option | Effect |
|---|---|
| Tell Varrow you know | academy +10, crown −5 |
| Keep their secret | crown +10, academy −5 |
| (Persuasion 15) "Ask Ashby directly. He'd tell you." | Success: crown +5, academy +5, flag `marsh_asks_ashby`. Failure: no change. |

**`crowns_banner`** (Marsh, major; offered when `Factions.MajorOpen("crown")`, via `Factions.StartMajor`):
- Stage 1: "The Ashsworn have the old beacon. Clear it." The gated group `beacon_ashsworn` (Unique, `QuestId crowns_banner`, stages 1–1): 4 `ashsworn_zealot` + 1 `ashsworn_keeper`, Style Talk, TalkDC 17, ScareDC 16.
  - Spotted: "Grey-robed figures turn from a fire that burns without smoke. 'She is waking,' one says, quite calmly. 'Her fire will burn the world clean. You can stand in it or stand aside.'"
  - Befriended: "You talk for a long time. One by one they put down their knives. They don't look relieved. They look like people who have been very tired for a very long time."
  - Resolved by either path → stage 2.
- Stage 2: a lore spot `beacon_orders` in the beacon. The Ashsworn's own notes: they light no fires, gather where the ash falls thickest, wait for "the waking". Neutral. Reading it → stage 3.
- Stage 3: report to Marsh. She asks that the dragon be entered in the Royal Register: it wears a small brass registry tag.
  - Accept: the dragon's bond −1 and `DragonDb.Shift("marsh", -1)`.
  - (Animal Handling 14) "Let {dragon} get used to it first": success, no bond loss.
- Done: crown +30, 3000 XP, `crown_writ` (trinket, 0 gp, text "Bearer travels under the Crown's protection"), flag `banner_crown`. **[Claude's call]** Phase 5 posts soldiers in Thistledown off this flag.
- Monsters:
  - `ashsworn_zealot`: SRD Cult Fanatic stat line, no spells. AC 13, HP 33, two Dagger attacks +4 1d4+2 (multiattack), plus an Ember Touch melee +4 2d6 fire as the second attack's alternative. CR 2.
  - `ashsworn_keeper`: SRD Veteran stat line. AC 17, HP 58, Longsword ×2 +5 1d8+3. CR 3.
  - Both are human sprites with a grey robe tint.

- [ ] **Step 1: Write failing tests** in ForcesTests:
  - `RedoubtIsReachable`: Act 2. From the road's north track warp → `redoubt`. Every secret, NPC daytime spot and the beacon are `CanReachBeside`. In Act I the track narrates closed.
  - `CountedFullRun`: drive the stages (spot, group resolved peacefully, farmhouse secret, Holloway). XP paid once, crown standing +10.
  - `TwoCopiesEveryOptionCompletes`: each of the three options → stage 2, with the standing deltas as in the table.
  - `CrownsBannerCommitsAndPays`: hear all four (test hook `Factions.Hear`), finish `counted`, then `StartMajor` via Marsh's option → committed crown, `two_copies` closed if it was unfinished. Resolve the beacon by `ResolveGroup(victory)` in one run and by talking in another: the same XP. Lore → 3, Marsh → done, `banner_crown` set.
  - `CrownsBannerSurvivesSaveLoad`: save at stage 2 → load: the beacon group is gone, the lore spot is present, committed still crown.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement (art: palisade/tent tiles from the pack; the ashsworn tint; the beacon prop).
- [ ] **Step 4:** `ForcesTests` + `FactionTests` + `LoreTests` + `SmokeTests.WorldDataIsConsistent` → PASS.
- [ ] **Step 5:** Commit `"Act II: the Crown (Greywater Redoubt, Marsh, Counted, Two Copies, Under the Crown's Banner)"`.

### Task 3: The Academy: Cold Lanterns and The Ninth Circle

**Files:** `Maps.cs` (LanternRoad: six lantern posts become cold braziers `y`), `WorldDb.Forces.cs`, `DialogueDb.Academy.cs` (Perrin, Ashby, Varrow, Oakes), `QuestDb.cs`, `MonstersDb.cs`, `ItemsDb.cs`, `LoreDb.cs`, tests.

**Interfaces (produces):** quests `cold_lanterns`, `ninth_circle`; items `focus_stone` (quest item, stack 3), `ward_charm`; monster `cinder_bear`; flags `lantern_eyes`, `banner_academy`; lore `lantern_eyes`.

**`cold_lanterns`** (Perrin, Act II, "the Academy has made me Keeper of the Road Lamps, which means I'm the one who gets shouted at"):
- Stage 1: relight six cold lantern posts on the Lantern Road with the dragon (the existing brazier option). Trigger `LitAtLeast("lantern_road", 1, 2, 6)`.
  - **[Claude's call]** Without the dragon, a player with a lit torch can light a post too: add "Light it with my torch" to `UnlitBrazier`'s interact when the player's torch is lit, so the quest never depends on the dragon.
- Stage 2: tell Perrin. Before that, a quest secret on the last post (Arcana 14) reveals that each lamp holds a tiny ward-eye: the lamps watch the road for dragon fire. It sets `lantern_eyes` and lore `lantern_eyes` (neutral, and Perrin didn't know). Optional.
- Done: academy +10, 1000 XP. If `lantern_eyes`, Perrin's turn-in has a variant line ("They... watch? For what? ...Oh. Oh, I see.") and the dragon gets a "?" emote.
- What it shows: the Academy lights the road for everyone, and it watches everyone on it.

**`ninth_circle`** (Ashby, major; via `StartMajor`):
- Stage 1: "Circle nine needs three focus stones." Each is a `focus_stone`:
  - Varrow's Observatory: dialogue. She gives it gladly, or Insight 15 for her real reason: she wants the circle finished before the Crown asks for it.
  - Oakes's Glasshouse: dialogue. He gives it reluctantly; Persuasion 13, or if `glasshouse` is done he gives it freely.
  - The Starwell: a quest secret (Arcana 15) at the well's rim.
  - Holding 3 → stage 2 (ItemGained trigger, as in `overdue`).
- Stage 2: bring them to the Sealing Hall (the dragon waits upstairs, as in Phase 3). A quest secret at the dais "Set the stones" → stage 3, which spawns the gated group `circle_bear` (1 `cinder_bear`, Beasts, Unique, TalkDC 16).
  - Narration: "The circle flares. Something the Academy caught on the slopes is in it: a bear with a coal-glow for eyes. The binding holds for a breath, and then it doesn't."
  - Calm it or fight it → stage 4.
  - `cinder_bear`: SRD Brown Bear, AC 11, HP 34, Bite +6 1d8+4 and Claws +6 2d6+4 (multiattack), with an ember tint.
- Stage 4: report to Ashby. He's shaken: "It held for a breath. I need it to hold forever."
  - (Insight 14): he realises he would rather it hadn't been you down there. No change.
- Done: academy +30, 3000 XP, `ward_charm` (ring, +1 AC, attunement-free; reuse the Ring of Protection item type if one exists, else add one), flag `banner_academy`.

- [ ] **Step 1: Write failing tests:**
  - `ColdLanternsLitByDragonOrTorch`: light 4 by the dragon's `Light` and 2 by torch → stage 2. The Arcana secret sets `lantern_eyes`. Perrin → done once.
  - `NinthCircleFullRun`: committed academy via `StartMajor`. Stones from the 3 sources → 2. The dais secret → 3, with the group present. A peaceful resolve and `ResolveGroup(victory)` both pay the same → 4. Ashby → done, `banner_academy`.
  - `NinthCircleSurvivesSaveLoad`: save at 3 after the bear's resolution → no bear on load, stage 3 kept.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `ForcesTests` + `AcademyTests` + `DragonTests` → PASS.
- [ ] **Step 5:** Commit `"Act II: the Academy (Cold Lanterns, The Ninth Circle)"`.

### Task 4: The Fae Courts: the Gloaming Glade, Thimble, Sorrel, A Quarrel of Courts, The Green Bargain

**Files:** `Maps.cs` (`Glade`, about 40×32, `MagicLit`), `WorldDb.Forces.cs` (`BuildGlade()`; a warp beside the Whisperwood fairy ring), `DialogueDb.Forces.cs` (sorrel), `DialogueDb.SouthLane.cs` (Thimble's Act II nodes), `WorldDb.cs` (Thimble's Act II schedule), `QuestDb.cs`, `MonstersDb.cs`, `ItemsDb.cs`, `LoreDb.cs`, `DragonDb.cs`, tests.

**Interfaces (produces):** zone `glade` ("The Gloaming Glade", always dusk: a fixed violet-gold tint whatever the clock says); NPC `sorrel`; quests `court_quarrel`, `green_bargain`; flags `ring_open`, `fae_clause`, `banner_fae`; monster `thorn_guardian`; item `bloom_charm`.

**The way in:** a warp on the tile south of the Whisperwood fairy ring, with `MinAct 2`, `RequiresFlag ring_open` and no ClosedText (so Act I is silent).
- Thimble in Act II stands at the ring (18–23). His new nodes say the Courts are asking for "the little flame's people". He opens the ring (`ring_open`) once you agree to go, or after a Performance 13 dance.
- The ring's Act I secret is unchanged.

**The Glade** (hand-shaped):
- Three court quarters, each with a throne set piece:
  - Blossoms (spring: flowers, petals)
  - Embers (autumn: red leaves, a warm spring)
  - Frost (winter: white trees, an iced pool)
- They meet at a central stone table where Sorrel holds court.
- Floating lights `^` and mushroom rings, plus a mirror-pond secret: (Insight 14) shows your reflection a moment late. Lore `glade_mirror`: the Fae remember the Pact being *made*; they don't speak of how it broke. Neutral.

**Sorrel** is charming, exact and older than she looks. "A promise is the only thing in the world that's stronger for being old." Her first talk calls `Factions.Hear("fae")`. Insight 15: every word is true, and chosen. Dragon opinion: Fond (the Embers court is warm).

**`court_quarrel`** (Sorrel):
- The Blossom and Frost courts both claim the glade's last warm spring (the blight is chilling the Embers court, which used to share it).
- Stage 1 → hear each court's speaker. Two flavor NPCs, `blossom_speaker` and `frost_speaker`; talking to both sets flags → stage 2.
- Stage 2 → judge at Sorrel's table. Every option completes (stage 3, 1100 XP):

| Option | Effect |
|---|---|
| For the Blossoms | fae +5; Frost sulks (a line); `fae_judged_blossom` |
| For the Frost | fae +5; `fae_judged_frost` |
| (Persuasion 15) "Share it by season, as the Pact did." | Success: fae +15. Failure: fae +0, and Sorrel: "A good answer, badly argued." |

**`green_bargain`** (Sorrel, major; via `StartMajor`): the Courts will renew their part of the Pact in this valley if the party carries a token to each throne before the glade's "night" (three tokens, any order).
- Each throne is a quest secret with two routes:
  - Blossom: Performance 14 or Nature 14.
  - Ember: Arcana 14, or the dragon lights the throne's cold brazier `y`. Add `SecretDef.SucceedsIfLit` (a brazier key, `UnlitBrazier.KeyFor(zone, tile)`): if that key is in `World.LitBraziers`, interacting succeeds without a roll.
  - Frost: Persuasion 15, or fight the gated `frost_thorns` group (2 `thorn_guardian`; Beasts, TalkDC 15, `TalkSkill Nature`). Resolving it either way also completes the Frost throne.
  - The three flags → stage 2 (`FlagsAtLeast`).
- Stage 2: Sorrel offers the bargain scroll.
  - (Investigation 15 or Insight 16): find the hidden clause. "And the little flame shall owe the Courts one favour, to be asked when the stars ask it."
  - Options:
    - "Sign it as written": sets `fae_clause` if found or not. The player is told only if they found it.
    - (Persuasion 18, only if found) "Strike the clause.": success removes it, fae −5. Failure: sign as written or walk away (walking away = stage stays 2, try again tomorrow).
  - Either signing → stage 3 (done).
- Done: fae +30, 3000 XP, `bloom_charm` (amulet: once per long rest, a free *Healing Word* at its lowest level as a hotbar item. **[Claude's call]** If item-granted spells don't exist, make it a consumable "Bloom Draught" ×3 that heals 2d4+2), flag `banner_fae`.
- `thorn_guardian`: SRD Awakened Tree trimmed. AC 13, HP 45, Slam +6 2d6+4, vulnerable to fire. It uses a white-tinted tree sprite.

- [ ] **Step 1: Write failing tests:**
  - `FairyRingStaysAnActOneSecret`: Act 1, `ring_open` set. Stepping on the warp tile does nothing and narrates nothing. The fairy-ring secret still resolves.
  - `ThimbleOpensTheRingInActTwo`: Act 2. Thimble's option sets `ring_open`, and the warp leads to `glade`. Every throne and Sorrel are reachable.
  - `CourtQuarrelEveryJudgementCompletes`: each option → stage 3, with the deltas as in the table.
  - `GreenBargainEveryRoute`: committed fae. Blossom by Nature, Ember by the dragon's brazier, Frost by a peaceful resolve → 2. Find the clause and sign → `fae_clause`, done, `banner_fae`. In a second run, Frost by `ResolveGroup(victory)` pays the same XP.
  - `GreenBargainSurvivesSaveLoad`: save with 2 of 3 throne flags → load → stage 1, flags kept, the Frost group still resolved.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `ForcesTests` + `QuestTests` + `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act II: the Fae Courts (the Gloaming Glade, A Quarrel of Courts, The Green Bargain)"`.

### Task 5: The Wyrmwatch: Cragfoot, Calloway, Ember Eyes, The Long Hunt

**Files:** `Maps.cs` (`Cragfoot`, about 46×32; `AshmawLair`, about 26×20, Dark; open the Lantern Road's east edge at rows 14–15), `WorldDb.Forces.cs`, `DialogueDb.Forces.cs` (calloway, odo, cragfoot villagers), `QuestDb.cs`, `MonstersDb.cs`, `ItemsDb.cs`, `LoreDb.cs`, `DragonDb.cs`, tests.

**Interfaces (produces):** zones `cragfoot`, `ashmaw_lair`; NPCs `calloway`, `odo`, `mother_hale` (villager); quests `ember_eyes`, `long_hunt`; monsters `ember_eyed_wolf`, `ashmaw`; items `silver_bell`, `wyrmwatch_bolts`; flags `wyrmwatch_belled`, `banner_hunters`; lore `trophy_wall`.

**Cragfoot** (hand-shaped):
- A hamlet of six cottages on the mountain's foot, a goat pen and a shrine.
- The Wyrmwatch Lodge: a long timber hall with a trophy wall sign. Antlers, ember-eyed beasts' skulls, and in the middle one great old dragon tooth. Lore `trophy_wall`: hunters' tallies, each beast with the village it threatened. Grey: every tally saved someone.
- A training yard with targets, a palisade and a watchtower.
- A goat track climbs north to `ashmaw_lair`.

**Calloway** is weathered, blunt and kind to children. "I don't hate dragons. I've buried too many people to have the energy." His first talk calls `Factions.Hear("hunters")`. Insight 15: he's afraid of your dragon, honestly, and ashamed of it. Dragon opinion: Distrust. The trust system then gives Insight Advantage with him, which reveals that he's sincere: grey on purpose.

**`ember_eyes`** (Calloway):
- Feral ember-eyed wolves have been taking goats at night.
- Stage 1: be in Cragfoot after 20:00 → the gated, NightOnly-spawned group `cragfoot_wolves` (3 `ember_eyed_wolf`; Beasts, TalkDC 15) comes over the palisade. Resolving it → stage 2.
  - If calmed, Calloway: "They'll be back. They always come back." The XP is the same.
- Stage 2: Odo is hurt (a dialogue). Heal him: any healing spell or potion via Give Potion, or a Medicine 12 roll in dialogue → stage 3.
- Stage 3: a quest secret on a fallen wolf's spot (Investigation 12): a worn leather collar with a name tag, "BRAMBLE". It was someone's dog before the ash. Mother Hale knows it. The narration lands softly (PG). → stage 4.
- Stage 4 = done: hunters +10, 1200 XP.
- What it shows: the danger is real, and the beasts were someone's once.
- `ember_eyed_wolf`: SRD Dire Wolf, AC 14, HP 37, Bite +5 2d6+3, Pack Tactics (text), with an ember tint.

**`long_hunt`** (Calloway, major; via `StartMajor`):
- Stage 1: track the Ashmaw. Two quest secrets on the goat track (Survival 14 each, or Nature 14) → stage 2 (`FlagsAtLeast` 2).
- Stage 2: the lair (Dark). The gated `ashmaw_den` group: 1 `ashmaw`, Beasts, Unique, TalkDC 18, ScareDC 17, `TalkSkill AnimalHandling`. Resolved → stage 3.
  - `ashmaw`: SRD Wyvern, AC 14, HP 110, Bite +7 2d6+4, Stinger +7 2d6+4 plus 4d6 poison (Con DC 15, half) as multiattack. **[Claude's call]** The stinger poison is cut from 7d6 to 4d6 for a level 6–7 party; the cut is noted in the docs.
  - Calming it: "It isn't a dragon. It's a hungry, frightened animal with a coal in its eye. You talk it out of its rage, and it flies north, away from every village you know."
- Stage 3: in the den, a wyvern egg. Calloway's order: destroy it. Every option completes the stage → 4:

| Option | Effect |
|---|---|
| Destroy it | hunters +10, dragon bond −1 |
| Leave it | hunters −10, dragon bond +2 |
| Take it to the Academy | hunters −5 (no academy change; its quests are closed), flag `wyvern_egg_saved`, item `wyvern_egg` (quest item; no use yet, an Act III hook) |

- Stage 4: Calloway asks to "bell" the dragon: a silver bell on its collar so any village hears it coming.
  - Accept: `wyrmwatch_belled`, hunters +10, a dragon sulk bark.
  - Refuse: hunters −5.
  - Either → done.
- Done: hunters +30, 3000 XP, `wyrmwatch_bolts` (20 +1 crossbow bolts, or +1 arrows if that's the only ammo type), flag `banner_hunters`.

- [ ] **Step 1: Write failing tests:**
  - `CragfootIsReachable`: Act 2. The road's east warp → `cragfoot`. Every secret, NPC spot and the lair warp are reachable. The lair has the den group when the quest is at 2.
  - `EmberEyesFullRun`: night → the wolves present → a peaceful resolve → Odo healed via Medicine → collar → done. Same XP as a `ResolveGroup(victory)` run.
  - `LongHuntEveryChoiceCompletes`: each egg option × each bell option completes, with the deltas.
  - `LongHuntSurvivesSaveLoad`: save after the Ashmaw is resolved → no Ashmaw on load.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement (pack art for the wyvern if there is one, else a tinted dragon-class monster sprite; goats reuse Act I).
- [ ] **Step 4:** `ForcesTests` + `DragonTrustTests` + `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act II: the Wyrmwatch (Cragfoot, Ember Eyes, The Long Hunt)"`.

### Task 6: Acceptance, screenshots, docs, build

- [ ] **Play-mode `ForcesGoldenPath`** (one test, parameterised by faction ×4): Start at Act II → hear out all four (real dialogue for each leader) → finish that force's first scouting quest via test hooks → commit through its major's giver → every other force's quest dialogue options are hidden and the journal shows them Closed → finish the major via hooks → Answers at 5 → save/load holds everything.
- [ ] **`EveryForceFightHasAPeacefulPath`:** every `EnemyGroupDef` in the new zones (and the new gated road group) has Style ≠ None, a VouchFlag or a ParleyDialogue.
- [ ] **Secret guard:** `LoreTests.NoActTwoTextHintsAtTheSecret` scans DialogueDb.Forces (add it to the scanned set) and the new lore. Run it.
- [ ] **Screenshots** to `%TEMP%/embers_screenshots/act2_forces_*.png`: the Redoubt drill yard, the beacon, the Gloaming Glade's three thrones, Cragfoot by night with the wolves, the Ashmaw's lair, and the commit prompt. Review each place for landmarks (not grid-like or empty).
- [ ] **Docs:**
  - PLAYTEST.md: "New: the four forces", with a checklist.
  - DESIGN.md: hearing out, StartMajor, the quests table, the XP table.
  - LORE_PITCH.md: the new names.
- [ ] **Release:** RulesTests and the full play-mode suite green; rebuild the exe and zip; end-of-branch review; fix pass; merge `act2/phase4-four-forces` into main; push.
