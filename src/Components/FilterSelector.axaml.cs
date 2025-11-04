using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Code-behind for FilterSelector component - minimal logic, delegates to ViewModel
    /// </summary>
    public partial class FilterSelector : UserControl
    {
        // Events - forwarded from ViewModel for consistent API
        public event EventHandler<string>? FilterSelected;
        public event EventHandler<string>? FilterLoaded;
        public event EventHandler<string>? FilterCopyRequested;
        public event EventHandler<string>? FilterEditRequested;
        public event EventHandler<string>? FilterDeleteRequested;
        public event EventHandler? NewFilterRequested;

        // Properties exposed for external configuration
        public bool AutoLoadEnabled
        {
            get => ViewModel?.AutoLoadEnabled ?? true;
            set
            {
                if (ViewModel != null)
                    ViewModel.AutoLoadEnabled = value;
            }
        }

        public bool ShowCreateButton
        {
            get => ViewModel?.ShowCreateButton ?? true;
            set
            {
                if (ViewModel != null)
                    ViewModel.ShowCreateButton = value;
            }
        }

        public bool ShouldSwitchToVisualTab
        {
            get => ViewModel?.ShouldSwitchToVisualTab ?? false;
            set
            {
                if (ViewModel != null)
                    ViewModel.ShouldSwitchToVisualTab = value;
            }
        }

        public bool IsInSearchModal
        {
            get => ViewModel?.IsInSearchModal ?? false;
            set
            {
                if (ViewModel != null)
                    ViewModel.IsInSearchModal = value;
            }
        }

        public bool ShowSelectButton
        {
            get => ViewModel?.ShowSelectButton ?? true;
            set
            {
                if (ViewModel != null)
                    ViewModel.ShowSelectButton = value;
            }
        }

        public bool ShowActionButtons
        {
            get => ViewModel?.ShowActionButtons ?? true;
            set
            {
                if (ViewModel != null)
                    ViewModel.ShowActionButtons = value;
            }
        }

        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<
            FilterSelector,
            string
        >(nameof(Title), "Select Filter");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // ViewModel
        private FilterSelectorViewModel? ViewModel => DataContext as FilterSelectorViewModel;

        public FilterSelector()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeViewModel()
        {
            var spriteService = ServiceHelper.GetRequiredService<SpriteService>();
            var configurationService = ServiceHelper.GetRequiredService<IConfigurationService>();
            var filterCacheService = ServiceHelper.GetRequiredService<IFilterCacheService>();
            var viewModel = new FilterSelectorViewModel(
                spriteService,
                configurationService,
                filterCacheService
            );

            // Wire up ViewModel events to control events
            viewModel.FilterSelected += (s, e) => FilterSelected?.Invoke(this, e);
            viewModel.FilterLoaded += (s, e) => FilterLoaded?.Invoke(this, e);
            viewModel.FilterCopyRequested += (s, e) => FilterCopyRequested?.Invoke(this, e);
            viewModel.FilterEditRequested += (s, e) => FilterEditRequested?.Invoke(this, e);
            viewModel.FilterDeleteRequested += (s, e) => FilterDeleteRequested?.Invoke(this, e);
            viewModel.NewFilterRequested += (s, e) => NewFilterRequested?.Invoke(this, e);

            DataContext = viewModel;
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (ViewModel != null)
            {
                // Sync Title property
                ViewModel.Title = Title;

                // Initialize and load filters
                await ViewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Public method to refresh filters - delegates to ViewModel
        /// </summary>
        public async Task RefreshFiltersAsync()
        {
            try
            {
                if (ViewModel != null)
                {
                    await ViewModel.RefreshFiltersAsync();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelector", $"RefreshFiltersAsync failed: {ex.Message}");
                throw;
            }
        }
    }
}
