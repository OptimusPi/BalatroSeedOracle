using System;
using System.IO;
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
        [ObservableProperty]
        private bool _supportsAudio = true;

        [RelayCommand]
        private async Task ImportFilesAsync()
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
                        new FilePickerFileType("JAML Filters") { Patterns = new[] { "*.jaml" } },
                    },
                }
            );

            if (files.Count == 0)
                return;

            int successCount = 0;
            int failCount = 0;

            foreach (var file in files)
            {
                if (file is not IStorageFile storageFile)
                    continue;

                string text;
                await using (var stream = await storageFile.OpenReadAsync())
                using (var reader = new StreamReader(stream))
                {
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (!JamlConfigLoader.TryLoad(text, out var config, out var parseError) || config == null)
                {
                    DebugLogger.LogError("ToolsModalViewModel", $"Failed to parse {storageFile.Name}: {parseError ?? "Unknown error"}");
                    failCount++;
                    continue;
                }

                var baseName = !string.IsNullOrWhiteSpace(config.Name)
                    ? config.Name
                    : Path.GetFileNameWithoutExtension(storageFile.Name);

                Directory.CreateDirectory(FilterFiles.Dir);
                FilterFiles.Save(config, FilterFiles.Resolve(baseName));
                successCount++;
            }

            var message = successCount > 0
                ? $"Successfully imported {successCount} file(s)" + (failCount > 0 ? $"\n{failCount} file(s) failed to import" : "")
                : "Failed to import files";

            await ModalHelper.ShowSuccessAsync("IMPORT COMPLETE", message);
        }

        [RelayCommand]
        private void ShowWordLists()
        {
            var menu = App.GetService<Views.BalatroMainMenu>();
            menu?.HideModal();
            menu?.ShowWordListsModal();
        }

        [RelayCommand]
        private void ShowCredits()
        {
            var menu = App.GetService<Views.BalatroMainMenu>();
            menu?.HideModal();
            menu?.ShowCreditsModal();
        }

        [RelayCommand]
        private void ShowAudioVisualizerSettings()
        {
            var menu = App.GetService<Views.BalatroMainMenu>();
            menu?.HideModal();
            menu?.ShowAudioVisualizerSettingsModal();
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
            var dialog = new Avalonia.Controls.NativeWebDialog
            {
                Title = title,
                CanUserResize = true,
                Source = source,
            };
            dialog.Show();
        }

        [RelayCommand]
        private async Task NukeEverythingAsync()
        {
            var confirmed = await ModalHelper.ShowConfirmationAsync(
                "⚠️ CONFIRM NUKE ⚠️",
                "This will DELETE ALL:\n\n• All filter files in JamlFilters/\n• All search results in SearchResults/\n\nThis action CANNOT be undone!"
            );

            if (!confirmed)
                return;

            int deletedFilters = 0;
            int deletedResults = 0;

            if (Directory.Exists("JamlFilters"))
            {
                foreach (var file in Directory.GetFiles("JamlFilters", "*.jaml"))
                {
                    File.Delete(file);
                    deletedFilters++;
                }
            }

            if (Directory.Exists("SearchResults"))
            {
                deletedResults = Directory.GetFiles("SearchResults", "*.*", SearchOption.AllDirectories).Length;
                Directory.Delete("SearchResults", true);
            }

            await ModalHelper.ShowSuccessAsync(
                "💥 NUKE COMPLETE 💥",
                $"Deleted:\n{deletedFilters} filter files\n{deletedResults} search result files\n\npifreak loves you!"
            );

            DebugLogger.Log("ToolsModalViewModel", $"Nuked {deletedFilters} filters and {deletedResults} results");
        }
    }
}
