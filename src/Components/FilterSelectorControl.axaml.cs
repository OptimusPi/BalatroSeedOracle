using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class FilterSelectorControl : UserControl
    {
        // IsInSearchModal Dependency Property
        public static readonly StyledProperty<bool> IsInSearchModalProperty =
            AvaloniaProperty.Register<FilterSelectorControl, bool>(nameof(IsInSearchModal), defaultValue: false);

        public bool IsInSearchModal
        {
            get => GetValue(IsInSearchModalProperty);
            set => SetValue(IsInSearchModalProperty, value);
        }

        // Events that parent controls can subscribe to
        public event EventHandler<string>? FilterSelected;       // When a filter is clicked (for preview)
        public event EventHandler<string>? FilterEditRequested;   // When Edit button is clicked
        public event EventHandler<string>? FilterCopyRequested;
        public event EventHandler? NewFilterRequested;

        private FilterListViewModel? _viewModel;
        private Border? _filterListContainer;

        public FilterSelectorControl()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsInSearchModalProperty)
            {
                // Update ViewModel when IsInSearchModal changes
                _viewModel?.SetSearchModalMode((bool)change.NewValue!);
            }
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void InitializeViewModel()
        {
            _viewModel = new FilterListViewModel();
            DataContext = _viewModel;

            // Wire up SizeChanged event for dynamic pagination
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Find the filter list container
            _filterListContainer = this.FindControl<Border>("FilterListContainer");

            if (_filterListContainer != null)
            {
                // Subscribe to size changes
                _filterListContainer.GetObservable(BoundsProperty)
                    .Subscribe(bounds => UpdateItemsPerPage(bounds.Height));
            }
        }

        private void UpdateItemsPerPage(double containerHeight)
        {
            if (_viewModel != null && containerHeight > 0)
            {
                _viewModel.UpdateItemsPerPage(containerHeight);
            }
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
        private void OnEditFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _viewModel?.GetSelectedFilterPath();
            if (!string.IsNullOrEmpty(filterPath))
            {
                // Fire edit event for FiltersModal
                FilterEditRequested?.Invoke(this, filterPath);

                // Also fire FilterSelected for backwards compatibility with SearchModal
                // (SearchModal uses this to load the filter for searching)
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

        // Event handler for "Select This Filter" button (SearchModal context)
        private void OnSelectFilterClick(object? sender, RoutedEventArgs e)
        {
            var filterPath = _viewModel?.GetSelectedFilterPath();
            if (!string.IsNullOrEmpty(filterPath))
            {
                // Fire FilterSelected event for SearchModal to handle
                FilterSelected?.Invoke(this, filterPath);
            }
        }

        // Event handler for "+ NEW" button
        private void OnCreateNewFilterClick(object? sender, RoutedEventArgs e)
        {
            NewFilterRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
