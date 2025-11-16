using System.Collections.ObjectModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a logical operator (OR/AND/BannedItems) that can contain child items.
    /// Acts as a nested drop zone within main drop zones.
    /// </summary>
    public class FilterOperatorItem : FilterItem
    {
        private string _operatorType = "OR";

        public string OperatorType
        {
            get => _operatorType;
            set
            {
                if (_operatorType != value)
                {
                    _operatorType = value;
                    OnPropertyChanged();
                    // Also update related properties
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// User-friendly display name with proper spacing.
        /// Converts "BannedItems" to "Banned Items" for UI display.
        /// </summary>
        public new string DisplayName
        {
            get
            {
                return OperatorType switch
                {
                    "BannedItems" => "Banned Items",
                    _ => OperatorType // OR, AND stay as-is
                };
            }
            set
            {
                // Setter for compatibility, but we ignore it since DisplayName is computed from OperatorType
            }
        }

        /// <summary>
        /// Child items contained within this operator.
        /// When serialized: Creates a clause with type="or" or type="and" and Clauses[] array.
        /// Can be used in must[], should[], or mustNot[] arrays.
        /// </summary>
        public ObservableCollection<FilterItem> Children { get; set; } = new();

        public FilterOperatorItem(string operatorType)
        {
            _operatorType = operatorType;
            Type = "Operator";
            Name = operatorType;
            Category = "Operator";
            // DisplayName is now a computed property based on OperatorType
        }
    }
}
