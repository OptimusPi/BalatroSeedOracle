using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class FrequencyDebugWidget : UserControl
    {
        public FrequencyDebugWidgetViewModel ViewModel { get; }

        private Point _iconPressedPosition;

        public FrequencyDebugWidget()
        {
            InitializeComponent();

            ViewModel = new FrequencyDebugWidgetViewModel();
            DataContext = ViewModel;

            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            ViewModel.OnAttached(this);
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            ViewModel.OnDetached();
        }

        private void OnMinimizedIconPressed(
            object? sender,
            Avalonia.Input.PointerPressedEventArgs e
        )
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);
            // DON'T mark as handled - let drag behavior process it too
        }

        private void OnMinimizedIconReleased(
            object? sender,
            Avalonia.Input.PointerReleasedEventArgs e
        )
        {
            var releasePosition = e.GetPosition((Control)sender!);
            var distance =
                Math.Abs(releasePosition.X - _iconPressedPosition.X)
                + Math.Abs(releasePosition.Y - _iconPressedPosition.Y);

            // If pointer moved less than 5 pixels, treat as click (not drag)
            // Use smaller threshold than drag behavior (20px) to avoid conflicts
            if (distance < 5)
            {
                ViewModel.ExpandCommand.Execute(null);
                // DON'T mark as handled - let drag behavior clean up its state
            }
            // If distance >= 5, let drag behavior handle it
        }
    }
}
