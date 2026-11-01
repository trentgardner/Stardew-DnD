using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 5: Thistledown under the blight. Worried, never hopeless; unsettling, never grim. THE SECRET never
    /// leaks here: the shared dream is a door, a mountain and a slow knocking, nothing more.
    /// </summary>
    public static partial class DialogueDb
    {
        /// <summary>One worried line per blight stage (1, 2) for a townsperson, and the nodes it replaces.</summary>
        class Worry
        {
            public string[] Nodes;
            public string Stage1, Stage2;
            public Worry(string s1, string s2, params string[] nodes) { Stage1 = s1; Stage2 = s2; Nodes = nodes.Length > 0 ? nodes : new[] { "start" }; }
        }

        static readonly System.Collections.Generic.Dictionary<string, Worry> Worries = new System.Collections.Generic.Dictionary<string, Worry>
        {
            ["brannoc"] = new Worry(
                "Lamp oil's selling like it's free, and it's not free. Everyone wants a second lamp. Can't blame them: the nights feel longer.",
                "Sold my last lamp an hour ago. Sold the display lamp. Sold MY lamp. I'm going to be sitting in the dark counting money, which is a sentence I never thought I'd say.",
                "shop", "offhours"),
            ["elowen"] = new Worry(
                "The herbs come up grey at the tips. I've been singing to them. It isn't working, but it's helping me.",
                "Half the village has been in for something to help them sleep. I've run out of lavender. I've run out of chamomile. I'm starting to run out of kind things to say."),
            ["rhoswen"] = new Worry(
                "Another hearth out in Lower Sedge. I've got the watch walking the lanes with lanterns at night. It's not much. It's something people can see.",
                "Ash on the window sills every morning now. People are frightened, and frightened people do foolish things. I'm doubling the night watch. All four of us.",
                "after", "dragon", "progress"),
            ["tobin"] = new Worry(
                "My hearth went out. First time in forty years. Lit it again, mind. Took six tries and a lot of language.",
                "Hearth's out again. I've been sleeping at the Drake. Pip charges me for the stew, the cheek of it."),
            ["maddie"] = new Worry(
                "Everything's grey at the edges. Mum says it's just dust. It's not dust. Dust doesn't fall UP when you sneeze.",
                "I'm not scared. Buoy's scared. I'm holding his paw so HE'S not scared."),
            ["hesper"] = new Worry(
                "The bees are sluggish. They don't like the ash. Neither do I. Neither does Dunstan, and we AGREE on something, which is how you know it's bad.",
                "The hives have gone quiet. Not dead. Just quiet. Waiting, like the rest of us."),
            ["dunstan"] = new Worry(
                "Hesper's bees and mine are huddling together in the cold. Sharing a hive. I didn't know they could do that. Don't tell her I said it was sweet.",
                "We moved the hives into my barn, Hesper and I. Together. Don't make a thing of it. It's an emergency."),
            ["lark"] = new Worry(
                "Flowers don't last a day before the petals go grey. I've started selling them in the morning. They're still beautiful in the morning.",
                "Tamsin keeps saying we should move the wedding up. In case. In case of what, I asked. They didn't answer."),
            ["tamsin"] = new Worry(
                "Fish have gone deep. The water's warmer at the bottom than the top. That's backwards.",
                "Fish aren't biting. Haven't for a week. I still come down every morning. It's quiet. Lark worries."),
            ["wick"] = new Worry(
                "I'm not scared of the ash. I put some in a jar. It's my ash now. ...Mum made me throw it out.",
                "Mum lets me sleep in her room now. I tell her it's so SHE'S not scared. It's mostly so she's not scared.",
                "start", "hired"),
            ["hobb"] = new Worry(
                "The river's running grey under the scum. The mill wheel grinds it into the flour. I sift twice now. Then I sift it again.",
                "Haven't milled in a week. Can't sell grey flour. Can't eat it either, though I've tried. Tasted like a cold fireplace.",
                "start", "done", "clear"),
            ["wren"] = new Worry(
                "Tolls are down. Nobody's travelling the road. The few that do are going one way: away from the mountains.",
                "I've stopped charging the toll. Don't tell anyone. People leaving the valley have enough troubles without me."),
        };

        /// <summary>Who can tell the dream, and in what words. The dream itself is always the same.</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> Dreamers = new System.Collections.Generic.Dictionary<string, string>
        {
            ["elowen"] = "I keep dreaming the same thing. A great grey door, deep under the mountain, warm to the touch like a loaf from the oven. And something on the other side, knocking. Slow. Steady. As if it had all the time in the world.",
            ["tobin"] = "Dreams. Same one every night. A door, under the mountain, big as a barn. Grey stone. Warm. And knocking. Not loud. Just... patient. Wakes me up every time.",
            ["hesper"] = "Do you dream? I dream of a door. Under the Wyrmback. Grey, and warm when you touch it, and something behind it knocking, slow as a heartbeat. Dunstan dreams it too. We compared.",
            ["wren"] = "A door. That's all. A grey door under the mountain, warm, and something knocking on the far side, slow and steady. Every night this week. Half the travellers through here say they've dreamed it too.",
            ["hobb"] = "I dreamed of a door last night. Under the mountain. My father dreamed of one too, when I was a boy, he told me. Grey stone, warm, and a slow knocking behind it. He said it went away. It came back.",
        };

        public const string SharedDreamLore = "shared_dream";

        static void RhoswenAshJob(Dialogue d)
        {
            const string who = "Captain Rhoswen Vale";
            d.Node("ash_job", who,
                c => "Since you ask. Somebody's been marking doors with ash. A hand, pressed flat, right at eye height. Ironkettle's, the Drake's, and... yours. People are frightened enough without this. Find out who."
                    + (c.G.World.Flag("banner_crown") ? " And no, I'm not telling the Redoubt. Not yet. This is Thistledown's business." : ""),
                c => "She looks very tired.",
                c => c.G.Quests.Start("ash_on_doors", c.P))
                .Bye("We'll find out.");
            foreach (var id in new[] { "after", "dragon", "progress" })
                if (d.Nodes.TryGetValue(id, out var n))
                    BeforeBye(n, x => x.Say("Anything I can help with, Captain?", "ash_job", show: c => c.G.World.Act >= 2 && c.Blight >= 2 && c.G.Quests.Stage("ash_on_doors") == 0));
        }

        static Dialogue BlightDialogue(string id)
        {
            switch (id)
            {
                case "teodor": return Teodor();
                case "teodor_caught": return TeodorCaught();
                case "ally_holloway": return Ally(id, "Sergeant Dace Holloway",
                    c => "Marsh sent me. Said I'd been asking to get out of that checkpoint, so here I am, out of it. Careful what you wish for, my mother used to say. She also said wear a scarf. I'm wearing the scarf.",
                    "He's brought a crate of the Redoubt's best medicine, and a list, in Marsh's handwriting, of things he's not allowed to let you do. It's a long list.",
                    c => c.G.GiveItem(c.P, "potion_greater_healing", 3));
                case "ally_perrin": return Ally(id, "Perrin Quillby",
                    c => "Ashby sent me. With NOTES. Forty pages. I read them on the road and now I'm frightened of the mountain in a much more informed way. Here, he said to give you these. He said, and I quote, 'tell them to come back'.",
                    "Perrin is terrified, and determined, and has ink on his face.",
                    c => { c.G.GiveItem(c.P, "ember_of_return"); c.G.GiveItem(c.P, "potion_greater_healing", 2); });
                case "ally_thimble": return Ally(id, "Mister Thimble",
                    c => "The Courts want their bargain kept, and I want to see a volcano! Everyone wins. Here: dew from the Blossom throne. Drink it when things get hot. They will get hot."
                        + (c.G.World.Flag("fae_clause") ? " And remember, clever-boots: the little flame owes us one." : ""),
                    "He's sitting on a pebble that's floating slightly above the ash, so his waistcoat doesn't get dirty.",
                    c => c.G.GiveItem(c.P, "bloom_draught", 3));
                case "ally_odo": return Ally(id, "Odo Fenn",
                    c => "Calloway said, and I quote, 'Don't die, Odo.' So I won't. I scouted the road while I waited: anything worth finding round here, I've marked it for you. Look for the sparkles. That's a hunter's joke. They're scratches on rocks.",
                    "Odo has a crossbow, a new hood that fits, and a very serious face he keeps forgetting to make.",
                    c =>
                    {
                        c.G.GiveItem(c.P, "potion_greater_healing", 2);
                        foreach (var s in UnityEngine.Object.FindObjectsByType<SecretInteractable>(UnityEngine.FindObjectsSortMode.None))
                            if (s.Def != null && c.P.Zone != null && c.P.Zone.Objects.Contains(s.gameObject) && !c.G.World.Flag("secret:" + s.Def.Id)) s.MarkSniffed();
                    });
                default: return null;
            }
        }

        // =================================================================== Your side's ally at the Last Camp

        /// <summary>The ally your side sent: their greeting gives the party one gift, once (World flag ally_gift).</summary>
        /// <summary>World flag: your side's ally has given their last help at the Heart (once for the party).</summary>
        public const string AllyBoonFlag = "ally_boon";

        /// <summary>What each side's ally does for you at the Heart's threshold, before the Warden.</summary>
        static readonly System.Collections.Generic.Dictionary<string, (System.Func<DCtx, string> line, string look, string toast, System.Action<DCtx> apply)> Boons =
            new System.Collections.Generic.Dictionary<string, (System.Func<DCtx, string>, string, string, System.Action<DCtx>)>
            {
                ["ally_holloway"] = (c => "This is as far as I go. Somebody's got to hold the tunnel, and I've got a scarf. Here: the Redoubt's last trick. Drink it, it's horrible, it'll keep you standing.",
                    "He hands round a flask of something that tastes like boiled boots and makes your ribs feel armoured.", "Holloway's tonic: +15 temporary HP each.",
                    c => { foreach (var p in c.G.Players.Where(x => x.IsReady && x.Character != null)) p.Character.TempHP = System.Math.Max(p.Character.TempHP, 15); }),
                ["ally_perrin"] = (c => "Ashby gave me this. A ward. He said, and I quote, 'it will hold for one fight, Perrin, and not one minute more, so do not waste it on a staircase.' Hold still. This tingles.",
                    "Perrin traces a circle in the air around each of you. It hangs there for a moment, shining, then sinks into your skin.", "Ashby's ward: you resist fire for the fight with the Warden.",
                    c => c.G.World.SetFlag(WardActiveFlag)),
                ["ally_thimble"] = (c => "Oh, I'm not going in THERE. But I'll do you a little luck. The Courts' kind. It always comes back to us in the end, but you won't mind that today.",
                    "He blows on his fingers and flicks each of you on the nose. It feels like the first warm day of spring.", "The Courts' luck: Heroic Inspiration.",
                    c => { foreach (var p in c.G.Players.Where(x => x.IsReady && x.Character != null)) p.Character.HeroicInspiration = true; }),
                ["ally_odo"] = (c => "I watched it from the ledge for an hour. It favours its left side. It looks at the biggest one of you first. Calloway says: first blow's the one it never forgets. Make it count.",
                    "Odo draws you a quick map in the ash with his crossbow bolt, and taps the Warden's left flank twice.", "Odo's eye: your first attack on the Warden has Advantage.",
                    c => c.G.World.SetFlag(WardenMarkedFlag)),
            };

        public const string WardActiveFlag = "ward_active", WardenMarkedFlag = "warden_marked";

        static Dialogue Ally(string id, string who, System.Func<DCtx, string> greeting, string look, System.Action<DCtx> gift)
        {
            var d = new Dialogue(id, c => In(c, "emberhorn_heart") ? (c.G.World.Flag(AllyBoonFlag) ? "heart_again" : "boon") : c.G.World.Flag(WorldDb.AllyGift) ? "again" : "greet");
            if (Boons.TryGetValue(id, out var b))
            {
                d.Node("boon", who, b.line, c => b.look, c =>
                {
                    if (c.G.World.Flag(AllyBoonFlag)) return;
                    c.G.World.SetFlag(AllyBoonFlag);
                    b.apply(c);
                    foreach (var p in c.G.Players.Where(x => x.IsReady)) p.Hud.Toast(b.toast, UIKit.Gold);
                }).Bye("Thank you.");
                d.Node("heart_again", who, c => "Go on. We'll be right here. Somebody has to be.").Bye("Thank you.");
            }
            d.Node("greet", who, greeting, c => look, c =>
            {
                if (c.G.World.Flag(WorldDb.AllyGift)) return;
                c.G.World.SetFlag(WorldDb.AllyGift);
                gift(c);
            }).Bye("Thank you.");
            d.Node("again", who,
                c => c.G.Quests.Stage(Answers.Id) >= Answers.Gate ? "You've seen the gate? Then you know. Rest first. The mountain isn't going anywhere. That's rather the problem." : "The camp's yours whenever you need it. The fire stays lit. Somebody has to keep it lit.")
                .Bye("See you by the fire.");
            return d;
        }

        // =================================================================== Teodor Brisk: frightened and wrong, not evil

        static Dialogue Teodor()
        {
            var d = new Dialogue("teodor", c => "start");
            const string who = "Teodor Brisk";
            d.Node("start", who,
                c => c.G.World.Flag("teodor_jailed") ? "I'm not going anywhere. Tell the Captain I'm sorry. I meant to tell her myself. I keep not managing it."
                    : c.G.World.Flag("teodor_redeemed") ? "Candles? These stay lit a little longer. Don't ask me how. I think I'm just trying harder."
                    : c.Blight >= 2 ? "Candles! Long-burning, good tallow. Everybody needs more light these days. Don't they. Don't they just."
                    : "Candles, lamps, tapers! Light for every room, a penny a night!",
                c => "A thin, tidy man in a waxed apron, with ink-black hair and a candlemaker's burn across one palm.")
                .Bye("Not today, thanks.");
            return d;
        }

        static void AshDone(DCtx c, string fate)
        {
            if (c.G.Quests.Done("ash_on_doors")) return;
            c.G.World.SetFlag(fate);
            if (fate != "teodor_redeemed") c.G.World.SetFlag(WorldDb.TeodorAway);
            c.G.Quests.Set("ash_on_doors", 3, c.P);
        }

        static Dialogue TeodorCaught()
        {
            var d = new Dialogue("teodor_caught", c => "caught");
            const string who = "Teodor Brisk";
            d.Node("caught", who,
                c => "Don't- please. Please. It's only ash. It doesn't hurt anyone. It's a sign, that's all, so she knows which doors are friendly. The hearths are going out. Mine went first. A man at the Ford told me: the ones who meet at the old beacon say when she wakes, the cold ends. I just want the cold to end.",
                c => "In the dark of the square, a thin figure with a bowl of ash, frozen halfway to a door. It's Teodor the chandler. He's shaking.")
                .Roll(Skill.Persuasion, 15, "The cold isn't ending, Teodor. You're frightening your neighbours.", "redeemed", "stubborn", onSuccess: c => AshDone(c, "teodor_redeemed"))
                .Roll(Skill.Insight, 14, "(Insight) Who told you that, really?", "redeemed_creed", "stubborn", onSuccess: c => { LoreDb.Find("ashsworn_creed", c.P); AshDone(c, "teodor_redeemed"); })
                .Say("Come with me to the Captain.", "jailed", doIt: c => AshDone(c, "teodor_jailed"))
                .Say("Go. Don't come back.", "fled", doIt: c => AshDone(c, "teodor_fled"));
            d.Node("stubborn", who, c => "You don't understand. Nobody understands. It's so COLD.",
                c => "He clutches the bowl to his chest. He isn't going to be talked round tonight.")
                .Say("Come with me to the Captain.", "jailed", doIt: c => AshDone(c, "teodor_jailed"))
                .Say("Go. Don't come back.", "fled", doIt: c => AshDone(c, "teodor_fled"));
            d.Node("redeemed", who, c => "...Frightening them. I- I was trying to keep them warm. I'll scrub the doors. Tonight. All of them.",
                c => "He puts the bowl down very carefully, as if it might break, and starts to cry, quietly, with relief.").Bye("Get some sleep, Teodor.");
            d.Node("redeemed_creed", who, c => "I didn't ask his name. Grey coat. Kind voice. He said they light no fires of their own, they just wait for hers. I wanted so badly to believe him.",
                c => "He sets the bowl down and stares at his burned hand.").Bye("Get some sleep, Teodor.");
            d.Node("jailed", who, c => "...All right. All right. She'll be fair. The Captain's always fair.",
                c => "He walks beside you to the watch house without a word. Captain Vale meets you at the door, takes one look, and sighs like someone who has been let down by a friend.").Bye("(Leave him with the watch)");
            d.Node("fled", who, c => "Thank you. Thank you. I'll go somewhere warm.",
                c => "He's gone into the dark before you've finished deciding whether that was the right thing to do.").Bye("(Watch him go)");
            return d;
        }

        static void AddBlightLines(string id, Dialogue d)
        {
            if (id == "rhoswen") RhoswenAshJob(d);
            if (!Worries.TryGetValue(id, out var w)) return;
            foreach (var nodeId in w.Nodes)
            {
                if (!d.Nodes.TryGetValue(nodeId, out var n) || n.Text == null) continue;
                var inner = n.Text;
                n.Text = c => c.G.World.Act >= 2 && c.Blight >= 2 ? w.Stage2 : c.G.World.Act >= 2 && c.Blight >= 1 ? w.Stage1 : inner(c);
            }
            if (Dreamers.TryGetValue(id, out var dream))
            {
                string flag = "dream:" + id;
                var start = d.Nodes[w.Nodes[0]];
                BeforeBye(start, n => n.Say("You look tired.", "dream", show: c => c.G.World.Act >= 2 && c.Blight >= 2 && !c.G.World.Flag(flag)));
                d.Node("dream", start.Speaker, c => dream, c => null, c =>
                {
                    c.G.World.SetFlag(flag);
                    if (Dreamers.Keys.Count(k => c.G.World.Flag("dream:" + k)) >= 3) LoreDb.Find(SharedDreamLore, c.P);
                }).Say("(Back)", w.Nodes[0]);
            }
        }
    }
}
