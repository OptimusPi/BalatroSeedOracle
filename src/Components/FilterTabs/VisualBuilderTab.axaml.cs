using System;
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

                // Play card select sound
                SoundEffectService.Instance.PlayCardSelect();

                // CREATE GHOST IMAGE - Adorner layer approach from research
                CreateDragAdorner(item, e.GetPosition(this));

                var dragData = new Avalonia.Input.DataObject();
                dragData.Set("FilterItem", item);

                var result = await DragDrop.DoDragDrop(e, dragData, Avalonia.Input.DragDropEffects.Copy);

                // After await, marshal back to UI thread for cleanup
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    RemoveDragAdorner();
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
            
            // Handle both Grid and Border elements
            if (sender is Grid grid)
            {
                item = grid.DataContext as Models.FilterItem;
            }
            else if (sender is Border border)
            {
                item = border.DataContext as Models.FilterItem;
            }
            
            if (item != null)
            {
                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm == null) return;
                
                // Create and show configuration popup
                var configPopup = new Controls.ItemConfigPopup();
                configPopup.SetItem(item.ItemKey, item.ItemType, item.DisplayName);
                
                // Get current configuration if exists
                if (vm.ItemConfigs.TryGetValue(item.ItemKey, out var existingConfig))
                {
                    configPopup.LoadConfiguration(existingConfig);
                }
                
                var popup = new Avalonia.Controls.Primitives.Popup
                {
                    Child = configPopup,
                    Placement = Avalonia.Controls.PlacementMode.Pointer,
                    IsLightDismissEnabled = false
                };
                
                configPopup.ConfigApplied += (s, args) =>
                {
                    // Update the configuration
                    vm.UpdateItemConfig(item.ItemKey, args.Configuration);
                    popup.IsOpen = false;
                };
                
                configPopup.Cancelled += (s, args) =>
                {
                    popup.IsOpen = false;
                };
                
                configPopup.DeleteRequested += (s, args) =>
                {
                    // Remove the item from its zone
                    vm.RemoveItem(item);
                    popup.IsOpen = false;
                };
                
                popup.IsOpen = true;
            }
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
            try
            {
                // Find adorner layer - research shows this is the proper approach
                var topLevel = this.FindAncestorOfType<TopLevel>();
                _adornerLayer = topLevel != null ? AdornerLayer.GetAdornerLayer(topLevel) : null;
                if (_adornerLayer == null) return;
                
                // Create ghost image - 80% opacity with subtle sway like Balatro
                _dragAdorner = new Border
                {
                    Background = Brushes.Transparent,
                    Child = new StackPanel
                        {
                            Children =
                            {
                                new Image
                                {
                                    Source = item.ItemImage,
                                    Width = 71,
                                    Height = 95,
                                    Stretch = Stretch.Uniform,
                                    Opacity = 0.8 // BALATRO-STYLE 80% OPACITY
                                },
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
                    RenderTransform = new TranslateTransform(startPosition.X - 35, startPosition.Y - 47), // Center on cursor
                    ZIndex = 1000 // Ensure it's on top
                };
                
                AdornerLayer.SetAdornedElement(_dragAdorner, this);
                _adornerLayer.Children.Add(_dragAdorner);
                
                DebugLogger.Log("VisualBuilderTab", "Ghost image created with adorner layer");
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