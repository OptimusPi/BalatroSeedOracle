using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Oracle.Helpers;

namespace Oracle.Views;

public partial class MainWindow : Window
{
    private BalatroMainMenu? _mainMenu;

    public MainWindow()
    {
        InitializeComponent();

        // Set up the Buy Balatro link
        var buyBalatroLink = this.FindControl<TextBlock>("BuyBalatroLink");
        if (buyBalatroLink != null)
        {
            buyBalatroLink.PointerPressed += OnBuyBalatroClick;
        }

        // Get reference to main menu for cleanup
        _mainMenu = this.FindControl<BalatroMainMenu>("MainMenu");

        // Handle window closing
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        DebugLogger.LogImportant("MainWindow", "Window closing - initiating cleanup");

        // Prevent the window from closing immediately
        e.Cancel = true;

        // Do cleanup asynchronously to avoid blocking UI
        Task.Run(async () =>
        {
            try
            {
                DebugLogger.Log("MainWindow", "Starting cleanup");

                // CRITICAL: Stop any running Motely searches first
                if (_mainMenu != null)
                {
                    DebugLogger.LogImportant("MainWindow", "Stopping all Motely searches...");
                    await _mainMenu.StopAllSearchesAsync();
                    DebugLogger.LogImportant("MainWindow", "All searches stopped");
                }

                DebugLogger.Log("MainWindow", "Starting main menu disposal");

                // Give disposal 5 seconds max (increased because Motely needs time)
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
                // Close the window on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DebugLogger.LogImportant("MainWindow", "Closing window");
                    // Remove event handler to prevent recursion
                    Closing -= OnWindowClosing;
                    Close();
                });
            }
        });
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
}
