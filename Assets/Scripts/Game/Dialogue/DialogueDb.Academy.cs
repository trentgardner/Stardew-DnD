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
                default: return null;
            }
        }

        /// <summary>Act II lines bolted onto older dialogues.</summary>
        static void AddAcademyLines(string id, Dialogue d)
        {
            switch (id)
            {
                case "rhoswen": RhoswenAnswers(d); break;
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
