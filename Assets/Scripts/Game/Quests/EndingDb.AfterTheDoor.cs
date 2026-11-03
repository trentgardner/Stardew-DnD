using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>One titled group of beats in the ending sequence. Zone "town" = the party goes home to Thistledown first.</summary>
    public class EndingScene { public string Title; public string[] Beats; public string Zone; }

    /// <summary>One slide of the epilogue.</summary>
    public class EpilogueSlide { public string Title, Text; }

    public static class Ending
    {
        /// <summary>The ending's scenes have all been seen (the epilogue and credits come next).</summary>
        public const string Seen = "ending_seen";
        /// <summary>The credits have rolled: the story is told. The save is kept.</summary>
        public const string Complete = "game_complete";
        public const int MinScenes = 6, MinSlides = 8;
    }

    /// <summary>
    /// Act III Phase 7: the endings. EXEMPT from the secret guard (LoreTests.GuardScans): nothing here is reachable before
    /// WorldDb.MetHer, the choice at the turn, and the last fight. Warm and wry; never a verdict on which ending was right.
    /// </summary>
    public static class EndingDb
    {
        public const string CompanionsTitle = "The Ones Who Came With You", ThistledownTitle = "Thistledown";
        /// <summary>A phrase only Rhoswen's romance line contains (tests).</summary>
        public const string RomanceMarker = "kisses you";

        static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
        static string DragonOf(WorldState w) => w != null && w.EggHatched ? w.DragonName : "the dragon";
        static bool WarmBond(WorldState w) => w != null && w.DragonBond / 20 >= 3;

        public static string PonderLine(string ending) =>
            ending == Turn.End ? "The world is safe now. That's what everyone says. You find you can't say it out loud."
            : ending == Turn.Seal ? "She's asleep, and she isn't dreaming. Ashby promised. You hope he was right. You hope she isn't knocking."
            : "The fire is home, and so is she. Somewhere, a thousand years of kings are turning in their graves. Let them. Still, you wonder what the fire will want, now that it's free.";

        // ------------------------------------------------------------------ the scenes

        public static List<EndingScene> Scenes(string ending, WorldState w, IEnumerable<string> companionNames)
        {
            string d = DragonOf(w), D = Cap(d);
            var list = new List<EndingScene>();

            list.Add(new EndingScene
            {
                Title = "Her",
                Beats = ending == Turn.End ? new[]
                {
                    "When the smoke clears, there's nothing in the Great Square but ash, and in the ash, a small iron ring. The kind a child would wear on a string.",
                    $"{D} finds it first. It noses the ring out of the ashes and drops it at your feet, and then it sits down beside the place where she was, and it won't come away.",
                    "Nobody cheers. Somebody starts to, and stops.",
                }
                : ending == Turn.Seal ? new[]
                {
                    "Ashby sits down with his back against the grey door, the way you'd sit beside a sick friend, and doesn't get up for a long time.",
                    "\"She'll sleep,\" he says, mostly to himself. \"No dreams this time. I made sure. She won't know she's alone.\" He says it three times, as if counting.",
                    "On the other side of the door, nothing knocks. You listen for a long while to make sure.",
                }
                : new[]
                {
                    "She comes up the Roots stair holding Bran Tolley's hand, one step at a time, and stops at the top with her eyes screwed shut against the light.",
                    "\"Is that the sun?\" she whispers. Somebody tells her yes. She opens her eyes, very slowly, and looks at it for a long time, and doesn't say anything at all.",
                    "Then she sees the dragon, and runs.",
                },
            });

            list.Add(new EndingScene
            {
                Title = D,
                Beats = ending == Turn.End ? new[]
                {
                    WarmBond(w) ? $"{D} comes back to you in the end. It takes three days. It sleeps against your door every night after, as if making sure you don't go anywhere."
                                : $"{D} comes back to you in the end, but it's quieter now. It doesn't play with its food. It doesn't steal your boots.",
                    "It keeps the little iron ring in its hoard, right at the top, where it can see it.",
                }
                : ending == Turn.Seal ? new[]
                {
                    $"{D} sleeps outside the Undergate for three days and nights, and growls at anyone who comes too close to the stair.",
                    WarmBond(w) ? "On the fourth morning it comes and finds you, and pushes its head under your arm, and stays there." : "On the fourth morning it comes back up. It doesn't look at you for a while. Then, eventually, it does.",
                }
                : new[]
                {
                    $"{D} lets her ride on its back all the way round the Great Square, very slowly, with its wings held out like a canopy so she won't fall off.",
                    WarmBond(w) ? "Then it brings her back to you, and lies down with its head in your lap and its tail around her, and purrs like a kettle." : "Then it brings her back, and looks at you, very pleased with itself, as if the whole thing was its idea.",
                },
            });

            var names = (companionNames ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            if (names.Count > 0)
            {
                string[] T =
                {
                    "{0} says they always knew you'd get there in the end. They did not always know this.",
                    "{0} asks if it's over now. You say yes. They say, \"Good,\" and sleep for a day and a half.",
                    "{0} has been keeping a list of everything that tried to kill you. It's a long list. They read it out at the inn, and it gets a round of applause.",
                    "{0} is going home. They'll write. They actually do write, which surprises everyone.",
                    "{0} doesn't say much. They don't have to. They stand next to you a little longer than they need to.",
                    "{0} wants to know when the next adventure is. You tell them to give you a week. They give you four days.",
                };
                list.Add(new EndingScene { Title = CompanionsTitle, Beats = names.Select((n, i) => string.Format(T[i % T.Length], n)).ToArray() });
            }

            var leaders = new List<string>();
            if (TurnDb.MarshDefects(ending, w)) leaders.Add("Marsh pins the royal badge back on, upside down. \"So everyone remembers,\" she says. The Household Guard is hers now, and so, for the moment, is the city.");
            foreach (var f in Turn.Forces) leaders.Add(LeaderLine(f, TurnDb.Reaction(f, ending, w), ending));
            list.Add(new EndingScene { Title = "Aurelhold", Beats = leaders.ToArray() });

            list.Add(new EndingScene
            {
                Title = "The King",
                Beats = ending == Turn.End ? new[]
                {
                    "King Aldric keeps his throne. The city lets him. But everyone knows now what every King has known, and he can see it in every face that bows to him.",
                    "He doesn't light the lamps any more. There's nothing to light them with.",
                }
                : ending == Turn.Seal ? new[]
                {
                    "The lamps of Aurelhold are cold, and they stay cold. King Aldric walks the dark streets at night in a plain coat, and nobody bows.",
                    "Marsh's people keep him to the palace, mostly for his own sake. He asks, once, if the door is holding. Ashby tells him it's none of his business any more.",
                }
                : new[]
                {
                    "What's left of King Aldric is an old man without a crown. They try him in the Great Square, under the Sunstair, in front of everyone.",
                    "She asks, very seriously, if he's going to be all right. Nobody knows what to say to that, so she pats his hand, and he cries.",
                },
            });

            list.Add(new EndingScene
            {
                Title = "The Road Home",
                Zone = "town",
                Beats = new[]
                {
                    "The road south is still deep in snow, but the carriage knows the way. The Wyrmback falls behind you, peak by peak, until it's only a white line on the sky.",
                    ending == Turn.Save ? "Twelve of those peaks are glowing faintly, all the way round the ring, like embers in a grate. Nobody in the carriage says anything about it. Everyone keeps looking."
                        : "Somebody falls asleep on your shoulder. Somebody else snores. You watch the hills go by and don't think about anything at all, for a whole afternoon.",
                },
            });

            var home = new List<string>
            {
                "Thistledown has strung up lanterns across the square, and every single one of them is lit with ordinary fire.",
                "Brannoc Ironkettle has painted a new sign over the shop: OUTFITTERS TO HEROES (SOME RESTRICTIONS APPLY). He's very proud of it. Mother Elowen blesses you all on the temple steps, and doesn't ask a single question, which you'll love her for later.",
                "Pip Thistlewhit buys everyone a round on the house, and nails a little brass plaque above your usual table. It's spelled wrong. It's perfect.",
            };
            if (w != null && w.Hearts("rhoswen") >= 8)
                home.Add("Captain Rhoswen Vale walks straight through the crowd, takes your face in both hands, and kisses you in front of the whole town. The Watch pretends not to see. The Watch is very bad at pretending.");
            else
                home.Add("Captain Rhoswen Vale gives you a salute, and then, when she thinks nobody's looking, a hug.");
            home.Add(w != null && w.Flag("snik_friend")
                ? "Snik and the kobolds have come up from the mine with a present: a very large, very lumpy statue of a dragon. It might be the dragon. It might be you. Snik won't say."
                : "Someone has nailed a drawing to the notice board: a dragon, and some people, done in charcoal by a kobold. It's signed with a paw print.");
            home.Add($"And then a black-and-white blur comes skidding across the square: Buoy, who has decided this is the best day of his life. He doesn't stop wagging for an hour. {D} lets him sleep between its paws.");
            list.Add(new EndingScene { Title = ThistledownTitle, Zone = "town", Beats = home.ToArray() });
            return list;
        }

        static string LeaderLine(string force, Stance s, string ending)
        {
            switch (force)
            {
                case "crown":
                    return s == Stance.With ? "The Crown's knights escort you down the Sunstair with banners flying. They saluted the right side today, they tell each other. They keep telling each other."
                        : "The guards who stayed with the King lay their swords on the Sunstair, one by one. Some of them look relieved.";
                case "academy":
                    return s == Stance.With ? "Ashby shakes your hand for a long time. \"I was wrong about a great many things,\" he says. \"Not this. I think. Ask me in forty years.\""
                        : s == Stance.Aside ? "Ashby goes back to the Academy without saying goodbye. He sends a letter, later. It's eleven pages long, and the last page just says \"thank you\"."
                        : "Ashby comes to see you before he leaves. He doesn't apologise, and neither do you. You shake hands anyway. He's still a good man. That's the worst part.";
                case "fae":
                    return s == Stance.With ? "Sorrel kisses both your cheeks, and every flower in Aurelhold opens at once, in the snow. \"The Courts remember,\" she says. You're not sure that's a comfort."
                        : s == Stance.Aside ? "Sorrel shrugs, smiles, and is simply not there any more. The Courts watched. The Courts will remember. The Courts always do."
                        : "Sorrel leaves without a word. In the morning, every lock in the city has been changed to a very small, very rude riddle.";
                default:
                    return s == Stance.With ? "Calloway buys you a drink and doesn't make a speech, which for Calloway is a speech. The Wyrmwatch rings its bells for you, all night."
                        : s == Stance.Aside ? "Calloway nods to you from across the inn, once, and goes back to his ale. From Calloway, that's practically a song."
                        : "Calloway's crossbows go back on the rack. He doesn't look at you on the way out. \"You'd better be right,\" he says, to nobody.";
            }
        }

        // ------------------------------------------------------------------ the slides

        public static string ForceTitle(string force) => force == "crown" ? "The Crown" : force == "academy" ? "Starfall Academy" : force == "fae" ? "The Fae Courts" : "The Wyrmwatch";

        /// <summary>The stance a force's slide is written for (tests: it must match the reaction table).</summary>
        public static Stance ForceSlideStance(string force, string ending, WorldState w) => TurnDb.Reaction(force, ending, w);

        static string ForceSlide(string force, Stance s, string ending, WorldState w)
        {
            switch (force)
            {
                case "crown":
                    if (s == Stance.With) return "The Crown kept its throne and lost its secret. The statues in the Great Square were taken down, quietly, at night. Nobody has decided what to put up instead.";
                    return TurnDb.MarshDefects(ending, w)
                        ? "Marsh ran Aurelhold for a year and a day, and then handed it to a council of bakers, lamplighters and miners, and went home to sleep. The Crown is a word in the old annals now."
                        : "The Crown fell the way old trees do: slowly, and then all at once. Aurelhold argues about what comes next. It argues very loudly. It's never been so alive.";
                case "academy":
                    return s == Stance.With ? "Starfall Academy rewrote its annals, every altered page of them, and added a new course: Wards, Seals, and When Not To Use Them. Ashby teaches it. It's always full."
                        : s == Stance.Aside ? "Starfall Academy closed its doors for a season, to think. When they opened again the Restricted Section had a new rule carved over the door: ASK WHO IS ON THE OTHER SIDE."
                        : "Starfall Academy never quite forgave you, and never quite stopped studying what you did. There's a seminar on it now. You're spelled wrong in the reading list.";
                case "fae":
                    return s == Stance.With ? "The Fae Courts call the old Pact mended. Spring comes early in Sundermere, every year now, and the frosts are very polite about leaving."
                        : s == Stance.Aside ? "The Fae Courts watched, and weighed, and wrote it all down in a book that nobody mortal will ever read. The frosts come and go as they please."
                        : "The Fae Courts closed their roads for a hundred years. Some nights, from the Gloaming Glade, you can hear them arguing about you.";
                default:
                    return s == Stance.With ? "The Wyrmwatch rang its bells across the valley for a week, and then, for the first time in a thousand years, unstrung its crossbows."
                        : s == Stance.Aside ? "The Wyrmwatch went back to Cragfoot and its goats. Calloway still keeps one bolt in the rack, polished, just in case. He says it's sentimental."
                        : "The Wyrmwatch took down your name from the inn wall at Cragfoot. Somebody put it back up the next night. Nobody owns up to it.";
            }
        }

        public static List<EpilogueSlide> Slides(string ending, WorldState w, int day)
        {
            string d = DragonOf(w), D = Cap(d);
            var list = new List<EpilogueSlide>
            {
                new EpilogueSlide { Title = "Sundermere", Text = ending == Turn.End
                    ? "The fire is gone from the world, and the blight with it. So is the warmth. The winters are longer now, and harder, and nobody can quite say why."
                    : ending == Turn.Seal
                    ? "The blight lifted, slowly, like a fog. A little of the cold stayed behind. On still nights the old folk say they can feel the mountains holding their breath."
                    : "The Great Ones glow faintly in their ring, and winter broke early that year, all at once, in a single warm night. The farmers called it a miracle. The farmers were right." },
                new EpilogueSlide { Title = "Aurelhold", Text = ending == Turn.End
                    ? "Aurelhold is cold now, like everywhere else. It turns out to be a perfectly nice city when it's cold. People wear more scarves. They talk to their neighbours more."
                    : ending == Turn.Seal
                    ? "The ever-lamps never lit again. The city learned to burn wood and coal like everybody else, and complained about it, and was fine."
                    : "The ever-lamps went out, and the city lit candles instead, thousands of them, the night she came up into the sun. They still do it every year. They call it the Waking." },
            };
            foreach (var f in Turn.Forces) list.Add(new EpilogueSlide { Title = ForceTitle(f), Text = ForceSlide(f, ForceSlideStance(f, ending, w), ending, w) });
            list.Add(new EpilogueSlide { Title = D, Text = ending == Turn.End
                ? $"{D} grew, as dragons do. It kept a hoard: coins, buttons, one of your boots, and a small iron ring, right at the top."
                : ending == Turn.Seal
                ? $"{D} grew, as dragons do. Once a year it goes down to the Undergate and sits outside it for a day, and nobody, not even you, is allowed to come."
                : $"{D} grew, as dragons do, and taught her to fly, which was nobody's idea, and which everyone found out about at the same time." });
            list.Add(new EpilogueSlide { Title = "Her", Text = ending == Turn.End
                ? "She is a story again, the way she was before. Only now the story has a little girl in it, and people tell it more quietly."
                : ending == Turn.Seal
                ? "Behind a grey door under the mountains, a little girl is asleep. She isn't dreaming. Ashby goes down to check, every year, until he can't manage the stairs."
                : "Her name is Edda. She's learning to read, and is terrible at it, and doesn't care. She has a hundred friends now, and twelve of them are mountains." });
            list.Add(new EpilogueSlide { Title = "Thistledown", Text = "Thistledown is still Thistledown. The Sleeping Drake still has a plaque with your names spelled wrong. Buoy still meets every carriage, just in case it's you." });
            list.Add(new EpilogueSlide { Title = "You", Text = $"It was {day} {(day == 1 ? "day" : "days")} from the night the star fell to the end of it all. You don't count days any more. You find you don't need to." });
            list.Add(new EpilogueSlide { Title = "Afterwards", Text = PonderLine(ending) });
            return list;
        }
    }
}
