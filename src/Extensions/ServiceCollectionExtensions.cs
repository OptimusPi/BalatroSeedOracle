using BalatroSeedOracle.Services;
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
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();
            services.AddSingleton<IFilterCacheService, FilterCacheService>();
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<SearchManager>();
            // services.AddSingleton<SoundEffectService>(); // Removed - NAudio dependency
            services.AddSingleton<SoundFlowAudioManager>();
            services.AddSingleton<SoundEffectsService>(); // UI sound effects (card hover, button clicks, etc.)
            // FavoritesService uses a private constructor and singleton Instance
            services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
            // ClipboardService is static, no DI registration needed
            services.AddSingleton<DaylatroHighScoreService>();
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BalatroMainMenuViewModel>();
            services.AddSingleton<FiltersModalViewModel>();       // One filter modal at a time
            services.AddSingleton<SearchModalViewModel>();        // One search modal at a time
            services.AddTransient<AnalyzeModalViewModel>();
            services.AddTransient<AnalyzerViewModel>();
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<AudioVisualizerSettingsWidgetViewModel>();
            services.AddTransient<MusicMixerWidgetViewModel>();

            // Filter Tab ViewModels
            services.AddTransient<ViewModels.FilterTabs.VisualBuilderTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.JsonEditorTabViewModel>();
            services.AddTransient<ViewModels.FilterTabs.SaveFilterTabViewModel>();

            return services;
        }
    }
}
