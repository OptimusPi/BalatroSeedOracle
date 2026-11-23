using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BalatroSeedOracle.Analysis;
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

        // The results the modal was opened from, in on-screen order, for Left/Right paging.
        private string[] _seedList = [];
        private int _seedListIndex;
        private bool _analyzeQueued;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeSeedCommand))]
        [NotifyCanExecuteChangedFor(nameof(FindCommand))]
        private string _seedInput = "";

        /// <summary>The item to find in the seed: a joker, card, voucher, tag or boss name.</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FindCommand))]
        private string _findInput = "";

        [ObservableProperty]
        private int _findMinAnte = 0;

        [ObservableProperty]
        private int _findMaxAnte = MotelyUnitTestAnalyzer.DefaultMaxAnte;

        /// <summary>Inclusive last ante for the full ante breakdown (default 16).</summary>
        [ObservableProperty]
        private int _analyzeMaxAnte = MotelyUnitTestAnalyzer.DefaultMaxAnte;

        /// <summary>Shop cards per ante; 0 = engine defaults.</summary>
        [ObservableProperty]
        private int _analyzeShopSlots = 0;

        [ObservableProperty]
        private string _seedPasteText = "";

        [ObservableProperty]
        private string _loopWatchSummary = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FindCommand))]
        private bool _isFinding;

        /// <summary>What the last find found, or "not found in ..." with how deep it looked.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFindStatus))]
        private string _findStatus = "";

        /// <summary>The engine's one-line reason a find could not run (unknown name, bad seed or range).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFindError))]
        private string _findError = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeSeedCommand))]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSettingsTabActive))]
        [NotifyPropertyChangedFor(nameof(IsAnalyzerTabActive))]
        [NotifyPropertyChangedFor(nameof(SettingsTabVisible))]
        [NotifyPropertyChangedFor(nameof(AnalyzerTabVisible))]
        [NotifyPropertyChangedFor(nameof(TriangleColumn))]
        private AnalyzeModalTab _activeTab = AnalyzeModalTab.Settings;

        [ObservableProperty]
        private bool _showPlaceholder = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedDeck))]
        private int _deckIndex = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedStake))]
        private int _stakeIndex = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasAnalysisResults))]
        [NotifyPropertyChangedFor(nameof(AnalysisHeader))]
        private SeedAnalysisModel? _currentAnalysis;

        public AnalyzeModalViewModel(
            SpriteService spriteService,
            UserProfileService userProfileService
        )
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

        // The pickers count positions; the enums are values. MotelyStake skips 5 (Blue = 4,
        // Purple = 6 … Gold = 8), so casting the position analyzed Gold as Orange and Purple as
        // an undefined stake. Enum.GetValues lists the values in the pickers' order.
        private static readonly MotelyDeck[] Decks = Enum.GetValues<MotelyDeck>();
        private static readonly MotelyStake[] Stakes = Enum.GetValues<MotelyStake>();

        public MotelyDeck SelectedDeck => Decks[Math.Clamp(DeckIndex, 0, Decks.Length - 1)];
        public MotelyStake SelectedStake => Stakes[Math.Clamp(StakeIndex, 0, Stakes.Length - 1)];

        public bool HasAnalysisResults =>
            CurrentAnalysis is not null && string.IsNullOrEmpty(CurrentAnalysis.Error);
        public string AnalysisHeader =>
            CurrentAnalysis is not null
                ? $"Seed: {CurrentAnalysis.Seed} | Deck: {CurrentAnalysis.Deck} | Stake: {CurrentAnalysis.Stake}{SeedListPosition}"
                : "";

        private string SeedListPosition =>
            _seedList.Length > 1 ? $" | {_seedListIndex + 1} of {_seedList.Length} (Left/Right)" : "";

        public bool HasSeedList => _seedList.Length > 1;

        public ObservableCollection<AnteAnalysisModel> Antes { get; }

        /// <summary>Every place the last find saw the item, one row per hit.</summary>
        public ObservableCollection<LocateHitRow> FindRows { get; } = new();

        public ObservableCollection<LoopWatchHit> LoopHits { get; } = new();

        public bool HasLoopHits => LoopHits.Count > 0;

        public bool HasFindStatus => !string.IsNullOrEmpty(FindStatus);
        public bool HasFindError => !string.IsNullOrEmpty(FindError);

        #endregion

        #region Generated Property Changed Methods

        // Dependent display properties and command CanExecute notifications are wired
        // declaratively via [NotifyPropertyChangedFor] / [NotifyCanExecuteChangedFor]
        // on the backing fields above.
        partial void OnSeedInputChanged(string value)
        {
            UpdatePlaceholderVisibility();
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

                DebugLogger.Log(
                    "AnalyzeModalViewModel",
                    $"Starting analysis for seed: {SeedInput}"
                );

                // Clear previous results
                Antes.Clear();
                CurrentAnalysis = null;

                // Perform seed analysis
                var seed = SeedInput.Trim();
                var deck = SelectedDeck;
                var stake = SelectedStake;

                var maxAnte = AnalyzeMaxAnte;
                var shopSlots = AnalyzeShopSlots;
                var analysisData = await Task.Run(() =>
                {
                    var config = new MotelyUnitTestAnalysisConfig(
                        seed,
                        deck,
                        stake,
                        maxAnte,
                        shopSlots
                    );
                    return MotelyUnitTestAnalyzer.Analyze(config);
                });

                // Map Motely analysis to our models (populate fully BEFORE assigning
                // CurrentAnalysis — SeedAnalysisModel is not observable, so the UI only
                // sees data present at assignment time)
                var analysis = new SeedAnalysisModel
                {
                    Seed = seed,
                    Deck = deck,
                    Stake = stake,
                    Error = analysisData.Error,
                };

                LoopHits.Clear();
                if (!string.IsNullOrEmpty(analysisData.Error))
                {
                    DebugLogger.LogError(
                        "AnalyzeModalViewModel",
                        $"Analysis error: {analysisData.Error}"
                    );
                    LoopWatchSummary = "";
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
                            SmallBlindTag = new TagModel
                            {
                                BlindType = "Small Blind",
                                Tag = motelyAnte.SmallBlindTag,
                            },
                            BigBlindTag = new TagModel
                            {
                                BlindType = "Big Blind",
                                Tag = motelyAnte.BigBlindTag,
                            },
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
                            var packModel = new BoosterPackModel
                            {
                                PackType = pack.Type.GetPackType(),
                            };

                            foreach (var item in pack.Items)
                            {
                                packModel.Items.Add(FormatUtils.FormatItem(item.Item));
                            }

                            anteModel.BoosterPacks.Add(packModel);
                        }

                        Antes.Add(anteModel);
                    }

                    foreach (var hit in SeedLoopWatch.Scan(analysisData))
                        LoopHits.Add(hit);
                    LoopWatchSummary = SeedLoopWatch.SummarizeKinds(LoopHits);

                    // Attach ante collection to the analysis for the shared display component
                    analysis.Antes = Antes;

                    DebugLogger.Log(
                        "AnalyzeModalViewModel",
                        $"Analysis completed successfully: {Antes.Count} antes (0–{maxAnte})"
                    );
                }

                OnPropertyChanged(nameof(HasLoopHits));
                CurrentAnalysis = analysis;

                UpdatePlaceholderVisibility();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AnalyzeModalViewModel",
                    $"Error analyzing seed: {ex.Message}"
                );

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
                if (_analyzeQueued)
                {
                    // A held arrow key moved past this seed mid-analysis: show the latest one.
                    _analyzeQueued = false;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => _ = AnalyzeSeedCommand.ExecuteAsync(null));
                }
            }
        }

        private bool CanAnalyzeSeed()
        {
            return !string.IsNullOrWhiteSpace(SeedInput) && !IsAnalyzing;
        }

        /// <summary>Every ante and place the item appears in the seed, for the chosen deck and stake.</summary>
        [RelayCommand(CanExecute = nameof(CanFind))]
        private async Task FindAsync()
        {
            FindRows.Clear();
            FindStatus = "";
            FindError = "";

            var seed = SeedInput.Trim();
            var item = FindInput.Trim();
            var deck = SelectedDeck;
            var stake = SelectedStake;
            int minAnte = FindMinAnte;
            int maxAnte = FindMaxAnte;

            try
            {
                IsFinding = true;
                var result = await Task.Run(() =>
                    BalatroSeedOracle.Analysis.MotelySeedLocator.Locate(seed, deck, stake, item, minAnte, maxAnte)
                );

                foreach (var hit in result.Hits)
                    FindRows.Add(new LocateHitRow(hit.Ante, hit.Where, hit.Item));

                FindStatus =
                    result.Hits.Count == 0
                        ? $"{result.Name}: not found in {result.Depth}"
                        : $"{result.Name}: {result.Hits.Count} {(result.Hits.Count == 1 ? "hit" : "hits")} in {result.Depth}";
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                // Unknown name ("did you mean"), bad seed or bad ante range: the engine's line says which.
                FindError = ex.Message;
            }
            finally
            {
                IsFinding = false;
            }
        }

        private bool CanFind() =>
            !string.IsNullOrWhiteSpace(SeedInput) && !string.IsNullOrWhiteSpace(FindInput) && !IsFinding;

        [RelayCommand]
        private void ClearResults()
        {
            Antes.Clear();
            LoopHits.Clear();
            LoopWatchSummary = "";
            OnPropertyChanged(nameof(HasLoopHits));
            CurrentAnalysis = null;
            SeedInput = "";
            UpdatePlaceholderVisibility();
            DebugLogger.Log("AnalyzeModalViewModel", "Results cleared");
        }

        [RelayCommand]
        private void ApplySeedPaste()
        {
            var (seeds, extras) = SeedPaste.ParseWithMeta(SeedPasteText);
            if (seeds.Count == 0)
                return;

            if (extras.MaxAnte is int ma)
                AnalyzeMaxAnte = ma;

            if (
                extras.Deck is string deckName
                && Enum.TryParse<MotelyDeck>(deckName, ignoreCase: true, out var deck)
            )
            {
                var di = Array.IndexOf(Decks, deck);
                if (di >= 0)
                    DeckIndex = di;
            }

            if (
                extras.Stake is string stakeName
                && Enum.TryParse<MotelyStake>(stakeName, ignoreCase: true, out var stake)
            )
            {
                var si = Array.IndexOf(Stakes, stake);
                if (si >= 0)
                    StakeIndex = si;
            }

            if (seeds.Count == 1)
            {
                SetSeedAndAnalyze(seeds[0]);
                return;
            }

            var idx = extras.SeedIndex ?? 0;
            idx = Math.Clamp(idx, 0, seeds.Count - 1);
            SetSeedsAndAnalyze(seeds[idx], seeds);
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
                var analyzerViewModel = new AnalyzerViewModel { ShopSlots = AnalyzeShopSlots };
                var preset = Array.FindIndex(
                    analyzerViewModel.MaxAntePresets,
                    p => p >= AnalyzeMaxAnte
                );
                analyzerViewModel.MaxAntePresetIndex =
                    preset >= 0 ? preset : analyzerViewModel.MaxAntePresets.Length - 1;
                if (_seedList.Length > 1)
                    analyzerViewModel.SetSeeds(_seedList);
                else
                    analyzerViewModel.AddSeed(seed);
                var analyzerWindow = new Windows.AnalyzerWindow(analyzerViewModel);
                analyzerWindow.Show();

                DebugLogger.Log(
                    "AnalyzeModalViewModel",
                    $"Opened pop-out analyzer window for seed: {seed}"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "AnalyzeModalViewModel",
                    $"Error opening pop-out analyzer: {ex.Message}"
                );
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
        /// Analyze <paramref name="seed"/> and remember <paramref name="seeds"/> (the result list it
        /// came from, in on-screen order) so Left/Right can step through its neighbours.
        /// </summary>
        public void SetSeedsAndAnalyze(string seed, IReadOnlyList<string> seeds)
        {
            var index = -1;
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == seed)
                {
                    index = i;
                    break;
                }
            }
            _seedList = index >= 0 ? [.. seeds] : [seed];
            _seedListIndex = Math.Max(index, 0);
            OnPropertyChanged(nameof(HasSeedList));
            SetSeedAndAnalyze(seed);
        }

        /// <summary>Move to the previous (-1) or next (+1) seed in the result list.</summary>
        public void StepSeed(int delta)
        {
            var next = _seedListIndex + delta;
            if (next < 0 || next >= _seedList.Length)
                return;

            _seedListIndex = next;
            SeedInput = _seedList[next];
            ActiveTab = AnalyzeModalTab.Analyzer;

            if (IsAnalyzing)
            {
                _analyzeQueued = true;
                return;
            }
            _ = AnalyzeSeedCommand.ExecuteAsync(null);
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

    /// <summary>One place a find saw the item: the ante, where in it ("Buffoon Pack (pack 1) card 1"),
    /// and the item as the analyzer prints it, edition and stickers included.</summary>
    public sealed record LocateHitRow(int Ante, string Where, string Item)
    {
        public string AnteText => $"Ante {Ante}";
    }
}
