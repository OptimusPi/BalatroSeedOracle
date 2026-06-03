using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Settings Modal - Clean settings UI without feature flags
    /// Provides access to useful settings and resources
    /// </summary>
    public partial class SettingsModal : UserControl
    {
        public SettingsModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new SettingsModalViewModel();
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
                var filtersDir = AppPaths.FiltersDir;

                // Open the directory via the platform service (OS branching lives there).
                ServiceHelper.GetService<IPlatformServices>()?.OpenInFileManager(filtersDir);

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

                // Open the directory via the platform service (OS branching lives there).
                ServiceHelper.GetService<IPlatformServices>()?.OpenInFileManager(appDir);

                DebugLogger.Log("SettingsModal", $"Opened app directory: {appDir}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SettingsModal", $"Error opening app directory: {ex.Message}");
            }
        }

    }
}
