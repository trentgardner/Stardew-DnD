using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;

namespace DnD.Game
{
    public enum ActionCost { Action, Bonus, Free }
    public enum Targeting { None, Enemy, Ally, AllyDown, AllyDead, Tile }
    /// <summary>Which action-bar button an action lives under (see CombatHud).</summary>
    public enum ActionGroup { Main, Spells, Abilities, Actions, Items, End }

    public class CombatAction
    {
        public string Name;
        public ActionCost Cost;
        public ActionGroup Group;
        public Targeting Target;
        public int RangeFt;
        public int AreaTiles;          // sphere radius / cone length in tiles (0 = single target)
        public bool AreaFromSelf;      // cones & cubes originate at the caster
        public bool Enabled = true;
        public string Why;             // reason it's disabled
        public Func<Unit, Vector2Int, IEnumerator> Run;
        public string Tooltip;
        public bool EndsTurn;          // End Turn, Auto: the turn is over once it runs
    }

    public partial class CombatInstance
    {
        // ================================================================== player turn

        IEnumerator PlayerTurn(Unit u)
        {
            var p = u.Ctrl;
            var pc = (PlayerCharacter)u.C;
            var hud = p.Hud.Find<CombatHud>();
            if (hud == null) { hud = new CombatHud(this); p.Hud.Push(hud); }
            p.Hud.Narrate(u.Owner != null ? $"Your turn, {pc.Name}!" : $"{pc.Name}'s turn. Your orders?", u.Owner == null);
            Vector2Int cursor = u.Pos;
            int selected = 0;
            bool ended = false;
            bool busy = false;
            pc.Effects.RemoveAll(e => e.Id == "dodge");

            while (!ended && !Finished && u.Active)
            {
                if (p.StalledByDisconnect)
                {
                    foreach (var h in Humans) h.Hud.Narrate($"{p.Label}'s controller is disconnected; skipping {(u.Owner != null ? "their" : pc.Name + "'s")} turn.");
                    break;
                }
                var actions = BuildActions(u);
                selected = Mathf.Clamp(selected, 0, actions.Count - 1);
                var act = actions[selected];
                var turn = u.Cb.Turn;

                // --- cursor
                var input = p.Input;
                if (input.UsesMouse)
                {
                    var w = p.Cam.ScreenToWorldPoint(input.MouseScreen);
                    var hit = UnitUnderPoint(w);
                    var m = hit != null && hit != u ? hit.Pos : Zone.ToMap(new Vector3(w.x, w.y, 0));
                    if (Zone.InBounds(m)) cursor = m;
                }
                else
                {
                    var nav = input.Nav();
                    if (nav != Vector2Int.zero) cursor += new Vector2Int(nav.x, -nav.y);
                    var rs = input.Cursor;
                    if (rs.sqrMagnitude > 0.5f && Time.frameCount % 8 == 0) cursor += new Vector2Int(Mathf.RoundToInt(rs.x), -Mathf.RoundToInt(rs.y));
                }

                // --- previews
                string hint;
                if (act.Name == "Move")
                {
                    var reach = Reachable(u, turn.MovementLeftFt);
                    Highlight(reach, new Color(0.4f, 0.7f, 1f, 0.5f));
                    var target = UnitAt(cursor);
                    bool attackable = target != null && Hostile(u, target) && target.Active && turn.AttacksLeftInAction + (turn.ActionUsed ? 0 : 1) > 0;
                    ShowCursor(cursor, attackable ? new Color(1f, 0.4f, 0.3f) : reach.Contains(cursor) ? Color.white : new Color(1, 1, 1, 0.35f));
                    hint = attackable ? $"Attack {target.Name} with {pc.GetAttacks()[0].Name}" : reach.Contains(cursor) ? $"Move here ({Zone.DistanceFt(u.Pos, cursor)} ft)" : "Pick a tile to move, or an enemy to attack";
                }
                else
                {
                    var inRange = TilesInRange(u, act);
                    Highlight(inRange, new Color(1f, 0.55f, 0.3f, 0.35f));
                    var area = AreaTiles(u, act, cursor);
                    if (area.Count > 0) foreach (var t in area) { var go = new GameObject("a"); go.transform.position = Zone.ToWorld(t); var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = ArtRegistry.Get("tile_highlight"); sr.color = new Color(1, 0.2f, 0.2f, 0.45f); sr.sortingOrder = -790; _highlights.Add(go); }
                    ShowCursor(cursor, ValidTarget(u, act, cursor) ? new Color(1f, 0.85f, 0.3f) : new Color(1, 1, 1, 0.35f));
                    hint = act.Target == Targeting.None ? $"{act.Name}: press to use" : ValidTarget(u, act, cursor) ? $"{act.Name} → {(UnitAt(cursor)?.Name ?? "here")}" : $"{act.Name}: pick a target in range";
                }
                hud.ShowActions(u, actions, selected, hint);

                // --- input
                int clickedAction = hud.ActionClicked(input);
                int hot = input.HotbarPressed;
                if (hot >= 0 && hot + 1 < actions.Count) clickedAction = hot + 1;
                if (!input.UsesMouse)
                {
                    if (input.NextTab) selected = (selected + 1) % actions.Count;
                    if (input.PrevTab) selected = (selected - 1 + actions.Count) % actions.Count;
                }
                if (clickedAction == -2) { yield return null; continue; }
                if (clickedAction >= 0 && clickedAction < actions.Count)
                {
                    var a = actions[clickedAction];
                    if (!a.Enabled) { p.Hud.Toast(a.Why ?? "Not available.", UIKit.Bad); yield return null; continue; }
                    if (a.Target == Targeting.None) { busy = true; yield return a.Run(u, u.Pos); busy = false; selected = 0; if (a.EndsTurn) break; continue; }
                    selected = clickedAction;
                    yield return null;
                    continue;
                }
                if (input.EndTurn) { ended = true; break; }
                if (input.Cancel || input.MouseRight) { if (!hud.CloseFlyout()) selected = 0; yield return null; continue; }

                bool confirm = input.UsesMouse ? input.MouseLeft && !hud.PointerOverUi(input) : input.Confirm;
                if (input.UsesMouse && input.Confirm) confirm = true;
                if (confirm && !busy)
                {
                    if (act.Name == "Move")
                    {
                        var target = UnitAt(cursor);
                        var atk = actions.FirstOrDefault(x => x.Name.StartsWith("Attack"));
                        if (target != null && Hostile(u, target) && target.Active && atk != null && atk.Enabled)
                        {
                            if (ValidTarget(u, atk, cursor)) yield return atk.Run(u, cursor);
                            else
                            {
                                // Walk next to the target first (BG3 convenience), then attack if possible.
                                var adj = BestAdjacent(u, target, turn.MovementLeftFt);
                                if (adj.HasValue) { yield return MoveTo(u, adj.Value); if (u.Active && ValidTarget(u, atk, cursor)) yield return atk.Run(u, cursor); }
                                else p.Hud.Toast("Can't reach that target this turn.", UIKit.Bad);
                            }
                        }
                        else if (Reachable(u, turn.MovementLeftFt).Contains(cursor)) yield return MoveTo(u, cursor);
                    }
                    else if (act.Enabled)
                    {
                        if (act.Target == Targeting.None) { yield return act.Run(u, u.Pos); if (act.EndsTurn) break; }
                        else if (ValidTarget(u, act, cursor)) { yield return act.Run(u, cursor); selected = 0; }
                        else p.Hud.Toast("Invalid target.", UIKit.Bad);
                    }
                    else p.Hud.Toast(act.Why ?? "Not available.", UIKit.Bad);
                }
                if (CheckEndQuiet()) break;
                yield return null;
            }
            ClearHighlights();
            hud.ShowActions(null, null, 0, null);
        }

        bool CheckEndQuiet()
        {
            bool playersUp = Units.Any(x => x.C.Team == Team.Players && x.Active);
            bool enemiesUp = Units.Any(x => x.C.Team == Team.Enemies && x.Active);
            return !playersUp || !enemiesUp;
        }

        // ================================================================== action list

        List<CombatAction> BuildActions(Unit u)
        {
            var pc = (PlayerCharacter)u.C;
            var t = u.Cb.Turn;
            var list = new List<CombatAction>();
            list.Add(new CombatAction { Name = "Move", Cost = ActionCost.Free, Target = Targeting.Tile });

            var weapon = pc.GetAttacks()[0];
            list.Add(Need(new CombatAction
            {
                Name = $"Attack ({weapon.Name})", Cost = ActionCost.Action, Target = Targeting.Enemy, RangeFt = weapon.Ranged ? weapon.RangeLong : weapon.ReachFt,
                Tooltip = weapon.Summary(), Run = (me, tile) => AttackAction(me, UnitAt(tile), weapon),
            }, t, pc));

            if (pc.IsCaster)
            {
                foreach (var id in pc.Cantrips.Concat(pc.PreparedSpells).Concat(pc.BonusAlwaysPrepared).Distinct())
                {
                    var s = SpellsDb.Get(id);
                    if (s == null || !CombatUsable(s)) continue;
                    var a = SpellAction(u, pc, s);
                    a.Group = ActionGroup.Spells;
                    if (!pc.CanCastSpells) { a.Enabled = false; a.Why = "Beasts can't cast spells. Revert first."; }
                    list.Add(a);
                    if (pc.Metamagic.Contains("quickened") && a.Cost == ActionCost.Action && a.Enabled)
                    {
                        var orig = a.Run;
                        var quick = new CombatAction { Name = a.Name + " (Quickened, 2 pts)", Group = ActionGroup.Spells, Cost = ActionCost.Bonus, Target = a.Target, RangeFt = a.RangeFt,
                            AreaTiles = a.AreaTiles, AreaFromSelf = a.AreaFromSelf, Tooltip = "Metamagic: cast it as a Bonus Action for 2 sorcery points. " + a.Tooltip,
                            Run = (me, tile) => { pc.SpendResource("sorcery_points", 2); me.Cb.Turn.QuickenNextSpell = true; return orig(me, tile); } };
                        list.Add(Need(quick, t, pc, pc.ResourceLeft("sorcery_points") >= 2));
                    }
                }
            }
            if (pc.HasEffect("spiritual_weapon"))
                list.Add(Need(new CombatAction { Name = "Spiritual Weapon strike", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Enemy, RangeFt = 60,
                    Run = (me, tile) => SpellAttack(me, pc, UnitAt(tile), SpellsDb.Get("spiritual_weapon"), 2) }, t, pc));

            if (pc.ResourceMax("second_wind") > 0)
                list.Add(Need(new CombatAction { Name = $"Second Wind ({pc.ResourceLeft("second_wind")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Run = (me, tile) => SecondWind(me, pc) }, t, pc, pc.ResourceLeft("second_wind") > 0));
            if (pc.ResourceMax("action_surge") > 0)
                list.Add(new CombatAction { Name = "Action Surge", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None, Enabled = pc.ResourceLeft("action_surge") > 0 && t.ActionUsed,
                    Why = "Use your action first; once per short rest.", Run = (me, tile) => ActionSurge(me, pc) });
            if (pc.ResourceMax("bardic_inspiration") > 0)
                list.Add(Need(new CombatAction { Name = $"Bardic Inspiration ({pc.ResourceLeft("bardic_inspiration")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Ally, RangeFt = 60,
                    Run = (me, tile) => BardicInspiration(me, pc, UnitAt(tile)) }, t, pc, pc.ResourceLeft("bardic_inspiration") > 0));
            if (pc.ResourceMax("channel_divinity") > 0)
            {
                list.Add(Need(new CombatAction { Name = $"Divine Spark ({pc.ResourceLeft("channel_divinity")})", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.Ally, RangeFt = 30,
                    Tooltip = "Heal an ally 1d8+WIS, or target an enemy for radiant damage.", Run = (me, tile) => DivineSpark(me, pc, UnitAt(tile)) }, t, pc, pc.ResourceLeft("channel_divinity") > 0));
                if (pc.HasFeature("Preserve Life"))
                    list.Add(Need(new CombatAction { Name = "Preserve Life", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.None,
                        Run = (me, tile) => PreserveLife(me, pc) }, t, pc, pc.ResourceLeft("channel_divinity") > 0));
            }

            // Barbarian
            if (pc.ResourceMax("rage") > 0)
                list.Add(Need(new CombatAction { Name = pc.Raging ? "Raging!" : $"Rage ({pc.ResourceLeft("rage")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = $"+{pc.RageDamage} damage on Strength attacks, resist bludgeoning/piercing/slashing, Advantage on Strength checks and saves.",
                    Run = (me, tile) => Rage(me, pc) }, t, pc, pc.ResourceLeft("rage") > 0 && !pc.Raging));
            if (pc.HasFeature("Reckless Attack"))
                list.Add(new CombatAction { Name = "Reckless Attack", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                    Enabled = !pc.HasEffect("reckless") && !t.ActionUsed, Why = pc.HasEffect("reckless") ? "Already reckless this turn." : "Decide before your first attack.",
                    Tooltip = "Advantage on your Strength attacks this turn, but attacks against you have Advantage until your next turn.",
                    Run = (me, tile) => Reckless(me, pc) });
            // Rogue
            if (pc.HasFeature("Cunning Action"))
            {
                list.Add(Need(new CombatAction { Name = "Dash (Cunning)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None, Run = (me, tile) => Simple(me, "Dash", bonus: true) }, t, pc));
                list.Add(Need(new CombatAction { Name = "Disengage (Cunning)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None, Run = (me, tile) => Simple(me, "Disengage", bonus: true) }, t, pc));
                list.Add(Need(new CombatAction { Name = "Hide (Cunning)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Stealth check, DC 15. Success: your next attack has Advantage (they lose track of you).", Run = (me, tile) => Hide(me, pc) }, t, pc));
            }
            // Monk
            if (pc.HasFeature("Martial Arts"))
                list.Add(Need(new CombatAction { Name = "Martial Arts strike", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Enemy, RangeFt = 5,
                    Tooltip = $"Bonus Action: an unarmed strike (1d{pc.MartialArtsDie} + DEX).", Run = (me, tile) => UnarmedBonus(me, pc, UnitAt(tile), 1) }, t, pc));
            if (pc.ResourceMax("focus") > 0)
            {
                bool fp = pc.ResourceLeft("focus") > 0;
                list.Add(Need(new CombatAction { Name = $"Flurry of Blows (1 Focus, {pc.ResourceLeft("focus")} left)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Enemy, RangeFt = 5,
                    Tooltip = "Two unarmed strikes as a Bonus Action." + (pc.HasFeature("Open Hand Technique") ? " Open Hand: each hit can knock the target Prone (DEX save)." : ""),
                    Run = (me, tile) => { pc.SpendResource("focus"); return UnarmedBonus(me, pc, UnitAt(tile), 2); } }, t, pc, fp));
                list.Add(Need(new CombatAction { Name = "Patient Defense (1 Focus)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Disengage and Dodge as a Bonus Action.", Run = (me, tile) => PatientDefense(me, pc) }, t, pc, fp));
                list.Add(Need(new CombatAction { Name = "Step of the Wind (1 Focus)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Dash and Disengage as a Bonus Action.", Run = (me, tile) => StepOfTheWind(me, pc) }, t, pc, fp));
            }
            // Paladin
            if (pc.ResourceMax("lay_on_hands") > 0)
                list.Add(Need(new CombatAction { Name = $"Lay on Hands ({pc.ResourceLeft("lay_on_hands")} HP)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Ally, RangeFt = 5,
                    Tooltip = "Touch: heal from your pool (as much as they need, up to what's left).", Run = (me, tile) => LayOnHands(me, pc, UnitAt(tile)) }, t, pc, pc.ResourceLeft("lay_on_hands") > 0));
            if (pc.KnowsSpell("divine_smite"))
            {
                bool canSmite = pc.ResourceLeft("free_smite") > 0 || pc.LowestAvailableSlot(1) > 0;
                list.Add(Need(new CombatAction { Name = pc.HasEffect("smite_ready") ? "Smite ready!" : "Divine Smite (next hit)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Your next melee weapon hit this turn deals +2d8 radiant (+1d8 per slot level above 1st). Free once per Long Rest, then uses a spell slot.",
                    Run = (me, tile) => ReadySmite(me, pc) }, t, pc, canSmite && !pc.HasEffect("smite_ready")));
            }
            if (pc.HasFeature("Sacred Weapon"))
                list.Add(new CombatAction { Name = "Sacred Weapon", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                    Enabled = pc.ResourceLeft("channel_divinity") > 0 && !pc.HasEffect("sacred_weapon"), Why = "Needs a Channel Divinity use.",
                    Tooltip = $"Channel Divinity: add your CHA modifier ({AbilityUtil.Fmt(Math.Max(1, pc.Mod(Ability.CHA)))}) to weapon attack rolls for 10 minutes.",
                    Run = (me, tile) => SacredWeapon(me, pc) });
            // Sorcerer
            if (pc.ResourceMax("innate_sorcery") > 0)
                list.Add(Need(new CombatAction { Name = $"Innate Sorcery ({pc.ResourceLeft("innate_sorcery")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "For 1 minute: +1 to your spell save DC and Advantage on spell attacks.",
                    Run = (me, tile) => InnateSorcery(me, pc) }, t, pc, pc.ResourceLeft("innate_sorcery") > 0 && !pc.HasEffect("innate_sorcery")));
            if (pc.ResourceMax("sorcery_points") > 0)
            {
                list.Add(new CombatAction { Name = $"Font of Magic: make a level-1 slot (2 of {pc.ResourceLeft("sorcery_points")} pts)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Enabled = !t.BonusActionUsed && pc.ResourceLeft("sorcery_points") >= 2, Why = "Needs 2 sorcery points and your bonus action.",
                    Run = (me, tile) => { me.Cb.Turn.BonusActionUsed = true; if (pc.PointsToSlot(1)) Log($"{pc.Name} shapes raw magic into a spell slot."); return Nothing(); } });
                int lowest = pc.LowestAvailableSlot(1);
                list.Add(new CombatAction { Name = "Font of Magic: slot → sorcery points", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                    Enabled = lowest > 0 && pc.ResourceLeft("sorcery_points") < pc.ResourceMax("sorcery_points"), Why = "Needs a free slot and room for more points.",
                    Run = (me, tile) => { if (pc.SlotToPoints(lowest)) Log($"{pc.Name} unravels a level-{lowest} slot into sorcery points."); return Nothing(); } });
            }
            // Druid
            if (pc.WildShape != null)
                list.Add(Need(new CombatAction { Name = $"Revert to your own form", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Run = (me, tile) => RevertShape(me, pc) }, t, pc));
            else if (pc.ResourceMax("wild_shape") > 0)
                foreach (var form in pc.WildShapeForms())
                {
                    var def = MonstersDb.Get(form);
                    string f = form;
                    list.Add(Need(new CombatAction { Name = $"Wild Shape: {def.Name} ({pc.ResourceLeft("wild_shape")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                        Tooltip = $"{def.Name}: AC {def.AC}, speed {def.Speed} ft, {string.Join(", ", def.Attacks.Select(x => x.Name + " " + x.Damage))}. +{pc.Level} temp HP; no spells while shaped.",
                        Run = (me, tile) => Shapeshift(me, pc, f) }, t, pc, pc.ResourceLeft("wild_shape") > 0));
                }
            if (pc.HasFeature("Land's Aid"))
                list.Add(Need(new CombatAction { Name = "Land's Aid (1 Wild Shape)", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.Tile, RangeFt = 60, AreaTiles = 2,
                    Tooltip = "Thorny burst, 10-ft radius: CON save or 2d6 necrotic (half on a success). One ally in it heals 2d6.",
                    Run = (me, tile) => LandsAid(me, pc, tile) }, t, pc, pc.ResourceLeft("wild_shape") > 0 && pc.WildShape == null));
            if (pc.HasFeature("Steady Aim"))
                list.Add(Need(new CombatAction { Name = "Steady Aim", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "If you haven't moved: Advantage on your next attack this turn; your Speed becomes 0.",
                    Run = (me, tile) => SteadyAim(me, pc) }, t, pc, t.MovementLeftFt >= pc.Speed));

            list.Add(Need(new CombatAction { Name = "Dash", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.None, Run = (me, tile) => Simple(me, "Dash") }, t, pc));
            list.Add(Need(new CombatAction { Name = "Disengage", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.None, Run = (me, tile) => Simple(me, "Disengage") }, t, pc));
            list.Add(Need(new CombatAction { Name = "Dodge", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.None, Run = (me, tile) => Simple(me, "Dodge") }, t, pc));

            if (pc.CountOf("potion_healing") > 0 || pc.CountOf("potion_greater_healing") > 0)
            {
                list.Add(Need(new CombatAction { Name = "Drink Potion", Group = ActionGroup.Items, Cost = ActionCost.Bonus, Target = Targeting.None, Run = (me, tile) => DrinkPotion(me, pc) }, t, pc));
                list.Add(Need(new CombatAction { Name = "Give Potion to Ally", Group = ActionGroup.Items, Cost = ActionCost.Bonus, Target = Targeting.Ally, RangeFt = 5,
                    Tooltip = "Pour a healing potion down an adjacent ally's throat (2024 rules: a Bonus Action, within 5 ft). Works on allies who are down, too.",
                    Run = (me, tile) => GivePotion(me, pc, UnitAt(tile)) }, t, pc));
            }
            if (Units.Any(o => o != u && o.C.Team == Team.Players && o.C.IsDown && !o.C.IsDead))
                list.Add(Need(new CombatAction { Name = "Stabilize (Medicine DC 10)", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.AllyDown, RangeFt = 5,
                    Run = (me, tile) => StabilizeAlly(me, pc, UnitAt(tile)) }, t, pc));
            if ((pc.CountOf("ember_of_return") > 0 || pc.CountOf("scroll_revivify") > 0) && Units.Any(o => o.C.Team == Team.Players && o.C.IsDead))
                list.Add(Need(new CombatAction { Name = "Ember of Return", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.AllyDead, RangeFt = 5,
                    Run = (me, tile) => Revive(me, pc, UnitAt(tile)) }, t, pc));

            list.Add(Need(new CombatAction { Name = "Retreat", Group = ActionGroup.Actions, Cost = ActionCost.Action, Target = Targeting.None,
                Tooltip = "Call a retreat. Roll Athletics or Acrobatics (your best) vs DC 10 + the number of enemies still standing (max 15). Succeed and the whole party breaks away: no XP, but nobody dies. The monsters stay put, wounds and all.",
                Run = (me, tile) => RetreatAttempt(me, pc) }, t, pc));

            if (u.Owner == null)
                list.Add(new CombatAction { Name = "Auto", Group = ActionGroup.End, Cost = ActionCost.Free, Target = Targeting.None, EndsTurn = true,
                    Tooltip = $"Let {pc.Name} decide this turn (they fight on their own). To hand them over for good, talk to them outside of combat.",
                    Run = (me, tile) => AiTurn(me) });
            list.Add(new CombatAction { Name = "End Turn", Group = ActionGroup.End, Cost = ActionCost.Free, Target = Targeting.None, EndsTurn = true, Run = (me, tile) => EndTurnNow(me) });
            return list;
        }

        CombatAction Need(CombatAction a, TurnState t, PlayerCharacter pc, bool extra = true)
        {
            if (a.Cost == ActionCost.Action && t.ActionUsed) { a.Enabled = false; a.Why = "You've used your action."; }
            if (a.Cost == ActionCost.Bonus && t.BonusActionUsed) { a.Enabled = false; a.Why = "You've used your bonus action."; }
            if (!extra) { a.Enabled = false; a.Why = a.Why ?? "No uses left."; }
            return a;
        }

        static bool CombatUsable(SpellDef s) =>
            s.Kind != SpellKind.Utility || s.Id == "spare_the_dying" || s.Id == "misty_step";

        CombatAction SpellAction(Unit u, PlayerCharacter pc, SpellDef s)
        {
            var t = u.Cb.Turn;
            var a = new CombatAction
            {
                Name = s.Name + (s.IsCantrip ? "" : $" (L{s.Level})"),
                Cost = s.CastTime == CastTime.BonusAction ? ActionCost.Bonus : ActionCost.Action,
                RangeFt = Math.Max(5, s.RangeFt),
                Tooltip = s.Description,
            };
            switch (s.Kind)
            {
                case SpellKind.Heal: a.Target = Targeting.Ally; break;
                case SpellKind.Buff: a.Target = s.RangeFt == 0 ? Targeting.None : Targeting.Ally; break;
                case SpellKind.Utility: a.Target = s.Id == "spare_the_dying" ? Targeting.AllyDown : Targeting.Tile; break;
                default:
                    if (s.Shape == TargetShape.Single) a.Target = Targeting.Enemy;
                    else { a.Target = Targeting.Tile; a.AreaTiles = Math.Max(1, s.AreaFt / 5); a.AreaFromSelf = s.RangeFt == 0; if (a.AreaFromSelf) a.RangeFt = s.AreaFt; }
                    break;
            }
            if (s.CastTime == CastTime.Reaction || s.CastTime == CastTime.Minute || s.CastTime == CastTime.TenMinutes) { a.Enabled = false; a.Why = s.CastTime == CastTime.Reaction ? "Reaction: offered automatically when you're hit." : "Takes too long to cast in combat."; }
            if (!s.IsCantrip && pc.LowestAvailableSlot(s.Level) < 0 && !(s.Id == "hunters_mark" && pc.ResourceLeft("favored_enemy") > 0)) { a.Enabled = false; a.Why = "No spell slots left."; }
            if (!s.IsCantrip && t.CastLeveledSpellThisTurn) { a.Enabled = false; a.Why = "Only one spell slot per turn (2024 rules)."; }
            Need(a, t, pc);
            a.Run = (me, tile) => CastSpell(me, pc, s, tile);
            return a;
        }

        // ================================================================== targeting

        List<Vector2Int> TilesInRange(Unit u, CombatAction a)
        {
            var list = new List<Vector2Int>();
            if (a.Target == Targeting.None) return list;
            int r = Math.Max(1, a.RangeFt / 5);
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    var p = u.Pos + new Vector2Int(dx, dy);
                    if (Zone.IsWalkable(p) && (a.RangeFt <= 5 || Zone.LineOfSight(u.Pos, p))) list.Add(p);
                }
            return list;
        }

        List<Vector2Int> AreaTiles(Unit u, CombatAction a, Vector2Int cursor)
        {
            var list = new List<Vector2Int>();
            if (a.AreaTiles <= 0) return list;
            if (a.AreaFromSelf)
            {
                Vector2 dir = ((Vector2)(cursor - u.Pos));
                if (dir.sqrMagnitude < 0.01f) return list;
                dir.Normalize();
                for (int dx = -a.AreaTiles; dx <= a.AreaTiles; dx++)
                    for (int dy = -a.AreaTiles; dy <= a.AreaTiles; dy++)
                    {
                        var off = new Vector2(dx, dy);
                        if (off == Vector2.zero) continue;
                        if (Vector2.Dot(off.normalized, dir) < 0.7f) continue;
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > a.AreaTiles) continue;
                        var p = u.Pos + new Vector2Int(dx, dy);
                        if (Zone.IsWalkable(p)) list.Add(p);
                    }
            }
            else
            {
                if (Zone.DistanceFt(u.Pos, cursor) > a.RangeFt) return list;
                for (int dx = -a.AreaTiles; dx <= a.AreaTiles; dx++)
                    for (int dy = -a.AreaTiles; dy <= a.AreaTiles; dy++)
                    {
                        var p = cursor + new Vector2Int(dx, dy);
                        if (Zone.IsWalkable(p)) list.Add(p);
                    }
            }
            return list;
        }

        bool ValidTarget(Unit u, CombatAction a, Vector2Int tile)
        {
            if (a.Target == Targeting.None) return true;
            int dist = Zone.DistanceFt(u.Pos, tile);
            if (a.Target == Targeting.Tile)
            {
                if (a.AreaFromSelf) return tile != u.Pos && dist <= a.RangeFt;
                return dist <= a.RangeFt && Zone.IsWalkable(tile) && Zone.LineOfSight(u.Pos, tile);
            }
            var t = UnitAt(tile);
            if (t == null || dist > a.RangeFt) return false;
            if (a.RangeFt > 5 && !Zone.LineOfSight(u.Pos, tile)) return false;
            switch (a.Target)
            {
                case Targeting.Enemy: return Hostile(u, t) && !t.C.IsDead;
                case Targeting.Ally: return !Hostile(u, t) && !t.C.IsDead || (a.Name.StartsWith("Divine Spark") && !t.C.IsDead);
                case Targeting.AllyDown: return !Hostile(u, t) && t.C.IsDown && !t.C.IsDead;
                case Targeting.AllyDead: return !Hostile(u, t) && t.C.IsDead;
            }
            return false;
        }

        // ================================================================== movement

        HashSet<Vector2Int> Reachable(Unit u, int feet)
        {
            var result = new HashSet<Vector2Int>();
            var dist = new Dictionary<Vector2Int, int> { [u.Pos] = 0 };
            var q = new Queue<Vector2Int>();
            q.Enqueue(u.Pos);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in dirs)
                {
                    var n = c + d;
                    int cost = dist[c] + 5;
                    if (cost > feet || dist.ContainsKey(n) || !Zone.IsWalkable(n)) continue;
                    if (d.x != 0 && d.y != 0 && (!Zone.IsWalkable(new Vector2Int(c.x + d.x, c.y)) || !Zone.IsWalkable(new Vector2Int(c.x, c.y + d.y)))) continue;
                    var occ = Units.FirstOrDefault(o => o != u && o.Pos == n && !o.C.IsDead);
                    if (occ != null && Hostile(u, occ) && occ.Active) continue; // can't move through enemies
                    dist[n] = cost;
                    q.Enqueue(n);
                    if (occ == null) result.Add(n);
                }
            }
            return result;
        }

        Vector2Int? BestAdjacent(Unit u, Unit target, int feet)
        {
            var reach = Reachable(u, feet);
            reach.Add(u.Pos);
            Vector2Int? best = null;
            int bestCost = int.MaxValue;
            int r = Math.Max(1, (u.C.GetAttacks().FirstOrDefault()?.ReachFt ?? 5) / 5);
            foreach (var p in reach)
            {
                if (Zone.DistanceFt(p, target.Pos) > r * 5) continue;
                int cost = Zone.DistanceFt(u.Pos, p);
                if (cost < bestCost) { bestCost = cost; best = p; }
            }
            return best;
        }

        IEnumerator MoveTo(Unit u, Vector2Int dest)
        {
            var path = Zone.FindPath(u.Pos, dest, p => Units.Any(o => o != u && o.Pos == p && Hostile(u, o) && o.Active));
            var turn = u.Cb.Turn;
            foreach (var step in path)
            {
                if (turn.MovementLeftFt < 5 || !u.Active) break;
                if (Occupied(step, u) && step == dest) break;
                // Opportunity attacks from enemies whose reach we're leaving.
                if (!turn.Disengaged)
                {
                    foreach (var foe in Units.Where(o => o.Active && Hostile(o, u) && !o.Cb.Turn.ReactionUsed && !o.C.IsIncapacitated).ToList())
                    {
                        int reach = foe.C.GetAttacks().Where(a => !a.Ranged).Select(a => a.ReachFt).DefaultIfEmpty(0).Max();
                        if (reach == 0) continue;
                        if (Zone.DistanceFt(foe.Pos, u.Pos) <= reach && Zone.DistanceFt(foe.Pos, step) > reach)
                        {
                            foe.Cb.Turn.ReactionUsed = true;
                            Log($"{foe.Name} makes an opportunity attack!");
                            var melee = foe.C.GetAttacks().First(a => !a.Ranged);
                            if (foe.IsHuman) yield return AttackAction(foe, u, melee, consumeAction: false);
                            else yield return AiAttack(foe, u, melee);
                            if (!u.Active) yield break;
                        }
                    }
                }
                yield return u.A.WalkPath(new List<Vector2Int> { step }, 5f);
                turn.MovementLeftFt -= 5;
            }
            RefreshTracker();
        }

        // ================================================================== attacks (human)

        IEnumerator AttackAction(Unit me, Unit target, AttackOption atk, bool consumeAction = true)
        {
            if (target == null) yield break;
            var turn = me.Cb.Turn;
            if (consumeAction) turn.ActionUsed = true;
            me.A.Face(target.A.transform.position);
            var ctx = Context(me, target);
            var res = AttackResolver.RollAttack(me.C, target.C, atk, ctx, turn);
            if (res.OutOfRange) { me.Ctrl?.Hud.Toast("Out of range!", UIKit.Bad); yield break; }

            // Bardic Inspiration held: add it if it could turn a miss into a hit.
            if (!res.Hit && res.Roll.Natural != 1 && me.C is PlayerCharacter holder && holder.HeldInspirationDie > 0 && res.TargetAC - res.Roll.Total <= holder.HeldInspirationDie)
            {
                int v = Dice.AddBonusDie(res.Roll, holder.HeldInspirationDie);
                holder.HeldInspirationDie = 0;
                res.Hit = res.Roll.Total >= res.TargetAC;
                Log($"{me.Name} uses Bardic Inspiration (+{v})!");
            }
            string adv = res.AdvantageReasons.Count > 0 ? $" [adv: {string.Join(", ", res.AdvantageReasons)}]" : res.DisadvantageReasons.Count > 0 ? $" [dis: {string.Join(", ", res.DisadvantageReasons)}]" : "";
            Ticker($"{me.Name} attacks {target.Name}: {res.Roll.Breakdown()} vs AC {(me.IsHuman ? "?" : res.TargetAC.ToString())} → {(res.Critical ? "CRITICAL HIT!" : res.Hit ? "HIT" : "MISS")}{adv}");
            yield return new WaitForSeconds(0.5f);

            if (res.Hit)
            {
                Sfx.Play(res.Critical ? "crit" : "hit");
                var dmg = AttackResolver.RollDamage(res);
                if (me.C is PlayerCharacter sp && sp.Background.OriginFeat == "Savage Attacker" && !turn.CleaveUsed && !atk.IsSpell)
                {
                    var second = AttackResolver.RollDamage(res);
                    if (second.Total > dmg.Total) { dmg = second; Log("Savage Attacker: you reroll damage and keep the better result."); }
                }
                if (me.IsHuman)
                {
                    bool done = false;
                    me.Ctrl.Hud.Dice.RollDamage(res.Critical ? "CRITICAL! Roll damage!" : "Roll damage!", $"{atk.Name}: {atk.Damage}{(atk.DamageMod != 0 ? AbilityUtil.Fmt(atk.DamageMod) : "")}{(res.Critical ? " (dice doubled)" : "")}", dmg, DieColor.Damage, () => done = true);
                    while (!done) yield return null;
                }
                ApplyHitResults(me, target, res, dmg);
                if (me.C is PlayerCharacter smiter && !atk.Ranged && !atk.IsSpell && target.Active)
                {
                    var ready = smiter.Effects.FirstOrDefault(e => e.Id == "smite_ready");
                    if (ready != null)
                    {
                        smiter.Effects.Remove(ready);
                        var smite = SpellResolver.DivineSmiteDamage(ready.RepeatSaveDC, res.Critical);
                        yield return WaitDamageRoll(me, "Divine Smite!", $"{1 + ready.RepeatSaveDC}d8 radiant{(res.Critical ? " (critical: doubled)" : "")}", smite, DieColor.Damage);
                        var log = new List<string>();
                        int taken = target.C.TakeDamage(smite.Total, DamageType.Radiant, res.Critical, log);
                        target.A.Popup("-" + taken, UIKit.Gold);
                        foreach (var l in log) Log(l);
                    }
                }
                // Cleave mastery: one more attack against a creature next to the first target.
                if (me.C is PlayerCharacter hunter && hunter.HunterPrey == "horde_breaker" && atk.Weapon != null && !turn.HordeBreakerUsed)
                {
                    var second = Units.FirstOrDefault(o => o != target && o.Active && Hostile(me, o) && Zone.DistanceFt(o.Pos, target.Pos) <= 5 && Zone.DistanceFt(o.Pos, me.Pos) <= (atk.Ranged ? atk.RangeNormal : atk.ReachFt));
                    if (second != null)
                    {
                        turn.HordeBreakerUsed = true;
                        Log("Horde Breaker! You turn on a second foe.");
                        yield return AttackAction(me, second, atk, false);
                    }
                }
                if (atk.Mastery == WeaponMastery.Cleave && !turn.CleaveUsed)
                {
                    var next = Units.FirstOrDefault(o => o != target && o.Active && Hostile(me, o) && Zone.DistanceFt(o.Pos, target.Pos) <= 5 && Zone.DistanceFt(o.Pos, me.Pos) <= atk.ReachFt);
                    if (next != null)
                    {
                        turn.CleaveUsed = true;
                        Log("Cleave! Your swing carries into another foe.");
                        var cleave = new AttackOption { Name = atk.Name + " (Cleave)", ToHit = atk.ToHit, Damage = atk.Damage, DamageMod = Math.Min(0, atk.DamageMod), DamageType = atk.DamageType, ReachFt = atk.ReachFt, Ability = atk.Ability };
                        yield return AttackAction(me, next, cleave, false);
                    }
                }
            }
            else
            {
                Sfx.Play("miss");
                target.A.Popup("Miss", Color.white);
                foreach (var l in AttackResolver.ApplyMiss(res)) Log(l);
            }
            RefreshTracker();
        }

        void ApplyHitResults(Unit me, Unit target, AttackRollResult res, DamageRoll dmg)
        {
            int before = target.C.HP + target.C.TempHP;
            bool wasUp = !target.C.IsDown;
            var lines = AttackResolver.ApplyHit(res, dmg);
            foreach (var l in lines) Log(l);
            if (wasUp) OnFoeDropped(me, target);
            // Armor of Agathys: whoever hits the warded creature in melee takes 5 cold.
            if (!res.Attack.Ranged && !res.Attack.IsSpell && target.C.HasEffect("armor_of_agathys") && target.C.TempHP > 0)
            {
                var cold = new List<string>();
                int took = me.C.TakeDamage(5, DamageType.Cold, false, cold);
                me.A.Popup("-" + took, UIKit.UtilityBlue);
                Log($"{me.Name} is bitten by frost ({took} cold).");
            }
            if (target.C.HasEffect("armor_of_agathys") && target.C.TempHP <= 0) target.C.Effects.RemoveAll(e => e.Id == "armor_of_agathys");
            int lost = before - (target.C.HP + target.C.TempHP);
            target.A.Popup((res.Critical ? "CRIT " : "") + "-" + Math.Max(0, lost), res.Critical ? UIKit.D20Gold : UIKit.DamageRed);
            OnDamaged(target);
        }

        void OnDamaged(Unit target)
        {
            if (target.C.IsDead && target.DeathRound < 0) target.DeathRound = Enc.Round;
            if (target.C.IsDown && target.C.Team == Team.Enemies) { target.A.Popup("Defeated!", UIKit.Gold); }
            if (target.C.IsDown && target.Owner != null) target.Owner.Hud.Narrate(target.C.IsDead ? "Everything goes dark..." : "You fall unconscious! Death saves begin on your turn...");
            else if (target.C.IsDown && target.Comp != null) target.Comp.Leader?.Hud.Narrate(target.C.IsDead ? $"{target.Name} has fallen..." : $"{target.Name} is down! Get a potion or a healing spell to them (Items: Give Potion to Ally).");
        }

        // ================================================================== spells

        IEnumerator CastSpell(Unit me, PlayerCharacter pc, SpellDef s, Vector2Int tile)
        {
            var turn = me.Cb.Turn;
            int slot = s.IsCantrip ? 0 : pc.LowestAvailableSlot(s.Level);
            if (!SpellResolver.PaySlot(pc, s, slot)) { me.Ctrl?.Hud.Toast("No slot available.", UIKit.Bad); yield break; }
            if (s.CastTime == CastTime.BonusAction || turn.QuickenNextSpell) { turn.BonusActionUsed = true; turn.QuickenNextSpell = false; }
            else turn.ActionUsed = true;
            if (!s.IsCantrip) turn.CastLeveledSpellThisTurn = true;
            var log = new List<string>();
            SpellResolver.StartConcentration(pc, s, log);
            if (log.Count > 0) Enc.EndConcentration(pc);
            if (s.Concentration) pc.ConcentratingOn = s.Id;
            Sfx.Play("spell");
            Log($"{pc.Name} casts {s.Name}{(slot > 0 ? $" (level {slot} slot)" : "")}!");
            me.A.Face(Zone.ToWorld(tile));
            var target = UnitAt(tile);

            switch (s.Kind)
            {
                case SpellKind.Attack:
                    for (int i = 0; i < s.Projectiles; i++)
                    {
                        if (target == null || !target.Active) target = Units.FirstOrDefault(o => o.Active && Hostile(me, o) && Zone.DistanceFt(me.Pos, o.Pos) <= s.RangeFt);
                        if (target == null) break;
                        yield return SpellAttack(me, pc, target, s, slot);
                    }
                    break;

                case SpellKind.AutoHit:
                {
                    int darts = s.Projectiles + Math.Max(0, slot - s.Level);
                    var expr = new DiceExpr(darts, 4, darts);
                    var dmg = Dice.RollDamage(expr);
                    yield return WaitDamageRoll(me, $"{s.Name}: {darts} darts", $"{darts} × (1d4+1) force: never misses", dmg, DieColor.Damage);
                    var lines = new List<string>();
                    int lost = target.C.TakeDamage(dmg.Total, s.DamageType, false, lines);
                    lines.ForEach(Log);
                    target.A.Popup("-" + lost, UIKit.DamageRed);
                    if (lost > 0) AttackResolver.RaiseConcentrationCheck(target.C, lost);
                    OnDamaged(target);
                    break;
                }

                case SpellKind.Save:
                {
                    var targets = new List<Unit>();
                    var area = AreaTiles(me, new CombatAction { AreaTiles = s.Shape == TargetShape.Single ? 0 : Math.Max(1, s.AreaFt / 5), AreaFromSelf = s.RangeFt == 0, RangeFt = s.RangeFt == 0 ? s.AreaFt : s.RangeFt }, tile);
                    if (s.Shape == TargetShape.Single) { if (target != null) targets.Add(target); }
                    else targets.AddRange(Units.Where(o => !o.C.IsDead && area.Contains(o.Pos) && o != me));
                    DamageRoll dmg = null;
                    if (!s.Dice.IsNone)
                    {
                        dmg = SpellResolver.RollEffect(pc, s, Math.Max(slot, s.Level), false);
                        yield return WaitDamageRoll(me, s.Name, $"{SpellResolver.EffectDice(pc, s, Math.Max(slot, s.Level))} {s.DamageType.ToString().ToLower()} · {s.SaveAbility} save DC {pc.SpellSaveDC}", dmg, DieColor.Damage);
                    }
                    foreach (var t in targets)
                    {
                        var save = SpellResolver.TargetSave(pc, t.C, s);
                        bool saved = Checks.ResolveSave(save, pc.SpellSaveDC);
                        Ticker($"{t.Name} {s.SaveAbility} save: {save.Breakdown()} vs DC {pc.SpellSaveDC} → {(saved ? "SAVED" : "FAILED")}");
                        yield return new WaitForSeconds(0.45f);
                        if (dmg != null)
                        {
                            var lines = new List<string>();
                            int before = t.C.HP;
                            SpellResolver.ApplySaveDamage(pc, t.C, s, dmg, saved, lines);
                            lines.ForEach(Log);
                            t.A.Popup("-" + Math.Max(0, before - t.C.HP), UIKit.DamageRed);
                            OnDamaged(t);
                        }
                        if (!saved && s.EffectId != null && !t.C.IsDown) ApplySpellEffect(me, t, s);
                    }
                    break;
                }

                case SpellKind.Heal:
                {
                    var dmg = SpellResolver.RollEffect(pc, s, Math.Max(slot, s.Level), false);
                    yield return WaitDamageRoll(me, s.Name, $"Healing {target.Name}", dmg, DieColor.Healing);
                    bool wasDown = target.C.IsDown;
                    int h = target.C.Heal(dmg.Total);
                    target.A.Popup("+" + h, UIKit.HealGreen);
                    Sfx.Play("heal");
                    Log($"{target.Name} regains {h} HP{(wasDown ? " and gets back up!" : ".")}");
                    break;
                }

                case SpellKind.Mark:
                    if (target != null)
                    {
                        // A mark moves with you: drop any mark you've placed elsewhere first.
                        foreach (var o in Units) o.C.Effects.RemoveAll(e => e.Id == s.EffectId && e.Source == pc);
                        ApplySpellEffect(me, target, s);
                    }
                    break;

                case SpellKind.Buff:
                {
                    var recipients = new List<Unit>();
                    if (target != null) recipients.Add(target); else recipients.Add(me);
                    if (s.MaxTargets > 1)
                        recipients.AddRange(Units.Where(o => !recipients.Contains(o) && !Hostile(me, o) && !o.C.IsDead && Zone.DistanceFt(me.Pos, o.Pos) <= s.RangeFt).OrderBy(o => Zone.DistanceFt(me.Pos, o.Pos)).Take(s.MaxTargets - 1));
                    foreach (var r in recipients)
                    {
                        var fx = EffectLibrary.Create(s.EffectId, pc);
                        r.C.AddEffect(fx);
                        if (s.Id == "aid") r.C.Heal(5);
                        if (s.Id == "armor_of_agathys") r.C.TempHP = Math.Max(r.C.TempHP, 5 * Math.Max(1, slot));
                        r.A.Popup(fx.Name, UIKit.UtilityBlue);
                        Log($"{r.Name} is affected by {s.Name}.");
                    }
                    break;
                }

                case SpellKind.Utility:
                    if (s.Id == "spare_the_dying" && target?.C is PlayerCharacter down) { down.Stabilize(); target.A.Popup("Stable", UIKit.HealGreen); Log($"{target.Name} is stabilized."); }
                    else if (s.Id == "misty_step" && !Occupied(tile)) { me.A.PlaceAt(Zone, tile); me.A.Popup("*poof*", UIKit.UtilityBlue); }
                    break;
            }
            RefreshTracker();
        }

        IEnumerator SpellAttack(Unit me, PlayerCharacter pc, Unit target, SpellDef s, int slot)
        {
            var atk = SpellResolver.AsAttack(pc, s);
            atk.Damage = SpellResolver.EffectDice(pc, s, Math.Max(slot, s.Level));
            if (s.AddSpellMod) atk.DamageMod = pc.SpellMod;
            var ctx = Context(me, target);
            var res = AttackResolver.RollAttack(pc, target.C, atk, ctx);
            if (res.OutOfRange) yield break;
            Ticker($"{s.Name} → {target.Name}: {res.Roll.Breakdown()} → {(res.Critical ? "CRITICAL HIT!" : res.Hit ? "HIT" : "MISS")}");
            yield return new WaitForSeconds(0.45f);
            if (res.Hit)
            {
                var dmg = AttackResolver.RollDamage(res);
                yield return WaitDamageRoll(me, res.Critical ? $"CRITICAL {s.Name}!" : s.Name, $"{atk.Damage} {s.DamageType.ToString().ToLower()}", dmg, DieColor.Damage);
                ApplyHitResults(me, target, res, dmg);
                if (s.EffectId != null && !target.C.IsDown) ApplySpellEffect(me, target, s);
                if (s.Id == "eldritch_blast" && pc.Invocations.Contains("repelling_blast") && target.Active) Push(me, target, 2);
            }
            else
            {
                target.A.Popup("Miss", Color.white);
                if (s.IsCantrip && pc.HasFeature("Potent Cantrip"))
                {
                    var dmg = SpellResolver.RollEffect(pc, s, 0, false);
                    var lines = new List<string>();
                    int lost = target.C.TakeDamage(dmg.Total / 2, s.DamageType, false, lines);
                    Log($"Potent Cantrip: {target.Name} still takes {lost} damage.");
                    OnDamaged(target);
                }
            }
        }

        void ApplySpellEffect(Unit me, Unit t, SpellDef s)
        {
            if (s.EffectId == "push10") { Push(me, t, 2); return; }
            var fx = EffectLibrary.Create(s.EffectId, me.C, (me.C as PlayerCharacter)?.SpellSaveDC ?? 10);
            if (s.EffectId == "laughter") t.C.AddEffect(EffectLibrary.Create("prone", me.C));
            t.C.AddEffect(fx);
            t.A.Popup(fx.Name, UIKit.UtilityBlue);
            Log($"{t.Name} is {fx.Name.ToLower()}!");
        }

        public void Push(Unit from, Unit t, int tiles)
        {
            var dir = t.Pos - from.Pos;
            dir = new Vector2Int(Math.Sign(dir.x), Math.Sign(dir.y));
            if (dir == Vector2Int.zero) return;
            var dest = t.Pos;
            for (int i = 0; i < tiles; i++)
            {
                var n = dest + dir;
                if (!Zone.IsWalkable(n) || Occupied(n, t)) break;
                dest = n;
            }
            if (dest != t.Pos) { t.A.PlaceAt(Zone, dest); Log($"{t.Name} is pushed back!"); }
        }

        IEnumerator WaitDamageRoll(Unit me, string title, string sub, DamageRoll roll, DieColor color)
        {
            if (!me.IsHuman)
            {
                Ticker($"{me.Name}: {title}: {roll.Breakdown()}");
                yield return new WaitForSeconds(0.6f);
                yield break;
            }
            bool done = false;
            me.Ctrl.Hud.Dice.RollDamage(title, sub, roll, color, () => done = true);
            while (!done) yield return null;
        }

        // ================================================================== features & basic actions

        static IEnumerator Nothing() { yield break; }

        IEnumerator InnateSorcery(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("innate_sorcery");
            me.Cb.Turn.BonusActionUsed = true;
            pc.AddEffect(EffectLibrary.Create("innate_sorcery", pc));
            me.A.Popup("Innate Sorcery", UIKit.UtilityBlue);
            Log($"{pc.Name}'s blood burns with draconic magic (+1 spell DC, Advantage on spell attacks).");
            yield break;
        }

        /// <summary>Things that happen when a hero drops a foe to 0 HP (Ember's Blessing).</summary>
        void OnFoeDropped(Unit me, Unit target)
        {
            if (!(me.C is PlayerCharacter pc) || !target.C.IsDown || !Hostile(me, target)) return;
            var log = new List<string>();
            pc.OnReducedHostileToZero(log);
            foreach (var l in log) Log(l);
        }

        IEnumerator Shapeshift(Unit me, PlayerCharacter pc, string form)
        {
            var log = new List<string>();
            if (!pc.StartWildShape(form, log)) yield break;
            me.Cb.Turn.BonusActionUsed = true;
            me.Cb.Turn.MovementLeftFt = Math.Min(me.Cb.Turn.MovementLeftFt, pc.Speed);
            WildShapeFx.Transform(me.A, pc);
            foreach (var l in log) Log(l);
        }

        IEnumerator RevertShape(Unit me, PlayerCharacter pc)
        {
            var log = new List<string>();
            pc.EndWildShape(log);
            me.Cb.Turn.BonusActionUsed = true;
            WildShapeFx.Transform(me.A, pc);
            foreach (var l in log) Log(l);
            yield break;
        }

        IEnumerator LandsAid(Unit me, PlayerCharacter pc, Vector2Int tile)
        {
            if (!pc.SpendResource("wild_shape")) yield break;
            me.Cb.Turn.ActionUsed = true;
            var inArea = Units.Where(o => o.Active && Zone.DistanceFt(o.Pos, tile) <= 10).ToList();
            var dmg = Dice.RollDamage(new DiceExpr(2, 6));
            yield return WaitDamageRoll(me, "Land's Aid", "2d6 necrotic · CON save", dmg, DieColor.Damage);
            foreach (var o in inArea.Where(o => Hostile(me, o)))
            {
                var save = Checks.RollSave(o.C, Ability.CON, AdvantageState.Normal);
                bool ok = save.Total >= pc.SpellSaveDC;
                var log = new List<string>();
                int lost = o.C.TakeDamage(ok ? dmg.Total / 2 : dmg.Total, DamageType.Necrotic, false, log);
                o.A.Popup("-" + lost, UIKit.DamageRed);
                Log($"{o.Name} {(ok ? "resists" : "is caught by")} the thorns (CON {save.Total} vs DC {pc.SpellSaveDC}): {lost} damage.");
                OnDamaged(o);
            }
            var friend = inArea.Where(o => !Hostile(me, o) && !o.C.IsDead).OrderBy(o => o.C.HP / (float)Math.Max(1, o.C.EffectiveMaxHP)).FirstOrDefault();
            if (friend != null)
            {
                var heal = Dice.RollDamage(new DiceExpr(2, 6));
                yield return WaitDamageRoll(me, "Land's Aid", $"2d6 healing for {friend.Name}", heal, DieColor.Healing);
                int h = friend.C.Heal(heal.Total);
                friend.A.Popup("+" + h, UIKit.HealGreen);
                Log($"{friend.Name} is restored by the land (+{h} HP).");
            }
        }

        IEnumerator UnarmedBonus(Unit me, PlayerCharacter pc, Unit target, int strikes)
        {
            if (target == null) yield break;
            me.Cb.Turn.BonusActionUsed = true;
            for (int i = 0; i < strikes && target.Active; i++)
            {
                int hpBefore = target.C.HP;
                yield return AttackAction(me, target, pc.UnarmedStrike(), consumeAction: false);
                // Open Hand Technique: a Flurry hit can knock the target down.
                if (strikes == 2 && pc.HasFeature("Open Hand Technique") && target.Active && target.C.HP < hpBefore && !target.C.Has(Condition.Prone))
                {
                    int dc = 8 + pc.ProficiencyBonus + pc.Mod(Ability.WIS);
                    var save = Checks.RollSave(target.C, Ability.DEX, AdvantageState.Normal);
                    Ticker($"Open Hand: {target.Name} DEX save {save.Breakdown()} vs DC {dc}");
                    if (save.Total < dc) { target.C.AddEffect(EffectLibrary.Create("prone", pc)); target.A.Popup("Prone!", UIKit.Gold); Log($"{target.Name} is knocked Prone!"); }
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }

        IEnumerator PatientDefense(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("focus");
            var t = me.Cb.Turn;
            t.BonusActionUsed = true;
            t.Disengaged = true;
            pc.AddEffect(EffectLibrary.Create("dodge", pc));
            me.A.Popup("Patient Defense", UIKit.UtilityBlue);
            Log($"{pc.Name} slips into a defensive stance (Disengage + Dodge).");
            yield break;
        }

        IEnumerator StepOfTheWind(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("focus");
            var t = me.Cb.Turn;
            t.BonusActionUsed = true;
            t.Disengaged = true;
            t.MovementLeftFt += pc.Speed;
            me.A.Popup("Step of the Wind", UIKit.UtilityBlue);
            Log($"{pc.Name} moves like the wind (Dash + Disengage).");
            yield break;
        }

        IEnumerator LayOnHands(Unit me, PlayerCharacter pc, Unit target)
        {
            if (target == null) yield break;
            int need = target.C.EffectiveMaxHP - target.C.HP;
            int amount = Math.Min(need, pc.ResourceLeft("lay_on_hands"));
            if (amount <= 0) { me.Ctrl?.Hud.Toast($"{target.Name} is already at full health.", UIKit.Dim); yield break; }
            me.Cb.Turn.BonusActionUsed = true;
            pc.SpendResource("lay_on_hands", amount);
            int h = target.C.Heal(amount);
            target.A.Popup("+" + h, UIKit.HealGreen);
            Sfx.Play("heal");
            Log($"{pc.Name} lays hands on {target.Name}: +{h} HP ({pc.ResourceLeft("lay_on_hands")} left in the pool).");
        }

        IEnumerator ReadySmite(Unit me, PlayerCharacter pc)
        {
            int slot = 1;
            if (pc.ResourceLeft("free_smite") > 0) pc.SpendResource("free_smite");
            else
            {
                slot = pc.LowestAvailableSlot(1);
                if (slot <= 0) yield break;
                pc.SlotsUsed[slot]++;
            }
            me.Cb.Turn.BonusActionUsed = true;
            pc.AddEffect(new ActiveEffect { Id = "smite_ready", Name = "Smite ready", Source = pc, RoundsLeft = 1, AttackDie = 0, RepeatSaveDC = slot });
            me.A.Popup("Smite ready!", UIKit.Gold);
            Log($"{pc.Name}'s weapon glows with radiant light. (Divine Smite, level {slot})");
        }

        IEnumerator SacredWeapon(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("channel_divinity");
            pc.AddEffect(EffectLibrary.Create("sacred_weapon", pc));
            me.A.Popup("Sacred Weapon", UIKit.Gold);
            Log($"{pc.Name} raises their weapon in prayer: +{Math.Max(1, pc.Mod(Ability.CHA))} to attack rolls.");
            yield break;
        }

        IEnumerator Rage(Unit me, PlayerCharacter pc)
        {
            me.Cb.Turn.BonusActionUsed = true;
            var log = new List<string>();
            if (pc.StartRage(log))
            {
                me.A.Popup("RAGE!", UIKit.DamageRed);
                Sfx.Play("combat_start");
            }
            foreach (var l in log) Log(l);
            yield break;
        }

        IEnumerator Reckless(Unit me, PlayerCharacter pc)
        {
            pc.AddEffect(EffectLibrary.Create("reckless", pc));
            me.A.Popup("Reckless!", UIKit.Gold);
            Log($"{pc.Name} attacks recklessly. (Advantage on Strength attacks; enemies get Advantage too.)");
            yield break;
        }

        IEnumerator Hide(Unit me, PlayerCharacter pc)
        {
            me.Cb.Turn.BonusActionUsed = true;
            D20Result res = null;
            if (me.IsHuman) me.Ctrl.Hud.Dice.RollD20("Hide", "Stealth · DC 15", () => Checks.RollSkill(pc, Skill.Stealth, AdvantageState.Normal), 15, true, r => res = r);
            else res = Checks.RollSkill(pc, Skill.Stealth, AdvantageState.Normal);
            while (res == null) yield return null;
            if (res.Total >= 15)
            {
                pc.AddEffect(new ActiveEffect { Id = "hidden", Name = "Hidden", Source = pc, RoundsLeft = 2, AdvantageOnNextAttack = true });
                me.A.Popup("Hidden", UIKit.UtilityBlue);
                Log($"{pc.Name} slips out of sight ({res.Total}). Their next attack has Advantage.");
            }
            else Log($"{pc.Name} tries to hide, but they spot you ({res.Total}).");
        }

        IEnumerator SteadyAim(Unit me, PlayerCharacter pc)
        {
            var t = me.Cb.Turn;
            t.BonusActionUsed = true;
            t.MovementLeftFt = 0;
            pc.AddEffect(EffectLibrary.Create("steady_aim", pc));
            me.A.Popup("Steady...", UIKit.UtilityBlue);
            Log($"{pc.Name} takes careful aim (Advantage on the next attack).");
            yield break;
        }

        IEnumerator SecondWind(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("second_wind");
            me.Cb.Turn.BonusActionUsed = true;
            var roll = Dice.RollDamage(new DiceExpr(1, 10, pc.Level));
            yield return WaitDamageRoll(me, "Second Wind", $"1d10 + {pc.Level}", roll, DieColor.Healing);
            int h = pc.Heal(roll.Total);
            me.A.Popup("+" + h, UIKit.HealGreen);
            Log($"{pc.Name} catches a second wind (+{h} HP).");
        }

        IEnumerator ActionSurge(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("action_surge");
            me.Cb.Turn.ActionUsed = false;
            me.A.Popup("ACTION SURGE!", UIKit.D20Gold);
            Log($"{pc.Name} surges forward with a burst of energy: another action!");
            yield break;
        }

        IEnumerator BardicInspiration(Unit me, PlayerCharacter pc, Unit target)
        {
            if (target == null || target == me) { me.Ctrl?.Hud.Toast("Inspire someone else!", UIKit.Bad); yield break; }
            pc.SpendResource("bardic_inspiration");
            me.Cb.Turn.BonusActionUsed = true;
            if (target.C is PlayerCharacter t) t.HeldInspirationDie = pc.BardicDie;
            target.A.Popup($"Inspired! d{pc.BardicDie}", UIKit.UtilityBlue);
            Log($"{pc.Name} inspires {target.Name} (d{pc.BardicDie}).");
            target.Ctrl?.Hud.Toast($"You hold a Bardic Inspiration d{pc.BardicDie}! It's offered when you fail a roll.", UIKit.UtilityBlue);
        }

        IEnumerator DivineSpark(Unit me, PlayerCharacter pc, Unit target)
        {
            if (target == null) yield break;
            pc.SpendResource("channel_divinity");
            me.Cb.Turn.ActionUsed = true;
            var roll = Dice.RollDamage(new DiceExpr(1, 8, pc.Mod(Ability.WIS)));
            if (!Hostile(me, target))
            {
                yield return WaitDamageRoll(me, "Divine Spark", $"Healing {target.Name}", roll, DieColor.Healing);
                int h = target.C.Heal(roll.Total);
                target.A.Popup("+" + h, UIKit.HealGreen);
                Log($"Divine light mends {target.Name} (+{h}).");
            }
            else
            {
                yield return WaitDamageRoll(me, "Divine Spark", $"Radiant · CON save DC {pc.SpellSaveDC} for half", roll, DieColor.Damage);
                var save = Checks.RollSave(target.C, Ability.CON, AdvantageState.Normal);
                bool saved = Checks.ResolveSave(save, pc.SpellSaveDC);
                Ticker($"{target.Name} CON save: {save.Breakdown()} → {(saved ? "half" : "full")}");
                var lines = new List<string>();
                int lost = target.C.TakeDamage(saved ? roll.Total / 2 : roll.Total, DamageType.Radiant, false, lines);
                target.A.Popup("-" + lost, UIKit.DamageRed);
                OnDamaged(target);
            }
        }

        IEnumerator PreserveLife(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("channel_divinity");
            me.Cb.Turn.ActionUsed = true;
            int pool = 5 * pc.Level;
            foreach (var a in Units.Where(o => !Hostile(me, o) && !o.C.IsDead && Zone.DistanceFt(me.Pos, o.Pos) <= 30).OrderBy(o => o.C.HP))
            {
                int cap = a.C.EffectiveMaxHP / 2 - a.C.HP;
                if (cap <= 0 || pool <= 0) continue;
                int h = a.C.Heal(Math.Min(cap, pool));
                pool -= h;
                a.A.Popup("+" + h, UIKit.HealGreen);
            }
            Log($"{pc.Name} channels the Dawnmother's light: Preserve Life!");
            Sfx.Play("heal");
            yield break;
        }

        IEnumerator Simple(Unit me, string what, bool bonus = false)
        {
            var t = me.Cb.Turn;
            if (bonus) t.BonusActionUsed = true; else t.ActionUsed = true;
            switch (what)
            {
                case "Dash": t.MovementLeftFt += me.C.Speed; break;
                case "Disengage": t.Disengaged = true; break;
                case "Dodge": me.C.AddEffect(EffectLibrary.Create("dodge", me.C)); break;
            }
            me.A.Popup(what, Color.white);
            Log($"{me.Name} takes the {what} action.");
            yield break;
        }

        IEnumerator DrinkPotion(Unit me, PlayerCharacter pc)
        {
            string id = pc.CountOf("potion_healing") > 0 ? "potion_healing" : "potion_greater_healing";
            var def = ItemsDb.Get<ConsumableDef>(id);
            pc.RemoveItem(id);
            me.Cb.Turn.BonusActionUsed = true;
            var roll = Dice.RollDamage(def.Amount);
            yield return WaitDamageRoll(me, def.Name, def.Amount.ToString(), roll, DieColor.Healing);
            int h = pc.Heal(roll.Total);
            me.A.Popup("+" + h, UIKit.HealGreen);
            Log($"{pc.Name} drinks a potion (+{h} HP).");
        }

        IEnumerator StabilizeAlly(Unit me, PlayerCharacter pc, Unit target)
        {
            if (!(target?.C is PlayerCharacter down)) yield break;
            me.Cb.Turn.ActionUsed = true;
            D20Result res = null;
            if (me.IsHuman) me.Ctrl.Hud.Dice.RollD20("Medicine Check", $"Stabilize {target.Name} · DC 10", () => Checks.RollSkill(pc, Skill.Medicine, AdvantageState.Normal), 10, true, r => res = r);
            else res = Checks.RollSkill(pc, Skill.Medicine, AdvantageState.Normal);
            while (res == null) yield return null;
            if (Checks.IsSuccess(Checks.ResolveCheck(res, 10))) { down.Stabilize(); target.A.Popup("Stable", UIKit.HealGreen); Log($"{target.Name} is stabilized."); }
            else Log($"{pc.Name} fails to stabilize {target.Name}.");
        }

        IEnumerator Revive(Unit me, PlayerCharacter pc, Unit target)
        {
            if (!(target?.C is PlayerCharacter dead) || !dead.IsDead) yield break;
            var diff = G.Settings.Difficulty;
            if (diff >= Difficulty.Hard && target.DeathRound >= 0 && Enc.Round - target.DeathRound > 10)
            { me.Ctrl?.Hud.Toast("Too late. More than a minute has passed. (Hard difficulty)", UIKit.Bad); yield break; }
            string item = pc.CountOf("ember_of_return") > 0 ? "ember_of_return" : "scroll_revivify";
            pc.RemoveItem(item);
            me.Cb.Turn.ActionUsed = true;
            dead.Revive(diff >= Difficulty.Hard ? 1 : 0, null);
            target.DeathRound = -1;
            target.A.Popup("Returned!", UIKit.D20Gold);
            Log($"{pc.Name} presses the {ItemsDb.Get(item).Name} to {target.Name}'s heart. They gasp back to life!");
            Sfx.Play("heal");
        }

        IEnumerator GivePotion(Unit me, PlayerCharacter pc, Unit target)
        {
            if (target == null || target.C.IsDead) yield break;
            if (target == me) { yield return DrinkPotion(me, pc); yield break; }
            string id = pc.CountOf("potion_greater_healing") > 0 ? "potion_greater_healing" : "potion_healing";
            if (!pc.RemoveItem(id)) yield break;
            me.Cb.Turn.BonusActionUsed = true;
            var def = (ConsumableDef)ItemsDb.Get(id);
            me.A.Face(target.A.transform.position);
            var roll = Dice.RollDamage(def.Amount);
            yield return WaitDamageRoll(me, def.Name, $"{pc.Name} tips it into {target.Name}'s mouth", roll, DieColor.Healing);
            bool wasDown = target.C.IsDown;
            int healed = target.C.Heal(roll.Total);
            target.A.Popup("+" + healed, UIKit.HealGreen);
            Sfx.Play("heal");
            Ticker($"{pc.Name} gives {target.Name} a {def.Name}: +{healed} HP{(wasDown ? $". {target.Name} is back on their feet!" : "")}");
            RefreshTracker();
        }

        IEnumerator RetreatAttempt(Unit me, PlayerCharacter pc)
        {
            me.Cb.Turn.ActionUsed = true;
            int foes = Units.Count(o => o.Active && Hostile(o, me));
            int dc = Math.Min(15, 10 + foes);
            var skill = pc.SkillBonus(Skill.Athletics) >= pc.SkillBonus(Skill.Acrobatics) ? Skill.Athletics : Skill.Acrobatics;
            D20Result res = null;
            me.Ctrl.Hud.Dice.RollD20("Retreat!", $"{SkillUtil.Name(skill)} {AbilityUtil.Fmt(pc.SkillBonus(skill))} · DC {dc} ({foes} enemies standing)",
                () => Checks.RollSkill(pc, skill, AdvantageState.Normal), dc, true, r => res = r);
            while (res == null) yield return null;
            if (Checks.IsSuccess(Checks.ResolveCheck(res, dc)))
            {
                Ticker($"{pc.Name} shouts \"FALL BACK!\" and the party breaks away!");
                yield return new WaitForSeconds(0.6f);
                Retreat();
            }
            else Ticker($"{pc.Name} looks for a way out, but the enemy cuts off the escape!");
        }

        IEnumerator EndTurnNow(Unit me) { me.Cb.Turn.ActionUsed = true; me.Cb.Turn.BonusActionUsed = true; me.Cb.Turn.MovementLeftFt = 0; yield break; }

        // ================================================================== death saves

        IEnumerator DeathSave(Unit u, PlayerCharacter pc)
        {
            D20Result res = null;
            if (u.IsHuman)
                u.Ctrl.Hud.Dice.RollD20("Death Saving Throw", $"10+ succeeds · {pc.DeathSuccesses} successes, {pc.DeathFailures} failures", pc.RollDeathSave, 10, false, r => res = r);
            else res = pc.RollDeathSave();
            while (res == null) yield return null;
            var log = new List<string>();
            var outcome = pc.ApplyDeathSave(res, log);
            log.ForEach(Log);
            Ticker($"{pc.Name} death save: {res.Natural} → {outcome}");
            if (outcome == PlayerCharacter.DeathSaveOutcome.Died) { u.DeathRound = Enc.Round; u.Ctrl?.Hud.Narrate(u.Owner != null ? "Your spirit slips free of your body. Your friends can still bring you back..." : $"{u.Name}'s spirit slips free. An Ember of Return or the temple can still bring them back..."); }
            if (outcome == PlayerCharacter.DeathSaveOutcome.Revived) { u.A.Popup("NAT 20! Back up!", UIKit.D20Gold); Sfx.Play("crit"); }
            yield return new WaitForSeconds(0.4f);
        }

        // ================================================================== AI (monsters and companions)

        IEnumerator AiTurn(Unit u)
        {
            yield return new WaitForSeconds(0.35f);
            var turn = u.Cb.Turn;
            var foes = Units.Where(o => Hostile(u, o) && o.Active).ToList();
            if (foes.Count == 0) yield break;

            // Companion healer: pick up a fallen friend first.
            if (u.C is PlayerCharacter cpc)
            {
                var down = Units.FirstOrDefault(o => !Hostile(u, o) && o.C.IsDown && !o.C.IsDead);
                var hw = SpellsDb.Get("healing_word");
                if (down != null && cpc.KnowsSpell("healing_word") && cpc.LowestAvailableSlot(1) > 0 && Zone.DistanceFt(u.Pos, down.Pos) <= 60)
                {
                    yield return CastSpell(u, cpc, hw, down.Pos);
                    turn.ActionUsed = false; // Healing Word is a bonus action
                }
            }

            var monster = u.C as Monster;
            bool coward = monster != null && (monster.Def.Behavior == MonsterBehavior.Coward || monster.HP <= monster.MaxHP * 0.3f) && monster.Def.Id.StartsWith("kobold") && UnityEngine.Random.value < 0.5f;
            if (coward)
            {
                var far = Reachable(u, turn.MovementLeftFt).OrderByDescending(p => foes.Min(f => Zone.DistanceFt(p, f.Pos))).FirstOrDefault();
                if (monster.Def.NimbleEscape) turn.Disengaged = true;
                Log($"{u.Name} panics and scrambles away!");
                yield return MoveTo(u, far);
                yield break;
            }

            // Choose attack style.
            var attacks = u.C.GetAttacks();
            AttackOption ranged = attacks.FirstOrDefault(a => a.Ranged);
            AttackOption melee = attacks.FirstOrDefault(a => !a.Ranged);
            if (u.C is PlayerCharacter caster && caster.IsCaster)
            {
                var cantrip = caster.Cantrips.Select(SpellsDb.Get).FirstOrDefault(s => s != null && s.Kind == SpellKind.Attack && s.RangeFt > 5);
                if (cantrip != null) ranged = SpellResolver.AsAttack(caster, cantrip);
            }
            bool frontline = u.Comp != null && u.Comp.IsFrontline;
            bool prefersRange = ranged != null && (monster != null ? monster.Def.Behavior == MonsterBehavior.Ranged || melee == null : !frontline || melee == null);

            var target = foes.OrderBy(f => Zone.DistanceFt(u.Pos, f.Pos)).First();
            if (frontline)
            {
                // Bodyguard: go for whichever enemy is closest to the most hurt friend (usually whoever is being swarmed).
                var ward = Units.Where(o => o != u && o.Active && !Hostile(u, o)).OrderBy(o => o.C.HP / (float)Math.Max(1, o.C.EffectiveMaxHP)).FirstOrDefault();
                if (ward != null) target = foes.OrderBy(f => Zone.DistanceFt(f.Pos, ward.Pos) * 2 + Zone.DistanceFt(u.Pos, f.Pos)).First();
            }
            if (u.C is PlayerCharacter bpc && !u.IsHuman && !turn.BonusActionUsed) yield return AiBonusAction(u, bpc, target);
            if (prefersRange)
            {
                int dist = Zone.DistanceFt(u.Pos, target.Pos);
                if (dist > ranged.RangeNormal || !Zone.LineOfSight(u.Pos, target.Pos))
                {
                    var spot = Reachable(u, turn.MovementLeftFt).Where(p => Zone.DistanceFt(p, target.Pos) <= ranged.RangeNormal && Zone.LineOfSight(p, target.Pos))
                        .OrderBy(p => Zone.DistanceFt(u.Pos, p)).Cast<Vector2Int?>().FirstOrDefault();
                    if (spot.HasValue) yield return MoveTo(u, spot.Value);
                }
                if (u.Active && Zone.DistanceFt(u.Pos, target.Pos) <= ranged.RangeLong && Zone.LineOfSight(u.Pos, target.Pos))
                {
                    if (ranged.IsSpell) yield return SpellAttack(u, (PlayerCharacter)u.C, target, ranged.Spell, 0);
                    else yield return AiAttack(u, target, ranged);
                    yield break;
                }
            }
            if (melee == null) yield break;
            if (Zone.DistanceFt(u.Pos, target.Pos) > melee.ReachFt)
            {
                var adj = BestAdjacent(u, target, turn.MovementLeftFt);
                if (adj.HasValue) yield return MoveTo(u, adj.Value);
                else
                {
                    // Can't reach this turn: close the distance.
                    var path = Zone.FindPath(u.Pos, target.Pos, p => Units.Any(o => o != u && o.Pos == p && o.Active));
                    int steps = Math.Min(turn.MovementLeftFt / 5, Math.Max(0, path.Count - 1));
                    if (steps > 0) yield return MoveTo(u, path[steps - 1]);
                }
            }
            if (u.Active && target.Active && Zone.DistanceFt(u.Pos, target.Pos) <= melee.ReachFt)
            {
                if (u.C is PlayerCharacter) yield return AttackAction(u, target, melee);
                else yield return AiAttack(u, target, melee);
            }
        }

        /// <summary>Companion bonus action before moving: heal up, rage, or mark the target.</summary>
        IEnumerator AiBonusAction(Unit u, PlayerCharacter pc, Unit target)
        {
            if (pc.ResourceLeft("second_wind") > 0 && pc.HP < pc.EffectiveMaxHP / 2) { yield return SecondWind(u, pc); yield break; }
            if (pc.ResourceLeft("lay_on_hands") > 0)
            {
                var hurt = Units.Where(o => o.Active && !Hostile(u, o) && o.C.HP < o.C.EffectiveMaxHP * 0.4f && Zone.DistanceFt(u.Pos, o.Pos) <= 5)
                    .OrderBy(o => o.C.HP / (float)Math.Max(1, o.C.EffectiveMaxHP)).FirstOrDefault();
                if (hurt != null) { yield return LayOnHands(u, pc, hurt); yield break; }
            }
            if (pc.ResourceLeft("rage") > 0 && !pc.Raging && Zone.DistanceFt(u.Pos, target.Pos) <= pc.Speed + 5) { yield return Rage(u, pc); yield break; }
            if (pc.ConcentratingOn == null && Zone.DistanceFt(u.Pos, target.Pos) <= 90 && Zone.LineOfSight(u.Pos, target.Pos))
            {
                foreach (var id in new[] { "hunters_mark", "hex" })
                {
                    var mark = SpellsDb.Get(id);
                    bool canPay = pc.LowestAvailableSlot(1) > 0 || (id == "hunters_mark" && pc.ResourceLeft("favored_enemy") > 0);
                    if (pc.KnowsSpell(id) && canPay && pc.CanCastSpells) { yield return CastSpell(u, pc, mark, target.Pos); yield break; }
                }
            }
        }

        /// <summary>A monster (or reaction) attack: rolls shown on the HUD, damage rolled automatically, players may react.</summary>
        IEnumerator AiAttack(Unit me, Unit target, AttackOption atk)
        {
            me.Cb.Turn.ActionUsed = true;
            me.A.Face(target.A.transform.position);
            var ctx = Context(me, target);
            var res = AttackResolver.RollAttack(me.C, target.C, atk, ctx, me.Cb.Turn);
            if (res.OutOfRange) yield break;
            Ticker($"{me.Name} → {target.Name} ({atk.Name}): {res.Roll.Breakdown()} vs AC {res.TargetAC} → {(res.Critical ? "CRITICAL HIT!" : res.Hit ? "HIT" : "MISS")}");
            yield return new WaitForSeconds(0.5f);

            // Reactions: Shield (wizard target) and Cutting Words (any Lore bard nearby).
            if (res.Hit && !res.Critical && target.IsHuman && target.C is PlayerCharacter tpc && tpc.KnowsSpell("shield") && tpc.LowestAvailableSlot(1) > 0
                && !target.Cb.Turn.ReactionUsed && res.Roll.Total < res.TargetAC + 5)
            {
                bool? yes = null;
                target.Ctrl.Hud.Push(new ChoiceScreen("Reaction: Shield?", $"{me.Name} hits you with a {res.Roll.Total}. Cast Shield for +5 AC (uses a spell slot)?",
                    new List<(string, Action)> { ("Cast Shield!", () => yes = true), ("Take the hit", () => yes = false) }, false));
                while (yes == null) yield return null;
                if (yes == true)
                {
                    SpellResolver.PaySlot(tpc, SpellsDb.Get("shield"), tpc.LowestAvailableSlot(1));
                    target.Cb.Turn.ReactionUsed = true;
                    tpc.AddEffect(EffectLibrary.Create("shield", tpc));
                    res.TargetAC = target.C.ArmorClass;
                    res.Hit = res.Roll.Total >= res.TargetAC;
                    target.A.Popup("Shield!", UIKit.UtilityBlue);
                    Log($"{tpc.Name} casts Shield: AC {res.TargetAC}. {(res.Hit ? "Still hits!" : "The attack glances off!")}");
                }
            }
            if (res.Hit && !res.Critical)
            {
                var bard = Units.FirstOrDefault(o => o.IsHuman && o.Active && o.C is PlayerCharacter b && b.HasFeature("Cutting Words") && b.ResourceLeft("bardic_inspiration") > 0 && !o.Cb.Turn.ReactionUsed && Zone.DistanceFt(o.Pos, me.Pos) <= 60);
                if (bard != null)
                {
                    var bpc = (PlayerCharacter)bard.C;
                    bool? yes = null;
                    bard.Ctrl.Hud.Push(new ChoiceScreen("Reaction: Cutting Words?", $"{me.Name} rolled {res.Roll.Total} to hit {target.Name}. Spend Bardic Inspiration to subtract a d{bpc.BardicDie}?",
                        new List<(string, Action)> { ("Mock them mercilessly!", () => yes = true), ("Hold your tongue", () => yes = false) }, false));
                    while (yes == null) yield return null;
                    if (yes == true)
                    {
                        bpc.SpendResource("bardic_inspiration");
                        bard.Cb.Turn.ReactionUsed = true;
                        int v = Dice.AddBonusDie(res.Roll, -bpc.BardicDie);
                        res.Hit = res.Roll.Total >= res.TargetAC;
                        Log($"Cutting Words! -{v} → {res.Roll.Total}. {(res.Hit ? "Still hits." : "It misses!")}");
                        me.A.Popup($"-{v}", UIKit.UtilityBlue);
                    }
                }
            }

            if (res.Hit)
            {
                Sfx.Play(res.Critical ? "crit" : "hit");
                var dmg = AttackResolver.RollDamage(res);
                Ticker($"{me.Name} damage: {dmg.Breakdown()}");
                // Monk: Deflect Attacks (reaction) against bludgeoning, piercing or slashing hits.
                if (target.C is PlayerCharacter monk && monk.HasFeature("Deflect Attacks") && !target.Cb.Turn.ReactionUsed
                    && (atk.DamageType == DamageType.Bludgeoning || atk.DamageType == DamageType.Piercing || atk.DamageType == DamageType.Slashing))
                {
                    target.Cb.Turn.ReactionUsed = true;
                    int cut = monk.DeflectReduction();
                    dmg.Terms.Add(new RollTerm("deflected", -Math.Min(cut, dmg.Total)));
                    target.A.Popup("Deflect!", UIKit.UtilityBlue);
                    Log($"{monk.Name} deflects the blow (-{cut} damage).");
                }
                ApplyHitResults(me, target, res, dmg);
                yield return new WaitForSeconds(0.35f);
            }
            else
            {
                Sfx.Play("miss");
                target.A.Popup("Miss", Color.white);
            }
            RefreshTracker();
        }

        /// <summary>Concentration save when a concentrating creature takes damage.</summary>
        public void ConcentrationCheck(Creature c, int damage)
        {
            if (c.ConcentratingOn == null) return;
            int dc = Math.Max(10, damage / 2);
            var save = Checks.RollSave(c, Ability.CON, AdvantageState.Normal);
            bool ok = Checks.ResolveSave(save, dc);
            Ticker($"{c.Name} concentration (CON save): {save.Total} vs DC {dc} → {(ok ? "holds" : "BROKEN")}");
            if (!ok) Enc.EndConcentration(c);
        }
    }
}
