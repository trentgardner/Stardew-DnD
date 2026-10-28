using System;

namespace DnD.Game
{
    public enum TileShape { Single, Blob, Rect }

    /// <summary>
    /// Picks the edge/corner/strip variant of a terrain tile from its same-terrain neighbors, so paths,
    /// water and sand get soft borders and roofs, walls and cliffs get proper ends. Variants are plain
    /// ArtRegistry ids (e.g. "tile_path_ne"); callers fall back to the base id when a variant has no art.
    /// same(dx, dy): is the neighbor at that offset the same terrain? dy = +1 is SOUTH (map rows grow southward).
    /// </summary>
    public static class TileStyler
    {
        public static string Variant(string baseId, TileShape shape, Func<int, int, bool> same, int x, int y)
        {
            switch (shape)
            {
                case TileShape.Blob: return baseId + "_" + Blob(same);
                case TileShape.Rect: return baseId + "_" + Rect(same);
                default: return baseId;
            }
        }

        static string Blob(Func<int, int, bool> same)
        {
            bool n = same(0, -1), s = same(0, 1), e = same(1, 0), w = same(-1, 0);
            if (!n && !s && !e && !w) return "dot";
            if (!e && !w) return n && s ? "v_m" : n ? "v_s" : "v_n";
            if (!n && !s) return e && w ? "h_m" : e ? "h_w" : "h_e";
            if (!n && !w) return "nw";
            if (!n && !e) return "ne";
            if (!s && !w) return "sw";
            if (!s && !e) return "se";
            if (!n) return "n";
            if (!s) return "s";
            if (!w) return "w";
            if (!e) return "e";
            if (!same(1, -1)) return "ine";
            if (!same(-1, -1)) return "inw";
            if (!same(1, 1)) return "ise";
            if (!same(-1, 1)) return "isw";
            return "c";
        }

        static string Rect(Func<int, int, bool> same)
        {
            bool l = !same(-1, 0), r = !same(1, 0), top = !same(0, -1), bot = !same(0, 1);
            string col = l && !r ? "l" : r && !l ? "r" : "m";
            string row = bot ? "bot" : top ? "top" : "mid";
            return col + "_" + row;
        }
    }
}
