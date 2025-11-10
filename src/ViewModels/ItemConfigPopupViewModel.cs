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
    private string _itemType = "";

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

    // Individual ante properties for proper MVVM binding (can't use indexers in XAML)
    [ObservableProperty]
    private bool _ante0 = true;

    [ObservableProperty]
    private bool _ante1 = true;

    [ObservableProperty]
    private bool _ante2 = true;

    [ObservableProperty]
    private bool _ante3 = true;

    [ObservableProperty]
    private bool _ante4 = true;

    [ObservableProperty]
    private bool _ante5 = true;

    [ObservableProperty]
    private bool _ante6 = true;

    [ObservableProperty]
    private bool _ante7 = true;

    [ObservableProperty]
    private bool _ante8 = true;

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
        ItemType = config.ItemType;
        // ItemName will be set from the view
        // ItemImage will be set from the view

        // Determine visibility based on item type
        AntesVisible = true;
        EditionVisible = config.ItemType == "Joker" || config.ItemType == "StandardCard";
        SealVisible = config.ItemType == "PlayingCard" || config.ItemType == "StandardCard";
        EnhancementVisible = config.ItemType == "PlayingCard" || config.ItemType == "StandardCard";
        RankVisible = config.ItemType == "PlayingCard" || config.ItemType == "StandardCard";
        SuitVisible = config.ItemType == "PlayingCard" || config.ItemType == "StandardCard";
        SourcesVisible =
            config.ItemType == "Joker"
            || config.ItemType == "SoulJoker"
            || config.ItemType == "Tarot"
            || config.ItemType == "Spectral"
            || config.ItemType == "Planet"
            || config.ItemType == "PlayingCard"
            || config.ItemType == "StandardCard";

        if (config.Antes != null)
        {
            // Reset all to false first
            Ante0 = Ante1 = Ante2 = Ante3 = Ante4 = Ante5 = Ante6 = Ante7 = Ante8 = false;

            // Set specified antes to true
            foreach (var ante in config.Antes)
            {
                switch (ante)
                {
                    case 0:
                        Ante0 = true;
                        break;
                    case 1:
                        Ante1 = true;
                        break;
                    case 2:
                        Ante2 = true;
                        break;
                    case 3:
                        Ante3 = true;
                        break;
                    case 4:
                        Ante4 = true;
                        break;
                    case 5:
                        Ante5 = true;
                        break;
                    case 6:
                        Ante6 = true;
                        break;
                    case 7:
                        Ante7 = true;
                        break;
                    case 8:
                        Ante8 = true;
                        break;
                }
            }
        }

        SelectedEdition = config.Edition ?? "none";
        SelectedSeal = config.Seal ?? "None";
        SelectedEnhancement = config.Enhancement ?? "None";
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

        ShopSlotsText =
            config.ShopSlots != null && config.ShopSlots.Count > 0
                ? string.Join(",", config.ShopSlots)
                : "";
        PackSlotsText =
            config.PackSlots != null && config.PackSlots.Count > 0
                ? string.Join(",", config.PackSlots)
                : "";
    }

    private void InitLists()
    {
        // Populate ranks and suits from metadata layout
        Ranks = new List<string>
        {
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "Jack",
            "Queen",
            "King",
            "Ace",
        };
        Suits = new List<string> { "Hearts", "Clubs", "Diamonds", "Spades" };

        // Include "None" option for enhancements
        Enhancements = new List<string>
        {
            "None",
            "Bonus",
            "Mult",
            "Wild",
            "Lucky",
            "Glass",
            "Steel",
            "Stone",
            "Gold",
        };

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
        if (
            !string.IsNullOrWhiteSpace(SelectedEnhancement)
            && !string.Equals(SelectedEnhancement, "None", StringComparison.OrdinalIgnoreCase)
        )
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
            if (
                !string.IsNullOrWhiteSpace(SelectedSeal)
                && !string.Equals(SelectedSeal, "None", StringComparison.OrdinalIgnoreCase)
            )
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
        if (Ante0)
            antes.Add(0);
        if (Ante1)
            antes.Add(1);
        if (Ante2)
            antes.Add(2);
        if (Ante3)
            antes.Add(3);
        if (Ante4)
            antes.Add(4);
        if (Ante5)
            antes.Add(5);
        if (Ante6)
            antes.Add(6);
        if (Ante7)
            antes.Add(7);
        if (Ante8)
            antes.Add(8);

        var config = new ItemConfig
        {
            ItemKey = ItemKey,
            ItemType = ItemType,
            Antes = antes,
            Edition = SelectedEdition,
            Seal = SelectedSeal,
            Enhancement = SelectedEnhancement,
            Rank = SelectedRank,
            Suit = SelectedSuit,
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
        if (string.IsNullOrWhiteSpace(text))
            return result;
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
