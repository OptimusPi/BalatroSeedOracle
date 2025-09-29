using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
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
            
            // Setup JSON editor after initialization
            _jsonEditor = this.FindControl<TextEditor>("JsonEditor");
            if (_jsonEditor != null)
            {
                _jsonEditor.TextArea.TextEntering += OnTextEntering;
                _jsonEditor.TextArea.TextEntered += OnTextEntered;
            }
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
        
        private void OnTextEntering(object? sender, TextInputEventArgs e)
        {
            // Simple autocomplete trigger - no complex window management
        }
        
        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (_jsonEditor?.TextArea == null) return;
            
            // Show autocomplete on quote or after colon
            if (e.Text == "\"" || e.Text == ":")
            {
                ShowJsonCompletions();
            }
        }
        
        private void ShowJsonCompletions()
        {
            if (_jsonEditor?.TextArea == null) return;
            
            var completionWindow = new CompletionWindow(_jsonEditor.TextArea);
            var data = completionWindow.CompletionList.CompletionData;
            
            // Add JSON property completions
            data.Add(new JsonCompletionData("type", "\"type\": \"Joker\""));
            data.Add(new JsonCompletionData("Value", "\"Value\": \"Blueprint\""));
            data.Add(new JsonCompletionData("Edition", "\"Edition\": \"Negative\""));
            data.Add(new JsonCompletionData("antes", "\"antes\": [1, 2, 3, 4, 5]"));
            data.Add(new JsonCompletionData("must", "\"must\": []"));
            data.Add(new JsonCompletionData("should", "\"should\": []"));
            data.Add(new JsonCompletionData("mustNot", "\"mustNot\": []"));
            data.Add(new JsonCompletionData("deck", "\"deck\": \"Red\""));
            data.Add(new JsonCompletionData("stake", "\"stake\": \"White\""));
            
            // Add common types
            data.Add(new JsonCompletionData("Joker", "\"Joker\""));
            data.Add(new JsonCompletionData("SoulJoker", "\"SoulJoker\""));
            data.Add(new JsonCompletionData("StandardCard", "\"StandardCard\""));
            data.Add(new JsonCompletionData("Voucher", "\"Voucher\""));
            data.Add(new JsonCompletionData("TarotCard", "\"TarotCard\""));
            
            completionWindow.Show();
            completionWindow.Closed += (o, args) => completionWindow = null;
        }
    }
    
    public class JsonCompletionData : ICompletionData
    {
        public JsonCompletionData(string text, string content)
        {
            Text = text;
            Content = content;
        }
        
        public string Text { get; }
        public object Content { get; }
        public object Description => $"Insert {Text}";
        public double Priority => 1.0;
        public Avalonia.Media.IImage? Image => null;
        
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Content.ToString());
        }
    }
}