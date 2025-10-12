using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// GenieWidget - AI-powered filter generation widget
    /// MVVM pattern - ALL business logic in ViewModel, drag handled by DraggableWidgetBehavior
    /// </summary>
    public partial class GenieWidget : UserControl
    {
        public GenieWidgetViewModel? ViewModel { get; }

        public GenieWidget()
        {
            // Check feature flag - hide widget if disabled
            if (!FeatureFlagsService.Instance.IsEnabled(FeatureFlagsService.GENIE_ENABLED))
            {
                IsVisible = false;
                return;
            }

            ViewModel = new GenieWidgetViewModel();
            DataContext = ViewModel;

            InitializeComponent();

            this.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDetachedFromVisualTree(object? sender, EventArgs e)
        {
            // Cleanup if needed
        }
    }
}
