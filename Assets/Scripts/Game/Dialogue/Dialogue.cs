using System;
using System.Collections.Generic;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    public class DCtx
    {
        public PlayerSlot P;
        public NpcAgent Npc;
        public CompanionAgent Comp;
        public RecruitAgent Recruit;
        public DialogueScreen Screen;
        public CheckOutcome LastOutcome;
        public D20Result LastRoll;
        public PlayerCharacter PC => P.Character;
        public string NpcId => Npc != null ? Npc.Def.Id : Recruit != null ? "recruit_" + Recruit.Data.Character.Name : Comp != null ? "comp" : "none";
        public GameRoot G => GameRoot.I;
        public string Name => PC.Name;
        public bool Is(string classId) => PC.Class.Id == classId;
        public bool Bg(string bgId) => PC.Background.Id == bgId;
        public bool Species(string id) => PC.Species.Id == id;
        public int Hearts => G.World.Hearts(NpcId);
    }

    public class DOption
    {
        public string Text;
        public Func<DCtx, bool> Show = c => true;
        /// <summary>Role tag shown in brackets, e.g. "Bard", "Acolyte" (for role-based options).</summary>
        public string Role;
        public Skill? Check;
        public int DC = 12;
        /// <summary>Friendship lowers social DCs by 1 per heart (max 3).</summary>
        public bool Social = true;
        public Func<DCtx, AdvantageState> Advantage;
        public string Next;              // after a plain option or any check result without a specific branch
        public string Success, Failure, CritSuccess, CritFailure;
        public Action<DCtx> Do;          // runs when chosen (before branching) for plain options
        public Action<DCtx> OnSuccess, OnFailure, OnCritSuccess, OnCritFailure;
        public bool End;
        public bool OncePerDay;          // e.g. performing for tips

        public string Label(DCtx c)
        {
            string tag = "";
            if (Role != null) tag += $"[{Role}] ";
            if (Check.HasValue) tag += $"[{SkillUtil.Name(Check.Value)}] ";
            return tag + Text;
        }

        public string LockKey(DCtx c) => $"{c.P.Index}:{c.NpcId}:{(Check.HasValue ? Check.Value.ToString() : Text)}";
    }

    public class DNode
    {
        public string Id;
        public string Speaker;               // null = narrator only
        public Func<DCtx, string> Text;
        public Func<DCtx, string> Narration; // the DM's description
        public Action<DCtx> OnEnter;
        public List<DOption> Options = new List<DOption>();
        public string AutoNext;              // continue without options

        public DNode Opt(DOption o) { Options.Add(o); return this; }

        public DNode Say(string text, string next = null, Action<DCtx> doIt = null, bool end = false, Func<DCtx, bool> show = null, string role = null)
        {
            Options.Add(new DOption { Text = text, Next = next, Do = doIt, End = end, Show = show ?? (c => true), Role = role });
            return this;
        }

        public DNode Roll(Skill skill, int dc, string text, string success, string failure, Action<DCtx> onSuccess = null, Action<DCtx> onFailure = null,
                          string role = null, Func<DCtx, bool> show = null, string critSuccess = null, string critFailure = null,
                          Action<DCtx> onCritSuccess = null, Action<DCtx> onCritFailure = null, bool social = true, Func<DCtx, AdvantageState> adv = null)
        {
            Options.Add(new DOption
            {
                Check = skill, DC = dc, Text = text, Success = success, Failure = failure, OnSuccess = onSuccess, OnFailure = onFailure,
                Role = role, Show = show ?? (c => true), CritSuccess = critSuccess, CritFailure = critFailure,
                OnCritSuccess = onCritSuccess, OnCritFailure = onCritFailure, Social = social, Advantage = adv,
            });
            return this;
        }

        public DNode Bye(string text = "Goodbye.") => Say(text, end: true);
    }

    public class Dialogue
    {
        public string Id;
        public Func<DCtx, string> Start;
        public readonly Dictionary<string, DNode> Nodes = new Dictionary<string, DNode>();

        public Dialogue(string id, Func<DCtx, string> start) { Id = id; Start = start; }

        public DNode Node(string id, string speaker, Func<DCtx, string> text, Func<DCtx, string> narration = null, Action<DCtx> onEnter = null)
        {
            var n = new DNode { Id = id, Speaker = speaker, Text = text, Narration = narration, OnEnter = onEnter };
            Nodes[id] = n;
            return n;
        }
    }

    /// <summary>The conversation window: portrait name, line, DM narration, options and the dice for checks.</summary>
    public class DialogueScreen : GameScreen
    {
        readonly string _dialogueId;
        readonly DCtx _ctx;
        Dialogue _d;
        DNode _node;
        MenuList _menu;
        UnityEngine.UI.Text _speaker, _line, _narr, _hearts;
        List<DOption> _visible = new List<DOption>();
        bool _rolling;

        public DialogueScreen(string dialogueId, NpcAgent npc, CompanionAgent comp, RecruitAgent recruit = null)
        {
            _dialogueId = dialogueId;
            _ctx = new DCtx { Npc = npc, Comp = comp, Recruit = recruit };
        }

        public override void Build()
        {
            _ctx.P = Slot;
            _ctx.Screen = this;
            _d = DialogueDb.Get(_dialogueId);
            Root = UIKit.Box(Hud.Root, "Dialogue", new Vector2(0.5f, 0), new Vector2(420, 190), new Vector2(0, 40));
            UIKit.Panel(Root);
            var sp = UIKit.Rect(Root, "Speaker", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -22), new Vector2(-10, -4));
            _speaker = UIKit.Label(sp, "", 13, UIKit.Ink, TextAnchor.MiddleLeft);
            _speaker.fontStyle = FontStyle.Bold;
            _hearts = UIKit.Label(sp, "", 11, UIKit.Bad, TextAnchor.MiddleRight);
            var narr = UIKit.Rect(Root, "Narr", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -56), new Vector2(-10, -22));
            _narr = UIKit.Label(narr, "", 10, new Color32(110, 80, 60, 255), TextAnchor.UpperLeft);
            _narr.fontStyle = FontStyle.Italic;
            var line = UIKit.Rect(Root, "Line", new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -100), new Vector2(-10, -56));
            _line = UIKit.Label(line, "", 11, UIKit.Ink, TextAnchor.UpperLeft);
            var opts = UIKit.Rect(Root, "Options", new Vector2(0, 0), new Vector2(1, 0), new Vector2(8, 6), new Vector2(-8, 90));
            _menu = new MenuList(opts) { RowHeight = 16, FontSize = 10 };
            GameRoot.I.World.MetToday(_ctx.NpcId);
            Goto(_d.Start(_ctx));
        }

        public void SetSpeaker(string name) { if (_speaker != null) _speaker.text = name; }

        public void Goto(string nodeId)
        {
            if (nodeId == null || !_d.Nodes.TryGetValue(nodeId, out _node)) { Close(); return; }
            _speaker.text = _node.Speaker ?? "";
            _node.OnEnter?.Invoke(_ctx);   // may override the speaker (recruits) or close the window
            if (Closed) return;
            string narration = _node.Narration?.Invoke(_ctx);
            _narr.text = narration ?? "";
            _line.text = _node.Text != null ? _node.Text(_ctx) : "";
            if (_node.Speaker != null && !string.IsNullOrEmpty(_line.text)) _line.text = "\"" + _line.text + "\"";
            int hearts = GameRoot.I.World.Hearts(_ctx.NpcId);
            _hearts.text = _ctx.Npc != null ? new string('♥', hearts) + new string('♡', Mathf.Max(0, 5 - hearts)) : "";

            _menu.Clear();
            _visible.Clear();
            var w = GameRoot.I.World;
            foreach (var o in _node.Options)
            {
                if (!o.Show(_ctx)) continue;
                bool locked = o.Check.HasValue && o.Social && w.IsLocked(o.LockKey(_ctx));
                bool usedToday = o.OncePerDay && w.IsLocked("once:" + o.LockKey(_ctx));
                string label = o.Label(_ctx);
                if (locked) label += "  (they won't fall for that again today)";
                if (usedToday) label += "  (done for today)";
                _visible.Add(o);
                var opt = o;
                _menu.Add(label, () => Choose(opt), !locked && !usedToday);
            }
            if (_visible.Count == 0)
            {
                string next = _node.AutoNext;
                _menu.Add(next != null ? "Continue..." : "(Leave)", () => { if (next != null) Goto(next); else Close(); });
            }
            _menu.Build();
        }

        void Choose(DOption o)
        {
            if (o.OncePerDay) GameRoot.I.World.Lock("once:" + o.LockKey(_ctx), 24 * 60);
            if (!o.Check.HasValue)
            {
                o.Do?.Invoke(_ctx);
                if (o.End) { Close(); return; }
                if (!Closed) Goto(o.Next ?? (_node.Id));
                return;
            }

            // A real skill check: the player rolls the d20.
            var skill = o.Check.Value;
            var pc = _ctx.PC;
            int dc = o.DC;
            if (o.Social) dc -= Mathf.Min(3, GameRoot.I.World.Hearts(_ctx.NpcId));
            var adv = o.Advantage != null ? o.Advantage(_ctx) : AdvantageState.Normal;
            if (o.Social && _ctx.Npc != null && GameRoot.I.World.CharmedBy(_ctx.NpcId, Slot.Index)) adv = Dice.Combine(true, adv == AdvantageState.Disadvantage);
            string sub = $"{SkillUtil.Name(skill)} ({SkillUtil.AbilityOf(skill)}) {AbilityUtil.Fmt(pc.SkillBonus(skill))}" +
                         (adv == AdvantageState.Advantage ? " · Advantage" : adv == AdvantageState.Disadvantage ? " · Disadvantage" : "") +
                         " · DC hidden";
            _rolling = true;
            Hud.Dice.RollD20($"{SkillUtil.Name(skill)} Check", sub, () => Checks.RollSkill(pc, skill, adv), dc, true, r =>
            {
                _rolling = false;
                var outcome = Checks.ResolveCheck(r, dc);
                _ctx.LastOutcome = outcome;
                _ctx.LastRoll = r;
                var w = GameRoot.I.World;
                // XP for overcoming a real challenge the first time (DC 12+), more for a natural 20.
                if (Checks.IsSuccess(outcome) && o.DC >= 12)
                    GameRoot.I.AwardOnce(Slot, $"check:{_ctx.NpcId}:{o.Check}:{o.Text.GetHashCode()}", o.DC * 2 * (outcome == CheckOutcome.CriticalSuccess ? 3 : 2) / 2, "a clever check");
                string next;
                switch (outcome)
                {
                    case CheckOutcome.CriticalSuccess:
                        Sfx.Play("success");
                        (o.OnCritSuccess ?? o.OnSuccess)?.Invoke(_ctx);
                        next = o.CritSuccess ?? o.Success;
                        if (o.Social) w.AddFriendship(_ctx.NpcId, 20);
                        break;
                    case CheckOutcome.Success:
                        Sfx.Play("success");
                        o.OnSuccess?.Invoke(_ctx);
                        next = o.Success;
                        if (o.Social) w.AddFriendship(_ctx.NpcId, 10);
                        break;
                    case CheckOutcome.CriticalFailure:
                        Sfx.Play("fail");
                        (o.OnCritFailure ?? o.OnFailure)?.Invoke(_ctx);
                        next = o.CritFailure ?? o.Failure;
                        if (o.Social) { w.Lock(o.LockKey(_ctx), 24 * 60); w.AddFriendship(_ctx.NpcId, -10); }
                        break;
                    default:
                        Sfx.Play("fail");
                        o.OnFailure?.Invoke(_ctx);
                        next = o.Failure;
                        // 2024 Influence rule: can't repeat the same approach with this creature for 24 hours.
                        if (o.Social) w.Lock(o.LockKey(_ctx), 24 * 60);
                        break;
                }
                if (Closed) return;
                if (o.End && next == null) { Close(); return; }
                Goto(next ?? o.Next ?? _node.Id);
            });
        }

        public override void Tick()
        {
            if (_rolling) return;
            _menu.Tick(In, Cam);
            if (In.Cancel && !Closed) Close();
        }
    }
}
