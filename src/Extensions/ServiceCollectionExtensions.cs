using Microsoft.Extensions.DependencyInjection;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBalatroSeedOracleServices(this IServiceCollection services)
        {
            // Services
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IFilterService, FilterService>();
            services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();
            services.AddSingleton<SpriteService>();
            services.AddSingleton<UserProfileService>();
            services.AddSingleton<SearchManager>();
            services.AddSingleton<SoundEffectService>();
            services.AddSingleton<SoundFlowAudioManager>();
            // FavoritesService uses a private constructor and singleton Instance
            services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
            // ClipboardService is static, no DI registration needed
            services.AddSingleton<DaylatroHighScoreService>();
            services.AddSingleton<FilterSerializationService>();
            services.AddSingleton<WidgetPositionService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BalatroMainMenuViewModel>();
            services.AddTransient<FiltersModalViewModel>();
            services.AddTransient<SearchModalViewModel>();
            services.AddTransient<AnalyzeModalViewModel>();
            services.AddTransient<AnalyzerViewModel>();
            services.AddTransient<CreditsModalViewModel>();
            services.AddTransient<ComprehensiveFiltersModalViewModel>();
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