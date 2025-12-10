using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Motely.Services;

namespace BalatroSeedOracle.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBalatroSeedOracleServices(
            this IServiceCollection services
        )
        {
            // Platform-specific services (DI selects correct implementation)
#if BROWSER
            services.AddSingleton<IFertilizerService, InMemoryFertilizerService>();
#else
            services.AddSingleton<IFertilizerService, DuckDbFertilizerService>();
#endif

            // Services
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();
            services.AddSingleton<IFilterCacheService, FilterCacheService>();
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<SearchManager>();
            services.AddSingleton<SoundFlowAudioManager>();
            services.AddSingleton<SoundEffectsService>();
            services.AddSingleton<TransitionService>();
            services.AddSingleton<EventFXService>();
            services.AddSingleton<SearchTransitionManager>();
            services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
            services.AddSingleton<DaylatroHighScoreService>();
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();
            services.AddTransient<ClauseConversionService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BalatroMainMenuViewModel>();
            services.AddSingleton<FiltersModalViewModel>();
            services.AddSingleton<SearchModalViewModel>();
            services.AddTransient<AnalyzeModalViewModel>();
            services.AddTransient<AnalyzerViewModel>();
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<AudioVisualizerSettingsWidgetViewModel>();
            services.AddTransient<MusicMixerWidgetViewModel>();
            services.AddTransient<TransitionDesignerWidgetViewModel>();
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
