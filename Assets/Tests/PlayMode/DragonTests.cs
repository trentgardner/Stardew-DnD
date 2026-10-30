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
    /// <summary>Phase 6: the party's baby dragon.</summary>
    public class DragonTests
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
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Hatches the egg for p (guardians talked down) and names the dragon, skipping the UI.</summary>
        static void Hatch(GameRoot g, PlayerSlot p, string name = "Cinder")
        {
            g.PacifyGroup("mine_egg", p);
            g.HatchEgg(p, Vector3.zero);
            p.Hud.CloseAll();
            g.NameDragon(p, name);
        }

        // ============================================================ Task 1: the creature and party ownership

        [UnityTest]
        public IEnumerator HatchedDragonIsAPartyCreature()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            g.Warp(p, "egg_shrine", WorldDb.Zones["egg_shrine"].DefaultSpawn, false);
            yield return Frames(2);
            Assert.IsNull(g.Dragon, "no dragon before the hatching");
            Hatch(g, p, "Ziggy");
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual(Team.Players, g.Dragon.Team);
            Assert.AreEqual("Ziggy", g.Dragon.Name);
            Assert.AreEqual(10, g.Dragon.MaxHP, "party level 1");
            Assert.IsNotNull(g.DragonAgent);
            Assert.AreSame(g.Dragon, g.DragonAgent.Actor.Creature);
        }

        [UnityTest]
        public IEnumerator DragonFollowsNearestPlayerWithoutFlicker()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(2), "fighter", "human");
            var p2 = g.DebugStart(Still(3), "cleric", "elf");
            yield return Frames(2);
            g.Warp(p1, "town", new Vector2Int(20, 20), false);
            g.Warp(p2, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(2);
            Hatch(g, p1);
            yield return Frames(2);
            var dragon = g.DragonAgent;
            for (int i = 0; i < 60; i++)
            {
                Assert.AreSame(p1, dragon.Leader, "P2 is in another zone: the dragon stays with P1");
                Assert.AreEqual("town", dragon.Actor.Zone.Def.Id);
                yield return null;
            }

            // Both in town: P2 comes over to the dragon while P1 walks off.
            g.Warp(p2, "town", new Vector2Int(20, 21), false);
            yield return Frames(2);
            Assert.AreSame(p1, dragon.Leader, "P2 isn't clearly nearer yet: no hand-off (hysteresis)");
            p1.Avatar.PlaceAt(p1.Zone, new Vector2Int(40, 20)); // P1 alone (MoveWithin would bring the party along)
            int switches = 0;
            var last = dragon.Leader;
            for (int i = 0; i < 60; i++)
            {
                if (dragon.Leader != last) { switches++; last = dragon.Leader; }
                yield return null;
            }
            Assert.AreSame(p2, dragon.Leader, "the dragon follows whoever is nearest");
            Assert.AreEqual(1, switches, "one clean hand-off, no flicker");
        }

        [UnityTest]
        public IEnumerator RestRestoresTheDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            g.Dragon.HP = 2;
            g.Dragon.BreathReady = false;
            g.World.DragonScared = true;
            g.World.DragonSniffUsed = true;
            g.OnPartyRest(false);
            Assert.AreEqual(g.Dragon.MaxHP, g.Dragon.HP);
            Assert.IsTrue(g.Dragon.BreathReady);
            Assert.IsFalse(g.World.DragonScared);
            Assert.IsFalse(g.World.DragonSniffUsed);
        }

        [UnityTest]
        public IEnumerator DragonDamageAndBondSurviveSaveAndLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p, "Biscuit");
            g.Dragon.HP = 4;
            g.World.DragonBond = 37;
            g.World.DragonScared = true;
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual("Biscuit", g.Dragon.Name);
            Assert.AreEqual(4, g.Dragon.HP);
            Assert.AreEqual(37, g.World.DragonBond);
            Assert.IsTrue(g.World.DragonScared);
        }

        [UnityTest]
        public IEnumerator OldHatchedSaveGetsAWorkingDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            string path = SaveSystem.Save(false);
            // A save from before Phase 6: no dragon fields at all except the name and leader.
            var lines = File.ReadAllLines(path).Where(l => !(l.Contains("\"Dragon") && !l.Contains("\"DragonName") && !l.Contains("\"DragonLeader")));
            File.WriteAllLines(path, lines);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual(g.Dragon.MaxHP, g.Dragon.HP);
            Assert.AreEqual(0, g.World.DragonBond);
            Assert.IsFalse(g.World.DragonScared);
        }
    }
}
