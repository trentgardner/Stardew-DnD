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
        /// <summary>Quest-gated group: only around while QuestId's stage is within MinStage..MaxStage (QuestId null = always).</summary>
        public string QuestId;
        public int MinStage, MaxStage;
        /// <summary>A nonlethal fistfight: no magic, no weapons, no items, nobody dies (knocked out instead), no party wipe.</summary>
        public bool Brawl;
        /// <summary>If this world flag is set when they spot you, a friend vouched for you: they stand down (full XP) and VouchText is narrated.</summary>
        public string VouchFlag, VouchText;
        public bool InStage(QuestLog q) => QuestId == null || (q.Stage(QuestId) >= MinStage && q.Stage(QuestId) <= MaxStage);
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
        /// <summary>Quest spot: only present while QuestId is at QuestStage. Success sets SetsFlag and moves the quest to AdvanceTo.</summary>
        public string QuestId;
        public int QuestStage = -1;
        public int AdvanceTo = -1;
        public string SetsFlag;
        public bool IsQuestSpot => QuestId != null;
        /// <summary>Only there after dark (20:00 to 05:00).</summary>
        public bool NightOnly;
        /// <summary>Success settles this enemy group peacefully (full XP, as if talked down), e.g. returning a lost cub.</summary>
        public string PacifiesGroup;
        /// <summary>A sneak route: success moves the player (and their companions) to this tile in the same zone. Once found, no roll again.</summary>
        public Vector2Int? PassageTo;
        /// <summary>Success opens this conversation instead of the result message (e.g. catching a thief in the act).</summary>
        public string Dialogue;
    }

    /// <summary>The sign hanging beside a building's door: the prompt names the building, reading it gives the text.</summary>
    public class BuildingSign
    {
        public string Name, Text;
        public BuildingSign(string name, string text) { Name = name; Text = text; }
    }

    public class ZoneDef
    {
        public string Id, Name;
        public string[] Map;
        public bool Dark;
        public bool Indoors;
        public bool SafeForLongRest;
        public string EnterNarration;
        /// <summary>Music track id; defaults to "music_{Id}" (see Assets/Editor/ArtMap.txt audio lines).</summary>
        public string Music;
        public string MusicId => Music ?? "music_" + Id;
        public Vector2Int DefaultSpawn;
        public List<WarpDef> Warps = new List<WarpDef>();
        public List<EnemyGroupDef> EnemyGroups = new List<EnemyGroupDef>();
        public List<ChestDef> Chests = new List<ChestDef>();
        public Dictionary<Vector2Int, string> Signs = new Dictionary<Vector2Int, string>();
        public Dictionary<Vector2Int, SecretDef> Secrets = new Dictionary<Vector2Int, SecretDef>();
        /// <summary>Keyed by the DOOR tile; the sign hangs on the wall just right of the door.</summary>
        public Dictionary<Vector2Int, BuildingSign> DoorSigns = new Dictionary<Vector2Int, BuildingSign>();
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
        /// <summary>Story conditions: only while a world flag is set / not set, or while a timed lock runs (e.g. a night in the cell).</summary>
        public string RequiresFlag, ForbidFlag, RequiresLock;
        public bool Conditional => RequiresFlag != null || ForbidFlag != null || RequiresLock != null;
        public bool Applies(WorldState w) =>
            (RequiresFlag == null || (w != null && w.Flag(RequiresFlag))) &&
            (ForbidFlag == null || w == null || !w.Flag(ForbidFlag)) &&
            (RequiresLock == null || (w != null && w.IsLocked(RequiresLock)));
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

    public static partial class WorldDb
    {
        public static readonly Dictionary<string, ZoneDef> Zones = new Dictionary<string, ZoneDef>();
        public static readonly Dictionary<string, NpcDef> Npcs = new Dictionary<string, NpcDef>();

        public static readonly string[] Weekdays = { "Dawnday", "Tideday", "Emberday", "Stormday", "Leafday", "Coinday", "Hearthday" };
        public const int Dawnday = 0, Tideday = 1, Emberday = 2, Stormday = 3, Leafday = 4, Coinday = 5, Hearthday = 6;

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
            town.Warps.Add(new WarpDef(6, 40, "forge", 6, 7));
            town.Warps.Add(new WarpDef(27, 40, "watchhouse", 5, 6));
            town.Warps.Add(new WarpDef(4, 49, "cottage_lark", 4, 5));
            town.DoorSigns[new Vector2Int(7, 9)] = new BuildingSign("Temple of the Dawnmother", "TEMPLE OF THE DAWNMOTHER\n\nAll are welcome. Healing, blessings and second chances, by donation.\nHearthday service at nine bells.");
            town.DoorSigns[new Vector2Int(32, 10)] = new BuildingSign("The Sleeping Drake", "THE SLEEPING DRAKE\nInn & Tavern\n\nRooms, stew, and the best cider in the valley. Story night every Emberday.\nNo dragons on the furniture (sleeping or otherwise).");
            town.DoorSigns[new Vector2Int(14, 14)] = new BuildingSign("Ironkettle General Goods", "IRONKETTLE GENERAL GOODS\n\nOpen eight bells to six. Closed Hearthdays.\nPrices are prices.");
            town.DoorSigns[new Vector2Int(8, 28)] = new BuildingSign("Your Cottage", "HOME\n\nThe little sign is new. Someone (Maddie) has painted your name on it. It's spelled wrong, and there's a dragon in the corner.");
            town.DoorSigns[new Vector2Int(29, 29)] = new BuildingSign("Maddie's House", "MADDIES HOWSE\n(and mum and dad)\n\nKNOCK LOUD. DRAGONS WELCOME.");
            town.DoorSigns[new Vector2Int(6, 40)] = new BuildingSign("Ashforge Smithy", "ASHFORGE SMITHY\n\nWeapons, armor, horseshoes, hinges.\nIf it's iron, Garrick can make it. If it's broken, Garrick can fix it. If it's a kettle, see Brannoc.");
            town.DoorSigns[new Vector2Int(27, 40)] = new BuildingSign("The Watch House", "THE WATCH HOUSE\n\nReport crimes, lost things and suspicious small scaly persons here.");
            town.DoorSigns[new Vector2Int(34, 41)] = new BuildingSign("Meadowsweet Cottage", "MEADOWSWEET COTTAGE\n\nHoney for sale.\nNOT to Burrows.");
            town.DoorSigns[new Vector2Int(45, 41)] = new BuildingSign("Burrow Cottage", "BURROW COTTAGE\n\nHoney for sale. BETTER honey. Ask anyone.\n(Not Hesper.)");
            town.DoorSigns[new Vector2Int(4, 49)] = new BuildingSign("Lark's Cottage", "LARK'S FLOWERS\n\nA bunch for every feeling. Knock gently: the jars rattle.");
            town.Signs[new Vector2Int(29, 41)] = "THISTLEDOWN WATCH\n\nCaptain R. Vale, commanding.\nOffice hours: whenever the Captain is not out doing something more important.\n\nBelow, in a child's handwriting: \"and maddie (deputy)\"";
            town.Signs[new Vector2Int(12, 45)] = "SOUTH LANE GREEN\n\nPlease do not feed the ducks bread. Or pie. Or Mrs Meadowsweet's honey cake. They have had ENOUGH, Dunstan.";
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
            town.Secrets[new Vector2Int(44, 27)] = new SecretDef
            {
                Id = "pond_ring_dive", Sprite = "item_ring", Prompt = "Dive for the ring", Skill = Rules.Skill.Athletics, DC = 13,
                QuestId = "pond_ring", QuestStage = 1, AdvanceTo = 2,
                Success = "You take a breath and go under. Cold, green, weedy... and there: a glint in the mud. You come up gasping with a silver ring in your fist.",
                Failure = "You surface with a mouthful of pondweed and a very offended duck. Try again tomorrow, when the water settles.",
                AlreadyFound = "Just ducks now.",
                Items = { new KeyValuePair<string, int>("pond_ring", 1) },
            };
            town.Secrets[new Vector2Int(32, 30)] = new SecretDef
            {
                Id = "pond_ring_fish", Sprite = "item_ring", Prompt = "Fish for the ring with a bent hook", Skill = Rules.Skill.Survival, DC = 12,
                QuestId = "pond_ring", QuestStage = 1, AdvanceTo = 2,
                Success = "Patience, a bent hook and a steady wrist... the line snags, you ease it up, and a silver ring dangles from the hook, dripping.",
                Failure = "You catch: one boot, two weeds, zero rings. The pond keeps its secret today.",
                AlreadyFound = "The pond is just a pond again.",
                Items = { new KeyValuePair<string, int>("pond_ring", 1) },
            };
            town.Secrets[new Vector2Int(25, 47)] = new SecretDef
            {
                Id = "spot_herb_hearthroot", Sprite = "obj_herb", Prompt = "Dig up the hearthroot", Skill = Rules.Skill.Medicine, DC = 12,
                QuestId = "elowens_herbs", QuestStage = 1, SetsFlag = "got_herb_hearthroot",
                Success = "You dig carefully around the knobbly root and lift it out whole. It's warm, like a stone left in the sun.",
                Failure = "The root snaps. Useless. There's another one sprouting nearby; it'll be ready tomorrow.",
                AlreadyFound = "Just a hole, already filling with clover.",
                Items = { new KeyValuePair<string, int>("herb_hearthroot", 1) },
            };
            town.Secrets[new Vector2Int(31, 5)] = new SecretDef
            {
                Id = "barrel_tracks", Sprite = "obj_barrel_tapped", Prompt = "Look around behind the Drake", Skill = Rules.Skill.Investigation, DC = 12,
                QuestId = "pips_barrel", QuestStage = 1, AdvanceTo = 2, SetsFlag = "barrel_tracks",
                Success = "A ring in the dirt where a barrel stood. Spilled stout. And small boot prints, heavy on the heels like someone rolling something big, heading east... toward the orchard.",
                Failure = "Mud, a cat, and the smell of old beer. Nothing that tells you anything. Maybe the light will be better tomorrow.",
                AlreadyFound = "The trail leads east, toward the orchard.",
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
            // ---- Phase 4 quest spots and the owlbear
            var goats = new[] { (8, 13, "goat_1", "A goat is chewing on a fern, looking deeply pleased with itself."), (30, 24, "goat_2", "A goat is standing on a stump, staring at nothing, as goats do."), (12, 27, "goat_3", "A goat is trying to eat the goblins' washing line.") };
            foreach (var (gx, gy, gid, gtext) in goats)
                forest.Secrets[new Vector2Int(gx, gy)] = new SecretDef
                {
                    Id = gid, Sprite = "mon_goat_0", Prompt = "Coax the goat home", Skill = Rules.Skill.AnimalHandling, DC = 11,
                    QuestId = "goats", QuestStage = 1, SetsFlag = gid,
                    Success = gtext + " You scratch behind its horns, it sniffs your pockets, and then it trots off toward Thistledown like this was its idea all along.",
                    Failure = gtext + " It butts you squarely in the knee and goes back to eating. (Try again tomorrow, when it's in a better mood.)",
                    AlreadyFound = "That goat's gone home.",
                };
            forest.Secrets[new Vector2Int(3, 9)] = new SecretDef
            {
                Id = "owlbear_den", Sprite = "obj_log", Prompt = "Return the cub to the owlbear den", Skill = Rules.Skill.Nature, DC = 10,
                QuestId = "haunted_mill", QuestStage = 3, PacifiesGroup = "forest_owlbear",
                Success = "You find the den: a hollow under a fallen oak, lined with feathers. The cub wriggles free and tumbles inside. From the dark comes a low, enormous hoot. Then a softer one. You back away very, very slowly, and nothing follows you.",
                Failure = "Every hollow log looks like a den, and none of them smell like owlbear. The cub sneezes on you. Try again tomorrow.",
                AlreadyFound = "The den is quiet. Somewhere inside, something small is snoring.",
            };
            forest.Secrets[new Vector2Int(40, 20)] = new SecretDef
            {
                Id = "lost_tracks_1", Sprite = "fx_leaf_0", Prompt = "Look for Maddie's tracks", Skill = Rules.Skill.Survival, DC = 13,
                QuestId = "lost_child", QuestStage = 1, AdvanceTo = 2,
                Success = "Small boot prints in the soft earth, and a stick sword dropped beside them. She went west, deeper in.",
                Failure = "Leaves, mud, deer prints. Nothing small enough to be Maddie's.",
                AlreadyFound = "Her trail leads west.",
            };
            forest.Secrets[new Vector2Int(16, 21)] = new SecretDef
            {
                Id = "lost_tracks_2", Sprite = "fx_leaf_0", Prompt = "Follow the tiny lights", Skill = Rules.Skill.Perception, DC = 13,
                QuestId = "lost_child", QuestStage = 2, AdvanceTo = 3,
                Success = "Between the trees: tiny lights, bobbing like fireflies, all drifting the same way. Toward the fairy ring. And faintly, a child laughing.",
                Failure = "Trees. More trees. Was that a light? No. Maybe.",
                AlreadyFound = "The lights drift toward the fairy ring.",
            };
            forest.Secrets[new Vector2Int(27, 9)] = new SecretDef
            {
                Id = "spot_herb_moonpetal", Sprite = "obj_herb", Prompt = "Pick the glowing moonpetal", Skill = Rules.Skill.Nature, DC = 12,
                QuestId = "elowens_herbs", QuestStage = 1, SetsFlag = "got_herb_moonpetal",
                Success = "You ease the moonpetal out roots and all, the way Elowen showed you. It glows softly in your hand.",
                Failure = "You tug, and the petals close up tight and dim. It'll open again tomorrow.",
                AlreadyFound = "Just leaves here now.",
                Items = { new KeyValuePair<string, int>("herb_moonpetal", 1) },
            };
            var owlbear = new EnemyGroupDef { Id = "forest_owlbear", SightTiles = 7, Style = EncounterStyle.Beasts, Unique = true, Label = "the owlbear", TalkDC = 14, ScareDC = 16,
                QuestId = "haunted_mill", MinStage = 2, MaxStage = 3,
                Spotted = "Branches split. An OWLBEAR shoulders out of the trees: feathers bristling, beak clacking, eyes searching for something. She's huge. (The DM leans in: this is a deadly fight for a young party.) She hasn't charged... yet.",
                Befriended = "You keep your voice low and your hands open. The owlbear's feathers slowly flatten. She huffs, sniffs the air (the cub's scent is on you), and lumbers off toward the lake." };
            owlbear.Members.Add(new EnemySpawn("owlbear", 2, 7));
            forest.EnemyGroups.Add(owlbear);

            var stash = new ChestDef { Id = "goblin_stash", Pos = new Vector2Int(7, 30), Copper = 1200 };
            stash.Items.Add(new KeyValuePair<string, int>("rations", 3));
            stash.Items.Add(new KeyValuePair<string, int>("gemstone", 1));
            stash.Items.Add(new KeyValuePair<string, int>("torch", 2));
            forest.Chests.Add(stash);
            Zones[forest.Id] = forest;

            BuildFord();
            BuildCrater();

            var shop = new ZoneDef { Id = "shop", Name = "Ironkettle General Goods", Map = Maps.Shop, Indoors = true, DefaultSpawn = new Vector2Int(6, 7),
                EnterNarration = "The shop smells of lamp oil and leather. Every shelf is organized with terrifying dwarven precision.", ShopCounter = new Vector2Int(6, 3) };
            shop.Warps.Add(new WarpDef(6, 8, "town", 14, 15));
            Zones[shop.Id] = shop;

            var tavernBrawl = new EnemyGroupDef { Id = "drake_brawlers", Brawl = true, Unique = true, Style = EncounterStyle.None, SightTiles = 0, Label = "the farmhands",
                QuestId = "bar_brawl", MinStage = 1, MaxStage = 1 };
            tavernBrawl.Members.Add(new EnemySpawn("brawler", 12, 6));
            tavernBrawl.Members.Add(new EnemySpawn("brawler", 13, 8));
            tavernBrawl.Members.Add(new EnemySpawn("brawler", 11, 9));

            var tavern = new ZoneDef { Id = "tavern", Name = "The Sleeping Drake", Map = Maps.Tavern, Indoors = true, DefaultSpawn = new Vector2Int(8, 9), SafeForLongRest = true,
                EnterNarration = "The Sleeping Drake is warm, loud and smells like stew. Above the hearth hangs a carved wooden dragon, asleep, with a little nightcap." };
            tavern.Warps.Add(new WarpDef(8, 10, "town", 32, 11));
            tavern.EnemyGroups.Add(tavernBrawl);
            Zones[tavern.Id] = tavern;

            var temple = new ZoneDef { Id = "temple", Name = "Temple of the Dawnmother", Map = Maps.Temple, Indoors = true, DefaultSpawn = new Vector2Int(5, 7), SafeForLongRest = true,
                EnterNarration = "Morning light pours through tall windows onto a golden sunburst. It is very quiet, and very peaceful." };
            temple.Warps.Add(new WarpDef(5, 9, "town", 7, 10));
            temple.Warps.Add(new WarpDef(6, 9, "town", 7, 10));
            Zones[temple.Id] = temple;

            var forge = new ZoneDef { Id = "forge", Name = "Ashforge Smithy", Map = Maps.Forge, Indoors = true, DefaultSpawn = new Vector2Int(6, 7),
                EnterNarration = "Heat hits you like a friendly shove. Horseshoes, hinges and a few honest swords hang on the walls, and everything smells of coal and hot iron.", ShopCounter = new Vector2Int(6, 3), Music = "music_shop" };
            forge.Warps.Add(new WarpDef(6, 8, "town", 6, 41));
            Zones[forge.Id] = forge;

            var watch = new ZoneDef { Id = "watchhouse", Name = "The Watch House", Map = Maps.WatchHouse, Indoors = true, DefaultSpawn = new Vector2Int(5, 6), Music = "music_shop",
                EnterNarration = "One desk buried in paperwork, one holding cell (currently storing a broom), and a wall map of the valley stuck full of pins. Most of the pins say 'kobold?'." };
            watch.Warps.Add(new WarpDef(5, 7, "town", 27, 41));
            Zones[watch.Id] = watch;

            var cottage = new ZoneDef { Id = "cottage_lark", Name = "Lark's Cottage", Map = Maps.CottageLark, Indoors = true, DefaultSpawn = new Vector2Int(4, 5), Music = "music_home",
                EnterNarration = "Every surface holds a jar of flowers. Pressed petals are drying between the pages of a very large book." };
            cottage.Warps.Add(new WarpDef(4, 6, "town", 4, 50));
            Zones[cottage.Id] = cottage;

            var home = new ZoneDef { Id = "home", Name = "Your Cottage", Map = Maps.Home, Indoors = true, DefaultSpawn = new Vector2Int(5, 5), SafeForLongRest = true,
                BedPos = new Vector2Int(2, 2), EnterNarration = "Home. Small, a little drafty, and entirely yours." };
            home.Warps.Add(new WarpDef(5, 7, "town", 8, 29));
            home.Chests.Add(new ChestDef { Id = "home_storage", Pos = new Vector2Int(7, 2), IsStorage = true });
            Zones[home.Id] = home;

            var mine = new ZoneDef { Id = "mine", Name = "The Old Copper Mine", Map = Maps.Mine, Dark = true, DefaultSpawn = new Vector2Int(19, 27),
                EnterNarration = "The air is cold and smells of wet stone... and, faintly, of woodsmoke. Somewhere deep below, many small voices are chanting." };
            mine.Warps.Add(new WarpDef(19, 28, "town", 25, 5));
            mine.Warps.Add(new WarpDef(20, 28, "town", 26, 5));
            mine.Warps.Add(new WarpDef(19, 2, "warrens", 5, 29)); // the stairs down where the egg used to be

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
            BuildWarrens();

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
            ford.DoorSigns[new Vector2Int(37, 19)] = new BuildingSign("Old Hobb's Mill", "HOBB'S MILL\n\nFlour, meal and grumbling.\nEst. a very long time ago.");
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
            ford.Secrets[new Vector2Int(38, 22)] = new SecretDef
            {
                Id = "mill_noises", Sprite = "obj_crate", Prompt = "Listen for the 'ghost'", Skill = Rules.Skill.Investigation, DC = 11,
                QuestId = "haunted_mill", QuestStage = 1, AdvanceTo = 2, NightOnly = true,
                Success = "The wailing is coming from behind the flour sacks. You ease one aside... and two enormous round eyes blink up at you. A fluffy, feathery, furious OWLBEAR CUB, about the size of a dog, with flour on its beak.",
                Failure = "A wail... from the left? The right? Under the mill? You lose it in the creak of the wheel. Try again tomorrow night.",
                AlreadyFound = "The cub is here, watching you.",
            };
            ford.Secrets[new Vector2Int(39, 22)] = new SecretDef
            {
                Id = "owlbear_cub", Sprite = "mon_owlbear_cub_0", Prompt = "Calm the owlbear cub", Skill = Rules.Skill.AnimalHandling, DC = 13,
                QuestId = "haunted_mill", QuestStage = 2, AdvanceTo = 3,
                Success = "You crouch, hum, and hold out a crust. The cub sniffs, nibbles, and then climbs into your arms like it's been waiting all week for someone to ask. It is very heavy. It smells of flour.",
                Failure = "The cub hisses, puffs up to twice its size, and hides behind the sacks. Maybe tomorrow, with a snack.",
                AlreadyFound = "The cub is snoozing in your arms.",
            };
            ford.Secrets[new Vector2Int(32, 12)] = new SecretDef
            {
                Id = "spot_herb_riverleaf", Sprite = "obj_herb", Prompt = "Gather riverleaf from the bank", Skill = Rules.Skill.Nature, DC = 11,
                QuestId = "elowens_herbs", QuestStage = 1, SetsFlag = "got_herb_riverleaf",
                Success = "Cool green leaves, picked from the base so the plant will grow back. They smell of mint and mud.",
                Failure = "You grab a handful of something. It's nettles. It's definitely nettles. Try again tomorrow.",
                AlreadyFound = "The riverleaf patch is growing back.",
                Items = { new KeyValuePair<string, int>("herb_riverleaf", 1) },
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
            brannoc.Schedule.Add(new ScheduleEntry(8, "tavern", 11, 5, "enjoying his day off", Hearthday)); // shop closed
            brannoc.Schedule.Add(new ScheduleEntry(9, "temple", 3, 3, "at the Hearthday service, pretending not to sing", Hearthday));
            brannoc.Schedule.Add(new ScheduleEntry(11, "tavern", 11, 5, "enjoying his day off", Hearthday));
            brannoc.Schedule.Add(new ScheduleEntry(10, "town", 21, 15, "selling odds and ends at the market", Coinday));
            brannoc.Schedule.Add(new ScheduleEntry(14, "shop", 6, 2, "counting the market takings", Coinday));
            Npcs[brannoc.Id] = brannoc;

            var elowen = new NpcDef { Id = "elowen", Name = "Mother Elowen", Title = "Priestess of the Dawnmother", Look = "npc_elowen", Dialogue = "elowen", ShopId = "temple" };
            elowen.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            elowen.Schedule.Add(new ScheduleEntry(6, "temple", 6, 2, "tending the altar"));
            elowen.Schedule.Add(new ScheduleEntry(17, "town", 5, 11, "tending the temple flowers"));
            elowen.Schedule.Add(new ScheduleEntry(20, "temple", 6, 2, "at evening prayers"));
            elowen.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            elowen.Schedule.Add(new ScheduleEntry(9, "temple", 5, 2, "leading the Hearthday service", Hearthday));
            elowen.Schedule.Add(new ScheduleEntry(11, "temple", 6, 2, "tidying up after the service", Hearthday));
            elowen.Schedule.Add(new ScheduleEntry(19, "tavern", 10, 5, "at story night with Brannoc (holding hands under the table)", Emberday) { RequiresFlag = "poems_revealed" });
            elowen.Schedule.Add(new ScheduleEntry(20, "tavern", 10, 5, "at story night with Brannoc (prayers can wait, just this once)", Emberday) { RequiresFlag = "poems_revealed" });
            elowen.Schedule.Add(new ScheduleEntry(22, "temple", 6, 2, "at late prayers, humming", Emberday) { RequiresFlag = "poems_revealed" });
            Npcs[elowen.Id] = elowen;

            var pip = new NpcDef { Id = "pip", Name = "Pip Thistlewhit", Title = "Keeper of the Sleeping Drake", Look = "npc_pip", Dialogue = "pip", ShopId = "tavern" };
            pip.Schedule.Add(new ScheduleEntry(0, "tavern", 4, 1, "wiping down the bar"));
            pip.Schedule.Add(new ScheduleEntry(2, null, 0, 0, "asleep"));
            pip.Schedule.Add(new ScheduleEntry(9, "tavern", 4, 1, "behind the bar"));
            pip.Schedule.Add(new ScheduleEntry(9, null, 0, 0, "sleeping in (it's Hearthday)", Hearthday));
            pip.Schedule.Add(new ScheduleEntry(11, "tavern", 4, 1, "behind the bar, yawning", Hearthday));
            Npcs[pip.Id] = pip;

            var rhoswen = new NpcDef { Id = "rhoswen", Name = "Captain Rhoswen Vale", Title = "Captain of the Watch", Look = "npc_rhoswen", Dialogue = "rhoswen", Romanceable = true };
            rhoswen.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            rhoswen.Schedule.Add(new ScheduleEntry(7, "town", 20, 15, "frowning at the notice board"));
            rhoswen.Schedule.Add(new ScheduleEntry(12, "tavern", 11, 8, "eating lunch standing up"));
            rhoswen.Schedule.Add(new ScheduleEntry(13, "town", 24, 6, "watching the mine road"));
            rhoswen.Schedule.Add(new ScheduleEntry(19, "tavern", 10, 7, "off duty (supposedly)"));
            rhoswen.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            rhoswen.Schedule.Add(new ScheduleEntry(7, "town", 27, 42, "drilling the watch (all two of them)", Stormday));
            rhoswen.Schedule.Add(new ScheduleEntry(10, "watchhouse", 3, 2, "doing paperwork, furiously", Stormday));
            Npcs[rhoswen.Id] = rhoswen;

            var tobin = new NpcDef { Id = "tobin", Name = "Old Tobin", Title = "Farmer", Look = "npc_villager_tobin", Dialogue = "tobin" };
            tobin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            tobin.Schedule.Add(new ScheduleEntry(6, "town", 18, 25, "fussing over his fence"));
            tobin.Schedule.Add(new ScheduleEntry(12, "town", 12, 19, "looking for his spoon"));
            tobin.Schedule.Add(new ScheduleEntry(18, "tavern", 12, 6, "telling the same story again"));
            tobin.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            tobin.Schedule.Add(new ScheduleEntry(10, "town", 23, 18, "selling apples (and opinions) at the market", Coinday));
            tobin.Schedule.Add(new ScheduleEntry(9, "temple", 3, 5, "at the Hearthday service, front row", Hearthday));
            tobin.Schedule.Add(new ScheduleEntry(11, "town", 18, 25, "fussing over his fence", Hearthday));
            Npcs[tobin.Id] = tobin;

            var maddie = new NpcDef { Id = "maddie", Name = "Maddie", Title = "Village kid", Look = "npc_villager_maddie", Dialogue = "maddie" };
            maddie.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            maddie.Schedule.Add(new ScheduleEntry(8, "town", 32, 31, "skipping stones at the pond"));
            maddie.Schedule.Add(new ScheduleEntry(14, "town", 24, 18, "playing 'dragon' with a stick"));
            maddie.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "home for supper"));
            maddie.Schedule.Add(new ScheduleEntry(8, "town", 35, 27, "feeding the ducks (bread, against the rules)", Leafday));
            maddie.Schedule.Add(new ScheduleEntry(9, "temple", 4, 6, "at the Hearthday service, fidgeting", Hearthday));
            maddie.Schedule.Add(new ScheduleEntry(11, "town", 16, 47, "playing on the South Lane green", Hearthday));
            maddie.Schedule.Add(new ScheduleEntry(15, "forest", 14, 21, "lost in the woods, following tiny lights") { RequiresFlag = "maddie_lost", ForbidFlag = "maddie_found" });
            maddie.Schedule.Add(new ScheduleEntry(19, "forest", 14, 21, "lost in the woods, following tiny lights") { RequiresFlag = "maddie_lost", ForbidFlag = "maddie_found" });
            Npcs[maddie.Id] = maddie;

            var hobb = new NpcDef { Id = "hobb", Name = "Old Hobb", Title = "Miller", Look = "npc_hobb", Dialogue = "hobb" };
            hobb.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            hobb.Schedule.Add(new ScheduleEntry(6, "ford", 37, 20, "glaring at the idle mill wheel"));
            hobb.Schedule.Add(new ScheduleEntry(12, "ford", 40, 25, "weeding the garden"));
            hobb.Schedule.Add(new ScheduleEntry(18, "ford", 36, 20, "sitting on the step with a pipe"));
            hobb.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            hobb.Schedule.Add(new ScheduleEntry(12, "ford", 36, 20, "mending flour sacks on the step", Dawnday));
            Npcs[hobb.Id] = hobb;

            BuildSouthLaneNpcs();
            BuildPlacesNpcs();
        }

        /// <summary>The South Lane folk (Phase 4). Names are side content pending lore approval.</summary>
        static void BuildSouthLaneNpcs()
        {
            var garrick = new NpcDef { Id = "garrick", Name = "Garrick Ashforge", Title = "Blacksmith", Look = "npc_garrick", Dialogue = "garrick", ShopId = "forge" };
            garrick.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            garrick.Schedule.Add(new ScheduleEntry(7, "town", 11, 40, "hammering at the anvil"));
            garrick.Schedule.Add(new ScheduleEntry(12, "forge", 6, 2, "minding the forge"));
            garrick.Schedule.Add(new ScheduleEntry(18, "tavern", 9, 7, "having a well-earned pint"));
            garrick.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            garrick.Schedule.Add(new ScheduleEntry(10, "town", 29, 20, "hawking horseshoes at the market", Coinday));
            garrick.Schedule.Add(new ScheduleEntry(14, "forge", 6, 2, "minding the forge", Coinday));
            garrick.Schedule.Add(new ScheduleEntry(19, "tavern", 9, 4, "telling the one about the wyvern at story night", Emberday));
            garrick.Schedule.Add(new ScheduleEntry(9, "temple", 3, 7, "at the Hearthday service, back row", Hearthday));
            garrick.Schedule.Add(new ScheduleEntry(11, "town", 11, 40, "tinkering on his day off", Hearthday));
            Npcs[garrick.Id] = garrick;

            var hesper = new NpcDef { Id = "hesper", Name = "Hesper Meadowsweet", Title = "Beekeeper", Look = "npc_hesper", Dialogue = "hesper" };
            hesper.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            hesper.Schedule.Add(new ScheduleEntry(7, "town", 34, 43, "glaring across the stream"));
            hesper.Schedule.Add(new ScheduleEntry(11, "town", 36, 45, "tending her garden"));
            hesper.Schedule.Add(new ScheduleEntry(17, "town", 34, 42, "sitting on her doorstep"));
            hesper.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            hesper.Schedule.Add(new ScheduleEntry(19, "tavern", 4, 7, "at story night, heckling", Emberday));
            hesper.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep", Emberday));
            hesper.Schedule.Add(new ScheduleEntry(9, "temple", 7, 5, "at the Hearthday service, left side", Hearthday));
            hesper.Schedule.Add(new ScheduleEntry(11, "town", 36, 45, "tending her garden", Hearthday));
            Npcs[hesper.Id] = hesper;

            var dunstan = new NpcDef { Id = "dunstan", Name = "Dunstan Burrow", Title = "Beekeeper (the other one)", Look = "npc_dunstan", Dialogue = "dunstan" };
            dunstan.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            dunstan.Schedule.Add(new ScheduleEntry(7, "town", 44, 43, "glaring across the stream"));
            dunstan.Schedule.Add(new ScheduleEntry(11, "town", 45, 47, "fussing with his beehives"));
            dunstan.Schedule.Add(new ScheduleEntry(17, "town", 45, 42, "sitting on his doorstep"));
            dunstan.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            dunstan.Schedule.Add(new ScheduleEntry(19, "tavern", 13, 7, "at story night, heckling the heckler", Emberday));
            dunstan.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep", Emberday));
            dunstan.Schedule.Add(new ScheduleEntry(9, "temple", 8, 6, "at the Hearthday service, right side (pointedly)", Hearthday));
            dunstan.Schedule.Add(new ScheduleEntry(11, "town", 45, 47, "fussing with his beehives", Hearthday));
            Npcs[dunstan.Id] = dunstan;

            var lark = new NpcDef { Id = "lark", Name = "Lark", Title = "Flower seller", Look = "npc_lark", Dialogue = "lark" };
            lark.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            lark.Schedule.Add(new ScheduleEntry(7, "town", 8, 46, "hanging out the washing"));
            lark.Schedule.Add(new ScheduleEntry(10, "town", 25, 22, "selling flowers by the statue"));
            lark.Schedule.Add(new ScheduleEntry(16, "town", 16, 47, "daydreaming on the green"));
            lark.Schedule.Add(new ScheduleEntry(20, "cottage_lark", 4, 3, "pressing flowers"));
            lark.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            lark.Schedule.Add(new ScheduleEntry(9, "temple", 8, 7, "at the Hearthday service, humming along", Hearthday));
            lark.Schedule.Add(new ScheduleEntry(11, "town", 16, 47, "daydreaming on the green", Hearthday));
            Npcs[lark.Id] = lark;

            var tamsin = new NpcDef { Id = "tamsin", Name = "Tamsin", Title = "Fisher", Look = "npc_tamsin", Dialogue = "tamsin" };
            tamsin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            tamsin.Schedule.Add(new ScheduleEntry(6, "town", 47, 27, "fishing at the pond"));
            tamsin.Schedule.Add(new ScheduleEntry(12, "town", 40, 43, "pacing on the footbridge"));
            tamsin.Schedule.Add(new ScheduleEntry(15, "town", 36, 27, "fishing, not catching anything"));
            tamsin.Schedule.Add(new ScheduleEntry(19, "tavern", 6, 9, "nursing a cider, nervously"));
            tamsin.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            tamsin.Schedule.Add(new ScheduleEntry(9, "temple", 7, 7, "at the Hearthday service, sitting near Lark (casually)", Hearthday));
            tamsin.Schedule.Add(new ScheduleEntry(11, "town", 40, 43, "pacing on the footbridge", Hearthday));
            tamsin.Schedule.Add(new ScheduleEntry(17, "town", 17, 47, "strolling the green with Lark") { RequiresFlag = "lark_tamsin_engaged" });
            lark.Schedule.Add(new ScheduleEntry(17, "town", 16, 47, "strolling the green with Tamsin") { RequiresFlag = "lark_tamsin_engaged" });
            Npcs[tamsin.Id] = tamsin;

            var wick = new NpcDef { Id = "wick", Name = "Wick", Title = "Apprentice troublemaker", Look = "npc_wick", Dialogue = "wick" };
            wick.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "at home with his mum"));
            wick.Schedule.Add(new ScheduleEntry(9, "town", 45, 7, "hiding in the orchard with a barrel") { RequiresFlag = "barrel_tracks", ForbidFlag = "wick_caught" });
            wick.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "at home with his mum") { RequiresFlag = "barrel_tracks", ForbidFlag = "wick_caught" });
            wick.Schedule.Add(new ScheduleEntry(10, "tavern", 12, 3, "sweeping the floor (enthusiastically)") { RequiresFlag = "wick_hired" });
            wick.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "home with his mum, who's feeling better") { RequiresFlag = "wick_hired" });
            wick.Schedule.Add(new ScheduleEntry(0, "watchhouse", 10, 2, "sulking in the holding cell") { RequiresLock = "wick_cell" });
            wick.Schedule.Add(new ScheduleEntry(10, "tavern", 12, 3, "sweeping on his day off, for tips", Hearthday) { RequiresFlag = "wick_hired" });
            Npcs[wick.Id] = wick;

            var thimble = new NpcDef { Id = "thimble", Name = "Mister Thimble", Title = "Fae trickster", Look = "npc_thimble", Dialogue = "thimble" };
            thimble.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "somewhere else entirely"));
            thimble.Schedule.Add(new ScheduleEntry(18, "forest", 6, 9, "sitting on a lily pad that isn't there"));
            thimble.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "somewhere else entirely"));
            thimble.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "away at Court (it's Hearthday)", Hearthday));
            Npcs[thimble.Id] = thimble;
        }

        /// <summary>Where an NPC should be at a given hour and weekday (story conditions need the world; without it they never apply).</summary>
        public static ScheduleEntry Current(NpcDef npc, int hour, int weekday, WorldState world = null)
        {
            ScheduleEntry best = null;
            // Later entries win; at the same hour, story-conditional beats weekday beats generic.
            // Each runs from its hour until the next entry of any kind.
            foreach (var e in npc.Schedule)
            {
                if (e.OnlyWeekday >= 0 && e.OnlyWeekday != weekday) continue;
                if (e.FromHour > hour) continue;
                if (e.Conditional && !e.Applies(world)) continue;
                if (best == null || e.FromHour > best.FromHour || (e.FromHour == best.FromHour && Rank(e) > Rank(best))) best = e;
            }
            return best;
        }

        static int Rank(ScheduleEntry e) => (e.Conditional ? 2 : 0) + (e.OnlyWeekday >= 0 ? 1 : 0);
    }
}
