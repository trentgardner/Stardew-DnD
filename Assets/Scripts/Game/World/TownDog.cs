using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Buoy, Thistledown's neighbourhood dog: a black-and-white Australian Shepherd who belongs to everybody on South Lane
    /// and nobody in particular. Not a companion. He wanders his patch by day, naps on the porch at night, and loves pets.
    /// (A dedication: the name and the colours are a real dog's.)
    /// </summary>
    public class TownDog : Interactable
    {
        public Actor Actor;
        public const float Roam = 5f;

        /// <summary>
        /// Where Buoy spends each day of the week: mostly South Lane, the rest day outside the Sleeping Drake (Pip slips him
        /// scraps), and Leafday splashing about at Miller's Ford. Acts II and III: point these at the new home base.
        /// </summary>
        public static (string zone, Vector2Int tile) HomeFor(int weekday)
        {
            switch (weekday)
            {
                case WorldDb.Leafday: return ("ford", new Vector2Int(6, 14));
                case WorldDb.Hearthday: return ("town", new Vector2Int(32, 12));
                default: return ("town", new Vector2Int(36, 43));
            }
        }
        public Vector2Int HomeSpot => HomeFor(GameRoot.I.Clock.WeekdayIndex).tile;
        public override float Range => 1.4f;
        SpriteRenderer _emote;
        float _emoteUntil;

        public static string CollarText =>
            "A well-loved leather collar with a little brass tag, worn smooth from years of chin scratches. It reads: BUOY.\n\n" +
            "On the back, in tiny letters: \"If found, he's probably just visiting. Give him a pat and point him home.\"";

        static readonly string[] PetLines =
        {
            "Buoy wiggles his whole back half so hard he nearly falls over. Aussies don't wag their tails; they wag everything.",
            "Buoy leans his entire weight against your legs and gazes up at you like you invented sunshine.",
            "Buoy flops onto his back for belly rubs, paws tucked up, completely shameless.",
            "Buoy tries to herd you toward a nice patch of shade. You let him.",
            "You scratch behind Buoy's ears. He makes a happy little \"awoo\" and nudges your hand for more.",
            "Buoy brings you a stick, drops it at your feet, then absolutely refuses to let go of it.",
            "Buoy sneezes with joy, spins in a circle, and presses his nose into your palm.",
        };

        public static TownDog Spawn(GameRoot g)
        {
            var (zoneId, tile) = HomeFor(g.Clock.WeekdayIndex);
            var zone = g.Zones[zoneId];
            var a = Actor.Spawn<Actor>("Buoy", "npc_buoy", false, zone, NearestWalkable(zone, tile));
            var d = a.gameObject.AddComponent<TownDog>();
            d.Actor = a;
            return d;
        }

        static Vector2Int NearestWalkable(Zone z, Vector2Int at)
        {
            for (int r = 0; r < 6; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var p = at + new Vector2Int(dx, dy);
                        if (z.IsWalkable(p)) return p;
                    }
            return at;
        }

        static bool Night => GameRoot.I.Clock.Hour >= 21 || GameRoot.I.Clock.Hour < 6;

        public override string PromptText(PlayerSlot p) => Night ? "Pet Buoy (he's snoozing on the porch)" : "Say hi to Buoy";

        public override void Interact(PlayerSlot p)
        {
            Actor.Face(p.Avatar.transform.position);
            p.Hud.Push(new ChoiceScreen("Buoy", Night ? "A black-and-white Aussie, curled up nose-to-tail. One ear lifts when you come close." :
                "A black-and-white Australian Shepherd with a white blaze and a very busy tail end. He trots over like you're old friends.",
                new List<(string, System.Action)>
                {
                    ("Pet Buoy", () => PetHim(p)),
                    ("Look at his collar", () => p.Hud.Push(new MessageScreen("Buoy's Collar", CollarText))),
                    ("Never mind", () => { }),
                }));
        }

        /// <summary>Pets (any time) and, the first time each day, a good-boy blessing: Heroic Inspiration.</summary>
        public void PetHim(PlayerSlot p)
        {
            var g = GameRoot.I;
            p.Hud.Narrate(PetLines[Random.Range(0, PetLines.Length)], false);
            Emote("heart");
            g.AwardOnce(p, "met_buoy", 10, "making friends with Buoy");
            string key = "buoy_pet:" + p.Index;
            if (g.World.IsLocked(key)) return;
            g.World.Lock(key, 24 * 60 - g.Clock.Minutes); // until midnight
            p.Character.HeroicInspiration = true;
            p.Hud.Toast("Nobody stays grumpy after petting Buoy. Heroic Inspiration!", UIKit.Gold);
        }

        void Emote(string kind)
        {
            if (_emote == null)
            {
                var go = new GameObject("Emote");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0.35f, 0.8f, 0);
                _emote = go.AddComponent<SpriteRenderer>();
                _emote.sortingOrder = 31000;
            }
            _emote.sprite = ArtRegistry.Get("fx_emote_" + kind);
            _emote.enabled = true;
            _emoteUntil = Time.time + 1.5f;
        }

        void Start() => StartCoroutine(Life());

        void Update() { if (_emote != null && _emote.enabled && Time.time > _emoteUntil) _emote.enabled = false; }

        /// <summary>Potter about the neighbourhood by day; go home and nap at night.</summary>
        IEnumerator Life()
        {
            var g = GameRoot.I;
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(2f, 6f));
                // A new day somewhere else: he trots over while nobody's looking.
                var (zoneId, home) = HomeFor(g.Clock.WeekdayIndex);
                if (Actor.Zone.Def.Id != zoneId)
                {
                    if (g.AnyPlayerIn(Actor.Zone) || !g.Zones.TryGetValue(zoneId, out var next)) continue;
                    Actor.PlaceAt(next, NearestWalkable(next, home));
                }
                var zone = Actor.Zone;
                if (Night)
                {
                    yield return WalkTo(NearestWalkable(zone, home));
                    Emote("sleep");
                    continue;
                }
                var target = home + new Vector2Int(Random.Range(-(int)Roam, (int)Roam + 1), Random.Range(-(int)Roam, (int)Roam + 1));
                if (!zone.IsWalkable(target) || Vector2.Distance(target, home) > Roam) continue;
                yield return WalkTo(target);
            }
        }

        IEnumerator WalkTo(Vector2Int target)
        {
            var zone = Actor.Zone;
            var path = zone.FindPath(Actor.MapPos, target, null, 400);
            if (path == null) yield break;
            foreach (var step in path)
            {
                var w = zone.ToWorld(step);
                Actor.Face(w);
                Actor.Moving = true;
                while (Vector2.Distance(transform.position, w) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, w, 2.2f * Time.deltaTime);
                    yield return null;
                }
            }
            Actor.Moving = false;
        }
    }
}
