using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act II Phase 5: the blight reaches known places, one neighbour serves the Queen, and the road climbs to the Emberhorn.
    /// Nothing here may hint at THE SECRET (see LoreTests.NoActTwoTextHintsAtTheSecret).
    /// </summary>
    public static partial class WorldDb
    {
        static void BuildEmberhorn()
        {
            BuildBlightBeasts();
        }

        // =================================================================== Ember-eyed beasts in known places

        static void BuildBlightBeasts()
        {
            var wolves = new EnemyGroupDef
            {
                Id = "blight_wolves_forest", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the ember-eyed wolves", MinBlight = 2,
                Spotted = "Three wolves stand among the trees where no wolves should be, grey with ash, their eyes glowing like coals in a dying fire.",
                Befriended = "You hold out empty hands and speak low and slow. The lead wolf's burning eyes flicker. It whines, and the pack melts back into the trees.",
            };
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 40, 24));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 42, 25));
            wolves.Members.Add(new EnemySpawn("ember_eyed_wolf", 41, 27));
            Zones["forest"].EnemyGroups.Add(wolves);

            var boars = new EnemyGroupDef
            {
                Id = "blight_boars_ford", Style = EncounterStyle.Beasts, TalkDC = 14, ScareDC = 14, Label = "the ember-eyed boars", MinBlight = 2,
                Spotted = "Two boars are tearing up Hobb's turnip field, ash in their bristles, eyes like hot coals. They look up at you together.",
                Befriended = "You back off slowly, talking the whole way. The boars snort smoke, decide you aren't worth it, and trot off toward the hills.",
            };
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 38, 10));
            boars.Members.Add(new EnemySpawn("ember_eyed_boar", 41, 11));
            Zones["ford"].EnemyGroups.Add(boars);

            var hounds = new EnemyGroupDef
            {
                Id = "blight_hounds_crater", Style = EncounterStyle.Beasts, TalkDC = 15, ScareDC = 14, Label = "the cinder hounds", MinBlight = 3,
                Spotted = "Two hounds pace the crater's rim, breath smoking, as if they're waiting for something to climb out.",
                Befriended = "You stand your ground and speak steadily. The hounds stare past you, toward the mountains, then lope away together.",
            };
            hounds.Members.Add(new EnemySpawn("cinder_hound", 6, 13));
            hounds.Members.Add(new EnemySpawn("cinder_hound", 8, 15));
            Zones["crater"].EnemyGroups.Add(hounds);
        }
    }
}
