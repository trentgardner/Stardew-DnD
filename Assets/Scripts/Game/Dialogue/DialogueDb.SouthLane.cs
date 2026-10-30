using DnD.Rules;

namespace DnD.Game
{
    /// <summary>The South Lane folk (Phase 4). Names are side content pending lore approval (docs/LORE_PITCH.md).</summary>
    public static partial class DialogueDb
    {
        // =================================================================== Garrick Ashforge (the forge)

        static Dialogue Garrick()
        {
            var d = new Dialogue("garrick", c => "start");
            const string who = "Garrick Ashforge";
            d.Node("start", who,
                c => c.Npc != null && c.Npc.Actor.Zone.Def.Id == "forge"
                    ? "Welcome to the Ashforge! If it's iron, I can make it. If it's not iron, I can probably still hit it with a hammer until it is."
                    : "Mind the sparks! Shop's inside if you're buying. If you're just watching, you can pump the bellows.",
                c => "A broad, soot-streaked man with forearms like hams and a laugh you can hear across the green.")
                .Say("Show me what you've got.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "forge"); }, end: true)
                .Roll(Skill.Athletics, 13, "(Pump the bellows) How hard can it be?", "bellows_yes", "bellows_no", social: false)
                .Roll(Skill.Persuasion, 14, "A discount for someone who'll be fighting kobolds with your steel?", "disc_yes", "disc_no",
                      onSuccess: c => c.G.World.SetShopModifier(c.P.Index, "forge", -10, c.G.Clock.Day))
                .Say("You and Brannoc both sell swords?", "rival")
                .Bye();
            d.Node("bellows_yes", who, c => "HA! Look at that fire sing! You've got a smith's shoulders, friend. Come back and I'll teach you to fold steel.",
                c => "The coals roar white-hot. Garrick claps you on the back hard enough to rearrange your spine.",
                c => c.G.World.AddFriendship("garrick", 25)).Say("Maybe I will.", "start");
            d.Node("bellows_no", who, c => "That's a... gentle breeze. Very relaxing. The fire's having a nap now. Here, let me.").Say("Right.", "start");
            d.Node("disc_yes", who, c => "Kobolds, eh? Can't have you chasing them with a butter knife. Ten percent, today. Don't tell the dwarf.")
                .Say("The wares, then.", doIt: c => { c.Screen.Close(); c.G.OpenShop(c.P, "forge"); }, end: true);
            d.Node("disc_no", who, c => "Ha! Everyone's fighting kobolds this week. The price is the price.").Say("Worth a shot.", "start");
            d.Node("rival", who, c => "Brannoc sells what he buys. I sell what I make. He'll tell you mine are too heavy. I'll tell you his are too shiny. We're both right. Pint every Emberday, and we argue about it.")
                .Say("Sounds like a friendship.", "start");
            return d;
        }

        // =================================================================== Hesper Meadowsweet and Dunstan Burrow (the feud)

        static Dialogue Hesper()
        {
            var d = new Dialogue("hesper", c => "start");
            const string who = "Hesper Meadowsweet";
            d.Node("start", who, c => "Mind the bees, dear. They don't sting friends. They DO sting Burrows. I've trained them.",
                c => "A small, fierce old woman in a straw hat with a veil, holding a smoking bee-pot like a weapon.")
                .Say("About the fence... Captain Vale sent me. What happened?", "bb_story", show: c => c.G.Quests.Stage("bad_blood") == 1 && !c.G.World.Flag("bb_heard_hesper"))
                .Roll(Skill.Insight, 12, "(Read her) This isn't really about a fence, is it?", "bb_insight", "bb_insight_no", social: false,
                      show: c => c.G.Quests.Active("bad_blood") && !c.G.World.Flag("bb_insight_hesper"), onSuccess: c => c.G.World.SetFlag("bb_insight_hesper"))
                .Roll(Skill.Persuasion, 14, "(Mediate) Come to the footbridge. Talk to him. Just talk.", "bb_mediated", "bb_mediate_fail",
                      show: c => c.G.Quests.Stage("bad_blood") == 2, adv: MediateAdvantage, onSuccess: Mediated)
                .Say("(Side with Hesper) The fence stays where Albert put it.", "bb_sided", show: c => c.G.Quests.Stage("bad_blood") == 2,
                      doIt: c => SideWith(c, "hesper", "dunstan"))
                .Say("You and your neighbor don't get along?", "feud")
                .Roll(Skill.Nature, 11, "Your bees look healthy. What do you feed them?", "bees_yes", "bees_no", social: false)
                .Bye("Good day, Mrs Meadowsweet.");
            d.Node("bb_story", who, c => "Vale sent you, did she? Fine. My Albert built that fence forty years ago. Straight as a sunbeam. Now it's a FOOT west and Dunstan Burrow swears blind it walked there by itself.",
                onEnter: c => HeardSide(c, "bb_heard_hesper")).Say("I'll hear his side too.", "start");
            d.Node("bb_insight", who, c => "...Albert and Dunstan built it together, you know. Best friends. After the funeral Dunstan stopped coming round. Couldn't look at me, I suppose. So I watch the fence instead.",
                c => "Her hand rests on the old post the way you'd rest a hand on someone's shoulder.").Say("I'm sorry.", "start");
            d.Node("bb_insight_no", who, c => "What are you squinting at? It's a fence. It's ALWAYS been about the fence.").Say("Of course.", "start");
            d.Node("bb_mediated", who, c => "...Hello, Dunstan.",
                c => "On the footbridge, the two of them stand a careful yard apart. Nobody speaks for a long time. Then Dunstan says, \"I miss him too, Hes.\" And Hesper Meadowsweet, who has never cried in public in her life, does. By sunset the fence has been moved back exactly half a foot, and they're arguing cheerfully about whose honey is better.")
                .Bye("(Leave them to it.)");
            d.Node("bb_mediate_fail", who, c => "Talk to HIM? I'd sooner talk to the bees. At least they listen.").Say("Maybe another day.", "start");
            d.Node("bb_sided", who, c => "HA! You hear that, Burrow? Even the adventurers agree! Albert's fence stays put!",
                c => "Across the stream, a door slams.").Bye("Glad that's settled.");

            d.Node("feud", who, c => "Neighbor! HA. That fence was a foot further east when my Albert built it. A FOOT. Dunstan Burrow moves it at night. I've seen the dirt.")
                .Say("I'll leave you to it.", "start");
            d.Node("bees_yes", who, c => "Clover and thyme, and a bit of lavender from my Albert's old patch. You know bees! Here: honeycomb. Don't share it with anyone named Burrow.",
                onEnter: c => c.G.World.AddFriendship("hesper", 25)).Bye("Thank you!");
            d.Node("bees_no", who, c => "Feed them? Dear, they feed themselves. They're bees.").Bye("Of course.");
            return d;
        }

        static Dialogue Dunstan()
        {
            var d = new Dialogue("dunstan", c => "start");
            const string who = "Dunstan Burrow";
            d.Node("start", who, c => "Morning. If you're here from Hesper, tell her the fence is EXACTLY where it's always been, and her bees can keep their opinions to themselves.",
                c => "A round, ruddy man in a beekeeper's hat, standing exactly on the property line with his arms folded.")
                .Say("About the fence... Captain Vale sent me. What happened?", "bb_story", show: c => c.G.Quests.Stage("bad_blood") == 1 && !c.G.World.Flag("bb_heard_dunstan"))
                .Roll(Skill.Persuasion, 14, "(Mediate) Come to the footbridge. Talk to her. Just talk.", "bb_mediated", "bb_mediate_fail",
                      show: c => c.G.Quests.Stage("bad_blood") == 2, adv: MediateAdvantage, onSuccess: Mediated)
                .Say("(Side with Dunstan) A fence is a fence. Yours is fine where it is.", "bb_sided", show: c => c.G.Quests.Stage("bad_blood") == 2,
                      doIt: c => SideWith(c, "dunstan", "hesper"))
                .Say("What's the story with the fence?", "feud")
                .Roll(Skill.Insight, 12, "(Read him) You don't actually seem angry.", "insight_yes", "insight_no", social: false,
                      onSuccess: c => c.G.World.SetFlag("bb_insight_dunstan"))
                .Bye("Good day, Mr Burrow.");
            d.Node("bb_story", who, c => "The fence? The fence was SINKING. Albert's old posts were rotting through. I reset them on firmer ground. A foot over. I was going to tell her! I just... never found the right moment. For six years.",
                onEnter: c => HeardSide(c, "bb_heard_dunstan")).Say("I'll hear her side too.", "start");
            d.Node("bb_mediated", who, c => "...Hello, Hes.",
                c => "On the footbridge, the two of them stand a careful yard apart. Then Dunstan says, \"I miss him too, Hes. I fixed his fence because I couldn't fix anything else.\" Hesper takes his arm. By sunset they're arguing cheerfully about whose honey is better, which, you're told, is how it used to be.")
                .Bye("(Leave them to it.)");
            d.Node("bb_mediate_fail", who, c => "Talk to her? She'll set the bees on me. She's trained them. Probably.").Say("Maybe another day.", "start");
            d.Node("bb_sided", who, c => "There! Somebody with SENSE! A fence is a fence!",
                c => "Across the stream, a window slams so hard a bee falls off the sill.").Bye("Glad that's settled.");
            d.Node("feud", who, c => "Story? There's no story. There's a fence, and there's a woman who thinks it walks. Fences don't walk. ...Mine doesn't.")
                .Say("Right.", "start");
            d.Node("insight_yes", who, c => "...Angry? No. Tired, mostly. We used to be friends, you know. Before. Never mind.",
                c => "For a moment the bluster drops and he just looks across the stream at her cottage, like a man looking at an old photograph.",
                c => c.G.World.AddFriendship("dunstan", 15)).Say("Before what?", "start");
            d.Node("insight_no", who, c => "Are you squinting at me? Don't squint at me. The bees don't like squinting.").Say("Sorry.", "start");
            return d;
        }

        static void HeardSide(DCtx c, string flag)
        {
            c.G.World.SetFlag(flag);
            if (c.G.World.Flag("bb_heard_hesper") && c.G.World.Flag("bb_heard_dunstan")) c.G.Quests.Set("bad_blood", 2, c.P);
        }

        /// <summary>Understanding both of them (both Insight reads) makes the talk easier.</summary>
        static AdvantageState MediateAdvantage(DCtx c) =>
            c.G.World.Flag("bb_insight_hesper") && c.G.World.Flag("bb_insight_dunstan") ? AdvantageState.Advantage : AdvantageState.Normal;

        static void Mediated(DCtx c)
        {
            c.G.World.SetFlag("bb_mediated");
            c.G.World.AddFriendship("hesper", 50);
            c.G.World.AddFriendship("dunstan", 50);
            c.G.GiveItem(c.P, "rations", 2); // honey cake, wrapped for the road
            c.G.Quests.Set("bad_blood", 3, c.P);
        }

        static void SideWith(DCtx c, string winner, string loser)
        {
            c.G.World.AddFriendship(winner, 50);
            c.G.World.AddFriendship(loser, -50);
            c.G.Quests.Set("bad_blood", 3, c.P);
        }

        // =================================================================== Wick (Pip's barrel)

        static Dialogue Wick()
        {
            var d = new Dialogue("wick", c => c.G.World.Flag("wick_hired") ? "hired" : c.G.World.IsLocked("wick_cell") ? "cell" : "start");
            const string who = "Wick";
            d.Node("start", who, c => "It's not STOLEN. It's... borrowed. Long-term. Very long-term.",
                c => "A skinny teenager sitting on a barrel of Hearthfire Stout under an apple tree, a stack of tin cups beside him and a hand-painted sign: STOUT 2CP.")
                .Say("Why'd you take it?", "why")
                .Roll(Skill.Intimidation, 11, "Off the barrel. Now.", "why", "wick_scoff")
                .Bye("(Leave him for now.)");
            d.Node("wick_scoff", who, c => "Or what? You'll tell Pip? ...Please don't tell Pip.").Say("Why'd you take it?", "why");
            d.Node("why", who, c => "Mum's sick. The good medicine's at the temple and it costs silver, and nobody hires a kid who's 'trouble'. So. Two coppers a cup. I've got nearly enough.",
                c => "He's telling the truth. His knuckles are raw from rolling the barrel two miles in the dark.")
                .Say("Come with me. We'll sort this out with Pip together.", "mercy", doIt: c =>
                {
                    c.G.World.SetFlag("wick_mercy"); c.G.World.SetFlag("wick_caught");
                    c.G.Quests.Set("pips_barrel", 3, c.P);
                })
                .Say("Stealing is stealing. You're coming to the watch house.", "justice", doIt: c =>
                {
                    c.G.World.SetFlag("wick_justice"); c.G.World.SetFlag("wick_caught");
                    c.G.World.Lock("wick_cell", 2 * 24 * 60);
                    c.G.Quests.Set("pips_barrel", 3, c.P);
                });
            d.Node("mercy", who, c => "...Really? Okay. Okay. I'll roll it back. Will you talk to Pip first? She scares me. She's very small but she scares me.").Bye("I'll talk to her.");
            d.Node("justice", who, c => "...Fine. Can someone at least tell my mum where I am?",
                c => "He hops off the barrel and trudges toward the watch house. Captain Vale will keep him two nights, with a blanket and, knowing her, extra supper.").Bye("I'll tell her.");
            d.Node("hired", who, c => "Pip says I'm the best sweeper she's ever had! And Mother Elowen gave Mum the medicine at half price when Pip told her. Thanks. Really.").Bye("Proud of you, Wick.");
            d.Node("cell", who, c => "Two nights. The blanket's itchy. The Captain keeps giving me extra stew, though, so. Could be worse.").Bye("Hang in there.");
            return d;
        }

        // =================================================================== Lark and Tamsin (the ring couple)

        static Dialogue Lark()
        {
            var d = new Dialogue("lark", c => "start");
            const string who = "Lark";
            d.Node("start", who, c => "Flowers? Every bunch comes with a meaning. Yellow for friendship, red for courage, and blue for 'I couldn't decide so I picked blue'.",
                c => "A young woman with petals stuck in her braid, a basket on her hip, and a smile that seems to make the flowers perk up.")
                .Roll(Skill.Performance, 10, "(Pick a bouquet and make a speech about it)", "bouquet_yes", "bouquet_no", social: false)
                .Say("Do you know Tamsin, the fisher?", "tamsin")
                .Bye("They're lovely. Good day!");
            d.Node("bouquet_yes", who, c => "Oh, that's GOOD. You should sell flowers. Actually, don't, I need the business. Here, keep that one.",
                onEnter: c => c.G.World.AddFriendship("lark", 20)).Bye("Thank you!");
            d.Node("bouquet_no", who, c => "That's... a very sincere speech about a dandelion. I respect it.").Bye("It's a meaningful dandelion.");
            d.Node("tamsin", who, c => "Tamsin? Everyone knows Tamsin. Catches nothing, talks less, has nice hands. ...What? I notice hands. It's a florist thing.",
                c => "She goes pink and suddenly becomes very interested in a daisy.").Say("Of course.", "start");
            return d;
        }

        static Dialogue Tamsin()
        {
            var d = new Dialogue("tamsin", c => "start");
            const string who = "Tamsin";
            d.Node("start", who, c => "Mm. Fish aren't biting.",
                c => "A lanky, quiet fisher with a cap pulled low, holding a rod with no bait on the hook. They keep glancing at the green.")
                .Say("You look worried. Is something wrong?", "ring_lost", show: c => c.G.Quests.Stage("pond_ring") == 0)
                .Say("I found your ring!", "ring_back", show: c => c.G.Quests.Stage("pond_ring") == 2 && c.G.HasQuestItem(c.P, "pond_ring"),
                      doIt: c => { c.G.TakeQuestItem(c.P, "pond_ring"); c.G.World.AddFriendship("tamsin", 100); c.G.Quests.Set("pond_ring", 3, c.P); })
                .Roll(Skill.Insight, 11, "(Read Tamsin) You're not really here for the fish, are you?", "insight_yes", "insight_no", social: false)
                .Say("Any tips for fishing?", "fish")
                .Bye("Good luck.");
            d.Node("ring_lost", who, c => "...I had a ring. For someone. I was practicing what to say, on the pond's edge, and I got to the bit where you hold it out, and a duck startled me, and... plop.",
                c => "Tamsin stares at the pond like it personally betrayed them.",
                c => c.G.Quests.Start("pond_ring", c.P)).Say("I'll find it.", "start").Say("A duck?", "ring_duck");
            d.Node("ring_duck", who, c => "A very large duck. Don't laugh.").Say("I'm not laughing. I'll find it.", "start");
            d.Node("ring_back", who, c => "You found it. You actually... Right. Right! No more practicing. She's on the green. I'm going. Now. Before I lose my nerve. Come and watch?",
                c => "On the green, Tamsin gets down on one knee, opens their mouth, and Lark says \"YES\" before a single word comes out. The ducks applaud. (They don't. But it feels like they do.)")
                .Roll(Skill.Performance, 10, "(Lead the cheering)", "ring_cheer", "ring_cheer", social: false)
                .Bye("Congratulations!");
            d.Node("ring_cheer", who, c => "Ha! You'll be at the wedding. That's not a question.", onEnter: c => c.G.World.AddFriendship("lark", 25)).Bye("Wouldn't miss it.");
            d.Node("insight_yes", who, c => "...No. I'm here because from this spot you can see the flower stall. Don't. Don't say anything. I'm working up to it.",
                onEnter: c => c.G.World.AddFriendship("tamsin", 15)).Say("Working up to what?", "working");
            d.Node("working", who, c => "Saying something. Eventually. Possibly this year.").Bye("You've got this.");
            d.Node("insight_no", who, c => "I'm here for the fish. That's what fishers do. Fish.").Bye("Clearly.");
            d.Node("fish", who, c => "Bait helps. ...I should probably use bait.").Bye("Probably.");
            return d;
        }
    }
}
