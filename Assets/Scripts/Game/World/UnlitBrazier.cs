using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// A cold brazier in a dark place (map char 'y'). The dragon can light it with a puff of flame; it stays lit for good
    /// (WorldState.LitBraziers, key "zoneId:x,y").
    /// </summary>
    public class UnlitBrazier : Interactable
    {
        public static readonly List<UnlitBrazier> All = new List<UnlitBrazier>();
        public string Key, ZoneId;
        /// <summary>What it looks like lit, and what it's called ("brazier", "lantern post").</summary>
        public string LitSprite = "obj_brazier", Noun = "brazier";
        public bool Lit { get; private set; }
        public override float Range => 1.6f;

        protected override void OnEnable() { base.OnEnable(); All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        public static string KeyFor(string zoneId, Vector2Int m) => $"{zoneId}:{m.x},{m.y}";

        public void Init(string zoneId, Vector2Int m, string litSprite = "obj_brazier", string noun = "brazier")
        {
            LitSprite = litSprite;
            Noun = noun;
            ZoneId = zoneId;
            Key = KeyFor(zoneId, m);
            if (GameRoot.I.World.LitBraziers.Contains(Key)) SetLit();
        }

        public void SetLit()
        {
            Lit = true;
            GetComponent<SpriteRenderer>().sprite = ArtRegistry.Get(LitSprite);
            var l = GetComponent<LightSource>() ?? LightSource.Attach(gameObject, 4f, new Color(1f, 0.8f, 0.45f, 1f));
            l.On = true;
        }

        public override bool Available(PlayerSlot p) => !Lit;

        public override string PromptText(PlayerSlot p) =>
            DragonHere(p) ? $"Ask {GameRoot.I.World.DragonName} to light the {Noun}" : TorchLit(p) ? $"Light the {Noun} with your torch" : $"Look at the cold {Noun}";

        static bool TorchLit(PlayerSlot p) => p.Avatar != null && p.Avatar.TorchMinutes > 0;

        /// <summary>It's lit for good (saved), and any quest counting lit lights hears about it.</summary>
        public void MarkLit(PlayerSlot p)
        {
            if (Lit) return;
            GameRoot.I.World.LitBraziers.Add(Key);
            SetLit();
            GameRoot.I.Quests.Notify(QuestEvent.BrazierLit, ZoneId, p);
        }

        static bool DragonHere(PlayerSlot p)
        {
            var a = GameRoot.I.DragonAgent;
            return a != null && a.gameObject.activeSelf && a.Actor.Zone == p.Zone;
        }

        public override void Interact(PlayerSlot p)
        {
            if (DragonHere(p)) { GameRoot.I.DragonAgent.GetComponent<DragonPet>().Light(p, this); return; }
            if (TorchLit(p))
            {
                MarkLit(p);
                p.Hud.Narrate($"You touch your torch to the {Noun}. It catches, slowly, and then all at once.", false);
                return;
            }
            p.Hud.Narrate(Noun == "brazier" ? "An iron brazier full of cold ash and half-burnt sticks. If only you had something that breathed fire..."
                : "A black iron lantern post. The wick is dry and the glass is clean. A torch would do it. So would a dragon.", false);
        }
    }
}
