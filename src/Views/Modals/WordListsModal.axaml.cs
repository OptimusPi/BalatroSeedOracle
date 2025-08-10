using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Oracle.Views.Modals
{
    public partial class WordListsModal : UserControl
    {
        private ComboBox? _fileSelector;
        private TextBox? _textEditor;
        private TextBlock? _statusText;
        private Button? _saveButton;

        private string _wordListsPath = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "WordLists"
        );
        private string? _currentFile;
        private bool _hasUnsavedChanges = false;

        public WordListsModal()
        {
            InitializeComponent();
            EnsureDirectoryExists();
            LoadFileList();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _fileSelector = this.FindControl<ComboBox>("FileSelector");
            _textEditor = this.FindControl<TextBox>("TextEditor");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _saveButton = this.FindControl<Button>("SaveButton");

            if (_fileSelector != null)
            {
                _fileSelector.SelectionChanged += OnFileSelectionChanged;
            }

            if (_textEditor != null)
            {
                _textEditor.TextChanged += (s, e) =>
                {
                    _hasUnsavedChanges = true;
                    UpdateStatus("Modified - click Save to persist changes");
                };
            }

            // Load the first file by default
            if (_fileSelector != null && _fileSelector.Items.Count > 0)
            {
                _fileSelector.SelectedIndex = 0;
            }
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

test
debug
temp"
                );
            }
        }

        private void LoadFileList()
        {
            if (_fileSelector == null)
                return;

            _fileSelector.Items.Clear();

            var files = Directory
                .GetFiles(_wordListsPath, "*.txt")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                _fileSelector.Items.Add(new ComboBoxItem { Content = file });
            }

            if (_fileSelector.Items.Count > 0)
            {
                _fileSelector.SelectedIndex = 0;
            }
        }

        private async void OnFileSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_fileSelector?.SelectedItem is ComboBoxItem item && item.Content is string fileName)
            {
                if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentFile))
                {
                    // Ask to save changes
                    var result = await ShowSavePrompt();
                    if (result)
                    {
                        SaveCurrentFile();
                    }
                }

                LoadFile(fileName);
            }
        }

        private void LoadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_wordListsPath, fileName);
                if (File.Exists(filePath) && _textEditor != null)
                {
                    _textEditor.Text = File.ReadAllText(filePath);
                    _currentFile = fileName;
                    _hasUnsavedChanges = false;
                    UpdateStatus($"Loaded: {fileName}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading file: {ex.Message}");
            }
        }

        private void OnNewClick(object? sender, RoutedEventArgs e)
        {
            var newFileName = $"custom_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var newFilePath = Path.Combine(_wordListsPath, newFileName);

            try
            {
                File.WriteAllText(
                    newFilePath,
                    "# New word list\n# One word or phrase per line\n\n"
                );
                LoadFileList();

                // Select the new file
                if (_fileSelector != null)
                {
                    for (int i = 0; i < _fileSelector.Items.Count; i++)
                    {
                        if (
                            _fileSelector.Items[i] is ComboBoxItem item
                            && item.Content?.ToString() == newFileName
                        )
                        {
                            _fileSelector.SelectedIndex = i;
                            break;
                        }
                    }
                }

                UpdateStatus($"Created: {newFileName}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error creating file: {ex.Message}");
            }
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFile) || _textEditor == null)
                return;

            try
            {
                var filePath = Path.Combine(_wordListsPath, _currentFile);
                File.WriteAllText(filePath, _textEditor.Text);
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
            if (_statusText != null)
            {
                _statusText.Text = message;
            }
        }

        private Task<bool> ShowSavePrompt()
        {
            // For now, just return false (don't save)
            // In a real app, you'd show a dialog
            return Task.FromResult(false);
        }
    }
}
