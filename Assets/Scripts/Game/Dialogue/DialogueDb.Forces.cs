using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 4: the four forces' people. Every force is right about something and asks a real price; nobody here
    /// is the good one. THE SECRET never leaks here (LoreTests scans every line that names the Queen).
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue ForcesDialogue(string id)
        {
            switch (id)
            {
                case "marsh": return Marsh();
                default: return null;
            }
        }

        /// <summary>The "we'll stand with you" option every force's leader offers once its major quest is open.</summary>
        static DNode CommitOption(this DNode n, string text, string faction, string questId) =>
            n.Say(text, doIt: c => Factions.StartMajor(c.P, questId), end: true,
                show: c => Factions.MajorOpen(faction) && c.G.Quests.Stage(questId) == 0);

        /// <summary>What a leader says while you're still hearing everyone out (or need to do one of their jobs first).</summary>
        static string NotYet(DCtx c, string faction, string job) =>
            !Factions.HeardAll()
                ? $"Hear the others out first. All of them. You've heard {Factions.HeardCount} of four. Then come back and tell me who's right."
                : job;

        static bool Uncommitted(DCtx c) => c.G.World.CommittedFaction == null;

        // =================================================================== The Crown: Lady-Commander Isaine Marsh
        // Crisp, fair, tired of being the villain in other people's stories. Means every word.

        static Dialogue Marsh()
        {
            var d = new Dialogue("marsh", c =>
                !c.G.World.Flag("met_marsh") ? "intro"
                : c.G.Quests.Stage("crowns_banner") == 3 ? "banner_report"
                : c.G.Quests.Stage("crowns_banner") is int s && s >= 1 && s < 3 ? "banner_task"
                : "start");
            const string who = "Lady-Commander Isaine Marsh";
            d.Node("intro", who,
                c => "You're the ones with the dragon. Don't look like that: half the valley knows, and the other half has heard it from the first half. Isaine Marsh. I hold this hill for the Crown. Sit, if you like. The stew is terrible, the tea is worse, and I'll tell you the truth, which is rarer than either.",
                c => "A lean woman with grey at her temples and ink on her fingers, in a Crown coat with the gold worn off the buttons. She doesn't stand when you come in, but she does put down her pen.",
                c => { c.G.World.SetFlag("met_marsh"); Factions.Hear("crown"); })
                .Say("What does the Crown want with our dragon?", "want")
                .Say("Why should we trust the Crown?", "trust");
            d.Node("want", who,
                c => "The same thing it wants with every river, road and granary in this valley: to keep it from killing anyone. The Academy studies. The Fae bargain. The hunters hunt. Somebody has to keep forty villages fed and safe while they do it, and that somebody is the realm. The realm offers you its protection: soldiers on Thistledown's bridge, grain when the harvest goes grey, a writ that opens every door. The price is that your dragon answers to the realm when the realm needs it. I won't pretty that up.")
                .Roll(Skill.Insight, 15, "(Insight) Does she mean it?", "want_ins", "want_ins_no")
                .Say("And if we say no?", "no")
                .Say("(Back)", "start");
            d.Node("want_ins", who, c => "...",
                c => "She means every word. She has also been ordered to bring your dragon in by any means, and you can see her choosing the polite ones, on purpose, every day.")
                .Say("(Back)", "start");
            d.Node("want_ins_no", who, c => "...", c => "A commander's face: practiced, patient, giving nothing away.").Say("(Back)", "start");
            d.Node("no", who, c => "Then you say no, and I write it down, and someone less polite than me reads it. I'd rather you said yes. I'd rather you said it because you'd heard everyone out and still thought we were right.")
                .Say("(Back)", "start");
            d.Node("trust", who, c => "You shouldn't. Not on my word. Trust the bridge that's still standing and the granary that's still full. Then decide.")
                .Say("(Back)", "start");

            d.Node("start", who,
                c => c.G.World.CommittedFaction == "crown" ? "The realm remembers its friends. So do I."
                    : c.G.World.CommittedFaction != null ? $"You stand with {FactionDb.NameOf(c.G.World.CommittedFaction)} now. I hope they're worth it. I mean that."
                    : "Back again. Good. Somebody in this valley should be asking questions, and I'd rather it was you.")
                .Say("What does the Crown want with our dragon?", "want")
                .Say("Who writes to you from the Academy?", "letters", doIt: c => c.G.Quests.Start("two_copies", c.P),
                    show: c => c.G.World.Flag("varrow_post") && c.G.Quests.Stage("two_copies") == 0 && c.G.Quests.CanStart("two_copies"))
                .Say("About Magister Varrow's letters...", "letters", show: c => c.G.Quests.Stage("two_copies") == 1 && c.G.Quests.CanStart("two_copies"))
                .Say("Is there something we can do for the Crown?", "job", show: c => Uncommitted(c) && !Factions.MajorOpen("crown"))
                .CommitOption("We'll stand with the Crown.", "crown", "crowns_banner")
                .Bye("Good day, Commander.");
            d.Node("job", who, c => NotYet(c, "crown", "Sergeant Holloway always has a job that needs doing. Help him, and then come and see me."))
                .Say("(Back)", "start");

            d.Node("letters", who,
                c => "Ah. Varrow. Yes: the Crown has read the Academy's notes on dragons for a year. Every measurement, every theory, every circle they draw under that library. If the Academy is building a cage under the realm's feet, the realm deserves to know what's in it. And who holds the key.",
                c => "She says it without shame, and without pleasure either.")
                .Say("Then Varrow's colleagues should know you read them.", "letters_done", doIt: c => { Factions.Adjust("academy", 10, c.P); Factions.Adjust("crown", -5, c.P); c.G.Quests.Set("two_copies", 2, c.P); })
                .Say("Your secret's safe with us.", "letters_done", doIt: c => { Factions.Adjust("crown", 10, c.P); Factions.Adjust("academy", -5, c.P); c.G.Quests.Set("two_copies", 2, c.P); })
                .Roll(Skill.Persuasion, 15, "Ask Ashby directly. He'd tell you, if you asked him to his face.", "letters_ask", "letters_ask_no",
                    onSuccess: c => { Factions.Adjust("crown", 5, c.P); Factions.Adjust("academy", 5, c.P); c.G.World.SetFlag("marsh_asks_ashby"); c.G.Quests.Set("two_copies", 2, c.P); },
                    onFailure: c => c.G.Quests.Set("two_copies", 2, c.P));
            d.Node("letters_done", who, c => "Then that's settled. Thank you for asking me first. Most people don't.").Say("(Back)", "start");
            d.Node("letters_ask", who, c => "...Perhaps I would. Perhaps I'll go up that tower myself and ask. Three hundred stairs, I'm told. Well. I've climbed worse hills for worse reasons.")
                .Say("(Back)", "start");
            d.Node("letters_ask_no", who, c => "Ashby is a brilliant man who has spent his whole life being right. I'm not going to ask him anything he can say no to.")
                .Say("(Back)", "start");

            d.Node("banner_task", who,
                c => c.G.Quests.Stage("crowns_banner") == 1
                    ? "The old beacon. They call themselves the Ashsworn. They came in the night and they've been sitting by a fire that doesn't smoke ever since, waiting for something. My soldiers won't go near them. I don't blame them. Clear them out, however you can. I'd rather nobody died."
                    : "Whatever they left behind up there, I want to see it.")
                .Bye("On it.");
            d.Node("banner_report", who,
                c => "\"Be ready to open the door.\" What door? Whose? ...Thank you. This goes to the capital tonight. One more thing, and then the realm stands behind you for good. Your dragon goes in the Royal Register. It wears this.",
                c => $"She holds up a small brass tag on a ring, stamped with the Crown and a number. {c.G.World.DragonName} looks at it as if it were a bee.")
                .Say("Do it.", "banner_done", doIt: c => { c.G.World.DragonBond -= 1; DragonDb.Shift("marsh", -1); BannerDone(c); })
                .Roll(Skill.AnimalHandling, 14, $"Let it get used to the tag first.", "banner_done", "banner_done",
                    onSuccess: BannerDone,
                    onFailure: c => { c.G.World.DragonBond -= 1; DragonDb.Shift("marsh", -1); BannerDone(c); });
            d.Node("banner_done", who,
                c => "Welcome to the realm's service. You'll find it more generous than its reputation. Here: the writ. Show it anywhere the Crown's flag flies.",
                c => $"{c.G.World.DragonName} scratches at the tag for a while, then forgets it, mostly.")
                .Bye("Thank you, Commander.");
            return d;
        }

        static void BannerDone(DCtx c)
        {
            if (c.G.Quests.Done("crowns_banner")) return;
            Factions.Adjust("crown", 30, c.P);
            c.G.Quests.Set("crowns_banner", 4, c.P);
        }
    }
}
