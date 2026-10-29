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
                .Say("You and your neighbor don't get along?", "feud")
                .Roll(Skill.Nature, 11, "Your bees look healthy. What do you feed them?", "bees_yes", "bees_no", social: false)
                .Bye("Good day, Mrs Meadowsweet.");
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
                .Say("What's the story with the fence?", "feud")
                .Roll(Skill.Insight, 12, "(Read him) You don't actually seem angry.", "insight_yes", "insight_no", social: false)
                .Bye("Good day, Mr Burrow.");
            d.Node("feud", who, c => "Story? There's no story. There's a fence, and there's a woman who thinks it walks. Fences don't walk. ...Mine doesn't.")
                .Say("Right.", "start");
            d.Node("insight_yes", who, c => "...Angry? No. Tired, mostly. We used to be friends, you know. Before. Never mind.",
                c => "For a moment the bluster drops and he just looks across the stream at her cottage, like a man looking at an old photograph.",
                c => c.G.World.AddFriendship("dunstan", 15)).Say("Before what?", "start");
            d.Node("insight_no", who, c => "Are you squinting at me? Don't squint at me. The bees don't like squinting.").Say("Sorry.", "start");
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
                .Roll(Skill.Insight, 11, "(Read Tamsin) You're not really here for the fish, are you?", "insight_yes", "insight_no", social: false)
                .Say("Any tips for fishing?", "fish")
                .Bye("Good luck.");
            d.Node("insight_yes", who, c => "...No. I'm here because from this spot you can see the flower stall. Don't. Don't say anything. I'm working up to it.",
                onEnter: c => c.G.World.AddFriendship("tamsin", 15)).Say("Working up to what?", "working");
            d.Node("working", who, c => "Saying something. Eventually. Possibly this year.").Bye("You've got this.");
            d.Node("insight_no", who, c => "I'm here for the fish. That's what fishers do. Fish.").Bye("Clearly.");
            d.Node("fish", who, c => "Bait helps. ...I should probably use bait.").Bye("Probably.");
            return d;
        }
    }
}
