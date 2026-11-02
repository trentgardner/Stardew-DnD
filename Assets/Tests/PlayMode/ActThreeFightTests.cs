using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act III Phase 1 acceptance: every class, with a companion of another class, fights the Crown at 11 and 13.</summary>
    public class ActThreeFightTests
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

        static readonly string[] Classes = { "barbarian", "bard", "cleric", "druid", "fighter", "monk", "paladin", "ranger", "rogue", "sorcerer", "warlock", "wizard" };

        /// <summary>A medium-to-hard encounter for a pair at that level.</summary>
        static string[] Foes(int level) =>
            level <= 11 ? new[] { "crown_knight", "crown_knight", "crown_knight", "crown_knight", "crown_battlemage" } : new[] { "stone_guardian", "crown_champion", "crown_champion" };

        IEnumerator FightAt(int level)
        {
            var g = GameRoot.I;
            for (int i = 0; i < Classes.Length; i++)
            {
                string cls = Classes[i], buddy = Classes[(i + 5) % Classes.Length];
                var p = g.DebugStart(new FuzzInput(cls.GetHashCode() ^ level) { ConfirmChance = 0.35f, EndTurnChance = 0.01f }, cls, "human", level: level);
                var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices(buddy, "dwarf", "Buddy " + buddy));
                pc.XP = 999999;
                while (pc.Level < level) pc.LevelUp(pc.AutoPlan(), null);
                var comp = g.SpawnCompanion(p, pc, "hums");
                comp.Manual = false; // the companion AI drives it, new spells and all
                yield return null;
                g.Warp(p, "forest", new Vector2Int(20, 20), false);
                yield return null; yield return null;
                p.Hud.CloseAll();
                g.StartGroupCombat(g.DebugSpawnGroup(p, Foes(level)), p);
                Assert.AreEqual(1, g.Combats.Count(c => !c.Finished), $"{cls} L{level}: one fight");
                Time.timeScale = 6;
                float start = Time.realtimeSinceStartup;
                while (g.AnyInCombat && Time.realtimeSinceStartup - start < 150f) yield return null;
                Time.timeScale = 1;
                Debug.Log($"[act3 fight] {cls} + {buddy} L{level}: {(g.AnyInCombat ? "STILL GOING" : "done")} in {Time.realtimeSinceStartup - start:0}s, hero {(p.Character.IsDown ? "down" : "standing")}");
                Assert.IsFalse(g.AnyInCombat, $"{cls} + {buddy} at level {level}: the fight finishes (won or lost) within 150 real seconds");
                g.QuitToTitle();
                yield return null;
            }
        }

        [UnityTest, Timeout(2400000)] public IEnumerator FightAtLevelElevenWithEachClass() { yield return FightAt(11); }
        [UnityTest, Timeout(2400000)] public IEnumerator FightAtLevelThirteenWithEachClass() { yield return FightAt(13); }
    }
}
