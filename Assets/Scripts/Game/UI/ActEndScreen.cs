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

        readonly int _act;
        public ActEndScreen(PlayerSlot p, int act = 1) { _act = act; }

        /// <summary>A line for the side the party stood with (what changed because of them).</summary>
        public static string SideLine(string faction)
        {
            switch (faction)
            {
                case "crown": return "The Crown's banner flies over Thistledown's bridge, and the granaries are full.";
                case "academy": return "The Academy lights the Lantern Road again, end to end, and the lamps watch the road.";
                case "fae": return "The Courts' spring runs warm in the Gloaming Glade, for now, and the frosts come on time.";
                case "hunters": return "The Wyrmwatch keeps its watch at Cragfoot, and the goats sleep safe.";
                default: return "You stood with no one, and everyone noticed.";
            }
        }

        static string ActTwoText()
        {
            var g = GameRoot.I;
            var sb = new StringBuilder();
            sb.AppendLine("<b>End of Act II: Answers</b>\n");
            sb.AppendLine("The Warden of the Emberhorn is at rest, and the mountain sleeps. Down in the valley the ash is thinning, and one by one, Thistledown's hearths catch again.");
            sb.AppendLine(SideLine(g.World.CommittedFaction));
            if (g.Dragon != null) sb.AppendLine($"{g.World.DragonName} walked into the mountain's heart beside you, and walked out again.");
            if (g.World.Flag(WorldDb.OathSecond)) sb.AppendLine("Two Ideals of the Oathbound are yours now.");
            sb.AppendLine();
            sb.AppendLine("<i>\"Hold... the door,\" the Warden said. Half the valley dreams of a door under the mountain. Something beneath the Wyrmback is still waking.</i>");
            sb.AppendLine("\n<b>Act III</b> is coming. Until then, the world is yours: finish what you started, and see who still needs you.");
            return sb.ToString();
        }

        /// <summary>The options offered (tests).</summary>
        public System.Collections.Generic.IEnumerable<string> RowTexts => _menu.Entries.Select(e => e.Text);

        public override void Build()
        {
            var g = GameRoot.I;
            if (_act >= 2) { BuildWith(ActTwoText(), "Continue your journey"); return; }
            int done = QuestDb.All.Keys.Count(id => g.Quests.Done(id)), total = QuestDb.All.Count;
            var sb = new StringBuilder();
            sb.AppendLine("<b>End of Act I: Home</b>\n");
            sb.AppendLine($"{g.Clock.Day} {(g.Clock.Day == 1 ? "day" : "days")} in Thistledown. {done} of {total} {(total == 1 ? "story" : "stories")} seen through.");
            if (g.Dragon != null) sb.AppendLine($"{g.World.DragonName} the dragonling, bond {DragonPet.Hearts}.");
            if (g.World.XpAwarded.Any(k => k.EndsWith(":met_buoy"))) sb.AppendLine("Buoy has decided you're his people.");
            sb.AppendLine();
            sb.AppendLine("<i>Word travels fast. A rider in royal colours is already on the road from the capital. A carriage with the " +
                          "Starfall Academy's crest has been seen at the ford, asking polite questions about missing eggs. And in Whisperwood, " +
                          "on nights with no moon, the fairy ring has started to hum.</i>");
            sb.AppendLine("\n<b>Act II</b> is coming. Until then, Thistledown is yours: finish what you started, and give the little one a good home.");
            BuildWith(sb.ToString(), "Keep exploring Thistledown");
        }

        void BuildWith(string text, string stay)
        {
            Text = text;
            Window(new Vector2(380, 250), null);
            var t = UIKit.Rect(Root, "Text", Vector2.zero, Vector2.one, new Vector2(16, 30), new Vector2(-16, -14));
            var lbl = UIKit.Label(t, Text, 10, UIKit.Ink);
            lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 7; lbl.resizeTextMaxSize = 10;
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 6), new Vector2(-8, 26));
            _menu = new MenuList(list) { RowHeight = 18, FontSize = 11 };
            _menu.Add(stay, Close);
            if (_act < 2 && GameRoot.I.World.Act < 2)
                _menu.Add("Begin Act II (preview: a season passes)", () => { Close(); GameRoot.I.BeginActTwo(Slot); });
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
