using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views;

public partial class MainWindow : Window
{
    private BalatroMainMenu? _mainMenu;

    public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public BalatroMainMenu? MainMenu => _mainMenu;

    public MainWindow(MainWindowViewModel viewModel, BalatroMainMenu mainMenu)
    {
        InitializeComponent();

        DataContext = viewModel;

        // Set up the Buy Balatro link
        if (BuyBalatroLink is not null)
        {
            BuyBalatroLink.PointerPressed += OnBuyBalatroClick;
        }

        // Host the DI-created main menu instance (no FindControl service-locator anti-pattern)
        _mainMenu = mainMenu;
        if (MainMenuHost is not null)
        {
            MainMenuHost.Content = mainMenu;
        }
        else
        {
            DebugLogger.LogError(
                "MainWindow",
                "MainMenuHost not found - cannot attach BalatroMainMenu"
            );
        }

        // Sync IsVibeOutMode from MainMenu to MainWindow
        if (_mainMenu?.ViewModel is not null && ViewModel is not null)
        {
            _mainMenu.ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_mainMenu.ViewModel.IsVibeOutMode))
                {
                    ViewModel.IsVibeOutMode = _mainMenu.ViewModel.IsVibeOutMode;
                }
            };
        }

        // Initialize notification service
        var notificationService = ServiceHelper.GetService<Services.NotificationService>();
        if (notificationService != null)
        {
            notificationService.Initialize(this);
        }

        // Handle window closing
        Closing += OnWindowClosing;

        // Handle window resize to reposition widgets
        SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        DebugLogger.LogImportant("MainWindow", "Window closing - initiating cleanup");

        // Prevent the window from closing immediately
        e.Cancel = true;

        // Do cleanup asynchronously to avoid blocking UI
        _ = CleanupAndExitAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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
        try
        {
            // Get the widget position service
            var positionService = ServiceHelper.GetService<Services.WidgetPositionService>();
            if (positionService != null)
            {
                // Handle window resize by repositioning widgets that are now out of bounds
                positionService.HandleWindowResize(e.NewSize.Width, e.NewSize.Height);

                DebugLogger.Log(
                    "MainWindow",
                    $"Window resized to {e.NewSize.Width}x{e.NewSize.Height}, widgets repositioned"
                );
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Error handling window resize: {ex.Message}");
        }
    }

    private async Task CleanupAndExitAsync()
    {
        try
        {
            DebugLogger.Log("MainWindow", "Starting cleanup");

            // First ensure any running search state is saved
            var userProfileService =
                BalatroSeedOracle.Helpers.ServiceHelper.GetService<BalatroSeedOracle.Services.UserProfileService>();
            if (userProfileService is not null)
            {
                DebugLogger.LogImportant(
                    "MainWindow",
                    "Flushing user profile to save search state..."
                );
                userProfileService.FlushProfile();
            }

            // Stop any running Motely searches first
            if (_mainMenu != null)
            {
                DebugLogger.LogImportant("MainWindow", "Stopping all Motely searches...");
                await _mainMenu.StopAllSearchesAsync();
                DebugLogger.LogImportant("MainWindow", "All searches stopped");
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
