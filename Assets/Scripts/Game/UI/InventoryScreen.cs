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

        /// <summary>The co-op partner standing next to you (trade panel), if any.</summary>
        public PlayerSlot Partner => _partner;

        /// <summary>Sends the whole stack in one of your bag slots to your trade partner.</summary>
        public void SendToPartner(int bagIndex)
        {
            if (_partner == null || bagIndex < 0 || Bag.Slots[bagIndex] == null) return;
            string name = ItemsDb.Get(Bag.Slots[bagIndex].ItemId)?.Name ?? "items";
            int moved = Bag.QuickTransfer(bagIndex, _partner.Character.Bag);
            if (moved > 0) _partner.Hud.Toast($"{Slot.Character.Name} gave you {moved}× {name}", UIKit.Gold);
        }

        /// <summary>Moves a whole backpack stack into someone else's bag; whatever doesn't fit stays with you (and you're told).</summary>
        public void SendTo(int bagIndex, Inventory target, string name)
        {
            var st = Bag.Slots[bagIndex];
            if (st == null || target == null) return;
            int total = st.Count;
            int moved = Bag.QuickTransfer(bagIndex, target);
            if (moved < total) Hud.Toast($"{name}'s bag is full ({total - moved} didn't fit).", UIKit.Bad);
            else Hud.Toast($"Gave {name} {moved}× {ItemsDb.Get(st.ItemId)?.Name}.", UIKit.UtilityBlue);
            Sfx.Play("ui");
        }

        /// <summary>Hands coins to your trade partner (coins weigh nothing).</summary>
        public void GiveGold(int copper)
        {
            if (_partner == null || !Slot.Character.GiveCopper(_partner.Character, copper)) return;
            Sfx.Play("coin");
            _partner.Hud.Toast($"{Slot.Character.Name} gave you {Currency.Format(copper)}", UIKit.Gold);
        }

        Inventory Bag => Slot.Character.Bag;

        public override void Build()
        {
            if (_tabs == null) BuildTabs();
            var other = OtherInv;
            int otherCount = other != null ? Mathf.Min(other.Size, 36) : 0, otherRows = Mathf.CeilToInt(otherCount / 12f);
            float h = other != null ? 265 + 24 + otherRows * 24 : 265;
            Window(new Vector2(320, h), other != null ? null : "Inventory");

            float y = other != null ? -8 : -26;
            if (other != null)
            {
                // Tabs: one per bag you can reach (click, or LT/RT on a controller). A single bag just gets a title.
                float x = 10;
                for (int i = 0; i < _tabs.Count; i++)
                {
                    var t = _tabs[i];
                    string label = t.Partner != null ? $"Trade: {t.Label}" : t.Label;
                    float w = Mathf.Clamp(label.Length * 7 + 14, 50, 120);
                    var rt = UIKit.Rect(Root, "Tab" + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, y - 18), new Vector2(x + w, y));
                    if (_tabs.Count > 1) UIKit.Image(rt, i == _tab ? new Color(1f, 0.85f, 0.45f) : new Color(0.85f, 0.8f, 0.7f, 0.6f));
                    var lbl = UIKit.Label(rt, label, i == _tab ? 11 : 10, UIKit.Ink, _tabs.Count > 1 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
                    if (i == _tab) lbl.fontStyle = FontStyle.Bold;
                    _tabRts.Add(rt);
                    x += w + 4;
                }
                y -= 22;
                MakeGrid(other, _otherCells, y, otherCount);
                y -= otherRows * 24 + 6;
            }
            var bagTitle = UIKit.Rect(Root, "BagTitle", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, y - 16), new Vector2(-10, y));
            UIKit.Label(bagTitle, "Backpack (top row = hotbar)", 11, UIKit.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            y -= 18;
            MakeGrid(Bag, _bagCells, y, Bag.Size);
            y -= 3 * 24 + 6;

            // Equipment row
            string[] eq = { "weapon", "offset", "armor", "shield", "Cloak", "Ring", "Amulet", "Trinket", "Focus" };
            for (int i = 0; i < eq.Length; i++)
            {
                var c = MakeCell(null, i, new Vector2(12 + i * 26, y), eq[i]);
                c.EquipSlot = eq[i];
                _equipCells.Add(c);
            }
            var eqLbl = UIKit.Rect(Root, "EqLbl", new Vector2(0, 1), new Vector2(1, 1), new Vector2(250, y - 22), new Vector2(-4, y));
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

        /// <summary>One bag shown beside yours: the chest/loot/storage you opened, a companion, or your co-op partner.</summary>
        class Tab { public string Label; public Inventory Inv; public PlayerSlot Partner; }
        List<Tab> _tabs;
        int _tab;
        readonly List<RectTransform> _tabRts = new List<RectTransform>();

        /// <summary>What's in the panel beside your backpack right now (null when there's nobody and nothing).</summary>
        public Inventory OtherInv => _tabs != null && _tabs.Count > 0 ? _tabs[_tab].Inv : null;
        public IReadOnlyList<string> Tabs => (_tabs ?? new List<Tab>()).ConvertAll(t => t.Label);

        /// <summary>Shows another bag beside yours. A stack you're holding stays in your hand (so you can carry it across).</summary>
        public void SelectTab(int i)
        {
            if (_tabs == null || i < 0 || i >= _tabs.Count || i == _tab) return;
            _tab = i;
            _partner = _tabs[_tab].Partner;
            Rebuild();
        }

        /// <summary>The chest you opened (if any) first, then each companion, then a co-op partner within 3 tiles.</summary>
        void BuildTabs()
        {
            _tabs = new List<Tab>();
            var g = GameRoot.I;
            if (_otherInv != null) _tabs.Add(new Tab { Label = _otherTitle, Inv = _otherInv });
            foreach (var comp in g.CompanionsOf(Slot))
                if (comp.Character != null) _tabs.Add(new Tab { Label = comp.Character.Name, Inv = comp.Character.Bag });
            foreach (var p in g.Players)
                if (p != Slot && p.IsReady && p.Zone == Slot.Zone && Vector2.Distance(p.Avatar.transform.position, Slot.Avatar.transform.position) < 3f)
                    _tabs.Add(new Tab { Label = p.Character.Name, Inv = p.Character.Bag, Partner = p });
            _tab = Mathf.Clamp(_tab, 0, Mathf.Max(0, _tabs.Count - 1));
            _partner = _tabs.Count > 0 ? _tabs[_tab].Partner : null;
        }

        void Rebuild()
        {
            if (Root) Object.Destroy(Root.gameObject);
            _bagCells.Clear(); _otherCells.Clear(); _equipCells.Clear(); _tabRts.Clear();
            Build();
        }

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
                case "offset": return pc.OffSetWeapon?.Id;
                case "armor": return pc.Armor?.Id;
                case "shield": return pc.Shield ? "shield" : null;
                default: return pc.Worn.Find(w => w.Slot.ToString() == slot)?.Id;
            }
        }

        void Redraw()
        {
            if (Closed || Root == null) return;   // an options menu can outlive the inventory (e.g. combat starts)
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
                string cmp = ItemCompare.Describe(pc, def);
                if (cmp.Length > 0) head += "\n" + cmp;
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
            slot == "weapon" ? "Wpn" : slot == "offset" ? "Wpn 2" : slot == "armor" ? "Armor" : slot == "shield" ? "Shld" : slot == "Cloak" ? "Cloak" : slot == "Ring" ? "Ring" : slot == "Amulet" ? "Amul" : slot == "Trinket" ? "Trnk" : slot == "Focus" ? "Focus" : slot;

        static string SlotLong(string slot) => slot == "weapon" ? "Weapon" : slot == "offset" ? "Off-set weapon (swap: Z / hold X)" : slot == "armor" ? "Armor" : slot == "shield" ? "Shield" : slot;

        public override void Tick()
        {
            if (_partner != null && !GameRoot.I.Players.Contains(_partner))
            {
                // Your trade partner left the game: nothing more can go to (or come from) their bag.
                _partner = null;
                Hud.Toast("Your partner left, so the trade is closed.", UIKit.Dim);
                Close();
                return;
            }
            // Switching bags: click a tab (mouse) or LT/RT (controller).
            if (_tabs != null && _tabs.Count > 1)
            {
                if (In.UsesMouse && In.MouseLeft)
                    for (int i = 0; i < _tabRts.Count; i++)
                        if (UIKit.Contains(_tabRts[i], In.MouseScreen, Cam)) { SelectTab(i); return; }
                if (!In.UsesMouse && In.Spells) { SelectTab((_tab + 1) % _tabs.Count); return; }
                if (!In.UsesMouse && In.Journal) { SelectTab((_tab + _tabs.Count - 1) % _tabs.Count); return; }
            }
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
                GiveGold(In.QuickModifier ? 1000 : 100);
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
                if (def is WeaponDef) opts.Add(("Equip as off-set weapon (swap: Z / hold X)", () =>
                {
                    if (!Slot.Character.EquipFromBag(index, out var m, offSet: true)) Hud.Toast(m, UIKit.Bad); else Hud.Toast(m, UIKit.UtilityBlue);
                    Redraw();
                }));
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
            if (mine)
                foreach (var comp in g.CompanionsOf(Slot))
                    if (comp.Character != null && comp.Character.Bag != OtherInv)
                    {
                        var who = comp.Character;
                        opts.Add(($"Send to {who.Name}", () => { SendTo(index, who.Bag, who.Name); Redraw(); }));
                    }
            if (OtherInv != null)
                opts.Add((mine ? $"Send to {_tabs[_tab].Label}" : "Take", () => { _panel = mine ? 0 : 1; _cursor = index; QuickTransfer(); }));
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
                if (Bag.Slots[c.Index] == null) return;
                int to = c.Index < Inventory.HotbarSize ? Bag.ToBackpack(c.Index) : Bag.ToHotbar(c.Index);
                if (to < 0) Hud.Toast(c.Index < Inventory.HotbarSize ? "Your backpack is full." : "Your hotbar is full.", UIKit.Bad);
            }
            else if (_partner != null && c.Inv == Bag) SendToPartner(c.Index);
            else if (c.Inv == Bag) SendTo(c.Index, target, _tabs[_tab].Label); // says so if it doesn't all fit
            else c.Inv.QuickTransfer(c.Index, target);
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
                if (left > 0 && ItemsDb.Get(_held.ItemId)?.QuestItem == true) left = GameRoot.I.World.Storage.Add(_held.ItemId, left);
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
