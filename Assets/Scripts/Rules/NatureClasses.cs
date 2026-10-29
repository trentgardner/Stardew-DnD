using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules.Content;

namespace DnD.Rules
{
    /// <summary>Druid (Wild Shape, Primal Order, Circle of the Land) and Ranger (Favored Enemy, Hunter) rules.</summary>
    public partial class PlayerCharacter
    {
        // ---------------------------------------------------------------- druid

        /// <summary>Druid level 1 choice: "magician" (extra cantrip, WIS to Arcana/Nature) or "warden" (medium armor, martial weapons).</summary>
        public string PrimalOrder;
        /// <summary>Circle of the Land terrain ("arid", "polar", "temperate").</summary>
        public string LandType;
        /// <summary>Spells always prepared from a choice (circle spells); they don't count against the prepared limit.</summary>
        public readonly List<string> BonusAlwaysPrepared = new List<string>();

        /// <summary>The beast the druid is currently shaped into (null = their own form).</summary>
        public MonsterDef WildShape;
        public bool CanCastSpells => WildShape == null;

        public static readonly Dictionary<string, string[]> CircleSpells = new Dictionary<string, string[]>
        {
            { "arid", new[] { "burning_hands", "fire_bolt" } },
            { "polar", new[] { "fog_cloud", "hold_person", "ray_of_frost" } },
            { "temperate", new[] { "misty_step", "shocking_grasp", "sleep" } },
        };

        /// <summary>Beast forms this druid knows (2024: 4 forms up to CR 1/4 at level 2, CR 1/2 at level 4; no flying until 8).</summary>
        public List<string> WildShapeForms()
        {
            var list = new List<string>();
            if (Class.Id != "druid" || Level < 2) return list;
            list.AddRange(new[] { "wolf", "boar", "cat", "frog" });
            if (Level >= 4) list.Add("black_bear");
            return list;
        }

        /// <summary>Bonus Action: take a beast's shape. Keeps your HP, mind and skills; gains temp HP equal to your druid level.</summary>
        public bool StartWildShape(string beastId, List<string> log)
        {
            if (WildShape != null || !WildShapeForms().Contains(beastId)) return false;
            var def = MonstersDb.Get(beastId);
            if (def == null || !SpendResource("wild_shape")) return false;
            WildShape = def;
            TempHP = Math.Max(TempHP, Level);
            log?.Add($"{Name} shifts into a {def.Name.ToLower()}! (+{Level} temporary HP)");
            return true;
        }

        public void EndWildShape(List<string> log)
        {
            if (WildShape == null) return;
            log?.Add($"{Name} returns to their own form.");
            WildShape = null;
        }

        // ---------------------------------------------------------------- ranger

        /// <summary>Hunter's Prey choice at Ranger 3: "colossus_slayer" or "horde_breaker".</summary>
        public string HunterPrey;
    }
}
