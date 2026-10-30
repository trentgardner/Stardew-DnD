using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Phase 5's new places: Starfall Crater, the Kobold Warrens, the egg shrine and the Old Watchtower.</summary>
    public static partial class WorldDb
    {
        /// <summary>The First Ideal of the Oathbound, carved at the top of the Old Watchtower. Wording pending lore approval.</summary>
        public const string FirstIdeal = "Light before rest. The watch keeps the light so others may sleep.";

        static void BuildWatchtower()
        {
            var z = new ZoneDef
            {
                Id = "watchtower", Name = "The Old Watchtower", Map = Maps.Watchtower, DefaultSpawn = new Vector2Int(15, 24), Music = "music_watchtower",
                EnterNarration = "On the ridge above the ford, a broken tower of grey stone stands against the sky. Ivy holds half of it together. The courtyard wall has fallen in places, and a stone sentry still keeps watch at the gate, its face worn smooth by a few hundred years of weather.",
            };
            z.Warps.Add(new WarpDef(15, 25, "ford", 36, 1));
            z.Warps.Add(new WarpDef(16, 25, "ford", 37, 1));
            z.Signs[new Vector2Int(9, 21)] = "A stone sentry in a long cloak, one hand raised as if to say 'halt' and the other holding a lantern carved from the same stone. Rain has worn away the face.\n\nAt its feet, half buried in moss: \"WE KEEP THE LIGHT.\"";

            var pz = new PuzzleDef();
            // 1. The three fires: light the first and the last, let the middle sleep.
            pz.Pieces.Add(new PuzzlePiece { Id = "lever_1", Kind = PieceKind.Lever, Pos = new Vector2Int(18, 12),
                Hint = "A worn plaque beside the levers, in old letters: \"The watch lit the first fire and the last, and let the middle sleep.\"", HintSkill = Rules.Skill.History, HintDC = 12 });
            pz.Pieces.Add(new PuzzlePiece { Id = "lever_2", Kind = PieceKind.Lever, Pos = new Vector2Int(20, 12) });
            pz.Pieces.Add(new PuzzlePiece { Id = "lever_3", Kind = PieceKind.Lever, Pos = new Vector2Int(22, 12) });
            pz.Pieces.Add(new PuzzlePiece { Id = "gate_1", Kind = PieceKind.Gate, Pos = new Vector2Int(10, 11), OpensWhen = new[] { "lever_1", "!lever_2", "lever_3" }, Latches = true });
            // 2. The heavy floor: two plates, one block. Solo players use the block; in co-op, a friend or a waiting companion stands on the other.
            pz.Pieces.Add(new PuzzlePiece { Id = "plate_a", Kind = PieceKind.Plate, Pos = new Vector2Int(7, 8) });
            pz.Pieces.Add(new PuzzlePiece { Id = "plate_b", Kind = PieceKind.Plate, Pos = new Vector2Int(14, 8) });
            pz.Pieces.Add(new PuzzlePiece { Id = "block", Kind = PieceKind.Block, Pos = new Vector2Int(10, 9) });
            pz.Pieces.Add(new PuzzlePiece { Id = "gate_2", Kind = PieceKind.Gate, Pos = new Vector2Int(10, 6), OpensWhen = new[] { "plate_a", "plate_b" }, Latches = true });
            // 3. The sunlit stair: bounce the window's light off both mirrors onto the crystal by the stair gate.
            pz.Pieces.Add(new PuzzlePiece { Id = "window", Kind = PieceKind.Light, Pos = new Vector2Int(8, 1), Dir = 2 });
            pz.Pieces.Add(new PuzzlePiece { Id = "mirror_1", Kind = PieceKind.Mirror, Pos = new Vector2Int(8, 4), Dir = 0,
                Hint = "Scratches on the floor show where the light once fell: straight down from the window, then east along the wall, then up to the crystal by the stair.", HintSkill = Rules.Skill.Investigation, HintDC = 13 });
            pz.Pieces.Add(new PuzzlePiece { Id = "mirror_2", Kind = PieceKind.Mirror, Pos = new Vector2Int(14, 4), Dir = 1 });
            pz.Pieces.Add(new PuzzlePiece { Id = "crystal", Kind = PieceKind.Crystal, Pos = new Vector2Int(15, 2) });
            pz.Pieces.Add(new PuzzlePiece { Id = "gate_3", Kind = PieceKind.Gate, Pos = new Vector2Int(16, 3), OpensWhen = new[] { "crystal" }, Latches = true });
            z.Puzzle = pz;

            // The lantern room.
            z.Secrets[new Vector2Int(21, 2)] = new SecretDef
            {
                Id = "tower_inscription", Sprite = "obj_inscription", Prompt = "Read the inscription", Skill = Rules.Skill.History, DC = 10,
                Success = "Carved into the lantern-stone, in letters that were old when Thistledown was young:\n\n\"" + FirstIdeal + "\"\n\nBelow it, a sigil: a sword and a lantern, crossed. The mark of the Oathbound, the knightly orders who once kept towers like this one all across the valley.",
                Failure = "The letters are old, and moss has crept into them. You make out 'light' and 'watch' and lose the rest. Fresh eyes tomorrow.",
                AlreadyFound = "\"" + FirstIdeal + "\"",
            };
            var chest = new ChestDef { Id = "tower_chest", Pos = new Vector2Int(24, 3) };
            chest.Items.Add(new KeyValuePair<string, int>("watchwarden_cloak", 1));
            chest.Items.Add(new KeyValuePair<string, int>("potion_healing", 2));
            z.Chests.Add(chest);
            var armor = new EnemyGroupDef { Id = "tower_armor", ParleyDialogue = "watch_armor", Unique = true, Style = EncounterStyle.None, SightTiles = 4, Label = "the last watchman" };
            armor.Members.Add(new EnemySpawn("animated_armor", 19, 3));
            z.EnemyGroups.Add(armor);
            Zones[z.Id] = z;

            Zones["ford"].Warps.Add(new WarpDef(36, 0, "watchtower", 15, 24));
            Zones["ford"].Warps.Add(new WarpDef(37, 0, "watchtower", 16, 24));
        }

        /// <summary>The Harvest Festival: the square's dressing, the four games, and where everyone stands while it runs.</summary>
        static void BuildFestival()
        {
            var town = Zones["town"];
            town.FestivalGames.Add((new Vector2Int(22, 15), FestivalKind.AppleBob));
            town.FestivalGames.Add((new Vector2Int(31, 15), FestivalKind.Archery));
            town.FestivalGames.Add((new Vector2Int(27, 19), FestivalKind.PieJudging));
            town.FestivalGames.Add((new Vector2Int(25, 13), FestivalKind.BardStage));
            foreach (var pos in new[] { new Vector2Int(20, 13), new Vector2Int(30, 13), new Vector2Int(18, 17), new Vector2Int(32, 17) })
                town.FestivalProps.Add((pos, "obj_bunting"));
            town.FestivalProps.Add((new Vector2Int(23, 19), "obj_pie_table"));

            void At(string npc, int x, int y, string activity) =>
                Npcs[npc].Schedule.Add(new ScheduleEntry(Festival.OpenHour, "town", x, y, activity) { RequiresLock = Festival.Lock });
            At("pip", 30, 20, "pouring free cider at the stall");
            At("brannoc", 27, 18, "judging the pie contest with tremendous seriousness");
            At("hesper", 26, 20, "guarding her Honey Crumble");
            At("dunstan", 28, 20, "guarding his Golden Honey Tart");
            At("maddie", 23, 15, "running the apple-bobbing barrel (and getting soaked)");
            At("rhoswen", 30, 16, "running the archery butts");
            At("lark", 24, 14, "dancing by the stage");
            At("tamsin", 26, 14, "dancing by the stage (badly, happily)");
            At("tobin", 21, 18, "on his third slice of pie");
            At("elowen", 19, 20, "blessing the harvest loaves");
            At("garrick", 24, 22, "arm-wrestling all comers");
            At("wick", 22, 20, "sneaking extra pie");
            At("hobb", 32, 19, "complaining that the cider's too sweet");
            At("perrin", 20, 19, "taking notes on 'quaint local customs'");
        }

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

            // the thief comes back for more after dark: catch Snik in the act at the market stalls
            Zones["town"].Secrets[new Vector2Int(20, 17)] = new SecretDef
            {
                Id = "snik_stakeout", Sprite = "obj_tracks", Prompt = "Keep watch on the market stalls", Skill = Rules.Skill.Perception, DC = 13,
                QuestId = "fallen_star", QuestStage = FallenStar.Trail, NightOnly = true, Dialogue = "snik_caught",
                Success = "",
                Failure = "Hours of nothing. Then a clatter, a squeak, and something small scrambles away over the rooftops before you can turn around. Try again tomorrow night. (Or follow the tracks from the crater.)",
                AlreadyFound = "The stalls are quiet now.",
            };

            Zones["forest"].Warps.Add(new WarpDef(30, 0, "crater", 21, 28));
            Zones["forest"].Warps.Add(new WarpDef(31, 0, "crater", 22, 28));
        }

        static void BuildWarrens()
        {
            var w = new ZoneDef
            {
                Id = "warrens", Name = "The Kobold Warrens", Map = Maps.Warrens, Dark = true, DefaultSpawn = new Vector2Int(5, 29), Music = "music_warrens",
                EnterNarration = "The old mine's stairs spiral down into warmth and firelight. A whole village is tucked into the cavern below: little mud huts, braziers, a mushroom farm, washing hung between stalagmites. And everywhere, small voices, singing to something deeper still.",
            };
            w.Warps.Add(new WarpDef(5, 30, "mine", 19, 3));
            w.Warps.Add(new WarpDef(6, 30, "mine", 19, 3));
            w.Warps.Add(new WarpDef(37, 2, "egg_shrine", 12, 15));

            var gate = new EnemyGroupDef { Id = "warrens_gate", Label = "the gate guards", TalkDC = 13, ScareDC = 13, Unique = true, SightTiles = 6, VouchFlag = "snik_friend",
                Spotted = "Two kobolds leap up from behind a barricade of crates, spears wobbling, and a third whirls a sling. \"HALT! Is the GATE! Nobody passes the gate without the WORD!\" ...A pause. \"What is the word? We are asking you. We forgot.\"",
                Befriended = "The guards confer in rapid squeaks, decide you're probably allowed, and haul the crates aside with a great deal of grunting.",
                VouchText = "\"STOP! Is friends!\" Snik's voice rings out from somewhere above. The gate guards squint at you, then at each other, then shrug and haul the crates aside. \"Snik says friends. Friends pass. Wipe your feet.\"" };
            gate.Members.Add(new EnemySpawn("kobold", 19, 22));
            gate.Members.Add(new EnemySpawn("kobold", 22, 22));
            gate.Members.Add(new EnemySpawn("kobold_slinger", 21, 24));
            w.EnemyGroups.Add(gate);
            w.Secrets[new Vector2Int(16, 20)] = new SecretDef
            {
                Id = "warrens_crack", Sprite = "obj_crack", Prompt = "Squeeze through the crack in the wall", Skill = Rules.Skill.Acrobatics, DC = 12, PassageTo = new Vector2Int(16, 18),
                Success = "You turn sideways, breathe out, and wriggle through a gap no tall-one was ever meant to fit. You pop out on the far side of the gate, behind the guards' backs. Nobody saw.",
                Failure = "You get stuck. Properly stuck. It takes a long, undignified minute to unstick yourself. Maybe tomorrow, after fewer pies.",
                AlreadyFound = "You squeeze back through the crack.",
            };

            var warband = new EnemyGroupDef { Id = "warrens_warband", Label = "Krag's warband", TalkDC = 14, ScareDC = 15, Unique = true, SightTiles = 6, VouchFlag = "elder_blessing",
                Spotted = "The biggest kobold you have ever seen (he comes up to your belt) plants a spear across the passage. \"I am KRAG. Nobody goes down to the Great Egg. Not without the Elder's say. Not without going through Krag.\" His two warriors try very hard to look fierce.",
                Befriended = "Krag looks at you for a long moment, then grunts and steps aside. \"Krag is not scared. Krag is being... polite.\"",
                VouchText = "Krag sees the Elder's mark daubed on your hand and lowers his spear, very slowly, looking deeply disappointed. \"The Elder says you pass. So. You pass.\" He sniffs. \"Krag was going to win.\"" };
            warband.Members.Add(new EnemySpawn("kobold_warboss", 36, 6));
            warband.Members.Add(new EnemySpawn("kobold", 35, 7));
            warband.Members.Add(new EnemySpawn("kobold", 37, 7));
            w.EnemyGroups.Add(warband);
            w.Secrets[new Vector2Int(33, 5)] = new SecretDef
            {
                Id = "warrens_crawl", Sprite = "obj_crawlway", Prompt = "Crawl through the low tunnel", Skill = Rules.Skill.Athletics, DC = 13, PassageTo = new Vector2Int(35, 3),
                Success = "On hands and knees through a kobold-sized tunnel, dragging yourself over loose stones. It comes out right beside the stairs down, behind Krag's warband. Krag is still glaring the wrong way.",
                Failure = "The tunnel narrows to nothing and your shoulders won't fit past the bend. You back out, scraped and dusty. Tomorrow.",
                AlreadyFound = "You crawl back through the low tunnel.",
            };

            var hoard = new ChestDef { Id = "warrens_hoard", Pos = new Vector2Int(7, 5), Copper = 1000 };
            hoard.Items.Add(new KeyValuePair<string, int>("copper_trinket", 2));
            hoard.Items.Add(new KeyValuePair<string, int>("potion_healing", 1));
            w.Chests.Add(hoard);
            Zones[w.Id] = w;

            var s = new ZoneDef
            {
                Id = "egg_shrine", Name = "Shrine of the Great Egg", Map = Maps.EggShrine, Dark = true, DefaultSpawn = new Vector2Int(12, 15), Music = "music_shrine",
                EggPos = new Vector2Int(13, 2),
                EnterNarration = "The deepest cave of all is warm as a summer afternoon. Every surface is covered in offerings: spoons, buttons, bits of glass, a single boot. And on a heap of them at the far end, glowing red as a coal, sits an egg as big as a barrel. It's humming. You can feel it in your teeth.",
            };
            s.Warps.Add(new WarpDef(12, 16, "warrens", 37, 3));
            s.Warps.Add(new WarpDef(13, 16, "warrens", 37, 3));
            s.Signs[new Vector2Int(8, 11)] = "A heap of offerings with a label scratched into a slate:\n\nKETTLE (very shiny, from the BIG shop)\nSPOON (lucky, from OLD MAN)\nCHICKENS (warm, for egg)\n\nThere's a kettle-shaped dent in the pile. Someone has moved the kettle somewhere safer. Snik, probably.";
            s.Signs[new Vector2Int(17, 11)] = "Scratched into the wall, in very careful kobold letters:\n\nTHE GREAT EGG FELL FROM THE SKY\nWE CARRIED IT HOME\nWE KEEP IT WARM\nWE SING TO IT\nIT SINGS BACK";
            var egg = new EnemyGroupDef { Id = "mine_egg", ParleyDialogue = "snik", SightTiles = 7, Unique = true, Label = "Snik and the egg-guards", TalkDC = 13, ScareDC = 15 };
            egg.Members.Add(new EnemySpawn("snik", 13, 5));
            egg.Members.Add(new EnemySpawn("kobold", 10, 6));
            egg.Members.Add(new EnemySpawn("kobold", 17, 6));
            s.EnemyGroups.Add(egg);
            s.Secrets[new Vector2Int(4, 5)] = new SecretDef
            {
                Id = "offering_pile", Sprite = "obj_crate", Prompt = "Dig through the offering pile", Skill = Rules.Skill.Investigation, DC = 11,
                Success = "Under the buttons and the bent nails: a heap of copper coins... and behind a little fence of spoons, a pen holding four very content chickens, each wearing a tiny crown made of bottle caps. They've been fed, watered, and sung to. They've never been happier.",
                Failure = "Buttons. Nails. A sock. Another sock. You lose the thread. Try again tomorrow.",
                AlreadyFound = "The chickens cluck at you, regally.",
                Copper = 400,
            };
            Zones[s.Id] = s;
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
            perrin.Schedule.Add(new ScheduleEntry(8, "tavern", 5, 7, "taking Hearthday off to write letters home (they're mostly about pie)", Hearthday));
            Npcs[perrin.Id] = perrin;

            var ssark = new NpcDef { Id = "ssark", Name = "Elder Ssark", Title = "Elder of the Warrens", Look = "npc_ssark", Dialogue = "ssark" };
            ssark.Schedule.Add(new ScheduleEntry(0, "warrens", 18, 6, "dozing by the door of the great hut"));
            ssark.Schedule.Add(new ScheduleEntry(7, "warrens", 18, 6, "sitting outside the great hut, telling stories to the hatchlings"));
            ssark.Schedule.Add(new ScheduleEntry(7, "warrens", 20, 8, "leading the Hearthday egg-song", Hearthday));
            Npcs[ssark.Id] = ssark;
            var tik = new NpcDef { Id = "tik", Name = "Tik", Title = "Trader of Shinies", Look = "npc_tik", Dialogue = "tik", ShopId = "kobold" };
            tik.Schedule.Add(new ScheduleEntry(0, "warrens", 30, 10, "asleep under the stall, guarding it"));
            tik.Schedule.Add(new ScheduleEntry(6, "warrens", 30, 10, "polishing shinies at the stall"));
            tik.Schedule.Add(new ScheduleEntry(6, "warrens", 31, 10, "haggling extra hard (it's market day, even underground)", Coinday));
            Npcs[tik.Id] = tik;

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
            BuildFestival();
        }
    }
}
