using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DnD.Game
{
    /// <summary>
    /// A built map. Zones sit side by side in world space (each at its own X offset) so two players
    /// can be in different zones with independent split-screen cameras. Only occupied zones run AI.
    /// One tile = one Unity unit = 5 feet.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        public ZoneDef Def;
        public Vector2Int Origin;
        public bool[,] Blocked;   // [col,row] map coords
        public readonly List<GameObject> Objects = new List<GameObject>();

        Tilemap _base, _ground, _solid;
        static readonly Dictionary<string, Tile> TileCache = new Dictionary<string, Tile>();

        public int W => Def.Width;
        public int H => Def.Height;

        // ------------------------------------------------------------ coordinates

        public Vector3 ToWorld(Vector2Int map) => new Vector3(Origin.x + map.x + 0.5f, Origin.y + (H - 1 - map.y) + 0.5f, 0);

        public Vector2Int ToMap(Vector3 world)
        {
            int col = Mathf.FloorToInt(world.x - Origin.x);
            int rowFromBottom = Mathf.FloorToInt(world.y - Origin.y);
            return new Vector2Int(col, H - 1 - rowFromBottom);
        }

        public bool InBounds(Vector2Int m) => m.x >= 0 && m.y >= 0 && m.x < W && m.y < H;
        public bool Contains(Vector3 world) => InBounds(ToMap(world));
        public bool IsWalkable(Vector2Int m) => InBounds(m) && !Blocked[m.x, m.y];
        public char CharAt(Vector2Int m) => InBounds(m) ? Def.Map[m.y][m.x] : 'X';

        public Bounds WorldBounds => new Bounds(new Vector3(Origin.x + W / 2f, Origin.y + H / 2f), new Vector3(W, H, 1));

        // ------------------------------------------------------------ building

        static Tile GetTile(string spriteId, bool solid)
        {
            string key = spriteId + (solid ? "#s" : "");
            if (TileCache.TryGetValue(key, out var t)) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = spriteId != null ? ArtRegistry.Get(spriteId) : null;
            t.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            TileCache[key] = t;
            return t;
        }

        public static Zone Build(ZoneDef def, int index, Transform parent)
        {
            var go = new GameObject("Zone_" + def.Id);
            go.transform.SetParent(parent, false);
            var zone = go.AddComponent<Zone>();
            zone.Def = def;
            zone.Origin = new Vector2Int(index * 100, 0);
            zone.Blocked = new bool[def.Width, def.Height];

            var grid = go.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            go.transform.position = new Vector3(zone.Origin.x, zone.Origin.y, 0);

            // Base floor under every cell, so see-through tiles (plaza edges, counters, crops) never show black.
            zone._base = MakeLayer(go.transform, "Base", -1001, false);
            zone._ground = MakeLayer(go.transform, "Ground", -1000, false);
            zone._solid = MakeLayer(go.transform, "Solid", -999, true);

            for (int row = 0; row < def.Height; row++)
                for (int col = 0; col < def.Width; col++)
                    zone.BuildCell(new Vector2Int(col, row), def.Map[row][col]);
            // Building signs hang on the wall right of each door.
            foreach (var kv in def.DoorSigns)
            {
                var sign = zone.Prop(kv.Key + Vector2Int.right, "obj_hanging_sign").AddComponent<SignInteractable>();
                sign.Title = kv.Value.Name; sign.Prompt = $"Read sign: {kv.Value.Name}"; sign.Text = kv.Value.Text; sign.Reach = 1.8f;
            }
            // Quest spots can sit on any walkable tile (they come and go with their quest's stage).
            foreach (var kv in def.Secrets)
                if ("qOY".IndexOf(zone.CharAt(kv.Key)) < 0)
                    zone.Prop(kv.Key, kv.Value.Sprite).AddComponent<SecretInteractable>().Def = kv.Value;

            if (def.Puzzle != null) PuzzleRoom.Build(zone);

            zone._base.CompressBounds();
            zone._ground.CompressBounds();
            zone._solid.CompressBounds();
            return zone;
        }

        static Tilemap MakeLayer(Transform parent, string name, int order, bool collide)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            if (collide)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var col = go.AddComponent<TilemapCollider2D>();
                col.compositeOperation = Collider2D.CompositeOperation.Merge;
                go.AddComponent<CompositeCollider2D>();
            }
            return tm;
        }

        Vector3Int Cell(Vector2Int m) => new Vector3Int(m.x, H - 1 - m.y, 0);

        string FloorFor(Vector2Int m)
        {
            if (Def.Dark) return "tile_cave_floor";
            if (Def.Id == "temple") return "tile_stone_floor";
            if (Def.Indoors) return "tile_wood_floor";
            if (Def.Id == "forest") return ((m.x * 5 + m.y * 11) % 4 == 0) ? "tile_forest_grass_v1" : "tile_forest_grass";
            return ((m.x * 7 + m.y * 13) % 5 == 0) ? "tile_grass_v1" : "tile_grass";
        }

        void Ground(Vector2Int m, string sprite) => _ground.SetTile(Cell(m), GetTile(Styled(m, sprite), false));
        void Wall(Vector2Int m, string sprite) { _solid.SetTile(Cell(m), GetTile(Styled(m, sprite), true)); Blocked[m.x, m.y] = true; }

        /// <summary>Map chars whose tiles take edge/corner pieces from their neighbors, and which neighbor chars count as "the same".</summary>
        static readonly Dictionary<char, (TileShape shape, string same)> Styles = new Dictionary<char, (TileShape, string)>
        {
            { ',', (TileShape.Blob, ",bDEM") },   // dirt paths run into bridges, doors and the mine mouth
            { ';', (TileShape.Blob, ";") },       // sand
            { '~', (TileShape.Blob, "~b") },      // water (a bridge has water under it)
            { 'o', (TileShape.Blob, "oSOmNp") },  // cobbled square around the statue, well, stalls, benches and planters
            { 'X', (TileShape.Rect, "XM") },      // cliffs (the mine entrance is cut into them)
            { 'R', (TileShape.Rect, "R") },
            { 'U', (TileShape.Rect, "U") },
            { 'G', (TileShape.Rect, "G") },
            { 'W', (TileShape.Rect, "WwD") },     // walls continue through windows and doors
            { 'C', (TileShape.Rect, "C") },
            { 'f', (TileShape.Rect, "f") },
        };

        /// <summary>The neighbor-aware variant of a tile if real art exists for it, else the plain tile.</summary>
        string Styled(Vector2Int m, string baseId)
        {
            if (baseId == null || !Styles.TryGetValue(CharAt(m), out var st)) return baseId;
            string v = TileStyler.Variant(baseId, st.shape, (dx, dy) => st.same.IndexOf(CharAt(new Vector2Int(m.x + dx, m.y + dy))) >= 0, m.x, m.y);
            return ArtRegistry.Resolve(v, baseId);
        }
        bool NextToCobbles(Vector2Int m) =>
            CharAt(m + Vector2Int.left) == 'o' || CharAt(m + Vector2Int.right) == 'o' || CharAt(m + Vector2Int.up) == 'o' || CharAt(m + Vector2Int.down) == 'o';

        /// <summary>Puzzle pieces come and go: a block slides, a gate opens. Toggles the invisible blocker on one tile.</summary>
        public void SetSolid(Vector2Int m, bool solid)
        {
            if (!InBounds(m)) return;
            _solid.SetTile(Cell(m), solid ? GetTile("tile_none", true) : null);
            Blocked[m.x, m.y] = solid;
        }

        public GameObject PuzzleProp(Vector2Int m, string sprite, bool glow) => Prop(m, sprite, glow);

        /// <summary>Invisible blocker under props (trees, tables...).</summary>
        void Solid(Vector2Int m) { _solid.SetTile(Cell(m), GetTile("tile_none", true)); Blocked[m.x, m.y] = true; }

        void BuildCell(Vector2Int m, char c)
        {
            string floor = FloorFor(m);
            _base.SetTile(Cell(m), GetTile(floor, false));
            switch (c)
            {
                case '.': Ground(m, floor); break;
                case ',': Ground(m, "tile_path"); break;
                case '*': Ground(m, Def.Id == "forest" ? "tile_forest_flowers" : "tile_flowers"); break;
                case ':': Ground(m, Def.Indoors || Def.Dark ? floor : "tile_cobbles"); break; // outdoors: old flagstones
                case '=': Ground(m, "tile_wood_floor"); break;
                case '_': Ground(m, "tile_rug"); break;
                case '~': Wall(m, "tile_water"); break;
                case 'b': Ground(m, "tile_bridge"); break;
                case ';': Ground(m, "tile_sand"); break;
                case 'j': Ground(m, "tile_reeds"); break;
                case 'v': Ground(m, "tile_crops"); break;
                case 'k': Ground(m, "tile_mushrooms"); break;
                case 'o': Ground(m, "tile_cobbles"); break;
                case 'm': Ground(m, "tile_cobbles"); Solid(m); Prop(m, "obj_stall"); break;
                case 'P': Ground(m, floor); Solid(m); Prop(m, "obj_pillar"); break;
                case 'K': Ground(m, floor); Solid(m); Prop(m, "obj_forge", true); break;
                case 'N': Ground(m, NextToCobbles(m) ? "tile_cobbles" : floor); Solid(m); Prop(m, "obj_bench"); break;
                case 'p': Ground(m, NextToCobbles(m) ? "tile_cobbles" : floor); Solid(m); Prop(m, "obj_planter"); break;
                case 'L': Ground(m, floor); Solid(m); Prop(m, "obj_washing_line"); break;
                case 'F': Ground(m, floor); Solid(m); Prop(m, "obj_campfire", true); break;
                case 'S':
                    Ground(m, "tile_cobbles"); Solid(m);
                    var statue = Prop(m, "obj_statue").AddComponent<SignInteractable>();
                    statue.Title = "Dragon Statue"; statue.Prompt = "Look at the statue";
                    statue.Text = Def.Signs.TryGetValue(m, out var st) ? st : "A stone dragon.";
                    break;
                case 'O':
                case 'Y':
                case 'q':
                    Ground(m, c == 'O' ? "tile_cobbles" : c == 'Y' ? "tile_mushrooms" : floor); Solid(m);
                    if (Def.Secrets.TryGetValue(m, out var secret))
                        Prop(m, secret.Sprite, c == 'Y').AddComponent<SecretInteractable>().Def = secret;
                    else Debug.LogWarning($"{Def.Id}: no SecretDef at {m}");
                    break;
                case 'X': Wall(m, Def.Dark ? "tile_cave_wall" : "tile_cliff"); break; // rock in the mine, green cliffs outdoors
                case 'W': Wall(m, Def.Id == "temple" || Def.Id == "watchtower" ? "tile_stone_wall" : "tile_house_wall"); break;
                case 'w': Wall(m, "tile_window"); break;
                case 'U': Wall(m, "tile_roof_blue"); break;
                case 'R': Wall(m, "tile_roof"); break;
                case 'G': Wall(m, "tile_roof_gold"); break;
                case 'C': Wall(m, "tile_counter"); break;
                case 'D':
                    Ground(m, Def.Indoors ? floor : "tile_door");
                    if (!Def.Indoors && WarpAt(m) == null)
                    {
                        Solid(m);
                        var door = Prop(m, "tile_none").AddComponent<SignInteractable>();
                        door.Title = "Door"; door.Prompt = "Knock";
                        door.Text = "Locked. From inside, someone shouts: \"WE DON'T WANT ANY!\"";
                    }
                    break;
                case 'M': Ground(m, "tile_mine_entrance"); break;
                case 'E': Ground(m, "tile_exit_up"); break;
                case 'V': Ground(m, "tile_stairs_down"); break;
                case 'd': Ground(m, "tile_scorched"); break;
                case 'e': Ground(m, floor); Solid(m); Prop(m, "obj_crystal", true); break;
                case 'H': Ground(m, floor); Solid(m); Prop(m, "obj_hut"); break;
                case 'u': Ground(m, floor); Solid(m); Prop(m, "obj_tent"); break;
                case 'i': Ground(m, floor); Solid(m); Prop(m, "obj_brazier", true); break;
                // puzzle pieces: the puzzle layer dresses these; a closed gate and a push block are solid
                case 'Z': case 'J': Ground(m, floor); Solid(m); break;
                case 'Q': case 'I': case '/': case '@': case '%': Ground(m, floor); break;
                case 'T': Ground(m, floor); Solid(m); Prop(m, "obj_tree"); break;
                case 't': Ground(m, floor); Solid(m); Prop(m, "obj_tree"); break;
                case 'h': Ground(m, floor); Solid(m); Prop(m, "obj_bush"); break;
                case 'r': Ground(m, floor); Solid(m); Prop(m, "obj_rock"); break;
                case 'f': Ground(m, floor); Wall(m, "tile_fence"); break;
                case 'l': Ground(m, floor); Solid(m); Prop(m, "obj_lantern", true); break;
                case 'x': Ground(m, floor); Solid(m); Prop(m, (m.x + m.y) % 2 == 0 ? "obj_crate" : "obj_barrel"); break;
                case 'a': Ground(m, floor); Solid(m); Prop(m, "obj_table"); break;
                case 'A': Ground(m, floor); Solid(m); Prop(m, "obj_altar", true); break;
                case 's':
                case 'n':
                    Ground(m, CharAt(new Vector2Int(m.x + 1, m.y)) == 'o' ? "tile_cobbles" : floor); Solid(m);
                    var sign = Prop(m, "obj_sign");
                    var s = sign.AddComponent<SignInteractable>();
                    if (c == 'n') s.Title = "Notice Board";
                    s.Text = Def.Signs.TryGetValue(m, out var txt) ? txt : "The sign is too weathered to read.";
                    break;
                case 'B':
                    Ground(m, floor); Solid(m);
                    Prop(m, "obj_bed").AddComponent<BedInteractable>();
                    break;
                case 'c':
                    Ground(m, floor); Solid(m);
                    var chestDef = Def.Chests.Find(ch => ch.Pos == m);
                    var chest = Prop(m, "obj_chest").AddComponent<ChestInteractable>();
                    chest.Def = chestDef;
                    break;
                case 'g':
                    Ground(m, floor); Solid(m);
                    Prop(m, "obj_egg_glow", true).AddComponent<EggInteractable>();
                    break;
                default: Ground(m, floor); break;
            }
        }

        GameObject Prop(Vector2Int m, string sprite, bool glow = false)
        {
            var go = new GameObject(sprite);
            go.transform.SetParent(transform, false);
            go.transform.position = ToWorld(m) + new Vector3(0, -0.4f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtRegistry.Get(sprite);
            sr.sortingOrder = YSort.OrderFor(go.transform.position.y);
            Objects.Add(go);
            if (glow) LightSource.Attach(go, 3f, new Color(1f, 0.85f, 0.5f, 1f));
            return go;
        }

        // ------------------------------------------------------------ queries

        /// <summary>Breadth-first path on walkable tiles (4-way + diagonals not cutting corners).</summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to, System.Func<Vector2Int, bool> extraBlocked = null, int maxNodes = 4000)
        {
            var result = new List<Vector2Int>();
            if (!InBounds(from) || !InBounds(to)) return result;
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            prev[from] = from;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
            int n = 0;
            while (q.Count > 0 && n++ < maxNodes)
            {
                var cur = q.Dequeue();
                if (cur == to) break;
                foreach (var d in dirs)
                {
                    var nx = cur + d;
                    if (prev.ContainsKey(nx) || !IsWalkable(nx)) continue;
                    if (d.x != 0 && d.y != 0 && (!IsWalkable(new Vector2Int(cur.x + d.x, cur.y)) || !IsWalkable(new Vector2Int(cur.x, cur.y + d.y)))) continue;
                    if (extraBlocked != null && nx != to && extraBlocked(nx)) continue;
                    prev[nx] = cur;
                    q.Enqueue(nx);
                }
            }
            if (!prev.ContainsKey(to)) return result;
            var p = to;
            while (p != from) { result.Add(p); p = prev[p]; }
            result.Reverse();
            return result;
        }

        /// <summary>Straight-line visibility between tiles (walls block sight).</summary>
        public bool LineOfSight(Vector2Int a, Vector2Int b)
        {
            int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                var p = new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)), Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)));
                char c = CharAt(p);
                if (c == 'X' || c == 'W' || c == 'w' || c == 'T' || c == 'U' || c == 'R' || c == 'G' || c == 'S' || c == 'P' || c == 'e' || c == 'H') return false;
                if (c == 'Z' && Blocked[p.x, p.y]) return false; // a closed puzzle gate
            }
            return true;
        }

        public WarpDef WarpAt(Vector2Int m) => Def.Warps.Find(w => w.At == m);

        /// <summary>Chebyshev distance in feet (2024 grid rule: diagonals cost 5 ft).</summary>
        public static int DistanceFt(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)) * 5;
    }

    /// <summary>Keeps sprites drawn in the right front-to-back order (lower on screen = in front).</summary>
    public class YSort : MonoBehaviour
    {
        SpriteRenderer _sr;
        public int Offset;
        public static int OrderFor(float y) => Mathf.RoundToInt(-y * 16);
        void Awake() { _sr = GetComponent<SpriteRenderer>(); }
        void LateUpdate() { if (_sr) _sr.sortingOrder = OrderFor(transform.position.y) + Offset; }
    }
}
