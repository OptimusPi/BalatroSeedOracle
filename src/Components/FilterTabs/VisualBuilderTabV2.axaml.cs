using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Components.FilterTabs
{
    public partial class VisualBuilderTabV2 : UserControl
    {
        private FilterItem? _draggedItem;
        private Point _dragStartPoint;
        private bool _isDragging;
        
        public VisualBuilderTabV2()
        {
            InitializeComponent();
            SetupDragDrop();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void SetupDragDrop()
        {
            // Get drop zones and set up event handlers
            var mustDropZone = this.FindControl<Border>("MustDropZone");
            var shouldDropZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotDropZone = this.FindControl<Border>("MustNotDropZone");
            
            if (mustDropZone != null)
            {
                mustDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                mustDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                mustDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }
            
            if (shouldDropZone != null)
            {
                shouldDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                shouldDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                shouldDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }
            
            if (mustNotDropZone != null)
            {
                mustNotDropZone.AddHandler(DragDrop.DropEvent, OnDrop);
                mustNotDropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                mustNotDropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            }
        }
        
        private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is FilterItem item)
            {
                _draggedItem = item;
                _dragStartPoint = e.GetPosition(this);
                _isDragging = false;
            }
        }
        
        private async void OnItemPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedItem != null && !_isDragging)
            {
                var currentPoint = e.GetPosition(this);
                var distance = Point.Distance(_dragStartPoint, currentPoint);
                
                if (distance > 5) // Start drag after 5 pixels of movement
                {
                    _isDragging = true;
                    
                    var dragData = new DataObject();
                    dragData.Set("FilterItem", _draggedItem);
                    
                    await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
                    
                    _draggedItem = null;
                    _isDragging = false;
                }
            }
        }
        
        private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _draggedItem = null;
            _isDragging = false;
        }
        
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                e.DragEffects = e.Data.Contains("FilterItem") ? DragDropEffects.Copy : DragDropEffects.None;
                border.Classes.Add("drag-over");
            }
        }
        
        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Classes.Remove("drag-over");
            }
        }
        
        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Classes.Remove("drag-over");
                
                if (e.Data.Contains("FilterItem"))
                {
                    var item = e.Data.Get("FilterItem") as FilterItem;
                    if (item != null && DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel viewModel)
                    {
                        // Determine which collection to add to based on the drop zone
                        if (border.Name == "MustDropZone")
                        {
                            viewModel.AddToMustCommand.Execute(item);
                        }
                        else if (border.Name == "ShouldDropZone")
                        {
                            viewModel.AddToShouldCommand.Execute(item);
                        }
                        else if (border.Name == "MustNotDropZone")
                        {
                            viewModel.AddToMustNotCommand.Execute(item);
                        }
                    }
                }
            }
        }
    }
}