using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    // Zelda-style room puzzles (Phase 5). Exploration flavor only: the dice still decide every check (hints),
    // and nothing here touches D&D rules. Pieces sit on map tiles; the map chars (Q I J Z / @ %) just mark where.

    public enum PieceKind { Plate, Lever, Block, Mirror, Gate, Light, Crystal }

    public class PuzzlePiece
    {
        public string Id;
        public PieceKind Kind;
        public Vector2Int Pos;
        /// <summary>Gate: every id must be "on"; "!id" means it must be off.</summary>
        public string[] OpensWhen;
        /// <summary>Gate: once open, it stays open for good (world flag "puzzle:{zone}:{id}").</summary>
        public bool Latches;
        /// <summary>Light: the beam's heading, 0=N 1=E 2=S 3=W. Mirror: 0 = '/', 1 = '\'.</summary>
        public int Dir;
        /// <summary>Optional, on levers and mirrors: the first look is a dice-tray check that reveals this hint.</summary>
        public string Hint;
        public Skill HintSkill;
        public int HintDC;
    }

    public class PuzzleDef
    {
        public List<PuzzlePiece> Pieces = new List<PuzzlePiece>();
    }

    public static class PuzzleLogic
    {
        static readonly Vector2Int[] Step = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) }; // map rows grow southward

        /// <summary>'/' swaps N↔E and S↔W; '\' swaps N↔W and S↔E.</summary>
        public static int Reflect(int dir, int mirror) => mirror == 0 ? new[] { 1, 0, 3, 2 }[dir] : new[] { 3, 2, 1, 0 }[dir];

        /// <summary>The tiles the beam lights, starting one tile out from `start`. Mirrors turn it; it stops before a solid tile or after 64 steps.</summary>
        public static List<Vector2Int> TraceBeam(Vector2Int start, int dir, Func<Vector2Int, bool> solid, IReadOnlyDictionary<Vector2Int, int> mirrors)
        {
            var tiles = new List<Vector2Int>();
            var cur = start;
            for (int i = 0; i < 64; i++)
            {
                var next = cur + Step[dir];
                bool isMirror = mirrors != null && mirrors.ContainsKey(next);
                if (!isMirror && solid(next)) break;
                tiles.Add(next);
                cur = next;
                if (isMirror) dir = Reflect(dir, mirrors[next]);
            }
            return tiles;
        }

        public static bool GateOpen(PuzzlePiece gate, Func<string, bool> isOn)
        {
            if (gate.OpensWhen == null) return false;
            foreach (var cond in gate.OpensWhen)
            {
                bool want = !cond.StartsWith("!");
                if (isOn(want ? cond : cond.Substring(1)) != want) return false;
            }
            return true;
        }
    }

    /// <summary>Runs one zone's puzzle: piece state, the beam, gates opening and closing, and the reset when everyone leaves.</summary>
    public class PuzzleRoom : MonoBehaviour
    {
        public Zone Zone;
        PuzzleDef Def => Zone.Def.Puzzle;
        readonly Dictionary<string, PuzzlePieceObject> _objects = new Dictionary<string, PuzzlePieceObject>();
        readonly Dictionary<string, bool> _on = new Dictionary<string, bool>();
        readonly Dictionary<string, Vector2Int> _pos = new Dictionary<string, Vector2Int>();
        readonly Dictionary<string, int> _dir = new Dictionary<string, int>();
        readonly List<GameObject> _beam = new List<GameObject>();
        List<Vector2Int> _beamTiles = new List<Vector2Int>();
        float _nextRefresh;
        bool _occupied;

        public static PuzzleRoom Build(Zone zone)
        {
            var room = zone.gameObject.AddComponent<PuzzleRoom>();
            room.Zone = zone;
            foreach (var piece in zone.Def.Puzzle.Pieces)
            {
                var obj = zone.PuzzleProp(piece.Pos, SpriteFor(piece, false), piece.Kind == PieceKind.Crystal || piece.Kind == PieceKind.Light).AddComponent<PuzzlePieceObject>();
                obj.Room = room; obj.Piece = piece;
                room._objects[piece.Id] = obj;
                if (piece.Kind == PieceKind.Plate) obj.GetComponent<SpriteRenderer>().sortingOrder = -998; // flat on the floor, under feet
            }
            room.ResetUnlatched();
            return room;
        }

        string FlagKey(PuzzlePiece gate) => $"puzzle:{Zone.Def.Id}:{gate.Id}";
        GameRoot G => GameRoot.I;

        public bool IsOn(string id) => _on.TryGetValue(id, out var v) && v;
        public Vector2Int PosOf(string id) => _pos.TryGetValue(id, out var v) ? v : Def.Pieces.First(x => x.Id == id).Pos;
        public int DirOf(string id) => _dir.TryGetValue(id, out var v) ? v : Def.Pieces.First(x => x.Id == id).Dir;
        public IReadOnlyList<Vector2Int> BeamTiles => _beamTiles;

        static string SpriteFor(PuzzlePiece p, bool on)
        {
            switch (p.Kind)
            {
                case PieceKind.Plate: return on ? "obj_plate_down" : "obj_plate";
                case PieceKind.Lever: return on ? "obj_lever_on" : "obj_lever";
                case PieceKind.Block: return "obj_push_block";
                case PieceKind.Mirror: return "obj_mirror_a"; // swapped to _b by Dir in Redraw
                case PieceKind.Gate: return "obj_gate";
                case PieceKind.Light: return "obj_light_window";
                default: return on ? "obj_light_crystal_lit" : "obj_light_crystal";
            }
        }

        /// <summary>Blocks, levers and mirrors go back to their map state. Latched gates that opened stay open.</summary>
        public void ResetUnlatched()
        {
            foreach (var p in Def.Pieces)
            {
                if (p.Kind == PieceKind.Block)
                {
                    if (_pos.TryGetValue(p.Id, out var at) && at != p.Pos) Zone.SetSolid(at, false);
                    _pos[p.Id] = p.Pos;
                    Zone.SetSolid(p.Pos, true);
                    _objects[p.Id].transform.position = Zone.ToWorld(p.Pos) + new Vector3(0, -0.4f, 0);
                }
                if (p.Kind == PieceKind.Lever) _on[p.Id] = false;
                if (p.Kind == PieceKind.Mirror) _dir[p.Id] = p.Dir;
                if (p.Kind == PieceKind.Mirror || p.Kind == PieceKind.Light || p.Kind == PieceKind.Crystal) Zone.SetSolid(p.Pos, true);
            }
            Refresh();
        }

        /// <summary>Recomputes plates, the beam, crystals and gates from where everything stands right now.</summary>
        public void Refresh()
        {
            if (G == null) return;
            var standing = OccupiedTiles();
            foreach (var p in Def.Pieces)
            {
                if (p.Kind == PieceKind.Plate) _on[p.Id] = standing.Contains(p.Pos) || Def.Pieces.Any(b => b.Kind == PieceKind.Block && PosOf(b.Id) == p.Pos);
                if (p.Kind == PieceKind.Light) _on[p.Id] = true;
                if (p.Kind == PieceKind.Mirror) _on[p.Id] = DirOf(p.Id) == 1;
            }
            // Gates can block the beam and the beam can open gates: settle it in two passes.
            for (int pass = 0; pass < 2; pass++)
            {
                TraceAllBeams();
                foreach (var c in Def.Pieces.Where(x => x.Kind == PieceKind.Crystal))
                    _on[c.Id] = _beamTiles.Count > 0 && LitBy(c.Pos);
                foreach (var gate in Def.Pieces.Where(x => x.Kind == PieceKind.Gate))
                {
                    bool latched = gate.Latches && G.World.Flag(FlagKey(gate));
                    bool open = latched || PuzzleLogic.GateOpen(gate, IsOn);
                    if (open && gate.Latches && !latched) G.World.SetFlag(FlagKey(gate));
                    SetGate(gate, open);
                }
            }
            Redraw();
        }

        bool LitBy(Vector2Int crystal)
        {
            // each beam's last tile counts; TraceAllBeams keeps the ends
            foreach (var end in _beamEnds)
                if (end == crystal || (Mathf.Abs(end.x - crystal.x) + Mathf.Abs(end.y - crystal.y)) == 1) return true;
            return false;
        }

        readonly List<Vector2Int> _beamEnds = new List<Vector2Int>();

        void TraceAllBeams()
        {
            _beamTiles = new List<Vector2Int>();
            _beamEnds.Clear();
            var mirrors = Def.Pieces.Where(x => x.Kind == PieceKind.Mirror).ToDictionary(x => x.Pos, x => DirOf(x.Id));
            foreach (var light in Def.Pieces.Where(x => x.Kind == PieceKind.Light))
            {
                var tiles = PuzzleLogic.TraceBeam(light.Pos, light.Dir, t => !Zone.InBounds(t) || Zone.Blocked[t.x, t.y], mirrors);
                _beamTiles.AddRange(tiles);
                if (tiles.Count > 0) _beamEnds.Add(tiles[tiles.Count - 1]);
            }
        }

        void SetGate(PuzzlePiece gate, bool open)
        {
            bool was = IsOn(gate.Id);
            if (!open && was && AnyoneOn(gate.Pos)) return; // never shut a gate on someone standing in it
            _on[gate.Id] = open;
            if (open == was && Zone.Blocked[gate.Pos.x, gate.Pos.y] == !open) return;
            Zone.SetSolid(gate.Pos, !open);
            if (open && !was && _occupied) Sfx.Play("door");
        }

        void Redraw()
        {
            foreach (var p in Def.Pieces)
            {
                var o = _objects[p.Id];
                var sr = o.GetComponent<SpriteRenderer>();
                if (p.Kind == PieceKind.Gate) { sr.enabled = !IsOn(p.Id); continue; }
                string id = p.Kind == PieceKind.Mirror ? (DirOf(p.Id) == 0 ? "obj_mirror_a" : "obj_mirror_b") : SpriteFor(p, IsOn(p.Id));
                sr.sprite = ArtRegistry.Get(id);
            }
            while (_beam.Count < _beamTiles.Count)
            {
                var go = new GameObject("fx_beam");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ArtRegistry.Get("fx_beam");
                sr.sortingOrder = 5000;
                sr.color = new Color(1f, 1f, 1f, 0.75f);
                _beam.Add(go);
            }
            for (int i = 0; i < _beam.Count; i++)
            {
                bool show = i < _beamTiles.Count;
                _beam[i].SetActive(show);
                if (show) _beam[i].transform.position = Zone.ToWorld(_beamTiles[i]);
            }
        }

        /// <summary>Tiles held down by someone: players, companions (waiting or not) and enemies who are up. Downed or vanished creatures don't count.</summary>
        HashSet<Vector2Int> OccupiedTiles()
        {
            var set = new HashSet<Vector2Int>();
            foreach (var p in G.Players)
                if (p.IsReady && p.Zone == Zone && !p.Character.IsDown) set.Add(p.MapPos);
            foreach (var c in G.Companions)
                if (c != null && c.isActiveAndEnabled && c.Actor != null && c.Actor.Zone == Zone && (c.Character == null || !c.Character.IsDown)) set.Add(c.Actor.MapPos);
            foreach (var e in FindObjectsByType<EnemyAgent>())
                if (e.Zone == Zone && e.gameObject.activeInHierarchy && !e.Monster.IsDown) set.Add(e.MapPos);
            return set;
        }

        public bool AnyoneOn(Vector2Int t) => OccupiedTiles().Contains(t);

        void Update()
        {
            if (G == null || !G.InGame) return;
            bool occupied = G.Players.Any(p => p.IsReady && p.Zone == Zone);
            if (_occupied && !occupied) ResetUnlatched(); // the last player left: unlatched pieces go back
            _occupied = occupied;
            if (!occupied || Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + 0.2f;
            Refresh();
        }

        // ------------------------------------------------------------ piece actions

        public void ToggleLever(PuzzlePiece p)
        {
            _on[p.Id] = !IsOn(p.Id);
            Sfx.Play("clunk");
            Refresh();
        }

        public void TurnMirror(PuzzlePiece p)
        {
            _dir[p.Id] = 1 - DirOf(p.Id);
            Sfx.Play("clunk");
            Refresh();
        }

        /// <summary>Shoves a block one tile away from the pusher, onto free floor only. Returns false if it won't budge.</summary>
        public bool Push(PuzzlePiece block, Vector2Int from)
        {
            var at = PosOf(block.Id);
            var d = at - from;
            var step = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(Math.Sign(d.x), 0) : new Vector2Int(0, Math.Sign(d.y));
            if (step == Vector2Int.zero) return false;
            var to = at + step;
            if (!CanTakeBlock(to)) return false;
            Zone.SetSolid(at, false);
            Zone.SetSolid(to, true);
            _pos[block.Id] = to;
            _objects[block.Id].transform.position = Zone.ToWorld(to) + new Vector3(0, -0.4f, 0);
            Sfx.Play("door");
            Refresh();
            return true;
        }

        bool CanTakeBlock(Vector2Int t)
        {
            if (!Zone.IsWalkable(t)) return false;                       // walls, props, other blocks, closed gates
            if (Zone.WarpAt(t) != null || Zone.CharAt(t) == 'D') return false;
            if (Def.Pieces.Any(x => x.Kind == PieceKind.Gate && x.Pos == t)) return false; // even an open gate
            if (AnyoneOn(t)) return false;
            return true;
        }
    }

    /// <summary>The prop for one puzzle piece. Levers, mirrors and blocks are interactable; the rest just show state.</summary>
    public class PuzzlePieceObject : Interactable
    {
        public PuzzleRoom Room;
        public PuzzlePiece Piece;
        public override float Range => 1.5f;
        string HintKey => $"puzzlehint:{Room.Zone.Def.Id}:{Piece.Id}";
        bool HintPending(PlayerSlot p) => Piece.Hint != null && !GameRoot.I.World.Flag(HintKey) && !GameRoot.I.World.IsLocked(HintKey + ":" + p.Index);

        public override bool Available(PlayerSlot p) => Piece.Kind == PieceKind.Lever || Piece.Kind == PieceKind.Mirror || Piece.Kind == PieceKind.Block;

        public override string PromptText(PlayerSlot p)
        {
            if (HintPending(p)) return Piece.Kind == PieceKind.Lever ? "Study the levers" : "Study the mirror";
            switch (Piece.Kind)
            {
                case PieceKind.Lever: return "Pull the lever";
                case PieceKind.Mirror: return "Turn the mirror";
                default: return "Push the block";
            }
        }

        public override void Interact(PlayerSlot p)
        {
            if (p.IsBusy) return;
            if (HintPending(p)) { StudyHint(p); return; }
            switch (Piece.Kind)
            {
                case PieceKind.Lever: Room.ToggleLever(Piece); break;
                case PieceKind.Mirror: Room.TurnMirror(Piece); break;
                case PieceKind.Block:
                    if (!Room.Push(Piece, p.MapPos)) p.Hud.Narrate("It won't budge that way.", false);
                    break;
            }
        }

        void StudyHint(PlayerSlot p)
        {
            var g = GameRoot.I;
            var pc = p.Character;
            string sub = $"{SkillUtil.Name(Piece.HintSkill)} ({SkillUtil.AbilityOf(Piece.HintSkill)}) {AbilityUtil.Fmt(pc.SkillBonus(Piece.HintSkill))} · DC hidden";
            p.Hud.Dice.RollD20($"{SkillUtil.Name(Piece.HintSkill)} Check", sub, () => Checks.RollSkill(pc, Piece.HintSkill, AdvantageState.Normal), Piece.HintDC, true, r =>
            {
                if (Checks.IsSuccess(Checks.ResolveCheck(r, Piece.HintDC)))
                {
                    g.World.SetFlag(HintKey);
                    Sfx.Play("secret");
                    p.Hud.Push(new MessageScreen("You notice something", Piece.Hint));
                }
                else
                {
                    g.World.Lock(HintKey + ":" + p.Index, 24 * 60 - g.Clock.Minutes); // trial and error until tomorrow
                    p.Hud.Narrate("Nothing makes sense of it. Trial and error it is.", false);
                }
            });
        }
    }
}
