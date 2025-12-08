using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Settings Modal - Clean settings UI without feature flags
    /// Provides access to useful settings and resources
    /// </summary>
    public partial class SettingsModal : UserControl
    {
        private SettingsModalViewModel? _viewModel;

        public SettingsModal()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new SettingsModalViewModel();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            // Unsubscribe from old VM
            if (_viewModel != null)
            {
                _viewModel.FeatureTogglesChanged -= OnFeatureTogglesChanged;
            }

            // Subscribe to new VM
            _viewModel = DataContext as SettingsModalViewModel;
            if (_viewModel != null)
            {
                _viewModel.FeatureTogglesChanged += OnFeatureTogglesChanged;
            }
        }

        private void OnFeatureTogglesChanged(object? sender, EventArgs e)
        {
            // Refresh main menu widget visibility
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu?.DataContext is BalatroMainMenuViewModel mainMenuVm)
            {
                mainMenuVm.RefreshFeatureToggles();
            }
        }

        private void OnWordListsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Show word lists modal with back navigation to Settings
                mainMenu.ShowWordListsModalFromSettings();
            }
            else
            {
                DebugLogger.LogError(
                    "SettingsModal",
                    "Could not find BalatroMainMenu in visual tree"
                );
            }
        }

        private void OnCreditsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Show credits modal with back navigation to Settings
                mainMenu.ShowCreditsModalFromSettings();
            }
            else
            {
                DebugLogger.LogError(
                    "SettingsModal",
                    "Could not find BalatroMainMenu in visual tree"
                );
            }
        }

        private void OnOpenFiltersDirectoryClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Use AppPaths for proper cross-platform user data directory
                var filtersDir = Helpers.AppPaths.FiltersDir;

                // Open the directory in the default file manager
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start("explorer.exe", filtersDir);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", filtersDir);
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", filtersDir);
                }

                DebugLogger.Log("SettingsModal", $"Opened filters directory: {filtersDir}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "SettingsModal",
                    $"Error opening filters directory: {ex.Message}"
                );
            }
        }

        private void OnOpenAppDirectoryClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Use AppPaths.DataRootDir for the proper AppData folder, not bin folder
                var appDir = AppPaths.DataRootDir;

                // Open the directory in the default file manager
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start("explorer.exe", appDir);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", appDir);
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", appDir);
                }

                DebugLogger.Log("SettingsModal", $"Opened app directory: {appDir}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SettingsModal", $"Error opening app directory: {ex.Message}");
            }
        }

        private void OnAddWidgetsClick(object? sender, RoutedEventArgs e)
        {
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu != null)
            {
                mainMenu.ShowWidgetPickerFromSettings();
            }
            else
            {
                DebugLogger.LogError(
                    "SettingsModal",
                    "Could not find BalatroMainMenu in visual tree"
                );
            }
        }
    }
}
