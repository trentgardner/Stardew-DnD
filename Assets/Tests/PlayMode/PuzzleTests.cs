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
    /// <summary>The puzzle layer (Phase 5): plates, levers, push blocks, mirrors, the light beam and gates.</summary>
    public class PuzzleTests
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
            WorldDb.Zones.Remove("test_puzzle");
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        // ============================================================ pure logic

        [Test]
        public void BeamReflectsOffMirrors()
        {
            System.Func<Vector2Int, bool> solid = t => t.x < 0 || t.y < 0 || t.x > 4 || t.y > 4;
            var mirrors = new Dictionary<Vector2Int, int> { { new Vector2Int(2, 2), 0 } };
            var beam = PuzzleLogic.TraceBeam(new Vector2Int(0, 2), 1, solid, mirrors);
            Assert.AreEqual(new Vector2Int(1, 2), beam[0], "the beam starts one tile out from the light");
            Assert.Contains(new Vector2Int(2, 2), beam, "it reaches the mirror");
            Assert.AreEqual(new Vector2Int(2, 0), beam[beam.Count - 1], "'/' turns an eastbound beam north");
            mirrors[new Vector2Int(2, 2)] = 1;
            beam = PuzzleLogic.TraceBeam(new Vector2Int(0, 2), 1, solid, mirrors);
            Assert.AreEqual(new Vector2Int(2, 4), beam[beam.Count - 1], "'\\' turns it south");
        }

        [Test]
        public void GateNeedsAllConditions()
        {
            var gate = new PuzzlePiece { Id = "g", Kind = PieceKind.Gate, OpensWhen = new[] { "a", "!b" } };
            var on = new HashSet<string>();
            System.Func<string, bool> isOn = id => on.Contains(id);
            Assert.IsFalse(PuzzleLogic.GateOpen(gate, isOn));
            on.Add("a");
            Assert.IsTrue(PuzzleLogic.GateOpen(gate, isOn));
            on.Add("b");
            Assert.IsFalse(PuzzleLogic.GateOpen(gate, isOn));
        }

        // ============================================================ in a test zone

        //  0123456789
        // 0XXXXXXXXXX
        // 1X..X.....X
        // 2X..J.Q...X
        // 3X........X
        // 4X.I..Q.ZXX
        // 5X......E.X   (E = an exit, so the block can't be pushed onto it)
        // 6XXXXXXXXXX
        static ZoneDef TestZone(bool latches = true)
        {
            var z = new ZoneDef
            {
                Id = "test_puzzle", Name = "Test Puzzle", DefaultSpawn = new Vector2Int(1, 1), EnterNarration = "A test room.",
                Map = new[] {
                    "XXXXXXXXXX",
                    "X..X.....X",
                    "X..J.Q...X",
                    "X........X",
                    "X.I..Q.ZXX",
                    "X......E.X",
                    "XXXXXXXXXX" },
                Puzzle = new PuzzleDef(),
            };
            z.Warps.Add(new WarpDef(7, 5, "town", 25, 19));
            z.Puzzle.Pieces.Add(new PuzzlePiece { Id = "block", Kind = PieceKind.Block, Pos = new Vector2Int(3, 2) });
            z.Puzzle.Pieces.Add(new PuzzlePiece { Id = "plate_a", Kind = PieceKind.Plate, Pos = new Vector2Int(5, 2) });
            z.Puzzle.Pieces.Add(new PuzzlePiece { Id = "plate_b", Kind = PieceKind.Plate, Pos = new Vector2Int(5, 4) });
            z.Puzzle.Pieces.Add(new PuzzlePiece { Id = "lever", Kind = PieceKind.Lever, Pos = new Vector2Int(2, 4) });
            z.Puzzle.Pieces.Add(new PuzzlePiece { Id = "gate", Kind = PieceKind.Gate, Pos = new Vector2Int(7, 4), OpensWhen = new[] { "plate_a", "lever" }, Latches = latches });
            return z;
        }

        static PuzzleRoom Room(GameRoot g) => g.Zones["test_puzzle"].GetComponent<PuzzleRoom>();
        static PuzzlePieceObject Piece(string id) => Object.FindObjectsByType<PuzzlePieceObject>().Single(x => x.Piece.Id == id);

        [UnityTest]
        public IEnumerator BlockOnlyMovesOntoFreeFloor()
        {
            WorldDb.Zones["test_puzzle"] = TestZone();
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            yield return Frames(2);
            var room = Room(g);
            var block = Piece("block");

            g.Warp(p, "test_puzzle", new Vector2Int(3, 3), false); // south of the block: pushes it north, into the wall
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(3, 2), room.PosOf("block"), "it won't go into a wall");

            g.Warp(p, "test_puzzle", new Vector2Int(2, 2), false); // west of it: pushes east
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "dwarf", "Comp"));
            var comp = g.SpawnCompanion(p, pc, "hums");
            yield return Frames(2);
            comp.Waiting = true;
            comp.Actor.PlaceAt(p.Zone, new Vector2Int(4, 2));
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(3, 2), room.PosOf("block"), "it won't go onto a companion");

            comp.Actor.PlaceAt(p.Zone, new Vector2Int(1, 3));
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(4, 2), room.PosOf("block"), "free floor: it slides one tile");
            Assert.IsTrue(p.Zone.IsWalkable(new Vector2Int(3, 2)), "the tile it left is open again");
            Assert.IsFalse(p.Zone.IsWalkable(new Vector2Int(4, 2)), "and its new tile is solid");
            g.Warp(p, "test_puzzle", new Vector2Int(3, 2), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(5, 2), room.PosOf("block"));
            room.Refresh();
            Assert.IsTrue(room.IsOn("plate_a"), "a block on a plate holds it down");

            // toward the gate: a column with the gate at (7,4); park the block at (7,3) first is impossible (gate below), so push south from (5,2)
            g.Warp(p, "test_puzzle", new Vector2Int(5, 1), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(5, 3), room.PosOf("block"));
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(5, 4), room.PosOf("block"), "onto plate b");
            g.Warp(p, "test_puzzle", new Vector2Int(4, 4), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(6, 4), room.PosOf("block"));
            g.Warp(p, "test_puzzle", new Vector2Int(5, 4), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(6, 4), room.PosOf("block"), "it won't go onto a gate");
            g.Warp(p, "test_puzzle", new Vector2Int(6, 3), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(6, 5), room.PosOf("block"));
            g.Warp(p, "test_puzzle", new Vector2Int(5, 5), false);
            yield return Frames(2);
            block.Interact(p);
            Assert.AreEqual(new Vector2Int(6, 5), room.PosOf("block"), "it won't go onto a warp");
        }

        [UnityTest]
        public IEnumerator CompanionWaitingOnAPlateHoldsIt()
        {
            WorldDb.Zones["test_puzzle"] = TestZone();
            var g = GameRoot.I;
            var p = g.DebugStart(Still(2), "fighter", "human");
            yield return Frames(2);
            g.Warp(p, "test_puzzle", new Vector2Int(1, 1), false);
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "dwarf", "Comp"));
            var comp = g.SpawnCompanion(p, pc, "hums");
            yield return Frames(2);
            var room = Room(g);
            comp.Waiting = true;
            comp.Actor.PlaceAt(p.Zone, new Vector2Int(5, 4));
            yield return Frames(2);
            room.Refresh();
            Assert.IsTrue(room.IsOn("plate_b"), "a companion told to wait on the plate holds it");
            comp.Character.HP = 0;
            room.Refresh();
            Assert.IsFalse(room.IsOn("plate_b"), "a downed companion doesn't");
        }

        [UnityTest]
        public IEnumerator PuzzleResetsOnReentryButLatchedGatesStayOpen()
        {
            WorldDb.Zones["test_puzzle"] = TestZone();
            var g = GameRoot.I;
            var p = g.DebugStart(Still(3), "fighter", "human");
            yield return Frames(2);
            g.Warp(p, "test_puzzle", new Vector2Int(2, 3), false);
            yield return Frames(2);
            var room = Room(g);
            Assert.IsFalse(p.Zone.IsWalkable(new Vector2Int(7, 4)), "the gate starts closed");
            Piece("lever").Interact(p);
            Assert.IsTrue(room.IsOn("lever"));
            g.Warp(p, "test_puzzle", new Vector2Int(5, 2), false); // stand on plate a
            yield return Frames(2);
            room.Refresh();
            Assert.IsTrue(room.IsOn("gate"), "lever + plate opens the gate");
            Assert.IsTrue(p.Zone.IsWalkable(new Vector2Int(7, 4)));
            g.Warp(p, "test_puzzle", new Vector2Int(2, 2), false);
            yield return Frames(2);
            Piece("block").Interact(p);
            Assert.AreEqual(new Vector2Int(4, 2), room.PosOf("block"));

            g.Warp(p, "town", WorldDb.Zones["town"].DefaultSpawn, false);
            yield return Frames(15);
            g.Warp(p, "test_puzzle", new Vector2Int(1, 1), false);
            yield return Frames(15);
            Assert.AreEqual(new Vector2Int(3, 2), room.PosOf("block"), "the block went back");
            Assert.IsFalse(room.IsOn("lever"), "the lever went back");
            Assert.IsTrue(room.IsOn("gate"), "the latched gate stays open");
            Assert.IsTrue(p.Zone.IsWalkable(new Vector2Int(7, 4)));

            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(15);
            Assert.IsTrue(Room(g).IsOn("gate"), "and stays open after a save and load");
            Assert.IsTrue(g.Zones["test_puzzle"].IsWalkable(new Vector2Int(7, 4)));
        }

        [UnityTest]
        public IEnumerator UnlatchedGateClosesWhenThePlateIsLeft()
        {
            WorldDb.Zones["test_puzzle"] = TestZone(latches: false);
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            g.Warp(p, "test_puzzle", new Vector2Int(2, 3), false);
            yield return Frames(2);
            var room = Room(g);
            Piece("lever").Interact(p);
            g.Warp(p, "test_puzzle", new Vector2Int(5, 2), false);
            yield return Frames(2);
            room.Refresh();
            Assert.IsTrue(room.IsOn("gate"));
            g.Warp(p, "test_puzzle", new Vector2Int(4, 3), false);
            yield return Frames(2);
            room.Refresh();
            Assert.IsFalse(room.IsOn("gate"), "step off the plate and it closes");
            Assert.IsFalse(p.Zone.IsWalkable(new Vector2Int(7, 4)));
        }
    }
}
