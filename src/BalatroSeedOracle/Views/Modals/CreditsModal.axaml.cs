using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class CreditsModal : UserControl
    {
        public CreditsModal()
        {
            InitializeComponent();
            DataContext = ServiceHelper.GetRequiredService<CreditsModalViewModel>();

            // Set up event handler for link clicks
            this.AddHandler(PointerPressedEvent, OnLinkClick, handledEventsToo: true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLinkClick(object? sender, PointerPressedEventArgs e)
        {
            // Check if the clicked element is a credit link
            var source = e.Source as TextBlock;
            if (source != null && source.Name == "CreditLink" && source.Tag is string url)
            {
                try
                {
                    // Open the URL in the default browser
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    BalatroSeedOracle.Helpers.DebugLogger.LogError("CreditsModal", $"Error opening link: {ex.Message}");
                }
            }
        }
    }
}
