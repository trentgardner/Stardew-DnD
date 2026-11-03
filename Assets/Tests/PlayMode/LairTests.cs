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
    /// <summary>Act III Phase 4: below Aurelhold, down to the door.</summary>
    public class LairTests
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
        static PlayerSlot ActThree(GameRoot g, int seed, string cls = "fighter", string side = "academy") => CapitalTests.ActThree(g, seed, cls, side);
        static DCtx Ctx(PlayerSlot p) => CapitalTests.Ctx(p);
        static SecretInteractable SecretIn(string id) => CapitalTests.SecretIn(id);
        static int Count(PlayerSlot p, string text) => p.Hud.LogLines.Count(l => l.Contains(text));

        /// <summary>Steps the avatar onto a warp tile (from the tile beside it), as a player walking would.</summary>
        static IEnumerator StepOnto(GameRoot g, PlayerSlot p, string zone, Vector2Int at, Vector2Int from)
        {
            if (p.Zone.Def.Id != zone) { g.Warp(p, zone, from, false); yield return Frames(3); }
            p.Avatar.PlaceAt(p.Zone, from);
            yield return Frames(2);
            p.Avatar.PlaceAt(p.Zone, at);
            yield return Frames(3);
        }

        // ============================================================ Task 1: engine bits and the Undergate

        [UnityTest]
        public IEnumerator SilentZonePlaysNoMusic()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 1);
            yield return Frames(2);
            var roots = WorldDb.Zones["forest"];
            try
            {
                g.Warp(p, "forest", new Vector2Int(34, 8), false);
                yield return Frames(3);
                Assert.AreEqual(roots.MusicId, g.WantedMusic(), "a normal zone has its music");
                roots.Silent = true;
                Assert.IsNull(g.WantedMusic(), "a silent zone has none");
                g.StartGroupCombat("forest_wolves", p);
                yield return Frames(2);
                Assert.IsTrue(g.AnyInCombat);
                Assert.IsNull(g.WantedMusic(), "not even fight music");
            }
            finally { roots.Silent = false; }
        }

        [UnityTest]
        public IEnumerator UndergateOpensOnlyWhenEverythingPointsDown()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 2);
            yield return Frames(2);
            var gate = new Vector2Int(52, 20);
            var roots = WorldDb.Zones["roots"];
            Assert.IsNotNull(roots.Warps.Find(w => w.At == gate), "there is a way through");
            yield return StepOnto(g, p, "roots", gate, gate + Vector2Int.left);
            Assert.AreEqual("roots", p.Zone.Def.Id, "the Undergate is shut at the start of Act III");
            Assert.GreaterOrEqual(Count(p, "King's own seal"), 1, "and it says why");
            Assert.IsFalse(SecretIn("undergate_bar").Available(p), "nobody heaves on the King's gate for no reason");

            g.Quests.Set(WarmCity.Id, WarmCity.Below, p);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(4);
            p = g.Players[0];
            g.Warp(p, "roots", new Vector2Int(50, 20), false);
            yield return Frames(3);
            var bar = SecretIn("undergate_bar");
            Assert.IsTrue(bar.Available(p), "once everything points down, the bar can be heaved");
            int xp = p.Character.XP;
            bar.Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(WarmCity.Opened, g.Quests.Stage(WarmCity.Id));
            Assert.IsTrue(g.World.Flag(WorldDb.UndergateOpen));
            Assert.AreEqual(xp + 2000, p.Character.XP);
            p.Hud.CloseAll();
            yield return StepOnto(g, p, "roots", gate, gate + Vector2Int.left);
            Assert.AreEqual("undergate", p.Zone.Def.Id, "the Undergate leads down");
            var back = WorldDb.Zones["undergate"].Warps.Find(w => w.ToZone == "roots");
            Assert.IsNotNull(back);
            yield return StepOnto(g, p, "undergate", back.At, back.At + Vector2Int.right);
            Assert.AreEqual("roots", p.Zone.Def.Id, "and back up");
            g.QuitToTitle();
            yield return Frames(2);

            // Crale's key opens it the same way.
            p = ActThree(g, 3);
            yield return Frames(2);
            g.Quests.Start(DeepShift.Id, p);
            g.Quests.Set(DeepShift.Id, DeepShift.Below, p);
            g.Quests.Set(WarmCity.Id, WarmCity.Below, p);
            var crale = DialogueDb.Get("crale");
            var start = crale.Nodes[crale.Start(Ctx(p))];
            var open = start.Options.FirstOrDefault(o => o.Text.Contains("Open the Undergate"));
            Assert.IsNotNull(open, "Crale has the key");
            Assert.IsTrue(open.Show(Ctx(p)));
            var keyNode = crale.Nodes[open.Next];
            keyNode.OnEnter?.Invoke(Ctx(p));
            foreach (var o in keyNode.Options) o.Do?.Invoke(Ctx(p));
            Assert.AreEqual(WarmCity.Opened, g.Quests.Stage(WarmCity.Id), "Crale opens it");
            Assert.IsTrue(g.World.Flag(WorldDb.UndergateOpen));
        }

        [Test]
        public void WarmCityStagesPayToLevelTwelve()
        {
            var q = QuestDb.Get(WarmCity.Id);
            int sum = q.StageXp.Values.Sum();
            Assert.GreaterOrEqual(PlayerCharacter.XpThresholds[11] + sum, PlayerCharacter.XpThresholds[12], "the main quest alone reaches level 12 by the door");
            Assert.AreEqual(2000, q.StageXp[WarmCity.Opened]);
            Assert.AreEqual(4000, q.StageXp[WarmCity.Deep]);
            Assert.AreEqual(6000, q.StageXp[WarmCity.Door]);
        }

        // ============================================================ Task 2: the Undergate Works, the Keepers, Bran

        static DOption Opt(string dialogue, string node, Skill skill) => CapitalTests.Opt(dialogue, node, skill);

        /// <summary>An Act III game with the Undergate open and the party standing at the foot of its stair.</summary>
        static IEnumerator Below(GameRoot g, PlayerSlot p)
        {
            g.Quests.Set(WarmCity.Id, WarmCity.Opened, p, 0f);
            g.Warp(p, "undergate", WorldDb.Zones["undergate"].DefaultSpawn, false);
            yield return Frames(3);
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator UndergateWorksReachable()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 10);
            yield return Frames(2);
            yield return Below(g, p);
            var z = p.Zone;
            AcademyTests.AssertAllReachable(g, z, z.Def.DefaultSpawn);
            Assert.IsTrue(AcademyTests.CanReachBeside(z, z.Def.DefaultSpawn, new Vector2Int(42, 14)), "the stair down");
            foreach (var id in new[] { "bran", "hap_mossy", "deep_shift_1", "deep_shift_2" })
                foreach (int h in new[] { 3, 10, 21 })
                    foreach (int day in new[] { 0, 6 })
                    {
                        var at = WorldDb.Current(WorldDb.Npcs[id], h, day, g.World);
                        if (at?.Zone != "undergate") continue;
                        Assert.IsTrue(AcademyTests.CanReachBeside(z, z.Def.DefaultSpawn, at.Pos), $"{id} at {h}:00 day {day}");
                    }
        }

        [UnityTest]
        public IEnumerator KeepersTalkOrFight()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 11);
            yield return Frames(2);
            yield return Below(g, p);
            var keepers = WorldDb.Zones["undergate"].EnemyGroups.Single(x => x.Id == "undergate_keepers");
            Assert.AreEqual("banner_crown", keepers.VouchFlag);
            int fightXp = keepers.Members.Sum(m => MonstersDb.Get(m.MonsterId).XP);
            int xp = p.Character.XP;
            g.PacifyGroup("undergate_keepers", p);
            Assert.AreEqual(xp + fightXp, p.Character.XP, "talking them round pays the same as winning");
            Assert.IsTrue(g.World.Flag("keepers_resolved"));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(4);
            Assert.IsTrue(g.World.Flag("keepers_resolved"), "they stay stood aside");
            Assert.IsTrue(g.World.PacifiedGroups.Contains("undergate_keepers"));
        }

        [UnityTest]
        public IEnumerator BranComesHomeEveryWay()
        {
            var g = GameRoot.I;
            for (int way = 0; way < 3; way++)
            {
                var p = ActThree(g, 12 + way);
                yield return Frames(2);
                yield return Below(g, p);
                var d = DialogueDb.Get("bran");
                g.Quests.Start(DeepShift.Id, p);
                g.Quests.Set(DeepShift.Id, DeepShift.Below, p, 0f);
                Assert.AreEqual("start", d.Start(Ctx(p)));
                int xp = p.Character.XP;
                if (way == 0) Opt("bran", "start", Skill.Persuasion).OnSuccess(Ctx(p));
                if (way == 1)
                {
                    Opt("bran", "start", Skill.Insight).OnSuccess?.Invoke(Ctx(p));
                    var home = d.Nodes["homesick"].Options.First(o => o.Text.Contains("marry the money"));
                    home.Do(Ctx(p));
                }
                if (way == 2)
                {
                    g.World.SetFlag("keepers_resolved");
                    var free = d.Nodes["start"].Options.First(o => o.Text.Contains("no one left to stop you"));
                    Assert.IsTrue(free.Show(Ctx(p)));
                    free.Do(Ctx(p));
                }
                Assert.AreEqual(DeepShift.Home, g.Quests.Stage(DeepShift.Id), $"way {way}");
                Assert.AreEqual(xp + 3000, p.Character.XP, $"way {way} pays");
                Assert.IsTrue(g.World.Flag("bran_home"));
                var at = WorldDb.Current(WorldDb.Npcs["bran"], 10, 0, g.World);
                Assert.AreEqual("roots", at.Zone, "Bran is home in the Roots");
                Assert.AreEqual(new Vector2Int(10, 6), at.Pos);
                var mags = DialogueDb.Get("mags");
                Assert.AreEqual("home", mags.Start(Ctx(p)), "and Mags knows it");
                g.QuitToTitle();
                yield return Frames(2);
            }
            // Before the deep shift is known about, Bran's options never show.
            var q = ActThree(g, 20);
            yield return Frames(2);
            yield return Below(g, q);
            var bran = DialogueDb.Get("bran");
            Assert.AreEqual("stranger", bran.Start(Ctx(q)));
            Assert.IsFalse(bran.Nodes["stranger"].Options.Any(o => o.Check != null), "no rolls for a stranger");
        }

        // ============================================================ Task 3: the Iron Vaults, the Royal Crypts, the Third Ideal

        static PuzzlePieceObject PieceIn(Zone z, string id) => Object.FindObjectsByType<PuzzlePieceObject>(FindObjectsSortMode.None).Single(x => x.Room.Zone == z && x.Piece.Id == id);

        static IEnumerator Into(GameRoot g, PlayerSlot p, string zone, Vector2Int? at = null)
        {
            g.Warp(p, zone, at ?? WorldDb.Zones[zone].DefaultSpawn, false);
            yield return Frames(3);
            p.Hud.CloseAll();
        }

        [UnityTest]
        public IEnumerator VaultBeamOpensTheGate()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 30);
            yield return Frames(2);
            g.Quests.Set(WarmCity.Id, WarmCity.Opened, p, 0f);
            yield return Into(g, p, "iron_vaults");
            var z = p.Zone;
            var room = z.GetComponent<PuzzleRoom>();
            Assert.IsFalse(room.IsOn("crystal"), "dark to begin with");
            Assert.IsFalse(z.IsWalkable(new Vector2Int(33, 13)), "the far door is shut");
            g.World.SetFlag("puzzlehint:iron_vaults:mirror_1");

            // Half-turned, then leave: it all goes back.
            PieceIn(z, "mirror_1").Interact(p);
            Assert.AreEqual(1, room.DirOf("mirror_1"));
            yield return Into(g, p, "undergate");
            yield return Into(g, p, "iron_vaults");
            z = p.Zone; room = z.GetComponent<PuzzleRoom>();
            Assert.AreEqual(0, room.DirOf("mirror_1"), "an unfinished puzzle resets when you leave");

            foreach (var m in new[] { "mirror_1", "mirror_2", "mirror_3" }) PieceIn(z, m).Interact(p);
            Assert.IsTrue(room.IsOn("crystal"), "the beam reaches the crystal");
            Assert.IsTrue(z.IsWalkable(new Vector2Int(33, 13)), "and the far door opens");
            Assert.IsTrue(g.World.Flag("puzzle:iron_vaults:vault_gate"), "for good");

            yield return Into(g, p, "undergate");
            yield return Into(g, p, "iron_vaults");
            Assert.IsTrue(p.Zone.IsWalkable(new Vector2Int(33, 13)), "still open after leaving");
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(4);
            p = g.Players[0];
            yield return Into(g, p, "iron_vaults");
            Assert.IsTrue(p.Zone.IsWalkable(new Vector2Int(33, 13)), "and after a reload");
        }

        [UnityTest]
        public IEnumerator CryptLidsOpenTheStairDown()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 31);
            yield return Frames(2);
            yield return Into(g, p, "royal_crypts");
            var z = p.Zone;
            var room = z.GetComponent<PuzzleRoom>();
            var gate = new Vector2Int(18, 20);
            Assert.IsFalse(z.IsWalkable(gate));
            g.World.SetFlag("puzzlehint:royal_crypts:lid_1");

            // Shove a lid the wrong way, leave, come back: it's back where it started.
            p.Avatar.PlaceAt(z, new Vector2Int(9, 12));
            yield return Frames(1);
            PieceIn(z, "lid_1").Interact(p);
            Assert.AreEqual(new Vector2Int(11, 12), room.PosOf("lid_1"));
            yield return Into(g, p, "iron_vaults");
            yield return Into(g, p, "royal_crypts");
            z = p.Zone; room = z.GetComponent<PuzzleRoom>();
            Assert.AreEqual(new Vector2Int(10, 12), room.PosOf("lid_1"), "the lid is back by its plinth");

            foreach (var (lid, x) in new[] { ("lid_1", 10), ("lid_2", 26) })
            {
                p.Avatar.PlaceAt(z, new Vector2Int(x, 13));
                yield return Frames(1);
                PieceIn(z, lid).Interact(p);
                p.Avatar.PlaceAt(z, new Vector2Int(x, 12));
                yield return Frames(1);
                PieceIn(z, lid).Interact(p);
                Assert.AreEqual(new Vector2Int(x, 10), room.PosOf(lid), lid + " back on its plinth");
            }
            Assert.IsTrue(z.IsWalkable(gate), "the stair down opens");
            Assert.IsTrue(g.World.Flag("puzzle:royal_crypts:crypt_gate"));
        }

        [UnityTest]
        public IEnumerator CryptStairIsAShortcutBothWays()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 32);
            yield return Frames(2);
            var stair = new Vector2Int(6, 14);
            var down = WorldDb.Zones["capital"].Warps.Find(w => w.At == stair);
            Assert.IsNotNull(down, "the cathedral crypt stair is a way down");
            yield return StepOnto(g, p, "capital", stair, stair + Vector2Int.right + Vector2Int.down);
            Assert.AreEqual("capital", p.Zone.Def.Id, "sealed from above at first");
            Assert.GreaterOrEqual(Count(p, "doesn't open from this side"), 1);

            yield return Into(g, p, "royal_crypts");
            Assert.IsTrue(g.World.Flag("crypt_stair_open"), "the crypts open the way up");
            var up = WorldDb.Zones["royal_crypts"].Warps.Find(w => w.ToZone == "capital");
            Assert.IsNotNull(up);
            yield return StepOnto(g, p, "royal_crypts", up.At, up.At + Vector2Int.right);
            Assert.AreEqual("capital", p.Zone.Def.Id, "the stair comes up in the cathedral yard");
            Assert.LessOrEqual(Vector2Int.Distance(p.MapPos, stair), 1.5f);
            yield return StepOnto(g, p, "capital", stair, stair + Vector2Int.right + Vector2Int.down);
            Assert.AreEqual("royal_crypts", p.Zone.Def.Id, "and goes back down");
        }

        [UnityTest]
        public IEnumerator ThirdIdealGrantsItsBoon()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 33, "paladin");
            yield return Frames(2);
            yield return Into(g, p, "royal_crypts");
            Assert.IsFalse(p.Character.ThirdIdeal);
            SecretIn("third_ideal_history").Resolve(p, CheckOutcome.Success);
            Assert.IsTrue(g.World.Flag(WorldDb.OathThird));
            Assert.IsTrue(g.World.LoreFound.Contains("third_ideal"));
            Assert.IsTrue(p.Character.ThirdIdeal, "the Third Ideal holds you up now");
            StringAssert.Contains(WorldDb.ThirdIdeal, LoreDb.Get("third_ideal").Text);
            Assert.IsFalse(SecretIn("third_ideal_religion").Available(p) && !g.World.Flag(WorldDb.OathSecond), "the kneeling way needs the Second Ideal");

            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(4);
            Assert.IsTrue(g.Players[0].Character.ThirdIdeal, "after a reload too");
            var p2 = g.DebugStart(Still(34), "rogue", "dwarf");
            yield return Frames(3);
            Assert.IsTrue(p2.Character.ThirdIdeal, "and for a player who joins later");
        }

        [UnityTest]
        public IEnumerator VaultsAndCryptsReachable()
        {
            var g = GameRoot.I;
            var p = ActThree(g, 35);
            yield return Frames(2);
            g.World.SetFlag("puzzle:iron_vaults:vault_gate");
            g.World.SetFlag("puzzle:royal_crypts:crypt_gate");
            foreach (var (zone, extra) in new[] { ("iron_vaults", new[] { new Vector2Int(38, 13), new Vector2Int(21, 8), new Vector2Int(29, 8), new Vector2Int(29, 20) }),
                                                  ("royal_crypts", new[] { new Vector2Int(1, 2), new Vector2Int(18, 23), new Vector2Int(10, 12), new Vector2Int(26, 12) }) })
            {
                yield return Into(g, p, zone);
                var z = p.Zone;
                AcademyTests.AssertAllReachable(g, z, z.Def.DefaultSpawn);
                foreach (var t in extra) Assert.IsTrue(AcademyTests.CanReachBeside(z, z.Def.DefaultSpawn, t), $"{zone}: {t}");
            }
        }

        [UnityTest]
        public IEnumerator GatherAtBringsEveryone()
        {
            var g = GameRoot.I;
            var p1 = ActThree(g, 4);
            yield return Frames(2);
            var p2 = g.DebugStart(Still(5), "cleric", "dwarf");
            yield return Frames(2);
            p2.Hud.CloseAll();
            g.Warp(p1, "town", WorldDb.TownCarriageStop, false);
            g.Warp(p2, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            g.GatherAt("roots", WorldDb.RootsStairFoot);
            yield return Frames(3);
            Assert.AreEqual("roots", p1.Zone.Def.Id);
            Assert.AreEqual("roots", p2.Zone.Def.Id);
            Assert.LessOrEqual(Vector2Int.Distance(p1.MapPos, p2.MapPos), 1.5f, "side by side");
        }
    }
}
