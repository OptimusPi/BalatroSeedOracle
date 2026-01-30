using System;
using System.ComponentModel;
using Avalonia.Controls;
using BalatroSeedOracle.Desktop.Components.Widgets;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Desktop.Views;

/// <summary>
/// Desktop-specific extensions for MainWindow to add platform-specific widgets.
/// Uses PropertyChanged subscription for visibility (AOT-safe, no reflection Binding).
/// Pattern: "folder thing" — all desktop widget logic lives here in Desktop project only; no shared provider interface.
/// </summary>
public static class MainWindowExtensions
{
    /// <summary>
    /// Bind control visibility to a ViewModel bool property without reflection (AOT/trim-safe).
    /// </summary>
    private static void BindVisibility(
        Control control,
        BalatroMainMenuViewModel vm,
        string propertyName,
        Func<bool> getter)
    {
        control.IsVisible = getter();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == propertyName)
                control.IsVisible = getter();
        };
    }
    /// <summary>
    /// Initialize desktop-specific widgets for BalatroMainMenu.
    /// Should be called after MainWindow is created and shown.
    /// </summary>
    public static void InitializeDesktopWidgets(this MainWindow mainWindow)
    {
        if (mainWindow.MainMenu == null)
        {
            DebugLogger.LogError("DesktopWidgetInit", "MainMenu not found");
            return;
        }

        // Check if already loaded
        if (mainWindow.MainMenu.IsLoaded)
        {
            AddDesktopWidgets(mainWindow.MainMenu);
        }
        else
        {
            // Subscribe to Loaded event to add widgets after XAML is loaded
            mainWindow.MainMenu.Loaded += (sender, e) =>
            {
                AddDesktopWidgets(mainWindow.MainMenu);
            };
        }
    }

    private static void AddDesktopWidgets(BalatroMainMenu mainMenu)
    {
        // Direct access via public property (DesktopCanvas is x:Name in shared assembly)
        var desktopCanvas = mainMenu.DesktopCanvasHost;
        if (desktopCanvas == null)
        {
            DebugLogger.LogError("DesktopWidgetInit", "DesktopCanvas not found");
            return;
        }

        var viewModel = mainMenu.ViewModel;
        if (viewModel == null)
        {
            DebugLogger.LogError("DesktopWidgetInit", "ViewModel not found");
            return;
        }

        // Add API Host Widget
        if (viewModel.ApiHostWidgetViewModel != null)
        {
            var apiHostWidget = new ApiHostWidget
            {
                DataContext = viewModel.ApiHostWidgetViewModel,
                ClipToBounds = false,
                [Avalonia.Controls.Grid.ZIndexProperty] = viewModel.ApiHostWidgetViewModel.WidgetZIndex,
            };
            
            BindVisibility(apiHostWidget, viewModel, nameof(BalatroMainMenuViewModel.IsHostApiWidgetVisible), () => viewModel.IsHostApiWidgetVisible);
            desktopCanvas.Children.Add(apiHostWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added ApiHostWidget");
        }

        // Add Music Mixer Widget
        var musicMixerVm = ServiceHelper.GetService<MusicMixerWidgetViewModel>();
        if (musicMixerVm != null)
        {
            var musicMixerWidget = new MusicMixerWidget
            {
                DataContext = musicMixerVm,
                ClipToBounds = false,
                [Avalonia.Controls.Panel.ZIndexProperty] = musicMixerVm.WidgetZIndex,
            };
            
            BindVisibility(musicMixerWidget, viewModel, nameof(BalatroMainMenuViewModel.IsMusicMixerWidgetVisible), () => viewModel.IsMusicMixerWidgetVisible);
            desktopCanvas.Children.Add(musicMixerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added MusicMixerWidget");
        }

        // Add Audio Visualizer Settings Widget
        var visualizerVm = ServiceHelper.GetService<AudioVisualizerSettingsWidgetViewModel>();
        if (visualizerVm != null)
        {
            var visualizerWidget = new AudioVisualizerSettingsWidget
            {
                DataContext = visualizerVm,
                ClipToBounds = false,
                [Avalonia.Controls.Panel.ZIndexProperty] = visualizerVm.WidgetZIndex,
            };
            
            BindVisibility(visualizerWidget, viewModel, nameof(BalatroMainMenuViewModel.IsVisualizerWidgetVisible), () => viewModel.IsVisualizerWidgetVisible);
            desktopCanvas.Children.Add(visualizerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added AudioVisualizerSettingsWidget");
        }

        // Add Audio Mixer Widget
        var audioMixerVm = ServiceHelper.GetService<AudioMixerWidgetViewModel>();
        if (audioMixerVm != null)
        {
            var audioMixerWidget = new AudioMixerWidget
            {
                DataContext = audioMixerVm,
                ClipToBounds = false,
                [Avalonia.Controls.Panel.ZIndexProperty] = audioMixerVm.WidgetZIndex,
            };
            
            desktopCanvas.Children.Add(audioMixerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added AudioMixerWidget");
        }

        // Add Frequency Debug Widget
        var frequencyDebugVm = ServiceHelper.GetService<FrequencyDebugWidgetViewModel>();
        if (frequencyDebugVm != null)
        {
            var frequencyDebugWidget = new FrequencyDebugWidget
            {
                DataContext = frequencyDebugVm,
                ClipToBounds = false,
                [Avalonia.Controls.Panel.ZIndexProperty] = frequencyDebugVm.WidgetZIndex,
            };
            
            desktopCanvas.Children.Add(frequencyDebugWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added FrequencyDebugWidget");
        }

        // Add Transition Designer Widget
        var transitionDesignerVm = ServiceHelper.GetService<TransitionDesignerWidgetViewModel>();
        if (transitionDesignerVm != null)
        {
            var transitionDesignerWidget = new TransitionDesignerWidget
            {
                DataContext = transitionDesignerVm,
                ClipToBounds = false,
                [Avalonia.Controls.Panel.ZIndexProperty] = transitionDesignerVm.WidgetZIndex,
            };
            
            BindVisibility(transitionDesignerWidget, viewModel, nameof(BalatroMainMenuViewModel.IsTransitionDesignerWidgetVisible), () => viewModel.IsTransitionDesignerWidgetVisible);
            desktopCanvas.Children.Add(transitionDesignerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added TransitionDesignerWidget");
        }

        DebugLogger.Log("DesktopWidgetInit", "Desktop widgets initialized successfully");
    }
}
