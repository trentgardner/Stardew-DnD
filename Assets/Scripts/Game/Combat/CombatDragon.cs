using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>The party's hatchling in a fight: a bite, Ember Breath on a 5–6 recharge, and a scared retreat at 0 HP.</summary>
    public partial class CombatInstance
    {
        /// <summary>The dragon's turn under a player's command. Short on purpose: it's a pet, not a fifth hero.</summary>
        public List<CombatAction> DragonActions(Unit u)
        {
            var d = (Dragonling)u.C;
            var t = u.Cb.Turn;
            var bite = d.GetAttacks()[0];
            var list = new List<CombatAction>();
            list.Add(new CombatAction { Name = "Move", Cost = ActionCost.Free, Target = Targeting.Tile });
            list.Add(Need(new CombatAction { Name = $"Attack ({bite.Name})", Cost = ActionCost.Action, Target = Targeting.Enemy, RangeFt = bite.ReachFt,
                Tooltip = bite.Summary(), Run = (me, tile) => AttackAction(me, UnitAt(tile), bite) }, t, null));
            var breath = new CombatAction { Name = "Ember Breath", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.Tile,
                AreaTiles = d.BreathConeTiles, AreaFromSelf = true, RangeFt = 5,
                Tooltip = $"A puff of fire in a 5-ft cone: {d.BreathDamage} fire, DEX save DC {d.BreathDC} for half. Recharges on a 5–6 at the start of {d.Name}'s turn.",
                Why = d.BreathReady ? null : "Recharging (5–6 on a d6 each turn).",
                Run = (me, tile) => EmberBreath(me, tile) };
            list.Add(Need(breath, t, null, d.BreathReady));
            list.Add(Need(new CombatAction { Name = "Dodge", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.None, Run = (me, tile) => Simple(me, "Dodge") }, t, null));
            list.Add(new CombatAction { Name = "Auto", Group = ActionGroup.End, Cost = ActionCost.Free, Target = Targeting.None, EndsTurn = true,
                Tooltip = $"Let {d.Name} decide this turn. To let it fight on its own for good, pet it outside of combat.", Run = (me, tile) => AiTurn(me) });
            list.Add(new CombatAction { Name = "End Turn", Group = ActionGroup.End, Cost = ActionCost.Free, Target = Targeting.None, EndsTurn = true, Run = (me, tile) => EndTurnNow(me) });
            return list;
        }

        /// <summary>Who a breath at this tile would catch (everyone in the cone but the dragon itself).</summary>
        List<Unit> BreathTargets(Unit me, Vector2Int tile)
        {
            var d = (Dragonling)me.C;
            var area = AreaTiles(me, new CombatAction { AreaTiles = d.BreathConeTiles, AreaFromSelf = true, RangeFt = 5 }, tile);
            return Units.Where(o => o != me && !o.C.IsDead && o.Active && area.Contains(o.Pos)).ToList();
        }

        /// <summary>Ember Breath: one damage roll, then a visible DEX save for everyone in the cone (half on a success).</summary>
        public IEnumerator EmberBreath(Unit me, Vector2Int tile)
        {
            var d = (Dragonling)me.C;
            me.Cb.Turn.ActionUsed = true;
            d.BreathReady = false;
            me.A.Face(Zone.ToWorld(tile));
            var targets = BreathTargets(me, tile);
            Sfx.Play("spell");
            me.A.Popup("Ember Breath!", UIKit.Gold);
            var dmg = Dice.RollDamage(d.BreathDamage);
            yield return WaitDamageRoll(me, "Ember Breath!", $"{d.BreathDamage} fire · DEX save DC {d.BreathDC} for half", dmg, DieColor.Damage);
            if (targets.Count == 0) Log($"{me.Name} puffs a cloud of sparks at nothing in particular.");
            foreach (var t in targets)
            {
                var save = Checks.RollSave(t.C, Ability.DEX, AdvantageState.Normal);
                bool saved = Checks.ResolveSave(save, d.BreathDC);
                Ticker($"{t.Name} DEX save: {save.Breakdown()} vs DC {d.BreathDC} → {(saved ? "SAVED" : "FAILED")}");
                yield return new WaitForSeconds(0.45f);
                var lines = new List<string>();
                int before = t.C.HP;
                t.C.TakeDamage(saved ? dmg.Total / 2 : dmg.Total, DamageType.Fire, false, lines);
                lines.ForEach(Log);
                t.A.Popup("-" + Math.Max(0, before - t.C.HP), UIKit.DamageRed);
                OnDamaged(t);
            }
            RefreshTracker();
        }

        /// <summary>Start of the dragon's turn: a d6 for the breath, shown in the log. d6 is for tests; null rolls the real die.</summary>
        public void RechargeBreath(Unit u, Func<int> d6)
        {
            if (!(u.C is Dragonling d) || d.BreathReady) return;
            int r = d6 != null ? d6() : Dice.Roll(6);
            d.RollRecharge(() => r);
            Log($"{u.Name}'s Ember Breath Recharge: d6 = {r} → {(d.BreathReady ? "ready!" : "not yet (needs 5–6)")}");
        }

        /// <summary>The AI breathes when its target is in the cone and no friend is.</summary>
        bool GoodBreath(Unit me, Vector2Int tile)
        {
            if (!(me.C is Dragonling d) || !d.BreathReady || Zone.DistanceFt(me.Pos, tile) > 5) return false;
            var hit = BreathTargets(me, tile);
            return hit.Any(o => Hostile(me, o)) && !hit.Any(o => !Hostile(me, o));
        }

        /// <summary>At 0 HP the hatchling isn't slain: it bolts for safety and sits out fights until the party rests.</summary>
        void ScareOffDragon(Unit u)
        {
            if (!u.A.gameObject.activeSelf) return;
            G.World.DragonScared = true;
            Log($"{u.Name} retreats, scared! (Back to their brave self after a rest.)");
            foreach (var h in Humans) h.Hud.Narrate($"{u.Name} squeals and bolts behind the nearest rock! They'll be brave again after a rest.");
            u.A.gameObject.SetActive(false);
        }
    }
}
