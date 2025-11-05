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
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// Configure Filter Tab - Visual builder without SHOULD zone
    /// Reuses VisualBuilderTabViewModel but hides SHOULD UI
    /// </summary>
    public partial class ConfigureFilterTab : UserControl
    {
        private Models.FilterItem? _draggedItem;
        private Border? _dragAdorner;
        private TranslateTransform? _adornerTransform;
        private AdornerLayer? _adornerLayer;
        private TopLevel? _topLevel;
        private bool _isDragging = false;
        private bool _isAnimating = false;
        private bool _isDraggingTray = false;
        private Avalonia.Point _dragStartPosition;
        private Control? _originalDragSource;
        private string? _sourceDropZone; // Track which drop zone the item came from (MustDropZone, MustNotDropZone, or null for shelf)

        public ConfigureFilterTab()
        {
            InitializeComponent();

            // Only set DataContext if not already set by parent (e.g., from FiltersModalViewModel)
            // This allows both tab instances to share the same VisualBuilderTabViewModel
            if (DataContext == null)
            {
                DataContext =
                    ServiceHelper.GetRequiredService<ViewModels.FilterTabs.VisualBuilderTabViewModel>();
            }

            // Setup drop zones AFTER the control is attached to visual tree
            this.AttachedToVisualTree += (s, e) => SetupDropZones();
        }

        private void SetupDropZones()
        {
            // Find drop zones - only MUST and MUST NOT (no SHOULD!)
            var mustZone = this.FindControl<Border>("MustDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");

            DebugLogger.Log(
                "ConfigureFilterTab",
                $"Drop zones found - Must: {mustZone != null}, MustNot: {mustNotZone != null}"
            );

            // Setup operator tray drop zones
            SetupOperatorTray();

            // Attach pointer handlers to TopLevel so we get events even when pointer moves outside the control
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.PointerMoved += OnPointerMovedManualDrag;
                topLevel.PointerReleased += OnPointerReleasedManualDrag;
                DebugLogger.Log(
                    "ConfigureFilterTab",
                    "Manual drag pointer handlers attached to TopLevel"
                );
            }
            else
            {
                DebugLogger.LogError(
                    "ConfigureFilterTab",
                    "Failed to attach pointer handlers - no TopLevel"
                );
            }
        }

        private void SetupOperatorTray()
        {
            var trayOrBorder = this.FindControl<Border>("TrayOrOperator");
            var trayAndBorder = this.FindControl<Border>("TrayAndOperator");

            if (trayOrBorder != null)
            {
                trayOrBorder.AddHandler(DragDrop.DragOverEvent, OnTrayOrDragOver);
                trayOrBorder.AddHandler(DragDrop.DragLeaveEvent, OnTrayOrDragLeave);
                trayOrBorder.AddHandler(DragDrop.DropEvent, OnTrayOrDrop);
                DebugLogger.Log("ConfigureFilterTab", "OR Tray drag/drop handlers attached");
            }

            if (trayAndBorder != null)
            {
                trayAndBorder.AddHandler(DragDrop.DragOverEvent, OnTrayAndDragOver);
                trayAndBorder.AddHandler(DragDrop.DragLeaveEvent, OnTrayAndDragLeave);
                trayAndBorder.AddHandler(DragDrop.DropEvent, OnTrayAndDrop);
                DebugLogger.Log("ConfigureFilterTab", "AND Tray drag/drop handlers attached");
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

                    // RIGHT-CLICK on shelf items: Do nothing (no config for shelf items)
                    if (pointerPoint.Properties.IsRightButtonPressed)
                    {
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
                        "ConfigureFilterTab",
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

                    // Phase 2: Transition to DragActive state
                    var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                    vm?.EnterDragActiveState();

                    // Don't capture pointer - we're already handling PointerMoved on the UserControl itself
                    // Capturing to sender (the small image) would prevent us from getting events outside its bounds
                }
                else
                {
                    DebugLogger.Log("ConfigureFilterTab", "No item found for drag operation");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureFilterTab", $"Drag operation failed: {ex.Message}");
                RemoveDragAdorner();
                _isDragging = false;

                // Phase 2: Return to Default state on error
                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                vm?.EnterDefaultState();
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
                // The sender is the Border (not Grid), so get its DataContext
                var control = sender as Control;
                var item = control?.DataContext as Models.FilterItem;

                if (item == null)
                {
                    DebugLogger.Log("ConfigureFilterTab", $"No item found for drop zone drag - sender type: {sender?.GetType().Name}");
                    return;
                }

                // Check which button was pressed
                var pointerPoint = e.GetCurrentPoint(control);

                DebugLogger.Log(
                    "ConfigureFilterTab",
                    $"Drop zone item pressed - Left: {pointerPoint.Properties.IsLeftButtonPressed}, Right: {pointerPoint.Properties.IsRightButtonPressed}, Middle: {pointerPoint.Properties.IsMiddleButtonPressed}"
                );

                // RIGHT-CLICK: Open config popup
                if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    DebugLogger.Log("ConfigureFilterTab", $"Opening config popup for {item.Name}");
                    ShowItemConfigPopup(item, control);
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
                // NOTE: No SHOULD zone in this tab!
                if (vm.SelectedMust.Contains(item))
                {
                    _sourceDropZone = "MustDropZone";
                    DebugLogger.Log(
                        "ConfigureFilterTab",
                        $"Drag initiated from Must zone: {item.Name}"
                    );
                }
                else if (vm.SelectedMustNot.Contains(item))
                {
                    _sourceDropZone = "MustNotDropZone";
                    DebugLogger.Log(
                        "ConfigureFilterTab",
                        $"Drag initiated from MustNot zone: {item.Name}"
                    );
                }

                // Now start the drag operation
                _draggedItem = item;
                _isDragging = true;
                _originalDragSource = control;

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
                            "ConfigureFilterTab",
                            "✅ Showing return overlay immediately on drag start"
                        );
                    }

                    // Show overlays for OTHER drop zones (not the source)
                    ShowDropZoneOverlays(_sourceDropZone);

                    // Phase 2: Transition to DragActive state
                    vm?.EnterDragActiveState();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureFilterTab", $"Drop zone drag failed: {ex.Message}");
                RemoveDragAdorner();
                _isDragging = false;

                // Phase 2: Return to Default state on error
                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                vm?.EnterDefaultState();
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
                    _adornerTransform.X = position.X - 24;
                    _adornerTransform.Y = position.Y - 32;

                    // Log less frequently to avoid spam
                    if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100 < 16) // ~10fps logging
                    {
                        // Ghost position tracking (spam removed)
                    }
                }

                // Provide visual feedback for operator tray borders (highlight when hovering)
                var trayOrBorder = this.FindControl<Border>("TrayOrOperator");
                var trayAndBorder = this.FindControl<Border>("TrayAndOperator");

                if (_topLevel == null)
                    return;
                var cursorPos = e.GetPosition(_topLevel);

                // Check if over operator tray (only allow non-operators to be dropped)
                if (_draggedItem != null && _draggedItem is not FilterOperatorItem)
                {
                    if (IsPointOverControl(cursorPos, trayOrBorder, _topLevel))
                    {
                        // Highlight OR tray
                        if (trayOrBorder != null)
                            trayOrBorder.BorderThickness = new Avalonia.Thickness(3);
                        if (trayAndBorder != null)
                            trayAndBorder.BorderThickness = new Avalonia.Thickness(2);
                    }
                    else if (IsPointOverControl(cursorPos, trayAndBorder, _topLevel))
                    {
                        // Highlight AND tray
                        if (trayAndBorder != null)
                            trayAndBorder.BorderThickness = new Avalonia.Thickness(3);
                        if (trayOrBorder != null)
                            trayOrBorder.BorderThickness = new Avalonia.Thickness(2);
                    }
                    else
                    {
                        // Reset tray borders
                        if (trayOrBorder != null)
                            trayOrBorder.BorderThickness = new Avalonia.Thickness(2);
                        if (trayAndBorder != null)
                            trayAndBorder.BorderThickness = new Avalonia.Thickness(2);
                    }
                }

                // NOTE: Drop zone overlays remain visible throughout the entire drag operation
                // They are shown in OnItemPointerPressed/OnDropZoneItemPointerPressed
                // and hidden only in RemoveDragAdorner when the drag completes
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ConfigureFilterTab",
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

            // Get the appropriate collection based on zone (NO SHOULD!)
            System.Collections.ObjectModel.ObservableCollection<Models.FilterItem>? collection =
                zoneName switch
                {
                    "MustDropZone" => vm.SelectedMust,
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


        private async void OnPointerReleasedManualDrag(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _draggedItem == null)
                return;

            try
            {
                DebugLogger.Log("ConfigureFilterTab", "Manual drag operation released");

                var vm =
                    DataContext
                    as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm == null)
                {
                    DebugLogger.Log("ConfigureFilterTab", "Drop failed - no ViewModel");
                    return;
                }

                // Get cursor position and find which zone we dropped on
                if (_topLevel == null)
                {
                    DebugLogger.Log("ConfigureFilterTab", "Drop failed - no TopLevel");
                    return;
                }

                var cursorPos = e.GetPosition(_topLevel);

                var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
                var dropZoneContainer = this.FindControl<Grid>("DropZoneContainer");
                var trayOrBorder = this.FindControl<Border>("TrayOrOperator");
                var trayAndBorder = this.FindControl<Border>("TrayAndOperator");

                Border? targetZone = null;
                string? zoneName = null;

                // Check if dropped on Favorites (CategoryNav)
                var categoryNav = this.FindControl<StackPanel>("CategoryNav");
                if (_draggedItem is not FilterOperatorItem && IsPointOverControl(cursorPos, categoryNav, _topLevel))
                {
                    // Add to favorites
                    DebugLogger.Log("ConfigureFilterTab", $"Dropped {_draggedItem.Name} into Favorites");

                    var favoritesService = ServiceHelper.GetService<Services.FavoritesService>();
                    if (favoritesService != null)
                    {
                        favoritesService.AddFavoriteItem(_draggedItem.Name);

                        // Update the item's IsFavorite flag
                        _draggedItem.IsFavorite = true;

                        // Refresh the view to show it in Favorites category
                        if (vm.SelectedMainCategory == "Favorites")
                        {
                            // TODO: Add public method to refresh view or make RebuildGroupedItems public
                            // For now, switching categories will show the new favorite
                        }

                        DebugLogger.Log("ConfigureFilterTab", $"✅ {_draggedItem.Name} added to favorites");
                    }

                    return; // Early exit - handled
                }

                // Check if dropped on operator tray (only for non-operators)
                if (_draggedItem is not FilterOperatorItem)
                {
                    if (IsPointOverControl(cursorPos, trayOrBorder, _topLevel))
                    {
                        // Drop into OR tray
                        DebugLogger.Log("ConfigureFilterTab", $"Dropped {_draggedItem.Name} into OR tray");

                        var itemCopy = new Models.FilterItem
                        {
                            Name = _draggedItem.Name,
                            Type = _draggedItem.Type,
                            Category = _draggedItem.Category,
                            DisplayName = _draggedItem.DisplayName,
                            ItemImage = _draggedItem.ItemImage,
                        };

                        vm.TrayOrOperator.Children.Add(itemCopy);

                        // Reset tray border
                        if (trayOrBorder != null)
                            trayOrBorder.BorderThickness = new Avalonia.Thickness(2);

                        return; // Early exit - handled
                    }
                    else if (IsPointOverControl(cursorPos, trayAndBorder, _topLevel))
                    {
                        // Drop into AND tray
                        DebugLogger.Log("ConfigureFilterTab", $"Dropped {_draggedItem.Name} into AND tray");

                        var itemCopy = new Models.FilterItem
                        {
                            Name = _draggedItem.Name,
                            Type = _draggedItem.Type,
                            Category = _draggedItem.Category,
                            DisplayName = _draggedItem.DisplayName,
                            ItemImage = _draggedItem.ItemImage,
                        };

                        vm.TrayAndOperator.Children.Add(itemCopy);

                        // Reset tray border
                        if (trayAndBorder != null)
                            trayAndBorder.BorderThickness = new Avalonia.Thickness(2);

                        return; // Early exit - handled
                    }
                }

                // Check if over the item grid first (return to shelf)
                if (IsPointOverControl(cursorPos, itemGridBorder, _topLevel))
                {
                    targetZone = itemGridBorder;
                    zoneName = "ItemGridBorder";
                }
                // Check if over drop zone container - determine which half (NO SHOULD!)
                else if (dropZoneContainer != null && IsPointOverControl(cursorPos, dropZoneContainer, _topLevel))
                {
                    // Get position within the drop zone container
                    var localPos = e.GetPosition(dropZoneContainer);
                    var containerHeight = dropZoneContainer.Bounds.Height;

                    // Divide into halves
                    var halfHeight = containerHeight / 2.0;

                    if (localPos.Y < halfHeight)
                    {
                        // Top half - MUST
                        zoneName = "MustDropZone";
                        targetZone = this.FindControl<Border>("MustDropZone");
                    }
                    else
                    {
                        // Bottom half - BANNED
                        zoneName = "MustNotDropZone";
                        targetZone = this.FindControl<Border>("MustNotDropZone");
                    }

                    DebugLogger.Log("ConfigureFilterTab", $"Drop zone detection: Y={localPos.Y:F1}, Height={containerHeight:F1}, Half={halfHeight:F1}, Zone={zoneName}");
                }

                if (targetZone != null && zoneName != null)
                {
                    DebugLogger.Log(
                        "ConfigureFilterTab",
                        $"✅ Dropping {_draggedItem.Name} into {zoneName}"
                    );

                    // SPECIAL CASE: ItemGridBorder (return to shelf) - remove from drop zone if dragging from one
                    if (zoneName == "ItemGridBorder")
                    {
                        if (_sourceDropZone != null)
                        {
                            DebugLogger.Log(
                                "ConfigureFilterTab",
                                $"↩️ RETURNING {_draggedItem.Name} from {_sourceDropZone} to shelf"
                            );
                            switch (_sourceDropZone)
                            {
                                case "MustDropZone":
                                    vm.SelectedMust.Remove(_draggedItem);
                                    vm.IsDragging = false;
                                    break;
                                case "MustNotDropZone":
                                    vm.SelectedMustNot.Remove(_draggedItem);
                                    vm.IsDragging = false;
                                    break;
                            }
                            // Play trash sound (or card drop)
                            // SoundEffectService.Instance.PlayCardDrop();
                        }
                        else
                        {
                            DebugLogger.Log(
                                "ConfigureFilterTab",
                                "Can't trash item from shelf (it was never added)"
                            );
                        }
                    }
                    // Check if we're dropping to the same zone we dragged from
                    else if (_sourceDropZone != null && zoneName == _sourceDropZone)
                    {
                        DebugLogger.Log(
                            "ConfigureFilterTab",
                            $"Dropped to same zone - canceling (item stays where it was)"
                        );
                        // Item stays in original zone, collapse to that zone
                        switch (zoneName)
                        {
                            case "MustDropZone":
                                vm.IsDragging = false;
                                break;
                            case "MustNotDropZone":
                                vm.IsDragging = false;
                                break;
                        }
                    }
                    else
                    {
                        // If dragging from a drop zone, remove from source first
                        if (_sourceDropZone != null)
                        {
                            DebugLogger.Log(
                                "ConfigureFilterTab",
                                $"Removing {_draggedItem.Name} from {_sourceDropZone}"
                            );
                            switch (_sourceDropZone)
                            {
                                case "MustDropZone":
                                    vm.SelectedMust.Remove(_draggedItem);
                                    break;
                                case "MustNotDropZone":
                                    vm.SelectedMustNot.Remove(_draggedItem);
                                    break;
                            }
                        }

                        // Play card drop sound
                        // SoundEffectService.Instance.PlayCardDrop();

                        // Check if dropping onto an operator (nested drop zone)
                        // DISABLED: Operators in drop zones are READ-ONLY
                        // Users must drag operator back to top shelf to edit it
                        var targetOperator = FindOperatorAtPosition(cursorPos, zoneName, vm);

                        if (targetOperator != null && _draggedItem is not Models.FilterOperatorItem)
                        {
                            // IMPORTANT: Only allow drops into TOP SHELF tray operators (TrayOrOperator, TrayAndOperator)
                            // Operators already in drop zones are READ-ONLY to prevent accidental "disappearing" items
                            bool isTrayOperator = (targetOperator == vm.TrayOrOperator || targetOperator == vm.TrayAndOperator);

                            if (isTrayOperator)
                            {
                                DebugLogger.Log(
                                    "ConfigureFilterTab",
                                    $"📦 Adding {_draggedItem.Name} to {targetOperator.OperatorType} tray operator"
                                );
                                // Add to operator's children (top shelf staging area only)
                                targetOperator.Children.Add(_draggedItem);
                            }
                            else
                            {
                                // Operator is in a drop zone (MUST/MUSTNOT) - treat as regular drop
                                DebugLogger.Log(
                                    "ConfigureFilterTab",
                                    $"⚠️ Operator in drop zone is READ-ONLY - dropping {_draggedItem.Name} next to it instead"
                                );
                                // Fall through to regular zone add logic below
                                targetOperator = null; // Treat as if no operator was hit
                            }
                        }

                        // Only process if we didn't add to tray operator above
                        if (targetOperator == null)
                        {
                            if (_draggedItem is Models.FilterOperatorItem operatorItem)
                            {
                                DebugLogger.Log(
                                    "ConfigureFilterTab",
                                    $"➕ Adding {_draggedItem.DisplayName} operator to {zoneName}"
                                );

                                // Check if this is one of the tray operators
                                bool isTrayOperator = (operatorItem == vm.TrayOrOperator || operatorItem == vm.TrayAndOperator);

                                // If it's a tray operator, create a COPY with its children
                                Models.FilterItem itemToAdd = _draggedItem;
                                if (isTrayOperator && operatorItem.Children.Count > 0)
                                {
                                    // Create a copy of the operator with deep copied children
                                    var operatorCopy = new Models.FilterOperatorItem(operatorItem.OperatorType)
                                    {
                                        DisplayName = operatorItem.DisplayName,
                                        Name = operatorItem.Name,
                                        Type = operatorItem.Type,
                                        Category = operatorItem.Category,
                                    };

                                    // Deep copy all children to avoid binding issues
                                    foreach (var child in operatorItem.Children.ToList())
                                    {
                                        var childCopy = new Models.FilterItem
                                        {
                                            Name = child.Name,
                                            Type = child.Type,
                                            Category = child.Category,
                                            DisplayName = child.DisplayName,
                                            ItemKey = child.ItemKey,
                                            ItemImage = child.ItemImage,
                                            IsFavorite = child.IsFavorite,
                                            Status = child.Status,
                                            // Copy configuration properties
                                            Value = child.Value,
                                            Label = child.Label,
                                            Antes = child.Antes,
                                            Edition = child.Edition,
                                            IncludeBoosterPacks = child.IncludeBoosterPacks,
                                            IncludeShopStream = child.IncludeShopStream,
                                            IncludeSkipTags = child.IncludeSkipTags,
                                            // Copy playing card properties
                                            Rank = child.Rank,
                                            Suit = child.Suit,
                                            Enhancement = child.Enhancement,
                                            Seal = child.Seal
                                        };
                                        operatorCopy.Children.Add(childCopy);
                                    }

                                    itemToAdd = operatorCopy;
                                    DebugLogger.Log("ConfigureFilterTab", $"Created operator copy with {operatorCopy.Children.Count} deep-copied children");
                                }

                                // Add operator to zone (operators can't go inside operators)
                                switch (zoneName)
                                {
                                    case "MustDropZone":
                                        vm.AddToMustCommand.Execute(itemToAdd);
                                        vm.IsDragging = false;
                                        break;
                                    case "MustNotDropZone":
                                        vm.AddToMustNotCommand.Execute(itemToAdd);
                                        vm.IsDragging = false;
                                        break;
                                }

                                // Clear the tray operator's children after copying them
                                if (isTrayOperator)
                                {
                                    operatorItem.Children.Clear();
                                    DebugLogger.Log(
                                        "ConfigureFilterTab",
                                        $"Cleared tray operator {operatorItem.OperatorType} children after copying"
                                    );
                                }
                            }
                            else
                            {
                                // Add to target zone (allows duplicates from shelf!)
                                switch (zoneName)
                                {
                                    case "MustDropZone":
                                        vm.AddToMustCommand.Execute(_draggedItem);
                                        vm.IsDragging = false;
                                        break;
                                    case "MustNotDropZone":
                                        vm.AddToMustNotCommand.Execute(_draggedItem);
                                        vm.IsDragging = false;
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    DebugLogger.Log("ConfigureFilterTab", "Drop cancelled - not over any drop zone");

                    // IMPORTANT: Stop dragging BEFORE animation to prevent glitchy cursor following!
                    _isDragging = false;

                    // If this item came from a drop zone, animate back (it never left the collection)
                    if (_sourceDropZone != null)
                    {
                        DebugLogger.Log(
                            "ConfigureFilterTab",
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
                            "ConfigureFilterTab",
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
                _isDraggingTray = false; // Re-enable drop acceptance on trays
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
                    "ConfigureFilterTab",
                    $"Error animating ghost back: {ex.Message}"
                );
            }
            finally
            {
                _isAnimating = false; // Allow new drags after animation completes
            }
        }

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
                // REMOVED: OverlayInputPassThroughElement was causing ALL clicks to pass through popup!
                // This made the popup completely non-interactive - buttons/radiobuttons didn't work
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
                        "ConfigureFilterTab",
                        $"Category click failed: {ex.Message}"
                    );
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreateDragAdorner(Models.FilterItem item, Avalonia.Point startPosition)
        {
            DebugLogger.Log("ConfigureFilterTab", $"Creating drag adorner for item: {item?.Name}");

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
                        "ConfigureFilterTab",
                        $"🔍 ADORNER SEARCH [{timestamp}] TopLevel type: {_topLevel.GetType().Name}"
                    );

                    // First try standard GetAdornerLayer
                    _adornerLayer = AdornerLayer.GetAdornerLayer(_topLevel);
                    DebugLogger.Log(
                        "ConfigureFilterTab",
                        $"GetAdornerLayer result: {_adornerLayer != null}"
                    );

                    if (_adornerLayer == null && _topLevel is Window window)
                    {
                        DebugLogger.Log(
                            "ConfigureFilterTab",
                            $"Window.Content type: {window.Content?.GetType().Name ?? "null"}"
                        );

                        if (window.Content is Panel panel)
                        {
                            DebugLogger.Log(
                                "ConfigureFilterTab",
                                $"Panel has {panel.Children.Count} children"
                            );

                            // Fallback: Find AdornerLayer in the Panel's children
                            foreach (var child in panel.Children)
                            {
                                DebugLogger.Log(
                                    "ConfigureFilterTab",
                                    $"Panel child: {child.GetType().Name}"
                                );

                                if (child is AdornerLayer layer)
                                {
                                    _adornerLayer = layer;
                                    DebugLogger.Log(
                                        "ConfigureFilterTab",
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
                        "ConfigureFilterTab",
                        $"Failed to find adorner layer! TopLevel: {_topLevel != null}"
                    );
                    return;
                }

                // Create ghost image - different for operators vs regular items
                Control cardContent;

                if (item is Models.FilterOperatorItem operatorItem)
                {
                    // Special visual for operators - show a compact box with children count
                    var blue = Application.Current?.FindResource("Blue") as IBrush ?? Brushes.DodgerBlue;
                    var green = Application.Current?.FindResource("Green") as IBrush ?? Brushes.LimeGreen;
                    var darkBg = Application.Current?.FindResource("DarkBackground") as IBrush ?? new SolidColorBrush(Color.FromRgb(45, 54, 59));
                    var white = Application.Current?.FindResource("White") as IBrush ?? Brushes.White;
                    var balatroFont = Application.Current?.FindResource("BalatroFont") as Avalonia.Media.FontFamily ?? Avalonia.Media.FontFamily.Default;

                    var operatorBorder = new Border
                    {
                        Background = darkBg,
                        BorderBrush = operatorItem.OperatorType == "OR" ? blue : green,
                        BorderThickness = new Avalonia.Thickness(2),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Avalonia.Thickness(8),
                        Width = 100,
                        Child = new StackPanel
                        {
                            Spacing = 4,
                            Children =
                            {
                                new Border
                                {
                                    Background = operatorItem.OperatorType == "OR" ? blue : green,
                                    CornerRadius = new CornerRadius(4),
                                    Padding = new Avalonia.Thickness(6, 3),
                                    Child = new TextBlock
                                    {
                                        Text = operatorItem.OperatorType,
                                        FontFamily = balatroFont,
                                        FontSize = 14,
                                        FontWeight = FontWeight.Bold,
                                        Foreground = white,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                    }
                                },
                                new TextBlock
                                {
                                    Text = $"{operatorItem.Children.Count} items",
                                    FontFamily = balatroFont,
                                    FontSize = 11,
                                    Foreground = white,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Opacity = 0.7,
                                }
                            }
                        },
                        Opacity = 0.9,
                    };

                    cardContent = operatorBorder;
                }
                else
                {
                    // Regular item - show card image
                    var imageGrid = new Grid { Width = 48, Height = 64 };

                    // Main card image - ensure item and ItemImage are not null
                    if (item?.ItemImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.ItemImage,
                                Width = 48,
                                Height = 64,
                                Stretch = Stretch.Uniform,
                                Opacity = 0.8, // BALATRO-STYLE 80% OPACITY
                            }
                        );
                    }
                    else
                    {
                        // Fallback for missing image - show placeholder
                        DebugLogger.LogError("ConfigureFilterTab", $"Item {item?.Name ?? "unknown"} has no image!");
                    }

                    // Soul face overlay for legendary jokers
                    if (item?.SoulFaceImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.SoulFaceImage,
                                Width = 48,
                                Height = 64,
                                Stretch = Stretch.Uniform,
                                Opacity = 1.0,
                            }
                        );
                    }

                    cardContent = new Border
                    {
                        Background = Brushes.Transparent,
                        Child = imageGrid,
                    };
                }

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
                                Text = item?.DisplayName ?? "Unknown Item",
                                Foreground = Brushes.White,
                                FontSize = 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 4, 0, 0),
                                Opacity = 1,
                            },
                        },
                    },
                };

                // Position the drag adorner (CardDragBehavior handles rotation/tilt)
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
                    "ConfigureFilterTab",
                    $"Ghost image created at ({startPosition.X}, {startPosition.Y}) with sway animation triggered"
                );
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ConfigureFilterTab",
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
                    DebugLogger.Log("ConfigureFilterTab", "Ghost image removed and disposed");
                }

                // Hide all drop zone overlays when drag ends
                HideAllDropZoneOverlays();

                // Phase 2: Return to Default state when drag ends
                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                vm?.EnterDefaultState();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "ConfigureFilterTab",
                    $"Failed to remove drag adorner: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Show drop zone overlays. If excludeZone is specified, that zone won't show its overlay.
        /// Shows ALL valid drop targets when drag starts so user knows where they can drop.
        /// </summary>
        private void ShowDropZoneOverlays(string? excludeZone = null)
        {
            // Show dramatic backdrop that dims everything
            var backdrop = this.FindControl<Border>("DragBackdrop");
            if (backdrop != null)
                backdrop.IsVisible = true;

            // Show ALL drop zone overlays so user can see where to drop (NO SHOULD!)
            var mustOverlay = this.FindControl<Border>("MustDropOverlay");
            var mustNotOverlay = this.FindControl<Border>("MustNotDropOverlay");

            // Show all overlays except the source zone (if dragging from a zone)
            if (mustOverlay != null)
                mustOverlay.IsVisible = (excludeZone != "MustDropZone");
            if (mustNotOverlay != null)
                mustNotOverlay.IsVisible = (excludeZone != "MustNotDropZone");

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

        #region Operator Tray Handlers

        /// <summary>
        /// Handle clicking on OR operator in tray - starts dragging the populated operator
        /// </summary>
        private void OnTrayOrPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isDragging || _isAnimating)
                return;

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || vm.TrayOrOperator.Children.Count == 0)
                return;

            var pointerPoint = e.GetCurrentPoint(sender as Control);
            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

            // Start dragging the OR operator with its children
            _draggedItem = vm.TrayOrOperator;
            _isDragging = true;
            _isDraggingTray = true; // Disable drop acceptance on trays while dragging
            _originalDragSource = sender as Control;
            _sourceDropZone = null; // From tray, not a drop zone

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null && _originalDragSource != null)
            {
                var sourcePos = _originalDragSource.TranslatePoint(new Avalonia.Point(0, 0), topLevel);
                _dragStartPosition = sourcePos ?? e.GetPosition(topLevel);
            }
            else
            {
                _dragStartPosition = e.GetPosition(this);
            }

            DebugLogger.Log("ConfigureFilterTab", $"Started dragging OR operator with {vm.TrayOrOperator.Children.Count} children");

            CreateDragAdorner(_draggedItem, _dragStartPosition);
            ShowDropZoneOverlays();

            e.Handled = true;
        }

        /// <summary>
        /// Handle clicking on AND operator in tray - starts dragging the populated operator
        /// </summary>
        private void OnTrayAndPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isDragging || _isAnimating)
                return;

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || vm.TrayAndOperator.Children.Count == 0)
                return;

            var pointerPoint = e.GetCurrentPoint(sender as Control);
            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

            // Start dragging the AND operator with its children
            _draggedItem = vm.TrayAndOperator;
            _isDragging = true;
            _isDraggingTray = true; // Disable drop acceptance on trays while dragging
            _originalDragSource = sender as Control;
            _sourceDropZone = null;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null && _originalDragSource != null)
            {
                var sourcePos = _originalDragSource.TranslatePoint(new Avalonia.Point(0, 0), topLevel);
                _dragStartPosition = sourcePos ?? e.GetPosition(topLevel);
            }
            else
            {
                _dragStartPosition = e.GetPosition(this);
            }

            DebugLogger.Log("ConfigureFilterTab", $"Started dragging AND operator with {vm.TrayAndOperator.Children.Count} children");

            CreateDragAdorner(_draggedItem, _dragStartPosition);
            ShowDropZoneOverlays();

            e.Handled = true;
        }

        /// <summary>
        /// Handle drag over OR tray - allow dropping items INTO the tray
        /// </summary>
        private void OnTrayOrDragOver(object? sender, DragEventArgs e)
        {
            // Keep drop zone overlays visible - hiding them creates erratic flickering

            // DISABLE drop acceptance if we're currently dragging a tray (prevents confusing "aim while editing" UX)
            if (_isDraggingTray)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Allow regular items OR operators being moved back from drop zones
            if (_draggedItem != null)
            {
                bool canDrop = (_draggedItem is not FilterOperatorItem) ||
                               (_draggedItem is FilterOperatorItem && !string.IsNullOrEmpty(_sourceDropZone));

                if (canDrop)
                {
                    e.DragEffects = DragDropEffects.Copy;

                    // ONLY highlight if dragging FROM a drop zone (editing an existing operator)
                    // Do NOT highlight when dragging from shelf (building new operator)
                    if (sender is Border border && !string.IsNullOrEmpty(_sourceDropZone))
                    {
                        border.BorderThickness = new Avalonia.Thickness(3);
                    }
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handle drag leave from OR tray - reset visual state
        /// </summary>
        private void OnTrayOrDragLeave(object? sender, DragEventArgs e)
        {
            // Overlays stay visible during drag - no need to restore

            // Reset border thickness
            if (sender is Border border)
            {
                border.BorderThickness = new Avalonia.Thickness(2);
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handle dropping an item INTO the OR tray
        /// </summary>
        private void OnTrayOrDrop(object? sender, DragEventArgs e)
        {
            // Reset visual feedback
            if (sender is Border border)
            {
                border.BorderThickness = new Avalonia.Thickness(2);
            }

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || _draggedItem == null)
                return;

            // Only allow regular FilterItems (not operators)
            if (_draggedItem is not FilterOperatorItem)
            {
                DebugLogger.Log("ConfigureFilterTab", $"Adding {_draggedItem.Name} to OR tray");

                // Add a COPY of the item to the tray (so users can add same item multiple times)
                var itemCopy = new Models.FilterItem
                {
                    Name = _draggedItem.Name,
                    Type = _draggedItem.Type,
                    Category = _draggedItem.Category,
                    DisplayName = _draggedItem.DisplayName,
                    ItemImage = _draggedItem.ItemImage,
                };

                vm.TrayOrOperator.Children.Add(itemCopy);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle drag over AND tray - allow dropping items INTO the tray
        /// </summary>
        private void OnTrayAndDragOver(object? sender, DragEventArgs e)
        {
            // Keep drop zone overlays visible - hiding them creates erratic flickering

            // DISABLE drop acceptance if we're currently dragging a tray (prevents confusing "aim while editing" UX)
            if (_isDraggingTray)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // Allow regular items OR operators being moved back from drop zones
            if (_draggedItem != null)
            {
                bool canDrop = (_draggedItem is not FilterOperatorItem) ||
                               (_draggedItem is FilterOperatorItem && !string.IsNullOrEmpty(_sourceDropZone));

                if (canDrop)
                {
                    e.DragEffects = DragDropEffects.Copy;

                    // ONLY highlight if dragging FROM a drop zone (editing an existing operator)
                    // Do NOT highlight when dragging from shelf (building new operator)
                    if (sender is Border border && !string.IsNullOrEmpty(_sourceDropZone))
                    {
                        border.BorderThickness = new Avalonia.Thickness(3);
                    }
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handle drag leave from AND tray - reset visual state
        /// </summary>
        private void OnTrayAndDragLeave(object? sender, DragEventArgs e)
        {
            // Overlays stay visible during drag - no need to restore

            // Reset border thickness
            if (sender is Border border)
            {
                border.BorderThickness = new Avalonia.Thickness(2);
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handle dropping an item INTO the AND tray
        /// </summary>
        private void OnTrayAndDrop(object? sender, DragEventArgs e)
        {
            // Reset visual feedback
            if (sender is Border border)
            {
                border.BorderThickness = new Avalonia.Thickness(2);
            }

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || _draggedItem == null)
                return;

            // Only allow regular FilterItems (not operators)
            if (_draggedItem is not FilterOperatorItem)
            {
                DebugLogger.Log("ConfigureFilterTab", $"Adding {_draggedItem.Name} to AND tray");

                // Add a COPY of the item to the tray (so users can add same item multiple times)
                var itemCopy = new Models.FilterItem
                {
                    Name = _draggedItem.Name,
                    Type = _draggedItem.Type,
                    Category = _draggedItem.Category,
                    DisplayName = _draggedItem.DisplayName,
                    ItemImage = _draggedItem.ItemImage,
                };

                vm.TrayAndOperator.Children.Add(itemCopy);
                e.Handled = true;
            }
        }

        #endregion
    }
}
