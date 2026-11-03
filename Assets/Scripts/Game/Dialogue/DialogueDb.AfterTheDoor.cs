using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: Edda. EXEMPT from the secret guard: she can only be spoken to beyond the grey door, once WorldDb.MetHer
    /// is set. Her voice: short, a little formal, curious. She is blameless and doesn't understand. Never creepy, never villainous.
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue AfterTheDoorDialogue(string id) => id == "edda" ? Edda() : null;

        static Dialogue Edda()
        {
            var d = new Dialogue("edda", c => c.G.World.Flag("promised_her") ? "again" : "start");
            const string who = "Edda";
            string Dragon(DCtx c) => c.G.World.DragonName;
            d.Node("start", who,
                c => "Are you real? You're not him. He only comes once a year, and it isn't the day.",
                c => $"She hasn't taken her hand off {Dragon(c)}'s nose.")
                .Say("We're real. Who's \"him\"?", "him")
                .Say("What's your name?", "name")
                .Say("How long have you been here?", "here");
            d.Node("name", who,
                c => "Edda. I think. Nobody's said it in a long time. Will you say it? ...Thank you. That's nice.",
                c => "She says it over to herself, quietly, as if she's putting it somewhere safe.")
                .Say("How long have you been here?", "here")
                .Say("Who's \"him\"?", "him")
                .Say("Is it warm in here because of you?", "fire")
                .Say("Do you know the Ashen Queen?", "queen");
            d.Node("here", who,
                c => "Since the big ones lay down. They sleep round the house, so I'm not on my own. I knock sometimes. So somebody knows I'm here.",
                c => "She points at the drawings on the wall: twelve sleeping dragons, in a ring.")
                .Say("That was us. We knocked back.", "knock", show: c => c.G.World.Flag("knocked_back"))
                .Say("Who's \"him\"?", "him")
                .Say("Is it warm in here because of you?", "fire");
            d.Node("knock", who,
                c => "That was *you*? I thought I dreamed it. Nobody ever knocks back.",
                c => $"She looks at you the way {Dragon(c)} looked at you the day it hatched.",
                c => { if (!c.G.World.Flag("edda_knock_bond")) { c.G.World.SetFlag("edda_knock_bond"); DragonPet.AddBond(c.P, 10, "she was so happy somebody knocked back"); } })
                .Say("Who's \"him\"?", "him")
                .Say("Is it warm in here because of you?", "fire");
            d.Node("him", who,
                c => "The man with the iron ring. He brings a present and says sorry. He never says what for. He's a different man, sometimes. Older. Then younger.",
                c => "She nods at the row of presents along the wall. There are a great many of them.")
                .Roll(Skill.History, 13, "(History) \"It's the King. Every King.\"", "ring", "fire", onSuccess: c => c.G.World.SetFlag("ring_link"),
                      show: c => c.G.World.LoreFound.Contains("the_kings_ring"))
                .Say("Is it warm in here because of you?", "fire")
                .Say("Do you know the Ashen Queen?", "queen");
            d.Node("ring", who,
                c => "A king? Like in stories? ...Then why is he sorry?",
                c => "Nobody has an answer for her. Not one you'd want to say out loud.")
                .Say("Is it warm in here because of you?", "fire")
                .Say("Do you know the Ashen Queen?", "queen");
            d.Node("fire", who,
                c => "It gets hot when I'm scared. Or sad. I try not to be. The lamps up there flicker when I cry, he said once. Is that true?",
                c => "The lamps in the room flicker, very slightly, as she says it.")
                .Roll(Skill.Arcana, 15, "(Arcana) Look at the fire in her.", "seal", "leave", onSuccess: c => c.G.World.SetFlag("seal_understood"))
                .Roll(Skill.Insight, 12, "(Insight) Is she telling the truth?", "truth", "leave", onSuccess: c => c.G.World.SetFlag("her_truth"))
                .Say("Do you know the Ashen Queen?", "queen")
                .Say("We have to go. For now.", "leave");
            d.Node("seal", null,
                c => "",
                c => "You see it, the way you'd see a candle through a hand: a fire the size of the world, folded up inside a little girl and held there by the iron, the chains, the wards, the mountains themselves. Every lock in the Deep is for this. It's all for her.")
                .Say("Do you know the Ashen Queen?", "queen")
                .Say("We have to go. For now.", "leave");
            d.Node("truth", null,
                c => "",
                c => "Every word. She has no idea what she is, or why she's here. She thinks this is just where she lives.")
                .Say("Do you know the Ashen Queen?", "queen")
                .Say("We have to go. For now.", "leave");
            d.Node("queen", who,
                c => "Who's that? Is she nice? ...Is she the one everybody's scared of? He said people were scared. I'm sorry they're scared.",
                c => "She says it the way you'd say sorry for somebody else's bad dream.")
                .Say("We have to go. For now.", "leave");
            d.Node("leave", who,
                c => "Are you going? Can I come?",
                c => "She walks with you to the door, and stops at the threshold like it's the edge of a cliff. \"I can't. It holds me. It's alright. It's always held me. Will you come back?\"")
                .Say("We'll come back. I promise.", doIt: c => c.G.World.SetFlag("promised_her"), end: true)
                .Say("We'll try.", end: true);
            d.Node("again", who,
                c => $"You came back! {Dragon(c)} came back!",
                c => $"{Dragon(c)} is already lying down beside her, as if it never left.")
                .Say("Who's \"him\"?", "him")
                .Say("Is it warm in here because of you?", "fire")
                .Say("Do you know the Ashen Queen?", "queen")
                .Say("We have to go. We'll be back.", "leave");
            return d;
        }
    }
}
