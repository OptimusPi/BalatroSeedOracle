using BalatroSeedOracle.MCP;

namespace BalatroSeedOracle.MCP.CLI;

/// <summary>
/// CLI entry point for Balatro Seed Oracle MCP Server
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await McpServerHost.RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
