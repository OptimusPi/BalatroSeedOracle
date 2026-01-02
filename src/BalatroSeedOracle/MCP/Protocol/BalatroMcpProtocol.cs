using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BalatroSeedOracle.MCP;

namespace BalatroSeedOracle.MCP.Protocol;

/// <summary>
/// MCP Protocol implementation for Balatro Seed Oracle
/// Handles JSON-RPC 2.0 communication with proper tool definitions
/// </summary>
public class BalatroMcpProtocol
{
    private readonly BalatroMcpServer _server;
    private readonly ILogger<BalatroMcpProtocol> _logger;

    public BalatroMcpProtocol(BalatroMcpServer server, ILogger<BalatroMcpProtocol> logger)
    {
        _server = server;
        _logger = logger;
    }

    /// <summary>
    /// Handle MCP request and return response
    /// </summary>
    public async Task<string> HandleRequestAsync(string jsonRequest)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonRequest);
            var root = jsonDoc.RootElement;
            
            var method = root.GetProperty("method").GetString();
            var id = root.GetProperty("id");
            
            _logger.LogInformation("MCP Request: {Method}", method);

            var response = method switch
            {
                "initialize" => await HandleInitializeAsync(id),
                "tools/list" => await HandleToolsListAsync(id),
                "tools/call" => await HandleToolCallAsync(id, root.GetProperty("params")),
                "resources/list" => await HandleResourcesListAsync(id),
                "prompts/list" => await HandlePromptsListAsync(id),
                _ => CreateErrorResponse(id, -32601, $"Method not found: {method}")
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return CreateErrorResponse("unknown", -32603, $"Internal error: {ex.Message}");
        }
    }

    private async Task<string> HandleInitializeAsync(JsonElement id)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { },
                    resources = new { },
                    prompts = new { }
                },
                serverInfo = new
                {
                    name = "Balatro Seed Oracle MCP",
                    version = "1.0.0",
                    description = "MCP server for Balatro seed filtering with JAML schema support"
                }
            }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> HandleToolsListAsync(JsonElement id)
    {
        var tools = new[]
        {
            new
            {
                name = "list_filters",
                description = "List all available JAML filters",
                inputSchema = new { type = "object", properties = new { } }
            },
            new
            {
                name = "list_seed_sources",
                description = "List all available seed sources (DuckDB, CSV, TXT files)",
                inputSchema = new { type = "object", properties = new { } }
            },
            new
            {
                name = "create_filter",
                description = "Create a new JAML filter with validation",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Filter name" },
                        description = new { type = "string", description = "Filter description" },
                        jamlContent = new { type = "string", description = "JAML content following the schema" }
                    },
                    required = new[] { "name", "jamlContent" }
                }
            },
            new
            {
                name = "validate_jaml",
                description = "Validate JAML content against the schema",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        jamlContent = new { type = "string", description = "JAML content to validate" }
                    },
                    required = new[] { "jamlContent" }
                }
            },
            new
            {
                name = "get_jaml_schema",
                description = "Get the JAML schema for reference",
                inputSchema = new { type = "object", properties = new { } }
            },
            new
            {
                name = "generate_template",
                description = "Generate JAML templates for common patterns",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        templateType = new 
                        { 
                            type = "string", 
                            @enum = new[] { "basic", "joker", "deck", "voucher", "complex" },
                            description = "Type of template to generate"
                        }
                    },
                    required = new[] { "templateType" }
                }
            },
            new
            {
                name = "generate_jaml_with_context",
                description = "Generate JAML with Balatro knowledge context",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        userRequest = new { type = "string", description = "Natural language description of desired filter" }
                    },
                    required = new[] { "userRequest" }
                }
            },
            new
            {
                name = "get_balatro_knowledge",
                description = "Get Balatro knowledge for specific items",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        itemType = new { type = "string", description = "Type of item (joker, deck, voucher, etc.)" },
                        itemName = new { type = "string", description = "Name of the specific item" }
                    },
                    required = new[] { "itemType", "itemName" }
                }
            },
            new
            {
                name = "analyze_seed",
                description = "Analyze a single seed using the /analyze route",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        seed = new { type = "string", description = "Seed to analyze" }
                    },
                    required = new[] { "seed" }
                }
            }
        };

        var response = new
        {
            jsonrpc = "2.0",
            id,
            result = new { tools }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> HandleToolCallAsync(JsonElement id, JsonElement paramsElement)
    {
        try
        {
            var toolName = paramsElement.GetProperty("name").GetString();
            var arguments = paramsElement.GetProperty("arguments");

            _logger.LogInformation("MCP Tool Call: {Tool}", toolName);

            var result = toolName switch
            {
                "list_filters" => await _server.ListFiltersAsync(),
                "list_seed_sources" => await _server.ListSeedSourcesAsync(),
                "create_filter" => await HandleCreateFilterAsync(arguments),
                "validate_jaml" => await HandleValidateJamlAsync(arguments),
                "get_jaml_schema" => await _server.GetJamlSchemaAsync(),
                "generate_template" => await HandleGenerateTemplateAsync(arguments),
                "generate_jaml_with_context" => await HandleGenerateJamlWithContextAsync(arguments),
                "get_balatro_knowledge" => await HandleGetBalatroKnowledgeAsync(arguments),
                "analyze_seed" => await HandleAnalyzeSeedAsync(arguments),
                _ => throw new ArgumentException($"Unknown tool: {toolName}")
            };

            var response = new
            {
                jsonrpc = "2.0",
                id,
                result = JsonSerializer.Deserialize<object>(result)
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tool call");
            return CreateErrorResponse(id, -32602, $"Tool call error: {ex.Message}");
        }
    }

    private async Task<string> HandleCreateFilterAsync(JsonElement arguments)
    {
        var name = arguments.GetProperty("name").GetString() ?? throw new ArgumentException("Missing name");
        var description = arguments.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
        var jamlContent = arguments.GetProperty("jamlContent").GetString() ?? throw new ArgumentException("Missing jamlContent");

        return await _server.CreateFilterAsync(name, description, jamlContent);
    }

    private async Task<string> HandleValidateJamlAsync(JsonElement arguments)
    {
        var jamlContent = arguments.GetProperty("jamlContent").GetString() ?? throw new ArgumentException("Missing jamlContent");
        return await _server.ValidateJamlAsync(jamlContent);
    }

    private async Task<string> HandleGenerateTemplateAsync(JsonElement arguments)
    {
        var templateType = arguments.GetProperty("templateType").GetString() ?? throw new ArgumentException("Missing templateType");
        return await _server.GenerateTemplateAsync(templateType);
    }

    private async Task<string> HandleGenerateJamlWithContextAsync(JsonElement arguments)
    {
        var userRequest = arguments.GetProperty("userRequest").GetString() ?? throw new ArgumentException("Missing userRequest");
        return await _server.GenerateJamlWithContextAsync(userRequest);
    }

    private async Task<string> HandleGetBalatroKnowledgeAsync(JsonElement arguments)
    {
        var itemType = arguments.GetProperty("itemType").GetString() ?? throw new ArgumentException("Missing itemType");
        var itemName = arguments.GetProperty("itemName").GetString() ?? throw new ArgumentException("Missing itemName");
        return await _server.GetBalatroKnowledgeAsync(itemType, itemName);
    }

    private async Task<string> HandleAnalyzeSeedAsync(JsonElement arguments)
    {
        var seed = arguments.GetProperty("seed").GetString() ?? throw new ArgumentException("Missing seed");
        return await _server.AnalyzeSeedAsync(seed);
    }

    private async Task<string> HandleResourcesListAsync(JsonElement id)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result = new { resources = new object[0] }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<string> HandlePromptsListAsync(JsonElement id)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result = new { prompts = new object[0] }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    private string CreateErrorResponse(JsonElement id, int code, string message)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message
            }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }
}
