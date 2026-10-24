using System;
using System.Collections;
using System.Collections.Generic;
using DnD.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// The dice tray. The rules engine decides results; this shows them as real dice the player rolls.
    /// Big modal tray: skill checks, saves, damage, healing, ability scores ("press to roll").
    /// Small HUD ticker: attack rolls and enemy rolls during combat (no press needed).
    /// </summary>
    public class DiceRoller
    {
        readonly PlayerHud _hud;
        RectTransform _tray, _diceRow, _ticker;
        Text _title, _sub, _result, _prompt;
        Text _tickerText;
        float _tickerTimer;

        public bool IsOpen { get; private set; }
        bool _waitingPress;
        bool _pressed;
        List<string> _choices;
        int _choice = -1;

        public DiceRoller(PlayerHud hud) { _hud = hud; Build(); }

        void Build()
        {
            _tray = UIKit.Box(_hud.Root, "DiceTray", new Vector2(0.5f, 0.5f), new Vector2(300, 170), Vector2.zero);
            UIKit.Panel(_tray, true);
            var titleRt = UIKit.Rect(_tray, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -28), new Vector2(-10, -6));
            _title = UIKit.Label(titleRt, "", 15, UIKit.Gold, TextAnchor.MiddleCenter);
            var subRt = UIKit.Rect(_tray, "Sub", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -46), new Vector2(-10, -28));
            _sub = UIKit.Label(subRt, "", 11, UIKit.Parchment, TextAnchor.MiddleCenter);
            _diceRow = UIKit.Rect(_tray, "Dice", new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(10, -28), new Vector2(-10, 28));
            var resRt = UIKit.Rect(_tray, "Result", new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 22), new Vector2(-10, 56));
            _result = UIKit.Label(resRt, "", 12, UIKit.Parchment, TextAnchor.MiddleCenter);
            var prRt = UIKit.Rect(_tray, "Prompt", new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 4), new Vector2(-10, 22));
            _prompt = UIKit.Label(prRt, "", 11, UIKit.Gold, TextAnchor.MiddleCenter);
            _tray.gameObject.SetActive(false);

            _ticker = UIKit.Box(_hud.Root, "RollTicker", new Vector2(0.5f, 1f), new Vector2(330, 44), new Vector2(0, -34));
            UIKit.Panel(_ticker, true);
            _tickerText = UIKit.Label(_ticker, "", 11, UIKit.Parchment, TextAnchor.MiddleCenter);
            _ticker.gameObject.SetActive(false);
        }

        public static Color ColorFor(DieColor c)
        {
            switch (c)
            {
                case DieColor.D20: return UIKit.D20Gold;
                case DieColor.Damage: return UIKit.DamageRed;
                case DieColor.Healing: return UIKit.HealGreen;
                default: return UIKit.UtilityBlue;
            }
        }

        string PressText => _hud.Slot.Input.UsesMouse ? "[E] or click" : "(A)";

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// A d20 Test the player rolls by pressing a button. `reroll` produces a fresh roll for
        /// Heroic Inspiration / Lucky Copper. `dc` (hidden) is revealed afterwards if given.
        /// </summary>
        public void RollD20(string title, string subtitle, Func<D20Result> roll, int? dc, bool isAbilityCheck, Action<D20Result> done)
        {
            _hud.StartCoroutine(D20Flow(title, subtitle, roll, dc, isAbilityCheck, done));
        }

        /// <summary>Damage or healing the player rolls by pressing a button.</summary>
        public void RollDamage(string title, string subtitle, DamageRoll roll, DieColor color, Action done)
        {
            _hud.StartCoroutine(DamageFlow(title, subtitle, roll, color, done));
        }

        /// <summary>Generic "roll these dice" for ability scores etc.</summary>
        public void RollDice(string title, List<DieRoll> dice, DieColor color, string resultText, bool requirePress, Action done)
        {
            _hud.StartCoroutine(GenericFlow(title, dice, color, resultText, requirePress, done));
        }

        /// <summary>Small, non-blocking combat readout ("d20: 14 +5 = 19 vs AC 15, HIT").</summary>
        public void Ticker(string text, float seconds = 3.2f)
        {
            _ticker.gameObject.SetActive(true);
            _tickerText.text = text;
            _tickerTimer = seconds;
        }

        public void Update()
        {
            if (_tickerTimer > 0)
            {
                _tickerTimer -= Time.unscaledDeltaTime;
                if (_tickerTimer <= 0) _ticker.gameObject.SetActive(false);
            }
            if (!IsOpen) return;
            var input = _hud.Slot.Input;
            if (_waitingPress && (input.Confirm || input.MouseLeft || input.EndTurn)) _pressed = true;
            if (_choices != null)
            {
                // choice hotkeys: E/A = first, R/X = second, Esc/B = last
                if (input.Confirm || input.MouseLeft) _choice = 0;
                else if ((input.NextTab || input.AltAction) && _choices.Count > 2) _choice = 1;
                else if (input.Cancel) _choice = _choices.Count - 1;
            }
        }

        // ------------------------------------------------------------------ flows

        IEnumerator D20Flow(string title, string subtitle, Func<D20Result> roll, int? dc, bool isCheck, Action<D20Result> done)
        {
            Open(title, subtitle);
            var pc = _hud.Slot.Character;
            var dieViews = new List<DieView>();
            dieViews.Add(DieView.Create(_diceRow, 20, UIKit.D20Gold, 0));
            _prompt.text = $"Press {PressText} to roll!";
            _result.text = "";
            yield return WaitPress();

            var r = roll();
            yield return Tumble(dieViews, r);
            ShowD20Result(r, dc, isCheck);

            // Failure → offer rerolls / bonus dice the player is holding.
            while (dc.HasValue && !Succeeded(r, dc.Value, isCheck))
            {
                var opts = new List<string>();
                var actions = new List<Func<D20Result>>();
                if (pc.HeroicInspiration) { opts.Add("Reroll (Heroic Inspiration)"); actions.Add(() => { pc.HeroicInspiration = false; return roll(); }); }
                if (pc.HasLuckyReroll) { opts.Add("Reroll (Fennick's Lucky Copper)"); actions.Add(() => { pc.LuckyRerollUsed = true; return roll(); }); }
                if (pc.HeldInspirationDie > 0)
                {
                    int die = pc.HeldInspirationDie;
                    opts.Add($"Add Bardic Inspiration (d{die})");
                    var current = r;
                    actions.Add(() => { pc.HeldInspirationDie = 0; Dice.AddBonusDie(current, die); return current; });
                }
                if (opts.Count == 0) break;
                opts.Add("Accept the result");
                actions.Add(null);
                int pick = -1;
                yield return Choose(opts, i => pick = i);
                if (actions[pick] == null) break;
                var next = actions[pick]();
                if (next != r)
                {
                    r = next;
                    UIKit.Clear(_diceRow);
                    dieViews.Clear();
                    dieViews.Add(DieView.Create(_diceRow, 20, UIKit.D20Gold, 0));
                    yield return Tumble(dieViews, r);
                }
                ShowD20Result(r, dc, isCheck);
            }

            _prompt.text = $"{PressText} to continue";
            yield return WaitPress();
            Close();
            done?.Invoke(r);
        }

        static bool Succeeded(D20Result r, int dc, bool isCheck) =>
            isCheck ? Checks.IsSuccess(Checks.ResolveCheck(r, dc)) : r.Total >= dc;

        void ShowD20Result(D20Result r, int? dc, bool isCheck)
        {
            string line = r.Breakdown();
            if (dc.HasValue)
            {
                bool ok = Succeeded(r, dc.Value, isCheck);
                string margin = isCheck ? Checks.MarginText(r, dc.Value) : (ok ? $"Success (DC {dc})" : $"Failure (DC {dc})");
                line += "\n" + UIKit.Colorize(margin, ok ? UIKit.Good : UIKit.Bad);
            }
            _result.text = line;
            if (r.IsNat20) _hud.Flash(UIKit.D20Gold);
            else if (r.IsNat1) _hud.Flash(UIKit.Bad);
        }

        IEnumerator DamageFlow(string title, string subtitle, DamageRoll roll, DieColor color, Action done)
        {
            Open(title, subtitle);
            var views = new List<DieView>();
            var all = new List<DieRoll>(roll.Dice);
            if (roll.Extra != null) all.AddRange(roll.Extra.Dice);
            for (int i = 0; i < all.Count; i++)
            {
                bool extra = i >= roll.Dice.Count;
                views.Add(DieView.Create(_diceRow, all[i].Sides, extra ? (Color)new Color32(247, 168, 106, 255) : ColorFor(color), i));
            }
            Layout(views);
            _prompt.text = $"Press {PressText} to roll {(color == DieColor.Healing ? "healing" : "damage")}!";
            _result.text = "";
            yield return WaitPress();
            yield return TumbleMany(views, all);
            _result.text = roll.Breakdown();
            if (roll.Critical) _hud.Flash(UIKit.DamageRed);
            _prompt.text = $"{PressText} to continue";
            yield return WaitPress();
            Close();
            done?.Invoke();
        }

        IEnumerator GenericFlow(string title, List<DieRoll> dice, DieColor color, string resultText, bool requirePress, Action done)
        {
            Open(title, "");
            var views = new List<DieView>();
            for (int i = 0; i < dice.Count; i++) views.Add(DieView.Create(_diceRow, dice[i].Sides, ColorFor(color), i));
            Layout(views);
            if (requirePress) { _prompt.text = $"Press {PressText} to roll!"; yield return WaitPress(); }
            yield return TumbleMany(views, dice);
            _result.text = resultText;
            _prompt.text = requirePress ? $"{PressText} to continue" : "";
            if (requirePress) yield return WaitPress();
            else yield return new WaitForSecondsRealtime(0.5f);
            Close();
            done?.Invoke();
        }

        // ------------------------------------------------------------------ helpers

        void Open(string title, string subtitle)
        {
            IsOpen = true;
            _tray.gameObject.SetActive(true);
            _tray.SetAsLastSibling();
            _title.text = title;
            _sub.text = subtitle;
            _result.text = "";
            _prompt.text = "";
            UIKit.Clear(_diceRow);
            Sfx.Play("dice_shake");
        }

        void Close()
        {
            IsOpen = false;
            _choices = null;
            _tray.gameObject.SetActive(false);
        }

        IEnumerator WaitPress()
        {
            yield return null; // don't consume the press that opened us
            _pressed = false;
            _waitingPress = true;
            while (!_pressed) yield return null;
            _waitingPress = false;
            _pressed = false;
        }

        IEnumerator Choose(List<string> options, Action<int> picked)
        {
            _choices = options;
            _choice = -1;
            var keys = _hud.Slot.Input.UsesMouse ? new[] { "[E]", "[R]", "[Esc]" } : new[] { "(A)", "(X)", "(B)" };
            var parts = new List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                string key = i == options.Count - 1 ? keys[2] : (i == 0 ? keys[0] : keys[1]);
                parts.Add($"{key} {options[i]}");
            }
            _prompt.text = string.Join("   ", parts);
            yield return null;
            while (_choice < 0) yield return null;
            int c = _choice;
            if (c >= options.Count - 1) c = options.Count - 1;
            _choices = null;
            picked(c);
        }

        static void Layout(List<DieView> views)
        {
            float spacing = Mathf.Min(44, 270f / Mathf.Max(1, views.Count));
            float start = -(views.Count - 1) * spacing / 2f;
            for (int i = 0; i < views.Count; i++) views[i].Rt.anchoredPosition = new Vector2(start + i * spacing, 0);
        }

        IEnumerator Tumble(List<DieView> views, D20Result r)
        {
            // Advantage/disadvantage shows both d20s.
            if (r.Dice.Count > 1 && views.Count == 1)
                views.Add(DieView.Create(_diceRow, 20, UIKit.D20Gold, 1));
            var bonus = r.BonusDice;
            foreach (var b in bonus) views.Add(DieView.Create(_diceRow, Mathf.Abs(b.Sides), UIKit.UtilityBlue, views.Count));
            Layout(views);
            var all = new List<DieRoll>(r.Dice);
            all.AddRange(bonus);
            yield return TumbleMany(views, all);
        }

        IEnumerator TumbleMany(List<DieView> views, List<DieRoll> results)
        {
            Sfx.Play("dice_roll");
            float t = 0, dur = 0.85f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < views.Count; i++)
                {
                    var v = views[i];
                    v.Rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((t + i) * 30f) * 25f * (1 - t / dur));
                    v.Rt.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(t * 18f + i)) * 0.15f * (1 - t / dur));
                    v.SetValue(UnityEngine.Random.Range(1, v.Sides + 1));
                }
                yield return null;
            }
            for (int i = 0; i < views.Count && i < results.Count; i++)
            {
                views[i].Rt.localRotation = Quaternion.identity;
                views[i].Rt.localScale = Vector3.one;
                views[i].SetValue(results[i].Value);
                views[i].SetDiscarded(results[i].Discarded);
                if (results[i].Sides == 20 && !results[i].Discarded)
                {
                    if (results[i].Value == 20) views[i].Highlight(UIKit.D20Gold);
                    if (results[i].Value == 1) views[i].Highlight(UIKit.Bad);
                }
            }
            Sfx.Play("dice_land");
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    /// <summary>A single on-screen die.</summary>
    public class DieView
    {
        public RectTransform Rt;
        public int Sides;
        Text _value, _label;
        Image _img;

        public static DieView Create(RectTransform parent, int sides, Color color, int index)
        {
            var v = new DieView { Sides = sides };
            v.Rt = UIKit.Box(parent, "d" + sides, new Vector2(0.5f, 0.5f), new Vector2(40, 40), new Vector2(index * 44, 0));
            v._img = UIKit.Image(v.Rt, color, ArtRegistry.Get("ui_die"));
            v._value = UIKit.Label(v.Rt, "?", 16, UIKit.Ink, TextAnchor.MiddleCenter);
            v._value.fontStyle = FontStyle.Bold;
            var lab = UIKit.Rect(v.Rt, "Kind", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -14), new Vector2(0, 0));
            v._label = UIKit.Label(lab, "d" + sides, 9, UIKit.Parchment, TextAnchor.MiddleCenter);
            return v;
        }

        public void SetValue(int v) => _value.text = v.ToString();
        public void SetDiscarded(bool d) { if (d) { _img.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); _label.text = "dropped"; } }
        public void Highlight(Color c) { _value.color = c == UIKit.Bad ? Color.white : UIKit.Ink; _img.color = c; _label.text = c == UIKit.Bad ? "NAT 1" : "NAT 20!"; }
    }
}
