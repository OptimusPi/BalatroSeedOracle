using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Controls
{
    public partial class EnhancedItemConfigPopup : UserControl
    {
        public class ItemConfig
        {
            public string ItemName { get; set; } = "";
            public string ItemType { get; set; } = "";
            public int Score { get; set; } = 10;
            public int[] SearchAntes { get; set; } = new int[] { 1, 2, 3 };
            public string[] Sources { get; set; } = new string[] { "Shop", "SmallBlindTag", "BigBlindTag" };
            public string Edition { get; set; } = "Any";
        }

        public event EventHandler<ItemConfig>? ConfigSaved;
        public event EventHandler? ConfigCancelled;

        private ItemConfig _currentConfig = new();

        public EnhancedItemConfigPopup()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            Loaded += (s, e) =>
            {
                var scoreSlider = this.FindControl<Slider>("ScoreSlider")!;
                var scoreValue = this.FindControl<TextBlock>("ScoreValue")!;
                var anteSelector = this.FindControl<AnteSelector>("AnteSelector")!;
                var sourceSelector = this.FindControl<SourceSelector>("SourceSelector")!;
                var editionSelector = this.FindControl<EditionSelector>("EditionSelector")!;

                // Score slider updates
                scoreSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty)
                    {
                        var value = (int)scoreSlider.Value;
                        scoreValue.Text = value.ToString();
                        _currentConfig.Score = value;
                    }
                };

                // Ante selector updates
                anteSelector.SelectedAntesChanged += (s, antes) =>
                {
                    _currentConfig.SearchAntes = antes;
                };

                // Source selector updates
                sourceSelector.SourceChanged += (s, source) =>
                {
                    _currentConfig.Sources = new string[] { source };
                };

                // Edition selector updates
                editionSelector.EditionChanged += (s, edition) =>
                {
                    _currentConfig.Edition = edition;
                };
            };
        }

        public void ConfigureItem(string itemName, string itemType, Avalonia.Media.IImage? itemImage = null)
        {
            var itemNameBlock = this.FindControl<TextBlock>("ItemName")!;
            var itemImageControl = this.FindControl<Image>("ItemImage")!;

            itemNameBlock.Text = $"Configure {itemName}";
            
            if (itemImage != null)
            {
                itemImageControl.Source = itemImage;
            }

            _currentConfig.ItemName = itemName;
            _currentConfig.ItemType = itemType;
        }

        public void SetConfiguration(ItemConfig config)
        {
            _currentConfig = config;

            // Update controls with the configuration
            Loaded += (s, e) =>
            {
                var scoreSlider = this.FindControl<Slider>("ScoreSlider")!;
                var scoreValue = this.FindControl<TextBlock>("ScoreValue")!;
                var anteSelector = this.FindControl<AnteSelector>("AnteSelector")!;
                var sourceSelector = this.FindControl<SourceSelector>("SourceSelector")!;
                var editionSelector = this.FindControl<EditionSelector>("EditionSelector")!;

                scoreSlider.Value = config.Score;
                scoreValue.Text = config.Score.ToString();
                anteSelector.SetSelectedAntes(config.SearchAntes);
                if (config.Sources != null && config.Sources.Length > 0)
                    sourceSelector.SetSelectedSource(config.Sources[0]);
                editionSelector.SetSelectedEdition(config.Edition);
            };
        }

        private void OnSave(object? sender, RoutedEventArgs e)
        {
            ConfigSaved?.Invoke(this, _currentConfig);
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            ConfigCancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
