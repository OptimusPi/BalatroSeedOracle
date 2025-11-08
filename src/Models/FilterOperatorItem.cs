using System.Collections.ObjectModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a logical operator (OR/AND) that can contain child items.
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
            DisplayName = operatorType;
            Category = "Operator";
        }
    }
}
