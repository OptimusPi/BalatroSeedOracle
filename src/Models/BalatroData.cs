using System;
using System.Collections.Generic;
using System.Linq;
using Motely;

namespace Oracle.Models;

/// <summary>
/// Complete Balatro game data for ouija.json configuration
/// Uses Motely.Core enums as the source of truth for item names
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
        { "Negative", "Negative" }
    };

    // Legacy mappings for items with inconsistent names in example.ouija.json
    public static readonly Dictionary<string, string> LegacyNameMappings = new()
    {
        // Joker ID fixes
        { "OopsAll6s", "OopsAll6s" }, // Actually correct in Motely
        { "Oops_All_6s", "OopsAll6s" }, // Map underscore version to correct
        
        // Voucher ID fixes
        { "BlankVoucher", "Blank" },
        
        // Other special cases
        { "SoulJoker", "Spectral" }, // SoulJoker actually refers to The Soul spectral card
    };

    private static void InitializeJokers()
    {
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
        foreach (var tarot in Enum.GetValues<MotelyTarotCard>())
        {
            var name = tarot.ToString();
            var displayName = FormatDisplayName(name);
            TarotCards[name] = displayName;
        }
    }

    private static void InitializeSpectralCards()
    {
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
        // TODO: Update when Motely deck enums are available
        Decks["RedDeck"] = "Red Deck";
        Decks["BlueDeck"] = "Blue Deck";
        Decks["YellowDeck"] = "Yellow Deck";
        Decks["GreenDeck"] = "Green Deck";
        Decks["BlackDeck"] = "Black Deck";
        Decks["MagicDeck"] = "Magic Deck";
        Decks["NebulousDeck"] = "Nebulous Deck";
        Decks["GhostDeck"] = "Ghost Deck";
        Decks["AbandonedDeck"] = "Abandoned Deck";
        Decks["CheckeredDeck"] = "Checkered Deck";
        Decks["ZodiacDeck"] = "Zodiac Deck";
        Decks["PaintedDeck"] = "Painted Deck";
        Decks["AnagraphDeck"] = "Anagraph Deck";
        Decks["PlasmaDeck"] = "Plasma Deck";
        Decks["ErraticDeck"] = "Erratic Deck";
    }

    private static void InitializeStakes()
    {
        // TODO: Update when Motely stake enums are available
        Stakes["WhiteStake"] = "White Stake";
        Stakes["RedStake"] = "Red Stake";
        Stakes["GreenStake"] = "Green Stake";
        Stakes["BlackStake"] = "Black Stake";
        Stakes["BlueStake"] = "Blue Stake";
        Stakes["PurpleStake"] = "Purple Stake";
        Stakes["OrangeStake"] = "Orange Stake";
        Stakes["GoldStake"] = "Gold Stake";
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
            { "perkeo", "Perkeo" }
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
            { "TheSoul", "The Soul" },
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
            { "RiffRaff", "Riff-Raff" },
            { "MailInRebate", "Mail-In Rebate" },
            { "WheelofFortune", "Wheel of Fortune" },
            { "SockandBuskin", "Sock and Buskin" },
            { "DriversLicense", "Driver's License" },
            { "PlanetX", "Planet X" },
            
            // Tag special cases
            { "D6Tag", "D6 Tag" },
            
            // Other special formatting
            { "MrBones", "Mr. Bones" },
            { "ChaostheClown", "Chaos the Clown" }
        };

        if (specialCases.TryGetValue(enumName, out var special))
            return special;

        // Add spaces before capital letters (except the first one)
        var result = string.Empty;
        for (int i = 0; i < enumName.Length; i++)
        {
            if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1]))
                result += " ";
            result += enumName[i];
        }

        return result;
    }

    /// <summary>
    /// Gets the correct item ID, handling legacy mappings
    /// </summary>
    public static string GetCorrectItemId(string itemId)
    {
        if (LegacyNameMappings.TryGetValue(itemId, out var correctId))
            return correctId;
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
            _ => false
        };
    }

    // Additional properties for backwards compatibility
    public static readonly List<string> LegendaryJokers = new List<string>();
    public static readonly Dictionary<string, List<string>> JokersByRarity = new Dictionary<string, List<string>>();

    // Initialize these in the static constructor
    static void InitializeCompatibilityCollections()
    {
        // Initialize LegendaryJokers
        foreach (var joker in Enum.GetValues<MotelyJokerLegendary>())
        {
            LegendaryJokers.Add(joker.ToString().ToLower());
        }

        // Initialize JokersByRarity
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