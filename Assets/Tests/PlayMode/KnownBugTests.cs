using System.Collections;
using System.Collections.Generic;
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
    /// <summary>The known-bug sweep (2026-11-03): one test per bug, each written to fail before its fix.</summary>
    public class KnownBugTests
    {
        string _tempSaves;
        const string Arena = "lantern_road";
        static readonly Vector2Int BossAt = new Vector2Int(10, 17), HeroAt = new Vector2Int(10, 18);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            MonstersDb.All["test_breather"] = new MonsterDef
            {
                Id = "test_breather", Name = "Test Breather", SpriteId = "mon_ogre", AC = 10, HitDice = DiceExpr.Parse("10d10+100"), CR = 10, XP = 100,
                Attacks = { new MonsterAttackDef { Name = "Rend", ToHit = 10, Damage = DiceExpr.Parse("2d6+6"), DamageType = DamageType.Slashing, ReachFt = 10 } },
                Abilities = { new MonsterAbilityDef { Name = "Grey Breath", Telegraph = true, Shape = TargetShape.Cone, AreaFt = 30, Dice = DiceExpr.Parse("10d6"), DamageType = DamageType.Fire, SaveDC = 17 } },
                WeakSpotFlag = "test_weakness",
            };
            var grp = new EnemyGroupDef { Id = "test_breather_grp", Style = EncounterStyle.None, Unique = true };
            grp.Members.Add(new EnemySpawn("test_breather", BossAt.x, BossAt.y));
            WorldDb.Zones[Arena].EnemyGroups.Add(grp);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == "test_breather_grp");
            MonstersDb.All.Remove("test_breather");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
            Dice.Rng = new SystemRandom();
            PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap;
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static CombatInstance BreatherFight(GameRoot g, PlayerSlot p, out Unit boss, out Unit hero)
        {
            g.Warp(p, Arena, HeroAt, false);
            p.Hud.CloseAll();
            g.StartGroupCombat("test_breather_grp", p);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            boss = c.Units.Single(u => u.C is Monster m && m.Def.Id == "test_breather");
            hero = c.Units.Single(u => u.Owner == p);
            hero.A.PlaceAt(c.Zone, HeroAt);
            boss.A.PlaceAt(c.Zone, BossAt);
            return c;
        }

        static IEnumerator Fight(string cls, int level, string group, string zone, Vector2Int at, System.Action<PlayerSlot, CombatInstance, Unit> got)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(level * 7), cls, "human", level: level);
            yield return Frames(2);
            g.Warp(p, zone, at, false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat(group, p);
            var c = g.Combats.Single();
            var u = c.Units.First(x => x.Owner == p);
            foreach (var x in c.Units) if (x.Cb == null) x.Cb = new Combatant(x.C);
            got(p, c, u);
        }

        static void Run(CombatAction a, Unit u, Vector2Int tile)
        {
            var run = a.Run(u, tile);
            while (run != null && run.MoveNext()) { }
        }

        // ------------------------------------------------------------------ 1, 3, 4: a boss's breath

        [UnityTest]
        public IEnumerator IncapacitatedBossLosesItsBreath()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(3);
            var c = BreatherFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            yield return c.DebugAiTurn(boss);
            Assert.IsNotNull(m.Charging, "it draws breath");
            m.AddEffect(EffectLibrary.Create("hold_person", hero.C, 30));
            int hp = hero.C.HP;
            yield return c.ReleaseBreath(boss, m);
            Assert.AreEqual(hp, hero.C.HP, "a paralyzed boss can't let the breath go (SRD: incapacitated, no actions)");
            Assert.IsNull(m.Charging, "the breath is lost");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("loses the breath")));
        }

        [UnityTest]
        public IEnumerator RetreatLetsTheBreathGo()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "dwarf");
            yield return Frames(3);
            var c = BreatherFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            yield return c.DebugAiTurn(boss);
            Assert.IsNotNull(m.Charging);
            c.Retreat();
            yield return Frames(2);
            Assert.IsNull(m.Charging, "fleeing makes it waste the breath; it has to draw again next fight");
            Assert.AreEqual(Color.white, boss.A.Sr.color, "and the red glow goes");
        }

        // ------------------------------------------------------------------ 2: the Warden's ward for a late joiner

        [UnityTest]
        public IEnumerator LateJoinerGetsTheWard()
        {
            var g = GameRoot.I;
            var p = VolcanoTests.AtTheGate(g, g.DebugStart(Still(4), "paladin", "human"), "academy");
            yield return Frames(2);
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            g.World.SetFlag(DialogueDb.WardActiveFlag);
            g.Warp(p, "emberhorn_heart", new Vector2Int(6, 10), false);
            yield return Frames(30);
            p.Hud.CloseAll();
            VolcanoTests.Opt(DialogueDb.Get("warden_parley").Nodes["turn"], "(Roll initiative").Do(new DCtx { P = p });
            yield return Frames(3);
            var c = g.Combats.Single();
            Assert.IsTrue(p.Character.Resistances.Contains(DamageType.Fire), "the first player has the ward");
            var p2 = g.DebugStart(Still(5), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            g.Warp(p2, "emberhorn_heart", p.MapPos + Vector2Int.left, false);
            yield return Frames(2);
            if (!c.Units.Any(u => u.Owner == p2)) c.JoinLate(p2);
            Assert.IsTrue(p2.Character.Resistances.Contains(DamageType.Fire), "so does a player who rushes in late");
        }

        // ------------------------------------------------------------------ 6, 7: attacks per action

        [UnityTest]
        public IEnumerator BrawlKeepsTheSecondPunch()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(15), "fighter", "human", level: 5);
            yield return Frames(2);
            g.Quests.Set("bar_brawl", 1, p, 0f); // Pip's farmhands are spoiling for it
            g.Warp(p, "tavern", new Vector2Int(8, 9), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            g.StartGroupCombat("drake_brawlers", p);
            var c = g.Combats.Single();
            foreach (var x in c.Units) if (x.Cb == null) x.Cb = new Combatant(x.C);
            var u = c.Units.First(x => x.Owner == p);
            Assert.IsTrue(c.Brawl);
            u.Cb.Turn.ActionUsed = true;
            u.Cb.Turn.AttacksLeftInAction = 1;
            var second = c.ActionsFor(p).FirstOrDefault(a => a.Name.StartsWith("Unarmed Strike") && a.Cost == ActionCost.Free);
            Assert.IsNotNull(second, "Extra Attack's second punch is on the bar in a brawl");
        }

        // ------------------------------------------------------------------ 9, 10: spell previews

        [UnityTest]
        public IEnumerator WallOfFirePreviewsAsAWall()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return Fight("wizard", 9, "forest_wolves", "forest", new Vector2Int(34, 8), (a, b, d) => { p = a; c = b; u = d; });
            var pc = (PlayerCharacter)u.C;
            if (!pc.PreparedSpells.Contains("wall_of_fire")) pc.PreparedSpells.Add("wall_of_fire");
            var wall = c.ActionsFor(p).First(a => a.Name.StartsWith("Wall of Fire"));
            var tiles = c.DebugAreaTiles(u, wall, u.Pos + new Vector2Int(4, 0));
            Assert.That(tiles.Count, Is.InRange(1, SpellsDb.Get("wall_of_fire").ZoneLineTiles), "the preview is the wall's tiles (clipped by the map), not a square of hundreds");
        }

        [UnityTest]
        public IEnumerator QuickenedLightningBoltIsStillALine()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return Fight("sorcerer", 5, "forest_wolves", "forest", new Vector2Int(34, 8), (a, b, d) => { p = a; c = b; u = d; });
            var pc = (PlayerCharacter)u.C;
            if (!pc.PreparedSpells.Contains("lightning_bolt")) pc.PreparedSpells.Add("lightning_bolt");
            if (!pc.Metamagic.Contains("quickened")) pc.Metamagic.Add("quickened");
            var quick = c.ActionsFor(p).First(a => a.Name.StartsWith("Lightning Bolt") && a.Name.Contains("Quickened"));
            Assert.IsTrue(quick.AreaLine, "the Quickened copy keeps the line shape");
        }

        // ------------------------------------------------------------------ 22: Spiritual Weapon stays with its caster

        [UnityTest]
        public IEnumerator SpiritualWeaponStrikesAgain()
        {
            PlayerSlot p = null; CombatInstance c = null; Unit u = null;
            yield return Fight("cleric", 5, "forest_wolves", "forest", new Vector2Int(34, 8), (a, b, d) => { p = a; c = b; u = d; });
            var pc = (PlayerCharacter)u.C;
            if (!pc.PreparedSpells.Contains("spiritual_weapon")) pc.PreparedSpells.Add("spiritual_weapon");
            var wolf = c.Units.First(x => x.C.Team == Team.Enemies);
            var input = new RollerInput(); var me = p; input.PressWhen = () => me.Hud.Dice.IsOpen && Time.frameCount % 2 == 0; p.Input = input;
            yield return c.DebugCast(u, "spiritual_weapon", wolf.Pos);
            Assert.IsTrue(pc.HasEffect("spiritual_weapon"), "the weapon is the caster's, hit or miss");
            Assert.IsFalse(wolf.C.HasEffect("spiritual_weapon"), "not the wolf's");
            u.Cb.Turn.BonusActionUsed = false;
            Assert.IsTrue(c.ActionsFor(p).Any(a => a.Name == "Spiritual Weapon strike"), "it can strike again as a Bonus Action");
        }

        // ------------------------------------------------------------------ 11-14: the interface

        [UnityTest]
        public IEnumerator SheetCountsEveryAbilityScoreImprovement()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human", level: 13);
            yield return Frames(2);
            var names = CharacterSheetScreen.FeatureNames(p.Character).ToList();
            Assert.Contains("Ability Score Improvement ×4", names, "a level-13 fighter has had four (4, 6, 8, 12)");
        }

        [UnityTest]
        public IEnumerator AClosedWindowDisappearsAtOnce()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(7), "fighter", "human");
            yield return Frames(2);
            p.Hud.CloseAll();
            var m = new MessageScreen("Test", "A window under the dice tray.");
            p.Hud.Push(m);
            yield return Frames(2);
            p.Hud.Dice.RollD20("Test roll", "", () => Dice.RollD20(AdvantageState.Normal), null, false, r => { });
            yield return Frames(2);
            Assert.IsTrue(p.Hud.Dice.IsOpen);
            m.Close();
            yield return Frames(2);
            Assert.IsFalse(m.Root.gameObject.activeSelf, "closed means gone from the screen, even while the dice tray is up");
        }

        [UnityTest]
        public IEnumerator NarratorStaysOutOfTheWayOfWindows()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(8), "fighter", "human");
            yield return Frames(2);
            p.Hud.CloseAll();
            p.Hud.Push(new MessageScreen("Test", "A tall window."));
            p.Hud.Narrate("Something happens.");
            yield return Frames(3);
            Assert.IsFalse(p.Hud.NarratorVisible, "the narrator box waits while a window is open (the line is in the log)");
            p.Hud.CloseAll();
            yield return Frames(3);
            Assert.IsTrue(p.Hud.NarratorVisible, "and comes back when it closes");
        }

        [UnityTest]
        public IEnumerator HeartsLeaveRoomForTheCloseButton()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(9), "fighter", "human");
            yield return Frames(3);
            p.Hud.CloseAll();
            var npc = Object.FindObjectsByType<NpcAgent>(FindObjectsSortMode.None).First(n => n.Def.Id == "brannoc");
            npc.Interact(p);
            yield return Frames(3);
            var d = p.Hud.Find<DialogueScreen>();
            Assert.IsNotNull(d);
            var hearts = d.Root.Find("Hearts") as RectTransform;
            Assert.IsNotNull(hearts, "the hearts have their own box");
            Assert.LessOrEqual(hearts.offsetMax.x, -50f, "which stops short of the ✕ in the corner");
        }

        // ------------------------------------------------------------------ 15-19: the world

        [UnityTest]
        public IEnumerator TheSearchableEverLampGlows()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 10);
            yield return Frames(2);
            g.Warp(p, "capital", new Vector2Int(40, 30), false);
            yield return Frames(3);
            var lamp = Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).First(s => s.Def.Id == "ever_lamp");
            Assert.IsNotNull(lamp.GetComponent<LightSource>(), "it glows like every other ever-lamp");
        }

        [Test]
        public void EverySecretTileHasItsSecret()
        {
            foreach (var z in WorldDb.Zones.Values)
                for (int y = 0; y < z.Map.Length; y++)
                    for (int x = 0; x < z.Map[y].Length; x++)
                        if ("OYq".IndexOf(z.Map[y][x]) >= 0)
                            Assert.IsTrue(z.Secrets.ContainsKey(new Vector2Int(x, y)), $"{z.Id}: the secret tile at ({x},{y}) has a SecretDef");
        }

        [UnityTest]
        public IEnumerator NightPatrolArrivesAtDusk()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 11);
            yield return Frames(2);
            g.Clock.Minutes = 12 * 60;
            g.Warp(p, "roots", new Vector2Int(20, 20), false);
            yield return Frames(3);
            bool Patrol() => Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(a => a.Group.Id == "roots_patrol" && a.isActiveAndEnabled);
            Assert.IsFalse(Patrol(), "no patrol at noon");
            g.Clock.Minutes = 20 * 60;
            g.DebugHour(20);
            yield return Frames(3);
            Assert.IsTrue(Patrol(), "the patrol comes out at dusk without leaving and coming back");
            g.Clock.Minutes = 6 * 60;
            g.DebugHour(6);
            yield return Frames(3);
            Assert.IsFalse(Patrol(), "and goes home at dawn");
        }

        [UnityTest]
        public IEnumerator CraleHasNoLadsLeft()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 12);
            yield return Frames(2);
            g.World.DefeatedGroups.Add("crale_men");
            var ctx = CapitalTests.Ctx(p);
            var men = DialogueDb.Get("crale").Nodes["men"];
            Assert.IsFalse(men.Options.Any(o => o.Text.StartsWith("(Fists up") && (o.Show == null || o.Show(ctx))), "no fight is offered with the crew already beaten");
            StringAssert.DoesNotContain("LADS", men.Text(ctx), "and he doesn't shout for lads who aren't there");
        }

        [Test]
        public void FirstStreetRetryIsOutOfSight()
        {
            Assert.Greater(Vector2.Distance(FinaleDb.RetrySpot(1), FinaleDb.StreetSpot(1)), 9f, "coming to behind the barricade, you get a moment before the fight sees you");
        }

        [UnityTest]
        public IEnumerator JoinerSeesTheEndingConfirm()
        {
            var g = GameRoot.I;
            var p1 = CapitalTests.ActThree(g, 13);
            yield return Frames(2);
            yield return FinaleTests.ToTheChoice(g, p1);
            g.OfferEnding(p1, Turn.Seal);
            yield return Frames(3);
            Assert.IsNotNull(p1.Hud.Find<ChoiceScreen>());
            var p2 = g.DebugStart(Still(14), "cleric", "dwarf");
            yield return Frames(3);
            Assert.IsNotNull(p2.Hud.Find<ChoiceScreen>(), "a player who joins while the choice is open is asked too");
        }
    }
}
