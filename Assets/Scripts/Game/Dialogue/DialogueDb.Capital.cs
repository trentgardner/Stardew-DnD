using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 3: the people of Aurelhold. The city is warm, proud and sincere; the Crown's people believe the legend.
    /// Suspicion of the Crown may grow here. Nothing here may hint at THE SECRET (LoreTests scans this file).
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue CapitalDialogue(string id)
        {
            switch (id)
            {
                case "quell": return Quell();
                case "wenna": return Wenna();
                case "hesketh": return Hesketh();
                case "merriwether": return Merriwether();
                case "capital_guard": return CapitalGuard();
                default: return null;
            }
        }

        // =================================================================== the King's carriage

        static Dialogue Quell()
        {
            var d = new Dialogue("quell", c => In(c, "capital") ? "capital" : c.G.World.Flag("met_quell") ? "town" : "intro");
            const string who = "Royal Courier Bertram Quell";
            d.Node("intro", who,
                c => $"The heroes of the Emberhorn! Bertram Quell, Royal Courier, at your service, and this is the King's own carriage. And that is... {c.G.World.DragonName}. Of course it is. The King did say 'and the dragon'. I had hoped he meant a small one.",
                c => $"The carriage is red and gold and has clearly been built for six people and no dragons. {c.G.World.DragonName} sniffs the horses. The horses pretend this isn't happening.",
                c => c.G.World.SetFlag("met_quell"))
                .Say("Take us to the capital.", "ride")
                .Say("What does the King want with us?", "why")
                .Bye("Not yet. Wait for us.");
            d.Node("why", who,
                c => "To thank you, I should think! The Emberhorn's quiet, the ash is thinning, and half the realm says it was you. And there's the Pageant, of course: a thousand years since the Fall. Everyone who's anyone will be in Aurelhold. Two days up the King's Road. The passes are snowed in, but the horses know the way.")
                .Say("Take us to the capital.", "ride")
                .Bye("Not yet. Wait for us.");
            d.Node("town", who, c => "Ready when you are. The horses are not, but they never are.")
                .Say("Take us to the capital.", "ride")
                .Bye("Not yet.");
            d.Node("ride", who, c => "Splendid! Everybody in. The dragon... will walk alongside. Yes. Good. Hup!")
                .Say("(Ride to Aurelhold.)", doIt: c => c.G.RideCarriage(c.P, "capital", WorldDb.CapitalCarriageStop), end: true);
            d.Node("capital", who,
                c => c.G.Clock.IsNight ? "Evening! The horses are asleep. I could wake them. They'd hold it against me." : "Back down to Thistledown? Two days on the King's Road. I'll have you there and back whenever you like. It's what the King pays me for, and I like the view.")
                .Say("Take us home to Thistledown.", "ride_home")
                .Bye("We'll stay a while.");
            d.Node("ride_home", who, c => "Home it is. Mind the step. Mind the dragon.")
                .Say("(Ride to Thistledown.)", doIt: c => c.G.RideCarriage(c.P, "town", WorldDb.TownCarriageStop), end: true);
            return d;
        }

        // =================================================================== the Great Square

        static Dialogue Wenna()
        {
            var d = new Dialogue("wenna", c => c.G.World.Flag("met_wenna") ? "start" : "intro");
            const string who = "Wenna Brightwick";
            d.Node("intro", who,
                c => "Mind the lamp, love, it's warm. Wenna Brightwick, Lamplighter of the Great Square. Thirty years in the job. Do you want to know a secret? I've never lit a single one. They're always lit. I polish them, mostly. And I count them. Forty-four, all burning, every night of my life.",
                c => $"She lets {c.G.World.DragonName} sniff the lamp-post. It sniffs for a long time, then sits back and looks at the ground.",
                c => { c.G.World.SetFlag("met_wenna"); c.G.World.AddFriendship("wenna", 30); })
                .Say("What keeps them burning?", "burning")
                .Say("The rest of the realm went grey. Why not here?", "warm")
                .Bye("Good to meet you, Wenna.");
            d.Node("burning", who,
                c => "The King's blessing, they say. Every King since the first. My gran said the lamps came with the Crown, and the Crown came with the lamps, and that's all anyone needs to know. I used to put my hand on the posts on winter nights. Warm as a hearth. Still are.")
                .Say("The rest of the realm went grey. Why not here?", "warm")
                .Bye("Thanks, Wenna.");
            d.Node("warm", who,
                c => "Did it? We heard. Grey fields, cold hearths, folk walking up the King's Road with their whole lives on a cart. We took them in, as many as we could. It's never cold in Aurelhold. Never once, in a thousand years. Makes you proud. Mostly.")
                .Roll(Skill.Insight, 13, "(Insight) \"Mostly?\"", "warm_insight", "warm_fail")
                .Bye("Thanks, Wenna.");
            d.Node("warm_insight", who,
                c => "...Mostly. Some winters I wonder what it costs, that's all. Nothing's free. Not even warmth. Don't tell the Pageant-Master I said so, he'd have me off the float.")
                .Bye("Your secret's safe.");
            d.Node("warm_fail", who, c => "Proud, I said! Proud as anything. Mind the lamp.").Bye("Thanks, Wenna.");
            d.Node("start", who,
                c => c.G.Clock.Hour < 9 ? "Morning. Forty-four, all burning. Same as ever." : "Polishing. Always polishing. They don't need it, but I'd go spare doing nothing.")
                .Say("What keeps them burning?", "burning")
                .Bye("See you, Wenna.");
            return d;
        }

        static Dialogue Hesketh()
        {
            var d = new Dialogue("hesketh", c => "start");
            const string who = "Hesketh";
            d.Node("start", who,
                c => c.G.World.Flag("dragon_capital_intro")
                    ? $"You're the ones with the dragon! It picked up every bun I dropped. Every one. Didn't eat a single one. Here: on the house. And one for {c.G.World.DragonName}. It's earned it."
                    : "Buns! Hot buns! Pageant buns, with a little sugar crown on top!",
                c => "The buns are still warm. Everything in Aurelhold is.")
                .Bye("Thank you!");
            return d;
        }

        static Dialogue Merriwether()
        {
            var d = new Dialogue("merriwether", c => "start");
            const string who = "Bandmaster Merriwether";
            d.Node("start", who,
                c => "The Pageant March! A thousand years! Forty players, thirty-nine of whom can read music! From the top, ladies and gentlemen: and ONE, and TWO-",
                c => "The band strikes up. It's loud, brave and roughly in tune. Somewhere in the brass section someone is playing a different march entirely, with total conviction.")
                .Roll(Skill.Performance, 14, "(Performance) Join in.", "join_ok", "join_bad")
                .Bye("Lovely. Keep at it.");
            d.Node("join_ok", who, c => "YES! THAT! Did everybody hear that? THAT is the Pageant March! You, friend, are welcome in my band any day of the year.",
                    c => "Even the trumpet who was playing the other march switches over.")
                .Bye("Any time, Bandmaster.");
            d.Node("join_bad", who, c => "...Interesting. Bold. Very bold. We'll... take it from the top. Again.").Bye("Sorry.");
            return d;
        }

        static Dialogue CapitalGuard()
        {
            var d = new Dialogue("capital_guard", c => "start");
            d.Node("start", "Crown guard",
                c => c.G.Clock.IsNight
                    ? "Quiet night. They're always quiet here. Nobody leaves Aurelhold in winter, and nobody comes but the King's guests."
                    : $"Welcome to Aurelhold, honoured guests. Please keep the dragon to the main streets. Please don't let it sit on the market. Please- it's very large, isn't it.")
                .Bye("Carry on.");
            return d;
        }
    }
}
