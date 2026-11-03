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
    /// <summary>Act III Phase 7: the endings (scenes, epilogue slides, credits, the dedication).</summary>
    public class EndingTests
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
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
            Dice.Rng = new SystemRandom();
            GameRoot.FlightResults.Clear();
        }

        static WorldState Chosen(string ending, params string[] flags)
        {
            var w = new WorldState();
            w.SetFlag(WorldDb.MetHer);
            w.SetFlag(Turn.EndingFlag(ending));
            w.SetFlag(Turn.Chosen);
            foreach (var f in flags) w.SetFlag(f);
            return w;
        }

        static IEnumerable<string> AllText(List<EndingScene> scenes) => scenes.SelectMany(s => s.Beats.Prepend(s.Title));
        static IEnumerable<string> AllText(List<EpilogueSlide> slides) => slides.SelectMany(s => new[] { s.Title, s.Text });

        // ================================================================== Task 1: the text

        [Test]
        public void EachEndingHasEnoughScenesAndSlides()
        {
            foreach (var e in Turn.Endings)
            {
                var w = Chosen(e);
                Assert.GreaterOrEqual(EndingDb.Scenes(e, w, new[] { "Tamsin", "Odo" }).Count, Ending.MinScenes, e + ": scenes");
                var slides = EndingDb.Slides(e, w, 120);
                Assert.GreaterOrEqual(slides.Count, Ending.MinSlides, e + ": slides");
                Assert.AreEqual(EndingDb.PonderLine(e), slides.Last().Text, e + ": it ends on the ponder line");
            }
        }

        [Test]
        public void ScenesNeverEmptyOrTemplated()
        {
            foreach (var e in Turn.Endings)
            {
                var bare = EndingDb.Scenes(e, Chosen(e), new string[0]);
                foreach (var t in AllText(bare).Concat(AllText(EndingDb.Slides(e, Chosen(e), 1))))
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(t), e + ": no empty text");
                    StringAssert.DoesNotContain("{", t, e + ": no template left in");
                }
                Assert.IsFalse(bare.Any(s => s.Title == EndingDb.CompanionsTitle), e + ": no companions, no companions scene");
                var two = EndingDb.Scenes(e, Chosen(e), new[] { "Tamsin", "Odo" }).Single(s => s.Title == EndingDb.CompanionsTitle);
                Assert.IsTrue(two.Beats.Any(b => b.Contains("Tamsin")) && two.Beats.Any(b => b.Contains("Odo")), e + ": each companion has a line");
            }
        }

        [Test]
        public void EndingsAreDifferent()
        {
            var her = Turn.Endings.Select(e => EndingDb.Scenes(e, Chosen(e), new string[0]).First().Beats).ToList();
            for (int a = 0; a < her.Count; a++)
                for (int b = a + 1; b < her.Count; b++)
                    Assert.IsEmpty(her[a].Intersect(her[b]), "each ending's first scene is its own");
            Assert.AreEqual(3, Turn.Endings.Select(EndingDb.PonderLine).Distinct().Count());
            Assert.AreEqual("The world is safe now. That's what everyone says. You find you can't say it out loud.", EndingDb.PonderLine(Turn.End));
        }

        [Test]
        public void SlidesFollowTheReactionTable()
        {
            foreach (var e in Turn.Endings)
                foreach (var side in Turn.Forces)
                    foreach (bool marsh in new[] { false, true })
                    {
                        var w = Chosen(e, "banner_" + side);
                        w.CommittedFaction = side;
                        if (marsh) w.SetFlag("banner_crown");
                        foreach (var f in Turn.Forces)
                            Assert.AreEqual(TurnDb.Reaction(f, e, w), EndingDb.ForceSlideStance(f, e, w), $"{e}/{side}/{marsh}: {f}");
                        var slides = EndingDb.Slides(e, w, 50);
                        foreach (var f in Turn.Forces) Assert.IsTrue(slides.Any(s => s.Title == EndingDb.ForceTitle(f)), $"{e}: a slide for {f}");
                    }
        }

        [Test]
        public void BuoyAlwaysComesHome()
        {
            foreach (var e in Turn.Endings)
            {
                var home = EndingDb.Scenes(e, Chosen(e), new string[0]).Single(s => s.Title == EndingDb.ThistledownTitle);
                Assert.AreEqual("town", home.Zone);
                Assert.IsTrue(home.Beats.Any(b => b.Contains("Buoy")), e + ": Buoy is there");
            }
        }

        [Test]
        public void RomanceLineNeedsEightHearts()
        {
            var w = Chosen(Turn.Save);
            string Home() => string.Join("\n", EndingDb.Scenes(Turn.Save, w, new string[0]).Single(s => s.Title == EndingDb.ThistledownTitle).Beats);
            StringAssert.DoesNotContain(EndingDb.RomanceMarker, Home());
            w.AddFriendship("rhoswen", 700);
            StringAssert.DoesNotContain(EndingDb.RomanceMarker, Home(), "seven hearts isn't enough");
            w.AddFriendship("rhoswen", 100);
            StringAssert.Contains(EndingDb.RomanceMarker, Home(), "eight hearts");
        }

        [Test]
        public void NoVerdicts()
        {
            foreach (var e in Turn.Endings)
                foreach (var t in AllText(EndingDb.Scenes(e, Chosen(e), new[] { "Tamsin" })).Concat(AllText(EndingDb.Slides(e, Chosen(e), 9))))
                    foreach (var bad in new[] { "the right choice", "you were right", "the wrong choice", "you chose wrong" })
                        StringAssert.DoesNotContain(bad, t.ToLowerInvariant(), e + ": the game never says which ending was right");
        }

        [Test]
        public void EndingFilesAreGated()
        {
            var files = Directory.GetFiles(Application.dataPath, "EndingDb.AfterTheDoor.cs", SearchOption.AllDirectories);
            Assert.AreEqual(1, files.Length);
            StringAssert.Contains("MetHer", File.ReadAllText(files[0]));
            Assert.IsFalse(LoreTests.GuardScans("EndingDb.AfterTheDoor.cs"));
        }

        // ================================================================== Task 2: the sequence, the epilogue, the credits

        [Test]
        public void DedicationIsInTheCredits()
        {
            Assert.AreEqual("To my beautiful fiancée Lauren, this game is dedicated. I love you forever.", GameRoot.Dedication);
            StringAssert.Contains(GameRoot.Dedication, GameRoot.CreditsText);
            Assert.IsTrue(GameRoot.CreditsText.TrimEnd().EndsWith(GameRoot.Dedication), "the dedication is the last thing in the credits");
            StringAssert.Contains("System Reference Document 5.2", GameRoot.CreditsText);
            StringAssert.Contains("Creative Commons Attribution 4.0", GameRoot.CreditsText);
            StringAssert.Contains("Ninja Adventure", GameRoot.CreditsText);
            StringAssert.Contains("not affiliated with", GameRoot.CreditsText);
            StringAssert.Contains("Buoy", GameRoot.CreditsText);
        }

        class Seen
        {
            public readonly List<string> Titles = new List<string>(), Slides = new List<string>();
            public string Credits; public bool HomeForThistledown; public bool SawDedication;
        }

        /// <summary>Closes everything the ending shows, for whoever leads, until the title screen (or the stop condition).</summary>
        static IEnumerator WatchTheEnding(GameRoot g, Seen seen, System.Func<bool> stop = null, float limit = 120f)
        {
            float start = Time.realtimeSinceStartup;
            while (g.InGame && Time.realtimeSinceStartup - start < limit)
            {
                if (stop != null && stop()) yield break;
                yield return null;
                var lead = g.Players.FirstOrDefault(p => p.IsReady);
                if (lead == null) continue;
                var top = lead.Hud.Top;
                if (top == null || top.Closed) continue;
                if (top is MessageScreen m)
                {
                    seen.Titles.Add(m.Title);
                    if (m.Title == EndingDb.ThistledownTitle) seen.HomeForThistledown = g.Players.Where(p => p.IsReady).All(p => p.Zone.Def.Id == "town");
                    yield return FinaleTests.Frames(2); m.Close();
                }
                else if (top is EpilogueScreen e) { seen.Slides.Add(e.Slide.Title); yield return FinaleTests.Frames(2); e.Close(); }
                else if (top is CreditsScreen c)
                {
                    seen.Credits = c.Text;
                    yield return new WaitForSecondsRealtime(2.2f); // the credits can be closed after two seconds
                    seen.SawDedication = c.ShowingDedication || c.Text.TrimEnd().EndsWith(GameRoot.Dedication);
                    c.Close();
                }
                yield return FinaleTests.Frames(2);
            }
        }

        /// <summary>The last fight for this ending, won at once. The ending begins.</summary>
        static IEnumerator WinTheFinale(GameRoot g, PlayerSlot p, string ending)
        {
            yield return FinaleTests.AtTheBoss(g, p, ending);
            var c = FinaleTests.BossFightOf(g, ending);
            yield return FinaleTests.BeatTheBoss(g, p, c, ending);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EndingPlaysScenesSlidesAndCredits()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "fae");
            yield return FinaleTests.Frames(2);
            yield return WinTheFinale(g, p, Turn.Save);
            var expected = EndingDb.Scenes(Turn.Save, g.World, g.CompanionsOf(p).Where(c => !c.IsDragon && c.Character != null).Select(c => c.Character.Name)).Select(s => s.Title).ToList();
            var slides = EndingDb.Slides(Turn.Save, g.World, g.Clock.Day).Select(s => s.Title).ToList();
            var seen = new Seen();
            yield return WatchTheEnding(g, seen);
            int at = 0;
            foreach (var t in expected) { int i = seen.Titles.IndexOf(t, at); Assert.GreaterOrEqual(i, 0, "scene in order: " + t); at = i + 1; }
            Assert.IsTrue(seen.HomeForThistledown, "home in Thistledown for the Thistledown scene");
            CollectionAssert.AreEqual(slides, seen.Slides, "every slide, in order (Esc/close advances one, never skips)");
            Assert.IsNotNull(seen.Credits, "the credits rolled");
            StringAssert.Contains(GameRoot.Dedication, seen.Credits);
            Assert.IsTrue(seen.SawDedication, "the credits end on the dedication");
            Assert.IsFalse(g.InGame, "back to the title");
            var saves = Directory.GetFiles(_tempSaves, "*", SearchOption.AllDirectories);
            Assert.IsTrue(saves.Any(f => File.ReadAllText(f).Contains(Ending.Complete)), "the completed story is saved");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EndingResumesAfterLoad()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "academy");
            yield return FinaleTests.Frames(2);
            yield return WinTheFinale(g, p, Turn.Seal);
            var seen = new Seen();
            yield return WatchTheEnding(g, seen, () => seen.Titles.Contains("Her"));
            Assert.IsTrue(g.World.Flag(Finale.Won));
            string path = SaveSystem.Save(false);
            g.QuitToTitle();
            yield return FinaleTests.Frames(2);
            g.LoadGame(path);
            yield return FinaleTests.Frames(5);
            var again = new Seen();
            yield return WatchTheEnding(g, again, () => again.Titles.Contains("Her"), 30f);
            Assert.IsTrue(again.Titles.Contains("Her"), "the ending starts again from the first scene");
            Assert.IsTrue(g.World.Flag(Finale.Flown));
            Assert.IsFalse(Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(a => a.Group.Id == Finale.BossGroup(Turn.Seal) && !a.Monster.IsDown), "the last fight isn't redone");
            Assert.IsTrue(g.EndingPlaying);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EpilogueSurvivesALeadDrop()
        {
            var g = GameRoot.I;
            var p1 = FinaleTests.Roller(g, "fae");
            yield return FinaleTests.Frames(2);
            var p2 = g.DebugStart(FinaleTests.Still(51), "cleric", "dwarf");
            yield return FinaleTests.Frames(2);
            p2.Hud.CloseAll();
            yield return WinTheFinale(g, p1, Turn.Save);
            var seen = new Seen();
            yield return WatchTheEnding(g, seen, () => seen.Slides.Count >= 2);
            g.DropOut(p1);
            yield return FinaleTests.Frames(5);
            yield return WatchTheEnding(g, seen);
            Assert.AreEqual(EndingDb.Slides(Turn.Save, g.World ?? new WorldState(), 1).Count, seen.Slides.Count, $"P2 saw the rest of the slides. titles=[{string.Join(" | ", seen.Titles)}] inGame={g.InGame} ending={g.EndingPlaying} players={g.Players.Count(x => x.IsReady)} top={g.Players.FirstOrDefault(x => x.IsReady)?.Hud.Top?.GetType().Name}");
            Assert.IsNotNull(seen.Credits, "and the credits");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator CompletedSaveLoadsQuietly()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "hunters");
            yield return FinaleTests.Frames(2);
            yield return FinaleTests.ToTheChoice(g, p);
            g.ChooseEnding(p, Turn.End);
            yield return FinaleTests.CloseAll(g);
            foreach (var f in new[] { Finale.Started, Finale.StreetDone(1), Finale.StreetDone(2), Finale.StreetDone(3), Finale.Flown, Finale.Won, Ending.Seen, Ending.Complete }) g.World.SetFlag(f);
            g.World.ClearFlag(Finale.StreetOpen(1));
            string path = SaveSystem.Save(false);
            g.QuitToTitle();
            yield return FinaleTests.Frames(2);
            g.LoadGame(path);
            yield return FinaleTests.Frames(8);
            p = g.Players.First();
            var notes = new List<string>();
            for (int i = 0; i < 10; i++) { var b = FinaleTests.Beat(p); if (b != null) { notes.Add(b.Title); b.Close(); } yield return FinaleTests.Frames(3); }
            Assert.AreEqual(1, notes.Count(t => t == "The story is told"), "one note");
            Assert.IsFalse(notes.Contains("Her"), "the ending doesn't replay");
            Assert.IsFalse(g.EndingPlaying);
            Assert.AreEqual(0, g.Combats.Count);
            Assert.IsNull(p.Hud.Find<EpilogueScreen>(), "no epilogue");
        }

        [UnityTest]
        public IEnumerator CaptureCredits()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "fae");
            yield return FinaleTests.Frames(3);
            p.Hud.CloseAll(); p.Hud.HideNarration();
            var c = new CreditsScreen();
            p.Hud.Push(c);
            yield return new WaitForSecondsRealtime(6f);
            AcademyTests.Shot("act7_credits_scroll");
            c.SkipToDedication();
            yield return FinaleTests.Frames(5);
            AcademyTests.Shot("act7_credits_dedication");
            Assert.IsTrue(c.ShowingDedication);
            c.Close();
            // One epilogue slide, for the look.
            var slide = EndingDb.Slides(Turn.Save, g.World, 120).Last();
            var e = new EpilogueScreen(slide, 10, 11);
            p.Hud.Push(e);
            yield return FinaleTests.Frames(5);
            AcademyTests.Shot("act7_slide_ponder");
            e.Close();
        }

        // ================================================================== Task 3

        [UnityTest]
        public IEnumerator SealFightIsLit()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "academy");
            yield return FinaleTests.Frames(2);
            yield return FinaleTests.AtTheBoss(g, p, Turn.Seal);
            var deep = g.Zones["the_deep"];
            var at = deep.ToWorld(FinaleDb.AshbyAtTheDoor);
            Assert.IsTrue(LightSource.All.Any(l => l != null && l.On && l.Radius >= 5f && Vector2.Distance(l.transform.position, at) <= 3f), "Ashby's circle lights the Deep");
        }

        [UnityTest]
        public IEnumerator LevelCapHoldsThroughTheEnd()
        {
            var g = GameRoot.I;
            var p = FinaleTests.Roller(g, "fae");
            yield return FinaleTests.Frames(2);
            yield return WinTheFinale(g, p, Turn.Save);
            Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap, "still 13 after the last fight");
            Assert.AreEqual(13, PlayerCharacter.ActThreeLevelCap);
        }
    }
}
