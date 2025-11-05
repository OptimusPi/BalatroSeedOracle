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
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// Configure Score Tab - Shows SHOULD items as a list of rows
    /// Reuses VisualBuilderTabViewModel but with a simpler row-based UI
    /// </summary>
    public partial class ConfigureScoreTab : UserControl
    {
        private Models.FilterItem? _draggedItem;
        private Border? _dragAdorner;
        private Avalonia.Media.TranslateTransform? _adornerTransform;
        private Avalonia.Controls.Primitives.AdornerLayer? _adornerLayer;
        private TopLevel? _topLevel;
        private bool _isDragging = false;
        private Avalonia.Point _dragStartPosition;
        private Control? _originalDragSource;

        public ConfigureScoreTab()
        {
            InitializeComponent();

            // Only set DataContext if not already set by parent (e.g., from FiltersModalViewModel)
            // This allows both tab instances to share the same VisualBuilderTabViewModel
            if (DataContext == null)
            {
                DataContext = ServiceHelper.GetRequiredService<ViewModels.FilterTabs.VisualBuilderTabViewModel>();
            }

            // Setup drop zone handling after visual tree is ready
            this.AttachedToVisualTree += (s, e) => SetupDropZones();
        }

        private void SetupDropZones()
        {
            var scoreListZone = this.FindControl<Border>("ScoreListDropZone");
            if (scoreListZone != null)
            {
                scoreListZone.AddHandler(DragDrop.DragOverEvent, OnScoreListDragOver);
                scoreListZone.AddHandler(DragDrop.DragLeaveEvent, OnScoreListDragLeave);
                scoreListZone.AddHandler(DragDrop.DropEvent, OnScoreListDrop);
                DebugLogger.Log("ConfigureScoreTab", "Score list drop handlers attached");
            }

            // Setup OR tray drop zone
            var orTray = this.FindControl<Border>("OrTray");
            if (orTray != null)
            {
                orTray.AddHandler(DragDrop.DragOverEvent, OnOrTrayDragOver);
                orTray.AddHandler(DragDrop.DragLeaveEvent, OnOrTrayDragLeave);
                orTray.AddHandler(DragDrop.DropEvent, OnOrTrayDrop);
                DebugLogger.Log("ConfigureScoreTab", "OR tray drop handlers attached");
            }

            // Setup AND tray drop zone
            var andTray = this.FindControl<Border>("AndTray");
            if (andTray != null)
            {
                andTray.AddHandler(DragDrop.DragOverEvent, OnAndTrayDragOver);
                andTray.AddHandler(DragDrop.DragLeaveEvent, OnAndTrayDragLeave);
                andTray.AddHandler(DragDrop.DropEvent, OnAndTrayDrop);
                DebugLogger.Log("ConfigureScoreTab", "AND tray drop handlers attached");
            }

            // Attach pointer handlers to TopLevel for drag operations
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                topLevel.PointerMoved += OnPointerMovedManualDrag;
                topLevel.PointerReleased += OnPointerReleasedManualDrag;
                DebugLogger.Log("ConfigureScoreTab", "Manual drag pointer handlers attached");
            }
        }

        private void OnCardPointerEntered(object? sender, PointerEventArgs e)
        {
            // No special handling needed for this tab
        }

        private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isDragging)
                return;

            try
            {
                Models.FilterItem? item = null;

                // Handle various control types
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
                    var pointerPoint = e.GetCurrentPoint(sender as Control);

                    // Only left-click drag
                    if (!pointerPoint.Properties.IsLeftButtonPressed)
                    {
                        return;
                    }

                    _draggedItem = item;
                    _isDragging = true;
                    _originalDragSource = sender as Control;

                    // Get position relative to TopLevel
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

                    DebugLogger.Log("ConfigureScoreTab", $"Manual drag start for item: {item.Name}");

                    // Hide original during drag
                    item.IsBeingDragged = true;

                    // Create ghost image
                    CreateDragAdorner(item, _dragStartPosition);

                    // Show drop overlay
                    ShowDropOverlay();

                    // Phase 2: Transition to DragActive state
                    var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                    vm?.EnterDragActiveState();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Drag operation failed: {ex.Message}");
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
                // Update adorner position
                if (_adornerTransform != null && _topLevel != null)
                {
                    var position = e.GetPosition(_topLevel);
                    _adornerTransform.X = position.X - 24;
                    _adornerTransform.Y = position.Y - 32;
                }

                // Note: Overlay stays visible throughout entire drag operation
                // This provides continuous visual feedback about where to drop
                // The overlay will be hidden only when drag completes in RemoveDragAdorner()
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Error in OnPointerMovedManualDrag: {ex.Message}");
            }
        }

        private void OnPointerReleasedManualDrag(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging || _draggedItem == null)
                return;

            try
            {
                DebugLogger.Log("ConfigureScoreTab", "Manual drag operation released");

                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                if (vm == null)
                {
                    DebugLogger.Log("ConfigureScoreTab", "Drop failed - no ViewModel");
                    return;
                }

                // Check if dropped on score list zone
                if (_topLevel == null)
                {
                    DebugLogger.Log("ConfigureScoreTab", "Drop failed - no TopLevel");
                    return;
                }

                var cursorPos = e.GetPosition(_topLevel);
                var scoreListZone = this.FindControl<Border>("ScoreListDropZone");
                var orTray = this.FindControl<Border>("OrTray");
                var andTray = this.FindControl<Border>("AndTray");

                // Check which zone the item was dropped on
                if (IsPointOverControl(cursorPos, orTray, _topLevel))
                {
                    // Add to OR tray
                    DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to OR tray");
                    vm.AddToOrTrayCommand.Execute(_draggedItem);
                }
                else if (IsPointOverControl(cursorPos, andTray, _topLevel))
                {
                    // Add to AND tray
                    DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to AND tray");
                    vm.AddToAndTrayCommand.Execute(_draggedItem);
                }
                else if (IsPointOverControl(cursorPos, scoreListZone, _topLevel))
                {
                    // Add to SHOULD collection (score columns)
                    DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to score columns");
                    vm.AddToShouldCommand.Execute(_draggedItem);
                }
                else
                {
                    DebugLogger.Log("ConfigureScoreTab", "Drop cancelled - not over any valid zone");
                }
            }
            finally
            {
                // Cleanup - RemoveDragAdorner will hide overlays
                RemoveDragAdorner();
                if (_draggedItem != null)
                {
                    _draggedItem.IsBeingDragged = false;
                }
                _isDragging = false;
                _draggedItem = null;
                _originalDragSource = null;
            }
        }

        private bool IsPointOverControl(Avalonia.Point point, Control? control, TopLevel topLevel)
        {
            if (control == null)
                return false;

            try
            {
                var controlPos = control.TranslatePoint(new Avalonia.Point(0, 0), topLevel);
                if (!controlPos.HasValue)
                    return false;

                var bounds = new Avalonia.Rect(controlPos.Value, new Avalonia.Size(control.Bounds.Width, control.Bounds.Height));
                return bounds.Contains(point);
            }
            catch
            {
                return false;
            }
        }

        private void CreateDragAdorner(Models.FilterItem item, Avalonia.Point startPosition)
        {
            DebugLogger.Log("ConfigureScoreTab", $"Creating drag adorner for item: {item?.Name}");

            try
            {
                _topLevel = TopLevel.GetTopLevel(this);

                if (_topLevel == null)
                {
                    // Fallback: manually walk up visual tree
                    Avalonia.Visual? current = this.GetVisualParent();
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

                // Find AdornerLayer
                if (_topLevel != null)
                {
                    _adornerLayer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(_topLevel);

                    if (_adornerLayer == null && _topLevel is Window window)
                    {
                        if (window.Content is Panel panel)
                        {
                            foreach (var child in panel.Children)
                            {
                                if (child is Avalonia.Controls.Primitives.AdornerLayer layer)
                                {
                                    _adornerLayer = layer;
                                    DebugLogger.Log("ConfigureScoreTab", "Found AdornerLayer in MainWindow Panel");
                                    break;
                                }
                            }
                        }
                    }
                }

                if (_adornerLayer == null)
                {
                    DebugLogger.LogError("ConfigureScoreTab", $"Failed to find adorner layer! TopLevel: {_topLevel != null}");
                    return;
                }

                // Create ghost image
                var imageGrid = new Grid { Width = 48, Height = 64 };

                if (item?.ItemImage != null)
                {
                    imageGrid.Children.Add(new Image
                    {
                        Source = item.ItemImage,
                        Width = 48,
                        Height = 64,
                        Stretch = Avalonia.Media.Stretch.Uniform,
                        Opacity = 0.8,
                    });
                }

                // Soul face overlay for legendary jokers
                if (item?.SoulFaceImage != null)
                {
                    imageGrid.Children.Add(new Image
                    {
                        Source = item.SoulFaceImage,
                        Width = 48,
                        Height = 64,
                        Stretch = Avalonia.Media.Stretch.Uniform,
                        Opacity = 1.0,
                    });
                }

                var cardContent = new Border
                {
                    Background = Avalonia.Media.Brushes.Transparent,
                    Child = imageGrid,
                };

                _dragAdorner = new Border
                {
                    Background = Avalonia.Media.Brushes.Transparent,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            cardContent,
                            new TextBlock
                            {
                                Text = item?.DisplayName ?? "Unknown Item",
                                Foreground = Avalonia.Media.Brushes.White,
                                FontSize = 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Margin = new Avalonia.Thickness(0, 4, 0, 0),
                                Opacity = 1,
                            },
                        },
                    },
                };

                // Position the drag adorner
                _adornerTransform = new Avalonia.Media.TranslateTransform
                {
                    X = startPosition.X,
                    Y = startPosition.Y,
                };
                _dragAdorner.RenderTransform = _adornerTransform;
                _dragAdorner.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                _dragAdorner.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;

                _adornerLayer.Children.Add(_dragAdorner);

                DebugLogger.Log("ConfigureScoreTab", $"Ghost image created at ({startPosition.X}, {startPosition.Y})");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Failed to create drag adorner: {ex.Message}");
            }
        }

        private void RemoveDragAdorner()
        {
            try
            {
                if (_dragAdorner != null && _adornerLayer != null)
                {
                    _adornerLayer.Children.Remove(_dragAdorner);

                    // Cleanup
                    if (_dragAdorner.Child is StackPanel stack)
                    {
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
                    DebugLogger.Log("ConfigureScoreTab", "Ghost image removed and disposed");
                }

                // Hide all drop zone overlays when drag ends
                HideAllDropZoneOverlays();

                // Phase 2: Return to Default state when drag ends
                var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                vm?.EnterDefaultState();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("ConfigureScoreTab", $"Failed to remove drag adorner: {ex.Message}");
            }
        }

        private void ShowDropOverlay()
        {
            // Show dramatic backdrop that dims everything
            var backdrop = this.FindControl<Border>("DragBackdrop");
            if (backdrop != null)
                backdrop.IsVisible = true;

            // Show OR tray overlay
            var orTrayOverlay = this.FindControl<Border>("OrTrayDropOverlay");
            if (orTrayOverlay != null)
                orTrayOverlay.IsVisible = true;

            // Show AND tray overlay
            var andTrayOverlay = this.FindControl<Border>("AndTrayDropOverlay");
            if (andTrayOverlay != null)
                andTrayOverlay.IsVisible = true;

            // Show the score list overlay so user can see where to drop
            var scoreListOverlay = this.FindControl<Border>("ScoreListDropOverlay");
            if (scoreListOverlay != null)
                scoreListOverlay.IsVisible = true;
        }

        private void HideAllDropZoneOverlays()
        {
            // Hide dramatic backdrop
            var backdrop = this.FindControl<Border>("DragBackdrop");
            if (backdrop != null)
                backdrop.IsVisible = false;

            // Hide OR tray overlay
            var orTrayOverlay = this.FindControl<Border>("OrTrayDropOverlay");
            if (orTrayOverlay != null)
                orTrayOverlay.IsVisible = false;

            // Hide AND tray overlay
            var andTrayOverlay = this.FindControl<Border>("AndTrayDropOverlay");
            if (andTrayOverlay != null)
                andTrayOverlay.IsVisible = false;

            // Hide score list overlay
            var scoreListOverlay = this.FindControl<Border>("ScoreListDropOverlay");
            if (scoreListOverlay != null)
                scoreListOverlay.IsVisible = false;
        }

        private void OnCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category)
            {
                try
                {
                    var vm = DataContext as BalatroSeedOracle.ViewModels.FilterTabs.VisualBuilderTabViewModel;
                    if (vm != null)
                    {
                        vm.SetCategory(category);
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("ConfigureScoreTab", $"Category click failed: {ex.Message}");
                }
            }
        }

        #region Drop Zone Handlers

        private void OnScoreListDragOver(object? sender, DragEventArgs e)
        {
            if (_draggedItem != null)
            {
                e.DragEffects = DragDropEffects.Copy;
                // Note: Overlay stays visible throughout drag - shown in OnItemPointerPressed
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnScoreListDragLeave(object? sender, DragEventArgs e)
        {
            // Keep overlay visible during drag - it stays visible until drop completes
            // This provides continuous visual feedback about where to drop
            e.Handled = true;
        }

        private void OnScoreListDrop(object? sender, DragEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || _draggedItem == null)
                return;

            // Add to SHOULD collection (score columns)
            DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to score columns");
            vm.AddToShouldCommand.Execute(_draggedItem);

            // Overlay will be hidden by RemoveDragAdorner in OnPointerReleasedManualDrag
            e.Handled = true;
        }

        private void OnOrTrayDragOver(object? sender, DragEventArgs e)
        {
            if (_draggedItem != null)
            {
                e.DragEffects = DragDropEffects.Copy;

                // Highlight the OR tray
                if (sender is Border tray)
                {
                    tray.BorderThickness = new Avalonia.Thickness(2);
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnOrTrayDragLeave(object? sender, DragEventArgs e)
        {
            // Reset tray appearance
            if (sender is Border tray)
            {
                tray.BorderThickness = new Avalonia.Thickness(0, 0, 0, 2);
            }
            e.Handled = true;
        }

        private void OnOrTrayDrop(object? sender, DragEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || _draggedItem == null)
                return;

            // Reset tray appearance
            if (sender is Border tray)
            {
                tray.BorderThickness = new Avalonia.Thickness(0, 0, 0, 2);
            }

            // Add to OR tray
            DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to OR tray");
            vm.AddToOrTrayCommand.Execute(_draggedItem);

            e.Handled = true;
        }

        private void OnAndTrayDragOver(object? sender, DragEventArgs e)
        {
            if (_draggedItem != null)
            {
                e.DragEffects = DragDropEffects.Copy;

                // Highlight the AND tray
                if (sender is Border tray)
                {
                    tray.BorderThickness = new Avalonia.Thickness(2);
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnAndTrayDragLeave(object? sender, DragEventArgs e)
        {
            // Reset tray appearance
            if (sender is Border tray)
            {
                tray.BorderThickness = new Avalonia.Thickness(0, 0, 0, 2);
            }
            e.Handled = true;
        }

        private void OnAndTrayDrop(object? sender, DragEventArgs e)
        {
            var vm = DataContext as ViewModels.FilterTabs.VisualBuilderTabViewModel;
            if (vm == null || _draggedItem == null)
                return;

            // Reset tray appearance
            if (sender is Border tray)
            {
                tray.BorderThickness = new Avalonia.Thickness(0, 0, 0, 2);
            }

            // Add to AND tray
            DebugLogger.Log("ConfigureScoreTab", $"Adding {_draggedItem.Name} to AND tray");
            vm.AddToAndTrayCommand.Execute(_draggedItem);

            e.Handled = true;
        }

        #endregion

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Detach global pointer handlers
            if (_topLevel != null)
            {
                _topLevel.PointerMoved -= OnPointerMovedManualDrag;
                _topLevel.PointerReleased -= OnPointerReleasedManualDrag;
            }

            // Ensure cleanup
            RemoveDragAdorner();
            base.OnDetachedFromVisualTree(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
