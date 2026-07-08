using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BalatroSeedOracle.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalatroSeedOracle.ViewModels
{
    /// <summary>
    /// ViewModel for the Word Lists modal - manages seed keyword list files.
    /// </summary>
    public partial class WordListsModalViewModel : ObservableObject
    {
        private readonly string _wordListsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "WordLists"
        );

        private string? _currentFile;
        private bool _hasUnsavedChanges;
        private bool _isLoadingFile;

        public ObservableCollection<string> Files { get; } = new();

        [ObservableProperty]
        private string? _selectedFile;

        [ObservableProperty]
        private string _editorText = string.Empty;

        [ObservableProperty]
        private bool _isEditorReadOnly;

        [ObservableProperty]
        private string _statusMessage = "Edit word lists used by filters";

        public WordListsModalViewModel()
        {
            EnsureDirectoryExists();
            LoadFileList();
        }

        partial void OnSelectedFileChanged(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (_hasUnsavedChanges && !string.IsNullOrEmpty(_currentFile))
            {
                // No save prompt implemented yet - matches previous behavior (changes discarded)
            }

            LoadFile(value);
        }

        partial void OnEditorTextChanged(string value)
        {
            if (_isLoadingFile)
                return;

            _hasUnsavedChanges = true;
            StatusMessage = "Modified - click Save to persist changes";
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_wordListsPath))
            {
                Directory.CreateDirectory(_wordListsPath);
                CreateDefaultFiles();
            }
        }

        private void CreateDefaultFiles()
        {
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
            Files.Clear();

            var files = Directory
                .GetFiles(_wordListsPath, "*.db")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f);

            foreach (var file in files)
            {
                Files.Add(file);
            }

            if (Files.Count > 0)
            {
                SelectedFile = Files[0];
            }
        }

        private void LoadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_wordListsPath, fileName);
                if (!File.Exists(filePath))
                    return;

                var fileInfo = new FileInfo(filePath);
                _isLoadingFile = true;
                try
                {
                    if (fileInfo.Length > 1_000_000) // 1MB
                    {
                        var lines = File.ReadLines(filePath).Take(10000).ToList();
                        EditorText =
                            string.Join("\n", lines)
                            + $"\n\n... (showing first {lines.Count:N0} lines only)\n"
                            + $"Large file ({fileInfo.Length / 1024:N0} KB) - read-only preview.\n"
                            + $"Use external text editor to view/edit full file.";
                        IsEditorReadOnly = true;
                        StatusMessage = "Large file - showing preview only (read-only)";
                    }
                    else
                    {
                        EditorText = File.ReadAllText(filePath);
                        IsEditorReadOnly = false;
                        StatusMessage = $"Loaded: {fileName}";
                    }
                }
                finally
                {
                    _isLoadingFile = false;
                }

                _currentFile = fileName;
                _hasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("WordListsModalViewModel", $"Error loading file: {ex.Message}");
                StatusMessage = $"Error loading file: {ex.Message}";
            }
        }

        [RelayCommand]
        private void NewFile()
        {
            var newFileName = $"custom_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
            var newFilePath = Path.Combine(_wordListsPath, newFileName);

            try
            {
                File.WriteAllText(
                    newFilePath,
                    "# New word list\n# One word or phrase per line\n\n"
                );
                LoadFileList();

                if (Files.Contains(newFileName))
                {
                    SelectedFile = newFileName;
                }

                StatusMessage = $"Created: {newFileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating file: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrEmpty(_currentFile))
                return;

            try
            {
                var filePath = Path.Combine(_wordListsPath, _currentFile);
                File.WriteAllText(filePath, EditorText);
                _hasUnsavedChanges = false;
                StatusMessage = $"Saved: {_currentFile}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving file: {ex.Message}";
            }
        }

        /// <summary>
        /// Applies clipboard text pasted by the view (clipboard access is a view concern).
        /// </summary>
        public void PasteText(string text)
        {
            EditorText = text;
            _hasUnsavedChanges = true;
            StatusMessage = "Pasted from clipboard - click Save to persist";
        }

        public void ReportStatus(string message)
        {
            StatusMessage = message;
        }
    }
}
