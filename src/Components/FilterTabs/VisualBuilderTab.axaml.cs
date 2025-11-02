using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Constants;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// PROPER MVVM Visual Builder Tab - zero business logic, pure view
    /// ViewModel set via XAML binding, not code-behind
    /// </summary>
    public partial class VisualBuilderTab : UserControl
    {
        private Models.FilterItem? _draggedItem;
        private Border? _dragAdorner;
        private TranslateTransform? _adornerTransform; // Transform for positioning the ghost (allows sway to work)
        private AdornerLayer? _adornerLayer;
        private TopLevel? _topLevel;
        private bool _isDragging = false;
        private bool _isAnimating = false; // Track if rubber-band animation is playing
        private Avalonia.Point _dragStartPosition;
        private Control? _originalDragSource; // Store the original control to animate back to
        private string? _sourceDropZone; // Track which drop zone the item came from (MustDropZone, ShouldDropZone, MustNotDropZone, or null for shelf)

        public VisualBuilderTab()
        {
            InitializeComponent();

            // Set DataContext to the VisualBuilderTabViewModel
            DataContext =
                ServiceHelper.GetRequiredService<ViewModels.FilterTabs.VisualBuilderTabViewModel>();

            // Setup drop zones AFTER the control is attached to visual tree
            this.AttachedToVisualTree += (s, e) => SetupDropZones();
        }

        private void SetupDropZones()
        {
            // Find drop zones - we'll check them manually via hit testing
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Drop zones found - Must: {mustZone != null}, Should: {shouldZone != null}, MustNot: {mustNotZone != null}"
            );

            // Attach pointer handlers to TopLevel so we get events even when pointer moves outside the control
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.PointerMoved += OnPointerMovedManualDrag;
                topLevel.PointerReleased += OnPointerReleasedManualDrag;
                DebugLogger.Log(
                    "VisualBuilderTab",
                    "Manual drag pointer handlers attached to TopLevel"
                );
            }
            else
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    "Failed to attach pointer handlers - no TopLevel"
                );
            }
        }

        private void OnCardPointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            // Play subtle card hover sound - disabled (NAudio removed)
            // SoundEffectService.Instance.PlayCardHover();

            // No rotation code needed - the BalatroCardSwayBehavior handles all animation
            // The invisible hitbox (sender) never rotates, preventing seizure-inducing flicker
        }

        private void OnItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Prevent starting a new drag if one is already in progress or animating
            if (_isDragging || _isAnimating)
                return;

            try
            {
                Models.FilterItem? item = null;

                // Handle Border (invisible hitbox), Image, StackPanel, and Grid elements
                if (sender is Border border)
                {
                    item = border.DataContext as Models.FilterItem;
                }
                else if (sender is Image image)
                {
                    item = image.DataContext as Models.FilterItem;
                }
                else if (sender is StackPanel stackPanel)
                {
                    item = stackPanel.DataContext as Models.FilterItem;
                }
                else if (sender is Grid grid)
                {
                    item = grid.DataContext as Models.FilterItem;
                }

                if (item != null)
                {
                    // Check which button was pressed
                    var pointerPoint = e.GetCurrentPoint(sender as Control);

                    // RIGHT-CLICK: Open config popup
                    if (pointerPoint.Properties.IsRightButtonPressed)
                    {
                        ShowItemConfigPopup(item, sender as Control);
                        e.Handled = true;
                        return;
                    }

                    // LEFT-CLICK ONLY: Start drag operation
                    if (!pointerPoint.Properties.IsLeftButtonPressed)
                    {
                        return; // Not left click, ignore
                    }
                    _draggedItem = item;
                    _isDragging = true;
                    _originalDragSource = sender as Control;
                    _sourceDropZone = null; // This item is from the shelf, not a drop zone

                    // Get position relative to TopLevel for absolute positioning
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel != null && _originalDragSource != null)
                    {
                        var sourcePos = _originalDragSource.TranslatePoint(
                            new Avalonia.Point(0, 0),
                            topLevel
                        );
                        _dragStartPosition = sourcePos ?? e.GetPosition(topLevel);
                    }
                    else
                    {
                        _dragStartPosition = e.GetPosition(this);
                    }

                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"🎯 MANUAL DRAG START for item: {item.Name}"
                    );

                    // Play card select sound
                    // SoundEffectService.Instance.PlayCardSelect();

                    // COMPLETELY hide the original card during drag (not just opacity)
                    // This prevents CardDragBehavior from interfering with ghost movement
                    item.IsBeingDragged = true;

                    // CREATE GHOST IMAGE
                    CreateDragAdorner(item, _dragStartPosition);

                    // Show ALL drop zone overlays when dragging from center (zones don't expand, just overlays appear)
                    ShowDropZoneOverlays();

                    // Don't capture pointer - we're already handling PointerMoved on the UserControl itself
                    // Capturing to sender (the small image) would prevent us from getting events outside its bounds
                }
                else
                {
                    DebugLogger.Log("VisualBuilderTab", "No item found for drag operation");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Drag operation failed: {ex.Message}");
                RemoveDragAdorner();
                _isDragging = false;
            }
        }

        private void OnDropZoneItemPointerPressed(
            object? sender,
            Avalonia.Input.PointerPressedEventArgs e
        )
        {
            // Prevent starting a new drag if one is already in progress or animating
            if (_isDragging || _isAnimating)
                return;

            try
            {
                var grid = sender as Grid;
                var item = grid?.DataContext as Models.FilterItem;

                if (item == null)
                {
                    DebugLogger.Log("VisualBuilderTab", "No item found for drop zone drag");
                    return;
                }

                // Check which button was pressed
                var pointerPoint = e.GetCurrentPoint(grid);

                // RIGHT-CLICK: Open config popup
                if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    ShowItemConfigPopup(item, grid);
                    e.Handled = true;
                    return;
                }

                // LEFT-CLICK ONLY: Start drag operation
                if (!pointerPoint.Properties.IsLeftButtonPressed)
                {
                    return; // Not left click, ignore
                }

                var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm == null)
                    return;

                // Figure out which drop zone this item is in by checking which collection contains it
                // NOTE: We do NOT remove the item here! This allows:
                // 1. Dragging duplicates (same item with different config to same zone)
                // 2. Cancelling drag with rubber-band animation
                // We only remove when successfully dropped to a DIFFERENT zone
                if (vm.SelectedMust.Contains(item))
                {
                    _sourceDropZone = "MustDropZone";
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Drag initiated from Must zone: {item.Name}"
                    );
                }
                else if (vm.SelectedShould.Contains(item))
                {
                    _sourceDropZone = "ShouldDropZone";
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Drag initiated from Should zone: {item.Name}"
                    );
                }
                else if (vm.SelectedMustNot.Contains(item))
                {
                    _sourceDropZone = "MustNotDropZone";
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Drag initiated from MustNot zone: {item.Name}"
                    );
                }

                // Now start the drag operation
                _draggedItem = item;
                _isDragging = true;
                _originalDragSource = grid;

                // Get position relative to TopLevel
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null && _originalDragSource != null)
                {
                    var sourcePos = _originalDragSource.TranslatePoint(
                        new Avalonia.Point(0, 0),
                        topLevel
                    );
                    _dragStartPosition = sourcePos ?? e.GetPosition(topLevel);
                }
                else
                {
                    _dragStartPosition = e.GetPosition(this);
                }

                // Play sound - disabled (NAudio removed)
                // SoundEffectService.Instance.PlayCardSelect();

                // Create ghost
                CreateDragAdorner(item, _dragStartPosition);

                // Show "Return to shelf" overlay + OTHER drop zones when dragging from drop zones (zones don't expand, just overlays)
                if (_sourceDropZone != null)
                {
                    var returnOverlay = this.FindControl<Border>("ReturnOverlay");
                    if (returnOverlay != null)
                    {
                        returnOverlay.IsVisible = true;
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            "✅ Showing return overlay immediately on drag start"
                        );
                    }

                    // Show overlays for OTHER drop zones (not the source)
                    ShowDropZoneOverlays(_sourceDropZone);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Drop zone drag failed: {ex.Message}");
                RemoveDragAdorner();
                _isDragging = false;
            }
        }

        private void OnPointerMovedManualDrag(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _draggedItem == null)
                return;

            try
            {
                // Update adorner position using TranslateTransform (allows CardDragBehavior sway to work)
                if (_adornerTransform != null && _topLevel != null)
                {
                    var position = e.GetPosition(_topLevel);
                    _adornerTransform.X = position.X - 35;
                    _adornerTransform.Y = position.Y - 47;

                    // Log less frequently to avoid spam
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100 < 16) // ~10fps logging
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Ghost moved to ({position.X}, {position.Y})"
                        );
                    }
                }

                // Check if we're over a drop zone and provide visual feedback
                var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
                var returnOverlay = this.FindControl<Border>("ReturnOverlay");
                var dropZoneContainer = this.FindControl<Grid>("DropZoneContainer");

                var mustOverlay = this.FindControl<Border>("MustDropOverlay");
                var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");
                var mustNotOverlay = this.FindControl<Border>("MustNotDropOverlay");

                if (_topLevel == null)
                    return;
                var cursorPos = e.GetPosition(_topLevel);

                // Check if over item grid (return to shelf)
                if (IsPointOverControl(cursorPos, itemGridBorder, _topLevel))
                {
                    // Show overlay if dragging FROM drop zones (sourceDropZone != null)
                    if (returnOverlay != null && _sourceDropZone != null)
                    {
                        returnOverlay.IsVisible = true;
                    }

                    // Hide ALL drop zone overlays when over return area
                    // BUT only if we're dragging FROM a drop zone (not from shelf)
                    if (_sourceDropZone != null)
                    {
                        if (mustOverlay != null) mustOverlay.IsVisible = false;
                        if (shouldOverlay != null) shouldOverlay.IsVisible = false;
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = false;
                    }
                }
                // Check if over drop zone container - determine which third
                else if (dropZoneContainer != null && IsPointOverControl(cursorPos, dropZoneContainer, _topLevel))
                {
                    // Get position within the drop zone container
                    var localPos = e.GetPosition(dropZoneContainer);
                    var containerHeight = dropZoneContainer.Bounds.Height;

                    // Divide into thirds
                    var thirdHeight = containerHeight / 3.0;

                    // Hide return overlay
                    if (returnOverlay != null) returnOverlay.IsVisible = false;

                    if (localPos.Y < thirdHeight)
                    {
                        // Top third - MUST - Show ONLY this overlay, hide others
                        if (mustOverlay != null) mustOverlay.IsVisible = true;
                        if (shouldOverlay != null) shouldOverlay.IsVisible = false;
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = false;

                        // Expand MUST zone, collapse others (accordion style during drag)
                        if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
                        {
                            vm.IsMustExpanded = true;
                            vm.IsShouldExpanded = false;
                            vm.IsCantExpanded = false;
                        }
                    }
                    else if (localPos.Y < thirdHeight * 2)
                    {
                        // Middle third - SHOULD - Show ONLY this overlay, hide others
                        if (mustOverlay != null) mustOverlay.IsVisible = false;
                        if (shouldOverlay != null) shouldOverlay.IsVisible = true;
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = false;

                        // Expand SHOULD zone, collapse others (accordion style during drag)
                        if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
                        {
                            vm.IsMustExpanded = false;
                            vm.IsShouldExpanded = true;
                            vm.IsCantExpanded = false;
                        }
                    }
                    else
                    {
                        // Bottom third - CAN'T - Show ONLY this overlay, hide others
                        if (mustOverlay != null) mustOverlay.IsVisible = false;
                        if (shouldOverlay != null) shouldOverlay.IsVisible = false;
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = true;

                        // Expand MUST-NOT zone, collapse others (accordion style during drag)
                        if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
                        {
                            vm.IsMustExpanded = false;
                            vm.IsShouldExpanded = false;
                            vm.IsCantExpanded = true;
                        }
                    }
                }
                else
                {
                    // Not over any drop zone - show ALL overlays again (back to initial drag state)
                    // Exclude source zone if dragging from a drop zone
                    if (_sourceDropZone == null)
                    {
                        // Dragging from shelf - show all drop zone overlays
                        if (mustOverlay != null) mustOverlay.IsVisible = true;
                        if (shouldOverlay != null) shouldOverlay.IsVisible = true;
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = true;
                    }
                    else
                    {
                        // Dragging from a drop zone - show all overlays EXCEPT source
                        if (mustOverlay != null) mustOverlay.IsVisible = (_sourceDropZone != "MustDropZone");
                        if (shouldOverlay != null) shouldOverlay.IsVisible = (_sourceDropZone != "ShouldDropZone");
                        if (mustNotOverlay != null) mustNotOverlay.IsVisible = (_sourceDropZone != "MustNotDropZone");
                    }
                    if (returnOverlay != null) returnOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    $"Error in OnPointerMovedManualDrag: {ex.Message}"
                );
            }
        }

        private bool IsPointOverControl(Avalonia.Point point, Control? control, TopLevel topLevel)
        {
            if (control == null)
                return false;

            try
            {
                // Transform point from TopLevel coordinates to control coordinates
                var controlPos = control.TranslatePoint(new Avalonia.Point(0, 0), topLevel);
                if (!controlPos.HasValue)
                    return false;

                var bounds = new Avalonia.Rect(
                    controlPos.Value,
                    new Avalonia.Size(control.Bounds.Width, control.Bounds.Height)
                );
                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Find if the cursor is over a FilterOperatorControl within a drop zone.
        /// Returns the FilterOperatorItem if found, otherwise null.
        /// </summary>
        private Models.FilterOperatorItem? FindOperatorAtPosition(
            Avalonia.Point cursorPos,
            string zoneName,
            ViewModels.FilterTabs.VisualBuilderTabViewModel vm
        )
        {
            if (_topLevel == null)
                return null;

            // Get the appropriate collection based on zone
            System.Collections.ObjectModel.ObservableCollection<Models.FilterItem>? collection =
                zoneName switch
                {
                    "MustDropZone" => vm.SelectedMust,
                    "ShouldDropZone" => vm.SelectedShould,
                    "MustNotDropZone" => vm.SelectedMustNot,
                    _ => null,
                };

            if (collection == null)
                return null;

            // Find all FilterOperatorControls in the visual tree
            var operators = collection.OfType<Models.FilterOperatorItem>();

            foreach (var operatorItem in operators)
            {
                // Find all FilterOperatorControl visuals
                var operatorControls = this.GetVisualDescendants()
                    .OfType<Components.FilterOperatorControl>()
                    .Where(c => c.DataContext == operatorItem);

                foreach (var operatorControl in operatorControls)
                {
                    // Check if cursor is over this operator control
                    if (IsPointOverControl(cursorPos, operatorControl, _topLevel))
                    {
                        return operatorItem;
                    }
                }
            }

            return null;
        }

        private void RemoveDragOverClassExcept(Border? exceptZone)
        {
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");
            var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
            var returnOverlay = this.FindControl<Border>("ReturnOverlay");

            // Keep overlay visible when dragging from drop zones
            // If dragging from shelf (_sourceDropZone == null), hide it normally
            if (returnOverlay != null && exceptZone != itemGridBorder && _sourceDropZone == null)
            {
                returnOverlay.IsVisible = false;
            }

            if (mustZone != exceptZone)
                mustZone?.Classes.Remove("drag-over");
            if (shouldZone != exceptZone)
                shouldZone?.Classes.Remove("drag-over");
            if (mustNotZone != exceptZone)
                mustNotZone?.Classes.Remove("drag-over");
            if (itemGridBorder != exceptZone)
                itemGridBorder?.Classes.Remove("drag-over");
        }

        private async void OnPointerReleasedManualDrag(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _draggedItem == null)
                return;

            try
            {
                DebugLogger.Log("VisualBuilderTab", "Manual drag operation released");

                // Remove all visual feedback
                RemoveDragOverClassExcept(null);

                var vm =
                    DataContext
                    as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm == null)
                {
                    DebugLogger.Log("VisualBuilderTab", "Drop failed - no ViewModel");
                    return;
                }

                // Get cursor position and find which zone we dropped on
                if (_topLevel == null)
                {
                    DebugLogger.Log("VisualBuilderTab", "Drop failed - no TopLevel");
                    return;
                }

                var cursorPos = e.GetPosition(_topLevel);

                var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
                var returnOverlay = this.FindControl<Border>("ReturnOverlay");
                var dropZoneContainer = this.FindControl<Grid>("DropZoneContainer");

                Border? targetZone = null;
                string? zoneName = null;

                // Check if over the item grid first (return to shelf)
                if (IsPointOverControl(cursorPos, itemGridBorder, _topLevel))
                {
                    targetZone = itemGridBorder;
                    zoneName = "ItemGridBorder";
                }
                // Check if over drop zone container - determine which third
                else if (dropZoneContainer != null && IsPointOverControl(cursorPos, dropZoneContainer, _topLevel))
                {
                    // Get position within the drop zone container
                    var localPos = e.GetPosition(dropZoneContainer);
                    var containerHeight = dropZoneContainer.Bounds.Height;

                    // Divide into thirds
                    var thirdHeight = containerHeight / 3.0;

                    if (localPos.Y < thirdHeight)
                    {
                        // Top third - MUST
                        zoneName = "MustDropZone";
                        targetZone = this.FindControl<Border>("MustDropZone");
                    }
                    else if (localPos.Y < thirdHeight * 2)
                    {
                        // Middle third - SHOULD
                        zoneName = "ShouldDropZone";
                        targetZone = this.FindControl<Border>("ShouldDropZone");
                    }
                    else
                    {
                        // Bottom third - CAN'T
                        zoneName = "MustNotDropZone";
                        targetZone = this.FindControl<Border>("MustNotDropZone");
                    }

                    DebugLogger.Log("VisualBuilderTab", $"Drop zone detection: Y={localPos.Y:F1}, Height={containerHeight:F1}, Third={thirdHeight:F1}, Zone={zoneName}");
                }

                if (targetZone != null && zoneName != null)
                {
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"✅ Dropping {_draggedItem.Name} into {zoneName}"
                    );

                    // Hide overlay after drop
                    if (returnOverlay != null)
                    {
                        returnOverlay.IsVisible = false;
                    }

                    // SPECIAL CASE: ItemGridBorder (return to shelf) - remove from drop zone if dragging from one
                    if (zoneName == "ItemGridBorder")
                    {
                        if (_sourceDropZone != null)
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"↩️ RETURNING {_draggedItem.Name} from {_sourceDropZone} to shelf"
                            );
                            switch (_sourceDropZone)
                            {
                                case "MustDropZone":
                                    vm.SelectedMust.Remove(_draggedItem);
                                    vm.CollapseToZone("MUST");
                                    break;
                                case "ShouldDropZone":
                                    vm.SelectedShould.Remove(_draggedItem);
                                    vm.CollapseToZone("SHOULD");
                                    break;
                                case "MustNotDropZone":
                                    vm.SelectedMustNot.Remove(_draggedItem);
                                    vm.CollapseToZone("CANT");
                                    break;
                            }
                            // Play trash sound (or card drop)
                            // SoundEffectService.Instance.PlayCardDrop();
                        }
                        else
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                "Can't trash item from shelf (it was never added)"
                            );
                        }
                    }
                    // Check if we're dropping to the same zone we dragged from
                    else if (_sourceDropZone != null && zoneName == _sourceDropZone)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Dropped to same zone - canceling (item stays where it was)"
                        );
                        // Item stays in original zone, collapse to that zone
                        switch (zoneName)
                        {
                            case "MustDropZone":
                                vm.CollapseToZone("MUST");
                                break;
                            case "ShouldDropZone":
                                vm.CollapseToZone("SHOULD");
                                break;
                            case "MustNotDropZone":
                                vm.CollapseToZone("CANT");
                                break;
                        }
                    }
                    else
                    {
                        // If dragging from a drop zone, remove from source first
                        if (_sourceDropZone != null)
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"Removing {_draggedItem.Name} from {_sourceDropZone}"
                            );
                            switch (_sourceDropZone)
                            {
                                case "MustDropZone":
                                    vm.SelectedMust.Remove(_draggedItem);
                                    break;
                                case "ShouldDropZone":
                                    vm.SelectedShould.Remove(_draggedItem);
                                    break;
                                case "MustNotDropZone":
                                    vm.SelectedMustNot.Remove(_draggedItem);
                                    break;
                            }
                        }

                        // Play card drop sound
                        // SoundEffectService.Instance.PlayCardDrop();

                        // Check if dropping onto an operator (nested drop zone)
                        var targetOperator = FindOperatorAtPosition(cursorPos, zoneName, vm);

                        if (targetOperator != null && _draggedItem is not Models.FilterOperatorItem)
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"📦 Adding {_draggedItem.Name} to {targetOperator.OperatorType} operator"
                            );
                            // Add to operator's children instead of main zone
                            targetOperator.Children.Add(_draggedItem);
                        }
                        else if (_draggedItem is Models.FilterOperatorItem)
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"➕ Adding {_draggedItem.DisplayName} operator to {zoneName}"
                            );
                            // Add operator to zone (operators can't go inside operators)
                            switch (zoneName)
                            {
                                case "MustDropZone":
                                    vm.AddToMustCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("MUST");
                                    break;
                                case "ShouldDropZone":
                                    vm.AddToShouldCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("SHOULD");
                                    break;
                                case "MustNotDropZone":
                                    vm.AddToMustNotCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("CANT");
                                    break;
                            }
                        }
                        else
                        {
                            // Add to target zone (allows duplicates from shelf!)
                            switch (zoneName)
                            {
                                case "MustDropZone":
                                    vm.AddToMustCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("MUST");
                                    break;
                                case "ShouldDropZone":
                                    vm.AddToShouldCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("SHOULD");
                                    break;
                                case "MustNotDropZone":
                                    vm.AddToMustNotCommand.Execute(_draggedItem);
                                    vm.CollapseToZone("CANT");
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    DebugLogger.Log("VisualBuilderTab", "Drop cancelled - not over any drop zone");

                    // IMPORTANT: Stop dragging BEFORE animation to prevent glitchy cursor following!
                    _isDragging = false;

                    // If this item came from a drop zone, animate back (it never left the collection)
                    if (_sourceDropZone != null)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Drag from {_sourceDropZone} cancelled - item stays in original zone"
                        );

                        // BALATRO-STYLE: Animate back to show cancellation
                        await AnimateGhostBackToOrigin();

                        // Item was never removed, so no need to add it back!
                    }
                    else
                    {
                        // Item came from shelf - just animate back
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            "Returning to shelf with rubber band animation"
                        );
                        await AnimateGhostBackToOrigin();
                    }
                }
            }
            finally
            {
                // Always cleanup
                RemoveDragAdorner();
                if (_draggedItem != null)
                {
                    _draggedItem.IsBeingDragged = false;
                }
                _isDragging = false;
                _draggedItem = null;
                _originalDragSource = null;
                _sourceDropZone = null; // Clear the source zone
            }
        }

        private async Task AnimateGhostBackToOrigin()
        {
            if (_dragAdorner == null || _topLevel == null)
                return;

            _isAnimating = true; // Prevent new drags during animation

            try
            {
                if (_adornerTransform == null)
                    return;

                // Get current position
                var startX = _adornerTransform.X;
                var startY = _adornerTransform.Y;

                // Target is the original position
                var targetX = _dragStartPosition.X - 35;
                var targetY = _dragStartPosition.Y - 47;

                // Animate over 200ms with easing
                var duration = 200; // milliseconds
                var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                while (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime < duration)
                {
                    var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime;
                    var progress = (double)elapsed / duration;

                    // Ease out quad for smooth deceleration
                    var eased = 1 - (1 - progress) * (1 - progress);

                    var currentX = startX + (targetX - startX) * eased;
                    var currentY = startY + (targetY - startY) * eased;

                    _adornerTransform.X = currentX;
                    _adornerTransform.Y = currentY;

                    await Task.Delay(16); // ~60fps
                }

                // Ensure final position is exact
                _adornerTransform.X = targetX;
                _adornerTransform.Y = targetY;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    $"Error animating ghost back: {ex.Message}"
                );
            }
            finally
            {
                _isAnimating = false; // Allow new drags after animation completes
            }
        }

        // REMOVED: Double-click handler - right-click is the only way to configure items

        private void ShowItemConfigPopup(Models.FilterItem item, Control? sourceControl)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null)
                return;

            var config = vm.ItemConfigs.TryGetValue(item.ItemKey, out var existingConfig)
                ? existingConfig
                : new Models.ItemConfig { ItemKey = item.ItemKey, ItemType = item.ItemType };
            var popupViewModel = new ViewModels.ItemConfigPopupViewModel(config);
            popupViewModel.ItemName = item.DisplayName;
            popupViewModel.ItemImage = item.ItemImage;

            // Create the View
            var configPopup = new Controls.ItemConfigPopup { DataContext = popupViewModel };

            var popup = new Avalonia.Controls.Primitives.Popup
            {
                Child = configPopup,
                Placement = Avalonia.Controls.PlacementMode.Pointer,
                PlacementTarget = sourceControl,
                IsLightDismissEnabled = true,
            };

            popupViewModel.ConfigApplied += (appliedConfig) =>
            {
                vm.UpdateItemConfig(item.ItemKey, appliedConfig);
                popup.IsOpen = false;
            };

            popupViewModel.Cancelled += () =>
            {
                popup.IsOpen = false;
            };

            popupViewModel.DeleteRequested += () =>
            {
                vm.RemoveItem(item);
                popup.IsOpen = false;
            };

            popup.IsOpen = true;
        }

        // No pagination - categories are now directly clickable in left nav

        private void OnCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category)
            {
                // WORKAROUND FOR AVALONIA ISSUE #15593 - Populate from code-behind
                try
                {
                    var vm =
                        DataContext
                        as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                    if (vm != null)
                    {
                        // Disable reactive updates temporarily
                        vm.SetCategory(category);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(
                        "VisualBuilderTab",
                        $"Category click failed: {ex.Message}"
                    );
                }
            }
        }

        private void OnClearSearch(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm != null)
            {
                vm.SearchFilter = "";
            }
        }

        private void OnClearAll(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm != null)
            {
                vm.SelectedMust.Clear();
                vm.SelectedShould.Clear();
                vm.SelectedMustNot.Clear();
            }
        }

        private void OnFavoritesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm != null)
            {
                // Show favorites by setting category to "Joker" and then filtering for favorites
                vm.SetCategory("Joker");
                vm.SelectedCategory = "Favorites";
                DebugLogger.Log("VisualBuilderTab", "Showing favorites");
            }
        }

        private async void OnStartOverClick(
            object? sender,
            Avalonia.Interactivity.RoutedEventArgs e
        )
        {
            // Get Balatro color resources from App.axaml
            var darkBg =
                Application.Current?.FindResource("DarkBackground")
                as Avalonia.Media.SolidColorBrush;
            var modalGrey =
                Application.Current?.FindResource("ModalGrey") as Avalonia.Media.SolidColorBrush;
            var red = Application.Current?.FindResource("Red") as Avalonia.Media.SolidColorBrush;
            var white =
                Application.Current?.FindResource("White") as Avalonia.Media.SolidColorBrush;

            // Show confirmation dialog with Balatro-style colors
            var dialog = new Window
            {
                Width = 400,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background =
                    darkBg
                    ?? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(45, 54, 59)),
                Title = "Start Over?",
                TransparencyLevelHint = new[] { WindowTransparencyLevel.None },
                SystemDecorations = SystemDecorations.Full,
            };

            var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 20 };

            var label = new TextBlock
            {
                Text = "Clear everything and start over with a fresh filter?\nAre you sure?",
                FontSize = 16,
                Foreground = white,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 15,
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 120,
                Height = 45,
                FontSize = 16,
                Background = modalGrey,
                Foreground = white,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            var confirmButton = new Button
            {
                Content = "YES, START OVER",
                Width = 180,
                Height = 45,
                FontSize = 16,
                Background = red,
                Foreground = white,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };

            cancelButton.Click += (s, ev) => dialog.Close();
            confirmButton.Click += (s, ev) =>
            {
                var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm != null)
                {
                    // Clear all drop zones
                    vm.SelectedMust.Clear();
                    vm.SelectedShould.Clear();
                    vm.SelectedMustNot.Clear();

                    // Reset search filter
                    vm.SearchFilter = "";

                    // Reset to first category (Joker)
                    vm.SetCategory("Joker");
                }
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(confirmButton);

            panel.Children.Add(label);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            var owner = Avalonia.Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateDragAdorner(Models.FilterItem item, Avalonia.Point startPosition)
        {
            DebugLogger.Log("VisualBuilderTab", $"Creating drag adorner for item: {item?.Name}");

            try
            {
                // Find adorner layer - walk up visual tree to find Window/TopLevel
                _topLevel = TopLevel.GetTopLevel(this);

                if (_topLevel == null)
                {
                    // Fallback: manually walk up the visual tree
                    Visual? current = this.GetVisualParent();
                    while (current != null && _topLevel == null)
                    {
                        if (current is TopLevel tl)
                        {
                            _topLevel = tl;
                            break;
                        }
                        current = current.GetVisualParent();
                    }
                }

                // Find the AdornerLayer - it's a child of the Panel in MainWindow
                if (_topLevel != null)
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"🔍 ADORNER SEARCH [{timestamp}] TopLevel type: {_topLevel.GetType().Name}"
                    );

                    // First try standard GetAdornerLayer
                    _adornerLayer = AdornerLayer.GetAdornerLayer(_topLevel);
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"GetAdornerLayer result: {_adornerLayer != null}"
                    );

                    if (_adornerLayer == null && _topLevel is Window window)
                    {
                        DebugLogger.Log(
                            "VisualBuilderTab",
                            $"Window.Content type: {window.Content?.GetType().Name ?? "null"}"
                        );

                        if (window.Content is Panel panel)
                        {
                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"Panel has {panel.Children.Count} children"
                            );

                            // Fallback: Find AdornerLayer in the Panel's children
                            foreach (var child in panel.Children)
                            {
                                DebugLogger.Log(
                                    "VisualBuilderTab",
                                    $"Panel child: {child.GetType().Name}"
                                );

                                if (child is AdornerLayer layer)
                                {
                                    _adornerLayer = layer;
                                    DebugLogger.Log(
                                        "VisualBuilderTab",
                                        "✅ Found AdornerLayer in MainWindow Panel"
                                    );
                                    break;
                                }
                            }
                        }
                    }
                }

                if (_adornerLayer == null)
                {
                    DebugLogger.LogError(
                        "VisualBuilderTab",
                        $"Failed to find adorner layer! TopLevel: {_topLevel != null}"
                    );
                    return;
                }

                // Create ghost image - 80% opacity with subtle sway like Balatro
                // For legendary jokers, layer the soul face on top
                var imageGrid = new Grid { Width = 71, Height = 95 };

                // Main card image
                imageGrid.Children.Add(
                    new Image
                    {
                        Source = item!.ItemImage!, // Non-null: every FilterItem must have an image
                        Width = 71,
                        Height = 95,
                        Stretch = Stretch.Uniform,
                        Opacity = 0.8, // BALATRO-STYLE 80% OPACITY
                    }
                );

                // Soul face overlay for legendary jokers
                if (item.SoulFaceImage != null)
                {
                    imageGrid.Children.Add(
                        new Image
                        {
                            Source = item.SoulFaceImage,
                            Width = 71,
                            Height = 95,
                            Stretch = Stretch.Uniform,
                            Opacity = 1.0,
                        }
                    );
                }

                // Create card content that will have physics applied
                var cardContent = new Border
                {
                    Background = Brushes.Transparent,
                    Child = imageGrid,
                };

                // ADD BALATRO-STYLE SWAY PHYSICS TO THE CARD!
                // Apply the CardDragBehavior for tilt, sway, and juice effects
                var dragBehavior = new Behaviors.CardDragBehavior
                {
                    IsEnabled = true,
                    JuiceAmount = 0.4, // Balatro default juice on pickup
                };
                Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(cardContent).Add(dragBehavior);

                _dragAdorner = new Border
                {
                    Background = Brushes.Transparent,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            cardContent, // Card with physics
                            new TextBlock
                            {
                                Text = item.DisplayName,
                                Foreground = Brushes.White,
                                FontSize = 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 4, 0, 0),
                                Opacity = 1,
                            },
                        },
                    },
                };

                // Create TranslateTransform for positioning (allows CardDragBehavior sway to work)
                _adornerTransform = new TranslateTransform
                {
                    X = startPosition.X,
                    Y = startPosition.Y,
                };
                _dragAdorner.RenderTransform = _adornerTransform;
                _dragAdorner.HorizontalAlignment = HorizontalAlignment.Left;
                _dragAdorner.VerticalAlignment = VerticalAlignment.Top;

                // Don't use SetAdornedElement - it causes relative positioning issues
                _adornerLayer.Children.Add(_dragAdorner);

                // TRIGGER THE SWAY ANIMATION manually by simulating a pointer press on the card
                // Since the ghost won't receive real pointer events, we fake them
                if (_topLevel != null)
                {
                    var fakePointerArgs = new PointerPressedEventArgs(
                        cardContent,
                        new Avalonia.Input.Pointer(0, Avalonia.Input.PointerType.Mouse, true),
                        _topLevel,
                        new Avalonia.Point(35, 47), // Center of the card
                        (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        new PointerPointProperties(
                            RawInputModifiers.None,
                            PointerUpdateKind.LeftButtonPressed
                        ),
                        KeyModifiers.None
                    );

                    cardContent.RaiseEvent(fakePointerArgs);
                }

                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Ghost image created at ({startPosition.X}, {startPosition.Y}) with sway animation triggered"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    $"Failed to create drag adorner: {ex.Message}"
                );
            }
        }

        private void RemoveDragAdorner()
        {
            try
            {
                if (_dragAdorner != null && _adornerLayer != null)
                {
                    _adornerLayer.Children.Remove(_dragAdorner);

                    // Properly dispose visual elements to prevent memory leak
                    if (_dragAdorner.Child is StackPanel stack)
                    {
                        // Clear bindings and references
                        foreach (var child in stack.Children)
                        {
                            if (child is Image img)
                            {
                                img.Source = null;
                            }
                        }
                        stack.Children.Clear();
                    }

                    _dragAdorner = null;
                    _adornerLayer = null;
                    _topLevel = null;
                    DebugLogger.Log("VisualBuilderTab", "Ghost image removed and disposed");
                }

                // Hide all drop zone overlays when drag ends
                HideAllDropZoneOverlays();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    $"Failed to remove drag adorner: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Show drop zone overlays. If excludeZone is specified, that zone won't show its overlay.
        /// Also shows Favorites overlay. Shows ALL drop zone overlays when drag starts.
        /// </summary>
        private void ShowDropZoneOverlays(string? excludeZone = null)
        {
            // Show dramatic backdrop that dims everything
            var backdrop = this.FindControl<Border>("DragBackdrop");
            if (backdrop != null)
                backdrop.IsVisible = true;

            // Show ALL drop zone overlays (excluding the source zone if specified)
            if (excludeZone != "MustDropZone")
            {
                var mustOverlay = this.FindControl<Border>("MustDropOverlay");
                if (mustOverlay != null)
                    mustOverlay.IsVisible = true;
            }

            if (excludeZone != "ShouldDropZone")
            {
                var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");
                if (shouldOverlay != null)
                    shouldOverlay.IsVisible = true;
            }

            if (excludeZone != "MustNotDropZone")
            {
                var mustNotOverlay = this.FindControl<Border>("MustNotDropOverlay");
                if (mustNotOverlay != null)
                    mustNotOverlay.IsVisible = true;
            }

            // Always show Favorites overlay during drag
            var favoritesOverlay = this.FindControl<Border>("FavoritesDropOverlay");
            if (favoritesOverlay != null)
                favoritesOverlay.IsVisible = true;
        }

        /// <summary>
        /// Hide all drop zone overlays including Favorites
        /// </summary>
        private void HideAllDropZoneOverlays()
        {
            // Hide dramatic backdrop
            var backdrop = this.FindControl<Border>("DragBackdrop");
            if (backdrop != null)
                backdrop.IsVisible = false;

            var mustOverlay = this.FindControl<Border>("MustDropOverlay");
            if (mustOverlay != null)
                mustOverlay.IsVisible = false;

            var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");
            if (shouldOverlay != null)
                shouldOverlay.IsVisible = false;

            var mustNotOverlay = this.FindControl<Border>("MustNotDropOverlay");
            if (mustNotOverlay != null)
                mustNotOverlay.IsVisible = false;

            var returnOverlay = this.FindControl<Border>("ReturnOverlay");
            if (returnOverlay != null)
                returnOverlay.IsVisible = false;

            var favoritesOverlay = this.FindControl<Border>("FavoritesDropOverlay");
            if (favoritesOverlay != null)
                favoritesOverlay.IsVisible = false;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Detach global pointer handlers
            if (_topLevel != null)
            {
                _topLevel.PointerMoved -= OnPointerMovedManualDrag;
                _topLevel.PointerReleased -= OnPointerReleasedManualDrag;
            }

            // Ensure cleanup on navigation away
            RemoveDragAdorner();
            base.OnDetachedFromVisualTree(e);
        }

        /// <summary>
        /// Handle MUST zone label click - expand MUST, collapse others
        /// </summary>
        private void OnMustLabelClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.ExpandMustCommand.Execute(null);
                DebugLogger.Log("VisualBuilderTab", "MUST zone expanded via label click");
            }
        }

        /// <summary>
        /// Handle SHOULD zone label click - expand SHOULD, collapse others
        /// </summary>
        private void OnShouldLabelClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.ExpandShouldCommand.Execute(null);
                DebugLogger.Log("VisualBuilderTab", "SHOULD zone expanded via label click");
            }
        }

        /// <summary>
        /// Handle CAN'T zone label click - expand CAN'T, collapse others
        /// </summary>
        private void OnCantLabelClick(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.ExpandCantCommand.Execute(null);
                DebugLogger.Log("VisualBuilderTab", "CAN'T zone expanded via label click");
            }
        }
    }
}
