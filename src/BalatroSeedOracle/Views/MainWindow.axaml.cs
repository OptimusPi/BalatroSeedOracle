using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views;

public partial class MainWindow : Window
{
    private BalatroMainMenu? _mainMenu;

    public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public BalatroMainMenu? MainMenu => _mainMenu;

    // Parameterless constructor required for XAML loading
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, BalatroMainMenu mainMenu)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Set up the Buy Balatro link
        if (BuyBalatroLink is not null)
        {
            BuyBalatroLink.PointerPressed += OnBuyBalatroClick;
        }

        // Host the DI-created main menu instance.
        _mainMenu = mainMenu;
        MainContentHost.Content = _mainMenu;

        // Sync IsVibeOutMode from MainMenu to MainWindow
        if (_mainMenu?.ViewModel is not null && ViewModel is not null)
        {
            _mainMenu.ViewModel.PropertyChanged += (s, e) =>
            {
                if (
                    e.PropertyName == nameof(_mainMenu.ViewModel.IsVibeOutMode)
                    && ViewModel is not null
                )
                {
                    ViewModel.IsVibeOutMode = _mainMenu.ViewModel.IsVibeOutMode;
                }
            };
        }

        Closing += OnWindowClosing;
        SizeChanged += OnWindowSizeChanged;
        UpdateUiScale();
    }

    // The fixed design canvas the UI is authored against (must match the inner Panel in XAML).
    private const double DesignWidth = 1582.0;
    private const double DesignHeight = 830.0;
    // Never shrink below this so text stays legible on very small windows.
    private const double MinScale = 0.4;

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        DebugLogger.LogImportant("MainWindow", "Window closing - initiating cleanup");

        // Prevent the window from closing immediately
        e.Cancel = true;

        CleanupAndExit();
    }

    private void OnBuyBalatroClick(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Open the Balatro website in the default browser
            var url = "https://www.playbalatro.com/";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Error opening Balatro website: {ex.Message}");
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateUiScale();
    }

    /// <summary>
    /// Balatro-style uniform scaling: fit the fixed design canvas (1582x830) inside the
    /// current window bounds. Uniform (min of the two ratios) so the UI never distorts —
    /// it smooths down on small windows and scales up crisply on large / maximized ones.
    /// </summary>
    private void UpdateUiScale()
    {
        if (ScaleRoot?.LayoutTransform is not Avalonia.Media.ScaleTransform uiScale || ScaleHost is null)
            return;

        var availW = ScaleHost.Bounds.Width;
        var availH = ScaleHost.Bounds.Height;
        if (availW <= 0 || availH <= 0)
            return;

        var scale = Math.Min(availW / DesignWidth, availH / DesignHeight);
        if (scale < MinScale)
            scale = MinScale;

        uiScale.ScaleX = scale;
        uiScale.ScaleY = scale;
    }

    private void CleanupAndExit()
    {
        try
        {
            DebugLogger.Log("MainWindow", "Starting cleanup");

            // First ensure any running search state is saved
            var userProfileService =
                ServiceHelper.GetService<BalatroSeedOracle.Services.UserProfileService>();
            if (userProfileService is not null)
            {
                DebugLogger.LogImportant(
                    "MainWindow",
                    "Flushing user profile to save search state..."
                );
                userProfileService.FlushProfile();
            }

            DebugLogger.Log("MainWindow", "Starting main menu disposal");

            // Dispose synchronously - Dispose() is synchronous by design
            // If disposal takes too long, we'll timeout and force close anyway
            try
            {
                _mainMenu?.Dispose();
                DebugLogger.Log("MainWindow", "Main menu disposed successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("MainWindow", $"Error disposing main menu: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("MainWindow", $"Error during disposal: {ex.Message}");
        }
        finally
        {
            // Force close after cleanup completes
            Environment.Exit(0);
        }
    }
}
