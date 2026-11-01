using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 3: the Lantern Road and the Starfall Academy. The Academy must feel powerful and mysterious (user):
    /// visible magic everywhere, a few mysteries, and something at play under the library.
    /// Nothing here may hint at THE SECRET (see LoreTests.NoActTwoTextHintsAtTheSecret).
    /// </summary>
    public static partial class WorldDb
    {
        /// <summary>World flag set while Act II runs (schedules key off it).</summary>
        public const string ActTwoFlag = "act2";

        static void BuildLanternRoad()
        {
            var z = new ZoneDef
            {
                Id = "lantern_road", Name = "The Lantern Road", Map = Maps.LanternRoad, DefaultSpawn = new Vector2Int(1, 14), Music = "music_road",
                EnterNarration = "The Lantern Road climbs east out of the valley. Iron lantern posts line it every hundred paces, cold in the daylight. The fields on either side should be gold this time of year. They're grey.",
            };
            z.Warps.Add(new WarpDef(0, 14, "ford", 44, 14));
            z.Warps.Add(new WarpDef(0, 15, "ford", 44, 15));
            // The gate warps to the Academy are added by BuildAcademy.

            z.Signs[new Vector2Int(6, 13)] = "A lantern post of black iron, taller than a man. A brass plate on its foot: LIT BY THE ACADEMY, FOR ALL WHO TRAVEL.\n\nThe glass is clean. The wick is dry. Nobody has lit it in a while.";

            z.DoorSigns[new Vector2Int(32, 21)] = new BuildingSign("Abandoned Farmhouse", "Chalked on the door: GONE TO PORT VARREL.\n\nUnderneath, in another hand: SO HAVE WE.\n\nThe door is swollen shut with ash.");
            z.Secrets[new Vector2Int(24, 8)] = new SecretDef
            {
                Id = "halfway_lamp", Sprite = "obj_brazier", Prompt = "Tend the Halfway Lamp", Skill = Rules.Skill.Religion, DC = 12, Daily = true, HeroicInspiration = true,
                Success = "An old roadside shrine with a lamp that is never allowed to go out. You add a twig to the flame and say the travellers' words. It burns a little brighter, just for you.",
                Failure = "You fumble the old words and the flame gutters. It doesn't go out. It never does. Try again tomorrow.",
                AlreadyFound = "The Halfway Lamp burns on, steady as a heartbeat.",
            };
            z.Secrets[new Vector2Int(35, 23)] = new SecretDef
            {
                Id = "farm_cellar", Sprite = "obj_crate", Prompt = "Pry at the cellar hatch", Skill = Rules.Skill.Investigation, DC = 14,
                Success = "Under the ash, a hatch. Under the hatch, the family's emergency shelf, untouched: they left in a hurry, but they left it for whoever came next.",
                Failure = "The hatch is stuck fast under a crust of ash. You'll need a better angle. Tomorrow.",
                AlreadyFound = "An empty shelf, and a scratched note: 'GONE TO MY SISTER'S IN PORT VARREL. TAKE WHAT YOU NEED.'",
                Items = { new KeyValuePair<string, int>("potion_greater_healing", 2) },
                Copper = 3000,
            };
            z.Secrets[new Vector2Int(14, 22)] = new SecretDef
            {
                Id = "ash_circle", Sprite = "obj_rock", Prompt = "Study the pattern in the ash", Skill = Rules.Skill.Nature, DC = 13,
                Success = "The ash hasn't fallen at random. It lies in rings, wide and even, like ripples on a pond.",
                Failure = "Just ash, ankle deep. It gets in your boots and stays there.",
                AlreadyFound = "Rings in the ash, as even as ripples.",
                Lore = "ash_fields",
            };
            z.Secrets[new Vector2Int(47, 10)] = new SecretDef
            {
                Id = "checkpoint_ledger", Sprite = "item_scroll_revivify", Prompt = "Read the checkpoint ledger (quietly)", Skill = Rules.Skill.SleightOfHand, DC = 15,
                Success = "You turn the pages without a sound.",
                Failure = "A soldier glances your way and you put the ledger down very naturally. Maybe another day.",
                AlreadyFound = "The same neat columns: names, dates, and a tick in the 'animal' box.",
                Lore = "checkpoint_orders",
            };

            var hounds = new EnemyGroupDef { Id = "road_hounds", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the cinder hounds",
                Spotted = "Two hounds lift their heads from the ash. Their eyes glow like banked coals, and their breath smokes.",
                Befriended = "You hold still and speak low. The hounds sniff the air, whine at something only they can feel, and lope away into the grey." };
            hounds.Members.Add(new EnemySpawn("cinder_hound", 10, 20));
            hounds.Members.Add(new EnemySpawn("cinder_hound", 12, 23));
            z.EnemyGroups.Add(hounds);
            var boars = new EnemyGroupDef { Id = "road_boars", Style = EncounterStyle.Beasts, TalkDC = 14, ScareDC = 14, Label = "the ember-eyed boars",
                Spotted = "Something is rooting through the ruined farm. Three boars, huge, with ash in their bristles and a coal-glow where their eyes should be.",
                Befriended = "You back away slowly, talking the whole time. The boars watch you with those burning eyes, then go back to the turnips." };
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 31, 23));
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 33, 24));
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 30, 22));
            z.EnemyGroups.Add(boars);
            var ankheg = new EnemyGroupDef { Id = "road_ankheg", Style = EncounterStyle.Beasts, TalkDC = 16, ScareDC = 13, Label = "the ankheg", SightTiles = 4,
                Spotted = "The bank by the stream bulges, splits, and something long and armoured pours out of it, mandibles clicking.",
                Befriended = "You stamp and shout and throw a rock. The ankheg decides you're more trouble than you're worth and sinks back into the mud." };
            ankheg.Members.Add(new EnemySpawn("ankheg", 43, 25));
            z.EnemyGroups.Add(ankheg);
            Zones[z.Id] = z;

            // The Crown checkpoint: Sergeant Holloway (his force's quests come in a later phase).
            var holloway = new NpcDef { Id = "holloway", Name = "Sergeant Dace Holloway", Title = "Crown garrison, the Lantern Road", Look = "npc_holloway", Dialogue = "holloway" };
            holloway.Schedule.Add(new ScheduleEntry(0, "lantern_road", 46, 11, "dozing on his feet at the barrier"));
            holloway.Schedule.Add(new ScheduleEntry(6, "lantern_road", 46, 11, "checking travellers at the barrier"));
            holloway.Schedule.Add(new ScheduleEntry(19, "lantern_road", 48, 12, "warming his hands at the fire"));
            holloway.Schedule.Add(new ScheduleEntry(6, "lantern_road", 48, 11, "writing his weekly letter home to Port Varrel", Hearthday));
            Npcs[holloway.Id] = holloway;
        }

        static void BuildAcademy()
        {
            var z = new ZoneDef
            {
                Id = "academy", Name = "The Starfall Academy", Map = Maps.Academy, DefaultSpawn = new Vector2Int(27, 40), Music = "music_academy",
                BlightWeight = 0.15f, // the wards hold the ash back
                MagicLit = true,
                EnterNarration = "The gate opens onto another world. Stone floats here, turning slowly in the air. Wards glimmer along the walls like heat over a road, and in the great courtyard a brass orrery taller than a house turns all by itself. Somewhere a bell is ringing, and you can't tell which one.",
            };
            // The road's gate (the road side is added here because this zone is built after it).
            Zones["lantern_road"].Warps.Add(new WarpDef(52, 0, "academy", 27, 40) { MinAct = 2 });
            Zones["lantern_road"].Warps.Add(new WarpDef(53, 0, "academy", 28, 40) { MinAct = 2 });
            z.Warps.Add(new WarpDef(27, 43, "lantern_road", 52, 1));
            z.Warps.Add(new WarpDef(28, 43, "lantern_road", 53, 1));
            z.Warps.Add(new WarpDef(8, 26, "glasshouse", 9, 10));
            z.Warps.Add(new WarpDef(48, 25, "observatory", 7, 9));
            // The library (12,11) and the Orrery Tower (44,13) doors are added with their interiors.

            z.DoorSigns[new Vector2Int(12, 11)] = new BuildingSign("The Library of Quiet Stars", "THE LIBRARY OF QUIET STARS\n\nSilence is requested. Silence is expected. Silence will be enforced.\n- M. Thorne, Archivist");
            z.DoorSigns[new Vector2Int(44, 13)] = new BuildingSign("The Orrery Tower", "THE ORRERY TOWER\nHigh Magister C. Ashby\n\nVisitors are welcome. The stairs are not.");
            z.DoorSigns[new Vector2Int(48, 25)] = new BuildingSign("The Observatory", "THE OBSERVATORY\n\nDo not touch the lenses. Do not touch the charts. Do not touch the Magister.");
            z.DoorSigns[new Vector2Int(8, 26)] = new BuildingSign("The Glasshouse", "THE GLASSHOUSE\nProf. B. Oakes\n\nPlease close the door. The lilies feel the cold.");
            z.Signs[new Vector2Int(37, 10)] = "THE THIRTEEN BELLS\n\nRung at every hour by the Porters' Guild, as they have been since the founding.\n\nA newer notice, pinned beneath: STUDENTS ARE NOT TO BE ON THE BELL GREEN AFTER MIDNIGHT. THIS MEANS YOU, PELL.";
            z.Signs[new Vector2Int(27, 13)] = "Twelve bronze dragons lie in a ring, wings folded, heads resting on their forelegs, around a pit of carved stone flames. Each is a little different. Students have rubbed their noses golden.";

            z.Secrets[new Vector2Int(34, 10)] = new SecretDef
            {
                Id = "thirteenth_bell", Sprite = "obj_inscription", Prompt = "Listen for the thirteenth bell", Skill = Rules.Skill.Perception, DC = 14,
                FromHour = 0, ToHour = 1, Lore = "thirteenth_bell",
                Success = "You stand on the bell green in the dark and count. Twelve. Then, a breath late, deep enough to feel in your teeth: thirteen. It doesn't come from the tower. It comes from under your feet.",
                Failure = "The night wind carries the sound away and you lose count somewhere around nine. Come back tomorrow night.",
                AlreadyFound = "Twelve, and one more, from somewhere below.",
            };
            z.Secrets[new Vector2Int(12, 34)] = new SecretDef
            {
                Id = "starwell", Sprite = "obj_starwell", Prompt = "Look into the Starwell", Skill = Rules.Skill.Arcana, DC = 15, Daily = true, HeroicInspiration = true, Lore = "starwell",
                Success = "You look past your reflection, past the water, into a depth that goes on much further than any well should. Something down there looks back, kindly. You come away feeling lucky.",
                Failure = "Water, stars, your own face. The magic is there; you just can't find the edge of it today.",
                AlreadyFound = "The stars in the water have moved since this morning.",
            };
            z.Lore.Add(new LoreSpotDef { Id = "great_ones_statue", Pos = new Vector2Int(29, 14), Prompt = "Read the plaque on the plinth", Sprite = "obj_inscription" });
            z.Lore.Add(new LoreSpotDef { Id = "primer_ashen_age", Pos = new Vector2Int(19, 34), Prompt = "Pick up the dropped textbook", Sprite = "item_scroll_revivify" });
            Zones[z.Id] = z;

            var glass = new ZoneDef
            {
                Id = "glasshouse", Name = "The Glasshouse", Map = Maps.Glasshouse, Indoors = true, DefaultSpawn = new Vector2Int(9, 10), Music = "music_academy",
                EnterNarration = "Warm, wet air and the smell of growing things. Lilies the colour of embers line the beds, and some of them have gone grey at the edges, like paper held too near a candle.",
            };
            glass.Warps.Add(new WarpDef(9, 11, "academy", 8, 27));
            Zones[glass.Id] = glass;

            var obs = new ZoneDef
            {
                Id = "observatory", Name = "The Observatory", Map = Maps.Observatory, Indoors = true, DefaultSpawn = new Vector2Int(7, 9), Music = "music_academy",
                EnterNarration = "A domed room open to the sky through a slit in the roof. A model of the heavens turns on its own in a circle of glowing runes, and every surface is covered in star charts, most of them annotated in a furious, beautiful hand.",
            };
            obs.Warps.Add(new WarpDef(7, 10, "academy", 48, 26));
            Zones[obs.Id] = obs;

        }

        /// <summary>The Academy's people (after BuildPlacesNpcs: Perrin gets his Act II schedule here).</summary>
        static void BuildAcademyNpcs()
        {
            var perrin = Npcs["perrin"];
            const string a2 = ActTwoFlag;
            perrin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep in the junior scholars' dormitory") { RequiresFlag = a2 });
            perrin.Schedule.Add(new ScheduleEntry(8, "academy", 24, 23, "showing first-years the orrery (and getting it wrong)") { RequiresFlag = a2 });
            perrin.Schedule.Add(new ScheduleEntry(18, "academy", 21, 34, "eating supper and reading at the same time") { RequiresFlag = a2 });
            perrin.Schedule.Add(new ScheduleEntry(19, "academy", 21, 34, "eating supper and reading at the same time") { RequiresFlag = a2 });
            perrin.Schedule.Add(new ScheduleEntry(20, "academy", 21, 34, "still reading, long after supper") { RequiresFlag = a2 });
            perrin.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep in the junior scholars' dormitory") { RequiresFlag = a2 });

            var juniper = new NpcDef { Id = "juniper", Name = "Juniper", Title = "Second-year student", Look = "npc_juniper", Dialogue = "juniper" };
            juniper.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            juniper.Schedule.Add(new ScheduleEntry(9, "academy", 32, 25, "sketching the orrery"));
            juniper.Schedule.Add(new ScheduleEntry(17, "academy", 22, 34, "at supper, talking with her mouth full"));
            juniper.Schedule.Add(new ScheduleEntry(21, null, 0, 0, "asleep"));
            juniper.Schedule.Add(new ScheduleEntry(9, "academy", 14, 31, "drawing the Starwell (no lectures on Hearthday)", Hearthday));
            Npcs[juniper.Id] = juniper;

            var pell = new NpcDef { Id = "pell", Name = "Pell", Title = "Second-year student (allegedly)", Look = "npc_pell", Dialogue = "pell" };
            pell.Schedule.Add(new ScheduleEntry(0, "academy", 33, 10, "on the bell green, waiting for the thirteenth bell"));
            pell.Schedule.Add(new ScheduleEntry(1, null, 0, 0, "asleep (late for class)"));
            pell.Schedule.Add(new ScheduleEntry(10, "academy", 20, 34, "skipping a lecture on the refectory lawn"));
            pell.Schedule.Add(new ScheduleEntry(18, "academy", 35, 11, "loitering near the bell tower"));
            pell.Schedule.Add(new ScheduleEntry(10, "academy", 29, 36, "feeding the geese (they hate him)", Coinday));
            Npcs[pell.Id] = pell;

            var oakes = new NpcDef { Id = "oakes", Name = "Professor Bramwell Oakes", Title = "Master of the Glasshouse", Look = "npc_oakes", Dialogue = "oakes" };
            oakes.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            oakes.Schedule.Add(new ScheduleEntry(7, "glasshouse", 9, 6, "fretting over the ember-lilies"));
            oakes.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "at dinner, still talking about soil"));
            oakes.Schedule.Add(new ScheduleEntry(7, "academy", 46, 40, "pruning the hedge maze, very slowly", Leafday));
            Npcs[oakes.Id] = oakes;

            var varrow = new NpcDef { Id = "varrow", Name = "Magister Isolde Varrow", Title = "Astronomer", Look = "npc_varrow", Dialogue = "varrow" };
            varrow.Schedule.Add(new ScheduleEntry(0, "academy", 47, 27, "at the telescopes, counting"));
            varrow.Schedule.Add(new ScheduleEntry(3, null, 0, 0, "asleep, at last"));
            varrow.Schedule.Add(new ScheduleEntry(11, "observatory", 5, 2, "correcting star charts"));
            varrow.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "at dinner"));
            varrow.Schedule.Add(new ScheduleEntry(20, "academy", 47, 27, "at the telescopes, counting"));
            varrow.Schedule.Add(new ScheduleEntry(11, "library", 25, 21, "borrowing more star charts than she's allowed", Dawnday));
            Npcs[varrow.Id] = varrow;

            foreach (var (id, x) in new[] { ("crown_guard_w", 25), ("crown_guard_e", 30) })
            {
                var guard = new NpcDef { Id = id, Name = "Crown soldier", Title = "The Academy garrison", Look = "npc_crown_soldier", Dialogue = "crown_guard" };
                guard.Schedule.Add(new ScheduleEntry(0, "academy", x, 40, "standing a long watch at the gate"));
                guard.Schedule.Add(new ScheduleEntry(6, "academy", x, 40, "polishing armour that is already polished (inspection day)", Stormday));
                Npcs[guard.Id] = guard;
            }
        }

        /// <summary>World flags for the library's ways in.</summary>
        public const string StacksUnlooped = "stacks_unlooped", RestrictedPass = "restricted_pass", ThornePassShown = "thorne_pass_shown";

        static void BuildLibrary()
        {
            var academy = Zones["academy"];
            academy.Warps.Add(new WarpDef(12, 11, "library", 17, 22));

            var lib = new ZoneDef
            {
                Id = "library", Name = "The Library of Quiet Stars", Map = Maps.Library, Indoors = true, DefaultSpawn = new Vector2Int(17, 22), Music = "music_library",
                EnterNarration = "Shelves climb into a dark that might be a ceiling and might be sky. Lanterns drift between the stacks on their own, turning their light toward whoever is reading. It is so quiet you can hear the books breathing. You're almost sure that's a figure of speech.",
            };
            lib.Warps.Add(new WarpDef(17, 23, "academy", 12, 12));
            // The Endless Stacks: until you see through the illusion, the corridor's end is its beginning.
            lib.Warps.Add(new WarpDef(29, 2, "library", 2, 2) { ForbidFlag = StacksUnlooped, Narration = "The shelves go on. And on. That reading lamp looks familiar." });
            lib.Warps.Add(new WarpDef(33, 2, "restricted", 1, 9) { RequiresFlag = RestrictedPass, ClosedText = "Runes crawl across the door like ink in water. It does not open for you." });
            const string seeThrough = "You stop looking at the shelves and look at the gaps between them. The corridor folds up like a paper fan, and the door at its end was three steps away all along.";
            lib.Secrets[new Vector2Int(15, 1)] = new SecretDef
            {
                Id = "stacks_illusion", Sprite = "obj_bookshelf", Prompt = "Something about these shelves is wrong (Arcana)", Skill = Rules.Skill.Arcana, DC = 15, SetsFlag = StacksUnlooped,
                Success = seeThrough, Failure = "The shelves are shelves. Probably. Your head hurts. Try again tomorrow.", AlreadyFound = "Just shelves now. Honest ones.",
            };
            lib.Secrets[new Vector2Int(17, 1)] = new SecretDef
            {
                Id = "stacks_illusion_inv", Sprite = "obj_bookshelf", Prompt = "Count the books on this shelf (Investigation)", Skill = Rules.Skill.Investigation, DC = 15, SetsFlag = StacksUnlooped,
                Success = "The same green book, the same chipped spine, every forty paces. " + seeThrough, Failure = "You lose count. You start again. You lose count again. Tomorrow.", AlreadyFound = "Just shelves now. Honest ones.",
            };
            lib.Secrets[new Vector2Int(31, 1)] = new SecretDef
            {
                Id = "unweave_ward", Sprite = "obj_ward", Prompt = "Unweave the ward on the door (Arcana)", Skill = Rules.Skill.Arcana, DC = 16, SetsFlag = RestrictedPass,
                Success = "You find the loose thread in the spell and pull, gently. The runes on the door blink, confused, and let you through.",
                Failure = "The ward notices you. It doesn't do anything. It just notices. Come back when it's forgotten you.",
                AlreadyFound = "The ward still recognises you as a friend. It's wrong, but it's polite.",
            };
            lib.Secrets[new Vector2Int(31, 3)] = new SecretDef
            {
                Id = "pick_ward_lock", Sprite = "obj_inscription", Prompt = "Pick the lock under the runes (Sleight of Hand)", Skill = Rules.Skill.SleightOfHand, DC = 16, SetsFlag = RestrictedPass,
                Success = "Under all that magic is a perfectly ordinary lock, because somebody had to have a key. Click.",
                Failure = "A pin slips. Somewhere behind the door, something heavy shifts in its sleep. Leave it for today.",
                AlreadyFound = "The lock remembers your picks.",
            };
            Zones[lib.Id] = lib;

            var res = new ZoneDef
            {
                Id = "restricted", Name = "The Restricted Section", Map = Maps.Restricted, Indoors = true, Dark = true, DefaultSpawn = new Vector2Int(1, 9), Music = "music_restricted",
                EnterNarration = "The books in here are chained to their shelves, and some of the chains are new. Suits of armour stand in the alcoves. One of them turns its helm, very slowly, to follow you.",
            };
            res.Warps.Add(new WarpDef(0, 9, "library", 32, 2));
            res.Warps.Add(new WarpDef(21, 15, "sealing_hall", 12, 19));
            var guardians = new EnemyGroupDef
            {
                Id = "restricted_guardians", Style = EncounterStyle.None, Unique = true, SightTiles = 5, Label = "the guardians",
                VouchFlag = ThornePassShown, VouchText = "The armours turn their helms to the Archivist's seal on your pass, and settle back into their alcoves. The rug, grudgingly, lies flat.",
                Spotted = "The armours step out of their alcoves in perfect step. On the floor, the long red rug ripples, though there's no wind.",
            };
            guardians.Members.Add(new EnemySpawn("animated_armor", 8, 8));
            guardians.Members.Add(new EnemySpawn("animated_armor", 14, 8));
            guardians.Members.Add(new EnemySpawn("rug_of_smothering", 11, 12));
            res.EnemyGroups.Add(guardians);
            res.Lore.Add(new LoreSpotDef { Id = "emberhorn_warden", Pos = new Vector2Int(12, 4), Prompt = "Read the chained bestiary", Sprite = "item_scroll_revivify" });
            res.Lore.Add(new LoreSpotDef { Id = "altered_annals", Pos = new Vector2Int(20, 12), Prompt = "Leaf through the Academy annals", Sprite = "item_scroll_revivify" });
            Zones[res.Id] = res;

            var hall = new ZoneDef
            {
                Id = "sealing_hall", Name = "The Sealing Hall", Map = Maps.SealingHall, Indoors = true, Dark = true, DragonStays = true, DefaultSpawn = new Vector2Int(12, 19), Music = "music_sealing",
                EnterNarration = "Someone has been building something down here, for a long time, with great care. A circle of runes, ring inside ring, wide as a barn. Pillars stand around it like teeth. In the middle, an empty dais. The circle is big enough to hold a dragon. Not a small one.",
            };
            hall.Warps.Add(new WarpDef(12, 20, "restricted", 21, 14));
            hall.Lore.Add(new LoreSpotDef { Id = "sealing_work", Pos = new Vector2Int(5, 17), Prompt = "Read the notes on the workbench", Sprite = "item_scroll_revivify" });
            Zones[hall.Id] = hall;

            var thorne = new NpcDef { Id = "thorne", Name = "Archivist Mirelle Thorne", Title = "Keeper of the Library of Quiet Stars", Look = "npc_thorne", Dialogue = "thorne" };
            thorne.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep (with a book)"));
            thorne.Schedule.Add(new ScheduleEntry(7, "library", 17, 18, "at the long desk, stamping returns with great force"));
            thorne.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep (with a book)"));
            thorne.Schedule.Add(new ScheduleEntry(7, "academy", 10, 31, "reading by the Starwell on her day off (the library is closed)", Hearthday));
            Npcs[thorne.Id] = thorne;
        }

        static void BuildAshbyTower()
        {
            Zones["academy"].Warps.Add(new WarpDef(44, 13, "ashby_tower", 8, 12));
            var z = new ZoneDef
            {
                Id = "ashby_tower", Name = "The Orrery Tower", Map = Maps.AshbyTower, Indoors = true, DefaultSpawn = new Vector2Int(8, 12), Music = "music_ashby",
                EnterNarration = "The stairs climb, and climb. You count three hundred and twelve. From outside, the tower has four windows. At the top is a round study full of star charts and books and quiet, with a great window looking north, at the mountains.",
            };
            z.Warps.Add(new WarpDef(8, 13, "academy", 44, 14));
            Zones[z.Id] = z;

            var ashby = new NpcDef { Id = "ashby", Name = "High Magister Corvane Ashby", Title = "High Magister of the Starfall Academy", Look = "npc_ashby", Dialogue = "ashby" };
            ashby.Schedule.Add(new ScheduleEntry(0, "ashby_tower", 8, 2, "at the great window, watching the mountains"));
            ashby.Schedule.Add(new ScheduleEntry(8, "ashby_tower", 8, 7, "at his desk, writing very small"));
            ashby.Schedule.Add(new ScheduleEntry(22, "ashby_tower", 8, 2, "at the great window, watching the mountains"));
            ashby.Schedule.Add(new ScheduleEntry(18, "academy", 27, 19, "walking the courtyard alone at dusk, as he does every Hearthday", Hearthday));
            Npcs[ashby.Id] = ashby;
        }

        static void BuildProfessorsQuests()
        {
            var academy = Zones["academy"];
            academy.Secrets[new Vector2Int(37, 33)] = new SecretDef
            {
                Id = "geese_nest", Sprite = "obj_bush", Prompt = "Coax the geese off their nest", Skill = Rules.Skill.AnimalHandling, DC = 13,
                QuestId = "overdue", QuestStage = 1, SetsFlag = "book_geese", Items = { new KeyValuePair<string, int>("overdue_book", 1) },
                Success = "You talk low and steady and offer the head goose a crust. It considers you with deep contempt, then waddles off. Under it, carefully incubated: a library book. It's warm. It's VERY overdue.",
                Failure = "The goose rises up to its full height and HONKS. You retreat with your dignity mostly intact. Try again tomorrow.",
                AlreadyFound = "The geese glare at you. They remember.",
            };
            academy.Secrets[new Vector2Int(46, 34)] = new SecretDef
            {
                Id = "maze_book", Sprite = "obj_hedge", Prompt = "Reach into the hedge", Skill = Rules.Skill.Investigation, DC = 8,
                QuestId = "overdue", QuestStage = 1, SetsFlag = "book_maze", Items = { new KeyValuePair<string, int>("overdue_book", 1) },
                Success = "At the heart of the maze, wedged deep in the hedge where someone hid it during a game of hide-and-seek eleven years ago: a library book. The bookmark is still in chapter three.",
                Failure = "Leaves, twigs, a lost glove. The book must be deeper in. Tomorrow.",
                AlreadyFound = "An empty gap in the hedge, exactly book-shaped.",
            };

            var glass = Zones["glasshouse"];
            var blights = new EnemyGroupDef
            {
                Id = "glasshouse_blights", Style = EncounterStyle.Beasts, TalkSkill = Rules.Skill.Nature, TalkDC = 14, ScareDC = 15, Label = "the blighted vines", SightTiles = 5,
                QuestId = "glasshouse", MinStage = 1, MaxStage = 1, Unique = true,
                Spotted = "The grey vines in the far beds uncoil, and stand, and come toward you on roots like knotted fingers.",
                Befriended = "You kneel and lay a hand on the soil. Something in the roots is cold and wrong and frightened. You soothe what you can. One by one the vines sink back into their beds and lie still.",
            };
            blights.Members.Add(new EnemySpawn("vine_blight", 4, 5));
            blights.Members.Add(new EnemySpawn("vine_blight", 15, 5));
            blights.Members.Add(new EnemySpawn("vine_blight", 5, 8));
            blights.Members.Add(new EnemySpawn("vine_blight", 14, 8));
            glass.EnemyGroups.Add(blights);
            const string heat = "At the heart of the glasshouse, under the beds, the old heat-stone that warms everything: a block of volcanic rock from the Emberhorn's slopes. It's cracked clean through, and the fire inside it has gone grey.";
            glass.Secrets[new Vector2Int(9, 3)] = new SecretDef
            {
                Id = "cracked_heatstone", Sprite = "obj_rock", Prompt = "Find the root of it (Nature)", Skill = Rules.Skill.Nature, DC = 13,
                QuestId = "glasshouse", QuestStage = 2, AdvanceTo = 3, Lore = "grey_fire",
                Success = heat, Failure = "Soil, roots, more soil. Whatever it is, it's deeper. Tomorrow.", AlreadyFound = "The cracked heat-stone, cold and grey.",
            };
            glass.Secrets[new Vector2Int(10, 3)] = new SecretDef
            {
                Id = "cracked_heatstone_arcana", Sprite = "obj_rock", Prompt = "Feel for the magic (Arcana)", Skill = Rules.Skill.Arcana, DC = 13,
                QuestId = "glasshouse", QuestStage = 2, AdvanceTo = 3, Lore = "grey_fire",
                Success = heat, Failure = "The magic here is tangled up with the growing. You can't find the thread today.", AlreadyFound = "The cracked heat-stone, cold and grey.",
            };
        }
    }
}
