namespace BalatroSeedOracle.Models
{
    public class FilterItem : SelectableItem
    {
        public FilterItemStatus Status { get; set; }

        public bool IsMustHave => Status == FilterItemStatus.MustHave;
        public bool IsShouldHave => Status == FilterItemStatus.ShouldHave;
        public bool IsMustNotHave => Status == FilterItemStatus.MustNotHave;
    }

    public enum FilterItemStatus
    {
        MustHave,
        ShouldHave,
        MustNotHave,
    }
}
