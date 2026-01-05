using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.DuckDB;
using BalatroSeedOracle.Services.Storage;
using BalatroSeedOracle.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBalatroSeedOracleServices(
            this IServiceCollection services
        )
        {

            // Services
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // App data storage (Desktop: files, Browser: localStorage)
#if BROWSER
            services.AddSingleton<IAppDataStore, BrowserLocalStorageAppDataStore>();
#else
            services.AddSingleton<IAppDataStore, DesktopAppDataStore>();
#endif

            // DuckDB - Platform-specific implementations
#if BROWSER
            services.AddSingleton<IDuckDBService, BrowserDuckDBService>();
#else
            services.AddSingleton<IDuckDBService, DesktopDuckDBService>();
#endif
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();
            services.AddSingleton<IFilterCacheService, FilterCacheService>();
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<SearchManager>();
            services.AddSingleton<SearchStateManager>();
#if !BROWSER
            services.AddSingleton<SoundFlowAudioManager>();
            services.AddSingleton<SoundEffectsService>();
#endif
            services.AddSingleton<TransitionService>();
            services.AddSingleton<TriggerService>();
            services.AddSingleton<EventFXService>();
            services.AddSingleton<SearchTransitionManager>();
            services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
            services.AddSingleton<FertilizerService>();
            services.AddSingleton<DaylatroHighScoreService>();
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();
            services.AddSingleton<WidgetWindowManager>();
            services.AddTransient<ClauseConversionService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BalatroMainMenuViewModel>();

            // Views (for browser DI)
            services.AddTransient<Views.BalatroMainMenu>();
            services.AddSingleton<FiltersModalViewModel>();
            services.AddSingleton<SearchModalViewModel>();
            #if !BROWSER
            services.AddTransient<AnalyzeModalViewModel>();
            services.AddTransient<AnalyzerViewModel>();
#endif
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<AudioVisualizerSettingsWidgetViewModel>();
            services.AddTransient<MusicMixerWidgetViewModel>();
            services.AddTransient<TransitionDesignerWidgetViewModel>(sp =>
                new TransitionDesignerWidgetViewModel(
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
