using System.Collections.Generic;

namespace DnD.Game
{
    /// <summary>Things that happen in the world that can move a quest along.</summary>
    public enum QuestEvent { ZoneEntered, GroupResolved, ItemGained, FlagSet, SecretFound }

    /// <summary>When event On happens with this Id and the quest is at FromStage, it moves to ToStage.</summary>
    public class QuestTrigger
    {
        public QuestEvent On;
        public string Id;
        public int FromStage, ToStage;
        public QuestTrigger(QuestEvent on, string id, int from, int to) { On = on; Id = id; FromStage = from; ToStage = to; }
    }

    /// <summary>Paid once when the quest reaches its last stage. Xp and Copper go to every player; Item to whoever finished it.</summary>
    public class QuestReward
    {
        public int Xp, Copper;
        public string Item;
        public string Npc;
        public int Friendship;
        public string Flag;
    }

    public class QuestDef
    {
        public string Id, Title;
        /// <summary>NPC id of whoever hands it out (shown in the journal), or null.</summary>
        public string Giver;
        /// <summary>Journal text per stage. Stage 0 = not started (""); the last stage = complete.</summary>
        public string[] StageText;
        public List<QuestTrigger> Triggers = new List<QuestTrigger>();
        public QuestReward Reward;
        public int Last => StageText.Length - 1;
    }

    /// <summary>
    /// Every quest. Talk-driven stages are set from DialogueDb (Quests.Set); world-driven stages use Triggers.
    /// Stage numbers of existing quests must never change meaning (they're in players' saves).
    /// </summary>
    public static class QuestDb
    {
        public static readonly Dictionary<string, QuestDef> All = new Dictionary<string, QuestDef>();

        public static QuestDef Get(string id) => id != null && All.TryGetValue(id, out var q) ? q : null;

        static QuestDef Add(QuestDef q) { All[q.Id] = q; return q; }

        static QuestDb()
        {
            Add(new QuestDef
            {
                Id = "fallen_star", Title = "The Fallen Star", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale asked you to investigate the old copper mine north of Thistledown, where something fell from the sky.",
                    "You found the source: a glowing red egg in the depths of the mine, guarded by devoted kobolds.",
                    "The egg hatched! A baby dragon has chosen you. Return to Captain Vale.",
                    "Complete. Thistledown has a dragon now. The capital will hear of this soon..." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", 1, 2), new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", 0, 2) },
                Reward = new QuestReward { Xp = 300, Copper = 5000, Npc = "rhoswen", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "kettle", Title = "Grandmother's Kettle", Giver = "brannoc",
                StageText = new[] {
                    "", "Brannoc's copper kettle was stolen by something with small scaly feet.",
                    "Complete. Brannoc has his kettle back (and you have a permanent discount)." },
                Triggers = { new QuestTrigger(QuestEvent.ItemGained, "copper_kettle", 0, 1) },
                Reward = new QuestReward { Xp = 100, Npc = "brannoc", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "spoon", Title = "Tobin's Lucky Spoon", Giver = "tobin",
                StageText = new[] {
                    "", "Old Tobin's lucky spoon was stolen by 'a tiny lizard man in a cape.'",
                    "Complete. The soup is lucky again." },
                Triggers = { new QuestTrigger(QuestEvent.ItemGained, "lucky_spoon", 0, 1) },
                Reward = new QuestReward { Xp = 75, Item = "potion_healing", Npc = "tobin", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "east_road", Title = "The Broken Bridge", Giver = "hobb",
                StageText = new[] {
                    "", "The bridge at Miller's Ford is out, and a crew calling themselves the Ford Wardens charges a 'toll' at the ford upstream. Old Hobb the miller is stuck on the far bank. Deal with the Wardens however you like.",
                    "The ford is clear. Let Old Hobb know (across the river, by the mill).",
                    "Complete. The east road is open and Hobb's mill is turning again." },
                Triggers = { new QuestTrigger(QuestEvent.ZoneEntered, "ford", 0, 1), new QuestTrigger(QuestEvent.GroupResolved, "ford_toll", 1, 2) },
                Reward = new QuestReward { Xp = 150, Copper = 2500, Item = "potion_greater_healing", Npc = "hobb", Friendship = 100 },
            });
        }
    }
}
