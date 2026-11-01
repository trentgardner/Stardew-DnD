using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 3: the Lantern Road and the Starfall Academy. Every force is right about something; Ashby is half right
    /// and frightened, never a villain. THE SECRET never leaks here (LoreTests scans every line that names the Queen).
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue AcademyDialogue(string id)
        {
            switch (id)
            {
                case "holloway": return Holloway();
                case "crown_guard": return CrownGuard();
                case "juniper": return Juniper();
                case "pell": return Pell();
                case "oakes": return Oakes();
                case "varrow": return Varrow();
                case "thorne": return Thorne();
                case "ashby": return Ashby();
                default: return null;
            }
        }

        /// <summary>The player is talking to an NPC who's standing in this zone right now.</summary>
        static bool In(DCtx c, string zone) => c.Npc != null && c.Npc.Actor != null && c.Npc.Actor.Zone != null && c.Npc.Actor.Zone.Def.Id == zone;

        // =================================================================== Perrin, a familiar face (Act II)

        static void PerrinAtTheAcademy(Dialogue d)
        {
            const string who = "Perrin Quillby";
            var inner = d.Start;
            d.Start = c => c.G.World.Act >= 2 && In(c, "academy") ? (c.G.World.Flag("perrin_a2") ? "a2_start" : "a2_intro") : inner(c);
            d.Node("a2_intro", who,
                c => "YOU! You came! Oh, this is wonderful, this is- is that- it GREW. It's enormous. May I- no. No, I may not, it's making the face. Welcome to the Starfall Academy! Isn't it magnificent? Don't touch the floating stones, they bite. They don't bite. They do hum, though.",
                c => "Perrin has new robes that fit. He's very proud of them. There's already ink on the sleeve.",
                c => c.G.World.SetFlag("perrin_a2"))
                .Say("We're looking for High Magister Ashby.", "a2_ashby")
                .Say("What is this place, really?", "a2_place");
            d.Node("a2_ashby", who,
                c => "Of course you are. Everyone is, lately. He's at the top of the Orrery Tower, north-east, the gold one. He's wonderful. He's terrifying. He's both, mostly before breakfast. Be polite, be brief, and don't mention the stairs: he's sensitive about the stairs.")
                .Say("What is this place, really?", "a2_place")
                .Bye("Thanks, Perrin.");
            d.Node("a2_place", who,
                c => "The greatest house of learning in the realm. Eight hundred years of it! The wards keep out the weather, the ash and the second-years. The Library of Quiet Stars has books that read YOU. And the bells... we don't talk about the bells.",
                c => "He says it cheerfully. Then he glances at the bell tower, and stops being cheerful for a moment.")
                .Say("The bells?", "a2_bells")
                .Bye("Thanks, Perrin.");
            d.Node("a2_bells", who, c => "There are twelve. Everyone knows there are twelve. Please stop asking me about the thirteenth, it gives me the shivers.")
                .Bye("We'll stop asking.");
            d.Node("a2_start", who,
                c => c.G.Clock.Hour >= 18 ? "Supper's terrible, the company's excellent. Sit! Tell me everything. Leave out the parts with blood." : "Have you seen the orrery up close? I've been explaining it to the first-years. I've been explaining it wrong, but with ENTHUSIASM.")
                .Say("Where's Ashby again?", "a2_ashby")
                .Bye("See you, Perrin.");
        }

        // =================================================================== The Academy's people

        static Dialogue CrownGuard()
        {
            var d = new Dialogue("crown_guard", c => "start");
            d.Node("start", "Crown soldier",
                c => c.G.Clock.Hour >= 20 || c.G.Clock.Hour < 6
                    ? "Quiet night. They're all quiet nights here, except for the bells. You get used to the bells. You don't get used to the thirteenth."
                    : "Garrison duty. We guard the scholars. The scholars guard the wards. The wards guard everything else. Nobody tells me what the wards guard.")
                .Bye("Carry on.");
            return d;
        }

        static Dialogue Juniper()
        {
            var d = new Dialogue("juniper", c => c.G.World.Flag("met_juniper") ? "start" : "intro");
            const string who = "Juniper";
            d.Node("intro", who,
                c => $"Is that a DRAGON? An actual one? Can I- may I- I'm not going to touch, I just want to look. Hello. Hello, you. Oh, it's looking at me. {c.G.World.DragonName}? That's a perfect name. I'm Juniper. Second year. Illusions, mostly, and drawing.",
                c => $"She holds out the back of her hand, the way you would to a strange dog, and waits. {c.G.World.DragonName} sniffs it, considers, and leans in for a scratch.",
                c => { c.G.World.SetFlag("met_juniper"); c.G.World.AddFriendship("juniper", 50); })
                .Say("You're good with it.", "good")
                .Say("Anything strange around here lately?", "strange");
            d.Node("good", who, c => "My gran kept geese. If you can win over a goose, you can win over anything. Geese are basically dragons with worse manners.")
                .Say("Anything strange around here lately?", "strange")
                .Bye("Nice to meet you, Juniper.");
            d.Node("strange", who,
                c => "Strange? It's the Academy. Last week a staircase went on holiday. But... the professors have been going down under the library a lot. At night. With crates. And nobody will say why. Pell says it's a ballroom. Pell is an idiot.")
                .Bye("Thanks, Juniper.");
            d.Node("start", who,
                c => c.G.Clock.Hour >= 17 ? $"Did {c.G.World.DragonName} eat? I saved a biscuit. It's a bit squashed. Dragons don't mind squashed." : "I'm drawing the orrery. It keeps moving. That's the whole problem with the orrery.")
                .Say("Anything strange around here lately?", "strange")
                .Bye("See you, Juniper.");
            return d;
        }

        static Dialogue Pell()
        {
            var d = new Dialogue("pell", c => "start");
            const string who = "Pell";
            bool wantsBook(DCtx c) => c.G.Quests.Stage("overdue") == 1 && !c.G.World.Flag("book_pell");
            d.Node("start", who,
                c => c.G.Clock.Hour < 1
                    ? "Shh! Shh. Listen. Any second now. Twelve, and then... You'll see. You'll hear. Whatever."
                    : "Lectures are optional. Philosophically. If you're from the Archivist, I've never heard of her.")
                .Say("Thorne wants her book back.", "book", show: wantsBook)
                .Bye("Carry on, Pell.");
            d.Node("book", who, c => "What book? I don't have a book. I've never seen a book. ...It's a really good book, though. 'Ninety Nights Under the Wyrmback.' I'm on night sixty.")
                .Roll(Skill.Persuasion, 13, "You can finish it properly once it's back. Thorne will let you, if you ask nicely.", "gives", "no")
                .Roll(Skill.Intimidation, 12, "Book. Now.", "gives", "no")
                .Roll(Skill.Deception, 14, "Thorne's coming across the lawn right now.", "gives", "no")
                .Say("I'll trade you a star shard for it.", "gives", doIt: c => c.G.TakeQuestItem(c.P, "star_shard"), show: c => c.P.Character.CountOf("star_shard") > 0)
                .Say("Here: ten gold for your trouble.", "gives", doIt: c => c.P.Character.Copper -= 1000, show: c => c.P.Character.Copper >= 1000)
                .Bye("Never mind.");
            d.Node("gives", who,
                c => "Fine. FINE. Here. Night sixty, she'll know where I got to, she always knows. ...Listen. Since you're here. Have you heard the bells at midnight? Twelve, and then one more. Everyone says I'm making it up. Come to the bell green at midnight. You'll see.",
                c => "He hands over a dog-eared book with a pressed fern for a bookmark.",
                c => { if (!c.G.World.Flag("book_pell")) { c.G.GiveItem(c.P, "overdue_book"); c.G.World.SetFlag("book_pell"); } })
                .Bye("Thanks, Pell.");
            d.Node("no", who, c => "Nope. Not today. Today I am a wall.", c => "He folds his arms and becomes, as promised, a wall.").Bye("We'll be back.");
            return d;
        }

        static Dialogue Oakes()
        {
            var d = new Dialogue("oakes", c => c.G.Quests.Stage("glasshouse") == 3 ? "done" : c.G.World.Flag("met_oakes") ? "start" : "intro");
            const string who = "Professor Bramwell Oakes";
            d.Node("intro", who,
                c => "Mind the lilies! Mind them, mind them, there. Thank you. Bramwell Oakes, Master of the Glasshouse. Forty years I've grown ember-lilies, and they've never once looked like this.",
                c => "A round, earthy man with soil under every fingernail and a magnifying glass on a chain. He cradles a grey-edged lily like a sick kitten.",
                c => c.G.World.SetFlag("met_oakes"))
                .Say("Can we help?", "ask", show: c => c.G.Quests.Stage("glasshouse") == 0)
                .Bye("I hope they recover.");
            d.Node("start", who, c => c.G.Quests.Stage("glasshouse") == 2 ? "Still? Still. The vines are quiet, bless you, but it's at the root, I know it is. Look under the beds." : "Grey at the edges. Grey! As if the fire in them is going out.")
                .Say("Can we help?", "ask", show: c => c.G.Quests.Stage("glasshouse") == 0)
                .Bye("Good luck, Professor.");
            d.Node("ask", who,
                c => "Would you? The far beds have... woken up. The vines. They've gone grey and they move, and they don't like me any more. I don't want them hurt if it can be helped. They're frightened, I think. Plants can be frightened. Nobody believes me.",
                onEnter: c => c.G.Quests.Start("glasshouse", c.P))
                .Bye("We'll be gentle if we can.");
            d.Node("done", who,
                c => "The heat-stone. The HEAT-STONE. A hundred years it's warmed this place, and now... grey fire. I'll order a new one, from the coast, far from the mountains. Here, take these. If the lilies can't be warm, at least you can.",
                c => "He presses two warm, glowing petals into your hands, and then, after a moment's thought, hugs you. He smells of compost and kindness.",
                c => { c.G.Quests.Set("glasshouse", 4, c.P); c.G.GiveItem(c.P, "ember_lily"); })
                .Bye("Thank you, Professor.");
            return d;
        }

        // =================================================================== High Magister Corvane Ashby
        // Brilliant, beloved, frightened and half right. Never a villain. He says "the Queen" and "the tyrant", nothing more.

        /// <summary>Hearing him out, whichever way it goes, moves "Answers" on (the FlagSet trigger).</summary>
        static void Heard(DCtx c) { c.G.World.SetFlag("ashby_heard"); Factions.Hear("academy"); }

        static Dialogue Ashby()
        {
            var d = new Dialogue("ashby", c => c.G.World.Flag("ashby_heard") ? "again" : c.G.World.Flag("met_ashby") ? "plan" : "intro");
            const string who = "High Magister Corvane Ashby";
            d.Node("intro", who,
                c => "Come in, come in. You'll have counted the stairs. Everyone counts the stairs. Sit. Tea? It pours itself. It's the only spell I've ever been vain about.",
                c => $"A tall, thin man with grey at his temples and ink on his cuffs, kinder-looking than you expected. The teapot rises, pours, and settles. He doesn't look at it. He looks at {c.G.World.DragonName}, for a long moment, with an expression you can't read.",
                c => c.G.World.SetFlag("met_ashby"))
                .Say("The hearths are going cold. We came for answers.", "legend");
            d.Node("legend", who,
                c => "Then you'll have heard the story. Everyone has. Long ago the Ashen Queen drank the fire of the world, and the Great Ones lay down upon her, and the mountains were made. Every first-year here can recite it. I could recite it before I could read.")
                .Say("And now?", "waking");
            d.Node("waking", who,
                c => "Now they're waking. Your star. Your egg. One by one the Great Ones stir in their sleep, and every time one stirs, the hold loosens a little. The cold hearths, the grey ash, the beasts with coals for eyes: that's her, breathing through the cracks.",
                c => "He says it simply, the way a doctor gives bad news.")
                .Say("What do you mean to do about it?", "plan");
            d.Node("plan", who,
                c => "Understand the waking dragons. Then seal them, gently and forever, before she wakes with them. I don't hate dragons. I've loved them my whole life. That is exactly why I know what one can do.",
                c => $"His eyes go back to {c.G.World.DragonName}. \"I'd like to study yours, one day. Not now. Not without your leave. But think on it.\"")
                .Roll(Skill.Insight, 14, "(Insight) Read him.", "plan_insight", "plan_insight_no")
                .Roll(Skill.Arcana, 15, "(Arcana) A sealing on that scale... how would it even hold?", "plan_arcana", "plan_arcana_no",
                    onSuccess: c => Factions.Adjust("academy", 5, c.P))
                .Roll(Skill.Persuasion, 16, "We've seen the Sealing Hall. Tell us about it, to our faces.", "hall_yes", "hall_no",
                    onSuccess: c => { Factions.Adjust("academy", 5, c.P); c.G.World.SetFlag("ashby_hall_confronted"); },
                    onFailure: c => c.G.World.SetFlag("ashby_hall_confronted"),
                    show: c => c.G.World.Flag("seen:sealing_hall"))
                .Say("You could be right.", "agree", doIt: c => { Factions.Adjust("academy", 10, c.P); Heard(c); })
                .Say("You're talking about a cage.", "cage", doIt: c => { Factions.Adjust("academy", -5, c.P); Heard(c); })
                .Say("Keep away from our dragon.", "keep_away", doIt: c => { Factions.Adjust("academy", -5, c.P); DragonPet.AddBond(c.P, 2, "standing up for it"); Heard(c); });
            d.Node("plan_insight", who, c => "...",
                c => "He hasn't slept properly in a long time. There are ink stains on both hands, as if he writes until one gives out and then switches. And he's frightened. Not of you.")
                .Say("(Back)", "plan");
            d.Node("plan_insight_no", who, c => "...", c => "A scholar's face: patient, warm, giving nothing away.").Say("(Back)", "plan");
            d.Node("plan_arcana", who,
                c => "Oh, you KNOW the theory. Nested circles, yes, twelve anchors, the binding drawn from the bound creature's own fire so it can never burn its way out... You've read Velloran. No one reads Velloran. Come back any time. I mean that.",
                c => "For a moment he forgets to be High Magister and is simply delighted.")
                .Say("(Back)", "plan");
            d.Node("plan_arcana_no", who, c => "It's complicated. It's VERY complicated. I'll lend you a book.", c => "He doesn't lend you a book.").Say("(Back)", "plan");
            d.Node("hall_yes", who,
                c => "...Yes. You have. Good. I'd rather you asked me than whispered about me. It isn't finished. It may never be. It's the most careful thing I have ever built, and I pray every night I never have to use it.",
                c => "He is quiet a long time. When he speaks again his voice is lower. \"If you think I'm wrong, tell me. Someone should.\"")
                .Say("(Back)", "plan");
            d.Node("hall_no", who, c => "The Academy has many rooms. Some are not mine to show. Some are not yours to see.", c => "The warmth goes out of the room, just for a moment, like a cloud over the sun.")
                .Say("(Back)", "plan");
            d.Node("agree", who,
                c => "Thank you. You don't know how rare it is, to be heard and not humoured. Go carefully. Come back. We'll speak again.",
                c => $"{c.G.World.DragonName} watches him the whole time, and doesn't blink.")
                .Bye("We'll come back.");
            d.Node("cage", who,
                c => "Yes. I am. A kind one, I hope. Would you rather the alternative? I would truly like to know. I've had no better idea in thirty years, and I have been looking.",
                c => "He doesn't argue. He just looks tired.")
                .Bye("We'll find another way.");
            d.Node("keep_away", who,
                c => "Of course. Of course. I meant no harm. I never do. That has never once been the problem.",
                c => $"{c.G.World.DragonName} presses against your legs. You can feel it trembling, or growling, or both.")
                .Bye("Good day, High Magister.");
            d.Node("again", who,
                c => c.G.Clock.Hour >= 22 || c.G.Clock.Hour < 8
                    ? "Forgive me. I was watching the mountains. They look the same as ever, don't they? That's the frightening part."
                    : "Ah. You came back. More tea? It's still vain.")
                .Bye("Goodbye, High Magister.");
            return d;
        }

        static Dialogue Thorne()
        {
            var d = new Dialogue("thorne", c => c.G.Quests.Stage("overdue") == 2 ? "returned" : c.G.World.Flag("met_thorne") ? "start" : "intro");
            const string who = "Archivist Mirelle Thorne";
            d.Node("intro", who,
                c => "Visitors. Wonderful. Lower your voices: the books are listening, and some of them are rude. Mirelle Thorne, Archivist. If you've come to borrow, you'll need a reason. If you've come to return, you'll need a better one, because I KNOW how late it is.",
                c => "A narrow, upright woman with half-moon spectacles and a ring of keys that never jingles. Every stamp she makes on a returns card sounds like a verdict.",
                c => c.G.World.SetFlag("met_thorne"))
                .Say("What's beyond the long corridor?", "corridor")
                .Bye("We'll be quiet.");
            d.Node("corridor", who,
                c => "The Restricted Section. Restricted, as in: not for you. The corridor makes sure of that. It's a very old joke and it still works.")
                .Roll(Skill.Insight, 14, "(Insight) She's hiding something. What?", "corridor_insight", "corridor_insight_no")
                .Say("How would someone earn a pass?", "earn", show: c => c.G.Quests.Stage("overdue") == 0)
                .Bye("Understood.");
            d.Node("corridor_insight", who, c => "...",
                c => "Her eyes flick to the floor, as if she can see through it. Whatever is below the Restricted Section, she knows about it, and she hates it, and she has been told not to say so.")
                .Say("(Back)", "corridor");
            d.Node("corridor_insight_no", who, c => "...", c => "She stamps a card. The verdict is: none of your business.").Say("(Back)", "corridor");
            d.Node("earn", who,
                c => "Earn? Hm. Three books are missing from my shelves, very overdue. The geese by the pond are sitting on one. Pell 'borrowed' another, and Pell thinks I don't know. The third is somewhere in the hedge maze, where it has been for eleven years. Bring them back and I'll stamp you a pass. And I'll walk you through the corridor myself.",
                onEnter: c => c.G.Quests.Start("overdue", c.P))
                .Bye("Three books. On it.");
            d.Node("returned", who,
                c => "Eleven years. ELEVEN. And this one has goose on it. ...Thank you. I mean that. Here: your pass. Show it to the armours, they'll know my seal. And come, I'll walk you through the stacks. Mind you don't look at the gaps.",
                c => "She stamps a small brass card so hard the desk jumps, then takes you down the endless corridor, which, with her beside you, ends quite normally at a warded door.",
                c =>
                {
                    for (int i = 0; i < 3; i++) c.G.TakeQuestItem(c.P, "overdue_book");
                    foreach (var f in new[] { WorldDb.RestrictedPass, WorldDb.ThornePassShown, WorldDb.StacksUnlooped }) c.G.World.SetFlag(f);
                    c.G.Quests.Set("overdue", 3, c.P);
                })
                .Bye("Thank you, Archivist.");
            d.Node("start", who, c => "Quietly, please.")
                .Say("What's beyond the long corridor?", "corridor")
                .Bye("Sorry.");
            return d;
        }

        static Dialogue Varrow()
        {
            bool night(DCtx c) => c.G.Clock.Hour >= 20 || c.G.Clock.Hour < 3;
            var d = new Dialogue("varrow", c =>
                c.G.Quests.Stage("measure_wyrm") == 1 && night(c) && In(c, "academy") ? "measure" : c.G.World.Flag("met_varrow") ? "start" : "intro");
            const string who = "Magister Isolde Varrow";
            void Done(DCtx c) => c.G.Quests.Set("measure_wyrm", 2, c.P);
            d.Node("intro", who,
                c => "You're the ones from the valley. With the... yes. Isolde Varrow. I count stars. Lately I count other things. Wingspans, for instance. Yours must be- no. Later. I'm being rude.",
                c => $"A tall woman with ink-stained cuffs and an astrolabe on a ribbon round her wrist. Her eyes keep sliding back to {c.G.World.DragonName}, measuring.",
                c => c.G.World.SetFlag("met_varrow"))
                .Say("Measure what, exactly?", "ask", show: c => c.G.Quests.Stage("measure_wyrm") == 0)
                .Bye("Nice to meet you, Magister.");
            d.Node("start", who, c => night(c) ? "Clear sky tonight. Every star where it should be. That's what worries me." : "Charts, charts, charts.")
                .Say("Measure what, exactly?", "ask", show: c => c.G.Quests.Stage("measure_wyrm") == 0)
                .Roll(Skill.Insight, 15, "(Insight) Who else reads your notes?", "post", "post_no", show: c => c.G.Quests.Stage("measure_wyrm") >= 1 && !c.G.World.Flag("varrow_post"))
                .Bye("Goodbye, Magister.");
            d.Node("ask", who,
                c => $"{c.G.World.DragonName}. Wingspan, length, the temperature of its breath, how it stands against the stars. The stars over the Wyrmback have been... restless. I want to know if a waking dragon is part of the same sky. Bring it to the telescopes after dark. I'll be gentle.",
                onEnter: c => c.G.Quests.Start("measure_wyrm", c.P))
                .Bye("We'll think about it.");
            d.Node("measure", who,
                c => $"There you are. Look at it in the starlight. Look at the SHAPE of it. Now: may I? A tape, a thermometer, five minutes. It won't hurt. It may tickle.",
                c => $"{c.G.World.DragonName} eyes the measuring tape like it's a snake.")
                .Say("Go ahead.", "measured", doIt: c => { DragonPet.AddBond(c.P, -3, "being measured"); Factions.Adjust("academy", 5, c.P); Done(c); })
                .Say("Not tonight. Sorry, Magister.", "refused", doIt: Done)
                .Roll(Skill.AnimalHandling, 13, $"Let it decide. (Kneel beside it, calm and close)", "decided_yes", "decided_no",
                    onSuccess: c => { DragonPet.AddBond(c.P, 2, "being asked"); Factions.Adjust("academy", 5, c.P); Done(c); },
                    onFailure: c => { DragonPet.AddBond(c.P, -1, "being asked, and sulking about it"); Done(c); }, social: false)
                .Roll(Skill.Insight, 15, "(Insight) Who else reads your notes?", "post", "post_no", show: c => !c.G.World.Flag("varrow_post"));
            d.Node("measured", who, c => "Remarkable. Remarkable. Thank you. Here: a chart of tonight's sky. Look at the circled ones.",
                c => $"{c.G.World.DragonName} submits to the tape with the air of a cat at the vet, and sulks for an hour after.").Bye("Goodnight, Magister.");
            d.Node("refused", who, c => "Of course. Of course. Its sky, not mine. Take this anyway: a chart of tonight. Look at the circled ones.",
                c => "She hides her disappointment well. Not perfectly.").Bye("Goodnight, Magister.");
            d.Node("decided_yes", who, c => "It... offered me its wing. Did you see that? It OFFERED. Here, take the chart, take it, I need to write this down.",
                c => $"{c.G.World.DragonName} holds out one wing, very dignified, for exactly as long as it takes to measure, and not one second longer.").Bye("Goodnight, Magister.");
            d.Node("decided_no", who, c => "No? No. Fair. Here, take the chart anyway. Some other night, perhaps.",
                c => $"{c.G.World.DragonName} turns its back on the tape, and on her, and on you a bit too.").Bye("Goodnight, Magister.");
            d.Node("post", who, c => "...",
                c => "She writes every note twice. The first copy goes in her own book. The second she folds, seals with plain, unmarked wax, and gives to the evening post, every evening, without fail. She doesn't know you've noticed.",
                c => c.G.World.SetFlag("varrow_post"))
                .Bye("Goodnight, Magister.");
            d.Node("post_no", who, c => "...", c => "Just a scholar, scribbling.").Bye("Goodnight, Magister.");
            return d;
        }

        /// <summary>Act II lines bolted onto older dialogues.</summary>
        static void AddAcademyLines(string id, Dialogue d)
        {
            switch (id)
            {
                case "rhoswen": RhoswenAnswers(d); break;
                case "perrin": PerrinAtTheAcademy(d); break;
            }
        }

        static void RhoswenAnswers(Dialogue d)
        {
            const string who = "Captain Rhoswen Vale";
            var inner = d.Start;
            d.Start = c => c.G.World.Act >= 2 && c.G.Quests.Stage(Answers.Id) == Answers.Road ? "answers_road" : inner(c);
            d.Node("answers_road", who,
                c => "Another hearth went cold in Lower Sedge last night. Third this week. I've got a watch to run and a village to keep calm, and no idea what I'm fighting. If anybody knows, it's the Academy. Take the Lantern Road east past the ford. It's open again: the scholars mend it every autumn.",
                c => "She looks at the dragon a moment too long. \"And keep that one close. People are frightened. Frightened people count things.\"")
                .Bye("We'll find answers.");
        }

        // =================================================================== Sergeant Dace Holloway (the Crown checkpoint)

        static Dialogue Holloway()
        {
            var d = new Dialogue("holloway", c => c.G.World.Flag("met_holloway") ? "start" : "intro");
            const string who = "Sergeant Dace Holloway";
            d.Node("intro", who,
                c => "Halt. Well. Halt-ish. Crown checkpoint. Names, where from, where bound, and anything with you that has more than two legs.",
                c => "A broad, tired man in a Crown tabard that's been mended more times than it's been washed. He has a ledger under one arm and a pencil behind each ear, because he keeps losing the first one.",
                c => c.G.World.SetFlag("met_holloway"))
                .Say("Thistledown, bound for the Academy.", "logged")
                .Say("Why a checkpoint on a scholars' road?", "why");
            d.Node("logged", who,
                c => "Thistledown. Academy. And... one large dog. Question mark. Go on through. Mind the farm: the boars there aren't right in the head.",
                c => $"He looks at {c.G.World.DragonName}. {c.G.World.DragonName} looks back. He writes something down, crosses it out, and writes it again.")
                .Say("Why a checkpoint on a scholars' road?", "why")
                .Bye("Thanks, Sergeant.");
            d.Node("why", who,
                c => "Because the fields went grey and the beasts went wrong and someone in the capital wants it counted. We're here to keep the road safe. Mostly from the road.")
                .Roll(Skill.Insight, 13, "(Insight) Does he know why they're counting?", "why_insight", "why_insight_no")
                .Bye("Fair enough.");
            d.Node("why_insight", who, c => "...",
                c => "He doesn't know. He doesn't like not knowing, and he likes the new column in his ledger even less. He's a decent man doing a job he only half understands, and he's homesick.")
                .Say("(Back)", "start");
            d.Node("why_insight_no", who, c => "...", c => "A soldier's face. Practiced, polite, closed.").Say("(Back)", "start");
            d.Node("start", who,
                c => c.G.Clock.Hour >= 19 || c.G.Clock.Hour < 6
                    ? "Fire's warm if you want it. Don't tell the lieutenant. There isn't a lieutenant. Don't tell her anyway."
                    : "Road's quiet. Too quiet, my sergeant used to say. I never knew what he meant till this year.")
                .Say("Why a checkpoint on a scholars' road?", "why")
                .Say("Where are you from?", "home")
                .Bye("Take care, Sergeant.");
            d.Node("home", who, c => "Port Varrel. Salt and gulls and my mother's fish stew. I put in for a transfer back. They sent me further inland. That's the army for you: every request answered, none of them granted.")
                .Bye("I hope you get home.");
            return d;
        }
    }
}
