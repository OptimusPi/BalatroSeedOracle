namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Result returned from FilterSelectionModal indicating what action the user took
    /// </summary>
    public class FilterSelectionResult
    {
        public bool Cancelled { get; set; }
        public FilterAction Action { get; set; }
        public string? FilterId { get; set; }
    }

    /// <summary>
    /// Actions available in FilterSelectionModal
    /// </summary>
    public enum FilterAction
    {
        Cancelled,
        CreateNew,
        Search,
        Edit,
        Copy,
        Delete,
        Analyze,
    }
}
