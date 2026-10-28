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
        Directory.CreateDirectory(FilterFiles.Dir);
        Directory.CreateDirectory("SearchResults");
        Directory.CreateDirectory("WordLists");
        Directory.CreateDirectory("VisualizerPresets");
        Directory.CreateDirectory("MixerPresets");

        var profile = new UserProfileService();
        Register(profile);
        Register(SpriteService.Instance);

        var gpuService = new GpuDeviceService();
        Register<IGpuDeviceService>(gpuService);
        Register(gpuService);

        var lakeService = new DungmotLakeService(profile.GetProfile().DungmotLakePath);
        Register<IDungmotLakeService>(lakeService);
        Register(lakeService);

        var partyService = new PartySearchService();
        Register<IPartySearchService>(partyService);
        Register(partyService);

        Func<AnalyzeModalViewModel> analyzeFactory = () =>
            new AnalyzeModalViewModel(SpriteService.Instance, profile);

        FiltersModalViewModel filtersVM = null!;
        filtersVM = new FiltersModalViewModel(
            profile,
            () => new ViewModels.FilterTabs.ValidateFilterTabViewModel(filtersVM)
        );
        var searchVM = new SearchModalViewModel(profile, analyzeFactory, gpuService, lakeService, partyService);
        var menuVM = new BalatroMainMenuViewModel(profile, searchVM, filtersVM, analyzeFactory, gpuService);
        var mainMenu = new Views.BalatroMainMenu(menuVM);
        Register(filtersVM);
        Register(searchVM);
        Register(menuVM);
        Register(mainMenu);
        Register(new FilterListViewModel(SpriteService.Instance));
        Register(new PaginatedFilterBrowserViewModel(profile));
        Register(new Views.Modals.ToolsModal(new ToolsModalViewModel()));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += OnUIThreadException;

            var mainWindow = new Views.MainWindow(new MainWindowViewModel(profile), mainMenu);
            Register(mainWindow);
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            mainMenu.ApplyShaderParameters(ShaderPresetHelper.Load("normal"));
            _ = SpriteService.Instance.PreloadAllSpritesAsync(null);

            // Background music: stems are opened on a worker thread and played through the
            // same NAudio output as the sound effects. Muted at launch => started on unmute.
            if (profile.GetProfile().IsMusicMuted)
                DebugLogger.Log(nameof(MusicService), "Music muted in the profile; it starts when unmuted");
            else
                _ = MusicService.Instance.StartAsync();
            menuVM.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BalatroMainMenuViewModel.Volume) && menuVM.Volume > 0)
                    _ = MusicService.Instance.StartAsync();
            };

            desktop.ShutdownRequested += (_, _) => profile.FlushProfile();
            desktop.Exit += (_, _) =>
            {
                if (MusicService.IsInstanceCreated)
                    MusicService.Instance.Dispose();
                SoundEffectService.DisposeShared();
            };
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
