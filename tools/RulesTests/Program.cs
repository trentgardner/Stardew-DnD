using System;
using System.Collections.Generic;
using DnD.Rules;
using DnD.Rules.Content;

namespace RulesTests
{
    /// <summary>Tiny dependency-free test runner for the rules engine.</summary>
    public static partial class Program
    {
        static int _passed, _failed;

        static void Check(bool cond, string name)
        {
            if (cond) _passed++;
            else { _failed++; Console.WriteLine("FAIL: " + name); }
        }

        static void Eq<T>(T actual, T expected, string name)
        {
            bool ok = Equals(actual, expected);
            if (ok) _passed++;
            else { _failed++; Console.WriteLine($"FAIL: {name} (expected {expected}, got {actual})"); }
        }

        public static int Main()
        {
            DiceTests();
            AbilityTests();
            CharacterTests();
            CheckTests();
            AttackTests();
            DeathSaveTests();
            InventoryTests();
            ItemTests();
            SpellTests();
            EncounterTests();
            ContentTests();
            LevelUpTests();

            Console.WriteLine($"\n{_passed} passed, {_failed} failed");
            return _failed == 0 ? 0 : 1;
        }

        static void DiceTests()
        {
            var e = DiceExpr.Parse("2d6+3");
            Eq(e.Count, 2, "parse count"); Eq(e.Sides, 6, "parse sides"); Eq(e.Bonus, 3, "parse bonus");
            Eq(DiceExpr.Parse("1d4-1").Bonus, -1, "parse negative");
            Eq(DiceExpr.Parse("d20").Count, 1, "parse implied count");
            Eq(e.ToString(), "2d6+3", "expr tostring");

            Dice.Rng = new ScriptedRandom(5, 17);
            var adv = Dice.RollD20(AdvantageState.Advantage);
            Eq(adv.Natural, 17, "advantage keeps higher");
            Dice.Rng = new ScriptedRandom(5, 17);
            var dis = Dice.RollD20(AdvantageState.Disadvantage);
            Eq(dis.Natural, 5, "disadvantage keeps lower");

            Dice.Rng = new ScriptedRandom(3, 4);
            var crit = Dice.RollDamage(DiceExpr.Parse("1d8"), true, new[] { new RollTerm("STR", 3) });
            Eq(crit.Dice.Count, 2, "crit doubles dice");
            Eq(crit.Total, 10, "crit total = dice + mod once");

            Dice.Rng = new ScriptedRandom(1, 2, 6);
            var gwf = Dice.RollDamage(DiceExpr.Parse("3d6"), false, null, 3);
            Eq(gwf.Total, 12, "great weapon fighting treats 1-2 as 3");

            Dice.Rng = new ScriptedRandom(6, 1, 5, 4);
            var four = Dice.Roll4d6DropLowest();
            Eq(Dice.SumKept(four), 15, "4d6 drop lowest");
        }

        static void AbilityTests()
        {
            Eq(AbilityUtil.Modifier(10), 0, "mod 10");
            Eq(AbilityUtil.Modifier(9), -1, "mod 9");
            Eq(AbilityUtil.Modifier(8), -1, "mod 8");
            Eq(AbilityUtil.Modifier(15), 2, "mod 15");
            Eq(AbilityUtil.Modifier(20), 5, "mod 20");
            Eq(AbilityUtil.Modifier(1), -5, "mod 1");
        }

        static PlayerCharacter Fighter()
        {
            Dice.Rng = new SystemRandom(1);
            return CharacterFactory.Create(CharacterFactory.DefaultChoices("fighter", "human", "Test Fighter"));
        }

        static void CharacterTests()
        {
            var f = Fighter();
            Eq(f.Score(Ability.STR), 17, "fighter STR 15 + 2 background");
            Eq(f.Score(Ability.CON), 15, "fighter CON 14 + 1 background");
            Eq(f.MaxHP, 12, "fighter L1 HP = 10 + CON 2");
            Eq(f.ArmorClass, 19, "chain mail 16 + shield 2 + defense style 1");
            Eq(f.ProficiencyBonus, 2, "PB L1");
            var atk = f.GetAttacks()[0];
            Eq(atk.ToHit, 5, "longsword to hit = STR 3 + PB 2");
            Eq(atk.Damage.ToString(), "1d8", "longsword one-handed with shield");
            Eq(atk.Mastery, WeaponMastery.Sap, "fighter has longsword mastery");
            Check(f.HeroicInspiration, "human starts with heroic inspiration");
            Check(f.SkillProficiencies.Contains(Skill.Athletics), "soldier gives athletics");

            f.FightingStyle = "dueling";
            Eq(f.ArmorClass, 18, "without defense style");
            Eq(f.GetAttacks()[0].DamageMod, 3 + 2, "dueling +2 damage one-handed");

            f.XP = 300;
            Check(f.CanLevelUp, "300 xp → level 2");
            f.LevelUp(new LevelUpPlan(), null);
            Eq(f.Level, 2, "now level 2");
            Eq(f.MaxHP, 12 + 6 + 2, "average HP gain d10 = 6 + CON");
            Eq(f.ResourceMax("action_surge"), 1, "action surge at L2");
            f.XP = 900; f.LevelUp(new LevelUpPlan { HpRoll = 7 }, null);
            Eq(f.MaxHP, 20 + 7 + 2, "rolled HP gain");
            Check(f.HasFeature("Improved Critical"), "champion at 3");
            Eq(f.GetAttacks()[0].CritOn, 19, "champion crits on 19");
            f.XP = 99999;
            Check(f.CanLevelUp, "level 4 is reachable (Act I cap)");
            f.LevelUp(f.AutoPlan(), null);
            Check(!f.CanLevelUp, "Act I level cap 4");

            Dice.Rng = new SystemRandom(2);
            var dwarf = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "dwarf", "Test Dwarf"));
            Eq(dwarf.MaxHP, 8 + 2 + 1, "dwarf cleric HP includes Dwarven Toughness");
            Eq(dwarf.Darkvision, 120, "2024 dwarf darkvision 120");
            Check(dwarf.Armor.Id == "chain_mail", "protector cleric gets heavy armor");

            var wiz = CharacterFactory.Create(CharacterFactory.DefaultChoices("wizard", "elf", "Test Wizard"));
            Eq(wiz.SpellSaveDC, 8 + 2 + 3, "wizard DC = 8 + PB + INT");
            Eq(wiz.SlotsMax(1), 2, "wizard L1 slots");
            Check(wiz.SkillProficiencies.Contains(Skill.Perception), "elf keen senses");
            Check(wiz.Cantrips.Contains("prestidigitation"), "sage magic initiate");
        }

        static void CheckTests()
        {
            var bard = CharacterFactory.Create(CharacterFactory.DefaultChoices("bard", "half_elf", "Test Bard"));
            Eq(bard.Score(Ability.CHA), 15, "bard CHA");
            Eq(bard.SkillBonus(Skill.Persuasion), 2 + 2, "persuasion = CHA 2 + PB 2");

            Dice.Rng = new ScriptedRandom(20);
            var r = Checks.RollSkill(bard, Skill.Persuasion, AdvantageState.Normal);
            Eq(Checks.ResolveCheck(r, 30), CheckOutcome.CriticalSuccess, "nat 20 house rule succeeds");
            Dice.Rng = new ScriptedRandom(1);
            r = Checks.RollSkill(bard, Skill.Persuasion, AdvantageState.Normal);
            Eq(Checks.ResolveCheck(r, 2), CheckOutcome.CriticalFailure, "nat 1 house rule fails");
            Dice.Rng = new ScriptedRandom(11);
            r = Checks.RollSkill(bard, Skill.Persuasion, AdvantageState.Normal);
            Eq(r.Total, 15, "11 + 4");
            Eq(Checks.ResolveCheck(r, 15), CheckOutcome.Success, "meets DC");

            bard.Level = 2;
            Eq(bard.SkillBonus(Skill.Arcana), 1 + 1, "jack of all trades adds half PB");

            bard.Exhaustion = 2;
            Dice.Rng = new ScriptedRandom(10);
            r = Checks.RollSkill(bard, Skill.Persuasion, AdvantageState.Normal);
            Eq(r.Total, 10 + 4 - 4, "exhaustion -2 per level");
        }

        static void AttackTests()
        {
            var f = Fighter();
            var kobold = new Monster(MonstersDb.Get("kobold"), false);
            var atk = f.GetAttacks()[0];
            var ctx = new AttackContext { DistanceFt = 5 };

            Dice.Rng = new ScriptedRandom(20);
            var res = AttackResolver.RollAttack(f, kobold, atk, ctx);
            Check(res.Hit && res.Critical, "nat 20 crits");

            Dice.Rng = new ScriptedRandom(1);
            res = AttackResolver.RollAttack(f, kobold, atk, ctx);
            Check(!res.Hit, "nat 1 misses");

            Dice.Rng = new ScriptedRandom(7);
            res = AttackResolver.RollAttack(f, kobold, atk, ctx);
            Check(res.Hit, "7 + 5 = 12 hits AC 12");

            Dice.Rng = new ScriptedRandom(10);
            res = AttackResolver.RollAttack(f, kobold, atk, new AttackContext { DistanceFt = 30 });
            Check(res.OutOfRange, "melee out of range");

            // Sap mastery applies disadvantage to the target's next attack
            var big = new Monster(MonstersDb.Get("snik"), false);
            Dice.Rng = new ScriptedRandom(15, 3);
            res = AttackResolver.RollAttack(f, big, atk, ctx);
            var dmg = AttackResolver.RollDamage(res);
            Eq(dmg.Total, 3 + 3, "damage = die + STR");
            AttackResolver.ApplyHit(res, dmg);
            Check(big.HasEffect("sap"), "sap applied");
            Dice.Rng = new ScriptedRandom(18, 4);
            var back = AttackResolver.RollAttack(big, f, big.GetAttacks()[0], ctx);
            Eq(back.Roll.Natural, 4, "sapped attacker rolls with disadvantage");
            Check(!big.HasEffect("sap"), "sap consumed");

            // Pack tactics
            Dice.Rng = new ScriptedRandom(3, 19);
            var pack = AttackResolver.RollAttack(kobold, f, kobold.GetAttacks()[0], new AttackContext { DistanceFt = 5, AllyAdjacentToTarget = true });
            Eq(pack.Roll.Natural, 19, "pack tactics advantage");

            // Ranged attack with an enemy adjacent → disadvantage
            var slinger = new Monster(MonstersDb.Get("kobold_slinger"), false);
            Dice.Rng = new ScriptedRandom(15, 2);
            var sling = AttackResolver.RollAttack(slinger, f, slinger.GetAttacks()[0], new AttackContext { DistanceFt = 5, EnemyAdjacentToAttacker = true });
            Eq(sling.Roll.Natural, 2, "ranged in melee has disadvantage");

            // Magic weapon extra damage
            f.MainHand = ItemsDb.Get<WeaponDef>("emberwrought_blade");
            var flame = f.GetAttacks()[0];
            Dice.Rng = new ScriptedRandom(15, 5, 2);
            res = AttackResolver.RollAttack(f, big, flame, ctx);
            dmg = AttackResolver.RollDamage(res);
            Check(dmg.Extra != null && dmg.Extra.Total == 2, "emberwrought adds 1d4 fire");
            int hpBefore = big.HP;
            AttackResolver.ApplyHit(res, dmg);
            Eq(hpBefore - big.HP, 8 + 2, "fire extra applied");

            var staff = ItemsDb.Get<WeaponDef>("starfall_staff");
            f.MainHand = staff; f.Shield = false;
            var sAtk = f.GetAttacks()[0];
            Eq(sAtk.ToHit, 5 + 1, "+1 staff to hit");
            Eq(sAtk.DamageMod, 3 + 1, "+1 staff damage");
            Eq(sAtk.Damage.ToString(), "1d8", "quarterstaff versatile without shield");
        }

        static void DeathSaveTests()
        {
            var f = Fighter();
            var log = new List<string>();
            f.TakeDamage(f.HP, DamageType.Slashing, false, log);
            Check(f.IsDown && !f.IsDead, "down at 0 but not dead");
            Check(f.Has(Condition.Unconscious), "unconscious at 0");

            Dice.Rng = new ScriptedRandom(15);
            f.ApplyDeathSave(f.RollDeathSave(), log);
            Eq(f.DeathSuccesses, 1, "success");
            Dice.Rng = new ScriptedRandom(1);
            f.ApplyDeathSave(f.RollDeathSave(), log);
            Eq(f.DeathFailures, 2, "nat 1 = two failures");
            f.TakeDamage(1, DamageType.Piercing, false, log);
            Check(f.IsDead, "third failure from damage = dead");

            f.Revive(0, log);
            Check(!f.IsDead && f.HP == 1, "ember of return revives at 1 HP");

            var g = Fighter();
            g.TakeDamage(g.HP, DamageType.Slashing, false, log);
            Dice.Rng = new ScriptedRandom(20);
            Eq(g.ApplyDeathSave(g.RollDeathSave(), log), PlayerCharacter.DeathSaveOutcome.Revived, "nat 20 death save revives");
            Eq(g.HP, 1, "with 1 HP");

            var h = Fighter();
            h.TakeDamage(h.HP + h.MaxHP, DamageType.Slashing, false, log);
            Check(h.IsDead, "massive damage kills");

            var k = Fighter();
            k.Wear(ItemsDb.Get<WondrousDef>("dawnmothers_charm"));
            Dice.Rng = new ScriptedRandom(3, 16);
            Eq(k.RollDeathSave().Natural, 16, "dawnmother's charm grants advantage");
        }

        static void InventoryTests()
        {
            var inv = new Inventory(6);
            Eq(inv.Add("potion_healing", 25), 0, "25 potions fit in 6 slots");
            Eq(inv.Slots[0].Count, 20, "stack caps at 20");
            Eq(inv.Slots[1].Count, 5, "overflow to next slot");
            Eq(inv.Count("potion_healing"), 25, "count");

            Eq(inv.Add("longsword", 4), 0, "weapons don't stack");
            Eq(inv.Slots[4].ItemId, "longsword", "weapon in own slot");
            Eq(inv.Add("longsword", 1), 1, "full inventory returns leftover");

            var half = inv.PickUpHalf(0);
            Eq(half.Count, 10, "pick up half");
            Eq(inv.Slots[0].Count, 10, "half remains");
            var leftover = inv.Place(1, half);
            Eq(inv.Slots[1].Count, 15, "merge into partial stack");
            Check(leftover == null, "nothing left over");

            inv.Move(1, 2);
            Eq(inv.Slots[2].ItemId, "potion_healing", "move different item swaps");
            Eq(inv.Slots[1].ItemId, "longsword", "swapped back");
            Eq(inv.Count("potion_healing"), 25, "move conserves items");

            Check(inv.Remove("potion_healing", 25), "remove all");
            Eq(inv.Count("potion_healing"), 0, "removed");

            var a = new Inventory(4); var b = new Inventory(4);
            a.Add("torch", 15);
            Eq(a.QuickTransfer(0, b), 15, "quick transfer between players");
            Eq(b.Count("torch"), 15, "other player has them");

            var c = new Inventory(4);
            c.Slots[0] = new ItemStack("torch", 5); c.Slots[2] = new ItemStack("torch", 7);
            c.Consolidate();
            Eq(c.Slots[0].Count, 12, "consolidate merges");
            Check(c.Slots[2] == null, "emptied slot");

            Check(c.Split(0), "split");
            Eq(c.Slots[0].Count + c.Slots[1].Count, 12, "split conserves");

            var d = new Inventory(16);
            d.Add("torch", 3); d.Add("dagger", 1); d.Add("potion_healing", 2);
            d.Slots[13] = new ItemStack("gemstone", 1);
            d.Sort();
            Eq(d.Count("gemstone") + d.Count("torch") + d.Count("dagger") + d.Count("potion_healing"), 7, "sort conserves");

            var held = c.PickUp(0, 0);
            var leftAfter = c.PlaceOne(3, held);
            Eq(c.Slots[3].Count, 1, "place one");
            Eq(leftAfter.Count, held.Count, "held keeps the rest");
        }

        static void ItemTests()
        {
            var f = Fighter();
            Eq(f.CarryCapacity, 17 * 15, "carry capacity STR x 15");
            float before = f.CarriedWeight;
            f.Copper += 100000;
            Eq(f.CarriedWeight, before, "coins are weightless");
            Check(!f.IsOverloaded, "starting gear not overloaded");
            for (int i = 0; i < 4; i++) f.AddItem("plate");
            Check(f.IsOverloaded, "4 plate armors overload");
            Eq(f.Speed, 5, "overloaded speed 5");

            var g = Fighter();
            int slot = Array.FindIndex(g.Bag.Slots, s => s == null);
            g.Bag.Slots[slot] = new ItemStack("greatsword", 1);
            string msg;
            Check(g.EquipFromBag(slot, out msg), "equip greatsword: " + msg);
            Eq(g.MainHand.Id, "greatsword", "now wielding greatsword");
            Check(!g.Shield, "shield stowed for two-handed");
            Eq(g.Bag.Slots[slot].ItemId, "longsword", "old weapon swapped into same slot");
            Eq(g.CountOf("shield"), 1, "shield in bag");

            var w = CharacterFactory.Create(CharacterFactory.DefaultChoices("wizard", "gnome", "W"));
            int s2 = Array.FindIndex(w.Bag.Slots, s => s == null);
            w.Bag.Slots[s2] = new ItemStack("plate", 1);
            Check(!w.EquipFromBag(s2, out msg), "wizard can't wear plate");

            int ac = g.ArmorClass;
            g.Wear(ItemsDb.Get<WondrousDef>("veilthread_cloak"));
            Eq(g.ArmorClass, ac + 1, "veilthread cloak +1 AC");
            int save = g.SaveBonus(Ability.WIS);
            g.Wear(ItemsDb.Get<WondrousDef>("oathbound_signet"));
            Eq(g.SaveBonus(Ability.WIS), save + 1, "signet +1 saves");
        }

        static void SpellTests()
        {
            var wiz = CharacterFactory.Create(CharacterFactory.DefaultChoices("wizard", "human", "W"));
            var mm = SpellsDb.Get("magic_missile");
            Check(SpellResolver.PaySlot(wiz, mm, 1), "pay slot");
            Eq(wiz.SlotsLeft(1), 1, "slot spent");
            Check(SpellResolver.PaySlot(wiz, mm, 1), "pay second");
            Check(!SpellResolver.PaySlot(wiz, mm, 1), "no slots left");

            var bh = SpellsDb.Get("burning_hands");
            Eq(SpellResolver.EffectDice(wiz, bh, 2).ToString(), "4d6", "upcast adds a die");

            var cleric = CharacterFactory.Create(CharacterFactory.DefaultChoices("cleric", "human", "C"));
            cleric.Level = 3;
            Dice.Rng = new ScriptedRandom(4, 4);
            var heal = SpellResolver.RollEffect(cleric, SpellsDb.Get("cure_wounds"), 1, false);
            Eq(heal.Total, 8 + 3 + 3, "cure wounds 2d8 + WIS + disciple of life (2+1)");

            var kobold = new Monster(MonstersDb.Get("kobold"), false);
            var sf = SpellsDb.Get("sacred_flame");
            Dice.Rng = new ScriptedRandom(6);
            var dmg = SpellResolver.RollEffect(cleric, sf, 0, false);
            var log = new List<string>();
            SpellResolver.ApplySaveDamage(cleric, kobold, sf, dmg, true, log);
            Eq(kobold.HP, kobold.MaxHP, "sacred flame no damage on save");

            wiz.Level = 3;
            var rof = SpellsDb.Get("fire_bolt");
            var k2 = new Monster(MonstersDb.Get("snik"), false);
            Dice.Rng = new ScriptedRandom(8);
            var fb = SpellResolver.RollEffect(wiz, rof, 0, false);
            Check(wiz.HasFeature("Potent Cantrip"), "evoker potent cantrip");
            int before = k2.HP;
            SpellResolver.ApplySaveDamage(wiz, k2, SpellsDb.Get("ray_of_frost"), fb, true, log);
            Eq(before - k2.HP, 4, "potent cantrip half on save");

            Eq(cleric.PreparedSpells.Contains("bless"), true, "life domain spells (from acolyte too)");
            Check(SpellsDb.ForClass("bard", 2).Count > 8, "bard spell list populated");
        }

        static void EncounterTests()
        {
            var f = Fighter();
            var k = new Monster(MonstersDb.Get("kobold"), false);
            var enc = new Encounter();
            Dice.Rng = new ScriptedRandom(10, 10, 5);
            enc.Add(f); enc.Add(k);
            enc.Sort();
            Check(enc.Order.Count == 2, "two combatants");
            enc.StartTurn(null);
            var first = enc.Current.Creature;
            enc.Advance();
            Check(enc.Current.Creature != first, "advance");
            enc.Advance();
            Eq(enc.Round, 2, "round 2");

            k.AddEffect(EffectLibrary.Create("sap", f));
            // sap lasts until the start of the fighter's next turn
            while (enc.Current.Creature != f) enc.Advance();
            enc.StartTurn(null);
            Check(!k.HasEffect("sap"), "sap expires at start of source's turn");

            var log = new List<string>();
            k.TakeDamage(100, DamageType.Slashing, false, log);
            Team winner;
            Check(enc.IsOver(out winner) && winner == Team.Players, "players win");
        }

        static void ContentTests()
        {
            Eq(CharacterDb.Classes.Count, 4, "4 playtest classes");
            Eq(CharacterDb.Species.Count, 5, "5 species");
            Eq(CharacterDb.Backgrounds.Count, 4, "4 backgrounds");
            foreach (var c in CharacterDb.Classes.Values)
            {
                if (c.StartingWeapon != null) Check(ItemsDb.Get(c.StartingWeapon) != null, "starting weapon exists: " + c.Id);
                foreach (var it in c.StartingItems) Check(ItemsDb.Get(it) != null, "starting item exists: " + it);
            }
            foreach (var s in SpellsDb.All.Values) Check(!string.IsNullOrEmpty(s.Description), "spell has description: " + s.Id);
            foreach (var it in ItemsDb.All.Values) Check(it.Weight >= 0, "weight set: " + it.Id);
            Check(ItemsDb.Get("plate").Weight == 65, "plate weighs 65");

            var rng = new Random(42);
            for (int i = 0; i < 20; i++)
            {
                var r = CharacterFactory.GenerateRecruit(rng);
                Check(r.Character.MaxHP > 0 && !string.IsNullOrEmpty(r.Pitch), "recruit generated");
            }
        }
    }
}
