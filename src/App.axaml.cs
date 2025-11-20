using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
                // Show loading window and pre-load sprites before showing main window
                ShowLoadingWindowAndPreloadSprites(desktop);

                // Handle app exit
                desktop.ShutdownRequested += OnShutdownRequested;

                DebugLogger.Log("App", "Application initialization complete");
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

    private async void ShowLoadingWindowAndPreloadSprites(
        IClassicDesktopStyleApplicationLifetime desktop
    )
    {
        try
        {
            DebugLogger.LogImportant(
                "App",
                "Starting shader-driven intro with sprite pre-loading..."
            );

            // Create main window FIRST (so we have access to shader background)
            var mainWindow = new Views.MainWindow();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            // Give UI a moment to render and initialize shader
            await System.Threading.Tasks.Task.Delay(100);

            // Get reference to BalatroMainMenu and its shader background
            var mainMenu = mainWindow.FindControl<Views.BalatroMainMenu>("MainMenu");
            if (mainMenu == null)
            {
                DebugLogger.LogError(
                    "App",
                    "Failed to find BalatroMainMenu - falling back to normal startup"
                );
                await PreloadSpritesWithoutTransition();
                return;
            }

            // SMOOTH INTRO TRANSITION - Dark pixelated → Vibrant Balatro
            DebugLogger.LogImportant("App", "Starting SMOOTH intro transition (Dark → Normal)");

            var introParams = Helpers.ShaderPresetHelper.Load("intro");
            var normalParams = Helpers.ShaderPresetHelper.Load("normal");

            // Apply intro state immediately
            ApplyShaderParametersToMainMenu(mainMenu, introParams);

            // Get TransitionService and start the transition
            var transitionService = _serviceProvider?.GetService<Services.TransitionService>();
            if (transitionService != null)
            {
                // Start transition - we'll drive progress via sprite loading
                transitionService.StartTransition(
                    introParams,
                    normalParams,
                    parameters => ApplyShaderParametersToMainMenu(mainMenu, parameters)
                );

                // Preload sprites WITH SMOOTH TRANSITION driven by progress
                await PreloadSpritesWithTransition(transitionService);
            }
            else
            {
                DebugLogger.LogError("App", "TransitionService not found - falling back to instant transition");
                ApplyShaderParametersToMainMenu(mainMenu, normalParams);
                await PreloadSpritesWithoutTransition();
            }

            // Initialize background music with SoundFlow (8-track)
            try
            {
                var audioManager =
                    _serviceProvider!.GetRequiredService<Services.SoundFlowAudioManager>();
                DebugLogger.LogImportant("App", "Starting 8-track audio with SoundFlow");
                DebugLogger.Log("App", $"Audio manager initialized: {audioManager}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("App", $"Failed to initialize audio: {ex.Message}");
            }

            // Complete the intro transition NOW (after everything is ready)
            if (transitionService != null)
            {
                transitionService.SetProgress(1.0f);
            }

            DebugLogger.LogImportant(
                "App",
                "Application ready! All sprites pre-loaded with shader intro."
            );
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Error during sprite pre-load: {ex.Message}");
            DebugLogger.LogError("App", $"Stack trace: {ex.StackTrace}");

            // Fall back to showing main window without transition
            if (desktop.MainWindow == null)
            {
                desktop.MainWindow = new Views.MainWindow();
                desktop.MainWindow.Show();
            }
            await PreloadSpritesWithoutTransition();
        }
    }

    /// <summary>
    /// Applies shader parameters to BalatroMainMenu's shader background.
    /// Uses reflection to access private _shaderBackground field.
    /// </summary>
    private void ApplyShaderParametersToMainMenu(
        Views.BalatroMainMenu mainMenu,
        Models.ShaderParameters parameters
    )
    {
        try
        {
            // Access private _shaderBackground field via reflection
            var shaderBackgroundField = typeof(Views.BalatroMainMenu).GetField(
                "_shaderBackground",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (
                shaderBackgroundField?.GetValue(mainMenu)
                is Controls.BalatroShaderBackground shaderBackground
            )
            {
                // Apply all shader parameters
                shaderBackground.SetTime(parameters.TimeSpeed);
                shaderBackground.SetSpinTime(parameters.SpinTimeSpeed);
                shaderBackground.SetMainColor(parameters.MainColor);
                shaderBackground.SetAccentColor(parameters.AccentColor);
                shaderBackground.SetBackgroundColor(parameters.BackgroundColor);
                shaderBackground.SetContrast(parameters.Contrast);
                shaderBackground.SetSpinAmount(parameters.SpinAmount);
                shaderBackground.SetParallax(parameters.ParallaxX, parameters.ParallaxY);
                shaderBackground.SetZoomScale(parameters.ZoomScale);
                shaderBackground.SetSaturationAmount(parameters.SaturationAmount);
                shaderBackground.SetSaturationAmount2(parameters.SaturationAmount2);
                shaderBackground.SetPixelSize(parameters.PixelSize);
                shaderBackground.SetSpinEase(parameters.SpinEase);
                shaderBackground.SetLoopCount(parameters.LoopCount);
            }
            else
            {
                DebugLogger.LogError("App", "Failed to get shader background reference");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to apply shader parameters: {ex.Message}");
        }
    }

    /// <summary>
    /// Pre-load sprites WITH smooth shader transition driven by progress
    /// </summary>
    private async System.Threading.Tasks.Task PreloadSpritesWithTransition(
        Services.TransitionService transitionService
    )
    {
        try
        {
            var spriteService = Services.SpriteService.Instance;
            var introStart = DateTime.UtcNow;
            var minIntro = TimeSpan.FromSeconds(3.14);

            var uniformProgressTask = System.Threading.Tasks.Task.Run(async () =>
            {
                while (true)
                {
                    var elapsed = DateTime.UtcNow - introStart;
                    if (elapsed >= minIntro)
                    {
                        // Stop at 95% - caller will complete to 100% when fully ready
                        transitionService.SetProgress(0.95f);
                        break;
                    }
                    float t = (float)(elapsed.TotalMilliseconds / minIntro.TotalMilliseconds);
                    float p = (1f - (1f - t) * (1f - t)) * 0.95f; // Scale to 95% max
                    transitionService.SetProgress(p);
                    await System.Threading.Tasks.Task.Delay(16).ConfigureAwait(false);
                }
            });

            var progress = new Progress<(string category, int current, int total)>(update =>
            {
                DebugLogger.Log(
                    "App",
                    $"Loading {update.category}: {update.current}/{update.total}"
                );
            });

            await spriteService.PreloadAllSpritesAsync(progress);

            await uniformProgressTask.ConfigureAwait(false);

            DebugLogger.LogImportant("App", "Sprites pre-loaded with smooth transition!");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to pre-load sprites with transition: {ex.Message}");
            // Don't complete transition here - let caller handle it
            transitionService.SetProgress(0.95f);
        }
    }

    /// <summary>
    /// Fallback: Pre-load sprites without transition effect
    /// </summary>
    private async System.Threading.Tasks.Task PreloadSpritesWithoutTransition()
    {
        try
        {
            var spriteService = Services.SpriteService.Instance;
            await spriteService.PreloadAllSpritesAsync(null);
            DebugLogger.Log("App", "Sprites pre-loaded (no transition)");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to pre-load sprites: {ex.Message}");
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
