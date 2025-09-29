using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
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
        private TextEditor? _jsonEditor;
        public JsonEditorTabViewModel? ViewModel => DataContext as JsonEditorTabViewModel;

        public JsonEditorTab()
        {
            InitializeComponent();
            
            // Note: DataContext will be set by parent, not via DI
            // DataContext = ServiceHelper.GetRequiredService<ViewModels.FilterTabs.JsonEditorTabViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // Get reference to the TextEditor
            _jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            
            // Set up two-way binding for TextEditor
            if (_jsonEditor != null)
            {
                // When ViewModel changes, update editor
                DataContextChanged += (s, e) =>
                {
                    if (ViewModel != null)
                    {
                        _jsonEditor.Text = ViewModel.JsonContent;
                        
                        // Subscribe to ViewModel property changes
                        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                    }
                };
                
                // When editor text changes, update ViewModel
                _jsonEditor.TextChanged += (s, e) =>
                {
                    if (ViewModel != null)
                    {
                        ViewModel.JsonContent = _jsonEditor.Text ?? "";
                    }
                };
            }
        }
        
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JsonEditorTabViewModel.JsonContent) && _jsonEditor != null && ViewModel != null)
            {
                // Only update if different to avoid infinite loop
                if (_jsonEditor.Text != ViewModel.JsonContent)
                {
                    _jsonEditor.Text = ViewModel.JsonContent;
                }
            }
        }
    }
}