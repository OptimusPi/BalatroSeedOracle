using System;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the simplified dock settings modal (theme picker only)
    /// </summary>
    public partial class SettingsModalViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;
        private readonly SearchManager? _searchManager; // Nullable if service might fail
        private readonly IModalHost? _modalHost;
        private readonly IPlatformServices? _platformServices;

        [ObservableProperty]
        private int _visualizerTheme;

        // Search Engine Settings
        [ObservableProperty]
        private int _selectedSearchEngineIndex; // 0=Local, 1=Public, 2=Custom

        [ObservableProperty]
        private string _customRemoteUrl = "http://localhost:5000";

        [ObservableProperty]
        private bool _isCustomUrlVisible;

        partial void OnVisualizerThemeChanged(int value)
        {
            SaveVisualizerTheme();
        }

        partial void OnSelectedSearchEngineIndexChanged(int value)
        {
            IsCustomUrlVisible = value == 2;
            UpdateSearchEngine();
        }

        partial void OnCustomRemoteUrlChanged(string value)
        {
            if (SelectedSearchEngineIndex == 2)
                UpdateSearchEngine();
        }

        private void UpdateSearchEngine()
        {
            if (_searchManager is null) return;
            switch (SelectedSearchEngineIndex)
            {
                case 0:
                    _searchManager.SetEngine(_searchManager.LocalEngine);
                    break;
                case 1:
                    _searchManager.SetRemoteUrl("https://api.motely.gg");
                    break;
                case 2:
                    if (!string.IsNullOrWhiteSpace(CustomRemoteUrl))
                        _searchManager.SetRemoteUrl(CustomRemoteUrl);
                    break;
            }
        }

        public SettingsModalViewModel(
            IModalHost? modalHost = null,
            IPlatformServices? platformServices = null)
        {
            _userProfileService =
                App.GetService<UserProfileService>()
                ?? throw new InvalidOperationException("UserProfileService not available");

            // Allow null for previewer/design time, but log warning
            _searchManager = App.GetService<SearchManager>();
            if (_searchManager == null)
            {
                DebugLogger.Log("SettingsModalViewModel", "SearchManager not available");
            }

            _modalHost = modalHost;
            _platformServices = platformServices;

            LoadSettings();
        }

        #region Events

        public event EventHandler? CloseRequested;
        public event EventHandler? AdvancedSettingsRequested;
        public event EventHandler<int>? VisualizerThemeChanged;

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            var profile = _userProfileService.GetProfile();
            VisualizerTheme = profile.VisualizerSettings.ThemeIndex;

            if (_searchManager is not null)
            {
                if (_searchManager.ActiveEngine.IsLocal)
                {
                    SelectedSearchEngineIndex = 0;
                }
                else if (_searchManager.ActiveEngine.Name.Contains("api.motely.gg"))
                {
                    SelectedSearchEngineIndex = 1;
                }
                else
                {
                    SelectedSearchEngineIndex = 2;
                    IsCustomUrlVisible = true;
                }
            }

            DebugLogger.Log(
                "SettingsModalViewModel",
                $"Settings loaded - Visualizer theme: {VisualizerTheme}"
            );
        }

        private void SaveVisualizerTheme()
        {
            var profile = _userProfileService.GetProfile();
            profile.VisualizerSettings.ThemeIndex = VisualizerTheme;
            _userProfileService.SaveProfile(profile);
            VisualizerThemeChanged?.Invoke(this, VisualizerTheme);
            DebugLogger.Log("SettingsModalViewModel", $"Visualizer theme saved: {VisualizerTheme}");
        }

        [RelayCommand]
        private void Close()
        {
            DebugLogger.Log("SettingsModalViewModel", "Close requested");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OpenWordLists()
        {
            _modalHost?.ShowWordListsModalFromSettings();
        }

        [RelayCommand]
        private void OpenCredits()
        {
            _modalHost?.ShowCreditsModal();
        }

        [RelayCommand]
        private void OpenFiltersDirectory()
        {
            try
            {
                _platformServices?.OpenInFileManager(AppPaths.FiltersDir);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SettingsModalViewModel", $"Error opening filters directory: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenAppDirectory()
        {
            try
            {
                _platformServices?.OpenInFileManager(AppPaths.DataRootDir);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SettingsModalViewModel", $"Error opening app directory: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenAdvancedSettings()
        {
            DebugLogger.Log("SettingsModalViewModel", "Advanced settings requested");
            // First close the current modal
            CloseRequested?.Invoke(this, EventArgs.Empty);
            // Then request opening advanced settings
            AdvancedSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
