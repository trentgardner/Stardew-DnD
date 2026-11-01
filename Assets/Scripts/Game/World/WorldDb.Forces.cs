using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 4: the four forces' homes and quests. Every force is right about something and asks a real price.
    /// Nothing here may hint at THE SECRET (see LoreTests.NoActTwoTextHintsAtTheSecret).
    /// </summary>
    public static partial class WorldDb
    {
        static void BuildForces()
        {
            BuildRedoubt();
        }

        // =================================================================== The Crown: Greywater Redoubt

        static void BuildRedoubt()
        {
            var road = Zones["lantern_road"];
            road.Warps.Add(new WarpDef(45, 0, "redoubt", 21, 30) { MinAct = 2 });
            road.Warps.Add(new WarpDef(46, 0, "redoubt", 22, 30) { MinAct = 2 });
            road.Signs[new Vector2Int(44, 2)] = "A painted board on a post: GREYWATER REDOUBT. CROWN BUSINESS ONLY. Underneath, in chalk: AND STEW.";

            // Counted: Holloway's late supply wagon, and the drivers hiding in the farmhouse.
            road.Signs[new Vector2Int(19, 24)] = "A Crown supply wagon, tipped on its side in the ash. Sacks of flour split open, a wheel gone, and big paw-prints everywhere. No blood. The harness was cut, not torn: the drivers let the horses run and ran too.";
            var wagonHounds = new EnemyGroupDef
            {
                Id = "wagon_hounds", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the cinder hounds", SightTiles = 6,
                QuestId = "counted", MinStage = 1, MaxStage = 1, Unique = true,
                Spotted = "Three hounds are worrying at the spilled flour around the wagon. They look up together, eyes like banked coals, and their breath smokes.",
                Befriended = "You keep your voice low and your hands open. The hounds sniff at you, sneeze at the flour, and lope off into the grey together, as if something far away had called them.",
            };
            wagonHounds.Members.Add(new EnemySpawn("cinder_hound", 18, 22));
            wagonHounds.Members.Add(new EnemySpawn("cinder_hound", 22, 23));
            wagonHounds.Members.Add(new EnemySpawn("cinder_hound", 21, 26));
            road.EnemyGroups.Add(wagonHounds);
            road.Secrets[new Vector2Int(33, 22)] = new SecretDef
            {
                Id = "counted_drivers", Sprite = "obj_bush", Prompt = "Call through the farmhouse window", Skill = Rules.Skill.Persuasion, DC = 12,
                QuestId = "counted", QuestStage = 2, AdvanceTo = 3,
                Success = "\"It's safe. The hounds are gone. Holloway sent us.\" A long pause. Then the door scrapes open and two Crown drivers come out blinking, covered in ash, holding a frying pan and a ladle like swords. \"Two days,\" says one. \"Two days on turnips.\" They set off up the road for the checkpoint at a brisk, hungry walk.",
                Failure = "\"That's what the LAST voice said!\" someone shouts from inside, and a turnip bounces off the shutter. Give them a while to calm down. Tomorrow.",
                AlreadyFound = "The farmhouse is empty again, apart from a neat pile of turnip peelings.",
            };

            var z = new ZoneDef
            {
                Act = 2,
                Id = "redoubt", Name = "Greywater Redoubt", Map = Maps.Redoubt, DefaultSpawn = new Vector2Int(21, 29), Music = "music_redoubt",
                EnterNarration = "A Crown outpost, timber palisade and canvas tents, set on a rise north of the road. Soldiers drill in the yard to a sergeant's bark. Woodsmoke, horses and stew. In the north-east corner a ruined beacon tower is roped off, and the soldiers don't look at it.",
            };
            z.Warps.Add(new WarpDef(21, 31, "lantern_road", 45, 1));
            z.Warps.Add(new WarpDef(22, 31, "lantern_road", 46, 1));
            z.Signs[new Vector2Int(5, 12)] = "From up on the lookout you can see the whole valley. The Lantern Road winding west. Thistledown, a smudge of chimney smoke. Grey fields where gold ones should be. And over everything, the Wyrmback, very still.";
            z.Signs[new Vector2Int(32, 13)] = "BY ORDER OF THE LADY-COMMANDER\nNO ENTRY\n\nThe rope is new. Someone has tied a little bundle of dried flowers to it, the way you would at a grave.";
            z.Secrets[new Vector2Int(14, 9)] = new SecretDef
            {
                Id = "redoubt_dice", Sprite = "obj_crate", Prompt = "Join the soldiers' dice game", Skill = Rules.Skill.Insight, DC = 13, Daily = true, Copper = 1500,
                Success = "You watch the faces instead of the dice. The corporal blinks when she's bluffing. You come away fifteen gold richer and with three new friends who want a rematch.",
                Failure = "You lose your stake to a private who swears this is his first game. It is not his first game. Tomorrow.",
                AlreadyFound = "The soldiers are waiting for you to come back with more money.",
            };
            z.Secrets[new Vector2Int(26, 26)] = new SecretDef
            {
                Id = "quartermaster_crate", Sprite = "obj_crate", Prompt = "Check the quartermaster's crates", Skill = Rules.Skill.Investigation, DC = 14,
                Items = { new KeyValuePair<string, int>("potion_greater_healing", 1), new KeyValuePair<string, int>("potion_healing", 2) },
                Success = "Under the crate marked SALT is a crate marked ALSO SALT, and under that one, the medical stores somebody forgot to sign out. Into your pack they go.",
                Failure = "Salt, salt and more salt. You'll find the good crate another day.",
                AlreadyFound = "Salt. The quartermaster has a lot of salt.",
            };

            // Under the Crown's Banner: the Ashsworn hold the old beacon.
            var beacon = new EnemyGroupDef
            {
                Id = "beacon_ashsworn", Style = EncounterStyle.Talk, TalkDC = 17, ScareDC = 16, Label = "the Ashsworn", SightTiles = 6,
                QuestId = "crowns_banner", MinStage = 1, MaxStage = 1, Unique = true,
                Spotted = "Grey-robed figures turn from a fire that burns without smoke. \"She is waking,\" one says, quite calmly. \"Her fire will burn the world clean. You can stand in it, or you can stand aside.\"",
                Befriended = "You talk for a long time. One by one they put down their knives. They don't look relieved. They look like people who have been very tired for a very long time. The soldiers lead them away without a word.",
            };
            beacon.Members.Add(new EnemySpawn("ashsworn_keeper", 33, 10));
            beacon.Members.Add(new EnemySpawn("ashsworn_zealot", 30, 8));
            beacon.Members.Add(new EnemySpawn("ashsworn_zealot", 36, 7));
            beacon.Members.Add(new EnemySpawn("ashsworn_zealot", 31, 10));
            beacon.Members.Add(new EnemySpawn("ashsworn_zealot", 35, 10));
            z.EnemyGroups.Add(beacon);
            z.Secrets[new Vector2Int(33, 8)] = new SecretDef
            {
                Id = "beacon_notes", Sprite = "item_scroll_revivify", Prompt = "Search what they left behind", Skill = Rules.Skill.Investigation, DC = 8,
                QuestId = "crowns_banner", QuestStage = 2, AdvanceTo = 3, Lore = "beacon_orders",
                Success = "Bedrolls, a cold pot, and a sheaf of notes wrapped in oilcloth, written in a dozen different hands.",
                Failure = "Ash and bedrolls. Look again.",
                AlreadyFound = "Bedrolls and a cold pot.",
            };
            Zones[z.Id] = z;

            var marsh = new NpcDef { Id = "marsh", Name = "Lady-Commander Isaine Marsh", Title = "Commander of Greywater Redoubt", Look = "npc_marsh", Dialogue = "marsh" };
            marsh.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep in the command tent"));
            marsh.Schedule.Add(new ScheduleEntry(6, "redoubt", 12, 9, "at the map table, moving pins"));
            marsh.Schedule.Add(new ScheduleEntry(11, "redoubt", 10, 21, "watching the drill with her arms folded"));
            marsh.Schedule.Add(new ScheduleEntry(14, "redoubt", 12, 9, "at the map table, writing reports"));
            marsh.Schedule.Add(new ScheduleEntry(22, null, 0, 0, "asleep in the command tent"));
            marsh.Schedule.Add(new ScheduleEntry(6, "redoubt", 29, 24, "eating with the soldiers at the mess fire, as she does every Hearthday", Hearthday));
            Npcs[marsh.Id] = marsh;

            foreach (var (id, x, y, act) in new[] { ("redoubt_soldier_a", 9, 21, "drilling, badly"), ("redoubt_soldier_b", 33, 14, "guarding the rope, and not looking past it") })
            {
                var s = new NpcDef { Id = id, Name = "Crown soldier", Title = "Greywater Redoubt", Look = "npc_crown_soldier", Dialogue = "crown_guard" };
                s.Schedule.Add(new ScheduleEntry(0, "redoubt", x, y, act));
                s.Schedule.Add(new ScheduleEntry(19, "redoubt", 27, 24, "at the mess fire, complaining about the stew"));
                s.Schedule.Add(new ScheduleEntry(23, "redoubt", x, y, act));
                s.Schedule.Add(new ScheduleEntry(6, "redoubt", 25, 17, "polishing armour for inspection", Stormday));
                Npcs[s.Id] = s;
            }
        }
    }
}
