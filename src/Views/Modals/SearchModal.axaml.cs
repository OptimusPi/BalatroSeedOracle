using System;
using Avalonia.Controls;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// PROPER MVVM SearchModal
    /// Minimal code-behind - only initialization and event forwarding
    /// </summary>
    public partial class SearchModal : UserControl
    {
        private readonly SearchModalViewModel _viewModel;

        public SearchModal()
        {
            InitializeComponent();

            // Inject ViewModel via DI
            _viewModel = ServiceHelper.GetRequiredService<SearchModalViewModel>();
            DataContext = _viewModel;

            // Forward events from ViewModel to View
            _viewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event for modal close
        /// </summary>
        public event EventHandler? CloseRequested;

        // Public property to access ViewModel for callers that need it
        public SearchModalViewModel ViewModel => _viewModel;
    }
}