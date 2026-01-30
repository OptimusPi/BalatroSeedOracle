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

        /// <summary>Parameterless ctor for XAML loader only. Throws at runtime. Creator must pass ViewModel.</summary>
        public FiltersModal()
            : this(throwForDesignTimeOnly: true)
        {
        }

        private FiltersModal(bool throwForDesignTimeOnly)
        {
            if (throwForDesignTimeOnly)
                throw new InvalidOperationException("Do not use FiltersModal(). Use new FiltersModal(menu.ViewModel.FiltersModalViewModel).");
            InitializeComponent();
        }

        public FiltersModal(FiltersModalViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            viewModel.InitializeTabs();
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
        }
    }
}
