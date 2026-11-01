using DnD.Rules;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 6: the Warden of the Emberhorn. The illusion of peace: real dialogue and real dice, and for a moment it seems
    /// to work, but every path ends in the fight (the user's deliberate exception). Rolling well always pays something.
    /// THE SECRET: the Warden speaks of the door, the lock and the fire, never of what lies beyond the door.
    /// </summary>
    public static partial class DialogueDb
    {
        public const string WardenHesitated = "warden_hesitated", WardenSparesDragon = "warden_spares_dragon", WardenWeakness = "warden_weakness";
        static readonly string[] ParleyTries = { "warden_try_persuade", "warden_try_show", "warden_try_insight" };

        static Dialogue VolcanoDialogue(string id) => id == "warden_parley" ? WardenParley() : null;

        static int Tries(DCtx c) => System.Linq.Enumerable.Count(ParleyTries, c.G.World.Flag);
        static bool DragonHere(DCtx c) => c.G.Dragon != null && c.G.DragonAgent != null && c.G.DragonAgent.Actor.Zone == c.P.Zone;

        /// <summary>The checks still open, plus "it's time" once two are spent: the talk ends, as it must, in a fight.</summary>
        static DNode ParleyOptions(DNode n)
        {
            n.Roll(Skill.Persuasion, 18, "You've kept the fire long enough. Rest.", "hesitates", "unmoved",
                onSuccess: c => c.G.World.SetFlag(WardenHesitated), onFailure: c => { },
                show: c => !c.G.World.Flag(ParleyTries[0]) && Tries(c) < 2);
            n.Roll(Skill.AnimalHandling, 15, "(Bring the young dragon forward)", "recognises", "unmoved",
                onSuccess: c => c.G.World.SetFlag(WardenSparesDragon), social: false,
                show: c => !c.G.World.Flag(ParleyTries[1]) && Tries(c) < 2 && DragonHere(c));
            n.Roll(Skill.Insight, 14, "(Insight) Watch its throat as it speaks.", "weakness", "unmoved",
                onSuccess: c => c.G.World.SetFlag(WardenWeakness), social: false,
                show: c => !c.G.World.Flag(ParleyTries[2]) && Tries(c) < 2 && c.G.World.LoreFound.Contains("emberhorn_warden") && !c.G.World.Flag(WardenWeakness));
            n.Say("(Draw your weapon)", "turn");
            return n;
        }

        static Dialogue WardenParley()
        {
            var d = new Dialogue("warden_parley", c => "speak");
            const string who = "The Warden of the Emberhorn";
            d.Node("speak", who,
                c => "Sparks. Go home. The door is shut. I am the lock. I have been the lock for a thousand years, and I am so very tired, and still I hold.",
                c => "Its voice is a landslide, slow and deep. Grey ash sifts from its scales when it moves. Its eyes are the colour of a fire that has almost gone out." +
                     (DragonHere(c) ? $" They find {c.G.World.DragonName}, and something in them flickers." : ""));
            ParleyOptions(d.Nodes["speak"]);
            // Every roll marks which approach was tried (pass or fail), so each can be tried once.
            d.Nodes["speak"].Options[0].OnSuccess += c => c.G.World.SetFlag(ParleyTries[0]);
            d.Nodes["speak"].Options[0].OnFailure += c => c.G.World.SetFlag(ParleyTries[0]);
            d.Nodes["speak"].Options[1].OnSuccess += c => c.G.World.SetFlag(ParleyTries[1]);
            d.Nodes["speak"].Options[1].OnFailure += c => c.G.World.SetFlag(ParleyTries[1]);
            d.Nodes["speak"].Options[2].OnSuccess += c => c.G.World.SetFlag(ParleyTries[2]);
            d.Nodes["speak"].Options[2].OnFailure += c => c.G.World.SetFlag(ParleyTries[2]);

            d.Node("hesitates", who, c => "...Rest. Rest. I had forgotten the word.",
                c => "The great head lowers, just a little. For a moment the grey in its eyes thins, and you see something old and tired and almost kind. (It hesitates: if it comes to a fight, you'll have the first move.)");
            d.Node("recognises", who, c => "...Kin. Kin, in this place. Go home, kin. This is no place for one of us.",
                c => $"{c.G.World.DragonName} stands its ground and chirps, once. The Warden looks at it for a long time. (It won't turn on {c.G.World.DragonName} while any of itself is left.)");
            d.Node("weakness", who, c => "...",
                c => "As it speaks, the plates of its throat part, just slightly, and glow. The bestiary in the Restricted Section: when it draws breath, strike the glowing throat, and it will choke on its own fire. You know where to hit it.");
            d.Node("unmoved", who, c => "Go home.", c => "Nothing. The grey fire in its eyes doesn't change.");
            foreach (var id in new[] { "hesitates", "recognises", "weakness", "unmoved" })
            {
                var node = d.Nodes[id];
                node.Options.AddRange(d.Nodes["speak"].Options);
                node.Opt(new DOption { Text = "(It's time.)", Next = "turn", Show = c => Tries(c) >= 2 });
            }
            d.Node("turn", who, c => "The fire... the fire is rising. I cannot hold it and hold back from you both. Forgive me. I am the lock.",
                c => "For a moment it seemed to work. Then the grey fire rises in its eyes like a tide, and the Warden of the Emberhorn remembers what it is for. It rears up, wings filling the caldera.")
                .Say("(Roll initiative!)", doIt: StartWardenFight, end: true);
            return d;
        }

        static void StartWardenFight(DCtx c)
        {
            c.Screen?.Close();
            c.G.StartGroupCombat("warden", c.P, enemiesSurprised: c.G.World.Flag(WardenHesitated));
        }
    }
}
