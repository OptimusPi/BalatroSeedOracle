using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.ViewModels
{
    public partial class AnalyzeModalViewModel : ObservableObject
    {
        private readonly SpriteService _spriteService;
        private readonly UserProfileService _userProfileService;

        [ObservableProperty]
        private string _seedInput = "";

        [ObservableProperty]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        private AnalyzeModalTab _activeTab = AnalyzeModalTab.Settings;

        [ObservableProperty]
        private bool _showPlaceholder = true;

        [ObservableProperty]
        private int _deckIndex = 0;

        [ObservableProperty]
        private int _stakeIndex = 0;

        [ObservableProperty]
        private SeedAnalysisModel? _currentAnalysis;

        public AnalyzeModalViewModel(SpriteService spriteService, UserProfileService userProfileService)
        {
            _spriteService = spriteService;
            _userProfileService = userProfileService;

            Antes = new ObservableCollection<AnteAnalysisModel>();
        }

        #region Properties

        public bool IsSettingsTabActive => ActiveTab == AnalyzeModalTab.Settings;
        public bool IsAnalyzerTabActive => ActiveTab == AnalyzeModalTab.Analyzer;
        public bool SettingsTabVisible => ActiveTab == AnalyzeModalTab.Settings;
        public bool AnalyzerTabVisible => ActiveTab == AnalyzeModalTab.Analyzer;
        public int TriangleColumn => ActiveTab == AnalyzeModalTab.Settings ? 0 : 1;

        public MotelyDeck SelectedDeck => (MotelyDeck)DeckIndex;
        public MotelyStake SelectedStake => (MotelyStake)StakeIndex;

        public bool HasAnalysisResults =>
            CurrentAnalysis != null && !string.IsNullOrEmpty(CurrentAnalysis.Error) == false;
        public string AnalysisHeader =>
            CurrentAnalysis != null
                ? $"Seed: {CurrentAnalysis.Seed} | Deck: {CurrentAnalysis.Deck} | Stake: {CurrentAnalysis.Stake}"
                : "";

        public ObservableCollection<AnteAnalysisModel> Antes { get; }

        #endregion

        #region Generated Property Changed Methods

        partial void OnSeedInputChanged(string value)
        {
            AnalyzeSeedCommand.NotifyCanExecuteChanged();
            UpdatePlaceholderVisibility();
        }

        partial void OnIsAnalyzingChanged(bool value)
        {
            AnalyzeSeedCommand.NotifyCanExecuteChanged();
        }

        partial void OnActiveTabChanged(AnalyzeModalTab value)
        {
            OnPropertyChanged(nameof(IsSettingsTabActive));
            OnPropertyChanged(nameof(IsAnalyzerTabActive));
            OnPropertyChanged(nameof(SettingsTabVisible));
            OnPropertyChanged(nameof(AnalyzerTabVisible));
            OnPropertyChanged(nameof(TriangleColumn));
        }

        partial void OnDeckIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedDeck));
        }

        partial void OnStakeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedStake));
        }

        partial void OnCurrentAnalysisChanged(SeedAnalysisModel? value)
        {
            OnPropertyChanged(nameof(HasAnalysisResults));
            OnPropertyChanged(nameof(AnalysisHeader));
        }

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanAnalyzeSeed))]
        private async Task AnalyzeSeedAsync()
        {
            if (string.IsNullOrWhiteSpace(SeedInput))
                return;

            try
            {
                IsAnalyzing = true;
                ShowPlaceholder = false;

                DebugLogger.Log("AnalyzeModalViewModel", $"Starting analysis for seed: {SeedInput}");

                // Clear previous results
                Antes.Clear();
                CurrentAnalysis = null;

                // Perform seed analysis
                var seed = SeedInput.Trim();
                var deck = SelectedDeck;
                var stake = SelectedStake;

                var analysisData = await Task.Run(() =>
                {
                    var config = new Motely.Analysis.MotelySeedAnalysisConfig(seed, deck, stake);
                    return Motely.Analysis.MotelySeedAnalyzer.Analyze(config);
                });

                // Map Motely analysis to our models
                CurrentAnalysis = new SeedAnalysisModel
                {
                    Seed = seed,
                    Deck = deck,
                    Stake = stake,
                    Error = analysisData.Error,
                };

                if (!string.IsNullOrEmpty(analysisData.Error))
                {
                    DebugLogger.LogError("AnalyzeModalViewModel", $"Analysis error: {analysisData.Error}");
                }
                else
                {
                    // Convert Motely antes to our model
                    foreach (var motelyAnte in analysisData.Antes)
                    {
                        var anteModel = new AnteAnalysisModel
                        {
                            AnteNumber = motelyAnte.Ante,
                            Boss = motelyAnte.Boss,
                            Voucher = motelyAnte.Voucher,
                            SmallBlindTag = new TagModel { BlindType = "Small Blind", Tag = motelyAnte.SmallBlindTag },
                            BigBlindTag = new TagModel { BlindType = "Big Blind", Tag = motelyAnte.BigBlindTag },
                        };

                        // Convert shop items
                        foreach (var shopItem in motelyAnte.ShopQueue)
                        {
                            anteModel.ShopItems.Add(
                                new ShopItemModel
                                {
                                    TypeCategory = shopItem.TypeCategory,
                                    ItemType = shopItem.Type,
                                    Edition = shopItem.Edition,
                                }
                            );
                        }

                        // Convert booster packs
                        foreach (var pack in motelyAnte.Packs)
                        {
                            var packModel = new BoosterPackModel { PackType = (MotelyBoosterPackType)pack.Type };

                            foreach (var item in pack.Items)
                            {
                                packModel.Items.Add(item.ToString());
                            }

                            anteModel.BoosterPacks.Add(packModel);
                        }

                        Antes.Add(anteModel);
                    }

                    // Attach ante collection to the current analysis for shared display component
                    if (CurrentAnalysis != null)
                    {
                        CurrentAnalysis.Antes = Antes;
                    }

                    DebugLogger.Log("AnalyzeModalViewModel", $"Analysis completed successfully: {Antes.Count} antes");
                }

                UpdatePlaceholderVisibility();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AnalyzeModalViewModel", $"Error analyzing seed: {ex.Message}");

                CurrentAnalysis = new SeedAnalysisModel
                {
                    Seed = SeedInput.Trim(),
                    Deck = SelectedDeck,
                    Stake = SelectedStake,
                    Error = $"Failed to analyze seed: {ex.Message}",
                };
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private bool CanAnalyzeSeed()
        {
            return !string.IsNullOrWhiteSpace(SeedInput) && !IsAnalyzing;
        }

        [RelayCommand]
        private void ClearResults()
        {
            Antes.Clear();
            CurrentAnalysis = null;
            SeedInput = "";
            UpdatePlaceholderVisibility();
            DebugLogger.Log("AnalyzeModalViewModel", "Results cleared");
        }

        [RelayCommand]
        private void SwitchToSettingsTab()
        {
            ActiveTab = AnalyzeModalTab.Settings;
        }

        [RelayCommand]
        private void SwitchToAnalyzerTab()
        {
            ActiveTab = AnalyzeModalTab.Analyzer;
        }

        [RelayCommand]
        private void PopOutAnalyzer()
        {
            try
            {
                var seed = SeedInput ?? "";
                var analyzerViewModel = new AnalyzerViewModel();
                analyzerViewModel.AddSeed(seed);
                var analyzerWindow = new Windows.AnalyzerWindow(analyzerViewModel);
                analyzerWindow.Show();

                DebugLogger.Log("AnalyzeModalViewModel", $"Opened pop-out analyzer window for seed: {seed}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AnalyzeModalViewModel", $"Error opening pop-out analyzer: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set seed and trigger analysis programmatically
        /// </summary>
        public void SetSeedAndAnalyze(string seed)
        {
            SeedInput = seed;
            ActiveTab = AnalyzeModalTab.Analyzer;

            if (AnalyzeSeedCommand.CanExecute(null))
            {
                _ = AnalyzeSeedCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Called when deck is selected from the DeckAndStakeSelector
        /// </summary>
        public void OnDeckSelected()
        {
            // Switch to analyzer tab when deck is selected
            ActiveTab = AnalyzeModalTab.Analyzer;
            DebugLogger.Log("AnalyzeModalViewModel", "Deck selected, switching to analyzer tab");
        }

        /// <summary>
        /// Get sprite for shop item
        /// </summary>
        public IImage? GetItemSprite(ShopItemModel item)
        {
            return item.TypeCategory switch
            {
                MotelyItemTypeCategory.Joker => _spriteService.GetJokerImage(item.ItemName),
                MotelyItemTypeCategory.TarotCard => _spriteService.GetTarotImage(item.ItemName),
                MotelyItemTypeCategory.PlanetCard => _spriteService.GetTarotImage(item.ItemName),
                MotelyItemTypeCategory.SpectralCard => _spriteService.GetTarotImage(item.ItemName),
                _ => null,
            };
        }

        /// <summary>
        /// Get sprite for booster pack
        /// </summary>
        public IImage? GetBoosterSprite(BoosterPackModel pack)
        {
            return _spriteService.GetBoosterImage(pack.PackSpriteKey);
        }

        /// <summary>
        /// Get sprite for tag
        /// </summary>
        public IImage? GetTagSprite(TagModel tag)
        {
            return _spriteService.GetTagImage(tag.TagSpriteKey);
        }

        #endregion

        #region Helper Methods

        private void UpdatePlaceholderVisibility()
        {
            ShowPlaceholder = string.IsNullOrWhiteSpace(SeedInput) && Antes.Count == 0;
        }

        #endregion
    }
}
