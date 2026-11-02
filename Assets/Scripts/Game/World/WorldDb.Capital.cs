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

        /// <summary>Where the stair from the capital comes down into the Roots.</summary>
        public static readonly Vector2Int RootsStairFoot = new Vector2Int(4, 6);

        static void BuildRoots()
        {
            var z = new ZoneDef
            {
                Act = 3,
                Id = "roots", Name = "The Roots of Aurelhold", Map = Maps.Roots, DefaultSpawn = RootsStairFoot, Music = "music_capital",
                BlightWeight = 0f, WarmLit = true, Shadowed = true,
                EnterNarration = "The Roots. The city's underside, where the mountain leans out over the rooftops and the light comes from oil lamps and steam. Washing hangs across every alley. It's warm down here too. Warmer, if anything.",
            };
            var capital = Zones["capital"];
            capital.Warps.Add(new WarpDef(59, 30, "roots", RootsStairFoot.x, RootsStairFoot.y));
            capital.Warps.Add(new WarpDef(60, 30, "roots", RootsStairFoot.x, RootsStairFoot.y + 1));
            foreach (var at in new[] { new Vector2Int(2, 5), new Vector2Int(3, 5), new Vector2Int(2, 6), new Vector2Int(3, 6) })
                z.Warps.Add(new WarpDef(at.x, at.y, "capital", 58, 30));
            z.Warps.Add(new WarpDef(20, 27, "steamhouse", 8, 10));

            z.Signs[new Vector2Int(5, 4)] = "UP: THE GREAT SQUARE\n\nTwo hundred and twelve steps. Somebody has counted. Somebody has written the number on the wall, with feeling.";
            z.Signs[new Vector2Int(51, 23)] = "A gate of black iron twice the height of a house, set straight into the mountain. It's barred from this side with a beam as thick as a tree, and sealed with the King's own seal, in red wax the size of a dinner plate.\n\nThe iron is warm.\n\nPainted on the rock beside it: THE UNDERGATE. DEEP SHIFT ONLY. BY ROYAL WARRANT.";
            z.DoorSigns[new Vector2Int(20, 27)] = new BuildingSign("The Steamhouse", "THE STEAMHOUSE\n\nHot baths, one copper. Hotter baths, two.\nThe water comes up from below. Don't ask where. We don't.");
            z.DoorSigns[new Vector2Int(50, 17)] = new BuildingSign("Foreman's Office", "FOUNDATION WORKS\nForeman's Office\n\nKnock. Wait. Knock again. Go away.");

            var patrol = new EnemyGroupDef { Id = "roots_patrol", Style = EncounterStyle.Talk, TalkDC = 16, ScareDC = 18, Label = "the Crown patrol", NightOnly = true,
                Spotted = "Lanterns swing round the corner: three of the King's own, in good steel. \"Curfew in the Roots, by order. State your business, or state it to the magistrate.\"",
                Befriended = "You talk, they listen. The captain looks at your dragon, at you, and at the hour, and decides she's off duty in ten minutes anyway. \"Mind the steps going up.\"",
                VouchFlag = "banner_crown", VouchText = "The captain sees the Crown's colours on you and stands her patrol aside. \"Evening, friends of the Redoubt. Lady-Commander Marsh speaks well of you.\"" };
            patrol.Members.Add(new EnemySpawn("crown_knight", 26, 18));
            patrol.Members.Add(new EnemySpawn("crown_knight", 27, 19));
            patrol.Members.Add(new EnemySpawn("crown_battlemage", 26, 21));
            z.EnemyGroups.Add(patrol);

            var men = new EnemyGroupDef { Id = "crale_men", Style = EncounterStyle.Talk, TalkDC = 15, ScareDC = 15, Label = "Crale's works crew", Brawl = true, Unique = true,
                RequiresFlag = "crale_men_called",
                BrawlText = "Crale bellows across the yard: \"FISTS! Nobody draws steel in my works, and nobody gets killed! The Crown'd have my hide!\"",
                Spotted = "Three of the biggest men in the Roots put down their shovels and roll up their sleeves.",
                Befriended = "\"...Foreman says we're done.\" They pick their shovels back up, not looking at you." };
            men.Members.Add(new EnemySpawn("brawler", 43, 22));
            men.Members.Add(new EnemySpawn("brawler", 45, 22));
            men.Members.Add(new EnemySpawn("brawler", 44, 23));
            z.EnemyGroups.Add(men);

            z.Secrets[new Vector2Int(48, 18)] = new SecretDef
            {
                Id = "works_ledger", Sprite = "item_scroll_revivify", Prompt = "Read the works ledger (quietly)", Skill = Rules.Skill.SleightOfHand, DC = 16,
                QuestId = DeepShift.Id, QuestStage = DeepShift.Crale, AdvanceTo = DeepShift.Below,
                Success = "Crale's ledger lies open on the office windowsill. You turn the pages without a sound.",
                Failure = "Crale looks up from the yard, and you lean on the wall very casually until he looks away. Another day.",
                AlreadyFound = "WENT DOWN. CAME UP. DEEP SHIFT. PAID.",
                Lore = "works_ledger",
            };
            Zones[z.Id] = z;

            var steam = new ZoneDef
            {
                Act = 3, Indoors = true,
                Id = "steamhouse", Name = "The Steamhouse", Map = Maps.Steamhouse, DefaultSpawn = new Vector2Int(8, 10), Music = "music_capital",
                EnterNarration = "Steam, everywhere. Stone benches round a pool the size of a pond, and the water's so hot it moves on its own. Workers off the night shift sit up to their chins in it with their eyes shut.",
            };
            steam.Warps.Add(new WarpDef(8, 11, "roots", 20, 28));
            steam.Secrets[new Vector2Int(9, 8)] = new SecretDef
            {
                Id = "hot_spring", Sprite = "fx_smoke_1", Prompt = "Soak in the deep pool", Skill = Rules.Skill.Medicine, DC = 13, Daily = true, HeroicInspiration = true,
                Success = "You ease in, inch by inch, until only your nose is out. The water comes up from below, near boiling, and smells of nothing. Not sulphur. Not stone. Just heat. You climb out a new person. (Heroic Inspiration!)",
                Failure = "Too hot. Far too hot. You sit on the edge with your feet in and pretend that was the plan. Maybe tomorrow.",
                AlreadyFound = "You've had your soak today. Your fingers are still wrinkled.",
            };
            Zones[steam.Id] = steam;

            var mags = new NpcDef { Id = "mags", Name = "Mags Tolley", Title = "Laundress, the Roots", Look = "npc_mags", Dialogue = "mags" };
            mags.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            mags.Schedule.Add(new ScheduleEntry(8, "roots", 21, 30, "wringing out washing by the Steamhouse"));
            mags.Schedule.Add(new ScheduleEntry(20, null, 0, 0, "at home, writing a letter"));
            Npcs[mags.Id] = mags;

            var crale = new NpcDef { Id = "crale", Name = "Foreman Crale", Title = "The Foundation Works", Look = "npc_crale", Dialogue = "crale" };
            crale.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            crale.Schedule.Add(new ScheduleEntry(6, "roots", 44, 21, "shouting at the works crew"));
            crale.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "at the Gilded Wyvern, drinking alone"));
            Npcs[crale.Id] = crale;

            var tilly = new NpcDef { Id = "tilly", Name = "Tilly Brightwick", Title = "Lamplighter of the Roots", Look = "npc_tilly", Dialogue = "tilly" };
            tilly.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            tilly.Schedule.Add(new ScheduleEntry(8, "roots", 24, 11, "filling the oil lamps"));
            tilly.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep"));
            Npcs[tilly.Id] = tilly;
        }
    }
}
