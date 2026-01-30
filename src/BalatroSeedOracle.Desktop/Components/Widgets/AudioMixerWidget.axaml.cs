using Avalonia;
using BalatroSeedOracle.Components;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Desktop.Components.Widgets
{
    public partial class AudioMixerWidget : BaseWidgetControl
    {
        public AudioMixerWidget()
        {
            InitializeComponent();
        }

        protected override void OnWidgetAttached()
        {
            base.OnWidgetAttached();

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnAttached(this);
            }
        }

        protected override void OnWidgetDetached()
        {
            base.OnWidgetDetached();

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnDetached();
            }
        }

        // Event handlers inherited from BaseWidgetControl:
        // - OnMinimizedIconPressed
        // - OnMinimizedIconReleased
    }
}
