using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 7: the ending sequence. EXEMPT from the secret guard: it only runs after WorldDb.MetHer, the choice at
    /// the turn, and the last fight. Scenes (titled beats) → home to Thistledown → the epilogue slides → the credits →
    /// game_complete, saved, and back to the title.
    /// </summary>
    public partial class GameRoot
    {
        /// <summary>The user's dedication, in his words (spelling corrected only). Never cut, never reworded.</summary>
        public const string Dedication = "To my beautiful fiancée Lauren, this game is dedicated. I love you forever.";

        /// <summary>Everything the credits show, in order. The dedication is always last.</summary>
        public static string CreditsText => string.Join("\n", new[]
        {
            "<b>EMBERS OF SUNDERMERE</b>",
            "",
            "Made by Trent, with Claude",
            "",
            "Buoy as himself (a very good boy)",
            "",
            "<b>Rules</b>",
            "This work includes material from the System Reference Document 5.2 (\"SRD 5.2\") by Wizards of the Coast LLC, available at https://www.dndbeyond.com/srd. The SRD 5.2 is licensed under the Creative Commons Attribution 4.0 International License, available at https://creativecommons.org/licenses/by/4.0/legalcode.",
            "This Game is not affiliated with, endorsed, sponsored, or specifically approved by Wizards of the Coast LLC.",
            "",
            "<b>Art, music and sound</b>",
            "The Ninja Adventure Asset Pack by Pixel-boy and AAA (CC0). Thank you.",
            "",
            "<b>Thank you for playing.</b>",
            "",
            Dedication,
        });

        public bool EndingPlaying { get; private set; }

        IEnumerable<string> CompanionNames() =>
            Companions.Where(c => c != null && !c.IsDragon && c.Character != null).Select(c => c.Character.Name).Distinct();

        PlayerSlot Lead => Players.FirstOrDefault(p => p.IsReady);

        /// <summary>The whole ending: scenes, home, the epilogue, the credits. Then the story is told, and saved.</summary>
        public IEnumerator PlayEnding(PlayerSlot by, string ending)
        {
            if (EndingPlaying || ending == null) yield break;
            EndingPlaying = true;
            MusicOverride = "music_home";
            foreach (var scene in EndingDb.Scenes(ending, World, CompanionNames()))
            {
                if (scene.Zone == "town" && !Players.Where(p => p.IsReady).All(p => p.Zone != null && p.Zone.Def.Id == "town"))
                {
                    foreach (var p in Players.Where(p => p.IsReady)) p.Hud.CloseAll();
                    GatherAt("town", WorldDb.TownCarriageStop);
                    var buoy = FindObjectsByType<TownDog>(FindObjectsSortMode.None).FirstOrDefault();
                    if (buoy != null && Zones.TryGetValue("town", out var town)) buoy.Actor.PlaceAt(town, WorldDb.TownCarriageStop + new Vector2Int(1, 4)); // below the beat window, so he is on screen
                    yield return null;
                }
                if (Lead == null) { EndingPlaying = false; yield break; }
                yield return PlayBeats(Lead, scene.Beats, title: scene.Title);
            }
            World.SetFlag(Ending.Seen);
            SaveSystem.Save(true);
            yield return PlaySlides(EndingDb.Slides(ending, World, Clock.Day));
            yield return RollCredits();
            World.SetFlag(Ending.Complete);
            SaveSystem.Save(true);
            EndingPlaying = false;
            QuitToTitle();
        }

        /// <summary>The epilogue: one full-screen slide at a time, on every screen. The lead's press advances; a lead who leaves hands over.</summary>
        public IEnumerator PlaySlides(List<EpilogueSlide> slides)
        {
            for (int i = 0; i < slides.Count; i++)
            {
                var shown = new Dictionary<PlayerSlot, EpilogueScreen>();
                foreach (var p in Players.Where(x => x.IsReady)) { p.Hud.CloseAll(); var s = new EpilogueScreen(slides[i], i, slides.Count); p.Hud.Push(s); shown[p] = s; }
                while (true)
                {
                    var lead = Lead;
                    if (lead == null) yield break;
                    if (!shown.TryGetValue(lead, out var mine) || mine.Closed) break;
                    yield return null;
                }
                foreach (var p in Players.Where(x => x.IsReady)) p.Hud.CloseAll();
                yield return null;
            }
        }

        IEnumerator RollCredits()
        {
            var shown = new Dictionary<PlayerSlot, CreditsScreen>();
            foreach (var p in Players.Where(x => x.IsReady)) { p.Hud.CloseAll(); var s = new CreditsScreen(); p.Hud.Push(s); shown[p] = s; }
            MusicOverride = "music_title";
            while (true)
            {
                var lead = Lead;
                if (lead == null) yield break;
                if (!shown.TryGetValue(lead, out var mine) || mine.Closed) break;
                yield return null;
            }
            foreach (var p in Players.Where(x => x.IsReady)) p.Hud.CloseAll();
            MusicOverride = null;
        }

        IEnumerator PlayEndingSoon(string ending)
        {
            yield return null;
            yield return null;
            yield return PlayEnding(Lead, ending);
        }

        /// <summary>On load, after the last fight: the ending plays from the start, or (once told) a quiet note and free roam.</summary>
        bool ResumeTheEnding()
        {
            if (!World.Flag(Finale.Won)) return false;
            if (World.Flag(Ending.Complete))
            {
                Lead?.Hud.Push(new MessageScreen("The story is told", "Sundermere will remember. You can wander as long as you like, or start a new story from the title."));
                return true;
            }
            StartCoroutine(PlayEndingSoon(Turn.Ending(World)));
            return true;
        }
    }
}
