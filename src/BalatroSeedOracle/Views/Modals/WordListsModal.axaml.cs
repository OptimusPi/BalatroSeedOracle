using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using BalatroSeedOracle.Helpers;

namespace BalatroSeedOracle.Views.Modals
{
    /// <summary>
    /// Word lists modal - manages seed keyword lists.
    /// Uses direct x:Name field access (no FindControl anti-pattern).
    /// File I/O logic kept in code-behind (platform-specific).
    /// </summary>
    public partial class WordListsModal : UserControl
    {
        private string _wordListsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "WordLists"
        );
        private string? _currentFile;
        private bool _hasUnsavedChanges = false;

        public WordListsModal()
        {
            InitializeComponent();
            
            // Wire up events using direct x:Name field access
            FileSelector.SelectionChanged += OnFileSelectionChanged;
            TextEditor.TextChanged += (s, e) =>
            {
                _hasUnsavedChanges = true;
                UpdateStatus("Modified - click Save to persist changes");
            };
            
            EnsureDirectoryExists();
            LoadFileList();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_wordListsPath))
            {
                Directory.CreateDirectory(_wordListsPath);
                // Create default files
                CreateDefaultFiles();
            }
        }

        private void CreateDefaultFiles()
        {
            // Create default keywords.txt
            var keywordsPath = Path.Combine(_wordListsPath, "keywords.txt");
            if (!File.Exists(keywordsPath))
            {
                File.WriteAllText(
                    keywordsPath,
                    @"# Keywords for filter matching
# One word or phrase per line
# Lines starting with # are comments

joker
tarot
spectral
voucher
boss
tag"
                );
            }

            // Create default banned_words.txt
            var bannedPath = Path.Combine(_wordListsPath, "banned_words.txt");
            if (!File.Exists(bannedPath))
            {
                File.WriteAllText(
                    bannedPath,
                    @"# Words to exclude from filters
# One word per line
"
                );
            }
        }

        private void LoadFileList()
        {
            FileSelector.Items.Clear();

            var files = Directory
                .GetFiles(_wordListsPath, "*.db")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                FileSelector.Items.Add(new ComboBoxItem { Content = file });
            }

            if (FileSelector.Items.Count > 0)
            {
                FileSelector.SelectedIndex = 0;
            }
        }

        private async void OnFileSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (FileSelector.SelectedItem is ComboBoxItem item && item.Content is string fileName)
                {
                    if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentFile))
                    {
                        var result = await ShowSavePrompt();
                        if (result)
                        {
                            SaveCurrentFile();
                        }
                    }

                    LoadFile(fileName);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("WordListsModal", $"Error in OnFileSelectionChanged: {ex.Message}");
                UpdateStatus($"Error loading file: {ex.Message}");
            }
        }

        private void LoadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_wordListsPath, fileName);
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);

                    if (fileInfo.Length > 1_000_000) // 1MB
                    {
                        var lines = File.ReadLines(filePath).Take(10000).ToList();
                        TextEditor.Text =
                            string.Join("\n", lines)
                            + $"\n\n... (showing first {lines.Count:N0} lines only)\n"
                            + $"Large file ({fileInfo.Length / 1024:N0} KB) - read-only preview.\n"
                            + $"Use external text editor to view/edit full file.";
                        TextEditor.IsReadOnly = true;
                        UpdateStatus($"Large file - showing preview only (read-only)");
                    }
                    else
                    {
                        TextEditor.Text = File.ReadAllText(filePath);
                        TextEditor.IsReadOnly = false;
                        UpdateStatus($"Loaded: {fileName}");
                    }

                    _currentFile = fileName;
                    _hasUnsavedChanges = false;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading file: {ex.Message}");
            }
        }

        private void OnNewClick(object? sender, RoutedEventArgs e)
        {
            var newFileName = $"custom_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            var newFilePath = Path.Combine(_wordListsPath, newFileName);

            try
            {
                File.WriteAllText(newFilePath, "# New word list\n# One word or phrase per line\n\n");
                LoadFileList();

                // Select the new file
                for (int i = 0; i < FileSelector.Items.Count; i++)
                {
                    if (FileSelector.Items[i] is ComboBoxItem item && item.Content?.ToString() == newFileName)
                    {
                        FileSelector.SelectedIndex = i;
                        break;
                    }
                }

                UpdateStatus($"Created: {newFileName}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error creating file: {ex.Message}");
            }
        }

        private async void OnPasteClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard == null)
                {
                    UpdateStatus("Clipboard not available");
                    return;
                }

                var clipboardText = await topLevel.Clipboard.TryGetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    TextEditor.Text = clipboardText;
                    _hasUnsavedChanges = true;
                    UpdateStatus("Pasted from clipboard - click Save to persist");
                }
                else
                {
                    UpdateStatus("Clipboard is empty");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error pasting: {ex.Message}");
            }
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFile))
                return;

            try
            {
                var filePath = Path.Combine(_wordListsPath, _currentFile);
                File.WriteAllText(filePath, TextEditor.Text);
                _hasUnsavedChanges = false;
                UpdateStatus($"Saved: {_currentFile}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving file: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        private Task<bool> ShowSavePrompt()
        {
            return Task.FromResult(false);
        }
    }
}
