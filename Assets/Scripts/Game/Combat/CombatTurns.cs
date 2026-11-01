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
        public bool AreaLine;          // a 5-ft-wide line (Lightning Bolt)
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
            var pc = u.C; // a player character, or the dragon
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
            PurgeScaredDragon();
            bool playersUp = PlayersUp;
            bool enemiesUp = Units.Any(x => x.C.Team == Team.Enemies && (x.Active || (x.C.IsBanished && !x.C.IsDead)));
            return !playersUp || !enemiesUp;
        }

        // ================================================================== action list

        /// <summary>What a player could do on their turn right now (tests and tooltips).</summary>
        public List<CombatAction> ActionsFor(PlayerSlot p)
        {
            var u = Units.First(x => x.Owner == p);
            if (u.Cb != null) return BuildActions(u);
            // Before initiative is rolled: answer as if it were the start of their turn, without touching the fight.
            u.Cb = new Combatant(u.C);
            try { return BuildActions(u); } finally { u.Cb = null; }
        }

        /// <summary>Non-magical things you can still do in a fistfight.</summary>
        static readonly string[] BrawlAllowed = { "Move", "Unarmed Strike", "Tankard", "Dash", "Disengage", "Dodge", "End Turn", "Auto", "Rage", "Raging",
            "Reckless Attack", "Second Wind", "Action Surge", "Martial Arts strike", "Flurry of Blows", "Patient Defense", "Step of the Wind", "Hide (Cunning)",
            "Steady Aim", "Bardic Inspiration" };

        List<CombatAction> BuildActions(Unit u)
        {
            if (u.C is Dragonling) return DragonActions(u);
            var all = BuildAllActions(u);
            if (!Brawl) return all;
            var pc = (PlayerCharacter)u.C;
            var t = u.Cb.Turn;
            var fists = pc.UnarmedStrike();
            // Improvised weapon (2024): 1d4, no proficiency bonus.
            var tankard = new AttackOption { Name = "Tankard (improvised)", ToHit = pc.Mod(Ability.STR), Damage = new DiceExpr(1, 4), DamageMod = pc.Mod(Ability.STR),
                DamageType = DamageType.Bludgeoning, Ability = Ability.STR };
            var list = all.Where(a => BrawlAllowed.Any(n => a.Name.StartsWith(n))).ToList();
            list.Insert(1, Need(new CombatAction { Name = "Unarmed Strike", Cost = ActionCost.Action, Target = Targeting.Enemy, RangeFt = 5,
                Tooltip = $"Unarmed Strike: {AbilityUtil.Fmt(fists.ToHit)} to hit, {fists.Damage} {AbilityUtil.Fmt(fists.DamageMod)} bludgeoning", Run = (me, tile) => AttackAction(me, UnitAt(tile), fists) }, t, pc));
            list.Insert(2, Need(new CombatAction { Name = "Tankard (improvised, 1d4)", Cost = ActionCost.Action, Target = Targeting.Enemy, RangeFt = 5,
                Tooltip = "Improvised weapon: 1d4 bludgeoning, no proficiency bonus. Pip will bill you.", Run = (me, tile) => AttackAction(me, UnitAt(tile), tankard) }, t, pc));
            return list;
        }

        List<CombatAction> BuildAllActions(Unit u)
        {
            var pc = (PlayerCharacter)u.C;
            var t = u.Cb.Turn;
            var list = new List<CombatAction>();
            list.Add(new CombatAction { Name = "Move", Cost = ActionCost.Free, Target = Targeting.Tile });

            var weapon = pc.GetAttacks()[0];
            list.Add(WeaponAttackAction(u, pc, weapon, $"Attack ({weapon.Name})", ActionGroup.Main));
            // Fighter 9: Tactical Master: the same attack with Push, Sap or Slow instead of the weapon's own mastery.
            foreach (var m in pc.TacticalMasteries())
            {
                var swapped = CloneAttack(weapon); swapped.Mastery = m;
                list.Add(WeaponAttackAction(u, pc, swapped, $"Attack with {m} (Tactical Master)", ActionGroup.Abilities));
            }
            // Barbarian 9: Brutal Strike (only when attacking recklessly).
            if (pc.HasFeature("Brutal Strike") && pc.HasEffect("reckless") && !pc.HasEffect("brutal_ready"))
                foreach (var (kind, label, tip) in new[] { (1, "Forceful Blow", "pushes the target 15 ft"), (2, "Hamstring Blow", "−15 ft Speed until your next turn") })
                    list.Add(new CombatAction { Name = $"Brutal Strike: {label}", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                        Tooltip = $"Give up Reckless Advantage on your next Strength attack: if it hits, +1d10 damage and it {tip}.",
                        Run = (me, tile) => { var e = EffectLibrary.Create("brutal_ready", pc); e.Power = kind; pc.AddEffect(e); me.A.Popup(label + "!", UIKit.DamageRed); return Nothing(); } });
            // Rogue 5: Cunning Strike riders for the next Sneak Attack.
            if (pc.HasFeature("Cunning Strike") && !pc.HasEffect("cunning_ready") && !t.SneakAttackUsed)
                foreach (var (kind, label, tip) in new[] { (1, "Trip", "DEX save or fall Prone"), (2, "Poison", "CON save or Poisoned for a minute"), (3, "Withdraw", "you move half your Speed without provoking") })
                    list.Add(new CombatAction { Name = $"Cunning Strike: {label}", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                        Tooltip = $"Your next Sneak Attack rolls one die fewer, and {tip}.",
                        Run = (me, tile) => { var e = EffectLibrary.Create("cunning_ready", pc); e.Power = kind; pc.AddEffect(e); me.A.Popup(label + "?", UIKit.UtilityBlue); return Nothing(); } });

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
            var storm = pc.Effects.FirstOrDefault(e => e.Id == "call_lightning");
            if (storm != null && pc.ConcentratingOn == "call_lightning")
            {
                var cl = SpellsDb.Get("call_lightning");
                list.Add(Need(new CombatAction { Name = "Call Lightning (again)", Group = ActionGroup.Spells, Cost = ActionCost.Action, Target = Targeting.Tile, RangeFt = cl.RangeFt, AreaTiles = 1,
                    Tooltip = "Your storm is still overhead: call another bolt, no slot needed.", Run = (me, tile) => CastSpell(me, pc, cl, tile, storm.Power) }, t, pc));
            }
            if (pc.HasEffect("swift_quiver") && weapon.Ranged)
                list.Add(Need(new CombatAction { Name = "Swift Quiver (2 shots)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.Enemy, RangeFt = weapon.RangeLong,
                    Tooltip = "Bonus Action: two attacks with your ranged weapon.", Run = (me, tile) => SwiftQuiver(me, UnitAt(tile), weapon) }, t, pc));
            if (pc.HasEffect("haste") && !t.HasteActionUsed)
                list.Add(new CombatAction { Name = $"Hasted Attack ({weapon.Name})", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.Enemy, RangeFt = weapon.Ranged ? weapon.RangeLong : weapon.ReachFt,
                    Tooltip = "Haste: one extra weapon attack this turn.", Run = (me, tile) => { me.Cb.Turn.HasteActionUsed = true; return AttackAction(me, UnitAt(tile), weapon, false); } });
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
                    Tooltip = $"{(pc.FlurryStrikes == 3 ? "Three" : "Two")} unarmed strikes as a Bonus Action." + (pc.HasFeature("Open Hand Technique") ? " Open Hand: each hit can knock the target Prone (DEX save)." : ""),
                    Run = (me, tile) => { pc.SpendResource("focus"); return UnarmedBonus(me, pc, UnitAt(tile), pc.FlurryStrikes); } }, t, pc, fp));
                if (pc.HasFeature("Stunning Strike") && !t.StunningStrikeUsed && !pc.HasEffect("stun_ready"))
                    list.Add(new CombatAction { Name = "Stunning Strike (next hit, 1 Focus)", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None, Enabled = fp, Why = "No Focus left.",
                        Tooltip = $"Your next monk-weapon or unarmed hit this turn: CON save (DC {pc.KiSaveDC}) or Stunned until your next turn.",
                        Run = (me, tile) => { pc.AddEffect(new ActiveEffect { Id = "stun_ready", Name = "Stunning Strike ready", Source = pc, RoundsLeft = 1 }); me.A.Popup("Focus...", UIKit.UtilityBlue); return Nothing(); } });
                list.Add(Need(new CombatAction { Name = "Patient Defense (1 Focus)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Disengage and Dodge as a Bonus Action.", Run = (me, tile) => PatientDefense(me, pc) }, t, pc, fp));
                list.Add(Need(new CombatAction { Name = "Step of the Wind (1 Focus)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Dash and Disengage as a Bonus Action.", Run = (me, tile) => StepOfTheWind(me, pc) }, t, pc, fp));
            }
            if (pc.ResourceMax("divine_intervention") > 0 && !pc.HasEffect("divine_ready"))
                list.Add(new CombatAction { Name = "Divine Intervention", Group = ActionGroup.Spells, Cost = ActionCost.Free, Target = Targeting.None, Enabled = pc.ResourceLeft("divine_intervention") > 0, Why = "Once per Long Rest.",
                    Tooltip = "Call on Aurelith: your next cleric spell of level 5 or lower this turn needs no slot.",
                    Run = (me, tile) => { pc.AddEffect(new ActiveEffect { Id = "divine_ready", Name = "Divine Intervention", Source = pc, RoundsLeft = 1 }); me.A.Popup("Aurelith listens", UIKit.Gold); return Nothing(); } });
            if (pc.HasFeature("Wild Resurgence") && pc.ResourceLeft("wild_shape") == 0 && pc.LowestAvailableSlot(1) > 0 && !t.WildResurgenceUsed)
                list.Add(new CombatAction { Name = "Wild Resurgence (slot → Wild Shape)", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                    Tooltip = "Spend your lowest spell slot to regain a Wild Shape use.", Run = (me, tile) => { me.Cb.Turn.WildResurgenceUsed = true; var l = new List<string>(); pc.WildResurgence(l); l.ForEach(Log); return Nothing(); } });
            if (pc.HasFeature("Font of Inspiration") && pc.ResourceLeft("bardic_inspiration") < pc.ResourceMax("bardic_inspiration") && pc.LowestAvailableSlot(1) > 0)
                list.Add(new CombatAction { Name = "Font of Inspiration (slot → Inspiration)", Group = ActionGroup.Abilities, Cost = ActionCost.Free, Target = Targeting.None,
                    Tooltip = "Spend your lowest spell slot to regain a Bardic Inspiration use.", Run = (me, tile) => { var l = new List<string>(); pc.FontOfInspiration(l); l.ForEach(Log); return Nothing(); } });
            if (pc.ResourceMax("wholeness") > 0)
                list.Add(Need(new CombatAction { Name = $"Wholeness of Body ({pc.ResourceLeft("wholeness")})", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = $"Heal 1d{pc.MartialArtsDie} + WIS.", Run = (me, tile) => { me.Cb.Turn.BonusActionUsed = true; var l = new List<string>(); int h = pc.WholenessOfBody(l); me.A.Popup("+" + h, UIKit.HealGreen); l.ForEach(Log); return Nothing(); } },
                    t, pc, pc.ResourceLeft("wholeness") > 0));
            if (pc.ResourceMax("tireless") > 0)
                list.Add(Need(new CombatAction { Name = $"Tireless ({pc.ResourceLeft("tireless")})", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.None,
                    Tooltip = "Gain 1d8 + WIS temporary HP.", Run = (me, tile) => { me.Cb.Turn.ActionUsed = true; var l = new List<string>(); int th = pc.Tireless(l); me.A.Popup("+" + th + " temp", UIKit.HealGreen); l.ForEach(Log); return Nothing(); } },
                    t, pc, pc.ResourceLeft("tireless") > 0));
            if (pc.HasFeature("Abjure Foes") && pc.ResourceMax("channel_divinity") > 0)
                list.Add(Need(new CombatAction { Name = $"Abjure Foes ({pc.ResourceLeft("channel_divinity")})", Group = ActionGroup.Abilities, Cost = ActionCost.Action, Target = Targeting.None,
                    Tooltip = $"Channel Divinity: up to {pc.AbjureFoesTargets} enemies within 60 ft make a WIS save (DC {pc.SpellSaveDC}) or are Frightened (it ends if they take damage).",
                    Run = (me, tile) => AbjureFoes(me, pc) }, t, pc, pc.ResourceLeft("channel_divinity") > 0));
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
            if (pc.HasFeature("Sorcery Incarnate") && pc.ResourceLeft("innate_sorcery") == 0 && !pc.HasEffect("innate_sorcery"))
                list.Add(Need(new CombatAction { Name = "Innate Sorcery (2 pts, Sorcery Incarnate)", Group = ActionGroup.Abilities, Cost = ActionCost.Bonus, Target = Targeting.None,
                    Tooltip = "Out of uses: burn 2 sorcery points to unleash Innate Sorcery anyway.",
                    Run = (me, tile) => { me.Cb.Turn.BonusActionUsed = true; var l = new List<string>(); if (pc.SorceryIncarnate(l)) me.A.Popup("Innate Sorcery", UIKit.UtilityBlue); l.ForEach(Log); return Nothing(); } },
                    t, pc, pc.ResourceLeft("sorcery_points") >= 2));
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

            if (pc.MainHand != null || pc.OffSetWeapon != null)
                list.Add(new CombatAction { Name = "Swap Weapons", Group = ActionGroup.Actions, Cost = ActionCost.Free, Target = Targeting.None,
                    Enabled = !t.ObjectInteractionUsed, Why = "You've already swapped this turn.",
                    Tooltip = $"Free once per turn (your object interaction): {(pc.MainHand?.Name ?? "empty hands")} ⇄ {(pc.OffSetWeapon?.Name ?? "empty hands")}.",
                    Run = (me, tile) => { me.Cb.Turn.ObjectInteractionUsed = true; pc.SwapWeaponSets(out var m); Log($"{pc.Name}: {m}"); me.A.Popup("Swap!", Color.white); return Nothing(); } });
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
            s.Kind != SpellKind.Utility || s.Id == "spare_the_dying" || s.Id == "misty_step" || s.Id == "dimension_door" || s.Id == "guardian_of_faith";

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
                    else { a.Target = Targeting.Tile; a.AreaTiles = Math.Max(1, s.AreaFt / 5); a.AreaFromSelf = s.RangeFt == 0; a.AreaLine = s.Shape == TargetShape.Line && s.ZoneLineTiles == 0; if (a.AreaFromSelf) a.RangeFt = s.AreaFt; }
                    break;
            }
            if (s.Kind == SpellKind.Heal && s.MaxTargets > 1) { a.Target = Targeting.Tile; a.AreaTiles = Math.Max(1, s.AreaFt / 5); }
            if (s.CastTime == CastTime.Reaction || s.CastTime == CastTime.Minute || s.CastTime == CastTime.TenMinutes) { a.Enabled = false; a.Why = s.CastTime == CastTime.Reaction ? "Reaction: offered automatically when you're hit." : "Takes too long to cast in combat."; }
            bool divine = DivineInterventionCovers(pc, s);
            if (divine) a.Name += " (Divine Intervention)";
            if (!s.IsCantrip && !divine && pc.LowestAvailableSlot(s.Level) < 0 && !(s.Id == "hunters_mark" && pc.ResourceLeft("favored_enemy") > 0)) { a.Enabled = false; a.Why = "No spell slots left."; }
            if (!s.IsCantrip && t.CastLeveledSpellThisTurn) { a.Enabled = false; a.Why = "Only one spell slot per turn (2024 rules)."; }
            Need(a, t, pc);
            a.Run = (me, tile) => CastSpell(me, pc, s, tile);
            return a;
        }

        // ================================================================== lines, walls, smites

        /// <summary>A 5-ft-wide line from `from` towards `toward`, `length` tiles long (Lightning Bolt).</summary>
        List<Vector2Int> LineTiles(Vector2Int from, Vector2Int toward, int length)
        {
            var list = new List<Vector2Int>();
            Vector2 dir = toward - from;
            if (dir.sqrMagnitude < 0.01f) return list;
            dir.Normalize();
            for (int i = 1; i <= length; i++)
            {
                var p = from + new Vector2Int(Mathf.RoundToInt(dir.x * i), Mathf.RoundToInt(dir.y * i));
                if (!Zone.IsWalkable(p)) break;
                if (!list.Contains(p)) list.Add(p);
            }
            return list;
        }

        /// <summary>A wall `length` tiles long through `centre`, square to the caster's line of sight (Wall of Fire).</summary>
        List<Vector2Int> WallTiles(Unit me, Vector2Int centre, int length)
        {
            var d = centre - me.Pos;
            var side = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
            var list = new List<Vector2Int>();
            for (int i = -length / 2; i < length - length / 2; i++)
            {
                var p = centre + side * i;
                if (Zone.IsWalkable(p)) list.Add(p);
            }
            return list;
        }

        static IEnumerable<Vector2Int> Neighbours(Vector2Int p)
        {
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) if (dx != 0 || dy != 0) yield return p + new Vector2Int(dx, dy);
        }

        /// <summary>Blinding, Staggering or Banishing Smite landing on a melee hit.</summary>
        IEnumerator SpellSmite(Unit me, PlayerCharacter pc, Unit target, ActiveEffect ready, bool critical)
        {
            pc.Effects.Remove(ready);
            string id = ready.Id.Replace("_ready", "");
            var spell = SpellsDb.Get(id);
            var type = id == "blinding_smite" ? DamageType.Radiant : id == "staggering_smite" ? DamageType.Psychic : DamageType.Force;
            var expr = SpellResolver.SmiteDice(id, Math.Max(spell.Level, ready.Power));
            var roll = Dice.RollDamage(expr, critical);
            yield return WaitDamageRoll(me, spell.Name + "!", $"{expr} {type.ToString().ToLower()}{(critical ? " (critical: doubled)" : "")}", roll, DieColor.Damage);
            var log = new List<string>();
            int taken = target.C.TakeDamage(roll.Total, type, critical, log);
            target.A.Popup("-" + taken, UIKit.Gold);
            log.ForEach(Log);
            OnDamaged(target);
            if (!target.Active) yield break;
            if (id == "blinding_smite") { target.C.AddEffect(EffectLibrary.Create("blinded_smite", pc, pc.SpellSaveDC)); target.A.Popup("Blinded", UIKit.UtilityBlue); }
            else if (id == "staggering_smite")
            {
                var save = Checks.RollSave(target.C, Ability.WIS, AdvantageState.Normal);
                if (!Checks.ResolveSave(save, pc.SpellSaveDC)) { target.C.AddEffect(EffectLibrary.Create("staggered", pc)); target.Cb.Turn.ReactionUsed = true; target.A.Popup("Staggered", UIKit.UtilityBlue); }
                else Log($"{target.Name} shrugs off the stagger ({save.Total} vs DC {pc.SpellSaveDC}).");
            }
            else if (id == "banishing_smite" && target.C.HP <= 50)
            {
                if (pc.ConcentratingOn != null) EndConcentration(pc);
                pc.ConcentratingOn = "banishing_smite";
                target.C.AddEffect(EffectLibrary.Create("banished_smite", pc));
                SyncBanished();
                Log($"{target.Name} is banished!");
            }
        }

        /// <summary>Brutal Strike and Cunning Strike effects after the hit lands.</summary>
        void StrikeRiders(Unit me, Unit target, AttackRollResult res)
        {
            var pc = (PlayerCharacter)me.C;
            if (res.BrutalStrike == 1) { Push(me, target, 3); Log("Forceful Blow!"); }
            if (res.BrutalStrike == 2) { target.C.AddEffect(new ActiveEffect { Id = "hamstrung", Name = "Hamstrung", Source = pc, RoundsLeft = 1, SpeedPenalty = 15 }); target.A.Popup("Hamstrung", UIKit.UtilityBlue); }
            if (res.CunningStrike == 0) return;
            int dc = 8 + pc.ProficiencyBonus + pc.Mod(Ability.DEX);
            if (res.CunningStrike == 3)
            {
                me.Cb.Turn.MovementLeftFt += pc.Speed / 2;
                me.Cb.Turn.Disengaged = true;
                me.A.Popup("Withdraw", UIKit.UtilityBlue);
                Log($"{pc.Name} slips away (half Speed, no opportunity attacks).");
                return;
            }
            var ab = res.CunningStrike == 1 ? Ability.DEX : Ability.CON;
            var save = Checks.RollSave(target.C, ab, AdvantageState.Normal, res.CunningStrike == 2 ? "poison" : null);
            bool ok = Checks.ResolveSave(save, dc);
            Ticker($"{target.Name} {ab} save vs Cunning Strike: {save.Breakdown()} vs DC {dc} → {(ok ? "SAVED" : "FAILED")}");
            if (ok) return;
            if (res.CunningStrike == 1) { target.C.AddEffect(EffectLibrary.Create("prone", pc)); target.A.Popup("Tripped!", UIKit.UtilityBlue); }
            else { target.C.AddEffect(new ActiveEffect { Id = "poisoned", Name = "Poisoned", Source = pc, RoundsLeft = 10, Condition = Condition.Poisoned, RepeatSave = Ability.CON, RepeatSaveDC = dc }); target.A.Popup("Poisoned", UIKit.UtilityBlue); }
        }

        /// <summary>Lightning Arrow: the shot becomes lightning (4d8, half on a miss) and bursts over everyone within 10 ft.</summary>
        IEnumerator LightningArrow(Unit me, PlayerCharacter pc, Unit target, ActiveEffect ready, bool hit, bool critical)
        {
            pc.Effects.Remove(ready);
            int lvl = Math.Max(3, ready.Power);
            var main = Dice.RollDamage(new DiceExpr(4 + (lvl - 3), 8), critical);
            yield return WaitDamageRoll(me, "Lightning Arrow!", $"{4 + lvl - 3}d8 lightning{(hit ? "" : " (miss: half)")}", main, DieColor.Damage);
            var log = new List<string>();
            int taken = target.C.TakeDamage(hit ? main.Total : main.Total / 2, DamageType.Lightning, critical, log);
            target.A.Popup("-" + taken, UIKit.DamageRed);
            log.ForEach(Log);
            OnDamaged(target);
            var burst = Dice.RollDamage(new DiceExpr(2 + (lvl - 3), 8));
            foreach (var o in Units.Where(o => o != target && o != me && o.Active && Zone.DistanceFt(o.Pos, target.Pos) <= 10).ToList())
            {
                var save = Checks.RollSave(o.C, Ability.DEX, AdvantageState.Normal);
                bool ok = Checks.ResolveSave(save, pc.SpellSaveDC);
                var l2 = new List<string>();
                int t2 = o.C.TakeDamage(ok ? burst.Total / 2 : burst.Total, DamageType.Lightning, false, l2);
                o.A.Popup("-" + t2, UIKit.DamageRed);
                l2.ForEach(Log);
                OnDamaged(o);
            }
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
            if (a.AreaLine) return LineTiles(u.Pos, cursor, a.AreaTiles);
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

        /// <summary>Owl-shaped druids (and anyone under Fly) cross water.</summary>
        public static bool Flies(Unit u) => (u.C is PlayerCharacter p && p.Flying) || (u.C is Monster m && m.Def.FlySpeed > 0);

        public HashSet<Vector2Int> Reachable(Unit u, int feet)
        {
            bool fly = Flies(u);
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
                    if (cost > feet || dist.ContainsKey(n) || !Zone.IsPassable(n, fly)) continue;
                    if (d.x != 0 && d.y != 0 && (!Zone.IsPassable(new Vector2Int(c.x + d.x, c.y), fly) || !Zone.IsPassable(new Vector2Int(c.x, c.y + d.y), fly))) continue;
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
            var path = Zone.FindPath(u.Pos, dest, p => Units.Any(o => o != u && o.Pos == p && Hostile(u, o) && o.Active), flying: Flies(u));
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
                            _opportunity = true;
                            if (foe.IsHuman) yield return AttackAction(foe, u, melee, consumeAction: false);
                            else yield return AiAttack(foe, u, melee);
                            _opportunity = false;
                            if (!u.Active) yield break;
                        }
                    }
                }
                yield return u.A.WalkPath(new List<Vector2Int> { step }, 5f);
                turn.MovementLeftFt -= 5;
                if (Zones.Count > 0) { DrawZones(); TriggerZones(u); if (!u.Active) break; }
                foreach (var o in Units) if (o != u && Zones.Any(z => z.FollowsOwner && z.Owner == u.C)) TriggerZones(o); // a moving aura sweeps over others
            }
            RefreshTracker();
        }

        // ================================================================== attacks (human)

        IEnumerator AttackAction(Unit me, Unit target, AttackOption atk, bool consumeAction = true)
        {
            if (target == null) yield break;
            var turn = me.Cb.Turn;
            if (consumeAction)
            {
                turn.ActionUsed = true;
                turn.AttacksLeftInAction = me.C is PlayerCharacter apc && !atk.IsSpell ? apc.AttacksPerAction - 1 : 0;
            }
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

            var arrow = atk.Ranged && !atk.IsSpell && me.C is PlayerCharacter archer ? archer.Effects.FirstOrDefault(e => e.Id == "lightning_arrow_ready") : null;
            if (arrow != null)
            {
                Sfx.Play(res.Hit ? "hit" : "miss");
                yield return LightningArrow(me, (PlayerCharacter)me.C, target, arrow, res.Hit, res.Critical);
                RefreshTracker();
                yield break;
            }

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
                if (target.Active && (res.BrutalStrike > 0 || res.CunningStrike > 0)) StrikeRiders(me, target, res);
                if (target.Active && me.C is PlayerCharacter monk && monk.HasFeature("Stunning Strike") && !atk.Ranged && (atk.Weapon == null || monk.IsMonkWeapon(atk.Weapon)))
                {
                    var stunReady = monk.Effects.FirstOrDefault(e => e.Id == "stun_ready");
                    bool auto = !me.IsHuman && monk.ResourceLeft("focus") >= 2 && !target.C.Has(Condition.Stunned);
                    if (stunReady != null || auto)
                    {
                        if (stunReady != null) monk.Effects.Remove(stunReady);
                        var sl = new List<string>();
                        if (monk.TryStunningStrike(target.C, turn, sl)) target.A.Popup(target.C.Has(Condition.Stunned) ? "STUNNED" : "Reeling", UIKit.Gold);
                        sl.ForEach(Log);
                    }
                }
                if (me.C is PlayerCharacter smiter && !atk.Ranged && !atk.IsSpell && target.Active)
                {
                    var spellSmite = smiter.Effects.FirstOrDefault(e => e.Id == "blinding_smite_ready" || e.Id == "staggering_smite_ready" || e.Id == "banishing_smite_ready");
                    if (spellSmite != null) yield return SpellSmite(me, smiter, target, spellSmite, res.Critical);
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
            TryChoke(me, target);
        }

        /// <summary>
        /// The weak spot: a hit on a boss while it draws breath, once the party knows where to strike (WeakSpotFlag), chokes it.
        /// The breath is lost, it takes 4d10 force, and it has no legendary actions left this round.
        /// </summary>
        public bool TryChoke(Unit me, Unit target)
        {
            if (!(target.C is Monster m) || m.Charging == null || !target.Active || m.Def.WeakSpotFlag == null || !G.World.Flag(m.Def.WeakSpotFlag)) return false;
            m.Charging = null;
            m.LegendaryLeft = 0;
            var extra = Dice.RollDamage(DiceExpr.Parse("4d10"));
            var lines = new List<string>();
            int took = m.TakeDamage(extra.Total, DamageType.Force, false, lines);
            Ticker($"{me.Name} strikes the glowing throat! {target.Name} chokes on its own fire: {extra.Breakdown()} force");
            Log($"{target.Name} chokes on its own fire! (+{took} force, the breath is lost)");
            lines.ForEach(Log);
            target.A.Popup("CHOKES! -" + took, UIKit.D20Gold);
            target.A.Sr.color = Color.white;
            OnDamaged(target);
            return true;
        }

        public void OnDamaged(Unit target)
        {
            if (target.C is Monster boss && !boss.IsDown)
            {
                string phase = boss.CheckPhaseTwo();
                if (phase != null)
                {
                    Log($"{target.Name}: phase two!");
                    foreach (var h in Humans) h.Hud.Narrate(phase);
                    target.A.Popup("ENRAGED", UIKit.DamageRed);
                }
            }
            if (target.C.IsDead && target.DeathRound < 0) target.DeathRound = Enc.Round;
            if (target.C is PlayerCharacter shaped) WildShapeFx.Sync(target.A, shaped);
            if (target.C is Dragonling && target.C.IsDown) { ScareOffDragon(target); return; }
            if (target.C.IsDown && target.C.Team == Team.Enemies) { target.A.Popup("Defeated!", UIKit.Gold); }
            if (target.C.IsDown && target.Owner != null) target.Owner.Hud.Narrate(target.C.IsDead ? "Everything goes dark..." : "You fall unconscious! Death saves begin on your turn...");
            else if (target.C.IsDown && target.Comp != null) target.Comp.Leader?.Hud.Narrate(target.C.IsDead ? $"{target.Name} has fallen..." : $"{target.Name} is down! Get a potion or a healing spell to them (Items: Give Potion to Ally).");
        }

        // ================================================================== spells

        /// <summary>Spell attack rolls made in this fight (tests: Eldritch Blast beams).</summary>
        public int SpellAttackRolls;

        /// <summary>Tests: let the AI take this unit's turn right now.</summary>
        public IEnumerator DebugAiTurn(Unit u) => AiTurn(u);

        /// <summary>Tests: cast a spell as this unit right now (pays the slot as usual).</summary>
        public IEnumerator DebugCast(Unit me, string spellId, Vector2Int tile) => CastSpell(me, (PlayerCharacter)me.C, SpellsDb.Get(spellId), tile);

        static bool DivineInterventionCovers(PlayerCharacter pc, SpellDef s) =>
            pc.HasEffect("divine_ready") && !s.IsCantrip && s.Level <= 5 && s.Classes.Contains("cleric");

        IEnumerator CastSpell(Unit me, PlayerCharacter pc, SpellDef s, Vector2Int tile, int freeAtSlot = 0)
        {
            var turn = me.Cb.Turn;
            bool recast = freeAtSlot > 0; // Call Lightning calling again: the spell (and its concentration) is already going
            if (freeAtSlot == 0 && DivineInterventionCovers(pc, s))
            {
                pc.RemoveEffect("divine_ready");
                pc.SpendResource("divine_intervention");
                freeAtSlot = s.Level;
                Log($"Aurelith answers! {pc.Name} casts {s.Name} without a slot (Divine Intervention).");
            }
            int slot = freeAtSlot > 0 ? freeAtSlot : s.IsCantrip ? 0 : pc.LowestAvailableSlot(s.Level);
            if (freeAtSlot == 0 && !SpellResolver.PaySlot(pc, s, slot)) { me.Ctrl?.Hud.Toast("No slot available.", UIKit.Bad); yield break; }
            if (s.CastTime == CastTime.BonusAction || turn.QuickenNextSpell) { turn.BonusActionUsed = true; turn.QuickenNextSpell = false; }
            else turn.ActionUsed = true;
            if (!s.IsCantrip) turn.CastLeveledSpellThisTurn = true;
            var log = new List<string>();
            if (!recast)
            {
                if (s.Concentration && pc.ConcentratingOn != null) EndConcentration(pc);
                SpellResolver.StartConcentration(pc, s, log);
                if (s.Concentration) pc.ConcentratingOn = s.Id;
            }
            Sfx.Play("spell");
            Log($"{pc.Name} casts {s.Name}{(slot > 0 ? $" (level {slot} slot)" : "")}!");
            me.A.Face(Zone.ToWorld(tile));
            var target = UnitAt(tile);

            switch (s.Kind)
            {
                case SpellKind.Attack:
                    for (int i = 0; i < SpellResolver.Projectiles(pc, s); i++)
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
                    List<Vector2Int> area;
                    if (s.ZoneLineTiles > 0) area = WallTiles(me, tile, s.ZoneLineTiles);
                    else area = AreaTiles(me, new CombatAction { AreaTiles = s.Shape == TargetShape.Single ? 0 : Math.Max(1, s.AreaFt / 5), AreaFromSelf = s.RangeFt == 0, AreaLine = s.Shape == TargetShape.Line, RangeFt = s.RangeFt == 0 ? s.AreaFt : s.RangeFt }, tile);
                    if (s.Shape == TargetShape.Single) { if (target != null) targets.Add(target); }
                    else targets.AddRange(Units.Where(o => o.Active && area.Contains(o.Pos) && o != me && (!s.OnlyEnemies || Hostile(me, o))));
                    if (s.MaxTargets > 1 && targets.Count > s.MaxTargets) targets = targets.OrderBy(o => Zone.DistanceFt(tile, o.Pos)).Take(s.MaxTargets).ToList();
                    // Evoker 6: Sculpt Spells: friends in the blast are left untouched.
                    var sculpted = SpellResolver.Sculpted(pc, s, targets.Select(o => o.C));
                    if (sculpted.Count > 0) { targets.RemoveAll(o => sculpted.Contains(o.C)); Log($"{pc.Name} sculpts the spell around {string.Join(", ", sculpted.Select(c => c.Name))}."); }
                    if (s.Id == "call_lightning" && !recast) { var storm = EffectLibrary.Create("call_lightning", pc); storm.Power = Math.Max(slot, s.Level); pc.AddEffect(storm); }
                    if (s.ZoneLineTiles > 0)
                    {
                        // Wall of Fire: the wall appears, everyone caught in it saves now, then it keeps burning.
                        var wall = ZoneRules.Create(pc, s, slot, tile.x, tile.y, area.Select(t => (t.x, t.y)).ToList());
                        AddZone(wall);
                        TriggerZoneOnAll(wall);
                        break;
                    }
                    DamageRoll dmg = null, second = null;
                    if (!s.Dice.IsNone)
                    {
                        dmg = SpellResolver.RollEffect(pc, s, Math.Max(slot, s.Level), false);
                        yield return WaitDamageRoll(me, s.Name, $"{SpellResolver.EffectDice(pc, s, Math.Max(slot, s.Level))} {s.DamageType.ToString().ToLower()} · {s.SaveAbility} save DC {pc.SpellSaveDC}", dmg, DieColor.Damage);
                        second = SpellResolver.RollSecond(s, false);
                        if (second != null) yield return WaitDamageRoll(me, s.Name, $"+ {s.SecondDice} {s.SecondDamageType.ToString().ToLower()}", second, DieColor.Damage);
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
                            SpellResolver.ApplySaveDamage(pc, t.C, s, dmg, saved, lines, second);
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
                    var healed = new List<Unit>();
                    if (s.MaxTargets > 1)
                        healed.AddRange(Units.Where(o => !o.C.IsDead && !Hostile(me, o) && Zone.DistanceFt(tile, o.Pos) <= Math.Max(5, s.AreaFt) && Zone.DistanceFt(me.Pos, o.Pos) <= s.RangeFt)
                            .OrderBy(o => o.C.HP * 100 / Math.Max(1, o.C.MaxHP)).Take(s.MaxTargets));
                    else if (target != null) healed.Add(target);
                    if (healed.Count == 0) { me.Ctrl?.Hud.Toast("No allies there.", UIKit.Bad); break; }
                    var dmg = SpellResolver.RollEffect(pc, s, Math.Max(slot, s.Level), false);
                    yield return WaitDamageRoll(me, s.Name, healed.Count == 1 ? $"Healing {healed[0].Name}" : $"Healing {healed.Count} allies", dmg, DieColor.Healing);
                    Sfx.Play("heal");
                    foreach (var hu in healed)
                    {
                        bool wasDown = hu.C.IsDown;
                        int h = hu.C.Heal(SpellResolver.HealTotal(dmg, hu.C));
                        hu.A.Popup("+" + h, UIKit.HealGreen);
                        Log($"{hu.Name} regains {h} HP{(wasDown ? " and gets back up!" : ".")}");
                    }
                    int blessed = healed.Any(o => o != me) ? SpellResolver.BlessedHealerAmount(pc, s, Math.Max(slot, s.Level)) : 0;
                    if (blessed > 0) { int bh = pc.Heal(blessed); if (bh > 0) { me.A.Popup("+" + bh, UIKit.HealGreen); Log($"Blessed Healer: {pc.Name} regains {bh} HP."); } }
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
                    if (s.Id == "spirit_guardians")
                    {
                        AddZone(ZoneRules.Create(pc, s, slot, me.Pos.x, me.Pos.y));
                        me.A.Popup("Spirit Guardians", UIKit.Gold);
                        Log("Spectral guardians wheel around " + pc.Name + ".");
                        break;
                    }
                    if (s.Id == "greater_restoration")
                    {
                        var who = target ?? me;
                        int removed = who.C.Effects.RemoveAll(e => e.Condition == Condition.Charmed || e.Condition == Condition.Frightened || e.Condition == Condition.Paralyzed || e.Condition == Condition.Petrified);
                        if (who.C.Exhaustion > 0) { who.C.Exhaustion--; removed++; }
                        who.A.Popup("Restored", UIKit.HealGreen);
                        Log(removed > 0 ? $"{who.Name} is restored." : $"{who.Name} feels refreshed, though nothing ailed them.");
                        break;
                    }
                    var recipients = new List<Unit>();
                    if (target != null) recipients.Add(target); else recipients.Add(me);
                    if (s.MaxTargets > 1)
                        recipients.AddRange(Units.Where(o => !recipients.Contains(o) && !Hostile(me, o) && !o.C.IsDead && Zone.DistanceFt(me.Pos, o.Pos) <= s.RangeFt).OrderBy(o => Zone.DistanceFt(me.Pos, o.Pos)).Take(s.MaxTargets - 1));
                    foreach (var r in recipients)
                    {
                        var fx = EffectLibrary.Create(s.EffectId, pc);
                        fx.Power = Math.Max(slot, s.Level);
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
                    else if (s.Id == "dimension_door" && !Occupied(tile) && Zone.IsWalkable(tile))
                    {
                        var buddy = Units.FirstOrDefault(o => o != me && o.Active && !Hostile(me, o) && Zone.DistanceFt(o.Pos, me.Pos) <= 5);
                        me.A.PlaceAt(Zone, tile);
                        me.A.Popup("*pop*", UIKit.UtilityBlue);
                        if (buddy != null)
                        {
                            var spot = Neighbours(tile).FirstOrDefault(n => Zone.IsWalkable(n) && !Occupied(n));
                            if (spot != default) { buddy.A.PlaceAt(Zone, spot); Log($"{buddy.Name} steps through with {pc.Name}."); }
                        }
                    }
                    else if (s.Id == "guardian_of_faith" && Zone.IsWalkable(tile))
                    {
                        var g = ZoneRules.Create(pc, s, slot, tile.x, tile.y);
                        AddZone(g);
                        Log("A spectral guardian appears, sword raised.");
                    }
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
            SpellAttackRolls++;
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
                if (lost > 0) AttackResolver.RaiseConcentrationCheck(o.C, lost);
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
                if (strikes >= 2 && pc.HasFeature("Open Hand Technique") && target.Active && target.C.HP < hpBefore && !target.C.Has(Condition.Prone))
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


        static AttackOption CloneAttack(AttackOption a) => new AttackOption
        {
            Name = a.Name, ToHit = a.ToHit, Damage = a.Damage, DamageMod = a.DamageMod, DamageType = a.DamageType, ReachFt = a.ReachFt, RangeNormal = a.RangeNormal,
            RangeLong = a.RangeLong, Ranged = a.Ranged, Mastery = a.Mastery, Weapon = a.Weapon, CritOn = a.CritOn, ExtraDamage = a.ExtraDamage, ExtraDamageType = a.ExtraDamageType,
            MinDieValue = a.MinDieValue, Ability = a.Ability, Spell = a.Spell,
        };

        /// <summary>A weapon attack button: the Attack action, or (Extra Attack) one of the attacks it still has left.</summary>
        CombatAction WeaponAttackAction(Unit u, PlayerCharacter pc, AttackOption atk, string name, ActionGroup group)
        {
            var t = u.Cb.Turn;
            int range = atk.Ranged ? atk.RangeLong : atk.ReachFt;
            if (t.ActionUsed && t.AttacksLeftInAction > 0)
            {
                int max = pc.AttacksPerAction;
                return new CombatAction { Name = $"{name} {max - t.AttacksLeftInAction + 1}/{max}", Group = group, Cost = ActionCost.Free, Target = Targeting.Enemy, RangeFt = range,
                    Tooltip = "Extra Attack: this Attack action still has an attack left. " + atk.Summary(),
                    Run = (me, tile) => { me.Cb.Turn.AttacksLeftInAction--; return AttackAction(me, UnitAt(tile), atk, false); } };
            }
            return Need(new CombatAction { Name = name, Group = group, Cost = ActionCost.Action, Target = Targeting.Enemy, RangeFt = range,
                Tooltip = (pc.AttacksPerAction > 1 ? $"Extra Attack: {pc.AttacksPerAction} attacks. " : "") + atk.Summary(), Run = (me, tile) => AttackAction(me, UnitAt(tile), atk) }, t, pc);
        }

        IEnumerator SwiftQuiver(Unit me, Unit target, AttackOption weapon)
        {
            me.Cb.Turn.BonusActionUsed = true;
            for (int i = 0; i < 2; i++)
            {
                if (target == null || !target.Active) target = Units.Where(o => o.Active && Hostile(me, o) && Zone.DistanceFt(me.Pos, o.Pos) <= weapon.RangeLong).OrderBy(o => Zone.DistanceFt(me.Pos, o.Pos)).FirstOrDefault();
                if (target == null) yield break;
                yield return AttackAction(me, target, weapon, false);
            }
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
                if (pc.HasFeature("Instinctive Pounce")) { me.Cb.Turn.MovementLeftFt += pc.Speed / 2; log.Add("Instinctive Pounce: +half your Speed this turn."); }
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

        IEnumerator AbjureFoes(Unit me, PlayerCharacter pc)
        {
            pc.SpendResource("channel_divinity");
            me.Cb.Turn.ActionUsed = true;
            me.A.Popup("Abjure Foes!", UIKit.Gold);
            Log($"{pc.Name} raises a holy symbol: \"Begone!\"");
            foreach (var t in Units.Where(o => o.Active && Hostile(me, o) && Zone.DistanceFt(me.Pos, o.Pos) <= 60).OrderBy(o => Zone.DistanceFt(me.Pos, o.Pos)).Take(pc.AbjureFoesTargets).ToList())
            {
                var save = Checks.RollSave(t.C, Ability.WIS, AdvantageState.Normal);
                bool ok = Checks.ResolveSave(save, pc.SpellSaveDC);
                Ticker($"{t.Name} WIS save: {save.Breakdown()} vs DC {pc.SpellSaveDC} → {(ok ? "SAVED" : "FAILED")}");
                if (!ok) { t.C.AddEffect(new ActiveEffect { Id = "abjured", Name = "Frightened", Source = pc, RoundsLeft = 10, Condition = Condition.Frightened, EndsOnDamage = true, RepeatSave = Ability.WIS, RepeatSaveDC = pc.SpellSaveDC }); t.A.Popup("Frightened", UIKit.UtilityBlue); }
                yield return new WaitForSeconds(0.35f);
            }
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
            if (pc.HasFeature("Tactical Shift")) { me.Cb.Turn.MovementLeftFt += pc.Speed / 2; me.Cb.Turn.Disengaged = true; Log("Tactical Shift: half your Speed, no opportunity attacks."); }
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
            var foes = Units.Where(o => Hostile(u, o) && o.Active && !Spares(u, o)).ToList();
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

            // The teenage dragon covers a hurt friend with a wing before it bites.
            if (u.C is Dragonling teen && teen.IsTeen && !turn.BonusActionUsed)
            {
                var hurt = Units.FirstOrDefault(o => o != u && o.Active && !Hostile(u, o) && o.C.HP < o.C.EffectiveMaxHP / 2 && Zone.DistanceFt(u.Pos, o.Pos) <= 5);
                if (hurt != null) yield return ShieldWing(u, hurt);
            }
            if (monster != null && monster.Def.Abilities.Count > 0)
            {
                bool used = false;
                yield return TryMonsterAbility(u, monster, foes, b => used = b);
                if (used) yield break;
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
            if (u.C is PlayerCharacter lpc && lpc.IsCaster && lpc.CanCastSpells && !u.IsHuman && !turn.ActionUsed)
            {
                bool cast = false;
                yield return AiCastLeveled(u, lpc, foes, b => cast = b);
                if (cast) yield break;
            }
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
                    if (ranged.IsSpell)
                        for (int beam = 0; beam < SpellResolver.Projectiles((PlayerCharacter)u.C, ranged.Spell) && u.Active; beam++)
                        {
                            if (!target.Active) target = Units.Where(o => o.Active && Hostile(u, o) && Zone.DistanceFt(u.Pos, o.Pos) <= ranged.RangeLong).OrderBy(o => o.C.HP).FirstOrDefault();
                            if (target == null) break;
                            yield return SpellAttack(u, (PlayerCharacter)u.C, target, ranged.Spell, 0);
                        }
                    else if (u.C is PlayerCharacter) yield return CompanionAttacks(u, target, ranged);
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
                    var path = Zone.FindPath(u.Pos, target.Pos, p => Units.Any(o => o != u && o.Pos == p && o.Active), flying: Flies(u));
                    int steps = Math.Min(turn.MovementLeftFt / 5, Math.Max(0, path.Count - 1));
                    if (steps > 0) yield return MoveTo(u, path[steps - 1]);
                }
            }
            if (u.Active && target.Active && Zone.DistanceFt(u.Pos, target.Pos) <= melee.ReachFt)
            {
                if (u.C is PlayerCharacter) yield return CompanionAttacks(u, target, melee);
                else if (GoodBreath(u, target.Pos)) yield return EmberBreath(u, target.Pos);
                else if (monster != null) yield return MonsterAttackRoutine(u, monster, target);
                else yield return AiAttack(u, target, melee);
            }
        }

        /// <summary>A companion's Attack action, using every attack Extra Attack gives (switching targets if one falls).</summary>
        IEnumerator CompanionAttacks(Unit u, Unit target, AttackOption atk)
        {
            yield return AttackAction(u, target, atk);
            while (u.Active && u.Cb.Turn.AttacksLeftInAction > 0)
            {
                u.Cb.Turn.AttacksLeftInAction--;
                if (!target.Active) target = Units.Where(o => o.Active && Hostile(u, o) && Zone.DistanceFt(u.Pos, o.Pos) <= atk.MaxRangeFt).OrderBy(o => o.C.HP).FirstOrDefault();
                if (target == null) yield break;
                yield return AttackAction(u, target, atk, false);
            }
        }

        /// <summary>Multiattack: each attack in the routine, switching to another adjacent foe if the first falls.</summary>
        IEnumerator MonsterAttackRoutine(Unit u, Monster m, Unit target)
        {
            foreach (var atk in m.AttackRoutine())
            {
                if (!Monster.KeepsAttacking(m)) yield break;
                if (target == null || !target.Active || Zone.DistanceFt(u.Pos, target.Pos) > atk.MaxRangeFt)
                    target = Units.Where(o => o.Active && Hostile(u, o) && Zone.DistanceFt(u.Pos, o.Pos) <= atk.MaxRangeFt).OrderBy(o => o.C.HP).FirstOrDefault();
                if (target == null) yield break;
                yield return AiAttack(u, target, atk);
            }
        }

        /// <summary>A charged area ability (breath, spray) if it would catch at least two foes, or one when it's the only foe left.</summary>
        /// <summary>A breath it drew last turn goes off now, aimed wherever it catches the most foes.</summary>
        public IEnumerator ReleaseBreath(Unit u, Monster m)
        {
            var ab = m.Charging;
            m.Charging = null;
            u.A.Sr.color = Color.white;
            if (ab == null || !u.Active) yield break;
            var foes = Units.Where(o => Hostile(u, o) && o.Active).ToList();
            int tiles = Math.Max(1, ab.AreaFt / 5);
            Vector2Int best = u.Pos + Vector2Int.down; int bestHits = -1;
            foreach (var f in foes)
            {
                if (Zone.DistanceFt(u.Pos, f.Pos) > ab.AreaFt) continue;
                var area = AreaTiles(u, new CombatAction { AreaTiles = tiles, AreaFromSelf = true, AreaLine = ab.Shape == TargetShape.Line, RangeFt = ab.AreaFt }, f.Pos);
                int hits = foes.Count(o => area.Contains(o.Pos));
                if (hits > bestHits) { bestHits = hits; best = f.Pos; }
            }
            u.A.Face(Zone.ToWorld(best));
            Log($"{u.Name} lets go: {ab.Name}!");
            Sfx.Play("spell");
            var zoneTiles = AreaTiles(u, new CombatAction { AreaTiles = tiles, AreaFromSelf = true, AreaLine = ab.Shape == TargetShape.Line, RangeFt = ab.AreaFt }, best);
            yield return AbilitySaves(u, ab, zoneTiles);
        }

        /// <summary>Everyone in the area saves against the ability (the dragon's wings shelter friends from fire).</summary>
        IEnumerator AbilitySaves(Unit u, MonsterAbilityDef ab, List<Vector2Int> zoneTiles)
        {
            var dmg = Dice.RollDamage(ab.Dice);
            Ticker($"{u.Name}: {ab.Name}: {dmg.Breakdown()} · {ab.SaveAbility} save DC {ab.SaveDC}");
            yield return new WaitForSeconds(0.5f);
            foreach (var t in Units.Where(o => o != u && o.Active && zoneTiles.Contains(o.Pos)).ToList())
            {
                var lines = new List<string>();
                bool sheltered = ab.DamageType == DamageType.Fire && !(t.C is Dragonling) && Units.Any(o => o != t && o.C is Dragonling && o.Active && !Hostile(o, t) && o.C.Immunities.Contains(DamageType.Fire) && Zone.DistanceFt(o.Pos, t.Pos) <= 5);
                var save = Checks.Rescue(t.C, ab.SaveAbility, ab.SaveDC, Checks.RollSave(t.C, ab.SaveAbility, sheltered ? AdvantageState.Advantage : AdvantageState.Normal), lines);
                bool ok = Checks.ResolveSave(save, ab.SaveDC);
                if (sheltered) Log($"{t.Name} is sheltered by {G.World.DragonName}'s wings (Advantage on the save).");
                Ticker($"{t.Name} {ab.SaveAbility} save{(sheltered ? $" (sheltered by {G.World.DragonName}'s wings: Advantage)" : "")}: {save.Breakdown()} vs DC {ab.SaveDC} → {(ok ? "SAVED" : "FAILED")}");
                int lost = t.C.TakeDamage(Checks.AreaDamage(t.C, ab.SaveAbility, dmg.Total, ok, true), ab.DamageType, false, lines);
                lines.ForEach(Log);
                t.A.Popup("-" + lost, UIKit.DamageRed);
                if (lost > 0) AttackResolver.RaiseConcentrationCheck(t.C, lost);
                OnDamaged(t);
                yield return new WaitForSeconds(0.3f);
            }
        }

        public const int EruptionDC = 15;

        /// <summary>
        /// Lair eruptions this round: for each boss in its second phase, LairEruptions walkable tiles near the party (within
        /// 2 tiles of a hero, preferring tiles someone stands on). Empty when no boss is in phase two.
        /// </summary>
        public List<Vector2Int> PickEruptionTiles(System.Random rng)
        {
            var tiles = new List<Vector2Int>();
            foreach (var bu in Units)
            {
                if (!(bu.C is Monster m) || !bu.Active || !m.PhaseTwo || m.Def.LairEruptions <= 0) continue;
                var near = new List<Vector2Int>();
                foreach (var h in Units.Where(o => o.Active && Hostile(bu, o)))
                    for (int dx = -2; dx <= 2; dx++)
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            var t = h.Pos + new Vector2Int(dx, dy);
                            if (Zone.InBounds(t) && Zone.IsWalkable(t) && t != bu.Pos && !near.Contains(t) && !tiles.Contains(t)) near.Add(t);
                        }
                var occupied = near.Where(t => Units.Any(o => o.Active && Hostile(bu, o) && o.Pos == t)).ToList();
                for (int i = 0; i < m.Def.LairEruptions && near.Count > 0; i++)
                {
                    var pool = i == 0 && occupied.Count > 0 ? occupied : near;
                    var pick = pool[rng.Next(pool.Count)];
                    tiles.Add(pick);
                    near.Remove(pick);
                    occupied.Remove(pick);
                }
            }
            return tiles;
        }

        /// <summary>The floor bursts on these tiles: whoever stands there makes a DEX save, 2d10 fire, half on a success.</summary>
        public IEnumerator Erupt(List<Vector2Int> tiles)
        {
            Log("The floor erupts! Lava bursts up through the cracks.");
            foreach (var h in Humans) h.Hud.Toast("The floor erupts!", UIKit.DamageRed);
            Sfx.Play("spell");
            foreach (var t in tiles) EruptionFx(t);
            yield return new WaitForSeconds(0.4f);
            foreach (var u in Units.Where(o => o.Active && tiles.Contains(o.Pos)).ToList())
            {
                var dmg = Dice.RollDamage(DiceExpr.Parse("2d10"));
                var lines = new List<string>();
                var save = Checks.Rescue(u.C, Ability.DEX, EruptionDC, Checks.RollSave(u.C, Ability.DEX, AdvantageState.Normal), lines);
                bool ok = Checks.ResolveSave(save, EruptionDC);
                Ticker($"Eruption under {u.Name}: {dmg.Breakdown()} fire · DEX save {save.Breakdown()} vs DC {EruptionDC} → {(ok ? "SAVED" : "FAILED")}");
                int lost = u.C.TakeDamage(Checks.AreaDamage(u.C, Ability.DEX, dmg.Total, ok, true), DamageType.Fire, false, lines);
                lines.ForEach(Log);
                u.A.Popup(lost > 0 ? "-" + lost : "Immune", lost > 0 ? UIKit.DamageRed : UIKit.UtilityBlue);
                if (lost > 0) AttackResolver.RaiseConcentrationCheck(u.C, lost);
                OnDamaged(u);
                yield return new WaitForSeconds(0.25f);
            }
        }

        void EruptionFx(Vector2Int t)
        {
            var go = new GameObject("Eruption");
            go.transform.position = Zone.ToWorld(t);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArtRegistry.Get("fx_smoke");
            sr.color = new Color(1f, 0.45f, 0.15f, 0.9f);
            sr.sortingOrder = 25000;
            UnityEngine.Object.Destroy(go, 1.2f);
        }

        /// <summary>After another creature's turn: each legendary monster may take one legendary action.</summary>
        public IEnumerator LegendaryOpportunities(Unit justActed)
        {
            foreach (var u in Units.ToList())
            {
                if (u == justActed || !u.Active || !(u.C is Monster m) || m.Def.LegendaryPerRound <= 0 || m.LegendaryLeft <= 0) continue;
                var foes = Units.Where(o => Hostile(u, o) && o.Active && !Spares(u, o)).ToList();
                if (foes.Count == 0) yield break;
                foreach (var la in m.Def.LegendaryActions)
                {
                    if (la.Cost > m.LegendaryLeft) continue;
                    var inRange = foes.Where(f => Zone.DistanceFt(u.Pos, f.Pos) <= la.RangeFt).OrderBy(f => f.C.HP).ToList();
                    if (inRange.Count == 0) continue;
                    m.SpendLegendary(la.Cost);
                    var target = inRange[0];
                    Log($"Legendary action: {u.Name} uses {la.Name}!");
                    if (la.Kind == LegendaryKind.Attack)
                    {
                        var atk = m.GetAttacks().FirstOrDefault(a => a.Name == la.Attack);
                        if (atk == null) break;
                        bool spent = u.Cb.Turn.ActionUsed;
                        yield return AiAttack(u, target, atk);
                        u.Cb.Turn.ActionUsed = spent; // a legendary action isn't its Action
                    }
                    else
                    {
                        var dmg = Dice.RollDamage(la.Dice);
                        var lines = new List<string>();
                        var save = Checks.Rescue(target.C, la.SaveAbility, la.SaveDC, Checks.RollSave(target.C, la.SaveAbility, AdvantageState.Normal), lines);
                        bool ok = Checks.ResolveSave(save, la.SaveDC);
                        Ticker($"{u.Name}: {la.Name} → {target.Name}: {dmg.Breakdown()} · {la.SaveAbility} save {save.Breakdown()} vs DC {la.SaveDC} → {(ok ? "SAVED" : "FAILED")}");
                        int lost = target.C.TakeDamage(ok ? dmg.Total / 2 : dmg.Total, la.DamageType, false, lines);
                        lines.ForEach(Log);
                        target.A.Popup("-" + lost, UIKit.DamageRed);
                        if (lost > 0) AttackResolver.RaiseConcentrationCheck(target.C, lost);
                        OnDamaged(target);
                        yield return new WaitForSeconds(0.4f);
                    }
                    break; // one legendary action per opportunity
                }
            }
        }

        IEnumerator TryMonsterAbility(Unit u, Monster m, List<Unit> foes, Action<bool> used)
        {
            used(false);
            foreach (var ab in m.Def.Abilities)
            {
                if (!m.IsCharged(ab)) continue;
                int tiles = Math.Max(1, ab.AreaFt / 5);
                Vector2Int best = default; int bestHits = 0;
                foreach (var f in foes)
                {
                    if (Zone.DistanceFt(u.Pos, f.Pos) > ab.AreaFt) continue;
                    var area = AreaTiles(u, new CombatAction { AreaTiles = tiles, AreaFromSelf = true, AreaLine = ab.Shape == TargetShape.Line, RangeFt = ab.AreaFt }, f.Pos);
                    int hits = foes.Count(o => area.Contains(o.Pos));
                    if (hits > bestHits) { bestHits = hits; best = f.Pos; }
                }
                if (bestHits < Math.Min(2, foes.Count)) continue;
                m.Spend(ab);
                u.Cb.Turn.ActionUsed = true;
                if (ab.Telegraph)
                {
                    m.Charging = ab;
                    u.A.Face(Zone.ToWorld(best));
                    u.A.Sr.color = new Color(1f, 0.55f, 0.45f);
                    Log($"{u.Name} rears back, its throat glowing. (It will breathe at the start of its next turn: spread out!)");
                    Ticker($"{u.Name} draws a great breath. Its throat glows through the plates.");
                    foreach (var h in Humans) h.Hud.Toast($"{u.Name} is drawing breath! Spread out" + (m.Def.WeakSpotFlag != null && G.World.Flag(m.Def.WeakSpotFlag) ? ", or strike the glowing throat!" : "!"), UIKit.DamageRed);
                    used(true);
                    yield break;
                }
                u.A.Face(Zone.ToWorld(best));
                Log($"{u.Name} uses {ab.Name}!");
                Sfx.Play("spell");
                var zoneTiles = AreaTiles(u, new CombatAction { AreaTiles = tiles, AreaFromSelf = true, AreaLine = ab.Shape == TargetShape.Line, RangeFt = ab.AreaFt }, best);
                yield return AbilitySaves(u, ab, zoneTiles);
                used(true);
                yield break;
            }
        }

        /// <summary>
        /// A companion caster's levelled spells (Act II): mass heals when friends are hurting, a concentration buff
        /// (Spirit Guardians, Haste) when free, and the area spell that catches the most enemies and no friends.
        /// </summary>
        IEnumerator AiCastLeveled(Unit u, PlayerCharacter pc, List<Unit> foes, Action<bool> cast)
        {
            cast(false);
            var known = pc.Cantrips.Concat(pc.PreparedSpells).Concat(pc.BonusAlwaysPrepared).Distinct().Select(SpellsDb.Get)
                .Where(s => s != null && !s.IsCantrip && (s.CastTime == CastTime.Action || (s.Kind == SpellKind.Heal && s.CastTime == CastTime.BonusAction && !u.Cb.Turn.BonusActionUsed))
                    && pc.LowestAvailableSlot(s.Level) > 0).ToList();
            if (known.Count == 0 || u.Cb.Turn.CastLeveledSpellThisTurn) yield break;
            var friends = Units.Where(o => !Hostile(u, o) && !o.C.IsDead).ToList();

            // 1. Two or more friends below half: a mass heal on the most hurt one.
            var hurt = friends.Where(o => o.C.HP < o.C.EffectiveMaxHP / 2).ToList();
            var massHeal = known.Where(s => s.Kind == SpellKind.Heal && s.MaxTargets > 1).OrderByDescending(s => s.Level).FirstOrDefault();
            if (massHeal != null && hurt.Count >= 2)
            {
                var centre = hurt.OrderBy(o => o.C.HP).First();
                if (Zone.DistanceFt(u.Pos, centre.Pos) <= massHeal.RangeFt) { yield return CastSpell(u, pc, massHeal, centre.Pos); cast(massHeal.CastTime == CastTime.Action); yield break; }
            }
            // 2. Free concentration: Spirit Guardians when enemies are close, Haste on the front line.
            if (pc.ConcentratingOn == null)
            {
                var sg = known.FirstOrDefault(s => s.Id == "spirit_guardians");
                if (sg != null && foes.Count(f => Zone.DistanceFt(u.Pos, f.Pos) <= 15) >= 1) { yield return CastSpell(u, pc, sg, u.Pos); cast(true); yield break; }
                var haste = known.FirstOrDefault(s => s.Id == "haste");
                var front = friends.Where(o => o.Active && o != u && Zone.DistanceFt(u.Pos, o.Pos) <= haste?.RangeFt && foes.Any(f => Zone.DistanceFt(o.Pos, f.Pos) <= 5)).FirstOrDefault();
                if (haste != null && front != null && !front.C.HasEffect("haste")) { yield return CastSpell(u, pc, haste, front.Pos); cast(true); yield break; }
            }
            // 3. The best area damage spell: most enemies, no friends (Sculpt Spells forgives a few).
            Vector2Int best = default; SpellDef bestSpell = null; int bestScore = 1;
            foreach (var s in known.Where(s => s.Kind == SpellKind.Save && s.Shape != TargetShape.Single && !s.Dice.IsNone && s.ZoneLineTiles == 0 && (!s.Concentration || pc.ConcentratingOn == null)))
            {
                foreach (var f in foes)
                {
                    var probe = new CombatAction { AreaTiles = Math.Max(1, s.AreaFt / 5), AreaFromSelf = s.RangeFt == 0, AreaLine = s.Shape == TargetShape.Line, RangeFt = s.RangeFt == 0 ? s.AreaFt : s.RangeFt };
                    if (!probe.AreaFromSelf && Zone.DistanceFt(u.Pos, f.Pos) > s.RangeFt) continue;
                    var area = AreaTiles(u, probe, f.Pos);
                    int enemies = foes.Count(o => area.Contains(o.Pos));
                    int allies = friends.Count(o => o != u && area.Contains(o.Pos)) - SpellResolver.Sculpted(pc, s, friends.Where(o => area.Contains(o.Pos)).Select(o => o.C)).Count;
                    if (area.Contains(u.Pos) && !probe.AreaFromSelf) allies++;
                    if (allies > 0) continue;
                    int score = enemies * 10 + s.Level;
                    if (enemies >= 2 && score > bestScore) { bestScore = score; best = f.Pos; bestSpell = s; }
                }
            }
            if (bestSpell != null) { yield return CastSpell(u, pc, bestSpell, best); cast(true); }
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
            if (pc.ResourceLeft("wholeness") > 0 && pc.HP < pc.EffectiveMaxHP / 2) { u.Cb.Turn.BonusActionUsed = true; var wl = new List<string>(); pc.WholenessOfBody(wl); wl.ForEach(Log); yield break; }
            var smite = new[] { "banishing_smite", "staggering_smite", "blinding_smite" }.Select(SpellsDb.Get)
                .FirstOrDefault(s => s != null && pc.KnowsSpell(s.Id) && pc.LowestAvailableSlot(s.Level) > 0 && !pc.HasEffect(s.EffectId));
            if (smite != null && Zone.DistanceFt(u.Pos, target.Pos) <= pc.Speed + 5 && pc.CanCastSpells) { yield return CastSpell(u, pc, smite, u.Pos); yield break; }
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
                // Rogue 5: Uncanny Dodge (reaction) halves the hit.
                if (target.C is PlayerCharacter dodger && dodger.HasFeature("Uncanny Dodge") && ctx.TargetCanSeeAttacker)
                {
                    int full = dmg.Total, halved = AttackResolver.UncannyDodge(dodger, full, target.Cb.Turn);
                    if (halved < full) { dmg.Terms.Add(new RollTerm("uncanny dodge", -(full - halved))); target.A.Popup("Dodge!", UIKit.UtilityBlue); Log($"{dodger.Name} rolls with the blow (Uncanny Dodge: half damage)."); }
                }
                ApplyHitResults(me, target, res, dmg);
                yield return new WaitForSeconds(0.35f);
                // Berserker 10: Retaliation: hit back at whoever just hurt you from 5 ft.
                if (target.C is PlayerCharacter brute && brute.HasFeature("Retaliation") && target.Active && me.Active && !target.Cb.Turn.ReactionUsed && Zone.DistanceFt(target.Pos, me.Pos) <= 5)
                {
                    target.Cb.Turn.ReactionUsed = true;
                    Log($"{brute.Name} retaliates!");
                    var melee = brute.GetAttacks().FirstOrDefault(a => !a.Ranged);
                    if (melee != null) yield return AttackAction(target, me, melee, false);
                }
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
            if (!ok) EndConcentration(c);
        }
    }
}
