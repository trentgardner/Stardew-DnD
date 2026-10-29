using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// One looping music track at a time, by id (Assets/Resources/Audio/{id}.ogg). Switching tracks fades
    /// the old one out and the new one in. Missing tracks are simply silent, like ArtRegistry's fallback idea.
    /// </summary>
    public class Music : MonoBehaviour
    {
        public static float Volume = 0.5f;
        public static string Current { get; private set; }

        static Music _i;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        AudioSource _src;
        float _fade = 1f;
        string _pending;

        public static bool HasRealClip(string id) => Clip(id) != null;

        static AudioClip Clip(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!Cache.TryGetValue(id, out var c)) Cache[id] = c = Resources.Load<AudioClip>("Audio/" + id);
            return c;
        }

        /// <summary>Asks for a track; does nothing if it's already the one playing (safe to call every frame).</summary>
        public static void Play(string id)
        {
            if (id == Current) return;
            if (_i == null)
            {
                var go = new GameObject("Music");
                DontDestroyOnLoad(go);
                _i = go.AddComponent<Music>();
                _i._src = go.AddComponent<AudioSource>();
                _i._src.loop = true;
                _i._src.playOnAwake = false;
            }
            Current = id;
            _i._pending = id;
        }

        public static void Stop() => Play(null);

        void Update()
        {
            // Fade out, swap, fade in (about half a second each way).
            if (_pending != null || (Current == null && _src.isPlaying))
            {
                _fade = Mathf.MoveTowards(_fade, 0f, Time.unscaledDeltaTime * 2f);
                if (_fade <= 0f)
                {
                    _src.Stop();
                    var clip = Clip(_pending);
                    _pending = null;
                    if (clip != null) { _src.clip = clip; _src.Play(); }
                }
            }
            else _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime * 2f);
            _src.volume = Volume * _fade;
        }
    }
}
