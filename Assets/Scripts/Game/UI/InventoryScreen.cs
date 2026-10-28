using System.Collections.Generic;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Stardew-style inventory. Optional second panel: a chest, loot, home storage, or your co-op partner (trade).
    ///   Mouse:   drag, or click to pick up / place / swap / merge · right-click an item for its options menu
    ///            (use, equip, give a potion to an ally, move, split) · while holding: right-click places one ·
    ///            double-click use/equip · Shift+click quick-transfer · T sort · G give 1 gp (Shift: 10)
    ///   Gamepad: D-pad/stick move · A pick/place · X options (holding: place one) · R3+A quick-transfer · Y use/equip ·
    ///            View sort · LB/RB switch panel · B close
    /// Closing while holding a stack puts it back.
    /// </summary>
    public class InventoryScreen : GameScreen
    {
        readonly Inventory _otherInv;
        readonly string _otherTitle;
        PlayerSlot _partner;

        class Cell { public Inventory Inv; public int Index; public RectTransform Rt; public Image Icon, Bg; public Text Count; public string EquipSlot; }

        readonly List<Cell> _bagCells = new List<Cell>();
        readonly List<Cell> _otherCells = new List<Cell>();
        readonly List<Cell> _equipCells = new List<Cell>();
        int _panel; // 0 bag, 1 other, 2 equipment
        int _cursor;
        ItemStack _held;
        (int panel, int cell)? _dragFrom;   // where the held stack was picked up by a mouse press (for drag-and-drop)
        Vector2 _pressPos;
        float _lastClickTime = -1;
        (int panel, int cell) _lastClickCell = (-1, -1);
        Image _heldIcon;
        Text _heldCount, _info, _footer;

        public InventoryScreen(Inventory other, string otherTitle) { _otherInv = other; _otherTitle = otherTitle; }

        Inventory Bag => Slot.Character.Bag;

        public override void Build()
        {
            // Trade with your partner if they're standing nearby and nothing else is open.
            if (_otherInv == null)
            {
                foreach (var p in GameRoot.I.Players)
                    if (p != Slot && p.IsReady && p.Zone == Slot.Zone && Vector2.Distance(p.Avatar.transform.position, Slot.Avatar.transform.position) < 3f)
                        _partner = p;
            }
            var other = OtherInv;
            float h = other != null ? 345 : 265;
            Window(new Vector2(320, h), other != null ? null : "Inventory");

            float y = other != null ? -8 : -26;
            if (other != null)
            {
                var title = UIKit.Rect(Root, "OtherTitle", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, y - 16), new Vector2(-10, y));
                UIKit.Label(title, _partner != null ? $"Trade with {_partner.Character.Name}" : _otherTitle, 12, UIKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                y -= 18;
                MakeGrid(other, _otherCells, y, Mathf.Min(other.Size, 24));
                y -= Mathf.CeilToInt(Mathf.Min(other.Size, 24) / 12f) * 24 + 6;
            }
            var bagTitle = UIKit.Rect(Root, "BagTitle", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, y - 16), new Vector2(-10, y));
            UIKit.Label(bagTitle, "Backpack (top row = hotbar)", 11, UIKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            y -= 18;
            MakeGrid(Bag, _bagCells, y, Bag.Size);
            y -= 3 * 24 + 6;

            // Equipment row
            string[] eq = { "weapon", "armor", "shield", "Cloak", "Ring", "Amulet", "Trinket", "Focus" };
            for (int i = 0; i < eq.Length; i++)
            {
                var c = MakeCell(null, i, new Vector2(12 + i * 26, y), eq[i]);
                c.EquipSlot = eq[i];
                _equipCells.Add(c);
            }
            var eqLbl = UIKit.Rect(Root, "EqLbl", new Vector2(0, 1), new Vector2(1, 1), new Vector2(222, y - 22), new Vector2(-8, y));
            UIKit.Label(eqLbl, "Equipped", 10, UIKit.Dim, TextAnchor.MiddleLeft);
            y -= 28;

            var infoRt = UIKit.Rect(Root, "Info", new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 22), new Vector2(-10, y));
            _info = UIKit.Label(infoRt, "", 9, UIKit.Ink);
            var foot = UIKit.Rect(Root, "Footer", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 4), new Vector2(-8, 20));
            _footer = UIKit.Label(foot, "", 8, UIKit.Dim, TextAnchor.MiddleCenter);
            _footer.text = In.UsesMouse
                ? "Drag/click: move · <b>Right-click: options</b> · Double-click: use/equip · Shift+click: send · T: sort" + (_partner != null ? " · G: gold" : "")
                : "A: move · <b>X: options</b> · Y: use/equip · R3+A: send · View: sort · LB/RB: panel · B: close";

            var held = UIKit.Box(Root, "Held", new Vector2(0, 1), new Vector2(18, 18), Vector2.zero);
            _heldIcon = UIKit.Image(held, Color.white);
            _heldCount = UIKit.Label(held, "", 8, UIKit.Ink, TextAnchor.LowerRight);
            _heldIcon.enabled = false;

            _panel = 0;
            Redraw();
        }

        Inventory OtherInv => _otherInv ?? _partner?.Character.Bag;

        void MakeGrid(Inventory inv, List<Cell> into, float y, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int col = i % 12, row = i / 12;
                into.Add(MakeCell(inv, i, new Vector2(12 + col * 24.5f, y - row * 24), null));
            }
        }

        Cell MakeCell(Inventory inv, int index, Vector2 pos, string label)
        {
            var rt = UIKit.Box(Root, "Cell", new Vector2(0, 1), new Vector2(22, 22), pos);
            var c = new Cell { Inv = inv, Index = index, Rt = rt };
            c.Bg = UIKit.Image(rt, Color.white, ArtRegistry.Get("ui_slot"));
            c.Icon = UIKit.Image(UIKit.Fill(rt, "Icon", 2), Color.white);
            c.Count = UIKit.Label(rt, "", 8, UIKit.Ink, TextAnchor.LowerRight);
            if (label != null) { c.Count.fontSize = 6; c.Count.color = UIKit.Dim; c.Count.alignment = TextAnchor.MiddleCenter; } // empty equipment slots show their name
            return c;
        }

        List<Cell> Panel(int p) => p == 0 ? _bagCells : p == 1 ? _otherCells : _equipCells;

        string EquippedId(string slot)
        {
            var pc = Slot.Character;
            switch (slot)
            {
                case "weapon": return pc.MainHand?.Id;
                case "armor": return pc.Armor?.Id;
                case "shield": return pc.Shield ? "shield" : null;
                default: return pc.Worn.Find(w => w.Slot.ToString() == slot)?.Id;
            }
        }

        void Redraw()
        {
            void Draw(List<Cell> cells, int panel)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    var c = cells[i];
                    string id; int count;
                    if (c.EquipSlot != null) { id = EquippedId(c.EquipSlot); count = id != null ? 1 : 0; }
                    else { var st = c.Inv.Slots[c.Index]; id = st?.ItemId; count = st?.Count ?? 0; }
                    c.Icon.enabled = id != null;
                    if (id != null) c.Icon.sprite = ArtRegistry.Get(ItemsDb.Get(id)?.Icon ?? "missing");
                    if (c.EquipSlot == null) c.Count.text = count > 1 ? count.ToString() : "";
                    else c.Count.text = id == null ? SlotShort(c.EquipSlot) : "";
                    bool sel = panel == _panel && i == _cursor;
                    bool hot = panel == 0 && i < Inventory.HotbarSize;
                    c.Bg.color = sel ? new Color(1f, 0.8f, 0.35f) : hot ? new Color(0.95f, 0.92f, 0.8f) : (c.EquipSlot != null ? new Color(0.85f, 0.9f, 1f) : Color.white);
                }
            }
            Draw(_bagCells, 0);
            Draw(_otherCells, 1);
            Draw(_equipCells, 2);

            _heldIcon.enabled = _held != null;
            if (_held != null)
            {
                _heldIcon.sprite = ArtRegistry.Get(ItemsDb.Get(_held.ItemId)?.Icon ?? "missing");
                _heldCount.text = _held.Count > 1 ? _held.Count.ToString() : "";
            }
            else _heldCount.text = "";

            // Info about the hovered item
            var pc = Slot.Character;
            string hoveredId = HoveredId();
            var def = hoveredId != null ? ItemsDb.Get(hoveredId) : null;
            var hc = Current;
            string head = $"{Currency.Format(pc.Copper)}   Weight {pc.CarriedWeight:0.#}/{pc.CarryCapacity} lb" + (pc.IsOverloaded ? UIKit.Colorize("  OVERLOADED (Speed 5 ft)", UIKit.Bad) : "");
            if (_partner != null) head += $"\n{_partner.Character.Name}: {Currency.Format(_partner.Character.Copper)}";
            if (def != null)
            {
                string extra = "";
                if (def is WeaponDef w) extra = $"{w.Category} weapon · {w.Damage}{(w.Has(WeaponProperty.Versatile) ? $" ({w.VersatileDamage} two-handed)" : "")} {w.DamageType.ToString().ToLower()}{(w.MagicBonus > 0 ? $" · +{w.MagicBonus}" : "")}{(!w.ExtraDamage.IsNone ? $" · +{w.ExtraDamage} {w.ExtraDamageType.ToString().ToLower()}" : "")}\n{w.PropertyText()}\nMastery: {MasteryText.Describe(w.Mastery)}{(pc.MasteredWeapons.Contains(w.Id) ? "" : UIKit.Colorize(" (not mastered)", UIKit.Dim))}";
                else if (def is ArmorDef a) extra = $"{a.Category} armor · AC {a.BaseAC}{(a.MaxDex < 0 ? " + DEX" : a.MaxDex > 0 ? $" + DEX (max {a.MaxDex})" : "")}{(a.StrengthRequirement > 0 ? $" · STR {a.StrengthRequirement}" : "")}{(a.StealthDisadvantage ? " · Stealth Disadvantage" : "")}";
                else if (def is WondrousDef wd) extra = $"{wd.Rarity} wondrous item{(wd.RequiresAttunement ? " (requires attunement)" : "")}\n<i>{wd.Lore}</i>";
                string hint = hc != null && hc.EquipSlot != null ? $"equipped: {(In.UsesMouse ? "click" : "A")} to take off"
                    : _held == null ? $"{(In.UsesMouse ? "right-click" : "X")} for options" : "";
                head += $"\n\n<b>{def.Name}</b>  ({def.Weight:0.#} lb, {def.PriceText})" + (hint.Length > 0 ? UIKit.Colorize("  · " + hint, UIKit.Dim) : "") + $"\n{def.Description}\n{extra}";
            }
            else if (hc != null && hc.EquipSlot != null)
                head += $"\n\n<b>{SlotLong(hc.EquipSlot)} slot</b>: empty.\n" + UIKit.Colorize($"Equip something from your backpack: {(In.UsesMouse ? "double-click it, or right-click → Equip" : "select it and press Y")}.", UIKit.Dim);
            _info.text = head;
        }

        string HoveredId()
        {
            var cells = Panel(_panel);
            if (_cursor < 0 || _cursor >= cells.Count) return null;
            var c = cells[_cursor];
            if (c.EquipSlot != null) return EquippedId(c.EquipSlot);
            return c.Inv.Slots[c.Index]?.ItemId;
        }

        static string SlotShort(string slot) =>
            slot == "weapon" ? "Wpn" : slot == "armor" ? "Armor" : slot == "shield" ? "Shld" : slot == "Cloak" ? "Cloak" : slot == "Ring" ? "Ring" : slot == "Amulet" ? "Amul" : slot == "Trinket" ? "Trnk" : slot == "Focus" ? "Focus" : slot;

        static string SlotLong(string slot) => slot == "weapon" ? "Weapon" : slot == "armor" ? "Armor" : slot == "shield" ? "Shield" : slot;

        public override void Tick()
        {
            var cells = Panel(_panel);

            // Mouse hover / click / drag
            bool clicked = false, rightClicked = false, overCell = false;
            if (In.UsesMouse)
            {
                for (int p = 0; p < 3; p++)
                {
                    var list = Panel(p);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!UIKit.Contains(list[i].Rt, In.MouseScreen, Cam)) continue;
                        overCell = true;
                        if (_panel != p || _cursor != i) { _panel = p; _cursor = i; Redraw(); }
                        if (In.MouseLeft) clicked = true;
                        if (In.MouseRight) rightClicked = true;
                    }
                }
                // Drag-and-drop: a stack picked up by a press drops where the button is released (a click without moving keeps holding it).
                if (In.MouseLeftUp && _held != null && _dragFrom.HasValue)
                {
                    var from = _dragFrom.Value;
                    bool moved = (In.MouseScreen - _pressPos).sqrMagnitude > 64;
                    _dragFrom = null;
                    if (overCell && (_panel != from.panel || _cursor != from.cell)) { PrimaryAction(); return; }
                    if (!overCell && moved) { _panel = from.panel; _cursor = from.cell; PrimaryAction(); return; } // dropped outside: put it back
                }
                // Double-click: use / equip.
                if (clicked && Current != null && Current.EquipSlot == null)
                {
                    var here = (_panel, _cursor);
                    if (Time.unscaledTime - _lastClickTime < 0.35f && here == _lastClickCell)
                    {
                        _lastClickTime = -1;
                        _dragFrom = null;
                        if (_held != null) PrimaryAction(); // the first click picked it up: put it back first
                        UseOrEquip();
                        return;
                    }
                    _lastClickTime = Time.unscaledTime;
                    _lastClickCell = here;
                }
                if (_held != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, In.MouseScreen, Cam, out var local);
                    ((RectTransform)_heldIcon.transform).anchoredPosition = local + new Vector2(Root.rect.width / 2f + 6, -Root.rect.height / 2f - 6);
                }
            }
            else if (_held != null && _cursor < cells.Count)
            {
                ((RectTransform)_heldIcon.transform).anchoredPosition = cells[_cursor].Rt.anchoredPosition + new Vector2(10, -10);
            }

            // Keyboard / gamepad navigation
            var nav = In.Nav();
            if (nav != Vector2Int.zero && cells.Count > 0)
            {
                int cols = _panel == 2 ? cells.Count : 12;
                int next = _cursor + nav.x - nav.y * cols;
                if (next >= 0 && next < cells.Count) _cursor = next;
                else if (nav.y < 0 && _panel == 1) { _panel = 0; _cursor %= 12; }
                else if (nav.y < 0 && _panel == 0) { _panel = 2; _cursor = Mathf.Min(_cursor % 12, _equipCells.Count - 1); }
                else if (nav.y > 0 && _panel == 2) { _panel = 0; _cursor = 24 + Mathf.Min(_cursor, 11); }
                else if (nav.y > 0 && _panel == 0 && OtherInv != null) { _panel = 1; _cursor = Mathf.Min(_cursor, _otherCells.Count - 1); }
                Redraw();
            }
            if (!In.UsesMouse && (In.NextTab || In.PrevTab))
            {
                int dir = In.NextTab ? 1 : -1;
                do { _panel = (_panel + dir + 3) % 3; } while (Panel(_panel).Count == 0);
                _cursor = Mathf.Clamp(_cursor, 0, Panel(_panel).Count - 1);
                Redraw();
            }

            bool confirm = clicked || (!In.UsesMouse && In.Confirm) || (In.UsesMouse && In.Confirm);
            bool alt = rightClicked || (!In.UsesMouse && In.AltAction) || (In.UsesMouse && In.AltAction && !rightClicked);

            if (confirm && In.QuickModifier) QuickTransfer();
            else if (confirm)
            {
                bool wasEmpty = _held == null;
                PrimaryAction();
                if (clicked && wasEmpty && _held != null) { _dragFrom = (_panel, _cursor); _pressPos = In.MouseScreen; }
            }
            else if (alt && _held == null) OpenOptions();
            else if (alt) SecondaryAction();

            if (In.UseItem) UseOrEquip();
            if (In.Sort) { Bag.Sort(); Sfx.Play("ui"); Redraw(); }
            if (_partner != null && In.Give && _held == null)
            {
                int amt = In.QuickModifier ? 1000 : 100;
                if (Slot.Character.GiveCopper(_partner.Character, amt))
                {
                    Sfx.Play("coin");
                    _partner.Hud.Toast($"{Slot.Character.Name} gave you {Currency.Format(amt)}", UIKit.Gold);
                }
                Redraw();
            }

            // Esc/B closes; on keyboard I/Tab toggles too (on gamepad, Y is use/equip here).
            if (In.Cancel || (In.UsesMouse && In.Inventory)) Close();
        }

        Cell Current { get { var c = Panel(_panel); return _cursor >= 0 && _cursor < c.Count ? c[_cursor] : null; } }

        void PrimaryAction()
        {
            var c = Current;
            if (c == null) return;
            if (c.EquipSlot != null)
            {
                if (_held == null) { Slot.Character.Unequip(c.EquipSlot); Redraw(); }
                return;
            }
            if (_held == null) _held = c.Inv.PickUp(c.Index);
            else _held = c.Inv.Place(c.Index, _held);
            Sfx.Play("ui");
            Redraw();
        }

        /// <summary>Opens the options menu for a backpack slot, as if it were right-clicked (used by tests).</summary>
        public void OpenOptionsFor(int bagIndex) { _panel = 0; _cursor = bagIndex; OpenOptions(); }

        /// <summary>The item's options menu (right-click / X): everything you can do with it, spelled out.</summary>
        void OpenOptions()
        {
            var c = Current;
            if (c == null) return;
            if (c.EquipSlot != null) { if (EquippedId(c.EquipSlot) != null) { Slot.Character.Unequip(c.EquipSlot); Redraw(); } return; }
            var st = c.Inv.Slots[c.Index];
            if (st == null) return;
            var def = ItemsDb.Get(st.ItemId);
            if (def == null) return;
            var g = GameRoot.I;
            int index = c.Index;
            var inv = c.Inv;
            var opts = new List<(string, System.Action)>();
            bool mine = inv == Bag;
            if (mine)
            {
                bool gear = def.Kind == ItemKind.Weapon || def.Kind == ItemKind.Armor || def.Kind == ItemKind.Shield || def.Kind == ItemKind.Wondrous;
                var cons = def as ConsumableDef;
                if (gear) opts.Add(("Equip", () => { ItemUse.UseFromBag(Slot, index); Redraw(); }));
                else if (cons != null && cons.Effect == ConsumableEffect.Heal) opts.Add(("Drink it yourself", () => { ItemUse.UseFromBag(Slot, index); Redraw(); }));
                else if (cons != null) opts.Add(("Use", () => { ItemUse.UseFromBag(Slot, index); Redraw(); }));
                if (cons != null && cons.Effect == ConsumableEffect.Heal)
                {
                    foreach (var comp in g.CompanionsOf(Slot))
                        if (comp.Character != null)
                        {
                            var who = comp.Character;
                            opts.Add(($"Give to {who.Name} to drink ({who.HP}/{who.EffectiveMaxHP} HP)", () => { g.GivePotionTo(Slot, who, index); Redraw(); }));
                        }
                    foreach (var p in g.Players)
                        if (p != Slot && p.IsReady && p.Zone == Slot.Zone && Vector2.Distance(p.MapPos, Slot.MapPos) <= 3)
                        {
                            var who = p.Character;
                            opts.Add(($"Give to {who.Name} to drink ({who.HP}/{who.EffectiveMaxHP} HP)", () => { g.GivePotionTo(Slot, who, index); Redraw(); }));
                        }
                }
            }
            if (OtherInv != null)
                opts.Add((mine ? $"Send to {(_partner != null ? _partner.Character.Name : _otherTitle)}" : "Take", () => { _panel = mine ? 0 : 1; _cursor = index; QuickTransfer(); }));
            else if (mine)
                opts.Add((index < Inventory.HotbarSize ? "Move to backpack" : "Move to hotbar", () => { _panel = 0; _cursor = index; QuickTransfer(); }));
            if (st.Count > 1) opts.Add(("Split stack (pick up half)", () => { _held = inv.PickUpHalf(index); Redraw(); }));
            opts.Add(("Cancel", null));
            Hud.Push(new ChoiceScreen(def.Name + (st.Count > 1 ? $" ×{st.Count}" : ""), def.Description, opts));
        }

        void SecondaryAction()
        {
            var c = Current;
            if (c == null || c.EquipSlot != null) return;
            if (_held == null) _held = c.Inv.PickUpHalf(c.Index);
            else _held = c.Inv.PlaceOne(c.Index, _held);
            Redraw();
        }

        void QuickTransfer()
        {
            var c = Current;
            if (c == null || c.EquipSlot != null || _held != null) return;
            Inventory target;
            if (c.Inv == Bag) target = OtherInv;
            else target = Bag;
            if (target == null)
            {
                // No second panel: move between hotbar and backpack.
                var st = Bag.Slots[c.Index];
                if (st == null) return;
                int start = c.Index < Inventory.HotbarSize ? Inventory.HotbarSize : 0;
                int end = c.Index < Inventory.HotbarSize ? Bag.Size : Inventory.HotbarSize;
                for (int i = start; i < end; i++) if (Bag.Slots[i] == null) { Bag.Move(c.Index, i); break; }
            }
            else
            {
                string name = ItemsDb.Get(c.Inv.Slots[c.Index].ItemId)?.Name ?? "items";
                int moved = c.Inv.QuickTransfer(c.Index, target);
                if (moved > 0 && _partner != null && target == _partner.Character.Bag)
                    _partner.Hud.Toast($"{Slot.Character.Name} gave you {moved}× {name}", UIKit.Gold);
            }
            Sfx.Play("ui");
            Redraw();
        }

        void UseOrEquip()
        {
            var c = Current;
            if (c == null || _held != null) return;
            if (c.EquipSlot != null) { Slot.Character.Unequip(c.EquipSlot); Redraw(); return; }
            if (c.Inv != Bag) return;
            ItemUse.UseFromBag(Slot, c.Index);
            Redraw();
        }

        public override void OnClose()
        {
            if (_held != null && Slot.Character == null) { _held = null; return; }
            if (_held != null)
            {
                int left = Bag.ReturnHeld(_held);
                if (left > 0 && OtherInv != null) left = OtherInv.Add(_held.ItemId, left);
                if (left > 0) Slot.Hud.Toast($"No room: dropped {left}× {ItemsDb.Get(_held.ItemId)?.Name}", UIKit.Bad);
                _held = null;
            }
        }
    }

    /// <summary>Using items out of combat (in combat, the action bar handles costs).</summary>
    public static class ItemUse
    {
        public static void UseFromBag(PlayerSlot p, int index)
        {
            var pc = p.Character;
            var st = pc.Bag.Slots[index];
            if (st == null) return;
            var def = ItemsDb.Get(st.ItemId);
            if (def == null) return;

            if (def.Kind == ItemKind.Weapon || def.Kind == ItemKind.Armor || def.Kind == ItemKind.Shield || def.Kind == ItemKind.Wondrous)
            {
                if (pc.EquipFromBag(index, out var msg)) { p.Hud.Toast(msg); Sfx.Play("ui"); }
                else if (msg != null) p.Hud.Toast(msg, UIKit.Bad);
                return;
            }
            if (!(def is ConsumableDef c)) { p.Hud.Toast($"{def.Name}: {def.Description}"); return; }
            switch (c.Effect)
            {
                case ConsumableEffect.Heal:
                    if (pc.HP >= pc.EffectiveMaxHP) { p.Hud.Toast("You're already at full health."); return; }
                    pc.Bag.PickUp(index, 1);
                    var roll = Dice.RollDamage(c.Amount);
                    p.Hud.Dice.RollDamage($"{c.Name}", "You uncork it and drink.", roll, DieColor.Healing, () =>
                    {
                        int healed = pc.Heal(roll.Total);
                        p.Avatar.Popup("+" + healed, UIKit.HealGreen);
                        Sfx.Play("heal");
                    });
                    break;
                case ConsumableEffect.Light:
                    p.Avatar.LightTorch();
                    break;
                case ConsumableEffect.Ration:
                    p.Hud.Narrate("You chew a strip of dried meat thoughtfully. It tastes like determination and salt.", false);
                    break;
                case ConsumableEffect.Revive:
                case ConsumableEffect.SpellScroll:
                    GameRoot.I.TryRevive(p, st.ItemId);
                    break;
            }
        }
    }
}
