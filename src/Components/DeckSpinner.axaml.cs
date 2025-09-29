using System;
using Avalonia.Controls;
using BalatroSeedOracle.Controls;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Simple wrapper around PanelSpinner for deck selection compatibility
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
            
            // Wire up the selection changed event to DeckChanged
            if (_innerSpinner != null)
            {
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
        public string SelectedDeckName => "Red"; // Default for now
        
        public void SetStakeIndex(int stakeIndex)
        {
            // TODO: Implement when PanelSpinner API is understood
        }
    }
}