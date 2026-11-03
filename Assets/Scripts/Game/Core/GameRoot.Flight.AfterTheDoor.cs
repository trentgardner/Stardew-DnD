using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 6: "Who rides {dragon}?" and the one flight of the game. EXEMPT from the secret guard: it only runs
    /// after the streets are won, long after WorldDb.MetHer and the choice at the turn.
    /// The user will hand this to their fiancée: the prompt makes the hand-off one press, and nothing starts until the rider confirms.
    /// </summary>
    public partial class GameRoot
    {
        /// <summary>What the passes won; the boss fight reads and clears these.</summary>
        public static readonly List<FlightEffect> FlightResults = new List<FlightEffect>();
        /// <summary>What the last flight won (kept after the boss fight uses FlightResults up).</summary>
        public static readonly List<FlightEffect> LastFlightWon = new List<FlightEffect>();

        /// <summary>Every player's camera follows this while it's set (the dragon, in the air).</summary>
        Transform _flightCam;
        int _riderCandidate = -1;
        bool _flying;

        public bool Flying => _flying;

        void ResetFlight() { _flying = false; _riderCandidate = -1; _flightCam = null; FlightResults.Clear(); LastFlightWon.Clear(); }

        string DragonCalled => World.EggHatched ? World.DragonName : "the dragon";

        /// <summary>The streets are won: the dragon comes down for one of you.</summary>
        public void BeginFlight(PlayerSlot by, string ending)
        {
            if (World.Flag(Finale.Flown) || _flying) return;
            StartCoroutine(BeginFlightSoon(by, ending));
        }

        IEnumerator BeginFlightSoon(PlayerSlot by, string ending)
        {
            yield return null;
            if (World.Flag(Finale.DragonWaits))
            {
                World.ClearFlag(Finale.DragonWaits);
                if (DragonAgent != null) DragonAgent.Waiting = false;
            }
            var route = FinaleDb.Route(ending, World);
            yield return PlayBeats(by, route.Takeoff);
            OfferRider();
        }

        /// <summary>Opens the prompt for every ready player (solo: no prompt, the one player rides).</summary>
        public void OfferRider()
        {
            if (World.Flag(Finale.Flown) || _flying) return;
            World.SetFlag(Finale.RiderPending);
            var ready = Players.Where(p => p.IsReady).ToList();
            if (ready.Count == 0) return;
            if (ready.Count == 1) { ConfirmRider(ready[0]); return; }
            _riderCandidate = -1;
            string title = $"Who rides {Cap(DragonCalled)}?";
            foreach (var p in ready)
            {
                p.Hud.CloseAll();
                var options = ready.Select(x => { var who = x; return (who.Character.Name, (System.Action)(() => PickRider(who))); }).ToList();
                p.Hud.Push(new ChoiceScreen(title, $"Only one of you can go up. {Cap(DragonCalled)} is waiting.", options));
            }
        }

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        /// <summary>Someone picked a rider: they confirm, or hand it over in one press.</summary>
        void PickRider(PlayerSlot who)
        {
            if (!World.Flag(Finale.RiderPending) || _flying || !who.IsReady) return;
            _riderCandidate = who.Index;
            StartCoroutine(PickRiderSoon(who));
        }

        IEnumerator PickRiderSoon(PlayerSlot who)
        {
            yield return null; // let the screen that chose close first
            if (_riderCandidate != who.Index) yield break;
            var others = Players.Where(p => p.IsReady && p != who).ToList();
            foreach (var p in Players.Where(p => p.IsReady)) p.Hud.CloseAll();
            var opts = new List<(string, System.Action)> { ("Take the reins", () => ConfirmRider(who)) };
            foreach (var o in others) { var other = o; opts.Add(($"Let {other.Character.Name} ride", () => PickRider(other))); }
            who.Hud.Push(new ChoiceScreen($"Ride {Cap(DragonCalled)}?", $"{Cap(DragonCalled)} lowers its neck for you.", opts));
            foreach (var o in others) o.Hud.Push(new MessageScreen("", $"{who.Character.Name} is climbing up onto {DragonCalled}'s back..."));
        }

        /// <summary>The rider is confirmed: nothing starts before this.</summary>
        public void ConfirmRider(PlayerSlot rider)
        {
            if (rider == null || !rider.IsReady || !World.Flag(Finale.RiderPending) || _flying) return;
            World.SetFlag(Finale.RiderFlag(rider.Index));
            World.ClearFlag(Finale.RiderPending);
            _riderCandidate = -1;
            foreach (var p in Players.Where(p => p.IsReady && p != rider)) p.Hud.CloseAll();
            StartCoroutine(Fly(rider, Turn.Ending(World)));
        }

        /// <summary>Called every frame: a candidate who dropped out hands the prompt back to whoever's left.</summary>
        void FlightTick()
        {
            if (_riderCandidate < 0 || World == null || !World.Flag(Finale.RiderPending)) return;
            var cand = Players.FirstOrDefault(p => p.Index == _riderCandidate);
            if (cand == null || !cand.IsReady) { _riderCandidate = -1; OfferRider(); }
        }

        /// <summary>The flight: takeoff, two passes with the rider's rolls, the landing, then the last fight.</summary>
        public IEnumerator Fly(PlayerSlot rider, string ending)
        {
            if (_flying) yield break;
            _flying = true;
            FlightResults.Clear();
            var route = FinaleDb.Route(ending, World);
            var capital = Zones["capital"];
            var agent = DragonAgent;
            var pet = agent != null ? agent.GetComponent<DragonPet>() : null;
            Transform dragonT = agent != null ? agent.Actor.transform : rider.Avatar.transform;
            var ys = rider.Avatar.GetComponent<YSort>();
            var ctrl = rider.Avatar as PlayerController;
            if (agent != null) { agent.Waiting = true; if (pet != null) pet.StaysPut = true; agent.Actor.PlaceAt(capital, route.Waypoints[0]); }
            if (rider.Zone != capital) Warp(rider, "capital", route.Waypoints[0], false);
            if (ctrl != null) ctrl.enabled = false;
            if (ys != null) ys.Offset += 40;
            _flightCam = dragonT;
            foreach (var p in Players.Where(p => p.IsReady && p != rider)) p.Hud.Narrate($"{rider.Character.Name} is flying {DragonCalled} over Aurelhold!");

            for (int i = 1; i < route.Waypoints.Count; i++)
            {
                Vector3 from = dragonT.position, to = capital.ToWorld(route.Waypoints[i]);
                float t = 0, dur = Mathf.Max(0.3f, Vector3.Distance(from, to) / 6f);
                while (t < dur)
                {
                    t += Time.deltaTime;
                    dragonT.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur));
                    if (agent != null) rider.Avatar.transform.position = dragonT.position + new Vector3(0, 0.35f, 0);
                    yield return null;
                }
                if (i == 1 || i == 3) yield return Pass(rider, route.Passes[i == 1 ? 0 : 1]);
            }

            yield return PlayBeats(rider, route.Landing);
            _flightCam = null;
            if (ys != null) ys.Offset -= 40;
            if (ctrl != null) ctrl.enabled = true;
            GatherAt(route.LandingZone, route.LandingSpot);
            if (agent != null)
            {
                var zone = Zones[route.LandingZone];
                agent.Actor.PlaceAt(zone, route.LandingSpot + new Vector2Int(-1, 1));
                agent.Waiting = false;
                if (pet != null) pet.StaysPut = false;
            }
            foreach (var p in Players.Where(p => p.IsReady)) foreach (var c in CompanionsOf(p)) if (!c.IsDragon && c.Character != null) c.Actor.PlaceAt(Zones[route.LandingZone], route.LandingSpot + new Vector2Int(1, 1));
            World.SetFlag(Finale.Flown);
            if (Quests.Stage(WarmCity.Id) < WarmCity.Boss) Quests.Set(WarmCity.Id, WarmCity.Boss, rider);
            _flying = false;
            SaveSystem.Save(true);
            StartFinaleBoss(ending);
        }

        /// <summary>One pass: the rider picks, rolls (visible dice), and wins an edge for the fight, or takes a scorching.</summary>
        IEnumerator Pass(PlayerSlot rider, FlightPass pass)
        {
            FlightOption picked = null;
            var opts = pass.Options.Select(o => { var opt = o; return (opt.Label, (System.Action)(() => picked = opt)); }).ToList();
            var screen = new ChoiceScreen(pass.Title, pass.Text, opts);
            screen.OnCancel = () => picked = picked ?? pass.Options[0]; // Esc: the first way through
            rider.Hud.Push(screen);
            foreach (var p in Players.Where(p => p.IsReady && p != rider)) p.Hud.Narrate(pass.Text);
            while (picked == null && rider.IsReady) yield return null;
            if (picked == null) picked = pass.Options[0];

            var pc = rider.Character;
            var terms = new List<RollTerm> { new RollTerm(picked.Skill.ToString(), pc.SkillBonus(picked.Skill)) };
            D20Result res = null;
            rider.Hud.Dice.RollD20(pass.Title, $"{picked.Skill} · DC {picked.Dc}", () => Dice.RollD20(AdvantageState.Normal, terms), picked.Dc, true, r => res = r);
            while (res == null && rider.IsReady) yield return null;
            while (rider.Hud.Dice.IsOpen) yield return null;
            bool ok = res != null && res.Total >= picked.Dc;
            if (ok)
            {
                FlightResults.Add(picked.OnSuccess);
                foreach (var p in Players.Where(p => p.IsReady)) p.Hud.Narrate(picked.Success, true);
            }
            else
            {
                var burn = Dice.RollDamage(DiceExpr.Parse("3d6"));
                int lost = Mathf.Min(burn.Total, Mathf.Max(0, pc.HP - 1));
                pc.HP -= lost;
                rider.Avatar.Popup("-" + lost, UIKit.DamageRed);
                foreach (var p in Players.Where(p => p.IsReady)) p.Hud.Narrate($"{picked.Failure} ({burn.Breakdown()} fire)", true);
            }
            yield return new WaitForSeconds(0.6f);
        }

    }
}
