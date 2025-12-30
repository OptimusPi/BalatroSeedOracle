using System.Text.Json;
using Microsoft.Extensions.Logging;
using Motely;
using BalatroSeedOracle.MCP.Knowledge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BalatroSeedOracle.MCP;

/// <summary>
/// MCP Server for Balatro Seed Oracle with JAML schema support
/// Provides tools for creating, validating, and managing JAML filters
/// </summary>
public class BalatroMcpServer
{
    private readonly ILogger<BalatroMcpServer> _logger;
    private readonly string _filtersPath;
    private readonly string _seedSourcesPath;
    private readonly AccurateBalatroKnowledgeBase _knowledgeBase;

    public BalatroMcpServer(ILogger<BalatroMcpServer> logger, AccurateBalatroKnowledgeBase knowledgeBase)
    {
        _logger = logger;
        _knowledgeBase = knowledgeBase;
        _filtersPath = "JamlFilters";
        _seedSourcesPath = "SeedSources";
        
        // Ensure directories exist
        Directory.CreateDirectory(_filtersPath);
        Directory.CreateDirectory(_seedSourcesPath);
    }

    /// <summary>
    /// Get available JAML filters
    /// </summary>
    public async Task<string> ListFiltersAsync()
    {
        try
        {
            var filters = Directory.GetFiles(_filtersPath, "*.jaml")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name)
                .ToList();

            var result = new
            {
                success = true,
                filters = filters,
                count = filters.Count
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing filters");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get available seed sources
    /// </summary>
    public async Task<string> ListSeedSourcesAsync()
    {
        try
        {
            var seedSources = new List<object>();
            
            // Add DuckDB files
            var dbFiles = Directory.GetFiles(_seedSourcesPath, "*.db")
                .Select(file => new
                {
                    name = Path.GetFileName(file),
                    type = "duckdb",
                    size = new FileInfo(file).Length,
                    lastModified = new FileInfo(file).LastWriteTime
                });

            // Add CSV files
            var csvFiles = Directory.GetFiles(_seedSourcesPath, "*.csv")
                .Select(file => new
                {
                    name = Path.GetFileName(file),
                    type = "csv",
                    size = new FileInfo(file).Length,
                    lastModified = new FileInfo(file).LastWriteTime
                });

            // Add TXT files
            var txtFiles = Directory.GetFiles(_seedSourcesPath, "*.txt")
                .Select(file => new
                {
                    name = Path.GetFileName(file),
                    type = "text",
                    size = new FileInfo(file).Length,
                    lastModified = new FileInfo(file).LastWriteTime
                });

            seedSources.AddRange(dbFiles);
            seedSources.AddRange(csvFiles);
            seedSources.AddRange(txtFiles);

            var result = new
            {
                success = true,
                seedSources = seedSources.OrderBy(s => s.name),
                count = seedSources.Count
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing seed sources");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new JAML filter using the schema
    /// </summary>
    public async Task<string> CreateFilterAsync(string name, string description, string jamlContent)
    {
        try
        {
            // Validate JAML against schema
            if (!JamlConfigLoader.TryLoadFromJamlString(jamlContent, out var config, out var error))
            {
                return JsonSerializer.Serialize(new 
                { 
                    success = false, 
                    error = $"Invalid JAML: {error}" 
                });
            }

            // Sanitize filename
            var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeName}.jaml";
            var filePath = Path.Combine(_filtersPath, fileName);

            // Check if file exists and add timestamp if needed
            if (File.Exists(filePath))
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                fileName = $"{safeName}_{timestamp}.jaml";
                filePath = Path.Combine(_filtersPath, fileName);
            }

            // Add metadata to JAML
            var jamlWithMetadata = AddMetadataToJaml(jamlContent, name, description);

            await File.WriteAllTextAsync(filePath, jamlWithMetadata);

            var result = new
            {
                success = true,
                fileName = fileName,
                path = filePath,
                message = $"Filter '{name}' created successfully"
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating filter");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Validate JAML content against the schema
    /// </summary>
    public async Task<string> ValidateJamlAsync(string jamlContent)
    {
        try
        {
            var success = JamlConfigLoader.TryLoadFromJamlString(jamlContent, out var config, out var error);

            var result = new
            {
                success = success,
                valid = success,
                error = error,
                config = success ? new
                {
                    hasMustClauses = config?.Must?.Count > 0,
                    hasShouldClauses = config?.Should?.Count > 0,
                    shouldScore = config?.ShouldScore ?? 1,
                    description = config?.Description,
                    author = config?.Author
                } : null
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JAML");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get JAML schema for reference
    /// </summary>
    public async Task<string> GetJamlSchemaAsync()
    {
        try
        {
            var schemaPath = "jaml.schema.json";
            if (File.Exists(schemaPath))
            {
                var schema = await File.ReadAllTextAsync(schemaPath);
                var result = new
                {
                    success = true,
                    schema = schema,
                    message = "JAML schema loaded successfully"
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                return JsonSerializer.Serialize(new 
                { 
                    success = false, 
                    error = "JAML schema file not found" 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JAML schema");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Generate JAML with Balatro knowledge context
    /// </summary>
    public async Task<string> GenerateJamlWithContextAsync(string userRequest)
    {
        try
        {
            // Get Balatro context for the request
            var context = _knowledgeBase.GetContextForPrompt(userRequest);
            
            // Create enhanced prompt with context
            var enhancedPrompt = $@"
{context}

USER REQUEST: {userRequest}

Generate a JAML filter that matches this request.
Requirements:
1. Follow the JAML schema exactly
2. Include proper must/should clauses
3. Add appropriate scores for should clauses (default is 1)
4. Use specific Balatro items and their synergies
5. Be realistic about what's achievable in Balatro

Generate only the JAML content, no explanations.";

            var result = new
            {
                success = true,
                context = context,
                enhancedPrompt = enhancedPrompt,
                message = "Context-enhanced prompt generated for JAML generation"
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating context");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get Balatro knowledge for a specific item
    /// </summary>
    public async Task<string> GetBalatroKnowledgeAsync(string itemType, string itemName)
    {
        try
        {
            var context = _knowledgeBase.GetContextForPrompt($"{itemType} {itemName}");
            
            var result = new
            {
                success = true,
                itemType = itemType,
                itemName = itemName,
                context = context,
                message = $"Knowledge retrieved for {itemType}: {itemName}"
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Balatro knowledge");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
    public async Task<string> GenerateTemplateAsync(string templateType)
    {
        try
        {
            var template = templateType.ToLowerInvariant() switch
            {
                "basic" => GetBasicTemplate(),
                "joker" => GetJokerTemplate(),
                "deck" => GetDeckTemplate(),
                "voucher" => GetVoucherTemplate(),
                "complex" => GetComplexTemplate(),
                _ => throw new ArgumentException($"Unknown template type: {templateType}")
            };

            var result = new
            {
                success = true,
                templateType = templateType,
                template = template,
                message = $"Template '{templateType}' generated successfully"
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template");
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private string AddMetadataToJaml(string jamlContent, string name, string description)
    {
        var lines = jamlContent.Split('\n').ToList();
        
        // Add metadata at the beginning if not present
        if (!lines.Any(l => l.TrimStart().StartsWith("name:")))
        {
            lines.Insert(0, $"name: {name}");
        }
        
        if (!lines.Any(l => l.TrimStart().StartsWith("description:")))
        {
            lines.Insert(1, $"description: {description}");
        }
        
        if (!lines.Any(l => l.TrimStart().StartsWith("author:")))
        {
            lines.Insert(2, "author: BalatroMCP");
        }
        
        if (!lines.Any(l => l.TrimStart().StartsWith("dateCreated:")))
        {
            lines.Insert(3, $"dateCreated: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        }

        return string.Join('\n', lines);
    }

    private string GetBasicTemplate()
    {
        return @"name: Basic Filter Template
description: A simple template for basic filtering
author: BalatroMCP
dateCreated: 2025-01-01T00:00:00Z

must:
  - type: joker
    name: Example Joker

should:
  - type: playing_card
    rank: A
    score: 1";
    }

    private string GetJokerTemplate()
    {
        return @"name: Joker Search Template
description: Template for finding specific jokers
author: BalatroMCP
dateCreated: 2025-01-01T00:00:00Z

must:
  - type: joker
    name: YourJokerName

should:
  - type: voucher
    name: Helpful Voucher
    score: 2
  - type: playing_card
    rank: K
    suit: Spades
    score: 1";
    }

    private string GetDeckTemplate()
    {
        return @"name: Deck Strategy Template
description: Template for deck-based filtering
author: BalatroMCP
dateCreated: 2025-01-01T00:00:00Z

must:
  - type: deck
    name: Target Deck

should:
  - or:
    - type: joker
      name: Supporting Joker 1
      score: 2
    - type: joker
      name: Supporting Joker 2
      score: 2
  - type: playing_card
    enhancement: Steel
    score: 1";
    }

    private string GetVoucherTemplate()
    {
        return @"name: Voucher Strategy Template
description: Template for voucher-based filtering
author: BalatroMCP
dateCreated: 2025-01-01T00:00:00Z

must:
  - type: voucher
    name: Required Voucher

should:
  - and:
    - type: joker
      name: Synergy Joker
      score: 2
    - type: playing_card
      rank: 5
      score: 1";
    }

    private string GetComplexTemplate()
    {
        return @"name: Complex Strategy Template
description: Template for advanced multi-condition filtering
author: BalatroMCP
dateCreated: 2025-01-01T00:00:00Z

must:
  - and:
    - type: joker
      name: Core Joker
    - type: deck
      name: Specific Deck

should:
  - or:
    - and:
      - type: voucher
        name: Voucher A
        score: 3
      - type: playing_card
        rank: A
        suit: Hearts
        score: 1
    - and:
      - type: voucher
        name: Voucher B
        score: 3
      - type: playing_card
        enhancement: Glass
        score: 1
  - type: joker
    edition: Polychrome
    score: 2";
    }
}
