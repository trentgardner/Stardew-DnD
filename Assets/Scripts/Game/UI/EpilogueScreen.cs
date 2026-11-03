using UnityEngine;

namespace DnD.Game
{
    /// <summary>One slide of the epilogue: full screen, dark, a title and a few lines. Any press (or Esc/B) goes on to the next.</summary>
    public class EpilogueScreen : GameScreen
    {
        public readonly EpilogueSlide Slide;
        readonly int _index, _count;
        public EpilogueScreen(EpilogueSlide slide, int index, int count) { Slide = slide; _index = index; _count = count; }
        public override bool ShowCloseButton => false;

        public override void Build()
        {
            Root = UIKit.Rect(Hud.Root, "EpilogueScreen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIKit.Image(Root, new Color32(14, 12, 18, 255));
            var title = UIKit.Rect(Root, "Title", new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);
            var t = UIKit.Label(title, Slide.Title, 20, UIKit.Gold, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            var body = UIKit.Rect(Root, "Body", new Vector2(0.14f, 0.24f), new Vector2(0.86f, 0.6f), Vector2.zero, Vector2.zero);
            var b = UIKit.Label(body, Slide.Text, 13, UIKit.Parchment, TextAnchor.UpperCenter);
            b.resizeTextForBestFit = true; b.resizeTextMinSize = 8; b.resizeTextMaxSize = 13;
            var foot = UIKit.Rect(Root, "Foot", new Vector2(0, 0.06f), new Vector2(1, 0.12f), Vector2.zero, Vector2.zero);
            bool last = _index >= _count - 1;
            UIKit.Label(foot, (In.UsesMouse ? "[E] / click: " : "(A): ") + (last ? "the end" : "next"), 9, UIKit.Dim, TextAnchor.MiddleCenter);
        }

        public override void Tick()
        {
            if (In.Confirm || In.Cancel || In.MouseLeft) Close();
        }
    }

    /// <summary>
    /// The credits: they scroll up slowly, then the dedication is shown on its own, centred, and held until a press.
    /// A press during the scroll jumps to the dedication; a press on the dedication (after a moment) ends the game.
    /// </summary>
    public class CreditsScreen : GameScreen
    {
        public string Text => GameRoot.CreditsText;
        public bool ShowingDedication { get; private set; }
        public override bool ShowCloseButton => false;
        RectTransform _scroll, _dedication;
        float _opened, _dedicatedAt;
        const float Speed = 40f;

        public override void Build()
        {
            Root = UIKit.Rect(Hud.Root, "CreditsScreen", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIKit.Image(Root, new Color32(8, 7, 12, 255));
            string body = Text.Substring(0, Text.Length - GameRoot.Dedication.Length).TrimEnd();
            _scroll = UIKit.Rect(Root, "Scroll", new Vector2(0.15f, 0), new Vector2(0.85f, 0), new Vector2(0, -620), new Vector2(0, 0));
            var lbl = UIKit.Label(_scroll, body, 12, UIKit.Parchment, TextAnchor.UpperCenter);
            lbl.supportRichText = true;
            _dedication = UIKit.Rect(Root, "Dedication", new Vector2(0.12f, 0.3f), new Vector2(0.88f, 0.7f), Vector2.zero, Vector2.zero);
            var d = UIKit.Label(_dedication, GameRoot.Dedication, 16, UIKit.Gold, TextAnchor.MiddleCenter);
            d.fontStyle = FontStyle.Italic;
            _dedication.gameObject.SetActive(false);
            _opened = Time.unscaledTime;
        }

        /// <summary>Jumps to the dedication (a press during the scroll does the same).</summary>
        public void SkipToDedication() => ShowDedication();

        void ShowDedication()
        {
            if (ShowingDedication) return;
            ShowingDedication = true;
            _dedicatedAt = Time.unscaledTime;
            _scroll.gameObject.SetActive(false);
            _dedication.gameObject.SetActive(true);
        }

        public override void Tick()
        {
            if (!ShowingDedication)
            {
                _scroll.anchoredPosition += new Vector2(0, Speed * Time.unscaledDeltaTime);
                float top = Root.rect.height + _scroll.rect.height;
                if (_scroll.anchoredPosition.y > top) ShowDedication();
            }
            bool press = In.Confirm || In.Cancel || In.MouseLeft;
            if (!press || Time.unscaledTime - _opened < 2f) return;
            if (!ShowingDedication) ShowDedication();
            else if (Time.unscaledTime - _dedicatedAt > 1.5f) Close();
        }
    }
}
