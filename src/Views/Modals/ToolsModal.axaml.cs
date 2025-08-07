using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Oracle.Helpers;

namespace Oracle.Views.Modals
{
    public partial class ToolsModal : UserControl
    {
        public ToolsModal()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnAnalyzerClick(object? sender, RoutedEventArgs e)
        {
            // Get the parent modal container
            var parent = this.Parent;
            while (parent != null && !(parent is BalatroMainMenu))
            {
                parent = parent.Parent;
            }
            
            if (parent is BalatroMainMenu mainMenu)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show analyzer modal using ModalHelper extension
                ModalHelper.ShowAnalyzerModal(mainMenu);
            }
        }
        
        private void OnWordListsClick(object? sender, RoutedEventArgs e)
        {
            // Get the parent modal container
            var parent = this.Parent;
            while (parent != null && !(parent is BalatroMainMenu))
            {
                parent = parent.Parent;
            }
            
            if (parent is BalatroMainMenu mainMenu)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show word lists modal using ModalHelper extension
                ModalHelper.ShowWordListsModal(mainMenu);
            }
        }
    }
}