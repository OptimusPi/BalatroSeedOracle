using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Windows
{
    /// <summary>
    /// Base widget window with screen edge snapping capabilities
    /// Replaces the custom desktop canvas system with proper Avalonia windows
    /// </summary>
    public partial class WidgetWindow : Window
    {
        private const double SnapDistance = 30.0; // Distance from edge to trigger snap
        private const double SnapOffset = 10.0;   // Distance from edge when snapped
        private bool _isSnapped = false;
        private SnapEdge _snapEdge = SnapEdge.None;
        private Point _dragStartPoint;
        private PixelPoint _windowStartPos;
        private Size _windowStartSize;
        
        public enum SnapEdge
        {
            None, Left, Right, Top, Bottom, 
            TopLeft, TopRight, BottomLeft, BottomRight
        }

        public WidgetWindow()
        {
            // Set up window properties
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is not null)
            {
                Owner = desktop.MainWindow;
            }

            SystemDecorations = SystemDecorations.None;
            // TransparencyLevelHint = WindowTransparencyLevel.Transparent;
            Background = Brushes.Transparent;
            
            // Subscribe to events
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PositionChanged += OnPositionChanged;
            Resized += OnResized;
            
            // Initialize snap indicator
            UpdateSnapIndicator();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            // Set window content from ViewModel
            if (DataContext is BaseWidgetViewModel vm)
            {
                Content = vm.WidgetContent;
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is not Control control || control.Classes.Contains("no-drag"))
                return;

            // Start drag operation
            _dragStartPoint = e.GetPosition(this);
            _windowStartPos = Position;
            _windowStartSize = ClientSize;
            
            // Unsnap if we were snapped
            if (_isSnapped)
            {
                _isSnapped = false;
                _snapEdge = SnapEdge.None;
                UpdateSnapIndicator();
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && !_isSnapped)
            {
                var currentPoint = e.GetPosition(this);
                var delta = currentPoint - _dragStartPoint;
                
                // Update window position
                var newPos = new PixelPoint(
                    (int)(_windowStartPos.X + delta.X),
                    (int)(_windowStartPos.Y + delta.Y)
                );
                
                Position = newPos;
                
                // Check for snap opportunities
                CheckAndSnapToEdges();
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Snap operation completed in OnPointerMoved
        }

        private void OnPositionChanged(object? sender, PixelPointEventArgs e)
        {
            UpdateSnapIndicator();
        }

        private void OnResized(object? sender, WindowResizedEventArgs e)
        {
            UpdateSnapIndicator();
        }

        private void CheckAndSnapToEdges()
        {
            if (_isSnapped) return; // Don't snap if already snapped
            
            // Use actual screen bounds; fallback to window bounds if unavailable
            var primary = Screens?.Primary;
            var screenBounds = primary?.WorkingArea is { } pixelRect 
                ? new Rect(pixelRect.X, pixelRect.Y, pixelRect.Width, pixelRect.Height)
                : new Rect(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
            var windowBounds = new Rect(Position.X, Position.Y, ClientSize.Width, ClientSize.Height);
            
            // Check each edge
            if (Math.Abs(windowBounds.Left - screenBounds.Left) < SnapDistance)
            {
                SnapToEdge(SnapEdge.Left, screenBounds);
            }
            else if (Math.Abs(screenBounds.Right - windowBounds.Right) < SnapDistance)
            {
                SnapToEdge(SnapEdge.Right, screenBounds);
            }
            else if (Math.Abs(windowBounds.Top - screenBounds.Top) < SnapDistance)
            {
                SnapToEdge(SnapEdge.Top, screenBounds);
            }
            else if (Math.Abs(screenBounds.Bottom - windowBounds.Bottom) < SnapDistance)
            {
                SnapToEdge(SnapEdge.Bottom, screenBounds);
            }
            else if (Math.Abs(windowBounds.Left - screenBounds.Left) < SnapDistance && 
                     Math.Abs(windowBounds.Top - screenBounds.Top) < SnapDistance)
            {
                SnapToEdge(SnapEdge.TopLeft, screenBounds);
            }
            else if (Math.Abs(screenBounds.Right - windowBounds.Right) < SnapDistance && 
                     Math.Abs(windowBounds.Top - screenBounds.Top) < SnapDistance)
            {
                SnapToEdge(SnapEdge.TopRight, screenBounds);
            }
            else if (Math.Abs(windowBounds.Left - screenBounds.Left) < SnapDistance && 
                     Math.Abs(screenBounds.Bottom - windowBounds.Bottom) < SnapDistance)
            {
                SnapToEdge(SnapEdge.BottomLeft, screenBounds);
            }
            else if (Math.Abs(screenBounds.Right - windowBounds.Right) < SnapDistance && 
                     Math.Abs(screenBounds.Bottom - windowBounds.Bottom) < SnapDistance)
            {
                SnapToEdge(SnapEdge.BottomRight, screenBounds);
            }
        }

        private void SnapToEdge(SnapEdge edge, Rect screenBounds)
        {
            _isSnapped = true;
            _snapEdge = edge;
            
            var newX = Position.X;
            var newY = Position.Y;
            
            switch (edge)
            {
                case SnapEdge.Left:
                    newX = (int)(screenBounds.Left + SnapOffset);
                    break;
                case SnapEdge.Right:
                    newX = (int)(screenBounds.Right - ClientSize.Width - SnapOffset);
                    break;
                case SnapEdge.Top:
                    newY = (int)(screenBounds.Top + SnapOffset);
                    break;
                case SnapEdge.Bottom:
                    newY = (int)(screenBounds.Bottom - ClientSize.Height - SnapOffset);
                    break;
                case SnapEdge.TopLeft:
                    newX = (int)(screenBounds.Left + SnapOffset);
                    newY = (int)(screenBounds.Top + SnapOffset);
                    break;
                case SnapEdge.TopRight:
                    newX = (int)(screenBounds.Right - ClientSize.Width - SnapOffset);
                    newY = (int)(screenBounds.Top + SnapOffset);
                    break;
                case SnapEdge.BottomLeft:
                    newX = (int)(screenBounds.Left + SnapOffset);
                    newY = (int)(screenBounds.Bottom - ClientSize.Height - SnapOffset);
                    break;
                case SnapEdge.BottomRight:
                    newX = (int)(screenBounds.Right - ClientSize.Width - SnapOffset);
                    newY = (int)(screenBounds.Bottom - ClientSize.Height - SnapOffset);
                    break;
            }
            
            Position = new PixelPoint(newX, newY);
            UpdateSnapIndicator();
        }

        private void UpdateSnapIndicator()
        {
            var indicator = this.FindControl<Border>("SnapIndicator");
            if (indicator != null)
            {
                indicator.IsVisible = _isSnapped;
                
                // Update indicator position based on snap edge
                if (_isSnapped)
                {
                    indicator.Margin = _snapEdge switch
                    {
                        SnapEdge.Top => new Thickness(0, -20, 0, 0),
                        SnapEdge.Bottom => new Thickness(0, 0, 0, -20),
                        SnapEdge.Left => new Thickness(-20, 0, 0, 0),
                        SnapEdge.Right => new Thickness(0, 0, -20, 0),
                        SnapEdge.TopLeft => new Thickness(-20, -20, 0, 0),
                        SnapEdge.TopRight => new Thickness(0, -20, -20, 0),
                        SnapEdge.BottomLeft => new Thickness(-20, 0, 0, -20),
                        SnapEdge.BottomRight => new Thickness(0, 0, -20, -20),
                        _ => new Thickness(0, -20, 0, 0)
                    };
                }
            }
        }

        /// <summary>
        /// Create a widget window with the specified content
        /// </summary>
        public static WidgetWindow Create<T>(T content) where T : Control
        {
            var window = new WidgetWindow();
            window.Content = content;
            return window;
        }

        /// <summary>
        /// Create a widget window with ViewModel content
        /// </summary>
        public static WidgetWindow Create(BaseWidgetViewModel viewModel)
        {
            var window = new WidgetWindow();
            window.DataContext = viewModel;
            return window;
        }
    }
}
