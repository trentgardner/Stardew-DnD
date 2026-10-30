using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Phase 5's new places: Starfall Crater, the Kobold Warrens, the egg shrine and the Old Watchtower.</summary>
    public static partial class WorldDb
    {
        static void BuildCrater()
        {
            var z = new ZoneDef
            {
                Id = "crater", Name = "Starfall Crater", Map = Maps.Crater, DefaultSpawn = new Vector2Int(21, 27), Music = "music_crater",
                EnterNarration = "The trail tops the ridge and the hillside falls away into a great scorched bowl. The earth is black and glassy, still faintly warm underfoot. Crystals the green-gold of the falling star jut from the ground, humming to themselves. At the very center: a hollow. Empty.",
            };
            z.Warps.Add(new WarpDef(21, 29, "forest", 30, 1));
            z.Warps.Add(new WarpDef(22, 29, "forest", 31, 1));
            z.Signs[new Vector2Int(9, 21)] = "STARFALL ACADEMY\nOFFICIAL FIELD SURVEY\n\nDo NOT touch the crystals.\n(Unless you are P. Quillby, Junior Scholar.)\n\nUnderneath, in different handwriting: \"we touched them. they tickle.\"";

            z.Secrets[new Vector2Int(22, 14)] = new SecretDef
            {
                Id = "crater_hollow", Sprite = "obj_star_hollow", Prompt = "Study the empty hollow", Skill = Rules.Skill.Investigation, DC = 12,
                QuestId = "fallen_star", QuestStage = FallenStar.Crater,
                Success = "You climb down into the hollow. No star. Just pieces of a shattered casing, thick as pottery and still warm... and scaled, on the outside, like a lizard's back. Whatever fell out of the sky, it wasn't a star. It was wrapped in something.\n\nAll around the hollow, the ash is trampled flat by small clawed feet. Lots of them.",
                Failure = "Ash, glassy rock, and a smell like a blacksmith's on a hot day. Whatever happened here, you can't make sense of it yet. Fresh eyes tomorrow.",
                AlreadyFound = "The hollow's empty, apart from the broken casing and a lot of small footprints.",
            };
            z.Secrets[new Vector2Int(38, 2)] = new SecretDef
            {
                Id = "crater_tracks", Sprite = "obj_tracks", Prompt = "Follow the little clawed tracks", Skill = Rules.Skill.Survival, DC = 13,
                QuestId = "fallen_star", QuestStage = FallenStar.Trail,
                Success = "Small three-toed prints, a dozen pairs at least, all pressed deep, as if they were carrying something heavy between them. They wind up the goat path and over the ridge, straight toward the old copper mine north of Thistledown.\n\nSomeone has dropped a single, very polished spoon along the way.",
                Failure = "The tracks vanish into a scree slope. Goats, rocks, more goats. You'll try again tomorrow when the light's better.",
                AlreadyFound = "The tracks lead toward the old copper mine.",
            };
            z.Secrets[new Vector2Int(28, 11)] = new SecretDef
            {
                Id = "crystal_shard", Sprite = "obj_crystal", Prompt = "Work a shard loose from the crystals", Skill = Rules.Skill.Arcana, DC = 13,
                Success = "You find the seam where the crystal's magic is thinnest and tap it, gently. A sliver comes away clean and lies in your palm, glowing like a coal that forgot to be hot.",
                Failure = "The crystal hums louder, and your fingers go numb. Best leave it until tomorrow.",
                AlreadyFound = "The crystals hum, but they've given up all the shards they're going to.",
                Items = { new KeyValuePair<string, int>("star_shard", 1) },
            };
            z.Secrets[new Vector2Int(15, 16)] = new SecretDef
            {
                Id = "star_iron", Sprite = "obj_rock", Prompt = "Heave the scorched boulder aside", Skill = Rules.Skill.Athletics, DC = 14,
                Success = "With a grunt and a crunch, the boulder rolls over. Underneath, pressed into the glassy earth, is a lump of dark, pitted metal that came down with the star. It's heavy. It's warm. It's definitely worth something.",
                Failure = "The boulder doesn't so much as wobble. Your back has opinions about this. Tomorrow.",
                AlreadyFound = "Just the hollow where the star-iron was.",
                Items = { new KeyValuePair<string, int>("star_iron", 1) },
            };
            z.Secrets[new Vector2Int(3, 2)] = new SecretDef
            {
                Id = "crater_overlook", Sprite = "obj_flagstone", Prompt = "Take in the view from the overlook", Skill = Rules.Skill.Perception, DC = 12, Daily = true, HeroicInspiration = true,
                Success = "From up here you can see the whole valley at once: the crater's black bowl, Whisperwood rolling away green below, the river flashing silver past Thistledown's rooftops, and the Wyrmback Mountains standing over all of it like old guardians. It's the kind of view that makes you want to protect something. (Heroic Inspiration!)",
                Failure = "Wind in your eyes and a goat in your way. Lovely view of the goat, though. Try again tomorrow.",
                AlreadyFound = "The view's still beautiful, but you've already drunk your fill of it today.",
            };

            var beetles = new EnemyGroupDef { Id = "crater_beetles", SightTiles = 5, Style = EncounterStyle.Beasts, Label = "the fire beetles", TalkDC = 11, ScareDC = 10,
                Spotted = "Three beetles the size of dogs are clustered around the crystals, the glands on their backs glowing like coals. One turns toward you and clicks its mandibles, the way a cat flicks its tail. They don't like sharing.",
                Befriended = "You move slowly and keep well away from their crystals. The beetles click at each other, decide you're not interested in their shinies, and go back to basking." };
            beetles.Members.Add(new EnemySpawn("giant_fire_beetle", 26, 12));
            beetles.Members.Add(new EnemySpawn("giant_fire_beetle", 28, 15));
            beetles.Members.Add(new EnemySpawn("giant_fire_beetle", 24, 17));
            z.EnemyGroups.Add(beetles);
            Zones[z.Id] = z;

            Zones["forest"].Warps.Add(new WarpDef(30, 0, "crater", 21, 28));
            Zones["forest"].Warps.Add(new WarpDef(31, 0, "crater", 22, 28));
        }

        /// <summary>Phase 5 NPCs, and the townsfolk who wander up to gawk at the crater.</summary>
        static void BuildPlacesNpcs()
        {
            var perrin = new NpcDef { Id = "perrin", Name = "Perrin Quillby", Title = "Starfall Academy scholar", Look = "npc_perrin", Dialogue = "perrin" };
            perrin.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep in the Drake's spare room"));
            perrin.Schedule.Add(new ScheduleEntry(8, "crater", 7, 22, "measuring the crater with a very long piece of string"));
            perrin.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "walking back to town, taking notes as he goes"));
            perrin.Schedule.Add(new ScheduleEntry(20, "tavern", 5, 7, "at a corner table, writing a very long letter"));
            perrin.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep in the Drake's spare room"));
            Npcs[perrin.Id] = perrin;

            // gawkers: the crater is the most exciting thing to happen to Thistledown in years
            Npcs["tobin"].Schedule.Add(new ScheduleEntry(14, "crater", 14, 20, "gawking at the crater ('back in my day, stars stayed UP')", Leafday));
            Npcs["tobin"].Schedule.Add(new ScheduleEntry(17, "town", 18, 25, "fussing over his fence", Leafday));
            Npcs["maddie"].Schedule.Add(new ScheduleEntry(14, "crater", 12, 20, "hunting for star pieces at the crater", Coinday));
            Npcs["maddie"].Schedule.Add(new ScheduleEntry(17, "town", 24, 18, "playing 'dragon' with a stick", Coinday));
            // Wick only wanders off when he isn't hiding from Pip (before the barrel goes missing) or working for him (after)
            Npcs["wick"].Schedule.Add(new ScheduleEntry(14, "crater", 13, 21, "poking a crystal with a stick", Coinday) { ForbidFlag = "barrel_tracks" });
            Npcs["wick"].Schedule.Add(new ScheduleEntry(17, null, 0, 0, "at home with his mum", Coinday) { ForbidFlag = "barrel_tracks" });
            Npcs["wick"].Schedule.Add(new ScheduleEntry(14, "crater", 13, 21, "poking a crystal with a stick (it's his day off)", Coinday) { RequiresFlag = "wick_hired" });
            Npcs["wick"].Schedule.Add(new ScheduleEntry(17, null, 0, 0, "home with his mum, who's feeling better", Coinday) { RequiresFlag = "wick_hired" });
        }
    }
}
