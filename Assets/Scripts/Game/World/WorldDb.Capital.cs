using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 3: Aurelhold, the capital, inside the ring of the Wyrmback. Grand, bright and warm while the realm went grey,
    /// full of Crown pageantry, and a little off if you look. Nothing here may hint at THE SECRET before the door
    /// (LoreTests.NoActTwoTextHintsAtTheSecret scans this file): suspicion of the Crown is fine; she is never hinted at.
    /// </summary>
    public static partial class WorldDb
    {
        /// <summary>Where the King's carriage sets you down: inside the Gate of Wings, and in Thistledown square.</summary>
        public static readonly Vector2Int CapitalCarriageStop = new Vector2Int(31, 41), TownCarriageStop = new Vector2Int(27, 18);

        static void BuildCapital()
        {
            var z = new ZoneDef
            {
                Act = 3,
                Id = "capital", Name = "Aurelhold", Map = Maps.Capital, DefaultSpawn = CapitalCarriageStop, Music = "music_capital",
                BlightWeight = 0f, // the only warm place left in the realm
                WarmLit = true,
                EnterNarration = "Aurelhold. Snow on every peak of the ring, and not a flake in the streets. The air is warm. The lamps are lit at noon. Somewhere a band is rehearsing, badly, and three hundred people have stopped to stare at your dragon.",
            };
            // Doors to the palace, the Hall of Records and the Gilded Wyvern come with their interiors; the Roots stair with the Roots.

            z.DoorSigns[new Vector2Int(31, 9)] = new BuildingSign("The Palace of Aurelhold", "THE PALACE OF AURELHOLD\n\nThe King receives petitioners from nine bells to five.\nBy order of the Crown, long may it keep the flame.");
            z.DoorSigns[new Vector2Int(54, 10)] = new BuildingSign("The Hall of Records", "THE HALL OF RECORDS\nThe Royal Annals of Aurelhold\n\nScholars by appointment. Ink by permission. Food never.");
            z.DoorSigns[new Vector2Int(53, 22)] = new BuildingSign("The Gilded Wyvern", "THE GILDED WYVERN\nFine Rooms, Finer Ale\n\nDragons welcome in the yard. Dragons NOT welcome in the cellar.");
            z.DoorSigns[new Vector2Int(9, 10)] = new BuildingSign("The Lantern Cathedral", "THE LANTERN CATHEDRAL\nof the Dawnmother\n\nServices at dawn and dusk. The doors are closed between, for the choir's sake.");

            z.Signs[new Vector2Int(31, 23)] = "A knight of the Crown stands on a plinth of white stone, sword raised high. Under her boot, half-sunk in carved flames, is a great crowned shape with its wings broken.\n\nThe crowned shape has been worn smooth by a thousand years of hands. Nobody rubs this one's nose.";
            z.Signs[new Vector2Int(28, 16)] = "THE THOUSANDTH PAGEANT!\n\nOne month hence, in the Great Square: THE FALL OF THE QUEEN, with music, fireworks and a cast of hundreds.\n\nPerformers wanted. DRAGONS ESPECIALLY WANTED.\n(Enquire F. Glass, Pageant-Master, the Square.)";
            z.Signs[new Vector2Int(57, 29)] = "DOWN: THE ROOTS\n\nWorks, Steamhouse, Lower Market.\nMind the steps. Mind the steam. Mind your purse.";
            z.Signs[new Vector2Int(36, 42)] = "THE KING'S CARRIAGE\n\nFor the King's guests, to and from Thistledown.\nAsk the courier. Do not feed the horses sugar. (They have been fed sugar.)";
            z.Signs[new Vector2Int(29, 45)] = "THE KING'S ROAD, south\n\nCLOSED: SNOW.\n\nThe drifts are higher than the sign. Only the King's carriage knows the passes in winter.";

            z.Lore.Add(new LoreSpotDef { Id = "victory_statue", Pos = new Vector2Int(31, 24), Prompt = "Read the plinth", Sprite = "obj_inscription" });

            z.Secrets[new Vector2Int(37, 23)] = new SecretDef
            {
                Id = "ever_lamp", Sprite = "obj_ever_lamp", Prompt = "Look closely at the lamp", Skill = Rules.Skill.Arcana, DC = 16,
                Success = "No oil. No wick. The flame sits on bare iron and burns on nothing at all, and the post is warm, like a hearthstone. The warmth isn't coming from the flame. It's coming up from the ground.",
                Failure = "Pretty. Warm. You can't see what feeds it. Try again tomorrow.",
                AlreadyFound = "The flame burns on nothing. The iron is warm. The warmth comes from below.",
                SetsFlag = "clue_lamps", Lore = "ever_lamps",
            };
            Zones[z.Id] = z;
        }

        static void BuildCapitalNpcs()
        {
            const string a3 = ActThreeFlag;
            var quell = new NpcDef { Id = "quell", Name = "Royal Courier Bertram Quell", Title = "The King's carriage", Look = "npc_quell", Dialogue = "quell" };
            quell.Schedule.Add(new ScheduleEntry(0, "town", TownCarriageStop.x + 1, TownCarriageStop.y, "waiting by the King's carriage, stamping his feet") { RequiresFlag = a3, ForbidFlag = "quell_in_capital" });
            quell.Schedule.Add(new ScheduleEntry(0, "capital", 33, 42, "by the King's carriage, brushing down the horses") { RequiresFlag = "quell_in_capital" });
            Npcs[quell.Id] = quell;

            var wenna = new NpcDef { Id = "wenna", Name = "Wenna Brightwick", Title = "Lamplighter of the Great Square", Look = "npc_wenna", Dialogue = "wenna" };
            wenna.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            wenna.Schedule.Add(new ScheduleEntry(7, "capital", 30, 25, "polishing a lamp that doesn't need polishing"));
            wenna.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "at home"));
            Npcs[wenna.Id] = wenna;

            var hesketh = new NpcDef { Id = "hesketh", Name = "Hesketh", Title = "Baker, the Grand Market", Look = "npc_hesketh", Dialogue = "hesketh" };
            hesketh.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep (bakers sleep early)"));
            hesketh.Schedule.Add(new ScheduleEntry(6, "capital", 8, 22, "selling buns from a tray"));
            hesketh.Schedule.Add(new ScheduleEntry(15, null, 0, 0, "asleep (bakers sleep early)"));
            Npcs[hesketh.Id] = hesketh;

            var merriwether = new NpcDef { Id = "merriwether", Name = "Bandmaster Merriwether", Title = "The Pageant Band", Look = "npc_merriwether", Dialogue = "merriwether" };
            merriwether.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            merriwether.Schedule.Add(new ScheduleEntry(10, "capital", 33, 20, "rehearsing the band (they need it)"));
            merriwether.Schedule.Add(new ScheduleEntry(18, "capital", 51, 24, "at the Gilded Wyvern's yard tables"));
            merriwether.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            Npcs[merriwether.Id] = merriwether;

            for (int i = 0; i < 2; i++)
            {
                var guard = new NpcDef { Id = "gate_guard_" + i, Name = "Crown guard", Title = "The Gate of Wings", Look = "npc_crown_soldier", Dialogue = "capital_guard" };
                guard.Schedule.Add(new ScheduleEntry(0, "capital", i == 0 ? 29 : 34, 43, "standing guard at the Gate of Wings"));
                Npcs[guard.Id] = guard;
            }
        }
    }
}
