# Act III Phase 7: The Endings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After the last fight, the game closes its story in an unrushed, cinematic, feel-good way that still makes you ponder whether you did right. It plays scenes for her, the dragon, each companion, the faction leaders and the King, then goes home to Thistledown (Brannoc, Elowen, Pip, Rhoswen, the kobolds, Buoy). An epilogue in slides follows, built from your choices and ending on a "did we do right?" line. Then the credits (with the SRD 5.2 attribution) and back to the title. The save is kept. This replaces Phase 6's To Be Continued card.

**Architecture:** `EndingDb.AfterTheDoor.cs` holds pure tables: `Scenes(ending, w)` returns titled beat groups, and `Slides(ending, w)` returns the epilogue slides. Both read `Turn`, `TurnDb`, `FinaleDb` and Act I/II state. `GameRoot.Ending.AfterTheDoor.cs` runs the sequence through `PlayBeats` (titled) and moves the party home before the Thistledown scene. Two new screens follow: `EpilogueScreen` (full-screen slides, lead-driven in co-op) and `CreditsScreen` (scrolling, skippable). Finally `game_complete` is set, the game saves, and it quits to the title.

**Tech Stack:** Unity 6000.5 / C#; `tools/RulesTests` (dotnet) for the balance table; play-mode tests via `.superpowers/unity-test.sh`.

**Spec:** `docs/superpowers/specs/2026-11-02-act3-design.md` ("The finale must not feel rushed", "The three endings", goals 5, 10, 12; "Non-goals"; "Cut order"). Consumes Phase 5 (`Turn`, `TurnDb`) and Phase 6 (`Finale`, `FinaleDb`, `FinaleWon`, `VictorySoon`) as merged on main (56ade66).

**Branch:** `act3/phase7-endings`. **Ledger:** `.superpowers/sdd/2026-11-03-act3-phase7-endings/progress.md` (force-add).

## Global Constraints

- Tone: warm and wry (Mercer + Mulligan), bright fantasy, PG-13. Sad and sombre is fine (the seal); horror and gore are not. **She is never to blame.** **The game never says which ending was right**: every ending's last slide is a question or an open line, never a verdict.
- Each ending is different in its scenes and slides. The Seal is sad and sombre, the Save is rewarding, and the End is a little hollow ("the epilogue doesn't let you forget who she was").
- Not rushed: at least 6 titled scenes and at least 8 slides per ending. A perfect party sees all of them. Esc/B advances a beat or slide; it never skips the sequence.
- Every window closes with Esc/B. The credits close with Esc/B or any confirm at the end.
- Co-op: every beat and slide goes to every ready player, and the lead's press advances (the `PlayBeats` rule). A player who drops mid-sequence doesn't stall it.
- Secret guard: the new story files are `*.AfterTheDoor.cs` and contain `MetHer`. Add them to `FinaleTests.FinaleFilesAreGated`'s list (or an `EndingTests` twin).
- `World.DragonName` only when `World.EggHatched`, else "the dragon". Companion lines use the companion's `Character.Name`.
- The game ends after the credits (non-goal: open-ended post-game). The save is kept. Loading a completed save shows a short "your story is told" note and lets you wander.
- **The dedication (the user, 2026-11-03: "this is very important to me"), exact copy, never cut, its own centred screen in the credits:** "To my beautiful fiancée Lauren, this game is dedicated. I love you forever." (The user's words; spelling corrected only: "fiancée". Her name is spelled **Lauren**.)
- Credits must carry NOTICE.md's SRD 5.2 CC-BY-4.0 attribution text verbatim ("This work includes material from the System Reference Document 5.2 ..."), the Ninja Adventure credit, and the Wizards non-affiliation line.

## Review Focus

1. **Loading a save made during the ending sequence** (after `finale_won`, before `game_complete`) replays the ending from the start. The epilogue and credits are never lost, and the last fight is never redone. → Task 2 `EndingResumesAfterLoad`.
2. **Co-op drop mid-epilogue:** the lead drops while a slide is up. The other player becomes the lead and the slides continue to the credits. → Task 2 `EpilogueSurvivesALeadDrop`.
3. **A party that met nobody optional:** no companions, no Rhoswen hearts, `snik_friend` unset, Buoy never met. Every scene still has text (no empty beats, no "{name}" left in), and the companions scene is skipped rather than empty. → Task 1 `ScenesNeverEmptyOrTemplated`.
4. **Every ending × every Act II side × Marsh defects or not:** slides mention the right stance for each force and never contradict the reaction table (a force that fought you isn't thanked). → Task 1 `SlidesFollowTheReactionTable`.
5. **Loading a completed save** doesn't restart the finale, the flight or the ending. It shows the note once per load, and the world is free to walk. → Task 2 `CompletedSaveLoadsQuietly`.

---

### Task 1: The endings' text (EndingDb)

**Files:**
- Create: `Assets/Scripts/Game/Quests/EndingDb.AfterTheDoor.cs`
- Test: `Assets/Tests/PlayMode/EndingTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public class EndingScene { public string Title; public string[] Beats; public string Zone; /* null = where you are; "town" = go home first */ }
  public class EpilogueSlide { public string Title, Text; }
  public static class Ending
  {
      public const string Seen = "ending_seen", Complete = "game_complete";
      public const int MinScenes = 6, MinSlides = 8;
  }
  public static class EndingDb
  {
      public static List<EndingScene> Scenes(string ending, WorldState w, IEnumerable<string> companionNames);
      public static List<EpilogueSlide> Slides(string ending, WorldState w);
      public static string PonderLine(string ending);   // the last slide's text
  }
  ```
- **Scenes** (in order; each 2–3 beats, titled):
  1. **"Her"**: End: the ashes; a small iron ring among them; the dragon won't leave. Seal: the knock, the silence; Ashby sits down against the door and doesn't get up for a long time. Save: Edda comes up the Roots stair into daylight for the first time in a thousand years, squints, and asks if that's the sun.
  2. **"{Dragon}"**: per ending, and per bond (`DragonPet.Hearts` ≥ 6 gets the warm variant). End: it comes back to you eventually, quieter. Seal: it sleeps by the Undergate for three days. Save: it lets her ride on its back round the Great Square, very slowly.
  3. **"The ones who came with you"**: one line per companion name (pick from 6 templates by index), with the name filled in. Skipped when there are no companions.
  4. **"Aurelhold"**: one line each from Marsh, Ashby, Sorrel and Calloway, chosen by `TurnDb.Reaction(force, ending, w)` (With / Aside / Against), plus Marsh's own line when `MarshDefects`. Then the King's fate: End (he keeps his throne, and the city knows what he is); Seal (cold lamps, a king nobody bows to); Save (an old man without a crown, tried in the Great Square).
  5. **"The road home"** (`Zone = "town"`): the King's carriage (or what's left of the Crown's), the Wyrmback behind you, snow.
  6. **"Thistledown"** (`Zone = "town"`): Brannoc (a new sign), Elowen (a blessing), Pip (a round on the house, and a plaque), and Rhoswen (`w.Hearts("rhoswen") >= 8` gets the romance line; otherwise a captain's salute). Snik and the kobolds if `snik_friend` (otherwise a kobold drawing nailed to the notice board). And **Buoy**, always: he comes running and won't stop wagging. (The dedication: never cut.)
- **Slides** (in order):
  - the realm (the blight: End = the fire's gone, so is the warmth; Seal = the cold stays a little; Save = the Great Ones glow and winter breaks);
  - Aurelhold;
  - one slide per force, by stance and ending;
  - the dragon;
  - her (End: the ring on a chain round the Wyrmwatch captain's neck, or yours; Seal: the door, and the knock nobody answers; Save: she learns to read, and is terrible at it, and doesn't care);
  - Thistledown;
  - the party ("{N} days since the star fell");
  - the ponder line.
- **Ponder lines** (exact copy):
  - End: "The world is safe now. That's what everyone says. You find you can't say it out loud."
  - Seal: "She's asleep, and she isn't dreaming. Ashby promised. You hope he was right. You hope she isn't knocking."
  - Save: "The fire is home, and so is she. Somewhere, a thousand years of kings are turning in their graves. Let them. Still, you wonder what the fire will want, now that it's free."

- [ ] **Step 1: Write the failing tests** in `EndingTests.cs` (plain `[Test]`s on `WorldState`s built like FinaleTests' `Chosen(...)`):
  - `EachEndingHasEnoughScenesAndSlides`: for each ending, `Scenes(e, w, new[]{"Tamsin","Odo"}).Count >= Ending.MinScenes` and `Slides(e, w).Count >= Ending.MinSlides`; the last slide's text == `PonderLine(e)`.
  - `ScenesNeverEmptyOrTemplated`: with no companions, no hearts and no flags, every beat is non-empty and contains no `{`. "The ones who came with you" is absent. With 2 names, that scene has a beat containing each name.
  - `EndingsAreDifferent`: no beat text is shared between any two endings' "Her" scenes, and the three ponder lines differ.
  - `SlidesFollowTheReactionTable`: for each ending × side in {crown, academy, fae, hunters} × `banner_crown` on/off, the force slide for each force contains the stance marker that `FinaleDb`'s table gives (assert via `EndingDb.ForceSlideStance(force, e, w) == TurnDb.Reaction(force, e, w)`; expose that tiny helper for the test).
  - `BuoyAlwaysComesHome` and `RomanceLineNeedsEightHearts`: the Thistledown scene contains "Buoy" always, and contains the romance line only with `Hearts("rhoswen") >= 8`.
  - `NoVerdicts`: no slide or beat contains "the right choice", "you were right" or "the wrong choice".
- [ ] **Step 2:** `bash .superpowers/unity-test.sh DnD.Tests.EndingTests`. Expected: FAIL (compile: `EndingDb` missing).
- [ ] **Step 3:** Implement `EndingDb` per the Interfaces block; write all the copy.
- [ ] **Step 4:** Run `DnD.Tests.EndingTests;DnD.Tests.LoreTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 7: the endings' scenes and epilogue`.

---

### Task 2: The ending sequence, the epilogue screen, the credits

**Files:**
- Create: `Assets/Scripts/Game/Core/GameRoot.Ending.AfterTheDoor.cs` (`PlayEnding`, `PlaySlides`, resume)
- Create: `Assets/Scripts/Game/UI/EpilogueScreen.cs`, `Assets/Scripts/Game/UI/CreditsScreen.cs`
- Modify: `Assets/Scripts/Game/Core/GameRoot.Finale.AfterTheDoor.cs` (`VictorySoon`: replace the To Be Continued push with `PlayEnding`; `CheckTheFinale`: resume rules)
- Modify: `Assets/Tests/PlayMode/FinaleTests.cs` (the golden paths and `TheWinIsSavedAndAnnounced` expect the ending's first scene title instead of the card)
- Test: `Assets/Tests/PlayMode/EndingTests.cs`

**Interfaces:**
- Consumes: Task 1's `EndingDb.Scenes/Slides`, `Ending.Seen/Complete`; `PlayBeats(by, beats, before, title)`; `GatherAt(zone, pos)`; `WorldDb.TownCarriageStop`.
- Produces:
  ```csharp
  public IEnumerator PlayEnding(PlayerSlot by, string ending);   // scenes → slides → credits → complete
  public IEnumerator PlaySlides(PlayerSlot by, List<EpilogueSlide> slides);
  public bool EndingPlaying { get; }
  // EpilogueScreen(EpilogueSlide slide, int index, int count): full-screen dark backdrop, title, text, "(A) Next" / "(A) The end"; Esc/B = next.
  // CreditsScreen(): scrolls GameRoot.CreditsText at ~40 px/s; confirm or Esc/B closes (any time after 2 s); closing → onDone.
  public static string CreditsText { get; }   // title, "Made by Trent, with Claude", "For Buoy (and his people)", NOTICE.md's SRD and Ninja Adventure text verbatim
  public const string Dedication = "To my beautiful fiancée Lauren, this game is dedicated. I love you forever.";
  // The credits end on the dedication alone, centred, held until a press (it is the last thing on screen before the title).
  ```
- `PlayEnding`: set `EndingPlaying`; for each scene: if `Zone == "town"` and the party isn't there yet, `GatherAt("town", TownCarriageStop)` (the dragon too), and walk Buoy (`TownDog`) to the party. Then `PlayBeats(lead, scene.Beats, title: scene.Title)`. Then `World.SetFlag(Ending.Seen)` and save. Then `PlaySlides`, then the credits. After the credits: set `Ending.Complete`, save, `QuitToTitle()`.
- Resume (`CheckTheFinale` on load): `finale_won` && !`game_complete` → `PlayEnding` from the start (Review Focus 1). `game_complete` → one MessageScreen "The story is told", body "Sundermere will remember. You can wander as long as you like, or start a new story from the title.", and nothing else starts (Review Focus 5).
- Lead handling: each beat and slide re-picks the lead as the first ready player (`PlayBeats` already does this per beat). `PlaySlides` mirrors it.

- [ ] **Step 1: Write the failing tests** in `EndingTests.cs` (reuse FinaleTests' `AtTheBoss` + `BeatTheBoss` pattern; copy the helpers or make them `internal static` in FinaleTests):
  - `EndingPlaysScenesSlidesAndCredits`: Save ending. Beat the boss and close every beat, collecting titles. Assert the titles come in `Scenes(...)` order. The players are in `town` for the Thistledown scene. Then an `EpilogueScreen` per slide (`MinSlides` at least), then a `CreditsScreen` whose text contains "System Reference Document 5.2" and "Creative Commons Attribution 4.0".
    `DedicationIsInTheCredits`: `CreditsText` contains `GameRoot.Dedication` exactly; `Dedication == "To my beautiful fiancée Lauren, this game is dedicated. I love you forever."`; the last thing the credits show before closing is the dedication. Closing it → `game_complete` set, the title screen is showing, and the save file exists with `game_complete`.
  - `EscAdvancesButNeverSkips`: pressing Esc on each slide (`Close()`) still shows the next one. The count of slides seen == `Slides(...).Count`.
  - `EndingResumesAfterLoad`: after `finale_won`, save before the first scene is closed, then load. The first scene's title comes back, no boss group is spawned, and `Flown` is still set.
  - `EpilogueSurvivesALeadDrop`: co-op. At slide 2, `DropOut(p1)`. P2 sees slide 3, and the sequence reaches the credits.
  - `CompletedSaveLoadsQuietly`: load a save with `game_complete` → exactly one "The story is told" MessageScreen, no `EpilogueScreen`, no combat, `EndingPlaying` false.
- [ ] **Step 2:** Run `DnD.Tests.EndingTests`. Expected: FAIL.
- [ ] **Step 3:** Implement. Update FinaleTests' expectations of the card (`TheWinIsSavedAndAnnounced`, the golden paths, `CaptureFinale`) to the first ending scene's title.
- [ ] **Step 4:** Run `DnD.Tests.EndingTests;DnD.Tests.FinaleTests`. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 7: the ending sequence, the epilogue and the credits`.

---

### Task 3: Act III balance table, XP check, and two Phase 6 visual minors

**Files:**
- Test: `tools/RulesTests/BossTests.cs` (`ActThreeBalanceTable`)
- Modify: `Assets/Editor/ArtMap.txt` (Ashen Queen tint), `Assets/Scripts/Game/Core/GameRoot.Finale.AfterTheDoor.cs` (light the seal fight)
- Test: `Assets/Tests/PlayMode/EndingTests.cs`

**Interfaces:** none new beyond the test names.

- [ ] **Step 1: Write the failing tests:**
  - `ActThreeBalanceTable` (RulesTests, the `ActTwoBalanceTable` method at level 13 with the ActThreeLevelCap): the streets' compositions as fixed lists in the test, End s1-s3, Seal/Save s1-s3 (no Marsh), and Save s2 with the academy and hunters Against (the biggest: 2 knights, a battlemage, a stone guardian, 2 hunters). Each needs a median 1–8 rounds. Print the table.
  - `MainQuestStillReachesThirteenAtTheTurn` (play mode): the existing Phase 5 check still passes with warm_city 10–13 XP added (it's after the turn). Assert `PlayerCharacter.LevelCap` is still 13 after `finale_won`.
  - `SealFightIsLit` (play mode): during the seal boss fight, `the_deep` has a light source within 3 tiles of `FinaleDb.AshbyAtTheDoor`. Use the zone's light API (the one `Lighting.cs` exposes for braziers). The implementer picks it; ledger which.
- [ ] **Step 2:** Run `dotnet run` in tools/RulesTests, then `DnD.Tests.EndingTests`. Expected: the balance row may pass already (it's a pin); `SealFightIsLit` FAILS.
- [ ] **Step 3:** Light Ashby's circle while the seal fight is open (a glow at the door: the brazier light from `Lighting.cs`, radius ~6). Retint `mon_ashen_queen` to ash-grey with ember red (it must read differently from the grown dragon, which is red). Re-import art. Tune any balance row outside 1–8 by editing `FinaleDb.StreetMembers` (ledger it).
- [ ] **Step 4:** Rerun both. Expected: PASS.
- [ ] **Step 5:** Commit `Act III Phase 7: balance table, a lit seal fight, an ashen Queen`.

---

### Task 4: Whole-act golden paths, screenshots, docs, verify, merge

**Files:**
- Test: `Assets/Tests/PlayMode/EndingTests.cs` (`ActThreeGoldenPathEnd/Seal/Save`, `CaptureEndings`)
- Modify: `docs/DESIGN.md` ("The endings (Act III Phase 7)"), `docs/PLAYTEST.md` (Act III complete; checklist), `docs/LORE_PITCH.md` (any new names), `README.md` (the game is complete: how to play to the end)

- [ ] **Step 1: Golden paths, one per ending.** Each starts at the meeting (`MetHer`, `meeting_seen`, warm_city Door, standing in the Roots) and walks up into the questioning. It opens the King's confession through dialogue `questioning` (`confess` → every leader → `decide`), sets the save gate's flags for Save, and picks the ending through `OfferEnding` + "Yes". Then it plays the three streets, the rider, the flight, the boss with every phase, every ending scene, every slide and the credits, and lands on the title with `game_complete` saved. Assert the warm_city stages pass 8 → 13 in order, and that no window is left open between stages.
- [ ] **Step 2: Screenshots** (`CaptureEndings`): every ending's "Her" scene, the Thistledown scene with Buoy on screen, three slides (the first, a force slide and the ponder line), and the credits at the start, the attribution, and the dedication screen (check "fiancée" renders: the é must be in the font). Review every PNG: text fits, Buoy is visible, slides are readable on the split screen.
- [ ] **Step 3: Docs.** DESIGN: the sequence, the tables, resume, the completed-save rule and the credits. PLAYTEST: "You can play to the end now", with a checklist (did each ending land? did it feel rushed? did the last line make you think?). README: the game is complete.
- [ ] **Step 4: Verify.** `dotnet run` in tools/RulesTests (all pass); the full play-mode suite (all pass; rerun any order-flaky test alone and ledger it); the exe + zip build ("Build OK").
- [ ] **Step 5: Final review** (self-review unless the user asks for a reviewer), one fix pass for Critical/Important with RED→GREEN tests, full suite again, then merge `--no-ff` to main and push. Update project-status memory: Act III complete; next is the optional release prep (a settings/credits menu entry, how-to-play readme, itch.io packaging).
