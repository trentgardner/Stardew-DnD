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
    /// <summary>
    /// The HUD's spacing (the user, 2026-11-03: "make sure all hud items are spaced correctly. no overlapping text. even spacing").
    /// Every element is forced on at once (a long narration, a stack of toasts, a prompt, the hotbar label) and nothing may overlap
    /// or run off its half of the screen, solo or split-screen, exploring or in a fight.
    /// </summary>
    public class HudLayoutTests
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

        /// <summary>Everything on at once.</summary>
        static void Crowd(PlayerSlot p)
        {
            p.Hud.CloseAll();
            p.Hud.Narrate("The sun slips behind the hills and Thistledown lights its lanterns. Night is when the woods get strange: carry a torch, and know where your bed is.");
            for (int i = 0; i < 5; i++) p.Hud.Toast($"+{(i + 1) * 15} XP (discovering somewhere with a rather long name)");
            p.Hud.SetPrompt("Talk to Brannoc Ironkettle, Shopkeeper");
            p.Character.Bag.Add("potion_healing", 1);
            p.Hud.HotbarIndex = 0;
        }

        static List<string> Problems(PlayerSlot p)
        {
            var problems = new List<string>();
            var items = p.Hud.LayoutBounds();
            var hud = p.Hud.HudRect;
            const float slack = 1f; // a pixel of touching is fine
            foreach (var (n, r) in items)
                if (r.xMin < hud.xMin - slack || r.xMax > hud.xMax + slack || r.yMin < hud.yMin - slack || r.yMax > hud.yMax + slack)
                    problems.Add($"{n} runs off the screen ({r} in {hud})");
            for (int a = 0; a < items.Count; a++)
                for (int b = a + 1; b < items.Count; b++)
                {
                    var ra = items[a].rect; var rb = items[b].rect;
                    float w = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin), h = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);
                    if (w > slack && h > slack) problems.Add($"{items[a].name} overlaps {items[b].name} ({ra} / {rb})");
                }
            problems.AddRange(p.Hud.OverflowingTexts());
            return problems;
        }

        static void Check(PlayerSlot p, string state)
        {
            Debug.Log($"HUD LAYOUT {state} p{p.Index + 1} in {p.Hud.HudRect}: " + string.Join(" | ", p.Hud.LayoutBounds().Select(x => $"{x.name} {x.rect}")));
            var problems = Problems(p);
            Assert.IsEmpty(problems, $"{state}, player {p.Index + 1}:\n" + string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator SoloExploringHasRoomForEverything()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(3);
            g.SpawnCompanion(p, DnD.Rules.CharacterFactory.Create(DnD.Rules.CharacterFactory.DefaultChoices("cleric", "dwarf", "Wick")), "hums");
            yield return Frames(2);
            Crowd(p);
            yield return Frames(3);
            AcademyTests.Shot("hud_solo_exploring");
            Check(p, "solo, exploring");
        }

        [UnityTest]
        public IEnumerator SplitScreenExploringHasRoomForEverything()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(3), "cleric", "dwarf");
            yield return Frames(3);
            Crowd(p1); Crowd(p2);
            yield return Frames(3);
            AcademyTests.Shot("hud_split_exploring");
            Check(p1, "split-screen, exploring");
            Check(p2, "split-screen, exploring");
        }

        [UnityTest]
        public IEnumerator FightsHaveRoomForEverything()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(4), "fighter", "human", level: 5);
            yield return Frames(2);
            g.Warp(p1, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p1.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p1);
            yield return Frames(3);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new DnD.Rules.Combatant(u.C);
            for (int i = 0; i < 4; i++) p1.Hud.Toast($"+{i} copper and a long line of loot text");
            p1.Hud.Narrate("Roll for initiative!");
            yield return Frames(3);
            p1.Hud.Dice.Ticker("The wolf circles, low and patient, looking for an opening.");
            yield return Frames(3);
            AcademyTests.Shot("hud_solo_fight");
            Check(p1, "solo, in a fight");

            var p2 = g.DebugStart(Still(5), "cleric", "dwarf");
            yield return Frames(3);
            g.Warp(p2, "forest", p1.MapPos + Vector2Int.left, false);
            yield return Frames(2);
            c.JoinLate(p2);
            for (int i = 0; i < 4; i++) p2.Hud.Toast($"+{i} copper and a long line of loot text");
            yield return Frames(3);
            p1.Hud.Dice.Ticker("The wolf circles, low and patient, looking for an opening.");
            p2.Hud.Dice.Ticker("The wolf circles, low and patient, looking for an opening.");
            yield return Frames(3);
            AcademyTests.Shot("hud_split_fight");
            Check(p1, "split-screen, in a fight");
            Check(p2, "split-screen, in a fight");
        }
    }
}
