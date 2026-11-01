using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 5: the blight follows the story, known places worsen, and one neighbour serves the Queen.</summary>
    public class BlightStoryTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            QuestDb.All.Remove("test_stageflags");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static int Count(PlayerSlot p, string text) => p.Hud.LogLines.Count(l => l.Contains(text));

        static PlayerSlot ActTwo(GameRoot g, PlayerSlot p)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            return p;
        }

        // ============================================================ Task 1: the engine

        [UnityTest]
        public IEnumerator BlightFollowsTheStoryOnLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            Blight.Sync();
            Assert.AreEqual(0, g.World.BlightStage, "no blight in Act I");
            ActTwo(g, p);
            Assert.AreEqual(1, g.World.BlightStage, "Act II begins: stage 1");
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.AreEqual(2, g.World.BlightStage, "a side chosen: stage 2");
            g.Quests.Set(Answers.Id, 7, p);
            Assert.AreEqual(3, g.World.BlightStage, "on the Emberhorn Road: stage 3");

            g.Quests.Stages[Answers.Id] = Answers.Chosen;
            g.World.BlightStage = 0; // a save from before the blight was staged
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(2, g.World.BlightStage, "loading an older save catches the blight up");
        }

        [UnityTest]
        public IEnumerator EasedBlightStaysEased()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(2), "fighter", "human"));
            yield return Frames(2);
            g.Quests.Set(Answers.Id, 7, p);
            Assert.AreEqual(3, g.World.BlightStage);
            Blight.Ease();
            Assert.AreEqual(2, g.World.BlightStage);
            g.Quests.Start("glasshouse", p); // any quest change re-syncs
            Assert.AreEqual(2, g.World.BlightStage, "eased stays eased");
        }

        [UnityTest]
        public IEnumerator MinBlightGatesAGroup()
        {
            var g = GameRoot.I;
            ActTwo(g, g.DebugStart(Still(3), "fighter", "human"));
            yield return Frames(2);
            var grp = new EnemyGroupDef { Id = "test_blight_grp", MinBlight = 2 };
            Assert.AreEqual(1, g.World.BlightStage);
            Assert.IsFalse(grp.InStage(g.Quests));
            g.World.BlightStage = 2;
            Assert.IsTrue(grp.InStage(g.Quests));
        }

        [UnityTest]
        public IEnumerator StageFlagsAreSet()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            QuestDb.All["test_stageflags"] = new QuestDef { Id = "test_stageflags", Title = "T", StageText = new[] { "", "a", "b", "c" }, StageFlags = { [2] = "test_stage_flag" } };
            g.Quests.Start("test_stageflags", p);
            Assert.IsFalse(g.World.Flag("test_stage_flag"));
            g.Quests.Set("test_stageflags", 3, p);
            Assert.IsTrue(g.World.Flag("test_stage_flag"), "jumping past a stage still sets its flag");
        }

        // ============================================================ Task 2: known places under the blight

        static readonly (string npc, string node)[] Worried =
        {
            ("brannoc", "shop"), ("elowen", "start"), ("rhoswen", "after"), ("tobin", "start"), ("maddie", "start"), ("hesper", "start"),
            ("dunstan", "start"), ("lark", "start"), ("tamsin", "start"), ("wick", "start"), ("hobb", "start"), ("wren", "start"),
        };

        [UnityTest]
        public IEnumerator TownsfolkFearTheAsh()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(10), "bard", "human");
            yield return Frames(2);
            var c = new DCtx { P = p };
            var actOne = Worried.ToDictionary(w => w.npc, w => DialogueDb.Get(w.npc).Nodes[w.node].Text(c));
            ActTwo(g, p);
            Assert.AreEqual(1, g.World.BlightStage);
            var one = Worried.ToDictionary(w => w.npc, w => DialogueDb.Get(w.npc).Nodes[w.node].Text(c));
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.AreEqual(2, g.World.BlightStage);
            foreach (var (npc, node) in Worried)
            {
                string two = DialogueDb.Get(npc).Nodes[node].Text(c);
                Assert.AreNotEqual(actOne[npc], one[npc], $"{npc} worries at stage 1");
                Assert.AreNotEqual(one[npc], two, $"{npc} worries more at stage 2");
            }
        }

        [UnityTest]
        public IEnumerator ThreeTellersMakeTheDream()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(11), "bard", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var tired = DialogueDb.Get("elowen").Nodes["start"].Options.Single(o => o.Text == "You look tired.");
            Assert.IsFalse(tired.Show(c), "no dream at stage 1");
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.IsTrue(tired.Show(c));
            foreach (var npc in new[] { "elowen", "tobin" }) DialogueDb.Get(npc).Nodes["dream"].OnEnter(c);
            Assert.IsFalse(g.World.LoreFound.Contains(DialogueDb.SharedDreamLore), "two tellers are a coincidence");
            Assert.IsFalse(tired.Show(c), "Elowen has told hers");
            DialogueDb.Get("hesper").Nodes["dream"].OnEnter(c);
            Assert.IsTrue(g.World.LoreFound.Contains(DialogueDb.SharedDreamLore), "three are a pattern");
        }

        [UnityTest]
        public IEnumerator BlightBeastsOnlyAtTheirStage()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(12), "ranger", "human"));
            yield return Frames(2);
            var groups = new[] { ("forest", "blight_wolves_forest", 2), ("ford", "blight_boars_ford", 2), ("crater", "blight_hounds_crater", 3) };
            foreach (var (zone, id, min) in groups)
            {
                var grp = WorldDb.Zones[zone].EnemyGroups.Single(e => e.Id == id);
                Assert.AreEqual(min, grp.MinBlight);
                Assert.IsFalse(grp.InStage(g.Quests), $"{id} not at stage 1");
                var z = g.Zones[zone];
                foreach (var m in grp.Members)
                {
                    Assert.IsTrue(z.IsWalkable(m.Pos), $"{id} at {m.Pos}");
                    Assert.IsTrue(z.FindPath(z.Def.DefaultSpawn, m.Pos, null, 40000).Count > 0, $"{id} reachable at {m.Pos}");
                    foreach (var other in z.Def.EnemyGroups.Where(o => o.MinBlight == 0))
                        foreach (var om in other.Members)
                            Assert.Greater(Vector2Int.Distance(m.Pos, om.Pos), 6f, $"{id} keeps clear of {other.Id}");
                }
            }
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            yield return Frames(2);
            Assert.IsTrue(WorldDb.Zones["forest"].EnemyGroups.Single(e => e.Id == "blight_wolves_forest").InStage(g.Quests));
            Assert.IsTrue(WorldDb.Zones["ford"].EnemyGroups.Single(e => e.Id == "blight_boars_ford").InStage(g.Quests));
            Assert.IsFalse(WorldDb.Zones["crater"].EnemyGroups.Single(e => e.Id == "blight_hounds_crater").InStage(g.Quests));
            Assert.IsTrue(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(e => e.Group != null && e.Group.Id == "blight_wolves_forest"), "the wolves are out in the Whisperwood");
            g.Quests.Set(Answers.Id, Answers.OnTheRoad, p);
            Assert.IsTrue(WorldDb.Zones["crater"].EnemyGroups.Single(e => e.Id == "blight_hounds_crater").InStage(g.Quests));
        }

        [UnityTest]
        public IEnumerator BuoyLeansAtStageTwo()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(13), "bard", "human"));
            yield return Frames(2);
            var buoy = Object.FindObjectsByType<TownDog>(FindObjectsSortMode.None).Single();
            buoy.PetHim(p);
            Assert.AreEqual(0, Count(p, "until the ash stops falling"), "not at stage 1");
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            buoy.PetHim(p);
            Assert.AreEqual(1, Count(p, "until the ash stops falling"));
        }

        // ============================================================ Task 3: Ash on the Doors

        static SecretInteractable SpotNamed(string id) => Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == id);

        static void StartAshQuest(GameRoot g, PlayerSlot p)
        {
            var c = new DCtx { P = p };
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            DialogueDb.Get("rhoswen").Nodes["ash_job"].OnEnter(c);
        }

        [UnityTest]
        public IEnumerator AshOnTheDoorsRuns()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(20), "rogue", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            var rho = DialogueDb.Get("rhoswen");
            var ask = rho.Nodes["after"].Options.Single(o => o.Text == "Anything I can help with, Captain?");
            Assert.IsFalse(ask.Show(c), "not at blight 1");
            StartAshQuest(g, p);
            Assert.AreEqual(1, g.Quests.Stage("ash_on_doors"));
            g.Clock.Minutes = 12 * 60;
            g.Warp(p, "town", new Vector2Int(24, 17), false);
            yield return Frames(3);
            foreach (var id in new[] { "ash_mark_1", "ash_mark_2", "ash_mark_3" }) { SpotNamed(id).Resolve(p, Rules.CheckOutcome.Success); p.Hud.CloseAll(); }
            Assert.AreEqual(2, g.Quests.Stage("ash_on_doors"));
            Assert.IsFalse(SpotNamed("ash_stakeout").Available(p), "nobody comes at noon");
            g.Clock.Minutes = 22 * 60;
            yield return Frames(2);
            Assert.IsTrue(SpotNamed("ash_stakeout").Available(p));
            SpotNamed("ash_stakeout").Resolve(p, Rules.CheckOutcome.Success);
            yield return Frames(2);
            Assert.IsTrue(p.Hud.Top is DialogueScreen, "Teodor, caught in the act");
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator TeodorEveryFate()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(21), "rogue", "human"));
            yield return Frames(2);
            var c = new DCtx { P = p };
            StartAshQuest(g, p);
            var caught = DialogueDb.Get("teodor_caught").Nodes["caught"];
            var teo = WorldDb.Npcs["teodor"];
            void Reset()
            {
                g.Quests.Stages["ash_on_doors"] = 2; g.Quests.Paid.Remove("ash_on_doors");
                foreach (var f in new[] { "teodor_redeemed", "teodor_jailed", "teodor_fled", WorldDb.TeodorAway }) g.World.Flags.Remove(f);
            }
            var cases = new (string name, System.Action run, string flag, string zone)[]
            {
                ("talked down", () => caught.Options.Single(o => o.Check == Rules.Skill.Persuasion).OnSuccess(c), "teodor_redeemed", "town"),
                ("insight", () => caught.Options.Single(o => o.Check == Rules.Skill.Insight).OnSuccess(c), "teodor_redeemed", "town"),
                ("the Captain", () => Opt(caught, "Come with me").Do(c), "teodor_jailed", "watchhouse"),
                ("sent away", () => Opt(caught, "Go. Don't").Do(c), "teodor_fled", null),
            };
            foreach (var (name, run, flag, zone) in cases)
            {
                Reset();
                int xp = p.Character.XP;
                run();
                run();
                Assert.IsTrue(g.Quests.Done("ash_on_doors"), name);
                Assert.AreEqual(xp + 900 + (name == "insight" ? LoreDb.FindXp : 0), p.Character.XP, $"{name}: paid once (Insight also finds the creed)");
                Assert.IsTrue(g.World.Flag(flag), name);
                var e = WorldDb.Current(teo, 12, 1, g.World);
                Assert.AreEqual(zone, e?.Zone, $"{name}: where Teodor is at noon");
                Assert.AreEqual(zone, WorldDb.Current(teo, 12, WorldDb.Hearthday, g.World)?.Zone, $"{name}: and on Hearthday too");
                Assert.IsFalse(SpotNamed("ash_stakeout").Available(p), $"{name}: the stakeout never returns");
            }
            Assert.IsTrue(g.World.LoreFound.Contains("ashsworn_creed"), "Insight learns the creed");
        }

        [UnityTest]
        public IEnumerator TeodorIsNotInActOne()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(22), "rogue", "human");
            yield return Frames(2);
            foreach (var day in new[] { 0, 1, WorldDb.Hearthday }) Assert.IsNull(WorldDb.Current(WorldDb.Npcs["teodor"], 12, day, g.World)?.Zone, "Act I stays light: he isn't on the map");
        }

        static DOption Opt(DNode n, string startsWith) => n.Options.Single(o => o.Text.StartsWith(startsWith));

        [UnityTest]
        public IEnumerator DragonFeelsTheBlight()
        {
            var g = GameRoot.I;
            var p = ActTwo(g, g.DebugStart(Still(5), "fighter", "human"));
            yield return Frames(2);
            g.Quests.Set(Answers.Id, Answers.Chosen, p);
            Assert.AreEqual(2, g.World.BlightStage);
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(3);
            Assert.AreEqual(1, Count(p, "goes still, nose up"), "the dragon feels the ash");
            g.Warp(p, "tavern", WorldDb.Zones["tavern"].DefaultSpawn, false); // indoors: too little ash to bark at
            yield return Frames(2);
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(3);
            Assert.AreEqual(1, Count(p, "goes still, nose up"), "once a day per place");
        }
    }
}
