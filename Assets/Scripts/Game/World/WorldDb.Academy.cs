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
            Npcs[holloway.Id] = holloway;
        }

        static void BuildAcademy()
        {
            var z = new ZoneDef
            {
                Id = "academy", Name = "The Starfall Academy", Map = Maps.Academy, DefaultSpawn = new Vector2Int(27, 40), Music = "music_academy",
                BlightWeight = 0.15f, // the wards hold the ash back
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
            Npcs[juniper.Id] = juniper;

            var pell = new NpcDef { Id = "pell", Name = "Pell", Title = "Second-year student (allegedly)", Look = "npc_pell", Dialogue = "pell" };
            pell.Schedule.Add(new ScheduleEntry(0, "academy", 33, 10, "on the bell green, waiting for the thirteenth bell"));
            pell.Schedule.Add(new ScheduleEntry(1, null, 0, 0, "asleep (late for class)"));
            pell.Schedule.Add(new ScheduleEntry(10, "academy", 20, 34, "skipping a lecture on the refectory lawn"));
            pell.Schedule.Add(new ScheduleEntry(18, "academy", 35, 11, "loitering near the bell tower"));
            Npcs[pell.Id] = pell;

            var oakes = new NpcDef { Id = "oakes", Name = "Professor Bramwell Oakes", Title = "Master of the Glasshouse", Look = "npc_oakes", Dialogue = "oakes" };
            oakes.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            oakes.Schedule.Add(new ScheduleEntry(7, "glasshouse", 9, 6, "fretting over the ember-lilies"));
            oakes.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "at dinner, still talking about soil"));
            Npcs[oakes.Id] = oakes;

            var varrow = new NpcDef { Id = "varrow", Name = "Magister Isolde Varrow", Title = "Astronomer", Look = "npc_varrow", Dialogue = "varrow" };
            varrow.Schedule.Add(new ScheduleEntry(0, "academy", 47, 27, "at the telescopes, counting"));
            varrow.Schedule.Add(new ScheduleEntry(3, null, 0, 0, "asleep, at last"));
            varrow.Schedule.Add(new ScheduleEntry(11, "observatory", 5, 2, "correcting star charts"));
            varrow.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "at dinner"));
            varrow.Schedule.Add(new ScheduleEntry(20, "academy", 47, 27, "at the telescopes, counting"));
            Npcs[varrow.Id] = varrow;

            foreach (var (id, x) in new[] { ("crown_guard_w", 25), ("crown_guard_e", 30) })
            {
                var guard = new NpcDef { Id = id, Name = "Crown soldier", Title = "The Academy garrison", Look = "npc_crown_soldier", Dialogue = "crown_guard" };
                guard.Schedule.Add(new ScheduleEntry(0, "academy", x, 40, "standing a long watch at the gate"));
                Npcs[guard.Id] = guard;
            }
        }
    }
}
