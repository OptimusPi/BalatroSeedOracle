using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// MVVM ViewModel wrapper around ItemConfig for the Visual Builder tab.
    /// ItemConfig remains the single source of truth for filter data.
    /// This wrapper adds: selection state, drag state, images, and observable children.
    /// </summary>
    public partial class FilterBuilderItemViewModel : ObservableObject
    {
        /// <summary>
        /// The actual domain model - single source of truth for filter configuration
        /// </summary>
        public ItemConfig Config { get; }

        // UI-only state (NOT persisted to JSON)
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isBeingDragged;

        [ObservableProperty]
        private IImage? _itemImage;

        [ObservableProperty]
        private IImage? _soulFaceImage;

        // Computed display properties that delegate to Config
        public string DisplayName => Config.ItemName;
        public string Name => Config.ItemName; // Alias for compatibility
        public string ItemType => Config.ItemType;
        public string Type => Config.ItemType; // Alias for compatibility
        public string ItemKey => Config.ItemKey;
        public string Category => Config.ItemType; // For filtering/grouping

        // Additional UI-only properties for compatibility with existing code
        public bool IsFavorite { get; set; }

        // For OR/AND operators - wraps Config.Children
        public ObservableCollection<FilterBuilderItemViewModel>? ChildViewModels { get; set; }

        // Check if this is an operator clause
        public bool IsOperatorClause => Config.OperatorType != null;
        public string? OperatorType => Config.OperatorType;

        public FilterBuilderItemViewModel(ItemConfig config)
        {
            Config = config;

            // Load images synchronously (they're already cached by SpriteService)
            LoadImages();

            // Wrap children if this is an OR/AND operator
            if (config.Children != null && config.Children.Count > 0)
            {
                ChildViewModels = new ObservableCollection<FilterBuilderItemViewModel>(
                    config.Children.Select(child => new FilterBuilderItemViewModel(child))
                );
            }
        }

        private void LoadImages()
        {
            // Load main item image
            if (!string.IsNullOrEmpty(Config.ItemType) && !string.IsNullOrEmpty(Config.ItemName))
            {
                ItemImage = SpriteService.Instance.GetItemImage(Config.ItemName, Config.ItemType);

                // Check for soul face variants (legendary jokers)
                if (Config.ItemType == "Joker")
                {
                    SoulFaceImage = SpriteService.Instance.GetJokerSoulImage(Config.ItemName);
                }
            }
        }

        /// <summary>
        /// Update Config.Children from ChildViewModels (when user modifies OR/AND clause)
        /// </summary>
        public void SyncChildrenToConfig()
        {
            if (ChildViewModels != null)
            {
                Config.Children = ChildViewModels.Select(vm => vm.Config).ToList();
            }
        }
    }
}
