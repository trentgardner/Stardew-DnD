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
        public PlayerSlot Owner;          // human-controlled
        public EnemyAgent Enemy;
        public CompanionAgent Comp;
        public Combatant Cb;
        public int DeathRound = -1;
        public Vector2Int Pos => A.MapPos;
        public bool IsHuman => Owner != null;
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
        bool _surprise;

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

        public IEnumerator Run(bool playersSurprised)
        {
            _surprise = playersSurprised;
            Sfx.Play("combat_start");
            foreach (var h in Humans) { h.Hud.ShowLog(true); h.Hud.CloseAll(); h.Hud.Push(new CombatHud(this)); h.Hud.Narrate("Roll for initiative!"); }
            DrawGrid(true);

            // Initiative: humans roll their own d20s, everyone else rolls on the ticker.
            foreach (var u in Units.ToList())
            {
                var adv = u.C.InitiativeAdvantage ? AdvantageState.Advantage : AdvantageState.Normal;
                if (_surprise && u.C.Team == Team.Players) adv = Dice.Combine(adv == AdvantageState.Advantage, true);
                var terms = new List<RollTerm> { new RollTerm("initiative", u.C.InitiativeBonus) };
                if (u.C.D20Penalty != 0) terms.Add(new RollTerm("exhaustion", u.C.D20Penalty));
                if (u.IsHuman)
                {
                    D20Result res = null;
                    u.Owner.Hud.Dice.RollD20("Initiative!", $"DEX {AbilityUtil.Fmt(u.C.InitiativeBonus)}{(adv == AdvantageState.Disadvantage ? " · surprised (Disadvantage)" : adv == AdvantageState.Advantage ? " · Advantage" : "")}",
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

        public IEnumerable<PlayerSlot> Humans => Units.Where(u => u.IsHuman).Select(u => u.Owner);

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
                var party = Units.Where(u => u.C.Team == Team.Players && u.C is PlayerCharacter).ToList();
                int share = party.Count > 0 ? xp / party.Count : 0;
                foreach (var u in party) ((PlayerCharacter)u.C).XP += share;
                int copper = 0;
                var items = new List<string>();
                foreach (var f in foes)
                {
                    var def = ((Monster)f.C).Def;
                    copper += UnityEngine.Random.Range(def.CopperMin, def.CopperMax + 1);
                    foreach (var kv in def.Loot) if (UnityEngine.Random.value <= kv.Value) items.Add(kv.Key);
                    if (f.Enemy != null) f.Enemy.gameObject.SetActive(false);
                }
                var humans = Humans.Where(h => h.Character != null).ToList();
                foreach (var h in humans)
                {
                    h.Character.Copper += copper / Math.Max(1, humans.Count);
                    h.Hud.Narrate($"Victory! {(share > 0 ? $"+{share} XP each." : "")} {(copper > 0 ? $"You split {Currency.Format(copper)} in loot." : "")}");
                    if (h.Character.CanLevelUp) h.Hud.Toast("★ You can level up! (Character sheet: C / View)", UIKit.D20Gold);
                }
                foreach (var it in items)
                {
                    var taker = humans.FirstOrDefault(h => h.Character.Bag.SpaceFor(it) > 0) ?? humans.FirstOrDefault();
                    if (taker == null) continue;
                    taker.Character.AddItem(it);
                    foreach (var h in humans) h.Hud.Toast($"{taker.Character.Name} found: {ItemsDb.Get(it)?.Name}", UIKit.Gold);
                    if (it == "copper_kettle" || it == "lucky_spoon") G.Quests.Set(it == "copper_kettle" ? "kettle" : "spoon", 1);
                }
                if (GroupId != null && foes.All(f => f.C.IsDown)) { G.World.DefeatedGroups.Add(GroupId); G.World.DefeatedDay[GroupId] = G.Clock.Day; }
            }
            foreach (var u in Units)
            {
                if (u.Enemy != null) u.Enemy.InCombat = false;
                if (u.Owner != null) u.Owner.Combat = null;
                // Out of combat, effects with round durations simply end.
                u.C.Effects.RemoveAll(e => e.RoundsLeft > 0 && e.Id != "aid" && e.Id != "mage_armor");
            }
            G.OnCombatFinished(this, victory);
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
        }

        void ShowCursor(Vector2Int t, Color c)
        {
            if (_cursor == null)
            {
                _cursor = new GameObject("CombatCursor");
                var sr = _cursor.AddComponent<SpriteRenderer>();
                sr.sprite = ArtRegistry.Get("tile_cursor");
                sr.sortingOrder = 31000;
            }
            _cursor.SetActive(true);
            _cursor.transform.position = Zone.ToWorld(t);
            _cursor.GetComponent<SpriteRenderer>().color = c;
        }

        public void RefreshTracker()
        {
            foreach (var h in Humans) h.Hud.Find<CombatHud>()?.RefreshTracker(this);
        }
    }
}
