using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Storage;
using BalatroSeedOracle.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBalatroSeedOracleServices(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<IConfigurationService>(sp => new ConfigurationService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetService<UserProfileService>(),
                sp.GetService<IFilterCacheService>(),
                sp.GetService<IPlatformServices>()
            ));

            // Note: IAppDataStore, IDuckDBService, and IPlatformServices are registered by platform-specific projects (Desktop/Browser)
            // IApiHostService is also registered by platform-specific projects

            services.AddSingleton<IFilterService>(sp => new FilterService(
                sp.GetRequiredService<IConfigurationService>(),
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();
            services.AddSingleton<IFilterCacheService>(sp => new FilterCacheService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetService<IPlatformServices>()
            ));
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>(sp => new UserProfileService(
                sp.GetRequiredService<IAppDataStore>(),
                sp.GetRequiredService<IPlatformServices>()
            ));
            services.AddSingleton<SearchManager>();
            services.AddSingleton<SearchStateManager>();

            // Note: SoundFlowAudioManager and SoundEffectsService are registered by Desktop Program.cs only
            services.AddSingleton<TransitionService>();
            services.AddSingleton<TriggerService>();
            services.AddSingleton<EventFXService>();
            services.AddSingleton<SearchTransitionManager>(sp => new SearchTransitionManager(
                sp.GetRequiredService<TransitionService>(),
                sp.GetRequiredService<UserProfileService>(),
                sp.GetService<Views.MainWindow>(),
                sp.GetService<Views.BalatroMainMenu>()
            ));
            services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
            services.AddSingleton<FertilizerService>(sp => new FertilizerService(
                sp.GetRequiredService<IDuckDBService>(),
                sp.GetRequiredService<IPlatformServices>()
            ));
            services.AddSingleton<DaylatroHighScoreService>();
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();
            services.AddSingleton<WidgetWindowManager>();
            services.AddTransient<ClauseConversionService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BalatroMainMenuViewModel>(sp => new BalatroMainMenuViewModel(
                sp.GetRequiredService<UserProfileService>(),
                sp.GetService<IApiHostService>(),
                sp.GetService<SoundFlowAudioManager>(),
                sp.GetService<EventFXService>(),
                sp.GetService<WidgetPositionService>()
            ));

            // Views
            // Keep these as singletons so there is exactly one MainMenu instance shared by:
            // - MainWindow (desktop)
            // - ISingleViewApplicationLifetime.MainView (browser/mobile)
            // - Services that need to talk to the active menu (SearchTransitionManager)
            services.AddSingleton<Views.BalatroMainMenu>();
            services.AddSingleton<Views.MainWindow>();
            services.AddSingleton<FiltersModalViewModel>();
            services.AddSingleton<SearchModalViewModel>();

            // Note: AnalyzeModalViewModel and AnalyzerViewModel are registered by Desktop Program.cs only
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<AudioVisualizerSettingsWidgetViewModel>();
            services.AddTransient<MusicMixerWidgetViewModel>();
            services.AddTransient<TransitionDesignerWidgetViewModel>(sp => new TransitionDesignerWidgetViewModel(
                sp.GetService<TransitionService>(),
                sp.GetService<TriggerService>()
            ));
            services.AddTransient<EventFXWidgetViewModel>();
            services.AddTransient<DeckAndStakeViewModel>();
            services.AddTransient<BaseWidgetViewModel>();
            services.AddTransient<GenieWidgetViewModel>();
            services.AddTransient<FilterListViewModel>();
            services.AddTransient<PaginatedFilterBrowserViewModel>();
            services.AddTransient<FilterSelectionModalViewModel>();

            // Filter Tab ViewModels
            services.AddTransient<ViewModels.FilterTabs.VisualBuilderTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.DeckStakeTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.JsonEditorTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.ValidateFilterTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.SaveFilterTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.ConfigureFilterTabViewModel>();

            return services;
        }
    }
}
