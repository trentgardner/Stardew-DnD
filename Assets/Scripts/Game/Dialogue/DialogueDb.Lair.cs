using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: the people below Aurelhold, down to the door (never beyond it).
    /// Dread and suspicion of the Crown are allowed. Nothing here may hint at THE SECRET (LoreTests scans this file).
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue LairDialogue(string id)
        {
            switch (id)
            {
                case "grey_door": return GreyDoorChoices();
                case "gaoler_parley": return GaolerParley();
                case "bran": return Bran();
                case "hap_mossy": return HapMossy();
                case "deep_shift_1": return DeepShiftHand("deep_shift_1", "Lew Carrow",
                    "Scrape, oil, scrape, oil. The pipes like it gentle. Don't ask me how I know. You just know, after a while, the way you know a horse.");
                case "deep_shift_2": return DeepShiftHand("deep_shift_2", "Dobbin Hale",
                    "Up top? Haven't seen it in six years. Is the King's Square still gold? I used to sell chestnuts there. Funny what you miss.");
                default: return null;
            }
        }

        // =================================================================== the grey door

        static Dialogue GreyDoorChoices()
        {
            var d = new Dialogue("grey_door", c => "door");
            d.Node("door", null,
                c => "",
                c => "The grey door from the dream. It's warm to the touch, like a hearthstone, and the chains end in its frame. There's no lock. There's no handle on this side at all. There's a handle on the other side; you can tell from the hinges.")
                .Say("Knock.", doIt: c => Knock(c), end: true)
                .Say("Open the door.", doIt: c => c.G.OpenGreyDoor(c.P), end: true)
                .Bye("Not yet.");
            return d;
        }

        static Dialogue GaolerParley()
        {
            const string tried = "gaoler_deceive";
            var d = new Dialogue("gaoler_parley", c => "face");
            void Fight(DCtx c) { c.Screen?.Close(); c.G.StartGroupCombat("gaoler", c.P); }
            void Kneel(DCtx c) { c.Screen?.Close(); c.G.PacifyGroup("gaoler", c.P); }
            void BackAway(DCtx c) { c.G.World.ClearFlag("parley:gaoler"); c.G.CalmGroup("gaoler"); }
            d.Node("face", "The Iron Gaoler",
                c => "HOLD.",
                c => "The word comes out of it like a bell rung underwater. It takes one step toward you, and the whole floor feels it.")
                .Roll(Skill.Deception, 18, "(Deception) Speak as the King: \"Stand aside.\"", "kneels", "unconvinced",
                      onFailure: c => c.G.World.Lock(tried, 24 * 60), show: c => !c.G.World.IsLocked(tried))
                .Say("(Draw your weapon)", doIt: Fight, end: true)
                .Say("(Back away, slowly)", doIt: BackAway, end: true);
            d.Node("kneels", "The Iron Gaoler", c => "...HOLD.",
                c => "You give the order in the King's own cadence. The Gaoler's head turns, slowly, to the door, and back to you. Then it kneels, the way it must have knelt a thousand times, and is still.")
                .Say("(Walk past it)", doIt: Kneel, end: true);
            d.Node("unconvinced", "The Iron Gaoler", c => "HOLD.",
                c => "It has heard one voice for a thousand years, and that wasn't it. The chains on its arms begin to unwind.")
                .Say("(Draw your weapon)", doIt: Fight, end: true)
                .Say("(Back away, fast)", doIt: BackAway, end: true);
            return d;
        }

        static void Knock(DCtx c)
        {
            c.G.World.SetFlag("knocked_back");
            LoreDb.Find("the_door", c.P);
            c.P.Hud.Narrate("You knock: three times, slow. For a long moment, nothing. Then, from the other side, very quietly: three knocks back.", true);
        }

        // =================================================================== the deep shift

        static Dialogue Bran()
        {
            var d = new Dialogue("bran", c => c.G.Quests.Stage(DeepShift.Id) == 0 ? "stranger" : c.G.Quests.Stage(DeepShift.Id) >= DeepShift.Home ? "home" : "start");
            const string who = "Bran Tolley";
            void Home(DCtx c) => c.G.Quests.Set(DeepShift.Id, DeepShift.Home, c.P);
            d.Node("stranger", who,
                c => "Visitors? Down here? ...Mind the pipes, they're warm. Sorry. Shift's on. If you're ever up in the Roots, there's a laundress called Mags by the Steamhouse. Tell her I'm well.")
                .Bye("We'll tell her.");
            d.Node("start", who,
                c => "Mags sent you? Mags sent you. Eleven years down. The Keepers say if we go up, the fire comes with us: into the Roots, into my Mags. So we stay, and they pay, and she gets the money.",
                c => "He's holding a rag so oily it shines. His hands are shaking.",
                c => { if (c.G.Quests.Stage(DeepShift.Id) < DeepShift.Below) c.G.Quests.Set(DeepShift.Id, DeepShift.Below, c.P); }) // finding him counts
                .Roll(Skill.Persuasion, 15, "(Persuasion) \"The fire's been going up those pipes for years. It's in every lamp in the city. It never hurt anyone.\"", "going", "stay", onSuccess: Home)
                .Roll(Skill.Insight, 13, "(Insight) Read him.", "homesick", "stay")
                .Say("There's no one left to stop you. The Keepers have stood aside.", "going", doIt: Home, show: c => c.G.World.Flag("keepers_resolved"))
                .Bye("Think about it.");
            d.Node("homesick", who, c => "...What?",
                    c => "He isn't staying for the Crown, or the fire, or even the money. He's staying because eleven years is a long time, and he's afraid she's stopped waiting.")
                .Say("\"Mags didn't marry the money. She writes you every week. She still lights a candle for you.\"", "going", doIt: Home)
                .Bye("Think about it.");
            d.Node("stay", who, c => "I can't. I'm sorry. Tell her I'm well.", c => "He goes back to the pipes, scraping harder than he needs to.").Bye("We'll come back.");
            d.Node("going", who,
                c => "...Every week? ...Right. Right. Hap! HAP! I'm going up. Tell the Keepers they can dock my pay. Tell them they can have it all.",
                c => "He drops the rag on the floor and walks to the stair without looking back. Then he stops, and comes back, and shakes all your hands, and goes.")
                .Bye("Go on. She's waiting.");
            d.Node("home", who, c => "Up top. The sky's still there. I didn't think it would be.").Bye("It is.");
            return d;
        }

        static Dialogue HapMossy()
        {
            var d = new Dialogue("hap_mossy", c => "start");
            d.Node("start", "Hap Mossy",
                c => c.G.World.Flag("bran_home")
                    ? "Bran went up, then. Good lad. If he can go up... maybe I'll see the sky one day. Is it still blue? It was blue."
                    : "Forty years on the deep shift. Came down a lad. I keep the cairns now, and I keep the names on them. Somebody ought to.",
                c => "He touches the nearest cairn as he talks, the way you'd touch a friend's shoulder.")
                .Say("What's beyond the vaults?", "beyond")
                .Bye("Look after yourself, Hap.");
            d.Node("beyond", "Hap Mossy",
                c => "Don't know. Never asked. The pipes come from down there, warm as a hand, and the Keepers stand in the way, and the vaults are iron all through. You don't build all that for nothing, do you? You don't build all that to keep folk out.")
                .Bye("No. You don't.");
            return d;
        }

        static Dialogue DeepShiftHand(string id, string name, string line)
        {
            var d = new Dialogue(id, c => "start");
            d.Node("start", name, c => line).Bye("Take care.");
            return d;
        }
    }
}
