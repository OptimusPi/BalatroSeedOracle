using System;
using System.Linq;
using Avalonia;
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
            if (_parentWindow != null)
            {
                _parentWindow.PropertyChanged -= OnParentWindowPropertyChanged;
            }

            // Find the parent window
            _parentWindow = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();

            if (_parentWindow != null)
            {
                _parentWindow.PropertyChanged += OnParentWindowPropertyChanged;
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

        private void OnParentWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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
                    _parentWindow.WindowState = WindowState.Normal;
                }
                else
                {
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

            _isMaximized = _parentWindow.WindowState == WindowState.Maximized;

            if (_isMaximized)
            {
                IconBlock.Data = (Avalonia.Media.Geometry)this.FindResource("WindowRestoreIconData")!;
                ToolTip.SetTip(MaximizeBtn, "Restore Window");
                Avalonia.Automation.AutomationProperties.SetName(MaximizeBtn, "Restore Window");
                LabelBlock.Text = "";
            }
            else
            {
                IconBlock.Data = (Avalonia.Media.Geometry)this.FindResource("FullscreenIconData")!;
                ToolTip.SetTip(MaximizeBtn, "Maximize Window");
                Avalonia.Automation.AutomationProperties.SetName(MaximizeBtn, "Maximize Window");
                LabelBlock.Text = "";
            }

            MaximizeStateChanged?.Invoke(this, _isMaximized);
        }

        public bool IsMaximized => _isMaximized;

        public void SetMaximized(bool maximized)
        {
            if (_parentWindow == null)
                return;

            _parentWindow.WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
        }

        public void ShowLabel(bool show)
        {
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
