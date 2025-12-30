using System.Text.Json;
using Microsoft.Extensions.Logging;
using Motely;

namespace BalatroSeedOracle.MCP.Knowledge;

/// <summary>
/// RAG knowledge base for JAML Genie
/// Provides Balatro context for JAML generation
/// </summary>
public class BalatroKnowledgeBase
{
    private readonly ILogger<BalatroKnowledgeBase> _logger;
    private readonly Dictionary<string, BalatroItem> _jokers;
    private readonly Dictionary<string, BalatroItem> _decks;
    private readonly Dictionary<string, BalatroItem> _vouchers;
    private readonly Dictionary<string, BalatroItem> _enhancements;
    private readonly Dictionary<string, BalatroItem> _editions;
    private readonly Dictionary<string, BalatroItem> _seals;

    public BalatroKnowledgeBase(ILogger<BalatroKnowledgeBase> logger)
    {
        _logger = logger;
        _jokers = new Dictionary<string, BalatroItem>();
        _decks = new Dictionary<string, BalatroItem>();
        _vouchers = new Dictionary<string, BalatroItem>();
        _enhancements = new Dictionary<string, BalatroItem>();
        _editions = new Dictionary<string, BalatroItem>();
        _seals = new Dictionary<string, BalatroItem>();

        LoadKnowledgeBase();
    }

    /// <summary>
    /// Get relevant context for JAML generation
    /// </summary>
    public string GetContextForPrompt(string userRequest)
    {
        var context = new System.Text.StringBuilder();
        
        // Add basic Balatro rules
        context.AppendLine("=== BALATRO GAME KNOWLEDGE ===");
        context.AppendLine("Balatro is a poker roguelike where you build poker hands to score chips.");
        context.AppendLine("You collect jokers, vouchers, and build decks to achieve high scores.");
        context.AppendLine();

        // Add relevant jokers based on request
        var relevantJokers = GetRelevantJokers(userRequest);
        if (relevantJokers.Any())
        {
            context.AppendLine("RELEVANT JOKERS:");
            foreach (var joker in relevantJokers.Take(10)) // Limit context size
            {
                context.AppendLine($"- {joker.Name}: {joker.Description}");
                if (joker.Synergies?.Any() == true)
                {
                    context.AppendLine($"  Synergizes with: {string.Join(", ", joker.Synergies)}");
                }
            }
            context.AppendLine();
        }

        // Add relevant decks
        var relevantDecks = GetRelevantDecks(userRequest);
        if (relevantDecks.Any())
        {
            context.AppendLine("RELEVANT DECKS:");
            foreach (var deck in relevantDecks.Take(5))
            {
                context.AppendLine($"- {deck.Name}: {deck.Description}");
            }
            context.AppendLine();
        }

        // Add common patterns
        context.AppendLine("COMMON STRATEGIES:");
        context.AppendLine("- Mult scaling: Focus on jokers that multiply hand scores");
        context.AppendLine("- Chip scaling: Focus on jokers that add base chips");
        context.AppendLine("- Hand enhancement: Look for jokers that enhance specific poker hands");
        context.AppendLine("- Deck synergy: Match jokers to deck strengths (e.g., Flush decks with flush jokers)");
        context.AppendLine();

        return context.ToString();
    }

    private void LoadKnowledgeBase()
    {
        // Load from JSON files or embed knowledge
        LoadJokers();
        LoadDecks();
        LoadVouchers();
        LoadEnhancements();
        LoadEditions();
        LoadSeals();
    }

    private void LoadJokers()
    {
        // Essential jokers with their mechanics
        var jokers = new[]
        {
            new BalatroItem 
            { 
                Name = "Blueprint", 
                Description = "Doubles the effect of other jokers",
                Synergies = new[] { "mult jokers", "chip jokers", "hand enhancement jokers" },
                Tags = new[] { "scaling", "multiplier", "synergy" }
            },
            new BalatroItem 
            { 
                Name = "Baron", 
                Description = "Doubles the effect of face card jokers",
                Synergies = new[] { "face cards", "K, Q, J" },
                Tags = new[] { "face cards", "multiplier" }
            },
            new BalatroItem 
            { 
                Name = "Stuntman", 
                Description = "Gains mult when discarding cards, loses mult when scoring",
                Synergies = new[] { "high discard strategies", "draw power" },
                Tags = new[] { "discard", "scaling", "volatile" }
            },
            new BalatroItem 
            { 
                Name = "Supernova", 
                Description = "Gives X3 mult for each hand played, self-destructs after 5 hands",
                Synergies = new[] { "fast games", "burst damage" },
                Tags = new[] { "burst", "high mult", "limited uses" }
            },
            new BalatroItem 
            { 
                Name = "Cavendish", 
                Description = "If hand contains a 2, gives X4 mult and destroys itself",
                Synergies = new[] { "low card strategies", "twos" },
                Tags = new[] { "condition", "high mult", "consumable" }
            },
            new BalatroItem 
            { 
                Name = "Fortune Teller", 
                Description = "Gives +X2 mult for each tarot card in possession",
                Synergies = new[] { "tarot cards", "card consumption" },
                Tags = new[] { "scaling", "tarot", "collection" }
            },
            new BalatroItem 
            { 
                Name = "Ramen", 
                Description = "Retriggers all jokers in play when a hand is scored",
                Synergies = new[] { "trigger jokers", "chain effects" },
                Tags = new[] { "retrigger", "chain", "combo" }
            },
            new BalatroItem 
            { 
                Name = "Sock and Buskin", 
                Description = "Each played card gives +2 mult when scored",
                Synergies = new[] { "high card count hands", "5 card hands" },
                Tags = new[] { "card count", "mult", "hand size" }
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
                Description = "Starts with 1 extra discard, hearts are red cards",
                Synergies = new[] { "discard strategies", "heart cards" },
                Tags = new[] { "discard", "hearts", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Blue Deck", 
                Description = "Starts with +1 hand size, spades are blue cards",
                Synergies = new[] { "large hands", "spade cards" },
                Tags = new[] { "hand size", "spades", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Yellow Deck", 
                Description = "Starts with $20, diamonds are yellow cards",
                Synergies = new[] { "economy", "diamond cards" },
                Tags = new[] { "money", "diamonds", "economy" }
            },
            new BalatroItem 
            { 
                Name = "Green Deck", 
                Description = "Starts with +1 hand size, clubs are green cards",
                Synergies = new[] { "large hands", "club cards" },
                Tags = new[] { "hand size", "clubs", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Black Deck", 
                Description = "Starts with 1 extra discard, spades and clubs are black cards",
                Synergies = new[] { "discard strategies", "black cards" },
                Tags = new[] { "discard", "black cards", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Checkered Deck", 
                Description = "Starts with 1 extra hand and discard, cards are checkered",
                Synergies = new[] { "utility", "flexible" },
                Tags = new[] { "utility", "flexible", "all-around" }
            },
            new BalatroItem 
            { 
                Name = "Zebra Deck", 
                Description = "Only black and red cards, no face cards",
                Synergies = new[] { "number cards", "black/red strategies" },
                Tags = new[] { "number cards", "limited types", "challenging" }
            },
            new BalatroItem 
            { 
                Name = "Abandoned Deck", 
                Description = "No face cards, enhanced cards appear more often",
                Synergies = new[] { "enhanced cards", "number cards" },
                Tags = new[] { "enhancements", "number cards", "high rarity" }
            },
            new BalatroItem 
            { 
                Name = "Ectoplasm Deck", 
                Description = "Jokers cannot be sold, spectral cards appear more often",
                Synergies = new[] { "permanent jokers", "spectral cards" },
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
                Synergies = new[] { "tarot strategies", "card enhancement" },
                Tags = new[] { "tarot", "shop", "enhancement" }
            },
            new BalatroItem 
            { 
                Name = "Planet Tycoon", 
                Description = "Planet cards appear more often in shop",
                Synergies = new[] { "poker hand enhancement", "leveling hands" },
                Tags = new[] { "planet", "shop", "hand enhancement" }
            },
            new BalatroItem 
            { 
                Name = "Overstock", 
                Description = "Shop has 1 extra consumable slot",
                Synergies = new[] { "consumable strategies", "flexible shopping" },
                Tags = new[] { "shop", "consumables", "utility" }
            },
            new BalatroItem 
            { 
                Name = "Hone", 
                Description = "Start run with 1 random poker hand level up",
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
            new BalatroItem { Name = "Bonus", Description = "+30 chips", Tags = new[] { "chips", "basic" } },
            new BalatroItem { Name = "Mult", Description = "+4 mult", Tags = new[] { "mult", "basic" } },
            new BalatroItem { Name = "Wild", Description = "Can be any card", Tags = new[] { "flexible", "wild" } },
            new BalatroItem { Name = "Glass", Description = "X2 mult, breaks when scored", Tags = new[] { "high mult", "fragile" } },
            new BalatroItem { Name = "Steel", Description = "Retriggers when scored", Tags = new[] { "retrigger", "consistent" } },
            new BalatroItem { Name = "Gold", Description = "+$5 when scored", Tags = new[] { "money", "economy" } },
            new BalatroItem { Name = "Lucky", Description = "20% chance to give +20 mult", Tags = new[] { "rng", "high mult" } },
            new BalatroItem { Name = "Stone", Description = "No mult from scoring, +1 mult per stone card", Tags = new[] { "scaling", "building" } }
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
            new BalatroItem { Name = "Foil", Description = "+50 chips when sold", Tags = new[] { "chips", "sell value" } },
            new BalatroItem { Name = "Holo", Description = "+1 mult when sold", Tags = new[] { "mult", "sell value" } },
            new BalatroItem { Name = "Polychrome", Description = "+1.5X mult when sold", Tags = new[] { "mult multiplier", "sell value" } },
            new BalatroItem { Name = "Negative", Description = "No sell value, +1 hand size", Tags = new[] { "hand size", "permanent" } }
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
            new BalatroItem { Name = "Red", Description = "Retriggers when scored", Tags = new[] { "retrigger", "extra damage" } },
            new BalatroItem { Name = "Blue", Description = "Gives +1 hand size when drawn", Tags = new[] { "hand size", "draw" } },
            new BalatroItem { Name = "Gold", Description = "Gives +$3 when scored", Tags = new[] { "money", "economy" } },
            new BalartoItem { Name = "Purple", Description = "Gives +1 discard when drawn", Tags = new[] { "discard", "utility" } }
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
                joker.Synergies?.Any(synergy => synergy.Contains(keyword)) == true
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
                deck.Synergies?.Any(synergy => synergy.Contains(keyword)) == true
            )
        );
    }
}

public class BalatroItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[]? Synergies { get; set; }
    public string[]? Requirements { get; set; }
    public int? Rarity { get; set; }
}
