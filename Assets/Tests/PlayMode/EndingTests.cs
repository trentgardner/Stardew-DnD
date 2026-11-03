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
    }
}
