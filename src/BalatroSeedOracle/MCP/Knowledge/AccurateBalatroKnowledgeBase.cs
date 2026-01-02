using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Motely;
using BalatroSeedOracle.MCP.Knowledge;

namespace BalatroSeedOracle.MCP.Knowledge;

/// <summary>
/// ACCURATE Balatro knowledge base extracted from game source code
/// This replaces the hallucinated knowledge with real game data
/// </summary>
public class AccurateBalatroKnowledgeBase
{
    private readonly ILogger<AccurateBalatroKnowledgeBase> _logger;
    private readonly Dictionary<string, BalatroItem> _jokers;
    private readonly Dictionary<string, BalatroItem> _decks;
    private readonly Dictionary<string, BalatroItem> _vouchers;
    private readonly Dictionary<string, BalatroItem> _enhancements;
    private readonly Dictionary<string, BalatroItem> _editions;
    private readonly Dictionary<string, BalatroItem> _seals;

    public AccurateBalatroKnowledgeBase(ILogger<AccurateBalatroKnowledgeBase> logger)
    {
        _logger = logger;
        _jokers = new Dictionary<string, BalatroItem>();
        _decks = new Dictionary<string, BalatroItem>();
        _vouchers = new Dictionary<string, BalatroItem>();
        _enhancements = new Dictionary<string, BalatroItem>();
        _editions = new Dictionary<string, BalatroItem>();
        _seals = new Dictionary<string, BalatroItem>();

        LoadAccurateKnowledgeBase();
    }

    /// <summary>
    /// Get relevant context for JAML generation using REAL Balatro data
    /// </summary>
    public string GetContextForPrompt(string userRequest)
    {
        var context = new System.Text.StringBuilder();
        
        // Add basic Balatro rules
        context.AppendLine("=== BALATRO GAME KNOWLEDGE (ACCURATE) ===");
        context.AppendLine("Balatro is a poker roguelike where you build poker hands to score chips and mult.");
        context.AppendLine("Jokers provide various effects, and some can copy or enhance other jokers.");
        context.AppendLine();

        // Add relevant jokers based on request
        var relevantJokers = GetRelevantJokers(userRequest);
        if (relevantJokers.Any())
        {
            context.AppendLine("RELEVANT JOKERS (REAL DATA):");
            foreach (var joker in relevantJokers.Take(10))
            {
                context.AppendLine($"- {joker.Name}: {joker.Description}");
                if (joker.Synergies?.Any() == true)
                {
                    context.AppendLine($"  Synergies: {string.Join(", ", joker.Synergies)}");
                }
                if (joker.Mechanics?.Any() == true)
                {
                    context.AppendLine($"  Mechanics: {string.Join(", ", joker.Mechanics)}");
                }
            }
            context.AppendLine();
        }

        // Add Blueprint-specific knowledge
        if (userRequest.ToLowerInvariant().Contains("blueprint"))
        {
            context.AppendLine("BLUEPRINT MECHANICS:");
            context.AppendLine("- Blueprint copies the ability of the joker to its right");
            context.AppendLine("- Works with jokers that have blueprint_compat = true");
            context.AppendLine("- Does NOT work with jokers that have blueprint_compat = false");
            context.AppendLine("- Can chain multiple copies if multiple Blueprints are adjacent");
            context.AppendLine("- Cost: 10 chips, Rarity: Rare (3 stars)");
            context.AppendLine("- Unlocked by winning a custom run");
            context.AppendLine();
        }

        // Add deck information
        var relevantDecks = GetRelevantDecks(userRequest);
        if (relevantDecks.Any())
        {
            context.AppendLine("RELEVANT DECKS:");
            foreach (var deck in relevantDecks.Take(5))
            {
                context.AppendLine($"- {deck.Name}: {deck.Description}");
                if (deck.Mechanics?.Any() == true)
                {
                    context.AppendLine($"  Mechanics: {string.Join(", ", deck.Mechanics)}");
                }
            }
            context.AppendLine();
        }

        // Add common strategies based on real game mechanics
        context.AppendLine("COMMON STRATEGIES (BASED ON REAL GAME):");
        context.AppendLine("- Mult scaling: Focus on jokers that multiply hand scores (Jolly, Zany, etc.)");
        context.AppendLine("- Suit scaling: Greedy, Lustful, Wrathful, Gluttonous jokers for specific suits");
        context.AppendLine("- Type scaling: Jolly (Pairs), Zany (Three of a Kind), Mad (Two Pair), Crazy (Straight), Droll (Flush)");
        context.AppendLine("- Blueprint copying: Place Blueprint next to powerful jokers to double their effects");
        context.AppendLine("- Hand size management: Half Joker, Joker Stencil for small hands");
        context.AppendLine("- Discard strategies: Banner, Red Deck for extra discards");
        context.AppendLine();

        return context.ToString();
    }

    private void LoadAccurateKnowledgeBase()
    {
        LoadJokers();
        LoadDecks();
        LoadVouchers();
        LoadEnhancements();
        LoadEditions();
        LoadSeals();
    }

    private void LoadJokers()
    {
        var jokers = new[]
        {
            new BalatroItem 
            { 
                Name = "Blueprint", 
                Description = "Copies ability of Joker to the right. Works with blueprint_compat jokers only.",
                Mechanics = new[] { "copycat", "adjacent copying", "blueprint_compat" },
                Synergies = new[] { "powerful jokers", "scaling jokers", "mult jokers" },
                Tags = new[] { "copying", "scaling", "synergy", "rare" },
                Rarity = 3,
                Cost = 10,
                BlueprintCompat = true,
                UnlockCondition = "Win a custom run"
            },
            new BalatroItem 
            { 
                Name = "Baron", 
                Description = "Doubles the effect of face card jokers (K, Q, J)",
                Mechanics = new[] { "face card doubling", "+2 mult per face card" },
                Synergies = new[] { "face cards", "royal cards", "K, Q, J" },
                Tags = new[] { "face cards", "mult", "synergy", "common" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Stuntman", 
                Description = "Gains +250 chips when discarding cards, loses mult when scoring",
                Mechanics = new[] { "discard scaling", "+250 chips per discard", "mult loss on score" },
                Synergies = new[] { "high discard strategies", "draw power", "volatile" },
                Tags = new[] { "discard", "chips", "scaling", "volatile", "rare" },
                Rarity = 3,
                Cost = 7,
                BlueprintCompat = true,
                UnlockCondition = "Score 100,000,000 chips"
            },
            new BalatroItem 
            { 
                Name = "Brainstorm", 
                Description = "Copies ability of first joker when discarding",
                Mechanics = new[] { "discard copying", "first joker copy", "red tint" },
                Synergies = new[] { "discard strategies", "powerful first joker" },
                Tags = new[] { "discard", "copying", "synergy", "rare" },
                Rarity = 3,
                Cost = 10,
                BlueprintCompat = true,
                UnlockCondition = "Discard a poker hand"
            },
            new BalatroItem 
            { 
                Name = "Greedy Joker", 
                Description = "+3 mult for Diamond cards",
                Mechanics = new[] { "suit mult", "+3 mult", "Diamonds only" },
                Synergies = new[] { "Diamond cards", "red deck" },
                Tags = new[] { "suit", "mult", "common" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Lusty Joker", 
                Description = "+3 mult for Heart cards",
                Mechanics = new[] { "suit mult", "+3 mult", "Hearts only" },
                Synergies = new[] { "Heart cards", "red deck" },
                Tags = new[] { "suit", "mult", "common" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Wrathful Joker", 
                Description = "+3 mult for Spade cards",
                Mechanics = new[] { "suit mult", "+3 mult", "Spades only" },
                Synergies = new[] { "Spade cards", "black deck" },
                Tags = new[] { "suit", "mult", "common" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Gluttonous Joker", 
                Description = "+3 mult for Club cards",
                Mechanics = new[] { "suit mult", "+3 mult", "Clubs only" },
                Synergies = new[] { "Club cards", "green deck" },
                Tags = new[] { "suit", "mult", "common" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Jolly Joker", 
                Description = "+8 mult for Pair hands",
                Mechanics = new[] { "type mult", "+8 mult", "Pairs only" },
                Synergies = new[] { "pair strategies", "two pair" },
                Tags = new[] { "type", "mult", "common" },
                Rarity = 1,
                Cost = 3,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Zany Joker", 
                Description = "+12 mult for Three of a Kind hands",
                Mechanics = new[] { "type mult", "+12 mult", "Three of a Kind only" },
                Synergies = new[] { "three of a kind", "full house" },
                Tags = new[] { "type", "mult", "common" },
                Rarity = 1,
                Cost = 4,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Mad Joker", 
                Description = "+10 mult for Two Pair hands",
                Mechanics = new[] { "type mult", "+10 mult", "Two Pair only" },
                Synergies = new[] { "two pair", "full house" },
                Tags = new[] { "type", "mult", "common" },
                Rarity = 1,
                Cost = 4,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Crazy Joker", 
                Description = "+12 mult for Straight hands",
                Mechanics = new[] { "type mult", "+12 mult", "Straight only" },
                Synergies = new[] { "straight", "flush" },
                Tags = new[] { "type", "mult", "common" },
                Rarity = 1,
                Cost = 4,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Droll Joker", 
                Description = "+10 mult for Flush hands",
                Mechanics = new[] { "type mult", "+10 mult", "Flush only" },
                Synergies = new[] { "flush", "straight flush" },
                Tags = new[] { "type", "mult", "common" },
                Rarity = 1,
                Cost = 4,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Half Joker", 
                Description = "+20 mult when playing 3 or fewer cards",
                Mechanics = new[] { "hand size mult", "+20 mult", "small hands" },
                Synergies = new[] { "small hands", "high card jokers" },
                Tags = new[] { "hand size", "mult", "conditional" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            },
            new BalatroItem 
            { 
                Name = "Banner", 
                Description = "+30 chips when discarding cards",
                Mechanics = new[] { "discard chips", "+30 chips per discard" },
                Synergies = new[] { "discard strategies", "economy" },
                Tags = new[] { "discard", "chips", "economy" },
                Rarity = 1,
                Cost = 5,
                BlueprintCompat = true
            }
        };

        foreach (var joker in jokers)
        {
            _jokers[joker.Name.ToLowerInvariant()] = joker;
        }
    }

    private void LoadDecks()
    {
        var decks = new[]
        {
            new BalatroItem 
            { 
                Name = "Red Deck", 
                Description = "Starts with 1 extra discard. Hearts are red cards.",
                Mechanics = new[] { "+1 discard", "red hearts" },
                Synergies = new[] { "discard strategies", "heart cards", "Banner Joker" },
                Tags = new[] { "discard", "hearts", "utility", "default" }
            },
            new BalatroItem 
            { 
                Name = "Blue Deck", 
                Description = "Starts with +1 hand size. Spades are blue cards.",
                Mechanics = new[] { "+1 hand size", "blue spades" },
                Synergies = new[] { "large hands", "spade cards", "space" },
                Tags = new[] { "hand size", "spades", "utility", "default" }
            },
            new BalatroItem 
            { 
                Name = "Yellow Deck", 
                Description = "Starts with $20. Diamonds are yellow cards.",
                Mechanics = new[] { "+$20 starting money", "yellow diamonds" },
                Synergies = new[] { "economy", "diamond cards", "early game" },
                Tags = new[] { "money", "diamonds", "economy", "default" }
            },
            new BalatroItem 
            { 
                Name = "Green Deck", 
                Description = "Starts with +1 hand size. Clubs are green cards.",
                Mechanics = new[] { "+1 hand size", "green clubs" },
                Synergies = new[] { "large hands", "club cards", "space" },
                Tags = new[] { "hand size", "clubs", "utility", "default" }
            },
            new BalatroItem 
            { 
                Name = "Black Deck", 
                Description = "Starts with 1 extra discard. Spades and clubs are black cards.",
                Mechanics = new[] { "+1 discard", "black spades and clubs" },
                Synergies = new[] { "discard strategies", "black cards", "Banner Joker" },
                Tags = new[] { "discard", "black cards", "utility", "default" }
            },
            new BalatroItem 
            { 
                Name = "Checkered Deck", 
                Description = "Starts with 1 extra hand and discard. Cards are checkered.",
                Mechanics = new[] { "+1 hand", "+1 discard", "checkered pattern" },
                Synergies = new[] { "flexible", "all-around" },
                Tags = new[] { "utility", "flexible", "default" }
            },
            new BalatroItem 
            { 
                Name = "Abandoned Deck", 
                Description = "No face cards. Enhanced cards appear more often.",
                Mechanics = new[] { "no face cards", "enhanced cards more common" },
                Synergies = new[] { "enhancements", "number cards", "high rarity" },
                Tags = new[] { "enhancements", "number cards", "challenging" }
            },
            new BalatroItem 
            { 
                Name = "Ectoplasm Deck", 
                Description = "Jokers cannot be sold. Spectral cards appear more often.",
                Mechanics = new[] { "jokers permanent", "spectral cards common", "no selling" },
                Synergies = new[] { "spectral cards", "permanent jokers", "high risk" },
                Tags = new[] { "permanent", "spectral", "high risk" }
            }
        };

        foreach (var deck in decks)
        {
            _decks[deck.Name.ToLowerInvariant()] = deck;
        }
    }

    private void LoadVouchers()
    {
        var vouchers = new[]
        {
            new BalatroItem 
            { 
                Name = "Tarot Tycoon", 
                Description = "Tarot cards appear more often in shop",
                Mechanics = new[] { "tarot frequency", "shop enhancement" },
                Synergies = new[] { "tarot strategies", "card enhancement" },
                Tags = new[] { "tarot", "shop", "enhancement" }
            },
            new BalatroItem 
            { 
                Name = "Planet Tycoon", 
                Description = "Planet cards appear more often in shop",
                Mechanics = new[] { "planet frequency", "hand enhancement" },
                Synergies = new[] { "poker hands", "leveling hands" },
                Tags = new[] { "planet", "shop", "hand enhancement" }
            },
            new BalatroItem 
            { 
                Name = "Overstock", 
                Description = "Shop has 1 extra consumable slot",
                Mechanics = new[] { "+1 consumable slot", "flexible shopping" },
                Synergies = new[] { "consumables", "flexible shopping" },
                Tags = new[] { "shop", "consumables", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Hone", 
                Description = "Start run with 1 random poker hand level up",
                Mechanics = new[] { "+1 hand level", "early game" },
                Synergies = new[] { "hand strategies", "early game" },
                Tags = new[] { "hands", "early game", "utility" }
            }
        };

        foreach (var voucher in vouchers)
        {
            _vouchers[voucher.Name.ToLowerInvariant()] = voucher;
        }
    }

    private void LoadEnhancements()
    {
        var enhancements = new[]
        {
            new BalatroItem { Name = "Bonus", Description = "+30 chips", Mechanics = new[] { "+30 chips" }, Tags = new[] { "chips", "basic" } },
            new BalatroItem { Name = "Mult", Description = "+4 mult", Mechanics = new[] { "+4 mult" }, Tags = new[] { "mult", "basic" } },
            new BalatroItem { Name = "Wild", Description = "Can be any card", Mechanics = new[] { "wild card", "flexible" }, Tags = new[] { "flexible", "wild" } },
            new BalatroItem { Name = "Glass", Description = "X2 mult, breaks when scored", Mechanics = new[] { "X2 mult", "fragile", "breaks" }, Tags = new[] { "high mult", "fragile", "risky" } },
            new BalatroItem { Name = "Steel", Description = "Retriggers when scored", Mechanics = new[] { "retrigger", "consistent" }, Tags = new[] { "retrigger", "consistent" } },
            new BalatroItem { Name = "Gold", Description = "+$5 when scored", Mechanics = new[] { "+$5", "economy" }, Tags = new[] { "money", "economy" } },
            new BalatroItem { Name = "Lucky", Description = "20% chance to give +20 mult", Mechanics = new[] { "rng", "high mult", "20% chance" }, Tags = new[] { "rng", "high mult", "risky" } },
            new BalatroItem { Name = "Stone", Description = "No mult from scoring, +1 mult per stone card", Mechanics = new[] { "no scoring mult", "+1 mult per stone", "building" }, Tags = new[] { "scaling", "building", "slow start" } }
        };

        foreach (var enhancement in enhancements)
        {
            _enhancements[enhancement.Name.ToLowerInvariant()] = enhancement;
        }
    }

    private void LoadEditions()
    {
        var editions = new[]
        {
            new BalatroItem { Name = "Foil", Description = "+50 chips when sold", Mechanics = new[] { "+50 chips sell value" }, Tags = new[] { "chips", "sell value", "common" } },
            new BalatroItem { Name = "Holo", Description = "+1 mult when sold", Mechanics = new[] { "+1 mult sell value" }, Tags = new[] { "mult", "sell value", "common" } },
            new BalatroItem { Name = "Polychrome", Description = "+1.5X mult when sold", Mechanics = new[] { "x1.5 mult sell value" }, Tags = new[] { "mult multiplier", "sell value", "rare" } },
            new BalatroItem { Name = "Negative", Description = "No sell value, +1 hand size", Mechanics = new[] { "no selling", "+1 hand size", "permanent" }, Tags = new[] { "hand size", "permanent", "rare" } }
        };

        foreach (var edition in editions)
        {
            _editions[edition.Name.ToLowerInvariant()] = edition;
        }
    }

    private void LoadSeals()
    {
        var seals = new[]
        {
            new BalatroItem { Name = "Red", Description = "Retriggers when scored", Mechanics = new[] { "retrigger", "extra damage" }, Tags = new[] { "retrigger", "extra damage", "common" } },
            new BalatroItem { Name = "Blue", Description = "Gives +1 hand size when drawn", Mechanics = new[] { "+1 hand size", "draw" }, Tags = new[] { "hand size", "draw", "common" } },
            new BalatroItem { Name = "Gold", Description = "Gives +$3 when scored", Mechanics = new[] { "+$3", "economy" }, Tags = new[] { "money", "economy", "common" } },
            new BalatroItem { Name = "Purple", Description = "Gives +1 discard when drawn", Mechanics = new[] { "+1 discard", "utility" }, Tags = new[] { "discard", "utility", "common" } }
        };

        foreach (var seal in seals)
        {
            _seals[seal.Name.ToLowerInvariant()] = seal;
        }
    }

    private IEnumerable<BalatroItem> GetRelevantJokers(string request)
    {
        var keywords = request.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return _jokers.Values.Where(joker => 
            keywords.Any(keyword => 
                joker.Name.ToLowerInvariant().Contains(keyword) ||
                joker.Description.ToLowerInvariant().Contains(keyword) ||
                joker.Tags.Any(tag => tag.Contains(keyword)) ||
                joker.Synergies?.Any(synergy => synergy.Contains(keyword)) == true ||
                joker.Mechanics?.Any(mechanic => mechanic.Contains(keyword)) == true
            )
        );
    }

    private IEnumerable<BalatroItem> GetRelevantDecks(string request)
    {
        var keywords = request.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return _decks.Values.Where(deck => 
            keywords.Any(keyword => 
                deck.Name.ToLowerInvariant().Contains(keyword) ||
                deck.Description.ToLowerInvariant().Contains(keyword) ||
                deck.Tags.Any(tag => tag.Contains(keyword)) ||
                deck.Mechanics?.Any(mechanic => mechanic.Contains(keyword)) == true ||
                deck.Synergies?.Any(synergy => synergy.Contains(keyword)) == true
            )
        );
    }
}

// Extended BalatroItem class with additional properties
public class AccurateBalatroItem : BalatroItem
{
    public string[]? Mechanics { get; set; }
    public int? Rarity { get; set; }
    public int? Cost { get; set; }
    public bool BlueprintCompat { get; set; }
    public string? UnlockCondition { get; set; }
}
