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

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// YAML Editor Tab for filter editing.
    /// YAML (Joker Ante Markup Language) is a YAML-based format for Balatro filters.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// </summary>
    public partial class YAMLEditorTab : UserControl
    {
        private FoldingManager? _foldingManager;
        private readonly JamlFoldingStrategy _foldingStrategy = new();
        private JamlErrorMarkerService? _errorMarkerService;
        private JamlHoverTooltipService? _hoverTooltipService;
        private JamlCodeSnippetService? _snippetService;
        private CompletionWindow? _completionWindow;

        // Tracked separately from ViewModel/DataContext so a reassignment unsubscribes the
        // actual previous ViewModel instead of leaking a subscription to it.
        private JamlEditorTabViewModel? _subscribedViewModel;

        public JamlEditorTabViewModel? ViewModel => DataContext as JamlEditorTabViewModel;

        public YAMLEditorTab()
        {
            InitializeComponent();
            SetupEditor();
        }

        private void SetupEditor()
        {
            // Direct x:Name field access - no FindControl!
            // Load custom dark mode syntax highlighting (YAML uses YAML syntax)
            LoadCustomYAMLSyntaxHighlighting();

            // Install code folding
            InstallCodeFolding();

            // When ViewModel changes, update editor
            DataContextChanged += (s, e) =>
            {
                if (_subscribedViewModel != null)
                {
                    _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                    _subscribedViewModel.JumpToError -= OnJumpToError;
                    _subscribedViewModel = null;
                }

                if (ViewModel != null)
                {
                    YAMLEditor.Text = ViewModel.YamlContent;

                    // Subscribe to ViewModel property changes
                    ViewModel.PropertyChanged += OnViewModelPropertyChanged;

                    // Subscribe to jump to error event
                    ViewModel.JumpToError += OnJumpToError;

                    _subscribedViewModel = ViewModel;
                }
            };

            // When editor text changes, update ViewModel
            YAMLEditor.TextChanged += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.YamlContent = YAMLEditor.Text ?? "";

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
            if (YAMLEditor?.TextArea == null)
                return;

            // Configure editor options
            YAMLEditor.Options.IndentationSize = 2;
            YAMLEditor.Options.ConvertTabsToSpaces = true;
            // Note: HighlightCurrentLine and EnableBracketMatching may not be available in this version of AvaloniaEdit
            // These features are handled by the TextArea.TextView rendering

            // Install error marker service
            _errorMarkerService = new JamlErrorMarkerService(YAMLEditor);
            YAMLEditor.TextArea.TextView.BackgroundRenderers.Add(_errorMarkerService);

            // Install hover tooltips
            _hoverTooltipService = new JamlHoverTooltipService(YAMLEditor);
            _hoverTooltipService.Install();

            // Install code snippets
            _snippetService = new JamlCodeSnippetService(YAMLEditor);

            // Handle Tab for snippet expansion
            YAMLEditor.TextArea.KeyDown += (s, e) =>
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
            YAMLEditor.TextArea.TextView.PointerPressed += OnMouseDown;

            // Subscribe to text changes for validation
            YAMLEditor.TextChanged += OnEditorTextChanged;
        }

        private void OnMouseDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (YAMLEditor?.TextArea?.Caret == null || YAMLEditor.Document == null)
                return;

            var point = e.GetCurrentPoint(YAMLEditor);
            if (
                point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            )
            {
                // Use caret position instead of mouse position for simplicity
                var offset = YAMLEditor.CaretOffset;
                GoToDefinition(offset);
            }
        }

        private void GoToDefinition(int offset)
        {
            if (YAMLEditor?.Document == null)
                return;

            var line = YAMLEditor.Document.GetLineByOffset(offset);
            var lineText = YAMLEditor.Document.GetText(line.Offset, line.Length);
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
            if (YAMLEditor?.Document == null)
                return;

            var text = YAMLEditor.Document.Text;
            var pattern = $@"(\w+)\s*:\s*&{Regex.Escape(anchorName)}\s+";
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);

            if (match.Success)
            {
                var lineNumber = YAMLEditor.Document.GetLineByOffset(match.Index).LineNumber;
                var line = YAMLEditor.Document.GetLineByNumber(lineNumber);
                YAMLEditor.CaretOffset = line.Offset;
                YAMLEditor.TextArea.Caret.BringCaretToView();
            }
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            // Validate and mark errors
            if (YAMLEditor?.Document != null && ViewModel != null)
            {
                ValidateAndMarkErrors();
            }
        }

        private void ValidateAndMarkErrors()
        {
            if (_errorMarkerService is null || YAMLEditor?.Document is null || ViewModel is null)
                return;

            _errorMarkerService.ClearErrors();

            var yamlContent = YAMLEditor.Document.Text;
            if (string.IsNullOrWhiteSpace(yamlContent))
                return;

            // YAML validation via the engine's own loader (one grammar, no YAML)
            if (!JamlConfigLoader.TryLoad(yamlContent, out _, out var error) && error is not null)
            {
                var lineMatch = Regex.Match(error, @"line (\d+)");
                var lineNumber =
                    lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var parsed)
                        ? parsed
                        : 1;
                _errorMarkerService.AddError(
                    lineNumber,
                    0,
                    50,
                    error,
                    JamlErrorMarkerService.ErrorSeverity.Error
                );
            }
            else
            {
                // Check for undefined anchor references
                CheckAnchorReferences(yamlContent);
            }

            _errorMarkerService.UpdateErrors();
        }

        private void CheckAnchorReferences(string yamlContent)
        {
            if (_errorMarkerService is null || YAMLEditor?.Document is null)
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
                    var lineNumber = YAMLEditor.Document.GetLineByOffset(match.Index).LineNumber;
                    var column =
                        match.Index - YAMLEditor.Document.GetLineByNumber(lineNumber).Offset;
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
            if (YAMLEditor?.TextArea == null)
                return;

            // Handle Ctrl+Space for autocomplete
            YAMLEditor.TextArea.KeyDown += OnKeyDown;

            // Handle text input for auto-trigger
            YAMLEditor.TextArea.TextEntered += OnTextEntered;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+Space triggers autocomplete
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowYAMLCompletions();
                e.Handled = true;
            }
        }

        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (YAMLEditor?.TextArea == null)
                return;

            // Open completion after "- " / "key: " / "[" / ", ", and on the first letter of a word
            if (_completionWindow is null)
            {
                var offset = YAMLEditor.CaretOffset;
                var before = YAMLEditor.Document.GetText(0, offset);
                if (JamlLanguage.ShouldAutoTrigger(before, e.Text ?? ""))
                    ShowYAMLCompletions();
            }
        }

        private void ShowYAMLCompletions()
        {
            if (YAMLEditor?.TextArea == null)
                return;

            // Get text before cursor for context-aware completions
            var offset = YAMLEditor.CaretOffset;
            var textBeforeCursor = YAMLEditor.Text.Substring(
                0,
                Math.Min(offset, YAMLEditor.Text.Length)
            );

            // Get SMART context-aware completions
            var smartCompletions = JamlAutocompletionHelper.GetCompletionsForContext(
                textBeforeCursor
            );

            if (smartCompletions.Count == 0)
                return; // No completions available

            var completionWindow = new CompletionWindow(YAMLEditor.TextArea);

            // The completion replaces the identifier being typed (not a list "-" or a key's ":")
            var wordStart = JamlLanguage.TokenStart(textBeforeCursor, offset);
            completionWindow.StartOffset = wordStart;
            completionWindow.EndOffset = offset;

            var data = completionWindow.CompletionList.CompletionData;

            // Add all smart completions
            foreach (var completion in smartCompletions)
            {
                data.Add(completion);
            }

            _completionWindow = completionWindow;
            completionWindow.Closed += (o, args) => _completionWindow = null;
            completionWindow.Show();
        }

        private void LoadCustomYAMLSyntaxHighlighting()
        {
            if (YAMLEditor == null)
                return;

            try
            {
                // Load custom YAML dark mode syntax highlighting (uses YAML highlighting since YAML is YAML-based)
                var xshdPath = Path.Combine(AppContext.BaseDirectory, "Resources", "YAMLDark.xshd");

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
                        YAMLEditor.SyntaxHighlighting = definition;
                    }
                    DebugLogger.Log(
                        "YAMLEditorTab",
                        "Custom YAML dark mode syntax highlighting loaded"
                    );
                }
                else
                {
                    DebugLogger.LogError(
                        "YAMLEditorTab",
                        $"YAMLDark.xshd not found at {xshdPath}, using default"
                    );
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "YAMLEditorTab",
                    $"Failed to load custom syntax highlighting: {ex.Message}"
                );
            }
        }

        private void InstallCodeFolding()
        {
            if (YAMLEditor?.TextArea == null)
                return;

            try
            {
                // Install folding manager
                _foldingManager = FoldingManager.Install(YAMLEditor.TextArea);

                // Initial folding update
                UpdateCodeFolding();

                DebugLogger.Log("YAMLEditorTab", "Code folding installed successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(
                    "YAMLEditorTab",
                    $"Failed to install code folding: {ex.Message}"
                );
            }
        }

        private void UpdateCodeFolding()
        {
            if (_foldingManager == null || YAMLEditor?.Document == null)
                return;

            try
            {
                // Update foldings using brace folding strategy (handles [] for YAML arrays)
                _foldingStrategy.UpdateFoldings(_foldingManager, YAMLEditor.Document);
            }
            catch (Exception ex)
            {
                // Silently ignore folding errors to avoid disrupting editing
                DebugLogger.LogError("YAMLEditorTab", $"Error updating foldings: {ex.Message}");
            }
        }

        protected override void OnDetachedFromVisualTree(
            Avalonia.VisualTreeAttachmentEventArgs e
        )
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _subscribedViewModel.JumpToError -= OnJumpToError;
                _subscribedViewModel = null;
            }
            base.OnDetachedFromVisualTree(e);
        }

        private void OnViewModelPropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            if (
                e.PropertyName == nameof(JamlEditorTabViewModel.YamlContent)
                && YAMLEditor != null
                && ViewModel != null
            )
            {
                // Only update if different to avoid infinite loop
                if (YAMLEditor.Text != ViewModel.YamlContent)
                {
                    YAMLEditor.Text = ViewModel.YamlContent;
                }
            }
        }

        private void OnJumpToError(int lineNumber, int column)
        {
            if (YAMLEditor?.Document == null)
                return;

            try
            {
                var line = YAMLEditor.Document.GetLineByNumber(lineNumber);
                var offset = line.Offset + Math.Min(column, line.Length);
                YAMLEditor.CaretOffset = offset;
                YAMLEditor.TextArea.Caret.BringCaretToView();
            }
            catch
            {
                // Silently ignore if line doesn't exist
            }
        }
    }
}
