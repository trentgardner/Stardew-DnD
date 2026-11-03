using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act III Phase 5: the turn. Back up into the light, the questioning on the Sunstair, the choice and the break.</summary>
    public class TurnTests
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
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static PlayerSlot ActThree(GameRoot g, int seed, string cls = "fighter", string side = "academy") => CapitalTests.ActThree(g, seed, cls, side);
        static DCtx Ctx(PlayerSlot p) => CapitalTests.Ctx(p);
        static int Count(PlayerSlot p, string text) => p.Hud.LogLines.Count(l => l.Contains(text));
        static MessageScreen Beat(PlayerSlot p) => p.Hud.Find<MessageScreen>();
        static DialogueScreen Talk(PlayerSlot p) => p.Hud.Find<DialogueScreen>();
        static readonly string[] Speakers = { "king_q", "marsh_q", "ashby_q", "sorrel_q", "calloway_q" };
        static readonly string[] Originals = { "king_aldric", "marsh_capital", "ashby_capital", "sorrel_capital", "calloway_capital" };
        static Dialogue Q => DialogueDb.Get("questioning");

        /// <summary>The state at the end of the meeting: warm_city 7, met her, the scene seen. Standing in the roots.</summary>
        static IEnumerator Met(GameRoot g, PlayerSlot p, params string[] more)
        {
            g.Quests.Set(WarmCity.Id, WarmCity.Door, p, 0f);
            foreach (var f in new[] { WorldDb.MetHer, "meeting_seen" }.Concat(more)) g.World.SetFlag(f);
            g.Warp(p, "roots", new Vector2Int(50, 20), false);
            yield return Frames(3);
        }

        /// <summary>Walks up into the capital and closes both beats: the questioning dialogue is open.</summary>
        static IEnumerator Surface(GameRoot g, PlayerSlot p)
        {
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(4);
            for (int i = 0; i < 6 && Talk(p) == null; i++)
            {
                var b = Beat(p);
                if (b != null) b.Close();
                yield return Frames(4);
            }
        }

        static WorldState W(params string[] flags)
        {
            var w = new WorldState();
            foreach (var f in flags) w.Flags.Add(f);
            return w;
        }

        static DOption OptTo(string node, string next) => Q.Nodes[node].Options.First(o => o.Next == next || o.Success == next);
        static DOption OptText(string node, string text) => Q.Nodes[node].Options.First(o => o.Text.StartsWith(text));

        // ============================================================ Task 1: surfacing and the speakers

        [UnityTest]
        public IEnumerator TheTurnStartsOnSurfacingAndAfterLoad()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 1);
            yield return Frames(2);
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(4);
            Assert.IsFalse(g.World.Flag(Turn.Questioning), "Start at Act III: nothing until you've met her");

            yield return Met(g, p);
            Assert.IsFalse(g.World.Flag(Turn.Questioning), "not below");
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(4);
            Assert.IsTrue(g.World.Flag(Turn.Questioning), "the city is waiting when you come up");
            Assert.AreEqual(WarmCity.Called, g.Quests.Stage(WarmCity.Id));
            Assert.LessOrEqual(Vector2Int.Distance(p.MapPos, WorldDb.SunstairSpot), 2f, "gathered at the foot of the Sunstair");
            g.QuitToTitle();
            yield return Frames(2);

            // An old save made standing in the capital at stage 7: the questioning begins after loading.
            p = ActThree(g, 2);
            yield return Frames(2);
            g.Warp(p, "capital", new Vector2Int(40, 30), false);
            yield return Frames(3);
            g.Quests.Set(WarmCity.Id, WarmCity.Door, p, 0f);
            g.World.SetFlag(WorldDb.MetHer); g.World.SetFlag("meeting_seen");
            Assert.IsFalse(g.World.Flag(Turn.Questioning));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(5);
            Assert.IsTrue(g.World.Flag(Turn.Questioning), "after a load, too");
        }

        [UnityTest]
        public IEnumerator QuestioningWaitsForAFight()
        {
            var g = GameRoot.I;
            var p1 = ActThree(g, 3);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(4), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return Met(g, p1);
            g.Warp(p2, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            g.StartGroupCombat("forest_wolves", p2);
            yield return Frames(2);
            Assert.IsTrue(p2.InCombat);
            g.Warp(p1, "capital", new Vector2Int(58, 30), false);
            yield return Frames(4);
            Assert.IsFalse(g.World.Flag(Turn.Questioning));
            Assert.GreaterOrEqual(Count(p1, "Not with a fight still going"), 1);
            Assert.AreEqual("forest", p2.Zone.Def.Id);

            foreach (var c in g.Combats.ToList()) c.Finished = true;
            g.Combats.Clear();
            yield return Frames(3);
            g.Warp(p1, "roots", new Vector2Int(50, 20), false);
            yield return Frames(3);
            g.Warp(p1, "capital", new Vector2Int(58, 30), false);
            yield return Frames(5);
            Assert.IsTrue(g.World.Flag(Turn.Questioning));
            Assert.AreEqual("capital", p1.Zone.Def.Id);
            Assert.AreEqual("capital", p2.Zone.Def.Id, "nobody is left behind");
            Assert.IsNotNull(Beat(p1)); Assert.IsNotNull(Beat(p2), "both see the beats");
        }

        [UnityTest]
        public IEnumerator SpeakersOnlyAtTheQuestioning()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 5);
            yield return Frames(2);
            var w = g.World;
            void Check(bool speakers, bool originalsGone, string when)
            {
                for (int h = 0; h < 24; h++)
                    for (int d = 0; d < 7; d++)
                    {
                        foreach (var id in Speakers)
                        {
                            var e = WorldDb.Current(WorldDb.Npcs[id], h, d, w);
                            if (speakers)
                            {
                                Assert.IsNotNull(e, $"{id} at {h}:00 day {d} {when}");
                                Assert.AreEqual("capital", e.Zone);
                            }
                            else Assert.IsNull(e, $"{id} at {h}:00 day {d} {when}");
                        }
                        if (originalsGone) foreach (var id in Originals) Assert.IsNull(WorldDb.Current(WorldDb.Npcs[id], h, d, w), $"{id} {when}");
                    }
            }
            Check(false, false, "before");
            w.SetFlag(WorldDb.MetHer);
            w.SetFlag(Turn.Questioning);
            Check(true, true, "during");
            w.SetFlag(Turn.Chosen);
            Check(false, true, "after");
            // Their spots really are walkable in the live zone.
            var zone = g.Zones["capital"];
            foreach (var id in Speakers) Assert.IsTrue(zone.IsWalkable(WorldDb.Npcs[id].Schedule[0].Pos), id + " stands somewhere walkable");
            Assert.IsTrue(zone.IsWalkable(WorldDb.SunstairSpot) && zone.IsWalkable(WorldDb.SunstairSpot + Vector2Int.right), "the party's spot too");
        }

        [Test]
        public void MainQuestReachesThirteenAtTheTurn()
        {
            int sum = QuestDb.Get(WarmCity.Id).StageXp.Values.Sum();
            Assert.GreaterOrEqual(85000 + sum, PlayerCharacter.XpThresholds[13], "level 13 at the turn on the main quest alone");
        }

        // ============================================================ Task 2: the table, the gate, betrayal

        [Test]
        public void BaseTableMatchesSpec()
        {
            var w = W();
            var expect = new Dictionary<string, Stance[]>
            {
                ["crown"] = new[] { Stance.With, Stance.Against, Stance.Against },
                ["academy"] = new[] { Stance.Aside, Stance.With, Stance.Against },
                ["fae"] = new[] { Stance.Against, Stance.Aside, Stance.With },
                ["hunters"] = new[] { Stance.With, Stance.Aside, Stance.Against },
            };
            foreach (var kv in expect)
                for (int i = 0; i < 3; i++)
                    Assert.AreEqual(kv.Value[i], TurnDb.Reaction(kv.Key, Turn.Endings[i], w), kv.Key + " on " + Turn.Endings[i]);
        }

        [Test]
        public void CrownNeverMovesOffSealOrSave()
        {
            var w = W("banner_crown", Turn.Swayed("crown"));
            Assert.AreEqual(Stance.Against, TurnDb.Reaction("crown", Turn.Seal, w));
            Assert.AreEqual(Stance.Against, TurnDb.Reaction("crown", Turn.Save, w));
            Assert.IsTrue(TurnDb.MarshDefects(Turn.Seal, w));
            Assert.IsTrue(TurnDb.MarshDefects(Turn.Save, w));
            Assert.IsFalse(TurnDb.MarshDefects(Turn.End, w));
            Assert.IsFalse(TurnDb.MarshDefects(Turn.Save, W()), "not without banner or sway");
        }

        [Test]
        public void ModifiersStepOnceAndCap()
        {
            Assert.AreEqual(Stance.With, TurnDb.Reaction("hunters", Turn.Save, W("banner_hunters", Turn.Swayed("hunters"), "wyrmwatch_belled")));
            Assert.AreEqual(Stance.Aside, TurnDb.Reaction("hunters", Turn.Save, W("wyrmwatch_belled")));
            Assert.AreEqual(Stance.Aside, TurnDb.Reaction("hunters", Turn.Seal, W("wyrmwatch_belled")), "the bell only matters on save");
            Assert.AreEqual(Stance.Aside, TurnDb.Reaction("academy", Turn.Save, W("wyvern_egg_saved")));
            Assert.AreEqual(Stance.Aside, TurnDb.Reaction("academy", Turn.Save, W("marsh_asks_ashby", Turn.Swayed("crown"))), "Marsh's trust carries Ashby when she defects");
            Assert.AreEqual(Stance.Against, TurnDb.Reaction("academy", Turn.Save, W("marsh_asks_ashby")), "but not when she doesn't");
        }

        [Test]
        public void FaeClauseFloorsTheFae()
        {
            Assert.AreEqual(Stance.Aside, TurnDb.Reaction("fae", Turn.End, W("fae_clause")));
            Assert.AreEqual(Stance.With, TurnDb.Reaction("fae", Turn.Save, W("fae_clause")));
        }

        [Test]
        public void SaveGateListsExactlyWhatIsMissing()
        {
            var w = W();
            Assert.AreEqual(3, TurnDb.SaveGateMissing(w).Count);
            w.Flags.Add("seal_understood");
            CollectionAssert.AreEqual(new[] { TurnDb.GatePact, TurnDb.GateTrust }, TurnDb.SaveGateMissing(w));
            w.LoreFound.Add("the_great_ones_below");
            CollectionAssert.AreEqual(new[] { TurnDb.GateTrust }, TurnDb.SaveGateMissing(w));
            w.Flags.Add("promised_her");
            Assert.IsEmpty(TurnDb.SaveGateMissing(w));
            Assert.IsFalse(TurnDb.SaveGateMissing(W(Turn.Swayed("academy"))).Contains(TurnDb.GateHeld), "winning Ashby over also clears it");
            Assert.IsFalse(TurnDb.SaveGateMissing(W(Turn.FaeFavourCalled)).Contains(TurnDb.GatePact), "so does the favour");
        }

        [Test]
        public void BetrayalIsYourOwnSideAgainstYou()
        {
            WorldState Side(string f) { var w = W("banner_" + f); w.CommittedFaction = f; return w; }
            Assert.IsFalse(TurnDb.Betrays(Turn.Save, Side("hunters")), "loyalty lifts them to Aside");
            Assert.IsTrue(TurnDb.Betrays(Turn.Save, Side("crown")), "the throne never forgives");
            Assert.IsFalse(TurnDb.Betrays(Turn.End, Side("fae")));
            Assert.IsFalse(TurnDb.Betrays(Turn.Save, Side("academy")));
            Assert.IsFalse(TurnDb.Betrays(Turn.Save, W()), "no side, no betrayal");
        }

        [Test]
        public void EveryBreakLineRenders()
        {
            foreach (var side in new string[] { null, "crown", "academy", "fae", "hunters" })
            {
                var w = side == null ? W() : W("banner_" + side);
                w.CommittedFaction = side;
                foreach (var f in Turn.Forces)
                    foreach (Stance s in System.Enum.GetValues(typeof(Stance)))
                        foreach (var e in Turn.Endings)
                        {
                            var line = TurnDb.BreakLine(f, s, e, w);
                            Assert.IsNotEmpty(line);
                            Assert.IsFalse(line.Contains("{"), line);
                        }
                foreach (var e in Turn.Endings)
                    foreach (var beat in GameRoot.BreakBeats(e, w)) { Assert.IsNotEmpty(beat); Assert.IsFalse(beat.Contains("{"), beat); }
            }
        }

        // ============================================================ Task 3: the questioning

        [UnityTest]
        public IEnumerator QuestioningBeatsThenTheKing()
        {
            var g = GameRoot.I;
            var p1 = ActThree(g, 10);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(11), "wizard", "elf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return Met(g, p1);
            g.Warp(p1, "capital", new Vector2Int(58, 30), false);
            yield return Frames(5);
            var seen = new List<string>();
            for (int i = 0; i < 6 && Talk(p1) == null; i++)
            {
                var b = Beat(p1);
                Assert.IsNotNull(Beat(p2) ?? b, "P2 sees it too");
                if (b != null) { seen.Add(b.Body); b.Close(); }
                yield return Frames(4);
            }
            Assert.AreEqual(2, seen.Count);
            StringAssert.Contains("Great Square is full", seen[0]);
            StringAssert.Contains("King Aldric stands", seen[1]);
            Assert.IsNotNull(Talk(p1));
            Assert.AreEqual("questioning", Talk(p1).DialogueId);
            Assert.AreEqual("open", Talk(p1).NodeId);
            Assert.GreaterOrEqual(Count(p2, "Tell me what you found"), 1, "P2 hears the King");
            Assert.IsTrue(g.World.Flag(Turn.BeatsSeen));
        }

        [UnityTest]
        public IEnumerator EveryRouteConfesses()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 12);
            yield return Frames(2);
            yield return Met(g, p);
            yield return Surface(g, p);
            var t = Talk(p);
            // 1. The lie works, and the dragon gives it away; the ring.
            Assert.AreEqual("lie_ok", Q.Nodes["open"].Options.First(o => o.Check == Skill.Deception).Success);
            t.Goto("lie_ok");
            Assert.AreEqual("truth", Q.Nodes["lie_ok"].Options[0].Next);
            g.World.SetFlag("ring_link");
            Assert.IsTrue(Q.Nodes["truth"].Options.Any(o => o.Next == "confess" && !o.Check.HasValue && o.Show(Ctx(p))), "the ring goes straight to it");
            // 2. Insight fails → deny; both rolls fail → roar → confess.
            var insight = Q.Nodes["truth"].Options.First(o => o.Check == Skill.Insight);
            Assert.AreEqual("deny", insight.Failure);
            foreach (var o in Q.Nodes["deny"].Options.Where(o => o.Check.HasValue)) { Assert.AreEqual("confess", o.Success); Assert.AreEqual("roar", o.Failure); }
            Assert.IsTrue(Q.Nodes["deny"].Options.Any(o => !o.Check.HasValue && o.Next == "roar"), "or let the dragon answer");
            Assert.AreEqual("confess", Q.Nodes["roar"].Options.Single().Next);
            Assert.AreEqual("truth", Q.Nodes["lie_bad"].Options.Single().Next);
            Assert.IsFalse(g.World.Flag(Turn.Confessed));
            t.Goto("confess");
            Assert.IsTrue(g.World.Flag(Turn.Confessed), "the truth about the Crown comes out on every route");
            Assert.AreEqual("voices", Q.Start(Ctx(p)));
        }

        [UnityTest]
        public IEnumerator DecideWaitsForEveryVoice()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 13);
            yield return Frames(2);
            yield return Met(g, p);
            yield return Surface(g, p);
            var t = Talk(p);
            t.Goto("confess");
            foreach (var n in new[] { "marsh", "ashby", "sorrel" }) t.Goto(n);
            var decide = Q.Nodes["voices"].Options.First(o => o.Next == "decide");
            Assert.IsFalse(decide.Show(Ctx(p)), "not until everyone has spoken");
            Assert.AreEqual("voices", Q.Start(Ctx(p)));
            t.Goto("calloway");
            Assert.IsTrue(decide.Show(Ctx(p)));
            Assert.AreEqual("decide", Q.Start(Ctx(p)));
            var notYet = OptText("decide", "Not yet");
            Assert.IsTrue(notYet.End);
            t.Close();
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Turn.Questioning));
            Assert.IsFalse(g.World.Flag(Turn.Chosen));
            var ashby = Object.FindObjectsByType<NpcAgent>(FindObjectsSortMode.None).Single(n => n.Def.Id == "ashby_q");
            p.Hud.Push(new DialogueScreen(ashby.Def.Dialogue, ashby, null));
            yield return Frames(2);
            Assert.AreEqual("decide", Talk(p).NodeId, "any speaker picks up where you left off");
        }

        [UnityTest]
        public IEnumerator SwaysSetFlagsAndPayOnce()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 14, side: "hunters");
            yield return Frames(2);
            yield return Met(g, p);
            yield return Surface(g, p);
            var t = Talk(p);
            int xp = p.Character.XP;
            foreach (var f in new[] { "crown", "academy", "hunters" })
            {
                t.Goto($"sway_{f}_ok");
                Assert.IsTrue(g.World.Flag(Turn.Swayed(f)), f);
            }
            Assert.AreEqual(xp + 3000, p.Character.XP, "1000 each");
            t.Goto("sway_crown_ok");
            Assert.AreEqual(xp + 3000, p.Character.XP, "once");
            // The banner lowers the DC for your own side.
            Assert.IsTrue(Q.Nodes["calloway"].Options.Any(o => o.Check == Skill.Persuasion && o.DC == 13 && o.Show(Ctx(p))));
            Assert.IsFalse(Q.Nodes["calloway"].Options.Any(o => o.Check == Skill.Persuasion && o.DC == 16 && o.Show(Ctx(p))));
            // The fae favour: no roll.
            var favour = Q.Nodes["sorrel"].Options.First(o => o.Next == "sway_fae_favour");
            Assert.IsFalse(favour.Show(Ctx(p)));
            g.World.SetFlag("fae_clause");
            Assert.IsTrue(favour.Show(Ctx(p)));
            favour.Do(Ctx(p));
            Assert.IsTrue(g.World.Flag(Turn.FaeFavourCalled));
            Assert.IsTrue(g.World.Flag(Turn.Swayed("fae")));
            Assert.IsTrue(Q.Nodes.Values.SelectMany(n => n.Options).Where(o => o.Check.HasValue).All(o => !o.Social), "no roll locks another");
        }

        [UnityTest]
        public IEnumerator SaveGateSaysWhatIsMissingAndCanBeMet()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 15);
            yield return Frames(2);
            yield return Met(g, p, "seal_understood");
            yield return Surface(g, p);
            var t = Talk(p);
            t.Goto("confess");
            foreach (var n in new[] { "marsh", "ashby", "sorrel", "calloway" }) t.Goto(n);
            t.Goto("decide");
            var shown = Q.Nodes["decide"].Options.Where(o => o.Show(Ctx(p))).Select(o => o.Text).ToList();
            Assert.IsTrue(shown.Any(s => s.Contains("not ready")), "Save is listed, marked");
            Assert.IsFalse(shown.Contains("Save her."));
            t.Goto("gate_short");
            string n2 = Q.Nodes["gate_short"].Narration(Ctx(p));
            StringAssert.Contains(TurnDb.GatePact, n2);
            StringAssert.Contains(TurnDb.GateTrust, n2);
            Assert.IsFalse(n2.Contains(TurnDb.GateHeld));
            t.Close();
            yield return Frames(2);

            // Go back down, promise her, read the ring, come back up.
            g.World.SetFlag("promised_her");
            g.World.LoreFound.Add("the_great_ones_below");
            g.Warp(p, "roots", new Vector2Int(50, 20), false);
            yield return Frames(3);
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(3);
            Assert.IsNull(Beat(p), "no beats again");
            g.OpenQuestioning(p);
            yield return Frames(2);
            Assert.AreEqual("decide", Talk(p).NodeId);
            shown = Q.Nodes["decide"].Options.Where(o => o.Show(Ctx(p))).Select(o => o.Text).ToList();
            Assert.Contains("Save her.", shown);
            Assert.IsFalse(shown.Any(s => s.Contains("not ready")));
        }

        [UnityTest]
        public IEnumerator QuestioningSurvivesSaveLoad()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 16);
            yield return Frames(2);
            yield return Met(g, p);
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(5);
            Assert.IsNotNull(Beat(p), "beat 1");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(6);
            p = g.Players[0];
            Assert.IsNull(Beat(p), "loading never replays the beats");
            Assert.IsTrue(g.World.Flag(Turn.Questioning));
            Assert.AreEqual("open", Q.Start(Ctx(p)));
            g.OpenQuestioning(p);
            yield return Frames(2);
            var t = Talk(p);
            t.Goto("confess"); t.Goto("marsh"); t.Goto("sorrel"); t.Goto("sway_fae_ok");
            p.Hud.CloseAll();
            path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(6);
            p = g.Players[0];
            Assert.IsTrue(g.World.Flag(Turn.Confessed) && g.World.Flag(Turn.Heard("crown")) && g.World.Flag(Turn.Heard("fae")) && g.World.Flag(Turn.Swayed("fae")));
            Assert.AreEqual("voices", Q.Start(Ctx(p)));
            Assert.IsNull(Beat(p));
        }

        [UnityTest]
        public IEnumerator TurnDialogueRenders()
        {
            var g = GameRoot.I;
            foreach (var side in new[] { "crown", "academy", "fae", "hunters" })
            {
                var p = ActThree(g, 17, side: side);
                yield return Frames(2);
                g.World.SetFlag(WorldDb.MetHer);
                foreach (var flags in new[] { new string[0], new[] { "ring_link", "fae_clause" } })
                {
                    foreach (var f in flags) g.World.SetFlag(f);
                    foreach (var n in Q.Nodes.Values)
                    {
                        string t = n.Text(Ctx(p)) + " " + (n.Narration?.Invoke(Ctx(p)) ?? "");
                        Assert.IsFalse(t.Contains("{"), n.Id);
                        Assert.IsNotEmpty(t.Trim(), n.Id);
                        foreach (var o in n.Options) Assert.IsFalse(o.Text.Contains("{"), n.Id + ": " + o.Text);
                    }
                }
                g.QuitToTitle();
                yield return Frames(2);
            }
        }

        // ============================================================ Task 4: the choice and the break

        /// <summary>Surfaced, confessed, everyone heard, at decide.</summary>
        static IEnumerator AtDecide(GameRoot g, PlayerSlot p, params string[] flags)
        {
            yield return Met(g, p, flags);
            yield return Surface(g, p);
            var t = Talk(p);
            t.Goto("confess");
            foreach (var n in new[] { "marsh", "ashby", "sorrel", "calloway" }) t.Goto(n);
            t.Goto("decide");
        }

        static IEnumerator Pick(GameRoot g, PlayerSlot p, string ending)
        {
            g.OfferEnding(p, ending);
            yield return Frames(3);
        }

        static IEnumerator PlayBreak(GameRoot g, PlayerSlot p, List<string> seen)
        {
            for (int i = 0; i < 8; i++)
            {
                var b = Beat(p);
                if (b != null && b.Title == "To Be Continued") break;
                if (b != null) { seen.Add(b.Body); b.Close(); }
                yield return Frames(4);
            }
        }

        [UnityTest]
        public IEnumerator ChoiceNeedsEveryoneToAgree()
        {
            var g = GameRoot.I;
            var p1 = ActThree(g, 20);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(21), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return Met(g, p1);
            g.Warp(p1, "capital", new Vector2Int(58, 30), false);
            yield return Frames(5);
            for (int i = 0; i < 6 && Talk(p1) == null; i++) { Beat(p1)?.Close(); yield return Frames(4); }
            var t = Talk(p1);
            t.Goto("confess");
            foreach (var n in new[] { "marsh", "ashby", "sorrel", "calloway" }) t.Goto(n);

            yield return Pick(g, p1, Turn.End);
            Assert.IsNotNull(p1.Hud.Find<ChoiceScreen>());
            Assert.IsNotNull(p2.Hud.Find<ChoiceScreen>(), "both are asked");
            p1.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(2);
            Assert.IsFalse(g.World.Flag(Turn.Chosen), "not until everyone agrees");
            StringAssert.Contains("Waiting for", Beat(p1).Body);
            p2.Hud.Find<ChoiceScreen>().Choose("Not yet");
            yield return Frames(3);
            Assert.IsFalse(g.World.Flag(Turn.Chosen));
            Assert.IsNull(p1.Hud.Find<ChoiceScreen>()); Assert.IsNull(p2.Hud.Find<ChoiceScreen>());
            Assert.AreEqual("decide", Talk(p1)?.NodeId, "back to the question");

            yield return Pick(g, p1, Turn.End);
            p1.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(1);
            p2.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Turn.Chosen));
            Assert.IsTrue(g.World.Flag(Turn.EndingFlag(Turn.End)));
            Assert.AreEqual(WarmCity.Break, g.Quests.Stage(WarmCity.Id));
        }

        [UnityTest]
        public IEnumerator SoloChoosesWithOneYesAndEscCancels()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 22);
            yield return Frames(2);
            yield return AtDecide(g, p);
            yield return Pick(g, p, Turn.Seal);
            var c = p.Hud.Find<ChoiceScreen>();
            c.OnCancel(); // Esc / the close button
            c.Close();
            yield return Frames(3);
            Assert.IsFalse(g.World.Flag(Turn.Chosen));
            Assert.AreEqual("decide", Talk(p)?.NodeId);
            yield return Pick(g, p, Turn.Seal);
            p.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Turn.EndingFlag(Turn.Seal)));
        }

        [UnityTest]
        public IEnumerator ChoiceIsAtomicAndFinal()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 23);
            yield return Frames(2);
            yield return AtDecide(g, p);
            g.ChooseEnding(p, Turn.Seal);
            yield return Frames(3);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(6);
            p = g.Players[0];
            Assert.AreEqual(Turn.Seal, Turn.Ending(g.World));
            Assert.AreEqual(1, Turn.Endings.Count(e => g.World.Flag(Turn.EndingFlag(e))));
            for (int h = 0; h < 24; h++) foreach (var id in Speakers) Assert.IsNull(WorldDb.Current(WorldDb.Npcs[id], h, 0, g.World));
            g.ChooseEnding(p, Turn.Save);
            Assert.AreEqual(Turn.Seal, Turn.Ending(g.World), "once only");
            g.OfferEnding(p, Turn.End);
            yield return Frames(3);
            Assert.IsNull(p.Hud.Find<ChoiceScreen>(), "never offered again");
        }

        [UnityTest]
        public IEnumerator BetrayalCosts()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 24, side: "crown");
            yield return Frames(2);
            yield return AtDecide(g, p, "seal_understood", "promised_her", Turn.FaeFavourCalled);
            int before = Factions.Standing("crown");
            g.ChooseEnding(p, Turn.Save);
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Turn.Betrayed));
            Assert.AreEqual(System.Math.Max(-100, before - 50), Factions.Standing("crown"));
            var seen = new List<string>();
            yield return PlayBreak(g, p, seen);
            StringAssert.Contains("You stood with us", seen[1]);
            StringAssert.Contains("unpins the royal badge", seen[1], "Marsh still comes over: she was yours");
            g.QuitToTitle();
            yield return Frames(2);

            p = ActThree(g, 25, side: "hunters");
            yield return Frames(2);
            yield return AtDecide(g, p);
            g.ChooseEnding(p, Turn.End);
            yield return Frames(2);
            Assert.IsFalse(g.World.Flag(Turn.Betrayed));
        }

        [UnityTest]
        public IEnumerator BreakShowsWhoStandsWhere()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 26, side: "academy");
            yield return Frames(2);
            yield return AtDecide(g, p);
            g.ChooseEnding(p, Turn.Seal);
            var seen = new List<string>();
            yield return PlayBreak(g, p, seen);
            Assert.AreEqual(3, seen.Count);
            StringAssert.Contains("rolls up his sleeves", seen[1]);
            StringAssert.Contains("\"Guards.\"", seen[1]);
            StringAssert.Contains("Your funeral", seen[1]);
            Assert.AreEqual("To Be Continued", Beat(p)?.Title, "Phase 6 picks up here");
            Assert.IsTrue(GameRoot.BreakBeats(Turn.Seal, W(Turn.Swayed("crown")))[1].Contains("unpins the royal badge"));
        }

        [UnityTest]
        public IEnumerator DragonReactsToTheChoice()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 27);
            yield return Frames(2);
            yield return AtDecide(g, p);
            g.World.DragonBond = 50;
            g.ChooseEnding(p, Turn.End);
            Assert.AreEqual(40, g.World.DragonBond);
            StringAssert.Contains("won't move", GameRoot.BreakBeats(Turn.End, g.World)[2]);
            g.QuitToTitle();
            yield return Frames(2);
            p = ActThree(g, 28);
            yield return Frames(2);
            yield return AtDecide(g, p, "seal_understood", "promised_her", Turn.FaeFavourCalled);
            g.World.DragonBond = 50;
            g.ChooseEnding(p, Turn.Save);
            Assert.AreEqual(60, g.World.DragonBond);
        }

        [UnityTest]
        public IEnumerator EveryEndingIsChoosable()
        {
            var g = GameRoot.I;
            int seed = 30;
            foreach (var side in new[] { "crown", "academy", "fae", "hunters" })
                foreach (var e in Turn.Endings)
                {
                    var p = ActThree(g, seed++, side: side);
                    yield return Frames(2);
                    yield return Met(g, p, "seal_understood", "promised_her", Turn.FaeFavourCalled);
                    g.Warp(p, "capital", new Vector2Int(58, 30), false);
                    yield return Frames(4);
                    g.ChooseEnding(p, e);
                    yield return Frames(2);
                    Assert.AreEqual(e, Turn.Ending(g.World), side + " " + e);
                    Assert.AreEqual(1, Turn.Endings.Count(x => g.World.Flag(Turn.EndingFlag(x))));
                    g.QuitToTitle();
                    yield return Frames(2);
                }
        }

        // ============================================================ Task 5: golden paths and screenshots

        [UnityTest]
        public IEnumerator TurnGoldenPath()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 60, side: "academy");
            yield return Frames(2);
            // The end of Phase 4's golden path: the whole main quest paid, met her, promised, understood the seal.
            foreach (var st in new[] { WarmCity.Arrived, WarmCity.Audience, WarmCity.Below, WarmCity.Opened, WarmCity.Deep, WarmCity.Door }) g.Quests.Set(WarmCity.Id, st, p);
            yield return Met(g, p, "promised_her", "seal_understood");
            yield return Surface(g, p);
            var t = Talk(p);
            Assert.AreEqual("open", t.NodeId);
            t.Goto("lie_bad"); t.Goto("truth"); t.Goto("deny"); t.Goto("roar"); t.Goto("confess"); t.Goto("confess2"); t.Goto("voices");
            t.Goto("marsh"); t.Goto("voices"); t.Goto("ashby"); t.Goto("voices");
            t.Goto("sorrel"); t.Goto("sway_fae_ok"); t.Goto("voices"); t.Goto("calloway"); t.Goto("voices");
            t.Goto("decide");
            OptText("decide", "Save her.").Do(Ctx(p));
            yield return Frames(3);
            p.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(3);
            var seen = new List<string>();
            yield return PlayBreak(g, p, seen);
            Assert.AreEqual(3, seen.Count);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(6);
            p = g.Players[0];
            Assert.AreEqual(WarmCity.Break, g.Quests.Stage(WarmCity.Id));
            Assert.AreEqual(Turn.Save, Turn.Ending(g.World));
            Assert.GreaterOrEqual(p.Character.XP, PlayerCharacter.XpThresholds[13], "level 13 at the turn");
            for (int h = 0; h < 24; h++) foreach (var id in Speakers) Assert.IsNull(WorldDb.Current(WorldDb.Npcs[id], h, 0, g.World));
            var buoy = Object.FindObjectsByType<TownDog>().SingleOrDefault();
            Assert.IsNotNull(buoy, "Buoy is still about");
            Assert.AreEqual("town", buoy.Actor.Zone.Def.Id, "in Thistledown");
            StringAssert.Contains("Buoy", buoy.PromptText(p));
            g.QuitToTitle();
            yield return Frames(2);

            // End (the hunters' side): no betrayal, the Crown beside you.
            p = ActThree(g, 61, side: "hunters");
            yield return Frames(2);
            yield return AtDecide(g, p);
            yield return Pick(g, p, Turn.End);
            p.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(3);
            Assert.AreEqual(Turn.End, Turn.Ending(g.World));
            Assert.AreEqual(Stance.With, TurnDb.Reaction("crown", Turn.End, g.World));
            Assert.AreEqual(Stance.With, TurnDb.Reaction("hunters", Turn.End, g.World));
            Assert.IsFalse(g.World.Flag(Turn.Betrayed));
            g.QuitToTitle();
            yield return Frames(2);

            // Seal (the Crown's side, Marsh won over): Marsh defects, and the throne counts it a betrayal.
            p = ActThree(g, 62, side: "crown");
            yield return Frames(2);
            yield return AtDecide(g, p);
            Talk(p).Goto("sway_crown_ok");
            g.ChooseEnding(p, Turn.Seal);
            yield return Frames(2);
            Assert.IsTrue(TurnDb.MarshDefects(Turn.Seal, g.World));
            Assert.IsTrue(g.World.Flag(Turn.Betrayed));
            Assert.AreEqual(Stance.With, TurnDb.Reaction("academy", Turn.Seal, g.World));
        }

        [UnityTest]
        public IEnumerator CaptureTurn()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 90, "wizard", "fae");
            yield return Frames(2);
            g.Clock.Minutes = 12 * 60;
            yield return Met(g, p, "seal_understood", "fae_clause");
            g.Warp(p, "capital", new Vector2Int(58, 30), false);
            yield return Frames(40);
            AcademyTests.Shot("act5_beat1");
            Beat(p)?.Close();
            yield return Frames(20);
            AcademyTests.Shot("act5_beat2");
            Beat(p)?.Close();
            yield return Frames(20);
            p.Hud.CloseAll();
            p.Hud.HideNarration();
            yield return Frames(5);
            AcademyTests.Shot("act5_sunstair");
            g.OpenQuestioning(p);
            yield return Frames(3);
            AcademyTests.Shot("act5_open");
            var t = Talk(p);
            t.Goto("confess");
            yield return Frames(3);
            AcademyTests.Shot("act5_confess");
            t.Goto("confess2");
            yield return Frames(3);
            AcademyTests.Shot("act5_confess2");
            t.Goto("voices");
            yield return Frames(3);
            AcademyTests.Shot("act5_voices");
            t.Goto("sorrel");
            yield return Frames(3);
            AcademyTests.Shot("act5_sorrel");
            foreach (var n in new[] { "marsh", "ashby", "calloway" }) t.Goto(n);
            t.Goto("gate_short");
            yield return Frames(3);
            AcademyTests.Shot("act5_gate_short");
            t.Goto("decide");
            yield return Frames(3);
            AcademyTests.Shot("act5_decide");
            g.World.SetFlag("promised_her"); g.World.SetFlag(Turn.FaeFavourCalled);
            yield return Pick(g, p, Turn.Save);
            yield return Frames(3);
            AcademyTests.Shot("act5_confirm");
            p.Hud.Find<ChoiceScreen>().Choose("Yes");
            yield return Frames(5);
            Beat(p)?.Close();
            yield return Frames(10);
            AcademyTests.Shot("act5_break2");
            Beat(p)?.Close();
            yield return Frames(10);
            AcademyTests.Shot("act5_break3");
        }
    }
}
