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

        // ------------------------------------------------------------------ the fight through the streets

        /// <summary>Fills the street groups from the ending and Act II state. Before any capital spawn: at the choice and on load.</summary>
        public void PrepareFinale()
        {
            var ending = Turn.Ending(World);
            if (ending == null) return;
            string prefix = Finale.Prefix + "street_";
            foreach (var g in WorldDb.Zones["capital"].EnemyGroups.Where(g => g.Id.StartsWith(prefix)))
            {
                if (!int.TryParse(g.Id.Substring(prefix.Length), out int n)) continue;
                g.Members.Clear();
                g.Members.AddRange(FinaleDb.StreetMembers(n, ending, World));
            }
        }

        /// <summary>After the break: the city fights itself, and stage 1 opens in the Great Square.</summary>
        public void BeginFinale(PlayerSlot by, string ending)
        {
            if (World.Flag(Finale.Started)) return;
            World.SetFlag(Finale.Started);
            PrepareFinale();
            if (Quests.Stage(WarmCity.Id) < WarmCity.Streets) Quests.Set(WarmCity.Id, WarmCity.Streets, by);
            if (ending == Turn.End) World.SetFlag(Finale.DragonWaits);
            ApplyDragonWaits();
            SaveSystem.Save(true);
            OpenStreet(by, 1);
        }

        /// <summary>End her: the dragon lies across the top of the stair down, and won't fight for this.</summary>
        void ApplyDragonWaits()
        {
            var agent = DragonAgent;
            if (agent == null || !World.Flag(Finale.DragonWaits) || !Zones.TryGetValue("capital", out var capital)) return;
            agent.Waiting = true;
            agent.Actor.PlaceAt(capital, FinaleDb.DragonWaitSpot);
            var pet = agent.GetComponent<DragonPet>();
            if (pet != null) pet.StaysPut = true;
        }

        void SpawnStreet(int n)
        {
            var capital = Zones["capital"];
            foreach (var g in capital.Def.EnemyGroups.Where(g => g.Id == Finale.StreetGroup(n)).ToList()) SpawnGroupIfNeeded(capital, g);
        }

        void OpenStreet(PlayerSlot by, int n)
        {
            World.SetFlag(Finale.StreetOpen(n));
            SpawnStreet(n);
            StartCoroutine(PlayBeats(by, FinaleDb.StreetIntro(n, Turn.Ending(World), World), title: FinaleDb.StreetTitle(n)));
        }

        /// <summary>A finale group was beaten: the next stage opens, or the streets are won.</summary>
        void OnFinaleGroupResolved(string groupId)
        {
            if (!Finale.IsFinaleGroup(groupId) || Turn.Ending(World) == null) return;
            var by = Players.FirstOrDefault(p => p.IsReady);
            for (int n = 1; n <= Finale.Stages; n++)
            {
                if (groupId != Finale.StreetGroup(n)) continue;
                World.SetFlag(Finale.StreetDone(n));
                if (n < Finale.Stages) { SaveSystem.Save(true); if (by != null) OpenStreet(by, n + 1); }
                else
                {
                    if (Quests.Stage(WarmCity.Id) < WarmCity.Flight) Quests.Set(WarmCity.Id, WarmCity.Flight, by);
                    SaveSystem.Save(true);
                    if (by != null) BeginFlight(by, Turn.Ending(World));
                }
            }
        }

        /// <summary>The flight (Task 5 builds it). For now: the dragon comes.</summary>
        public void BeginFlight(PlayerSlot by, string ending)
        {
            by.Hud.Narrate("The dragon comes.");
        }

        /// <summary>On load: replay the break if its beats weren't seen, or put the finale back as it was.</summary>
        public void CheckTheFinale()
        {
            if (!InGame || World == null || !World.Flag(Turn.Chosen)) return;
            var ending = Turn.Ending(World);
            var by = Players.FirstOrDefault(p => p.IsReady);
            if (ending == null || by == null) return;
            PrepareFinale();
            if (!World.Flag(Finale.Started)) { StartCoroutine(TheBreak(by, ending)); return; }
            ApplyDragonWaits();
            int n = Finale.CurrentStreet(World);
            if (n >= 1 && n <= Finale.Stages) { World.SetFlag(Finale.StreetOpen(n)); SpawnStreet(n); }
        }

        /// <summary>
        /// A wipe in a finale fight: the fallen come to behind a barricade, a little way back. No day lost, no fee, no temple.
        /// Returns false when it isn't a finale fight (the usual wipe applies).
        /// </summary>
        bool FinaleWipe(CombatInstance c, List<PlayerSlot> fallen)
        {
            if (!Finale.IsFinaleGroup(c.GroupId) || !World.Flag(Turn.Chosen)) return false;
            var spot = FinaleDb.RetrySpot(Finale.CurrentStreet(World));
            int i = 0;
            foreach (var p in fallen)
            {
                var pc = p.Character;
                if (pc.IsDead) pc.Revive(0, null);
                pc.HP = System.Math.Max(1, pc.EffectiveMaxHP / 2);
                pc.DeathSuccesses = pc.DeathFailures = 0; pc.Stable = false;
                foreach (var comp in CompanionsOf(p)) if (comp.Character != null) { if (comp.Character.IsDead) comp.Character.Revive(0, null); comp.Character.HP = System.Math.Max(1, comp.Character.EffectiveMaxHP / 2); }
                Warp(p, "capital", spot + new Vector2Int(i++, 0), false);
                p.Hud.Push(new MessageScreen("Behind the barricade", "You come to behind an overturned market cart, with somebody's cloak under your head. The fighting hasn't stopped. It's waiting for you."));
            }
            SpawnStreet(Finale.CurrentStreet(World));
            return true;
        }
    }
}
