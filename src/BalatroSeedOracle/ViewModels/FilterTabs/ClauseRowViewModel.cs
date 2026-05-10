using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// ViewModel for a single clause row in the Validate Filter tab
    /// Supports nested clauses for OR/AND operators
    /// </summary>
    public partial class ClauseRowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _clauseType = ""; // "Joker", "Or", "And", etc.

        [ObservableProperty]
        private string _displayText = ""; // Human-readable description

        [ObservableProperty]
        private string _itemKey = ""; // Reference to ItemConfig

        [ObservableProperty]
        private bool _isExpanded = false; // For nested OR/AND

        [ObservableProperty]
        private int _nestingLevel = 0; // 0 = top-level, 1+ = nested

        [ObservableProperty]
        private ObservableCollection<ClauseRowViewModel> _children = new();

        // Display properties
        [ObservableProperty]
        private IImage? _iconPath; // Sprite image from SpriteService

        [ObservableProperty]
        private string? _editionBadge; // "Foil", "Holographic", etc.

        [ObservableProperty]
        private string _anteRange = ""; // "Antes 1-3"

        [ObservableProperty]
        private int? _minCount; // "Min: 2"

        [ObservableProperty]
        private int _scoreValue; // For Should clauses

        // Commands (will be wired up by parent ViewModel)
        public ICommand? EditClauseCommand { get; set; }
        public ICommand? RemoveClauseCommand { get; set; }
        public ICommand? ToggleExpandCommand { get; set; }

        public ClauseRowViewModel()
        {
            // Initialize toggle expand command
            ToggleExpandCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                IsExpanded = !IsExpanded;
            });
        }

        /// <summary>
        /// Check if this is an operator clause (OR/AND)
        /// </summary>
        public bool IsOperator => ClauseType == "Or" || ClauseType == "And";

        /// <summary>
        /// Check if this clause has children
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// Check if this clause has a score value
        /// </summary>
        public bool HasScore => ScoreValue > 0;

        /// <summary>
        /// Get margin for indentation based on nesting level
        /// </summary>
        public string IndentMargin => $"{NestingLevel * 20},0,0,0";

        /// <summary>
        /// Category string for icon sizing (CategoryToWidth/Height converters). Uses ClauseType for operator clauses.
        /// </summary>
        public string Category => ClauseType;
    }
}
