using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void CasterTests13()
        {
            Dice.Rng = new SystemRandom();

            // FullCastersGetSixthAtElevenSeventhAtThirteen
            foreach (var cls in new[] { "cleric", "druid", "bard", "sorcerer", "wizard" })
            {
                var c11 = AtLevelThirteen(cls, 11);
                Eq(c11.SlotsMax(6), 1, cls + " 11: a 6th-level slot");
                Eq(c11.SlotsMax(7), 0, cls + " 11: no 7th yet");
                Eq(AtLevelThirteen(cls, 13).SlotsMax(7), 1, cls + " 13: a 7th-level slot");
                Check(c11.Class.Features.Any(f => f.Level == 11), cls + " 11 has a feature line to show on the level-up screen");
            }

            // ArcanumChoiceOfferedAtElevenAndThirteenOnly
            var w10 = AtLevelThirteen("warlock", 10);
            PlayerCharacter.LevelCap = PlayerCharacter.ActThreeLevelCap;
            try
            {
                var at11 = w10.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Arcanum);
                Check(at11 != null && at11.Count == 1, "warlock 10 → 11: Mystic Arcanum (6th)");
                Check(at11 != null && at11.Options.Count >= 2 && at11.Options.All(id => SpellsDb.Get(id).Level == 6), "...from 6th-level warlock spells");
                w10.LevelUp(w10.AutoPlan(), null);
                Check(w10.PendingChoices().All(c => c.Kind != ChoiceKind.Arcanum), "11 → 12: none");
                w10.LevelUp(w10.AutoPlan(), null);
                var at13 = w10.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.Arcanum);
                Check(at13 != null && at13.Options.All(id => SpellsDb.Get(id).Level == 7), "12 → 13: Mystic Arcanum (7th)");
                w10.LevelUp(w10.AutoPlan(), null);
            }
            finally { PlayerCharacter.LevelCap = PlayerCharacter.ActOneLevelCap; }
            Eq(w10.MysticArcanum.Count, 2, "a level-13 warlock has two arcana");
            Check(AtLevelThirteen("wizard", 11).PendingChoices().All(c => c.Kind != ChoiceKind.Arcanum), "only warlocks");

            // MysticArcanumIsOncePerLongRestAndNotASlot
            var w = w10;
            string six = w.ArcanumAt(6), seven = w.ArcanumAt(7);
            Check(six != null && seven != null, "one 6th, one 7th");
            Eq(w.MaxSpellLevelAt(13), 5, "Pact slots stay 5th-level");
            Eq(w.SlotsMax(5), 3, "warlock 11+: three Pact slots");
            int pactBefore = w.SlotsLeft(5);
            Check(w.CastArcanum(six), "cast the 6th-level arcanum");
            Eq(w.SlotsLeft(5), pactBefore, "it doesn't touch the Pact slots");
            Check(!w.CastArcanum(six), "only once");
            Check(w.CastArcanum(seven), "the 7th-level one is separate");
            Check(!SpellResolver.PaySlot(w, SpellsDb.Get(six), 5), "and it can't be cast from a Pact slot");
            w.ShortRest(null);
            Check(!w.ArcanumReady(six), "a Short Rest doesn't bring it back");
            w.LongRest(true, null);
            Check(w.ArcanumReady(six) && w.ArcanumReady(seven), "a Long Rest does");
            Check(!w.CastArcanum("fireball"), "not a spell that isn't your arcanum");
        }
    }
}
