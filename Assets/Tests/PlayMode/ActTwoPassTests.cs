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
    /// <summary>Act II Phase 7: pacing, the End of Act II card, and the act end to end.</summary>
    public class ActTwoPassTests
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

        const int Start = 6500, Level9 = 48000, Level10 = 64000;

        static void Band(int players)
        {
            int main = ActTwoXp.Available(false, players, false), mainWarden = ActTwoXp.Available(false, players, true), all = ActTwoXp.Available(true, players, false);
            Debug.Log($"Act II XP per player ({players}p), on top of {Start}: main path to the gate {main}, main + the Warden {mainWarden}, everything to the gate {all}");
            Assert.GreaterOrEqual(main, Level9 - Start, "the main path reaches level 9 by the gate");
            Assert.GreaterOrEqual(mainWarden, Level10 - Start, "the Warden's fall brings level 10");
            Assert.GreaterOrEqual(all, Level10 - Start, "a completionist is level 10 by the gate");
            Assert.LessOrEqual(all, (int)(Level10 * 1.3) - Start, "but not so far past it that the finale is a formality");
        }

        [Test] public void ActTwoXpBandHoldsForSolo() => Band(1);
        [Test] public void ActTwoXpBandHoldsForTwoPlayers() => Band(2);

        [UnityTest]
        public IEnumerator LevelTenIsTheCap()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Assert.AreEqual(10, PlayerCharacter.LevelCap);
            var pc = p.Character;
            pc.XP = 70000;
            int guard = 0;
            while (pc.CanLevelUp && guard++ < 20) pc.LevelUp(pc.AutoPlan(), new System.Collections.Generic.List<string>());
            Assert.AreEqual(10, pc.Level, "level 10 and no further in Act II");
            Assert.IsFalse(pc.CanLevelUp);
        }
    }
}
