using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Oracle.Helpers;

namespace Oracle.Views.Modals
{
    public partial class ToolsModal : UserControl
    {
        public ToolsModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnImportFilesClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Filter Configuration",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            
            if (files.Count > 0)
            {
                try
                {
                    // Create JsonItemConfigs directory if it doesn't exist
                    var jsonConfigsDir = Path.Combine(Directory.GetCurrentDirectory(), "JsonItemConfigs");
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
                            DebugLogger.LogError("ToolsModal", $"Failed to import file {file.Name}: {ex.Message}");
                            failCount++;
                        }
                    }
                    
                    // Show result message
                    var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                    if (mainMenu != null)
                    {
                        var message = successCount > 0 
                            ? $"Successfully imported {successCount} file(s)" + (failCount > 0 ? $"\n{failCount} file(s) failed to import" : "")
                            : "Failed to import files";
                            
                        // Create a simple message modal
                        var messageModal = new StandardModal("IMPORT COMPLETE");
                        var messageText = new TextBlock
                        {
                            Text = message,
                            FontSize = 16,
                            Margin = new Avalonia.Thickness(20),
                            TextAlignment = Avalonia.Media.TextAlignment.Center
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

        private void OnAnalyzerClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            
            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show analyzer modal using ModalHelper extension
                mainMenu.ShowAnalyzerModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
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
    }
}