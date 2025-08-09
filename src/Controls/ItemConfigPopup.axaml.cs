using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Oracle.Services;

namespace Oracle.Controls
{
    public partial class ItemConfigPopup : UserControl
    {
        public event EventHandler<ItemConfigEventArgs>? ConfigApplied;
        public event EventHandler? DeleteRequested;
        public event EventHandler? Cancelled;
        private static readonly bool[] DefaultMustAntes = [true, false, false, false, false, false, false, false];
        private static readonly bool[] DefaultShouldAntes = [true, false, false, false, false, false, false, false];
        private static readonly bool[] DefaultCouldAntes = [true, false, false, false, false, false, false, false];

        private string _itemKey = "";
        private bool[] _selectedAntes = DefaultMustAntes.ToArray();
        private bool _isJoker = false;

        public ItemConfigPopup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Initialize after loading
            Dispatcher.UIThread.Post(() =>
            {
                LoadEditionImages();
            });
        }

        private void LoadEditionImages()
        {
            var spriteService = SpriteService.Instance;

            // Load Normal edition image
            var normalImage = this.Find<Image>("EditionNormalImage");
            if (normalImage != null)
                normalImage.Source = spriteService.GetEditionImage("normal");

            // Load Foil edition image
            var foilImage = this.Find<Image>("EditionFoilImage");
            if (foilImage != null)
            {
                foilImage.Source = spriteService.GetEditionImage("foil");
            }

            // Load Holographic edition image
            var holoImage = this.Find<Image>("EditionHoloImage");
            if (holoImage != null)
            {
                holoImage.Source = spriteService.GetEditionImage("holographic");
            }

            // Load Polychrome edition image
            var polyImage = this.Find<Image>("EditionPolyImage");
            if (polyImage != null)
            {
                polyImage.Source = spriteService.GetEditionImage("polychrome");
            }

            // Load Negative edition image
            var negativeImage = this.Find<Image>("EditionNegativeImage");
            if (negativeImage != null)
            {
                negativeImage.Source = spriteService.GetEditionImage("negative");
            }
        }

        public void SetItem(string itemKey, string itemName, ItemConfig? existingConfig = null)
        {
            _itemKey = itemKey;

            // Check if this is a joker (editions only apply to jokers)
            _isJoker = IsJokerItem(itemKey);

            var nameText = this.FindControl<TextBlock>("ItemNameText");
            if (nameText != null)
                nameText.Text = itemName;

            // Show/hide edition section based on item type
            var editionBorder = this.FindControl<Border>("EditionSection");
            if (editionBorder != null)
                editionBorder.IsVisible = _isJoker;

            if (existingConfig != null)
            {
                // Load existing ante configuration
                if (existingConfig.Antes != null && existingConfig.Antes.Count > 0)
                {
                    // Clear all antes first
                    for (int i = 0; i < 8; i++)
                    {
                        _selectedAntes[i] = false;
                    }

                    // Set selected antes
                    foreach (var ante in existingConfig.Antes)
                    {
                        if (ante >= 1 && ante <= 8)
                        {
                            _selectedAntes[ante - 1] = true;
                        }
                    }

                    UpdateAnteCheckboxes();
                }
                else
                {
                    // Default to all antes selected
                    for (int i = 0; i < 8; i++)
                    {
                        _selectedAntes[i] = true;
                    }
                    UpdateAnteCheckboxes();
                }

                // Set edition
                if (!string.IsNullOrEmpty(existingConfig.Edition))
                {
                    switch (existingConfig.Edition.ToLower())
                    {
                        case "foil":
                            SetRadioButton("EditionFoil");
                            break;
                        case "holographic":
                            SetRadioButton("EditionHolo");
                            break;
                        case "polychrome":
                            SetRadioButton("EditionPoly");
                            break;
                        case "negative":
                            SetRadioButton("EditionNegative");
                            break;
                        default:
                            SetRadioButton("EditionNormal");
                            break;
                    }
                }

                // Set sources
                if (existingConfig.Sources != null)
                {
                    SetCheckBox("SourceTags", existingConfig.Sources.Contains("tag"));
                    SetCheckBox("SourcePacks", existingConfig.Sources.Contains("booster"));
                    SetCheckBox("SourceShop", existingConfig.Sources.Contains("shop"));
                }
                
                // Set label
                if (!string.IsNullOrEmpty(existingConfig.Label))
                {
                    var labelTextBox = this.FindControl<TextBox>("LabelTextBox");
                    if (labelTextBox != null)
                        labelTextBox.Text = existingConfig.Label;
                }
            }
        }

        private void SetRadioButton(string name)
        {
            var radio = this.FindControl<RadioButton>(name);
            if (radio != null)
                radio.IsChecked = true;
        }

        private void SetCheckBox(string name, bool isChecked)
        {
            var check = this.FindControl<CheckBox>(name);
            if (check != null)
                check.IsChecked = isChecked;
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var config = new ItemConfig
            {
                ItemKey = _itemKey,
                Antes = GetSelectedAntes(),
                Edition = GetSelectedEdition(),
                Sources = GetSelectedSources(),
                Label = GetLabel()
            };

            ConfigApplied?.Invoke(this, new ItemConfigEventArgs { Config = config });
        }

        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private List<int>? GetSelectedAntes()
        {
            var antes = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                if (_selectedAntes[i])
                {
                    antes.Add(i + 1);
                }
            }

            // If all antes are selected, return null (means "any ante")
            if (antes.Count == 8)
            {
                return null;
            }

            return antes.Count > 0 ? antes : null;
        }

        public string GetItem()
        {
            return _itemKey;
        }

        private void UpdateAnteCheckboxes()
        {
            for (int i = 0; i < 8; i++)
            {
                var checkbox = this.FindControl<CheckBox>($"Ante{i + 1}");
                if (checkbox != null)
                {
                    checkbox.IsChecked = _selectedAntes[i];
                }
            }
        }

        private string GetSelectedEdition()
        {
            if (this.FindControl<RadioButton>("EditionFoil")?.IsChecked == true) return "foil";
            if (this.FindControl<RadioButton>("EditionHolo")?.IsChecked == true) return "holographic";
            if (this.FindControl<RadioButton>("EditionPoly")?.IsChecked == true) return "polychrome";
            if (this.FindControl<RadioButton>("EditionNegative")?.IsChecked == true) return "negative";
            return "none";
        }

        private List<string> GetSelectedSources()
        {
            var sources = new List<string>();

            if (this.FindControl<CheckBox>("SourceTags")?.IsChecked == true) sources.Add("tag");
            if (this.FindControl<CheckBox>("SourcePacks")?.IsChecked == true) sources.Add("booster");
            if (this.FindControl<CheckBox>("SourceShop")?.IsChecked == true) sources.Add("shop");

            // Default to main sources if none selected
            if (sources.Count == 0)
            {
                sources.AddRange(new[] { "tag", "booster", "shop" });
            }

            return sources;
        }
        
        private string? GetLabel()
        {
            var labelTextBox = this.FindControl<TextBox>("LabelTextBox");
            if (labelTextBox != null && !string.IsNullOrWhiteSpace(labelTextBox.Text))
            {
                return labelTextBox.Text.Trim();
            }
            return null;
        }

        private bool IsJokerItem(string itemKey)
        {
            // In Balatro, editions can apply to:
            // - Jokers
            // - Playing cards
            // - Vouchers (in some cases)
            // For now, we'll enable editions for jokers only
            // TODO: Extend to support playing cards when that feature is added
            
            return itemKey.Contains("joker") ||
                   itemKey.Contains("Joker") ||
                   itemKey.StartsWith("j_") || // Common joker prefix pattern
                   IsSpecificJoker(itemKey);
        }

        private bool IsSpecificJoker(string itemKey)
        {
            // Add specific joker names that might not follow the pattern
            var jokerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "blueprint", "brainstorm", "satellite", "showman", "flower_pot",
                "merry_andy", "oops_all_6s", "the_idol", "seeing_double",
                "matador", "hit_the_road", "the_duo", "the_trio", "the_family",
                "the_order", "the_tribe", "stuntman", "invisible_joker",
                "brainstorm", "satellite", "showman", "flower_pot",
                "blueprint", "wee_joker", "joker", "greedy_joker",
                "lusty_joker", "wrathful_joker", "gluttonous_joker"
                // Add more as needed
            };

            return jokerKeys.Contains(itemKey);
        }

        private void OnAnteClick(object? sender, RoutedEventArgs e)
        {
            Oracle.Helpers.DebugLogger.Log("ItemConfigPopup", $"OnAnteClick called. Sender type: {sender?.GetType().Name}");
            
            if (sender is CheckBox checkBox && checkBox.Name != null)
            {
                // Extract ante number from checkbox name (e.g., "Ante1" -> 1)
                if (checkBox.Name.StartsWith("Ante") && int.TryParse(checkBox.Name.Substring(4), out int anteNum))
                {
                    if (anteNum >= 1 && anteNum <= 8)
                    {
                        // Update internal state based on the checkbox's current checked state
                        _selectedAntes[anteNum - 1] = checkBox.IsChecked == true;
                        
                        Oracle.Helpers.DebugLogger.Log("ItemConfigPopup", $"Ante {anteNum} set to: {checkBox.IsChecked}");
                    }
                }
            }
        }

        private void OnEditionClick(object? sender, RoutedEventArgs e)
        {
            Oracle.Helpers.DebugLogger.Log("ItemConfigPopup", $"OnEditionClick called. Sender type: {sender?.GetType().Name}");
            
            // RadioButton Checked event is already handled properly by Avalonia
            // Just log the change for debugging
            if (sender is RadioButton rb)
            {
                Oracle.Helpers.DebugLogger.Log("ItemConfigPopup", $"Edition RadioButton {rb.Name} checked: {rb.IsChecked}");
            }
        }

        private void OnSourceClick(object? sender, RoutedEventArgs e)
        {
            // Simple handler - checkbox state is already updated by Avalonia
            if (sender is CheckBox checkBox)
            {
                Oracle.Helpers.DebugLogger.Log("ItemConfigPopup", $"Source {checkBox.Name} set to: {checkBox.IsChecked}");
            }
        }
    }

    public class ItemConfigEventArgs : EventArgs
    {
        public ItemConfig Config { get; set; } = new();
    }

    public class ItemConfig
    {
        public string ItemKey { get; set; } = "";
        public List<int>? Antes { get; set; }
        public string Edition { get; set; } = "none";
        public List<string> Sources { get; set; } = new();
        public string? Label { get; set; }
    }
}