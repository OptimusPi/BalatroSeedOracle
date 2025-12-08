using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class FrequencyDebugWidget : BaseWidgetControl
    {
        public FrequencyDebugWidgetViewModel ViewModel { get; }

        public FrequencyDebugWidget()
        {
            InitializeComponent();

            ViewModel = new FrequencyDebugWidgetViewModel();
            DataContext = ViewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnWidgetAttached()
        {
            base.OnWidgetAttached();
            ViewModel.OnAttached(this);
        }

        protected override void OnWidgetDetached()
        {
            base.OnWidgetDetached();
            ViewModel.OnDetached();
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}
