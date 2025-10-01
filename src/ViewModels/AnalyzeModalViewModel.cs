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
    public class AnalyzeModalViewModel : BaseViewModel
    {
        private readonly SpriteService _spriteService;
        private readonly UserProfileService _userProfileService;

        private string _seedInput = "";
        private bool _isAnalyzing = false;
        private string _currentActiveTab = "AnalyzerTab";
        private bool _showPlaceholder = true;

        public AnalyzeModalViewModel(SpriteService spriteService, UserProfileService userProfileService)
        {
            _spriteService = spriteService;
            _userProfileService = userProfileService;

            AnalysisResults = new ObservableCollection<AnalysisResultItem>();

            // Initialize commands
            AnalyzeSeedCommand = new AsyncRelayCommand(AnalyzeSeedAsync, CanAnalyzeSeed);
            ClearResultsCommand = new RelayCommand(ClearResults);
            SwitchTabCommand = new RelayCommand<string>(SwitchTab);
        }

        #region Properties

        public string SeedInput
        {
            get => _seedInput;
            set
            {
                if (SetProperty(ref _seedInput, value))
                {
                    ((AsyncRelayCommand)AnalyzeSeedCommand).NotifyCanExecuteChanged();
                    ShowPlaceholder = string.IsNullOrWhiteSpace(value) && AnalysisResults.Count == 0;
                }
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        public string CurrentActiveTab
        {
            get => _currentActiveTab;
            set => SetProperty(ref _currentActiveTab, value);
        }

        public bool ShowPlaceholder
        {
            get => _showPlaceholder;
            set => SetProperty(ref _showPlaceholder, value);
        }

        public ObservableCollection<AnalysisResultItem> AnalysisResults { get; }

        #endregion

        #region Commands

        public ICommand AnalyzeSeedCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand SwitchTabCommand { get; }

        #endregion

        #region Command Implementations

        private async Task AnalyzeSeedAsync()
        {
            if (string.IsNullOrWhiteSpace(SeedInput))
                return;

            try
            {
                IsAnalyzing = true;
                ShowPlaceholder = false;

                DebugLogger.Log("AnalyzeModalViewModel", $"Starting analysis for seed: {SeedInput}");

                // Clear previous results
                AnalysisResults.Clear();

                // Perform seed analysis
                await AnalyzeSeedInternal(SeedInput.Trim());

                DebugLogger.Log("AnalyzeModalViewModel", "Seed analysis completed");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("AnalyzeModalViewModel", $"Error analyzing seed: {ex.Message}");
                
                // Add error result
                AnalysisResults.Add(new AnalysisResultItem
                {
                    Title = "Analysis Error",
                    Description = $"Failed to analyze seed: {ex.Message}",
                    Type = AnalysisResultType.Error
                });
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private bool CanAnalyzeSeed()
        {
            return !string.IsNullOrWhiteSpace(SeedInput) && !IsAnalyzing;
        }

        private void ClearResults()
        {
            AnalysisResults.Clear();
            SeedInput = "";
            ShowPlaceholder = true;
            DebugLogger.Log("AnalyzeModalViewModel", "Results cleared");
        }

        private void SwitchTab(string? tabName)
        {
            if (!string.IsNullOrEmpty(tabName))
            {
                CurrentActiveTab = tabName;
                DebugLogger.Log("AnalyzeModalViewModel", $"Switched to tab: {tabName}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task AnalyzeSeedInternal(string seed)
        {
            try
            {
                // USE THE REAL MOTELY ANALYZER 
                var config = new Motely.Analysis.MotelySeedAnalysisConfig(
                    seed, 
                    Motely.MotelyDeck.Red, // TODO: Get from UI selection
                    Motely.MotelyStake.White // TODO: Get from UI selection
                );
                
                var analysis = await Task.Run(() => 
                {
                    return Motely.Analysis.MotelySeedAnalyzer.Analyze(config);
                });
                
                // Display REAL analysis results
                if (!string.IsNullOrEmpty(analysis.Error))
                {
                    AnalysisResults.Add(new AnalysisResultItem
                    {
                        Title = "Analysis Error",
                        Description = analysis.Error,
                        Type = AnalysisResultType.Error
                    });
                }
                else
                {
                    AnalysisResults.Add(new AnalysisResultItem
                    {
                        Title = "Seed Analysis Complete",
                        Description = $"Analyzed {analysis.Antes.Count} antes for seed {seed}",
                        Type = AnalysisResultType.Success
                    });
                    
                    // Add ante-by-ante analysis
                    foreach (var ante in analysis.Antes)
                    {
                        AnalysisResults.Add(new AnalysisResultItem
                        {
                            Title = $"Ante {ante.Ante}",
                            Description = ante.ToString(),
                            Type = AnalysisResultType.Info
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                AnalysisResults.Add(new AnalysisResultItem
                {
                    Title = "Analysis Failed",
                    Description = ex.Message,
                    Type = AnalysisResultType.Error
                });
            }

            AnalysisResults.Add(new AnalysisResultItem
            {
                Title = "Analysis Complete",
                Description = "Seed analysis functionality will be implemented when analyzer reference is available",
                Type = AnalysisResultType.Warning
            });
        }

        #endregion
    }

    public class AnalysisResultItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public AnalysisResultType Type { get; set; }
    }

    public enum AnalysisResultType
    {
        Info,
        Warning,
        Error,
        Success
    }
}