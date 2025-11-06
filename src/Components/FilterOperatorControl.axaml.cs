using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Models;
using System;
using System.Collections.Specialized;
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

            // Make the entire container draggable by clicking the header
            var headerBorder = this.FindControl<Border>("OperatorHeader");
            if (headerBorder != null)
            {
                headerBorder.PointerPressed += OnHeaderPointerPressed;
            }

            // Set up the operator type tag for styling
            DataContextChanged += (s, e) =>
            {
                if (DataContext is FilterOperatorItem operatorItem)
                {
                    var containerBorder = this.FindControl<Border>("OperatorContainer");
                    var headerBorder = this.FindControl<Border>("OperatorHeader");

                    // Set the border color based on operator type
                    if (containerBorder != null)
                    {
                        containerBorder.BorderBrush = operatorItem.OperatorType == "OR"
                            ? Application.Current?.FindResource("Green") as Avalonia.Media.IBrush
                            : Application.Current?.FindResource("Red") as Avalonia.Media.IBrush;
                    }

                    // Set the header background based on operator type
                    if (headerBorder != null)
                    {
                        headerBorder.Background = operatorItem.OperatorType == "OR"
                            ? Application.Current?.FindResource("Green") as Avalonia.Media.IBrush
                            : Application.Current?.FindResource("Red") as Avalonia.Media.IBrush;
                    }

                    // Subscribe to Children collection changes to update fanned layout
                    operatorItem.Children.CollectionChanged += OnChildrenCollectionChanged;

                    // Initial layout update
                    UpdateFannedLayout();
                }
            };

            // Attach to visual tree to set up card drag handlers
            this.AttachedToVisualTree += (s, e) => SetupCardDragHandlers();
        }

        private void SetupCardDragHandlers()
        {
            var itemsControl = this.FindControl<ItemsControl>("ChildrenItemsControl");
            if (itemsControl == null)
                return;

            // Add pointer handlers to enable dragging individual cards
            itemsControl.AddHandler(PointerPressedEvent, OnCardPointerPressed, handledEventsToo: true);
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Let the event bubble up - operators in drop zones need to be draggable
            // Parent handlers (OnDropZoneItemPointerPressed, OnTrayOrPointerPressed) will handle dragging
        }

        private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Find the card that was clicked
            var point = e.GetCurrentPoint(this);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            // Walk up the visual tree to find the Border container
            var source = e.Source as Visual;
            while (source != null)
            {
                if (source is Border border && border.DataContext is FilterItem item)
                {
                    // Start drag operation for this individual card
                    var data = new Avalonia.Input.DataObject();
                    data.Set("FilterItem", item);

                    DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
                    e.Handled = true;
                    return;
                }
                source = source.GetVisualParent();
            }
        }

        private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the fanned card layout whenever children change
            UpdateFannedLayout();
        }

        private void UpdateFannedLayout()
        {
            if (DataContext is not FilterOperatorItem operatorItem)
                return;

            var itemsControl = this.FindControl<ItemsControl>("ChildrenItemsControl");
            if (itemsControl == null)
                return;

            // Wait for the visual tree to be FULLY rendered (Background priority runs after layout/render)
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CalculateFannedPositions(operatorItem);
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        private void CalculateFannedPositions(FilterOperatorItem operatorItem)
        {
            var itemsControl = this.FindControl<ItemsControl>("ChildrenItemsControl");
            if (itemsControl == null)
                return;

            int count = operatorItem.Children.Count;
            if (count == 0)
                return;

            // Fanning parameters (poker hand style)
            double baseAngle = -15.0;  // Start angle for first card
            double angleDelta = count > 1 ? 30.0 / (count - 1) : 0;  // Spread across 30 degrees
            double xOffset = 18.0;     // Horizontal spacing between cards
            double cardWidth = 40.0;

            // Center the fan
            double totalWidth = (count - 1) * xOffset + cardWidth;
            double startX = -totalWidth / 2.0 + cardWidth / 2.0;

            // Apply transforms to each card container
            var containers = itemsControl.GetVisualDescendants().OfType<Border>().ToList();

            for (int i = 0; i < Math.Min(count, containers.Count); i++)
            {
                var container = containers[i];
                if (container.RenderTransform is not TransformGroup transformGroup)
                    continue;

                // Calculate angle for this card
                double angle = baseAngle + (i * angleDelta);

                // Calculate position
                double x = startX + (i * xOffset);
                double y = 0;

                // Update transforms
                if (transformGroup.Children.Count >= 2)
                {
                    if (transformGroup.Children[0] is RotateTransform rotateTransform)
                    {
                        rotateTransform.Angle = angle;
                    }

                    if (transformGroup.Children[1] is TranslateTransform translateTransform)
                    {
                        translateTransform.X = x;
                        translateTransform.Y = y;
                    }
                }

                // Set Z-Index so cards on the right appear in front
                // Use high base value (100) to ensure cards render above operator borders
                container.ZIndex = 100 + i;
            }
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
