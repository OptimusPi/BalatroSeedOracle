using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Motely.Filters;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// MVVM ViewModel wrapper around IJamlClause for the Visual Builder tab.
    /// IJamlClause remains the single source of truth for filter data.
    /// This wrapper adds: selection state, drag state, images, and observable children.
    /// </summary>
    public partial class FilterBuilderItemViewModel : ObservableObject
    {
        /// <summary>
        /// The actual domain model - single source of truth for filter configuration
        /// </summary>
        public IJamlClause Clause { get; }

        // UI-only state (NOT persisted to JSON)
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isBeingDragged;

        [ObservableProperty]
        private IImage? _itemImage;

        [ObservableProperty]
        private IImage? _soulFaceImage;

        // Computed display properties that delegate to Clause via extension methods
        public string DisplayName => Clause.GetValueName() ?? "";
        public string Name => Clause.GetValueName() ?? "";
        public string ItemType => Clause.GetTypeName() ?? "";
        public string Type => Clause.GetTypeName() ?? "";
        public string ItemKey => Clause.GetValueName() ?? "";
        public string Category => Clause.GetTypeName() ?? "";

        // Additional UI-only properties for compatibility with existing code
        public bool IsFavorite { get; set; }

        // For OR/AND operators - wraps LogicClause.Clauses
        public ObservableCollection<FilterBuilderItemViewModel>? ChildViewModels { get; set; }

        // Check if this is an operator clause
        public bool IsOperatorClause => Clause is LogicClause;
        public string? OperatorType => Clause switch
        {
            AndClause => "And",
            OrClause => "Or",
            _ => null
        };

        public FilterBuilderItemViewModel(IJamlClause clause)
        {
            Clause = clause;

            // Load images synchronously (they're already cached by SpriteService)
            LoadImages();

            // Wrap children if this is a LogicClause (OR/AND)
            if (clause is LogicClause logic && logic.Clauses.Length > 0)
            {
                ChildViewModels = new ObservableCollection<FilterBuilderItemViewModel>(
                    logic.Clauses.Select(child => new FilterBuilderItemViewModel(child))
                );
            }
        }

        private void LoadImages()
        {
            var valueName = Clause.GetValueName();
            var typeName = Clause.GetTypeName();

            if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(valueName))
            {
                ItemImage = SpriteService.Instance.GetItemImage(valueName, typeName);

                if (typeName == "joker" || typeName == "legendaryJoker")
                {
                    SoulFaceImage = SpriteService.Instance.GetJokerSoulImage(valueName);
                }
            }
        }

        /// <summary>
        /// Update LogicClause.Clauses from ChildViewModels (when user modifies OR/AND clause)
        /// </summary>
        public void SyncChildrenToConfig()
        {
            if (ChildViewModels is not null && Clause is LogicClause logic)
            {
                logic.Clauses = ChildViewModels.Select(vm => vm.Clause).ToArray();
            }
        }
    }
}
