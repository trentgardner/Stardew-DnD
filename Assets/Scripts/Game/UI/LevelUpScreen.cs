using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Walks the player through one level-up: hit points (roll the die or take the average), then each
    /// choice the new level asks for (skills, cantrips, spells, an Ability Score Improvement or feat),
    /// then a summary to confirm. Nothing changes until Confirm; Esc / ✕ backs out with no changes.
    /// </summary>
    public class LevelUpScreen : GameScreen
    {
        readonly Action _done;
        PlayerCharacter _pc;
        List<LevelUpChoice> _choices;
        readonly LevelUpPlan _plan = new LevelUpPlan();
        bool _hpChosen;
        int _page; // 0 = HP, 1..n = choices, n+1 = summary
        MenuList _menu;
        Text _header, _info;

        public LevelUpScreen(Action done = null) { _done = done; }

        public int Page => _page;
        public int PageCount => _choices.Count + 2;
        public LevelUpChoice CurrentChoice => _page >= 1 && _page <= _choices.Count ? _choices[_page - 1] : null;
        List<string> Picks(int i) => _plan.Picks[i];

        public override void Build()
        {
            _pc = Slot.Character;
            _choices = _pc.PendingChoices();
            foreach (var _ in _choices) _plan.Picks.Add(new List<string>());
            Window(new Vector2(440, 300), $"Level {_pc.Level + 1}!");
            _header = UIKit.Label(UIKit.Rect(Root, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -46), new Vector2(-12, -26)), "", 11, UIKit.Ink, TextAnchor.MiddleLeft);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(10, 10), new Vector2(-4, -50));
            _menu = new MenuList(list) { RowHeight = 17, FontSize = 10, VisibleRows = 13, Wrap = false };
            _menu.OnHover = e => _info.text = e.Tooltip ?? "";
            var info = UIKit.Rect(Root, "Info", new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(6, 10), new Vector2(-12, -50));
            _info = UIKit.Label(info, "", 9, UIKit.Ink, TextAnchor.UpperLeft);
            _info.verticalOverflow = VerticalWrapMode.Truncate;
            ShowPage();
        }

        void ShowPage()
        {
            _menu.Clear();
            _info.text = "";
            if (_page == 0)
            {
                _header.text = $"Hit points: roll your d{_pc.Class.HitDie}, or take the average.";
                _menu.Add($"Roll d{_pc.Class.HitDie}!", RollHp, true, $"Roll the die in the tray. You add your CON modifier ({AbilityUtil.Fmt(_pc.Mod(Ability.CON))}), and get at least 1.");
                _menu.Add($"Take the average ({_pc.AverageHitDie})", PickHpAverage, true, $"A steady {_pc.AverageHitDie} + CON {AbilityUtil.Fmt(_pc.Mod(Ability.CON))}. No risk, no glory.");
            }
            else if (CurrentChoice != null)
            {
                var c = CurrentChoice;
                var picks = Picks(_page - 1);
                _header.text = $"{c.Title}  ({picks.Count}/{Need(c)} chosen)";
                foreach (var id in c.Options)
                {
                    string opt = id;
                    bool on = picks.Contains(id);
                    _menu.Add((on ? "✓ " : "   ") + PlayerCharacter.DescribeOption(id), () => { Toggle(opt); }, true, Describe(id));
                }
                _menu.Add(picks.Count >= Need(c) ? "Next ▸" : $"Next ▸ (choose {Need(c) - picks.Count} more)", Next, picks.Count >= Need(c));
                _menu.Add("◂ Back", Back);
            }
            else
            {
                _header.text = "Ready? Here's what you're taking:";
                var lines = new List<string> { _plan.HpRoll.HasValue ? $"HP: rolled {_plan.HpRoll} on the d{_pc.Class.HitDie}" : $"HP: the average ({_pc.AverageHitDie})" };
                for (int i = 0; i < _choices.Count; i++)
                    lines.Add($"{_choices[i].Title}: {string.Join(", ", Picks(i).Select(PlayerCharacter.DescribeOption))}");
                foreach (var f in _pc.Class.Features.Where(f => f.Level == _pc.Level + 1)) lines.Add($"New feature: {f.Name}");
                if (_pc.Class.Subclass != null) foreach (var f in _pc.Class.Subclass.Features.Where(f => f.Level == _pc.Level + 1)) lines.Add($"New feature ({_pc.Class.Subclass.Name}): {f.Name}");
                _info.text = string.Join("\n", lines);
                _menu.Add("Confirm level up", Confirm);
                _menu.Add("◂ Back", Back);
            }
            _menu.Build();
        }

        int Need(LevelUpChoice c) => Math.Min(c.Count, c.Options.Count);

        string Describe(string id)
        {
            var s = SpellsDb.Get(id);
            if (s != null) return $"<b>{s.Name}</b> ({(s.Level == 0 ? "cantrip" : "level " + s.Level)} {s.School})\n{s.Description}";
            if (id.StartsWith("asi:")) return "Ability Score Improvement: raise your scores (to a maximum of 20).";
            if (id.StartsWith("grappler:")) return "Grappler: +1 STR or DEX. You're an expert at grabbing and holding foes.";
            if (Enum.TryParse<Skill>(id, out var sk)) return $"{SkillUtil.Name(sk)} ({SkillUtil.AbilityOf(sk)}). Now {AbilityUtil.Fmt(_pc.SkillBonus(sk))}.";
            return "";
        }

        // ------------------------------------------------------------ actions (public so tests can drive the screen)

        public void PickHpAverage() { _plan.HpRoll = null; _hpChosen = true; Next(); }

        void RollHp()
        {
            var roll = Dice.RollDamage(new DiceExpr(1, _pc.Class.HitDie));
            Hud.Dice.RollDamage("Hit Points", $"d{_pc.Class.HitDie} + CON {AbilityUtil.Fmt(_pc.Mod(Ability.CON))}", roll, DieColor.Healing, () =>
            {
                if (Closed || Root == null) return;   // closed while the die rolled: nothing was applied yet
                _plan.HpRoll = roll.Dice[0].Value;
                _hpChosen = true;
                Next();
            });
        }

        public void Toggle(string id)
        {
            var c = CurrentChoice;
            if (c == null || !c.Options.Contains(id)) return;
            var picks = Picks(_page - 1);
            if (picks.Contains(id)) picks.Remove(id);
            else if (picks.Count < Need(c)) picks.Add(id);
            else if (Need(c) == 1) { picks.Clear(); picks.Add(id); }
            Sfx.Play("ui");
            int keep = _menu.Index;
            ShowPage();
            _menu.Index = Mathf.Min(keep, _menu.Entries.Count - 1);
            _menu.Refresh();
        }

        public void Next()
        {
            if (_page == 0 && !_hpChosen) return;
            if (CurrentChoice != null && Picks(_page - 1).Count < Need(CurrentChoice)) return;
            if (_page < PageCount - 1) { _page++; ShowPage(); }
        }

        public void Back() { if (_page > 0) { _page--; ShowPage(); } }

        public void Confirm()
        {
            if (_page != PageCount - 1) return;
            var log = new List<string>();
            _pc.LevelUp(_plan, log);
            Sfx.Play("levelup");
            Close();
            Hud.Push(new MessageScreen($"Level {_pc.Level}!", string.Join("\n", log), _done));
        }

        public override void Tick()
        {
            _menu.Tick(In, Cam);
            if (In.Cancel) Close();
        }
    }
}
