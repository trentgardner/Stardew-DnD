using System.Collections.Generic;

namespace DnD.Game
{
    public static partial class DialogueDb
    {
        /// <summary>
        /// What townsfolk do when the dragon is with you: one aside a day, shown in the narration of their greeting.
        /// {0} is the dragon's name. (Captain Vale, id rhoswen, has her own dragon conversation in the main quest.)
        /// </summary>
        public static readonly Dictionary<string, string> DragonAsides = new Dictionary<string, string>
        {
            ["pip"] = "Pip eyes {0} and quietly moves the good tablecloths out of reach. \"Lovely. Adorable. If it sets my bar on fire, you're paying.\"",
            ["brannoc"] = "Brannoc peers at {0} over his spectacles. \"Hm. Fireproof gloves are on the second shelf. No reason. Just mentioning.\"",
            ["elowen"] = "Mother Elowen kneels and lets {0} sniff her hand. \"Oh, little one. There's always a warm spot by the temple hearth for you.\"",
            ["perrin"] = "Perrin nearly drops his notebook at the sight of {0}. \"Is that... from the STAR? May I measure it? Just the wingspan! It won't feel a thing!\"",
            ["garrick"] = "Garrick watches {0} sniff at the forge and grins. \"Ha! Finally, an apprentice who isn't scared of the heat.\"",
        };

        /// <summary>The aside for this greeting, or null (no dragon with you, no line for this person, or already said today).</summary>
        public static string DragonAside(DCtx c)
        {
            var g = GameRoot.I;
            if (c.Npc == null || g.Dragon == null || !DragonAsides.TryGetValue(c.NpcId, out var line)) return null;
            var a = g.DragonAgent;
            if (a == null || !a.gameObject.activeSelf || a.Actor.Zone != c.P.Zone) return null;
            string key = "dragon_aside:" + c.NpcId;
            if (g.World.IsLocked(key)) return null;
            g.World.Lock(key, 24 * 60);
            return string.Format(line, g.World.DragonName);
        }
    }
}
