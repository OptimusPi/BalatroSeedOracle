using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;

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

        // Sources checkboxes
        [ObservableProperty]
        private bool _includeBoosterPacks = true;

        [ObservableProperty]
        private bool _includeShopStream = true;

        [ObservableProperty]
        private bool _includeSkipTags = true;

        public ItemConfigPanelViewModel(FilterItem item, Action? onApply = null, Action? onClose = null)
        {
            _item = item;
            _onApply = onApply;
            _onClose = onClose;

            // Initialize from item
            ItemName = item.DisplayName;
            Label = item.Label;

            // Load antes configuration
            if (item.Antes != null && item.Antes.Length > 0)
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
            if (item.ShopSlots != null && item.ShopSlots.Length > 0)
            {
                AllShopSlotsSelected = false;
                ShopSlot1Selected = item.ShopSlots.Contains(1);
                ShopSlot2Selected = item.ShopSlots.Contains(2);
                ShopSlot3Selected = item.ShopSlots.Contains(3);
                ShopSlot4Selected = item.ShopSlots.Contains(4);
            }

            // Load pack positions configuration
            if (item.PackPositions != null && item.PackPositions.Length > 0)
            {
                AllPackPositionsSelected = false;
                PackPos1Selected = item.PackPositions.Contains(1);
                PackPos2Selected = item.PackPositions.Contains(2);
                PackPos3Selected = item.PackPositions.Contains(3);
                PackPos4Selected = item.PackPositions.Contains(4);
                PackPos5Selected = item.PackPositions.Contains(5);
            }

            // Load sources configuration
            IncludeBoosterPacks = item.IncludeBoosterPacks;
            IncludeShopStream = item.IncludeShopStream;
            IncludeSkipTags = item.IncludeSkipTags;
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

        [RelayCommand]
        private void Apply()
        {
            // Save label
            _item.Label = Label;

            // Save antes configuration
            if (AllAntesSelected)
            {
                _item.Antes = null; // null means "all antes"
            }
            else
            {
                var antes = new System.Collections.Generic.List<int>();
                if (Ante1Selected) antes.Add(1);
                if (Ante2Selected) antes.Add(2);
                if (Ante3Selected) antes.Add(3);
                if (Ante4Selected) antes.Add(4);
                if (Ante5Selected) antes.Add(5);
                if (Ante6Selected) antes.Add(6);
                if (Ante7Selected) antes.Add(7);
                if (Ante8Selected) antes.Add(8);
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
                if (ShopSlot1Selected) slots.Add(1);
                if (ShopSlot2Selected) slots.Add(2);
                if (ShopSlot3Selected) slots.Add(3);
                if (ShopSlot4Selected) slots.Add(4);
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
                if (PackPos1Selected) positions.Add(1);
                if (PackPos2Selected) positions.Add(2);
                if (PackPos3Selected) positions.Add(3);
                if (PackPos4Selected) positions.Add(4);
                if (PackPos5Selected) positions.Add(5);
                _item.PackPositions = positions.Count > 0 ? positions.ToArray() : null;
            }

            // Save sources configuration
            _item.IncludeBoosterPacks = IncludeBoosterPacks;
            _item.IncludeShopStream = IncludeShopStream;
            _item.IncludeSkipTags = IncludeSkipTags;

            _onApply?.Invoke();
            _onClose?.Invoke();
        }

        [RelayCommand]
        private void Reset()
        {
            Label = null;
            AllAntesSelected = true;
            AllShopSlotsSelected = true;
            AllPackPositionsSelected = true;
            IncludeBoosterPacks = true;
            IncludeShopStream = true;
            IncludeSkipTags = true;
        }

        [RelayCommand]
        private void Close()
        {
            _onClose?.Invoke();
        }
    }
}
