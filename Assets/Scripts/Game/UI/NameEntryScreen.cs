using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// "Name your dragon": type a name on the keyboard (letters, spaces, ' and -, up to 16), or cycle suggestions with a
    /// controller. Enter / (A) on "That's the name!" accepts; Esc / (B) / ✕ keeps whatever is typed. Empty = the default.
    /// </summary>
    public class NameEntryScreen : GameScreen
    {
        public const string Default = "Cinder";
        public static readonly string[] Suggestions = { "Cinder", "Ember", "Pebble", "Sir Toasty", "Biscuit" };

        public string Typed { get; private set; } = "";
        readonly Action<string> _onName;
        bool _done;
        int _suggestion = -1;
        Text _field;
        MenuList _menu;

        public NameEntryScreen(Action<string> onName) { _onName = onName; }

        public override void Build()
        {
            Window(new Vector2(300, 150), "Name your dragon");
            var t = UIKit.Rect(Root, "Prompt", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -52), new Vector2(-12, -26));
            UIKit.Label(t, "The hatchling chirps expectantly. Type a name" + (In.UsesMouse ? "" : " (or pick a suggestion)") + ":", 10, UIKit.Ink);
            var f = UIKit.Rect(Root, "Field", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -74), new Vector2(-12, -54));
            UIKit.Image(f, new Color(1, 1, 1, 0.35f));
            _field = UIKit.Label(f, "", 12, UIKit.Ink, TextAnchor.MiddleCenter);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -78));
            _menu = new MenuList(list) { RowHeight = 18, FontSize = 11 };
            _menu.Add("That's the name!", Submit);
            _menu.Add("Suggest another", () =>
            {
                _suggestion = (_suggestion + 1) % Suggestions.Length;
                Typed = Suggestions[_suggestion];
                Refresh();
            });
            _menu.Build();
            In.TypingMode = true;
            if (Keyboard.current != null) Keyboard.current.onTextInput += Type;
            Refresh();
        }

        void Refresh()
        {
            if (_field != null) _field.text = Typed.Length > 0 ? $"<b>{Typed}</b>_" : $"<color=#8a7a6a>{Default}</color>_";
        }

        /// <summary>One typed character (keyboard text input; tests call it directly).</summary>
        public void Type(char c)
        {
            if (_done) return;
            if (c == '\b') { if (Typed.Length > 0) Typed = Typed.Substring(0, Typed.Length - 1); }
            else if (!char.IsControl(c) && Typed.Length < 16 && (char.IsLetter(c) || c == ' ' || c == '\'' || c == '-')) Typed += c;
            else return;
            Refresh();
        }

        public void Submit()
        {
            if (_done) return;
            _done = true;
            _onName?.Invoke(string.IsNullOrWhiteSpace(Typed) ? Default : Typed.Trim());
            Close();
        }

        public override void Tick()
        {
            _menu?.Tick(In, Cam);
            if (In.Cancel) Close();
        }

        public override void OnClose()
        {
            In.TypingMode = false;
            if (Keyboard.current != null) Keyboard.current.onTextInput -= Type;
            if (!_done && !Hud.ClosingAll) Submit(); // Esc or ✕: keep what's typed (or the default)
        }
    }
}
