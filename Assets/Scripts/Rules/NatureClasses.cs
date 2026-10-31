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
            if (Level >= 8) { list.Add("dire_wolf"); list.Add("giant_owl"); } // CR 1, and flying
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

        /// <summary>Flying right now (an owl's shape, or the Fly spell): crosses water on the grid.</summary>
        public bool Flying => (WildShape != null && WildShape.FlySpeed > 0) || HasEffect("flying");

        /// <summary>Circle spells after 3, by land and druid level (curated stand-ins where the SRD spell isn't in the game).</summary>
        public static readonly Dictionary<string, Dictionary<int, string[]>> CircleSpellsLater = new Dictionary<string, Dictionary<int, string[]>>
        {
            { "arid", new Dictionary<int, string[]> { { 5, new[] { "fireball" } }, { 7, new[] { "wall_of_fire" } }, { 9, new[] { "cone_of_cold" } } } },
            { "polar", new Dictionary<int, string[]> { { 5, new[] { "sleet_storm" } }, { 7, new[] { "ice_storm" } }, { 9, new[] { "cone_of_cold" } } } },
            { "temperate", new Dictionary<int, string[]> { { 5, new[] { "lightning_bolt" } }, { 7, new[] { "greater_invisibility" } }, { 9, new[] { "hold_monster" } } } },
        };

        /// <summary>Druid 5: Wild Resurgence: out of Wild Shape uses, spend a spell slot to get one back (once per turn in play).</summary>
        public bool WildResurgence(List<string> log)
        {
            if (!HasFeature("Wild Resurgence") || ResourceLeft("wild_shape") > 0) return false;
            int slot = LowestAvailableSlot(1);
            if (slot <= 0) return false;
            SlotsUsed[slot]++;
            ResourcesUsed["wild_shape"] = ResourceMax("wild_shape") - 1;
            log?.Add($"{Name} trades a level {slot} slot for a Wild Shape (Wild Resurgence).");
            return true;
        }

        /// <summary>Land 6: Natural Recovery: on a short rest, recover spell slots totalling up to half your druid level (no 6th+), once per long rest.</summary>
        void NaturalRecovery(List<string> log)
        {
            if (!HasFeature("Natural Recovery") || !SpendResource("natural_recovery")) return;
            int budget = (Level + 1) / 2, got = 0;
            for (int l = Math.Min(5, budget); l >= 1; l--)
                while (SlotsUsed[l] > 0 && l <= budget) { SlotsUsed[l]--; budget -= l; got += l; }
            if (got > 0) log?.Add($"{Name} draws strength from the land: {got} levels of spell slots back (Natural Recovery).");
            else RegainResource("natural_recovery", 1); // nothing spent: keep it for later
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
