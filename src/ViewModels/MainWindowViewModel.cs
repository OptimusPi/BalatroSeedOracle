using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly UserProfileService _userProfileService;
        private readonly SearchManager _searchManager;

        [ObservableProperty]
        private string _windowTitle = "Balatro Seed Oracle";

        [ObservableProperty]
        private bool _isModalOpen = false;

        [ObservableProperty]
        private string? _currentModalType;

        public MainWindowViewModel(UserProfileService userProfileService, SearchManager searchManager)
        {
            _userProfileService = userProfileService;
            _searchManager = searchManager;

            InitializeAsync();
        }

        #region Command Implementations

        [RelayCommand]
        private void OpenFiltersModal()
        {
            CurrentModalType = "Filters";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Filters modal");
        }

        [RelayCommand]
        private void OpenSearchModal()
        {
            CurrentModalType = "Search";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Search modal");
        }

        [RelayCommand]
        private void OpenAnalyzeModal()
        {
            CurrentModalType = "Analyze";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Analyze modal");
        }

        [RelayCommand]
        private void OpenCreditsModal()
        {
            CurrentModalType = "Credits";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Credits modal");
        }

        [RelayCommand]
        private void OpenToolsModal()
        {
            CurrentModalType = "Tools";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Tools modal");
        }

        [RelayCommand]
        private void CloseModal()
        {
            IsModalOpen = false;
            CurrentModalType = null;
            DebugLogger.Log("MainWindowViewModel", "Closed modal");
        }

        #endregion

        #region Helper Methods

        private async void InitializeAsync()
        {
            try
            {
                await LoadUserProfileAsync();
                DebugLogger.Log("MainWindowViewModel", "MainWindow initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MainWindowViewModel", $"Error initializing MainWindow: {ex.Message}");
            }
        }

        private async Task LoadUserProfileAsync()
        {
            try
            {
                var profile = await _userProfileService.LoadUserProfileAsync();
                if (profile != null)
                {
                    DebugLogger.Log("MainWindowViewModel", "User profile loaded successfully");
                }
                else
                {
                    DebugLogger.Log("MainWindowViewModel", "No user profile found, using defaults");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MainWindowViewModel", $"Error loading user profile: {ex.Message}");
            }
        }

        #endregion
    }
}