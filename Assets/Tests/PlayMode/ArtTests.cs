using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    public class ArtTests
    {
        string _tempSaves;

        // Ids that are deliberately code-drawn (overlays, the dice-colored UI) rather than pack art.
        static readonly HashSet<string> CodeDrawn = new HashSet<string> { "tile_none", "tile_grid", "tile_highlight", "tile_cursor", "ui_white", "ui_die", "missing" };

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

        [Test]
        public void ResolvePrefersRealArtVariants()
        {
            Assert.IsTrue(ArtRegistry.IsReal("test_real"), "test_real.png ships in Resources/Art for this test");
            Assert.IsFalse(ArtRegistry.IsReal("nope_not_a_sprite"));
            Assert.AreEqual("test_real", ArtRegistry.Resolve("nope_not_a_sprite", "test_real"));
            Assert.AreEqual("test_real", ArtRegistry.Resolve("test_real", "nope_not_a_sprite"));
            Assert.AreEqual("last_fallback", ArtRegistry.Resolve("nope_a", "last_fallback"), "nothing real: the last id (placeholder) wins");
        }

        [UnityTest]
        public IEnumerator EveryZoneDrawsOnlyRealArt()
        {
            var g = GameRoot.I;
            ArtRegistry.ClearCache();
            var p = g.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            var missing = new SortedSet<string>();
            foreach (var z in WorldDb.Zones.Values)
            {
                g.Warp(p, z.Id, z.DefaultSpawn, false);
                yield return Frames(30);
            }
            foreach (var id in ArtRegistry.Requested)
                if (!CodeDrawn.Contains(id) && !ArtRegistry.IsReal(id)) missing.Add(id);
            Assert.IsEmpty(missing, $"{missing.Count} sprite ids still use placeholder art: {string.Join(", ", missing)}");
        }
    }
}
