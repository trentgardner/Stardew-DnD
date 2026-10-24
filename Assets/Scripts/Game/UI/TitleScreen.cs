using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>Title menu: New Game (pick difficulty), Continue, Load (all playthroughs), Quit.</summary>
    public class TitleScreen : MonoBehaviour
    {
        GameRoot _g;
        MenuList _menu;
        Text _info;
        RectTransform _root;
        readonly HybridInput _input = new HybridInput();

        public static TitleScreen Create(GameRoot g)
        {
            var go = new GameObject("TitleScreen");
            var t = go.AddComponent<TitleScreen>();
            t._g = g;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 1f;
            t._root = UIKit.Fill(go.transform, "Root");
            UIKit.Image(UIKit.Fill(t._root, "Bg"), new Color32(30, 22, 26, 255));

            var title = UIKit.Box(t._root, "Title", new Vector2(0.5f, 1), new Vector2(560, 70), new Vector2(0, -30));
            var tl = UIKit.Label(title, "EMBERS OF SUNDERMERE", 34, UIKit.Gold, TextAnchor.MiddleCenter);
            tl.fontStyle = FontStyle.Bold;
            var sub = UIKit.Box(t._root, "Sub", new Vector2(0.5f, 1), new Vector2(560, 20), new Vector2(0, -98));
            UIKit.Label(sub, "a cozy high-fantasy dungeon crawler · playtest build", 11, UIKit.Parchment, TextAnchor.MiddleCenter).fontStyle = FontStyle.Italic;

            var panel = UIKit.Box(t._root, "Menu", new Vector2(0.5f, 0.5f), new Vector2(380, 170), new Vector2(0, -30));
            UIKit.Panel(panel);
            var list = UIKit.Rect(panel, "List", Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
            t._menu = new MenuList(list) { RowHeight = 17, FontSize = 12, VisibleRows = 9, Wrap = false };
            var info = UIKit.Box(t._root, "Info", new Vector2(0.5f, 0), new Vector2(560, 50), new Vector2(0, 10));
            t._info = UIKit.Label(info, "", 10, UIKit.Parchment, TextAnchor.MiddleCenter);
            t.Main();
            return t;
        }

        void Main()
        {
            _menu.Clear();
            var saves = SaveSystem.List();
            if (saves.Count > 0) _menu.Add($"Continue: {saves[0].PlaythroughName}", () => _g.LoadGame(saves[0].Path));
            _menu.Add("New Game", ShowDifficulty);
            _menu.Add("Load Game", Load, saves.Count > 0);
            _menu.Add("Quit", () => { Application.Quit(); });
            _menu.Build();
            _info.text = "Keyboard & mouse or controller. A second player joins any time by pressing START on another controller.\nSaves: " + SaveSystem.Root;
        }

        void ShowDifficulty()
        {
            _menu.Clear();
            void Opt(DnD.Game.Difficulty d, string text)
            {
                _menu.Add(text, () => { Destroy(gameObject); _g.StartNewGame(_input, d); });
            }
            Opt(DnD.Game.Difficulty.Story, "Story: forgiving. Cheap revives; resting brings back the fallen.");
            Opt(DnD.Game.Difficulty.Normal, "Normal: the intended experience.");
            Opt(DnD.Game.Difficulty.Hard, "Hard: revives are pricey and must be quick.");
            Opt(DnD.Game.Difficulty.Hardcore, "Hardcore: one save. A party wipe ends the story.");
            _menu.Add("Back", Main);
            _menu.Index = 1;
            _menu.Build();
        }

        void Load()
        {
            _menu.Clear();
            foreach (var s in SaveSystem.List())
            {
                var path = s.Path;
                _menu.Add($"{s.PlaythroughName}: {s.Summary}", () => _g.LoadGame(path));
            }
            _menu.Add("Back", Main);
            _menu.Build();
        }

        void Update()
        {
            _menu.Tick(_input, null);
            if (_input.Cancel) Main();
        }
    }
}
