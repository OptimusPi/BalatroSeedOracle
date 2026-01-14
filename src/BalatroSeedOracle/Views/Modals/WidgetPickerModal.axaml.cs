using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class WidgetPickerModal : UserControl
    {
        private readonly UserProfileService? _userProfileService;
        private FeatureToggles _toggles = new();

        // UI Elements
        private Button? _musicMixerButton;
        private Button? _visualizerButton;
        private Button? _transitionDesignerButton;
        private Button? _fertilizerButton;
        private Button? _hostApiButton;
        private Button? _eventFXButton;
        private TextBlock? _musicMixerStatus;
        private TextBlock? _visualizerStatus;
        private TextBlock? _transitionDesignerStatus;
        private TextBlock? _fertilizerStatus;
        private TextBlock? _hostApiStatus;
        private TextBlock? _eventFXStatus;

        public WidgetPickerModal()
        {
            InitializeComponent();

            _userProfileService = App.GetService<UserProfileService>();
            RefreshToggles();

            // Direct field access from x:Name - no FindControl anti-pattern!
            _musicMixerButton = MusicMixerButton;
            _visualizerButton = VisualizerButton;
            _transitionDesignerButton = TransitionDesignerButton;
            _fertilizerButton = FertilizerButton;
            _hostApiButton = HostApiButton;
            _eventFXButton = EventFXButton;
            _musicMixerStatus = MusicMixerStatus;
            _visualizerStatus = VisualizerStatus;
            _transitionDesignerStatus = TransitionDesignerStatus;
            _fertilizerStatus = FertilizerStatus;
            _hostApiStatus = HostApiStatus;
            _eventFXStatus = EventFXStatus;

            UpdateButtonStates();
        }

        private void RefreshToggles()
        {
            _toggles = _userProfileService?.GetProfile().FeatureToggles ?? new FeatureToggles();
            
            DebugLogger.Log("WidgetPickerModal", 
                $"Refreshed toggles: Mixer={_toggles.ShowMusicMixer}, Viz={_toggles.ShowVisualizer}, Trans={_toggles.ShowTransitionDesigner}, Fert={_toggles.ShowFertilizer}, Host={_toggles.ShowHostServer}, EventFX={_toggles.ShowEventFX}");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Refresh toggles when the modal loads to ensure current state
            this.AttachedToVisualTree += (s, e) => RefreshTogglesAndUI();
        }

        private void RefreshTogglesAndUI()
        {
            RefreshToggles();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            UpdateButton(_musicMixerButton, _musicMixerStatus, _toggles.ShowMusicMixer);
            UpdateButton(_visualizerButton, _visualizerStatus, _toggles.ShowVisualizer);
            UpdateButton(
                _transitionDesignerButton,
                _transitionDesignerStatus,
                _toggles.ShowTransitionDesigner
            );
            UpdateButton(_fertilizerButton, _fertilizerStatus, _toggles.ShowFertilizer);
            UpdateButton(_hostApiButton, _hostApiStatus, _toggles.ShowHostServer);
            UpdateButton(_eventFXButton, _eventFXStatus, _toggles.ShowEventFX);
        }

        private void UpdateButton(Button? button, TextBlock? status, bool isEnabled)
        {
            if (button == null || status == null)
                return;

            // Update button class for visual styling
            button.Classes.Clear();
            button.Classes.Add(isEnabled ? "btn-green" : "btn-red");

            // Update status text
            status.Text = isEnabled ? "(ON)" : "(OFF)";
        }

        private void OnMusicMixerClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowMusicMixer = !_toggles.ShowMusicMixer;
            SaveAndRefresh();
        }

        private void OnVisualizerClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowVisualizer = !_toggles.ShowVisualizer;
            SaveAndRefresh();
        }

        private void OnTransitionDesignerClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowTransitionDesigner = !_toggles.ShowTransitionDesigner;
            SaveAndRefresh();
        }

        private void OnFertilizerClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowFertilizer = !_toggles.ShowFertilizer;
            SaveAndRefresh();
        }

        private void OnHostApiClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowHostServer = !_toggles.ShowHostServer;
            SaveAndRefresh();
        }

        private void OnEventFXClick(object? sender, RoutedEventArgs e)
        {
            _toggles.ShowEventFX = !_toggles.ShowEventFX;
            SaveAndRefresh();
        }

        private void SaveAndRefresh()
        {
            // Save to profile
            if (_userProfileService != null)
            {
                var profile = _userProfileService.GetProfile();
                profile.FeatureToggles = _toggles;
                _userProfileService.SaveProfile(profile);
                DebugLogger.Log(
                    "WidgetPickerModal",
                    $"Saved toggles: Mixer={_toggles.ShowMusicMixer}, Viz={_toggles.ShowVisualizer}, Trans={_toggles.ShowTransitionDesigner}, Fert={_toggles.ShowFertilizer}, Host={_toggles.ShowHostServer}, EventFX={_toggles.ShowEventFX}"
                );
            }

            // Update button visuals
            UpdateButtonStates();

            // Refresh main menu widget visibility
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu?.DataContext is BalatroMainMenuViewModel mainMenuVm)
            {
                mainMenuVm.RefreshFeatureToggles();
            }
        }
    }
}
