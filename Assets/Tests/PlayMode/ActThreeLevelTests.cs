using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>Act III Phase 1: levels 11-13 in the running game.</summary>
    public class ActThreeLevelTests
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
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>A debug start at `level`, warped into the forest wolves' fight.</summary>
        static IEnumerator WolfFight(string cls, int level, int seed, System.Action<PlayerSlot, CombatInstance, Unit> got)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(seed), cls, "human", level: level);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            got(p, c, u);
        }

        static void Run(CombatAction a, Unit u, Vector2Int tile)
        {
            var run = a.Run(u, tile);
            while (run != null && run.MoveNext()) { }
        }

        [UnityTest]
        public IEnumerator ThreeAttacksSurviveRetargetAndActionSurge()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("fighter", 11, 1, (a, b, d) => { p = a; c = b; u = d; });
            var pc = (PlayerCharacter)u.C;
            Assert.AreEqual(11, pc.Level, "debug start previews Act III levels");
            Assert.AreEqual(PlayerCharacter.ActThreeLevelCap, PlayerCharacter.LevelCap);
            var first = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            StringAssert.Contains("3 attacks", first.Tooltip, "Two Extra Attacks: three per Attack action");
            // Right after attack 1 of the action killed wolf A:
            var wolves = c.Units.Where(x => x.C.Team == Team.Enemies).ToList();
            wolves[0].C.HP = 0;
            u.Cb.Turn.ActionUsed = true;
            u.Cb.Turn.AttacksLeftInAction = pc.AttacksPerAction - 1;
            var second = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("2/3"));
            Assert.IsNotNull(second, "attack 2/3 is on the bar after the kill");
            Assert.AreEqual(Targeting.Enemy, second.Target, "it picks a new target");
            u.Cb.Turn.AttacksLeftInAction--; // swing 2 spent
            Assert.IsNotNull(c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Attack (") && a.Name.EndsWith("3/3")), "then 3/3 (the bar is rebuilt each time a menu opens, and the count holds)");
            u.Cb.Turn.AttacksLeftInAction = 0;
            var surge = c.ActionsFor(p).First(a => a.Name == "Action Surge");
            Assert.IsTrue(surge.Enabled, "Action Surge once the action is spent");
            Run(surge, u, u.Pos);
            var again = c.ActionsFor(p).First(a => a.Name.StartsWith("Attack ("));
            Assert.AreEqual(ActionCost.Action, again.Cost, "Action Surge gives a whole new Attack action...");
            StringAssert.Contains("3 attacks", again.Tooltip, "...with three more attacks");
        }

        [UnityTest]
        public IEnumerator ImprovedCunningStrikeOffersTwoToggles()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("rogue", 11, 2, (a, b, d) => { p = a; c = b; u = d; });
            var trip = c.ActionsFor(p).First(a => a.Name == "Cunning Strike: Trip");
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.StartsWith("Second Cunning Strike")), "the second slot opens after the first pick");
            Run(trip, u, u.Pos);
            var seconds = c.ActionsFor(p).Where(a => a.Name.StartsWith("Second Cunning Strike")).Select(a => a.Name).ToList();
            CollectionAssert.AreEquivalent(new[] { "Second Cunning Strike: Poison", "Second Cunning Strike: Withdraw" }, seconds, "two effects, not the same one twice");
            StringAssert.Contains("one more Sneak Attack die", c.ActionsFor(p).First(a => a.Name == "Second Cunning Strike: Poison").Tooltip, "the die cost is shown");
        }

        [UnityTest]
        public IEnumerator FleetStepFollowsAnotherBonusAction()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return WolfFight("monk", 11, 3, (a, b, d) => { p = a; c = b; u = d; });
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.Contains("Fleet Step")), "no Fleet Step before a Bonus Action");
            Run(c.ActionsFor(p).First(a => a.Name.StartsWith("Patient Defense")), u, u.Pos);
            var fleet = c.ActionsFor(p).FirstOrDefault(a => a.Name.Contains("Fleet Step"));
            Assert.IsNotNull(fleet, "Patient Defense, then Fleet Step");
            int move = u.Cb.Turn.MovementLeftFt;
            Run(fleet, u, u.Pos);
            Assert.Greater(u.Cb.Turn.MovementLeftFt, move, "it Dashes");
            Assert.IsFalse(c.ActionsFor(p).Any(a => a.Name.Contains("Fleet Step")), "once per turn");
        }

        /// <summary>A level-13 companion of `cls` with `spell` prepared, beside a level-11 fighter, in the forest wolves' fight.</summary>
        static IEnumerator CasterFight(string cls, string spell, int seed, System.Action<CombatInstance, Unit, Unit, Unit> got)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(seed), "fighter", "human", level: 11);
            var caster = CharacterFactory.Create(CharacterFactory.DefaultChoices(cls, "elf", "Sable"));
            caster.XP = 999999;
            while (caster.Level < 13) caster.LevelUp(caster.AutoPlan(), null);
            if (!caster.PreparedSpells.Contains(spell)) caster.PreparedSpells.Add(spell);
            var comp = g.SpawnCompanion(p, caster, "hums");
            comp.Manual = false;
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.C == caster);
            var hero = c.Units.First(u => u.Owner == p);
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies && u.Active);
            if (me.Cb == null) me.Cb = new Combatant(me.C);
            hero.C.MaxHP = hero.C.HP = 300; wolf.C.MaxHP = wolf.C.HP = 300;
            var near = wolf.Pos + Vector2Int.left;
            if (!hero.A.Zone.IsWalkable(near) || c.Occupied(near)) near = wolf.Pos + Vector2Int.right;
            hero.A.PlaceAt(hero.A.Zone, near);
            got(c, me, hero, wolf);
        }

        [UnityTest]
        public IEnumerator FireStormSparesAlliesInTheArea()
        {
            CombatInstance c = null; Unit me = null, hero = null, wolf = null;
            yield return CasterFight("druid", "fire_storm", 4, (a, b, d, e) => { c = a; me = b; hero = d; wolf = e; });
            var run = c.DebugCast(me, "fire_storm", wolf.Pos);
            while (run.MoveNext()) { }
            Assert.Less(wolf.C.HP, 300, "the wolf burns");
            Assert.AreEqual(300, hero.C.HP, "the fighter beside it is untouched");
        }

        [UnityTest]
        public IEnumerator ChainLightningLeapsToOtherWolves()
        {
            CombatInstance c = null; Unit me = null, hero = null, wolf = null;
            yield return CasterFight("sorcerer", "chain_lightning", 5, (a, b, d, e) => { c = a; me = b; hero = d; wolf = e; });
            var others = c.Units.Where(u => u.C.Team == Team.Enemies && u.Active && u != wolf).ToList();
            foreach (var o in others) o.C.MaxHP = o.C.HP = 300;
            var run = c.DebugCast(me, "chain_lightning", wolf.Pos);
            while (run.MoveNext()) { }
            Assert.Less(wolf.C.HP, 300, "the first wolf is struck");
            int near = others.Count(o => Vector2Int.Distance(o.Pos, wolf.Pos) <= 6);
            if (near > 0) Assert.IsTrue(others.Any(o => o.C.HP < 300), "the bolt leaps to another wolf");
            Assert.AreEqual(300, hero.C.HP, "it never leaps to an ally");
        }

        [UnityTest]
        public IEnumerator MysticArcanumSavesAndCastsOncePerLongRest()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "warlock", "human", level: 13);
            yield return Frames(2);
            var pc = p.Character;
            Assert.AreEqual(2, pc.MysticArcanum.Count, "a level-13 warlock knows two arcana");
            var arcana = pc.MysticArcanum.ToList();
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            pc = g.Players[0].Character;
            CollectionAssert.AreEqual(arcana, pc.MysticArcanum, "the arcana are saved");
            p = g.Players[0];
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            if (u.Cb == null) u.Cb = new Combatant(u.C);
            string six = pc.ArcanumAt(6);
            var btn = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith(SpellsDb.Get(six).Name) && a.Name.Contains("Arcanum"));
            Assert.IsNotNull(btn, "the 6th-level arcanum is on the Spells menu");
            Assert.IsTrue(btn.Enabled, "and castable even though the Pact slots are 5th-level");
            pc.CastArcanum(six);
            btn = c.ActionsFor(p).First(a => a.Name.StartsWith(SpellsDb.Get(six).Name) && a.Name.Contains("Arcanum"));
            Assert.IsFalse(btn.Enabled, "once used, it waits for a Long Rest");
            StringAssert.Contains("Long Rest", btn.Why);
        }

        [UnityTest]
        public IEnumerator LevelUpScreenOffersMysticArcanum()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(7), "warlock", "human", level: 10);
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            p.Character.XP = PlayerCharacter.XpThresholds[11];
            yield return Frames(2);
            var screen = new LevelUpScreen();
            p.Hud.Push(screen);
            yield return Frames(2);
            screen.PickHpAverage();
            int guard = 0;
            bool sawArcanum = false;
            while (screen.CurrentChoice != null && guard++ < 10)
            {
                var c = screen.CurrentChoice;
                if (c.Kind == ChoiceKind.Arcanum)
                {
                    sawArcanum = true;
                    StringAssert.Contains("Mystic Arcanum", c.Title);
                    Assert.IsTrue(screen.RowTexts.Any(t => t.Contains(SpellsDb.Get(c.Options[0]).Name)), "the 6th-level options are listed by name");
                }
                foreach (var id in c.Options.Take(c.Count).ToList()) screen.Toggle(id);
                screen.Next();
            }
            Assert.IsTrue(sawArcanum, "warlock 11 picks a Mystic Arcanum");
            screen.Confirm();
            yield return Frames(2);
            Assert.AreEqual(11, p.Character.Level);
            Assert.AreEqual(1, p.Character.MysticArcanum.Count);
        }

        [UnityTest]
        public IEnumerator CharacterSheetFitsAtLevelThirteenInSplitScreen()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(11), "warlock", "human", level: 13);
            var p2 = g.DebugStart(Still(12), "wizard", "elf", level: 13);
            yield return Frames(10);
            foreach (var p in new[] { p1, p2 }) p.Hud.Push(new CharacterSheetScreen());
            yield return Frames(10);
            var cols = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Where(t => t.transform.parent != null && t.transform.parent.name.StartsWith("Sheet")).ToList();
            foreach (var label in cols)
            {
                var rt = (RectTransform)label.transform;
                Assert.LessOrEqual(label.preferredHeight, rt.rect.height + 2f, $"the sheet's {label.transform.parent.name} column fits ({label.preferredHeight:0} > {rt.rect.height:0}): {label.text.Substring(0, System.Math.Min(40, label.text.Length))}");
            }
            Assert.Greater(cols.Count, 3, "found the sheet columns");
            Shot("act3_sheet_l13");
        }

        [UnityTest]
        public IEnumerator CompanionAiNeverTargetsAlliesWithNewAreas()
        {
            for (int seed = 0; seed < 5; seed++)
            {
                CombatInstance c = null; Unit me = null, hero = null, wolf = null;
                yield return CasterFight("sorcerer", "fire_storm", 20 + seed, (a, b, d, e) => { c = a; me = b; hero = d; wolf = e; });
                var pc = (PlayerCharacter)me.C;
                if (!pc.PreparedSpells.Contains("chain_lightning")) pc.PreparedSpells.Add("chain_lightning");
                if (!pc.PreparedSpells.Contains("delayed_blast_fireball")) pc.PreparedSpells.Add("delayed_blast_fireball");
                for (int turn = 0; turn < 3; turn++)
                {
                    me.Cb.Turn.Reset(me.C);
                    var run = c.DebugAiTurn(me);
                    int guard = 0;
                    while (run.MoveNext() && guard++ < 100000) { }
                    Assert.AreEqual(300, hero.C.HP, $"seed {seed}, turn {turn}: the companion never hits the fighter with a spell");
                }
                GameRoot.I.QuitToTitle();
                yield return null;
            }
        }

        static void Shot(string name)
        {
            string dir = Path.Combine(Path.GetTempPath(), "embers_screenshots");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24);
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var cam in cams) { if (!cam.isActiveAndEnabled) continue; var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator CrownSoldiersParryAndFireballToTheEnd()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(31) { ConfirmChance = 0.35f, EndTurnChance = 0.01f }, "fighter", "human", level: 11);
            yield return Frames(2);
            g.Warp(p, "forest", new Vector2Int(20, 20), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat(g.DebugSpawnGroup(p, "crown_knight", "crown_battlemage", "iron_sentinel"), p);
            var c = g.Combats.Single();
            Assert.IsTrue(c.Units.Any(u => u.C is Monster m && m.Def.Id == "crown_battlemage"), "the battlemage joins the fight");
            Time.timeScale = 6;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 150f) yield return null;
            Time.timeScale = 1;
            Assert.IsFalse(g.AnyInCombat, "the fight finishes (won or lost)");
        }
    }
}
