using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a group of filter items (e.g., "Legendary Jokers", "Vouchers")
    /// Used for visual grouping in filter tabs.
    /// Single shared model - eliminates 3 duplicate class definitions (~20 lines saved).
    /// </summary>
    public class FilterItemGroup : ObservableObject
    {
        public string GroupName { get; set; } = "";
        public ObservableCollection<FilterItem> Items { get; set; } = new();

        // All items render at same size: 5-wide (380px shelf, 70px cards)
        public double ShelfMaxWidth => 380;
        public double CardWidth => 70;
        public double CardHeight => 110;
        public double ImageWidth => 64;
        public double ImageHeight => 85;
    }
}
