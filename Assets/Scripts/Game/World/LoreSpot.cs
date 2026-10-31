using UnityEngine;

namespace DnD.Game
{
    /// <summary>Where a piece of lore sits in a zone (ZoneDef.Lore).</summary>
    public class LoreSpotDef
    {
        public string Id;
        public Vector2Int Pos;
        public string Sprite = "item_scroll_revivify";
        public string Prompt = "Read";
    }

    /// <summary>A mural, carving or record you can read: it goes into the journal's Lore page.</summary>
    public class LoreSpot : Interactable
    {
        public LoreSpotDef Def;
        public override float Range => 1.5f;
        public override string PromptText(PlayerSlot p) => Def.Prompt;

        /// <summary>Only in the act its lore belongs to (the legend waits for Act II).</summary>
        public bool InAct => LoreDb.Get(Def.Id) is LoreDef l && GameRoot.I.World.Act >= l.MinAct;
        public override bool Available(PlayerSlot p) => InAct;

        SpriteRenderer _sr;
        void Update()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _sr.enabled = InAct;
        }

        public override void Interact(PlayerSlot p)
        {
            var l = LoreDb.Get(Def.Id);
            if (l == null || !InAct) return;
            LoreDb.Find(Def.Id, p);
            p.Hud.Push(new MessageScreen(l.Title, l.Text));
        }
    }
}
