using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using Microsoft.Extensions.DependencyInjection;

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
            DebugLogger.Log("App", "Initializing application services");

            // Ensure required directories exist
            EnsureDirectoriesExist();

            // Set up services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize filter cache on startup for fast filter access
            InitializeFilterCache();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Views.MainWindow();

                // Initialize background music with SoundFlow (8-track)
                try
                {
                    var audioManager =
                        _serviceProvider.GetRequiredService<Services.SoundFlowAudioManager>();
                    DebugLogger.LogImportant("App", "Starting 8-track audio with SoundFlow");
                    DebugLogger.Log("App", $"Audio manager initialized: {audioManager}");
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("App", $"Failed to initialize audio: {ex.Message}");
                }

                // Handle app exit
                desktop.ShutdownRequested += OnShutdownRequested;

                DebugLogger.Log("App", "MainWindow created successfully");
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

            // Flush user profile first to ensure all settings are saved
            var userProfileService = _serviceProvider?.GetService<Services.UserProfileService>();
            if (userProfileService != null)
            {
                DebugLogger.Log("App", "Flushing user profile...");
                userProfileService.FlushProfile();
                DebugLogger.Log("App", "User profile flushed");
            }

            // Stop audio
            var audioManager = _serviceProvider?.GetService<Services.SoundFlowAudioManager>();
            audioManager?.Dispose();

            // Dispose filter cache
            var filterCache = _serviceProvider?.GetService<Services.IFilterCacheService>();
            filterCache?.Dispose();

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
        // Register all MVVM services and ViewModels
        services.AddBalatroSeedOracleServices();

        // Register existing singleton services
        services.AddSingleton<Services.SpriteService>(provider => Services.SpriteService.Instance);
        // ClipboardService is static, no need to register
    }

    private void InitializeFilterCache()
    {
        try
        {
            DebugLogger.Log("App", "Initializing filter cache...");
            var filterCache = _serviceProvider?.GetService<Services.IFilterCacheService>();
            if (filterCache != null)
            {
                filterCache.Initialize();
                DebugLogger.Log(
                    "App",
                    $"Filter cache initialized with {filterCache.Count} filters"
                );
            }
            else
            {
                DebugLogger.LogError("App", "FilterCacheService not found in DI container");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to initialize filter cache: {ex.Message}");
        }
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
                DebugLogger.Log("App", $"Created directory: {jsonFiltersDir}");
            }

            // Create other required directories
            var searchResultsDir = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "SearchResults"
            );
            if (!System.IO.Directory.Exists(searchResultsDir))
            {
                System.IO.Directory.CreateDirectory(searchResultsDir);
                DebugLogger.Log("App", $"Created directory: {searchResultsDir}");
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
