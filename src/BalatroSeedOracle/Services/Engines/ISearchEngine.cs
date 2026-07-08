using System;
using System.Threading.Tasks;
using Motely;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.Services.Engines
{
    /// <summary>
    /// Abstraction for a search execution backend (Local CPU or Remote API).
    /// </summary>
    public interface ISearchEngine
    {
        string Name { get; }
        bool IsLocal { get; }

        /// <summary>
        /// Starts a search with the given configuration.
        /// </summary>
        Task<string> StartSearchAsync(JamlConfig config, SearchOptionsDto options);

        /// <summary>
        /// Stops a running search.
        /// </summary>
        Task StopSearchAsync(string searchId);

        /// <summary>
        /// Checks if the engine is available/connected.
        /// </summary>
        Task<bool> PingAsync();
    }
}
