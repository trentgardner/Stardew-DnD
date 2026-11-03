using System.Collections;
using System.Linq;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: through the grey door. EXEMPT from the secret guard. Nothing here runs until the party opens the door,
    /// and ThroughTheDoor sets WorldDb.MetHer (and saves) before anything is shown. No music, no fanfare: three quiet beats,
    /// the dragon goes to her first, and then she speaks.
    /// </summary>
    public partial class GameRoot
    {
        public void ThroughTheDoor(PlayerSlot by)
        {
            if (Players.Any(p => p.IsReady && p.InCombat)) { by.Hud.Narrate("Not with a fight still going."); return; }
            World.SetFlag(WorldDb.MetHer);
            if (Quests.Stage(WarmCity.Id) < WarmCity.Door) Quests.Set(WarmCity.Id, WarmCity.Door, by);
            SaveSystem.Save(true);
            GatherAt("beyond_the_door", WorldDb.ThresholdSpot);
            if (!World.Flag("meeting_seen")) StartCoroutine(MeetingScene(by));
            else foreach (var p in Players.Where(x => x.IsReady)) p.Hud.Narrate("Edda is sitting on the bed, swinging her feet. She waves as if you'd only been gone a minute.", true);
        }

        IEnumerator MeetingScene(PlayerSlot by)
        {
            yield return null; // let the warp settle
            string name = World.EggHatched ? World.DragonName : "The dragon";
            var beats = new[]
            {
                "The door opens onto a room. Just a room: low and warm, lamplit, no bigger than a cottage kitchen. Charcoal drawings cover the walls: twelve dragons, asleep in a ring, drawn over and over by the same hand, getting better as they go. Along one wall, a row of presents: a wooden horse, a tin soldier, a music box, a doll in a court dress five hundred years out of fashion. One from every century.",
                "Behind the bed, someone is hiding. A little girl, seven or eight, in a nightdress gone grey with age, ash-pale hair, and eyes that catch the lamplight like coals. She's staring at you. She is terrified of you.",
                $"{name} has been afraid of fire since the day it hatched. It walks past all of you, right up to her, folds itself down small, which takes some doing, and puts its great head in her lap. She stares at it. Then, very carefully, she pats it. And she laughs.",
            };
            for (int i = 0; i < beats.Length; i++)
            {
                if (i == 2) yield return DragonGoesToHer();
                var lead = by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady);
                if (lead == null) yield break;
                MessageScreen mine = null;
                foreach (var p in Players.Where(x => x.IsReady))
                {
                    var screen = new MessageScreen("", beats[i]);
                    p.Hud.Push(screen);
                    if (p == lead) mine = screen;
                }
                while (mine != null && !mine.Closed && lead.IsReady) yield return null;
                foreach (var p in Players.Where(x => x.IsReady && x != lead)) p.Hud.CloseAll(); // the other player's copy goes with it
                yield return null;
            }
            World.SetFlag("meeting_seen");
            var her = FindObjectsByType<NpcAgent>(FindObjectsSortMode.None).FirstOrDefault(n => n.Def.Id == "edda");
            var talker = by.IsReady ? by : Players.FirstOrDefault(p => p.IsReady);
            talker?.Hud.Push(new DialogueScreen("edda", her, null));
        }

        IEnumerator DragonGoesToHer()
        {
            var pet = DragonAgent?.GetComponent<DragonPet>();
            var zone = Zones["beyond_the_door"];
            if (pet == null) yield break;
            yield return pet.GoTo(zone.ToWorld(WorldDb.HerSpot + Vector2Int.right), 0.3f);
            pet.StaysPut = true; // it won't leave her side while you're here
            pet.Emote("heart");
        }
    }
}
