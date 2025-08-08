using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
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
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            
            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show analyzer modal using ModalHelper extension
                mainMenu.ShowAnalyzerModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }
        
        private void OnWordListsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            
            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show word lists modal using ModalHelper extension
                mainMenu.ShowWordListsModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }
        
        private void OnCreditsClick(object? sender, RoutedEventArgs e)
        {
            // Find the main menu in the visual tree
            var mainMenu = this.FindAncestorOfType<BalatroMainMenu>();
            
            if (mainMenu != null)
            {
                // Hide current modal
                mainMenu.HideModalContent();
                
                // Show credits modal using ModalHelper extension
                mainMenu.ShowCreditsModal();
            }
            else
            {
                DebugLogger.LogError("ToolsModal", "Could not find BalatroMainMenu in visual tree");
            }
        }
    }
}