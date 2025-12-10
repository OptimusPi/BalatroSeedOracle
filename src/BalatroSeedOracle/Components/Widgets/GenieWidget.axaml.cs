using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// GenieWidget - AI-powered filter generation widget
    /// Uses BaseWidgetControl for common functionality
    /// </summary>
    public partial class GenieWidget : BaseWidgetControl
    {
        public GenieWidgetViewModel? ViewModel { get; }

        public GenieWidget()
        {
            var searchManager = ServiceHelper.GetRequiredService<SearchManager>();
            var positionService = ServiceHelper.GetService<WidgetPositionService>();
            ViewModel = new GenieWidgetViewModel(searchManager, positionService);
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}
