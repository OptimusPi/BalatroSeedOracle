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
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Configuration popup specifically for Playing Card items
    /// </summary>
    public partial class PlayingCardConfigPopup : ItemConfigPopupBase
    {
        private bool[] _selectedAntes = new bool[8] { true, true, true, true, true, true, true, true };
        
        // UI Controls
        private CheckBox[] _anteCheckBoxes = new CheckBox[8];
        private RadioButton? _noneRadio;
        private RadioButton? _foilRadio;
        private RadioButton? _holographicRadio;
        private RadioButton? _polychromeRadio;
        private RadioButton? _negativeRadio;
        
        // Seal controls
        private RadioButton? _noSealRadio;
        private RadioButton? _redSealRadio;
        private RadioButton? _blueSealRadio;
        private RadioButton? _goldSealRadio;
        private RadioButton? _purpleSealRadio;
        
        // Enhancement controls  
        private RadioButton? _noEnhancementRadio;
        private RadioButton? _bonusRadio;
        private RadioButton? _multRadio;
        private RadioButton? _wildRadio;
        private RadioButton? _glassRadio;
        private RadioButton? _steelRadio;
        private RadioButton? _stoneRadio;
        private RadioButton? _luckyRadio;

        public PlayingCardConfigPopup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            var content = BuildUI();
            Content = CreatePopupContainer(content);
        }

        private StackPanel BuildUI()
        {
            var mainPanel = new StackPanel { Spacing = 10 };

            // Header
            mainPanel.Children.Add(CreateHeader());

            // Editions section (playing cards can have editions)
            mainPanel.Children.Add(CreateEditionsSection());
            
            // Seals section (playing cards can have seals)
            mainPanel.Children.Add(CreateSealsSection());
            
            // Enhancements section (playing cards can have enhancements)
            mainPanel.Children.Add(CreateEnhancementsSection());

            // Antes section
            mainPanel.Children.Add(CreateAntesSection());

            // Button bar
            mainPanel.Children.Add(CreateButtonBar());

            return mainPanel;
        }

        private Border CreateEditionsSection()
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
                Text = "EDITION",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                FontSize = 11,
                Foreground =
                    Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Edition radio buttons
            var editionsPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(-2),
            };

            _noneRadio = CreateEditionRadio("None", "none", true);
            _foilRadio = CreateEditionRadio("Foil", "foil", false);
            _holographicRadio = CreateEditionRadio("Holo", "holographic", false);
            _polychromeRadio = CreateEditionRadio("Poly", "polychrome", false);
            _negativeRadio = CreateEditionRadio("Neg", "negative", false);

            editionsPanel.Children.Add(_noneRadio);
            editionsPanel.Children.Add(_foilRadio);
            editionsPanel.Children.Add(_holographicRadio);
            editionsPanel.Children.Add(_polychromeRadio);
            editionsPanel.Children.Add(_negativeRadio);

            Grid.SetRow(editionsPanel, 1);
            grid.Children.Add(editionsPanel);

            border.Child = grid;
            return border;
        }

        private RadioButton CreateEditionRadio(string displayName, string value, bool isChecked)
        {
            var radio = new RadioButton
            {
                GroupName = "Edition",
                IsChecked = isChecked,
                Margin = new Thickness(2),
            };

            var border = new Border
            {
                Background =
                    isChecked
                        ? Application.Current?.FindResource("GreenAccentVeryDark") as IBrush ??
                          new SolidColorBrush(Color.Parse("#1a5f1a"))
                        : Application.Current?.FindResource("VeryDarkBackground") as IBrush ??
                          new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderBrush =
                    isChecked
                        ? Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green
                        : Application.Current?.FindResource("DarkerGrey") as IBrush ??
                          new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                MinWidth = 45,
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = displayName,
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        isChecked
                            ? Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green
                            : Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            };

            radio.Content = border;

            // Add click handler to update visual state
            radio.Click += (s, e) =>
            {
                UpdateRadioVisuals();
            };

            return radio;
        }

        private void UpdateRadioVisuals()
        {
            UpdateEditionVisual(_noneRadio, _noneRadio?.IsChecked == true);
            UpdateEditionVisual(_foilRadio, _foilRadio?.IsChecked == true);
            UpdateEditionVisual(_holographicRadio, _holographicRadio?.IsChecked == true);
            UpdateEditionVisual(_polychromeRadio, _polychromeRadio?.IsChecked == true);
            UpdateEditionVisual(_negativeRadio, _negativeRadio?.IsChecked == true);
        }

        private void UpdateEditionVisual(RadioButton? radio, bool isSelected)
        {
            if (radio?.Content is Border border)
            {
                if (isSelected)
                {
                    border.Background =
                        Application.Current?.FindResource("GreenAccentVeryDark") as IBrush ??
                        new SolidColorBrush(Color.Parse("#1a5f1a"));
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
                        Application.Current?.FindResource("VeryDarkBackground") as IBrush ??
                        new SolidColorBrush(Color.Parse("#2a2a2a"));
                    border.BorderBrush =
                        Application.Current?.FindResource("DarkerGrey") as IBrush ??
                        new SolidColorBrush(Color.Parse("#1a1a1a"));
                    if (border.Child is TextBlock text)
                    {
                        text.Foreground =
                            Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray;
                    }
                }
            }
        }

        private Border CreateSealsSection()
        {
            var border = new Border
            {
                Background = Application.Current?.FindResource("ItemConfigDarkBg") as IBrush ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "SEAL",
                FontFamily = Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                FontSize = 11,
                Foreground = Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Seal radio buttons
            var sealsPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(-2) };

            _noSealRadio = CreateEditionRadio("None", "none", true);
            _redSealRadio = CreateEditionRadio("Red", "red", false);
            _blueSealRadio = CreateEditionRadio("Blue", "blue", false);
            _goldSealRadio = CreateEditionRadio("Gold", "gold", false);
            _purpleSealRadio = CreateEditionRadio("Purple", "purple", false);

            sealsPanel.Children.Add(_noSealRadio);
            sealsPanel.Children.Add(_redSealRadio);
            sealsPanel.Children.Add(_blueSealRadio);
            sealsPanel.Children.Add(_goldSealRadio);
            sealsPanel.Children.Add(_purpleSealRadio);

            Grid.SetRow(sealsPanel, 1);
            grid.Children.Add(sealsPanel);

            border.Child = grid;
            return border;
        }

        private Border CreateEnhancementsSection()
        {
            var border = new Border
            {
                Background = Application.Current?.FindResource("ItemConfigDarkBg") as IBrush ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };

            // Header
            var header = new TextBlock
            {
                Text = "ENHANCEMENT",
                FontFamily = Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                FontSize = 11,
                Foreground = Application.Current?.FindResource("LightGrey") as IBrush ?? Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 6),
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Enhancement radio buttons
            var enhancementsPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(-2) };

            _noEnhancementRadio = CreateEditionRadio("None", "none", true);
            _bonusRadio = CreateEditionRadio("Bonus", "bonus", false);
            _multRadio = CreateEditionRadio("Mult", "mult", false);
            _wildRadio = CreateEditionRadio("Wild", "wild", false);
            _glassRadio = CreateEditionRadio("Glass", "glass", false);
            _steelRadio = CreateEditionRadio("Steel", "steel", false);
            _stoneRadio = CreateEditionRadio("Stone", "stone", false);
            _luckyRadio = CreateEditionRadio("Lucky", "lucky", false);

            enhancementsPanel.Children.Add(_noEnhancementRadio);
            enhancementsPanel.Children.Add(_bonusRadio);
            enhancementsPanel.Children.Add(_multRadio);
            enhancementsPanel.Children.Add(_wildRadio);
            enhancementsPanel.Children.Add(_glassRadio);
            enhancementsPanel.Children.Add(_steelRadio);
            enhancementsPanel.Children.Add(_stoneRadio);
            enhancementsPanel.Children.Add(_luckyRadio);

            Grid.SetRow(enhancementsPanel, 1);
            grid.Children.Add(enhancementsPanel);

            border.Child = grid;
            return border;
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
                Text = "ANTES TO SEARCH",
                FontFamily =
                    Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                FontSize = 11,
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
                var checkbox = new CheckBox { IsChecked = true, Margin = new Thickness(2) };

                var anteBorder = new Border
                {
                    Background =
                        Application.Current?.FindResource("VeryDarkBackground") as IBrush ??
                        new SolidColorBrush(Color.Parse("#2a2a2a")),
                    BorderBrush =
                        Application.Current?.FindResource("DarkerGrey") as IBrush ??
                        new SolidColorBrush(Color.Parse("#1a1a1a")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 2),
                    MinWidth = 24,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = new TextBlock
                    {
                        Text = anteNum.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        FontFamily =
                            Application.Current?.FindResource("BalatroFont") as FontFamily ?? FontFamily.Default,
                        FontSize = 12,
                        Foreground =
                            Application.Current?.FindResource("MediumGrey") as IBrush ?? Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                };

                checkbox.Content = anteBorder;

                // Store reference
                _anteCheckBoxes[i] = checkbox;

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
                    Application.Current?.FindResource("GreenAccentVeryDark") as IBrush ??
                    new SolidColorBrush(Color.Parse("#1a5f1a"));
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
                    Application.Current?.FindResource("VeryDarkBackground") as IBrush ??
                    new SolidColorBrush(Color.Parse("#2a2a2a"));
                border.BorderBrush =
                    Application.Current?.FindResource("DarkerGrey") as IBrush ??
                    new SolidColorBrush(Color.Parse("#1a1a1a"));
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
                return;

            // Load edition
            if (!string.IsNullOrEmpty(config.Edition))
            {
                switch (config.Edition.ToLower())
                {
                    case "foil":
                        _foilRadio!.IsChecked = true;
                        break;
                    case "holographic":
                        _holographicRadio!.IsChecked = true;
                        break;
                    case "polychrome":
                        _polychromeRadio!.IsChecked = true;
                        break;
                    case "negative":
                        _negativeRadio!.IsChecked = true;
                        break;
                    default:
                        _noneRadio!.IsChecked = true;
                        break;
                }
                UpdateRadioVisuals();
            }

            // Load antes
            if (config.Antes != null && config.Antes.Count > 0)
            {
                // Clear all first
                for (int i = 0; i < 8; i++)
                {
                    _selectedAntes[i] = false;
                    _anteCheckBoxes[i].IsChecked = false;
                }

                // Set selected antes
                foreach (var ante in config.Antes)
                {
                    if (ante >= 1 && ante <= 8)
                    {
                        _selectedAntes[ante - 1] = true;
                        _anteCheckBoxes[ante - 1].IsChecked = true;
                    }
                }
            }
        }

        protected override ItemConfig BuildConfiguration()
        {
            var config = new ItemConfig
            {
                ItemKey = ItemKey,
                Antes = GetSelectedAntes(),
                Edition = GetSelectedEdition(),
                Seal = GetSelectedSeal(),
                Enhancement = GetSelectedEnhancement(),
            };

            return config;
        }

        // SetCard method removed - ConfiguredPlayingCard class deleted

        private string GetSelectedEdition()
        {
            if (_foilRadio?.IsChecked == true) return "foil";
            if (_holographicRadio?.IsChecked == true) return "holographic";
            if (_polychromeRadio?.IsChecked == true) return "polychrome";
            if (_negativeRadio?.IsChecked == true) return "negative";
            return "none";
        }

        private string GetSelectedSeal()
        {
            if (_redSealRadio?.IsChecked == true) return "Red";
            if (_blueSealRadio?.IsChecked == true) return "Blue"; 
            if (_goldSealRadio?.IsChecked == true) return "Gold";
            if (_purpleSealRadio?.IsChecked == true) return "Purple";
            return "None";
        }

        private string GetSelectedEnhancement()
        {
            if (_bonusRadio?.IsChecked == true) return "Bonus";
            if (_multRadio?.IsChecked == true) return "Mult";
            if (_wildRadio?.IsChecked == true) return "Wild";
            if (_glassRadio?.IsChecked == true) return "Glass";
            if (_steelRadio?.IsChecked == true) return "Steel";
            if (_stoneRadio?.IsChecked == true) return "Stone";
            if (_luckyRadio?.IsChecked == true) return "Lucky";
            return "None";
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