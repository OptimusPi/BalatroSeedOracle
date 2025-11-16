using System;

namespace BalatroSeedOracle.Models
{
    public class SearchResultEventArgs : EventArgs
    {
        public SearchResult Result { get; set; } = new();
    }
}
