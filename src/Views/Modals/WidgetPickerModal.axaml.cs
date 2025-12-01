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
        private FeatureToggles _toggles;

        // UI Elements
        private Button? _musicMixerButton;
        private Button? _visualizerButton;
        private Button? _transitionDesignerButton;
        private TextBlock? _musicMixerStatus;
        private TextBlock? _visualizerStatus;
        private TextBlock? _transitionDesignerStatus;

        public WidgetPickerModal()
        {
            InitializeComponent();

            _userProfileService = App.GetService<UserProfileService>();
            _toggles = _userProfileService?.GetProfile().FeatureToggles ?? new FeatureToggles();

            // Find UI elements
            _musicMixerButton = this.FindControl<Button>("MusicMixerButton");
            _visualizerButton = this.FindControl<Button>("VisualizerButton");
            _transitionDesignerButton = this.FindControl<Button>("TransitionDesignerButton");
            _musicMixerStatus = this.FindControl<TextBlock>("MusicMixerStatus");
            _visualizerStatus = this.FindControl<TextBlock>("VisualizerStatus");
            _transitionDesignerStatus = this.FindControl<TextBlock>("TransitionDesignerStatus");

            UpdateButtonStates();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
                    $"Saved toggles: Mixer={_toggles.ShowMusicMixer}, Viz={_toggles.ShowVisualizer}, Trans={_toggles.ShowTransitionDesigner}"
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
