using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly UserProfileService _userProfileService;
        private readonly SearchManager _searchManager;
        
        private string _windowTitle = "Balatro Seed Oracle";
        private bool _isModalOpen = false;
        private string? _currentModalType;

        public MainWindowViewModel(UserProfileService userProfileService, SearchManager searchManager)
        {
            _userProfileService = userProfileService;
            _searchManager = searchManager;

            // Initialize commands
            OpenFiltersModalCommand = new RelayCommand(OpenFiltersModal);
            OpenSearchModalCommand = new RelayCommand(OpenSearchModal);
            OpenAnalyzeModalCommand = new RelayCommand(OpenAnalyzeModal);
            OpenCreditsModalCommand = new RelayCommand(OpenCreditsModal);
            OpenToolsModalCommand = new RelayCommand(OpenToolsModal);
            CloseModalCommand = new RelayCommand(CloseModal);

            InitializeAsync();
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public bool IsModalOpen
        {
            get => _isModalOpen;
            set => SetProperty(ref _isModalOpen, value);
        }

        public string? CurrentModalType
        {
            get => _currentModalType;
            set => SetProperty(ref _currentModalType, value);
        }

        #endregion

        #region Commands

        public ICommand OpenFiltersModalCommand { get; }
        public ICommand OpenSearchModalCommand { get; }
        public ICommand OpenAnalyzeModalCommand { get; }
        public ICommand OpenCreditsModalCommand { get; }
        public ICommand OpenToolsModalCommand { get; }
        public ICommand CloseModalCommand { get; }

        #endregion

        #region Command Implementations

        private void OpenFiltersModal()
        {
            CurrentModalType = "Filters";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Filters modal");
        }

        private void OpenSearchModal()
        {
            CurrentModalType = "Search";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Search modal");
        }

        private void OpenAnalyzeModal()
        {
            CurrentModalType = "Analyze";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Analyze modal");
        }

        private void OpenCreditsModal()
        {
            CurrentModalType = "Credits";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Credits modal");
        }

        private void OpenToolsModal()
        {
            CurrentModalType = "Tools";
            IsModalOpen = true;
            DebugLogger.Log("MainWindowViewModel", "Opened Tools modal");
        }

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