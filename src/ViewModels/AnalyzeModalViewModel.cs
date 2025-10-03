using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using Motely;
using Avalonia.Media;
using DebugLogger = BalatroSeedOracle.Helpers.DebugLogger;

namespace BalatroSeedOracle.ViewModels
{
    public class AnalyzeModalViewModel : BaseViewModel
    {
        private readonly SpriteService _spriteService;
        private readonly UserProfileService _userProfileService;

        private string _seedInput = "";
        private bool _isAnalyzing = false;
        private AnalyzeModalTab _activeTab = AnalyzeModalTab.Settings;
        private bool _showPlaceholder = true;
        private int _deckIndex = 0;
        private int _stakeIndex = 0;
        private SeedAnalysisModel? _currentAnalysis;

        public AnalyzeModalViewModel(SpriteService spriteService, UserProfileService userProfileService)
        {
            _spriteService = spriteService;
            _userProfileService = userProfileService;

            Antes = new ObservableCollection<AnteAnalysisModel>();

            // Initialize commands
            AnalyzeSeedCommand = new AsyncRelayCommand(AnalyzeSeedAsync, CanAnalyzeSeed);
            ClearResultsCommand = new RelayCommand(ClearResults);
            SwitchToSettingsTabCommand = new RelayCommand(() => ActiveTab = AnalyzeModalTab.Settings);
            SwitchToAnalyzerTabCommand = new RelayCommand(() => ActiveTab = AnalyzeModalTab.Analyzer);
            PopOutAnalyzerCommand = new RelayCommand(PopOutAnalyzer);
        }

        #region Properties

        public string SeedInput
        {
            get => _seedInput;
            set
            {
                if (SetProperty(ref _seedInput, value))
                {
                    ((AsyncRelayCommand)AnalyzeSeedCommand).NotifyCanExecuteChanged();
                    UpdatePlaceholderVisibility();
                }
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                if (SetProperty(ref _isAnalyzing, value))
                {
                    ((AsyncRelayCommand)AnalyzeSeedCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public AnalyzeModalTab ActiveTab
        {
            get => _activeTab;
            set
            {
                if (SetProperty(ref _activeTab, value))
                {
                    OnPropertyChanged(nameof(IsSettingsTabActive));
                    OnPropertyChanged(nameof(IsAnalyzerTabActive));
                    OnPropertyChanged(nameof(SettingsTabVisible));
                    OnPropertyChanged(nameof(AnalyzerTabVisible));
                    OnPropertyChanged(nameof(TriangleColumn));
                }
            }
        }

        public bool IsSettingsTabActive => ActiveTab == AnalyzeModalTab.Settings;
        public bool IsAnalyzerTabActive => ActiveTab == AnalyzeModalTab.Analyzer;
        public bool SettingsTabVisible => ActiveTab == AnalyzeModalTab.Settings;
        public bool AnalyzerTabVisible => ActiveTab == AnalyzeModalTab.Analyzer;
        public int TriangleColumn => ActiveTab == AnalyzeModalTab.Settings ? 0 : 1;

        public bool ShowPlaceholder
        {
            get => _showPlaceholder;
            set => SetProperty(ref _showPlaceholder, value);
        }

        public int DeckIndex
        {
            get => _deckIndex;
            set
            {
                if (SetProperty(ref _deckIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedDeck));
                }
            }
        }

        public int StakeIndex
        {
            get => _stakeIndex;
            set
            {
                if (SetProperty(ref _stakeIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedStake));
                }
            }
        }

        public MotelyDeck SelectedDeck => (MotelyDeck)DeckIndex;
        public MotelyStake SelectedStake => (MotelyStake)StakeIndex;

        public SeedAnalysisModel? CurrentAnalysis
        {
            get => _currentAnalysis;
            private set
            {
                if (SetProperty(ref _currentAnalysis, value))
                {
                    OnPropertyChanged(nameof(HasAnalysisResults));
                    OnPropertyChanged(nameof(AnalysisHeader));
                }
            }
        }

        public bool HasAnalysisResults => CurrentAnalysis != null && !string.IsNullOrEmpty(CurrentAnalysis.Error) == false;
        public string AnalysisHeader => CurrentAnalysis != null
            ? $"Seed: {CurrentAnalysis.Seed} | Deck: {CurrentAnalysis.Deck} | Stake: {CurrentAnalysis.Stake}"
            : "";

        public ObservableCollection<AnteAnalysisModel> Antes { get; }

        #endregion

        #region Commands

        public ICommand AnalyzeSeedCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand SwitchToSettingsTabCommand { get; }
        public ICommand SwitchToAnalyzerTabCommand { get; }
        public ICommand PopOutAnalyzerCommand { get; }

        #endregion

        #region Command Implementations

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
                    Error = analysisData.Error
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
                            Voucher = motelyAnte.Voucher,
                            SmallBlindTag = new TagModel
                            {
                                BlindType = "Small Blind",
                                Tag = motelyAnte.SmallBlindTag
                            },
                            BigBlindTag = new TagModel
                            {
                                BlindType = "Big Blind",
                                Tag = motelyAnte.BigBlindTag
                            }
                        };

                        // Convert shop items
                        foreach (var shopItem in motelyAnte.ShopQueue)
                        {
                            anteModel.ShopItems.Add(new ShopItemModel
                            {
                                TypeCategory = shopItem.TypeCategory,
                                ItemValue = shopItem.Value,
                                Edition = shopItem.Edition
                            });
                        }

                        // Convert booster packs
                        foreach (var pack in motelyAnte.Packs)
                        {
                            var packModel = new BoosterPackModel
                            {
                                PackType = (MotelyBoosterPackType)pack.Type
                            };

                            foreach (var item in pack.Items)
                            {
                                packModel.Items.Add(item.ToString());
                            }

                            anteModel.BoosterPacks.Add(packModel);
                        }

                        Antes.Add(anteModel);
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
                    Error = $"Failed to analyze seed: {ex.Message}"
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

        private void ClearResults()
        {
            Antes.Clear();
            CurrentAnalysis = null;
            SeedInput = "";
            UpdatePlaceholderVisibility();
            DebugLogger.Log("AnalyzeModalViewModel", "Results cleared");
        }

        private void PopOutAnalyzer()
        {
            try
            {
                var seed = SeedInput ?? "";
                var analyzerWindow = new Windows.AnalyzerWindow(seed);
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
                _ = AnalyzeSeedAsync();
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
                _ => null
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