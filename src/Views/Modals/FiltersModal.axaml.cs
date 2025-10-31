using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FiltersModal : UserControl
    {
        public FiltersModalViewModel? ViewModel => DataContext as FiltersModalViewModel;

        public FiltersModal()
        {
            InitializeComponent();

            // Create ViewModel directly for now - avoid DI complexity
            try
            {
                var configService = ServiceHelper.GetService<IConfigurationService>();
                var filterService = ServiceHelper.GetService<IFilterService>();

                if (configService == null || filterService == null)
                {
                    // Create defaults if DI fails
                    configService = new ConfigurationService();
                    filterService = new FilterService(configService);
                }

                var viewModel = new FiltersModalViewModel(configService, filterService);
                DataContext = viewModel;

                // Initialize tabs now that we're on the UI thread
                viewModel.InitializeTabs();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FiltersModal", $"Failed to initialize: {ex.Message}");
                // Create a minimal fallback
                DataContext = null;
            }
        }

        /// <summary>
        /// Load configuration from file (delegates to ViewModel)
        /// </summary>
        public async Task LoadConfigAsync(string configPath)
        {
            if (ViewModel != null)
            {
                await ViewModel.LoadFilter();
            }
        }

        /// <summary>
        /// Enable all tabs (for editing existing filters)
        /// </summary>
        public void EnableAllTabs()
        {
            // In proper MVVM, tabs are always enabled via binding
            // This is for backwards compatibility with old calls
        }
    }
}
