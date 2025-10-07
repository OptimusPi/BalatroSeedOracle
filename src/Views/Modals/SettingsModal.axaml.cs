using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class SettingsModal : UserControl
    {
        public event EventHandler? CloseRequested;
        private List<FeatureToggleViewModel> _featureToggles;

        public SettingsModal()
        {
            InitializeComponent();
            _featureToggles = new List<FeatureToggleViewModel>();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadSettings()
        {
            // Load feature flags
            var flags = FeatureFlagsService.Instance.GetAllFlags();
            var descriptions = FeatureFlagsService.GetFeatureDescriptions();

            _featureToggles.Clear();
            foreach (var flag in flags)
            {
                _featureToggles.Add(new FeatureToggleViewModel
                {
                    Key = flag.Key,
                    Name = descriptions.TryGetValue(flag.Key, out var desc) ? desc : flag.Key,
                    Description = GetDetailedDescription(flag.Key),
                    IsEnabled = flag.Value
                });
            }

            // Sort: Daylatro first, then Genie, then others
            _featureToggles = _featureToggles
                .OrderBy(f => f.Key == FeatureFlagsService.DAYLATRO_ENABLED ? 0 :
                              f.Key == FeatureFlagsService.GENIE_ENABLED ? 1 : 2)
                .ThenBy(f => f.Name)
                .ToList();

            var togglesControl = this.FindControl<ItemsControl>("FeatureToggles");
            if (togglesControl != null)
            {
                togglesControl.ItemsSource = _featureToggles;
            }

            // Load general settings
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            if (userProfileService != null)
            {
                var authorBox = this.FindControl<TextBox>("AuthorNameBox");
                if (authorBox != null)
                {
                    authorBox.Text = userProfileService.GetAuthorName();
                }

                var threadBox = this.FindControl<NumericUpDown>("ThreadCountBox");
                if (threadBox != null)
                {
                    threadBox.Value = Environment.ProcessorCount; // Default to CPU cores
                }
            }
        }

        private string GetDetailedDescription(string key)
        {
            return key switch
            {
                FeatureFlagsService.GENIE_ENABLED => "Experimental AI assistant for filter suggestions",
                FeatureFlagsService.DAYLATRO_ENABLED => "Show daily challenge seeds and leaderboards",
                FeatureFlagsService.SHADER_BACKGROUNDS => "GPU-rendered animated backgrounds",
                FeatureFlagsService.VIBE_AUDIO => "Multi-track adaptive music system",
                FeatureFlagsService.EXPERIMENTAL_SEARCH => "Beta search optimizations and features",
                FeatureFlagsService.DEBUG_MODE => "Show detailed logs and diagnostics",
                _ => ""
            };
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            // Save feature flags
            foreach (var toggle in _featureToggles)
            {
                FeatureFlagsService.Instance.SetFeature(toggle.Key, toggle.IsEnabled);
            }

            // Save general settings
            var userProfileService = ServiceHelper.GetService<UserProfileService>();
            if (userProfileService != null)
            {
                var authorBox = this.FindControl<TextBox>("AuthorNameBox");
                if (authorBox != null && !string.IsNullOrWhiteSpace(authorBox.Text))
                {
                    userProfileService.SetAuthorName(authorBox.Text.Trim());
                }

                var threadBox = this.FindControl<NumericUpDown>("ThreadCountBox");
                if (threadBox != null && threadBox.Value.HasValue)
                {
                    // Save thread count preference to user profile (custom property)
                    // For now just log it - can implement in UserProfileService later
                    DebugLogger.Log("SettingsModal", $"Thread count preference: {(int)threadBox.Value.Value}");
                }

                userProfileService.SaveProfile();
            }

            DebugLogger.Log("SettingsModal", "Settings saved successfully");
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnResetClick(object? sender, RoutedEventArgs e)
        {
            // Reset to defaults
            _featureToggles[0].IsEnabled = false; // Genie OFF
            _featureToggles[1].IsEnabled = true;  // Daylatro ON
            _featureToggles[2].IsEnabled = true;  // Shaders ON
            _featureToggles[3].IsEnabled = true;  // Audio ON
            _featureToggles[4].IsEnabled = false; // Experimental OFF
            _featureToggles[5].IsEnabled = false; // Debug OFF

            // Refresh UI
            LoadSettings();

            DebugLogger.Log("SettingsModal", "Settings reset to defaults");
        }

        // ViewModel for feature toggles
        public class FeatureToggleViewModel : INotifyPropertyChanged
        {
            private bool _isEnabled;

            public string Key { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";

            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    if (_isEnabled != value)
                    {
                        _isEnabled = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}