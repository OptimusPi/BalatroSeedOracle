using System;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Services.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BalatroSeedOracle.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBalatroSeedOracleServices(this IServiceCollection services)
    {
        // ── Core Services ──
        services.AddSingleton<IFilterCacheService>(sp => new FilterCacheService(
            sp.GetRequiredService<IAppDataStore>(),
            sp.GetService<IPlatformServices>()
        ));
        services.AddSingleton<IConfigurationService>(sp => new ConfigurationService(
            sp.GetRequiredService<IAppDataStore>(),
            sp.GetService<UserProfileService>(),
            sp.GetService<IFilterCacheService>(),
            sp.GetService<IPlatformServices>()
        ));
        services.AddSingleton<IFilterService>(sp => new FilterService(
            sp.GetRequiredService<IConfigurationService>(),
            sp.GetRequiredService<IAppDataStore>(),
            sp.GetService<IPlatformServices>()
        ));
        services.AddSingleton<UserProfileService>(sp => new UserProfileService(
            sp.GetRequiredService<IAppDataStore>(),
            sp.GetRequiredService<IPlatformServices>()
        ));
        services.AddSingleton<SearchManager>();
        services.AddSingleton<TransitionService>();
        services.AddSingleton<TriggerService>();
        services.AddSingleton<EventFXService>();
        services.AddSingleton<FavoritesService>(_ => FavoritesService.Instance);
        services.AddSingleton<DaylatroHighScoreService>();
        services.AddSingleton<FilterSerializationService>();
        services.AddSingleton<WidgetPositionService>();
        services.AddSingleton<WidgetWindowManager>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<SearchTransitionManager>(sp => new SearchTransitionManager(
            sp.GetRequiredService<TransitionService>(),
            sp.GetRequiredService<UserProfileService>()
        ));
        services.AddTransient<ClauseConversionService>();

        return services;
    }
}
