using System;
using Avalonia.Controls;
using BalatroSeedOracle.Desktop.Components.Widgets;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;
using Microsoft.Extensions.DependencyInjection;

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
        Func<bool> getter
    )
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
    public static void InitializeDesktopWidgets(this MainWindow mainWindow, IServiceProvider services)
    {
        if (mainWindow.MainMenu == null)
        {
            DebugLogger.LogError("DesktopWidgetInit", "MainMenu not found");
            return;
        }

        // Check if already loaded
        if (mainWindow.MainMenu.IsLoaded)
        {
            AddDesktopWidgets(mainWindow.MainMenu, services);
        }
        else
        {
            // Subscribe to Loaded event to add widgets after XAML is loaded
            mainWindow.MainMenu.Loaded += (sender, e) =>
            {
                AddDesktopWidgets(mainWindow.MainMenu, services);
            };
        }
    }

    private static void AddDesktopWidgets(BalatroMainMenu mainMenu, IServiceProvider services)
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

        // API Host Widget removed alongside Motely.API. The toggle in the menu is
        // kept as a no-op for now so user preferences round-trip.

        // Add Music Mixer Widget
        var musicMixerVm = services.GetService<MusicMixerWidgetViewModel>();
        if (musicMixerVm != null)
        {
            var musicMixerWidget = new MusicMixerWidget
            {
                DataContext = musicMixerVm,
                ClipToBounds = false,
                [Avalonia.Visual.ZIndexProperty] = musicMixerVm.WidgetZIndex,
            };

            BindVisibility(
                musicMixerWidget,
                viewModel,
                nameof(BalatroMainMenuViewModel.IsMusicMixerWidgetVisible),
                () => viewModel.IsMusicMixerWidgetVisible
            );
            desktopCanvas.Children.Add(musicMixerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added MusicMixerWidget");
        }

        // Add Audio Visualizer Settings Widget
        var visualizerVm = services.GetService<AudioVisualizerSettingsWidgetViewModel>();
        if (visualizerVm != null)
        {
            var visualizerWidget = new AudioVisualizerSettingsWidget
            {
                DataContext = visualizerVm,
                ClipToBounds = false,
                [Avalonia.Visual.ZIndexProperty] = visualizerVm.WidgetZIndex,
            };

            BindVisibility(
                visualizerWidget,
                viewModel,
                nameof(BalatroMainMenuViewModel.IsVisualizerWidgetVisible),
                () => viewModel.IsVisualizerWidgetVisible
            );
            desktopCanvas.Children.Add(visualizerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added AudioVisualizerSettingsWidget");
        }

        // Add Audio Mixer Widget
        var audioMixerVm = services.GetService<AudioMixerWidgetViewModel>();
        if (audioMixerVm != null)
        {
            var audioMixerWidget = new AudioMixerWidget
            {
                DataContext = audioMixerVm,
                ClipToBounds = false,
                [Avalonia.Visual.ZIndexProperty] = audioMixerVm.WidgetZIndex,
            };

            desktopCanvas.Children.Add(audioMixerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added AudioMixerWidget");
        }

        // Add Frequency Debug Widget
        var frequencyDebugVm = services.GetService<FrequencyDebugWidgetViewModel>();
        if (frequencyDebugVm != null)
        {
            var frequencyDebugWidget = new FrequencyDebugWidget
            {
                DataContext = frequencyDebugVm,
                ClipToBounds = false,
                [Avalonia.Visual.ZIndexProperty] = frequencyDebugVm.WidgetZIndex,
            };

            desktopCanvas.Children.Add(frequencyDebugWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added FrequencyDebugWidget");
        }

        // Add Transition Designer Widget
        var transitionDesignerVm = services.GetService<TransitionDesignerWidgetViewModel>();
        if (transitionDesignerVm != null)
        {
            var transitionDesignerWidget = new TransitionDesignerWidget
            {
                DataContext = transitionDesignerVm,
                ClipToBounds = false,
                [Avalonia.Visual.ZIndexProperty] = transitionDesignerVm.WidgetZIndex,
            };

            BindVisibility(
                transitionDesignerWidget,
                viewModel,
                nameof(BalatroMainMenuViewModel.IsTransitionDesignerWidgetVisible),
                () => viewModel.IsTransitionDesignerWidgetVisible
            );
            desktopCanvas.Children.Add(transitionDesignerWidget);
            DebugLogger.Log("DesktopWidgetInit", "Added TransitionDesignerWidget");
        }

        DebugLogger.Log("DesktopWidgetInit", "Desktop widgets initialized successfully");
    }
}
