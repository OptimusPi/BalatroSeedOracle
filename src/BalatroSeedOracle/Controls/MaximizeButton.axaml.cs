using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace BalatroSeedOracle.Controls
{
    /// <summary>
    /// Maximize/restore button for windows.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class MaximizeButton : UserControl
    {
        private Window? _parentWindow;
        private bool _isMaximized = false;

        public event EventHandler<bool>? MaximizeStateChanged;

        public MaximizeButton()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void OnAttachedToVisualTree(
            object? sender,
            Avalonia.VisualTreeAttachmentEventArgs e
        )
        {
            // Detach from any previous window first — re-attaching to the same or a
            // different window must not stack a second subscription on top.
            if (_parentWindow != null)
            {
                _parentWindow.PropertyChanged -= OnParentWindowPropertyChanged;
            }

            // Find the parent window
            _parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();

            if (_parentWindow != null)
            {
                _parentWindow.PropertyChanged += OnParentWindowPropertyChanged;

                // Initialize button state
                UpdateButtonState();
            }
        }

        private void OnDetachedFromVisualTree(
            object? sender,
            Avalonia.VisualTreeAttachmentEventArgs e
        )
        {
            if (_parentWindow != null)
            {
                _parentWindow.PropertyChanged -= OnParentWindowPropertyChanged;
                _parentWindow = null;
            }
        }

        private void OnParentWindowPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Window.WindowStateProperty)
            {
                UpdateButtonState();
            }
        }

        // view-only: OK — window-state toggle is view-layer code
        private void MaximizeBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (_parentWindow == null)
                return;

            try
            {
                if (_isMaximized)
                {
                    // Restore to normal
                    _parentWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    // Maximize
                    _parentWindow.WindowState = WindowState.Maximized;
                }
            }
            catch
            {
                // Handle any errors silently
            }
        }

        private void UpdateButtonState()
        {
            if (_parentWindow == null)
                return;

            // Direct x:Name field access - no FindControl!
            _isMaximized = _parentWindow.WindowState == WindowState.Maximized;

            if (_isMaximized)
            {
                // Show restore icon and tooltip
                IconBlock.Data = (Avalonia.Media.Geometry)this.FindResource("WindowRestoreIconData")!;
                ToolTip.SetTip(MaximizeBtn, "Restore Window");
                // The visible text label collapses to "" in narrow layouts (see ShowLabel) —
                // AutomationProperties.Name keeps this button's purpose available to screen
                // readers regardless of whether the text label is currently shown.
                Avalonia.Automation.AutomationProperties.SetName(MaximizeBtn, "Restore Window");
                LabelBlock.Text = "";
            }
            else
            {
                // Show maximize icon and tooltip
                IconBlock.Data = (Avalonia.Media.Geometry)this.FindResource("FullscreenIconData")!;
                ToolTip.SetTip(MaximizeBtn, "Maximize Window");
                Avalonia.Automation.AutomationProperties.SetName(MaximizeBtn, "Maximize Window");
                LabelBlock.Text = "";
            }

            // Fire event for any listeners
            MaximizeStateChanged?.Invoke(this, _isMaximized);
        }

        /// <summary>
        /// Get current maximize state
        /// </summary>
        public bool IsMaximized => _isMaximized;

        /// <summary>
        /// Programmatically set window state
        /// </summary>
        public void SetMaximized(bool maximized)
        {
            if (_parentWindow == null)
                return;

            _parentWindow.WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
        }

        /// <summary>
        /// Show text label next to icon
        /// </summary>
        public void ShowLabel(bool show)
        {
            // Direct x:Name field access - no FindControl!
            if (show)
            {
                LabelBlock.Text = _isMaximized ? "RESTORE" : "MAXIMIZE";
            }
            else
            {
                LabelBlock.Text = "";
            }
        }
    }
}
