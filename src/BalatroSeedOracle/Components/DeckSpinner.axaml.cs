using System;
using Avalonia;
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

        // Expose ShowArrows property to forward to inner PanelSpinner
        public static readonly StyledProperty<bool> ShowArrowsProperty =
            Avalonia.AvaloniaProperty.Register<DeckSpinner, bool>(
                nameof(ShowArrows),
                defaultValue: true
            );

        public bool ShowArrows
        {
            get => GetValue(ShowArrowsProperty);
            set => SetValue(ShowArrowsProperty, value);
        }

        // Bindable SelectedDeckIndex for MVVM
        public static readonly StyledProperty<int> SelectedDeckIndexProperty =
            Avalonia.AvaloniaProperty.Register<DeckSpinner, int>(
                nameof(SelectedDeckIndex),
                defaultValue: 0,
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay
            );

        public int SelectedDeckIndex
        {
            get => GetValue(SelectedDeckIndexProperty);
            set => SetValue(SelectedDeckIndexProperty, value);
        }

        // Bindable StakeIndex for MVVM (updates overlay)
        public static readonly StyledProperty<int> StakeIndexProperty =
            Avalonia.AvaloniaProperty.Register<DeckSpinner, int>(
                nameof(StakeIndex),
                defaultValue: 0
            );

        public int StakeIndex
        {
            get => GetValue(StakeIndexProperty);
            set => SetValue(StakeIndexProperty, value);
        }

        public DeckSpinner()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Direct field access from x:Name
            _innerSpinner = InnerPanelSpinner;

            if (_innerSpinner != null)
            {
                // Forward ShowArrows property to inner PanelSpinner
                _innerSpinner.ShowArrows = ShowArrows;

                // Populate with deck items using factory (includes deck images via SpriteService)
                _innerSpinner.Items = PanelItemFactory.CreateDeckItems();

                // Wire up the selection changed event to DeckChanged
                _innerSpinner.SelectionChanged += (s, item) =>
                {
                    if (item != null)
                    {
                        // Reflect inner selection into bindable property
                        SelectedDeckIndex = _innerSpinner.SelectedIndex;
                        DeckChanged?.Invoke(this, SelectedDeckIndex);
                    }
                };

                // Apply initial SelectedDeckIndex
                _innerSpinner.SelectedIndex = Math.Max(
                    0,
                    Math.Min(SelectedDeckIndex, _innerSpinner.Items.Count - 1)
                );

                // Apply initial stake overlay to deck images
                SetStakeIndex(StakeIndex);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ShowArrowsProperty)
            {
                if (_innerSpinner != null)
                {
                    _innerSpinner.ShowArrows = change.GetNewValue<bool>();
                }
            }
            else if (change.Property == StakeIndexProperty)
            {
                SetStakeIndex(change.GetNewValue<int>());
            }
        }

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
            // Map index to stake name
            string stakeName = stakeIndex switch
            {
                0 => "White",
                1 => "Red",
                2 => "Green",
                3 => "Black",
                4 => "Blue",
                5 => "Purple",
                6 => "Orange",
                7 => "Gold",
                _ => "White",
            };

            // Refresh inner spinner images to include stake sticker
            if (_innerSpinner != null)
            {
                int current = _innerSpinner.SelectedIndex;
                _innerSpinner.Items = PanelItemFactory.CreateDeckItemsWithStake(stakeName);
                _innerSpinner.SelectedIndex = Math.Max(
                    0,
                    Math.Min(current, _innerSpinner.Items.Count - 1)
                );
                _innerSpinner.RefreshCurrentImage();
            }
        }
    }
}
