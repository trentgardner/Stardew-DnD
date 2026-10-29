using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    /// <summary>Sorcerer (Innate Sorcery, Font of Magic, Metamagic, Draconic Sorcery) and Warlock (Pact Magic, Invocations, Ember Patron) rules.</summary>
    public partial class PlayerCharacter
    {
        // ---------------------------------------------------------------- warlock

        /// <summary>Eldritch Invocations known (ids, e.g. "agonizing_blast").</summary>
        public readonly List<string> Invocations = new List<string>();

        public static readonly string[] InvocationOptions = { "agonizing_blast", "armor_of_shadows", "repelling_blast", "pact_of_the_blade", "eldritch_mind", "ember_sight" };

        public int InvocationsKnownAt(int level) => Class.Id != "warlock" ? 0 : level >= 5 ? 5 : level >= 2 ? 3 : 1;

        /// <summary>Ember Patron (SRD Fiend): when you drop a hostile creature to 0 HP, gain temp HP = CHA + warlock level.</summary>
        public void OnReducedHostileToZero(List<string> log)
        {
            if (!HasFeature("Ember's Blessing")) return;
            int t = Math.Max(1, Mod(Ability.CHA) + Level);
            if (t > TempHP) { TempHP = t; log?.Add($"Ember's Blessing: {Name} gains {t} temporary HP."); }
        }

        // ---------------------------------------------------------------- sorcerer

        /// <summary>Metamagic options known (ids, e.g. "quickened").</summary>
        public readonly List<string> Metamagic = new List<string>();

        public static readonly string[] MetamagicOptions = { "quickened", "empowered", "careful", "distant", "extended", "twinned" };

        /// <summary>Font of Magic: spend a spell slot for sorcery points equal to its level (not past your maximum).</summary>
        public bool SlotToPoints(int slotLevel)
        {
            if (ResourceMax("sorcery_points") == 0 || SlotsLeft(slotLevel) <= 0) return false;
            ResourcesUsed.TryGetValue("sorcery_points", out int used);
            if (used <= 0) return false; // already full
            SlotsUsed[slotLevel]++;
            ResourcesUsed["sorcery_points"] = Math.Max(0, used - slotLevel);
            return true;
        }

        /// <summary>Font of Magic: create a spell slot from sorcery points (level 1 costs 2, level 2 costs 3, 2024 table).</summary>
        public bool PointsToSlot(int slotLevel)
        {
            int cost = slotLevel == 1 ? 2 : slotLevel == 2 ? 3 : slotLevel == 3 ? 5 : slotLevel == 4 ? 6 : 7;
            if (ResourceMax("sorcery_points") == 0 || slotLevel > MaxSpellLevelAt(Level) || !SpendResource("sorcery_points", cost)) return false;
            SlotsUsed[slotLevel]--; // may go below zero: an extra slot until the next Long Rest
            return true;
        }
    }
}
