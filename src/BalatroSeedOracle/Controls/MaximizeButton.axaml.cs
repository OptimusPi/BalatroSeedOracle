using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Material;

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
        }

        private void OnAttachedToVisualTree(
            object? sender,
            Avalonia.VisualTreeAttachmentEventArgs e
        )
        {
            // Find the parent window
            _parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();

            if (_parentWindow != null)
            {
                // Subscribe to window state changes
                _parentWindow.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Window.WindowStateProperty)
                    {
                        UpdateButtonState();
                    }
                };

                // Initialize button state
                UpdateButtonState();
            }
        }

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
                IconBlock.Kind = PackIconMaterialKind.WindowRestore;
                ToolTip.SetTip(MaximizeBtn, "Restore Window");
                LabelBlock.Text = "";
            }
            else
            {
                // Show maximize icon and tooltip
                IconBlock.Kind = PackIconMaterialKind.Fullscreen;
                ToolTip.SetTip(MaximizeBtn, "Maximize Window");
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
