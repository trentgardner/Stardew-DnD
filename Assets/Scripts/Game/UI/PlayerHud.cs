using System.Collections.Generic;
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
        protected PlayerSlot Slot => Hud.Slot;
        protected PlayerInput In => Hud.Slot.Input;
        protected Camera Cam => Hud.Slot.Cam;

        public abstract void Build();
        public abstract void Tick();
        public virtual void OnClose() { }
        public void Close() { Closed = true; }

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
        public GameScreen Top => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        Image _tint, _flash;
        RectTransform _status, _clock, _hotbar, _prompt, _narrator, _toasts, _log;
        Text _statusText, _clockText, _promptText, _narratorText, _logText;
        Image _hpFill;
        readonly List<Image> _hotIcons = new List<Image>();
        readonly List<Text> _hotCounts = new List<Text>();
        readonly List<Image> _hotBgs = new List<Image>();
        public int HotbarIndex;
        float _narratorTimer, _flashTimer;
        readonly List<string> _logLines = new List<string>();
        readonly List<(Text t, float time)> _toastItems = new List<(Text, float)>();

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
            hud.Canvas.sortingOrder = 10;
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

            // Status (top-left)
            _status = UIKit.Box(Root, "Status", new Vector2(0, 1), new Vector2(170, 58), new Vector2(6, -6));
            UIKit.Panel(_status, true);
            var stRt = UIKit.Rect(_status, "Text", Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4));
            _statusText = UIKit.Label(stRt, "", 10, UIKit.Parchment);
            var hpBack = UIKit.Rect(_status, "HPBack", new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -30), new Vector2(-8, -24));
            UIKit.Image(hpBack, new Color(0.25f, 0.08f, 0.08f, 1));
            var hpFillRt = UIKit.Fill(hpBack, "Fill");
            _hpFill = UIKit.Image(hpFillRt, UIKit.DamageRed, ArtRegistry.Get("ui_white"));
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;

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

            // Combat / event log (bottom-left)
            _log = UIKit.Box(Root, "Log", new Vector2(0, 0), new Vector2(200, 90), new Vector2(6, 40));
            UIKit.Panel(_log, true);
            _logText = UIKit.Label(UIKit.Rect(_log, "Text", Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4)), "", 8, UIKit.Parchment, TextAnchor.LowerLeft);
            _log.gameObject.SetActive(false);

            _flash = UIKit.Image(UIKit.Fill(Root, "Flash"), new Color(1, 1, 1, 0));
        }

        // ------------------------------------------------------------------ screens

        public void Push(GameScreen s)
        {
            s.Hud = this;
            s.Build();
            _screens.Add(s);
            Sfx.Play("ui");
        }

        /// <summary>True while CloseAll runs: screens skip follow-up callbacks (no new popups mid-teardown).</summary>
        public bool ClosingAll { get; private set; }

        public void CloseAll()
        {
            var copy = new List<GameScreen>(_screens);
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
            Dice.Update();
            if (!Dice.IsOpen && _screens.Count > 0)
            {
                var top = Top;
                top.Tick();
                for (int i = _screens.Count - 1; i >= 0; i--)
                {
                    if (!_screens[i].Closed) continue;
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
                if (left <= 0) { Destroy(t.transform.parent.gameObject); _toastItems.RemoveAt(i); continue; }
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
            _clock.gameObject.SetActive(show);
            _hotbar.gameObject.SetActive(show && !Slot.InCombat);
            if (!show) { _tint.color = new Color(0, 0, 0, 0); return; }

            string cond = "";
            if (pc.IsDead) cond = UIKit.Colorize(" DEAD", UIKit.Bad);
            else if (pc.IsDown) cond = UIKit.Colorize($" DOWN ({pc.DeathSuccesses}✓ {pc.DeathFailures}✗)", UIKit.Bad);
            if (pc.Exhaustion > 0) cond += UIKit.Colorize($" Exhaustion {pc.Exhaustion}", UIKit.Gold);
            if (pc.IsOverloaded) cond += UIKit.Colorize(" Overloaded", UIKit.Gold);
            if (pc.HeroicInspiration) cond += UIKit.Colorize(" ✦Insp", UIKit.D20Gold);
            string slots = "";
            if (pc.IsCaster)
            {
                for (int l = 1; l <= 2; l++) if (pc.SlotsMax(l) > 0) slots += $"  L{l}: {pc.SlotsLeft(l)}/{pc.SlotsMax(l)}";
            }
            string light = Slot.Avatar != null && Slot.Avatar.TorchMinutes > 0 ? $"  Torch {Mathf.CeilToInt(Slot.Avatar.TorchMinutes)}m" : "";
            _statusText.text = $"<b>{pc.Name}</b>  Lv{pc.Level} {pc.Class.Name}\n\n" +
                               $"HP {pc.HP}/{pc.EffectiveMaxHP}{(pc.TempHP > 0 ? $" (+{pc.TempHP})" : "")}  AC {pc.ArmorClass}{slots}\n{cond}{light}";
            _hpFill.fillAmount = pc.EffectiveMaxHP > 0 ? Mathf.Clamp01(pc.HP / (float)pc.EffectiveMaxHP) : 0;

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
            _narrator.gameObject.SetActive(true);
            _narrator.SetAsLastSibling();
            _narratorText.text = text;
            _narratorTimer = 4f + text.Length * 0.045f;
            AddLog("<i>" + text + "</i>");
        }

        public void Toast(string text, Color? color = null)
        {
            var box = UIKit.Box(_toasts, "Toast", new Vector2(1, 1), new Vector2(170, 18), Vector2.zero);
            UIKit.Panel(box, true);
            var t = UIKit.Label(box, text, 9, color ?? UIKit.Parchment, TextAnchor.MiddleCenter);
            _toastItems.Insert(0, (t, Time.unscaledTime + 4f));
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
