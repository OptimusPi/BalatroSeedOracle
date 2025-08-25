using System;
using System.Collections.Generic;
using System.Linq;
using Motely;
using BalatroSeedOracle.Helpers;

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
        public static readonly Dictionary<string, string> SpectralCards =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Vouchers = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BossBlinds = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> PlanetCards =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BoosterPacks =
            new Dictionary<string, string>();
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
            // Add wildcard entries first
            Jokers["anylegendary"] = "Any Legendary";
            Jokers["anyrare"] = "Any Rare";
            Jokers["anyuncommon"] = "Any Uncommon";
            Jokers["anycommon"] = "Any Common";
            Jokers["anyjoker"] = "Any Joker";

            // Common Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerCommon>())
            {
                var name = joker.ToString();
                var displayName = FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Uncommon Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerUncommon>())
            {
                var name = joker.ToString();
                var displayName = FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Rare Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerRare>())
            {
                var name = joker.ToString();
                var displayName = FormatDisplayName(name);
                Jokers[name] = displayName;
            }

            // Legendary Jokers
            foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
            {
                var name = joker.ToString();
                var displayName = FormatDisplayName(name);
                Jokers[name] = displayName;
            }
        }

        private static void InitializeTarotCards()
        {
            // Add wildcard entry first
            TarotCards["anytarot"] = "Any Tarot";

            foreach (var tarot in Enum.GetValues<MotelyTarotCard>())
            {
                var name = tarot.ToString();
                var displayName = FormatDisplayName(name);
                TarotCards[name] = displayName;
            }
        }

        private static void InitializeSpectralCards()
        {
            // Add wildcard entry first
            SpectralCards["anyspectral"] = "Any Spectral";

            foreach (var spectral in Enum.GetValues<MotelySpectralCard>())
            {
                var name = spectral.ToString();
                var displayName = FormatDisplayName(name);
                SpectralCards[name] = displayName;
            }
        }

        private static void InitializeVouchers()
        {
            foreach (var voucher in Enum.GetValues<MotelyVoucher>())
            {
                var name = voucher.ToString();
                var displayName = FormatDisplayName(name);
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
                var displayName = FormatDisplayName(name);
                Tags[name] = displayName;
            }
        }

        private static void InitializeBossBlinds()
        {
            foreach (var boss in Enum.GetValues<MotelyBossBlind>())
            {
                var name = boss.ToString();
                var displayName = FormatDisplayName(name);
                BossBlinds[name] = displayName;
            }
        }

        private static void InitializePlanetCards()
        {
            foreach (var planet in Enum.GetValues<MotelyPlanetCard>())
            {
                var name = planet.ToString();
                var displayName = FormatDisplayName(name);
                PlanetCards[name] = displayName;
            }
        }

        private static void InitializeBoosterPacks()
        {
            foreach (var pack in Enum.GetValues<MotelyBoosterPack>())
            {
                var name = pack.ToString();
                var displayName = FormatDisplayName(name);
                BoosterPacks[name] = displayName;
            }
        }

        private static void InitializeDecks()
        {
            Decks["Red"] = "Red Deck";
            Decks["Blue"] = "Blue Deck";
            Decks["Yellow"] = "Yellow Deck";
            Decks["Green"] = "Green Deck";
            Decks["Black"] = "Black Deck";
            Decks["Magic"] = "Magic Deck";
            Decks["Nebulous"] = "Nebulous Deck";
            Decks["Ghost"] = "Ghost Deck";
            Decks["Abandoned"] = "Abandoned Deck";
            Decks["Checkered"] = "Checkered Deck";
            Decks["Zodiac"] = "Zodiac Deck";
            Decks["Painted"] = "Painted Deck";
            Decks["Anagraph"] = "Anagraph Deck";
            Decks["Plasma"] = "Plasma Deck";
            Decks["Erratic"] = "Erratic Deck";
        }

        public static readonly Dictionary<string, string> DeckDescriptions = new()
        {
            { "Red", "+1 discards every round" },
            { "Blue", "+1 hands every round" },
            { "Yellow", "Start with extra $10" },
            { "Green", "$2 per remaining hand/discard\nEnd of round (no interest)" },
            { "Black", "+1 Joker slot\n-1 hand every round" },
            { "Magic", "Start run with the 'Crystal Ball' Voucher" },
            { "Nebulous", "Start run with a 'Telescope' Voucher" },
            { "Ghost", "Spectral cards may appear in the shop\nStart with a 'Hex' Spectral" },
            { "Abandoned", "No face cards in deck" },
            { "Checkered", "Start with 26 spades and 26 hearts" },
            { "Zodiac", "Start run with 'Tarot Merchant'\n'Planet Merchant' and 'Overstock'" },
            { "Painted", "+2 hand size\n-1 Joker slot" },
            { "Anagraph", "After defeating each boss blind,\nchange all cards to a random rank" },
            {
                "Plasma",
                "Balance chips and mult\nwhen calculating score for played hand\nX2 base blind size"
            },
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
                { "riffraff", "Riff-raff" },
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
                // Wildcard entries
                { "anyjoker", "Any Joker" },
                { "anycommon", "Any Common" },
                { "anyuncommon", "Any Uncommon" },
                { "anyrare", "Any Rare" },
                { "anylegendary", "Any Legendary" },
            };

            if (jokerDisplayNames.TryGetValue(spriteName.ToLowerInvariant(), out var displayName))
            {
                return displayName;
            }

            // Fallback: try to format the sprite name
            return FormatDisplayName(spriteName);
        }

        private static string FormatDisplayName(string enumName)
        {
            // Special cases that need custom formatting
            var specialCases = new Dictionary<string, string>
            {
                // Numbers
                { "EightBall", "8 Ball" },
                { "Cloud9", "Cloud 9" },
                { "OopsAll6s", "Oops! All 6s" },
                // Tarot cards with "The" prefix
                { "TheFool", "The Fool" },
                { "TheMagician", "The Magician" },
                { "TheHighPriestess", "The High Priestess" },
                { "TheEmpress", "The Empress" },
                { "TheEmperor", "The Emperor" },
                { "TheHierophant", "The Hierophant" },
                { "TheLovers", "The Lovers" },
                { "TheChariot", "The Chariot" },
                { "TheHermit", "The Hermit" },
                { "TheWheel", "The Wheel of Fortune" },
                { "TheJustice", "Justice" },
                { "TheHangedMan", "The Hanged Man" },
                { "TheDeath", "Death" },
                { "TheTemperance", "Temperance" },
                { "TheDevil", "The Devil" },
                { "TheTower", "The Tower" },
                { "TheStar", "The Star" },
                { "TheMoon", "The Moon" },
                { "TheSun", "The Sun" },
                { "TheJudgement", "Judgement" },
                { "TheWorld", "The World" },
                // Spectral cards
                { "Soul", "The Soul" },
                { "TheSoul", "The Soul" },
                { "BlackHole", "Black Hole" },
                { "DejaVu", "Deja Vu" },
                // Boss blinds
                { "TheArm", "The Arm" },
                { "TheOx", "The Ox" },
                { "TheSerpent", "The Serpent" },
                { "TheEye", "The Eye" },
                { "TheClub", "The Club" },
                { "TheFlint", "The Flint" },
                { "TheHead", "The Head" },
                { "TheNeedle", "The Needle" },
                { "TheWall", "The Wall" },
                { "TheGoad", "The Goad" },
                { "TheWater", "The Water" },
                { "TheWindow", "The Window" },
                { "TheManacle", "The Manacle" },
                { "ThePlant", "The Plant" },
                { "TheVerdant", "The Verdant Leaf" },
                { "VioletVessel", "Violet Vessel" },
                { "CrimsonHeart", "Crimson Heart" },
                { "AmberAcorn", "Amber Acorn" },
                { "CeruleanBell", "Cerulean Bell" },
                // Multi-word special formatting
                { "ToTheMoon", "To the Moon" },
                { "ToDoList", "To Do List" },
                { "RiffRaff", "Riff-raff" },
                { "MailInRebate", "Mail-In Rebate" },
                { "WheelofFortune", "Wheel of Fortune" },
                { "SockandBuskin", "Sock and Buskin" },
                { "DriversLicense", "Driver's License" },
                { "PlanetX", "Planet X" },
                // Tag special cases
                { "D6Tag", "D6 Tag" },
                // Other special formatting
                { "MrBones", "Mr. Bones" },
                { "ChaostheClown", "Chaos the Clown" },
            };

            if (specialCases.TryGetValue(enumName, out var special))
            {
                return special;
            }

            // Add spaces before capital letters (except the first one)
            var result = string.Empty;
            for (int i = 0; i < enumName.Length; i++)
            {
                if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1]))
                {
                    result += " ";
                }

                result += enumName[i];
            }

            return result;
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

        /// <summary>
        /// Formats a pack name for display
        /// </summary>
        public static string FormatPackName(string packName)
        {
            return packName switch
            {
                "Arcana" => "Arcana Pack",
                "JumboArcana" => "Jumbo Arcana Pack",
                "MegaArcana" => "Mega Arcana Pack",
                "Celestial" => "Celestial Pack",
                "JumboCelestial" => "Jumbo Celestial Pack",
                "MegaCelestial" => "Mega Celestial Pack",
                "Spectral" => "Spectral Pack",
                "JumboSpectral" => "Jumbo Spectral Pack",
                "MegaSpectral" => "Mega Spectral Pack",
                "Buffoon" => "Buffoon Pack",
                "JumboBuffoon" => "Jumbo Buffoon Pack",
                "MegaBuffoon" => "Mega Buffoon Pack",
                "Standard" => "Standard Pack",
                "JumboStandard" => "Jumbo Standard Pack",
                "MegaStandard" => "Mega Standard Pack",
                _ => packName
            };
        }

        /// <summary>
        /// Formats a playing card for display
        /// </summary>
        public static string FormatPlayingCard(string cardStr)
        {
            if (cardStr.Length >= 2)
            {
                var suit = cardStr[0] switch
                {
                    'C' => "Clubs",
                    'D' => "Diamonds",
                    'H' => "Hearts",
                    'S' => "Spades",
                    _ => "Unknown"
                };

                var rank = cardStr.Substring(1) switch
                {
                    "2" => "2",
                    "3" => "3",
                    "4" => "4",
                    "5" => "5",
                    "6" => "6",
                    "7" => "7",
                    "8" => "8",
                    "9" => "9",
                    "10" => "10",
                    "J" => "Jack",
                    "Q" => "Queen",
                    "K" => "King",
                    "A" => "Ace",
                    _ => cardStr.Substring(1)
                };

                return $"{rank} of {suit}";
            }
            return cardStr;
        }

        /// <summary>
        /// Formats a joker name for display (analyzer output)
        /// </summary>
        public static string FormatJokerNameForAnalyzer(string name)
        {
            // Use existing display name logic, but add special cases for analyzer
            if (Jokers.TryGetValue(name, out var displayName))
                return displayName;
                
            // Fallback to FormatDisplayName if not found
            return FormatDisplayName(name);
        }

        /// <summary>
        /// Formats a tarot name for display (analyzer output)
        /// </summary>
        public static string FormatTarotNameForAnalyzer(string name)
        {
            if (TarotCards.TryGetValue(name, out var displayName))
                return FormatDisplayName(displayName);
                
            return FormatDisplayName(name);
        }

        /// <summary>
        /// Formats a voucher name for display (analyzer output)
        /// </summary>
        public static string FormatVoucherNameForAnalyzer(string name)
        {
            if (Vouchers.TryGetValue(name, out var displayName))
                return displayName;
                
            return FormatDisplayName(name);
        }

        /// <summary>
        /// Formats a tag name for display (analyzer output)
        /// </summary>
        public static string FormatTagNameForAnalyzer(string name)
        {
            if (Tags.TryGetValue(name, out var displayName))
                return displayName;
                
            return FormatDisplayName(name);
        }

        public static readonly List<string> LegendaryJokers = new List<string>();
        public static readonly Dictionary<string, List<string>> JokersByRarity =
            new Dictionary<string, List<string>>();

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
            JokersByRarity["Common"].Add("anycommon");
            JokersByRarity["Common"].Add("anyjoker");

            JokersByRarity["Uncommon"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerUncommon>())
            {
                JokersByRarity["Uncommon"].Add(joker.ToString().ToLower());
            }
            JokersByRarity["Uncommon"].Add("anyuncommon");

            JokersByRarity["Rare"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerRare>())
            {
                JokersByRarity["Rare"].Add(joker.ToString().ToLower());
            }
            JokersByRarity["Rare"].Add("anyrare");

            JokersByRarity["Legendary"] = new List<string>();
            foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
            {
                JokersByRarity["Legendary"].Add(joker.ToString().ToLower());
            }
            JokersByRarity["Legendary"].Add("anylegendary");
        }
    }
}
