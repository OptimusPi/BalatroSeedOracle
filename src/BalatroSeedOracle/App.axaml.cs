using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle;

public partial class App : Application
{
    private static readonly Dictionary<Type, object> _services = new();

    private static void Register<T>(T instance)
        where T : class => _services[typeof(T)] = instance;

    /// <summary>Hand-wired registry. The object graph is built once in OnFrameworkInitializationCompleted.</summary>
    public static T? GetService<T>()
        where T : class => _services.TryGetValue(typeof(T), out var s) ? (T)s : null;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            DebugLogger.LogError("APP_DOMAIN", $"{e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            DebugLogger.LogError("TASK", $"{e.Exception}");
            e.SetObserved();
        };

        // All data lives in the run directory, same convention as Motely.CLI.
        Directory.CreateDirectory("JamlFilters");
        Directory.CreateDirectory("SearchResults");
        Directory.CreateDirectory("WordLists");
        Directory.CreateDirectory("VisualizerPresets");
        Directory.CreateDirectory("MixerPresets");

        var profile = new UserProfileService();
        Register(profile);
        Register(SpriteService.Instance);

        Func<AnalyzeModalViewModel> analyzeFactory = () =>
            new AnalyzeModalViewModel(SpriteService.Instance, profile);

        FiltersModalViewModel filtersVM = null!;
        filtersVM = new FiltersModalViewModel(
            profile,
            () => new ViewModels.FilterTabs.ValidateFilterTabViewModel(filtersVM)
        );
        var searchVM = new SearchModalViewModel(profile, analyzeFactory);
        var menuVM = new BalatroMainMenuViewModel(profile, searchVM, filtersVM, analyzeFactory);
        var mainMenu = new Views.BalatroMainMenu(menuVM);
        Register(filtersVM);
        Register(searchVM);
        Register(menuVM);
        Register(mainMenu);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += OnUIThreadException;

            var mainWindow = new Views.MainWindow(new MainWindowViewModel(profile), mainMenu);
            Register(mainWindow);
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            mainMenu.ApplyShaderParameters(ShaderPresetHelper.Load("normal"));
            _ = SpriteService.Instance.PreloadAllSpritesAsync(null);

            desktop.ShutdownRequested += (_, _) => profile.FlushProfile();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Crash loudly and visibly: log it and put it on the ErrorBoundary.</summary>
    private void OnUIThreadException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        DebugLogger.LogError("UI_THREAD", $"{e.Exception}");
        if (
            ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.FindControl<Controls.ErrorBoundary>("MainContentHost")
                is { } boundary
        )
        {
            boundary.HasError = true;
            boundary.ErrorMessage = $"{e.Exception.GetType().Name}: {e.Exception.Message}\n\n{e.Exception.StackTrace}";
            e.Handled = true;
        }
    }
}
