using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// GenieWidget - AI-powered filter generation widget
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class GenieWidget : UserControl
    {
        public GenieWidgetViewModel? ViewModel { get; }

        // Track click vs drag for minimized icon
        private Avalonia.Point _iconPressedPosition;

        public GenieWidget()
        {
            ViewModel = new GenieWidgetViewModel();
            DataContext = ViewModel;

            InitializeComponent();

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex
            // ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex
            // this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead

            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            // Cleanup if needed
        }

        /// <summary>
        /// Track pointer pressed position to detect drag vs click
        /// </summary>
        private void OnMinimizedIconPressed(
            object? sender,
            Avalonia.Input.PointerPressedEventArgs e
        )
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);
            // DON'T mark as handled - let drag behavior process it too
        }

        /// <summary>
        /// On release: if no drag happened, expand the widget
        /// </summary>
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
            if (distance < 5 && ViewModel != null)
            {
                ViewModel.ExpandCommand.Execute(null);
                // DON'T mark as handled - let drag behavior clean up its state
            }
            // If distance >= 5, let drag behavior handle it
        }
    }
}
