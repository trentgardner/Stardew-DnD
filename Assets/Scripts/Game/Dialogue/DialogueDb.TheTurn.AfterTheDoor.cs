using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 5: the questioning on the Sunstair. EXEMPT from the secret guard: the five speakers who use it exist only
    /// while Turn.Questioning is set, which comes after WorldDb.MetHer and the meeting. Real dialogue with real dice: the King is
    /// made to confess on every route, each force says what it wants, and the party may win leaders over. Only then can it decide.
    /// Every roll is one-shot (social: false), because one leader's failed Persuasion mustn't lock another's.
    /// </summary>
    public static partial class DialogueDb
    {
        static Dialogue TurnDialogue(string id) => id == "questioning" ? Questioning() : null;

        static bool AllHeard(WorldState w) => Turn.Forces.All(f => w.Flag(Turn.Heard(f)));

        static Dialogue Questioning()
        {
            var d = new Dialogue("questioning", c =>
            {
                var w = c.G.World;
                return !w.Flag(Turn.Confessed) ? "open" : !AllHeard(w) ? "voices" : "decide";
            });
            string Name(DCtx c) => c.G.World.EggHatched ? c.G.World.DragonName : "the dragon";
            const string king = "King Aldric", marsh = "Marsh", ashby = "Ashby", sorrel = "Sorrel", calloway = "Calloway";
            AdvantageState If(bool yes) => yes ? AdvantageState.Advantage : AdvantageState.Normal;

            // Every line said at the questioning reaches the other player's log too.
            DNode N(string node, string speaker, System.Func<DCtx, string> text, System.Func<DCtx, string> narration = null, System.Action<DCtx> onEnter = null)
            {
                return d.Node(node, speaker, text, narration, c =>
                {
                    onEnter?.Invoke(c);
                    string said = text?.Invoke(c);
                    c.G.EchoToOthers(c.P, string.IsNullOrEmpty(said) ? narration?.Invoke(c) : $"{speaker}: \"{said}\"");
                });
            }

            N("open", king, c => "You went below. Into a place no one was meant to go. Tell me what you found.",
                c => "The square holds its breath.")
                .Say("The truth. All of it.", "truth")
                .Roll(Skill.Deception, 16, "Nothing. Old iron and dust.", "lie_ok", "lie_bad", social: false);
            N("lie_ok", king, c => "...Thank the stars.",
                c => $"He believes you. Then {Name(c)} growls, low, at the King. Not at anyone else. And every ever-lamp around the square flickers at once.")
                .Say("(Tell them the truth.)", "truth");
            N("lie_bad", sorrel, c => "Oh, that was *dreadful*. Tell them properly.",
                c => "Somebody in the crowd laughs, and stops.")
                .Say("(Tell them the truth.)", "truth");
            N("truth", null, c => "",
                c => "You tell them. The Deep, the chains, the twelve sleeping heads, the grey door. And what's behind it: a little girl called Edda, who has been a child for a thousand years, and who asked you if you were real. The square goes very quiet.")
                .Say("The iron ring. Every King. Every year, a present, and \"sorry\".", "confess", show: c => c.G.World.Flag("ring_link"))
                .Roll(Skill.Insight, 14, "(Insight) Watch the King's face.", "confess", "deny", social: false);
            N("deny", king, c => "Lies. A monster wearing a face. That is what it does.",
                c => "His hand has gone to the iron ring on his finger. He doesn't seem to know it.")
                .Roll(Skill.Intimidation, 15, "Look at your hand, Majesty. Look at the ring.", "confess", "roar", social: false)
                .Roll(Skill.Persuasion, 15, "Your people deserve the truth. So does she.", "confess", "roar", social: false)
                .Say($"(Let the dragon answer.)", "roar");
            N("roar", null, c => "",
                c => $"Before anyone can speak, {Name(c)} roars, and the King flinches, and that's answer enough for everyone watching.")
                .Say("(Wait.)", "confess");
            N("confess", king,
                c => "...Yes. I knew. Every King knows. A thousand years ago my house sealed the fire of the world inside a child, to break the Ember Pact and keep the power for ourselves. The Ashen Queen is the fire. She is only what holds it.",
                c => "He sits down on the Sunstair, quite suddenly, like a much older man.",
                c => c.G.World.SetFlag(Turn.Confessed))
                .Say("Go on.", "confess2");
            N("confess2", king,
                c => "The lamps of this city burn on it still. Every King is told on his crowning night, and every year he goes down with a present, and says he is sorry, and does nothing. I am sorry. I have done nothing.",
                c => "Nobody in the square says anything at all.")
                .Say("What happens if the seal breaks?", "voices");
            N("voices", null, c => "",
                c => AllHeard(c.G.World) ? "Everyone has said their piece. Everyone is looking at you." : "Everyone wants to speak at once.")
                .Say("Marsh.", "marsh", show: c => !c.G.World.Flag(Turn.Heard("crown")))
                .Say("Ashby.", "ashby", show: c => !c.G.World.Flag(Turn.Heard("academy")))
                .Say("Sorrel.", "sorrel", show: c => !c.G.World.Flag(Turn.Heard("fae")))
                .Say("Calloway.", "calloway", show: c => !c.G.World.Flag(Turn.Heard("hunters")))
                .Say("Decide.", "decide", show: c => AllHeard(c.G.World));

            bool Side(DCtx c, string f) => c.G.World.Flag("banner_" + f);

            N("marsh", marsh, c => "I swore an oath to protect this realm. I thought that meant him. Seal it again. Keep people safe. Let the King answer for the rest, after.",
                c => "She doesn't look at the King.", c => c.G.World.SetFlag(Turn.Heard("crown")))
                .Roll(Skill.Persuasion, 15, "Your oath was to the realm, not to the lie.", "sway_crown_ok", "sway_crown_bad", social: false, show: c => !Side(c, "crown"))
                .Roll(Skill.Persuasion, 12, "Your oath was to the realm, not to the lie.", "sway_crown_ok", "sway_crown_bad", social: false, show: c => Side(c, "crown"))
                .Say("Thank you, Marsh.", "voices");
            N("ashby", ashby, c => "I have spent my life on the seal. I was *proud* of it. Seal her. Properly, this time, so she sleeps and doesn't dream. It's the kindest thing left. I can do it. I know how.",
                c => "He takes a long breath before the last part.", c => c.G.World.SetFlag(Turn.Heard("academy")))
                .Roll(Skill.Arcana, 15, "Show him what you saw: the fire isn't her.", "sway_academy_ok", "sway_academy_bad", social: false,
                      show: c => !Side(c, "academy"), adv: c => If(c.G.World.Flag("seal_understood")))
                .Roll(Skill.Arcana, 12, "Show him what you saw: the fire isn't her.", "sway_academy_ok", "sway_academy_bad", social: false,
                      show: c => Side(c, "academy"), adv: c => If(c.G.World.Flag("seal_understood")))
                .Say("Thank you, Ashby.", "voices");
            N("sorrel", sorrel, c => "The Pact was a promise, and *that* crown broke it. Let her out. The Courts remember what was owed.",
                c => "She nods at the King without looking at him.", c => c.G.World.SetFlag(Turn.Heard("fae")))
                .Say("(The favour the dragon owes the Courts.)", "sway_fae_favour", show: c => c.G.World.Flag("fae_clause"),
                     doIt: c => { c.G.World.SetFlag(Turn.FaeFavourCalled); Sway(c, "fae"); })
                .Roll(Skill.Insight, 14, "What do the Courts actually want?", "sway_fae_ok", "sway_fae_bad", social: false)
                .Say("Thank you, Sorrel.", "voices");
            N("calloway", calloway, c => "I've buried forty hunters to fire. I don't care how small it looks. End it, while it's chained.",
                c => "Her crossbow is on her back. Her hand isn't far from it.", c => c.G.World.SetFlag(Turn.Heard("hunters")))
                .Roll(Skill.Persuasion, 16, "She didn't burn your villages. *They* did, by keeping her.", "sway_hunters_ok", "sway_hunters_bad", social: false,
                      show: c => !Side(c, "hunters"), adv: c => If(c.G.World.Flag("wyrmwatch_belled")))
                .Roll(Skill.Persuasion, 13, "She didn't burn your villages. *They* did, by keeping her.", "sway_hunters_ok", "sway_hunters_bad", social: false,
                      show: c => Side(c, "hunters"), adv: c => If(c.G.World.Flag("wyrmwatch_belled")))
                .Roll(Skill.Intimidation, 16, "Put the crossbow down, Huntmaster.", "sway_hunters_ok", "sway_hunters_bad", social: false,
                      show: c => !Side(c, "hunters"), adv: c => If(c.G.World.Flag("wyrmwatch_belled")))
                .Roll(Skill.Intimidation, 13, "Put the crossbow down, Huntmaster.", "sway_hunters_ok", "sway_hunters_bad", social: false,
                      show: c => Side(c, "hunters"), adv: c => If(c.G.World.Flag("wyrmwatch_belled")))
                .Say("Thank you, Calloway.", "voices");

            void SwayNodes(string f, string speaker, string ok, string bad)
            {
                N($"sway_{f}_ok", speaker, c => ok, null, c => Sway(c, f)).Say("(Go on.)", "voices");
                N($"sway_{f}_bad", speaker, c => bad).Say("(Go on.)", "voices");
            }
            SwayNodes("crown", marsh, "...The realm. Not the lie. Yes.", "I hear you. I don't know if I can do it.");
            SwayNodes("academy", ashby, "Oh. Oh, I've been so *stupid*.", "I want to believe you. I've wanted to believe a great many things.");
            SwayNodes("fae", sorrel, "Freedom it is, then. How fun.", "Oh, what we *want*. Wouldn't you like to know.");
            SwayNodes("hunters", calloway, "...Fine. I'll hear you out. I won't promise more.", "Words. I've heard words over a lot of graves.");
            N("sway_fae_favour", sorrel, c => $"Ah. Yes. We'll take it now: the Courts stand where {Name(c)} stands. Don't say we never gave you anything.",
                c => "Somewhere in the market, every flower turns its face towards you.")
                .Say("(Go on.)", "voices");

            N("decide", king, c => "Then it falls to you. Everyone in this square will follow what you choose, or fight it. What will you do?",
                c => $"{(c.G.World.EggHatched ? c.G.World.DragonName : "The dragon")} is watching you, not the King.")
                .Say("End her.", doIt: c => c.G.OfferEnding(c.P, Turn.End))
                .Say("Seal her away.", doIt: c => c.G.OfferEnding(c.P, Turn.Seal))
                .Say("Save her.", doIt: c => c.G.OfferEnding(c.P, Turn.Save), show: c => TurnDb.SaveGateMissing(c.G.World).Count == 0)
                .Say("Save her. (You're not ready: see what's missing.)", "gate_short", show: c => TurnDb.SaveGateMissing(c.G.World).Count > 0)
                .Say("Not yet. We need time.", end: true);
            N("gate_short", null, c => "",
                c => "You can't do it yet. Not like this. You'd need:\n" + string.Join("\n", TurnDb.SaveGateMissing(c.G.World).Select(m => "• " + m)))
                .Say("(Back.)", "decide");
            return d;
        }

        static void Sway(DCtx c, string force)
        {
            c.G.World.SetFlag(Turn.Swayed(force));
            foreach (var p in c.G.Players.Where(x => x.IsReady))
                c.G.AwardOnce(p, "sway:" + force, 1000, $"winning {FactionDb.NameOf(force)} over");
        }
    }
}
