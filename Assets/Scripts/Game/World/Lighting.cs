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
    /// Per-player darkness for dark zones: one texel per tile whose opacity depends on nearby lights and on the
    /// viewing player's darkvision, stretched over the zone with bilinear filtering so light fades smoothly
    /// instead of in hard tile-sized squares. Lives on a layer only that player's camera renders.
    /// </summary>
    public class DarknessOverlay : MonoBehaviour
    {
        PlayerSlot _slot;
        Zone _zone;
        Texture2D _tex;
        Color32[] _px;
        GameObject _quad;

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
            if (_quad) Destroy(_quad);
            if (_tex) Destroy(_tex);
            _zone = z;
            _tex = null;
            if (z == null || !z.Def.Dark) return;
            _tex = new Texture2D(z.W, z.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _px = new Color32[z.W * z.H];
            _quad = new GameObject("dark");
            _quad.layer = _slot.DarkLayer;
            _quad.transform.SetParent(transform, false);
            _quad.transform.position = new Vector3(z.Origin.x, z.Origin.y, 0);
            var sr = _quad.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_tex, new Rect(0, 0, z.W, z.H), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = 30000;
        }

        void OnDestroy()
        {
            if (_tex) Destroy(_tex);
        }

        void LateUpdate()
        {
            var z = _slot.Zone;
            if (z != _zone) Rebuild(z);
            if (_tex == null) return;

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
                    // Darkvision: see in darkness as if dim light (shown as a gloomy half-light), fading at the edge.
                    if (dv > 0)
                    {
                        float d = Vector2.Distance(p, eye);
                        if (d <= dv) vis = Mathf.Max(vis, Mathf.Lerp(0.55f, 0.35f, Mathf.InverseLerp(dv - 2f, dv, d)));
                    }
                    // Texture rows run bottom-up; map rows run top-down.
                    _px[(z.H - 1 - y) * z.W + x] = new Color(0.02f, 0.01f, 0.03f, Mathf.Clamp01(1f - vis) * 0.97f);
                }
            _tex.SetPixels32(_px);
            _tex.Apply(false);
        }
    }
}
