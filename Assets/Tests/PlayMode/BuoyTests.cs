using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Buoy: the neighbourhood dog in Thistledown (a black-and-white Aussie). Not a companion: you just get to pet him.</summary>
    public class BuoyTests
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

        [UnityTest]
        public IEnumerator BuoyLivesInThistledownAndLovesPets()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return null;
            var buoy = Object.FindObjectsByType<TownDog>().SingleOrDefault();
            Assert.IsNotNull(buoy, "Buoy is in town");
            Assert.AreEqual("town", buoy.Actor.Zone.Def.Id);
            StringAssert.Contains("Buoy", buoy.PromptText(p));
            StringAssert.Contains("BUOY", TownDog.CollarText, "his name is on his collar");
            Assert.IsTrue(ArtRegistry.Get("npc_buoy_0") != null && ArtRegistry.Get("npc_buoy_1") != null, "his own black-and-white sprite");

            p.Character.HeroicInspiration = false;
            buoy.PetHim(p);
            Assert.IsTrue(p.Character.HeroicInspiration, "nobody stays grumpy after petting Buoy: Heroic Inspiration, once a day");
            p.Character.HeroicInspiration = false;
            buoy.PetHim(p);
            Assert.IsFalse(p.Character.HeroicInspiration, "once a day");
            Assert.IsFalse(g.Companions.Any(c => c.GetComponent<TownDog>() != null), "he's the neighbourhood's dog, not a companion");
        }

        [UnityTest]
        public IEnumerator BuoyStaysInHisNeighbourhood()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(2), "fighter", "human");
            yield return null;
            var buoy = Object.FindObjectsByType<TownDog>().Single();
            Time.timeScale = 8;
            for (int i = 0; i < 600; i++)
            {
                Assert.LessOrEqual(Vector2.Distance(buoy.Actor.MapPos, buoy.HomeSpot), TownDog.Roam + 1.5f, "wanders, but stays near home");
                Assert.IsTrue(buoy.Actor.Zone.IsWalkable(buoy.Actor.MapPos), "never inside a wall or the river");
                yield return null;
            }
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator BuoyGetsAroundOnHisDays()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "fighter", "human");
            yield return null;
            var buoy = Object.FindObjectsByType<TownDog>().Single();
            Assert.AreEqual("town", buoy.Actor.Zone.Def.Id);
            g.Warp(p, "forest", WorldDb.Zones["forest"].DefaultSpawn, false); // nobody in town to see him go
            g.Clock.Day = 1 + WorldDb.Leafday;
            Time.timeScale = 8;
            float t = Time.realtimeSinceStartup;
            while (buoy.Actor.Zone.Def.Id != "ford" && Time.realtimeSinceStartup - t < 20f) yield return null;
            Time.timeScale = 1;
            Assert.AreEqual("ford", buoy.Actor.Zone.Def.Id, "Leafday: splashing about at Miller's Ford");
            Assert.IsTrue(buoy.Actor.Zone.IsWalkable(buoy.Actor.MapPos));
        }
    }
}
