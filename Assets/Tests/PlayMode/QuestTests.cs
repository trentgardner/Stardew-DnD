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
    }
}
