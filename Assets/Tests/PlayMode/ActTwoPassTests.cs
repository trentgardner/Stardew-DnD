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
    /// <summary>Act II Phase 7: pacing, the End of Act II card, and the act end to end.</summary>
    public class ActTwoPassTests
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
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static FuzzInput Still(int seed) => new FuzzInput(seed) { ConfirmChance = 0, CancelChance = 0, MenuChance = 0, EndTurnChance = 0 };
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        const int Start = 6500, Level9 = 48000, Level10 = 64000;

        static void Band(int players)
        {
            int main = ActTwoXp.Available(false, players, false), mainWarden = ActTwoXp.Available(false, players, true), all = ActTwoXp.Available(true, players, false);
            Debug.Log($"Act II XP per player ({players}p), on top of {Start}: main path to the gate {main}, main + the Warden {mainWarden}, everything to the gate {all}");
            Assert.GreaterOrEqual(main, Level9 - Start, "the main path reaches level 9 by the gate");
            Assert.GreaterOrEqual(mainWarden, Level10 - Start, "the Warden's fall brings level 10");
            Assert.GreaterOrEqual(all, Level10 - Start, "a completionist is level 10 by the gate");
            Assert.LessOrEqual(all, (int)(Level10 * 1.3) - Start, "but not so far past it that the finale is a formality");
        }

        [Test] public void ActTwoXpBandHoldsForSolo() => Band(1);
        [Test] public void ActTwoXpBandHoldsForTwoPlayers() => Band(2);

        // ============================================================ Task 2: the End of Act II card

        static IEnumerator WardenDown(GameRoot g, PlayerSlot p, string side)
        {
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Factions.Commit(side, p);
            g.World.SetFlag("banner_" + side);
            g.Quests.Set(Answers.Id, Answers.Gate, p);
            yield return Frames(2);
            g.World.SetFlag(GameRoot.WardenFallen);
            g.Quests.Set(Answers.Id, Answers.Rest, p);
        }

        [UnityTest]
        public IEnumerator EndOfActTwoCardOnce()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(10), "fighter", "dwarf");
            var p2 = g.DebugStart(Still(11), "wizard", "elf");
            yield return Frames(2);
            yield return WardenDown(g, p1, "crown");
            p2.Hud.CloseAll();
            Assert.IsTrue(g.ShowActTwoEnd());
            yield return Frames(2);
            foreach (var p in new[] { p1, p2 })
            {
                var card = p.Hud.Top as ActEndScreen;
                Assert.IsNotNull(card, "both players see it");
                StringAssert.Contains("End of Act II", card.Text);
                StringAssert.Contains("Act III", card.Text);
            }
            Assert.IsFalse(g.ShowActTwoEnd(), "once per playthrough");
            p1.Hud.CloseAll(); p2.Hud.CloseAll();
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(240);
            Assert.IsFalse(g.Players[0].Hud.Top is ActEndScreen, "never again after a reload");
        }

        [UnityTest]
        public IEnumerator EndCardForOldSaves()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(12), "fighter", "dwarf");
            yield return Frames(2);
            yield return WardenDown(g, p, "fae");
            Assert.IsFalse(g.World.Flag(GameRoot.ActTwoEndSeen));
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(240);
            Assert.IsTrue(g.World.Flag(GameRoot.ActTwoEndSeen), "a save from after the Warden but before the card shows it once");
            Assert.IsTrue(g.Players[0].Hud.Top is ActEndScreen);
        }

        [UnityTest]
        public IEnumerator EndCardNamesYourSide([Values("crown", "academy", "fae", "hunters")] string side)
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(13), "fighter", "dwarf");
            yield return Frames(2);
            yield return WardenDown(g, p, side);
            g.ShowActTwoEnd();
            yield return Frames(2);
            StringAssert.Contains(ActEndScreen.SideLine(side), ((ActEndScreen)p.Hud.Top).Text);
        }

        // ============================================================ Task 4: the act end to end

        static SecretInteractable SpotNamed(string id) => Object.FindObjectsByType<SecretInteractable>(FindObjectsSortMode.None).Single(s => s.Def.Id == id);

        [UnityTest, Timeout(900000)]
        public IEnumerator ActTwoGoldenPath()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(20), "paladin", "human");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            var c = new DCtx { P = p };
            Assert.GreaterOrEqual(p.Character.XP, PlayerCharacter.XpThresholds[5], "Act II starts at level 5's XP");
            int start = p.Character.XP;

            // The road and the Academy: every fight on the Lantern Road talked down (the same XP as fighting).
            g.Warp(p, "lantern_road", new Vector2Int(2, 14), false);
            yield return Frames(3);
            foreach (var grp in WorldDb.Zones["lantern_road"].EnemyGroups.Where(e => e.QuestId == null)) g.PacifyGroup(grp.Id, p);
            g.Warp(p, "academy", new Vector2Int(27, 40), false);
            yield return Frames(3);
            g.Warp(p, "ashby_tower", new Vector2Int(8, 12), false);
            yield return Frames(3);
            DialogueDb.Get("ashby").Nodes["plan"].Options.Single(o => o.Text == "You could be right.").Do(c);
            Assert.AreEqual(Answers.Heard, g.Quests.Stage(Answers.Id));

            // Hear everyone out, do the Academy's errand, stand with the Academy, finish the Ninth Circle.
            DialogueDb.Get("marsh").Nodes["intro"].OnEnter(c);
            DialogueDb.Get("sorrel").Nodes["intro"].OnEnter(c);
            DialogueDb.Get("calloway").Nodes["intro"].OnEnter(c);
            Assert.IsTrue(Factions.HeardAll());
            DialogueDb.Get("perrin").Nodes["lamps"].OnEnter(c);
            g.Quests.Set("cold_lanterns", 3, p);
            g.World.SetFlag("met_ashby");
            var commit = DialogueDb.Get("ashby").Nodes["again"].Options.Single(o => o.Text.StartsWith("We'll help you finish"));
            Assert.IsTrue(commit.Show(c));
            commit.Do(c);
            yield return Frames(2);
            (p.Hud.Top as ChoiceScreen).Choose("Stand with");
            yield return Frames(2);
            Assert.AreEqual(2, g.World.BlightStage, "the ash thickens once you choose");
            g.Quests.Set("ninth_circle", 3, p);
            g.PacifyGroup("circle_bear", p);
            g.Quests.Set("ninth_circle", 5, p);
            Assert.AreEqual(Answers.Allies, g.Quests.Stage(Answers.Id));

            // The Emberhorn Road, to the gate.
            var warp = WorldDb.Zones["cragfoot"].Warps.Find(w => w.ToZone == "emberhorn_road");
            g.Warp(p, warp.ToZone, warp.ToPos, false);
            yield return Frames(3);
            foreach (var grp in WorldDb.Zones["emberhorn_road"].EnemyGroups) g.PacifyGroup(grp.Id, p);
            g.Warp(p, "emberhorn_road", new Vector2Int(55, 14), false);
            yield return Frames(3);
            SpotNamed("emberhorn_gate").Resolve(p, CheckOutcome.Success);
            p.Hud.CloseAll();
            Assert.AreEqual(Answers.Gate, g.Quests.Stage(Answers.Id));
            Debug.Log($"Golden path: XP at the gate {p.Character.XP} (started Act II at {start})");
            Assert.GreaterOrEqual(p.Character.XP, PlayerCharacter.XpThresholds[9], "level 9 by the gate on the main path");

            // The Throat and the Warden.
            foreach (var grp in WorldDb.Zones["emberhorn_throat"].EnemyGroups) g.PacifyGroup(grp.Id, p);
            g.Warp(p, "emberhorn_heart", new Vector2Int(2, 10), false);
            yield return Frames(3);
            p.Hud.CloseAll();
            DialogueDb.Get("warden_parley").Nodes["turn"].Options.First(o => o.Text.StartsWith("(Roll initiative")).Do(c);
            var input = p.Input;
            p.Input = new FuzzInput(7) { CancelChance = 0, MenuChance = 0 };
            yield return Frames(2);
            foreach (var fight in g.Combats.ToList()) foreach (var u in fight.Units) if (u.C.Team == Team.Enemies) u.C.HP = 0;
            Time.timeScale = 4;
            float t0 = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - t0 < 90f) yield return null;
            Time.timeScale = 1;
            p.Input = input;
            Assert.IsFalse(g.AnyInCombat);
            Assert.AreEqual(Answers.Rest, g.Quests.Stage(Answers.Id));
            Assert.GreaterOrEqual(p.Character.XP, PlayerCharacter.XpThresholds[10], "level 10 when the Warden falls");
            yield return new WaitForSecondsRealtime(3.5f);
            yield return Frames(2);
            Assert.IsTrue(g.World.Flag(GameRoot.ActTwoEndSeen), "the End of Act II card");
            Shot("act2_end_card");
            p.Hud.CloseAll();
            string path = SaveSystem.Save(false);
            g.LoadGame(path);
            yield return Frames(3);
            Assert.AreEqual(Answers.Rest, g.Quests.Stage(Answers.Id));
            Assert.AreEqual(2, g.World.BlightStage);
        }

        static void Shot(string name)
        {
            string dir = Path.Combine(Path.GetTempPath(), "embers_screenshots");
            Directory.CreateDirectory(dir);
            var rt = new RenderTexture(1280, 720, 24);
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            System.Array.Sort(cams, (a, b) => a.depth.CompareTo(b.depth));
            foreach (var cam in cams) { if (!cam.isActiveAndEnabled) continue; var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old; }
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator LevelTenIsTheCap()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(1), "fighter", "dwarf");
            yield return Frames(2);
            g.ApplyActTwoStart(p);
            p.Hud.CloseAll();
            Assert.AreEqual(10, PlayerCharacter.LevelCap);
            var pc = p.Character;
            pc.XP = 70000;
            int guard = 0;
            while (pc.CanLevelUp && guard++ < 20) pc.LevelUp(pc.AutoPlan(), new System.Collections.Generic.List<string>());
            Assert.AreEqual(10, pc.Level, "level 10 and no further in Act II");
            Assert.IsFalse(pc.CanLevelUp);
        }
    }
}
