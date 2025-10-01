using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Motely;
using Motely.Analysis;

namespace BalatroSeedOracle.ViewModels;

public partial class AnalyzerViewModel : ObservableObject
{
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
    private MotelySeedAnalysis? _currentAnalysis;

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
        // Placeholder for Ante 9+ support
        await Task.CompletedTask;
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
        }
        catch (Exception ex)
        {
            CurrentAnalysis = new MotelySeedAnalysis($"Error: {ex.Message}", []);
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    public MotelyAnteAnalysis? GetCurrentAnte()
    {
        if (CurrentAnalysis == null || CurrentAnteIndex < 0 || CurrentAnteIndex >= CurrentAnalysis.Antes.Count)
            return null;

        return CurrentAnalysis.Antes[CurrentAnteIndex];
    }

    // Properties for UI binding
    public string CurrentAnteDisplay => CurrentAnteIndex >= 0 && CurrentAnalysis?.Antes.Count > 0
        ? $"== ANTE {CurrentAnalysis.Antes[CurrentAnteIndex].Ante} =="
        : "";

    public string CurrentBossDisplay => GetCurrentAnte()?.Boss.ToString() ?? "";

    public string CurrentVoucherDisplay => GetCurrentAnte()?.Voucher.ToString() ?? "None";

    public string CurrentSmallTagDisplay => GetCurrentAnte()?.SmallBlindTag.ToString() ?? "";

    public string CurrentBigTagDisplay => GetCurrentAnte()?.BigBlindTag.ToString() ?? "";

    public List<string> CurrentShopItems
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null) return [];

            return ante.ShopQueue
                .Select((item, i) => $"{i + 1}) {FormatUtils.FormatItem(item)}")
                .ToList();
        }
    }

    public List<PackDisplayInfo> CurrentPacks
    {
        get
        {
            var ante = GetCurrentAnte();
            if (ante == null) return [];

            return ante.Packs.Select(pack => new PackDisplayInfo
            {
                Name = FormatUtils.FormatPackName(pack.Type),
                Items = pack.Items.Select(item => FormatUtils.FormatItem(item)).ToList(),
                PackType = pack.Type.GetPackType()
            }).ToList();
        }
    }

    public string AnteNavigationDisplay => CurrentAnalysis != null && CurrentAnalysis.Antes.Count > 0
        ? $"ANTE {(GetCurrentAnte()?.Ante ?? 1)} of {CurrentAnalysis.Antes.Count}"
        : "ANTE 1 of 8";

    partial void OnCurrentAnteIndexChanged(int value)
    {
        // Notify UI of all ante-related property changes
        OnPropertyChanged(nameof(CurrentAnteDisplay));
        OnPropertyChanged(nameof(CurrentBossDisplay));
        OnPropertyChanged(nameof(CurrentVoucherDisplay));
        OnPropertyChanged(nameof(CurrentSmallTagDisplay));
        OnPropertyChanged(nameof(CurrentBigTagDisplay));
        OnPropertyChanged(nameof(CurrentShopItems));
        OnPropertyChanged(nameof(CurrentPacks));
        OnPropertyChanged(nameof(AnteNavigationDisplay));
    }

    partial void OnCurrentAnalysisChanged(MotelySeedAnalysis? value)
    {
        // Notify UI of all ante-related property changes when analysis updates
        OnPropertyChanged(nameof(CurrentAnteDisplay));
        OnPropertyChanged(nameof(CurrentBossDisplay));
        OnPropertyChanged(nameof(CurrentVoucherDisplay));
        OnPropertyChanged(nameof(CurrentSmallTagDisplay));
        OnPropertyChanged(nameof(CurrentBigTagDisplay));
        OnPropertyChanged(nameof(CurrentShopItems));
        OnPropertyChanged(nameof(CurrentPacks));
        OnPropertyChanged(nameof(AnteNavigationDisplay));
    }
}

public class PackDisplayInfo
{
    public required string Name { get; init; }
    public required List<string> Items { get; init; }
    public required MotelyBoosterPackType PackType { get; init; }

    public string PackColor => PackType switch
    {
        MotelyBoosterPackType.Arcana => "#9370DB",
        MotelyBoosterPackType.Celestial => "#4169E1",
        MotelyBoosterPackType.Spectral => "#20B2AA",
        MotelyBoosterPackType.Buffoon => "#FF6347",
        MotelyBoosterPackType.Standard => "#4682B4",
        _ => "#FFFFFF"
    };
}
