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

    public class PlayerCharacter : Creature
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

        public readonly HashSet<Skill> SkillProficiencies = new HashSet<Skill>();
        public readonly HashSet<Skill> Expertise = new HashSet<Skill>();

        public string FightingStyle;      // Fighter: "defense", "dueling", "great_weapon", "archery"
        public string DivineOrder;        // Cleric: "protector", "thaumaturge"
        public readonly List<string> MasteredWeapons = new List<string>();

        public WeaponDef MainHand;
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
        public bool HeroicInspiration;

        /// <summary>Bardic Inspiration die held from an ally (0 = none).</summary>
        public int HeldInspirationDie;

        public override bool IsPlayerCharacter => true;
        public override int ProficiencyBonus => 2 + (Level - 1) / 4;
        protected override int BaseSpeed => IsOverloaded ? 5 : Species != null ? Species.Speed : 30;

        /// <summary>2024 rules: a Medium creature can carry STR × 15 lb. Coins weigh nothing (house rule).</summary>
        public int CarryCapacity => Score(Ability.STR) * 15;

        public float CarriedWeight
        {
            get
            {
                float w = Bag.TotalWeight();
                if (MainHand != null) w += MainHand.Weight;
                if (Armor != null) w += Armor.Weight;
                if (Shield) w += 6;
                foreach (var x in Worn) w += x.Weight;
                return w;
            }
        }

        public bool IsOverloaded => CarriedWeight > CarryCapacity;
        public override int Darkvision => Species != null ? Species.Darkvision : 0;

        protected override int ScoreBonus(Ability a) => BackgroundBonus[(int)a];

        public PlayerCharacter() { Team = Team.Players; }

        // ---------------------------------------------------------------- creation

        /// <summary>Copies base scores into Scores and computes level 1 HP.</summary>
        public void FinalizeCreation()
        {
            Array.Copy(BaseScores, Scores, 6);
            MaxHP = Class.HitDie + Mod(Ability.CON) + (Species?.BonusHpPerLevel ?? 0);
            HP = MaxHP;
            if (Species != null && Species.HeroicInspirationOnRest) HeroicInspiration = true;
            foreach (var s in Class.Subclass != null && Level >= 3 ? Class.Subclass.AlwaysPreparedSpells : new List<string>())
                if (!PreparedSpells.Contains(s)) PreparedSpells.Add(s);
        }

        // ---------------------------------------------------------------- leveling

        public static readonly int[] XpThresholds = { 0, 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000 };
        public const int PlaytestLevelCap = 3;

        public bool CanLevelUp => Level < PlaytestLevelCap && Level + 1 < XpThresholds.Length && XP >= XpThresholds[Level + 1];

        public int AverageHitDie => Class.HitDie / 2 + 1;

        /// <summary>
        /// Levels up. hpRoll is the value of the rolled hit die, or null to take the average
        /// (Critical Role style: the player chooses each time).
        /// </summary>
        public int LevelUp(int? hpRoll, List<string> log)
        {
            Level++;
            int gain = Math.Max(1, (hpRoll ?? AverageHitDie) + Mod(Ability.CON)) + (Species?.BonusHpPerLevel ?? 0);
            MaxHP += gain;
            HP += gain;
            log?.Add($"{Name} reaches level {Level}! +{gain} max HP.");
            foreach (var f in Class.Features) if (f.Level == Level) log?.Add($"New feature: {f.Name}");
            if (Class.Subclass != null)
            {
                foreach (var f in Class.Subclass.Features) if (f.Level == Level) log?.Add($"New feature ({Class.Subclass.Name}): {f.Name}");
                if (Level == 3)
                    foreach (var s in Class.Subclass.AlwaysPreparedSpells)
                        if (!PreparedSpells.Contains(s)) PreparedSpells.Add(s);
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
                int dex = Mod(Ability.DEX);
                int ac;
                if (Armor != null)
                {
                    int dexPart = Armor.MaxDex < 0 ? dex : Math.Min(dex, Armor.MaxDex);
                    if (Armor.Category == ArmorCategory.Heavy) dexPart = 0;
                    ac = Armor.BaseAC + dexPart;
                    if (FightingStyle == "defense") ac += 1;
                }
                else if (HasEffect("mage_armor")) ac = 13 + dex;
                else ac = 10 + dex;
                if (Shield) ac += 2;
                ac += WornBonus(w => w.AcBonus);
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
            return b;
        }

        public override int SaveBonus(Ability a)
        {
            int b = Mod(a);
            if (Array.IndexOf(Class.SavingThrows, a) >= 0) b += ProficiencyBonus;
            b += WornBonus(w => w.SaveBonus);
            return b;
        }

        public override bool InitiativeAdvantage => HasFeature("Remarkable Athlete");
        public override int InitiativeBonus => Mod(Ability.DEX) + (Background != null && Background.OriginFeat == "Alert" ? ProficiencyBonus : 0);

        public override bool AdvantageOnSave(Ability a, string vs)
        {
            if (Species == null) return false;
            if (Species.AdvantageMentalSaves && (a == Ability.INT || a == Ability.WIS || a == Ability.CHA)) return true;
            if (Species.AdvantageVsCharmed && vs == "charmed") return true;
            if (Species.PoisonResilience && vs == "poison") return true;
            return false;
        }

        // ---------------------------------------------------------------- spellcasting

        public bool IsCaster => Class.Caster != CasterType.None;
        public int SpellMod => IsCaster ? Mod(Class.SpellAbility) : 0;
        public int SpellSaveDC => 8 + ProficiencyBonus + SpellMod;
        public int SpellAttackBonus => ProficiencyBonus + SpellMod + WornBonus(w => w.SpellAttackBonus);

        public int SlotsMax(int slotLevel) => SpellSlots.For(Class.Caster, Level, slotLevel);
        public int SlotsLeft(int slotLevel) => SlotsMax(slotLevel) - SlotsUsed[slotLevel];

        public int LowestAvailableSlot(int minLevel)
        {
            for (int l = Math.Max(1, minLevel); l <= 9; l++) if (SlotsLeft(l) > 0) return l;
            return -1;
        }

        public bool KnowsSpell(string id) => Cantrips.Contains(id) || PreparedSpells.Contains(id);

        public int CantripDiceMultiplier => Level >= 17 ? 4 : Level >= 11 ? 3 : Level >= 5 ? 2 : 1;

        // ---------------------------------------------------------------- resources

        public int ResourceMax(string res)
        {
            switch (res)
            {
                case "second_wind": return Class.Id == "fighter" ? (Level >= 10 ? 4 : Level >= 4 ? 3 : 2) : 0;
                case "action_surge": return Class.Id == "fighter" && Level >= 2 ? 1 : 0;
                case "channel_divinity": return Class.Id == "cleric" && Level >= 2 ? (Level >= 6 ? 3 : 2) : 0;
                case "bardic_inspiration": return Class.Id == "bard" ? Math.Max(1, Mod(Ability.CHA)) : 0;
                case "arcane_recovery": return Class.Id == "wizard" ? 1 : 0;
                default: return 0;
            }
        }

        public int ResourceLeft(string res)
        {
            ResourcesUsed.TryGetValue(res, out int used);
            return ResourceMax(res) - used;
        }

        public bool SpendResource(string res)
        {
            if (ResourceLeft(res) <= 0) return false;
            ResourcesUsed.TryGetValue(res, out int used);
            ResourcesUsed[res] = used + 1;
            return true;
        }

        void RegainResource(string res, int amount)
        {
            if (!ResourcesUsed.TryGetValue(res, out int used)) return;
            ResourcesUsed[res] = Math.Max(0, used - amount);
        }

        public int BardicDie => Level >= 15 ? 12 : Level >= 10 ? 10 : Level >= 5 ? 8 : 6;

        // ---------------------------------------------------------------- attacks

        public bool IsProficientWith(WeaponDef w) => w.Category == WeaponCategory.Simple || Class.MartialWeapons || DivineOrder == "protector";

        public AttackOption WeaponAttack(WeaponDef w)
        {
            bool finesse = w.Has(WeaponProperty.Finesse);
            Ability ab = w.IsRanged ? Ability.DEX : Ability.STR;
            if (finesse && Mod(Ability.DEX) > Mod(Ability.STR)) ab = Ability.DEX;
            int mod = Mod(ab);
            int toHit = mod + (IsProficientWith(w) ? ProficiencyBonus : 0) + w.MagicBonus;
            if (w.IsRanged && FightingStyle == "archery") toHit += 2;

            var dmg = w.Damage;
            bool twoHanding = !Shield && w.Has(WeaponProperty.Versatile);
            if (twoHanding) dmg = w.VersatileDamage;

            int dmgMod = mod + w.MagicBonus;
            bool oneHandedMelee = !w.IsRanged && !w.Has(WeaponProperty.TwoHanded) && !twoHanding;
            if (FightingStyle == "dueling" && oneHandedMelee) dmgMod += 2;

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
                MinDieValue = FightingStyle == "great_weapon" && (w.Has(WeaponProperty.TwoHanded) || twoHanding) && !w.IsRanged ? 3 : 0,
            };
            return opt;
        }

        public AttackOption UnarmedStrike()
        {
            return new AttackOption
            {
                Name = "Unarmed Strike",
                ToHit = Mod(Ability.STR) + ProficiencyBonus,
                Damage = new DiceExpr(0, 0, 1),
                DamageMod = Mod(Ability.STR),
                DamageType = DamageType.Bludgeoning,
                Ability = Ability.STR,
            };
        }

        public override List<AttackOption> GetAttacks()
        {
            var list = new List<AttackOption>();
            list.Add(MainHand != null ? WeaponAttack(MainHand) : UnarmedStrike());
            return list;
        }

        public int AttacksPerAction => Class.Id == "fighter" && Level >= 5 ? 2 : 1;

        // ---------------------------------------------------------------- death saves

        protected override void OnDamaged(int hpBefore, int amount, bool critical, List<string> log)
        {
            if (HP > 0) return;
            int overflow = -HP;
            HP = 0;
            if (hpBefore > 0)
            {
                // Massive damage: remaining damage >= max HP kills outright.
                if (overflow >= MaxHP) { Die(log, "is slain outright by massive damage"); return; }
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
            RegainResource("second_wind", 1);
            RegainResource("action_surge", 99);
            RegainResource("channel_divinity", 1);
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
        public bool EquipFromBag(int slot, out string message)
        {
            message = null;
            var stack = Bag.Slots[slot];
            if (stack == null) return false;
            var def = Content.ItemsDb.Get(stack.ItemId);
            string returned = null;
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
                        if (Shield) { message = "You already have a shield."; return false; }
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

        public bool CanWearArmor(ArmorDef a)
        {
            switch (a.Category)
            {
                case ArmorCategory.Light: return Class.LightArmor;
                case ArmorCategory.Medium: return Class.MediumArmor;
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
