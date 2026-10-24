using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>A light in the world (lantern, glowing egg, torch carried by an avatar).</summary>
    public class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();
        /// <summary>Radius in tiles (bright for the first half, dim to the edge, like D&D bright/dim light).</summary>
        public float Radius = 4f;
        public Color Tint = Color.white;
        public bool On = true;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static LightSource Attach(GameObject go, float radius, Color tint)
        {
            var l = go.AddComponent<LightSource>();
            l.Radius = radius;
            l.Tint = tint;
            return l;
        }
    }

    /// <summary>
    /// Per-player darkness for dark zones: a grid of black squares whose opacity depends on nearby lights
    /// and on the viewing player's darkvision. Lives on a layer only that player's camera renders.
    /// </summary>
    public class DarknessOverlay : MonoBehaviour
    {
        PlayerSlot _slot;
        Zone _zone;
        SpriteRenderer[,] _cells;

        public static DarknessOverlay Create(PlayerSlot slot)
        {
            var go = new GameObject($"Darkness_P{slot.Index + 1}");
            DontDestroyOnLoad(go);
            var d = go.AddComponent<DarknessOverlay>();
            d._slot = slot;
            return d;
        }

        void Rebuild(Zone z)
        {
            foreach (Transform c in transform) Destroy(c.gameObject);
            _zone = z;
            _cells = null;
            if (z == null || !z.Def.Dark) return;
            _cells = new SpriteRenderer[z.W, z.H];
            var spr = ArtRegistry.Get("tile_dark");
            for (int x = 0; x < z.W; x++)
                for (int y = 0; y < z.H; y++)
                {
                    var go = new GameObject("d");
                    go.layer = _slot.DarkLayer;
                    go.transform.SetParent(transform, false);
                    go.transform.position = z.ToWorld(new Vector2Int(x, y));
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    sr.sortingOrder = 30000;
                    _cells[x, y] = sr;
                }
        }

        void LateUpdate()
        {
            var z = _slot.Zone;
            if (z != _zone) Rebuild(z);
            if (_cells == null) return;

            var lights = new List<(Vector3 pos, float r)>();
            foreach (var l in LightSource.All)
                if (l.On && l.Radius > 0 && z.Contains(l.transform.position)) lights.Add((l.transform.position, l.Radius));

            float dv = 0;
            Vector3 eye = Vector3.zero;
            if (_slot.Character != null && _slot.Avatar != null)
            {
                dv = _slot.Character.Darkvision / 5f;
                eye = _slot.Avatar.transform.position;
            }

            for (int x = 0; x < z.W; x++)
                for (int y = 0; y < z.H; y++)
                {
                    var p = z.ToWorld(new Vector2Int(x, y));
                    float vis = 0;
                    foreach (var (lp, r) in lights)
                    {
                        float d = Vector2.Distance(p, lp);
                        if (d > r) continue;
                        float v = d < r * 0.5f ? 1f : Mathf.Lerp(1f, 0.45f, (d - r * 0.5f) / (r * 0.5f));
                        if (v > vis) vis = v;
                    }
                    // Darkvision: see in darkness as if dim light (shown as a gloomy half-light).
                    if (dv > 0 && Vector2.Distance(p, eye) <= dv && vis < 0.55f) vis = 0.55f;
                    _cells[x, y].color = new Color(0.02f, 0.01f, 0.03f, Mathf.Clamp01(1f - vis) * 0.97f);
                }
        }
    }
}
