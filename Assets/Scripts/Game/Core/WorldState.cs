using System.Collections.Generic;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    public enum Difficulty { Story, Normal, Hard, Hardcore }

    public class Settings
    {
        public Difficulty Difficulty = Difficulty.Normal;
        public float DayLengthMinutes = 30;
        public bool NarratorKeyMomentsOnly;
        public static readonly float[] DayLengthOptions = { 20, 30, 45, 60 };
    }

    /// <summary>Everything about the world that changes and must be saved.</summary>
    public class WorldState
    {
        public readonly Dictionary<string, int> Friendship = new Dictionary<string, int>();
        /// <summary>Absolute game minute until which a key is locked (social approaches, once-per-day options).</summary>
        public readonly Dictionary<string, float> Locks = new Dictionary<string, float>();
        public readonly Dictionary<string, int> LastMetDay = new Dictionary<string, int>();
        public readonly HashSet<string> Flags = new HashSet<string>();
        public readonly HashSet<string> OpenedChests = new HashSet<string>();
        public readonly HashSet<string> DefeatedGroups = new HashSet<string>();
        /// <summary>Day each group was defeated; ordinary groups respawn after RespawnDays.</summary>
        public readonly Dictionary<string, int> DefeatedDay = new Dictionary<string, int>();
        public const int RespawnDays = 3;
        /// <summary>One-time XP awards already given (skill checks, discoveries): "player:key".</summary>
        public readonly HashSet<string> XpAwarded = new HashSet<string>();
        public readonly HashSet<string> PacifiedGroups = new HashSet<string>();
        /// <summary>Shop price modifier in percent for a player at a shop, valid on one day: key "p:shop" → (pct, day).</summary>
        public readonly Dictionary<string, Vector2Int> ShopModifiers = new Dictionary<string, Vector2Int>();
        public readonly Dictionary<string, int> Charmed = new Dictionary<string, int>(); // "npc:player" → day
        public Inventory Storage = new Inventory(36);
        public bool EggHatched;
        public string DragonName = "Cinder";
        public bool KettleReturned;
        public int RecruitWeek = -1;
        public int RecruitSeed;

        static float Now => GameRoot.I.Clock.Day * 1440f + GameRoot.I.Clock.Minutes;

        public int Hearts(string npc) => Friendship.TryGetValue(npc, out var f) ? Mathf.Clamp(f / 100, 0, 10) : 0;

        public void AddFriendship(string npc, int amount)
        {
            Friendship.TryGetValue(npc, out var f);
            int before = f / 100;
            f = Mathf.Clamp(f + amount, -500, 1000);
            Friendship[npc] = f;
            int after = f / 100;
            if (after > before && WorldDb.Npcs.TryGetValue(npc, out var def))
                foreach (var p in GameRoot.I.Players) p.Hud?.Toast($"♥ {def.Name} likes you more", UIKit.Bad);
        }

        /// <summary>First chat of the day gives a little friendship, Stardew style.</summary>
        public void MetToday(string npc)
        {
            int day = GameRoot.I.Clock.Day;
            if (LastMetDay.TryGetValue(npc, out var d) && d == day) return;
            LastMetDay[npc] = day;
            AddFriendship(npc, 8);
        }

        public bool IsLocked(string key) => Locks.TryGetValue(key, out var until) && until > Now;
        public void Lock(string key, float minutes) => Locks[key] = Now + minutes;

        public bool Flag(string f) => Flags.Contains(f);
        public void SetFlag(string f) => Flags.Add(f);

        public void SetShopModifier(int player, string shop, int pct, int day) => ShopModifiers[$"{player}:{shop}"] = new Vector2Int(pct, day);

        /// <summary>Total price modifier: today's haggling result plus permanent perks.</summary>
        public int ShopModifier(int player, string shop)
        {
            int pct = 0;
            if (ShopModifiers.TryGetValue($"{player}:{shop}", out var v) && v.y == GameRoot.I.Clock.Day) pct += v.x;
            if (shop == "general" && KettleReturned) pct -= 10;
            return pct;
        }

        public bool CharmedBy(string npc, int player) => Charmed.TryGetValue($"{npc}:{player}", out var d) && d == GameRoot.I.Clock.Day;

        /// <summary>Recruits currently drinking at the Sleeping Drake (a fresh pair each in-game week).</summary>
        public List<CharacterFactory.Recruit> TavernRecruitsHere(GameRoot g) => g.TavernRecruits;
    }

    /// <summary>Quest log. Stages are simple integers with journal text per stage.</summary>
    public class QuestLog
    {
        public readonly Dictionary<string, int> Stages = new Dictionary<string, int>();

        public class QuestDef { public string Id, Title; public string[] StageText; }

        public static readonly Dictionary<string, QuestDef> Defs = new Dictionary<string, QuestDef>
        {
            { "fallen_star", new QuestDef { Id = "fallen_star", Title = "The Fallen Star", StageText = new[] {
                "", "Captain Vale asked you to investigate the old copper mine north of Thistledown, where something fell from the sky.",
                "You found the source: a glowing red egg in the depths of the mine, guarded by devoted kobolds.",
                "The egg hatched! A baby dragon has chosen you. Return to Captain Vale.",
                "Complete. Thistledown has a dragon now. The capital will hear of this soon..." } } },
            { "kettle", new QuestDef { Id = "kettle", Title = "Grandmother's Kettle", StageText = new[] {
                "", "Brannoc's copper kettle was stolen by something with small scaly feet.", "Complete. Brannoc has his kettle back (and you have a permanent discount)." } } },
            { "spoon", new QuestDef { Id = "spoon", Title = "Tobin's Lucky Spoon", StageText = new[] {
                "", "Old Tobin's lucky spoon was stolen by 'a tiny lizard man in a cape.'", "Complete. The soup is lucky again." } } },
            { "east_road", new QuestDef { Id = "east_road", Title = "The Broken Bridge", StageText = new[] {
                "", "The bridge at Miller's Ford is out, and a crew calling themselves the Ford Wardens charges a 'toll' at the ford upstream. Old Hobb the miller is stuck on the far bank. Deal with the Wardens however you like.",
                "The ford is clear. Let Old Hobb know (across the river, by the mill).",
                "Complete. The east road is open and Hobb's mill is turning again." } } },
        };

        public int Stage(string id) => Stages.TryGetValue(id, out var s) ? s : 0;

        public void Start(string id)
        {
            if (Stage(id) > 0) return;
            Set(id, 1);
        }

        public void Set(string id, int stage)
        {
            if (Stage(id) >= stage) return;
            Stages[id] = stage;
            if (Defs.TryGetValue(id, out var q) && GameRoot.I != null)
                foreach (var p in GameRoot.I.Players)
                    p.Hud?.Toast(stage == 1 ? $"New quest: {q.Title}" : stage == q.StageText.Length - 1 ? $"Quest complete: {q.Title}" : $"Quest updated: {q.Title}", UIKit.Gold);
        }

        public bool Done(string id) => Defs.TryGetValue(id, out var q) && Stage(id) >= q.StageText.Length - 1;
    }
}
