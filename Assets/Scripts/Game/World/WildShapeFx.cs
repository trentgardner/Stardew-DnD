using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Druid Wild Shape visuals: the actor's sprite becomes the beast's (or the hero's again) with a puff of
    /// smoke and leaves. Beasts only have side-facing art, so a shaped druid isn't directional.
    /// </summary>
    public static class WildShapeFx
    {
        public static void Transform(Actor a, PlayerCharacter pc)
        {
            if (a == null) return;
            a.Look = pc.WildShape != null ? pc.WildShape.SpriteId : a.HomeLook;
            a.Directional = pc.WildShape == null;
            a.RefreshSprite();
            Poof.At(a.transform.position + new Vector3(0, 0.3f, 0));
            Sfx.Play("spell");
        }

        /// <summary>Puts the sprite back in line with the rules if the form ended on its own (e.g. dropping to 0 HP).</summary>
        public static void Sync(Actor a, PlayerCharacter pc)
        {
            if (a == null || pc == null) return;
            string want = pc.WildShape != null ? pc.WildShape.SpriteId : a.HomeLook;
            if (a.Look != want) Transform(a, pc);
        }
    }

    /// <summary>A one-shot smoke-and-leaves puff (pack FX), removed when the animation ends.</summary>
    public class Poof : MonoBehaviour
    {
        Sprite[] _frames;
        SpriteRenderer _sr;
        float _t;

        public static void At(Vector3 pos)
        {
            var go = new GameObject("Poof");
            go.transform.position = pos;
            var p = go.AddComponent<Poof>();
            p._sr = go.AddComponent<SpriteRenderer>();
            p._sr.sortingOrder = 29000;
            p._frames = ArtRegistry.Frames("fx_smoke", 6);
            p._sr.sprite = p._frames[0];
            for (int i = 0; i < 4; i++) Leaf.Spawn(pos, i);
        }

        void Update()
        {
            _t += Time.deltaTime;
            int f = (int)(_t / 0.07f);
            if (f >= _frames.Length) { Destroy(gameObject); return; }
            _sr.sprite = _frames[f];
        }
    }

    /// <summary>A little leaf that drifts out of the puff and fades.</summary>
    public class Leaf : MonoBehaviour
    {
        Vector3 _vel;
        SpriteRenderer _sr;
        float _life = 0.8f;

        public static void Spawn(Vector3 pos, int i)
        {
            var go = new GameObject("Leaf");
            go.transform.position = pos;
            var l = go.AddComponent<Leaf>();
            l._sr = go.AddComponent<SpriteRenderer>();
            l._sr.sprite = ArtRegistry.Get("fx_leaf_" + (i % 3));
            l._sr.sortingOrder = 29001;
            float ang = (i * 90 + 45) * Mathf.Deg2Rad;
            l._vel = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang) + 0.5f, 0) * 1.5f;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0) { Destroy(gameObject); return; }
            transform.position += _vel * Time.deltaTime;
            _vel *= 0.92f;
            var c = _sr.color; c.a = Mathf.Clamp01(_life / 0.4f); _sr.color = c;
        }
    }
}
