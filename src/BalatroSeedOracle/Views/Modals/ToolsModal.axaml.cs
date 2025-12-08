using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class ToolsModal : UserControl
    {
        private readonly UserProfileService? _userProfileService;

        public ToolsModal()
        {
            InitializeComponent();
            _userProfileService = ServiceHelper.GetService<UserProfileService>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnImportFilesClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Import Filter Configuration",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
                    },
                }
            );

            if (files.Count > 0)
            {
                try
                {
                    // Create JsonItemFilters directory if it doesn't exist
                    var jsonConfigsDir = AppPaths.FiltersDir;
                    if (!Directory.Exists(jsonConfigsDir))
                    {
                        Directory.CreateDirectory(jsonConfigsDir);
                    }

                    int successCount = 0;
                    int failCount = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var importedFilePath = file.Path.LocalPath;

                            // Copy the imported file
                            var fileName = Path.GetFileName(importedFilePath);
                            var destinationPath = Path.Combine(jsonConfigsDir, fileName);

                            // Handle duplicate names
                            if (File.Exists(destinationPath))
                            {
                                var baseName = Path.GetFileNameWithoutExtension(fileName);
                                var extension = Path.GetExtension(fileName);
                                var counter = 1;
                                do
                                {
                                    fileName = $"{baseName}_{counter}{extension}";
                                    destinationPath = Path.Combine(jsonConfigsDir, fileName);
                                    counter++;
                                } while (File.Exists(destinationPath));
                            }

                            File.Copy(importedFilePath, destinationPath, overwrite: false);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "ToolsModal",
                                $"Failed to import file {file.Name}: {ex.Message}"
                            );
                            failCount++;
                        }
                    }

                    // Show result message
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    if (mainMenu != null)
                    {
                        var message =
                            successCount > 0
                                ? $"Successfully imported {successCount} file(s)"
                                    + (
                                        failCount > 0
                                            ? $"\n{failCount} file(s) failed to import"
                                            : ""
                                    )
                                : "Failed to import files";

                        // Create a simple message modal
                        var messageModal = new StandardModal("IMPORT COMPLETE");
                        var messageText = new TextBlock
                        {
                            Text = message,
                            FontSize = 16,
                            Margin = new Avalonia.Thickness(20),
                            TextAlignment = Avalonia.Media.TextAlignment.Center,
                        };
                        messageModal.SetContent(messageText);
                        messageModal.BackClicked += (s, ev) =>
                        {
                            mainMenu.HideModalContent();
                            // Re-show the tools modal
                            mainMenu.ShowToolsModal();
                        };
                        mainMenu.ShowModalContent(messageModal, "IMPORT COMPLETE");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("ToolsModal", $"Failed to import files: {ex.Message}");
                }
            }
        }

        private void OnWordListsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();

                // Show word lists modal using ModalHelper extension
                mainMenu.ShowWordListsModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }

        private void OnCreditsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();

                // Show credits modal using ModalHelper extension
                mainMenu.ShowCreditsModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }

        private void OnAudioVisualizerSettingsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();

            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();

                // Show audio visualizer settings modal
                mainMenu.ShowAudioVisualizerSettingsModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }

        private void OnNukeEverythingClick(object? sender, RoutedEventArgs e)
        {
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu == null)
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
                return;
            }

            // Create confirmation modal
            var confirmModal = new StandardModal("⚠️ CONFIRM NUKE ⚠️");
            var confirmPanel = new StackPanel { Spacing = 20, Margin = new Avalonia.Thickness(20) };

            confirmPanel.Children.Add(
                new TextBlock
                {
                    Text = "This will DELETE ALL:",
                    FontSize = 18,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    Foreground = Avalonia.Media.Brushes.Red,
                }
            );

            confirmPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        "• All filter files in JsonItemFilters/\n• All search results in SearchResults/\n\nThis action CANNOT be undone!",
                    FontSize = 16,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                }
            );

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 20,
                Margin = new Avalonia.Thickness(0, 20, 0, 0),
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Classes = { "btn-green" },
                MinWidth = 100,
                MinHeight = 40,
            };

            var nukeButton = new Button
            {
                Content = "🔥 NUKE IT ALL 🔥",
                Classes = { "btn-red" },
                MinWidth = 150,
                MinHeight = 40,
                Background = this.FindResource("Red") as Avalonia.Media.IBrush,
            };

            cancelButton.Click += (s, ev) =>
            {
                mainMenu.HideModalContent();
                mainMenu.ShowToolsModal();
            };

            nukeButton.Click += (s, ev) =>
            {
                try
                {
                    int deletedFilters = 0;
                    int deletedResults = 0;

                    // Delete all files in JsonItemFilters
                    var filtersDir = AppPaths.FiltersDir;
                    if (Directory.Exists(filtersDir))
                    {
                        var filterFiles = Directory.GetFiles(filtersDir, "*.json");
                        foreach (var file in filterFiles)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedFilters++;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    "NukeEverything",
                                    $"Failed to delete {file}: {ex.Message}"
                                );
                            }
                        }
                    }

                    // Delete all files in SearchResults
                    var resultsDir = AppPaths.SearchResultsDir;
                    if (Directory.Exists(resultsDir))
                    {
                        var resultFiles = Directory.GetFiles(
                            resultsDir,
                            "*.*",
                            SearchOption.AllDirectories
                        );
                        foreach (var file in resultFiles)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedResults++;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    "NukeEverything",
                                    $"Failed to delete {file}: {ex.Message}"
                                );
                            }
                        }

                        // Also delete subdirectories
                        var subdirs = Directory.GetDirectories(resultsDir);
                        foreach (var dir in subdirs)
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(
                                    "NukeEverything",
                                    $"Failed to delete directory {dir}: {ex.Message}"
                                );
                            }
                        }
                    }

                    // Show results
                    mainMenu.HideModalContent();

                    var resultModal = new StandardModal("💥 NUKE COMPLETE 💥");
                    var resultText = new TextBlock
                    {
                        Text =
                            $"Deleted:\n{deletedFilters} filter files\n{deletedResults} search result files\n\npifreak loves you!",
                        FontSize = 16,
                        Margin = new Avalonia.Thickness(20),
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                    };
                    resultModal.SetContent(resultText);
                    resultModal.BackClicked += (s, ev) =>
                    {
                        mainMenu.HideModalContent();
                        mainMenu.ShowToolsModal();
                    };
                    mainMenu.ShowModalContent(resultModal, "NUKE COMPLETE");

                    DebugLogger.Log(
                        "NukeEverything",
                        $"Nuked {deletedFilters} filters and {deletedResults} results"
                    );
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("NukeEverything", $"Nuke operation failed: {ex.Message}");

                    mainMenu.HideModalContent();
                    var errorModal = new StandardModal("ERROR");
                    var errorText = new TextBlock
                    {
                        Text = $"Nuke operation failed:\n{ex.Message}",
                        FontSize = 16,
                        Margin = new Avalonia.Thickness(20),
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        Foreground = Avalonia.Media.Brushes.Red,
                    };
                    errorModal.SetContent(errorText);
                    errorModal.BackClicked += (s, ev) =>
                    {
                        mainMenu.HideModalContent();
                        mainMenu.ShowToolsModal();
                    };
                    mainMenu.ShowModalContent(errorModal, "ERROR");
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(nukeButton);
            confirmPanel.Children.Add(buttonPanel);

            confirmModal.SetContent(confirmPanel);
            mainMenu.ShowModalContent(confirmModal, "CONFIRM NUKE");
        }
    }
}
