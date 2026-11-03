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
        /// <summary>Act III: a new line the journal shows under this entry ("Read again") once the party has met her. Null = none.</summary>
        public string RevealText;
    }

    /// <summary>
    /// Lore for the journal (Act II). The legend is told the way the world tells it. Records that are missing are just
    /// missing: no culprit is ever implied here. (The secret-leak test checks every line that names the Queen.)
    /// </summary>
    public static partial class LoreDb
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
            Add("ashsworn_creed", "What the Ashsworn Say", Records,
                "As Teodor Brisk heard it from a stranger at the Ford: the hearths are going out because the old fire is waking under the mountain, and when the Queen wakes the cold will end. Those who open the way for her will be warm. Those who stand in the way will not. Teodor didn't ask who the stranger was. He says he wishes he had.");
            Add("grey_grove", "The Grey Grove", Places,
                "On the Emberhorn Road stands a grove that didn't burn. It greyed: bark, leaf and root all at once, every tree still standing exactly where it grew, as if something had breathed on it from very far away.");
            Add("second_ideal", "The Second Ideal", Pact,
                "Carved at an Oathbound waystation on the Emberhorn Road, in the same hand as the First: \"" + WorldDb.SecondIdeal + "\" The knights who swore it kept watch at the edge of the mountain's shadow. The names of those who stood there are worn away.");
            // Act II Phase 6: the Emberhorn.
            Add("iron_ring", "The Iron Ring", Records,
                "Deep in the Emberhorn, set into the living rock: an iron ring as wide as a cartwheel, old beyond reckoning. The chain that hung from it is long gone, melted away link by link. Someone, once, wanted something to stay here.");
            Add("warden_last_words", "The Warden's Last Words", Legend,
                "The Warden of the Emberhorn, one of the Great Ones who lie down around the mountain's fire, laid its head on the stone and said only: \"Hold... the door.\" Then the light went out of it, and the mountain went quiet.");
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
            // Act III Phase 3: Aurelhold.
            Add("victory_statue", "The Victory of the Crown", Legend,
                "Cut into the plinth in the Great Square, in letters a hand high and freshly gilded for the Thousandth Pageant: \"A THOUSAND YEARS. Here the first King of Aurelhold struck down the Ashen Queen and bound her fire, so that the realm might never burn again. LONG MAY THE CROWN KEEP THE FLAME.\"", 3);
            Add("ever_lamps", "The Ever-Lamps", Places,
                "Aurelhold's lamps have no oil and no wicks. They have never been lit, as far as anyone knows, and never gone out. The city calls it the King's blessing. The iron posts are warm to the touch, like a hearthstone, and the warmth comes up out of the ground.", 3);
            Add("works_ledger", "The Works Ledger", Records,
                "The Foundation Works' ledger, two columns to a page: WENT DOWN and CAME UP. Day shifts and night shifts, names and ticks, back and forth for years. Then a third column, DEEP SHIFT, in a different ink. Forty names in it, going back further than the foreman has been alive. Not one of them has a tick under CAME UP. Every one has a tick under PAID.", 3);
            Add("the_kings_ring", "The King's Ring", Records,
                "King Aldric IX wears a plain iron ring under his gold one. Every King of Aurelhold has, in every portrait, on the same finger. The court calls it tradition and nobody remembers why. When the Emberhorn comes up, his thumb turns it round and round.", 3);
            Add("royal_annals", "The Royal Annals", Records,
                "Nine hundred volumes of the Crown's own history in red leather, one for every year. Year 1, the founding, is gold-leafed and glorious. Year 2 is an empty binding: every page cut out with a razor, long ago. The Archivist-Royal says water damage. The Academy's Annals were 'corrected' for the same years.", 3);
            Add("sealed_crypt", "The Sealed Crypt", Places,
                "Under the Lantern Cathedral, a royal crypt sealed with iron and the King's seal. The verger swears it hasn't been opened in a century. The dust on its stair lies a year thick, except for one clean path down the middle, worn smooth by somebody's feet.", 3);
            // Act III Phase 4: below Aurelhold (before the door).
            Add("warm_pipes", "The Warm Pipes", Places,
                "Under the Roots, fat iron pipes run up the rock toward the city: to the ever-lamps, the Steamhouse, the palace hearths. None of them comes from a furnace. They come out of the stone, from somewhere below, warm as a hand. The deep shift tends them and never asks what's on the other end.", 3);
            Add("deep_shift_rest", "The Deep Shift's Rest", Records,
                "Forty cairns below the Undergate, each with a name chalked on a flat stone and touched up so often the chalk is an inch thick. Hap Mossy keeps them. 'They went peaceful, mostly,' he says. 'Old age. It's a long shift.'", 3);
            Add("ward_stones", "The Ward-Stones", Places,
                "In the Iron Vaults below the Undergate, black ward-stones are set into every wall, each carved over and over with one word in a script older than the Crown: HOLD. The wards are strong and old, and every one of them faces the same way: inward, and down. Nothing there was built to keep anyone out.", 3);
            Add("kings_path", "The Kings' Path", Records,
                "In the Royal Crypts the dust lies a year thick, except for one clean path. It comes down the cathedral stair, runs past every King's tomb without stopping, and goes on down. One pair of feet, once a year, for a very long time.", 3);
            Add("third_ideal", "The Third Ideal", Pact,
                "Scratched on the tomb of the last of the Oathbound, in the crypt of the Kings she served: \"" + WorldDb.ThirdIdeal + "\" Nobody knows why she was buried there, or who let her be.", 3);
            Add("the_great_ones_below", "The Great Ones, Below", Legend,
                "The Great Ones didn't lie down on top of anything. In the Deep, at the roots of the Wyrmback, they lie in a ring, chins on the floor, facing in: the way you'd sit around a fire, or around someone you meant to keep company.", 3);
            Add("the_door", "The Door", Legend,
                "The door from the valley's dream, real, at the bottom of everything: grey, warm, and knocked on from the other side.", 3);
            Add("missing_pages", "Missing Pages", Records,
                "A parish ledger, swollen with damp, left on the mill's shelf by some long-gone clerk. Births, harvests and tithes, year after year in a careful hand, until the century the legends talk about. Those pages have been cut out, neatly, close to the spine. The record picks up again eighty years later as if nothing had happened.");
            RegisterReveals(); // the "Read again" lines (LoreDb.AfterTheDoor.cs), shown only once the party has met her
        }

        public static LoreDef Get(string id) => id != null && All.TryGetValue(id, out var l) ? l : null;

        /// <summary>Gives an entry its "Read again" line (null clears it). Only the *.AfterTheDoor.cs files call this, once WorldDb.MetHer is set.</summary>
        public static void SetReveal(string id, string text) { var l = Get(id); if (l != null) l.RevealText = text; }

        /// <summary>True when the journal should show this entry's reveal: the party has met her, and the entry has one.</summary>
        public static bool Revealed(LoreDef l, WorldState w) => l.RevealText != null && w.Flag(WorldDb.MetHer);

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
