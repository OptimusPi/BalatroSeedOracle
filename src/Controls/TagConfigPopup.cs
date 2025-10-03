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
    /// Configuration popup specifically for Tag items
    /// </summary>
    public partial class TagConfigPopup : ItemConfigPopupBase
    {
        private bool[] _selectedAntes = new bool[9]
        {
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            true,
        };
        private HashSet<string> _selectedSources = new() { "skip" };
        private string _tagType = "smallblindtag"; // Default to small blind

        // UI Controls
        private CheckBox[] _anteCheckBoxes = new CheckBox[9];
        private CheckBox? _sourceSkip;
        private RadioButton? _smallBlindRadio;
        private RadioButton? _bigBlindRadio;

        public TagConfigPopup()
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

            // Tag Type section (Small Blind vs Big Blind)
            mainPanel.Children.Add(CreateTagTypeSection());

            // Antes section
            mainPanel.Children.Add(CreateAntesSection());

            // Sources section (tags only appear from skipping blinds)
            mainPanel.Children.Add(CreateSourcesSection());

            // Button bar
            mainPanel.Children.Add(CreateButtonBar());

            return mainPanel;
        }

        private Border CreateTagTypeSection()
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
                Text = "TAG TYPE",
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

            // Radio buttons for tag type
            var radioPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
            };

            _smallBlindRadio = new RadioButton
            {
                GroupName = "TagType",
                IsChecked = true,
                Content = new TextBlock
                {
                    Text = "Small Blind",
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily
                        ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green,
                }
            };

            _bigBlindRadio = new RadioButton
            {
                GroupName = "TagType",
                IsChecked = false,
                Content = new TextBlock
                {
                    Text = "Big Blind",
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily
                        ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        Application.Current?.FindResource("AccentRed") as IBrush ?? Brushes.Red,
                }
            };

            // Add handlers
            _smallBlindRadio.Click += (s, e) => _tagType = "smallblindtag";
            _bigBlindRadio.Click += (s, e) => _tagType = "bigblindtag";

            radioPanel.Children.Add(_smallBlindRadio);
            radioPanel.Children.Add(_bigBlindRadio);

            Grid.SetRow(radioPanel, 1);
            grid.Children.Add(radioPanel);

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

            for (int i = 0; i < 9; i++)
            {
                var anteNum = i;
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

            // Source checkboxes (tags only appear from skipping blinds)
            var sourcesPanel = new StackPanel { Spacing = 4 };

            _sourceSkip = CreateSourceCheckBox("From Skipping Blinds", "skip");
            sourcesPanel.Children.Add(_sourceSkip);

            Grid.SetRow(sourcesPanel, 1);
            grid.Children.Add(sourcesPanel);

            border.Child = grid;
            return border;
        }

        private CheckBox CreateSourceCheckBox(string displayName, string sourceKey)
        {
            var checkbox = new CheckBox
            {
                IsChecked = true,
                IsEnabled = false, // Tags always come from skipping blinds
            };

            var border = new Border
            {
                Background =
                    Application.Current?.FindResource("GreenAccentVeryDark") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1a5f1a")),
                BorderBrush =
                    Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Opacity = 0.8, // Slightly faded since it's disabled
                Child = new TextBlock
                {
                    Text = displayName + " (Always)",
                    FontFamily =
                        Application.Current?.FindResource("BalatroFont") as FontFamily
                        ?? FontFamily.Default,
                    FontSize = 12,
                    Foreground =
                        Application.Current?.FindResource("AccentGreen") as IBrush ?? Brushes.Green,
                },
            };

            checkbox.Content = border;
            return checkbox;
        }

        protected override void LoadConfiguration(ItemConfig? config)
        {
            if (config == null)
                return;

            // Load tag type
            if (!string.IsNullOrEmpty(config.TagType))
            {
                _tagType = config.TagType;
                if (_tagType == "bigblindtag" && _bigBlindRadio != null)
                {
                    _bigBlindRadio.IsChecked = true;
                    _smallBlindRadio!.IsChecked = false;
                }
                else if (_smallBlindRadio != null)
                {
                    _smallBlindRadio.IsChecked = true;
                    _bigBlindRadio!.IsChecked = false;
                }
            }

            // Load antes
            if (config.Antes != null && config.Antes.Count > 0)
            {
                // Clear all first
                for (int i = 0; i < 9; i++)
                {
                    _selectedAntes[i] = false;
                    _anteCheckBoxes[i].IsChecked = false;
                }

                // Set selected antes
                foreach (var ante in config.Antes)
                {
                    if (ante >= 0 && ante <= 8)
                    {
                        _selectedAntes[ante] = true;
                        _anteCheckBoxes[ante].IsChecked = true;
                    }
                }
            }

            // Sources are always skip for tags
        }

        protected override ItemConfig BuildConfiguration()
        {
            var config = new ItemConfig
            {
                ItemKey = ItemKey,
                Antes = GetSelectedAntes(),
                Edition = "none", // Tags don't have editions
                Sources = new List<string> { "skip" }, // Always from skipping
                TagType = _tagType, // Include the tag type (smallblindtag or bigblindtag)
            };

            return config;
        }

        private List<int>? GetSelectedAntes()
        {
            var antes = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                if (_selectedAntes[i])
                {
                    antes.Add(i);
                }
            }

            // Always return the actual selected antes, never null
            // This ensures the user's selection is preserved exactly
            return antes.Count > 0 ? antes : new List<int>();
        }
    }
}
