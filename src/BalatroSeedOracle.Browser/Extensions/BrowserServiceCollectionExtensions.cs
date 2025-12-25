using Microsoft.Extensions.DependencyInjection;
using BalatroSeedOracle.Services.Storage;

namespace BalatroSeedOracle.Extensions
{
    public static class BrowserServiceCollectionExtensions
    {
        public static IServiceCollection AddBrowserServices(this IServiceCollection services)
        {
            // Register browser-specific app data store
            services.AddSingleton<IAppDataStore, BrowserLocalStorageAppDataStore>();
            
            return services;
        }
    }
}
