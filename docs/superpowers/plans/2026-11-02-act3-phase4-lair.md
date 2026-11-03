# Act III Phase 4: The Lair and the Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The clues of Aurelhold point down, and the party goes down. They pass through the Undergate into the deep shift's works, then the Crown's Iron Vaults and the Royal Crypts, and finally the Deep: the old prison at the roots of the Wyrmback. They get past its Iron Gaoler and open the grey door from the dream. Beyond it is the reveal. It is quiet and small. A little girl is scared of them, and the party's dragon goes to her first. Afterwards the journal's legend entries gain their "Read again" lines.

**Architecture:** This follows the capital's data-driven pattern:
- New zones go in `WorldDb.Lair.cs`, with dialogue in `DialogueDb.Lair.cs`. Both are scanned by the secret guard.
- Everything that shows who she is lives in three exempt files: `WorldDb.AfterTheDoor.cs`, `DialogueDb.AfterTheDoor.cs` and `LoreDb.AfterTheDoor.cs`. Their content is defined statically, but each piece is reachable only after `WorldDb.MetHer` is set:
  - her schedule entries require `MetHer`;
  - the room is reached only through `GameRoot.ThroughTheDoor`, which sets `MetHer` before anything is shown;
  - `LoreDb.Revealed` already checks the flag.
- Small engine additions:
  - `ZoneDef.Silent` (no music);
  - `DragonPet.GoTo` (a scripted walk);
  - a group warp shared with the carriage;
  - the Third Ideal's boon, a rules feature.

**Tech Stack:** C# (Unity 6000.5.9f1).
- Rules tests: `dotnet run --project tools/RulesTests`.
- Play-mode tests: `.superpowers/unity-test.sh [filter]`. The full suite takes ~25 min, so run it in the background.
- Art: `Assets/Editor/ArtMap.txt` plus `.superpowers/import-art.sh`. Import fails silently on a compile error, so compile first.

**Spec:** docs/superpowers/specs/2026-11-02-act3-design.md (§2 "The lair under the capital", "The truth", the anti-goals, goal 9 "the Third Ideal"). Roadmap item 4 is in docs/superpowers/plans/2026-11-02-act3-phase1-levels.md.

## Global Constraints

- **THE SECRET until the door (spec anti-goal):** nothing a player can see before `ThroughTheDoor` may hint that she is a child.
  - The lair may grow dread and suspicion of the Crown: chains, wards facing inward, guards who were never told what they guard.
  - No toys, cots, small shoes or lullabies before the door.
  - `LoreTests.NoActTwoTextHintsAtTheSecret` stays green over `WorldDb.Lair.cs` and `DialogueDb.Lair.cs`.
  - Her name, look and every line about her live only in `*.AfterTheDoor.cs`, and each of those files contains `MetHer`.
- **The reveal is unassuming (user):** no boss music, no fanfare, no title card.
  - The room is `Silent`.
  - The scene is three short narration screens, then her dialogue.
  - The word "Queen" appears in the room only when she asks who that is.
- **Sympathy (user):** she is blameless and doesn't understand. Anger points at the people who kept her. She is never villainous, never creepy, never pitiable in a grim way. PG-13 and bright fantasy. Sad is allowed; horror is not.
- **The dragon goes to her first** (spec). It was scared of fire all of Act II.
- **No ending choice here.** She can't leave (the room holds her). The party promises to come back. The turn is Phase 5.
- **Co-op:** every ready player goes through the door together and sees every beat. World flags are shared. Either player can drive.
- Dice are always visible. Peaceful options pay the same XP as fighting (the Keepers, the sentinels, the guardian, the Gaoler).
- Every window closes with Esc/B and has a ✕. Tests run muted. Every new NPC has a weekday schedule entry. Every outdoor `D` has a DoorSign. Every NPC spot is walkable.
- Working names are approved in bulk; list them in LORE_PITCH.md.
- Branch `act3/phase4-lair`. Merge to main and push after the full suite passes.

## Names (working)

- **The Undergate Works:** the deep shift's camp below the Roots. It has the **Warm Pipes** that carry heat up to the city.
  - **Bran Tolley** (Mags's husband).
  - Old **Hap Mossy**, forty years on the deep shift.
  - **Ser Orla Hadley**, captain of **the Keepers of the Undergate**: Crown soldiers on a lifetime posting, never told what they keep.
- **The Iron Vaults:** the Crown's ward-vaults: iron, ward-stones and ward-lamps.
- **The Royal Crypts:** the Kings of Aurelhold lie in a long hall. A clean path runs through the dust. The tomb of **Dame Ysolde**, the last of the Oathbound, carries the Third Ideal.
- **The Deep:** a round cavern at the roots of the mountains. Twelve great stone heads, the Great Ones, rest with their chins on the floor in a ring, facing inward. Twelve iron rings hold chains that run to **the grey door**. **The Iron Gaoler** stands before the door.
- **Beyond the door (exempt files only):** **Edda**. Nobody has said her name in a long time. Her room is warm and small. On the walls are charcoal drawings of twelve sleeping dragons, which she calls her friends who sleep round the house. Along one wall stands a row of presents, one from every century, from the man with the iron ring who comes once a year and says sorry.
- **The Third Ideal:** "No one is born a monster. I will judge none by the story told of them."

## Review Focus

1. **Co-op at the door:**
   - P2 in another zone, in a menu, or in a fight elsewhere.
   - `ThroughTheDoor` refuses while anyone ready is fighting and says why.
   - Otherwise it brings every ready player, closes their screens, and shows each of them every beat.
   - Nobody meets her alone while the other player is left behind.
   - Test: `ThroughTheDoorTakesEveryone` (Task 5).
2. **Save/load around the reveal:**
   - `MetHer` and warm_city stage 7 are set *before* the first beat, so a save at any moment afterwards is consistent.
   - Loading inside her room never replays the scene, and her dialogue starts at "again".
   - Loading a save made just before the door changes nothing.
   - Test: `MeetingSurvivesSaveLoad` (Task 5).
3. **Puzzles and leaving mid-way:**
   - Walking out of the vault or crypt mid-puzzle resets the unlatched pieces.
   - A block shoved into a corner can always be undone by leaving.
   - Latched gates stay open after a reload.
   - Tests: `VaultBeamOpensTheGate` and `CryptLidsOpenTheStairDown` (Task 3).
4. **The secret guard over the lair:**
   - The pre-door text names the door, the Gaoler and the Keepers constantly, so every one of those lines must pass the guard.
   - The exempt files must stay unreachable without `MetHer`.
   - Her NPC is absent from every schedule query until the flag is set.
   - Tests: `LairFilesAreScannedBySecretGuard` (Task 1) and `HerNothingBeforeTheDoor` (Task 5).
5. **Old saves and odd routes:**
   - A Phase 3 save already at warm_city 4 loads with the Undergate's opening secret available.
   - Start at Act III reaches the Undergate at stage 1, closed.
   - Coming back up through the crypt shortcut and down again works both ways.
   - A resolved Gaoler stays resolved after a reload.
   - Tests: `UndergateOpensOnlyWhenEverythingPointsDown` (Task 1), `CryptStairIsAShortcutBothWays` (Task 3), `GaolerFightsOrStandsAside` (Task 4).

---

### Task 1: Engine bits, the rest of "The Warm City", and opening the Undergate

**Files:**
- `World/WorldDb.cs` (`ZoneDef.Silent`)
- `Core/GameRoot.cs` (`WantedMusic` returns null for a Silent zone, even over fight music)
- `World/DragonPet.cs` (`GoTo`)
- `Core/GameRoot.ActThree.cs` (`GatherAt`, which RideCarriage now uses)
- `Quests/QuestDb.cs`
- `World/WorldDb.Lair.cs` (new, `BuildLair()`, called from the static ctor after `BuildCapitalNpcs()`)
- `Dialogue/DialogueDb.Lair.cs` (new, `LairDialogue(id)`, wired into `DialogueDb.Get` like `CapitalDialogue`)
- `World/WorldDb.Capital.cs` (the Undergate warps)
- Tests: `Assets/Tests/PlayMode/LairTests.cs` (new; SetUp/TearDown as CapitalTests, reusing `CapitalTests.ActThree/Ctx/Opt/SecretIn` made internal static) and `LoreTests.cs`

**Interfaces (produces):**
```csharp
public static class WarmCity { public const string Id = "warm_city"; public const int Summoned = 1, Arrived = 2, Audience = 3, Below = 4, Opened = 5, Deep = 6, Door = 7; }
public class ZoneDef { public bool Silent; }                       // no music at all in this zone
public IEnumerator DragonPet.GoTo(Vector3 target, float near)      // public wrapper over RunTo; sets Busy while walking
public void GameRoot.GatherAt(string zone, Vector2Int pos)          // warp every ready player side by side from pos (no checks, no text)
public const string WorldDb.UndergateOpen = "undergate_open";
```

**warm_city:**
- StageText:
  - 5: "The Undergate stands open. The deep shift's stair goes down into the dark, and warm air comes up it."
  - 6: "The Deep, at the roots of the Wyrmback. Chains as thick as a man run from twelve iron rings to a grey door. You've seen it before, in a dream."
  - 7: "You opened the door." (Phase 5 rewrites it.)
  - 8: placeholder "Back up into the light."
- StageXp: 5 → 2000, 6 → 4000, 7 → 6000.
- `StageFlags[Opened] = UndergateOpen`.
- Trigger: `ZoneEntered "the_deep"`, 6 from 5.

**Opening the Undergate** (two Secrets at roots (51,20) and (51,21), both `QuestId warm_city, QuestStage Below, AdvanceTo Opened`):
- `undergate_bar`: Athletics 16, "Heave the great bar".
  - Success: "You set your shoulder under the bar and lift. Iron groans, the King's seal cracks down the middle, and the Undergate swings inward on hinges that someone has kept oiled. Warm air breathes out of the dark."
- `undergate_lock`: Sleight of Hand 15, "Work the King's lock".
  - Success: "The lock is old, and fine, and has been opened recently: there's fresh oil in the wards. It gives with a soft click. The seal stays whole, and the gate swings in."
- Failure for both: "The Undergate doesn't move. You'll try again tomorrow."
- Crale's dialogue gains an option when deep_shift ≥ Below and warm_city = Below: "Open the Undergate for us." He sets the stage with the line: "I've the key. I've always had the key. God help me, I've never once used it."
- The roots warps (52,20)/(52,21) → `undergate` (1,14)/(1,15), with `RequiresFlag = UndergateOpen`. ClosedText is the existing Undergate sign line.

- [ ] **Step 1: Write failing tests** in LairTests:
  - `SilentZonePlaysNoMusic`:
    - A Silent test zone gives `WantedMusic() == null`, even with `AnyInCombat`.
    - A normal zone still gives its music.
  - `UndergateOpensOnlyWhenEverythingPointsDown`:
    - Start at Act III. The roots warp at stage 1 refuses with the closed text, and the secrets are unavailable.
    - Set stage Below, then save and load. Resolving `undergate_bar` gives stage 5, `UndergateOpen`, and +2000 XP.
    - The warp now lands in `undergate`, and the way back lands in `roots`.
    - Separately, Crale's option at Below opens it the same way.
  - `WarmCityStagesPayToLevelTwelve`: 85000 + the sum of warm_city StageXp ≥ 100000. That is level 12 on the main quest alone, before fights and lore.
  - `GatherAtBringsEveryone`: P1 in town and P2 in the forest. `GatherAt("roots", p)` puts both in roots on adjacent tiles.

  In LoreTests, add `LairFilesAreScannedBySecretGuard`: `WorldDb.Lair.cs` and `DialogueDb.Lair.cs` each exist once and `GuardScans` them.
- [ ] **Step 2:** `.superpowers/unity-test.sh LairTests` → FAIL (compile).
- [ ] **Step 3:** Implement. RideCarriage's warp loop becomes `GatherAt`; CarriageTakesEveryoneBothWays stays green.
- [ ] **Step 4:** `LairTests` + `CapitalTests` + `LoreTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: the Undergate opens; silent zones, gathered warps, and the rest of 'The Warm City'"`.

### Task 2: The Undergate Works, the Keepers, and Bran comes home

**Files:**
- `Maps.cs` (`Undergate`, about 44×30)
- `WorldDb.Lair.cs`, `DialogueDb.Lair.cs`
- `QuestDb.cs` (deep_shift stage 4)
- `LoreDb.cs`, `DragonDb.cs`
- `WorldDb.Capital.cs` (Bran's home schedule, Mags's new node in DialogueDb.Capital.cs)
- ArtMap
- Tests: LairTests

**Zone `undergate`:**
- Fields: "The Undergate Works", `Act = 3`, `Dark`, `BlightWeight = 0`, `Music = "music_undergate"` ("21 - Dungeon.ogg", reused).
- Layout, hand-shaped, west to east:
  - The stair foot from the Roots.
  - The deep shift's camp: bunks, a long table, a cook-fire that is never needed (the rock is warm), and washing hung on the pipes.
  - **The Warm Pipes:** fat iron pipes up the cavern wall, warm to the touch.
  - The Keepers' post: a barracks door and a parade ground.
  - The **Deep Shift's Rest**: cairns with names chalked on them.
  - The descending stair east to `iron_vaults` (Task 3).
- EnterNarration: "The stair goes down a long way, and it gets warmer the whole time. At the bottom, lamplight: a camp of bunks and washing lines, a long table set for twenty, and men in work-leathers who stop and stare at you as if you'd come down from the moon."
- Lore `warm_pipes`: Secret, Investigation 14, "Follow the warm pipes".
  - Text: "Every pipe runs up, toward the city: the ever-lamps, the Steamhouse, the palace hearths. None of them comes from a furnace. They come out of the rock, from below, warm as a hand. The deep shift scrapes them, oils them, and never asks what's on the other end."
- Lore `deep_shift_rest`: Lore spot.
  - Text: "Forty cairns, each with a name chalked on a flat stone and touched up so often the chalk is an inch thick. Hap Mossy keeps them. 'They went peaceful, mostly,' he says. 'Old age. It's a long shift.'"
- Group `undergate_keepers`:
  - `crown_champion` + 2 `crown_knight` + `crown_battlemage`, Style Talk, Persuasion DC 17, VouchFlag `banner_crown`, Unique, `ResolvedFlag = "keepers_resolved"`.
  - Spotted: "Soldiers in old-fashioned Crown plate fall in across the way, a grey-haired captain at their head. \"Nobody comes down,\" says Ser Orla Hadley. \"Nobody goes on. Those are the orders, and I've kept them for forty years.\""
  - Befriended: "You ask her what's beyond the vaults. She opens her mouth, and closes it. \"I was never told,\" she says at last. \"None of us were. You keep it, you don't ask.\" She steps aside. \"Go on, then. Come back and tell me.\""
- NPCs (each with a weekday variation):
  - `bran` (Bran Tolley): at the camp table by day, in a bunk at night. After `bran_home` he moves to the Roots, at Mags's terrace (10,6) by day.
  - `hap_mossy` (OldMan2 look): by the cairns.
  - Two `deep_shift_N` workers: flavour lines.
- deep_shift stage 4 "Home":
  - StageText: "Bran Tolley came up out of the dark and walked home to the Roots. Mags hasn't let go of him since."
  - 3000 XP.
  - `Reward.Flag = "bran_home"`.
- Dialogue `bran`:
  - "start": "Eleven years down. The Keepers say if we go up, the fire comes with us, into the Roots, into my Mags. So we stay, and they pay, and she gets the money."
  - Options that set stage 4:
    - Persuasion 15: "The fire's been going up those pipes for years. It's in every lamp in the city. It never hurt anyone."
    - Insight 13, which reads that he's homesick and lets you say: "Mags didn't marry the money."
    - Automatic once `keepers_resolved`: "There's no one left to stop you."
  - Before stage 3 of deep_shift: Bran has no quest options, only flavour.
- Mags's new node at `bran_home`: "He's home. He's thin as a rake and he can't sleep without the warm, so I've put a hot brick in the bed. You brought him up. Whatever you need, you've got it."
- The dragon's bark on first entry: "{name} sniffs the warm pipes, and goes very still, and then won't go near them."
- ArtMap:
  - `npc_bran`: Villager4.
  - `npc_hap_mossy`: OldMan2.
  - Workers: Villager5.
  - `music_undergate`.

- [ ] **Step 1: Write failing tests:**
  - `UndergateWorksReachable`: `AcademyTests.AssertAllReachable` over the camp, the pipes secret, the cairns, the stair east and each NPC spot.
  - `KeepersTalkOrFight`:
    - Persuasion success resolves them with the same XP as winning, and sets `keepers_resolved`.
    - A fresh start with `banner_crown` gets the vouch DC.
    - After a reload they are still gone.
  - `BranComesHomeEveryWay`:
    - Each of the three options, each from a fresh start at deep_shift Below.
    - Each gives stage 4, +3000 XP, `bran_home`.
    - Bran's `Current` is roots (10,6) at 10:00.
    - Mags's start node is the home node.
    - Before deep_shift Below, no option sets the stage.
- [ ] **Step 2:** `.superpowers/unity-test.sh LairTests` → FAIL.
- [ ] **Step 3:** Implement and import art.
- [ ] **Step 4:** `LairTests` + `CapitalTests` + `WorldDataIsConsistent` + `EveryNpcHasAWeeklyVariation` + `EveryBuildingDoorHasASign` → PASS.
- [ ] **Step 5:** Commit `"Act III: the Undergate Works, the Keepers of the Undergate, and Bran comes home"`.

### Task 3: The Iron Vaults, the Royal Crypts, the shortcut, and the Third Ideal

**Files:**
- `Maps.cs` (`IronVaults` about 40×28, `RoyalCrypts` about 36×26)
- `WorldDb.Lair.cs`, `LoreDb.cs`, `WorldDb.Capital.cs` (crypt warp)
- `World/WorldDb.Places.cs` (`ThirdIdeal`, `OathThird`)
- `Rules/PlayerCharacter.cs` and the damage path (the boon)
- `Core/GameRoot.cs` (grant the feature)
- `tools/RulesTests/` (new `ThirdIdealTests.cs`)
- ArtMap
- Tests: LairTests

**Zone `iron_vaults`:**
- Fields: "The Iron Vaults", Dark, `Music = "music_undergate"`.
- Layout: ward-vaults off a central gallery, each with black ward-stones. The gallery ends at a gate.
- EnterNarration: "Iron. Iron doors, iron floors, iron bands across the rock, and set in every wall a black stone carved with the same word, over and over, in a script older than the Crown: HOLD."
- Lore `ward_stones`: Secret, Arcana 15.
  - Text: "The wards are strong, and old, and every one of them faces the same way: inward and down. Nothing here was built to keep anyone out."
- The **puzzle**, a beam of ward-light:
  - Pieces: a `Light` "ward_lamp" pointing east, three `Mirror`s, and a `Crystal`.
  - `Gate` "vault_gate" (Latches) opens when the crystal is lit.
  - The first mirror carries an Arcana 13 hint: "The lamp's light wants to reach the black crystal over the far door. Turn the mirrors."
  - Positions are laid so exactly one mirror setting lights the crystal. The test drives that solution.
- Groups:
  - `vault_sentinels`: 5 `iron_sentinel`, Style Beasts, TalkSkill Arcana DC 16.
    - Befriended: "You find the order cut into their chests, HOLD, and add a word to it in the same script: DONE. They fold down where they stand."
  - `vault_guardian`: `stone_guardian`, Unique, TalkSkill Arcana DC 18.
    - Befriended: "You speak the ward-word back to it. The guardian considers this for a long, grinding moment, then steps into an alcove and is a statue again."

**Zone `royal_crypts`:**
- Fields: "The Royal Crypts", Dark, `Music = "music_crypts"` ("40 - Crypt.ogg", reused).
- Layout:
  - A long hall of royal tombs with stone effigies, each King's hands folded over an empty ring-finger.
  - The stair up to the capital's Lantern Cathedral at the north-west.
  - The stair down to `the_deep`, behind a gate.
- Lore `kings_path`: Secret, Survival 14.
  - Text: "The clean path through the dust comes down the cathedral stair, runs past every King's tomb without stopping, and goes on down. One pair of feet, once a year, for a very long time."
- The **puzzle:**
  - Two effigy lids (`Block`) and two `Plate`s under the empty plinths.
  - `Gate` "crypt_gate" (Latches) opens when both plates are pressed.
  - Hint on the first lid, History 13: "Two plinths stand empty, and their lids lie on the floor beside them. Somebody meant them to go back."
- **The shortcut:**
  - The crypts' north-west stair → capital (6,14). It is always open from below, and setting foot in the crypts sets `crypt_stair_open`.
  - Capital (6,14) → `royal_crypts`, with `RequiresFlag crypt_stair_open`.
  - ClosedText: "Iron, and the King's seal. It doesn't open from this side."
- **The Third Ideal**, at Dame Ysolde's tomb (two Secrets, like the Second):
  - `third_ideal_history`: History 15, "Read the scratched words".
    - Text: "Among the Kings, one plain tomb: DAME YSOLDE, LAST OF THE OATHBOUND. Under the name, not carved but scratched, as if with a knife-point by someone kneeling: \"" + ThirdIdeal + "\""
  - `third_ideal_religion`: Religion 10, `RequiresFlag WorldDb.OathSecond`, "Kneel and say the Second Ideal".
  - Both: `SetsFlag OathThird`, `Lore "third_ideal"`.
  - LoreDb `third_ideal` (Pact, MinAct 3): "Scratched on the tomb of the last of the Oathbound, in the crypt of the Kings she served: \"" + ThirdIdeal + "\" Nobody knows why she was buried there, or who let her be."
- **The boon** (rules): feature **"Third Ideal"**. Once per long rest, when damage would drop the character to 0 HP and doesn't kill outright, they drop to 1 HP instead. The toast reads: "The Third Ideal holds you up. (1 HP)".
  - It uses the resource key `third_ideal_used`, which a long rest clears.
  - GameRoot adds the feature to every player character when `OathThird` is set, on load, and when a player joins.
  - Companions don't get it (the Second Ideal has the same rule).

- [ ] **Step 1: Write failing tests:**
  - RulesTests `ThirdIdealHoldsAtOneOncePerLongRest`:
    - A level-12 fighter with the feature takes damage to 0 → 1 HP.
    - A second time → 0.
    - Damage ≥ HP + max HP still kills outright.
    - After a long rest it works again.
    - Without the feature → 0.
  - Play-mode tests:
    - `VaultBeamOpensTheGate`:
      - Turn the mirrors to the solution; the crystal is lit and the gate opens.
      - Leave the zone and come back: the gate is still open (latched), and after a reload too.
      - A half-turned set of mirrors resets on leaving.
    - `CryptLidsOpenTheStairDown`:
      - Push both lids onto the plates; the gate opens and latches.
      - Push one lid into a corner, leave and re-enter: it is back at its start.
    - `CryptStairIsAShortcutBothWays`:
      - Before `crypt_stair_open`, capital (6,14) refuses with the closed text.
      - Enter the crypts; the up-stair lands at capital (6,14) or beside it.
      - Capital (6,14) now leads down.
    - `ThirdIdealGrantsItsBoon`:
      - Resolve `third_ideal_history`: `OathThird` is set, and every player's character has the feature.
      - Save/load: the feature is still there.
      - A P2 who joins afterwards gets it.
    - `VaultsAndCryptsReachable`: AssertAllReachable over each secret, puzzle piece and stair (with the gates opened).
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement and import art (`music_crypts`, an effigy prop if the pack lacks one: a 16×16 `pattern` of a stone knight lying down).
- [ ] **Step 4:** RulesTests + `LairTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: the Iron Vaults and the Royal Crypts, the cathedral shortcut, and the Third Ideal"`.

### Task 4: The Deep, the Iron Gaoler, and the grey door

**Files:**
- `Maps.cs` (`TheDeep`, about 48×36)
- `WorldDb.Lair.cs`, `DialogueDb.Lair.cs`
- `Rules/Content/MonstersDb.cs` (`iron_gaoler`)
- `LoreDb.cs`, ArtMap
- `tools/RulesTests/BossTests.cs`
- Tests: LairTests

**Zone `the_deep`:**
- Fields: "The Deep", Dark, `Music = "music_deep"` ("37 - Dark Forest.ogg", unused so far), `DragonStays = false`.
- Layout:
  - The stair in from the crypts at the south.
  - A vast round floor with twelve stone heads (a new map char `9`: a Great One's head, solid, drawn 2x like the statue) in a ring, all facing inward.
  - Twelve iron rings set in the floor before them, with chains (char `c`, walkable, drawn on the floor) running north.
  - The **grey door** at the north wall (char `0`, solid, an Interactable).
- EnterNarration: "The stair opens onto a floor so wide the lamplight doesn't reach the other side. Around its edge, in a ring, lie heads of stone, each as big as a house, their chins on the floor and their eyes shut. Dragons. Twelve of them. Chains as thick as a man run from twelve iron rings across the floor to the far wall, where there is a door. It's grey. You've seen it before. You've all seen it before."
- Lore `the_great_ones_below`: Lore spot.
  - Text: "The Great Ones didn't lie down on top of anything. They lay down around it, in a ring, facing in, the way you'd sit around a fire, or around someone you meant to keep company."
  - Pre-door, this is wonder and dread, not a hint.
- The dragon's bark on entry: "{name} has been shivering since the stair. Now it stops, and lifts its head, and stares at the grey door as if it's heard its name."
- **The Iron Gaoler**, group `gaoler`:
  - Style Talk, TalkSkill Deception DC 18 ("Speak as the King": "It has heard one voice for a thousand years. You do your best impression of it."), ScareDC 99, Unique, `ResolvedFlag = "gaoler_resolved"`.
  - Spotted: "Before the grey door, something moves that you took for part of the wall: a figure of black iron three times your height, chains wound round its arms. Its face is a blank iron plate with one word cut into it. HOLD."
  - Befriended: "You give the order in the King's own cadence. The Gaoler's head turns, slowly, to the door, and back to you. Then it kneels, the way it must have knelt a thousand times, and is still."
  - Fighting it pays the same XP.
- MonstersDb `iron_gaoler`, a boss (SRD Iron Golem spirit, tuned):
  - AC 19, HP `17d10+85` (~178), CR 12, XP 8400, Speed 30, Scale 2.2, look `mon_gaoler` (GiantBlueSamurai, tinted iron-grey).
  - Scores 24/9/20/3/11/1.
  - Immune to poison and psychic. Not fire: the party's dragon still matters.
  - Attacks: Slam +11, `3d8+7` bludgeoning; Multiattack Slam ×2.
  - Ability **Binding Chains**: Line 30 ft, `6d8` force, STR save DC 17, Recharge 5–6, Telegraph.
  - LegendaryPerRound 2:
    - Chain Lash (Attack, Slam, reach 15).
    - Ward Pulse (SaveBurst 20 ft, `3d8` force, CON DC 16).
  - `PhaseTwoAt = 0.5`, `PhaseTwoRechargeMin = 4`, `LairEruptions = 0`.
  - PhaseTwoText: "The chains on the floor pull taut all at once, and the whole Deep rings like a struck bell. The Gaoler straightens. For the first time, it hurries."
- **The grey door**: an Interactable on char `0`, with two prompts.
  - "Knock":
    - Before `gaoler_resolved`, and any time after, it sets `knocked_back`.
    - Narration: "You knock: three times, slow. For a long moment, nothing. Then, from the other side, very quietly: three knocks back."
    - Pre-door safe: it never says who.
  - "Open the door":
    - Only when `gaoler_resolved`. It calls `GameRoot.ThroughTheDoor(p)` (Task 5).
    - Before that: "The Gaoler stands between you and the door."
  - DoorSign text: "The grey door from the dream. It's warm to the touch, like a hearthstone, and the chains end in its frame. There's no lock. There's no handle on this side at all. There's a handle on the other side; you can tell from the hinges."
- LoreDb `the_door` (Legend, MinAct 3, recorded on first knock): "The door from the valley's dream, real, at the bottom of everything: grey, warm, and knocked on from the other side."

- [ ] **Step 1: Write failing tests:**
  - RulesTests `GaolerIsABossNotAWall`, mirroring `WardenIsABossNotAWall`:
    - Median 4–9 rounds vs level-12 parties of four.
    - Slowest ≤ 12.
    - Damage per round vs AC 18 ≥ 30.
    - Printed in the balance output.
  - Play-mode tests:
    - `DeepIsReachable`: AssertAllReachable over the ring, the lore spot and the tile before the door.
    - Entering `the_deep` at stage 5 → stage 6, +4000 XP.
    - `DoorWaitsForTheGaoler`:
      - "Open" before `gaoler_resolved` narrates the refusal and doesn't warp.
      - "Knock" sets `knocked_back` and records `the_door` lore.
    - `GaolerFightsOrStandsAside`:
      - A Deception success resolves it, with XP equal to its fight XP.
      - Save/load: it is still resolved, and no Gaoler spawns.
      - A fresh start: `StartGroupCombat("gaoler")` builds a combat with the boss's legendary budget of 2.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement (map chars `9`, `c`, `0` in `Zone.cs` with legend lines in `Maps.cs`; `obj_great_one_head`, `obj_chain`, `obj_grey_door` patterns in ArtMap; `mon_gaoler`; `music_deep`). Import art.
- [ ] **Step 4:** RulesTests + `LairTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: the Deep, the Iron Gaoler, and the grey door"`.

### Task 5: Beyond the door: Edda, the meeting, and "Read again"

**Files (every line about her is in these exempt files, and each contains `MetHer`):**
- `World/WorldDb.AfterTheDoor.cs` (new: `BuildBeyondTheDoor()` from the static ctor; zone `beyond_the_door`, NPC `edda`)
- `Dialogue/DialogueDb.AfterTheDoor.cs` (new: `AfterTheDoorDialogue(id)`, wired into `DialogueDb.Get`)
- `Quests/LoreDb.AfterTheDoor.cs` (new: `RegisterReveals()`, called at the end of LoreDb's static setup; it only calls `SetReveal`)
- `Core/GameRoot.AfterTheDoor.cs` (new: `ThroughTheDoor`, the meeting scene)
- ArtMap (`npc_edda`)
- Tests: LairTests, LoreTests

**Interfaces (produces, for Phase 5):**
```csharp
public void GameRoot.ThroughTheDoor(PlayerSlot by)
// World flags: WorldDb.MetHer; "her_truth" (Insight), "seal_understood" (Arcana), "ring_link" (the King's ring),
// "knocked_back" (Task 4), "promised_her" (you said you'd come back), "meeting_seen" (the scene has played).
```

**`ThroughTheDoor(by)`**, in this order:
1. If any ready player is InCombat, narrate "Not with a fight still going." and stop.
2. Set `MetHer`. Set warm_city to `Door` (if it is lower). Then `SaveSystem.Save(true)`.
3. Close every ready player's screens. `GatherAt("beyond_the_door", (3,7))`.
4. Place the dragon beside the lead player.
5. If `meeting_seen` is not set, start `MeetingScene(by)`.
   Otherwise narrate: "Edda is sitting on the bed, swinging her feet. She waves as if you'd only been gone a minute."

**`MeetingScene`:** three MessageScreens pushed to every ready player.
- The scene advances when the triggering player closes each screen. The other player's copy closes too.
- The dragon's walk plays between beats 2 and 3, and `meeting_seen` is set after the last beat.
- Then dialogue `edda` opens for `by`.

Beats:
1. "The door opens onto a room. Just a room: low and warm, lamplit, no bigger than a cottage kitchen. Charcoal drawings cover the walls: twelve dragons, asleep in a ring, drawn over and over by the same hand, getting better as they go. Along one wall, a row of presents: a wooden horse, a tin soldier, a music box, a doll in a court dress five hundred years out of fashion. One from every century."
2. "Behind the bed, someone is hiding. A little girl, seven or eight, in a nightdress gone grey with age, ash-pale hair, and eyes that catch the lamplight like coals. She's staring at you. She is terrified of you."
3. Before this beat, `Dragon.GoTo(her tile)` runs.
   "{name} has been afraid of fire since the day it hatched. It walks past all of you, right up to her, folds itself down small, which takes some doing, and puts its great head in her lap. She stares at it. Then, very carefully, she pats it. And she laughs."

**Zone `beyond_the_door`:**
- Fields: "Beyond the Door", 14×10, `Indoors`, `Silent`, not Dark.
- Warm light from two lamps. The bed, the drawings (Signs), the presents (Signs), the door back at the south → `the_deep` before the door.
- Signs:
  - The drawings: "Twelve dragons asleep in a ring. Under one, in a child's careful letters: MY FRIENDS. THEY SLEEP ROUND THE HOUSE SO I AM NOT ON MY OWN."
  - The presents: "A wooden horse. A tin soldier. A music box that plays three notes. Under each, in a different grown-up hand: 'Forgive me.'"
- EnterNarration: none. The scene is the narration.

**NPC `edda`:**
- Look `npc_edda`: Child sheet, a pale ash tint. If ArtMap `tint` doesn't apply to actors, use the untinted Child sheet and ledger it.
- A small amber `ArcaneHalo` (radius 0.8): the fire showing faintly.
- Schedule:
  - All day by the bed (5,4), `RequiresFlag MetHer`.
  - Hearthday: "drawing on the wall" at (9,3), `RequiresFlag MetHer`.
- `DragonDb` opinion: "{name} won't leave her side." The dragon won't follow the party out until they leave.
- `GoneFlag` is not used. She never leaves the room in Phase 4.

**Dialogue `edda`:**
- Start: "start" the first time; "again" once `promised_her` is set.

| Node | Text (her voice; short, a little formal, curious) | Options |
|---|---|---|
| start | "Are you real? You're not him. He only comes once a year, and it isn't the day." | "We're real. Who's 'him'?" → him · "What's your name?" → name · "How long have you been here?" → here |
| name | "Edda. I think. Nobody's said it in a long time. Will you say it? ...Thank you. That's nice." | → here / him / fire / queen |
| here | "Since the big ones lay down. They sleep round the house, so I'm not on my own. I knock sometimes. So somebody knows I'm here." | if `knocked_back`: "That was us. We knocked back." → knock · → him / fire |
| knock | "That was *you*? I thought I dreamed it. Nobody ever knocks back." (bond +10 for the dragon; sets nothing else) | → him / fire |
| him | "The man with the iron ring. He brings a present and says sorry. He never says what for. He's a different man, sometimes. Older. Then younger." | if lore `the_kings_ring` found: History 13 "It's the King. Every King." → ring (sets `ring_link`) · → fire |
| ring | "A king? Like in stories? ...Then why is he sorry?" | → fire / queen |
| fire | "It gets hot when I'm scared. Or sad. I try not to be. The lamps up there flicker when I cry, he said once. Is that true?" | Arcana 15: look at the fire in her → seal (sets `seal_understood`) · Insight 12: is she telling the truth → truth (sets `her_truth`) |
| seal | Narration: "You see it, the way you'd see a candle through a hand: a fire the size of the world, folded up inside a little girl and held there by the iron, the chains, the wards, the mountains themselves. Every lock in the Deep is for this. It's all for her." | → queen / leave |
| truth | Narration: "Every word. She has no idea what she is, or why she's here. She thinks this is just where she lives." | → queen / leave |
| queen | Only reached by asking "Do you know the Ashen Queen?": "Who's that? Is she nice? ...Is she the one everybody's scared of? He said people were scared. I'm sorry they're scared." | → leave |
| leave | "Are you going? Can I come?" She walks with you to the door, and stops at the threshold like it's the edge of a cliff. "I can't. It holds me. It's alright. It's always held me. Will you come back?" | "We'll come back. I promise." (sets `promised_her`) · "We'll try." |
| again | "You came back! {name} came back!" | → him / fire / queen / leave (only the nodes not yet seen give flags) |

**Leaving the room:**
- The first time anyone leaves through the room's door, every ready player gets a narration: "Your journal: some of what you read on the road reads differently now. (★ Read again)". This sets `reveal_toast`.

**The "Read again" lines** (`LoreDb.AfterTheDoor.cs`, exact copy):
- shared_dream: "The knocking was her. Slow and steady, so somebody would know she was there."
- warden_last_words: "Hold the door. Not against something terrible. Shut, on a little girl."
- thirteenth_bell: "Twelve bells for the Great Ones who lie round her. The thirteenth was always for her."
- iron_ring: "Someone wanted something to stay here. Someone wanted *her* to stay."
- altered_annals: "Two sets of annals, cut and corrected for the same years. The year they put her there."
- royal_annals: "Year 2 of the Crown, razored out. The year the first King came home with an iron ring."
- sealing_work: "'Gently, and forever.' Ashby's circles were made for her."
- ashen_queen: "A dragon of grey fire, in every temple. Nobody ever painted the girl it was folded up inside."
- skipping_rhyme: "Bar the door, and the Ashen Queen won't come no more. She never could."
- primer_ashen_age: "'Why must they never wake?' Because then somebody would go and look."
- victory_statue: "The crowned figure in the carved flames, worn smooth by a thousand years of hands. Look again: she's the size of a child."
- ever_lamps: "Her warmth, piped up from the Deep to light the city that keeps her there. The lamps flicker when she cries."
- the_kings_ring: "The same iron as the rings in the Deep. Every King wears the key."
- sealed_crypt: "One pair of feet, once a year, with a present and an apology."
- works_ledger: "Forty men on the deep shift, oiling the pipes that carry her heat, never told whose it was."
- great_ones_statue: "'Who lay down so the world could wake.' They lay down round her, so she wouldn't be alone."

- [ ] **Step 1: Write failing tests:**
  - `HerNothingBeforeTheDoor`:
    - With every Act III flag except `MetHer`: `WorldDb.Current(Npcs["edda"], h, d, w) == null` for every hour and weekday.
    - `LoreDb.Revealed` is false for every entry.
    - The journal text contains no "Read again".
  - `ThroughTheDoorTakesEveryone`:
    - P2 in combat in the forest → refused, nobody moves, and the line is narrated.
    - Combat ends, and P2 is in a menu in town.
    - `ThroughTheDoor(p1)` → both are in `beyond_the_door` with their screens closed (P2's menu too).
    - `MetHer`, warm_city 7, `Music` silent.
    - Each player has beat 1 on screen.
  - `MeetingSceneNeverSkipsABeat`:
    - Closing the screens in turn shows beats 1, 2 and 3 in order.
    - The dragon ends within 1.5 tiles of Edda.
    - `meeting_seen` is set, and dialogue `edda` is open at "start".
  - `HerDialogueEveryPath`:
    - Every node's Text and Narration render without error and without "{name}" left over.
    - The `knock` option appears only with `knocked_back`; the `ring` option only with the `the_kings_ring` lore.
    - Arcana/Insight/History successes set `seal_understood`, `her_truth` and `ring_link`; the promise sets `promised_her`; "again" becomes the start.
  - `ReadAgainAfterTheMeeting`:
    - At least 14 entries have RevealText, and each entry id exists.
    - After `MetHer`, the journal shows "★ Read again" for found entries only.
    - Leaving the room narrates the toast once (not on a second exit).
  - `ReturningIsQuiet`: a second ThroughTheDoor plays no MessageScreen and narrates the wave line.
  - `MeetingSurvivesSaveLoad`:
    - Save inside the room mid-scene (after beat 1) and load.
    - `MetHer`, stage 7 and her schedule hold.
    - No MessageScreen reappears by itself, and her dialogue opens.
    - A save from just before the door, loaded, has none of these flags.

  In LoreTests, add `RevealFilesExistAndAreGated`: the four `*.AfterTheDoor.cs` files exist, and each contains `MetHer`. The existing `AfterTheDoorFilesAreGatedOnMeetingHer` covers the general rule.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Implement and import art.
- [ ] **Step 4:** `LairTests` + `LoreTests` + `ActThreeIntoTests` → PASS.
- [ ] **Step 5:** Commit `"Act III: beyond the door"`. The message says nothing more than that: commit messages are visible too.

### Task 6: Golden path, screenshots, docs, and ship

**Files:**
- `LairTests.cs` (golden path, `CaptureLair`)
- `docs/PLAYTEST.md`, `docs/DESIGN.md`, `docs/LORE_PITCH.md`

- [ ] **Step 1:** `LairGoldenPath`. It runs from warm_city Below with deep_shift Below:
  - Crale opens the Undergate.
  - Persuade the Keepers; Bran comes home.
  - Solve the vault beam; pass the sentinels (Arcana) and the guardian.
  - Solve the crypt lids; read the Third Ideal.
  - Enter the Deep; knock; talk past the Gaoler.
  - ThroughTheDoor, play every beat, and talk to her through every flag-setting node; promise.
  - Leave through the crypt shortcut to the capital.
  - Save and load. Assert:
    - warm_city 7, deep_shift 4;
    - `MetHer`, `OathThird`, `promised_her`;
    - ≥ 14 revealed entries;
    - player 1's XP ≥ `XpThresholds[12]`;
    - Buoy still pettable in Thistledown.
- [ ] **Step 2:** `CaptureLair` writes screenshots to %TEMP%/embers_screenshots/act4_*.png:
  - the Undergate camp, the vault beam, the crypt hall, the Deep with the ring of heads, the Gaoler, beat 2 and the dragon's head in her lap.
  Review every shot. Fix anything grid-like, empty, unreadable or loud. Her room must read as small, warm and quiet.
- [ ] **Step 3: Docs.**
  - PLAYTEST: "New: Below Aurelhold" section and checklist. Keep the reveal itself out of the section heading and summary: describe it as "what's behind the door", with the spoiler under a clearly marked spoiler line, because the user's fiancée may read it.
  - DESIGN: "The lair (Act III Phase 4)", covering:
    - zones, chars `9 c 0`, `ZoneDef.Silent`, `GatherAt`, `DragonPet.GoTo`, `ThroughTheDoor`;
    - the AfterTheDoor files rule, the Third Ideal boon, flags for Phase 5, the Gaoler's stats.
  - LORE_PITCH: "Act III, the lair": the names above.
- [ ] **Step 4:** RulesTests, then the full play-mode suite in the background. Fix anything red.
- [ ] **Step 5:** Build the exe and zip.
- [ ] **Step 6:** Self-review against this plan's Review Focus. Run one fix pass with RED→GREEN tests. Ledger the deferred minors.
- [ ] **Step 7:** Merge `act3/phase4-lair` into main with `--no-ff` and push. Update project-status memory.
