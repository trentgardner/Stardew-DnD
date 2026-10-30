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
        public string Key;
        public bool Lit { get; private set; }
        public override float Range => 1.6f;

        protected override void OnEnable() { base.OnEnable(); All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

        public static string KeyFor(string zoneId, Vector2Int m) => $"{zoneId}:{m.x},{m.y}";

        public void Init(string zoneId, Vector2Int m)
        {
            Key = KeyFor(zoneId, m);
            if (GameRoot.I.World.LitBraziers.Contains(Key)) SetLit();
        }

        public void SetLit()
        {
            Lit = true;
            GetComponent<SpriteRenderer>().sprite = ArtRegistry.Get("obj_brazier");
            var l = GetComponent<LightSource>() ?? LightSource.Attach(gameObject, 4f, new Color(1f, 0.8f, 0.45f, 1f));
            l.On = true;
        }

        public override bool Available(PlayerSlot p) => !Lit;

        public override string PromptText(PlayerSlot p) => DragonHere(p) ? $"Ask {GameRoot.I.World.DragonName} to light the brazier" : "Look at the cold brazier";

        static bool DragonHere(PlayerSlot p)
        {
            var a = GameRoot.I.DragonAgent;
            return a != null && a.gameObject.activeSelf && a.Actor.Zone == p.Zone;
        }

        public override void Interact(PlayerSlot p)
        {
            if (DragonHere(p)) { GameRoot.I.DragonAgent.GetComponent<DragonPet>().Light(p, this); return; }
            p.Hud.Narrate("An iron brazier full of cold ash and half-burnt sticks. If only you had something that breathed fire...", false);
        }
    }
}
