using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class AudioMixerWidget : UserControl
    {
        public AudioMixerWidget()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnAttached(this);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnDetached();
            }
        }

        private Point _iconPressedPosition;

        private void OnMinimizedIconPressed(object? sender, PointerPressedEventArgs e)
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.BringToFront();
            }
            // DON'T mark as handled - let drag behavior process it too
        }

        private void OnMinimizedIconReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                var releasePosition = e.GetPosition((Control)sender!);
                var distance =
                    Math.Abs(releasePosition.X - _iconPressedPosition.X)
                    + Math.Abs(releasePosition.Y - _iconPressedPosition.Y);

                // If pointer moved less than 5 pixels, treat as click (not drag)
                // Use smaller threshold than drag behavior (20px) to avoid conflicts
                if (distance < 5)
                {
                    if (vm.IsMinimized)
                    {
                        vm.ExpandCommand.Execute(null);
                    }
                    else
                    {
                        vm.MinimizeCommand.Execute(null);
                    }
                    // DON'T mark as handled - let drag behavior clean up its state
                }
                // If distance >= 5, let drag behavior handle it
            }
        }
    }
}
