using System.Linq;
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
        /// <summary>Warlock 11/13: Mystic Arcanum: one 6th- and one 7th-level spell, each castable once per Long Rest without a slot.</summary>
        public readonly List<string> MysticArcanum = new List<string>();

        /// <summary>The Mystic Arcanum spell of a level, or null.</summary>
        public string ArcanumAt(int spellLevel) => MysticArcanum.FirstOrDefault(id => Content.SpellsDb.Get(id)?.Level == spellLevel);

        /// <summary>Mystic Arcanum: true if this spell is one of yours and its once-per-Long-Rest use is still there.</summary>
        public bool ArcanumReady(string spellId)
        {
            var s = Content.SpellsDb.Get(spellId);
            return s != null && MysticArcanum.Contains(spellId) && ResourceLeft("arcanum_" + s.Level) > 0;
        }

        /// <summary>Spends a Mystic Arcanum use (cast at exactly the spell's level, never with a Pact slot). False if it's spent or not yours.</summary>
        public bool CastArcanum(string spellId)
        {
            if (!ArcanumReady(spellId)) return false;
            return SpendResource("arcanum_" + Content.SpellsDb.Get(spellId).Level);
        }

        public static readonly string[] InvocationOptions = { "agonizing_blast", "armor_of_shadows", "repelling_blast", "pact_of_the_blade", "eldritch_mind", "ember_sight",
            "thirsting_blade", "misty_visions", "beast_speech" };

        /// <summary>Invocations with a prerequisite: (level, other invocation).</summary>
        static readonly Dictionary<string, (int level, string needs)> InvocationPrereqs = new Dictionary<string, (int, string)>
        {
            { "thirsting_blade", (5, "pact_of_the_blade") },
        };

        public bool CanLearnInvocation(string id, int atLevel) =>
            !InvocationPrereqs.TryGetValue(id, out var p) || (atLevel >= p.level && Invocations.Contains(p.needs));

        /// <summary>Thirsting Blade: Extra Attack with a pact weapon (a melee weapon, with Pact of the Blade).</summary>
        bool ThirstingBlade => Invocations.Contains("thirsting_blade") && Invocations.Contains("pact_of_the_blade") && MainHand != null && !MainHand.IsRanged;

        /// <summary>Sorcerer 6 (Draconic): Elemental Affinity type ("fire", "cold", "lightning", "acid", "poison").</summary>
        public string DraconicElement;
        public static readonly string[] DraconicElements = { "fire", "cold", "lightning", "acid", "poison" };

        DamageType? AffinityType => DraconicElement == null || !HasFeature("Elemental Affinity") ? (DamageType?)null
            : (DamageType)Enum.Parse(typeof(DamageType), char.ToUpper(DraconicElement[0]) + DraconicElement.Substring(1));

        /// <summary>Sorcerer 7: Sorcery Incarnate: out of Innate Sorcery uses, spend 2 sorcery points to activate it anyway.</summary>
        public bool SorceryIncarnate(List<string> log)
        {
            if (!HasFeature("Sorcery Incarnate") || ResourceLeft("innate_sorcery") > 0 || HasEffect("innate_sorcery") || !SpendResource("sorcery_points", 2)) return false;
            AddEffect(EffectLibrary.Create("innate_sorcery", this));
            log?.Add($"{Name} burns 2 sorcery points to unleash Innate Sorcery.");
            return true;
        }

        /// <summary>Sorcerer 5: Sorcerous Restoration on a short rest: half your level in sorcery points, once per long rest.</summary>
        void SorcerousRestoration(List<string> log)
        {
            if (!HasFeature("Sorcerous Restoration")) return;
            ResourcesUsed.TryGetValue("sorcery_points", out int used);
            if (used <= 0 || !SpendResource("sorcerous_restoration")) return;
            int back = Math.Min(used, Level / 2);
            ResourcesUsed["sorcery_points"] = used - back;
            log?.Add($"{Name} recovers {back} sorcery points (Sorcerous Restoration).");
        }

        public int InvocationsKnownAt(int level) => Class.Id != "warlock" ? 0 : level >= 18 ? 10 : level >= 15 ? 9 : level >= 12 ? 8 : level >= 9 ? 7 : level >= 7 ? 6 : level >= 5 ? 5 : level >= 2 ? 3 : 1;

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
