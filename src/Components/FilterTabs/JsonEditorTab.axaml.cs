using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BalatroSeedOracle.ViewModels.FilterTabs;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM JSON Editor Tab - replaces JSON editing logic from original FiltersModal
    /// Minimal code-behind, all logic in JsonEditorTabViewModel
    /// </summary>
    public partial class JsonEditorTab : UserControl
    {
        public JsonEditorTabViewModel? ViewModel => DataContext as JsonEditorTabViewModel;

        public JsonEditorTab()
        {
            InitializeComponent();
            
            // Set up ViewModel
            DataContext = new JsonEditorTabViewModel();
            
            // Set up AvaloniaEdit binding (doesn't support direct XAML binding)
            if (ViewModel != null)
            {
                var editor = this.FindControl<AvaloniaEdit.TextEditor>("JsonEditor");
                if (editor != null)
                {
                    editor.Text = ViewModel.JsonContent;
                    ViewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ViewModel.JsonContent) && editor.Text != ViewModel.JsonContent)
                        {
                            editor.Text = ViewModel.JsonContent;
                        }
                    };
                    
                    editor.TextChanged += (s, e) =>
                    {
                        if (ViewModel.JsonContent != editor.Text)
                        {
                            ViewModel.JsonContent = editor.Text ?? "";
                        }
                    };
                }
            }
            
            DebugLogger.Log("JsonEditorTab", "MVVM JSON Editor Tab created with AvaloniaEdit binding");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}