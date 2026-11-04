using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>The title's little cinematic: Thistledown at golden hour behind the menu.</summary>
    public class TitleTests
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
            GameRoot.I.QuitToTitle();
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

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static TitleScreen Title => Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None).FirstOrDefault();

        [UnityTest]
        public IEnumerator TitleOpensOnThistledown()
        {
            var t = Title;
            Assert.IsNotNull(t);
            Assert.IsNotNull(t.Backdrop, "the town is behind the title");
            Assert.AreEqual("town", t.Backdrop.Zone.Def.Id);
            Assert.IsTrue(t.Backdrop.Walkers.Any(w => w.name == "Buoy"), "Buoy is there");
            Assert.GreaterOrEqual(t.Backdrop.Walkers.Count, 4, "and a few familiar faces");
            Assert.IsFalse(t.MenuReady, "the intro plays first");
            var at = t.Backdrop.CameraPosition;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(Vector3.Distance(at, t.Backdrop.CameraPosition), 0.05f, "the camera drifts");
            t.SkipIntro();
            yield return Frames(2);
            Assert.IsTrue(t.MenuReady, "any press goes straight to the menu");
        }

        [UnityTest]
        public IEnumerator StartingAGameTakesTheTitleTownAway()
        {
            Assert.IsNotNull(Title.Backdrop);
            var g = GameRoot.I;
            g.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            Assert.AreEqual(0, Object.FindObjectsByType<TitleBackdrop>(FindObjectsSortMode.None).Length, "gone at once, before the real town is built");
            yield return Frames(2);
            Assert.IsNull(Title, "and the title with it");
        }

        [UnityTest]
        public IEnumerator CaptureTitle()
        {
            yield return new WaitForSecondsRealtime(0.9f);
            AcademyTests.Shot("title_0_fade");
            yield return new WaitForSecondsRealtime(1.9f);
            AcademyTests.Shot("title_1_title");
            yield return new WaitForSecondsRealtime(3.2f);
            AcademyTests.Shot("title_2_menu");
            yield return new WaitForSecondsRealtime(8f);
            AcademyTests.Shot("title_3_drift");
        }
    }
}
