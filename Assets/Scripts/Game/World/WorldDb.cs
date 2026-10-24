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

    public class EnemyGroupDef
    {
        public string Id;
        public List<EnemySpawn> Members = new List<EnemySpawn>();
        /// <summary>If set, noticing the party opens this dialogue instead of starting combat.</summary>
        public string ParleyDialogue;
        public int SightTiles = 6;
    }

    public class ChestDef
    {
        public string Id;
        public Vector2Int Pos;
        public List<KeyValuePair<string, int>> Items = new List<KeyValuePair<string, int>>();
        public int Copper;
        public bool IsStorage;   // the player's home chest (persistent storage, not loot)
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
                Id = "town", Name = "Thistledown", Map = Maps.Town, DefaultSpawn = new Vector2Int(19, 16),
                EnterNarration = "Thistledown. Smoke curls from chimneys, a dog barks somewhere, and to the north the Wyrmback Mountains loom like sleeping giants.",
            };
            town.Warps.Add(new WarpDef(8, 10, "shop", 6, 7));
            town.Warps.Add(new WarpDef(30, 10, "tavern", 8, 9));
            town.Warps.Add(new WarpDef(8, 22, "temple", 5, 8));
            town.Warps.Add(new WarpDef(29, 22, "home", 5, 6));
            town.Warps.Add(new WarpDef(19, 2, "mine", 19, 27));
            town.Warps.Add(new WarpDef(20, 2, "mine", 20, 27));
            town.Signs[new Vector2Int(21, 3)] = "DANGER: OLD COPPER MINE. CLOSED BY ORDER OF THE WATCH.\n\nSomeone has drawn a small, very proud dragon on the sign in charcoal.";
            town.Signs[new Vector2Int(22, 13)] = "THISTLEDOWN NOTICE BOARD\n\nMISSING: 4 chickens, 1 copper kettle, Old Tobin's lucky spoon.\nReport suspicious small scaly persons to Captain Vale.\n\nAlso: did ANYONE else see the falling star last night?";
            Zones[town.Id] = town;

            var shop = new ZoneDef { Id = "shop", Name = "Ironkettle General Goods", Map = Maps.Shop, Indoors = true, DefaultSpawn = new Vector2Int(6, 7),
                EnterNarration = "The shop smells of lamp oil and leather. Every shelf is organized with terrifying dwarven precision.", ShopCounter = new Vector2Int(6, 3) };
            shop.Warps.Add(new WarpDef(6, 8, "town", 8, 11));
            Zones[shop.Id] = shop;

            var tavern = new ZoneDef { Id = "tavern", Name = "The Sleeping Drake", Map = Maps.Tavern, Indoors = true, DefaultSpawn = new Vector2Int(8, 9), SafeForLongRest = true,
                EnterNarration = "The Sleeping Drake is warm, loud and smells like stew. Above the hearth hangs a carved wooden dragon, asleep, with a little nightcap." };
            tavern.Warps.Add(new WarpDef(8, 10, "town", 30, 11));
            Zones[tavern.Id] = tavern;

            var temple = new ZoneDef { Id = "temple", Name = "Temple of the Dawnmother", Map = Maps.Temple, Indoors = true, DefaultSpawn = new Vector2Int(5, 7), SafeForLongRest = true,
                EnterNarration = "Morning light pours through tall windows onto a golden sunburst. It is very quiet, and very peaceful." };
            temple.Warps.Add(new WarpDef(5, 9, "town", 8, 23));
            temple.Warps.Add(new WarpDef(6, 9, "town", 8, 23));
            Zones[temple.Id] = temple;

            var home = new ZoneDef { Id = "home", Name = "Your Cottage", Map = Maps.Home, Indoors = true, DefaultSpawn = new Vector2Int(5, 5), SafeForLongRest = true,
                BedPos = new Vector2Int(2, 2), EnterNarration = "Home. Small, a little drafty, and entirely yours." };
            home.Warps.Add(new WarpDef(5, 7, "town", 29, 23));
            home.Chests.Add(new ChestDef { Id = "home_storage", Pos = new Vector2Int(7, 2), IsStorage = true });
            Zones[home.Id] = home;

            var mine = new ZoneDef { Id = "mine", Name = "The Old Copper Mine", Map = Maps.Mine, Dark = true, DefaultSpawn = new Vector2Int(19, 27),
                EnterNarration = "The air is cold and smells of wet stone... and, faintly, of woodsmoke. Somewhere deep below, many small voices are chanting." };
            mine.Warps.Add(new WarpDef(19, 28, "town", 19, 3));
            mine.Warps.Add(new WarpDef(20, 28, "town", 20, 3));
            mine.EggPos = new Vector2Int(19, 2);

            var g1 = new EnemyGroupDef { Id = "mine_hub" };
            g1.Members.Add(new EnemySpawn("kobold", 15, 18));
            g1.Members.Add(new EnemySpawn("kobold", 24, 19));
            mine.EnemyGroups.Add(g1);
            var g2 = new EnemyGroupDef { Id = "mine_rats", SightTiles = 5 };
            g2.Members.Add(new EnemySpawn("giant_rat", 4, 12));
            g2.Members.Add(new EnemySpawn("giant_rat", 6, 14));
            g2.Members.Add(new EnemySpawn("giant_rat", 7, 10));
            mine.EnemyGroups.Add(g2);
            var g3 = new EnemyGroupDef { Id = "mine_east" };
            g3.Members.Add(new EnemySpawn("kobold_slinger", 35, 15));
            g3.Members.Add(new EnemySpawn("kobold_slinger", 36, 12));
            g3.Members.Add(new EnemySpawn("kobold", 33, 17));
            mine.EnemyGroups.Add(g3);
            var g4 = new EnemyGroupDef { Id = "mine_egg", ParleyDialogue = "snik", SightTiles = 7 };
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
            elowen.Schedule.Add(new ScheduleEntry(17, "town", 10, 23, "tending the temple flowers"));
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
            rhoswen.Schedule.Add(new ScheduleEntry(7, "town", 23, 13, "frowning at the notice board"));
            rhoswen.Schedule.Add(new ScheduleEntry(12, "tavern", 11, 8, "eating lunch standing up"));
            rhoswen.Schedule.Add(new ScheduleEntry(13, "town", 21, 4, "watching the mine road"));
            rhoswen.Schedule.Add(new ScheduleEntry(19, "tavern", 10, 7, "off duty (supposedly)"));
            rhoswen.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep"));
            Npcs[rhoswen.Id] = rhoswen;

            var tobin = new NpcDef { Id = "tobin", Name = "Old Tobin", Title = "Farmer", Look = "npc_villager_tobin", Dialogue = "tobin" };
            tobin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            tobin.Schedule.Add(new ScheduleEntry(6, "town", 6, 27, "fussing over his fence"));
            tobin.Schedule.Add(new ScheduleEntry(12, "town", 14, 16, "looking for his spoon"));
            tobin.Schedule.Add(new ScheduleEntry(18, "tavern", 12, 6, "telling the same story again"));
            tobin.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            Npcs[tobin.Id] = tobin;

            var maddie = new NpcDef { Id = "maddie", Name = "Maddie", Title = "Village kid", Look = "npc_villager_maddie", Dialogue = "maddie" };
            maddie.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            maddie.Schedule.Add(new ScheduleEntry(8, "town", 33, 24, "skipping stones at the pond"));
            maddie.Schedule.Add(new ScheduleEntry(14, "town", 17, 12, "playing 'dragon' with a stick"));
            maddie.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "home for supper"));
            Npcs[maddie.Id] = maddie;
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
