using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace BalatroSeedOracle.Controls
{
    public partial class EditionSelector : UserControl
    {
        private readonly Dictionary<string, EditionInfo> _editionInfo = new()
        {
            { "", new EditionInfo("Any Edition", "⚪", "Card can have any edition type", "#CCCCCC") },
            { "Normal", new EditionInfo("Normal", "⚪", "No special effect - base card", "#FFFFFF") },
            { "Foil", new EditionInfo("Foil", "✨", "+50 chips when scored", "#C0C0C0") },
            { "Holographic", new EditionInfo("Holographic", "🌈", "+10 mult when scored", "#FF69B4") },
            { "Polychrome", new EditionInfo("Polychrome", "🎭", "x1.5 mult when scored", "#FF4500") },
            { "Negative", new EditionInfo("Negative", "🖤", "+1 joker slot (permanent)", "#8B008B") }
        };
        
        public event EventHandler<string>? EditionChanged;
        
        public EditionSelector()
        {
            InitializeComponent();
        }
        
        public string GetSelectedEdition()
        {
            var comboBox = this.FindControl<ComboBox>("EditionComboBox")!;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            return selectedItem?.Tag?.ToString() ?? "";
        }
        
        public void SetSelectedEdition(string edition)
        {
            var comboBox = this.FindControl<ComboBox>("EditionComboBox")!;
            
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ComboBoxItem item && 
                    item.Tag?.ToString() == edition)
                {
                    comboBox.SelectedIndex = i;
                    UpdateDisplay(edition);
                    break;
                }
            }
        }
        
        private void EditionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem)
            {
                var editionTag = selectedItem.Tag?.ToString() ?? "";
                UpdateDisplay(editionTag);
                EditionChanged?.Invoke(this, editionTag);
            }
        }
        
        private void UpdateDisplay(string edition)
        {
            var descriptionTextBlock = this.FindControl<TextBlock>("DescriptionTextBlock")!;
            var previewBorder = this.FindControl<Border>("PreviewBorder")!;
            var previewEmojiBlock = this.FindControl<TextBlock>("PreviewEmojiBlock")!;
            var previewNameBlock = this.FindControl<TextBlock>("PreviewNameBlock")!;
            var previewEffectBlock = this.FindControl<TextBlock>("PreviewEffectBlock")!;
            
            if (_editionInfo.TryGetValue(edition, out var info))
            {
                // Update description
                descriptionTextBlock.Text = info.Description;
                
                // Show/hide preview
                if (string.IsNullOrEmpty(edition))
                {
                    previewBorder.IsVisible = false;
                }
                else
                {
                    previewBorder.IsVisible = true;
                    previewEmojiBlock.Text = info.Emoji;
                    previewNameBlock.Text = info.Name;
                    previewEffectBlock.Text = info.Effect;
                    
                    // Set colors based on edition
                    var color = Brush.Parse(info.Color);
                    previewNameBlock.Foreground = color;
                    previewEmojiBlock.Foreground = color;
                    
                    // Special effects for different editions
                    ApplyEditionEffects(edition, previewBorder);
                }
            }
            else
            {
                descriptionTextBlock.Text = "Unknown edition";
                previewBorder.IsVisible = false;
            }
        }
        
        private void ApplyEditionEffects(string edition, Border previewBorder)
        {
            // Reset border first
            previewBorder.BorderThickness = new Avalonia.Thickness(1);
            previewBorder.BorderBrush = Brush.Parse("#444444");
            
            switch (edition)
            {
                case "Foil":
                    // Shiny silver effect
                    previewBorder.BorderBrush = Brush.Parse("#C0C0C0");
                    previewBorder.BorderThickness = new Avalonia.Thickness(2);
                    break;
                    
                case "Holographic":
                    // Rainbow effect (simulated with gradient)
                    previewBorder.BorderBrush = Brush.Parse("#FF69B4");
                    previewBorder.BorderThickness = new Avalonia.Thickness(3);
                    break;
                    
                case "Polychrome":
                    // Multi-color effect
                    previewBorder.BorderBrush = Brush.Parse("#FF4500");
                    previewBorder.BorderThickness = new Avalonia.Thickness(3);
                    break;
                    
                case "Negative":
                    // Dark/void effect
                    previewBorder.BorderBrush = Brush.Parse("#8B008B");
                    previewBorder.BorderThickness = new Avalonia.Thickness(2);
                    previewBorder.Background = Brush.Parse("#22000022");
                    break;
                    
                default:
                    // Normal appearance
                    break;
            }
        }
        
        /// <summary>
        /// Get user-friendly name for the edition
        /// </summary>
        public static string GetEditionDisplayName(string edition)
        {
            return edition switch
            {
                "" => "Any Edition",
                "Normal" => "Normal",
                "Foil" => "Foil (+50 chips)",
                "Holographic" => "Holographic (+10 mult)",
                "Polychrome" => "Polychrome (x1.5 mult)",
                "Negative" => "Negative (+1 joker slot)",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get all available edition options
        /// </summary>
        public static string[] GetAllEditions()
        {
            return new[]
            {
                "",
                "Normal",
                "Foil",
                "Holographic",
                "Polychrome",
                "Negative"
            };
        }
        
        /// <summary>
        /// Get the power level of an edition (for sorting/comparison)
        /// </summary>
        public static int GetEditionPowerLevel(string edition)
        {
            return edition switch
            {
                "Normal" => 1,
                "Foil" => 2,
                "Holographic" => 3,
                "Polychrome" => 4,
                "Negative" => 5,
                _ => 0
            };
        }
    }
    
    internal class EditionInfo
    {
        public string Name { get; }
        public string Emoji { get; }
        public string Description { get; }
        public string Effect { get; }
        public string Color { get; }
        
        public EditionInfo(string name, string emoji, string description, string color)
        {
            Name = name;
            Emoji = emoji;
            Description = description;
            Effect = description; // For now, same as description
            Color = color;
        }
    }
}
