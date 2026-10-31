using System;
using System.Collections.Generic;
using DnD.Rules.Content;

namespace DnD.Rules
{
    /// <summary>
    /// Stardew-style slot inventory. Slots hold stacks up to each item's MaxStack.
    /// The UI drives it with a "held stack" (the stack on the cursor):
    ///   PickUp(slot, n) → held;  Place(slot, held) → whatever is still held (leftover or swapped stack).
    /// All operations conserve items: nothing is ever created or lost.
    /// </summary>
    [Serializable]
    public class Inventory
    {
        public const int DefaultSize = 36;     // 3 rows of 12, first row = hotbar
        public const int HotbarSize = 12;

        public ItemStack[] Slots;

        public Inventory(int size = DefaultSize) { Slots = new ItemStack[size]; }

        public int Size => Slots.Length;
        public event Action Changed;
        void Notify() => Changed?.Invoke();

        static int MaxStackOf(string id)
        {
            var def = ItemsDb.Get(id);
            return def != null ? def.MaxStack : 1;
        }

        public bool IsEmpty(int slot) => Slots[slot] == null || Slots[slot].Count <= 0;

        // ------------------------------------------------------------ counting / adding / removing

        public int Count(string id)
        {
            int n = 0;
            foreach (var s in Slots) if (s != null && s.ItemId == id) n += s.Count;
            return n;
        }

        /// <summary>How many of this item could be added right now.</summary>
        public int SpaceFor(string id)
        {
            int max = MaxStackOf(id), space = 0;
            foreach (var s in Slots)
            {
                if (s == null) space += max;
                else if (s.ItemId == id) space += Math.Max(0, max - s.Count);
            }
            return space;
        }

        /// <summary>Adds items, topping up existing stacks first, then empty slots. Returns the leftover that didn't fit.</summary>
        public int Add(string id, int count)
        {
            if (count <= 0) return 0;
            int max = MaxStackOf(id);
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                var s = Slots[i];
                if (s == null || s.ItemId != id || s.Count >= max) continue;
                int add = Math.Min(count, max - s.Count);
                s.Count += add; count -= add;
            }
            for (int i = 0; i < Slots.Length && count > 0; i++)
            {
                if (Slots[i] != null) continue;
                int add = Math.Min(count, max);
                Slots[i] = new ItemStack(id, add); count -= add;
            }
            Notify();
            return count;
        }

        /// <summary>Removes items (from the last stacks first, so the hotbar stays stocked). False if not enough.</summary>
        public bool Remove(string id, int count)
        {
            if (Count(id) < count) return false;
            for (int i = Slots.Length - 1; i >= 0 && count > 0; i--)
            {
                var s = Slots[i];
                if (s == null || s.ItemId != id) continue;
                int take = Math.Min(count, s.Count);
                s.Count -= take; count -= take;
                if (s.Count <= 0) Slots[i] = null;
            }
            Notify();
            return true;
        }

        // ------------------------------------------------------------ cursor (held stack) operations

        /// <summary>Lifts up to `count` items out of a slot (count &lt;= 0 = whole stack).</summary>
        public ItemStack PickUp(int slot, int count = 0)
        {
            var s = Slots[slot];
            if (s == null) return null;
            if (count <= 0 || count >= s.Count) { Slots[slot] = null; Notify(); return s; }
            s.Count -= count;
            Notify();
            return new ItemStack(s.ItemId, count);
        }

        /// <summary>Picks up half the stack, rounded up (right-click / X button).</summary>
        public ItemStack PickUpHalf(int slot)
        {
            var s = Slots[slot];
            if (s == null) return null;
            return PickUp(slot, (s.Count + 1) / 2);
        }

        /// <summary>
        /// Puts a held stack into a slot. Empty slot: placed. Same item: merged up to max (leftover stays held).
        /// Different item: swapped (the old slot contents become held). Returns what is held afterwards.
        /// </summary>
        public ItemStack Place(int slot, ItemStack held)
        {
            if (held == null || held.Count <= 0) return null;
            var s = Slots[slot];
            if (s == null) { Slots[slot] = held; Notify(); return null; }
            if (s.ItemId == held.ItemId)
            {
                int max = MaxStackOf(s.ItemId);
                int add = Math.Min(held.Count, max - s.Count);
                s.Count += add; held.Count -= add;
                Notify();
                return held.Count > 0 ? held : null;
            }
            Slots[slot] = held;
            Notify();
            return s;
        }

        /// <summary>Places exactly one item from the held stack (right-click while holding).</summary>
        public ItemStack PlaceOne(int slot, ItemStack held)
        {
            if (held == null || held.Count <= 0) return null;
            var s = Slots[slot];
            if (s == null) { Slots[slot] = new ItemStack(held.ItemId, 1); held.Count--; }
            else if (s.ItemId == held.ItemId && s.Count < MaxStackOf(s.ItemId)) { s.Count++; held.Count--; }
            Notify();
            return held.Count > 0 ? held : null;
        }

        /// <summary>Returns a held stack to the inventory (closing the menu while holding something). Returns leftover.</summary>
        public int ReturnHeld(ItemStack held) => held == null ? 0 : Add(held.ItemId, held.Count);

        // ------------------------------------------------------------ slot-to-slot

        /// <summary>Drag from one slot to another: merge if same item, otherwise swap.</summary>
        public void Move(int from, int to)
        {
            if (from == to || Slots[from] == null) return;
            var held = PickUp(from);
            var back = Place(to, held);
            if (back != null)
            {
                // Swapped or partially merged: put the remainder back where the drag began.
                if (Slots[from] == null) Slots[from] = back;
                else Add(back.ItemId, back.Count);
            }
            Notify();
        }

        /// <summary>Splits a stack in half into the first empty slot. False if no room.</summary>
        public bool Split(int slot)
        {
            var s = Slots[slot];
            if (s == null || s.Count < 2) return false;
            int empty = Array.FindIndex(Slots, x => x == null);
            if (empty < 0) return false;
            int half = s.Count / 2;
            s.Count -= half;
            Slots[empty] = new ItemStack(s.ItemId, half);
            Notify();
            return true;
        }

        /// <summary>Shift-click: sends a whole slot to another inventory (other player, chest, shop). Returns how many moved.</summary>
        public int QuickTransfer(int slot, Inventory target, int count = 0)
        {
            var s = Slots[slot];
            if (s == null || target == null) return 0;
            int n = count <= 0 ? s.Count : Math.Min(count, s.Count);
            int leftover = target.Add(s.ItemId, n);
            int moved = n - leftover;
            s.Count -= moved;
            if (s.Count <= 0) Slots[slot] = null;
            Notify();
            return moved;
        }

        /// <summary>Moves a backpack stack to the first free hotbar slot (0–11). Returns that slot, or -1 if the hotbar is full.</summary>
        public int ToHotbar(int slot) => MoveToFirstFree(slot, 0, HotbarSize);

        /// <summary>Moves a hotbar stack to the first free backpack slot. Returns that slot, or -1 if the backpack is full.</summary>
        public int ToBackpack(int slot) => MoveToFirstFree(slot, HotbarSize, Slots.Length);

        int MoveToFirstFree(int slot, int start, int end)
        {
            if (Slots[slot] == null) return -1;
            for (int i = start; i < end; i++) if (Slots[i] == null) { Move(slot, i); return i; }
            return -1;
        }

        /// <summary>Combines partial stacks of the same item.</summary>
        public void Consolidate()
        {
            // Merge later partial stacks into earlier ones, leaving positions otherwise untouched.
            for (int i = 1; i < Slots.Length; i++)
            {
                var s = Slots[i];
                if (s == null) continue;
                int max = MaxStackOf(s.ItemId);
                if (max <= 1) continue;
                for (int j = 0; j < i && s.Count > 0; j++)
                {
                    var t = Slots[j];
                    if (t == null || t.ItemId != s.ItemId || t.Count >= max) continue;
                    int move = Math.Min(s.Count, max - t.Count);
                    t.Count += move; s.Count -= move;
                }
                if (s.Count <= 0) Slots[i] = null;
            }
            Notify();
        }

        /// <summary>Auto-sort the backpack (hotbar row is left alone). Groups by kind, then name.</summary>
        public void Sort()
        {
            Consolidate();
            var items = new List<ItemStack>();
            for (int i = HotbarSize; i < Slots.Length; i++)
            {
                if (Slots[i] != null) items.Add(Slots[i]);
                Slots[i] = null;
            }
            items.Sort((a, b) =>
            {
                var da = ItemsDb.Get(a.ItemId); var db = ItemsDb.Get(b.ItemId);
                int k = (da?.Kind ?? ItemKind.Gear).CompareTo(db?.Kind ?? ItemKind.Gear);
                if (k != 0) return k;
                int n = string.Compare(da?.Name, db?.Name, StringComparison.Ordinal);
                return n != 0 ? n : b.Count.CompareTo(a.Count);
            });
            int slot = HotbarSize;
            foreach (var it in items)
            {
                while (slot < Slots.Length && Slots[slot] != null) slot++;
                if (slot >= Slots.Length) { Add(it.ItemId, it.Count); continue; }
                Slots[slot++] = it;
            }
            Notify();
        }

        /// <summary>Total weight in pounds of everything in the bag.</summary>
        public float TotalWeight()
        {
            float w = 0;
            foreach (var s in Slots)
            {
                if (s == null) continue;
                var def = ItemsDb.Get(s.ItemId);
                if (def != null) w += def.Weight * s.Count;
            }
            return w;
        }

        public int TotalItems()
        {
            int n = 0;
            foreach (var s in Slots) if (s != null) n += s.Count;
            return n;
        }
    }
}
