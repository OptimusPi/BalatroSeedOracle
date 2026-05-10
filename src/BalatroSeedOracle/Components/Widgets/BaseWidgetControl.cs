using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Base class for all widget UserControls.
    /// Provides common functionality like click vs drag detection.
    /// </summary>
    public abstract class BaseWidgetControl : UserControl
    {
        // Track press position for click vs drag detection
        private Point _iconPressedPosition;

        /// <summary>
        /// Handle pointer press on minimized widget icon.
        /// Records position for click detection.
        /// </summary>
        protected void OnMinimizedIconPressed(object? sender, PointerPressedEventArgs e)
        {
            _iconPressedPosition = e.GetPosition((Control)sender!);

            // Bring widget to front when interacted with
            if (DataContext is BaseWidgetViewModel vm)
            {
                vm.BringToFront();
            }
        }

        /// <summary>
        /// Handle pointer release on minimized widget icon.
        /// Expands widget if movement is less than 5px (click, not drag).
        /// </summary>
        protected void OnMinimizedIconReleased(object? sender, PointerReleasedEventArgs e)
        {
            var currentPosition = e.GetPosition((Control)sender!);
            var distance =
                Math.Abs(_iconPressedPosition.X - currentPosition.X)
                + Math.Abs(_iconPressedPosition.Y - currentPosition.Y);

            // If pointer moved less than 5px, treat as click and expand
            if (distance < 5 && DataContext is BaseWidgetViewModel vm)
            {
                vm.ExpandCommand.Execute(null);
            }
        }

        /// <summary>
        /// Common initialization for widget lifecycle.
        /// Called when widget is attached to visual tree.
        /// </summary>
        protected virtual void OnWidgetAttached()
        {
            // Override in derived classes for custom initialization
        }

        /// <summary>
        /// Common cleanup for widget lifecycle.
        /// Called when widget is detached from visual tree.
        /// </summary>
        protected virtual void OnWidgetDetached()
        {
            // Override in derived classes for custom cleanup
        }

        protected BaseWidgetControl()
        {
            AttachedToVisualTree += (s, e) => OnWidgetAttached();
            DetachedFromVisualTree += (s, e) => OnWidgetDetached();
        }
    }
}
