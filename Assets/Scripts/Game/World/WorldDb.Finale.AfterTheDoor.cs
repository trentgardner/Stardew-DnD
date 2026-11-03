namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 6: the finale's fights in Aurelhold. EXEMPT from the secret guard (LoreTests.GuardScans): every group
    /// requires a flag that is only set after WorldDb.MetHer and the choice at the turn. Members are filled by
    /// GameRoot.PrepareFinale from the ending and Act II state (FinaleDb.StreetMembers).
    /// </summary>
    public static partial class WorldDb
    {
        static void BuildFinale()
        {
            var z = Zones["capital"];
            for (int n = 1; n <= Finale.Stages; n++)
            {
                z.EnemyGroups.Add(new EnemyGroupDef
                {
                    Id = Finale.StreetGroup(n), Style = EncounterStyle.None, Unique = true, SightTiles = 8,
                    RequiresFlag = Finale.StreetOpen(n), ResolvedFlag = Finale.StreetDone(n),
                    Label = "the fighting in " + FinaleDb.StreetTitle(n).Replace("The ", "the "),
                });
            }
        }
    }
}
