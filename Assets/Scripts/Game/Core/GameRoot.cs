using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DnD.Game
{
    /// <summary>
    /// The game's hub. Created automatically when any scene loads (no scene setup needed).
    /// Owns players, the world, the clock and all running fights.
    /// </summary>
    // Runs before avatars and HUDs, so a controller's Start press is claimed as a join before P1 can read it as Pause.
    [DefaultExecutionOrder(-100)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot I;

        public Settings Settings = new Settings();
        public GameClock Clock = new GameClock();
        public WorldState World = new WorldState();
        public QuestLog Quests = new QuestLog();
        public readonly List<PlayerSlot> Players = new List<PlayerSlot>();
        public readonly Dictionary<string, Zone> Zones = new Dictionary<string, Zone>();
        public readonly List<CompanionAgent> Companions = new List<CompanionAgent>();
        public readonly List<CharacterFactory.Recruit> TavernRecruits = new List<CharacterFactory.Recruit>();
        public readonly List<CombatInstance> Combats = new List<CombatInstance>();
        public string PlaythroughId, PlaythroughName;
        public bool InGame { get; private set; }
        public bool AnyInCombat => Combats.Count > 0;

        Transform _worldRoot;
        readonly List<NpcAgent> _npcs = new List<NpcAgent>();
        readonly List<RecruitAgent> _recruitAgents = new List<RecruitAgent>();
        readonly Dictionary<string, List<EnemyAgent>> _groups = new Dictionary<string, List<EnemyAgent>>();
        readonly List<PcData> _pendingPlayers = new List<PcData>();
        readonly Dictionary<int, float> _deathTime = new Dictionary<int, float>();
        readonly HashSet<int> _sleeping = new HashSet<int>();
        readonly Dictionary<int, float> _lastLongRest = new Dictionary<int, float>();
        /// <summary>Groups someone is currently talking to (one conversation per group, even in co-op).</summary>
        readonly HashSet<string> _parley = new HashSet<string>();
        TitleScreen _title;
        Camera _idleCam;
        float _lastAutosave = -999;

        float NowAbs => Clock.Day * 1440f + Clock.Minutes;

        // ================================================================== boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("GameRoot");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Clock.HourChanged += OnHourChanged;
            Quests.Changed += OnQuestsChanged;
            AttackResolver.ConcentrationCheckNeeded += (c, dmg) => Combats.FirstOrDefault(x => x.Units.Any(u => u.C == c))?.ConcentrationCheck(c, dmg);
            AttackResolver.PushRequested += (from, to, ft) =>
            {
                var cb = Combats.FirstOrDefault(x => x.Units.Any(u => u.C == to));
                if (cb == null) return;
                cb.Push(cb.Units.First(u => u.C == from), cb.Units.First(u => u.C == to), ft / 5);
            };
            Creature.ConcentrationBroken += c => { foreach (var cb in Combats) cb.Enc.EndConcentration(c); };
            foreach (var cam in FindObjectsByType<Camera>()) if (cam.gameObject.scene.IsValid()) cam.gameObject.SetActive(false);
            ShowTitle();
        }

        // ================================================================== title & sessions

        void ShowTitle()
        {
            InGame = false;
            Music.Play("music_title");
            if (_idleCam == null)
            {
                var go = new GameObject("TitleCamera");
                go.transform.SetParent(transform);
                _idleCam = go.AddComponent<Camera>();
                _idleCam.orthographic = true;
                _idleCam.backgroundColor = new Color32(30, 22, 26, 255);
                _idleCam.clearFlags = CameraClearFlags.SolidColor;
                go.AddComponent<AudioListener>();
            }
            _idleCam.gameObject.SetActive(true);
            _title = TitleScreen.Create(this);
        }

        public void StartNewGame(PlayerInput device, Difficulty diff)
        {
            ResetWorld();
            Settings.Difficulty = diff;
            PlaythroughId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int n = SaveSystem.List().Select(s => s.PlaythroughId).Distinct().Count() + 1;
            PlaythroughName = $"Playthrough {n}" + (diff == Difficulty.Hardcore ? " (Hardcore)" : "");
            Clock.Day = 1; Clock.Minutes = 7 * 60;
            BuildWorld();
            InGame = true;
            var slot = AddPlayer(device);
            BeginCreation(slot);
        }

        /// <summary>Skips the title and creation screens (used by automated tests and quick dev testing).</summary>
        public PlayerSlot DebugStart(PlayerInput device, string classId, string speciesId, Difficulty diff = Difficulty.Normal)
        {
            if (!InGame)
            {
                ResetWorld();
                Settings.Difficulty = diff;
                PlaythroughId = "debug_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                PlaythroughName = "Debug";
                Clock.Day = 1; Clock.Minutes = 9 * 60;
                BuildWorld();
                InGame = true;
            }
            var slot = AddPlayer(device);
            var pc = CharacterFactory.Create(CharacterFactory.DefaultChoices(classId, speciesId, $"{speciesId} {classId}"));
            SpawnPlayer(slot, pc, null);
            return slot;
        }

        public void LoadGame(string path)
        {
            var d = SaveSystem.Read(path);
            if (d == null) return;
            var device = Players.FirstOrDefault()?.Input ?? new HybridInput();
            ResetWorld();
            RemovePlayers(); // loading mid-game: start from a clean slate (P2 rejoins with Start)
            PlaythroughId = d.PlaythroughId; PlaythroughName = d.PlaythroughName;
            Settings.Difficulty = (Difficulty)d.Difficulty;
            Settings.DayLengthMinutes = d.DayLength > 0 ? d.DayLength : 30;
            Settings.NarratorKeyMomentsOnly = d.NarratorKeyOnly;
            Clock.Day = d.Day; Clock.Minutes = d.Minutes;
            SaveSystem.ApplyWorld(d, this);
            BuildWorld();
            InGame = true;
            _pendingPlayers.Clear();
            _pendingPlayers.AddRange(d.Players.Skip(1));
            var p1 = AddPlayer(device);
            SpawnPlayer(p1, SaveSystem.FromData(d.Players[0]), d.Players[0]);
            foreach (var c in d.Companions)
            {
                var leader = Players.FirstOrDefault(p => p.Index == c.Leader) ?? p1;
                var comp = SpawnCompanion(leader, SaveSystem.FromData(c.Pc), c.Quirk);
                comp.Manual = !c.AutoCombat;
                comp.Stance = (CompanionStance)c.Stance;
                comp.TakePoint = c.TakePoint;
            }
            if (World.EggHatched) SpawnDragon(Players.FirstOrDefault(p => p.Index == d.DragonLeader) ?? p1);
            p1.Hud.Narrate($"Welcome back. {Clock.Weekday}, day {Clock.Day}." + (_pendingPlayers.Count > 0 ? " Player 2: press Start (or Enter) to rejoin!" : ""));
        }

        public void QuitToTitle()
        {
            ResetWorld();
            RemovePlayers();
            ShowTitle();
        }

        void RemovePlayers()
        {
            foreach (var p in Players)
            {
                if (p.Hud) Destroy(p.Hud.gameObject);
                if (p.Cam) Destroy(p.Cam.gameObject);
            }
            foreach (var d in FindObjectsByType<DarknessOverlay>()) Destroy(d.gameObject);
            Players.Clear();
        }

        void ResetWorld()
        {
            InGame = false;
            // Close menus first, while characters still exist (screens may tidy up the inventory on close).
            foreach (var p in Players) p.Hud?.CloseAll();
            foreach (var c in Combats) c.Finished = true;
            Combats.Clear();
            StopAllCoroutines();
            if (_worldRoot) Destroy(_worldRoot.gameObject);
            Zones.Clear(); _npcs.Clear(); _groups.Clear(); Companions.Clear(); _recruitAgents.Clear(); TavernRecruits.Clear();
            _deathTime.Clear(); _sleeping.Clear(); _lastLongRest.Clear(); _parley.Clear(); _pendingPlayers.Clear();
            World = new WorldState();
            Quests = new QuestLog();
            Quests.Changed += OnQuestsChanged;
            Clock = new GameClock();
            Clock.HourChanged += OnHourChanged;
            foreach (var p in Players) { if (p.Avatar) Destroy(p.Avatar.gameObject); p.Avatar = null; p.Character = null; p.Combat = null; }
            if (_title) { Destroy(_title.gameObject); _title = null; }
        }

        void BuildWorld()
        {
            _worldRoot = new GameObject("World").transform;
            int i = 0;
            foreach (var def in WorldDb.Zones.Values) Zones[def.Id] = Zone.Build(def, i++, _worldRoot);
            foreach (var npc in WorldDb.Npcs.Values) _npcs.Add(NpcAgent.Create(npc));
            foreach (var z in Zones.Values) foreach (var g in z.Def.EnemyGroups) SpawnGroupIfNeeded(z, g);
            RefreshRecruits();
        }

        // ================================================================== players

        public PlayerSlot AddPlayer(PlayerInput input)
        {
            int index = 0;
            while (Players.Any(p => p.Index == index)) index++;
            if (input is HybridInput hy) hy.Claimed = pad => Players.Any(p => p.Input is GamepadInput gi && gi.Pad == pad);
            var slot = new PlayerSlot { Index = index, Input = input };
            var camGo = new GameObject($"Camera_P{index + 1}");
            DontDestroyOnLoad(camGo);
            slot.Cam = camGo.AddComponent<Camera>();
            slot.Cam.orthographic = true;
            slot.Cam.backgroundColor = Color.black;
            slot.Cam.clearFlags = CameraClearFlags.SolidColor;
            slot.Cam.transform.position = new Vector3(0, 0, -10);
            slot.Cam.nearClipPlane = 0.1f;
            slot.Hud = PlayerHud.Create(slot);
            DarknessOverlay.Create(slot);
            Players.Add(slot);
            if (_idleCam) _idleCam.gameObject.SetActive(false);
            LayoutCameras();
            return slot;
        }

        /// <summary>Characters of players who left (or haven't rejoined since a load). The next Start press brings one back.</summary>
        public IReadOnlyList<PcData> PendingPlayers => _pendingPlayers;

        /// <summary>A co-op partner leaves: their character waits (and is saved) until a controller presses Start again.</summary>
        public void DropOut(PlayerSlot p)
        {
            if (p == null || !Players.Contains(p) || Players.Count < 2) return;
            if (p.IsReady)
            {
                var pd = SaveSystem.ToData(p.Character);
                pd.Zone = p.Zone.Def.Id; pd.X = p.MapPos.x; pd.Y = p.MapPos.y;
                pd.Torch = p.Avatar.TorchMinutes; pd.LightSpell = p.Avatar.LightSpellMinutes;
                _pendingPlayers.Add(pd);
            }
            p.Hud?.CloseAll();
            var stay = Players.First(x => x != p);
            // Companions (and the dragon, which belongs to everyone) wait with the rest of the party.
            foreach (var c in CompanionsOf(p).ToList()) c.Leader = stay;
            string name = p.Label;
            _sleeping.Remove(p.Index);
            if (p.Avatar) Destroy(p.Avatar.gameObject);
            if (p.Hud) Destroy(p.Hud.gameObject);
            if (p.Cam) Destroy(p.Cam.gameObject);
            foreach (var d in FindObjectsByType<DarknessOverlay>()) if (d.Slot == p) Destroy(d.gameObject);
            Players.Remove(p);
            LayoutCameras();
            foreach (var x in Players) x.Hud?.Narrate($"{name} heads off for now. (Press Start to rejoin.)");
            // Someone was waiting in bed for the player who just left.
            if (_sleeping.Count > 0 && Players.Where(x => x.IsReady && !x.StalledByDisconnect).All(x => _sleeping.Contains(x.Index))) DoLongRest();
        }

        void LayoutCameras()
        {
            int n = Players.Count;
            for (int i = 0; i < n; i++)
            {
                var cam = Players[i].Cam;
                Rect r;
                if (n == 1) r = new Rect(0, 0, 1, 1);
                else if (n == 2) r = new Rect(i * 0.5f, 0, 0.5f, 1);
                else r = new Rect((i % 2) * 0.5f, i < 2 ? 0.5f : 0, 0.5f, 0.5f);
                cam.rect = r;
                // Each player only sees their own darkness layer (personal darkvision).
                int mask = ~0;
                for (int j = 0; j < 4; j++) if (j != Players[i].Index) mask &= ~(1 << (8 + j));
                cam.cullingMask = mask;
                // Pixel-perfect-ish zoom: integer scale of 16px tiles.
                float zoom = Mathf.Max(2, Mathf.Round(cam.pixelHeight / 330f));
                cam.orthographicSize = cam.pixelHeight / (2f * ArtRegistry.PixelsPerUnit * zoom);
            }
        }

        void BeginCreation(PlayerSlot slot)
        {
            slot.Hud.Push(new CharacterCreationScreen(pc =>
            {
                SpawnPlayer(slot, pc, null);
                if (Players.Count == 1)
                    slot.Hud.Push(new MessageScreen("The Fallen Star",
                        "Last night, a star fell over Thistledown. It streaked green-gold across the sky and vanished into the Wyrmback Mountains, and for a moment the whole valley hummed, like a plucked string.\n\nThis morning, the chickens are missing.",
                        () => slot.Hud.Narrate("You wake in your cottage. Somewhere outside, Captain Vale is shouting about chickens. (Tip: talk to people with E / A. Your journal is J / LT.)")));
                else slot.Hud.Narrate($"{pc.Name} joins the adventure!");
            }));
        }

        void SpawnPlayer(PlayerSlot slot, PlayerCharacter pc, PcData saved)
        {
            slot.Character = pc;
            Zone zone = Zones["home"];
            Vector2Int pos = zone.Def.DefaultSpawn;
            if (saved != null && Zones.TryGetValue(saved.Zone ?? "", out var z))
            {
                zone = z;
                pos = new Vector2Int(saved.X, saved.Y);
                if (!zone.IsWalkable(pos)) pos = zone.Def.DefaultSpawn; // the map changed since this save
            }
            else if (Players.Count > 1)
            {
                var other = Players.FirstOrDefault(p => p != slot && p.IsReady);
                if (other != null) { zone = other.Zone; pos = other.MapPos; }
            }
            slot.Avatar = PlayerController.Create(slot, zone, pos);
            if (saved != null) { slot.Avatar.TorchMinutes = saved.Torch; slot.Avatar.LightSpellMinutes = saved.LightSpell; }
            _lastLongRest[slot.Index] = NowAbs;
            SnapCamera(slot);
        }

        void SnapCamera(PlayerSlot p)
        {
            if (p.Avatar == null) return;
            var pos = p.Avatar.transform.position;
            p.Cam.transform.position = ClampCam(p, new Vector3(pos.x, pos.y, -10));
        }

        Vector3 ClampCam(PlayerSlot p, Vector3 target)
        {
            var z = p.Zone;
            if (z == null) return target;
            var b = z.WorldBounds;
            float h = p.Cam.orthographicSize, w = h * p.Cam.aspect;
            float x = b.size.x <= w * 2 ? b.center.x : Mathf.Clamp(target.x, b.min.x + w, b.max.x - w);
            float y = b.size.y <= h * 2 ? b.center.y : Mathf.Clamp(target.y, b.min.y + h, b.max.y - h);
            return new Vector3(x, y, -10);
        }

        // ================================================================== frame loop

        void Update()
        {
            if (!InGame) return;
            Music.Play(WantedMusic());

            // Drop-in: an unassigned controller (or the keyboard) presses Start / Enter.
            TryDropIn();

            // Quests waiting for an item notice it however it arrived (loot, chests, trades, gifts).
            if (Time.unscaledTime >= _nextItemCheck) { _nextItemCheck = Time.unscaledTime + 0.5f; CheckQuestItems(); }

            // Clock runs unless every ready player is busy (menus, dialogue, dice, combat).
            var ready = Players.Where(p => p.IsReady).ToList();
            if (ready.Count > 0 && !ready.All(p => p.IsBusy) && _sleeping.Count == 0)
            {
                float before = Clock.Minutes;
                Clock.Tick(Time.deltaTime, Settings.DayLengthMinutes);
                float advanced = Clock.Minutes - before;
                if (advanced < 0) advanced += 1440;
                foreach (var p in ready) p.Avatar.TickLight(advanced);
            }

            // Co-op partners walking into an ongoing fight join it.
            foreach (var p in ready)
            {
                if (p.InCombat || p.Character.IsDown) continue;
                foreach (var c in Combats)
                {
                    if (c.Finished || c.Zone != p.Zone) continue;
                    if (c.Units.Any(u => Vector2.Distance(u.Pos, p.MapPos) <= 8)) { c.JoinLate(p); break; }
                }
            }
        }

        void LateUpdate()
        {
            foreach (var p in Players)
            {
                if (p.Avatar == null) continue;
                var target = p.Avatar.transform.position;
                var cur = p.InCombat ? p.Combat.Current : null;
                // Watch whoever is acting: a monster, or a companion you're commanding.
                if (cur != null && cur.A != null && cur.A != p.Avatar) target = Vector3.Lerp(target, cur.A.transform.position, 0.5f);
                var want = ClampCam(p, new Vector3(target.x, target.y, -10));
                p.Cam.transform.position = Vector3.Lerp(p.Cam.transform.position, want, 1 - Mathf.Exp(-10 * Time.deltaTime));
            }
            if (Players.Count > 0 && (Screen.width != _lastW || Screen.height != _lastH)) { _lastW = Screen.width; _lastH = Screen.height; LayoutCameras(); }
        }

        int _lastW, _lastH;

        /// <summary>Fight music if anyone's fighting, otherwise Player 1's zone (the town has a quieter night tune).</summary>
        string WantedMusic()
        {
            if (AnyInCombat) return "music_fight";
            var lead = Players.FirstOrDefault(p => p.IsReady);
            if (lead == null) return null;
            var z = lead.Zone.Def;
            bool night = Clock.Hour >= 20 || Clock.Hour < 6;
            return night && !z.Indoors && !z.Dark && Music.HasRealClip(z.MusicId + "_night") ? z.MusicId + "_night" : z.MusicId;
        }

        void TryDropIn()
        {
            // A player whose controller dropped picks up whichever free controller presses Start.
            var lost = Players.FirstOrDefault(p => p.Disconnected && p.Input is GamepadInput);
            if (lost != null)
            {
                foreach (var pad in Gamepad.all)
                    if (pad.startButton.wasPressedThisFrame && !Players.Any(p => p.Input is GamepadInput gi && gi.Pad == pad)
                        && !Players.Any(p => p.Input is HybridInput h && h.OwnsPad(pad)))
                    {
                        ((GamepadInput)lost.Input).Rebind(pad);
                        lost.Hud.Toast("Controller connected. Welcome back!", UIKit.Good);
                        return;
                    }
            }
            if (Players.Count >= 4) return;
            PlayerInput joiner = null;
            foreach (var pad in Gamepad.all)
                if (pad.startButton.wasPressedThisFrame && !Players.Any(p => p.Input is GamepadInput gi && gi.Pad == pad)
                    && !Players.Any(p => p.Input is HybridInput h && h.OwnsPad(pad))) joiner = new GamepadInput(pad);
            var kb = Keyboard.current;
            if (joiner == null && kb != null && kb.enterKey.wasPressedThisFrame && !Players.Any(p => p.Input is KeyboardMouseInput || p.Input is HybridInput) && Players.All(p => !p.IsBusy))
                joiner = new KeyboardMouseInput();
            if (joiner == null) return;
            var slot = AddPlayer(joiner);
            if (_pendingPlayers.Count > 0)
            {
                var d = _pendingPlayers[0];
                _pendingPlayers.RemoveAt(0);
                SpawnPlayer(slot, SaveSystem.FromData(d), d);
                slot.Hud.Narrate($"{slot.Character.Name} rejoins the party!");
            }
            else BeginCreation(slot);
        }

        // ================================================================== time-of-day events

        void OnHourChanged(int hour)
        {
            foreach (var n in _npcs) n.UpdateSchedule(false);
            if (hour == 0) RefreshRecruits();
            if (hour == 2)
            {
                // Staying up all night: CON save or gain Exhaustion.
                foreach (var p in Players.Where(x => x.IsReady && !x.Character.IsDead))
                {
                    if (NowAbs - (_lastLongRest.TryGetValue(p.Index, out var t) ? t : NowAbs) < 18 * 60) continue;
                    var save = Checks.RollSave(p.Character, Ability.CON, AdvantageState.Normal);
                    bool ok = save.Total >= 10;
                    p.Hud.Dice.Ticker($"Staying up late: CON save {save.Breakdown()} vs DC 10 → {(ok ? "you push through" : "+1 Exhaustion")}");
                    if (!ok) p.Character.Exhaustion = Math.Min(6, p.Character.Exhaustion + 1);
                    p.Hud.Narrate("It's very late. Your eyelids feel like lead. (Find a bed: home, the tavern, or the temple.)", false);
                }
            }
        }

        void RefreshRecruits()
        {
            int week = (Clock.Day - 1) / 7;
            if (World.RecruitWeek != week || TavernRecruits.Count == 0 && World.RecruitWeek < 0)
            {
                World.RecruitWeek = week;
                World.RecruitSeed = week * 7919 + (PlaythroughId?.GetHashCode() ?? 0);
                TavernRecruits.Clear();
                var rng = new System.Random(World.RecruitSeed);
                for (int i = 0; i < 2; i++) TavernRecruits.Add(CharacterFactory.GenerateRecruit(rng));
            }
            else if (TavernRecruits.Count == 0)
            {
                var rng = new System.Random(World.RecruitSeed);
                for (int i = 0; i < 2; i++) TavernRecruits.Add(CharacterFactory.GenerateRecruit(rng));
            }
            // Don't show recruits who already joined someone.
            TavernRecruits.RemoveAll(r => Companions.Any(c => c.Character != null && c.Character.Name == r.Character.Name));
            foreach (var a in _recruitAgents) if (a) Destroy(a.gameObject);
            _recruitAgents.Clear();
            if (!Zones.TryGetValue("tavern", out var tav)) return;
            Vector2Int[] spots = { new Vector2Int(3, 6), new Vector2Int(12, 4), new Vector2Int(8, 8) };
            for (int i = 0; i < TavernRecruits.Count && i < spots.Length; i++)
            {
                var r = TavernRecruits[i];
                var actor = Actor.Spawn<Actor>("Recruit_" + r.Character.Name, $"npc_{r.Character.Class.Id}", true, tav, spots[i]);
                var ra = actor.gameObject.AddComponent<RecruitAgent>();
                ra.Data = r; ra.Actor = actor;
                _recruitAgents.Add(ra);
            }
        }

        // ================================================================== zones & warps

        public bool AnyPlayerIn(Zone z) => Players.Any(p => p.Zone == z);

        public void Warp(PlayerSlot p, string zoneId, Vector2Int pos, bool sfx)
        {
            if (!Zones.TryGetValue(zoneId, out var zone)) return;
            if (sfx) Sfx.Play("door");
            p.Avatar.PlaceAt(zone, pos);
            foreach (var c in CompanionsOf(p)) if (!c.Waiting) c.Actor.PlaceAt(zone, pos);
            SnapCamera(p);
            foreach (var g in zone.Def.EnemyGroups) SpawnGroupIfNeeded(zone, g);
            foreach (var n in _npcs) n.UpdateSchedule(false);

            AwardOnce(p, "zone:" + zoneId, 15, $"discovering {zone.Def.Name}");
            Quests.Notify(QuestEvent.ZoneEntered, zoneId, p);
            string key = $"entered:{p.Index}:{zoneId}:{Clock.Day}";
            if (!World.IsLocked(key)) { World.Lock(key, 24 * 60); p.Hud.Narrate(zone.Def.EnterNarration, !World.Flag("seen:" + zoneId)); World.SetFlag("seen:" + zoneId); }

            if (zone.Def.Dark && p.Character.Darkvision <= 0 && !p.Avatar.HasLight)
            {
                if (p.Character.CountOf("torch") > 0) p.Avatar.LightTorch();
                else p.Hud.Toast("It's pitch black and you have no light! (Buy torches from Brannoc.)", UIKit.Bad);
            }
            if (zone.Def.Dark && p.Character.Darkvision > 0) p.Hud.Toast($"Your darkvision ({p.Character.Darkvision} ft) shows the world in shades of grey.", UIKit.UtilityBlue);

            if (Settings.Difficulty != Difficulty.Hardcore && Time.unscaledTime - _lastAutosave > 30) { _lastAutosave = Time.unscaledTime; SaveSystem.Save(true); }
        }

        // ================================================================== enemies & combat

        void SpawnGroupIfNeeded(Zone z, EnemyGroupDef g)
        {
            bool resolved = World.DefeatedGroups.Contains(g.Id) || World.PacifiedGroups.Contains(g.Id);
            if (resolved && !g.Unique && World.DefeatedDay.TryGetValue(g.Id, out var day) && Clock.Day - day >= WorldState.RespawnDays)
            {
                // A new band moves in (whether the last one was beaten, befriended or scared off).
                World.DefeatedGroups.Remove(g.Id);
                World.PacifiedGroups.Remove(g.Id);
                World.Flags.Remove("fled:" + g.Id);
                World.DefeatedDay.Remove(g.Id);
                if (_groups.TryGetValue(g.Id, out var old)) foreach (var a in old) if (a) Destroy(a.gameObject);
                _groups.Remove(g.Id);
            }
            if (!g.InStage(Quests))
            {
                // A quest-gated group that isn't part of the story right now (not yet, or not any more).
                if (_groups.TryGetValue(g.Id, out var gone) && !Combats.Any(c => c.GroupId == g.Id && !c.Finished) && !_parley.Contains(g.Id))
                {
                    foreach (var a in gone) if (a) Destroy(a.gameObject);
                    _groups.Remove(g.Id);
                }
                return;
            }
            if (World.DefeatedGroups.Contains(g.Id) || _groups.ContainsKey(g.Id)) return;
            if (World.PacifiedGroups.Contains(g.Id) && World.Flag("fled:" + g.Id)) return;
            var list = new List<EnemyAgent>();
            foreach (var m in g.Members)
            {
                var a = EnemyAgent.Create(z, g, m);
                if (World.PacifiedGroups.Contains(g.Id)) { a.enabled = false; a.Monster.Team = Team.Neutral; }
                list.Add(a);
            }
            _groups[g.Id] = list;
        }

        /// <summary>
        /// Being spotted doesn't force a fight: the DM pauses and asks what you do (talk, scare, calm, slip away, or fight).
        /// Groups with a story conversation (Snik) use theirs the first time.
        /// </summary>
        public void OnEnemyNoticed(EnemyAgent e, PlayerSlot p)
        {
            var g = e.Group;
            if (Combats.Any(c => c.GroupId == g.Id) || _parley.Contains(g.Id) || World.Flag("passage:" + g.Id)) return;
            if (_groups.TryGetValue(g.Id, out var agents)) foreach (var a in agents) if (a) a.Alerted = true;
            if (g.ParleyDialogue != null && !World.Flag("parley:" + g.Id))
            {
                World.SetFlag("parley:" + g.Id);
                _parley.Add(g.Id);
                p.Hud.Push(new DialogueScreen(g.ParleyDialogue, null, null, null, g));
                return;
            }
            if (g.Style != EncounterStyle.None)
            {
                _parley.Add(g.Id);
                p.Hud.Push(new DialogueScreen("encounter", null, null, null, g));
                return;
            }
            p.Hud.Narrate($"A {e.Monster.Name.ToLower()} spots you!");
            StartGroupCombat(g.Id, p);
        }

        public void EndParley(string groupId) => _parley.Remove(groupId);

        /// <summary>Story hooks for a group being dealt with, whichever way it went (fight, talk, scare).</summary>
        void OnGroupResolved(string groupId)
        {
            Quests.Notify(QuestEvent.GroupResolved, groupId);
        }

        /// <summary>Paid off: the group lets you be (no XP, they're still there, and you can still deal with them later).</summary>
        public void GrantPassage(string groupId)
        {
            World.SetFlag("passage:" + groupId);
            CalmGroup(groupId);
        }

        /// <summary>The sharpest-eyed member's passive Perception: the DC to slip away from the group.</summary>
        public int GroupPassivePerception(string groupId) =>
            _groups.TryGetValue(groupId, out var agents) ? agents.Where(a => a != null && !a.Monster.IsDown).Select(a => a.Monster.PassivePerception).DefaultIfEmpty(10).Max() : 10;

        /// <summary>You crept up unseen and struck first: the enemies are surprised (Disadvantage on initiative).</summary>
        public void Ambush(string groupId, PlayerSlot p)
        {
            if (p.IsBusy) return;
            p.Avatar.Sneaking = false;
            StartGroupCombat(groupId, p, enemiesSurprised: true);
        }

        /// <summary>Slipped away: the group loses track of you for an hour of game time.</summary>
        public void CalmGroup(string groupId, float minutes = 60)
        {
            World.Lock("calm:" + groupId, minutes);
            if (_groups.TryGetValue(groupId, out var agents)) foreach (var a in agents) if (a) a.Alerted = false;
        }

        public void OnCombatRetreat(CombatInstance c)
        {
            Combats.Remove(c);
            if (c.GroupId != null) CalmGroup(c.GroupId);
            foreach (var h in c.Humans) h.Hud.Narrate("You got away! They've lost track of you for now. Rest up, then finish this however you like.");
        }

        /// <summary>Each player's share of an encounter's XP. Companions never take a share (they match their leader instead).</summary>
        public static int EncounterShare(int xp, int players) => xp / Math.Max(1, players);

        /// <summary>
        /// Rolls the group's loot and splits it between players. Used for victories AND peaceful endings,
        /// so talking a group down is worth exactly what fighting them is.
        /// </summary>
        public void SplitLoot(List<PlayerSlot> humans, IEnumerable<MonsterDef> defs)
        {
            if (humans.Count == 0) return;
            int copper = 0;
            var items = new List<string>();
            foreach (var def in defs)
            {
                copper += UnityEngine.Random.Range(def.CopperMin, def.CopperMax + 1);
                foreach (var kv in def.Loot) if (UnityEngine.Random.value <= kv.Value) items.Add(kv.Key);
            }
            foreach (var h in humans)
            {
                int cp = copper / humans.Count;
                if (cp > 0) { h.Character.Copper += cp; h.Hud.Toast($"+{Currency.Format(cp)}", UIKit.Gold); }
            }
            foreach (var it in items)
            {
                var taker = humans.FirstOrDefault(h => h.Character.Bag.SpaceFor(it) > 0) ?? humans.First();
                if (taker.Character.AddItem(it) > 0) World.Storage.Add(it, 1);
                foreach (var h in humans) h.Hud.Toast($"{taker.Character.Name} got: {ItemsDb.Get(it)?.Name}", UIKit.Gold);
                Quests.Notify(QuestEvent.ItemGained, it, taker);
            }
        }

        public void StartGroupCombat(string groupId, PlayerSlot instigator, bool enemiesSurprised = false)
        {
            if (Combats.Any(x => x.GroupId == groupId && !x.Finished)) return; // already fighting this group
            if (!_groups.TryGetValue(groupId, out var agents) || agents.All(a => a == null || a.Monster.IsDown)) return;
            var zone = agents.First(a => a != null).Zone;
            var c = new CombatInstance(this, zone, groupId);
            foreach (var a in agents.Where(a => a != null && !a.Monster.IsDown)) { a.StopAllCoroutines(); a.Moving = false; c.AddUnit(a.Monster, a, enemy: a); }
            foreach (var p in Players.Where(p => p.IsReady && p.Zone == zone && !p.InCombat && !p.Character.IsDead))
            {
                if (p != instigator && Vector2.Distance(p.MapPos, instigator.MapPos) > 12) continue;
                p.Hud.CloseAll();
                c.AddUnit(p.Character, p.Avatar, p);
                foreach (var comp in CompanionsOf(p))
                    if (comp.Character != null && !comp.Character.IsDead && comp.Actor.Zone == zone && Vector2.Distance(comp.Actor.MapPos, p.MapPos) <= 12)
                        c.AddUnit(comp.Character, comp.Actor, comp: comp);
            }
            if (!c.Units.Any(u => u.Owner != null))
            {
                // Nobody's there to fight (e.g. a conversation ended in a threat while you stood elsewhere): don't run a fight with no heroes.
                foreach (var u in c.Units) if (u.Enemy != null) u.Enemy.InCombat = false;
                return;
            }
            Combats.Add(c);
            StartCoroutine(c.Run(false, enemiesSurprised));
        }

        public void OnCombatFinished(CombatInstance c, bool victory)
        {
            Combats.Remove(c);
            foreach (var u in c.Units.Where(u => u.C is PlayerCharacter pc && pc.IsDead)) _deathTime[u.C.Uid] = NowAbs;
            if (!victory) StartCoroutine(PartyWipe(c));
            else if (c.GroupId != null) OnGroupResolved(c.GroupId);
        }

        IEnumerator PartyWipe(CombatInstance c)
        {
            var fallen = c.Humans.ToList();
            bool everyone = Players.Where(p => p.IsReady).All(p => fallen.Contains(p));
            yield return new WaitForSeconds(1.5f);
            if (Settings.Difficulty == Difficulty.Hardcore && everyone)
            {
                SaveSystem.DeletePlaythrough(PlaythroughId);
                foreach (var p in fallen) p.Hud.Push(new MessageScreen("Your Story Ends",
                    "The party has fallen. In Hardcore, death is final.\n\nThe bards of Thistledown will sing of you. Probably. Pip will at least put up a little plaque.", QuitToTitle));
                yield break;
            }
            // Regroup: the surviving monsters heal up over the day.
            if (_groups.TryGetValue(c.GroupId ?? "", out var agents))
            {
                foreach (var a in agents) if (a) Destroy(a.gameObject);
                _groups.Remove(c.GroupId);
            }
            // A real party wipe costs everyone a day. If a partner is still up and about elsewhere,
            // the fallen are simply carried to the temple and the world keeps its time.
            if (everyone) Clock.SleepUntil(8);
            foreach (var p in fallen)
            {
                var pc = p.Character;
                if (pc.IsDead) pc.Revive(0, null);
                pc.HP = Math.Max(1, pc.EffectiveMaxHP / 2);
                pc.DeathSuccesses = pc.DeathFailures = 0; pc.Stable = false;
                int fee = pc.Copper / 10;
                pc.Copper -= fee;
                foreach (var comp in CompanionsOf(p)) if (comp.Character != null) { if (comp.Character.IsDead) comp.Character.Revive(0, null); comp.Character.HP = Math.Max(1, comp.Character.EffectiveMaxHP / 2); }
                Warp(p, "temple", new Vector2Int(5, 5), false);
                p.Hud.Push(new MessageScreen("You wake in the temple...",
                    everyone
                        ? $"Incense. Soft singing. A warm hand on your forehead.\n\n\"Shepherds found you at dawn and carried you here,\" Mother Elowen says. \"The Dawnmother isn't done with you yet.\"\n\nA day has passed. ({Currency.Format(fee)} was left as a temple donation.)"
                        : $"Incense. Soft singing. A warm hand on your forehead.\n\n\"A woodcutter found you and hauled you here on his cart,\" Mother Elowen says. \"Go on. Your friends are still out there.\"\n\n({Currency.Format(fee)} was left as a temple donation.)"));
            }
        }

        public bool EnemiesNear(PlayerSlot p, float tiles) =>
            _groups.Values.SelectMany(x => x).Any(a => a != null && a.isActiveAndEnabled && a.Zone == p.Zone && !a.Monster.IsDown && a.Monster.Team == Team.Enemies && Vector2.Distance(a.MapPos, p.MapPos) <= tiles);

        /// <summary>
        /// The group was talked down, calmed or scared off. Same XP and same loot as beating them:
        /// befriended groups share their treasure, fleeing ones drop it.
        /// </summary>
        public void PacifyGroup(string groupId, PlayerSlot p, bool fled = false)
        {
            if (World.PacifiedGroups.Contains(groupId)) return;
            World.PacifiedGroups.Add(groupId);
            World.DefeatedDay[groupId] = Clock.Day;
            int xp = 0;
            var defs = new List<MonsterDef>();
            if (_groups.TryGetValue(groupId, out var agents))
                foreach (var a in agents)
                {
                    if (a == null || a.Monster.IsDead) continue;
                    xp += a.Monster.Def.XP;
                    defs.Add(a.Monster.Def);
                    a.Monster.Team = Team.Neutral;
                    a.enabled = false;
                    if (fled) a.gameObject.SetActive(false);
                }
            if (fled) World.SetFlag("fled:" + groupId);
            var here = Players.Where(x => x.IsReady && x.Zone == p.Zone).ToList();
            int share = EncounterShare(xp, here.Count);
            foreach (var x in here) GiveXP(x, share, fled ? "scaring them off" : "resolving it peacefully");
            SplitLoot(here, defs); // fleeing groups drop it, friendly ones share it
            OnGroupResolved(groupId);
        }

        // ================================================================== companions

        public IEnumerable<CompanionAgent> CompanionsOf(PlayerSlot p) => Companions.Where(c => c != null && c.Leader == p);

        public void HireRecruit(PlayerSlot p, RecruitAgent r)
        {
            int partySize = Players.Count(x => x.IsReady) + Companions.Count(c => !c.IsDragon);
            if (partySize >= 4) { p.Hud.Toast("Your party is full (4 max).", UIKit.Bad); return; }
            var pc = r.Data.Character;
            TavernRecruits.Remove(r.Data);
            _recruitAgents.Remove(r);
            Destroy(r.gameObject);
            SpawnCompanion(p, pc, r.Data.Quirk);
            p.Hud.Toast($"{pc.Name} joins your party!", UIKit.Good);
            if (!World.Flag("tip_companion:" + p.Index))
            {
                World.SetFlag("tip_companion:" + p.Index);
                p.Hud.Narrate($"{pc.Name}'s health shows under yours. In a fight, you choose their moves on their turn (or press Auto). Talk to them to change that, set their role, or tell them to wait.");
            }
        }

        public CompanionAgent SpawnCompanion(PlayerSlot leader, PlayerCharacter pc, string quirk)
        {
            pc.Team = Team.Players;
            var c = CompanionAgent.Create(leader, $"npc_{pc.Class.Id}_{pc.Species.Id}", true, pc); // same look as in the tavern
            c.Quirk = quirk;
            Companions.Add(c);
            return c;
        }

        /// <summary>Out of combat: hand a healing potion to an ally to drink (the item goes from your bag to them).</summary>
        public bool GivePotionTo(PlayerSlot giver, Creature target, int bagIndex)
        {
            var st = giver.Character.Bag.Slots[bagIndex];
            if (st == null || !(ItemsDb.Get(st.ItemId) is ConsumableDef def) || def.Effect != ConsumableEffect.Heal) return false;
            if (target.IsDead) { giver.Hud.Toast($"{target.Name} is beyond a potion's help. (Ember of Return, or the temple.)", UIKit.Bad); return false; }
            if (target.HP >= target.EffectiveMaxHP) { giver.Hud.Toast($"{target.Name} is already at full health."); return false; }
            giver.Character.Bag.PickUp(bagIndex, 1);
            var roll = Dice.RollDamage(def.Amount);
            giver.Hud.Dice.RollDamage(def.Name, $"{target.Name} drinks it", roll, DieColor.Healing, () =>
            {
                int healed = target.Heal(roll.Total);
                giver.Hud.Toast($"{target.Name} regains {healed} HP", UIKit.HealGreen);
                Sfx.Play("heal");
            });
            return true;
        }

        public void DismissCompanion(CompanionAgent c)
        {
            Companions.Remove(c);
            var r = new CharacterFactory.Recruit { Character = c.Character, Quirk = c.Quirk, PersuasionDC = 8, HireCostCp = 0,
                Pitch = $"{c.Character.Name}, your old companion, waves you over. \"Back for me?\"" };
            TavernRecruits.Add(r);
            Destroy(c.gameObject);
            RefreshRecruits();
        }

        void SpawnDragon(PlayerSlot leader)
        {
            var d = CompanionAgent.Create(leader, "mon_dragonling", false, null);
            d.IsDragon = true;
            Companions.Add(d);
        }

        public void HatchEgg(PlayerSlot p, Vector3 eggPos)
        {
            World.EggHatched = true;
            Quests.Set("fallen_star", 3, p);
            Sfx.Play("crit");
            p.Hud.Push(new MessageScreen("The Egg Hatches",
                "The egg pulses once, twice, then splits with a sound like a log cracking in a fire. A tiny, damp, furious red dragon tumbles out, sneezes a puff of sparks onto your boots, looks up at you...\n\n...and decides, with total certainty, that you are its mother.",
                () =>
                {
                    var names = new[] { "Cinder", "Ember", "Pebble", "Sir Toasty", "Biscuit" };
                    p.Hud.Push(new ChoiceScreen("Name your dragon", "The dragonling chirps expectantly.",
                        names.Select(n => (n, (Action)(() =>
                        {
                            World.DragonName = n;
                            SpawnDragon(p);
                            foreach (var x in Players.Where(x => x.IsReady)) GiveXP(x, 100, "hatching the egg");
                            p.Hud.Narrate($"{n} chirps happily and scrambles up to ride on your shoulder... then falls off. Then tries again. Return to Captain Vale!");
                        }))).ToList(), false));
                }));
        }

        // ================================================================== economy, quests, XP

        public void OpenShop(PlayerSlot p, string shop) => p.Hud.Push(new ShopScreen(shop));

        float _nextItemCheck;

        /// <summary>Quest-gated groups appear or leave as their quest moves on.</summary>
        /// <summary>Story flags can move people around (someone gets a job, goes home, ends up in a cell).</summary>
        public void RefreshNpcs() { foreach (var n in _npcs) if (n) n.UpdateSchedule(false); }

        void OnQuestsChanged()
        {
            RefreshNpcs();
            foreach (var z in Zones.Values)
                foreach (var grp in z.Def.EnemyGroups)
                    if (grp.QuestId != null) SpawnGroupIfNeeded(z, grp);
        }

        /// <summary>A quest item counts wherever the party keeps it: this player's bag, a partner's, or home storage.</summary>
        public bool HasQuestItem(PlayerSlot p, string id) =>
            p.Character.CountOf(id) > 0 || Players.Any(x => x.Character != null && x.Character.CountOf(id) > 0) || World.Storage.Count(id) > 0;

        /// <summary>Hands one quest item over (from this player first, then a partner, then home storage).</summary>
        public bool TakeQuestItem(PlayerSlot p, string id)
        {
            if (p.Character.RemoveItem(id)) return true;
            foreach (var x in Players) if (x.Character != null && x.Character.RemoveItem(id)) return true;
            return World.Storage.Remove(id, 1);
        }

        void CheckQuestItems()
        {
            foreach (var q in QuestDb.All.Values)
                foreach (var t in q.Triggers)
                {
                    if (t.On != QuestEvent.ItemGained || Quests.Stage(q.Id) != t.FromStage) continue;
                    var holder = Players.FirstOrDefault(p => p.IsReady && p.Character != null && p.Character.CountOf(t.Id) > 0);
                    if (holder != null) Quests.Notify(QuestEvent.ItemGained, t.Id, holder);
                }
        }

        public void GiveItem(PlayerSlot p, string id, int count = 1)
        {
            int left = p.Character.AddItem(id, count);
            var name = ItemsDb.Get(id)?.Name ?? id;
            if (left > 0) p.Hud.Toast($"No room for {name}! (Stored at home.)", UIKit.Bad);
            if (left > 0) World.Storage.Add(id, left);
            else p.Hud.Toast($"Received: {name}", UIKit.Gold);
            Quests.Notify(QuestEvent.ItemGained, id, p);
        }

        public void GiveCopper(PlayerSlot p, int cp)
        {
            p.Character.Copper += cp;
            p.Hud.Toast($"+{Currency.Format(cp)}", UIKit.Gold);
            Sfx.Play("coin");
        }

        public void GiveXP(PlayerSlot p, int xp, string reason)
        {
            if (xp <= 0 || p.Character == null) return;
            bool could = p.Character.CanLevelUp;
            p.Character.XP += xp;
            p.Hud.Toast($"+{xp} XP ({reason})", UIKit.D20Gold);
            if (!could && p.Character.CanLevelUp) Sfx.Play("levelup");
            if (!could && p.Character.CanLevelUp) p.Hud.Toast("★ Level up available! Open your character sheet (C / View).", UIKit.D20Gold);
            foreach (var c in CompanionsOf(p)) if (c.Character != null) { c.Character.XP += xp; AutoLevel(c.Character); }
        }

        /// <summary>Companions level up on their own (average HP, auto choices).</summary>
        static void AutoLevel(PlayerCharacter pc)
        {
            while (pc.CanLevelUp) pc.LevelUp(pc.AutoPlan(), new List<string>());
        }

        public void AwardOnce(PlayerSlot p, string key, int xp, string reason)
        {
            string k = p.Index + ":" + key;
            if (World.XpAwarded.Contains(k)) return;
            World.XpAwarded.Add(k);
            GiveXP(p, xp, reason);
        }

        public void ReturnKettle(PlayerSlot p)
        {
            TakeQuestItem(p, "copper_kettle");
            World.KettleReturned = true;
            Quests.Set("kettle", 2, p);
        }

        public void ReturnSpoon(PlayerSlot p)
        {
            TakeQuestItem(p, "lucky_spoon");
            World.SetFlag("spoon_returned");
            Quests.Set("spoon", 2, p);
        }

        public void CompleteFallenStar(PlayerSlot p)
        {
            Quests.Set("fallen_star", 4, p);
        }

        public void CompleteEastRoad(PlayerSlot p)
        {
            Quests.Set("east_road", 3, p);
        }

        public void RentRoom(PlayerSlot p)
        {
            if (World.Flag("pip_free_room") && !World.Flag("pip_free_room_used"))
            {
                World.SetFlag("pip_free_room_used");
                p.Hud.Toast("Pip: \"This one's on the house. For Wick.\"", UIKit.Gold);
                BeginLongRest(p);
                return;
            }
            if (p.Character.Copper < 50) { p.Hud.Toast("You can't afford a room (5 sp).", UIKit.Bad); return; }
            p.Character.Copper -= 50;
            BeginLongRest(p);
        }

        // ================================================================== rest & sleep

        public void BeginLongRest(PlayerSlot p)
        {
            if (p.Zone.Def.Dark)
            {
                // Camping in a dungeon: a hidden encounter roll, shown to the player.
                p.Hud.Dice.RollD20("Keeping Watch", "Camping in the mine. 6 or higher: a quiet night.", () => Dice.RollD20(AdvantageState.Normal), 6, false, r =>
                {
                    if (r.Total < 6)
                    {
                        p.Hud.Narrate("Halfway through the night, something skitters in the dark. Your watch shouts a warning: AMBUSH!");
                        AmbushAt(p);
                    }
                    else FinishSleeping(p);
                });
                return;
            }
            FinishSleeping(p);
        }

        void FinishSleeping(PlayerSlot p)
        {
            _sleeping.Add(p.Index);
            var awake = Players.Where(x => x.IsReady && !x.StalledByDisconnect && !_sleeping.Contains(x.Index)).ToList();
            if (awake.Count > 0)
            {
                p.Hud.Push(new ChoiceScreen("Zzz...", $"Waiting for {string.Join(", ", awake.Select(a => a.Character.Name))} to go to bed too.",
                    new List<(string, Action)> { ("Get up", () => _sleeping.Remove(p.Index)) }, false));
                foreach (var a in awake) a.Hud.Toast($"{p.Character.Name} is asleep. Find a bed to end the day.", UIKit.UtilityBlue);
                return;
            }
            DoLongRest();
        }

        void DoLongRest()
        {
            Clock.SleepUntil(6);
            foreach (var p in Players.Where(x => x.IsReady))
            {
                p.Hud.CloseAll();
                bool safe = p.Zone.Def.SafeForLongRest || p.Zone.Def.Indoors;
                bool ate = safe || p.Character.RemoveItem("rations");
                var log = new List<string>();
                if (p.Character.IsDead && Settings.Difficulty == Difficulty.Story) p.Character.Revive(0, log);
                p.Character.LongRest(ate, log);
                foreach (var c in CompanionsOf(p)) if (c.Character != null) { if (c.Character.IsDead && Settings.Difficulty == Difficulty.Story) c.Character.Revive(0, null); c.Character.LongRest(true, null); }
                _lastLongRest[p.Index] = NowAbs;
                p.Hud.Narrate($"You wake refreshed. It's {Clock.Weekday}, day {Clock.Day}." + (!ate ? " (You went to bed hungry.)" : ""));
            }
            _sleeping.Clear();
            SaveSystem.Save(true);
            _lastAutosave = Time.unscaledTime;
        }

        void AmbushAt(PlayerSlot p)
        {
            var zone = p.Zone;
            var g = new EnemyGroupDef { Id = "ambush_" + Clock.Day + "_" + UnityEngine.Random.Range(0, 9999), Style = EncounterStyle.None, Unique = true };
            var spots = new List<Vector2Int>();
            for (int dx = -4; dx <= 4 && spots.Count < 2; dx++)
                for (int dy = -4; dy <= 4 && spots.Count < 2; dy++)
                {
                    var q = p.MapPos + new Vector2Int(dx, dy);
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) >= 3 && zone.IsWalkable(q)) spots.Add(q);
                }
            foreach (var s in spots) g.Members.Add(new EnemySpawn(UnityEngine.Random.value < 0.5f ? "kobold" : "giant_rat", s.x, s.y));
            _groups[g.Id] = g.Members.Select(m => EnemyAgent.Create(zone, g, m)).ToList();
            StartGroupCombat(g.Id, p);
        }

        // ================================================================== healing & revival

        public bool AnyDeadAlly(PlayerSlot p) =>
            Players.Any(x => x.IsReady && x.Character.IsDead) || CompanionsOf(p).Any(c => c.Character != null && c.Character.IsDead);

        public void TempleRevive(PlayerSlot p)
        {
            int cost = Settings.Difficulty >= Difficulty.Hard ? 10000 : 0;
            if (p.Character.Copper < cost) { p.Hud.Toast("You can't afford the rite (100 gp).", UIKit.Bad); return; }
            p.Character.Copper -= cost;
            foreach (var x in Players.Where(x => x.IsReady && x.Character.IsDead))
            {
                x.Character.Revive(0, null);
                if (x.Zone != p.Zone) Warp(x, p.Zone.Def.Id, p.MapPos, false);
                x.Hud.Narrate("Warmth floods back into you. Mother Elowen smiles. \"Welcome back, child.\"");
            }
            foreach (var c in Companions.Where(c => c.Character != null && c.Character.IsDead)) c.Character.Revive(0, null);
            Sfx.Play("heal");
        }

        public void TryRevive(PlayerSlot p, string itemId)
        {
            Creature target = null;
            foreach (var x in Players) if (x != p && x.IsReady && x.Character.IsDead && Vector2.Distance(x.MapPos, p.MapPos) <= 2.5f) target = x.Character;
            foreach (var c in Companions) if (c.Character != null && c.Character.IsDead && Vector2.Distance(c.Actor.MapPos, p.MapPos) <= 2.5f) target = c.Character;
            if (target == null) { p.Hud.Toast("No fallen ally nearby.", UIKit.Bad); return; }
            if (Settings.Difficulty >= Difficulty.Hard && _deathTime.TryGetValue(target.Uid, out var t) && NowAbs - t > 10)
            { p.Hud.Toast("Too late: the spark has faded. Only the temple can help now.", UIKit.Bad); return; }
            p.Character.RemoveItem(itemId);
            ((PlayerCharacter)target).Revive(Settings.Difficulty >= Difficulty.Hard ? 1 : 0, null);
            p.Hud.Narrate($"You press the {ItemsDb.Get(itemId).Name} to {target.Name}'s heart. A flare of dragonfire, and they gasp back to life!");
            Sfx.Play("heal");
        }

        public Creature LowestHpAlly(PlayerSlot p)
        {
            var list = new List<Creature> { p.Character };
            foreach (var x in Players) if (x != p && x.IsReady && x.Zone == p.Zone && Vector2.Distance(x.MapPos, p.MapPos) <= 6) list.Add(x.Character);
            foreach (var c in CompanionsOf(p)) if (c.Character != null) list.Add(c.Character);
            return list.Where(c => !c.IsDead).OrderBy(c => c.HP / (float)Math.Max(1, c.EffectiveMaxHP)).First();
        }
    }
}
