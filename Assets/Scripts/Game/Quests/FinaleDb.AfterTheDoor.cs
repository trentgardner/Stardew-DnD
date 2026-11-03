using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 6: the finales. EXEMPT from the secret guard: nothing here is reachable before WorldDb.MetHer and the
    /// choice at the turn (Turn.Chosen). Flag names and ids.
    /// </summary>
    public static class Finale
    {
        /// <summary>Every finale fight's group id starts with this (allies only join these).</summary>
        public const string Prefix = "finale_";
        public static bool IsFinaleGroup(string groupId) => groupId != null && groupId.StartsWith(Prefix);

        /// <summary>The fight through the streets: three stages, in order.</summary>
        public const int Stages = 3;
        public static string StreetGroup(int n) => Prefix + "street_" + n;
        public static string StreetOpen(int n) => Prefix + "street_open_" + n;
        public static string StreetDone(int n) => Prefix + "street_done_" + n;
        /// <summary>The break's beats have played and the fight for the city has begun.</summary>
        public const string Started = "finale_started";
        /// <summary>End her: the dragon won't fight for it, and waits at the top of the stair down until the flight.</summary>
        public const string DragonWaits = "finale_dragon_waits";
        /// <summary>The dragon is down for a rider and the prompt is waiting for someone to confirm.</summary>
        public const string RiderPending = "finale_rider_pending";
        /// <summary>The flight is over and the party has landed for the last fight.</summary>
        public const string Flown = "finale_flown";
        public static string RiderFlag(int playerIndex) => "finale_rider_" + playerIndex;
        /// <summary>The last fight for each ending.</summary>
        public static string BossGroup(string ending) => Prefix + "boss_" + ending;
        public static string BossOpen(string ending) => Prefix + "boss_open_" + ending;
        /// <summary>The last fight is won (Phase 7's endings begin here).</summary>
        public const string Won = "finale_won";
        /// <summary>Seal her: Ashby needs this many rounds at the door.</summary>
        public const int SealRounds = 7;

        /// <summary>1..3 = the first stage not yet won (open or about to be); 4 = all won; 0 = the fight hasn't begun.</summary>
        public static int CurrentStreet(WorldState w)
        {
            if (w == null || !w.Flag(Started)) return 0;
            for (int n = 1; n <= Stages; n++) if (!w.Flag(StreetDone(n))) return n;
            return Stages + 1;
        }
    }

    /// <summary>The finales' tables, built from the ending and Act II state (TurnDb's reactions).</summary>
    public static partial class FinaleDb
    {
        static readonly Dictionary<string, (string monsterId, string name)> AllyOf = new Dictionary<string, (string, string)>
        {
            ["crown"] = ("crown_knight", "Crown Knight"),
            ["academy"] = ("academy_warden", "Ashby's Ward-Mage"),
            ["fae"] = ("fae_knight", "Knight of the Courts"),
            ["hunters"] = ("wyrmwatch_hunter", "Wyrmwatch Hunter"),
        };

        public const int MaxAllies = 3;

        /// <summary>One ally per force that stands With you (Turn.Forces order), plus Marsh's Household Guard if she defects. At most 3.</summary>
        public static List<(string monsterId, string name)> Allies(string ending, WorldState w)
        {
            var list = new List<(string, string)>();
            if (ending == null || w == null) return list;
            if (TurnDb.MarshDefects(ending, w)) list.Add(("crown_knight", "Household Guard"));
            foreach (var f in Turn.Forces)
                if (TurnDb.Reaction(f, ending, w) == Stance.With) list.Add(AllyOf[f]);
            return list.Take(MaxAllies).ToList();
        }

        // ------------------------------------------------------------------ the streets

        static readonly Vector2Int[] Spots = { new Vector2Int(31, 19), new Vector2Int(52, 25), new Vector2Int(55, 30) };
        static readonly Vector2Int[] Retries = { new Vector2Int(31, 15), new Vector2Int(44, 24), new Vector2Int(49, 28) };
        static readonly Vector2Int[] Spread = { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1) };
        static readonly string[] Titles = { "The Great Square", "The Wyvern Yard", "The Head of the Roots Stair" };

        /// <summary>The top of the stair down to the Roots, where the dragon lies on the End path.</summary>
        public static readonly Vector2Int DragonWaitSpot = new Vector2Int(58, 29);

        public static Vector2Int StreetSpot(int n) => Spots[n - 1];
        public static Vector2Int RetrySpot(int n) => Retries[Mathf.Clamp(n, 1, Finale.Stages) - 1];
        public static string StreetTitle(int n) => Titles[n - 1];

        /// <summary>Who stands in the way at stage n (at most 6, one per tile around StreetSpot).</summary>
        public static List<EnemySpawn> StreetMembers(int n, string ending, WorldState w)
        {
            var ids = new List<string>();
            if (ending == Turn.End)
                ids.AddRange(n == 1 ? new[] { "cinder_hound", "cinder_hound" } : n == 2 ? new[] { "fire_elemental", "cinder_hound", "cinder_hound" } : new[] { "fire_elemental", "fire_elemental", "salamander" });
            else
            {
                // The guards who stayed with the King.
                var core = new List<string>(n == 1 ? new[] { "crown_knight", "crown_knight" } : n == 2 ? new[] { "crown_knight", "crown_knight", "crown_battlemage" } : new[] { "crown_champion", "crown_knight", "crown_knight" });
                if (TurnDb.MarshDefects(ending, w)) core.Remove("crown_knight");
                ids.AddRange(core);
            }
            bool Against(string f) => TurnDb.Reaction(f, ending, w) == Stance.Against;
            if (Against("academy")) ids.Add(n == 1 ? "academy_warden" : n == 2 ? "stone_guardian" : null);
            if (Against("fae")) ids.Add(n == 1 ? "fae_knight" : n == 3 ? "thorn_guardian" : null);
            if (Against("hunters") && n == 2) { ids.Add("wyrmwatch_hunter"); ids.Add("wyrmwatch_hunter"); }
            var at = StreetSpot(n);
            return ids.Where(i => i != null).Take(Spread.Length).Select((id, i) => new EnemySpawn(id, at.x + Spread[i].x, at.y + Spread[i].y)).ToList();
        }

        /// <summary>The beats before each stage's fight: the city, then who's waiting.</summary>
        public static string[] StreetIntro(int n, string ending, WorldState w)
        {
            string name = w != null && w.EggHatched ? w.DragonName : "the dragon";
            if (ending == Turn.End)
                return n == 1 ? new[]
                {
                    "Every lamp in Aurelhold is out. For the first time in a thousand years the city is dark at noon, and very, very cold.",
                    "Then the paving of the Great Square cracks, and things made of fire come up through it like water through a broken dam. They aren't her. They're what was poured into her, looking for a way out.",
                }
                : n == 2 ? new[]
                {
                    "The Wyvern Yard is full of smoke. Marigold Penn is throwing buckets of ale at a burning hound and swearing like a sailor.",
                    "More fire is coming up from the Roots. Whatever is under the city, it is getting bigger, and it is getting closer to the surface.",
                }
                : new[]
                {
                    $"At the head of the Roots stair the heat is like a forge door. {Cap(name)} is still lying across the top step, and won't look at you.",
                    "The stair is pouring fire. Hold it here, and whatever is coming will have to come up somewhere else.",
                };
            if (n == 1) return new[]
            {
                "Steel comes out all over the Great Square. The Household Guard closes ranks on the Sunstair, and the King walks back into his palace without looking round.",
                "\"Hold them here,\" a captain shouts. \"By order of the King. Nobody leaves the square.\"",
            };
            if (n == 2) return new[]
            {
                "The Wyvern Yard. Somebody has dragged the Pageant's scenery across the lane as a barricade: a paper Queen, wings and all, lying on her side.",
                "Behind it, more of the King's soldiers, and they've had time to get ready.",
            };
            return new[]
            {
                "The head of the Roots stair. The way down is the only way that matters now, and the King knows it.",
                "His champion is standing on the top step, with the last of the guard behind her.",
            };
        }

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }

    /// <summary>What a pass of the flight wins for the boss fight.</summary>
    public enum FlightEffect { BossWounded, NoLegendaryFirstRound, AddsScattered, PartyInspired }

    /// <summary>One choice on a pass: the rider's check, its DC, and what success wins.</summary>
    public class FlightOption
    {
        public string Label;
        public Rules.Skill Skill;
        public int Dc;
        public FlightEffect OnSuccess;
        public string Success, Failure;
    }

    public class FlightPass
    {
        public string Title, Text;
        public List<FlightOption> Options = new List<FlightOption>();
    }

    /// <summary>The flight over Aurelhold for one ending: where it goes, its two passes, and where it lands.</summary>
    public class FlightRoute
    {
        public List<Vector2Int> Waypoints;   // in the capital; a pass comes at waypoints 1 and 3
        public FlightPass[] Passes;          // exactly 2
        public string LandingZone;
        public Vector2Int LandingSpot;
        public string[] Takeoff, Landing;
    }

    public static partial class FinaleDb
    {
        /// <summary>Who waits at the landing. Seal: the King and his Ring-knights (one fewer if the flight scattered them).</summary>
        public static List<EnemySpawn> BossMembers(string ending, bool scattered)
        {
            if (ending == Turn.End) return new List<EnemySpawn> { new EnemySpawn("ashen_queen", 31, 25) };
            if (ending == Turn.Save) return new List<EnemySpawn> { new EnemySpawn("crowned_fire", 31, 12) };
            var list = new List<EnemySpawn> { new EnemySpawn("king_regalia", 24, 9), new EnemySpawn("crown_knight", 22, 10) };
            if (!scattered) list.Add(new EnemySpawn("crown_knight", 26, 10));
            return list;
        }

        /// <summary>Where Ashby works at the grey door while you hold the King off.</summary>
        public static readonly Vector2Int AshbyAtTheDoor = new Vector2Int(24, 6);

        /// <summary>The beats after the last fight is won. Phase 7 writes what comes after.</summary>
        public static string[] Victory(string ending, WorldState w)
        {
            string name = Cap(w != null && w.EggHatched ? w.DragonName : "the dragon");
            if (ending == Turn.End) return new[]
            {
                "The fire goes out of it all at once, like a candle pinched between two fingers. What's left falls in on itself, and is ash, and the Great Square is very, very quiet.",
                $"{name} creeps forward and noses at the ashes, very gently, as if it's looking for someone. Then it lies down beside them, and won't get up.",
            };
            if (ending == Turn.Seal) return new[]
            {
                "Ashby closes the last circle. The grey door shuts with a sound like a sigh, and every chain in the Deep pulls tight at once.",
                "Behind the door, very faintly, someone knocks. Once. Then nothing. The King's regalia goes dark and cold, and so, far above you, do the lamps of Aurelhold.",
            };
            return new[]
            {
                "The fire tears itself out of the King and goes up, and out, and round: into the twelve Great Ones lying in their ring. All the way round the Wyrmback, the mountains begin to glow.",
                "What's left on the Sunstair is an old man without a crown, blinking at the sky. And far below, behind a grey door that's standing open, a little girl wakes up warm, and isn't afraid.",
            };
        }

        /// <summary>The landing spots: the Great Square before the statue, the Deep before the grey door, the foot of the Sunstair.</summary>
        public static readonly Vector2Int EndLanding = new Vector2Int(31, 20), SealLanding = new Vector2Int(24, 14), SaveLanding = new Vector2Int(31, 16);

        static FlightOption Opt(string label, Rules.Skill skill, int dc, FlightEffect fx, string success, string failure) =>
            new FlightOption { Label = label, Skill = skill, Dc = dc, OnSuccess = fx, Success = success, Failure = failure };

        public static FlightRoute Route(string ending, WorldState w)
        {
            string name = w != null && w.EggHatched ? w.DragonName : "the dragon";
            string Name = Cap(name);
            var start = DragonWaitSpot;
            if (ending == Turn.End)
                return new FlightRoute
                {
                    Waypoints = new List<Vector2Int> { start, new Vector2Int(44, 20), new Vector2Int(20, 13), new Vector2Int(10, 9), EndLanding },
                    LandingZone = "capital", LandingSpot = EndLanding,
                    Takeoff = new[]
                    {
                        $"{Name} gets up off the stair. It looks down into the Roots for a long moment, at the fire pouring up it. Then it looks at you. It isn't her any more. {Name} knows it before you do.",
                        $"It crouches, and holds still, the way it never has: an invitation. One of you climbs up. Then the ground drops away, and Aurelhold is a map of fire below you.",
                    },
                    Passes = new[]
                    {
                        new FlightPass
                        {
                            Title = "Over the Great Square", Text = "Something enormous is climbing the spire of the Lantern Cathedral, wings half open, the stones glowing where it grips them. The legend's shape. Every statue in the city got it right.",
                            Options =
                            {
                                Opt($"Breathe on it as it climbs (Animal Handling, DC 15)", Rules.Skill.AnimalHandling, 15, FlightEffect.BossWounded, $"{Name} pours fire into fire, and for once it's the bigger flame. The thing on the spire screams and loses its grip.", "It turns its head, and the breath slides off it like water. Its answer singes you as you pull away."),
                                Opt("Bank hard and come at it out of the sun (Survival, DC 15)", Rules.Skill.Survival, 15, FlightEffect.NoLegendaryFirstRound, "It never sees you coming. When you hit, it's still looking the wrong way.", "It sees you coming a mile off. A wingtip catches you on the way past."),
                            },
                        },
                        new FlightPass
                        {
                            Title = "The cathedral spire", Text = "It lets go of the spire and comes for you. The sky turns to fire.",
                            Options =
                            {
                                Opt("Dive through the smoke and drive it down (Athletics, DC 17)", Rules.Skill.Athletics, 17, FlightEffect.AddsScattered, "You fold, drop and hit it square. It goes down into the Great Square like a falling star, and half the fire in the streets goes out with the impact.", "You hit it, but it hits back. You go down together, the long way."),
                                Opt("Pull up and shout to the city below (Persuasion, DC 17)", Rules.Skill.Persuasion, 17, FlightEffect.PartyInspired, "Your voice carries over the whole square. Below you, every friend you've got hears it, and looks up, and grins.", "The roar of the fire takes your words away. You wheel and go down to join them anyway."),
                            },
                        },
                    },
                    Landing = new[]
                    {
                        "The thing from the spire hits the Great Square hard enough to crack the Victory statue in half. It gets up. Of course it gets up.",
                        $"{Name} lands beside you in a spray of sparks, and the others come running in from the edges of the square. Whatever this is, it ends here.",
                    },
                };
            if (ending == Turn.Seal)
                return new FlightRoute
                {
                    Waypoints = new List<Vector2Int> { start, new Vector2Int(48, 14), new Vector2Int(40, 6), new Vector2Int(56, 4), new Vector2Int(60, 4) },
                    LandingZone = "the_deep", LandingSpot = SealLanding,
                    Takeoff = new[]
                    {
                        "The King's men have barred the Roots stair and set it burning. There's no way down on foot.",
                        $"\"There's another way,\" Ashby says, very quietly, and looks at {name}. \"The Chimney. Where the warm air comes up.\" {Name} crouches. One of you climbs up, and Ashby climbs up behind, holding on for dear life.",
                    },
                    Passes = new[]
                    {
                        new FlightPass
                        {
                            Title = "Over the palace wall", Text = "Bolt-throwers on the palace wall swing round to follow you. Someone up there has been waiting a thousand years for a dragon to shoot at.",
                            Options =
                            {
                                Opt($"Burn the bolt-throwers (Animal Handling, DC 15)", Rules.Skill.AnimalHandling, 15, FlightEffect.AddsScattered, "The bolt-throwers go up like kindling, and the knights waiting beside them run for the stairs. Fewer of them will make it down.", "A bolt goes past close enough to part your hair. The next one doesn't miss."),
                                Opt("Fly low over the rooftops (Acrobatics, DC 15)", Rules.Skill.Acrobatics, 15, FlightEffect.NoLegendaryFirstRound, "Chimney pots, washing lines, a startled cat. Nobody on the wall gets a clean shot, and nobody below knows where you went.", "You clip a chimney. Bricks everywhere. Everyone knows exactly where you went."),
                            },
                        },
                        new FlightPass
                        {
                            Title = "The Chimney", Text = "A black shaft into the mountain, wide as a street, with warm air roaring up out of it. Far, far down, a red glow.",
                            Options =
                            {
                                Opt("Fold the wings and drop (Athletics, DC 17)", Rules.Skill.Athletics, 17, FlightEffect.BossWounded, "You fall like a stone, open your wings at the last moment, and land hard enough to knock the King off his feet.", "The updraught throws you against the rock, again and again, all the way down."),
                                Opt("Hold on and let Ashby ward you (Arcana, DC 17)", Rules.Skill.Arcana, 17, FlightEffect.PartyInspired, "Ashby's ward closes round you like a hand, and you come down out of the dark with the light of it still on your skin.", "The ward flickers out halfway down. Ashby says a word his students never heard him say."),
                            },
                        },
                    },
                    Landing = new[]
                    {
                        "You come out of the dark into the Deep, and land in front of the grey door. The King is already there, in black iron and gold, with the Ring-knights behind him.",
                        "\"You'd lock her away from me,\" King Aldric says. \"From the realm. Do you know how cold it will be?\" Ashby is already drawing the first circle on the floor. \"Keep him off me,\" he says. \"Seven minutes. Maybe less.\"",
                    },
                };
            return new FlightRoute
            {
                Waypoints = new List<Vector2Int> { start, new Vector2Int(44, 22), new Vector2Int(36, 12), new Vector2Int(31, 5), SaveLanding },
                LandingZone = "capital", LandingSpot = SaveLanding,
                Takeoff = new[]
                {
                    "Every lamp in Aurelhold flares white at once, and every one of them leans toward the palace, like candles in a draught. Up on the palace roof, something is drinking them.",
                    $"{Name} doesn't wait to be asked. It crouches beside you, wings already open. One of you climbs up, and the Great Square drops away beneath you.",
                },
                Passes = new[]
                {
                    new FlightPass
                    {
                        Title = "Over the lamps", Text = "Lines of fire run from every lamp in the city to the palace roof. Where they meet, a shape is standing up, and up, and up, wearing a crown.",
                        Options =
                        {
                            Opt("Breathe on the lamp-lines and cut them (Animal Handling, DC 15)", Rules.Skill.AnimalHandling, 15, FlightEffect.AddsScattered, "The lines part where you burn them, and half the lamps in the city go dark. Whatever is on the roof, it's getting less to drink.", "The lines are too bright to fly through. You come round singed and empty-handed."),
                            Opt("Go high, and come down on it (Acrobatics, DC 15)", Rules.Skill.Acrobatics, 15, FlightEffect.BossWounded, "You climb until the air is thin, then drop. You hit it square in the crown.", "It turns its burning face up to you as you come down, and you have to break away."),
                        },
                    },
                    new FlightPass
                    {
                        Title = "The palace roof", Text = "It sees you. It has the King's eyes, and the King's voice: \"Mine. All of it. It was always mine.\"",
                        Options =
                        {
                            Opt($"Roar back at it (Intimidation, DC 17)", Rules.Skill.Intimidation, 17, FlightEffect.NoLegendaryFirstRound, $"{Name} roars, and so do you. For one moment the thing on the roof forgets it's a king, and remembers it's afraid.", "It laughs, and the laugh is made of fire."),
                            Opt("Call down to the square: \"Now!\" (Persuasion, DC 17)", Rules.Skill.Persuasion, 17, FlightEffect.PartyInspired, "Down in the square, everybody who ever liked you hears it. You can see them grinning from up here.", "Nobody can hear you over the roaring. You go down to them instead."),
                        },
                    },
                },
                Landing = new[]
                {
                    "It comes down off the roof to meet you, and lands at the top of the Sunstair. The steps crack under its feet. The King's crown is still on its head, gone white-hot.",
                    $"{Name} lands at the foot of the stair, and the others come up beside you. Up on the steps, the Crowned Fire spreads its arms. \"Kneel,\" it says.",
                },
            };
        }
    }
}
