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
    /// <summary>Act III Phase 2: into Act III (after the Warden, Start at Act III, the grown dragon).</summary>
    public class ActThreeIntoTests
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

        /// <summary>An Act II game with the Warden down and the End of Act II card on screen.</summary>
        static IEnumerator WardenDown(GameRoot g, PlayerSlot p, string side)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Factions.Commit(side, p);
            g.World.SetFlag("banner_" + side);
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            yield return Frames(2);
            g.World.SetFlag(GameRoot.WardenFallen);
            g.Quests.Set(Answers.Id, Answers.Rest, p);
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator BeginActThreeFromTheCard()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human", level: 10);
            yield return Frames(2);
            yield return WardenDown(g, p, "academy");
            Assert.IsTrue(g.ShowActTwoEnd());
            yield return Frames(2);
            var card = (ActEndScreen)p.Hud.Top;
            Assert.IsTrue(card.RowTexts.Any(t => t.StartsWith("Begin Act III")), "the End of Act II card offers Act III");
            int day = g.Clock.Day;
            g.BeginActThree(p);
            yield return Frames(2);
            Assert.AreEqual(3, g.World.Act);
            Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap, "the cap is 13");
            Assert.AreEqual(day + 14, g.Clock.Day, "two weeks pass");
            Assert.IsTrue(g.Dragon.IsGrown, "the dragon is grown");
            Assert.AreEqual(g.Dragon.MaxHP, g.Dragon.HP, "and fully rested");
            Assert.AreEqual("mon_dragon_grown", g.DragonAgent.Actor.Look, "with its grown look");
            Assert.AreEqual(GameRoot.GrownDragonScale, g.DragonAgent.Actor.transform.localScale.x, 0.01f);
            Assert.IsTrue(g.World.Flag(WorldDb.ActThreeFlag));
            var msg = p.Hud.Top as MessageScreen;
            Assert.IsNotNull(msg, "the summons is on screen");
        }

        [UnityTest]
        public IEnumerator NoBeginActThreeBeforeTheWarden()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            g.BeginActThree(p);
            Assert.AreEqual(2, g.World.Act, "the Warden still stands: Act II goes on");
        }

        [UnityTest]
        public IEnumerator ActThreeSaveLoadsWithCapThirteenAndGrownDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "wizard", "elf");
            yield return Frames(2);
            yield return WardenDown(g, p, "crown");
            g.BeginActThree(p);
            p.Hud.CloseAll();
            int act3Day = g.World.ActThreeDay;
            string path = SaveSystem.Save(false);
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(3, g.World.Act);
            Assert.AreEqual(act3Day, g.World.ActThreeDay, "the day Act III began is saved");
            Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap, "loading an Act III save sets the cap to 13");
            Assert.IsTrue(g.Dragon.IsGrown, "the dragon is still grown");
        }

        [UnityTest]
        public IEnumerator GrownDragonFightsAndBreathesThirtyFeet()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human", level: 11);
            yield return Frames(2);
            yield return WardenDown(g, p, "fae");
            g.BeginActThree(p);
            p.Hud.CloseAll();
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var du = c.Units.FirstOrDefault(u => u.C is Dragonling);
            Assert.IsNotNull(du, "the dragon joins the fight");
            if (du.Cb == null) du.Cb = new Combatant(du.C);
            var acts = c.DragonActions(du);
            Assert.IsTrue(acts.Any(a => a.Name == "Attack (Bite + Claw)"));
            var breath = acts.First(a => a.Name == "Ember Breath");
            Assert.AreEqual(30, breath.RangeFt, "a grown dragon's breath is a 30-ft cone");
        }

        [UnityTest]
        public IEnumerator StartAtActThreeForEachSide()
        {
            foreach (var side in new[] { "crown", "academy", "fae", "hunters" })
            {
                var g = GameRoot.I;
                var p = g.DebugStart(Still(5), "cleric", "dwarf");
                yield return Frames(2);
                g.ApplyActThreeStart(p, side);
                yield return Frames(2);
                Assert.AreEqual(3, g.World.Act, side);
                Assert.AreEqual(side, g.World.CommittedFaction, side);
                Assert.IsTrue(g.World.Flag("banner_" + side), side + ": the side's banner");
                Assert.IsTrue(g.World.Flag(GameRoot.WardenFallen) && g.World.Flag(GameRoot.ActTwoEndSeen), side + ": Act II is told");
                Assert.AreEqual(Answers.Rest, g.Quests.Stage(Answers.Id), side);
                Assert.IsTrue(g.World.BlightEased, side + ": the Warden's fall eased the blight");
                Assert.AreEqual(2, g.World.BlightStage, side + ": stage 2 (eased from 3), as after a played Act II");
                Assert.AreEqual(10, p.Character.Level, side + ": levels 1-10 picked for you");
                Assert.IsTrue(p.Character.CanLevelUp, side + ": level 11 is yours to choose");
                Assert.IsTrue(g.Dragon != null && g.Dragon.IsGrown, side + ": a grown dragon");
                Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap);
                g.QuitToTitle();
                yield return Frames(2);
            }
        }

        [UnityTest]
        public IEnumerator PartnerJoiningActThreeGetsTheActThreeKit()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human");
            yield return Frames(2);
            g.ApplyActThreeStart(p, "hunters");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(7), "rogue", "elf");
            yield return Frames(2);
            Assert.AreEqual(10, p2.Character.Level, "the partner arrives at level 10");
            Assert.GreaterOrEqual(p2.Character.XP, PlayerCharacter.XpThresholds[11], "with level 11 to choose");
        }
    }
}
