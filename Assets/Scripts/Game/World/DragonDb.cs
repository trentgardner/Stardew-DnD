using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>How the dragon feels about someone. It can tell more than it can say.</summary>
    public enum DragonFeeling { Distrust = -2, Wary = -1, Neutral = 0, Fond = 1, Trust = 2 }

    /// <summary>
    /// The teenage dragon's opinions of people (Act II). It shows them (emotes, barks, the journal) and they matter a little:
    /// with someone it trusts, social checks go easier; around someone it distrusts, you read them better. It never blocks anything.
    /// New NPCs add their entries in their own phase.
    /// </summary>
    public static class DragonDb
    {
        public static readonly Dictionary<string, DragonFeeling> Base = new Dictionary<string, DragonFeeling>
        {
            ["elowen"] = DragonFeeling.Trust,    // warm hands, smells of honey cakes
            // Act II: the road and the Academy
            ["juniper"] = DragonFeeling.Trust,   // brings it biscuits, asks before petting
            // Act III: the capital
            ["quell"] = DragonFeeling.Fond,      // shares the horses' sugar
            ["wenna"] = DragonFeeling.Fond,      // lets it sniff the lamps
            ["gate_guard_0"] = DragonFeeling.Wary,
            ["gate_guard_1"] = DragonFeeling.Wary,
            ["oakes"] = DragonFeeling.Fond,      // the glasshouse is warm and smells green
            ["varrow"] = DragonFeeling.Wary,     // measuring tapes, and that look
            ["holloway"] = DragonFeeling.Wary,   // the ledger, the counting
            ["sorrel"] = DragonFeeling.Fond,
            ["ally_perrin"] = DragonFeeling.Fond,
            ["ally_thimble"] = DragonFeeling.Fond,
            ["ally_holloway"] = DragonFeeling.Wary,
            ["ally_odo"] = DragonFeeling.Wary,
            ["calloway"] = DragonFeeling.Distrust, // the trophy wall; the tooth
            ["odo"] = DragonFeeling.Wary,     // she smells of warm leaves
            ["marsh"] = DragonFeeling.Wary,      // the registry, the brass tag
            ["ashby"] = DragonFeeling.Distrust,  // it can smell the Sealing Hall on him
            ["lark"] = DragonFeeling.Trust,      // flowers to sniff
            ["maddie"] = DragonFeeling.Trust,    // children and dragons
            ["ssark"] = DragonFeeling.Trust,     // the egg-singer: kin
            ["tik"] = DragonFeeling.Fond,        // shiny buttons
            ["pip"] = DragonFeeling.Fond,
            ["brannoc"] = DragonFeeling.Fond,
            ["garrick"] = DragonFeeling.Fond,    // the forge is warm
            ["tobin"] = DragonFeeling.Fond,
            ["rhoswen"] = DragonFeeling.Fond,
            ["hesper"] = DragonFeeling.Fond,
            ["tamsin"] = DragonFeeling.Fond,     // fish
            ["dunstan"] = DragonFeeling.Wary,    // the bees, mostly
            ["wick"] = DragonFeeling.Wary,       // pulled its tail, once
            ["thimble"] = DragonFeeling.Wary,    // fae tricks make its scales itch
            ["perrin"] = DragonFeeling.Distrust, // keeps writing things down about it
        };

        public static DragonFeeling Opinion(string npcId, WorldState w)
        {
            if (string.IsNullOrEmpty(npcId)) return DragonFeeling.Neutral;
            int v = Base.TryGetValue(npcId, out var b) ? (int)b : 0;
            if (w != null && w.DragonOpinionShift.TryGetValue(npcId, out var s)) v += s;
            return (DragonFeeling)Math.Max(-2, Math.Min(2, v));
        }

        /// <summary>Something happened that changes its mind (faction quests use this). Saved.</summary>
        public static void Shift(string npcId, int delta)
        {
            var w = GameRoot.I.World;
            w.DragonOpinionShift.TryGetValue(npcId, out var s);
            int baseV = Base.TryGetValue(npcId, out var b) ? (int)b : 0;
            // Keep the stored shift inside what can matter (so a later shift the other way is felt at once).
            w.DragonOpinionShift[npcId] = Math.Max(-2 - baseV, Math.Min(2 - baseV, s + delta));
        }

        static readonly Skill[] Charm = { Skill.Persuasion, Skill.Deception, Skill.Performance };

        /// <summary>Trust: Advantage on Persuasion, Deception and Performance with them. Distrust: Advantage on Insight (its hackles tell you).</summary>
        public static AdvantageState CheckAdvantage(string npcId, Skill skill, bool dragonNear)
        {
            if (!dragonNear || GameRoot.I == null) return AdvantageState.Normal;
            var f = Opinion(npcId, GameRoot.I.World);
            if (f == DragonFeeling.Trust && Array.IndexOf(Charm, skill) >= 0) return AdvantageState.Advantage;
            if (f == DragonFeeling.Distrust && skill == Skill.Insight) return AdvantageState.Advantage;
            return AdvantageState.Normal;
        }

        /// <summary>What the party sees when the dragon notices someone (null for neutral or strangers).</summary>
        public static string Bark(string npcId, string dragon)
        {
            if (GameRoot.I == null || !WorldDb.Npcs.TryGetValue(npcId, out var def)) return null;
            switch (Opinion(npcId, GameRoot.I.World))
            {
                case DragonFeeling.Trust: return $"{dragon} bounds over to {def.Name} and leans against their legs, purring like a kettle.";
                case DragonFeeling.Fond: return $"{dragon} gives {def.Name} a friendly sniff.";
                case DragonFeeling.Wary: return $"{dragon} keeps you between itself and {def.Name}, watching.";
                case DragonFeeling.Distrust: return $"{dragon} hisses at {def.Name}, smoke curling from its nostrils.";
                default: return null;
            }
        }

        public static string EmoteFor(DragonFeeling f) =>
            f == DragonFeeling.Trust ? "heart" : f == DragonFeeling.Fond ? "happy" : f == DragonFeeling.Wary ? "wary" : f == DragonFeeling.Distrust ? "distrust" : null;

        /// <summary>"Cinder trusts: … · Cinder is wary of: …" for the journal (only people you've met).</summary>
        public static string TrustSummary(string dragon, WorldState w)
        {
            var met = WorldDb.Npcs.Values.Where(n => w.Friendship.ContainsKey(n.Id) || Base.ContainsKey(n.Id)).ToList();
            var trusts = met.Where(n => Opinion(n.Id, w) >= DragonFeeling.Fond).Select(n => n.Name).ToList();
            var wary = met.Where(n => Opinion(n.Id, w) <= DragonFeeling.Wary).Select(n => n.Name).ToList();
            var parts = new List<string>();
            if (trusts.Count > 0) parts.Add($"{dragon} trusts: {string.Join(", ", trusts)}");
            if (wary.Count > 0) parts.Add($"{dragon} is wary of: {string.Join(", ", wary)}");
            return string.Join(" · ", parts);
        }

        /// <summary>Phase 5 calls this in blighted places: the dragon feels a fire that frightens it.</summary>
        public static string BlightBark(string dragon) =>
            $"{dragon} goes still, nose up, and presses against your side. Something in the ash smells of fire it doesn't like.";
    }
}
