using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels.Widgets
{
    /// <summary>
    /// ViewModel for managing collection of widgets and layout
    /// </summary>
    public partial class WidgetContainerViewModel : ObservableObject
    {
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly IWidgetLayoutService _layoutService;
        private readonly IDockingService _dockingService;
        private readonly WidgetFactory _widgetFactory;

        /// <summary>
        /// Collection of widgets in the container
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<IWidget> _widgets = new();

        /// <summary>
        /// Currently selected widget
        /// </summary>
        [ObservableProperty]
        private IWidget? _selectedWidget = null;

        /// <summary>
        /// Whether a drag operation is in progress
        /// </summary>
        [ObservableProperty]
        private bool _isDragging = false;

        /// <summary>
        /// Whether dock zones should be shown
        /// </summary>
        [ObservableProperty]
        private bool _showDockZones = false;

        /// <summary>
        /// Collection of current dock zones
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DockZone> _dockZones = new();

        /// <summary>
        /// Container size for layout calculations
        /// </summary>
        [ObservableProperty]
        private Size _containerSize = new Size(1200, 800);

        /// <summary>
        /// Whether widgets are hidden (for legacy compatibility)
        /// </summary>
        [ObservableProperty]
        private bool _areWidgetsHidden = false;

        public WidgetContainerViewModel(
            IWidgetRegistry widgetRegistry,
            IWidgetLayoutService layoutService,
            IDockingService dockingService,
            WidgetFactory widgetFactory)
        {
            _widgetRegistry = widgetRegistry ?? throw new ArgumentNullException(nameof(widgetRegistry));
            _layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));
            _dockingService = dockingService ?? throw new ArgumentNullException(nameof(dockingService));
            _widgetFactory = widgetFactory ?? throw new ArgumentNullException(nameof(widgetFactory));

            // Subscribe to docking service events
            _dockingService.DockZonesRequested += OnDockZonesRequested;
            _dockingService.DockZonesHidden += OnDockZonesHidden;
            _dockingService.WidgetDocked += OnWidgetDocked;
            
            // Register built-in widgets
            _widgetFactory.RegisterBuiltInWidgets();
        }

        /// <summary>
        /// Create a new widget of the specified type
        /// </summary>
        [RelayCommand]
        public async Task CreateWidgetAsync(string widgetTypeId)
        {
            try
            {
                var widget = _widgetFactory.CreateWidget(widgetTypeId);
                if (widget != null)
                {
                    // Set default position
                    var occupiedPositions = Widgets
                        .Where(w => w.State == WidgetState.Minimized)
                        .Select(w => w.Position);
                    var defaultPosition = _layoutService.GetNextDefaultPosition(occupiedPositions);
                    var pixelPosition = _layoutService.CalculatePixelPosition(defaultPosition);
                    
                    widget.Position = pixelPosition;
                    
                    // Subscribe to widget events
                    widget.StateChanged += OnWidgetStateChanged;
                    widget.CloseRequested += OnWidgetCloseRequested;
                    
                    Widgets.Add(widget);
                }
            }
            catch (Exception ex)
            {
                // Log error (would use actual logging in real implementation)
                Console.WriteLine($"Failed to create widget {widgetTypeId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a widget from the container
        /// </summary>
        [RelayCommand]
        public async Task RemoveWidgetAsync(IWidget widget)
        {
            if (widget == null) return;

            try
            {
                // Unsubscribe from events
                widget.StateChanged -= OnWidgetStateChanged;
                widget.CloseRequested -= OnWidgetCloseRequested;
                
                // Save state if needed
                await widget.SaveStateAsync();
                
                Widgets.Remove(widget);
                
                if (SelectedWidget == widget)
                    SelectedWidget = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove widget {widget.Id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Select and focus a widget
        /// </summary>
        [RelayCommand]
        public void SelectWidget(IWidget widget)
        {
            SelectedWidget = widget;
        }

        /// <summary>
        /// Start drag operation for a widget
        /// </summary>
        [RelayCommand]
        public void StartDrag(IWidget widget)
        {
            if (widget == null) return;
            
            IsDragging = true;
            SelectedWidget = widget;
            
            // Start docking operation if widget is open
            if (widget.State == WidgetState.Open)
            {
                _dockingService.StartDockingOperation(widget, ContainerSize);
            }
        }

        /// <summary>
        /// End drag operation at specified position
        /// </summary>
        [RelayCommand]
        public void EndDrag(Point position)
        {
            if (!IsDragging || SelectedWidget == null) return;
            
            try
            {
                if (SelectedWidget.State == WidgetState.Minimized)
                {
                    // Handle grid positioning for minimized widgets
                    var gridPosition = _layoutService.CalculateGridPosition(position);
                    var occupiedPositions = Widgets
                        .Where(w => w != SelectedWidget && w.State == WidgetState.Minimized)
                        .Select(w => _layoutService.CalculateGridPosition(w.Position));
                    
                    var availablePosition = _layoutService.FindNearestAvailablePosition(gridPosition, occupiedPositions);
                    var pixelPosition = _layoutService.CalculatePixelPosition(availablePosition);
                    
                    SelectedWidget.Position = pixelPosition;
                }
                else if (SelectedWidget.State == WidgetState.Open)
                {
                    // Handle docking for open widgets
                    var docked = _dockingService.EndDockingOperation(SelectedWidget, position);
                    if (!docked)
                    {
                        // If not docked, position normally
                        SelectedWidget.Position = position;
                    }
                }
            }
            finally
            {
                IsDragging = false;
                SelectedWidget = null;
            }
        }

        /// <summary>
        /// Update container size for layout calculations
        /// </summary>
        public void UpdateContainerSize(Size newSize)
        {
            ContainerSize = newSize;
            _layoutService.NotifyLayoutChanged(newSize);
        }

        private void OnWidgetStateChanged(object? sender, WidgetStateChangedEventArgs e)
        {
            // Handle widget state changes if needed
            // For example, update positioning when widget changes from open to minimized
        }

        private void OnWidgetCloseRequested(object? sender, EventArgs e)
        {
            if (sender is IWidget widget)
            {
                _ = RemoveWidgetAsync(widget);
            }
        }

        private void OnDockZonesRequested(object? sender, DockZonesEventArgs e)
        {
            DockZones.Clear();
            foreach (var zone in e.DockZones)
            {
                DockZones.Add(zone);
            }
            ShowDockZones = true;
        }

        private void OnDockZonesHidden(object? sender, EventArgs e)
        {
            ShowDockZones = false;
            DockZones.Clear();
        }

        private void OnWidgetDocked(object? sender, WidgetDockedEventArgs e)
        {
            // Update widget positioning based on docking
            var widget = Widgets.FirstOrDefault(w => w.Id == e.Widget.Id);
            if (widget != null)
            {
                widget.IsDocked = true;
                widget.DockPosition = e.Position;
                widget.Position = new Point(e.Bounds.X, e.Bounds.Y);
                widget.Size = new Size(e.Bounds.Width, e.Bounds.Height);
            }
        }
    }
}