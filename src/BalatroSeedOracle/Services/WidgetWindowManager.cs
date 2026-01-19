using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages multiple widget windows with proper lifecycle and positioning
    /// Replaces the desktop canvas approach with proper Avalonia windows
    /// </summary>
    public class WidgetWindowManager
    {
        private readonly List<BaseWidgetViewModel> _activeWidgets = new();
        private readonly WidgetPositionService _positionService;

        public static WidgetWindowManager Instance { get; } = new();

        private WidgetWindowManager()
        {
            _positionService = Helpers.ServiceHelper.GetService<WidgetPositionService>() ?? new WidgetPositionService();
        }

        /// <summary>
        /// Create and show a new widget window
        /// </summary>
        public void CreateWidget<T>(T viewModel)
            where T : BaseWidgetViewModel
        {
            if (viewModel == null)
                return;

            // Set widget content if it's a control
            if (viewModel is Control control)
            {
                viewModel.WidgetContent = control;
            }

            // Create the window
            var window = new Windows.WidgetWindow();
            window.DataContext = viewModel;
            viewModel.WidgetWindow = window;

            // Position using collision avoidance
            var (x, y) = _positionService.FindNextAvailablePosition(viewModel, viewModel.IsMinimized);
            window.Position = new PixelPoint((int)x, (int)y);

            // Register and show
            _positionService.RegisterWidget(viewModel);
            _activeWidgets.Add(viewModel);

            window.Show();

            DebugLogger.Log("WidgetWindowManager", $"Created widget: {viewModel.WidgetTitle}");
        }

        /// <summary>
        /// Close a specific widget window
        /// </summary>
        public void CloseWidget(BaseWidgetViewModel viewModel)
        {
            if (viewModel == null)
                return;

            // Unregister and cleanup
            _positionService.UnregisterWidget(viewModel);
            _activeWidgets.Remove(viewModel);

            if (viewModel.WidgetWindow != null)
            {
                viewModel.WidgetWindow.Close();
                viewModel.WidgetWindow = null;
            }

            DebugLogger.Log("WidgetWindowManager", $"Closed widget: {viewModel.WidgetTitle}");
        }

        /// <summary>
        /// Close all widget windows
        /// </summary>
        public void CloseAllWidgets()
        {
            var widgetsToClose = _activeWidgets.ToList();
            foreach (var widget in widgetsToClose)
            {
                CloseWidget(widget);
            }

            DebugLogger.Log("WidgetWindowManager", "Closed all widgets");
        }

        /// <summary>
        /// Get all active widgets
        /// </summary>
        public IReadOnlyList<BaseWidgetViewModel> GetActiveWidgets()
        {
            return _activeWidgets.AsReadOnly();
        }

        /// <summary>
        /// Find widget by title
        /// </summary>
        public BaseWidgetViewModel? FindWidget(string title)
        {
            return _activeWidgets.FirstOrDefault(w => w.WidgetTitle == title);
        }

        /// <summary>
        /// Bring widget to front
        /// </summary>
        public void BringToFront(BaseWidgetViewModel viewModel)
        {
            if (viewModel?.WidgetWindow != null)
            {
                viewModel.BringWidgetToFrontCommand?.Execute(null);
            }
        }

        /// <summary>
        /// Minimize all widgets
        /// </summary>
        public void MinimizeAll()
        {
            foreach (var widget in _activeWidgets)
            {
                widget.MinimizeCommand?.Execute(null);
            }
        }

        /// <summary>
        /// Expand all widgets
        /// </summary>
        public void ExpandAll()
        {
            foreach (var widget in _activeWidgets)
            {
                widget.ExpandCommand?.Execute(null);
            }
        }

        /// <summary>
        /// Arrange widgets in a grid pattern
        /// </summary>
        public void ArrangeInGrid()
        {
            const double spacing = 120.0;
            const double startX = 50.0;
            const double startY = 50.0;
            const int columns = 4;

            for (int i = 0; i < _activeWidgets.Count; i++)
            {
                var widget = _activeWidgets[i];
                if (widget.WidgetWindow != null)
                {
                    var row = i / columns;
                    var col = i % columns;

                    var x = startX + (col * spacing);
                    var y = startY + (row * spacing);

                    widget.WidgetWindow.Position = new PixelPoint((int)x, (int)y);
                }
            }
        }

        /// <summary>
        /// Arrange widgets along screen edges
        /// </summary>
        public void ArrangeAlongEdges()
        {
            var screenBounds = GetScreenBounds();
            var widgets = _activeWidgets.ToList();

            // Distribute widgets along edges
            var perEdge = Math.Max(1, widgets.Count / 4);

            for (int i = 0; i < widgets.Count; i++)
            {
                var widget = widgets[i];
                if (widget.WidgetWindow == null)
                    continue;

                var edge = (SnapEdge)((i / perEdge) % 4);
                var position = GetEdgePosition(edge, screenBounds, i % perEdge, perEdge);

                widget.WidgetWindow.Position = position;
            }
        }

        /// <summary>
        /// Show all widget windows
        /// </summary>
        public void ShowAllWidgets()
        {
            foreach (var widget in _activeWidgets)
            {
                if (widget.WidgetWindow != null && !widget.WidgetWindow.IsVisible)
                {
                    widget.WidgetWindow.Show();
                }
            }

            DebugLogger.Log("WidgetWindowManager", $"Showed all {_activeWidgets.Count} widgets");
        }

        /// <summary>
        /// Hide all widget windows
        /// </summary>
        public void HideAllWidgets()
        {
            foreach (var widget in _activeWidgets)
            {
                if (widget.WidgetWindow != null && widget.WidgetWindow.IsVisible)
                {
                    widget.WidgetWindow.Hide();
                }
            }

            DebugLogger.Log("WidgetWindowManager", $"Hidden all {_activeWidgets.Count} widgets");
        }

        private Rect GetScreenBounds()
        {
            // For now, use a reasonable default screen size
            // TODO: Get actual screen bounds when needed
            return new Rect(0, 0, 1920, 1080);
        }

        private PixelPoint GetEdgePosition(SnapEdge edge, Rect screenBounds, int index, int perEdge)
        {
            var spacing = screenBounds.Height / (perEdge + 1);

            return edge switch
            {
                SnapEdge.Left => new PixelPoint(20, (int)(screenBounds.Top + spacing * (index + 1))),
                SnapEdge.Right => new PixelPoint(
                    (int)(screenBounds.Right - 120),
                    (int)(screenBounds.Top + spacing * (index + 1))
                ),
                SnapEdge.Top => new PixelPoint((int)(screenBounds.Left + spacing * (index + 1)), 20),
                SnapEdge.Bottom => new PixelPoint(
                    (int)(screenBounds.Left + spacing * (index + 1)),
                    (int)(screenBounds.Bottom - 120)
                ),
                _ => new PixelPoint(50, 50),
            };
        }

        private enum SnapEdge
        {
            Left,
            Right,
            Top,
            Bottom,
        }
    }
}
