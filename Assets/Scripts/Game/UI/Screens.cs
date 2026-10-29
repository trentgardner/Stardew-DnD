using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    // ======================================================================== simple screens

    public class MessageScreen : GameScreen
    {
        readonly string _title, _text;
        readonly Action _onClose;
        public MessageScreen(string title, string text, Action onClose = null) { _title = title; _text = text; _onClose = onClose; }
        public override bool ShowCloseButton => false; // any key or click closes it

        public override void Build()
        {
            Window(new Vector2(340, 170), _title);
            var rt = UIKit.Rect(Root, "Text", Vector2.zero, Vector2.one, new Vector2(14, 26), new Vector2(-14, -30));
            UIKit.Label(rt, _text, 11, UIKit.Ink);
            var f = UIKit.Rect(Root, "Foot", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 20));
            UIKit.Label(f, In.UsesMouse ? "[E] / click to close" : "(A) to close", 9, UIKit.Dim, TextAnchor.MiddleCenter);
        }

        public override void Tick()
        {
            if (In.Confirm || In.Cancel || In.MouseLeft) { Close(); }
        }

        public override void OnClose() { if (!Hud.ClosingAll) _onClose?.Invoke(); }
    }

    public class ChoiceScreen : GameScreen
    {
        readonly string _title, _text;
        readonly List<(string label, Action act)> _opts;
        readonly bool _cancellable;
        MenuList _menu;

        public ChoiceScreen(string title, string text, List<(string, Action)> options, bool cancellable = true)
        { _title = title; _text = text; _opts = options; _cancellable = cancellable; }
        public override bool ShowCloseButton => _cancellable;

        public override void Build()
        {
            float h = 70 + _opts.Count * 18 + (string.IsNullOrEmpty(_text) ? 0 : 40);
            Window(new Vector2(300, h), _title);
            float top = -28;
            if (!string.IsNullOrEmpty(_text))
            {
                var t = UIKit.Rect(Root, "Text", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, top - 40), new Vector2(-12, top));
                UIKit.Label(t, _text, 10, UIKit.Ink);
                top -= 44;
            }
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, top));
            _menu = new MenuList(list) { RowHeight = 18, FontSize = 11 };
            foreach (var (label, act) in _opts) { var a = act; _menu.Add(label, () => { Close(); a?.Invoke(); }); }
            _menu.Build();
        }

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (_cancellable && In.Cancel) Close();
        }
    }

    // ======================================================================== shop

    public static class ShopDb
    {
        public static List<string> Stock(string shop)
        {
            switch (shop)
            {
                case "general":
                    return new List<string> {
                        "potion_healing", "torch", "rations", "dagger", "handaxe", "javelin", "mace", "quarterstaff", "spear", "shortbow", "light_crossbow",
                        "shortsword", "scimitar", "rapier", "longsword", "battleaxe", "warhammer", "flail", "greatsword", "greataxe", "maul", "longbow",
                        "padded", "leather", "studded_leather", "hide", "chain_shirt", "scale_mail", "breastplate", "chain_mail", "splint", "shield", "starfall_staff" };
                case "temple":
                    var t = new List<string> { "ember_of_return", "potion_healing", "potion_greater_healing", "scroll_revivify" };
                    if (GameRoot.I.World.Hearts("elowen") >= 3) t.Add("dawnmothers_charm");
                    return t;
                case "tavern":
                    return new List<string> { "rations", "torch", "potion_healing" };
                default: return new List<string>();
            }
        }

        public static string Owner(string shop) => shop == "general" ? "Brannoc" : shop == "temple" ? "Mother Elowen" : "Pip";

        /// <summary>Base price in copper with difficulty scaling for Embers of Return.</summary>
        public static int BasePrice(ItemDef def)
        {
            if (def.Id == "ember_of_return")
            {
                switch (GameRoot.I.Settings.Difficulty)
                {
                    case Difficulty.Story: return 2500;
                    case Difficulty.Normal: return 5000;
                    case Difficulty.Hard: return 15000;
                    default: return 30000;
                }
            }
            return def.PriceCp;
        }
    }

    public class ShopScreen : GameScreen
    {
        readonly string _shop;
        bool _selling;
        MenuList _menu;
        Text _info, _header;

        public ShopScreen(string shop) { _shop = shop; }

        int Mod => GameRoot.I.World.ShopModifier(Slot.Index, _shop);
        int BuyPrice(ItemDef d) => Mathf.Max(1, Mathf.RoundToInt(ShopDb.BasePrice(d) * (100 + Mod) / 100f));
        static int SellPrice(ItemDef d) => d.PriceCp / 2;

        public override void Build()
        {
            Window(new Vector2(380, 280), null);
            var h = UIKit.Rect(Root, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -40), new Vector2(-10, -4));
            _header = UIKit.Label(h, "", 12, UIKit.Ink, TextAnchor.MiddleLeft);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(8, 22), new Vector2(0, -44));
            _menu = new MenuList(list) { RowHeight = 15, FontSize = 10, VisibleRows = 14, Wrap = false };
            _menu.OnHover = e => ShowInfo();
            var info = UIKit.Rect(Root, "Info", new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(6, 22), new Vector2(-10, -44));
            _info = UIKit.Label(info, "", 9, UIKit.Ink);
            var f = UIKit.Rect(Root, "Foot", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 20));
            UIKit.Label(f, In.UsesMouse ? "Click/E: buy or sell · Q/R: switch Buy/Sell · Esc: leave" : "A: buy/sell · LB/RB: switch Buy/Sell · B: leave", 9, UIKit.Dim, TextAnchor.MiddleCenter);
            Rebuild();
        }

        List<string> _ids = new List<string>();

        void Rebuild()
        {
            var pc = Slot.Character;
            string mod = Mod == 0 ? "" : Mod < 0 ? UIKit.Colorize($"  ({-Mod}% off today!)", UIKit.Good) : UIKit.Colorize($"  (+{Mod}% today...)", UIKit.Bad);
            _header.text = $"<b>{ShopDb.Owner(_shop)}'s {( _selling ? "— SELL" : "— BUY")}</b>{mod}\nYour purse: {Currency.Format(pc.Copper)}";
            int keep = _menu.Index;
            _menu.Clear();
            _ids.Clear();
            if (!_selling)
            {
                foreach (var id in ShopDb.Stock(_shop))
                {
                    var d = ItemsDb.Get(id);
                    if (d == null) continue;
                    _ids.Add(id);
                    int price = BuyPrice(d);
                    _menu.Add($"{d.Name}  —  {Currency.Format(price)}", () => Buy(d), pc.Copper >= price);
                }
            }
            else
            {
                foreach (var st in pc.Bag.Slots)
                {
                    if (st == null || _ids.Contains(st.ItemId)) continue;
                    var d = ItemsDb.Get(st.ItemId);
                    if (d == null || d.PriceCp <= 0) continue;
                    _ids.Add(st.ItemId);
                    _menu.Add($"{d.Name} ×{pc.CountOf(d.Id)}  —  {Currency.Format(SellPrice(d))}", () => Sell(d));
                }
                if (_ids.Count == 0) _menu.Add("(Nothing to sell)", null, false);
            }
            _menu.Build();
            _menu.Index = Mathf.Clamp(keep, 0, Mathf.Max(0, _menu.Entries.Count - 1));
            _menu.Refresh();
            ShowInfo();
        }

        void ShowInfo()
        {
            if (_menu.Index < 0 || _menu.Index >= _ids.Count) { _info.text = ""; return; }
            var d = ItemsDb.Get(_ids[_menu.Index]);
            string extra = "";
            if (d is WeaponDef w) extra = $"\n{w.Category} · {w.Damage} {w.DamageType.ToString().ToLower()}\n{w.PropertyText()}\nMastery: {w.Mastery}";
            if (d is ArmorDef a) extra = $"\n{a.Category} armor · AC {a.BaseAC}{(a.MaxDex < 0 ? " + DEX" : a.MaxDex > 0 ? " + DEX (max 2)" : "")}{(a.StrengthRequirement > 0 ? $"\nNeeds STR {a.StrengthRequirement}" : "")}";
            if (d is WondrousDef wd) extra = $"\n<i>{wd.Lore}</i>";
            _info.text = $"<b>{d.Name}</b>\n{d.Description}{extra}\nWeight: {d.Weight:0.#} lb\nYou have: {Slot.Character.CountOf(d.Id)}";
        }

        void Buy(ItemDef d)
        {
            var pc = Slot.Character;
            int price = BuyPrice(d);
            if (pc.Copper < price) return;
            if (pc.Bag.SpaceFor(d.Id) < 1) { Hud.Toast("Your pack is full!", UIKit.Bad); return; }
            pc.Copper -= price;
            pc.AddItem(d.Id);
            Sfx.Play("coin");
            Hud.Toast($"Bought {d.Name}");
            Rebuild();
        }

        void Sell(ItemDef d)
        {
            var pc = Slot.Character;
            if (!pc.RemoveItem(d.Id)) return;
            pc.Copper += SellPrice(d);
            Sfx.Play("coin");
            Rebuild();
        }

        public override void Tick()
        {
            if (In.NextTab || In.PrevTab) { _selling = !_selling; _menu.Index = 0; Rebuild(); }
            _menu.Tick(In, Cam);
            if (In.Cancel) Close();
        }
    }

    // ======================================================================== rest

    public class RestScreen : GameScreen
    {
        readonly bool _bed;
        MenuList _menu;
        Text _text;
        public RestScreen(bool bed) { _bed = bed; }

        public override void Build()
        {
            Window(new Vector2(320, 200), "Rest");
            var t = UIKit.Rect(Root, "Text", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -80), new Vector2(-12, -28));
            _text = UIKit.Label(t, "", 10, UIKit.Ink);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -84));
            _menu = new MenuList(list) { RowHeight = 18, FontSize = 11 };
            Rebuild();
        }

        void Rebuild()
        {
            var pc = Slot.Character;
            var g = GameRoot.I;
            _text.text = $"HP {pc.HP}/{pc.EffectiveMaxHP} · Hit Dice {pc.HitDiceRemaining}/{pc.Level} (d{pc.Class.HitDie})" +
                         (pc.Exhaustion > 0 ? $"\nExhaustion {pc.Exhaustion}: a long rest with food removes 1 level." : "") +
                         (Slot.Zone.Def.Dark ? UIKit.Colorize("\nResting in a dungeon is dangerous. Someone will need to keep watch...", UIKit.Bad) : "");
            _menu.Clear();
            _menu.Add($"Short Rest (1 hour): spend Hit Dice", ShortRest, !g.EnemiesNear(Slot, 10));
            bool canLong = _bed || Slot.Zone.Def.SafeForLongRest || Slot.Zone.Def.Dark;
            _menu.Add(_bed || Slot.Zone.Def.SafeForLongRest ? "Long Rest (sleep until morning)" : "Long Rest (camp here, risky)", () => { Close(); g.BeginLongRest(Slot); }, canLong && !g.EnemiesNear(Slot, 10));
            _menu.Add("Never mind", Close);
            _menu.Build();
        }

        void ShortRest()
        {
            var pc = Slot.Character;
            var g = GameRoot.I;
            g.Clock.Advance(60);
            pc.ShortRest(null);
            if (pc.Class.Id == "wizard" && pc.SpendResource("arcane_recovery"))
            {
                int budget = (pc.Level + 1) / 2;
                for (int l = 2; l >= 1; l--) while (budget >= l && pc.SlotsUsed[l] > 0) { pc.SlotsUsed[l]--; budget -= l; }
                Hud.Toast("Arcane Recovery: you regain spell slots.", UIKit.UtilityBlue);
            }
            Hud.Narrate("You rest for an hour, bandaging wounds and catching your breath.", false);
            SpendDice();
        }

        void SpendDice()
        {
            var pc = Slot.Character;
            if (pc.HitDiceRemaining <= 0 || pc.HP >= pc.EffectiveMaxHP) { Rebuild(); return; }
            Hud.Push(new ChoiceScreen("Spend a Hit Die?", $"Roll d{pc.Class.HitDie} + CON to heal. ({pc.HitDiceRemaining} left)", new List<(string, Action)>
            {
                ("Roll a Hit Die", () =>
                {
                    int before = pc.HP;
                    pc.HitDiceSpent++;
                    var roll = Dice.RollDamage(new DiceExpr(1, pc.Class.HitDie), false, new[] { new RollTerm("CON", pc.Mod(Ability.CON)) });
                    Hud.Dice.RollDamage("Hit Die", "Short Rest healing", roll, DieColor.Healing, () =>
                    {
                        int h = pc.Heal(Mathf.Max(0, roll.Total));
                        Slot.Avatar.Popup("+" + h, UIKit.HealGreen);
                        SpendDice();
                    });
                }),
                ("Done resting", Rebuild),
            }));
        }

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (In.Cancel) Close();
        }
    }

    // ======================================================================== character sheet

    public class CharacterSheetScreen : GameScreen
    {
        Text _left, _right;
        MenuList _menu;

        public override void Build()
        {
            Window(new Vector2(480, 300), null);
            var l = UIKit.Rect(Root, "Left", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(12, 40), new Vector2(-4, -8));
            _left = UIKit.Label(l, "", 9, UIKit.Ink);
            var r = UIKit.Rect(Root, "Right", new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(4, 40), new Vector2(-12, -8));
            _right = UIKit.Label(r, "", 9, UIKit.Ink);
            var m = UIKit.Rect(Root, "Menu", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 4), new Vector2(-8, 38));
            _menu = new MenuList(m) { RowHeight = 16, FontSize = 10 };
            Rebuild();
        }

        void Rebuild()
        {
            var pc = Slot.Character;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<size=14><b>{pc.Name}</b></size>");
            sb.AppendLine($"Level {pc.Level} {pc.Species.Name} {pc.Class.Name}{(pc.Level >= 3 && pc.Class.Subclass != null ? $" ({pc.Class.Subclass.Name})" : "")}");
            sb.AppendLine($"{pc.Background.Name} · XP {pc.XP}/{(pc.Level < PlayerCharacter.XpThresholds.Length - 1 ? PlayerCharacter.XpThresholds[pc.Level + 1] : 0)}");
            sb.AppendLine($"HP {pc.HP}/{pc.EffectiveMaxHP} · AC {pc.ArmorClass} · Speed {pc.Speed} ft · Init {AbilityUtil.Fmt(pc.InitiativeBonus)}");
            sb.AppendLine($"Proficiency +{pc.ProficiencyBonus} · Passive Perception {pc.PassivePerception}");
            if (pc.IsCaster) sb.AppendLine($"Spell DC {pc.SpellSaveDC} · Spell attack {AbilityUtil.Fmt(pc.SpellAttackBonus)}");
            sb.AppendLine($"Carrying {pc.CarriedWeight:0.#}/{pc.CarryCapacity} lb");
            sb.AppendLine();
            foreach (var a in AbilityUtil.All)
            {
                bool save = Array.IndexOf(pc.Class.SavingThrows, a) >= 0;
                sb.AppendLine($"<b>{a}</b> {pc.Score(a),2} ({AbilityUtil.Fmt(pc.Mod(a))})   save {AbilityUtil.Fmt(pc.SaveBonus(a))}{(save ? " ●" : "")}");
            }
            sb.AppendLine();
            sb.AppendLine("<b>Weapon</b>: " + pc.GetAttacks()[0].Summary());
            _left.text = sb.ToString();

            var sk = new System.Text.StringBuilder("<b>Skills</b>  (● proficient, ◆ expertise)\n");
            foreach (var s in SkillUtil.All)
            {
                string mark = pc.Expertise.Contains(s) ? "◆" : pc.IsProficient(s) ? "●" : "  ";
                sk.AppendLine($"{mark} {SkillUtil.Name(s)} {AbilityUtil.Fmt(pc.SkillBonus(s))}");
            }
            sk.AppendLine("\n<b>Features</b>");
            foreach (var f in pc.Class.Features) if (f.Level <= pc.Level) sk.AppendLine($"· {f.Name}");
            if (pc.Level >= 3 && pc.Class.Subclass != null) foreach (var f in pc.Class.Subclass.Features) sk.AppendLine($"· {f.Name}");
            foreach (var t in pc.Species.Traits) sk.AppendLine($"· {t.Split(':')[0]}");
            sk.AppendLine($"· {pc.Background.OriginFeat}");
            _right.text = sk.ToString();

            _menu.Clear();
            if (pc.CanLevelUp) _menu.Add($"★ LEVEL UP to {pc.Level + 1}!", StartLevelUp);
            if (pc.ResourceMax("wild_shape") > 0 && !Slot.InCombat) _menu.Add(pc.WildShape != null ? $"Wild Shape: return to your own form" : $"Wild Shape ({pc.ResourceLeft("wild_shape")} left)…", OpenWildShape);
            _menu.Add("Close", Close);
            _menu.Build();
        }

        /// <summary>Out of combat: pick a beast to become (or turn back), e.g. a cat to squeeze through a gap.</summary>
        void OpenWildShape()
        {
            var pc = Slot.Character;
            if (pc.WildShape != null) { pc.EndWildShape(null); WildShapeFx.Transform(Slot.Avatar, pc); Close(); return; }
            var opts = new List<(string, Action)>();
            foreach (var form in pc.WildShapeForms())
            {
                var def = MonstersDb.Get(form);
                string f = form;
                opts.Add(($"{def.Name}: AC {def.AC}, speed {def.Speed} ft. {def.Description}", () =>
                {
                    if (pc.StartWildShape(f, null)) { WildShapeFx.Transform(Slot.Avatar, pc); Close(); }
                    else Hud.Toast("No Wild Shape uses left. (One comes back on a short rest.)", UIKit.Bad);
                }));
            }
            opts.Add(("Never mind", () => { }));
            Hud.Push(new ChoiceScreen("Wild Shape", $"Become a beast you know. You keep your mind and HP, gain {pc.Level} temporary HP, and can't cast spells until you change back.", opts, false));
        }

        /// <summary>Opens the level-up screen (the player's own choices); the sheet refreshes afterwards.</summary>
        public void StartLevelUp() => Hud.Push(new LevelUpScreen(Rebuild));

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (In.Cancel || In.CharacterSheet) Close();
        }
    }

    // ======================================================================== spellbook

    public class SpellbookScreen : GameScreen
    {
        MenuList _menu;
        Text _info;
        bool _prepareMode;
        List<SpellDef> _shown = new List<SpellDef>();

        public override void Build()
        {
            Window(new Vector2(420, 270), null);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(8, 22), new Vector2(0, -8));
            _menu = new MenuList(list) { RowHeight = 15, FontSize = 10, VisibleRows = 15, Wrap = false };
            _menu.OnHover = e => ShowInfo();
            var info = UIKit.Rect(Root, "Info", new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(6, 22), new Vector2(-10, -8));
            _info = UIKit.Label(info, "", 9, UIKit.Ink);
            var f = UIKit.Rect(Root, "Foot", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 20));
            UIKit.Label(f, In.UsesMouse ? "E/click: cast (out of combat) · Q/R: toggle Prepare mode · Esc: close" : "A: cast · LB/RB: Prepare mode · B: close", 9, UIKit.Dim, TextAnchor.MiddleCenter);
            Rebuild();
        }

        void Rebuild()
        {
            var pc = Slot.Character;
            _menu.Clear();
            _shown.Clear();
            if (!pc.IsCaster)
            {
                _menu.Add("You don't cast spells. (You hit things. It's great.)", null, false);
                _menu.Build();
                _info.text = pc.Class.Id == "fighter" ? $"Second Wind: {pc.ResourceLeft("second_wind")}/{pc.ResourceMax("second_wind")}\nAction Surge: {pc.ResourceLeft("action_surge")}/{pc.ResourceMax("action_surge")}" : "";
                return;
            }
            int maxLevel = pc.Level >= 3 ? 2 : 1;
            if (_prepareMode)
            {
                var always = pc.Level >= 3 && pc.Class.Subclass != null ? pc.Class.Subclass.AlwaysPreparedSpells : new List<string>();
                int count = pc.PreparedSpells.Count(x => !always.Contains(x));
                int max = pc.Class.PreparedSpells[pc.Level];
                IEnumerable<SpellDef> pool = pc.Class.Id == "wizard" ? pc.Spellbook.Select(SpellsDb.Get).Where(s => s != null) : SpellsDb.ForClass(pc.Class.Id, maxLevel);
                _menu.Add($"— PREPARE SPELLS ({count}/{max}) —", null, false);
                _shown.Add(null);
                foreach (var s in pool.Where(s => !s.IsCantrip && s.Level <= maxLevel).OrderBy(s => s.Level))
                {
                    bool prepared = pc.PreparedSpells.Contains(s.Id);
                    bool locked = always.Contains(s.Id);
                    var sp = s;
                    _shown.Add(s);
                    _menu.Add($"{(prepared ? "■" : "□")} {s.Name} (L{s.Level}){(locked ? " [domain]" : "")}", () =>
                    {
                        if (locked) return;
                        if (prepared) pc.PreparedSpells.Remove(sp.Id);
                        else if (count < max) pc.PreparedSpells.Add(sp.Id);
                        Rebuild();
                    }, !locked);
                }
            }
            else
            {
                _menu.Add($"Slots: L1 {pc.SlotsLeft(1)}/{pc.SlotsMax(1)}{(pc.SlotsMax(2) > 0 ? $" · L2 {pc.SlotsLeft(2)}/{pc.SlotsMax(2)}" : "")}", null, false);
                _shown.Add(null);
                foreach (var id in pc.Cantrips.Concat(pc.PreparedSpells).Distinct())
                {
                    var s = SpellsDb.Get(id);
                    if (s == null) continue;
                    _shown.Add(s);
                    bool castable = OutOfCombatCastable(s) && !Slot.InCombat && (s.IsCantrip || pc.LowestAvailableSlot(s.Level) > 0);
                    _menu.Add($"{s.Name} ({(s.IsCantrip ? "cantrip" : "L" + s.Level)})", () => Cast(s), castable);
                }
            }
            _menu.Build();
            if (_menu.Entries.Count > 1) { _menu.Index = 1; _menu.Refresh(); }
            ShowInfo();
        }

        static bool OutOfCombatCastable(SpellDef s) =>
            s.Kind == SpellKind.Heal || s.Id == "light" || s.Id == "guidance" || s.Id == "mage_armor" || s.Id == "aid" || s.Id == "lesser_restoration" || s.Id == "detect_magic";

        void ShowInfo()
        {
            int i = _menu.Index;
            if (i < 0 || i >= _shown.Count || _shown[i] == null) { _info.text = ""; return; }
            var s = _shown[i];
            string kind = s.IsCantrip ? $"{s.School} cantrip" : $"Level {s.Level} {s.School}";
            _info.text = $"<b>{s.Name}</b>\n{kind}{(s.Concentration ? " · Concentration" : "")}{(s.Ritual ? " · Ritual" : "")}\n" +
                         $"{s.CastTime} · Range {(s.RangeFt == 0 ? "Self" : s.RangeFt == 5 ? "Touch" : s.RangeFt + " ft")}\n\n{s.Description}";
        }

        void Cast(SpellDef s)
        {
            var pc = Slot.Character;
            int slot = s.IsCantrip ? 0 : pc.LowestAvailableSlot(s.Level);
            if (!s.IsCantrip && slot < 0) return;
            if (s.Id == "light") { Slot.Avatar.LightSpellMinutes = 60; Hud.Toast("Your staff glows with soft light (1 hour).", UIKit.UtilityBlue); Sfx.Play("spell"); return; }
            if (s.Id == "detect_magic") { Hud.Narrate("You close your eyes and feel for magic. Faint warmth pulses from somewhere deep beneath the mountains...", false); return; }
            SpellResolver.PaySlot(pc, s, slot);
            if (s.Kind == SpellKind.Heal)
            {
                var target = GameRoot.I.LowestHpAlly(Slot);
                var roll = SpellResolver.RollEffect(pc, s, slot, false);
                Hud.Dice.RollDamage(s.Name, $"Healing {target.Name}", roll, DieColor.Healing, () =>
                {
                    int h = target.Heal(roll.Total);
                    Hud.Toast($"{target.Name} regains {h} HP", UIKit.HealGreen);
                    Sfx.Play("heal");
                    Rebuild();
                });
                return;
            }
            var fx = EffectLibrary.Create(s.EffectId ?? s.Id, pc);
            if (s.Id == "aid") { fx.RoundsLeft = -1; pc.AddEffect(fx); pc.Heal(5); }
            else if (s.Id == "mage_armor") { fx.RoundsLeft = -1; pc.AddEffect(fx); }
            else if (s.Id == "lesser_restoration") pc.Effects.RemoveAll(e => e.Condition == Condition.Poisoned || e.Condition == Condition.Blinded || e.Condition == Condition.Paralyzed);
            else pc.AddEffect(fx);
            Hud.Toast($"You cast {s.Name}.", UIKit.UtilityBlue);
            Sfx.Play("spell");
            Rebuild();
        }

        public override void Tick()
        {
            if (In.NextTab || In.PrevTab) { _prepareMode = !_prepareMode; Rebuild(); }
            _menu.Tick(In, Cam);
            if (In.Cancel || In.Spells) Close();
        }
    }

    // ======================================================================== journal

    public class JournalScreen : GameScreen
    {
        public override void Build()
        {
            Window(new Vector2(420, 270), "Journal");
            var g = GameRoot.I;
            var sb = new System.Text.StringBuilder("<b>Quests</b>\n");
            bool any = false;
            var done = new List<string>();
            foreach (var q in QuestDb.All.Values)
            {
                int st = g.Quests.Stage(q.Id);
                if (st == 0) continue;
                any = true;
                if (g.Quests.Done(q.Id)) { done.Add(q.Title); continue; }
                string giver = q.Giver != null && WorldDb.Npcs.TryGetValue(q.Giver, out var n) ? $" <i>({n.Name})</i>" : "";
                sb.AppendLine($"• <b>{q.Title}</b>{giver}: {q.StageText[Mathf.Min(st, q.Last)]}");
            }
            if (done.Count > 0) sb.AppendLine($"✓ <i>Completed:</i> {string.Join(", ", done)}");
            if (!any) sb.AppendLine("Nothing yet. Maybe talk to people? Captain Vale looked like she needed help.");
            sb.AppendLine("\n<b>Friends in Thistledown</b>");
            foreach (var n in WorldDb.Npcs.Values)
            {
                int h = g.World.Hearts(n.Id);
                if (!g.World.Friendship.ContainsKey(n.Id)) continue;
                sb.AppendLine($"{n.Name} — {new string('♥', h)}{new string('♡', Mathf.Max(0, 5 - h))}");
            }
            if (g.World.EggHatched) sb.AppendLine($"\n{g.World.DragonName} the dragonling — ♥♥♥♥♥ (obviously)");
            var rt = UIKit.Rect(Root, "Text", Vector2.zero, Vector2.one, new Vector2(14, 22), new Vector2(-14, -28));
            UIKit.Label(rt, sb.ToString(), 10, UIKit.Ink);
        }

        public override void Tick() { if (In.Cancel || In.Journal || In.Confirm) Close(); }
    }

    // ======================================================================== pause / saves / settings

    public class PauseScreen : GameScreen
    {
        MenuList _menu;
        Text _info;
        enum Page { Main, Load, Settings, Controls }
        Page _page;

        /// <summary>Is there a menu row starting with this text on the current page?</summary>
        public bool HasOption(string prefix) => _menu != null && _menu.Entries.Exists(e => e.Text.StartsWith(prefix));

        public override void Build()
        {
            Window(new Vector2(360, 280), "Paused");
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 60), new Vector2(-10, -28));
            _menu = new MenuList(list) { RowHeight = 17, FontSize = 11, VisibleRows = 11, Wrap = false };
            var info = UIKit.Rect(Root, "Info", new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 6), new Vector2(-12, 58));
            _info = UIKit.Label(info, "", 9, UIKit.Dim);
            Show(Page.Main);
        }

        void Show(Page p)
        {
            _page = p;
            var g = GameRoot.I;
            _menu.Clear();
            _info.text = "";
            switch (p)
            {
                case Page.Main:
                    bool hardcore = g.Settings.Difficulty == Difficulty.Hardcore;
                    _menu.Add("Resume", Close);
                    _menu.Add(hardcore ? "Save (Hardcore: only by sleeping)" : "Save Game (new save in this playthrough)", () =>
                    {
                        string file = SaveSystem.Save(false);
                        Hud.Toast(file != null ? "Game saved." : "Can't save right now.", file != null ? UIKit.Good : UIKit.Bad);
                        Close();
                    }, !hardcore && !g.AnyInCombat);
                    _menu.Add("Load Game", () => Show(Page.Load));
                    _menu.Add("Settings", () => Show(Page.Settings));
                    _menu.Add("Controls", () => Show(Page.Controls));
                    foreach (var other in g.Players)
                        if (other != Hud.Slot && other.IsReady && other.Disconnected)
                        {
                            var gone = other;
                            _menu.Add($"Send {gone.Character.Name} home (their controller is lost)", () => { Close(); g.DropOut(gone); }, !gone.InCombat);
                        }
                    if (Hud.Slot.Index > 0)
                        _menu.Add(Hud.Slot.InCombat ? "Leave the game (not during a fight)" : "Leave the game (your character waits for you)",
                            () => { var me = Hud.Slot; Close(); g.DropOut(me); }, !Hud.Slot.InCombat);
                    _menu.Add("Quit to Title (unsaved progress is lost)", () => { Close(); g.QuitToTitle(); });
                    _info.text = $"Playthrough: {g.PlaythroughName} · {g.Settings.Difficulty}\nDay {g.Clock.Day}, {g.Clock.TimeText()}";
                    break;
                case Page.Load:
                    foreach (var s in SaveSystem.List())
                    {
                        var info = s;
                        _menu.Add($"{s.PlaythroughName}: {s.Summary}", () => { Close(); g.LoadGame(info.Path); });
                    }
                    if (_menu.Entries.Count == 0) _menu.Add("(No saves yet)", null, false);
                    _menu.Add("Back", () => Show(Page.Main));
                    break;
                case Page.Settings:
                    _menu.Add($"Day length: {g.Settings.DayLengthMinutes} real minutes", () =>
                    {
                        var opts = Settings.DayLengthOptions;
                        int i = Array.IndexOf(opts, g.Settings.DayLengthMinutes);
                        g.Settings.DayLengthMinutes = opts[(i + 1) % opts.Length];
                        Show(Page.Settings);
                    });
                    _menu.Add($"Narrator: {(g.Settings.NarratorKeyMomentsOnly ? "Key moments only" : "Full DM narration")}", () =>
                    {
                        g.Settings.NarratorKeyMomentsOnly = !g.Settings.NarratorKeyMomentsOnly;
                        Show(Page.Settings);
                    });
                    _menu.Add($"Sound volume: {Mathf.RoundToInt(Sfx.Volume * 100)}%", () => { Sfx.Volume = Sfx.Volume >= 1f ? 0f : Sfx.Volume + 0.2f; Show(Page.Settings); });
                    _menu.Add($"Music volume: {Mathf.RoundToInt(Music.Volume * 100)}%", () => { Music.Volume = Music.Volume >= 0.99f ? 0f : Mathf.Min(1f, Music.Volume + 0.2f); Show(Page.Settings); });
                    _menu.Add($"Difficulty: {g.Settings.Difficulty} (set when the playthrough began)", null, false);
                    _menu.Add("Back", () => Show(Page.Main));
                    break;
                case Page.Controls:
                    _menu.Add("Back", () => Show(Page.Main));
                    _info.text = "";
                    var c = UIKit.Rect(Root, "ControlsText", new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 60), new Vector2(-14, -50));
                    UIKit.Label(c, In.UsesMouse
                        ? "WASD move · E/click interact · I inventory · C character · K spells · J journal · Esc menu\nAny window: Esc, its ✕ button, or its own key again closes it.\n1-0 hotbar · Ctrl sneak (creep up unseen, then E to AMBUSH) · Alt highlight\nInventory: drag items · right-click for options (use, equip, give a potion to an ally) · double-click to use\nCombat: click a tile to move, an enemy to attack · 1-9 actions · Space end turn · Esc cancel\nCompanions: you pick their moves on their turn (or press Auto). Talk to them to change it."
                        : "Left stick move · A interact · Y inventory · View character · RT spells · LT journal · Menu pause\nB closes any window.\nLB/RB hotbar · X use item · L3 sneak (creep up unseen, then A to AMBUSH)\nInventory: A move · X options (use, equip, give a potion to an ally) · Y use/equip\nCombat: stick moves cursor · A confirm · B cancel · LB/RB actions · X end turn\nCompanions: you pick their moves on their turn (or Auto). Talk to them to change it.", 8, UIKit.Ink).name = "ControlsText";
                    break;
            }
            _menu.Build();
        }

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (In.Cancel) { if (_page == Page.Main) Close(); else { var t = Root.Find("ControlsText"); if (t) UnityEngine.Object.Destroy(t.gameObject); Show(Page.Main); } }
        }
    }

    // ======================================================================== tavern recruits

    public class RecruitAgent : Interactable
    {
        public CharacterFactory.Recruit Data;
        public Actor Actor;
        public override float Range => 1.6f;
        public override string PromptText(PlayerSlot p) => $"Approach the {Data.Character.Species.Name.ToLower()} {Data.Character.Class.Name.ToLower()}";
        public override void Interact(PlayerSlot p) { Actor.Face(p.Avatar.transform.position); p.Hud.Push(new DialogueScreen("recruit", null, null, this)); }
    }
}
