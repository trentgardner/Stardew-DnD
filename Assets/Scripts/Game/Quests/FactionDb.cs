using System;
using System.Collections.Generic;
using System.Linq;

namespace DnD.Game
{
    /// <summary>One of the four forces that want the dragon. Each is right about something.</summary>
    public class FactionDef
    {
        public string Id, Name, Belief;
    }

    /// <summary>The four forces (Act II). Their quests carry QuestDef.Faction; the major quest of each is the point of no return.</summary>
    public static class FactionDb
    {
        public static readonly Dictionary<string, FactionDef> All = new Dictionary<string, FactionDef>
        {
            ["crown"] = new FactionDef { Id = "crown", Name = "the Crown", Belief = "A dragon belongs to the realm, and the realm needs protecting." },
            ["academy"] = new FactionDef { Id = "academy", Name = "the Starfall Academy", Belief = "Understand it first. Fear can wait for facts." },
            ["fae"] = new FactionDef { Id = "fae", Name = "the Fae Courts", Belief = "The old Pact was a promise. Promises can be renewed, on the right terms." },
            ["hunters"] = new FactionDef { Id = "hunters", Name = "the Wyrmwatch", Belief = "Every village we've lost to an ember-eyed beast had someone who said 'it's only small'." },
        };

        public static string NameOf(string id) => id != null && All.TryGetValue(id, out var f) ? f.Name : id;
        public static string Title(string id) { var n = NameOf(id); return string.IsNullOrEmpty(n) ? n : char.ToUpper(n[0]) + n.Substring(1); }
    }

    /// <summary>Standing with the four forces, and the forced choice (spec: scout all four, then one major quest closes the others).</summary>
    public static class Factions
    {
        static WorldState W => GameRoot.I.World;

        /// <summary>The last "the party has chosen" message sent to the other players (tests read it).</summary>
        public static string LastNotice;

        public static int Standing(string faction) => W.Standing.TryGetValue(faction, out var s) ? s : 0;

        /// <summary>-100..100. Helping one force usually costs you with another: the content decides by calling this twice.</summary>
        public static void Adjust(string faction, int delta, PlayerSlot by)
        {
            if (!FactionDb.All.ContainsKey(faction) || delta == 0) return;
            W.Standing[faction] = Math.Max(-100, Math.Min(100, Standing(faction) + delta));
            string msg = $"{FactionDb.Title(faction)} {(delta > 0 ? "thinks better of you" : "thinks less of you")}.";
            foreach (var p in GameRoot.I.Players.Where(p => p.IsReady)) p.Hud?.Toast(msg, delta > 0 ? UIKit.Gold : UIKit.Dim);
        }

        /// <summary>Shows the plain-words confirm prompt to whoever is about to commit. Returns the screen (tests click it).</summary>
        public static ChoiceScreen OfferCommit(PlayerSlot by, string faction, Action onYes)
        {
            var others = FactionDb.All.Keys.Where(k => k != faction).Select(FactionDb.NameOf).ToList();
            string body = $"This is the moment you choose a side. Standing with {FactionDb.NameOf(faction)} closes {string.Join(", ", others.Take(others.Count - 1))} and {others.Last()}'s paths for good. Are you sure?";
            var screen = new ChoiceScreen("Choose a side", body, new List<(string, Action)>
            {
                ($"Stand with {FactionDb.NameOf(faction)}", () => { Commit(faction, by); onYes?.Invoke(); }),
                ("Not yet", null),
            });
            by.Hud.Push(screen);
            return screen;
        }

        /// <summary>The party stands with this force: every other force's quests close (in progress or not). Once only.</summary>
        public static void Commit(string faction, PlayerSlot by)
        {
            if (W.CommittedFaction != null || !FactionDb.All.ContainsKey(faction)) return;
            W.CommittedFaction = faction;
            LastNotice = $"{by?.Character?.Name ?? "The party"} has chosen to stand with {FactionDb.NameOf(faction)}.";
            foreach (var p in GameRoot.I.Players.Where(p => p.IsReady))
            {
                if (p == by) p.Hud?.Toast($"You stand with {FactionDb.NameOf(faction)}.", UIKit.Gold);
                else p.Hud?.Narrate(LastNotice);
            }
            SaveSystem.Save(true);
        }
    }
}
