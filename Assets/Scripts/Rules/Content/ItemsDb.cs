using System.Collections.Generic;

namespace DnD.Rules.Content
{
    /// <summary>SRD 5.2 weapons, armor and the game's consumables.</summary>
    public static class ItemsDb
    {
        public static readonly Dictionary<string, ItemDef> All = new Dictionary<string, ItemDef>();

        static WeaponProperty F => WeaponProperty.Finesse;
        static WeaponProperty L => WeaponProperty.Light;
        static WeaponProperty H => WeaponProperty.Heavy;
        static WeaponProperty TH => WeaponProperty.TwoHanded;
        static WeaponProperty V => WeaponProperty.Versatile;
        static WeaponProperty T => WeaponProperty.Thrown;
        static WeaponProperty R => WeaponProperty.Reach;
        static WeaponProperty A => WeaponProperty.Ammunition;
        static WeaponProperty LD => WeaponProperty.Loading;

        static void W(string id, string name, WeaponCategory cat, string dmg, DamageType type, WeaponProperty props,
                      WeaponMastery mastery, int gp, string versatile = null, int range = 0, int longRange = 0)
        {
            All[id] = new WeaponDef
            {
                Id = id, Name = name, Kind = ItemKind.Weapon, Category = cat, Damage = DiceExpr.Parse(dmg),
                VersatileDamage = versatile != null ? DiceExpr.Parse(versatile) : DiceExpr.Parse(dmg),
                DamageType = type, Properties = props, Mastery = mastery, PriceCp = gp * 100,
                RangeNormal = range, RangeLong = longRange, Icon = "item_" + id,
                Description = $"{dmg} {type.ToString().ToLower()}. Mastery: {mastery}.",
            };
        }

        static void Arm(string id, string name, ArmorCategory cat, int ac, int maxDex, int gp, int str = 0, bool stealthDis = false)
        {
            All[id] = new ArmorDef
            {
                Id = id, Name = name, Kind = ItemKind.Armor, Category = cat, BaseAC = ac, MaxDex = maxDex,
                PriceCp = gp * 100, StrengthRequirement = str, StealthDisadvantage = stealthDis, Icon = "item_" + id,
                Description = $"{cat} armor, AC {ac}{(maxDex < 0 ? " + DEX" : maxDex > 0 ? " + DEX (max " + maxDex + ")" : "")}.",
            };
        }

        static void C(string id, string name, ConsumableEffect fx, int priceCp, string amount, string desc, string spell = null)
        {
            All[id] = new ConsumableDef
            {
                Id = id, Name = name, Kind = ItemKind.Consumable, Effect = fx, PriceCp = priceCp,
                Amount = amount != null ? DiceExpr.Parse(amount) : DiceExpr.None, Description = desc, SpellId = spell, Icon = "item_" + id,
            };
        }

        static ItemsDb()
        {
            var S = WeaponCategory.Simple; var M = WeaponCategory.Martial;
            var Bl = DamageType.Bludgeoning; var Pi = DamageType.Piercing; var Sl = DamageType.Slashing;

            // Simple melee
            W("club", "Club", S, "1d4", Bl, L, WeaponMastery.Slow, 0);
            W("dagger", "Dagger", S, "1d4", Pi, F | L | T, WeaponMastery.Nick, 2, null, 20, 60);
            W("greatclub", "Greatclub", S, "1d8", Bl, TH, WeaponMastery.Push, 0);
            W("handaxe", "Handaxe", S, "1d6", Sl, L | T, WeaponMastery.Vex, 5, null, 20, 60);
            W("javelin", "Javelin", S, "1d6", Pi, T, WeaponMastery.Slow, 0, null, 30, 120);
            W("light_hammer", "Light Hammer", S, "1d4", Bl, L | T, WeaponMastery.Nick, 2, null, 20, 60);
            W("mace", "Mace", S, "1d6", Bl, WeaponProperty.None, WeaponMastery.Sap, 5);
            W("quarterstaff", "Quarterstaff", S, "1d6", Bl, V, WeaponMastery.Topple, 0, "1d8");
            W("sickle", "Sickle", S, "1d4", Sl, L, WeaponMastery.Nick, 1);
            W("spear", "Spear", S, "1d6", Pi, T | V, WeaponMastery.Sap, 1, "1d8", 20, 60);
            // Simple ranged
            W("light_crossbow", "Light Crossbow", S, "1d8", Pi, A | LD | TH, WeaponMastery.Slow, 25, null, 80, 320);
            W("shortbow", "Shortbow", S, "1d6", Pi, A | TH, WeaponMastery.Vex, 25, null, 80, 320);
            W("sling", "Sling", S, "1d4", Bl, A, WeaponMastery.Slow, 0, null, 30, 120);
            // Martial melee
            W("battleaxe", "Battleaxe", M, "1d8", Sl, V, WeaponMastery.Topple, 10, "1d10");
            W("flail", "Flail", M, "1d8", Bl, WeaponProperty.None, WeaponMastery.Sap, 10);
            W("glaive", "Glaive", M, "1d10", Sl, H | R | TH, WeaponMastery.Graze, 20);
            W("greataxe", "Greataxe", M, "1d12", Sl, H | TH, WeaponMastery.Cleave, 30);
            W("greatsword", "Greatsword", M, "2d6", Sl, H | TH, WeaponMastery.Graze, 50);
            W("halberd", "Halberd", M, "1d10", Sl, H | R | TH, WeaponMastery.Cleave, 20);
            W("longsword", "Longsword", M, "1d8", Sl, V, WeaponMastery.Sap, 15, "1d10");
            W("maul", "Maul", M, "2d6", Bl, H | TH, WeaponMastery.Topple, 10);
            W("morningstar", "Morningstar", M, "1d8", Pi, WeaponProperty.None, WeaponMastery.Sap, 15);
            W("rapier", "Rapier", M, "1d8", Pi, F, WeaponMastery.Vex, 25);
            W("scimitar", "Scimitar", M, "1d6", Sl, F | L, WeaponMastery.Nick, 25);
            W("shortsword", "Shortsword", M, "1d6", Pi, F | L, WeaponMastery.Vex, 10);
            W("trident", "Trident", M, "1d8", Pi, T | V, WeaponMastery.Topple, 5, "1d10", 20, 60);
            W("warhammer", "Warhammer", M, "1d8", Bl, V, WeaponMastery.Push, 15, "1d10");
            W("war_pick", "War Pick", M, "1d8", Pi, V, WeaponMastery.Sap, 5, "1d10");
            W("whip", "Whip", M, "1d4", Sl, F | R, WeaponMastery.Slow, 2);
            // Martial ranged
            W("hand_crossbow", "Hand Crossbow", M, "1d6", Pi, A | L | LD, WeaponMastery.Vex, 75, null, 30, 120);
            W("heavy_crossbow", "Heavy Crossbow", M, "1d10", Pi, A | H | LD | TH, WeaponMastery.Push, 50, null, 100, 400);
            W("longbow", "Longbow", M, "1d8", Pi, A | H | TH, WeaponMastery.Slow, 50, null, 150, 600);

            // Armor
            Arm("padded", "Padded Armor", ArmorCategory.Light, 11, -1, 5, 0, true);
            Arm("leather", "Leather Armor", ArmorCategory.Light, 11, -1, 10);
            Arm("studded_leather", "Studded Leather", ArmorCategory.Light, 12, -1, 45);
            Arm("hide", "Hide Armor", ArmorCategory.Medium, 12, 2, 10);
            Arm("chain_shirt", "Chain Shirt", ArmorCategory.Medium, 13, 2, 50);
            Arm("scale_mail", "Scale Mail", ArmorCategory.Medium, 14, 2, 50, 0, true);
            Arm("breastplate", "Breastplate", ArmorCategory.Medium, 14, 2, 400);
            Arm("half_plate", "Half Plate", ArmorCategory.Medium, 15, 2, 750, 0, true);
            Arm("ring_mail", "Ring Mail", ArmorCategory.Heavy, 14, 0, 30, 0, true);
            Arm("chain_mail", "Chain Mail", ArmorCategory.Heavy, 16, 0, 75, 13, true);
            Arm("splint", "Splint Armor", ArmorCategory.Heavy, 17, 0, 200, 15, true);
            Arm("plate", "Plate Armor", ArmorCategory.Heavy, 18, 0, 1500, 15, true);
            All["shield"] = new ItemDef { Id = "shield", Name = "Shield", Kind = ItemKind.Shield, PriceCp = 1000, Icon = "item_shield", Description = "+2 AC." };

            // Consumables
            C("potion_healing", "Potion of Healing", ConsumableEffect.Heal, 5000, "2d4+2", "Drink (Bonus Action) to regain 2d4+2 HP. Roll it yourself!");
            C("potion_greater_healing", "Potion of Greater Healing", ConsumableEffect.Heal, 15000, "4d4+4", "Regain 4d4+4 HP.");
            C("ember_of_return", "Ember of Return", ConsumableEffect.Revive, 5000, null,
              "A coal said to be a fragment of dragonfire. Press it to a fallen ally's heart to bring them back with 1 HP.");
            C("scroll_revivify", "Scroll of Revivify", ConsumableEffect.SpellScroll, 30000, null, "Returns a creature that died within the last minute to life with 1 HP.", "revivify");
            C("torch", "Torch", ConsumableEffect.Light, 1, null, "Bright light 20 ft, dim light 20 ft more. Burns for 1 hour.");
            C("rations", "Rations (1 day)", ConsumableEffect.Ration, 50, null, "Travel food. Only matters when you're far from a hot meal.");

            AddMagicItems();
            ApplyWeights();

            All["gemstone"] = new ItemDef { Id = "gemstone", Name = "Polished Gemstone", Kind = ItemKind.Treasure, PriceCp = 1000, Icon = "item_gem", Description = "Kobolds love these." };
            All["copper_trinket"] = new ItemDef { Id = "copper_trinket", Name = "Shiny Copper Trinket", Kind = ItemKind.Treasure, PriceCp = 50, Icon = "item_trinket", Description = "Probably stolen from a farmhouse." };
            All["star_shard"] = new ItemDef { Id = "star_shard", Name = "Star Shard", Kind = ItemKind.Treasure, PriceCp = 1000, Icon = "item_star_shard", Description = "A sliver of green-gold crystal from the crater. It glows faintly, and hums if you hold it to your ear." };
            All["star_iron"] = new ItemDef { Id = "star_iron", Name = "Lump of Star-Iron", Kind = ItemKind.Treasure, PriceCp = 2500, Icon = "item_star_iron", Description = "Heavy, dark and pitted, still faintly warm. Garrick would love this." };
            All["copper_kettle"] = new ItemDef { Id = "copper_kettle", Name = "Dented Copper Kettle", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_copper_trinket", StackSize = 1, QuestItem = true, Description = "Brannoc's grandmother's kettle. Smells faintly of kobold." };
            All["pond_ring"] = new ItemDef { Id = "pond_ring", Name = "Silver Ring", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_ring", StackSize = 1, QuestItem = true, Description = "A silver ring with a tiny blue stone. Still a little damp. Tamsin's." };
            All["brannoc_poem"] = new ItemDef { Id = "brannoc_poem", Name = "Anonymous Poem", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_poem", StackSize = 1, QuestItem = true, Description = "Sealed with wax and a thumbprint that is definitely Brannoc's. For Mother Elowen. From 'a secret admirer of the dawn'." };
            All["goat_bell"] = new ItemDef { Id = "goat_bell", Name = "Tobin's Goat Bell", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_goat_bell", StackSize = 1, Weight = 0.5f, Description = "A brass bell on a red strap. It rings whenever you're trying to sneak. Tobin swears it's lucky." };
            All["herb_moonpetal"] = new ItemDef { Id = "herb_moonpetal", Name = "Moonpetal", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_herb", StackSize = 1, QuestItem = true, Description = "A pale flower that glows faintly. For Mother Elowen." };
            All["herb_riverleaf"] = new ItemDef { Id = "herb_riverleaf", Name = "Riverleaf", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_herb", StackSize = 1, QuestItem = true, Description = "Cool, damp leaves that smell of mint and mud. For Mother Elowen." };
            All["herb_hearthroot"] = new ItemDef { Id = "herb_hearthroot", Name = "Hearthroot", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_herb", StackSize = 1, QuestItem = true, Description = "A knobbly root, warm to the touch. For Mother Elowen." };
            All["fae_trinket"] = new ItemDef { Id = "fae_trinket", Name = "Thimble's Mirror", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_copper_trinket", StackSize = 1, Description = "A pocket mirror that never fogs. Your reflection sometimes winks a moment before you do." };
            All["lucky_spoon"] = new ItemDef { Id = "lucky_spoon", Name = "Old Tobin's Lucky Spoon", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_dagger", StackSize = 1, QuestItem = true, Description = "A worn wooden spoon. Tobin insists it makes soup taste better. (It does.)" };
            All["egg_shard"] = new ItemDef { Id = "egg_shard", Name = "Warm Eggshell Shard", Kind = ItemKind.Treasure, PriceCp = 0, Icon = "item_shard", Description = "Faintly warm. Smells like a campfire." };
        }

        /// <summary>SRD 5.2 item weights in pounds.</summary>
        static void ApplyWeights()
        {
            var w = new Dictionary<string, float>
            {
                {"club",2},{"dagger",1},{"greatclub",10},{"handaxe",2},{"javelin",2},{"light_hammer",2},{"mace",4},
                {"quarterstaff",4},{"sickle",2},{"spear",3},{"light_crossbow",5},{"shortbow",2},{"sling",0},
                {"battleaxe",4},{"flail",2},{"glaive",6},{"greataxe",7},{"greatsword",6},{"halberd",6},{"longsword",3},
                {"maul",10},{"morningstar",4},{"rapier",2},{"scimitar",3},{"shortsword",2},{"trident",4},{"warhammer",5},
                {"war_pick",2},{"whip",3},{"hand_crossbow",3},{"heavy_crossbow",18},{"longbow",2},
                {"padded",8},{"leather",10},{"studded_leather",13},{"hide",12},{"chain_shirt",20},{"scale_mail",45},
                {"breastplate",20},{"half_plate",40},{"ring_mail",40},{"chain_mail",55},{"splint",60},{"plate",65},{"shield",6},
                {"potion_healing",0.5f},{"potion_greater_healing",0.5f},{"ember_of_return",0.1f},{"scroll_revivify",0},
                {"torch",1},{"rations",2},{"gemstone",0},{"copper_trinket",0.1f},{"egg_shard",0.5f},
                {"copper_kettle",2},{"lucky_spoon",0.1f},{"emberwrought_blade",3},{"starfall_staff",4},{"veilthread_cloak",1},{"oathbound_signet",0},
                {"dawnmothers_charm",0},{"starfall_wand",1},{"fennicks_lucky_copper",0},
            };
            foreach (var kv in w) if (All.TryGetValue(kv.Key, out var item)) item.Weight = kv.Value;
        }

        static void Wondrous(string id, string name, WornSlot slot, int gp, bool attune, string desc, string lore,
                             int ac = 0, int save = 0, int spellAtk = 0, bool deathAdv = false, bool reroll = false, string rarity = "Uncommon")
        {
            All[id] = new WondrousDef
            {
                Id = id, Name = name, Kind = ItemKind.Wondrous, Slot = slot, PriceCp = gp * 100, RequiresAttunement = attune,
                Description = desc, Lore = lore, AcBonus = ac, SaveBonus = save, SpellAttackBonus = spellAtk,
                DeathSaveAdvantage = deathAdv, RerollPerLongRest = reroll, Rarity = rarity, Icon = "item_" + id,
            };
        }

        /// <summary>
        /// Lore-tied magic items. Deliberately modest (common/uncommon) for levels 1-3:
        /// each is a nice edge or a flavorful once-a-day trick, never a power spike.
        /// </summary>
        static void AddMagicItems()
        {
            // Weapon: a sliver of pre-Sundering dragonfire. +1d4 fire, no attack bonus (weaker than a Flame Tongue).
            var blade = (WeaponDef)((WeaponDef)All["longsword"]).MemberwiseCloneWeapon();
            blade.Id = "emberwrought_blade"; blade.Name = "Emberwrought Blade"; blade.Rarity = "Uncommon";
            blade.ExtraDamage = new DiceExpr(1, 4); blade.ExtraDamageType = DamageType.Fire;
            blade.PriceCp = 40000; blade.Icon = "item_emberwrought_blade";
            blade.Description = "Longsword. 1d8 slashing (1d10 two-handed) + 1d4 fire. Mastery: Sap. Its edge glows like a banked coal.";
            All[blade.Id] = blade;

            // Weapon: a plain +1 staff from the Starfall Academy's lost-and-found.
            var staff = (WeaponDef)((WeaponDef)All["quarterstaff"]).MemberwiseCloneWeapon();
            staff.Id = "starfall_staff"; staff.Name = "Starfall Training Staff"; staff.Rarity = "Uncommon";
            staff.MagicBonus = 1; staff.PriceCp = 30000; staff.Icon = "item_starfall_staff";
            staff.Description = "+1 Quarterstaff. Stamped 'PROPERTY OF STARFALL ACADEMY, DO NOT REMOVE.' Someone removed it.";
            All[staff.Id] = staff;

            Wondrous("veilthread_cloak", "Veilthread Cloak", WornSlot.Cloak, 300, true,
                "+1 AC. Woven from thread that shimmers between colors when nobody's looking directly at it.",
                "Fae of the Court of Blossoms trade these for favors. Never ask which favor.", ac: 1);

            Wondrous("oathbound_signet", "Oathbound Signet", WornSlot.Ring, 300, true,
                "+1 to all saving throws.",
                "A ring of the long-gone Oathbound knights. Engraved inside: 'Journey before comfort.'", save: 1);

            Wondrous("dawnmothers_charm", "Dawnmother's Charm", WornSlot.Amulet, 150, false,
                "You roll death saving throws with Advantage.",
                "A sunburst pendant blessed by Aurelith's clergy. Mother Elowen hands these to adventurers she likes.", deathAdv: true);

            Wondrous("starfall_wand", "Starfall Wand", WornSlot.Focus, 250, true,
                "+1 to spell attack rolls.",
                "A student's practice wand from the Starfall Academy, tipped with a fleck of fallen star.", spellAtk: 1);

            Wondrous("fennicks_lucky_copper", "Fennick's Lucky Copper", WornSlot.Trinket, 50, false,
                "Once per long rest, after you roll a d20 Test, you can reroll it and take the new roll.",
                "A copper coin with a laughing face on both sides. Bards swear by them. Gamblers swear at them.", reroll: true, rarity: "Common");
        }

        public static T Get<T>(string id) where T : ItemDef => All.TryGetValue(id, out var it) ? it as T : null;
        public static ItemDef Get(string id) => All.TryGetValue(id, out var it) ? it : null;
    }
}
