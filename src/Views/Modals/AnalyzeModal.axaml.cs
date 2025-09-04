using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Motely;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
// using Motely.Analysis.MotelySeedAnalyzer = Motely.Motely.Analysis.MotelySeedAnalyzer; // TODO: Fix analyzer reference

namespace BalatroSeedOracle.Views.Modals
{
    public partial class AnalyzeModal : UserControl
    {
        private readonly SpriteService _spriteService;
        private readonly UserProfileService _userProfileService;

        // Deck and Stake selector component
        private DeckAndStakeSelector? _deckAndStakeSelector;

        // UI Elements
        private Button? _settingsTab;
        private Button? _analyzerTab;
        private StackPanel? _settingsPanel;
        private ScrollViewer? _analyzerPanel;
        private Grid? _triangleContainer;

        // Analyzer UI
        private TextBox? _seedInput;
        private StackPanel? _resultsPanel;
        private TextBlock? _placeholderText;

        public AnalyzeModal()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            _userProfileService = ServiceHelper.GetRequiredService<UserProfileService>();
        }

        /// <summary>
        /// Programmatically set the seed, switch to analyzer tab, and run analysis.
        /// Safe to call after control construction; will defer execution until Loaded if needed.
        /// </summary>
        public void SetSeedAndAnalyze(string seed)
        {
            void Execute()
            {
                if (_seedInput != null)
                {
                    _seedInput.Text = seed;
                }
                // Switch to analyzer tab
                SetActiveTab(1);
                // Trigger analysis
                OnAnalyzeClick(this, new RoutedEventArgs());
            }

            if (this.IsLoaded)
            {
                Execute();
            }
            else
            {
                // Defer until loaded
                this.Loaded += (s, _) => Execute();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get tab buttons
            _settingsTab = this.FindControl<Button>("SettingsTab");
            _analyzerTab = this.FindControl<Button>("AnalyzerTab");

            // Get panels
            _settingsPanel = this.FindControl<StackPanel>("SettingsPanel");
            _analyzerPanel = this.FindControl<ScrollViewer>("AnalyzerPanel");

            // Find triangle pointer's parent Grid (for animation)
            var tabTriangle = this.FindControl<Polygon>("TabTriangle");
            _triangleContainer = tabTriangle?.Parent as Grid;

            // Get deck/stake selector component
            _deckAndStakeSelector = this.FindControl<DeckAndStakeSelector>("DeckAndStakeSelector");

            // Get analyzer UI elements
            _seedInput = this.FindControl<TextBox>("SeedInput");
            _resultsPanel = this.FindControl<StackPanel>("ResultsPanel");
            _placeholderText = this.FindControl<TextBlock>("PlaceholderText");
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Subscribe to DeckSelected event to switch to analyzer tab
            if (_deckAndStakeSelector != null)
            {
                _deckAndStakeSelector.DeckSelected += (s, _) =>
                {
                    // Switch to analyzer tab when deck is selected
                    SetActiveTab(1);
                };
            }
        }

        private void OnSettingsTabClick(object? sender, RoutedEventArgs e)
        {
            SetActiveTab(0);
        }

        private void OnAnalyzerTabClick(object? sender, RoutedEventArgs e)
        {
            SetActiveTab(1);
        }

        private void SetActiveTab(int tabIndex)
        {
            // Update button states
            _settingsTab?.Classes.Set("active", tabIndex == 0);
            _analyzerTab?.Classes.Set("active", tabIndex == 1);

            // Show/hide panels based on selected tab
            if (_settingsPanel != null)
                _settingsPanel.IsVisible = (tabIndex == 0);
            if (_analyzerPanel != null)
                _analyzerPanel.IsVisible = (tabIndex == 1);

            // Move triangle container to correct column
            if (_triangleContainer != null)
            {
                Grid.SetColumn(_triangleContainer, tabIndex);
            }
        }

        private void OnPopOutAnalyzerClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Get the current seed value
                var seed = _seedInput?.Text ?? "";
                
                // Create and show dedicated analyzer window
                var analyzerWindow = new Windows.AnalyzerWindow(seed);
                analyzerWindow.Show();
                
                BalatroSeedOracle.Helpers.DebugLogger.Log("AnalyzeModal", $"Opened pop-out analyzer window for seed: {seed}");
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError("AnalyzeModal", $"Error opening pop-out analyzer: {ex.Message}");
            }
        }

        // Analyzer functionality
        private async void OnAnalyzeClick(object? sender, RoutedEventArgs e)
        {
            var seed = _seedInput?.Text?.Trim();
            if (string.IsNullOrEmpty(seed) || _resultsPanel == null)
            {
                return;
            }

            // Clear previous results
            _resultsPanel.Children.Clear();
            if (_placeholderText != null)
                _placeholderText.IsVisible = false;

            // Get deck and stake from selector component
            var deckIndex = _deckAndStakeSelector?.DeckIndex ?? 0;
            var stakeIndex = _deckAndStakeSelector?.StakeIndex ?? 0;
            var deck = (MotelyDeck)deckIndex;
            var stake = (MotelyStake)stakeIndex;

            // Show loading indicator
            var loadingText = new TextBlock
            {
                Text = "Analyzing seed...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
                FontSize = 16,
            };
            _resultsPanel.Children.Add(loadingText);

            // Run analysis in background
            Motely.Analysis.MotelySeedAnalysis analysisData = await Task.Run(() =>
                Motely.Analysis.MotelySeedAnalyzer.Analyze(new Motely.Analysis.MotelySeedAnalysisConfig(seed, deck, stake))
            );

            // Remove loading indicator
            _resultsPanel.Children.Remove(loadingText);

            // Display results
            DisplayResults(seed, deck, stake, analysisData);
        }

        private void DisplayResults(
            string seed,
            MotelyDeck deck,
            MotelyStake stake,
            Motely.Analysis.MotelySeedAnalysis analysisData
        )
        {
            if (_resultsPanel == null)
            {
                return;
            }

            // Add header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            headerPanel.Children.Add(
                new TextBlock
                {
                    Text = $"Seed: {seed}",
                    FontSize = 24,

                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            );
            headerPanel.Children.Add(
                new TextBlock
                {
                    Text = $"Deck: {deck}, Stake: {stake}",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.8,
                }
            );
            _resultsPanel.Children.Add(headerPanel);

            // Display each ante
            foreach (var ante in analysisData.Antes)
            {
                var antePanel = new Border
                {
                    Classes = { "ante-panel" },
                    Padding = new Thickness(20),
                };
                var anteContent = new StackPanel();

                // Ante header
                anteContent.Children.Add(
                    new TextBlock { Text = $"ANTE {ante.Ante}", Classes = { "section-header" } }
                );

                // Voucher section
                if (ante.Voucher != 0)
                {
                    anteContent.Children.Add(
                        new TextBlock
                        {
                            Text = "VOUCHER",
                            FontSize = 16,

                            Margin = new Thickness(0, 10, 0, 5),
                        }
                    );

                    var voucherName = ante.Voucher.ToString();
                    anteContent.Children.Add(
                        new TextBlock
                        {
                            Text = voucherName,
                            FontSize = 14,
                            Margin = new Thickness(20, 0, 0, 10),
                        }
                    );
                }

                // Shop section
                if (ante.ShopQueue.Count > 0)
                {
                    anteContent.Children.Add(
                        new TextBlock
                        {
                            Text = "SHOP",
                            FontSize = 16,

                            Margin = new Thickness(0, 10, 0, 5),
                        }
                    );

                    var shopPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var shopItem in ante.ShopQueue)
                    {
                        var itemControl = CreateShopItemDisplay(shopItem);
                        shopPanel.Children.Add(itemControl);
                    }
                    anteContent.Children.Add(shopPanel);
                }

                // Booster packs section
                if (ante.Packs.Count > 0)
                {
                    anteContent.Children.Add(
                        new TextBlock
                        {
                            Text = "BOOSTER PACKS",
                            FontSize = 16,

                            Margin = new Thickness(0, 15, 0, 5),
                        }
                    );

                    var packsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var pack in ante.Packs)
                    {
                        var packControl = CreateBoosterPackDisplay(pack);
                        packsPanel.Children.Add(packControl);
                    }
                    anteContent.Children.Add(packsPanel);
                }

                // Tags section
                anteContent.Children.Add(
                    new TextBlock
                    {
                        Text = "SKIP TAGS",
                        FontSize = 16,
                        Margin = new Thickness(0, 15, 0, 5),
                    }
                );

                var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                tagsPanel.Children.Add(CreateTagDisplay("Small Blind", ante.SmallBlindTag));
                tagsPanel.Children.Add(CreateTagDisplay("Big Blind", ante.BigBlindTag));
                anteContent.Children.Add(tagsPanel);

                antePanel.Child = anteContent;
                _resultsPanel.Children.Add(antePanel);
            }
        }

        private Control CreateShopItemDisplay(MotelyItem item)
        {
            var container = new Border
            {
                Classes = { "shop-slot" },
                Width = 100,
                Height = 140,
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            // Add sprite based on item type
            switch (item.TypeCategory)
            {
                case MotelyItemTypeCategory.Joker:
                    var joker = item.GetJoker();
                    var jokerSprite = _spriteService.GetJokerImage(joker.ToString());
                    if (jokerSprite != null)
                    {
                        var image = new Image
                        {
                            Source = jokerSprite,
                            Width = 71,
                            Height = 95,
                            Stretch = Stretch.UniformToFill,
                        };
                        content.Children.Add(image);
                    }

                    // Add edition indicator if applicable
                    if (item.Edition != MotelyItemEdition.None)
                    {
                        content.Children.Add(
                            new TextBlock
                            {
                                Text = item.Edition.ToString(),
                                FontSize = 10,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Foreground = GetEditionColor(item.Edition),
                            }
                        );
                    }
                    break;

                case MotelyItemTypeCategory.TarotCard:
                    var tarot = item.GetTarot();
                    var tarotSprite = _spriteService.GetTarotImage(tarot.ToString());
                    if (tarotSprite != null)
                    {
                        var image = new Image
                        {
                            Source = tarotSprite,
                            Width = 71,
                            Height = 95,
                            Stretch = Stretch.UniformToFill,
                        };
                        content.Children.Add(image);
                    }
                    break;

                case MotelyItemTypeCategory.PlanetCard:
                    var planet = item.GetPlanet();
                    var planetSprite = _spriteService.GetTarotImage(planet.ToString());
                    if (planetSprite != null)
                    {
                        var image = new Image
                        {
                            Source = planetSprite,
                            Width = 71,
                            Height = 95,
                            Stretch = Stretch.UniformToFill,
                        };
                        content.Children.Add(image);
                    }
                    break;

                default:
                    content.Children.Add(
                        new TextBlock
                        {
                            Text = item.ToString(),
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        }
                    );
                    break;
            }


            container.Child = content;
            return container;
        }

        private Control CreateBoosterPackDisplay(Motely.Analysis.MotelyBoosterPackAnalysis pack)
        {
            var container = new Border
            {
                Classes = { "shop-slot" },
                MinWidth = 100,
                MaxWidth = 150,
                MinHeight = 140,
                Margin = new Thickness(4),
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            // Get pack sprite from the new booster sprites
            var packName = pack.Type.ToString().ToLowerInvariant().Replace("_", "");
            var packSprite = _spriteService.GetBoosterImage(packName);
            if (packSprite != null)
            {
                var image = new Image
                {
                    Source = packSprite,
                    Width = 71,
                    Height = 95,
                    Stretch = Stretch.Uniform,
                };
                content.Children.Add(image);
            }

            var packText = new StackPanel();
            packText.Children.Add(
                new TextBlock
                {
                    Text = pack.Type.ToString().Replace("Pack", ""),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            );

            // Show pack contents
            if (pack.Items.Count > 0)
            {
                packText.Children.Add(
                    new TextBlock
                    {
                        Text = string.Join(", ", pack.Items),
                        FontSize = 8,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 90,
                        Opacity = 0.8,
                        Margin = new Thickness(0, 2, 0, 0),
                    }
                );
            }

            content.Children.Add(packText);

            container.Child = content;
            return container;
        }

        private Control CreateTagDisplay(string blindType, MotelyTag tag)
        {
            var container = new Border { Classes = { "tag-display" }, Margin = new Thickness(4) };

            var content = new StackPanel { Orientation = Orientation.Horizontal };

            // Get tag sprite
            var tagSprite = _spriteService.GetTagImage(tag.ToString());
            if (tagSprite != null)
            {
                var image = new Image
                {
                    Source = tagSprite,
                    Width = 34,
                    Height = 34,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                content.Children.Add(image);
            }

            var textPanel = new StackPanel();
            textPanel.Children.Add(
                new TextBlock
                {
                    Text = blindType,
                    FontSize = 10,
                    Opacity = 0.7,
                }
            );
            textPanel.Children.Add(
                new TextBlock
                {
                    Text = tag.ToString().Replace("Tag", ""),
                    FontSize = 12
                }
            );
            content.Children.Add(textPanel);

            container.Child = content;
            return container;
        }

        private IBrush GetEditionColor(MotelyItemEdition edition)
        {
            return edition switch
            {
                MotelyItemEdition.Foil => Application.Current?.FindResource("BrighterBlue")
                    as IBrush
                    ?? new SolidColorBrush(Color.Parse("#8FC5FF")),
                MotelyItemEdition.Holographic => Application.Current?.FindResource("LightPurple")
                    as IBrush
                    ?? new SolidColorBrush(Color.Parse("#FF8FFF")),
                MotelyItemEdition.Polychrome => Application.Current?.FindResource("GoldGradient1")
                    as IBrush
                    ?? new SolidColorBrush(Color.Parse("#FFD700")),
                MotelyItemEdition.Negative => Application.Current?.FindResource("RedHighlight")
                    as IBrush
                    ?? new SolidColorBrush(Color.Parse("#FF5555")),
                _ => Brushes.White,
            };
        }
    }
}
