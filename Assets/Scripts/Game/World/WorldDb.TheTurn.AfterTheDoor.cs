using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 5: the questioning on the Sunstair. EXEMPT from the secret guard (LoreTests.GuardScans). The five speakers
    /// are separate NpcDefs that share dialogue "questioning" (DialogueDb caches dialogue per id, so the originals can't switch
    /// dialogue by flag). Every entry requires Turn.Questioning, which is only set after WorldDb.MetHer and the meeting.
    /// The originals step out (GoneFlag) while the questioning runs, and stay out: after it, the square is a battlefield.
    /// </summary>
    public static partial class WorldDb
    {
        /// <summary>Where the party stands at the questioning: the foot of the Sunstair, facing the palace.</summary>
        public static readonly Vector2Int SunstairSpot = new Vector2Int(31, 14);

        static void BuildTheTurn()
        {
            Speaker("king_q", "king_aldric", 31, 11, "on the Sunstair, waiting for an answer");
            Speaker("marsh_q", "marsh_capital", 29, 12, "at the King's shoulder, watching you");
            Speaker("ashby_q", "ashby_capital", 26, 13, "at the foot of the Sunstair, watching you");
            Speaker("sorrel_q", "sorrel_capital", 34, 12, "at the foot of the Sunstair, watching you");
            Speaker("calloway_q", "calloway_capital", 37, 13, "at the foot of the Sunstair, watching you");

            void Speaker(string id, string original, int x, int y, string doing)
            {
                var o = Npcs[original];
                var n = new NpcDef { Id = id, Name = o.Name, Title = o.Title, Look = o.Look, Dialogue = "questioning" };
                n.Schedule.Add(new ScheduleEntry(0, "capital", x, y, doing) { RequiresFlag = Turn.Questioning, ForbidFlag = Turn.Chosen });
                Npcs[id] = n;
                o.GoneFlag = Turn.Questioning; // (none of the five had a GoneFlag in Act III; MetHer comes first)
            }
        }
    }
}
