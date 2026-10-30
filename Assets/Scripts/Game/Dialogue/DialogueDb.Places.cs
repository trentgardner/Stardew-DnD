using DnD.Rules;

namespace DnD.Game
{
    /// <summary>Phase 5's new places. Names are side content pending lore approval (docs/LORE_PITCH.md).</summary>
    public static partial class DialogueDb
    {
        /// <summary>Dialogues that live in this file (DialogueDb.Get checks here first).</summary>
        static Dialogue PlacesDialogue(string id)
        {
            switch (id)
            {
                case "perrin": return Perrin();
                case "ssark": return Ssark();
                case "tik": return Tik();
                case "snik_caught": return SnikCaught();
                case "watch_armor": return WatchArmor();
                default: return null;
            }
        }

        /// <summary>Extra lines bolted onto older dialogues for the new places (e.g. townsfolk gawking at the crater).</summary>
        static void AddPlacesLines(string id, Dialogue d)
        {
            switch (id)
            {
                case "tobin": CraterLine(d, "Old Tobin", "Seventy years I've lived under that sky and the stars stayed UP. Now look. A hole. In my hills. Who's going to fill it in, eh? Not the Academy, I'll tell you that."); break;
                case "maddie": CraterLine(d, "Maddie", "I'm looking for star bits! Mum says if I find one I can keep it. Perrin says I'm not allowed to touch anything. Perrin isn't my mum."); break;
                case "snik": SnikFriend(d); break;
                case "rhoswen": RhoswenTowerLines(d); break;
                case "wick": CraterLine(d, "Wick", "Did you know the crystals tickle? Don't tell the Academy man. He gets a vein in his forehead."); break;
            }
        }

        static void CraterLine(Dialogue d, string who, string line)
        {
            var inner = d.Start;
            d.Start = c => c.Npc != null && c.Npc.Actor != null && c.Npc.Actor.Zone != null && c.Npc.Actor.Zone.Def.Id == "crater" ? "crater_gawk" : inner(c);
            d.Node("crater_gawk", who, c => line).Bye("Enjoy the view.");
        }

        // =================================================================== Perrin Quillby (Starfall Academy)

        static Dialogue Perrin()
        {
            var d = new Dialogue("perrin", c => c.G.World.Flag("met_perrin") ? "start" : "intro");
            const string who = "Perrin Quillby";
            d.Node("intro", who,
                c => "Oh! Oh, hello! Careful, careful, you're standing on a- no, that's just a rock. Perrin Quillby, Junior Scholar, Starfall Academy. Well. Junior Junior Scholar. I'm here to document the Significant Arcane Event!",
                c => "A thin young man in robes two sizes too big, with ink on his fingers, ink on his nose, and a measuring string wound round one arm like a bandage.",
                c => c.G.World.SetFlag("met_perrin"))
                .Say("What do you make of it?", "theory")
                .Say("Junior Junior?", "junior");
            d.Node("junior", who, c => "There was an exam. There was a small fire. It wasn't my fault, the fire started it. Anyway! They sent me because everyone senior was busy. Which means this is MY discovery. Probably.")
                .Say("What do you make of it?", "theory");
            d.Node("start", who,
                c => c.Npc != null && c.Npc.Actor != null && c.Npc.Actor.Zone != null && c.Npc.Actor.Zone.Def.Id == "tavern"
                    ? "Ah, the crater person! Sit, sit. I'm writing to the Academy. Do you spell 'unprecedented' with one 'c' or two? It feels like it should be three."
                    : "The crater person! Hello again! I've measured the crater four times. I got four different answers. Science is going wonderfully.")
                .Say("What do you make of it?", "theory")
                .Say("Any idea where the star went?", "hint_hollow", show: c => c.G.Quests.Stage("fallen_star") == FallenStar.Crater)
                .Say("Something carried the star off. Any ideas?", "hint_tracks", show: c => c.G.Quests.Stage("fallen_star") == FallenStar.Trail)
                .Roll(Skill.Insight, 12, "(Read him) Who are you really reporting to?", "sent_yes", "sent_no", social: false)
                .Bye("Good luck with the science.");
            d.Node("theory", who, c => "My working theory is a star-metal meteorite with a rare crystalline resonance. My second theory is a comet that got lost. My third theory is that it was a very large firework. I'm keeping an open mind.",
                c => "He says this with total confidence. You suspect every one of them is wrong.")
                .Say("Any idea where the star went?", "hint_hollow", show: c => c.G.Quests.Stage("fallen_star") == FallenStar.Crater)
                .Say("Something carried the star off. Any ideas?", "hint_tracks", show: c => c.G.Quests.Stage("fallen_star") == FallenStar.Trail)
                .Bye("Fascinating. Goodbye.");
            d.Node("hint_hollow", who, c => "Went? Stars don't GO anywhere, they- hm. Well. Now that you mention it, the hollow in the middle is rather empty, isn't it. I haven't looked closely. It's hot down there and I bruise easily. Perhaps you could study it? For science?")
                .Say("I'll take a look.", end: true);
            d.Node("hint_tracks", who, c => "Ah! Yes! The footprints! Small. Clawed. Lots of them. My theory is a very short bear. Possibly several very short bears. They went off toward the north-east, over the goat path. I didn't follow. Bears.",
                c => "He's pointing at the north-east rim, where a line of little prints climbs up the goat path toward the mountains.")
                .Say("Very short bears. Right.", end: true);
            d.Node("sent_yes", who, c => "...Fine. The Academy wants a full report. A FULL report, they said, underlined. And if anything 'of significant value' turned up here, I'm to write at once, and they'll send people. Serious people. With wagons.",
                c => "He fiddles with his string. He doesn't seem to like that part of the job very much.",
                c => c.G.World.AddFriendship("perrin", 15))
                .Say("And will you write?", "sent_write");
            d.Node("sent_write", who, c => "I... haven't decided. Everyone here keeps giving me pie. It's very hard to write a stern report about people who keep giving you pie.").Bye("Keep eating the pie.");
            d.Node("sent_no", who, c => "Reporting? To the Academy! About the crater! Obviously! Why, who else would I- that's a very searching look. Please stop it.").Bye("Just curious.");
            return d;
        }

        // =================================================================== Snik, caught in the act (the night stakeout)

        /// <summary>Snik agrees to speak for you: the peaceful road through the warrens.</summary>
        static void SnikBefriended(DCtx c)
        {
            c.G.World.SetFlag("ladle_returned");
            c.G.World.SetFlag("snik_friend");
            c.G.Quests.Start("go_between", c.P);
            c.G.Quests.Set("fallen_star", FallenStar.Warrens, c.P);
        }

        /// <summary>Snik bolts for home. You know where the thief lives now, but he won't vouch for you.</summary>
        static void SnikFled(DCtx c, bool droppedLadle)
        {
            if (droppedLadle) c.G.World.SetFlag("ladle_returned");
            c.G.Quests.Set("fallen_star", FallenStar.Warrens, c.P);
        }

        static Dialogue SnikCaught()
        {
            var d = new Dialogue("snik_caught", c => "start");
            const string who = "Snik, Egg-Warden";
            System.Func<DCtx, AdvantageState> adv = c => c.G.World.Flag("snik_caught_read") ? AdvantageState.Advantage : AdvantageState.Normal;
            d.Node("start", who, c => "...Snik is not here. This is a different kobold. A much taller one. Who is also leaving.",
                c => "Halfway up a market stall, frozen in your lantern light: a kobold in a saucepan helmet, clutching Pip's best copper ladle to his chest like a newborn. Behind him, a sack clinks with spoons.")
                .Say("Put the ladle back and talk to me.", "friend", SnikBefriended)
                .Roll(Skill.Persuasion, 12, "Nobody's in trouble. Tell me what the shinies are for.", "friend", "flee", onSuccess: SnikBefriended, onFailure: c => SnikFled(c, false), adv: adv)
                .Roll(Skill.Insight, 11, "(Read him) He's not greedy. He's desperate.", "read_yes", "read_no", social: false, show: c => !c.G.World.Flag("snik_caught_read"))
                .Roll(Skill.AnimalHandling, 14, "(Hold out a hand, slowly) Who's a good little lizard?", "pet_yes", "flee", onSuccess: SnikBefriended, onFailure: c => SnikFled(c, false), role: "Joke")
                .Roll(Skill.Intimidation, 12, "Drop it. NOW.", "scared", "flee", onSuccess: c => SnikFled(c, true), onFailure: c => SnikFled(c, false))
                .Roll(Skill.Athletics, 13, "(Grab him)", "grabbed", "flee", onSuccess: SnikBefriended, onFailure: c => SnikFled(c, false), social: false);
            d.Node("read_yes", who, c => "...",
                c => "He's shaking. Not from fear of you. He keeps glancing north, toward the mountains, the way a parent glances at a door they left a child behind. (Advantage on your Persuasion.)",
                c => c.G.World.SetFlag("snik_caught_read")).Say("Okay.", "start");
            d.Node("read_no", who, c => "Why is tall-one squinting. Is tall-one going to sneeze. Snik will wait.").Say("...", "start");
            d.Node("friend", who, c => "The Great Egg needs SHINIES. To keep warm! It fell from the sky and it is far from home and it is COLD, tall-one, and Snik is Egg-Warden and Snik has to keep it warm. The ladle was very shiny. Snik is sorry. A little.",
                c => "He puts the ladle back on its hook, very gently, and pats it once. \"...Tall-one is not angry? Tall-one could come and SEE. Snik will tell the gate you are friend. Snik will tell the Elder. Under the old mine, past the stairs. Come!\"")
                .Bye("I'll come. Go home, Snik.");
            d.Node("pet_yes", who, c => "...Snik is not a good little lizard. Snik is EGG-WARDEN.",
                c => "But his tail is wagging. He leans into the scratch, just a little, then catches himself and puts the ladle back with tremendous dignity. \"Fine. FINE. You are friend. Come to the warrens. Snik will tell them.\"")
                .Bye("Go home, Snik.");
            d.Node("grabbed", who, c => "UNHAND SNIK. Snik is a WARDEN. This is an OUTRAGE.",
                c => "You've got him by the scruff. He kicks, sulks, kicks again, and finally goes limp and puts the ladle back. \"...Snik will talk. Snik will tell the gate you are friend. But Snik is sulking. Snik wants that known.\"")
                .Bye("Noted. Go home, Snik.");
            d.Node("scared", who, c => "EEP!",
                c => "The ladle clangs to the cobbles. Snik vaults the stall, bounces off a rain barrel, and pelts north toward the old mine, saucepan helmet clanging all the way. You know where the thief lives now.")
                .Bye("(Return the ladle to Pip's stall)");
            d.Node("flee", who, c => "SNIK WAS NEVER HERE!",
                c => "He's off like a shot, ladle and all, scrambling over the rooftops and away north toward the old mine. At least you know where he's going.")
                .Bye("...Right.");
            return d;
        }

        /// <summary>Snik's egg parley gets a new opening for a friend who already earned the Elder's blessing.</summary>
        static void SnikFriend(Dialogue d)
        {
            var inner = d.Start;
            d.Start = c => c.G.Quests.Done("go_between") ? "friend" : inner(c);
            d.Node("friend", "Snik, Egg-Warden", c => "FRIEND! Friend is HERE! Kobolds, lower slings, this is the tall-one Snik TOLD you about! Come, come, meet the Great Egg! Say hello! It can hear you!",
                c => "Snik hops from foot to foot, saucepan rattling. The egg-guards drop their slings and start a song of welcome that is loud, sincere and spectacularly off-key. The egg pulses, warm as a hearth.",
                c => c.G.PacifyGroup("mine_egg", c.P))
                .Say("(Approach the egg)", end: true);
        }

        // =================================================================== The last watchman (the Old Watchtower)

        static void ArmorKneels(DCtx c) => c.G.PacifyGroup("tower_armor", c.P);

        static Dialogue WatchArmor()
        {
            var d = new Dialogue("watch_armor", c => "start");
            const string who = "The Last Watchman";
            d.Node("start", who, c => "...WHO... CLIMBS... THE TOWER. SPEAK THE WATCHWORD.",
                c => "The suit of armor by the lantern creaks upright. Dust pours from its joints. There's nobody inside it, only a faint glow where a face should be, and the smell, very faint, of lamp oil. Its gauntlet closes on the hilt of a sword that has not been drawn in centuries.")
                .Say("(Recite the words from the wall) \"" + WorldDb.FirstIdeal + "\"", "kneel", ArmorKneels, show: c => c.G.World.Flag("secret:tower_inscription"))
                .Roll(Skill.History, 10, "(Recall the old watchword) The Oathbound kept the light...", "kneel", "wrong", onSuccess: ArmorKneels, show: c => !c.G.World.Flag("secret:tower_inscription"), social: false)
                .Roll(Skill.Religion, 13, "We serve the light too. The same light you kept.", "kneel", "wrong", onSuccess: ArmorKneels)
                .Roll(Skill.Persuasion, 15, "Your watch is over, sentinel. Let us carry it now.", "kneel", "wrong", onSuccess: ArmorKneels)
                .Say("(Draw your weapon)", doIt: c => { c.Screen.Close(); c.G.StartGroupCombat("tower_armor", c.P); }, end: true);
            d.Node("kneel", who, c => "...THE WATCH... IS KEPT.",
                c => "The armor lowers its sword, sinks to one knee with a long groan of old iron, and bows its empty helm toward the lantern. The glow inside it fades, gently, like a lamp being turned down at the end of a long night. It does not move again.")
                .Bye("(Leave it to its rest)");
            d.Node("wrong", who, c => "...THAT IS NOT. THE WATCHWORD.",
                c => "The empty helm turns toward you. The sword comes up. Roll initiative!")
                .Say("(Fight!)", doIt: c => { c.Screen.Close(); c.G.StartGroupCombat("tower_armor", c.P); }, end: true);
            return d;
        }

        /// <summary>Captain Vale hands out the watchtower job once the main quest is under way, and hears what the inscription says.</summary>
        static void RhoswenTowerLines(Dialogue d)
        {
            const string who = "Captain Rhoswen Vale";
            d.Node("tower_job", who, c => "Actually... yes. There's an old watchtower on the ridge north of Miller's Ford. Older than the village. My grandmother said there were words carved at the top that nobody's read in a hundred years. With stars falling out of the sky, I'd like to know what our ancestors were watching FOR.",
                c => "She taps the ridge on a map pinned inside her breastplate. The map is mostly coffee stains.",
                c => c.G.Quests.Start("watchtower", c.P))
                .Say("I'll scout it.", end: true);
            d.Node("tower_done", who, c => "\"" + WorldDb.FirstIdeal + "\"",
                c => "She repeats it under her breath, then again, and goes very quiet. Then she pulls out a notebook and writes it down. Then she writes it down again, more neatly. \"The watch keeps the light so others may sleep. ...That's the job, isn't it. That was always the job.\"",
                c => c.G.Quests.Set("watchtower", 3, c.P))
                .Bye("It is.");
            foreach (var id in new[] { "intro", "progress", "dragon", "after" })
                if (d.Nodes.TryGetValue(id, out var n))
                {
                    n.Options.Insert(System.Math.Max(0, n.Options.Count - 1),
                        new DOption { Text = "Any other work, Captain?", Next = "tower_job", Show = c => c.G.Quests.Stage("watchtower") == 0 && c.G.Quests.Stage("fallen_star") >= FallenStar.Trail });
                    n.Options.Insert(System.Math.Max(0, n.Options.Count - 1),
                        new DOption { Text = "I read the words at the top of the watchtower.", Next = "tower_done", Show = c => c.G.Quests.Stage("watchtower") == 2 });
                }
        }

        // =================================================================== Elder Ssark (the Kobold Warrens)

        /// <summary>The first shiny in the bag that the Elder would accept, or null.</summary>
        static string ShinyFor(DCtx c) => c.PC.CountOf("copper_trinket") > 0 ? "copper_trinket" : c.PC.CountOf("star_shard") > 0 ? "star_shard" : null;

        /// <summary>The gate was fought, not talked down: the Elder is warier of you.</summary>
        static bool GateFought(DCtx c) => c.G.World.DefeatedGroups.Contains("warrens_gate") && !c.G.World.PacifiedGroups.Contains("warrens_gate");

        static void Bless(DCtx c) => c.G.World.SetFlag("elder_blessing");

        static Dialogue Ssark()
        {
            var d = new Dialogue("ssark", c => c.G.World.Flag("elder_blessing") ? "blessed" : c.G.Quests.Stage("go_between") >= 2 ? "vouched" : "start");
            const string who = "Elder Ssark";
            System.Func<DCtx, AdvantageState> adv = c => c.G.World.Flag("ssark_insight") ? AdvantageState.Advantage : AdvantageState.Normal;
            d.Node("start", who,
                c => GateFought(c)
                    ? "Tall-one. You hurt my gate-guards. They will mend. Kobolds always mend. But you will understand if I do not offer you tea."
                    : "A tall-one. In the warrens. Sit, sit, if you can fold yourself small enough. I am Ssark, eldest of the egg-singers. You have come about the Great Egg. Everyone comes about the Great Egg.",
                c => "A tiny, ancient kobold wrapped in a shawl knitted from at least nine different stolen scarves. Her eyes are milky, but they follow you perfectly.")
                .Roll(Skill.Persuasion, 15, "We mean the Egg no harm. Let us see it.", "bless_yes", "bless_no", onSuccess: Bless, show: c => !GateFought(c), adv: adv)
                .Roll(Skill.Persuasion, 17, "We mean the Egg no harm. Let us see it.", "bless_yes", "bless_no", onSuccess: Bless, show: GateFought, adv: adv)
                .Roll(Skill.Insight, 12, "(Read her) You're afraid. But not of us.", "insight_yes", "insight_no", social: false, show: c => !c.G.World.Flag("ssark_insight"))
                .Say("(Offer a shiny)", "shiny", c =>
                {
                    string item = ShinyFor(c);
                    if (item == null) return;
                    c.PC.RemoveItem(item);
                    c.P.Hud.Toast($"-1 {Rules.Content.ItemsDb.Get(item).Name}", UIKit.Bad);
                    Bless(c);
                }, show: c => ShinyFor(c) != null)
                .Bye("I'll leave you in peace.");
            d.Node("insight_yes", who, c => "...Clever tall-one. Yes. Not of you. Of the OTHER tall-ones. The ones with books and wagons. The Academy. When the Egg hatches, they will come, and they will take. That is what tall-ones do with shinies.",
                c => "Her claws tighten on her shawl. For a moment she looks every one of her very many years.",
                c => c.G.World.SetFlag("ssark_insight"))
                .Roll(Skill.Persuasion, 15, "Then let us help you protect it.", "bless_yes", "bless_no", onSuccess: Bless, show: c => !GateFought(c), adv: adv)
                .Roll(Skill.Persuasion, 17, "Then let us help you protect it.", "bless_yes", "bless_no", onSuccess: Bless, show: GateFought, adv: adv)
                .Bye("I'll think on it.");
            d.Node("insight_no", who, c => "You stare at an old woman very hard. It is rude. I have outstared cave bears, little one.").Say("Sorry.", "start");
            d.Node("bless_yes", who, c => "...Hm. You talk like Snik. Too much, and with your whole heart. Hold out your hand.",
                c => "She dips a claw in a pot of red clay and draws a little curl on the back of your hand: an egg, with a flame inside. \"Krag will know it. Go down. Be gentle. It is listening.\"")
                .Bye("Thank you, Elder.");
            d.Node("bless_no", who, c => "Words, words. Tall-ones have so many words. Come back when you have fewer of them, and better ones.").Bye("Fair enough.");
            d.Node("shiny", who, c => "Ohhh. Oh, that is a GOOD one. The Egg will like this one very much. Hold out your hand, generous tall-one.",
                c => "She tucks the shiny into her shawl with a speed that suggests long practice, then daubs a little red clay curl on the back of your hand: an egg, with a flame inside. \"Krag will know it. Go down.\"")
                .Bye("Thank you, Elder.");
            d.Node("vouched", who, c => "Snik told me about you. Snik talks a great deal. But he says you put the ladle BACK, and you did not have to, and that a tall-one who gives back a shiny is a tall-one worth knowing. Hold out your hand.",
                c => "She daubs a little red clay curl on the back of your hand: an egg, with a flame inside.")
                .Say("(Hold out your hand)", "blessed", Bless);
            d.Node("blessed", who, c => "Go down to the Great Egg, tall-one. Krag will let you pass. He will sulk, but he will let you pass.").Bye("Thank you, Elder.");
            return d;
        }

        // =================================================================== Tik (Tik's Shiny Stall)

        static Dialogue Tik()
        {
            var d = new Dialogue("tik", c => "start");
            const string who = "Tik";
            d.Node("start", who, c => "Welcome welcome WELCOME to Tik's Shiny Stall! Best shinies in the warrens! Only shinies in the warrens! Tik buys shinies at FULL PRICE. Tik is very generous. Tik is also very bad at sums.",
                c => "A wiry kobold in a waistcoat made entirely of buttons. He rubs his claws together so fast they squeak.")
                .Say("Let's trade.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "kobold"); }, end: true)
                .Say("What's this Great Egg everyone sings about?", "egg")
                .Bye("Maybe later.");
            d.Node("egg", who, c => "It fell from the sky! Boom! Right in the hills! Snik found it first, Snik always finds things first. We carried it home. Took eleven of us and a door. Now we keep it warm with shinies. Shinies are warm. Everyone knows that.")
                .Say("Let's trade.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "kobold"); }, end: true)
                .Bye("Everyone knows that.");
            return d;
        }
    }
}
