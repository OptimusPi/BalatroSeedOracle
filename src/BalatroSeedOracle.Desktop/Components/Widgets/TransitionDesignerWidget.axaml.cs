using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Components;

namespace BalatroSeedOracle.Desktop.Components.Widgets
{
    /// <summary>
    /// Transition Designer Widget - Design and test audio/visual transitions
    /// Agnostic, modular transition design interface
    /// </summary>
    public partial class TransitionDesignerWidget : BaseWidgetControl
    {
        public TransitionDesignerWidget()
        {
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
