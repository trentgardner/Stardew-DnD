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
                case "sorrel": return Sorrel();
                case "calloway": return Calloway();
                case "odo": return Odo();
                case "mother_hale": return MotherHale();
                case "wyvern_egg": return WyvernEgg();
                case "blossom_speaker": return CourtSpeaker("blossom_speaker", "Speaker of Blossoms", "heard_blossom",
                    "The spring belongs to whoever makes the most of it, and that is us. Every flower in this glade drinks from it. Frost would sit on it, and keep it pretty, and let nothing grow.",
                    "A slim figure in a gold mask shaped like a sleepy fox, petals caught in their hair. They speak in a hurry, as if spring might end while they're talking.");
                case "frost_speaker": return CourtSpeaker("frost_speaker", "Speaker of Frost", "heard_frost",
                    "The Embers are failing, and someone must keep the spring safe until they recover. Blossoms would drink it dry by midsummer. We keep things. That is what winter is for.",
                    "A tall figure in a silver mask, breath smoking in the warm air. They speak slowly, as if every word had to be carried a long way.");
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
                case "thimble": ThimbleOpensTheRing(d); break;
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

        // =================================================================== The Fae Courts: Thimble, Herald Sorrel, the court speakers
        // Charming, exact, older than they look. Every word true, and chosen. The bargains are beautiful and they have clauses.

        static void ThimbleOpensTheRing(Dialogue d)
        {
            const string who = "Mister Thimble";
            var inner = d.Start;
            d.Start = c => c.G.World.Act >= 2 ? (c.G.World.Flag(WorldDb.RingOpen) ? "a2_again" : "a2_ring") : inner(c);
            void open(DCtx c)
            {
                if (c.G.World.Flag(WorldDb.RingOpen)) return;
                c.G.World.SetFlag(WorldDb.RingOpen);
                foreach (var p in c.G.Players) if (p.IsReady) p.Hud.Narrate("The fairy ring brightens, mushroom by mushroom, until the air in its middle shimmers like a heat haze. Step into it, and you'll be somewhere else.");
            }
            d.Node("a2_ring", who,
                c => "THERE you are. The Courts have been asking and asking. 'Where are the little flame's people? Bring us the little flame's people, Thimble!' Well, here you are, and here's me, and there's the ring. Shall I open it? It's very rude to keep a Court waiting. They do it to everyone else, of course.",
                c => $"He tips his hat to {c.G.World.DragonName}, which sneezes at him.")
                .Say("Open it. We'll go.", "a2_open", doIt: open)
                .Roll(Skill.Performance, 13, "(Dance in the ring first, for luck)", "a2_danced", "a2_flop", onSuccess: c => { c.G.World.AddFriendship("thimble", 30); open(c); })
                .Say("Who are the Courts, exactly?", "a2_courts")
                .Bye("Not yet.");
            d.Node("a2_courts", who,
                c => "Three, these days. Blossoms, Embers and Frost. There used to be more chairs at the table. Don't ask about the chairs. Herald Sorrel speaks for all of them, and she is very, very old, and very good at promises.")
                .Say("Open it. We'll go.", "a2_open", doIt: open)
                .Bye("Not yet.");
            d.Node("a2_danced", who, c => "Oh, BRAVO. The mushrooms will be talking about that for a century. In you go, then!").Bye("Thank you, Mister Thimble.");
            d.Node("a2_flop", who, c => "That was... a dance. Of a kind. Never mind! I'll open it anyway, I just won't tell anyone you did that.")
                .Say("Open it. We'll go.", "a2_open", doIt: open)
                .Bye("Not yet.");
            d.Node("a2_open", who, c => "Mind your manners, don't eat anything, and if anyone offers you a deal, read it twice. Go on!").Bye("Thank you, Mister Thimble.");
            d.Node("a2_again", who, c => "The ring is open for you, clever-boots. Sorrel is waiting. Sorrel is always waiting. It's her best trick.")
                .Say("Fancy a game, for old times' sake?", "start")
                .Bye("Thank you, Mister Thimble.");
        }

        static Dialogue CourtSpeaker(string id, string who, string heardFlag, string claim, string look)
        {
            var d = new Dialogue(id, c => "start");
            d.Node("start", who,
                c => c.G.Quests.Stage("court_quarrel") == 1 && !c.G.World.Flag(heardFlag) ? claim : "The Herald will judge. Or you will. The Courts will remember either way.",
                c => look,
                c => { if (c.G.Quests.Stage("court_quarrel") == 1) c.G.World.SetFlag(heardFlag); })
                .Bye("We'll remember.");
            return d;
        }

        static Dialogue Sorrel()
        {
            var d = new Dialogue("sorrel", c =>
                !c.G.World.Flag("met_sorrel") ? "intro"
                : c.G.Quests.Stage("court_quarrel") == 2 && c.G.Quests.CanStart("court_quarrel") ? "judge"
                : c.G.Quests.Stage("green_bargain") == 2 ? "bargain"
                : c.G.Quests.Stage("green_bargain") == 1 ? "bargain_task"
                : "start");
            const string who = "Herald Sorrel";
            d.Node("intro", who,
                c => "The little flame's people. Welcome to the Gloaming. Sit, sit. Eat nothing, drink nothing, and you'll go home exactly as you came, I promise you. You see? Promises are easy. It's keeping them that takes a thousand years.",
                c => $"A tall woman in a gown of falling leaves, with a face that is young until she smiles. Her eyes rest on {c.G.World.DragonName} with something like hunger and something like grief.",
                c => { c.G.World.SetFlag("met_sorrel"); Factions.Hear("fae"); })
                .Say("What do the Courts want with our dragon?", "want")
                .Say("What happened to the Pact?", "pact");
            d.Node("want", who,
                c => "What we have always wanted: the Pact, kept. Once, dragons and Courts and the little short-lived peoples shared the fire of the world, fairly, by season and by promise. Then the Pact broke, and the world has been running cold ever since. Your little flame is the first new fire in a thousand years. Bring it into the Courts' keeping, and the Pact can be renewed. On the right terms.")
                .Roll(Skill.Insight, 15, "(Insight) What are 'the right terms'?", "want_ins", "want_ins_no")
                .Say("(Back)", "start");
            d.Node("want_ins", who, c => "...",
                c => "Every word she says is true, and every word was chosen. 'The right terms' means the Courts' terms. She believes, with her whole ancient heart, that they would be the right ones.")
                .Say("(Back)", "start");
            d.Node("want_ins_no", who, c => "...", c => "She smiles. You learn nothing from it, which you suspect is the point.").Say("(Back)", "start");
            d.Node("pact", who,
                c => "It broke. That is all anyone remembers truly, and the Courts remember everything. The rest is stories, and stories are told by whoever is left. Ask the Academy. They have so many books.")
                .Say("(Back)", "start");

            d.Node("start", who,
                c => c.G.World.CommittedFaction == "fae" ? "The Courts remember their friends. Always."
                    : c.G.World.CommittedFaction != null ? $"You chose {FactionDb.NameOf(c.G.World.CommittedFaction)}. A promise is a promise. We'll see how they keep theirs."
                    : "Back again. You've the look of someone weighing promises. Good. Weigh ours too.")
                .Say("What do the Courts want with our dragon?", "want")
                .Say("Is there anything we can do for the Courts?", "quarrel", show: c => c.G.Quests.Stage("court_quarrel") == 0 && c.G.Quests.CanStart("court_quarrel"))
                .Say("Is there anything more we can do?", "fae_job", show: c => Uncommitted(c) && c.G.Quests.Stage("court_quarrel") > 0 && !Factions.MajorOpen("fae"))
                .CommitOption("We'll stand with the Courts.", "fae", "green_bargain")
                .Bye("Farewell, Herald.");
            d.Node("fae_job", who, c => NotYet(c, "fae", "Settle our little quarrel first. Then we'll talk of bigger things."))
                .Say("(Back)", "start");
            d.Node("quarrel", who,
                c => "A small thing. A family thing. The Embers are cold this year, and the last warm spring in the glade is theirs no longer. Now the Blossoms and the Frost both claim it. Speak to their speakers. Then judge between them. A mortal judgement is a fresh one: the Courts are tired of their own.",
                onEnter: c => c.G.Quests.Start("court_quarrel", c.P))
                .Bye("We'll hear them out.");

            void judged(DCtx c, int fae, string flag) { if (c.G.Quests.Done("court_quarrel")) return; if (fae != 0) Factions.Adjust("fae", fae, c.P); if (flag != null) c.G.World.SetFlag(flag); c.G.Quests.Set("court_quarrel", 3, c.P); }
            d.Node("judge", who, c => "Well, judge? Who keeps the spring?")
                .Say("The Blossoms. Let it be used.", "judged", doIt: c => judged(c, 5, "fae_judged_blossom"))
                .Say("The Frost. Let it be kept safe.", "judged", doIt: c => judged(c, 5, "fae_judged_frost"))
                .Roll(Skill.Persuasion, 15, "Share it by season, as the Pact did.", "judged_wise", "judged_badly",
                    onSuccess: c => judged(c, 15, "fae_judged_seasons"), onFailure: c => judged(c, 0, "fae_judged_seasons"));
            d.Node("judged", who, c => "So be it. One court is pleased, and one will remember. That is how judgements go.").Say("(Back)", "start");
            d.Node("judged_wise", who, c => "...By season. As it was. Oh, that was well said. The Courts have not heard that said in a very long time.",
                c => "Somewhere in the glade, both courts' leaves stir at once.").Say("(Back)", "start");
            d.Node("judged_badly", who, c => "A good answer, badly argued. They'll share it, grudgingly. It's a start.").Say("(Back)", "start");

            d.Node("bargain_task", who,
                c => "A token for each throne: Blossoms, Embers, Frost. Each court must accept what you bring. The Embers will want a fire they can believe in. The Frost will want to be asked nicely, or shown you're strong enough to ask rudely.")
                .Bye("We'll carry them.");
            d.Node("bargain", who,
                c => "Every throne has accepted. Then here is the bargain, written fair. The Courts renew their part of the Pact in this valley: the frosts come on time, the blossoms come on time, the hearths of your people stay lit. Sign, and it is done.",
                c => "The scroll is green and gold and smells of rain. The writing is lovely, and very, very small.")
                .Roll(Skill.Investigation, 15, "(Investigation) Read the small writing.", "clause", "no_clause", social: false, show: c => !c.G.World.Flag("fae_clause_found"))
                .Roll(Skill.Insight, 16, "(Insight) Watch her while we read.", "clause", "no_clause", show: c => !c.G.World.Flag("fae_clause_found"))
                .Roll(Skill.Persuasion, 18, "Strike the clause about our dragon, and we'll sign.", "struck", "not_struck", show: c => c.G.World.Flag("fae_clause_found") && !c.G.World.Flag("fae_clause_tried"),
                    onSuccess: c => { c.G.World.SetFlag("fae_clause_tried"); c.G.World.SetFlag("fae_clause_struck"); Factions.Adjust("fae", -5, c.P); },
                    onFailure: c => c.G.World.SetFlag("fae_clause_tried"))
                .Say("We'll sign it as written.", "signed", doIt: c => Signed(c, struck: false))
                .Bye("Not today.");
            d.Node("clause", who, c => "...",
                c => "Tucked into the curl of a capital letter: \"And the little flame shall owe the Courts one favour, to be asked when the stars ask it.\" Sorrel watches you find it, and doesn't look sorry.",
                c => c.G.World.SetFlag("fae_clause_found"))
                .Say("(Back)", "bargain");
            d.Node("no_clause", who, c => "...", c => "Lovely writing. Very small. You can't make out half of it.").Say("(Back)", "bargain");
            d.Node("struck", who, c => "...Struck, then. You bargain like a Court. I find I don't mind.",
                c => "She draws one long fingernail through the line, and the ink unwrites itself.")
                .Say("Then we'll sign.", "signed", doIt: c => Signed(c, struck: true));
            d.Node("not_struck", who, c => "No. That clause is the price of the rest. Sign it as it is, or come back when you're ready.")
                .Say("We'll sign it as written.", "signed", doIt: c => Signed(c, struck: false))
                .Bye("Not today.");
            d.Node("signed", who,
                c => "And so it is renewed, a little, here. Take this, from the Blossom throne: dew in a cup. It will mend you when you need mending.",
                c => "Your name shines on the scroll for a moment, then sinks into the paper like water into soil.")
                .Bye("Farewell, Herald.");
            return d;
        }

        static void Signed(DCtx c, bool struck)
        {
            if (c.G.Quests.Done("green_bargain")) return;
            if (!struck) c.G.World.SetFlag("fae_clause");
            Factions.Adjust("fae", 30, c.P);
            c.G.GiveItem(c.P, "bloom_draught", 3);
            c.G.Quests.Set("green_bargain", 3, c.P);
        }

        // =================================================================== The Wyrmwatch: Huntmaster Bryn Calloway, Odo, Mother Hale
        // Weathered, blunt, kind to children. Brave people protecting villages from beasts that really are dangerous.

        static Dialogue Calloway()
        {
            var d = new Dialogue("calloway", c =>
                !c.G.World.Flag("met_calloway") ? "intro"
                : c.G.Quests.Stage("ember_eyes") == 5 ? "eyes_done"
                : c.G.Quests.Stage("long_hunt") == 4 ? "hunt_report"
                : c.G.Quests.Stage("long_hunt") is int s && s >= 1 && s < 4 ? "hunt_task"
                : "start");
            const string who = "Huntmaster Bryn Calloway";
            d.Node("intro", who,
                c => $"So that's it. That's the one everybody's talking about. ...It's smaller than I thought. They always are, at first. Bryn Calloway. I run the Wyrmwatch. Keep your hand on its collar while you're in my village, and we'll get along.",
                c => $"A broad, weathered woman in grey oiled wool, with a crossbow across her back and old burn scars up one arm. She doesn't take her eyes off {c.G.World.DragonName} once.",
                c => { c.G.World.SetFlag("met_calloway"); Factions.Hear("hunters"); })
                .Say("What does the Wyrmwatch want with our dragon?", "want")
                .Roll(Skill.Insight, 15, "(Insight) She's afraid of it.", "afraid", "afraid_no")
                .Say("What's the Wyrmwatch?", "what");
            d.Node("want", who,
                c => "Want? Nothing. I want it to stay small and stay kind and stay yours. But I've buried too many people to believe in 'stay'. Every village we've lost to an ember-eyed beast had somebody in it who said 'it's only small'. So we watch. We'd like to watch yours. A bell on its collar, so a village can hear it coming. A hunter near it, always. And if the day comes, someone who can do what needs doing.")
                .Say("And if we say no?", "no")
                .Say("(Back)", "start");
            d.Node("no", who, c => "Then you say no. I'm not the Crown. I don't take things. I just wait, and I'm usually right, and I hate it.")
                .Say("(Back)", "start");
            d.Node("afraid", who, c => "...",
                c => "She is. Honestly, deeply afraid of the young dragon at your heel, and ashamed of it, because it hasn't done anything. She's seen what grown ones do. She keeps her hands still on purpose.")
                .Say("(Back)", "start");
            d.Node("afraid_no", who, c => "...", c => "A hunter's face: patient, watchful, unreadable.").Say("(Back)", "start");
            d.Node("what", who,
                c => "An order. Old as the Pact, or near enough. We hunt what comes down off the mountain with fire in its eyes. Cinder hounds. Ember-eyed bears. Worse. There used to be a few of us. This year there's a lot of us, and it's still not enough.")
                .Say("(Back)", "start");

            d.Node("start", who,
                c => c.G.World.CommittedFaction == "hunters" ? "The Wyrmwatch looks after its own. That's you now."
                    : c.G.World.CommittedFaction != null ? $"{FactionDb.Title(c.G.World.CommittedFaction)}, is it. Well. I hope they're right. I hope I'm wrong. I'd like that for once."
                    : c.G.Clock.Hour >= 20 || c.G.Clock.Hour < 6 ? "Quiet. Too quiet. Keep your voice down: they hear further than you'd think." : "Still here? Good. Somebody ought to see what it's like out here, before they decide anything.")
                .Say("What does the Wyrmwatch want with our dragon?", "want")
                .Say("Is there anything we can do for Cragfoot?", "eyes", show: c => c.G.Quests.Stage("ember_eyes") == 0 && c.G.Quests.CanStart("ember_eyes"))
                .Say("Is there anything more we can do?", "hunt_job", show: c => Uncommitted(c) && c.G.Quests.Stage("ember_eyes") > 0 && !Factions.MajorOpen("hunters"))
                .CommitOption("We'll stand with the Wyrmwatch.", "hunters", "long_hunt")
                .Bye("Take care, Huntmaster.");
            d.Node("hunt_job", who, c => NotYet(c, "hunters", "Help me with the goats first. Then we'll talk about the thing that's been eating them."))
                .Say("(Back)", "start");
            d.Node("eyes", who,
                c => "Something's taking the goats. At night, over the palisade, no noise. Mother Hale's lost three and her dog. I've got two hunters fit to stand and one of them's Odo. Keep watch with us tonight, by the pen, after dark. If it's what I think it is, you'll want your weapons out and your dragon close.",
                onEnter: c => c.G.Quests.Start("ember_eyes", c.P))
                .Bye("We'll keep watch.");
            d.Node("eyes_done", who,
                c => "Bramble. Mother Hale's dog. ...He went missing in the spring, before the ash got bad. I'll tell her. No, I'll tell her: it's my job. That's the part of the work nobody sings about. They were somebody's, once. Most of them were.",
                c => "She turns the collar over in her scarred hands for a long moment.",
                c => { if (!c.G.Quests.Done("ember_eyes")) { Factions.Adjust("hunters", 10, c.P); c.G.Quests.Set("ember_eyes", 6, c.P); } })
                .Bye("I'm sorry, Huntmaster.");

            d.Node("hunt_task", who,
                c => c.G.Quests.Stage("long_hunt") == 1
                    ? "The Ashmaw. A wyvern, ember-eyed, nests somewhere up the goat track. It's had nine goats and a mule, and it's going to have a child next. Read the track: claw marks, bones. Find where it sleeps."
                    : c.G.Quests.Stage("long_hunt") == 2 ? "The cliffs, then. Go carefully. Kill it if you have to. Drive it off if you can. I won't think less of you either way. Much."
                    : "Its nest. Look in its nest. Then come back and tell me what you found.")
                .Bye("On it.");
            d.Node("hunt_report", who,
                c => "It's done, then. Cragfoot will sleep tonight. One more thing, and then the Wyrmwatch stands with you, all of us, for good. A bell. Silver, small. On its collar. So that whatever it grows into, a village will hear it coming.",
                c => $"She holds out a small silver bell on a leather loop. {c.G.World.DragonName} stares at it.")
                .Say("Do it.", "hunt_belled", doIt: c => { c.G.World.SetFlag("wyrmwatch_belled"); Factions.Adjust("hunters", 10, c.P); DragonPet.AddBond(c.P, -1, "the bell"); HuntDone(c); })
                .Say("No bell.", "hunt_unbelled", doIt: c => { Factions.Adjust("hunters", -5, c.P); HuntDone(c); });
            d.Node("hunt_belled", who,
                c => "Thank you. I mean it. Here: a Wyrmwatch cloak. You've earned it.",
                c => $"The bell tinkles with every step {c.G.World.DragonName} takes. It sulks for an hour, then discovers it can ring the bell on purpose, and is unbearable about it.")
                .Bye("Thank you, Huntmaster.");
            d.Node("hunt_unbelled", who,
                c => "...Your call. It's your dragon. I'll just listen harder. Here: a Wyrmwatch cloak anyway. You've earned that much.")
                .Bye("Thank you, Huntmaster.");
            return d;
        }

        static void HuntDone(DCtx c)
        {
            if (c.G.Quests.Done("long_hunt")) return;
            Factions.Adjust("hunters", 30, c.P);
            c.G.Quests.Set("long_hunt", 5, c.P);
        }

        static Dialogue Odo()
        {
            var d = new Dialogue("odo", c => c.G.Quests.Stage("ember_eyes") == 3 ? "hurt" : "start");
            const string who = "Odo Fenn";
            void healed(DCtx c) { if (c.G.Quests.Stage("ember_eyes") == 3) c.G.Quests.Set("ember_eyes", 4, c.P); }
            d.Node("start", who,
                c => c.G.Quests.Done("ember_eyes") || c.G.Quests.Stage("ember_eyes") >= 4
                    ? "Still got all my fingers. Counted 'em twice. Thanks to you."
                    : "Odo Fenn, hunter of the Wyrmwatch. Second-best shot in Cragfoot. There are two of us.",
                c => "A cheerful young man in a hood two sizes too big, holding a crossbow like it might go off.")
                .Bye("Good hunting, Odo.");
            d.Node("hurt", who,
                c => "It's fine. It's fine! It's a scratch. It's a really big scratch. Is it meant to be that colour?",
                c => "Odo sits against the palisade with his leg out, very pale, a wolf's bite through his boot.")
                .Roll(Skill.Medicine, 12, "(Medicine) Clean and bind the wound.", "patched", "patch_fail", social: false, onSuccess: healed)
                .Say("Here, drink this.", "patched", doIt: c => { if (c.G.TakeQuestItem(c.P, "potion_healing") || c.G.TakeQuestItem(c.P, "potion_greater_healing")) healed(c); },
                    show: c => c.P.Character.CountOf("potion_healing") > 0 || c.P.Character.CountOf("potion_greater_healing") > 0)
                .Bye("Hold on, Odo.");
            d.Node("patched", who, c => "Oh, that's better. That's much better. I'm going to be sick. No I'm not. Yes I am. No. Thank you!",
                c => "The colour comes back to his face, slowly.").Bye("Rest, Odo.");
            d.Node("patch_fail", who, c => "Ow. OW. Maybe let someone else have a go?", c => "The bandage slips. You'll need to try again, or find something stronger.")
                .Say("(Try again)", "hurt");
            return d;
        }

        static Dialogue MotherHale()
        {
            var d = new Dialogue("mother_hale", c => "start");
            const string who = "Mother Hale";
            d.Node("start", who,
                c => c.G.Quests.Done("ember_eyes")
                    ? "Bryn told me. About Bramble. ...He was a good dog. A silly, good dog. I'm glad it was you who found him, and not the crows."
                    : "Have you seen a dog? Black and white, one ear up, answers to Bramble, or to biscuits. He went off in the spring. He'll come back. They do.",
                c => "A small, sturdy old woman with a shawl and a lamp she keeps lit in the window, day and night.")
                .Bye("Take care, Mother Hale.");
            return d;
        }

        /// <summary>The Ashmaw's nest: one egg, and three ways to leave it.</summary>
        static Dialogue WyvernEgg()
        {
            var d = new Dialogue("wyvern_egg", c => "nest");
            void done(DCtx c) { if (c.G.Quests.Stage("long_hunt") == 3) c.G.Quests.Set("long_hunt", 4, c.P); }
            d.Node("nest", null, c => "",
                c => $"In the nest of bones and branches, one egg: grey, leathery, warm. Something inside it shifts when you lean close. Calloway's order was plain. {c.G.World.DragonName} sniffs the egg, very carefully, and looks at you.")
                .Say("Destroy it. That's the job.", "smashed", doIt: c => { Factions.Adjust("hunters", 10, c.P); DragonPet.AddBond(c.P, -1, "the egg"); done(c); })
                .Say("Leave it be.", "left", doIt: c => { Factions.Adjust("hunters", -10, c.P); DragonPet.AddBond(c.P, 2, "the egg"); done(c); })
                .Say("Take it to the Academy. Let them raise it.", "taken", doIt: c => { Factions.Adjust("hunters", -5, c.P); c.G.World.SetFlag("wyvern_egg_saved"); c.G.GiveItem(c.P, "wyvern_egg"); done(c); });
            d.Node("smashed", null, c => "", c => $"It's quick. It's quieter than you expected. {c.G.World.DragonName} turns away and won't look at you for a while.").Bye("(Leave the nest)");
            d.Node("left", null, c => "", c => $"You leave it in its nest, warm in the dark. {c.G.World.DragonName} presses against your legs all the way down the mountain.").Bye("(Leave the nest)");
            d.Node("taken", null, c => "", c => $"You wrap it in a cloak. It's heavier than it looks. {c.G.World.DragonName} insists on walking right beside it.").Bye("(Leave the nest)");
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
