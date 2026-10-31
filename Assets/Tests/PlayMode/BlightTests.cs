using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 2: blight stages (the Ashen Queen's influence, seen in the world).</summary>
    public class BlightTests
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

        [UnityTest]
        public IEnumerator StageZeroLooksLikeActOne()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            Assert.AreEqual(0, g.World.BlightStage);
            Assert.AreEqual(0f, Blight.OverlayAlpha(g.Zones["town"]), "no ash at stage 0");
        }

        [UnityTest]
        public IEnumerator BlightTintScalesWithStageAndWeight()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            Blight.SetStage(2);
            float town = Blight.OverlayAlpha(g.Zones["town"]), forest = Blight.OverlayAlpha(g.Zones["forest"]), tavern = Blight.OverlayAlpha(g.Zones["tavern"]);
            Assert.Greater(forest, town, "the wild is greyer than the village");
            Assert.Greater(town, tavern, "and indoors is the least grey");
            Blight.SetStage(3);
            Assert.Greater(Blight.OverlayAlpha(g.Zones["forest"]), forest, "a higher stage is greyer");
        }

        [UnityTest]
        public IEnumerator BlightTintUpdatesInPlace()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            var zone = p.Zone;
            Blight.SetStage(1);
            yield return Frames(1);
            var overlay = zone.transform.Find("BlightOverlay");
            Assert.IsNotNull(overlay, "the ash overlay appears in the zone you're standing in");
            Assert.Greater(overlay.GetComponent<SpriteRenderer>().color.a, 0f);
        }

        [UnityTest]
        public IEnumerator EaseLowersTheStageAndIsSaved()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            Blight.SetStage(3);
            Blight.Ease();
            Assert.AreEqual(2, g.World.BlightStage);
            Assert.IsTrue(g.World.BlightEased);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.AreEqual(2, g.World.BlightStage);
            Assert.IsTrue(g.World.BlightEased);
            Assert.Greater(Blight.OverlayAlpha(g.Zones["forest"]), 0f, "and it still shows after loading");
        }

        [UnityTest]
        public IEnumerator DialogueVariantShowsAtStageOne()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return Frames(2);
            var ctx = new DCtx { P = p };
            string before = DialogueDb.Get("garrick").Nodes["start"].Text(ctx);
            Blight.SetStage(1);
            string after = DialogueDb.Get("garrick").Nodes["start"].Text(ctx);
            Assert.AreNotEqual(before, after, "Garrick talks about the forge going cold");
            StringAssert.Contains("lit", after);
            StringAssert.Contains("cold", DialogueDb.Get("pip").Nodes["start"].Text(ctx), "Pip's heard about a hearth going cold");
        }
    }
}
