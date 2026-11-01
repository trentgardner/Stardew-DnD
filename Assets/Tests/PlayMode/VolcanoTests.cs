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
    /// <summary>Act II Phase 6: the boss engine and the Warden of the Emberhorn.</summary>
    public class VolcanoTests
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
            MonstersDb.All["test_boss"] = new MonsterDef
            {
                Id = "test_boss", Name = "Test Boss", SpriteId = "mon_ogre", AC = 18, HitDice = DiceExpr.Parse("10d10+100"), CR = 10, XP = 5900, Scale = 2f,
                Attacks = { new MonsterAttackDef { Name = "Rend", ToHit = 10, Damage = DiceExpr.Parse("2d6+6"), DamageType = DamageType.Slashing, ReachFt = 10 } },
                Abilities = { new MonsterAbilityDef { Name = "Grey Breath", Telegraph = true, Shape = TargetShape.Cone, AreaFt = 30, Dice = DiceExpr.Parse("10d6"), DamageType = DamageType.Fire, SaveDC = 17 } },
                LegendaryPerRound = 2,
                LegendaryActions = { new LegendaryActionDef { Name = "Tail Sweep", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Rend", RangeFt = 15 } },
                WeakSpotFlag = "test_weakness", PhaseTwoAt = 0.5, PhaseTwoText = "test phase two", PhaseTwoRechargeMin = 4, LairEruptions = 3,
                Immunities = { DamageType.Fire },
            };
            var grp = new EnemyGroupDef { Id = "test_boss_grp", Style = EncounterStyle.None, Unique = true };
            grp.Members.Add(new EnemySpawn("test_boss", BossAt.x, BossAt.y));
            WorldDb.Zones[Arena].EnemyGroups.Add(grp);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == "test_boss_grp");
            MonstersDb.All.Remove("test_boss");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>A fight with the test boss, paused at initiative (Still input never rolls it): the test drives the routines.</summary>
        static CombatInstance BossFight(GameRoot g, PlayerSlot p, out Unit boss, out Unit hero)
        {
            g.Warp(p, Arena, HeroAt, false);
            g.StartGroupCombat("test_boss_grp", p);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            boss = c.Units.Single(u => u.C is Monster m && m.Def.Id == "test_boss");
            hero = c.Units.Single(u => u.Owner == p);
            hero.A.PlaceAt(c.Zone, HeroAt);
            boss.A.PlaceAt(c.Zone, BossAt);
            return c;
        }

        [UnityTest]
        public IEnumerator LegendaryActionAfterAHerosTurn()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(3);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            Assert.AreEqual(2.0f, boss.A.Sr.transform.localScale.x, 0.01f, "drawn huge");
            yield return c.LegendaryOpportunities(hero);
            Assert.AreEqual(1, m.LegendaryLeft, "one legendary action spent");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("Legendary action: Test Boss uses Tail Sweep")));
            yield return c.LegendaryOpportunities(boss);
            Assert.AreEqual(1, m.LegendaryLeft, "never right after its own turn");
        }

        [UnityTest]
        public IEnumerator TelegraphThenBreathe()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "dwarf");
            yield return Frames(3);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            yield return c.DebugAiTurn(boss);
            Assert.IsNotNull(m.Charging, "it draws breath instead of breathing at once");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("rears back")));
            int hp = hero.C.HP;
            yield return c.ReleaseBreath(boss, m);
            Assert.IsNull(m.Charging);
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("lets go: Grey Breath")));
            Assert.Less(hero.C.HP, hp, "the breath lands");
        }

        [UnityTest]
        public IEnumerator WeakSpotChokes()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "fighter", "dwarf");
            yield return Frames(3);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            m.Charging = m.Def.Abilities[0];
            Assert.IsFalse(c.TryChoke(hero, boss), "not without knowing the weak spot");
            Assert.IsNotNull(m.Charging);
            g.World.SetFlag("test_weakness");
            int hp = m.HP;
            Assert.IsTrue(c.TryChoke(hero, boss));
            Assert.IsNull(m.Charging, "the breath is lost");
            Assert.Less(m.HP, hp, "4d10 force");
            Assert.AreEqual(0, m.LegendaryLeft);
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("chokes on its own fire")));
        }

        [UnityTest]
        public IEnumerator DragonWingsShelter()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "dwarf");
            yield return Frames(3);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            yield return Frames(2);
            g.Warp(p, Arena, HeroAt, false);
            yield return Frames(2);
            g.DragonAgent.Actor.PlaceAt(p.Zone, HeroAt + Vector2Int.right);
            var c = BossFight(g, p, out var boss, out var hero);
            var dragon = c.Units.Single(u => u.C is Dragonling);
            dragon.A.PlaceAt(c.Zone, HeroAt + Vector2Int.right);
            var m = (Monster)boss.C;
            m.Charging = m.Def.Abilities[0];
            yield return c.ReleaseBreath(boss, m);
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("sheltered by")), "the dragon's wings give Advantage on the save");
        }

        // ============================================================ Task 2: phase two and lair eruptions

        [UnityTest]
        public IEnumerator PhaseTwoAtHalf()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(10), "fighter", "dwarf");
            yield return Frames(3);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            m.HP = m.MaxHP / 2 + 5;
            c.OnDamaged(boss);
            Assert.IsFalse(m.PhaseTwo);
            m.HP = m.MaxHP / 2;
            c.OnDamaged(boss);
            Assert.IsTrue(m.PhaseTwo);
            c.OnDamaged(boss);
            Assert.AreEqual(1, p.Hud.LogLines.Count(l => l.Contains("test phase two")), "narrated once");
            Assert.AreEqual(4, m.RechargeMinFor(m.Def.Abilities[0]));
        }

        [UnityTest]
        public IEnumerator EruptionsOnlyOnFloor()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(11), "fighter", "dwarf");
            yield return Frames(3);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            g.Warp(p, Arena, HeroAt, false);
            yield return Frames(2);
            g.DragonAgent.Actor.PlaceAt(p.Zone, HeroAt + Vector2Int.right);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            Assert.AreEqual(0, c.PickEruptionTiles(new System.Random(1)).Count, "no eruptions in phase one");
            m.HP = m.MaxHP / 2;
            c.OnDamaged(boss);
            for (int seed = 0; seed < 50; seed++)
            {
                var tiles = c.PickEruptionTiles(new System.Random(seed));
                Assert.AreEqual(3, tiles.Count, $"seed {seed}: three eruptions");
                foreach (var t in tiles) Assert.IsTrue(c.Zone.InBounds(t) && c.Zone.IsWalkable(t), $"seed {seed}: {t} is floor");
            }
            var dragon = c.Units.Single(u => u.C is Dragonling);
            dragon.A.PlaceAt(c.Zone, HeroAt + Vector2Int.right);
            int dhp = dragon.C.HP;
            yield return c.Erupt(new System.Collections.Generic.List<Vector2Int> { dragon.Pos });
            Assert.AreEqual(dhp, dragon.C.HP, "the red dragon shrugs off the lava");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("The floor erupts")));
        }

        // ============================================================ Task 3: the Emberhorn

        static PlayerSlot AtTheGate(GameRoot g, PlayerSlot p, string side)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Factions.Commit(side, p);
            g.World.SetFlag("banner_" + side);
            g.Quests.Set(Answers.Id, Answers.OnTheRoad, p);
            return p;
        }

        static bool CanReachBeside(Zone z, Vector2Int from, Vector2Int m)
        {
            if (z.IsWalkable(m) && (m == from || z.FindPath(from, m, null, 40000).Count > 0)) return true;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var t = m + d;
                if (z.IsWalkable(t) && (t == from || z.FindPath(from, t, null, 40000).Count > 0)) return true;
            }
            return false;
        }

        [UnityTest]
        public IEnumerator GateOpensAtAnswersEight()
        {
            var g = GameRoot.I;
            var p = AtTheGate(g, g.DebugStart(Still(20), "fighter", "dwarf"), "crown");
            yield return Frames(2);
            var gate = WorldDb.Zones["emberhorn_road"].Warps.Find(w => w.At == new Vector2Int(59, 14));
            Assert.IsFalse(gate.Open(g.World), "look up at the mountain first");
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            Assert.IsTrue(g.World.Flag(WorldDb.EmberhornGate));
            Assert.IsTrue(gate.Open(g.World));
            Assert.AreEqual("emberhorn_throat", gate.ToZone);
            Assert.IsTrue(WorldDb.Zones["emberhorn_throat"].Warps.Any(w => w.ToZone == "emberhorn_heart"));
        }

        [UnityTest]
        public IEnumerator EmberhornIsReachable()
        {
            var g = GameRoot.I;
            var p = AtTheGate(g, g.DebugStart(Still(21), "fighter", "dwarf"), "crown");
            yield return Frames(2);
            foreach (var id in new[] { "emberhorn_throat", "emberhorn_heart" })
            {
                var z = g.Zones[id];
                var from = z.Def.DefaultSpawn;
                foreach (var kv in z.Def.Secrets) Assert.IsTrue(CanReachBeside(z, from, kv.Key), $"{id}: secret {kv.Value.Id}");
                foreach (var l in z.Def.Lore) Assert.IsTrue(CanReachBeside(z, from, l.Pos), $"{id}: lore {l.Id}");
                foreach (var w in z.Def.Warps) Assert.IsTrue(CanReachBeside(z, from, w.At), $"{id}: warp at {w.At}");
                foreach (var grp in z.Def.EnemyGroups) foreach (var m in grp.Members) Assert.IsTrue(CanReachBeside(z, from, m.Pos), $"{id}: {grp.Id} at {m.Pos}");
            }
            Assert.IsTrue(CanReachBeside(g.Zones["emberhorn_heart"], g.Zones["emberhorn_heart"].Def.DefaultSpawn, new Vector2Int(3, 12)), "the ally's spot");
        }

        [Test]
        public void WardenStatBlock()
        {
            var w = MonstersDb.Get("warden");
            Assert.AreEqual(18, w.AC);
            CollectionAssert.AreEqual(new[] { "Rend", "Rend", "Rend" }, w.Multiattack);
            Assert.IsTrue(w.Abilities.Single().Telegraph, "the breath is drawn first");
            Assert.AreEqual(2, w.LegendaryPerRound);
            Assert.AreEqual(2, w.LegendaryActions.Count);
            Assert.AreEqual(0.5, w.PhaseTwoAt);
            Assert.AreEqual("warden_weakness", w.WeakSpotFlag);
            Assert.Contains(DamageType.Fire, w.Immunities);
            Assert.IsNotNull(w.Attacks.SingleOrDefault(a => a.Name == "Tail"), "the tail sweep has an attack to use");
        }

        [UnityTest]
        public IEnumerator AllyBoonOncePerSide([Values("crown", "academy", "fae", "hunters")] string side)
        {
            var g = GameRoot.I;
            var p = AtTheGate(g, g.DebugStart(Still(22), "fighter", "dwarf"), side);
            yield return Frames(2);
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            string ally = new System.Collections.Generic.Dictionary<string, string> { ["crown"] = "ally_holloway", ["academy"] = "ally_perrin", ["fae"] = "ally_thimble", ["hunters"] = "ally_odo" }[side];
            Assert.AreEqual("emberhorn_heart", WorldDb.Current(WorldDb.Npcs[ally], 12, 1, g.World)?.Zone, "the ally went ahead to the Heart");
            g.Warp(p, "emberhorn_heart", new Vector2Int(2, 10), false);
            yield return Frames(4);
            p.Hud.CloseAll();
            var agent = Object.FindObjectsByType<NpcAgent>(FindObjectsSortMode.None).Single(n => n.Def.Id == ally);
            var c = new DCtx { P = p, Npc = agent };
            var d = DialogueDb.Get(ally);
            Assert.AreEqual("boon", d.Start(c));
            p.Character.TempHP = 0; p.Character.HeroicInspiration = false;
            d.Nodes["boon"].OnEnter(c);
            switch (side)
            {
                case "crown": Assert.GreaterOrEqual(p.Character.TempHP, 15); break;
                case "academy": Assert.IsTrue(g.World.Flag(DialogueDb.WardActiveFlag)); break;
                case "fae": Assert.IsTrue(p.Character.HeroicInspiration); break;
                case "hunters": Assert.IsTrue(g.World.Flag(DialogueDb.WardenMarkedFlag)); break;
            }
            p.Character.TempHP = 0; p.Character.HeroicInspiration = false;
            d.Nodes["boon"].OnEnter(c);
            Assert.AreEqual(0, p.Character.TempHP, "once");
            Assert.IsFalse(p.Character.HeroicInspiration, "once");
            Assert.AreEqual("heart_again", d.Start(c));
        }

        // ============================================================ Task 4: the illusion of peace, the fight, the half solution

        static PlayerSlot AtTheHeart(GameRoot g, PlayerSlot p, string side = "crown")
        {
            AtTheGate(g, p, side);
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            g.Warp(p, "emberhorn_heart", new Vector2Int(2, 10), false);
            p.Hud.CloseAll();
            return p;
        }

        static IEnumerator FinishFight(GameRoot g, PlayerSlot p, bool win)
        {
            var input = p.Input;
            p.Input = new FuzzInput(7) { CancelChance = 0, MenuChance = 0 };
            yield return Frames(2);
            foreach (var c in g.Combats.ToList())
                foreach (var u in c.Units)
                {
                    if (win && u.C.Team == Team.Enemies) u.C.HP = 0;
                    if (!win && u.C.Team == Team.Players) { u.C.HP = 0; if (u.C is PlayerCharacter pc) pc.DeathFailures = 3; }
                }
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 90f) yield return null;
            Time.timeScale = 1;
            p.Input = input;
            yield return Frames(30);
            Assert.IsFalse(g.AnyInCombat, "the fight ends");
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator TalkAlwaysEndsInAFight()
        {
            var g = GameRoot.I;
            var p = AtTheHeart(g, g.DebugStart(Still(30), "bard", "elf"));
            yield return Frames(3);
            var d = DialogueDb.Get("warden_parley");
            foreach (var node in d.Nodes.Values.Where(n => n.Id != "turn"))
            {
                // From every node, some path reaches "turn".
                var seen = new System.Collections.Generic.HashSet<string>();
                var q = new System.Collections.Generic.Queue<string>(); q.Enqueue(node.Id);
                while (q.Count > 0)
                {
                    var id = q.Dequeue();
                    if (!seen.Add(id) || !d.Nodes.ContainsKey(id)) continue;
                    foreach (var o in d.Nodes[id].Options) foreach (var nx in new[] { o.Next, o.Success, o.Failure }) if (nx != null) q.Enqueue(nx);
                }
                Assert.IsTrue(seen.Contains("turn"), $"{node.Id} can reach the fight");
            }
            var c = new DCtx { P = p };
            Opt(d.Nodes["turn"], "(Roll initiative").Do(c);
            yield return Frames(2);
            Assert.IsTrue(g.Combats.Any(x => x.GroupId == "warden"), "the talk ends in the fight");
            Assert.IsFalse(g.World.PacifiedGroups.Contains("warden"), "no peaceful way past it");
        }

        static DOption Opt(DNode n, string startsWith) => n.Options.First(o => o.Text.StartsWith(startsWith));

        [UnityTest]
        public IEnumerator InsightOnlyWithTheBestiary()
        {
            var g = GameRoot.I;
            var p = AtTheHeart(g, g.DebugStart(Still(31), "cleric", "dwarf"));
            yield return Frames(3);
            var c = new DCtx { P = p };
            var insight = DialogueDb.Get("warden_parley").Nodes["speak"].Options.Single(o => o.Check == Skill.Insight);
            Assert.IsFalse(insight.Show(c), "no bestiary, no weak spot");
            g.World.LoreFound.Add("emberhorn_warden");
            Assert.IsTrue(insight.Show(c), "you read the bestiary in the Restricted Section");
            insight.OnSuccess(c);
            Assert.IsTrue(g.World.Flag(DialogueDb.WardenWeakness));
        }

        [UnityTest]
        public IEnumerator RollingWellPays()
        {
            var g = GameRoot.I;
            var p = AtTheHeart(g, g.DebugStart(Still(32), "bard", "elf"));
            yield return Frames(3);
            var c = new DCtx { P = p };
            var d = DialogueDb.Get("warden_parley");
            d.Nodes["speak"].Options.Single(o => o.Check == Skill.Persuasion).OnSuccess(c);
            Assert.IsTrue(g.World.Flag(DialogueDb.WardenHesitated));
            var show = d.Nodes["speak"].Options.Single(o => o.Check == Skill.AnimalHandling);
            show.OnSuccess(c);
            Assert.IsTrue(g.World.Flag(DialogueDb.WardenSparesDragon));
            Assert.IsTrue(Opt(d.Nodes["hesitates"], "(It's time").Show(c), "two tries: it's time");
            Opt(d.Nodes["turn"], "(Roll initiative").Do(c);
            yield return Frames(2);
            Assert.IsTrue(g.Combats.Single().EnemiesSurprised, "it hesitated: the party gets the first move");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator VictoryEasesTheBlightOnce()
        {
            var g = GameRoot.I;
            var p = AtTheHeart(g, g.DebugStart(Still(33), "fighter", "dwarf"));
            yield return Frames(3);
            Assert.AreEqual(3, g.World.BlightStage);
            var c = new DCtx { P = p };
            Opt(DialogueDb.Get("warden_parley").Nodes["turn"], "(Roll initiative").Do(c);
            yield return FinishFight(g, p, true);
            Assert.AreEqual(Answers.Rest, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(2, g.World.BlightStage, "the half solution: the ash thins");
            Assert.IsTrue(g.World.BlightEased && g.World.Flag(GameRoot.WardenFallen));
            Assert.IsTrue(g.World.LoreFound.Contains("warden_last_words"));
            Assert.IsTrue(p.Hud.LogLines.Any(l => l.Contains("Hold... the door")));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(2, g.World.BlightStage, "eased exactly once, after a reload too");
            Assert.AreEqual(Answers.Rest, g.Quests.Stage(Answers.Id));
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator WipeResetsTheWarden()
        {
            var g = GameRoot.I;
            var p = AtTheHeart(g, g.DebugStart(Still(34), "fighter", "dwarf"));
            yield return Frames(3);
            g.World.SetFlag(DialogueDb.WardenHesitated);
            g.World.SetFlag(DialogueDb.WardenWeakness);
            g.World.SetFlag("warden_try_persuade");
            var c = new DCtx { P = p };
            Opt(DialogueDb.Get("warden_parley").Nodes["turn"], "(Roll initiative").Do(c);
            yield return FinishFight(g, p, false);
            yield return Frames(120);
            Assert.IsFalse(g.World.DefeatedGroups.Contains("warden"), "the Warden still stands");
            Assert.IsFalse(g.World.Flag(DialogueDb.WardenHesitated), "the talk's boons must be earned again");
            Assert.IsFalse(g.World.Flag("warden_try_persuade"), "and every approach can be tried again");
            Assert.IsTrue(g.World.Flag(DialogueDb.WardenWeakness), "what you learned, you keep");
            Assert.AreNotEqual(Answers.Rest, g.Quests.Stage(Answers.Id));
        }

        [UnityTest]
        public IEnumerator CoopPartnerJoinsTheWardenFight()
        {
            var g = GameRoot.I;
            var p1 = AtTheHeart(g, g.DebugStart(Still(35), "fighter", "dwarf"));
            var p2 = g.DebugStart(Still(36), "wizard", "elf");
            yield return Frames(3);
            p2.Hud.CloseAll();
            g.Warp(p2, "town", WorldDb.Zones["town"].DefaultSpawn, false); // P2 is back home when P1 starts it
            yield return Frames(2);
            Opt(DialogueDb.Get("warden_parley").Nodes["turn"], "(Roll initiative").Do(new DCtx { P = p1 });
            yield return Frames(2);
            var fight = g.Combats.Single();
            Assert.IsFalse(fight.Humans.Contains(p2), "P2 is elsewhere");
            g.Warp(p2, "emberhorn_heart", new Vector2Int(4, 10), false);
            yield return Frames(10);
            Assert.IsTrue(fight.Humans.Contains(p2), "P2 walks in and joins the fight");
            Assert.IsFalse(p2.Hud.Top is DialogueScreen, "not stuck in a conversation");
        }

        [UnityTest]
        public IEnumerator NoBreathOrLegendaryFromTheFallen()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "dwarf");
            yield return Frames(3);
            var c = BossFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;
            m.Charging = m.Def.Abilities[0];
            m.HP = 0;
            int lines = c.LogLines.Count;
            yield return c.ReleaseBreath(boss, m);
            yield return c.LegendaryOpportunities(hero);
            Assert.IsFalse(c.LogLines.Skip(lines).Any(l => l.Contains("lets go") || l.Contains("Legendary action")), "nothing from a fallen boss");
        }
    }
}
