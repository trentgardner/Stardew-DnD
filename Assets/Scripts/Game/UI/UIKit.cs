using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>Helpers for building uGUI from code with the game's parchment look.</summary>
    public static class UIKit
    {
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.Load<Font>("Fonts/GameFont");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static readonly Color Ink = new Color32(59, 42, 30, 255);
        public static readonly Color Parchment = new Color32(242, 226, 194, 255);
        public static readonly Color Gold = new Color32(232, 200, 122, 255);
        public static readonly Color Dim = new Color32(140, 120, 100, 255);
        public static readonly Color Good = new Color32(90, 170, 90, 255);
        public static readonly Color Bad = new Color32(200, 70, 60, 255);
        public static readonly Color Night = new Color32(30, 22, 26, 230);
        public static readonly Color D20Gold = new Color32(232, 184, 70, 255);
        public static readonly Color DamageRed = new Color32(214, 74, 58, 255);
        public static readonly Color HealGreen = new Color32(90, 190, 100, 255);
        public static readonly Color UtilityBlue = new Color32(90, 140, 210, 255);

        public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name, float pad = 0) =>
            Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(pad, pad), new Vector2(-pad, -pad));

        /// <summary>Anchored box: anchor point + pivot at the same spot, fixed size, offset from anchor.</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            var rt = Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            if (size.x > 120) rt.gameObject.AddComponent<FitWidth>().Max = size.x;
            return rt;
        }

        public static Image Image(RectTransform rt, Color color, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null) img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(RectTransform rt, bool dark = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ArtRegistry.Get(dark ? "ui_panel_dark" : "ui_panel");
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 8f; // keeps the 4px pixel-art frame thin on a 640x360 canvas
            img.raycastTarget = false;
            if (img.sprite.border == Vector4.zero) img.type = UnityEngine.UI.Image.Type.Simple;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, string name = "Label")
        {
            var rt = Fill(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static bool Contains(RectTransform rt, Vector2 screen, Camera cam) =>
            rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam);

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Colorize(string s, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
    }

    /// <summary>Shrinks a box to fit its parent (e.g. a half-width split-screen canvas).</summary>
    public class FitWidth : MonoBehaviour
    {
        public float Max;
        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null || parent.rect.width <= 0) return;
            float w = Mathf.Min(Max, parent.rect.width - 12);
            if (Mathf.Abs(rt.sizeDelta.x - w) > 0.5f) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            // keep edge-anchored boxes on screen
            if (rt.anchorMin.x == 0 && rt.anchoredPosition.x + w > parent.rect.width) rt.anchoredPosition = new Vector2(Mathf.Max(6, parent.rect.width - w - 6), rt.anchoredPosition.y);
        }
    }

    /// <summary>
    /// A vertical list of selectable rows driven by one player's input (keys/D-pad/stick + mouse hover/click).
    /// Used for dialogue options, shops, menus and character creation.
    /// </summary>
    public class MenuList
    {
        public class Entry
        {
            public string Text;
            public Action OnSelect;
            public bool Enabled = true;
            public string Tooltip;
            public RectTransform Rect;
            public Text Label;
            public Image Bg;
        }

        public readonly List<Entry> Entries = new List<Entry>();
        public int Index;
        public RectTransform Root;
        public int RowHeight = 20;
        public int FontSize = 13;
        public Action<Entry> OnHover;
        public bool Wrap = true;
        int _scroll;
        public int VisibleRows = 999;

        public MenuList(RectTransform root) { Root = root; }

        public Entry Add(string text, Action onSelect, bool enabled = true, string tooltip = null)
        {
            var e = new Entry { Text = text, OnSelect = onSelect, Enabled = enabled, Tooltip = tooltip };
            Entries.Add(e);
            return e;
        }

        public void Clear()
        {
            Entries.Clear();
            Index = 0;
            _scroll = 0;
            if (Root != null) UIKit.Clear(Root);
        }

        public void Build()
        {
            UIKit.Clear(Root);
            int visible = Mathf.Min(VisibleRows, Entries.Count);
            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                int row = i - _scroll;
                bool show = row >= 0 && row < visible;
                e.Rect = UIKit.Rect(Root, "Row" + i, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -(row + 1) * RowHeight), new Vector2(0, -row * RowHeight));
                e.Bg = UIKit.Image(e.Rect, new Color(0, 0, 0, 0));
                e.Label = UIKit.Label(e.Rect, e.Text, FontSize, UIKit.Ink, TextAnchor.MiddleLeft);
                e.Label.rectTransform.offsetMin = new Vector2(6, 0);
                e.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                e.Rect.gameObject.SetActive(show);
            }
            Refresh();
        }

        public void Refresh()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                if (e.Bg == null) continue;
                bool sel = i == Index;
                e.Bg.color = sel ? new Color32(201, 160, 92, 160) : new Color(0, 0, 0, 0);
                e.Label.color = !e.Enabled ? UIKit.Dim : UIKit.Ink;
                e.Label.text = (sel ? "▶ " : "   ") + e.Text;
            }
        }

        void EnsureVisible()
        {
            if (VisibleRows >= Entries.Count) return;
            int old = _scroll;
            if (Index < _scroll) _scroll = Index;
            if (Index >= _scroll + VisibleRows) _scroll = Index - VisibleRows + 1;
            if (old != _scroll) Build();
        }

        /// <summary>Processes input. Returns true if an entry was activated.</summary>
        public bool Tick(PlayerInput input, Camera cam)
        {
            if (Entries.Count == 0) return false;
            var nav = input.Nav();
            if (nav.y != 0)
            {
                int n = Entries.Count;
                Index = Wrap ? (Index - nav.y + n) % n : Mathf.Clamp(Index - nav.y, 0, n - 1);
                EnsureVisible();
                Refresh();
                OnHover?.Invoke(Entries[Index]);
            }
            if (input.UsesMouse)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (!UIKit.Contains(Entries[i].Rect, input.MouseScreen, cam)) continue;
                    if (Index != i) { Index = i; Refresh(); OnHover?.Invoke(Entries[i]); }
                    if (input.MouseLeft) return Activate();
                }
                if (input.Scroll != 0 && VisibleRows < Entries.Count)
                {
                    Index = Mathf.Clamp(Index - (int)Mathf.Sign(input.Scroll), 0, Entries.Count - 1);
                    EnsureVisible(); Refresh();
                }
            }
            if (input.Confirm) return Activate();
            return false;
        }

        bool Activate()
        {
            if (Index < 0 || Index >= Entries.Count) return false;
            var e = Entries[Index];
            if (!e.Enabled) return false;
            e.OnSelect?.Invoke();
            return true;
        }
    }
}
