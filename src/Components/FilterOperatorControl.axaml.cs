using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Components
{
    public partial class FilterOperatorControl : UserControl
    {
        public FilterOperatorControl()
        {
            InitializeComponent();

            // Set up drag-drop handlers for the child drop zone
            var dropZone = this.FindControl<Border>("ChildrenDropZone");
            if (dropZone != null)
            {
                dropZone.AddHandler(DragDrop.DragOverEvent, OnChildrenDragOver);
                dropZone.AddHandler(DragDrop.DropEvent, OnChildrenDrop);
            }
        }

        private void OnChildrenDragOver(object? sender, DragEventArgs e)
        {
            // Only allow FilterItems (not operators) to be dropped here
            if (e.Data.Get("FilterItem") is FilterItem item && item is not FilterOperatorItem)
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;

                // Visual feedback - highlight the drop zone
                if (sender is Border border)
                {
                    border.Opacity = 0.7;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void OnChildrenDrop(object? sender, DragEventArgs e)
        {
            // Reset visual feedback
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }

            if (e.Data.Get("FilterItem") is FilterItem item && item is not FilterOperatorItem)
            {
                // Get the operator item from DataContext
                if (DataContext is FilterOperatorItem operatorItem)
                {
                    // Add the item to this operator's children
                    operatorItem.Children.Add(item);
                    e.Handled = true;
                }
            }
        }
    }
}
