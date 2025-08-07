using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Oracle.Components;
using Oracle.Services;
using Oracle.Helpers;
using Motely;
using SeedAnalyzerCapture = Motely.SeedAnalyzerCapture;

namespace Oracle.Views.Modals
{
    public partial class AnalyzerModal : UserControl
    {
        private readonly SpriteService _spriteService;

        public AnalyzerModal()
        {
            InitializeComponent();
            _spriteService = ServiceHelper.GetRequiredService<SpriteService>();
        }

        private async void OnAnalyzeClick(object? sender, RoutedEventArgs e)
        {
            var seed = SeedInput.Text?.Trim();
            if (string.IsNullOrEmpty(seed))
            {
                return;
            }

            // Clear previous results
            ResultsPanel.Children.Clear();
            PlaceholderText.IsVisible = false;

            // Get deck and stake from combo box
            var (deck, stake) = ParseDeckStake(DeckStakeCombo.SelectedIndex);

            // Show loading indicator
            var loadingText = new TextBlock
            {
                Text = "Analyzing seed...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
                FontSize = 16
            };
            ResultsPanel.Children.Add(loadingText);

            // Run analysis in background
            var analysisData = await Task.Run(() => SeedAnalyzerCapture.CaptureAnalysis(seed, deck, stake));

            // Remove loading indicator
            ResultsPanel.Children.Remove(loadingText);

            // Display results
            DisplayResults(seed, deck, stake, analysisData);
        }


        private void DisplayResults(string seed, MotelyDeck deck, MotelyStake stake, List<SeedAnalyzerCapture.AnteData> analysisData)
        {
            // Add header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Seed: {seed}",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Deck: {deck}, Stake: {stake}",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.8
            });
            ResultsPanel.Children.Add(headerPanel);

            // Display each ante
            foreach (var ante in analysisData)
            {
                var antePanel = new Border { Classes = { "ante-panel" } };
                var anteContent = new StackPanel();

                // Ante header
                anteContent.Children.Add(new TextBlock
                {
                    Text = $"ANTE {ante.Ante}",
                    Classes = { "section-header" }
                });

                // Voucher section
                if (ante.Voucher != 0)
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "VOUCHER",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 10, 0, 5)
                    });

                    var voucherName = ante.Voucher.ToString();
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = voucherName,
                        FontSize = 14,
                        Margin = new Thickness(20, 0, 0, 10)
                    });
                }

                // Shop section
                if (ante.ShopQueue.Any())
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "SHOP",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 10, 0, 5)
                    });

                    var shopPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var shopItem in ante.ShopQueue)
                    {
                        var itemControl = CreateShopItemDisplay(shopItem);
                        shopPanel.Children.Add(itemControl);
                    }
                    anteContent.Children.Add(shopPanel);
                }

                // Booster packs section
                if (ante.Packs.Any())
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "BOOSTER PACKS",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 15, 0, 5)
                    });

                    var packsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var pack in ante.Packs)
                    {
                        var packControl = CreateBoosterPackDisplay(pack);
                        packsPanel.Children.Add(packControl);
                    }
                    anteContent.Children.Add(packsPanel);
                }

                // Tags section
                if (ante.Tags.Any())
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "SKIP TAGS",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 15, 0, 5)
                    });

                    var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    for (int i = 0; i < ante.Tags.Count; i++)
                    {
                        var blindType = i == 0 ? "Small Blind" : "Big Blind";
                        tagsPanel.Children.Add(CreateTagDisplay(blindType, ante.Tags[i]));
                    }
                    anteContent.Children.Add(tagsPanel);
                }

                antePanel.Child = anteContent;
                ResultsPanel.Children.Add(antePanel);
            }
        }

        private Control CreateShopItemDisplay(SeedAnalyzerCapture.ShopItem item)
        {
            var container = new Border
            {
                Classes = { "shop-slot" },
                Width = 100,
                Height = 140
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
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
                            Stretch = Stretch.UniformToFill
                        };
                        content.Children.Add(image);
                    }

                    // Add edition indicator if applicable
                    if (item.Item.Edition != MotelyItemEdition.None)
                    {
                        content.Children.Add(new TextBlock
                        {
                            Text = item.Item.Edition.ToString(),
                            FontSize = 10,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = GetEditionColor(item.Item.Edition)
                        });
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
                            Stretch = Stretch.UniformToFill
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
                            Stretch = Stretch.UniformToFill
                        };
                        content.Children.Add(image);
                    }
                    break;

                default:
                    content.Children.Add(new TextBlock
                    {
                        Text = item.FormattedName,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    break;
            }

            // Add slot number badge
            var slotBadge = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#3a3a3a")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            slotBadge.Child = new TextBlock
            {
                Text = $"#{item.Slot}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Medium
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
                Margin = new Thickness(4)
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Get pack sprite from the new booster sprites
            var packName = pack.PackType.ToString().ToLower().Replace("_", "");
            var packSprite = _spriteService.GetBoosterImage(packName);
            if (packSprite != null)
            {
                var image = new Image
                {
                    Source = packSprite,
                    Width = 71,
                    Height = 95,
                    Stretch = Stretch.Uniform
                };
                content.Children.Add(image);
            }

            var packText = new StackPanel();
            packText.Children.Add(new TextBlock
            {
                Text = pack.PackType.ToString().Replace("Pack", ""),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold
            });

            // Show pack contents
            if (pack.Contents.Any())
            {
                packText.Children.Add(new TextBlock
                {
                    Text = string.Join(", ", pack.Contents),
                    FontSize = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 90,
                    Opacity = 0.8,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            content.Children.Add(packText);

            container.Child = content;
            return container;
        }

        private Control CreateTagDisplay(string blindType, MotelyTag tag)
        {
            var container = new Border
            {
                Classes = { "tag-display" },
                Margin = new Thickness(4)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

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
                    Margin = new Thickness(0, 0, 8, 0)
                };
                content.Children.Add(image);
            }

            var textPanel = new StackPanel();
            textPanel.Children.Add(new TextBlock
            {
                Text = blindType,
                FontSize = 10,
                Opacity = 0.7
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = tag.ToString().Replace("Tag", ""),
                FontSize = 12,
                FontWeight = FontWeight.Bold
            });
            content.Children.Add(textPanel);

            container.Child = content;
            return container;
        }

        private IBrush GetEditionColor(MotelyItemEdition edition)
        {
            return edition switch
            {
                MotelyItemEdition.Foil => new SolidColorBrush(Color.Parse("#8FC5FF")),
                MotelyItemEdition.Holographic => new SolidColorBrush(Color.Parse("#FF8FFF")),
                MotelyItemEdition.Polychrome => new SolidColorBrush(Color.Parse("#FFD700")),
                MotelyItemEdition.Negative => new SolidColorBrush(Color.Parse("#FF5555")),
                _ => Brushes.White
            };
        }

        private (MotelyDeck deck, MotelyStake stake) ParseDeckStake(int selectedIndex)
        {
            return selectedIndex switch
            {
                0 => (MotelyDeck.Red, MotelyStake.White),
                1 => (MotelyDeck.Red, MotelyStake.Red),
                2 => (MotelyDeck.Red, MotelyStake.Green),
                3 => (MotelyDeck.Red, MotelyStake.Black),
                4 => (MotelyDeck.Red, MotelyStake.Blue),
                5 => (MotelyDeck.Red, MotelyStake.Purple),
                6 => (MotelyDeck.Red, MotelyStake.Orange),
                7 => (MotelyDeck.Red, MotelyStake.Gold),
                8 => (MotelyDeck.Blue, MotelyStake.White),
                9 => (MotelyDeck.Yellow, MotelyStake.White),
                10 => (MotelyDeck.Green, MotelyStake.White),
                11 => (MotelyDeck.Black, MotelyStake.White),
                12 => (MotelyDeck.Magic, MotelyStake.White),
                13 => (MotelyDeck.Nebula, MotelyStake.White),
                14 => (MotelyDeck.Ghost, MotelyStake.White),
                15 => (MotelyDeck.Abandoned, MotelyStake.White),
                16 => (MotelyDeck.Checkered, MotelyStake.White),
                17 => (MotelyDeck.Zodiac, MotelyStake.White),
                18 => (MotelyDeck.Painted, MotelyStake.White),
                19 => (MotelyDeck.Anaglyph, MotelyStake.White),
                20 => (MotelyDeck.Plasma, MotelyStake.White),
                21 => (MotelyDeck.Erratic, MotelyStake.White),
                _ => (MotelyDeck.Red, MotelyStake.White)
            };
        }

        private string GetAnteIcon(int ante)
        {
            return ante switch
            {
                1 => "🔵",
                2 => "🟢",
                3 => "🟡",
                4 => "🟠",
                5 => "🔴",
                6 => "🟣",
                7 => "⚫",
                8 => "⚪",
                _ => "❓"
            };
        }

        private string FormatVoucherName(string name)
        {
            // Add spaces before capital letters
            return System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();
        }

        private string FormatPackName(string name)
        {
            // Remove "Pack" and add spaces
            name = name.Replace("Pack", "");
            name = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();

            // Special formatting for pack types
            return name switch
            {
                "Jumbo Arcana" => "Jumbo Arcana Pack",
                "Jumbo Buffoon" => "Jumbo Buffoon Pack",
                "Jumbo Celestial" => "Jumbo Celestial Pack",
                "Jumbo Spectral" => "Jumbo Spectral Pack",
                "Jumbo Standard" => "Jumbo Standard Pack",
                "Mega Arcana" => "Mega Arcana Pack",
                "Mega Buffoon" => "Mega Buffoon Pack",
                "Mega Celestial" => "Mega Celestial Pack",
                "Mega Spectral" => "Mega Spectral Pack",
                "Mega Standard" => "Mega Standard Pack",
                _ => name + " Pack"
            };
        }
    }
}