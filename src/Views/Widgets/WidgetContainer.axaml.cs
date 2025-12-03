using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels.Widgets;

namespace BalatroSeedOracle.Views.Widgets
{
    /// <summary>
    /// Widget container view - manages the workspace layout with grid support
    /// </summary>
    public partial class WidgetContainer : UserControl
    {
        private Canvas? _gridOverlay;
        private Canvas? _dockZonesCanvas;
        private Canvas? _widgetCanvas;

        public WidgetContainer()
        {
            InitializeComponent();
            SetupGridOverlay();
            SetupDragAndDrop();
            SetupWidgetContentBinding();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _gridOverlay = this.FindControl<Canvas>("GridOverlay");
            _dockZonesCanvas = this.FindControl<Canvas>("DockZonesCanvas");
            _widgetCanvas = this.FindControl<Canvas>("WidgetCanvas");
        }

        private void SetupGridOverlay()
        {
            // Grid overlay setup for drag feedback
            SizeChanged += OnSizeChanged;
        }

        private void SetupDragAndDrop()
        {
            // Setup pointer events for drag and drop
            if (_widgetCanvas != null)
            {
                _widgetCanvas.PointerMoved += OnCanvasPointerMoved;
                _widgetCanvas.PointerReleased += OnCanvasPointerReleased;
            }
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            // Update drag position for docking service
            if (DataContext is WidgetContainerViewModel viewModel && viewModel.IsDragging)
            {
                var position = e.GetPosition(_widgetCanvas);
                // The docking service handles highlighting dock zones
            }
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // End drag operation
            if (DataContext is WidgetContainerViewModel viewModel && viewModel.IsDragging)
            {
                var position = e.GetPosition(_widgetCanvas);
                viewModel.EndDragCommand.Execute(position);
            }
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Update grid overlay when container size changes
            if (DataContext is WidgetContainerViewModel viewModel)
            {
                viewModel.UpdateContainerSize(e.NewSize);
            }
        }

        /// <summary>
        /// Show grid overlay during drag operations
        /// </summary>
        public void ShowGridOverlay()
        {
            if (_gridOverlay != null)
                _gridOverlay.IsVisible = true;
        }

        /// <summary>
        /// Hide grid overlay
        /// </summary>
        public void HideGridOverlay()
        {
            if (_gridOverlay != null)
                _gridOverlay.IsVisible = false;
        }

        /// <summary>
        /// Setup widget content binding to handle GetContentView() calls
        /// </summary>
        private void SetupWidgetContentBinding()
        {
            // Listen for DataContext changes to update widget content
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is WidgetContainerViewModel vm)
            {
                // Subscribe to widget collection changes
                vm.Widgets.CollectionChanged += OnWidgetsCollectionChanged;
            }
        }

        private void OnWidgetsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // When widgets are added/removed, update their content presenters
            if (e.NewItems != null)
            {
                foreach (var newWidget in e.NewItems.OfType<IWidget>())
                {
                    UpdateWidgetContent(newWidget);
                }
            }
        }

        private void UpdateWidgetContent(IWidget widget)
        {
            // Find the ContentPresenter for this widget and set its content
            // This is a simplified approach - in a real implementation you'd need to
            // find the specific ContentPresenter for this widget instance
            try
            {
                var contentView = widget.GetContentView();
                // The ContentPresenter will be updated through the DataTemplate
                // For now, this ensures the content is available
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update widget content: {ex.Message}");
            }
        }
    }
}