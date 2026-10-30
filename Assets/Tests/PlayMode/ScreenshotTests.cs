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

        /// <summary>Phase 5: the crater, the kobold village, the light puzzle mid-solve and the festival square.</summary>
        [UnityTest]
        public IEnumerator CapturePhase5Places()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(5) { ConfirmChance = 0, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 }, "ranger", "elf");
            yield return Settle();
            g.Clock.Minutes = 11 * 60;
            g.Warp(p, "crater", new Vector2Int(18, 21), false);
            yield return Settle(40);
            p.Hud.CloseAll();
            yield return Settle(5);
            Capture("p5_01_crater");

            g.World.SetFlag("snik_friend");
            g.Warp(p, "warrens", new Vector2Int(20, 9), false);
            yield return Settle(40);
            p.Hud.CloseAll();
            yield return Settle(5);
            Capture("p5_02_kobold_village");

            foreach (var gate in new[] { "gate_1", "gate_2" }) g.World.SetFlag("puzzle:watchtower:" + gate);
            g.World.SetFlag("puzzlehint:watchtower:mirror_1");
            g.Warp(p, "watchtower", new Vector2Int(10, 3), false);
            yield return Settle(20);
            p.Hud.CloseAll();
            var mirror = Object.FindObjectsByType<PuzzlePieceObject>().First(x => x.Piece.Id == "mirror_1");
            mirror.Interact(p); // half solved: the beam now runs east along the wall toward the second mirror
            yield return Settle(20);
            Capture("p5_03_light_puzzle");

            g.Clock.Day = 10; g.Clock.Minutes = 14 * 60 - 1; g.Clock.Advance(1);
            g.Warp(p, "town", new Vector2Int(25, 18), false);
            foreach (var n in Object.FindObjectsByType<NpcAgent>()) n.UpdateSchedule(true);
            yield return Settle(40);
            p.Hud.CloseAll();
            yield return Settle(5);
            Capture("p5_04_festival_square");
        }

        /// <summary>Phase 6: the hatching mid-scene, and the dragon's pet menu with a heart bubble.</summary>
        [UnityTest]
        public IEnumerator CapturePhase6Dragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(6) { ConfirmChance = 0, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 }, "sorcerer", "human");
            yield return Settle();
            g.Warp(p, "egg_shrine", new Vector2Int(13, 4), false); // two steps below the egg, as when you walk up to it
            yield return Settle(20);
            g.PacifyGroup("mine_egg", p);
            p.Hud.CloseAll();
            g.HatchEgg(p, Vector3.zero);
            float t = Time.time;
            while (Time.time - t < 5.6f) yield return null; // just after the flash: the hatchling on its shell
            Capture("p6_01_hatching");
            for (int i = 0; i < 900 && !(p.Hud.Top is MessageScreen); i++) yield return null;
            p.Hud.Top?.Close();
            yield return Settle(3);
            (p.Hud.Top as NameEntryScreen)?.Submit();
            yield return Settle(40);
            p.Hud.CloseAll();
            var pet = g.DragonAgent.GetComponent<DragonPet>();
            pet.Emote("heart");
            pet.OpenMenu(p);
            yield return Settle(5);
            Capture("p6_02_dragon_menu");

            p.Hud.CloseAll();
            var buoy = Object.FindObjectsByType<TownDog>().First();
            g.Warp(p, "town", buoy.Actor.MapPos + new Vector2Int(1, 0), false);
            yield return Settle(20);
            p.Hud.CloseAll();
            buoy.PetHim(p);
            yield return Settle(10);
            Capture("p6_03_buoy");
        }

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

            g.Warp(p, "town", new Vector2Int(25, 19), false);
            yield return Settle();
            Capture("02_town_morning");

            g.Warp(p, "town", new Vector2Int(36, 20), false);
            yield return Settle();
            Capture("02b_town_bridge");

            g.Warp(p, "town", new Vector2Int(14, 25), false);
            yield return Settle();
            Capture("02c_town_farm");

            g.Warp(p, "forest", new Vector2Int(40, 18), false);
            yield return Settle();
            Capture("02d_forest_entrance");

            g.Warp(p, "forest", new Vector2Int(13, 19), false);
            yield return Settle();
            Capture("02e_forest_fairy_ring");

            g.Warp(p, "forest", new Vector2Int(29, 9), false);
            yield return Settle();
            Capture("02f_forest_shrine");

            // Phase 4: the South Lane, a market-day square and the forge.
            g.Warp(p, "town", new Vector2Int(20, 43), false);
            yield return Settle();
            Capture("20a_south_lane");
            g.Warp(p, "town", new Vector2Int(38, 43), false);
            yield return Settle();
            Capture("20a2_south_lane_feud");
            g.Clock.Day = 6; g.Clock.Minutes = 11 * 60; // Coinday: market day
            g.Warp(p, "town", new Vector2Int(25, 19), false);
            foreach (var n in Object.FindObjectsByType<NpcAgent>()) n.UpdateSchedule(true);
            yield return Settle();
            Capture("20b_square_market");
            g.Clock.Day = 1; g.Clock.Minutes = 13 * 60;
            g.Warp(p, "forge", new Vector2Int(6, 5), false);
            foreach (var n in Object.FindObjectsByType<NpcAgent>()) n.UpdateSchedule(true);
            yield return Settle();
            Capture("20c_forge");
            g.Clock.Minutes = 10 * 60;
            foreach (var q in new[] { "bad_blood", "pond_ring", "goats", "elowens_herbs", "wolf_bounty", "pips_barrel" }) g.Quests.Start(q);
            g.Quests.Stages["kettle"] = 2; g.Quests.Paid.Add("kettle");
            p.Hud.Push(new JournalScreen());
            yield return Settle();
            Capture("20d_journal");
            p.Hud.CloseAll();

            g.Warp(p, "town", new Vector2Int(25, 19), false);
            yield return Settle();

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

            // A companion: party HP frame under the status panel, and the item options menu with "Give to...".
            g.HireRecruit(p, Object.FindObjectsByType<RecruitAgent>()[0]);
            foreach (var comp in g.CompanionsOf(p)) if (comp.Character != null) comp.Character.HP = Mathf.Max(1, comp.Character.EffectiveMaxHP / 2);
            p.Character.AddItem("potion_healing", 2);
            var inv = new InventoryScreen(null, null);
            p.Hud.Push(inv);
            yield return Settle(5);
            inv.OpenOptionsFor(System.Array.FindIndex(p.Character.Bag.Slots, s => s != null && s.ItemId == "potion_healing"));
            yield return Settle(5);
            Capture("08b_item_options_give_potion");
            p.Hud.CloseAll();

            p.Hud.Push(new DialogueScreen("companion", null, System.Linq.Enumerable.First(g.CompanionsOf(p))));
            yield return Settle();
            Capture("08c_companion_orders");
            p.Hud.CloseAll();

            // Being spotted: the DM asks what you do.
            g.Warp(p, "forest", new Vector2Int(14, 26), false);
            yield return Settle(5);
            p.Hud.CloseAll();
            g.OnEnemyNoticed(System.Array.Find(Object.FindObjectsByType<EnemyAgent>(), e => e.Group.Id == "forest_goblins"), p);
            yield return Settle();
            Capture("08d_encounter_choice");
            p.Hud.CloseAll();
            g.CalmGroup("forest_goblins");

            // Miller's Ford
            g.Warp(p, "ford", new Vector2Int(6, 14), false);
            yield return Settle();
            Capture("08e_ford_road");
            g.Warp(p, "ford", new Vector2Int(24, 12), false);
            yield return Settle();
            Capture("08f_ford_bridge");
            g.Warp(p, "ford", new Vector2Int(36, 22), false);
            foreach (var n in Object.FindObjectsByType<NpcAgent>()) n.UpdateSchedule(true);
            yield return Settle();
            Capture("08g_ford_mill");
            g.Warp(p, "ford", new Vector2Int(20, 9), false);
            yield return Settle(60);
            Capture("08h_ford_wardens");
            p.Hud.CloseAll();
            foreach (var comp in System.Linq.Enumerable.ToList(g.CompanionsOf(p))) if (!comp.IsDragon) g.DismissCompanion(comp);

            g.Warp(p, "mine", new Vector2Int(19, 26), false);
            yield return Settle();
            Capture("09_mine_dark_darkvision");

            g.Warp(p, "mine", new Vector2Int(19, 20), false);
            g.StartGroupCombat("mine_hub", p);
            yield return Settle(90);
            Capture("10_combat");

            // Clicking an enemy's body (which draws up into the tile above its feet) must pick that enemy.
            var combat = g.Combats[0];
            foreach (var u in combat.Units)
            {
                if (u.IsHuman) continue;
                var body = u.A.Sr.bounds.center + new Vector3(0, u.A.Sr.bounds.extents.y * 0.5f, 0);
                Assert.AreNotEqual(u.Pos, u.A.Zone.ToMap(body), "test assumes the sprite overlaps the tile above");
                Assert.AreSame(u, combat.UnitUnderPoint(body), $"clicking {u.Name}'s body should target it");
            }

            // Roll through initiative (and any enemy turns) until it's the wizard's turn, then show the action bar.
            float until = Time.realtimeSinceStartup + 60;
            bool MyTurn() => combat.Current != null && combat.Current.IsHuman && !p.Hud.Dice.IsOpen && p.Hud.Top is CombatHud;
            while (!MyTurn() && !combat.Finished && Time.realtimeSinceStartup < until)
            {
                input.ConfirmChance = p.Hud.Dice.IsOpen || !(p.Hud.Top is CombatHud) ? 1 : 0; // roll dice, answer reaction prompts
                yield return null;
            }
            input.ConfirmChance = 0;
            yield return Settle(20);
            var chud = p.Hud.Find<CombatHud>();
            if (chud == null || combat.Finished || !MyTurn()) Debug.LogWarning("Fight ended before the wizard's turn (random dice); skipped the action bar shots.");
            else
            {
                Capture("10b_combat_turn");
                chud.OpenGroup(ActionGroup.Spells);
                yield return Settle(5);
                Capture("10c_combat_spells");
            }

            g.QuitToTitle();
            yield return Settle(3);
            var p1 = g.DebugStart(new FuzzInput(2) { ConfirmChance = 0, MenuChance = 0 }, "fighter", "dwarf");
            var p2 = g.DebugStart(new FuzzInput(3) { ConfirmChance = 0, MenuChance = 0 }, "bard", "gnome");
            g.Warp(p1, "town", new Vector2Int(12, 19), false);
            g.Warp(p2, "tavern", new Vector2Int(8, 8), false);
            yield return Settle();
            Capture("11_split_screen");
            Assert.Pass("Screenshots written to " + Dir);
        }
    }
}
