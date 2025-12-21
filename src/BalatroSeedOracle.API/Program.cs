using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using Motely.Filters;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add minimal services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register BSO Services
builder.Services.AddBalatroSeedOracleServices();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

// Health check
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Close endpoint
app.MapPost("/close", (SearchManager searchManager) => 
{
    searchManager.StopAllSearches();
    searchManager.Dispose();
    Task.Run(async () => {
        await Task.Delay(500);
        app.StopAsync();
    });
    return Results.Ok(new { message = "Server shutting down..." });
});

// Search endpoints
app.MapPost("/api/search", async (SearchRequest request, SearchManager searchManager) => 
{
    try 
    {
        // Parse the JAML filter
        var config = MotelyJsonConfig.LoadFromJson(request.Filter);
        if (config == null)
        {
            return Results.BadRequest(new { error = "Invalid filter JAML" });
        }

        // Create search criteria
        var criteria = new SearchCriteria
        {
            ThreadCount = Environment.ProcessorCount,
            BatchSize = 3,
            Deck = "Red",
            Stake = "White",
            MinScore = 0
        };

        // Start the search
        var searchInstance = await searchManager.StartSearchAsync(criteria, config);
        
        return Results.Ok(new { 
            searchId = searchInstance.SearchId, 
            status = "started",
            jaml = request.Filter,
            results = new List<object>() 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/api/search/{id}", async (string id, SearchManager searchManager) => 
{
    var searchInstance = searchManager.GetSearch(id);
    if (searchInstance == null)
    {
        return Results.NotFound(new { error = "Search not found" });
    }

    var results = await searchInstance.GetTopResultsAsync("score", false, 1000);
    
    return Results.Ok(new { 
        searchId = id, 
        status = searchInstance.IsRunning ? "running" : "completed",
        results = results
    });
});

// Results endpoints - for shared links with filter params
app.MapGet("/api/results", (string? filter) => 
{
    if (string.IsNullOrEmpty(filter))
    {
        return Results.BadRequest(new { error = "Filter parameter required" });
    }
    
    // TODO: Implement result retrieval for specific filter if needed
    return Results.Ok(new { 
        filter = filter,
        results = new List<object>()
    });
});

// WebSocket endpoint for streaming seed search results
app.Map("/ws/{searchId}", async (string searchId, HttpContext context, SearchManager searchManager) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var searchInstance = searchManager.GetSearch(searchId);
        if (searchInstance == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        // Subscription task to send results as they're found
        var tcs = new TaskCompletionSource();
        
        EventHandler<SearchResultEventArgs> resultHandler = async (s, e) => {
            if (webSocket.State == WebSocketState.Open)
            {
                var message = JsonSerializer.Serialize(new { 
                    type = "result",
                    searchId = searchId, 
                    seed = e.Result.Seed, 
                    score = e.Result.TotalScore,
                    timestamp = DateTime.UtcNow
                });
                
                await webSocket.SendAsync(
                    Encoding.UTF8.GetBytes(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        };

        EventHandler<SearchProgress> progressHandler = async (s, e) => {
            if (webSocket.State == WebSocketState.Open)
            {
                var message = JsonSerializer.Serialize(new { 
                    type = "progress",
                    searchId = searchId, 
                    progress = e.PercentComplete,
                    seedsSearched = e.SeedsSearched,
                    resultsFound = e.ResultsFound,
                    timestamp = DateTime.UtcNow
                });
                
                await webSocket.SendAsync(
                    Encoding.UTF8.GetBytes(message),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        };

        searchInstance.ResultReceived += resultHandler;
        searchInstance.ProgressUpdated += progressHandler;

        try 
        {
            // Keep the connection open until it's closed by client or search finishes
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[1024 * 4];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }
        }
        finally 
        {
            searchInstance.ResultReceived -= resultHandler;
            searchInstance.ProgressUpdated -= progressHandler;
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();

// Simple request models
record SearchRequest(string Filter, int MaxResults = 100);
