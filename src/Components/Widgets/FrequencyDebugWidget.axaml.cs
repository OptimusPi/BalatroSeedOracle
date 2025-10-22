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

        private void OnMinimizedIconPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);
        }

        private void OnMinimizedIconReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            var releasePosition = e.GetPosition((Control)sender!);
            var distance = Math.Abs(releasePosition.X - _iconPressedPosition.X) + Math.Abs(releasePosition.Y - _iconPressedPosition.Y);

            if (distance < 20)
            {
                ViewModel.ExpandCommand.Execute(null);
            }
        }
    }
}
