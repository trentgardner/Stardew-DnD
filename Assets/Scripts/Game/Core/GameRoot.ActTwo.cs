using System.Linq;
using DnD.Rules;

namespace DnD.Game
{
    /// <summary>Moving from Act I into Act II.</summary>
    public partial class GameRoot
    {
        /// <summary>The level cap follows the act; companions with banked XP catch up when it rises.</summary>
        void ApplyActCap()
        {
            PlayerCharacter.LevelCap = World.Act >= 2 ? PlayerCharacter.ActTwoLevelCap : PlayerCharacter.ActOneLevelCap;
            foreach (var c in Companions) if (c != null && c.Character != null) AutoLevel(c.Character);
        }

        /// <summary>
        /// A season passes: four weeks later the party is rested, the cap is 10, companions catch up and the
        /// dragon is a gangly teenager. Unfinished Act I stories stay open.
        /// </summary>
        public void BeginActTwo(PlayerSlot by)
        {
            if (World.Act >= 2) return;
            World.Act = 2;
            Clock.Day += 28;
            Clock.Minutes = 8 * 60;
            World.ActTwoDay = Clock.Day;
            World.DragonStage = (int)DragonStage.Wyrmling;
            if (Dragon != null) { Dragon.Stage = DragonStage.Wyrmling; Dragon.SetPartyLevel(PartyLevel); Dragon.HP = Dragon.MaxHP; }
            World.DragonDamage = 0; World.DragonScared = false;
            foreach (var p in Players.Where(p => p.Character != null)) p.Character.LongRest(true, null);
            foreach (var c in Companions.Where(c => c != null && c.Character != null)) c.Character.LongRest(true, null);
            ApplyActCap();
            DragonGrew();
            string name = World.EggHatched ? World.DragonName : "the egg";
            foreach (var p in Players.Where(p => p.IsReady))
            {
                p.Hud.CloseAll();
                p.Hud.Push(new MessageScreen("A Season Passes",
                    $"Summer turns to autumn in Thistledown. The harvest comes in, the nights draw cold, and {name} grows: all elbows, wings and opinions, " +
                    "too big for a lap and very unhappy about it.\n\nThen the rider in royal colours finally arrives, and with them, other visitors. " +
                    "Down the valley, a hearth fire went out last night, and no one could light it again.",
                    () => p.Hud.Narrate("Act II begins. (You can now reach level 10: open your character sheet when you level up.)")));
            }
            SaveSystem.Save(true);
        }

        /// <summary>The dragon's body catches up with its new stage (Task 3 sets the look).</summary>
        partial void DragonGrew();
    }
}
