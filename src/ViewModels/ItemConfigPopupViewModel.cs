using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels;

public partial class ItemConfigPopupViewModel : ObservableObject
{
    public delegate void ConfigAppliedEventHandler(ItemConfig config);
    public event ConfigAppliedEventHandler? ConfigApplied;

    public delegate void CancelledEventHandler();
    public event CancelledEventHandler? Cancelled;

    public delegate void DeleteRequestedEventHandler();
    public event DeleteRequestedEventHandler? DeleteRequested;

    [ObservableProperty]
    private string _itemKey = "";

    [ObservableProperty]
    private string _itemName = "";

    [ObservableProperty]
    private IImage? _itemImage;

    [ObservableProperty]
    private bool _antesVisible;

    [ObservableProperty]
    private bool _editionVisible;

    [ObservableProperty]
    private bool _sealVisible;

    [ObservableProperty]
    private bool _enhancementVisible;

    [ObservableProperty]
    private bool _rankVisible;

    [ObservableProperty]
    private bool _suitVisible;

    [ObservableProperty]
    private List<bool> _selectedAntes = Enumerable.Repeat(true, 9).ToList();

    [ObservableProperty]
    private string _selectedEdition = "none";

    [ObservableProperty]
    private string _selectedSeal = "None";

    [ObservableProperty]
    private string _selectedEnhancement = "None";

    [ObservableProperty]
    private string _selectedRank = "Ace";

    [ObservableProperty]
    private string _selectedSuit = "Spades";

    public ItemConfigPopupViewModel(ItemConfig config)
    {
        Configure(config);
    }

    public void Configure(ItemConfig config)
    {
        ItemKey = config.ItemKey;
        // ItemName will be set from the view
        // ItemImage will be set from the view

        // Determine visibility based on item type or other properties
        // This is a placeholder, the actual logic will be more complex
        AntesVisible = true;
        EditionVisible = config.ItemType == "Joker";
        SealVisible = config.ItemType == "PlayingCard";
        EnhancementVisible = config.ItemType == "PlayingCard";
        RankVisible = config.ItemType == "PlayingCard";
        SuitVisible = config.ItemType == "PlayingCard";

        if (config.Antes != null)
        {
            var newAntes = Enumerable.Repeat(false, 9).ToList();
            foreach (var ante in config.Antes)
            {
                if (ante >= 0 && ante < 9)
                {
                    newAntes[ante] = true;
                }
            }
            SelectedAntes = newAntes;
        }

        SelectedEdition = config.Edition;
        SelectedSeal = config.Seal;
        SelectedEnhancement = config.Enhancement;
        SelectedRank = config.Rank ?? "Ace";
        SelectedSuit = config.Suit ?? "Spades";
    }

    [RelayCommand]
    private void Apply()
    {
        var antes = new List<int>();
        for (int i = 0; i < SelectedAntes.Count; i++)
        {
            if (SelectedAntes[i])
            {
                antes.Add(i);
            }
        }

        var config = new ItemConfig
        {
            ItemKey = ItemKey,
            Antes = antes,
            Edition = SelectedEdition,
            Seal = SelectedSeal,
            Enhancement = SelectedEnhancement,
            Rank = SelectedRank,
            Suit = SelectedSuit
        };

        ConfigApplied?.Invoke(config);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke();
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteRequested?.Invoke();
    }
}