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
using Oracle.Components;
using Oracle.Helpers;
using Oracle.Services;
using SeedAnalyzerCapture = Motely.SeedAnalyzerCapture;

namespace Oracle.Views.Modals
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

            // DeckStakeSelector will handle its own initialization
        }

        private void OnFilterTabClick(object? sender, RoutedEventArgs e)
        {
            SetActiveTab(0);
        }

        private void OnSettingsTabClick(object? sender, RoutedEventArgs e)
        {
            SetActiveTab(1);
        }

        private void OnAnalyzerTabClick(object? sender, RoutedEventArgs e)
        {
            SetActiveTab(2);
        }

        private void SetActiveTab(int tabIndex)
        {
            // Update button states
            _settingsTab?.Classes.Set("active", tabIndex == 1);
            _analyzerTab?.Classes.Set("active", tabIndex == 2);

            // Move triangle container to correct column
            if (_triangleContainer != null)
            {
                Grid.SetColumn(_triangleContainer, tabIndex);
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
            var analysisData = await Task.Run(() =>
                SeedAnalyzerCapture.CaptureAnalysis(seed, deck, stake)
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
            List<SeedAnalyzerCapture.AnteData> analysisData
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
            foreach (var ante in analysisData)
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
                if (ante.Tags.Count > 0)
                {
                    anteContent.Children.Add(
                        new TextBlock
                        {
                            Text = "SKIP TAGS",
                            FontSize = 16,

                            Margin = new Thickness(0, 15, 0, 5),
                        }
                    );

                    var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    for (int i = 0; i < ante.Tags.Count; i++)
                    {
                        var blindType = i == 0 ? "Small Blind" : "Big Blind";
                        tagsPanel.Children.Add(CreateTagDisplay(blindType, ante.Tags[i]));
                    }
                    anteContent.Children.Add(tagsPanel);
                }

                antePanel.Child = anteContent;
                _resultsPanel.Children.Add(antePanel);
            }
        }

        private Control CreateShopItemDisplay(SeedAnalyzerCapture.ShopItem item)
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
            switch (item.Item.TypeCategory)
            {
                case MotelyItemTypeCategory.Joker:
                    var joker = (MotelyJoker)(item.Item.Value & 0xFFFF & ~(0b1111 << 16));
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
                    if (item.Item.Edition != MotelyItemEdition.None)
                    {
                        content.Children.Add(
                            new TextBlock
                            {
                                Text = item.Item.Edition.ToString(),
                                FontSize = 10,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Foreground = GetEditionColor(item.Item.Edition),
                            }
                        );
                    }
                    break;

                case MotelyItemTypeCategory.TarotCard:
                    var tarot = (MotelyTarotCard)(item.Item.Value & 0xFFFF & ~(0b1111 << 16));
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
                    var planet = (MotelyPlanetCard)(item.Item.Value & 0xFFFF & ~(0b1111 << 16));
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
                            Text = item.FormattedName,
                            TextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        }
                    );
                    break;
            }

            // Add slot number badge
            var slotBadge = new Border
            {
                Background =
                    Application.Current?.FindResource("GridLineGrey") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#3a3a3a")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
            };

            slotBadge.Child = new TextBlock
            {
                Text = $"#{item.Slot}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Medium,
            };

            content.Children.Add(slotBadge);

            container.Child = content;
            return container;
        }

        private Control CreateBoosterPackDisplay(SeedAnalyzerCapture.PackContent pack)
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
            var packName = pack.PackType.ToString().ToLowerInvariant().Replace("_", "");
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
                    Text = pack.PackType.ToString().Replace("Pack", ""),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeight.Bold,
                }
            );

            // Show pack contents
            if (pack.Contents.Count > 0)
            {
                packText.Children.Add(
                    new TextBlock
                    {
                        Text = string.Join(", ", pack.Contents),
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
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
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
