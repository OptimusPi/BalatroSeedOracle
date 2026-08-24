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

            // Initialize effect-text descriptions from en-us localization (#12)
            InitializeDescriptions();
        }

        public static readonly Dictionary<string, string> Jokers = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> TarotCards =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> SpectralCards =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Vouchers =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BossBlinds =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> PlanetCards =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BoosterPacks =
            new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Decks = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Stakes = new Dictionary<string, string>();

        /// <summary>
        /// Card-effect descriptions keyed by item name (lowercased), for the card-picker
        /// hover tooltip (issue #12). Empty by default — populate from a data file or
        /// hand-authored entries. The tooltip mechanism gracefully shows only known
        /// metadata when no description exists here, so no fabricated effect text ships.
        /// </summary>
        public static readonly Dictionary<string, string> Descriptions =
            new Dictionary<string, string>();

        /// <summary>Returns the effect description for an item name, or null if unknown.</summary>
        public static string? GetDescription(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            return Descriptions.TryGetValue(name.ToLowerInvariant(), out var d) ? d : null;
        }

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
        public static readonly Dictionary<string, List<string>> JokersByRarity =
            new Dictionary<string, List<string>>();

        static void InitializeCompatibilityCollections()
        {
            Helpers.DebugLogger.Log("Initializing compatibility collections...");
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

        private static void InitializeDescriptions()
        {
            Descriptions["8 ball"] = "X in X chance for each\nplayed 8 to create a\nTarot card when scored\n(Must have room)";
            Descriptions["abandoned deck"] = "Start run with\nno Face Cards\nin your deck";
            Descriptions["abstract joker"] = "+X Mult for\neach Joker card";
            Descriptions["acrobat"] = "xX Mult on final\nhand of round";
            Descriptions["amber acorn"] = "Flips and shuffles\nall Joker cards";
            Descriptions["anaglyph deck"] = "After defeating each\nBoss Blind, gain a Double Tag";
            Descriptions["ancient joker"] = "Each played card with\nX suit gives\nxX Mult when scored,\nsuit changes at end of round";
            Descriptions["ankh"] = "Create a copy of a\nrandom Joker, destroy\nall other Jokers";
            Descriptions["antimatter"] = "+1 Joker Slot";
            Descriptions["arcana pack"] = "Choose X of up to\nX Tarot cards to\nbe used immediately";
            Descriptions["arrowhead"] = "Played cards with\nSpade suit give\n+X Chips when scored";
            Descriptions["astronomer"] = "All Planet cards and\nCelestial Packs in\nthe shop are free";
            Descriptions["aura"] = "Add Foil, Holographic,\nor Polychrome effect to\n1 selected card in hand";
            Descriptions["banner"] = "+X Chips for\neach remaining\ndiscard";
            Descriptions["baron"] = "Each King\nheld in hand\ngives xX Mult";
            Descriptions["base"] = "No extra effects";
            Descriptions["baseball card"] = "Uncommon Jokers\neach give xX Mult";
            Descriptions["black deck"] = "+X Joker slot\n-X hand\nevery round";
            Descriptions["black hole"] = "Upgrade every\npoker hand\nby 1 level";
            Descriptions["black stake"] = "Shop can have Eternal Jokers\n(Can't be sold or destroyed)\nApplies all previous Stakes";
            Descriptions["black sticker"] = "Used this Joker\nto win on Black\nStake difficulty";
            Descriptions["blackboard"] = "xX Mult if all\ncards held in hand\nare Spades or Clubs";
            Descriptions["blank"] = "Does nothing?";
            Descriptions["bloodstone"] = "X in X chance for\nplayed cards with\nHeart suit to give\nxX Mult when scored";
            Descriptions["blue deck"] = "+X hand\nevery round";
            Descriptions["blue joker"] = "+X Chips for each\nremaining card in deck";
            Descriptions["blue seal"] = "Creates the Planet card\nfor final played poker hand\nof round if held in hand\n(Must have room)";
            Descriptions["blue stake"] = "-1 Discard\nApplies all previous Stakes";
            Descriptions["blue sticker"] = "Used this Joker\nto win on Blue\nStake difficulty";
            Descriptions["blueprint"] = "Copies ability of\nJoker to the right";
            Descriptions["bootstraps"] = "+X Mult for every\n$X you have";
            Descriptions["boss tag"] = "Rerolls the\nBoss Blind";
            Descriptions["brainstorm"] = "Copies the ability\nof leftmost Joker";
            Descriptions["buffoon pack"] = "Choose X of up to\nX Joker cards";
            Descriptions["buffoon tag"] = "Gives a free\nMega Buffoon Pack";
            Descriptions["bull"] = "+X Chips for\neach $1 you have";
            Descriptions["burglar"] = "When Blind is selected,\ngain +X Hands and\nlose all discards";
            Descriptions["burnt joker"] = "Upgrade the level of\nthe first discarded\npoker hand each round";
            Descriptions["business card"] = "Played face cards have\na X in X chance to\ngive $2 when scored";
            Descriptions["campfire"] = "This Joker gains xX Mult\nfor each card sold, resets\nwhen Boss Blind is defeated";
            Descriptions["canio"] = "This Joker gains xX Mult\nwhen a face card\nis destroyed";
            Descriptions["card sharp"] = "xX Mult if played\npoker hand has already\nbeen played this round";
            Descriptions["cartomancer"] = "Create a Tarot card\nwhen Blind is selected\n(Must have room)";
            Descriptions["castle"] = "This Joker gains +X Chips\nper discarded X card,\nsuit changes every round";
            Descriptions["cavendish"] = "xX Mult\nX in X chance this\ncard is destroyed\nat end of round";
            Descriptions["celestial pack"] = "Choose X of up to\nX Planet cards to\nbe used immediately";
            Descriptions["ceremonial dagger"] = "When Blind is selected,\ndestroy Joker to the right\nand permanently add double\nits sell value to this Mult";
            Descriptions["ceres"] = "Levels up Four of a Kind\n+X Mult and +Y Chips per level";
            Descriptions["certificate"] = "When round begins,\nadd a random playing\ncard with a random\nseal to your hand";
            Descriptions["cerulean bell"] = "Forces 1 card to\nalways be selected";
            Descriptions["chaos the clown"] = "X free Reroll\nper shop";
            Descriptions["charm tag"] = "Gives a free\nMega Arcana Pack";
            Descriptions["checkered deck"] = "Start run with\n26 Spades and\n26 Hearts in deck";
            Descriptions["chicot"] = "Disables effect of\nevery Boss Blind";
            Descriptions["clearance sale"] = "All cards and packs in\nshop are X% off";
            Descriptions["clever joker"] = "+X Chips if played\nhand contains\na X";
            Descriptions["cloud 9"] = "Earn $X for each\n9 in your full deck\nat end of round";
            Descriptions["constellation"] = "This Joker gains\nxX Mult every time\na Planet card is used";
            Descriptions["coupon tag"] = "Initial cards and\nbooster packs in next\nshop are free";
            Descriptions["crafty joker"] = "+X Chips if played\nhand contains\na X";
            Descriptions["crazy joker"] = "+X Mult if played\nhand contains\na X";
            Descriptions["credit card"] = "Go up to\n-$X in debt";
            Descriptions["crimson heart"] = "One random Joker\ndisabled every hand";
            Descriptions["cryptid"] = "Create X copies of\n1 selected card\nin your hand";
            Descriptions["crystal ball"] = "+1 consumable slot";
            Descriptions["d6 tag"] = "Rerolls in next shop\nstart at $0";
            Descriptions["death"] = "Select X cards,\nconvert the left card\ninto the right card\n(Drag to rearrange)";
            Descriptions["debuffed"] = "All abilities\nare disabled";
            Descriptions["deja vu"] = "Add a Red Seal\nto 1 selected\ncard in your hand";
            Descriptions["delayed gratification"] = "Earn $X per discard if\nno discards are used\nby end of the round";
            Descriptions["devious joker"] = "+X Chips if played\nhand contains\na X";
            Descriptions["diet cola"] = "Sell this card to\ncreate a free Voucher";
            Descriptions["director's cut"] = "Reroll Boss Blind\n1 time per Ante,\n$X per roll";
            Descriptions["dna"] = "If first hand of round\nhas only 1 card, add a\npermanent copy to deck\nand draw it to hand";
            Descriptions["double tag"] = "Gives a copy of the\nnext selected Tag\nDouble Tag excluded";
            Descriptions["driver's license"] = "xX Mult if you have\nat least 16 Enhanced\ncards in your full deck";
            Descriptions["droll joker"] = "+X Mult if played\nhand contains\na X";
            Descriptions["drunkard"] = "+X discard\neach round";
            Descriptions["dusk"] = "Retrigger all played\ncards in final\nhand of round";
            Descriptions["earth"] = "Levels up Full House\n+X Mult and +Y Chips per level";
            Descriptions["economy tag"] = "Doubles your money\n(Max of $X)";
            Descriptions["ectoplasm"] = "Add Negative to\na random Joker,\n-X hand size";
            Descriptions["egg"] = "Gains $X of\nsell value at\nend of round";
            Descriptions["eris"] = "Levels up Flush Five\n+X Mult and +Y Chips per level";
            Descriptions["erosion"] = "+X Mult for each\ncard below X\nin your full deck";
            Descriptions["erratic deck"] = "All Ranks and\nSuits in deck\nare randomized";
            Descriptions["eternal"] = "Can't be sold\nor destroyed";
            Descriptions["ethereal tag"] = "Gives a free\nSpectral Pack";
            Descriptions["even steven"] = "Played cards with\neven rank give\n+X Mult when scored\n(10, 8, 6, 4, 2)";
            Descriptions["faceless joker"] = "Earn $X if X or\nmore face cards\nare discarded\nat the same time";
            Descriptions["familiar"] = "Destroy 1 random\ncard in your hand, add\nX random Enhanced face\ncards to your hand";
            Descriptions["fibonacci"] = "Each played Ace,\n2, 3, 5, or 8 gives\n+X Mult when scored";
            Descriptions["flash card"] = "This Joker gains +X Mult\nper reroll in the shop";
            Descriptions["flower pot"] = "xX Mult if poker\nhand contains a\nDiamond card, Club card,\nHeart card, and Spade card";
            Descriptions["foil"] = "+X Chips";
            Descriptions["foil tag"] = "Next base edition shop\nJoker is free and\nbecomes Foil";
            Descriptions["fortune teller"] = "+X Mult per Tarot\ncard used this run";
            Descriptions["four fingers"] = "All Flushes and\nStraights can be\nmade with 4 cards";
            Descriptions["garbage tag"] = "Gives $X per unused\ndiscard this run";
            Descriptions["ghost deck"] = "Spectral cards may\nappear in the shop,\nstart with a Hex card";
            Descriptions["gift card"] = "Add $X of sell value\nto every Joker and\nConsumable card at\nend of round";
            Descriptions["glass card"] = "xX Mult\nX in X chance to\ndestroy card";
            Descriptions["glass joker"] = "This Joker gains xX Mult\nfor every Glass Card\nthat is destroyed";
            Descriptions["glow up"] = "Foil, Holographic, and\nPolychrome cards\nappear X more often";
            Descriptions["gluttonous joker"] = "Played cards with\nClubs suit give\n+X Mult when scored";
            Descriptions["gold card"] = "$X if this\ncard is held in hand\nat end of round";
            Descriptions["gold seal"] = "Earn $3 when this\ncard is played\nand scores";
            Descriptions["gold stake"] = "Shop can have Rental Jokers\n(Costs $3 per round)\nApplies all previous Stakes";
            Descriptions["gold sticker"] = "Used this Joker\nto win on Gold\nStake difficulty";
            Descriptions["golden joker"] = "Earn $X at\nend of round";
            Descriptions["golden ticket"] = "Played Gold cards\nearn $X when scored";
            Descriptions["grabber"] = "Permanently\ngain +X hand\nper round";
            Descriptions["greedy joker"] = "Played cards with\nDiamonds suit give\n+X Mult when scored";
            Descriptions["green deck"] = "At end of each Round:\n$X per remaining Hand\n$X per remaining Discard\nEarn no Interest";
            Descriptions["green joker"] = "+X Mult per hand played\n-X Mult per discard";
            Descriptions["green stake"] = "Required score scales\nfaster for each Ante\nApplies all previous Stakes";
            Descriptions["green sticker"] = "Used this Joker\nto win on Green\nStake difficulty";
            Descriptions["grim"] = "Destroy 1 random\ncard in your hand,\nadd X random Enhanced\nAces to your hand";
            Descriptions["gros michel"] = "+X Mult\nX in X chance this\ncard is destroyed\nat end of round";
            Descriptions["hack"] = "Retrigger\neach played\n2, 3, 4, or 5";
            Descriptions["half joker"] = "+X Mult if played\nhand contains\nX or fewer cards";
            Descriptions["hallucination"] = "X in X chance to create\na Tarot card when any\nBooster Pack is opened\n(Must have room)";
            Descriptions["handy tag"] = "Gives $X per played\nhand this run";
            Descriptions["hanging chad"] = "Retrigger first played\ncard used in scoring\nX additional times";
            Descriptions["hex"] = "Add Polychrome to a\nrandom Joker, destroy\nall other Jokers";
            Descriptions["hieroglyph"] = "-X Ante,\n-X hand\neach round";
            Descriptions["hiker"] = "Every played card\npermanently gains\n+X Chips when scored";
            Descriptions["hit the road"] = "This Joker gains xX Mult\nfor every Jack\ndiscarded this round";
            Descriptions["hologram"] = "This Joker gains xX Mult\nevery time a playing card\nis added to your deck";
            Descriptions["holographic"] = "+X Mult";
            Descriptions["holographic tag"] = "Next base edition shop\nJoker is free and\nbecomes Holographic";
            Descriptions["hone"] = "Foil, Holographic, and\nPolychrome cards\nappear X more often";
            Descriptions["ice cream"] = "+X Chips\n-X Chips for\nevery hand played";
            Descriptions["illusion"] = "Playing cards in shop\nmay have an Enhancement,\nEdition, and/or a Seal";
            Descriptions["immolate"] = "Destroys X random\ncards in hand,\ngain $X";
            Descriptions["incantation"] = "Destroy 1 random\ncard in your hand, add X\nrandom Enhanced numbered\ncards to your hand";
            Descriptions["investment tag"] = "After defeating\nthe Boss Blind,\ngain $X";
            Descriptions["invisible joker"] = "After X rounds,\nsell this card to\nDuplicate a random Joker";
            Descriptions["joker"] = "+X Mult";
            Descriptions["joker stencil"] = "X1 Mult for each\nempty Joker slot\nJoker Stencil included";
            Descriptions["jolly joker"] = "+X Mult if played\nhand contains\na Pair";
            Descriptions["judgement"] = "Creates a random\nJoker card\n(Must have room)";
            Descriptions["juggle tag"] = "+X hand size\nnext round";
            Descriptions["juggler"] = "+X hand size";
            Descriptions["jumbo arcana pack"] = "Choose X of up to\nX Tarot cards to\nbe used immediately";
            Descriptions["jumbo buffoon pack"] = "Choose X of up to\nX Joker cards";
            Descriptions["jumbo celestial pack"] = "Choose X of up to\nX Planet cards to\nbe used immediately";
            Descriptions["jumbo spectral pack"] = "Choose X of up to\nX Spectral cards to\nbe used immediately";
            Descriptions["jumbo standard pack"] = "Choose X of up to\nX Playing cards to\nadd to your deck";
            Descriptions["jupiter"] = "Levels up Flush\n+X Mult and +Y Chips per level";
            Descriptions["justice"] = "Enhances 1 selected\ncard into a\nLucky Card";
            Descriptions["liquidation"] = "All cards and packs in\nshop are X% off";
            Descriptions["loyalty card"] = "xX Mult every\nX hands played";
            Descriptions["luchador"] = "Sell this card to\ndisable the current\nBoss Blind";
            Descriptions["lucky card"] = "X in X chance\nfor +X Mult\nX in X chance\nto win $X";
            Descriptions["lucky cat"] = "This Joker gains xX Mult\nevery time a Lucky card\nsuccessfully triggers";
            Descriptions["lusty joker"] = "Played cards with\nHearts suit give\n+X Mult when scored";
            Descriptions["mad joker"] = "+X Mult if played\nhand contains\na X";
            Descriptions["madness"] = "When Small Blind or Big Blind\nis selected, gain xX Mult\nand destroy a random Joker";
            Descriptions["magic deck"] = "Start run with Crystal Ball\nvoucher and 2 copies of The Fool";
            Descriptions["magic trick"] = "Playing cards can\nbe purchased\nfrom the shop";
            Descriptions["mail-in rebate"] = "Earn $X for each\ndiscarded X, rank\nchanges every round";
            Descriptions["marble joker"] = "Adds one Stone card\nto deck when\nBlind is selected";
            Descriptions["mars"] = "Levels up Four of a Kind\n+X Mult and +Y Chips per level";
            Descriptions["matador"] = "Earn $X if played\nhand triggers the\nBoss Blind ability";
            Descriptions["medium"] = "Add a Purple Seal\nto 1 selected\ncard in your hand";
            Descriptions["mega arcana pack"] = "Choose X of up to\nX Tarot cards to\nbe used immediately";
            Descriptions["mega buffoon pack"] = "Choose X of up to\nX Joker cards";
            Descriptions["mega celestial pack"] = "Choose X of up to\nX Planet cards to\nbe used immediately";
            Descriptions["mega spectral pack"] = "Choose X of up to\nX Spectral cards to\nbe used immediately";
            Descriptions["mega standard pack"] = "Choose X of up to\nX Playing cards to\nadd to your deck";
            Descriptions["mercury"] = "Levels up Pair\n+X Mult and +Y Chips per level";
            Descriptions["merry andy"] = "+X discards\neach round,\n-X hand size";
            Descriptions["meteor tag"] = "Gives a free\nMega Celestial Pack";
            Descriptions["midas mask"] = "All played face cards\nbecome Gold cards\nwhen scored";
            Descriptions["mime"] = "Retrigger all\ncard held in\nhand abilities";
            Descriptions["money tree"] = "Raise the cap on\ninterest earned in\neach round to $X";
            Descriptions["mr. bones"] = "Prevents Death\nif chips scored\nare at least 25%\nof required chips\n(self destructs)";
            Descriptions["mult card"] = "+X Mult";
            Descriptions["mystic summit"] = "+X Mult when\nX discards\nremaining";
            Descriptions["nacho tong"] = "Permanently\ngain +X hand\nper round";
            Descriptions["nebula deck"] = "Start run with Telescope\nvoucher\n-1 consumable slot";
            Descriptions["negative"] = "+X Joker slot";
            Descriptions["negative tag"] = "Next base edition shop\nJoker is free and\nbecomes Negative";
            Descriptions["neptune"] = "Levels up Straight Flush\n+X Mult and +Y Chips per level";
            Descriptions["obelisk"] = "This Joker gains xX Mult\nper consecutive hand played\nwithout playing your\nmost played poker hand";
            Descriptions["observatory"] = "Planet cards in your\nconsumable area give\nxX Mult for their\nspecified poker hand";
            Descriptions["odd todd"] = "Played cards with\nodd rank give\n+X Chips when scored\n(A, 9, 7, 5, 3)";
            Descriptions["omen globe"] = "Spectral cards may\nappear in any of\nthe Arcana Packs";
            Descriptions["onyx agate"] = "Played cards with\nClub suit give\n+X Mult when scored";
            Descriptions["oops! all 6s"] = "Doubles all listed\nprobabilities\n(ex: 1 in 3 -> 2 in 3)";
            Descriptions["orange stake"] = "Shop can have Perishable Jokers\n(Debuffed after 5 Rounds)\nApplies all previous Stakes";
            Descriptions["orange sticker"] = "Used this Joker\nto win on Orange\nStake difficulty";
            Descriptions["orbital tag"] = "Upgrade a poker hand\nby X levels";
            Descriptions["ouija"] = "Converts all cards\nin hand to a single\nrandom rank\n-1 hand size";
            Descriptions["overstock"] = "+1 card slot\navailable in shop";
            Descriptions["overstock plus"] = "+1 card slot\navailable in shop";
            Descriptions["paint brush"] = "+X hand size";
            Descriptions["painted deck"] = "+X hand size,\n-X Joker slot";
            Descriptions["palette"] = "+X hand size";
            Descriptions["pareidolia"] = "All cards are\nconsidered\nface cards";
            Descriptions["perishable"] = "Debuffed after\nX rounds";
            Descriptions["perkeo"] = "Creates a Negative copy of\n1 random consumable\ncard in your possession\nat the end of the shop";
            Descriptions["petroglyph"] = "-X Ante,\n-X discard\neach round";
            Descriptions["photograph"] = "First played face\ncard gives xX Mult\nwhen scored";
            Descriptions["pinned"] = "This Joker stays\npinned to the\nleftmost position";
            Descriptions["planet merchant"] = "Planet cards appear\nX more frequently\nin the shop";
            Descriptions["planet tycoon"] = "Planet cards appear\nX more frequently\nin the shop";
            Descriptions["planet x"] = "Levels up Five of a Kind\n+X Mult and +Y Chips per level";
            Descriptions["plasma deck"] = "Balance Chips and\nMult when calculating\nscore for played hand\nX base Blind size";
            Descriptions["pluto"] = "Levels up High Card\n+X Mult and +Y Chips per level";
            Descriptions["polychrome"] = "xX Mult";
            Descriptions["polychrome tag"] = "Next base edition shop\nJoker is free and\nbecomes Polychrome";
            Descriptions["popcorn"] = "+X Mult\n-X Mult per\nround played";
            Descriptions["purple seal"] = "Creates a Tarot card\nwhen discarded\n(Must have room)";
            Descriptions["purple stake"] = "Required score scales\nfaster for each Ante\nApplies all previous Stakes";
            Descriptions["purple sticker"] = "Used this Joker\nto win on Purple\nStake difficulty";
            Descriptions["raised fist"] = "Adds double the rank\nof lowest ranked card\nheld in hand to Mult";
            Descriptions["ramen"] = "xX Mult,\nloses xX Mult\nper card discarded";
            Descriptions["rare tag"] = "Shop has a free\nRare Joker";
            Descriptions["recyclomancy"] = "Permanently\ngain +X discard\neach round";
            Descriptions["red card"] = "This Joker gains\n+X Mult when any\nBooster Pack is skipped";
            Descriptions["red deck"] = "+X discard\nevery round";
            Descriptions["red seal"] = "Retrigger this\ncard 1 time";
            Descriptions["red stake"] = "Small Blind gives\nno reward money\nApplies all previous Stakes";
            Descriptions["red sticker"] = "Used this Joker\nto win on Red\nStake difficulty";
            Descriptions["rental"] = "Lose $X at\nend of round";
            Descriptions["reroll glut"] = "Rerolls cost\n$X less";
            Descriptions["reroll surplus"] = "Rerolls cost\n$X less";
            Descriptions["reserved parking"] = "Each face card\nheld in hand has\na X in X chance\nto give $X";
            Descriptions["retcon"] = "Reroll Boss Blind\nunlimited times,\n$X per roll";
            Descriptions["ride the bus"] = "This Joker gains +X Mult\nper consecutive hand\nplayed without a\nscoring face card";
            Descriptions["riff-raff"] = "When Blind is selected,\ncreate X Common Jokers\n(Must have room)";
            Descriptions["rocket"] = "Earn $X at end of round\nPayout increases by $X\nwhen Boss Blind is defeated";
            Descriptions["rough gem"] = "Played cards with\nDiamond suit earn\n$X when scored";
            Descriptions["runner"] = "Gains +X Chips\nif played hand\ncontains a Straight";
            Descriptions["satellite"] = "Earn $X at end of\nround per unique Planet\ncard used this run";
            Descriptions["saturn"] = "Levels up Straight\n+X Mult and +Y Chips per level";
            Descriptions["scary face"] = "Played face cards\ngive +X Chips\nwhen scored";
            Descriptions["scholar"] = "Played Aces\ngive +X Chips\nand +X Mult\nwhen scored";
            Descriptions["séance"] = "If poker hand is a\nX, create a\nrandom Spectral card\n(Must have room)";
            Descriptions["seed money"] = "Raise the cap on\ninterest earned in\neach round to $X";
            Descriptions["seeing double"] = "xX Mult if played\nhand has a scoring\nClub card and a scoring\ncard of any other suit";
            Descriptions["seltzer"] = "Retrigger all\ncards played for\nthe next X hands";
            Descriptions["shoot the moon"] = "Each Queen\nheld in hand\ngives +X Mult";
            Descriptions["shortcut"] = "Allows Straights to be\nmade with gaps of 1 rank\n(ex: 10 8 6 5 3)";
            Descriptions["showman"] = "Joker, Tarot, Planet,\nand Spectral cards may\nappear multiple times";
            Descriptions["sigil"] = "Converts all cards\nin hand to a single\nrandom suit";
            Descriptions["sixth sense"] = "If first hand of round is\na single 6, destroy it and\ncreate a Spectral card\n(Must have room)";
            Descriptions["sly joker"] = "+X Chips if played\nhand contains\na X";
            Descriptions["smeared joker"] = "Hearts and Diamonds\ncount as the same suit,\nSpades and Clubs\ncount as the same suit";
            Descriptions["smiley face"] = "Played face cards\ngive +X Mult\nwhen scored";
            Descriptions["sock and buskin"] = "Retrigger all\nplayed face cards";
            Descriptions["space joker"] = "X in X chance to\nupgrade level of\nplayed poker hand";
            Descriptions["spare trousers"] = "This Joker gains +X Mult\nif played hand contains\na Two Pair";
            Descriptions["spectral pack"] = "Choose X of up to\nX Spectral cards to\nbe used immediately";
            Descriptions["speed tag"] = "Gives $X per skipped\nBlind this run";
            Descriptions["splash"] = "Every played card\ncounts in scoring";
            Descriptions["square joker"] = "This Joker gains +X Chips\nif played hand has\nexactly 4 cards";
            Descriptions["standard pack"] = "Choose X of up to\nX Playing cards to\nadd to your deck";
            Descriptions["standard tag"] = "Gives a free\nMega Standard Pack";
            Descriptions["steel card"] = "xX Mult\nwhile this card\nstays in hand";
            Descriptions["steel joker"] = "Gives xX Mult\nfor each Steel Card\nin your full deck";
            Descriptions["stone card"] = "+X Chips\nno rank or suit";
            Descriptions["stone joker"] = "Gives +X Chips for\neach Stone Card\nin your full deck";
            Descriptions["strength"] = "Increases rank of\nup to X selected\ncards by 1";
            Descriptions["stuntman"] = "+X Chips,\n-X hand size";
            Descriptions["supernova"] = "Adds the number of times\npoker hand has been\nplayed this run to Mult";
            Descriptions["superposition"] = "Create a Tarot card if\npoker hand contains an\nAce and a Straight\n(Must have room)";
            Descriptions["swashbuckler"] = "Adds the sell value\nof all other owned\nJokers to Mult";
            Descriptions["talisman"] = "Add a Gold Seal\nto 1 selected\ncard in your hand";
            Descriptions["tarot merchant"] = "Tarot cards appear\nX more frequently\nin the shop";
            Descriptions["tarot tycoon"] = "Tarot cards appear\nX more frequently\nin the shop";
            Descriptions["telescope"] = "Celestial Packs always\ncontain the Planet\ncard for your most\nplayed poker hand";
            Descriptions["temperance"] = "Gives the total sell\nvalue of all current\nJokers (Max of $X)";
            Descriptions["the arm"] = "Decrease level of\nplayed poker hand";
            Descriptions["the chariot"] = "Enhances 1 selected\ncard into a\nSteel Card";
            Descriptions["the club"] = "All Club cards\nare debuffed";
            Descriptions["the devil"] = "Enhances 1 selected\ncard into a\nGold Card";
            Descriptions["the duo"] = "xX Mult if played\nhand contains\na Pair";
            Descriptions["the emperor"] = "Creates up to X\nrandom Tarot cards\n(Must have room)";
            Descriptions["the empress"] = "Enhances 2\nselected cards to\nMult Cards";
            Descriptions["the eye"] = "No repeat hand\ntypes this round";
            Descriptions["the family"] = "xX Mult if played\nhand contains\na Four of a Kind";
            Descriptions["the fish"] = "Cards drawn face down\nafter each hand played";
            Descriptions["the flint"] = "Base Chips and\nMult are halved";
            Descriptions["the fool"] = "Creates the last\nTarot or Planet card\nused during this run\n(The Fool excluded)";
            Descriptions["the goad"] = "All Spade cards\nare debuffed";
            Descriptions["the hanged man"] = "Destroys up to\nX selected cards";
            Descriptions["the head"] = "All Heart cards\nare debuffed";
            Descriptions["the hermit"] = "Doubles money\n(Max of $X)";
            Descriptions["the hierophant"] = "Enhances 2\nselected cards to\nBonus Cards";
            Descriptions["the high priestess"] = "Creates up to X\nrandom Planet cards\n(Must have room)";
            Descriptions["the hook"] = "Discards 2 random\ncards per hand played";
            Descriptions["the house"] = "First hand is\ndrawn face down";
            Descriptions["the idol"] = "Each played X of X suit\ngives xX Mult when scored\nCard changes every round";
            Descriptions["the lovers"] = "Enhances 1 selected\ncard into a\nWild Card";
            Descriptions["the magician"] = "Enhances 2\nselected cards to\nLucky Cards";
            Descriptions["the manacle"] = "-1 Hand Size";
            Descriptions["the mark"] = "All face cards are\ndrawn face down";
            Descriptions["the moon"] = "Converts up to\n1 selected cards\nto Clubs";
            Descriptions["the mouth"] = "Play only 1 hand\ntype this round";
            Descriptions["the needle"] = "Play only 1 hand";
            Descriptions["the order"] = "xX Mult if played\nhand contains\na Straight";
            Descriptions["the ox"] = "Playing a X\nsets money to $0";
            Descriptions["the pillar"] = "Cards played previously\nthis Ante are debuffed";
            Descriptions["the plant"] = "All face cards\nare debuffed";
            Descriptions["the psychic"] = "Must play 5 cards";
            Descriptions["the serpent"] = "After Play or Discard,\nalways draw 3 cards";
            Descriptions["the soul"] = "Creates a\nLegendary Joker\n(Must have room)";
            Descriptions["the star"] = "Converts up to\n1 selected cards\nto Spades";
            Descriptions["the sun"] = "Converts up to\n1 selected cards\nto Hearts";
            Descriptions["the tooth"] = "Lose $1 per\ncard played";
            Descriptions["the tower"] = "Enhances 1 selected\ncard into a\nStone Card";
            Descriptions["the tribe"] = "xX Mult if played\nhand contains\na Flush";
            Descriptions["the trio"] = "xX Mult if played\nhand contains\na Three of a Kind";
            Descriptions["the wall"] = "Extra large blind";
            Descriptions["the water"] = "Start with\n0 discards";
            Descriptions["the wheel"] = "1 in 7 cards get\ndrawn face down";
            Descriptions["the wheel of fortune"] = "X in X chance to add\nFoil, Holographic, or\nPolychrome edition\nto a random Joker";
            Descriptions["the window"] = "All Diamond cards\nare debuffed";
            Descriptions["the world"] = "Converts up to\n1 selected cards\nto Diamonds";
            Descriptions["throwback"] = "xX Mult for each\nBlind skipped this run";
            Descriptions["to do list"] = "Earn $X if poker hand\nis a X, changes\nat end of round";
            Descriptions["to the moon"] = "Earn an extra $X of\ninterest for every $5 you\nhave at end of round";
            Descriptions["top-up tag"] = "Create up to X\nCommon Jokers\n(Must have room)";
            Descriptions["trading card"] = "If first discard of round\nhas only 1 card, destroy\nit and earn $X";
            Descriptions["trance"] = "Add a Blue Seal\nto 1 selected\ncard in your hand";
            Descriptions["triboulet"] = "Played Kings and\nQueens each give\nxX Mult when scored";
            Descriptions["troubadour"] = "+X hand size,\n-X hand each round";
            Descriptions["turtle bean"] = "+X hand size,\nreduces by\nX every round";
            Descriptions["uncommon tag"] = "Shop has a free\nUncommon Joker";
            Descriptions["uranus"] = "Levels up Two Pair\n+X Mult and +Y Chips per level";
            Descriptions["vagabond"] = "Create a Tarot card\nif hand is played\nwith $X or less";
            Descriptions["vampire"] = "This Joker gains xX Mult\nper scoring Enhanced card played,\nremoves card Enhancement";
            Descriptions["venus"] = "Levels up Three of a Kind\n+X Mult and +Y Chips per level";
            Descriptions["verdant leaf"] = "All cards debuffed\nuntil 1 Joker sold";
            Descriptions["violet vessel"] = "Very large blind";
            Descriptions["voucher tag"] = "Adds one Voucher\nto the next shop";
            Descriptions["walkie talkie"] = "Each played 10 or 4\ngives +X Chips and\n+X Mult when scored";
            Descriptions["wasteful"] = "Permanently\ngain +X discard\neach round";
            Descriptions["wee joker"] = "This Joker gains\n+X Chips when each\nplayed 2 is scored";
            Descriptions["white stake"] = "Base Difficulty";
            Descriptions["white sticker"] = "Used this Joker\nto win on White\nStake difficulty";
            Descriptions["wild card"] = "Can be used\nas any suit";
            Descriptions["wily joker"] = "+X Chips if played\nhand contains\na X";
            Descriptions["wraith"] = "Creates a random\nRare Joker,\nsets money to $0";
            Descriptions["wrathful joker"] = "Played cards with\nSpades suit give\n+X Mult when scored";
            Descriptions["yellow deck"] = "Start with\nextra $X";
            Descriptions["yorick"] = "This Joker gains\nxX Mult every X cards\ndiscarded";
            Descriptions["zany joker"] = "+X Mult if played\nhand contains\na Three of a Kind";
            Descriptions["zodiac deck"] = "Start run with Tarot Merchant,\nPlanet Merchant,\nand Overstock";
        }
    }
}
