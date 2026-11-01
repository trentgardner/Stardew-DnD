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
    }
}
