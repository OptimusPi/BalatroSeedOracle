using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    public partial class FilterCreationModal : UserControl
    {
        public event EventHandler? CloseRequested;

        public FilterCreationModal()
        {
            InitializeComponent();

            // Wire up events
            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += OnCloseButtonClick;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}