# Act III Phase 5: The Turn Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The party comes back up from beyond the door, and the whole capital is waiting at the Sunstair: King Aldric, Marsh, Ashby, Sorrel and Calloway. In a real conversation with dice, the King is made to confess. Each force says what it wants done with her, and the party can win leaders over. Then the party makes the three-way choice: end her, seal her, or save her. The save path is gated, but the gate is generous and says exactly what's missing and where to get it. The choice breaks the forces into allies and enemies through a reaction table built from Act II state. Phase 5 ends at "the break". Phase 6 builds the fights.

**Architecture:**
- **Everything that names her or the endings lives in exempt `*.AfterTheDoor.cs` files.** Each one contains `MetHer`, and the scene only runs after `MetHer` and `meeting_seen` are set:
  - `GameRoot.TheTurn.AfterTheDoor.cs`: the trigger, the scene, the choice and the break;
  - `DialogueDb.TheTurn.AfterTheDoor.cs`: dialogue `questioning`;
  - `WorldDb.TheTurn.AfterTheDoor.cs`: the five questioning NPCs;
  - `TurnDb.AfterTheDoor.cs`: the reaction table, the save gate and betrayal (pure logic over `WorldState`).
- **The questioning happens outdoors on the Sunstair in the Great Square (zone `capital`), not in the throne room.** The throne room is `DragonStays`, and the spec wants the dragon present and reacting. This is also where Phase 6's fight through the streets begins.
- **The five speakers are new NpcDefs** (`king_q`, `marsh_q`, `ashby_q`, `sorrel_q`, `calloway_q`) that share dialogue `questioning`:
  - Each one's only schedule entry requires `Turn.Questioning` and forbids `Turn.Chosen`.
  - The originals (`king_aldric` and the four `*_capital` leaders) get `GoneFlag = Turn.Questioning`.
  - This avoids `DialogueDb.Get`'s per-id cache and needs no schedule-priority change.
- The three-beat MessageScreen loop from `MeetingScene` is pulled out into `PlayBeats`, which the questioning and the break reuse.

**Tech Stack:** C# (Unity 6000.5.9f1).
- Rules tests: `dotnet run --project tools/RulesTests`.
- Play-mode tests: `.superpowers/unity-test.sh [filter]`. The full suite takes ~25 min, so run it in the background.

**Spec:** docs/superpowers/specs/2026-11-02-act3-design.md (§3 "Back to the capital", "The three endings", "The four forces in Act III", "The finale must not feel rushed": real dialogue at the turn, anti-goals). Roadmap item 5 is in docs/superpowers/plans/2026-11-02-act3-phase1-levels.md. Phase 4's outputs (flags `her_truth`, `seal_understood`, `ring_link`, `knocked_back`, `promised_her`, `meeting_seen`) are in docs/superpowers/plans/2026-11-02-act3-phase4-lair.md, Task 5.

## Global Constraints

- **THE SECRET:**
  - `LoreTests.NoActTwoTextHintsAtTheSecret` stays green.
  - QuestDb stage texts for warm_city 7–9 must pass the guard (no "child", "girl", "little"...). They speak of "what you found below".
  - Every line that names Edda or the endings lives in a `*.AfterTheDoor.cs` file that contains `MetHer`.
- **Only three endings (user):** `Turn.End = "end"`, `Turn.Seal = "seal"`, `Turn.Save = "save"`. No fourth option. "Not yet" defers the choice and never counts as a choice.
- **Any ending is choosable, including betraying your own side.** Betrayal has consequences (Task 2).
- **The game never says which ending is right.** No option is marked "good". The gate's copy is about what the party *knows*, not about virtue.
- **Not rushed (spec anti-goal):** the decide node opens only after the confession and all four leaders have spoken. A party that skips nothing sees ≥ 10 dialogue nodes before it can choose.
- **The truth about the Crown comes out on every path:** `king_confessed` is set on every route to `decide`, including a successful lie and a failed confrontation.
- **Co-op:**
  - Every ready player is gathered and sees every MessageScreen beat.
  - The other player sees each dialogue node's line echoed in their log.
  - The final choice needs **every ready player** to confirm. One "Not yet" cancels it for everyone.
- Dice are always visible. Every window closes with Esc/B and has a ✕, except the beats (MessageScreen, any key) and the confirm (cancellable = "Not yet").
- Tone: bright fantasy, PG-13. The King is guilty and tired, not a cackling villain. Leaders are grey. Edda is never blamed.
- Old saves still load: all state is world flags. No new save fields.
- Working names are approved in bulk; list new ones in LORE_PITCH.md.
- Branch `act3/phase5-turn`. Ledger `.superpowers/sdd/2026-11-02-act3-phase5-turn/progress.md`. Merge to main and push after the full suite passes.

## Names and flags

```csharp
// Quests/QuestDb.cs (guard-scanned: no story words beyond these)
public static class WarmCity { /* existing */ public const int Called = 8, Break = 9; }
// Quests/TurnDb.AfterTheDoor.cs
public static class Turn
{
    public const string End = "end", Seal = "seal", Save = "save";
    public const string Questioning = "questioning", Confessed = "king_confessed", Chosen = "turn_chosen";
    public static string EndingFlag(string ending) => "ending_" + ending;     // exactly one is set at the choice
    public static string Swayed(string force) => "swayed_" + force;           // force ids from FactionDb
    public static string Heard(string force) => "turn_heard_" + force;        // that leader has spoken at the questioning
    public const string FaeFavourCalled = "fae_favour_called", Betrayed = "betrayed_side";
    public static string Ending(WorldState w);                                // null until chosen
}
public enum Stance { Against, Aside, With }
```

`WorldDb.SunstairSpot = (31,13)` is where the party gathers. The speakers' tiles are below; each must be walkable (`CapitalTests` already checks every NPC spot):
- king (31,10)
- marsh (28,11)
- ashby (29,12)
- sorrel (33,12)
- calloway (34,11)

If any tile isn't walkable, shift it by one tile and ledger the change.

## Review Focus

1. **Save/load mid-questioning.**
   - `Questioning` is set and saved before the first beat.
   - Loading in the square never replays the beats. Talking to any speaker resumes at the right node: `open` if never opened, `voices` if mid-way, `decide` once everyone has spoken.
   - Sway and heard flags survive.
   - The choice is atomic: flags are set, then saved, then the break plays. A load after the choice never offers the choice again.
   - Test: `QuestioningSurvivesSaveLoad` (Task 3).
2. **Co-op agreement.**
   - P2 in combat elsewhere → the questioning refuses, nobody moves, and it retries on the next entry to `capital`.
   - At the choice, P1 confirming alone does nothing until P2 confirms. P2's "Not yet" closes both prompts and sets nothing.
   - Solo needs one confirm.
   - Test: `ChoiceNeedsEveryoneToAgree` (Task 4).
3. **The save gate is never a hidden fail state.**
   - Short of the gate, the Save option is still listed, marked with what's missing, and opens `gate_short`. That node names each missing piece and where to get it.
   - "Not yet" lets the party leave, go back down to Edda, return, and resume.
   - Test: `SaveGateSaysWhatIsMissingAndCanBeMet` (Task 3).
4. **Old saves and odd routes.**
   - A Phase 4 save made standing in the capital at warm_city 7 starts the questioning after loading (`CheckTheTurn` runs after load, not only on zone entry).
   - Surfacing through the crypt shortcut or the roots stair both reach it.
   - Start at Act III never triggers it without `meeting_seen`.
   - Test: `TheTurnStartsOnSurfacingAndAfterLoad` (Task 1).
5. **The secret guard over new files.**
   - New QuestDb text passes the guard.
   - The four new `*.AfterTheDoor.cs` files contain `MetHer`.
   - The five speakers are absent from every schedule query until `Questioning` is set. The originals are present until then and gone after.
   - Tests: `TurnFilesAreGated` (Task 1) and `SpeakersOnlyAtTheQuestioning` (Task 1).

---

### Task 1: Surfacing, warm_city 8–9, the speakers, and `PlayBeats`

**Files:**
- `Quests/QuestDb.cs` (warm_city 7 text, stages 8–9, XP)
- `Quests/TurnDb.AfterTheDoor.cs` (new; `Turn` constants only in this task)
- `World/WorldDb.TheTurn.AfterTheDoor.cs` (new: `BuildTheTurn()`, called from the static ctor after `BuildBeyondTheDoor()`)
- `World/WorldDb.Capital.cs` (`SunstairSpot`)
- `Core/GameRoot.AfterTheDoor.cs` (extract `PlayBeats`)
- `Core/GameRoot.TheTurn.AfterTheDoor.cs` (new: `CheckTheTurn`, `BeginQuestioning` skeleton)
- `Core/GameRoot.cs` (call `CheckTheTurn()` where `QuestEvent.ZoneEntered` is raised in `Warp`, and once after a save loads)
- Tests: `Assets/Tests/PlayMode/TurnTests.cs` (new; SetUp/TearDown and helpers as LairTests) and `LoreTests.cs`

**Interfaces (produces):**
```csharp
public IEnumerator GameRoot.PlayBeats(PlayerSlot by, string[] beats, System.Func<int, IEnumerator> before = null)
    // MeetingScene's loop, unchanged in behaviour: each beat goes to every ready player; it advances when the lead closes theirs;
    // the others' copies close; `before(i)` runs before beat i (MeetingScene passes the dragon's walk for i == 2).
public void GameRoot.CheckTheTurn()   // stage 7 or 8 && meeting_seen && !Questioning && some ready player is in "capital" → BeginQuestioning(that player)
public void GameRoot.BeginQuestioning(PlayerSlot by)
```

**warm_city:**
- Text:
  - 7 (rewrite): "You opened the door, and what was behind it changes everything. Now you have to go back up and tell them."
  - 8: "All of Aurelhold is waiting at the Sunstair: the King, Marsh, Ashby, Sorrel and Calloway. They want to know what you found below, and what you mean to do about it."
  - 9: "The choice is made. Steel is coming out all over the Great Square."
- StageXp: 8 → 4000, 9 → 8000.

**`BeginQuestioning(by)`** (this task covers only steps 1–4; Task 3 adds the beats and the dialogue):
1. If any ready player is InCombat, narrate "Not with a fight still going." to `by` and stop. `CheckTheTurn` retries on the next entry.
2. `Quests.Set(warm_city, Called)` if lower. Set `Turn.Questioning`. Then `SaveSystem.Save(true)`.
3. `GatherAt("capital", SunstairSpot)`.
4. Place the dragon beside the lead player.

**Speakers** (`BuildTheTurn`):
- Each is an NpcDef with `Dialogue = "questioning"`, the original's Look, Name and Title, and one entry: `new ScheduleEntry(0, "capital", x, y, activity) { RequiresFlag = Turn.Questioning, ForbidFlag = Turn.Chosen }`.
- Activities: "on the Sunstair, waiting for an answer" (king) and "at the foot of the Sunstair, watching you" (leaders).
- Set `GoneFlag = Turn.Questioning` on `king_aldric`, `marsh_capital`, `ashby_capital`, `sorrel_capital` and `calloway_capital`.
- The file's header comment explains why the defs are separate, and the file mentions `WorldDb.MetHer` (the gate rule).

- [ ] **Step 1: Write failing tests** in TurnTests:
  - `TheTurnStartsOnSurfacingAndAfterLoad`:
    - Start at Act III, then set warm_city 7, `MetHer` and `meeting_seen`, standing in `roots`.
    - Step onto the roots→capital warp → warm_city 8, `Questioning`, the player within 2 tiles of `SunstairSpot`.
    - Separately, with the same flags, save while standing in `capital` and load → the questioning has begun.
    - Separately, Start at Act III at stage 1 entering `capital` → no `Questioning`.
  - `QuestioningWaitsForAFight`: P2 InCombat in `forest`. P1 enters `capital` at stage 7 → no `Questioning`, P1's log has "Not with a fight still going.", P2's zone is unchanged. P2's fight ends and P1 re-enters → it begins and both players are in `capital`.
  - `SpeakersOnlyAtTheQuestioning`: for every hour 0–23 and weekday 0–6:
    - Before `Questioning`: each of the five `*_q` resolves to null via `WorldDb.Current`.
    - After: each `*_q` is at its tile in `capital`, and the five originals are null.
    - After `Turn.Chosen`: the `*_q` are null again.
  - `MainQuestReachesThirteenAtTheTurn`: `85000 + sum(warm_city StageXp) >= PlayerCharacter.XpThresholds[13]` (120000).
  - `MeetingSceneStillPlaysEveryBeat`: rerun LairTests' `MeetingSceneNeverSkipsABeat` after the refactor; it stays green without edits.

  In LoreTests, add `TurnFilesAreGated`: the four new files exist, end in `.AfterTheDoor.cs` and contain `MetHer`. The QuestDb guard scan still passes with the new stage text.
- [ ] **Step 2:** Run `.superpowers/unity-test.sh TurnTests` → FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** `TurnTests`, `LairTests` and `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: back up into the light; the city waits at the Sunstair"`.

### Task 2: TurnDb: the reaction table, the save gate, and betrayal

**Files:**
- `Quests/TurnDb.AfterTheDoor.cs`
- Test: `Assets/Tests/PlayMode/TurnTests.cs` (pure-logic tests over a fresh `WorldState`; no scene needed)

**Interfaces (produces):**
```csharp
public static class TurnDb
{
    public static Stance Reaction(string force, string ending, WorldState w);
    public static bool MarshDefects(string ending, WorldState w);            // ending != End && (banner_crown || Swayed("crown"))
    public static List<string> SaveGateMissing(WorldState w);                // empty = Save is open; each string is player-facing copy
    public static bool Betrays(string ending, WorldState w);                 // the committed force's Reaction is Against
    public static string BreakLine(string force, Stance s, string ending, WorldState w); // one leader line for the break
}
```

**Base table** (`Reaction` before modifiers):

| force | end | seal | save |
|---|---|---|---|
| crown | With | Against | Against |
| academy | Aside | With | Against |
| fae | Against | Aside | With |
| hunters | With | Aside | Against |

**Modifiers.** Each applies once and moves the stance one step toward With (capped at With), in this order:
1. **The crown row never moves on seal/save.** The throne is the enemy unless you end her. Marsh's defection is `MarshDefects`, not a change in stance. On end, the crown is already With.
2. `banner_<force>`: your own side's loyalty.
3. `Swayed(force)`: won over at the questioning.
4. Hooks:
   - `wyrmwatch_belled` → hunters, on save only;
   - `wyvern_egg_saved` → academy;
   - `marsh_asks_ashby` → academy, but only when `MarshDefects` is true for that ending.
5. **Floor:** `fae_clause` → the fae are never below Aside. The Courts have a claim to collect and want the dragon alive.

**Save gate** (`SaveGateMissing`). The exact copy goes in the list:
- `!(seal_understood || Swayed("academy"))`:
  "You don't understand how the fire is held in her. (Look at the fire in her with Arcana, or win Ashby over.)"
- `!(Swayed("fae") || FaeFavourCalled || LoreFound "the_great_ones_below")`:
  "Nobody here knows how the old Pact worked. (Win Sorrel over, or read the ring of the Great Ones in the Deep.)"
- `!promised_her`:
  "She doesn't know anyone is coming back for her. (Go back down and promise her.)"

**Betrayal consequences** are applied in Task 4, but defined here: if `Betrays` is true, set `Turn.Betrayed`, call `Factions.Adjust(committed, -50)`, and `BreakLine` for that force uses its betrayal variant.

**`BreakLine` copy** (keyed by force × stance; `{name}` is the dragon):
- crown:
  - With: "Marsh draws her sword and stands beside you. \"For the realm. God help us all.\""
  - Against: "\"Guards.\" The King doesn't raise his voice. He doesn't need to."
- crown, when `MarshDefects`: "Marsh looks at the King for a long moment. Then she unpins the royal badge from her collar, drops it on the Sunstair, and walks down the steps to stand with you. A dozen of the Household Guard follow her."
- academy:
  - With: "Ashby rolls up his sleeves. \"Well. I suppose I'd better be useful.\""
  - Aside: "Ashby shakes his head and steps back. \"I won't stop you. I won't help you either.\""
  - Against: "Ashby raises his staff, and his hands are shaking. \"I'm sorry. I can't let you do this.\""
- fae:
  - With: "Sorrel laughs, delighted, and the flowers in the market all turn to face you."
  - Aside: "Sorrel shrugs. \"The Courts will watch. We're very good at watching.\""
  - Against: "Sorrel's smile goes thin. \"That's not the bargain. That was never the bargain.\""
- hunters:
  - With: "Calloway whistles, and Wyrmwatch crossbows come up on every roof around the square, pointed away from you."
  - Aside: "Calloway spits. \"Your funeral. We'll be at the inn.\""
  - Against: "Calloway's crossbow comes up, and so do forty others."
- Betrayal variant (the committed force, Against): prefix "You stood with us. " to the Against line.

- [ ] **Step 1: Write failing tests:**
  - `BaseTableMatchesSpec`: a fresh WorldState gives the 12 cells of the table.
  - `CrownNeverMovesOffSealOrSave`: with `banner_crown` and `Swayed("crown")`, crown is still Against on seal/save, and `MarshDefects` is true on seal/save and false on end.
  - `ModifiersStepOnceAndCap`:
    - `banner_hunters` + `Swayed("hunters")` + `wyrmwatch_belled` on save → With (Against → Aside → With → capped).
    - `wyrmwatch_belled` alone on seal → Aside (no effect off save).
    - `wyvern_egg_saved` on save → academy Aside.
  - `FaeClauseFloorsTheFae`: `fae_clause` on end → fae Aside.
  - `SaveGateListsExactlyWhatIsMissing`:
    - A fresh world → 3 strings.
    - `seal_understood` → 2.
    - `+ lore the_great_ones_below` → 1.
    - `+ promised_her` → empty.
    - `Swayed("academy")` alone also clears the first.
  - `BetrayalIsYourOwnSideAgainstYou`:
    - `banner_hunters` with save → hunters Aside after loyalty, so `Betrays` is false.
    - `banner_crown` with save → `Betrays` is true.
    - `banner_fae` with end → fae Aside (Against + loyalty), so false.
    - `banner_academy` with save → Aside, so false.
  - `EveryBreakLineRenders`: every force × stance × ending has a non-empty line, with no "{" left after formatting.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS.
- [ ] **Step 5:** Commit `"Act III: who stands where (the turn's reaction table and the save gate)"`.

### Task 3: The questioning: beats and dialogue

**Files:**
- `Core/GameRoot.TheTurn.AfterTheDoor.cs` (BeginQuestioning steps 5–6, `EchoToOthers`)
- `Dialogue/DialogueDb.TheTurn.AfterTheDoor.cs` (new: `TurnDialogue(id)`, appended to the `??` chain in `DialogueDb.Get`)
- Tests: TurnTests

**Interfaces (produces):**
```csharp
public void GameRoot.EchoToOthers(PlayerSlot speaker, string line)   // Narrate(line) to every other ready player
// dialogue "questioning": nodes open, lie_ok, lie_bad, truth, deny, confess, voices, marsh, ashby, sorrel, calloway,
//   sway_<force>_ok / sway_<force>_bad, decide, gate_short
```

**BeginQuestioning, continued:**
5. If `turn_beats_seen` is not set, `PlayBeats(by, beats)`, then set `turn_beats_seen`.
6. Open `DialogueScreen("questioning", king_q agent, null)` for the lead player.

Beats:
1. "The stair from below lets out into sunlight and noise. The whole Great Square is full: guardsmen in red and gold, Wyrmwatch hunters on the rooftops, Academy robes, a drift of Fae flowers along the fountain's rim. Every face turns to you."
2. "King Aldric stands at the top of the Sunstair, with Marsh at his shoulder, and Ashby, Sorrel and Calloway below. Nobody has drawn a weapon. Everyone is ready to. {name} plants itself at your back and looks up at the King, and keeps looking."

**Dialogue `questioning`:**
- Start:
  - `!Confessed` → "open";
  - any leader not `Heard` → "voices";
  - otherwise → "decide".
- Every node's `OnEnter` calls `c.G.EchoToOthers(c.P, "<speaker>: <text>")`.

| Node | Speaker | Text (exact copy for the story beats) | Options |
|---|---|---|---|
| open | King Aldric | "You went below. Into a place no one was meant to go. Tell me what you found." | "The truth. All of it." → truth · Deception 16 "Nothing. Old iron and dust." → lie_ok / lie_bad |
| lie_ok | King Aldric | "...Thank the stars." | narration: "He believes you. Then {name} growls, low, at the King. Not at anyone else. And every ever-lamp around the square flickers at once." → "(Tell them the truth.)" → truth |
| lie_bad | Sorrel | "Oh, that was *dreadful*. Tell them properly." | → truth |
| truth | (narrator) | narration: "You tell them. The Deep, the chains, the twelve sleeping heads, the grey door. And what's behind it: a little girl called Edda, who has been a child for a thousand years, and who asked you if you were real. The square goes very quiet." | if `ring_link`: "The iron ring. Every King. Every year, a present, and 'sorry'." → confess · Insight 14 "(Insight) Watch the King's face." → confess / deny |
| deny | King Aldric | "Lies. A monster wearing a face. That is what it does." | Intimidation 15 → confess / confess (success and failure both lead there; failure narration: "Before you can speak, {name} roars, and the King flinches, and that's answer enough for everyone watching.") · Persuasion 15 → confess / confess · "(Let {name} answer.)" → confess |
| confess | King Aldric | "...Yes. I knew. Every King knows. A thousand years ago my house sealed the fire of the world inside a child, to break the Ember Pact and keep the power for ourselves. The Ashen Queen is the fire. She is only what holds it. The lamps of this city burn on it still. Every King is told on his crowning night, and every year he goes down with a present, and says he is sorry, and does nothing. I am sorry. I have done nothing." | OnEnter sets `Confessed`. → "What happens if the seal breaks?" → voices |
| voices | (narrator) | narration: "Everyone wants to speak at once." Each leader's option hides once `Heard`. "Decide." shows only when all four are `Heard`. | "Marsh." → marsh · "Ashby." → ashby · "Sorrel." → sorrel · "Calloway." → calloway · "Decide." → decide |
| marsh | Marsh | "I swore an oath to protect this realm. I thought that meant him." She doesn't look at the King. "Seal it again. Keep people safe. Let the King answer for the rest, after." | OnEnter sets `Heard("crown")`. Persuasion 15 (12 with `banner_crown`) "Your oath was to the realm, not to the lie." → sway_crown_ok / sway_crown_bad · "Thank you, Marsh." → voices |
| ashby | Ashby | "I have spent my life on the seal. I was *proud* of it." A long breath. "Seal her. Properly, this time, so she sleeps and doesn't dream. It's the kindest thing left. I can do it. I know how." | `Heard("academy")`. Arcana 15 (advantage with `seal_understood`; DC 12 with `banner_academy`) "Show him what you saw: the fire isn't her." → sway_academy_ok / _bad · "Thank you, Ashby." → voices |
| sorrel | Sorrel | "The Pact was a promise, and *that* crown broke it." She nods at the King. "Let her out. The Courts remember what was owed." | `Heard("fae")`. If `fae_clause`: "(The favour {name} owes the Courts.)" → sway_fae_ok directly, and sets `FaeFavourCalled`. Sorrel: "Ah. Yes. We'll take it now: the Courts stand where {name} stands. Don't say we never gave you anything." · Insight 14 "What do the Courts actually want?" → sway_fae_ok / _bad · "Thank you, Sorrel." → voices |
| calloway | Calloway | "I've buried forty hunters to fire. I don't care how small it looks. End it, while it's chained." | `Heard("hunters")`. Persuasion 16 or Intimidation 16 (DC 13 with `banner_hunters`; advantage with `wyrmwatch_belled`) "She didn't burn your villages. *They* did, by keeping her." → sway_hunters_ok / _bad · "Thank you, Calloway." → voices |
| sway_<f>_ok | that leader | one line, softened (Marsh: "...The realm. Not the lie. Yes."; Ashby: "Oh. Oh, I've been so *stupid*."; Sorrel: "Freedom it is, then. How fun."; Calloway: "...Fine. I'll hear you out. I won't promise more.") | sets `Swayed(f)` → voices |
| sway_<f>_bad | that leader | one line, unmoved | → voices |
| decide | King Aldric | "Then it falls to you. Everyone in this square will follow what you choose, or fight it. What will you do?" | "End her." → Task 4 · "Seal her away." → Task 4 · Save (see below) · "Not yet. We need time." → End (the dialogue closes; `Questioning` stays set; talking to any speaker resumes here) |
| gate_short | (narrator) | narration: "You can't do it yet. Not like this. You'd need:\n" + each `SaveGateMissing` line, bulleted | → decide |

- **The Save option:** when `SaveGateMissing` is empty, it reads "Save her." and goes to Task 4's confirm. Otherwise it reads "Save her. (You're not ready: see what's missing.)" and goes to gate_short.
- **XP:** each `sway_*_ok` pays 1000 XP to the party once (Quests' XP helper, as the Keepers' parley does).

- [ ] **Step 1: Write failing tests:**
  - `QuestioningBeatsThenTheKing`: BeginQuestioning → each player has beat 1. Close beats 1 and 2 in turn → the lead has `questioning` open at "open", and the other player's log has the King's "open" line.
  - `EveryRouteConfesses`, run three times:
    - force the lie to succeed → truth → `ring_link` route;
    - force the lie to fail → Insight fails → deny → Intimidation fails;
    - the "(Let {name} answer.)" route.

    Each reaches `confess` with `Confessed` set.
  - `DecideWaitsForEveryVoice`:
    - At `voices` with 3 heard, "Decide." is hidden.
    - After the 4th, it shows and the start node resolves to "decide".
    - Pick "Not yet" → the screen closes, `Questioning` is still set, `Chosen` is not.
    - Talking to `ashby_q` opens at "decide".
  - `SwaysSetFlagsAndPayOnce`: force each sway roll to succeed → `Swayed(f)` set and +1000 XP each. Re-entering the leader's node doesn't offer the roll twice (Heard hides the leader). `fae_clause` sets `FaeFavourCalled` and `Swayed("fae")` with no roll.
  - `SaveGateSaysWhatIsMissingAndCanBeMet`:
    - With `seal_understood` only: the Save label contains "not ready", and choosing it shows gate_short listing exactly the two missing lines.
    - Pick "Not yet". `ThroughTheDoor` again, then Edda's `again` → `leave` → promise. Read lore `the_great_ones_below`. Return to the square.
    - Talking to `king_q` opens at "decide", and the label is now "Save her." with no "not ready".
  - `QuestioningSurvivesSaveLoad`:
    - Save after beat 1 and load → no MessageScreen reappears, and talking to `king_q` opens at "open".
    - Save at voices with 2 heard and one sway, then load → the flags hold and the start is "voices".
  - `TurnDialogueRenders`: every node's Text/Narration renders with no leftover "{" for each combination of `ring_link`, `fae_clause` and banners (four sides).
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** `TurnTests` + `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: the questioning on the Sunstair"`.

### Task 4: The choice and the break

**Files:**
- `Core/GameRoot.TheTurn.AfterTheDoor.cs` (`OfferEnding`, `ChooseEnding`, `TheBreak`)
- `Dialogue/DialogueDb.TheTurn.AfterTheDoor.cs` (decide's three options call `OfferEnding`)
- Tests: TurnTests

**Interfaces (produces, for Phase 6):**
```csharp
public void GameRoot.OfferEnding(PlayerSlot by, string ending)  // the confirm, for every ready player
public void GameRoot.ChooseEnding(PlayerSlot by, string ending) // atomic: flags, consequences, save, then the break
// World flags after the choice: Turn.Chosen, Turn.EndingFlag(ending), optionally Turn.Betrayed; warm_city = Break (9).
// Phase 6 reads TurnDb.Reaction(force, Turn.Ending(w), w) and TurnDb.MarshDefects to build its fights.
```

**`OfferEnding(by, ending)`:**
- Close the questioning dialogue. Push to every ready player a `ChoiceScreen(title, body, [("Yes, " + label, yes), ("Not yet", no)], cancellable: true)`.
- Titles and bodies (plain words, no judgement):
  - end: "End her" / "Her fire will come out of her as what the stories say, and you'll have to put it down. There's no undoing this."
  - seal: "Seal her away" / "Ashby's way: she sleeps, and the door is closed for good. She won't understand why. There's no undoing this."
  - save: "Save her" / "Draw the fire out of her and set her free. The Crown will come for that fire with everything it has. There's no undoing this."
- Co-op:
  - When a player picks Yes, record it. That player's screen shows a MessageScreen "Waiting for {other name}..." that closes when the choice resolves.
  - When every ready player has picked Yes → `ChooseEnding(by, ending)`.
  - Any "Not yet" (or ✕) closes every player's prompt and waiting screen, clears the record, and reopens `questioning` at "decide" for `by`.
  - Solo: Yes calls `ChooseEnding` at once.

**`ChooseEnding(by, ending)`**, in this order:
1. If `Turn.Chosen` is already set, return.
2. Set `Turn.EndingFlag(ending)` and `Turn.Chosen`.
3. If `TurnDb.Betrays(ending, w)`: set `Turn.Betrayed` and `Factions.Adjust(committed, -50, by)`.
4. `Quests.Set(warm_city, Break, by)`. Then `SaveSystem.Save(true)`.
5. Start `TheBreak(by, ending)`.

**`TheBreak`:** `PlayBeats` with three beats.
1. The ending's opening:
   - end: "The King closes his eyes. \"Then let it end. Let it finally be over.\" Far below the square, something enormous stirs, and every lamp in Aurelhold goes out at once."
   - seal: "Ashby lets out a breath he seems to have been holding for forty years. \"I'll need the Sealing Hall's work, and time, and you between me and anyone who wants her fire.\" The King's face goes very still."
   - save: "For a moment nobody moves. Then the King says, quite softly, \"No.\" And for the first time, he sounds like a king."
2. The four `TurnDb.BreakLine`s, one per line, crown first. If `MarshDefects`, its line replaces the crown With/Against line. The crown Against line still follows, with "The King looks at the guards who stayed." as the lead-in.
3. The dragon's beat:
   - end: "{name} looks at you. It doesn't come when you call. It goes and stands at the top of the stair down, between you and it, and lies down across the way, and won't move."
   - seal: "{name} makes a sound you've never heard it make. Then it comes and stands beside you anyway."
   - save: "{name} throws back its head and roars, and the sound rolls off the mountains all the way round the ring."

   Bond: end −10 ("you chose to end her"); seal 0; save +10.
- After the beats, every ready player gets a MessageScreen "To Be Continued": "The fight for Aurelhold is coming in the next update. Your choice is saved." It's a placeholder: Phase 6 removes it. Ledger it.

- [ ] **Step 1: Write failing tests:**
  - `ChoiceNeedsEveryoneToAgree`:
    - Co-op at decide. P1 picks End → P1 has the confirm. P2 has one too.
    - P1 Yes → nothing set, and P1 shows "Waiting for".
    - P2 Yes → `Chosen`, `ending_end`, warm_city 9.
    - Separately, P1 Yes then P2 "Not yet" → nothing set, both prompts closed, and P1 has `questioning` at "decide".
    - Solo Yes → chosen at once.
  - `ChoiceIsAtomicAndFinal`:
    - After choosing seal, save and load → `ending_seal` is the only ending flag.
    - No speaker resolves in any schedule query.
    - `ChooseEnding(p, Save)` again changes nothing.
  - `BetrayalCosts`:
    - `banner_crown`, choose save → `Turn.Betrayed`, crown standing −50, and break beat 2 contains "You stood with us."
    - `banner_hunters`, choose end → no betrayal.
  - `BreakShowsWhoStandsWhere`:
    - For the academy side with no sways, choosing seal → beat 2 contains Ashby's With line, the crown Against line, and Calloway's Aside line.
    - With `Swayed("crown")` → beat 2 contains Marsh's defection line.
  - `DragonReactsToTheChoice`: end → bond −10 and beat 3 has "won't move"; save → +10.
  - `EveryEndingIsChoosable`: from a fresh questioning at decide, for each of the three endings and each of the four sides (12 runs, using the minimal gate flags for save), `ChooseEnding` completes and leaves exactly one ending flag.
- [ ] **Step 2:** Run → FAIL. **Step 3:** Implement. **Step 4:** PASS.
- [ ] **Step 5:** Commit `"Act III: the choice, and the break"`.

### Task 5: Golden paths, screenshots, docs, and ship

**Files:**
- `TurnTests.cs` (`TurnGoldenPath`, `CaptureTurn`)
- `docs/PLAYTEST.md`, `docs/DESIGN.md`, `docs/LORE_PITCH.md`

- [ ] **Step 1:** `TurnGoldenPath`:
  - Start at Act III (academy). Set the Phase 4 golden-path end state: warm_city 7, `MetHer`, `meeting_seen`, `promised_her`, `seal_understood`.
  - Walk up from `roots` into `capital` and play both beats.
  - Lie and fail; confront through Insight; hear all four and sway Sorrel; decide Save; confirm.
  - Play the break, then save and load. Assert:
    - warm_city 9, `ending_save`;
    - player 1's XP ≥ `XpThresholds[13]`;
    - no speaker present;
    - Buoy still pettable in Thistledown.
  - Repeat, shortened, for End (hunters side) and Seal (crown side, Marsh swayed → `MarshDefects`).
- [ ] **Step 2:** `CaptureTurn` writes screenshots to %TEMP%/embers_screenshots/act5_*.png:
  - the full square at beat 1;
  - the Sunstair with all five speakers and the dragon;
  - the King's confess node;
  - the voices hub;
  - gate_short;
  - the confirm prompt;
  - break beat 2.

  Review every shot. Fix crowding on the Sunstair, overlapping names, text that overflows the dialogue box (the confess line is long: split it into confess + confess2 if it overflows), and anything empty or unreadable.
- [ ] **Step 3: Docs.**
  - PLAYTEST: "New: Back to the capital" with a checklist (both co-op confirms, the gate, every ending reachable). Spoilers go under a clearly marked spoiler line, as in Phase 4.
  - DESIGN: "The turn (Act III Phase 5)": the flags, the reaction table and modifiers, the gate, betrayal, the speakers pattern, `PlayBeats`, `CheckTheTurn`, and the To Be Continued placeholder for Phase 6.
  - LORE_PITCH: "Act III, the turn": the Sunstair questioning, and Marsh's badge.
- [ ] **Step 4:** RulesTests, then the full play-mode suite in the background. Fix anything red.
- [ ] **Step 5:** Build the exe and zip.
- [ ] **Step 6:** Self-review against Review Focus. Run one fix pass with RED→GREEN tests. Ledger the deferred minors.
- [ ] **Step 7:** Merge `act3/phase5-turn` into main with `--no-ff` and push. Update project-status memory (NEXT: Phase 6, the finales).
