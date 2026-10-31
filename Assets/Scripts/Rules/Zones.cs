using System;
using System.Collections.Generic;

namespace DnD.Rules
{
    /// <summary>
    /// A spell that lingers on the battlefield: Spirit Guardians (an aura that moves with the caster),
    /// Guardian of Faith (a fixed spot), Wall of Fire (a line of tiles). The combat layer draws it and calls
    /// Trigger when a creature enters it or ends its turn there; the rules for what happens live here.
    /// </summary>
    public class SpellZone
    {
        public SpellDef Spell;
        public PlayerCharacter Owner;
        public int Slot;
        /// <summary>Centre tile (ignored while FollowsOwner: the owner's tile is the centre).</summary>
        public int X, Y;
        public int Radius;
        public bool FollowsOwner;
        /// <summary>Explicit tiles (Wall of Fire); null for round zones.</summary>
        public List<(int x, int y)> Tiles;
        /// <summary>Damage it can still deal before it vanishes (Guardian of Faith: 60); -1 = unlimited.</summary>
        public int DamageLeft = -1;
        public bool Spent;
        public readonly HashSet<Creature> HitThisTurn = new HashSet<Creature>();
        public bool Concentration => Spell.Concentration;
    }

    public static class ZoneRules
    {
        public static SpellZone Create(PlayerCharacter owner, SpellDef s, int slot, int x, int y, List<(int x, int y)> tiles = null)
        {
            var z = new SpellZone { Spell = s, Owner = owner, Slot = Math.Max(slot, s.Level), X = x, Y = y, Radius = s.ZoneRadiusTiles, Tiles = tiles };
            z.FollowsOwner = s.Id == "spirit_guardians";
            if (s.Id == "guardian_of_faith") z.DamageLeft = 60;
            return z;
        }

        /// <summary>Is tile (x, y) inside the zone? ownerX/Y is where the caster stands (for auras).</summary>
        public static bool Contains(SpellZone z, int x, int y, int ownerX, int ownerY)
        {
            if (z.Spent) return false;
            if (z.Tiles != null) return z.Tiles.Contains((x, y));
            int cx = z.FollowsOwner ? ownerX : z.X, cy = z.FollowsOwner ? ownerY : z.Y;
            return Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)) <= z.Radius;
        }

        /// <summary>Whether the zone can hurt this creature at all (the guardians and the guardian spare the caster's side).</summary>
        public static bool Affects(SpellZone z, Creature c) =>
            !z.Spent && !c.IsDead && !c.IsBanished && c != z.Owner && (z.Spell.Id == "wall_of_fire" || c.Team != z.Owner.Team);

        /// <summary>A creature entered the zone or ended its turn in it: save, take damage (once per turn). Returns HP lost.</summary>
        public static int Trigger(SpellZone z, Creature victim, List<string> log)
        {
            if (!Affects(z, victim) || z.HitThisTurn.Contains(victim)) return 0;
            z.HitThisTurn.Add(victim);
            var s = z.Spell;
            if (s.Id == "spirit_guardians") victim.AddEffect(EffectLibrary.Create("spirit_slowed", z.Owner));
            var save = Checks.Rescue(victim, s.SaveAbility, z.Owner.SpellSaveDC, Checks.RollSave(victim, s.SaveAbility, AdvantageState.Normal), log);
            bool saved = Checks.ResolveSave(save, z.Owner.SpellSaveDC);
            log?.Add($"{victim.Name} {s.SaveAbility} save vs {s.Name}: {save.Total} vs DC {z.Owner.SpellSaveDC} → {(saved ? "saved" : "failed")}");
            var dmg = Dice.RollDamage(SpellResolver.EffectDice(z.Owner, s, z.Slot), false);
            int amount = Checks.AreaDamage(victim, s.SaveAbility, dmg.Total, saved, true);
            if (z.DamageLeft >= 0) amount = Math.Min(amount, z.DamageLeft);
            int lost = amount > 0 ? victim.TakeDamage(amount, s.DamageType, false, log) : 0;
            if (lost > 0) AttackResolver.RaiseConcentrationCheck(victim, lost);
            if (z.DamageLeft >= 0)
            {
                z.DamageLeft -= amount;
                if (z.DamageLeft <= 0) { z.Spent = true; log?.Add($"The {s.Name} fades, its duty done."); }
            }
            return lost;
        }

        /// <summary>Call at the start of every turn: "once per turn" resets.</summary>
        public static void NewTurn(SpellZone z) => z.HitThisTurn.Clear();
    }
}
