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

    public MainWindow()
    {
        InitializeComponent();

        // Set DataContext to ViewModel from DI
        DataContext = ServiceHelper.GetRequiredService<MainWindowViewModel>();

        // Set up the Buy Balatro link
        var buyBalatroLink = this.FindControl<TextBlock>("BuyBalatroLink");
        if (buyBalatroLink is not null)
        {
            buyBalatroLink.PointerPressed += OnBuyBalatroClick;
        }

        // Get reference to main menu for cleanup
        _mainMenu = this.FindControl<BalatroMainMenu>("MainMenu");

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

            // Give disposal 5 seconds max (increased because Motely needs time)
            // Use Task.Run for synchronous Dispose with timeout
            var disposeTask = Task.Run(() => _mainMenu?.Dispose());
            if (await Task.WhenAny(disposeTask, Task.Delay(5000)) != disposeTask)
            {
                DebugLogger.LogError(
                    "MainWindow",
                    "Main menu disposal timed out after 5 seconds - forcing close"
                );
            }
            else
            {
                DebugLogger.Log("MainWindow", "Main menu disposed successfully");
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
