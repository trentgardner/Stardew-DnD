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
        /// <summary>The day the party finished the Harvest Festival quest (0 = not yet): the festival stops coming back after that.</summary>
        public int FestivalDoneDay;
        public string DragonName = "Cinder";
        /// <summary>The dragon's damage taken (0 = full HP; the max comes from the party's level). Saved, so old saves load at full health.</summary>
        public int DragonDamage;
        /// <summary>Dropped to 0 HP in a fight: it hides behind the party until the next rest and sits out fights.</summary>
        public bool DragonScared;
        /// <summary>0–100, raised by petting, play, treats and fights won together. No fail state.</summary>
        public int DragonBond;
        /// <summary>Sniff is once per short rest, shared by the whole party.</summary>
        public bool DragonSniffUsed;
        /// <summary>Braziers the dragon has lit: "zoneId:x,y". They stay lit.</summary>
        public readonly HashSet<string> LitBraziers = new HashSet<string>();
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
        public void SetFlag(string f)
        {
            if (!Flags.Add(f) || GameRoot.I == null) return;
            GameRoot.I.Quests.Notify(QuestEvent.FlagSet, f);
            GameRoot.I.RefreshNpcs();
        }

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
}
