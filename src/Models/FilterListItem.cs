using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a filter entry in the selector list
    /// </summary>
    public partial class FilterListItem : ObservableObject
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Description { get; set; } = "";

        /// <summary>
        /// Display index for the current page (1-based like Balatro)
        /// </summary>
        public string DisplayIndex => Number.ToString();

        [ObservableProperty]
        private bool _isSelected = false;
    }
}