using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DnD.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DnD.Tests
{
    /// <summary>
    /// The automated playtest's reading pass: every line of every conversation, and every story beat from the turn to the
    /// credits, is shown in its real window and must fit (no text cut off or spilling out of the box), solo and split-screen.
    /// </summary>
    public class TextFitTests
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

        /// <summary>Text in this window that doesn't fit its box (best-fit text is measured at its smallest size).</summary>
        static IEnumerable<string> Spills(RectTransform root, string where)
        {
            foreach (var tx in root.GetComponentsInChildren<Text>(false))
            {
                if (string.IsNullOrWhiteSpace(tx.text)) continue;
                var r = tx.rectTransform.rect;
                if (r.width <= 0 || r.height <= 0) continue;
                var settings = tx.GetGenerationSettings(r.size);
                settings.resizeTextForBestFit = false;
                settings.fontSize = tx.resizeTextForBestFit ? tx.resizeTextMinSize : tx.fontSize;
                settings.horizontalOverflow = HorizontalWrapMode.Wrap;
                settings.verticalOverflow = VerticalWrapMode.Overflow;
                float scale = tx.pixelsPerUnit;
                float h = tx.cachedTextGeneratorForLayout.GetPreferredHeight(tx.text, settings) / scale;
                if (h > r.height + 2f)
                    yield return $"{where}: \"{(tx.text.Length > 70 ? tx.text.Substring(0, 70) + "..." : tx.text)}\" needs {h:0} of {r.height:0}";
            }
        }

        /// <summary>Every dialogue id the game can open (read from DialogueDb's own switch).</summary>
        static List<string> DialogueIds()
        {
            var ids = new HashSet<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts/Game/Dialogue"), "DialogueDb*.cs"))
                foreach (Match m in Regex.Matches(File.ReadAllText(f), @"case ""([a-z0-9_]+)"":\s*(?:d = |return )"))
                    ids.Add(m.Groups[1].Value);
            return ids.Where(id => DialogueDb.Get(id) != null).OrderBy(x => x).ToList();
        }

        IEnumerator EveryConversationFits(bool split)
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 1);
            yield return Frames(2);
            if (split) { var p2 = g.DebugStart(Still(2), "cleric", "dwarf"); yield return Frames(2); p2.Hud.CloseAll(); }
            var problems = new List<string>();
            var skipped = new List<string>();
            int nodes = 0;
            foreach (var id in DialogueIds())
            {
                var d = DialogueDb.Get(id);
                p.Hud.CloseAll();
                var npc = Object.FindObjectsByType<NpcAgent>(FindObjectsSortMode.None).FirstOrDefault(n => n.Def.Dialogue == id);
                var screen = new DialogueScreen(id, npc, null);
                bool opened = true;
                try { p.Hud.Push(screen); } catch (System.Exception) { opened = false; } // needs a companion or an encounter to open
                if (!opened) { p.Hud.CloseAll(); skipped.Add(id); continue; }
                yield return Frames(1);
                foreach (var nodeId in d.Nodes.Keys.ToList())
                {
                    try { screen.Goto(nodeId); } catch (System.Exception e) { problems.Add($"{id}/{nodeId}: throws {e.GetType().Name}"); continue; }
                    if (screen.Closed || screen.Root == null) { p.Hud.CloseAll(); screen = new DialogueScreen(id, npc, null); p.Hud.Push(screen); yield return Frames(1); continue; }
                    Canvas.ForceUpdateCanvases();
                    problems.AddRange(Spills(screen.Root, $"{id}/{nodeId}"));
                    nodes++;
                }
                p.Hud.CloseAll();
            }
            Debug.Log($"TEXT FIT: {nodes} conversation nodes read ({(split ? "split" : "solo")}), {problems.Count} problems; skipped (need a companion or encounter): {string.Join(", ", skipped)}");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(60)));
        }

        [UnityTest, Timeout(600000)] public IEnumerator EveryConversationFitsSolo() => EveryConversationFits(false);
        [UnityTest, Timeout(600000)] public IEnumerator EveryConversationFitsSplitScreen() => EveryConversationFits(true);

        [UnityTest, Timeout(600000)]
        public IEnumerator EveryStoryBeatFits()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 3);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(4), "cleric", "dwarf"); // the narrowest windows: split-screen
            yield return Frames(2);
            var beats = new List<(string where, string title, string text)>();
            foreach (var e in Turn.Endings)
            {
                var w = new WorldState();
                w.SetFlag(WorldDb.MetHer); w.SetFlag(Turn.EndingFlag(e)); w.SetFlag(Turn.Chosen);
                foreach (var b in GameRoot.BreakBeats(e, w)) beats.Add(($"break/{e}", "", b));
                for (int n = 1; n <= Finale.Stages; n++) foreach (var b in FinaleDb.StreetIntro(n, e, w)) beats.Add(($"street{n}/{e}", FinaleDb.StreetTitle(n), b));
                var route = FinaleDb.Route(e, w);
                foreach (var b in route.Takeoff.Concat(route.Landing)) beats.Add(($"flight/{e}", "", b));
                foreach (var b in FinaleDb.Victory(e, w)) beats.Add(($"victory/{e}", "", b));
                foreach (var s in EndingDb.Scenes(e, w, new[] { "Tamsin Wick-Hollowell", "Odo" })) foreach (var b in s.Beats) beats.Add(($"ending/{e}/{s.Title}", s.Title, b));
            }
            foreach (var id in new[] { "ashen_queen", "king_regalia", "crowned_fire" })
                foreach (var ph in DnD.Rules.Content.MonstersDb.Get(id).Phases) foreach (var b in ph.Beats) beats.Add(($"phase/{id}", "", b));
            var problems = new List<string>();
            foreach (var pl in new[] { p, p2 })
                foreach (var (where, title, text) in beats)
                {
                    pl.Hud.CloseAll();
                    var m = new MessageScreen(title, text);
                    pl.Hud.Push(m);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    problems.AddRange(Spills(m.Root, $"{where} (player {pl.Index + 1})"));
                }
            Debug.Log($"TEXT FIT: {beats.Count} story beats × 2 screens, {problems.Count} problems");
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(60)));
        }
    }
}
