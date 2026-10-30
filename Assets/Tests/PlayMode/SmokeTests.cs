using System.Collections;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>
    /// A fake player that mashes random buttons. Any exception or error logged during a test fails it,
    /// so fuzzing every screen is a cheap way to find game-breaking bugs.
    /// </summary>
    public class FuzzInput : DnD.Game.PlayerInput
    {
        readonly System.Random _r;
        int _frame = -1;
        bool _confirm, _cancel, _end, _inv, _char, _spells, _journal, _next, _prev, _alt, _use, _sort, _give;
        int _hot = -1;
        Vector2 _move, _nav;
        public float ConfirmChance = 0.25f, CancelChance = 0.03f, EndTurnChance = 0.03f, MenuChance = 0.004f;
        public bool AllowPause;

        public FuzzInput(int seed) { _r = new System.Random(seed); }

        bool Roll(float p) => _r.NextDouble() < p;

        void Tick()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _confirm = Roll(ConfirmChance);
            _cancel = Roll(CancelChance);
            _end = Roll(EndTurnChance);
            _inv = Roll(MenuChance); _char = Roll(MenuChance); _spells = Roll(MenuChance); _journal = Roll(MenuChance);
            _next = Roll(0.03f); _prev = Roll(0.02f); _alt = Roll(0.03f); _use = Roll(0.02f); _sort = Roll(0.01f); _give = Roll(0.01f);
            _hot = Roll(0.01f) ? _r.Next(0, 12) : -1;
            if (Roll(0.04f)) _move = new Vector2((float)_r.NextDouble() * 2 - 1, (float)_r.NextDouble() * 2 - 1);
            _nav = Roll(0.3f) ? new Vector2(_r.Next(-1, 2), _r.Next(-1, 2)) : Vector2.zero;
        }

        public override string DeviceName => "Fuzz";
        public override bool IsConnected => true;
        public override Vector2 Move { get { Tick(); return _move; } }
        public override bool Confirm { get { Tick(); return _confirm; } }
        public override bool Cancel { get { Tick(); return _cancel; } }
        public override bool Inventory { get { Tick(); return _inv; } }
        public override bool CharacterSheet { get { Tick(); return _char; } }
        public override bool Spells { get { Tick(); return _spells; } }
        public override bool Journal { get { Tick(); return _journal; } }
        public override bool EndTurn { get { Tick(); return _end; } }
        public override bool Pause => AllowPause && Confirm && Cancel;
        public override bool NextTab { get { Tick(); return _next; } }
        public override bool PrevTab { get { Tick(); return _prev; } }
        public override bool AltAction { get { Tick(); return _alt; } }
        public override bool QuickModifier => false;
        public override bool SneakToggle { get { Tick(); return _r.NextDouble() < 0.002; } }
        public override bool Highlight => false;
        public override int HotbarPressed { get { Tick(); return _hot; } }
        public override Vector2 Cursor => Vector2.zero;
        public override bool UseItem { get { Tick(); return _use; } }
        public override bool Sort { get { Tick(); return _sort; } }
        public override bool Give { get { Tick(); return _give; } }
        protected override Vector2 NavRaw() { Tick(); return _nav; }
    }

    /// <summary>Presses exactly the keys a test asks for, for one frame (Esc = Cancel + Pause, like the real keyboard).</summary>
    public class ScriptInput : DnD.Game.PlayerInput
    {
        int _frame = -1;
        public bool Esc, Inv, Next, Prev, Alt, Left, Right;
        /// <summary>Where the mouse is (screen pixels). Stays put between frames.</summary>
        public Vector2 MousePos;
        bool _esc, _inv, _next, _prev, _alt, _left, _right;

        void Tick()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _esc = Esc; _inv = Inv; _next = Next; _prev = Prev; _alt = Alt; _left = Left; _right = Right;
            Esc = Inv = Next = Prev = Alt = Left = Right = false;
        }

        public override Vector2 MouseScreen => MousePos;
        public override bool MouseLeft { get { Tick(); return _left; } }
        public override bool MouseRight { get { Tick(); return _right; } }

        public override string DeviceName => "Script";
        public override bool IsConnected => true;
        public override bool UsesMouse => true;
        public override Vector2 Move => Vector2.zero;
        public override bool Confirm => false;
        public override bool Cancel { get { Tick(); return _esc; } }
        public override bool Pause { get { Tick(); return _esc; } }
        public override bool Inventory { get { Tick(); return _inv; } }
        public override bool CharacterSheet => false;
        public override bool Spells => false;
        public override bool Journal => false;
        public override bool EndTurn => false;
        public override bool NextTab { get { Tick(); return _next; } }
        public override bool PrevTab { get { Tick(); return _prev; } }
        public override bool AltAction { get { Tick(); return _alt; } }
        public override bool QuickModifier => false;
        public override bool SneakToggle => false;
        public override bool Highlight => false;
        public override int HotbarPressed => -1;
        public override Vector2 Cursor => Vector2.zero;
        public override bool UseItem => false;
        public override bool Sort => false;
        public override bool Give => false;
        protected override Vector2 NavRaw() => Vector2.zero;
    }

    public class SmokeTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f; // tests run silently
            Sfx.Volume = 0f;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator EveryClassAndSpeciesSpawns()
        {
            foreach (var cls in DnD.Rules.Content.CharacterDb.Classes.Keys.ToList())
                foreach (var sp in new[] { "human", "elf", "half_elf", "dwarf", "gnome" })
                {
                    var slot = GameRoot.I.DebugStart(new FuzzInput(1) { ConfirmChance = 0 }, cls, sp);
                    yield return null;
                    Assert.IsNotNull(slot.Avatar, $"{cls}/{sp} avatar");
                    Assert.Greater(slot.Character.MaxHP, 0);
                    GameRoot.I.QuitToTitle();
                    yield return null;
                }
        }

        [UnityTest]
        public IEnumerator FuzzTheTown()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(7), "bard", "half_elf");
            g.Warp(p, "town", new Vector2Int(25, 19), false);
            Time.timeScale = 4;
            yield return Frames(2500);
            Assert.IsTrue(g.InGame);
        }

        [UnityTest]
        public IEnumerator FuzzTheForest()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(11), "fighter", "human");
            g.Warp(p, "forest", new Vector2Int(40, 18), false);
            Time.timeScale = 4;
            yield return Frames(2500);
            Assert.IsTrue(g.InGame);
        }

        /// <summary>Searching a hidden spot rolls the visible d20 and either rewards once or locks until tomorrow.</summary>
        [UnityTest]
        public IEnumerator SecretSearchRollsAndRewardsOnce()
        {
            var g = GameRoot.I;
            var input = new FuzzInput(5) { ConfirmChance = 1, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 };
            var p = g.DebugStart(input, "bard", "human");
            g.Warp(p, "forest", new Vector2Int(19, 25), false);
            yield return null;
            var log = System.Array.Find(Object.FindObjectsByType<SecretInteractable>(), s => s.Def.Id == "forest_log");
            Assert.IsNotNull(log, "hollow log secret was not built");
            int potions = p.Character.CountOf("potion_healing");

            log.Interact(p);
            Assert.IsTrue(p.Hud.Dice.IsOpen, "the search should open the dice roller");
            for (int i = 0; i < 600 && (p.Hud.Dice.IsOpen || !p.Hud.HasScreen); i++) yield return null;
            Assert.IsTrue(p.Hud.Top is MessageScreen, "a result message should follow the roll");
            bool found = g.World.Flag("secret:forest_log");
            Assert.AreEqual(found ? potions + 2 : potions, p.Character.CountOf("potion_healing"));
            p.Hud.CloseAll();

            // A second attempt never pays out twice: either "already found" or "come back tomorrow".
            log.Interact(p);
            Assert.IsFalse(p.Hud.Dice.IsOpen, "no second roll on the same day");
            Assert.AreEqual(found ? potions + 2 : potions, p.Character.CountOf("potion_healing"));
        }

        /// <summary>Hand-edited maps drift: every warp, spawn, NPC spot and secret must land on a sensible tile.</summary>
        [UnityTest]
        public IEnumerator WorldDataIsConsistent()
        {
            var g = GameRoot.I;
            g.DebugStart(new FuzzInput(1), "cleric", "dwarf");
            yield return null;
            foreach (var z in g.Zones.Values)
            {
                Assert.IsTrue(z.IsWalkable(z.Def.DefaultSpawn), $"{z.Def.Id} default spawn {z.Def.DefaultSpawn}");
                foreach (var w in z.Def.Warps)
                {
                    Assert.IsTrue(g.Zones.ContainsKey(w.ToZone), $"{z.Def.Id} warp to unknown zone {w.ToZone}");
                    Assert.IsTrue(g.Zones[w.ToZone].IsWalkable(w.ToPos), $"{z.Def.Id} warp {w.At} lands on blocked {w.ToZone} {w.ToPos}");
                    Assert.AreEqual(null, g.Zones[w.ToZone].WarpAt(w.ToPos), $"{z.Def.Id} warp {w.At} lands on another warp");
                }
                foreach (var grp in z.Def.EnemyGroups)
                    foreach (var m in grp.Members)
                        Assert.IsTrue(z.IsWalkable(m.Pos), $"{z.Def.Id} {grp.Id} spawn {m.Pos}");
                foreach (var kv in z.Def.Secrets)
                    if (kv.Value.IsQuestSpot) Assert.IsTrue(z.IsWalkable(kv.Key) || "qOY".IndexOf(z.CharAt(kv.Key)) >= 0, $"{z.Def.Id} quest spot {kv.Value.Id} at {kv.Key} is blocked");
                    else Assert.IsTrue("qOY".IndexOf(z.CharAt(kv.Key)) >= 0, $"{z.Def.Id} secret {kv.Value.Id} at {kv.Key} is not on a secret tile");
                foreach (var c in z.Def.Chests)
                    Assert.AreEqual('c', z.CharAt(c.Pos), $"{z.Def.Id} chest {c.Id}");
            }
            foreach (var npc in WorldDb.Npcs.Values)
                foreach (var e in npc.Schedule)
                    if (e.Zone != null)
                        Assert.IsTrue(g.Zones[e.Zone].IsWalkable(e.Pos), $"{npc.Id} at {e.Zone} {e.Pos} ({e.Activity})");
        }

        [UnityTest, Timeout(900000)] // 16 conversations x 3 rounds of 400 frames
        public IEnumerator FuzzEveryConversation()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(11) { ConfirmChance = 0.12f, CancelChance = 0.005f }, "cleric", "dwarf");
            foreach (var id in new[] { "brannoc", "elowen", "pip", "rhoswen", "tobin", "maddie", "snik", "wren", "hobb", "garrick", "hesper", "dunstan", "lark", "tamsin", "wick", "thimble" })
            {
                for (int round = 0; round < 3; round++)
                {
                    p.Hud.CloseAll();
                    var npc = Object.FindObjectsByType<NpcAgent>().FirstOrDefault(n => n.Def.Dialogue == id);
                    p.Hud.Push(new DialogueScreen(id, npc, null));
                    yield return Frames(400);
                    while (g.AnyInCombat) yield return Frames(60); // Snik may start a fight
                }
            }
        }

        [UnityTest]
        public IEnumerator FightInTheMineWithEachClass()
        {
            foreach (var cls in DnD.Rules.Content.CharacterDb.Classes.Keys.ToList())
            {
                var g = GameRoot.I;
                var p = g.DebugStart(new FuzzInput(cls.GetHashCode()) { ConfirmChance = 0.35f, EndTurnChance = 0.01f }, cls, "human");
                g.Warp(p, "mine", new Vector2Int(19, 20), false);
                yield return null;
                g.StartGroupCombat("mine_hub", p);
                g.StartGroupCombat("mine_hub", p); // a second request must not start a duplicate fight
                Assert.AreEqual(1, g.Combats.Count, "exactly one combat");
                Time.timeScale = 6;
                float start = Time.realtimeSinceStartup;
                while (g.AnyInCombat && Time.realtimeSinceStartup - start < 150f) yield return null;
                Time.timeScale = 1;
                Assert.IsFalse(g.AnyInCombat, $"{cls}: combat should finish (won or lost) within 150 real seconds");
                g.QuitToTitle();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SaveAndLoadRoundTrip()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(3) { ConfirmChance = 0 }, "wizard", "gnome");
            yield return null;
            p.Character.XP = 420;
            p.Character.Copper = 12345;
            p.Character.AddItem("torch", 7);
            p.Character.HP = 3;
            p.Character.AsiBonus[(int)Ability.INT] = 2;
            p.Character.Feats.Add("grappler");
            g.World.AddFriendship("brannoc", 150);
            g.Quests.Start("fallen_star");
            g.Warp(p, "tavern", new Vector2Int(8, 9), false);
            string path = SaveSystem.Save(false);
            Assert.IsNotNull(path);
            var before = SaveSystem.ToData(p.Character);
            g.QuitToTitle();
            yield return null;
            g.LoadGame(path);
            yield return null;
            var after = g.Players[0];
            Assert.AreEqual(before.XP, after.Character.XP);
            Assert.AreEqual(12345, after.Character.Copper);
            Assert.AreEqual(3, after.Character.HP);
            Assert.AreEqual(2, after.Character.AsiBonus[(int)Ability.INT], "ability score improvements are saved");
            CollectionAssert.Contains(after.Character.Feats, "grappler", "feats are saved");
            Assert.AreEqual(before.BagIds, SaveSystem.ToData(after.Character).BagIds);
            Assert.AreEqual("tavern", after.Zone.Def.Id);
            Assert.AreEqual(1, g.World.Hearts("brannoc"));
            Assert.AreEqual(1, g.Quests.Stage("fallen_star"));
            Assert.GreaterOrEqual(SaveSystem.List().Count, 1);
        }

        [Test]
        public void ClassChoicesSurviveSaving()
        {
            var lock_ = CharacterFactory.Create(CharacterFactory.DefaultChoices("warlock", "human", "Vex"));
            lock_.Invocations.Clear(); lock_.Invocations.Add("ember_sight");
            var back = SaveSystem.FromData(SaveSystem.ToData(lock_));
            CollectionAssert.AreEqual(new[] { "ember_sight" }, back.Invocations, "invocations are saved (and not reset to the default)");

            var sorc = CharacterFactory.Create(CharacterFactory.DefaultChoices("sorcerer", "elf", "Ash"));
            sorc.Metamagic.Add("quickened"); sorc.Metamagic.Add("careful");
            CollectionAssert.AreEquivalent(new[] { "quickened", "careful" }, SaveSystem.FromData(SaveSystem.ToData(sorc)).Metamagic, "metamagic is saved");
        }

        [UnityTest]
        public IEnumerator MultiplePlaythroughsAreKeptApart()
        {
            var g = GameRoot.I;
            var a = g.DebugStart(new FuzzInput(1) { ConfirmChance = 0 }, "fighter", "human");
            string idA = g.PlaythroughId;
            SaveSystem.Save(false);
            g.QuitToTitle();
            yield return new WaitForSecondsRealtime(1.1f); // distinct timestamped id
            g.DebugStart(new FuzzInput(2) { ConfirmChance = 0 }, "bard", "elf");
            string idB = g.PlaythroughId;
            SaveSystem.Save(false);
            SaveSystem.Save(true);
            Assert.AreNotEqual(idA, idB);
            var list = SaveSystem.List();
            Assert.AreEqual(1, list.Count(s => s.PlaythroughId == idA));
            Assert.AreEqual(2, list.Count(s => s.PlaythroughId == idB));
        }

        [UnityTest]
        public IEnumerator FuzzInventoryShopAndMenus()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(21) { ConfirmChance = 0.3f, CancelChance = 0.002f }, "fighter", "dwarf");
            p.Character.Copper = 100000;
            p.Hud.Push(new InventoryScreen(null, null));
            yield return Frames(600);
            p.Hud.CloseAll();
            p.Hud.Push(new ShopScreen("general"));
            yield return Frames(600);
            p.Hud.CloseAll();
            p.Hud.Push(new InventoryScreen(g.World.Storage, "Storage"));
            yield return Frames(600);
            p.Hud.CloseAll();
            p.Hud.Push(new CharacterSheetScreen());
            yield return Frames(200);
            p.Hud.CloseAll();
            p.Hud.Push(new SpellbookScreen());
            yield return Frames(200);
            p.Hud.CloseAll();
            p.Hud.Push(new RestScreen(true));
            yield return Frames(300);
            p.Hud.CloseAll();
            int total = p.Character.Bag.TotalItems();
            Assert.GreaterOrEqual(total, 0);
        }

        [UnityTest]
        public IEnumerator CharacterCreationWithRandomInput()
        {
            var g = GameRoot.I;
            g.StartNewGame(new FuzzInput(99) { ConfirmChance = 0.2f, CancelChance = 0.01f }, Difficulty.Normal);
            Time.timeScale = 4;
            yield return Frames(4000);
            Assert.IsTrue(g.InGame);
        }

        [UnityTest]
        public IEnumerator TwoPlayerSplitScreen()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(5), "fighter", "human");
            var p2 = g.DebugStart(new FuzzInput(6), "cleric", "elf");
            yield return null;
            Assert.AreEqual(2, g.Players.Count);
            Assert.AreEqual(0.5f, p1.Cam.rect.width, 0.01f);
            Assert.AreEqual(0.5f, p2.Cam.rect.width, 0.01f);
            g.Warp(p2, "shop", new Vector2Int(6, 7), false);
            Time.timeScale = 4;
            yield return Frames(1500);
        }

        [UnityTest]
        public IEnumerator FuzzTheFord()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(13), "cleric", "elf");
            g.Warp(p, "ford", new Vector2Int(1, 14), false);
            Time.timeScale = 4;
            yield return Frames(2500);
            Assert.IsTrue(g.InGame);
        }

        /// <summary>Playtest bug: Esc closed a menu and the same key press opened the pause menu (menus felt impossible to leave).</summary>
        [UnityTest]
        public IEnumerator EscClosesMenusWithoutOpeningAnother()
        {
            var g = GameRoot.I;
            var input = new ScriptInput();
            var p = g.DebugStart(input, "bard", "human");
            yield return Frames(2);
            var screens = new GameScreen[] { new InventoryScreen(null, null), new CharacterSheetScreen(), new SpellbookScreen(), new JournalScreen(), new ShopScreen("general"), new RestScreen(true) };
            foreach (var s in screens)
            {
                p.Hud.Push(s);
                yield return Frames(2);
                input.Esc = true;
                yield return Frames(3);
                Assert.IsFalse(p.Hud.HasScreen, $"Esc should close {s.GetType().Name} and nothing else should open");
            }
            input.Inv = true;
            yield return Frames(3);
            Assert.IsTrue(p.Hud.Top is InventoryScreen, "I opens the inventory");
            input.Inv = true;
            yield return Frames(3);
            Assert.IsFalse(p.Hud.HasScreen, "I again closes it (and doesn't reopen it)");
            input.Esc = true;
            yield return Frames(3);
            Assert.IsTrue(p.Hud.Top is PauseScreen, "Esc in the world opens the menu");
            input.Esc = true;
            yield return Frames(3);
            Assert.IsFalse(p.Hud.HasScreen, "Esc again closes the menu");
        }

        /// <summary>"Reward all playstyles": talking a group down pays exactly the XP of beating them, and companions never dilute it.</summary>
        [UnityTest]
        public IEnumerator TalkingAndFightingPayTheSameXp()
        {
            var g = GameRoot.I;
            int expected = 3 * DnD.Rules.Content.MonstersDb.Get("goblin").XP;
            var input = new FuzzInput(4) { ConfirmChance = 1, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 };
            int[] gained = new int[2], compGained = new int[2];
            for (int run = 0; run < 2; run++)
            {
                var p = g.DebugStart(input, "bard", "human");
                yield return null;
                g.HireRecruit(p, Object.FindObjectsByType<RecruitAgent>().First());
                var comp = g.CompanionsOf(p).First();
                g.Warp(p, "forest", new Vector2Int(19, 25), false);
                yield return null;
                p.Hud.CloseAll();
                int before = p.Character.XP, compBefore = comp.Character.XP;
                if (run == 0) g.PacifyGroup("forest_goblins", p);
                else
                {
                    g.StartGroupCombat("forest_goblins", p);
                    foreach (var u in g.Combats.Single().Units) if (u.C.Team == Team.Enemies) u.C.HP = 0;
                    Time.timeScale = 4;
                    float start = Time.realtimeSinceStartup;
                    while (g.AnyInCombat && Time.realtimeSinceStartup - start < 60f) yield return null;
                    Time.timeScale = 1;
                    Assert.IsFalse(g.AnyInCombat, "the fight should end once every goblin is down");
                }
                gained[run] = p.Character.XP - before;
                compGained[run] = comp.Character.XP - compBefore;
                g.QuitToTitle();
                yield return null;
            }
            Assert.AreEqual(expected, gained[0], "peaceful XP = the group's full XP");
            Assert.AreEqual(expected, gained[1], "combat XP = the group's full XP (the companion doesn't take a share)");
            Assert.AreEqual(gained[0], compGained[0], "companions earn what their leader earns");
            Assert.AreEqual(gained[1], compGained[1]);
        }

        [UnityTest]
        public IEnumerator BeingSpottedOffersChoicesAndSneakingAllowsAmbush()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(3) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "fighter", "human");
            g.Warp(p, "forest", new Vector2Int(19, 25), false);
            yield return null;
            p.Hud.CloseAll();
            var gob = Object.FindObjectsByType<EnemyAgent>().First(e => e.Group.Id == "forest_goblins");
            g.OnEnemyNoticed(gob, p);
            Assert.IsFalse(g.AnyInCombat, "being spotted asks what you do instead of starting a fight");
            Assert.IsTrue(p.Hud.Top is DialogueScreen, "the encounter choice is shown");
            var first = p.Hud.Top;
            g.OnEnemyNoticed(gob, p);
            Assert.AreSame(first, p.Hud.Top, "a second sighting mid-conversation doesn't stack another one");
            yield return Frames(3);

            p.Hud.CloseAll();
            p.Avatar.Sneaking = true;
            var amb = gob.GetComponent<AmbushInteractable>();
            Assert.IsTrue(amb.Available(p), "sneaking players can ambush an unaware enemy");
            amb.Interact(p);
            Assert.IsTrue(g.AnyInCombat, "the ambush starts the fight");
        }

        [UnityTest]
        public IEnumerator CompanionsTakeOrdersCanBeHealedAndThePartyCanRetreat()
        {
            var g = GameRoot.I;
            var input = new FuzzInput(17) { ConfirmChance = 1, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
            var p = g.DebugStart(input, "bard", "human");
            yield return null;
            g.HireRecruit(p, Object.FindObjectsByType<RecruitAgent>().First());
            var comp = g.CompanionsOf(p).First();
            Assert.IsTrue(comp.Manual, "by default you command your companions in combat");

            // Out of combat: hand them a potion.
            comp.Character.HP = 1;
            p.Character.AddItem("potion_healing");
            int idx = System.Array.FindIndex(p.Character.Bag.Slots, s => s != null && s.ItemId == "potion_healing");
            int potions = p.Character.CountOf("potion_healing");
            Assert.IsTrue(g.GivePotionTo(p, comp.Character, idx));
            for (int i = 0; i < 300 && p.Hud.Dice.IsOpen; i++) yield return null;
            Assert.Greater(comp.Character.HP, 1, "the potion healed the companion");
            Assert.AreEqual(potions - 1, p.Character.CountOf("potion_healing"));

            // In combat: the companion's unit is driven by its leader.
            g.Warp(p, "mine", new Vector2Int(19, 20), false);
            yield return null;
            p.Hud.CloseAll();
            g.StartGroupCombat("mine_hub", p);
            var c = g.Combats.Single();
            var cu = c.Units.First(u => u.Comp == comp);
            Assert.AreSame(p, cu.Ctrl, "the leader controls the companion's turns");
            Assert.IsTrue(cu.IsHuman);

            // Retreat: once it's someone's turn, a successful retreat ends the fight with no winner.
            float until = Time.realtimeSinceStartup + 30;
            while (!(c.Current != null && c.Current.IsHuman && p.Hud.Top is CombatHud && !p.Hud.Dice.IsOpen) && !c.Finished && Time.realtimeSinceStartup < until) yield return null;
            if (!c.Finished)
            {
                c.Retreat();
                yield return Frames(5);
                Assert.IsFalse(g.AnyInCombat);
                Assert.IsFalse(p.InCombat);
                Assert.IsTrue(g.World.IsLocked("calm:mine_hub"), "the group loses track of you for a while");
                Assert.IsFalse(g.World.DefeatedGroups.Contains("mine_hub"), "retreating isn't winning");
                Assert.IsFalse(p.Hud.Find<CombatHud>() != null, "combat UI closed");
            }
        }

        /// <summary>Playtest 3: companions walked on top of each other, and the player wanted them in front when a fight starts.</summary>
        [UnityTest]
        public IEnumerator CompanionsKeepFormationAndTakePointIntoFights()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(9) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 }, "bard", "human");
            yield return null;
            foreach (var r in Object.FindObjectsByType<RecruitAgent>().Take(2).ToList()) g.HireRecruit(p, r);
            var comps = g.CompanionsOf(p).Where(c => !c.IsDragon).ToList();
            Assert.AreEqual(2, comps.Count);
            g.Warp(p, "town", new Vector2Int(25, 19), false);
            yield return Frames(90);
            Assert.Greater(Vector2.Distance(comps[0].transform.position, comps[1].transform.position), 0.6f, "companions keep separate spots instead of stacking");

            comps[0].TakePoint = true;
            comps[0].Stance = CompanionStance.Ranged; // only taking point should put them in front
            comps[1].Stance = CompanionStance.Ranged;
            g.Warp(p, "mine", new Vector2Int(19, 24), false);
            yield return null;
            p.Hud.CloseAll();
            g.StartGroupCombat("mine_hub", p);
            var c = g.Combats.Single();
            var me = c.Units.First(u => u.Owner == p);
            var point = c.Units.First(u => u.Comp == comps[0]);
            var foe = c.Units.Where(u => u.C.Team == Team.Enemies).OrderBy(u => Zone.DistanceFt(u.Pos, me.Pos)).First();
            Assert.Less(Zone.DistanceFt(point.Pos, foe.Pos), Zone.DistanceFt(me.Pos, foe.Pos), "the point companion starts the fight between you and the enemy");
        }

        [UnityTest]
        public IEnumerator TheFordQuestCanBeSolvedByTalking()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(2) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0 }, "bard", "human");
            g.Warp(p, "ford", new Vector2Int(1, 14), false);
            yield return null;
            Assert.AreEqual(1, g.Quests.Stage("east_road"), "arriving at the ford starts the quest");
            int before = p.Character.XP;
            g.World.SetFlag("wren_legit");
            g.PacifyGroup("ford_toll", p);
            Assert.AreEqual(2, g.Quests.Stage("east_road"));
            int expected = new[] { "wren", "bandit", "bandit" }.Sum(id => DnD.Rules.Content.MonstersDb.Get(id).XP);
            Assert.AreEqual(expected, p.Character.XP - before, "talking Wren's crew down pays their full XP");
            g.CompleteEastRoad(p);
            Assert.AreEqual(3, g.Quests.Stage("east_road"));
            Assert.IsTrue(g.Quests.Done("east_road"));
        }

        [UnityTest]
        public IEnumerator HatchTheEggPeacefully()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0 }, "bard", "human");
            g.Warp(p, "egg_shrine", WorldDb.Zones["egg_shrine"].DefaultSpawn, false);
            yield return null;
            g.PacifyGroup("mine_egg", p);
            Assert.AreEqual(FallenStar.Egg, g.Quests.Stage("fallen_star"));
            Assert.AreEqual(1, p.Character.CountOf("copper_kettle"));
            g.HatchEgg(p, Vector3.zero);
            Assert.IsTrue(g.World.EggHatched);
            Assert.AreEqual(FallenStar.Hatched, g.Quests.Stage("fallen_star"));
        }
    }
}
