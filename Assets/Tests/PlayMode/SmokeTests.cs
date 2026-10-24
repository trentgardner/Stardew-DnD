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
            foreach (var cls in new[] { "fighter", "wizard", "cleric", "bard" })
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
            g.Warp(p, "town", new Vector2Int(19, 16), false);
            Time.timeScale = 4;
            yield return Frames(2500);
            Assert.IsTrue(g.InGame);
        }

        [UnityTest]
        public IEnumerator FuzzEveryConversation()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(11) { ConfirmChance = 0.12f, CancelChance = 0.005f }, "cleric", "dwarf");
            foreach (var id in new[] { "brannoc", "elowen", "pip", "rhoswen", "tobin", "maddie", "snik" })
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
            foreach (var cls in new[] { "fighter", "wizard", "cleric", "bard" })
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
            Assert.AreEqual(before.BagIds, SaveSystem.ToData(after.Character).BagIds);
            Assert.AreEqual("tavern", after.Zone.Def.Id);
            Assert.AreEqual(1, g.World.Hearts("brannoc"));
            Assert.AreEqual(1, g.Quests.Stage("fallen_star"));
            Assert.GreaterOrEqual(SaveSystem.List().Count, 1);
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
        public IEnumerator HatchTheEggPeacefully()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(8) { ConfirmChance = 0 }, "bard", "human");
            g.Warp(p, "mine", new Vector2Int(19, 9), false);
            yield return null;
            g.PacifyGroup("mine_egg", p);
            Assert.AreEqual(2, g.Quests.Stage("fallen_star"));
            Assert.AreEqual(1, p.Character.CountOf("copper_kettle"));
            g.HatchEgg(p, Vector3.zero);
            Assert.IsTrue(g.World.EggHatched);
            Assert.AreEqual(3, g.Quests.Stage("fallen_star"));
        }
    }
}
