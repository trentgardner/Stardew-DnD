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

        /// <summary>How long a lost controller can hold up a fight or a dice roll before the game moves on without it.</summary>
        public static float DisconnectGraceSeconds = 10f;
        float _lostAt = -1;

        /// <summary>This player's controller was unplugged (or its battery died). They stand still until it's back.</summary>
        public bool Disconnected
        {
            get
            {
                bool lost = Input != null && !Input.IsConnected;
                if (!lost) _lostAt = -1;
                else if (_lostAt < 0) _lostAt = Time.unscaledTime;
                return lost;
            }
        }

        /// <summary>Disconnected for longer than the grace period: fights skip their turn, dice roll themselves.</summary>
        public bool StalledByDisconnect => Disconnected && Time.unscaledTime - _lostAt >= DisconnectGraceSeconds;

        /// <summary>Busy = the world should not tick for this player (menus, dialogue, dice, combat, lost controller).</summary>
        public bool IsBusy => Hud != null && (Hud.HasScreen || Hud.Dice.IsOpen) || InCombat || Disconnected;

        public string Label => Character != null ? Character.Name : $"Player {Index + 1}";
    }
}
