using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Title menu: New Game (pick difficulty), Continue, Load (all playthroughs), Quit. Opens on a little cinematic: Thistledown
    /// at golden hour fades in from black, the camera drifts over the square, the title rises, then the menu. Any press skips it.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        GameRoot _g;
        MenuList _menu;
        Text _info;
        RectTransform _root;
        readonly HybridInput _input = new HybridInput();
        public TitleBackdrop Backdrop { get; private set; }
        RectTransform _panel;

        /// <summary>The menu box fits its rows (up to the list's nine), centred a little below the middle.</summary>
        void Fit()
        {
            if (_panel == null) return;
            int rows = Mathf.Clamp(_menu.Entries.Count, 1, 9);
            _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, rows * 17 + 20);
        }
        CanvasGroup _black, _head, _body;
        float _t;
        readonly List<(RectTransform rt, Image img, float speed, float phase, float sway)> _motes = new List<(RectTransform, Image, float, float, float)>();
        /// <summary>When the intro has played out: title from 1.6 s, menu from 3.8 s, fully in at 5 s.</summary>
        public const float IntroSeconds = 5f;
        public bool MenuReady => _t >= IntroSeconds;

        /// <summary>Skips the intro to its end (any press does this).</summary>
        public void SkipIntro() { if (_t < IntroSeconds) _t = IntroSeconds; }

        static CanvasGroup Group(RectTransform parent, string name)
        {
            var rt = UIKit.Fill(parent, name);
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        public static TitleScreen Create(GameRoot g)
        {
            var go = new GameObject("TitleScreen");
            var t = go.AddComponent<TitleScreen>();
            t._g = g;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (g.TitleCamera != null) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = g.TitleCamera; canvas.planeDistance = 1f; } // drawn by the title camera, over its town
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 1f;
            t._root = UIKit.Fill(go.transform, "Root");
            if (g.TitleCamera != null) t.Backdrop = TitleBackdrop.Create(g.TitleCamera);
            // Golden hour over the town, and a soft shade at the top and bottom so the words read over it.
            UIKit.Image(UIKit.Fill(t._root, "Warmth"), new Color(1f, 0.72f, 0.42f, 0.14f));
            UIKit.Image(UIKit.Rect(t._root, "ShadeBottom", Vector2.zero, new Vector2(1, 0.13f), Vector2.zero, Vector2.zero), new Color(0.12f, 0.08f, 0.1f, 0.4f));
            // Fireflies drifting up out of the square.
            for (int i = 0; i < 18; i++)
            {
                var m = UIKit.Box(t._root, "Mote", new Vector2(Random.value, 0), new Vector2(3, 3), Vector2.zero);
                var img = UIKit.Image(m, new Color(1f, 0.85f, 0.45f, 0.8f));
                t._motes.Add((m, img, Random.Range(10f, 22f), Random.Range(0f, 360f), Random.Range(6f, 16f)));
                m.anchoredPosition = new Vector2(0, Random.Range(0f, 360f));
            }

            t._head = Group(t._root, "Head");
            var title = UIKit.Box((RectTransform)t._head.transform, "Title", new Vector2(0.5f, 1), new Vector2(560, 70), new Vector2(0, -30));
            var tl = UIKit.Label(title, "EMBERS OF SUNDERMERE", 34, UIKit.Gold, TextAnchor.MiddleCenter);
            tl.fontStyle = FontStyle.Bold;
            var shadow = tl.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.15f, 0.08f, 0.05f, 0.9f); shadow.effectDistance = new Vector2(2, -2);
            var sub = UIKit.Box((RectTransform)t._head.transform, "Sub", new Vector2(0.5f, 1), new Vector2(560, 20), new Vector2(0, -98));
            var sl = UIKit.Label(sub, "a cozy high-fantasy adventure for one or two", 11, UIKit.Parchment, TextAnchor.MiddleCenter);
            sl.fontStyle = FontStyle.Italic;
            sl.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.05f, 0.9f);

            t._body = Group(t._root, "Body");
            var panel = UIKit.Box((RectTransform)t._body.transform, "Menu", new Vector2(0.5f, 0.5f), new Vector2(380, 170), new Vector2(0, -30));
            t._panel = panel;
            UIKit.Panel(panel);
            var list = UIKit.Rect(panel, "List", Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            t._menu = new MenuList(list) { RowHeight = 17, FontSize = 12, VisibleRows = 9, Wrap = false };
            var info = UIKit.Box((RectTransform)t._body.transform, "Info", new Vector2(0.5f, 0), new Vector2(560, 50), new Vector2(0, 10));
            t._info = UIKit.Label(info, "", 10, UIKit.Parchment, TextAnchor.MiddleCenter);
            t._black = Group(t._root, "Black");
            UIKit.Image((RectTransform)t._black.transform, Color.black);
            t.Main();
            t.Animate();
            return t;
        }

        void Main()
        {
            _menu.Clear();
            var saves = SaveSystem.List();
            if (saves.Count > 0) _menu.Add($"Continue: {saves[0].PlaythroughName}", () => _g.LoadGame(saves[0].Path));
            _menu.Add("New Game", ShowDifficulty);
            _menu.Add("Load Game", Load, saves.Count > 0);
            _menu.Add("Quit", () => { Application.Quit(); });
            _menu.Build();
            Fit();
            _info.text = "Keyboard & mouse or controller. A second player joins any time by pressing START on another controller.\nSaves: " + SaveSystem.Root;
        }

        void ShowDifficulty()
        {
            _menu.Clear();
            void Opt(DnD.Game.Difficulty d, string text)
            {
                _menu.Add(text, () => ShowStart(d));
            }
            Opt(DnD.Game.Difficulty.Story, "Story: forgiving. Cheap revives; resting brings back the fallen.");
            Opt(DnD.Game.Difficulty.Normal, "Normal: the intended experience.");
            Opt(DnD.Game.Difficulty.Hard, "Hard: revives are pricey and must be quick.");
            Opt(DnD.Game.Difficulty.Hardcore, "Hardcore: one save. A party wipe ends the story.");
            _menu.Add("Back", Main);
            _menu.Index = 1;
            _menu.Build();
            Fit();
        }

        void ShowStart(DnD.Game.Difficulty d)
        {
            _menu.Clear();
            _menu.Add("Act I: Home (level 1, the whole story)", () => { Destroy(gameObject); _g.StartNewGame(_input, d); });
            _menu.Add("Start at Act II (level 5: the dragon has hatched)", () => { Destroy(gameObject); _g.StartNewGame(_input, d, actTwo: true); });
            _menu.Add("Start at Act III (level 11: after the Warden)", () => ShowActThreeSide(d));
            _menu.Add("Back", ShowDifficulty);
            _menu.Build();
            Fit();
            _info.text = "Starting later skips the acts before it, with a ready-made character and a dragon of the right age. The whole story starts at Act I.";
        }

        /// <summary>Starting at Act III: which side did the party stand with in Act II?</summary>
        void ShowActThreeSide(DnD.Game.Difficulty d)
        {
            _menu.Clear();
            foreach (var f in new[] { "crown", "academy", "fae", "hunters" })
            {
                string side = f;
                _menu.Add($"We stood with {DnD.Game.FactionDb.NameOf(side)}", () => { Destroy(gameObject); _g.StartNewGame(_input, d, actThreeSide: side); });
            }
            _menu.Add("Back", () => ShowStart(d));
            _menu.Build();
            Fit();
            _info.text = "Starting at Act III skips Acts I and II and hands you a grown dragon. Pick the side you stood with in Act II.";
        }

        void Load()
        {
            _menu.Clear();
            foreach (var s in SaveSystem.List())
            {
                var path = s.Path;
                _menu.Add($"{s.PlaythroughName}: {s.Summary}", () => _g.LoadGame(path));
            }
            _menu.Add("Back", Main);
            _menu.Build();
            Fit();
        }

        void Update()
        {
            if (!MenuReady)
            {
                _t += Time.unscaledDeltaTime;
                bool anyKey = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame;
                if (_input.Confirm || _input.Cancel || _input.MouseLeft || anyKey) SkipIntro(); // any press: straight to the menu
                Animate();
                return;
            }
            Animate();
            _menu.Tick(_input, null);
            if (_input.Cancel) Main();
        }

        /// <summary>The intro's fades, and the fireflies (which keep drifting behind the menu).</summary>
        void Animate()
        {
            if (_black != null) _black.alpha = 1f - Mathf.Clamp01(_t / 2.2f);
            if (_black != null) _black.blocksRaycasts = _black.alpha > 0.01f;
            if (_head != null)
            {
                float h = Mathf.Clamp01((_t - 1.6f) / 1.6f);
                _head.alpha = h;
                _head.transform.localPosition = new Vector3(0, (1f - h) * -10f, 0); // the title rises a little as it appears
            }
            if (_body != null) { _body.alpha = Mathf.Clamp01((_t - 3.8f) / 1.2f); _body.interactable = MenuReady; }
            float now = Time.unscaledTime;
            foreach (var (rt, img, speed, phase, sway) in _motes)
            {
                if (rt == null) continue;
                var p = rt.anchoredPosition;
                p.y += speed * Time.unscaledDeltaTime;
                if (p.y > 380f) p.y = -10f;
                p.x = Mathf.Sin((now + phase) * 0.6f) * sway;
                rt.anchoredPosition = p;
                var c = img.color; c.a = 0.35f + 0.45f * Mathf.Abs(Mathf.Sin(now * 1.7f + phase)); img.color = c;
            }
        }

        void OnDestroy()
        {
            if (Backdrop != null) Destroy(Backdrop.gameObject);
        }
    }
}
