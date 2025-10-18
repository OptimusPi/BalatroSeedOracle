using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
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

    // Preview image for playing cards (composited)
    [ObservableProperty]
    private IImage? _previewImage;

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
    private bool _sourcesVisible;

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

    // Lists for UI bindings
    [ObservableProperty]
    private List<string> _ranks = new();

    [ObservableProperty]
    private List<string> _suits = new();

    [ObservableProperty]
    private List<string> _enhancements = new();

    // Edition images for selection UI
    [ObservableProperty]
    private Dictionary<string, IImage> _editionImages = new();

    // Sources and advanced options
    [ObservableProperty]
    private string _selectedSource = ""; // empty => Any Source

    [ObservableProperty]
    private bool _skipBlindTags;

    [ObservableProperty]
    private bool _isMegaArcana;

    // Comma-separated slot inputs for simple UI handling
    [ObservableProperty]
    private string _shopSlotsText = "";

    [ObservableProperty]
    private string _packSlotsText = "";

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
        SourcesVisible = config.ItemType == "Joker" || config.ItemType == "SoulJoker" ||
                         config.ItemType == "Tarot" || config.ItemType == "Spectral" ||
                         config.ItemType == "Planet" || config.ItemType == "PlayingCard";

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

        InitLists();
        UpdatePreview();

        // Initialize sources from config
        if (config.Sources is IEnumerable<string> srcEnum)
        {
            SelectedSource = srcEnum.FirstOrDefault() ?? "";
        }
        else if (config.Sources is string[] srcArray && srcArray.Length > 0)
        {
            SelectedSource = srcArray[0] ?? "";
        }
        else if (config.Sources is List<string> srcList && srcList.Count > 0)
        {
            SelectedSource = srcList[0] ?? "";
        }
        else
        {
            SelectedSource = "";
        }

        SkipBlindTags = config.SkipBlindTags;
        IsMegaArcana = config.IsMegaArcana;

        ShopSlotsText = config.ShopSlots != null && config.ShopSlots.Count > 0
            ? string.Join(",", config.ShopSlots)
            : "";
        PackSlotsText = config.PackSlots != null && config.PackSlots.Count > 0
            ? string.Join(",", config.PackSlots)
            : "";
    }

    private void InitLists()
    {
        // Populate ranks and suits from metadata layout
        Ranks = new List<string> { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };
        Suits = new List<string> { "Hearts", "Clubs", "Diamonds", "Spades" };

        // Include "None" option for enhancements
        Enhancements = new List<string> { "None", "Bonus", "Mult", "Wild", "Lucky", "Glass", "Steel", "Stone", "Gold" };

        // Edition images for selector
        var ss = SpriteService.Instance;
        var editionMap = new Dictionary<string, string>
        {
            { "Normal", "Normal" },
            { "Foil", "Foil" },
            { "Holographic", "Holographic" },
            { "Polychrome", "Polychrome" },
            { "Negative", "Negative" },
        };
        var imgs = new Dictionary<string, IImage>();
        foreach (var kvp in editionMap)
        {
            var img = ss.GetEditionImage(kvp.Value);
            if (img != null)
            {
                imgs[kvp.Key] = img;
            }
        }
        EditionImages = imgs;
    }

    private void UpdatePreview()
    {
        // Only render for playing cards
        if (!RankVisible)
        {
            PreviewImage = ItemImage;
            return;
        }

        var ss = SpriteService.Instance;

        // Base card: enhancement or blank
        IImage? baseCard = null;
        if (!string.IsNullOrWhiteSpace(SelectedEnhancement) && !string.Equals(SelectedEnhancement, "None", StringComparison.OrdinalIgnoreCase))
        {
            baseCard = ss.GetEnhancementImage(SelectedEnhancement);
        }
        baseCard ??= ss.GetSpecialImage("BlankCard");

        // Pattern overlay (suit/rank)
        var pattern = ss.GetPlayingCardImage(SelectedSuit, SelectedRank, SelectedEnhancement);

        if (baseCard == null && pattern == null)
        {
            PreviewImage = null;
            return;
        }

        // Composite using render target (full size, UI scales down)
        var width = 142;
        var height = 190;
        var renderTarget = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using (var ctx = renderTarget.CreateDrawingContext())
        {
            if (baseCard != null)
            {
                ctx.DrawImage(baseCard, new Rect(0, 0, width, height));
            }
            if (pattern != null)
            {
                ctx.DrawImage(pattern, new Rect(0, 0, width, height));
            }

            // Optional: overlay seal if present
            if (!string.IsNullOrWhiteSpace(SelectedSeal) && !string.Equals(SelectedSeal, "None", StringComparison.OrdinalIgnoreCase))
            {
                var sealImg = ss.GetSealImage(SelectedSeal, width, height);
                if (sealImg != null)
                {
                    ctx.DrawImage(sealImg, new Rect(0, 0, width, height));
                }
            }
        }
        PreviewImage = renderTarget;
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

        // Apply sources if supported
        if (SourcesVisible)
        {
            // Normalize selected source into an array; empty string means any source, omit in that case
            if (!string.IsNullOrEmpty(SelectedSource))
            {
                config.Sources = new[] { SelectedSource };
            }

            // Advanced options
            config.SkipBlindTags = SkipBlindTags;
            config.IsMegaArcana = IsMegaArcana;

            // Parse comma-separated slot inputs
            var shopSlots = ParseSlots(ShopSlotsText);
            var packSlots = ParseSlots(PackSlotsText);
            config.ShopSlots = shopSlots.Count > 0 ? shopSlots : null;
            config.PackSlots = packSlots.Count > 0 ? packSlots : null;
        }

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

    // Update preview when selections change
    partial void OnSelectedRankChanged(string value) => UpdatePreview();
    partial void OnSelectedSuitChanged(string value) => UpdatePreview();
    partial void OnSelectedEnhancementChanged(string value) => UpdatePreview();
    partial void OnSelectedSealChanged(string value) => UpdatePreview();

    private static List<int> ParseSlots(string text)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out var value))
            {
                result.Add(value);
            }
        }
        return result;
    }
}