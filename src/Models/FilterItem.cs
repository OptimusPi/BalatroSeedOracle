namespace BalatroSeedOracle.Models
{
    public class FilterItem : SelectableItem
    {
        public FilterItemStatus Status { get; set; }
    }

    public enum FilterItemStatus
    {
        MustHave,
        ShouldHave,
        MustNotHave
    }
}