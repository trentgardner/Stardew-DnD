using System.Linq;
using DnD.Rules;
using UnityEngine;

namespace DnD.Game
{
    /// <summary>Moving from Act I into Act II.</summary>
    public partial class GameRoot
    {
        /// <summary>The level cap follows the act; companions with banked XP catch up when it rises.</summary>
        void ApplyActCap()
        {
            PlayerCharacter.LevelCap = World.Act >= 3 ? PlayerCharacter.ActThreeLevelCap : World.Act >= 2 ? PlayerCharacter.ActTwoLevelCap : PlayerCharacter.ActOneLevelCap;
            if (World.Act >= 2)
            {
                World.SetFlag(WorldDb.ActTwoFlag);
                // The main quest of Act II begins with it (an Act II save from before it existed picks it up here too).
                var p1 = Players.FirstOrDefault(p => p.Character != null);
                if (p1 != null && Quests.Stage(Answers.Id) == 0) Quests.Start(Answers.Id, p1);
                if (World.Flag("ashby_heard")) Factions.Hear("academy"); // Phase 3 saves heard him before hearing-out existed
                if (p1 != null && Quests.Stage(Answers.Id) == Answers.Chosen && new[] { "banner_crown", "banner_academy", "banner_fae", "banner_hunters" }.Any(World.Flag))
                    Quests.Set(Answers.Id, Answers.Allies, p1); // a Phase 4 save that already finished its side's major quest
                if (Quests.Stage(Answers.Id) >= Answers.Gate) World.SetFlag(WorldDb.EmberhornGate); // a Phase 5 save that already reached the gate
                WorldDb.SendAllyAhead(World);
                if (World.Flag(WardenFallen) && !World.Flag(ActTwoEndSeen)) StartCoroutine(ActTwoEndSoon()); // a save from before the card existed
            }
            if (World.Act >= 3)
            {
                World.SetFlag(WorldDb.ActThreeFlag);
                // Act III opens in the capital (a Phase 2 Act III save picks the quest up here too).
                var p1 = Players.FirstOrDefault(p => p.Character != null);
                if (p1 != null && Quests.Stage(WarmCity.Id) == 0) Quests.Start(WarmCity.Id, p1);
            }
            foreach (var c in Companions) if (c != null && c.Character != null) AutoLevel(c.Character);
            Blight.Sync();
            foreach (var z in Zones.Values) foreach (var grp in z.Def.EnemyGroups) if (grp.MinBlight > 0) SpawnGroupIfNeeded(z, grp);
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

        bool _startActTwo;
        int _bellDay = -1;

        /// <summary>The Thirteen Bells: at midnight everyone on the Academy grounds hears twelve, and then a thirteenth.</summary>
        void AcademyBells()
        {
            if (Clock.Hour != 0 || _bellDay == Clock.Day) return;
            _bellDay = Clock.Day;
            foreach (var p in Players.Where(p => p.IsReady && p.Zone != null && p.Zone.Def.Id == "academy"))
                p.Hud.Narrate("The bells ring midnight. Twelve... and then, a breath late, a thirteenth.");
        }

        /// <summary>
        /// A new game that starts at Act II (level 5 preview): Act I's story counts as told, the dragon has hatched
        /// and is a teenager, and the player levels 1→5 through the level-up screen with real choices.
        /// </summary>
        public void ApplyActTwoStart(PlayerSlot p)
        {
            World.Act = 2;
            Clock.Day = 29; Clock.Minutes = 8 * 60;
            World.ActTwoDay = Clock.Day;
            Quests.Set("fallen_star", FallenStar.Done, p, 0f);
            foreach (var f in new[] { "snik_friend", "elder_blessing", "act1_ended", "act1_card:0", "act1_card:1" }) World.SetFlag(f);
            World.EggHatched = true;
            World.DragonStage = (int)DragonStage.Wyrmling;
            if (Dragon == null) SpawnDragon(p);
            ApplyActCap();
            GiveActTwoKit(p);
            DragonGrew();
            p.Hud.Push(new NameEntryScreen(n => RenameDragon(n)));
        }

        /// <summary>Anyone starting (or joining) at Act II: level-5 XP and a few supplies.</summary>
        void GiveActTwoKit(PlayerSlot p)
        {
            var pc = p.Character;
            if (pc == null) return;
            pc.XP = System.Math.Max(pc.XP, PlayerCharacter.XpThresholds[5]);
            pc.Copper += 15000;
            pc.AddItem("potion_healing", 2);
            pc.AddItem("potion_greater_healing", 1);
            p.Hud.Toast("★ Level up available! Open your character sheet (C / View).", UIKit.D20Gold);
        }

        void RenameDragon(string name)
        {
            World.DragonName = string.IsNullOrWhiteSpace(name) ? "Cinder" : name.Trim();
            if (Dragon != null) Dragon.Name = World.DragonName;
            if (DragonAgent != null) DragonAgent.Actor.name = World.DragonName;
        }

        public const float TeenDragonScale = 1.2f;

        /// <summary>The dragon's body catches up with its stage: a teenager is bigger and a deeper red.</summary>
        void DragonGrew()
        {
            var a = DragonAgent;
            if (a == null || Dragon == null || !Dragon.IsTeen) return;
            a.Actor.transform.localScale = Vector3.one * (Dragon.IsGrown ? GrownDragonScale : TeenDragonScale);
            string look = Dragon.IsGrown && ArtRegistry.IsReal("mon_dragon_grown_0") ? "mon_dragon_grown" : "mon_dragonling_teen";
            if (ArtRegistry.IsReal(look + "_0")) { a.Actor.Look = look; a.Actor.RefreshSprite(); }
        }
    }
}
