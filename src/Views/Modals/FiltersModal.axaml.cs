using System;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Services;
using System.Linq;
using Avalonia.Markup.Xaml;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModal : UserControl
    {
        public FiltersModalViewModel ViewModel { get; }
        private Components.BalatroTabControl? _tabHeader;
        private bool _suppressHeaderSync = false;

        public FiltersModal()
        {
            var configurationService = App.GetService<IConfigurationService>() ?? throw new InvalidOperationException("Could not resolve IConfigurationService");
            var filterService = App.GetService<IFilterService>() ?? throw new InvalidOperationException("Could not resolve IFilterService");
            ViewModel = new FiltersModalViewModel(configurationService, filterService);
            DataContext = ViewModel;

            InitializeComponent();

            // Initialize dynamic tabs content
            ViewModel.InitializeTabs();

            // Wire up the Balatro-style tab header
            _tabHeader = this.FindControl<Components.BalatroTabControl>("TabHeader");
            if (_tabHeader != null)
            {
                // Set tab titles from ViewModel
                var titles = ViewModel.TabItems.Select(t => t.Header).ToArray();
                _tabHeader.SetTabs(titles);

                // When user clicks a header tab, update ViewModel selection and visibility
                _tabHeader.TabChanged += (s, tabIndex) =>
                {
                    _suppressHeaderSync = true;
                    ViewModel.SelectedTabIndex = tabIndex;
                    ViewModel.UpdateTabVisibility(tabIndex);
                    _suppressHeaderSync = false;
                };

                // When ViewModel changes tab programmatically, sync the header control
                ViewModel.PropertyChanged += (s, e) =>
                {
                    if (!_suppressHeaderSync && e.PropertyName == nameof(ViewModel.SelectedTabIndex))
                    {
                        _tabHeader.SwitchToTab(ViewModel.SelectedTabIndex);
                    }
                };
            }
        }
    }
}