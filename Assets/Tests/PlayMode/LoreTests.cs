using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 2: the lore journal, and a guard that keeps THE SECRET out of every line of text.</summary>
    public class LoreTests
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
        public IEnumerator FindingLoreRecordsItOnceWithXp()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "wizard", "human");
            yield return Frames(2);
            int xp = p.Character.XP;
            Assert.IsTrue(LoreDb.Find("ashen_queen", p));
            Assert.IsTrue(g.World.LoreFound.Contains("ashen_queen"));
            Assert.AreEqual(xp + LoreDb.FindXp, p.Character.XP, "discovery XP");
            Assert.IsFalse(LoreDb.Find("ashen_queen", p), "only once");
            Assert.AreEqual(xp + LoreDb.FindXp, p.Character.XP);
        }

        [UnityTest]
        public IEnumerator JournalLorePageListsFoundEntries()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "wizard", "human");
            yield return Frames(2);
            LoreDb.Find("missing_pages", p);
            var j = new JournalScreen();
            p.Hud.Push(j);
            yield return Frames(2);
            j.ShowLore(true);
            StringAssert.Contains("Missing Pages", j.Text);
            StringAssert.DoesNotContain("The Ashen Queen", j.Text, "only what you've found");
        }

        [UnityTest]
        public IEnumerator LoreSpotIsInteractableAndSaved()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "wizard", "human");
            yield return Frames(2);
            g.World.Act = 2;
            g.Warp(p, "temple", new Vector2Int(2, 2), false);
            yield return Frames(3);
            var spot = Object.FindObjectsByType<LoreSpot>(FindObjectsSortMode.None).FirstOrDefault(s => s.Def.Id == "ashen_queen");
            Assert.IsNotNull(spot, "the temple mural is in the temple");
            spot.Interact(p);
            Assert.IsTrue(g.World.LoreFound.Contains("ashen_queen"));
            p.Hud.CloseAll();
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsTrue(g.World.LoreFound.Contains("ashen_queen"), "found lore is saved");
            foreach (var id in new[] { "sleeping_mountains", "skipping_rhyme", "missing_pages" })
                Assert.IsTrue(Object.FindObjectsByType<LoreSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(s => s.Def.Id == id), $"{id} is placed somewhere");
        }

        static readonly string[] LegendSpots = { "ashen_queen", "sleeping_mountains", "skipping_rhyme", "missing_pages" };

        [UnityTest]
        public IEnumerator ActOneHidesTheLegendUntilActTwo()
        {
            // The user: Act I is light. The legend and the clues wait for Act II.
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "wizard", "human");
            yield return Frames(2);
            Assert.AreEqual(1, g.World.Act);
            var spots = Object.FindObjectsByType<LoreSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => LegendSpots.Contains(s.Def.Id)).ToList();
            Assert.AreEqual(4, spots.Count);
            yield return Frames(1);
            foreach (var s in spots)
            {
                Assert.IsFalse(s.Available(p), $"{s.Def.Id} can't be read in Act I");
                Assert.IsFalse(s.GetComponent<SpriteRenderer>().enabled, $"{s.Def.Id} isn't even visible in Act I");
            }
            g.BeginActTwo(p);
            yield return Frames(2);
            foreach (var s in spots)
            {
                Assert.IsTrue(s.Available(p), $"{s.Def.Id} appears in Act II");
                Assert.IsTrue(s.GetComponent<SpriteRenderer>().enabled);
            }
        }

        [UnityTest]
        public IEnumerator ActOneHasOnlyAFewLightChestNotes()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "wizard", "human");
            yield return Frames(2);
            var actOne = LoreDb.All.Values.Where(l => l.MinAct <= 1).ToList();
            Assert.That(actOne.Count, Is.InRange(1, 2), "one or two ominous notes in Act I, no more");
            foreach (var l in actOne)
            {
                string t = (l.Title + " " + l.Text).ToLowerInvariant();
                foreach (var heavy in new[] { "queen", "ashen", "academy", "scholar", "crown", "weak", "pact", "legend" })
                    Assert.IsFalse(t.Contains(heavy), $"Act I note '{l.Title}' stays light (mentions '{heavy}')");
                var chest = Object.FindObjectsByType<ChestInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.Def != null && c.Def.Lore == l.Id);
                Assert.IsNotNull(chest, $"'{l.Title}' is found in a chest");
                chest.Interact(p);
                Assert.IsTrue(g.World.LoreFound.Contains(l.Id), $"opening the chest records '{l.Title}'");
                p.Hud.CloseAll();
            }
        }

        /// <summary>A banned word at the start of a word ("child" catches "children"; "mother" doesn't catch "Dawnmother").</summary>
        static bool Leaks(string lower, string word) => Regex.IsMatch(lower, @"\b" + Regex.Escape(word.Trim()));

        /// <summary>
        /// THE SECRET (spec): nothing may hint that the Ashen Queen is a child, or that the Crown is behind it. Every line of
        /// game text that names the Queen is checked against words that would give it away.
        /// </summary>
        static readonly string[] Banned = { "child", "young", "little", "small", "baby", "hatchling", "daughter", " son", "girl", "crying", "cries", "afraid",
            "frightened", "scared", "innocent", "mother", "orphan", "kid", "crown did", "the king did", "royal order", "by order of the crown" };
        /// <summary>Lines that talk about her, her servants, her waking, the Warden who keeps her in, or the door (Phase 7: every Act II voice).</summary>
        const string Topic = @"\bqueen\b|\bashen\b|\bashsworn\b|(?<!egg-)\bwarden\b|\bwaking\b|\bthe door\b";

        /// <summary>The guard itself works: a planted leak is caught (Phase 7 found its word matcher had never matched anything).</summary>
        [Test]
        public void SecretGuardCatchesALeak()
        {
            string planted = "the warden was only a child when the crown did it".ToLowerInvariant();
            Assert.IsTrue(Regex.IsMatch(planted, Topic), "the topic is recognised");
            Assert.IsTrue(Banned.Any(b => Leaks(planted, b)), "and the leak is caught");
            Assert.IsFalse(Leaks("the dawnmother's temple", "mother"), "a banned word only counts at the start of a word");
        }

        [Test]
        public void NoActTwoTextHintsAtTheSecret()
        {
            string[] banned = Banned;
            var root = Path.Combine(Application.dataPath, "Scripts");
            var offenders = new System.Collections.Generic.List<string>();
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                foreach (var line in File.ReadAllLines(file))
                {
                    if (line.TrimStart().StartsWith("//")) continue; // code comments aren't game text
                    // Only the text the player reads: the string literals on the line (not identifiers like ScareDC).
                    var l = string.Join(" ", Regex.Matches(line, @"""[^""]*""").Cast<Match>().Select(m => m.Value)).ToLowerInvariant();
                    if (!Regex.IsMatch(l, Topic)) continue;
                    foreach (var b in banned)
                        if (Leaks(l, b)) offenders.Add($"{Path.GetFileName(file)}: '{b}' in: {line.Trim()}");
                }
            foreach (var lore in LoreDb.All.Values)
                if (Regex.IsMatch(lore.Text.ToLowerInvariant(), Topic))
                    foreach (var b in banned) if (Leaks(lore.Text.ToLowerInvariant(), b)) offenders.Add($"lore {lore.Id}: '{b}'");
            Assert.IsEmpty(offenders, "secret leak:\n" + string.Join("\n", offenders));
        }
    }
}
