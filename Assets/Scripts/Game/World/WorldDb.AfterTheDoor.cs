using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: beyond the grey door. EXEMPT from the secret guard (LoreTests.GuardScans): this is where the truth lives.
    /// Nothing here can be seen before the party opens the door: the room is reached only by GameRoot.ThroughTheDoor, which
    /// sets WorldDb.MetHer first, and every schedule entry below requires MetHer.
    /// </summary>
    public static partial class WorldDb
    {
        public static readonly Vector2Int HerSpot = new Vector2Int(4, 4);
        public static readonly Vector2Int ThresholdSpot = new Vector2Int(5, 7);

        static void BuildBeyondTheDoor()
        {
            var z = new ZoneDef
            {
                Act = 3,
                Id = "beyond_the_door", Name = "Beyond the Door", Map = Maps.BeyondTheDoor, Indoors = true, Silent = true, DefaultSpawn = ThresholdSpot,
                BlightWeight = 0f, DragonStays = false,
            };
            z.Warps.Add(new WarpDef(6, 9, "the_deep", 24, 2));
            z.Warps.Add(new WarpDef(7, 9, "the_deep", 24, 2));
            z.Lore.Add(new LoreSpotDef { Id = "her_drawings", Pos = new Vector2Int(9, 2), Prompt = "Look at the drawings", Sprite = "obj_inscription" });
            z.Lore.Add(new LoreSpotDef { Id = "her_presents", Pos = new Vector2Int(11, 5), Prompt = "Look at the presents", Sprite = "obj_crate" });
            Zones[z.Id] = z;
        }

        static void BuildBeyondTheDoorNpcs()
        {
            var edda = new NpcDef { Id = "edda", Name = "Edda", Title = "", Look = "npc_edda", Dialogue = "edda", Glow = new Color(1f, 0.6f, 0.25f, 0.55f) };
            edda.Schedule.Add(new ScheduleEntry(0, "beyond_the_door", HerSpot.x, HerSpot.y, "sitting on the bed, swinging her feet") { RequiresFlag = MetHer });
            edda.Schedule.Add(new ScheduleEntry(0, "beyond_the_door", 9, 3, "drawing another dragon on the wall", Hearthday) { RequiresFlag = MetHer });
            Npcs[edda.Id] = edda;
            DragonDb.Base["edda"] = DragonFeeling.Trust; // it won't leave her side
        }
    }
}
