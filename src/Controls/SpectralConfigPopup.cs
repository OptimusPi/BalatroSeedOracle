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
using Oracle.Services;

namespace Oracle.Controls
{
    /// <summary>
    /// Configuration popup specifically for Spectral card items
    /// </summary>
    public partial class SpectralConfigPopup : ItemConfigPopupBase
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
        private HashSet<string> _selectedSources = new() { "shop", "pack" };

        // UI Controls
        private CheckBox[] _anteCheckBoxes = new CheckBox[8];
        private RadioButton? _editionNormal;
        private RadioButton? _editionFoil;
        private RadioButton? _editionHolo;
        private RadioButton? _editionPoly;
        private RadioButton? _editionNegative;
        private CheckBox? _sourceShop;
        private CheckBox? _sourcePack;

        public SpectralConfigPopup()
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

            // Antes section
            mainPanel.Children.Add(CreateAntesSection());

            // Edition section
            mainPanel.Children.Add(CreateEditionSection());

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
                Text = "ANTES TO SEARCH",
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
                        Application.Current?.FindResource("VeryDarkBackground") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#2a2a2a")),
                    BorderBrush =
                        Application.Current?.FindResource("DarkerGrey") as IBrush
                        ?? new SolidColorBrush(Color.Parse("#1a1a1a")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 2),
                    MinWidth = 24,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = new TextBlock
                    {
                        Text = anteNum.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        FontFamily =
                            Application.Current?.FindResource("BalatroFont") as FontFamily
                            ?? FontFamily.Default,
                        FontSize = 12,

                        Foreground =
                            Application.Current?.FindResource("MediumGrey") as IBrush
                            ?? Brushes.Gray,
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
                Text = "EDITION",
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

            _editionNormal = CreateEditionRadioButton("Normal", "normal", true);
            _editionFoil = CreateEditionRadioButton("Foil", "foil", false);
            _editionHolo = CreateEditionRadioButton("Holo", "holographic", false);
            _editionPoly = CreateEditionRadioButton("Poly", "polychrome", false);
            _editionNegative = CreateEditionRadioButton("Negative", "negative", false);

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
                Padding = new Thickness(6, 4),
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            // Try to load edition image
            var image = new Image
            {
                Width = 35,
                Height = 47,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Source = SpriteService.Instance.GetEditionImage(editionKey),
            };

            border.Child = image;
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

            _sourceShop = CreateSourceCheckBox("From Shop (Ghost Deck)", "shop");
            _sourcePack = CreateSourceCheckBox("From Spectral Pack", "pack");

            sourcesPanel.Children.Add(_sourceShop);
            sourcesPanel.Children.Add(_sourcePack);

            Grid.SetRow(sourcesPanel, 1);
            grid.Children.Add(sourcesPanel);

            border.Child = grid;
            return border;
        }

        private CheckBox CreateSourceCheckBox(string displayName, string sourceKey)
        {
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

            checkbox.Click += (s, e) =>
            {
                if (checkbox.IsChecked == true)
                {
                    _selectedSources.Add(sourceKey);
                }
                else
                {
                    _selectedSources.Remove(sourceKey);
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
                return;

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

            // Load edition
            if (!string.IsNullOrEmpty(config.Edition))
            {
                _selectedEdition = config.Edition;
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
                _selectedSources.Clear();
                foreach (var source in config.Sources)
                {
                    _selectedSources.Add(source);
                }

                _sourceShop?.SetCurrentValue(
                    CheckBox.IsCheckedProperty,
                    _selectedSources.Contains("shop")
                );
                _sourcePack?.SetCurrentValue(
                    CheckBox.IsCheckedProperty,
                    _selectedSources.Contains("pack")
                );
            }
        }

        protected override ItemConfig BuildConfiguration()
        {
            var config = new ItemConfig
            {
                ItemKey = ItemKey,
                Antes = GetSelectedAntes(),
                Edition = _selectedEdition,
                Sources = _selectedSources.ToList(),
            };

            return config;
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
