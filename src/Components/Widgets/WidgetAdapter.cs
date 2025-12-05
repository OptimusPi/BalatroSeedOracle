using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components.Widgets
{
    /// <summary>
    /// Adapter that wraps existing BaseWidgetControl instances to implement IWidget interface
    /// This allows seamless integration of existing widgets with the new widget system
    /// </summary>
    public class WidgetAdapter : IWidget
    {
        private readonly BaseWidgetControl _control;
        private readonly BaseWidgetViewModel _viewModel;
        private string _id;
        private WidgetState _state;
        private DockPosition _dockPosition = DockPosition.None;

        public WidgetAdapter(BaseWidgetControl control, BaseWidgetViewModel viewModel)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            _id = Guid.NewGuid().ToString();
            _state = _viewModel.IsMinimized ? WidgetState.Minimized : WidgetState.Open;

            // Subscribe to view model property changes
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BaseWidgetViewModel.IsMinimized):
                    var newState = _viewModel.IsMinimized ? WidgetState.Minimized : WidgetState.Open;
                    if (newState != _state)
                    {
                        var oldState = _state;
                        _state = newState;
                        StateChanged?.Invoke(this, new WidgetStateChangedEventArgs(Id, oldState, newState));
                    }
                    break;
            }
        }

        #region IWidget Implementation

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Title
        {
            get => _viewModel.WidgetTitle;
            set => _viewModel.WidgetTitle = value;
        }

        public string IconResource
        {
            get => _viewModel.WidgetIcon;
            set => _viewModel.WidgetIcon = value;
        }

        public WidgetState State => _state;

        public int NotificationCount
        {
            get => _viewModel.ShowNotificationBadge ? 
                   (int.TryParse(_viewModel.NotificationText.Replace("k", "000").Replace("K", "000").Replace("M", "000000"), out var count) ? count : 0) 
                   : 0;
            set => _viewModel.SetNotification(value);
        }

        public double ProgressValue { get; set; } = 0.0;

        public bool ShowCloseButton { get; set; } = true;

        public bool ShowPopOutButton { get; set; } = false;

        public Point Position
        {
            get => new Point(_viewModel.PositionX, _viewModel.PositionY);
            set
            {
                _viewModel.PositionX = value.X;
                _viewModel.PositionY = value.Y;
            }
        }

        public Size Size
        {
            get => new Size(_viewModel.Width, _viewModel.Height);
            set
            {
                _viewModel.Width = value.Width;
                _viewModel.Height = value.Height;
            }
        }

        public bool IsDocked
        {
            get => _dockPosition != DockPosition.None;
            set
            {
                if (!value)
                    _dockPosition = DockPosition.None;
                // If setting to true, DockPosition should be set separately
            }
        }

        public DockPosition DockPosition
        {
            get => _dockPosition;
            set => _dockPosition = value;
        }

        public object? PersistedState { get; set; }

        public event EventHandler<WidgetStateChangedEventArgs>? StateChanged;
        public event EventHandler<EventArgs>? CloseRequested;

        public async Task OpenAsync()
        {
            _viewModel.ExpandCommand.Execute(null);
            await Task.CompletedTask;
        }

        public async Task MinimizeAsync()
        {
            _viewModel.MinimizeCommand.Execute(null);
            await Task.CompletedTask;
        }

        public async Task CloseAsync()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            _viewModel.CloseCommand.Execute(null);
            await Task.CompletedTask;
        }

        public UserControl GetContentView()
        {
            return _control;
        }

        public void UpdateNotifications(int count)
        {
            NotificationCount = count;
        }

        public void UpdateProgress(double value)
        {
            ProgressValue = Math.Clamp(value, 0.0, 1.0);
        }

        public async Task<object?> SaveStateAsync()
        {
            // Return the current view model state that should be persisted
            return new
            {
                Position = Position,
                Size = Size,
                DockPosition = DockPosition,
                IsMinimized = _viewModel.IsMinimized,
                // Add any additional widget-specific state here
                PersistedState
            };
        }

        public async Task LoadStateAsync(object? state)
        {
            if (state is not System.Text.Json.JsonElement element) 
                return;

            try
            {
                if (element.TryGetProperty("Position", out var positionElement) && positionElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (positionElement.TryGetProperty("X", out var x) && positionElement.TryGetProperty("Y", out var y))
                    {
                        Position = new Point(x.GetDouble(), y.GetDouble());
                    }
                }

                if (element.TryGetProperty("Size", out var sizeElement) && sizeElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (sizeElement.TryGetProperty("Width", out var width) && sizeElement.TryGetProperty("Height", out var height))
                    {
                        Size = new Size(width.GetDouble(), height.GetDouble());
                    }
                }

                if (element.TryGetProperty("DockPosition", out var dockElement))
                {
                    if (Enum.TryParse<DockPosition>(dockElement.GetString(), out var dockPos))
                    {
                        DockPosition = dockPos;
                    }
                }

                if (element.TryGetProperty("IsMinimized", out var minimizedElement))
                {
                    var isMinimized = minimizedElement.GetBoolean();
                    if (isMinimized != _viewModel.IsMinimized)
                    {
                        if (isMinimized)
                            await MinimizeAsync();
                        else
                            await OpenAsync();
                    }
                }

                if (element.TryGetProperty("PersistedState", out var persistedElement))
                {
                    PersistedState = persistedElement;
                }
            }
            catch (Exception)
            {
                // Ignore state loading errors and use defaults
            }

            await Task.CompletedTask;
        }

        #endregion

        #region IDisposable Pattern

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    if (_viewModel != null)
                        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}