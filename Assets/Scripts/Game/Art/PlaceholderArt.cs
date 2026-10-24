using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Generates simple 16x16 Stardew-flavored pixel art for any sprite id that has no real art yet.
    /// Placeholder quality on purpose: readable, consistent palette, cheap to replace.
    /// </summary>
    public static class PlaceholderArt
    {
        const int N = 16;

        // ------------------------------------------------------------------ palette
        static Color32 C(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Outline = C("2b1d24");

        // ------------------------------------------------------------------ entry point
        public static Sprite Generate(string id)
        {
            Texture2D tex;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (id.StartsWith("tile_")) tex = Tile(id.Substring(5));
            else if (id.StartsWith("hero_") || id.StartsWith("npc_") || id.StartsWith("ally_")) { tex = Humanoid(id); pivot = new Vector2(0.5f, 0.1f); }
            else if (id.StartsWith("mon_")) { tex = Monster(id); pivot = new Vector2(0.5f, 0.1f); }
            else if (id.StartsWith("item_")) tex = Item(id.Substring(5));
            else if (id.StartsWith("obj_")) { tex = Obj(id.Substring(4)); pivot = new Vector2(0.5f, 0.1f); }
            else if (id.StartsWith("ui_")) tex = Ui(id.Substring(3));
            else tex = Missing();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            var border = id == "ui_panel" || id == "ui_panel_dark" || id == "ui_slot" ? new Vector4(4, 4, 4, 4) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ArtRegistry.PixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }

        static Texture2D NewTex(int w = N, int h = N)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Clear;
            t.SetPixels32(px);
            return t;
        }

        static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177;
                return h ^ (h >> 16);
            }
        }

        static float Rand(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFF) / 65535f;

        static Texture2D Missing()
        {
            var t = NewTex();
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                    t.SetPixel(x, y, ((x / 4 + y / 4) % 2 == 0) ? (Color)C("ff00ff") : Color.black);
            return t;
        }

        /// <summary>Draws a template where row 0 is the TOP of the sprite.</summary>
        static void Stamp(Texture2D t, string[] rows, Dictionary<char, Color32> pal, int ox = 0, int oy = 0)
        {
            int h = rows.Length;
            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int x = 0; x < row.Length; x++)
                {
                    char ch = row[x];
                    if (ch == '.' || ch == ' ') continue;
                    if (!pal.TryGetValue(ch, out var col)) continue;
                    int px = x + ox, py = (h - 1 - r) + oy;
                    if (px >= 0 && py >= 0 && px < t.width && py < t.height) t.SetPixel(px, py, col);
                }
            }
        }

        // ------------------------------------------------------------------ tiles

        static Texture2D Noise(Color32 baseCol, Color32 dark, Color32 light, int seed, float darkChance, float lightChance)
        {
            var t = NewTex();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float r = Rand(x, y, seed);
                    Color32 c = baseCol;
                    if (r < darkChance) c = dark;
                    else if (r > 1 - lightChance) c = light;
                    t.SetPixel(x, y, c);
                }
            return t;
        }

        static Texture2D Tile(string kind)
        {
            int seed = kind.GetHashCode();
            switch (kind)
            {
                case "grass":
                case "grass_v1":
                case "grass_v2":
                {
                    var t = Noise(C("5a9e3c"), C("4a8a32"), C("6cb348"), seed, 0.12f, 0.08f);
                    // little grass tufts
                    for (int i = 0; i < 3; i++)
                    {
                        int x = 2 + (int)(Rand(i, 1, seed) * 11), y = 2 + (int)(Rand(i, 2, seed) * 11);
                        t.SetPixel(x, y, C("3f7a2b")); t.SetPixel(x + 1, y + 1, C("3f7a2b")); t.SetPixel(x + 2, y, C("3f7a2b"));
                    }
                    return t;
                }
                case "flowers":
                {
                    var t = Tile("grass");
                    Color32[] petals = { C("f2d14e"), C("e86a8a"), C("ffffff"), C("9b7fe0") };
                    for (int i = 0; i < 4; i++)
                    {
                        int x = 2 + (int)(Rand(i, 7, seed) * 11), y = 2 + (int)(Rand(i, 9, seed) * 11);
                        var p = petals[i % petals.Length];
                        t.SetPixel(x, y + 1, p); t.SetPixel(x - 1, y, p); t.SetPixel(x + 1, y, p); t.SetPixel(x, y - 1, p);
                        t.SetPixel(x, y, C("f7e08a"));
                    }
                    return t;
                }
                case "path": return Noise(C("c9a46a"), C("b38f58"), C("d8b67e"), seed, 0.15f, 0.1f);
                case "sand": return Noise(C("e3cf8f"), C("d4bd7a"), C("efdea6"), seed, 0.1f, 0.1f);
                case "water":
                {
                    var t = Noise(C("3f8fd2"), C("3680c0"), C("4a9ddf"), seed, 0.1f, 0.05f);
                    for (int x = 0; x < N; x++)
                    {
                        if ((x + 2) % 8 < 3) t.SetPixel(x, 11, C("8cc8f0"));
                        if ((x + 6) % 8 < 3) t.SetPixel(x, 4, C("8cc8f0"));
                    }
                    return t;
                }
                case "wood_floor":
                {
                    var t = Noise(C("a0693a"), C("8d5a30"), C("b07843"), seed, 0.08f, 0.06f);
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 0, C("6e4423")); t.SetPixel(x, 8, C("6e4423")); }
                    t.SetPixel(5, 4, C("6e4423")); t.SetPixel(12, 12, C("6e4423"));
                    return t;
                }
                case "stone_floor":
                {
                    var t = Noise(C("8a8580"), C("77726d"), C("9b9690"), seed, 0.1f, 0.08f);
                    for (int i = 0; i < N; i++) { t.SetPixel(i, 0, C("5e5a56")); t.SetPixel(0, i, C("5e5a56")); t.SetPixel(i, 8, C("5e5a56")); }
                    for (int i = 0; i < 8; i++) t.SetPixel(8, i + 8, C("5e5a56"));
                    return t;
                }
                case "cave_floor": return Noise(C("5b4b42"), C("4c3e36"), C("6b594e"), seed, 0.18f, 0.1f);
                case "cave_wall":
                {
                    var t = Noise(C("3a2e2a"), C("2c2220"), C("4a3b35"), seed, 0.2f, 0.12f);
                    for (int x = 0; x < N; x++) t.SetPixel(x, 0, C("1e1615"));
                    return t;
                }
                case "stone_wall":
                {
                    var t = Noise(C("9c958c"), C("857e76"), C("aea79e"), seed, 0.1f, 0.1f);
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 0, Outline); t.SetPixel(x, 5, C("6d6760")); t.SetPixel(x, 10, C("6d6760")); }
                    for (int y = 0; y < 5; y++) { t.SetPixel(4, y, C("6d6760")); t.SetPixel(12, y + 5, C("6d6760")); t.SetPixel(7, y + 10, C("6d6760")); }
                    return t;
                }
                case "house_wall":
                {
                    var t = Noise(C("e8d5b0"), C("d9c49c"), C("f2e2c2"), seed, 0.06f, 0.06f);
                    for (int y = 0; y < N; y++) { t.SetPixel(0, y, C("7a4f2c")); t.SetPixel(15, y, C("7a4f2c")); }
                    for (int x = 0; x < N; x++) t.SetPixel(x, 0, C("7a4f2c"));
                    return t;
                }
                case "window":
                {
                    var t = Tile("house_wall");
                    for (int y = 5; y < 12; y++) for (int x = 4; x < 12; x++) t.SetPixel(x, y, (x == 7 || x == 8 || y == 8) ? C("7a4f2c") : C("9fd3e8"));
                    return t;
                }
                case "roof":
                {
                    var t = Noise(C("b0453a"), C("963a31"), C("c25247"), seed, 0.08f, 0.06f);
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 3, C("7d2c25")); t.SetPixel(x, 11, C("7d2c25")); }
                    return t;
                }
                case "roof_blue":
                {
                    var t = Noise(C("4a6fa5"), C("3e5e8e"), C("5a80b8"), seed, 0.08f, 0.06f);
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 3, C("2f4870")); t.SetPixel(x, 11, C("2f4870")); }
                    return t;
                }
                case "roof_gold":
                {
                    var t = Noise(C("d9a441"), C("c28f35"), C("e8b856"), seed, 0.08f, 0.06f);
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 3, C("8f6421")); t.SetPixel(x, 11, C("8f6421")); }
                    return t;
                }
                case "door":
                {
                    var t = Tile("house_wall");
                    for (int y = 0; y < 13; y++) for (int x = 3; x < 13; x++) t.SetPixel(x, y, (x == 3 || x == 12 || y == 12) ? C("4a2e18") : C("8a5a32"));
                    t.SetPixel(10, 6, C("f2d14e"));
                    return t;
                }
                case "mine_entrance":
                {
                    var t = Tile("cave_wall");
                    for (int y = 0; y < 12; y++) for (int x = 3; x < 13; x++) t.SetPixel(x, y, C("0d0908"));
                    for (int y = 0; y < 13; y++) { t.SetPixel(2, y, C("8a5a32")); t.SetPixel(13, y, C("8a5a32")); }
                    for (int x = 2; x < 14; x++) t.SetPixel(x, 12, C("8a5a32"));
                    return t;
                }
                case "exit_up":
                {
                    var t = Tile("cave_floor");
                    for (int i = 0; i < 6; i++) for (int x = 2 + i; x < 14 - i; x++) t.SetPixel(x, 4 + i, C("e8c87a"));
                    return t;
                }
                case "fence":
                {
                    var t = Tile("grass");
                    for (int x = 0; x < N; x++) { t.SetPixel(x, 9, C("8a5a32")); t.SetPixel(x, 5, C("8a5a32")); }
                    for (int y = 2; y < 13; y++) { t.SetPixel(3, y, C("6e4423")); t.SetPixel(12, y, C("6e4423")); }
                    return t;
                }
                case "bridge":
                {
                    var t = Tile("water");
                    for (int y = 0; y < N; y++) for (int x = 1; x < 15; x++) t.SetPixel(x, y, (y % 4 == 0) ? C("6e4423") : C("a0693a"));
                    return t;
                }
                case "counter":
                {
                    var t = Tile("wood_floor");
                    for (int y = 3; y < 14; y++) for (int x = 0; x < N; x++) t.SetPixel(x, y, y > 11 ? C("c08a52") : C("7a4f2c"));
                    return t;
                }
                case "rug":
                {
                    var t = NewTex();
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                            t.SetPixel(x, y, (x == 1 || y == 1 || x == 14 || y == 14) ? C("e8c87a") : C("8e3b46"));
                    return t;
                }
                case "none": return NewTex();
                case "dark": { var t = NewTex(); for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) t.SetPixel(x, y, Color.black); return t; }
                case "grid":
                {
                    var t = NewTex();
                    var g = new Color32(255, 255, 255, 40);
                    for (int i = 0; i < N; i++) { t.SetPixel(i, 0, g); t.SetPixel(0, i, g); }
                    return t;
                }
                case "highlight":
                {
                    var t = NewTex();
                    var g = new Color32(120, 200, 255, 70);
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) t.SetPixel(x, y, g);
                    return t;
                }
                case "cursor":
                {
                    var t = NewTex();
                    var g = new Color32(255, 230, 120, 255);
                    for (int i = 0; i < N; i++)
                    {
                        if (i < 4 || i > 11) { t.SetPixel(i, 0, g); t.SetPixel(i, 15, g); t.SetPixel(0, i, g); t.SetPixel(15, i, g); }
                    }
                    return t;
                }
                default: return Missing();
            }
        }

        // ------------------------------------------------------------------ humanoids

        static readonly string[] Body =
        {
            "................",
            ".....OOOOOO.....",
            "....OHHHHHHO....",
            "....OHHHHHHO....",
            "....OHSSSSHO....",
            "....OSESSESO....",
            "....OSSSSSSO....",
            ".....OSSSSO.....",
            "....OTTTTTTO....",
            "...OSTTAATTSO...",
            "...OSTTAATTSO...",
            "...OOTTTTTTOO...",
            "....OPPPPPPO....",
            "....OPPOOPPO....",
            "....OBBOOBBO....",
            "................",
        };

        class Look
        {
            public Color32 Hair, Skin, Tunic, Accent, Pants, Boots, Hat;
            public bool PointyHat, Hood, Beard, Short, Ears, Helmet, Apron;
        }

        static Look LookFor(string id, out string dir, out int frame)
        {
            // id: {kind}_{a}_{b}_{dir}_{frame}  e.g. hero_fighter_dwarf_down_0, npc_brannoc_down_1
            var parts = id.Split('_');
            dir = "down"; frame = 0;
            int n = parts.Length;
            if (n >= 2 && int.TryParse(parts[n - 1], out int f)) { frame = f; n--; }
            if (n >= 2 && (parts[n - 1] == "down" || parts[n - 1] == "up" || parts[n - 1] == "side")) { dir = parts[n - 1]; n--; }
            string kind = parts[0];
            string a = n > 1 ? parts[1] : "";
            string b = n > 2 ? parts[2] : "human";
            if (n > 3) b = parts[2] + "_" + parts[3]; // half_elf

            var L = new Look { Skin = C("f1c7a0"), Hair = C("5a3a22"), Pants = C("4a4a6a"), Boots = C("3b2a1e"), Accent = C("e8c87a") };
            int seed = id.GetHashCode();
            if (kind == "hero" || kind == "ally")
            {
                switch (a)
                {
                    case "fighter": L.Tunic = C("8e9aa6"); L.Accent = C("b0453a"); L.Pants = C("5a5a6a"); L.Helmet = kind == "ally"; break;
                    case "wizard": L.Tunic = C("3e5ea8"); L.Accent = C("e8c87a"); L.Pants = C("2f467e"); L.PointyHat = true; L.Hat = C("2f467e"); break;
                    case "cleric": L.Tunic = C("eae4d6"); L.Accent = C("d9a441"); L.Pants = C("b5ab98"); break;
                    case "bard": L.Tunic = C("4f9a5a"); L.Accent = C("9b4fa8"); L.Pants = C("6a3e7a"); L.Hood = false; break;
                    default: L.Tunic = C("7a6a5a"); break;
                }
                switch (b)
                {
                    case "dwarf": L.Short = true; L.Beard = true; L.Hair = C("a8552e"); break;
                    case "gnome": L.Short = true; L.Hair = C("e8e0d0"); break;
                    case "elf": L.Ears = true; L.Hair = C("e8d27a"); L.Skin = C("f4d7bc"); break;
                    case "half_elf": L.Ears = true; L.Hair = C("2b2b3a"); break;
                    default: L.Hair = new[] { C("5a3a22"), C("2b2b3a"), C("c27a3a"), C("e8d27a") }[(int)(Hash(1, 1, seed) % 4)]; break;
                }
            }
            else // named NPCs
            {
                switch (a)
                {
                    case "brannoc": L.Short = true; L.Beard = true; L.Hair = C("8a8a8a"); L.Tunic = C("8a5a32"); L.Apron = true; L.Accent = C("c8b89a"); break;
                    case "elowen": L.Hair = C("d8d0c0"); L.Tunic = C("f2ecd8"); L.Accent = C("d9a441"); L.Hood = true; L.Hat = C("f2ecd8"); break;
                    case "pip": L.Short = true; L.Hair = C("c25247"); L.Tunic = C("e8c87a"); L.Apron = true; L.Accent = C("ffffff"); break;
                    case "rhoswen": L.Ears = true; L.Hair = C("2b2b3a"); L.Tunic = C("4a6fa5"); L.Accent = C("e8c87a"); L.Helmet = true; break;
                    case "villager": L.Tunic = new[] { C("7a9a5a"), C("a86a4a"), C("6a7aa8") }[(int)(Hash(2, 3, seed) % 3)]; break;
                    default:
                        L.Tunic = C("7a6a5a");
                        // generic recruits: npc_fighter / npc_wizard...
                        if (a == "fighter") { L.Tunic = C("8e9aa6"); L.Accent = C("4a6fa5"); }
                        if (a == "wizard") { L.Tunic = C("6a3e7a"); L.PointyHat = true; L.Hat = C("4a2a5a"); }
                        if (a == "cleric") { L.Tunic = C("eae4d6"); L.Accent = C("b0453a"); }
                        if (a == "bard") { L.Tunic = C("c27a3a"); L.Accent = C("4f9a5a"); }
                        break;
                }
            }
            if (L.Tunic.a == 0) L.Tunic = C("7a6a5a");
            return L;
        }

        static Texture2D Humanoid(string id)
        {
            var L = LookFor(id, out string dir, out int frame);
            var rows = (string[])Body.Clone();

            if (dir == "up")
            {
                rows[4] = "....OHHHHHHO....";
                rows[5] = "....OHHHHHHO....";
                rows[6] = "....OHHHHHHO....";
            }
            else if (dir == "side")
            {
                rows[4] = "....OHHSSSSO....";
                rows[5] = "....OHHSSESO....";
                rows[6] = "....OHSSSSSSO...";
            }
            if (L.Ears && dir != "up") rows[5] = "...SO" + rows[5].Substring(5, 6) + "OS...";
            if (L.Beard && dir != "up") { rows[6] = "....OHHHHHHO...."; rows[7] = ".....OHHHHO....."; }
            if (L.Apron) { rows[10] = "...OSTWWWWTSO..."; rows[11] = "...OOTWWWWTOO..."; }
            if (frame % 2 == 1)
            {
                rows[13] = "....OPPO.OPPO...";
                rows[14] = "...OBBO...OBBO..";
            }

            var pal = new Dictionary<char, Color32>
            {
                { 'O', Outline }, { 'H', L.Hair }, { 'S', L.Skin }, { 'E', C("2b1d24") }, { 'T', L.Tunic },
                { 'A', L.Accent }, { 'P', L.Pants }, { 'B', L.Boots }, { 'W', C("f2ecd8") },
            };

            var t = NewTex();
            int yOff = L.Short ? -2 : 0;
            if (L.Short)
            {
                // squash: drop the neck row and one torso row so dwarves and gnomes read as shorter
                var list = new List<string>(rows);
                list.RemoveAt(7);
                list.RemoveAt(10);
                list.Insert(0, "................");
                list.Insert(0, "................");
                rows = list.ToArray();
                yOff = 0;
            }
            Stamp(t, rows, pal, 0, yOff);

            int top = L.Short ? 13 : 15;
            if (L.PointyHat)
            {
                var hat = new Dictionary<char, Color32> { { 'O', Outline }, { 'W', L.Hat }, { 'Y', C("e8c87a") } };
                Stamp(t, new[]
                {
                    ".......OO.......",
                    "......OWWO......",
                    ".....OWWWWO.....",
                    "....OWWYWWWO....",
                    "...OOWWWWWWOO...",
                }, hat, 0, top - 4 + (L.Short ? 0 : 0) - 0);
            }
            if (L.Hood)
            {
                var hood = new Dictionary<char, Color32> { { 'O', Outline }, { 'W', L.Hat } };
                Stamp(t, new[] { ".....OOOOOO.....", "....OWWWWWWO....", "...OWWWWWWWWO..." }, hood, 0, top - 3);
            }
            if (L.Helmet)
            {
                var helm = new Dictionary<char, Color32> { { 'O', Outline }, { 'M', C("b8c0c8") }, { 'm', C("8e9aa6") } };
                Stamp(t, new[] { ".....OOOOOO.....", "....OMMMMMMO....", "....OmmmmmmO...." }, helm, 0, top - 3);
            }
            if (frame % 2 == 1)
            {
                // tiny bob: shift everything down one pixel by re-reading
                var px = t.GetPixels32();
                var t2 = NewTex();
                for (int y = 1; y < N; y++) for (int x = 0; x < N; x++) t2.SetPixel(x, y - 1, px[y * N + x]);
                return t2;
            }
            return t;
        }

        // ------------------------------------------------------------------ monsters

        static readonly string[] SmallCreature =
        {
            "................",
            "................",
            ".....O....O.....",
            ".....OO..OO.....",
            "....ORRRRRRO....",
            "...ORRERRERRO...",
            "...ORRRRRRRRO...",
            "....ORRYYRRO....",
            ".....ORRRRO.....",
            "....OLRRRRLO....",
            "...OLRRRRRRLO...",
            "...OORRRRRROO...",
            "....ORRRRRRO....",
            "....OLLOOLLO....",
            "....OOO..OOO....",
            "................",
        };

        static readonly string[] Quadruped =
        {
            "................",
            "................",
            "................",
            "................",
            "..OO............",
            ".OFFO...........",
            "OFEFFOOOOOOO....",
            "OFFFFFFFFFFFO...",
            ".OOFFFFFFFFFFO.O",
            "...OFFFFFFFFFOFO",
            "...OFFFFFFFFFFO.",
            "...OFOOFOOOFOFO.",
            "...OFO.FO..OFO..",
            "...OO..OO..OO...",
            "................",
            "................",
        };

        static Texture2D Monster(string id)
        {
            string m = id.Substring(4);
            int us = m.LastIndexOf('_');
            if (us > 0 && int.TryParse(m.Substring(us + 1), out _)) m = m.Substring(0, us);
            var t = NewTex();
            switch (m)
            {
                case "kobold":
                case "kobold_slinger":
                case "snik":
                {
                    var pal = new Dictionary<char, Color32> { { 'O', Outline }, { 'R', C("b5523a") }, { 'L', C("8a3b2a") }, { 'E', C("f2d14e") }, { 'Y', C("e8c8a0") } };
                    if (m == "kobold_slinger") { pal['R'] = C("a8603a"); pal['L'] = C("7a4022"); }
                    Stamp(t, SmallCreature, pal);
                    if (m == "snik")
                    {
                        var pot = new Dictionary<char, Color32> { { 'O', Outline }, { 'M', C("9aa3ad") }, { 'H', C("5e5a56") } };
                        Stamp(t, new[] { "....OOOOOOO.....", "...OMMMMMMMOHHH.", "...OMMMMMMMO...." }, pot, 0, 10);
                    }
                    return t;
                }
                case "goblin":
                {
                    var pal = new Dictionary<char, Color32> { { 'O', Outline }, { 'R', C("6a9a3a") }, { 'L', C("5a5a3a") }, { 'E', C("f25a3a") }, { 'Y', C("8ab45a") } };
                    Stamp(t, SmallCreature, pal);
                    t.SetPixel(2, 11, C("6a9a3a")); t.SetPixel(13, 11, C("6a9a3a"));
                    return t;
                }
                case "rat":
                {
                    var pal = new Dictionary<char, Color32> { { 'O', Outline }, { 'F', C("7a6a62") }, { 'E', C("e83a3a") } };
                    Stamp(t, Quadruped, pal, 0, -2);
                    return t;
                }
                case "wolf":
                {
                    var pal = new Dictionary<char, Color32> { { 'O', Outline }, { 'F', C("9a9a9a") }, { 'E', C("f2d14e") } };
                    Stamp(t, Quadruped, pal);
                    return t;
                }
                case "dragonling":
                {
                    var pal = new Dictionary<char, Color32> { { 'O', Outline }, { 'R', C("e86a3a") }, { 'L', C("c24a2a") }, { 'E', C("2b1d24") }, { 'Y', C("f7c86a") }, { 'W', C("f7a86a") } };
                    Stamp(t, new[]
                    {
                        "................",
                        "................",
                        "....O......O....",
                        "...ORO....ORO...",
                        "....ORRRRRRO....",
                        "...ORRERRERRO...",
                        "...ORRRYYRRRO...",
                        "OO..ORRRRRRO..OO",
                        "OWO.OLRRRRLO.OWO",
                        "OWWOLRRYYRRLOWWO",
                        ".OWWORRYYRROWWO.",
                        "..OOORRRRRROOO..",
                        "....ORRRRRRO.OO.",
                        "....OLLOOLLOOO..",
                        "....OOO..OOO....",
                        "................",
                    }, pal);
                    return t;
                }
                default: return Missing();
            }
        }

        // ------------------------------------------------------------------ objects

        static Texture2D Obj(string kind)
        {
            var t = NewTex();
            switch (kind)
            {
                case "tree":
                {
                    for (int y = 0; y < 5; y++) for (int x = 6; x < 10; x++) t.SetPixel(x, y, x == 6 ? C("5a3a22") : C("7a4f2c"));
                    for (int y = 4; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                        {
                            float dx = x - 7.5f, dy = y - 10f;
                            float d = dx * dx / 56f + dy * dy / 34f;
                            if (d < 1f) t.SetPixel(x, y, d > 0.8f ? (Color)Outline : (Rand(x, y, 9) < 0.2f ? (Color)C("2f6a2a") : (dy > 2 ? (Color)C("4f9a3a") : (Color)C("3f8030"))));
                        }
                    return t;
                }
                case "bush":
                {
                    for (int y = 0; y < 10; y++) for (int x = 1; x < 15; x++)
                        {
                            float dx = x - 7.5f, dy = y - 4.5f;
                            float d = dx * dx / 48f + dy * dy / 22f;
                            if (d < 1f) t.SetPixel(x, y, d > 0.75f ? (Color)Outline : (Rand(x, y, 3) < 0.25f ? (Color)C("3f8030") : (Color)C("4f9a3a")));
                        }
                    return t;
                }
                case "rock":
                {
                    for (int y = 0; y < 9; y++) for (int x = 2; x < 14; x++)
                        {
                            float dx = x - 7.5f, dy = y - 4f;
                            float d = dx * dx / 36f + dy * dy / 18f;
                            if (d < 1f) t.SetPixel(x, y, d > 0.7f ? (Color)Outline : (dy > 1 ? (Color)C("a8a29a") : (Color)C("8a857e")));
                        }
                    return t;
                }
                case "bed":
                {
                    for (int y = 0; y < 15; y++) for (int x = 2; x < 14; x++)
                        {
                            Color c = C("8a5a32");
                            if (x > 2 && x < 13 && y > 0 && y < 14) c = y > 10 ? (Color)C("f2ecd8") : (Color)C("4a6fa5");
                            t.SetPixel(x, y, c);
                        }
                    return t;
                }
                case "chest":
                case "chest_open":
                {
                    for (int y = 1; y < 11; y++) for (int x = 2; x < 14; x++)
                            t.SetPixel(x, y, (y == 1 || y == 10 || x == 2 || x == 13) ? (Color)Outline : (y == 6 ? (Color)C("d9a441") : (Color)C("8a5a32")));
                    t.SetPixel(7, 6, C("f7e08a")); t.SetPixel(8, 6, C("f7e08a"));
                    if (kind == "chest_open") for (int x = 3; x < 13; x++) t.SetPixel(x, 9, C("1e1615"));
                    return t;
                }
                case "sign":
                {
                    for (int y = 0; y < 6; y++) t.SetPixel(7, y, C("6e4423"));
                    for (int y = 6; y < 13; y++) for (int x = 2; x < 14; x++) t.SetPixel(x, y, (y == 6 || y == 12 || x == 2 || x == 13) ? (Color)Outline : (Color)C("c08a52"));
                    return t;
                }
                case "barrel":
                {
                    for (int y = 0; y < 13; y++) for (int x = 3; x < 13; x++)
                            t.SetPixel(x, y, (x == 3 || x == 12) ? (Color)Outline : ((y == 3 || y == 9) ? (Color)C("5e5a56") : (Color)C("8a5a32")));
                    return t;
                }
                case "table":
                {
                    for (int y = 6; y < 12; y++) for (int x = 1; x < 15; x++) t.SetPixel(x, y, (y == 6 || y == 11) ? (Color)C("6e4423") : (Color)C("a0693a"));
                    for (int y = 0; y < 6; y++) { t.SetPixel(2, y, C("6e4423")); t.SetPixel(13, y, C("6e4423")); }
                    return t;
                }
                case "altar":
                {
                    for (int y = 0; y < 10; y++) for (int x = 2; x < 14; x++) t.SetPixel(x, y, (y == 9) ? (Color)C("d9a441") : (Color)C("eae4d6"));
                    for (int y = 10; y < 15; y++) t.SetPixel(7, y, C("f7e08a"));
                    for (int x = 5; x < 10; x++) t.SetPixel(x, 12, C("f7e08a"));
                    return t;
                }
                case "egg":
                case "egg_glow":
                {
                    for (int y = 0; y < 14; y++) for (int x = 2; x < 14; x++)
                        {
                            float dx = x - 7.5f, dy = y - 6.5f;
                            float d = dx * dx / 25f + dy * dy / 49f;
                            if (d < 1f)
                            {
                                Color c = d > 0.8f ? (Color)Outline : (Rand(x, y, 5) < 0.18f ? (Color)C("e86a3a") : (Color)C("c24a2a"));
                                if (kind == "egg_glow" && d < 0.3f) c = C("f7c86a");
                                t.SetPixel(x, y, c);
                            }
                        }
                    return t;
                }
                case "lantern":
                {
                    for (int y = 0; y < 14; y++) t.SetPixel(7, y, C("3b2a1e"));
                    for (int y = 10; y < 15; y++) for (int x = 5; x < 10; x++) t.SetPixel(x, y, C("f7c86a"));
                    return t;
                }
                case "crate":
                {
                    for (int y = 0; y < 12; y++) for (int x = 2; x < 14; x++)
                            t.SetPixel(x, y, (x == 2 || x == 13 || y == 0 || y == 11 || x == y + 2) ? (Color)C("6e4423") : (Color)C("b07843"));
                    return t;
                }
                default: return Missing();
            }
        }

        // ------------------------------------------------------------------ item icons

        static void Line(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                if (x0 >= 0 && y0 >= 0 && x0 < N && y0 < N) t.SetPixel(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static void Disc(Texture2D t, float cx, float cy, float r, Color fill, Color edge)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d < r) t.SetPixel(x, y, d > r - 1.2f ? edge : fill);
                }
        }

        static Texture2D Item(string id)
        {
            var t = NewTex();
            Color steel = C("c8d0d8"), dark = C("5e6a74"), wood = C("8a5a32"), gold = C("d9a441");
            string cat = id;
            if (id.Contains("sword") || id == "scimitar" || id == "rapier" || id == "emberwrought_blade" || id == "glaive" || id == "halberd") cat = "sword";
            else if (id.Contains("axe")) cat = "axe";
            else if (id.Contains("staff") || id == "spear" || id == "javelin" || id == "trident" || id == "quarterstaff") cat = "staff";
            else if (id.Contains("bow")) cat = "bow";
            else if (id == "mace" || id == "club" || id.Contains("hammer") || id == "maul" || id == "flail" || id == "morningstar" || id == "greatclub" || id == "war_pick") cat = "mace";
            else if (id == "dagger" || id == "sickle" || id == "whip" || id == "sling") cat = "dagger";
            else if (id.Contains("armor") || id.Contains("mail") || id == "leather" || id == "padded" || id == "hide" || id == "breastplate" || id == "plate" || id == "splint" || id.Contains("shirt") || id.Contains("leather")) cat = "armor";
            else if (id.StartsWith("potion")) cat = "potion";
            else if (id.StartsWith("scroll")) cat = "scroll";

            switch (cat)
            {
                case "sword":
                    Line(t, 3, 3, 12, 12, id == "emberwrought_blade" ? (Color)C("f7a86a") : steel);
                    Line(t, 4, 3, 12, 11, dark);
                    Line(t, 2, 6, 6, 2, gold);
                    t.SetPixel(2, 2, wood); t.SetPixel(1, 1, gold);
                    break;
                case "axe":
                    Line(t, 3, 2, 11, 12, wood);
                    for (int y = 9; y < 15; y++) for (int x = 10; x < 15; x++) if (x - 10 <= (y - 8)) t.SetPixel(x, y, steel);
                    break;
                case "staff":
                    Line(t, 3, 2, 12, 13, wood); Line(t, 4, 2, 13, 13, C("6e4423"));
                    if (id == "starfall_staff") Disc(t, 12.5f, 13f, 2.5f, C("9fd3e8"), C("ffffff"));
                    else if (id != "quarterstaff") Line(t, 11, 13, 14, 15, steel);
                    break;
                case "bow":
                    for (int i = 0; i < 12; i++) t.SetPixel(3 + (int)(Mathf.Sin(i / 11f * Mathf.PI) * 4), 2 + i, wood);
                    Line(t, 3, 2, 3, 13, C("e8e0d0"));
                    break;
                case "mace":
                    Line(t, 3, 2, 10, 9, wood);
                    Disc(t, 11.5f, 11.5f, 3.2f, steel, dark);
                    break;
                case "dagger":
                    Line(t, 5, 5, 11, 11, steel); Line(t, 3, 6, 6, 3, gold); Line(t, 3, 3, 4, 4, wood);
                    break;
                case "armor":
                {
                    Color body = id.Contains("leather") || id == "padded" || id == "hide" ? (Color)C("8a5a32") : steel;
                    for (int y = 2; y < 13; y++) for (int x = 3; x < 13; x++) t.SetPixel(x, y, (x == 3 || x == 12 || y == 2) ? (Color)Outline : body);
                    for (int x = 1; x < 15; x++) for (int y = 10; y < 14; y++) if (x < 4 || x > 11) t.SetPixel(x, y, body);
                    for (int y = 9; y < 13; y++) for (int x = 6; x < 10; x++) t.SetPixel(x, y, Clear);
                    break;
                }
                case "shield":
                    for (int y = 1; y < 15; y++) for (int x = 3; x < 13; x++)
                        {
                            if (Mathf.Abs(x - 7.5f) < 5 - Mathf.Max(0, 6 - y) * 0.6f) t.SetPixel(x, y, (x == 3 || x == 12 || y == 14) ? (Color)Outline : (Color)C("4a6fa5"));
                        }
                    Line(t, 8, 3, 8, 12, gold); Line(t, 5, 9, 11, 9, gold);
                    break;
                case "potion":
                    Disc(t, 7.5f, 5.5f, 5f, id.Contains("greater") ? (Color)C("b0302a") : (Color)C("e84a4a"), Outline);
                    for (int y = 10; y < 14; y++) for (int x = 6; x < 10; x++) t.SetPixel(x, y, C("c8e8f0"));
                    for (int x = 5; x < 11; x++) t.SetPixel(x, 14, wood);
                    t.SetPixel(5, 7, Color.white);
                    break;
                case "scroll":
                    for (int y = 3; y < 13; y++) for (int x = 3; x < 13; x++) t.SetPixel(x, y, C("efdea6"));
                    for (int x = 2; x < 14; x++) { t.SetPixel(x, 2, wood); t.SetPixel(x, 13, wood); }
                    for (int y = 5; y < 11; y += 2) Line(t, 5, y, 10, y, C("8a7a5a"));
                    break;
                case "ember_of_return":
                    Disc(t, 7.5f, 7.5f, 5.5f, C("e86a3a"), C("8a2a1a"));
                    Disc(t, 7.5f, 8.5f, 2.5f, C("f7e08a"), C("f7c86a"));
                    break;
                case "torch":
                    Line(t, 5, 1, 10, 9, wood); Disc(t, 10.5f, 11.5f, 3f, C("f7c86a"), C("e86a3a"));
                    break;
                case "rations":
                    for (int y = 3; y < 11; y++) for (int x = 2; x < 14; x++) t.SetPixel(x, y, (y == 3 || y == 10) ? (Color)C("6e4423") : (Color)C("c8a87a"));
                    Line(t, 8, 3, 8, 10, C("8a3b2a"));
                    break;
                case "gemstone": Disc(t, 7.5f, 7.5f, 5f, C("4ac8e8"), C("2a7a9a")); t.SetPixel(6, 9, Color.white); break;
                case "copper_trinket": Disc(t, 7.5f, 7.5f, 4.5f, C("c87a4a"), C("8a4a2a")); break;
                case "egg_shard":
                    for (int y = 3; y < 12; y++) for (int x = 4; x < 12; x++) if (x + y < 18 && x - y > -6) t.SetPixel(x, y, C("c24a2a"));
                    break;
                case "veilthread_cloak":
                    for (int y = 1; y < 15; y++) for (int x = 3; x < 13; x++) if (Mathf.Abs(x - 7.5f) < 2 + y * 0.25f) t.SetPixel(x, y, Rand(x, y, 4) < 0.3f ? (Color)C("b0a0e0") : (Color)C("6a5aa8"));
                    break;
                case "oathbound_signet": Disc(t, 7.5f, 7.5f, 5f, gold, Outline); Disc(t, 7.5f, 7.5f, 3f, Clear, gold); t.SetPixel(7, 12, C("4ac8e8")); break;
                case "dawnmothers_charm":
                    Line(t, 2, 14, 7, 9, gold); Line(t, 13, 14, 8, 9, gold);
                    Disc(t, 7.5f, 6f, 3.5f, C("f7e08a"), gold);
                    break;
                case "starfall_wand": Line(t, 3, 3, 11, 11, C("e8e0d0")); Disc(t, 12f, 12f, 2.5f, C("9fd3e8"), Color.white); break;
                case "fennicks_lucky_copper": Disc(t, 7.5f, 7.5f, 5.5f, C("c87a4a"), C("8a4a2a")); t.SetPixel(6, 8, Outline); t.SetPixel(9, 8, Outline); Line(t, 6, 6, 9, 6, Outline); break;
                default: Disc(t, 7.5f, 7.5f, 5f, C("9a9a9a"), Outline); break;
            }
            return t;
        }

        // ------------------------------------------------------------------ UI bits

        static Texture2D Ui(string kind)
        {
            switch (kind)
            {
                case "panel":
                {
                    var t = NewTex();
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            bool edge = x == 0 || y == 0 || x == N - 1 || y == N - 1;
                            bool inner = x == 1 || y == 1 || x == N - 2 || y == N - 2;
                            t.SetPixel(x, y, edge ? (Color)C("3b2a1e") : inner ? (Color)C("c08a52") : (Color)C("f2e2c2"));
                        }
                    return t;
                }
                case "panel_dark":
                {
                    var t = NewTex();
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            bool edge = x == 0 || y == 0 || x == N - 1 || y == N - 1;
                            t.SetPixel(x, y, edge ? (Color)C("e8c87a") : new Color32(30, 22, 26, 225));
                        }
                    return t;
                }
                case "slot":
                {
                    var t = NewTex();
                    for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                        {
                            bool edge = x == 0 || y == 0 || x == N - 1 || y == N - 1;
                            t.SetPixel(x, y, edge ? (Color)C("8a5a32") : (Color)C("e3cf9f"));
                        }
                    return t;
                }
                case "die":
                {
                    var t = NewTex(32, 32);
                    for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        {
                            // hexagon (d20 silhouette)
                            float dx = Mathf.Abs(x - 15.5f), dy = Mathf.Abs(y - 15.5f);
                            bool inside = dy < 15 && dx < 14 && dx * 0.55f + dy < 16.5f;
                            bool edge = inside && !(dy < 13.5f && dx < 12.5f && dx * 0.55f + dy < 15f);
                            if (inside) t.SetPixel(x, y, edge ? Color.black : Color.white);
                        }
                    return t;
                }
                case "white":
                {
                    var t = NewTex(4, 4);
                    for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
                    return t;
                }
                case "glow":
                {
                    var t = NewTex(64, 64);
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                        {
                            float d = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
                            float a = Mathf.Clamp01(1 - d);
                            t.SetPixel(x, y, new Color(1, 1, 1, a * a));
                        }
                    return t;
                }
                default: return Missing();
            }
        }
    }
}
