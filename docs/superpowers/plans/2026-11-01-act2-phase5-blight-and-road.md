# Act II Phase 5: The Blight and the Road to the Emberhorn Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The Queen's influence becomes visible in places the party already loves, and the main quest carries the party from its chosen side to the foot of the volcano:
- Visible influence: the blight worsens with the story, townsfolk are frightened and share one dream, ember-eyed beasts roam known zones, and one Thistledown neighbour secretly serves her.
- The road: the Emberhorn Road, with the Second Ideal hidden on it and the chosen force's ally waiting at the camp.

**Architecture:** Same data-driven style as Phases 3–4. New content goes in `WorldDb.Emberhorn.cs` and `DialogueDb.Blight.cs`. Small engine additions:
1. `Blight.StoryStage` + `Blight.Sync()`: the stage follows "Answers".
2. `EnemyGroupDef.MinBlight`.
3. `QuestDef.StageFlags`: reaching a stage sets a flag.
4. The dragon's blight bark.
5. The Second Ideal's boon on long rest.

**Tech Stack:** C# (Unity 6000.5.9f1), tools/RulesTests (.NET 8), Unity play-mode tests (`.superpowers/unity-test.sh [filter]`; the full suite is ~20 min, so run it in the background). Art via `Assets/Editor/ArtMap.txt` + `.superpowers/import-art.sh`.

**Spec:** docs/superpowers/specs/2026-10-31-act2-design.md (Intent: a big bad "whose influence they can see on the world"; Goal 6 main quest; Goal 7 blight; the teenage dragon "reacts to the blight"; the builder-check decisions: blight staged by main-quest progress, the Second Ideal "as a hidden inscription with one small boon", the chosen force "gives its own flavour to the main quest … who travels with you, what help you get"). Roadmap item 5 in docs/superpowers/plans/2026-10-31-act2-phase1-levels.md.

## Global Constraints

- **THE SECRET:** nothing may hint that the Ashen Queen is a child, or that the Crown of old did it. `LoreTests.NoActTwoTextHintsAtTheSecret` stays green. The shared dream, the Ashsworn neighbour and the dragon's barks speak of a door, a mountain, fire and waking. Never a voice, crying, fear of her, or size.
- Tone: unsettling, not gory or horror (spec anti-goal). Townsfolk are worried, never hopeless.
- **Act I stays light:** nothing in this phase happens before Act II. `ActOneTests` and the Act I XP audit stay unchanged. Blight stays 0 in Act I.
- Every force stays grey. The ally the chosen force sends is helpful and still has their own agenda.
- Peaceful options pay the same XP as fighting. Dice are always visible. Every new NPC gets a weekly habit; every outdoor door gets a sign.
- **Spec cut order:** the dragon's mid-act growth is cut (cut #3). Blight variety is kept small (cut #4): overlay, cold lights, dialogue, beasts.
- Branch `act2/phase5-blight-road`. Merge and push after the full suite passes. Names are working names; list them in LORE_PITCH.md.

## Names (working)

- **The Emberhorn Road:** the high road east of Cragfoot, up to the mountain's gate. On it:
  - **the Oathbound waystation** (the Second Ideal)
  - **the Last Camp**, where your ally waits
  - an Ashsworn camp, **the Grey Vigil**
- **Teodor Brisk**, Thistledown's chandler, who has been marking doors with ash.
- **The Second Ideal:** "I will stand where the dark is deepest, so that none need stand there alone."

## Blight stages (story-driven)

| Stage | When | What you see |
|---|---|---|
| 0 | Act I | nothing |
| 1 | Act II begins (Answers ≥ 1) | light ash tint; hearths and forges dimmed; the stage-1 lines |
| 2 | the party has chosen a side (Answers ≥ 5) | heavier tint; drifting ash; ember-eyed beasts in the Whisperwood and Miller's Ford; the shared dream; Ash on the Doors |
| 3 | on the Emberhorn Road (Answers ≥ 7) | heaviest tint; ember-eyed hounds near Starfall Crater |

`BlightEased` (Phase 6) lowers the story stage by one.

## Review Focus

- **Saves from Phases 2–4 in Act II with blight 0:** after load the stage matches the story (Answers 4 → 1, Answers 5 → 2). It never rises in Act I. Test: `BlightFollowsTheStoryOnLoad` (Task 1).
- **Eased blight never creeps back:** with `BlightEased`, every later sync gives the story stage − 1, never more. Test: `EasedBlightStaysEased` (Task 1).
- **Blight beasts and quest gates together:** a group with both `MinBlight` and a QuestId needs both. Blight beasts never spawn on top of an Act I story group, so a known zone keeps working for unfinished Act I quests. Test: `BlightBeastsOnlyAtTheirStage` (Task 2).
- **Ash on the Doors when Teodor is jailed or gone:** his schedule follows his fate. Jailed: in the watchhouse cell. Fled: gone from the map. Redeemed: back at his stall. His dialogue never offers the stakeout again. Test: `TeodorEveryFate` (Task 3).
- **The Emberhorn Road for each force, and in co-op:** each force's ally appears at the Last Camp and gives their gift once (not once per player, not again after load). Test: `TheAllyMatchesYourSide` (Task 4).

---

### Task 1: Engine: the blight follows the story; blight-gated beasts; stage flags; the dragon feels it

**Files:**
- `World/Blight.cs` (StoryStage, Sync)
- `World/WorldDb.cs` (`EnemyGroupDef.MinBlight`, folded into `InStage`)
- `Quests/QuestDb.cs` (`QuestDef.StageFlags`)
- `Quests/QuestLog.cs` (apply StageFlags in Set; call `Blight.Sync` on Changed)
- `Core/GameRoot.ActTwo.cs` (`ApplyActCap` calls Sync)
- `World/DragonPet.cs` (the bark)
- Tests: `Assets/Tests/PlayMode/BlightStoryTests.cs` (new; SetUp/TearDown as ForcesTests)

**Interfaces (produces):**
```csharp
public static class Blight {
    public static int StoryStage(WorldState w, QuestLog q);  // 0 in Act I; else 1 / 2 (Answers >= 5) / 3 (Answers >= 7); minus 1 if BlightEased (min 0)
    public static void Sync();                               // SetStage(StoryStage(..)) if it differs
}
public class EnemyGroupDef { public int MinBlight; }          // present only while World.BlightStage >= MinBlight (and InStage as before)
public class QuestDef { public Dictionary<int, string> StageFlags = new Dictionary<int, string>(); } // reaching a stage sets the flag (once)
```
- `DragonPet`: on entering a zone whose `Blight.OverlayAlpha` is ≥ 0.15, once per zone per day (`World.Lock("blightbark:" + zone)`), narrate `DragonDb.BlightBark(name)` to players in that zone and show the "?" emote. Teen stage only.

- [ ] **Step 1: Write failing tests:**
  - `BlightFollowsTheStoryOnLoad`:
    - Act 1 → stage 0.
    - `ApplyActTwoStart` → 1.
    - Set Answers 5 → 2. Set 7 → 3.
    - A save with Answers 5 and `BlightStage = 0` → after load, 2.
  - `EasedBlightStaysEased`: Answers 7, `Blight.Ease()` → 2. Quests.Set another quest (a Changed event) → still 2.
  - `MinBlightGatesAGroup`: a test group with MinBlight 2 and no quest. `InStage` is false at stage 1 and true at stage 2.
  - `StageFlagsAreSet`: a test quest with `StageFlags[2] = "f"`. Set 2 → flag `f` set.
  - `DragonFeelsTheBlight`: Act II, stage 2. Warp to `forest` → the narration contains "goes still, nose up" once. Warp out and back the same day → no second bark.
- [ ] **Step 2:** `.superpowers/unity-test.sh BlightStoryTests` → FAIL (compile).
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `BlightStoryTests` + `BlightTests` + `ActOneTests` + `ActTwoTransitionTests` → PASS.
- [ ] **Step 5:** Commit `"Act II: the blight follows the story; blight-gated beasts; stage flags; the dragon feels the ash"`.

### Task 2: Known places under the blight: frightened townsfolk, the shared dream, ember-eyed beasts

**Files:** `Dialogue/DialogueDb.Blight.cs` (new: `AddBlightLines(id, d)` hooked from `DialogueDb.Get` like `AddForceLines`), `World/WorldDb.Emberhorn.cs` (new: `BuildBlightBeasts()`), `Quests/LoreDb.cs`, tests in BlightStoryTests.

**Interfaces (produces):** lore `shared_dream`; flags `dream:<npc>`; groups `blight_wolves_forest`, `blight_boars_ford`, `blight_hounds_crater`.

**Frightened townsfolk:** each NPC's start node gets a variant first line while `c.Blight >= 1`, and a second at `>= 2`. Both keep the NPC's voice. Use the existing `DCtx.Blight` pattern from Garrick/Pip.
- NPCs: Brannoc, Elowen, Rhoswen, Tobin, Maddie, Hesper, Dunstan, Lark, Tamsin, Wick, Hobb, Wren (Garrick and Pip already have theirs).
- Tone examples:
  - Elowen (stage 1): "The herbs come up grey at the tips. I've been singing to them. It isn't working, but it's helping me."
  - Maddie (stage 2): "I'm not scared. Buoy's scared. I'm holding his paw so HE'S not scared."
- **Buoy:** at blight ≥ 2 his pet line becomes "Buoy leans his whole weight against your legs and stays there until the ash stops falling."

**The shared dream** (blight ≥ 2): five townsfolk can tell it in their own words, as an extra option "You look tired." on their start node: Elowen, Tobin, Hesper, Wren and Hobb.
- The dream is always the same: "a great grey door under the mountain, warm to the touch, and something on the other side knocking, slow and steady, as if it has all the time in the world."
- Hearing it sets `dream:<npc>`. On the third different teller, record lore `shared_dream` ("The Same Dream", category Legend): the valley dreams one dream, and the old folk say their grandparents dreamed it too.

**Ember-eyed beasts in known places** (each Beasts style, peaceful = same XP, not Unique: they come back like other wild groups):

| Group | Members | Zone | MinBlight | TalkDC |
|---|---|---|---|---|
| `blight_wolves_forest` | 3 `ember_eyed_wolf` | Whisperwood, an open glade away from Act I story groups | 2 | 15 |
| `blight_boars_ford` | 2 `ember_eyed_boar` | Miller's Ford fields | 2 | 14 |
| `blight_hounds_crater` | 2 `cinder_hound` | Starfall Crater rim | 3 | 15 |

- [ ] **Step 1: Write failing tests:**
  - `TownsfolkFearTheAsh`: Act II stage 1, then 2. For each NPC listed, `Get(id).Nodes[start].Text(c)` differs between stage 0, stage 1 and stage 2. The Act I text is unchanged at stage 0.
  - `ThreeTellersMakeTheDream`: stage 2. Choosing the dream option for Elowen and Tobin → no lore. Hesper → lore `shared_dream`. The option is hidden at stage 1.
  - `BlightBeastsOnlyAtTheirStage`: stage 1 → the three groups are not InStage. Stage 2 → wolves and boars are; hounds aren't. Stage 3 → all three. Each spawn tile is walkable and reachable from the zone's DefaultSpawn. None is within 6 tiles of an existing Act I group's members.
  - `BuoyLeansAtStageTwo`: Buoy's pet narration contains "leans his whole weight" at stage 2 only.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `LoreTests` + `SmokeTests.WorldDataIsConsistent` + `QuestTests`.
- [ ] **Step 5:** Commit `"Act II: Thistledown under the blight (frightened townsfolk, the shared dream, ember-eyed beasts)"`.

### Task 3: Ash on the Doors (one neighbour secretly serves the Queen)

**Files:** `WorldDb.Emberhorn.cs` (`BuildTeodor()`: NPC, schedule, quest spots in `town`), `DialogueDb.Blight.cs` (`teodor`, and Rhoswen's quest start), `QuestDb.cs`, `LoreDb.cs`, tests.

**Interfaces (produces):** NPC `teodor` ("Teodor Brisk", chandler; a free market stall or bench spot in the square by day, home at night); quest `ash_on_doors`; flags `teodor_redeemed` / `teodor_jailed` / `teodor_fled`.

**`ash_on_doors`** ("Ash on the Doors", giver Rhoswen, Act 2, no faction):
- Offered by Rhoswen at blight ≥ 2: "Somebody's been marking doors with ash. A hand, pressed flat. People are frightened enough."
- Stages:
  1. Find the marks: three daytime quest spots on walkable tiles beside three town doors (Investigation 10), setting `ash_mark_1..3`. `AllFlags` → stage 2.
  2. "Watch the square after dark": a NightOnly quest spot in the square (Perception 13). Success opens dialogue `teodor_caught` (`SecretDef.Dialogue`).
  3. Complete (900 XP). Reached by any resolution of the dialogue.
- **Teodor**, caught with a bowl of ash: "The hearths are going out. Mine first. The ones who meet at the beacon say when she wakes, the cold ends. I just want the cold to end." Grey: he's frightened and wrong, not evil. No hint of the secret.
- Every resolution completes the quest:

| Resolution | Effect |
|---|---|
| (Persuasion 15) talk him down | `teodor_redeemed`: he scrubs the doors, stays at his stall, and later sells candles that "stay lit a little longer" |
| (Insight 14) "Who told you that?" | the same as talking him down, plus lore `ashsworn_creed` |
| "Come with me to the Captain." | `teodor_jailed`: the watchhouse cell. Rhoswen is grateful and sad. |
| "Go. Don't come back." | `teodor_fled`: he's gone from the map |

- If the party stood with the Crown and `banner_crown` is set, Rhoswen mentions the Redoubt would want to know. Flavour only.

- [ ] **Step 1: Write failing tests:**
  - `AshOnTheDoorsRuns`: blight 2. Rhoswen's option is shown (hidden at blight 1). Three marks → stage 2. The night spot isn't present at noon; at 22:00 it is, and success opens `teodor_caught`.
  - `TeodorEveryFate`: each of the four resolutions completes the quest once (XP once) and sets its flag. `WorldDb.Current(teodor, …)` is the stall (redeemed), the watchhouse cell (jailed) or null (fled). The stakeout spot never returns.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `LoreTests` + `QuestTests` + `WorldDataIsConsistent`.
- [ ] **Step 5:** Commit `"Act II: Ash on the Doors (Teodor Brisk)"`.

### Task 4: The road to the Emberhorn, and your side's ally

**Files:** `World/Maps.cs` (`EmberhornRoad`, about 60×30; Cragfoot's east edge opens at rows 15–16), `WorldDb.Emberhorn.cs` (`BuildEmberhornRoad()`), `DialogueDb.Blight.cs` (the four allies), `QuestDb.cs` (Answers 5–8), `MonstersDb.cs` (if needed), `LoreDb.cs`, ArtMap, tests in `Assets/Tests/PlayMode/EmberhornTests.cs` (new).

**Interfaces (produces):** zone `emberhorn_road` ("The Emberhorn Road", `Act 2`, `BlightWeight 0.8`); flag `emberhorn_open`; `Answers.Allies = 6, OnTheRoad = 7, Gate = 8` (`Answers.Road` is already stage 1); NPC ally ids `ally_holloway`, `ally_perrin`, `ally_thimble`, `ally_odo` (each a separate NpcDef with RequiresFlag `banner_<force>`, standing at the Last Camp); groups `grey_vigil` and `road_cinder_hounds`.

**Answers, continued** (stage numbers keep their meaning):

| Stage | Journal text | Reached by |
|---|---|---|
| 5 | "You've chosen a side. Finish what your allies asked of you." | committing (existing) |
| 6 | "Your allies agree on one thing: the answers are in the Emberhorn, the mountain whose fire has gone grey. The high road east of Cragfoot is open." | `FlagsAtLeast(5, 6, 1, banner_crown, banner_academy, banner_fae, banner_hunters)`; `StageFlags[6] = "emberhorn_open"` |
| 7 | "Cross the Emberhorn Road to the mountain's gate. Someone from your side is waiting at the Last Camp." | `ZoneEntered emberhorn_road` 6→7 |
| 8 | "The Emberhorn's gate. (The mountain waits in a later update.)" | quest spot `emberhorn_gate` (Perception 5) at the gate, 7→8 |

- StageXp: {6: 500, 8: 1500}.

**Cragfoot → road:**
- Warps at Cragfoot's east edge (45,15)/(45,16): `MinAct 2`, `RequiresFlag emberhorn_open`.
- ClosedText: "The high road east is buried under a fresh rockslide, still smoking. The hunters say it'll take a reason, and a crew, to clear it."
- Stage 6 sets the flag, so the hunters cleared it "for you".

**The Emberhorn Road** (hand-shaped):
- Grey ash slopes `d`, black rock `r`, cliffs `X`, and a switchback path climbing east.
- The **Last Camp**: tents, a campfire, and your ally.
- The **Oathbound waystation**: ruined pillars `P` and an altar, with the Second Ideal (Task 5).
- The **Grey Vigil**, an Ashsworn camp: group `grey_vigil`, 3 `ashsworn_zealot` + 1 `ashsworn_keeper`, Talk, TalkDC 17, Unique.
- A pack of `cinder_hound` ×3, `road_cinder_hounds`, Beasts, TalkDC 15.
- One scorched grove secret (Nature 14, lore `grey_grove`).
- The **gate**: a great cliff face with a carved door far up the slope. Its tile has the quest spot `emberhorn_gate` and a closed warp (`MinAct 3`, ClosedText "The way into the mountain is open, and hot, and waiting. (Act II's finale arrives in a later update.)").

**The ally and their help** (once only for the party: `World.Flag("ally_gift")`):

| Force | Ally | Gift | Flavour |
|---|---|---|---|
| crown | Sergeant Holloway | 3 `potion_greater_healing` | "Marsh sent me. Said I've been asking to get out of that checkpoint. Careful what you wish for." The Grey Vigil's keeper recognises his tabard (an extra line). |
| academy | Perrin | 1 `ember_of_return` + 2 `potion_greater_healing` | Ashby sent him with notes. He's terrified and determined. |
| fae | Mister Thimble | 3 `bloom_draught` | "The Courts want their bargain kept. I want to see a volcano. Everyone wins!" If `fae_clause`: "Remember, the little flame owes us one." |
| hunters | Odo Fenn | 2 `potion_greater_healing` + the road's hidden spots sparkle (all secrets in the zone `MarkSniffed`) | "Calloway said, and I quote, 'Don't die, Odo.'" |

- The allies don't fight; they're camp company. Phase 6 brings them to the mountain.
- Dragon opinions:

| Opinion | Who |
|---|---|
| Fond | ally_perrin, ally_thimble |
| Wary | ally_holloway, ally_odo |

- [ ] **Step 1: Write failing tests:**
  - `HighRoadOpensWithYourBanner`: Act 2, Answers 5. The Cragfoot east warp is closed with its text. Set `banner_fae` → Answers 6, `emberhorn_open`, the warp is open. Entering the road → Answers 7. Every secret, group, NPC spot and warp is reachable.
  - `TheAllyMatchesYourSide`: for each force (Commit + banner flag), only that ally is present at the Last Camp. Their gift node gives the gift once. A second talk, a second player and a save/load give nothing more. With `fae_clause`, Thimble's line mentions the favour.
  - `GateEndsTheRoadForNow`: the gate spot → Answers 8 with 1500 XP once. The gate warp stays closed with its text.
  - `PhaseFourSaveContinues`: a save at Answers 5 with `banner_hunters` already set → on load, Answers advances to 6 and `emberhorn_open` is set.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement. Pick pack art for the road (scorched tiles exist). Music `music_emberhorn` = "Audio/Musics/10 - Dark Castle.ogg" ("24 - Final Area" is saved for the volcano).
- [ ] **Step 4:** PASS + `ForcesTests` + `AcademyTests` + `WorldDataIsConsistent`.
- [ ] **Step 5:** Commit `"Act II: the Emberhorn Road (Answers to the gate, your side's ally, the Grey Vigil)"`.

### Task 5: The Second Ideal

**Files:** `WorldDb.Emberhorn.cs` (the waystation secrets), `World/WorldDb.Places.cs` (`SecondIdeal` const beside `FirstIdeal`), `Core/GameRoot.cs` (long-rest hook), `Quests/LoreDb.cs`, tests in EmberhornTests.

**Interfaces (produces):** `WorldDb.SecondIdeal` = "I will stand where the dark is deepest, so that none need stand there alone."; flag `oath_second`; lore `second_ideal`.

- The waystation altar has two secret routes: History 14 ("Read the worn carving") or Religion 14 ("Kneel and say the First Ideal"). The Religion route shows only with `secret:tower_inscription`, and is DC 10 then.
  - Success text quotes the Second Ideal, and sets `oath_second` + lore `second_ideal`: the Oathbound swore it at the edge of the mountain's shadow. Neutral, with no culprit.
- **The small boon:** while `oath_second` is set, every player who finishes a long rest gains Heroic Inspiration, with the toast "The Second Ideal steadies you. (Heroic Inspiration)". Find the existing long-rest completion point in GameRoot (where HP and slots refill).
- **Echoes:** Rhoswen gets one line if `oath_second` and the watchtower quest is done ("Two of them now. Someone should be writing these down. ...I am. I'm writing them down.").

- [ ] **Step 1: Write failing tests:**
  - `SecondIdealByEitherRoute`: the History route sets `oath_second` and the lore. A fresh run with `secret:tower_inscription` shows the Religion route at DC 10. Without it, Religion is hidden.
  - `SecondIdealInspiresOnLongRest`: two players, `oath_second`. A long rest → both have Heroic Inspiration. Without the flag → no change.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** PASS + `RestTests` (or whichever suite covers long rests) + `LoreTests`.
- [ ] **Step 5:** Commit `"Act II: the Second Ideal of the Oathbound"`.

### Task 6: Acceptance, screenshots, docs, build

- [ ] **Play-mode `BlightAndRoadGoldenPath`:** Start at Act II → blight 1.
  - Commit to the Wyrmwatch → blight 2: the town lines change, and the beasts appear in the forest.
  - Finish the Long Hunt via hooks → Answers 6 → take the high road → blight 3, Answers 7 → Odo's gift.
  - Find the Second Ideal → the gate → Answers 8.
  - Save, load: everything holds.
- [ ] **Screenshots** (`act2_blight_*`): Thistledown at blight 2 by day; Whisperwood at blight 2; the Emberhorn Road at the Last Camp, the waystation and the Grey Vigil; the gate. Review for "unsettling, not grimdark", and for landmarks.
- [ ] **Secret guard** green; Act I tests green.
- [ ] **Docs:**
  - PLAYTEST.md: "New: the blight and the Emberhorn Road", with a checklist.
  - DESIGN.md: the blight table, MinBlight, StageFlags, the allies table, the Second Ideal.
  - LORE_PITCH.md: the new names, the dream, the Second Ideal.
- [ ] **Release:** RulesTests + the full suite green; build the exe + zip; end-of-branch self-review; fix pass; merge; push.
