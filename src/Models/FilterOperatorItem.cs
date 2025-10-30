using System.Collections.ObjectModel;

namespace BalatroSeedOracle.Models
{
    /// <summary>
    /// Represents a logical operator (OR/AND) that can contain child items.
    /// Acts as a nested drop zone within main drop zones.
    /// </summary>
    public class FilterOperatorItem : FilterItem
    {
        public string OperatorType { get; set; } = "OR"; // "OR" or "AND"

        /// <summary>
        /// Child items contained within this operator.
        /// When serialized:
        /// - OR operator → Multiple Should clauses
        /// - AND operator → Single Must clause with all items
        /// </summary>
        public ObservableCollection<FilterItem> Children { get; set; } = new();

        public FilterOperatorItem(string operatorType)
        {
            OperatorType = operatorType;
            Type = "Operator";
            Name = operatorType;
            DisplayName = operatorType;
            Category = "Operator";
        }
    }
}
