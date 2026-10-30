using System;
using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>
    /// Where each quest stands. Stages are integers (see QuestDb for the text). Reaching a quest's last stage pays its
    /// reward exactly once (Paid is saved, so reloading or a second player finishing it again never pays twice).
    /// </summary>
    public class QuestLog
    {
        public readonly Dictionary<string, int> Stages = new Dictionary<string, int>();
        public readonly HashSet<string> Paid = new HashSet<string>();

        /// <summary>Raised after any stage change (quest spots and gated groups re-check themselves).</summary>
        public event Action Changed;

        public int Stage(string id) => Stages.TryGetValue(id, out var s) ? s : 0;
        public bool Done(string id) => QuestDb.Get(id) is QuestDef q && Stage(id) >= q.Last;
        public bool Active(string id) => QuestDb.Get(id) is QuestDef q && Stage(id) > 0 && Stage(id) < q.Last;

        public void Start(string id, PlayerSlot by = null)
        {
            if (Stage(id) > 0) return;
            Set(id, 1, by);
        }

        /// <summary>Moves a quest forward (never back). `by` is who did it: they get the reward item.</summary>
        public void Set(string id, int stage, PlayerSlot by = null) => Set(id, stage, by, 1f);

        /// <summary>rewardScale shrinks XP and coin (e.g. 1/3 when the watch found the lost child instead of you).</summary>
        public void Set(string id, int stage, PlayerSlot by, float rewardScale)
        {
            if (Stage(id) >= stage) return;
            Stages[id] = stage;
            var q = QuestDb.Get(id);
            var g = GameRoot.I;
            if (q != null && g != null)
            {
                string msg = stage == 1 ? $"New quest: {q.Title}" : stage >= q.Last ? $"Quest complete: {q.Title}" : $"Quest updated: {q.Title}";
                foreach (var p in g.Players) p.Hud?.Toast(msg, UIKit.Gold);
                if (stage >= q.Last) PayReward(q, by, rewardScale);
            }
            Changed?.Invoke();
        }

        void PayReward(QuestDef q, PlayerSlot by, float scale)
        {
            if (!Paid.Add(q.Id) || q.Reward == null) return;
            var g = GameRoot.I;
            var r = q.Reward;
            var players = g.Players.Where(p => p.IsReady && p.Character != null).ToList();
            foreach (var p in players)
            {
                int cp = (int)(r.Copper * scale), xp = (int)(r.Xp * scale);
                if (cp > 0) g.GiveCopper(p, cp);
                g.GiveXP(p, xp, q.Title);
            }
            var taker = by != null && by.Character != null ? by : players.FirstOrDefault();
            if (r.Item != null && taker != null) g.GiveItem(taker, r.Item);
            if (r.Npc != null && r.Friendship != 0) g.World.AddFriendship(r.Npc, r.Friendship);
            if (r.Flag != null) g.World.SetFlag(r.Flag);
        }

        /// <summary>A world event happened: advance every quest that was waiting for it.</summary>
        public void Notify(QuestEvent e, string id, PlayerSlot by = null)
        {
            foreach (var q in QuestDb.All.Values)
                foreach (var t in q.Triggers)
                    if (t.On == e && Stage(q.Id) == t.FromStage && Matches(t, id))
                    {
                        Set(q.Id, t.ToStage, by);
                        break;
                    }
        }

        static bool Matches(QuestTrigger t, string id)
        {
            if (t.AllOf == null) return t.Id == id;
            var w = GameRoot.I?.World;
            if (w == null || System.Array.IndexOf(t.AllOf, id) < 0) return false;
            return t.AtLeast > 0 ? t.AllOf.Count(w.Flag) >= t.AtLeast : t.AllOf.All(w.Flag);
        }

        /// <summary>After loading: quests already finished (including in saves from before rewards were tracked) count as paid.</summary>
        public void MarkFinishedAsPaid()
        {
            foreach (var kv in Stages)
                if (Done(kv.Key)) Paid.Add(kv.Key);
        }
    }
}
