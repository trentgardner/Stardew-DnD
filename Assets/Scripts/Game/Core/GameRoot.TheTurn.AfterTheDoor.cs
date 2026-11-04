using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 5: the turn. EXEMPT from the secret guard. It begins only once WorldDb.MetHer and meeting_seen are set and
    /// the party comes back up into the capital: the whole city waits at the Sunstair, the King is made to confess, every force
    /// has its say, and the party chooses one of three endings. Then the break: who stands with you, who doesn't.
    /// Phase 6 picks up from the break (the fights).
    /// </summary>
    public partial class GameRoot
    {
        bool _turnPending;

        /// <summary>Called on every zone entry and after a load: starts the questioning when the party first comes back up.</summary>
        public void CheckTheTurn()
        {
            if (!InGame || World == null || _turnPending) return;
            int st = Quests.Stage(WarmCity.Id);
            if ((st != WarmCity.Door && st != WarmCity.Called) || !World.Flag(WorldDb.MetHer) || !World.Flag("meeting_seen") || World.Flag(Turn.Questioning)) return;
            var p = Players.FirstOrDefault(x => x.IsReady && x.Avatar != null && x.Zone != null && x.Zone.Def.Id == "capital");
            if (p == null) return;
            _turnPending = true;
            StartCoroutine(BeginQuestioningSoon(p));
        }

        IEnumerator BeginQuestioningSoon(PlayerSlot p)
        {
            yield return null; // never warp anyone from inside a warp
            _turnPending = false;
            if (p.IsReady && !World.Flag(Turn.Questioning)) BeginQuestioning(p);
        }

        public void BeginQuestioning(PlayerSlot by)
        {
            if (Players.Any(p => p.IsReady && p.InCombat)) { by.Hud.Narrate("Not with a fight still going."); return; }
            if (!World.Flag(WorldDb.MetHer)) return;
            if (Quests.Stage(WarmCity.Id) < WarmCity.Called) Quests.Set(WarmCity.Id, WarmCity.Called, by);
            World.SetFlag(Turn.Questioning);
            SaveSystem.Save(true);
            GatherAt("capital", WorldDb.SunstairSpot);
            if (DragonAgent != null && DragonAgent.Actor.Zone == Zones["capital"])
            {
                DragonAgent.Actor.PlaceAt(Zones["capital"], WorldDb.SunstairDragonSpot);
                var pet = DragonAgent.GetComponent<DragonPet>();
                if (pet != null) pet.StaysPut = true; // it holds its ground beside you until you leave the square
            }
            StartCoroutine(QuestioningScene(by));
        }

        IEnumerator QuestioningScene(PlayerSlot by)
        {
            yield return null; // let the warp settle
            if (!World.Flag(Turn.BeatsSeen))
            {
                string name = World.EggHatched ? World.DragonName : "The dragon";
                yield return PlayBeats(by, new[]
                {
                    "The stair from below lets out into sunlight and noise. The whole Great Square is full: guardsmen in red and gold, Wyrmwatch hunters on the rooftops, Academy robes, a drift of Fae flowers along the fountain's rim. Every face turns to you.",
                    $"King Aldric stands at the top of the Sunstair, with Marsh at his shoulder, and Ashby, Sorrel and Calloway below. Nobody has drawn a weapon. Everyone is ready to. {name} plants itself at your back and looks up at the King, and keeps looking.",
                });
                World.SetFlag(Turn.BeatsSeen);
            }
            OpenQuestioning(by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady));
        }

        /// <summary>Opens dialogue "questioning" (with the King as the one you face) for this player. The dialogue picks its own node.</summary>
        public void OpenQuestioning(PlayerSlot p)
        {
            if (p == null || World.Flag(Turn.Chosen)) return;
            foreach (var n in _npcs) n.UpdateSchedule(false);
            var king = _npcs.FirstOrDefault(n => n.Def.Id == "king_q");
            p.Hud.Push(new DialogueScreen("questioning", king, null));
        }

        /// <summary>Co-op: the other players see what's said at the questioning in their log.</summary>
        public void EchoToOthers(PlayerSlot speaker, string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            foreach (var p in Players.Where(x => x.IsReady && x != speaker)) p.Hud.Narrate(line);
        }

        // ------------------------------------------------------------------ the choice

        readonly HashSet<int> _endingYes = new HashSet<int>();
        string _endingOffered;

        public static string EndingLabel(string ending) => ending == Turn.End ? "End her" : ending == Turn.Seal ? "Seal her away" : "Save her";

        static string EndingBody(string ending) =>
            ending == Turn.End ? "Her fire will come out of her as what the stories say, and you'll have to put it down. There's no undoing this."
            : ending == Turn.Seal ? "Ashby's way: she sleeps, and the door is closed for good. She won't understand why. There's no undoing this."
            : "Draw the fire out of her and set her free. The Crown will come for that fire with everything it has. There's no undoing this.";

        /// <summary>The confirm, for every ready player. The choice is made only when everyone says yes; one "Not yet" cancels it.</summary>
        public void OfferEnding(PlayerSlot by, string ending)
        {
            if (World.Flag(Turn.Chosen) || !Turn.Endings.Contains(ending)) return;
            if (ending == Turn.Save && TurnDb.SaveGateMissing(World).Count > 0) return;
            _endingYes.Clear();
            _endingOffered = ending;
            StartCoroutine(OfferSoon(by, ending)); // a frame later, so the dialogue that asked can finish closing
        }

        IEnumerator OfferSoon(PlayerSlot by, string ending)
        {
            yield return null;
            if (_endingOffered != ending || World.Flag(Turn.Chosen)) yield break;
            _endingOfferedBy = by;
            foreach (var p in Players.Where(x => x.IsReady).ToList()) PushEndingConfirm(p, by, ending);
        }

        PlayerSlot _endingOfferedBy;

        /// <summary>Quitting to the title forgets any ending confirm that was waiting for answers.</summary>
        void ResetTurnOffer() { _endingOffered = null; _endingOfferedBy = null; _endingYes.Clear(); }

        void PushEndingConfirm(PlayerSlot p, PlayerSlot by, string ending)
        {
            var me = p;
            p.Hud.CloseAll();
            var screen = new ChoiceScreen(EndingLabel(ending), EndingBody(ending), new List<(string, System.Action)>
            {
                ("Yes. " + EndingLabel(ending) + ".", () => AgreeToEnding(me, by, ending)),
                ("Not yet", () => CancelEnding(by)),
            });
            screen.OnCancel = () => CancelEnding(by);
            p.Hud.Push(screen);
        }

        /// <summary>A player joins while the party is being asked something everyone must answer: they're asked too.</summary>
        void OnPlayerJoinedMidScene(PlayerSlot p)
        {
            if (World == null || !p.IsReady) return;
            if (_endingOffered != null && World.Flag(Turn.Questioning) && !World.Flag(Turn.Chosen)) { PushEndingConfirm(p, _endingOfferedBy ?? p, _endingOffered); return; }
            if (World.Flag(Finale.RiderPending) && !Flying) OfferRider();
        }

        void AgreeToEnding(PlayerSlot who, PlayerSlot by, string ending)
        {
            if (_endingOffered != ending) return;
            _endingYes.Add(who.Index);
            var ready = Players.Where(x => x.IsReady).ToList();
            if (ready.All(p => _endingYes.Contains(p.Index))) { _endingOffered = null; ChooseEnding(by, ending); return; }
            var waitingOn = ready.Where(p => !_endingYes.Contains(p.Index)).Select(p => p.Character?.Name ?? "your partner");
            who.Hud.Push(new MessageScreen("", $"Waiting for {string.Join(" and ", waitingOn)}..."));
        }

        void CancelEnding(PlayerSlot by)
        {
            if (_endingOffered == null) return;
            _endingOffered = null;
            _endingYes.Clear();
            StartCoroutine(BackToTheQuestion(by));
        }

        IEnumerator BackToTheQuestion(PlayerSlot by)
        {
            yield return null;
            foreach (var p in Players.Where(x => x.IsReady)) p.Hud.CloseAll();
            OpenQuestioning(by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady));
        }

        /// <summary>Atomic: the flags, the consequences, the save, and only then the break. Once only.</summary>
        public void ChooseEnding(PlayerSlot by, string ending)
        {
            if (World.Flag(Turn.Chosen) || !World.Flag(WorldDb.MetHer) || !Turn.Endings.Contains(ending)) return;
            World.SetFlag(Turn.EndingFlag(ending));
            World.SetFlag(Turn.Chosen);
            if (TurnDb.Betrays(ending, World))
            {
                World.SetFlag(Turn.Betrayed);
                Factions.Adjust(World.CommittedFaction, -50, by);
            }
            int bond = ending == Turn.End ? -10 : ending == Turn.Save ? 10 : 0;
            if (bond != 0) DragonPet.AddBond(by, bond, ending == Turn.End ? "you chose to end her" : "you chose to save her");
            if (Quests.Stage(WarmCity.Id) < WarmCity.Break) Quests.Set(WarmCity.Id, WarmCity.Break, by);
            foreach (var p in Players.Where(x => x.IsReady)) p.Hud.CloseAll();
            foreach (var n in _npcs) n.UpdateSchedule(false);
            SaveSystem.Save(true);
            StartCoroutine(TheBreak(by, ending));
        }

        /// <summary>The three beats of the break (the ending's opening, who stands where, the dragon), then Phase 6's placeholder.</summary>
        public static string[] BreakBeats(string ending, WorldState w)
        {
            string name = w.EggHatched ? w.DragonName : "The dragon";
            string open = ending == Turn.End
                ? "The King closes his eyes. \"Then let it end. Let it finally be over.\" Far below the square, something enormous stirs, and every lamp in Aurelhold goes out at once."
                : ending == Turn.Seal
                ? "Ashby lets out a breath he seems to have been holding for forty years. \"I'll need the Sealing Hall's work, and time, and you between me and anyone who wants her fire.\" The King's face goes very still."
                : "For a moment nobody moves. Then the King says, quite softly, \"No.\" And for the first time, he sounds like a king.";
            string dragon = ending == Turn.End
                ? $"{name} looks at you. It doesn't come when you call. It goes and stands at the top of the stair down, between you and it, and lies down across the way, and won't move."
                : ending == Turn.Seal
                ? $"{name} makes a sound you've never heard it make. Then it comes and stands beside you anyway."
                : $"{name} throws back its head and roars, and the sound rolls off the mountains all the way round the ring.";
            return new[] { open, TurnDb.BreakSummary(ending, w), dragon };
        }

        internal IEnumerator TheBreak(PlayerSlot by, string ending)
        {
            yield return null;
            yield return PlayBeats(by, BreakBeats(ending, World));
            BeginFinale(by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady) ?? by, ending); // Phase 6: the fight for Aurelhold
        }
    }
}
