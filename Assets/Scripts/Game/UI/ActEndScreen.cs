using System.Linq;
using System.Text;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// "End of Act I: Home": shown once per player after Captain Vale's thanks. A short recap of your Act I, a glimpse of
    /// what's coming in Act II, and back to Thistledown (free play continues).
    /// </summary>
    public class ActEndScreen : GameScreen
    {
        public string Text { get; private set; } = "";
        MenuList _menu;

        public ActEndScreen(PlayerSlot p) { }

        public override void Build()
        {
            var g = GameRoot.I;
            int done = QuestDb.All.Keys.Count(id => g.Quests.Done(id)), total = QuestDb.All.Count;
            var sb = new StringBuilder();
            sb.AppendLine("<b>End of Act I: Home</b>\n");
            sb.AppendLine($"{g.Clock.Day} days in Thistledown. {done} of {total} stories seen through.");
            if (g.Dragon != null) sb.AppendLine($"{g.World.DragonName} the dragonling, bond {DragonPet.Hearts}.");
            if (g.World.XpAwarded.Any(k => k.EndsWith(":met_buoy"))) sb.AppendLine("Buoy has decided you're his people.");
            sb.AppendLine();
            sb.AppendLine("<i>Word travels fast. A rider in royal colours is already on the road from the capital. A carriage with the " +
                          "Starfall Academy's crest has been seen at the ford, asking polite questions about missing eggs. And in Whisperwood, " +
                          "on nights with no moon, the fairy ring has started to hum.</i>");
            sb.AppendLine("\n<b>Act II</b> is coming. Until then, Thistledown is yours: finish what you started, and give the little one a good home.");
            Text = sb.ToString();

            Window(new Vector2(380, 250), null);
            var t = UIKit.Rect(Root, "Text", Vector2.zero, Vector2.one, new Vector2(16, 30), new Vector2(-16, -14));
            var lbl = UIKit.Label(t, Text, 10, UIKit.Ink);
            lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 7; lbl.resizeTextMaxSize = 10;
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 6), new Vector2(-8, 26));
            _menu = new MenuList(list) { RowHeight = 18, FontSize = 11 };
            _menu.Add("Keep exploring Thistledown", Close);
            _menu.Build();
            Sfx.Play("levelup");
        }

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (In.Cancel) Close();
        }
    }
}
