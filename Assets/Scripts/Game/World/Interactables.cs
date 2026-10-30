using System.Collections.Generic;
using DnD.Rules;
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
        public string Title = "Sign";
        public string Prompt = "Read";
        public float Reach = 1.4f;
        public override float Range => Reach;
        public override string PromptText(PlayerSlot p) => Prompt;
        public override void Interact(PlayerSlot p) => p.Hud.Push(new MessageScreen(Title, Text));
    }

    /// <summary>
    /// A hidden spot or special place (hollow log, fairy ring...). The player rolls the skill check on screen;
    /// success gives the reward once (or once a day), failure can be retried the next day.
    /// </summary>
    public class SecretInteractable : Interactable
    {
        public SecretDef Def;
        public override float Range => 1.5f;
        string FoundKey => "secret:" + Def.Id;
        string TodayKey(PlayerSlot p) => $"secret:{Def.Id}:{p.Index}";

        public override string PromptText(PlayerSlot p) => Def.Prompt;

        bool Present
        {
            get
            {
                if (!Def.IsQuestSpot) return true;
                var g = GameRoot.I;
                if (g.Quests.Stage(Def.QuestId) != Def.QuestStage) return false;
                if (!Def.Daily && g.World.Flag(FoundKey)) return false; // done: the goat went home, the herb is picked
                if (Def.NightOnly && g.Clock.Hour >= 5 && g.Clock.Hour < 20) return false;
                return true;
            }
        }
        public override bool Available(PlayerSlot p) => Present;

        SpriteRenderer _sr;
        void Update()
        {
            if (!Def.IsQuestSpot) return;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.enabled = Present;
        }

        public override void Interact(PlayerSlot p)
        {
            if (p.IsBusy || !Present) return; // one roll at a time (two inputs in the same frame must not open two dice trays)
            var g = GameRoot.I;
            var w = g.World;
            if (Def.PassageTo.HasValue && w.Flag(FoundKey)) { p.Hud.Narrate(Def.AlreadyFound, false); g.MoveWithin(p, Def.PassageTo.Value); return; } // a known way through
            if (!Def.Daily && w.Flag(FoundKey)) { p.Hud.Narrate(Def.AlreadyFound, false); return; }
            if (w.IsLocked(TodayKey(p)))
            {
                p.Hud.Narrate(Def.Daily ? Def.AlreadyFound : "You've already tried today. Come back tomorrow with fresh eyes.", false);
                return;
            }

            var pc = p.Character;
            string sub = $"{SkillUtil.Name(Def.Skill)} ({SkillUtil.AbilityOf(Def.Skill)}) {AbilityUtil.Fmt(pc.SkillBonus(Def.Skill))} · DC hidden";
            p.Hud.Dice.RollD20($"{SkillUtil.Name(Def.Skill)} Check", sub, () => Checks.RollSkill(pc, Def.Skill, AdvantageState.Normal), Def.DC, true,
                r => Resolve(p, Checks.ResolveCheck(r, Def.DC)));
        }

        /// <summary>Everything after the roll: lock-outs, rewards, quest progress and the result window.</summary>
        public void Resolve(PlayerSlot p, CheckOutcome outcome)
        {
            var g = GameRoot.I;
            var w = g.World;
            var pc = p.Character;
            {
                bool success = Checks.IsSuccess(outcome);
                if (Def.Daily || !success) w.Lock(TodayKey(p), 24 * 60 - g.Clock.Minutes); // until midnight
                if (!success)
                {
                    Sfx.Play("fail");
                    p.Hud.Push(new MessageScreen(Def.Prompt, Def.Failure));
                    return;
                }

                Sfx.Play("secret"); // the found-a-secret jingle
                bool first = !w.Flag(FoundKey);
                w.SetFlag(FoundKey);
                if (!Def.IsQuestSpot) g.AwardOnce(p, FoundKey, Def.DC * (outcome == CheckOutcome.CriticalSuccess ? 3 : 2), "finding a secret"); // quest spots: the quest pays
                foreach (var kv in Def.Items) g.GiveItem(p, kv.Key, kv.Value);
                if (Def.Copper > 0)
                {
                    pc.Copper += Def.Copper;
                    p.Hud.Toast($"+{Currency.Format(Def.Copper)}", UIKit.Gold);
                }
                if (Def.HeroicInspiration) pc.HeroicInspiration = true;
                string text = Def.Success;
                if (first && Def.FirstTimeItem != null)
                {
                    g.GiveItem(p, Def.FirstTimeItem);
                    text += "\n\n" + Def.FirstTimeText;
                }
                if (Def.SetsFlag != null) w.SetFlag(Def.SetsFlag);
                if (Def.PacifiesGroup != null) g.PacifyGroup(Def.PacifiesGroup, p, fled: true);
                if (Def.IsQuestSpot && Def.AdvanceTo > 0) g.Quests.Set(Def.QuestId, Def.AdvanceTo, p);
                g.Quests.Notify(QuestEvent.SecretFound, Def.Id, p);
                if (Def.PassageTo.HasValue) g.MoveWithin(p, Def.PassageTo.Value);
                p.Hud.Push(new MessageScreen(Def.Prompt, text));
            }
        }
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
            var loot = new Inventory(12);
            foreach (var kv in Def.Items) loot.Add(kv.Key, kv.Value);
            if (Def.Copper > 0)
            {
                p.Character.Copper += Def.Copper;
                p.Hud.Toast($"+{Currency.Format(Def.Copper)}", UIKit.Gold);
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
