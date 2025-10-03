using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// PROPER MVVM SearchModal - zero business logic in code-behind
    /// All logic delegated to SearchModalViewModel
    /// </summary>
    public partial class SearchModal : UserControl
    {
        public SearchModalViewModel? ViewModel => DataContext as SearchModalViewModel;

        public SearchModal()
        {
            InitializeComponent();
            
            // PROPER MVVM: Inject ViewModel via DI, no manual control finding
            var viewModel = ServiceHelper.GetRequiredService<SearchModalViewModel>();
            DataContext = viewModel;
            
            // Wire up close event
            viewModel.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Connect to an existing search instance (for resuming searches)
        /// </summary>
        public async void ConnectToExistingSearch(string searchId)
        {
            try
            {
                if (ViewModel != null)
                    await ViewModel.ConnectToExistingSearch(searchId);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Failed to connect to search: {ex.Message}");
            }
        }

        /// <summary>
        /// Load configuration from file path
        /// </summary>
        public void LoadConfigFromPath(string configPath)
        {
            ViewModel?.LoadConfigFromPath(configPath);
        }

        /// <summary>
        /// Event for modal close
        /// </summary>
        public event EventHandler? CloseRequested;
        
        /// <summary>
        /// Event for creating shortcuts (backwards compatibility)
        /// </summary>
        public event EventHandler<string>? CreateShortcutRequested
        {
            add { if (ViewModel != null) ViewModel.CreateShortcutRequested += value; }
            remove { if (ViewModel != null) ViewModel.CreateShortcutRequested -= value; }
        }

        // Backwards compatibility wrapper methods
        public string GetCurrentSearchId() => ViewModel?.CurrentSearchId ?? string.Empty;
        public void SetCurrentFilterPath(string path) => ViewModel?.LoadConfigFromPath(path);
        public async Task LoadFilterAsync() => await (ViewModel?.LoadFilterAsync() ?? Task.CompletedTask);
        public async Task LoadFilterAsync(string configPath) => await (ViewModel?.LoadFilterAsync(configPath) ?? Task.CompletedTask);
        public void GoToSearchTab() => ViewModel!.SelectedTabIndex = 2; // Search tab
        public async void SetSearchInstance(string searchId) 
        {
            try
            {
                if (ViewModel != null)
                    await ViewModel.ConnectToExistingSearch(searchId);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Failed to set search instance: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle tab button click - switches tabs
        /// </summary>
        private void OnTabButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
            {
                if (ViewModel != null)
                    ViewModel.SelectedTabIndex = tabIndex;
            }
        }

        /// <summary>
        /// Handle maximize button click - toggles window maximize state
        /// </summary>
        private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var window = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized 
                        ? WindowState.Normal 
                        : WindowState.Maximized;
                        
                    // Update button icon
                    if (sender is Button button)
                    {
                        button.Content = window.WindowState == WindowState.Maximized ? "🗗" : "⛶";
                        ToolTip.SetTip(button, window.WindowState == WindowState.Maximized ? "Restore Window" : "Maximize Window");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SearchModal", $"Error toggling window state: {ex.Message}");
            }
        }
    }
}