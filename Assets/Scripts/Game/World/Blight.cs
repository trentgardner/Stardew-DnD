using System.Collections.Generic;
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
        static readonly Color Ash = new Color(0x9a / 255f, 0x95 / 255f, 0x8f / 255f, 1f);
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
        public static float OverlayAlpha(Zone z) => 0.12f * W.BlightStage * Weight(z.Def);

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
                    sr.sprite = ArtRegistry.Get("tile_highlight");
                    sr.drawMode = SpriteDrawMode.Simple;
                    sr.sortingOrder = -950; // over the ground, under the props and people
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
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    var em = ps.emission;
                    em.rateOverTime = 0;
                    go.GetComponent<ParticleSystemRenderer>().sortingOrder = 30000;
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
