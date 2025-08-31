using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components
{
    public partial class VisualFilterBuilder : UserControl
    {
        public class DraggableItem
        {
            public string ItemType { get; set; } = "";
            public string ItemId { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Category { get; set; } = "";
        }

        private ItemsControl? _itemPalette;
        private ItemsControl? _mustItems;
        private ItemsControl? _shouldItems;
        private ItemsControl? _mustNotItems;
        private TextBox? _searchBox;

        private Border? _mustDropZone;
        private Border? _shouldDropZone;
        private Border? _mustNotDropZone;

        private ObservableCollection<DraggableItem> _allItems = new();
        private ObservableCollection<DraggableItem> _paletteItems = new();
        private ObservableCollection<DraggableItem> _mustItemsList = new();
        private ObservableCollection<DraggableItem> _shouldItemsList = new();
        private ObservableCollection<DraggableItem> _mustNotItemsList = new();

        private string _currentCategory = "CommonJokers";

        public VisualFilterBuilder()
        {
            InitializeComponent();
            InitializeDragDrop();
            LoadItems();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _itemPalette = this.FindControl<ItemsControl>("ItemPalette");
            _mustItems = this.FindControl<ItemsControl>("MustItems");
            _shouldItems = this.FindControl<ItemsControl>("ShouldItems");
            _mustNotItems = this.FindControl<ItemsControl>("MustNotItems");
            _searchBox = this.FindControl<TextBox>("SearchBox");

            _mustDropZone = this.FindControl<Border>("MustDropZone");
            _shouldDropZone = this.FindControl<Border>("ShouldDropZone");
            _mustNotDropZone = this.FindControl<Border>("MustNotDropZone");

            // Set up item sources
            if (_itemPalette != null) _itemPalette.ItemsSource = _paletteItems;
            if (_mustItems != null) _mustItems.ItemsSource = _mustItemsList;
            if (_shouldItems != null) _shouldItems.ItemsSource = _shouldItemsList;
            if (_mustNotItems != null) _mustNotItems.ItemsSource = _mustNotItemsList;

            // Wire up search
            if (_searchBox != null)
            {
                _searchBox.TextChanged += OnSearchTextChanged;
            }

            // Wire up category buttons
            WireCategoryButtons();
        }

        private void WireCategoryButtons()
        {
            var buttons = new[]
            {
                ("FavoritesTab", "Favorites"),
                ("SoulJokersTab", "Legendary"),
                ("RareJokersTab", "Rare"),
                ("UncommonJokersTab", "Uncommon"),
                ("CommonJokersTab", "Common"),
                ("VouchersTab", "Voucher"),
                ("TarotsTab", "Tarot"),
                ("SpectralsTab", "Spectral"),
                ("TagsTab", "Tag"),
                ("BossesTab", "Boss"),
                ("ClearTab", "Clear")
            };

            foreach (var (name, category) in buttons)
            {
                var button = this.FindControl<Button>(name);
                if (button != null)
                {
                    button.Click += (s, e) => OnCategoryClick(category);
                }
            }
        }

        private void OnCategoryClick(string category)
        {
            if (category == "Clear")
            {
                ClearAllDropZones();
                return;
            }

            _currentCategory = category;
            FilterItems();
        }

        private void ClearAllDropZones()
        {
            _mustItemsList.Clear();
            _shouldItemsList.Clear();
            _mustNotItemsList.Clear();
        }

        private void InitializeDragDrop()
        {
            if (_mustDropZone != null)
            {
                AddHandler(DragDrop.DropEvent, OnDrop);
                AddHandler(DragDrop.DragOverEvent, OnDragOver);
                AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
                
                _mustDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                _mustDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                _mustDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }

            if (_shouldDropZone != null)
            {
                _shouldDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                _shouldDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                _shouldDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }

            if (_mustNotDropZone != null)
            {
                _mustNotDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                _mustNotDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                _mustNotDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }
        }

        private void LoadItems()
        {
            // Load jokers
            var commonJokers = new[]
            {
                new DraggableItem { ItemType = "joker", ItemId = "j_joker", DisplayName = "Joker", Category = "Common" },
                new DraggableItem { ItemType = "joker", ItemId = "j_greedy_joker", DisplayName = "Greedy Joker", Category = "Common" },
                new DraggableItem { ItemType = "joker", ItemId = "j_lusty_joker", DisplayName = "Lusty Joker", Category = "Common" },
                new DraggableItem { ItemType = "joker", ItemId = "j_wrathful_joker", DisplayName = "Wrathful Joker", Category = "Common" },
                new DraggableItem { ItemType = "joker", ItemId = "j_gluttonous_joker", DisplayName = "Gluttonous Joker", Category = "Common" },
            };

            var rareJokers = new[]
            {
                new DraggableItem { ItemType = "joker", ItemId = "j_baseball", DisplayName = "Baseball Card", Category = "Rare" },
                new DraggableItem { ItemType = "joker", ItemId = "j_bloodstone", DisplayName = "Bloodstone", Category = "Rare" },
                new DraggableItem { ItemType = "joker", ItemId = "j_blueprint", DisplayName = "Blueprint", Category = "Rare" },
                new DraggableItem { ItemType = "joker", ItemId = "j_brainstorm", DisplayName = "Brainstorm", Category = "Rare" },
            };

            var legendaryJokers = new[]
            {
                new DraggableItem { ItemType = "joker", ItemId = "j_chicot", DisplayName = "Chicot", Category = "Legendary" },
                new DraggableItem { ItemType = "joker", ItemId = "j_perkeo", DisplayName = "Perkeo", Category = "Legendary" },
                new DraggableItem { ItemType = "joker", ItemId = "j_triboulet", DisplayName = "Triboulet", Category = "Legendary" },
                new DraggableItem { ItemType = "joker", ItemId = "j_yorick", DisplayName = "Yorick", Category = "Legendary" },
                new DraggableItem { ItemType = "joker", ItemId = "j_canetestino", DisplayName = "Canetestino", Category = "Legendary" },
            };

            // Load vouchers
            var vouchers = new[]
            {
                new DraggableItem { ItemType = "voucher", ItemId = "v_overstock_norm", DisplayName = "Overstock", Category = "Voucher" },
                new DraggableItem { ItemType = "voucher", ItemId = "v_clearance_sale", DisplayName = "Clearance Sale", Category = "Voucher" },
                new DraggableItem { ItemType = "voucher", ItemId = "v_telescope", DisplayName = "Telescope", Category = "Voucher" },
                new DraggableItem { ItemType = "voucher", ItemId = "v_observatory", DisplayName = "Observatory", Category = "Voucher" },
            };

            // Add all items
            foreach (var item in commonJokers.Concat(rareJokers).Concat(legendaryJokers).Concat(vouchers))
            {
                _allItems.Add(item);
            }

            FilterItems();
        }

        private void FilterItems()
        {
            _paletteItems.Clear();
            
            var query = _searchBox?.Text?.ToLowerInvariant() ?? "";
            var filtered = _allItems.Where(item =>
            {
                if (!string.IsNullOrEmpty(query) && !item.DisplayName.ToLowerInvariant().Contains(query))
                    return false;
                
                if (_currentCategory == "Common" && item.Category == "Common") return true;
                if (_currentCategory == "Rare" && item.Category == "Rare") return true;
                if (_currentCategory == "Legendary" && item.Category == "Legendary") return true;
                if (_currentCategory == "Voucher" && item.ItemType == "voucher") return true;
                
                return false;
            });

            foreach (var item in filtered)
            {
                _paletteItems.Add(item);
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            FilterItems();
        }

        private async void OnDragStart(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is DraggableItem item)
            {
                border.Classes.Add("is-dragging");
                
                var dragData = new DataObject();
                dragData.Set("DraggableItem", item);
                
                try
                {
                    await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
                }
                finally
                {
                    border.Classes.Remove("is-dragging");
                }
            }
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (sender is Border border && e.Data.Contains("DraggableItem"))
            {
                e.DragEffects = DragDropEffects.Copy;
                border.Classes.Add("drag-over");
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Classes.Remove("drag-over");
            }
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (sender is Border dropZone && e.Data.Contains("DraggableItem"))
            {
                dropZone.Classes.Remove("drag-over");
                
                var item = e.Data.Get("DraggableItem") as DraggableItem;
                if (item == null) return;

                // Determine which list to add to
                ObservableCollection<DraggableItem>? targetList = null;
                if (dropZone == _mustDropZone) targetList = _mustItemsList;
                else if (dropZone == _shouldDropZone) targetList = _shouldItemsList;
                else if (dropZone == _mustNotDropZone) targetList = _mustNotItemsList;

                if (targetList != null && !targetList.Any(i => i.ItemId == item.ItemId))
                {
                    targetList.Add(item);
                    DebugLogger.Log("VisualFilterBuilder", $"Added {item.DisplayName} to {dropZone.Name}");
                }
            }
        }

        public Dictionary<string, List<string>> GetFilterData()
        {
            return new Dictionary<string, List<string>>
            {
                ["must"] = _mustItemsList.Select(i => i.ItemId).ToList(),
                ["should"] = _shouldItemsList.Select(i => i.ItemId).ToList(),
                ["mustNot"] = _mustNotItemsList.Select(i => i.ItemId).ToList()
            };
        }

        public void LoadFilterData(Dictionary<string, List<string>> data)
        {
            ClearAllDropZones();
            
            if (data.ContainsKey("must"))
            {
                foreach (var itemId in data["must"])
                {
                    var item = _allItems.FirstOrDefault(i => i.ItemId == itemId);
                    if (item != null) _mustItemsList.Add(item);
                }
            }
            
            if (data.ContainsKey("should"))
            {
                foreach (var itemId in data["should"])
                {
                    var item = _allItems.FirstOrDefault(i => i.ItemId == itemId);
                    if (item != null) _shouldItemsList.Add(item);
                }
            }
            
            if (data.ContainsKey("mustNot"))
            {
                foreach (var itemId in data["mustNot"])
                {
                    var item = _allItems.FirstOrDefault(i => i.ItemId == itemId);
                    if (item != null) _mustNotItemsList.Add(item);
                }
            }
        }
    }
}