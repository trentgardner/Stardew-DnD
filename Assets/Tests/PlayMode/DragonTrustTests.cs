using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 2: the teenage dragon trusts some people and distrusts others.</summary>
    public class DragonTrustTests
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
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static IEnumerator WithDragon(GameRoot g, PlayerSlot p)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator TrustedNpcGivesAdvantageOnPersuasion()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "bard", "human");
            yield return WithDragon(g, p);
            Assert.AreEqual(DragonFeeling.Trust, DragonDb.Opinion("elowen", g.World));
            Assert.AreEqual(AdvantageState.Advantage, DragonDb.CheckAdvantage("elowen", Skill.Persuasion, true), "the dragon nuzzles Elowen: she softens");
            Assert.AreEqual(AdvantageState.Normal, DragonDb.CheckAdvantage("elowen", Skill.Persuasion, false), "only when the dragon is there");
        }

        [UnityTest]
        public IEnumerator DistrustedNpcGivesAdvantageOnInsightOnly()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "bard", "human");
            yield return WithDragon(g, p);
            Assert.AreEqual(DragonFeeling.Distrust, DragonDb.Opinion("perrin", g.World), "it doesn't like Perrin one bit");
            Assert.AreEqual(AdvantageState.Advantage, DragonDb.CheckAdvantage("perrin", Skill.Insight, true), "hackles up: something's off");
            Assert.AreEqual(AdvantageState.Normal, DragonDb.CheckAdvantage("perrin", Skill.Persuasion, true), "but it doesn't help you charm him");
        }

        [UnityTest]
        public IEnumerator UnknownNpcIsNeutral()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "bard", "human");
            yield return WithDragon(g, p);
            Assert.AreEqual(DragonFeeling.Neutral, DragonDb.Opinion("nobody_at_all", g.World));
            Assert.AreEqual(AdvantageState.Normal, DragonDb.CheckAdvantage("nobody_at_all", Skill.Insight, true));
            Assert.IsNull(DragonDb.Bark("nobody_at_all", "Cinder"), "no bark for strangers");
        }

        [UnityTest]
        public IEnumerator OpinionShiftIsSaved()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return WithDragon(g, p);
            Assert.AreEqual(DragonFeeling.Wary, DragonDb.Opinion("wick", g.World));
            DragonDb.Shift("wick", 3);
            Assert.AreEqual(DragonFeeling.Trust, DragonDb.Opinion("wick", g.World), "Wick won it over");
            DragonDb.Shift("wick", 5);
            Assert.AreEqual(DragonFeeling.Trust, DragonDb.Opinion("wick", g.World), "and it can't go past Trust");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(DragonFeeling.Trust, DragonDb.Opinion("wick", g.World), "the dragon remembers");
        }

        [UnityTest]
        public IEnumerator DragonEmotesNearNpcOncePerDay()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return WithDragon(g, p);
            var pet = g.DragonAgent.GetComponent<DragonPet>();
            Assert.IsTrue(pet.NoticeNpc("perrin"), "it hisses at Perrin");
            Assert.IsFalse(pet.NoticeNpc("perrin"), "once a day is enough");
            Assert.IsFalse(pet.NoticeNpc("tik_never_heard_of"), "strangers get nothing");
            g.Clock.Day++;
            Assert.IsTrue(pet.NoticeNpc("perrin"), "and again tomorrow");
        }

        [UnityTest]
        public IEnumerator JournalListsTrust()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human");
            yield return WithDragon(g, p);
            string line = DragonPet.JournalLine();
            StringAssert.Contains("trusts", line);
            StringAssert.Contains("Mother Elowen", line);
            StringAssert.Contains("wary of", line);
        }

        [UnityTest]
        public IEnumerator TeenSulksWhenNobodyPetsIt()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(7), "fighter", "human");
            yield return WithDragon(g, p);
            var pet = g.DragonAgent.GetComponent<DragonPet>();
            StringAssert.Contains("sulks", pet.MoodTick(), "ignored all day: a teenage sulk");
            pet.PetIt(p);
            Assert.IsNull(pet.MoodTick(), "petted: no sulk");
        }
    }
}
