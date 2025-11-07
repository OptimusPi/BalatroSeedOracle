using System;
using System.Collections.ObjectModel;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service interface for filtering filter items by search text.
    /// Single source of truth for filtering logic - eliminates ~300 lines of duplicate code.
    /// </summary>
    public interface IFilterItemFilterService
    {
        /// <summary>
        /// Applies search filter to a single collection
        /// </summary>
        void ApplyFilter(
            string searchFilter,
            ObservableCollection<FilterItem> source,
            ObservableCollection<FilterItem> destination
        );

        /// <summary>
        /// Applies search filter to all collections
        /// </summary>
        void ApplyFilterToAll(
            string searchFilter,
            FilterItemCollections all,
            FilterItemCollections filtered
        );
    }

    /// <summary>
    /// Implementation of FilterItemFilterService.
    /// Centralized filtering logic for all filter items.
    /// </summary>
    public class FilterItemFilterService : IFilterItemFilterService
    {
        public void ApplyFilter(
            string searchFilter,
            ObservableCollection<FilterItem> source,
            ObservableCollection<FilterItem> destination)
        {
            destination.Clear();

            if (string.IsNullOrEmpty(searchFilter))
            {
                foreach (var item in source)
                    destination.Add(item);
                return;
            }

            var filter = searchFilter.ToLowerInvariant();
            foreach (var item in source)
            {
                if (item.Name.ToLowerInvariant().Contains(filter) ||
                    item.DisplayName.ToLowerInvariant().Contains(filter))
                {
                    destination.Add(item);
                }
            }
        }

        public void ApplyFilterToAll(
            string searchFilter,
            FilterItemCollections all,
            FilterItemCollections filtered)
        {
            ApplyFilter(searchFilter, all.AllJokers, filtered.AllJokers);
            ApplyFilter(searchFilter, all.AllTags, filtered.AllTags);
            ApplyFilter(searchFilter, all.AllVouchers, filtered.AllVouchers);
            ApplyFilter(searchFilter, all.AllTarots, filtered.AllTarots);
            ApplyFilter(searchFilter, all.AllPlanets, filtered.AllPlanets);
            ApplyFilter(searchFilter, all.AllSpectrals, filtered.AllSpectrals);
            ApplyFilter(searchFilter, all.AllBosses, filtered.AllBosses);
            ApplyFilter(searchFilter, all.AllWildcards, filtered.AllWildcards);
            ApplyFilter(searchFilter, all.AllStandardCards, filtered.AllStandardCards);
        }
    }
}
