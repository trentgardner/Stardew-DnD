using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    // All positions in this file are MAP coordinates: (column, row) exactly as they appear in Maps.cs,
    // with row 0 at the top. Zone converts them to world tiles.

    public class WarpDef
    {
        public Vector2Int At;
        public string ToZone;
        public Vector2Int ToPos;
        public WarpDef(int x, int y, string zone, int tx, int ty) { At = new Vector2Int(x, y); ToZone = zone; ToPos = new Vector2Int(tx, ty); }
    }

    public class EnemySpawn
    {
        public string MonsterId;
        public Vector2Int Pos;
        public EnemySpawn(string id, int x, int y) { MonsterId = id; Pos = new Vector2Int(x, y); }
    }

    /// <summary>What you can try when a group spots you: talk to people, calm animals, or (None) only fight or run.</summary>
    public enum EncounterStyle { Talk, Beasts, None }

    public class EnemyGroupDef
    {
        public string Id;
        public List<EnemySpawn> Members = new List<EnemySpawn>();
        /// <summary>If set, noticing the party opens this story dialogue (once) instead of the generic encounter choice.</summary>
        public string ParleyDialogue;
        public int SightTiles = 6;
        public EncounterStyle Style = EncounterStyle.Talk;
        /// <summary>Persuasion (people) or Animal Handling (beasts) DC to end it peacefully; Intimidation DC to scare them off.</summary>
        public int TalkDC = 13, ScareDC = 13;
        /// <summary>"the goblins": used in the encounter text.</summary>
        public string Label = "the creatures";
        /// <summary>The DM's description when they spot you, and when you talk them down.</summary>
        public string Spotted, Befriended;
        /// <summary>Story groups never come back once resolved (everything else respawns after a few days).</summary>
        public bool Unique;
    }

    public class ChestDef
    {
        public string Id;
        public Vector2Int Pos;
        public List<KeyValuePair<string, int>> Items = new List<KeyValuePair<string, int>>();
        public int Copper;
        public bool IsStorage;   // the player's home chest (persistent storage, not loot)
    }

    /// <summary>
    /// A hidden or special spot found with a skill check (Zelda-style secrets). Found once, unless Daily.
    /// A failed search can be retried the next day (D&D: try again when circumstances change).
    /// </summary>
    public class SecretDef
    {
        public string Id;
        public string Sprite;            // prop drawn on the tile
        public string Prompt;            // "Search the hollow log"
        public Rules.Skill Skill;
        public int DC;
        public string Success, Failure, AlreadyFound;
        public List<KeyValuePair<string, int>> Items = new List<KeyValuePair<string, int>>();
        public int Copper;
        public bool Daily;               // can be done once per day (e.g. the fairy ring)
        public bool HeroicInspiration;   // grants Heroic Inspiration on success
        public string FirstTimeItem;     // extra item only on the very first success
        public string FirstTimeText;
    }

    public class ZoneDef
    {
        public string Id, Name;
        public string[] Map;
        public bool Dark;
        public bool Indoors;
        public bool SafeForLongRest;
        public string EnterNarration;
        public Vector2Int DefaultSpawn;
        public List<WarpDef> Warps = new List<WarpDef>();
        public List<EnemyGroupDef> EnemyGroups = new List<EnemyGroupDef>();
        public List<ChestDef> Chests = new List<ChestDef>();
        public Dictionary<Vector2Int, string> Signs = new Dictionary<Vector2Int, string>();
        public Dictionary<Vector2Int, SecretDef> Secrets = new Dictionary<Vector2Int, SecretDef>();
        public Vector2Int? EggPos;
        public Vector2Int? BedPos;
        public Vector2Int? ShopCounter;

        public int Width => Map[0].Length;
        public int Height => Map.Length;
    }

    public class ScheduleEntry
    {
        public int FromHour;
        public string Zone;      // null = away (asleep, out of town)
        public Vector2Int Pos;
        public string Activity;  // flavor for the narrator ("tending the bar")
        public int OnlyWeekday = -1;
        public ScheduleEntry(int hour, string zone, int x, int y, string activity, int weekday = -1)
        { FromHour = hour; Zone = zone; Pos = new Vector2Int(x, y); Activity = activity; OnlyWeekday = weekday; }
    }

    public class NpcDef
    {
        public string Id, Name, Look, Title;
        public string Dialogue;
        public string ShopId;
        public List<ScheduleEntry> Schedule = new List<ScheduleEntry>();
        public bool Romanceable;
    }

    public static class WorldDb
    {
        public static readonly Dictionary<string, ZoneDef> Zones = new Dictionary<string, ZoneDef>();
        public static readonly Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();

        public static readonly string[] Weekdays = { "Dawnday", "Tideday", "Emberday", "Stormday", "Leafday", "Coinday", "Hearthday" };

        static WorldDb()
        {
            var town = new ZoneDef
            {
                Id = "town", Name = "Thistledown", Map = Maps.Town, DefaultSpawn = new Vector2Int(25, 19),
                EnterNarration = "Thistledown. Smoke curls from chimneys, the river chatters under the old bridge, and to the north the Wyrmback Mountains loom like sleeping giants.",
            };
            town.Warps.Add(new WarpDef(14, 14, "shop", 6, 7));
            town.Warps.Add(new WarpDef(32, 10, "tavern", 8, 9));
            town.Warps.Add(new WarpDef(7, 9, "temple", 5, 8));
            town.Warps.Add(new WarpDef(8, 28, "home", 5, 6));
            town.Warps.Add(new WarpDef(25, 4, "mine", 19, 27));
            town.Warps.Add(new WarpDef(26, 4, "mine", 20, 27));
            town.Warps.Add(new WarpDef(0, 18, "forest", 44, 18));
            town.Warps.Add(new WarpDef(0, 19, "forest", 44, 19));
            town.Warps.Add(new WarpDef(49, 18, "ford", 1, 14));
            town.Warps.Add(new WarpDef(49, 19, "ford", 1, 15));
            town.Signs[new Vector2Int(27, 5)] = "DANGER: OLD COPPER MINE. CLOSED BY ORDER OF THE WATCH.\n\nSomeone has drawn a small, very proud dragon on the sign in charcoal.";
            town.Signs[new Vector2Int(19, 14)] = "THISTLEDOWN NOTICE BOARD\n\nMISSING: 4 chickens, 1 copper kettle, Old Tobin's lucky spoon.\nReport suspicious small scaly persons to Captain Vale.\n\nAlso: did ANYONE else see the falling star last night?";
            town.Signs[new Vector2Int(25, 16)] = "A weathered stone dragon curls around the top of a pillar, wings half-open, one eye closed as if winking.\n\nThe plaque reads: \"EMBERWING, WHO SAT WITH US A WHILE.\"\n\nThe village kids rub its nose for luck. The nose is very shiny.";
            town.Signs[new Vector2Int(2, 16)] = "WEST: WHISPERWOOD\n\nStay on the path. Don't dance in the mushroom rings.\n(Or DO. We're a sign, not your mother.)";
            town.Signs[new Vector2Int(46, 16)] = "EAST ROAD: MILLER'S FORD, then PORT VARREL, 3 DAYS\n\nBridge out at Miller's Ford. Travelers report 'toll-keepers' at the ford upstream.\n\nThe orchard is open. The apples are not free. - Tobin";
            town.Secrets[new Vector2Int(21, 21)] = new SecretDef
            {
                Id = "town_well", Sprite = "obj_well", Prompt = "Peer into the old well", Skill = Rules.Skill.Investigation, DC = 14,
                Success = "Halfway down, a loose brick. Behind it, wrapped in oilcloth: a copper coin with a laughing face on both sides. Somebody hid their luck here and never came back for it.",
                Failure = "Dark water, a few wishing coppers, your own reflection looking unimpressed. Maybe with better light tomorrow.",
                AlreadyFound = "Just water and wishes now.",
                Items = { new KeyValuePair<string, int>("fennicks_lucky_copper", 1) },
            };
            town.Secrets[new Vector2Int(33, 33)] = new SecretDef
            {
                Id = "town_stump", Sprite = "obj_stump", Prompt = "Look at the hollow stump", Skill = Rules.Skill.Perception, DC = 11,
                Success = "Tucked inside: a tin box. Three marbles, a drawing of a dragon wearing a crown, and a note: \"MADDIES TREZHUR. KEEP OWT. THIS MEENS YOU.\" You put it back exactly how you found it. Mostly.",
                Failure = "Just a stump. A very ordinary stump. Suspiciously ordinary.",
                AlreadyFound = "Maddie's treasure is safe. You saw nothing.",
            };
            Zones[town.Id] = town;

            var forest = new ZoneDef
            {
                Id = "forest", Name = "Whisperwood", Map = Maps.Forest, DefaultSpawn = new Vector2Int(44, 18),
                EnterNarration = "Whisperwood. The trees lean in close, the light goes green and gold, and somewhere ahead you hear a stream... and, faintly, someone giggling.",
            };
            forest.Warps.Add(new WarpDef(45, 18, "town", 1, 18));
            forest.Warps.Add(new WarpDef(45, 19, "town", 1, 19));
            forest.Signs[new Vector2Int(43, 16)] = "WHISPERWOOD\n\nWOLVES: north-east. GOBLINS: south-west, probably.\nFAIRIES: everywhere, technically.\n\nCarved underneath, smaller: \"the old shrine is north. bring a friend.\"";
            forest.Signs[new Vector2Int(29, 5)] = "A moss-covered standing stone in the middle of the ruins. Faded carvings show a great dragon sheltering a village under its wings while stars fall around them.\n\nThe flagstones nearby look like people once knelt here. One of them sits a little crooked.";
            forest.Secrets[new Vector2Int(10, 18)] = new SecretDef
            {
                Id = "fairy_ring", Sprite = "obj_fae_ring", Prompt = "Dance in the fairy ring", Skill = Rules.Skill.Performance, DC = 13, Daily = true, HeroicInspiration = true,
                Success = "You dance. Tiny lights rise out of the mushrooms and dance WITH you, laughing like little bells. You leave feeling ridiculous and wonderful. (Heroic Inspiration!)",
                Failure = "You dance. The mushrooms are deeply unimpressed. Try again tomorrow, maybe with more feeling.",
                AlreadyFound = "The ring is quiet. The fairies have had enough dancing for today.",
                FirstTimeItem = "veilthread_cloak",
                FirstTimeText = "When the music stops, a cloak lies folded in the middle of the ring, shimmering between colors. A tiny voice: \"For the good dancer. Don't tell the Court.\"",
            };
            forest.Secrets[new Vector2Int(20, 24)] = new SecretDef
            {
                Id = "forest_log", Sprite = "obj_log", Prompt = "Search the hollow log", Skill = Rules.Skill.Perception, DC = 12,
                Success = "Something glints in the rot: a traveler's pouch, long forgotten. Two healing potions, still sealed, and a few coins.",
                Failure = "Beetles. So many beetles. If there's anything else in there, you'll look again tomorrow.",
                AlreadyFound = "Just beetles now. They've redecorated.",
                Items = { new KeyValuePair<string, int>("potion_healing", 2) }, Copper = 600,
            };
            forest.Secrets[new Vector2Int(32, 4)] = new SecretDef
            {
                Id = "shrine_stone", Sprite = "obj_flagstone", Prompt = "Examine the crooked flagstone", Skill = Rules.Skill.Investigation, DC = 14,
                Success = "You lever the stone up. Underneath, in a little hollow lined with dried flowers: a slim wand tipped with a fleck of fallen star, and a note in neat handwriting. \"If found, return to the Starfall Academy. Or keep it. I failed the exam anyway.\"",
                Failure = "It's crooked, but it won't budge, and you can't see why. Maybe it needs fresh eyes tomorrow.",
                AlreadyFound = "The hollow under the stone is empty, apart from the dried flowers.",
                Items = { new KeyValuePair<string, int>("starfall_wand", 1) },
            };
            var goblins = new EnemyGroupDef { Id = "forest_goblins", SightTiles = 6, Label = "the goblins", TalkDC = 13, ScareDC = 12,
                Spotted = "Three goblins look up from a bubbling cookpot. The biggest one points a ladle at you. \"OI. That's OUR clearing. Toll's... uh... everything you got.\" Their hands drift toward their bows, but nobody's drawn yet.",
                Befriended = "The goblins huddle, mutter, and decide you're more trouble than you're worth. The big one shrugs and offers you a bowl of something brown. It's... actually not bad." };
            goblins.Members.Add(new EnemySpawn("goblin", 9, 27));
            goblins.Members.Add(new EnemySpawn("goblin", 13, 29));
            goblins.Members.Add(new EnemySpawn("goblin", 10, 30));
            forest.EnemyGroups.Add(goblins);
            var wolves = new EnemyGroupDef { Id = "forest_wolves", SightTiles = 7, Style = EncounterStyle.Beasts, Label = "the wolves", TalkDC = 13, ScareDC = 12,
                Spotted = "Two grey wolves rise from the ferns, hackles up, lips peeled back. They're thin. Hungry. But they haven't lunged... yet.",
                Befriended = "The lead wolf's ears come forward. It sniffs your hand, huffs, and trots off into the trees with its packmate." };
            wolves.Members.Add(new EnemySpawn("wolf", 36, 6));
            wolves.Members.Add(new EnemySpawn("wolf", 39, 7));
            forest.EnemyGroups.Add(wolves);
            var stash = new ChestDef { Id = "goblin_stash", Pos = new Vector2Int(7, 30), Copper = 1200 };
            stash.Items.Add(new KeyValuePair<string, int>("rations", 3));
            stash.Items.Add(new KeyValuePair<string, int>("gemstone", 1));
            stash.Items.Add(new KeyValuePair<string, int>("torch", 2));
            forest.Chests.Add(stash);
            Zones[forest.Id] = forest;

            BuildFord();

            var shop = new ZoneDef { Id = "shop", Name = "Ironkettle General Goods", Map = Maps.Shop, Indoors = true, DefaultSpawn = new Vector2Int(6, 7),
                EnterNarration = "The shop smells of lamp oil and leather. Every shelf is organized with terrifying dwarven precision.", ShopCounter = new Vector2Int(6, 3) };
            shop.Warps.Add(new WarpDef(6, 8, "town", 14, 15));
            Zones[shop.Id] = shop;

            var tavern = new ZoneDef { Id = "tavern", Name = "The Sleeping Drake", Map = Maps.Tavern, Indoors = true, DefaultSpawn = new Vector2Int(8, 9), SafeForLongRest = true,
                EnterNarration = "The Sleeping Drake is warm, loud and smells like stew. Above the hearth hangs a carved wooden dragon, asleep, with a little nightcap." };
            tavern.Warps.Add(new WarpDef(8, 10, "town", 32, 11));
            Zones[tavern.Id] = tavern;

            var temple = new ZoneDef { Id = "temple", Name = "Temple of the Dawnmother", Map = Maps.Temple, Indoors = true, DefaultSpawn = new Vector2Int(5, 7), SafeForLongRest = true,
                EnterNarration = "Morning light pours through tall windows onto a golden sunburst. It is very quiet, and very peaceful." };
            temple.Warps.Add(new WarpDef(5, 9, "town", 7, 10));
            temple.Warps.Add(new WarpDef(6, 9, "town", 7, 10));
            Zones[temple.Id] = temple;

            var home = new ZoneDef { Id = "home", Name = "Your Cottage", Map = Maps.Home, Indoors = true, DefaultSpawn = new Vector2Int(5, 5), SafeForLongRest = true,
                BedPos = new Vector2Int(2, 2), EnterNarration = "Home. Small, a little drafty, and entirely yours." };
            home.Warps.Add(new WarpDef(5, 7, "town", 8, 29));
            home.Chests.Add(new ChestDef { Id = "home_storage", Pos = new Vector2Int(7, 2), IsStorage = true });
            Zones[home.Id] = home;

            var mine = new ZoneDef { Id = "mine", Name = "The Old Copper Mine", Map = Maps.Mine, Dark = true, DefaultSpawn = new Vector2Int(19, 27),
                EnterNarration = "The air is cold and smells of wet stone... and, faintly, of woodsmoke. Somewhere deep below, many small voices are chanting." };
            mine.Warps.Add(new WarpDef(19, 28, "town", 25, 5));
            mine.Warps.Add(new WarpDef(20, 28, "town", 26, 5));
            mine.EggPos = new Vector2Int(19, 2);

            var g1 = new EnemyGroupDef { Id = "mine_hub", Label = "the kobolds", TalkDC = 12, ScareDC = 12,
                Spotted = "Two kobolds on guard duty leap up, spears wobbling. \"HALT! Nobody passes! Snik's orders! ...Who are you? Are you important?\"",
                Befriended = "The kobolds confer in rapid squeaks and decide you are Very Important. They wave you through and offer you a shiny pebble each." };
            g1.Members.Add(new EnemySpawn("kobold", 15, 18));
            g1.Members.Add(new EnemySpawn("kobold", 24, 19));
            mine.EnemyGroups.Add(g1);
            var g2 = new EnemyGroupDef { Id = "mine_rats", SightTiles = 5, Style = EncounterStyle.Beasts, Label = "the giant rats", TalkDC = 12, ScareDC = 10,
                Spotted = "Red eyes in the dark. Three giant rats, each the size of a dog, squeal and bunch up to pounce.",
                Befriended = "The rats sniff the air, decide you're not food, and scurry off into a crack in the wall." };
            g2.Members.Add(new EnemySpawn("giant_rat", 4, 12));
            g2.Members.Add(new EnemySpawn("giant_rat", 6, 14));
            g2.Members.Add(new EnemySpawn("giant_rat", 7, 10));
            mine.EnemyGroups.Add(g2);
            var g3 = new EnemyGroupDef { Id = "mine_east", Label = "the kobold slingers", TalkDC = 13, ScareDC = 13,
                Spotted = "\"INTRUDERS!\" Slings start whirling. A kobold hops up on a crate: \"This is the TREASURE room! Treasure room is for KOBOLDS!\"",
                Befriended = "The slingers lower their slings, a little embarrassed. \"...You can look. But no touching the good rocks.\"" };
            g3.Members.Add(new EnemySpawn("kobold_slinger", 35, 15));
            g3.Members.Add(new EnemySpawn("kobold_slinger", 36, 12));
            g3.Members.Add(new EnemySpawn("kobold", 33, 17));
            mine.EnemyGroups.Add(g3);
            var g4 = new EnemyGroupDef { Id = "mine_egg", ParleyDialogue = "snik", SightTiles = 7, Unique = true, Label = "Snik and the egg-guards", TalkDC = 13, ScareDC = 15 };
            g4.Members.Add(new EnemySpawn("snik", 19, 5));
            g4.Members.Add(new EnemySpawn("kobold", 16, 6));
            g4.Members.Add(new EnemySpawn("kobold", 23, 6));
            mine.EnemyGroups.Add(g4);

            var c1 = new ChestDef { Id = "mine_nook", Pos = new Vector2Int(4, 3), Copper = 1500 };
            c1.Items.Add(new KeyValuePair<string, int>("oathbound_signet", 1));
            c1.Items.Add(new KeyValuePair<string, int>("potion_healing", 1));
            mine.Chests.Add(c1);
            var c2 = new ChestDef { Id = "mine_east_chest", Pos = new Vector2Int(37, 11), Copper = 800 };
            c2.Items.Add(new KeyValuePair<string, int>("potion_healing", 2));
            c2.Items.Add(new KeyValuePair<string, int>("copper_trinket", 3));
            c2.Items.Add(new KeyValuePair<string, int>("torch", 2));
            c2.Items.Add(new KeyValuePair<string, int>("lucky_spoon", 1));
            mine.Chests.Add(c2);
            Zones[mine.Id] = mine;

            BuildNpcs();
        }

        /// <summary>Miller's Ford, east of town: a broken bridge, a "toll" crew at the ford, a mill, a boar meadow.</summary>
        static void BuildFord()
        {
            var ford = new ZoneDef
            {
                Id = "ford", Name = "Miller's Ford", Map = Maps.Ford, DefaultSpawn = new Vector2Int(1, 14),
                EnterNarration = "The east road runs down to the river... and stops. Half a stone bridge juts over the water like a broken tooth. Upstream, you hear voices and smell a cookfire. Across the river, a mill wheel sits still.",
            };
            ford.Warps.Add(new WarpDef(0, 14, "town", 48, 18));
            ford.Warps.Add(new WarpDef(0, 15, "town", 48, 19));
            ford.Signs[new Vector2Int(3, 13)] = "MILLER'S FORD\n\nWEST: Thistledown\nEAST: Port Varrel (3 days)\n\nScratched underneath: \"bridge is OUT. use the ford. mind the wardens.\"";
            ford.Signs[new Vector2Int(25, 13)] = "The bridge is broken clean through: the spring flood took the middle span. Upstream there's a shallow ford, and smoke from a camp beside it.";
            ford.Signs[new Vector2Int(22, 7)] = "FORD WARDENS\nOFFICIAL TOLL CROSSING\n\n10 silver per party. Very official. Do not ask by whose authority.";
            ford.Signs[new Vector2Int(42, 13)] = "EAST ROAD\n\nA fallen oak blocks the way to Port Varrel. It's going to take more than one afternoon to clear. (The road continues in a future update.)";
            ford.Secrets[new Vector2Int(6, 5)] = new SecretDef
            {
                Id = "ford_waystone", Sprite = "obj_flagstone", Prompt = "Read the old waystone", Skill = Rules.Skill.History, DC = 13,
                Success = "The carvings list towns that haven't existed for centuries. One line is newer, scratched by hand: \"for whoever needs it more.\" Behind a loose chunk at the base: a wrapped gem and a sealed potion.",
                Failure = "Old letters, worn smooth. You can make out the word 'Dragon' and not much else. Maybe tomorrow, in better light.",
                AlreadyFound = "The waystone keeps its old names. You keep its secret.",
                Items = { new KeyValuePair<string, int>("gemstone", 1), new KeyValuePair<string, int>("potion_greater_healing", 1) },
            };
            ford.Secrets[new Vector2Int(33, 21)] = new SecretDef
            {
                Id = "ford_flour", Sprite = "obj_crate", Prompt = "Check the flour sacks", Skill = Rules.Skill.Investigation, DC = 11,
                Success = "One sack is lumpier than the others. Inside, among the flour: Hobb's rainy-day purse. (He'll pretend he doesn't know. He's very bad at pretending.)",
                Failure = "Flour. Flour everywhere. On you, mostly.",
                AlreadyFound = "Just flour now.",
                Copper = 800,
            };
            ford.Secrets[new Vector2Int(30, 24)] = new SecretDef
            {
                Id = "ford_fishing", Sprite = "obj_log", Prompt = "Try to tickle a trout", Skill = Rules.Skill.Survival, DC = 12, Daily = true,
                Success = "Slow hands, cold water, patience... and a fat river trout flops onto the bank. Dinner! (You wrap it up as trail rations.)",
                Failure = "The trout looks at you. You look at the trout. The trout leaves. Try again tomorrow.",
                AlreadyFound = "The fish are wise to you today.",
                Items = { new KeyValuePair<string, int>("rations", 2) },
            };
            var toll = new EnemyGroupDef { Id = "ford_toll", ParleyDialogue = "wren", SightTiles = 7, Unique = true, Label = "the Ford Wardens", TalkDC = 14, ScareDC = 14,
                Befriended = "Wren sighs and lowers her boathook. \"Fine. FINE. Go on through.\"" };
            toll.Members.Add(new EnemySpawn("wren", 19, 5));
            toll.Members.Add(new EnemySpawn("bandit", 24, 4));
            toll.Members.Add(new EnemySpawn("bandit", 24, 7));
            ford.EnemyGroups.Add(toll);
            var boars = new EnemyGroupDef { Id = "ford_boars", SightTiles = 5, Style = EncounterStyle.Beasts, Label = "the boars", TalkDC = 12, ScareDC = 11,
                Spotted = "Two boars burst out of the flowers, snorting, tusks low, pawing the ground. It's THEIR meadow.",
                Befriended = "The boars snuffle at you, decide you smell boring, and go back to rooting for truffles." };
            boars.Members.Add(new EnemySpawn("boar", 10, 22));
            boars.Members.Add(new EnemySpawn("boar", 14, 24));
            ford.EnemyGroups.Add(boars);
            var box = new ChestDef { Id = "toll_box", Pos = new Vector2Int(15, 2), Copper = 1500 };
            box.Items.Add(new KeyValuePair<string, int>("potion_healing", 1));
            box.Items.Add(new KeyValuePair<string, int>("rations", 2));
            ford.Chests.Add(box);
            Zones[ford.Id] = ford;
        }

        static void BuildNpcs()
        {
            var brannoc = new NpcDef { Id = "brannoc", Name = "Brannoc Ironkettle", Title = "Shopkeeper", Look = "npc_brannoc", Dialogue = "brannoc", ShopId = "general" };
            brannoc.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            brannoc.Schedule.Add(new ScheduleEntry(8, "shop", 6, 2, "minding the counter"));
            brannoc.Schedule.Add(new ScheduleEntry(18, "tavern", 4, 5, "nursing an ale"));
            brannoc.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            brannoc.Schedule.Add(new ScheduleEntry(8, "tavern", 11, 5, "enjoying his day off", 6)); // Hearthday: shop closed
            Npcs[brannoc.Id] = brannoc;

            var elowen = new NpcDef { Id = "elowen", Name = "Mother Elowen", Title = "Priestess of the Dawnmother", Look = "npc_elowen", Dialogue = "elowen", ShopId = "temple" };
            elowen.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            elowen.Schedule.Add(new ScheduleEntry(6, "temple", 6, 2, "tending the altar"));
            elowen.Schedule.Add(new ScheduleEntry(17, "town", 5, 11, "tending the temple flowers"));
            elowen.Schedule.Add(new ScheduleEntry(20, "temple", 6, 2, "at evening prayers"));
            elowen.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            Npcs[elowen.Id] = elowen;

            var pip = new NpcDef { Id = "pip", Name = "Pip Thistlewhit", Title = "Keeper of the Sleeping Drake", Look = "npc_pip", Dialogue = "pip", ShopId = "tavern" };
            pip.Schedule.Add(new ScheduleEntry(0, "tavern", 4, 1, "wiping down the bar"));
            pip.Schedule.Add(new ScheduleEntry(2, null, 0, 0, "asleep"));
            pip.Schedule.Add(new ScheduleEntry(9, "tavern", 4, 1, "behind the bar"));
            Npcs[pip.Id] = pip;

            var rhoswen = new NpcDef { Id = "rhoswen", Name = "Captain Rhoswen Vale", Title = "Captain of the Watch", Look = "npc_rhoswen", Dialogue = "rhoswen", Romanceable = true };
            rhoswen.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            rhoswen.Schedule.Add(new ScheduleEntry(7, "town", 20, 15, "frowning at the notice board"));
            rhoswen.Schedule.Add(new ScheduleEntry(12, "tavern", 11, 8, "eating lunch standing up"));
            rhoswen.Schedule.Add(new ScheduleEntry(13, "town", 24, 6, "watching the mine road"));
            rhoswen.Schedule.Add(new ScheduleEntry(19, "tavern", 10, 7, "off duty (supposedly)"));
            rhoswen.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            Npcs[rhoswen.Id] = rhoswen;

            var tobin = new NpcDef { Id = "tobin", Name = "Old Tobin", Title = "Farmer", Look = "npc_villager_tobin", Dialogue = "tobin" };
            tobin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            tobin.Schedule.Add(new ScheduleEntry(6, "town", 18, 25, "fussing over his fence"));
            tobin.Schedule.Add(new ScheduleEntry(12, "town", 12, 19, "looking for his spoon"));
            tobin.Schedule.Add(new ScheduleEntry(18, "tavern", 12, 6, "telling the same story again"));
            tobin.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            Npcs[tobin.Id] = tobin;

            var maddie = new NpcDef { Id = "maddie", Name = "Maddie", Title = "Village kid", Look = "npc_villager_maddie", Dialogue = "maddie" };
            maddie.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            maddie.Schedule.Add(new ScheduleEntry(8, "town", 32, 31, "skipping stones at the pond"));
            maddie.Schedule.Add(new ScheduleEntry(14, "town", 24, 18, "playing 'dragon' with a stick"));
            maddie.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "home for supper"));
            Npcs[maddie.Id] = maddie;

            var hobb = new NpcDef { Id = "hobb", Name = "Old Hobb", Title = "Miller", Look = "npc_hobb", Dialogue = "hobb" };
            hobb.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            hobb.Schedule.Add(new ScheduleEntry(6, "ford", 37, 20, "glaring at the idle mill wheel"));
            hobb.Schedule.Add(new ScheduleEntry(12, "ford", 40, 25, "weeding the garden"));
            hobb.Schedule.Add(new ScheduleEntry(18, "ford", 36, 20, "sitting on the step with a pipe"));
            hobb.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            Npcs[hobb.Id] = hobb;
        }

        /// <summary>Where an NPC should be at a given hour and weekday.</summary>
        public static ScheduleEntry Current(NpcDef npc, int hour, int weekday)
        {
            ScheduleEntry best = null;
            // Weekday-specific entries override generic ones from their hour until the next entry of either kind.
            foreach (var e in npc.Schedule)
            {
                if (e.OnlyWeekday >= 0 && e.OnlyWeekday != weekday) continue;
                if (e.FromHour > hour) continue;
                if (best == null || e.FromHour > best.FromHour || (e.FromHour == best.FromHour && e.OnlyWeekday >= 0)) best = e;
            }
            return best;
        }
    }
}
