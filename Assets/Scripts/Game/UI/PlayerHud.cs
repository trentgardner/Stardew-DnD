using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>Base class for modal per-player screens (inventory, dialogue, shop...).</summary>
    public abstract class GameScreen
    {
        public PlayerHud Hud;
        public RectTransform Root;
        public bool Closed;
        /// <summary>Frame this screen opened on: it ignores input until the next frame, so the key that opened it can't also close it.</summary>
        public int OpenedFrame = -1;
        /// <summary>Most windows get a ✕ button (and close on Esc / B). Screens that must be finished (creation, forced choices) say no.</summary>
        public virtual bool ShowCloseButton => true;
        RectTransform _closeBtn;
        protected PlayerSlot Slot => Hud.Slot;
        protected PlayerInput In => Hud.Slot.Input;
        protected Camera Cam => Hud.Slot.Cam;

        public abstract void Build();
        public abstract void Tick();
        public virtual void OnClose() { }
        public void Close() { Closed = true; if (Root) Root.gameObject.SetActive(false); } // gone from the screen now, removed next frame

        /// <summary>A ✕ in the window's top-right corner, labelled with the device's back button.</summary>
        protected void AddCloseButton()
        {
            if (!ShowCloseButton || Root == null) return;
            bool mouse = Hud.Slot.Input.UsesMouse;
            _closeBtn = UIKit.Rect(Root, "Close", new Vector2(1, 1), new Vector2(1, 1), new Vector2(mouse ? -48 : -36, -18), new Vector2(-4, -4));
            UIKit.Image(_closeBtn, new Color32(120, 50, 45, 220));
            UIKit.Label(_closeBtn, mouse ? "✕ Esc" : "✕ (B)", 8, UIKit.Parchment, TextAnchor.MiddleCenter);
        }

        public bool CloseClicked(PlayerInput input) =>
            _closeBtn != null && input.UsesMouse && input.MouseLeft && UIKit.Contains(_closeBtn, input.MouseScreen, Hud.Slot.Cam);

        protected RectTransform Window(Vector2 size, string title)
        {
            Root = UIKit.Box(Hud.Root, GetType().Name, new Vector2(0.5f, 0.5f), size, Vector2.zero);
            UIKit.Panel(Root);
            if (title != null)
            {
                var t = UIKit.Rect(Root, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -24), new Vector2(-10, -4));
                var lbl = UIKit.Label(t, title, 15, UIKit.Ink, TextAnchor.MiddleCenter);
                lbl.fontStyle = FontStyle.Bold;
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 9; lbl.resizeTextMaxSize = 15; // a long title shrinks onto one line
                lbl.verticalOverflow = VerticalWrapMode.Truncate;
            }
            AddCloseButton();
            return Root;
        }
    }

    /// <summary>
    /// One player's on-screen interface, drawn on a canvas bound to that player's split-screen camera.
    /// </summary>
    public class PlayerHud : MonoBehaviour
    {
        public PlayerSlot Slot;
        public Canvas Canvas;
        public RectTransform Root;
        public DiceRoller Dice;

        readonly List<GameScreen> _screens = new List<GameScreen>();
        public bool HasScreen => _screens.Count > 0;
        /// <summary>Frame a screen last closed on. The world ignores menu keys that frame (Esc closing a menu must not reopen the pause menu).</summary>
        public int LastClosedFrame = -1;
        public GameScreen Top => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        Image _tint, _flash;
        RectTransform _status, _clock, _hotbar, _prompt, _narrator, _toasts, _log;
        Text _statusText, _hpText, _statsText, _clockText, _promptText, _narratorText, _logText;
        Image _hpFill;
        readonly List<Image> _hotIcons = new List<Image>();
        readonly List<Text> _hotCounts = new List<Text>();
        readonly List<Image> _hotBgs = new List<Image>();
        int _hotbarIndex;
        /// <summary>The selected hotbar slot. Changing it flashes the item's name above the bar.</summary>
        public int HotbarIndex
        {
            get => _hotbarIndex;
            set
            {
                _hotbarIndex = value;
                var st = Slot?.Character?.Bag.Slots[value];
                if (_hotLabelBox == null) return;
                if (st == null) { _hotLabelBox.gameObject.SetActive(false); return; }
                _hotLabel.text = ItemsDb.Get(st.ItemId)?.Name ?? "";
                _hotLabelBox.gameObject.SetActive(true);
                _hotLabelBox.SetAsLastSibling(); // over the interact prompt and the narrator for its two seconds
                _hotLabelUntil = Time.unscaledTime + 2f;
            }
        }
        readonly List<RectTransform> _hotSlots = new List<RectTransform>();
        readonly List<Outline> _hotOutlines = new List<Outline>();
        Text _hotLabel, _hotHint;
        RectTransform _hotLabelBox;
        float _hotLabelUntil;
        float _narratorTimer, _flashTimer;
        readonly List<string> _logLines = new List<string>();
        readonly List<(Text t, float time)> _toastItems = new List<(Text, float)>();
        RectTransform _party, _disconnect;
        public bool DisconnectBannerVisible => _disconnect != null && _disconnect.gameObject.activeSelf;
        readonly List<(Text name, Image fill, Text hp)> _partyRows = new List<(Text, Image, Text)>();

        public static PlayerHud Create(PlayerSlot slot)
        {
            var go = new GameObject($"HUD_P{slot.Index + 1}");
            DontDestroyOnLoad(go);
            var hud = go.AddComponent<PlayerHud>();
            hud.Slot = slot;
            hud.Canvas = go.AddComponent<Canvas>();
            hud.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            hud.Canvas.worldCamera = slot.Cam;
            hud.Canvas.planeDistance = 1f;
            hud.Canvas.sortingOrder = short.MaxValue; // above darkness and floating text
            hud.Canvas.pixelPerfect = true;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 1f;
            hud.Root = UIKit.Fill(go.transform, "Root");
            hud.BuildHud();
            hud.Dice = new DiceRoller(hud);
            return hud;
        }

        void BuildHud()
        {
            _tint = UIKit.Image(UIKit.Fill(Root, "NightTint"), new Color(0, 0, 0, 0));

            // Status (top-left): name line, HP bar with the numbers inside it, then stats and conditions.
            _status = UIKit.Box(Root, "Status", new Vector2(0, 1), new Vector2(170, 50), new Vector2(6, -6));
            UIKit.Panel(_status, true);
            _statusText = UIKit.Label(UIKit.Rect(_status, "Name", new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -18), new Vector2(-8, -5)), "", 10, UIKit.Parchment, TextAnchor.MiddleLeft);
            _statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var hpBack = UIKit.Rect(_status, "HPBack", new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -31), new Vector2(-8, -20));
            UIKit.Image(hpBack, new Color(0.25f, 0.08f, 0.08f, 1));
            var hpFillRt = UIKit.Fill(hpBack, "Fill");
            _hpFill = UIKit.Image(hpFillRt, UIKit.DamageRed, ArtRegistry.Get("ui_white"));
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpText = UIKit.Label(hpBack, "", 8, Color.white, TextAnchor.MiddleCenter);
            _hpText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            _statsText = UIKit.Label(UIKit.Rect(_status, "Stats", Vector2.zero, new Vector2(1, 1), new Vector2(8, 4), new Vector2(-8, -33)), "", 9, UIKit.Parchment, TextAnchor.UpperLeft);
            _statsText.verticalOverflow = VerticalWrapMode.Overflow;

            // Party frames (under the status panel): companions' and co-op partners' HP.
            _party = UIKit.Rect(Root, "Party", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            _party.pivot = new Vector2(0, 1);
            _party.sizeDelta = new Vector2(170, 0);

            // Clock (top-right)
            _clock = UIKit.Box(Root, "Clock", new Vector2(1, 1), new Vector2(120, 46), new Vector2(-6, -6));
            UIKit.Panel(_clock, true);
            _clockText = UIKit.Label(UIKit.Rect(_clock, "Text", Vector2.zero, Vector2.one, new Vector2(6, 3), new Vector2(-6, -3)), "", 10, UIKit.Parchment, TextAnchor.MiddleRight);

            // Hotbar (bottom)
            _hotbar = UIKit.Box(Root, "Hotbar", new Vector2(0.5f, 0), new Vector2(12 * 22 + 8, 30), new Vector2(0, 4));
            UIKit.Panel(_hotbar, true);
            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                var s = UIKit.Box(_hotbar, "Slot" + i, new Vector2(0, 0.5f), new Vector2(20, 20), new Vector2(5 + i * 22, 0));
                _hotSlots.Add(s);
                var bg = UIKit.Image(s, Color.white, ArtRegistry.Get("ui_slot"));
                _hotBgs.Add(bg);
                var ol = bg.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(1f, 0.78f, 0.2f, 1f);
                ol.effectDistance = new Vector2(1.5f, -1.5f);
                ol.enabled = false;
                _hotOutlines.Add(ol);
                var icon = UIKit.Fill(s, "Icon", 2);
                var img = UIKit.Image(icon, Color.white);
                img.enabled = false;
                _hotIcons.Add(img);
                var cnt = UIKit.Rect(s, "Count", Vector2.zero, Vector2.one, new Vector2(0, -2), new Vector2(0, 0));
                _hotCounts.Add(UIKit.Label(cnt, "", 8, UIKit.Ink, TextAnchor.LowerRight));
            }

            // The selected item's name (fades), and a key reminder beside the bar.
            _hotLabelBox = UIKit.Box(Root, "HotLabel", new Vector2(0.5f, 0), new Vector2(150, 15), new Vector2(0, 37));
            UIKit.Image(_hotLabelBox, new Color(0.08f, 0.06f, 0.05f, 0.85f));
            _hotLabel = UIKit.Label(_hotLabelBox, "", 9, UIKit.Parchment, TextAnchor.MiddleCenter);
            _hotLabelBox.gameObject.SetActive(false);
            var hh = UIKit.Box(Root, "HotHint", new Vector2(0.5f, 0), new Vector2(110, 26), new Vector2(12 * 11 + 4 + 60, 6));
            _hotHint = UIKit.Label(hh, "", 7, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft);
            _hotHint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            // Lost controller banner (center)
            _disconnect = UIKit.Box(Root, "Disconnected", new Vector2(0.5f, 0.5f), new Vector2(240, 40), Vector2.zero);
            UIKit.Panel(_disconnect, true);
            UIKit.Label(_disconnect, "Controller disconnected.\nPlug it back in, or press Start on any controller.", 10, UIKit.Parchment, TextAnchor.MiddleCenter);
            _disconnect.gameObject.SetActive(false);

            // Interaction prompt

            _prompt = UIKit.Box(Root, "Prompt", new Vector2(0.5f, 0), new Vector2(220, 18), new Vector2(0, 40));
            UIKit.Panel(_prompt, true);
            _promptText = UIKit.Label(_prompt, "", 10, UIKit.Gold, TextAnchor.MiddleCenter);
            _prompt.gameObject.SetActive(false);

            // DM narrator (parchment box)
            _narrator = UIKit.Box(Root, "Narrator", new Vector2(0.5f, 0), new Vector2(360, 54), new Vector2(0, 62));
            UIKit.Panel(_narrator);
            var nRt = UIKit.Rect(_narrator, "Text", Vector2.zero, Vector2.one, new Vector2(10, 6), new Vector2(-10, -6));
            _narratorText = UIKit.Label(nRt, "", 10, UIKit.Ink, TextAnchor.MiddleLeft);
            _narratorText.fontStyle = FontStyle.Italic;
            _narratorText.resizeTextForBestFit = true; _narratorText.resizeTextMinSize = 7; _narratorText.resizeTextMaxSize = 10; // a long line in a narrow half
            _narrator.gameObject.SetActive(false);

            // Toasts (right)
            _toasts = UIKit.Box(Root, "Toasts", new Vector2(1, 1), new Vector2(170, 200), new Vector2(-6, -56));

            // Combat / event log (bottom-left, small: the battlefield matters more)
            _log = UIKit.Box(Root, "Log", new Vector2(0, 0), new Vector2(160, 50), new Vector2(6, 4));
            UIKit.Panel(_log, true);
            _log.gameObject.AddComponent<RectMask2D>(); // clip old lines instead of spilling out
            _logText = UIKit.Label(UIKit.Rect(_log, "Text", Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4)), "", 8, UIKit.Parchment, TextAnchor.LowerLeft);
            _log.gameObject.SetActive(false);

            _flash = UIKit.Image(UIKit.Fill(Root, "Flash"), new Color(1, 1, 1, 0));
        }

        // ------------------------------------------------------------------ screens

        public void Push(GameScreen s)
        {
            s.Hud = this;
            s.OpenedFrame = Time.frameCount;
            s.Build();
            _screens.Add(s);
            Sfx.Play("ui");
        }

        /// <summary>True while CloseAll runs: screens skip follow-up callbacks (no new popups mid-teardown).</summary>
        public bool ClosingAll { get; private set; }

        public void CloseAll()
        {
            var copy = new List<GameScreen>(_screens);
            if (copy.Count > 0) LastClosedFrame = Time.frameCount;
            _screens.Clear();
            ClosingAll = true;
            for (int i = copy.Count - 1; i >= 0; i--)
            {
                copy[i].Closed = true;
                try { copy[i].OnClose(); }
                catch (System.Exception e) { Debug.LogException(e); }
                if (copy[i].Root) Destroy(copy[i].Root.gameObject);
            }
            ClosingAll = false;
        }

        public T Find<T>() where T : GameScreen
        {
            foreach (var s in _screens) if (s is T t) return t;
            return null;
        }

        void Update()
        {
            if (Slot == null) return;
            if (Slot.Character == null && !(Top is CharacterCreationScreen)) { if (HasScreen && !(Top is MessageScreen)) CloseAll(); }
            bool lost = Slot.Disconnected;
            if (_disconnect.gameObject.activeSelf != lost) { _disconnect.gameObject.SetActive(lost); if (lost) _disconnect.SetAsLastSibling(); }
            Dice.Update();
            if (!Dice.IsOpen && _screens.Count > 0)
            {
                var top = Top;
                if (top.OpenedFrame != Time.frameCount)
                {
                    if (top.CloseClicked(Slot.Input)) top.Close();
                    else top.Tick();
                }
                for (int i = _screens.Count - 1; i >= 0; i--)
                {
                    if (!_screens[i].Closed) continue;
                    LastClosedFrame = Time.frameCount;
                    _screens[i].OnClose();
                    if (_screens[i].Root) Destroy(_screens[i].Root.gameObject);
                    _screens.RemoveAt(i);
                }
            }
            RefreshHud();

            if (_narratorTimer > 0) { _narratorTimer -= Time.unscaledDeltaTime; if (_narratorTimer <= 0) _narrator.gameObject.SetActive(false); }
            // The narrator's box would sit on top of a window's bottom edge: it waits while one is open (the line is in the log).
            bool wantNarrator = _narratorTimer > 0 && !_screens.Any(s => !s.Closed);
            if (_narrator.gameObject.activeSelf != wantNarrator) _narrator.gameObject.SetActive(wantNarrator);
            if (_flashTimer > 0) { _flashTimer -= Time.unscaledDeltaTime; var c = _flash.color; c.a = Mathf.Clamp01(_flashTimer * 1.5f) * 0.35f; _flash.color = c; }
            for (int i = _toastItems.Count - 1; i >= 0; i--)
            {
                var (t, time) = _toastItems[i];
                float left = time - Time.unscaledTime;
                if (left <= 0) { _toastKey.Remove(t); Destroy(t.transform.parent.gameObject); _toastItems.RemoveAt(i); continue; }
                var col = t.color; col.a = Mathf.Clamp01(left); t.color = col;
            }
            for (int i = 0; i < _toastItems.Count; i++)
                ((RectTransform)_toastItems[i].t.transform.parent).anchoredPosition = new Vector2(0, -i * 20);
        }

        float _toastW = 170f;
        bool _statusTight;

        /// <summary>
        /// Fits the HUD to its width (a full screen is 640 wide; a split-screen half 290-320 on a TV or laptop, 240 on a 4:3 one).
        /// Top: the status card and party on the left, the clock on the right, toasts in the column under the clock (never over the
        /// status column). Bottom, from the edge up: hotbar · item label · interaction prompt · narrator, each in its own band.
        /// </summary>
        void Layout()
        {
            float w = Root.rect.width;
            if (w <= 0) return;
            const float edge = 6f, gap = 6f, statusW = 170f;
            // The clock gives way first, down to 96 wide; past that the status card narrows too.
            float clockW = Mathf.Clamp(w - statusW - 2 * edge - gap, 96f, 120f);
            float statusFit = Mathf.Min(statusW, w - clockW - 2 * edge - gap);
            _clock.sizeDelta = new Vector2(clockW, _clock.sizeDelta.y);
            _status.sizeDelta = new Vector2(statusFit, _status.sizeDelta.y);
            _party.sizeDelta = new Vector2(statusFit, _party.sizeDelta.y);
            bool tight = statusFit < statusW;
            _statusTight = tight;
            _statusText.horizontalOverflow = tight ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            _statusText.resizeTextForBestFit = tight; _statusText.resizeTextMinSize = 7; _statusText.resizeTextMaxSize = 10;
            foreach (var tx in new[] { _statusText, _statsText, _hpText })
                if (tx != null)
                {
                    tx.resizeTextForBestFit = tight; tx.resizeTextMinSize = 6; tx.resizeTextMaxSize = tx.fontSize;
                    tx.horizontalOverflow = tight ? HorizontalWrapMode.Wrap : tx.horizontalOverflow;
                    tx.verticalOverflow = tight ? VerticalWrapMode.Truncate : tx.verticalOverflow; // shrink, and never spill onto the bar below
                }
            FitLog();
            // Toasts: the right-hand column, as wide as the room beside the status column allows.
            _toastW = Mathf.Min(170f, w - edge - statusFit - gap - edge);
            // Under the clock; in a fight, under the round tracker too.
            float toastTop = -56f;
            float trackerBottom = TrackerBottom();
            if (!float.IsNaN(trackerBottom)) toastTop = Mathf.Min(toastTop, trackerBottom - 4f);
            _toasts.anchoredPosition = new Vector2(-6f, toastTop);
            // A narrow fight has no room for toast boxes beside the ticker and tracker: their lines go to the combat log instead.
            _toasts.gameObject.SetActive(!(Slot.InCombat && w < 600));
            _toasts.sizeDelta = new Vector2(_toastW, _toasts.sizeDelta.y);
            foreach (var (t, _) in _toastItems) if (t != null) ((RectTransform)t.transform.parent).sizeDelta = new Vector2(_toastW, 18);
            // The hotbar: twelve slots, shrunk to fit a narrow half.
            float step = Mathf.Min(22f, (w - 2 * edge - 8f) / 12f);
            _hotbar.sizeDelta = new Vector2(12 * step + 8f, _hotbar.sizeDelta.y);
            for (int i = 0; i < _hotSlots.Count; i++)
            {
                var sl = (RectTransform)_hotSlots[i].transform;
                sl.sizeDelta = new Vector2(step - 2f, step - 2f);
                sl.anchoredPosition = new Vector2(5f + i * step, 0);
            }
            // The key reminder only beside the bar when there's room for it.
            float hintRoom = (w - _hotbar.sizeDelta.x) / 2f - edge;
            _hotHint.transform.parent.gameObject.SetActive(hintRoom >= 112f && !Slot.InCombat);
            ((RectTransform)_hotHint.transform.parent).anchoredPosition = new Vector2(_hotbar.sizeDelta.x / 2f + 4f + 55f, 6f);
            // The bottom bands.
            _prompt.anchoredPosition = new Vector2(0, 56f);
            _prompt.sizeDelta = new Vector2(Mathf.Min(220f, w - 2 * edge), _prompt.sizeDelta.y);
            _narrator.anchoredPosition = new Vector2(0, 78f);
            _narrator.sizeDelta = new Vector2(Mathf.Min(360f, w - 2 * edge), _narrator.sizeDelta.y);
            _hotLabelBox.sizeDelta = new Vector2(Mathf.Min(150f, w - 2 * edge), _hotLabelBox.sizeDelta.y);
        }

        /// <summary>The lowest point of these elements' drawn content, measured down from the HUD's top (0 if none is showing).</summary>
        float BottomOf(params RectTransform[] items)
        {
            float lowest = 0f;
            var c = new Vector3[4];
            foreach (var t in items)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                foreach (var gr in t.GetComponentsInChildren<Graphic>(false))
                {
                    if (!gr.enabled || gr.color.a < 0.02f) continue;
                    gr.rectTransform.GetWorldCorners(c);
                    lowest = Mathf.Min(lowest, Root.InverseTransformPoint(c[0]).y - Root.rect.yMax);
                }
            }
            return lowest;
        }

        /// <summary>
        /// Where the combat ticker (the DM's line in a fight) goes: on a full screen, between the status column and the clock just
        /// under the round tracker; on a narrow half, full width, just under the status card, the party and the tracker.
        /// </summary>
        public (float y, float width) TickerSlot()
        {
            float w = Root.rect.width;
            float tb = TrackerBottom();
            if (w >= 600)
            {
                float side = 6f + _status.sizeDelta.x + 6f;
                return (float.IsNaN(tb) ? -26f : tb - 4f, Mathf.Min(330f, w - 2 * side));
            }
            var tracker = Root.Find("CombatHud/Tracker") as RectTransform;
            float below = BottomOf(_status, _party, tracker);
            return (below - 4f, w - 12f);
        }

        /// <summary>The bottom of the combat round tracker, measured down from the HUD's top (NaN when there's no fight).</summary>
        float TrackerBottom()
        {
            var tracker = Slot != null && Slot.InCombat ? Root.Find("CombatHud/Tracker") as RectTransform : null;
            if (tracker == null || !tracker.gameObject.activeInHierarchy) return float.NaN;
            var c = new Vector3[4];
            tracker.GetWorldCorners(c);
            return Root.InverseTransformPoint(c[0]).y - Root.rect.yMax;
        }

        void RefreshHud()
        {
            Layout();
            var pc = Slot.Character;
            var g = GameRoot.I;
            bool show = pc != null && g != null && g.InGame;
            _status.gameObject.SetActive(show);
            _party.gameObject.SetActive(show);
            _clock.gameObject.SetActive(show);
            _hotbar.gameObject.SetActive(show && !Slot.InCombat);
            // Half-width screens: the combat bar fills the bottom edge, so the log sits above it.
            _log.anchoredPosition = new Vector2(6, Root.rect.width >= 600 ? 4 : 44);
            if (Slot.InCombat && _narrator.gameObject.activeSelf) _narrator.gameObject.SetActive(false); // narration moves to the ticker in a fight
            if (!show) { _tint.color = new Color(0, 0, 0, 0); return; }

            string cond = "";
            if (pc.IsDead) cond = UIKit.Colorize(" DEAD", UIKit.Bad);
            else if (pc.IsDown) cond = UIKit.Colorize($" DOWN ({pc.DeathSuccesses}✓ {pc.DeathFailures}✗)", UIKit.Bad);
            if (pc.Exhaustion > 0) cond += UIKit.Colorize($" Exhaustion {pc.Exhaustion}", UIKit.Gold);
            if (pc.IsOverloaded) cond += UIKit.Colorize(" Overloaded", UIKit.Gold);
            if (pc.HeroicInspiration) cond += UIKit.Colorize(" ✦Insp", UIKit.D20Gold);
            if (pc.CanLevelUp) cond += UIKit.Colorize($" ★ Level up! ({(Slot.Input.UsesMouse ? "C" : "View")})", UIKit.D20Gold);
            var slotParts = new List<string>();
            if (pc.IsCaster)
                for (int l = 1; l <= 2; l++) if (pc.SlotsMax(l) > 0) slotParts.Add($"{pc.SlotsLeft(l)}/{pc.SlotsMax(l)}");
            string slots = slotParts.Count > 0 ? "   Slots " + string.Join(" · ", slotParts) : "";
            // The torch goes on the second line with conditions: the first line is full (AC, XP, slots).
            if (Slot.Avatar != null && Slot.Avatar.TorchMinutes > 0) cond = $" Torch {Mathf.CeilToInt(Slot.Avatar.TorchMinutes)}m" + cond;
            _statusText.text = (_statusTight ? $"<b>{pc.Name}</b>  <color=#c8b48c>Lv{pc.Level}</color>" : $"<b>{pc.Name}</b>  <color=#c8b48c>Lv{pc.Level} {pc.Class.Name}</color>")
                + (pc.WildShape != null ? $"  <color=#7ed957>❦ {pc.WildShape.Name}</color>" : "");
            _hpText.text = $"{pc.HP} / {pc.EffectiveMaxHP}{(pc.TempHP > 0 ? $"  <color=#9dff7a>+{pc.TempHP}</color>" : "")}";
            int next = pc.Level < PlayerCharacter.XpThresholds.Length - 1 ? PlayerCharacter.XpThresholds[pc.Level + 1] : 0;
            string xp = next > 0 ? $"   XP {pc.XP}/{next}" : "";
            _statsText.text = $"AC {pc.ArmorClass}{xp}{slots}" + (cond.Length > 0 ? "\n" + cond.TrimStart() : "");
            _status.sizeDelta = new Vector2(_status.sizeDelta.x, cond.Length > 0 ? 61 : 50);
            _hpFill.fillAmount = pc.EffectiveMaxHP > 0 ? Mathf.Clamp01(pc.HP / (float)pc.EffectiveMaxHP) : 0;

            RefreshParty(_status.sizeDelta.y);

            var c = g.Clock;
            _clockText.text = $"{c.Weekday}, Day {c.Day}\n<b>{c.TimeText()}</b>\n{Currency.Format(pc.Copper)}";

            for (int i = 0; i < Inventory.HotbarSize; i++)
            {
                var st = pc.Bag.Slots[i];
                _hotIcons[i].enabled = st != null;
                if (st != null) _hotIcons[i].sprite = ArtRegistry.Get(ItemsDb.Get(st.ItemId)?.Icon ?? "missing");
                _hotCounts[i].text = st != null && st.Count > 1 ? st.Count.ToString() : "";
                bool sel = i == HotbarIndex;
                _hotBgs[i].color = sel ? new Color(1f, 0.85f, 0.4f) : Color.white;
                _hotOutlines[i].enabled = sel;
                _hotSlots[i].localScale = Vector3.one * (sel ? 1.15f : 1f);
                if (sel) _hotSlots[i].SetAsLastSibling();
            }
            if (_hotLabelBox.gameObject.activeSelf && Time.unscaledTime > _hotLabelUntil) _hotLabelBox.gameObject.SetActive(false);
            _hotHint.text = Slot.Input.UsesMouse ? "Q/R or wheel: select\nX or click: use · 1–0: quick-use" : "LB/RB: select\nX: use";

            var zone = Slot.Zone;
            var tint = zone != null && !zone.Def.Indoors && !zone.Def.Dark ? g.Clock.AmbientTint() : new Color(0, 0, 0, 0);
            if (zone != null && zone.Def.MagicLit && tint.a > 0) tint = new Color(0.22f, 0.12f, 0.42f, tint.a * 0.55f); // the wards light the night violet
            if (zone != null && zone.Def.WarmLit && tint.a > 0) tint = new Color(0.32f, 0.17f, 0.05f, tint.a * 0.5f); // Aurelhold's lamps keep the night warm
            if (zone != null && zone.Def.Shadowed) tint = new Color(0.16f, 0.08f, 0.03f, Mathf.Max(0.38f, tint.a)); // under the mountain: lamplight and steam, day and night
            if (zone != null && zone.Def.Dusk) tint = DuskTint; // the Fae glade: always gold-and-violet dusk
            _tint.color = tint;
        }

        public static readonly Color DuskTint = new Color(0.42f, 0.2f, 0.4f, 0.26f);
        /// <summary>The ambient overlay's colour right now (tests read it).</summary>
        public Color AmbientTint => _tint != null ? _tint.color : Color.clear;

        /// <summary>Everyone fighting beside you: your companions, then other players. Name, HP bar, numbers.</summary>
        void RefreshParty(float statusH)
        {
            var g = GameRoot.I;
            var members = new List<(string name, Creature c)>();
            foreach (var comp in g.CompanionsOf(Slot)) if (comp.Character != null) members.Add((comp.Character.Name + (comp.Manual ? "" : " (auto)"), comp.Character));
            foreach (var p in g.Players) if (p != Slot && p.IsReady) members.Add((p.Character.Name, p.Character));
            if (g.Dragon != null) members.Add((g.Dragon.Name + (g.World.DragonScared ? " (scared)" : ""), g.Dragon));
            if (members.Count != _partyRows.Count)
            {
                UIKit.Clear(_party);
                _partyRows.Clear();
                for (int i = 0; i < members.Count; i++)
                {
                    var row = UIKit.Rect(_party, "Member" + i, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -(i + 1) * 15), new Vector2(0, -i * 15 - 1));
                    UIKit.Panel(row, true);
                    var name = UIKit.Label(UIKit.Rect(row, "Name", Vector2.zero, new Vector2(0.5f, 1), new Vector2(6, 0), Vector2.zero), "", 8, UIKit.Parchment, TextAnchor.MiddleLeft);
                    name.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var bar = UIKit.Rect(row, "Bar", new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0, 3), new Vector2(-5, -3));
                    UIKit.Image(bar, new Color(0.25f, 0.08f, 0.08f, 1));
                    var fill = UIKit.Image(UIKit.Fill(bar, "Fill"), UIKit.DamageRed, ArtRegistry.Get("ui_white"));
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    var hp = UIKit.Label(bar, "", 7, Color.white, TextAnchor.MiddleCenter);
                    hp.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                    _partyRows.Add((name, fill, hp));
                }
            }
            for (int i = 0; i < members.Count; i++)
            {
                var (name, c) = members[i];
                var row = _partyRows[i];
                row.name.text = name;
                row.fill.fillAmount = c.EffectiveMaxHP > 0 ? Mathf.Clamp01(c.HP / (float)c.EffectiveMaxHP) : 0;
                row.fill.color = c.IsDead ? UIKit.Dim : c.HP <= c.EffectiveMaxHP / 3 ? UIKit.Gold : UIKit.DamageRed;
                row.hp.text = c.IsDead ? "fallen" : c.IsDown ? "DOWN" : $"{c.HP}/{c.EffectiveMaxHP}";
            }
            // Half-width combat: the turn-order strip sits under the status panel, so the party goes below it.
            float y = -6 - statusH - 2;
            float tb = TrackerBottom();
            if (!float.IsNaN(tb) && tb - 4f < y && Root.rect.width < 600) y = tb - 4f; // under the round tracker in a narrow fight
            _party.anchoredPosition = new Vector2(6, y);
        }

        // ------------------------------------------------------------------ messages

        public void SetPrompt(string text)
        {
            bool on = !string.IsNullOrEmpty(text) && !HasScreen && !Slot.InCombat;
            _prompt.gameObject.SetActive(on);
            if (on) _promptText.text = text;
        }

        /// <summary>The DM speaks. Stays up a few seconds (longer for longer text).</summary>
        /// <summary>
        /// Where every visible HUD element sits, in the HUD's own space (tests: nothing overlaps, nothing runs off the screen).
        /// The persistent panels, plus the combat HUD's tracker and action bar while a fight is on. Bounds come from what's
        /// actually drawn (visible images and non-empty text), so an empty container takes no room.
        /// </summary>
        public List<(string name, Rect rect)> LayoutBounds()
        {
            var list = new List<(string, Rect)>();
            void Add(string name, RectTransform t)
            {
                if (t == null || !t.gameObject.activeInHierarchy) return;
                bool any = false; Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                var corners = new Vector3[4];
                foreach (var gr in t.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
                {
                    if (!gr.enabled || gr.color.a < 0.02f) continue;
                    if (gr is Text tx && string.IsNullOrWhiteSpace(tx.text)) continue;
                    gr.rectTransform.GetWorldCorners(corners);
                    foreach (var c in corners) { var l = (Vector2)Root.InverseTransformPoint(c); min = Vector2.Min(min, l); max = Vector2.Max(max, l); }
                    any = true;
                }
                if (any) list.Add((name, Rect.MinMaxRect(min.x, min.y, max.x, max.y)));
            }
            foreach (var n in new[] { "Status", "Party", "Clock", "Hotbar", "HotLabel", "HotHint", "Prompt", "Narrator", "Toasts", "Log", "RollTicker" })
                Add(n, Root.Find(n) as RectTransform);
            var combat = Root.Find("CombatHud") as RectTransform;
            if (combat != null && combat.gameObject.activeInHierarchy)
                foreach (var n in new[] { "Tracker", "ActionBar" }) Add(n, combat.Find(n) as RectTransform);
            return list;
        }

        /// <summary>HUD text that is bigger than its box (tests): one that would spill sideways or downwards.</summary>
        public List<string> OverflowingTexts()
        {
            var list = new List<string>();
            foreach (var n in new[] { "Status", "Party", "Clock", "Hotbar", "HotLabel", "HotHint", "Prompt", "Narrator", "Toasts", "Log" })
            {
                var t = Root.Find(n);
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                foreach (var tx in t.GetComponentsInChildren<Text>(false))
                {
                    if (string.IsNullOrWhiteSpace(tx.text) || tx.resizeTextForBestFit) continue;
                    var r = tx.rectTransform.rect;
                    if (tx.horizontalOverflow == HorizontalWrapMode.Overflow && tx.preferredWidth > r.width + 2) list.Add($"{n}/{tx.name}: \"{tx.text}\" is {tx.preferredWidth:0} wide in {r.width:0}");
                    if (tx.verticalOverflow == VerticalWrapMode.Overflow && tx.preferredHeight > r.height + 2) list.Add($"{n}/{tx.name}: \"{tx.text}\" is {tx.preferredHeight:0} tall in {r.height:0}");
                }
            }
            return list;
        }

        /// <summary>The HUD's own rectangle (its half of the screen in split-screen).</summary>
        public Rect HudRect => Root.rect;

        /// <summary>Whether the narrator's box is on screen (tests).</summary>
        public bool NarratorVisible => _narrator != null && _narrator.gameObject.activeSelf;

        public void Narrate(string text, bool keyMoment = true)
        {
            var g = GameRoot.I;
            if (!keyMoment && g != null && g.Settings.NarratorKeyMomentsOnly) return;
            AddLog("<i>" + text + "</i>");
            // In combat the DM's line goes in the small roll ticker up top instead of a box over the fight.
            if (Slot.InCombat) { Dice.Ticker("<i>" + text + "</i>", 2.5f); return; }
            _narrator.gameObject.SetActive(true);
            _narrator.SetAsLastSibling();
            _narratorText.text = text;
            _narratorTimer = 4f + text.Length * 0.045f;
        }

        /// <summary>Hides the narrator's box now (a cutscene wants the screen).</summary>
        public void HideNarration() { _narratorTimer = 0; _narrator.gameObject.SetActive(false); }

        /// <summary>The toasts on screen right now, newest first (as displayed, including "×N").</summary>
        public List<string> ToastTexts => _toastItems.Select(x => x.t.text).ToList();
        readonly Dictionary<Text, (string text, int n)> _toastKey = new Dictionary<Text, (string, int)>();

        public void Toast(string text, Color? color = null)
        {
            // The same message again while it's still showing: count it up instead of stacking copies.
            for (int i = 0; i < _toastItems.Count; i++)
            {
                var old = _toastItems[i].t;
                if (old == null || !_toastKey.TryGetValue(old, out var k) || k.text != text) continue;
                _toastKey[old] = (text, k.n + 1);
                old.text = $"{text} ×{k.n + 1}";
                _toastItems[i] = (old, Time.unscaledTime + 4f);
                AddLog(text);
                return;
            }
            var box = UIKit.Box(_toasts, "Toast", new Vector2(1, 1), new Vector2(_toastW, 18), Vector2.zero);
            UIKit.Panel(box, true);
            var t = UIKit.Label(box, text, 9, color ?? UIKit.Parchment, TextAnchor.MiddleCenter);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 6; t.resizeTextMaxSize = 9;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.rectTransform.offsetMin = new Vector2(4, 1); t.rectTransform.offsetMax = new Vector2(-4, -1);
            _toastItems.Insert(0, (t, Time.unscaledTime + 4f));
            _toastKey[t] = (text, 1);
            AddLog(text);
        }

        /// <summary>The last few log lines (narration, toasts), oldest first.</summary>
        public IReadOnlyList<string> LogLines => _logLines;

        public void AddLog(string line)
        {
            _logLines.Add(line);
            while (_logLines.Count > 9) _logLines.RemoveAt(0);
            FitLog();
        }

        /// <summary>The log box shows the newest lines that fit in it (older ones would otherwise spill up over the HUD).</summary>
        void FitLog()
        {
            if (_logText == null) return;
            float room = _logText.rectTransform.rect.height;
            var shown = new List<string>();
            for (int i = _logLines.Count - 1; i >= 0; i--)
            {
                shown.Insert(0, _logLines[i]);
                _logText.text = string.Join("\n", shown);
                if (room > 0 && _logText.preferredHeight > room - 4 && shown.Count > 1) { shown.RemoveAt(0); break; }
            }
            _logText.text = string.Join("\n", shown);
        }

        public void ShowLog(bool on) => _log.gameObject.SetActive(on);

        public void Flash(Color c)
        {
            c.a = 0.35f;
            _flash.color = c;
            _flashTimer = 0.6f;
        }

        public void HotbarScroll(int dir) => HotbarIndex = (HotbarIndex + dir + Inventory.HotbarSize) % Inventory.HotbarSize;
        /// <summary>The selected item's name, shown above the hotbar for 2 s after the selection changes ("" once it fades).</summary>
        public string HotbarLabel => _hotLabelBox != null && _hotLabelBox.gameObject.activeSelf ? _hotLabel.text : "";
        public Vector2 HotbarSlotScreenPos(int i) => RectTransformUtility.WorldToScreenPoint(Slot.Cam, _hotSlots[i].TransformPoint(_hotSlots[i].rect.center));
        /// <summary>The hotbar slot under a screen point, or -1.</summary>
        public int HotbarSlotAt(Vector2 screen)
        {
            for (int i = 0; i < _hotSlots.Count; i++) if (UIKit.Contains(_hotSlots[i], screen, Slot.Cam)) return i;
            return -1;
        }
        public float HotbarSlotScale(int i) => _hotSlots[i].localScale.x;
    }
}
