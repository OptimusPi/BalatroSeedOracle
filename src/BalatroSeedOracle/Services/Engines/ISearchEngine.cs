using System;
using System.Threading.Tasks;
using Motely.Filters; // Restored
using Motely; // For SearchOptionsDto

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
        Task<string> StartSearchAsync(MotelyJsonConfig config, SearchOptionsDto options);
        
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
