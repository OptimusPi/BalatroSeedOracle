using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;

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
        }

        private void OnWordListsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Show word lists modal using ModalHelper extension
                mainMenu.ShowWordListsModal();
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
                // Show credits modal using ModalHelper extension
                mainMenu.ShowCreditsModal();
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
                var filtersDir = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "JsonItemFilters"
                );

                if (!System.IO.Directory.Exists(filtersDir))
                {
                    System.IO.Directory.CreateDirectory(filtersDir);
                }

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
                var appDir = AppContext.BaseDirectory;

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
    }
}
