using System.Linq;
using System.Collections.Generic;

namespace DnD.Game
{
    /// <summary>Things that happen in the world that can move a quest along.</summary>
    public enum QuestEvent { ZoneEntered, GroupResolved, ItemGained, FlagSet, SecretFound, BrazierLit }

    /// <summary>Act II's main quest, "answers".</summary>
    public static class Answers { public const string Id = "answers"; public const int Road = 1, Academy = 2, Tower = 3, Heard = 4, Chosen = 5, Allies = 6, OnTheRoad = 7, Gate = 8, Rest = 9; }

    /// <summary>Act III side quest, "deep_shift" (stage 4, Bran comes home, is Phase 4's).</summary>
    public static class DeepShift { public const string Id = "deep_shift"; public const int Asked = 1, Crale = 2, Below = 3; }

    /// <summary>Act III's opening quest, "warm_city": the capital (stage 5 is the way down, Phase 4 onward).</summary>
    public static class WarmCity { public const string Id = "warm_city"; public const int Summoned = 1, Arrived = 2, Audience = 3, Below = 4; }

    /// <summary>Stages of the main quest, "fallen_star" (save version 2 numbering).</summary>
    public static class FallenStar { public const int Crater = 1, Trail = 2, Warrens = 3, Egg = 4, Hatched = 5, Done = 6; }

    /// <summary>When event On happens with this Id and the quest is at FromStage, it moves to ToStage.</summary>
    public class QuestTrigger
    {
        public QuestEvent On;
        public string Id;
        public int FromStage, ToStage;
        /// <summary>FlagSet only: fires when ALL of these flags are set (Id is ignored).</summary>
        public string[] AllOf;
        public QuestTrigger(QuestEvent on, string id, int from, int to) { On = on; Id = id; FromStage = from; ToStage = to; }
        public static QuestTrigger AllFlags(int from, int to, params string[] flags) => new QuestTrigger(QuestEvent.FlagSet, null, from, to) { AllOf = flags };
        /// <summary>FlagSet only: with AllOf, fires once at least this many of them are set (0 = all of them).</summary>
        public int AtLeast;
        /// <summary>BrazierLit only: fires once at least n braziers in this zone are lit.</summary>
        public string LitZone;
        public static QuestTrigger LitAtLeast(string zone, int from, int to, int n) => new QuestTrigger(QuestEvent.BrazierLit, zone, from, to) { LitZone = zone, AtLeast = n };
        public static QuestTrigger FlagsAtLeast(int from, int to, int n, params string[] flags) => new QuestTrigger(QuestEvent.FlagSet, null, from, to) { AllOf = flags, AtLeast = n };
    }

    /// <summary>Paid once when the quest reaches its last stage. Xp and Copper go to every player; Item to whoever finished it.</summary>
    public class QuestReward
    {
        public int Xp, Copper;
        public string Item;
        public string Npc;
        public int Friendship;
        public string Flag;
    }

    public class QuestDef
    {
        public string Id, Title;
        /// <summary>NPC id of whoever hands it out (shown in the journal), or null.</summary>
        public string Giver;
        /// <summary>Journal text per stage. Stage 0 = not started (""); the last stage = complete.</summary>
        public string[] StageText;
        public List<QuestTrigger> Triggers = new List<QuestTrigger>();
        public QuestReward Reward;
        /// <summary>XP for reaching a stage (milestones on a long quest), paid once to every ready player.</summary>
        public Dictionary<int, int> StageXp = new Dictionary<int, int>();
        /// <summary>Act II: which of the four forces this quest belongs to (null = nobody's), and whether it's that force's major quest (the point of no return).</summary>
        public string Faction;
        /// <summary>The act this quest belongs to (the Act I XP audit only counts Act I).</summary>
        public int Act = 1;
        public bool Major;
        /// <summary>Reaching a stage sets this world flag (e.g. a road opens).</summary>
        public Dictionary<int, string> StageFlags = new Dictionary<int, string>();
        public int Last => StageText.Length - 1;
    }

    /// <summary>
    /// Every quest. Talk-driven stages are set from DialogueDb (Quests.Set); world-driven stages use Triggers.
    /// Stage numbers of existing quests must never change meaning (they're in players' saves).
    /// </summary>
    public static class QuestDb
    {
        public static readonly Dictionary<string, QuestDef> All = new Dictionary<string, QuestDef>();

        public static QuestDef Get(string id) => id != null && All.TryGetValue(id, out var q) ? q : null;

        static QuestDef Add(QuestDef q) { All[q.Id] = q; return q; }

        /// <summary>Tests only: a scouting and a major quest for each of the four forces. Never shipped in the real list.</summary>
        public static void RegisterTestQuests()
        {
            foreach (var f in FactionDb.All.Keys)
            {
                Add(new QuestDef { Id = "test_scout_" + f, Title = "Test scouting: " + f, Faction = f, StageText = new[] { "", "Scouting.", "Done." } });
                Add(new QuestDef { Id = "test_major_" + f, Title = "Test major: " + f, Faction = f, Major = true, StageText = new[] { "", "Committed.", "Done." } });
            }
        }

        public static void RemoveTestQuests()
        {
            foreach (var id in All.Keys.Where(k => k.StartsWith("test_scout_") || k.StartsWith("test_major_")).ToList()) All.Remove(id);
        }

        static QuestDb()
        {
            Add(new QuestDef
            {
                Act = 3, Id = DeepShift.Id, Title = "The Deep Shift", Giver = "mags",
                StageXp = { [DeepShift.Below] = 4000 },
                StageFlags = { [DeepShift.Below] = "clue_shift" },
                StageText = new[] {
                    "", "Mags Tolley's husband Bran works the deep shift at the Foundation Works, in the Roots. His letters come up every week. He never does.",
                    "Foreman Crale won't talk about the deep shift. The works ledger might.",
                    "The deep shift goes down through the Undergate and stays down, on the Crown's orders and the Crown's pay. Bran is alive, somewhere below.",
                    "Bran comes home." },
            });
            Add(new QuestDef
            {
                Act = 3, Id = WarmCity.Id, Title = "The Warm City", Giver = "quell",
                StageXp = { [WarmCity.Arrived] = 2000, [WarmCity.Audience] = 3000, [WarmCity.Below] = 6000 },
                StageText = new[] {
                    "", "A royal courier is waiting in Thistledown square with the King's own carriage.",
                    "Aurelhold, the capital, inside the ring of the Wyrmback. Everywhere else went grey. Here, every lamp is lit. The King expects you at the palace.",
                    "The King was gracious, and asked your dragon to lead the Thousandth Pageant. The city is beautiful. Some of it doesn't add up.",
                    "Lamps that burn on nothing. Records with holes in them. A crypt nobody admits to using. A shift that never comes up. Everything in Aurelhold points down, to the Undergate in the Roots.",
                    "Below Aurelhold." },
                Triggers = {
                    new QuestTrigger(QuestEvent.ZoneEntered, "capital", WarmCity.Summoned, WarmCity.Arrived),
                    QuestTrigger.FlagsAtLeast(WarmCity.Audience, WarmCity.Below, 3, "clue_lamps", "clue_records", "clue_crypt", "clue_shift"),
                },
            });
            Add(new QuestDef
            {
                Id = "fallen_star", Title = "The Fallen Star", Giver = "rhoswen",
                // Milestones so the main story alone gets a party to level 3 by the egg (co-op splits fight XP).
                StageXp = { [FallenStar.Trail] = 100, [FallenStar.Warrens] = 100, [FallenStar.Egg] = 100 },
                StageText = new[] {
                    "", "Captain Vale asked you to find where the star fell, in the hills past Whisperwood's north trail.",
                    "The crater is empty. Something carried the star away on small clawed feet, and things keep going missing in town. Follow the tracks, or catch the thief in the act after dark.",
                    "The trail leads into the old copper mine and down, to the kobolds' warrens.",
                    "You found the source: a glowing red egg in the depths of the mine, guarded by devoted kobolds.",
                    "The egg hatched! A baby dragon has chosen you. Return to Captain Vale.",
                    "Complete. Thistledown has a dragon now. The capital will hear of this soon..." },
                Triggers = {
                    new QuestTrigger(QuestEvent.SecretFound, "crater_hollow", FallenStar.Crater, FallenStar.Trail),
                    new QuestTrigger(QuestEvent.SecretFound, "crater_tracks", FallenStar.Trail, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.ZoneEntered, "warrens", FallenStar.Crater, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.ZoneEntered, "warrens", FallenStar.Trail, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", 0, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Crater, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Trail, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Warrens, FallenStar.Egg),
                },
                Reward = new QuestReward { Xp = 300, Copper = 5000, Npc = "rhoswen", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = Answers.Id, Title = "Answers", Giver = "rhoswen",
                // Act II's spine carries most of the act's XP (Phase 7 audit: level 9 on the main path by the gate, 10 when the Warden falls, in co-op too).
                StageXp = { [Answers.Academy] = 3000, [Answers.Tower] = 2000, [Answers.Heard] = 6000, [Answers.Chosen] = 8000, [Answers.Allies] = 6000, [Answers.OnTheRoad] = 4000, [Answers.Gate] = 6000, [Answers.Rest] = 6000 },
                StageFlags = { [Answers.Allies] = WorldDb.EmberhornOpen, [Answers.Gate] = WorldDb.EmberhornGate },
                StageText = new[] {
                    "", "Hearths are going cold down the valley. Captain Vale says if anyone has answers, it's the Starfall Academy, up the Lantern Road past Miller's Ford.",
                    "You've reached the Starfall Academy. Find High Magister Ashby. They say he lives at the top of the Orrery Tower.",
                    "Ashby's door is open. Go up and hear him out.",
                    "Ashby wants your dragon studied and the dragons sealed. He isn't the only one with plans for it. Hear out the Crown, the Fae Courts and the Wyrmwatch, then choose who you stand with.",
                    "You've chosen a side. Finish what your allies asked of you.",
                    "Your allies agree on one thing: the answers are in the Emberhorn, the mountain whose fire has gone grey. The high road east of Cragfoot is open.",
                    "Cross the Emberhorn Road to the mountain's gate. Someone from your side is waiting at the Last Camp.",
                    "The Emberhorn's gate stands open. The Warden of the Emberhorn waits in the mountain's heart.",
                    "The Warden of the Emberhorn is at rest, and the mountain sleeps. The ash will thin. But something beneath the Wyrmback is still waking." },
                Triggers = {
                    new QuestTrigger(QuestEvent.ZoneEntered, "academy", Answers.Road, Answers.Academy),
                    new QuestTrigger(QuestEvent.ZoneEntered, "ashby_tower", Answers.Academy, Answers.Tower),
                    new QuestTrigger(QuestEvent.ZoneEntered, "ashby_tower", Answers.Road, Answers.Tower),
                    new QuestTrigger(QuestEvent.FlagSet, "ashby_heard", Answers.Road, Answers.Heard),
                    new QuestTrigger(QuestEvent.FlagSet, "ashby_heard", Answers.Academy, Answers.Heard),
                    new QuestTrigger(QuestEvent.FlagSet, "ashby_heard", Answers.Tower, Answers.Heard),
                    new QuestTrigger(QuestEvent.FlagSet, Factions.CommittedFlag, Answers.Heard, Answers.Chosen),
                    new QuestTrigger(QuestEvent.FlagSet, Factions.CommittedFlag, Answers.Tower, Answers.Chosen),
                    new QuestTrigger(QuestEvent.FlagSet, Factions.CommittedFlag, Answers.Academy, Answers.Chosen),
                    new QuestTrigger(QuestEvent.FlagSet, Factions.CommittedFlag, Answers.Road, Answers.Chosen),
                    QuestTrigger.FlagsAtLeast(Answers.Chosen, Answers.Allies, 1, "banner_crown", "banner_academy", "banner_fae", "banner_hunters"),
                    new QuestTrigger(QuestEvent.ZoneEntered, "emberhorn_road", Answers.Allies, Answers.OnTheRoad),
                },
            });
            // ------------------------------------------------------------ Act II Phase 5: the blight at home
            Add(new QuestDef
            {
                Act = 2, Id = "ash_on_doors", Title = "Ash on the Doors", Giver = "rhoswen",
                StageText = new[] {
                    "", "Someone is marking Thistledown's doors with ash: a hand, pressed flat. Look at the marks (the general store, the Sleeping Drake, and your own front door).",
                    "Whoever it is comes at night. Wait in the square after dark.",
                    "Complete. You know who was marking the doors, and why." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "ash_mark_1", "ash_mark_2", "ash_mark_3") },
                Reward = new QuestReward { Xp = 900, Npc = "rhoswen", Friendship = 50 },
            });

            // ------------------------------------------------------------ Act II Phase 4: the Crown
            Add(new QuestDef
            {
                Act = 2, Id = "counted", Title = "Counted", Giver = "holloway", Faction = "crown",
                StageText = new[] {
                    "", "Sergeant Holloway's supply wagon from Greywater Redoubt is two days late. Look for it in the ash fields south of the Lantern Road.",
                    "The hounds are gone from the wagon. The drivers cut the horses loose and ran: their tracks lead to the abandoned farmhouse.",
                    "The drivers are safe and on their way. Tell Holloway.",
                    "Complete. The Crown keeps this road fed, one late wagon at a time." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "wagon_hounds", 1, 2) },
                Reward = new QuestReward { Xp = 1200, Item = "potion_greater_healing", Npc = "holloway", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "two_copies", Title = "Two Copies", Giver = "varrow", Faction = "crown",
                StageText = new[] {
                    "", "Magister Varrow's second copies go out with the evening post. The post rider takes the Lantern Road north, to Greywater Redoubt. Ask whoever reads them.",
                    "Complete. You know who reads the Academy's notes, and you decided what to do about it." },
                Reward = new QuestReward { Xp = 1000 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "crowns_banner", Title = "Under the Crown's Banner", Giver = "marsh", Faction = "crown", Major = true,
                StageText = new[] {
                    "", "You stand with the Crown. Lady-Commander Marsh wants the Ashsworn out of the old beacon tower in the Redoubt's north-east corner.",
                    "The beacon is clear. Search what the Ashsworn left behind.",
                    "You have the Ashsworn's notes. Report to Lady-Commander Marsh.",
                    "Complete. Your dragon wears the realm's brass tag, and the realm stands behind you." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "beacon_ashsworn", 1, 2) },
                Reward = new QuestReward { Xp = 3000, Item = "crown_writ", Npc = "marsh", Friendship = 100, Flag = "banner_crown" },
            });

            // ------------------------------------------------------------ Act II Phase 4: the Academy
            Add(new QuestDef
            {
                Act = 2, Id = "cold_lanterns", Title = "Cold Lanterns", Giver = "perrin", Faction = "academy",
                StageText = new[] {
                    "", "Perrin is Keeper of the Road Lamps now, and six of the Lantern Road's lanterns have gone cold. Relight them: a torch will do, and so will a dragon.",
                    "The road is lit again. Tell Perrin.",
                    "Complete. The Academy lights the road for everyone who travels it." },
                Triggers = { QuestTrigger.LitAtLeast("lantern_road", 1, 2, 6) },
                Reward = new QuestReward { Xp = 1000, Npc = "perrin", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "ninth_circle", Title = "The Ninth Circle", Giver = "ashby", Faction = "academy", Major = true,
                StageText = new[] {
                    "", "You stand with the Academy. Ashby's ninth circle needs three focus stones: ask Magister Varrow and Professor Oakes for theirs, and draw the third from the Starwell.",
                    "You have all three stones. Take them down to the Sealing Hall and set them in the circle.",
                    "The circle has caught something, and it isn't holding.",
                    "The circle held for a breath. Tell Ashby what happened.",
                    "Complete. The ninth circle is finished, and the Academy stands behind you." },
                Triggers = {
                    QuestTrigger.AllFlags(1, 2, "stone_varrow", "stone_oakes", "stone_starwell"),
                    new QuestTrigger(QuestEvent.GroupResolved, "circle_bear", 3, 4),
                },
                Reward = new QuestReward { Xp = 3000, Item = "ward_charm", Npc = "ashby", Friendship = 100, Flag = "banner_academy" },
            });

            // ------------------------------------------------------------ Act II Phase 4: the Fae Courts
            Add(new QuestDef
            {
                Act = 2, Id = "court_quarrel", Title = "A Quarrel of Courts", Giver = "sorrel", Faction = "fae",
                StageText = new[] {
                    "", "The Courts of Blossoms and Frost both claim the Gloaming Glade's last warm spring. Hear each court's speaker out.",
                    "You've heard both sides. Give Herald Sorrel your judgement at the stone table.",
                    "Complete. The Courts have your judgement, and the Fae never forget a judgement." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "heard_blossom", "heard_frost") },
                Reward = new QuestReward { Xp = 1100, Npc = "sorrel", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "green_bargain", Title = "The Green Bargain", Giver = "sorrel", Faction = "fae", Major = true,
                StageText = new[] {
                    "", "You stand with the Fae Courts. Carry a token to each of the three thrones (Blossoms, Embers and Frost) so the Courts will renew their part of the Pact.",
                    "Every throne has accepted its token. Sign the bargain with Herald Sorrel.",
                    "Complete. The bargain is signed, and the Courts stand behind you." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "throne_blossom", "throne_ember", "throne_frost") },
                Reward = new QuestReward { Xp = 3000, Npc = "sorrel", Friendship = 100, Flag = "banner_fae" },
            });

            // ------------------------------------------------------------ Act II Phase 4: the Wyrmwatch
            Add(new QuestDef
            {
                Act = 2, Id = "ember_eyes", Title = "Ember Eyes", Giver = "calloway", Faction = "hunters",
                StageText = new[] {
                    "", "Something has been taking Cragfoot's goats at night. Keep watch over the empty pen after dark.",
                    "Wolves over the palisade! Deal with them.",
                    "The wolves are gone, but Odo Fenn is hurt. Help him.",
                    "Odo will live. Search where the wolves came through the palisade.",
                    "You found a dog's collar on one of them. Tell Huntmaster Calloway.",
                    "Complete. The danger is real, and the beasts were someone's, once." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "cragfoot_wolves", 2, 3) },
                Reward = new QuestReward { Xp = 1200, Npc = "calloway", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "long_hunt", Title = "The Long Hunt", Giver = "calloway", Faction = "hunters", Major = true,
                StageText = new[] {
                    "", "You stand with the Wyrmwatch. Track the Ashmaw, the wyvern taking Cragfoot's goats, up the goat track north of the hamlet.",
                    "The trail leads into the cliffs. Find the Ashmaw's lair.",
                    "The Ashmaw is dealt with. Search its nest.",
                    "Report to Huntmaster Calloway.",
                    "Complete. The hunt is over, and the Wyrmwatch stands behind you." },
                Triggers = {
                    QuestTrigger.AllFlags(1, 2, "ashmaw_track_a", "ashmaw_track_b"),
                    new QuestTrigger(QuestEvent.GroupResolved, "ashmaw_den", 2, 3),
                },
                Reward = new QuestReward { Xp = 3000, Item = "wyrmwatch_cloak", Npc = "calloway", Friendship = 100, Flag = "banner_hunters" },
            });

            Add(new QuestDef
            {
                Act = 2, Id = "overdue", Title = "Overdue", Giver = "thorne",
                StageText = new[] {
                    "", "Archivist Thorne will open the Restricted Section to you, if you bring back three very overdue books: one the geese have been sitting on, one Pell 'borrowed', and one lost somewhere in the hedge maze.",
                    "You have all three books. Return them to Archivist Thorne.",
                    "Complete. Thorne stamped your pass with great force. The Restricted Section is open to you." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "book_geese", "book_pell", "book_maze") },
                Reward = new QuestReward { Xp = 600, Copper = 4000, Npc = "thorne", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "glasshouse", Title = "The Wilting Glasshouse", Giver = "oakes",
                StageText = new[] {
                    "", "Professor Oakes's ember-lilies are turning grey, and something is moving in the far beds of the Glasshouse. Deal with it, kindly if you can.",
                    "The vines are still. Now find what's wrong at the root.",
                    "You found it: the glasshouse's heat-stone is cracked, and its fire has gone grey. Tell Professor Oakes.",
                    "Complete. Oakes is going to try a new heat-stone, from somewhere far from the mountains." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "glasshouse_blights", 1, 2) },
                Reward = new QuestReward { Xp = 700, Npc = "oakes", Friendship = 100, Item = "ember_lily" },
            });
            Add(new QuestDef
            {
                Act = 2, Id = "measure_wyrm", Title = "The Measure of a Wyrm", Giver = "varrow",
                StageText = new[] {
                    "", "Magister Varrow wants to measure your dragon under the stars. Find her at the Observatory's telescopes after dark (eight bells and later).",
                    "Complete. Varrow has her numbers, or doesn't. Either way, she gave you a star chart." },
                Reward = new QuestReward { Xp = 500, Npc = "varrow", Friendship = 50, Item = "star_chart" },
            });
            Add(new QuestDef
            {
                Id = "harvest_festival", Title = "The Harvest Festival", Giver = "pip",
                StageText = new[] {
                    "", "Pip says the Harvest Festival is on Day 10 in the square, from nine bells. Try the games!",
                    "Complete. You ate too much pie. Everyone did." },
                Triggers = { QuestTrigger.FlagsAtLeast(1, 2, 3, "fest_bob", "fest_archery", "fest_pie", "fest_stage") },
                Reward = new QuestReward { Xp = 150, Copper = 1000, Npc = "pip", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "watchtower", Title = "The Watchtower Inscription", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale asked you to scout the Old Watchtower on the ridge north of Miller's Ford.",
                    "You read the inscription at the top of the tower. Tell Captain Vale what it says.",
                    "Complete. Captain Vale has copied the words into her notebook. Twice." },
                Triggers = {
                    new QuestTrigger(QuestEvent.SecretFound, "tower_inscription", 0, 2),
                    new QuestTrigger(QuestEvent.SecretFound, "tower_inscription", 1, 2),
                },
                Reward = new QuestReward { Xp = 200, Copper = 2000, Npc = "rhoswen", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "go_between", Title = "Snik Speaks for You", Giver = null,
                StageText = new[] {
                    "", "Snik agreed to speak for you in the kobold warrens below the old mine. Meet him at the village gate.",
                    "Snik talked the gate guards round. Now he needs Elder Ssark's blessing.",
                    "Complete. The Elder blessed you; the way to the Great Egg is open." },
                Triggers = {
                    new QuestTrigger(QuestEvent.GroupResolved, "warrens_gate", 1, 2),
                    new QuestTrigger(QuestEvent.FlagSet, "elder_blessing", 1, 3),
                    new QuestTrigger(QuestEvent.FlagSet, "elder_blessing", 2, 3),
                },
                Reward = new QuestReward { Xp = 150, Item = "lucky_pebble" },
            });
            Add(new QuestDef
            {
                Id = "kettle", Title = "Grandmother's Kettle", Giver = "brannoc",
                StageText = new[] {
                    "", "Brannoc's copper kettle was stolen by something with small scaly feet.",
                    "Complete. Brannoc has his kettle back (and you have a permanent discount)." },
                Triggers = { new QuestTrigger(QuestEvent.ItemGained, "copper_kettle", 0, 1) },
                Reward = new QuestReward { Xp = 100, Npc = "brannoc", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "spoon", Title = "Tobin's Lucky Spoon", Giver = "tobin",
                StageText = new[] {
                    "", "Old Tobin's lucky spoon was stolen by 'a tiny lizard man in a cape.'",
                    "Complete. The soup is lucky again." },
                Triggers = { new QuestTrigger(QuestEvent.ItemGained, "lucky_spoon", 0, 1) },
                Reward = new QuestReward { Xp = 75, Item = "potion_healing", Npc = "tobin", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "east_road", Title = "The Broken Bridge", Giver = "hobb",
                StageText = new[] {
                    "", "The bridge at Miller's Ford is out, and a crew calling themselves the Ford Wardens charges a 'toll' at the ford upstream. Old Hobb the miller is stuck on the far bank. Deal with the Wardens however you like.",
                    "The ford is clear. Let Old Hobb know (across the river, by the mill).",
                    "Complete. The east road is open and Hobb's mill is turning again." },
                Triggers = { new QuestTrigger(QuestEvent.ZoneEntered, "ford", 0, 1), new QuestTrigger(QuestEvent.GroupResolved, "ford_toll", 1, 2) },
                Reward = new QuestReward { Xp = 150, Copper = 2500, Item = "potion_greater_healing", Npc = "hobb", Friendship = 100 },
            });

            // ------------------------------------------------------------ Phase 4: town quests
            Add(new QuestDef
            {
                Id = "bad_blood", Title = "Bad Blood at the Bridge", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale wants the feud on the South Lane settled before somebody gets stung. Hear out Hesper Meadowsweet and Dunstan Burrow.",
                    "You've heard both sides of the fence. Help them talk it out, or pick a side.",
                    "Complete. The fence on the South Lane is quiet. (For now.)" },
                Reward = new QuestReward { Xp = 100, Copper = 1000 },
            });
            Add(new QuestDef
            {
                Id = "pond_ring", Title = "A Ring in the Pond", Giver = "tamsin",
                StageText = new[] {
                    "", "Tamsin dropped a ring in the town pond. Dive for it (Athletics) or fish for it (Survival) from the pond's edge.",
                    "You found the ring! Bring it back to Tamsin.",
                    "Complete. Tamsin finally asked. Lark said yes before the question was even finished." },
                Reward = new QuestReward { Xp = 100, Item = "copper_trinket", Npc = "lark", Friendship = 100, Flag = "lark_tamsin_engaged" },
            });
            Add(new QuestDef
            {
                Id = "secret_poems", Title = "Brannoc's Secret Poems", Giver = "brannoc",
                StageText = new[] {
                    "", "Brannoc asked you to deliver an anonymous poem to Mother Elowen at the temple. Very anonymous. Extremely anonymous.",
                    "Complete. The poem found its reader." },
                Reward = new QuestReward { Xp = 75 },
            });
            Add(new QuestDef
            {
                Id = "pips_barrel", Title = "Pip's Missing Barrel", Giver = "pip",
                StageText = new[] {
                    "", "A barrel of Hearthfire Stout vanished from behind the Sleeping Drake. Look around the back of the tavern.",
                    "Spilled stout and small boot prints lead east, toward the orchard.",
                    "You found the thief: Wick, a teenager selling the stout to pay for his mum's medicine. Settle it with Pip.",
                    "Complete. The Drake's stout is back where it belongs." },
                Reward = new QuestReward { Xp = 100, Npc = "pip", Friendship = 25 },
            });

            // ------------------------------------------------------------ Phase 4: wilds quests
            Add(new QuestDef
            {
                Id = "goats", Title = "Goats Gone Astray", Giver = "tobin",
                StageText = new[] {
                    "", "Three of Old Tobin's goats ate through the fence and wandered off into Whisperwood. Find them and coax them home (Animal Handling).",
                    "All three goats are home. Tell Tobin.",
                    "Complete. The goats are home, and already eating the new fence." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "goat_1", "goat_2", "goat_3") },
                Reward = new QuestReward { Xp = 75, Item = "goat_bell", Npc = "tobin", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "haunted_mill", Title = "The Haunted Mill", Giver = "hobb",
                StageText = new[] {
                    "", "Something wails around Old Hobb's mill at night. Hobb says it's a ghost. Look around the mill yard after dark.",
                    "It's no ghost: it's a lost owlbear cub, hungry and scared. Calm it down (Animal Handling)... and somewhere, its mother is looking for it.",
                    "The cub trusts you. Carry it back to the owlbear den by the lake in Whisperwood.",
                    "Complete. The mill is quiet, and Hobb is pretending he was never scared." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "forest_owlbear", 2, 4), new QuestTrigger(QuestEvent.GroupResolved, "forest_owlbear", 3, 4) },
                Reward = new QuestReward { Xp = 150, Npc = "hobb", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "wolf_bounty", Title = "Wolves on the Road", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale's bounty: wolves in Whisperwood keep scaring travelers. Deal with them however you like.",
                    "The wolves won't trouble anyone now. Collect the bounty from Captain Vale.",
                    "Complete. The road through Whisperwood is safe(r)." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "forest_wolves", 1, 2) },
                Reward = new QuestReward { Xp = 50, Copper = 1500 },
            });
            Add(new QuestDef
            {
                Id = "lost_child", Title = "Lost Child in the Woods", Giver = "maddie",
                StageText = new[] {
                    "", "Maddie hasn't come home! She was last seen heading toward Whisperwood. Find her before dark (8 pm). Look for tracks (Survival or Perception).",
                    "Small boot prints, and a dropped stick sword, lead deeper into Whisperwood.",
                    "Tiny dancing lights... Maddie must be near the fairy ring.",
                    "Complete. Maddie is home safe, full of stories about fairies." },
                Reward = new QuestReward { Xp = 150, Npc = "maddie", Friendship = 200 },
            });
            Add(new QuestDef
            {
                Id = "elowens_herbs", Title = "Mother Elowen's Herbs", Giver = "elowen",
                StageText = new[] {
                    "", "Mother Elowen needs three herbs for her remedies: moonpetal (Whisperwood), riverleaf (Miller's Ford) and hearthroot (the South Lane green).",
                    "You have all three herbs. Bring them to Mother Elowen.",
                    "Complete. The temple's remedy shelf is full again." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "got_herb_moonpetal", "got_herb_riverleaf", "got_herb_hearthroot") },
                Reward = new QuestReward { Xp = 100, Item = "potion_healing", Npc = "elowen", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "bar_brawl", Title = "Bar Brawl at the Drake", Giver = "pip",
                StageText = new[] {
                    "", "Payday at the Sleeping Drake, and the farmhands want a fistfight. Pip's rules: fists only, no magic, nobody dies.",
                    "Complete. Win or lose, the Drake will be talking about it for weeks." },
                Reward = new QuestReward { Xp = 100, Npc = "pip", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "fae_trickster", Title = "The Fae Trickster", Giver = "thimble",
                StageText = new[] {
                    "", "Mister Thimble, a fae with a frog's face and a fox's grin, wants to play a riddle game at the pond in Whisperwood.",
                    "Complete. You played the fae's game. Mister Thimble says the Court is waking up. He wouldn't say what that means." },
                Reward = new QuestReward { Xp = 100 },
            });
        }
    }
}
