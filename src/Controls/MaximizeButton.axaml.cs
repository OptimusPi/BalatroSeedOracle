using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace BalatroSeedOracle.Controls
{
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
        
        private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
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
            if (_parentWindow == null) return;
            
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
            if (_parentWindow == null) return;
            
            var iconBlock = this.FindControl<TextBlock>("IconBlock")!;
            var labelBlock = this.FindControl<TextBlock>("LabelBlock")!;
            var maximizeBtn = this.FindControl<Button>("MaximizeBtn")!;
            
            _isMaximized = _parentWindow.WindowState == WindowState.Maximized;
            
            if (_isMaximized)
            {
                // Show restore icon and tooltip
                iconBlock.Text = "🗗";  // Restore icon
                ToolTip.SetTip(maximizeBtn, "Restore Window");
                labelBlock.Text = "";
            }
            else
            {
                // Show maximize icon and tooltip
                iconBlock.Text = "⛶";  // Maximize icon
                ToolTip.SetTip(maximizeBtn, "Maximize Window");
                labelBlock.Text = "";
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
            if (_parentWindow == null) return;
            
            _parentWindow.WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
        }
        
        /// <summary>
        /// Show text label next to icon
        /// </summary>
        public void ShowLabel(bool show)
        {
            var labelBlock = this.FindControl<TextBlock>("LabelBlock")!;
            
            if (show)
            {
                labelBlock.Text = _isMaximized ? "RESTORE" : "MAXIMIZE";
            }
            else
            {
                labelBlock.Text = "";
            }
        }
    }
}
