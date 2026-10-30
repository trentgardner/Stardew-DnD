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
    /// <summary>Phase 6: the party's baby dragon.</summary>
    public class DragonTests
    {
        string _tempSaves;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
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

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>Hatches the egg for p (guardians talked down) and names the dragon, skipping the UI.</summary>
        static void Hatch(GameRoot g, PlayerSlot p, string name = "Cinder")
        {
            g.PacifyGroup("mine_egg", p);
            g.HatchEgg(p, Vector3.zero);
            p.Hud.CloseAll();
            g.NameDragon(p, name);
        }

        // ============================================================ Task 1: the creature and party ownership

        [UnityTest]
        public IEnumerator HatchedDragonIsAPartyCreature()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "human");
            g.Warp(p, "egg_shrine", WorldDb.Zones["egg_shrine"].DefaultSpawn, false);
            yield return Frames(2);
            Assert.IsNull(g.Dragon, "no dragon before the hatching");
            Hatch(g, p, "Ziggy");
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual(Team.Players, g.Dragon.Team);
            Assert.AreEqual("Ziggy", g.Dragon.Name);
            Assert.AreEqual(10, g.Dragon.MaxHP, "party level 1");
            Assert.IsNotNull(g.DragonAgent);
            Assert.AreSame(g.Dragon, g.DragonAgent.Actor.Creature);
        }

        [UnityTest]
        public IEnumerator DragonFollowsNearestPlayerWithoutFlicker()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(2), "fighter", "human");
            var p2 = g.DebugStart(Still(3), "cleric", "elf");
            yield return Frames(2);
            g.Warp(p1, "town", new Vector2Int(20, 20), false);
            g.Warp(p2, "forest", WorldDb.Zones["forest"].DefaultSpawn, false);
            yield return Frames(2);
            Hatch(g, p1);
            yield return Frames(2);
            var dragon = g.DragonAgent;
            for (int i = 0; i < 60; i++)
            {
                Assert.AreSame(p1, dragon.Leader, "P2 is in another zone: the dragon stays with P1");
                Assert.AreEqual("town", dragon.Actor.Zone.Def.Id);
                yield return null;
            }

            // Both in town: P2 comes over to the dragon while P1 walks off.
            g.Warp(p2, "town", new Vector2Int(20, 21), false);
            yield return Frames(2);
            Assert.AreSame(p1, dragon.Leader, "P2 isn't clearly nearer yet: no hand-off (hysteresis)");
            p1.Avatar.PlaceAt(p1.Zone, new Vector2Int(40, 20)); // P1 alone (MoveWithin would bring the party along)
            int switches = 0;
            var last = dragon.Leader;
            for (int i = 0; i < 60; i++)
            {
                if (dragon.Leader != last) { switches++; last = dragon.Leader; }
                yield return null;
            }
            Assert.AreSame(p2, dragon.Leader, "the dragon follows whoever is nearest");
            Assert.AreEqual(1, switches, "one clean hand-off, no flicker");
        }

        [UnityTest]
        public IEnumerator RestRestoresTheDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(4), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            g.Dragon.HP = 2;
            g.Dragon.BreathReady = false;
            g.World.DragonScared = true;
            g.World.DragonSniffUsed = true;
            g.OnPartyRest(false);
            Assert.AreEqual(g.Dragon.MaxHP, g.Dragon.HP);
            Assert.IsTrue(g.Dragon.BreathReady);
            Assert.IsFalse(g.World.DragonScared);
            Assert.IsFalse(g.World.DragonSniffUsed);
        }

        [UnityTest]
        public IEnumerator DragonDamageAndBondSurviveSaveAndLoad()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p, "Biscuit");
            g.Dragon.HP = 4;
            g.World.DragonBond = 37;
            g.World.DragonScared = true;
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual("Biscuit", g.Dragon.Name);
            Assert.AreEqual(4, g.Dragon.HP);
            Assert.AreEqual(37, g.World.DragonBond);
            Assert.IsTrue(g.World.DragonScared);
        }

        [UnityTest]
        public IEnumerator OldHatchedSaveGetsAWorkingDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(6), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            string path = SaveSystem.Save(false);
            // A save from before Phase 6: no dragon fields at all except the name and leader.
            var lines = File.ReadAllLines(path).Where(l => !(l.Contains("\"Dragon") && !l.Contains("\"DragonName") && !l.Contains("\"DragonLeader")));
            File.WriteAllLines(path, lines);
            g.LoadGame(path);
            yield return Frames(2);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual(g.Dragon.MaxHP, g.Dragon.HP);
            Assert.AreEqual(0, g.World.DragonBond);
            Assert.IsFalse(g.World.DragonScared);
        }

        // ============================================================ Task 2: the dragon in combat

        /// <summary>P1 in the forest by the wolves, the dragon hatched and at their side.</summary>
        static IEnumerator ByTheWolves(GameRoot g, PlayerSlot p)
        {
            Hatch(g, p);
            g.Warp(p, "forest", new Vector2Int(34, 8), false);
            yield return Frames(3);
            p.Hud.CloseAll();
        }

        static Unit DragonUnit(CombatInstance c) => c.Units.FirstOrDefault(u => u.C is Dragonling);

        [UnityTest]
        public IEnumerator DragonJoinsFightNearAPlayer()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(10), "fighter", "human");
            yield return Frames(2);
            yield return ByTheWolves(g, p);
            Assert.AreEqual("forest", g.DragonAgent.Actor.Zone.Def.Id, "the dragon came along");
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var du = DragonUnit(c);
            Assert.IsNotNull(du, "the dragon fights alongside");
            Assert.IsTrue(du.Comp.IsDragon);
            Assert.AreSame(p, du.Ctrl, "under the player's command by default");
        }

        [UnityTest]
        public IEnumerator ScaredDragonSitsOut()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(11), "fighter", "human");
            yield return Frames(2);
            yield return ByTheWolves(g, p);
            g.World.DragonScared = true;
            g.StartGroupCombat("forest_wolves", p);
            Assert.IsNull(DragonUnit(g.Combats.Single()), "a scared dragon hides behind the party");
        }

        [UnityTest]
        public IEnumerator EitherPlayerCommandsTheDragon()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(12), "fighter", "human");
            var p2 = g.DebugStart(Still(13), "cleric", "elf");
            yield return Frames(2);
            yield return ByTheWolves(g, p1);
            g.Warp(p2, "forest", new Vector2Int(34, 9), false);
            p1.Avatar.PlaceAt(p1.Zone, WorldDb.Zones["forest"].DefaultSpawn); // P1 wanders off, far from the wolves
            yield return null;
            var agent = g.DragonAgent;
            agent.Leader = p1;
            agent.Actor.PlaceAt(p2.Zone, new Vector2Int(33, 9));
            g.StartGroupCombat("forest_wolves", p2);
            var c = g.Combats.Single();
            Assert.IsFalse(p1.InCombat, "P1 is too far away to join");
            var du = DragonUnit(c);
            Assert.IsNotNull(du);
            Assert.AreSame(p2, du.Ctrl, "the player in the fight commands the dragon");
        }

        [UnityTest]
        public IEnumerator EmberBreathRollsASaveAndRecharges()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(14), "fighter", "human");
            yield return Frames(2);
            yield return ByTheWolves(g, p);
            g.DragonAgent.Manual = false; // its own dice on the ticker, no tray to click through
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var du = DragonUnit(c);
            if (du.Cb == null) du.Cb = new Combatant(du.C);
            var wolf = c.Units.First(u => u.C.Team == Team.Enemies);
            wolf.A.PlaceAt(c.Zone, du.Pos + Vector2Int.right);
            yield return c.EmberBreath(du, wolf.Pos);
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("DEX save")), "the wolf rolls a visible DEX save");
            Assert.IsFalse(g.Dragon.BreathReady, "the breath needs to recharge");
            c.RechargeBreath(du, () => 4);
            Assert.IsFalse(g.Dragon.BreathReady, "a 4 doesn't recharge it");
            c.RechargeBreath(du, () => 6);
            Assert.IsTrue(g.Dragon.BreathReady, "a 6 does");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("Recharge")), "the recharge roll is shown");
            var acts = c.DragonActions(du).Select(a => a.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Move", "Attack (Bite)", "Ember Breath", "Dodge", "Auto", "End Turn" }, acts);
        }

        [UnityTest]
        public IEnumerator ScaredDragonLeavesFightAndPartyCanStillWin()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(15) { CancelChance = 0, MenuChance = 0 }, "fighter", "human"); // clicks through the dice trays
            yield return Frames(2);
            yield return ByTheWolves(g, p);
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            var du = DragonUnit(c);
            g.Dragon.TakeDamage(999, DamageType.Piercing, false, new System.Collections.Generic.List<string>());
            c.OnDamaged(du);
            Assert.IsTrue(g.World.DragonScared);
            Assert.IsFalse(g.Dragon.IsDead, "it never dies");
            Assert.IsFalse(du.A.gameObject.activeSelf, "it bolts out of the fight");
            Assert.IsTrue(c.LogLines.Any(l => l.Contains("retreats, scared")));
            Assert.IsFalse(c.Finished, "the fight goes on");

            foreach (var u in c.Units) if (u.C.Team == Team.Enemies) u.C.HP = 0;
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 60f) yield return null;
            Time.timeScale = 1;
            Assert.IsFalse(g.AnyInCombat);
            Assert.IsTrue(g.World.DefeatedGroups.Contains("forest_wolves"), "a victory");
            yield return Frames(2);
            Assert.IsTrue(g.DragonAgent.gameObject.activeSelf, "it comes back out once the fight is over");
            Assert.IsTrue(g.World.DragonScared, "still scared until a rest");
        }

        [UnityTest]
        public IEnumerator DragonIsNotEnoughToKeepAFightGoing()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(16) { CancelChance = 0, MenuChance = 0 }, "fighter", "human"); // clicks through the dice trays
            yield return Frames(2);
            yield return ByTheWolves(g, p);
            g.StartGroupCombat("forest_wolves", p);
            var c = g.Combats.Single();
            Assert.IsTrue(DragonUnit(c).Active);
            g.Dragon.MaxHP = g.Dragon.HP = 999; // the wolves can't knock it out, so only the rule can end the fight
            p.Character.HP = 0;
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            while (!c.Finished && Time.realtimeSinceStartup - start < 60f) yield return null;
            Time.timeScale = 1;
            Assert.IsTrue(c.Finished, "the hatchling alone doesn't keep a lost fight going");
        }

        [UnityTest]
        public IEnumerator DragonJoinsOnlyOneFight()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(17), "fighter", "human");
            var p2 = g.DebugStart(Still(18), "cleric", "elf");
            yield return Frames(2);
            yield return ByTheWolves(g, p1);
            g.StartGroupCombat("forest_wolves", p1);
            Assert.IsNotNull(DragonUnit(g.Combats.Single()));
            g.Warp(p2, "forest", new Vector2Int(11, 25), false);
            g.DragonAgent.Actor.PlaceAt(p2.Zone, new Vector2Int(11, 26));
            g.StartGroupCombat("forest_goblins", p2);
            var second = g.Combats.First(x => x.GroupId == "forest_goblins");
            Assert.IsNull(DragonUnit(second), "already busy with the wolves");
        }

        // ============================================================ Task 3: pet, fetch, treats, bond

        static DragonPet Pet(GameRoot g) => g.DragonAgent.GetComponent<DragonPet>();

        [UnityTest]
        public IEnumerator PettingRaisesBondOncePerHour()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(20), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            yield return null;
            Assert.IsNotNull(Pet(g), "the dragon has its pet behaviour");
            Assert.AreEqual(0, g.World.DragonBond);
            Pet(g).PetIt(p);
            Pet(g).PetIt(p);
            Assert.AreEqual(2, g.World.DragonBond, "petting counts once an hour");
            g.Clock.Advance(61);
            Pet(g).PetIt(p);
            Assert.AreEqual(4, g.World.DragonBond);
        }

        [UnityTest]
        public IEnumerator TreatNeedsAndConsumesATreat()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(21), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            yield return null;
            Assert.IsFalse(Pet(g).GiveTreat(p), "no treat in the bag");
            Assert.AreEqual(0, g.World.DragonBond);
            p.Character.AddItem("dragon_treat");
            Assert.IsTrue(Pet(g).GiveTreat(p));
            Assert.AreEqual(0, p.Character.CountOf("dragon_treat"), "eaten");
            Assert.AreEqual(5, g.World.DragonBond);
            Assert.IsTrue(ShopDb.Stock("tavern").Contains("dragon_treat"), "Pip sells them");
            Assert.IsTrue(ShopDb.Stock("general").Contains("dragon_treat"), "and so does the general store");
        }

        [UnityTest]
        public IEnumerator FetchBringsTheStickBack()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(22), "fighter", "human");
            g.Warp(p, "town", WorldDb.Zones["town"].DefaultSpawn, false);
            yield return Frames(2);
            Hatch(g, p);
            yield return Frames(30);
            int bond = g.World.DragonBond;
            yield return Pet(g).Fetch(p);
            Assert.LessOrEqual(Vector2.Distance(g.DragonAgent.transform.position, p.Avatar.transform.position), 1.5f, "back at your side");
            Assert.IsNull(GameObject.Find("FetchStick"), "the stick is gone (it has it)");
            Assert.AreEqual(bond + 3, g.World.DragonBond);
        }

        [UnityTest]
        public IEnumerator BondTierShowsOnTheJournal()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(23), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p, "Pebble");
            g.World.DragonBond = 45;
            StringAssert.Contains("Pebble", DragonPet.JournalLine());
            StringAssert.Contains("♥♥♡♡♡", DragonPet.JournalLine());
            g.World.DragonBond = 100;
            StringAssert.Contains("♥♥♥♥♥", DragonPet.JournalLine());
        }

        [UnityTest]
        public IEnumerator BondNeverGoesPastAHundred()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(24), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            DragonPet.AddBond(p, 250, "test");
            Assert.AreEqual(100, g.World.DragonBond);
        }

        // ============================================================ Task 4: Sniff and braziers

        [UnityTest]
        public IEnumerator SniffMarksNearbySecretsAndGivesAdvantage()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(30), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            g.Warp(p, "forest", new Vector2Int(19, 25), false);
            yield return Frames(3);
            var log = System.Array.Find(Object.FindObjectsByType<SecretInteractable>(), s => s.Def.Id == "forest_log");
            Assert.IsFalse(log.Sniffed);
            Assert.AreEqual(AdvantageState.Normal, log.CheckAdvantage);
            Assert.IsTrue(Pet(g).Sniff(p));
            Assert.IsTrue(log.Sniffed, "the hollow log is a few steps away");
            Assert.AreEqual(AdvantageState.Advantage, log.CheckAdvantage, "the dragon points it out: Advantage on the check");
            var far = Object.FindObjectsByType<SecretInteractable>().Where(s => Vector2.Distance(s.transform.position, p.Avatar.transform.position) > 9f);
            Assert.IsTrue(far.All(s => !s.Sniffed), "secrets far away stay hidden");
        }

        [UnityTest]
        public IEnumerator SniffOncePerShortRestShared()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(31), "fighter", "human");
            var p2 = g.DebugStart(Still(32), "cleric", "elf");
            yield return Frames(2);
            Hatch(g, p1);
            yield return null;
            Assert.IsTrue(Pet(g).Sniff(p1));
            Assert.IsFalse(Pet(g).Sniff(p2), "one sniff per short rest for the whole party");
            g.OnPartyRest(false);
            Assert.IsTrue(Pet(g).Sniff(p2), "a rest clears the nose");
        }

        [UnityTest]
        public IEnumerator LitBrazierStaysLitAfterReload()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(33), "fighter", "human");
            yield return Frames(2);
            Hatch(g, p);
            g.Warp(p, "mine", WorldDb.Zones["mine"].DefaultSpawn, false);
            yield return Frames(3);
            var b = UnlitBrazier.All.FirstOrDefault(x => x.Key == "mine:13,2");
            Assert.IsNotNull(b, "the mine has cold braziers");
            Assert.IsFalse(b.Lit);
            Assert.IsTrue(Pet(g).Light(p, b));
            Assert.IsTrue(b.Lit);
            Assert.IsTrue(b.GetComponent<LightSource>() != null && b.GetComponent<LightSource>().On);
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            var again = UnlitBrazier.All.FirstOrDefault(x => x.Key == "mine:13,2");
            Assert.IsNotNull(again);
            Assert.IsTrue(again.Lit, "still burning after a reload");
            Assert.IsTrue(again.GetComponent<LightSource>().On);
        }

        [Test]
        public void UnlitBraziersOnlyWhereDark()
        {
            int count = 0;
            foreach (var z in WorldDb.Zones.Values)
                foreach (var row in z.Map)
                    if (row.Contains('y')) { count++; Assert.IsTrue(z.Dark, $"{z.Id} has a cold brazier but isn't dark"); }
            Assert.Greater(count, 0, "some dark places have cold braziers to light");
        }

        // ============================================================ Task 5: the hatching scene and naming

        static IEnumerator WaitFor(System.Func<bool> cond, int frames = 900)
        {
            for (int i = 0; i < frames && !cond(); i++) yield return null;
        }

        [UnityTest]
        public IEnumerator HatchSceneEndsInNamingAndSpawnsTheDragon()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(40), "fighter", "human");
            g.Warp(p, "egg_shrine", WorldDb.Zones["egg_shrine"].DefaultSpawn, false);
            yield return Frames(2);
            g.PacifyGroup("mine_egg", p);
            p.Hud.CloseAll();
            int xp = p.Character.XP;
            g.HatchEgg(p, Vector3.zero);
            Assert.IsTrue(p.Hud.Top is HatchCutscene, "the hatching plays as a little scene");
            Assert.IsNull(g.Dragon, "no dragon until it's named");
            yield return WaitFor(() => p.Hud.Top is MessageScreen);
            Assert.IsTrue(p.Hud.Top is MessageScreen, "the scene ends in the narration");
            p.Hud.Top.Close();
            yield return WaitFor(() => p.Hud.Top is NameEntryScreen, 10);
            var naming = p.Hud.Top as NameEntryScreen;
            Assert.IsNotNull(naming, "then you name your dragon");
            foreach (var ch in "Ziggy") naming.Type(ch);
            naming.Submit();
            yield return Frames(2);
            Assert.AreEqual("Ziggy", g.World.DragonName);
            Assert.IsNotNull(g.Dragon);
            Assert.AreEqual("Ziggy", g.Dragon.Name);
            Assert.AreEqual(FallenStar.Hatched, g.Quests.Stage("fallen_star"));
            Assert.GreaterOrEqual(p.Character.XP, xp + 100);
            Assert.IsNull(GameObject.Find("HatchlingProp"), "the scene cleans up after itself");
        }

        [UnityTest]
        public IEnumerator NameEntryRejectsEmptyAndKeepsDefault()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(41), "fighter", "human");
            yield return Frames(2);
            string got = null;
            var s = new NameEntryScreen(n => got = n);
            p.Hud.Push(s);
            yield return null;
            foreach (var ch in "3!") s.Type(ch);
            Assert.AreEqual("", s.Typed, "digits and symbols aren't name letters");
            s.Submit();
            Assert.AreEqual("Cinder", got, "an empty name keeps the default");

            got = null;
            var s2 = new NameEntryScreen(n => got = n);
            p.Hud.Push(s2);
            yield return null;
            foreach (var ch in "Sir Reginald Toastypants") s2.Type(ch);
            Assert.AreEqual(16, s2.Typed.Length, "names stop at 16 letters");
            s2.Type('\b');
            Assert.AreEqual(15, s2.Typed.Length, "backspace works");
            s2.Close(); // Esc / ✕: keep what's typed
            yield return Frames(2);
            Assert.AreEqual("Sir Reginald To", got);
        }

        [UnityTest]
        public IEnumerator HatchSceneCanBeSkipped()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(42), "fighter", "human");
            g.Warp(p, "egg_shrine", WorldDb.Zones["egg_shrine"].DefaultSpawn, false);
            yield return Frames(2);
            g.PacifyGroup("mine_egg", p);
            p.Hud.CloseAll();
            g.HatchEgg(p, Vector3.zero);
            var scene = (HatchCutscene)p.Hud.Top;
            scene.Skip();
            yield return Frames(2);
            Assert.IsFalse(p.Hud.Top is MessageScreen, "no skipping in the first second (a stray key press)");
            float t = Time.time;
            yield return WaitFor(() => Time.time - t > 1.1f);
            scene.Skip();
            yield return Frames(3);
            Assert.IsTrue(p.Hud.Top is MessageScreen, "skipped straight to the narration");
        }

        [Test]
        public void TownsfolkNoticeTheDragon()
        {
            foreach (var id in new[] { "pip", "brannoc", "elowen", "perrin" })
            {
                Assert.IsTrue(WorldDb.Npcs.ContainsKey(id), id);
                Assert.IsTrue(DialogueDb.DragonAsides.ContainsKey(id), $"{id} has a line about the dragon");
                StringAssert.Contains("{0}", DialogueDb.DragonAsides[id], "the aside uses the dragon's name");
            }
        }
    }
}
