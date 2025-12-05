using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.ViewModels.Widgets;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class WidgetPickerModal : UserControl
    {
        public event EventHandler<EventArgs>? CloseRequested;

        private readonly UserProfileService? _userProfileService;
        private readonly WidgetContainerViewModel? _widgetContainer;
        private FeatureToggles _toggles;

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
            _toggles = _userProfileService?.GetProfile().FeatureToggles ?? new FeatureToggles();
            
            // Get widget container from the main menu
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            if (mainMenu?.DataContext is BalatroMainMenuViewModel mainMenuVm)
            {
                _widgetContainer = mainMenuVm.WidgetContainer;
            }

            // Find UI elements
            _musicMixerButton = this.FindControl<Button>("MusicMixerButton");
            _visualizerButton = this.FindControl<Button>("VisualizerButton");
            _transitionDesignerButton = this.FindControl<Button>("TransitionDesignerButton");
            _fertilizerButton = this.FindControl<Button>("FertilizerButton");
            _hostApiButton = this.FindControl<Button>("HostApiButton");
            _eventFXButton = this.FindControl<Button>("EventFXButton");
            _musicMixerStatus = this.FindControl<TextBlock>("MusicMixerStatus");
            _visualizerStatus = this.FindControl<TextBlock>("VisualizerStatus");
            _transitionDesignerStatus = this.FindControl<TextBlock>("TransitionDesignerStatus");
            _fertilizerStatus = this.FindControl<TextBlock>("FertilizerStatus");
            _hostApiStatus = this.FindControl<TextBlock>("HostApiStatus");
            _eventFXStatus = this.FindControl<TextBlock>("EventFXStatus");

            UpdateButtonStates();
        }


        private void UpdateButtonStates()
        {
            UpdateButton(_musicMixerButton, _musicMixerStatus, "Audio Mixer");
            UpdateButton(_visualizerButton, _visualizerStatus, "Audio Visualizer");
            UpdateButton(_transitionDesignerButton, _transitionDesignerStatus, "Transition Designer");
            UpdateButton(_fertilizerButton, _fertilizerStatus, "Fertilizer Pile");
            UpdateButton(_hostApiButton, _hostApiStatus, "Host API Server");
            UpdateButton(_eventFXButton, _eventFXStatus, "Event Effects");
        }

        private void UpdateButton(Button? button, TextBlock? status, string widgetName)
        {
            if (button == null || status == null)
                return;

            // Update button class for visual styling (always create button style)
            button.Classes.Clear();
            button.Classes.Add("btn-green");

            // Update status text to indicate creation
            status.Text = "(CREATE)";
        }

        private void OnMusicMixerClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("audio-mixer");
        }

        private void OnVisualizerClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("audio-visualizer");
        }

        private void OnTransitionDesignerClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("transition-designer");
        }

        private void OnFertilizerClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("fertilizer");
        }

        private void OnHostApiClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("host-api");
        }

        private void OnEventFXClick(object? sender, RoutedEventArgs e)
        {
            CreateWidget("event-fx");
        }

        private async void CreateWidget(string widgetId)
        {
            try
            {
                // Get widget container from main menu
                var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
                if (mainMenu?.DataContext is BalatroMainMenuViewModel mainMenuVm)
                {
                    var widgetContainer = mainMenuVm.WidgetContainer;
                    if (widgetContainer != null)
                    {
                        await widgetContainer.CreateWidgetAsync(widgetId);
                        
                        // Close the modal after creating widget
                        CloseRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log("WidgetPickerModal", $"Failed to create widget {widgetId}: {ex.Message}");
            }
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
