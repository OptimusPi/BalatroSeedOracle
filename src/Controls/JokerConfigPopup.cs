using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Configuration popup specifically for Joker items
    /// </summary>
    public partial class JokerConfigPopup : ItemConfigPopupBase
    {
        private bool[] _selectedAntes = new bool[8]
        {
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            true,
        };
        private string _selectedEdition = "none";
        private Dictionary<string, List<int>> _selectedSources = new()
        {
            { "packSlots", new List<int> { 0, 1, 2, 3 } },
            { "shopSlots", new List<int> { 0, 1, 2, 3 } }
        };

        // UI Controls
        private CheckBox[] _anteCheckBoxes = new CheckBox[8];
        private RadioButton? _editionNormal;
        private RadioButton? _editionFoil;
        private RadioButton? _editionHolo;
        private RadioButton? _editionPoly;
        private RadioButton? _editionNegative;
        private CheckBox? _sourceTag;
        private CheckBox? _sourceBooster;
        private CheckBox? _sourceShop;
        private CheckBox? _stickerEternal;
        private CheckBox? _stickerPerishable;
        private CheckBox? _stickerRental;
        private bool _isLegendaryJoker = false;
    private Border? _stickersSection; // container for sticker controls so we can hide for soul jokers

        public JokerConfigPopup()
        {
            InitializeComponent();
        }
        
        public void SetIsLegendaryJoker(bool isLegendary)
        {
            _isLegendaryJoker = isLegendary;
            
            // If it's a legendary joker, disable shop source and update UI
            if (_isLegendaryJoker && _sourceShop != null)
            {
                _sourceShop.IsEnabled = false;
                ToolTip.SetTip(_sourceShop, "Soul jokers can only be obtained from The Soul spectral card (tags/packs)");
                
                // Uncheck shop if it was checked
                _sourceShop.IsChecked = false;
                _selectedSources["shopSlots"] = new List<int>();
            }

            // Soul (legendary) jokers cannot have stickers – hide the stickers section entirely
            if (_isLegendaryJoker && _stickersSection != null)
            {
                _stickersSection.IsVisible = false;
                // Clear any sticker selections just in case user toggled before marking legendary
                _stickerEternal = null;
                _stickerPerishable = null;
                _stickerRental = null;
            }
        }

        private void InitializeComponent()
        {
            var content = BuildUI();
            Content = CreatePopupContainer(content);
        }

        private StackPanel BuildUI()
        {
            var mainPanel = new StackPanel { Spacing = 8 };

            // Header
            mainPanel.Children.Add(CreateHeader());

            // Antes section
            mainPanel.Children.Add(CreateAntesSection());

            // Edition section
            mainPanel.Children.Add(CreateEditionSection());

            // Stickers section
            mainPanel.Children.Add(CreateStickersSection());

            // Sources section
            mainPanel.Children.Add(CreateSourcesSection());

            // Button bar
            mainPanel.Children.Add(CreateButtonBar());

            return mainPanel;
        }

        private Border CreateAntesSection()
        {
            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("ItemConfigDarkBg") as IBrush
                    ?? Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "Search Antes:",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily
                    ?? FontFamily.Default,
                FontSize = 14,
                FontWeight = FontWeight.Medium,
                Foreground =
                    Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Ante buttons
            var anteGrid = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(-2),
            };

            for (int i = 0; i < 8; i++)
            {
                var anteNum = i + 1;
                var checkbox = new CheckBox { IsChecked = true, Margin = new Thickness(3) };

                var anteBorder = new Border
                {
                    Background =
                        Application.Current?.FindResource("VeryDarkBackground") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                    BorderBrush =
                        Application.Current?.FindResource("DarkerGrey") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(4, 2),
                    MinWidth = 32,
                    MinHeight = 32,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = new TextBlock
                    {
                        Text = anteNum.ToString(),
                        FontFamily =
                            Application.Current?.FindResource("BalatroFont") as FontFamily
                            ?? FontFamily.Default,
                        FontSize = 14,
                        FontWeight = FontWeight.Medium,

                        Foreground =
                            Application.Current?.FindResource("MediumGrey") as IBrush
                            ?? Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                };

                checkbox.Content = anteBorder;

                // Store reference
                _anteCheckBoxes[i] = checkbox;

                // Initialize visual state to match the checkbox
                UpdateAnteVisual(anteBorder, true);

                // Add handler
                int index = i; // Capture for closure
                checkbox.Click += (s, e) =>
                {
                    _selectedAntes[index] = checkbox.IsChecked == true;
                    UpdateAnteVisual(anteBorder, checkbox.IsChecked == true);
                };

                anteGrid.Children.Add(checkbox);
            }

            Grid.SetRow(anteGrid, 1);
            grid.Children.Add(anteGrid);

            border.Child = grid;
            return border;
        }

        private void UpdateAnteVisual(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.Background =
                    Application.Current?.FindResource("GreenAccentVeryDark") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a5f1a"));
                border.BorderBrush =
                    Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                }
            }
            else
            {
                border.Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a"));
                border.BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a"));
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray;
                }
            }
        }

        private Border CreateEditionSection()
        {
            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("ItemConfigDarkBg") as IBrush
                    ?? Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "Require Edition?",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily
                    ?? FontFamily.Default,
                FontSize = 11,
                Foreground =
                    Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Edition buttons
            var editionGrid = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(-2),
            };

            _editionNormal = CreateEditionRadioButton("Normal", "none", true);
            _editionFoil = CreateEditionRadioButton("Foil", "Foil", false);
            _editionHolo = CreateEditionRadioButton("Holo", "Holographic", false);
            _editionPoly = CreateEditionRadioButton("Poly", "Polychrome", false);
            _editionNegative = CreateEditionRadioButton("Negative", "Negative", false);

            editionGrid.Children.Add(_editionNormal);
            editionGrid.Children.Add(_editionFoil);
            editionGrid.Children.Add(_editionHolo);
            editionGrid.Children.Add(_editionPoly);
            editionGrid.Children.Add(_editionNegative);

            Grid.SetRow(editionGrid, 1);
            grid.Children.Add(editionGrid);

            border.Child = grid;
            return border;
        }

        private Border CreateStickersSection()
        {
            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("ItemConfigDarkBg") as IBrush
                    ?? Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "Require Stickers?",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily
                    ?? FontFamily.Default,
                FontSize = 11,
                Foreground =
                    Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Sticker checkboxes
            var stickerPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(-2),
            };

            _stickerEternal = CreateStickerCheckBox("Eternal", "eternal");
            _stickerPerishable = CreateStickerCheckBox("Perishable", "perishable");
            _stickerRental = CreateStickerCheckBox("Rental", "rental");

            stickerPanel.Children.Add(_stickerEternal);
            stickerPanel.Children.Add(_stickerPerishable);
            stickerPanel.Children.Add(_stickerRental);

            Grid.SetRow(stickerPanel, 1);
            grid.Children.Add(stickerPanel);

            border.Child = grid;
            _stickersSection = border;

            // If this popup was already marked as legendary before section creation (edge case), hide now
            if (_isLegendaryJoker)
            {
                border.IsVisible = false;
            }

            return border;
        }

        private CheckBox CreateStickerCheckBox(string displayName, string stickerKey)
        {
            var checkbox = new CheckBox
            {
                IsChecked = false,
                Margin = new Thickness(3),
            };

            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                MinWidth = 80,
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = displayName,
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily
                        ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            };

            checkbox.Content = border;

            // Add click handler to update visual state
            checkbox.Click += (s, e) =>
            {
                UpdateStickerVisual(border, checkbox.IsChecked == true);
            };

            return checkbox;
        }

        private void UpdateStickerVisual(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.Background =
                    Application.Current?.FindResource("GreenAccentVeryDark") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a5f1a"));
                border.BorderBrush =
                    Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                }
            }
            else
            {
                border.Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a"));
                border.BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a"));
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray;
                }
            }
        }

        private RadioButton CreateEditionRadioButton(
            string displayName,
            string editionKey,
            bool isChecked
        )
        {
            var radio = new RadioButton
            {
                GroupName = "ItemEdition",
                IsChecked = isChecked,
                Margin = new Thickness(2),
            };

            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 4),
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            // Try to load edition image
            if (editionKey == "negative")
            {
                // Create a fake negative edition with inverted colors
                var negativePanel = new Panel
                {
                    Width = 35,
                    Height = 47,
                    Background = new SolidColorBrush(Color.Parse("#1a1f2e")), // Dark blue-ish background
                };

                var invertedRect = new Border
                {
                    Width = 29,
                    Height = 41,
                    Background = new SolidColorBrush(Color.Parse("#FFEFEE")), // Light inverted color
                    CornerRadius = new CornerRadius(3),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                negativePanel.Children.Add(invertedRect);
                border.Child = negativePanel;
            }
            else
            {
                var image = new Image
                {
                    Width = 35,
                    Height = 47,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Source = SpriteService.Instance.GetEditionImage(editionKey),
                };
                border.Child = image;
            }
            radio.Content = border;

            // Add handler
            radio.IsCheckedChanged += (s, e) =>
            {
                if (radio.IsChecked == true)
                {
                    _selectedEdition = editionKey;
                    UpdateEditionVisual(border, true);
                }
                else
                {
                    UpdateEditionVisual(border, false);
                }
            };

            return radio;
        }

        private void UpdateEditionVisual(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.Background =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a"));
                border.BorderBrush =
                    Application.Current?.FindResource("Gold") as IBrush ?? Brushes.Gold;
            }
            else
            {
                border.Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a"));
                border.BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a"));
            }
        }

        private Border CreateSourcesSection()
        {
            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("ItemConfigDarkBg") as IBrush
                    ?? Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "SOURCES",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily
                    ?? FontFamily.Default,
                FontSize = 11,
                Foreground =
                    Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Source checkboxes
            var sourcesPanel = new StackPanel { Spacing = 4 };

            _sourceTag = CreateSourceCheckBox("From Tags", "tag");
            _sourceBooster = CreateSourceCheckBox("From Booster Packs", "booster");
            _sourceShop = CreateSourceCheckBox("From Shop", "shop");

            sourcesPanel.Children.Add(_sourceTag);
            sourcesPanel.Children.Add(_sourceBooster);
            sourcesPanel.Children.Add(_sourceShop);

            Grid.SetRow(sourcesPanel, 1);
            grid.Children.Add(sourcesPanel);

            border.Child = grid;
            return border;
        }

        private CheckBox CreateSourceCheckBox(string displayName, string sourceKey)
        {
            // Default to checked for initial state (will be updated by LoadConfiguration if needed)
            var checkbox = new CheckBox { IsChecked = true };

            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = displayName,
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily
                        ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray,
                },
            };

            checkbox.Content = border;
            
            // Set initial visual state (checked by default)
            UpdateSourceVisual(border, true);

            checkbox.Click += (s, e) =>
            {
                if (checkbox.IsChecked == true)
                {
                    // Add default slots when enabling a source
                    if (sourceKey == "tag" || sourceKey == "booster")
                    {
                        _selectedSources["packSlots"] = new List<int> { 0, 1, 2, 3 };
                    }
                    else if (sourceKey == "shop")
                    {
                        _selectedSources["shopSlots"] = new List<int> { 0, 1, 2, 3 };
                    }
                }
                else
                {
                    // Clear slots when disabling a source
                    if (sourceKey == "tag" || sourceKey == "booster")
                    {
                        _selectedSources["packSlots"] = new List<int>();
                    }
                    else if (sourceKey == "shop")
                    {
                        _selectedSources["shopSlots"] = new List<int>();
                    }
                }
                UpdateSourceVisual(border, checkbox.IsChecked == true);
            };

            return checkbox;
        }

        private void UpdateSourceVisual(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.Background =
                    Application.Current?.FindResource("GreenAccentVeryDark") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a5f1a"));
                border.BorderBrush =
                    Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green;
                }
            }
            else
            {
                border.Background =
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#2a2a2a"));
                border.BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a1a1a"));
                if (border.Child is TextBlock text)
                {
                    text.Foreground =
                        Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray;
                }
            }
        }

        protected override void LoadConfiguration(ItemConfig? config)
        {
            if (config == null)
            {
                return;
            }

            // Load stickers
            if (config.Stickers != null && config.Stickers.Count > 0)
            {
                if (_stickerEternal != null)
                {
                    _stickerEternal.IsChecked = config.Stickers.Contains("eternal");
                    if (_stickerEternal.Content is Border eternalBorder)
                        UpdateStickerVisual(eternalBorder, _stickerEternal.IsChecked == true);
                }
                if (_stickerPerishable != null)
                {
                    _stickerPerishable.IsChecked = config.Stickers.Contains("perishable");
                    if (_stickerPerishable.Content is Border perishableBorder)
                        UpdateStickerVisual(perishableBorder, _stickerPerishable.IsChecked == true);
                }
                if (_stickerRental != null)
                {
                    _stickerRental.IsChecked = config.Stickers.Contains("rental");
                    if (_stickerRental.Content is Border rentalBorder)
                        UpdateStickerVisual(rentalBorder, _stickerRental.IsChecked == true);
                }
            }

            // Load antes
            if (config.Antes != null && config.Antes.Count > 0)
            {
                // Clear all first
                for (int i = 0; i < 8; i++)
                {
                    _selectedAntes[i] = false;
                    _anteCheckBoxes[i].IsChecked = false;
                    
                    // Update visual for the ante button
                    if (_anteCheckBoxes[i].Content is Border border)
                    {
                        UpdateAnteVisual(border, false);
                    }
                }

                // Set selected antes
                foreach (var ante in config.Antes)
                {
                    if (ante >= 1 && ante <= 8)
                    {
                        _selectedAntes[ante - 1] = true;
                        _anteCheckBoxes[ante - 1].IsChecked = true;
                        
                        // Update visual for the ante button
                        if (_anteCheckBoxes[ante - 1].Content is Border border)
                        {
                            UpdateAnteVisual(border, true);
                        }
                    }
                }
            }

            // Load edition
            if (!string.IsNullOrEmpty(config.Edition))
            {
                // Capitalize first letter to match expected format
                var editionKey = char.ToUpper(config.Edition[0]) + config.Edition.Substring(1).ToLower();
                _selectedEdition = editionKey;
                
                switch (config.Edition.ToLower())
                {
                    case "foil":
                        _editionFoil?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
                        break;
                    case "holographic":
                        _editionHolo?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
                        break;
                    case "polychrome":
                        _editionPoly?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
                        break;
                    case "negative":
                        _editionNegative?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
                        break;
                    default:
                        _editionNormal?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
                        break;
                }
            }

            // Load sources
            if (config.Sources != null)
            {
                // Handle both old format (array of strings) and new format (object with slots)
                if (config.Sources is List<object> sourcesList)
                {
                    // Old format - convert to new format
                    _selectedSources["packSlots"] = sourcesList.Contains("tag") || sourcesList.Contains("booster") 
                        ? new List<int> { 0, 1, 2, 3 } 
                        : new List<int>();
                    _selectedSources["shopSlots"] = sourcesList.Contains("shop") 
                        ? new List<int> { 0, 1, 2, 3 } 
                        : new List<int>();
                        
                    _sourceTag?.SetCurrentValue(CheckBox.IsCheckedProperty, sourcesList.Contains("tag"));
                    _sourceBooster?.SetCurrentValue(CheckBox.IsCheckedProperty, sourcesList.Contains("booster"));
                    // Don't set shop for legendary jokers
                    if (!_isLegendaryJoker)
                    {
                        _sourceShop?.SetCurrentValue(CheckBox.IsCheckedProperty, sourcesList.Contains("shop"));
                    }
                }
                else if (config.Sources is Dictionary<string, object> sourcesDict)
                {
                    // New format - load slots
                    if (sourcesDict.ContainsKey("packSlots") && sourcesDict["packSlots"] is List<int> packSlots)
                    {
                        _selectedSources["packSlots"] = new List<int>(packSlots);
                    }
                    if (sourcesDict.ContainsKey("shopSlots") && sourcesDict["shopSlots"] is List<int> shopSlots)
                    {
                        _selectedSources["shopSlots"] = new List<int>(shopSlots);
                    }
                    
                    // Set checkboxes based on whether slots exist
                    bool hasPackSlots = _selectedSources["packSlots"].Count > 0;
                    bool hasShopSlots = _selectedSources["shopSlots"].Count > 0;
                    
                    _sourceTag?.SetCurrentValue(CheckBox.IsCheckedProperty, hasPackSlots);
                    _sourceBooster?.SetCurrentValue(CheckBox.IsCheckedProperty, hasPackSlots);
                    // Don't set shop for legendary jokers
                    if (!_isLegendaryJoker)
                    {
                        _sourceShop?.SetCurrentValue(CheckBox.IsCheckedProperty, hasShopSlots);
                    }
                }
                
                // Update visuals for source checkboxes
                if (_sourceTag?.Content is Border tagBorder)
                {
                    UpdateSourceVisual(tagBorder, _sourceTag.IsChecked == true);
                }
                if (_sourceBooster?.Content is Border boosterBorder)
                {
                    UpdateSourceVisual(boosterBorder, _sourceBooster.IsChecked == true);
                }
                if (_sourceShop?.Content is Border shopBorder)
                {
                    // For legendary jokers, always show shop as disabled
                    UpdateSourceVisual(shopBorder, _isLegendaryJoker ? false : (_sourceShop.IsChecked == true));
                }
            }
            else
            {
                // No sources specified in config - this means it's a new item
                // Default to having all sources enabled (as they are checked by default)
                // The visuals should already be correct from initialization
            }
        }

        protected override ItemConfig BuildConfiguration()
        {
            var config = new ItemConfig
            {
                ItemKey = ItemKey,
                Antes = GetSelectedAntes(),
                Edition = _selectedEdition,
                Sources = _selectedSources,
                Stickers = GetSelectedStickers(),
            };

            return config;
        }
        
        private List<string>? GetSelectedStickers()
        {
            var stickers = new List<string>();
            
            if (_stickerEternal?.IsChecked == true)
                stickers.Add("eternal");
            if (_stickerPerishable?.IsChecked == true)
                stickers.Add("perishable");
            if (_stickerRental?.IsChecked == true)
                stickers.Add("rental");
            
            return stickers.Count > 0 ? stickers : null;
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
    }
}
