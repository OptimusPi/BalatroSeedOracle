using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    public partial class AudioMixerWidget : UserControl
    {
        public AudioMixerWidget()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnAttached(this);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.OnDetached();
            }
        }

        private void OnMinimizedIconPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                vm.BringToFront();
            }
        }

        private void OnMinimizedIconReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (DataContext is AudioMixerWidgetViewModel vm)
            {
                if (vm.IsMinimized)
                {
                    vm.ExpandCommand.Execute(null);
                }
                else
                {
                    vm.MinimizeCommand.Execute(null);
                }
            }
        }
    }
}
