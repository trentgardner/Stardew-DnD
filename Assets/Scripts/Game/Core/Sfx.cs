using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Sound effects by id. Uses Assets/Resources/Audio/{id}.wav/.ogg if present, otherwise a tiny
    /// generated placeholder sound. Same drop-in idea as ArtRegistry.
    /// </summary>
    public static class Sfx
    {
        static AudioSource _src;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        public static float Volume = 0.6f;

        public static void Play(string id, float volume = 1f)
        {
            if (_src == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                _src = go.AddComponent<AudioSource>();
                if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
            }
            var clip = Get(id);
            if (clip != null) _src.PlayOneShot(clip, volume * Volume);
        }

        static AudioClip Get(string id)
        {
            if (Cache.TryGetValue(id, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + id) ?? Generate(id);
            Cache[id] = c;
            return c;
        }

        static AudioClip Generate(string id)
        {
            const int rate = 22050;
            float len; System.Func<float, float> wave;
            var rng = new System.Random(id.GetHashCode());
            switch (id)
            {
                case "dice_shake": len = 0.25f; wave = t => Clack(t, rng, 0.03f) * 0.5f; break;
                case "dice_roll": len = 0.7f; wave = t => Clack(t, rng, 0.06f) * (1 - t / 0.7f); break;
                case "dice_land": len = 0.12f; wave = t => Noise(rng) * Mathf.Exp(-t * 40) + Mathf.Sin(t * 2 * Mathf.PI * 300) * Mathf.Exp(-t * 30) * 0.5f; break;
                case "hit": len = 0.18f; wave = t => (Noise(rng) * 0.6f + Mathf.Sin(t * 2 * Mathf.PI * 110)) * Mathf.Exp(-t * 18); break;
                case "miss": len = 0.2f; wave = t => Noise(rng) * 0.4f * Mathf.Sin(t / 0.2f * Mathf.PI); break;
                case "crit": len = 0.4f; wave = t => (Mathf.Sin(t * 2 * Mathf.PI * (440 + t * 600)) * 0.6f + Noise(rng) * 0.3f) * Mathf.Exp(-t * 6); break;
                case "heal": len = 0.45f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * (520 + t * 400)) * 0.4f * Mathf.Exp(-t * 4); break;
                case "spell": len = 0.4f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * (300 + Mathf.Sin(t * 40) * 80)) * 0.4f * Mathf.Exp(-t * 5); break;
                case "door": len = 0.25f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * 90) * Mathf.Exp(-t * 10) * 0.6f; break;
                case "coin": len = 0.25f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * (t < 0.08f ? 990 : 1320)) * 0.35f * Mathf.Exp(-t * 8); break;
                case "ui": len = 0.05f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * 660) * 0.25f; break;
                case "success": len = 0.5f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * (t < 0.15f ? 523 : t < 0.3f ? 659 : 784)) * 0.3f * Mathf.Exp(-t * 3); break;
                case "fail": len = 0.4f; wave = t => Mathf.Sin(t * 2 * Mathf.PI * (330 - t * 300)) * 0.3f * Mathf.Exp(-t * 4); break;
                case "combat_start": len = 0.6f; wave = t => (Mathf.Sin(t * 2 * Mathf.PI * 147) + Mathf.Sin(t * 2 * Mathf.PI * 220)) * 0.3f * Mathf.Exp(-t * 3); break;
                default: len = 0.05f; wave = t => 0; break;
            }
            int n = (int)(rate * len);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)rate), -1, 1);
            var clip = AudioClip.Create(id, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);

        static float Clack(float t, System.Random r, float spacing)
        {
            float phase = (t % spacing) / spacing;
            return Noise(r) * Mathf.Exp(-phase * 25);
        }
    }
}
