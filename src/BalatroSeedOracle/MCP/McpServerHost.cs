using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BalatroSeedOracle.MCP;
using BalatroSeedOracle.MCP.Protocol;

namespace BalatroSeedOracle.MCP;

/// <summary>
/// MCP Server entry point for Balatro Seed Oracle
/// Provides stdio transport for JSON-RPC 2.0 communication
/// </summary>
public class McpServerHost
{
    public static async Task RunAsync(string[]? args = null)
    {
        // Create host builder
        var builder = Host.CreateApplicationBuilder(args ?? Array.Empty<string>());
        
        // Configure logging to stderr (stdout is for JSON-RPC only)
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
            options.SingleLine = true;
        });

        // Register services
        builder.Services.AddSingleton<AccurateBalatroKnowledgeBase>();
        builder.Services.AddSingleton<BalatroMcpServer>();
        builder.Services.AddSingleton<BalatroMcpProtocol>();

        var host = builder.Build();

        // Get services
        var protocol = host.Services.GetRequiredService<BalatroMcpProtocol>();
        var logger = host.Services.GetRequiredService<ILogger<McpServerHost>>();

        logger.LogInformation("Balatro Seed Oracle MCP Server starting...");

        // Run stdio server
        await RunStdioLoopAsync(protocol, logger);
    }

    private static async Task RunStdioLoopAsync(BalatroMcpProtocol protocol, ILogger logger)
    {
        await foreach (var line in Console.In.ReadAllLinesAsync())
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var response = await protocol.HandleRequestAsync(line);
                Console.WriteLine(response);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing request: {Line}", line);
                
                var errorResponse = new
                {
                    jsonrpc = "2.0",
                    id = "unknown",
                    error = new
                    {
                        code = -32603,
                        message = "Internal error"
                    }
                };

                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                Console.Out.Flush();
            }
        }
    }
}
