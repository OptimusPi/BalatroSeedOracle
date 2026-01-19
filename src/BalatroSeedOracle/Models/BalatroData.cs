using System;
using System.Collections.Generic;
using System.Linq;
using BalatroSeedOracle.Helpers;
using Motely;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Complete Balatro game data for .json configuration
    /// Uses Motely enums as the source of truth for item names
    /// </summary>
    public static class BalatroData
    {
        static BalatroData()
        {
            // Initialize all dictionaries from Motely enums
            InitializeJokers();
            InitializeTarotCards();
            InitializeSpectralCards();
            InitializeVouchers();
            InitializeTags();
            InitializeBossBlinds();
            InitializePlanetCards();
            InitializeBoosterPacks();
            InitializeDecks();
            InitializeStakes();

            // Initialize compatibility collections
            InitializeCompatibilityCollections();
        }

        public static readonly Dictionary<string, string> Jokers = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> TarotCards = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> SpectralCards = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Vouchers = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BossBlinds = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> PlanetCards = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BoosterPacks = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Decks = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Stakes = new Dictionary<string, string>();

        public static readonly Dictionary<string, string> Editions = new()
        {
            { "None", "None" },
            { "Foil", "Foil" },
            { "Holographic", "Holographic" },
            { "Polychrome", "Polychrome" },
            { "Negative", "Negative" },
        };

        private static void InitializeJokers()
        {
            // Wildcard entries are in the Favorites section now
            // Old "any*" entries removed - use "Wildcard_Joker *" instead

            // Common Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerCommon>())
            {
                var name = joker.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Uncommon Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerUncommon>())
            {
                var name = joker.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Rare Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerRare>())
            {
                var name = joker.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Legendary Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
            {
                var name = joker.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Jokers[name] = displayName;
            }
        }

        private static void InitializeTarotCards()
        {
            // Add wildcard entries first
            TarotCards["any"] = "Any Tarot";
            TarotCards["*"] = "Any Tarot";
            TarotCards["anytarot"] = "Any Tarot";

            foreach (var tarot in Enum.GetValues<MotelyTarotCard>())
            {
                var name = tarot.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                TarotCards[name] = displayName;
            }
        }

        private static void InitializeSpectralCards()
        {
            foreach (var spectral in Enum.GetValues<MotelySpectralCard>())
            {
                var name = spectral.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                SpectralCards[name] = displayName;
            }
        }

        private static void InitializeVouchers()
        {
            foreach (var voucher in Enum.GetValues<MotelyVoucher>())
            {
                var name = voucher.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Vouchers[name] = displayName;
            }
        }

        private static void InitializeTags()
        {
            // Add wildcard entries first
            //Tags["anytag"] = "Any Tag";
            //Tags["anysmall"] = "Any Small";
            //Tags["anybig"] = "Any Big";

            foreach (var tag in Enum.GetValues<MotelyTag>())
            {
                var name = tag.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                Tags[name] = displayName;
            }
        }

        private static void InitializeBossBlinds()
        {
            foreach (var boss in Enum.GetValues<MotelyBossBlind>())
            {
                var name = boss.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                BossBlinds[name] = displayName;
            }
        }

        private static void InitializePlanetCards()
        {
            // Add wildcard entries first
            PlanetCards["any"] = "Any Planet";
            PlanetCards["*"] = "Any Planet";
            PlanetCards["anyplanet"] = "Any Planet";

            foreach (var planet in Enum.GetValues<MotelyPlanetCard>())
            {
                var name = planet.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                PlanetCards[name] = displayName;
            }
        }

        private static void InitializeBoosterPacks()
        {
            foreach (var pack in Enum.GetValues<MotelyBoosterPack>())
            {
                var name = pack.ToString();
                var displayName = FormatUtils.FormatDisplayName(name);
                BoosterPacks[name] = displayName;
            }
        }

        private static void InitializeDecks()
        {
            Decks["Red"] = "Red";
            Decks["Blue"] = "Blue";
            Decks["Yellow"] = "Yellow";
            Decks["Green"] = "Green";
            Decks["Black"] = "Black";
            Decks["Magic"] = "Magic";
            Decks["Nebula"] = "Nebula";
            Decks["Ghost"] = "Ghost";
            Decks["Abandoned"] = "Abandoned";
            Decks["Checkered"] = "Checkered";
            Decks["Zodiac"] = "Zodiac";
            Decks["Painted"] = "Painted";
            Decks["Anaglyph"] = "Anaglyph";
            Decks["Plasma"] = "Plasma";
            Decks["Erratic"] = "Erratic";
        }

        public static readonly Dictionary<string, string> DeckDescriptions = new()
        {
            { "Red", "+1 discards every round" },
            { "Blue", "+1 hands every round" },
            { "Yellow", "Start with extra $10" },
            { "Green", "$2 per remaining hand/discard\nEnd of round (no interest)" },
            { "Black", "+1 Joker slot\n-1 hand every round" },
            { "Magic", "Start run with the 'Crystal Ball' Voucher" },
            { "Nebula", "Start run with a 'Telescope' Voucher" },
            { "Ghost", "Spectral cards may appear in the shop\nStart with a 'Hex' Spectral" },
            { "Abandoned", "No face cards in deck" },
            { "Checkered", "Start with 26 spades and 26 hearts" },
            { "Zodiac", "Start run with 'Tarot Merchant'\n'Planet Merchant' and 'Overstock'" },
            { "Painted", "+2 hand size\n-1 Joker slot" },
            { "Anaglyph", "After defeating each Boss Blind,\ngain a Double Tag" },
            { "Plasma", "Balance chips and mult\nwhen calculating score for played hand\nX2 base blind size" },
            { "Erratic", "All ranks and suits are randomized" },
        };

        private static void InitializeStakes()
        {
            Stakes["White"] = "White Stake";
            Stakes["Red"] = "Red Stake";
            Stakes["Green"] = "Green Stake";
            Stakes["Black"] = "Black Stake";
            Stakes["Blue"] = "Blue Stake";
            Stakes["Purple"] = "Purple Stake";
            Stakes["Orange"] = "Orange Stake";
            Stakes["Gold"] = "Gold Stake";
        }

        /// <summary>
        /// Get display name from lowercase sprite name (e.g., "weejoker" -> "Wee Joker")
        /// </summary>
        public static string GetDisplayNameFromSprite(string spriteName)
        {
            // Joker sprite name to display name mapping
            var jokerDisplayNames = new Dictionary<string, string>
            {
                { "joker", "Joker" },
                { "greedyjoker", "Greedy Joker" },
                { "lustyjoker", "Lusty Joker" },
                { "wrathfuljoker", "Wrathful Joker" },
                { "gluttonousjoker", "Gluttonous Joker" },
                { "jollyjoker", "Jolly Joker" },
                { "zanyjoker", "Zany Joker" },
                { "madjoker", "Mad Joker" },
                { "crazyjoker", "Crazy Joker" },
                { "drolljoker", "Droll Joker" },
                { "slyjoker", "Sly Joker" },
                { "wilyjoker", "Wily Joker" },
                { "cleverjoker", "Clever Joker" },
                { "deviousjoker", "Devious Joker" },
                { "craftyjoker", "Crafty Joker" },
                { "halfjoker", "Half Joker" },
                { "jokerstencil", "Joker Stencil" },
                { "fourfingers", "Four Fingers" },
                { "mime", "Mime" },
                { "creditcard", "Credit Card" },
                { "ceremonialdagger", "Ceremonial Dagger" },
                { "banner", "Banner" },
                { "mysticsummit", "Mystic Summit" },
                { "marblejoker", "Marble Joker" },
                { "loyaltycard", "Loyalty Card" },
                { "8ball", "8 Ball" },
                { "misprint", "Misprint" },
                { "dusk", "Dusk" },
                { "raisedfist", "Raised Fist" },
                { "chaostheclown", "Chaos the Clown" },
                { "fibonacci", "Fibonacci" },
                { "steeljoker", "Steel Joker" },
                { "scaryface", "Scary Face" },
                { "abstractjoker", "Abstract Joker" },
                { "delayedgratification", "Delayed Gratification" },
                { "hack", "Hack" },
                { "pareidolia", "Pareidolia" },
                { "grosmichel", "Gros Michel" },
                { "evensteven", "Even Steven" },
                { "oddtodd", "Odd Todd" },
                { "scholar", "Scholar" },
                { "businesscard", "Business Card" },
                { "supernova", "Supernova" },
                { "ridethebus", "Ride the Bus" },
                { "spacejoker", "Space Joker" },
                { "egg", "Egg" },
                { "burglar", "Burglar" },
                { "blackboard", "Blackboard" },
                { "runner", "Runner" },
                { "icecream", "Ice Cream" },
                { "dna", "DNA" },
                { "splash", "Splash" },
                { "bluejoker", "Blue Joker" },
                { "sixthsense", "Sixth Sense" },
                { "constellation", "Constellation" },
                { "hiker", "Hiker" },
                { "facelessjoker", "Faceless Joker" },
                { "greenjoker", "Green Joker" },
                { "superposition", "Superposition" },
                { "todolist", "To Do List" },
                { "cavendish", "Cavendish" },
                { "cardsharp", "Card Sharp" },
                { "redcard", "Red Card" },
                { "madness", "Madness" },
                { "squarejoker", "Square Joker" },
                { "seance", "Seance" },
                { "riffraff", "Riff-Raff" },
                { "vampire", "Vampire" },
                { "shortcut", "Shortcut" },
                { "hologram", "Hologram" },
                { "vagabond", "Vagabond" },
                { "baron", "Baron" },
                { "cloud9", "Cloud 9" },
                { "rocket", "Rocket" },
                { "obelisk", "Obelisk" },
                { "midasmask", "Midas Mask" },
                { "luchador", "Luchador" },
                { "photograph", "Photograph" },
                { "giftcard", "Gift Card" },
                { "turtlebean", "Turtle Bean" },
                { "erosion", "Erosion" },
                { "reservedparking", "Reserved Parking" },
                { "mailinrebate", "Mail-In Rebate" },
                { "tothemoon", "To the Moon" },
                { "hallucination", "Hallucination" },
                { "fortuneteller", "Fortune Teller" },
                { "juggler", "Juggler" },
                { "drunkard", "Drunkard" },
                { "stonejoker", "Stone Joker" },
                { "goldenjoker", "Golden Joker" },
                { "luckycat", "Lucky Cat" },
                { "baseballcard", "Baseball Card" },
                { "bull", "Bull" },
                { "dietcola", "Diet Cola" },
                { "tradingcard", "Trading Card" },
                { "flashcard", "Flash Card" },
                { "popcorn", "Popcorn" },
                { "sparetrousers", "Spare Trousers" },
                { "ancientjoker", "Ancient Joker" },
                { "ramen", "Ramen" },
                { "walkietalkie", "Walkie Talkie" },
                { "seltzer", "Seltzer" },
                { "castle", "Castle" },
                { "smileyface", "Smiley Face" },
                { "campfire", "Campfire" },
                { "goldenticket", "Golden Ticket" },
                { "mrbones", "Mr. Bones" },
                { "acrobat", "Acrobat" },
                { "sockandbuskin", "Sock and Buskin" },
                { "swashbuckler", "Swashbuckler" },
                { "troubadour", "Troubadour" },
                { "certificate", "Certificate" },
                { "smearedjoker", "Smeared Joker" },
                { "throwback", "Throwback" },
                { "hangingchad", "Hanging Chad" },
                { "roughgem", "Rough Gem" },
                { "bloodstone", "Bloodstone" },
                { "arrowhead", "Arrowhead" },
                { "onyxagate", "Onyx Agate" },
                { "glassjoker", "Glass Joker" },
                { "showman", "Showman" },
                { "flowerpot", "Flower Pot" },
                { "blueprint", "Blueprint" },
                { "weejoker", "Wee Joker" },
                { "merryandy", "Merry Andy" },
                { "oopsall6s", "Oops! All 6s" },
                { "theidol", "The Idol" },
                { "seeingdouble", "Seeing Double" },
                { "matador", "Matador" },
                { "hittheroad", "Hit the Road" },
                { "theduo", "The Duo" },
                { "thetrio", "The Trio" },
                { "thefamily", "The Family" },
                { "theorder", "The Order" },
                { "thetribe", "The Tribe" },
                { "stuntman", "Stuntman" },
                { "invisiblejoker", "Invisible Joker" },
                { "brainstorm", "Brainstorm" },
                { "satellite", "Satellite" },
                { "shootthemoon", "Shoot the Moon" },
                { "driverslicense", "Driver's License" },
                { "cartomancer", "Cartomancer" },
                { "astronomer", "Astronomer" },
                { "burntjoker", "Burnt Joker" },
                { "bootstraps", "Bootstraps" },
                { "canio", "Canio" },
                { "triboulet", "Triboulet" },
                { "yorick", "Yorick" },
                { "chicot", "Chicot" },
                { "perkeo", "Perkeo" },
            };

            // Handle Wildcard_* names first
            if (spriteName.StartsWith("Wildcard_", StringComparison.OrdinalIgnoreCase))
            {
                // "Wildcard_Joker" -> "Any Joker"
                // "Wildcard_JokerLegendary" -> "Any Legendary"
                // "Wildcard_Tarot" -> "Any Tarot"
                var suffix = spriteName.Substring(9); // Remove "Wildcard_" prefix

                // Handle special cases
                if (suffix.Equals("Joker", StringComparison.OrdinalIgnoreCase))
                    return "Any Joker";
                if (suffix.Equals("JokerCommon", StringComparison.OrdinalIgnoreCase))
                    return "Any Common";
                if (suffix.Equals("JokerUncommon", StringComparison.OrdinalIgnoreCase))
                    return "Any Uncommon";
                if (suffix.Equals("JokerRare", StringComparison.OrdinalIgnoreCase))
                    return "Any Rare";
                if (suffix.Equals("JokerLegendary", StringComparison.OrdinalIgnoreCase))
                    return "Any Legendary";

                // For other types, just format as "Any <Type>"
                return "Any " + FormatUtils.FormatDisplayName(suffix);
            }

            if (jokerDisplayNames.TryGetValue(spriteName.ToLowerInvariant(), out var displayName))
            {
                return displayName;
            }

            // Fallback: try to format the sprite name
            return FormatUtils.FormatDisplayName(spriteName);
        }

        /// <summary>
        /// Gets the correct item ID
        /// </summary>
        public static string GetCorrectItemId(string itemId)
        {
            return itemId;
        }

        /// <summary>
        /// Checks if an item exists in the specified category
        /// </summary>
        public static bool ItemExists(string type, string itemId)
        {
            itemId = GetCorrectItemId(itemId);

            return type switch
            {
                "Joker" => Jokers.ContainsKey(itemId),
                "Tarot" => TarotCards.ContainsKey(itemId),
                "Spectral" => SpectralCards.ContainsKey(itemId),
                "Voucher" => Vouchers.ContainsKey(itemId),
                "Tag" => Tags.ContainsKey(itemId),
                "Boss" => BossBlinds.ContainsKey(itemId),
                "Planet" => PlanetCards.ContainsKey(itemId),
                _ => false,
            };
        }

        public static readonly List<string> LegendaryJokers = new List<string>();
        public static readonly Dictionary<string, List<string>> JokersByRarity = new Dictionary<string, List<string>>();

        static void InitializeCompatibilityCollections()
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log("Initializing compatibility collections...");
            // Initialize LegendaryJokers
            foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
            {
                var jokerName = joker.ToString();
                LegendaryJokers.Add(jokerName.ToLower());
            }

            // Initialize JokersByRarity - wildcards at the END
            JokersByRarity["Common"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerCommon>())
            {
                JokersByRarity["Common"].Add(joker.ToString().ToLower());
            }

            JokersByRarity["Uncommon"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerUncommon>())
            {
                JokersByRarity["Uncommon"].Add(joker.ToString().ToLower());
            }

            JokersByRarity["Rare"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerRare>())
            {
                JokersByRarity["Rare"].Add(joker.ToString().ToLower());
            }

            JokersByRarity["Legendary"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
            {
                JokersByRarity["Legendary"].Add(joker.ToString().ToLower());
            }
        }
    }
}
