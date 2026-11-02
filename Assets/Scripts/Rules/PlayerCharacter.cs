using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    [Serializable]
    public class ItemStack
    {
        public string ItemId;
        public int Count;
        public ItemStack(string id, int count) { ItemId = id; Count = count; }
    }

    public partial class PlayerCharacter : Creature
    {
        public SpeciesDef Species;
        public BackgroundDef Background;
        public ClassDef Class;
        public int Level = 1;
        public int XP;

        /// <summary>Rolled scores as assigned by the player, before background increases.</summary>
        public int[] BaseScores = { 10, 10, 10, 10, 10, 10 };
        /// <summary>Background ability increases (+2/+1 or +1/+1/+1).</summary>
        public int[] BackgroundBonus = new int[6];
        /// <summary>Ability Score Improvements and feat increases from leveling (never past 20).</summary>
        public int[] AsiBonus = new int[6];
        /// <summary>Feats taken (by id), e.g. "grappler".</summary>
        public readonly List<string> Feats = new List<string>();

        public readonly HashSet<Skill> SkillProficiencies = new HashSet<Skill>();
        public readonly HashSet<Skill> Expertise = new HashSet<Skill>();

        public string FightingStyle;      // Fighter: "defense", "dueling", "great_weapon", "archery"
        /// <summary>Champion 7: Additional Fighting Style.</summary>
        public string FightingStyle2;
        public bool HasFightingStyle(string id) => id != null && (FightingStyle == id || FightingStyle2 == id);
        public string DivineOrder;        // Cleric: "protector", "thaumaturge"
        public readonly List<string> MasteredWeapons = new List<string>();

        public WeaponDef MainHand;
        /// <summary>The second weapon set (BG3-style melee/ranged): swapped in with SwapWeaponSets. Null = an empty hand.</summary>
        public WeaponDef OffSetWeapon;
        /// <summary>The shield belongs to the main set: it's on your back while the off-set is out.</summary>
        public bool ShieldStowed;
        /// <summary>Item ids this player marked as junk ("sell all junk" at any shop). Quest items never.</summary>
        public readonly HashSet<string> Junk = new HashSet<string>();
        public bool MarkJunk(string itemId, bool on)
        {
            var def = Content.ItemsDb.Get(itemId);
            if (def == null || def.QuestItem) return false;
            if (on) Junk.Add(itemId); else Junk.Remove(itemId);
            return true;
        }
        public ArmorDef Armor;
        public bool Shield;
        public Inventory Bag = new Inventory();
        public int Copper;
        /// <summary>Wondrous items being worn (one per slot; at most 3 needing attunement, 2024 rules).</summary>
        public readonly List<WondrousDef> Worn = new List<WondrousDef>();
        public const int MaxAttuned = 3;
        public bool LuckyRerollUsed;

        public int WornBonus(System.Func<WondrousDef, int> pick)
        {
            int b = 0;
            foreach (var w in Worn) b += pick(w);
            return b;
        }

        public bool CanWear(WondrousDef item, out string reason)
        {
            reason = null;
            int attuned = 0;
            foreach (var w in Worn) if (w.RequiresAttunement && w.Slot != item.Slot) attuned++;
            if (item.RequiresAttunement && attuned >= MaxAttuned) { reason = "You can only attune to 3 items."; return false; }
            return true;
        }

        /// <summary>Wears an item, replacing whatever was in the same slot. Returns the replaced item (or null).</summary>
        public WondrousDef Wear(WondrousDef item)
        {
            WondrousDef old = Worn.Find(w => w.Slot == item.Slot);
            if (old != null) Worn.Remove(old);
            Worn.Add(item);
            return old;
        }

        public bool HasLuckyReroll => !LuckyRerollUsed && Worn.Exists(w => w.RerollPerLongRest);
        public bool DeathSaveAdvantage => Worn.Exists(w => w.DeathSaveAdvantage);

        public readonly List<string> Cantrips = new List<string>();
        public readonly List<string> PreparedSpells = new List<string>();
        public readonly List<string> Spellbook = new List<string>();   // Wizard only
        public int[] SlotsUsed = new int[10];

        public readonly Dictionary<string, int> ResourcesUsed = new Dictionary<string, int>();
        public int HitDiceSpent;

        public int DeathSuccesses, DeathFailures;
        public bool Stable;
        /// <summary>A nonlethal fight (a bar brawl): at 0 HP you're knocked out, unconscious and stable (2024 knockout rule). No death saves, no massive-damage death.</summary>
        public bool Nonlethal;
        public bool HeroicInspiration;

        /// <summary>Bardic Inspiration die held from an ally (0 = none).</summary>
        public int HeldInspirationDie;

        public override bool IsPlayerCharacter => true;
        public override int ProficiencyBonus => 2 + (Level - 1) / 4;
        protected override int BaseSpeed => WildShape != null ? Math.Max(WildShape.Speed, WildShape.FlySpeed) : IsOverloaded ? 5 : (Species != null ? Species.Speed : 30) + (UnarmoredMovement ? (Level >= 10 ? 20 : Level >= 6 ? 15 : 10) : 0) + (FastMovement ? 10 : 0);
        bool FastMovement => Class != null && (Class.Id == "barbarian" && Level >= 5 || Class.Id == "ranger" && Level >= 6) && (Armor == null || Armor.Category != ArmorCategory.Heavy);

        // ---------------------------------------------------------------- monk

        public int MartialArtsDie => Level >= 17 ? 12 : Level >= 11 ? 10 : Level >= 5 ? 8 : 6;
        bool UnarmoredMovement => Class != null && Class.Id == "monk" && Level >= 2 && Armor == null && !Shield;
        /// <summary>Simple melee weapons and martial melee weapons with the Light property.</summary>
        public bool IsMonkWeapon(WeaponDef w) => Class.Id == "monk" && !w.IsRanged && (w.Category == WeaponCategory.Simple || w.Has(WeaponProperty.Light));
        /// <summary>Deflect Attacks: 1d10 + DEX + monk level of damage prevented.</summary>
        public int DeflectReduction() => Dice.Roll(10) + Mod(Ability.DEX) + Level;

        /// <summary>2024 rules: a Medium creature can carry STR × 15 lb. Coins weigh nothing (house rule).</summary>
        public int CarryCapacity => Score(Ability.STR) * 15;

        public float CarriedWeight
        {
            get
            {
                float w = Bag.TotalWeight();
                if (MainHand != null) w += MainHand.Weight;
                if (OffSetWeapon != null) w += OffSetWeapon.Weight;
                if (Armor != null) w += Armor.Weight;
                if (Shield) w += 6;
                foreach (var x in Worn) w += x.Weight;
                return w;
            }
        }

        public bool IsOverloaded => CarriedWeight > CarryCapacity;
        public override int Darkvision => Invocations.Contains("ember_sight") ? Math.Max(120, Species?.Darkvision ?? 0) : Species != null ? Species.Darkvision : 0;

        protected override int ScoreBonus(Ability a)
        {
            // Wild Shape: the beast's body (STR, DEX, CON), your own mind.
            if (WildShape != null && (a == Ability.STR || a == Ability.DEX || a == Ability.CON)) return WildShape.Scores[(int)a] - Scores[(int)a];
            return BackgroundBonus[(int)a] + AsiBonus[(int)a];
        }

        public PlayerCharacter() { Team = Team.Players; }

        // ---------------------------------------------------------------- creation

        /// <summary>Copies base scores into Scores and computes level 1 HP.</summary>
        public void FinalizeCreation()
        {
            Array.Copy(BaseScores, Scores, 6);
            MaxHP = Class.HitDie + Mod(Ability.CON) + (Species?.BonusHpPerLevel ?? 0);
            HP = MaxHP;
            if (Species != null && Species.HeroicInspirationOnRest) HeroicInspiration = true;
            foreach (var s in Class.Subclass != null ? Class.Subclass.AlwaysPreparedAt(Level) : new List<string>())
                if (!PreparedSpells.Contains(s)) PreparedSpells.Add(s);
        }

        // ---------------------------------------------------------------- leveling

        public static readonly int[] XpThresholds = { 0, 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000, 85000, 100000, 120000 };
        public const int ActOneLevelCap = 4, ActTwoLevelCap = 10, ActThreeLevelCap = 13;
        /// <summary>The highest level the current act allows (4 in Act I, 10 in Act II). Set by the game when the act changes.</summary>
        public static int LevelCap = ActOneLevelCap;

        public bool CanLevelUp => Level < LevelCap && Level + 1 < XpThresholds.Length && XP >= XpThresholds[Level + 1];

        public int AverageHitDie => Class.HitDie / 2 + 1;

        /// <summary>
        /// Gains a level: HP and new features only (choices are applied by LevelUp(plan)). hpRoll is the
        /// rolled hit die, or null to take the average (Critical Role style: the player chooses each time).
        /// </summary>
        public int GainLevel(int? hpRoll, List<string> log)
        {
            Level++;
            int gain = Math.Max(1, (hpRoll ?? AverageHitDie) + Mod(Ability.CON)) + (Species?.BonusHpPerLevel ?? 0);
            MaxHP += gain;
            HP += gain;
            log?.Add($"{Name} reaches level {Level}! +{gain} max HP.");
            foreach (var f in Class.Features) if (f.Level == Level) log?.Add($"New feature: {f.Name}");
            if (HasFeature("Draconic Resilience")) { int extra = Level == 3 ? 3 : 1; MaxHP += extra; HP += extra; }
            foreach (var kv in Class.AlwaysPrepared)
                if (kv.Value == Level && !PreparedSpells.Contains(kv.Key)) PreparedSpells.Add(kv.Key);
            if (Class.Subclass != null)
            {
                foreach (var f in Class.Subclass.Features) if (f.Level == Level) log?.Add($"New feature ({Class.Subclass.Name}): {f.Name}");
                foreach (var s in Class.Subclass.AlwaysPreparedAt(Level))
                    if (!PreparedSpells.Contains(s)) PreparedSpells.Add(s);
                if (LandType != null && CircleSpellsLater.TryGetValue(LandType, out var later) && later.TryGetValue(Level, out var circle))
                    foreach (var s in circle) if (!BonusAlwaysPrepared.Contains(s)) BonusAlwaysPrepared.Add(s);
            }
            return gain;
        }

        public bool HasFeature(string name)
        {
            foreach (var f in Class.Features) if (f.Level <= Level && f.Name == name) return true;
            if (Class.Subclass != null && Level >= 3)
                foreach (var f in Class.Subclass.Features) if (f.Level <= Level && f.Name == name) return true;
            return false;
        }

        // ---------------------------------------------------------------- derived stats

        protected override int BaseArmorClass
        {
            get
            {
                if (WildShape != null) return WildShape.AC;
                int dex = Mod(Ability.DEX);
                int ac;
                if (Armor != null)
                {
                    int dexPart = Armor.MaxDex < 0 ? dex : Math.Min(dex, Armor.MaxDex);
                    if (Armor.Category == ArmorCategory.Heavy) dexPart = 0;
                    ac = Armor.BaseAC + dexPart;
                    if (HasFightingStyle("defense")) ac += 1;
                }
                else if (HasEffect("mage_armor")) ac = 13 + dex;
                else if (Class.Id == "barbarian") ac = 10 + dex + Mod(Ability.CON); // Unarmored Defense
                else if (Class.Id == "monk" && !Shield) ac = 10 + dex + Mod(Ability.WIS); // Unarmored Defense (no shield either)
                else if (HasFeature("Draconic Resilience")) ac = 10 + dex + Mod(Ability.CHA);
                else if (Invocations.Contains("armor_of_shadows")) ac = 13 + dex; // Mage Armor at will
                else ac = 10 + dex;
                if (Shield) ac += 2;
                ac += WornBonus(w => w.AcBonus);
                if (HasEffect("barkskin")) ac = Math.Max(ac, 17);
                return ac;
            }
        }

        public bool IsProficient(Skill s) => SkillProficiencies.Contains(s);

        public override int SkillBonus(Skill s)
        {
            int b = Mod(SkillUtil.AbilityOf(s));
            if (Expertise.Contains(s)) b += 2 * ProficiencyBonus;
            else if (SkillProficiencies.Contains(s)) b += ProficiencyBonus;
            else if (HasFeature("Jack of All Trades")) b += ProficiencyBonus / 2;
            if (DivineOrder == "thaumaturge" && (s == Skill.Arcana || s == Skill.Religion)) b += Math.Max(1, Mod(Ability.WIS));
            if (PrimalOrder == "magician" && (s == Skill.Arcana || s == Skill.Nature)) b += Math.Max(1, Mod(Ability.WIS));
            return b;
        }

        public override int SaveBonus(Ability a)
        {
            int b = Mod(a);
            if (Array.IndexOf(Class.SavingThrows, a) >= 0) b += ProficiencyBonus;
            b += WornBonus(w => w.SaveBonus);
            return b;
        }

        public override bool InitiativeAdvantage => HasFeature("Remarkable Athlete") || HasFeature("Feral Instinct");

        public override bool ConditionImmune(Condition c) =>
            (Raging && HasFeature("Mindless Rage") && (c == Condition.Charmed || c == Condition.Frightened))
            || (c == Condition.Poisoned && HasFeature("Nature's Ward"));

        /// <summary>Bard 5: Font of Inspiration: spend a spell slot to regain a Bardic Inspiration use.</summary>
        public bool FontOfInspiration(List<string> log)
        {
            if (!HasFeature("Font of Inspiration") || ResourceLeft("bardic_inspiration") >= ResourceMax("bardic_inspiration")) return false;
            int slot = LowestAvailableSlot(1);
            if (slot <= 0) return false;
            SlotsUsed[slot]++;
            RegainResource("bardic_inspiration", 1);
            log?.Add($"{Name} turns a level {slot} slot into Bardic Inspiration.");
            return true;
        }

        /// <summary>Land 10: Nature's Ward: resistance to the damage type of your land.</summary>
        DamageType? NaturesWardResistance => !HasFeature("Nature's Ward") ? (DamageType?)null
            : LandType == "arid" ? DamageType.Fire : LandType == "polar" ? DamageType.Cold : LandType == "temperate" ? DamageType.Lightning : DamageType.Poison;

        // ---------------------------------------------------------------- monk, paladin, ranger 5-10

        /// <summary>Ranger 7 (Hunter): "escape_the_horde" or "multiattack_defense".</summary>
        public string HunterDefense;
        /// <summary>Multiattack Defense: the creature that hit you this turn (its other attacks have Disadvantage). Combat only.</summary>
        public Creature MultiattackDefenseAgainst;

        public int FlurryStrikes => HasFeature("Heightened Focus") ? 3 : 2;
        public int AbjureFoesTargets => Math.Max(1, Mod(Ability.CHA));
        public int KiSaveDC => 8 + ProficiencyBonus + Mod(Ability.WIS);

        /// <summary>Monk 5: Stunning Strike on a hit: 1 Focus, once per turn. CON save or Stunned until your next turn; on a success its Speed is halved and the next attack against it has Advantage.</summary>
        public bool TryStunningStrike(Creature target, TurnState turn, List<string> log)
        {
            if (!HasFeature("Stunning Strike") || turn == null || turn.StunningStrikeUsed || target.IsDead || !SpendResource("focus")) return false;
            turn.StunningStrikeUsed = true;
            var save = Checks.RollSave(target, Ability.CON, AdvantageState.Normal);
            if (!Checks.ResolveSave(save, KiSaveDC))
            {
                target.AddEffect(new ActiveEffect { Id = "stunned_strike", Name = "Stunned", Source = this, RoundsLeft = 1, Condition = Condition.Stunned });
                log?.Add($"Stunning Strike! {target.Name} is Stunned ({save.Total} vs DC {KiSaveDC}).");
            }
            else
            {
                target.AddEffect(new ActiveEffect { Id = "stun_glance", Name = "Reeling", Source = this, RoundsLeft = 1, SpeedHalved = true, GrantsAdvantageToAttackers = true, ConsumedWhenAttacked = true });
                log?.Add($"{target.Name} resists the stun but reels ({save.Total} vs DC {KiSaveDC}).");
            }
            return true;
        }

        /// <summary>Open Hand 6: Wholeness of Body: heal a Martial Arts die + WIS. Returns HP healed (0 if no uses).</summary>
        public int WholenessOfBody(List<string> log)
        {
            if (!SpendResource("wholeness")) return 0;
            int h = Heal(Math.Max(1, Dice.Roll(MartialArtsDie) + Mod(Ability.WIS)));
            log?.Add($"{Name} centres themself: +{h} HP (Wholeness of Body).");
            return h;
        }

        /// <summary>Ranger 10: Tireless: temp HP 1d8 + WIS. Returns the temp HP (0 if no uses).</summary>
        public int Tireless(List<string> log)
        {
            if (!SpendResource("tireless")) return 0;
            int t = Math.Max(1, Dice.Roll(8) + Mod(Ability.WIS));
            TempHP = Math.Max(TempHP, t);
            log?.Add($"{Name} pushes through (Tireless: {t} temporary HP).");
            return t;
        }

        /// <summary>Fighter 9: Tactical Master: swap a weapon's mastery for Push, Sap or Slow on an attack.</summary>
        public List<WeaponMastery> TacticalMasteries() =>
            HasFeature("Tactical Master") ? new List<WeaponMastery> { WeaponMastery.Push, WeaponMastery.Sap, WeaponMastery.Slow } : new List<WeaponMastery>();

        /// <summary>Things that happen at the start of each of your turns in combat (Champion 10: Heroic Warrior).</summary>
        public void StartOfTurnInCombat(List<string> log)
        {
            if (HasFeature("Heroic Warrior") && !HeroicInspiration && !IsDown)
            {
                HeroicInspiration = true;
                log?.Add($"{Name} feels heroic: Heroic Inspiration!");
            }
        }
        public override int InitiativeBonus => Mod(Ability.DEX) + (Background != null && Background.OriginFeat == "Alert" ? ProficiencyBonus : 0);

        public override bool AdvantageOnSave(Ability a, string vs)
        {
            if (EffectSaveAdvantage(a)) return true;
            if (vs == "concentration" && Invocations.Contains("eldritch_mind")) return true;
            if (a == Ability.STR && Raging) return true;
            if (a == Ability.DEX && HasFeature("Danger Sense") && !Has(Condition.Incapacitated)) return true;
            if (Species == null) return false;
            if (Species.AdvantageMentalSaves && (a == Ability.INT || a == Ability.WIS || a == Ability.CHA)) return true;
            if (Species.AdvantageVsCharmed && vs == "charmed") return true;
            if (Species.PoisonResilience && vs == "poison") return true;
            return false;
        }

        // ---------------------------------------------------------------- spellcasting

        public bool IsCaster => Class.Caster != CasterType.None;
        public int SpellMod => IsCaster ? Mod(Class.SpellAbility) : 0;
        public int SpellSaveDC => 8 + ProficiencyBonus + SpellMod + (HasEffect("innate_sorcery") ? 1 : 0);
        public int SpellAttackBonus => ProficiencyBonus + SpellMod + WornBonus(w => w.SpellAttackBonus);

        public int SlotsMax(int slotLevel) => SpellSlots.For(Class.Caster, Level, slotLevel);
        public int SlotsLeft(int slotLevel) => SlotsMax(slotLevel) - SlotsUsed[slotLevel];

        public int LowestAvailableSlot(int minLevel)
        {
            for (int l = Math.Max(1, minLevel); l <= 9; l++) if (SlotsLeft(l) > 0) return l;
            return -1;
        }

        public bool KnowsSpell(string id) => Cantrips.Contains(id) || PreparedSpells.Contains(id) || BonusAlwaysPrepared.Contains(id);

        public int CantripDiceMultiplier => Level >= 17 ? 4 : Level >= 11 ? 3 : Level >= 5 ? 2 : 1;

        // ---------------------------------------------------------------- resources

        public int ResourceMax(string res)
        {
            switch (res)
            {
                case "second_wind": return Class.Id == "fighter" ? (Level >= 10 ? 4 : Level >= 4 ? 3 : 2) : 0;
                case "action_surge": return Class.Id == "fighter" && Level >= 2 ? 1 : 0;
                case "sorcerous_restoration": return HasFeature("Sorcerous Restoration") ? 1 : 0;
                case "embers_luck": return HasFeature("Ember's Own Luck") ? Math.Max(1, Mod(Ability.CHA)) : 0;
                case "divine_intervention": return HasFeature("Divine Intervention") ? 1 : 0;
                case "natural_recovery": return HasFeature("Natural Recovery") ? 1 : 0;
                case "wholeness": return HasFeature("Wholeness of Body") ? Math.Max(1, Mod(Ability.WIS)) : 0;
                case "tireless": return HasFeature("Tireless") ? Math.Max(1, Mod(Ability.WIS)) : 0;
                case "indomitable": return Class.Id == "fighter" && Level >= 9 ? (Level >= 17 ? 3 : Level >= 13 ? 2 : 1) : 0;
                case "channel_divinity": return Class.Id == "cleric" && Level >= 2 ? (Level >= 6 ? 3 : 2) : Class.Id == "paladin" && Level >= 3 ? (Level >= 11 ? 3 : 2) : 0;
                case "focus": return Class.Id == "monk" && Level >= 2 ? Level : 0;
                case "uncanny_metabolism": return Class.Id == "monk" && Level >= 2 ? 1 : 0;
                case "lay_on_hands": return Class.Id == "paladin" ? 5 * Level : 0;
                case "free_smite": return Class.Id == "paladin" && Level >= 2 ? 1 : 0;
                case "innate_sorcery": return Class.Id == "sorcerer" ? 2 : 0;
                case "sorcery_points": return Class.Id == "sorcerer" && Level >= 2 ? Level : 0;
                case "magical_cunning": return Class.Id == "warlock" && Level >= 2 ? 1 : 0;
                case "wild_shape": return Class.Id == "druid" && Level >= 2 ? (Level >= 17 ? 4 : Level >= 6 ? 3 : 2) : 0;
                case "favored_enemy": return Class.Id == "ranger" ? (Level >= 17 ? 6 : Level >= 13 ? 5 : Level >= 9 ? 4 : Level >= 5 ? 3 : 2) : 0;
                case "bardic_inspiration": return Class.Id == "bard" ? Math.Max(1, Mod(Ability.CHA)) : 0;
                case "arcane_recovery": return Class.Id == "wizard" ? 1 : 0;
                case "rage": return Class.Id == "barbarian" ? (Level >= 17 ? 6 : Level >= 12 ? 5 : Level >= 6 ? 4 : Level >= 3 ? 3 : 2) : 0;
                default: return 0;
            }
        }

        public int ResourceLeft(string res)
        {
            ResourcesUsed.TryGetValue(res, out int used);
            return ResourceMax(res) - used;
        }

        public bool SpendResource(string res) => SpendResource(res, 1);

        /// <summary>Spends several points at once (Lay on Hands pool, Focus Points). False (and nothing spent) if there aren't enough.</summary>
        public bool SpendResource(string res, int amount)
        {
            if (amount <= 0 || ResourceLeft(res) < amount) return false;
            ResourcesUsed.TryGetValue(res, out int used);
            ResourcesUsed[res] = used + amount;
            return true;
        }

        void RegainResource(string res, int amount)
        {
            if (!ResourcesUsed.TryGetValue(res, out int used)) return;
            ResourcesUsed[res] = Math.Max(0, used - amount);
        }

        public int BardicDie => Level >= 15 ? 12 : Level >= 10 ? 10 : Level >= 5 ? 8 : 6;

        // ---------------------------------------------------------------- attacks

        public bool IsProficientWith(WeaponDef w) => w.Category == WeaponCategory.Simple || Class.MartialWeapons || DivineOrder == "protector" || PrimalOrder == "warden"
            || Class.MartialFinesseOrLight && (w.Has(WeaponProperty.Finesse) || w.Has(WeaponProperty.Light))
            || Invocations.Contains("pact_of_the_blade");

        // ---------------------------------------------------------------- barbarian

        public int RageDamage => Level >= 16 ? 4 : Level >= 9 ? 3 : 2;
        public bool Raging => HasEffect("rage");

        /// <summary>Bonus Action: enter a Rage (spends a use). False if out of uses or already raging.</summary>
        public bool StartRage(List<string> log)
        {
            if (Raging || !SpendResource("rage")) return false;
            Effects.Add(EffectLibrary.Create("rage", this));
            if (HasFeature("Mindless Rage")) Effects.RemoveAll(e => e.Condition == Condition.Charmed || e.Condition == Condition.Frightened);
            log?.Add($"{Name} flies into a RAGE! (+{RageDamage} damage, resists bludgeoning/piercing/slashing)");
            return true;
        }

        public override int TakeDamage(int amount, DamageType type, bool critical, List<string> log)
        {
            // Rage: resistance to bludgeoning, piercing and slashing (added only for this hit).
            bool rageResist = ((Raging && (type == DamageType.Bludgeoning || type == DamageType.Piercing || type == DamageType.Slashing)) || NaturesWardResistance == type
                || AffinityType == type || (HasFeature("Ember Resilience") && type == DamageType.Fire)) && !Resistances.Contains(type);
            if (rageResist) Resistances.Add(type);
            try { return base.TakeDamage(amount, type, critical, log); }
            finally { if (rageResist) Resistances.Remove(type); }
        }

        const string RelentlessKey = "relentless_dc_steps";
        /// <summary>Barbarian 11: the DC of the next Relentless Rage save (10, +5 per use since the last rest).</summary>
        public int RelentlessRageDC { get { ResourcesUsed.TryGetValue(RelentlessKey, out int s); return 10 + 5 * s; } }

        /// <summary>Brutal Strike riders this barbarian can pick: 1 Forceful, 2 Hamstring; 13: 3 Staggering, 4 Sundering.</summary>
        public List<int> BrutalStrikeOptions()
        {
            var l = new List<int>();
            if (!HasFeature("Brutal Strike")) return l;
            l.Add(1); l.Add(2);
            if (HasFeature("Improved Brutal Strike")) { l.Add(3); l.Add(4); }
            return l;
        }

        /// <summary>Rogue: Cunning Strike effects allowed on one Sneak Attack (2 with Improved Cunning Strike).</summary>
        public int CunningStrikesPerSneak => HasFeature("Improved Cunning Strike") ? 2 : HasFeature("Cunning Strike") ? 1 : 0;

        /// <summary>Monk: Deflect Attacks covers bludgeoning/piercing/slashing; Deflect Energy (13) covers every type.</summary>
        public bool CanDeflect(DamageType t) => HasFeature("Deflect Attacks")
            && (HasFeature("Deflect Energy") || t == DamageType.Bludgeoning || t == DamageType.Piercing || t == DamageType.Slashing);

        /// <summary>Ranger 13: Relentless Hunter: damage never breaks concentration on Hunter's Mark.</summary>
        public bool ConcentrationImmuneToDamage => HasFeature("Relentless Hunter") && ConcentratingOn == "hunters_mark";

        public AttackOption WeaponAttack(WeaponDef w)
        {
            bool finesse = w.Has(WeaponProperty.Finesse) || IsMonkWeapon(w);
            Ability ab = w.IsRanged ? Ability.DEX : Ability.STR;
            if (finesse && Mod(Ability.DEX) > Mod(Ability.STR)) ab = Ability.DEX;
            bool shillelagh = HasEffect("shillelagh") && (w.Id == "club" || w.Id == "quarterstaff");
            if (shillelagh) ab = Class.SpellAbility;
            if (Invocations.Contains("pact_of_the_blade") && !w.IsRanged && Mod(Ability.CHA) > Mod(ab)) ab = Ability.CHA;
            int mod = Mod(ab);
            int toHit = mod + (IsProficientWith(w) ? ProficiencyBonus : 0) + w.MagicBonus;
            if (w.IsRanged && HasFightingStyle("archery")) toHit += 2;
            if (HasEffect("sacred_weapon")) toHit += Math.Max(1, Mod(Ability.CHA));

            var dmg = w.Damage;
            bool twoHanding = !Shield && w.Has(WeaponProperty.Versatile);
            if (twoHanding) dmg = w.VersatileDamage;
            if (IsMonkWeapon(w) && dmg.Count == 1 && dmg.Sides < MartialArtsDie) dmg = new DiceExpr(1, MartialArtsDie, dmg.Bonus); // Martial Arts die
            if (shillelagh) dmg = new DiceExpr(1, Level >= 17 ? 12 : Level >= 11 ? 10 : Level >= 5 ? 10 : 8);

            int dmgMod = mod + w.MagicBonus;
            if (Raging && ab == Ability.STR && !w.IsRanged) dmgMod += RageDamage;
            bool oneHandedMelee = !w.IsRanged && !w.Has(WeaponProperty.TwoHanded) && !twoHanding;
            if (HasFightingStyle("dueling") && oneHandedMelee) dmgMod += 2;

            var opt = new AttackOption
            {
                Name = w.Name,
                ToHit = toHit,
                Damage = dmg,
                DamageMod = dmgMod,
                DamageType = w.DamageType,
                ReachFt = w.ReachFt,
                Ranged = w.IsRanged,
                RangeNormal = w.RangeNormal,
                RangeLong = w.RangeLong,
                Weapon = w,
                Ability = ab,
                Mastery = MasteredWeapons.Contains(w.Id) ? w.Mastery : WeaponMastery.None,
                CritOn = HasFeature("Improved Critical") ? 19 : 20,
                ExtraDamage = w.ExtraDamage,
                ExtraDamageType = w.ExtraDamageType,
                MinDieValue = HasFightingStyle("great_weapon") && (w.Has(WeaponProperty.TwoHanded) || twoHanding) && !w.IsRanged ? 3 : 0,
            };
            return opt;
        }

        public AttackOption UnarmedStrike()
        {
            bool monk = Class.Id == "monk";
            var ab = monk && Mod(Ability.DEX) > Mod(Ability.STR) ? Ability.DEX : Ability.STR;
            return new AttackOption
            {
                Name = "Unarmed Strike",
                ToHit = Mod(ab) + ProficiencyBonus,
                Damage = monk ? new DiceExpr(1, MartialArtsDie) : new DiceExpr(0, 0, 1),
                DamageMod = Mod(ab),
                DamageType = DamageType.Bludgeoning,
                Ability = ab,
            };
        }

        public override List<AttackOption> GetAttacks()
        {
            if (WildShape != null) return Monster.AttacksOf(WildShape);
            var list = new List<AttackOption>();
            list.Add(MainHand != null ? WeaponAttack(MainHand) : UnarmedStrike());
            return list;
        }

        static readonly string[] ExtraAttackClasses = { "fighter", "barbarian", "monk", "paladin", "ranger" };
        public int AttacksPerAction => Class.Id == "fighter" && Level >= 11 ? 3 : Level >= 5 && (Array.IndexOf(ExtraAttackClasses, Class.Id) >= 0 || ThirstingBlade) ? 2 : 1;

        // ---------------------------------------------------------------- death saves

        protected override void OnDamaged(int hpBefore, int amount, bool critical, List<string> log)
        {
            if (HP > 0) return;
            EndWildShape(log); // dropping to 0 HP ends Wild Shape
            int overflow = -HP;
            HP = 0;
            if (Nonlethal)
            {
                if (hpBefore > 0) log?.Add($"{Name} is knocked out cold!");
                Stable = true;
                DeathSuccesses = DeathFailures = 0;
                ConcentratingOn = null;
                return;
            }
            if (hpBefore > 0)
            {
                // Massive damage: remaining damage >= max HP kills outright.
                if (overflow >= MaxHP) { Die(log, "is slain outright by massive damage"); return; }
                // Barbarian 11: Relentless Rage: a CON save to keep fighting (DC 10, +5 per use, reset by a rest).
                if (Raging && HasFeature("Relentless Rage"))
                {
                    int dc = RelentlessRageDC;
                    ResourcesUsed.TryGetValue(RelentlessKey, out int steps);
                    ResourcesUsed[RelentlessKey] = steps + 1;
                    var save = Checks.RollSave(this, Ability.CON, AdvantageState.Normal);
                    if (Checks.ResolveSave(save, dc))
                    {
                        HP = 2 * Level;
                        log?.Add($"{Name} refuses to fall! Relentless Rage (CON {save.Total} vs DC {dc}): back up with {HP} HP.");
                        return;
                    }
                    log?.Add($"Relentless Rage fails (CON {save.Total} vs DC {dc}).");
                }
                Stable = false;
                DeathSuccesses = DeathFailures = 0;
                log?.Add($"{Name} drops to 0 HP and falls unconscious!");
                ConcentratingOn = null;
            }
            else
            {
                Stable = false;
                DeathFailures += critical ? 2 : 1;
                log?.Add($"{Name} takes damage while down: {(critical ? 2 : 1)} death save failure(s).");
                if (DeathFailures >= 3) Die(log, "has died");
            }
        }

        void Die(List<string> log, string how)
        {
            HP = 0;
            IsDead = true;
            log?.Add($"{Name} {how}.");
        }

        public enum DeathSaveOutcome { Success, Failure, Stabilized, Revived, Died }

        public D20Result RollDeathSave()
        {
            var terms = new List<RollTerm>();
            if (D20Penalty != 0) terms.Add(new RollTerm("exhaustion", D20Penalty));
            return Dice.RollD20(DeathSaveAdvantage ? AdvantageState.Advantage : AdvantageState.Normal, terms);
        }

        /// <summary>Applies an already-rolled death saving throw (the UI shows the d20).</summary>
        public DeathSaveOutcome ApplyDeathSave(D20Result roll, List<string> log)
        {
            int n = roll.Natural;
            if (n == 20)
            {
                HP = 1; DeathSuccesses = DeathFailures = 0; Stable = false;
                log?.Add($"Natural 20! {Name} surges back to consciousness with 1 HP!");
                return DeathSaveOutcome.Revived;
            }
            if (n == 1) DeathFailures += 2;
            else if (roll.Total >= 10) DeathSuccesses++;
            else DeathFailures++;

            if (DeathFailures >= 3) { Die(log, "has died"); return DeathSaveOutcome.Died; }
            if (DeathSuccesses >= 3)
            {
                Stable = true; DeathSuccesses = DeathFailures = 0;
                log?.Add($"{Name} is stable.");
                return DeathSaveOutcome.Stabilized;
            }
            return roll.Total >= 10 && n != 1 ? DeathSaveOutcome.Success : DeathSaveOutcome.Failure;
        }

        public override int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            if (HP == 0) { DeathSuccesses = DeathFailures = 0; Stable = false; }
            return base.Heal(amount);
        }

        /// <summary>Ember of Return / Revivify: returns a dead character with 1 HP.</summary>
        public void Revive(int exhaustionGained, List<string> log)
        {
            IsDead = false;
            HP = 1;
            DeathSuccesses = DeathFailures = 0;
            Stable = false;
            Exhaustion = Math.Min(6, Exhaustion + exhaustionGained);
            log?.Add($"{Name} gasps back to life!");
        }

        /// <summary>Restores the dead state when loading a save.</summary>
        public void MarkDead() { HP = 0; IsDead = true; }

        public void Stabilize()
        {
            if (HP == 0 && !IsDead) { Stable = true; DeathSuccesses = DeathFailures = 0; }
        }

        // ---------------------------------------------------------------- rests

        public int HitDiceRemaining => Level - HitDiceSpent;

        /// <summary>Spends one hit die during a short rest. Returns the die roll result (already applied).</summary>
        public DamageRoll SpendHitDie(List<string> log)
        {
            if (HitDiceRemaining <= 0) return null;
            HitDiceSpent++;
            var roll = Dice.RollDamage(new DiceExpr(1, Class.HitDie), false, new[] { new RollTerm("CON", Mod(Ability.CON)) });
            int healed = Heal(Math.Max(0, roll.Total));
            log?.Add($"{Name} spends a hit die and recovers {healed} HP.");
            return roll;
        }

        public void ShortRest(List<string> log)
        {
            ResourcesUsed.Remove(RelentlessKey);
            RegainResource("second_wind", 1);
            RegainResource("action_surge", 99);
            RegainResource("channel_divinity", 1);
            RegainResource("rage", 1);
            RegainResource("focus", 99);
            RegainResource("wild_shape", 1);
            if (Class.Caster == CasterType.Pact) SlotsUsed = new int[10]; // Pact Magic: slots back on a Short Rest
            if (HasFeature("Font of Inspiration")) RegainResource("bardic_inspiration", 99);
            SorcerousRestoration(log);
            NaturalRecovery(log);
            if (HasFeature("Tireless") && Exhaustion > 0) { Exhaustion--; log?.Add($"{Name} shakes off some weariness (Tireless)."); }
            log?.Add($"{Name} finishes a short rest.");
        }

        public void LongRest(bool ate, List<string> log)
        {
            if (IsDead) return;
            HP = EffectiveMaxHP;
            TempHP = 0;
            HitDiceSpent = 0;
            SlotsUsed = new int[10];
            ResourcesUsed.Clear();
            DeathSuccesses = DeathFailures = 0;
            Stable = false;
            Effects.RemoveAll(e => e.Id != "aid");
            ConcentratingOn = null;
            if (ate && Exhaustion > 0) Exhaustion--;
            else if (!ate) { Exhaustion = Math.Min(6, Exhaustion + 1); log?.Add($"{Name} went to bed hungry (+1 Exhaustion)."); }
            if (Species != null && Species.HeroicInspirationOnRest) HeroicInspiration = true;
            LuckyRerollUsed = false;
            if (Exhaustion >= 6) Die(log, "succumbs to exhaustion");
            log?.Add($"{Name} finishes a long rest.");
        }

        // ---------------------------------------------------------------- inventory

        public int CountOf(string itemId) => Bag.Count(itemId);

        /// <summary>Adds to the bag. Returns the leftover that didn't fit (the game drops it on the ground).</summary>
        public int AddItem(string itemId, int count = 1) => Bag.Add(itemId, count);

        public bool RemoveItem(string itemId, int count = 1) => Bag.Remove(itemId, count);

        /// <summary>
        /// Equips the item in a bag slot. Whatever was equipped goes back into that same slot (a clean swap).
        /// </summary>
        public bool EquipFromBag(int slot, out string message, bool offSet = false)
        {
            message = null;
            var stack = Bag.Slots[slot];
            if (stack == null) return false;
            var def = Content.ItemsDb.Get(stack.ItemId);
            string returned = null;
            if (offSet)
            {
                if (!(def is WeaponDef ow)) { message = "Only a weapon goes in the off-set."; return false; }
                returned = OffSetWeapon?.Id; OffSetWeapon = ow;
                Bag.PickUp(slot, 1);
                if (returned != null) { if (Bag.Slots[slot] == null) Bag.Slots[slot] = new ItemStack(returned, 1); else Bag.Add(returned, 1); }
                message = $"{def.Name} is your off-set weapon. (Swap: Z, or hold X on a controller.)";
                return true;
            }
            switch (def)
            {
                case WeaponDef w:
                    if (w.Has(WeaponProperty.TwoHanded) && Shield)
                    {
                        if (Bag.SpaceFor("shield") < 1) { message = "No room to stow your shield."; return false; }
                        Shield = false;
                        Bag.Add("shield", 1);
                    }
                    returned = MainHand?.Id; MainHand = w; break;
                case ArmorDef a:
                    if (!CanWearArmor(a)) { message = $"You aren't trained in {a.Category.ToString().ToLower()} armor."; return false; }
                    returned = Armor?.Id; Armor = a; break;
                case WondrousDef wd:
                    if (!CanWear(wd, out message)) return false;
                    returned = Wear(wd)?.Id; break;
                default:
                    if (def != null && def.Kind == ItemKind.Shield)
                    {
                        if (!Class.Shields) { message = "You aren't trained with shields."; return false; }
                        if (MainHand != null && MainHand.Has(WeaponProperty.TwoHanded)) { message = "Your weapon needs both hands."; return false; }
                        if (Shield || ShieldStowed) { message = "You already have a shield."; return false; }
                        Shield = true; break;
                    }
                    message = "That can't be equipped.";
                    return false;
            }
            Bag.PickUp(slot, 1);
            if (returned != null)
            {
                if (Bag.Slots[slot] == null) Bag.Slots[slot] = new ItemStack(returned, 1);
                else Bag.Add(returned, 1);
            }
            message = $"Equipped {def.Name}.";
            return true;
        }

        /// <summary>
        /// Swaps the main and off-set weapons. The shield goes on your back with the main set and comes back with it,
        /// so a sword-and-board fighter can swap to a longbow. (In combat the caller charges the free object interaction.)
        /// </summary>
        public bool SwapWeaponSets(out string message)
        {
            (MainHand, OffSetWeapon) = (OffSetWeapon, MainHand);
            if (ShieldStowed) { ShieldStowed = false; Shield = true; }
            else if (Shield) { Shield = false; ShieldStowed = true; }
            message = MainHand != null ? $"You draw your {MainHand.Name}." : "You put your weapon away.";
            return true;
        }

        public bool CanWearArmor(ArmorDef a)
        {
            switch (a.Category)
            {
                case ArmorCategory.Light: return Class.LightArmor;
                case ArmorCategory.Medium: return Class.MediumArmor || PrimalOrder == "warden";
                default: return Class.HeavyArmor || DivineOrder == "protector";
            }
        }

        /// <summary>Unequips into the bag. slotName: "weapon", "armor", "shield", or a WornSlot name.</summary>
        public bool Unequip(string slotName)
        {
            string id = null;
            switch (slotName)
            {
                case "weapon": id = MainHand?.Id; break;
                case "offset": id = OffSetWeapon?.Id; break;
                case "armor": id = Armor?.Id; break;
                case "shield": id = Shield ? "shield" : null; break;
                default:
                    var w = Worn.Find(x => x.Slot.ToString() == slotName);
                    id = w?.Id; break;
            }
            if (id == null || Bag.SpaceFor(id) < 1) return false;
            Bag.Add(id, 1);
            switch (slotName)
            {
                case "weapon": MainHand = null; break;
                case "offset": OffSetWeapon = null; break;
                case "armor": Armor = null; break;
                case "shield": Shield = false; break;
                default: Worn.RemoveAll(x => x.Slot.ToString() == slotName); break;
            }
            return true;
        }

        /// <summary>Gives gold to another player (co-op transfers).</summary>
        public bool GiveCopper(PlayerCharacter other, int cp)
        {
            if (cp <= 0 || Copper < cp) return false;
            Copper -= cp; other.Copper += cp;
            return true;
        }
    }
}
