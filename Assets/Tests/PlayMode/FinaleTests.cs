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
    /// <summary>Act III Phase 6: the finales (phase beats, allies, the streets, the flight, the bosses).</summary>
    public class FinaleTests
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
            MonstersDb.All["test_phased"] = new MonsterDef
            {
                Id = "test_phased", Name = "Test Phased", SpriteId = "mon_ogre", AC = 18, HitDice = DiceExpr.Parse("10d10+100"), CR = 10, XP = 5900,
                Attacks = { new MonsterAttackDef { Name = "Rend", ToHit = 10, Damage = DiceExpr.Parse("2d6+6"), DamageType = DamageType.Slashing, ReachFt = 10 } },
                LegendaryPerRound = 1,
                LegendaryActions = { new LegendaryActionDef { Name = "Tail", Cost = 1, Kind = LegendaryKind.Attack, Attack = "Rend", RangeFt = 15 } },
                Phases =
                {
                    new BossPhaseDef { At = 0.66, Beats = new[] { "beat A1", "beat A2" } },
                    new BossPhaseDef { At = 0.33, Beats = new[] { "beat B" } },
                },
            };
            var grp = new EnemyGroupDef { Id = "test_phased_grp", Style = EncounterStyle.None, Unique = true };
            grp.Members.Add(new EnemySpawn("test_phased", BossAt.x, BossAt.y));
            WorldDb.Zones[Arena].EnemyGroups.Add(grp);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == "test_phased_grp");
            MonstersDb.All.Remove("test_phased");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
            Dice.Rng = new SystemRandom();
            GameRoot.FlightResults.Clear();
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        /// <summary>The beat on top of the stack (closed screens linger while the initiative dice is open, so skip them).</summary>
        static MessageScreen Beat(PlayerSlot p) => p.Hud.Top is MessageScreen m && !m.Closed ? m : null;

        /// <summary>A fight with the phased test boss, paused at initiative (Still input never rolls it): the test drives it.</summary>
        static CombatInstance PhasedFight(GameRoot g, PlayerSlot p, out Unit boss, out Unit hero)
        {
            g.Warp(p, Arena, HeroAt, false);
            g.StartGroupCombat("test_phased_grp", p);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            boss = c.Units.Single(u => u.C is Monster m && m.Def.Id == "test_phased");
            hero = c.Units.Single(u => u.Owner == p);
            hero.A.PlaceAt(c.Zone, HeroAt);
            boss.A.PlaceAt(c.Zone, BossAt);
            return c;
        }

        // ================================================================== Task 2: phase beats

        [UnityTest]
        public IEnumerator HugeHitStillPlaysEveryBeat()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(3);
            var c = PhasedFight(g, p, out var boss, out var hero);
            var m = (Monster)boss.C;

            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.IsTrue(c.BeatsPlaying, "the fight stops for the phase");
            Assert.AreEqual("beat A1", Beat(p)?.Body);
            Assert.IsFalse(m.IsDown, "not down at a threshold");
            Beat(p).Close();
            yield return Frames(2);
            Assert.AreEqual("beat A2", Beat(p)?.Body, "every beat, in order");
            Beat(p).Close();
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying, "the fight goes on");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("phase 2")), "logged (phase 1 is the opening)");

            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.AreEqual("beat B", Beat(p)?.Body);
            Beat(p).Close();
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying);

            c.DebugDamage(boss, 9999);
            Assert.IsTrue(m.IsDown, "the last phase can be finished");
        }

        [UnityTest]
        public IEnumerator AiWaitsForBeats()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "dwarf");
            yield return Frames(3);
            var c = PhasedFight(g, p, out var boss, out var hero);
            c.DebugDamage(boss, 9999);
            yield return Frames(2);
            Assert.IsTrue(c.BeatsPlaying);
            int hp = hero.C.HP;
            var ai = g.StartCoroutine(c.DebugAiTurn(boss));
            yield return Frames(30);
            Assert.AreEqual(hp, hero.C.HP, "nothing hits you while the story is on screen");
            Assert.IsFalse(c.LogLines.Any(l => l.Contains("Rend")), "the boss hasn't swung");
            while (Beat(p) != null) { Beat(p).Close(); yield return Frames(2); }
            yield return Frames(3);
            Assert.IsFalse(c.BeatsPlaying);
        }

        // ================================================================== Task 3: allies

        static WorldState Chosen(string ending, params string[] flags)
        {
            var w = new WorldState();
            w.SetFlag(WorldDb.MetHer);
            w.SetFlag(Turn.EndingFlag(ending));
            w.SetFlag(Turn.Chosen);
            foreach (var f in flags) w.SetFlag(f);
            return w;
        }

        [Test]
        public void AlliesTableFollowsReactions()
        {
            var end = FinaleDb.Allies(Turn.End, Chosen(Turn.End)).Select(a => a.monsterId).ToList();
            CollectionAssert.Contains(end, "crown_knight", "the Crown stands with you if you end her");
            CollectionAssert.Contains(end, "wyrmwatch_hunter", "and so does Wyrmwatch");
            var save = FinaleDb.Allies(Turn.Save, Chosen(Turn.Save, "banner_fae"));
            CollectionAssert.Contains(save.Select(a => a.monsterId).ToList(), "fae_knight");
            Assert.IsFalse(save.Any(a => a.name == "Household Guard"), "Marsh only defects if she's yours");
            var marsh = FinaleDb.Allies(Turn.Save, Chosen(Turn.Save, "banner_fae", "banner_crown"));
            Assert.IsTrue(marsh.Any(a => a.name == "Household Guard"), "Marsh's Household Guard comes with her");
            foreach (var e in Turn.Endings)
                Assert.LessOrEqual(FinaleDb.Allies(e, Chosen(e, "banner_crown", "banner_fae", "banner_academy", "banner_hunters", "swayed_academy", "swayed_fae", "swayed_hunters")).Count, 3);
        }

        void SetUpFinaleTestGroup()
        {
            var grp = new EnemyGroupDef { Id = Finale.Prefix + "test", Style = EncounterStyle.None, Unique = true };
            grp.Members.Add(new EnemySpawn("crown_knight", BossAt.x, BossAt.y));
            grp.Members.Add(new EnemySpawn("crown_knight", BossAt.x + 1, BossAt.y));
            WorldDb.Zones[Arena].EnemyGroups.Add(grp);
        }

        CombatInstance FinaleTestFight(GameRoot g, PlayerSlot p, string ending)
        {
            g.World.SetFlag(WorldDb.MetHer); g.World.SetFlag(Turn.EndingFlag(ending)); g.World.SetFlag(Turn.Chosen);
            g.Warp(p, Arena, HeroAt, false);
            g.StartGroupCombat(Finale.Prefix + "test", p);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            return c;
        }

        [UnityTest]
        public IEnumerator AlliesFightForYouAndLeave()
        {
            SetUpFinaleTestGroup();
            try
            {
                var g = GameRoot.I;
                var p = g.DebugStart(Still(3), "fighter", "dwarf");
                yield return Frames(3);
                var c = FinaleTestFight(g, p, Turn.End);
                int xpBefore = p.Character.XP; // after the warp (discovering the arena pays 15)
                var allies = c.Units.Where(u => u.Ally).ToList();
                Assert.AreEqual(2, allies.Count, "the Crown and Wyrmwatch each send someone");
                Assert.IsTrue(allies.All(a => a.C.Team == Team.Players && a.Ctrl == null));
                var foe = c.Units.First(u => u.C.Team == Team.Enemies);
                foreach (var a in allies) { a.A.PlaceAt(c.Zone, foe.Pos + Vector2Int.left); break; }
                int heroHp = p.Character.HP, foeHp = foe.C.HP;
                for (int i = 0; i < 6 && foe.C.HP == foeHp; i++) yield return c.DebugAiTurn(allies[0]);
                Assert.Less(foe.C.HP, foeHp, "an ally attacks the enemy");
                Assert.AreEqual(heroHp, p.Character.HP, "and never you");
                foreach (var f in c.Units.Where(u => u.C.Team == Team.Enemies).ToList()) c.DebugDamage(f, 9999);
                c.DebugEndCheck();
                yield return Frames(5);
                Assert.IsTrue(c.Finished);
                Assert.AreEqual(0, Object.FindObjectsByType<FinaleAlly>(FindObjectsSortMode.None).Length, "allies leave when the fight ends");
                Assert.AreEqual(xpBefore + GameRoot.EncounterShare(1400, 1), p.Character.XP, "XP from the foes only");
                Assert.IsFalse(g.CompanionsOf(p).Any(cm => allies.Any(a => a.C == cm.Character)), "allies aren't companions");
            }
            finally { WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == Finale.Prefix + "test"); }
        }

        [UnityTest]
        public IEnumerator AllyDownIsNotAWipe()
        {
            SetUpFinaleTestGroup();
            try
            {
                var g = GameRoot.I;
                var p = g.DebugStart(Still(4), "fighter", "dwarf");
                yield return Frames(3);
                var c = FinaleTestFight(g, p, Turn.End);
                foreach (var a in c.Units.Where(u => u.Ally).ToList()) c.DebugDamage(a, 9999);
                c.DebugEndCheck();
                yield return Frames(3);
                Assert.IsFalse(c.Finished, "the fight goes on");
                Assert.IsTrue(c.LogLines.Any(l => l.Contains("falls back")), "the ally falls back");
                // And when every player is down, allies still standing don't keep the fight going.
                var ally2 = c.Units.Where(u => u.Ally).ToList();
                foreach (var a in ally2) { a.C.HP = a.C.MaxHP; }
                p.Character.HP = 0;
                Assert.IsFalse(c.DebugPlayersUp, "allies don't count as the party");
            }
            finally { WorldDb.Zones[Arena].EnemyGroups.RemoveAll(e => e.Id == Finale.Prefix + "test"); }
        }

        // ================================================================== Task 4: the streets

        static System.Collections.Generic.List<string> Ids(System.Collections.Generic.List<EnemySpawn> m) => m.Select(x => x.MonsterId).ToList();

        [Test]
        public void StreetMembersFollowTheTable()
        {
            var endFire = FinaleDb.StreetMembers(1, Turn.End, Chosen(Turn.End, "fae_clause"));
            CollectionAssert.AreEqual(new[] { "cinder_hound", "cinder_hound" }, Ids(endFire), "End, the Fae held back: only the fire comes up");
            CollectionAssert.Contains(Ids(FinaleDb.StreetMembers(1, Turn.End, Chosen(Turn.End))), "fae_knight", "the Fae stand against ending her");
            var sealMarsh = Ids(FinaleDb.StreetMembers(1, Turn.Seal, Chosen(Turn.Seal, "banner_crown")));
            Assert.AreEqual(1, sealMarsh.Count(i => i == "crown_knight"), "Marsh took half the guard with her");
            Assert.IsFalse(sealMarsh.Contains("fae_knight"), "the Fae stand aside for the seal");
            foreach (var e in Turn.Endings)
                for (int n = 1; n <= Finale.Stages; n++)
                {
                    var m = FinaleDb.StreetMembers(n, e, Chosen(e));
                    Assert.That(m.Count, Is.InRange(1, 6), $"{e} stage {n}: 1-6 foes");
                    Assert.AreEqual(m.Count, m.Select(x => x.Pos).Distinct().Count(), $"{e} stage {n}: one per tile");
                }
        }

        [UnityTest]
        public IEnumerator StreetSpotsAreReachable()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 5);
            yield return Frames(2);
            var z = g.Zones["capital"];
            for (int n = 1; n <= Finale.Stages; n++)
            {
                foreach (var e in Turn.Endings)
                    foreach (var m in FinaleDb.StreetMembers(n, e, Chosen(e)))
                        Assert.IsTrue(z.IsWalkable(m.Pos), $"{e} stage {n}: {m.MonsterId} at {m.Pos} stands on open ground");
                foreach (var spot in new[] { FinaleDb.StreetSpot(n), FinaleDb.RetrySpot(n) })
                {
                    Assert.IsTrue(z.IsWalkable(spot), $"stage {n}: {spot} is walkable");
                    Assert.Greater(z.FindPath(WorldDb.SunstairSpot, spot, null, 40000).Count, 0, $"stage {n}: {spot} is reachable from the Sunstair");
                }
            }
            Assert.IsTrue(z.IsWalkable(FinaleDb.DragonWaitSpot));
        }

        /// <summary>Met her, questioned, standing at the foot of the Sunstair: ready to choose.</summary>
        static IEnumerator ToTheChoice(GameRoot g, PlayerSlot p, params string[] flags)
        {
            g.Quests.Set(WarmCity.Id, WarmCity.Called, p, 0f);
            foreach (var f in new[] { WorldDb.MetHer, "meeting_seen", Turn.Questioning, Turn.BeatsSeen }.Concat(flags)) g.World.SetFlag(f);
            g.GatherAt("capital", WorldDb.SunstairSpot); // everyone ready
            yield return Frames(3);
            foreach (var q in g.Players.Where(x => x.IsReady)) q.Hud.CloseAll();
        }

        /// <summary>Closes beats (untitled break beats and titled stage intros) until none is left.</summary>
        static IEnumerator CloseBeats(PlayerSlot p, System.Collections.Generic.List<MessageScreen> seen = null)
        {
            for (int i = 0; i < 12; i++)
            {
                yield return Frames(4);
                var b = Beat(p);
                if (b == null) { yield return Frames(4); if (Beat(p) == null) yield break; continue; }
                seen?.Add(b);
                b.Close();
            }
        }

        static bool Spawned(string groupId) => Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).Any(a => a.Group.Id == groupId && !a.Monster.IsDown);

        /// <summary>Fights stage n and wins it at once.</summary>
        static IEnumerator WinStreet(GameRoot g, PlayerSlot p, int n)
        {
            p.Hud.CloseAll();
            g.StartGroupCombat(Finale.StreetGroup(n), p);
            yield return Frames(2);
            var c = g.Combats.Single(x => x.GroupId == Finale.StreetGroup(n));
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            foreach (var f in c.Units.Where(u => u.C.Team == Team.Enemies).ToList()) c.DebugDamage(f, 9999);
            c.DebugEndCheck();
            yield return Frames(4);
        }

        [UnityTest]
        public IEnumerator StreetsRunInOrder()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 6, side: "academy");
            yield return Frames(2);
            yield return ToTheChoice(g, p);
            g.ChooseEnding(p, Turn.Seal);
            var seen = new System.Collections.Generic.List<MessageScreen>();
            yield return CloseBeats(p, seen);
            Assert.IsFalse(seen.Any(b => b.Title == "To Be Continued"), "no placeholder after the break any more");
            Assert.IsTrue(seen.Any(b => b.Title == FinaleDb.StreetTitle(1)), "the first stage's intro played");
            Assert.AreEqual(WarmCity.Streets, g.Quests.Stage(WarmCity.Id));
            Assert.IsTrue(g.World.Flag(Finale.Started));
            Assert.IsTrue(Spawned(Finale.StreetGroup(1)), "stage 1 is waiting in the square");
            Assert.IsFalse(Spawned(Finale.StreetGroup(2)) || Spawned(Finale.StreetGroup(3)), "and only stage 1");
            for (int n = 1; n <= Finale.Stages; n++)
            {
                Assert.AreEqual(n, Finale.CurrentStreet(g.World));
                yield return WinStreet(g, p, n);
                seen.Clear();
                yield return CloseBeats(p, seen);
                Assert.IsTrue(g.World.Flag(Finale.StreetDone(n)), $"stage {n} done");
                if (n < Finale.Stages)
                {
                    Assert.IsTrue(Spawned(Finale.StreetGroup(n + 1)), $"stage {n + 1} opens");
                    Assert.IsTrue(seen.Any(b => b.Title == FinaleDb.StreetTitle(n + 1)), $"stage {n + 1}'s intro played");
                }
            }
            Assert.AreEqual(Finale.Stages + 1, Finale.CurrentStreet(g.World));
            Assert.GreaterOrEqual(g.Quests.Stage(WarmCity.Id), WarmCity.Flight, "the streets are won: up into the air");
        }

        [UnityTest]
        public IEnumerator EndPathDragonStaysOut()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 7, side: "hunters");
            yield return Frames(2);
            yield return ToTheChoice(g, p);
            g.ChooseEnding(p, Turn.End);
            yield return CloseBeats(p);
            Assert.IsTrue(g.World.Flag(Finale.DragonWaits));
            Assert.LessOrEqual(Vector2.Distance(g.DragonAgent.Actor.MapPos, FinaleDb.DragonWaitSpot), 1.5f, "it lies across the top of the stair down");
            p.Hud.CloseAll();
            g.StartGroupCombat(Finale.StreetGroup(1), p);
            yield return Frames(2);
            Assert.IsFalse(g.Combats.Single().Units.Any(u => u.C is Dragonling), "it won't fight for this");
        }

        [UnityTest]
        public IEnumerator FinaleWipeRetriesInTheCapital()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 8, side: "academy");
            yield return Frames(2);
            yield return ToTheChoice(g, p);
            g.ChooseEnding(p, Turn.Seal);
            yield return CloseBeats(p);
            yield return WinStreet(g, p, 1);
            yield return CloseBeats(p);
            int day = g.Clock.Day, copper = p.Character.Copper;
            g.StartGroupCombat(Finale.StreetGroup(2), p);
            yield return Frames(2);
            var c = g.Combats.Single();
            foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            foreach (var u in c.Units.Where(u => u.C.Team == Team.Players && !u.Ally)) u.C.HP = 0;
            c.DebugEndCheck();
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Frames(3);
            Assert.AreEqual("capital", p.Zone.Def.Id, "never the Thistledown temple");
            Assert.LessOrEqual(Vector2.Distance(p.MapPos, FinaleDb.RetrySpot(2)), 2f, "behind the barricade before the next fight");
            Assert.AreEqual(day, g.Clock.Day, "no day lost");
            Assert.AreEqual(copper, p.Character.Copper, "no temple fee");
            Assert.Greater(p.Character.HP, 0);
            Assert.IsTrue(Spawned(Finale.StreetGroup(2)), "stage 2 is waiting again");
        }

        [UnityTest]
        public IEnumerator FinaleResumesAfterLoadAtEveryStage()
        {
            var g = GameRoot.I;
            var p = CapitalTests.ActThree(g, 9, side: "academy");
            yield return Frames(2);
            yield return ToTheChoice(g, p);
            // (a) Saved by the choice itself, before the break's beats were seen: they replay, then stage 1 opens.
            g.ChooseEnding(p, Turn.Seal);
            yield return Frames(2);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(5);
            p = g.Players.First();
            var seen = new System.Collections.Generic.List<MessageScreen>();
            yield return CloseBeats(p, seen);
            Assert.GreaterOrEqual(seen.Count, 3, "the break replays");
            Assert.IsTrue(g.World.Flag(Finale.StreetOpen(1)));
            Assert.IsTrue(Spawned(Finale.StreetGroup(1)));
            // (b) After stage 1: stage 2 is open, stage 1 doesn't come back.
            yield return WinStreet(g, p, 1);
            yield return CloseBeats(p);
            path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(5);
            p = g.Players.First();
            yield return CloseBeats(p);
            Assert.IsFalse(Spawned(Finale.StreetGroup(1)), "stage 1 stays won");
            Assert.IsTrue(Spawned(Finale.StreetGroup(2)), "stage 2 is waiting");
            Assert.AreEqual(2, Finale.CurrentStreet(g.World));
            // (c) After stage 3: the streets stay won.
            yield return WinStreet(g, p, 2);
            yield return CloseBeats(p);
            yield return WinStreet(g, p, 3);
            yield return CloseBeats(p);
            path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(5);
            p = g.Players.First();
            Assert.AreEqual(Finale.Stages + 1, Finale.CurrentStreet(g.World));
            Assert.GreaterOrEqual(g.Quests.Stage(WarmCity.Id), WarmCity.Flight);
            Assert.IsFalse(Spawned(Finale.StreetGroup(3)));
        }

        [Test]
        public void FinaleFilesAreGated()
        {
            var root = Application.dataPath;
            foreach (var name in new[] { "FinaleDb.AfterTheDoor.cs", "GameRoot.Finale.AfterTheDoor.cs", "GameRoot.Flight.AfterTheDoor.cs", "WorldDb.Finale.AfterTheDoor.cs", "MonstersDb.Finale.AfterTheDoor.cs" })
            {
                var files = Directory.GetFiles(root, name, SearchOption.AllDirectories);
                Assert.AreEqual(1, files.Length, name + " exists");
                StringAssert.Contains("MetHer", File.ReadAllText(files[0]), name + " is gated on meeting her");
                Assert.IsFalse(LoreTests.GuardScans(name));
            }
        }

        // ================================================================== Task 5: the rider and the flight

        /// <summary>An Act III character whose input presses the dice tray whenever a roll waits (and does nothing else).</summary>
        static PlayerSlot Roller(GameRoot g, string side, string cls = "fighter")
        {
            var input = new RollerInput();
            var p = g.DebugStart(input, cls, "human");
            input.PressWhen = () => p.Hud != null && p.Hud.Dice.IsOpen && Time.frameCount % 2 == 0;
            g.ApplyActThreeStart(p, side);
            p.Hud.CloseAll();
            p.Character.HeroicInspiration = false; // no reroll offers in the tray
            return p;
        }

        /// <summary>From the choice through all three streets: the dragon is coming down for a rider.</summary>
        static IEnumerator ToTheFlight(GameRoot g, PlayerSlot p, string ending)
        {
            yield return ToTheChoice(g, p);
            g.ChooseEnding(p, ending);
            yield return CloseAll(g);
            for (int n = 1; n <= Finale.Stages; n++) { yield return WinStreet(g, p, n); if (n < Finale.Stages) yield return CloseAll(g); }
        }

        /// <summary>Closes every player's beats (not choices) for a while.</summary>
        static IEnumerator CloseAll(GameRoot g, int rounds = 12)
        {
            for (int i = 0; i < rounds; i++)
            {
                yield return Frames(4);
                bool any = false;
                foreach (var q in g.Players.Where(x => x.IsReady)) { var b = Beat(q); if (b != null) { b.Close(); any = true; } }
                if (!any) { yield return Frames(4); if (g.Players.All(q => !q.IsReady || Beat(q) == null)) yield break; }
            }
        }

        /// <summary>Rides it out: closes beats, picks option `pick` on each pass, until the party has landed.</summary>
        static IEnumerator RideOut(GameRoot g, PlayerSlot rider, string ending, int pick, System.Collections.Generic.List<string> titles = null)
        {
            var route = FinaleDb.Route(ending, g.World);
            float start = Time.realtimeSinceStartup;
            while (!g.World.Flag(Finale.Flown) && Time.realtimeSinceStartup - start < 90f)
            {
                yield return Frames(3);
                foreach (var q in g.Players.Where(x => x.IsReady)) { var b = Beat(q); if (b != null) b.Close(); }
                if (rider.Hud.Top is ChoiceScreen cs && !cs.Closed)
                {
                    titles?.Add(cs.Title);
                    foreach (var pass in route.Passes) if (cs.Title == pass.Title) cs.Choose(pass.Options[pick].Label);
                }
            }
        }

        [UnityTest]
        public IEnumerator SoloRidesWithoutAPrompt()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return ToTheFlight(g, p, Turn.Seal);
            var titles = new System.Collections.Generic.List<string>();
            yield return RideOut(g, p, Turn.Seal, 0, titles);
            Assert.IsFalse(titles.Any(t => t.StartsWith("Who rides")), "solo: no prompt");
            Assert.IsTrue(g.World.Flag(Finale.RiderFlag(p.Index)), "the one player rides");
            Assert.IsTrue(g.World.Flag(Finale.Flown));
        }

        static string TrayState(PlayerSlot q)
        {
            var d = q.Hud.Dice; var t = d.GetType(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            string V(string f) { var fi = t.GetField(f, F); var v = fi?.GetValue(d); return v is UnityEngine.UI.Text tx ? tx.text : v?.ToString() ?? "null"; }
            return $"waiting={V("_waitingPress")} flow={V("_flowRunning")} prompt='{V("_prompt")}' choices={V("_choices")} confirm={q.Input.Confirm}";
        }

        static ChoiceScreen Choice(PlayerSlot p) => p.Hud.Top is ChoiceScreen c && !c.Closed ? c : null;

        [UnityTest]
        public IEnumerator CoopRiderPromptHandsOverInOnePress()
        {
            var g = GameRoot.I;
            var p1 = Roller(g, "academy");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(31), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return ToTheFlight(g, p1, Turn.Seal);
            yield return CloseAll(g);
            Assert.IsTrue(g.World.Flag(Finale.RiderPending));
            StringAssert.StartsWith("Who rides", Choice(p1)?.Title, "P1 is asked");
            StringAssert.StartsWith("Who rides", Choice(p2)?.Title, "and so is P2");
            var dragonAt = g.DragonAgent.Actor.transform.position;
            Choice(p1).Choose(p1.Character.Name); // the wrong player triggered it
            yield return Frames(3);
            StringAssert.StartsWith("Ride", Choice(p1)?.Title);
            Assert.IsTrue(Choice(p1).Choose("Let " + p2.Character.Name), "one press hands it over");
            yield return Frames(3);
            StringAssert.StartsWith("Ride", Choice(p2)?.Title, "now P2 is asked to confirm");
            Assert.IsFalse(g.Flying || g.World.Flag(Finale.Flown), "nothing has started");
            Assert.AreEqual(dragonAt, g.DragonAgent.Actor.transform.position, "the dragon hasn't moved");
            Choice(p2).Choose("Take the reins");
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Finale.RiderFlag(p2.Index)), "P2 rides");
            Assert.IsFalse(g.World.Flag(Finale.RiderFlag(p1.Index)));
            Assert.IsTrue(g.Flying);
        }

        [UnityTest]
        public IEnumerator RiderPromptSurvivesADrop()
        {
            var g = GameRoot.I;
            var p1 = Roller(g, "academy");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(32), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return ToTheFlight(g, p1, Turn.Seal);
            yield return CloseAll(g);
            Choice(p1).Choose(p2.Character.Name);
            yield return Frames(3);
            g.DropOut(p2);
            yield return Frames(5);
            Assert.IsTrue(g.World.Flag(Finale.RiderFlag(p1.Index)) || Choice(p1) != null, "the flight goes to whoever is left");
            Assert.IsTrue(g.World.Flag(Finale.RiderFlag(p1.Index)), "alone now: P1 rides");
        }

        /// <summary>Final review: the rider drops out in mid-air. The flight lands whoever is left, with no errors.</summary>
        [UnityTest]
        public IEnumerator RiderDropsMidFlight()
        {
            var g = GameRoot.I;
            var p1 = Roller(g, "academy");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(35), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return ToTheFlight(g, p1, Turn.Seal);
            yield return CloseAll(g);
            Choice(p1).Choose(p2.Character.Name);
            yield return Frames(3);
            Choice(p2).Choose("Take the reins");
            for (int i = 0; i < 40 && !g.Flying; i++) yield return Frames(2);
            Assert.IsTrue(g.Flying, "P2 is in the air");
            yield return new WaitForSecondsRealtime(0.5f);
            g.DropOut(p2);
            yield return RideOut(g, p1, Turn.Seal, 0);
            Assert.IsTrue(g.World.Flag(Finale.Flown), "the flight still lands");
            Assert.AreEqual("the_deep", p1.Zone.Def.Id, "P1 is at the landing");
            Assert.IsFalse(g.Flying);
        }

        [UnityTest]
        public IEnumerator EscOnThePromptNeverLosesTheFlight()
        {
            var g = GameRoot.I;
            var p1 = Roller(g, "academy");
            yield return Frames(2);
            var p2 = g.DebugStart(Still(33), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            yield return ToTheFlight(g, p1, Turn.Seal);
            yield return CloseAll(g);
            Choice(p1).Close();
            Choice(p2).Close();
            yield return Frames(3);
            Assert.IsTrue(g.World.Flag(Finale.RiderPending), "still waiting for a rider");
            Assert.IsFalse(g.Flying);
            StringAssert.StartsWith("Ride", g.DragonAgent.PromptText(p1));
            g.DragonAgent.Interact(p1);
            yield return Frames(2);
            StringAssert.StartsWith("Who rides", Choice(p1)?.Title, "the dragon asks again");
        }

        [UnityTest]
        public IEnumerator LoadAtRiderPromptReoffersIt()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return ToTheFlight(g, p, Turn.Save);
            string path = SaveSystem.Save(false); // after the streets, before the dragon came
            g.QuitToTitle();
            yield return Frames(2);
            g.LoadGame(path);
            yield return Frames(5);
            p = g.Players.First();
            var roller = new RollerInput(); var me = p; roller.PressWhen = () => me.Hud != null && me.Hud.Dice.IsOpen && Time.frameCount % 2 == 0;
            p.Input = roller; // a loaded game takes the default device: put the dice-presser back
            Assert.IsFalse(g.World.Flag(Finale.Flown));
            yield return RideOut(g, p, Turn.Save, 1);
            Assert.IsTrue(g.World.Flag(Finale.Flown), "the dragon came back down, and you flew");
        }

        [UnityTest]
        public IEnumerator FlightRollsAreVisibleAndCount()
        {
            var g = GameRoot.I;
            var p = Roller(g, "hunters");
            yield return Frames(2);
            yield return ToTheFlight(g, p, Turn.End);
            Dice.Rng = new ScriptedRandom(Enumerable.Repeat(20, 400).ToArray());
            bool sawTray = false;
            var route = FinaleDb.Route(Turn.End, g.World);
            float start = Time.realtimeSinceStartup;
            while (!g.World.Flag(Finale.Flown) && Time.realtimeSinceStartup - start < 90f)
            {
                yield return Frames(2);
                sawTray |= p.Hud.Dice.IsOpen;
                var b = Beat(p); if (b != null) b.Close();
                if (Choice(p) is ChoiceScreen cs) foreach (var pass in route.Passes) if (cs.Title == pass.Title) cs.Choose(pass.Options[0].Label);
            }
            Assert.IsTrue(sawTray, "the rider's rolls go through the dice tray");
            CollectionAssert.AreEquivalent(new[] { route.Passes[0].Options[0].OnSuccess, route.Passes[1].Options[0].OnSuccess }, GameRoot.LastFlightWon, "what the flight won reached the boss fight");
        }

        [UnityTest]
        public IEnumerator FailedPassesScorchButNeverKill()
        {
            var g = GameRoot.I;
            var p = Roller(g, "hunters");
            yield return Frames(2);
            yield return ToTheFlight(g, p, Turn.End);
            p.Character.HP = 4;
            Dice.Rng = new ScriptedRandom(Enumerable.Repeat(1, 400).ToArray());
            yield return RideOut(g, p, Turn.End, 1);
            Assert.AreEqual(0, GameRoot.LastFlightWon.Count, "nothing won");
            Assert.AreEqual(1, p.Character.HP, "scorched down to 1 HP, never 0");
        }

        [UnityTest]
        public IEnumerator FlightLandsEveryone()
        {
            var g = GameRoot.I;
            foreach (var (ending, side, coop) in new[] { (Turn.End, "hunters", false), (Turn.Seal, "academy", true), (Turn.Save, "fae", false) })
            {
                var p1 = Roller(g, side);
                yield return Frames(2);
                PlayerSlot p2 = null;
                if (coop) { p2 = g.DebugStart(Still(34), "cleric", "dwarf"); yield return Frames(2); p2.Hud.CloseAll(); }
                yield return ToTheFlight(g, p1, ending);
                yield return CloseAll(g);
                if (coop) { Choice(p1).Choose(p1.Character.Name); yield return Frames(3); Choice(p1).Choose("Take the reins"); }
                yield return RideOut(g, p1, ending, 0);
                var route = FinaleDb.Route(ending, g.World);
                Assert.IsTrue(g.World.Flag(Finale.Flown), ending + ": landed");
                Assert.AreEqual(WarmCity.Boss, g.Quests.Stage(WarmCity.Id), ending);
                foreach (var q in g.Players.Where(x => x.IsReady))
                {
                    Assert.AreEqual(route.LandingZone, q.Zone.Def.Id, $"{ending}: {q.Character.Name} is at the landing");
                    Assert.LessOrEqual(Vector2.Distance(q.MapPos, route.LandingSpot), 3f, ending);
                    Assert.IsTrue(q.Avatar.enabled, ending + ": control is back");
                }
                Assert.AreEqual(route.LandingZone, g.DragonAgent.Actor.Zone.Def.Id, ending + ": the dragon landed too");
                Assert.LessOrEqual(Vector2.Distance(g.DragonAgent.Actor.MapPos, route.LandingSpot), 3f, ending);
                Assert.IsFalse(g.DragonAgent.Waiting, ending + ": the dragon is back with you");
                g.QuitToTitle();
                yield return Frames(3);
            }
        }

        // ================================================================== Task 6: the three boss fights

        static readonly System.Collections.Generic.Dictionary<string, string> BossOf = new System.Collections.Generic.Dictionary<string, string>
        { [Turn.End] = "ashen_queen", [Turn.Seal] = "king_regalia", [Turn.Save] = "crowned_fire" };

        /// <summary>Straight to the landing (the flight is Task 5's): flown, standing at the landing, the boss fight paused at initiative.</summary>
        static IEnumerator AtTheBoss(GameRoot g, PlayerSlot p, string ending, params FlightEffect[] won)
        {
            yield return ToTheChoice(g, p);
            g.ChooseEnding(p, ending);
            yield return CloseAll(g);
            foreach (var f in new[] { Finale.Started, Finale.StreetDone(1), Finale.StreetDone(2), Finale.StreetDone(3), Finale.Flown }) g.World.SetFlag(f);
            g.World.ClearFlag(Finale.StreetOpen(1)); // the streets are won (their groups leave on the next warp)
            g.World.ClearFlag(Finale.DragonWaits);
            g.Quests.Set(WarmCity.Id, WarmCity.Boss, p, 0f);
            var route = FinaleDb.Route(ending, g.World);
            if (p.Input is RollerInput r) r.PressWhen = () => false; // the fight waits at initiative: the test drives it
            g.GatherAt(route.LandingZone, route.LandingSpot);
            yield return Frames(2);
            GameRoot.FlightResults.Clear();
            GameRoot.FlightResults.AddRange(won);
            g.StartFinaleBoss(ending);
            yield return Frames(3);
        }

        static CombatInstance BossFightOf(GameRoot g, string ending)
        {
            var c = g.Combats.SingleOrDefault(x => x.GroupId == Finale.BossGroup(ending) && !x.Finished);
            if (c != null) foreach (var u in c.Units) if (u.Cb == null) u.Cb = new Combatant(u.C);
            return c;
        }

        static Unit BossUnit(CombatInstance c, string ending) => c.Units.Single(u => u.C is Monster m && m.Def.Id == BossOf[ending]);

        /// <summary>Hits the boss through every phase (closing the beats), then knocks it down.</summary>
        static IEnumerator BeatTheBoss(GameRoot g, PlayerSlot p, CombatInstance c, string ending)
        {
            var boss = BossUnit(c, ending);
            for (int i = 0; i < 6 && !boss.C.IsDown; i++)
            {
                c.DebugDamage(boss, 99999);
                yield return Frames(2);
                for (int k = 0; k < 8 && c.BeatsPlaying; k++) { var b = Beat(p); if (b != null) b.Close(); yield return Frames(3); }
            }
            foreach (var f in c.Units.Where(u => u.C.Team == Team.Enemies && !u.C.IsDown).ToList()) c.DebugDamage(f, 99999);
            c.DebugEndCheck();
            yield return Frames(3);
        }

        [UnityTest]
        public IEnumerator EachBossStartsWhereItLands()
        {
            var g = GameRoot.I;
            foreach (var (ending, side) in new[] { (Turn.End, "hunters"), (Turn.Seal, "academy"), (Turn.Save, "fae") })
            {
                var p = Roller(g, side);
                yield return Frames(2);
                yield return AtTheBoss(g, p, ending);
                var c = BossFightOf(g, ending);
                Assert.IsNotNull(c, ending + ": the last fight has begun");
                Assert.AreEqual(FinaleDb.Route(ending, g.World).LandingZone, c.Zone.Def.Id, ending);
                Assert.IsNotNull(BossUnit(c, ending), ending + ": the boss is in it");
                if (ending == Turn.Seal) Assert.AreEqual(2, c.Units.Count(u => u.C is Monster m && m.Def.Id == "crown_knight" && u.C.Team == Team.Enemies), "the Ring-knights");
                Assert.AreEqual(FinaleDb.Allies(ending, g.World).Count, c.Units.Count(u => u.Ally), ending + ": whoever stands with you is here");
                g.QuitToTitle();
                yield return Frames(3);
            }
        }

        [UnityTest]
        public IEnumerator FlightEffectsApply()
        {
            var g = GameRoot.I;
            var p = Roller(g, "fae");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.Save, FlightEffect.BossWounded, FlightEffect.NoLegendaryFirstRound, FlightEffect.AddsScattered, FlightEffect.PartyInspired);
            var c = BossFightOf(g, Turn.Save);
            var m = (Monster)BossUnit(c, Turn.Save).C;
            Assert.AreEqual(m.MaxHP - m.MaxHP / 10, m.HP, "it starts 10% down");
            Assert.AreEqual(0, m.LegendaryLeft, "no legendary actions in the first round");
            Assert.IsFalse(m.IsCharged(m.Def.Abilities[0]), "its breath starts spent");
            Assert.IsTrue(p.Character.HeroicInspiration, "the party is inspired");
            Assert.AreEqual(0, GameRoot.FlightResults.Count, "the results are used up");
        }

        [UnityTest]
        public IEnumerator AddsScatteredOnTheSealMeansOneFewerKnight()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.Seal, FlightEffect.AddsScattered);
            var c = BossFightOf(g, Turn.Seal);
            Assert.AreEqual(1, c.Units.Count(u => u.C is Monster m && m.Def.Id == "crown_knight" && u.C.Team == Team.Enemies));
        }

        [UnityTest]
        public IEnumerator SealHoldsForSevenRounds()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.Seal);
            var c = BossFightOf(g, Turn.Seal);
            Assert.AreEqual(0, g.SealProgress);
            Assert.IsTrue(Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Any(a => a.name == "Ashby" && a.Zone == c.Zone), "Ashby is at the door");
            int finishedAt = -1;
            for (int r = 2; r <= 20 && !c.Finished; r++)
            {
                g.OnCombatRound(c, r);
                yield return Frames(2);
                if (c.Finished) { finishedAt = r; break; }
                if (r == 3)
                {
                    // The King reaches his phase: he hauls on the chains and the seal slips back one.
                    var king = BossUnit(c, Turn.Seal);
                    c.DebugDamage(king, 99999);
                    yield return Frames(2);
                    while (c.BeatsPlaying) { var b = Beat(p); if (b != null) b.Close(); yield return Frames(3); }
                    Assert.AreEqual(1, g.SealProgress, "the chains pull it back");
                }
            }
            // Seven rounds of progress from round 2's start, plus the one the chains cost.
            Assert.AreEqual(Finale.SealRounds + 2, finishedAt, "the seal closes a round late");
            Assert.AreEqual(Finale.SealRounds, g.SealProgress);
            Assert.IsTrue(g.World.Flag(Finale.Won));
        }

        [UnityTest]
        public IEnumerator BeatingTheKingAlsoSeals()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.Seal);
            var c = BossFightOf(g, Turn.Seal);
            yield return BeatTheBoss(g, p, c, Turn.Seal);
            Assert.IsTrue(c.Finished);
            Assert.IsTrue(g.World.Flag(Finale.Won));
        }

        [UnityTest]
        public IEnumerator TheWinIsSavedAndAnnounced()
        {
            var g = GameRoot.I;
            var p = Roller(g, "hunters");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.End);
            var c = BossFightOf(g, Turn.End);
            yield return BeatTheBoss(g, p, c, Turn.End);
            Assert.IsTrue(g.World.Flag(Finale.Won));
            Assert.AreEqual(WarmCity.Won, g.Quests.Stage(WarmCity.Id));
            Assert.IsFalse(g.Quests.Active(WarmCity.Id), "the quest is complete");
            MessageScreen card = null;
            var seen = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 40 && card == null; i++)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                var b = Beat(p);
                if (b == null) continue;
                if (b.Title == "To Be Continued") card = b; else { seen.Add(b.Body); b.Close(); }
            }
            Assert.IsNotNull(card, "the placeholder card for Phase 7");
            CollectionAssert.IsSubsetOf(FinaleDb.Victory(Turn.End, g.World), seen, "the victory beats played first");
        }

        [UnityTest]
        public IEnumerator BossWipeRetriesAtTheLanding()
        {
            var g = GameRoot.I;
            var p = Roller(g, "academy");
            yield return Frames(2);
            yield return AtTheBoss(g, p, Turn.Seal);
            var c = BossFightOf(g, Turn.Seal);
            g.OnCombatRound(c, 2);
            Assert.AreEqual(1, g.SealProgress);
            foreach (var u in c.Units.Where(u => u.C.Team == Team.Players && !u.Ally)) u.C.HP = 0;
            c.DebugEndCheck();
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Frames(3);
            var route = FinaleDb.Route(Turn.Seal, g.World);
            Assert.AreEqual(route.LandingZone, p.Zone.Def.Id, "you come to in the Deep");
            Assert.LessOrEqual(Vector2.Distance(p.MapPos, route.LandingSpot), 3f);
            Assert.IsTrue(g.World.Flag(Finale.Flown), "no second flight");
            Assert.AreEqual(0, g.SealProgress, "Ashby starts the seal again");
            Assert.IsTrue(Spawned(Finale.BossGroup(Turn.Seal)), "the King is waiting");
            var king = Object.FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None).First(a => a.Monster.Def.Id == "king_regalia");
            Assert.AreEqual(king.Monster.MaxHP, king.Monster.HP, "at full strength");
        }

        // ================================================================== Task 7: golden paths and screenshots

        /// <summary>
        /// From the choice to the To Be Continued card: the three streets, the rider, both passes, the landing, every phase beat
        /// and the victory. Collects every beat's text in order. No window may be left open between stages.
        /// </summary>
        IEnumerator GoldenPath(string ending, string side, bool coop, bool p2Rides)
        {
            var g = GameRoot.I;
            var p1 = Roller(g, side);
            yield return Frames(2);
            PlayerSlot p2 = null;
            if (coop) { p2 = g.DebugStart(Still(40), "cleric", "dwarf"); yield return Frames(2); p2.Hud.CloseAll(); }
            ((RollerInput)p1.Input).PressWhen = () => p1.Hud != null && p1.Hud.Dice.IsOpen && Time.frameCount % 2 == 0 && !g.World.Flag(Finale.Flown); // the boss fight waits at initiative
            var beats = new System.Collections.Generic.List<string>();
            yield return ToTheChoice(g, p1);
            g.ChooseEnding(p1, ending);
            for (int n = 1; n <= Finale.Stages; n++)
            {
                for (int i = 0; i < 16; i++) { yield return Frames(4); var b = Beat(p1); if (b != null) { beats.Add(b.Body); b.Close(); } else if (i > 3) break; }
                // A roll still tumbling in the tray (a fight's initiative) holds closed screens until it's done.
                for (float t0 = Time.realtimeSinceStartup; g.Players.Any(x => x.IsReady && x.Hud.Dice.IsOpen) && Time.realtimeSinceStartup - t0 < 6f;) yield return null;
                yield return Frames(3);
                for (int i = 0; i < 8; i++) { var b = Beat(p1); if (b == null) break; beats.Add(b.Body); b.Close(); yield return Frames(4); }
                foreach (var q in g.Players.Where(x => x.IsReady)) Assert.IsNull(q.Hud.Top, $"{ending}: nothing left open before stage {n}: top={q.Hud.Top?.GetType().Name} title='{(q.Hud.Top as MessageScreen)?.Title}' body='{(q.Hud.Top as MessageScreen)?.Body}' closed={q.Hud.Top?.Closed} dice={q.Hud.Dice.IsOpen} tray={TrayState(q)} combats={g.Combats.Count}");
                yield return WinStreet(g, p1, n);
            }
            // The takeoff beats, then the rider.
            for (int i = 0; i < 16 && Choice(p1) == null && !g.Flying; i++) { yield return Frames(4); var b = Beat(p1); if (b != null) { beats.Add(b.Body); b.Close(); } }
            var rider = p1;
            if (coop)
            {
                StringAssert.StartsWith("Who rides", Choice(p1)?.Title);
                rider = p2Rides ? p2 : p1;
                Choice(p1).Choose(rider.Character.Name);
                yield return Frames(3);
                Choice(rider).Choose("Take the reins");
                if (rider == p2) { var roller = new RollerInput(); roller.PressWhen = () => p2.Hud != null && p2.Hud.Dice.IsOpen && Time.frameCount % 2 == 0 && !g.World.Flag(Finale.Flown); p2.Input = roller; }
            }
            var route = FinaleDb.Route(ending, g.World);
            float start = Time.realtimeSinceStartup;
            while (!g.World.Flag(Finale.Flown) && Time.realtimeSinceStartup - start < 90f)
            {
                yield return Frames(3);
                foreach (var q in g.Players.Where(x => x.IsReady)) { var b = Beat(q); if (b != null) { if (q == rider) beats.Add(b.Body); b.Close(); } }
                if (Choice(rider) is ChoiceScreen cs) foreach (var pass in route.Passes) if (cs.Title == pass.Title) cs.Choose(pass.Options[1].Label);
            }
            Assert.IsTrue(g.World.Flag(Finale.RiderFlag(rider.Index)), ending + ": the chosen player rode");
            yield return Frames(3);
            var c = BossFightOf(g, ending);
            Assert.IsNotNull(c, ending + ": the last fight began on landing");
            var boss = BossUnit(c, ending);
            var def = ((Monster)boss.C).Def;
            for (int i = 0; i < 6 && !boss.C.IsDown; i++)
            {
                c.DebugDamage(boss, 99999);
                yield return Frames(2);
                for (int k = 0; k < 10 && c.BeatsPlaying; k++) { var b = Beat(p1); if (b != null) { beats.Add(b.Body); b.Close(); } yield return Frames(3); }
            }
            foreach (var f in c.Units.Where(u => u.C.Team == Team.Enemies && !u.C.IsDown).ToList()) c.DebugDamage(f, 99999);
            c.DebugEndCheck();
            MessageScreen card = null;
            for (int i = 0; i < 60 && card == null; i++)
            {
                yield return new WaitForSecondsRealtime(0.15f);
                var b = Beat(p1);
                if (b == null) continue;
                if (b.Title == "To Be Continued") card = b; else { beats.Add(b.Body); b.Close(); }
            }
            Assert.IsNotNull(card, ending + ": the placeholder card");

            // Every beat, in order: the break, the three stage intros, the takeoff, the landing, each phase, the victory.
            var expected = new System.Collections.Generic.List<string>();
            expected.AddRange(GameRoot.BreakBeats(ending, g.World));
            for (int n = 1; n <= Finale.Stages; n++) expected.AddRange(FinaleDb.StreetIntro(n, ending, g.World));
            expected.AddRange(route.Takeoff);
            expected.AddRange(route.Landing);
            foreach (var ph in def.Phases) expected.AddRange(ph.Beats);
            expected.AddRange(FinaleDb.Victory(ending, g.World));
            int at = 0;
            foreach (var e in expected)
            {
                int found = beats.IndexOf(e, at);
                Assert.GreaterOrEqual(found, 0, $"{ending}: missing or out of order: \"{e.Substring(0, System.Math.Min(60, e.Length))}...\"");
                at = found + 1;
            }
            Assert.AreEqual(WarmCity.Won, g.Quests.Stage(WarmCity.Id));
            Assert.IsTrue(g.World.Flag(Finale.Won));
        }

        [UnityTest, Timeout(400000)] public IEnumerator FinaleGoldenPathEnd() => GoldenPath(Turn.End, "hunters", false, false);
        [UnityTest, Timeout(400000)] public IEnumerator FinaleGoldenPathSeal() => GoldenPath(Turn.Seal, "academy", false, false);
        [UnityTest, Timeout(400000)] public IEnumerator FinaleGoldenPathSave() => GoldenPath(Turn.Save, "fae", false, false);
        [UnityTest, Timeout(400000)] public IEnumerator FinaleGoldenPathCoopSave() => GoldenPath(Turn.Save, "fae", true, true);

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureFinale()
        {
            var g = GameRoot.I;
            foreach (var (ending, side) in new[] { (Turn.End, "hunters"), (Turn.Seal, "academy"), (Turn.Save, "fae") })
            {
                var p1 = Roller(g, side, "wizard");
                yield return Frames(2);
                PlayerSlot p2 = null;
                if (ending == Turn.Save) { p2 = g.DebugStart(Still(41), "cleric", "dwarf"); yield return Frames(2); p2.Hud.CloseAll(); }
                ((RollerInput)p1.Input).PressWhen = () => p1.Hud != null && p1.Hud.Dice.IsOpen && Time.frameCount % 2 == 0 && !g.World.Flag(Finale.Flown);
                g.Clock.Minutes = 12 * 60;
                yield return ToTheChoice(g, p1);
                g.ChooseEnding(p1, ending);
                for (int n = 1; n <= Finale.Stages; n++)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        yield return Frames(6);
                        var b = Beat(p1);
                        if (b == null) { if (i > 3) break; continue; }
                        if (b.Title == FinaleDb.StreetTitle(n)) { AcademyTests.Shot($"act6_{ending}_street{n}_intro"); }
                        b.Close();
                    }
                    if (n == 1 || ending != Turn.Save)
                    {
                        p1.Hud.CloseAll(); p1.Hud.HideNarration();
                        g.StartGroupCombat(Finale.StreetGroup(n), p1);
                        yield return Frames(30);
                        AcademyTests.Shot($"act6_{ending}_street{n}_fight");
                    }
                    yield return WinStreet(g, p1, n);
                }
                for (int i = 0; i < 16 && Choice(p1) == null && !g.Flying; i++)
                {
                    yield return Frames(6);
                    var b = Beat(p1);
                    if (b != null) { if (i < 2) AcademyTests.Shot($"act6_{ending}_takeoff{i}"); b.Close(); }
                }
                if (p2 != null)
                {
                    yield return Frames(5);
                    AcademyTests.Shot($"act6_{ending}_who_rides");
                    Choice(p1).Choose(p1.Character.Name);
                    yield return Frames(5);
                    AcademyTests.Shot($"act6_{ending}_ride_confirm");
                    Choice(p1).Choose("Take the reins");
                }
                var route = FinaleDb.Route(ending, g.World);
                float start = Time.realtimeSinceStartup;
                bool midShot = false;
                int pass = 0;
                while (!g.World.Flag(Finale.Flown) && Time.realtimeSinceStartup - start < 90f)
                {
                    yield return Frames(3);
                    if (!midShot && g.Flying && Time.realtimeSinceStartup - start > 1.2f) { AcademyTests.Shot($"act6_{ending}_flight"); midShot = true; }
                    foreach (var q in g.Players.Where(x => x.IsReady)) { var b = Beat(q); if (b != null) { AcademyTests.Shot($"act6_{ending}_landing_{q.Index}"); b.Close(); } }
                    if (Choice(p1) is ChoiceScreen cs)
                        foreach (var ps in route.Passes)
                            if (cs.Title == ps.Title) { yield return Frames(4); AcademyTests.Shot($"act6_{ending}_pass{pass++}"); cs.Choose(ps.Options[0].Label); }
                }
                yield return Frames(30);
                p1.Hud.HideNarration();
                AcademyTests.Shot($"act6_{ending}_boss");
                // Roll initiative so the tray closes, and look at the boss on the field (the party can't fall for this shot).
                foreach (var q in g.Players.Where(x => x.IsReady)) q.Character.TempHP = 999;
                ((RollerInput)p1.Input).PressWhen = () => p1.Hud != null && p1.Hud.Dice.IsOpen && Time.frameCount % 2 == 0;
                for (float t0 = Time.realtimeSinceStartup; Time.realtimeSinceStartup - t0 < 4f;) { if (p2 != null) { var b2 = Beat(p2); if (b2 != null) b2.Close(); } yield return null; }
                p1.Hud.HideNarration();
                AcademyTests.Shot($"act6_{ending}_boss_field");
                var c = BossFightOf(g, ending);
                var boss = BossUnit(c, ending);
                // The fight is live now: wait for a player's turn (it waits for input) so no attack is mid-swing when we force the end.
                for (float t0 = Time.realtimeSinceStartup; !(c.Current != null && c.Current.IsHuman) && Time.realtimeSinceStartup - t0 < 20f;) yield return null;
                c.DebugDamage(boss, 99999);
                yield return Frames(8);
                AcademyTests.Shot($"act6_{ending}_phase1");
                while (c.BeatsPlaying) { Beat(p1)?.Close(); yield return Frames(4); }
                if (ending == Turn.Seal) { g.OnCombatRound(c, 2); yield return Frames(5); AcademyTests.Shot("act6_seal_progress"); }
                for (int i = 0; i < 6 && !boss.C.IsDown; i++)
                {
                    c.DebugDamage(boss, 99999);
                    yield return Frames(2);
                    while (c.BeatsPlaying) { Beat(p1)?.Close(); yield return Frames(4); }
                }
                foreach (var f in c.Units.Where(u => u.C.Team == Team.Enemies && !u.C.IsDown).ToList()) c.DebugDamage(f, 99999);
                c.DebugEndCheck();
                for (int i = 0; i < 60; i++)
                {
                    yield return new WaitForSecondsRealtime(0.15f);
                    var b = Beat(p1);
                    if (b == null) continue;
                    if (b.Title == "To Be Continued") { AcademyTests.Shot($"act6_{ending}_tbc"); break; }
                    AcademyTests.Shot($"act6_{ending}_victory{i}");
                    b.Close();
                }
                g.QuitToTitle();
                yield return Frames(3);
            }
        }
    }
}
