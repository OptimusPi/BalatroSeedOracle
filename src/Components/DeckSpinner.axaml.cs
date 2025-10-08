using System;
using Avalonia.Controls;
using BalatroSeedOracle.Controls;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Deck selection component using PanelSpinner with deck images from SpriteService
    /// </summary>
    public partial class DeckSpinner : UserControl
    {
        private PanelSpinner? _innerSpinner;

        public event EventHandler<int>? DeckChanged;

        public DeckSpinner()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _innerSpinner = this.FindControl<PanelSpinner>("InnerPanelSpinner");

            if (_innerSpinner != null)
            {
                // Populate with deck items using factory (includes deck images via SpriteService)
                _innerSpinner.Items = PanelItemFactory.CreateDeckItems();

                // Wire up the selection changed event to DeckChanged
                _innerSpinner.SelectionChanged += (s, item) =>
                {
                    if (item != null)
                    {
                        SelectedDeckIndex = _innerSpinner.SelectedIndex;
                        DeckChanged?.Invoke(this, SelectedDeckIndex);
                    }
                };
            }
        }

        // Compatibility properties for existing code
        public int SelectedDeckIndex { get; set; } = 0;

        public string SelectedDeckName
        {
            get
            {
                if (_innerSpinner?.SelectedItem != null)
                {
                    return _innerSpinner.SelectedItem.Title;
                }
                return "Red Deck"; // Default
            }
        }

        public void SetStakeIndex(int stakeIndex)
        {
            // Stakes are handled separately by DeckAndStakeSelector's StakeSpinner
        }
    }
}
