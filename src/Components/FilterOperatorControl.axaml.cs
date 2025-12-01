using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Models;

#pragma warning disable CS0618 // Suppress obsolete warnings for DataObject/DragDrop - new DataTransfer API not fully available in Avalonia 11.3

namespace BalatroSeedOracle.Components
{
    public partial class FilterOperatorControl : UserControl
    {
        private const string FilterItemFormatId = "BalatroSeedOracle.FilterItem";

        public FilterOperatorControl()
        {
            InitializeComponent();

            Helpers.DebugLogger.Log("FilterOperatorControl", "Constructor called");

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

            // Set up the operator control
            DataContextChanged += (s, e) =>
            {
                if (DataContext is FilterOperatorItem operatorItem)
                {
                    Helpers.DebugLogger.Log(
                        "FilterOperatorControl",
                        $"DataContext set - Type: {operatorItem.OperatorType}, Children: {operatorItem.Children.Count}"
                    );

                    // Subscribe to Children collection changes to update fanned layout
                    operatorItem.Children.CollectionChanged += OnChildrenCollectionChanged;

                    // Initial layout update
                    UpdateFannedLayout();

                    Helpers.DebugLogger.Log(
                        "FilterOperatorControl",
                        "UpdateFannedLayout() called from DataContext"
                    );
                }
                else
                {
                    Helpers.DebugLogger.Log(
                        "FilterOperatorControl",
                        "DataContext changed but not FilterOperatorItem"
                    );
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
            itemsControl.AddHandler(
                PointerPressedEvent,
                OnCardPointerPressed,
                handledEventsToo: true
            );
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Let the event bubble up - operators in drop zones need to be draggable
            // Parent handlers (OnDropZoneItemPointerPressed, OnTrayOrPointerPressed) will handle dragging
        }

        private async void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
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
                    var data = new DataObject();
                    data.Set(FilterItemFormatId, item);

                    await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
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
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    CalculateFannedPositions(operatorItem);
                },
                Avalonia.Threading.DispatcherPriority.Background
            );
        }

        private void CalculateFannedPositions(FilterOperatorItem operatorItem)
        {
            var itemsControl = this.FindControl<ItemsControl>("ChildrenItemsControl");
            if (itemsControl == null)
                return;

            int count = operatorItem.Children.Count;
            if (count == 0)
                return;

            // Enhanced fanning parameters for dramatic poker hand effect
            double cardWidth = UIConstants.JokerSpriteWidth;

            // Adjust parameters based on card count for optimal visual effect
            double baseAngle;
            double angleDelta;
            double xOffset;
            double yArcHeight;

            if (count == 1)
            {
                // Single card - no rotation, centered
                baseAngle = 0;
                angleDelta = 0;
                xOffset = 0;
                yArcHeight = 0;
            }
            else if (count == 2)
            {
                // Two cards - slight spread
                baseAngle = -8.0;
                angleDelta = 16.0;
                xOffset = 22.0;
                yArcHeight = 3.0;
            }
            else if (count <= 4)
            {
                // 3-4 cards - moderate fan
                baseAngle = -12.0;
                angleDelta = 24.0 / (count - 1);
                xOffset = 20.0;
                yArcHeight = 8.0;
            }
            else if (count <= 6)
            {
                // 5-6 cards - fuller fan
                baseAngle = -15.0;
                angleDelta = 30.0 / (count - 1);
                xOffset = 18.0;
                yArcHeight = 12.0;
            }
            else
            {
                // 7+ cards - dramatic poker hand fan
                baseAngle = -18.0;
                angleDelta = 36.0 / (count - 1);
                xOffset = 15.0;
                yArcHeight = 15.0;
            }

            // Center the fan horizontally
            double totalWidth = (count - 1) * xOffset + cardWidth;
            double startX = -totalWidth / 2.0 + cardWidth / 2.0;

            // Apply transforms to each card container
            var containers = itemsControl.GetVisualDescendants().OfType<Border>().ToList();

            for (int i = 0; i < Math.Min(count, containers.Count); i++)
            {
                var container = containers[i];
                if (container.RenderTransform is not TransformGroup transformGroup)
                    continue;

                // Calculate angle for this card (alternating left/right from center)
                double normalizedPosition = count > 1 ? (double)i / (count - 1) : 0.5;
                double angle = baseAngle + (i * angleDelta);

                // Calculate position with arc effect (cards curve upward at edges)
                double x = startX + (i * xOffset);

                // Parabolic arc: higher at edges, lower in center
                double centerOffset = normalizedPosition - 0.5; // -0.5 to 0.5
                double y = yArcHeight * (4 * centerOffset * centerOffset); // Parabola formula

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
            if (e.Data.Contains(FilterItemFormatId))
            {
                var item = e.Data.Get(FilterItemFormatId) as FilterItem;
                if (item is not null and not FilterOperatorItem)
                {
                    e.DragEffects = DragDropEffects.Move;
                    e.Handled = true;

                    // Balatro-style visual feedback - white border and colored overlay
                    if (sender is Border dropZone && !dropZone.Classes.Contains("drag-active"))
                    {
                        dropZone.Classes.Add("drag-active");
                    }

                    var operatorContainer = this.FindControl<Border>("OperatorContainer");
                    if (
                        operatorContainer != null
                        && !operatorContainer.Classes.Contains("drag-over")
                    )
                    {
                        operatorContainer.Classes.Add("drag-over");
                    }
                    return;
                }
            }

            e.DragEffects = DragDropEffects.None;
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

            if (e.Data.Contains(FilterItemFormatId))
            {
                var item = e.Data.Get(FilterItemFormatId) as FilterItem;
                if (item is not null and not FilterOperatorItem)
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
}
