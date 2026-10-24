using System.Collections.Generic;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Stardew-style inventory. Optional second panel: a chest, loot, home storage, or your co-op partner (trade).
    ///   Mouse:   left-click pick up / place / swap / merge · right-click pick half / place one ·
    ///            Shift+click quick-transfer · U or middle-click use/equip · T sort · G give 1 gp (Shift: 10)
    ///   Gamepad: D-pad/stick move · A pick/place · X half/one · R3+A quick-transfer · Y use/equip ·
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
            float h = other != null ? 330 : 250;
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
                ? "Click: move · Right-click: split · Shift+click: send · U: use/equip · T: sort" + (_partner != null ? " · G: give gold" : "") + " · Esc: close"
                : "A: move · X: split · R3+A: send · Y: use/equip · View: sort · LB/RB: panel · B: close";

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
            if (label != null) { c.Count.text = ""; c.Count.alignment = TextAnchor.UpperLeft; }
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
            string head = $"{Currency.Format(pc.Copper)}   Weight {pc.CarriedWeight:0.#}/{pc.CarryCapacity} lb" + (pc.IsOverloaded ? UIKit.Colorize("  OVERLOADED (Speed 5 ft)", UIKit.Bad) : "");
            if (_partner != null) head += $"\n{_partner.Character.Name}: {Currency.Format(_partner.Character.Copper)}";
            if (def != null)
            {
                string extra = "";
                if (def is WeaponDef w) extra = $"{w.Category} weapon · {w.Damage}{(w.Has(WeaponProperty.Versatile) ? $" ({w.VersatileDamage} two-handed)" : "")} {w.DamageType.ToString().ToLower()}{(w.MagicBonus > 0 ? $" · +{w.MagicBonus}" : "")}{(!w.ExtraDamage.IsNone ? $" · +{w.ExtraDamage} {w.ExtraDamageType.ToString().ToLower()}" : "")}\n{w.PropertyText()}\nMastery: {MasteryText.Describe(w.Mastery)}{(pc.MasteredWeapons.Contains(w.Id) ? "" : UIKit.Colorize(" (not mastered)", UIKit.Dim))}";
                else if (def is ArmorDef a) extra = $"{a.Category} armor · AC {a.BaseAC}{(a.MaxDex < 0 ? " + DEX" : a.MaxDex > 0 ? $" + DEX (max {a.MaxDex})" : "")}{(a.StrengthRequirement > 0 ? $" · STR {a.StrengthRequirement}" : "")}{(a.StealthDisadvantage ? " · Stealth Disadvantage" : "")}";
                else if (def is WondrousDef wd) extra = $"{wd.Rarity} wondrous item{(wd.RequiresAttunement ? " (requires attunement)" : "")}\n<i>{wd.Lore}</i>";
                head += $"\n\n<b>{def.Name}</b>  ({def.Weight:0.#} lb, {def.PriceText})\n{def.Description}\n{extra}";
            }
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

        public override void Tick()
        {
            var cells = Panel(_panel);

            // Mouse hover / click
            bool clicked = false, rightClicked = false;
            if (In.UsesMouse)
            {
                for (int p = 0; p < 3; p++)
                {
                    var list = Panel(p);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!UIKit.Contains(list[i].Rt, In.MouseScreen, Cam)) continue;
                        if (_panel != p || _cursor != i) { _panel = p; _cursor = i; Redraw(); }
                        if (In.MouseLeft) clicked = true;
                        if (In.MouseRight) rightClicked = true;
                    }
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
            else if (confirm) PrimaryAction();
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
