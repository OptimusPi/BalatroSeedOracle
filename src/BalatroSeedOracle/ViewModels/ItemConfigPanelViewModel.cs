using System;
using System.Linq;
using BalatroSeedOracle.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    public partial class ItemConfigPanelViewModel : ObservableObject
    {
        private readonly FilterItem _item;
        private readonly Action? _onApply;
        private readonly Action? _onClose;

        [ObservableProperty]
        private string _itemName = "";

        [ObservableProperty]
        private string? _label;

        [ObservableProperty]
        private int _score = 1;

        [ObservableProperty]
        private int _minCount = 1;

        [ObservableProperty]
        private string _edition = "None";

        [ObservableProperty]
        private string _seal = "None";

        [ObservableProperty]
        private string _enhancement = "None";

        [ObservableProperty]
        private string? _rank;

        [ObservableProperty]
        private string? _suit;

        [ObservableProperty]
        private bool _isEternal;

        [ObservableProperty]
        private bool _isPerishable;

        [ObservableProperty]
        private bool _isRental;

        public bool IsLabelVisible => _item?.Status == FilterItemStatus.ShouldHave;
        public bool IsScoreVisible => _item?.Status == FilterItemStatus.ShouldHave;
        public bool IsMinCountVisible => _item?.Status == FilterItemStatus.MustHave;

        public bool IsEditionVisible =>
            _item?.ItemType == "Joker"
            || _item?.ItemType == "SoulJoker"
            || _item?.ItemType == "StandardCard";

        public bool IsStickersVisible =>
            _item?.ItemType == "Joker" || _item?.ItemType == "SoulJoker";

        public bool IsSealVisible => _item?.ItemType == "StandardCard";
        public bool IsEnhancementVisible => _item?.ItemType == "StandardCard";
        public bool IsRankSuitVisible => _item?.ItemType == "StandardCard";

        // Additional JAML item types
        public bool IsTarotCardVisible => _item?.ItemType == "TarotCard";
        public bool IsPlanetCardVisible => _item?.ItemType == "PlanetCard";
        public bool IsSpectralCardVisible => _item?.ItemType == "SpectralCard";
        public bool IsBossBlindVisible => _item?.ItemType == "BossBlind";
        public bool IsVoucherVisible => _item?.ItemType == "Voucher";
        public bool IsTagVisible => _item?.ItemType == "Tag";

        // Antes checkboxes
        [ObservableProperty]
        private bool _allAntesSelected = true;

        [ObservableProperty]
        private bool _ante1Selected = false;

        [ObservableProperty]
        private bool _ante2Selected = false;

        [ObservableProperty]
        private bool _ante3Selected = false;

        [ObservableProperty]
        private bool _ante4Selected = false;

        [ObservableProperty]
        private bool _ante5Selected = false;

        [ObservableProperty]
        private bool _ante6Selected = false;

        [ObservableProperty]
        private bool _ante7Selected = false;

        [ObservableProperty]
        private bool _ante8Selected = false;

        // Shop slots checkboxes
        [ObservableProperty]
        private bool _allShopSlotsSelected = true;

        [ObservableProperty]
        private bool _shopSlot1Selected = false;

        [ObservableProperty]
        private bool _shopSlot2Selected = false;

        [ObservableProperty]
        private bool _shopSlot3Selected = false;

        [ObservableProperty]
        private bool _shopSlot4Selected = false;

        // Pack positions checkboxes
        [ObservableProperty]
        private bool _allPackPositionsSelected = true;

        [ObservableProperty]
        private bool _packPos1Selected = false;

        [ObservableProperty]
        private bool _packPos2Selected = false;

        [ObservableProperty]
        private bool _packPos3Selected = false;

        [ObservableProperty]
        private bool _packPos4Selected = false;

        [ObservableProperty]
        private bool _packPos5Selected = false;

        // Sources checkboxes - according to JAML schema: "shop", "pack", "tag", "voucher"
        [ObservableProperty]
        private bool _sourceShop = true;

        [ObservableProperty]
        private bool _sourcePack = true;

        [ObservableProperty]
        private bool _sourceTag = true;

        [ObservableProperty]
        private bool _sourceVoucher = true;

        // Nested logic conditions for JAML and/or groups
        [ObservableProperty]
        private bool _enableAndGroup = false;

        [ObservableProperty]
        private bool _enableOrGroup = false;

        public ItemConfigPanelViewModel(
            FilterItem item,
            Action? onApply = null,
            Action? onClose = null
        )
        {
            _item = item;
            _onApply = onApply;
            _onClose = onClose;

            // Initialize from item
            ItemName = item.DisplayName;
            Label = item.Label;
            Score = item.Score > 0 ? item.Score : 1;
            MinCount = item.MinCount > 0 ? item.MinCount : 1;

            // Initialize edition, seal, enhancement, rank, suit
            Edition = item.Edition ?? "None";
            Seal = item.Seal ?? "None";
            Enhancement = item.Enhancement ?? "None";
            Rank = item.Rank;
            Suit = item.Suit;

            // Initialize stickers
            if (item.Stickers is not null)
            {
                IsEternal = item.Stickers.Contains("eternal");
                IsPerishable = item.Stickers.Contains("perishable");
                IsRental = item.Stickers.Contains("rental");
            }

            // Load antes configuration
            if (item.Antes is not null && item.Antes.Length > 0)
            {
                AllAntesSelected = false;
                Ante1Selected = item.Antes.Contains(1);
                Ante2Selected = item.Antes.Contains(2);
                Ante3Selected = item.Antes.Contains(3);
                Ante4Selected = item.Antes.Contains(4);
                Ante5Selected = item.Antes.Contains(5);
                Ante6Selected = item.Antes.Contains(6);
                Ante7Selected = item.Antes.Contains(7);
                Ante8Selected = item.Antes.Contains(8);
            }

            // Load shop slots configuration
            if (item.ShopSlots is not null && item.ShopSlots.Length > 0)
            {
                AllShopSlotsSelected = false;
                ShopSlot1Selected = item.ShopSlots.Contains(1);
                ShopSlot2Selected = item.ShopSlots.Contains(2);
                ShopSlot3Selected = item.ShopSlots.Contains(3);
                ShopSlot4Selected = item.ShopSlots.Contains(4);
            }

            // Load pack positions configuration
            if (item.PackPositions is not null && item.PackPositions.Length > 0)
            {
                AllPackPositionsSelected = false;
                PackPos1Selected = item.PackPositions.Contains(1);
                PackPos2Selected = item.PackPositions.Contains(2);
                PackPos3Selected = item.PackPositions.Contains(3);
                PackPos4Selected = item.PackPositions.Contains(4);
                PackPos5Selected = item.PackPositions.Contains(5);
            }

            // Load sources configuration according to JAML schema
            if (item.Sources is not null && item.Sources.Length > 0)
            {
                SourceShop = item.Sources.Contains("shop");
                SourcePack = item.Sources.Contains("pack");
                SourceTag = item.Sources.Contains("tag");
                SourceVoucher = item.Sources.Contains("voucher");
            }
            else
            {
                // Default to all sources if none specified
                SourceShop = true;
                SourcePack = true;
                SourceTag = true;
                SourceVoucher = true;
            }

            // Load nested logic conditions
            EnableAndGroup = item.AndConditions is not null && item.AndConditions.Length > 0;
            EnableOrGroup = item.OrConditions is not null && item.OrConditions.Length > 0;
        }

        partial void OnAllAntesSelectedChanged(bool value)
        {
            if (value)
            {
                // Uncheck all individual antes when "All" is selected
                Ante1Selected = false;
                Ante2Selected = false;
                Ante3Selected = false;
                Ante4Selected = false;
                Ante5Selected = false;
                Ante6Selected = false;
                Ante7Selected = false;
                Ante8Selected = false;
            }
        }

        partial void OnAllShopSlotsSelectedChanged(bool value)
        {
            if (value)
            {
                ShopSlot1Selected = false;
                ShopSlot2Selected = false;
                ShopSlot3Selected = false;
                ShopSlot4Selected = false;
            }
        }

        partial void OnAllPackPositionsSelectedChanged(bool value)
        {
            if (value)
            {
                PackPos1Selected = false;
                PackPos2Selected = false;
                PackPos3Selected = false;
                PackPos4Selected = false;
                PackPos5Selected = false;
            }
        }

        partial void OnItemNameChanged(string value)
        {
            if (_item is not null)
            {
                _item.DisplayName = value;
            }
        }

        partial void OnLabelChanged(string? value)
        {
            if (_item is not null)
            {
                _item.Label = value;
            }
        }

        partial void OnScoreChanged(int value)
        {
            if (_item is not null)
            {
                _item.Score = value;
            }
        }

        partial void OnMinCountChanged(int value)
        {
            if (_item is not null)
            {
                _item.MinCount = value;
            }
        }

        partial void OnEditionChanged(string value)
        {
            if (_item is not null)
            {
                _item.Edition = value == "None" ? null : value;
                UpdatePreviewImage();
            }
        }

        partial void OnSealChanged(string value)
        {
            if (_item is not null)
            {
                _item.Seal = value == "None" ? null : value;
                UpdatePreviewImage();
            }
        }

        partial void OnEnhancementChanged(string value)
        {
            if (_item is not null)
            {
                _item.Enhancement = value == "None" ? null : value;
                UpdatePreviewImage();
            }
        }

        partial void OnRankChanged(string? value)
        {
            if (_item is not null)
            {
                _item.Rank = value;
                UpdatePreviewImage();
            }
        }

        partial void OnSuitChanged(string? value)
        {
            if (_item is not null)
            {
                _item.Suit = value;
                UpdatePreviewImage();
            }
        }

        private void UpdatePreviewImage()
        {
            if (_item is null)
                return;

            if (_item.Category == "Playing Cards" || _item.ItemType == "StandardCard")
            {
                var suit = _item.Suit ?? "Spades";
                var rank = _item.Rank ?? "Ace";
                _item.ItemImage = Services.SpriteService.Instance.GetPlayingCardImage(
                    suit,
                    rank,
                    _item.Enhancement,
                    _item.Seal,
                    _item.Edition
                );
            }
            else
            {
                // Use the robust GetItemImage helper
                _item.ItemImage = Services.SpriteService.Instance.GetItemImage(
                    _item.Name,
                    _item.ItemType
                );
            }
        }

        partial void OnIsEternalChanged(bool value)
        {
            UpdateStickers();
        }

        partial void OnIsPerishableChanged(bool value)
        {
            UpdateStickers();
        }

        partial void OnIsRentalChanged(bool value)
        {
            UpdateStickers();
        }

        private void UpdateStickers()
        {
            if (_item is null)
                return;
            var stickers = new System.Collections.Generic.List<string>();
            if (IsEternal)
                stickers.Add("eternal");
            if (IsPerishable)
                stickers.Add("perishable");
            if (IsRental)
                stickers.Add("rental");
            _item.Stickers = stickers.Count > 0 ? stickers : null;

            // CRITICAL: Update the preview images for the stickers too!
            UpdatePreviewImage();
        }

        [RelayCommand]
        private void Apply()
        {
            // Update all item properties from current ViewModel state
            _item.Label = Label;
            _item.Score = Score;
            _item.MinCount = MinCount;

            // Save antes configuration
            if (AllAntesSelected)
            {
                _item.Antes = null; // null means "all antes"
            }
            else
            {
                var antes = new System.Collections.Generic.List<int>();
                if (Ante1Selected)
                    antes.Add(1);
                if (Ante2Selected)
                    antes.Add(2);
                if (Ante3Selected)
                    antes.Add(3);
                if (Ante4Selected)
                    antes.Add(4);
                if (Ante5Selected)
                    antes.Add(5);
                if (Ante6Selected)
                    antes.Add(6);
                if (Ante7Selected)
                    antes.Add(7);
                if (Ante8Selected)
                    antes.Add(8);
                _item.Antes = antes.Count > 0 ? antes.ToArray() : null;
            }

            // Save shop slots configuration
            if (AllShopSlotsSelected)
            {
                _item.ShopSlots = null; // null means "all slots"
            }
            else
            {
                var slots = new System.Collections.Generic.List<int>();
                if (ShopSlot1Selected)
                    slots.Add(1);
                if (ShopSlot2Selected)
                    slots.Add(2);
                if (ShopSlot3Selected)
                    slots.Add(3);
                if (ShopSlot4Selected)
                    slots.Add(4);
                _item.ShopSlots = slots.Count > 0 ? slots.ToArray() : null;
            }

            // Save pack positions configuration
            if (AllPackPositionsSelected)
            {
                _item.PackPositions = null; // null means "all positions"
            }
            else
            {
                var positions = new System.Collections.Generic.List<int>();
                if (PackPos1Selected)
                    positions.Add(1);
                if (PackPos2Selected)
                    positions.Add(2);
                if (PackPos3Selected)
                    positions.Add(3);
                if (PackPos4Selected)
                    positions.Add(4);
                if (PackPos5Selected)
                    positions.Add(5);
                _item.PackPositions = positions.Count > 0 ? positions.ToArray() : null;
            }

            // Save sources configuration according to JAML schema
            var sources = new System.Collections.Generic.List<string>();
            if (SourceShop)
                sources.Add("shop");
            if (SourcePack)
                sources.Add("pack");
            if (SourceTag)
                sources.Add("tag");
            if (SourceVoucher)
                sources.Add("voucher");
            _item.Sources = sources.Count > 0 ? sources.ToArray() : null;

            // Save edition, seal, enhancement, rank, suit - validate against JAML schema
            _item.Edition = ValidateEdition(Edition);
            _item.Seal = ValidateSeal(Seal);
            _item.Enhancement = ValidateEnhancement(Enhancement);
            _item.Rank = ValidateRank(Rank);
            _item.Suit = ValidateSuit(Suit);

            // Save stickers
            var stickers = new System.Collections.Generic.List<string>();
            if (IsEternal)
                stickers.Add("eternal");
            if (IsPerishable)
                stickers.Add("perishable");
            if (IsRental)
                stickers.Add("rental");
            _item.Stickers = stickers.Count > 0 ? stickers : null;

            // Save nested logic conditions
            _item.AndConditions = EnableAndGroup ? new FilterItem[0] : null; // Placeholder for future implementation
            _item.OrConditions = EnableOrGroup ? new FilterItem[0] : null; // Placeholder for future implementation

            _onApply?.Invoke();
            _onClose?.Invoke();
        }

        [RelayCommand]
        private void Reset()
        {
            Label = null;
            Score = 1;
            MinCount = 1;
            Edition = "None";
            Seal = "None";
            Enhancement = "None";
            Rank = null;
            Suit = null;
            IsEternal = false;
            IsPerishable = false;
            IsRental = false;
            AllAntesSelected = true;
            AllShopSlotsSelected = true;
            AllPackPositionsSelected = true;
            SourceShop = true;
            SourcePack = true;
            SourceTag = true;
            SourceVoucher = true;
            EnableAndGroup = false;
            EnableOrGroup = false;
        }

        [RelayCommand]
        private void Close()
        {
            _onClose?.Invoke();
        }

        [RelayCommand]
        private void Cancel()
        {
            _onClose?.Invoke();
        }

        // JAML Schema Validation Methods
        private string? ValidateEdition(string? edition)
        {
            var validEditions = new[] { "Foil", "Holographic", "Polychrome", "Negative" };
            return (edition != "None" && validEditions.Contains(edition)) ? edition : null;
        }

        private string? ValidateSeal(string? seal)
        {
            var validSeals = new[] { "Red", "Blue", "Gold", "Purple" };
            return (seal != "None" && validSeals.Contains(seal)) ? seal : null;
        }

        private string? ValidateEnhancement(string? enhancement)
        {
            var validEnhancements = new[]
            {
                "Bonus",
                "Mult",
                "Wild",
                "Glass",
                "Steel",
                "Stone",
                "Lucky",
                "Gold",
            };
            return (enhancement != "None" && validEnhancements.Contains(enhancement))
                ? enhancement
                : null;
        }

        private string? ValidateRank(string? rank)
        {
            var validRanks = new[]
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
                "J",
                "Q",
                "K",
                "A",
            };
            return validRanks.Contains(rank) ? rank : null;
        }

        private string? ValidateSuit(string? suit)
        {
            var validSuits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            return validSuits.Contains(suit) ? suit : null;
        }
    }
}
