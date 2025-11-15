using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    /// PROPER MVVM Visual Builder Tab - zero business logic, pure view
    /// ViewModel set via XAML binding, not code-behind
    /// </summary>
    public partial class VisualBuilderTab : UserControl
    {
        private Models.FilterItem? _draggedItem;
        private Border? _dragAdorner;
        private TranslateTransform? _adornerTransform; // Transform for positioning the ghost (allows sway to work)
        private RotateTransform? _adornerLeanTransform; // Transform for velocity-based lean effect
        private AdornerLayer? _adornerLayer;
        private TopLevel? _topLevel;
        private bool _isDragging = false;
        private bool _isAnimating = false; // Track if rubber-band animation is playing
        private bool _isDraggingTray = false; // Track if we're dragging an operator tray (disables drop acceptance on trays)
        private Avalonia.Point _dragStartPosition;
        private Avalonia.Point _dragOffset; // Offset from card origin to click point (maintains grab position)
        private Avalonia.Point _previousMousePosition; // Track previous frame position for velocity calculation
        private Control? _originalDragSource; // Store the original control to animate back to
        private string? _sourceDropZone; // Track which drop zone the item came from (MustDropZone, ShouldDropZone, MustNotDropZone, or null for shelf)

        // Simple LERP system for smooth drag adornment positioning
        private Avalonia.Point _adornerTargetPosition; // Target position (where mouse is)
        private Avalonia.Threading.DispatcherTimer? _springUpdateTimer;

        public VisualBuilderTab()
        {
            InitializeComponent();

            // Initialize 60fps spring physics timer
            _springUpdateTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // 60fps
            };
            _springUpdateTimer.Tick += OnSpringUpdateTick;

            // Set DataContext to the VisualBuilderTabViewModel
            DataContext =
                ServiceHelper.GetRequiredService<ViewModels.FilterTabs.VisualBuilderTabViewModel>();

            DebugLogger.Log(
                "VisualBuilderTab",
                $"DataContext set to: {DataContext?.GetType().Name ?? "null"}"
            );

            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"ViewModel collections - Must: {vm.SelectedMust.Count}, Should: {vm.SelectedShould.Count}"
                );

                // CRITICAL-001 FIX: Use named methods for event handlers so they can be unsubscribed
                vm.SelectedMust.CollectionChanged += OnMustCollectionChanged;
                vm.SelectedShould.CollectionChanged += OnShouldCollectionChanged;
            }

            // Setup drop zones AFTER the control is attached to visual tree
            this.AttachedToVisualTree += (s, e) => SetupDropZones();

            // Subscribe to ViewModel PropertyChanged
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
                {
                    vm.PropertyChanged += OnViewModelPropertyChanged;
                    // Arrow position is now handled via data binding in XAML
                }
            };
        }

        private void SetupDropZones()
        {
            // Find drop zones - we'll check them manually via hit testing
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Drop zones found - Must: {mustZone != null}, Should: {shouldZone != null}"
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

        private void SetupOperatorTray()
        {
            var unifiedTray = this.FindControl<Border>("UnifiedTray");

            if (unifiedTray != null)
            {
                unifiedTray.AddHandler(DragDrop.DragOverEvent, OnUnifiedTrayDragOver);
                unifiedTray.AddHandler(DragDrop.DragLeaveEvent, OnUnifiedTrayDragLeave);
                unifiedTray.AddHandler(DragDrop.DropEvent, OnUnifiedTrayDrop);
                DebugLogger.Log("VisualBuilderTab", "Unified Tray drag/drop handlers attached");
            }

            // Subscribe to UnifiedOperator.Children collection changes to update fanned layout
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.UnifiedOperator.Children.CollectionChanged += OnUnifiedTrayChildrenChanged;
                // Initial layout
                UpdateUnifiedTrayFannedLayout();
            }
        }

        private void OnUnifiedTrayChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the fanned card layout whenever children change
            UpdateUnifiedTrayFannedLayout();
        }

        private void UpdateUnifiedTrayFannedLayout()
        {
            if (DataContext is not ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
                return;

            var itemsControl = this.FindControl<ItemsControl>("UnifiedTrayItemsControl");
            if (itemsControl == null)
                return;

            // Wait for the visual tree to be fully rendered
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    CalculateUnifiedTrayFannedPositions(vm);
                },
                Avalonia.Threading.DispatcherPriority.Background
            );
        }

        private void CalculateUnifiedTrayFannedPositions(ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
        {
            var itemsControl = this.FindControl<ItemsControl>("UnifiedTrayItemsControl");
            if (itemsControl == null)
                return;

            int count = vm.UnifiedOperator.Children.Count;
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
                xOffset = 25.0;
                yArcHeight = 3.0;
            }
            else if (count <= 4)
            {
                // 3-4 cards - moderate fan
                baseAngle = -12.0;
                angleDelta = 24.0 / (count - 1);
                xOffset = 22.0;
                yArcHeight = 8.0;
            }
            else if (count <= 6)
            {
                // 5-6 cards - fuller fan
                baseAngle = -15.0;
                angleDelta = 30.0 / (count - 1);
                xOffset = 20.0;
                yArcHeight = 12.0;
            }
            else
            {
                // 7+ cards - dramatic poker hand fan
                baseAngle = -18.0;
                angleDelta = 36.0 / (count - 1);
                xOffset = 17.0;
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

                // Calculate angle for this card
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
                container.ZIndex = 100 + i;
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

                        // Store the click offset relative to the card origin (where user grabbed)
                        var clickPos = e.GetPosition(topLevel);
                        _dragOffset = new Avalonia.Point(
                            clickPos.X - _dragStartPosition.X,
                            clickPos.Y - _dragStartPosition.Y
                        );

                        // Initialize previous position for velocity tracking
                        _previousMousePosition = clickPos;
                    }
                    else
                    {
                        _dragStartPosition = e.GetPosition(this);
                        _dragOffset = new Avalonia.Point(0, 0);
                        _previousMousePosition = e.GetPosition(this);
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

                    // Start spring physics timer
                    _springUpdateTimer?.Start();

                    // Hide the original control during drag
                    if (_originalDragSource != null)
                    {
                        _originalDragSource.Opacity = 0; // Hide original while dragging
                        DebugLogger.Log("VisualBuilderTab", "Hidden original shelf card during drag");
                    }

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
                // The sender is the Border (not Grid), so get its DataContext
                var control = sender as Control;
                var item = control?.DataContext as Models.FilterItem;

                if (item == null)
                {
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"No item found for drop zone drag - sender type: {sender?.GetType().Name}"
                    );
                    return;
                }

                // Check which button was pressed
                var pointerPoint = e.GetCurrentPoint(control);

                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"Drop zone item pressed - Left: {pointerPoint.Properties.IsLeftButtonPressed}, Right: {pointerPoint.Properties.IsRightButtonPressed}, Middle: {pointerPoint.Properties.IsMiddleButtonPressed}"
                );

                // RIGHT-CLICK: Open config popup
                if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    DebugLogger.Log("VisualBuilderTab", $"Opening config popup for {item.Name}");
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

                    // Store the click offset relative to the card origin (where user grabbed)
                    var clickPos = e.GetPosition(topLevel);
                    _dragOffset = new Avalonia.Point(
                        clickPos.X - _dragStartPosition.X,
                        clickPos.Y - _dragStartPosition.Y
                    );

                    // Initialize previous position for velocity tracking
                    _previousMousePosition = clickPos;
                }
                else
                {
                    _dragStartPosition = e.GetPosition(this);
                    _dragOffset = new Avalonia.Point(0, 0);
                    _previousMousePosition = e.GetPosition(this);
                }

                // Play sound - disabled (NAudio removed)
                // SoundEffectService.Instance.PlayCardSelect();

                // Create ghost
                CreateDragAdorner(item, _dragStartPosition);

                // Start spring physics timer
                _springUpdateTimer?.Start();

                // Hide the original control during drag (make it invisible, not just transparent)
                if (_originalDragSource != null)
                {
                    _originalDragSource.Opacity = 0; // Hide original while dragging
                    DebugLogger.Log("VisualBuilderTab", "Hidden original card during drag");
                }

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

        private void OnSpringUpdateTick(object? sender, EventArgs e)
        {
            if (_dragAdorner == null || _adornerTransform == null) return;

            UpdateSpringPhysics();
        }

        private void UpdateSpringPhysics()
        {
            if (_adornerTransform == null) return;

            // Simple lerp towards target - no complex physics needed!
            const double lerpFactor = 0.3; // adjust for feel (higher = faster, snappier)

            double currentX = _adornerTransform.X;
            double currentY = _adornerTransform.Y;

            // Interpolate position towards target
            _adornerTransform.X = currentX + ((_adornerTargetPosition.X - currentX) * lerpFactor);
            _adornerTransform.Y = currentY + ((_adornerTargetPosition.Y - currentY) * lerpFactor);

            // Lean based on distance to target (simple and works!)
            if (_adornerLeanTransform != null)
            {
                double deltaX = _adornerTargetPosition.X - currentX;
                double leanAngle = -deltaX * 0.5; // Scale factor
                leanAngle = Math.Clamp(leanAngle, -15.0, 15.0);
                _adornerLeanTransform.Angle = leanAngle;
            }
        }

        private void OnPointerMovedManualDrag(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _draggedItem == null)
                return;

            try
            {
                // Update target position only - timer will handle spring physics
                if (_topLevel != null)
                {
                    var mousePosition = e.GetPosition(_topLevel);

                    // Update target position (where the mouse cursor is, minus grab offset)
                    _adornerTargetPosition = new Avalonia.Point(
                        mousePosition.X - _dragOffset.X,
                        mousePosition.Y - _dragOffset.Y
                    );

                    // Restart spring timer if it was stopped by settlement optimization
                    if (_springUpdateTimer != null && !_springUpdateTimer.IsEnabled)
                    {
                        _springUpdateTimer.Start();
                    }

                    // Update previous position for other calculations
                    _previousMousePosition = mousePosition;
                }

                // Check if we're over a drop zone and provide visual feedback
                var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
                var returnOverlay = this.FindControl<Border>("ReturnOverlay");
                var dropZoneContainer = this.FindControl<Grid>("DropZoneContainer");
                var unifiedTray = this.FindControl<Border>("UnifiedTray");

                if (_topLevel == null)
                    return;
                var cursorPos = e.GetPosition(_topLevel);

                // Check if over unified operator tray (only allow non-operators to be dropped)
                bool isOverTray = false;
                if (_draggedItem != null && _draggedItem is not FilterOperatorItem)
                {
                    if (IsPointOverControl(cursorPos, unifiedTray, _topLevel))
                    {
                        isOverTray = true;
                        // Highlight unified tray
                        if (unifiedTray != null)
                            unifiedTray.BorderThickness = new Avalonia.Thickness(3);
                    }
                    else
                    {
                        // Reset tray border
                        if (unifiedTray != null)
                            unifiedTray.BorderThickness = new Avalonia.Thickness(2);
                    }
                }

                // Keep return overlay visible the ENTIRE time when dragging FROM a drop zone
                // Only hide it when actually dropping or canceling
                if (returnOverlay != null && _sourceDropZone != null)
                {
                    returnOverlay.IsVisible = true;
                }

                // Check if over item grid (return to shelf)
                if (IsPointOverControl(cursorPos, itemGridBorder, _topLevel) && !isOverTray)
                {
                    // Drop zone overlays are handled by PointerEntered/Exited events - don't manipulate them here
                }
                // Check if over drop zone container
                else if (
                    dropZoneContainer != null
                    && IsPointOverControl(cursorPos, dropZoneContainer, _topLevel)
                )
                {
                    // Drop zone overlays are handled by PointerEntered/Exited events - don't manipulate them here
                    // Zones stay always visible - no accordion-style expansion during drag
                }
                else
                {
                    // Not over any drop zone or return area
                    // Drop zone overlays are handled by PointerEntered/Exited events - don't manipulate them here
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
            var itemGridBorder = this.FindControl<Border>("ItemGridBorder");
            var returnOverlay = this.FindControl<Border>("ReturnOverlay");

            // Keep overlay visible when dragging from drop zones
            // Only hide return overlay if dragging from shelf (not from a drop zone)
            // When dragging from a drop zone, keep it visible the entire time
            if (returnOverlay != null && _sourceDropZone == null)
            {
                // Only hide if we're not hovering over the return zone
                if (exceptZone != itemGridBorder)
                {
                    returnOverlay.IsVisible = false;
                }
            }

            if (mustZone != exceptZone)
                mustZone?.Classes.Remove("drag-over");
            if (shouldZone != exceptZone)
                shouldZone?.Classes.Remove("drag-over");
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
                var unifiedTray = this.FindControl<Border>("UnifiedTray");

                Border? targetZone = null;
                string? zoneName = null;

                // HIGH-004 FIX: Check if dropped on Favorites (CategoryNav) with null safety
                var categoryNav = this.FindControl<StackPanel>("CategoryNav");
                if (
                    _draggedItem is not FilterOperatorItem
                    && IsPointOverControl(cursorPos, categoryNav, _topLevel)
                )
                {
                    // Validate item has a name before adding to favorites
                    if (string.IsNullOrEmpty(_draggedItem?.Name))
                    {
                        DebugLogger.LogError(
                            "VisualBuilderTab",
                            "Cannot add item with null/empty name to favorites"
                        );
                        return;
                    }

                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Dropped {_draggedItem.Name} into Favorites"
                    );

                    var favoritesService = ServiceHelper.GetService<Services.FavoritesService>();
                    if (favoritesService != null)
                    {
                        try
                        {
                            favoritesService.AddFavoriteItem(_draggedItem.Name);

                            // Update the item's IsFavorite flag
                            _draggedItem.IsFavorite = true;

                            // Refresh the view to show it in Favorites category
                            if (vm?.SelectedMainCategory == "Favorites")
                            {
                                // Force refresh of Favorites category
                                vm.SetCategory("Favorites");
                            }

                            DebugLogger.Log(
                                "VisualBuilderTab",
                                $"✅ {_draggedItem.Name} added to favorites"
                            );
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(
                                "VisualBuilderTab",
                                $"Failed to add favorite: {ex.Message}"
                            );
                        }
                    }
                    else
                    {
                        DebugLogger.LogError("VisualBuilderTab", "FavoritesService not available");
                    }

                    return; // Early exit - handled
                }

                // Check if dropped on unified operator tray (only for FilterItem, NOT operators)
                if (_draggedItem is FilterItem && IsPointOverControl(cursorPos, unifiedTray, _topLevel))
                {
                    // Drop into unified tray
                    DebugLogger.Log(
                        "VisualBuilderTab",
                        $"Dropped {_draggedItem.Name} into unified tray"
                    );

                    var itemCopy = new Models.FilterItem
                    {
                        Name = _draggedItem.Name,
                        Type = _draggedItem.Type,
                        Category = _draggedItem.Category,
                        DisplayName = _draggedItem.DisplayName,
                        ItemKey = _draggedItem.ItemKey,
                        ItemImage = _draggedItem.ItemImage,
                        IsFavorite = _draggedItem.IsFavorite,
                        Status = _draggedItem.Status,
                        // Copy configuration properties
                        Value = _draggedItem.Value,
                        Label = _draggedItem.Label,
                        Antes = _draggedItem.Antes,
                        Edition = _draggedItem.Edition,
                        IncludeBoosterPacks = _draggedItem.IncludeBoosterPacks,
                        IncludeShopStream = _draggedItem.IncludeShopStream,
                        IncludeSkipTags = _draggedItem.IncludeSkipTags,
                        // Copy playing card properties
                        Rank = _draggedItem.Rank,
                        Suit = _draggedItem.Suit,
                        Enhancement = _draggedItem.Enhancement,
                        Seal = _draggedItem.Seal,
                        // Copy stickers for overlay rendering (eternal/perishable/rental)
                        Stickers = _draggedItem.Stickers != null ? new List<string>(_draggedItem.Stickers) : null,
                        // Note: SoulFaceImage, EditionImage, DebuffedOverlayImage are read-only (computed from other properties)
                        IsInBannedItemsTray = _draggedItem.IsInBannedItemsTray
                    };

                    vm.UnifiedOperator.Children.Add(itemCopy);

                    // Reset tray border
                    if (unifiedTray != null)
                        unifiedTray.BorderThickness = new Avalonia.Thickness(2);

                    return; // Early exit - handled
                }

                // Check if over the item grid first (return to shelf)
                if (IsPointOverControl(cursorPos, itemGridBorder, _topLevel))
                {
                    targetZone = itemGridBorder;
                    zoneName = "ItemGridBorder";
                }
                // Check if over drop zones - use proper hit testing instead of Y-position math
                else if (dropZoneContainer != null && IsPointOverControl(cursorPos, dropZoneContainer, _topLevel))
                {
                    var mustZone = this.FindControl<Border>("MustDropZone");
                    var shouldZone = this.FindControl<Border>("ShouldDropZone");

                    // Use direct hit testing on each drop zone (fixes bug where operator tray offset broke Y-position math)
                    if (mustZone != null && IsPointOverControl(cursorPos, mustZone, _topLevel))
                    {
                        zoneName = "MustDropZone";
                        targetZone = mustZone;
                        DebugLogger.Log("VisualBuilderTab", "✅ Over MUST drop zone (FILTER)");
                    }
                    else if (shouldZone != null && IsPointOverControl(cursorPos, shouldZone, _topLevel))
                    {
                        zoneName = "ShouldDropZone";
                        targetZone = shouldZone;
                        DebugLogger.Log("VisualBuilderTab", "✅ Over SHOULD drop zone (SCORING)");
                    }
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
                                    vm.IsDragging = false;
                                    break;
                                case "ShouldDropZone":
                                    vm.SelectedShould.Remove(_draggedItem);
                                    vm.IsDragging = false;
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
                                vm.IsDragging = false;
                                break;
                            case "ShouldDropZone":
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
                            // IMPORTANT: Only allow drops into TOP SHELF unified operator
                            // Operators already in drop zones are READ-ONLY to prevent accidental "disappearing" items
                            bool isUnifiedOperator = (targetOperator == vm.UnifiedOperator);

                            if (isUnifiedOperator)
                            {
                                DebugLogger.Log(
                                    "VisualBuilderTab",
                                    $"📦 Adding {_draggedItem.Name} to unified {targetOperator.OperatorType} operator"
                                );
                                // Add to operator's children (top shelf staging area only)
                                targetOperator.Children.Add(_draggedItem);
                            }
                            else
                            {
                                // Operator is in a drop zone (MUST/SHOULD/MUSTNOT) - treat as regular drop
                                DebugLogger.Log(
                                    "VisualBuilderTab",
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
                                    "VisualBuilderTab",
                                    $"➕ Adding {_draggedItem.DisplayName} operator to {zoneName}"
                                );

                                // VALIDATION: BannedItems can ONLY drop into MUST zone
                                if (operatorItem.OperatorType == "BannedItems" && zoneName == "ShouldDropZone")
                                {
                                    DebugLogger.Log(
                                        "VisualBuilderTab",
                                        "🚫 BLOCKED: BannedItems cannot drop into SHOULD zone!"
                                    );
                                    // Animate rubber-band back to origin
                                    await AnimateGhostBackToOrigin();
                                    return;
                                }

                                // Check if this is the unified operator
                                bool isUnifiedOperator = (operatorItem == vm.UnifiedOperator);

                                // If it's the unified operator, create a COPY with its children
                                Models.FilterItem itemToAdd = _draggedItem;
                                if (isUnifiedOperator && operatorItem.Children.Count > 0)
                                {
                                    // Create a copy of the operator with deep copied children
                                    var operatorCopy = new Models.FilterOperatorItem(
                                        operatorItem.OperatorType
                                    )
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
                                            Seal = child.Seal,
                                            // Copy stickers for overlay rendering (eternal/perishable/rental)
                                            Stickers = child.Stickers != null ? new List<string>(child.Stickers) : null,
                                        };
                                        operatorCopy.Children.Add(childCopy);
                                    }

                                    itemToAdd = operatorCopy;
                                    DebugLogger.Log(
                                        "VisualBuilderTab",
                                        $"Created operator copy with {operatorCopy.Children.Count} deep-copied children"
                                    );
                                }

                                // MERGE LOGIC: Check if BannedItems tray already exists in MUST zone
                                if (operatorItem.OperatorType == "BannedItems" && zoneName == "MustDropZone")
                                {
                                    var existingBanned = vm.SelectedMust
                                        .OfType<Models.FilterOperatorItem>()
                                        .FirstOrDefault(x => x.OperatorType == "BannedItems");

                                    if (existingBanned != null)
                                    {
                                        DebugLogger.Log(
                                            "VisualBuilderTab",
                                            $"🔀 MERGING BannedItems: Adding {operatorItem.Children.Count} items to existing BannedItems tray"
                                        );

                                        // Merge all children into existing BannedItems tray
                                        foreach (var child in operatorItem.Children.ToList())
                                        {
                                            // Set IsInBannedItemsTray flag for debuffed overlay
                                            child.IsInBannedItemsTray = true;
                                            existingBanned.Children.Add(child);
                                        }

                                        // Clear the source tray
                                        if (isUnifiedOperator)
                                        {
                                            operatorItem.Children.Clear();
                                        }

                                        vm.IsDragging = false;
                                        // Don't add the operator - we merged into existing one
                                        return; // Exit early, skip normal add logic
                                    }
                                }

                                // Set IsInBannedItemsTray flag for all children if this is a BannedItems tray
                                if (itemToAdd is Models.FilterOperatorItem bannedOp && bannedOp.OperatorType == "BannedItems")
                                {
                                    foreach (var child in bannedOp.Children)
                                    {
                                        child.IsInBannedItemsTray = true;
                                    }
                                }

                                // Add operator to zone (operators can't go inside operators)
                                switch (zoneName)
                                {
                                    case "MustDropZone":
                                        vm.AddToMustCommand.Execute(itemToAdd);
                                        vm.IsDragging = false;
                                        break;
                                    case "ShouldDropZone":
                                        vm.AddToShouldCommand.Execute(itemToAdd);
                                        vm.IsDragging = false;
                                        break;
                                }

                                // Clear the unified operator's children after copying them
                                if (isUnifiedOperator)
                                {
                                    operatorItem.Children.Clear();
                                    DebugLogger.Log(
                                        "VisualBuilderTab",
                                        $"Cleared unified operator {operatorItem.OperatorType} children after copying"
                                    );
                                }
                            }
                            else
                            {
                                // Create a COPY of the item to avoid sharing instances with the palette
                                var itemCopy = new Models.FilterItem
                                {
                                    Name = _draggedItem.Name,
                                    Type = _draggedItem.Type,
                                    Category = _draggedItem.Category,
                                    DisplayName = _draggedItem.DisplayName,
                                    ItemKey = _draggedItem.ItemKey,
                                    ItemImage = _draggedItem.ItemImage,
                                    IsFavorite = _draggedItem.IsFavorite,
                                    Status = _draggedItem.Status,
                                    // Copy configuration properties
                                    Value = _draggedItem.Value,
                                    Label = _draggedItem.Label,
                                    Antes = _draggedItem.Antes,
                                    Edition = _draggedItem.Edition,
                                    IncludeBoosterPacks = _draggedItem.IncludeBoosterPacks,
                                    IncludeShopStream = _draggedItem.IncludeShopStream,
                                    IncludeSkipTags = _draggedItem.IncludeSkipTags,
                                    // Copy playing card properties
                                    Rank = _draggedItem.Rank,
                                    Suit = _draggedItem.Suit,
                                    Enhancement = _draggedItem.Enhancement,
                                    Seal = _draggedItem.Seal,
                                    // Copy stickers for overlay rendering (eternal/perishable/rental)
                                    Stickers = _draggedItem.Stickers != null ? new List<string>(_draggedItem.Stickers) : null,
                                    // Note: SoulFaceImage, EditionImage, DebuffedOverlayImage are read-only (computed from other properties)
                                    IsInBannedItemsTray = _draggedItem.IsInBannedItemsTray
                                };

                                // Add the COPY to target zone (allows duplicates from shelf!)
                                switch (zoneName)
                                {
                                    case "MustDropZone":
                                        vm.AddToMustCommand.Execute(itemCopy);
                                        vm.IsDragging = false;
                                        break;
                                    case "ShouldDropZone":
                                        vm.AddToShouldCommand.Execute(itemCopy);
                                        vm.IsDragging = false;
                                        break;
                                }
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

                // Restore original control visibility
                if (_originalDragSource != null)
                {
                    _originalDragSource.Opacity = 1; // Show original again
                    DebugLogger.Log("VisualBuilderTab", "Restored original card visibility");
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

            // Find overlay controls
            var overlay = this.FindControl<Grid>("PopupOverlay");
            var popupContent = this.FindControl<ContentControl>("PopupContent");

            if (overlay == null || popupContent == null)
            {
                DebugLogger.LogError(
                    "VisualBuilderTab",
                    "Could not find PopupOverlay or PopupContent controls"
                );
                return;
            }

            // Set up event handlers
            popupViewModel.ConfigApplied += (appliedConfig) =>
            {
                vm.UpdateItemConfig(item.ItemKey, appliedConfig);
                overlay.IsVisible = false;
                popupContent.Content = null;
            };

            popupViewModel.Cancelled += () =>
            {
                overlay.IsVisible = false;
                popupContent.Content = null;
            };

            popupViewModel.DeleteRequested += () =>
            {
                vm.RemoveItem(item);
                overlay.IsVisible = false;
                popupContent.Content = null;
            };

            // Show the overlay with the popup
            popupContent.Content = configPopup;
            overlay.IsVisible = true;
        }

        /// <summary>
        /// Handle clicks on the overlay background (dismiss popup)
        /// </summary>
        private void OnOverlayBackgroundClick(object? sender, PointerPressedEventArgs e)
        {
            var overlay = this.FindControl<Grid>("PopupOverlay");
            var popupContent = this.FindControl<ContentControl>("PopupContent");

            if (overlay != null && popupContent != null)
            {
                overlay.IsVisible = false;
                popupContent.Content = null;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handle clicks on popup content (prevent dismissal)
        /// </summary>
        private void OnPopupContentClick(object? sender, PointerPressedEventArgs e)
        {
            // Stop propagation to prevent overlay background click
            e.Handled = true;
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
            try
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

                        // Clear unified operator tray
                        vm.UnifiedOperator.Children.Clear();

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
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Error in OnStartOverClick: {ex.Message}");
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

                // Create ghost image - different for operators vs regular items
                Control cardContent;

                if (item is Models.FilterOperatorItem operatorItem)
                {
                    // Simplified OR/AND clause adorner - just show a compact badge-style indicator
                    var blue =
                        Application.Current?.FindResource("Blue") as IBrush ?? Brushes.DodgerBlue;
                    var green =
                        Application.Current?.FindResource("Green") as IBrush ?? Brushes.LimeGreen;
                    var red =
                        Application.Current?.FindResource("Red") as IBrush ?? Brushes.Red;
                    var darkBg =
                        Application.Current?.FindResource("DarkBackground") as IBrush
                        ?? new SolidColorBrush(Color.FromRgb(45, 54, 59));
                    var white =
                        Application.Current?.FindResource("White") as IBrush ?? Brushes.White;

                    // Choose border color based on operator type
                    IBrush borderBrush = operatorItem.OperatorType switch
                    {
                        "BannedItems" => red,
                        "AND" => blue,
                        _ => green  // OR
                    };

                    // Create a simple, compact badge-style adorner
                    var operatorContent = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8
                    };

                    // Operator type label
                    var typeText = new TextBlock
                    {
                        Text = operatorItem.OperatorType == "BannedItems" ? "BAN" : operatorItem.OperatorType,
                        FontSize = 14,
                        FontWeight = FontWeight.Bold,
                        Foreground = white,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    operatorContent.Children.Add(typeText);

                    // Show count if has children
                    if (operatorItem.Children.Count > 0)
                    {
                        var countBadge = new Border
                        {
                            Background = borderBrush,
                            CornerRadius = new CornerRadius(10),
                            Padding = new Avalonia.Thickness(6, 2),
                            Child = new TextBlock
                            {
                                Text = operatorItem.Children.Count.ToString(),
                                FontSize = 12,
                                FontWeight = FontWeight.Bold,
                                Foreground = white
                            }
                        };
                        operatorContent.Children.Add(countBadge);
                    }

                    // Show first few item thumbnails if available
                    if (operatorItem.Children.Count > 0)
                    {
                        var thumbnailPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = -10 // Overlap slightly
                        };

                        // Show up to 3 mini thumbnails
                        int thumbnailCount = Math.Min(3, operatorItem.Children.Count);
                        for (int i = 0; i < thumbnailCount; i++)
                        {
                            var child = operatorItem.Children[i];
                            if (child.ItemImage != null)
                            {
                                var thumbnail = new Border
                                {
                                    Width = 28,
                                    Height = 36,
                                    CornerRadius = new CornerRadius(4),
                                    ClipToBounds = true,
                                    BorderBrush = darkBg,
                                    BorderThickness = new Avalonia.Thickness(1),
                                    Child = new Image
                                    {
                                        Source = child.ItemImage,
                                        Width = 28,
                                        Height = 36,
                                        Stretch = Avalonia.Media.Stretch.UniformToFill
                                    }
                                };
                                thumbnailPanel.Children.Add(thumbnail);
                            }
                        }

                        if (thumbnailPanel.Children.Count > 0)
                        {
                            operatorContent.Children.Add(thumbnailPanel);
                        }
                    }

                    // Wrap in a clean border
                    var operatorBorder = new Border
                    {
                        Background = darkBg,
                        BorderBrush = borderBrush,
                        BorderThickness = new Avalonia.Thickness(2),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Avalonia.Thickness(10, 6),
                        Child = operatorContent,
                        Opacity = 1.0 // Full opacity, no translucency
                    };

                    cardContent = operatorBorder;
                }
                else
                {
                    // Regular item - show card image (native 1x Balatro sprite size)
                    var imageGrid = new Grid { Width = 71, Height = 95 };

                    // HIGH-003 FIX: Always add image with fallback for null sources
                    var imageSource = item?.ItemImage;
                    if (imageSource == null)
                    {
                        DebugLogger.LogError(
                            "VisualBuilderTab",
                            $"Item {item?.Name ?? "unknown"} has no image - using transparent fallback"
                        );
                    }

                    imageGrid.Children.Add(
                        new Image
                        {
                            Source = imageSource, // Can be null - Avalonia handles gracefully
                            Width = 71,
                            Height = 95,
                            Stretch = Stretch.Uniform,
                            Opacity = imageSource != null ? 1.0 : 0.3, // NO transparency for valid images
                        }
                    );

                    // Soul face overlay for legendary jokers
                    if (item?.SoulFaceImage != null)
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

                    // Edition overlay (foil, holo, polychrome, negative)
                    if (item?.EditionImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.EditionImage,
                                Width = 71,
                                Height = 95,
                                Stretch = Stretch.Uniform,
                                Opacity = 1.0,
                            }
                        );
                    }

                    // Seal overlay (purple, gold, red, blue - for StandardCards)
                    if (!string.IsNullOrEmpty(item?.Seal) && item.Seal != "None")
                    {
                        var sealImage = Services.SpriteService.Instance.GetSealImage(item.Seal.ToLowerInvariant());
                        if (sealImage != null)
                        {
                            imageGrid.Children.Add(
                                new Image
                                {
                                    Source = sealImage,
                                    Width = 71,
                                    Height = 95,
                                    Stretch = Stretch.Uniform,
                                    Opacity = 1.0,
                                }
                            );
                        }
                    }

                    // Sticker overlays (perishable, eternal, rental - for Jokers)
                    if (item?.PerishableStickerImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.PerishableStickerImage,
                                Width = 71,
                                Height = 95,
                                Stretch = Stretch.Uniform,
                                Opacity = 1.0,
                            }
                        );
                    }

                    if (item?.EternalStickerImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.EternalStickerImage,
                                Width = 71,
                                Height = 95,
                                Stretch = Stretch.Uniform,
                                Opacity = 1.0,
                            }
                        );
                    }

                    if (item?.RentalStickerImage != null)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.RentalStickerImage,
                                Width = 71,
                                Height = 95,
                                Stretch = Stretch.Uniform,
                                Opacity = 1.0,
                            }
                        );
                    }

                    // Debuffed overlay (for BannedItems tray)
                    if (item?.DebuffedOverlayImage != null && item?.IsInBannedItemsTray == true)
                    {
                        imageGrid.Children.Add(
                            new Image
                            {
                                Source = item.DebuffedOverlayImage,
                                Width = 71,
                                Height = 95,
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
                    Child = cardContent, // Just the card, no label (cleaner drag visual)
                };

                // Initialize adorner to appear at the mouse position (accounting for drag offset)
                // The adorner should immediately snap to where it would be if following the mouse
                // Mouse position = startPosition + _dragOffset (startPosition is card pos, offset is from card to mouse)
                // Adorner position = Mouse position - _dragOffset (to keep grab point under cursor)
                // This simplifies to just startPosition, BUT we want it to snap to current mouse immediately

                // Get current mouse position if available
                var currentMousePos = _topLevel != null ?
                    _previousMousePosition :  // Use the stored mouse position from drag start
                    new Avalonia.Point(startPosition.X + _dragOffset.X, startPosition.Y + _dragOffset.Y);

                // Position adorner so the grabbed point stays under the cursor
                double initialX = currentMousePos.X - _dragOffset.X;
                double initialY = currentMousePos.Y - _dragOffset.Y;

                _adornerTargetPosition = new Avalonia.Point(initialX, initialY);

                // Create transform group for positioning AND velocity-based lean
                _adornerTransform = new TranslateTransform
                {
                    X = initialX,
                    Y = initialY,
                };
                _adornerLeanTransform = new RotateTransform
                {
                    Angle = 0.0
                };

                // Apply transforms: translate for position, rotate for velocity lean
                // Set rotation origin to center-bottom of card for natural lean
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(_adornerLeanTransform);
                transformGroup.Children.Add(_adornerTransform);

                _dragAdorner.RenderTransform = transformGroup;
                _dragAdorner.RenderTransformOrigin = new RelativePoint(0.5, 0.8, RelativeUnit.Relative); // Pivot near bottom-center
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
                // Stop spring physics timer
                _springUpdateTimer?.Stop();

                // CRITICAL: Hide adorner BEFORE attempting removal (prevents ghost flash)
                if (_dragAdorner != null)
                {
                    _dragAdorner.IsVisible = false;
                }

                if (_dragAdorner != null && _adornerLayer != null)
                {
                    try
                    {
                        _adornerLayer.Children.Remove(_dragAdorner);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError("RemoveDragAdorner", $"Failed to remove adorner: {ex.Message}");
                        // If removal fails, try clearing entire layer as fallback
                        try
                        {
                            _adornerLayer.Children.Clear();
                        }
                        catch
                        {
                            // Even clearing failed - layer might be disposed
                        }
                    }

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

                    DebugLogger.Log("VisualBuilderTab", "Ghost image removed and disposed");
                }
            }
            finally
            {
                // ALWAYS clear references, even if cleanup fails
                _dragAdorner = null;
                _adornerTransform = null;
                _adornerLeanTransform = null;
                _adornerLayer = null;
                _topLevel = null;
            }

            // Hide all drop zone overlays when drag ends
            HideAllDropZoneOverlays();
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

            // Show ALL drop zone overlays so user can see where to drop
            var mustOverlay = this.FindControl<Border>("MustDropOverlay");
            var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");

            // Check if we're dragging a BannedItems operator (cannot drop in SHOULD)
            bool isDraggingBannedItems = _draggedItem is FilterOperatorItem opItem &&
                                         opItem.OperatorType == "BannedItems";

            // Show all overlays except the source zone (if dragging from a zone)
            if (mustOverlay != null)
                mustOverlay.IsVisible = (excludeZone != "MustDropZone");
            if (shouldOverlay != null)
                shouldOverlay.IsVisible = (excludeZone != "ShouldDropZone") && !isDraggingBannedItems;

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

            var returnOverlay = this.FindControl<Border>("ReturnOverlay");
            if (returnOverlay != null)
                returnOverlay.IsVisible = false;

            var favoritesOverlay = this.FindControl<Border>("FavoritesDropOverlay");
            if (favoritesOverlay != null)
                favoritesOverlay.IsVisible = false;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // CRITICAL-001 FIX: Unsubscribe from ViewModel events to prevent memory leaks
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.PropertyChanged -= OnViewModelPropertyChanged;

                // Unsubscribe collection change handlers
                vm.SelectedMust.CollectionChanged -= OnMustCollectionChanged;
                vm.SelectedShould.CollectionChanged -= OnShouldCollectionChanged;
            }

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

        // Collection change handlers with stored references for unsubscription
        private void OnMustCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"SelectedMust CollectionChanged - Action: {e.Action}, NewItems: {e.NewItems?.Count ?? 0}, Count: {vm.SelectedMust.Count}"
                );
            }
        }

        private void OnShouldCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                DebugLogger.Log(
                    "VisualBuilderTab",
                    $"SelectedShould CollectionChanged - Action: {e.Action}, NewItems: {e.NewItems?.Count ?? 0}, Count: {vm.SelectedShould.Count}"
                );
            }
        }


        /// <summary>
        /// Handle MUST zone label click - zones are always expanded now
        /// </summary>
        private void OnMustLabelClick(object? sender, PointerPressedEventArgs e)
        {
            // Zones are always expanded - no action needed
            DebugLogger.Log("VisualBuilderTab", "MUST zone label clicked (zones always expanded)");
        }

        /// <summary>
        /// Handle pointer entering MUST drop zone - show overlay only during drag
        /// </summary>
        private void OnMustZonePointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.IsMustHovered = true;
            }

            // HIDE overlay when hovering - you're already here, don't need the map!
            if (_isDragging)
            {
                var mustOverlay = this.FindControl<Border>("MustDropOverlay");
                if (mustOverlay != null)
                    mustOverlay.IsVisible = false;
            }
        }

        /// <summary>
        /// Handle pointer leaving MUST drop zone - show overlay again (navigational aid from afar)
        /// </summary>
        private void OnMustZonePointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.IsMustHovered = false;
            }

            // SHOW overlay when leaving - guide user from a distance
            if (_isDragging)
            {
                var mustOverlay = this.FindControl<Border>("MustDropOverlay");
                if (mustOverlay != null)
                    mustOverlay.IsVisible = true;
            }
        }

        /// <summary>
        /// Handle SHOULD zone label click - zones are always expanded now
        /// </summary>
        private void OnShouldLabelClick(object? sender, PointerPressedEventArgs e)
        {
            // Zones are always expanded - no action needed
            DebugLogger.Log(
                "VisualBuilderTab",
                "SHOULD zone label clicked (zones always expanded)"
            );
        }

        /// <summary>
        /// Handle pointer entering SHOULD drop zone - show overlay only during drag
        /// </summary>
        private void OnShouldZonePointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.IsShouldHovered = true;
            }

            // HIDE overlay when hovering - you're already here, don't need the map!
            if (_isDragging)
            {
                var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");
                if (shouldOverlay != null)
                    shouldOverlay.IsVisible = false;
            }
        }

        /// <summary>
        /// Handle pointer leaving SHOULD drop zone - show overlay again (navigational aid from afar)
        /// </summary>
        private void OnShouldZonePointerExited(object? sender, PointerEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                vm.IsShouldHovered = false;
            }

            // SHOW overlay when leaving - guide user from a distance
            if (_isDragging)
            {
                var shouldOverlay = this.FindControl<Border>("ShouldDropOverlay");
                if (shouldOverlay != null)
                    shouldOverlay.IsVisible = true;
            }
        }


        #region Unified Operator Tray Handlers

        /// <summary>
        /// Handle clicking on unified operator in tray - starts dragging the populated operator
        /// </summary>
        private void OnUnifiedTrayPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isDragging || _isAnimating)
                return;

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || vm.UnifiedOperator.Children.Count == 0)
                return;

            var pointerPoint = e.GetCurrentPoint(sender as Control);
            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

            // Start dragging the unified operator with its children
            _draggedItem = vm.UnifiedOperator;
            _isDragging = true;
            _isDraggingTray = true; // Disable drop acceptance on trays while dragging
            _originalDragSource = sender as Control;
            _sourceDropZone = null; // From tray, not a drop zone

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null && _originalDragSource != null)
            {
                var sourcePos = _originalDragSource.TranslatePoint(
                    new Avalonia.Point(0, 0),
                    topLevel
                );
                _dragStartPosition = sourcePos ?? e.GetPosition(topLevel);

                // Store the click offset relative to the card origin (where user grabbed)
                var clickPos = e.GetPosition(topLevel);
                _dragOffset = new Avalonia.Point(
                    clickPos.X - _dragStartPosition.X,
                    clickPos.Y - _dragStartPosition.Y
                );

                // Initialize previous position for velocity tracking
                _previousMousePosition = clickPos;
            }
            else
            {
                _dragStartPosition = e.GetPosition(this);
                _dragOffset = new Avalonia.Point(0, 0);
                _previousMousePosition = e.GetPosition(this);
            }

            DebugLogger.Log(
                "VisualBuilderTab",
                $"Started dragging unified operator ({vm.UnifiedOperator.OperatorType}) with {vm.UnifiedOperator.Children.Count} children"
            );

            CreateDragAdorner(_draggedItem, _dragStartPosition);

            // Start spring physics timer
            _springUpdateTimer?.Start();

            ShowDropZoneOverlays();

            e.Handled = true;
        }

        /// <summary>
        /// Handle drag over unified tray - allow dropping items INTO the tray
        /// </summary>
        private void OnUnifiedTrayDragOver(object? sender, DragEventArgs e)
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
                bool canDrop =
                    (_draggedItem is not FilterOperatorItem)
                    || (
                        _draggedItem is FilterOperatorItem && !string.IsNullOrEmpty(_sourceDropZone)
                    );

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
        /// Handle drag leave from unified tray - reset visual state
        /// </summary>
        private void OnUnifiedTrayDragLeave(object? sender, DragEventArgs e)
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
        /// Handle dropping an item INTO the unified tray
        /// </summary>
        private void OnUnifiedTrayDrop(object? sender, DragEventArgs e)
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
                DebugLogger.Log("VisualBuilderTab", $"Adding {_draggedItem.Name} to unified tray");

                // Add a COPY of the item to the tray (so users can add same item multiple times)
                var itemCopy = new Models.FilterItem
                {
                    Name = _draggedItem.Name,
                    Type = _draggedItem.Type,
                    Category = _draggedItem.Category,
                    DisplayName = _draggedItem.DisplayName,
                    ItemImage = _draggedItem.ItemImage,
                };

                vm.UnifiedOperator.Children.Add(itemCopy);
                e.Handled = true;
            }
        }

        #endregion

        #region Property Changed Handler

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            // Arrow positioning is now handled via data binding in XAML
            // Other property change handling can go here if needed
        }

        #endregion

        #region Carousel Pagination Event Handlers

        // MUST zone pagination
        private void OnMustPrevClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "MUST Previous page clicked");
            }
        }

        private void OnMustNextClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "MUST Next page clicked");
            }
        }

        // SHOULD zone pagination
        private void OnShouldPrevClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "SHOULD Previous page clicked");
            }
        }

        private void OnShouldNextClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "SHOULD Next page clicked");
            }
        }

        // BANNED zone pagination
        private void OnBannedPrevClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "BANNED Previous page clicked");
            }
        }

        private void OnBannedNextClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                // TODO: Implement pagination logic in ViewModel
                DebugLogger.Log("VisualBuilderTab", "BANNED Next page clicked");
            }
        }

        #endregion
    }
}
