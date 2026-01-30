using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// JAML Editor Tab for filter editing.
    /// JAML (Joker Ante Markup Language) is a YAML-based format for Balatro filters.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class JamlEditorTab : UserControl
    {
        private FoldingManager? _foldingManager;
        private readonly BraceFoldingStrategy _foldingStrategy = new();
        private JamlErrorMarkerService? _errorMarkerService;
        private JamlHoverTooltipService? _hoverTooltipService;
        private JamlCodeSnippetService? _snippetService;

        public JamlEditorTabViewModel? ViewModel => DataContext as JamlEditorTabViewModel;

        public JamlEditorTab()
        {
            InitializeComponent();
            SetupEditor();
        }

        private void SetupEditor()
        {
            // Direct x:Name field access - no FindControl!
            // Load custom dark mode syntax highlighting (JAML uses YAML syntax)
            LoadCustomJamlSyntaxHighlighting();

            // Install code folding
            InstallCodeFolding();

            // When ViewModel changes, update editor
            DataContextChanged += (s, e) =>
            {
                if (ViewModel != null)
                {
                    JamlEditor.Text = ViewModel.JamlContent;

                    // Subscribe to ViewModel property changes
                    ViewModel.PropertyChanged += OnViewModelPropertyChanged;

                    // Subscribe to jump to error event
                    ViewModel.JumpToError += OnJumpToError;
                }
            };

            // When editor text changes, update ViewModel
            JamlEditor.TextChanged += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.JamlContent = JamlEditor.Text ?? "";

                    // Update code folding when document changes
                    UpdateCodeFolding();
                }
            };

            // Install autocomplete
            InstallAutocomplete();

            // Install editor enhancements
            InstallEditorEnhancements();
        }

        private void InstallEditorEnhancements()
        {
            if (JamlEditor?.TextArea == null)
                return;

            // Configure editor options
            JamlEditor.Options.IndentationSize = 2;
            JamlEditor.Options.ConvertTabsToSpaces = true;
            // Note: HighlightCurrentLine and EnableBracketMatching may not be available in this version of AvaloniaEdit
            // These features are handled by the TextArea.TextView rendering

            // Install error marker service
            _errorMarkerService = new JamlErrorMarkerService(JamlEditor);
            JamlEditor.TextArea.TextView.BackgroundRenderers.Add(_errorMarkerService);

            // Install hover tooltips
            _hoverTooltipService = new JamlHoverTooltipService(JamlEditor);
            _hoverTooltipService.Install();

            // Install code snippets
            _snippetService = new JamlCodeSnippetService(JamlEditor);

            // Handle Tab for snippet expansion
            JamlEditor.TextArea.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    if (_snippetService?.TryExpandSnippet() == true)
                    {
                        e.Handled = true;
                    }
                }
            };

            // Handle Ctrl+Click for go-to-definition
            JamlEditor.TextArea.TextView.PointerPressed += OnMouseDown;

            // Subscribe to text changes for validation
            JamlEditor.TextChanged += OnEditorTextChanged;
        }

        private void OnMouseDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (JamlEditor?.TextArea?.Caret == null || JamlEditor.Document == null)
                return;

            var point = e.GetCurrentPoint(JamlEditor);
            if (
                point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            )
            {
                // Use caret position instead of mouse position for simplicity
                var offset = JamlEditor.CaretOffset;
                GoToDefinition(offset);
            }
        }

        private void GoToDefinition(int offset)
        {
            if (JamlEditor?.Document == null)
                return;

            var line = JamlEditor.Document.GetLineByOffset(offset);
            var lineText = JamlEditor.Document.GetText(line.Offset, line.Length);
            var column = offset - line.Offset;

            // Check if we're on an anchor reference (*anchor_name)
            var anchorMatch = Regex.Match(
                lineText.Substring(
                    Math.Max(0, column - 20),
                    Math.Min(20, lineText.Length - Math.Max(0, column - 20))
                ),
                @"\*(\w+)"
            );

            if (anchorMatch.Success)
            {
                var anchorName = anchorMatch.Groups[1].Value;
                FindAnchorDefinition(anchorName);
            }
        }

        private void FindAnchorDefinition(string anchorName)
        {
            if (JamlEditor?.Document == null)
                return;

            var text = JamlEditor.Document.Text;
            var pattern = $@"(\w+)\s*:\s*&{Regex.Escape(anchorName)}\s+";
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);

            if (match.Success)
            {
                var lineNumber = JamlEditor.Document.GetLineByOffset(match.Index).LineNumber;
                var line = JamlEditor.Document.GetLineByNumber(lineNumber);
                JamlEditor.CaretOffset = line.Offset;
                JamlEditor.TextArea.Caret.BringCaretToView();
            }
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            // Validate and mark errors
            if (JamlEditor?.Document != null && ViewModel != null)
            {
                ValidateAndMarkErrors();
            }
        }

        private void ValidateAndMarkErrors()
        {
            if (_errorMarkerService is null || JamlEditor?.Document is null || ViewModel is null)
                return;

            _errorMarkerService.ClearErrors();

            try
            {
                var yamlContent = JamlEditor.Document.Text;
                if (string.IsNullOrWhiteSpace(yamlContent))
                    return;

                // Basic YAML validation
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .WithNamingConvention(
                        YamlDotNet
                            .Serialization
                            .NamingConventions
                            .CamelCaseNamingConvention
                            .Instance
                    )
                    .IgnoreUnmatchedProperties()
                    .Build();

                deserializer.Deserialize<object>(yamlContent);

                // Check for undefined anchor references
                CheckAnchorReferences(yamlContent);
            }
            catch (YamlDotNet.Core.YamlException yamlEx)
            {
                // Parse YAML error location
                var lineMatch = Regex.Match(yamlEx.Message, @"line (\d+)");
                if (
                    lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var lineNumber)
                )
                {
                    _errorMarkerService.AddError(
                        lineNumber,
                        0,
                        50,
                        yamlEx.Message,
                        JamlErrorMarkerService.ErrorSeverity.Error
                    );
                }
            }
            catch (Exception ex)
            {
                // General error - mark first line
                _errorMarkerService.AddError(
                    1,
                    0,
                    50,
                    ex.Message,
                    JamlErrorMarkerService.ErrorSeverity.Error
                );
            }

            _errorMarkerService.UpdateErrors();
        }

        private void CheckAnchorReferences(string yamlContent)
        {
            if (_errorMarkerService is null || JamlEditor?.Document is null)
                return;

            // Find all anchor references
            var referenceMatches = Regex.Matches(yamlContent, @"\*(\w+)");
            var definedAnchors = new System.Collections.Generic.HashSet<string>();

            // Find all anchor definitions
            var definitionMatches = Regex.Matches(yamlContent, @"&(\w+)");
            foreach (Match match in definitionMatches)
            {
                definedAnchors.Add(match.Groups[1].Value);
            }

            // Check references
            foreach (Match match in referenceMatches)
            {
                var anchorName = match.Groups[1].Value;
                if (!definedAnchors.Contains(anchorName))
                {
                    var lineNumber = JamlEditor.Document.GetLineByOffset(match.Index).LineNumber;
                    var column =
                        match.Index - JamlEditor.Document.GetLineByNumber(lineNumber).Offset;
                    _errorMarkerService.AddError(
                        lineNumber,
                        column,
                        column + anchorName.Length + 1,
                        $"Anchor '{anchorName}' is not defined",
                        JamlErrorMarkerService.ErrorSeverity.Warning
                    );
                }
            }
        }

        private void InstallAutocomplete()
        {
            if (JamlEditor?.TextArea == null)
                return;

            // Handle Ctrl+Space for autocomplete
            JamlEditor.TextArea.KeyDown += OnKeyDown;

            // Handle text input for auto-trigger
            JamlEditor.TextArea.TextEntered += OnTextEntered;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+Space triggers autocomplete
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowJamlCompletions();
                e.Handled = true;
            }
        }

        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (JamlEditor?.TextArea == null)
                return;

            // Show autocomplete on colon, dash (for lists), or asterisk (for anchor references)
            if (e.Text == ":" || e.Text == "-" || e.Text == "*")
            {
                ShowJamlCompletions();
            }
        }

        private void ShowJamlCompletions()
        {
            if (JamlEditor?.TextArea == null)
                return;

            // Get text before cursor for context-aware completions
            var offset = JamlEditor.CaretOffset;
            var textBeforeCursor = JamlEditor.Text.Substring(
                0,
                Math.Min(offset, JamlEditor.Text.Length)
            );

            // Get SMART context-aware completions
            var smartCompletions = JamlAutocompletionHelper.GetCompletionsForContext(
                textBeforeCursor
            );

            if (smartCompletions.Count == 0)
                return; // No completions available

            var completionWindow = new CompletionWindow(JamlEditor.TextArea);

            // Find the start of the current word/token being typed
            var wordStart = offset;
            while (wordStart > 0)
            {
                var ch = JamlEditor.Document.GetCharAt(wordStart - 1);
                if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-' && ch != '*')
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

        private void LoadCustomJamlSyntaxHighlighting()
        {
            if (JamlEditor == null)
                return;

            try
            {
                // Load custom JAML dark mode syntax highlighting (uses YAML highlighting since JAML is YAML-based)
                var xshdPath = Path.Combine(AppContext.BaseDirectory, "Resources", "JamlDark.xshd");

                if (!File.Exists(xshdPath))
                {
                    xshdPath = Path.Combine(AppContext.BaseDirectory, "Resources", "YamlDark.xshd");
                }

                if (File.Exists(xshdPath))
                {
                    using (var reader = new XmlTextReader(xshdPath))
                    {
                        var definition = HighlightingLoader.Load(
                            reader,
                            HighlightingManager.Instance
                        );
                        JamlEditor.SyntaxHighlighting = definition;
                    }
                    DebugLogger.Log(
                        "JamlEditorTab",
                        "Custom JAML dark mode syntax highlighting loaded"
                    );
                }
                else
                {
                    DebugLogger.LogError(
                        "JamlEditorTab",
                        $"JamlDark.xshd not found at {xshdPath}, using default"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "JamlEditorTab",
                    $"Failed to load custom syntax highlighting: {ex.Message}"
                );
            }
        }

        private void InstallCodeFolding()
        {
            if (JamlEditor?.TextArea == null)
                return;

            try
            {
                // Install folding manager
                _foldingManager = FoldingManager.Install(JamlEditor.TextArea);

                // Initial folding update
                UpdateCodeFolding();

                DebugLogger.Log("JamlEditorTab", "Code folding installed successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "JamlEditorTab",
                    $"Failed to install code folding: {ex.Message}"
                );
            }
        }

        private void UpdateCodeFolding()
        {
            if (_foldingManager == null || JamlEditor?.Document == null)
                return;

            try
            {
                // Update foldings using brace folding strategy (handles [] for JAML arrays)
                _foldingStrategy.UpdateFoldings(_foldingManager, JamlEditor.Document);
            }
            catch (Exception ex)
            {
                // Silently ignore folding errors to avoid disrupting editing
                DebugLogger.LogError("JamlEditorTab", $"Error updating foldings: {ex.Message}");
            }
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (
                e.PropertyName == nameof(JamlEditorTabViewModel.JamlContent)
                && JamlEditor != null
                && ViewModel != null
            )
            {
                // Only update if different to avoid infinite loop
                if (JamlEditor.Text != ViewModel.JamlContent)
                {
                    JamlEditor.Text = ViewModel.JamlContent;
                }
            }
        }

        private void OnJumpToError(int lineNumber, int column)
        {
            if (JamlEditor?.Document == null)
                return;

            try
            {
                var line = JamlEditor.Document.GetLineByNumber(lineNumber);
                var offset = line.Offset + Math.Min(column, line.Length);
                JamlEditor.CaretOffset = offset;
                JamlEditor.TextArea.Caret.BringCaretToView();
            }
            catch
            {
                // Silently ignore if line doesn't exist
            }
        }
    }
}
