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
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls
{
    public partial class ItemConfigPopup : UserControl
    {
        public event EventHandler<ItemConfigEventArgs>? ConfigApplied;
        public event EventHandler? DeleteRequested;
        public event EventHandler? Cancelled;
        private static readonly bool[] DefaultMustAntes =
        [
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        ];
        private static readonly bool[] DefaultShouldAntes =
        [
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        ];
        private static readonly bool[] DefaultCouldAntes =
        [
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
        ];

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

        public void SetItem(string itemKey, string itemType, string displayName)
        {
            _itemKey = itemKey;

            // Check if this is a joker (editions only apply to jokers)
            _isJoker = itemType == "Joker";

            var nameText = this.FindControl<TextBlock>("ItemNameText");
            if (nameText != null)
                nameText.Text = displayName;
        }
        
        public void LoadConfiguration(ItemConfig config)
        {
            // Load antes if configured
            if (config.Antes != null && config.Antes.Count > 0)
            {
                _selectedAntes = new bool[8];
                foreach (var ante in config.Antes)
                {
                    if (ante >= 1 && ante <= 8)
                        _selectedAntes[ante - 1] = true;
                }
                UpdateAnteCheckboxes();
            }
            
            // Load edition
            if (!string.IsNullOrEmpty(config.Edition))
            {
                switch (config.Edition.ToLower())
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
            
            // Load score
            var scoreBox = this.Find<TextBox>("ScoreBox");
            if (scoreBox != null)
                scoreBox.Text = config.Score.ToString();
            
            // Show/hide edition section based on item type
            var editionBorder = this.FindControl<Border>("EditionSection");
            if (editionBorder != null)
                editionBorder.IsVisible = _isJoker;
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
                Label = GetLabel(),
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

            // Always return the actual selected antes, never null
            // This ensures the user's selection is preserved exactly  
            return antes.Count > 0 ? antes : new List<int>();
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
            if (this.FindControl<RadioButton>("EditionFoil")?.IsChecked == true)
                return "foil";
            if (this.FindControl<RadioButton>("EditionHolo")?.IsChecked == true)
                return "holographic";
            if (this.FindControl<RadioButton>("EditionPoly")?.IsChecked == true)
                return "polychrome";
            if (this.FindControl<RadioButton>("EditionNegative")?.IsChecked == true)
                return "negative";
            return "none";
        }

        private Dictionary<string, List<int>> GetSelectedSources()
        {
            var sources = new Dictionary<string, List<int>>();

            bool hasPacks = this.FindControl<CheckBox>("SourceTags")?.IsChecked == true ||
                           this.FindControl<CheckBox>("SourcePacks")?.IsChecked == true;
            bool hasShop = this.FindControl<CheckBox>("SourceShop")?.IsChecked == true;

            // Add default slots (4 each) when source is selected
            if (hasPacks)
            {
                sources["packSlots"] = new List<int> { 0, 1, 2, 3 };
            }
            else
            {
                sources["packSlots"] = new List<int>();
            }

            if (hasShop)
            {
                sources["shopSlots"] = new List<int> { 0, 1, 2, 3 };
            }
            else
            {
                sources["shopSlots"] = new List<int>();
            }

            // Default to all sources if none selected
            if (!hasPacks && !hasShop)
            {
                sources["packSlots"] = new List<int> { 0, 1, 2, 3 };
                sources["shopSlots"] = new List<int> { 0, 1, 2, 3 };
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
            // Enable editions for jokers and playing cards

            return itemKey.Contains("joker")
                || itemKey.Contains("Joker")
                || itemKey.StartsWith("j_")
                || itemKey.Contains("playingcard")
                || itemKey.Contains("PlayingCard")
                || IsSpecificJoker(itemKey);
        }

        private bool IsSpecificJoker(string itemKey)
        {
            // Add specific joker names that might not follow the pattern
            var jokerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "blueprint",
                "brainstorm",
                "satellite",
                "showman",
                "flower_pot",
                "merry_andy",
                "oops_all_6s",
                "the_idol",
                "seeing_double",
                "matador",
                "hit_the_road",
                "the_duo",
                "the_trio",
                "the_family",
                "the_order",
                "the_tribe",
                "stuntman",
                "invisible_joker",
                "brainstorm",
                "satellite",
                "showman",
                "flower_pot",
                "blueprint",
                "wee_joker",
                "joker",
                "greedy_joker",
                "lusty_joker",
                "wrathful_joker",
                "gluttonous_joker",
                // Add more as needed
            };

            return jokerKeys.Contains(itemKey);
        }

        private void OnAnteClick(object? sender, RoutedEventArgs e)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "ItemConfigPopup",
                $"OnAnteClick called. Sender type: {sender?.GetType().Name}"
            );

            if (sender is CheckBox checkBox && checkBox.Name != null)
            {
                // Extract ante number from checkbox name (e.g., "Ante1" -> 1)
                if (
                    checkBox.Name.StartsWith("Ante")
                    && int.TryParse(checkBox.Name.Substring(4), out int anteNum)
                )
                {
                    if (anteNum >= 1 && anteNum <= 8)
                    {
                        // Don't toggle programmatically - the checkbox already toggled itself
                        // Just read the new state
                        bool newState = checkBox.IsChecked == true;
                        
                        // Update internal state
                        _selectedAntes[anteNum - 1] = newState;

                        BalatroSeedOracle.Helpers.DebugLogger.Log(
                            "ItemConfigPopup",
                            $"Ante {anteNum} toggled to: {newState}"
                        );
                    }
                }
            }
        }

        private void OnEditionClick(object? sender, RoutedEventArgs e)
        {
            BalatroSeedOracle.Helpers.DebugLogger.Log(
                "ItemConfigPopup",
                $"OnEditionClick called. Sender type: {sender?.GetType().Name}"
            );

            // RadioButton Checked event is already handled properly by Avalonia
            // Just log the change for debugging
            if (sender is RadioButton rb)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "ItemConfigPopup",
                    $"Edition RadioButton {rb.Name} checked: {rb.IsChecked}"
                );
            }
        }

        private void OnSourceClick(object? sender, RoutedEventArgs e)
        {
            // Simple handler - checkbox state is already updated by Avalonia
            if (sender is CheckBox checkBox)
            {
                BalatroSeedOracle.Helpers.DebugLogger.Log(
                    "ItemConfigPopup",
                    $"Source {checkBox.Name} set to: {checkBox.IsChecked}"
                );
            }
        }
    }

    public class ItemConfigEventArgs : EventArgs
    {
        public ItemConfig Config { get; set; } = new();
        public ItemConfig Configuration => Config; // Alias for compatibility
    }

    public class ItemConfig
    {
        public string ItemKey { get; set; } = "";
        public string ItemType { get; set; } = ""; // Joker, Tag, Voucher, etc
        public string ItemName { get; set; } = ""; // Display name
        public List<int>? Antes { get; set; }
        public string Edition { get; set; } = "none";
        public string Seal { get; set; } = "None"; // Red, Blue, Gold, Purple
        public string Enhancement { get; set; } = "None"; // Bonus, Mult, Wild, Glass, Steel, Stone, Lucky
        public int Score { get; set; } = 1; // Score for should clauses
        public object? Sources { get; set; }
        public string? Label { get; set; }
        public string? TagType { get; set; } // "smallblindtag" or "bigblindtag" for tag items
        public List<string>? Stickers { get; set; } // "eternal", "perishable", "rental"
        public int? Min { get; set; } // Minimum count required (for Must items)
        public List<int>? ShopSlots { get; set; } // Shop slot positions
        public List<int>? PackSlots { get; set; } // Pack slot positions
        public bool SkipBlindTags { get; set; } // From skip blind tags
        public bool IsMegaArcana { get; set; } // Mega arcana pack only
        public bool IsSoulJoker { get; set; } // For SoulJoker type
        public bool IsMultiValue { get; set; } // For multi-value clauses
        public List<string>? Values { get; set; } // For multi-value clauses
    }
}
