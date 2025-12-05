using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using BalatroSeedOracle.Models;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Core;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Service implementation for widget docking functionality
    /// </summary>
    public class DockingService : IDockingService
    {
        private IReadOnlyCollection<DockZone>? _currentDockZones;
        private DockZone? _highlightedZone;

        /// <summary>
        /// Event fired when dock zones should be shown
        /// </summary>
        public event EventHandler<DockZonesEventArgs>? DockZonesRequested;

        /// <summary>
        /// Event fired when dock zones should be hidden
        /// </summary>
        public event EventHandler<EventArgs>? DockZonesHidden;

        /// <summary>
        /// Event fired when a widget is docked
        /// </summary>
        public event EventHandler<WidgetDockedEventArgs>? WidgetDocked;

        /// <summary>
        /// Create dock zones for the current container size
        /// </summary>
        /// <param name="containerSize">Container size</param>
        /// <returns>Collection of dock zones</returns>
        public IReadOnlyCollection<DockZone> CreateDockZones(Size containerSize)
        {
            var zones = new List<DockZone>();
            var margin = 50; // Zone activation margin
            var quarterWidth = containerSize.Width / 2;
            var quarterHeight = containerSize.Height / 2;

            // Left full height
            zones.Add(new DockZone
            {
                Position = DockPosition.LeftFull,
                Bounds = new Rect(0, 0, margin, containerSize.Height),
                DisplayText = "DOCK LEFT"
            });

            // Right full height
            zones.Add(new DockZone
            {
                Position = DockPosition.RightFull,
                Bounds = new Rect(containerSize.Width - margin, 0, margin, containerSize.Height),
                DisplayText = "DOCK RIGHT"
            });

            // Top left quarter
            zones.Add(new DockZone
            {
                Position = DockPosition.TopLeft,
                Bounds = new Rect(0, 0, quarterWidth, quarterHeight),
                DisplayText = "DOCK TOP LEFT"
            });

            // Top right quarter
            zones.Add(new DockZone
            {
                Position = DockPosition.TopRight,
                Bounds = new Rect(quarterWidth, 0, quarterWidth, quarterHeight),
                DisplayText = "DOCK TOP RIGHT"
            });

            // Bottom left quarter
            zones.Add(new DockZone
            {
                Position = DockPosition.BottomLeft,
                Bounds = new Rect(0, quarterHeight, quarterWidth, quarterHeight),
                DisplayText = "DOCK BOTTOM LEFT"
            });

            // Bottom right quarter
            zones.Add(new DockZone
            {
                Position = DockPosition.BottomRight,
                Bounds = new Rect(quarterWidth, quarterHeight, quarterWidth, quarterHeight),
                DisplayText = "DOCK BOTTOM RIGHT"
            });

            return zones.AsReadOnly();
        }

        /// <summary>
        /// Find dock zone at specified point
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="dockZones">Available dock zones</param>
        /// <returns>Dock zone at point or null</returns>
        public DockZone? FindDockZoneAtPoint(Point point, IEnumerable<DockZone> dockZones)
        {
            return dockZones.FirstOrDefault(zone => zone.IsActive && zone.ContainsPoint(point));
        }

        /// <summary>
        /// Calculate docked widget bounds
        /// </summary>
        /// <param name="position">Dock position</param>
        /// <param name="containerSize">Container size</param>
        /// <returns>Widget bounds when docked</returns>
        public Rect CalculateDockedBounds(DockPosition position, Size containerSize)
        {
            var halfWidth = containerSize.Width / 2;
            var halfHeight = containerSize.Height / 2;
            var margin = 4; // Small margin from container edges

            return position switch
            {
                DockPosition.LeftFull => new Rect(margin, margin, halfWidth - margin, containerSize.Height - 2 * margin),
                DockPosition.RightFull => new Rect(halfWidth, margin, halfWidth - margin, containerSize.Height - 2 * margin),
                DockPosition.TopLeft => new Rect(margin, margin, halfWidth - margin, halfHeight - margin),
                DockPosition.TopRight => new Rect(halfWidth, margin, halfWidth - margin, halfHeight - margin),
                DockPosition.BottomLeft => new Rect(margin, halfHeight, halfWidth - margin, halfHeight - margin),
                DockPosition.BottomRight => new Rect(halfWidth, halfHeight, halfWidth - margin, halfHeight - margin),
                _ => new Rect(0, 0, containerSize.Width, containerSize.Height)
            };
        }

        /// <summary>
        /// Start drag operation and show dock zones
        /// </summary>
        /// <param name="widget">Widget being dragged</param>
        /// <param name="containerSize">Container size</param>
        public void StartDockingOperation(IWidget widget, Size containerSize)
        {
            _currentDockZones = CreateDockZones(containerSize);
            
            // Activate all zones
            foreach (var zone in _currentDockZones)
            {
                zone.Activate();
            }

            DockZonesRequested?.Invoke(this, new DockZonesEventArgs(_currentDockZones));
        }

        /// <summary>
        /// Update drag operation with current mouse position
        /// </summary>
        /// <param name="mousePosition">Current mouse position</param>
        public void UpdateDragPosition(Point mousePosition)
        {
            if (_currentDockZones == null)
                return;

            // Clear previous highlight
            if (_highlightedZone != null)
            {
                _highlightedZone.ClearHighlight();
                _highlightedZone = null;
            }

            // Find and highlight current zone
            var currentZone = FindDockZoneAtPoint(mousePosition, _currentDockZones);
            if (currentZone != null)
            {
                currentZone.Highlight();
                _highlightedZone = currentZone;
            }
        }

        /// <summary>
        /// End drag operation and dock widget if in valid zone
        /// </summary>
        /// <param name="widget">Widget being dragged</param>
        /// <param name="dropPosition">Drop position</param>
        /// <returns>True if widget was docked, false otherwise</returns>
        public bool EndDockingOperation(IWidget widget, Point dropPosition)
        {
            var docked = false;
            
            if (_currentDockZones != null)
            {
                var targetZone = FindDockZoneAtPoint(dropPosition, _currentDockZones);
                if (targetZone != null)
                {
                    // Dock the widget
                    widget.DockPosition = targetZone.Position;
                    var bounds = CalculateDockedBounds(targetZone.Position, 
                        new Size(targetZone.Bounds.Width, targetZone.Bounds.Height));
                    
                    WidgetDocked?.Invoke(this, new WidgetDockedEventArgs(widget, targetZone.Position, bounds));
                    docked = true;
                }

                // Deactivate all zones
                foreach (var zone in _currentDockZones)
                {
                    zone.Deactivate();
                }
            }

            _currentDockZones = null;
            _highlightedZone = null;
            DockZonesHidden?.Invoke(this, EventArgs.Empty);

            return docked;
        }

        /// <summary>
        /// Undock a widget and return to grid layout
        /// </summary>
        /// <param name="widget">Widget to undock</param>
        public void UndockWidget(IWidget widget)
        {
            if (widget.IsDocked)
            {
                widget.DockPosition = DockPosition.None;
                // Note: Position would be handled by the layout service
            }
        }

        /// <summary>
        /// Check if a dock position is available
        /// </summary>
        /// <param name="position">Dock position to check</param>
        /// <param name="dockedWidgets">Currently docked widgets</param>
        /// <returns>True if position is available</returns>
        public bool IsDockPositionAvailable(DockPosition position, IEnumerable<IWidget> dockedWidgets)
        {
            if (position == DockPosition.None)
                return true;

            return !dockedWidgets.Any(w => w.IsDocked && w.DockPosition == position);
        }
    }
}