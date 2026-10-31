using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// The controller's item wheel (hold A on an item). Options sit around a circle, the first at the top, going clockwise.
    /// Point with the left stick; let go of A on an option to pick it (or press A); B closes. Nothing is picked if you
    /// let go of A without pointing anywhere, so the wheel stays open for a careful choice.
    /// </summary>
    public class RadialMenu : GameScreen
    {
        readonly List<(string label, Action act)> _opts;
        readonly List<Text> _labels = new List<Text>();
        readonly List<Image> _wedges = new List<Image>();
        int _highlight = -1;
        bool _waitingForRelease;

        public RadialMenu(List<(string label, Action act)> options) { _opts = options; }

        public IEnumerable<string> Labels { get { foreach (var (label, _) in _opts) yield return label; } }
        public string Highlighted => _highlight >= 0 ? _opts[_highlight].label : null;
        public override bool ShowCloseButton => false;

        public override void Build()
        {
            Root = UIKit.Fill(Hud.Root, "RadialMenu");
            UIKit.Image(Root, new Color(0, 0, 0, 0.35f));
            float radius = 78;
            for (int i = 0; i < _opts.Count; i++)
            {
                float ang = i * Mathf.PI * 2 / _opts.Count; // 0 = up, clockwise
                var pos = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * radius;
                var box = UIKit.Box(Root, "Opt" + i, new Vector2(0.5f, 0.5f), new Vector2(118, 26), pos);
                var bg = UIKit.Image(box, new Color(0.95f, 0.9f, 0.78f, 0.95f));
                _wedges.Add(bg);
                var t = UIKit.Label(box, _opts[i].label, 9, UIKit.Ink, TextAnchor.MiddleCenter);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 6; t.resizeTextMaxSize = 9;
                _labels.Add(t);
            }
            var hint = UIKit.Box(Root, "Hint", new Vector2(0.5f, 0.5f), new Vector2(160, 20), Vector2.zero);
            UIKit.Label(hint, "Stick: point · let go of A: pick · B: close", 7, UIKit.Parchment, TextAnchor.MiddleCenter);
            _waitingForRelease = In.ConfirmHeld;
        }

        public override void Tick()
        {
            var stick = In.Move;
            if (stick.magnitude > 0.5f)
            {
                float ang = Mathf.Atan2(stick.x, stick.y) * Mathf.Rad2Deg; // 0 = up, clockwise
                if (ang < 0) ang += 360;
                float step = 360f / _opts.Count;
                _highlight = Mathf.RoundToInt(ang / step) % _opts.Count;
            }
            for (int i = 0; i < _wedges.Count; i++)
                _wedges[i].color = i == _highlight ? new Color(1f, 0.8f, 0.35f, 1f) : new Color(0.95f, 0.9f, 0.78f, 0.95f);

            if (In.Cancel) { Close(); return; }
            if (_waitingForRelease && !In.ConfirmHeld)
            {
                _waitingForRelease = false;
                if (_highlight >= 0) { Pick(); return; }
            }
            else if (!_waitingForRelease && In.Confirm && _highlight >= 0) Pick();
        }

        void Pick()
        {
            var act = _opts[_highlight].act;
            Close();
            act?.Invoke();
        }
    }
}
