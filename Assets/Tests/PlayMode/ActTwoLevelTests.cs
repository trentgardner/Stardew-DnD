using System.Collections;
using System.Linq;
using System.IO;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act II Phase 1: levels 5-10 in the running game.</summary>
    public class ActTwoLevelTests
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
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };

        [UnityTest]
        public IEnumerator SecondFightingStyleSurvivesASaveAndTheCapResetsOnLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human", level: 7);
            yield return null;
            var pc = p.Character;
            Assert.AreEqual(7, pc.Level, "debug start can preview Act II levels");
            Assert.IsNotNull(pc.FightingStyle2, "champion 7 picked an additional fighting style");
            Assert.AreNotEqual(pc.FightingStyle, pc.FightingStyle2);
            string style2 = pc.FightingStyle2;
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return null;
            Assert.AreEqual(style2, g.Players[0].Character.FightingStyle2, "the second style is saved");
            Assert.AreEqual(PlayerCharacter.ActOneLevelCap, PlayerCharacter.LevelCap, "loading (an Act I save) puts the cap back to 4");
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator WallOfFireBurnsAtTurnEnd()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "wizard", "human", level: 9);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies && u.Active);
            var pc = (PlayerCharacter)p.Character;
            pc.ConcentratingOn = "wall_of_fire";
            var wall = ZoneRules.Create(pc, SpellsDb.Get("wall_of_fire"), 4, wolf.Pos.x, wolf.Pos.y, new System.Collections.Generic.List<(int x, int y)> { (wolf.Pos.x, wolf.Pos.y) });
            c.AddZone(wall);
            Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(r => r.gameObject.name == "zone_wall_of_fire"), "the wall is drawn on the map");
            wolf.C.MaxHP = wolf.C.HP = 500;
            c.TriggerZones(wolf);
            Assert.Less(wolf.C.HP, 500, "a wolf standing in the wall burns");
            int after = wolf.C.HP;
            c.TriggerZones(wolf);
            Assert.AreEqual(after, wolf.C.HP, "only once per turn");
            pc.ConcentratingOn = null;
            c.DrawZones();
            Assert.AreEqual(0, c.Zones.Count, "losing concentration puts the wall out");
        }

        [UnityTest]
        public IEnumerator TrollMultiattackAndHoundBreathFightToTheEnd()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(7) { ConfirmChance = 0.35f, EndTurnChance = 0.01f }, "fighter", "human", level: 7);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            var id = g.DebugSpawnGroup(p, "troll", "cinder_hound");
            g.StartGroupCombat(id, p);
            Assert.AreEqual(1, g.Combats.Count);
            var c = g.Combats.Single();
            Assert.IsTrue(c.Units.Any(u => u.C is Monster m && m.Def.Id == "troll"), "the troll joins the fight");
            Time.timeScale = 6;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 150f) yield return null;
            Time.timeScale = 1;
            Assert.IsFalse(g.AnyInCombat, "the fight finishes (won or lost)");
        }

        [UnityTest]
        public IEnumerator ExtraAttackSurvivesRetargetAfterKill()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human", level: 5);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            var first = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            Assert.AreEqual(ActionCost.Action, first.Cost);
            StringAssert.Contains("2 attacks", first.Tooltip, "the button says Extra Attack gives two");
            // The state right after the first swing of the Attack action killed wolf A:
            var wolves = c.Units.Where(x => x.C.Team == Team.Enemies).ToList();
            wolves[0].C.HP = 0;
            u.Cb.Turn.ActionUsed = true;
            u.Cb.Turn.AttacksLeftInAction = ((PlayerCharacter)u.C).AttacksPerAction - 1;
            var second = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("2/2"));
            Assert.IsNotNull(second, "the second attack is still on the bar after the action is spent");
            Assert.IsTrue(second.Enabled, "and usable");
            Assert.AreEqual(ActionCost.Free, second.Cost, "it doesn't need another action");
            Assert.AreEqual(Targeting.Enemy, second.Target, "it picks its own target (a different wolf)");
            Assert.IsTrue(c.ActionsFor(p).First(a => a.Name.StartsWith("Attack (")).Name.EndsWith("2/2"), "the plain Attack button becomes 'Attack 2/2' (clicking an enemy uses it)");
        }

        [UnityTest]
        public IEnumerator PaladinAuraUsesPositionsInAFight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "paladin", "human", level: 6);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var pal = (PlayerCharacter)p.Character;
            int bonus = System.Math.Max(1, pal.Mod(Ability.CHA));
            Assert.AreEqual(bonus, Auras.ProtectionBonus(pal), "the paladin's own saves get the aura");
            var wolf = g.Combats.Single().Units.First(u => u.C.Team == Team.Enemies);
            Assert.AreEqual(0, Auras.ProtectionBonus(wolf.C), "enemies never do, however close");
        }

        [UnityTest]
        public IEnumerator DruidShapesIntoAFlyingFormAtEight()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(8), "druid", "human", level: 8);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            var owl = c.ActionsFor(p).FirstOrDefault(a => a.Name.Contains("Giant Owl"));
            Assert.IsNotNull(owl, "Giant Owl is one of the druid's forms at 8");
            var run = owl.Run(u, u.Pos);
            while (run.MoveNext()) { }
            var pc = (PlayerCharacter)p.Character;
            Assert.AreEqual("giant_owl", pc.WildShape?.Id);
            Assert.AreEqual("mon_owlbear_cub", u.A.Look, "the sprite is the owl's");
            Assert.IsTrue(CombatInstance.Flies(u), "and it flies");
            // Somewhere on the map there's water: flying crosses it, walking doesn't.
            var zone = u.A.Zone;
            Vector2Int water = default; bool found = false;
            for (int x = 0; x < zone.W && !found; x++) for (int y = 0; y < zone.H && !found; y++) { var t = new Vector2Int(x, y); if (zone.CharAt(t) == '~') { water = t; found = true; } }
            Assume.That(found, "this zone has water");
            Assert.IsTrue(zone.IsPassable(water, true), "an owl can be over the water");
            Assert.IsFalse(zone.IsPassable(water, false), "a wolf can't");
        }

        [UnityTest]
        public IEnumerator FireballSparesSculptedAllies()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(9), "fighter", "human", level: 5);
            var wiz = CharacterFactory.Create(CharacterFactory.DefaultChoices("wizard", "elf", "Sable"));
            wiz.XP = 999999;
            while (wiz.Level < 6) wiz.LevelUp(wiz.AutoPlan(), null);
            if (!wiz.PreparedSpells.Contains("fireball")) wiz.PreparedSpells.Add("fireball");
            var comp = g.SpawnCompanion(p, wiz, "hums");
            comp.Manual = false;
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.C == wiz);
            var hero = c.Units.First(u => u.Owner == p);
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies && u.Active);
            if (me.Cb == null) me.Cb = new Combatant(me.C);
            hero.C.MaxHP = hero.C.HP = 300; wolf.C.MaxHP = wolf.C.HP = 300;
            // The fighter stands right next to the wolf; the wizard is well back.
            var near = wolf.Pos + Vector2Int.left;
            if (!hero.A.Zone.IsWalkable(near) || c.Occupied(near)) near = wolf.Pos + Vector2Int.right;
            hero.A.PlaceAt(hero.A.Zone, near);
            var run = c.DebugCast(me, "fireball", wolf.Pos);
            while (run.MoveNext()) { }
            Assert.Less(wolf.C.HP, 300, "the wolf is caught in the fireball");
            Assert.AreEqual(300, hero.C.HP, "the fighter next to it is sculpted out");
        }

        [UnityTest]
        public IEnumerator LevelUpScreenOffersMagicalSecretsAndScrolls()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(10), "bard", "human", level: 9);
            p.Character.XP = PlayerCharacter.XpThresholds[10];
            yield return Frames(2);
            var screen = new LevelUpScreen();
            p.Hud.Push(screen);
            yield return Frames(2);
            screen.PickHpAverage();
            int guard = 0;
            bool sawSecrets = false;
            while (screen.CurrentChoice != null && guard++ < 10)
            {
                var c = screen.CurrentChoice;
                if (c.Kind == ChoiceKind.Spell)
                {
                    sawSecrets = true;
                    Assert.Greater(screen.RowCount, screen.VisibleRows, "a long list: it scrolls");
                    Assert.IsTrue(screen.RowTexts.First().StartsWith("Next"), "Next is on top of a long list");
                    Assert.IsTrue(screen.RowTexts.Any(t => t.StartsWith("— Level")), "spells are grouped by level");
                    var other = c.Options.First(id => !SpellsDb.Get(id).Classes.Contains("bard"));
                    screen.Toggle(other);
                    foreach (var id in c.Options.Where(o => o != other).Take(c.Count - 1)) screen.Toggle(id);
                }
                else foreach (var id in c.Options.Take(c.Count).ToList()) screen.Toggle(id);
                screen.Next();
            }
            Assert.IsTrue(sawSecrets, "bard 10 picks Magical Secrets spells");
            screen.Confirm();
            yield return Frames(2);
            Assert.AreEqual(10, p.Character.Level);
            Assert.IsTrue(p.Character.PreparedSpells.Any(id => !SpellsDb.Get(id).Classes.Contains("bard")), "a spell from another list is now prepared");
        }

        [UnityTest]
        public IEnumerator CharacterSheetFitsAtLevelTenInSplitScreen()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(11), "paladin", "human", level: 10);
            var p2 = g.DebugStart(Still(12), "wizard", "elf", level: 10);
            yield return Frames(10);
            foreach (var p in new[] { p1, p2 })
            {
                var sheet = new CharacterSheetScreen();
                p.Hud.Push(sheet);
            }
            yield return Frames(10);
            foreach (var label in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Where(t => t.transform.parent != null && t.transform.parent.name.StartsWith("Sheet")))
            {
                var rt = (RectTransform)label.transform;
                Assert.LessOrEqual(label.preferredHeight, rt.rect.height + 2f, $"the sheet's {label.transform.parent.name} column fits ({label.preferredHeight:0} > {rt.rect.height:0}): {label.text.Substring(0, System.Math.Min(40, label.text.Length))}");
            }
            Assert.Greater(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Count(t => t.transform.parent != null && t.transform.parent.name.StartsWith("Sheet")), 3, "found the sheet columns");
            Shot("act2_sheet_l10");
        }

        [UnityTest]
        public IEnumerator CompanionsLevelToTenAutomatically()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(13), "fighter", "human", level: 5);
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "dwarf", "Bram"));
            var comp = g.SpawnCompanion(p, pc, "hums");
            yield return Frames(2);
            g.GiveXP(p, 70000, "test");
            Assert.AreEqual(10, pc.Level, "the companion follows to the cap");
            Assert.IsTrue(pc.PreparedSpells.Contains("mass_cure_wounds"), "with its domain spells");
            Assert.Greater(pc.SlotsMax(5), 0, "and 5th-level slots");
        }

        static void Shot(string name)
        {
            string dir = Path.Combine(Path.GetTempPath(), "embers_screenshots");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24);
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var c in cams) { if (!c.isActiveAndEnabled) continue; var old = c.targetTexture; c.targetTexture = rt; c.Render(); c.targetTexture = old; }
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt);
        }
    }
}
