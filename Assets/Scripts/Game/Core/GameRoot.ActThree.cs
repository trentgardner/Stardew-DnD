using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Moving from Act II into Act III: two weeks after the Warden, a grown dragon and a royal summons.</summary>
    public partial class GameRoot
    {
        public const float GrownDragonScale = 1.8f;
        /// <summary>Set while the King's carriage (and Quell) stands in the capital rather than in Thistledown.</summary>
        public const string CarriageInCapital = "quell_in_capital";

        /// <summary>
        /// The King's carriage: every ready player (and their companions and the dragon) rides to `zone`, set down side by side
        /// from `pos`. Screens close first; the ride happens a frame later so a dialogue can finish closing.
        /// </summary>
        public void RideCarriage(PlayerSlot by, string zone, Vector2Int pos)
        {
            if (Players.Any(p => p.IsReady && p.InCombat))
            {
                by.Hud.Narrate("Quell won't leave anyone behind mid-fight. \"When everyone's here and nobody's bleeding, we'll go.\"");
                return;
            }
            foreach (var p in Players.Where(p => p.IsReady)) p.Hud.CloseAll();
            StartCoroutine(Ride(zone, pos));
        }

        System.Collections.IEnumerator Ride(string zone, Vector2Int pos)
        {
            yield return null;
            bool first = !World.Flag("carriage_ridden");
            World.SetFlag("carriage_ridden");
            if (zone == "capital") World.SetFlag(CarriageInCapital); else World.ClearFlag(CarriageInCapital);
            int i = 0;
            foreach (var p in Players.Where(p => p.IsReady && p.Avatar != null).ToList())
            {
                p.Hud.CloseAll();
                Warp(p, zone, pos + new Vector2Int(i++, 0), false);
                p.Hud.Narrate(zone == "capital"
                    ? first
                        ? "Two days up the King's Road, through passes white with early snow. Then the carriage tops a ridge and the whole party goes quiet. A city fills the valley inside the mountains' ring, and every window in it is lit."
                        : "Two days up the King's Road, the snow deeper every mile, until Aurelhold glows up out of the valley like a hearth."
                    : "Two days down the King's Road, and the warmth goes out of the air a mile at a time. Thistledown looks small and grey and very dear.", true);
            }
        }

        /// <summary>
        /// Two weeks after the Warden falls: the party is rested, the cap is 13, companions catch up, the dragon is grown,
        /// and a letter under a royal seal invites the heroes of the Emberhorn to the capital. Only once the Warden is down.
        /// </summary>
        public void BeginActThree(PlayerSlot by)
        {
            if (World.Act != 2 || !World.Flag(WardenFallen)) return;
            World.Act = 3;
            Clock.Day += 14;
            Clock.Minutes = 8 * 60;
            World.ActThreeDay = Clock.Day;
            GrowUp();
            foreach (var p in Players.Where(p => p.Character != null)) p.Character.LongRest(true, null);
            foreach (var c in Companions.Where(c => c != null && c.Character != null)) c.Character.LongRest(true, null);
            ApplyActCap();
            string name = World.EggHatched ? World.DragonName : "the dragon";
            foreach (var p in Players.Where(p => p.IsReady))
            {
                p.Hud.CloseAll();
                p.Hud.Push(new MessageScreen("Two Weeks Later",
                    $"Winter comes early to the Wyrmback. The first snow settles on the Emberhorn, and it doesn't melt. " +
                    $"{name} has grown again: shoulders like a barn door, wings that block the sun, and a voice that rattles the shutters. " +
                    "Big enough to carry a rider, though nobody has quite dared to ask.\n\n" +
                    "Then a letter comes up the road under a royal seal of red and gold. The King himself would be honoured to receive " +
                    "the heroes of the Emberhorn, in the capital, at their earliest convenience. The courier waits for an answer.",
                    () => p.Hud.Narrate("Act III begins. (You can now reach level 13: open your character sheet when you level up.)")));
            }
            SaveSystem.Save(true);
        }

        /// <summary>The dragon is grown (Act III): full HP, its new size and look.</summary>
        void GrowUp()
        {
            World.DragonStage = (int)DragonStage.Young;
            if (Dragon != null) { Dragon.Stage = DragonStage.Young; Dragon.SetPartyLevel(PartyLevel); Dragon.HP = Dragon.MaxHP; }
            World.DragonDamage = 0; World.DragonScared = false;
            DragonGrew();
        }

        /// <summary>
        /// A new game that starts at Act III (level 11 preview): Acts I and II count as told, with the chosen side as the one
        /// the party stood with. The character arrives at level 10 (picked for them) and chooses level 11 themselves.
        /// </summary>
        public void ApplyActThreeStart(PlayerSlot p, string side)
        {
            ApplyActTwoStart(p); // Act I told, the egg hatched, the dragon named (its name screen is already up)
            foreach (var f in FactionDb.All.Keys) Factions.Hear(f);
            Factions.Commit(side, p);
            World.SetFlag("banner_" + side);
            Quests.Set(Answers.Id, Answers.Rest, p, 0f);
            foreach (var f in new[] { WardenFallen, ActTwoEndSeen, WorldDb.OathSecond, WorldDb.EmberhornGate }) World.SetFlag(f);
            World.Act = 3;
            Clock.Day = 120;
            World.ActThreeDay = Clock.Day;
            World.BlightEased = true; // the Warden's fall eased the blight, as after a played Act II
            GrowUp();
            ApplyActCap();
            GiveActThreeKit(p);
        }

        /// <summary>Anyone starting (or joining) at Act III: levels 1-10 picked for them, XP for 11 (their own choice), and supplies.</summary>
        void GiveActThreeKit(PlayerSlot p)
        {
            var pc = p.Character;
            if (pc == null) return;
            while (pc.Level < PlayerCharacter.ActTwoLevelCap)
            {
                pc.XP = System.Math.Max(pc.XP, PlayerCharacter.XpThresholds[pc.Level + 1]);
                pc.LevelUp(pc.AutoPlan(), null);
            }
            pc.XP = System.Math.Max(pc.XP, PlayerCharacter.XpThresholds[11]);
            pc.HP = pc.EffectiveMaxHP;
            pc.Copper += 60000;
            pc.AddItem("potion_greater_healing", 3);
            p.Hud.Toast("★ Level 11 is waiting! Open your character sheet (C / View).", UIKit.D20Gold);
        }
    }
}
