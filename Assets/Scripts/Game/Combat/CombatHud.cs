using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Combat UI, laid out like BG3 / XCOM so the battlefield stays visible:
    /// a thin turn-order strip at the top, and on your turn a one-row action bar along the bottom edge.
    /// Spells, class abilities, extra actions and items are folded into group buttons whose pop-up list
    /// only opens when clicked and closes as soon as you pick something (or click the map / right-click).
    /// Descriptions show in a small tooltip only while you hover or just after switching actions.
    /// </summary>
    public class CombatHud : GameScreen
    {
        class BarButton
        {
            public ActionGroup Group;
            public List<int> Actions = new List<int>();
            public RectTransform Rt;
            public Image Bg;
            public Text Label;
        }

        const int BarH = 22, LineH = 13, RowH = 13, Gap = 2;
        static readonly string[] HotKeyNames = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

        readonly CombatInstance _combat;
        RectTransform _tracker, _bar, _line, _tip, _flyout;
        Text _trackerText, _lineText, _tipText;
        readonly List<BarButton> _buttons = new List<BarButton>();
        readonly List<(int action, RectTransform rt, Image bg)> _rows = new List<(int, RectTransform, Image)>();
        string _signature;
        ActionGroup? _open;
        int _hover = -1, _lastSelected;
        float _tipUntil;
        float _lastWidth;
        List<CombatAction> _actions;

        public CombatHud(CombatInstance c) { _combat = c; }

        float Width => Hud.Root.rect.width > 0 ? Hud.Root.rect.width : 640;

        /// <summary>A bottom- or top-anchored box with no auto-fit (this screen sizes its boxes itself).</summary>
        static RectTransform Anchored(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            var rt = UIKit.Rect(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        public override void Build()
        {
            Root = UIKit.Fill(Hud.Root, "CombatHud");

            _tracker = Anchored(Root, "Tracker", new Vector2(0.5f, 1), new Vector2(330, 16), new Vector2(0, -6));
            UIKit.Panel(_tracker, true);
            _trackerText = UIKit.Label(_tracker, "", 8, UIKit.Parchment, TextAnchor.MiddleCenter);
            _trackerText.rectTransform.offsetMin = new Vector2(4, 1); _trackerText.rectTransform.offsetMax = new Vector2(-4, -1);
            _trackerText.resizeTextForBestFit = true; _trackerText.resizeTextMinSize = 6; _trackerText.resizeTextMaxSize = 8;
            _trackerText.verticalOverflow = VerticalWrapMode.Truncate;

            _bar = Anchored(Root, "ActionBar", new Vector2(0.5f, 0), new Vector2(300, BarH), new Vector2(0, 4));
            UIKit.Panel(_bar, true);

            _line = Anchored(Root, "TurnLine", new Vector2(0.5f, 0), new Vector2(300, LineH), new Vector2(0, 4 + BarH + Gap));
            UIKit.Image(_line, new Color(0.08f, 0.06f, 0.07f, 0.78f));
            _lineText = UIKit.Label(_line, "", 8, UIKit.Parchment, TextAnchor.MiddleCenter);
            _lineText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _flyout = Anchored(Root, "Flyout", new Vector2(0.5f, 0), new Vector2(150, 40), Vector2.zero);
            UIKit.Panel(_flyout, true);

            _tip = Anchored(Root, "Tooltip", new Vector2(0.5f, 0), new Vector2(260, 30), Vector2.zero);
            UIKit.Panel(_tip, true);
            _tipText = UIKit.Label(_tip, "", 8, UIKit.Parchment, TextAnchor.UpperLeft);
            _tipText.rectTransform.offsetMin = new Vector2(6, 3); _tipText.rectTransform.offsetMax = new Vector2(-6, -4);

            HideTurnUi();
            RefreshTracker(_combat);
        }

        void HideTurnUi()
        {
            _bar.gameObject.SetActive(false);
            _line.gameObject.SetActive(false);
            _flyout.gameObject.SetActive(false);
            _tip.gameObject.SetActive(false);
            _open = null;
            _signature = null;
            _actions = null;
        }

        // ------------------------------------------------------------------ turn order

        public void RefreshTracker(CombatInstance c)
        {
            if (_trackerText == null) return;
            var cur = c.Enc.Current?.Creature;
            var parts = new List<string>();
            foreach (var cb in c.Enc.Order)
            {
                var cr = cb.Creature;
                string name = cr.Name.Length > 10 ? cr.Name.Substring(0, 10) : cr.Name;
                string hp = cr.Team == Team.Players ? $"{cr.HP}/{cr.EffectiveMaxHP}" : HealthWord(cr);
                string s = $"{name} {hp}";
                if (cr.IsDead) s = $"<color=#777777>{name} ✝</color>";
                else if (cr.IsDown) s = $"<color=#aa6666>{name} ↓</color>";
                if (cr == cur) s = $"<b><color=#ffd27a>▶{s}</color></b>";
                else if (cr.Team == Team.Enemies && !cr.IsDown) s = $"<color=#ff9a8a>{s}</color>";
                parts.Add(s);
            }
            _trackerText.text = $"Round {c.Enc.Round}   " + string.Join("  ·  ", parts);
            PlaceTracker();
        }

        /// <summary>Full-width screens have room between the status and clock panels; half-width (split-screen) ones put it just below them.</summary>
        void PlaceTracker()
        {
            float w = Width;
            // status panel spans x 6..176, clock spans the right 126 px
            if (w >= 600) { _tracker.sizeDelta = new Vector2(w - 314, 16); _tracker.anchoredPosition = new Vector2(25, -6); }
            else { _tracker.sizeDelta = new Vector2(w - 12, 16); _tracker.anchoredPosition = new Vector2(0, -70); }
        }

        /// <summary>Enemies show a description instead of exact HP (like a DM would).</summary>
        static string HealthWord(Creature c)
        {
            float f = c.HP / (float)Mathf.Max(1, c.MaxHP);
            return f >= 1 ? "(unhurt)" : f > 0.5f ? "(hurt)" : f > 0.25f ? "(bloodied)" : "(barely standing)";
        }

        // ------------------------------------------------------------------ action bar

        static string GroupTitle(ActionGroup g) =>
            g == ActionGroup.Spells ? "Spells" : g == ActionGroup.Abilities ? "Abilities" : g == ActionGroup.Actions ? "Actions" : g == ActionGroup.Items ? "Items" : "";

        static string ShortName(CombatAction a) =>
            a.Name == "End Turn" ? "End Turn" : a.Name.StartsWith("Attack") ? "Attack" : a.Name;

        string KeyFor(int actionIndex)
        {
            if (!Slot.Input.UsesMouse) return "";
            if (actionIndex >= 1 && actionIndex <= HotKeyNames.Length) return $"<color=#9a8a70>{HotKeyNames[actionIndex - 1]}</color> ";
            return "";
        }

        public void ShowActions(Unit u, List<CombatAction> actions, int selected, string hint)
        {
            if (u == null || actions == null) { HideTurnUi(); return; }
            _actions = actions;
            bool mouse = Slot.Input.UsesMouse;
            if (selected != _lastSelected) { _lastSelected = selected; if (!mouse) _tipUntil = Time.unscaledTime + 3f; }

            string sig = mouse + "|" + selected + "|" + string.Join(",", actions.Select(a => a.Name + a.Enabled));
            if (sig != _signature || Mathf.Abs(Width - _lastWidth) > 0.5f) { _signature = sig; _lastWidth = Width; LayoutBar(selected); }
            _bar.gameObject.SetActive(true);
            PlaceTracker();

            // Hover (mouse) decides which description the tooltip shows.
            _hover = -1;
            if (mouse)
            {
                foreach (var r in _rows) if (UIKit.Contains(r.rt, Slot.Input.MouseScreen, Cam)) _hover = r.action;
                foreach (var r in _rows) r.bg.color = r.action == _hover ? new Color32(201, 160, 92, 140) : r.action == selected ? new Color32(201, 160, 92, 70) : new Color(0, 0, 0, 0);
                if (_hover < 0)
                    foreach (var b in _buttons)
                        if (b.Actions.Count == 1 && UIKit.Contains(b.Rt, Slot.Input.MouseScreen, Cam)) _hover = b.Actions[0];
            }

            // One-line turn summary: what you have left, and what clicking will do.
            var t = u.Cb.Turn;
            string dot(bool used) => used ? "<color=#666666>○</color>" : "<color=#7ad27a>●</color>";
            string pad = mouse ? "" : "   <color=#9a8a70>LB/RB action · A use · B back · X end</color>";
            _lineText.text = $"Action {dot(t.ActionUsed)} Bonus {dot(t.BonusActionUsed)} Reaction {dot(t.ReactionUsed)}  Move {t.MovementLeftFt} ft   <color=#e8c87a>{hint}</color>{pad}";
            float lineW = Mathf.Min(Width - 12, _lineText.preferredWidth + 16);
            _line.sizeDelta = new Vector2(lineW, LineH);
            _line.gameObject.SetActive(!_flyout.gameObject.activeSelf);

            // Tooltip: hovered entry, or (controller) the newly selected one for a few seconds.
            int tipFor = _hover >= 0 ? _hover : (!mouse && Time.unscaledTime < _tipUntil ? selected : -1);
            var ta = tipFor >= 0 && tipFor < actions.Count ? actions[tipFor] : null;
            string tip = ta == null ? null : TipText(ta);
            _tip.gameObject.SetActive(!string.IsNullOrEmpty(tip));
            if (!string.IsNullOrEmpty(tip))
            {
                float w = Mathf.Min(260, Width - 12);
                _tipText.text = tip;
                _tip.sizeDelta = new Vector2(w, 0);
                float h = _tipText.preferredHeight + 9;
                _tip.sizeDelta = new Vector2(w, h);
                float above = _flyout.gameObject.activeSelf ? _flyout.anchoredPosition.y + _flyout.sizeDelta.y : _line.anchoredPosition.y + LineH;
                _tip.anchoredPosition = new Vector2(0, above + Gap);
            }
        }

        static string TipText(CombatAction a)
        {
            string cost = a.Cost == ActionCost.Bonus ? "Bonus action" : a.Cost == ActionCost.Action ? "Action" : "Free";
            string range = a.RangeFt > 5 ? $" · {a.RangeFt} ft" : a.RangeFt == 5 ? " · melee" : "";
            string head = $"<b>{a.Name}</b>  <color=#9ab8ff>{cost}{range}</color>";
            string body = a.Tooltip;
            if (a.Name == "Move") body = "Click a tile to walk there, or click an enemy to walk up and attack it.";
            if (a.Name == "End Turn") body = "Finish your turn (Space).";
            if (!a.Enabled && a.Why != null) body = UIKit.Colorize(a.Why, UIKit.Bad) + (body != null ? "\n" + body : "");
            return body == null ? head : head + "\n" + body;
        }

        void LayoutBar(int selected)
        {
            UIKit.Clear(_bar);
            _buttons.Clear();

            // Main entries (Move, Attack) get their own buttons; each other group folds into one button
            // unless it holds a single entry. End Turn is always last.
            var order = new[] { ActionGroup.Main, ActionGroup.Spells, ActionGroup.Abilities, ActionGroup.Actions, ActionGroup.Items, ActionGroup.End };
            foreach (var g in order)
            {
                var idx = Enumerable.Range(0, _actions.Count).Where(i => _actions[i].Group == g).ToList();
                if (idx.Count == 0) continue;
                if (g == ActionGroup.Main || g == ActionGroup.End || idx.Count == 1)
                    foreach (var i in idx) _buttons.Add(new BarButton { Group = g, Actions = { i } });
                else
                    _buttons.Add(new BarButton { Group = g, Actions = idx });
            }

            float x = 4;
            foreach (var b in _buttons)
            {
                bool isGroup = b.Actions.Count > 1;
                bool holdsSelected = b.Actions.Contains(selected);
                bool anyEnabled = b.Actions.Any(i => _actions[i].Enabled);
                string text;
                if (isGroup)
                    text = GroupTitle(b.Group) + (holdsSelected ? $": {_actions[selected].Name}" : "") + " <color=#9a8a70>▴</color>";
                else
                {
                    var a = _actions[b.Actions[0]];
                    text = KeyFor(b.Actions[0]) + ShortName(a) + (a.Name == "End Turn" && !Slot.Input.UsesMouse ? " (X)" : "");
                }
                b.Rt = UIKit.Rect(_bar, "B" + _buttons.IndexOf(b), new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                b.Bg = UIKit.Image(b.Rt, Color.white);
                b.Label = UIKit.Label(b.Rt, text, 9, anyEnabled ? UIKit.Parchment : UIKit.Dim, TextAnchor.MiddleCenter);
                b.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                float w = Mathf.Max(30, b.Label.preferredWidth + 12);
                b.Rt.offsetMin = new Vector2(x, 3);
                b.Rt.offsetMax = new Vector2(x + w, -3);
                b.Bg.color = holdsSelected ? new Color32(201, 160, 92, 150)
                    : _open == b.Group && isGroup ? new Color32(201, 160, 92, 80)
                    : b.Group == ActionGroup.End ? new Color32(120, 50, 45, 160)
                    : new Color32(60, 46, 40, 200);
                x += w + Gap;
            }
            float barW = x + 2;
            _bar.sizeDelta = new Vector2(barW, BarH);
            // Too wide for a half-width screen: shrink the whole bar rather than spill off the edge.
            float max = Width - 8;
            _bar.localScale = barW > max ? Vector3.one * (max / barW) : Vector3.one;

            if (_open.HasValue && !_buttons.Any(b => b.Group == _open && b.Actions.Count > 1)) _open = null;
            BuildFlyout(selected);
        }

        void BuildFlyout(int selected)
        {
            UIKit.Clear(_flyout);
            _rows.Clear();
            var btn = _open.HasValue ? _buttons.FirstOrDefault(b => b.Group == _open && b.Actions.Count > 1) : null;
            _flyout.gameObject.SetActive(btn != null);
            if (btn == null) return;

            float w = 90;
            var labels = new List<Text>();
            for (int r = 0; r < btn.Actions.Count; r++)
            {
                int ai = btn.Actions[r];
                var a = _actions[ai];
                var rt = UIKit.Rect(_flyout, "R" + r, new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -4 - (r + 1) * RowH), new Vector2(-4, -4 - r * RowH));
                var bg = UIKit.Image(rt, new Color(0, 0, 0, 0));
                string cost = a.Cost == ActionCost.Bonus ? " <color=#9ab8ff>bonus</color>" : "";
                var lbl = UIKit.Label(rt, KeyFor(ai) + a.Name + cost, 9, a.Enabled ? UIKit.Parchment : UIKit.Dim, TextAnchor.MiddleLeft);
                lbl.rectTransform.offsetMin = new Vector2(4, 0);
                lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                w = Mathf.Max(w, lbl.preferredWidth + 16);
                _rows.Add((ai, rt, bg));
            }
            float h = btn.Actions.Count * RowH + 8;
            _flyout.sizeDelta = new Vector2(w, h);
            // Sit right above the group's button, kept on screen.
            float scale = _bar.localScale.x;
            float barLeft = -_bar.sizeDelta.x * scale / 2;
            float btnCenter = barLeft + (btn.Rt.offsetMin.x + btn.Rt.offsetMax.x) / 2 * scale;
            float half = Width / 2 - 4;
            float cx = Mathf.Clamp(btnCenter, -half + w / 2, half - w / 2);
            _flyout.anchoredPosition = new Vector2(cx, 4 + BarH * scale + Gap);
        }

        /// <summary>
        /// Handles a left click on the combat UI. Returns the index of an action to use/select,
        /// -2 if the click was used by the bar itself (opened/closed a group, or dismissed the pop-up), or -1.
        /// </summary>
        public int ActionClicked(PlayerInput input)
        {
            if (!input.UsesMouse || !input.MouseLeft || _actions == null || !_bar.gameObject.activeSelf) return -1;
            var m = input.MouseScreen;
            if (_open.HasValue)
                foreach (var r in _rows)
                    if (UIKit.Contains(r.rt, m, Cam)) { _open = null; _signature = null; return r.action; }
            foreach (var b in _buttons)
            {
                if (!UIKit.Contains(b.Rt, m, Cam)) continue;
                if (b.Actions.Count == 1) { _open = null; _signature = null; return b.Actions[0]; }
                _open = _open == b.Group ? (ActionGroup?)null : b.Group;
                _signature = null;
                Sfx.Play("ui");
                return -2;
            }
            if (_open.HasValue) { _open = null; _signature = null; return -2; } // click on the map just closes the pop-up
            return -1;
        }

        /// <summary>Opens a group's pop-up as if its button were clicked (used by tests).</summary>
        public void OpenGroup(ActionGroup g) { _open = g; _signature = null; }

        /// <summary>Closes the group pop-up if it's open. Returns true if it was.</summary>
        public bool CloseFlyout()
        {
            if (!_open.HasValue) return false;
            _open = null;
            _signature = null;
            return true;
        }

        public bool PointerOverUi(PlayerInput input) =>
            input.UsesMouse && (UIKit.Contains(_bar, input.MouseScreen, Cam) || UIKit.Contains(_flyout, input.MouseScreen, Cam) || UIKit.Contains(_tracker, input.MouseScreen, Cam));

        // Combat input is read by the combat coroutine; this screen only displays.
        public override void Tick() { }
    }
}
