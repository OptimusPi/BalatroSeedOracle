using System;
using System.Threading.Tasks;
using Motely.Filters;
using Motely; // For SearchOptionsDto
using Motely.Executors; // For MotelySearchOrchestrator
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Services.Engines
{
    public class LocalSearchEngine : ISearchEngine
    {
        private readonly IPlatformServices _platformServices;
        private IMotelySearchContext? _currentContext;

        public string Name => "Local (This Device)";
        public bool IsLocal => true;

        public LocalSearchEngine(IPlatformServices platformServices)
        {
            _platformServices = platformServices;
        }

        public Task<string> StartSearchAsync(MotelyJsonConfig config, SearchOptionsDto options)
        {
            // Determine storage mode based on platform capabilities
            // WASM -> InMemory, Desktop -> FileSystem
            bool useInMemory = !_platformServices.SupportsFileSystem;

            // Map DTO to JsonSearchParams
            var parameters = new JsonSearchParams
            {
                Threads = options.ThreadCount ?? Environment.ProcessorCount,
                BatchSize = options.BatchSize ?? 4,
                StartBatch = (ulong)(options.StartBatch ?? 0),
                EndBatch = (ulong)(options.EndBatch ?? 1000000),
                // Map StartSeed to StartBatch if provided
                SpecificSeed = options.SpecificSeed
            };

            if (!string.IsNullOrEmpty(options.StartSeed))
            {
                // SeedMath handles conversion
                 parameters.StartBatch = (ulong)SeedMath.SeedToSearchIndex(options.StartSeed);
            }

            // Launch the orchestrator synchronously (it runs on its own threads)
            _currentContext = MotelySearchOrchestrator.LaunchWithContext(
                config, 
                parameters, 
                useInMemory
            );

            // Return a dummy ID since local search is singleton-ish in this context
            return Task.FromResult("local-job-1");
        }

        public Task StopSearchAsync(string searchId)
        {
            if (_currentContext != null)
            {
                _currentContext.Cancel();
                _currentContext = null;
            }
            return Task.CompletedTask;
        }

        public Task<bool> PingAsync() => Task.FromResult(true);
    }
}
