using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: below Aurelhold. The Undergate Works, the Iron Vaults, the Royal Crypts, the Deep and the grey door.
    /// Dread and suspicion of the Crown are allowed. Nothing here may hint at THE SECRET (LoreTests scans this file);
    /// everything beyond the door lives in *.AfterTheDoor.cs.
    /// </summary>
    public static partial class WorldDb
    {
        /// <summary>World flag: the Undergate is open (warm_city stage 5).</summary>
        public const string UndergateOpen = "undergate_open";

        static void BuildLair()
        {
            BuildUndergate();
            BuildIronVaults();
            BuildRoyalCrypts();
            BuildTheDeep();
        }

        /// <summary>The people of the deep shift (called after the capital's people).</summary>
        static void BuildLairNpcs()
        {
            BuildBeyondTheDoorNpcs();
            var bran = new NpcDef { Id = "bran", Name = "Bran Tolley", Title = "The Deep Shift", Look = "npc_bran", Dialogue = "bran" };
            bran.Schedule.Add(new ScheduleEntry(0, "undergate", 6, 9, "asleep in a bunk, still in his boots") { ForbidFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(6, "undergate", 8, 17, "at the long table, oiling rags") { ForbidFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(20, "undergate", 6, 9, "asleep in a bunk, still in his boots") { ForbidFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(6, "undergate", 15, 5, "scraping the warm pipes (Hearthday)", Hearthday) { ForbidFlag = "bran_home" });
            // Home again.
            bran.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "at home with Mags") { RequiresFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(8, "roots", 10, 6, "sitting on his own front step, face to the light") { RequiresFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(20, null, 0, 0, "at home with Mags") { RequiresFlag = "bran_home" });
            bran.Schedule.Add(new ScheduleEntry(8, "roots", 21, 29, "helping Mags with the washing (Hearthday)", Hearthday) { RequiresFlag = "bran_home" });
            Npcs[bran.Id] = bran;

            var hap = new NpcDef { Id = "hap_mossy", Name = "Hap Mossy", Title = "Forty years on the deep shift", Look = "npc_hap_mossy", Dialogue = "hap_mossy" };
            hap.Schedule.Add(new ScheduleEntry(0, "undergate", 9, 9, "asleep by the bunks"));
            hap.Schedule.Add(new ScheduleEntry(7, "undergate", 32, 22, "tending the cairns"));
            hap.Schedule.Add(new ScheduleEntry(19, "undergate", 9, 13, "sitting by the cook-fire nobody needs"));
            hap.Schedule.Add(new ScheduleEntry(7, "undergate", 32, 19, "chalking the names fresh (Hearthday)", Hearthday));
            Npcs[hap.Id] = hap;

            foreach (var (id, name, day, night, hearth) in new[]
            {
                ("deep_shift_1", "Lew Carrow", new Vector2Int(9, 17), new Vector2Int(7, 9), new Vector2Int(19, 5)),
                ("deep_shift_2", "Dobbin Hale", new Vector2Int(12, 20), new Vector2Int(11, 9), new Vector2Int(12, 17)),
            })
            {
                var n = new NpcDef { Id = id, Name = name, Title = "The Deep Shift", Look = "npc_deep_shift", Dialogue = id };
                n.Schedule.Add(new ScheduleEntry(0, "undergate", night.x, night.y, "asleep in a bunk"));
                n.Schedule.Add(new ScheduleEntry(6, "undergate", day.x, day.y, "on shift"));
                n.Schedule.Add(new ScheduleEntry(20, "undergate", night.x, night.y, "asleep in a bunk"));
                n.Schedule.Add(new ScheduleEntry(6, "undergate", hearth.x, hearth.y, "resting (Hearthday)", Hearthday));
                Npcs[id] = n;
            }
        }

        // =================================================================== the Undergate, and the Works below it

        static void BuildUndergate()
        {
            var roots = Zones["roots"];
            const string shut = "The Undergate: black iron, twice the height of a house, barred and sealed with the King's own seal. It does not open.";
            roots.Warps.Add(new WarpDef(52, 20, "undergate", 1, 14) { RequiresFlag = UndergateOpen, ClosedText = shut });
            roots.Warps.Add(new WarpDef(52, 21, "undergate", 1, 15) { RequiresFlag = UndergateOpen, ClosedText = shut });
            roots.Secrets[new Vector2Int(51, 20)] = new SecretDef
            {
                Id = "undergate_bar", Sprite = "obj_inscription", Prompt = "Heave the great bar (Athletics)", Skill = Rules.Skill.Athletics, DC = 16,
                QuestId = WarmCity.Id, QuestStage = WarmCity.Below, AdvanceTo = WarmCity.Opened,
                Success = "You set your shoulder under the bar and lift. Iron groans, the King's seal cracks down the middle, and the Undergate swings inward on hinges that someone has kept oiled. Warm air breathes out of the dark.",
                Failure = "The Undergate doesn't move. You'll try again tomorrow.",
                AlreadyFound = "The Undergate stands open. Warm air breathes out of the dark.",
            };
            roots.Secrets[new Vector2Int(51, 21)] = new SecretDef
            {
                Id = "undergate_lock", Sprite = "obj_inscription", Prompt = "Work the King's lock (Sleight of Hand)", Skill = Rules.Skill.SleightOfHand, DC = 15,
                QuestId = WarmCity.Id, QuestStage = WarmCity.Below, AdvanceTo = WarmCity.Opened,
                Success = "The lock is old, and fine, and has been opened recently: there's fresh oil in the wards. It gives with a soft click. The seal stays whole, and the gate swings in.",
                Failure = "The Undergate doesn't move. You'll try again tomorrow.",
                AlreadyFound = "The Undergate stands open. Warm air breathes out of the dark.",
            };

            var z = new ZoneDef
            {
                Act = 3,
                Id = "undergate", Name = "The Undergate Works", Map = Maps.Undergate, Dark = true, DefaultSpawn = new Vector2Int(2, 14), Music = "music_undergate",
                BlightWeight = 0f,
                EnterNarration = "The stair goes down a long way, and it gets warmer the whole time. At the bottom, lamplight: a camp of bunks and washing lines, a long table set for twenty, and men in work-leathers who stop and stare at you as if you'd come down from the moon.",
            };
            z.Warps.Add(new WarpDef(0, 14, "roots", 50, 20));
            z.Warps.Add(new WarpDef(0, 15, "roots", 50, 21));
            z.Signs[new Vector2Int(22, 13)] = "KEEPERS' PARADE\n\nInspection at the sixth bell. Boots: polished. Questions: none.";
            z.Secrets[new Vector2Int(15, 6)] = new SecretDef
            {
                Id = "warm_pipes", Sprite = "obj_inscription", Prompt = "Follow the warm pipes (Investigation)", Skill = Rules.Skill.Investigation, DC = 14, Lore = "warm_pipes",
                Success = "Every pipe runs up, toward the city: the ever-lamps, the Steamhouse, the palace hearths. None of them comes from a furnace. They come out of the rock, from below, warm as a hand. The deep shift scrapes them, oils them, and never asks what's on the other end.",
                Failure = "Pipes, warm, everywhere. Your eyes can't follow them through the dark today.",
                AlreadyFound = "The warm pipes, running up toward the city from somewhere far below.",
            };
            z.Lore.Add(new LoreSpotDef { Id = "deep_shift_rest", Pos = new Vector2Int(32, 20), Prompt = "Read the names on the cairns", Sprite = "obj_inscription" });
            var keepers = new EnemyGroupDef
            {
                Id = "undergate_keepers", Style = EncounterStyle.Talk, TalkDC = 17, ScareDC = 18, Label = "the Keepers of the Undergate", Unique = true, ResolvedFlag = "keepers_resolved",
                Spotted = "Soldiers in old-fashioned Crown plate fall in across the way, a grey-haired captain at their head. \"Nobody comes down,\" says Ser Orla Hadley. \"Nobody goes on. Those are the orders, and I've kept them for forty years.\"",
                Befriended = "You ask her what's beyond the vaults. She opens her mouth, and closes it. \"I was never told,\" she says at last. \"None of us were. You keep it, you don't ask.\" She steps aside. \"Go on, then. Come back and tell me.\"",
                VouchFlag = "banner_crown",
                VouchText = "Ser Orla Hadley sees the Crown's colours on you, and the Lady-Commander's seal, and frowns at both for a long moment. \"Marsh's people. She never sent anyone down before.\" She steps aside. \"Go on, then. Come back and tell me what's there. Nobody ever has.\"",
            };
            keepers.Members.Add(new EnemySpawn("crown_champion", 25, 13));
            keepers.Members.Add(new EnemySpawn("crown_knight", 24, 13));
            keepers.Members.Add(new EnemySpawn("crown_knight", 26, 13));
            keepers.Members.Add(new EnemySpawn("crown_battlemage", 25, 12));
            z.EnemyGroups.Add(keepers);
            z.DoorSigns[new Vector2Int(25, 11)] = new BuildingSign("The Keepers' Post", "THE KEEPERS OF THE UNDERGATE\n\nBy royal warrant. Posted for life.\nNO ONE GOES ON.");
            Zones[z.Id] = z;
        }

        // =================================================================== the Iron Vaults

        static void BuildIronVaults()
        {
            var under = Zones["undergate"];
            under.Warps.Add(new WarpDef(42, 14, "iron_vaults", 1, 13));
            under.Warps.Add(new WarpDef(42, 15, "iron_vaults", 1, 14));

            var z = new ZoneDef
            {
                Act = 3,
                Id = "iron_vaults", Name = "The Iron Vaults", Map = Maps.IronVaults, Dark = true, DefaultSpawn = new Vector2Int(2, 13), Music = "music_undergate",
                BlightWeight = 0f,
                EnterNarration = "Iron. Iron doors, iron floors, iron bands across the rock, and set in every wall a black stone carved with the same word, over and over, in a script older than the Crown: HOLD.",
            };
            z.Warps.Add(new WarpDef(0, 13, "undergate", 41, 14));
            z.Warps.Add(new WarpDef(0, 14, "undergate", 41, 15));
            z.Warps.Add(new WarpDef(38, 13, "royal_crypts", 34, 12));
            z.Warps.Add(new WarpDef(38, 14, "royal_crypts", 34, 13));
            z.Secrets[new Vector2Int(14, 4)] = new SecretDef
            {
                Id = "ward_stones", Sprite = "obj_inscription", Prompt = "Read the ward-stones (Arcana)", Skill = Rules.Skill.Arcana, DC = 15, Lore = "ward_stones",
                Success = "The wards are strong, and old, and every one of them faces the same way: inward and down. Nothing here was built to keep anyone out.",
                Failure = "HOLD, HOLD, HOLD. The word swims in front of your eyes. You'll try again tomorrow.",
                AlreadyFound = "HOLD, on every stone. Facing inward. Facing down.",
            };

            var pz = new PuzzleDef();
            pz.Pieces.Add(new PuzzlePiece { Id = "ward_lamp", Kind = PieceKind.Light, Pos = new Vector2Int(21, 2), Dir = 2 });
            pz.Pieces.Add(new PuzzlePiece { Id = "mirror_1", Kind = PieceKind.Mirror, Pos = new Vector2Int(21, 8), Dir = 0,
                Hint = "The lamp's light wants to reach the black crystal over the far door. Turn the mirrors.", HintSkill = Rules.Skill.Arcana, HintDC = 13 });
            pz.Pieces.Add(new PuzzlePiece { Id = "mirror_2", Kind = PieceKind.Mirror, Pos = new Vector2Int(29, 8), Dir = 0 });
            pz.Pieces.Add(new PuzzlePiece { Id = "mirror_3", Kind = PieceKind.Mirror, Pos = new Vector2Int(29, 20), Dir = 1 });
            pz.Pieces.Add(new PuzzlePiece { Id = "crystal", Kind = PieceKind.Crystal, Pos = new Vector2Int(23, 20) });
            pz.Pieces.Add(new PuzzlePiece { Id = "vault_gate", Kind = PieceKind.Gate, Pos = new Vector2Int(33, 13), OpensWhen = new[] { "crystal" }, Latches = true });
            z.Puzzle = pz;

            var sentinels = new EnemyGroupDef
            {
                Id = "vault_sentinels", Style = EncounterStyle.Beasts, TalkSkill = Rules.Skill.Arcana, TalkDC = 16, ScareDC = 99, Label = "the iron sentinels",
                Spotted = "Iron figures step out of the alcoves along the gallery, one after another, with a sound like a forge being dropped down a stair. Each has one word cut into its chest.",
                Befriended = "You find the order cut into their chests, HOLD, and add a word to it in the same script: DONE. They fold down where they stand.",
            };
            foreach (var (x, y) in new[] { (8, 13), (10, 14), (12, 13), (14, 14), (16, 13) }) sentinels.Members.Add(new EnemySpawn("iron_sentinel", x, y));
            z.EnemyGroups.Add(sentinels);
            var guardian = new EnemyGroupDef
            {
                Id = "vault_guardian", Style = EncounterStyle.Beasts, TalkSkill = Rules.Skill.Arcana, TalkDC = 18, ScareDC = 99, Label = "the stone guardian", Unique = true,
                Spotted = "A statue by the far door turns its head. Stone grinds on stone. It steps down from its plinth.",
                Befriended = "You speak the ward-word back to it. The guardian considers this for a long, grinding moment, then steps into an alcove and is a statue again.",
            };
            guardian.Members.Add(new EnemySpawn("stone_guardian", 31, 16));
            z.EnemyGroups.Add(guardian);
            Zones[z.Id] = z;
        }

        // =================================================================== the Deep

        static void BuildTheDeep()
        {
            var crypts = Zones["royal_crypts"];
            crypts.Warps.Add(new WarpDef(18, 23, "the_deep", 24, 33));

            var z = new ZoneDef
            {
                Act = 3,
                Id = "the_deep", Name = "The Deep", Map = Maps.TheDeep, Dark = true, DefaultSpawn = new Vector2Int(24, 33), Music = "music_deep",
                BlightWeight = 0f, DragonStays = false,
                EnterNarration = "The stair opens onto a floor so wide the lamplight doesn't reach the other side. Around its edge, in a ring, lie heads of stone, each as big as a house, their chins on the floor and their eyes shut. Dragons. Twelve of them. Chains as thick as a man run from twelve iron rings across the floor to the far wall, where there is a door. It's grey. You've seen it before. You've all seen it before.",
            };
            z.Warps.Add(new WarpDef(23, 35, "royal_crypts", 18, 22));
            z.Warps.Add(new WarpDef(24, 35, "royal_crypts", 18, 22));
            z.Lore.Add(new LoreSpotDef { Id = "the_great_ones_below", Pos = new Vector2Int(24, 26), Prompt = "Look around the ring", Sprite = "obj_inscription" });
            var gaoler = new EnemyGroupDef
            {
                Id = "gaoler", Style = EncounterStyle.Talk, TalkSkill = Rules.Skill.Deception, TalkDC = 18, ScareDC = 99, Label = "the Iron Gaoler", Unique = true, SightTiles = 9,
                ResolvedFlag = "gaoler_resolved",
                Spotted = "Before the grey door, something moves that you took for part of the wall: a figure of black iron three times your height, chains wound round its arms. Its face is a blank iron plate with one word cut into it. HOLD.",
                Befriended = "You give the order in the King's own cadence. The Gaoler's head turns, slowly, to the door, and back to you. Then it kneels, the way it must have knelt a thousand times, and is still.",
            };
            gaoler.Members.Add(new EnemySpawn("iron_gaoler", 24, 7));
            z.EnemyGroups.Add(gaoler);
            Zones[z.Id] = z;
        }

        // =================================================================== the Royal Crypts

        static void BuildRoyalCrypts()
        {
            var capital = Zones["capital"];
            capital.Warps.Add(new WarpDef(6, 14, "royal_crypts", 2, 3) { RequiresFlag = "crypt_stair_open", ClosedText = "Iron, and the King's seal. It doesn't open from this side." });

            var z = new ZoneDef
            {
                Act = 3,
                Id = "royal_crypts", Name = "The Royal Crypts", Map = Maps.RoyalCrypts, Dark = true, DefaultSpawn = new Vector2Int(33, 12), Music = "music_crypts",
                BlightWeight = 0f,
                EnterNarration = "The Kings of Aurelhold lie in a long hall under the Lantern Cathedral, each on a tomb of white stone with their effigy on top, hands folded on their chests. Every one of those stone hands is bare on the same finger.",
            };
            z.Warps.Add(new WarpDef(35, 12, "iron_vaults", 37, 13));
            z.Warps.Add(new WarpDef(35, 13, "iron_vaults", 37, 14));
            z.Warps.Add(new WarpDef(1, 2, "capital", 6, 15) { Narration = "The stair winds up and up, and comes out under the Lantern Cathedral, by the crypt door, in the warm air of the city." });
            for (int x = 7; x < 30; x += 4)
            {
                z.Signs[new Vector2Int(x, 5)] = "A King of Aurelhold, in white stone, hands folded on his chest. The ring finger is bare. HE KEPT THE WATCH.";
                z.Signs[new Vector2Int(x, 18)] = "A King of Aurelhold, in white stone. The name is worn, but the words under it are clear: HE KEPT THE WATCH.";
            }
            z.Signs[new Vector2Int(31, 1)] = "A plain tomb among the Kings, with no effigy at all: DAME YSOLDE, LAST OF THE OATHBOUND. Something has been scratched under the name.";
            z.Secrets[new Vector2Int(4, 7)] = new SecretDef
            {
                Id = "kings_path", Sprite = "obj_inscription", Prompt = "Study the clean path in the dust (Survival)", Skill = Rules.Skill.Survival, DC = 14, Lore = "kings_path",
                Success = "The clean path through the dust comes down the cathedral stair, runs past every King's tomb without stopping, and goes on down. One pair of feet, once a year, for a very long time.",
                Failure = "Dust, and a path through it. You can't read more today.",
                AlreadyFound = "The clean path, going down.",
            };
            string ideal = "Among the Kings, one plain tomb: DAME YSOLDE, LAST OF THE OATHBOUND. Under the name, not carved but scratched, as if with a knife-point by someone kneeling:\n\n\"" + ThirdIdeal + "\"";
            z.Secrets[new Vector2Int(30, 2)] = new SecretDef
            {
                Id = "third_ideal_history", Sprite = "obj_inscription", Prompt = "Read the scratched words (History)", Skill = Rules.Skill.History, DC = 15,
                SetsFlag = OathThird, Lore = "third_ideal", Success = ideal,
                Failure = "The scratches are faint and shallow. You can't make them out today.",
                AlreadyFound = "\"" + ThirdIdeal + "\"",
            };
            z.Secrets[new Vector2Int(32, 2)] = new SecretDef
            {
                Id = "third_ideal_religion", Sprite = "obj_altar", Prompt = "Kneel and say the Second Ideal (Religion)", Skill = Rules.Skill.Religion, DC = 10,
                RequiresFlag = OathSecond, SetsFlag = OathThird, Lore = "third_ideal",
                Success = "You kneel and say the old words: \"" + SecondIdeal + "\" The dust on the tomb stirs, as if someone breathed out, and the scratches under the name come clear. " + ideal,
                Failure = "The words catch in your throat. Not today.",
                AlreadyFound = "\"" + ThirdIdeal + "\"",
            };

            var pz = new PuzzleDef();
            pz.Pieces.Add(new PuzzlePiece { Id = "plinth_1", Kind = PieceKind.Plate, Pos = new Vector2Int(10, 10) });
            pz.Pieces.Add(new PuzzlePiece { Id = "plinth_2", Kind = PieceKind.Plate, Pos = new Vector2Int(26, 10) });
            pz.Pieces.Add(new PuzzlePiece { Id = "lid_1", Kind = PieceKind.Block, Pos = new Vector2Int(10, 12),
                Hint = "Two plinths stand empty, and their lids lie on the floor beside them. Somebody meant them to go back.", HintSkill = Rules.Skill.History, HintDC = 13 });
            pz.Pieces.Add(new PuzzlePiece { Id = "lid_2", Kind = PieceKind.Block, Pos = new Vector2Int(26, 12) });
            pz.Pieces.Add(new PuzzlePiece { Id = "crypt_gate", Kind = PieceKind.Gate, Pos = new Vector2Int(18, 20), OpensWhen = new[] { "plinth_1", "plinth_2" }, Latches = true });
            z.Puzzle = pz;
            Zones[z.Id] = z;
        }
    }
}
