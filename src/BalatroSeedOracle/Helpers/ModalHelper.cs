using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Threading;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views.Modals;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// Helper methods for modal creation and management
    /// </summary>
    public static class ModalHelper
    {
        /// <summary>
        /// Creates and shows a standard modal with the given content
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="title">The modal title</param>
        /// <param name="content">The content to display in the modal</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowModal(
            this Views.BalatroMainMenu menu,
            string title,
            UserControl content
        )
        {
            var modal = new StandardModal(title);
            modal.SetContent(content);
            modal.BackClicked += (s, ev) => menu.HideModalContent();
            menu.ShowModalContent(modal, title);
            return modal;
        }

        /// <summary>
        /// Creates and shows the filter selection modal (gateway to Search, Designer, or Analyzer)
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="enableSearch">Show SEARCH button</param>
        /// <param name="enableEdit">Show EDIT button</param>
        /// <param name="enableCopy">Show COPY button</param>
        /// <param name="enableDelete">Show DELETE button</param>
        /// <param name="enableAnalyze">Show ANALYZE button</param>
        /// <returns>The result from the modal</returns>
        public static async Task<FilterSelectionResult> ShowFilterSelectionModal(
            this Views.BalatroMainMenu menu,
            bool enableSearch = false,
            bool enableEdit = false,
            bool enableCopy = false,
            bool enableDelete = false,
            bool enableAnalyze = false
        )
        {
            var modal = new FilterSelectionModal();
            var vm = new FilterSelectionModalViewModel(
                enableSearch,
                enableEdit,
                enableCopy,
                enableDelete,
                enableAnalyze
            );
            modal.DataContext = vm;

            var result = await modal.ShowDialog(menu.GetWindow());
            return vm.Result;
        }

        /// <summary>
        /// Creates and shows the filter designer modal (direct entry - includes filter list)
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowFiltersModal(this Views.BalatroMainMenu menu)
        {
            // Always show the original drag-and-drop FiltersModal
            var filtersContent = new Views.Modals.FiltersModal();
            return menu.ShowModal("FILTER DESIGNER", filtersContent);
        }

        /// <summary>
        /// Creates and shows the filter designer modal with a specific filter loaded for editing
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="config">The filter config to load for editing</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowFiltersModal(this Views.BalatroMainMenu menu, Motely.Filters.MotelyJsonConfig config)
        {
            var filtersContent = new Views.Modals.FiltersModal();
            
            // Load the filter into the modal for editing
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (filtersContent.ViewModel != null)
                    {
                        await filtersContent.ViewModel.LoadFilterForEditing(config);
                    }
                });
            });
            
            return menu.ShowModal("FILTER DESIGNER", filtersContent);
        }

        /// <summary>
        /// Creates and shows a search modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="configPath">Optional config path to load</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModal(
            this Views.BalatroMainMenu menu,
            string? configPath = null
        )
        {
            try
            {
                var searchContent = new SearchModal();

                // Handle modal close
                searchContent.CloseRequested += (sender, e) => menu.HideModalContent();

                // Handle desktop icon creation when modal closes with active search
                searchContent.ViewModel.CreateShortcutRequested += (sender, cfgPath) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "ModalHelper",
                        $"Desktop icon requested for config: {cfgPath}"
                    );
                    // Get the search ID from the modal
                    var searchId = searchContent.ViewModel.CurrentSearchId;
                    if (!string.IsNullOrEmpty(searchId))
                    {
                        menu.ShowSearchDesktopIcon(searchId, cfgPath);
                    }
                };

                if (!string.IsNullOrEmpty(configPath))
                {
                    // Load filter async and THEN navigate to search tab
                    _ = Task.Run(async () =>
                    {
                        await searchContent.ViewModel.LoadFilterAsync(configPath);
                        // AUTO-NAVIGATE: Take user to search tab AFTER filter loads!
                        Dispatcher.UIThread.Post(() =>
                            searchContent.ViewModel.SelectedTabIndex = 1
                        );
                    });
                }
                else
                {
                    // No filter to load, go to search tab immediately
                    searchContent.ViewModel.SelectedTabIndex = 1; // Search tab
                }
                return menu.ShowModal("🎰 SEED SEARCH", searchContent);
            }
            catch (Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "ModalHelper",
                    $"Failed to create SearchModal: {ex}"
                );
                throw;
            }
        }

        /// <summary>
        /// Creates and shows a search modal with a config object (no temp files!)
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="config">The MotelyJsonConfig object to search with</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModalWithConfig(
            this Views.BalatroMainMenu menu,
            Motely.Filters.MotelyJsonConfig config
        )
        {
            // This method should not be used - filters must be saved first!
            throw new NotSupportedException(
                "Filters must be saved before searching. Use ShowSearchModal with a file path instead."
            );
        }

        /// <summary>
        /// Creates and shows a tools modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowToolsModal(this Views.BalatroMainMenu menu)
        {
            var ToolView = new ToolsModal();
            return menu.ShowModal("MORE", ToolView);
        }

        /// <summary>
        /// Creates and shows a search modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModal(this Views.BalatroMainMenu menu)
        {
            var searchModal = new SearchModal();

            // DON'T auto-navigate for fresh launches - let user select filter first

            // Handle desktop icon creation when modal closes with active search
            if (searchModal.ViewModel != null)
            {
                searchModal.ViewModel.CreateShortcutRequested += (sender, configPath) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "ModalHelper",
                        $"Desktop icon requested for config: {configPath}"
                    );
                    // Get the search ID from the modal
                    var searchId = searchModal.ViewModel.CurrentSearchId;
                    if (!string.IsNullOrEmpty(searchId))
                    {
                        menu.ShowSearchDesktopIcon(searchId, configPath);
                    }
                };
            }

            return menu.ShowModal("MOTELY SEARCH", searchModal);
        }

        /// <summary>
        /// Creates and shows a search modal for an existing search instance
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <param name="searchId">The ID of the search instance to reconnect to</param>
        /// <param name="configPath">The config path for context</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowSearchModalForInstance(
            this Views.BalatroMainMenu menu,
            string searchId,
            string? configPath = null
        )
        {
            var searchModal = new SearchModal();

            // Remove the desktop widget that opened this modal
            menu.RemoveSearchDesktopIcon(searchId);

            // Set the search ID so the modal can reconnect
            if (searchModal.ViewModel != null)
            {
                _ = searchModal.ViewModel.ConnectToExistingSearch(searchId);

                // AUTO-NAVIGATE: Take user directly to search tab after connection!
                Dispatcher.UIThread.Post(() => searchModal.ViewModel.SelectedTabIndex = 1);

                // Handle desktop icon creation when modal closes with active search
                searchModal.ViewModel.CreateShortcutRequested += (sender, cfgPath) =>
                {
                    BalatroSeedOracle.Helpers.DebugLogger.Log(
                        "ModalHelper",
                        $"Desktop icon requested for search: {searchId}"
                    );
                    menu.ShowSearchDesktopIcon(searchId, cfgPath);
                };
            }

            return menu.ShowModal("MOTELY SEARCH", searchModal);
        }

        /// <summary>
        /// Creates and shows a word lists modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowWordListsModal(this Views.BalatroMainMenu menu)
        {
            var wordListsView = new WordListsModal();
            return menu.ShowModal("WORD LISTS", wordListsView);
        }

        /// <summary>
        /// Creates and shows a credits modal
        /// </summary>
        /// <param name="menu">The main menu to show the modal on</param>
        /// <returns>The created modal</returns>
        public static StandardModal ShowCreditsModal(this Views.BalatroMainMenu menu)
        {
            var creditsView = new CreditsModal();
            return menu.ShowModal("CREDITS", creditsView);
        }

        /// <summary>
        /// Creates and shows the advanced audio visualizer settings modal
        /// Note: The ViewModel handles settings persistence; MainMenu handles applying to shader
        /// </summary>
        public static StandardModal ShowAudioVisualizerSettingsModal(
            this Views.BalatroMainMenu menu
        )
        {
            var audioVisualizerView = new AudioVisualizerSettingsModal();

            // Wire up ViewModel events to MainMenu so changes apply to the background shader immediately
            if (audioVisualizerView.ViewModel != null)
            {
                var vm = audioVisualizerView.ViewModel;

                // The ViewModel saves to UserProfile; MainMenu applies to shader for immediate feedback
                // ThemeChangedEvent removed - ApplyVisualizerTheme was empty stub

                vm.MainColorChangedEvent += (s, colorIndex) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"Advanced modal: Main color changed to {colorIndex}"
                    );
                    menu.ApplyMainColor(colorIndex);
                };

                vm.AccentColorChangedEvent += (s, colorIndex) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"Advanced modal: Accent color changed to {colorIndex}"
                    );
                    menu.ApplyAccentColor(colorIndex);
                };

                // AudioIntensityChangedEvent removed - ApplyAudioIntensity was empty stub
                // ParallaxStrengthChangedEvent removed - ApplyParallaxStrength was empty stub

                vm.TimeSpeedChangedEvent += (s, speed) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"Advanced modal: Time speed changed to {speed}"
                    );
                    menu.ApplyTimeSpeed(speed);
                };

                // Wire up shader debug controls
                vm.ShaderContrastChangedEvent += (s, contrast) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"[SHADER DEBUG] Contrast changed to {contrast}"
                    );
                    menu.ApplyShaderContrast(contrast);
                };

                vm.ShaderSpinAmountChangedEvent += (s, spinAmount) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"[SHADER DEBUG] Spin amount changed to {spinAmount}"
                    );
                    menu.ApplyShaderSpinAmount(spinAmount);
                };

                vm.ShaderZoomPunchChangedEvent += (s, zoom) =>
                {
                    DebugLogger.Log("ModalHelper", $"[SHADER DEBUG] Zoom punch changed to {zoom}");
                    menu.ApplyShaderZoomPunch(zoom);
                };

                vm.ShaderMelodySaturationChangedEvent += (s, saturation) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"[SHADER DEBUG] Melody saturation changed to {saturation}"
                    );
                    menu.ApplyShaderMelodySaturation(saturation);
                };

                // Wire up shader effect audio source mappings
                vm.ShadowFlickerSourceChangedEvent += (s, sourceIndex) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"Shadow flicker source changed to {sourceIndex}"
                    );
                    menu.ApplyShadowFlickerSource(sourceIndex);
                };

                vm.SpinSourceChangedEvent += (s, sourceIndex) =>
                {
                    DebugLogger.Log("ModalHelper", $"Spin source changed to {sourceIndex}");
                    menu.ApplySpinSource(sourceIndex);
                };

                vm.TwirlSourceChangedEvent += (s, sourceIndex) =>
                {
                    DebugLogger.Log("ModalHelper", $"Twirl source changed to {sourceIndex}");
                    menu.ApplyTwirlSource(sourceIndex);
                };

                vm.ZoomThumpSourceChangedEvent += (s, sourceIndex) =>
                {
                    DebugLogger.Log("ModalHelper", $"Zoom thump source changed to {sourceIndex}");
                    menu.ApplyZoomThumpSource(sourceIndex);
                };

                vm.ColorSaturationSourceChangedEvent += (s, sourceIndex) =>
                {
                    DebugLogger.Log(
                        "ModalHelper",
                        $"Color saturation source changed to {sourceIndex}"
                    );
                    menu.ApplyColorSaturationSource(sourceIndex);
                };

                // BeatPulseSourceChangedEvent removed - ApplyBeatPulseSource was empty stub

                // Range events (advanced)
                /*
                vm.ContrastRangeChangedEvent += (s, range) =>
                {
                    DebugLogger.Log("ModalHelper", $"Contrast range changed: {range.min} - {range.max}");
                    menu.ApplyContrastRange(range.min, range.max);
                };

                vm.SpinRangeChangedEvent += (s, range) =>
                {
                    DebugLogger.Log("ModalHelper", $"Spin range changed: {range.min} - {range.max}");
                    menu.ApplySpinAmountRange(range.min, range.max);
                };

                vm.TwirlRangeChangedEvent += (s, range) =>
                {
                    DebugLogger.Log("ModalHelper", $"Twirl range changed: {range.min} - {range.max}");
                    menu.ApplyTwirlSpeedRange(range.min, range.max);
                };

                vm.ZoomPunchRangeChangedEvent += (s, range) =>
                {
                    DebugLogger.Log("ModalHelper", $"Zoom punch range changed: {range.min} - {range.max}");
                    menu.ApplyZoomPunchRange(range.min, range.max);
                };

                vm.MelodySatRangeChangedEvent += (s, range) =>
                {
                    DebugLogger.Log("ModalHelper", $"Melody saturation range changed: {range.min} - {range.max}");
                    menu.ApplyMelodySatRange(range.min, range.max);
                };
                */
            }

            return menu.ShowModal("VISUALIZER SETTINGS", audioVisualizerView);
        }

        /// <summary>
        /// Shows a simple text input dialog and returns the entered text
        /// </summary>
        /// <param name="window">Parent window</param>
        /// <param name="title">Dialog title</param>
        /// <param name="prompt">Prompt text to show</param>
        /// <param name="defaultValue">Default value for the text box</param>
        /// <returns>The entered text, or null if cancelled</returns>
        public static async Task<string?> ShowTextInputDialogAsync(
            Window window,
            string title,
            string prompt,
            string defaultValue = ""
        )
        {
            var tcs = new TaskCompletionSource<string?>();

            var dialog = new Window
            {
                Width = 400,
                Height = 180,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Avalonia.Media.Brushes.Transparent,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            };

            var textBox = new TextBox
            {
                Text = defaultValue,
                FontSize = 14,
                Margin = new Avalonia.Thickness(0, 8, 0, 0),
            };

            var okButton = new Button
            {
                Content = "Save",
                Classes = { "btn-blue" },
                MinWidth = 100,
                Height = 40,
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Classes = { "btn-red" },
                MinWidth = 100,
                Height = 40,
            };

            okButton.Click += (s, e) =>
            {
                tcs.TrySetResult(textBox.Text);
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                tcs.TrySetResult(null);
                dialog.Close();
            };

            // Build UI
            var mainBorder = new Border
            {
                Background = window.FindResource("DarkBorder") as Avalonia.Media.IBrush,
                BorderBrush = window.FindResource("LightGrey") as Avalonia.Media.IBrush,
                BorderThickness = new Avalonia.Thickness(3),
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(20),
            };

            var stack = new StackPanel { Spacing = 12 };

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                Foreground = window.FindResource("White") as Avalonia.Media.IBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });

            stack.Children.Add(new TextBlock
            {
                Text = prompt,
                FontSize = 14,
                Foreground = window.FindResource("LightGrey") as Avalonia.Media.IBrush,
            });

            stack.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 12,
                Margin = new Avalonia.Thickness(0, 8, 0, 0),
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            mainBorder.Child = stack;
            dialog.Content = mainBorder;

            // Focus text box when shown
            dialog.Opened += (s, e) => textBox.Focus();

            await dialog.ShowDialog(window);

            return await tcs.Task;
        }

        /// <summary>
        /// Creates a temp filter file for new filter creation
        /// </summary>
        private static async System.Threading.Tasks.Task<string> CreateTempFilter()
        {
            var filtersDir = AppPaths.FiltersDir;

            var tempPath = System.IO.Path.Combine(filtersDir, "_UNSAVED_CREATION.json");

            // Create basic empty filter structure
            var emptyFilter = new Motely.Filters.MotelyJsonConfig
            {
                Name = "New Filter",
                Description = "Created with Filter Designer",
                Author =
                    ServiceHelper.GetService<UserProfileService>()?.GetAuthorName() ?? "Unknown",
                DateCreated = System.DateTime.UtcNow,
                Must =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                Should =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
                MustNot =
                    new System.Collections.Generic.List<Motely.Filters.MotelyJsonConfig.MotleyJsonFilterClause>(),
            };

            var json = System.Text.Json.JsonSerializer.Serialize(
                emptyFilter,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System
                        .Text
                        .Json
                        .Serialization
                        .JsonIgnoreCondition
                        .WhenWritingNull,
                }
            );
            await System.IO.File.WriteAllTextAsync(tempPath, json);

            return tempPath;
        }

        /// <summary>
        /// Creates a cloned copy of an existing filter
        /// </summary>
        private static async System.Threading.Tasks.Task<string> CreateClonedFilter(
            string originalPath
        )
        {
            try
            {
                var originalJson = await System.IO.File.ReadAllTextAsync(originalPath);
                var config =
                    System.Text.Json.JsonSerializer.Deserialize<Motely.Filters.MotelyJsonConfig>(
                        originalJson
                    );

                if (config != null)
                {
                    // Update clone metadata
                    config.Name = $"{config.Name} (Copy)";
                    config.Author =
                        ServiceHelper.GetService<UserProfileService>()?.GetAuthorName()
                        ?? "Unknown";
                    config.DateCreated = System.DateTime.UtcNow;

                    var filtersDir = AppPaths.FiltersDir;
                    var clonedPath = System.IO.Path.Combine(filtersDir, $"{config.Name}.json");

                    var json = System.Text.Json.JsonSerializer.Serialize(
                        config,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = System
                                .Text
                                .Json
                                .Serialization
                                .JsonIgnoreCondition
                                .WhenWritingNull,
                        }
                    );
                    await System.IO.File.WriteAllTextAsync(clonedPath, json);

                    return clonedPath;
                }
            }
            catch (System.Exception ex)
            {
                BalatroSeedOracle.Helpers.DebugLogger.LogError(
                    "ModalHelper",
                    $"Failed to clone filter: {ex.Message}"
                );
            }

            return string.Empty;
        }
    }
}
