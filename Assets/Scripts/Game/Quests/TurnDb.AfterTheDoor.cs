using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>Where a force stands once the party has chosen.</summary>
    public enum Stance { Against, Aside, With }

    /// <summary>
    /// Act III Phase 5: the turn. EXEMPT from the secret guard: none of this is reachable before WorldDb.MetHer and the
    /// meeting (the questioning only begins once both are set). Flag names and the three endings.
    /// </summary>
    public static class Turn
    {
        public const string End = "end", Seal = "seal", Save = "save";
        public static readonly string[] Endings = { End, Seal, Save };
        public const string Questioning = "questioning", Confessed = "king_confessed", Chosen = "turn_chosen", BeatsSeen = "turn_beats_seen";
        public const string FaeFavourCalled = "fae_favour_called", Betrayed = "betrayed_side";
        public static string EndingFlag(string ending) => "ending_" + ending;
        public static string Swayed(string force) => "swayed_" + force;
        public static string Heard(string force) => "turn_heard_" + force;
        /// <summary>The leaders, in the order they speak and are listed at the break.</summary>
        public static readonly string[] Forces = { "crown", "academy", "fae", "hunters" };

        /// <summary>The ending chosen at the turn, or null before it (only after WorldDb.MetHer can one be set).</summary>
        public static string Ending(WorldState w) => w == null || !w.Flag(WorldDb.MetHer) ? null : Endings.FirstOrDefault(e => w.Flag(EndingFlag(e)));
    }

    /// <summary>The force-reaction table, the save gate, and betrayal (spec: "who stands with you and who fights you").</summary>
    public static class TurnDb
    {
        static readonly Dictionary<string, Stance[]> Base = new Dictionary<string, Stance[]>
        {
            //                     end            seal           save
            ["crown"] = new[] { Stance.With, Stance.Against, Stance.Against },
            ["academy"] = new[] { Stance.Aside, Stance.With, Stance.Against },
            ["fae"] = new[] { Stance.Against, Stance.Aside, Stance.With },
            ["hunters"] = new[] { Stance.With, Stance.Aside, Stance.Against },
        };

        static int Col(string ending) => ending == Turn.End ? 0 : ending == Turn.Seal ? 1 : 2;
        static Stance Up(Stance s) => s == Stance.With ? s : s + 1;

        public static Stance Reaction(string force, string ending, WorldState w)
        {
            var s = Base[force][Col(ending)];
            if (force == "crown") return s; // the throne is the enemy unless you end her; Marsh's choice is MarshDefects
            if (w.Flag("banner_" + force)) s = Up(s);
            if (w.Flag(Turn.Swayed(force))) s = Up(s);
            if (force == "hunters" && ending == Turn.Save && w.Flag("wyrmwatch_belled")) s = Up(s);
            if (force == "academy" && w.Flag("wyvern_egg_saved")) s = Up(s);
            if (force == "academy" && w.Flag("marsh_asks_ashby") && MarshDefects(ending, w)) s = Up(s);
            if (force == "fae" && w.Flag("fae_clause") && s < Stance.Aside) s = Stance.Aside;
            return s;
        }

        /// <summary>Marsh (and some of the Household Guard) turn on the throne: not on the end path, and only if she's yours.</summary>
        public static bool MarshDefects(string ending, WorldState w) => ending != Turn.End && (w.Flag("banner_crown") || w.Flag(Turn.Swayed("crown")));

        public const string GateHeld = "You don't understand how the fire is held in her. (Look at the fire in her with Arcana, or win Ashby over.)";
        public const string GatePact = "Nobody here knows how the old Pact worked. (Win Sorrel over, or read the ring of the Great Ones in the Deep.)";
        public const string GateTrust = "She doesn't know anyone is coming back for her. (Go back down and promise her.)";

        /// <summary>What the party still needs before it can save her. Empty = the way is open. Never a hidden fail state.</summary>
        public static List<string> SaveGateMissing(WorldState w)
        {
            var missing = new List<string>();
            if (!(w.Flag("seal_understood") || w.Flag(Turn.Swayed("academy")))) missing.Add(GateHeld);
            if (!(w.Flag(Turn.Swayed("fae")) || w.Flag(Turn.FaeFavourCalled) || w.LoreFound.Contains("the_great_ones_below"))) missing.Add(GatePact);
            if (!w.Flag("promised_her")) missing.Add(GateTrust);
            return missing;
        }

        /// <summary>The side the party stood with in Act II ends up fighting it.</summary>
        public static bool Betrays(string ending, WorldState w)
        {
            var side = w.CommittedFaction;
            return side != null && Base.ContainsKey(side) && Reaction(side, ending, w) == Stance.Against;
        }

        /// <summary>One line for each force at the break. `{name}` is not used: every line reads without the dragon.</summary>
        public static string BreakLine(string force, Stance s, string ending, WorldState w)
        {
            string line;
            switch (force)
            {
                case "crown":
                    line = s == Stance.With ? "Marsh draws her sword and stands beside you. \"For the realm. God help us all.\""
                         : "\"Guards.\" The King doesn't raise his voice. He doesn't need to.";
                    break;
                case "academy":
                    line = s == Stance.With ? "Ashby rolls up his sleeves. \"Well. I suppose I'd better be useful.\""
                         : s == Stance.Aside ? "Ashby shakes his head and steps back. \"I won't stop you. I won't help you either.\""
                         : "Ashby raises his staff, and his hands are shaking. \"I'm sorry. I can't let you do this.\"";
                    break;
                case "fae":
                    line = s == Stance.With ? "Sorrel laughs, delighted, and the flowers in the market all turn to face you."
                         : s == Stance.Aside ? "Sorrel shrugs. \"The Courts will watch. We're very good at watching.\""
                         : "Sorrel's smile goes thin. \"That's not the bargain. That was never the bargain.\"";
                    break;
                default:
                    line = s == Stance.With ? "Calloway whistles, and Wyrmwatch crossbows come up on every roof around the square, pointed away from you."
                         : s == Stance.Aside ? "Calloway spits. \"Your funeral. We'll be at the inn.\""
                         : "Calloway's crossbow comes up, and so do forty others.";
                    break;
            }
            if (s == Stance.Against && w.CommittedFaction == force)
                line = force == "crown" ? "\"You stood with us,\" the King says. \"You stood with us. Guards.\"" : "\"You stood with us.\" " + line;
            return line;
        }

        public const string MarshLine = "Marsh looks at the King for a long moment. Then she unpins the royal badge from her collar, drops it on the Sunstair, and walks down the steps to stand with you. A dozen of the Household Guard follow her.";
        public const string GuardsWhoStayed = "The King looks at the guards who stayed.";

        /// <summary>The break's second beat: every force, crown first.</summary>
        public static string BreakSummary(string ending, WorldState w)
        {
            var lines = new List<string>();
            foreach (var f in Turn.Forces)
            {
                var s = Reaction(f, ending, w);
                if (f == "crown" && MarshDefects(ending, w))
                {
                    lines.Add(MarshLine);
                    lines.Add(GuardsWhoStayed + " " + BreakLine(f, s, ending, w));
                }
                else lines.Add(BreakLine(f, s, ending, w));
            }
            return string.Join("\n", lines);
        }
    }
}
