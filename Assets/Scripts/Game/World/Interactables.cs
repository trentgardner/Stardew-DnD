using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Anything a player can walk up to and press Interact on.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public virtual float Range => 1.4f;
        public abstract string PromptText(PlayerSlot p);
        public abstract void Interact(PlayerSlot p);
        public virtual bool Available(PlayerSlot p) => true;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public static Interactable Nearest(PlayerSlot p, Vector3 pos, Vector2 facing)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var i in All)
            {
                if (i == null || !i.isActiveAndEnabled || !i.Available(p)) continue;
                Vector2 d = i.transform.position - pos;
                float dist = d.magnitude;
                if (dist > i.Range) continue;
                float score = dist - Vector2.Dot(d.normalized, facing) * 0.6f; // prefer what you're facing
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }
    }

    public class SignInteractable : Interactable
    {
        public string Text;
        public override string PromptText(PlayerSlot p) => "Read";
        public override void Interact(PlayerSlot p) => p.Hud.Push(new MessageScreen("Sign", Text));
    }

    public class BedInteractable : Interactable
    {
        public override string PromptText(PlayerSlot p) => "Sleep (Long Rest)";
        public override void Interact(PlayerSlot p) => p.Hud.Push(new RestScreen(true));
    }

    public class ChestInteractable : Interactable
    {
        public ChestDef Def;
        SpriteRenderer _sr;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (Def != null && !Def.IsStorage && GameRoot.I.World.OpenedChests.Contains(Def.Id)) _sr.sprite = ArtRegistry.Get("obj_chest_open");
        }

        public override string PromptText(PlayerSlot p)
        {
            if (Def == null) return "Chest";
            if (Def.IsStorage) return "Open storage chest";
            return GameRoot.I.World.OpenedChests.Contains(Def.Id) ? "Chest (empty)" : "Open chest";
        }

        public override void Interact(PlayerSlot p)
        {
            var g = GameRoot.I;
            if (Def == null) return;
            if (Def.IsStorage) { p.Hud.Push(new InventoryScreen(g.World.Storage, "Storage Chest")); return; }
            if (g.World.OpenedChests.Contains(Def.Id)) { p.Hud.Narrate("Empty. Someone, probably you, already cleaned it out.", false); return; }
            g.World.OpenedChests.Add(Def.Id);
            _sr.sprite = ArtRegistry.Get("obj_chest_open");
            Sfx.Play("coin");
            var loot = new Rules.Inventory(12);
            foreach (var kv in Def.Items) loot.Add(kv.Key, kv.Value);
            if (Def.Copper > 0)
            {
                p.Character.Copper += Def.Copper;
                p.Hud.Toast($"+{Rules.Currency.Format(Def.Copper)}", UIKit.Gold);
            }
            p.Hud.Push(new InventoryScreen(loot, "Chest"));
        }
    }

    /// <summary>The dragon egg. Hatches once its guardians are dealt with (by sword or by word).</summary>
    public class EggInteractable : Interactable
    {
        public override float Range => 1.8f;
        public override bool Available(PlayerSlot p) => !GameRoot.I.World.EggHatched;
        public override string PromptText(PlayerSlot p) => "Approach the egg";
        public override void Interact(PlayerSlot p)
        {
            var g = GameRoot.I;
            bool guarded = !g.World.DefeatedGroups.Contains("mine_egg") && !g.World.PacifiedGroups.Contains("mine_egg");
            if (guarded) { p.Hud.Narrate("Snik plants himself between you and the egg, spear trembling. \"NOT. ONE. STEP.\""); return; }
            g.HatchEgg(p, transform.position);
            GetComponent<SpriteRenderer>().sprite = ArtRegistry.Get("item_egg_shard");
        }
    }
}
