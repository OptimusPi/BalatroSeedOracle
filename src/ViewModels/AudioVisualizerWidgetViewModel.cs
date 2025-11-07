using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// Simplified Audio Visualizer Widget ViewModel - KISS principle
    /// Just 3 sliders (Intensity, Speed, Color) and preset management
    /// Advanced users edit JSON files directly
    /// </summary>
    public partial class AudioVisualizerWidgetViewModel : BaseWidgetViewModel
    {
        private readonly UserProfileService _userProfileService;

        public AudioVisualizerWidgetViewModel(UserProfileService userProfileService)
        {
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

            // Configure base widget properties
            WidgetTitle = "Visualizer";
            WidgetIcon = "🎨";
            IsMinimized = true; // Start minimized

            // Set fixed position for visualizer widget
            PositionX = 20;
            PositionY = 170;

            // Initialize presets
            LoadAvailablePresets();

            // Set default values
            Intensity = 70;
            Speed = 50;
            ColorShift = 100;

            // Load last used preset if any
            SelectedPreset = AvailablePresets.FirstOrDefault() ?? "Default";
        }

        #region Simple Properties

        [ObservableProperty]
        private ObservableCollection<string> _availablePresets = new();

        [ObservableProperty]
        private string _selectedPreset = "Default";

        [ObservableProperty]
        private double _intensity = 70.0;

        [ObservableProperty]
        private double _speed = 50.0;

        [ObservableProperty]
        private double _colorShift = 100.0;

        #endregion

        #region Property Change Handlers

        partial void OnIntensityChanged(double value)
        {
            // TODO: Apply intensity to visualizer system
            // For MVP: just track the value
        }

        partial void OnSpeedChanged(double value)
        {
            // TODO: Apply speed to visualizer system
            // For MVP: just track the value
        }

        partial void OnColorShiftChanged(double value)
        {
            // TODO: Apply color shift to visualizer system
            // For MVP: just track the value
        }

        partial void OnSelectedPresetChanged(string value)
        {
            LoadPresetByName(value);
        }

        #endregion

        #region Preset Management

        private void LoadAvailablePresets()
        {
            AvailablePresets.Clear();

            // Add built-in presets
            AvailablePresets.Add("Default");
            AvailablePresets.Add("Wave Rider");
            AvailablePresets.Add("Inferno");
            AvailablePresets.Add("Frozen");
            AvailablePresets.Add("Rainbow Cascade");
            AvailablePresets.Add("Electric Storm");
            AvailablePresets.Add("Sakura Dream");
            AvailablePresets.Add("Lunar Eclipse");

            // Scan for custom presets in presets/ folder
            string presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
            if (Directory.Exists(presetsPath))
            {
                foreach (string file in Directory.GetFiles(presetsPath, "*.json"))
                {
                    string presetName = Path.GetFileNameWithoutExtension(file);
                    if (!AvailablePresets.Contains(presetName))
                    {
                        AvailablePresets.Add(presetName);
                    }
                }
            }
        }

        private void LoadPresetByName(string presetName)
        {
            // TODO: Load preset JSON file and apply settings
            // For now, just update sliders based on preset name

            switch (presetName)
            {
                case "Wave Rider":
                    Intensity = 50;
                    Speed = 70;
                    ColorShift = 80;
                    break;
                case "Inferno":
                    Intensity = 90;
                    Speed = 60;
                    ColorShift = 100;
                    break;
                case "Frozen":
                    Intensity = 40;
                    Speed = 30;
                    ColorShift = 50;
                    break;
                case "Rainbow Cascade":
                    Intensity = 70;
                    Speed = 80;
                    ColorShift = 100;
                    break;
                case "Electric Storm":
                    Intensity = 95;
                    Speed = 90;
                    ColorShift = 90;
                    break;
                case "Sakura Dream":
                    Intensity = 60;
                    Speed = 40;
                    ColorShift = 70;
                    break;
                case "Lunar Eclipse":
                    Intensity = 80;
                    Speed = 50;
                    ColorShift = 60;
                    break;
                default: // Default
                    Intensity = 70;
                    Speed = 50;
                    ColorShift = 100;
                    break;
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task LoadPreset()
        {
            // TODO: Open file picker to load custom JSON preset
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SavePreset()
        {
            // TODO: Open save dialog to save current settings as JSON preset
            await Task.CompletedTask;
        }

        #endregion
    }
}
