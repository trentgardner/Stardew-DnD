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

        static void AddBlightLines(string id, Dialogue d)
        {
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
