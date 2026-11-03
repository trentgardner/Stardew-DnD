namespace DnD.Game
{
    /// <summary>
    /// Act III Phase 4: the "Read again" lines. EXEMPT from the secret guard. These are set at startup but only ever shown once
    /// WorldDb.MetHer is set (LoreDb.Revealed checks it). The two entries from her room can only be found beyond the door.
    /// </summary>
    public static partial class LoreDb
    {
        static void RegisterReveals()
        {
            Add("her_drawings", "The Drawings", Legend,
                "Twelve dragons asleep in a ring, drawn in charcoal over and over by the same hand, getting better as they go. Under one, in a child's careful letters: MY FRIENDS. THEY SLEEP ROUND THE HOUSE SO I AM NOT ON MY OWN.", 3);
            Add("her_presents", "The Presents", Records,
                "A wooden horse. A tin soldier. A music box that plays three notes. A doll in a court dress five hundred years out of fashion. One from every century, and under each, in a different grown-up hand: 'Forgive me.'", 3);

            SetReveal("shared_dream", "The knocking was her. Slow and steady, so somebody would know she was there.");
            SetReveal("warden_last_words", "Hold the door. Not against something terrible. Shut, on a little girl.");
            SetReveal("thirteenth_bell", "Twelve bells for the Great Ones who lie round her. The thirteenth was always for her.");
            SetReveal("iron_ring", "Someone wanted something to stay here. Someone wanted her to stay.");
            SetReveal("altered_annals", "Two sets of annals, cut and corrected for the same years. The year they put her there.");
            SetReveal("royal_annals", "Year 2 of the Crown, razored out. The year the first King came home with an iron ring.");
            SetReveal("sealing_work", "'Gently, and forever.' Ashby's circles were made for her.");
            SetReveal("ashen_queen", "A dragon of grey fire, in every temple. Nobody ever painted the girl it was folded up inside.");
            SetReveal("skipping_rhyme", "Bar the door, and the Ashen Queen won't come no more. She never could.");
            SetReveal("primer_ashen_age", "'Why must they never wake?' Because then somebody would go and look.");
            SetReveal("victory_statue", "The crowned figure in the carved flames, worn smooth by a thousand years of hands. Look again: she's the size of a child.");
            SetReveal("ever_lamps", "Her warmth, piped up from the Deep to light the city that keeps her there. The lamps flicker when she cries.");
            SetReveal("the_kings_ring", "The same iron as the rings in the Deep. Every King wears the key.");
            SetReveal("sealed_crypt", "One pair of feet, once a year, with a present and an apology.");
            SetReveal("works_ledger", "Forty men on the deep shift, oiling the pipes that carry her heat, never told whose it was.");
            SetReveal("great_ones_statue", "'Who lay down so the world could wake.' They lay down round her, so she wouldn't be alone.");
            // Gate: LoreDb.Revealed shows none of these until WorldDb.MetHer is set.
        }
    }
}
