using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Slices the Ninja Adventure pack (ArtSource/, gitignored) into one PNG per ArtRegistry id under
/// Assets/Resources/Art, driven by Assets/Editor/ArtMap.txt. ArtImporter then applies the pixel-art
/// import settings. Run from the Embers menu or: -executeMethod ArtSlicer.ImportAll
///
/// ArtMap.txt lines (# comments; paths relative to the pack root; positions in 16 px tiles):
///   tile  &lt;id&gt; &lt;sheet&gt; &lt;col&gt; &lt;row&gt; [w h]          one sprite from a tile rect (row 0 = top)
///   px    &lt;id&gt; &lt;sheet&gt; &lt;x&gt; &lt;y&gt; &lt;w&gt; &lt;h&gt;           one sprite from a pixel rect (y from top)
///   actor &lt;prefix&gt; &lt;sheet&gt;                       4-column walk sheet (down, up, left, right; rows = frames 0-3)
///                                                  → {prefix}_down_0..3, _up_0..3, _side_0..3 (side = right column)
///   still &lt;prefix&gt; &lt;sheet&gt; [frames=2]            non-directional actor: down-column frames → {prefix}_0..n-1
///   tint  &lt;id&gt; &lt;fromId&gt; &lt;hueDeg&gt; [sat×] [val×]  recolored copy of an id already written by this run
///   over  &lt;id&gt; &lt;baseId&gt; &lt;sheet&gt; &lt;col&gt; &lt;row&gt;     a tile drawn over an id already written (flowers on grass...)
///   recolor &lt;sheet&gt; &lt;hueDeg&gt; &lt;r×&gt; &lt;g×&gt; &lt;b×&gt;     recolors a whole sheet for the lines after it (hue shift, then multiply)
///   audio &lt;id&gt; &lt;file&gt;                             copies a sound to Assets/Resources/Audio/{id}.{ext}
/// </summary>
public static class ArtSlicer
{
    public const string PackRoot = "ArtSource/NinjaAdventure/Ninja Adventure - Asset Pack";
    const string MapPath = "Assets/Editor/ArtMap.txt";
    const string OutDir = "Assets/Resources/Art";
    const string AudioDir = "Assets/Resources/Audio";
    const int T = 16;

    [MenuItem("Embers/Import Art (Ninja Adventure)")]
    public static void ImportAll()
    {
        if (!Directory.Exists(PackRoot)) { Debug.LogError($"Art pack not found at {PackRoot}. See docs/ART_IMPORT_GUIDE.md."); return; }
        Directory.CreateDirectory(OutDir);
        Directory.CreateDirectory(AudioDir);
        var sheets = new Dictionary<string, Texture2D>();
        var written = new Dictionary<string, Texture2D>();
        int count = 0, lineNo = 0;
        foreach (var raw in File.ReadAllLines(MapPath))
        {
            lineNo++;
            var line = raw.Split('#')[0].Trim();
            if (line.Length == 0) continue;
            var a = Tokens(line);
            try
            {
                switch (a[0])
                {
                    case "tile":
                    {
                        int w = a.Count > 5 ? int.Parse(a[5]) : 1, h = a.Count > 6 ? int.Parse(a[6]) : 1;
                        count += Write(written, a[1], Crop(Sheet(sheets, a[2]), int.Parse(a[3]) * T, int.Parse(a[4]) * T, w * T, h * T));
                        break;
                    }
                    case "px":
                        count += Write(written, a[1], Crop(Sheet(sheets, a[2]), int.Parse(a[3]), int.Parse(a[4]), int.Parse(a[5]), int.Parse(a[6])));
                        break;
                    case "actor":
                    {
                        var s = Sheet(sheets, a[2]);
                        for (int f = 0; f < 4; f++)
                        {
                            count += Write(written, $"{a[1]}_down_{f}", Crop(s, 0 * T, f * T, T, T));
                            count += Write(written, $"{a[1]}_up_{f}", Crop(s, 1 * T, f * T, T, T));
                            count += Write(written, $"{a[1]}_side_{f}", Crop(s, 3 * T, f * T, T, T));
                        }
                        break;
                    }
                    case "still":
                    {
                        var s = Sheet(sheets, a[2]);
                        int frames = a.Count > 3 ? int.Parse(a[3]) : 2;
                        // Non-directional actors face right and are flipped by the game: use the right-facing column.
                        for (int f = 0; f < frames; f++) count += Write(written, $"{a[1]}_{f}", Crop(s, 3 * T, f * T, T, T));
                        break;
                    }
                    case "tint":
                    {
                        if (!written.TryGetValue(a[2], out var src)) throw new Exception($"tint source {a[2]} not written earlier in ArtMap");
                        float sat = a.Count > 4 ? float.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture) : 1f;
                        float val = a.Count > 5 ? float.Parse(a[5], System.Globalization.CultureInfo.InvariantCulture) : 1f;
                        count += Write(written, a[1], Tint(src, float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture), sat, val));
                        break;
                    }
                    case "over":
                    {
                        if (!written.TryGetValue(a[2], out var under)) throw new Exception($"over base {a[2]} not written earlier in ArtMap");
                        var top = Crop(Sheet(sheets, a[3]), int.Parse(a[4]) * T, int.Parse(a[5]) * T, T, T);
                        count += Write(written, a[1], Over(under, top));
                        break;
                    }
                    case "recolor":
                    {
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        var src = Sheet(sheets, a[1]);
                        var tinted = Tint(src, float.Parse(a[2], inv), 1f, 1f);
                        var px = tinted.GetPixels();
                        var mul = new Color(float.Parse(a[3], inv), float.Parse(a[4], inv), float.Parse(a[5], inv), 1);
                        for (int i = 0; i < px.Length; i++) px[i] *= mul;
                        tinted.SetPixels(px);
                        tinted.Apply();
                        sheets[a[1]] = tinted;
                        break;
                    }
                    case "audio":
                    {
                        string from = Path.Combine(PackRoot, a[2]);
                        File.Copy(from, Path.Combine(AudioDir, a[1] + Path.GetExtension(from)), true);
                        count++;
                        break;
                    }
                    default: throw new Exception("unknown directive " + a[0]);
                }
            }
            catch (Exception e) { Debug.LogError($"{MapPath}:{lineNo}: {e.Message}  ({raw.Trim()})"); }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log($"ArtSlicer: wrote {count} files.");
    }

    /// <summary>Splits on whitespace but keeps "quoted paths with spaces" together.</summary>
    static List<string> Tokens(string line)
    {
        var list = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            if (char.IsWhiteSpace(line[i])) { i++; continue; }
            if (line[i] == '"') { int end = line.IndexOf('"', i + 1); list.Add(line.Substring(i + 1, end - i - 1)); i = end + 1; continue; }
            int j = i; while (j < line.Length && !char.IsWhiteSpace(line[j])) j++;
            list.Add(line.Substring(i, j - i)); i = j;
        }
        return list;
    }

    static Texture2D Sheet(Dictionary<string, Texture2D> cache, string rel)
    {
        if (cache.TryGetValue(rel, out var t)) return t;
        string path = Path.Combine(PackRoot, rel);
        if (!File.Exists(path)) throw new FileNotFoundException("sheet not found: " + rel);
        t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path));
        cache[rel] = t;
        return t;
    }

    /// <summary>Copies a rect given in top-left pixel coordinates (as the sheets are drawn).</summary>
    static Texture2D Crop(Texture2D s, int x, int yTop, int w, int h)
    {
        if (x < 0 || yTop < 0 || x + w > s.width || yTop + h > s.height) throw new Exception($"rect {x},{yTop} {w}x{h} outside {s.width}x{s.height}");
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(s.GetPixels(x, s.height - yTop - h, w, h));
        t.Apply();
        return t;
    }

    static Texture2D Tint(Texture2D src, float hueDeg, float sat, float val)
    {
        var px = src.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            if (c.a <= 0) continue;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (s < 0.08f) continue; // keep outlines, whites and greys
            h = Mathf.Repeat(h + hueDeg / 360f, 1f);
            var n = Color.HSVToRGB(h, Mathf.Clamp01(s * sat), Mathf.Clamp01(v * val));
            n.a = c.a;
            px[i] = n;
        }
        var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    static Texture2D Over(Texture2D under, Texture2D top)
    {
        var a = under.GetPixels();
        var b = top.GetPixels();
        for (int i = 0; i < a.Length; i++) a[i] = Color.Lerp(a[i], new Color(b[i].r, b[i].g, b[i].b, 1), b[i].a);
        var t = new Texture2D(under.width, under.height, TextureFormat.RGBA32, false);
        t.SetPixels(a);
        t.Apply();
        return t;
    }

    static int Write(Dictionary<string, Texture2D> written, string id, Texture2D tex)
    {
        written[id] = tex;
        File.WriteAllBytes(Path.Combine(OutDir, id + ".png"), tex.EncodeToPNG());
        return 1;
    }
}
