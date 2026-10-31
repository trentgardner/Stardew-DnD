using UnityEngine;

namespace DnD.Game
{
    public static partial class WorldDb
    {
        /// <summary>Act II lore spots in Act I places (the legend is everywhere, once you know to look).</summary>
        static void PlaceLore()
        {
            void At(string zone, string id, int x, int y, string prompt, string sprite = "item_scroll_revivify")
            {
                if (Zones.TryGetValue(zone, out var z)) z.Lore.Add(new LoreSpotDef { Id = id, Pos = new Vector2Int(x, y), Prompt = prompt, Sprite = sprite });
            }
            At("temple", "ashen_queen", 2, 1, "Study the old mural");
            At("watchtower", "sleeping_mountains", 19, 2, "Read the carving on the back wall");
            At("cottage_lark", "skipping_rhyme", 3, 2, "Read the rhyme pinned by the window");
            At("ford", "missing_pages", 36, 20, "Leaf through the damp parish ledger");
        }
    }
}
