using System;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// The Harvest Festival: day 10 in Thistledown's square, from nine bells to ten at night. If the party misses it,
    /// the village tries again every seven days until they've been. While it runs, the world lock "festival" is set.
    /// </summary>
    public static class Festival
    {
        public const int FirstDay = 10, Repeat = 7, OpenHour = 9, CloseHour = 22, AnnounceDay = 6;
        public const string Lock = "festival";

        public static bool IsFestivalDay(int day, int doneDay) =>
            day == FirstDay || (day > FirstDay && (day - FirstDay) % Repeat == 0 && (doneDay == 0 || day == doneDay));

        public static bool IsOn(GameClock c, int doneDay) => IsFestivalDay(c.Day, doneDay) && c.Hour >= OpenHour && c.Hour < CloseHour;
    }

    public enum FestivalKind { AppleBob, Archery, PieJudging, BardStage }

    /// <summary>Shown only while the festival runs (bunting, pie tables, the stage...). Never solid.</summary>
    public class FestivalDressing : MonoBehaviour
    {
        SpriteRenderer _sr;
        void Update()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            var g = GameRoot.I;
            if (_sr != null) _sr.enabled = g != null && g.InGame && g.World.IsLocked(Festival.Lock);
        }
    }

    /// <summary>One of the festival games. Every check goes through the dice tray; the quest pays the XP, not the games.</summary>
    public class FestivalGame : Interactable
    {
        public FestivalKind Kind;
        public override float Range => 1.6f;

        static readonly string[] Flags = { "fest_bob", "fest_archery", "fest_pie", "fest_stage" };
        public static string FlagFor(FestivalKind k) => Flags[(int)k];
        string PlayedKey(PlayerSlot p) => $"fest:{Kind}:{p.Index}";

        public override bool Available(PlayerSlot p) => GameRoot.I != null && GameRoot.I.World.IsLocked(Festival.Lock);

        public override string PromptText(PlayerSlot p)
        {
            switch (Kind)
            {
                case FestivalKind.AppleBob: return "Bob for apples";
                case FestivalKind.Archery: return "Try the archery butts";
                case FestivalKind.PieJudging: return "Judge the pie contest";
                default: return "Take the bard stage";
            }
        }

        public override void Interact(PlayerSlot p)
        {
            var g = GameRoot.I;
            if (p.IsBusy || !Available(p)) return;
            if (g.World.IsLocked(PlayedKey(p))) { p.Hud.Narrate("You've had your turn at this one. Come back next festival!", false); return; }
            g.World.Lock(PlayedKey(p), 24 * 60 - g.Clock.Minutes); // once per player per festival day
            switch (Kind)
            {
                case FestivalKind.AppleBob: AppleBob(p, 0, 0); break;
                case FestivalKind.Archery: Archery(p, 0, 0); break;
                case FestivalKind.PieJudging: p.Hud.Push(new DialogueScreen("pie_judging", null, null)); break;
                default: BardStage(p); break;
            }
        }

        // ------------------------------------------------------------ apple bobbing: three Constitution checks

        static readonly int[] AppleDCs = { 10, 12, 14 };

        static void AppleBob(PlayerSlot p, int round, int apples)
        {
            if (round == AppleDCs.Length)
            {
                bool win = apples >= 2;
                Finish(p, FestivalKind.AppleBob, win, win
                    ? $"You come up spluttering with {apples} apples clamped in your teeth. Maddie declares you the winner and pins a ribbon to your soaking shirt."
                    : $"{(apples == 0 ? "Not a single apple" : "One lonely apple")}, and a great deal of barrel water up your nose. Maddie pats you on the head. \"Good try.\"");
                return;
            }
            var pc = p.Character;
            int dc = AppleDCs[round];
            p.Hud.Dice.RollD20($"Apple Bobbing ({round + 1} of 3)", $"Constitution {AbilityUtil.Fmt(pc.Mod(Ability.CON))} · DC hidden",
                () => Checks.RollAbilityCheck(pc, Ability.CON, AdvantageState.Normal), dc, true,
                r => AppleBob(p, round + 1, apples + (Checks.IsSuccess(Checks.ResolveCheck(r, dc)) ? 1 : 0)));
        }

        // ------------------------------------------------------------ archery: three attack rolls with a borrowed shortbow

        static readonly int[] TargetACs = { 12, 14, 16 };

        /// <summary>DEX modifier, plus proficiency if the character is proficient with the shortbow (a simple weapon).</summary>
        public static int ArcheryBonus(PlayerCharacter pc)
        {
            var bow = ItemsDb.Get<WeaponDef>("shortbow");
            return pc.Mod(Ability.DEX) + (bow != null && pc.IsProficientWith(bow) ? pc.ProficiencyBonus : 0);
        }

        public static string ArcherySubtitle(PlayerCharacter pc) => $"Borrowed shortbow {AbilityUtil.Fmt(ArcheryBonus(pc))} to hit · AC hidden";

        static void Archery(PlayerSlot p, int round, int hits)
        {
            if (round == TargetACs.Length)
            {
                bool win = hits >= 2;
                Finish(p, FestivalKind.Archery, win, win
                    ? $"{hits} arrows in the straw. Captain Vale nods, impressed despite herself, and ties a ribbon to your bow arm. \"Not bad. For a civilian.\""
                    : "Your arrows find the straw, the hay bales, and one of Tobin's fence posts. Captain Vale writes something in her notebook. You suspect it's not a compliment.");
                return;
            }
            var pc = p.Character;
            int ac = TargetACs[round];
            p.Hud.Dice.RollD20($"Archery: target {round + 1} of 3", ArcherySubtitle(pc),
                () => Dice.RollD20(AdvantageState.Normal, new[] { new RollTerm("shortbow", ArcheryBonus(pc)) }), ac, false,
                r =>
                {
                    bool hit = r.IsNat20 || (!r.IsNat1 && r.Total >= ac);
                    if (r.IsNat20) p.Hud.Narrate("BULLSEYE! The crowd roars. Somewhere, Maddie screams with joy.", false);
                    Archery(p, round + 1, hits + (hit ? 1 : 0));
                });
        }

        // ------------------------------------------------------------ the bard stage: one Performance check, tips by result

        static void BardStage(PlayerSlot p)
        {
            var pc = p.Character;
            string sub = $"Performance (CHA) {AbilityUtil.Fmt(pc.SkillBonus(Skill.Performance))}";
            p.Hud.Dice.RollD20("The Bard Stage", sub, () => Checks.RollSkill(pc, Skill.Performance, AdvantageState.Normal), null, true, r =>
            {
                var g = GameRoot.I;
                int t = r.Total;
                int tips = t >= 20 ? 1000 : t >= 15 ? 500 : t >= 10 ? 200 : 50;
                g.GiveCopper(p, tips);
                string text = t >= 20 ? "The square goes silent, then ERUPTS. People are crying. Lark throws flowers. Dunstan and Hesper are hugging and don't seem to have noticed."
                    : t >= 15 ? "A proper crowd gathers, clapping along. Even Old Tobin taps his foot, then pretends he didn't."
                    : t >= 10 ? "Polite applause, a few coins in the hat, and one very enthusiastic goat."
                    : "Polite applause. Very polite. Pip brings you a consolation cider.";
                Finish(p, FestivalKind.BardStage, t >= 15, $"{text}\n\n(Tips: {Currency.Format(tips)})");
            });
        }

        // ------------------------------------------------------------ the end of any game

        /// <summary>Marks the game played (win or lose), hands out the ribbon to winners, and closes the festival quest's books.</summary>
        public static void Finish(PlayerSlot p, FestivalKind kind, bool win, string text)
        {
            var g = GameRoot.I;
            if (win) g.GiveItem(p, "festival_ribbon");
            g.World.SetFlag(FlagFor(kind)); // the quest counts flags (FlagsAtLeast), so this can complete it
            if (g.Quests.Done("harvest_festival") && g.World.FestivalDoneDay == 0) g.World.FestivalDoneDay = g.Clock.Day;
            Sfx.Play(win ? "success" : "fail");
            if (text != null) p.Hud.Push(new MessageScreen("The Harvest Festival", text));
        }
    }
}
