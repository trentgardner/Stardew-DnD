using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Everything belonging to one human player: device, character, avatar, camera and HUD.</summary>
    public class PlayerSlot
    {
        public int Index;
        public PlayerInput Input;
        public PlayerCharacter Character;
        public PlayerController Avatar;
        public Camera Cam;
        public PlayerHud Hud;
        public CombatInstance Combat;
        /// <summary>Darkness overlay layer for this player's camera (each player sees their own darkvision).</summary>
        public int DarkLayer => 8 + Index;

        public Zone Zone => Avatar != null ? Avatar.Zone : null;
        public Vector2Int MapPos => Avatar != null ? Avatar.MapPos : Vector2Int.zero;
        public bool InCombat => Combat != null && !Combat.Finished;
        public bool IsReady => Character != null && Avatar != null;

        /// <summary>Busy = the world should not tick for this player (menus, dialogue, dice, combat).</summary>
        public bool IsBusy => Hud != null && (Hud.HasScreen || Hud.Dice.IsOpen) || InCombat;

        public string Label => Character != null ? Character.Name : $"Player {Index + 1}";
    }
}
