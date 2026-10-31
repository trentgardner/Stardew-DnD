using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>A piece of the world's history the party has found (a mural, a carving, a rhyme, a record).</summary>
    public class LoreDef
    {
        public string Id, Title, Category, Text;
    }

    /// <summary>
    /// Lore for the journal (Act II). The legend is told the way the world tells it. Records that are missing are just
    /// missing: no culprit is ever implied here. (The secret-leak test checks every line that names the Queen.)
    /// </summary>
    public static class LoreDb
    {
        public const string Legend = "The Legend", Records = "Records", Pact = "The Pact", Places = "Places";
        public const int FindXp = 25;

        public static readonly Dictionary<string, LoreDef> All = new Dictionary<string, LoreDef>();

        static void Add(string id, string title, string category, string text) =>
            All[id] = new LoreDef { Id = id, Title = title, Category = category, Text = text };

        static LoreDb()
        {
            Add("ashen_queen", "The Ashen Queen", Legend,
                "The temple mural shows a dragon of grey fire, wings spread over burning fields. Beneath it the priests painted the old hymn: \"When the Ashen Queen rose, the hearths of the world went cold, for she drank every flame. Then the Great Ones lay down upon her, and the mountains were made, and the fire came home to the hearths.\" In the last panel the Dawnmother's sun rises behind the Wyrmback, as if nothing could ever wake what sleeps there.");
            Add("sleeping_mountains", "The Sleeping Mountains", Places,
                "Carved into the watchtower's back wall, worn almost smooth: a line of mountains, each peak a dragon asleep with its head tucked under a wing. In the middle stands one hollow peak with a coal at its heart. The inscription reads: WE KEEP THE WATCH SO THE SLEEPERS NEED NOT WAKE.");
            Add("skipping_rhyme", "A Skipping Rhyme", Legend,
                "Lark hums it while she ties her bouquets, the one every skipping-rope in Thistledown knows: \"Ash on the hearth and ash on the stair, the Ashen Queen is everywhere! Blow out the candle, bar the door, and the Ashen Queen won't come no more!\"");
            Add("missing_pages", "Missing Pages", Records,
                "A parish ledger, swollen with damp, left on the mill's shelf by some long-gone clerk. Births, harvests and tithes, year after year in a careful hand, until the century the legends talk about. Those pages have been cut out, neatly, close to the spine. The record picks up again eighty years later as if nothing had happened.");
        }

        public static LoreDef Get(string id) => id != null && All.TryGetValue(id, out var l) ? l : null;

        /// <summary>Records a find for the whole party: a toast, and discovery XP once.</summary>
        public static bool Find(string id, PlayerSlot by)
        {
            var l = Get(id);
            var g = GameRoot.I;
            if (l == null || !g.World.LoreFound.Add(id)) return false;
            foreach (var p in g.Players.Where(p => p.IsReady))
            {
                p.Hud.Toast($"New lore: {l.Title} (journal)", UIKit.Gold);
                g.AwardOnce(p, "lore:" + id, FindXp, "lore");
            }
            return true;
        }
    }
}
