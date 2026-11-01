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
}
