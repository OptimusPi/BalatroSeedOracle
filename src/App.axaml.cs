using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            DebugLogger.Log("Initializing services...");

            // Ensure required directories exist
            EnsureDirectoriesExist();

            // Set up services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Views.MainWindow();

                // Handle app exit
                desktop.ShutdownRequested += OnShutdownRequested;

                DebugLogger.Log("MainWindow created successfully");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during initialization: {ex}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.ReadLine();
            throw;
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        try
        {
            DebugLogger.Log("App", "Shutdown requested - stopping all searches...");
            
            // Get the search manager and stop all active searches
            var searchManager = _serviceProvider?.GetService<Services.SearchManager>();
            if (searchManager != null)
            {
                DebugLogger.Log("App", "Stopping active searches...");
                searchManager.StopAllSearches();
                
                // Give searches a moment to actually stop
                System.Threading.Thread.Sleep(500);
                
                // Dispose the search manager which will dispose all searches
                searchManager.Dispose();
                DebugLogger.Log("App", "All searches stopped");
            }
            
            // Now dispose the service provider
            _serviceProvider?.Dispose();
            DebugLogger.Log("App", "Services disposed");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Error during shutdown: {ex.Message}");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<Services.SearchHistoryService>();
        services.AddSingleton<Services.SearchManager>();
        services.AddSingleton<Services.SpriteService>(provider => Services.SpriteService.Instance);
        services.AddSingleton<Services.FavoritesService>();
        services.AddSingleton<Services.UserProfileService>();
        // ClipboardService is static, no need to register
    }

    private void EnsureDirectoriesExist()
    {
        try
        {
            // Create JsonItemFilters directory
            var jsonFiltersDir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "JsonItemFilters"
            );
            if (!System.IO.Directory.Exists(jsonFiltersDir))
            {
                System.IO.Directory.CreateDirectory(jsonFiltersDir);
                DebugLogger.Log($"Created directory: {jsonFiltersDir}");
            }

            // Create other required directories
            var searchResultsDir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "SearchResults"
            );
            if (!System.IO.Directory.Exists(searchResultsDir))
            {
                System.IO.Directory.CreateDirectory(searchResultsDir);
                DebugLogger.Log($"Created directory: {searchResultsDir}");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to create directories: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    public static T? GetService<T>()
        where T : class
    {
        if (Current is App app)
        {
            return app._serviceProvider?.GetService<T>();
        }
        return null;
    }
}
