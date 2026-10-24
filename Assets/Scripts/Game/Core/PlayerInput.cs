using UnityEngine;
using UnityEngine.InputSystem;

namespace DnD.Game
{
    /// <summary>
    /// One player's input, regardless of device. Default bindings follow Baldur's Gate 3 where they apply:
    ///   Keyboard/mouse: WASD move, E/left-click interact, I inventory, C character, K spells, J journal,
    ///   Space end turn, 1-0 hotbar, Esc menu/back, Left Alt highlight, Ctrl sneak, Shift+click quick-transfer.
    ///   Gamepad: left stick move, A confirm/interact, B back, X end turn, Y inventory, View character,
    ///   Menu pause, LB/RB cycle actions, D-pad navigate, right stick moves the combat cursor, L3 sneak.
    /// </summary>
    public abstract class PlayerInput
    {
        public abstract string DeviceName { get; }
        public abstract bool IsConnected { get; }
        public virtual bool UsesMouse => false;

        public abstract Vector2 Move { get; }
        public abstract bool Confirm { get; }      // interact / select / roll the dice
        public abstract bool Cancel { get; }       // back / close
        public abstract bool Inventory { get; }
        public abstract bool CharacterSheet { get; }
        public abstract bool Spells { get; }
        public abstract bool Journal { get; }
        public abstract bool EndTurn { get; }
        public abstract bool Pause { get; }
        public abstract bool NextTab { get; }
        public abstract bool PrevTab { get; }
        public abstract bool AltAction { get; }    // split stack / secondary
        public abstract bool QuickModifier { get; } // shift (quick-transfer)
        public abstract bool SneakToggle { get; }
        public abstract bool Highlight { get; }
        public abstract int HotbarPressed { get; } // 0-11 or -1
        public abstract Vector2 Cursor { get; }    // right stick (gamepad) for combat cursor
        public abstract bool UseItem { get; }      // inventory: use / equip (KB: U, pad: Y)
        public abstract bool Sort { get; }         // inventory: auto-sort (KB: T, pad: View)
        public abstract bool Give { get; }         // trade: give gold to partner (KB: G, pad: X)

        /// <summary>While typing (character names), letter keys must not act as game keys.</summary>
        public virtual bool TypingMode { get; set; }

        // Menu navigation with key-repeat (cached per frame so several readers agree).
        float _navTimer;
        Vector2Int _lastNav, _navCached;
        int _navFrame = -1;
        public Vector2Int Nav()
        {
            if (_navFrame == Time.frameCount) return _navCached;
            _navFrame = Time.frameCount;
            _navCached = NavCompute();
            return _navCached;
        }

        Vector2Int NavCompute()
        {
            Vector2 m = NavRaw();
            var dir = Vector2Int.zero;
            if (Mathf.Abs(m.x) > 0.5f || Mathf.Abs(m.y) > 0.5f)
                dir = Mathf.Abs(m.x) > Mathf.Abs(m.y) ? new Vector2Int((int)Mathf.Sign(m.x), 0) : new Vector2Int(0, (int)Mathf.Sign(m.y));
            if (dir == Vector2Int.zero) { _lastNav = dir; _navTimer = 0; return dir; }
            if (dir != _lastNav) { _lastNav = dir; _navTimer = 0.35f; return dir; }
            _navTimer -= Time.unscaledDeltaTime;
            if (_navTimer <= 0) { _navTimer = 0.09f; return dir; }
            return Vector2Int.zero;
        }
        protected internal abstract Vector2 NavRaw();

        // Mouse (KB/M only)
        public virtual Vector2 MouseScreen => Vector2.zero;
        public virtual bool MouseLeft => false;
        public virtual bool MouseRight => false;
        public virtual float Scroll => 0;
    }

    public sealed class KeyboardMouseInput : PlayerInput
    {
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;

        public override string DeviceName => "Keyboard & Mouse";
        public override bool IsConnected => K != null;
        public override bool UsesMouse => M != null;

        public override Vector2 Move
        {
            get
            {
                if (K == null) return Vector2.zero;
                var v = Vector2.zero;
                if (Held(Key.W) || Held(Key.UpArrow)) v.y += 1;
                if (Held(Key.S) || Held(Key.DownArrow)) v.y -= 1;
                if (Held(Key.D) || Held(Key.RightArrow)) v.x += 1;
                if (Held(Key.A) || Held(Key.LeftArrow)) v.x -= 1;
                return v.sqrMagnitude > 1 ? v.normalized : v;
            }
        }

        bool P(Key k)
        {
            if (K == null || !K[k].wasPressedThisFrame) return false;
            if (!TypingMode) return true;
            return k == Key.Enter || k == Key.NumpadEnter || k == Key.Escape || k == Key.UpArrow || k == Key.DownArrow || k == Key.LeftArrow || k == Key.RightArrow;
        }

        bool Held(Key k) => K != null && K[k].isPressed && (!TypingMode || k == Key.UpArrow || k == Key.DownArrow || k == Key.LeftArrow || k == Key.RightArrow);

        public override bool Confirm => P(Key.E) || P(Key.Enter) || P(Key.NumpadEnter) || P(Key.F);
        public override bool Cancel => P(Key.Escape) || P(Key.Backspace);
        public bool AnyActivity => (K != null && K.anyKey.wasPressedThisFrame) || (M != null && (M.delta.ReadValue().sqrMagnitude > 4f || M.leftButton.wasPressedThisFrame || M.rightButton.wasPressedThisFrame));
        public override bool Inventory => P(Key.I) || P(Key.Tab);
        public override bool CharacterSheet => P(Key.C);
        public override bool Spells => P(Key.K);
        public override bool Journal => P(Key.J) || P(Key.L);
        public override bool EndTurn => P(Key.Space);
        public override bool Pause => P(Key.Escape);
        public override bool NextTab => P(Key.RightBracket) || P(Key.R);
        public override bool PrevTab => P(Key.LeftBracket) || P(Key.Q);
        public override bool AltAction => P(Key.X);
        public override bool QuickModifier => K != null && !TypingMode && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed);
        public override bool SneakToggle => P(Key.LeftCtrl) || P(Key.RightCtrl);
        public override bool Highlight => K != null && !TypingMode && K.leftAltKey.isPressed;
        public override Vector2 Cursor => Vector2.zero;
        public override bool UseItem => P(Key.U) || (M != null && M.middleButton.wasPressedThisFrame);
        public override bool Sort => P(Key.T);
        public override bool Give => P(Key.G);

        static readonly Key[] HotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals };
        public override int HotbarPressed
        {
            get
            {
                if (K == null || TypingMode) return -1;
                for (int i = 0; i < HotKeys.Length; i++) if (K[HotKeys[i]].wasPressedThisFrame) return i;
                return -1;
            }
        }

        protected internal override Vector2 NavRaw()
        {
            if (K == null) return Vector2.zero;
            var v = Vector2.zero;
            if (Held(Key.W) || Held(Key.UpArrow)) v.y += 1;
            if (Held(Key.S) || Held(Key.DownArrow)) v.y -= 1;
            if (Held(Key.D) || Held(Key.RightArrow)) v.x += 1;
            if (Held(Key.A) || Held(Key.LeftArrow)) v.x -= 1;
            return v;
        }

        public override Vector2 MouseScreen => M != null ? M.position.ReadValue() : Vector2.zero;
        public override bool MouseLeft => M != null && M.leftButton.wasPressedThisFrame;
        public override bool MouseRight => M != null && M.rightButton.wasPressedThisFrame;
        public override float Scroll => M != null ? M.scroll.ReadValue().y : 0;
    }

    public sealed class GamepadInput : PlayerInput
    {
        public readonly Gamepad Pad;
        public GamepadInput(Gamepad pad) { Pad = pad; }

        public override string DeviceName => Pad != null ? Pad.displayName : "Gamepad";
        public override bool IsConnected => Pad != null && Pad.added;

        public override Vector2 Move
        {
            get
            {
                if (!IsConnected) return Vector2.zero;
                var v = Pad.leftStick.ReadValue();
                if (v.magnitude < 0.2f) v = Vector2.zero;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public override bool Confirm => IsConnected && Pad.buttonSouth.wasPressedThisFrame;
        public override bool Cancel => IsConnected && Pad.buttonEast.wasPressedThisFrame;
        public bool AnyActivity
        {
            get
            {
                if (!IsConnected) return false;
                foreach (var c in Pad.allControls)
                    if (c is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) return true;
                return Pad.leftStick.ReadValue().sqrMagnitude > 0.25f || Pad.rightStick.ReadValue().sqrMagnitude > 0.25f;
            }
        }
        public override bool Inventory => IsConnected && Pad.buttonNorth.wasPressedThisFrame;
        public override bool CharacterSheet => IsConnected && Pad.selectButton.wasPressedThisFrame;
        public override bool Spells => IsConnected && Pad.rightTrigger.wasPressedThisFrame;
        public override bool Journal => IsConnected && Pad.leftTrigger.wasPressedThisFrame;
        public override bool EndTurn => IsConnected && Pad.buttonWest.wasPressedThisFrame;
        public override bool Pause => IsConnected && Pad.startButton.wasPressedThisFrame;
        public override bool NextTab => IsConnected && Pad.rightShoulder.wasPressedThisFrame;
        public override bool PrevTab => IsConnected && Pad.leftShoulder.wasPressedThisFrame;
        public override bool AltAction => IsConnected && Pad.buttonWest.wasPressedThisFrame;
        public override bool QuickModifier => IsConnected && Pad.rightStickButton.isPressed;
        public override bool SneakToggle => IsConnected && Pad.leftStickButton.wasPressedThisFrame;
        public override bool Highlight => IsConnected && Pad.rightStickButton.isPressed;
        public override int HotbarPressed => -1;
        public override bool UseItem => IsConnected && Pad.buttonNorth.wasPressedThisFrame;
        public override bool Sort => IsConnected && Pad.selectButton.wasPressedThisFrame;
        public override bool Give => IsConnected && Pad.buttonWest.wasPressedThisFrame;

        public override Vector2 Cursor
        {
            get
            {
                if (!IsConnected) return Vector2.zero;
                var v = Pad.rightStick.ReadValue();
                return v.magnitude < 0.25f ? Vector2.zero : v;
            }
        }

        protected internal override Vector2 NavRaw()
        {
            if (!IsConnected) return Vector2.zero;
            var d = Pad.dpad.ReadValue();
            if (d.sqrMagnitude > 0.1f) return d;
            return Pad.leftStick.ReadValue();
        }
    }

    /// <summary>
    /// Solo player input: keyboard + mouse AND any controller not owned by another player, hot-swappable.
    /// Button prompts follow whichever device was touched last.
    /// </summary>
    public sealed class HybridInput : PlayerInput
    {
        readonly KeyboardMouseInput _kb = new KeyboardMouseInput();
        readonly System.Collections.Generic.Dictionary<Gamepad, GamepadInput> _pads = new System.Collections.Generic.Dictionary<Gamepad, GamepadInput>();
        readonly System.Collections.Generic.List<GamepadInput> _active = new System.Collections.Generic.List<GamepadInput>();
        /// <summary>Returns true if a pad belongs to another player.</summary>
        public System.Func<Gamepad, bool> Claimed;
        bool _padLast;
        int _frame = -1;
        public Gamepad ActivePad { get; private set; }

        public override bool TypingMode { get => _kb.TypingMode; set => _kb.TypingMode = value; }

        void Refresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _active.Clear();
            foreach (var pad in Gamepad.all)
            {
                if (Claimed != null && Claimed(pad)) continue;
                if (!_pads.TryGetValue(pad, out var gi)) _pads[pad] = gi = new GamepadInput(pad);
                _active.Add(gi);
                if (gi.AnyActivity) { _padLast = true; ActivePad = pad; }
            }
            if (_kb.AnyActivity) _padLast = false;
        }

        bool Any(System.Func<PlayerInput, bool> f)
        {
            Refresh();
            if (f(_kb)) return true;
            foreach (var p in _active) if (f(p)) return true;
            return false;
        }

        public override string DeviceName => _padLast ? "Controller" : "Keyboard & Mouse";
        public override bool IsConnected => true;
        public override bool UsesMouse { get { Refresh(); return !_padLast && Mouse.current != null; } }

        public override Vector2 Move
        {
            get
            {
                Refresh();
                Vector2 best = _kb.Move;
                foreach (var p in _active) { var m = p.Move; if (m.sqrMagnitude > best.sqrMagnitude) best = m; }
                return best;
            }
        }

        public override bool Confirm => Any(p => p.Confirm);
        public override bool Cancel => Any(p => p.Cancel);
        public override bool Inventory => Any(p => p.Inventory);
        public override bool CharacterSheet => Any(p => p.CharacterSheet);
        public override bool Spells => Any(p => p.Spells);
        public override bool Journal => Any(p => p.Journal);
        public override bool EndTurn => Any(p => p.EndTurn);
        public override bool Pause => Any(p => p.Pause);
        public override bool NextTab => Any(p => p.NextTab);
        public override bool PrevTab => Any(p => p.PrevTab);
        public override bool AltAction => Any(p => p.AltAction);
        public override bool QuickModifier => Any(p => p.QuickModifier);
        public override bool SneakToggle => Any(p => p.SneakToggle);
        public override bool Highlight => Any(p => p.Highlight);
        public override bool UseItem => Any(p => p.UseItem);
        public override bool Sort => Any(p => p.Sort);
        public override bool Give => Any(p => p.Give);
        public override int HotbarPressed { get { Refresh(); return _kb.HotbarPressed; } }

        public override Vector2 Cursor
        {
            get
            {
                Refresh();
                foreach (var p in _active) { var c = p.Cursor; if (c != Vector2.zero) return c; }
                return Vector2.zero;
            }
        }

        protected internal override Vector2 NavRaw()
        {
            Refresh();
            Vector2 best = _kb.NavRaw();
            foreach (var p in _active) { var m = p.NavRaw(); if (m.sqrMagnitude > best.sqrMagnitude) best = m; }
            return best;
        }

        public override Vector2 MouseScreen => _kb.MouseScreen;
        public override bool MouseLeft => UsesMouse && _kb.MouseLeft;
        public override bool MouseRight => UsesMouse && _kb.MouseRight;
        public override float Scroll => _kb.Scroll;
    }

}
