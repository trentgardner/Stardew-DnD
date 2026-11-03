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
        }

        /// <summary>The people of the deep shift (called after the capital's people).</summary>
        static void BuildLairNpcs()
        {
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
    }
}
