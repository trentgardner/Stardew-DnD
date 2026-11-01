using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 5: the blight reaches known places, one neighbour serves the Queen, and the road climbs to the Emberhorn.
    /// Nothing here may hint at THE SECRET (see LoreTests.NoActTwoTextHintsAtTheSecret).
    /// </summary>
    public static partial class WorldDb
    {
        static void BuildEmberhorn()
        {
            BuildBlightBeasts();
            BuildTeodor();
        }

        // =================================================================== Ash on the Doors: Teodor Brisk, the chandler

        static void BuildTeodor()
        {
            var town = Zones["town"];
            const string q = "ash_on_doors";
            var marks = new[] { (15, 15, "beside the general store's door"), (33, 11, "beside the Sleeping Drake's door"), (9, 29, "beside your own front door") };
            for (int i = 0; i < marks.Length; i++)
            {
                var (x, y, where) = marks[i];
                town.Secrets[new Vector2Int(x, y)] = new SecretDef
                {
                    Id = "ash_mark_" + (i + 1), Sprite = "obj_inscription", Prompt = "Look at the grey mark " + where, Skill = Rules.Skill.Investigation, DC = 10,
                    QuestId = q, QuestStage = 1, SetsFlag = "ash_mark_" + (i + 1),
                    Success = i == 2
                        ? "A hand, pressed flat in ash, on your own door. A long-fingered hand, with a candlemaker's burn scar across the palm. And a smear of something waxy at the edge."
                        : "A hand, pressed flat in ash, right at eye height. The ash is fine and grey and still faintly warm. There's a smear of tallow at the edge of the print.",
                    Failure = "Just a grey smudge. You'd need a closer look in better light. Tomorrow.",
                    AlreadyFound = "The ash hand. Someone has tried to scrub it off and only made it bigger.",
                };
            }
            town.Secrets[new Vector2Int(24, 15)] = new SecretDef
            {
                Id = "ash_stakeout", Sprite = "obj_bush", Prompt = "Wait in the square in the dark", Skill = Rules.Skill.Perception, DC = 13, NightOnly = true,
                QuestId = q, QuestStage = 2, Dialogue = "teodor_caught",
                Success = "", Failure = "Nobody comes. Or nobody you see. You'll try again tomorrow night.",
                AlreadyFound = "The square, quiet now.",
            };

            var teodor = new NpcDef { Id = "teodor", Name = "Teodor Brisk", Title = "Chandler", Look = "npc_teodor", Dialogue = "teodor" };
            teodor.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "at home, with every candle lit"));
            teodor.Schedule.Add(new ScheduleEntry(8, "town", 29, 20, "selling candles by the market stall"));
            teodor.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "at home, with every candle lit"));
            teodor.Schedule.Add(new ScheduleEntry(8, "town", 7, 10, "lighting candles at the temple door", Hearthday));
            foreach (int h in new[] { 0, 8, 18 }) // his fate overrides every part of his day
            {
                teodor.Schedule.Add(new ScheduleEntry(h, "watchhouse", 9, 3, "in the holding cell, very quiet") { RequiresFlag = "teodor_jailed" });
                teodor.Schedule.Add(new ScheduleEntry(h, null, 0, 0, "gone from the valley") { RequiresFlag = "teodor_fled" });
            }
            Npcs[teodor.Id] = teodor;
        }

        // =================================================================== Ember-eyed beasts in known places

        static void BuildBlightBeasts()
        {
            var wolves = new EnemyGroupDef
            {
                Id = "blight_wolves_forest", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the ember-eyed wolves", MinBlight = 2,
                Spotted = "Three wolves stand among the trees where no wolves should be, grey with ash, their eyes glowing like coals in a dying fire.",
                Befriended = "You hold out empty hands and speak low and slow. The lead wolf's burning eyes flicker. It whines, and the pack melts back into the trees.",
            };
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 40, 24));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 42, 25));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 41, 27));
            Zones["forest"].EnemyGroups.Add(wolves);

            var boars = new EnemyGroupDef
            {
                Id = "blight_boars_ford", Style = EncounterStyle.Beasts, TalkDC = 14, ScareDC = 14, Label = "the ember-eyed boars", MinBlight = 2,
                Spotted = "Two boars are tearing up Hobb's turnip field, ash in their bristles, eyes like hot coals. They look up at you together.",
                Befriended = "You back off slowly, talking the whole way. The boars snort smoke, decide you aren't worth it, and trot off toward the hills.",
            };
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 38, 10));
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 41, 11));
            Zones["ford"].EnemyGroups.Add(boars);

            var hounds = new EnemyGroupDef
            {
                Id = "blight_hounds_crater", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the cinder hounds", MinBlight = 3,
                Spotted = "Two hounds pace the crater's rim, breath smoking, as if they're waiting for something to climb out.",
                Befriended = "You stand your ground and speak steadily. The hounds stare past you, toward the mountains, then lope away together.",
            };
            hounds.Members.Add(new EnemySpawn("cinder_hound", 6, 13));
            hounds.Members.Add(new EnemySpawn("cinder_hound", 8, 15));
            Zones["crater"].EnemyGroups.Add(hounds);
        }
    }
}
