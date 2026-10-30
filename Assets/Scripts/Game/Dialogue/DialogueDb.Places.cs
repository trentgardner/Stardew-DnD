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
    }
}
