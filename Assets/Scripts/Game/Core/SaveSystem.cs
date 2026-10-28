using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DnD.Rules;
using DnD.Rules.Content;
using UnityEngine;

namespace DnD.Game
{
    // ------------------------------------------------------------------ save file format (JsonUtility)

    [Serializable] public class KV { public string K; public int V; public KV(string k, int v) { K = k; V = v; } }
    [Serializable] public class KF { public string K; public float V; public KF(string k, float v) { K = k; V = v; } }

    [Serializable]
    public class PcData
    {
        public string Name, Species, Class, Background, SpriteId, FightingStyle, DivineOrder;
        public int Level, XP, Copper, HP, MaxHP, TempHP, Exhaustion, HitDiceSpent, DeathS, DeathF, HeldInsp;
        public bool Dead, Stable, Shield, Heroic, LuckyUsed;
        public int[] BaseScores, BgBonus, SlotsUsed;
        public List<string> Skills = new List<string>(), Expertise = new List<string>(), Mastered = new List<string>(), Worn = new List<string>();
        public List<string> Cantrips = new List<string>(), Prepared = new List<string>(), Spellbook = new List<string>();
        public List<string> BagIds = new List<string>();
        public List<int> BagCounts = new List<int>();
        public List<KV> Resources = new List<KV>();
        public string MainHand, Armor;
        // where they are
        public string Zone;
        public int X, Y;
        public float Torch, LightSpell;
    }

    [Serializable]
    public class CompanionData
    {
        public int Leader; public PcData Pc; public string Quirk;
        public bool AutoCombat; public int Stance; // old saves load as: you command them, default role
        public bool TakePoint;
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public string PlaythroughId, PlaythroughName;
        public string SavedAt;
        public bool IsAuto;
        public int Difficulty;
        public float DayLength;
        public bool NarratorKeyOnly;
        public int Day;
        public float Minutes;
        public List<PcData> Players = new List<PcData>();
        public List<CompanionData> Companions = new List<CompanionData>();
        public int DragonLeader = -1;
        // world
        public List<KV> Friendship = new List<KV>(), LastMet = new List<KV>(), Quests = new List<KV>(), DefeatedDay = new List<KV>();
        public List<KF> Locks = new List<KF>();
        public List<string> Flags = new List<string>(), Opened = new List<string>(), Defeated = new List<string>(), Pacified = new List<string>(), XpAwarded = new List<string>();
        public List<string> ShopModKeys = new List<string>();
        public List<int> ShopModPct = new List<int>(), ShopModDay = new List<int>();
        public List<string> StorageIds = new List<string>();
        public List<int> StorageCounts = new List<int>();
        public bool EggHatched, KettleReturned;
        public string DragonName;
        public int RecruitWeek, RecruitSeed;
    }

    public class SaveInfo
    {
        public string Path, PlaythroughId, PlaythroughName, Summary;
        public DateTime When;
        public bool IsAuto;
    }

    /// <summary>
    /// Saves live in {persistentDataPath}/saves/{playthroughId}/. Each playthrough has one autosave
    /// (autosave.json, overwritten) and any number of manual saves (save_yyyyMMdd_HHmmss.json).
    /// Hardcore playthroughs use a single hardcore.json written only when sleeping.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>
        /// Saves go to OneDrive (Documents/Embers of Sundermere/saves) when OneDrive exists, so the desktop and
        /// the laptop share playthroughs. Otherwise they live in Unity's per-user data folder.
        /// </summary>
        /// <summary>Tests point this at a temp folder so they never touch real saves.</summary>
        public static string OverrideRoot;

        public static string Root
        {
            get
            {
                if (OverrideRoot != null) return OverrideRoot;
                string od = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(od) && Directory.Exists(od))
                    return System.IO.Path.Combine(od, "Documents", "Embers of Sundermere", "saves");
                return System.IO.Path.Combine(Application.persistentDataPath, "saves");
            }
        }

        public static string Save(bool auto)
        {
            var g = GameRoot.I;
            if (g == null || !g.InGame || g.Players.Count == 0 || g.AnyInCombat) return null;
            bool hardcore = g.Settings.Difficulty == Difficulty.Hardcore;
            var data = Capture(g, auto);
            string dir = System.IO.Path.Combine(Root, g.PlaythroughId);
            Directory.CreateDirectory(dir);
            string file = hardcore ? "hardcore.json" : auto ? "autosave.json" : $"save_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = System.IO.Path.Combine(dir, file);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            return path;
        }

        public static List<SaveInfo> List()
        {
            var list = new List<SaveInfo>();
            if (!Directory.Exists(Root)) return list;
            foreach (var dir in Directory.GetDirectories(Root))
                foreach (var f in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(f));
                        if (d == null) continue;
                        DateTime.TryParse(d.SavedAt, out var when);
                        string who = string.Join(" & ", d.Players.Select(p => $"{p.Name} (Lv{p.Level} {p.Class})"));
                        string where = WorldDb.Zones.TryGetValue(d.Players.FirstOrDefault()?.Zone ?? "", out var z) ? z.Name : "?";
                        list.Add(new SaveInfo
                        {
                            Path = f, PlaythroughId = d.PlaythroughId, PlaythroughName = d.PlaythroughName, IsAuto = d.IsAuto, When = when,
                            Summary = $"{(d.IsAuto ? "[Auto] " : "")}{who} · Day {d.Day} · {where} · {when:MMM d, h:mm tt}",
                        });
                    }
                    catch (Exception e) { Debug.LogWarning($"Bad save {f}: {e.Message}"); }
                }
            return list.OrderByDescending(s => s.When).ToList();
        }

        public static SaveData Read(string path)
        {
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)); }
            catch (Exception e) { Debug.LogError($"Couldn't read save {path}: {e.Message}"); return null; }
        }

        public static void DeletePlaythrough(string id)
        {
            string dir = System.IO.Path.Combine(Root, id);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        // ------------------------------------------------------------------ capture

        static SaveData Capture(GameRoot g, bool auto)
        {
            var d = new SaveData
            {
                PlaythroughId = g.PlaythroughId, PlaythroughName = g.PlaythroughName, SavedAt = DateTime.Now.ToString("o"), IsAuto = auto,
                Difficulty = (int)g.Settings.Difficulty, DayLength = g.Settings.DayLengthMinutes, NarratorKeyOnly = g.Settings.NarratorKeyMomentsOnly,
                Day = g.Clock.Day, Minutes = g.Clock.Minutes,
            };
            foreach (var p in g.Players)
            {
                if (!p.IsReady) continue;
                var pd = ToData(p.Character);
                pd.Zone = p.Zone.Def.Id; pd.X = p.MapPos.x; pd.Y = p.MapPos.y;
                pd.Torch = p.Avatar.TorchMinutes; pd.LightSpell = p.Avatar.LightSpellMinutes;
                d.Players.Add(pd);
            }
            d.Players.AddRange(g.PendingPlayers); // partners who stepped away are still part of this playthrough
            foreach (var c in g.Companions)
            {
                if (c.IsDragon) { d.DragonLeader = c.Leader.Index; continue; }
                d.Companions.Add(new CompanionData { Leader = c.Leader.Index, Pc = ToData(c.Character), Quirk = c.Quirk, AutoCombat = !c.Manual, Stance = (int)c.Stance, TakePoint = c.TakePoint });
            }
            var w = g.World;
            foreach (var kv in w.Friendship) d.Friendship.Add(new KV(kv.Key, kv.Value));
            foreach (var kv in w.LastMetDay) d.LastMet.Add(new KV(kv.Key, kv.Value));
            foreach (var kv in g.Quests.Stages) d.Quests.Add(new KV(kv.Key, kv.Value));
            foreach (var kv in w.DefeatedDay) d.DefeatedDay.Add(new KV(kv.Key, kv.Value));
            foreach (var kv in w.Locks) d.Locks.Add(new KF(kv.Key, kv.Value));
            d.Flags.AddRange(w.Flags); d.Opened.AddRange(w.OpenedChests); d.Defeated.AddRange(w.DefeatedGroups); d.Pacified.AddRange(w.PacifiedGroups); d.XpAwarded.AddRange(w.XpAwarded);
            foreach (var kv in w.ShopModifiers) { d.ShopModKeys.Add(kv.Key); d.ShopModPct.Add(kv.Value.x); d.ShopModDay.Add(kv.Value.y); }
            foreach (var s in w.Storage.Slots) { d.StorageIds.Add(s?.ItemId ?? ""); d.StorageCounts.Add(s?.Count ?? 0); }
            d.EggHatched = w.EggHatched; d.KettleReturned = w.KettleReturned; d.DragonName = w.DragonName;
            d.RecruitWeek = w.RecruitWeek; d.RecruitSeed = w.RecruitSeed;
            return d;
        }

        public static PcData ToData(PlayerCharacter pc)
        {
            var d = new PcData
            {
                Name = pc.Name, Species = pc.Species.Id, Class = pc.Class.Id, Background = pc.Background.Id, SpriteId = pc.SpriteId,
                FightingStyle = pc.FightingStyle, DivineOrder = pc.DivineOrder, Level = pc.Level, XP = pc.XP, Copper = pc.Copper,
                HP = pc.HP, MaxHP = pc.MaxHP, TempHP = pc.TempHP, Exhaustion = pc.Exhaustion, HitDiceSpent = pc.HitDiceSpent,
                DeathS = pc.DeathSuccesses, DeathF = pc.DeathFailures, HeldInsp = pc.HeldInspirationDie, Dead = pc.IsDead, Stable = pc.Stable,
                Shield = pc.Shield, Heroic = pc.HeroicInspiration, LuckyUsed = pc.LuckyRerollUsed,
                BaseScores = (int[])pc.BaseScores.Clone(), BgBonus = (int[])pc.BackgroundBonus.Clone(), SlotsUsed = (int[])pc.SlotsUsed.Clone(),
                MainHand = pc.MainHand?.Id, Armor = pc.Armor?.Id,
            };
            d.Skills.AddRange(pc.SkillProficiencies.Select(s => s.ToString()));
            d.Expertise.AddRange(pc.Expertise.Select(s => s.ToString()));
            d.Mastered.AddRange(pc.MasteredWeapons);
            d.Worn.AddRange(pc.Worn.Select(w => w.Id));
            d.Cantrips.AddRange(pc.Cantrips); d.Prepared.AddRange(pc.PreparedSpells); d.Spellbook.AddRange(pc.Spellbook);
            foreach (var s in pc.Bag.Slots) { d.BagIds.Add(s?.ItemId ?? ""); d.BagCounts.Add(s?.Count ?? 0); }
            foreach (var kv in pc.ResourcesUsed) d.Resources.Add(new KV(kv.Key, kv.Value));
            return d;
        }

        public static PlayerCharacter FromData(PcData d)
        {
            var pc = new PlayerCharacter
            {
                Name = d.Name, Species = CharacterDb.Species[d.Species], Class = CharacterDb.Classes[d.Class], Background = CharacterDb.Backgrounds[d.Background],
                SpriteId = d.SpriteId, FightingStyle = d.FightingStyle, DivineOrder = d.DivineOrder, Level = d.Level, XP = d.XP, Copper = d.Copper,
                MaxHP = d.MaxHP, HP = d.HP, TempHP = d.TempHP, Exhaustion = d.Exhaustion, HitDiceSpent = d.HitDiceSpent,
                DeathSuccesses = d.DeathS, DeathFailures = d.DeathF, HeldInspirationDie = d.HeldInsp, Stable = d.Stable,
                Shield = d.Shield, HeroicInspiration = d.Heroic, LuckyRerollUsed = d.LuckyUsed,
                MainHand = d.MainHand != null ? ItemsDb.Get<WeaponDef>(d.MainHand) : null,
                Armor = d.Armor != null ? ItemsDb.Get<ArmorDef>(d.Armor) : null,
            };
            Array.Copy(d.BaseScores, pc.BaseScores, 6);
            Array.Copy(d.BaseScores, pc.Scores, 6);
            Array.Copy(d.BgBonus, pc.BackgroundBonus, 6);
            if (d.SlotsUsed != null && d.SlotsUsed.Length == 10) pc.SlotsUsed = (int[])d.SlotsUsed.Clone();
            foreach (var s in d.Skills) if (Enum.TryParse<Skill>(s, out var sk)) pc.SkillProficiencies.Add(sk);
            foreach (var s in d.Expertise) if (Enum.TryParse<Skill>(s, out var sk)) pc.Expertise.Add(sk);
            pc.MasteredWeapons.AddRange(d.Mastered);
            foreach (var w in d.Worn) { var wd = ItemsDb.Get<WondrousDef>(w); if (wd != null) pc.Wear(wd); }
            pc.Cantrips.AddRange(d.Cantrips); pc.PreparedSpells.AddRange(d.Prepared); pc.Spellbook.AddRange(d.Spellbook);
            for (int i = 0; i < d.BagIds.Count && i < pc.Bag.Size; i++)
                if (!string.IsNullOrEmpty(d.BagIds[i]) && ItemsDb.Get(d.BagIds[i]) != null) pc.Bag.Slots[i] = new ItemStack(d.BagIds[i], d.BagCounts[i]);
            foreach (var kv in d.Resources) pc.ResourcesUsed[kv.K] = kv.V;
            if (d.Dead) pc.MarkDead();
            return pc;
        }

        public static void ApplyWorld(SaveData d, GameRoot g)
        {
            var w = g.World;
            foreach (var kv in d.Friendship) w.Friendship[kv.K] = kv.V;
            foreach (var kv in d.LastMet) w.LastMetDay[kv.K] = kv.V;
            foreach (var kv in d.Quests) g.Quests.Stages[kv.K] = kv.V;
            foreach (var kv in d.DefeatedDay) w.DefeatedDay[kv.K] = kv.V;
            foreach (var kv in d.Locks) w.Locks[kv.K] = kv.V;
            foreach (var f in d.Flags) w.Flags.Add(f);
            foreach (var f in d.Opened) w.OpenedChests.Add(f);
            foreach (var f in d.Defeated) w.DefeatedGroups.Add(f);
            foreach (var f in d.Pacified) w.PacifiedGroups.Add(f);
            foreach (var f in d.XpAwarded) w.XpAwarded.Add(f);
            for (int i = 0; i < d.ShopModKeys.Count; i++) w.ShopModifiers[d.ShopModKeys[i]] = new Vector2Int(d.ShopModPct[i], d.ShopModDay[i]);
            for (int i = 0; i < d.StorageIds.Count && i < w.Storage.Size; i++)
                if (!string.IsNullOrEmpty(d.StorageIds[i])) w.Storage.Slots[i] = new ItemStack(d.StorageIds[i], d.StorageCounts[i]);
            w.EggHatched = d.EggHatched; w.KettleReturned = d.KettleReturned; w.DragonName = string.IsNullOrEmpty(d.DragonName) ? "Cinder" : d.DragonName;
            w.RecruitWeek = d.RecruitWeek; w.RecruitSeed = d.RecruitSeed;
        }
    }
}
