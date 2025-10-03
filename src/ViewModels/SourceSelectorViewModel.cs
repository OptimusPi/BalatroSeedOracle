using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the SourceSelector control.
    /// Manages item source selection with descriptions and previews.
    /// </summary>
    public class SourceSelectorViewModel : BaseViewModel
    {
        private SourceOptionViewModel? _selectedSource;
        private string _description = string.Empty;
        private bool _isPreviewVisible;
        private string _previewText = string.Empty;
        private string _previewColor = string.Empty;

        public SourceSelectorViewModel()
        {
            InitializeSources();

            // Set default selection (Any Source)
            if (Sources.Count > 0)
            {
                SelectedSource = Sources[0];
            }
        }

        #region Properties

        public ObservableCollection<SourceOptionViewModel> Sources { get; } = new();

        public SourceOptionViewModel? SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (SetProperty(ref _selectedSource, value))
                {
                    UpdateDisplay();
                    RaiseSourceChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            private set => SetProperty(ref _description, value);
        }

        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            private set => SetProperty(ref _isPreviewVisible, value);
        }

        public string PreviewText
        {
            get => _previewText;
            private set => SetProperty(ref _previewText, value);
        }

        public string PreviewColor
        {
            get => _previewColor;
            private set => SetProperty(ref _previewColor, value);
        }

        #endregion

        #region Events

        public event EventHandler<string>? SourceChanged;

        #endregion

        #region Initialization

        private void InitializeSources()
        {
            Sources.Add(new SourceOptionViewModel("", "Any Source", "Any source - item can come from anywhere in the run", "🌟"));
            Sources.Add(new SourceOptionViewModel("SmallBlindTag", "🏷️ Small Blind Tag", "Small Blind Skip Tag - obtained by skipping small blinds", "🏷️"));
            Sources.Add(new SourceOptionViewModel("BigBlindTag", "🏷️ Big Blind Tag", "Big Blind Skip Tag - obtained by skipping big blinds", "🏷️"));
            Sources.Add(new SourceOptionViewModel("StandardPack", "📦 Standard Pack", "Standard Booster Pack - contains random cards/items", "📦"));
            Sources.Add(new SourceOptionViewModel("BuffoonPack", "🃏 Buffoon Pack", "Buffoon Pack - contains 2 jokers + 1 consumable", "🃏"));
            Sources.Add(new SourceOptionViewModel("Shop", "🛒 Shop", "Shop - purchasable from the shop during blinds", "🛒"));
            Sources.Add(new SourceOptionViewModel("StartingItems", "⭐ Starting Items", "Starting Items - items that come with the deck/stake", "⭐"));
        }

        #endregion

        #region Private Methods

        private void UpdateDisplay()
        {
            if (_selectedSource == null)
            {
                Description = "Unknown source";
                IsPreviewVisible = false;
                return;
            }

            Description = _selectedSource.Description;

            if (string.IsNullOrEmpty(_selectedSource.Tag))
            {
                IsPreviewVisible = false;
            }
            else
            {
                IsPreviewVisible = true;

                var sourceName = _selectedSource.Tag switch
                {
                    "SmallBlindTag" => "Small Blind Tag",
                    "BigBlindTag" => "Big Blind Tag",
                    "StandardPack" => "Standard Pack",
                    "BuffoonPack" => "Buffoon Pack",
                    "Shop" => "Shop Purchase",
                    "StartingItems" => "Starting Item",
                    _ => "Unknown Source"
                };

                PreviewText = $"{_selectedSource.Emoji} Searching for items from: {sourceName}";

                // Set color based on source type
                PreviewColor = _selectedSource.Tag switch
                {
                    "SmallBlindTag" or "BigBlindTag" => "#FFD700", // Gold for tags
                    "StandardPack" or "BuffoonPack" => "#00BFFF",  // Blue for packs
                    "Shop" => "#32CD32",                            // Green for shop
                    "StartingItems" => "#FF69B4",                   // Pink for starting
                    _ => "#CCCCCC"                                  // Gray for any
                };
            }
        }

        private void RaiseSourceChanged()
        {
            SourceChanged?.Invoke(this, _selectedSource?.Tag ?? "");
        }

        #endregion

        #region Public Methods

        public string GetSelectedSource()
        {
            return _selectedSource?.Tag ?? "";
        }

        public void SetSelectedSource(string source)
        {
            var sourceOption = Sources.FirstOrDefault(s => s.Tag == source);
            if (sourceOption != null)
            {
                SelectedSource = sourceOption;
            }
        }

        public static string GetSourceDisplayName(string source)
        {
            return source switch
            {
                "" => "Any Source",
                "SmallBlindTag" => "Small Blind Tag",
                "BigBlindTag" => "Big Blind Tag",
                "StandardPack" => "Standard Pack",
                "BuffoonPack" => "Buffoon Pack",
                "Shop" => "Shop",
                "StartingItems" => "Starting Items",
                _ => "Unknown"
            };
        }

        public static string[] GetAllSources()
        {
            return new[]
            {
                "",
                "SmallBlindTag",
                "BigBlindTag",
                "StandardPack",
                "BuffoonPack",
                "Shop",
                "StartingItems"
            };
        }

        #endregion
    }

    /// <summary>
    /// ViewModel for a source option
    /// </summary>
    public class SourceOptionViewModel
    {
        public SourceOptionViewModel(string tag, string displayName, string description, string emoji)
        {
            Tag = tag;
            DisplayName = displayName;
            Description = description;
            Emoji = emoji;
        }

        public string Tag { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Emoji { get; }
    }
}
