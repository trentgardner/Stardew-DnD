using System;
using DnD.Game;
using NUnit.Framework;

namespace DnD.Tests
{
    public class TileStylerTests
    {
        /// <summary>Variant for the cell at (x, y) of a little ASCII map; '#' is the terrain being styled. Row 0 is north.</summary>
        static string At(string[] map, int x, int y, TileShape shape)
        {
            Func<int, int, bool> same = (dx, dy) =>
            {
                int nx = x + dx, ny = y + dy;
                return ny >= 0 && ny < map.Length && nx >= 0 && nx < map[ny].Length && map[ny][nx] == '#';
            };
            return TileStyler.Variant("t", shape, same, x, y);
        }

        [Test]
        public void BlobPicksOuterCornerAndInnerCorner()
        {
            var square = new[] { ".....", ".###.", ".###.", ".###.", "....." };
            Assert.AreEqual("t_ne", At(square, 3, 1, TileShape.Blob));
            Assert.AreEqual("t_nw", At(square, 1, 1, TileShape.Blob));
            Assert.AreEqual("t_se", At(square, 3, 3, TileShape.Blob));
            Assert.AreEqual("t_n", At(square, 2, 1, TileShape.Blob));
            Assert.AreEqual("t_w", At(square, 1, 2, TileShape.Blob));
            Assert.AreEqual("t_c", At(square, 2, 2, TileShape.Blob));

            // A big L: the cell at the elbow has all four sides filled but its north-east diagonal is empty.
            var ell = new[] { "###..", "###..", "#####", "#####", "#####" };
            Assert.AreEqual("t_ine", At(ell, 2, 2, TileShape.Blob));
        }

        [Test]
        public void BlobHandlesOneWideRuns()
        {
            var col = new[] { ".#.", ".#.", ".#.", "..." };
            Assert.AreEqual("t_v_n", At(col, 1, 0, TileShape.Blob));
            Assert.AreEqual("t_v_m", At(col, 1, 1, TileShape.Blob));
            Assert.AreEqual("t_v_s", At(col, 1, 2, TileShape.Blob));
            var row = new[] { "....", "###.", "...." };
            Assert.AreEqual("t_h_w", At(row, 0, 1, TileShape.Blob));
            Assert.AreEqual("t_h_m", At(row, 1, 1, TileShape.Blob));
            Assert.AreEqual("t_h_e", At(row, 2, 1, TileShape.Blob));
            var dot = new[] { "...", ".#.", "..." };
            Assert.AreEqual("t_dot", At(dot, 1, 1, TileShape.Blob));
        }

        [Test]
        public void RectPicksRoofPieces()
        {
            var roof = new[] { "#####", "#####", "#####" };
            Assert.AreEqual("t_l_top", At(roof, 0, 0, TileShape.Rect));
            Assert.AreEqual("t_m_top", At(roof, 2, 0, TileShape.Rect));
            Assert.AreEqual("t_m_mid", At(roof, 2, 1, TileShape.Rect));
            Assert.AreEqual("t_r_bot", At(roof, 4, 2, TileShape.Rect));
            var flat = new[] { "###" };
            Assert.AreEqual("t_l_bot", At(flat, 0, 0, TileShape.Rect), "a 1-high run uses the bottom row");
        }

        [Test]
        public void SingleIsJustTheBaseId()
        {
            Assert.AreEqual("t", At(new[] { "#" }, 0, 0, TileShape.Single));
        }
    }
}
