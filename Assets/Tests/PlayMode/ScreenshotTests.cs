using System.Collections;
using System.IO;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>
    /// Renders key scenes to PNGs in Temp/Screenshots so the look can be reviewed without playing.
    /// Run with: -runTests -testPlatform PlayMode -testFilter ScreenshotTests
    /// </summary>
    public class ScreenshotTests
    {
        static string Dir => Path.Combine(Path.GetTempPath(), "embers_screenshots");

        static void Capture(string name, int w = 1280, int h = 720)
        {
            Directory.CreateDirectory(Dir);
            var rt = new RenderTexture(w, h, 24);
            var cams = Object.FindObjectsByType<Camera>();
            System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var c in cams)
            {
                if (!c.isActiveAndEnabled) continue;
                var old = c.targetTexture;
                c.targetTexture = rt;
                c.Render();
                c.targetTexture = old;
            }
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SaveSystem.OverrideRoot = Path.Combine(Path.GetTempPath(), "embers_shot_saves");
            AudioListener.volume = 0; Sfx.Volume = 0;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
        }

        static IEnumerator Settle(int n = 30) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator CaptureKeyScenes()
        {
            yield return Settle(5);
            Capture("00_title");

            var g = GameRoot.I;
            var input = new FuzzInput(1) { ConfirmChance = 0, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 };
            var p = g.DebugStart(input, "wizard", "elf");
            yield return Settle();
            Capture("01_home");

            g.Warp(p, "town", new Vector2Int(19, 14), false);
            yield return Settle();
            Capture("02_town_morning");

            g.Clock.Minutes = 21 * 60;
            yield return Settle();
            Capture("03_town_night");
            g.Clock.Minutes = 10 * 60;

            g.Warp(p, "shop", new Vector2Int(6, 5), false);
            foreach (var n in Object.FindObjectsByType<NpcAgent>()) n.UpdateSchedule(true);
            yield return Settle();
            Capture("04_shop");

            p.Hud.Push(new DialogueScreen("brannoc", System.Array.Find(Object.FindObjectsByType<NpcAgent>(), n => n.Def.Id == "brannoc"), null));
            yield return Settle();
            Capture("05_dialogue");
            p.Hud.CloseAll();

            p.Hud.Dice.RollD20("Persuasion Check", "Persuasion (CHA) +2 · DC hidden", () => Checks.RollSkill(p.Character, Skill.Persuasion, AdvantageState.Normal), 14, true, r => { });
            yield return Settle(10);
            Capture("06_dice_prompt");
            p.Hud.CloseAll();

            p.Hud.Push(new InventoryScreen(null, null));
            yield return Settle();
            Capture("07_inventory");
            p.Hud.CloseAll();

            p.Hud.Push(new CharacterSheetScreen());
            yield return Settle();
            Capture("08_character_sheet");
            p.Hud.CloseAll();

            g.Warp(p, "mine", new Vector2Int(19, 26), false);
            yield return Settle();
            Capture("09_mine_dark_darkvision");

            g.Warp(p, "mine", new Vector2Int(19, 20), false);
            g.StartGroupCombat("mine_hub", p);
            yield return Settle(90);
            Capture("10_combat");

            g.QuitToTitle();
            yield return Settle(3);
            var p1 = g.DebugStart(new FuzzInput(2) { ConfirmChance = 0, MenuChance = 0 }, "fighter", "dwarf");
            var p2 = g.DebugStart(new FuzzInput(3) { ConfirmChance = 0, MenuChance = 0 }, "bard", "gnome");
            g.Warp(p1, "town", new Vector2Int(10, 15), false);
            g.Warp(p2, "tavern", new Vector2Int(8, 8), false);
            yield return Settle();
            Capture("11_split_screen");
            Assert.Pass("Screenshots written to " + Dir);
        }
    }
}
