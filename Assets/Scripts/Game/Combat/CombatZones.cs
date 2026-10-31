using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Lingering spells on the battlefield (Spirit Guardians, Guardian of Faith, Wall of Fire): drawing and triggering.</summary>
    public partial class CombatInstance
    {
        public readonly List<SpellZone> Zones = new List<SpellZone>();
        readonly Dictionary<SpellZone, List<GameObject>> _zoneFx = new Dictionary<SpellZone, List<GameObject>>();

        Unit UnitOf(Creature c) => Units.FirstOrDefault(u => u.C == c);

        public void AddZone(SpellZone z)
        {
            Zones.Add(z);
            DrawZones();
        }

        /// <summary>Drops zones whose caster stopped concentrating (or fell), or that have done their duty.</summary>
        void PruneZones()
        {
            foreach (var z in Zones.ToList())
            {
                var owner = UnitOf(z.Owner);
                bool gone = z.Spent || owner == null || z.Owner.IsDead || (z.Concentration && z.Owner.ConcentratingOn != z.Spell.Id);
                if (!gone) continue;
                Zones.Remove(z);
                if (_zoneFx.TryGetValue(z, out var fx)) { foreach (var g in fx) if (g) UnityEngine.Object.Destroy(g); _zoneFx.Remove(z); }
            }
        }

        static Color ZoneColor(SpellZone z) =>
            z.Spell.Id == "wall_of_fire" ? new Color(1f, 0.45f, 0.1f, 0.5f) : z.Spell.Id == "spirit_guardians" ? new Color(1f, 0.95f, 0.55f, 0.28f) : new Color(1f, 0.85f, 0.3f, 0.3f);

        public IEnumerable<Vector2Int> ZoneTiles(SpellZone z)
        {
            if (z.Tiles != null) { foreach (var t in z.Tiles) yield return new Vector2Int(t.x, t.y); yield break; }
            var owner = UnitOf(z.Owner);
            var c = z.FollowsOwner && owner != null ? owner.Pos : new Vector2Int(z.X, z.Y);
            for (int dx = -z.Radius; dx <= z.Radius; dx++)
                for (int dy = -z.Radius; dy <= z.Radius; dy++)
                {
                    var p = c + new Vector2Int(dx, dy);
                    if (Zone.IsWalkable(p)) yield return p;
                }
        }

        /// <summary>Redraws every zone (auras move with their caster).</summary>
        public void DrawZones()
        {
            PruneZones();
            var spr = ArtRegistry.Get("tile_highlight");
            foreach (var z in Zones)
            {
                if (_zoneFx.TryGetValue(z, out var old)) foreach (var g in old) if (g) UnityEngine.Object.Destroy(g);
                var list = new List<GameObject>();
                foreach (var t in ZoneTiles(z))
                {
                    var go = new GameObject("zone_" + z.Spell.Id);
                    go.transform.position = Zone.ToWorld(t);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    sr.color = ZoneColor(z);
                    sr.sortingOrder = -850;
                    list.Add(go);
                }
                _zoneFx[z] = list;
            }
        }

        void ClearZoneFx()
        {
            foreach (var kv in _zoneFx) foreach (var g in kv.Value) if (g) UnityEngine.Object.Destroy(g);
            _zoneFx.Clear();
            Zones.Clear();
        }

        bool InZone(SpellZone z, Unit u)
        {
            var owner = UnitOf(z.Owner);
            var o = owner != null ? owner.Pos : Vector2Int.zero;
            return ZoneRules.Contains(z, u.Pos.x, u.Pos.y, o.x, o.y);
        }

        /// <summary>A unit entered a zone or ended its turn in one: each zone it's in triggers (once per turn per zone).</summary>
        public void TriggerZones(Unit u)
        {
            PruneZones();
            if (!u.Active) return;
            foreach (var z in Zones.ToList())
            {
                if (!InZone(z, u) || !ZoneRules.Affects(z, u.C) || z.HitThisTurn.Contains(u.C)) continue;
                var lines = new List<string>();
                int before = u.C.HP;
                int lost = ZoneRules.Trigger(z, u.C, lines);
                lines.ForEach(Log);
                if (lost > 0) u.A.Popup("-" + lost, UIKit.DamageRed);
                OnDamaged(u);
                if (!u.Active) break;
            }
            if (Zones.Any(z => z.Spent)) DrawZones();
        }

        /// <summary>Every unit standing in this zone right now makes the save (Wall of Fire appearing on top of them).</summary>
        void TriggerZoneOnAll(SpellZone z)
        {
            foreach (var u in Units.ToList()) if (u.Active && InZone(z, u)) TriggerZones(u);
        }

        /// <summary>Ends a creature's concentration: its effects on everyone, and any zone it was holding.</summary>
        public void EndConcentration(Creature c)
        {
            Enc.EndConcentration(c);
            DrawZones();
            SyncBanished();
        }

        /// <summary>Banished creatures fade from the map until they come back.</summary>
        void SyncBanished()
        {
            foreach (var u in Units)
                if (u.A != null && u.A.Sr != null && !u.C.IsDead)
                {
                    var col = u.A.Sr.color;
                    col.a = u.C.IsBanished ? 0.15f : 1f;
                    u.A.Sr.color = col;
                }
        }

        void ZonesNewTurn()
        {
            foreach (var z in Zones) ZoneRules.NewTurn(z);
            DrawZones();
        }
    }
}
