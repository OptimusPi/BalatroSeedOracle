using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Views;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// PROPER MVVM Visual Builder Tab - zero business logic, pure view
    /// ViewModel set via XAML binding, not code-behind
    /// </summary>
    public partial class VisualBuilderTab : UserControl
    {
        private static AdornerLayer? _staticAdornerLayer;
        private Models.FilterItem? _draggedItem;
        private Border? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private TranslateTransform? _adornerTransform;
        private TopLevel? _topLevel;

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
            // Find drop zones and enable drag-drop
            var mustZone = this.FindControl<Border>("MustDropZone");
            var shouldZone = this.FindControl<Border>("ShouldDropZone");
            var mustNotZone = this.FindControl<Border>("MustNotDropZone");
            
            DebugLogger.Log("VisualBuilderTab", $"Drop zones found - Must: {mustZone != null}, Should: {shouldZone != null}, MustNot: {mustNotZone != null}");
            
            if (mustZone != null)
            {
                mustZone.AddHandler(DragDrop.DropEvent, OnDrop);
                mustZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                mustZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
                DebugLogger.Log("VisualBuilderTab", "Must zone handlers attached");
            }
            
            if (shouldZone != null)
            {
                shouldZone.AddHandler(DragDrop.DropEvent, OnDrop);
                shouldZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                shouldZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
                DebugLogger.Log("VisualBuilderTab", "Should zone handlers attached");
            }
            
            if (mustNotZone != null)
            {
                mustNotZone.AddHandler(DragDrop.DropEvent, OnDrop);
                mustNotZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                mustNotZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
                DebugLogger.Log("VisualBuilderTab", "MustNot zone handlers attached");
            }
        }
        
        private async void OnItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            try
            {
                await OnItemPointerPressedAsync(sender, e);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Drag operation failed: {ex.Message}");
                // Ensure adorner is cleaned up on error
                RemoveDragAdorner();
            }
        }

        private async Task OnItemPointerPressedAsync(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Ensure we're on UI thread at start
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => OnItemPointerPressedAsync(sender, e));
                return;
            }

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
                DebugLogger.Log("VisualBuilderTab", $"Starting drag for item: {item.Name}");

                // BALATRO-STYLE: Make the card "disappear" from grid while dragging
                item.IsBeingDragged = true;

                // Play card select sound
                SoundEffectService.Instance.PlayCardSelect();

                var topLevel = TopLevel.GetTopLevel(this);
                bool adornerCreated = CreateDragAdorner(item, e.GetPosition(topLevel));

                if (!adornerCreated)
                {
                    // Adorner creation failed - restore item visibility and abort drag
                    item.IsBeingDragged = false;
                    DebugLogger.LogError("VisualBuilderTab", "Drag aborted - failed to create adorner");
                    return;
                }

                var dragData = new Avalonia.Input.DataObject();
                dragData.Set("FilterItem", item);

                var result = await DragDrop.DoDragDrop(e, dragData, Avalonia.Input.DragDropEffects.Copy);

                // After await, marshal back to UI thread for cleanup
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RemoveDragAdorner();

                    // BALATRO-STYLE: Restore the card in the grid
                    item.IsBeingDragged = false;
                });

                DebugLogger.Log("VisualBuilderTab", $"Drag completed with result: {result}");
            }
            else
            {
                DebugLogger.Log("VisualBuilderTab", "No item found for drag operation");
            }
        }
        
        private void OnDragOver(object? sender, Avalonia.Input.DragEventArgs e)
        {
            DebugLogger.Log("VisualBuilderTab", $"OnDragOver called on {(sender as Border)?.Name ?? "unknown"}");
            
            if (e.Data.Contains("FilterItem"))
            {
                e.DragEffects = Avalonia.Input.DragDropEffects.Copy;
                DebugLogger.Log("VisualBuilderTab", "Drag accepted - FilterItem data found");
                
                // Add visual feedback
                if (sender is Border border && !border.Classes.Contains("drag-over"))
                {
                    border.Classes.Add("drag-over");
                }
            }
            else
            {
                e.DragEffects = Avalonia.Input.DragDropEffects.None;
                DebugLogger.Log("VisualBuilderTab", "Drag rejected - no FilterItem data");
            }
        }
        
        private void OnDragLeave(object? sender, Avalonia.Input.DragEventArgs e)
        {
            // Remove visual feedback
            if (sender is Border border)
            {
                border.Classes.Remove("drag-over");
            }
        }
        
        private void OnDrop(object? sender, Avalonia.Input.DragEventArgs e)
        {
            DebugLogger.Log("VisualBuilderTab", $"OnDrop called on {(sender as Border)?.Name ?? "unknown"}");
            
            // Remove visual feedback
            if (sender is Border border)
            {
                border.Classes.Remove("drag-over");
            }
            
            if (e.Data.Contains("FilterItem") && DataContext is BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel vm)
            {
                var item = e.Data.Get("FilterItem") as Models.FilterItem;
                if (item != null && sender is Border dropBorder)
                {
                    DebugLogger.Log("VisualBuilderTab", $"Dropping {item.Name} into {dropBorder.Name}");
                    
                    // Play card drop sound
                    SoundEffectService.Instance.PlayCardDrop();
                    
                    switch (dropBorder.Name)
                    {
                        case "MustDropZone":
                            vm.AddToMustCommand.Execute(item);
                            break;
                        case "ShouldDropZone":
                            vm.AddToShouldCommand.Execute(item);
                            break;
                        case "MustNotDropZone":
                            vm.AddToMustNotCommand.Execute(item);
                            break;
                    }
                }
            }
            else
            {
                DebugLogger.Log("VisualBuilderTab", "Drop failed - no FilterItem data or ViewModel");
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
        
        private bool CreateDragAdorner(Models.FilterItem item, Avalonia.Point startPosition)
        {
            try
            {
                if (_staticAdornerLayer != null)
                {
                    _adornerLayer = _staticAdornerLayer;
                }
                else
                {
                    _topLevel = TopLevel.GetTopLevel(this);
                    if (_topLevel == null)
                    {
                        DebugLogger.LogError("VisualBuilderTab", "TopLevel not found!");
                        return false;
                    }

                    var mainWindow = _topLevel as MainWindow;
                    if (mainWindow != null)
                    {
                        var panel = mainWindow.Content as Panel;
                        if (panel != null)
                        {
                            _adornerLayer = panel.Children.OfType<AdornerLayer>().FirstOrDefault();
                            if (_adornerLayer != null)
                            {
                                _staticAdornerLayer = _adornerLayer;
                                DebugLogger.Log("VisualBuilderTab", "Found and cached AdornerLayer from MainWindow!");
                            }
                        }
                    }

                    if (_adornerLayer == null)
                    {
                        DebugLogger.LogError("VisualBuilderTab", "AdornerLayer not found in MainWindow.Content!");
                        return false;
                    }
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
                        Opacity = 0.8
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
                        },
                    ZIndex = 1000 // Ensure it's on top
                };

                // Create transform and store reference so we can update it during drag
                _adornerTransform = new TranslateTransform(startPosition.X - 35, startPosition.Y - 47);
                _dragAdorner.RenderTransform = _adornerTransform;

                AdornerLayer.SetAdornedElement(_dragAdorner, this);
                _adornerLayer.Children.Add(_dragAdorner);

                // CRITICAL FIX: Attach global pointer handler to TopLevel so adorner follows cursor during drag
                // During DragDrop.DoDragDrop(), normal PointerMoved events don't reach the control
                if (_topLevel != null)
                {
                    _topLevel.PointerMoved += OnPointerMovedDuringDrag;
                    DebugLogger.Log("VisualBuilderTab", "Global pointer handler attached to TopLevel");
                }

                DebugLogger.Log("VisualBuilderTab", $"Ghost image created at ({startPosition.X}, {startPosition.Y})");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VisualBuilderTab", $"Failed to create drag adorner: {ex.Message}");
                return false;
            }
        }

        private void OnPointerMovedDuringDrag(object? sender, PointerEventArgs e)
        {
            // Update adorner position to follow cursor during drag
            if (_dragAdorner != null && _adornerTransform != null && _topLevel != null)
            {
                // Get position relative to TopLevel (where adorner layer is)
                var position = e.GetPosition(_topLevel);
                _adornerTransform.X = position.X - 35; // Center on cursor (half of 71px width)
                _adornerTransform.Y = position.Y - 47; // Center on cursor (half of 95px height)
            }
        }

        private void RemoveDragAdorner()
        {
            try
            {
                // CRITICAL: Detach global pointer handler from TopLevel to prevent memory leak
                if (_topLevel != null)
                {
                    _topLevel.PointerMoved -= OnPointerMovedDuringDrag;
                    DebugLogger.Log("VisualBuilderTab", "Global pointer handler detached from TopLevel");
                }

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
            // Ensure cleanup on navigation away
            RemoveDragAdorner();
            base.OnDetachedFromVisualTree(e);
        }
    }
}