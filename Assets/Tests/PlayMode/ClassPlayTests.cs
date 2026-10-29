using System.Collections;
using System.IO;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    public class ClassPlayTests
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

        [UnityTest]
        public IEnumerator DruidWildShapesAndChangesLook()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new ScriptInput(), "druid", "elf");
            var pc = p.Character;
            pc.XP = 300;
            pc.LevelUp(pc.AutoPlan(), null);
            yield return Frames(2);

            Assert.IsTrue(pc.StartWildShape("wolf", null));
            WildShapeFx.Transform(p.Avatar, pc);
            yield return Frames(3);
            StringAssert.StartsWith("mon_wolf", p.Avatar.Sr.sprite.name, "the druid now looks like a wolf");
            Assert.IsTrue(ArtRegistry.IsReal(p.Avatar.Sr.sprite.name), "wolf art is real");

            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            var back = g.Players[0];
            Assert.IsNotNull(back.Character.WildShape, "Wild Shape survives a save and load");
            StringAssert.StartsWith("mon_wolf", back.Avatar.Sr.sprite.name, "still a wolf after loading");

            back.Character.EndWildShape(null);
            WildShapeFx.Transform(back.Avatar, back.Character);
            yield return Frames(3);
            StringAssert.StartsWith("hero_druid", back.Avatar.Sr.sprite.name, "back to the druid's own look");
        }
    }
}
