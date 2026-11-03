namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: through the grey door. Exempt from the secret guard: nothing here runs, and nothing here can be seen,
    /// until the party opens the door, which sets WorldDb.MetHer before anything is shown.
    /// </summary>
    public partial class GameRoot
    {
        public void ThroughTheDoor(PlayerSlot by)
        {
            World.SetFlag(WorldDb.MetHer);
        }
    }
}
