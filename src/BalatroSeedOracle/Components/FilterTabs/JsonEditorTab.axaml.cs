using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// MVVM JSON Editor Tab - replaces JSON editing logic from original FiltersModal
    /// Minimal code-behind, all logic in JsonEditorTabViewModel
    /// </summary>
    public partial class JsonEditorTab : UserControl
    {
        private TextEditor? _jsonEditor;
        private FoldingManager? _foldingManager;
        private readonly BraceFoldingStrategy _foldingStrategy = new();

        public JsonEditorTabViewModel? ViewModel => DataContext as JsonEditorTabViewModel;

        public JsonEditorTab()
        {
            InitializeComponent();

            // Setup JSON editor after initialization
            _jsonEditor = JsonEditor;
            if (_jsonEditor != null)
            {
                _jsonEditor.TextArea.TextEntering += OnTextEntering;
                _jsonEditor.TextArea.TextEntered += OnTextEntered;
                _jsonEditor.TextArea.KeyDown += OnKeyDown;
            }

            if (ViewModel != null)
            {
                ViewModel.CopyToClipboardRequested += async (s, text) => await CopyToClipboardAsync(text);
            }
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                    DebugLogger.Log("JsonEditorTab", $"Copied to clipboard: {text}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JsonEditorTab", $"Failed to copy to clipboard: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get reference to the TextEditor
            _jsonEditor = JsonEditor;

            // Set up two-way binding for TextEditor
            if (_jsonEditor != null)
            {
                // Load custom dark mode syntax highlighting
                LoadCustomJsonSyntaxHighlighting();

                // Install code folding
                InstallCodeFolding();

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

                        // Update code folding when document changes
                        UpdateCodeFolding();
                    }
                };
            }
        }

        private void LoadCustomJsonSyntaxHighlighting()
        {
            if (_jsonEditor == null)
                return;

            try
            {
                // Load custom JSON dark mode syntax highlighting
                var xshdPath = Path.Combine(AppContext.BaseDirectory, "Resources", "JsonDark.xshd");

                if (File.Exists(xshdPath))
                {
                    using (var reader = new XmlTextReader(xshdPath))
                    {
                        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        _jsonEditor.SyntaxHighlighting = definition;
                    }
                    DebugLogger.Log("JsonEditorTab", "Custom JSON dark mode syntax highlighting loaded");
                }
                else
                {
                    // Fallback to default JSON highlighting with custom colors
                    DebugLogger.LogError("JsonEditorTab", $"JsonDark.xshd not found at {xshdPath}, using default");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JsonEditorTab", $"Failed to load custom syntax highlighting: {ex.Message}");
            }
        }

        private void InstallCodeFolding()
        {
            if (_jsonEditor?.TextArea == null)
                return;

            try
            {
                // Install folding manager
                _foldingManager = FoldingManager.Install(_jsonEditor.TextArea);

                // Initial folding update
                UpdateCodeFolding();

                DebugLogger.Log("JsonEditorTab", "Code folding installed successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("JsonEditorTab", $"Failed to install code folding: {ex.Message}");
            }
        }

        private void UpdateCodeFolding()
        {
            if (_foldingManager == null || _jsonEditor?.Document == null)
                return;

            try
            {
                // Update foldings using brace folding strategy (handles {} and [] for JSON)
                _foldingStrategy.UpdateFoldings(_foldingManager, _jsonEditor.Document);
            }
            catch (Exception ex)
            {
                // Silently ignore folding errors to avoid disrupting editing
                DebugLogger.LogError("JsonEditorTab", $"Error updating foldings: {ex.Message}");
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName == nameof(JsonEditorTabViewModel.JsonContent)
                && _jsonEditor != null
                && ViewModel != null
            )
            {
                // Only update if different to avoid infinite loop
                if (_jsonEditor.Text != ViewModel.JsonContent)
                {
                    _jsonEditor.Text = ViewModel.JsonContent;
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+Space triggers autocomplete
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowJsonCompletions();
                e.Handled = true;
            }
        }

        private void OnTextEntering(object? sender, TextInputEventArgs e)
        {
            // Simple autocomplete trigger - no complex window management
        }

        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (_jsonEditor?.TextArea == null)
                return;

            // Show autocomplete on quote or after colon
            if (e.Text == "\"" || e.Text == ":")
            {
                ShowJsonCompletions();
            }
        }

        private void ShowJsonCompletions()
        {
            if (_jsonEditor?.TextArea == null)
                return;

            // Get text before cursor for context-aware completions
            var offset = _jsonEditor.CaretOffset;
            var textBeforeCursor = _jsonEditor.Text.Substring(0, Math.Min(offset, _jsonEditor.Text.Length));

            // Get SMART context-aware completions
            var smartCompletions = JsonAutocompletionHelper.GetCompletionsForContext(textBeforeCursor);

            if (smartCompletions.Count == 0)
                return; // No completions available

            var completionWindow = new CompletionWindow(_jsonEditor.TextArea);

            // Find the start of the current word/token being typed
            // This ensures we replace what's already typed, not just insert
            var wordStart = offset;
            while (wordStart > 0)
            {
                var ch = _jsonEditor.Document.GetCharAt(wordStart - 1);
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-')
                    break;
                wordStart--;
            }

            // Set the completion segment to replace from wordStart to current position
            completionWindow.StartOffset = wordStart;
            completionWindow.EndOffset = offset;

            var data = completionWindow.CompletionList.CompletionData;

            // Add all smart completions
            foreach (var completion in smartCompletions)
            {
                data.Add(completion);
            }

            completionWindow.Show();
            completionWindow.Closed += (o, args) => completionWindow = null;
        }
    }
}
