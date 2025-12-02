using System;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.ViewModels.FilterTabs;

namespace BalatroSeedOracle.Components.FilterTabs
{
    /// <summary>
    /// JAML Editor Tab for filter editing
    /// JAML (Joker Ante Markup Language) is a YAML-based format for Balatro filters
    /// Minimal code-behind, all logic in JamlEditorTabViewModel
    /// </summary>
    public partial class JamlEditorTab : UserControl
    {
        private TextEditor? _jamlEditor;
        private FoldingManager? _foldingManager;
        private readonly BraceFoldingStrategy _foldingStrategy = new();

        public JamlEditorTabViewModel? ViewModel => DataContext as JamlEditorTabViewModel;

        public JamlEditorTab()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Get reference to the TextEditor
            _jamlEditor = this.FindControl<TextEditor>("JamlEditor");

            // Set up two-way binding for TextEditor
            if (_jamlEditor != null)
            {
                // Load custom dark mode syntax highlighting (JAML uses YAML syntax)
                LoadCustomJamlSyntaxHighlighting();

                // Install code folding
                InstallCodeFolding();

                // When ViewModel changes, update editor
                DataContextChanged += (s, e) =>
                {
                    if (ViewModel != null)
                    {
                        _jamlEditor.Text = ViewModel.JamlContent;

                        // Subscribe to ViewModel property changes
                        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
                    }
                };

                // When editor text changes, update ViewModel
                _jamlEditor.TextChanged += (s, e) =>
                {
                    if (ViewModel != null)
                    {
                        ViewModel.JamlContent = _jamlEditor.Text ?? "";

                        // Update code folding when document changes
                        UpdateCodeFolding();
                    }
                };
            }
        }

        private void LoadCustomJamlSyntaxHighlighting()
        {
            if (_jamlEditor == null)
                return;

            try
            {
                // Load custom JAML dark mode syntax highlighting (uses YAML highlighting since JAML is YAML-based)
                var xshdPath = Path.Combine(AppContext.BaseDirectory, "Resources", "JamlDark.xshd");

                // Fallback to YamlDark.xshd for backwards compatibility
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
                        _jamlEditor.SyntaxHighlighting = definition;
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
            if (_jamlEditor?.TextArea == null)
                return;

            try
            {
                // Install folding manager
                _foldingManager = FoldingManager.Install(_jamlEditor.TextArea);

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
            if (_foldingManager == null || _jamlEditor?.Document == null)
                return;

            try
            {
                // Update foldings using brace folding strategy (handles [] for JAML arrays)
                _foldingStrategy.UpdateFoldings(_foldingManager, _jamlEditor.Document);
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
                && _jamlEditor != null
                && ViewModel != null
            )
            {
                // Only update if different to avoid infinite loop
                if (_jamlEditor.Text != ViewModel.JamlContent)
                {
                    _jamlEditor.Text = ViewModel.JamlContent;
                }
            }
        }
    }
}
