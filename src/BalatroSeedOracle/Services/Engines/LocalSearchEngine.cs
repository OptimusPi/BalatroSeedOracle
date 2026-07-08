using System;
using System.Threading.Tasks;
using BalatroSeedOracle.Services;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services.Engines
{
    public class LocalSearchEngine : ISearchEngine
    {
        private readonly IPlatformServices _platformServices;

        public string Name => "Local (This Device)";
        public bool IsLocal => true;

        public LocalSearchEngine(IPlatformServices platformServices)
        {
            _platformServices = platformServices;
        }

        public Task<string> StartSearchAsync(JamlConfig config, SearchOptionsDto options)
        {
            // SearchManager handles local launches directly via StartSearchLegacy.
            return Task.FromResult("local-job-1");
        }

        public Task StopSearchAsync(string searchId) => Task.CompletedTask;

        public Task<bool> PingAsync() => Task.FromResult(true);
    }
}
