using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class FilterSelectorControl : UserControl
    {
        // Events that parent controls can subscribe to
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterCopyRequested;
        public event EventHandler? NewFilterRequested;

        private FilterListViewModel? _viewModel;

        public FilterSelectorControl()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void InitializeViewModel()
        {
            _viewModel = new FilterListViewModel();
            DataContext = _viewModel;
        }

        // Public method to refresh the filter list
        public void RefreshFilters()
        {
            _viewModel?.LoadFilters();
        }

        // Public method to get the currently selected filter path
        public string? GetSelectedFilterPath()
        {
            return _viewModel?.GetSelectedFilterPath();
        }

        // Event handler for filter list item click
        private void OnFilterListItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ViewModels.FilterListItem item)
            {
                _viewModel?.SelectFilter(item);

                // Fire FilterSelected event so parent controls can respond to filter selection
                var filterPath = _viewModel?.GetSelectedFilterPath();
                if (!string.IsNullOrEmpty(filterPath))
                {
                    FilterSelected?.Invoke(this, filterPath);
                }
            }
        }

        // Event handler for "Edit Filter" button
        // Note: This also fires FilterSelected event, which in SearchModal context means
        // "use this filter for search". The event name is generic to support different use cases.
        private void OnEditFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _viewModel?.GetSelectedFilterPath();
            if (!string.IsNullOrEmpty(filterPath))
            {
                // In SearchModal, this will load the filter for searching
                // In FiltersModal, this would open the filter editor
                FilterSelected?.Invoke(this, filterPath);
            }
        }

        // Event handler for "Copy Filter" button
        private void OnCopyFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _viewModel?.GetSelectedFilterPath();
            if (!string.IsNullOrEmpty(filterPath))
            {
                FilterCopyRequested?.Invoke(this, filterPath);
            }
        }

        // Event handler for "+ NEW" button
        private void OnCreateNewFilterClick(object? sender, RoutedEventArgs e)
        {
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
