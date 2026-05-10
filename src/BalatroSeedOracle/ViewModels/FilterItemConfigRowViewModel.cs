using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using BalatroSeedOracle.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for inline filter item configuration row
    /// Handles collapse/expand state and all configuration properties
    /// </summary>
    public partial class FilterItemConfigRowViewModel : ObservableObject
    {
        private readonly FilterItem _item;
        private readonly Action<FilterItem>? _removeCallback;

        [ObservableProperty]
        private bool _isExpanded = false;

        [ObservableProperty]
        private bool _isShouldItem;

        // Antes checkboxes (1-8)
        [ObservableProperty]
        private bool _ante1;

        [ObservableProperty]
        private bool _ante2;

        [ObservableProperty]
        private bool _ante3;

        [ObservableProperty]
        private bool _ante4;

        [ObservableProperty]
        private bool _ante5;

        [ObservableProperty]
        private bool _ante6;

        [ObservableProperty]
        private bool _ante7;

        [ObservableProperty]
        private bool _ante8;

        // Shop slots checkboxes (1-4)
        [ObservableProperty]
        private bool _slot1;

        [ObservableProperty]
        private bool _slot2;

        [ObservableProperty]
        private bool _slot3;

        [ObservableProperty]
        private bool _slot4;

        // Pack positions checkboxes (1-5)
        [ObservableProperty]
        private bool _pack1;

        [ObservableProperty]
        private bool _pack2;

        [ObservableProperty]
        private bool _pack3;

        [ObservableProperty]
        private bool _pack4;

        [ObservableProperty]
        private bool _pack5;

        [ObservableProperty]
        private int _score;

        [ObservableProperty]
        private string _label = "";

        public FilterItem Item => _item;

        /// <summary>
        /// Summary string shown when collapsed: "Antes:[1,2,3] Slots:[All] Score:9"
        /// </summary>
        public string ConfigSummary
        {
            get
            {
                var parts = new List<string>();

                // Antes summary
                var selectedAntes = GetSelectedAntes();
                if (selectedAntes.Any())
                {
                    parts.Add($"Antes:[{string.Join(",", selectedAntes)}]");
                }

                // Slots summary
                var selectedSlots = GetSelectedSlots();
                if (selectedSlots.Any())
                {
                    if (selectedSlots.Count == 4)
                    {
                        parts.Add("Slots:[All]");
                    }
                    else
                    {
                        parts.Add($"Slots:[{string.Join(",", selectedSlots)}]");
                    }
                }

                // Pack positions summary
                var selectedPacks = GetSelectedPacks();
                if (selectedPacks.Any())
                {
                    if (selectedPacks.Count == 5)
                    {
                        parts.Add("Packs:[All]");
                    }
                    else
                    {
                        parts.Add($"Packs:[{string.Join(",", selectedPacks)}]");
                    }
                }

                // Score (SHOULD items only)
                if (IsShouldItem && Score > 0)
                {
                    parts.Add($"Score:{Score}");
                }

                // Label
                if (!string.IsNullOrWhiteSpace(Label))
                {
                    parts.Add($"Label:{Label}");
                }

                return parts.Any() ? string.Join(" ", parts) : "Not configured";
            }
        }

        public FilterItemConfigRowViewModel(
            FilterItem item,
            bool isShouldItem,
            Action<FilterItem>? removeCallback = null
        )
        {
            _item = item;
            _isShouldItem = isShouldItem;
            _removeCallback = removeCallback;

            // Load existing config from item
            LoadConfigFromItem();

            // Subscribe to property changes to update summary and save to item
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(ConfigSummary) && e.PropertyName != nameof(IsExpanded))
                {
                    OnPropertyChanged(nameof(ConfigSummary));
                    SaveConfigToItem();
                }
            };

            // Initialize commands
            ToggleExpandCommand = new RelayCommand(ToggleExpand);
            RemoveCommand = new RelayCommand(Remove);

            QuickAntes1to3Command = new RelayCommand(() => SetAntes(1, 3));
            QuickAntes1to5Command = new RelayCommand(() => SetAntes(1, 5));
            QuickAntesAllCommand = new RelayCommand(() => SetAntes(1, 8));
            ClearAntesCommand = new RelayCommand(ClearAntes);

            QuickSlotsAllCommand = new RelayCommand(() => SetSlots(1, 4));
            ClearSlotsCommand = new RelayCommand(ClearSlots);

            QuickPacksAllCommand = new RelayCommand(() => SetPacks(1, 5));
            ClearPacksCommand = new RelayCommand(ClearPacks);
        }

        private void LoadConfigFromItem()
        {
            // Load Antes
            if (_item.Antes is not null)
            {
                Ante1 = _item.Antes.Contains(1);
                Ante2 = _item.Antes.Contains(2);
                Ante3 = _item.Antes.Contains(3);
                Ante4 = _item.Antes.Contains(4);
                Ante5 = _item.Antes.Contains(5);
                Ante6 = _item.Antes.Contains(6);
                Ante7 = _item.Antes.Contains(7);
                Ante8 = _item.Antes.Contains(8);
            }

            // Load Shop Slots
            if (_item.ShopSlots is not null)
            {
                Slot1 = _item.ShopSlots.Contains(1);
                Slot2 = _item.ShopSlots.Contains(2);
                Slot3 = _item.ShopSlots.Contains(3);
                Slot4 = _item.ShopSlots.Contains(4);
            }

            // Load Pack Positions
            if (_item.PackPositions is not null)
            {
                Pack1 = _item.PackPositions.Contains(1);
                Pack2 = _item.PackPositions.Contains(2);
                Pack3 = _item.PackPositions.Contains(3);
                Pack4 = _item.PackPositions.Contains(4);
                Pack5 = _item.PackPositions.Contains(5);
            }

            // Load Label
            if (!string.IsNullOrEmpty(_item.Label))
            {
                Label = _item.Label;
            }
        }

        private void SaveConfigToItem()
        {
            // Save Antes
            var selectedAntes = GetSelectedAntes();
            _item.Antes = selectedAntes.Any() ? selectedAntes.ToArray() : null;

            // Save Shop Slots
            var selectedSlots = GetSelectedSlots();
            _item.ShopSlots = selectedSlots.Any() ? selectedSlots.ToArray() : null;

            // Save Pack Positions
            var selectedPacks = GetSelectedPacks();
            _item.PackPositions = selectedPacks.Any() ? selectedPacks.ToArray() : null;

            // Save Label
            _item.Label = string.IsNullOrWhiteSpace(Label) ? null : Label;
        }

        private List<int> GetSelectedAntes()
        {
            var antes = new List<int>();
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
            return antes;
        }

        private List<int> GetSelectedSlots()
        {
            var slots = new List<int>();
            if (Slot1)
                slots.Add(1);
            if (Slot2)
                slots.Add(2);
            if (Slot3)
                slots.Add(3);
            if (Slot4)
                slots.Add(4);
            return slots;
        }

        private List<int> GetSelectedPacks()
        {
            var packs = new List<int>();
            if (Pack1)
                packs.Add(1);
            if (Pack2)
                packs.Add(2);
            if (Pack3)
                packs.Add(3);
            if (Pack4)
                packs.Add(4);
            if (Pack5)
                packs.Add(5);
            return packs;
        }

        private void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        private void Remove()
        {
            _removeCallback?.Invoke(_item);
        }

        private void SetAntes(int start, int end)
        {
            Ante1 = start <= 1 && end >= 1;
            Ante2 = start <= 2 && end >= 2;
            Ante3 = start <= 3 && end >= 3;
            Ante4 = start <= 4 && end >= 4;
            Ante5 = start <= 5 && end >= 5;
            Ante6 = start <= 6 && end >= 6;
            Ante7 = start <= 7 && end >= 7;
            Ante8 = start <= 8 && end >= 8;
        }

        private void ClearAntes()
        {
            Ante1 = Ante2 = Ante3 = Ante4 = Ante5 = Ante6 = Ante7 = Ante8 = false;
        }

        private void SetSlots(int start, int end)
        {
            Slot1 = start <= 1 && end >= 1;
            Slot2 = start <= 2 && end >= 2;
            Slot3 = start <= 3 && end >= 3;
            Slot4 = start <= 4 && end >= 4;
        }

        private void ClearSlots()
        {
            Slot1 = Slot2 = Slot3 = Slot4 = false;
        }

        private void SetPacks(int start, int end)
        {
            Pack1 = start <= 1 && end >= 1;
            Pack2 = start <= 2 && end >= 2;
            Pack3 = start <= 3 && end >= 3;
            Pack4 = start <= 4 && end >= 4;
            Pack5 = start <= 5 && end >= 5;
        }

        private void ClearPacks()
        {
            Pack1 = Pack2 = Pack3 = Pack4 = Pack5 = false;
        }

        // Commands
        public IRelayCommand ToggleExpandCommand { get; }
        public IRelayCommand RemoveCommand { get; }
        public IRelayCommand QuickAntes1to3Command { get; }
        public IRelayCommand QuickAntes1to5Command { get; }
        public IRelayCommand QuickAntesAllCommand { get; }
        public IRelayCommand ClearAntesCommand { get; }
        public IRelayCommand QuickSlotsAllCommand { get; }
        public IRelayCommand ClearSlotsCommand { get; }
        public IRelayCommand QuickPacksAllCommand { get; }
        public IRelayCommand ClearPacksCommand { get; }
    }
}
