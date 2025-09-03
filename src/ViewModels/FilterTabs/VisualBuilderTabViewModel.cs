using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    public class VisualBuilderTabViewModel : BaseViewModel
    {
        private string _searchFilter = "";

        // Available items
        public ObservableCollection<FilterItem> AllJokers { get; }
        public ObservableCollection<FilterItem> AllTags { get; }
        public ObservableCollection<FilterItem> AllVouchers { get; }

        // Filtered items (based on search)
        public ObservableCollection<FilterItem> FilteredJokers { get; }
        public ObservableCollection<FilterItem> FilteredTags { get; }
        public ObservableCollection<FilterItem> FilteredVouchers { get; }

        // Selected items
        public ObservableCollection<FilterItem> SelectedMust { get; }
        public ObservableCollection<FilterItem> SelectedShould { get; }
        public ObservableCollection<FilterItem> SelectedMustNot { get; }

        public VisualBuilderTabViewModel()
        {
            // Initialize collections
            AllJokers = new ObservableCollection<FilterItem>();
            AllTags = new ObservableCollection<FilterItem>();
            AllVouchers = new ObservableCollection<FilterItem>();
            
            FilteredJokers = new ObservableCollection<FilterItem>();
            FilteredTags = new ObservableCollection<FilterItem>();
            FilteredVouchers = new ObservableCollection<FilterItem>();
            
            SelectedMust = new ObservableCollection<FilterItem>();
            SelectedShould = new ObservableCollection<FilterItem>();
            SelectedMustNot = new ObservableCollection<FilterItem>();

            // Initialize commands
            AddToMustCommand = new RelayCommand<FilterItem>(AddToMust);
            AddToShouldCommand = new RelayCommand<FilterItem>(AddToShould);
            AddToMustNotCommand = new RelayCommand<FilterItem>(AddToMustNot);
            
            RemoveFromMustCommand = new RelayCommand<FilterItem>(RemoveFromMust);
            RemoveFromShouldCommand = new RelayCommand<FilterItem>(RemoveFromShould);
            RemoveFromMustNotCommand = new RelayCommand<FilterItem>(RemoveFromMustNot);

            // Load sample data
            LoadSampleData();
            ApplyFilter();
        }

        #region Properties

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand AddToMustCommand { get; }
        public ICommand AddToShouldCommand { get; }
        public ICommand AddToMustNotCommand { get; }
        
        public ICommand RemoveFromMustCommand { get; }
        public ICommand RemoveFromShouldCommand { get; }
        public ICommand RemoveFromMustNotCommand { get; }

        #endregion

        #region Command Implementations

        private void AddToMust(FilterItem? item)
        {
            if (item != null && !SelectedMust.Any(x => x.Name == item.Name))
            {
                SelectedMust.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST");
            }
        }

        private void AddToShould(FilterItem? item)
        {
            if (item != null && !SelectedShould.Any(x => x.Name == item.Name))
            {
                SelectedShould.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to SHOULD");
            }
        }

        private void AddToMustNot(FilterItem? item)
        {
            if (item != null && !SelectedMustNot.Any(x => x.Name == item.Name))
            {
                SelectedMustNot.Add(item);
                DebugLogger.Log("VisualBuilderTab", $"Added {item.Name} to MUST NOT");
            }
        }

        private void RemoveFromMust(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMust.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST");
            }
        }

        private void RemoveFromShould(FilterItem? item)
        {
            if (item != null)
            {
                SelectedShould.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from SHOULD");
            }
        }

        private void RemoveFromMustNot(FilterItem? item)
        {
            if (item != null)
            {
                SelectedMustNot.Remove(item);
                DebugLogger.Log("VisualBuilderTab", $"Removed {item.Name} from MUST NOT");
            }
        }

        #endregion

        #region Helper Methods

        private void LoadSampleData()
        {
            // Load from BalatroData - copied from original FiltersModal logic
            try 
            {
                // Load Favorites
                var favorites = FavoritesService.Instance.GetFavoriteItems();
                foreach (var fav in favorites)
                {
                    AllJokers.Add(new FilterItem { Name = fav, Type = "Joker" }); // Assuming favorites are jokers
                }

                // Load Jokers from BalatroData
                if (BalatroData.Jokers?.Keys != null)
                {
                    foreach (var jokerName in BalatroData.Jokers.Keys)
                    {
                        AllJokers.Add(new FilterItem { Name = jokerName, Type = "Joker" });
                    }
                }

                // Load Tags from BalatroData  
                if (BalatroData.Tags?.Keys != null)
                {
                    foreach (var tagName in BalatroData.Tags.Keys)
                    {
                        AllTags.Add(new FilterItem { Name = tagName, Type = "SmallBlindTag" });
                    }
                }

                // Load Vouchers from BalatroData
                if (BalatroData.Vouchers?.Keys != null)
                {
                    foreach (var voucherName in BalatroData.Vouchers.Keys)
                    {
                        AllVouchers.Add(new FilterItem { Name = voucherName, Type = "Voucher" });
                    }
                }

                // Generate playing cards list (copied from original)
                var playingCards = GeneratePlayingCardsList();
                foreach (var card in playingCards)
                {
                    AllVouchers.Add(new FilterItem { Name = card, Type = "PlayingCard" }); // Reusing vouchers collection for now
                }

                DebugLogger.Log("VisualBuilderTab", $"Loaded {AllJokers.Count} jokers, {AllTags.Count} tags, {AllVouchers.Count} vouchers/cards");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Error loading data: {ex.Message}");
                
                // Fallback to sample data
                AllJokers.Add(new FilterItem { Name = "Joker", Type = "Joker" });
                AllTags.Add(new FilterItem { Name = "Negative Tag", Type = "SmallBlindTag" });
                AllVouchers.Add(new FilterItem { Name = "Overstock", Type = "Voucher" });
            }
        }

        // Copied from original FiltersModal
        private List<string> GeneratePlayingCardsList()
        {
            var cards = new List<string>();
            var suits = new[] { "Hearts", "Diamonds", "Clubs", "Spades" };
            var ranks = new[] { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
            
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    cards.Add($"{rank} of {suit}");
                }
            }
            
            return cards;
        }

        private void ApplyFilter()
        {
            FilteredJokers.Clear();
            FilteredTags.Clear();
            FilteredVouchers.Clear();

            var filter = SearchFilter.ToLowerInvariant();

            foreach (var joker in AllJokers)
            {
                if (string.IsNullOrEmpty(filter) || joker.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredJokers.Add(joker);
                }
            }

            foreach (var tag in AllTags)
            {
                if (string.IsNullOrEmpty(filter) || tag.Name.ToLowerInvariant().Contains(filter))
                {
                    FilteredTags.Add(tag);
                }
            }
        }

        #endregion
    }
}