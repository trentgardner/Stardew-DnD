using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Game;
using DnD.Rules;
using DnD.Rules.Content;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DnD.Tests
{
    /// <summary>The data-driven quest system (Phase 4): definitions, triggers, once-only rewards, and the quests themselves.</summary>
    public class QuestTests
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

        // ============================================================ Task 1: the quest system

        [UnityTest]
        public IEnumerator EveryQuestIsWellFormed()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(1), "fighter", "human");
            yield return null;
            var groups = WorldDb.Zones.Values.SelectMany(z => z.EnemyGroups).Select(x => x.Id).ToHashSet();
            Assert.GreaterOrEqual(QuestDb.All.Count, 4);
            foreach (var q in QuestDb.All.Values)
            {
                Assert.GreaterOrEqual(q.StageText.Length, 2, q.Id);
                Assert.AreEqual("", q.StageText[0], $"{q.Id}: stage 0 means 'not started' and has no text");
                Assert.IsTrue(q.Giver == null || WorldDb.Npcs.ContainsKey(q.Giver), $"{q.Id}: unknown giver {q.Giver}");
                foreach (var t in q.Triggers)
                {
                    Assert.Less(t.FromStage, t.ToStage, $"{q.Id} trigger {t.On} {t.Id}");
                    Assert.LessOrEqual(t.ToStage, q.Last, $"{q.Id} trigger {t.On} {t.Id}");
                    if (t.On == QuestEvent.GroupResolved) Assert.IsTrue(groups.Contains(t.Id), $"{q.Id}: unknown group {t.Id}");
                    if (t.On == QuestEvent.ItemGained) Assert.IsNotNull(ItemsDb.Get(t.Id), $"{q.Id}: unknown item {t.Id}");
                    if (t.On == QuestEvent.ZoneEntered) Assert.IsTrue(WorldDb.Zones.ContainsKey(t.Id), $"{q.Id}: unknown zone {t.Id}");
                }
                if (q.Reward?.Item != null) Assert.IsNotNull(ItemsDb.Get(q.Reward.Item), $"{q.Id}: unknown reward item");
                if (q.Reward?.Npc != null) Assert.IsTrue(WorldDb.Npcs.ContainsKey(q.Reward.Npc), $"{q.Id}: unknown reward npc");
            }
        }

        [UnityTest]
        public IEnumerator TriggersAdvanceQuests()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(2), "fighter", "human");
            yield return null;
            g.Quests.Notify(QuestEvent.GroupResolved, "ford_toll");
            Assert.AreEqual(0, g.Quests.Stage("east_road"), "a trigger whose FromStage doesn't match does nothing");
            g.Quests.Notify(QuestEvent.ZoneEntered, "ford");
            Assert.AreEqual(1, g.Quests.Stage("east_road"));
            g.Quests.Notify(QuestEvent.GroupResolved, "ford_toll");
            Assert.AreEqual(2, g.Quests.Stage("east_road"));
            g.Quests.Notify(QuestEvent.ItemGained, "lucky_spoon");
            Assert.AreEqual(1, g.Quests.Stage("spoon"));
        }

        [UnityTest]
        public IEnumerator QuestRewardPaysEachPlayerOnce()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(Still(3), "fighter", "human");
            var p2 = g.DebugStart(Still(4), "cleric", "elf");
            yield return null;
            int x1 = p1.Character.XP, x2 = p2.Character.XP;
            g.Quests.Start("spoon");
            g.Quests.Set("spoon", 2, p1);
            g.Quests.Set("spoon", 2, p2);
            g.Quests.Notify(QuestEvent.ItemGained, "lucky_spoon");
            Assert.AreEqual(75, p1.Character.XP - x1, "P1 gets the quest XP exactly once");
            Assert.AreEqual(75, p2.Character.XP - x2, "P2 gets the quest XP exactly once");
            Assert.IsTrue(g.Quests.Done("spoon"));
        }

        [UnityTest]
        public IEnumerator OldSaveQuestsKeepStagesAndDontRepay()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(5), "fighter", "human");
            yield return null;
            g.Quests.Stages["kettle"] = 2; // what an old save holds: a finished quest, no record of payment
            string path = SaveSystem.Save(false);
            g.QuitToTitle();
            yield return null;
            g.LoadGame(path);
            yield return null;
            p = g.Players[0];
            Assert.AreEqual(2, g.Quests.Stage("kettle"));
            Assert.IsTrue(g.Quests.Done("kettle"));
            Assert.IsTrue(g.Quests.Paid.Contains("kettle"), "finished quests from old saves count as paid");
            int xp = p.Character.XP;
            g.Quests.Set("kettle", 2, p);
            Assert.AreEqual(xp, p.Character.XP, "no second payout");
            StringAssert.StartsWith("Complete", QuestDb.Get("kettle").StageText[g.Quests.Stage("kettle")]);
        }

        // ============================================================ Task 2: quest spots, gated groups, quest items, toasts

        [UnityTest]
        public IEnumerator QuestSpotOnlyAppearsAtItsStage()
        {
            var town = WorldDb.Zones["town"];
            var at = new Vector2Int(25, 19); // open cobbles on the square (not a q tile: quest spots can sit anywhere walkable)
            var spot = new SecretDef { Id = "test_spot", Sprite = "obj_crate", Prompt = "Test spot", Skill = Skill.Perception, DC = 10,
                Success = "ok", Failure = "no", AlreadyFound = "done", QuestId = "spoon", QuestStage = 1, AdvanceTo = 2, SetsFlag = "test_spot_flag" };
            town.Secrets[at] = spot;
            try
            {
                var g = GameRoot.I;
                var p = g.DebugStart(Still(6), "fighter", "human");
                yield return null;
                var si = Object.FindObjectsByType<SecretInteractable>().Single(x => x.Def == spot);
                Assert.IsFalse(si.Available(p), "hidden before the quest reaches its stage");
                g.Quests.Start("spoon");
                yield return null;
                Assert.IsTrue(si.Available(p), "present at its stage");
                si.Resolve(p, CheckOutcome.Success);
                Assert.AreEqual(2, g.Quests.Stage("spoon"), "success advances the quest");
                Assert.IsTrue(g.World.Flag("test_spot_flag"));
                yield return null;
                Assert.IsFalse(si.Available(p), "gone once the quest moves past its stage");
            }
            finally { town.Secrets.Remove(at); }
        }

        [UnityTest]
        public IEnumerator QuestGatedGroupSpawnsOnlyInStageRange()
        {
            var forest = WorldDb.Zones["forest"];
            var grp = new EnemyGroupDef { Id = "test_gated", Unique = true, QuestId = "spoon", MinStage = 1, MaxStage = 1 };
            grp.Members.Add(new EnemySpawn("wolf", 6, 21)); // far from the player: nobody gets spotted
            forest.EnemyGroups.Add(grp);
            try
            {
                var g = GameRoot.I;
                var p = g.DebugStart(Still(7), "fighter", "human");
                g.Warp(p, "forest", new Vector2Int(43, 18), false);
                yield return null;
                System.Func<int> count = () => Object.FindObjectsByType<EnemyAgent>().Count(e => e.Group == grp);
                Assert.AreEqual(0, count(), "not before stage 1");
                g.Quests.Start("spoon");
                yield return null;
                Assert.AreEqual(1, count(), "spawns at stage 1");
                g.Quests.Set("spoon", 2);
                yield return null;
                Assert.AreEqual(0, count(), "leaves after stage 1");
            }
            finally { forest.EnemyGroups.Remove(grp); }
        }

        [UnityTest]
        public IEnumerator QuestItemsCantBeSoldOrDropped()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(8), "fighter", "human");
            yield return null;
            foreach (var id in new[] { "copper_kettle", "lucky_spoon" })
            {
                Assert.IsTrue(ItemsDb.Get(id).QuestItem, id);
                Assert.AreEqual(0, ItemsDb.Get(id).PriceCp, $"{id}: shops only buy priced items");
            }
            g.World.Storage.Add("copper_kettle", 1);
            Assert.IsTrue(g.HasQuestItem(p, "copper_kettle"), "a quest item left in home storage still counts");
            Assert.IsTrue(g.TakeQuestItem(p, "copper_kettle"));
            Assert.AreEqual(0, g.World.Storage.Count("copper_kettle"));
            Assert.IsFalse(g.TakeQuestItem(p, "copper_kettle"));
        }

        [UnityTest]
        public IEnumerator DuplicateToastsMerge()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(9), "fighter", "human");
            yield return null;
            int before = p.Hud.ToastTexts.Count;
            for (int i = 0; i < 3; i++) p.Hud.Toast("You are already at full health.");
            var mine = p.Hud.ToastTexts.Where(t => t.StartsWith("You are already at full health.")).ToList();
            Assert.AreEqual(1, mine.Count, "one toast, not three");
            StringAssert.EndsWith("×3", mine[0]);
        }

        // ============================================================ Task 4: South Lane and the weekly rhythm

        static int Coinday => System.Array.IndexOf(WorldDb.Weekdays, "Coinday");
        static int Hearthday => System.Array.IndexOf(WorldDb.Weekdays, "Hearthday");

        [Test]
        public void EveryNpcHasAWeeklyVariation()
        {
            foreach (var npc in WorldDb.Npcs.Values)
                Assert.IsTrue(npc.Schedule.Any(e => e.OnlyWeekday >= 0), $"{npc.Id} does the same thing every day of the week");
        }

        [Test]
        public void MarketDayFillsTheSquare()
        {
            var inSquare = WorldDb.Npcs.Values.Select(n => WorldDb.Current(n, 11, Coinday))
                .Count(e => e != null && e.Zone == "town" && e.Pos.x >= 18 && e.Pos.x <= 32 && e.Pos.y >= 13 && e.Pos.y <= 23);
            Assert.GreaterOrEqual(inSquare, 3, "Coinday at 11:00: market stalls are busy");
        }

        [Test]
        public void HearthdayServiceAtTheTemple()
        {
            Assert.AreEqual("temple", WorldDb.Current(WorldDb.Npcs["elowen"], 10, Hearthday).Zone);
            int there = WorldDb.Npcs.Values.Count(n => WorldDb.Current(n, 10, Hearthday)?.Zone == "temple");
            Assert.GreaterOrEqual(there, 4, "Elowen and at least three villagers at the Hearthday service");
        }

        [UnityTest]
        public IEnumerator ForgeSellsWeapons()
        {
            var g = GameRoot.I;
            g.DebugStart(Still(10), "fighter", "human");
            yield return null;
            var stock = ShopDb.Stock("forge");
            CollectionAssert.Contains(stock, "longsword");
            CollectionAssert.Contains(stock, "chain_mail");
            CollectionAssert.Contains(stock, "shield");
            Assert.IsTrue(stock.All(id => ItemsDb.Get(id) != null && ItemsDb.Get(id).PriceCp > 0), "everything at the forge is a real, priced item");
            Assert.AreEqual("Garrick", ShopDb.Owner("forge"));
            Assert.IsTrue(g.Zones.ContainsKey("forge") && g.Zones.ContainsKey("watchhouse") && g.Zones.ContainsKey("cottage_lark"));
        }

        /// <summary>User request (2026-10-29): every building has a readable sign saying what it is.</summary>
        [UnityTest]
        public IEnumerator EveryBuildingDoorHasASign()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(11), "fighter", "human");
            yield return null;
            var signs = Object.FindObjectsByType<SignInteractable>();
            foreach (var z in g.Zones.Values.Where(z => !z.Def.Indoors))
                for (int y = 0; y < z.Def.Height; y++)
                    for (int x = 0; x < z.Def.Width; x++)
                    {
                        var door = new Vector2Int(x, y);
                        if (z.CharAt(door) != 'D') continue;
                        Assert.IsTrue(z.Def.DoorSigns.TryGetValue(door, out var sign), $"{z.Def.Id}: the door at {door} has no building sign");
                        Assert.IsTrue(signs.Any(si => si.Title == sign.Name && si.PromptText(p).Contains(sign.Name)), $"{z.Def.Id}: sign '{sign.Name}' isn't in the world");
                    }
        }

        // ============================================================ Task 5: town quests

        /// <summary>Plays one dialogue option the way DialogueScreen does (checks resolve to the given outcome). Returns the next node id.</summary>
        static string Choose(PlayerSlot p, string dialogueId, string node, string optStart, bool success = true)
        {
            var d = DialogueDb.Get(dialogueId);
            var npc = Object.FindObjectsByType<NpcAgent>().FirstOrDefault(n => n.Def.Dialogue == dialogueId);
            var c = new DCtx { P = p, Npc = npc };
            Assert.IsTrue(d.Nodes.ContainsKey(node), $"{dialogueId} has no node '{node}'");
            var o = d.Nodes[node].Options.FirstOrDefault(x => x.Text.StartsWith(optStart));
            Assert.IsNotNull(o, $"{dialogueId}/{node}: no option starting '{optStart}'");
            Assert.IsTrue(o.Show(c), $"{dialogueId}/{node}: option '{optStart}' is hidden");
            string next;
            if (o.Check.HasValue)
            {
                c.LastOutcome = success ? CheckOutcome.Success : CheckOutcome.Failure;
                (success ? o.OnSuccess : o.OnFailure)?.Invoke(c);
                next = (success ? o.Success : o.Failure) ?? o.Next;
            }
            else { o.Do?.Invoke(c); next = o.Next; }
            if (next != null && d.Nodes.TryGetValue(next, out var n)) n.OnEnter?.Invoke(c);
            return next;
        }

        static string StartNode(PlayerSlot p, string dialogueId)
        {
            var npc = Object.FindObjectsByType<NpcAgent>().FirstOrDefault(n => n.Def.Dialogue == dialogueId);
            var c = new DCtx { P = p, Npc = npc };
            var d = DialogueDb.Get(dialogueId);
            var id = d.Start(c);
            d.Nodes[id].OnEnter?.Invoke(c);
            return id;
        }

        [UnityTest]
        public IEnumerator BadBloodMediationAndSidingBothComplete()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(20), "bard", "human");
            yield return null;
            Choose(p, "rhoswen", StartNode(p, "rhoswen"), "Anything need doing");
            Assert.AreEqual(1, g.Quests.Stage("bad_blood"), "Captain Vale hands out the feud");
            Choose(p, "hesper", StartNode(p, "hesper"), "About the fence");
            Assert.AreEqual(1, g.Quests.Stage("bad_blood"), "one side of the story isn't enough");
            Choose(p, "dunstan", StartNode(p, "dunstan"), "About the fence");
            Assert.AreEqual(2, g.Quests.Stage("bad_blood"), "heard both sides");
            int xp = p.Character.XP, h = g.World.Friendship.GetValueOrDefault("hesper"), d = g.World.Friendship.GetValueOrDefault("dunstan");
            Choose(p, "hesper", StartNode(p, "hesper"), "(Mediate)");
            Assert.IsTrue(g.Quests.Done("bad_blood"));
            Assert.AreEqual(100, p.Character.XP - xp);
            Assert.Greater(g.World.Friendship["hesper"], h, "mediation makes both happier");
            Assert.Greater(g.World.Friendship["dunstan"], d);

            // Second playthrough of the same quest: siding with one neighbor also completes it.
            g.QuitToTitle();
            yield return null;
            p = g.DebugStart(Still(21), "fighter", "human");
            yield return null;
            g.Quests.Set("bad_blood", 2);
            Choose(p, "dunstan", StartNode(p, "dunstan"), "(Side with Dunstan)");
            Assert.IsTrue(g.Quests.Done("bad_blood"));
            Assert.Less(g.World.Friendship.GetValueOrDefault("hesper"), 0, "the other side is hurt");
        }

        [UnityTest]
        public IEnumerator PondRingCanBeFoundAndReturned()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(22), "rogue", "human");
            yield return null;
            Choose(p, "tamsin", StartNode(p, "tamsin"), "You look worried");
            Assert.AreEqual(1, g.Quests.Stage("pond_ring"));
            var spots = Object.FindObjectsByType<SecretInteractable>().Where(s => s.Def.QuestId == "pond_ring").ToList();
            Assert.AreEqual(2, spots.Count, "dive (Athletics) or fish (Survival)");
            Assert.IsTrue(spots.Any(s => s.Def.Skill == Skill.Athletics) && spots.Any(s => s.Def.Skill == Skill.Survival));
            spots.First().Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(2, g.Quests.Stage("pond_ring"));
            Assert.AreEqual(1, p.Character.CountOf("pond_ring"));
            Assert.IsTrue(ItemsDb.Get("pond_ring").QuestItem);
            int xp = p.Character.XP;
            Choose(p, "tamsin", StartNode(p, "tamsin"), "I found your ring");
            Assert.IsTrue(g.Quests.Done("pond_ring"));
            Assert.AreEqual(0, p.Character.CountOf("pond_ring"));
            Assert.AreEqual(100, p.Character.XP - xp);
            Assert.IsTrue(g.World.Flag("lark_tamsin_engaged"));
        }

        [UnityTest]
        public IEnumerator SecretPoemsRevealAddsTavernSchedule()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(23), "bard", "human");
            yield return null;
            Assert.AreNotEqual("tavern", WorldDb.Current(WorldDb.Npcs["elowen"], 20, WorldDb.Emberday, g.World)?.Zone);
            g.World.AddFriendship("brannoc", 200);
            Choose(p, "brannoc", "shop", "How's the poetry");
            Assert.AreEqual(1, g.Quests.Stage("secret_poems"));
            Assert.AreEqual(1, p.Character.CountOf("brannoc_poem"));
            Choose(p, "elowen", StartNode(p, "elowen"), "Someone asked me to give you this");
            Choose(p, "elowen", "poem_read", "It's from Brannoc");
            Assert.IsTrue(g.Quests.Done("secret_poems"));
            Assert.AreEqual("tavern", WorldDb.Current(WorldDb.Npcs["elowen"], 20, WorldDb.Emberday, g.World)?.Zone, "Elowen joins Brannoc at story night");
            Assert.AreEqual("tavern", WorldDb.Current(WorldDb.Npcs["brannoc"], 20, WorldDb.Emberday, g.World)?.Zone);
        }

        [UnityTest]
        public IEnumerator PipsBarrelMercyGivesWickAJob()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(24), "cleric", "dwarf");
            yield return null;
            Assert.IsNull(WorldDb.Current(WorldDb.Npcs["wick"], 12, 0, g.World)?.Zone, "Wick isn't around before the quest");
            Choose(p, "pip", StartNode(p, "pip"), "You look upset");
            Assert.AreEqual(1, g.Quests.Stage("pips_barrel"));
            var spot = Object.FindObjectsByType<SecretInteractable>().Single(s => s.Def.QuestId == "pips_barrel");
            spot.Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(2, g.Quests.Stage("pips_barrel"));
            Assert.AreEqual("town", WorldDb.Current(WorldDb.Npcs["wick"], 12, 0, g.World)?.Zone, "the tracks lead to Wick in the orchard");
            Choose(p, "wick", StartNode(p, "wick"), "Why'd you take it");
            Choose(p, "wick", "why", "Come with me");
            Assert.AreEqual(3, g.Quests.Stage("pips_barrel"));
            int xp = p.Character.XP;
            Choose(p, "pip", StartNode(p, "pip"), "(Persuade) Give Wick a job");
            Assert.IsTrue(g.Quests.Done("pips_barrel"));
            Assert.AreEqual(100, p.Character.XP - xp);
            Assert.IsTrue(g.World.Flag("wick_hired"));
            Assert.AreEqual("tavern", WorldDb.Current(WorldDb.Npcs["wick"], 12, 0, g.World)?.Zone, "Wick works at the Drake now");
        }

        // ============================================================ Task 6: wilds quests

        static SecretInteractable Spot(string id) => Object.FindObjectsByType<SecretInteractable>().Single(s => s.Def.Id == id);

        [UnityTest]
        public IEnumerator GoatsComeHomeAndTobinPays()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(30), "druid", "elf");
            yield return null;
            Choose(p, "tobin", StartNode(p, "tobin"), "Need a hand with anything");
            Assert.AreEqual(1, g.Quests.Stage("goats"));
            foreach (var id in new[] { "goat_1", "goat_2", "goat_3" })
            {
                Assert.IsTrue(Spot(id).Available(p), id);
                Spot(id).Resolve(p, CheckOutcome.Success);
                Assert.IsFalse(Spot(id).Available(p), $"{id} trots home once calmed");
            }
            Assert.AreEqual(2, g.Quests.Stage("goats"), "all three goats home");
            int xp = p.Character.XP;
            Choose(p, "tobin", StartNode(p, "tobin"), "Your goats are home");
            Assert.IsTrue(g.Quests.Done("goats"));
            Assert.AreEqual(75, p.Character.XP - xp);
            Assert.AreEqual(1, p.Character.CountOf("goat_bell"));
        }

        [UnityTest]
        public IEnumerator HauntedMillPeacefulAndFightPaySameXp()
        {
            var g = GameRoot.I;
            int owlbear = MonstersDb.Get("owlbear").XP;
            Assert.AreEqual(700, owlbear, "SRD owlbear: CR 3, 700 XP");
            int[] gained = new int[2];
            for (int run = 0; run < 2; run++)
            {
                // The fight run needs a player who acts on their turns (confirms every prompt), like TalkingAndFightingPayTheSameXp.
                var p = g.DebugStart(run == 0 ? Still(31) : new FuzzInput(4) { ConfirmChance = 1, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 }, "fighter", "human");
                yield return null;
                g.Warp(p, "forest", new Vector2Int(44, 18), false); // discover Whisperwood first, so its +15 XP isn't counted below
                yield return null;
                p.Hud.CloseAll();
                g.Quests.Stages["east_road"] = 3;
                g.Clock.Minutes = 21 * 60; // the "ghost" only wails at night
                Choose(p, "hobb", StartNode(p, "hobb"), "Anything else bothering you");
                Assert.AreEqual(1, g.Quests.Stage("haunted_mill"));
                Assert.IsTrue(Spot("mill_noises").Available(p), "the noises are there at night");
                int before = p.Character.XP;
                Spot("mill_noises").Resolve(p, CheckOutcome.Success);
                Assert.AreEqual(2, g.Quests.Stage("haunted_mill"));
                if (run == 0)
                {
                    Spot("owlbear_cub").Resolve(p, CheckOutcome.Success);
                    Assert.AreEqual(3, g.Quests.Stage("haunted_mill"));
                    g.Warp(p, "forest", new Vector2Int(44, 18), false);
                    yield return null;
                    Spot("owlbear_den").Resolve(p, CheckOutcome.Success);
                }
                else
                {
                    g.Warp(p, "forest", new Vector2Int(44, 18), false);
                    yield return null;
                    p.Hud.CloseAll();
                    g.StartGroupCombat("forest_owlbear", p);
                    foreach (var u in g.Combats.Single().Units) if (u.C.Team == Team.Enemies) u.C.HP = 0;
                    Time.timeScale = 4;
                    float start = Time.realtimeSinceStartup;
                    while (g.AnyInCombat && Time.realtimeSinceStartup - start < 60f) yield return null;
                    Time.timeScale = 1;
                    Assert.IsFalse(g.AnyInCombat, "the fight should end once the owlbear is down");
                }
                Assert.IsTrue(g.Quests.Done("haunted_mill"), $"run {run}: the mill is quiet (stage {g.Quests.Stage("haunted_mill")}, defeated {g.World.DefeatedGroups.Contains("forest_owlbear")})");
                gained[run] = p.Character.XP - before;
                g.QuitToTitle();
                yield return null;
            }
            Assert.AreEqual(owlbear + 150, gained[0], "returning the cub pays the owlbear's XP plus the quest");
            Assert.AreEqual(gained[0], gained[1], "peaceful and fighting pay the same");
        }

        [UnityTest]
        public IEnumerator WolfBountyCompletesFromAnyResolution()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(33), "ranger", "human");
            yield return null;
            Choose(p, "rhoswen", StartNode(p, "rhoswen"), "Anything need doing");
            Assert.AreEqual(1, g.Quests.Stage("wolf_bounty"), "the notice-board bounty comes with the odd jobs");
            g.Warp(p, "forest", new Vector2Int(44, 18), false);
            yield return null;
            g.PacifyGroup("forest_wolves", p);
            Assert.AreEqual(2, g.Quests.Stage("wolf_bounty"), "talking the wolves down counts");
            g.Warp(p, "town", new Vector2Int(25, 19), false);
            yield return null;
            int xp = p.Character.XP; // (after the town-discovery XP)
            Choose(p, "rhoswen", StartNode(p, "rhoswen"), "The wolves are dealt with");
            Assert.IsTrue(g.Quests.Done("wolf_bounty"));
            Assert.AreEqual(50, p.Character.XP - xp);
        }

        [UnityTest]
        public IEnumerator LostChildResolvesEvenIfYouSleep()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(34), "cleric", "human");
            yield return null;
            g.Clock.Day = 2; g.Clock.Minutes = 15 * 60; // Tideday, but too early in the game
            g.StoryTick();
            Assert.AreEqual(0, g.Quests.Stage("lost_child"), "not in the first days");
            g.Clock.Day = 9; g.Clock.Minutes = 15 * 60; // the second Tideday
            g.StoryTick();
            Assert.AreEqual(1, g.Quests.Stage("lost_child"), "Maddie goes missing on a Tideday afternoon");
            Assert.AreEqual("forest", WorldDb.Current(WorldDb.Npcs["maddie"], 17, g.Clock.WeekdayIndex, g.World)?.Zone);
            int xp = p.Character.XP;
            g.Clock.Day = 10; g.Clock.Minutes = 7 * 60; // slept through it
            g.StoryTick();
            Assert.IsTrue(g.Quests.Done("lost_child"), "the watch found her: never a soft-lock");
            Assert.AreEqual(50, p.Character.XP - xp, "smaller reward when the watch finds her");
            Assert.AreEqual("town", WorldDb.Current(WorldDb.Npcs["maddie"], 15, g.Clock.WeekdayIndex, g.World)?.Zone, "Maddie is home safe");

            // In time: tracks, then Maddie herself.
            g.QuitToTitle();
            yield return null;
            p = g.DebugStart(Still(35), "ranger", "elf");
            yield return null;
            g.Clock.Day = 9; g.Clock.Minutes = 15 * 60;
            g.StoryTick();
            Spot("lost_tracks_1").Resolve(p, CheckOutcome.Success);
            Spot("lost_tracks_2").Resolve(p, CheckOutcome.Success);
            Assert.AreEqual(3, g.Quests.Stage("lost_child"));
            xp = p.Character.XP;
            Choose(p, "maddie", StartNode(p, "maddie"), "Maddie! Everyone's looking");
            Assert.IsTrue(g.Quests.Done("lost_child"));
            Assert.AreEqual(150, p.Character.XP - xp);
        }

        [UnityTest]
        public IEnumerator HerbsFromThreeZonesComplete()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(36), "druid", "human");
            yield return null;
            Choose(p, "elowen", StartNode(p, "elowen"), "Can I help with anything");
            Assert.AreEqual(1, g.Quests.Stage("elowens_herbs"));
            var zones = new System.Collections.Generic.HashSet<string>();
            foreach (var z in g.Zones.Values)
                foreach (var kv in z.Def.Secrets)
                    if (kv.Value.QuestId == "elowens_herbs") zones.Add(z.Def.Id);
            CollectionAssert.AreEquivalent(new[] { "forest", "ford", "town" }, zones, "one herb in each of three places");
            foreach (var id in new[] { "herb_moonpetal", "herb_riverleaf", "herb_hearthroot" })
            {
                Assert.AreEqual(1, g.Quests.Stage("elowens_herbs"));
                Spot("spot_" + id).Resolve(p, CheckOutcome.Success);
                Assert.AreEqual(1, p.Character.CountOf(id), id);
            }
            Assert.AreEqual(2, g.Quests.Stage("elowens_herbs"));
            int xp = p.Character.XP;
            Choose(p, "elowen", StartNode(p, "elowen"), "I've brought your herbs");
            Assert.IsTrue(g.Quests.Done("elowens_herbs"));
            Assert.AreEqual(100, p.Character.XP - xp);
            Assert.AreEqual(0, p.Character.CountOf("herb_moonpetal"));
        }

        [UnityTest]
        public IEnumerator FaeTricksterPaysXpEitherWay()
        {
            var g = GameRoot.I;
            for (int run = 0; run < 2; run++)
            {
                var p = g.DebugStart(Still(37 + run), "wizard", "gnome");
                yield return null;
                g.Clock.Minutes = 19 * 60;
                Assert.AreEqual("forest", WorldDb.Current(WorldDb.Npcs["thimble"], 19, 0, g.World)?.Zone, "Thimble is at the pond in the evening");
                int xp = p.Character.XP;
                string node = Choose(p, "thimble", StartNode(p, "thimble"), "A riddle game");
                bool right = run == 1;
                node = Choose(p, "thimble", node, right ? "A river" : "A dragon");
                node = Choose(p, "thimble", node, right ? "Footsteps" : "Cake");
                Choose(p, "thimble", node, right ? "Tomorrow" : "Winter");
                Assert.IsTrue(g.Quests.Done("fae_trickster"));
                Assert.AreEqual(100, p.Character.XP - xp, "the XP comes either way");
                Assert.AreEqual(right ? 1 : 0, p.Character.CountOf("fae_trinket"), right ? "two or more right: the trinket" : "all wrong: just a laugh");
                g.QuitToTitle();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryQuestCanStart()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(39), "bard", "human");
            yield return null;
            // Each Phase 4 quest has a starting point a player can reach: a giver's option or a world trigger.
            g.Quests.Stages["east_road"] = 3;
            g.World.AddFriendship("brannoc", 200);
            Choose(p, "rhoswen", StartNode(p, "rhoswen"), "Anything need doing");
            Choose(p, "tamsin", StartNode(p, "tamsin"), "You look worried");
            Choose(p, "brannoc", "shop", "How's the poetry");
            Choose(p, "pip", StartNode(p, "pip"), "You look upset");
            Choose(p, "tobin", StartNode(p, "tobin"), "Need a hand with anything");
            Choose(p, "hobb", StartNode(p, "hobb"), "Anything else bothering you");
            Choose(p, "elowen", StartNode(p, "elowen"), "Can I help with anything");
            Choose(p, "thimble", StartNode(p, "thimble"), "A riddle game");
            g.Clock.Day = 9; g.Clock.Minutes = 15 * 60; g.StoryTick();
            foreach (var id in new[] { "bad_blood", "wolf_bounty", "pond_ring", "secret_poems", "pips_barrel", "goats", "haunted_mill", "elowens_herbs", "fae_trickster", "lost_child" })
                Assert.GreaterOrEqual(g.Quests.Stage(id), 1, id);
        }

        // ============================================================ Task 7: the bar brawl

        [UnityTest]
        public IEnumerator BarBrawlIsNonlethal()
        {
            var g = GameRoot.I;
            // A player who rolls their dice (initiative waits for the roll), like the other fight tests.
            var p = g.DebugStart(new FuzzInput(4) { ConfirmChance = 1, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 }, "cleric", "dwarf");
            yield return null;
            g.Clock.Day = 6; g.Clock.Minutes = 19 * 60; // Coinday evening: payday for the farmhands
            g.Warp(p, "tavern", new Vector2Int(8, 9), false);
            yield return null;
            p.Hud.CloseAll();
            string node = Choose(p, "pip", StartNode(p, "pip"), "Who are the rowdy lot");
            Choose(p, "pip", node, "(Roll up your sleeves)");
            Assert.AreEqual(1, g.Quests.Stage("bar_brawl"));
            yield return null;
            yield return null;
            var c = g.Combats.SingleOrDefault();
            Assert.IsNotNull(c, "the brawl starts");
            Assert.IsTrue(c.Brawl);
            var actions = c.ActionsFor(p);
            Assert.IsFalse(actions.Any(a => a.Group == ActionGroup.Spells), "NO MAGIC IN MY BAR");
            Assert.IsFalse(actions.Any(a => a.Group == ActionGroup.Items), "no potions in a fistfight");
            Assert.IsTrue(actions.Any(a => a.Name.StartsWith("Unarmed Strike")), "fists");
            Assert.IsTrue(actions.Any(a => a.Name.StartsWith("Tankard")), "and furniture");
            Assert.IsTrue(p.Character.Nonlethal);

            p.Character.TakeDamage(999, DamageType.Bludgeoning, false, new System.Collections.Generic.List<string>());
            Assert.IsFalse(p.Character.IsDead, "knocked out, not killed");
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            while (g.AnyInCombat && Time.realtimeSinceStartup - start < 30f) yield return null;
            Time.timeScale = 1;
            Assert.IsFalse(g.AnyInCombat, "the brawl ends when the party is knocked out");
            yield return null;
            Assert.AreEqual("tavern", p.Zone.Def.Id, "losing a brawl is not a party wipe (no trip to the temple)");
            Assert.AreEqual(1, p.Character.HP, "you wake up at 1 HP");
            Assert.IsFalse(p.Character.Nonlethal, "back to normal rules after the brawl");
            Assert.IsTrue(g.Quests.Done("bar_brawl"), "you were magnificent anyway");
        }

        // ============================================================ Task 8: acceptance

        /// <summary>Button-mashing through town, the South Lane, Whisperwood and the ford with every quest active (spots, gated groups, Wick, Maddie, Thimble all present).</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator QuestWalkthroughFuzz()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(new FuzzInput(12), "rogue", "half_elf");
            yield return null;
            foreach (var q in QuestDb.All.Keys) g.Quests.Start(q);
            g.Quests.Set("haunted_mill", 2); g.Quests.Set("pips_barrel", 2);
            g.World.SetFlag("maddie_lost"); g.World.Lock("lost_child_deadline", 600);
            g.Clock.Minutes = 18 * 60 + 30; // evening: Thimble is out, the mill wails soon
            var stops = new[] { ("town", new Vector2Int(20, 43)), ("town", new Vector2Int(25, 19)), ("forest", new Vector2Int(40, 18)), ("ford", new Vector2Int(30, 15)),
                ("crater", WorldDb.Zones["crater"].DefaultSpawn), ("warrens", new Vector2Int(20, 23)), ("egg_shrine", new Vector2Int(12, 12)), ("watchtower", new Vector2Int(15, 20)),
                ("town", new Vector2Int(24, 17)) };
            foreach (var (zone, at) in stops)
            {
                if (zone == "town" && at == new Vector2Int(24, 17)) { g.Clock.Day = 10; g.Clock.Minutes = 14 * 60 - 1; g.Clock.Advance(1); } // festival day, mid-afternoon

                g.Warp(p, zone, at, false);
                Time.timeScale = 4;
                for (int i = 0; i < 2000; i++) yield return null;
                Time.timeScale = 1;
                Assert.IsTrue(g.InGame, $"still in the game after fuzzing {zone}");
                p.Hud.CloseAll();
                foreach (var c in g.Combats.ToList()) c.Finished = true;
                g.Combats.Clear();
            }
        }

        // ============================================================ final review fixes

        /// <summary>Closing the conversation after handing the poem over (before choosing reveal/keep) must not lose the poem.</summary>
        [UnityTest]
        public IEnumerator PoemSurvivesClosingTheConversationEarly()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(41), "bard", "human");
            yield return null;
            g.World.AddFriendship("brannoc", 200);
            Choose(p, "brannoc", "shop", "How's the poetry");
            Choose(p, "elowen", StartNode(p, "elowen"), "Someone asked me to give you this");
            // (the player presses Esc here)
            Assert.IsTrue(g.HasQuestItem(p, "brannoc_poem"), "the poem is only handed over once you choose what to say");
            Assert.AreEqual(1, g.Quests.Stage("secret_poems"));
            Choose(p, "elowen", "poem_read", "I promised not to say");
            Assert.IsFalse(g.HasQuestItem(p, "brannoc_poem"));
            Assert.IsTrue(g.Quests.Done("secret_poems"));
        }

        /// <summary>A co-op partner who walks into the bar brawl plays by brawl rules too (knocked out, never killed).</summary>
        [UnityTest]
        public IEnumerator LateJoinerToABrawlIsNonlethal()
        {
            var g = GameRoot.I;
            var p1 = g.DebugStart(new FuzzInput(4) { ConfirmChance = 1, CancelChance = 0, EndTurnChance = 0, MenuChance = 0 }, "fighter", "human");
            var p2 = g.DebugStart(Still(42), "wizard", "elf");
            yield return null;
            g.Clock.Day = 6; g.Clock.Minutes = 19 * 60;
            g.Warp(p1, "tavern", new Vector2Int(8, 9), false);
            yield return null;
            g.Quests.Start("bar_brawl");
            g.StartGroupCombat("drake_brawlers", p1);
            var c = g.Combats.Single();
            Assert.IsTrue(c.Brawl);
            c.JoinLate(p2);
            Assert.IsTrue(p2.Character.Nonlethal, "the late joiner is knocked out, not killed");
            Assert.IsFalse(c.ActionsFor(p2).Any(a => a.Group == ActionGroup.Spells), "and can't cast in the bar either");
        }

        /// <summary>With every quest active at once, the journal still shows all of its text (it shrinks to fit instead of clipping).</summary>
        [UnityTest]
        public IEnumerator JournalFitsEveryActiveQuest()
        {
            var g = GameRoot.I;
            var p = g.DebugStart(Still(43), "bard", "human");
            yield return null;
            foreach (var q in QuestDb.All.Keys) g.Quests.Start(q);
            p.Hud.CloseAll();
            p.Hud.Push(new JournalScreen());
            yield return null;
            yield return null;
            var label = Object.FindObjectsByType<UnityEngine.UI.Text>().Where(t => t.text.StartsWith("<b>Quests</b>")).Single();
            Assert.AreEqual(VerticalWrapMode.Truncate, label.verticalOverflow, "the journal never spills outside its window");
            var gen = label.cachedTextGenerator;
            Assert.Greater(gen.characterCount, 0);
            // (Unity counts one end-of-text marker that is never visible.)
            Assert.AreEqual(gen.characterCount - 1, gen.characterCountVisible, "every line of the journal is visible");
        }
    }
}
