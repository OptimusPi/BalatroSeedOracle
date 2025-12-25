using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely;
using Motely.Analysis;

namespace BalatroSeedOracle.ViewModels;

public partial class AnalyzerViewModel : ObservableObject
{
    public event EventHandler<string>? CopyToClipboardRequested;
    [ObservableProperty]
    private ObservableCollection<string> _seedList = [];

    [ObservableProperty]
    private int _currentIndex = 0;

    [ObservableProperty]
    private string _currentSeed = string.Empty;

    [ObservableProperty]
    private MotelyDeck _selectedDeck = MotelyDeck.Red;

    [ObservableProperty]
    private MotelyStake _selectedStake = MotelyStake.White;

    [ObservableProperty]
    private int _selectedDeckIndex = 0;

    [ObservableProperty]
    private int _selectedStakeIndex = 0;

    // Display values for spinners
    public string[] DeckDisplayValues { get; } = BalatroData.Decks.Values.ToArray();
    public string[] StakeDisplayValues { get; } =
    [
        "White Stake",
        "Red Stake",
        "Green Stake",
        "Black Stake",
        "Blue Stake",
        "Purple Stake",
        "Orange Stake",
        "Gold Stake",
    ];

    [ObservableProperty]
    private MotelySeedAnalysis? _currentAnalysis;

    [ObservableProperty]
    private SeedAnalysisModel? _displayAnalysis;

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private int _currentAnteIndex = 0; // Which ante we're viewing (0 = Ante 1)

    [ObservableProperty]
    private string _resultCounter = string.Empty;

    public int TotalResults => SeedList.Count;

    public bool HasPreviousResult => CurrentIndex > 0;
    public bool HasNextResult => CurrentIndex < TotalResults - 1;

    partial void OnCurrentIndexChanged(int value)
    {
        UpdateResultCounter();
        LoadCurrentSeed();
    }

    partial void OnSeedListChanged(ObservableCollection<string> value)
    {
        UpdateResultCounter();
        if (value.Count > 0 && CurrentIndex < value.Count)
        {
            LoadCurrentSeed();
        }
    }

    partial void OnSelectedDeckChanged(MotelyDeck value)
    {
        _ = AnalyzeCurrentSeedAsync();
    }

    partial void OnSelectedStakeChanged(MotelyStake value)
    {
        _ = AnalyzeCurrentSeedAsync();
    }

    partial void OnSelectedDeckIndexChanged(int value)
    {
        if (value >= 0 && value < DeckDisplayValues.Length)
        {
            var deckName = DeckDisplayValues[value].Replace(" Deck", "");
            if (Enum.TryParse<MotelyDeck>(deckName, out var deck))
            {
                SelectedDeck = deck;
            }
        }
    }

    partial void OnSelectedStakeIndexChanged(int value)
    {
        if (value >= 0 && value < StakeDisplayValues.Length)
        {
            var stakeName = StakeDisplayValues[value].Replace(" Stake", "");
            if (Enum.TryParse<MotelyStake>(stakeName, out var stake))
            {
                SelectedStake = stake;
            }
        }
    }

    private void UpdateResultCounter()
    {
        if (TotalResults == 0)
        {
            ResultCounter = "No results";
        }
        else
        {
            ResultCounter = $"Viewing Result {CurrentIndex + 1} of {TotalResults}";
        }

        OnPropertyChanged(nameof(HasPreviousResult));
        OnPropertyChanged(nameof(HasNextResult));
    }

    private void LoadCurrentSeed()
    {
        if (CurrentIndex >= 0 && CurrentIndex < SeedList.Count)
        {
            CurrentSeed = SeedList[CurrentIndex];
            CurrentAnteIndex = 0; // Reset to Ante 1 when changing seeds
            _ = AnalyzeCurrentSeedAsync();
        }
    }

    [RelayCommand]
    private void PreviousResult()
    {
        if (HasPreviousResult)
        {
            CurrentIndex--;
        }
    }

    [RelayCommand]
    private void NextResult()
    {
        if (HasNextResult)
        {
            CurrentIndex++;
        }
    }

    [RelayCommand]
    private void ScrollUpAnte()
    {
        if (CurrentAnteIndex > 0)
        {
            CurrentAnteIndex--;
        }
    }

    [RelayCommand]
    private void ScrollDownAnte()
    {
        if (CurrentAnalysis != null && CurrentAnteIndex < CurrentAnalysis.Antes.Count - 1)
        {
            CurrentAnteIndex++;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAntesAsync()
    {
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CopyBlueprintUrlAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentSeed))
            return;

        try
        {
            // Format: https://miaklwalker.github.io/Blueprint/?seed={SEED}&deck={DECK}+Deck&antes={MAX_ANTE}&stake={STAKE}+Stake
            var deckName = SelectedDeck.ToString();
            var stakeName = SelectedStake.ToString();
            var maxAnte = CurrentAnalysis?.Antes.Count ?? 8;

            var url =
                $"https://miaklwalker.github.io/Blueprint/?seed={CurrentSeed}&deck={deckName}+Deck&antes={maxAnte}&stake={stakeName}+Stake";

            CopyToClipboardRequested?.Invoke(this, url);
            Helpers.DebugLogger.Log(
                "AnalyzerViewModel",
                $"Copied Blueprint URL to clipboard: {url}"
            );
        }
        catch (Exception ex)
        {
            Helpers.DebugLogger.LogError(
                "AnalyzerViewModel",
                $"Failed to copy Blueprint URL: {ex.Message}"
            );
        }
    }

    public void SetSeeds(IEnumerable<string> seeds)
    {
        SeedList = new ObservableCollection<string>(seeds);
        CurrentIndex = 0;
        if (SeedList.Count > 0)
        {
            LoadCurrentSeed();
        }
    }

    public void AddSeed(string seed)
    {
        if (!string.IsNullOrWhiteSpace(seed) && !SeedList.Contains(seed))
        {
            SeedList.Add(seed);
            if (SeedList.Count == 1)
            {
                CurrentIndex = 0;
                LoadCurrentSeed();
            }
        }
    }

    private async Task AnalyzeCurrentSeedAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentSeed))
        {
            CurrentAnalysis = null;
            return;
        }

        IsAnalyzing = true;

        try
        {
            // Run analyzer on background thread
            var analysis = await Task.Run(() =>
            {
                var config = new MotelySeedAnalysisConfig(CurrentSeed, SelectedDeck, SelectedStake);
                return MotelySeedAnalyzer.Analyze(config);
            });

            CurrentAnalysis = analysis;
            // Update mapped display analysis for shared component
            UpdateDisplayAnalysis();
        }
        catch (Exception ex)
        {
            CurrentAnalysis = new MotelySeedAnalysis($"Error: {ex.Message}", []);
            UpdateDisplayAnalysis();
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    public MotelyAnteAnalysis? GetCurrentAnte()
    {
        if (
            CurrentAnalysis == null
            || CurrentAnteIndex < 0
            || CurrentAnteIndex >= CurrentAnalysis.Antes.Count
        )
            return null;

        return CurrentAnalysis.Antes[CurrentAnteIndex];
    }

    // Properties for UI binding
    public string CurrentAnteDisplay =>
        CurrentAnteIndex >= 0 && CurrentAnalysis?.Antes.Count > 0
            ? $"== ANTE {CurrentAnalysis.Antes[CurrentAnteIndex].Ante} =="
            : "";

    public string CurrentBossDisplay => GetCurrentAnte()?.Boss.ToString() ?? "";

    public MotelyBossBlind? CurrentBoss => GetCurrentAnte()?.Boss;

    public string CurrentVoucherDisplay => GetCurrentAnte()?.Voucher.ToString() ?? "None";

    public MotelyVoucher? CurrentVoucher => GetCurrentAnte()?.Voucher;

    public string CurrentSmallTagDisplay => GetCurrentAnte()?.SmallBlindTag.ToString() ?? "";

    public string CurrentBigTagDisplay => GetCurrentAnte()?.BigBlindTag.ToString() ?? "";

    public List<string> CurrentShopItems
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null)
                return [];

            return ante
                .ShopQueue.Select((item, i) => $"{i + 1}) {FormatUtils.FormatItem(item)}")
                .ToList();
        }
    }

    public ObservableCollection<ShopItemViewModel> ShopItems
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null)
                return [];

            var items = new ObservableCollection<ShopItemViewModel>();
            for (int i = 0; i < ante.ShopQueue.Count; i++)
            {
                var item = ante.ShopQueue[i];
                items.Add(
                    new ShopItemViewModel
                    {
                        Index = i + 1,
                        ItemType = item.Type.ToString(),
                        DisplayName = FormatUtils.FormatItem(item),
                    }
                );
            }
            return items;
        }
    }

    public List<MotelyItem> CurrentShopItemsRaw
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null)
                return [];
            return ante.ShopQueue.ToList();
        }
    }

    public ObservableCollection<PackViewModel> BoosterPacks
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null)
                return [];

            var packs = new ObservableCollection<PackViewModel>();
            foreach (var pack in ante.Packs)
            {
                var packItems = new ObservableCollection<ShopItemViewModel>();
                for (int i = 0; i < pack.Items.Count; i++)
                {
                    var item = pack.Items[i];
                    packItems.Add(
                        new ShopItemViewModel
                        {
                            Index = i + 1,
                            ItemType = item.Type.ToString(),
                            DisplayName = FormatUtils.FormatItem(item),
                        }
                    );
                }

                packs.Add(
                    new PackViewModel
                    {
                        Name = FormatUtils.FormatPackName(pack.Type),
                        PackColor = pack.Type.GetPackType() switch
                        {
                            MotelyBoosterPackType.Arcana => "#9370DB",
                            MotelyBoosterPackType.Celestial => "#4169E1",
                            MotelyBoosterPackType.Spectral => "#20B2AA",
                            MotelyBoosterPackType.Buffoon => "#FF6347",
                            MotelyBoosterPackType.Standard => "#4682B4",
                            _ => "#FFFFFF",
                        },
                        Items = packItems,
                    }
                );
            }
            return packs;
        }
    }

    public List<PackDisplayInfo> CurrentPacks
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null)
                return [];

            return ante
                .Packs.Select(pack => new PackDisplayInfo
                {
                    Name = FormatUtils.FormatPackName(pack.Type),
                    Items = pack.Items.Select(item => FormatUtils.FormatItem(item)).ToList(),
                    RawItems = pack.Items.ToList(),
                    PackType = pack.Type.GetPackType(),
                })
                .ToList();
        }
    }

    public string AnteNavigationDisplay =>
        CurrentAnalysis != null && CurrentAnalysis.Antes.Count > 0
            ? $"ANTE {(GetCurrentAnte()?.Ante ?? 1)} of {CurrentAnalysis.Antes.Count}"
            : "ANTE 1 of 8";

    partial void OnCurrentAnteIndexChanged(int value)
    {
        // Notify UI of all ante-related property changes
        OnPropertyChanged(nameof(CurrentAnteDisplay));
        OnPropertyChanged(nameof(CurrentBossDisplay));
        OnPropertyChanged(nameof(CurrentBoss));
        OnPropertyChanged(nameof(CurrentVoucherDisplay));
        OnPropertyChanged(nameof(CurrentVoucher));
        OnPropertyChanged(nameof(CurrentSmallTagDisplay));
        OnPropertyChanged(nameof(CurrentBigTagDisplay));
        OnPropertyChanged(nameof(CurrentShopItems));
        OnPropertyChanged(nameof(CurrentShopItemsRaw));
        OnPropertyChanged(nameof(CurrentPacks));
        OnPropertyChanged(nameof(ShopItems));
        OnPropertyChanged(nameof(BoosterPacks));
        OnPropertyChanged(nameof(AnteNavigationDisplay));
    }

    partial void OnCurrentAnalysisChanged(MotelySeedAnalysis? value)
    {
        // Notify UI of all ante-related property changes when analysis updates
        OnPropertyChanged(nameof(CurrentAnteDisplay));
        OnPropertyChanged(nameof(CurrentBossDisplay));
        OnPropertyChanged(nameof(CurrentBoss));
        OnPropertyChanged(nameof(CurrentVoucherDisplay));
        OnPropertyChanged(nameof(CurrentVoucher));
        OnPropertyChanged(nameof(CurrentSmallTagDisplay));
        OnPropertyChanged(nameof(CurrentBigTagDisplay));
        OnPropertyChanged(nameof(CurrentShopItems));
        OnPropertyChanged(nameof(CurrentShopItemsRaw));
        OnPropertyChanged(nameof(CurrentPacks));
        OnPropertyChanged(nameof(ShopItems));
        OnPropertyChanged(nameof(BoosterPacks));
        OnPropertyChanged(nameof(AnteNavigationDisplay));

        // Also update the shared display mapping
        UpdateDisplayAnalysis();
    }

    private void UpdateDisplayAnalysis()
    {
        if (CurrentAnalysis == null)
        {
            DisplayAnalysis = null;
            return;
        }

        var mapped = new SeedAnalysisModel
        {
            Seed = CurrentSeed,
            Deck = SelectedDeck,
            Stake = SelectedStake,
            Error = CurrentAnalysis.Error,
        };

        foreach (var ante in CurrentAnalysis.Antes)
        {
            var anteModel = new AnteAnalysisModel
            {
                AnteNumber = ante.Ante,
                Boss = ante.Boss,
                Voucher = ante.Voucher,
                SmallBlindTag = new TagModel
                {
                    BlindType = "Small Blind",
                    Tag = ante.SmallBlindTag,
                },
                BigBlindTag = new TagModel { BlindType = "Big Blind", Tag = ante.BigBlindTag },
            };

            foreach (var item in ante.ShopQueue)
            {
                anteModel.ShopItems.Add(
                    new ShopItemModel
                    {
                        TypeCategory = item.TypeCategory,
                        ItemType = item.Type,
                        Edition = item.Edition,
                    }
                );
            }

            foreach (var pack in ante.Packs)
            {
                var packModel = new BoosterPackModel { PackType = pack.Type.GetPackType() };

                foreach (var packItem in pack.Items)
                {
                    packModel.Items.Add(packItem.ToString());
                }

                anteModel.BoosterPacks.Add(packModel);
            }

            mapped.Antes.Add(anteModel);
        }

        DisplayAnalysis = mapped;
    }
}

// ViewModels for MVVM data binding
public class ShopItemViewModel
{
    public int Index { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class PackViewModel
{
    public string Name { get; set; } = string.Empty;
    public string PackColor { get; set; } = "#FFFFFF";
    public ObservableCollection<ShopItemViewModel> Items { get; set; } = [];
}

public class PackDisplayInfo
{
    public required string Name { get; init; }
    public required List<string> Items { get; init; }
    public required List<MotelyItem> RawItems { get; init; }
    public required MotelyBoosterPackType PackType { get; init; }

    public string PackColor =>
        PackType switch
        {
            MotelyBoosterPackType.Arcana => "#9370DB",
            MotelyBoosterPackType.Celestial => "#4169E1",
            MotelyBoosterPackType.Spectral => "#20B2AA",
            MotelyBoosterPackType.Buffoon => "#FF6347",
            MotelyBoosterPackType.Standard => "#4682B4",
            _ => "#FFFFFF",
        };
}
