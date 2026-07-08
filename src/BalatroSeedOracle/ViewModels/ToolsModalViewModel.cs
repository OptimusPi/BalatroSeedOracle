using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using Motely.Filters.Jaml;

namespace BalatroSeedOracle.ViewModels
{
    public partial class ToolsModalViewModel : ObservableObject
    {
        private readonly UserProfileService? _userProfileService;
        private readonly IConfigurationService? _configurationService;
        private readonly IFilterService? _filterService;
        private readonly IFilterCacheService? _filterCacheService;
        private readonly IPlatformServices? _platformServices;
        private readonly IModalHost? _modalHost;

        [ObservableProperty]
        private bool _supportsAudio = false;

        public ToolsModalViewModel(
            UserProfileService? userProfileService = null,
            IConfigurationService? configurationService = null,
            IFilterService? filterService = null,
            IFilterCacheService? filterCacheService = null,
            IPlatformServices? platformServices = null,
            IModalHost? modalHost = null)
        {
            _userProfileService = userProfileService;
            _configurationService = configurationService;
            _filterService = filterService;
            _filterCacheService = filterCacheService;
            _platformServices = platformServices;
            _modalHost = modalHost;

            SupportsAudio = _platformServices?.SupportsAudio == true;
        }

        [RelayCommand]
        private async Task ImportFilesAsync()
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null);
                
                if (topLevel == null)
                {
                    DebugLogger.LogError("ToolsModalViewModel", "Could not get TopLevel for file picker");
                    return;
                }

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Import Filter Configuration",
                        AllowMultiple = true,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                            new FilePickerFileType("YAML Files") { Patterns = new[] { "*.jaml", "*.yaml" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
                        },
                    }
                );

                if (files.Count == 0)
                    return;

                var configurationService = _configurationService ?? ServiceHelper.GetRequiredService<IConfigurationService>();
                var filterService = _filterService ?? ServiceHelper.GetRequiredService<IFilterService>();
                var filterCache = _filterCacheService ?? ServiceHelper.GetService<IFilterCacheService>();

                int successCount = 0;
                int failCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        if (file is not IStorageFile storageFile)
                            continue;

                        var extension = Path.GetExtension(storageFile.Name).ToLowerInvariant();
                        if (extension != ".json" && extension != ".jaml" && extension != ".yaml")
                            continue;

                        string text;
                        await using (var stream = await storageFile.OpenReadAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            text = await reader.ReadToEndAsync().ConfigureAwait(false);
                        }

                        JamlConfig? config;
                        if (!JamlConfigLoader.TryLoad(text, out config, out var parseError) || config == null)
                        {
                            DebugLogger.LogError("ToolsModalViewModel", $"Failed to parse {storageFile.Name}: {parseError ?? "Unknown error"}");
                            failCount++;
                            continue;
                        }

                        var baseName = !string.IsNullOrWhiteSpace(config.Name)
                            ? config.Name
                            : Path.GetFileNameWithoutExtension(storageFile.Name);
                        var destKey = filterService.GenerateFilterFileName(baseName);

                        var saved = await configurationService.SaveFilterAsync(destKey, config).ConfigureAwait(false);
                        if (!saved)
                        {
                            DebugLogger.LogError("ToolsModalViewModel", $"Failed to save imported filter: {storageFile.Name}");
                            failCount++;
                            continue;
                        }

                        filterCache?.Initialize();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("ToolsModalViewModel", $"Failed to import file {file.Name}: {ex.Message}");
                        failCount++;
                    }
                }

                var message = successCount > 0
                    ? $"Successfully imported {successCount} file(s)" + (failCount > 0 ? $"\n{failCount} file(s) failed to import" : "")
                    : "Failed to import files";

                await ModalHelper.ShowSuccessAsync("IMPORT COMPLETE", message);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ToolsModalViewModel", $"ImportFilesAsync: {ex.Message}");
                await ModalHelper.ShowErrorAsync("ERROR", $"Failed to import files:\n{ex.Message}");
            }
        }

        [RelayCommand]
        private void ShowWordLists()
        {
            _modalHost?.HideModal();
            _modalHost?.ShowWordListsModal();
        }

        [RelayCommand]
        private void ShowCredits()
        {
            _modalHost?.HideModal();
            _modalHost?.ShowCreditsModal();
        }

        [RelayCommand]
        private void ShowAudioVisualizerSettings()
        {
            if (!SupportsAudio)
                return;

            _modalHost?.HideModal();
            _modalHost?.ShowAudioVisualizerSettingsModal();
        }

        [RelayCommand]
        private void OpenMyWebsite()
        {
            OpenWebView("My Website", new Uri("https://optimuspi.workers.dev/", UriKind.Absolute));
        }

        [RelayCommand]
        private void OpenBalatroSite()
        {
            OpenWebView("Balatro", new Uri("https://optimuspi.workers.dev/", UriKind.Absolute));
        }

        [RelayCommand]
        private void OpenWebApp()
        {
            OpenWebView("Web App", new Uri("http://localhost:3141/BSO/", UriKind.Absolute));
        }

        private void OpenWebView(string title, Uri source)
        {
            try
            {
                var dialog = new Avalonia.Controls.NativeWebDialog
                {
                    Title = title,
                    CanUserResize = true,
                    Source = source,
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ToolsModalViewModel", $"WebView failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NukeEverythingAsync()
        {
            var confirmed = await ModalHelper.ShowConfirmationAsync(
                "⚠️ CONFIRM NUKE ⚠️",
                "This will DELETE ALL:\n\n• All filter files in JsonFilters/ and JamlFilters/\n• All search results in SearchResults/\n\nThis action CANNOT be undone!"
            );

            if (!confirmed)
                return;

            try
            {
                int deletedFilters = 0;
                int deletedResults = 0;

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
                            DebugLogger.LogError("ToolsModalViewModel", $"Failed to delete {file}: {ex.Message}");
                        }
                    }
                }

                var resultsDir = AppPaths.SearchResultsDir;
                if (Directory.Exists(resultsDir))
                {
                    var resultFiles = Directory.GetFiles(resultsDir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in resultFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedResults++;
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("ToolsModalViewModel", $"Failed to delete {file}: {ex.Message}");
                        }
                    }

                    var subdirs = Directory.GetDirectories(resultsDir);
                    foreach (var dir in subdirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError("ToolsModalViewModel", $"Failed to delete directory {dir}: {ex.Message}");
                        }
                    }
                }

                await ModalHelper.ShowSuccessAsync(
                    "💥 NUKE COMPLETE 💥",
                    $"Deleted:\n{deletedFilters} filter files\n{deletedResults} search result files\n\npifreak loves you!"
                );

                DebugLogger.Log("ToolsModalViewModel", $"Nuked {deletedFilters} filters and {deletedResults} results");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ToolsModalViewModel", $"Nuke operation failed: {ex.Message}");
                await ModalHelper.ShowErrorAsync("ERROR", $"Nuke operation failed:\n{ex.Message}");
            }
        }
    }
}
