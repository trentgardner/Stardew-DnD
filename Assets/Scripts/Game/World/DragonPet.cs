using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// The dragon as a pet (it lives on the dragon's CompanionAgent): the menu you get by walking up to it, petting,
    /// fetch, treats, little emote bubbles, naps, and the party's bond with it (0–100, no fail state).
    /// </summary>
    public class DragonPet : MonoBehaviour
    {
        CompanionAgent _agent;
        SpriteRenderer _emote;
        float _emoteUntil, _still, _lastZ;
        /// <summary>Busy with a game of fetch: the follow logic leaves it alone.</summary>
        public bool Busy { get; private set; }
        public bool Napping { get; private set; }

        static GameRoot G => GameRoot.I;
        static string Name => G.World.DragonName;

        void Awake() => _agent = GetComponent<CompanionAgent>();

        // ================================================================== bond

        /// <summary>0–5 hearts: one per 20 points of bond.</summary>
        public static int BondTier => Mathf.Clamp(G.World.DragonBond / 20, 0, 5);
        public static string Hearts => new string('♥', BondTier) + new string('♡', 5 - BondTier);

        public static void AddBond(PlayerSlot p, int amount, string reason)
        {
            int before = BondTier;
            G.World.DragonBond = Mathf.Clamp(G.World.DragonBond + amount, 0, 100);
            if (BondTier > before)
                foreach (var x in G.Players.Where(x => x.IsReady)) x.Hud.Toast($"{Name} adores you all a little more. Bond {Hearts}", UIKit.Gold);
        }

        public static string JournalLine()
        {
            var d = G.Dragon;
            string hp = d != null ? $" · HP {d.HP}/{d.MaxHP}" : "";
            return $"{Name} the dragonling{hp}{(G.World.DragonScared ? " (hiding, scared: rest to calm them)" : "")} · Bond {Hearts}";
        }

        // ================================================================== the menu

        public void OpenMenu(PlayerSlot p)
        {
            int treats = p.Character.CountOf("dragon_treat");
            var opts = new List<(string, Action)>
            {
                ($"Pet {Name}", () => PetIt(p)),
                ("Play fetch", () => StartCoroutine(Fetch(p))),
                (treats > 0 ? $"Give a treat ({treats} left)" : "Give a treat (you have none)", () => GiveTreat(p)),
            };
            AddWorldOptions(p, opts);
            opts.Add((_agent.Waiting ? "Come along!" : "Wait here", () =>
            {
                _agent.Waiting = !_agent.Waiting;
                p.Hud.Narrate(_agent.Waiting ? $"{Name} sits, tail curled around their feet. Very good. Very patient. (For now.)" : $"{Name} bounds after you.", false);
            }));
            opts.Add((_agent.Manual ? $"In fights: let {Name} act on their own" : "In fights: I'll give the orders", () =>
            {
                _agent.Manual = !_agent.Manual;
                p.Hud.Toast(_agent.Manual ? $"You'll choose {Name}'s moves in fights." : $"{Name} will fight on their own.", UIKit.UtilityBlue);
            }));
            opts.Add(("Never mind", () => { }));
            p.Hud.Push(new ChoiceScreen(Name, $"Bond {Hearts}" + (G.World.DragonScared ? "\nStill shaky from the last fight. A rest will help." : ""), opts));
        }

        /// <summary>Sniff, and lighting things with a tiny flame.</summary>
        void AddWorldOptions(PlayerSlot p, List<(string, Action)> opts)
        {
            opts.Add((G.World.DragonSniffUsed ? "Sniff around (needs a rest first)" : "Sniff around for secrets", () => Sniff(p)));
            var b = NearestColdBrazier(p);
            if (b != null) opts.Add(("Light that brazier!", () => Light(p, b)));
            if (p.Character.CountOf("torch") > 0 && p.Avatar.TorchMinutes <= 0)
                opts.Add(("Light my torch", () =>
                {
                    p.Avatar.LightTorch();
                    Emote("spark");
                    p.Hud.Narrate($"You hold out a torch. {Name} sneezes on it. Close enough: it's lit.", false);
                }));
        }

        public const float SniffRange = 8f;

        /// <summary>
        /// Once per short rest (shared by the party): every hidden spot within 8 tiles that nobody has found yet starts to sparkle,
        /// and its next check rolls with Advantage.
        /// </summary>
        public bool Sniff(PlayerSlot p)
        {
            if (G.World.DragonSniffUsed) { p.Hud.Toast($"{Name}'s nose is all sniffed out. It needs a rest.", UIKit.Bad); return false; }
            G.World.DragonSniffUsed = true;
            var zone = _agent.Actor.Zone;
            int found = 0;
            foreach (var s in FindObjectsByType<SecretInteractable>())
            {
                if (s.Def == null || G.World.Flag("secret:" + s.Def.Id) || !s.Available(p)) continue;
                if (zone == null || !zone.Objects.Contains(s.gameObject)) continue;
                if (Vector2.Distance(s.transform.position, transform.position) > SniffRange) continue;
                s.MarkSniffed();
                found++;
            }
            Emote(found > 0 ? "spark" : "happy");
            p.Hud.Narrate(found > 0
                ? $"{Name} sniffs the air, freezes, and points their whole body at {(found == 1 ? "something" : "a few things")} nearby, tail quivering. (Sparkling spots: Advantage on the check.)"
                : $"{Name} sniffs everywhere, sneezes, and finds nothing here.", false);
            return true;
        }

        UnlitBrazier NearestColdBrazier(PlayerSlot p) =>
            UnlitBrazier.All.Where(b => !b.Lit && Vector2.Distance(b.transform.position, p.Avatar.transform.position) <= 3f)
                .OrderBy(b => Vector2.Distance(b.transform.position, p.Avatar.transform.position)).FirstOrDefault();

        /// <summary>A puff of flame: the brazier lights up and stays lit.</summary>
        public bool Light(PlayerSlot p, UnlitBrazier b)
        {
            if (b == null || b.Lit) return false;
            if (_agent.Actor.Zone != p.Zone) { p.Hud.Toast($"{Name} isn't here.", UIKit.Bad); return false; }
            G.World.LitBraziers.Add(b.Key);
            b.SetLit();
            _agent.Actor.Face(b.transform.position);
            Emote("spark");
            Sfx.Play("spell");
            p.Hud.Narrate($"{Name} takes a deep, important breath and puffs a tiny flame. The brazier crackles to life!", false);
            return true;
        }

        static readonly string[] PetLines =
        {
            "{0} chirps and headbutts your hand. A tiny puff of smoke escapes their nostrils.",
            "{0} rolls over for belly scratches, then pretends that never happened.",
            "{0} curls around your ankle, radiating warmth like a happy little hearth.",
            "{0} nibbles your sleeve, very gently, to show you who's in charge.",
            "You scratch the soft spot behind {0}'s horn-nubs. One back leg thumps uncontrollably.",
            "{0} purrs. Dragons aren't supposed to purr. {0} doesn't care.",
            "{0} is trying to eat a pebble. You gently take it away. They look betrayed.",
        };

        public void PetIt(PlayerSlot p)
        {
            p.Hud.Narrate(string.Format(PetLines[UnityEngine.Random.Range(0, PetLines.Length)], Name), false);
            Emote("heart");
            string key = "dragon_pet:" + p.Index;
            if (G.World.IsLocked(key)) return;
            G.World.Lock(key, 60);
            AddBond(p, 2, "petting");
        }

        static readonly string[] TreatLines =
        {
            "{0} snatches the jerky mid-air, chews twice, and sneezes a shower of happy sparks.",
            "{0} eats the treat in one gulp, then checks your hands, your pockets and your boots for more.",
            "{0} takes the jerky very seriously, carries it in a circle three times, and finally eats it.",
        };

        public bool GiveTreat(PlayerSlot p)
        {
            if (!p.Character.RemoveItem("dragon_treat"))
            {
                p.Hud.Toast("You don't have any dragon treats. (Pip and the general store sell them.)", UIKit.Bad);
                return false;
            }
            p.Hud.Narrate(string.Format(TreatLines[UnityEngine.Random.Range(0, TreatLines.Length)], Name), false);
            Emote("spark");
            AddBond(p, 5, "a treat");
            return true;
        }

        // ================================================================== fetch

        /// <summary>Throws a stick 3–5 tiles ahead; the dragon runs it down and brings it back. +3 bond, once an hour per player.</summary>
        public IEnumerator Fetch(PlayerSlot p)
        {
            if (Busy || p.Avatar == null) yield break;
            var zone = _agent.Actor.Zone;
            if (zone != p.Zone) yield break;
            Vector2Int? spot = FetchSpot(p, zone);
            if (spot == null) { p.Hud.Toast("No room to throw a stick here.", UIKit.Bad); yield break; }
            Busy = true;
            var stick = new GameObject("FetchStick");
            stick.transform.position = zone.ToWorld(spot.Value);
            var sr = stick.AddComponent<SpriteRenderer>();
            sr.sprite = ArtRegistry.Get("obj_stick");
            sr.sortingOrder = YSort.OrderFor(stick.transform.position.y);
            p.Hud.Narrate($"You throw a stick. {Name} tears after it, wings flapping uselessly!", false);
            Emote("spark");
            yield return RunTo(() => stick.transform.position, 0.3f);
            Destroy(stick);
            Emote("happy");
            yield return RunTo(() => p.Avatar != null ? p.Avatar.transform.position : transform.position, 1.0f);
            Emote("heart");
            Busy = false;
            string key = "dragon_fetch:" + p.Index;
            if (!G.World.IsLocked(key)) { G.World.Lock(key, 60); AddBond(p, 3, "fetch"); }
        }

        Vector2Int? FetchSpot(PlayerSlot p, Zone zone)
        {
            Vector2 f = p.Avatar.Facing.sqrMagnitude > 0.01f ? p.Avatar.Facing.normalized : Vector2.down;
            var dirs = new List<Vector2> { f };
            for (int i = 0; i < 8; i++) dirs.Add(Quaternion.Euler(0, 0, 45 * i) * Vector2.up);
            foreach (var dir in dirs)
                for (int dist = 5; dist >= 3; dist--)
                {
                    var tile = zone.ToMap(p.Avatar.transform.position + (Vector3)(dir * dist));
                    if (zone.IsWalkable(tile) && zone.LineOfSight(p.MapPos, tile)) return tile;
                }
            return null;
        }

        IEnumerator RunTo(Func<Vector3> target, float near)
        {
            var actor = _agent.Actor;
            float timeout = 4f;
            while (timeout > 0 && Vector2.Distance(transform.position, target()) > near)
            {
                actor.Moving = true;
                actor.Face(target());
                transform.position = Vector3.MoveTowards(transform.position, target(), 6f * Time.deltaTime);
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (Vector2.Distance(transform.position, target()) > near) transform.position = target(); // stuck on a corner: hop there
            actor.Moving = false;
        }

        // ================================================================== emotes and naps

        /// <summary>A little speech-bubble above the dragon for 1.5 s: heart, spark, happy or sleep.</summary>
        public void Emote(string kind)
        {
            if (_emote == null)
            {
                var go = new GameObject("Emote");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.35f, 0.85f, 0);
                _emote = go.AddComponent<SpriteRenderer>();
                _emote.sortingOrder = 31000;
            }
            _emote.sprite = ArtRegistry.Get("fx_emote_" + kind);
            _emote.enabled = true;
            _emoteUntil = Time.time + 1.5f;
        }

        void Update()
        {
            if (_emote != null && _emote.enabled && Time.time > _emoteUntil) _emote.enabled = false;
            // Naps: nobody has moved for 20 seconds and it's by its leader.
            bool anyMoving = G.Players.Any(x => x.IsReady && x.Avatar.Moving);
            var leader = _agent.Leader;
            bool byLeader = leader != null && leader.IsReady && leader.Zone == _agent.Actor.Zone && Vector2.Distance(leader.Avatar.transform.position, transform.position) <= 2f;
            if (anyMoving || Busy || G.DragonFighting || !byLeader) { _still = 0; Napping = false; return; }
            _still += Time.deltaTime;
            if (_still < 20f) return;
            Napping = true;
            if (Time.time - _lastZ > 3f) { _lastZ = Time.time; Emote("sleep"); }
        }
    }
}
