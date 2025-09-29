using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace BalatroSeedOracle.Controls
{
    public partial class SourceSelector : UserControl
    {
        private readonly Dictionary<string, string> _sourceDescriptions = new()
        {
            { "", "Any source - item can come from anywhere in the run" },
            { "SmallBlindTag", "Small Blind Skip Tag - obtained by skipping small blinds" },
            { "BigBlindTag", "Big Blind Skip Tag - obtained by skipping big blinds" },
            { "StandardPack", "Standard Booster Pack - contains random cards/items" },
            { "BuffoonPack", "Buffoon Pack - contains 2 jokers + 1 consumable" },
            { "Shop", "Shop - purchasable from the shop during blinds" },
            { "StartingItems", "Starting Items - items that come with the deck/stake" }
        };
        
        private readonly Dictionary<string, string> _sourceEmojis = new()
        {
            { "", "🌟" },
            { "SmallBlindTag", "🏷️" },
            { "BigBlindTag", "🏷️" },
            { "StandardPack", "📦" },
            { "BuffoonPack", "🃏" },
            { "Shop", "🛒" },
            { "StartingItems", "⭐" }
        };
        
        public event EventHandler<string>? SourceChanged;
        
        public SourceSelector()
        {
            InitializeComponent();
        }
        
        public string GetSelectedSource()
        {
            var comboBox = this.FindControl<ComboBox>("SourceComboBox")!;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            return selectedItem?.Tag?.ToString() ?? "";
        }
        
        public void SetSelectedSource(string source)
        {
            var comboBox = this.FindControl<ComboBox>("SourceComboBox")!;
            
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && 
                    item.Tag?.ToString() == source)
                {
                    comboBox.SelectedIndex = i;
                    UpdateDescription(source);
                    break;
                }
            }
        }
        
        private void SourceComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem)
            {
                var sourceTag = selectedItem.Tag?.ToString() ?? "";
                UpdateDescription(sourceTag);
                UpdatePreview(sourceTag);
                SourceChanged?.Invoke(this, sourceTag);
            }
        }
        
        private void UpdateDescription(string source)
        {
            var descriptionTextBlock = this.FindControl<TextBlock>("DescriptionTextBlock")!;
            
            if (_sourceDescriptions.TryGetValue(source, out var description))
            {
                descriptionTextBlock.Text = description;
            }
            else
            {
                descriptionTextBlock.Text = "Unknown source";
            }
        }
        
        private void UpdatePreview(string source)
        {
            var previewBorder = this.FindControl<Border>("PreviewBorder")!;
            var previewTextBlock = this.FindControl<TextBlock>("PreviewTextBlock")!;
            
            if (string.IsNullOrEmpty(source))
            {
                previewBorder.IsVisible = false;
                return;
            }
            
            previewBorder.IsVisible = true;
            
            var emoji = _sourceEmojis.TryGetValue(source, out var e) ? e : "❓";
            var sourceName = source switch
            {
                "SmallBlindTag" => "Small Blind Tag",
                "BigBlindTag" => "Big Blind Tag", 
                "StandardPack" => "Standard Pack",
                "BuffoonPack" => "Buffoon Pack",
                "Shop" => "Shop Purchase",
                "StartingItems" => "Starting Item",
                _ => "Unknown Source"
            };
            
            previewTextBlock.Text = $"{emoji} Searching for items from: {sourceName}";
            
            // Color the preview based on source type
            var color = source switch
            {
                "SmallBlindTag" or "BigBlindTag" => "#FFD700", // Gold for tags
                "StandardPack" or "BuffoonPack" => "#00BFFF",  // Blue for packs
                "Shop" => "#32CD32",                            // Green for shop
                "StartingItems" => "#FF69B4",                   // Pink for starting
                _ => "#CCCCCC"                                  // Gray for any
            };
            
            previewTextBlock.Foreground = Avalonia.Media.Brush.Parse(color);
        }
        
        /// <summary>
        /// Get user-friendly name for the source
        /// </summary>
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
        
        /// <summary>
        /// Get all available source options
        /// </summary>
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
    }
}
