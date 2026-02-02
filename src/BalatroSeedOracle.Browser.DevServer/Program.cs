using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Resolve path to Browser app output: prefer env, else sibling publish/wwwroot
var contentRoot = Environment.GetEnvironmentVariable("BSO_BROWSER_CONTENT_ROOT");
if (string.IsNullOrEmpty(contentRoot))
{
    var baseDir = AppContext.BaseDirectory;
    // From DevServer/bin/Debug/net10.0/ go up to src, then into Browser publish
    var browserPublish = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "BalatroSeedOracle.Browser", "bin", "Debug", "net10.0-browser", "wwwroot"));
    if (Directory.Exists(browserPublish))
        contentRoot = browserPublish;
    else
    {
        // Try publish/wwwroot (after dotnet publish Browser)
        browserPublish = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "BalatroSeedOracle.Browser", "bin", "Debug", "net10.0-browser", "publish", "wwwroot"));
        if (Directory.Exists(browserPublish))
            contentRoot = browserPublish;
    }
}

if (string.IsNullOrEmpty(contentRoot) || !Directory.Exists(contentRoot))
{
    Console.WriteLine("BSO Browser content not found. Run from Browser project first:");
    Console.WriteLine("  cd src/BalatroSeedOracle.Browser");
    Console.WriteLine("  dotnet build");
    Console.WriteLine("Then run this DevServer again. Or set BSO_BROWSER_CONTENT_ROOT to the path to wwwroot.");
    return 1;
}

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".mjs"] = "application/javascript";
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".json"] = "application/json";

var app = builder.Build();

// CRITICAL: COOP/COEP headers required for SharedArrayBuffer (multi-threading)
app.Use(async (context, next) =>
{
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentRoot),
    RequestPath = "",
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = false,
});
app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(contentRoot),
    ContentTypeProvider = provider,
});

var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(";").FirstOrDefault()?.Split(":").LastOrDefault() ?? "5080";
Console.WriteLine($"Serving BSO Browser from: {contentRoot}");
Console.WriteLine($"Open: http://localhost:{port}/");
app.Run();
return 0;
