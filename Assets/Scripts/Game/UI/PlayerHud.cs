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
        public void Close() { Closed = true; }

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
        public int HotbarIndex;
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
                _hotBgs.Add(UIKit.Image(s, Color.white, ArtRegistry.Get("ui_slot")));
                var icon = UIKit.Fill(s, "Icon", 2);
                var img = UIKit.Image(icon, Color.white);
                img.enabled = false;
                _hotIcons.Add(img);
                var cnt = UIKit.Rect(s, "Count", Vector2.zero, Vector2.one, new Vector2(0, -2), new Vector2(0, 0));
                _hotCounts.Add(UIKit.Label(cnt, "", 8, UIKit.Ink, TextAnchor.LowerRight));
            }

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

        void RefreshHud()
        {
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
            _statusText.text = $"<b>{pc.Name}</b>  <color=#c8b48c>Lv{pc.Level} {pc.Class.Name}</color>"
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
                _hotBgs[i].color = i == HotbarIndex ? new Color(1f, 0.85f, 0.4f) : Color.white;
            }

            var zone = Slot.Zone;
            _tint.color = zone != null && !zone.Def.Indoors && !zone.Def.Dark ? g.Clock.AmbientTint() : new Color(0, 0, 0, 0);
        }

        /// <summary>Everyone fighting beside you: your companions, then other players. Name, HP bar, numbers.</summary>
        void RefreshParty(float statusH)
        {
            var g = GameRoot.I;
            var members = new List<(string name, Creature c)>();
            foreach (var comp in g.CompanionsOf(Slot)) if (comp.Character != null) members.Add((comp.Character.Name + (comp.Manual ? "" : " (auto)"), comp.Character));
            foreach (var p in g.Players) if (p != Slot && p.IsReady) members.Add((p.Character.Name, p.Character));
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
            float y = -6 - statusH - 2 - (Slot.InCombat && Root.rect.width < 600 ? 20 : 0);
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
            var box = UIKit.Box(_toasts, "Toast", new Vector2(1, 1), new Vector2(170, 18), Vector2.zero);
            UIKit.Panel(box, true);
            var t = UIKit.Label(box, text, 9, color ?? UIKit.Parchment, TextAnchor.MiddleCenter);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 6; t.resizeTextMaxSize = 9;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.rectTransform.offsetMin = new Vector2(4, 1); t.rectTransform.offsetMax = new Vector2(-4, -1);
            _toastItems.Insert(0, (t, Time.unscaledTime + 4f));
            _toastKey[t] = (text, 1);
            AddLog(text);
        }

        public void AddLog(string line)
        {
            _logLines.Add(line);
            while (_logLines.Count > 9) _logLines.RemoveAt(0);
            _logText.text = string.Join("\n", _logLines);
        }

        public void ShowLog(bool on) => _log.gameObject.SetActive(on);

        public void Flash(Color c)
        {
            c.a = 0.35f;
            _flash.color = c;
            _flashTimer = 0.6f;
        }

        public void HotbarScroll(int dir) => HotbarIndex = (HotbarIndex + dir + Inventory.HotbarSize) % Inventory.HotbarSize;
    }
}
