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
            var bossDef = BossDef(ending);
            if (bossDef != null) { bossDef.Members.Clear(); bossDef.Members.AddRange(FinaleDb.BossMembers(ending, false)); }
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
            if (groupId == Finale.BossGroup(Turn.Ending(World))) { FinaleWon(by, Turn.Ending(World)); return; }
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
            else if (World.Flag(Finale.Flown) && !World.Flag(Finale.Won)) { OpenBoss(ending, false); }
            else if (n > Finale.Stages && !World.Flag(Finale.Flown))
            {
                // Saved between the streets and the landing: the dragon comes back down for a rider.
                if (World.Flag(Finale.RiderPending)) StartCoroutine(OfferRiderSoon());
                else BeginFlight(by, ending);
            }
        }

        /// <summary>
        /// A wipe in a finale fight: the fallen come to behind a barricade, a little way back. No day lost, no fee, no temple.
        /// Returns false when it isn't a finale fight (the usual wipe applies).
        /// </summary>
        bool FinaleWipe(CombatInstance c, List<PlayerSlot> fallen)
        {
            if (!Finale.IsFinaleGroup(c.GroupId) || !World.Flag(Turn.Chosen)) return false;
            bool boss = World.Flag(Finale.Flown);
            var ending = Turn.Ending(World);
            var route = boss ? FinaleDb.Route(ending, World) : null;
            string zone = boss ? route.LandingZone : "capital";
            var spot = boss ? route.LandingSpot : FinaleDb.RetrySpot(Finale.CurrentStreet(World));
            int i = 0;
            foreach (var p in fallen)
            {
                var pc = p.Character;
                if (pc.IsDead) pc.Revive(0, null);
                pc.HP = System.Math.Max(1, pc.EffectiveMaxHP / 2);
                pc.DeathSuccesses = pc.DeathFailures = 0; pc.Stable = false;
                foreach (var comp in CompanionsOf(p)) if (comp.Character != null) { if (comp.Character.IsDead) comp.Character.Revive(0, null); comp.Character.HP = System.Math.Max(1, comp.Character.EffectiveMaxHP / 2); }
                Warp(p, zone, spot + new Vector2Int(i++, 0), false);
                p.Hud.Push(new MessageScreen("Behind the barricade", "You come to behind an overturned market cart, with somebody's cloak under your head. The fighting hasn't stopped. It's waiting for you."));
            }
            if (boss) OpenBoss(ending, false); else SpawnStreet(Finale.CurrentStreet(World));
            return true;
        }

        IEnumerator OfferRiderSoon() { yield return null; yield return null; OfferRider(); }

        // ------------------------------------------------------------------ the last fight

        /// <summary>Seal her: rounds Ashby has had at the door (0..SealRounds). Resets with a retry.</summary>
        public int SealProgress { get; private set; }
        bool _sealPulled;
        Actor _ashbyAtTheDoor;
        static bool _phaseHooked;

        EnemyGroupDef BossDef(string ending)
        {
            if (ending == null) return null;
            string zone = ending == Turn.Seal ? "the_deep" : "capital";
            return WorldDb.Zones[zone].EnemyGroups.FirstOrDefault(g => g.Id == Finale.BossGroup(ending));
        }

        /// <summary>Puts the last fight in place (fresh), with or without the Ring-knight the flight scattered.</summary>
        void OpenBoss(string ending, bool scattered)
        {
            var def = BossDef(ending);
            if (def == null) return;
            World.SetFlag(Finale.BossOpen(ending));
            def.Members.Clear();
            def.Members.AddRange(FinaleDb.BossMembers(ending, scattered));
            if (_groups.TryGetValue(def.Id, out var old)) { foreach (var a in old) if (a) Destroy(a.gameObject); _groups.Remove(def.Id); }
            var zone = Zones[ending == Turn.Seal ? "the_deep" : "capital"];
            SpawnGroupIfNeeded(zone, def);
            if (ending == Turn.Seal)
            {
                SealProgress = 0;
                _sealPulled = false;
                if (_ashbyAtTheDoor == null) _ashbyAtTheDoor = Actor.Spawn<Actor>("Ashby", "npc_ashby", true, zone, FinaleDb.AshbyAtTheDoor);
            }
            if (!_phaseHooked) { CombatInstance.PhaseEntered += (m, ph) => I?.OnBossPhase(m); _phaseHooked = true; }
        }

        /// <summary>The landing: the last fight starts at once, with whatever the flight won.</summary>
        public void StartFinaleBoss(string ending)
        {
            var lead = Players.FirstOrDefault(p => p.IsReady && World.Flag(Finale.RiderFlag(p.Index))) ?? Players.FirstOrDefault(p => p.IsReady);
            if (lead == null || ending == null) return;
            bool scattered = ending == Turn.Seal && FlightResults.Contains(FlightEffect.AddsScattered);
            OpenBoss(ending, scattered);
            foreach (var p in Players.Where(p => p.IsReady)) p.Hud.CloseAll();
            StartGroupCombat(Finale.BossGroup(ending), lead);
            var c = Combats.FirstOrDefault(x => x.GroupId == Finale.BossGroup(ending) && !x.Finished);
            if (c != null) ApplyFlightResults(c, ending);
            LastFlightWon.Clear(); LastFlightWon.AddRange(FlightResults);
            FlightResults.Clear();
        }

        void ApplyFlightResults(CombatInstance c, string ending)
        {
            var boss = c.Units.FirstOrDefault(u => u.C is Monster m && m.Def.Phases.Count > 0);
            var m = boss?.C as Monster;
            foreach (var fx in FlightResults.Distinct())
            {
                switch (fx)
                {
                    case FlightEffect.BossWounded when m != null:
                        int floor = (int)System.Math.Ceiling(m.Def.Phases[0].At * m.MaxHP);
                        m.HP = System.Math.Max(floor + 1, m.HP - m.MaxHP / 10);
                        c.Log($"{m.Name} is hurt before the fight begins (the flight).");
                        break;
                    case FlightEffect.NoLegendaryFirstRound when m != null:
                        m.LegendaryLeft = 0;
                        c.Log($"{m.Name} is off balance: no legendary actions until its turn.");
                        break;
                    case FlightEffect.AddsScattered when m != null && ending != Turn.Seal:
                        if (m.Def.Abilities.Count > 0) m.Spend(m.Def.Abilities[0]);
                        c.Log($"{m.Name}'s {(m.Def.Abilities.Count > 0 ? m.Def.Abilities[0].Name : "fire")} is spent: it has to recharge.");
                        break;
                    case FlightEffect.PartyInspired:
                        foreach (var h in c.Humans) if (h.Character != null) h.Character.HeroicInspiration = true;
                        c.Log("Everyone takes heart: Heroic Inspiration.");
                        break;
                }
            }
        }

        /// <summary>Each new round of a fight. Seal her: Ashby gets one round further, and at seven the door is shut.</summary>
        public void OnCombatRound(CombatInstance c, int round)
        {
            if (c == null || round <= 1 || c.Finished || c.GroupId != Finale.BossGroup(Turn.Seal) || Turn.Ending(World) != Turn.Seal) return;
            SealProgress = System.Math.Min(Finale.SealRounds, SealProgress + 1);
            string line = SealProgress >= Finale.SealRounds ? "The seal: 7/7. Ashby draws the last line." : $"The seal: {SealProgress}/{Finale.SealRounds}. Ashby is still drawing.";
            c.Log(line);
            foreach (var h in c.Humans) h.Hud.Narrate(line);
            if (SealProgress < Finale.SealRounds) return;
            // The door is shut: the King's regalia goes dark, and everyone left standing on his side yields.
            foreach (var u in c.Units.Where(u => u.C.Team == Team.Enemies && !u.C.IsDown).ToList()) u.C.HP = 0;
            c.CheckEndNow();
        }

        /// <summary>The King reaches his phase: he hauls on the chains, and the seal slips back once.</summary>
        void OnBossPhase(Monster m)
        {
            if (m == null || m.Def.Id != "king_regalia" || _sealPulled || SealProgress <= 0) return;
            _sealPulled = true;
            SealProgress--;
            foreach (var p in Players.Where(p => p.IsReady)) p.Hud.Narrate($"The seal slips back: {SealProgress}/{Finale.SealRounds}.");
        }

        /// <summary>The last fight is won: saved, the victory beats, and Phase 7's placeholder.</summary>
        void FinaleWon(PlayerSlot by, string ending)
        {
            if (World.Flag(Finale.Won)) return;
            World.SetFlag(Finale.Won);
            if (Quests.Stage(WarmCity.Id) < WarmCity.Won) Quests.Set(WarmCity.Id, WarmCity.Won, by);
            SaveSystem.Save(true);
            StartCoroutine(VictorySoon(by, ending));
        }

        IEnumerator VictorySoon(PlayerSlot by, string ending)
        {
            yield return new WaitForSecondsRealtime(1.5f); // let the fight wind down
            var lead = by != null && by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady);
            if (lead == null) yield break;
            yield return PlayBeats(lead, FinaleDb.Victory(ending, World));
            if (_ashbyAtTheDoor) { Destroy(_ashbyAtTheDoor.gameObject); _ashbyAtTheDoor = null; }
            // Placeholder until Phase 7 builds the endings.
            foreach (var p in Players.Where(x => x.IsReady))
                p.Hud.Push(new MessageScreen("To Be Continued", "The fight for Aurelhold is won. What comes after is in the next update. Everything you chose is saved."));
        }
    }
}
