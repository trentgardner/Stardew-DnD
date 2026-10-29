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
    public class LevelUpTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f; // tests run silently
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

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static ScriptInput Quiet() => new ScriptInput();

        [UnityTest]
        public IEnumerator LevelUpScreenAppliesThePlayersPicks()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Quiet(), "wizard", "elf");
            p.Character.XP = 300;
            yield return Frames(2);
            var screen = new LevelUpScreen();
            p.Hud.Push(screen);
            yield return Frames(2);
            int hpBefore = p.Character.MaxHP;
            screen.PickHpAverage();
            // Wizard 1→2: Scholar expertise, then 2 spellbook spells. Pick the 2nd and 3rd listed spells.
            while (screen.CurrentChoice != null)
            {
                var c = screen.CurrentChoice;
                var picks = c.Kind == ChoiceKind.Spell ? c.Options.Skip(1).Take(c.Count).ToList() : c.Options.Take(c.Count).ToList();
                foreach (var id in picks) screen.Toggle(id);
                screen.Next();
            }
            Assert.AreEqual(1, p.Character.Level, "nothing changes before Confirm");
            screen.Confirm();
            yield return Frames(2);
            Assert.AreEqual(2, p.Character.Level);
            Assert.AreEqual(p.Character.AverageHitDie + p.Character.Mod(Ability.CON), p.Character.MaxHP - hpBefore);
            Assert.AreEqual(1, p.Character.Expertise.Count, "Scholar expertise picked");
        }

        [UnityTest]
        public IEnumerator EveryClassLevelsToFourThroughTheScreen()
        {
            var g = GameRoot.I;
            foreach (var cls in DnD.Rules.Content.CharacterDb.Classes.Keys.ToList())
            {
                var p = g.DebugStart(Quiet(), cls, "human");
                p.Character.XP = 2700;
                yield return Frames(2);
                for (int lvl = 2; lvl <= 4; lvl++)
                {
                    var screen = new LevelUpScreen();
                    p.Hud.Push(screen);
                    yield return Frames(2);
                    screen.PickHpAverage();
                    int guard = 0;
                    while (screen.CurrentChoice != null && guard++ < 20)
                    {
                        var c = screen.CurrentChoice;
                        foreach (var id in c.Options.Take(c.Count).ToList()) screen.Toggle(id);
                        screen.Next();
                    }
                    Assert.Less(guard, 20, $"{cls} {lvl}: stuck on a choice ({screen.CurrentChoice?.Title})");
                    screen.Confirm();
                    yield return Frames(2);
                    Assert.AreEqual(lvl, p.Character.Level, $"{cls} reaches level {lvl}");
                }
                g.QuitToTitle();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EscapeBacksOutWithoutChanges()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            var p = g.DebugStart(input, "fighter", "human");
            p.Character.XP = 300;
            yield return Frames(2);
            p.Hud.Push(new LevelUpScreen());
            yield return Frames(2);
            input.Esc = true;
            yield return Frames(3);
            Assert.IsFalse(p.Hud.Top is LevelUpScreen);
            Assert.AreEqual(1, p.Character.Level);
        }

        [UnityTest]
        public IEnumerator CharacterSheetOpensTheLevelUpScreen()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Quiet(), "fighter", "human");
            p.Character.XP = 300;
            yield return Frames(2);
            var sheet = new CharacterSheetScreen();
            p.Hud.Push(sheet);
            yield return Frames(2);
            sheet.StartLevelUp();
            yield return Frames(2);
            Assert.IsTrue(p.Hud.Top is LevelUpScreen, "the sheet's LEVEL UP opens the level-up screen");
        }

        [UnityTest]
        public IEnumerator CompanionsStillLevelAutomatically()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Quiet(), "fighter", "human");
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("bard", "gnome", "Lute"));
            var comp = g.SpawnCompanion(p, pc, "hums");
            yield return Frames(2);
            g.GiveXP(p, 2700, "test");
            yield return Frames(2);
            Assert.AreEqual(4, comp.Character.Level, "companions mirror their leader's XP and auto-level to 4");
        }
    }
}
