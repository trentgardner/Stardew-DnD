using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// The title screen's backdrop: Thistledown at golden hour, built on its own (no game running), with the camera drifting
    /// slowly over the square while a few familiar faces go about their afternoon and Buoy trots between them.
    /// </summary>
    public class TitleBackdrop : MonoBehaviour
    {
        public Zone Zone { get; private set; }
        public readonly List<Actor> Walkers = new List<Actor>();
        Camera _cam;
        float _t;
        /// <summary>The drift: a slow ping-pong across the square, start to end, in this many seconds.</summary>
        public const float DriftSeconds = 48f;
        static readonly Vector2Int DriftFrom = new Vector2Int(13, 19), DriftTo = new Vector2Int(37, 21);

        public static TitleBackdrop Create(Camera cam)
        {
            var go = new GameObject("TitleBackdrop");
            var b = go.AddComponent<TitleBackdrop>();
            b._cam = cam;
            b.Zone = Zone.Build(WorldDb.Zones["town"], 0, go.transform);
            b.SpawnWalkers();
            b.Frame(0f);
            return b;
        }

        void SpawnWalkers()
        {
            // Familiar faces from Act I, wandering near their usual spots, and Buoy, who goes wherever he likes.
            var folk = new (string name, string look, Vector2Int home, float speed, int roam, bool directional)[]
            {
                ("Brannoc", "npc_brannoc", new Vector2Int(20, 16), 1.2f, 4, true),
                ("Elowen", "npc_elowen", new Vector2Int(28, 20), 1.0f, 4, true),
                ("Pip", "npc_pip", new Vector2Int(24, 22), 1.4f, 5, true),
                ("Rhoswen", "npc_rhoswen", new Vector2Int(32, 17), 1.3f, 6, true),
                ("Buoy", "npc_buoy", new Vector2Int(25, 18), 3.2f, 9, false),
            };
            foreach (var f in folk)
            {
                var at = NearestWalkable(f.home);
                var a = Actor.Spawn<Actor>(f.name, f.look, f.directional, Zone, at);
                a.transform.SetParent(transform, true);
                Walkers.Add(a);
                StartCoroutine(Wander(a, at, f.speed, f.roam));
            }
        }

        Vector2Int NearestWalkable(Vector2Int at)
        {
            for (int r = 0; r < 8; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var p = at + new Vector2Int(dx, dy);
                        if (Zone.IsWalkable(p)) return p;
                    }
            return at;
        }

        IEnumerator Wander(Actor a, Vector2Int home, float speed, int roam)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 2f));
            while (a)
            {
                var target = NearestWalkable(home + new Vector2Int(Random.Range(-roam, roam + 1), Random.Range(-roam, roam + 1)));
                var path = Zone.FindPath(a.MapPos, target, null, 4000);
                if (path != null && path.Count > 0) yield return a.WalkPath(path, speed);
                yield return new WaitForSeconds(Random.Range(0.8f, 3.5f));
            }
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            Frame(_t);
        }

        /// <summary>Puts the camera where the drift is at time t (also sizes it like a player's camera).</summary>
        void Frame(float t)
        {
            if (_cam == null || Zone == null) return;
            float zoom = Mathf.Max(2, Mathf.Round(_cam.pixelHeight / 330f));
            _cam.orthographicSize = _cam.pixelHeight / (2f * ArtRegistry.PixelsPerUnit * zoom);
            float k = Mathf.PingPong(t / DriftSeconds, 1f);
            k = k * k * (3f - 2f * k); // ease at the ends
            Vector3 a = Zone.ToWorld(DriftFrom), b = Zone.ToWorld(DriftTo);
            var p = Vector3.Lerp(a, b, k);
            p.y += Mathf.Sin(t * 0.21f) * 0.35f; // a gentle bob, like a held camera
            _cam.transform.position = new Vector3(p.x, p.y, -10f);
        }

        public Vector3 CameraPosition => _cam != null ? _cam.transform.position : Vector3.zero;
    }
}
