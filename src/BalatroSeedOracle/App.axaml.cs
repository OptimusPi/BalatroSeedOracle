using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider Services => _serviceProvider!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            DebugLogger.Log("App", "Initializing application services");

            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Set up services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Get platform services for platform-specific initialization
            var platformServices = _serviceProvider.GetService<Services.IPlatformServices>();
            if (platformServices != null)
            {
                // Initialize DebugLogger with platform services (removes need for #if directives)
                Helpers.DebugLogger.Initialize(platformServices);

                // Initialize AppPaths with platform services (removes need for #if directives)
                Helpers.AppPaths.Initialize(platformServices);

                // Ensure required directories exist (platform-specific)
                if (platformServices.SupportsFileSystem)
                {
                    EnsureDirectoriesExist(platformServices);
                }

                // Copy sample content to AppData on first run (platform-specific)
                _ = platformServices.CopySamplesToAppDataAsync();
            }

            // Browser-specific initialization
            if (platformServices != null && !platformServices.SupportsFileSystem)
            {
                // Test localStorage interop early - fire-and-forget with proper error handling
                _ = TestLocalStorageInteropAsync();

                // Seed browser sample filters (fire-and-forget, best-effort)
                _ = SeedBrowserSampleFiltersAsync();
            }

            // Initialize filter cache (works on all platforms)
            InitializeFilterCache();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Set up UI thread exception handler
                Dispatcher.UIThread.UnhandledException += OnUIThreadException;

                // Show loading window and pre-load sprites before showing main window
                ShowLoadingWindowAndPreloadSprites(desktop);

                // Handle app exit
                desktop.ShutdownRequested += OnShutdownRequested;

                DebugLogger.Log("App", "Application initialization complete");
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                // Browser/Mobile platform - use single view instead of window
                DebugLogger.Log("App", "Initializing for Browser/Mobile platform");

                // Set up UI thread exception handler
                Dispatcher.UIThread.UnhandledException += OnUIThreadException;

                // For Browser/Mobile, we use BalatroMainMenu directly as the main view
                var mainMenu = _serviceProvider!.GetRequiredService<Views.BalatroMainMenu>();
                singleViewPlatform.MainView = mainMenu;

                // Pre-load sprites asynchronously (without the complex shader transition)
                _ = PreloadSpritesWithoutTransition();

                DebugLogger.Log("App", "Browser/Mobile initialization complete");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            HandleException("INITIALIZATION", ex);
            // Desktop platforms can use Console.ReadLine, browser cannot
            var platformServices = _serviceProvider?.GetService<Services.IPlatformServices>();
            if (platformServices?.SupportsFileSystem == true)
            {
                Console.ReadLine(); // Desktop only - browser has no stdin
            }
            throw;
        }
    }

    private async Task TestLocalStorageInteropAsync()
    {
        try
        {
            await Task.Delay(1000); // Wait for JS to initialize
            var testResult = await BalatroSeedOracle.Services.Storage.LocalStorageTester.TestLocalStorageInterop();
            DebugLogger.Log("App", $"LocalStorage interop test result: {(testResult ? "PASSED" : "FAILED")}");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"LocalStorage interop test failed: {ex.Message}");
        }
    }

    private async Task UniformProgressLoopAsync(
        Services.TransitionService transitionService,
        DateTime introStart,
        TimeSpan minIntro
    )
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
    }

    /// <summary>
    /// Handles unhandled exceptions on the UI thread
    /// </summary>
    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var ex = e.Exception;
        HandleException("UI_THREAD", ex);
        e.Handled = true;
    }

    /// <summary>
    /// Handles unhandled exceptions from AppDomain
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            HandleException("APP_DOMAIN", ex);
        }
        else
        {
            DebugLogger.LogError("APP_DOMAIN", $"❌ Non-exception error: {e.ExceptionObject}");
        }
    }

    /// <summary>
    /// Handles unobserved task exceptions
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException("TASK_SCHEDULER", e.Exception);
        e.SetObserved();
    }

    /// <summary>
    /// Centralized exception handling logic
    /// </summary>
    private void HandleException(string source, Exception ex)
    {
        DebugLogger.LogError(source, $"❌ Exception: {ex.Message}");
        DebugLogger.LogError(source, $"Stack trace: {ex.StackTrace}");

        // Log inner exceptions if they exist
        if (ex.InnerException != null)
        {
            DebugLogger.LogError(source, $"Inner Exception: {ex.InnerException.Message}");
            DebugLogger.LogError(source, $"Inner Stack trace: {ex.InnerException.StackTrace}");
        }

        // Write to crash log (platform-specific)
        var platformServices = _serviceProvider?.GetService<Services.IPlatformServices>();
        if (platformServices != null)
        {
            var errorMsg =
                $"=== {source} EXCEPTION: {DateTime.Now} ===\n"
                + $"Exception: {ex.GetType().FullName}\n"
                + $"Message: {ex.Message}\n"
                + $"Stack Trace:\n{ex.StackTrace}\n";

            if (ex.InnerException != null)
            {
                errorMsg +=
                    $"Inner Exception: {ex.InnerException.GetType().FullName}\n"
                    + $"Inner Message: {ex.InnerException.Message}\n"
                    + $"Inner Stack Trace:\n{ex.InnerException.StackTrace}\n";
            }
            errorMsg += "\n";

            _ = platformServices.WriteCrashLogAsync(errorMsg);
        }
    }

    private async void ShowLoadingWindowAndPreloadSprites(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            DebugLogger.LogImportant("App", "Starting shader-driven intro with sprite pre-loading...");

            // Create main window FIRST (so we have access to shader background)
            var mainWindow = _serviceProvider!.GetRequiredService<Views.MainWindow>();
            desktop.MainWindow = mainWindow;

            // Subscribe to window state changes to close popups when minimized
            mainWindow.PropertyChanged += (s, e) =>
            {
                if (e.Property == Window.WindowStateProperty)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        // Close all popups when window is minimized
                        var vm = mainWindow.MainMenu?.ViewModel;
                        if (vm is not null)
                        {
                            vm.IsVolumePopupOpen = false;
                            vm.IsWidgetDockVisible = false;
                        }
                    }
                }
            };

            mainWindow.Show();

            // Give UI a moment to render and initialize shader
            await System.Threading.Tasks.Task.Delay(100);

            // Get reference to BalatroMainMenu and its shader background
            var mainMenu = mainWindow.MainMenu;
            if (mainMenu is null)
            {
                DebugLogger.LogError("App", "Failed to find BalatroMainMenu - falling back to normal startup");
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
                var audioManager = _serviceProvider!.GetRequiredService<Services.SoundFlowAudioManager>();
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

            DebugLogger.LogImportant("App", "Application ready! All sprites pre-loaded with shader intro.");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Error during sprite pre-load: {ex.Message}");
            DebugLogger.LogError("App", $"Stack trace: {ex.StackTrace}");

            // Fall back to showing main window without transition
            if (desktop.MainWindow == null)
            {
                desktop.MainWindow = _serviceProvider!.GetRequiredService<Views.MainWindow>();
                desktop.MainWindow.Show();
            }
            await PreloadSpritesWithoutTransition();
        }
    }

    private void ApplyShaderParametersToMainMenu(Views.BalatroMainMenu mainMenu, Models.ShaderParameters parameters)
    {
        mainMenu.ApplyShaderParameters(parameters);
    }

    /// <summary>
    /// Pre-load sprites WITH smooth shader transition driven by progress
    /// </summary>
    private async System.Threading.Tasks.Task PreloadSpritesWithTransition(Services.TransitionService transitionService)
    {
        try
        {
            var spriteService = BalatroSeedOracle.Services.SpriteService.Instance;
            var introStart = DateTime.UtcNow;
            var minIntro = TimeSpan.FromSeconds(3.14);

            // Track uniform progress task - no fire-and-forget!
            _ = UniformProgressLoopAsync(transitionService, introStart, minIntro);

            var progress = new Progress<(string category, int current, int total)>(update =>
            {
                DebugLogger.Log("App", $"Loading {update.category}: {update.current}/{update.total}");
            });

            await spriteService.PreloadAllSpritesAsync(progress);

            // Uniform progress loop runs in background - no need to await
            // It will complete when minIntro time is reached

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
            var spriteService = BalatroSeedOracle.Services.SpriteService.Instance;
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
        services.AddSingleton<BalatroSeedOracle.Services.SpriteService>(provider =>
            BalatroSeedOracle.Services.SpriteService.Instance
        );

        // Register platform-specific services (set by Desktop/Browser Program.cs)
        PlatformServices.RegisterServices?.Invoke(services);
    }

    private async Task SeedBrowserSampleFiltersAsync()
    {
        try
        {
            var store = _serviceProvider?.GetService<Services.Storage.IAppDataStore>();
            if (store == null)
                return;

            const string sampleKey = "Filters/TelescopeObservatory.json";
            var exists = await store.ExistsAsync(sampleKey).ConfigureAwait(false);
            if (exists)
                return;

            var sampleJson =
                "{\n  \"name\": \"Perkeo Observatory\",\n  \"description\": \"Perkeo with the Telescope and Observatory Vouchers.\",\n  \"author\": \"tacodiva\",\n  \"dateCreated\": \"2025-01-01T05:46:12.6691000Z\",\n  \"must\": [\n    {\n      \"type\": \"Voucher\",\n      \"value\": \"Telescope\",\n      \"antes\": [1]\n    },\n    {\n      \"type\": \"Voucher\",\n      \"value\": \"Observatory\",\n      \"antes\": [2]\n    }\n  ],\n  \"should\": [],\n  \"mustNot\": []\n}";

            await store.WriteTextAsync(sampleKey, sampleJson).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort seeding; ignore failures.
        }
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
                DebugLogger.Log("App", $"Filter cache initialized with {filterCache.Count} filters");
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

    private void EnsureDirectoriesExist(Services.IPlatformServices platformServices)
    {
        try
        {
            // Directories are now managed by AppPaths - they are auto-created on access
            // Use platform services to ensure directories exist
            DebugLogger.Log("App", "Using AppPaths for directory management");

            // Touch directories to ensure they exist
            platformServices.EnsureDirectoryExists(AppPaths.FiltersDir);
            platformServices.EnsureDirectoryExists(AppPaths.SearchResultsDir);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to create directories: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a service from the DI container (temporary until full DI migration)
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
