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
            BuildEmberhornRoad();
            BuildEmberhornInside();
        }

        // =================================================================== Inside the Emberhorn: the Throat, the Ring Chamber, the Heart

        static void BuildEmberhornInside()
        {
            var throat = new ZoneDef
            {
                Act = 2,
                Id = "emberhorn_throat", Name = "The Throat of the Emberhorn", Map = Maps.EmberhornThroat, Dark = true, DefaultSpawn = new Vector2Int(1, 11), Music = "music_throat",
                BlightWeight = 0f,
                EnterNarration = "Inside, the mountain is a furnace. Rivers of lava run in channels older than any road, lighting the stone from below in red and gold. The air shimmers. Somewhere ahead, deep and slow, something breathes, and the whole mountain breathes with it.",
            };
            throat.Warps.Add(new WarpDef(0, 11, "emberhorn_road", 58, 14));
            throat.Warps.Add(new WarpDef(0, 12, "emberhorn_road", 58, 14));
            throat.Warps.Add(new WarpDef(39, 11, "emberhorn_heart", 1, 10));
            throat.Warps.Add(new WarpDef(39, 12, "emberhorn_heart", 1, 11));
            throat.Lore.Add(new LoreSpotDef { Id = "iron_ring", Pos = new Vector2Int(18, 4), Prompt = "Look at the great iron ring in the rock", Sprite = "obj_inscription" });
            throat.Secrets[new Vector2Int(26, 19)] = new SecretDef
            {
                Id = "oathbound_cache", Sprite = "obj_crate", Prompt = "Search the fallen knight's cairn", Skill = Rules.Skill.Perception, DC = 15,
                Items = { new KeyValuePair<string, int>("potion_greater_healing", 3) },
                Success = "Under a cairn of black stones, a knight's pack, wrapped in oilcloth that hasn't rotted because nothing rots here: three potions, and a sword-and-lantern badge. Someone stood here before you.",
                Failure = "Black stones, black dust, heat. You'll look again when your eyes stop watering.",
                AlreadyFound = "The cairn, rebuilt. You left the badge on top.",
            };
            var sal = new EnemyGroupDef
            {
                Id = "throat_salamanders", Style = EncounterStyle.Talk, TalkDC = 16, ScareDC = 16, Label = "the salamanders", Unique = true,
                Spotted = "Two salamanders rise out of the lava channel, spears glowing, coils hissing on the hot stone. \"Turn back, warm-blood. The Keeper sleeps badly.\"",
                Befriended = "You speak to them as guests in their home. The salamanders exchange a long look, then slide back into the lava. \"Go softly,\" one says. \"It does not know friend from fire any more.\"",
            };
            sal.Members.Add(new EnemySpawn("salamander", 15, 14));
            sal.Members.Add(new EnemySpawn("salamander", 17, 16));
            throat.EnemyGroups.Add(sal);
            var elem = new EnemyGroupDef
            {
                Id = "throat_elemental", Style = EncounterStyle.Beasts, TalkSkill = Rules.Skill.Arcana, TalkDC = 17, ScareDC = 17, Label = "the fire elemental", Unique = true,
                Spotted = "The lava ahead stands up. A living bonfire in the rough shape of a person turns toward you, crackling.",
                Befriended = "You find the thread of magic that holds it together and ease it, gently, the way you'd bank a fire for the night. The elemental sinks back into the lava with a long, contented sigh.",
            };
            elem.Members.Add(new EnemySpawn("fire_elemental", 28, 11));
            throat.EnemyGroups.Add(elem);
            Zones[throat.Id] = throat;

            var heart = new ZoneDef
            {
                Act = 2,
                Id = "emberhorn_heart", Name = "The Heart of the Emberhorn", Map = Maps.EmberhornHeart, Dark = true, DefaultSpawn = new Vector2Int(2, 10), Music = "music_heart",
                BlightWeight = 0f, DragonStays = false,
                EnterNarration = "The tunnel opens into the mountain's heart: a caldera of black glass, ringed with lava, open to a sky of smoke far above. In the middle, coiled around nothing, is a dragon. It is enormous. It is grey at the edges, like paper held too close to a candle. And it is awake.",
            };
            heart.Warps.Add(new WarpDef(0, 10, "emberhorn_throat", 38, 11));
            heart.Warps.Add(new WarpDef(0, 11, "emberhorn_throat", 38, 12));
            var warden = new EnemyGroupDef
            {
                Id = "warden", Style = EncounterStyle.Talk, ParleyDialogue = "warden_parley", Label = "the Warden of the Emberhorn", Unique = true, SightTiles = 9,
                TalkDC = 99, ScareDC = 99, // it can't be talked or scared off: the parley always ends in a fight
                Spotted = "The Warden's head rises. Its eyes find you.",
                Befriended = "",
            };
            warden.Members.Add(new EnemySpawn("warden", 15, 8));
            heart.EnemyGroups.Add(warden);
            Zones[heart.Id] = heart;
        }

        // =================================================================== The Emberhorn Road

        /// <summary>World flag: the high road east of Cragfoot is open (Answers stage 6).</summary>
        public const string EmberhornOpen = "emberhorn_open";
        /// <summary>World flag: your side's ally has given the party their gift (once for the whole party).</summary>
        public const string AllyGift = "ally_gift";

        /// <summary>World flag: the party has stood at the Emberhorn's gate (Answers 8): the way in is open.</summary>
        public const string EmberhornGate = "emberhorn_gate";

        /// <summary>World flag: your side's ally has gone ahead to the Heart (set once the gate is open; only your side's).</summary>
        public static string AllyAtHeart(string banner) => "at_heart:" + banner;

        /// <summary>Once the gate is open, the ally of the side you stand with goes ahead to wait at the Heart.</summary>
        public static void SendAllyAhead(WorldState w)
        {
            if (w == null || !w.Flag(EmberhornGate) || w.CommittedFaction == null) return;
            string banner = "banner_" + w.CommittedFaction;
            if (w.Flag(banner)) w.SetFlag(AllyAtHeart(banner));
        }

        static void BuildEmberhornRoad()
        {
            var crag = Zones["cragfoot"];
            const string closed = "The high road east is buried under a fresh rockslide, still smoking. The hunters say it'll take a reason, and a crew, to clear it.";
            crag.Warps.Add(new WarpDef(45, 15, "emberhorn_road", 1, 15) { MinAct = 2, RequiresFlag = EmberhornOpen, ClosedText = closed });
            crag.Warps.Add(new WarpDef(45, 16, "emberhorn_road", 1, 16) { MinAct = 2, RequiresFlag = EmberhornOpen, ClosedText = closed });

            var z = new ZoneDef
            {
                Act = 2,
                Id = "emberhorn_road", Name = "The Emberhorn Road", Map = Maps.EmberhornRoad, DefaultSpawn = new Vector2Int(2, 15), Music = "music_emberhorn",
                BlightWeight = 0.8f,
                EnterNarration = "The high road climbs east into the Wyrmback, switchbacking over slopes of grey ash. Ahead, filling half the sky, is the Emberhorn: a mountain with a crown of smoke and a glow at its peak like a coal someone forgot to bank. It's warm here. Too warm, for autumn.",
            };
            z.Warps.Add(new WarpDef(0, 15, "cragfoot", 44, 15));
            z.Warps.Add(new WarpDef(0, 16, "cragfoot", 44, 16));
            z.Warps.Add(new WarpDef(59, 14, "emberhorn_throat", 1, 11) { RequiresFlag = EmberhornGate, ClosedText = "The great door breathes hot air in your face. Look up at the mountain first: you should know what you're walking into." });
            z.DoorSigns[new Vector2Int(59, 14)] = new BuildingSign("The Emberhorn's Gate", "A door carved into the mountain's flank, taller than a tower, its edges glowing faintly like the rim of a cooling pot. It stands open. The air that comes out of it is hot, and slow, like breath.");
            z.Signs[new Vector2Int(12, 18)] = "The Last Camp's fire. Someone keeps it fed day and night. Nobody says why, and nobody lets it go out.";
            z.Signs[new Vector2Int(44, 8)] = "The Grey Vigil's fire burns without smoke, and gives no warmth at all when you hold out your hands.";

            // The Oathbound waystation: the Second Ideal.
            string ideal = "Under the ash, carved deep into the altar stone in the same old letters as the watchtower's:\n\n\"" + SecondIdeal + "\"\n\nBelow it, the sword and lantern of the Oathbound, and a line of names, too worn to read, of those who stood here.";
            z.Secrets[new Vector2Int(24, 5)] = new SecretDef
            {
                Id = "second_ideal_history", Sprite = "obj_inscription", Prompt = "Read the worn carving (History)", Skill = Rules.Skill.History, DC = 14,
                SetsFlag = OathSecond, Lore = "second_ideal", Success = ideal,
                Failure = "Ash has filled the letters. You can't make them out today.",
                AlreadyFound = "\"" + SecondIdeal + "\"",
            };
            z.Secrets[new Vector2Int(26, 5)] = new SecretDef
            {
                Id = "second_ideal_religion", Sprite = "obj_altar", Prompt = "Kneel and say the First Ideal (Religion)", Skill = Rules.Skill.Religion, DC = 10,
                RequiresFlag = "secret:tower_inscription", SetsFlag = OathSecond, Lore = "second_ideal",
                Success = "You kneel and say the old words: \"" + FirstIdeal + "\" The ash on the altar stirs, as if something breathed out, and the carving beneath it is clear. " + ideal,
                Failure = "The words catch in your throat. Not today.",
                AlreadyFound = "\"" + SecondIdeal + "\"",
            };
            z.Secrets[new Vector2Int(22, 23)] = new SecretDef
            {
                Id = "grey_grove", Sprite = "obj_rock", Prompt = "Look at the scorched trees", Skill = Rules.Skill.Nature, DC = 14, Lore = "grey_grove",
                Success = "The trees didn't burn. They greyed: bark, leaf and root, all at once, standing exactly where they grew, as if something had breathed on them from very far away.",
                Failure = "Dead trees on a dead slope. You can't read more than that today.",
                AlreadyFound = "The grey grove, standing as if nothing happened.",
            };
            z.Secrets[new Vector2Int(57, 13)] = new SecretDef
            {
                Id = "emberhorn_gate", Sprite = "obj_inscription", Prompt = "Look up at the mountain", Skill = Rules.Skill.Perception, DC = 5,
                QuestId = Answers.Id, QuestStage = Answers.OnTheRoad, AdvanceTo = Answers.Gate,
                Success = "A door, carved into the mountain's flank, taller than a tower, its edges glowing faintly like the rim of a cooling pot. It stands open. Hot air breathes out of it, slow, and in, slow. Somewhere deep inside, something very large is asleep.",
                Failure = "Smoke stings your eyes. You'll look again.",
                AlreadyFound = "The great door, open, breathing.",
            };

            var vigil = new EnemyGroupDef
            {
                Id = "grey_vigil", Style = EncounterStyle.Talk, TalkDC = 17, ScareDC = 16, Label = "the Grey Vigil", Unique = true,
                Spotted = "Ashsworn in grey sit around a smokeless fire on the slope, facing the mountain. One stands as you come near. \"You're on the road to the waking,\" she says. \"Turn back, or walk it with us.\"",
                Befriended = "You talk until the fire burns low. They don't change their minds, not all the way. But they put down their knives, and they let you pass, and one of them, very quietly, says good luck.",
            };
            vigil.Members.Add(new EnemySpawn("ashsworn_keeper", 44, 10));
            vigil.Members.Add(new EnemySpawn("ashsworn_zealot", 42, 9));
            vigil.Members.Add(new EnemySpawn("ashsworn_zealot", 46, 9));
            vigil.Members.Add(new EnemySpawn("ashsworn_zealot", 43, 7));
            z.EnemyGroups.Add(vigil);
            var hounds = new EnemyGroupDef
            {
                Id = "road_cinder_hounds", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the cinder hounds",
                Spotted = "Three hounds lope down the ash slope toward the road, smoke curling from their jaws.",
                Befriended = "You stand still and speak to them low and steady. The hounds circle once, sniffing, then turn uphill toward the mountain as if called.",
            };
            hounds.Members.Add(new EnemySpawn("cinder_hound", 33, 23));
            hounds.Members.Add(new EnemySpawn("cinder_hound", 35, 24));
            hounds.Members.Add(new EnemySpawn("cinder_hound", 37, 22));
            z.EnemyGroups.Add(hounds);
            Zones[z.Id] = z;

            // Your side's ally waits at the Last Camp (only the one you stand with).
            foreach (var (id, name, title, look, banner) in new[]
            {
                ("ally_holloway", "Sergeant Dace Holloway", "Sent by the Crown", "npc_holloway", "banner_crown"),
                ("ally_perrin", "Perrin Quillby", "Sent by the Academy", "npc_perrin", "banner_academy"),
                ("ally_thimble", "Mister Thimble", "Sent by the Fae Courts", "npc_thimble", "banner_fae"),
                ("ally_odo", "Odo Fenn", "Sent by the Wyrmwatch", "npc_odo", "banner_hunters"),
            })
            {
                var n = new NpcDef { Id = id, Name = name, Title = title, Look = look, Dialogue = id };
                n.Schedule.Add(new ScheduleEntry(0, "emberhorn_road", 10, 17, "keeping the Last Camp's fire fed") { RequiresFlag = banner, ForbidFlag = EmberhornGate });
                n.Schedule.Add(new ScheduleEntry(0, "emberhorn_road", 11, 18, "sitting up late by the fire, watching the mountain", Hearthday) { RequiresFlag = banner, ForbidFlag = EmberhornGate });
                // Once the gate is open they go ahead of you, to wait at the Heart's threshold.
                n.Schedule.Add(new ScheduleEntry(0, "emberhorn_heart", 3, 12, "waiting at the edge of the Heart, very still") { RequiresFlag = AllyAtHeart(banner) });
                n.Schedule.Add(new ScheduleEntry(0, "emberhorn_heart", 3, 12, "waiting at the edge of the Heart, very still", Hearthday) { RequiresFlag = AllyAtHeart(banner) });
                Npcs[id] = n;
            }
        }

        // =================================================================== Ash on the Doors: Teodor Brisk, the chandler

        /// <summary>Set when Teodor is jailed or has fled: his stall stands empty.</summary>
        public const string TeodorAway = "teodor_away";

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
            // Act II only (Act I stays light), and only while he's still free to keep his stall.
            teodor.Schedule.Add(new ScheduleEntry(8, "town", 29, 20, "selling candles by the market stall") { RequiresFlag = ActTwoFlag, ForbidFlag = TeodorAway });
            teodor.Schedule.Add(new ScheduleEntry(18, null, 0, 0, "at home, with every candle lit"));
            teodor.Schedule.Add(new ScheduleEntry(8, "town", 7, 10, "lighting candles at the temple door", Hearthday) { RequiresFlag = ActTwoFlag, ForbidFlag = TeodorAway });
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
