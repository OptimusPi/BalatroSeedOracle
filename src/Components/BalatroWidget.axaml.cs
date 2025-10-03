using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class BalatroWidget : UserControl
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public BalatroWidget()
        {
            InitializeComponent();
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is BaseWidgetViewModel viewModel && !viewModel.IsMinimized)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                e.Pointer.Capture(sender as Control);
            }
        }

        private void OnHeaderPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDragging && DataContext is BaseWidgetViewModel viewModel)
            {
                var currentPoint = e.GetPosition(this);
                var deltaX = currentPoint.X - _dragStartPoint.X;
                var deltaY = currentPoint.Y - _dragStartPoint.Y;

                viewModel.PositionX += deltaX;
                viewModel.PositionY += deltaY;

                _dragStartPoint = currentPoint;
            }
        }

        private void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);
            }
        }
    }
}
