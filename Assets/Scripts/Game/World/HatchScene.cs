using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DnD.Game
{
    /// <summary>
    /// Letterbox bars and a caption while the egg hatches. Holds the player's input (it's the top screen) and can be skipped
    /// with any button after the first second (so a stray key press from the last menu doesn't skip it).
    /// </summary>
    public class HatchCutscene : GameScreen
    {
        public bool Skipped { get; private set; }
        float _opened;
        Text _caption;
        public override bool ShowCloseButton => false;

        public override void Build()
        {
            _opened = Time.time;
            Root = UIKit.Fill(Hud.Root, "HatchCutscene");
            var top = UIKit.Rect(Root, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), Vector2.zero);
            UIKit.Image(top, Color.black);
            var bottom = UIKit.Rect(Root, "Bottom", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 34));
            UIKit.Image(bottom, Color.black);
            _caption = UIKit.Label(bottom, "", 10, UIKit.Parchment, TextAnchor.MiddleCenter);
            _caption.fontStyle = FontStyle.Italic;
        }

        public void Caption(string text) { if (_caption != null) _caption.text = text; }

        public void Skip() { if (Time.time - _opened >= 1f) Skipped = true; }

        public override void Tick() { if (In.Confirm || In.Cancel || In.MouseLeft) Skip(); }
    }

    /// <summary>
    /// The Act I climax: the egg glows and wobbles three times, cracks, flashes, and a damp, furious little dragon tumbles out,
    /// sneezes sparks and falls in love with whoever is closest. Then the narration and the naming.
    /// </summary>
    public static class HatchScene
    {
        public static IEnumerator Play(GameRoot g, PlayerSlot p, Vector3 eggPos, List<HatchCutscene> scenes)
        {
            var egg = Object.FindObjectsByType<EggInteractable>().FirstOrDefault(e => e.gameObject.activeInHierarchy);
            var eggSr = egg != null ? egg.GetComponent<SpriteRenderer>() : null;
            Vector3 pos = egg != null ? egg.transform.position : eggPos;
            GameObject flash = null, hatchling = null;
            SpriteRenderer emote = null;
            bool Skipping() => scenes.Any(s => s.Skipped || s.Closed);
            void Caption(string t) { foreach (var s in scenes) s.Caption(t); }
            void Egg(string sprite) { if (eggSr != null) eggSr.sprite = ArtRegistry.Get(sprite); }
            IEnumerator Wait(float seconds) { for (float t = 0; t < seconds && !Skipping(); t += Time.deltaTime) yield return null; }

            g.MusicOverride = "music_hatch";
            try
            {
                Egg("obj_egg");
                Caption("The egg pulses with a slow, warm light...");
                if (!Skipping()) yield return Wait(0.6f);
                for (int i = 0; i < 3 && !Skipping(); i++)
                {
                    Egg("obj_egg_glow");
                    Sfx.Play("ui");
                    for (float t = 0; t < 0.45f && !Skipping(); t += Time.deltaTime)
                    {
                        if (egg != null) egg.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 40f) * (6f + 4f * i));
                        yield return null;
                    }
                    if (egg != null) egg.transform.localRotation = Quaternion.identity;
                    Egg("obj_egg");
                    if (i == 1) Caption("...once, twice...");
                    if (!Skipping()) yield return Wait(0.45f);
                }
                Caption("CRACK.");
                Egg("obj_egg_crack1");
                Sfx.Play("hit");
                if (!Skipping()) yield return Wait(0.7f);
                Egg("obj_egg_crack2");
                Sfx.Play("hit");
                if (!Skipping()) yield return Wait(0.7f);

                var zoneRoot = egg != null ? egg.transform.parent : null; // scene props live in the zone, so a reset mid-scene clears them
                flash = new GameObject("HatchFlash");
                flash.transform.SetParent(zoneRoot, true);
                flash.transform.position = pos;
                flash.transform.localScale = Vector3.one * 4f;
                var fsr = flash.AddComponent<SpriteRenderer>();
                fsr.sprite = ArtRegistry.Get("fx_hatch_flash");
                fsr.sortingOrder = 31500;
                Sfx.Play("crit");
                Egg("item_egg_shard");
                hatchling = new GameObject("HatchlingProp");
                hatchling.transform.SetParent(zoneRoot, true);
                hatchling.transform.position = pos;
                var hsr = hatchling.AddComponent<SpriteRenderer>();
                hsr.sprite = ArtRegistry.Get("mon_dragonling_0");
                hsr.sortingOrder = YSort.OrderFor(pos.y) + 2;
                var eg = new GameObject("Emote");
                eg.transform.SetParent(hatchling.transform, false);
                eg.transform.localPosition = new Vector3(0.35f, 0.85f, 0);
                emote = eg.AddComponent<SpriteRenderer>();
                emote.sortingOrder = 31000;
                emote.enabled = false;
                for (float t = 0; t < 0.6f && !Skipping(); t += Time.deltaTime)
                {
                    fsr.color = new Color(1, 1, 1, 1f - t / 0.6f);
                    yield return null;
                }
                Object.Destroy(flash);
                Caption("A tiny, damp, furious red dragon tumbles out...");

                // Two hops, a sneeze of sparks, then love at first sight.
                for (int hop = 0; hop < 2 && !Skipping(); hop++)
                    for (float t = 0; t < 0.35f && !Skipping(); t += Time.deltaTime)
                    {
                        hatchling.transform.position = pos + Vector3.up * Mathf.Sin(t / 0.35f * Mathf.PI) * 0.35f;
                        hsr.sprite = ArtRegistry.Get(t < 0.17f ? "mon_dragonling_1" : "mon_dragonling_0");
                        yield return null;
                    }
                hatchling.transform.position = pos;
                emote.sprite = ArtRegistry.Get("fx_emote_spark");
                emote.enabled = true;
                Caption("...sneezes a puff of sparks onto your boots...");
                if (!Skipping()) yield return Wait(1.1f);
                emote.sprite = ArtRegistry.Get("fx_emote_heart");
                Caption("...and looks up at you.");
                if (!Skipping()) yield return Wait(1.3f);
            }
            finally
            {
                if (flash != null) Object.Destroy(flash);
                if (hatchling != null) Object.Destroy(hatchling);
                if (egg != null) egg.transform.localRotation = Quaternion.identity;
                Egg("item_egg_shard");
                g.MusicOverride = null;
                foreach (var s in scenes) if (!s.Closed) s.Close();
            }

            if (g.Dragon != null) yield break; // already named (e.g. a test, or a reload)
            yield return null;
            p.Hud.Push(new MessageScreen("The Egg Hatches",
                "The egg pulses once, twice, then splits with a sound like a log cracking in a fire. A tiny, damp, furious red dragon tumbles out, sneezes a puff of sparks onto your boots, looks up at you...\n\n...and decides, with total certainty, that you are its mother.",
                () => p.Hud.Push(new NameEntryScreen(n => g.NameDragon(p, n)))));
        }
    }
}
