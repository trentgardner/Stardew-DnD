using System.Collections.Generic;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: below Aurelhold. The Undergate Works, the Iron Vaults, the Royal Crypts, the Deep and the grey door.
    /// Dread and suspicion of the Crown are allowed. Nothing here may hint at THE SECRET (LoreTests scans this file);
    /// everything beyond the door lives in *.AfterTheDoor.cs.
    /// </summary>
    public static partial class WorldDb
    {
        /// <summary>World flag: the Undergate is open (warm_city stage 5).</summary>
        public const string UndergateOpen = "undergate_open";

        static void BuildLair()
        {
            BuildUndergate();
        }

        // =================================================================== the Undergate, and the Works below it

        static void BuildUndergate()
        {
            var roots = Zones["roots"];
            const string shut = "The Undergate: black iron, twice the height of a house, barred and sealed with the King's own seal. It does not open.";
            roots.Warps.Add(new WarpDef(52, 20, "undergate", 1, 14) { RequiresFlag = UndergateOpen, ClosedText = shut });
            roots.Warps.Add(new WarpDef(52, 21, "undergate", 1, 15) { RequiresFlag = UndergateOpen, ClosedText = shut });
            roots.Secrets[new Vector2Int(51, 20)] = new SecretDef
            {
                Id = "undergate_bar", Sprite = "obj_inscription", Prompt = "Heave the great bar (Athletics)", Skill = Rules.Skill.Athletics, DC = 16,
                QuestId = WarmCity.Id, QuestStage = WarmCity.Below, AdvanceTo = WarmCity.Opened,
                Success = "You set your shoulder under the bar and lift. Iron groans, the King's seal cracks down the middle, and the Undergate swings inward on hinges that someone has kept oiled. Warm air breathes out of the dark.",
                Failure = "The Undergate doesn't move. You'll try again tomorrow.",
                AlreadyFound = "The Undergate stands open. Warm air breathes out of the dark.",
            };
            roots.Secrets[new Vector2Int(51, 21)] = new SecretDef
            {
                Id = "undergate_lock", Sprite = "obj_inscription", Prompt = "Work the King's lock (Sleight of Hand)", Skill = Rules.Skill.SleightOfHand, DC = 15,
                QuestId = WarmCity.Id, QuestStage = WarmCity.Below, AdvanceTo = WarmCity.Opened,
                Success = "The lock is old, and fine, and has been opened recently: there's fresh oil in the wards. It gives with a soft click. The seal stays whole, and the gate swings in.",
                Failure = "The Undergate doesn't move. You'll try again tomorrow.",
                AlreadyFound = "The Undergate stands open. Warm air breathes out of the dark.",
            };

            var z = new ZoneDef
            {
                Act = 3,
                Id = "undergate", Name = "The Undergate Works", Map = Maps.Undergate, Dark = true, DefaultSpawn = new Vector2Int(2, 14), Music = "music_undergate",
                BlightWeight = 0f,
                EnterNarration = "The stair goes down a long way, and it gets warmer the whole time. At the bottom, lamplight: a camp of bunks and washing lines, a long table set for twenty, and men in work-leathers who stop and stare at you as if you'd come down from the moon.",
            };
            z.Warps.Add(new WarpDef(0, 14, "roots", 50, 20));
            z.Warps.Add(new WarpDef(0, 15, "roots", 50, 21));
            z.DoorSigns[new Vector2Int(25, 11)] = new BuildingSign("The Keepers' Post", "THE KEEPERS OF THE UNDERGATE\n\nBy royal warrant. Posted for life.\nNO ONE GOES ON.");
            Zones[z.Id] = z;
        }
    }
}
