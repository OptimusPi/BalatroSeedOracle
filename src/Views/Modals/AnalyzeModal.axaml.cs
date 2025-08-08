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
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Oracle.Components;
using Oracle.Services;
using Oracle.Helpers;
using Motely;
using SeedAnalyzerCapture = Motely.SeedAnalyzerCapture;

namespace Oracle.Views.Modals
{
    public partial class AnalyzeModal : UserControl
    {
        private readonly SpriteService _spriteService;
        private readonly UserProfileService _userProfileService;
        
        // Deck and stake tracking
        private int _currentDeckIndex = 0;
        private int _currentStakeIndex = 0;
        
        // UI Elements
        private Button? _filterTab;
        private Button? _settingsTab;
        private Button? _analyzerTab;
        private ScrollViewer? _filterPanel;
        private ScrollViewer? _settingsPanel;
        private ScrollViewer? _analyzerPanel;
        private Path? _tabIndicator;
        private TranslateTransform? _tabIndicatorTransform;
        private FilterSelector? _filterSelector;
        
        // Deck/Stake UI
        private Image? _deckPreviewImage;
        private Image? _stakeChipOverlay;
        private TextBlock? _deckNameText;
        private TextBlock? _deckDescText;
        private TextBlock? _stakeNameText;
        private TextBlock? _stakeDescText;
        private Canvas? _stakeChipsCanvas;
        
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
            _filterTab = this.FindControl<Button>("FilterTab");
            _settingsTab = this.FindControl<Button>("SettingsTab");
            _analyzerTab = this.FindControl<Button>("AnalyzerTab");
            
            // Get panels
            _filterPanel = this.FindControl<ScrollViewer>("FilterPanel");
            _settingsPanel = this.FindControl<ScrollViewer>("SettingsPanel");
            _analyzerPanel = this.FindControl<ScrollViewer>("AnalyzerPanel");
            
            // Get tab indicator
            _tabIndicator = this.FindControl<Path>("TabIndicator");
            if (_tabIndicator != null)
            {
                _tabIndicatorTransform = _tabIndicator.RenderTransform as TranslateTransform;
            }
            
            // Get filter selector
            _filterSelector = this.FindControl<FilterSelector>("FilterSelector");
            
            // Get deck/stake UI elements
            _deckPreviewImage = this.FindControl<Image>("DeckPreviewImage");
            _stakeChipOverlay = this.FindControl<Image>("StakeChipOverlay");
            _deckNameText = this.FindControl<TextBlock>("DeckNameText");
            _deckDescText = this.FindControl<TextBlock>("DeckDescText");
            _stakeNameText = this.FindControl<TextBlock>("StakeNameText");
            _stakeDescText = this.FindControl<TextBlock>("StakeDescText");
            _stakeChipsCanvas = this.FindControl<Canvas>("StakeChipsCanvas");
            
            // Get analyzer UI elements
            _seedInput = this.FindControl<TextBox>("SeedInput");
            _resultsPanel = this.FindControl<StackPanel>("ResultsPanel");
            _placeholderText = this.FindControl<TextBlock>("PlaceholderText");
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            
            // Initialize deck and stake display
            UpdateDeckDisplay();
            UpdateStakeDisplay();
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
            _filterTab?.Classes.Set("active", tabIndex == 0);
            _settingsTab?.Classes.Set("active", tabIndex == 1);
            _analyzerTab?.Classes.Set("active", tabIndex == 2);
            
            // Show/hide panels
            if (_filterPanel != null) _filterPanel.IsVisible = tabIndex == 0;
            if (_settingsPanel != null) _settingsPanel.IsVisible = tabIndex == 1;
            if (_analyzerPanel != null) _analyzerPanel.IsVisible = tabIndex == 2;
            
            // Animate tab indicator
            if (_tabIndicatorTransform != null)
            {
                // Calculate center position for each button
                // SELECT FILTER = ~140px, SETTINGS = ~100px, ANALYZE = ~100px + margins
                double targetX = tabIndex switch
                {
                    0 => 62,   // Center under SELECT FILTER
                    1 => 210,  // Center under SETTINGS  
                    2 => 350,  // Center under ANALYZE
                    _ => 62
                };
                
                var animation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
                    Easing = new CubicEaseOut(),
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter(TranslateTransform.XProperty, targetX) },
                            Cue = new Cue(1)
                        }
                    }
                };
                animation.RunAsync(_tabIndicatorTransform);
            }
        }

        // Deck navigation
        private void OnDeckLeftClick(object? sender, RoutedEventArgs e)
        {
            _currentDeckIndex--;
            if (_currentDeckIndex < 0) _currentDeckIndex = 14; // 15 decks total
            UpdateDeckDisplay();
        }

        private void OnDeckRightClick(object? sender, RoutedEventArgs e)
        {
            _currentDeckIndex++;
            if (_currentDeckIndex > 14) _currentDeckIndex = 0;
            UpdateDeckDisplay();
        }

        // Stake navigation
        private void OnStakeLeftClick(object? sender, RoutedEventArgs e)
        {
            _currentStakeIndex--;
            if (_currentStakeIndex < 0) _currentStakeIndex = 7; // 8 stakes total
            UpdateStakeDisplay();
        }

        private void OnStakeRightClick(object? sender, RoutedEventArgs e)
        {
            _currentStakeIndex++;
            if (_currentStakeIndex > 7) _currentStakeIndex = 0;
            UpdateStakeDisplay();
        }

        private void UpdateDeckDisplay()
        {
            var deckInfo = GetDeckInfo(_currentDeckIndex);
            
            if (_deckNameText != null) _deckNameText.Text = deckInfo.name;
            if (_deckDescText != null) _deckDescText.Text = deckInfo.description;
            
            // Update deck image
            if (_deckPreviewImage != null)
            {
                var deckSprite = _spriteService.GetDeckImage(deckInfo.spriteName);
                _deckPreviewImage.Source = deckSprite;
            }
            
            // Update stake overlay
            UpdateStakeOverlay();
        }

        private void UpdateStakeDisplay()
        {
            var stakeInfo = GetStakeInfo(_currentStakeIndex);
            
            if (_stakeNameText != null) _stakeNameText.Text = stakeInfo.name;
            if (_stakeDescText != null) _stakeDescText.Text = stakeInfo.description;
            
            // Update stake chips
            if (_stakeChipsCanvas != null)
            {
                _stakeChipsCanvas.Children.Clear();
                var chipSprite = _spriteService.GetStakeChipImage(stakeInfo.spriteName);
                
                if (chipSprite != null)
                {
                    // Create stacked chips effect
                    for (int i = 0; i <= _currentStakeIndex; i++)
                    {
                        var chip = new Image
                        {
                            Source = chipSprite,
                            Width = 29,
                            Height = 29,
                            Stretch = Stretch.Uniform
                        };
                        
                        Canvas.SetLeft(chip, 25 + (i % 4) * 6);
                        Canvas.SetTop(chip, 25 - (i / 4) * 6);
                        chip.ZIndex = i;
                        
                        _stakeChipsCanvas.Children.Add(chip);
                    }
                }
            }
            
            // Update stake overlay on deck
            UpdateStakeOverlay();
        }

        private void UpdateStakeOverlay()
        {
            if (_stakeChipOverlay != null && _currentStakeIndex > 0)
            {
                var stakeName = GetStakeInfo(_currentStakeIndex).spriteName;
                var overlaySprite = _spriteService.GetStakeChipImage(stakeName);
                _stakeChipOverlay.Source = overlaySprite;
                _stakeChipOverlay.IsVisible = true;
            }
            else if (_stakeChipOverlay != null)
            {
                _stakeChipOverlay.IsVisible = false;
            }
        }

        private (string name, string description, string spriteName) GetDeckInfo(int index)
        {
            return index switch
            {
                0 => ("Red Deck", "+1 Discard\nevery round", "red"),
                1 => ("Blue Deck", "+1 Hand\nevery round", "blue"),
                2 => ("Yellow Deck", "+$10 at\nstart of run", "yellow"),
                3 => ("Green Deck", "At end of each Round:\n+$1 interest per $5\n(max $5 interest)", "green"),
                4 => ("Black Deck", "+1 Joker slot\n-1 Hand every round", "black"),
                5 => ("Magic Deck", "Start run with the\nCrystal Ball voucher\nand 2 copies of The Fool", "magic"),
                6 => ("Nebula Deck", "Start run with the\nTelescope voucher\n-1 consumable slot", "nebula"),
                7 => ("Ghost Deck", "Spectral cards may\nappear in the shop\nstart with a Hex", "ghost"),
                8 => ("Abandoned Deck", "Start with no\nFace Cards\nin your deck", "abandoned"),
                9 => ("Checkered Deck", "Start with 26 Spades\nand 26 Hearts\nin deck", "checkered"),
                10 => ("Zodiac Deck", "Start run with\nTarot Merchant,\nPlanet Merchant,\nand Overstock", "zodiac"),
                11 => ("Painted Deck", "+2 Hand Size\n-1 Joker Slot", "painted"),
                12 => ("Anaglyph Deck", "After defeating each\nBoss Blind, gain a\nDouble Tag", "anaglyph"),
                13 => ("Plasma Deck", "Balance Chips and\nMult when calculating\nscore for played hand", "plasma"),
                14 => ("Erratic Deck", "All Ranks and Suits\nin deck are randomized", "erratic"),
                _ => ("Red Deck", "+1 Discard\nevery round", "red")
            };
        }

        private (string name, string description, string spriteName) GetStakeInfo(int index)
        {
            return index switch
            {
                0 => ("White Stake", "Base Difficulty", "white"),
                1 => ("Red Stake", "Small Blind gives\nno reward money", "red"),
                2 => ("Green Stake", "Required score scales\nfaster for each Ante", "green"),
                3 => ("Black Stake", "Shop can have\nEternal Jokers\n(Can't be sold or destroyed)", "black"),
                4 => ("Blue Stake", "-1 Discard", "blue"),
                5 => ("Purple Stake", "Required score scales\nfaster for each Ante", "purple"),
                6 => ("Orange Stake", "Shop can have\nPerishable Jokers\n(Debuffed after 5 Rounds)", "orange"),
                7 => ("Gold Stake", "Shop can have\nRental Jokers\n(Costs $1 per round)", "gold"),
                _ => ("White Stake", "Base Difficulty", "white")
            };
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
            if (_placeholderText != null) _placeholderText.IsVisible = false;

            // Get deck and stake from current selection
            var deck = (MotelyDeck)_currentDeckIndex;
            var stake = (MotelyStake)_currentStakeIndex;

            // Show loading indicator
            var loadingText = new TextBlock
            {
                Text = "Analyzing seed...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
                FontSize = 16
            };
            _resultsPanel.Children.Add(loadingText);

            // Run analysis in background
            var analysisData = await Task.Run(() => SeedAnalyzerCapture.CaptureAnalysis(seed, deck, stake));

            // Remove loading indicator
            _resultsPanel.Children.Remove(loadingText);

            // Display results
            DisplayResults(seed, deck, stake, analysisData);
        }

        private void DisplayResults(string seed, MotelyDeck deck, MotelyStake stake, List<SeedAnalyzerCapture.AnteData> analysisData)
        {
            if (_resultsPanel == null) return;

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
            _resultsPanel.Children.Add(headerPanel);

            // Display each ante
            foreach (var ante in analysisData)
            {
                var antePanel = new Border 
                { 
                    Classes = { "ante-panel" },
                    Padding = new Thickness(20)
                };
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
                _resultsPanel.Children.Add(antePanel);
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
                Background = Application.Current?.FindResource("GridLineGrey") as IBrush ?? new SolidColorBrush(Color.Parse("#3a3a3a")),
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
                MotelyItemEdition.Foil => Application.Current?.FindResource("BrighterBlue") as IBrush ?? new SolidColorBrush(Color.Parse("#8FC5FF")),
                MotelyItemEdition.Holographic => Application.Current?.FindResource("LightPurple") as IBrush ?? new SolidColorBrush(Color.Parse("#FF8FFF")),
                MotelyItemEdition.Polychrome => Application.Current?.FindResource("GoldGradient1") as IBrush ?? new SolidColorBrush(Color.Parse("#FFD700")),
                MotelyItemEdition.Negative => Application.Current?.FindResource("RedHighlight") as IBrush ?? new SolidColorBrush(Color.Parse("#FF5555")),
                _ => Brushes.White
            };
        }
    }
}