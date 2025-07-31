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
            await Task.Run(() =>
            {
                // Create analyzer context
                var filterDesc = new AnalyzerFilterDesc();
                var searchSettings = new MotelySearchSettings<AnalyzerFilterDesc.AnalyzerFilter>(filterDesc)
                    .WithDeck(deck)
                    .WithStake(stake)
                    .WithListSearch(new[] { seed })
                    .WithThreadCount(1);
                    
                var search = searchSettings.Start();
                
                // Wait for completion
                while (search.Status == MotelySearchStatus.Running)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                search.Dispose();
            });

            // Remove loading indicator
            ResultsPanel.Children.Remove(loadingText);

            // Get the analysis results by running our own analysis
            var results = await AnalyzeSeed(seed, deck, stake);
            
            // Display results visually
            DisplayResults(results);
        }

        private async Task<SeedAnalysisResults> AnalyzeSeed(string seed, MotelyDeck deck, MotelyStake stake)
        {
            var results = new SeedAnalysisResults { Seed = seed, Deck = deck, Stake = stake };

            // Run analysis in a task
            await Task.Run(() =>
            {
                var filterDesc = new AnalyzerFilterDesc();
                var ctx = new MotelyFilterCreationContext();
                var filter = filterDesc.CreateFilter(ref ctx);
                
                // We need to use the filter pattern to access the context
                // The context is provided by the filter system
                
                // Use a custom filter to extract the data
                var customFilter = new AnalysisExtractorFilterDesc(results);
                var searchSettings = new MotelySearchSettings<AnalysisExtractorFilterDesc.AnalysisExtractorFilter>(customFilter)
                    .WithDeck(deck)
                    .WithStake(stake)
                    .WithListSearch(new[] { seed })
                    .WithThreadCount(1);
                    
                var search = searchSettings.Start();
                
                // Wait for completion
                while (search.Status == MotelySearchStatus.Running)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                search.Dispose();
            });

            return results;
        }

        private void DisplayResults(SeedAnalysisResults results)
        {
            // Add header
            var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Seed: {results.Seed}",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Deck: {results.Deck}, Stake: {results.Stake}",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.8
            });
            ResultsPanel.Children.Add(headerPanel);

            // Display each ante
            foreach (var ante in results.Antes)
            {
                var antePanel = new Border { Classes = { "ante-panel" } };
                var anteContent = new StackPanel();

                // Ante header
                anteContent.Children.Add(new TextBlock
                {
                    Text = $"ANTE {ante.Ante}",
                    Classes = { "section-header" }
                });

                // Shop section
                if (ante.ShopItems.Any())
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "SHOP",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 10, 0, 5)
                    });

                    var shopPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var shopItem in ante.ShopItems)
                    {
                        var itemControl = CreateShopItemDisplay(shopItem);
                        shopPanel.Children.Add(itemControl);
                    }
                    anteContent.Children.Add(shopPanel);
                }

                // Booster packs section
                if (ante.BoosterPacks.Any())
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "BOOSTER PACKS",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 15, 0, 5)
                    });

                    var packsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    foreach (var pack in ante.BoosterPacks)
                    {
                        var packControl = CreateBoosterPackDisplay(pack);
                        packsPanel.Children.Add(packControl);
                    }
                    anteContent.Children.Add(packsPanel);
                }

                // Tags section
                if (ante.SmallBlindTag != 0 || ante.BigBlindTag != 0)
                {
                    anteContent.Children.Add(new TextBlock
                    {
                        Text = "SKIP TAGS",
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        Margin = new Thickness(0, 15, 0, 5)
                    });

                    var tagsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
                    if (ante.SmallBlindTag != 0)
                    {
                        tagsPanel.Children.Add(CreateTagDisplay("Small Blind", ante.SmallBlindTag));
                    }
                    if (ante.BigBlindTag != 0)
                    {
                        tagsPanel.Children.Add(CreateTagDisplay("Big Blind", ante.BigBlindTag));
                    }
                    anteContent.Children.Add(tagsPanel);
                }

                antePanel.Child = anteContent;
                ResultsPanel.Children.Add(antePanel);
            }
        }

        private Control CreateShopItemDisplay(ShopItemResult item)
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
            switch (item.Type)
            {
                case ShopState.ShopItem.ShopItemType.Joker:
                    var jokerSprite = _spriteService.GetJokerImage(item.Joker.ToString());
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
                    if (item.Edition != MotelyItemEdition.None)
                    {
                        content.Children.Add(new TextBlock
                        {
                            Text = item.Edition.ToString(),
                            FontSize = 10,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = GetEditionColor(item.Edition)
                        });
                    }
                    break;

                case ShopState.ShopItem.ShopItemType.Tarot:
                    var tarotSprite = _spriteService.GetTarotImage(item.Tarot.ToString());
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

                case ShopState.ShopItem.ShopItemType.Planet:
                    // Planet images are in the tarot sheet
                    var planetSprite = _spriteService.GetTarotImage(item.Planet.ToString());
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
                        Text = item.Type.ToString(),
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    break;
            }

            // Add slot number
            content.Children.Add(new TextBlock
            {
                Text = $"Slot {item.Slot}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.6,
                Margin = new Thickness(0, 2, 0, 0)
            });

            container.Child = content;
            return container;
        }

        private Control CreateBoosterPackDisplay(MotelyBoosterPack pack)
        {
            var container = new Border
            {
                Classes = { "shop-slot" },
                Width = 100,
                Height = 140,
                Margin = new Thickness(4)
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Get pack sprite from the new booster sprites
            var packName = pack.ToString().ToLower().Replace("_", "");
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

            content.Children.Add(new TextBlock
            {
                Text = pack.ToString().Replace("Pack", ""),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });

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
    }

    // Result classes
    public class SeedAnalysisResults
    {
        public string Seed { get; set; } = "";
        public MotelyDeck Deck { get; set; }
        public MotelyStake Stake { get; set; }
        public List<AnteResults> Antes { get; set; } = new();
    }

    public class AnteResults
    {
        public int Ante { get; set; }
        public List<ShopItemResult> ShopItems { get; set; } = new();
        public List<MotelyBoosterPack> BoosterPacks { get; set; } = new();
        public MotelyTag SmallBlindTag { get; set; }
        public MotelyTag BigBlindTag { get; set; }
    }

    public class ShopItemResult
    {
        public int Slot { get; set; }
        public ShopState.ShopItem.ShopItemType Type { get; set; }
        public MotelyJoker Joker { get; set; }
        public MotelyTarotCard Tarot { get; set; }
        public MotelyPlanetCard Planet { get; set; }
        public MotelyItemEdition Edition { get; set; }
    }

    // Custom filter to extract analysis data
    public struct AnalysisExtractorFilterDesc : IMotelySeedFilterDesc<AnalysisExtractorFilterDesc.AnalysisExtractorFilter>
    {
        private readonly SeedAnalysisResults _results;

        public AnalysisExtractorFilterDesc(SeedAnalysisResults results)
        {
            _results = results;
        }

        public AnalysisExtractorFilter CreateFilter(ref MotelyFilterCreationContext ctx)
        {
            return new AnalysisExtractorFilter(_results);
        }

        public struct AnalysisExtractorFilter : IMotelySeedFilter
        {
            private readonly SeedAnalysisResults _results;

            public AnalysisExtractorFilter(SeedAnalysisResults results)
            {
                _results = results;
            }

            public VectorMask Filter(ref MotelyVectorSearchContext ctx)
            {
                // For analyzer, we just want to check individual seeds
                return ctx.SearchIndividualSeeds(CheckSeed);
            }

            public bool CheckSeed(ref MotelySingleSearchContext ctx)
            {
                // Analyze each ante
                for (int ante = 1; ante <= 8; ante++)
                {
                    var anteResult = new AnteResults { Ante = ante };
                    
                    // Get shop
                    var shop = ctx.GenerateFullShop(ante);
                    int slots = ante == 1 ? ShopState.ShopSlotsAnteOne : ShopState.ShopSlots;
                    
                    for (int i = 0; i < slots; i++)
                    {
                        ref var item = ref shop.Items[i];
                        if (item.Type != ShopState.ShopItem.ShopItemType.Empty)
                        {
                            anteResult.ShopItems.Add(new ShopItemResult
                            {
                                Slot = i + 1,
                                Type = item.Type,
                                Joker = item.Joker,
                                Tarot = item.Tarot,
                                Planet = item.Planet,
                                Edition = item.Edition
                            });
                        }
                    }
                    
                    // Get booster packs
                    var packStream = ctx.CreateBoosterPackStream(ante);
                    var pack1 = ctx.GetNextBoosterPack(ref packStream);
                    var pack2 = ctx.GetNextBoosterPack(ref packStream);
                    
                    if (pack1 != 0)
                        anteResult.BoosterPacks.Add((MotelyBoosterPack)pack1);
                    if (pack2 != 0)
                        anteResult.BoosterPacks.Add((MotelyBoosterPack)pack2);
                    
                    // Get tags
                    var tagStream = ctx.CreateTagStream(ante);
                    var smallTag = ctx.GetNextTag(ref tagStream);
                    var bigTag = ctx.GetNextTag(ref tagStream);
                    
                    if (smallTag != 0)
                        anteResult.SmallBlindTag = smallTag;
                    if (bigTag != 0)
                        anteResult.BigBlindTag = bigTag;
                    
                    _results.Antes.Add(anteResult);
                }
                
                // Return false so we don't actually match this seed
                return false;
            }
        }
    }
}