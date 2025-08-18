using Avalonia.Controls;
using System;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Reusable base class for widgets that have a minimized and expanded view.
    /// Derived XAML should expose controls named "MinimizedView" and "ExpandedView".
    /// </summary>
    public abstract class CollapsibleWidgetBase : UserControl
    {
        protected Control? MinimizedView { get; private set; }
        protected Control? ExpandedView { get; private set; }
        public bool IsExpanded { get; private set; }

        protected void InitializeCollapsibleParts()
        {
            MinimizedView = this.FindControl<Control>("MinimizedView");
            ExpandedView = this.FindControl<Control>("ExpandedView");
            // If not found, leave null; derived classes can still function.
        }

        protected void Expand()
        {
            if (IsExpanded) return;
            IsExpanded = true;
            if (MinimizedView != null) MinimizedView.IsVisible = false;
            if (ExpandedView != null) ExpandedView.IsVisible = true;
            OnExpanded();
        }

        protected void Collapse()
        {
            if (!IsExpanded) return;
            IsExpanded = false;
            if (MinimizedView != null) MinimizedView.IsVisible = true;
            if (ExpandedView != null) ExpandedView.IsVisible = false;
            OnCollapsed();
        }

        protected virtual void OnExpanded() { }
        protected virtual void OnCollapsed() { }

        protected void Toggle()
        {
            if (IsExpanded) Collapse(); else Expand();
        }

        // Helpers for derived click handlers
        protected void HandleMinimizedClick(object? sender, Avalonia.Input.PointerPressedEventArgs e) => Expand();
        protected void HandleMinimizeButton(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Collapse();
    }
}
