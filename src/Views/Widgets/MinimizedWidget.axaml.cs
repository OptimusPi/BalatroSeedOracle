using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.Widgets;

namespace BalatroSeedOracle.Views.Widgets
{
    /// <summary>
    /// Minimized widget view - square button representation with drag support
    /// </summary>
    public partial class MinimizedWidget : UserControl
    {
        private bool _isDragging = false;
        private Point _startPoint;

        public MinimizedWidget()
        {
            InitializeComponent();
            SetupDragHandling();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupDragHandling()
        {
            var button = this.FindControl<Button>("WidgetButton");
            if (button != null)
            {
                button.PointerPressed += OnPointerPressed;
                button.PointerMoved += OnPointerMoved;
                button.PointerReleased += OnPointerReleased;
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _startPoint = e.GetCurrentPoint(this).Position;
                _isDragging = false;
                e.Pointer.Capture(this);
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.Pointer.Captured == this)
            {
                var currentPoint = e.GetCurrentPoint(this).Position;
                var diff = currentPoint - _startPoint;
                var distance = Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                
                if (!_isDragging && distance > 5) // Start drag threshold
                {
                    _isDragging = true;
                    // Notify container about drag start
                    if (DataContext is WidgetViewModel widget)
                    {
                        // Would notify parent container here
                    }
                }
                
                if (_isDragging)
                {
                    // Update position during drag
                    if (Parent is Visual parent)
                    {
                        var containerPoint = this.TranslatePoint(currentPoint, parent);
                        if (containerPoint.HasValue)
                        {
                            // Would update position in real implementation
                        }
                    }
                }
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                var releasePoint = e.GetCurrentPoint(this).Position;
                
                // Notify container about drag end
                if (DataContext is WidgetViewModel widget)
                {
                    // Would notify parent container with final position
                }
            }
            
            e.Pointer.Capture(null);
        }
    }
}