using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>Initiative tracker (top) and the action bar (bottom, only on your turn).</summary>
    public class CombatHud : GameScreen
    {
        readonly CombatInstance _combat;
        RectTransform _tracker, _bar, _rows;
        Text _trackerText, _resources, _hint;
        readonly List<RectTransform> _rowRects = new List<RectTransform>();

        public CombatHud(CombatInstance c) { _combat = c; }

        public override void Build()
        {
            Root = UIKit.Fill(Hud.Root, "CombatHud");
            _tracker = UIKit.Box(Root, "Tracker", new Vector2(0.5f, 1), new Vector2(360, 30), new Vector2(0, -80));
            UIKit.Panel(_tracker, true);
            _trackerText = UIKit.Label(_tracker, "", 9, UIKit.Parchment, TextAnchor.MiddleCenter);

            _bar = UIKit.Box(Root, "ActionBar", new Vector2(1, 0), new Vector2(210, 250), new Vector2(-6, 6));
            UIKit.Panel(_bar, true);
            var res = UIKit.Rect(_bar, "Res", new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -30), new Vector2(-8, -4));
            _resources = UIKit.Label(res, "", 9, UIKit.Gold);
            _rows = UIKit.Rect(_bar, "Rows", new Vector2(0, 0), new Vector2(1, 1), new Vector2(6, 34), new Vector2(-6, -32));
            var hint = UIKit.Rect(_bar, "Hint", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 4), new Vector2(-8, 32));
            _hint = UIKit.Label(hint, "", 8, UIKit.Parchment);
            _bar.gameObject.SetActive(false);
            RefreshTracker(_combat);
        }

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
        }

        /// <summary>Enemies show a description instead of exact HP (like a DM would).</summary>
        static string HealthWord(Creature c)
        {
            float f = c.HP / (float)Mathf.Max(1, c.MaxHP);
            return f >= 1 ? "(unhurt)" : f > 0.5f ? "(hurt)" : f > 0.25f ? "(bloodied)" : "(barely standing)";
        }

        public void ShowActions(Unit u, List<CombatAction> actions, int selected, string hint)
        {
            if (u == null || actions == null) { _bar.gameObject.SetActive(false); return; }
            _bar.gameObject.SetActive(true);
            var t = u.Cb.Turn;
            string dot(bool used) => used ? "<color=#666666>○</color>" : "<color=#7ad27a>●</color>";
            _resources.text = $"Action {dot(t.ActionUsed)}  Bonus {dot(t.BonusActionUsed)}  Reaction {dot(t.ReactionUsed)}\nMove {t.MovementLeftFt} ft";
            bool rebuild = _rowRects.Count != actions.Count;
            if (rebuild)
            {
                UIKit.Clear(_rows);
                _rowRects.Clear();
                for (int i = 0; i < actions.Count; i++)
                {
                    var rt = UIKit.Rect(_rows, "A" + i, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -(i + 1) * 12.5f), new Vector2(0, -i * 12.5f));
                    UIKit.Image(rt, new Color(0, 0, 0, 0));
                    UIKit.Label(rt, "", 9, UIKit.Parchment, TextAnchor.MiddleLeft);
                    _rowRects.Add(rt);
                }
            }
            bool mouse = Slot.Input.UsesMouse;
            for (int i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                var img = _rowRects[i].GetComponent<Image>();
                var lbl = _rowRects[i].GetComponentInChildren<Text>();
                img.color = i == selected ? new Color32(201, 160, 92, 140) : new Color(0, 0, 0, 0);
                string key = mouse ? (i == 0 ? "  " : i <= 9 ? (i % 10).ToString() : i == 10 ? "-" : i == 11 ? "=" : " ") : "";
                string cost = a.Cost == ActionCost.Bonus ? " <color=#9ab8ff>(bonus)</color>" : a.Cost == ActionCost.Free ? "" : "";
                string name = a.Enabled ? a.Name : $"<color=#777777>{a.Name}</color>";
                lbl.text = $"{(mouse ? key + " " : "")}{name}{cost}";
            }
            string tip = selected >= 0 && selected < actions.Count && actions[selected].Tooltip != null ? actions[selected].Tooltip + "\n" : "";
            _hint.text = tip + (hint ?? "") + (mouse ? "\nSpace: end turn · Right-click: cancel" : "\nLB/RB: choose · A: do it · X: end turn · B: cancel");
        }

        public int ActionClicked(PlayerInput input)
        {
            if (!input.UsesMouse || !input.MouseLeft || !_bar.gameObject.activeSelf) return -1;
            for (int i = 0; i < _rowRects.Count; i++)
                if (UIKit.Contains(_rowRects[i], input.MouseScreen, Cam)) return i;
            return -1;
        }

        public bool PointerOverUi(PlayerInput input) =>
            input.UsesMouse && (UIKit.Contains(_bar, input.MouseScreen, Cam) || UIKit.Contains(_tracker, input.MouseScreen, Cam));

        // Combat input is read by the combat coroutine; this screen only displays.
        public override void Tick() { }
    }
}
