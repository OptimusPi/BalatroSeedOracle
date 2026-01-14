using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BalatroSeedOracle.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Motely.Filters;

#pragma warning disable CS0618 // Suppress obsolete warnings for DataObject/DragDrop - new DataTransfer API not fully available in Avalonia 11.3

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterSelectionModal : UserControl
    {
        public FilterSelectionModalViewModel? ViewModel =>
            DataContext as FilterSelectionModalViewModel;

        public event EventHandler? CloseRequested;

        public FilterSelectionModal()
        {
            InitializeComponent();

            // Subscribe to DataContext changes to wire up the ViewModel event
            this.DataContextChanged += OnDataContextChanged;

            // Wire up deck/stake images when loaded
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Load default deck (Red with White Stake) - SpriteService expects SHORT deck names!
            LoadDeckAndStake("Red", "White");

            // Wire up drag-drop for import zone
            if (ImportDropZone != null)
            {
                ImportDropZone.AddHandler(DragDrop.DropEvent, OnFilterDrop);
                ImportDropZone.AddHandler(DragDrop.DragOverEvent, OnFilterDragOver);
            }
        }

        private void OnFilterDragOver(object? sender, DragEventArgs e)
        {
            // Check if files are being dragged
            e.DragEffects = e.Data.GetFiles()?.Any() == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void OnFilterDrop(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles();
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (file is not IStorageFile storageFile)
                    continue;

                var ext = Path.GetExtension(storageFile.Name).ToLowerInvariant();
                if (ext == ".jaml" || ext == ".json")
                {
                    await ImportFilterFile(storageFile);
                    break; // Only import first valid file
                }
            }

            e.Handled = true;
        }

        private void LoadDeckAndStake(string deckName, string stakeName)
        {
            // Deck/stake images are now handled via ViewModel bindings (DeckCardImage property)
            // This method is kept for compatibility but no longer manipulates UI directly
            DebugLogger.Log(
                "FilterSelectionModal",
                $"Deck/stake selection: {deckName} with {stakeName} stake (handled via ViewModel binding)"
            );
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            DebugLogger.Log("FilterSelectionModal", "🔵 DataContext changed!");

            // Unsubscribe from old ViewModel if any
            if (sender is FilterSelectionModal modal)
            {
                var oldVm = modal.DataContext as FilterSelectionModalViewModel;
                if (oldVm != null)
                {
                    DebugLogger.Log("FilterSelectionModal", "  Unsubscribing from old ViewModel");
                    oldVm.ModalCloseRequested -= OnModalCloseRequested;
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                    oldVm.DeleteConfirmationRequested -= OnDeleteConfirmationRequested;
                }

                // Subscribe to new ViewModel
                var newVm = modal.DataContext as FilterSelectionModalViewModel;
                if (newVm != null)
                {
                    DebugLogger.Log(
                        "FilterSelectionModal",
                        $"  Subscribing to new ViewModel - EnableSearch={newVm.EnableSearch}"
                    );
                    newVm.ModalCloseRequested += OnModalCloseRequested;
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
                    newVm.DeleteConfirmationRequested += OnDeleteConfirmationRequested;

                    // Load initial deck/stake if filter is already selected
                    if (newVm.SelectedFilter != null)
                    {
                        UpdateDeckAndStake(newVm.SelectedFilter);
                    }
                }
                else
                {
                    DebugLogger.LogError(
                        "FilterSelectionModal",
                        "  ❌ NEW DATACONTEXT IS NOT FilterSelectionModalViewModel!"
                    );
                }
            }
        }

        private async void OnDeleteConfirmationRequested(object? sender, string filterName)
        {
            // Get the parent window
            var parentWindow = Avalonia.Controls.TopLevel.GetTopLevel(this) as Window;

            if (parentWindow == null)
            {
                throw new InvalidOperationException(
                    "FilterSelectionModal must be shown from a Window context!"
                );
            }

            // Create styled confirmation dialog
            var dialog = new Window
            {
                Width = 450,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Avalonia.Media.Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            };

            bool? result = null;

            var yesButton = new Button
            {
                Content = "Yes",
                Classes = { "btn-red" },
                MinWidth = 120,
                Height = 45,
            };

            var noButton = new Button
            {
                Content = "No",
                Classes = { "btn-blue" },
                MinWidth = 120,
                Height = 45,
            };

            yesButton.Click += (s, ev) =>
            {
                result = true;
                dialog.Close();
            };

            noButton.Click += (s, ev) =>
            {
                result = false;
                dialog.Close();
            };

            // Main container
            var mainBorder = new Border
            {
                Background = this.FindResource("DarkBorder") as Avalonia.Media.IBrush,
                BorderBrush = this.FindResource("LightGrey") as Avalonia.Media.IBrush,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(16),
            };

            var mainGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

            // Title bar
            var titleBar = new Border
            {
                [Grid.RowProperty] = 0,
                Background = this.FindResource("ModalGrey") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(20, 12),
            };

            titleBar.Child = new TextBlock
            {
                Text = "Delete Filter?",
                FontSize = 24,
                Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            mainGrid.Children.Add(titleBar);

            // Content area
            var contentBorder = new Border
            {
                [Grid.RowProperty] = 1,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                Padding = new Thickness(24),
            };

            var contentStack = new StackPanel { Spacing = 12 };

            // Warning message with icon
            var warningPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                MaxWidth = 380, // Constrain width to prevent overflow
            };

            var warningIcon = new TextBlock
            {
                Text = "⚠",
                FontSize = 32,
                Foreground = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.Parse("#FF6B6B")
                ),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0),
            };

            var messageText = new TextBlock
            {
                Text = $"Are you sure you want to delete '{filterName}'?",
                FontSize = 14,
                Foreground = this.FindResource("White") as Avalonia.Media.IBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320, // Leave room for icon
            };

            warningPanel.Children.Add(warningIcon);
            warningPanel.Children.Add(messageText);
            contentStack.Children.Add(warningPanel);

            contentStack.Children.Add(
                new TextBlock
                {
                    Text = "This cannot be undone.",
                    FontSize = 14,
                    Foreground = this.FindResource("LightGrey") as Avalonia.Media.IBrush,
                    FontStyle = FontStyle.Italic,
                }
            );

            contentBorder.Child = contentStack;
            mainGrid.Children.Add(contentBorder);

            // Button area
            var buttonBorder = new Border
            {
                [Grid.RowProperty] = 2,
                Background = this.FindResource("DarkBackground") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(0, 0, 14, 14),
                Padding = new Thickness(20, 12, 20, 20),
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
            };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            buttonBorder.Child = buttonPanel;
            mainGrid.Children.Add(buttonBorder);

            mainBorder.Child = mainGrid;
            dialog.Content = mainBorder;

            await dialog.ShowDialog(parentWindow);

            if (result == true && ViewModel != null)
            {
                await ViewModel.ConfirmDeleteAsync();
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (e.PropertyName == nameof(FilterSelectionModalViewModel.SelectedFilter))
            {
                var vm = sender as FilterSelectionModalViewModel;
                if (vm?.SelectedFilter != null)
                {
                    UpdateDeckAndStake(vm.SelectedFilter);
                }
            }
        }

        private void UpdateDeckAndStake(FilterBrowserItem filter)
        {
            // Extract deck and stake names with fallbacks (handle both null and empty strings)
            var deckName = string.IsNullOrWhiteSpace(filter.DeckName) ? "Red" : filter.DeckName;
            var stakeName = string.IsNullOrWhiteSpace(filter.StakeName)
                ? "White"
                : filter.StakeName;

            // SpriteService expects SHORT deck names (just "Red", not "Red Deck")
            // Filter JSON stores short names like "Red", "Anaglyph", etc.
            // NO need to add " Deck" suffix!

            DebugLogger.Log(
                "FilterSelectionModal",
                $"Loading deck: {deckName}, stake: {stakeName}"
            );
            LoadDeckAndStake(deckName, stakeName);
        }

        private void OnModalCloseRequested(object? sender, EventArgs e)
        {
            DebugLogger.Log("FilterSelectionModal", "Close requested from ViewModel");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Shows the modal as a dialog and returns the result
        /// </summary>
        /// <param name="parent">Parent window</param>
        /// <returns>True if confirmed, false if cancelled</returns>
        public async System.Threading.Tasks.Task<bool> ShowDialog(Window parent)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            // Add to parent window
            if (parent.Content is Panel panel)
            {
                panel.Children.Add(this);

                void OnClose(object? s, EventArgs e)
                {
                    panel.Children.Remove(this);
                    CloseRequested -= OnClose;
                    tcs.SetResult(true);
                }

                CloseRequested += OnClose;
            }
            else
            {
                DebugLogger.LogError(
                    "FilterSelectionModal",
                    "Parent window content is not a Panel"
                );
                tcs.SetResult(false);
            }

            return await tcs.Task;
        }

        private async void OnBrowseFilterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    DebugLogger.LogError("FilterSelectionModal", "TopLevel not found for file picker");
                    return;
                }

                // Check if StorageProvider supports file operations (important for browser)
                if (!topLevel.StorageProvider.CanOpen)
                {
                    await MsBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandard("Not Supported", "File opening is not supported in this environment.")
                        .ShowAsync();
                    return;
                }

                DebugLogger.Log("FilterSelectionModal", "Opening file picker...");
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Import Filter",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Filter Files")
                            {
                                Patterns = new[] { "*.jaml", "*.json" }
                            },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } },
                        },
                    }
                );

                DebugLogger.Log("FilterSelectionModal", $"File picker returned {files.Count} files");

                if (files.Count > 0)
                {
                    if (files[0] is IStorageFile storageFile)
                    {
                        DebugLogger.Log("FilterSelectionModal", $"Selected file: {storageFile.Name}");
                        await ImportFilterFile(storageFile);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelectionModal", $"Error in OnBrowseFilterClick: {ex.Message}");
                DebugLogger.LogError("FilterSelectionModal", $"Stack trace: {ex.StackTrace}");
                
                // Show error message to user
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Error", $"Failed to open file picker: {ex.Message}")
                    .ShowAsync();
            }
        }

        private async System.Threading.Tasks.Task ImportFilterFile(IStorageFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                if (extension != ".jaml" && extension != ".json")
                {
                    DebugLogger.LogError(
                        "FilterSelectionModal",
                        $"Invalid file type: {extension}. Expected .jaml or .json"
                    );
                    return;
                }

                DebugLogger.Log("FilterSelectionModal", $"Reading file content: {file.Name}");
                string text;
                await using (var stream = await file.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    // Remove ConfigureAwait(false) to stay on UI thread context
                    text = await reader.ReadToEndAsync();
                }

                DebugLogger.Log("FilterSelectionModal", $"Read {text.Length} chars. Parsing...");

                MotelyJsonConfig? config;
                if (extension == ".jaml")
                {
                    if (!Motely.JamlConfigLoader.TryLoadFromJamlString(text, out config, out var parseError) || config == null)
                    {
                        DebugLogger.LogError(
                            "FilterSelectionModal",
                            $"Failed to parse JAML: {parseError ?? "Unknown error"}"
                        );
                        return;
                    }
                }
                else
                {
                    config = System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        text,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                        }
                    );

                    if (config == null)
                    {
                        DebugLogger.LogError("FilterSelectionModal", "Failed to deserialize JSON filter");
                        return;
                    }
                }

                DebugLogger.Log("FilterSelectionModal", $"Parsed config: {config.Name}. Saving...");

                var configurationService = ServiceHelper.GetRequiredService<IConfigurationService>();
                var filterService = ServiceHelper.GetRequiredService<IFilterService>();

                var baseName = !string.IsNullOrWhiteSpace(config.Name)
                    ? config.Name
                    : Path.GetFileNameWithoutExtension(file.Name);
                var destKey = filterService.GenerateFilterFileName(baseName);
                
                // Remove ConfigureAwait(false) here too
                var saved = await configurationService.SaveFilterAsync(destKey, config);
                if (!saved)
                {
                    DebugLogger.LogError("FilterSelectionModal", "Failed to save imported filter");
                    return;
                }

                DebugLogger.Log("FilterSelectionModal", $"Imported filter as: {destKey}");

                // Refresh the filter list
                if (ViewModel != null)
                {
                    DebugLogger.Log("FilterSelectionModal", "Refreshing filter list...");
                    ViewModel.FilterList.RefreshFilters();
                    
                    // Auto-select the imported filter if possible
                    // We need to reload the filters first, then find the one we just saved
                    // This logic might be better in the ViewModel, but let's just refresh for now.
                }
                else
                {
                    DebugLogger.Log("FilterSelectionModal", "WARNING: ViewModel is null, cannot refresh list");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("FilterSelectionModal", $"Failed to import filter: {ex.Message}");
                DebugLogger.LogError("FilterSelectionModal", $"Stack trace: {ex.StackTrace}");
                
                await MsBox.Avalonia.MessageBoxManager
                    .GetMessageBoxStandard("Import Error", $"Failed to import filter: {ex.Message}")
                    .ShowAsync();
            }
        }
    }
}
