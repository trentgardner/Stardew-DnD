using System;
using System.Collections.Generic;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DnD.Game
{
    /// <summary>
    /// Tabletop-style character creation in the player's own split-screen viewport.
    /// Steps: mode → name → species → class → background → ROLL 4d6 drop lowest (5 free rerolls)
    /// → assign rolls to abilities → background bonus → skills → class options → spells → review.
    /// </summary>
    public class CharacterCreationScreen : GameScreen
    {
        enum Step { Mode, Name, Species, Class, Background, Roll, Assign, Bonus, Skills, ClassOption, Cantrips, Spells, Review }

        readonly Action<PlayerCharacter> _done;
        Step _step;
        bool _quick;
        readonly CharacterChoices _ch = new CharacterChoices();
        MenuList _menu;
        Text _header, _info;
        string _typedName = "";
        List<List<DieRoll>> _rolls;
        int[] _rolled = new int[6];
        int _rerolls = CharacterFactory.FreeRerolls;
        readonly int[] _assigned = { -1, -1, -1, -1, -1, -1 }; // ability → index into _rolled
        bool _rollingNow;

        static readonly string[] Names = { "Arden", "Brynn", "Caspian", "Delphine", "Eamon", "Fiora", "Gideon", "Hazel", "Ivo", "Juniper", "Kestrel", "Lark", "Magnus", "Nessa", "Orrin", "Perrin", "Quinn", "Rowan", "Sorrel", "Talia", "Vesper", "Wynn" };

        public CharacterCreationScreen(Action<PlayerCharacter> done) { _done = done; }
        public override bool ShowCloseButton => false; // Esc steps back instead

        public override void Build()
        {
            Window(new Vector2(440, 300), null);
            var h = UIKit.Rect(Root, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -46), new Vector2(-12, -6));
            _header = UIKit.Label(h, "", 13, UIKit.Ink, TextAnchor.UpperLeft);
            var list = UIKit.Rect(Root, "List", new Vector2(0, 0), new Vector2(0.48f, 1), new Vector2(8, 10), new Vector2(0, -50));
            _menu = new MenuList(list) { RowHeight = 15, FontSize = 10, VisibleRows = 15, Wrap = false };
            _menu.OnHover = e => { if (e.Tooltip != null) _info.text = e.Tooltip; };
            var info = UIKit.Rect(Root, "Info", new Vector2(0.48f, 0), new Vector2(1, 1), new Vector2(8, 10), new Vector2(-12, -50));
            _info = UIKit.Label(info, "", 9, UIKit.Ink);
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
            _typedName = Names[UnityEngine.Random.Range(0, Names.Length)];
            Go(Step.Mode);
        }

        public override void OnClose() { In.TypingMode = false; if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }

        void OnText(char c)
        {
            if (_step != Step.Name) return;
            if (c == '\b') { if (_typedName.Length > 0) _typedName = _typedName.Substring(0, _typedName.Length - 1); }
            else if (!char.IsControl(c) && _typedName.Length < 16 && (char.IsLetter(c) || c == ' ' || c == '\'' || c == '-')) _typedName += c;
            else return;
            Go(Step.Name);
        }

        void Go(Step s)
        {
            _step = s;
            In.TypingMode = s == Step.Name;
            _menu.Clear();
            _info.text = "";
            string title = $"<b>Create Player {Slot.Index + 1}'s Character</b>";
            switch (s)
            {
                case Step.Mode:
                    _header.text = title + "\nHow would you like to begin?";
                    _menu.Add("Full creation (the whole tabletop experience)", () => { _quick = false; Go(Step.Name); }, true, "Choose everything yourself: species, class, background, roll your stats, assign them, pick skills and spells.");
                    _menu.Add("Quick start (pick a class, roll and go)", () => { _quick = true; Go(Step.Name); }, true, "Pick a name, species and class. You still roll your stats! Everything else gets sensible defaults.");
                    break;

                case Step.Name:
                    _header.text = title + $"\nName: <b>{_typedName}</b>_  <i>(type on the keyboard, Enter to continue)</i>";
                    _menu.Add("Continue", () => { _ch.Name = string.IsNullOrWhiteSpace(_typedName) ? "Adventurer" : _typedName.Trim(); Go(Step.Species); });
                    _menu.Add("Random name", () => { _typedName = Names[UnityEngine.Random.Range(0, Names.Length)]; Go(Step.Name); });
                    break;

                case Step.Species:
                    _header.text = title + "\nChoose your species.";
                    foreach (var sp in CharacterDb.Species.Values)
                    {
                        var x = sp;
                        _menu.Add(sp.Name, () => { _ch.SpeciesId = x.Id; Go(Step.Class); }, true, $"<b>{sp.Name}</b>\n{sp.Description}\n\n" + string.Join("\n", sp.Traits.Select(t => "· " + t)));
                    }
                    break;

                case Step.Class:
                    _header.text = title + "\nChoose your class.";
                    foreach (var c in CharacterDb.Classes.Values)
                    {
                        var x = c;
                        _menu.Add(c.Name, () =>
                        {
                            if (_quick)
                            {
                                var d = CharacterFactory.DefaultChoices(x.Id, _ch.SpeciesId, _ch.Name);
                                CopyDefaults(d);
                                Go(Step.Roll);
                            }
                            else { _ch.ClassId = x.Id; Go(Step.Background); }
                        }, true, $"<b>{c.Name}</b> (d{c.HitDie} hit die, primary {c.PrimaryAbility})\n{c.Description}\n\n" + string.Join("\n", c.Features.Select(f => $"L{f.Level} {f.Name}: {f.Text}")) + (c.Subclass != null ? $"\n\nLevel 3: {c.Subclass.Name}. {c.Subclass.Description}" : ""));
                    }
                    break;

                case Step.Background:
                    _header.text = title + "\nChoose your background.";
                    foreach (var b in CharacterDb.Backgrounds.Values)
                    {
                        var x = b;
                        _menu.Add(b.Name, () => { _ch.BackgroundId = x.Id; Go(Step.Roll); }, true,
                            $"<b>{b.Name}</b>\n{b.Description}\n\nAbility increases: {string.Join(", ", b.AbilityOptions)}\nSkills: {string.Join(", ", b.Skills.Select(SkillUtil.Name))}\nOrigin feat: {b.OriginFeat}: {b.OriginFeatText}\nStarting gold: {Currency.Format(b.StartingGoldCp)}");
                    }
                    break;

                case Step.Roll:
                    _header.text = title + $"\nRoll your ability scores! 4d6, drop the lowest. Free rerolls left: {_rerolls}";
                    if (_rolls == null) _menu.Add("🎲 Roll the dice!", RollAll);
                    else
                    {
                        _info.text = "Your rolls:\n\n" + string.Join("\n", _rolls.Select((r, i) => $"{_rolled[i],2}   [{string.Join(" ", r.Select(d => d.Discarded ? $"<color=#999999>{d.Value}</color>" : d.Value.ToString()))}]")) +
                                     $"\n\nTotal: {_rolled.Sum()}";
                        _menu.Add("Keep these rolls", () => { for (int i = 0; i < 6; i++) _assigned[i] = -1; if (_quick) { AutoAssign(); Go(Step.Review); } else Go(Step.Assign); });
                        _menu.Add($"Reroll all ({_rerolls} left)", () => { _rerolls--; RollAll(); }, _rerolls > 0);
                    }
                    break;

                case Step.Assign:
                {
                    var cls = CharacterDb.Classes[_ch.ClassId];
                    int next = NextUnassignedRoll();
                    _header.text = title + (next >= 0 ? $"\nAssign your <b>{_rolled[next]}</b> to which ability?" : "\nAll scores assigned!");
                    _info.text = "Rolled: " + string.Join(", ", _rolled.Select((v, i) => _assigned.Contains(i) ? $"<color=#999999>{v}</color>" : $"<b>{v}</b>")) + "\n\n" +
                                 string.Join("\n", AbilityUtil.All.Select(a => $"{AbilityUtil.FullName(a)}: {(_assigned[(int)a] >= 0 ? _rolled[_assigned[(int)a]].ToString() : "—")}{(a == cls.PrimaryAbility ? "  ★ primary" : "")}"));
                    if (next >= 0)
                    {
                        foreach (var a in AbilityUtil.All)
                        {
                            if (_assigned[(int)a] >= 0) continue;
                            var ab = a;
                            _menu.Add($"{AbilityUtil.FullName(a)}{(a == cls.PrimaryAbility ? " ★" : "")}", () => { _assigned[(int)ab] = next; Go(Step.Assign); });
                        }
                        _menu.Add("Auto-assign (recommended for your class)", () => { AutoAssign(); Go(Step.Assign); });
                    }
                    else
                    {
                        _menu.Add("Continue", () => { ApplyAssigned(); Go(Step.Bonus); });
                    }
                    _menu.Add("Start over", () => { for (int i = 0; i < 6; i++) _assigned[i] = -1; Go(Step.Assign); });
                    break;
                }

                case Step.Bonus:
                {
                    var bg = CharacterDb.Backgrounds[_ch.BackgroundId];
                    _header.text = title + $"\nYour {bg.Name} background raises your abilities.";
                    var o = bg.AbilityOptions;
                    foreach (var two in o)
                        foreach (var one in o)
                        {
                            if (two == one) continue;
                            var t2 = two; var t1 = one;
                            _menu.Add($"+2 {two}, +1 {one}", () => { _ch.PlusTwo = t2; _ch.PlusOne = t1; _ch.SpreadEvenly = false; Go(Step.Skills); }, true,
                                $"{two}: {_ch.Scores[(int)two]} → {Math.Min(20, _ch.Scores[(int)two] + 2)}\n{one}: {_ch.Scores[(int)one]} → {Math.Min(20, _ch.Scores[(int)one] + 1)}");
                        }
                    _menu.Add($"+1 {o[0]}, +1 {o[1]}, +1 {o[2]}", () => { _ch.SpreadEvenly = true; Go(Step.Skills); });
                    break;
                }

                case Step.Skills:
                {
                    var cls = CharacterDb.Classes[_ch.ClassId];
                    var sp = CharacterDb.Species[_ch.SpeciesId];
                    var bg = CharacterDb.Backgrounds[_ch.BackgroundId];
                    int need = cls.SkillChoices + sp.BonusSkillChoices;
                    int have = _ch.ClassSkills.Count + _ch.BonusSkills.Count;
                    var options = cls.SkillOptions.Length > 0 ? cls.SkillOptions.ToList() : SkillUtil.All.ToList();
                    var fixedSkills = bg.Skills.Concat(sp.GrantedSkills).ToList();
                    _header.text = title + $"\nChoose skills: {have}/{need}  (from {bg.Name}: {string.Join(", ", bg.Skills.Select(SkillUtil.Name))})";
                    _menu.Add(have >= need ? "✔ Continue" : $"Pick {need - have} more", () => Go(Step.ClassOption), have >= need);
                    foreach (var skill in SkillUtil.All)
                    {
                        if (fixedSkills.Contains(skill)) continue;
                        bool fromClass = options.Contains(skill);
                        bool picked = _ch.ClassSkills.Contains(skill) || _ch.BonusSkills.Contains(skill);
                        bool classFull = _ch.ClassSkills.Count >= cls.SkillChoices;
                        bool bonusFull = _ch.BonusSkills.Count >= sp.BonusSkillChoices;
                        bool canPick = picked || (fromClass && !classFull) || !bonusFull;
                        var sk = skill;
                        _menu.Add($"{(picked ? "■" : "□")} {SkillUtil.Name(skill)} ({SkillUtil.AbilityOf(skill)}){(fromClass ? "" : " *")}", () =>
                        {
                            if (_ch.ClassSkills.Remove(sk) || _ch.BonusSkills.Remove(sk)) { }
                            else if (fromClass && _ch.ClassSkills.Count < cls.SkillChoices) _ch.ClassSkills.Add(sk);
                            else if (_ch.BonusSkills.Count < sp.BonusSkillChoices) _ch.BonusSkills.Add(sk);
                            int keep = _menu.Index;
                            Go(Step.Skills);
                            _menu.Index = keep; _menu.Refresh();
                        }, canPick, SkillHelp(skill) + (fromClass ? "" : "\n\n* Not on your class list: uses a species bonus pick."));
                    }
                    break;
                }

                case Step.ClassOption:
                    if (_ch.ClassId == "fighter")
                    {
                        _header.text = title + "\nChoose a Fighting Style.";
                        void Style(string id, string name, string tip) => _menu.Add(name, () => { _ch.FightingStyle = id; Go(Step.Cantrips); }, true, tip);
                        Style("defense", "Defense", "+1 AC while wearing armor.");
                        Style("dueling", "Dueling", "+2 damage with a one-handed melee weapon and no other weapon.");
                        Style("great_weapon", "Great Weapon Fighting", "Treat 1s and 2s on damage dice as 3s with two-handed weapons.");
                        Style("archery", "Archery", "+2 to attack rolls with ranged weapons.");
                    }
                    else if (_ch.ClassId == "cleric")
                    {
                        _header.text = title + "\nChoose your Divine Order.";
                        _menu.Add("Protector", () => { _ch.DivineOrder = "protector"; Go(Step.Cantrips); }, true, "Trained for battle: heavy armor and martial weapons. You start in chain mail.");
                        _menu.Add("Thaumaturge", () => { _ch.DivineOrder = "thaumaturge"; Go(Step.Cantrips); }, true, "One extra cantrip, and add your WIS modifier to Arcana and Religion checks.");
                    }
                    else Go(Step.Cantrips);
                    break;

                case Step.Cantrips:
                {
                    var cls = CharacterDb.Classes[_ch.ClassId];
                    if (cls.Caster == CasterType.None) { Go(Step.Review); return; }
                    int need = cls.CantripsKnown[1] + (_ch.DivineOrder == "thaumaturge" && _ch.ClassId == "cleric" ? 1 : 0);
                    SpellPicker(title, "cantrips", need, SpellsDb.ForClass(cls.Id, 0).Where(x => x.IsCantrip), _ch.Cantrips, Step.Spells);
                    break;
                }

                case Step.Spells:
                {
                    var cls = CharacterDb.Classes[_ch.ClassId];
                    SpellPicker(title, "level 1 spells", cls.PreparedSpells[1], SpellsDb.ForClass(cls.Id, 1).Where(x => x.Level == 1), _ch.Spells, Step.Review);
                    break;
                }

                case Step.Review:
                {
                    var pc = CharacterFactory.Create(_ch);
                    _header.text = title + $"\n<b>{pc.Name}</b>, {pc.Species.Name} {pc.Class.Name} ({pc.Background.Name})";
                    _info.text = string.Join("\n", AbilityUtil.All.Select(a => $"{a} {pc.Score(a)} ({AbilityUtil.Fmt(pc.Mod(a))})")) +
                                 $"\n\nHP {pc.MaxHP} · AC {pc.ArmorClass} · Speed {pc.Speed} ft\nWeapon: {pc.GetAttacks()[0].Summary()}\nSkills: {string.Join(", ", pc.SkillProficiencies.Select(SkillUtil.Name))}" +
                                 (pc.IsCaster ? $"\nCantrips: {string.Join(", ", pc.Cantrips.Select(x => SpellsDb.Get(x)?.Name))}\nSpells: {string.Join(", ", pc.PreparedSpells.Select(x => SpellsDb.Get(x)?.Name))}" : "") +
                                 $"\nGold: {Currency.Format(pc.Copper)}";
                    _menu.Add("✔ Begin your adventure!", () => { Close(); _done(pc); });
                    _menu.Add("Start over", () => { _rolls = null; _rerolls = CharacterFactory.FreeRerolls; ResetChoices(); Go(Step.Mode); });
                    break;
                }
            }
            _menu.Build();
            if (_menu.Entries.Count > 0 && _menu.Entries[_menu.Index].Tooltip != null) _info.text = _menu.Entries[_menu.Index].Tooltip;
        }

        void SpellPicker(string title, string what, int need, IEnumerable<SpellDef> pool, List<string> into, Step next)
        {
            _header.text = title + $"\nChoose {need} {what}: {into.Count}/{need}";
            _menu.Add(into.Count >= need ? "✔ Continue" : $"Pick {need - into.Count} more", () => Go(next), into.Count >= need);
            foreach (var s in pool)
            {
                var sp = s;
                bool picked = into.Contains(s.Id);
                _menu.Add($"{(picked ? "■" : "□")} {s.Name}", () =>
                {
                    if (!into.Remove(sp.Id) && into.Count < need) into.Add(sp.Id);
                    int keep = _menu.Index;
                    Go(_step);
                    _menu.Index = keep; _menu.Refresh();
                }, picked || into.Count < need, $"<b>{s.Name}</b>{(s.Concentration ? " (Concentration)" : "")}\n{s.School} · {s.CastTime} · {(s.RangeFt == 0 ? "Self" : s.RangeFt == 5 ? "Touch" : s.RangeFt + " ft")}\n\n{s.Description}");
            }
        }

        static string SkillHelp(Skill s)
        {
            switch (s)
            {
                case Skill.Persuasion: return "Persuasion (CHA): convince people honestly. Haggling, recruiting, calming things down.";
                case Skill.Deception: return "Deception (CHA): lie convincingly.";
                case Skill.Intimidation: return "Intimidation (CHA): threats. Works, but people remember.";
                case Skill.Insight: return "Insight (WIS): read people. Spot lies, find what they really want.";
                case Skill.Perception: return "Perception (WIS): notice things. Also your passive alertness.";
                case Skill.Stealth: return "Stealth (DEX): sneak past enemies (hold Ctrl / click L3 to sneak).";
                case Skill.Performance: return "Performance (CHA): music, acting, a very convincing dragon impression.";
                case Skill.Athletics: return "Athletics (STR): climb, jump, shove, grapple.";
                case Skill.Medicine: return "Medicine (WIS): stabilize dying friends (DC 10).";
                case Skill.Religion: return "Religion (INT): gods, rites, holy symbols.";
                case Skill.Arcana: return "Arcana (INT): magic, spells, strange glowing eggs.";
                default: return $"{SkillUtil.Name(s)} ({SkillUtil.AbilityOf(s)})";
            }
        }

        void CopyDefaults(CharacterChoices d)
        {
            _ch.ClassId = d.ClassId; _ch.BackgroundId = d.BackgroundId; _ch.PlusTwo = d.PlusTwo; _ch.PlusOne = d.PlusOne; _ch.SpreadEvenly = d.SpreadEvenly;
            _ch.ClassSkills = d.ClassSkills; _ch.BonusSkills = d.BonusSkills; _ch.Cantrips = d.Cantrips; _ch.Spells = d.Spells;
            _ch.FightingStyle = d.FightingStyle; _ch.DivineOrder = d.DivineOrder;
        }

        void ResetChoices()
        {
            _ch.ClassSkills = new List<Skill>(); _ch.BonusSkills = new List<Skill>(); _ch.Cantrips = new List<string>(); _ch.Spells = new List<string>();
            for (int i = 0; i < 6; i++) _assigned[i] = -1;
        }

        void RollAll()
        {
            _rolls = CharacterFactory.RollAbilityScores();
            for (int i = 0; i < 6; i++) _rolled[i] = Dice.SumKept(_rolls[i]);
            Array.Sort(_rolled);
            Array.Reverse(_rolled);
            _rolls = _rolls.OrderByDescending(Dice.SumKept).ToList();
            var all = _rolls.SelectMany(x => x).ToList();
            _rollingNow = true;
            Hud.Dice.RollDice("Rolling 6 × 4d6 (drop lowest)", all, DieColor.D20, "Scores: " + string.Join(", ", _rolled), true, () => { _rollingNow = false; Go(Step.Roll); });
        }

        int NextUnassignedRoll()
        {
            for (int i = 0; i < 6; i++) if (!_assigned.Contains(i)) return i;
            return -1;
        }

        /// <summary>Puts the highest rolls into the class's most important abilities.</summary>
        void AutoAssign()
        {
            var order = new Dictionary<string, Ability[]>
            {
                { "fighter", new[] { Ability.STR, Ability.CON, Ability.DEX, Ability.WIS, Ability.CHA, Ability.INT } },
                { "wizard", new[] { Ability.INT, Ability.CON, Ability.DEX, Ability.WIS, Ability.CHA, Ability.STR } },
                { "cleric", new[] { Ability.WIS, Ability.CON, Ability.STR, Ability.CHA, Ability.DEX, Ability.INT } },
                { "bard", new[] { Ability.CHA, Ability.DEX, Ability.CON, Ability.WIS, Ability.INT, Ability.STR } },
            }[_ch.ClassId];
            var free = Enumerable.Range(0, 6).Where(i => !_assigned.Contains(i)).OrderByDescending(i => _rolled[i]).ToList();
            foreach (var a in order)
            {
                if (_assigned[(int)a] >= 0 || free.Count == 0) continue;
                _assigned[(int)a] = free[0];
                free.RemoveAt(0);
            }
            ApplyAssigned();
        }

        void ApplyAssigned()
        {
            for (int a = 0; a < 6; a++) if (_assigned[a] >= 0) _ch.Scores[a] = _rolled[_assigned[a]];
        }

        public override void Tick()
        {
            if (_rollingNow) return;
            _menu.Tick(In, Cam);
            if (In.Cancel && _step != Step.Mode && _step != Step.Name)
            {
                var back = _step == Step.Assign ? Step.Roll : _step == Step.Roll ? (_quick ? Step.Class : Step.Background) : (Step)Math.Max(0, (int)_step - 1);
                if (back == Step.ClassOption && _ch.ClassId != "fighter" && _ch.ClassId != "cleric") back = Step.Skills;
                Go(back);
            }
        }
    }
}
