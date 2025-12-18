var builder = WebApplication.CreateBuilder(args);

// Add minimal services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapPost("/close", () => 
{
    // Graceful shutdown logic here
    return Results.Ok(new { message = "Server shutting down..." });
});

// Search endpoints
app.MapPost("/api/search", (SearchRequest request) => 
{
    // TODO: Implement search logic
    return Results.Ok(new { 
        searchId = Guid.NewGuid().ToString(), 
        status = "started",
        jaml = request.Filter,
        results = new List<object>() // Top 1000 results will go here
    });
});

app.MapGet("/api/search/{id}", (string id) => 
{
    // TODO: Get search with JAML and top 1000 results
    return Results.Ok(new { 
        searchId = id, 
        status = "completed",
        jaml = "filter jaml here",
        results = new List<object>() // Top 1000 results
    });
});

// Results endpoints - for shared links with filter params
app.MapGet("/api/results", (string? filter) => 
{
    // If user selects filter or joins shared link with filter params
    if (string.IsNullOrEmpty(filter))
    {
        return Results.BadRequest(new { error = "Filter parameter required" });
    }
    
    // TODO: Get results for specific filter
    return Results.Ok(new { 
        filter = filter,
        results = new List<object>()
    });
});

// WebSocket endpoint for streaming seed search results
app.Map("/ws/{searchId}", async (string searchId, HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        // TODO: Stream search results to client
        while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            // Send seed results as they're found
            var message = System.Text.Json.JsonSerializer.Serialize(new { 
                searchId = searchId, 
                seed = "seed123", 
                score = 1000,
                timestamp = DateTime.UtcNow
            });
            
            await webSocket.SendAsync(
                System.Text.Encoding.UTF8.GetBytes(message),
                System.Net.WebSockets.WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            
            await Task.Delay(100); // Stream delay
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
