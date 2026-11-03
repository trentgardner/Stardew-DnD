using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>A soldier of a force that stands with you, in a finale fight only. AI-driven, on the party's side; gone when the fight ends.</summary>
    public class FinaleAlly : Actor
    {
        public Monster Monster;
    }

    /// <summary>
    /// Act III Phase 6: the finales. EXEMPT from the secret guard: none of it runs before WorldDb.MetHer and Turn.Chosen.
    /// </summary>
    public partial class GameRoot
    {
        /// <summary>Whoever stands with you sends someone to every finale fight.</summary>
        void AddFinaleAllies(CombatInstance c)
        {
            if (!Finale.IsFinaleGroup(c.GroupId) || World == null) return;
            var lead = c.Units.FirstOrDefault(u => u.Owner != null);
            if (lead == null) return;
            foreach (var (id, name) in FinaleDb.Allies(Turn.Ending(World), World))
            {
                var def = MonstersDb.Get(id);
                if (def == null) continue;
                var spot = FreeTileNear(c, lead.Pos, 3);
                var a = Actor.Spawn<FinaleAlly>(name, def.SpriteId, false, c.Zone, spot);
                a.Monster = new Monster(def) { Name = name, Team = Team.Players };
                a.Creature = a.Monster;
                if (def.Scale > 0f && def.Scale != 1f && a.Sr != null) a.Sr.transform.localScale *= def.Scale;
                var u = c.AddUnit(a.Monster, a);
                u.Ally = true;
                c.Log($"{name} fights beside you.");
            }
        }

        static Vector2Int FreeTileNear(CombatInstance c, Vector2Int from, int radius)
        {
            for (int r = 1; r <= radius; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var q = from + new Vector2Int(dx, dy);
                        if (c.Zone.IsWalkable(q) && !c.Units.Any(o => o.Pos == q && !o.C.IsDead)) return q;
                    }
            return from;
        }

        /// <summary>The fight is over (won, lost or fled): the allies go back to their own.</summary>
        void RemoveFinaleAllies(CombatInstance c)
        {
            foreach (var u in c.Units.Where(u => u.Ally).ToList())
                if (u.A) Destroy(u.A.gameObject);
        }
    }
}
