using System;
using Avalonia;
using Avalonia.Controls;
using BalatroSeedOracle.Components.Widgets;
using BalatroSeedOracle.Models;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.ViewModels.Widgets
{
    /// <summary>
    /// Adapter that wraps SearchWidgetViewModel to work with the new widget system
    /// </summary>
    public class SearchWidgetAdapter : WidgetViewModel
    {
        private readonly SearchWidgetViewModel _searchWidget;
        private readonly SearchWidget _searchControl;

        public SearchWidgetAdapter(
            SearchWidgetViewModel searchWidget,
            IWidgetLayoutService layoutService,
            IDockingService dockingService) : base(layoutService, dockingService)
        {
            _searchWidget = searchWidget ?? throw new ArgumentNullException(nameof(searchWidget));
            _searchControl = new SearchWidget { DataContext = searchWidget };

            // Set widget properties from SearchWidget
            Title = _searchWidget.FilterName ?? "Search";
            IconResource = "🔍";
            Size = new Size(350, 450);
            
            // Subscribe to search widget changes
            _searchWidget.PropertyChanged += OnSearchWidgetPropertyChanged;
        }

        private void OnSearchWidgetPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SearchWidgetViewModel.FilterName):
                    Title = _searchWidget.FilterName ?? "Search";
                    break;
            }
        }

        public override UserControl GetContentView()
        {
            return _searchControl;
        }

        public override async System.Threading.Tasks.Task<object?> SaveStateAsync()
        {
            // Save search widget specific state
            return new
            {
                SearchInstanceId = _searchWidget.SearchInstanceId,
                Position = Position,
                Size = Size,
                DockPosition = DockPosition,
                State = State,
                BaseState = await base.SaveStateAsync()
            };
        }

        public override async System.Threading.Tasks.Task LoadStateAsync(object? state)
        {
            await base.LoadStateAsync(state);
            // Additional search widget state loading if needed
        }

        protected override async System.Threading.Tasks.Task OnCloseAsync()
        {
            // Cleanup search widget
            if (_searchWidget is IDisposable disposable)
                disposable.Dispose();
            
            await base.OnCloseAsync();
        }
    }
}