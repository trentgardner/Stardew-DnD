using System.Collections.Generic;

namespace DnD.Game
{
    /// <summary>Things that happen in the world that can move a quest along.</summary>
    public enum QuestEvent { ZoneEntered, GroupResolved, ItemGained, FlagSet, SecretFound }

    /// <summary>Stages of the main quest, "fallen_star" (save version 2 numbering).</summary>
    public static class FallenStar { public const int Crater = 1, Trail = 2, Warrens = 3, Egg = 4, Hatched = 5, Done = 6; }

    /// <summary>When event On happens with this Id and the quest is at FromStage, it moves to ToStage.</summary>
    public class QuestTrigger
    {
        public QuestEvent On;
        public string Id;
        public int FromStage, ToStage;
        /// <summary>FlagSet only: fires when ALL of these flags are set (Id is ignored).</summary>
        public string[] AllOf;
        public QuestTrigger(QuestEvent on, string id, int from, int to) { On = on; Id = id; FromStage = from; ToStage = to; }
        public static QuestTrigger AllFlags(int from, int to, params string[] flags) => new QuestTrigger(QuestEvent.FlagSet, null, from, to) { AllOf = flags };
        /// <summary>FlagSet only: with AllOf, fires once at least this many of them are set (0 = all of them).</summary>
        public int AtLeast;
        public static QuestTrigger FlagsAtLeast(int from, int to, int n, params string[] flags) => new QuestTrigger(QuestEvent.FlagSet, null, from, to) { AllOf = flags, AtLeast = n };
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
                    "", "Captain Vale asked you to find where the star fell, in the hills past Whisperwood's north trail.",
                    "The crater is empty. Something carried the star away on small clawed feet, and things keep going missing in town. Follow the tracks, or catch the thief in the act after dark.",
                    "The trail leads into the old copper mine and down, to the kobolds' warrens.",
                    "You found the source: a glowing red egg in the depths of the mine, guarded by devoted kobolds.",
                    "The egg hatched! A baby dragon has chosen you. Return to Captain Vale.",
                    "Complete. Thistledown has a dragon now. The capital will hear of this soon..." },
                Triggers = {
                    new QuestTrigger(QuestEvent.SecretFound, "crater_hollow", FallenStar.Crater, FallenStar.Trail),
                    new QuestTrigger(QuestEvent.SecretFound, "crater_tracks", FallenStar.Trail, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.ZoneEntered, "warrens", FallenStar.Crater, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.ZoneEntered, "warrens", FallenStar.Trail, FallenStar.Warrens),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", 0, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Crater, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Trail, FallenStar.Egg),
                    new QuestTrigger(QuestEvent.GroupResolved, "mine_egg", FallenStar.Warrens, FallenStar.Egg),
                },
                Reward = new QuestReward { Xp = 300, Copper = 5000, Npc = "rhoswen", Friendship = 100 },
            });
            Add(new QuestDef
            {
                Id = "harvest_festival", Title = "The Harvest Festival", Giver = "pip",
                StageText = new[] {
                    "", "Pip says the Harvest Festival is on Day 10 in the square, from nine bells. Try the games!",
                    "Complete. You ate too much pie. Everyone did." },
                Triggers = { QuestTrigger.FlagsAtLeast(1, 2, 3, "fest_bob", "fest_archery", "fest_pie", "fest_stage") },
                Reward = new QuestReward { Xp = 150, Copper = 1000, Npc = "pip", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "watchtower", Title = "The Watchtower Inscription", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale asked you to scout the Old Watchtower on the ridge north of Miller's Ford.",
                    "You read the inscription at the top of the tower. Tell Captain Vale what it says.",
                    "Complete. Captain Vale has copied the words into her notebook. Twice." },
                Triggers = {
                    new QuestTrigger(QuestEvent.SecretFound, "tower_inscription", 0, 2),
                    new QuestTrigger(QuestEvent.SecretFound, "tower_inscription", 1, 2),
                },
                Reward = new QuestReward { Xp = 200, Copper = 2000, Npc = "rhoswen", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "go_between", Title = "Snik Speaks for You", Giver = null,
                StageText = new[] {
                    "", "Snik agreed to speak for you in the kobold warrens below the old mine. Meet him at the village gate.",
                    "Snik talked the gate guards round. Now he needs Elder Ssark's blessing.",
                    "Complete. The Elder blessed you; the way to the Great Egg is open." },
                Triggers = {
                    new QuestTrigger(QuestEvent.GroupResolved, "warrens_gate", 1, 2),
                    new QuestTrigger(QuestEvent.FlagSet, "elder_blessing", 1, 3),
                    new QuestTrigger(QuestEvent.FlagSet, "elder_blessing", 2, 3),
                },
                Reward = new QuestReward { Xp = 150, Item = "lucky_pebble" },
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

            // ------------------------------------------------------------ Phase 4: town quests
            Add(new QuestDef
            {
                Id = "bad_blood", Title = "Bad Blood at the Bridge", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale wants the feud on the South Lane settled before somebody gets stung. Hear out Hesper Meadowsweet and Dunstan Burrow.",
                    "You've heard both sides of the fence. Help them talk it out, or pick a side.",
                    "Complete. The fence on the South Lane is quiet. (For now.)" },
                Reward = new QuestReward { Xp = 100, Copper = 1000 },
            });
            Add(new QuestDef
            {
                Id = "pond_ring", Title = "A Ring in the Pond", Giver = "tamsin",
                StageText = new[] {
                    "", "Tamsin dropped a ring in the town pond. Dive for it (Athletics) or fish for it (Survival) from the pond's edge.",
                    "You found the ring! Bring it back to Tamsin.",
                    "Complete. Tamsin finally asked. Lark said yes before the question was even finished." },
                Reward = new QuestReward { Xp = 100, Item = "copper_trinket", Npc = "lark", Friendship = 100, Flag = "lark_tamsin_engaged" },
            });
            Add(new QuestDef
            {
                Id = "secret_poems", Title = "Brannoc's Secret Poems", Giver = "brannoc",
                StageText = new[] {
                    "", "Brannoc asked you to deliver an anonymous poem to Mother Elowen at the temple. Very anonymous. Extremely anonymous.",
                    "Complete. The poem found its reader." },
                Reward = new QuestReward { Xp = 75 },
            });
            Add(new QuestDef
            {
                Id = "pips_barrel", Title = "Pip's Missing Barrel", Giver = "pip",
                StageText = new[] {
                    "", "A barrel of Hearthfire Stout vanished from behind the Sleeping Drake. Look around the back of the tavern.",
                    "Spilled stout and small boot prints lead east, toward the orchard.",
                    "You found the thief: Wick, a teenager selling the stout to pay for his mum's medicine. Settle it with Pip.",
                    "Complete. The Drake's stout is back where it belongs." },
                Reward = new QuestReward { Xp = 100, Npc = "pip", Friendship = 25 },
            });

            // ------------------------------------------------------------ Phase 4: wilds quests
            Add(new QuestDef
            {
                Id = "goats", Title = "Goats Gone Astray", Giver = "tobin",
                StageText = new[] {
                    "", "Three of Old Tobin's goats ate through the fence and wandered off into Whisperwood. Find them and coax them home (Animal Handling).",
                    "All three goats are home. Tell Tobin.",
                    "Complete. The goats are home, and already eating the new fence." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "goat_1", "goat_2", "goat_3") },
                Reward = new QuestReward { Xp = 75, Item = "goat_bell", Npc = "tobin", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "haunted_mill", Title = "The Haunted Mill", Giver = "hobb",
                StageText = new[] {
                    "", "Something wails around Old Hobb's mill at night. Hobb says it's a ghost. Look around the mill yard after dark.",
                    "It's no ghost: it's a lost owlbear cub, hungry and scared. Calm it down (Animal Handling)... and somewhere, its mother is looking for it.",
                    "The cub trusts you. Carry it back to the owlbear den by the lake in Whisperwood.",
                    "Complete. The mill is quiet, and Hobb is pretending he was never scared." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "forest_owlbear", 2, 4), new QuestTrigger(QuestEvent.GroupResolved, "forest_owlbear", 3, 4) },
                Reward = new QuestReward { Xp = 150, Npc = "hobb", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "wolf_bounty", Title = "Wolves on the Road", Giver = "rhoswen",
                StageText = new[] {
                    "", "Captain Vale's bounty: wolves in Whisperwood keep scaring travelers. Deal with them however you like.",
                    "The wolves won't trouble anyone now. Collect the bounty from Captain Vale.",
                    "Complete. The road through Whisperwood is safe(r)." },
                Triggers = { new QuestTrigger(QuestEvent.GroupResolved, "forest_wolves", 1, 2) },
                Reward = new QuestReward { Xp = 50, Copper = 1500 },
            });
            Add(new QuestDef
            {
                Id = "lost_child", Title = "Lost Child in the Woods", Giver = "maddie",
                StageText = new[] {
                    "", "Maddie hasn't come home! She was last seen heading toward Whisperwood. Find her before dark (8 pm). Look for tracks (Survival or Perception).",
                    "Small boot prints, and a dropped stick sword, lead deeper into Whisperwood.",
                    "Tiny dancing lights... Maddie must be near the fairy ring.",
                    "Complete. Maddie is home safe, full of stories about fairies." },
                Reward = new QuestReward { Xp = 150, Npc = "maddie", Friendship = 200 },
            });
            Add(new QuestDef
            {
                Id = "elowens_herbs", Title = "Mother Elowen's Herbs", Giver = "elowen",
                StageText = new[] {
                    "", "Mother Elowen needs three herbs for her remedies: moonpetal (Whisperwood), riverleaf (Miller's Ford) and hearthroot (the South Lane green).",
                    "You have all three herbs. Bring them to Mother Elowen.",
                    "Complete. The temple's remedy shelf is full again." },
                Triggers = { QuestTrigger.AllFlags(1, 2, "got_herb_moonpetal", "got_herb_riverleaf", "got_herb_hearthroot") },
                Reward = new QuestReward { Xp = 100, Item = "potion_healing", Npc = "elowen", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "bar_brawl", Title = "Bar Brawl at the Drake", Giver = "pip",
                StageText = new[] {
                    "", "Payday at the Sleeping Drake, and the farmhands want a fistfight. Pip's rules: fists only, no magic, nobody dies.",
                    "Complete. Win or lose, the Drake will be talking about it for weeks." },
                Reward = new QuestReward { Xp = 100, Npc = "pip", Friendship = 50 },
            });
            Add(new QuestDef
            {
                Id = "fae_trickster", Title = "The Fae Trickster", Giver = "thimble",
                StageText = new[] {
                    "", "Mister Thimble, a fae with a frog's face and a fox's grin, wants to play a riddle game at the pond in Whisperwood.",
                    "Complete. You played the fae's game. Mister Thimble says the Court is waking up. He wouldn't say what that means." },
                Reward = new QuestReward { Xp = 100 },
            });
        }
    }
}
