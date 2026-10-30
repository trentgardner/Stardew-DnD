using System.Collections;
using System.Collections.Generic;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// A sprite that walks around: players, NPCs, monsters, companions.
    /// Sprite ids: "{Look}_{down|up|side}_{0|1}" for directional looks, "{Look}_{0|1}" otherwise.
    /// </summary>
    public class Actor : MonoBehaviour
    {
        public string Look;
        /// <summary>The look this actor spawned with (what Wild Shape reverts to).</summary>
        public string HomeLook;
        public bool Directional = true;
        public Zone Zone;
        public Creature Creature;
        public Vector2 Facing = Vector2.down;
        public bool Moving;
        public SpriteRenderer Sr;
        float _animT;
        int _frame;
        string _lastId;
        public Color BaseColor = Color.white;
        /// <summary>Pulsing tint while this actor is hovered as a target in combat (null = none).</summary>
        public Color? Highlight;

        public Vector2Int MapPos => Zone != null ? Zone.ToMap(transform.position) : Vector2Int.zero;

        public static T Spawn<T>(string name, string look, bool directional, Zone zone, Vector2Int mapPos) where T : Actor
        {
            var go = new GameObject(name);
            var a = go.AddComponent<T>();
            a.Look = a.HomeLook = look;
            a.Directional = directional;
            a.Sr = go.AddComponent<SpriteRenderer>();
            go.AddComponent<YSort>();
            a.PlaceAt(zone, mapPos);
            a.RefreshSprite();
            return a;
        }

        public void PlaceAt(Zone zone, Vector2Int mapPos)
        {
            Zone = zone;
            transform.SetParent(zone.transform, true);
            transform.position = zone.ToWorld(mapPos);
            var rb = GetComponent<Rigidbody2D>();
            if (rb) rb.position = transform.position;
        }

        protected virtual void LateUpdate()
        {
            _animT += Time.deltaTime;
            if (_animT > 0.18f) { _animT = 0; _frame = Moving ? (_frame + 1) % _frameCount : 0; }
            if (!Moving) _frame = 0;
            RefreshSprite();
        }

        int _frameCount = 2;
        string _artLook, _artLookFor;
        bool _artDirFor;

        /// <summary>
        /// The most specific look with real art: "hero_fighter_elf" → "hero_fighter" when there's no elf-specific art,
        /// "npc_villager_tobin" → "npc_villager". Falls back to the full look (placeholder art) if nothing is real.
        /// </summary>
        string ArtLook()
        {
            if (_artLookFor == Look && _artDirFor == Directional) return _artLook;
            _artLookFor = Look;
            _artDirFor = Directional;
            _artLook = Look;
            for (string look = Look; ; )
            {
                if (ArtRegistry.IsReal(Directional ? look + "_down_0" : look + "_0")) { _artLook = look; break; }
                int cut = look.LastIndexOf('_');
                if (cut <= look.IndexOf('_')) break; // keep at least "kind_name"
                look = look.Substring(0, cut);
            }
            _frameCount = ArtRegistry.IsReal(Directional ? _artLook + "_down_3" : _artLook + "_3") ? 4 : 2;
            return _artLook;
        }

        public void RefreshSprite()
        {
            if (Sr == null) return;
            string id, look = ArtLook();
            if (_frame >= _frameCount) _frame = 0;
            if (Directional)
            {
                string dir = Mathf.Abs(Facing.x) > Mathf.Abs(Facing.y) ? "side" : (Facing.y > 0 ? "up" : "down");
                id = $"{look}_{dir}_{_frame}";
                Sr.flipX = dir == "side" && Facing.x < 0;
            }
            else
            {
                id = $"{look}_{_frame}";
                Sr.flipX = Facing.x < 0;
            }
            if (id != _lastId) { Sr.sprite = ArtRegistry.Get(id); _lastId = id; }
            var c = BaseColor;
            if (Creature != null && Creature.IsDead) c = new Color(0.7f, 0.85f, 1f, 0.45f); // spirit
            else if (Creature != null && Creature.IsDown) c = new Color(0.8f, 0.6f, 0.6f, 1f);
            if (Highlight.HasValue) c = Color.Lerp(c, Highlight.Value, 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f));
            Sr.color = c;
            transform.localRotation = Creature != null && Creature.IsDown && !Creature.IsDead ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
        }

        public void Face(Vector3 target)
        {
            Vector2 d = target - transform.position;
            if (d.sqrMagnitude > 0.001f) Facing = d.normalized;
        }

        /// <summary>Walks tile to tile along a path (combat movement, NPC routines).</summary>
        public IEnumerator WalkPath(List<Vector2Int> path, float tilesPerSecond, System.Func<Vector2Int, bool> onStep = null)
        {
            var rb = GetComponent<Rigidbody2D>();
            Moving = true;
            foreach (var step in path)
            {
                Vector3 target = Zone.ToWorld(step);
                Face(target);
                while ((transform.position - target).sqrMagnitude > 0.0004f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, tilesPerSecond * Time.deltaTime);
                    if (rb) rb.position = transform.position;
                    yield return null;
                }
                transform.position = target;
                if (onStep != null && !onStep(step)) break;
            }
            Moving = false;
        }

        public void SnapToTile()
        {
            transform.position = Zone.ToWorld(MapPos);
            var rb = GetComponent<Rigidbody2D>();
            if (rb) rb.position = transform.position;
        }

        /// <summary>Floating combat text above the sprite.</summary>
        public void Popup(string text, Color color)
        {
            var go = new GameObject("Popup");
            go.transform.position = transform.position + new Vector3(0, 1.1f, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.color = color;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.font = UIKit.Font;
            go.GetComponent<MeshRenderer>().material = UIKit.Font.material;
            go.GetComponent<MeshRenderer>().sortingOrder = 32000;
            go.AddComponent<FloatAway>();
        }
    }

    public class FloatAway : MonoBehaviour
    {
        float _t;
        void Update()
        {
            _t += Time.deltaTime;
            transform.position += new Vector3(0, Time.deltaTime * 0.8f, 0);
            if (_t > 1.4f) Destroy(gameObject);
        }
    }

    // ===================================================================== player

    public class PlayerController : Actor
    {
        public PlayerSlot Slot;
        Rigidbody2D _rb;
        public bool Sneaking;
        public float TorchMinutes;      // remaining game-minutes of torchlight
        public float LightSpellMinutes; // remaining game-minutes of the Light cantrip
        LightSource _light;
        Interactable _focus;
        WarpDef _lastWarp;

        public static PlayerController Create(PlayerSlot slot, Zone zone, Vector2Int pos)
        {
            var pc = slot.Character;
            var look = LookFor(pc);
            var a = Spawn<PlayerController>("Player" + (slot.Index + 1), look, true, zone, pos);
            a.Slot = slot;
            a.Creature = pc;
            a._rb = a.gameObject.AddComponent<Rigidbody2D>();
            a._rb.gravityScale = 0;
            a._rb.freezeRotation = true;
            a._rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            a._rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = a.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.55f, 0.3f);
            col.offset = new Vector2(0, -0.35f);
            a._light = LightSource.Attach(a.gameObject, 0, new Color(1f, 0.8f, 0.5f));
            if (pc.WildShape != null) { a.Look = pc.WildShape.SpriteId; a.Directional = false; a.RefreshSprite(); } // loaded mid-Wild Shape
            return a;
        }

        public static string LookFor(PlayerCharacter pc) => $"hero_{pc.Class.Id}_{pc.Species.Id}";

        // Not on the frame a menu closed: the key that closed it (Esc, I, C...) must not reopen something.
        public bool CanControl => Slot.Character != null && !Slot.IsBusy && !Slot.Character.IsDown && GameRoot.I.InGame && Slot.Hud.LastClosedFrame != Time.frameCount;

        void Update()
        {
            if (_rb == null) return;
            if (!CanControl)
            {
                _rb.linearVelocity = Vector2.zero;
                if (!Slot.InCombat) Moving = false;
                Slot.Hud.SetPrompt(null);
                return;
            }

            var input = Slot.Input;
            Vector2 move = input.Move;
            float tilesPerSec = Slot.Character.Speed / 30f * 4.2f * (Sneaking ? 0.5f : 1f);
            _rb.linearVelocity = move * tilesPerSec;
            Moving = move.sqrMagnitude > 0.01f;
            if (Moving) Facing = move.normalized;

            if (input.SneakToggle)
            {
                Sneaking = !Sneaking;
                Slot.Hud.Toast(Sneaking ? "Sneaking (harder to notice)" : "Stopped sneaking");
            }
            BaseColor = Sneaking ? new Color(0.75f, 0.75f, 0.85f, 0.85f) : Color.white;

            // Warps (doors, mine entrance)
            var warp = Zone.WarpAt(MapPos);
            if (warp != null && warp != _lastWarp) { _lastWarp = warp; GameRoot.I.Warp(Slot, warp.ToZone, warp.ToPos, true); return; }
            if (warp == null) _lastWarp = null;

            // Interaction
            _focus = Interactable.Nearest(Slot, transform.position, Facing);
            string key = input.UsesMouse ? "[E]" : "(A)";
            Slot.Hud.SetPrompt(_focus != null ? $"{key} {_focus.PromptText(Slot)}" : null);
            if (_focus != null && input.Confirm) { _focus.Interact(Slot); return; }
            if (input.UsesMouse && input.MouseLeft)
            {
                var world = Slot.Cam.ScreenToWorldPoint(input.MouseScreen);
                world.z = 0;
                foreach (var i in Interactable.All)
                {
                    if (i == null || !i.isActiveAndEnabled || !i.Available(Slot)) continue;
                    if (Vector2.Distance(i.transform.position + new Vector3(0, 0.4f), world) < 0.7f && Vector2.Distance(i.transform.position, transform.position) <= i.Range + 0.6f)
                    { Face(i.transform.position); i.Interact(Slot); return; }
                }
            }

            // Menus
            if (input.Inventory) { Slot.Hud.Push(new InventoryScreen(null, null)); return; }
            if (input.CharacterSheet) { Slot.Hud.Push(new CharacterSheetScreen()); return; }
            if (input.Spells) { Slot.Hud.Push(new SpellbookScreen()); return; }
            if (input.Journal) { Slot.Hud.Push(new JournalScreen()); return; }
            if (input.Pause) { Slot.Hud.Push(new PauseScreen()); return; }

            // Hotbar: number keys use an item; gamepad LB/RB select, X uses.
            int hk = input.HotbarPressed;
            if (hk >= 0) { Slot.Hud.HotbarIndex = hk; ItemUse.UseFromBag(Slot, hk); }
            if (!input.UsesMouse)
            {
                if (input.NextTab) Slot.Hud.HotbarScroll(1);
                if (input.PrevTab) Slot.Hud.HotbarScroll(-1);
                if (input.AltAction) ItemUse.UseFromBag(Slot, Slot.Hud.HotbarIndex);
            }
            else if (input.Scroll != 0) Slot.Hud.HotbarScroll(input.Scroll > 0 ? -1 : 1);
        }

        public void TickLight(float gameMinutes)
        {
            if (TorchMinutes > 0) { TorchMinutes -= gameMinutes; if (TorchMinutes <= 0) { TorchMinutes = 0; Slot.Hud.Toast("Your torch gutters out.", UIKit.Gold); } }
            if (LightSpellMinutes > 0) LightSpellMinutes = Mathf.Max(0, LightSpellMinutes - gameMinutes);
            _light.Radius = (TorchMinutes > 0 || LightSpellMinutes > 0) ? 8f : 0f;
        }

        public bool HasLight => TorchMinutes > 0 || LightSpellMinutes > 0;

        public void LightTorch()
        {
            if (!Slot.Character.RemoveItem("torch")) { Slot.Hud.Toast("You have no torches."); return; }
            TorchMinutes = 60;
            Slot.Hud.Toast("You light a torch (1 hour).", UIKit.Gold);
        }
    }

    // ===================================================================== NPCs

    public class NpcAgent : Interactable
    {
        public NpcDef Def;
        public Actor Actor;
        ScheduleEntry _current;
        Coroutine _walk;
        public override float Range => 2.2f; // reach across shop counters
        public override bool Available(PlayerSlot p) => _current != null && _current.Zone != null;

        public static NpcAgent Create(NpcDef def)
        {
            var g = GameRoot.I;
            var a = Actor.Spawn<Actor>("NPC_" + def.Id, def.Look, true, g.Zones["town"], Vector2Int.zero);
            var n = a.gameObject.AddComponent<NpcAgent>();
            n.Def = def;
            n.Actor = a;
            n.UpdateSchedule(true);
            return n;
        }

        public string Activity => _current?.Activity ?? "";

        public void UpdateSchedule(bool teleport)
        {
            var g = GameRoot.I;
            var e = WorldDb.Current(Def, g.Clock.Hour, g.Clock.WeekdayIndex, g.World);
            if (e == _current && !teleport) return;
            _current = e;
            if (_walk != null) { StopCoroutine(_walk); _walk = null; Actor.Moving = false; }
            if (e == null || e.Zone == null) { Actor.Sr.enabled = false; return; }
            var zone = g.Zones[e.Zone];
            Actor.Sr.enabled = true;
            if (teleport || zone != Actor.Zone || !g.AnyPlayerIn(zone))
            {
                Actor.PlaceAt(zone, e.Pos);
                return;
            }
            var path = zone.FindPath(Actor.MapPos, e.Pos);
            if (path.Count > 0) _walk = StartCoroutine(Actor.WalkPath(path, 2.2f));
            else Actor.PlaceAt(zone, e.Pos);
        }

        public override string PromptText(PlayerSlot p) => $"Talk to {Def.Name}";

        public override void Interact(PlayerSlot p)
        {
            Actor.Face(p.Avatar.transform.position);
            p.Avatar.Face(transform.position);
            p.Hud.Push(new DialogueScreen(Def.Dialogue, this, null));
        }
    }

    // ===================================================================== enemies

    public class EnemyAgent : Actor
    {
        public Monster Monster;
        public EnemyGroupDef Group;
        public Vector2Int Home;
        float _think;
        float _wanderT;
        public bool InCombat;
        public bool Alerted;

        public static EnemyAgent Create(Zone zone, EnemyGroupDef group, EnemySpawn spawn)
        {
            var def = Rules.Content.MonstersDb.Get(spawn.MonsterId);
            var a = Spawn<EnemyAgent>(def.Name, def.SpriteId, false, zone, spawn.Pos);
            a.Monster = new Monster(def);
            a.Creature = a.Monster;
            a.Group = group;
            a.Home = spawn.Pos;
            a.gameObject.AddComponent<AmbushInteractable>().Enemy = a;
            return a;
        }

        void Update()
        {
            if (InCombat || Monster.IsDown) { if (!InCombat) gameObject.SetActive(false); return; }
            var g = GameRoot.I;
            if (!g.InGame) return;

            _wanderT -= Time.deltaTime;
            if (_wanderT <= 0)
            {
                _wanderT = Random.Range(2f, 5f);
                var target = Home + new Vector2Int(Random.Range(-1, 2), Random.Range(-1, 2));
                if (Zone.IsWalkable(target) && !Moving) StartCoroutine(WalkPath(Zone.FindPath(MapPos, target), 1.5f));
            }

            _think -= Time.deltaTime;
            if (_think > 0) return;
            _think = 0.25f;
            if (g.World.IsLocked("calm:" + Group.Id)) return; // the party slipped away or retreated: they've lost track of you for a while
            foreach (var p in g.Players)
            {
                if (!p.IsReady || p.Zone != Zone || p.InCombat || p.Character.IsDown || p.IsBusy) continue;
                float sight = Group.SightTiles * (p.Avatar.Sneaking ? 0.5f : 1f);
                // Without darkvision, monsters can only see players who carry light.
                if (Zone.Def.Dark && Monster.Darkvision <= 0 && !p.Avatar.HasLight) sight = Mathf.Min(sight, 1.5f);
                var pp = p.MapPos;
                float dist = Vector2.Distance(pp, MapPos);
                if (!p.Avatar.Sneaking && dist <= Group.SightTiles + 4 && !g.World.Flag("tip_sneak:" + p.Index))
                {
                    g.World.SetFlag("tip_sneak:" + p.Index);
                    p.Hud.Toast($"Enemies ahead! Sneak ({(p.Input.UsesMouse ? "Ctrl" : "L3")}) to slip past or ambush them.", UIKit.Gold);
                }
                if (dist > sight || !Zone.LineOfSight(MapPos, pp)) continue;
                if (p.Avatar.Sneaking && !StealthBeaten(p)) continue;
                g.OnEnemyNoticed(this, p);
                return;
            }
            // A companion walking point gets spotted first, so the encounter starts with them out in front.
            foreach (var c in g.Companions)
            {
                if (c == null || !c.TakePoint || c.Waiting || c.Character == null || c.Character.IsDown || c.Actor.Zone != Zone) continue;
                var leader = c.Leader;
                if (leader == null || !leader.IsReady || leader.Zone != Zone || leader.InCombat || leader.IsBusy || leader.Avatar.Sneaking) continue;
                var cp = c.Actor.MapPos;
                if (Vector2.Distance(cp, MapPos) > Group.SightTiles || !Zone.LineOfSight(MapPos, cp)) continue;
                if (Zone.Def.Dark && Monster.Darkvision <= 0 && !leader.Avatar.HasLight) continue;
                g.OnEnemyNoticed(this, leader);
                return;
            }
        }

        float _stealthCheckT;
        bool _stealthResult;
        /// <summary>Sneaking player: Stealth roll vs this monster's passive Perception, re-rolled every few seconds.</summary>
        bool StealthBeaten(PlayerSlot p)
        {
            if (Time.time < _stealthCheckT) return _stealthResult;
            _stealthCheckT = Time.time + 4f;
            var roll = Checks.RollSkill(p.Character, Skill.Stealth, AdvantageState.Normal);
            _stealthResult = roll.Total < Monster.PassivePerception;
            if (_stealthResult) p.Hud.Dice.Ticker($"Stealth: {roll.Breakdown()}: the {Monster.Name} spots you!");
            return _stealthResult;
        }
    }

    /// <summary>Sneaking up on an enemy who hasn't noticed you lets you start the fight on your terms (they're surprised).</summary>
    public class AmbushInteractable : Interactable
    {
        public EnemyAgent Enemy;
        public override float Range => 2.6f;
        public override bool Available(PlayerSlot p) =>
            p.Avatar != null && p.Avatar.Sneaking && Enemy.enabled && !Enemy.InCombat && !Enemy.Monster.IsDown && Enemy.Monster.Team == Team.Enemies;
        public override string PromptText(PlayerSlot p) => $"Ambush the {Enemy.Monster.Name.ToLower()}! (they're surprised)";
        public override void Interact(PlayerSlot p) => GameRoot.I.Ambush(Enemy.Group.Id, p);
    }

    // ===================================================================== companions

    /// <summary>How a companion fights when left to its own judgment.</summary>
    public enum CompanionStance { Auto, Frontline, Ranged }

    /// <summary>A recruited NPC adventurer or the dragonling. Follows its leader; fights alongside in combat.</summary>
    public class CompanionAgent : Interactable
    {
        public Actor Actor;
        public PlayerSlot Leader;
        public PlayerCharacter Character; // null for the dragonling
        public string Quirk;
        public bool IsDragon;
        /// <summary>In combat, the leader picks this companion's moves (BG3 style). Off = the companion plays its own turns.</summary>
        public bool Manual = true;
        public CompanionStance Stance;
        /// <summary>"Wait here": stays put instead of following (and won't be pulled into fights elsewhere).</summary>
        public bool Waiting;
        /// <summary>"Take point": walks a few steps ahead of the leader instead of behind, and steps up front when a fight starts.</summary>
        public bool TakePoint;
        public override float Range => 1.2f;

        /// <summary>Gets between the enemies and the party: fighters and clerics by default, or anyone told to.</summary>
        public bool IsFrontline => Stance == CompanionStance.Frontline ||
            Stance == CompanionStance.Auto && Character != null && (Character.Class.Id == "fighter" || Character.Class.Id == "cleric");

        public static CompanionAgent Create(PlayerSlot leader, string look, bool directional, PlayerCharacter pc)
        {
            var a = Actor.Spawn<Actor>(pc != null ? pc.Name : "Dragonling", look, directional, leader.Zone, leader.MapPos);
            a.Creature = pc;
            var c = a.gameObject.AddComponent<CompanionAgent>();
            c.Actor = a;
            c.Leader = leader;
            c.Character = pc;
            return c;
        }

        void Update()
        {
            if (IsDragon && GameRoot.I.DragonFighting) { Actor.Moving = false; return; } // holds its grid square
            if (IsDragon) PickNearestLeader();
            if (Leader == null || Leader.Avatar == null || Leader.InCombat && Character != null) return;
            if (Waiting) { Actor.Moving = false; return; }
            if (Actor.Zone != Leader.Zone) { Actor.PlaceAt(Leader.Zone, Leader.MapPos); return; }
            var target = FollowSpot();
            float d = Vector2.Distance(transform.position, target);
            Actor.Moving = d > 0.25f;
            if (d > 6f) transform.position = target;
            else if (Actor.Moving)
            {
                Actor.Face(target);
                transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Max(3.5f, d * 2f) * Time.deltaTime);
            }
        }

        /// <summary>
        /// The dragon belongs to the whole party: it follows whichever ready player is nearest (same zone first).
        /// It only switches when someone is clearly nearer (3+ tiles), so it never flickers between two players.
        /// </summary>
        void PickNearestLeader()
        {
            var g = GameRoot.I;
            if (g.Dragon != null && g.Dragon.MaxHP != Dragonling.MaxHPFor(g.PartyLevel)) g.Dragon.SetPartyLevel(g.PartyLevel);
            if (Leader != null && Leader.InCombat) return; // don't swap mid-fight
            bool leaderOk = Leader != null && Leader.IsReady;
            bool leaderHere = leaderOk && Leader.Zone == Actor.Zone;
            PlayerSlot best = null;
            float bestD = float.MaxValue;
            foreach (var p in g.Players)
            {
                if (!p.IsReady || p == Leader || p.InCombat || p.Zone != Actor.Zone) continue;
                float d = Vector2.Distance(p.MapPos, Actor.MapPos);
                if (d < bestD) { bestD = d; best = p; }
            }
            if (best == null) { if (!leaderOk) foreach (var p in g.Players) if (p.IsReady) { Leader = p; break; } return; }
            if (!leaderOk || !leaderHere || bestD + 3f <= Vector2.Distance(Leader.MapPos, Actor.MapPos)) Leader = best;
        }

        /// <summary>
        /// Each companion keeps its own spot in a loose formation (spread sideways, staggered rows) so they never stack
        /// on one tile. Point-takers walk ahead of the leader, everyone else behind; the dragonling rides at your side.
        /// </summary>
        Vector3 FollowSpot()
        {
            var leader = Leader.Avatar.transform.position;
            Vector2 f = Leader.Avatar.Facing.sqrMagnitude > 0.01f ? Leader.Avatar.Facing.normalized : Vector2.down;
            var side = new Vector2(-f.y, f.x);
            if (IsDragon) return leader + (Vector3)(side * 0.7f + f * 0.2f);
            var squad = new List<CompanionAgent>();
            foreach (var c in GameRoot.I.CompanionsOf(Leader)) if (!c.IsDragon && !c.Waiting && c.TakePoint == TakePoint) squad.Add(c);
            int i = Mathf.Max(0, squad.IndexOf(this)), n = Mathf.Max(1, squad.Count);
            float lateral = (i - (n - 1) / 2f) * 1.1f;
            float along = TakePoint ? 2f + (i % 2) * 0.4f : -(1.3f + (i % 2) * 0.5f);
            var spot = leader + (Vector3)(f * along + side * lateral);
            // Don't park inside a wall or a tree: fall back to straight behind the leader.
            if (!Leader.Zone.IsWalkable(Leader.Zone.ToMap(spot))) spot = leader - (Vector3)(f * (1.2f + i * 0.8f));
            return spot;
        }

        public override string PromptText(PlayerSlot p) => IsDragon ? $"Pet {GameRoot.I.World.DragonName}" : $"Talk to {Character.Name}";

        public override void Interact(PlayerSlot p)
        {
            if (IsDragon)
            {
                string[] lines =
                {
                    $"{GameRoot.I.World.DragonName} chirps and headbutts your hand. A tiny puff of smoke escapes their nostrils.",
                    $"{GameRoot.I.World.DragonName} is trying to eat a pebble. You gently take it away. They look betrayed.",
                    $"{GameRoot.I.World.DragonName} curls around your ankle, radiating warmth like a happy little hearth.",
                };
                p.Hud.Narrate(lines[Random.Range(0, lines.Length)]);
                return;
            }
            p.Hud.Push(new DialogueScreen("companion", null, this));
        }
    }
}
