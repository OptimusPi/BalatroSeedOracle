using System;
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
        private AdornerLayer? _adornerLayer;
        private TranslateTransform? _adornerTransform;
        private TopLevel? _topLevel;
        private bool _isDragging = false;
        private Avalonia.Point _dragStartPosition;

        public VisualBuilderTab()
        {
            InitializeComponent();

            // Set DataContext to the VisualBuilderTabViewModel
            DataContext = ServiceHelper.GetRequiredService<ViewModels.FilterTabs.VisualBuilderTabViewModel>();

            // Setup drop zones AFTER the control is attached to visual tree
            this.AttachedToVisualTree += (s, e) => SetupDropZones();
        }
        
        private void SetupDropZones()
        {
            // Find drop zones - we'll check them manually via hit testing
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");

            DebugLogger.Log("VisualBuilderTab", $"Drop zones found - Must: {mustZone != null}, Should: {shouldZone != null}, MustNot: {mustNotZone != null}");

            // Attach pointer handlers to TopLevel so we get events even when pointer moves outside the control
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.PointerMoved += OnPointerMovedManualDrag;
                topLevel.PointerReleased += OnPointerReleasedManualDrag;
                DebugLogger.Log("VisualBuilderTab", "Manual drag pointer handlers attached to TopLevel");
            }
            else
            {
                DebugLogger.LogError("VisualBuilderTab", "Failed to attach pointer handlers - no TopLevel");
            }
        }
        
        private void OnItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try
            {
                Models.FilterItem? item = null;

                // Handle Image, StackPanel, Grid, and Border elements
                if (sender is Image image)
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
                else if (sender is Border border)
                {
                    item = border.DataContext as Models.FilterItem;
                }

                if (item != null)
                {
                    _draggedItem = item;
                    _isDragging = true;
                    _dragStartPosition = e.GetPosition(this);

                    DebugLogger.Log("VisualBuilderTab", $"🎯 MANUAL DRAG START for item: {item.Name}");

                    // BALATRO-STYLE: Make the card "disappear" from grid while dragging
                    item.IsBeingDragged = true;

                    // Play card select sound
                    SoundEffectService.Instance.PlayCardSelect();

                    // CREATE GHOST IMAGE
                    CreateDragAdorner(item, _dragStartPosition);

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
        
        private void OnPointerMovedManualDrag(object? sender, PointerEventArgs e)
        {
            if (!_isDragging || _draggedItem == null) return;

            try
            {
                DebugLogger.Log("VisualBuilderTab", $"🖱️ Pointer moved during drag");

                // Update adorner position
                if (_dragAdorner != null && _adornerTransform != null && _topLevel != null)
                {
                    var position = e.GetPosition(_topLevel);
                    _adornerTransform.X = position.X - 35; // Center on cursor
                    _adornerTransform.Y = position.Y - 47;
                    DebugLogger.Log("VisualBuilderTab", $"Ghost moved to ({position.X}, {position.Y})");
                }

                // Check if we're over a drop zone and provide visual feedback
                var mustZone = this.FindControl<Border>("MustDropZone");
                var shouldZone = this.FindControl<Border>("ShouldDropZone");
                var mustNotZone = this.FindControl<Border>("MustNotDropZone");

                if (_topLevel == null) return;
                var cursorPos = e.GetPosition(_topLevel);

                Border? targetZone = null;

                if (IsPointOverControl(cursorPos, mustZone, _topLevel))
                {
                    targetZone = mustZone;
                }
                else if (IsPointOverControl(cursorPos, shouldZone, _topLevel))
                {
                    targetZone = shouldZone;
                }
                else if (IsPointOverControl(cursorPos, mustNotZone, _topLevel))
                {
                    targetZone = mustNotZone;
                }

                if (targetZone != null)
                {
                    // Add visual feedback
                    if (!targetZone.Classes.Contains("drag-over"))
                    {
                        targetZone.Classes.Add("drag-over");
                    }
                    RemoveDragOverClassExcept(targetZone);
                }
                else
                {
                    RemoveDragOverClassExcept(null);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Error in OnPointerMovedManualDrag: {ex.Message}");
            }
        }

        private bool IsPointOverControl(Avalonia.Point point, Control? control, TopLevel topLevel)
        {
            if (control == null) return false;

            try
            {
                // Transform point from TopLevel coordinates to control coordinates
                var controlPos = control.TranslatePoint(new Avalonia.Point(0, 0), topLevel);
                if (!controlPos.HasValue) return false;

                var bounds = new Avalonia.Rect(controlPos.Value, new Avalonia.Size(control.Bounds.Width, control.Bounds.Height));
                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        private void RemoveDragOverClassExcept(Border? exceptZone)
        {
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");

            if (mustZone != exceptZone)
                mustZone?.Classes.Remove("drag-over");
            if (shouldZone != exceptZone)
                shouldZone?.Classes.Remove("drag-over");
            if (mustNotZone != exceptZone)
                mustNotZone?.Classes.Remove("drag-over");
        }

        private void OnPointerReleasedManualDrag(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _draggedItem == null) return;

            try
            {
                DebugLogger.Log("VisualBuilderTab", $"🎯 MANUAL DRAG RELEASED");

                // Remove all visual feedback
                RemoveDragOverClassExcept(null);

                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
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

                var mustZone = this.FindControl<Border>("MustDropZone");
                var shouldZone = this.FindControl<Border>("ShouldDropZone");
                var mustNotZone = this.FindControl<Border>("MustNotDropZone");

                Border? targetZone = null;
                string? zoneName = null;

                if (IsPointOverControl(cursorPos, mustZone, _topLevel))
                {
                    targetZone = mustZone;
                    zoneName = "MustDropZone";
                }
                else if (IsPointOverControl(cursorPos, shouldZone, _topLevel))
                {
                    targetZone = shouldZone;
                    zoneName = "ShouldDropZone";
                }
                else if (IsPointOverControl(cursorPos, mustNotZone, _topLevel))
                {
                    targetZone = mustNotZone;
                    zoneName = "MustNotDropZone";
                }

                if (targetZone != null && zoneName != null)
                {
                    DebugLogger.Log("VisualBuilderTab", $"✅ Dropping {_draggedItem.Name} into {zoneName}");

                    // Play card drop sound
                    SoundEffectService.Instance.PlayCardDrop();

                    switch (zoneName)
                    {
                        case "MustDropZone":
                            vm.AddToMustCommand.Execute(_draggedItem);
                            break;
                        case "ShouldDropZone":
                            vm.AddToShouldCommand.Execute(_draggedItem);
                            break;
                        case "MustNotDropZone":
                            vm.AddToMustNotCommand.Execute(_draggedItem);
                            break;
                    }
                }
                else
                {
                    DebugLogger.Log("VisualBuilderTab", "Drop cancelled - not over any drop zone");
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
            }
        }
        
        private void OnItemDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Models.FilterItem? item = null;

            if (sender is Grid grid)
            {
                item = grid.DataContext as Models.FilterItem;
            }
            else if (sender is Border border)
            {
                item = border.DataContext as Models.FilterItem;
            }

            if (item == null) return;

            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null) return;

            var config = vm.ItemConfigs.TryGetValue(item.ItemKey, out var existingConfig)
                ? existingConfig
                : new Models.ItemConfig { ItemKey = item.ItemKey, ItemType = item.ItemType };
            var popupViewModel = new ViewModels.ItemConfigPopupViewModel(config);
            popupViewModel.ItemName = item.DisplayName;
            popupViewModel.ItemImage = item.ItemImage;


            // Create the View
            var configPopup = new Controls.ItemConfigPopup
            {
                DataContext = popupViewModel
            };

            var popup = new Avalonia.Controls.Primitives.Popup
            {
                Child = configPopup,
                Placement = Avalonia.Controls.PlacementMode.Pointer,
                IsLightDismissEnabled = false
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
                    var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                    if (vm != null)
                    {
                        // Disable reactive updates temporarily
                        vm.SetCategory(category);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Category click failed: {ex.Message}");
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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void CreateDragAdorner(Models.FilterItem item, Avalonia.Point startPosition)
        {
            DebugLogger.Log("VisualBuilderTab", $"🫡 CreateDragAdorner beginning,.......");

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
                    DebugLogger.Log("VisualBuilderTab", $"🔍 ADORNER SEARCH [{timestamp}] TopLevel type: {_topLevel.GetType().Name}");

                    // First try standard GetAdornerLayer
                    _adornerLayer = AdornerLayer.GetAdornerLayer(_topLevel);
                    DebugLogger.Log("VisualBuilderTab", $"GetAdornerLayer result: {_adornerLayer != null}");

                    if (_adornerLayer == null && _topLevel is Window window)
                    {
                        DebugLogger.Log("VisualBuilderTab", $"Window.Content type: {window.Content?.GetType().Name ?? "null"}");

                        if (window.Content is Panel panel)
                        {
                            DebugLogger.Log("VisualBuilderTab", $"Panel has {panel.Children.Count} children");

                            // Fallback: Find AdornerLayer in the Panel's children
                            foreach (var child in panel.Children)
                            {
                                DebugLogger.Log("VisualBuilderTab", $"Panel child: {child.GetType().Name}");

                                if (child is AdornerLayer layer)
                                {
                                    _adornerLayer = layer;
                                    DebugLogger.Log("VisualBuilderTab", "✅ Found AdornerLayer in MainWindow Panel");
                                    break;
                                }
                            }
                        }
                    }
                }

                if (_adornerLayer == null)
                {
                    DebugLogger.LogError("VisualBuilderTab", $"Failed to find adorner layer! TopLevel: {_topLevel != null}");
                    return;
                }

                // Create ghost image - 80% opacity with subtle sway like Balatro
                // For legendary jokers, layer the soul face on top
                var imageGrid = new Grid
                {
                    Width = 71,
                    Height = 95
                };

                // Main card image
                imageGrid.Children.Add(new Image
                {
                    Source = item.ItemImage,
                    Width = 71,
                    Height = 95,
                    Stretch = Stretch.Uniform,
                    Opacity = 0.8 // BALATRO-STYLE 80% OPACITY
                });

                // Soul face overlay for legendary jokers
                if (item.SoulFaceImage != null)
                {
                    imageGrid.Children.Add(new Image
                    {
                        Source = item.SoulFaceImage,
                        Width = 71,
                        Height = 95,
                        Stretch = Stretch.Uniform,
                        Opacity = 1.0
                    });
                }

                // Create card content that will have physics applied
                var cardContent = new Border
                {
                    Background = Brushes.Transparent,
                    Child = imageGrid
                };

                // ADD BALATRO-STYLE SWAY PHYSICS TO THE CARD!
                // Apply the CardDragBehavior for tilt, sway, and juice effects
                var dragBehavior = new Behaviors.CardDragBehavior
                {
                    IsEnabled = true,
                    JuiceAmount = 0.4 // Balatro default juice on pickup
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
                                    FontSize = 10,
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                    Margin = new Avalonia.Thickness(0, 4, 0, 0),
                                    Opacity = 0.9
                                }
                            }
                        }
                };

                // Create transform and store reference so we can update it during drag
                _adornerTransform = new TranslateTransform(startPosition.X, startPosition.Y);
                _dragAdorner.RenderTransform = _adornerTransform;

                AdornerLayer.SetAdornedElement(_dragAdorner, this);
                _adornerLayer.Children.Add(_dragAdorner);

                DebugLogger.Log("VisualBuilderTab", $"Ghost image created at ({startPosition.X}, {startPosition.Y})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Failed to create drag adorner: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Failed to remove drag adorner: {ex.Message}");
            }
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
    }
}