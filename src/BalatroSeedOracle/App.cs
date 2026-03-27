using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Declarative;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using BalatroSeedOracle.Extensions;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Theme;
using BalatroSeedOracle.UI;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle;

public class App : Application
{
    private ServiceProvider? _serviceProvider;

    public IServiceProvider Services => _serviceProvider!;

    /// <summary>
    /// Platform-specific initialization callback. Set by Desktop/Browser projects.
    /// </summary>
    public static Func<Task>? PlatformSpecificInitialization { get; set; }

    public override void Initialize()
    {
        // Pure C# — no AvaloniaXamlLoader.Load(this)
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            DebugLogger.Log("App", "Initializing application services");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var platformServices = _serviceProvider.GetService<IPlatformServices>();
            if (platformServices is not null)
            {
                DebugLogger.Initialize(platformServices);
                AppPaths.Initialize(platformServices);

                if (platformServices.SupportsFileSystem)
                    EnsureDirectoriesExist(platformServices);
            }

            // Browser-specific init
            if (platformServices != null && !platformServices.SupportsFileSystem)
            {
                _ = TestLocalStorageInteropAsync();
                _ = SeedBrowserSampleFiltersAsync();
            }

            InitializeFilterCache();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Dispatcher.UIThread.UnhandledException += OnUIThreadException;

                var mainWindow = new MainWindow(_serviceProvider);
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                _ = InitializeDesktopAsync(desktop);

                desktop.ShutdownRequested += OnShutdownRequested;
                DebugLogger.Log("App", "Desktop initialization complete");
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                Dispatcher.UIThread.UnhandledException += OnUIThreadException;
                DebugLogger.Log("App", "Initializing for Browser/Mobile");

                var mainMenu = new MainMenuComponent(_serviceProvider);
                singleView.MainView = mainMenu;

                _ = PreloadSpritesAsync();
                DebugLogger.Log("App", "Browser/Mobile initialization complete");
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            HandleException("INITIALIZATION", ex);
            throw;
        }
    }

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await PreloadSpritesAsync();

            // Initialize audio
            try
            {
                var audioManager = _serviceProvider?.GetService<IAudioManager>();
                if (audioManager != null)
                    DebugLogger.LogImportant("App", "Audio manager initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("App", $"Failed to initialize audio: {ex.Message}");
            }

            // Platform-specific initialization (search library, etc.)
            if (PlatformSpecificInitialization != null)
                await PlatformSpecificInitialization();

            DebugLogger.LogImportant("App", "Application ready.");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Error during desktop init: {ex.Message}");
        }
    }

    private async Task PreloadSpritesAsync()
    {
        try
        {
            var spriteService = SpriteService.Instance;
            await spriteService.PreloadAllSpritesAsync(null);
            DebugLogger.Log("App", "Sprites pre-loaded");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to pre-load sprites: {ex.Message}");
        }
    }

    private async Task TestLocalStorageInteropAsync()
    {
        try
        {
            await Task.Delay(1000);
            DebugLogger.Log("App", "LocalStorage interop test handled by platform services");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"LocalStorage interop test failed: {ex.Message}");
        }
    }

    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException("UI_THREAD", e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            HandleException("APP_DOMAIN", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException("TASK_SCHEDULER", e.Exception);
        e.SetObserved();
    }

    private void HandleException(string source, Exception ex)
    {
        DebugLogger.LogError(source, $"Exception: {ex.Message}");
        DebugLogger.LogError(source, $"Stack trace: {ex.StackTrace}");

        if (ex.InnerException != null)
        {
            DebugLogger.LogError(source, $"Inner: {ex.InnerException.Message}");
            DebugLogger.LogError(source, $"Inner stack: {ex.InnerException.StackTrace}");
        }

        var platformServices = _serviceProvider?.GetService<IPlatformServices>();
        if (platformServices != null)
        {
            var msg = $"=== {source} EXCEPTION: {DateTime.Now} ===\n"
                + $"Exception: {ex.GetType().FullName}\nMessage: {ex.Message}\n"
                + $"Stack Trace:\n{ex.StackTrace}\n";

            if (ex.InnerException != null)
                msg += $"Inner: {ex.InnerException.GetType().FullName}\n"
                    + $"Inner Message: {ex.InnerException.Message}\n"
                    + $"Inner Stack:\n{ex.InnerException.StackTrace}\n";

            _ = platformServices.WriteCrashLogAsync(msg);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        try
        {
            DebugLogger.Log("App", "Shutdown requested");

            var userProfileService = _serviceProvider?.GetService<UserProfileService>();
            userProfileService?.FlushProfile();

            var audioManager = _serviceProvider?.GetService<IAudioManager>();
            if (audioManager is IDisposable disposable)
                disposable.Dispose();

            var filterCache = _serviceProvider?.GetService<IFilterCacheService>();
            filterCache?.Dispose();

            var searchManager = _serviceProvider?.GetService<SearchManager>();
            if (searchManager != null)
            {
                searchManager.StopAllSearches();
                System.Threading.Thread.Sleep(500);
                searchManager.Dispose();
            }

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
        services.AddBalatroSeedOracleServices();

        services.AddSingleton<SpriteService>(provider => SpriteService.Instance);

        PlatformServices.RegisterServices?.Invoke(services);
    }

    private async Task SeedBrowserSampleFiltersAsync()
    {
        try
        {
            var store = _serviceProvider?.GetService<Storage.IAppDataStore>();
            if (store == null) return;

            const string sampleKey = "Filters/TelescopeObservatory.json";
            if (await store.ExistsAsync(sampleKey).ConfigureAwait(false))
                return;

            var sampleJson = """
            {
              "name": "Perkeo Observatory",
              "description": "Perkeo with the Telescope and Observatory Vouchers.",
              "author": "tacodiva",
              "dateCreated": "2025-01-01T05:46:12.6691000Z",
              "must": [
                { "type": "Voucher", "value": "Telescope", "antes": [1] },
                { "type": "Voucher", "value": "Observatory", "antes": [2] }
              ],
              "should": [],
              "mustNot": []
            }
            """;

            await store.WriteTextAsync(sampleKey, sampleJson).ConfigureAwait(false);
        }
        catch { /* Best-effort seeding */ }
    }

    private void InitializeFilterCache()
    {
        try
        {
            var filterCache = _serviceProvider?.GetService<IFilterCacheService>();
            if (filterCache != null)
            {
                filterCache.Initialize();
                DebugLogger.Log("App", $"Filter cache initialized with {filterCache.Count} filters");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to initialize filter cache: {ex.Message}");
        }
    }

    private void EnsureDirectoriesExist(IPlatformServices platformServices)
    {
        try
        {
            platformServices.EnsureDirectoryExists(AppPaths.FiltersDir);
            platformServices.EnsureDirectoryExists(AppPaths.SearchResultsDir);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("App", $"Failed to create directories: {ex.Message}");
        }
    }

    /// <summary>
    /// Service locator for transitional use. Prefer constructor injection.
    /// </summary>
    public static T? GetService<T>() where T : class
    {
        if (Current is App app)
            return app._serviceProvider?.GetService<T>();
        return null;
    }
}
