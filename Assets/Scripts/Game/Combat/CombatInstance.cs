using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>A creature taking part in a fight, plus its sprite and controller.</summary>
    public class Unit
    {
        public Creature C;
        public Actor A;
        public PlayerSlot Owner;          // a human player's own character
        public EnemyAgent Enemy;
        public CompanionAgent Comp;
        public Combatant Cb;
        public int DeathRound = -1;
        public Vector2Int Pos => A.MapPos;
        /// <summary>Who makes this unit's decisions and rolls its dice: its player, or the leader of a companion under manual command.</summary>
        public PlayerSlot Ctrl => Owner ?? (Comp != null && Comp.Manual && Comp.Leader != null && Comp.Leader.InCombat ? Comp.Leader : null);
        public bool IsHuman => Ctrl != null;
        public string Name => C.Name;
        public bool Active => !C.IsDead && !C.IsDown;
    }

    /// <summary>
    /// One turn-based fight. Several can run at once (co-op players in different places).
    /// Positions snap to a 5-ft grid; the game clock is paused for the participants.
    /// </summary>
    public partial class CombatInstance
    {
        public Zone Zone;
        public string GroupId;
        public readonly Encounter Enc = new Encounter();
        public readonly List<Unit> Units = new List<Unit>();
        public bool Finished;
        public Unit Current => Units.FirstOrDefault(u => Enc.Current != null && u.C == Enc.Current.Creature);
        readonly GameRoot G;
        readonly List<GameObject> _gridTiles = new List<GameObject>();
        readonly List<string> _log = new List<string>();
        bool _surprise, _enemiesSurprised;

        public CombatInstance(GameRoot g, Zone zone, string groupId) { G = g; Zone = zone; GroupId = groupId; }

        // ================================================================== setup

        public Unit AddUnit(Creature c, Actor a, PlayerSlot owner = null, EnemyAgent enemy = null, CompanionAgent comp = null)
        {
            var existing = Units.FirstOrDefault(x => x.C == c);
            if (existing != null) return existing;
            var u = new Unit { C = c, A = a, Owner = owner, Enemy = enemy, Comp = comp };
            Units.Add(u);
            if (enemy != null) enemy.InCombat = true;
            if (owner != null) owner.Combat = this;
            SnapUnit(u);
            return u;
        }

        void SnapUnit(Unit u)
        {
            var p = u.Pos;
            if (!Zone.IsWalkable(p) || Units.Any(o => o != u && o.Pos == p && !o.C.IsDead))
            {
                for (int r = 1; r < 4; r++)
                    for (int dx = -r; dx <= r; dx++)
                        for (int dy = -r; dy <= r; dy++)
                        {
                            var q = p + new Vector2Int(dx, dy);
                            if (Zone.IsWalkable(q) && !Units.Any(o => o != u && o.Pos == q && !o.C.IsDead)) { u.A.PlaceAt(Zone, q); goto done; }
                        }
            }
            u.A.SnapToTile();
            done:
            var rb = u.A.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;
        }

        /// <summary>Runs the fight. Surprised creatures roll initiative with Disadvantage (2024 rules).</summary>
        public IEnumerator Run(bool playersSurprised, bool enemiesSurprised = false)
        {
            _surprise = playersSurprised;
            _enemiesSurprised = enemiesSurprised;
            Sfx.Play("combat_start");
            foreach (var h in Humans) { h.Hud.ShowLog(true); h.Hud.CloseAll(); h.Hud.Push(new CombatHud(this)); h.Hud.Narrate(enemiesSurprised ? "AMBUSH! They never saw you coming. Roll for initiative!" : "Roll for initiative!"); }
            DeployFrontline();
            DrawGrid(true);

            // Initiative: humans roll their own d20s, everyone else rolls on the ticker.
            foreach (var u in Units.ToList())
            {
                var adv = u.C.InitiativeAdvantage ? AdvantageState.Advantage : AdvantageState.Normal;
                if (_surprise && u.C.Team == Team.Players) adv = Dice.Combine(adv == AdvantageState.Advantage, true);
                if (_enemiesSurprised && u.C.Team == Team.Enemies) adv = Dice.Combine(adv == AdvantageState.Advantage, true);
                var terms = new List<RollTerm> { new RollTerm("initiative", u.C.InitiativeBonus) };
                if (u.C.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", u.C.D20Penalty));
                if (u.IsHuman)
                {
                    D20Result res = null;
                    u.Ctrl.Hud.Dice.RollD20(u.Owner != null ? "Initiative!" : $"Initiative: {u.Name}", $"DEX {AbilityUtil.Fmt(u.C.InitiativeBonus)}{(adv == AdvantageState.Disadvantage ? " · surprised (Disadvantage)" : adv == AdvantageState.Advantage ? " · Advantage" : "")}",
                        () => Dice.RollD20(adv, terms), null, false, r => res = r);
                    while (res == null) yield return null;
                    u.Cb = Enc.Add(u.C);
                    u.Cb.InitiativeRoll = res;
                }
                else
                {
                    u.Cb = Enc.Add(u.C);
                    u.Cb.InitiativeRoll = Dice.RollD20(adv, terms);
                }
            }
            Enc.Sort();
            Log("Initiative: " + string.Join(", ", Enc.Order.Select(o => $"{o.Creature.Name} {o.Initiative}")));
            // Monk: Uncanny Metabolism, once per long rest when initiative is rolled.
            foreach (var u in Units)
                if (u.C is PlayerCharacter m && m.ResourceLeft("uncanny_metabolism") > 0 && m.ResourceLeft("focus") < m.ResourceMax("focus"))
                {
                    m.SpendResource("uncanny_metabolism");
                    m.ResourcesUsed.Remove("focus");
                    int h = m.Heal(Dice.Roll(m.MartialArtsDie) + m.Level);
                    Log($"{m.Name}'s Uncanny Metabolism: Focus restored, +{h} HP.");
                }

            while (!Finished)
            {
                var cur = Current;
                if (cur == null) { Enc.Advance(); continue; }
                var log = new List<string>();
                Enc.StartTurn(log);
                foreach (var l in log) Log(l);
                RefreshTracker();

                if (cur.C.IsDead) { /* skip */ }
                else if (cur.C.IsDown)
                {
                    if (cur.C is PlayerCharacter pc && !pc.Stable) yield return DeathSave(cur, pc);
                }
                else if (cur.C.IsIncapacitated) Log($"{cur.Name} is {(cur.C.Has(Condition.Paralyzed) ? "paralyzed" : "incapacitated")} and loses the turn.");
                else if (cur.IsHuman) yield return PlayerTurn(cur);
                else yield return AiTurn(cur);

                if (CheckEnd()) break;
                var endLog = new List<string>();
                Enc.EndTurn(endLog);
                foreach (var l in endLog) Log(l);
                Enc.Advance();
                yield return null;
            }
        }

        /// <summary>The human players in this fight (each has a combat HUD).</summary>
        public IEnumerable<PlayerSlot> Humans => Units.Where(u => u.Owner != null).Select(u => u.Owner);

        /// <summary>
        /// As the fight starts, front-line companions (and any told to take point) step between their leader and
        /// the nearest enemy, so the squishy party members start behind them.
        /// </summary>
        void DeployFrontline()
        {
            var foes = Units.Where(u => u.C.Team == Team.Enemies && u.Active).ToList();
            if (foes.Count == 0) return;
            foreach (var u in Units.Where(u => u.Comp != null && u.Active && (u.Comp.IsFrontline || u.Comp.TakePoint)).ToList())
            {
                var leader = Units.FirstOrDefault(o => o.Owner != null && o.Owner == u.Comp.Leader);
                if (leader == null) continue;
                var foe = foes.OrderBy(f => Zone.DistanceFt(f.Pos, leader.Pos)).First();
                int gap = Zone.DistanceFt(foe.Pos, leader.Pos) / 5;
                if (gap < 2) continue; // already toe to toe: no room to get in front
                Vector2 dir = ((Vector2)(foe.Pos - leader.Pos)).normalized;
                int step = Math.Min(2, gap - 1);
                var want = leader.Pos + new Vector2Int(Mathf.RoundToInt(dir.x * step), Mathf.RoundToInt(dir.y * step));
                Vector2Int? best = null;
                float bestScore = float.MaxValue;
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        var t = want + new Vector2Int(dx, dy);
                        if (!Zone.IsWalkable(t) || Occupied(t, u)) continue;
                        if (Zone.DistanceFt(t, foe.Pos) >= Zone.DistanceFt(leader.Pos, foe.Pos)) continue; // must actually be in front
                        float score = Vector2.Distance(t, want) + (Zone.LineOfSight(leader.Pos, t) ? 0 : 3);
                        if (score < bestScore) { bestScore = score; best = t; }
                    }
                if (!best.HasValue) continue;
                u.A.PlaceAt(Zone, best.Value);
                u.A.Face(foe.A.transform.position);
                Log($"{u.Name} steps in front of {leader.Name}!");
            }
        }

        public void JoinLate(PlayerSlot p)
        {
            var u = AddUnit(p.Character, p.Avatar, p);
            u.Cb = Enc.Join(p.Character);
            p.Hud.ShowLog(true);
            p.Hud.CloseAll();
            p.Hud.Push(new CombatHud(this));
            p.Hud.Narrate($"You rush into the fight! (Initiative {u.Cb.Initiative})");
            foreach (var c in G.CompanionsOf(p)) if (c.Character != null) { var cu = AddUnit(c.Character, c.Actor, comp: c); cu.Cb = Enc.Join(c.Character); }
            DrawGrid(true);
            RefreshTracker();
        }

        // ================================================================== helpers

        public void Log(string line)
        {
            _log.Add(line);
            foreach (var h in Humans) h.Hud.AddLog(line);
        }

        void Ticker(string text)
        {
            foreach (var h in Humans) h.Hud.Dice.Ticker(text);
            Log(text);
        }

        public Unit UnitAt(Vector2Int p) => Units.FirstOrDefault(u => u.Pos == p && !u.C.IsDead) ?? Units.FirstOrDefault(u => u.Pos == p);
        public bool Occupied(Vector2Int p, Unit except = null) => Units.Any(u => u != except && u.Pos == p && !u.C.IsDead);
        public static bool Hostile(Unit a, Unit b) => a.C.Team != b.C.Team;

        public AttackContext Context(Unit attacker, Unit target)
        {
            int dist = Zone.DistanceFt(attacker.Pos, target.Pos);
            bool dark = Zone.Def.Dark;
            bool attackerSees = !dark || attacker.C.Darkvision >= dist || LitNear(target.A.transform.position) || LitNear(attacker.A.transform.position);
            bool targetSees = !dark || target.C.Darkvision >= dist || LitNear(attacker.A.transform.position);
            return new AttackContext
            {
                DistanceFt = dist,
                AttackerCanSeeTarget = attackerSees && Zone.LineOfSight(attacker.Pos, target.Pos),
                TargetCanSeeAttacker = targetSees,
                AllyAdjacentToTarget = Units.Any(o => o != attacker && o.Active && o.C.Team == attacker.C.Team && Zone.DistanceFt(o.Pos, target.Pos) <= 5),
                EnemyAdjacentToAttacker = Units.Any(o => o.Active && Hostile(o, attacker) && Zone.DistanceFt(o.Pos, attacker.Pos) <= 5),
                AttackerInSunlight = !Zone.Def.Indoors && !Zone.Def.Dark && !G.Clock.IsNight,
            };
        }

        static bool LitNear(Vector3 p)
        {
            foreach (var l in LightSource.All) if (l.On && l.Radius > 0 && Vector2.Distance(l.transform.position, p) <= l.Radius) return true;
            return false;
        }

        bool CheckEnd()
        {
            bool playersUp = Units.Any(u => u.C.Team == Team.Players && u.Active);
            bool enemiesUp = Units.Any(u => u.C.Team == Team.Enemies && u.Active);
            if (playersUp && enemiesUp) return false;
            Finish(playersUp);
            return true;
        }

        // ================================================================== end of combat

        void Finish(bool victory)
        {
            Finished = true;
            DrawGrid(false);
            ClearHighlights();
            foreach (var h in Humans) { h.Hud.ShowLog(false); h.Hud.Find<CombatHud>()?.Close(); }
            if (victory)
            {
                var foes = Units.Where(u => u.C.Team == Team.Enemies).ToList();
                int xp = foes.Where(u => u.C.IsDown).Sum(u => ((Monster)u.C).Def.XP);
                var humans = Humans.Where(h => h.Character != null).ToList();
                // XP is split between the players only. Companions earn the same as their leader (GiveXP),
                // so recruiting help never shrinks your share, and talking a group down pays exactly the same.
                int share = GameRoot.EncounterShare(xp, humans.Count);
                foreach (var f in foes) if (f.Enemy != null) f.Enemy.gameObject.SetActive(false);
                foreach (var h in humans)
                {
                    h.Hud.Narrate($"Victory!{(share > 0 ? $" +{share} XP each." : "")}");
                    G.GiveXP(h, share, "victory");
                }
                G.SplitLoot(humans, foes.Select(f => ((Monster)f.C).Def));
                if (GroupId != null && foes.All(f => f.C.IsDown)) { G.World.DefeatedGroups.Add(GroupId); G.World.DefeatedDay[GroupId] = G.Clock.Day; }
            }
            ReleaseUnits();
            G.OnCombatFinished(this, victory);
        }

        /// <summary>The party got away: nobody wins, and the monsters stay where they are, wounds and all.</summary>
        public void Retreat()
        {
            Finished = true;
            DrawGrid(false);
            ClearHighlights();
            foreach (var h in Humans) { h.Hud.ShowLog(false); h.Hud.Find<CombatHud>()?.Close(); }
            foreach (var u in Units) if (u.C is PlayerCharacter pc && pc.IsDown && !pc.IsDead) pc.Stable = true; // carried out by friends
            // Enemies you dropped before pulling out still count: they won't be there when you come back.
            var dropped = Units.Where(u => u.C.Team == Team.Enemies && u.C.IsDown).ToList();
            int share = GameRoot.EncounterShare(dropped.Sum(u => ((Monster)u.C).Def.XP), Humans.Count());
            if (share > 0) foreach (var h in Humans.Where(h => h.Character != null).ToList()) G.GiveXP(h, share, "the foes you dropped");
            ReleaseUnits();
            G.OnCombatRetreat(this);
        }

        void ReleaseUnits()
        {
            foreach (var u in Units)
            {
                if (u.Enemy != null) u.Enemy.InCombat = false;
                if (u.Owner != null) u.Owner.Combat = null;
                // Out of combat, effects with round durations simply end.
                u.C.Effects.RemoveAll(e => e.RoundsLeft > 0 && e.Id != "aid" && e.Id != "mage_armor");
            }
        }

        // ================================================================== grid display

        void DrawGrid(bool on)
        {
            foreach (var g in _gridTiles) if (g) UnityEngine.Object.Destroy(g);
            _gridTiles.Clear();
            if (!on || Units.Count == 0) return;
            int minX = Units.Min(u => u.Pos.x) - 10, maxX = Units.Max(u => u.Pos.x) + 10;
            int minY = Units.Min(u => u.Pos.y) - 8, maxY = Units.Max(u => u.Pos.y) + 8;
            var spr = ArtRegistry.Get("tile_grid");
            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                {
                    var m = new Vector2Int(x, y);
                    if (!Zone.IsWalkable(m)) continue;
                    var go = new GameObject("grid");
                    go.transform.position = Zone.ToWorld(m);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    sr.sortingOrder = -900;
                    _gridTiles.Add(go);
                }
        }

        readonly List<GameObject> _highlights = new List<GameObject>();
        GameObject _cursor;

        void Highlight(IEnumerable<Vector2Int> tiles, Color c)
        {
            ClearHighlights();
            var spr = ArtRegistry.Get("tile_highlight");
            foreach (var t in tiles)
            {
                var go = new GameObject("hl");
                go.transform.position = Zone.ToWorld(t);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.color = c;
                sr.sortingOrder = -800;
                _highlights.Add(go);
            }
        }

        void ClearHighlights()
        {
            foreach (var g in _highlights) if (g) UnityEngine.Object.Destroy(g);
            _highlights.Clear();
            if (_cursor) _cursor.SetActive(false);
            SetHover(null, Color.clear);
        }

        Unit _hovered;

        void SetHover(Unit u, Color c)
        {
            if (_hovered != null && _hovered != u && _hovered.A) _hovered.A.Highlight = null;
            _hovered = u;
            if (u != null && u.A) u.A.Highlight = c;
        }

        /// <summary>
        /// The unit whose sprite is under a world point. Sprites stand on their tile but draw up into the tile
        /// above, so picking by sprite (front-most first) is what makes clicking on a creature's body work.
        /// </summary>
        public Unit UnitUnderPoint(Vector3 w)
        {
            Unit best = null;
            foreach (var u in Units)
            {
                if (u.C.IsDead || u.A == null || u.A.Sr == null || u.A.Sr.sprite == null) continue;
                var b = u.A.Sr.bounds;
                if (w.x < b.min.x || w.x > b.max.x || w.y < b.min.y || w.y > b.max.y) continue;
                if (best == null || u.A.transform.position.y < best.A.transform.position.y) best = u;
            }
            return best;
        }

        void ShowCursor(Vector2Int t, Color c)
        {
            if (_cursor == null)
            {
                _cursor = new GameObject("CombatCursor");
                var sr = _cursor.AddComponent<SpriteRenderer>();
                sr.sprite = ArtRegistry.Get("tile_cursor");
                sr.sortingOrder = -750; // on the floor under creatures, not a box drawn over them
            }
            _cursor.SetActive(true);
            _cursor.transform.position = Zone.ToWorld(t);
            _cursor.GetComponent<SpriteRenderer>().color = c;
            var cur = Current;
            var under = UnitAt(t);
            SetHover(under != null && under != cur && !under.C.IsDead && c.a > 0.5f && c != Color.white ? under : null, c);
        }

        public void RefreshTracker()
        {
            foreach (var h in Humans) h.Hud.Find<CombatHud>()?.RefreshTracker(this);
        }
    }
}
