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
            d.Node("start", who,
                c => c.G.Clock.Hour < 1
                    ? "Shh! Shh. Listen. Any second now. Twelve, and then... You'll see. You'll hear. Whatever."
                    : "Lectures are optional. Philosophically. If you're from the Archivist, I've never heard of her.")
                .Bye("Carry on, Pell.");
            return d;
        }

        static Dialogue Oakes()
        {
            var d = new Dialogue("oakes", c => c.G.World.Flag("met_oakes") ? "start" : "intro");
            const string who = "Professor Bramwell Oakes";
            d.Node("intro", who,
                c => "Mind the lilies! Mind them, mind them, there. Thank you. Bramwell Oakes, Master of the Glasshouse. Forty years I've grown ember-lilies, and they've never once looked like this.",
                c => "A round, earthy man with soil under every fingernail and a magnifying glass on a chain. He cradles a grey-edged lily like a sick kitten.",
                c => c.G.World.SetFlag("met_oakes"))
                .Bye("I hope they recover.");
            d.Node("start", who, c => "Grey at the edges. Grey! As if the fire in them is going out.").Bye("Good luck, Professor.");
            return d;
        }

        static Dialogue Varrow()
        {
            var d = new Dialogue("varrow", c => c.G.World.Flag("met_varrow") ? "start" : "intro");
            const string who = "Magister Isolde Varrow";
            d.Node("intro", who,
                c => $"You're the ones from the valley. With the... yes. Isolde Varrow. I count stars. Lately I count other things. Wingspans, for instance. Yours must be- no. Later. I'm being rude.",
                c => $"A tall woman with ink-stained cuffs and an astrolabe on a ribbon round her wrist. Her eyes keep sliding back to {c.G.World.DragonName}, measuring.",
                c => c.G.World.SetFlag("met_varrow"))
                .Bye("Nice to meet you, Magister.");
            d.Node("start", who, c => c.G.Clock.Hour >= 20 || c.G.Clock.Hour < 3 ? "Clear sky tonight. Every star where it should be. That's what worries me." : "Charts, charts, charts.").Bye("Goodbye, Magister.");
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
            d.Node("home", who, c => "Port Varrel. Salt and gulls and my mother's fish stew. I put in for a transfer back. They sent me further inland. That's the Crown for you: always listening, never hearing.")
                .Bye("I hope you get home.");
            return d;
        }
    }
}
