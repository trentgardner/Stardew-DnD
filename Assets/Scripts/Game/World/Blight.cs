using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// The blight (Act II): ash greys the land, hearths and forges burn low, drifting ash at stage 2+. Stages are set by the
    /// story (Phase 5) and eased by the volcano (Phase 6). Unsettling, not gory.
    /// </summary>
    public static class Blight
    {
        public const int MaxStage = 3;
        static readonly Color Ash = new Color(0.47f, 0.46f, 0.47f, 1f);
        static readonly Dictionary<LightSource, float> BaseRadius = new Dictionary<LightSource, float>();

        static WorldState W => GameRoot.I.World;

        /// <summary>How much a place feels it (0-1): the wild most, the village less, indoors least; the kobolds' egg-song holds it back in the warrens.</summary>
        public static float Weight(ZoneDef def)
        {
            if (def.BlightWeight >= 0) return def.BlightWeight;
            if (def.Id == "warrens") return 0.3f;
            if (def.Indoors) return 0.2f;
            if (def.Id == "town") return 0.5f;
            return 0.6f;
        }

        /// <summary>The ash overlay's opacity in a zone right now.</summary>
        public static float OverlayAlpha(Zone z) => 0.25f * W.BlightStage * Weight(z.Def);

        static Sprite _white;
        /// <summary>A plain white square (the overlay is tinted ash grey).</summary>
        static Sprite White
        {
            get
            {
                if (_white != null) return _white;
                var t = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
                t.SetPixels(px); t.Apply();
                _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                return _white;
            }
        }

        /// <summary>
        /// Where the story says the blight should be: none in Act I; 1 once Act II begins, 2 once the party has chosen a side,
        /// 3 on the Emberhorn Road. The volcano's half solution (BlightEased) keeps it one stage lower for good.
        /// </summary>
        public static int StoryStage(WorldState w, QuestLog q)
        {
            if (w == null || q == null || w.Act < 2) return 0;
            int a = q.Stage(Answers.Id);
            int s = a >= Answers.OnTheRoad ? 3 : a >= Answers.Chosen ? 2 : 1;
            if (w.BlightEased) s--;
            return Mathf.Clamp(s, 0, MaxStage);
        }

        /// <summary>Brings the blight in line with the story (after any quest change, on load, at the start of Act II).</summary>
        public static void Sync()
        {
            var g = GameRoot.I;
            if (g == null || g.World == null) return;
            int s = StoryStage(g.World, g.Quests);
            if (s != g.World.BlightStage) SetStage(s);
        }

        public static void SetStage(int stage)
        {
            W.BlightStage = Mathf.Clamp(stage, 0, MaxStage);
            ApplyAll();
        }

        /// <summary>The half solution: the volcano calmed, the blight steps back one stage.</summary>
        public static void Ease()
        {
            W.BlightEased = true;
            SetStage(W.BlightStage - 1);
        }

        public static void ApplyAll()
        {
            // Forget lights from worlds that have been torn down (a load or a new game rebuilds every zone).
            foreach (var dead in BaseRadius.Keys.Where(l => l == null).ToList()) BaseRadius.Remove(dead);
            foreach (var z in GameRoot.I.Zones.Values) if (z != null) Apply(z);
        }

        public static void Apply(Zone z)
        {
            float a = OverlayAlpha(z);
            var overlay = z.transform.Find("BlightOverlay");
            if (a <= 0f) { if (overlay != null) overlay.gameObject.SetActive(false); }
            else
            {
                if (overlay == null)
                {
                    var go = new GameObject("BlightOverlay");
                    go.transform.SetParent(z.transform, false);
                    var b = z.WorldBounds;
                    go.transform.position = new Vector3(b.center.x, b.center.y, 0);
                    go.transform.localScale = new Vector3(b.size.x, b.size.y, 1);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = White;
                    sr.drawMode = SpriteDrawMode.Simple;
                    sr.sortingOrder = 29000; // a haze over the whole place (ash in the air), under the HUD
                    overlay = go.transform;
                    // Drifting ash at stage 2 and up.
                    var ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startColor = new Color(0.75f, 0.73f, 0.7f, 0.7f);
                    main.startSize = 0.08f;
                    main.startSpeed = 0.15f;
                    main.startLifetime = 6f;
                    main.maxParticles = 120;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    // The overlay is scaled to the zone's size: let that stretch the emitter box, not each fleck.
                    main.scalingMode = ParticleSystemScalingMode.Shape;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    var em = ps.emission;
                    em.rateOverTime = 0;
                    var pr = go.GetComponent<ParticleSystemRenderer>();
                    pr.sortingOrder = 30000;
                    pr.material = new Material(Shader.Find("Sprites/Default"));
                }
                overlay.gameObject.SetActive(true);
                var r = overlay.GetComponent<SpriteRenderer>();
                r.color = new Color(Ash.r, Ash.g, Ash.b, a);
                var emission = overlay.GetComponent<ParticleSystem>().emission;
                emission.rateOverTime = W.BlightStage >= 2 ? 6f * W.BlightStage * Weight(z.Def) : 0f;
            }
            // Cold hearths and forges: the zone's lights burn at half strength once the blight has a foothold.
            foreach (var l in z.GetComponentsInChildren<LightSource>(true))
            {
                if (!BaseRadius.ContainsKey(l)) BaseRadius[l] = l.Radius;
                l.Radius = BaseRadius[l] * (W.BlightStage >= 1 && Weight(z.Def) >= 0.2f ? 0.5f : 1f);
            }
        }
    }
}
