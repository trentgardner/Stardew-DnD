using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>A piece of the world's history the party has found (a mural, a carving, a rhyme, a record).</summary>
    public class LoreDef
    {
        public string Id, Title, Category, Text;
        /// <summary>The first act it can be found in. Act I stays light (the user): only a couple of ominous notes in chests.</summary>
        public int MinAct = 2;
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

        static void Add(string id, string title, string category, string text, int minAct = 2) =>
            All[id] = new LoreDef { Id = id, Title = title, Category = category, Text = text, MinAct = minAct };

        static LoreDb()
        {
            // Act I: two small, ominous notes in chests. Nothing essential, nothing about the legend.
            Add("miners_scrawl", "A Miner's Scrawl", Records,
                "A scrap of paper wedged under the lid, in a cramped hand: \"Third night running the lamps won't stay lit on the lower level. Plenty of oil. The flame just shrinks and gives up, like something breathed on it. My partner says the mountain's hungry. I say my partner drinks. Moving my pick upstairs all the same.\"", minAct: 1);
            Add("lamp_log", "A Watchman's Lamp Log", Records,
                "The last page of a watchman's log, the rest long gone to damp: \"Night twelve. Smoke over the Wyrmback again, though nobody lives up there. The wind came down smelling of a cold hearth. Lit an extra lamp. Felt foolish. Lit another.\"", minAct: 1);

            // Act II Phase 3: the Lantern Road.
            Add("ash_fields", "Rings in the Ash", Places,
                "On the Lantern Road the ash lies in wide, even rings, as if something very far away breathed out and the wind carried the breath round in circles. The rings are centred somewhere to the north-east. Towards the mountains.");
            // Act II Phase 4: the four forces.
            Add("beacon_orders", "The Ashsworn's Notes", Records,
                "A sheaf of notes from the old beacon, in a dozen different hands, passed from one keeper to the next. They light no fires of their own. They gather where the ash falls thickest. They wait for the waking, and they write down every hearth that goes cold, as if keeping count for someone. The last page says only: \"Soon. Be ready to open the door.\"");
            Add("lantern_eyes", "The Lamps That Watch", Records,
                "Inside every lantern on the Lantern Road, behind the wick, a tiny ward-glyph is scratched into the iron: an eye, shut. When the lamp is lit, the eye opens. They were made to watch the road for one thing only, and they have been watching for a very long time: the colour of dragon fire.");
            Add("glade_mirror", "The Late Reflection", Pact,
                "In the Gloaming Glade's still pond your reflection runs a moment behind you, and for that moment it shows the glade as it was. The courts sit together at one table. There are more of them than three. Then you blink, and it is three again, and the other chairs were never there.");
            Add("trophy_wall", "The Hunters' Tallies", Records,
                "Beside the Wyrmwatch Lodge door, a board of tallies in many hands, going back further than the lodge itself. Each line is a beast and a village: 'Ember-eyed bear, Lower Sedge, two children saved.' 'Cinder hound pack, the Ford road, no one lost.' The newest lines are close together. Very close together. Above them all, nailed up long ago, one great old dragon's tooth, and no tally beside it at all.");
            // Act II Phase 5: the blight reaches home.
            Add("shared_dream", "The Same Dream", Legend,
                "Half the valley is dreaming the same dream: a great grey door deep under the mountain, warm to the touch, and something on the other side knocking, slow and steady, as if it had all the time in the world. The old folk say their grandparents dreamed it too, once, long ago, and then it stopped.");
            Add("checkpoint_orders", "Checkpoint Orders", Records,
                "A Crown checkpoint ledger, kept in a neat hand: names, dates, where from, where bound. One column is new this season, ruled in fresh ink: 'Travelling with an animal? (Describe.)' Three entries say 'dog'. One says 'large dog (?)'. The orders pinned inside the cover only say: count them all, and send the count on.");
            // Act II Phase 3: the Starfall Academy. The legend is everywhere, and everyone believes it.
            Add("great_ones_statue", "The Great Ones", Legend,
                "The plaque on the courtyard plinth, polished by a thousand students' thumbs: \"THE GREAT ONES, WHO LAY DOWN SO THE WORLD COULD WAKE.\" Below it, smaller: \"This Academy was founded on the night the star fell, in gratitude, by those who would learn what the Great Ones knew.\"");
            Add("primer_ashen_age", "A Primer of the Ashen Age", Legend,
                "A first-year textbook, dropped on the lawn. Chapter One: \"Long ago the Ashen Queen drank the fire of the world and would not give it back. Hearths went cold from the sea to the mountains. Then the Great Ones lay down upon her and slept, and became the Wyrmback.\" QUESTIONS: 1. Why did the hearths go cold? 2. Name three of the Great Ones. 3. Why must they never wake? In the margin someone has drawn a dragon with a speech bubble: \"five more minutes\".");
            Add("thirteenth_bell", "The Thirteenth Bell", Places,
                "The Academy rings one bell for every Great One sleeping in the Wyrmback: twelve. The thirteenth bell is older than the tower, and nobody has ever found it. The old porters say it rings so that whatever lies beneath the mountains knows it is still being watched.");
            Add("starwell", "The Starwell", Places,
                "The founders built their first tower around the place a star came down, eight hundred years ago. The well never runs dry and never freezes, and on clear nights the water holds more stars than the sky does. Students drop a copper in before exams. The well, by long tradition, keeps them.");
            // Act II Phase 3: the Restricted Section and the Sealing Hall.
            Add("emberhorn_warden", "The Warden of the Emberhorn", Records,
                "From a bestiary chained to its shelf, the page worn soft with reading: \"In the heart of the Emberhorn sleeps a dragon of the old blood, the Warden, set to keep the mountain's fire. Its fire has gone wrong; it burns grey at the edges. The wardens of old wrote this down for whoever must face it: when it draws breath, the plates of its throat part and glow, and a blow struck there as it breathes will make it choke on its own fire.\"");
            Add("altered_annals", "The Annals, Corrected", Records,
                "The Academy's annals, one fat volume per year. One year's entries have been written over in a different ink, neat and careful, the original lines scraped away so thoroughly the page has gone thin as skin. Hold it to the light and you can see that something was there. You can't see what.");
            Add("sealing_work", "Notes from the Sealing Hall", Records,
                "Notes left on a workbench, in several hands. \"Circle nine of twelve complete.\" \"Test binding held a fire-drake of the third size for one hour, four minutes.\" \"Not enough.\" And, underlined twice, in a hand you'll come to know: \"It must hold forever. Gently, and forever.\"");
            Add("grey_fire", "Grey Fire", Places,
                "The glasshouse's heat-stone was quarried from the Emberhorn's slopes a hundred years ago and has burned warm and steady ever since. Now it's cracked, and the fire inside has gone grey: not out, but wrong, like a hearth that gives light and no heat.");
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
