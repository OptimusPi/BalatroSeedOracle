using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// AudioVisualizerSettingsWidget - A movable, minimizable widget for audio visualizer settings
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class AudioVisualizerSettingsWidget : UserControl
    {
        public AudioVisualizerSettingsWidgetViewModel ViewModel { get; }

        // Track click vs drag for minimized icon
        private Avalonia.Point _iconPressedPosition;

        public AudioVisualizerSettingsWidget()
        {
            InitializeComponent();

            // Get ViewModel from DI container
            ViewModel =
                ServiceHelper.GetService<AudioVisualizerSettingsWidgetViewModel>()
                ?? throw new InvalidOperationException(
                    "AudioVisualizerSettingsWidgetViewModel service not registered in DI container"
                );
            DataContext = ViewModel;

            // Update ZIndex when IsMinimized changes - now handled by XAML binding to WidgetZIndex
            // ViewModel.PropertyChanged += (s, e) => { ... }; // REMOVED - use XAML binding instead

            // Set initial ZIndex - now handled by XAML binding to WidgetZIndex
            // this.ZIndex = ViewModel.IsMinimized ? 1 : 100; // REMOVED - use XAML binding instead

            // REMOVED: Initialize() method no longer exists
            // ViewModel.Initialize();

            // Wire up cleanup
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // CRITICAL: Initialize ViewModel with ownerControl reference so it can find BalatroMainMenu
            ViewModel.OnAttached(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            ViewModel.OnDetached();
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
            if (distance < 5)
            {
                ViewModel.ExpandCommand.Execute(null);
                // DON'T mark as handled - let drag behavior clean up its state
            }
            // If distance >= 5, let drag behavior handle it
        }
    }
}
