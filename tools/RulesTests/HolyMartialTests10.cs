using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    public static partial class Program
    {
        /// <summary>Who stands within how many feet of whom, for aura tests.</summary>
        sealed class FakeAuras : IAuraSource
        {
            public readonly Dictionary<Creature, int> Feet = new Dictionary<Creature, int>(); // distance from the paladin
            public IEnumerable<Creature> Within(Creature c, int feet)
            {
                yield return c;
                if (!Feet.TryGetValue(c, out int d)) yield break;
                foreach (var kv in Feet) if (kv.Key != c && System.Math.Abs(kv.Value - d) <= feet) yield return kv.Key;
            }
        }

        static void HolyMartialTests10()
        {
            Dice.Rng = new SystemRandom();

            // ---------------- Monk
            var m5 = AtLevelTen("monk", 5);
            Eq(m5.MartialArtsDie, 8, "monk 5: Martial Arts d8");
            // StunningStrikeOncePerTurnAndCostsFocus
            var turn = new TurnState(); turn.Reset(m5);
            int focus = m5.ResourceLeft("focus");
            var foe = Dummy(hp: 100);
            bool tried = m5.TryStunningStrike(foe, turn, null);
            Check(tried, "Stunning Strike is attempted");
            Eq(m5.ResourceLeft("focus"), focus - 1, "it costs 1 Focus");
            Check(foe.Has(Condition.Stunned) || foe.HasEffect("stun_glance"), "the target is Stunned, or slowed and exposed on a save");
            Check(!m5.TryStunningStrike(Dummy(hp: 100), turn, null), "only once per turn");
            var m6 = AtLevelTen("monk", 6);
            Check(m6.ResourceMax("wholeness") >= 1, "Open Hand 6: Wholeness of Body uses");
            m6.HP = 1;
            int healed = m6.WholenessOfBody(null);
            Check(healed >= 1 + m6.Mod(Ability.WIS) && m6.ResourceLeft("wholeness") == m6.ResourceMax("wholeness") - 1, "Wholeness of Body heals Martial Arts die + WIS");
            Eq(AtLevelTen("monk", 9).FlurryStrikes, 2, "Flurry: two strikes");
            Eq(AtLevelTen("monk", 10).FlurryStrikes, 3, "Heightened Focus: three");
            Check(AtLevelTen("monk", 7).HasFeature("Evasion"), "monk 7: Evasion");
            var m10 = AtLevelTen("monk", 10);
            var enc = new Encounter(); enc.Add(m10);
            m10.AddEffect(new ActiveEffect { Id = "poisoned", Name = "Poisoned", Condition = Condition.Poisoned, RoundsLeft = 10 });
            enc.EndTurn(null);
            Check(!m10.Has(Condition.Poisoned), "Self-Restoration ends Poisoned at the end of your turn");

            // ---------------- Paladin auras
            // AuraOfProtectionAddsChaToNearbyAllySaves
            var pal = AtLevelTen("paladin", 6);
            var near = Make("fighter"); var far = Make("wizard");
            var fake = new FakeAuras();
            fake.Feet[pal] = 0; fake.Feet[near] = 10; fake.Feet[far] = 15;
            Auras.Source = fake;
            try
            {
                int bonus = System.Math.Max(1, pal.Mod(Ability.CHA));
                Eq(Auras.ProtectionBonus(near), bonus, "an ally 10 ft away adds the paladin's CHA to saves");
                Eq(Auras.ProtectionBonus(far), 0, "15 ft is outside the aura");
                Eq(Auras.ProtectionBonus(pal), bonus, "the paladin too");
                var save = Checks.RollSave(near, Ability.WIS, AdvantageState.Normal);
                Check(save.Terms.Any(t => t.Label == "aura of protection" && t.Value == bonus), "the save shows the aura");
                pal.HP = 0;
                Eq(Auras.ProtectionBonus(near), 0, "an unconscious paladin gives no aura");
                pal.HP = pal.MaxHP;
                var pal10 = AtLevelTen("paladin", 10);
                fake.Feet.Clear(); fake.Feet[pal10] = 0; fake.Feet[near] = 5;
                near.AddEffect(EffectLibrary.Create("frightened", null, 15));
                Check(!near.Has(Condition.Frightened), "Aura of Courage: allies can't be frightened");
                near.AddEffect(EffectLibrary.Create("charmed", null, 15));
                Check(!near.Has(Condition.Charmed), "Aura of Devotion: or charmed");
            }
            finally { Auras.Source = null; }
            // AbjureFoesFrightensUpToChaTargets
            var p9 = AtLevelTen("paladin", 9);
            Eq(p9.AbjureFoesTargets, System.Math.Max(1, p9.Mod(Ability.CHA)), "Abjure Foes: up to CHA mod creatures");
            Check(p9.Class.Subclass.AlwaysPreparedAt(9).Contains("beacon_of_hope"), "Devotion 9 spells are always prepared");
            Check(p9.PreparedSpells.Contains("beacon_of_hope"), "...and actually prepared on reaching 9");

            // ---------------- Ranger
            var r6 = AtLevelTen("ranger", 6);
            Eq(r6.Speed, 40, "Roving: +10 ft Speed");
            PlayerCharacter.LevelCap = 10;
            var r6b = AtLevel("ranger", 6);
            var dt = r6b.PendingChoices().FirstOrDefault(c => c.Kind == ChoiceKind.HunterDefense);
            Check(dt != null && dt.Options.Contains("escape_the_horde") && dt.Options.Contains("multiattack_defense"), "Hunter 7: Defensive Tactics choice");
            PlayerCharacter.LevelCap = 4;
            // MultiattackDefenseGivesDisadvantageAfterFirstHit
            var r7 = AtLevelTen("ranger", 7);
            r7.HunterDefense = "multiattack_defense";
            var troll = new Monster(MonstersDb.Get("troll"));
            var ctx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true };
            var bite = troll.GetAttacks()[0];
            Check(AttackResolver.ComputeAdvantage(troll, r7, bite, ctx, new AttackRollResult()) == AdvantageState.Normal, "first attack: normal");
            AttackRollResult hitRes = null;
            for (int i = 0; i < 200 && hitRes == null; i++) { var rr = AttackResolver.RollAttack(troll, r7, bite, ctx); if (rr.Hit) hitRes = rr; }
            r7.HP = r7.MaxHP = 999;
            AttackResolver.ApplyHit(hitRes, AttackResolver.RollDamage(hitRes));
            Check(AttackResolver.ComputeAdvantage(troll, r7, bite, ctx, new AttackRollResult()) == AdvantageState.Disadvantage, "after it hits you, its other attacks this turn have Disadvantage");
            var enc2 = new Encounter(); enc2.Add(troll); enc2.Add(r7); enc2.CurrentIndex = 0; enc2.StartTurn(null);
            Check(AttackResolver.ComputeAdvantage(troll, r7, bite, ctx, new AttackRollResult()) == AdvantageState.Normal, "it wears off on the attacker's next turn");
            var esc = AtLevelTen("ranger", 7); esc.HunterDefense = "escape_the_horde";
            var octx = new AttackContext { DistanceFt = 5, AttackerCanSeeTarget = true, TargetCanSeeAttacker = true, Opportunity = true };
            Check(AttackResolver.ComputeAdvantage(troll, esc, bite, octx, new AttackRollResult()) == AdvantageState.Disadvantage, "Escape the Horde: opportunity attacks have Disadvantage");
            // TirelessGivesTempHp
            var r10 = AtLevelTen("ranger", 10);
            Check(r10.ResourceMax("tireless") >= 1, "Tireless uses");
            int gained = r10.Tireless(null);
            Check(gained >= 1 + r10.Mod(Ability.WIS) && r10.TempHP == gained, "Tireless: temp HP 1d8 + WIS");
            r10.Exhaustion = 2; r10.ShortRest(null);
            Eq(r10.Exhaustion, 1, "Tireless: a short rest removes a level of Exhaustion");
        }
    }
}
