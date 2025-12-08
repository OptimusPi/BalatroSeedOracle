using System;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels;

namespace BalatroSeedOracle.Components
{
    /// <summary>
    /// Transition Designer Widget - Design and test audio/visual transitions
    /// Agnostic, modular transition design interface
    /// </summary>
    public partial class TransitionDesignerWidget : BaseWidgetControl
    {
        public TransitionDesignerWidgetViewModel ViewModel { get; }

        public TransitionDesignerWidget()
        {
            InitializeComponent();

            // Get ViewModel from DI container
            ViewModel =
                ServiceHelper.GetService<TransitionDesignerWidgetViewModel>()
                ?? throw new InvalidOperationException(
                    "TransitionDesignerWidgetViewModel service not registered in DI container"
                );
            DataContext = ViewModel;
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
