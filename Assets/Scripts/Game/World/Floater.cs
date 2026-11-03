using UnityEngine;

namespace DnD.Game
{
    /// <summary>Visible magic: the thing hangs in the air and bobs gently (floating stones, the Starwell's light).</summary>
    public class Floater : MonoBehaviour
    {
        public float Amplitude = 0.08f, Period = 2.5f;
        Vector3 _home;
        float _phase;

        void Start()
        {
            _home = transform.localPosition;
            _phase = (transform.position.x * 0.37f + transform.position.y * 0.61f) % 1f; // neighbours never bob in step
        }

        void Update()
        {
            transform.localPosition = _home + new Vector3(0, Amplitude * Mathf.Sin((Time.time / Period + _phase) * Mathf.PI * 2f), 0);
        }
    }

    /// <summary>Visible magic: a ward or rune that pulses softly.</summary>
    public class WardShimmer : MonoBehaviour
    {
        public float Min = 0.65f, Max = 1f, Period = 3f;
        SpriteRenderer _sr;
        float _phase;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = (transform.position.x * 0.23f + transform.position.y * 0.41f) % 1f;
        }

        void Update()
        {
            if (_sr == null) return;
            float t = 0.5f + 0.5f * Mathf.Sin((Time.time / Period + _phase) * Mathf.PI * 2f);
            var c = _sr.color; c.a = Mathf.Lerp(Min, Max, t); _sr.color = c;
        }
    }

    /// <summary>Two-frame animation (the Grand Orrery turning): swaps between {Id}_0 and {Id}_1.</summary>
    public class FrameFlip : MonoBehaviour
    {
        public string Id;
        public float Seconds = 0.8f;
        SpriteRenderer _sr;
        Sprite _a, _b;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _a = ArtRegistry.Get(Id + "_0");
            _b = ArtRegistry.Get(Id + "_1");
        }

        void Update()
        {
            if (_sr != null) _sr.sprite = ((int)(Time.time / Seconds)) % 2 == 0 ? _a : _b;
        }
    }

    /// <summary>Visible magic: a soft cyan halo behind an arcane prop, brighter after dark.</summary>
    public class ArcaneHalo : MonoBehaviour
    {
        static Sprite _soft;
        SpriteRenderer _sr;
        float _phase;
        /// <summary>The halo's colour (cyan for the Academy's wards; amber for the capital's ever-lamps).</summary>
        public Color Tint = new Color(0.55f, 0.85f, 1f);

        static Sprite Soft
        {
            get
            {
                if (_soft != null) return _soft;
                const int n = 32;
                var t = new Texture2D(n, n) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d));
                    }
                t.SetPixels(px); t.Apply();
                _soft = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return _soft;
            }
        }

        public static void Attach(GameObject prop, float radius, Color? tint = null)
        {
            var go = new GameObject("halo");
            go.transform.SetParent(prop.transform, false);
            go.transform.localPosition = new Vector3(0, 0.45f, 0);
            float s = radius * 0.9f / Mathf.Max(0.01f, prop.transform.localScale.x);
            go.transform.localScale = new Vector3(s, s, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Soft;
            var parent = prop.GetComponent<SpriteRenderer>();
            sr.sortingOrder = parent != null ? parent.sortingOrder - 1 : 0;
            var h = go.AddComponent<ArcaneHalo>();
            if (tint.HasValue) h.Tint = tint.Value;
        }

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = (transform.position.x * 0.31f + transform.position.y * 0.17f) % 1f;
        }

        void Update()
        {
            if (_sr == null || GameRoot.I == null) return;
            bool night = GameRoot.I.Clock.IsNight;
            float pulse = 0.5f + 0.5f * Mathf.Sin((Time.time / 3f + _phase) * Mathf.PI * 2f);
            _sr.color = new Color(Tint.r, Tint.g, Tint.b, (night ? 0.45f : 0.16f) + 0.08f * pulse);
        }
    }
}
