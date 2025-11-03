using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.Models;
using System.Linq;

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
                dropZone.AddHandler(DragDrop.DragLeaveEvent, OnChildrenDragLeave);
            }

            // Set up the operator type tag for styling
            DataContextChanged += (s, e) =>
            {
                if (DataContext is FilterOperatorItem operatorItem)
                {
                    var headerBorder = this.FindControl<Border>("OperatorContainer");
                    var labelBorder = this.Get<Border>("Grid").Children.OfType<Border>().FirstOrDefault();
                    if (labelBorder != null)
                    {
                        labelBorder.Tag = operatorItem.OperatorType;
                    }

                    // Also set the border color based on operator type
                    if (headerBorder != null)
                    {
                        headerBorder.BorderBrush = operatorItem.OperatorType == "OR"
                            ? Application.Current?.FindResource("Green") as Avalonia.Media.IBrush
                            : Application.Current?.FindResource("Blue") as Avalonia.Media.IBrush;
                    }
                }
            };
        }

        private void OnChildrenDragOver(object? sender, DragEventArgs e)
        {
            // Only allow FilterItems (not operators) to be dropped here
            if (e.Data.Get("FilterItem") is FilterItem item && item is not FilterOperatorItem)
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;

                // Balatro-style visual feedback - white border and colored overlay
                if (sender is Border dropZone && !dropZone.Classes.Contains("drag-active"))
                {
                    dropZone.Classes.Add("drag-active");
                }

                var operatorContainer = this.FindControl<Border>("OperatorContainer");
                if (operatorContainer != null && !operatorContainer.Classes.Contains("drag-over"))
                {
                    operatorContainer.Classes.Add("drag-over");
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        private void OnChildrenDragLeave(object? sender, DragEventArgs e)
        {
            // Reset visual feedback - remove Balatro-style highlighting
            if (sender is Border dropZone)
            {
                dropZone.Classes.Remove("drag-active");
            }

            var operatorContainer = this.FindControl<Border>("OperatorContainer");
            if (operatorContainer != null)
            {
                operatorContainer.Classes.Remove("drag-over");
            }
        }

        private void OnChildrenDrop(object? sender, DragEventArgs e)
        {
            // Reset visual feedback - remove Balatro-style highlighting
            if (sender is Border dropZone)
            {
                dropZone.Classes.Remove("drag-active");
            }

            var operatorContainer = this.FindControl<Border>("OperatorContainer");
            if (operatorContainer != null)
            {
                operatorContainer.Classes.Remove("drag-over");
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
