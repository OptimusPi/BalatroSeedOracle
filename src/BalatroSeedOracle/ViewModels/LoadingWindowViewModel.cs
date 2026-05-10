using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalatroSeedOracle.ViewModels;

public partial class LoadingWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _loadingText = "Initializing...";

    [ObservableProperty]
    private double _progress = 0.0;

    [ObservableProperty]
    private string _currentCategory = "";

    [ObservableProperty]
    private int _currentCount = 0;

    [ObservableProperty]
    private int _totalCount = 0;

    public void UpdateProgress(string category, int current, int total)
    {
        CurrentCategory = category;
        CurrentCount = current;
        TotalCount = total;

        if (total > 0)
        {
            Progress = (double)current / total * 100.0;
        }

        LoadingText = $"Loading {category}... {current}/{total}";
    }

    public void SetMessage(string message)
    {
        LoadingText = message;
    }
}
