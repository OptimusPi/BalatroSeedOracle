using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Dock zone overlay with visual feedback during drag operations
    /// </summary>
    public partial class DockZoneOverlay : UserControl
    {
        public static readonly StyledProperty<bool> IsHighlightedProperty =
            AvaloniaProperty.Register<DockZoneOverlay, bool>(nameof(IsHighlighted));

        public static readonly StyledProperty<string> DisplayTextProperty =
            AvaloniaProperty.Register<DockZoneOverlay, string>(nameof(DisplayText), "DOCK WIDGET");

        public bool IsHighlighted
        {
            get => GetValue(IsHighlightedProperty);
            set => SetValue(IsHighlightedProperty, value);
        }

        public string DisplayText
        {
            get => GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        public DockZoneOverlay()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (change.Property == IsHighlightedProperty)
            {
                UpdateHighlight();
            }
        }

        private void UpdateHighlight()
        {
            // Visual feedback when highlighted
            Opacity = IsHighlighted ? 1.0 : 0.6;
        }
    }
}