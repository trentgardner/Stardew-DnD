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
                case "bran": return Bran();
                case "hap_mossy": return HapMossy();
                case "deep_shift_1": return DeepShiftHand("deep_shift_1", "Lew Carrow",
                    "Scrape, oil, scrape, oil. The pipes like it gentle. Don't ask me how I know. You just know, after a while, the way you know a horse.");
                case "deep_shift_2": return DeepShiftHand("deep_shift_2", "Dobbin Hale",
                    "Up top? Haven't seen it in six years. Is the King's Square still gold? I used to sell chestnuts there. Funny what you miss.");
                default: return null;
            }
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
