using System.Collections.Generic;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// All conversations. Lore names are PLACEHOLDERS pending approval (docs/LORE_PITCH.md).
    /// Role-gated options use Show = class/background/species checks.
    /// </summary>
    public static partial class DialogueDb
    {
        static readonly Dictionary<string, Dialogue> Cache = new Dictionary<string, Dialogue>();

        public static Dialogue Get(string id)
        {
            if (Cache.TryGetValue(id, out var d)) return d;
            switch (id)
            {
                case "brannoc": d = Brannoc(); break;
                case "elowen": d = Elowen(); break;
                case "pip": d = Pip(); break;
                case "rhoswen": d = Rhoswen(); break;
                case "tobin": d = Tobin(); break;
                case "maddie": d = Maddie(); break;
                case "snik": d = Snik(); break;
                case "wren": d = Wren(); break;
                case "hobb": d = Hobb(); break;
                case "garrick": d = Garrick(); break;
                case "hesper": d = Hesper(); break;
                case "dunstan": d = Dunstan(); break;
                case "lark": d = Lark(); break;
                case "tamsin": d = Tamsin(); break;
                case "wick": d = Wick(); break;
                case "thimble": d = Thimble(); break;
                case "recruit": d = Recruit(); break;
                case "companion": d = Companion(); break;
                default: d = new Dialogue(id, c => "start"); d.Node("start", null, c => "...").Bye(); break;
            }
            Cache[id] = d;
            return d;
        }

        static string Sir(DCtx c) => c.PC.Name;

        // =================================================================== Brannoc (shop)

        static Dialogue Brannoc()
        {
            var d = new Dialogue("brannoc", c => c.Npc.Actor.Zone.Def.Id == "shop" ? "shop" : "offhours");
            const string who = "Brannoc Ironkettle";

            d.Node("shop", who,
                c => c.G.World.KettleReturned
                    ? $"Ah, {Sir(c)}! My kettle-saving hero. What'll it be?"
                    : "Ironkettle General Goods. Everything you need, nothing you don't, and absolutely no haggling.",
                c => "A stout dwarf with a steel-grey beard braided into three precise plaits. A pencil is tucked behind one ear.")
                .Say("Let me see what you've got.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true)
                .Roll(Skill.Persuasion, 14, "Surely a dwarf of your refinement offers a discount to new adventurers?", "disc_yes", "disc_no",
                      onSuccess: c => c.G.World.SetShopModifier(c.P.Index, "general", -10, c.G.Clock.Day),
                      onCritSuccess: c => c.G.World.SetShopModifier(c.P.Index, "general", -20, c.G.Clock.Day),
                      critFailure: "disc_crit_fail", onCritFailure: c => c.G.World.SetShopModifier(c.P.Index, "general", 10, c.G.Clock.Day))
                .Roll(Skill.Intimidation, 16, "Lower your prices. Or else.", "intim_yes", "intim_no",
                      onSuccess: c => { c.G.World.SetShopModifier(c.P.Index, "general", -15, c.G.Clock.Day); c.G.World.AddFriendship("brannoc", -30); })
                .Roll(Skill.Deception, 15, "I'm actually a buyer for the Royal Quartermaster. Bulk orders. Very lucrative.", "decep_yes", "decep_no",
                      onSuccess: c => c.G.World.SetShopModifier(c.P.Index, "general", -15, c.G.Clock.Day),
                      show: c => c.Bg("criminal") || c.Is("bard"), role: "Silver tongue")
                .Roll(Skill.Performance, 12, "Perhaps a little song for the shopkeep?", "perf_yes", "perf_no",
                      onSuccess: c => c.G.World.SetShopModifier(c.P.Index, "general", -10, c.G.Clock.Day), show: c => c.Is("bard"), role: "Bard")
                .Roll(Skill.Insight, 12, "You seem distracted. Something on your mind?", "insight_yes", "insight_no", social: false)
                .Say("I found your copper kettle.", "kettle", doIt: c => c.G.ReturnKettle(c.P), show: c => c.G.HasQuestItem(c.P, "copper_kettle"))
                .Say("Did you see the falling star last night?", "star")
                .Say("How's the poetry going?", "poem_ask", show: c => c.G.Quests.Stage("secret_poems") == 0 && (c.G.World.Flag("brannoc_poet") || c.G.World.Hearts("brannoc") >= 2))
                .Bye();

            d.Node("poem_ask", who, c => "Shh! Not so loud. ...I wrote one. For someone. At the temple. I can't give it to her myself, I'd melt into my boots. Would you... deliver it? Anonymously. VERY anonymously.",
                c => "Brannoc produces a folded paper from his apron, sealed with wax and a thumbprint the size of a coin. His beard is quivering.",
                c => { c.G.GiveItem(c.P, "brannoc_poem"); c.G.Quests.Start("secret_poems", c.P); })
                .Say("Your secret's safe with me.", "shop");

            d.Node("disc_yes", who, c => c.LastOutcome == CheckOutcome.CriticalSuccess
                    ? "...Refinement. Hah! Nobody's called me refined since my mother. Twenty percent, today only, and you tell NO ONE."
                    : "Hmph. Refinement, is it. ...Fine. Ten percent. Today only. Don't spread it around.",
                c => "Brannoc's ears go faintly pink above his beard.")
                .Say("Let's see the wares.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true).Bye("Thanks, Brannoc.");
            d.Node("disc_no", who, c => "Nice try. Prices are prices. I don't make the rules. Well, I do. And the rule is 'no.'")
                .Say("Fair enough. Show me the goods.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true).Bye();
            d.Node("disc_crit_fail", who, c => "...Did you just break my counter?",
                c => "You lean on the counter with tremendous confidence. It creaks, then groans, then drops an inch. Brannoc stares at it. Then at you. Prices have just gone UP ten percent.")
                .Say("I'll... just look at the wares.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true).Bye("I'll see myself out.");
            d.Node("intim_yes", who, c => "...Fifteen percent. And then you leave. And then you think about what kind of person you are.",
                c => "He doesn't flinch, exactly. But his hand drifts away from the hammer under the counter.")
                .Say("Show me the goods.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true).Bye();
            d.Node("intim_no", who, c => "Or else WHAT? I've been threatened by a WYVERN, child. A wyvern. Prices stay where they are.",
                c => "Brannoc leans forward. He is much, much sturdier than he looked a moment ago.").Say("...Okay.", "shop");
            d.Node("decep_yes", who, c => "The Royal Quartermaster! Well why didn't you say so! Fifteen percent for the Crown, of course, of course.",
                c => "He straightens his apron and stands a little taller.")
                .Say("Excellent. The wares, please.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true);
            d.Node("decep_no", who, c => "And I'm secretly the Queen of the Fae. Prices are prices.").Say("Worth a try.", "shop");
            d.Node("perf_yes", who, c => "...That was lovely. Truly. I, er. I write a bit of poetry myself, you know. Ten percent. For the arts.",
                c => "Your song fills the little shop. By the last verse, Brannoc is pretending very hard that something is in his eye.")
                .Say("You write poetry?", "poetry").Say("The wares, then.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true);
            d.Node("poetry", who, c => "Nothing much. Odes. To ore. 'O Copper, Thou Humble Metal'... Don't laugh.", onEnter: c => { c.G.World.AddFriendship("brannoc", 25); c.G.World.SetFlag("brannoc_poet"); })
                .Say("I would never laugh.", "shop");
            d.Node("perf_no", who, c => "That's a lute, is it? I thought something was dying.").Say("Everyone's a critic.", "shop");
            d.Node("insight_yes", who, c => "...My kettle. My grandmother's copper kettle. Gone two nights ago, right off the windowsill. Little scaly footprints all over the sill.",
                c => "Behind the gruffness, he's genuinely upset. You notice a pale ring of clean wood on the windowsill where something sat for a long time.",
                c => c.G.Quests.Start("kettle"))
                .Say("I'll keep an eye out for it.", "shop", c => c.G.World.AddFriendship("brannoc", 15));
            d.Node("insight_no", who, c => "Mind? My mind is on my ledger. Are you buying something?").Say("Right. Sorry.", "shop");
            d.Node("kettle", who, c => "MY KETTLE! You found it! Where-- no, don't tell me. Kobolds, I'll bet. Listen: from now on, you get ten percent off. Permanently. And that's a dwarf's word.",
                c => "Brannoc cradles the kettle like a newborn.").Say("Happy to help.", "shop");
            d.Node("star", who, c => "Saw it. Biggest one I ever saw, and I've lived two hundred years. Came down north, by the old mine. Then the singing started. Little voices, at night, from up the mountain.")
                .Say("Singing?", "star2").Say("Thanks.", "shop");
            d.Node("star2", who, c => "Chanting, more like. My grandad used to say the Wyrmbacks are sleeping dragons. Load of rubbish, of course. ...Of course.").Say("Of course.", "shop");

            d.Node("offhours", who, c => c.G.Clock.WeekdayIndex == 6
                    ? "It's Hearthday. Shop's closed. Even dwarves rest. Occasionally."
                    : "Shop's closed. Come back at eight. I'm busy.",
                c => $"Brannoc is {c.Npc.Activity}.")
                .Roll(Skill.Persuasion, 13, "Just one quick purchase? Please?", "after_yes", "after_no",
                      onSuccess: c => { })
                .Say("Did you see the falling star?", "star")
                .Bye("Sorry to bother you.");
            d.Node("after_yes", who, c => "...Fine. FINE. One quick look. Don't tell Pip I'm a pushover.")
                .Say("Thank you!", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "general"); }, end: true);
            d.Node("after_no", who, c => "No. Eight o'clock. The shop has HOURS for a REASON.").Bye("Fair.");
            return d;
        }

        // =================================================================== Mother Elowen (temple)

        static Dialogue Elowen()
        {
            var d = new Dialogue("elowen", c => "start");
            const string who = "Mother Elowen";
            d.Node("herbs_ask", who, c => "Bless you. My remedy shelf is nearly bare. I need three herbs: moonpetal from Whisperwood, riverleaf from the bank at Miller's Ford, and hearthroot from the South Lane green. Gently, mind. Roots and all.",
                c => "She sketches each plant on a scrap of paper with surprising skill.",
                c => c.G.Quests.Start("elowens_herbs", c.P)).Say("Roots and all. Got it.", "start");
            d.Node("herbs_done", who, c => "Oh, beautifully picked! You've a healer's hands. Here: take the first remedy from the new batch.").Say("Thank you, Mother.", "start");
            d.Node("poem_read", who, c => "\"Thou art the dawn upon my anvil'd heart...\" Oh. Oh, my. Who... who wrote this?",
                c => "She reads it twice. The second time her lips move. By the end she is pressing it flat against her chest.")
                .Roll(Skill.Insight, 13, "(Read her) How does she really feel about it?", "poem_insight", "poem_insight_no", social: false)
                .Say("It's from Brannoc. He's been writing them for years.", "poem_revealed", doIt: c =>
                {
                    c.G.TakeQuestItem(c.P, "brannoc_poem"); // handed over only now: closing the window early keeps the poem
                    c.G.World.SetFlag("poems_revealed");
                    c.G.World.AddFriendship("brannoc", 100);
                    c.G.World.AddFriendship("elowen", 100);
                    c.G.Quests.Set("secret_poems", 1 + 1, c.P);
                })
                .Say("I promised not to say.", "poem_secret", doIt: c => { c.G.TakeQuestItem(c.P, "brannoc_poem"); c.G.World.AddFriendship("brannoc", 150); c.G.Quests.Set("secret_poems", 2, c.P); });
            d.Node("poem_insight", who, c => "...",
                c => "She is trying very hard to look serene and is failing completely. She is delighted. She has, you suspect, been hoping for exactly this for a long time.")
                .Say("(Back)", "poem_read");
            d.Node("poem_insight_no", who, c => "...", c => "Her face is calm as a pond. Whatever she feels, the Dawnmother is keeping it for her.").Say("(Back)", "poem_read");
            d.Node("poem_revealed", who, c => "Brannoc? BRANNOC IRONKETTLE? That grumpy, wonderful... Excuse me. I need to go and stand in the garden for a while. Thank you, my dear. Truly.").Bye("Of course.");
            d.Node("poem_secret", who, c => "A secret admirer. At my age! Well. I shall pin it by the altar, where the dawn can read it too. Thank you for carrying it.").Bye("My pleasure.");
            d.Node("start", who, c => c.G.AnyDeadAlly(c.P)
                    ? "Oh, my dear. Bring them here. The Dawnmother is not done with them yet."
                    : $"Light keep you, {Sir(c)}. What brings you to Aurelith's house?",
                c => "An older woman with laugh lines, flour on her sleeves and an air of absolute calm.")
                .Say("I'd like to see what the temple offers.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "temple"); }, end: true)
                .Say("Can I help with anything around the temple?", "herbs_ask", show: c => c.G.Quests.Stage("elowens_herbs") == 0)
                .Say("I've brought your herbs.", "herbs_done", show: c => c.G.Quests.Stage("elowens_herbs") == 2
                        && c.G.HasQuestItem(c.P, "herb_moonpetal") && c.G.HasQuestItem(c.P, "herb_riverleaf") && c.G.HasQuestItem(c.P, "herb_hearthroot"),
                      doIt: c =>
                      {
                          foreach (var h in new[] { "herb_moonpetal", "herb_riverleaf", "herb_hearthroot" }) c.G.TakeQuestItem(c.P, h);
                          c.G.Quests.Set("elowens_herbs", 3, c.P);
                      })
                .Say("Someone asked me to give you this.", "poem_read", show: c => c.G.Quests.Active("secret_poems") && c.G.HasQuestItem(c.P, "brannoc_poem"))
                .Say("Please, bring back my friend.", "revive", show: c => c.G.AnyDeadAlly(c.P))
                .Say("Could I have a blessing?", "blessing", show: c => !c.PC.HasEffect("guidance"))
                .Roll(Skill.Religion, 12, "Tell me about the Dawnmother and the old days.", "lore_yes", "lore_no", social: false)
                .Say("Aurelith's light upon you, Mother.", "acolyte", show: c => c.Bg("acolyte") || c.Is("cleric"), role: c_role(), doIt: c => c.G.World.AddFriendship("elowen", 15))
                .Say("Can I rest here?", "rest")
                .Bye();

            d.Node("revive", who, c => c.G.Settings.Difficulty <= Difficulty.Normal
                    ? "No payment, child. Just a promise to look after each other better. Stand back."
                    : "The rite asks for an offering: 100 gold, for the diamond dust. Then stand back.",
                c => "She kneels, lays a sunburst over the fallen one's heart, and begins to sing.")
                .Say("Thank you.", doIt: c => c.G.TempleRevive(c.P), end: true);
            d.Node("blessing", who, c => "Go with light. And try not to need me later.",
                c => "She touches your forehead with a warm thumb. (Your next ability check gets +1d4.)",
                c => c.PC.AddEffect(EffectLibrary.Create("guidance", c.PC))).Say("Thank you, Mother.", "start");
            d.Node("lore_yes", who, c => "Long ago, dragons, fae and mortals kept a pact: the Ember Pact. Dragonfire was the root of all magic. When the Pact broke, the dragons slept and the magic dimmed. My sisters keep embers in the vaults, coals that remember the fire. Some say they can call a soul back, if you're quick.",
                c => "Her eyes drift north, toward the mountains.").Say("And the falling star?", "lore2").Say("Thank you.", "start");
            d.Node("lore2", who, c => "If I were a betting woman, and Fennick knows I am, I'd say it was no star. I'd say something is waking up.").Say("...", "start");
            d.Node("lore_no", who, c => "Ah, that's a long story, and the bread is in the oven. Another time, dear.").Say("Of course.", "start");
            d.Node("acolyte", who, c => c.Hearts >= 2 && !c.G.World.Flag("charm_given")
                    ? "You've the Dawnmother's warmth about you. Here, take this. I give them to the ones I'd like to see again."
                    : "And upon you, little sibling. It does an old heart good to hear the words.",
                onEnter: c => { if (c.Hearts >= 2 && !c.G.World.Flag("charm_given")) { c.G.World.SetFlag("charm_given"); c.G.GiveItem(c.P, "dawnmothers_charm"); } })
                .Say("Thank you.", "start");
            d.Node("rest", who, c => "The pews are hard but the peace is free. Sleep as long as you need.")
                .Say("(Long rest here)", doIt: c => { c.Screen.Close(); c.P.Hud.Push(new RestScreen(true)); }, end: true)
                .Say("Not yet.", "start");
            return d;
        }

        static string c_role() => "Faithful";

        // =================================================================== Pip (tavern)

        static Dialogue Pip()
        {
            var d = new Dialogue("pip", c => "start");
            const string who = "Pip Thistlewhit";
            d.Node("start", who, c => "Welcome to the Sleeping Drake! Stew's hot, ale's cold, gossip's free. What'll it be?",
                c => "A gnome standing on a crate behind the bar, sleeves rolled up, polishing a tankard that is already clean.")
                .Say("I'd like a room for the night. (5 sp, long rest)", doIt: c => c.G.RentRoom(c.P), end: true)
                .Say("A bowl of stew, please. (2 sp)", "stew", show: c => c.PC.Copper >= 20, doIt: c => { c.PC.Copper -= 20; })
                .Say("Anyone here looking for work?", "recruits")
                .Say("Heard any gossip?", "gossip")
                .Roll(Skill.Performance, 10, "Mind if I play for the crowd?", "perf_yes", "perf_no", show: c => c.Is("bard"), role: "Bard",
                      onSuccess: c => c.G.GiveCopper(c.P, 50 + c.LastRoll.Total * 10), critSuccess: "perf_crit", onCritSuccess: c => c.G.GiveCopper(c.P, 500))
                .Roll(Skill.Persuasion, 12, "First drink's on the house, right?", "drink_yes", "drink_no")
                .Say("Shop for supplies.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "tavern"); }, end: true)
                .Say("You look upset, Pip. What's wrong?", "barrel", show: c => c.G.Quests.Stage("pips_barrel") == 0)
                .Say("Who are the rowdy lot in the corner?", "brawl_ask", show: c => c.G.Quests.Stage("bar_brawl") == 0 && c.G.Clock.WeekdayIndex == WorldDb.Coinday && c.G.Clock.Hour >= 18)
                .Roll(Skill.Persuasion, 13, "(Persuade) Give Wick a job? He's a good kid in a bad spot.", "wick_job", "wick_nojob",
                      show: c => c.G.Quests.Stage("pips_barrel") == 3 && c.G.World.Flag("wick_mercy"),
                      adv: c => AdvantageState.Advantage,
                      onSuccess: c => { c.G.World.SetFlag("wick_hired"); c.G.World.SetFlag("pip_free_room"); c.G.World.AddFriendship("pip", 100); c.G.Quests.Set("pips_barrel", 4, c.P); },
                      onFailure: c => c.G.Quests.Set("pips_barrel", 4, c.P))
                .Say("Wick took it. He's cooling his heels at the watch house.", "wick_jailed", show: c => c.G.Quests.Stage("pips_barrel") == 3 && c.G.World.Flag("wick_justice"),
                      doIt: c => c.G.Quests.Set("pips_barrel", 4, c.P))
                .Say("I'll pay for the barrel myself. (10 gp)", "barrel_paid", show: c => c.G.Quests.Active("pips_barrel") && c.PC.Copper >= 1000,
                      doIt: c => { c.PC.Copper -= 1000; c.G.World.SetFlag("wick_caught"); c.G.Quests.Set("pips_barrel", 4, c.P); })
                .Bye();
            d.Node("brawl_ask", who, c => "Farmhands. Payday. They've had three ciders each and decided they're the toughest folk in the valley. They're asking for a challenger. Fists only, mind! No magic in my bar, and if anyone DIES I'm banning everybody.",
                c => "In the corner, three broad farmhands are arm-wrestling the furniture. One of them waves at you, grinning, and cracks his knuckles.")
                .Say("(Roll up your sleeves) All right. Who's first?", end: true, doIt: c => { c.G.Quests.Start("bar_brawl", c.P); c.G.StartBrawlSoon(c.P); })
                .Say("Not tonight.", "start");
            d.Node("barrel", who, c => "Someone pinched a whole barrel of Hearthfire Stout from behind the Drake! A WHOLE barrel. Do you know how hard those are to roll? Whoever it is has legs of iron and no shame.",
                c => "Pip is polishing the same tankard so hard it squeaks.",
                c => c.G.Quests.Start("pips_barrel", c.P)).Say("I'll look around out back.", "start");
            d.Node("wick_job", who, c => "...His mum's sick? Oh, the poor lamb. Fine. FINE. He can sweep and stack and roll barrels, since he's so good at it. And YOU get a room on the house next time.",
                c => "Pip sniffs, very hard, and pretends it's the onions.").Say("Thanks, Pip.", "start");
            d.Node("wick_nojob", who, c => "I'm not running a charity, love. But... tell him I won't press it. And tell him to bring the barrel back. Today.").Say("Fair enough.", "start");
            d.Node("wick_jailed", who, c => "The watch house? Well. I suppose that's justice. ...Is he all right in there? Does he have a blanket? Never mind. Thank you.").Say("Glad to help.", "start");
            d.Node("barrel_paid", who, c => "You're paying for it? Well I never. Whoever took it owes YOU a drink, then. Not from my stout.").Say("Fair enough.", "start");
            d.Node("stew", who, c => "One bowl of Pip's Famous! It's famous because I tell everyone it's famous.",
                c => "It's genuinely excellent. Warmth spreads through you. (You regain 1d4 HP.)",
                c => { int h = c.PC.Heal(Dice.Roll(4)); c.P.Avatar.Popup("+" + h, UIKit.HealGreen); }).Say("Delicious.", "start");
            d.Node("recruits", who, c => c.G.World.TavernRecruitsHere(c.G).Count > 0
                    ? "Aye, a couple of sell-swords drifted in this week. Go introduce yourself. Mind your purse."
                    : "Not this week, love. Check back in a few days. Adventurers drift in like weather.").Say("Thanks.", "start");
            d.Node("gossip", who, c => GossipLine(c)).Say("Anything else?", "gossip").Say("Thanks, Pip.", "start");
            d.Node("perf_yes", who, c => "Hah! Not bad! The crowd liked it. Here's your cut of the tips.",
                c => "A few coins clatter into your hat. Someone in the back cheers.").Say("Thanks!", "start");
            d.Node("perf_crit", who, c => "BY FENNICK'S BEARD. Do that every night and I'll give you a room for free!",
                c => "The whole tavern is on its feet. Old Tobin is crying. A dwarf throws a full purse at you.").Say("Thank you, thank you.", "start");
            d.Node("perf_no", who, c => "...Maybe stick to adventuring, love.", c => "Someone throws a bread roll. It's not a compliment.").Say("Ouch.", "start");
            d.Node("drink_yes", who, c => "Oh, go on then. Cheers!", c => "You get a frothing tankard of Sleeping Drake Dark. Life is good.").Say("Cheers!", "start");
            d.Node("drink_no", who, c => "First drink's the same price as all the others, love. Nice try.").Say("Worth a shot.", "start");
            return d;
        }

        static string GossipLine(DCtx c)
        {
            string[] lines =
            {
                "Old Tobin swears a 'tiny lizard man in a cape' stole his lucky spoon. He's told the story four times tonight. The cape gets bigger every time.",
                "Brannoc's been in a foul mood since his grandmother's kettle went missing. Don't mention kettles.",
                "Captain Vale hasn't slept in two days. Something about the old mine. Be a dear and help her, would you?",
                "They say the Wyrmback Mountains are sleeping dragons. My gran said if you listen at night, you can hear them breathing.",
                "A traveller from the capital said the Starfall Academy sent scholars north. Looking for 'a significant arcane event.' Fancy.",
            };
            return lines[(c.G.Clock.Day + Random.Range(0, lines.Length)) % lines.Length];
        }

        // =================================================================== Captain Rhoswen Vale (main quest)

        static Dialogue Rhoswen()
        {
            var d = new Dialogue("rhoswen", c =>
            {
                int s = c.G.Quests.Stage("fallen_star");
                if (s == 0) return "intro";
                if (c.G.World.EggHatched && s < FallenStar.Done) return "dragon";
                if (s >= FallenStar.Done) return "after";
                return "progress";
            });
            const string who = "Captain Rhoswen Vale";
            d.Node("jobs", who, c => "Anything? Ha. Hesper Meadowsweet and Dunstan Burrow are at war over a FENCE on the South Lane. Last week a bee was involved. Could you talk sense into them before I have to arrest two beekeepers?",
                c => "She pulls a crumpled list from her belt. It is long. Several items are underlined three times.",
                c => { c.G.Quests.Start("bad_blood", c.P); c.G.Quests.Start("wolf_bounty", c.P); })
                .Say("I'll see what I can do.", end: true);
            d.Node("wolves_paid", who, c => "The wolves? Ha! Good. Bounty's fifteen gold. Don't let my uncle see me hand it over, he thinks the watch budget is for biscuits.").Bye("Pleasure doing business.");
            d.Node("intro", who, c => "You there! You look... capable. Something fell out of the sky last night, in the hills past Whisperwood's north trail. Since then: four chickens gone, Brannoc's kettle, and Tobin's spoon. My entire watch is four people, and one of them is my uncle.",
                c => "A half-elf in a dented breastplate, dark circles under her eyes, hand resting on a well-used sword.")
                .Say("I'll look into it.", "accept", c => c.G.Quests.Start("fallen_star"))
                .Roll(Skill.Insight, 11, "(Read her) You're more worried than you're letting on.", "insight_yes", "insight_no", social: false)
                .Roll(Skill.Persuasion, 13, "What's it pay?", "pay_yes", "pay_no", onSuccess: c => c.G.GiveCopper(c.P, 1000))
                .Bye("Not my problem.");
            d.Node("insight_yes", who, c => "...Fine. Yes. The singing. Every night, from the mine. And the falling star was GREEN at the end, like dragonfire in the old stories. I don't want to be the captain who ignored the end of the world.",
                c => "Her shoulders drop half an inch. She's been carrying this alone.", c => c.G.World.AddFriendship("rhoswen", 20))
                .Say("You're not alone now. I'll go.", "accept", c => c.G.Quests.Start("fallen_star"));
            d.Node("insight_no", who, c => "Worried? I'm a captain. Captains don't worry, we... strategize.").Say("Right. I'll look into it.", "accept", c => c.G.Quests.Start("fallen_star")).Bye("Good luck with that.");
            d.Node("pay_yes", who, c => "Ugh. Fine. Ten gold now, more when it's done. Don't spend it all at the Drake.").Say("Deal.", "accept", c => c.G.Quests.Start("fallen_star"));
            d.Node("pay_no", who, c => "It pays in the gratitude of a grateful village. And my undying respect. That's all I've got.").Say("...Fine. I'll do it.", "accept", c => c.G.Quests.Start("fallen_star"));
            d.Node("accept", who, c => "Thank you. Truly. Take the north trail through Whisperwood and keep climbing; you can't miss the scorch marks. Bring a light in case it leads somewhere dark. And {name}? Come back in one piece.".Replace("{name}", Sir(c)))
                .Say("Do you ever get a night off, Captain?", "flirt", show: c => c.Hearts >= 1)
                .Bye("I'll be back.");
            d.Node("progress", who, c => "Any luck? Whatever came down out there, the singing from the hills has been at it all night again.")
                .Roll(Skill.Persuasion, 14, "You know, you could come with me. Strictly for strategy.", "flirt_yes", "flirt_no", show: c => c.Hearts >= 2, role: "Charm")
                .Bye("Working on it.");
            d.Node("flirt", who, c => "A night off? I've heard of those. Like unicorns. Ask me again when the village stops being on fire.",
                c => "Was that... the hint of a smile?", c => c.G.World.AddFriendship("rhoswen", 10)).Bye("I'll hold you to that.");
            d.Node("flirt_yes", who, c => "...Strictly for strategy. Of course. I'll think about it. Over dinner. Which is also strategy.",
                c => "Captain Vale turns a remarkable shade of pink and becomes very interested in the notice board.").Bye("It's a date. A strategic one.");
            d.Node("flirt_no", who, c => "Ha. Nice try. Mine. Monsters. Go.").Bye("Worth a shot.");
            d.Node("dragon", who, c => $"Is that-- is that a DRAGON? A baby-- {Sir(c)}, why is there a DRAGON following you?",
                c => $"{c.G.World.DragonName} chirps at the captain and sneezes a tiny spark onto her boot. Rhoswen sits down very suddenly on a barrel.")
                .Say("It hatched from the 'star.' The kobolds were guarding it.", "dragon2");
            d.Node("dragon2", who, c => "...Right. Okay. The chickens make sense now. Here, your pay, you earned every copper. And... I think the capital is going to want to hear about this. I'm not sure that's a good thing.",
                onEnter: c => c.G.CompleteFallenStar(c.P)).Bye("We'll handle it. Together.");
            d.Node("after", who, c => $"Morning, {Sir(c)}. How's the little one? Still setting things on fire?")
                .Say("Do you ever get a night off, Captain?", "flirt")
                .Bye("Take care, Captain.");
            foreach (var id in new[] { "intro", "progress", "dragon", "after" })
                if (d.Nodes.TryGetValue(id, out var n))
                {
                    n.Options.Insert(System.Math.Max(0, n.Options.Count - 1),
                        new DOption { Text = "Anything need doing around town?", Next = "jobs", Show = c => c.G.Quests.Stage("bad_blood") == 0 || c.G.Quests.Stage("wolf_bounty") == 0 });
                    n.Options.Insert(System.Math.Max(0, n.Options.Count - 1),
                        new DOption { Text = "The wolves are dealt with.", Next = "wolves_paid", Show = c => c.G.Quests.Stage("wolf_bounty") == 2, Do = c => c.G.Quests.Set("wolf_bounty", 3, c.P) });
                }
            return d;
        }

        // =================================================================== villagers

        static Dialogue Tobin()
        {
            var d = new Dialogue("tobin", c => c.G.HasQuestItem(c.P, "lucky_spoon") ? "spoon" : "start");
            const string who = "Old Tobin";
            d.Node("start", who, c => c.G.World.Flag("spoon_returned")
                    ? "There's the spoon-finder! Best soup I've had in years, thanks to you."
                    : "My spoon! My lucky spoon! A little lizard man took it, I tell you. Little cape. Little hat. BIG attitude.",
                c => "A weathered old farmer leaning on a hoe, looking utterly betrayed by the universe.",
                c => { if (!c.G.World.Flag("spoon_returned")) c.G.Quests.Start("spoon"); })
                .Roll(Skill.Insight, 10, "Are you sure it wasn't just a lizard?", "insight", "insight", social: false)
                .Say("Need a hand with anything else?", "goats_ask", show: c => c.G.Quests.Stage("goats") == 0)
                .Say("Your goats are home.", "goats_done", show: c => c.G.Quests.Stage("goats") == 2, doIt: c => c.G.Quests.Set("goats", 3, c.P))
                .Bye("I'll keep an eye out.");
            d.Node("goats_ask", who, c => "Now that you mention it: my goats! Three of 'em ate through the fence and went off into Whisperwood. Clementine, Biscuit and Lord Horace. Lord Horace is the ringleader.",
                c => "Tobin points at a goat-shaped hole in his fence with a trembling finger.",
                c => c.G.Quests.Start("goats", c.P)).Say("I'll bring them home.", "start");
            d.Node("goats_done", who, c => "Clementine! Biscuit! LORD HORACE! Oh, you lot. Here: take Horace's old bell. He hates it. Keeps him from sneaking up on people.").Say("Thanks, Tobin.", "start");
            d.Node("insight", who, c => c.LastOutcome >= CheckOutcome.Success
                    ? "A regular lizard doesn't say 'FOR THE GREAT ONE' on its way out the window, does it? Does it?!"
                    : "Don't you take that tone. I know what I saw. Mostly.").Bye("Fair point.");
            d.Node("spoon", who, c => "MY SPOON! Oh, you beautiful soul. Here, take this, it's not much. My late wife's healing draught. She'd want it used.",
                c => "Tobin holds the spoon to his chest and does a small, stiff, very happy dance.",
                c => c.G.ReturnSpoon(c.P)).Bye("Enjoy your soup, Tobin.");
            return d;
        }

        static Dialogue Maddie()
        {
            var d = new Dialogue("maddie", c => c.G.Quests.Active("lost_child") ? "lost" : "start");
            const string who = "Maddie";
            d.Node("lost", who, c => "Shh! You'll scare them! The little lights are DANCING. They said I could stay for one more song. ...How many songs has it been? Is it dark?",
                c => "Maddie sits cross-legged by the fairy ring, muddy to the knees, surrounded by a slow swirl of tiny lights. She looks completely happy and slightly frozen.")
                .Say("Maddie! Everyone's looking for you! Let's get you home.", "found", doIt: c =>
                {
                    c.G.World.SetFlag("maddie_found");
                    c.G.Quests.Set("lost_child", QuestDb.Get("lost_child").Last, c.P);
                });
            d.Node("found", who, c => "Okay. Can we come back tomorrow? The lights said I'm a very good dancer. Better than you, probably.",
                c => "The lights swirl once around you both, like a wave goodbye, and wink out.").Bye("Probably. Let's go.");
            d.Node("start", who, c => "Are you an ADVENTURER? Have you ever fought a dragon? I'm going to be a dragon when I grow up.",
                c => "A gap-toothed kid holding a stick with absolute seriousness.")
                .Say("Did you see the falling star?", "star")
                .Roll(Skill.Performance, 8, "(Pretend to be a fearsome dragon) RAAAWR!", "play", "play_fail", social: false)
                .Bye("Keep practicing.");
            d.Node("star", who, c => "It wasn't a star. Stars don't have WINGS. I saw it! It went into the mountain and the mountain went 'mmmm' like when grandpa sleeps.").Say("Wings, huh?", "start");
            d.Node("play", who, c => "AAAH! Okay, okay, you win, you're the best dragon. I'm telling EVERYONE.", onEnter: c => c.G.World.AddFriendship("maddie", 20)).Bye("RAWR.");
            d.Node("play_fail", who, c => "...That was a duck. You did a duck.").Bye("It was a dragon duck.");
            return d;
        }

        // =================================================================== Snik the Egg-Warden (parley)

        static Dialogue Snik()
        {
            var d = new Dialogue("snik", c => "start");
            const string who = "Snik, Egg-Warden";
            d.Node("start", who, c => "WHO DARES approach the GREAT EGG?! Snik is Egg-Warden! Snik has a HAT! Turn back, tall ones, or face Snik's SPEAR!",
                c => "A kobold in a saucepan helmet stands before a glowing, red-scaled egg the size of a pumpkin. Two more kobolds peek out from behind rocks, slings ready. The egg pulses warmly, like a heartbeat.")
                .Roll(Skill.Insight, 10, "(Read Snik) What does he really want?", "insight_yes", "insight_no", social: false)
                .Roll(Skill.Persuasion, 13, "We've come to honor the Great One, not to harm it.", "peace", "fight_fail",
                      adv: c => c.G.World.Flag("snik_read") ? AdvantageState.Advantage : AdvantageState.Normal)
                .Roll(Skill.Intimidation, 15, "Stand aside, little warden, or we go through you.", "flee", "fight_fail")
                .Roll(Skill.Deception, 16, "We ARE dragons. In disguise. Very good disguises.", "peace_lie", "fight_fail", critSuccess: "peace_lie_crit")
                .Roll(Skill.Religion, 12, "The Dawnmother's light blesses this egg. We are its keepers too.", "peace", "fight_fail",
                      show: c => c.Is("cleric") || c.Bg("acolyte"), role: "Faithful")
                .Roll(Skill.Performance, 12, "A song! A ballad for the Great One's hatching day!", "peace_song", "fight_fail", show: c => c.Is("bard"), role: "Bard")
                .Roll(Skill.Arcana, 14, "The egg needs a worthy flame to hatch. We can provide it.", "peace", "fight_fail", show: c => c.Is("wizard") || c.Bg("sage"), role: "Scholar")
                .Say("Attack!", doIt: c => { c.Screen.Close(); c.G.StartGroupCombat("mine_egg", c.P); }, end: true);
            d.Node("insight_yes", who, c => "...",
                c => "Snik's spear is shaking. He's not angry. He's terrified you've come to hurt the egg. The other kobolds keep glancing at him for guidance. (Advantage on your Persuasion.)",
                c => c.G.World.SetFlag("snik_read")).Say("Okay.", "start");
            d.Node("insight_no", who, c => "WHY YOU LOOK AT SNIK LIKE THAT. STOP IT.").Say("Sorry.", "start");
            d.Node("peace", who, c => "You... you HONOR the Great One? ...The egg is warmer. The egg is warmer when you are near! The Great One CHOOSES you! Kobolds! LOWER SLINGS!",
                c => "Snik drops his spear. The kobolds cheer, a high, yippy sound, and start chanting your name. Badly.",
                c => c.G.PacifyGroup("mine_egg", c.P)).Say("(Approach the egg)", end: true);
            d.Node("peace_song", who, c => "IT IS A SONG FOR THE GREAT ONE! Kobolds! SING!",
                c => "The kobolds join in, enthusiastically and terribly. The egg pulses in time with the music. Snik is weeping openly.",
                c => c.G.PacifyGroup("mine_egg", c.P)).Say("(Approach the egg)", end: true);
            d.Node("peace_lie", who, c => "...Very good disguises. VERY good. Snik did not even notice! Forgive Snik, Great Ones!",
                c => "Snik prostrates himself. The other kobolds follow, then argue about who is prostrating more.",
                c => c.G.PacifyGroup("mine_egg", c.P)).Say("(Approach the egg)", end: true);
            d.Node("peace_lie_crit", who, c => "THE GREAT ONES HAVE RETURNED! Snik KNEW IT! Take this, Great Ones! Tribute! Snik found it! Snik is a GOOD warden!",
                c => "Snik presents you with a slightly dented copper kettle, a wooden spoon, four chicken feathers and a look of pure devotion.",
                c => c.G.PacifyGroup("mine_egg", c.P)).Say("(Accept the tribute and approach the egg)", end: true);
            d.Node("flee", who, c => "S-Snik is... Snik is going to get REINFORCEMENTS. Yes. Reinforcements. Kobolds! TACTICAL RETREAT!",
                c => "The kobolds scatter into the tunnels, Snik's saucepan helmet clanging all the way. In his panic, he drops a sack.",
                c => c.G.PacifyGroup("mine_egg", c.P, fled: true)).Say("(Approach the egg)", end: true);
            d.Node("fight_fail", who, c => "LIES! Snik smells LIES! Kobolds, DEFEND THE EGG!",
                c => "Slings whirl. Snik levels his spear with a war-shriek that is mostly squeak. Roll initiative!")
                .Say("(Fight!)", doIt: c => { c.Screen.Close(); c.G.StartGroupCombat("mine_egg", c.P); }, end: true);
            return d;
        }

        // =================================================================== Miller's Ford: Wren (parley) and Old Hobb

        static Dialogue Wren()
        {
            var d = new Dialogue("wren", c => "start");
            const string who = "Wren, Toll-Captain";
            System.Action<DCtx> fight = c => { c.Screen.Close(); c.G.StartGroupCombat("ford_toll", c.P); };
            d.Node("start", who, c => "Hold it right there, travelers! This is an OFFICIAL toll ford. Ten silver for the lot of you, payable to the Ford Wardens. That's us. We're the Ford Wardens.",
                    c => "A wiry woman in a patched green coat leans on a long boathook. Behind her, two big bargemen hold crossbows like they've only just been told which end is which.")
                .Roll(Skill.Insight, 12, "(Read her) Who are these people, really?", "insight_yes", "insight_no", social: false)
                .Roll(Skill.Persuasion, 14, "You're river folk, not bandits. Old Hobb needs hands to rebuild that bridge. He'd pay you.", "legit", "fight_fail",
                      adv: c => c.G.World.Flag("wren_read") || c.G.World.Flag("met_hobb") ? AdvantageState.Advantage : AdvantageState.Normal,
                      onSuccess: c => { c.G.World.SetFlag("wren_legit"); c.G.PacifyGroup("ford_toll", c.P); })
                .Roll(Skill.Intimidation, 14, "Step aside, or you're going for a swim.", "fled", "fight_fail",
                      onSuccess: c => c.G.PacifyGroup("ford_toll", c.P, fled: true))
                .Roll(Skill.Deception, 15, "We're from the Port Varrel tax office. We'll be needing our cut of these 'tolls'.", "fled_tax", "fight_fail",
                      show: c => c.Bg("criminal") || c.Is("bard"), role: "Silver tongue", onSuccess: c => c.G.PacifyGroup("ford_toll", c.P, fled: true))
                .Roll(Skill.Performance, 13, "A crossing this fine deserves a song. (Sing the ballad of the Old Bridge)", "legit_song", "fight_fail",
                      show: c => c.Is("bard"), role: "Bard", onSuccess: c => { c.G.World.SetFlag("wren_legit"); c.G.PacifyGroup("ford_toll", c.P); })
                .Say("(Pay the toll: 1 gp)", "paid", show: c => c.PC.Copper >= 100, doIt: c => { c.PC.Copper -= 100; c.G.GrantPassage("ford_toll"); })
                .Say("(Attack!)", doIt: fight, end: true);
            d.Node("insight_yes", who, c => "...",
                c => "That boathook is for poling barges, not fighting. Their boots are patched twice over, and the 'toll box' is mostly empty. These are river folk whose living washed away with the bridge. They're scared, and hungry. (Advantage on Persuasion.)",
                c => c.G.World.SetFlag("wren_read")).Say("I see.", "start");
            d.Node("insight_no", who, c => "Are you SQUINTING at me? Stop that. That's an official squint-free crossing.").Say("Sorry.", "start");
            d.Node("legit", who, c => "...Hobb would PAY us? To do real work? With our hands?",
                c => "Wren looks at her crew. The bigger bargeman is already nodding so hard his hat falls off. \"Fine. FINE. We'll go talk to the old grump. Ford's free, travelers. Go on.\"")
                .Say("(Continue)", end: true);
            d.Node("legit_song", who, c => "...Oh, that's not fair. You can't sing about the Old Bridge, that's not FAIR...",
                c => "By the second verse the bargemen are sniffling. By the third, Wren is too. \"Alright! Alright. We'll go see Hobb about rebuilding it. Happy?\"")
                .Say("(Continue)", end: true);
            d.Node("fled", who, c => "...Right. Toll's waived. Today. Special promotion. Lads, RUN.",
                c => "The Ford Wardens scatter into the woods, leaving their cookpot bubbling and a purse on the ground.")
                .Say("(Continue)", end: true);
            d.Node("fled_tax", who, c => "TAX? Nobody said anything about TAX. Lads! We were never here!",
                c => "They bolt so fast one of them leaves a boot behind. And a purse.")
                .Say("(Continue)", end: true);
            d.Node("paid", who, c => "Pleasure doing business! Mind the slippy stones.",
                c => "Wren bites the coin, nods, and waves you through. (They'll let you pass from now on, but they're still here...)")
                .Say("(Continue)", end: true);
            d.Node("fight_fail", who, c => "Oh, it's like THAT, is it? Wardens! Defend the... the toll!",
                c => "Crossbows come up, wobbly but loaded. Roll initiative!")
                .Say("(Fight!)", doIt: fight, end: true);
            return d;
        }

        static Dialogue Hobb()
        {
            var d = new Dialogue("hobb", c => c.G.Quests.Stage("east_road") >= 3 ? "done" : c.G.Quests.Stage("east_road") == 2 ? "clear" : "start");
            const string who = "Old Hobb";
            d.Node("start", who, c => "Visitors! From the WEST side! How'd you cross? ...Ah. The Wardens. Did they charge you? They charge ME to cross my own river.",
                    c => "A stooped old miller dusted with flour from eyebrows to boots. Behind him, the great mill wheel sits still in the current.",
                    c => c.G.World.SetFlag("met_hobb"))
                .Say("What happened to the bridge?", "bridge")
                .Say("Who are the Wardens?", "wardens")
                .Say("I'll sort it out.", "promise")
                .Bye("Take care, Hobb.");
            d.Node("bridge", who, c => "Spring flood. Took the middle span clean out. No bridge, no barges, no grain, and no grain means my wheel turns for nothing. Forty years I've milled for Thistledown.").Say("I see.", "start");
            d.Node("wardens", who, c => "Wren's lot. Barge crew. When the bridge went, so did their work. Can't blame 'em for trying, but a man can't pay ten silver every time he wants to buy cheese. If I had a few strong hands, I'd rebuild that bridge myself.").Say("Hm.", "start");
            d.Node("promise", who, c => "Would you? However you manage it, I'll make it worth your while. Talk sense into them, chase 'em off... I won't ask.").Bye("Leave it to me.");
            d.Node("clear", who, c => c.G.World.Flag("wren_legit")
                    ? "You did it! Wren's whole crew turned up at my door asking for WORK. We'll have planks across by next week, and a proper bridge by summer!"
                    : "The ford's clear? Well. I won't ask how. Thank you, friend.",
                    c => "Hobb shakes your hand with both of his, leaving two floury prints.")
                .Say("Glad to help.", "reward", doIt: c => c.G.CompleteEastRoad(c.P));
            d.Node("reward", who, c => "Here: twenty-five gold, and the best healing draught in the valley. My late wife's recipe. Don't tell the temple.").Bye("Thank you, Hobb.");
            d.Node("mill_ask", who, c => "Bothering me? No. Well. There's a ghost. In the mill. Wails all night like a kettle with a broken heart. I'm not SCARED. I just sleep in the garden now. By choice.",
                c => "Hobb's eyes dart toward the mill. He has, you notice, a frying pan within reach at all times.",
                c => c.G.Quests.Start("haunted_mill", c.P)).Say("I'll have a look after dark.", "done");
            d.Node("done", who, c => c.G.World.Flag("wren_legit") ? "Wheel's turning, bridge is coming along, and Wren's crew eat me out of house and home. Best problem I've had in years." : "Wheel's turning again! Come by any time.")
                .Say("Anything else bothering you?", "mill_ask", show: c => c.G.Quests.Stage("haunted_mill") == 0)
                .Roll(Skill.Persuasion, 10, "Any chance of a sack of flour for the road?", "flour", "noflour", onSuccess: c => c.G.GiveItem(c.P, "rations", 2))
                .Bye();
            d.Node("flour", who, c => "For you? Always. Here, some biscuits too.").Bye("Thanks!");
            d.Node("noflour", who, c => "Flour costs money, friend. Even for heroes.").Bye("Fair.");
            return d;
        }

        // =================================================================== recruits & companions

        static Dialogue Recruit()
        {
            var d = new Dialogue("recruit", c => "start");
            d.Node("start", null, c => "",
                c => c.Recruit.Data.Pitch)
                .Say("Hello there. Who are you?", "intro")
                .Bye("(Leave them be)");
            d.Node("intro", "", c => IntroLine(c), onEnter: c => c.Screen.SetSpeaker(c.Recruit.Data.Character.Name))
                .Roll(Skill.Persuasion, 12, "We could use someone like you. Join us?", "join", "nojoin", onSuccess: c => c.G.HireRecruit(c.P, c.Recruit),
                      adv: c => c.PC.KnowsSpell("charm_person") ? AdvantageState.Advantage : AdvantageState.Normal)
                .Say("I can pay. (Hire for a fee)", "paid", show: c => c.PC.Copper >= c.Recruit.Data.HireCostCp,
                     doIt: c => { c.PC.Copper -= c.Recruit.Data.HireCostCp; c.G.HireRecruit(c.P, c.Recruit); })
                .Roll(Skill.Insight, 10, "(Size them up)", "sized", "sized", social: false)
                .Bye("Maybe another time.");
            d.Node("join", "", c => "...Yeah. Yeah, alright. I've been sitting in this tavern too long anyway. Lead the way.").Say("(They join your party)", end: true);
            d.Node("nojoin", "", c => "Hm. I don't know you. Come back when you've got a reputation. Or coin.").Bye();
            d.Node("paid", "", c => $"Coin talks. {Currency.Format(c.Recruit.Data.HireCostCp)}, and I'm yours till the job's done.").Say("(They join your party)", end: true);
            d.Node("sized", null, c => "",
                c => c.LastOutcome >= CheckOutcome.Success
                    ? $"{c.Recruit.Data.Character.Name}: a level 1 {c.Recruit.Data.Character.Species.Name} {c.Recruit.Data.Character.Class.Name}. Solid. Probably reliable. They definitely {c.Recruit.Data.Quirk}."
                    : "You can't get a read on them. They stare back, unblinking, until you look away first.")
                .Say("Back to business.", "intro");
            return d;
        }

        static string IntroLine(DCtx c)
        {
            var pc = c.Recruit.Data.Character;
            switch (pc.Class.Id)
            {
                case "fighter": return $"Name's {pc.Name}. I hit things for money. I'm good at it. What do you want?";
                case "wizard": return $"{pc.Name}, formerly of the Starfall Academy. 'Formerly' is a long story. Are you here about my research?";
                case "cleric": return $"Blessings. I'm {pc.Name}. I go where I'm needed. Lately, that's been mostly here, drinking.";
                default: return $"{pc.Name}, bard, raconteur, and occasional fugitive! Looking for a story? I'm looking for a story.";
            }
        }

        static string RoleName(CompanionAgent a) =>
            a.Stance == CompanionStance.Frontline ? "front line" : a.Stance == CompanionStance.Ranged ? "hang back" : a.IsFrontline ? "front line (their call)" : "hang back (their call)";

        static int PotionIndex(PlayerCharacter pc)
        {
            for (int i = 0; i < pc.Bag.Size; i++)
            {
                var st = pc.Bag.Slots[i];
                if (st != null && (st.ItemId == "potion_healing" || st.ItemId == "potion_greater_healing")) return i;
            }
            return -1;
        }

        static Dialogue Companion()
        {
            var d = new Dialogue("companion", c => "start");
            d.Node("start", "", c =>
                    c.Comp.Character.HP < c.Comp.Character.EffectiveMaxHP / 2 ? $"I've been better, boss. ({c.Comp.Character.HP}/{c.Comp.Character.EffectiveMaxHP} HP)"
                    : c.Comp.Waiting ? "Still here. Holding the spot, like you said."
                    : "Ready when you are, boss.",
                    c => $"In fights: {(c.Comp.Manual ? "you call their moves" : "they act on their own")} · Role: {RoleName(c.Comp)} · Walking {(c.Comp.TakePoint ? "ahead of you" : "behind you")}",
                    onEnter: c => c.Screen.SetSpeaker(c.Comp.Character.Name))
                .Say("Here, drink this. (Give a healing potion)", "drank", show: c => PotionIndex(c.PC) >= 0 && c.Comp.Character.HP < c.Comp.Character.EffectiveMaxHP,
                     doIt: c => c.G.GivePotionTo(c.P, c.Comp.Character, PotionIndex(c.PC)))
                .Say("In a fight, I'll call your moves.", "manual", show: c => !c.Comp.Manual, doIt: c => c.Comp.Manual = true)
                .Say("In a fight, use your own judgment.", "auto", show: c => c.Comp.Manual, doIt: c => c.Comp.Manual = false)
                .Say("About your place in a fight...", "role")
                .Say("Take point. Walk ahead of me.", "point", show: c => !c.Comp.TakePoint, doIt: c => c.Comp.TakePoint = true)
                .Say("Fall in behind me.", "behind", show: c => c.Comp.TakePoint, doIt: c => c.Comp.TakePoint = false)
                .Say("Wait here.", show: c => !c.Comp.Waiting, doIt: c => { c.Comp.Waiting = true; c.P.Hud.Toast($"{c.Comp.Character.Name} waits here.", UIKit.UtilityBlue); }, end: true)
                .Say("Come with me.", show: c => c.Comp.Waiting, doIt: c => { c.Comp.Waiting = false; c.P.Hud.Toast($"{c.Comp.Character.Name} follows you again.", UIKit.UtilityBlue); }, end: true)
                .Say("Tell me about yourself.", "about")
                .Say("Wait at the Sleeping Drake. (Leave the party)", doIt: c => c.G.DismissCompanion(c.Comp), end: true)
                .Bye("Let's go.");
            d.Node("drank", "", c => "Ahh. That's the stuff. Tastes like cherries and regret.").Say("Better?", "start");
            d.Node("point", "", c => "Anything jumps out, it jumps at me first. That's the job.",
                    c => "They'll walk a few steps ahead of you. When a fight starts, they step in between you and the enemy.").Say("Good.", "start");
            d.Node("behind", "", c => "Right behind you, boss.",
                    c => "They'll follow behind you again. (Front-line fighters and clerics still step up when a fight starts.)").Say("Good.", "start");
            d.Node("manual", "", c => "You're the boss. Point, and I'll swing.",
                    c => "On their turn you'll get their action bar, just like yours. Press Auto any turn to let them decide.").Say("Good.", "start");
            d.Node("auto", "", c => "Leave it to me.",
                    c => "They'll take their own turns now, following the role you gave them.").Say("Good.", "start");
            d.Node("role", "", c => $"Where do you want me? Right now I'm planning to {RoleName(c.Comp)}.")
                .Say("Front line: get between the enemies and the rest of us.", "start", doIt: c => c.Comp.Stance = CompanionStance.Frontline)
                .Say("Hang back: use ranged attacks and spells, stay safe.", "start", doIt: c => c.Comp.Stance = CompanionStance.Ranged)
                .Say("Your call. (Fighters and clerics take the front, others hang back.)", "start", doIt: c => c.Comp.Stance = CompanionStance.Auto);
            d.Node("about", "", c => $"Me? Not much to tell. I'm a {c.Comp.Character.Species.Name.ToLower()} {c.Comp.Character.Class.Name.ToLower()}. And yes, I {c.Comp.Quirk}. Don't make it weird.").Say("Fair enough.", "start");
            return d;
        }

        // =================================================================== encounters (any hostile group that spots you)

        /// <summary>
        /// Built fresh for each group (its DCs differ). Every way out pays the same: peaceful or scared off = the group's
        /// full XP and loot (GameRoot.PacifyGroup); slipping away = nothing yet, they're still there.
        /// </summary>
        public static Dialogue Encounter(EnemyGroupDef g)
        {
            var d = new Dialogue("encounter", c => "start");
            bool beasts = g.Style == EncounterStyle.Beasts;
            string them = g.Label;
            string Them = char.ToUpper(them[0]) + them.Substring(1);
            int stealthDc = GameRoot.I.GroupPassivePerception(g.Id);
            System.Action<DCtx> fight = c => { c.Screen.Close(); c.G.StartGroupCombat(g.Id, c.P); };

            d.Node("start", null, c => "What do you do?",
                    c => g.Spotted ?? $"{Them} spot you! Weapons come up... but nobody has moved yet.")
                .Roll(Skill.Persuasion, g.TalkDC, "Easy. Nobody needs to get hurt today.", "befriended", "fail",
                      show: c => !beasts, onSuccess: c => c.G.PacifyGroup(g.Id, c.P))
                .Roll(Skill.Performance, g.TalkDC - 1, "(Strike up a song) ...Who wants to hear a ballad?", "befriended", "fail",
                      show: c => !beasts && c.Is("bard"), role: "Bard", onSuccess: c => c.G.PacifyGroup(g.Id, c.P))
                .Roll(Skill.Deception, g.TalkDC + 2, "Careful. The Watch is right behind us. All of them. With dogs.", "fled", "fail",
                      show: c => !beasts, onSuccess: c => c.G.PacifyGroup(g.Id, c.P, fled: true))
                .Roll(Skill.AnimalHandling, g.TalkDC, "(Slow movements, soft voice) Easy... easy now...", "befriended", "fail",
                      show: c => beasts, onSuccess: c => c.G.PacifyGroup(g.Id, c.P))
                .Roll(Skill.Intimidation, g.ScareDC, beasts ? "(Make yourself HUGE and roar)" : "Leave. Now. While you still can.", "fled", "fail",
                      onSuccess: c => c.G.PacifyGroup(g.Id, c.P, fled: true))
                .Roll(Skill.Stealth, stealthDc, "(Back away slowly into cover...)", "slipped", "fail", social: false,
                      onSuccess: c => { c.G.CalmGroup(g.Id); c.P.Avatar.Sneaking = true; })
                .Say("(Draw your weapon and fight!)", doIt: fight, end: true);
            d.Node("befriended", null, c => "",
                    c => g.Befriended ?? $"{Them} exchange looks, then lower their weapons. Nobody has to bleed today.")
                .Say("(Continue)", end: true);
            d.Node("fled", null, c => "",
                    c => $"{Them} take one look at you and BOLT, scattering into the {(c.P.Zone.Def.Dark ? "tunnels" : "trees")}. Whatever they were carrying, they drop.")
                .Say("(Continue)", end: true);
            d.Node("slipped", null, c => "",
                    c => $"You melt back into cover. {Them} blink, grumble, and lose track of you. (You're sneaking now. Creep around them, or get close unseen and press Interact to AMBUSH.)")
                .Say("(Continue)", end: true);
            d.Node("fail", null, c => "",
                    c => c.LastRoll != null && c.LastRoll.Natural == 1 ? "...That could NOT have gone worse. Roll for initiative!" : "It doesn't land. Roll for initiative!")
                .Say("(Fight!)", doIt: fight, end: true);
            return d;
        }
    }
}
