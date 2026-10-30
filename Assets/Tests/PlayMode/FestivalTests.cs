using System.Collections;
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
    /// <summary>Presses the dice tray's button whenever a roll is waiting, and nothing else (so the player never wanders off or talks to anyone).</summary>
    public class RollerInput : PlayerInput
    {
        public System.Func<bool> PressWhen = () => false;
        public override string DeviceName => "Roller";
        public override bool IsConnected => true;
        public override Vector2 Move => Vector2.zero;
        public override bool Confirm => PressWhen();
        public override bool Cancel => false;
        public override bool Pause => false;
        public override bool Inventory => false;
        public override bool CharacterSheet => false;
        public override bool Spells => false;
        public override bool Journal => false;
        public override bool EndTurn => false;
        public override bool NextTab => false;
        public override bool PrevTab => false;
        public override bool AltAction => false;
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

    /// <summary>The Harvest Festival (Phase 5): the calendar, the square, the four games and quest 14.</summary>
    public class FestivalTests
    {
        string _tempSaves;
        IRandom _rng;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tempSaves = Path.Combine(Path.GetTempPath(), "embers_test_saves_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.OverrideRoot = _tempSaves;
            AudioListener.volume = 0f;
            Sfx.Volume = 0f;
            _rng = Dice.Rng;
            if (GameRoot.I == null) new GameObject("GameRoot").AddComponent<GameRoot>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Dice.Rng = _rng;
            Time.timeScale = 1;
            if (GameRoot.I != null) GameRoot.I.QuitToTitle();
            yield return null;
            SaveSystem.OverrideRoot = null;
            if (Directory.Exists(_tempSaves)) Directory.Delete(_tempSaves, true);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static PlayerSlot Start(GameRoot g, string cls, string species = "human")
        {
            var input = new RollerInput();
            var p = g.DebugStart(input, cls, species);
            input.PressWhen = () => p.Hud != null && p.Hud.Dice.IsOpen;
            return p;
        }

        /// <summary>Sets the clock to just before `hour` on `day`, then lets an hour tick happen (as the game does).</summary>
        static void Tick(GameRoot g, int day, int hour)
        {
            g.Clock.Day = day;
            g.Clock.Minutes = hour * 60 - 1;
            g.Clock.Advance(1);
        }

        static FestivalGame Game(FestivalKind k) => Object.FindObjectsByType<FestivalGame>().Single(x => x.Kind == k);

        static IEnumerator Play(PlayerSlot p, FestivalKind k)
        {
            p.Hud.CloseAll();
            Game(k).Interact(p);
            Time.timeScale = 4;
            float start = Time.realtimeSinceStartup;
            yield return null;
            // the game rolls several times in a row; it's over when its flag is set and the tray has closed
            while ((!GameRoot.I.World.Flag(FestivalGame.FlagFor(k)) || p.Hud.Dice.IsOpen) && Time.realtimeSinceStartup - start < 30f) yield return null;
            Time.timeScale = 1;
            yield return null;
        }

        [Test]
        public void FestivalDaysAndHours()
        {
            foreach (int d in new[] { 10, 17, 24 }) Assert.IsTrue(Festival.IsFestivalDay(d, 0), $"day {d}");
            foreach (int d in new[] { 9, 11, 16 }) Assert.IsFalse(Festival.IsFestivalDay(d, 0), $"day {d}");
            Assert.IsFalse(Festival.IsFestivalDay(17, 10), "once you've had your festival, it doesn't come back");
            var c = new GameClock { Day = 10, Minutes = 8 * 60 };
            Assert.IsFalse(Festival.IsOn(c, 0), "not before nine bells");
            c.Minutes = 9 * 60;
            Assert.IsTrue(Festival.IsOn(c, 0));
            c.Minutes = 22 * 60;
            Assert.IsFalse(Festival.IsOn(c, 0), "over at ten at night");
        }

        [UnityTest]
        public IEnumerator FestivalComesBackIfYouMissIt()
        {
            var g = GameRoot.I;
            var p = Start(g, "fighter");
            yield return Frames(2);
            Tick(g, 9, 22);
            g.Clock.Day = 16; // a week asleep, or away in the wilds: day 10 came and went
            g.Clock.SleepUntil(8);
            Assert.AreEqual(17, g.Clock.Day);
            Assert.IsFalse(g.World.IsLocked("festival"), "not open yet at eight");
            g.Clock.Advance(60);
            Assert.IsTrue(g.World.IsLocked("festival"), "a week later, the village tries again");
            g.Warp(p, "town", new Vector2Int(24, 17), false);
            yield return Frames(2);
            Assert.IsTrue(Game(FestivalKind.AppleBob).Available(p));
        }

        [UnityTest]
        public IEnumerator FestivalFillsTheSquare()
        {
            var g = GameRoot.I;
            var p = Start(g, "fighter");
            yield return Frames(2);
            Tick(g, 9, 14);
            Assert.IsFalse(Game(FestivalKind.Archery).Available(p), "no games on an ordinary day");
            Tick(g, 10, 9);
            Assert.IsTrue(g.World.IsLocked("festival"));
            Assert.AreEqual(1, g.Quests.Stage("harvest_festival"), "Pip's been telling everyone about it");
            var e = WorldDb.Current(WorldDb.Npcs["pip"], 14, g.Clock.WeekdayIndex, g.World);
            Assert.AreEqual("festival", e.RequiresLock, "Pip is at the festival, not behind the bar");
            Assert.AreEqual("town", e.Zone);
            foreach (var n in WorldDb.Npcs.Values.Where(n => n.Schedule.Any(x => x.RequiresLock == "festival")))
                Assert.AreEqual("festival", WorldDb.Current(n, 14, g.Clock.WeekdayIndex, g.World).RequiresLock, n.Id);
            g.Warp(p, "town", new Vector2Int(24, 17), false);
            yield return Frames(2);
            var props = Object.FindObjectsByType<FestivalDressing>();
            Assert.Greater(props.Length, 0);
            Assert.IsTrue(props.All(x => x.GetComponent<SpriteRenderer>().enabled), "bunting and pie tables are out");
            foreach (FestivalKind k in System.Enum.GetValues(typeof(FestivalKind))) Assert.IsTrue(Game(k).Available(p), k.ToString());
            Assert.AreEqual("music_festival", g.WantedMusic());
            var town = g.Zones["town"];
            foreach (var kv in town.Def.FestivalProps) Assert.IsTrue(town.IsWalkable(kv.Item1), $"festival prop at {kv.Item1} stands on open ground and doesn't block it");
        }

        [UnityTest]
        public IEnumerator GamesOncePerPlayerAndQuestCompletesAtThree()
        {
            var g = GameRoot.I;
            var p = Start(g, "bard");
            yield return Frames(2);
            Tick(g, 10, 10);
            g.Warp(p, "town", new Vector2Int(24, 17), false);
            yield return Frames(2);
            Dice.Rng = new ScriptedRandom(Enumerable.Repeat(15, 200).ToArray());
            int xp = p.Character.XP;

            yield return Play(p, FestivalKind.AppleBob);
            Assert.IsTrue(g.World.Flag("fest_bob"));
            Assert.AreEqual(1, p.Character.CountOf("festival_ribbon"), "15s on the dice: two apples or more");
            yield return Play(p, FestivalKind.BardStage);
            Assert.IsTrue(g.World.Flag("fest_stage"));
            Assert.AreEqual(1, g.Quests.Stage("harvest_festival"));

            p.Hud.CloseAll();
            var d = DialogueDb.Get("pie_judging");
            var c = new DCtx { P = p };
            var opt = d.Nodes[d.Start(c)].Options.First(o => o.Check == Skill.Persuasion);
            c.LastOutcome = CheckOutcome.Success;
            opt.OnSuccess(c);
            Assert.IsTrue(g.World.Flag("fest_pie"));
            Assert.IsTrue(g.Quests.Done("harvest_festival"), "three games: the festival quest is done");
            Assert.AreEqual(150, p.Character.XP - xp, "quest XP, paid once (the games themselves pay none)");
            Assert.AreEqual(10, g.World.FestivalDoneDay);

            yield return Play(p, FestivalKind.Archery);
            Assert.IsTrue(g.World.Flag("fest_archery"), "a fourth game still plays");
            Assert.AreEqual(150, p.Character.XP - xp);

            p.Hud.CloseAll();
            Game(FestivalKind.AppleBob).Interact(p);
            yield return null;
            Assert.IsFalse(p.Hud.Dice.IsOpen, "one go per festival");
        }

        [UnityTest]
        public IEnumerator ArcheryUsesAttackRolls()
        {
            var g = GameRoot.I;
            var p = Start(g, "fighter", "elf");
            yield return Frames(2);
            var pc = p.Character;
            var bow = ItemsDb.Get<WeaponDef>("shortbow");
            Assert.AreEqual(pc.Mod(Ability.DEX) + (pc.IsProficientWith(bow) ? pc.ProficiencyBonus : 0), FestivalGame.ArcheryBonus(pc));
            var wiz = CharacterFactory.Create(CharacterFactory.DefaultChoices("wizard", "human", "W"));
            Assert.IsTrue(wiz.IsProficientWith(bow), "everyone is proficient with simple weapons in this engine");
            Assert.AreEqual(wiz.Mod(Ability.DEX) + wiz.ProficiencyBonus, FestivalGame.ArcheryBonus(wiz));
            StringAssert.Contains(AbilityUtil.Fmt(FestivalGame.ArcheryBonus(pc)), FestivalGame.ArcherySubtitle(pc), "the tray shows the bonus");
        }
    }
}
