using System;
using System.Collections.Generic;
using System.Linq;

namespace DnD.Rules
{
    /// <summary>Positional facts the game layer supplies when an attack is made.</summary>
    public class AttackContext
    {
        public int DistanceFt = 5;
        public bool AttackerCanSeeTarget = true;
        public bool TargetCanSeeAttacker = true;
        /// <summary>An ally of the attacker (not incapacitated) is within 5 ft of the target (Pack Tactics).</summary>
        public bool AllyAdjacentToTarget;
        /// <summary>A visible, conscious enemy is within 5 ft of the attacker (ranged attacks get Disadvantage).</summary>
        public bool EnemyAdjacentToAttacker;
        public bool AttackerInSunlight;
    }

    public class AttackRollResult
    {
        public Creature Attacker, Target;
        public AttackOption Attack;
        public D20Result Roll;
        public bool Hit, Critical, OutOfRange;
        /// <summary>The attack's context and the attacker's turn (for once-per-turn riders like Sneak Attack); may be null.</summary>
        public AttackContext Context;
        public TurnState Turn;
        public int TargetAC;
        public readonly List<string> AdvantageReasons = new List<string>();
        public readonly List<string> DisadvantageReasons = new List<string>();

        public string Summary()
        {
            if (OutOfRange) return "Out of range!";
            string res = Critical ? "CRITICAL HIT!" : Hit ? "HIT" : "MISS";
            return $"{Attacker.Name} → {Target.Name}: {Roll.Total} vs AC {TargetAC}, {res}";
        }
    }

    public static class AttackResolver
    {
        /// <summary>Works out Advantage/Disadvantage for an attack, with reasons for the HUD.</summary>
        public static AdvantageState ComputeAdvantage(Creature attacker, Creature target, AttackOption atk, AttackContext ctx, AttackRollResult res)
        {
            var adv = res.AdvantageReasons;
            var dis = res.DisadvantageReasons;

            if (!ctx.AttackerCanSeeTarget) dis.Add("can't see target");
            if (!ctx.TargetCanSeeAttacker) adv.Add("unseen attacker");
            if (attacker.Has(Condition.Prone)) dis.Add("you are prone");
            if (attacker.Has(Condition.Poisoned)) dis.Add("poisoned");
            if (attacker.Has(Condition.Frightened)) dis.Add("frightened");
            if (attacker.Has(Condition.Restrained)) dis.Add("restrained");
            if (attacker.Has(Condition.Blinded)) dis.Add("blinded");

            if (target.Has(Condition.Prone))
            {
                if (ctx.DistanceFt <= 5) adv.Add("target prone");
                else dis.Add("target prone (ranged)");
            }
            if (target.Has(Condition.Restrained)) adv.Add("target restrained");
            if (target.Has(Condition.Paralyzed) || target.Has(Condition.Unconscious) || target.Has(Condition.Stunned)) adv.Add("target helpless");
            if (target.Has(Condition.Blinded)) adv.Add("target blinded");
            if (target.HasEffect("dodge") && ctx.TargetCanSeeAttacker) dis.Add("target dodging");

            foreach (var e in target.Effects)
            {
                if (e.GrantsAdvantageToAttackers) adv.Add(e.Name.ToLower());
                if (e.AdvantageForAttacker == attacker) adv.Add("vex");
            }
            foreach (var e in attacker.Effects)
            {
                if (e.DisadvantageOnNextAttack) dis.Add(e.Name.ToLower());
                if (e.AdvantageOnNextAttack) adv.Add(e.Name.ToLower());
            }
            if (atk.IsSpell && attacker.HasEffect("innate_sorcery")) adv.Add("innate sorcery");
            bool strMelee = !atk.Ranged && !atk.IsSpell && attacker is PlayerCharacter apc && (atk.Weapon == null || !atk.Weapon.Has(WeaponProperty.Finesse) || apc.Mod(Ability.STR) >= apc.Mod(Ability.DEX));
            if (strMelee && attacker.HasEffect("reckless")) adv.Add("reckless");
            if (target.HasEffect("reckless")) adv.Add("target is reckless");

            if (atk.Ranged || (atk.IsSpell && atk.RangeNormal > 5))
            {
                if (ctx.EnemyAdjacentToAttacker) dis.Add("enemy adjacent");
                if (atk.RangeNormal > 0 && ctx.DistanceFt > atk.RangeNormal) dis.Add("long range");
            }

            if (attacker is Monster m)
            {
                if (m.Def.PackTactics && ctx.AllyAdjacentToTarget) adv.Add("pack tactics");
                if (m.Def.SunlightSensitivity && ctx.AttackerInSunlight) dis.Add("sunlight");
            }

            return Dice.Combine(adv.Count > 0, dis.Count > 0);
        }

        public static AttackRollResult RollAttack(Creature attacker, Creature target, AttackOption atk, AttackContext ctx, TurnState turn = null)
        {
            var res = new AttackRollResult { Attacker = attacker, Target = target, Attack = atk, TargetAC = target.ArmorClass, Context = ctx, Turn = turn };
            if (ctx.DistanceFt > atk.MaxRangeFt && !(atk.IsSpell && ctx.DistanceFt <= atk.RangeNormal))
            {
                res.OutOfRange = true;
                return res;
            }

            var adv = ComputeAdvantage(attacker, target, atk, ctx, res);
            var terms = new List<RollTerm> { new RollTerm(atk.IsSpell ? "spell attack" : "to hit", atk.ToHit) };
            if (attacker.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", attacker.D20Penalty));
            res.Roll = Dice.RollD20(adv, terms);
            foreach (var e in attacker.Effects) if (e.AttackDie > 0) Dice.AddBonusDie(res.Roll, e.AttackDie);

            // Consume one-shot effects.
            attacker.Effects.RemoveAll(e => e.DisadvantageOnNextAttack || e.AdvantageOnNextAttack);
            target.Effects.RemoveAll(e => e.ConsumedWhenAttacked && (e.AdvantageForAttacker == null || e.AdvantageForAttacker == attacker));

            int nat = res.Roll.Natural;
            if (nat == 1) res.Hit = false;
            else if (nat >= atk.CritOn) { res.Hit = true; res.Critical = true; }
            else res.Hit = res.Roll.Total >= res.TargetAC;

            // Attacks against a Paralyzed/Unconscious creature from within 5 ft are automatic crits.
            if (res.Hit && ctx.DistanceFt <= 5 && (target.Has(Condition.Paralyzed) || target.Has(Condition.Unconscious)))
                res.Critical = true;
            return res;
        }

        /// <summary>Rolls damage for a hit. Called when the player clicks "Roll Damage".</summary>
        public static DamageRoll RollDamage(AttackRollResult r)
        {
            var atk = r.Attack;
            var terms = new List<RollTerm>();
            if (atk.DamageMod != 0) terms.Add(new RollTerm("mod", atk.DamageMod));
            var roll = Dice.RollDamage(atk.Damage, r.Critical, terms, atk.MinDieValue);
            if (!atk.ExtraDamage.IsNone)
            {
                roll.Extra = Dice.RollDamage(atk.ExtraDamage, r.Critical);
                roll.ExtraType = atk.ExtraDamageType;
            }
            if (r.Attacker is PlayerCharacter pc && r.Turn != null)
            {
                // Rogue: Sneak Attack, once per turn.
                int sneak = SneakAttackDiceFor(pc);
                if (sneak > 0 && !r.Turn.SneakAttackUsed && SneakAttackEligible(r))
                {
                    r.Turn.SneakAttackUsed = true;
                    roll.Dice.AddRange(Dice.RollDamage(new DiceExpr(sneak, 6), r.Critical).Dice);
                    roll.Terms.Add(new RollTerm("sneak attack", 0));
                }
                // Hunter's Mark: +1d6 force on weapon hits against the creature you marked.
                if (atk.Weapon != null && r.Target.Effects.Any(e => e.Id == "hunters_mark" && e.Source == pc))
                {
                    var mark = Dice.RollDamage(new DiceExpr(1, 6), r.Critical);
                    if (roll.Extra == null) { roll.Extra = mark; roll.ExtraType = DamageType.Force; }
                    else roll.Dice.AddRange(mark.Dice);
                }
                // Hex: +1d6 necrotic on any attack hit against the creature you cursed.
                if (r.Target.Effects.Any(e => e.Id == "hex" && e.Source == pc))
                {
                    var hex = Dice.RollDamage(new DiceExpr(1, 6), r.Critical);
                    if (roll.Extra == null) { roll.Extra = hex; roll.ExtraType = DamageType.Necrotic; }
                    else roll.Dice.AddRange(hex.Dice);
                }
                // Hunter: Colossus Slayer, once per turn against a creature that's already hurt.
                if (pc.HunterPrey == "colossus_slayer" && atk.Weapon != null && r.Target.HP < r.Target.MaxHP && !r.Turn.ColossusUsed)
                {
                    r.Turn.ColossusUsed = true;
                    roll.Dice.AddRange(Dice.RollDamage(new DiceExpr(1, 8), r.Critical).Dice);
                    roll.Terms.Add(new RollTerm("colossus slayer", 0));
                }
                // Berserker: Frenzy, the first hit each turn while raging recklessly.
                if (pc.HasFeature("Frenzy") && pc.Raging && pc.HasEffect("reckless") && !r.Turn.FrenzyUsed && !atk.Ranged && !atk.IsSpell)
                {
                    r.Turn.FrenzyUsed = true;
                    roll.Dice.AddRange(Dice.RollDamage(new DiceExpr(pc.RageDamage, 6), r.Critical).Dice);
                    roll.Terms.Add(new RollTerm("frenzy", 0));
                }
            }
            return roll;
        }

        /// <summary>Number of Sneak Attack d6s for a character (0 if not a rogue).</summary>
        public static int SneakAttackDiceFor(PlayerCharacter pc) => pc.Class.Id == "rogue" ? (pc.Level + 1) / 2 : 0;

        /// <summary>Finesse or ranged weapon, and either Advantage or an ally next to the target without Disadvantage.</summary>
        public static bool SneakAttackEligible(AttackRollResult r)
        {
            var w = r.Attack.Weapon;
            if (w == null || !(w.Has(WeaponProperty.Finesse) || w.IsRanged)) return false;
            var adv = r.Roll.Advantage;
            return adv == AdvantageState.Advantage || (r.Context != null && r.Context.AllyAdjacentToTarget && adv != AdvantageState.Disadvantage);
        }

        /// <summary>Applies damage and weapon mastery effects on a hit. Returns log lines.</summary>
        public static List<string> ApplyHit(AttackRollResult r, DamageRoll dmg)
        {
            var log = new List<string>();
            var t = r.Target;
            int dealt = t.TakeDamage(dmg.Total, r.Attack.DamageType, r.Critical, log);
            log.Insert(0, $"{t.Name} takes {dealt} {r.Attack.DamageType.ToString().ToLower()} damage.");
            if (dmg.Extra != null && !t.IsDead)
            {
                int extra = t.TakeDamage(dmg.Extra.Total, dmg.ExtraType, r.Critical, log);
                log.Add($"+{extra} {dmg.ExtraType.ToString().ToLower()} damage.");
                dealt += extra;
            }
            if (dealt > 0) ConcentrationCheckNeeded?.Invoke(t, dealt);

            if (!t.IsDown)
            {
                switch (r.Attack.Mastery)
                {
                    case WeaponMastery.Sap: t.AddEffect(EffectLibrary.Create("sap", r.Attacker)); log.Add($"Sap! {t.Name} has Disadvantage on its next attack."); break;
                    case WeaponMastery.Slow: t.AddEffect(EffectLibrary.Create("slow", r.Attacker)); log.Add($"Slow! {t.Name}'s speed drops by 10 ft."); break;
                    case WeaponMastery.Vex: t.AddEffect(EffectLibrary.Create("vex", r.Attacker)); log.Add("Vex! Advantage on your next attack against it."); break;
                    case WeaponMastery.Topple:
                        int dc = 8 + r.Attacker.Mod(r.Attack.Ability) + r.Attacker.ProficiencyBonus;
                        var save = Checks.RollSave(t, Ability.CON, AdvantageState.Normal);
                        if (!Checks.ResolveSave(save, dc)) { t.AddEffect(EffectLibrary.Create("prone", r.Attacker)); log.Add($"Topple! {t.Name} fails a CON save ({save.Total} vs DC {dc}) and falls Prone."); }
                        else log.Add($"Topple: {t.Name} keeps its footing ({save.Total} vs DC {dc}).");
                        break;
                    case WeaponMastery.Push: log.Add($"Push! {t.Name} is shoved back."); PushRequested?.Invoke(r.Attacker, t, 10); break;
                }
            }

            // Monster on-hit riders (wolf knockdown, etc.).
            if (r.Attacker is Monster m && !t.IsDown)
            {
                var def = m.DefFor(r.Attack);
                if (def != null && !string.IsNullOrEmpty(def.OnHitEffect))
                {
                    bool applies = true;
                    if (def.OnHitSaveDC > 0)
                    {
                        var save = Checks.RollSave(t, def.OnHitSave, AdvantageState.Normal);
                        applies = !Checks.ResolveSave(save, def.OnHitSaveDC);
                        log.Add($"{t.Name} {def.OnHitSave} save: {save.Total} vs DC {def.OnHitSaveDC}{(applies ? ", failed" : ", saved")}.");
                    }
                    if (applies) { t.AddEffect(EffectLibrary.Create(def.OnHitEffect, m)); log.Add($"{t.Name} is {def.OnHitEffect}!"); }
                }
            }
            return log;
        }

        /// <summary>Graze mastery: damage on a miss equal to the ability modifier.</summary>
        public static List<string> ApplyMiss(AttackRollResult r)
        {
            var log = new List<string>();
            if (r.Attack.Mastery == WeaponMastery.Graze)
            {
                int mod = r.Attacker.Mod(r.Attack.Ability);
                if (mod > 0)
                {
                    int dealt = r.Target.TakeDamage(mod, r.Attack.DamageType, false, log);
                    log.Insert(0, $"Graze! {r.Target.Name} still takes {dealt} damage.");
                }
            }
            return log;
        }

        /// <summary>Raised when damage requires a Concentration save (DC max(10, half damage)).</summary>
        public static event Action<Creature, int> ConcentrationCheckNeeded;
        /// <summary>Raised when an effect pushes a creature (attacker, target, feet).</summary>
        public static event Action<Creature, Creature, int> PushRequested;

        public static void RaiseConcentrationCheck(Creature c, int dmg) => ConcentrationCheckNeeded?.Invoke(c, dmg);
    }

    public static class SpellResolver
    {
        public static AttackOption AsAttack(PlayerCharacter caster, SpellDef s)
        {
            return new AttackOption
            {
                Name = s.Name,
                ToHit = caster.SpellAttackBonus,
                Damage = s.Dice,
                DamageType = s.DamageType,
                ReachFt = s.RangeFt <= 5 ? 5 : s.RangeFt,
                RangeNormal = s.RangeFt,
                RangeLong = s.RangeFt,
                Ranged = s.RangeFt > 5,
                Spell = s,
                Ability = caster.Class.SpellAbility,
                DamageMod = s.Id == "eldritch_blast" && caster.Invocations.Contains("agonizing_blast") ? caster.SpellMod : 0,
            };
        }

        /// <summary>The dice a spell rolls at the given slot level (cantrip scaling, upcasting).</summary>
        public static DiceExpr EffectDice(PlayerCharacter caster, SpellDef s, int slotLevel)
        {
            var d = s.Dice;
            if (s.IsCantrip) d = new DiceExpr(d.Count * caster.CantripDiceMultiplier, d.Sides, d.Bonus);
            else if (slotLevel > s.Level && s.UpcastDice > 0) d = d.WithExtraDice(s.UpcastDice * (slotLevel - s.Level));
            return d;
        }

        public static DamageRoll RollEffect(PlayerCharacter caster, SpellDef s, int slotLevel, bool critical)
        {
            var terms = new List<RollTerm>();
            if (s.AddSpellMod) terms.Add(new RollTerm(caster.Class.SpellAbility.ToString(), caster.SpellMod));
            if (s.Kind == SpellKind.Heal && caster.HasFeature("Disciple of Life") && s.Level > 0)
                terms.Add(new RollTerm("Disciple of Life", 2 + Math.Max(slotLevel, s.Level)));
            if (s.Kind != SpellKind.Heal && caster.HasFeature("Empowered Evocation") && s.School == "Evocation")
                terms.Add(new RollTerm("Empowered Evocation", caster.Mod(Ability.INT)));
            return Dice.RollDamage(EffectDice(caster, s, slotLevel), critical, terms);
        }

        public static D20Result TargetSave(PlayerCharacter caster, Creature target, SpellDef s)
        {
            string vs = s.EffectId == "charmed" ? "charmed" : null;
            return Checks.RollSave(target, s.SaveAbility, AdvantageState.Normal, vs);
        }

        /// <summary>Applies save-based damage: full on fail, half (or Potent Cantrip half) on success.</summary>
        public static int ApplySaveDamage(PlayerCharacter caster, Creature target, SpellDef s, DamageRoll dmg, bool saved, List<string> log)
        {
            int amount = dmg.Total;
            if (saved)
            {
                bool half = s.HalfOnSave || (s.IsCantrip && caster.HasFeature("Potent Cantrip"));
                amount = half ? amount / 2 : 0;
            }
            if (amount <= 0) { log.Add($"{target.Name} avoids the effect."); return 0; }
            int dealt = target.TakeDamage(amount, s.DamageType, false, log);
            log.Insert(0, $"{target.Name} takes {dealt} {s.DamageType.ToString().ToLower()} damage{(saved ? " (saved)" : "")}.");
            if (dealt > 0) AttackResolver.RaiseConcentrationCheck(target, dealt);
            return dealt;
        }

        /// <summary>Spends the slot (or nothing for cantrips). Returns false if unavailable.</summary>
        /// <summary>Divine Smite: 2d8 radiant, +1d8 per slot level above 1st (doubled on a critical hit).</summary>
        public static DamageRoll DivineSmiteDamage(int slotLevel, bool critical) => Dice.RollDamage(new DiceExpr(1 + Math.Max(1, slotLevel), 8), critical);

        public static bool PaySlot(PlayerCharacter caster, SpellDef s, int slotLevel)
        {
            // Ranger: Favored Enemy casts Hunter's Mark without a slot (a few times per Long Rest).
            if (s.Id == "hunters_mark" && caster.SpendResource("favored_enemy")) return true;
            if (s.IsCantrip) return true;
            if (slotLevel < s.Level || caster.SlotsLeft(slotLevel) <= 0) return false;
            caster.SlotsUsed[slotLevel]++;
            return true;
        }

        public static void StartConcentration(Creature caster, SpellDef s, List<string> log)
        {
            if (!s.Concentration) return;
            if (caster.ConcentratingOn != null) caster.BreakConcentration(log);
            caster.ConcentratingOn = s.Id;
        }
    }

    /// <summary>Per-turn action economy.</summary>
    public class TurnState
    {
        public bool ActionUsed, BonusActionUsed, ReactionUsed;
        public int MovementLeftFt;
        public int AttacksLeftInAction;
        public bool Dashed, Disengaged;
        public bool CastLeveledSpellThisTurn;
        public bool CleaveUsed;
        public bool SneakAttackUsed, FrenzyUsed, ColossusUsed, HordeBreakerUsed;
        /// <summary>Sorcerer Quickened Spell: the next action spell this turn costs the Bonus Action instead.</summary>
        public bool QuickenNextSpell;

        public void Reset(Creature c)
        {
            ActionUsed = BonusActionUsed = false;
            ReactionUsed = false;
            MovementLeftFt = c.Speed;
            AttacksLeftInAction = 0;
            Dashed = Disengaged = false;
            CastLeveledSpellThisTurn = false;
            CleaveUsed = false;
            SneakAttackUsed = FrenzyUsed = ColossusUsed = HordeBreakerUsed = QuickenNextSpell = false;
        }
    }

    public class Combatant
    {
        public Creature Creature;
        public D20Result InitiativeRoll;
        public int Initiative => InitiativeRoll?.Total ?? 0;
        public readonly TurnState Turn = new TurnState();
        public Combatant(Creature c) { Creature = c; }
    }

    /// <summary>Initiative order and round tracking for one fight. Pure rules; positions live in the game layer.</summary>
    public class Encounter
    {
        public readonly List<Combatant> Order = new List<Combatant>();
        public int Round = 1;
        public int CurrentIndex;
        public Combatant Current => Order.Count > 0 ? Order[CurrentIndex] : null;

        public Combatant Add(Creature c)
        {
            foreach (var x in Order) if (x.Creature == c) return x;
            var cb = new Combatant(c);
            var adv = c.InitiativeAdvantage ? AdvantageState.Advantage : AdvantageState.Normal;
            var terms = new List<RollTerm> { new RollTerm("initiative", c.InitiativeBonus) };
            if (c.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", c.D20Penalty));
            cb.InitiativeRoll = Dice.RollD20(adv, terms);
            Order.Add(cb);
            return cb;
        }

        /// <summary>Sorts by initiative (DEX breaks ties). Call after the initial Add()s.</summary>
        public void Sort()
        {
            var current = Current;
            Order.Sort((a, b) =>
            {
                int c = b.Initiative.CompareTo(a.Initiative);
                if (c != 0) return c;
                return b.Creature.Mod(Ability.DEX).CompareTo(a.Creature.Mod(Ability.DEX));
            });
            if (current != null) CurrentIndex = Order.IndexOf(current);
        }

        /// <summary>Inserts a late joiner (co-op partner walking into the fight) in initiative order.</summary>
        public Combatant Join(Creature c)
        {
            var current = Current;
            var cb = Add(c);
            Sort();
            CurrentIndex = Order.IndexOf(current);
            return cb;
        }

        public void StartTurn(List<string> log)
        {
            var c = Current;
            c.Turn.Reset(c.Creature);
            // Durations tick at the start of the source's turn.
            foreach (var cb in Order)
            {
                var fx = cb.Creature.Effects;
                for (int i = fx.Count - 1; i >= 0; i--)
                {
                    var e = fx[i];
                    if (e.Source != c.Creature || e.RoundsLeft < 0) continue;
                    e.RoundsLeft--;
                    if (e.RoundsLeft <= 0)
                    {
                        fx.RemoveAt(i);
                        log?.Add($"{e.Name} ends on {cb.Creature.Name}.");
                    }
                }
            }
        }

        /// <summary>End-of-turn repeat saves (Hold Person, etc.).</summary>
        public void EndTurn(List<string> log)
        {
            var c = Current.Creature;
            for (int i = c.Effects.Count - 1; i >= 0; i--)
            {
                var e = c.Effects[i];
                if (e.RepeatSave == null) continue;
                var save = Checks.RollSave(c, e.RepeatSave.Value, AdvantageState.Normal);
                if (Checks.ResolveSave(save, e.RepeatSaveDC))
                {
                    c.Effects.RemoveAt(i);
                    log?.Add($"{c.Name} shakes off {e.Name} ({save.Total} vs DC {e.RepeatSaveDC}).");
                }
            }
        }

        public void Advance()
        {
            if (Order.Count == 0) return;
            CurrentIndex++;
            if (CurrentIndex >= Order.Count) { CurrentIndex = 0; Round++; }
        }

        public void Remove(Creature c)
        {
            int idx = Order.FindIndex(x => x.Creature == c);
            if (idx < 0) return;
            Order.RemoveAt(idx);
            if (idx < CurrentIndex) CurrentIndex--;
            if (CurrentIndex >= Order.Count) CurrentIndex = 0;
        }

        public bool IsOver(out Team winner)
        {
            bool playersUp = Order.Any(x => x.Creature.Team == Team.Players && !x.Creature.IsDown);
            bool enemiesUp = Order.Any(x => x.Creature.Team == Team.Enemies && !x.Creature.IsDown);
            winner = playersUp ? Team.Players : Team.Enemies;
            return !playersUp || !enemiesUp;
        }

        /// <summary>Removes all concentration effects maintained by a creature.</summary>
        public void EndConcentration(Creature caster)
        {
            foreach (var cb in Order)
                cb.Creature.Effects.RemoveAll(e => e.Concentration && e.Source == caster);
            caster.ConcentratingOn = null;
        }
    }
}
