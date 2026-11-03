using System.Linq;
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
                case "mags": return Mags();
                case "crale": return Crale();
                case "tilly": return Tilly();
                case "king_aldric": return King();
                case "oare": return Oare();
                case "marigold": return Marigold();
                case "marsh_capital": return MarshCapital();
                case "ashby_capital": return AshbyCapital();
                case "sorrel_capital": return SorrelCapital();
                case "calloway_capital": return CallowayCapital();
                case "glass": return Glass();
                case "dray": return Dray();
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

        // =================================================================== the Roots

        static Dialogue Mags()
        {
            var d = new Dialogue("mags", c => c.G.Quests.Stage(DeepShift.Id) >= DeepShift.Below ? "news" : c.G.Quests.Stage(DeepShift.Id) > 0 ? "waiting" : "intro");
            const string who = "Mags Tolley";
            d.Node("intro", who,
                c => "You're the ones from up top. With the dragon. Everybody's talking. Sorry, love, I don't mean to stare, I- you're important. You've got the King's ear. Could you ask about my Bran?",
                c => "She's wringing a sheet so hard the water's long gone out of it.")
                .Say("Who's Bran?", "bran")
                .Bye("Sorry. Another time.");
            d.Node("bran", who,
                c => "My husband. He works the deep shift at the Foundation Works, down past the Undergate. Triple pay, from the Crown. Two years now. His letters come up every week, regular as the bells, and they're his hand, I'd know it anywhere. But he never comes up. Not once. Not for our girl's naming day. Not when his mam died.",
                c => "She folds the sheet, very carefully, into a small square.")
                .Say("We'll ask Foreman Crale.", "yes", doIt: c => c.G.Quests.Start(DeepShift.Id, c.P))
                .Bye("I'm sorry. We can't promise anything.");
            d.Node("yes", who, c => "Would you? Crale's in the works yard, by the big gate. He's not a bad man. He's just scared of something. Everybody down here is, a little. Nobody says of what.").Bye("We'll find out.");
            d.Node("waiting", who, c => "Any word? ...No. That's all right. Next week, maybe. There's always a letter next week.").Bye("Not yet. We're still asking.");
            d.Node("news", who,
                c => "Alive. Down there. On the Crown's orders. ...Alive is more than I had this morning. Thank you. If you're going down there- no. You wouldn't. Nobody goes down there. Just... if you do.",
                c => $"{c.G.World.DragonName} lowers its huge head next to her, very gently, and she puts her hand on its nose and leaves it there a while.")
                .Bye("If we do, we'll look for him.");
            return d;
        }

        static Dialogue Crale()
        {
            var d = new Dialogue("crale", c => c.G.Quests.Stage(DeepShift.Id) >= DeepShift.Asked && c.G.Quests.Stage(DeepShift.Id) < DeepShift.Below ? "ask" : "start");
            const string who = "Foreman Crale";
            d.Node("start", who,
                c => c.G.Quests.Stage(DeepShift.Id) >= DeepShift.Below
                    ? "You again. I told you what I know. Now let me work, before somebody up top hears I've been talking."
                    : "Foundation Works. Dig, shore, haul, repeat. Mind the crates, mind the crane, mind the dragon off my scaffolds.")
                .Say("Open the Undergate for us.", "undergate", show: c => c.G.Quests.Stage(DeepShift.Id) >= DeepShift.Below && c.G.Quests.Stage(WarmCity.Id) == WarmCity.Below)
                .Bye("Carry on.");
            d.Node("undergate", who,
                c => "...You know, don't you. Not what's down there. Nobody knows that. But you know there's a down there.",
                c => "He takes a key from round his neck on a bootlace: black iron, heavy, warm from his skin. \"I've the key. I've always had the key. God help me, I've never once used it.\"")
                .Say("(Take the key and open the Undergate.)", doIt: c => c.G.Quests.Set(WarmCity.Id, WarmCity.Opened, c.P), end: true)
                .Bye("Not yet.");
            d.Node("ask", who,
                c => "Bran Tolley? Deep shift. Can't talk about the deep shift. Royal warrant. Get out of my yard.",
                c => "He doesn't look at the Undergate when he says it. He very carefully doesn't look at the Undergate.",
                c => c.G.Quests.Set(DeepShift.Id, DeepShift.Crale, c.P))
                .Roll(Skill.Insight, 14, "(Insight) Read him.", "insight_ok", "insight_bad")
                .Roll(Skill.Persuasion, 16, "(Persuasion) \"Her husband. Two years. Just tell her he's alive.\"", "told", "refused", onSuccess: c => c.G.Quests.Set(DeepShift.Id, DeepShift.Below, c.P))
                .Roll(Skill.Intimidation, 15, "(Intimidation) Lean on him.", "told", "men", onSuccess: c => c.G.Quests.Set(DeepShift.Id, DeepShift.Below, c.P),
                      onFailure: c => c.G.World.SetFlag("crale_men_called"))
                .Bye("We'll be back.");
            d.Node("insight_ok", who, c => "...What?",
                    c => "He isn't cruel. He's terrified. Of the gate, of the seal on it, of whoever signs the warrants. And under that, of something else: the thing he never lets himself think about.")
                .Roll(Skill.Persuasion, 16, "(Persuasion) \"Her husband. Two years. Just tell her he's alive.\"", "told", "refused", onSuccess: c => c.G.Quests.Set(DeepShift.Id, DeepShift.Below, c.P))
                .Bye("We'll be back.");
            d.Node("insight_bad", who, c => "Stop staring. I've got a works to run.", c => "Hard to read. Harder to like.").Bye("We'll be back.");
            d.Node("told", who,
                c => "...Fine. FINE. Crown orders, sealed. The deep shift goes down through the Undergate and stays down. Paid triple, paid to their families, paid regular. Letters come up in a sealed bag. I don't open it. I don't ask. Nobody who asked kept his job. He's alive. They're all alive, as far as I know. Now get out of my yard.",
                c => "He says 'as far as I know' like a man who has decided, very firmly, not to know any further.")
                .Bye("Thank you, Crale.");
            d.Node("refused", who, c => "No. Out. And don't come back without a warrant of your own.").Bye("We'll be back.");
            d.Node("men", who, c => "Right. LADS! These folk want to learn some manners!", c => "Three of the biggest men in the Roots put down their shovels.")
                .Say("(Fists up.)", doIt: c => { c.Screen.Close(); c.G.SpawnAndFight("crale_men", c.P); }, end: true);
            return d;
        }

        static Dialogue Tilly()
        {
            var d = new Dialogue("tilly", c => "start");
            d.Node("start", "Tilly Brightwick",
                c => "My sister does the lamps up top. Wenna. Thirty years and she's never had to light one! Down here we use oil, same as anybody. Funny old city. Nobody minds, so long as it's warm.")
                .Bye("Goodnight, Tilly.");
            return d;
        }

        // =================================================================== the palace, the records, the Wyvern

        static string SideLine(DCtx c)
        {
            var w = c.G.World;
            if (w.Flag("banner_crown")) return " Lady-Commander Marsh speaks well of you. That is rare.";
            if (w.Flag("banner_academy")) return " Corvane Ashby's friends, I'm told. He says you ask difficult questions. Good.";
            if (w.Flag("banner_fae")) return " You've been to the Courts. Mind their bargains. I've had to.";
            if (w.Flag("banner_hunters")) return " The Wyrmwatch's friends. You'll forgive my guards for counting your arrows.";
            return "";
        }

        static Dialogue King()
        {
            var d = new Dialogue("king_aldric", c => c.G.Quests.Stage(WarmCity.Id) >= WarmCity.Audience ? "again" : "audience");
            const string who = "King Aldric IX";
            d.Node("audience", who,
                c => "So you are the heroes of the Emberhorn. And that, outside on my stair, I'm told, is the dragon. Welcome to Aurelhold. You've done the realm a service it can't repay, so let me at least try." + SideLine(c),
                c => "He's younger than the coins make him look, and more tired. He comes down off the dais to shake your hands himself, which makes the courtiers twitch.")
                .Say("We came because you asked, Majesty. What do you need?", "ask")
                .Roll(Skill.Insight, 16, "(Insight) Watch him while you mention the Emberhorn.", "tell", "tell_fail", onSuccess: c => LoreDb.Find("the_kings_ring", c.P))
                .Roll(Skill.History, 15, "(History) The plain iron ring under his gold one...", "ring_history", "ring_fail");
            d.Node("tell", who, c => "...Yes. The Emberhorn. Remarkable. Quite remarkable.",
                    c => "He means every warm word. But each time you say 'Emberhorn', his thumb finds the plain iron ring under his gold one and turns it, round and round, and he's glad when the subject changes.")
                .Say("We came because you asked, Majesty. What do you need?", "ask");
            d.Node("tell_fail", who, c => "The Emberhorn. Yes. We heard the bells stop ringing grey all the way from here.", c => "A king, gracious and tired. Nothing more that you can see.")
                .Say("We came because you asked, Majesty. What do you need?", "ask");
            d.Node("ring_history", who, c => "This? An heirloom. Every King of Aurelhold wears one. Nobody remembers why. Tradition is like that.",
                    c => "It's true: every royal portrait you've passed has the same plain iron ring, under the gold, on the same finger.")
                .Say("We came because you asked, Majesty. What do you need?", "ask");
            d.Node("ring_fail", who, c => "Admiring my rings? I'd give you one, but the Treasurer would weep.").Say("We came because you asked, Majesty. What do you need?", "ask");
            d.Node("ask", who,
                c => $"Need? Nothing. Want: a great deal. In a month it will be a thousand years since the Fall. The Thousandth Pageant. I would like {c.G.World.DragonName} to lead it. A dragon who walks beside the Crown, a thousand years after the Fall. The people will never forget it.")
                .Say("We'd be honoured.", "close", doIt: c => c.G.World.SetFlag("pageant_yes"))
                .Say("We'll think about it.", "close")
                .Say("Why now? Why us?", "why");
            d.Node("why", who,
                c => "Because the realm went grey and the people are frightened, and frightened people need to see something hopeful walk down the middle of the street. And because you're the only ones who've stood in front of a dragon and lived. Two, actually. One of them likes you.")
                .Say("We'd be honoured.", "close", doIt: c => c.G.World.SetFlag("pageant_yes"))
                .Say("We'll think about it.", "close");
            d.Node("close", who,
                c => "Then rest. See my city. It's the only warm place left in the realm, and I'd like you to see why that matters. My Pageant-Master will find you. He finds everyone.",
                c => "Outside, on the Sunstair, the dragon is surrounded by a ring of very brave townsfolk and one very nervous guard.",
                c => c.G.Quests.Set(WarmCity.Id, WarmCity.Audience, c.P))
                .Bye("Thank you, Majesty.");
            d.Node("again", who, c => "Our heroes. Is the city treating you well? Good. Good. The Pageant draws closer every day.")
                .Bye("Majesty.");
            return d;
        }

        static Dialogue Oare()
        {
            var d = new Dialogue("oare", c => "start");
            const string who = "Archivist-Royal Delphine Oare";
            d.Node("start", who,
                c => "The Hall of Records. Nine hundred years of the Crown's business, every page accounted for. Do not lick your fingers to turn them. I can tell. I can always tell.")
                .Say("Every page?", "pages", show: c => !c.G.World.Flag("clue_records"))
                .Say("Year 2 of the Crown is missing. Cut out.", "year2", show: c => c.G.World.Flag("clue_records"))
                .Bye("We'll be careful.");
            d.Node("pages", who, c => "Every page that survived. Fire, flood, mice. Archives are a war against mice, mostly. Help yourselves. The Annals are on the west wall.").Bye("Thank you.");
            d.Node("year2", who, c => "Water damage. Long before my time. The binding was saved, the pages weren't. It happens.")
                .Roll(Skill.Insight, 13, "(Insight) Does she believe it?", "year2_true", "year2_unsure")
                .Bye("Of course.");
            d.Node("year2_true", who, c => "...Is there something else?", c => "She believes it completely. It's what she was told, and what her teacher was told. Nobody in this building has ever seen water damage that looked like a razor.").Bye("No. Thank you.");
            d.Node("year2_unsure", who, c => "Is there something else?").Bye("No. Thank you.");
            return d;
        }

        static Dialogue Marigold()
        {
            var d = new Dialogue("marigold", c => "start");
            const string who = "Marigold Penn";
            d.Node("start", who,
                c => "Welcome to the Wyvern, heroes! Best ale in Aurelhold and the only beds in the city that don't squeak. Your dragon's in my yard, eating my geraniums. I've decided to be honoured.")
                .Say("I'd like a room for the night. (5 sp, long rest)", doIt: c => c.G.RentRoom(c.P), end: true)
                .Say("Heard anything interesting?", "gossip")
                .Bye("Just looking.");
            d.Node("gossip", who,
                c => c.G.Clock.Hour >= 18
                    ? "Crale from the Foundation Works drinks alone at the end of the bar every night. Same stool. Never talks. Tips well. That's all I know and all I want to."
                    : "The Pageant-Master's been in three times today looking for a dragon. Yours'll do, I expect.")
                .Bye("Thanks, Marigold.");
            return d;
        }

        // =================================================================== the four leaders, in town for the Pageant

        static Dialogue MarshCapital()
        {
            var d = new Dialogue("marsh_capital", c => "start");
            const string who = "Lady-Commander Isaine Marsh";
            d.Node("start", who,
                c => (c.G.World.Flag("banner_crown") ? "There you are. Good. I asked for you to be on the guest list myself. " : "Heroes of the Emberhorn. Welcome to the capital. ")
                    + "The King called me up from the Redoubt to command his household guard for the Pageant. It's an honour. It's mostly polishing.",
                c => "She looks the same as at the Redoubt: straight-backed, tired, honest to the bone. The gold braid on her shoulder is new.")
                .Say("How do you like the capital?", "capital")
                .Bye("Good to see you, Commander.");
            d.Node("capital", who,
                c => "Warm. Clean. Proud. Everything the Redoubt isn't. One odd thing: I command the household guard, and there are doors in the palace even I'm not allowed through. 'Royal warrant.' I've never met a door I wasn't allowed through before.",
                c => "She says it lightly. She doesn't mean it lightly.")
                .Bye("We'll keep our eyes open.");
            return d;
        }

        static Dialogue AshbyCapital()
        {
            var d = new Dialogue("ashby_capital", c => "start");
            const string who = "High Magister Corvane Ashby";
            d.Node("start", who,
                c => (c.G.World.Flag("banner_academy") ? "My friends! Come, come, look at this. " : "Ah. You. Well, you may as well see it too. ")
                    + "I came for the Pageant and stayed for the Annals. Nine hundred volumes. Do you know, the Crown's own history has the same hole in it as ours?",
                c => "He's surrounded by open books, and he hasn't slept.")
                .Say("Year 2. Cut out.", "year2", show: c => c.G.World.Flag("clue_records"))
                .Say("What hole?", "hole", show: c => !c.G.World.Flag("clue_records"))
                .Bye("Get some sleep, Magister.");
            d.Node("hole", who, c => "Look for yourselves: the west wall. I won't spoil it. I'd like someone else to tell me I'm not imagining it.").Bye("We'll look.");
            d.Node("year2", who,
                c => "Year 2. And our Annals were 'corrected' for the very same years. I always assumed the instruction came from my predecessor. I never asked where HE had it from. A High Magister who never asked a question. Imagine.",
                c => "It's the first time you've seen him look frightened of something other than a dragon.")
                .Bye("We'll find out.");
            return d;
        }

        static Dialogue SorrelCapital()
        {
            var d = new Dialogue("sorrel_capital", c => "start");
            const string who = "Herald Sorrel";
            d.Node("start", who,
                c => (c.G.World.Flag("banner_fae") ? "Our friends from the Glade! Have a flower. Have several. " : "The dragon's people. How nice. ")
                    + "The Courts sent me to the Pageant. A thousand years, they're calling it. A thousand years since the Fall. In the Courts we count it differently. We say a thousand years since the Pact was broken.",
                c => "She tucks a flower behind the dragon's ear-frill. It allows this.")
                .Say("Broken by whom?", "who")
                .Bye("Enjoy the flowers, Herald.");
            d.Node("who", who,
                c => (c.G.World.Flag("fae_clause") ? $"Ah, ah. That's a question with a price, and {c.G.World.DragonName} already owes us one favour. The stars are nearly ready to ask it. " : "Ah, ah. That's a question with a price. ")
                    + "Ask the city instead. It's very proud of its lamps.")
                .Bye("We'll ask.");
            return d;
        }

        static Dialogue CallowayCapital()
        {
            var d = new Dialogue("calloway_capital", c => "start");
            const string who = "Huntmaster Bryn Calloway";
            d.Node("start", who,
                c => (c.G.World.Flag("banner_hunters") ? "Sit, sit! Drink with the Wyrmwatch. " : "Hm. The dragon-keepers. Sit if you like. ")
                    + "A thousand years since the Fall, and every hunter in the realm's in this taproom to drink to it. The Wyrmwatch was founded the same year, did you know? The very same. By royal charter.",
                c => c.G.World.Flag("wyrmwatch_belled") ? $"Her eyes go to {c.G.World.DragonName}'s bell, out in the yard. She nods, satisfied." : "Her eyes go to the yard where the dragon is, and stay there.")
                .Say("By royal charter?", "charter")
                .Bye("Another time, Huntmaster.");
            d.Node("charter", who,
                c => "Signed by the first King himself. 'To watch for fire.' The Crown's paid our bounties for a thousand years. Generous. Never late. Never once asked what we found.")
                .Bye("Interesting.");
            return d;
        }

        // =================================================================== the Pageant rehearsal

        static int ParadeBeats(DCtx c) => new[] { "parade_beat_1", "parade_beat_2", "parade_beat_3" }.Count(c.G.World.Flag);

        static Dialogue Glass()
        {
            var d = new Dialogue("glass", c =>
            {
                int st = c.G.Quests.Stage(DragonParade.Id);
                if (st >= DragonParade.Done) return "after";
                if (st == 0) return "intro";
                if (c.G.World.IsLocked("parade_retry")) return "tomorrow";
                return c.G.Clock.Hour >= 9 && c.G.Clock.Hour < 18 ? "ready" : "evening";
            });
            const string who = "Pageant-Master Fennimore Glass";
            d.Node("intro", who,
                c => $"IT'S YOU! It's THEM! It's the DRAGON! Fennimore Glass, Pageant-Master to the Crown, eleven years, never a cancelled show! {c.G.World.DragonName} must lead the Thousandth Pageant. " + (c.G.World.Flag("pageant_yes") ? "The King says you said yes! " : "The King says you might say yes! ") + "Rehearsal. Here. Today. Please.",
                c => "Behind him, forty performers, a paper Queen three storeys high with flames of red silk, and a band that has stopped playing to listen.")
                .Say("All right. Let's rehearse.", "ready", doIt: c => c.G.Quests.Start(DragonParade.Id, c.P))
                .Bye("Maybe later.");
            d.Node("ready", who, c => "Places! PLACES! The dragon walks at the head of the parade, you walk beside it, and when the paper Queen comes round the fountain- well. You'll see. From the top!")
                .Say("(Begin the rehearsal.)", "beat1", doIt: c => c.G.Quests.Set(DragonParade.Id, DragonParade.Rehearsal, c.P))
                .Bye("Not yet.");
            d.Node("beat1", who, c => "The march! Head high! Step, step, wave, step!",
                    c => "Two hundred people are watching. The band strikes up. You have to lead a dragon down the middle of the Great Square like you do this every day.")
                .Roll(Skill.Performance, 14, "(Performance) Lead the march.", "beat1_ok", "beat1_bad", onSuccess: c => c.G.World.SetFlag("parade_beat_1"));
            d.Node("beat1_ok", who, c => "MAGNIFICENT! Look at them! LOOK AT THEM!", c => "The crowd cheers. A few people throw flowers. One throws a bun.").Say("(Next.)", "beat2");
            d.Node("beat1_bad", who, c => "...Less waving. Much less waving. Onward!", c => "You trip over your own feet, then over the dragon's. The crowd is very kind about it.").Say("(Next.)", "beat2");
            d.Node("beat2", who, c => "And HERE comes the Queen!",
                    c => $"The paper Queen sweeps round the fountain on poles, silk flames streaming. {c.G.World.DragonName} sees it, goes very still, and opens its jaws.")
                .Roll(Skill.AnimalHandling, 14, $"(Animal Handling) Keep the dragon from eating the scenery.", "beat2_ok", "beat2_bad", onSuccess: c => c.G.World.SetFlag("parade_beat_2"));
            d.Node("beat2_ok", who, c => "It's PERFORMING! It's ACTING! It's a natural!", c => $"You put a hand on its neck and talk low, and {c.G.World.DragonName} closes its jaws and just watches the paper Queen go by, very seriously, all the way round.").Say("(Next.)", "beat3");
            d.Node("beat2_bad", who, c => "Not the- oh. Oh, the left wing. We have another left wing. Somewhere.", c => $"{c.G.World.DragonName} takes one careful bite of silk flame, decides it tastes of nothing, and spits it out on a tuba.").Say("(Next.)", "beat3");
            d.Node("beat3", who, c => "And for the FINALE-",
                    c => "Something creaks. The painted backdrop of the Wyrmback, forty feet of canvas and timber, begins to lean, slowly, towards the crowd.")
                .Roll(Skill.Acrobatics, 13, "(Acrobatics) Get under it and shove.", "beat3_ok", "beat3_bad", onSuccess: c => c.G.World.SetFlag("parade_beat_3"))
                .Roll(Skill.Persuasion, 13, "(Persuasion) Get the crowd to grab the ropes.", "beat3_ok", "beat3_bad", onSuccess: c => c.G.World.SetFlag("parade_beat_3"));
            d.Node("beat3_ok", who, c => "...THAT WAS IN THE SCRIPT. Everyone, that was IN THE SCRIPT.", c => "The backdrop rights itself to a roar from the crowd.").Say("(Take a bow.)", "result");
            d.Node("beat3_bad", who, c => "...Stagehands! STAGEHANDS!", c => "The backdrop comes down, gently, mostly on the band.").Say("(Take a bow.)", "result");
            d.Node("result", who,
                c => ParadeBeats(c) >= 2
                    ? "A TRIUMPH! A triumph. Mostly. Here, take this, wear it on the day, wear it every day. The city will be talking about this for another thousand years."
                    : "...We'll call that a dress rehearsal. Every great show has a terrible dress rehearsal. Same time tomorrow?",
                null,
                c =>
                {
                    if (ParadeBeats(c) >= 2) c.G.Quests.Set(DragonParade.Id, DragonParade.Done, c.P);
                    else
                    {
                        foreach (var f in new[] { "parade_beat_1", "parade_beat_2", "parade_beat_3" }) c.G.World.ClearFlag(f);
                        c.G.World.Lock("parade_retry", 18 * 60);
                    }
                })
                .Bye("Thank you, Pageant-Master.");
            d.Node("tomorrow", who, c => "Tomorrow! Tomorrow we'll be PERFECT.").Bye("Tomorrow.");
            d.Node("evening", who, c => "Rehearsals are by daylight! Come back in the morning! Bring the dragon! Bring the dragon's enthusiasm!").Bye("In the morning.");
            d.Node("after", who, c => "My stars! My dragon! The Pageant will be the greatest in a thousand years. Literally! It's the thousandth!").Bye("We'll be there.");
            return d;
        }

        static Dialogue Dray()
        {
            var d = new Dialogue("dray", c => "start");
            const string who = "Corwin Dray";
            d.Node("start", who,
                c => "Remedies, tonics, draughts and cures! The best apothecary in Aurelhold, which makes it the best in the realm. And the dearest. You pay for warm.")
                .Say("Show me what you have.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "capital_market"); }, end: true)
                .Bye("Just browsing.");
            return d;
        }
    }
}
