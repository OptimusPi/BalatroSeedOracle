using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Oracle.Services
{
    /// <summary>
    /// Provides JSON schema and validation data for Ouija configs
    /// </summary>
    public static class OuijaSchemaService
    {
        // Cache the enum values
        private static readonly string[] JokerNames = Enum.GetNames(typeof(Motely.MotelyJoker));
        private static readonly string[] TarotNames = Enum.GetNames(typeof(Motely.MotelyTarotCard));
        private static readonly string[] PlanetNames = Enum.GetNames(typeof(Motely.MotelyPlanetCard));
        private static readonly string[] SpectralNames = Enum.GetNames(typeof(Motely.MotelySpectralCard));
        private static readonly string[] TagNames = Enum.GetNames(typeof(Motely.MotelyTag));
        private static readonly string[] VoucherNames = Enum.GetNames(typeof(Motely.MotelyVoucher));
        private static readonly string[] DeckNames = Enum.GetNames(typeof(Motely.MotelyDeck));
        private static readonly string[] StakeNames = Enum.GetNames(typeof(Motely.MotelyStake));
        private static readonly string[] EditionNames = Enum.GetNames(typeof(Motely.MotelyItemEdition));
        private static readonly string[] SuitNames = Enum.GetNames(typeof(Motely.MotelyPlayingCardSuit));
        private static readonly string[] RankNames = Enum.GetNames(typeof(Motely.MotelyPlayingCardRank));
        
        private static readonly string[] ValidTypes = new[] { "Joker", "Tarot", "TarotCard", "Planet", "PlanetCard", 
            "Spectral", "SpectralCard", "Tag", "SmallBlindTag", "BigBlindTag", "Voucher", "PlayingCard" };
        
        private static readonly string[] ValidSources = new[] { "shop", "packs", "tags" };
        
        /// <summary>
        /// Get all valid values for a given field in a specific context
        /// </summary>
        public static IEnumerable<string> GetValidValues(string fieldName, string? itemType = null)
        {
            switch (fieldName.ToLower())
            {
                case "type":
                    return ValidTypes;
                    
                case "value":
                    return GetValidValuesForType(itemType);
                    
                case "edition":
                    return EditionNames;
                    
                case "deck":
                    return DeckNames;
                    
                case "stake":
                    return StakeNames;
                    
                case "sources":
                    return ValidSources;
                    
                case "suit":
                    return SuitNames;
                    
                case "rank":
                    return RankNames;
                    
                default:
                    return Enumerable.Empty<string>();
            }
        }
        
        private static IEnumerable<string> GetValidValuesForType(string? type)
        {
            if (string.IsNullOrEmpty(type))
                return Enumerable.Empty<string>();
                
            switch (type.ToLower())
            {
                case "joker":
                case "souljoker":
                    return JokerNames;
                    
                case "tarot":
                case "tarotcard":
                    return TarotNames;
                    
                case "planet":
                case "planetcard":
                    return PlanetNames;
                    
                case "spectral":
                case "spectralcard":
                    return SpectralNames;
                    
                case "tag":
                case "smallblindtag":
                case "bigblindtag":
                    return TagNames;
                    
                case "voucher":
                    return VoucherNames;
                    
                default:
                    return Enumerable.Empty<string>();
            }
        }
        
        /// <summary>
        /// Generate a JSON schema for Ouija configs
        /// </summary>
        public static string GenerateJsonSchema()
        {
            var schema = new JsonObject
            {
                ["$schema"] = "http://json-schema.org/draft-07/schema#",
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["deck"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(DeckNames.Select(d => JsonValue.Create(d)).ToArray()),
                        ["description"] = "The deck to use for the search"
                    },
                    ["stake"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(StakeNames.Select(s => JsonValue.Create(s)).ToArray()),
                        ["description"] = "The stake level for the search"
                    },
                    ["maxSearchAnte"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = 1,
                        ["maximum"] = 39,
                        ["default"] = 39,
                        ["description"] = "Maximum ante to search up to"
                    },
                    ["minimumScore"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = 0,
                        ["default"] = 0,
                        ["description"] = "Minimum score threshold for results"
                    },
                    ["must"] = CreateFilterArraySchema("Items that MUST be found"),
                    ["should"] = CreateFilterArraySchema("Items that contribute to scoring"),
                    ["mustNot"] = CreateFilterArraySchema("Items that MUST NOT be found")
                },
                ["required"] = new JsonArray("deck", "stake", "must", "should", "mustNot"),
                ["additionalProperties"] = false
            };
            
            return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        
        private static JsonObject CreateFilterArraySchema(string description)
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["description"] = description,
                ["items"] = CreateFilterItemSchema()
            };
        }
        
        private static JsonObject CreateFilterItemSchema()
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["type"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(ValidTypes.Select(t => JsonValue.Create(t)).ToArray()),
                        ["description"] = "Type of item to search for"
                    },
                    ["value"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Specific item name (depends on type)"
                    },
                    ["edition"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(EditionNames.Select(e => JsonValue.Create(e)).ToArray()),
                        ["description"] = "Edition requirement (None, Foil, Holographic, Polychrome, Negative)"
                    },
                    ["searchAntes"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "integer",
                            ["minimum"] = 1,
                            ["maximum"] = 39
                        },
                        ["minItems"] = 1,
                        ["description"] = "Which antes to search in"
                    },
                    ["score"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = 1,
                        ["default"] = 1,
                        ["description"] = "Score value (for 'should' items)"
                    },
                    ["sources"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray(ValidSources.Select(s => JsonValue.Create(s)).ToArray())
                        },
                        ["description"] = "Where to search: shop, packs, tags"
                    },
                    ["suit"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(SuitNames.Select(s => JsonValue.Create(s)).ToArray()),
                        ["description"] = "Suit for playing cards"
                    },
                    ["rank"] = new JsonObject
                    {
                        ["type"] = "string", 
                        ["enum"] = new JsonArray(RankNames.Select(r => JsonValue.Create(r)).ToArray()),
                        ["description"] = "Rank for playing cards"
                    }
                },
                ["required"] = new JsonArray("type", "searchAntes"),
                ["additionalProperties"] = false
            };
        }
        
        /// <summary>
        /// Get context-aware completions for a given position in JSON
        /// </summary>
        public static IEnumerable<CompletionItem> GetCompletions(string currentPath, string? currentValue, JsonContext context)
        {
            var completions = new List<CompletionItem>();
            
            // If we're in a property name position
            if (context.IsPropertyName)
            {
                completions.AddRange(GetPropertyCompletions(currentPath));
            }
            // If we're in a value position
            else if (context.IsValue)
            {
                completions.AddRange(GetValueCompletions(currentPath, context.PropertyName, context.ParentObject));
            }
            
            return completions;
        }
        
        private static IEnumerable<CompletionItem> GetPropertyCompletions(string path)
        {
            // Root level properties
            if (path == "$" || path == "")
            {
                yield return new CompletionItem("deck", "The deck to use", CompletionItemType.Property);
                yield return new CompletionItem("stake", "The stake level", CompletionItemType.Property);
                yield return new CompletionItem("maxSearchAnte", "Maximum ante to search", CompletionItemType.Property);
                yield return new CompletionItem("minimumScore", "Minimum score threshold", CompletionItemType.Property);
                yield return new CompletionItem("must", "Required items", CompletionItemType.Property);
                yield return new CompletionItem("should", "Scoring items", CompletionItemType.Property);
                yield return new CompletionItem("mustNot", "Forbidden items", CompletionItemType.Property);
            }
            // Filter item properties
            else if (path.Contains("must[") || path.Contains("should[") || path.Contains("mustNot["))
            {
                yield return new CompletionItem("type", "Type of item", CompletionItemType.Property);
                yield return new CompletionItem("value", "Item name", CompletionItemType.Property);
                yield return new CompletionItem("edition", "Edition requirement", CompletionItemType.Property);
                yield return new CompletionItem("searchAntes", "Antes to search", CompletionItemType.Property);
                yield return new CompletionItem("score", "Score value (for should)", CompletionItemType.Property);
                yield return new CompletionItem("sources", "Where to search", CompletionItemType.Property);
                yield return new CompletionItem("suit", "Card suit", CompletionItemType.Property);
                yield return new CompletionItem("rank", "Card rank", CompletionItemType.Property);
            }
        }
        
        private static IEnumerable<CompletionItem> GetValueCompletions(string path, string? propertyName, JsonObject? parentObject)
        {
            if (string.IsNullOrEmpty(propertyName))
                yield break;
                
            // Get the item type if we're in a filter item
            string? itemType = null;
            if (parentObject != null && parentObject.ContainsKey("type"))
            {
                itemType = parentObject["type"]?.ToString();
            }
            
            var validValues = GetValidValues(propertyName, itemType);
            foreach (var value in validValues)
            {
                yield return new CompletionItem(value, GetValueDescription(propertyName, value), CompletionItemType.Value);
            }
        }
        
        private static string GetValueDescription(string propertyName, string value)
        {
            switch (propertyName.ToLower())
            {
                case "edition":
                    return value switch
                    {
                        "None" => "No edition",
                        "Foil" => "+50 chips",
                        "Holographic" => "+10 mult", 
                        "Polychrome" => "x1.5 mult",
                        "Negative" => "+1 joker slot",
                        _ => value
                    };
                    
                case "sources":
                    return value switch
                    {
                        "shop" => "Items in the shop",
                        "packs" => "Items from booster packs",
                        "tags" => "Items from skip tags",
                        _ => value
                    };
                    
                default:
                    return value;
            }
        }
    }
    
    public class CompletionItem
    {
        public string Text { get; }
        public string Description { get; }
        public CompletionItemType Type { get; }
        
        public CompletionItem(string text, string description, CompletionItemType type)
        {
            Text = text;
            Description = description;
            Type = type;
        }
    }
    
    public enum CompletionItemType
    {
        Property,
        Value,
        Snippet
    }
    
    public class JsonContext
    {
        public bool IsPropertyName { get; set; }
        public bool IsValue { get; set; }
        public string? PropertyName { get; set; }
        public JsonObject? ParentObject { get; set; }
    }
}