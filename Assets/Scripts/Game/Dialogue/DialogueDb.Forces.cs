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

        /// <summary>Puts the options an `add` call appends in front of the node's goodbye options.</summary>
        static void BeforeBye(DNode n, System.Action<DNode> add)
        {
            int before = n.Options.Count;
            add(n);
            var added = n.Options.GetRange(before, n.Options.Count - before);
            n.Options.RemoveRange(before, added.Count);
            int at = n.Options.FindIndex(o => o.End);
            n.Options.InsertRange(at < 0 ? n.Options.Count : at, added);
        }

        /// <summary>Phase 4 lines bolted onto older dialogues.</summary>
        static void AddForceLines(string id, Dialogue d)
        {
            switch (id)
            {
                case "perrin": PerrinLamps(d); break;
                case "ashby": AshbyCircle(d); break;
                case "varrow": StoneFrom(d, "varrow", "Magister Isolde Varrow"); break;
                case "oakes": StoneFrom(d, "oakes", "Professor Bramwell Oakes"); break;
            }
        }

        // =================================================================== The Academy: Perrin's lamps, Ashby's circle

        static void PerrinLamps(Dialogue d)
        {
            const string who = "Perrin Quillby";
            var inner = d.Start;
            d.Start = c => c.G.World.Act >= 2 && c.G.Quests.Stage("cold_lanterns") == 2 && c.G.Quests.CanStart("cold_lanterns") ? "lamps_done" : inner(c);
            if (d.Nodes.TryGetValue("a2_start", out var start))
                BeforeBye(start, n => n.Say("You look harried, Perrin.", "lamps", show: c => c.G.Quests.Stage("cold_lanterns") == 0 && c.G.Quests.CanStart("cold_lanterns")));
            d.Node("lamps", who,
                c => "Harried! Yes! That's the word! They've made me Keeper of the Road Lamps, which sounds grand and means I'm the one who gets shouted at. Six of the lanterns on the Lantern Road have gone out and won't relight. The oil's fine. The wicks are fine. They just... give up. A good honest torch might do it. Or, um. Someone who breathes fire. If they were willing.",
                c => $"He looks at {c.G.World.DragonName} with enormous hope.",
                c => { c.G.Quests.Start("cold_lanterns", c.P); c.G.Quests.Notify(QuestEvent.BrazierLit, "lantern_road", c.P); })
                .Bye("We'll light them.");
            d.Node("lamps_done", who,
                c => c.G.World.Flag("lantern_eyes")
                    ? "All six! The road's lit end to end, I saw it from the wall- wait. They WATCH? With little eyes? For what? ...Oh. Oh, I see. For dragon fire. I didn't know. I swear I didn't know. I'm going to have a very polite word with somebody."
                    : "All six! The road's lit end to end, I saw it from the wall. Travellers will see their way again. That's the Academy's oldest promise, you know: a light on the road for anyone who walks it.",
                c => c.G.World.Flag("lantern_eyes") ? $"{c.G.World.DragonName} tilts its head at the nearest lamp, suspicious." : "He beams like one of his own lanterns.",
                c => { if (!c.G.Quests.Done("cold_lanterns")) { Factions.Adjust("academy", 10, c.P); c.G.Quests.Set("cold_lanterns", 3, c.P); } })
                .Bye("Glad to help, Perrin.");
        }

        static void AshbyCircle(Dialogue d)
        {
            const string who = "High Magister Corvane Ashby";
            var inner = d.Start;
            d.Start = c =>
            {
                int s = c.G.Quests.Stage("ninth_circle");
                if (s == 4) return "circle_report";
                if (s >= 1 && s < 4) return "circle_task";
                return inner(c);
            };
            if (d.Nodes.TryGetValue("again", out var again))
                BeforeBye(again, n => n
                    .Say("Is there anything we can do for the Academy?", "circle_job", show: c => Uncommitted(c) && !Factions.MajorOpen("academy"))
                    .CommitOption("We'll help you finish the ninth circle.", "academy", "ninth_circle"));
            d.Node("circle_job", who, c => NotYet(c, "academy", "Young Perrin is drowning in lamps, I'm told. Help him, and then come and talk to me about circles."))
                .Bye("We'll come back.");
            d.Node("circle_task", who,
                c => c.G.Quests.Stage("ninth_circle") == 1
                    ? "Three focus stones. Isolde has one in the Observatory; Bramwell keeps one in the Glasshouse, though he'll pretend he doesn't; and the Starwell will give a third to anyone with the wit to ask it properly."
                    : c.G.Quests.Stage("ninth_circle") == 2
                        ? "You have them? Then down to the hall. Set them at the circle's points. Your dragon won't follow you down there. I'm sorry for that. It's the right instinct."
                        : "Something is in the circle? Then deal with it, gently if you can, and quickly either way.")
                .Bye("On our way.");
            d.Node("circle_report", who,
                c => "It held. For a breath, it held. And then it didn't. ...Thank you. I need it to hold forever, and now I know how far I am from that, which is worth more than I can say.",
                c => "He takes the stones back from you one by one, very carefully, as if they were eggs.")
                .Roll(Skill.Insight, 14, "(Insight) What's he not saying?", "circle_ins", "circle_ins_no")
                .Say("It'll hold next time.", "circle_done", doIt: CircleDone);
            d.Node("circle_ins", who, c => "...",
                c => "He would rather it hadn't been you down there. He would rather it had been anyone else. He's grateful, and ashamed of asking, and he will ask again.")
                .Say("It'll hold next time.", "circle_done", doIt: CircleDone);
            d.Node("circle_ins_no", who, c => "...", c => "Tired eyes, a steady voice. He gives nothing else away.")
                .Say("It'll hold next time.", "circle_done", doIt: CircleDone);
            d.Node("circle_done", who,
                c => "Then let it be next time. Here. I made this last night, when I couldn't sleep. A small ward, for you. Something that holds.",
                c => "A silver ring with a ward-circle engraved inside the band, so fine you need to tilt it to the light.")
                .Bye("Thank you, High Magister.");
        }

        static void CircleDone(DCtx c)
        {
            if (c.G.Quests.Done("ninth_circle")) return;
            foreach (var p in c.G.Players) if (p.Character != null) while (p.Character.CountOf("focus_stone") > 0 && c.G.TakeQuestItem(p, "focus_stone")) { }
            Factions.Adjust("academy", 30, c.P);
            c.G.Quests.Set("ninth_circle", 5, c.P);
        }

        /// <summary>Varrow and Oakes each hold one of the ninth circle's focus stones.</summary>
        static void StoneFrom(Dialogue d, string npc, string who)
        {
            string flag = "stone_" + npc;
            bool wants(DCtx c) => c.G.Quests.Stage("ninth_circle") == 1 && !c.G.World.Flag(flag);
            void give(DCtx c) { if (!c.G.World.Flag(flag)) { c.G.GiveItem(c.P, "focus_stone"); c.G.World.SetFlag(flag); } }
            if (!d.Nodes.TryGetValue("start", out var start)) return;
            if (npc == "varrow")
            {
                BeforeBye(start, n => n.Say("Ashby needs your focus stone for the ninth circle.", "stone", show: wants));
                d.Node("stone", who,
                    c => "Of course he does. Here. Take it, take it. The sooner that circle is finished, the better I'll sleep.",
                    c => "She hands it over without a moment's hesitation, which is somehow more unsettling than if she'd argued.",
                    give)
                    .Roll(Skill.Insight, 15, "(Insight) Why the hurry?", "stone_ins", "stone_ins_no")
                    .Bye("Thank you, Magister.");
                d.Node("stone_ins", who, c => "...",
                    c => "She wants the circle finished before anyone else comes asking for it. Anyone at all. She's frightened of what happens if somebody else holds the key.")
                    .Bye("Thank you, Magister.");
                d.Node("stone_ins_no", who, c => "...", c => "A scholar in a hurry. Scholars are always in a hurry.").Bye("Thank you, Magister.");
            }
            else
            {
                BeforeBye(start, n => n
                    .Say("Ashby needs your focus stone for the ninth circle.", "stone_free", show: c => wants(c) && c.G.Quests.Done("glasshouse"))
                    .Roll(Skill.Persuasion, 13, "Ashby needs your focus stone for the ninth circle.", "stone_yes", "stone_no", show: c => wants(c) && !c.G.Quests.Done("glasshouse")));
                d.Node("stone_free", who,
                    c => "For you? After the lilies? Take it. I don't like what he's building down there, I'll tell you that for free. But I like you.",
                    c => "He digs it out of a pot of soil, where it was keeping a fern warm.", give)
                    .Bye("Thank you, Professor.");
                d.Node("stone_yes", who,
                    c => "Hmph. It warms my seedlings, you know. But Corvane's never once asked me for anything he didn't need. Go on.",
                    c => "He digs it out of a pot of soil, where it was keeping a fern warm.", give)
                    .Bye("Thank you, Professor.");
                d.Node("stone_no", who, c => "No. Not today. I've seen what that circle is for, and my ferns need it more. Ask me again when I've had my tea.")
                    .Bye("Another time.");
            }
        }

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
