using System.IO;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        static void MonsterTests13()
        {
            Dice.Rng = new SystemRandom();
            string[] ids = { "crown_knight", "crown_champion", "crown_battlemage", "stone_guardian", "iron_sentinel" };
            foreach (var id in ids) Check(MonstersDb.Get(id) != null, id + " exists");
            Eq(MonstersDb.Get("crown_knight").CR, 3.0, "Crown Knight CR 3");
            Eq(MonstersDb.Get("crown_champion").CR, 5.0, "Crown Champion CR 5");
            Eq(MonstersDb.Get("crown_battlemage").CR, 6.0, "Crown Battlemage CR 6");
            Eq(MonstersDb.Get("stone_guardian").CR, 10.0, "Stone Guardian CR 10");
            Eq(MonstersDb.Get("iron_sentinel").CR, 1.0, "Iron Sentinel CR 1");
            Eq(new Monster(MonstersDb.Get("crown_champion")).AttackRoutine().Count(), 3, "the champion thrusts three times");

            // ParryRaisesAcOncePerRound
            var knight = new Monster(MonstersDb.Get("crown_knight"));
            var turn = new TurnState(); turn.Reset(knight);
            Check(knight.TryParry(19, true, false, turn), "a melee 19 vs AC 18: parried (AC 20 for that hit)");
            Check(!knight.TryParry(19, true, false, turn), "only once per round (it's the reaction)");
            turn.Reset(knight);
            Check(!knight.TryParry(20, true, false, turn), "a 20 still hits through the parry");
            Check(!knight.TryParry(19, false, false, turn), "it can't parry arrows");
            Check(!knight.TryParry(19, true, true, turn), "nor a natural 20");
            Check(!new Monster(MonstersDb.Get("troll")).TryParry(10, true, false, turn), "trolls don't parry");

            // The battlemage's Fireball lands at range; it doesn't start at the mage
            var fb = MonstersDb.Get("crown_battlemage").Abilities.Single();
            Eq(fb.RangeFt, 150, "Fireball lands up to 150 ft away");
            Check(fb.Shape == TargetShape.Sphere && fb.AreaFt == 20, "a 20-ft sphere");

            // StoneGuardianAdvantageOnSpellSaves
            var golem = new Monster(MonstersDb.Get("stone_guardian"));
            Check(golem.AdvantageOnSave(Ability.DEX, "spell"), "Magic Resistance: Advantage against spells");
            Check(!golem.AdvantageOnSave(Ability.DEX, null), "...not against a breath or a trap");
            Check(!knight.AdvantageOnSave(Ability.DEX, "spell"), "knights have no Magic Resistance");
            Dice.Rng = new ScriptedRandom(3, 17);
            Eq(SpellResolver.TargetSave(AtLevelThirteen("wizard", 11), golem, SpellsDb.Get("fireball")).Natural, 17, "a wizard's Fireball save is rolled with Advantage");
            Dice.Rng = new SystemRandom();
            Check(golem.Immunities.Contains(DamageType.Poison) && golem.Immunities.Contains(DamageType.Psychic), "immune to poison and psychic");
            Eq(MonstersDb.Get("stone_guardian").Abilities.Single().EffectId, "slowed_spell", "its Slow slows");

            // NewMonstersHaveArtIds: every sprite id is in the art map (or reuses an existing one)
            string map = File.ReadAllText(Path.Combine(RepoRoot(), "Assets", "Editor", "ArtMap.txt"));
            foreach (var id in ids)
            {
                string sprite = MonstersDb.Get(id).SpriteId;
                Check(map.Contains(" " + sprite + "_0 ") && map.Contains(" " + sprite + "_1 "), $"{id}: {sprite} has two frames in the art map");
            }
        }

        static string RepoRoot()
        {
            var d = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets"))) d = d.Parent;
            return d?.FullName ?? ".";
        }
    }
}
