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

        // Ids that are deliberately code-drawn (overlays, the dice-colored UI and the parchment panels, kept for readability) rather than pack art.
        static readonly HashSet<string> CodeDrawn = new HashSet<string> { "tile_none", "tile_grid", "tile_highlight", "tile_cursor", "ui_white", "ui_die", "ui_panel", "ui_panel_dark", "ui_slot", "missing" };

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

        [Test]
        public void ImportedSpritesArePixelPerfect()
        {
            foreach (var id in new[] { "tile_grass", "hero_fighter_down_0" })
            {
                Assert.IsTrue(ArtRegistry.IsReal(id), $"{id} was sliced from the pack");
                var spr = ArtRegistry.Get(id);
                Assert.AreEqual(FilterMode.Point, spr.texture.filterMode, id);
                Assert.AreEqual(16f, spr.pixelsPerUnit, id);
                Assert.AreEqual(16f, spr.rect.width, id);
            }
        }

        [UnityTest]
        public IEnumerator HeroLookFallsBackToClassArt()
        {
            var p = GameRoot.I.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            yield return Frames(2);
            Assert.AreEqual("hero_fighter_human", p.Avatar.Look);
            StringAssert.StartsWith("hero_fighter_", p.Avatar.Sr.sprite.name);
            Assert.IsTrue(ArtRegistry.IsReal(p.Avatar.Sr.sprite.name), $"{p.Avatar.Sr.sprite.name} should be real art (class fallback)");
        }

        [UnityTest]
        public IEnumerator WalkCycleUsesFourFramesWhenAvailable()
        {
            var p = GameRoot.I.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            yield return Frames(2);
            var walker = Actor.Spawn<Actor>("walker", "hero_fighter_elf", true, p.Zone, p.MapPos);
            var seen = new HashSet<string>();
            walker.Moving = true;
            for (int i = 0; i < 120; i++) { yield return null; seen.Add(walker.Sr.sprite.name); }
            Object.Destroy(walker.gameObject);
            Assert.IsTrue(seen.Any(n => n.EndsWith("_2") || n.EndsWith("_3")), "frames 2/3 of the walk cycle play: " + string.Join(",", seen));
        }

        [Test]
        public void EveryItemHasRealArt()
        {
            var missing = new SortedSet<string>();
            foreach (var item in DnD.Rules.Content.ItemsDb.All.Values)
                if (!ArtRegistry.IsReal(item.Icon)) missing.Add(item.Icon);
            Assert.IsEmpty(missing, $"{missing.Count} item icons use placeholder art: {string.Join(" ", missing)}");
        }

        [UnityTest]
        public IEnumerator HiredCompanionsKeepTheirRealArt()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "fighter", "human");
            yield return Frames(2);
            foreach (var cls in DnD.Rules.Content.CharacterDb.Classes.Keys.ToList())
            {
                var pc = DnD.Rules.CharacterFactory.Create(DnD.Rules.CharacterFactory.DefaultChoices(cls, "dwarf", "Recruit " + cls));
                var comp = g.SpawnCompanion(p, pc, "hums");
                yield return Frames(2);
                Assert.IsTrue(ArtRegistry.IsReal(comp.Actor.Sr.sprite.name), $"{cls} companion draws real art, not {comp.Actor.Sr.sprite.name}");
                g.DismissCompanion(comp);
            }
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
