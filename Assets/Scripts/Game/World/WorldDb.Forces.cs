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
            BuildAcademyForce();
            BuildGlade();
            BuildCragfoot();
        }

        // =================================================================== The Wyrmwatch: Cragfoot, the lodge and the Ashmaw's lair

        static void BuildCragfoot()
        {
            var road = Zones["lantern_road"];
            road.Warps.Add(new WarpDef(59, 14, "cragfoot", 1, 15) { MinAct = 2 });
            road.Warps.Add(new WarpDef(59, 15, "cragfoot", 1, 16) { MinAct = 2 });

            var z = new ZoneDef
            {
                Act = 2,
                Id = "cragfoot", Name = "Cragfoot", Map = Maps.Cragfoot, DefaultSpawn = new Vector2Int(2, 15), Music = "music_cragfoot",
                EnterNarration = "A hamlet of stone cottages at the very foot of the Wyrmback, where the road gives up and the goat tracks begin. Smoke from six chimneys. A long timber hall with antlers over the door: the Wyrmwatch Lodge. And an empty goat pen with its gate hanging open.",
            };
            z.Warps.Add(new WarpDef(0, 15, "lantern_road", 58, 14));
            z.Warps.Add(new WarpDef(0, 16, "lantern_road", 58, 15));
            z.Warps.Add(new WarpDef(22, 0, "ashmaw_lair", 12, 18) { MinAct = 2 });
            z.Warps.Add(new WarpDef(23, 0, "ashmaw_lair", 13, 18) { MinAct = 2 });
            z.DoorSigns[new Vector2Int(31, 10)] = new BuildingSign("The Wyrmwatch Lodge", "THE WYRMWATCH LODGE\n\nWe watch so you can sleep.\n\nThe door is barred from inside. A hand-lettered card in the window: HUNTERS ON PATROL. KNOCK FOR THE HUNTMASTER (she's out front).");
            z.DoorSigns[new Vector2Int(6, 8)] = new BuildingSign("The Hale Cottage", "Mother Hale's cottage. A dog's water bowl by the step, full and untouched.");
            z.DoorSigns[new Vector2Int(13, 9)] = new BuildingSign("A Shepherd's Cottage", "Shuttered tight. A sign: GONE TO THE LOWLANDS WITH THE FLOCK. BACK WHEN IT'S SAFE.");
            z.DoorSigns[new Vector2Int(6, 22)] = new BuildingSign("The Fenn Cottage", "A hunter's cottage. Odo Fenn's name is carved over the door, badly, by Odo Fenn.");
            z.DoorSigns[new Vector2Int(13, 23)] = new BuildingSign("A Cheesemaker's Cottage", "The smell of goat's cheese, strong enough to lean on. Nobody's answering.");
            z.DoorSigns[new Vector2Int(18, 27)] = new BuildingSign("The Shrine-Keeper's Cottage", "A blue door with a hearth-knot painted on it, for luck. The paint is new.");
            z.DoorSigns[new Vector2Int(29, 26)] = new BuildingSign("A Weaver's Cottage", "Somebody is weaving inside. The loom doesn't stop when you knock.");
            z.Signs[new Vector2Int(19, 6)] = "The old watchtower is a stump now, but the view from beside it goes all the way up the Wyrmback. On a clear day, the hunters say, you can see the mountain breathe.";
            z.Lore.Add(new LoreSpotDef { Id = "trophy_wall", Pos = new Vector2Int(30, 11), Prompt = "Read the hunters' tallies by the door", Sprite = "obj_inscription" });

            z.Secrets[new Vector2Int(16, 6)] = new SecretDef
            {
                Id = "watchtower_cache", Sprite = "obj_crate", Prompt = "Check under the watchtower's loose stones", Skill = Rules.Skill.Perception, DC = 14,
                Items = { new KeyValuePair<string, int>("potion_greater_healing", 1), new KeyValuePair<string, int>("rations", 3) },
                Success = "A hunters' cache, wrapped in oilcloth: a potion, trail rations and a note. 'For whoever needs it. Put something back if you can.'",
                Failure = "Stones, moss, a very annoyed lizard. Look again another day.",
                AlreadyFound = "The cache, and the note. Somebody has put back a single sock.",
            };
            z.Secrets[new Vector2Int(38, 5)] = new SecretDef
            {
                Id = "lodge_carving", Sprite = "obj_crate", Prompt = "Search the woodpile behind the lodge", Skill = Rules.Skill.Investigation, DC = 13, Copper = 2000,
                Success = "Under the logs, a little wooden dragon, carved with great care and never painted, and a purse of coins. The tag on the purse says: FOR BRYN'S BIRTHDAY. The carving has no tag at all.",
                Failure = "Logs, logs and a spider who has made her peace with logs. Another day.",
                AlreadyFound = "The woodpile, a little tidier.",
            };

            // Ember Eyes: keep watch, the wolves come, Odo is hurt, and one of them wore a collar.
            const string e = "ember_eyes";
            z.Secrets[new Vector2Int(38, 21)] = new SecretDef
            {
                Id = "pen_watch", Sprite = "obj_bush", Prompt = "Keep watch over the empty pen", Skill = Rules.Skill.Perception, DC = 10, NightOnly = true,
                QuestId = e, QuestStage = 1, AdvanceTo = 2,
                Success = "You wait in the dark by the pen. An hour. Two. Then, along the palisade, three pairs of coals open in the blackness, and something drops over the fence without a sound. \"WOLVES!\" Odo yells, somewhere to your left.",
                Failure = "You doze off, and wake to a cold dawn and nothing at all. Try again tonight.",
                AlreadyFound = "The pen, empty.",
            };
            var wolves = new EnemyGroupDef
            {
                Id = "cragfoot_wolves", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the ember-eyed wolves", SightTiles = 8,
                QuestId = e, MinStage = 2, MaxStage = 2, Unique = true,
                Spotted = "Three wolves the size of ponies, grey with ash, eyes burning like banked coals. They don't growl. They just come.",
                Befriended = "You stand your ground and speak low and steady, and something in the biggest wolf's eyes flickers. It whines, and backs off, and the others follow it over the palisade into the night. \"They'll be back,\" says the Huntmaster behind you. \"They always come back.\"",
            };
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 40, 23));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 36, 24));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 41, 26));
            z.EnemyGroups.Add(wolves);
            z.Secrets[new Vector2Int(39, 22)] = new SecretDef
            {
                Id = "wolf_collar", Sprite = "obj_inscription", Prompt = "Search where the wolves came through", Skill = Rules.Skill.Investigation, DC = 12,
                QuestId = e, QuestStage = 4, AdvanceTo = 5,
                Success = "Caught on a splinter of the palisade: a worn leather collar, with a little brass tag. BRAMBLE. Someone in Cragfoot loved this dog, before the ash came and its eyes caught fire.",
                Failure = "Mud, tufts of grey fur, and nothing else you can find in this light. Try again tomorrow.",
                AlreadyFound = "The splinter where the collar caught.",
            };

            // The Long Hunt: two sets of signs on the goat track.
            const string h = "long_hunt";
            z.Secrets[new Vector2Int(21, 5)] = new SecretDef
            {
                Id = "ashmaw_claws", Sprite = "obj_rock", Prompt = "Read the marks on the rocks (Survival)", Skill = Rules.Skill.Survival, DC = 14,
                QuestId = h, QuestStage = 1, SetsFlag = "ashmaw_track_a",
                Success = "Claw marks, deep and fresh, going up. Something big landed here and took off again, hard, scattering gravel downhill.",
                Failure = "Scratches on rocks. Could be anything. Look again tomorrow, in better light.",
                AlreadyFound = "The claw marks, going up.",
            };
            z.Secrets[new Vector2Int(24, 12)] = new SecretDef
            {
                Id = "ashmaw_bones", Sprite = "obj_rock", Prompt = "Look at the scorched bones (Nature)", Skill = Rules.Skill.Nature, DC = 14,
                QuestId = h, QuestStage = 1, SetsFlag = "ashmaw_track_b",
                Success = "Goat bones, scorched grey, not black. Whatever ate this one breathes fire that has gone wrong. And it went home to eat it: up the track, into the cliffs.",
                Failure = "Bones. Sad ones. You can't read more from them today.",
                AlreadyFound = "The grey-scorched bones.",
            };
            Zones[z.Id] = z;

            var lair = new ZoneDef
            {
                Act = 2,
                Id = "ashmaw_lair", Name = "The Ashmaw's Lair", Map = Maps.AshmawLair, Dark = true, DefaultSpawn = new Vector2Int(12, 17), Music = "music_ashmaw",
                EnterNarration = "A cave mouth high in the cliffs, wide enough for wings. The floor is scorched grey in long streaks, and the air smells of smoke that went out a long time ago. Something breathes in the dark. Slowly. Like bellows.",
            };
            lair.Warps.Add(new WarpDef(12, 19, "cragfoot", 22, 1));
            lair.Warps.Add(new WarpDef(13, 19, "cragfoot", 23, 1));
            var maw = new EnemyGroupDef
            {
                Id = "ashmaw_den", Style = EncounterStyle.Beasts, TalkDC = 18, ScareDC = 17, Label = "the Ashmaw", SightTiles = 7,
                QuestId = h, MinStage = 2, MaxStage = 2, Unique = true,
                Spotted = "The Ashmaw uncoils from its nest: a wyvern, all leather wings and hooked tail, with ash in its scales and one eye burning like a coal. It screams, and the cave screams back.",
                Befriended = "It isn't a dragon. It's a hungry, frightened animal with a coal in its eye. You talk it down out of its rage, step by step, until it shakes its head like a dog coming out of water, and goes, out of the cave and north, away from every village you know.",
            };
            maw.Members.Add(new EnemySpawn("ashmaw", 12, 8));
            lair.EnemyGroups.Add(maw);
            lair.Secrets[new Vector2Int(12, 5)] = new SecretDef
            {
                Id = "ashmaw_nest", Sprite = "obj_inscription", Prompt = "Look in the nest", Skill = Rules.Skill.Investigation, DC = 5,
                QuestId = h, QuestStage = 3, Dialogue = "wyvern_egg",
                Success = "", Failure = "Bones and branches. Look again.", AlreadyFound = "The empty nest.",
            };
            Zones[lair.Id] = lair;

            var calloway = new NpcDef { Id = "calloway", Name = "Huntmaster Bryn Calloway", Title = "The Wyrmwatch", Look = "npc_calloway", Dialogue = "calloway" };
            calloway.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep in the lodge, boots on"));
            calloway.Schedule.Add(new ScheduleEntry(6, "cragfoot", 41, 9, "drilling the young hunters in the yard"));
            calloway.Schedule.Add(new ScheduleEntry(12, "cragfoot", 31, 12, "mending a crossbow on the lodge steps"));
            calloway.Schedule.Add(new ScheduleEntry(20, "cragfoot", 37, 19, "keeping watch over the empty pen"));
            calloway.Schedule.Add(new ScheduleEntry(23, null, 0, 0, "asleep in the lodge, boots on"));
            calloway.Schedule.Add(new ScheduleEntry(12, "cragfoot", 22, 26, "at the shrine, alone, as she is every Hearthday", Hearthday));
            Npcs[calloway.Id] = calloway;
            var odo = new NpcDef { Id = "odo", Name = "Odo Fenn", Title = "Hunter of the Wyrmwatch", Look = "npc_odo", Dialogue = "odo" };
            odo.Schedule.Add(new ScheduleEntry(0, "cragfoot", 33, 12, "on the lodge steps, sharpening something"));
            odo.Schedule.Add(new ScheduleEntry(0, "cragfoot", 40, 12, "losing at target practice", Stormday));
            Npcs[odo.Id] = odo;
            var hale = new NpcDef { Id = "mother_hale", Name = "Mother Hale", Title = "Cragfoot", Look = "npc_mother_hale", Dialogue = "mother_hale" };
            hale.Schedule.Add(new ScheduleEntry(0, null, 0, 0, "asleep"));
            hale.Schedule.Add(new ScheduleEntry(7, "cragfoot", 8, 10, "on her step, calling for someone"));
            hale.Schedule.Add(new ScheduleEntry(19, null, 0, 0, "indoors, with the lamp in the window"));
            hale.Schedule.Add(new ScheduleEntry(7, "cragfoot", 20, 18, "drawing water and gossiping", Coinday));
            Npcs[hale.Id] = hale;
        }

        // =================================================================== The Fae Courts: the Gloaming Glade

        /// <summary>World flag: Mister Thimble has opened the fairy ring (Act II).</summary>
        public const string RingOpen = "ring_open";

        static void BuildGlade()
        {
            var forest = Zones["forest"];
            forest.Warps.Add(new WarpDef(10, 19, "glade", 20, 27) { MinAct = 2, RequiresFlag = RingOpen });

            var z = new ZoneDef
            {
                Act = 2,
                Id = "glade", Name = "The Gloaming Glade", Map = Maps.Glade, DefaultSpawn = new Vector2Int(20, 27), Music = "music_glade",
                Dusk = true, BlightWeight = 0.3f,
                EnterNarration = "You step through the ring and the light changes. It's dusk here, gold and violet, and it has been dusk here for a very long time. Three courts share the glade: blossom to the north-west, falling leaves to the north-east, frost to the south. At its heart, a stone table, and someone waiting at it.",
            };
            z.Warps.Add(new WarpDef(19, 29, "forest", 11, 19));
            z.Warps.Add(new WarpDef(20, 29, "forest", 11, 19));
            z.Signs[new Vector2Int(8, 5)] = "The Throne of Blossoms: a living chair of white hawthorn, flowering out of season. Petals fall from it constantly and never pile up.";
            z.Signs[new Vector2Int(31, 4)] = "The Throne of Embers: a seat of red-gold leaves that are always just about to fall. The air around it should be warm. It's barely tepid, and the leaves at its edges have gone grey.";
            z.Signs[new Vector2Int(30, 26)] = "The Throne of Frost: carved ice that doesn't melt, with a hoarfrost cushion. Beautiful, and very hard to sit on.";

            z.Secrets[new Vector2Int(13, 18)] = new SecretDef
            {
                Id = "glade_mirror", Sprite = "obj_inscription", Prompt = "Look into the still pond", Skill = Rules.Skill.Insight, DC = 14, Lore = "glade_mirror",
                Success = "Your reflection is a moment late. When you blink, it blinks after. When you look away, it doesn't, not at once.",
                Failure = "Water, sky, your own face, perfectly on time. Today.",
                AlreadyFound = "Your reflection waits politely for you to finish looking.",
            };

            // The Green Bargain: a token for each throne.
            const string q = "green_bargain";
            z.Secrets[new Vector2Int(8, 6)] = new SecretDef
            {
                Id = "throne_blossom_song", Sprite = "obj_inscription", Prompt = "Offer the Blossom token with a performance", Skill = Rules.Skill.Performance, DC = 14,
                QuestId = q, QuestStage = 1, SetsFlag = "throne_blossom",
                Success = "You sing, or recite, or dance, and the hawthorn listens. When you finish, the token in your hand has become a flower, and the throne takes it.",
                Failure = "The hawthorn is unmoved. A petal lands on your nose, which feels like criticism. Tomorrow.",
                AlreadyFound = "The Blossom throne wears your flower.",
            };
            z.Secrets[new Vector2Int(9, 6)] = new SecretDef
            {
                Id = "throne_blossom_nature", Sprite = "obj_bush", Prompt = "Plant the Blossom token properly (Nature)", Skill = Rules.Skill.Nature, DC = 14,
                QuestId = q, QuestStage = 1, SetsFlag = "throne_blossom",
                Success = "You find the right soil, the right depth, the right side of the light. The token sprouts before you've stood up.",
                Failure = "The token sits in the soil and sulks. Wrong spot. Tomorrow.",
                AlreadyFound = "The Blossom throne wears your flower.",
            };
            z.Secrets[new Vector2Int(31, 5)] = new SecretDef
            {
                Id = "throne_ember", Sprite = "obj_inscription", Prompt = "Offer the Ember token (Arcana)", Skill = Rules.Skill.Arcana, DC = 14,
                QuestId = q, QuestStage = 1, SetsFlag = "throne_ember", SucceedsIfLit = UnlitBrazier.KeyFor("glade", new Vector2Int(30, 4)),
                Success = "The token catches like a coal and glows, and the leaves of the throne warm to red-gold around it, all the way to their grey edges.",
                Failure = "The token stays cold. The court needs a fire it can believe in. (Light the brazier by the throne, or try again tomorrow.)",
                AlreadyFound = "The Ember throne glows, a little warmer than before.",
            };
            z.Secrets[new Vector2Int(30, 25)] = new SecretDef
            {
                Id = "throne_frost", Sprite = "obj_inscription", Prompt = "Ask the Frost court to accept the token", Skill = Rules.Skill.Persuasion, DC = 15,
                QuestId = q, QuestStage = 1, SetsFlag = "throne_frost",
                Success = "You speak to the throne the way you would to an old, proud queen. Frost creeps over the token, delicately, and it is accepted.",
                Failure = "The cold deepens until your breath crackles. Not today.",
                AlreadyFound = "The Frost throne holds your token in a fist of ice.",
            };
            var thorns = new EnemyGroupDef
            {
                Id = "frost_thorns", Style = EncounterStyle.Beasts, TalkSkill = Rules.Skill.Nature, TalkDC = 15, ScareDC = 16, Label = "the thorn guardians", SightTiles = 5,
                QuestId = q, MinStage = 1, MaxStage = 1, Unique = true, ResolvedFlag = "throne_frost",
                Spotted = "Two white trees beside the Frost throne straighten up, shake the snow from their branches, and step toward you on roots like frozen ropes.",
                Befriended = "You speak to them as you would to an old orchard in winter, gently, about spring. The guardians creak, consider, and kneel. The Frost throne accepts your token without a word.",
            };
            thorns.Members.Add(new EnemySpawn("thorn_guardian", 28, 24));
            thorns.Members.Add(new EnemySpawn("thorn_guardian", 32, 25));
            z.EnemyGroups.Add(thorns);
            Zones[z.Id] = z;

            var sorrel = new NpcDef { Id = "sorrel", Name = "Herald Sorrel", Title = "Herald of the Three Courts", Look = "npc_sorrel", Dialogue = "sorrel" };
            sorrel.Schedule.Add(new ScheduleEntry(0, "glade", 20, 17, "at the stone table, waiting, as she always is"));
            sorrel.Schedule.Add(new ScheduleEntry(0, "glade", 32, 9, "at the last warm spring, trailing her fingers in it", Hearthday));
            Npcs[sorrel.Id] = sorrel;
            var blossom = new NpcDef { Id = "blossom_speaker", Name = "Speaker of Blossoms", Title = "The Court of Blossoms", Look = "npc_blossom_speaker", Dialogue = "blossom_speaker" };
            blossom.Schedule.Add(new ScheduleEntry(0, "glade", 10, 10, "arranging petals that rearrange themselves"));
            blossom.Schedule.Add(new ScheduleEntry(0, "glade", 7, 7, "sulking by the throne", Stormday));
            Npcs[blossom.Id] = blossom;
            var frost = new NpcDef { Id = "frost_speaker", Name = "Speaker of Frost", Title = "The Court of Frost", Look = "npc_frost_speaker", Dialogue = "frost_speaker" };
            frost.Schedule.Add(new ScheduleEntry(0, "glade", 25, 27, "watching the ice grow"));
            frost.Schedule.Add(new ScheduleEntry(0, "glade", 26, 23, "pacing, very slowly", Leafday));
            Npcs[frost.Id] = frost;

        }

        // =================================================================== The Academy: Cold Lanterns and the Ninth Circle

        static void BuildAcademyForce()
        {
            var road = Zones["lantern_road"];
            road.Secrets[new Vector2Int(51, 12)] = new SecretDef
            {
                Id = "lantern_eyes", Sprite = "obj_inscription", Prompt = "Look closer at the lantern's glass (Arcana)", Skill = Rules.Skill.Arcana, DC = 14,
                QuestId = "cold_lanterns", QuestStage = 2, SetsFlag = "lantern_eyes", Lore = "lantern_eyes",
                Success = "The flame isn't the only thing in there. Behind the wick, scratched into the iron, is a tiny glyph: an eye. It was shut while the lamp was cold. It's open now, and it's looking up the road.",
                Failure = "Glass, flame, a moth with poor judgement. Whatever caught your eye, it's gone. Tomorrow.",
                AlreadyFound = "The little iron eye behind the flame, wide awake.",
            };

            var academy = Zones["academy"];
            academy.Secrets[new Vector2Int(12, 33)] = new SecretDef
            {
                Id = "starwell_stone", Sprite = "obj_inscription", Prompt = "Draw a focus stone from the Starwell (Arcana)", Skill = Rules.Skill.Arcana, DC = 15,
                QuestId = "ninth_circle", QuestStage = 1, SetsFlag = "stone_starwell", Items = { new KeyValuePair<string, int>("focus_stone", 1) },
                Success = "You reach into the light, past the water, and your fingers close on something smooth and cold. It comes up dripping starlight: a grey focus stone, humming like a struck bell.",
                Failure = "The light slides away from your hand like a fish. Steady yourself and try again tomorrow.",
                AlreadyFound = "The Starwell's light has a small, stone-shaped gap in it.",
            };

            var hall = Zones["sealing_hall"];
            hall.Secrets[new Vector2Int(12, 10)] = new SecretDef
            {
                Id = "set_the_stones", Sprite = "obj_inscription", Prompt = "Set the focus stones in the circle", Skill = Rules.Skill.Arcana, DC = 10,
                QuestId = "ninth_circle", QuestStage = 2, AdvanceTo = 3,
                Success = "You set the three stones at the circle's points. The runes wake one ring at a time, inward, until the whole floor is light. And something that was standing in the middle of the dais all along, held so still you never saw it, opens its eyes.",
                Failure = "The stones won't sit right; the runes flicker and fade. Ashby's diagrams are very precise. Try again tomorrow.",
                AlreadyFound = "Three stones in the circle, humming together.",
            };
            var bear = new EnemyGroupDef
            {
                Id = "circle_bear", Style = EncounterStyle.Beasts, TalkDC = 16, ScareDC = 15, Label = "the cinder bear", SightTiles = 8,
                QuestId = "ninth_circle", MinStage = 3, MaxStage = 3, Unique = true,
                Spotted = "The circle flares. Something the Academy caught on the Emberhorn's slopes is in it: a bear with a coal-glow for eyes. The binding holds for a breath, and then it doesn't.",
                Befriended = "You stand very still and talk to it the way you'd talk to a frightened horse. The coal in its eyes dims. It sits down heavily on the runes, and lets the scholars lead it away to somewhere with trees.",
            };
            bear.Members.Add(new EnemySpawn("cinder_bear", 12, 8));
            hall.EnemyGroups.Add(bear);
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
